using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 루팅 상자 / 창고 컴포넌트. 자체 InventoryGrid를 소유. InteractableObject.Container와 연동.
///
/// 레이드 상자의 내용물은 MapSpawnController가 이 상자의 **종류(lootKind)** 루팅 표에서 채운다
/// (2026-09-11 docs/region-loot.md §루팅 정리 결정). 창고·보관함·시체도 같은 컴포넌트를 쓴다.
/// </summary>
public class LootContainer : MonoBehaviour
{
    [Header("격자 설정")]
    [Tooltip("격자 가로 칸 수")]
    [SerializeField] int gridWidth = 4;
    [Tooltip("격자 세로 칸 수")]
    [SerializeField] int gridHeight = 5;

    [Header("표시")]
    [Tooltip("상자 이름 (UI에 표시)")]
    [SerializeField] string containerName = "상자";

    [Header("루팅 종류")]
    [Tooltip("루팅 표의 상자 종류 — junk(길가 잡동사니)·trunk(차량 트렁크)·stall(노점 좌판)·crate(나무상자)·\n" +
             "register(계산대)·safe(금고)·int_*(실내 건물 종류). MapSpawnController가 이 종류의 표(Resources/loot_tables.txt)로 채운다.\n" +
             "고철(◈)은 register·safe·stall에서만 나온다. 이 셋은 예산과 무관하게 늘 채운다.")]
    [SerializeField] string lootKind = "crate";

    public InventoryGrid Grid { get; private set; }
    public string ContainerName => containerName;
    public string LootKind => string.IsNullOrEmpty(lootKind) ? "crate" : lootKind;
    public bool IsOpen { get; private set; }
    public bool IsLooted { get; private set; }

    /// <summary>다 뒤진 상자인지 — 수색 연출(하나씩 드러남)은 처음 열 때만(2026-09-11 되살림, LootListUI).</summary>
    public bool HasBeenSearched { get; set; }

    /// <summary>수색 중 몇 칸이 드러났나 — 도중에 닫아도 다음에 이어서 드러난다.</summary>
    public int RevealedCount { get; set; }

    void Awake()
    {
        Grid = new InventoryGrid(gridWidth, gridHeight);
    }

    /// <summary>빌더용 — 루팅 종류 지정.</summary>
    public void SetLootKind(string kind) => lootKind = kind;

    /// <summary>상자 열기(상태 표시). 실제 루팅은 루팅 목록 UI / 캐릭터 패널이 한다.</summary>
    public void Open(GameObject playerGO)
    {
        IsOpen = true;
    }

    /// <summary>상자 닫기</summary>
    public void Close()
    {
        IsOpen = false;
        if (Grid.ItemCount == 0) IsLooted = true;   // 비어 있으면 루팅 완료 처리
    }

    /// <summary>코드에서 아이템 추가</summary>
    public bool AddItem(ItemInstance item)
    {
        return Grid.TryAutoPlace(item);
    }

    /// <summary>런타임 생성 컨테이너 초기화(적 시체 등 — AddComponent 직후, 아이템 채우기 전에 호출).
    /// Awake가 만든 기본 격자를 지정 크기로 재생성한다.</summary>
    public void Setup(string name, int width, int height)
    {
        containerName = name;
        gridWidth = width;
        gridHeight = height;
        Grid = new InventoryGrid(width, height);
    }

    /// <summary>내용물에 맞춰 격자 크기를 정하고 배치하는 초기화(시체 등 동적 컨테이너).
    /// 행 수 = 필요 칸수/columns 올림 +1줄 여유, minRows~maxRows로 클램프.
    /// 반환 = 그래도 못 들어간 초과분(호출자가 바닥 드랍 등으로 처리).</summary>
    public List<ItemInstance> SetupAutoSize(string name, List<ItemInstance> items, int columns = 4, int minRows = 2, int maxRows = 6)
    {
        // 아이템 1개 = 슬롯 1칸(2026-09-09 격자 폐기) — 개수가 곧 필요 칸 수다.
        int cells = 0;
        if (items != null)
            foreach (var it in items)
                if (it != null && it.data != null) cells++;

        int rows = Mathf.Clamp(Mathf.CeilToInt(cells / (float)columns) + 1, minRows, maxRows);
        Setup(name, columns, rows);

        var overflow = new List<ItemInstance>();
        if (items != null)
            foreach (var it in items)
                if (it != null && !Grid.TryAutoPlace(it)) overflow.Add(it);
        return overflow;
    }
}
