using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 세이브/로드 매니저.
/// 모든 게임 상태를 JSON으로 직렬화하여 파일에 저장.
/// 자동 저장: 안전가옥 진입 시, 휴식 시, 레이드 귀환 시.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    const string SAVE_FILE = "save.json";
    const int SAVE_VERSION = 1;

    string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ═══════════════════════════
    //  저장
    // ═══════════════════════════

    /// <summary>
    /// 전체 게임 상태를 파일에 저장.
    /// </summary>
    public void Save()
    {
        var data = new GameSaveData();
        data.version = SAVE_VERSION;
        data.saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 퀘스트
        if (QuestManager.Instance != null)
        {
            data.completedQuests = QuestManager.Instance.CompletedQuestIds.ToList();
            data.flags = new List<FlagEntry>();
            foreach (var kvp in QuestManager.Instance.GetAllFlags())
            {
                data.flags.Add(new FlagEntry { key = kvp.Key, value = kvp.Value });
            }

            data.activeQuests = new List<ActiveQuestEntry>();
            foreach (var q in QuestManager.Instance.ActiveQuests)
            {
                var entry = new ActiveQuestEntry
                {
                    questId = q.data.questId,
                    state = (int)q.state,
                    progress = new List<ProgressEntry>()
                };
                foreach (var kvp in q.progress)
                    entry.progress.Add(new ProgressEntry { index = kvp.Key, count = kvp.Value });
                data.activeQuests.Add(entry);
            }
        }

        // NPC 관계
        if (NPCRelationshipManager.Instance != null)
        {
            data.relationships = NPCRelationshipManager.Instance.GetAllSaveData();
        }

        // 업적
        if (AchievementManager.Instance != null)
        {
            data.achievements = AchievementManager.Instance.GetSaveData();
        }

        // 화폐(스크랩)
        if (CurrencyManager.Instance != null)
        {
            data.currency = CurrencyManager.Instance.GetSaveData();
        }

        // 평판(전역 명성)
        if (ReputationManager.Instance != null)
        {
            data.reputation = ReputationManager.Instance.GetSaveData();
        }

        // 하이드아웃 모듈 레벨
        if (HideoutModuleManager.Instance != null)
        {
            data.hideoutModules = HideoutModuleManager.Instance.GetSaveData();
        }

        // 인벤토리(가방) + 장착 무기
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            var inv = playerGO.GetComponent<PlayerInventory>();
            if (inv != null && inv.Grid != null) data.bagItems = inv.Grid.GetSaveData();
            var eq = playerGO.GetComponent<PlayerEquipment>();
            if (eq != null) data.equippedWeapon = eq.GetSaveData();
        }

        // 창고 (안전가옥 가구 — static 목록)
        data.storageUnits = new List<StorageUnitEntry>();
        foreach (var f in SafehouseStorage.AllFurniture)
        {
            if (f == null || f.grid == null) continue;
            data.storageUnits.Add(new StorageUnitEntry { uid = f.uid, items = f.grid.GetSaveData() });
        }

        // 일일 의뢰
        if (DailyQuestManager.Instance != null)
        {
            data.dailyQuest = DailyQuestManager.Instance.GetSaveData();
        }

        // 튜토리얼
        if (TutorialPrompt.Instance != null)
        {
            data.shownTutorials = TutorialPrompt.Instance.GetShownIds().ToList();
        }

        // 스토리 재생 기록
        if (StoryPlayer.Instance != null)
        {
            data.playedScenes = StoryPlayer.Instance.GetPlayedScenes().ToList();
        }

        // 직렬화 & 저장
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[Save] 저장 완료: {SavePath}");
    }

    // ═══════════════════════════
    //  로드
    // ═══════════════════════════

    /// <summary>
    /// 세이브 파일에서 게임 상태 복원.
    /// </summary>
    public bool Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[Save] 세이브 파일 없음. 새 게임.");
            return false;
        }

        string json = File.ReadAllText(SavePath);
        var data = JsonUtility.FromJson<GameSaveData>(json);

        if (data == null || data.version != SAVE_VERSION)
        {
            Debug.LogWarning("[Save] 세이브 버전 불일치. 새 게임으로 시작.");
            return false;
        }

        // 퀘스트
        if (QuestManager.Instance != null)
        {
            // 완료 퀘스트 복원
            foreach (var qid in data.completedQuests)
                QuestManager.Instance.CompletedQuestIds.Add(qid);

            // 플래그 복원
            if (data.flags != null)
            {
                foreach (var f in data.flags)
                    QuestManager.Instance.SetFlag(f.key, f.value);
            }

            // 진행 중 퀘스트는 아래 playedScenes 복원 후 처리
        }

        // NPC 관계
        if (NPCRelationshipManager.Instance != null && data.relationships != null)
        {
            NPCRelationshipManager.Instance.LoadAllSaveData(data.relationships);
        }

        // 업적
        if (AchievementManager.Instance != null && data.achievements != null)
        {
            AchievementManager.Instance.LoadSaveData(data.achievements);
        }

        // 화폐(스크랩)
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.LoadSaveData(data.currency);
        }

        // 평판(전역 명성)
        if (ReputationManager.Instance != null)
        {
            ReputationManager.Instance.LoadSaveData(data.reputation);
        }

        // 하이드아웃 모듈 레벨
        if (HideoutModuleManager.Instance != null)
        {
            HideoutModuleManager.Instance.LoadSaveData(data.hideoutModules);
        }

        // 인벤토리(가방) + 장착 무기
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            var inv = playerGO.GetComponent<PlayerInventory>();
            if (inv != null && inv.Grid != null && data.bagItems != null)
                inv.Grid.LoadSaveData(data.bagItems);
            var eq = playerGO.GetComponent<PlayerEquipment>();
            if (eq != null) eq.LoadSaveData(data.equippedWeapon);
        }

        // 창고 (uid 매칭)
        if (data.storageUnits != null)
        {
            foreach (var e in data.storageUnits)
            {
                var f = SafehouseStorage.AllFurniture.FirstOrDefault(x => x != null && x.uid == e.uid);
                if (f != null && f.grid != null) f.grid.LoadSaveData(e.items);
            }
        }

        // 일일 의뢰
        if (DailyQuestManager.Instance != null && data.dailyQuest != null)
        {
            DailyQuestManager.Instance.LoadSaveData(data.dailyQuest);
        }

        // 튜토리얼
        if (TutorialPrompt.Instance != null && data.shownTutorials != null)
        {
            TutorialPrompt.Instance.SetShownIds(new HashSet<string>(data.shownTutorials));
        }

        // 스토리 재생 기록
        if (StoryPlayer.Instance != null && data.playedScenes != null)
        {
            StoryPlayer.Instance.SetPlayedScenes(new HashSet<string>(data.playedScenes));
        }

        // 진행 중 퀘스트 복원
        if (QuestManager.Instance != null && data.activeQuests != null)
        {
            foreach (var entry in data.activeQuests)
            {
                var questData = Resources.Load<QuestData>($"Data/Quests/{entry.questId}");
                if (questData == null) continue;

                if (QuestManager.Instance.AcceptQuest(questData))
                {
                    var active = QuestManager.Instance.ActiveQuests
                        .Find(q => q.data.questId == entry.questId);
                    if (active != null)
                    {
                        active.state = (QuestState)entry.state;
                        if (entry.progress != null)
                        {
                            foreach (var p in entry.progress)
                                active.progress[p.index] = p.count;
                        }
                    }
                }
            }
        }

        Debug.Log($"[Save] 로드 완료. 저장 시각: {data.saveTime}");
        return true;
    }

    /// <summary>
    /// 세이브 파일 존재 여부.
    /// </summary>
    public bool HasSave()
    {
        return File.Exists(SavePath);
    }

    /// <summary>
    /// 세이브 파일 삭제 (새 게임 시작).
    /// </summary>
    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[Save] 세이브 파일 삭제.");
        }
    }

    // ═══════════════════════════
    //  자동 저장 트리거
    // ═══════════════════════════

    /// <summary>
    /// 안전가옥 진입, 휴식, 레이드 귀환 시 호출.
    /// </summary>
    public void AutoSave()
    {
        Save();
    }

}

