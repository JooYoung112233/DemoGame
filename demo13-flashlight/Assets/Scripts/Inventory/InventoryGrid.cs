using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 슬롯 기반 인벤토리 컨테이너.
/// 플레이어 가방, 창고, 루팅 상자 모두 이 클래스 사용.
/// UI 독립 — 순수 데이터 로직만 담당.
///
/// **2026-09-09: 테트리스식 격자 폐기 → 아이템 1개 = 슬롯 1칸(RPG식).**
///   (docs/scope-cut.md 3번) `ItemData.gridWidth/gridHeight`(1~3칸 footprint)와 90도 회전은
///   더 이상 배치에 영향을 주지 않는다. 용량은 **칸 수(width×height)** 와 **무게**(PlayerInventory)
///   두 축으로만 제한된다 — 무게는 유지 결정.
///   클래스·API 이름(`InventoryGrid`, `gridX/gridY`, `rotated`)은 세이브 호환과
///   호출부 20여 곳을 건드리지 않으려고 그대로 뒀다. x/y는 이제 **슬롯 좌표**다.
/// </summary>
[System.Serializable]
public class InventoryGrid
{
    /// <summary>배치된 아이템 정보</summary>
    [System.Serializable]
    public class PlacedItem
    {
        public ItemInstance item;
        public int gridX;  // 슬롯 X
        public int gridY;  // 슬롯 Y
        public bool rotated; // [폐기 2026-09-09] 회전 개념 없음 — 세이브 호환용으로만 남김(항상 false)

        public PlacedItem(ItemInstance item, int x, int y, bool rotated = false)
        {
            this.item = item;
            gridX = x;
            gridY = y;
            this.rotated = false;
        }

        /// <summary>슬롯 1칸 고정(2026-09-09 격자 폐기).</summary>
        public int EffectiveWidth => 1;

        /// <summary>슬롯 1칸 고정(2026-09-09 격자 폐기).</summary>
        public int EffectiveHeight => 1;

        /// <summary>이 아이템이 (x,y) 칸을 점유하는지 — 슬롯 1칸이므로 좌표 일치.</summary>
        public bool Occupies(int x, int y) => item != null && x == gridX && y == gridY;
    }

    public int width;
    public int height;

    /// <summary>격자: 각 칸이 어떤 PlacedItem을 참조하는지 (null이면 비어있음)</summary>
    PlacedItem[,] grid;

    /// <summary>배치된 아이템 목록</summary>
    List<PlacedItem> items = new List<PlacedItem>();

    /// <summary>아이템 수용 필터 (null이면 전체 허용). 가구 카테고리 제한 등에 사용.</summary>
    public System.Func<ItemInstance, bool> AcceptFilter;

    /// <summary>변경 시 발행 (UI 갱신용)</summary>
    public event System.Action OnChanged;

    /// <summary>이 격자에 아이템이 새로 배치될 때 호출(옵트인, 기본 null → 기존 동작 무변경).
    /// 플레이어 격자(가방/주머니/보안)만 구독해 도감 '첫 획득' 발견 처리에 쓴다 — 줍기·루팅 드래그·자동배치·구매·제작 등 모든 경로를 한 지점에서 포착.</summary>
    public System.Action<ItemInstance> OnItemPlaced;

    /// <summary>외부에서 변경 알림 발행</summary>
    public void NotifyChanged() => OnChanged?.Invoke();

    public InventoryGrid(int width, int height)
    {
        this.width = width;
        this.height = height;
        grid = new PlacedItem[width, height];
    }

    /// <summary>격자 크기 변경. 기존 아이템을 자동 재배치하고, 넘치는 아이템은 overflow로 반환.</summary>
    public List<ItemInstance> Resize(int newWidth, int newHeight)
    {
        var overflow = new List<ItemInstance>();
        var oldItems = new List<PlacedItem>(items);

        // 격자 초기화
        items.Clear();
        width = newWidth;
        height = newHeight;
        grid = new PlacedItem[newWidth, newHeight];

        // 기존 아이템을 순서대로 재배치 시도
        for (int i = 0; i < oldItems.Count; i++)
        {
            if (!TryAutoPlace(oldItems[i].item))
                overflow.Add(oldItems[i].item);
        }

        OnChanged?.Invoke();
        return overflow;
    }

    #region 조회

