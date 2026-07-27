using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>스텝이 공유하는 실행 문맥.</summary>
public class QaContext
{
    public QaBot Bot;
    public QaReport Report;
    public QaTelemetry Tele;
    public System.Random Rng;
    public QaScenarioDef Scenario;
    public int Cycle;

    public int RandRange(int min, int max) => Rng.Next(min, max);
    public float Rand01() => (float)Rng.NextDouble();
}

/// <summary>
/// 시나리오 스텝(op) 구현 모음 — 사용자가 JSON에 적는 `op` 이름이 여기 등록된다. (docs/qa.md)
///
/// 원칙:
///   • **조작은 가능한 한 실제 입력**(GameInput 가상층)으로 — 이동·상호작용·패널 열기.
///   • **판정·거래는 게임 API 직접** — 상점 UI 클릭을 픽셀로 흉내내는 건 취약하므로,
///     경제식(ShopData.BuyPrice·sellRate)은 그대로 쓰되 트랜잭션은 API로 수행한다.
///     ("우리 게임이라 모든 접근 가능" — 목적은 픽셀 재현이 아니라 **밸런스·이상 탐지**.)
/// </summary>
public static class QaSteps
{
    public delegate IEnumerator StepFn(QaStepDef def, QaContext ctx);

    static Dictionary<string, StepFn> _ops;

    public static Dictionary<string, StepFn> Ops
    {
        get
        {
            if (_ops != null) return _ops;
            _ops = new Dictionary<string, StepFn>
            {
                { "wait",               Wait },
                { "cycle.begin",        CycleBegin },
                { "cycle.end",          CycleEnd },
                { "safehouse.ensure",   SafehouseEnsure },
                { "inventory.organize", InventoryOrganize },
                { "shop.sell",          ShopSell },
                { "shop.buy",           ShopBuy },
                { "quest.accept",       QuestAccept },
                { "raid.enter",         RaidEnter },
                { "raid.explore",       RaidExplore },
                { "raid.extract",       RaidExtract },
                { "settle.verify",      SettleVerify },
            };
            return _ops;
        }
    }

    public static bool Has(string op) => !string.IsNullOrEmpty(op) && Ops.ContainsKey(op);

    // ══════════════════════════════════════════════════════════════
    //  기본
    // ══════════════════════════════════════════════════════════════

    static IEnumerator Wait(QaStepDef d, QaContext c)
    {
        yield return c.Bot.WaitSec(d.budgetSec > 0 ? d.budgetSec : 1f);
    }

    static IEnumerator CycleBegin(QaStepDef d, QaContext c)
    {
        c.Tele.BeginCycle(c.Cycle);
        c.Report.Info("cycle", "BEGIN", $"사이클 {c.Cycle} 시작 — 소지금 {QaTelemetry.Money()} · Lv{QaTelemetry.Level()}");
        yield break;
    }

    static IEnumerator CycleEnd(QaStepDef d, QaContext c)
    {
        c.Tele.EndCycle();
        var m = c.Tele.Cycles.Count > 0 ? c.Tele.Cycles[c.Tele.Cycles.Count - 1] : null;
        if (m != null)
            c.Report.Info("cycle", "END",
                $"사이클 {m.cycle} 종료 — 소지금 {m.moneyStart}→{m.moneyEnd} · Lv {m.levelStart}→{m.levelEnd} · 루팅가치 {m.lootValue} · {m.durationSec:0}s");
        yield break;
    }

    // ══════════════════════════════════════════════════════════════
    //  정비 (안전가옥)
    // ══════════════════════════════════════════════════════════════

    static IEnumerator SafehouseEnsure(QaStepDef d, QaContext c)
    {
        const string Safehouse = "Safehouse";
        if (SceneManager.GetActiveScene().name == Safehouse) { c.Report.Info("safehouse", "OK", "이미 안전가옥"); yield break; }

        if (SceneTransitionManager.Instance == null)
        { c.Report.Error("safehouse", "NO_TRANSITION_MGR", "SceneTransitionManager 없음"); yield break; }

        // 진행 중인 전환이 있으면 요청이 조용히 무시된다 — 끝나길 먼저 기다린다.
        yield return c.Bot.WaitUntil(() => !SceneTransitionManager.Instance.IsTransitioning, 15f, null);
        SceneTransitionManager.Instance.TransitionTo(Safehouse, "default");
        float budget = d.budgetSec > 0 ? d.budgetSec : 25f;
        bool ok = false;
        yield return c.Bot.WaitUntil(() => SceneManager.GetActiveScene().name == Safehouse, budget, r => ok = r);

        if (!ok) yield return c.Bot.Blocked("safehouse", "SCENE_TIMEOUT", $"안전가옥 복귀가 {budget:0}초 내 실패", "TransitionTo(Safehouse) 호출함");
        else c.Report.Info("safehouse", "OK", "안전가옥 복귀");
    }

