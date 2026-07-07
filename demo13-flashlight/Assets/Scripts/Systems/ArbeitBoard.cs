using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 안전구역 아르바이트(납품 의뢰) 보드 — 로직 전담(UI는 MapSelectUI 아르바이트 패널).
/// 설계: docs/economy.md §안전구역 아르바이트 (2026-07-02).
///  - 의뢰 = 랜덤 재료(Material) 아이템 × 수량 → 납품 시 스크랩 보수.
///  - 보수 = sellPrice × 수량 × 배율(GameTuning.arbeitRewardMult, 10 단위 올림)
///    → 직접판매(sellRate≈0.6)보다 유리, 수배(×1.2 프리미엄)와 유사 축이되 "묶음 완수형".
///  - PP: 납품 누적 n회마다 +1 (GameTuning.arbeitPpEvery, TraitManager.GrantPP).
///  - 차감 순서: 창고 → 가방 → 주머니 (보안 컨테이너 제외 — 개인 소지품).
///  - 상태는 **런타임 전용**(세이브 안 함) — 위탁 슬롯/수배 remaining과 동일 원칙.
///    갱신 주기는 그레이박스 = 납품 완료 시 그 슬롯 즉시 재추첨(주기 갱신은 TBD).
/// </summary>
public static class ArbeitBoard
{
    public class Offer
    {
        public ItemData item;
        public int qty;
        public int reward;
    }

    static readonly List<Offer> offers = new List<Offer>();
    static int completedCount;   // 누적 납품 횟수 (PP 지급 판정용, 런타임 전용)
    static int pendingPp;        // TraitManager 부재 시점에 도달한 PP 회차 적립분

    /// <summary>세이브 로드/새 게임 시 런타임 상태 초기화 — 슬롯 간 이월 방지.</summary>
    public static void ResetRuntime()
    {
        offers.Clear();
        completedCount = 0;
        pendingPp = 0;
    }

    // ── 튜닝(GameTuning 폴백 기본값) ─────────────────────────────
    static int SlotCount => GameTuning.Instance != null ? GameTuning.Instance.arbeitSlotCount : 3;
    static int QtyMin => GameTuning.Instance != null ? GameTuning.Instance.arbeitQtyMin : 2;
    static int QtyMax => GameTuning.Instance != null ? GameTuning.Instance.arbeitQtyMax : 5;
    static float RewardMult => GameTuning.Instance != null ? GameTuning.Instance.arbeitRewardMult : 1.0f;
    public static int PpEvery => GameTuning.Instance != null ? GameTuning.Instance.arbeitPpEvery : 3;

    public static int CompletedCount => completedCount;

    /// <summary>현재 의뢰 목록(비어 있으면 생성).</summary>
    public static IReadOnlyList<Offer> Offers
    {
        get { EnsureOffers(); return offers; }
    }

    static void EnsureOffers()
    {
        while (offers.Count < SlotCount)
        {
            var o = Roll();
            if (o == null) break;   // 풀이 비면 무한루프 방지
            offers.Add(o);
        }
    }

    /// <summary>납품 의뢰 1건 추첨 — 재료(Material) 풀에서 랜덤(현재 걸린 품목과 중복 회피).</summary>
    static Offer Roll()
    {
        var pool = ItemDatabase.GetByCategory(ItemCategory.Material);
        if (pool != null)
            pool.RemoveAll(d => d == null || d.sellPrice <= 0);
        if (pool == null || pool.Count == 0) return null;

        ItemData pick = null;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            var cand = pool[Random.Range(0, pool.Count)];
            bool dup = false;
            for (int i = 0; i < offers.Count; i++)
                if (offers[i].item == cand) { dup = true; break; }
            if (!dup) { pick = cand; break; }
        }
        if (pick == null) pick = pool[Random.Range(0, pool.Count)];