    /// <summary>해당 칸의 아이템 (없으면 null)</summary>
    public PlacedItem GetAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        if (grid == null) grid = new PlacedItem[width, height];   // 역직렬화 등으로 grid 배열이 비면 지연 할당(NRE 방지)
        return grid[x, y];
    }

    /// <summary>전체 배치된 아이템 목록</summary>
    public List<PlacedItem> GetAll() => new List<PlacedItem>(items);

    /// <summary>아이템 수</summary>
    public int ItemCount => items.Count;

    /// <summary>해당 칸이 비어있는지</summary>
    public bool IsEmpty(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        if (grid == null) grid = new PlacedItem[width, height];   // NRE 방지(지연 할당)
        return grid[x, y] == null;
    }

    /// <summary>총 무게 합산</summary>
    public float TotalWeight
    {
        get
        {
            float w = 0f;
            for (int i = 0; i < items.Count; i++)
                w += items[i].item.TotalWeight;
            return w;
        }
    }

    /// <summary>특정 itemId를 가진 아이템의 총 수량</summary>
    public int CountItem(string itemId)
    {
        int count = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].item.data != null && items[i].item.data.itemId == itemId)
                count += items[i].item.stackCount;
        }
        return count;
    }

    #endregion

    #region 배치

    /// <summary>해당 슬롯에 배치 가능한지. (rotated 인자는 폐기 — 무시)</summary>
    public bool CanPlace(ItemInstance item, int x, int y, bool rotated = false)
    {
        if (item == null || item.data == null) return false;
        if (AcceptFilter != null && !AcceptFilter(item)) return false;
        if (x < 0 || y < 0 || x >= width || y >= height) return false;
        if (grid == null) grid = new PlacedItem[width, height];
        return grid[x, y] == null;
    }

    /// <summary>배치 시도. 성공하면 true. (rotated 인자는 폐기 — 무시)</summary>
    public bool TryPlace(ItemInstance item, int x, int y, bool rotated = false)
    {
        if (!CanPlace(item, x, y)) return false;

        var placed = new PlacedItem(item, x, y);
        items.Add(placed);
        grid[x, y] = placed;

        OnItemPlaced?.Invoke(item);
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>자동 배치 가능 여부만 확인 (실제 배치 안 함)</summary>
    public bool CanAutoPlace(ItemInstance item)
    {
        if (item == null || item.data == null) return false;
        if (AcceptFilter != null && !AcceptFilter(item)) return false;

        // 스택 가능한 기존 슬롯 확인
        if (item.data.maxStack > 1)
        {
            int remaining = item.stackCount;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].item.CanStackWith(item))
                {
                    int space = items[i].item.data.maxStack - items[i].item.stackCount;
                    remaining -= space;
                    if (remaining <= 0) return true;
                }
            }
        }

        return FindFreeSlot(item, out _, out _);
    }

    /// <summary>비어 있는 첫 슬롯을 찾는다(좌상단부터 행 우선).</summary>
    bool FindFreeSlot(ItemInstance item, out int fx, out int fy)
    {
        for (int gy = 0; gy < height; gy++)
            for (int gx = 0; gx < width; gx++)
                if (CanPlace(item, gx, gy))
                { fx = gx; fy = gy; return true; }
        fx = fy = -1;
        return false;
    }

    /// <summary>빈 슬롯에 자동 배치. 성공하면 true.</summary>
    public bool TryAutoPlace(ItemInstance item)
    {
        if (item == null || item.data == null) return false;
        if (AcceptFilter != null && !AcceptFilter(item)) return false;

        // 먼저 스택 가능한 기존 아이템에 합치기 시도
        if (item.data.maxStack > 1)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].item.CanStackWith(item))
                {
                    int remaining = items[i].item.TryStack(item);
                    if (remaining <= 0)
                    {
                        // 병합은 TryPlace를 안 타므로 발견 훅을 여기서도 발행(도감). Discover는 멱등이라 중복 무해.
                        OnItemPlaced?.Invoke(item);
                        OnChanged?.Invoke();
                        return true;
                    }
                }
            }
        }

        if (item.stackCount <= 0) return true;

        if (FindFreeSlot(item, out int fx, out int fy)) return TryPlace(item, fx, fy);
        return false; // 빈 슬롯 없음
    }

    /// <summary>해당 칸의 아이템 제거. 제거된 PlacedItem 반환.</summary>
    public PlacedItem RemoveAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;

        var placed = grid[x, y];
        if (placed == null) return null;

        return Remove(placed);
    }

    /// <summary>PlacedItem 제거</summary>
    public PlacedItem Remove(PlacedItem placed)
    {
        if (placed == null) return null;

        // 슬롯 참조 해제
        if (placed.gridX >= 0 && placed.gridX < width && placed.gridY >= 0 && placed.gridY < height)
            grid[placed.gridX, placed.gridY] = null;

        items.Remove(placed);
        OnChanged?.Invoke();
        return placed;
    }

    #endregion

    #region 스택

    /// <summary>해당 위치에 스택 시도. 남은 수량 반환.</summary>
    public int TryStackAt(ItemInstance item, int x, int y)
    {
        var target = GetAt(x, y);
        if (target == null) return item.stackCount;
        int before = item.stackCount;
        int remaining = target.item.TryStack(item);
        // 일부라도 병합됐으면 발견 훅 발행(도감) — 병합 경로는 TryPlace를 안 탄다.
        if (remaining < before) OnItemPlaced?.Invoke(item);
        return remaining;
    }

    #endregion

    #region 스왑

    /// <summary>해당 슬롯의 아이템과 교환. 기존 아이템 반환. (rotated 인자는 폐기 — 무시)</summary>
    public ItemInstance TrySwap(ItemInstance newItem, int x, int y, bool rotated = false)
    {
        var existing = GetAt(x, y);
        if (existing == null)
        {
            TryPlace(newItem, x, y, rotated);
            return null;
        }

        // 기존 아이템 정보 보존
        var oldItem = existing.item;
        int oldX = existing.gridX;
        int oldY = existing.gridY;
        Remove(existing);

        // 새 아이템 배치 시도
        if (CanPlace(newItem, x, y))
        {
            TryPlace(newItem, x, y);
            return oldItem;
        }
        else
        {
            // 배치 실패하면 기존 아이템 복원
            TryPlace(oldItem, oldX, oldY);
            return null;
        }
    }

    #endregion

    #region 유틸

    /// <summary>전체 비우기</summary>
    public void Clear()
    {
        items.Clear();
        grid = new PlacedItem[width, height];
        OnChanged?.Invoke();
    }

    /// <summary>
    /// 자동 정렬(재배치). 모든 아이템을 회수 → 카테고리·희귀도 순으로 정렬 →
    /// 앞 슬롯부터 다시 채운다. 재배치 실패분은 원래 위치로 복원.
    /// (2026-09-09 격자 폐기 후 "빈틈 메우기"는 의미가 없어졌다 — 이제 순서 정리다.)
    /// </summary>
    public void AutoSort()
    {
        if (width <= 0 || height <= 0) return;

        // 현재 배치 스냅샷 (원위치 복원용)
        var snapshot = new List<PlacedItem>(items);
        if (snapshot.Count == 0) return;

        // 정렬: 카테고리 순 → 희귀도 높은 순 → itemId
        var ordered = new List<PlacedItem>(snapshot);
        ordered.Sort((a, b) =>
        {
            int catA = a.item != null && a.item.data != null ? (int)a.item.data.category : 0;
            int catB = b.item != null && b.item.data != null ? (int)b.item.data.category : 0;
            if (catA != catB) return catA.CompareTo(catB);

            int rarA = a.item != null && a.item.data != null ? (int)a.item.data.rarity : 0;
            int rarB = b.item != null && b.item.data != null ? (int)b.item.data.rarity : 0;
            if (rarA != rarB) return rarB.CompareTo(rarA); // 희귀도 높은 순

            string idA = a.item != null && a.item.data != null ? a.item.data.itemId : "";
            string idB = b.item != null && b.item.data != null ? b.item.data.itemId : "";
            return string.CompareOrdinal(idA, idB);
        });

        // 전부 회수 후 순서대로 재배치
        items.Clear();
        grid = new PlacedItem[width, height];

        bool allPlaced = true;
        for (int i = 0; i < ordered.Count; i++)
        {
            // OnChanged 폭주 방지: 내부에서는 조용히 재배치
            if (!PlaceQuietly(ordered[i].item))
            {
                allPlaced = false;
                break;
            }
        }

        // 실패 시 원래 배치로 복원
        if (!allPlaced)
        {
            items.Clear();
            grid = new PlacedItem[width, height];
            for (int i = 0; i < snapshot.Count; i++)
            {
                var s = snapshot[i];
                if (!PlaceAtQuietly(s.item, s.gridX, s.gridY))
                    PlaceQuietly(s.item); // 최후 보루
            }
        }

        OnChanged?.Invoke();
    }

    /// <summary>OnChanged 발행 없이 자동 배치(AutoSort 내부 전용).</summary>
    bool PlaceQuietly(ItemInstance item)
    {
        if (item == null || item.data == null) return false;
        if (!FindFreeSlot(item, out int fx, out int fy)) return false;
        return PlaceAtQuietly(item, fx, fy);
    }

    /// <summary>OnChanged 발행 없이 지정 슬롯 배치(AutoSort 내부 전용).</summary>
    bool PlaceAtQuietly(ItemInstance item, int x, int y)
    {
        if (!CanPlace(item, x, y)) return false;

        var placed = new PlacedItem(item, x, y);
        items.Add(placed);
        grid[x, y] = placed;
        return true;
    }

    // ── 세이브/로드 (인벤·창고 공용) ──

    /// <summary>격자 내용을 직렬화 가능한 엔트리 목록으로.</summary>
    public List<GridItemEntry> GetSaveData()
    {
        var list = new List<GridItemEntry>();
        foreach (var p in items)
        {
            if (p.item == null || p.item.data == null) continue;
            list.Add(new GridItemEntry
            {
                itemId = p.item.data.itemId,
                count = p.item.stackCount,
                durability = p.item.durability,
                x = p.gridX, y = p.gridY, rotated = p.rotated,
                attachments = p.item.HasAnyAttachment ? (string[])p.item.attachments.Clone() : null,
                ammoCount = p.item.ammoCount,          // 반쯤 쓴 탄창은 남은 탄째로 저장돼야 한다
                ammoItemId = p.item.ammoItemId,
                // 보관함이면 내부 격자도 재귀 저장
                containerItems = p.item.IsContainer ? p.item.ContainerGrid.GetSaveData() : null,
            });
        }
        return list;
    }

    /// <summary>엔트리 목록으로 격자 복원 (저장 위치 우선, 실패 시 자동 배치).</summary>
    public void LoadSaveData(List<GridItemEntry> entries)
    {
        Clear();
        AppendSaveData(entries);
    }

    /// <summary>격자를 비우지 않고 엔트리들을 추가 배치(저장 위치 우선, 실패 시 자동 배치).
    /// 다른 격자(예: 상점 판매 트레이)의 내용을 이 격자로 합칠 때 사용.</summary>
    public void AppendSaveData(List<GridItemEntry> entries)
    {
        if (entries == null) return;
        foreach (var e in entries)
        {
            var data = ItemDatabase.Get(e.itemId);
            if (data == null) continue;
            var inst = new ItemInstance(data, e.count);
            if (data.hasDurability) inst.durability = e.durability;
            if (e.attachments != null && e.attachments.Length > 0)
                inst.attachments = (string[])e.attachments.Clone();
            inst.ammoCount  = e.ammoCount;
            inst.ammoItemId = e.ammoItemId;
            // 보관함이면 내부 격자 복원(재귀)
            if (inst.IsContainer && e.containerItems != null)
                inst.ContainerGrid.LoadSaveData(e.containerItems);
            if (!TryPlace(inst, e.x, e.y, e.rotated)) TryAutoPlace(inst);
        }
    }

    /// <summary>특정 itemId를 가진 아이템 찾기 (첫 번째)</summary>
    public PlacedItem FindItem(string itemId)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].item.data != null && items[i].item.data.itemId == itemId)
                return items[i];
        }
        return null;
    }

    /// <summary>특정 itemId를 수량만큼 소비. 성공하면 true.</summary>
    public bool ConsumeItem(string itemId, int count = 1)
    {
        int remaining = count;

        for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (items[i].item.data == null || items[i].item.data.itemId != itemId)
                continue;

            if (items[i].item.stackCount <= remaining)
            {
                remaining -= items[i].item.stackCount;
                Remove(items[i]);
            }
            else
            {
                items[i].item.stackCount -= remaining;
                remaining = 0;
                OnChanged?.Invoke();
            }
        }

        return remaining <= 0;
    }

    #endregion
}