    /// <summary>인벤 정리 — 무거운/안 쓰는 건 창고로, 의료·소비는 챙긴다. 무게 초과 해소.</summary>
    static IEnumerator InventoryOrganize(QaStepDef d, QaContext c)
    {
        var inv = c.Bot.Inv;
        if (inv == null) { c.Report.Warn("organize", "NO_INV", "PlayerInventory 없음"); yield break; }

        var stash = MainStash.Ensure()?.GetGrid();
        if (stash == null) { c.Report.Warn("organize", "NO_STASH", "메인 창고 없음 — 정리 스킵"); yield break; }

        int before = QaTelemetry.ItemsHeld();
        int moved = 0, lost = 0;

        // 가방의 비의료·비소비 아이템을 창고로 (다음 레이드 무게 확보)
        foreach (var g in new[] { inv.Grid, inv.PocketsGrid })
        {
            if (g == null) continue;
            foreach (var p in g.GetAll())
            {
                var data = p?.item?.data;
                if (data == null) continue;
                if (data.category == ItemCategory.Medical || data.category == ItemCategory.Consumable) continue;

                var inst = p.item;
                g.Remove(p);
                if (stash.TryAutoPlace(inst)) moved++;
                else if (!g.TryAutoPlace(inst)) { lost++; }   // 창고도 가방도 못 받으면 유실
            }
        }

        if (lost > 0) c.Report.Error("organize", "ITEM_LOST", $"창고·가방 모두 수용 실패로 {lost}개 유실 — 격자 로직 확인");
        c.Report.Info("organize", "OK", $"창고로 {moved}개 이동 (소지 {before}→{QaTelemetry.ItemsHeld()})");

        // 무게 초과가 남아 있으면 밸런스 신호
        if (inv.IsOverweight)
            c.Report.Warn("organize", "OVERWEIGHT", $"정리 후에도 무게 초과 (비율 {inv.WeightRatio:0.00}) — 가방 용량/무게 밸런스 확인");

        yield return c.Bot.WaitSec(0.2f);
    }

    /// <summary>창고 잡템 판매 — 실제 경제식(sellPrice × shop.sellRate) 사용.</summary>
    static IEnumerator ShopSell(QaStepDef d, QaContext c)
    {
        var shop = PickShop(d.param);
        if (shop == null) { c.Report.Warn("shop", "NO_SHOP", "ShopData 에셋을 못 찾음 — 판매 스킵"); yield break; }

        var stash = MainStash.Ensure()?.GetGrid();
        if (stash == null) { c.Report.Warn("shop", "NO_STASH", "창고 없음"); yield break; }

        float ratio = d.ratio > 0f ? Mathf.Clamp01(d.ratio) : 0.7f;
        var all = stash.GetAll();
        int target = Mathf.FloorToInt(all.Count * ratio);
        int sold = 0, gained = 0;

        for (int i = 0; i < all.Count && sold < target; i++)
        {
            var p = all[i];
            var data = p?.item?.data;
            if (data == null) continue;
            if (data.category == ItemCategory.Medical) continue;      // 의료는 남긴다(생존)
            if (data.category == ItemCategory.Key) continue;          // 열쇠는 팔지 않는다

            int unit = Mathf.Max(1, Mathf.RoundToInt(data.sellPrice * shop.sellRate));
            int amount = unit * Mathf.Max(1, p.item.stackCount);

            stash.Remove(p);
            CurrencyManager.Instance?.Add(amount, "QA 판매");
            gained += amount; sold++;
        }

        if (c.Tele.Current != null) c.Tele.Current.earned += gained;
        c.Report.Info("shop", "SELL", $"{shop.shopName}에 {sold}건 판매 → +{gained} (sellRate {shop.sellRate:0.##})");

        if (sold > 0 && gained == 0)
            c.Report.Warn("shop", "ZERO_PRICE", "판매했는데 수익 0 — sellPrice 0 아이템 다수 의심");
        yield return c.Bot.WaitSec(0.2f);
    }

