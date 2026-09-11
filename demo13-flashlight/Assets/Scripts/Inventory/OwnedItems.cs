using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 **지금 가진** 아이템 집계·차감 — 창고 + 가방 + 주머니를 한 덩어리로 본다.
/// (보안 컨테이너는 제외 — 개인 소지품)
///
/// 원래 `ArbeitBoard`에 있던 헬퍼인데, 아르바이트 보드가 폐기(2026-09-09, docs/scope-cut.md)되면서
/// 남은 사용처(`QuestBoard` 납품형 의뢰)를 위해 여기로 뽑아냈다.
/// </summary>
public static class OwnedItems
{
    /// <summary>창고+가방+주머니에 있는 해당 아이템 총 개수.</summary>
    public static int Count(ItemData data)
    {
        if (data == null) return 0;
        int n = 0;
        var stash = MainStash.Ensure()?.GetGrid();   // Instance 직접 접근 금지 — 부트 순서 무관 보장(상점/인벤과 동일)
        if (stash != null) n += stash.CountItem(data.itemId);
        var inv = Object.FindFirstObjectByType<PlayerInventory>();
        if (inv != null)
        {
            if (inv.Grid != null) n += inv.Grid.CountItem(data.itemId);
            if (inv.PocketsGrid != null) n += inv.PocketsGrid.CountItem(data.itemId);
        }
        return n;
    }

    /// <summary>차감 — **전부 있을 때만** 지운다(원자적). 창고 → 가방 → 주머니 순.</summary>
    public static bool TryRemove(ItemData data, int count)
    {
        if (data == null || count <= 0) return false;
        var plan = new List<(InventoryGrid grid, InventoryGrid.PlacedItem placed, int take)>();
        int need = count;
        var stash = MainStash.Ensure()?.GetGrid();
        if (stash != null) need = PlanRemove(stash, data, need, plan);
        var inv = Object.FindFirstObjectByType<PlayerInventory>();
        if (inv != null && need > 0)
        {
            if (inv.Grid != null) need = PlanRemove(inv.Grid, data, need, plan);
            if (inv.PocketsGrid != null && need > 0) need = PlanRemove(inv.PocketsGrid, data, need, plan);
        }
        if (need > 0) return false;

        foreach (var (grid, placed, take) in plan)
        {
            int stack = Mathf.Max(1, placed.item.stackCount);
            if (stack > take)
            {
                placed.item.stackCount = stack - take;
                grid.NotifyChanged();
            }
            else grid.Remove(placed);
        }
        return true;
    }

    static int PlanRemove(InventoryGrid grid, ItemData data, int count,
                          List<(InventoryGrid grid, InventoryGrid.PlacedItem placed, int take)> plan)
    {
        if (grid == null || data == null || count <= 0) return count;
        foreach (var placed in grid.GetAll())
        {
            if (count <= 0) break;
            if (placed?.item?.data == null || placed.item.data.itemId != data.itemId) continue;
            int stack = Mathf.Max(1, placed.item.stackCount);
            int take = Mathf.Min(stack, count);
            plan.Add((grid, placed, take));
            count -= take;
        }
        return count;
    }
}
