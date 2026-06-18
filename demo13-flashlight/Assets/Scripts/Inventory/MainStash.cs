using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 영속 메인 창고(보관함). 안전구역(안전가옥/하이드아웃)에서 인벤토리를 열면 우측 열에 표시.
/// 기본 가로 8 × 세로 30. 하이드아웃 'stash' 모듈 레벨당 세로 +7줄(최대 10렙 → 30+70=100).
/// 레벨업 시 내용물 보존하며 확장(축소 안 함). SaveManager가 내용물을 영속화.
/// </summary>
public class MainStash : MonoBehaviour
{
    public static MainStash Instance { get; private set; }

    public const int Width = 8;            // 칸수는 가로 기준
    public const int BaseHeight = 30;
    public const int HeightPerLevel = 7;   // stash 모듈 레벨당 +7줄

    public InventoryGrid Grid { get; private set; }

    bool subscribed;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureGrid();
        TrySubscribe();
    }

    void OnDestroy()
    {
        if (subscribed && HideoutModuleManager.Instance != null)
            HideoutModuleManager.Instance.OnModuleUpgraded -= OnModuleUpgraded;
        if (Instance == this) Instance = null;
    }

    void TrySubscribe()
    {
        if (subscribed || HideoutModuleManager.Instance == null) return;
        HideoutModuleManager.Instance.OnModuleUpgraded += OnModuleUpgraded;
        subscribed = true;
    }

    void OnModuleUpgraded(string module, int level)
    {
        if (module == "stash") EnsureGrid();
    }

    int TargetHeight()
    {
        int lv = HideoutModuleManager.Instance != null ? HideoutModuleManager.Instance.GetLevel("stash") : 0;
        return BaseHeight + HeightPerLevel * Mathf.Max(0, lv);
    }

    /// <summary>현재 창고 레벨에 맞춰 격자 확보(없으면 생성, 작으면 내용 보존하며 확장).</summary>
    void EnsureGrid()
    {
        TrySubscribe();   // Awake 시점에 매니저가 없었을 수 있어 지연 구독
        int h = TargetHeight();
        if (Grid == null) { Grid = new InventoryGrid(Width, h); return; }
        if (Grid.width != Width || Grid.height < h)
        {
            var saved = Grid.GetSaveData();
            Grid = new InventoryGrid(Width, Mathf.Max(h, Grid.height));
            Grid.LoadSaveData(saved);
        }
    }

    /// <summary>없으면 생성(온디맨드). 씬 재빌드 없이도 동작하도록.</summary>
    public static MainStash Ensure()
    {
        if (Instance == null)
        {
            var go = new GameObject("MainStash");
            DontDestroyOnLoad(go);
            go.AddComponent<MainStash>();
        }
        return Instance;
    }

    public InventoryGrid GetGrid()
    {
        EnsureGrid();
        return Grid;
    }

    // ── 세이브/로드 ──
    public List<GridItemEntry> GetSaveData() => GetGrid().GetSaveData();

    public void LoadSaveData(List<GridItemEntry> data)
    {
        EnsureGrid();   // 로드된 창고 레벨에 맞춰 크기 확보 후 로드 (레벨은 하이드아웃 모듈이 먼저 로드됨)
        if (data != null) Grid.LoadSaveData(data);
    }
}
