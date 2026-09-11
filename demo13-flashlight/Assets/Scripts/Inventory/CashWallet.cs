using UnityEngine;

/// <summary>현금 — 레이드에서 줍는 돈 (docs/economy.md §적 현금 드랍, 2026-09-11 사용자 결정).
/// 현금 아이템 1개 = ◈1(스택 수가 곧 금액), 무게 0. 들고 탈출하면 귀환 정산 때 ◈스크랩으로 바뀌고,
/// 죽으면 다른 소지품과 함께 잃는다("들고 나가야 내 것").</summary>
public static class CashWallet
{
    public const string ItemId = "cash";

    /// <summary>인벤(주머니·가방·보안 + 그 안에 든 가방)의 현금을 전부 빼서 스크랩으로 넣는다. 반환 = 넣은 금액.</summary>
    public static int SettleFromInventory(PlayerInventory inv)
    {
        if (inv == null) return 0;
        int total = 0;
        foreach (var g in new[] { inv.PocketsGrid, inv.Grid, inv.SecureGrid }) total += Drain(g);
        if (total > 0 && CurrencyManager.Instance != null) CurrencyManager.Instance.Add(total, "현금 정산");
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
