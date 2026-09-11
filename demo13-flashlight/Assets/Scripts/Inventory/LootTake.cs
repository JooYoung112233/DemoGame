using UnityEngine;

/// <summary>루팅에서 가져오기 — 인벤에 넣고, 원래 격자에서 빼고, 레이드 정산·수집 퀘스트에 기록하는 **한 곳**
/// (2026-09-11, docs/region-loot.md §루팅 정리 결정). 예전엔 바닥 줍기·시체 목록만 기록했고
/// 캐릭터 패널로 상자에서 꺼낸 것(드래그·TAKE ALL)은 레이드 결과·퀘스트에 안 잡혔다.</summary>
public static class LootTake
{
    /// <summary>한 칸 가져오기. 공간이 없거나 가방이 없으면 토스트 후 false.</summary>
    public static bool Take(PlayerInventory inv, InventoryGrid from, InventoryGrid.PlacedItem placed)
    {
        var it = placed?.item;
        if (inv == null || from == null || it == null) return false;
        if (!inv.TryAutoPlaceAnywhere(it, true))
        {
            ToastManager.Show(inv.HasBackpack ? "인벤토리 공간 부족" : "가방을 장착하세요", ToastManager.ToastType.Warning);
            return false;
        }
        from.Remove(placed);
        Record(it);
        return true;
    }

    /// <summary>이미 옮긴 아이템을 기록만 — 캐릭터 패널 드래그·TAKE ALL처럼 옮기는 쪽이 따로 있을 때.</summary>
    public static void Record(ItemInstance it)
    {
        if (it == null) return;
        if (RaidManager.Instance != null) RaidManager.Instance.TrackLoot(it);
        if (QuestManager.Instance != null && it.data != null)
            QuestManager.Instance.UpdateObjective(ObjectiveType.CollectItem, it.data.itemId, Mathf.Max(1, it.stackCount));
    }
}
