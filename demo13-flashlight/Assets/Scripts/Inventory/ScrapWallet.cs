using UnityEngine;

/// <summary>레이드에서 줍는 고철 화폐(◈) — "들고 나가야 내 것".
/// 2026-09-11 재화 역할 결정(docs/economy.md §재화 역할): 주 재화 = 고철, 현금 = 특별한 퀘스트·이벤트 전용.
/// 그래서 밴딧 시체가 현금 대신 이 아이템을 떨군다(§적 고철 드랍). 1개 = ◈1(스택 수가 곧 금액), 무게 0.
/// 들고 탈출하면 귀환 정산 때 ◈에 더해지고, 죽으면 다른 소지품과 함께 잃는다.</summary>
public static class ScrapWallet
{
    /// <summary>고철 화폐 아이템("◈ 고철"). 제작 재료 "고철 조각"(scrap_metal)과는 다른 물건이다.</summary>
    public const string ItemId = "scrap_money";

    /// <summary>현금 — 특별한 퀘스트·이벤트 전용이라 정산하지 않는다. (2026-09-11 전까진 밴딧 드랍이었다)</summary>
    public const string CashItemId = "cash";

    /// <summary>인벤(주머니·가방·보안 + 그 안에 든 가방)의 고철 화폐를 전부 빼서 ◈에 넣는다. 반환 = 넣은 금액.</summary>
    public static int SettleFromInventory(PlayerInventory inv)
    {
        if (inv == null) return 0;
        int total = 0;
        foreach (var g in new[] { inv.PocketsGrid, inv.Grid, inv.SecureGrid }) total += Drain(g);
        if (total > 0 && CurrencyManager.Instance != null) CurrencyManager.Instance.Add(total, "고철 정산");
        return total;
    }

    static int Drain(InventoryGrid g)
    {
        if (g == null) return 0;
        int sum = 0;
        foreach (var p in g.GetAll())
        {
            var it = p.item;
            if (it == null || it.data == null) continue;
            if (it.data.itemId == ItemId)
            {
                sum += Mathf.Max(1, it.stackCount);
                g.Remove(p);
                continue;
            }
            var inner = it.ContainerGrid;
            if (inner != null) sum += Drain(inner);
        }
        return sum;
    }
}