    /// <summary>상점 구매 — 카테고리 필터 + 소지금 상한 비율. 실제 BuyPrice 사용.</summary>
    static IEnumerator ShopBuy(QaStepDef d, QaContext c)
    {
        var shop = PickShop(null);
        if (shop == null) { c.Report.Warn("shop", "NO_SHOP", "ShopData 없음 — 구매 스킵"); yield break; }
        var cur = CurrencyManager.Instance;
        if (cur == null) { c.Report.Warn("shop", "NO_CURRENCY", "CurrencyManager 없음"); yield break; }

        ItemCategory? want = null;
        if (!string.IsNullOrEmpty(d.param) && d.param != "auto"
            && System.Enum.TryParse<ItemCategory>(d.param, out var parsed)) want = parsed;

        float ratio = d.ratio > 0f ? Mathf.Clamp01(d.ratio) : 0.5f;
        int budget = Mathf.FloorToInt(cur.Balance * ratio);
        int maxCount = d.count > 0 ? d.count : 3;

        var candidates = new List<ItemData>();
        foreach (var it in shop.stock)
            if (it != null && (want == null || it.category == want.Value)) candidates.Add(it);

        if (candidates.Count == 0)
        {
            c.Report.Warn("shop", "NO_STOCK", $"{shop.shopName} 재고에 조건({d.param}) 맞는 물건 없음");
            yield break;
        }

        var stash = MainStash.Ensure()?.GetGrid();
        int bought = 0, spent = 0;
        for (int i = 0; i < maxCount; i++)
        {
            // 자유도: 후보 중 무작위 선택(시드 고정이라 재현 가능)
            var pick = candidates[c.RandRange(0, candidates.Count)];
            int price = shop.BuyPrice(pick);
            if (price > budget - spent || !cur.CanAfford(price)) break;

            if (!cur.Spend(price, "QA 구매")) break;
            spent += price; bought++;

            var inst = new ItemInstance(pick, 1);
            bool stored = (stash != null && stash.TryAutoPlace(inst))
                          || (c.Bot.Inv != null && c.Bot.Inv.TryAutoPlaceAnywhere(inst));
            if (!stored)
                c.Report.Error("shop", "ITEM_LOST", $"{pick.displayName} 구매했으나 창고·가방 모두 수용 실패 — 돈만 나감({price})");
        }

        if (c.Tele.Current != null) c.Tele.Current.spent += spent;
        c.Report.Info("shop", "BUY", $"{shop.shopName}에서 {bought}개 구매 → -{spent} (잔액 {cur.Balance})");

        if (bought == 0 && budget > 0)
            c.Report.Warn("shop", "TOO_EXPENSIVE", $"예산 {budget}으로 아무것도 못 삼 — 최저가 대비 소지금이 너무 적음(초반 경제 확인)");
        yield return c.Bot.WaitSec(0.2f);
    }

    static ShopData PickShop(string shopId)
    {
        var shops = Resources.LoadAll<ShopData>("Data/Shops");
        if (shops == null || shops.Length == 0) return null;
        if (!string.IsNullOrEmpty(shopId) && shopId != "auto")
            foreach (var s in shops) if (s != null && s.shopId == shopId) return s;
        return shops[0];
    }

    /// <summary>의뢰 수주 — 게시판(BD) 우선, 없으면 BQ.</summary>
    static IEnumerator QuestAccept(QaStepDef d, QaContext c)
    {
        var qm = QuestManager.Instance;
        if (qm == null) { c.Report.Warn("quest", "NO_MANAGER", "QuestManager 없음"); yield break; }

        int want = d.count > 0 ? d.count : 1;
        int got = 0;

        var offers = QuestBoard.TodayOffers;
        if (offers != null)
        {
            for (int i = 0; i < offers.Count && got < want; i++)
            {
                if (QuestBoard.Accept(offers[i], out string msg)) { got++; c.Report.Info("quest", "ACCEPT", $"{offers[i].title}"); }
                else if (!string.IsNullOrEmpty(msg)) c.Report.Info("quest", "REJECT", $"{offers[i].questId}: {msg}");
            }
        }

        if (got < want)
        {
            var bq = QuestBoard.BqOffers();
            if (bq != null)
                for (int i = 0; i < bq.Count && got < want; i++)
                    if (QuestBoard.AcceptBq(bq[i], out string m2)) { got++; c.Report.Info("quest", "ACCEPT_BQ", $"{bq[i].title}"); }
        }

        if (c.Tele.Current != null) c.Tele.Current.questsAccepted += got;
        if (got == 0) c.Report.Warn("quest", "NO_ACCEPT", "수주 가능한 의뢰가 0 — 게시판 생성/평판 게이트 확인");
        else c.Report.Info("quest", "OK", $"{got}건 수주 (활성 {qm.ActiveQuests.Count})");
        yield return c.Bot.WaitSec(0.2f);
    }

