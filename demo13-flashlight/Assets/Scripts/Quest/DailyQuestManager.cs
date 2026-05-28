using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 일일 의뢰 시스템.
/// 전당포 주인의 반복 의뢰를 1일 1개 랜덤 제공.
/// QuestManager와 연동하여 수주/완료 처리.
/// </summary>
public class DailyQuestManager : MonoBehaviour
{
    public static DailyQuestManager Instance { get; private set; }

    [Header("일일 의뢰 풀")]
    [Tooltip("Resources/Data/DailyQuests/ 에서 자동 로드")]
    [SerializeField] QuestData[] dailyQuestPool;

    [Header("설정")]
    [Tooltip("의뢰 갱신 기준 (실시간 0시 기준)")]
    [SerializeField] bool useRealTimeDays = true;

    // 상태
    string currentDailyQuestId;
    bool completedToday;
    int lastResetDay; // DayOfYear 기반
    string lastQuestId; // 연속 중복 방지

    /// <summary>오늘 의뢰가 있는지</summary>
    public bool HasDailyQuest => !string.IsNullOrEmpty(currentDailyQuestId) && !completedToday;

    /// <summary>오늘 의뢰를 이미 완료했는지</summary>
    public bool IsCompletedToday => completedToday;

    /// <summary>현재 일일 의뢰 데이터</summary>
    public QuestData CurrentQuest => GetCurrentQuestData();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadQuestPool();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        CheckDailyReset();
    }

    void LoadQuestPool()
    {
        if (dailyQuestPool == null || dailyQuestPool.Length == 0)
        {
            dailyQuestPool = Resources.LoadAll<QuestData>("Data/DailyQuests");
            if (dailyQuestPool.Length == 0)
                Debug.LogWarning("[DailyQuest] Resources/Data/DailyQuests/ 에 퀘스트 데이터가 없습니다.");
        }
    }

    /// <summary>
    /// 날짜가 바뀌었으면 새 의뢰를 뽑는다.
    /// 안전가옥 진입 시, 휴식 시 호출.
    /// </summary>
    public void CheckDailyReset()
    {
        int today = GetCurrentDay();
        if (today != lastResetDay)
        {
            lastResetDay = today;
            completedToday = false;
            PickNewQuest();
        }
    }

    void PickNewQuest()
    {
        if (dailyQuestPool == null || dailyQuestPool.Length == 0) return;

        // 직전 의뢰 제외
        var candidates = dailyQuestPool
            .Where(q => q.questId != lastQuestId)
            .ToArray();

        if (candidates.Length == 0)
            candidates = dailyQuestPool;

        var picked = candidates[Random.Range(0, candidates.Length)];
        currentDailyQuestId = picked.questId;
        lastQuestId = currentDailyQuestId;

        Debug.Log($"[DailyQuest] 오늘의 의뢰: {picked.title}");
    }

    /// <summary>
    /// 일일 의뢰 수주. QuestManager에 등록.
    /// </summary>
    public bool AcceptDailyQuest()
    {
        if (completedToday) return false;
        if (string.IsNullOrEmpty(currentDailyQuestId)) return false;

        var questData = GetCurrentQuestData();
        if (questData == null) return false;

        if (QuestManager.Instance == null) return false;

        // 이미 진행 중인 일일 의뢰가 있으면 거절
        if (QuestManager.Instance.ActiveQuests.Any(q => IsDailyQuest(q.data)))
        {
            Debug.Log("[DailyQuest] 이미 일일 의뢰 진행 중.");
            return false;
        }

        return QuestManager.Instance.AcceptQuest(questData);
    }

    /// <summary>
    /// 일일 의뢰 완료 처리. QuestManager.CompleteQuest 후 호출.
    /// </summary>
    public void OnDailyQuestCompleted(string questId)
    {
        if (questId == currentDailyQuestId)
        {
            completedToday = true;
            Debug.Log("[DailyQuest] 오늘 의뢰 완료.");
        }
    }

    QuestData GetCurrentQuestData()
    {
        if (string.IsNullOrEmpty(currentDailyQuestId)) return null;
        return dailyQuestPool?.FirstOrDefault(q => q.questId == currentDailyQuestId);
    }

    bool IsDailyQuest(QuestData data)
    {
        return dailyQuestPool != null && dailyQuestPool.Any(q => q.questId == data.questId);
    }

    int GetCurrentDay()
    {
        if (useRealTimeDays)
            return System.DateTime.Now.DayOfYear + System.DateTime.Now.Year * 366;
        else
            return Mathf.FloorToInt(Time.time / 86400f); // 게임 내 24시간
    }

    // ═══════════════════════════
    //  세이브/로드
    // ═══════════════════════════

    [System.Serializable]
    public class DailyQuestSaveData
    {
        public string currentQuestId;
        public bool completedToday;
        public int lastResetDay;
        public string lastQuestId;
    }

    public DailyQuestSaveData GetSaveData()
    {
        return new DailyQuestSaveData
        {
            currentQuestId = currentDailyQuestId,
            completedToday = completedToday,
            lastResetDay = lastResetDay,
            lastQuestId = lastQuestId,
        };
    }

    public void LoadSaveData(DailyQuestSaveData data)
    {
        if (data == null) return;
        currentDailyQuestId = data.currentQuestId;
        completedToday = data.completedToday;
        lastResetDay = data.lastResetDay;
        lastQuestId = data.lastQuestId;
        CheckDailyReset(); // 날짜가 바뀌었으면 갱신
    }
}
