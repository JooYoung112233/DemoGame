using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 격자 기반 인벤토리 컨테이너.
/// 플레이어 가방, 창고, 루팅 상자 모두 이 클래스 사용.
/// UI 독립 — 순수 데이터 로직만 담당.
/// </summary>
[System.Serializable]
public class InventoryGrid
{
    /// <summary>배치된 아이템 정보</summary>
    [System.Serializable]
    public class PlacedItem
    {
        public ItemInstance item;
        public int gridX;  // 좌상단 X
        public int gridY;  // 좌상단 Y
        public bool rotated; // 90도 회전 여부

        public PlacedItem(ItemInstance item, int x, int y, bool rotated = false)
        {
            this.item = item;
            gridX = x;
            gridY = y;
            this.rotated = rotated;
        }

        /// <summary>회전 반영 실제 가로 칸 수</summary>
        public int EffectiveWidth => (item != null && item.data != null)
            ? (rotated ? item.data.gridHeight : item.data.gridWidth) : 1;

        /// <summary>회전 반영 실제 세로 칸 수</summary>
        public int EffectiveHeight => (item != null && item.data != null)
            ? (rotated ? item.data.gridWidth : item.data.gridHeight) : 1;

        /// <summary>이 아이템이 (x,y) 칸을 점유하는지</summary>
        public bool Occupies(int x, int y)
        {
            if (item == null || item.data == null) return false;
            return x >= gridX && x < gridX + EffectiveWidth
                && y >= gridY && y < gridY + EffectiveHeight;
        }
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

    /// <summary>외부에서 변경 알림 발행</summary>
    public void NotifyChanged() => OnChanged?.Invoke();

    public InventoryGrid(int width, int height)
    {
        this.width = width;
        this.height = height;
        grid = new PlacedItem[width, height];
    }

    #region 조회

    /// <summary>해당 칸의 아이템 (없으면 null)</summary>
    public PlacedItem GetAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
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

    /// <summary>해당 위치에 아이템 배치 가능한지 (rotated: 90도 회전)</summary>
    public bool CanPlace(ItemInstance item, int x, int y, bool rotated = false)
    {
        if (item == null || item.data == null) return false;
        if (AcceptFilter != null && !AcceptFilter(item)) return false;

        int w = rotated ? item.data.gridHeight : item.data.gridWidth;
        int h = rotated ? item.data.gridWidth : item.data.gridHeight;

        // 범위 체크
        if (x < 0 || y < 0 || x + w > width || y + h > height) return false;

        // 점유 체크
        for (int gx = x; gx < x + w; gx++)
            for (int gy = y; gy < y + h; gy++)
                if (grid[gx, gy] != null)
                    return false;

        return true;
    }

    /// <summary>배치 시도. 성공하면 true. (rotated: 90도 회전)</summary>
    public bool TryPlace(ItemInstance item, int x, int y, bool rotated = false)
    {
        if (!CanPlace(item, x, y, rotated)) return false;

        var placed = new PlacedItem(item, x, y, rotated);
        items.Add(placed);

        // 격자에 참조 기록
        int w = placed.EffectiveWidth;
        int h = placed.EffectiveHeight;
        for (int gx = x; gx < x + w; gx++)
            for (int gy = y; gy < y + h; gy++)
                grid[gx, gy] = placed;

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

        int w = item.data.gridWidth;
        int h = item.data.gridHeight;
        for (int gy = 0; gy <= height - h; gy++)
            for (int gx = 0; gx <= width - w; gx++)
                if (CanPlace(item, gx, gy, false))
                    return true;

        if (w != h)
        {
            int rw = h, rh = w;
            for (int gy = 0; gy <= height - rh; gy++)
                for (int gx = 0; gx <= width - rw; gx++)
                    if (CanPlace(item, gx, gy, true))
                        return true;
        }

        return false;
    }

    /// <summary>빈 자리에 자동 배치. 성공하면 true. 회전도 시도.</summary>
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
                        OnChanged?.Invoke();
                        return true;
                    }
                }
            }
        }

        if (item.stackCount <= 0) return true;

        // 기본 방향 시도
        int w = item.data.gridWidth;
        int h = item.data.gridHeight;
        for (int gy = 0; gy <= height - h; gy++)
            for (int gx = 0; gx <= width - w; gx++)
                if (CanPlace(item, gx, gy, false))
                    return TryPlace(item, gx, gy, false);

        // 회전 시도 (가로세로가 다를 때만)
        if (w != h)
        {
            int rw = h, rh = w;
            for (int gy = 0; gy <= height - rh; gy++)
                for (int gx = 0; gx <= width - rw; gx++)
                    if (CanPlace(item, gx, gy, true))
                        return TryPlace(item, gx, gy, true);
        }

        return false; // 공간 부족
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

        // 격자에서 참조 해제 (회전 반영)
        int w = placed.EffectiveWidth;
        int h = placed.EffectiveHeight;
        for (int gx = placed.gridX; gx < placed.gridX + w; gx++)
            for (int gy = placed.gridY; gy < placed.gridY + h; gy++)
                if (gx >= 0 && gx < width && gy >= 0 && gy < height)
                    grid[gx, gy] = null;

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
        return target.item.TryStack(item);
    }

    #endregion

    #region 스왑

    /// <summary>해당 위치의 아이템과 교환. 기존 아이템 반환. (rotated: 새 아이템의 회전)</summary>
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
        bool oldRotated = existing.rotated;
        int oldX = existing.gridX;
        int oldY = existing.gridY;
        Remove(existing);

        // 새 아이템 배치 시도
        if (CanPlace(newItem, x, y, rotated))
        {
            TryPlace(newItem, x, y, rotated);
            return oldItem;
        }
        else
        {
            // 배치 실패하면 기존 아이템 복원
            TryPlace(oldItem, oldX, oldY, oldRotated);
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