    // ══════════════════════════════════════════════════════════════
    //  모험 (레이드)
    // ══════════════════════════════════════════════════════════════

    static IEnumerator RaidEnter(QaStepDef d, QaContext c)
    {
        string scene = null;
        if (!string.IsNullOrEmpty(d.param) && d.param != "auto")
        {
            foreach (var r in WorldRegionCatalog.All)
                if (r.regionId == d.param && !string.IsNullOrEmpty(r.sceneName)) { scene = r.sceneName; break; }
        }
        if (scene == null)
            foreach (var r in WorldRegionCatalog.All)
                if (!string.IsNullOrEmpty(r.sceneName)) { scene = r.sceneName; break; }

        if (scene == null)
        { c.Report.Error("raid", "NO_PLAYABLE_REGION", "플레이 가능한 지역이 없음(sceneName 전부 비어 있음)"); yield break; }
        if (SceneTransitionManager.Instance == null)
        { c.Report.Error("raid", "NO_TRANSITION_MGR", "SceneTransitionManager 없음"); yield break; }

        c.Bot.MarkLootBaseline();
        yield return c.Bot.WaitUntil(() => !SceneTransitionManager.Instance.IsTransitioning, 15f, null);
        SceneTransitionManager.Instance.TransitionTo(scene, "");
        float budget = d.budgetSec > 0 ? d.budgetSec : 25f;
        bool ok = false;
        yield return c.Bot.WaitUntil(() => SceneManager.GetActiveScene().name == scene, budget, r => ok = r);

        if (!ok)
        {
            yield return c.Bot.Blocked("raid", "SCENE_TIMEOUT",
                $"{scene} 로드가 {budget:0}초 내 실패 — 빌드세팅 미등록 의심", $"TransitionTo({scene}, \"\")");
            yield break;
        }
        c.Report.Info("raid", "ENTER", $"레이드 진입: {scene}");
        yield return c.Bot.WaitSec(1f);
    }

    /// <summary>탐색 — 상자를 훑고, wander면 무작위 배회도 섞는다(자유도).</summary>
    static IEnumerator RaidExplore(QaStepDef d, QaContext c)
    {
        float budget = d.budgetSec > 0 ? d.budgetSec : 120f;
        int wantCrates = d.count > 0 ? d.count : 5;
        float deadline = Time.realtimeSinceStartup + budget;

        var crates = new List<LootContainer>(Object.FindObjectsByType<LootContainer>(FindObjectsSortMode.None));
        if (crates.Count == 0) c.Report.Warn("explore", "NO_CRATE", "씬에 LootContainer 0개 — 루팅 불가");
        c.Report.Metric("씬 상자 수", crates.Count);

        int opened = 0;
        while (opened < wantCrates && Time.realtimeSinceStartup < deadline)
        {
            // 자유도: 가장 가까운 것만 고르지 않고 가까운 3개 중 무작위
            var near = c.Bot.NearestCrates(crates, 3);
            if (near.Count == 0) break;
            var target = near[c.RandRange(0, near.Count)];
            crates.Remove(target);

            string raidScene = SceneManager.GetActiveScene().name;
            bool reached = false;
            yield return c.Bot.MoveTo(target.transform.position, 1.6f,
                                      Mathf.Min(20f, Mathf.Max(3f, deadline - Time.realtimeSinceStartup)), r => reached = r);

            // 이동 중 사망·씬 언로드로 레이드를 벗어났으면 탐색 중단(파괴된 참조 접근 방지).
            if (SceneManager.GetActiveScene().name != raidScene)
            { c.Report.Warn("explore", "SCENE_LEFT", "탐색 중 레이드 씬을 벗어남(사망?) — 탐색 중단"); break; }
            if (target == null) { c.Report.Warn("explore", "CRATE_GONE", "이동 중 상자가 사라짐"); continue; }
            if (!reached) { c.Report.Warn("explore", "UNREACHABLE", $"상자 '{target.ContainerName}' 도달 실패(길막힘?)"); continue; }

            c.Bot.Tap(KeyCode.E);
            yield return c.Bot.WaitSec(1.2f);
            if (target == null) { c.Report.Warn("explore", "CRATE_GONE", "수색 중 상자가 사라짐"); continue; }
            opened++;

            int taken = c.Bot.TakeAllFrom(target);
            c.Report.Info("explore", "CRATE", $"'{target.ContainerName}' 수색 — {taken}개 회수");
            c.Bot.Tap(KeyCode.Escape);
            yield return c.Bot.WaitSec(0.3f);

            if (d.wander && Time.realtimeSinceStartup < deadline)
                yield return c.Bot.Wander(c, 2.5f);   // 사이사이 배회 — 스턱·구멍 발견 확률 ↑
        }

        if (c.Tele.Current != null) c.Tele.Current.cratesOpened += opened;
        c.Report.Info("explore", "OK", $"상자 {opened}곳 수색 (목표 {wantCrates})");
        if (opened == 0 && crates.Count > 0)
            c.Report.Error("explore", "NO_LOOT_REACHED", "상자가 있는데 한 곳도 도달·수색 못 함 — 길찾기/충돌 확인");
    }

