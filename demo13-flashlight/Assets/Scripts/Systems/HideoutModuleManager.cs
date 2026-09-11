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
    public const int MaxLevel = 10;   // 시설 최대 10렙 (표는 Lv1~3, 이후는 공식으로 비용 산출)

    public struct Cost { public int scrap; public (string id, int qty)[] mats; }

    // 다재료 비용: 한 레벨에 여러 종류 재료 소비. m = (재료id, 수량) 튜플들.
    static Cost CM(int scrap, params (string id, int qty)[] m) => new Cost { scrap = scrap, mats = m };
    static Cost[] L(params Cost[] c) => c;

    // module → [Lv1, Lv2, Lv3] 비용 (tools/balance/hideout_modules.csv 미러). 각 레벨 2~3종 재료 + 고티어는 루디(ruby_shard).
    static readonly Dictionary<string, Cost[]> Table = new Dictionary<string, Cost[]>
    {
        ["stash"]     = L(CM(10000, ("scrap_metal",5), ("screw",6)),        CM(50000,  ("tool_part",5), ("plastic_sheet",4), ("wood_plank",6)),  CM(160000, ("circuit_board",3), ("tool_part",8), ("ruby_shard",1))),
        ["quarters"]  = L(CM(12000, ("cloth_rag",5), ("wood_plank",4)),     CM(55000,  ("wood_plank",6), ("plastic_sheet",4), ("tape_roll",3)),  CM(140000, ("wood_plank",10), ("cloth_rag",8), ("fuel_can",2))),
        ["workbench"] = L(CM(15000, ("tool_part",3), ("scrap_metal",6)),    CM(80000,  ("tool_part",6), ("wire",5), ("screw",8)),                CM(220000, ("circuit_board",3), ("tool_part",8), ("ruby_shard",1))),
        ["medbench"]  = L(CM(12000, ("chemical_flask",2), ("cloth_rag",4)), CM(60000,  ("chemical_flask",4), ("plastic_sheet",3), ("tape_roll",3)), CM(160000, ("chemical_flask",6), ("circuit_board",2), ("ruby_shard",1))),
        ["cooking"]   = L(CM(10000, ("scrap_metal",3), ("fuel_can",1)),     CM(40000,  ("fuel_can",2), ("tool_part",3), ("wire",3)),             CM(100000, ("fuel_can",3), ("circuit_board",2), ("chemical_flask",2))),
        ["generator"] = L(CM(18000, ("fuel_can",2), ("wire",4)),           CM(90000,  ("fuel_can",3), ("circuit_board",2), ("tool_part",4)),     CM(250000, ("wire",8), ("circuit_board",3), ("ruby_shard",2))),
        ["radio"]     = L(CM(25000, ("wire",5), ("circuit_board",1)),       CM(110000, ("circuit_board",2), ("wire",6), ("tool_part",3)),        CM(280000, ("circuit_board",4), ("flashlight_bulb",2), ("ruby_shard",1))),
        ["dispatch"]  = L(CM(30000, ("tool_part",3), ("wire",4)),          CM(130000, ("circuit_board",2), ("tool_part",5), ("screw",8)),       CM(380000, ("circuit_board",4), ("tool_part",8), ("ruby_shard",2))),
    };

    public static readonly string[] Modules =
        { "stash", "quarters", "workbench", "medbench", "cooking", "generator", "radio", "dispatch" };

    public static string DisplayName(string m) => m switch
    {
        "stash" => "창고", "quarters" => "침실", "workbench" => "작업대", "medbench" => "의료대",
        "cooking" => "취사대", "generator" => "발전기", "radio" => "라디오", "dispatch" => "파견 보드", _ => m,
    };

    Dictionary<string, int> levels = new Dictionary<string, int>();

    // ── 발전기 전력 (런타임 전용 — 세이브 안 함, 시작 OFF) ──
    bool generatorPowered;

    /// <summary>발전기 전력 ON 여부. 발전기 모듈 Lv>=1 + 전력 켜짐일 때만 true.</summary>
    public bool GeneratorPowered => generatorPowered && GetLevel("generator") >= 1;

    /// <summary>모듈 업그레이드 완료 (module, newLevel). UI·효과 구독.</summary>
    public event System.Action<string, int> OnModuleUpgraded;

    /// <summary>발전기 전력 상태 변경 (powered). UI 구독.</summary>
    public event System.Action<bool> OnPowerChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        HierarchyFolder.Persist(gameObject);
        SeedDefaultsIfEmpty();   // 새 게임: 침대(quarters)·창고(stash) 기본 Lv1 건설 상태
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>새 게임 기본 시설 — 침대·창고는 시작부터 Lv1. (이어하기 시 LoadSaveData가 저장값으로 덮어씀.)</summary>
    void SeedDefaultsIfEmpty()
    {
        if (levels.Count > 0) return;
        levels["quarters"] = 1;
        levels["stash"]    = 1;
    }

    public int GetLevel(string m) => levels.TryGetValue(m, out var l) ? l : 0;
    public bool IsMaxed(string m) => GetLevel(m) >= MaxLevel;

    /// <summary>다음 레벨 비용. 최대 레벨이면 null.</summary>
    public Cost? NextCost(string m)
    {
        int lv = GetLevel(m);
        if (lv >= MaxLevel) return null;
        if (!Table.TryGetValue(m, out var arr) || arr.Length == 0) return null;
        if (lv < arr.Length) return arr[lv];

        // 표(Lv1~3) 초과 → 마지막 비용을 레벨에 비례 스케일 (Lv4~10)
        var last = arr[arr.Length - 1];
        int over = lv - arr.Length + 1;                  // Lv3→Lv4 업글 시 lv=3 → over=1
        int scrap = Mathf.RoundToInt(last.scrap * Mathf.Pow(1.6f, over));
        var mats = new (string id, int qty)[last.mats.Length];
        for (int i = 0; i < last.mats.Length; i++)
            mats[i] = (last.mats[i].id, Mathf.Max(1, Mathf.RoundToInt(last.mats[i].qty * (1f + 0.5f * over))));
        return new Cost { scrap = scrap, mats = mats };
    }

    /// <summary>업그레이드 가능 여부 + 불가 사유.</summary>
    public bool CanUpgrade(string m, out string reason)
    {
        reason = null;
        var c = NextCost(m);
        if (c == null) { reason = "최대 레벨"; return false; }
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.CanAfford(c.Value.scrap))
        { reason = "스크랩 부족"; return false; }
        foreach (var (id, qty) in c.Value.mats)
            if (CountMaterial(id) < qty)
            {
                var data = ItemDatabase.Get(id);
                reason = $"재료 부족 ({(data != null ? data.displayName : id)} x{qty})";
                return false;
            }
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
        foreach (var (id, qty) in c.mats) ConsumeMaterial(id, qty);

        int nl = GetLevel(m) + 1;
        levels[m] = nl;
        Debug.Log($"[Hideout] {DisplayName(m)} → Lv{nl}");
        ToastManager.Show($"★ {DisplayName(m)} Lv{nl}", ToastManager.ToastType.Success);
        OnModuleUpgraded?.Invoke(m, nl);
        return true;
    }

    /// <summary>디버그용: 비용 소비 없이 레벨 강제 설정.</summary>
    public void ForceSetLevel(string m, int level)
    {
        level = Mathf.Clamp(level, 0, MaxLevel);
        levels[m] = level;
        Debug.Log($"[Hideout] {DisplayName(m)} → Lv{level} (디버그)");
        OnModuleUpgraded?.Invoke(m, level);
    }

    // ═══════════════════════════
    //  발전기 전력 (런타임 전용)
    // ═══════════════════════════

    /// <summary>발전기 전력 토글. ON 전환 시 fuel_can 1개 소비(없으면 실패), OFF는 무료.
    /// 반환: 성공 여부. reason = 실패 사유(성공 시 null).</summary>
    public bool ToggleGeneratorPower(out string reason)
    {
        reason = null;
        if (GetLevel("generator") < 1) { reason = "발전기 미건설"; return false; }

        if (!generatorPowered)
        {
            // ON 전환 → 연료 소비 (가방 + 창고 합산)
            if (CountMaterial("fuel_can") < 1)
            {
                reason = "연료(fuel_can) 필요";
                return false;
            }
            ConsumeMaterial("fuel_can", 1);
            generatorPowered = true;
        }
        else
        {
            // OFF 전환 → 무료
            generatorPowered = false;
        }

        Debug.Log($"[Hideout] 발전기 전력 → {(generatorPowered ? "ON" : "OFF")}");
        OnPowerChanged?.Invoke(GeneratorPowered);
        return true;
    }

    static InventoryGrid PlayerGrid()
    {
        var pgo = GameObject.FindGameObjectWithTag("Player");
        var inv = pgo != null ? pgo.GetComponent<PlayerInventory>() : null;
        return inv != null ? inv.Grid : null;
    }

    /// <summary>건설/업그레이드/연료 재료 보유량 = 가방(주머니) + 메인 창고 합산.
    /// (집에서 짓는 자재이므로 창고에 둔 재료도 함께 인정.)</summary>
    public static int CountMaterial(string id)
    {
        int n = 0;
        var bag = PlayerGrid();
        if (bag != null) n += bag.CountItem(id);
        if (MainStash.Instance != null && MainStash.Instance.Grid != null)
            n += MainStash.Instance.Grid.CountItem(id);
        return n;
    }

    /// <summary>재료 소비 — 창고 먼저, 부족분은 가방에서.</summary>
    static void ConsumeMaterial(string id, int qty)
    {
        if (qty <= 0) return;
        var stash = (MainStash.Instance != null) ? MainStash.Instance.Grid : null;
        if (stash != null)
        {
            int take = Mathf.Min(stash.CountItem(id), qty);
            if (take > 0) { stash.ConsumeItem(id, take); qty -= take; }
        }
        if (qty <= 0) return;
        PlayerGrid()?.ConsumeItem(id, qty);
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
