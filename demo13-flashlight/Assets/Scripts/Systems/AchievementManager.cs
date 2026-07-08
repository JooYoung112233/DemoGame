using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 실적 미션(업적) 시스템.
/// 누적 통계를 추적하고, 임계값 도달 시 보상 지급.
/// quests-region1.md의 AM-xxx 실적 미션 구현.
/// </summary>
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    // ═══════════════════════════
    //  통계 키 상수
    // ═══════════════════════════

    public const string STAT_KILLS = "kills";
    public const string STAT_LOOT_COUNT = "loot_count";
    public const string STAT_RAIDS_COMPLETE = "raids_complete";
    public const string STAT_NIGHT_RAIDS = "night_raids";
    public const string STAT_RUDI_DELIVERED = "rudi_delivered";
    public const string STAT_SHOP_PURCHASES = "shop_purchases";
    public const string STAT_MERCHANT_TRADES = "merchant_trades";

    // ═══════════════════════════
    //  데이터
    // ═══════════════════════════

    Dictionary<string, int> stats = new Dictionary<string, int>();
    HashSet<string> unlockedAchievements = new HashSet<string>();
    List<AchievementDef> definitions = new List<AchievementDef>();

    public event System.Action<AchievementDef> OnAchievementUnlocked;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        RegisterAchievements();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ═══════════════════════════
    //  업적 정의 등록
    // ═══════════════════════════

    void RegisterAchievements()
    {
        // 전투
        Register("AM-001", "첫 전투", STAT_KILLS, 1, 0);
        Register("AM-002", "밴딧 사냥꾼", STAT_KILLS, 10, 100);
        Register("AM-003", "폐상가의 공포", STAT_KILLS, 30, 200);
        Register("AM-004", "무상", STAT_KILLS, 100, 500);

        // 파밍
        Register("AM-010", "수집가", STAT_LOOT_COUNT, 20, 50);
        Register("AM-011", "폐상가 전문가", STAT_LOOT_COUNT, 50, 100);
        Register("AM-012", "쓸어담기", STAT_LOOT_COUNT, 100, 200);

        // 레이드
        Register("AM-020", "귀환병", STAT_RAIDS_COMPLETE, 5, 50);
        Register("AM-021", "단골", STAT_RAIDS_COMPLETE, 15, 100);
        Register("AM-022", "야행성", STAT_NIGHT_RAIDS, 10, 100);
        Register("AM-023", "생존 전문가", STAT_RAIDS_COMPLETE, 50, 200);

        // 루디
        Register("AM-030", "첫 루디", STAT_RUDI_DELIVERED, 1, 0);
        Register("AM-031", "회수꾼", STAT_RUDI_DELIVERED, 10, 200);
        Register("AM-032", "전문 회수꾼", STAT_RUDI_DELIVERED, 30, 500);

        // 거래
        Register("AM-040", "첫 거래", STAT_SHOP_PURCHASES, 1, 50);
        Register("AM-041", "단골 손님", STAT_SHOP_PURCHASES, 10, 100);
        Register("AM-042", "떠돌이의 친구", STAT_MERCHANT_TRADES, 5, 100);
    }

    void Register(string id, string name, string statKey, int threshold, int currencyReward)
    {
        definitions.Add(new AchievementDef
        {
            id = id,
            name = name,
            statKey = statKey,
            threshold = threshold,
            currencyReward = currencyReward,
        });
    }

    // ═══════════════════════════
    //  통계 추적
    // ═══════════════════════════

    /// <summary>
    /// 통계 증가. 호출 후 자동으로 업적 체크.
    /// </summary>
    public void AddStat(string statKey, int amount = 1)
    {
        if (!stats.ContainsKey(statKey))
            stats[statKey] = 0;
        stats[statKey] += amount;

        CheckAchievements(statKey);
    }

    /// <summary>
    /// 특정 통계 값 조회.
    /// </summary>
    public int GetStat(string statKey)
    {
        return stats.ContainsKey(statKey) ? stats[statKey] : 0;
    }

    void CheckAchievements(string statKey)
    {
        int value = GetStat(statKey);

        foreach (var def in definitions)
        {
            if (def.statKey != statKey) continue;
            if (unlockedAchievements.Contains(def.id)) continue;
            if (value < def.threshold) continue;

            // 업적 달성!
            unlockedAchievements.Add(def.id);
            GiveReward(def);
            OnAchievementUnlocked?.Invoke(def);
            Debug.Log($"[Achievement] 달성: {def.name} ({def.id})");
        }
    }

    void GiveReward(AchievementDef def)
    {
        if (def.currencyReward > 0)
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.Add(def.currencyReward, $"업적: {def.name}");
        }
    }

    /// <summary>
    /// 업적이 달성되었는지 확인.
    /// </summary>
    public bool IsUnlocked(string achievementId)
    {
        return unlockedAchievements.Contains(achievementId);
    }

    /// <summary>
    /// 전체 업적 목록과 진행도 반환 (UI용).
    /// </summary>
    public List<AchievementProgress> GetAllProgress()
    {
        var result = new List<AchievementProgress>();
        foreach (var def in definitions)
        {
            result.Add(new AchievementProgress
            {
                def = def,
                currentValue = GetStat(def.statKey),
                unlocked = unlockedAchievements.Contains(def.id),
            });
        }
        return result;
    }

    // ═══════════════════════════
    //  세이브/로드
    // ═══════════════════════════

    [System.Serializable]
    public class AchievementSaveData
    {
        public List<StatEntry> stats = new List<StatEntry>();
        public List<string> unlocked = new List<string>();
    }

    [System.Serializable]
    public class StatEntry
    {
        public string key;
        public int value;
    }

    public AchievementSaveData GetSaveData()
    {
        var data = new AchievementSaveData();
        foreach (var kvp in stats)
            data.stats.Add(new StatEntry { key = kvp.Key, value = kvp.Value });
        data.unlocked = unlockedAchievements.ToList();
        return data;
    }

    /// <summary>새 게임 — 업적 통계·해금 초기화. SaveManager.ResetToNewGame용.</summary>
    public void ResetForNewGame()
    {
        stats.Clear();
        unlockedAchievements = new HashSet<string>();
    }

    public void LoadSaveData(AchievementSaveData data)
    {
        if (data == null) return;
        stats.Clear();
        foreach (var entry in data.stats)
            stats[entry.key] = entry.value;
        unlockedAchievements = new HashSet<string>(data.unlocked);
    }
}

// ═══════════════════════════
//  데이터 구조
// ═══════════════════════════

[System.Serializable]
public class AchievementDef
{
    public string id;
    public string name;
    public string statKey;
    public int threshold;
    public int currencyReward;
}

public class AchievementProgress
{
    public AchievementDef def;
    public int currentValue;
    public bool unlocked;
}
