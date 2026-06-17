using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 영속 메인 창고(보관함). 안전구역(안전가옥/하이드아웃)에서 인벤토리를 열면
/// 우측 열에 항상 표시된다. 레이드 중에는 표시 안 함(상자 파밍만).
///
/// 격자는 지금 고정 크기(그레이박스). 추후 하이드아웃 'stash' 모듈 레벨에 비례해
/// 칸을 확장할 예정(레벨↑ = 창고↑). — docs/safehouse.md 창고.
/// SaveManager가 내용물을 영속화.
/// </summary>
public class MainStash : MonoBehaviour
{
    public static MainStash Instance { get; private set; }

    // 그레이박스 고정 크기 (Lv3 상정 — 세이브 순서와 무관하게 항상 수용 가능)
    const int Width = 10;
    const int Height = 14;

    public InventoryGrid Grid { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (Grid == null) Grid = new InventoryGrid(Width, Height);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>없으면 생성(온디맨드). 씬 재빌드 없이도 동작하도록.</summary>
    public static MainStash Ensure()
    {
        if (Instance == null)
        {
            var go = new GameObject("MainStash");
            DontDestroyOnLoad(go);
            go.AddComponent<MainStash>();   // Awake가 Instance·Grid 설정
        }
        return Instance;
    }

    public InventoryGrid GetGrid()
    {
        if (Grid == null) Grid = new InventoryGrid(Width, Height);
        return Grid;
    }

    // ── 세이브/로드 ──
    public List<GridItemEntry> GetSaveData() => GetGrid().GetSaveData();

    public void LoadSaveData(List<GridItemEntry> data)
    {
        var g = GetGrid();
        if (data != null) g.LoadSaveData(data);
    }
}
