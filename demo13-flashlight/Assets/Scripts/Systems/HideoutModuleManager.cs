using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하이드아웃 시설 모듈 시스템. 각 모듈 Lv0(미건설)~Lv3. 업그레이드 = 스크랩 + 재료 소비.
/// 비용 진실원: tools/balance/hideout_modules.csv (여기 하드 미러 — I29 BalanceConfig 로더가 추후 외부화).
/// 설계: quests-region1.md §9.7 ③ 모듈 트리. SaveManager가 레벨 영속화.
/// </summary>
public class HideoutModuleManager : MonoBehaviour
{
    public static HideoutModuleManager Instance { get; private set; }
    public const int MaxLevel = 3;

    public struct Cost { public int scrap; public (string id, int qty)[] mats; }

    static Cost C(int scrap, string id, int qty) => new Cost { scrap = scrap, mats = new[] { (id, qty) } };
    static Cost[] L(params Cost[] c) => c;

    // module → [Lv1, Lv2, Lv3] 비용 (hideout_modules.csv 미러)
    static readonly Dictionary<string, Cost[]> Table = new Dictionary<string, Cost[]>
    {
        ["stash"]     = L(C(10000, "scrap_metal", 5),    C(50000, "tool_part", 5),       C(160000, "circuit_board", 3)),
        ["quarters"]  = L(C(12000, "cloth_rag", 5),      C(55000, "wood_plank", 5),      C(140000, "wood_plank", 10)),
        ["workbench"] = L(C(15000, "tool_part", 3),      C(80000, "tool_part", 6),       C(220000, "circuit_board", 3)),
        ["medbench"]  = L(C(12000, "chemical_flask", 2), C(60000, "chemical_flask", 4),  C(160000, "chemical_flask", 6)),
        ["cooking"]   = L(C(10000, "scrap_metal", 3),    C(40000, "fuel_can", 1),        C(100000, "fuel_can", 2)),
        ["generator"] = L(C(18000, "fuel_can", 2),       C(90000, "fuel_can", 3),        C(250000, "wire", 5)),
        ["radio"]     = L(C(25000, "wire", 5),           C(110000, "circuit_board", 2),  C(280000, "circuit_board", 4)),
        ["dispatch"]  = L(C(30000, "tool_part", 3),      C(130000, "circuit_board", 2),  C(380000, "circuit_board", 4)),
    };

    public static readonly string[] Modules =
        { "stash", "quarters", "workbench", "medbench", "cooking", "generator", "radio", "dispatch" };

    public static string DisplayName(string m) => m switch
    {
        "stash" => "창고", "quarters" => "침실", "workbench" => "작업대", "medbench" => "의료대",
        "cooking" => "취사대", "generator" => "발전기", "radio" => "라디오", "dispatch" => "파견 보드", _ => m,
    };

    Dictionary<string, int> levels = new Dictionary<string, int>();

    /// <summary>모듈 업그레이드 완료 (module, newLevel). UI·효과 구독.</summary>
    public event System.Action<string, int> OnModuleUpgraded;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public int GetLevel(string m) => levels.TryGetValue(m, out var l) ? l : 0;
    public bool IsMaxed(string m) => GetLevel(m) >= MaxLevel;

    /// <summary>다음 레벨 비용. 최대 레벨이면 null.</summary>
    public Cost? NextCost(string m)
    {
        int lv = GetLevel(m);
        if (lv >= MaxLevel) return null;
        if (!Table.TryGetValue(m, out var arr) || lv >= arr.Length) return null;
        return arr[lv];
    }

    /// <summary>업그레이드 가능 여부 + 불가 사유.</summary>
    public bool CanUpgrade(string m, out string reason)
    {
        reason = null;
        var c = NextCost(m);
        if (c == null) { reason = "최대 레벨"; return false; }
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.CanAfford(c.Value.scrap))
        { reason = "스크랩 부족"; return false; }
        var grid = PlayerGrid();
        foreach (var (id, qty) in c.Value.mats)
            if (grid == null || grid.CountItem(id) < qty) { reason = $"재료 부족 ({id} x{qty})"; return false; }
        return true;
    }

    /// <summary>업그레이드 실행: 스크랩+재료 소비 후 레벨 +1.</summary>
    public bool Upgrade(string m)
    {
        if (!CanUpgrade(m, out var reason))
        {
            ToastManager.Show(reason, ToastManager.ToastType.Warning);
            return false;
        }
        var c = NextCost(m).Value;
        CurrencyManager.Instance.Spend(c.scrap, $"하이드아웃: {DisplayName(m)}");
        var grid = PlayerGrid();
        foreach (var (id, qty) in c.mats) grid?.ConsumeItem(id, qty);

        int nl = GetLevel(m) + 1;
        levels[m] = nl;
        Debug.Log($"[Hideout] {DisplayName(m)} → Lv{nl}");
        ToastManager.Show($"★ {DisplayName(m)} Lv{nl}", ToastManager.ToastType.Success);
        OnModuleUpgraded?.Invoke(m, nl);
        return true;
    }

    static InventoryGrid PlayerGrid()
    {
        var pgo = GameObject.FindGameObjectWithTag("Player");
        var inv = pgo != null ? pgo.GetComponent<PlayerInventory>() : null;
        return inv != null ? inv.Grid : null;
    }

    // ═══════════════════════════
    //  세이브/로드
    // ═══════════════════════════

    public List<HideoutModuleSaveEntry> GetSaveData()
    {
        var list = new List<HideoutModuleSaveEntry>();
        foreach (var kv in levels)
            list.Add(new HideoutModuleSaveEntry { module = kv.Key, level = kv.Value });
        return list;
    }

    public void LoadSaveData(List<HideoutModuleSaveEntry> data)
    {
        levels.Clear();
        if (data == null) return;
        foreach (var e in data)
            if (!string.IsNullOrEmpty(e.module)) levels[e.module] = e.level;
    }
}

[System.Serializable]
public class HideoutModuleSaveEntry
{
    public string module;
    public int level;
}