    static IEnumerator RaidExtract(QaStepDef d, QaContext c)
    {
        float budget = d.budgetSec > 0 ? d.budgetSec : 90f;
        InteractableObject best = null; float bestD = float.MaxValue;
        var player = TopDownPlayer.Instance;
        if (player == null) { c.Report.Error("extract", "NO_PLAYER", "플레이어 없음"); yield break; }

        foreach (var io in Object.FindObjectsByType<InteractableObject>(FindObjectsSortMode.None))
        {
            if (io == null || !io.gameObject.activeInHierarchy) continue;
            if (io.Type != InteractableObject.InteractType.ExitPoint) continue;
            float dist = Vector2.Distance(io.transform.position, player.transform.position);
            if (dist < bestD) { bestD = dist; best = io; }
        }

        if (best == null)
        {
            yield return c.Bot.Blocked("extract", "NO_EXIT", "활성 탈출구가 없어 레이드에서 나갈 수 없음", "씬의 ExitPoint 전수 검색");
            yield break;
        }

        string raidScene = SceneManager.GetActiveScene().name;
        bool reached = false;
        yield return c.Bot.MoveTo(best.transform.position, 1.4f, budget * 0.6f, r => reached = r);
        if (!reached)
        {
            yield return c.Bot.Blocked("extract", "EXIT_UNREACHABLE",
                $"탈출구 '{best.name}'까지 도달 실패 (직선거리 {bestD:0.#}m)", "MoveTo 반복 시도");
            yield break;
        }

        c.Bot.Tap(KeyCode.E);
        bool left = false;
        yield return c.Bot.WaitUntil(() => SceneManager.GetActiveScene().name != raidScene, budget * 0.4f, r => left = r);

        if (!left) yield return c.Bot.Blocked("extract", "EXTRACT_TIMEOUT", "탈출 상호작용 후 씬 전환 없음", "E키 상호작용 1회");
        else
        {
            if (c.Tele.Current != null) c.Tele.Current.raidCompleted = true;
            c.Report.Info("extract", "OK", $"탈출 성공 → {SceneManager.GetActiveScene().name}");
        }
    }

    static IEnumerator SettleVerify(QaStepDef d, QaContext c)
    {
        yield return c.Bot.WaitSec(d.budgetSec > 0 ? Mathf.Min(d.budgetSec, 4f) : 2.5f);

        int lootValue = c.Bot.LootValueSinceBaseline();
        if (c.Tele.Current != null) c.Tele.Current.lootValue += lootValue;

        var s = RaidManager.LastSettlement;
        if (s == null) { c.Report.Warn("settle", "NO_SETTLEMENT", "정산 데이터 없음(LastSettlement)"); yield break; }

        int xp = s.killXp + s.extractBonus;
        if (c.Tele.Current != null) c.Tele.Current.xpGained += xp;

        c.Report.Info("settle", "OK",
            $"정산 — 성공={s.success} 생존 {s.survivalTime:0}s · 킬XP {s.killXp} · 탈출 {s.extractBonus} · 루팅가치 {lootValue}");

        if (s.success && lootValue == 0)
            c.Report.Warn("settle", "EMPTY_RAID", "탈출은 했는데 가져온 가치가 0 — 루팅/예산제 확인");
    }
}