        int qty = Random.Range(QtyMin, Mathf.Max(QtyMin, QtyMax) + 1);   // min>max 설정 방어
        int reward = Mathf.CeilToInt(pick.sellPrice * qty * RewardMult / 10f) * 10;   // 10 단위 올림
        return new Offer { item = pick, qty = qty, reward = Mathf.Max(10, reward) };
    }

    // ── 보유량 / 납품 ────────────────────────────────────────────

    /// <summary>납품 가능 보유량 = 창고 + 가방 + 주머니 (보안 컨테이너 제외).</summary>
    public static int CountOwned(ItemData data)
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

    /// <summary>의뢰(index) 납품: 아이템 차감(창고→가방→주머니) + 보수 지급 + PP 판정 + 슬롯 재추첨.
    /// 성공 시 true. UI는 반환 후 Refresh.</summary>
    public static bool Deliver(int index, out string resultMsg)
    {
        EnsureOffers();
        resultMsg = "";
        if (index < 0 || index >= offers.Count) return false;
        var o = offers[index];
        if (o == null || o.item == null) return false;

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[ArbeitBoard] CurrencyManager 없음 — 납품 중단(아이템 미차감)");
            resultMsg = "지금은 납품할 수 없다";
            return false;
        }

        // 차감 계획 수립: 창고 → 가방 → 주머니 순으로 뺄 스택을 먼저 확정.
        // 계획이 수량을 못 채우면 아무것도 제거하지 않고 실패 — 부분 차감/공짜 보수 방지.
        var plan = new List<(InventoryGrid grid, InventoryGrid.PlacedItem placed, int take)>();
        int need = o.qty;
        var stash = MainStash.Ensure()?.GetGrid();
        if (stash != null) need = PlanRemove(stash, o.item, need, plan);
        var inv = Object.FindFirstObjectByType<PlayerInventory>();
        if (inv != null && need > 0)
        {
            if (inv.Grid != null) need = PlanRemove(inv.Grid, o.item, need, plan);
            if (inv.PocketsGrid != null && need > 0) need = PlanRemove(inv.PocketsGrid, o.item, need, plan);
        }
        if (need > 0)
        {
            resultMsg = $"{o.item.displayName} 부족 ({o.qty - need}/{o.qty})";
            return false;
        }

        // 계획 실행 (전량 확보 확인 후이므로 항상 완결)
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

        CurrencyManager.Instance.Add(o.reward, $"아르바이트: {o.item.displayName} ×{o.qty}");

        completedCount++;
        bool ppGranted = false;
        if (PpEvery > 0 && completedCount % PpEvery == 0) pendingPp++;
        if (pendingPp > 0)
        {
            if (TraitManager.Instance != null)
            {
                TraitManager.Instance.GrantPP(pendingPp);
                pendingPp = 0;
                ppGranted = true;
            }
            else Debug.LogWarning("[ArbeitBoard] TraitManager 없음 — PP 지급 적립 후 다음 납품에 재시도");
        }

        // 완료 슬롯 즉시 재추첨(그레이박스 — 갱신 주기 TBD)
        var fresh = Roll();
        if (fresh != null) offers[index] = fresh;
        else offers.RemoveAt(index);

        SaveCheckpoints.Instance?.InventoryChanged();

        resultMsg = $"{o.item.displayName} ×{o.qty} 납품 (+◈{o.reward:N0})" + (ppGranted ? "  · PP +1!" : "");
        return true;
    }

    /// <summary>소지품(창고→가방→주머니, 보안 제외)에서 itemId 기준 count개 **원자 차감**.
    /// 전량 확보 못 하면 아무것도 빼지 않고 false. 게시판 의뢰(QuestBoard) 등 납품 공용.</summary>
    public static bool TryRemoveOwned(ItemData data, int count)
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

    /// <summary>격자에서 itemId 일치 스택을 count개 확보할 계획을 plan에 누적. 남은 필요 수량 반환.
    /// CountOwned와 동일하게 **itemId 문자열** 기준 — 참조 비교면 itemId 중복 에셋이 "세지는데 안 빠지는" 구멍이 된다.</summary>
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