// ═══════════════════════════
//  세이브 데이터 구조
// ═══════════════════════════

[System.Serializable]
public class GameSaveData
{
    public int version;
    public string saveTime;

    // 퀘스트
    public List<string> completedQuests = new List<string>();
    public List<FlagEntry> flags = new List<FlagEntry>();
    public List<ActiveQuestEntry> activeQuests = new List<ActiveQuestEntry>();

    // NPC 관계
    public List<NPCRelationshipSaveEntry> relationships;

    // 업적
    public AchievementManager.AchievementSaveData achievements;

    // 화폐(스크랩)
    public int currency;

    // 평판(전역 명성)
    public int reputation;

    // 하이드아웃 모듈 레벨
    public List<HideoutModuleSaveEntry> hideoutModules;

    // 인벤토리(가방) + 장착 무기
    public List<GridItemEntry> bagItems = new List<GridItemEntry>();
    public string equippedWeapon;

    // 창고 (안전가옥 가구)
    public List<StorageUnitEntry> storageUnits = new List<StorageUnitEntry>();

    // 일일 의뢰
    public DailyQuestManager.DailyQuestSaveData dailyQuest;

    // 튜토리얼
    public List<string> shownTutorials = new List<string>();

    // 스토리
    public List<string> playedScenes = new List<string>();
}

[System.Serializable]
public class GridItemEntry
{
    public string itemId;
    public int count;
    public float durability;
    public int x;
    public int y;
    public bool rotated;
}

[System.Serializable]
public class StorageUnitEntry
{
    public int uid;
    public List<GridItemEntry> items = new List<GridItemEntry>();
}

[System.Serializable]
public class FlagEntry
{
    public string key;
    public bool value;
}

[System.Serializable]
public class ActiveQuestEntry
{
    public string questId;
    public int state;
    public List<ProgressEntry> progress = new List<ProgressEntry>();
}

[System.Serializable]
public class ProgressEntry
{
    public int index;
    public int count;
}

[System.Serializable]
public class NPCRelationshipSaveEntry
{
    public string npcId;
    public int affinity;
    public int trust;
    public int fear;
    public List<string> completedEvents = new List<string>();
    public List<FlagEntry> flags = new List<FlagEntry>();
}
