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
                { "title.newgame",      TitleNewGame },
                { "title.continue",     TitleContinue },
                { "story.skip",         StorySkip },
                { "goto",               GotoInteract },
                { "safehouse.ensure",   SafehouseEnsure },
                { "inventory.organize", InventoryOrganize },
                { "shop.sell",          ShopSell },
                { "shop.buy",           ShopBuy },
                { "quest.accept",       QuestAccept },
                { "raid.enter",         RaidEnter },
                { "raid.explore",       RaidExplore },
                { "ai.play",            AiPlay },
                { "combat.engage",      CombatEngage },
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
    //  게임 시작 (타이틀 → 프롤로그) — 신규 유저가 실제로 겪는 구간
    // ══════════════════════════════════════════════════════════════

    /// <summary>타이틀에서 새 게임 시작. **신규 유저 경로를 실제로 탄다**(프롤로그 포함).
    /// ※ 가상 입력은 uGUI EventSystem을 못 움직이므로 버튼은 API로 누른다(TitleScreen.StartNewGame).</summary>
    static IEnumerator TitleNewGame(QaStepDef d, QaContext c)
    {
        int slot = d.count;   // 0~2
        float budget = d.budgetSec > 0 ? d.budgetSec : 40f;

        if (!TitleScreen.IsShowing)
        { c.Report.Warn("title", "NO_TITLE", "타이틀 화면이 아님 — 새 게임 스킵(이미 인게임?)"); yield break; }
        if (!TitleScreen.StartNewGame(slot))
        { c.Report.Error("title", "NEW_GAME_FAIL", "새 게임 호출 실패"); yield break; }
        c.Report.Info("title", "NEW_GAME", $"슬롯 {slot} 새 게임 시작");

        bool ok = false;
        yield return c.Bot.WaitUntil(() => SceneManager.GetActiveScene().name == "Safehouse", budget, r => ok = r);
        if (!ok) { yield return c.Bot.Blocked("title", "NEW_GAME_TIMEOUT", $"새 게임 후 {budget:0}초 내 안전가옥 미도달", "TitleScreen.StartNewGame"); yield break; }
        c.Report.Info("title", "OK", "안전가옥 진입");
    }

    static IEnumerator TitleContinue(QaStepDef d, QaContext c)
    {
        int slot = d.count;
        if (!TitleScreen.ContinueGame(slot)) { c.Report.Warn("title", "NO_SAVE", $"슬롯 {slot} 세이브 없음 — 이어하기 스킵"); yield break; }
        bool ok = false;
        yield return c.Bot.WaitUntil(() => SceneManager.GetActiveScene().name == "Safehouse", d.budgetSec > 0 ? d.budgetSec : 40f, r => ok = r);
        c.Report.Info("title", ok ? "OK" : "TIMEOUT", ok ? "이어하기 진입" : "이어하기 후 안전가옥 미도달");
    }

    /// <summary>스토리(내레이션·대화·튜토)를 눌러 넘긴다 — 프롤로그·튜토 구간 통과용.
    /// 내레이션은 Space/좌클릭/Enter로 진행한다(NarrationUI). 타이핑 중이면 1번은 즉시완성, 2번째에 다음 줄.</summary>
    static IEnumerator StorySkip(QaStepDef d, QaContext c)
    {
        float budget = d.budgetSec > 0 ? d.budgetSec : 60f;
        float end = Time.realtimeSinceStartup + budget;
        int taps = 0;

        bool Busy()
            => (NarrationUI.Instance != null && NarrationUI.Instance.IsShowing)
               || (DialogueUI.Instance != null && DialogueUI.Instance.IsShowing)
               || (StoryPlayer.Instance != null && StoryPlayer.Instance.IsPlaying);

        // 스토리가 시작되기까지 잠깐 기다린다(프롤로그는 페이드 뒤에 뜬다).
        float w = 0f;
        while (!Busy() && w < 4f) { w += Time.unscaledDeltaTime; yield return null; }

        if (!Busy()) { c.Report.Info("story", "NONE", "넘길 스토리 없음"); yield break; }

        while (Busy() && Time.realtimeSinceStartup < end)
        {
            c.Bot.Tap(KeyCode.Space);
            taps++;
            yield return c.Bot.WaitSec(0.35f);
        }

        bool cleared = !Busy();
        if (cleared) c.Report.Info("story", "SKIPPED", $"스토리 통과 ({taps}회 입력, {budget - (end - Time.realtimeSinceStartup):0.0}s)");
        else yield return c.Bot.Blocked("story", "STORY_STUCK",
                $"{budget:0}초 동안 {taps}회 눌렀는데 스토리가 안 끝남 — 진행 불가 지점(신규 유저가 여기서 막힌다)",
                "Space 반복 입력");
    }

    // ══════════════════════════════════════════════════════════════
    //  실제 이동·상호작용
    // ══════════════════════════════════════════════════════════════

    /// <summary>지정 종류의 상호작용 대상까지 **실제로 걸어가서 E를 누른다**.
    ///
    /// 왜 필요한가: 거래·수주를 API로만 하면 **"그 NPC에 갈 수 있는가 / 상호작용이 배선돼 있는가 /
    /// UI가 열리는가"를 전혀 검증하지 못한다**(그래서 6스텝이 4초에 끝나던 문제).
    /// 이 op가 도달성·상호작용 배선·UI 오픈을 실제로 확인한다.
    ///
    /// param = InteractType 이름(Stash, NPC, MapBoard, Workbench, Bed, Radio, Dispatch …)
    /// ※ 거래 버튼 클릭 자체는 uGUI EventSystem이라 가상 입력으로 못 누른다 — 그 부분만 API로 남는다.
    /// </summary>
    static IEnumerator GotoInteract(QaStepDef d, QaContext c)
    {
        if (string.IsNullOrEmpty(d.param) ||
            !System.Enum.TryParse<InteractableObject.InteractType>(d.param, out var want))
        { c.Report.Warn("goto", "BAD_PARAM", $"InteractType '{d.param}' 파싱 실패"); yield break; }

        var player = TopDownPlayer.Instance;
        if (player == null) { c.Report.Error("goto", "NO_PLAYER", "플레이어 없음"); yield break; }

        // 현재 씬에서 해당 종류 중 가장 가까운 것
        InteractableObject best = null; float bestD = float.MaxValue;
        foreach (var io in Object.FindObjectsByType<InteractableObject>(FindObjectsSortMode.None))
        {
            if (io == null || !io.gameObject.activeInHierarchy) continue;
            if (io.Type != want) continue;
            float dist = Vector2.Distance(io.transform.position, player.transform.position);
            if (dist < bestD) { bestD = dist; best = io; }
        }

        if (best == null)
        {
            c.Report.Warn("goto", "NOT_PLACED",
                $"'{want}' 상호작용 대상이 이 씬({SceneManager.GetActiveScene().name})에 **배치돼 있지 않음** — 해당 기능에 접근 불가");
            yield break;
        }

        float budget = d.budgetSec > 0 ? d.budgetSec : 40f;
        bool reached = false;
        yield return c.Bot.MoveTo(best.transform.position, 1.4f, budget, r => reached = r);

        if (!reached)
        {
            c.Bot.NoteUnreachable();
            yield return c.Bot.Blocked("goto", "UNREACHABLE",
                $"'{want}'({best.name})까지 {budget:0}초 내 도달 실패 — 직선거리 {bestD:0.#}m, 길막힘 의심",
                "MoveTo 반복");
            yield break;
        }

        bool uiBefore = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();
        c.Bot.Tap(KeyCode.E);
        yield return c.Bot.WaitSec(0.8f);
        bool uiAfter = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();

        c.Report.Info("goto", "OK", $"'{want}'({best.name}) 도달 + E 상호작용 — UI {(uiAfter ? "열림" : "안 열림")}");

        // UI가 열려야 하는 종류인데 안 열리면 배선 문제
        bool expectsUi = want == InteractableObject.InteractType.NPC
                      || want == InteractableObject.InteractType.MapBoard
                      || want == InteractableObject.InteractType.Stash
                      || want == InteractableObject.InteractType.Workbench
                      || want == InteractableObject.InteractType.Radio
                      || want == InteractableObject.InteractType.Dispatch;
        if (expectsUi && !uiAfter && !uiBefore)
            c.Report.Error("goto", "NO_UI",
                $"'{want}'에 상호작용했는데 UI가 안 열림 — 상호작용 배선 or UI 오픈 실패");

        // 열렸으면 닫는다(다음 스텝이 이동해야 하므로)
        if (uiAfter && d.count == 0)
        {
            yield return c.Bot.CloseUi("goto");
            if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen())
                c.Report.Warn("goto", "UI_NOT_CLOSED", $"'{want}' UI가 ESC로 안 닫힘");
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  정비 (안전가옥)
    // ══════════════════════════════════════════════════════════════

    static IEnumerator SafehouseEnsure(QaStepDef d, QaContext c)
    {
        const string Safehouse = "Safehouse";
        float budget0 = d.budgetSec > 0 ? d.budgetSec : 25f;

        // ★ 타이틀이 떠 있으면 **정상 경로로 들어가야 한다**.
        //   씬만 강제 전환하면 타이틀 캔버스(sortingOrder 500)가 안 꺼져 화면이 타이틀에 덮인 채 남는다
        //   (게임은 뒤에서 도는데 화면은 그대로 = "화면이 안 꺼짐"). Hide()는 새게임/이어하기에서만 호출된다.
        if (TitleScreen.IsShowing)
        {
            bool started = TitleScreen.ContinueGame(0);          // 세이브 있으면 이어하기
            if (!started) started = TitleScreen.StartNewGame(0); // 없으면 새 게임
            c.Report.Info("safehouse", "TITLE", started ? "타이틀에서 정상 진입(캔버스 해제)" : "타이틀 진입 실패");

            bool ok0 = false;
            yield return c.Bot.WaitUntil(
                () => SceneManager.GetActiveScene().name == Safehouse && !TitleScreen.IsShowing, budget0, r => ok0 = r);

            if (!ok0)
                yield return c.Bot.Blocked("safehouse", "TITLE_STUCK",
                    $"타이틀에서 {budget0:0}초 내 진입 실패(타이틀 표시={TitleScreen.IsShowing})", "ContinueGame→StartNewGame");
            else c.Report.Info("safehouse", "OK", "안전가옥 진입");
            yield break;
        }

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

    // ── 건물 안/밖 (2026-07-11 "건물 = 전당포식 씬 전환") ────────────────
    //  건물 내부는 **별도 씬**이고, 출입은 E키가 아니라 BuildingEntrance **트리거**다.
    //  봇은 밖에서 상자로 걸어가다 입구를 밟아 그냥 빨려 들어간다 — 2026-07-28 QA에서
    //  3사이클 전부 Int_Generic에 갇혀 NO_EXIT로 끝났다(내부엔 탈출구가 없다).

    /// <summary>레이드가 이어지는 씬인가(외부 맵 또는 그 건물 내부).</summary>
    static bool IsRaidScene(string n) =>
        !string.IsNullOrEmpty(n) && (n.StartsWith("Int_") || (n != "Safehouse" && n != "Hideout"
            && n != "Systems" && n != "Pawnshop" && n != "ScrapMarket_GB"));

    static bool IsInterior() => SceneManager.GetActiveScene().name.StartsWith("Int_");

    /// <summary>건물 안이면 나가는 문(트리거)을 밟아 밖으로 나온다. 중첩 대비 최대 3번.</summary>
    static IEnumerator LeaveBuildingIfInside(QaContext c)
    {
        for (int hop = 0; hop < 3 && IsInterior(); hop++)
        {
            var player = TopDownPlayer.Instance;
            if (player == null) yield break;

            string from = SceneManager.GetActiveScene().name;
            BuildingEntrance door = null; float bestD = float.MaxValue;
            foreach (var be in Object.FindObjectsByType<BuildingEntrance>(FindObjectsSortMode.None))
            {
                if (be == null || !be.IsExit || string.IsNullOrEmpty(be.TargetScene)) continue;
                if (!be.gameObject.activeInHierarchy) continue;
                float dist = Vector2.Distance(be.transform.position, player.transform.position);
                if (dist < bestD) { bestD = dist; door = be; }
            }

            if (door == null)
            {
                c.Report.Error("building", "NO_INTERIOR_EXIT",
                    $"[{from}] 건물 내부인데 나가는 문(BuildingEntrance isExit)이 없다 — 플레이어가 갇힌다");
                yield break;
            }

            bool reached = false;
            // 트리거(문 1.3×1.0)를 실제로 밟아야 발동하므로 도착 판정을 좁게 잡는다.
            yield return c.Bot.MoveTo(door.transform.position, 0.6f, 25f, r => reached = r);
            yield return c.Bot.WaitSec(1.5f);   // 페이드 + additive 전환 대기

            string now = SceneManager.GetActiveScene().name;
            if (now != from)
            {
                c.Report.Info("building", "LEAVE", $"건물 밖으로 나옴 {from} → {now}");
                continue;
            }

            c.Report.Error("building", "EXIT_DOOR_FAIL",
                $"[{from}] 출구 문까지 {(reached ? "도달했는데" : "도달 못 해")} 씬이 안 바뀜 — 트리거 미발동 의심");
            yield break;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  ai.play — 플레이어 AI (2026-07-28)
    //
    //  정해진 순서를 재생하는 대신 **상황을 보고 목표를 고른다**.
    //  아는 것은 QaPerception이 준다(시야에 들어온 것만) — 전지 상태에서는
    //  루팅가치가 "사람이 얻을 값"이 아니라 이론 최대값이 되어 밸런스가 측정 안 된다.
    //  판단 근거는 전부 qa-decisions.jsonl에 남고, 애매하면 qa-ask.json으로 신고한다.
    // ══════════════════════════════════════════════════════════════════

    static bool IsCorpse(LootContainer box) =>
        box != null && box.GetComponent<EnemyController>() != null;

    static QaBrain.State BuildState(QaContext c, float t0, float budget, HashSet<int> opened,
                                     HashSet<int> passageDone, int moveFailStreak)
    {
        var bot = c.Bot;
        var per = bot.Perception;
        var player = TopDownPlayer.Instance;
        Vector2 pos = player != null ? (Vector2)player.transform.position : Vector2.zero;

        var s = new QaBrain.State
        {
            hp = PlayerHpRatio(),
            timeUsed = Mathf.Clamp01((Time.realtimeSinceStartup - t0) / Mathf.Max(1f, budget)),
            inInterior = IsInterior(),
            scene = SceneManager.GetActiveScene().name,
            crateDist = -1f, corpseDist = -1f, enemyDist = -1f, exitDist = -1f, doorDist = -1f, passageDist = -1f,
            blockedRecently = moveFailStreak > 0,
        };

        var inv = player != null ? player.GetComponent<PlayerInventory>() : null;
        s.weight = (inv != null && inv.MaxWeight > 0f) ? Mathf.Clamp01(inv.CurrentWeight / inv.MaxWeight) : 0f;

        int known = 0;
        foreach (var box in per.KnownCrates)
        {
            if (box == null || opened.Contains(box.GetEntityId())) continue;
            float dist = Vector2.Distance(box.transform.position, pos);
            if (IsCorpse(box)) { if (s.corpseDist < 0f || dist < s.corpseDist) s.corpseDist = dist; }
            else
            {
                known++;
                if (s.crateDist < 0f || dist < s.crateDist) s.crateDist = dist;
            }
        }
        s.knownCrates = known;

        // 판단에는 **보이는 적만** 넣는다(오라클 모드가 아니면).
        var foe = NearestEnemy(pos, 12f, visibleOnly: !per.Omniscient);
        if (foe != null) s.enemyDist = Vector2.Distance(foe.transform.position, pos);

        var exit = per.NearestKnown(InteractableObject.InteractType.ExitPoint, pos);
        if (exit != null) s.exitDist = Vector2.Distance(exit.transform.position, pos);

        // 아직 안 들어가 본 건물 입구(가장 가까운 것). 실외에서만 의미가 있다.
        var door = NearestUnvisitedDoor(pos, per.Omniscient);
        if (door != null) s.doorDist = Vector2.Distance(door.transform.position, pos);

        // 아직 안 열린(해결 안 된) 막힌 통로 — 아는 것 중 가장 가까운 것.
        var passage = NearestOpenPassage(per, pos, passageDone);
        if (passage != null) s.passageDist = Vector2.Distance(passage.transform.position, pos);

        return s;
    }

    /// <summary>아는 것 중 아직 안 열린 막힌 통로(가장 가까운 것). — <see cref="QaBrain.Goal.ClearPassage"/>가 쓴다.
    /// <paramref name="passageDone"/>에 든 것(도달불가·영구차단·개방완료)은 다시 고르지 않는다.</summary>
    static InteractableObject NearestOpenPassage(QaPerception per, Vector2 from, HashSet<int> passageDone)
    {
        InteractableObject best = null; float bestD = float.MaxValue;
        foreach (var io in per.KnownOthers)
        {
            if (io == null || io.Type != InteractableObject.InteractType.Passage) continue;
            if (passageDone != null && passageDone.Contains(io.GetEntityId())) continue;
            var bp = io.GetComponent<BlockedPassage>();
            if (bp == null || bp.IsOpen) continue;
            float d = Vector2.Distance(io.transform.position, from);
            if (d < bestD) { bestD = d; best = io; }
        }
        return best;
    }

    // 이미 들어갔다 나온 입구는 다시 안 들어간다(사람도 그렇다). 씬 단위로 기억.
    static readonly HashSet<int> _visitedDoors = new HashSet<int>();
    static string _doorScene = "";

    /// <summary>아직 안 들어가 본 건물 입구 중 가장 가까운 것.
    /// 2026-07-28 커밋 b2b3937로 루트가 실내로 옮겨져, 진입이 파밍의 전제가 됐다.</summary>
    static BuildingEntrance NearestUnvisitedDoor(Vector2 from, bool omniscient)
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene != _doorScene) { _doorScene = scene; _visitedDoors.Clear(); }

        BuildingEntrance best = null; float bestD = float.MaxValue;
        foreach (var be in Object.FindObjectsByType<BuildingEntrance>(FindObjectsSortMode.None))
        {
            if (be == null || be.IsExit || string.IsNullOrEmpty(be.TargetScene)) continue;   // 들어가는 문만
            if (!be.gameObject.activeInHierarchy) continue;
            if (_visitedDoors.Contains(be.GetEntityId())) continue;
            // 입구도 눈에 보여야 안다(오라클 모드 제외)
            if (!omniscient && !PlayerVision.CanSee(be.transform.position)) continue;
            float d = Vector2.Distance(be.transform.position, from);
            if (d < bestD) { bestD = d; best = be; }
        }
        return best;
    }

    /// <summary>AI가 스스로 판단하며 논다. 레이드 안에서 쓰는 것을 전제.</summary>
    static IEnumerator AiPlay(QaStepDef d, QaContext c)
    {
        float budget = d.budgetSec > 0 ? d.budgetSec : 180f;
        float t0 = Time.realtimeSinceStartup;
        float deadline = t0 + budget;

        var bot = c.Bot;
        var per = bot.Perception;
        var brain = bot.Brain;
        if (per == null || brain == null) { c.Report.Error("ai", "NO_BRAIN", "AI 미초기화"); yield break; }

        per.Omniscient = (d.param == "oracle");   // 오라클 모드 = 도달성 검증용(발견율 분모)
        c.Report.Info("ai", "START", per.Omniscient
            ? "오라클 모드 — 전지(도달성 검증용)"
            : "플레이어 모드 — 본 것만 알고 판단");

        string startScene = SceneManager.GetActiveScene().name;
        var opened = new HashSet<int>();
        var passageDone = new HashSet<int>();     // 도달불가·영구차단·개방완료 통로 — 재선택 안 함(NightOnly-낮 실패는 예외)
        int loots = 0, corpses = 0, fights = 0, explores = 0, enters = 0;
        var lastGoal = (QaBrain.Goal)(-1);
        int sameGoalRepeat = 0;
        int moveFailStreak = 0;   // Explore·EnterBuilding·LootCrate/Corpse 이동 실패 연속 횟수 → blockedRecently

        while (Time.realtimeSinceStartup < deadline)
        {
            var player = TopDownPlayer.Instance;
            if (player == null) break;
            if (!IsRaidScene(SceneManager.GetActiveScene().name))
            { c.Report.Info("ai", "RAID_OVER", $"레이드 종료(씬 {SceneManager.GetActiveScene().name}) — AI 정지"); break; }

            var st = BuildState(c, t0, budget, opened, passageDone, moveFailStreak);
            var goal = brain.Decide(st, out string why, out bool ambiguous);

            // 같은 목표만 계속 고르는데 진전이 없으면 교착이다.
            // 예전엔 무조건 Explore로 바꿨는데, **Explore 자체가 막힌 경우**엔 그게 해결이 아니다
            // (2026-07-28: 실내에서 벽 속 좌표를 목표로 잡고 계속 박았다).
            if (goal == lastGoal) sameGoalRepeat++; else { sameGoalRepeat = 0; lastGoal = goal; }
            if (sameGoalRepeat >= 5)
            {
                if (st.inInterior)
                {
                    c.Report.Warn("ai", "GOAL_LOOP", $"실내에서 '{goal}'만 {sameGoalRepeat}회 반복 — 건물 밖으로 나간다");
                    goal = QaBrain.Goal.LeaveBuilding;
                }
                else if (goal == QaBrain.Goal.Explore)
                {
                    c.Report.Warn("ai", "GOAL_LOOP", $"탐색이 {sameGoalRepeat}회 연속 헛돔 — 무작위 배회로 흔든다");
                    yield return c.Bot.Wander(c, 3f);
                    sameGoalRepeat = 0;
                    continue;
                }
                else
                {
                    c.Report.Warn("ai", "GOAL_LOOP", $"'{goal}'만 {sameGoalRepeat}회 반복 — 탐색으로 전환");
                    goal = QaBrain.Goal.Explore;
                }
                sameGoalRepeat = 0;
            }

            c.Report.Info("ai", goal.ToString(), why + (ambiguous ? "  ※애매" : ""));
            Vector2 pos = player.transform.position;

            switch (goal)
            {
                case QaBrain.Goal.Flee:
                {
                    // 예전엔 반대 방향으로 1.6초 이동하고 끝났다 — 그래서 매 판단마다 다시 Flee를
                    // 골라 제자리에서 반복하다 죽었다(실측: 점수 2.00 × 10회 반복 → 사망).
                    // 안전거리를 벌거나 시간이 다할 때까지 **계속** 도주하고, 방향은 매 스텝 갱신한다
                    // (적이 쫓아오므로 한 번 잡은 방향으로는 못 벌어진다).
                    const float safeDist = 18f;
                    const float maxFleeTime = 12f;
                    float fleeStart = Time.realtimeSinceStartup;
                    float hpAtStart = PlayerHpRatio();
                    bool escaped = false;

                    while (Time.realtimeSinceStartup - fleeStart < maxFleeTime)
                    {
                        player = TopDownPlayer.Instance;
                        if (player == null) break;
                        var hpc = player.GetComponent<Health>();
                        if (hpc != null && hpc.IsDead) break;

                        var foe = NearestEnemy(player.transform.position, 20f, visibleOnly: !per.Omniscient);
                        if (foe == null) { escaped = true; break; }   // 위협이 안 보임 = 이탈 성공

                        // 이름이 `d`면 AiPlay(QaStepDef d, …)의 인자를 가려 CS0136이 난다.
                        float foeDist = Vector2.Distance(foe.transform.position, player.transform.position);
                        if (foeDist >= safeDist) { escaped = true; break; }

                        Vector2 away = ((Vector2)player.transform.position - (Vector2)foe.transform.position).normalized;
                        GameInput.VSetMove(away);
                        yield return c.Bot.WaitSec(0.3f);
                    }

                    GameInput.VSetMove(Vector2.zero);
                    float hpEnd = PlayerHpRatio();
                    float fleeSec = Time.realtimeSinceStartup - fleeStart;

                    if (!escaped && hpEnd < hpAtStart - 0.001f)
                        // QA 한계가 아니라 게임 쪽 신호다 — 도망쳐도 못 벗어나면 추격속도/피격경직 문제일 수 있다.
                        c.Report.Error("flee", "CANNOT_ESCAPE",
                            $"{maxFleeTime:0}초 도망쳤는데도 체력이 계속 깎임 ({hpAtStart * 100f:0}%→{hpEnd * 100f:0}%)");
                    else if (escaped)
                        c.Report.Info("flee", "ESCAPED",
                            $"{fleeSec:0.#}초 만에 이탈 (체력 {hpAtStart * 100f:0}%→{hpEnd * 100f:0}%)");
                    else
                        c.Report.Warn("flee", "TIMEOUT",
                            $"{maxFleeTime:0}초 안에 안전거리({safeDist:0}m) 확보 실패 (체력 {hpAtStart * 100f:0}%→{hpEnd * 100f:0}%)");

                    brain.Outcome(goal, escaped, escaped ? "이탈 성공" : "이탈 실패");
                    break;
                }

                case QaBrain.Goal.EnterBuilding:
                {
                    var door = NearestUnvisitedDoor(pos, per.Omniscient);
                    if (door == null) { yield return c.Bot.WaitSec(0.2f); break; }
                    enters++;

                    int did = door.GetEntityId();
                    string from = SceneManager.GetActiveScene().name;
                    bool got = false;
                    // 입구는 트리거(문 1.3×1.0)라 **밟아야** 발동한다 — 도착 판정을 좁게.
                    yield return c.Bot.MoveTo(door.transform.position, 0.6f, 25f, r => got = r);
                    moveFailStreak = got ? 0 : moveFailStreak + 1;
                    yield return c.Bot.WaitSec(1.5f);          // 페이드 + additive 전환 대기

                    string now = SceneManager.GetActiveScene().name;
                    bool entered = now != from;
                    _visitedDoors.Add(did);                    // 성공이든 실패든 이 문은 소진
                    if (entered) c.Report.Info("building", "ENTER", $"건물 진입 {from} → {now}");
                    else c.Report.Warn("building", "ENTER_FAIL",
                        got ? "입구까지 갔는데 씬이 안 바뀜 — 트리거 미발동 의심" : "입구까지 도달 실패");
                    brain.Outcome(goal, entered, "건물 진입");
                    break;
                }

                case QaBrain.Goal.LeaveBuilding:
                {
                    bool wasInside = IsInterior();
                    yield return LeaveBuildingIfInside(c);
                    brain.Outcome(goal, wasInside && !IsInterior(), "건물 이탈");
                    break;
                }

                case QaBrain.Goal.ClearPassage:
                {
                    // "빠른 길이지만 시끄럽다" 대 "조용히 돌아간다"의 선택 — 버그가 아니라 콘텐츠다.
                    // 지금까지 봇은 길이 막히면 NO_PATH로 포기만 하고 이 선택지를 한 번도 안 밟았다.
                    var target = NearestOpenPassage(per, pos, passageDone);
                    if (target == null) { yield return c.Bot.WaitSec(0.2f); break; }

                    var bp = target.GetComponent<BlockedPassage>();
                    if (bp == null)   // InteractableObject는 있는데 BlockedPassage가 없다 — 배선 문제, 재선택 안 함
                    {
                        passageDone.Add(target.GetEntityId());
                        c.Report.Warn("passage", "NO_COMPONENT", $"'{target.name}' Passage 타입인데 BlockedPassage 없음");
                        brain.Outcome(goal, false, "컴포넌트 없음");
                        break;
                    }

                    int pid = target.GetEntityId();
                    float arrive = Mathf.Clamp(target.InteractRange * 0.6f, 0.8f, 1.6f);
                    bool reached = false;
                    yield return c.Bot.MoveTo(target.transform.position, arrive, 25f, r => reached = r);

                    if (!reached)
                    {
                        passageDone.Add(pid);   // 도달 못 하는 통로는 다시 안 고른다
                        c.Bot.NoteUnreachable();
                        c.Report.Warn("passage", "UNREACHABLE", $"'{target.name}' 통로까지 도달 실패");
                        brain.Outcome(goal, false, "도달 실패");
                        break;
                    }

                    if (bp.PassageMode == BlockedPassage.Mode.Permanent)
                    {
                        c.Bot.Tap(KeyCode.E);
                        yield return c.Bot.WaitSec(0.5f);
                        passageDone.Add(pid);   // 영구 차단 — 재선택 안 함
                        c.Report.Warn("passage", "BLOCKED_PERMANENT", $"'{target.name}' 영구 차단 — 돌아가야 함");
                        brain.Outcome(goal, false, "영구 차단");
                        break;
                    }

                    // 나머지 모드(Clearable/Locked/NightOnly/Code)는 E 홀드 채널이다 — 한 번 눌러 끝나지 않는다.
                    // 시작 여부부터 확인하고, 시작했으면 끝날 때까지 기다린다.
                    float clearT0 = Time.realtimeSinceStartup;
                    c.Bot.Tap(KeyCode.E);
                    yield return c.Bot.WaitSec(0.4f);
                    bool channelStarted = UseActionManager.Instance != null && UseActionManager.Instance.IsBusy;
                    if (channelStarted)
                        yield return c.Bot.WaitUntil(() => UseActionManager.Instance == null || !UseActionManager.Instance.IsBusy,
                                                      20f, null);
                    yield return c.Bot.WaitSec(0.3f);   // Open() 반영 대기

                    if (bp.IsOpen)
                    {
                        float elapsed = Time.realtimeSinceStartup - clearT0;
                        int nearFoes = CountEnemiesNear(target.transform.position, 20f);
                        passageDone.Add(pid);           // 개방 완료 — 재선택 안 함
                        c.Report.Info("passage", "CLEARED",
                            $"'{target.name}' 통로 개방 — {bp.PassageMode} · {elapsed:0.#}초 소요 · 20m 내 적 {nearFoes}기(소음 유인 확인)");
                        brain.Outcome(goal, true, "통로 개방");
                    }
                    else
                    {
                        // NightOnly가 낮이라 실패한 경우만 예외 — 밤엔 다시 시도할 수 있어야 한다.
                        bool nightBlockedByDay = bp.PassageMode == BlockedPassage.Mode.NightOnly;
                        if (!nightBlockedByDay) passageDone.Add(pid);
                        c.Report.Warn("passage", "CLEAR_FAIL",
                            $"'{target.name}' 개방 실패 — 모드 {bp.PassageMode}"
                            + (nightBlockedByDay ? " (낮 — 밤에 재시도 가능)" : ""));
                        brain.Outcome(goal, false, "개방 실패");
                    }
                    break;
                }

                case QaBrain.Goal.Extract:
                {
                    string before = SceneManager.GetActiveScene().name;
                    yield return RaidExtract(new QaStepDef { op = "raid.extract", budgetSec = 60f }, c);
                    brain.Outcome(goal, SceneManager.GetActiveScene().name != before, "레이드 이탈");
                    break;
                }

                case QaBrain.Goal.FightEnemy:
                {
                    fights++;
                    float hpBefore = PlayerHpRatio();
                    var foeBefore = NearestEnemy(pos, 12f, visibleOnly: !per.Omniscient);
                    yield return CombatEngage(new QaStepDef { op = "combat.engage", budgetSec = 35f, count = 1, ratio = 12f }, c);
                    bool killed = foeBefore == null
                                  || foeBefore.GetComponent<Health>() == null
                                  || foeBefore.GetComponent<Health>().IsDead;
                    brain.Outcome(goal, killed && PlayerHpRatio() > hpBefore * 0.5f, "교전");
                    break;
                }

                case QaBrain.Goal.LootCorpse:
                case QaBrain.Goal.LootCrate:
                {
                    bool wantCorpse = goal == QaBrain.Goal.LootCorpse;
                    LootContainer target = null; float bestD = float.MaxValue;
                    foreach (var box in per.KnownCrates)
                    {
                        if (box == null || opened.Contains(box.GetEntityId())) continue;
                        if (IsCorpse(box) != wantCorpse) continue;
                        float dist = Vector2.Distance(box.transform.position, pos);
                        if (dist < bestD) { bestD = dist; target = box; }
                    }
                    if (target == null) { yield return c.Bot.WaitSec(0.2f); break; }

                    int id = target.GetEntityId();
                    bool reached = false;
                    yield return c.Bot.MoveTo(target.transform.position, 1.5f, 20f, r => reached = r);
                    moveFailStreak = reached ? 0 : moveFailStreak + 1;
                    if (!reached)
                    {
                        opened.Add(id);   // 못 가는 건 다시 고르지 않는다(사람도 포기한다)
                        c.Report.Warn("ai", "GIVE_UP", $"{(wantCorpse ? "시체" : "상자")} 도달 실패 — 포기하고 다른 목표로");
                        c.Bot.NoteUnreachable();
                        brain.Outcome(goal, false, "도달 실패");
                        break;
                    }
                    if (target == null) break;

                    c.Bot.Tap(KeyCode.E);
                    yield return c.Bot.WaitSec(1.0f);
                    if (target == null) break;
                    int taken = c.Bot.TakeAllFrom(target);
                    yield return c.Bot.CloseUi("ai");
                    opened.Add(id);
                    if (wantCorpse) corpses++; else loots++;
                    c.Report.Info("ai", wantCorpse ? "CORPSE" : "CRATE",
                        $"{(wantCorpse ? "시체" : "상자")} 수색 — {taken}개 회수 (무게 {st.weight * 100f:0}%)");
                    // 열었는데 계속 빈손이면 '루팅'의 매력이 실제로 낮은 것 — 학습에 반영
                    brain.Outcome(goal, taken > 0, taken > 0 ? "회수 성공" : "빈 상자");
                    break;
                }

                default:   // Explore — 안 가본 쪽, 단 **갈 수 있는 지점**으로
                {
                    explores++;
                    // **갈 수 있는** 미탐색지를 격자 전파로 찾는다.
                    // 방향+거리 방식은 벽 반대편을 목표로 잡아 A*가 계속 실패했다.
                    Vector2 dest;
                    if (!per.TryReachableUnexplored(pos, out dest, st.inInterior ? 4f : 8f))
                        dest = per.UnexploredTarget(pos, st.inInterior ? 6f : 14f);   // 격자 없을 때 폴백

                    if ((dest - pos).sqrMagnitude < 1f)
                    {
                        // 격자가 "갈 데가 없다"고 답했다. 실내면 나가는 게 맞다.
                        if (st.inInterior)
                        {
                            c.Report.Info("ai", "EXPLORE_DONE", "실내에 더 갈 곳이 없음 — 건물 밖으로");
                            yield return LeaveBuildingIfInside(c);
                        }
                        else
                        {
                            c.Report.Warn("ai", "NO_WHERE_TO_GO", "걸을 수 있는 미탐색 지점을 못 찾음 — 격자/맵 확인");
                            yield return c.Bot.Wander(c, 2f);
                        }
                        break;
                    }

                    int knownBefore = per.KnownCrateCount;
                    bool moved = false;
                    // 이제 목적지가 멀다(가장 먼 미탐색지) — 이동 시간도 거리에 맞춰야 한다.
                    // 12초 고정이면 먼 구역은 영원히 못 간다.
                    float trek = Mathf.Clamp(Vector2.Distance(dest, pos) / 2.2f + 8f, 10f, 45f);
                    // 걷다가 적이 보이면 멈춘다 — 안 그러면 45초 내내 밀고 가며 적을 지나친다.
                    bool omni = per.Omniscient;
                    yield return c.Bot.MoveTo(dest, 2.5f, trek, r => moved = r,
                        abortIf: () =>
                        {
                            var pl = TopDownPlayer.Instance;
                            return pl != null && NearestEnemy(pl.transform.position, 10f, visibleOnly: !omni) != null;
                        });
                    moveFailStreak = moved ? 0 : moveFailStreak + 1;
                    if (!moved) c.Bot.NoteUnreachable();   // 그쪽이 막혔다는 신호(맵 구멍 후보)
                    // 탐색의 성패는 "갔는가"가 아니라 **새로 발견했는가**다
                    brain.Outcome(goal, per.KnownCrateCount > knownBefore || moved,
                                  per.KnownCrateCount > knownBefore ? "새 발견" : (moved ? "이동함" : "막힘"));
                    break;
                }
            }
        }

        GameInput.VSetMove(Vector2.zero);

        // 발견율 — 오라클(존재) 대비 플레이어(발견). 낮으면 맵이 안내를 못 하고 있다는 뜻.
        c.Report.Metric($"[{startScene}] 씬 상자 수", per.TotalCratesInScene);
        c.Report.Metric($"[{startScene}] 발견한 상자", per.KnownCrateCount);
        c.Report.Metric($"[{startScene}] 발견율(%)", per.CrateDiscoveryRate * 100f);
        c.Report.Info("ai", "OK",
            $"상자 {loots} · 시체 {corpses} · 교전 {fights} · 탐색 {explores} · 건물진입 {enters} · " +
            $"발견 {per.KnownCrateCount}/{per.TotalCratesInScene} ({per.CrateDiscoveryRate * 100f:0}%)");
        c.Report.Info("ai", "LEARNED", brain.TrustSummary());   // 런 중 무엇을 배웠나

        if (!per.Omniscient && per.TotalCratesInScene > 0 && per.KnownCrateCount == 0)
            c.Report.Error("ai", "DISCOVERY_ZERO",
                $"상자가 {per.TotalCratesInScene}개 있는데 **하나도 못 봤다** — 시야/배치/안내 문제");
    }

    // ── 전투 (2026-07-28) ────────────────────────────────────────────────
    //  정면 난타는 사람도 안 한다 — 스태미너·체력이 먼저 마른다.
    //  사거리 밖에서 맴돌다 적의 빈틈(회복·이동·경직)에 파고들어 치고 빠지는 **카이팅**.
    //  적이 윈드업(IsInWindup)이면 구르기로 흘린다. 처치 후 시체를 뒤진다(시체 = LootContainer).

    static float PlayerHpRatio()
    {
        var p = TopDownPlayer.Instance;
        if (p == null) return 0f;
        var h = p.GetComponent<Health>();
        return (h == null || h.MaxHp <= 0f) ? 1f : h.CurrentHp / h.MaxHp;
    }

    /// <summary>가장 가까운 살아있는 적. <paramref name="visibleOnly"/>면 **지금 보이는 적만**
    /// (게임의 시야 규칙 그대로). AI의 판단 입력은 반드시 보이는 것만 써야 한다 —
    /// 안 보이는 적을 알고 교전을 결정하면 그건 사람의 플레이가 아니다.</summary>
    static EnemyController NearestEnemy(Vector2 from, float maxDist, bool visibleOnly = false)
    {
        EnemyController best = null; float bestD = maxDist;
        foreach (var e in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            if (e == null || !e.gameObject.activeInHierarchy) continue;
            var h = e.GetComponent<Health>();
            if (h != null && h.IsDead) continue;
            if (visibleOnly && !PlayerVision.CanSee(e.transform.position)) continue;
            float dist = Vector2.Distance(e.transform.position, from);
            if (dist < bestD) { bestD = dist; best = e; }
        }
        return best;
    }

    /// <summary>발동 거부 사유 집계를 "사유×횟수, 사유×횟수" 문자열로.</summary>
    static string RejectSummary(Dictionary<string, int> rc)
    {
        if (rc == null || rc.Count == 0) return "없음";
        var parts = new List<string>();
        foreach (var kv in rc) parts.Add($"{kv.Key}×{kv.Value}");
        return string.Join(", ", parts);
    }

    /// <summary>가장 많이 나온 발동 거부 사유 하나.</summary>
    static string TopRejectReason(Dictionary<string, int> rc)
    {
        if (rc == null || rc.Count == 0) return "-";
        string top = "-"; int best = -1;
        foreach (var kv in rc) if (kv.Value > best) { best = kv.Value; top = kv.Key; }
        return best > 0 ? $"{top}×{best}" : "-";
    }

    /// <summary>반경 안의 살아있는 적 수 — 소음 유인 검증용(막힌 통로 철거·강제돌파의 대가를 수치로 남긴다).</summary>
    static int CountEnemiesNear(Vector2 pos, float radius)
    {
        int n = 0;
        foreach (var e in EnemyController.All)
        {
            if (e == null || !e.gameObject.activeInHierarchy || e.IsDead) continue;
            if (Vector2.Distance(e.transform.position, pos) <= radius) n++;
        }
        return n;
    }

    static void AimAt(Vector3 worldPos)
    {
        var cam = Camera.main;
        if (cam != null) GameInput.VSetMousePos(cam.WorldToScreenPoint(worldPos));
    }

    /// <summary>교전 — 카이팅으로 치고 빠지며, 처치하면 시체를 뒤진다.</summary>
    static IEnumerator CombatEngage(QaStepDef d, QaContext c)
    {
        float budget = d.budgetSec > 0 ? d.budgetSec : 45f;
        int wantKills = d.count > 0 ? d.count : 2;
        float deadline = Time.realtimeSinceStartup + budget;
        float searchR = d.ratio > 0f ? d.ratio : 14f;   // 교전 대상 탐색 반경(m)

        var ps = StatDB.Instance != null ? StatDB.Instance.playerStat : null;
        float range = ps != null && ps.lightRange > 0f ? ps.lightRange : 1.2f;
        float fleeHp = 0.35f;

        int kills = 0, swings = 0, dodges = 0;
        string scene = SceneManager.GetActiveScene().name;

        while (kills < wantKills && Time.realtimeSinceStartup < deadline)
        {
            var player = TopDownPlayer.Instance;
            if (player == null) yield break;

            var foe = NearestEnemy(player.transform.position, searchR);
            if (foe == null)
            {
                if (kills == 0) c.Report.Info("combat", "NO_ENEMY", $"반경 {searchR:0}m에 교전할 적 없음");
                break;
            }

            var foeHp = foe.GetComponent<Health>();
            // 계측: 적 체력 변화 — "안 맞는 것"과 "맞는데 못 죽이는 것"은 원인이 완전히 다르다.
            //   때린 횟수 많은데 체력 그대로 → 히트박스·사거리·상태 문제(AI 또는 게임 코드)
            //   체력은 깎이는데 못 죽임      → 데미지/HP 밸런스(게임 데이터, 내가 손댈 것 아님)
            float foeHpStart = foeHp != null ? foeHp.CurrentHp : -1f;
            float myHpStart = PlayerHpRatio();
            int swingsHere = 0;

            // ── CHAIN 계측 — 클릭→발동→적중 어디서 끊기는지 분리해서 잡는다.
            //   swings(클릭)만 세면 "발동은 되는데 안 맞는지" "애초에 발동이 안 되는지" 구분이 안 된다.
            int launchesHere = 0, hitsHere = 0;
            var rejectCounts = new Dictionary<string, int>();      // 발동 실패 사유별 집계
            float missDistSum = 0f, missAngleSum = 0f;             // 발동은 됐는데 못 맞힌 스윙의 거리·각도차 누적
            int missCount = 0;

            c.Report.Info("combat", "ENGAGE",
                $"교전 시작 — {foe.name} (거리 {Vector2.Distance(foe.transform.position, player.transform.position):0.#}m"
                + (foeHpStart >= 0f ? $", 적 HP {foeHpStart:0}" : "") + ")");

            // ── 한 마리와의 교전 루프 ──
            while (Time.realtimeSinceStartup < deadline)
            {
                player = TopDownPlayer.Instance;
                if (player == null || foe == null) break;
                if (foeHp != null && foeHp.IsDead) break;
                if (SceneManager.GetActiveScene().name != scene) break;   // 사망·전환

                float hp = PlayerHpRatio();
                if (hp <= fleeHp)
                {
                    c.Report.Warn("combat", "DISENGAGE", $"체력 {hp * 100f:0}% — 교전 이탈(도주)");
                    Vector2 away = ((Vector2)player.transform.position - (Vector2)foe.transform.position).normalized;
                    GameInput.VSetMove(away);
                    yield return c.Bot.WaitSec(1.5f);
                    GameInput.VSetMove(Vector2.zero);
                    yield break;
                }

                Vector2 toFoe = (Vector2)foe.transform.position - (Vector2)player.transform.position;
                float dist = toFoe.magnitude;
                Vector2 dir = dist > 0.01f ? toFoe / dist : Vector2.right;
                AimAt(foe.transform.position);

                if (foe.IsInWindup && dist < range + 1.2f)
                {
                    // 적이 때리려 한다 — 구르기로 흘리고 물러난다
                    c.Bot.Tap(KeyCode.Space);
                    dodges++;
                    GameInput.VSetMove(-dir);
                    yield return c.Bot.WaitSec(0.45f);
                }
                else if (dist > range + 0.4f)
                {
                    GameInput.VSetMove(dir);                    // 파고들기
                    yield return c.Bot.WaitSec(0.12f);
                }
                else if (dist < range - 0.5f)
                {
                    GameInput.VSetMove(-dir);                   // 너무 붙음 — 간격 회복
                    yield return c.Bot.WaitSec(0.12f);
                }
                else
                {
                    GameInput.VSetMove(Vector2.zero);

                    // 클릭 시점 스냅샷 — 발동 실패 시 사유를 배제법으로 가른다.
                    bool wasExhausted = player.IsExhausted;
                    var clickState = player.CurrentState;
                    float staminaBefore = player.StaminaCurrent;
                    float hpAtClick = foeHp != null ? foeHp.CurrentHp : -1f;
                    float distAtClick = dist;
                    float angleAtClick = Vector2.Angle(player.FacingDirection, dir);

                    GameInput.VClickMouse(0);                   // 약공격
                    swings++; swingsHere++;

                    // 발동 = 클릭 후 6프레임(~0.1초) 안에 Idle → 공격 상태로 전환됐는가.
                    bool launched = false;
                    float launchWait = 0f;
                    for (int fr = 0; fr < 6 && launchWait < 0.1f; fr++)
                    {
                        yield return null;
                        launchWait += Time.unscaledDeltaTime;
                        var nowP = TopDownPlayer.Instance;
                        var now = nowP != null ? nowP.CurrentState : TopDownPlayer.CombatState.Idle;
                        if (now == TopDownPlayer.CombatState.LightAttack
                            || now == TopDownPlayer.CombatState.HeavyCharge
                            || now == TopDownPlayer.CombatState.HeavyRelease)
                        { launched = true; break; }
                    }

                    float hitWait = 0f;
                    if (launched)
                    {
                        launchesHere++;

                        // 적중 = 발동 후 0.4초 동안 대상 체력이 실제로 하락했는가.
                        bool hitFound = false;
                        while (hitWait < 0.4f)
                        {
                            yield return null;
                            hitWait += Time.unscaledDeltaTime;
                            if (foeHp != null && hpAtClick >= 0f && foeHp.CurrentHp < hpAtClick - 0.01f)
                            { hitFound = true; break; }
                        }
                        if (hitFound) hitsHere++;
                        else { missDistSum += distAtClick; missAngleSum += angleAtClick; missCount++; }
                    }
                    else
                    {
                        // 발동 실패 사유 — 게임 코드를 못 읽는 부분(쿨다운 타이머는 private)은 배제법으로.
                        string reason;
                        if (wasExhausted) reason = "Exhausted";
                        else if (clickState != TopDownPlayer.CombatState.Idle) reason = clickState.ToString();
                        else
                        {
                            // StaminaGap: ConsumeStamina는 부족해도 _exhausted를 세우지 않으므로
                            // IsExhausted만으론 못 잡는다 — 클릭 시점 스태미너와 추정 비용을 비교한다.
                            float estCost = (StatDB.Instance != null && StatDB.Instance.playerStat != null)
                                ? StatDB.Instance.playerStat.lightStaminaCost : 6f;
                            reason = staminaBefore < estCost ? "StaminaGap" : "CooldownGap(추정)";
                        }
                        rejectCounts[reason] = rejectCounts.TryGetValue(reason, out int rc) ? rc + 1 : 1;
                    }

                    // 계측 대기(최대 0.5초)가 이미 흘렀으니 기존 치고 빠지기 리듬(0.5초)에서 남은 만큼만 채운다.
                    float already = launched ? (launchWait + hitWait) : launchWait;
                    float remain = Mathf.Max(0f, 0.5f - already);
                    GameInput.VSetMove(-dir);                   // 치고 빠지기
                    if (remain > 0f) yield return c.Bot.WaitSec(remain);
                }
            }

            GameInput.VSetMove(Vector2.zero);

            // ── 교전 1건 결산: 내가 준 피해 vs 받은 피해 ──────────────────
            float foeHpEnd = (foe != null && foeHp != null) ? foeHp.CurrentHp : 0f;
            float dealt = foeHpStart >= 0f ? Mathf.Max(0f, foeHpStart - foeHpEnd) : -1f;
            float taken = (myHpStart - PlayerHpRatio()) * 100f;
            c.Report.Info("combat", "TRADE",
                $"타격 {swingsHere}회 → 적 HP {foeHpStart:0}→{foeHpEnd:0} (준 피해 {dealt:0}) · "
                + $"내 체력 {myHpStart * 100f:0}%→{PlayerHpRatio() * 100f:0}% (받은 피해 {taken:0}%p)");

            if (swingsHere >= 4 && dealt <= 0.1f)
                c.Report.Error("combat", "NO_DAMAGE",
                    $"{swingsHere}회 때렸는데 적 체력이 **전혀 안 깎임** — 명중 안 됨(사거리·히트박스·공격 상태 확인). "
                    + "밸런스가 아니라 배선 문제다");
            else if (dealt > 0.1f && foeHpEnd > 0.1f && taken > dealt / Mathf.Max(1f, foeHpStart) * 100f)
                c.Report.Warn("combat", "LOSING_TRADE",
                    $"피해 교환이 불리하다 — 준 {dealt:0} / 받은 {taken:0}%p. 적이 세거나 카이팅이 안 먹힘");

            // ── CHAIN 결산: 클릭 → 발동 → 적중 어디서 끊기는지 ──────────────
            c.Report.Info("combat", "CHAIN",
                $"클릭 {swingsHere} → 발동 {launchesHere} (거부: {RejectSummary(rejectCounts)}) → "
                + $"적중 {hitsHere} → 데미지 {dealt:0}");

            if (swingsHere > 0 && launchesHere <= swingsHere / 2)
                c.Report.Error("combat", "ATTACK_BLOCKED",
                    $"클릭 {swingsHere}회 중 발동 {launchesHere}회뿐 — 절반 이상 거부됨. 최다 사유: {TopRejectReason(rejectCounts)}");
            else if (launchesHere > 0 && hitsHere == 0)
            {
                float avgDist = missCount > 0 ? missDistSum / missCount : -1f;
                float avgAngle = missCount > 0 ? missAngleSum / missCount : -1f;
                c.Report.Error("combat", "ATTACK_MISSES",
                    $"발동 {launchesHere}회가 전부 헛스윙 — 명중 0"
                    + (missCount > 0 ? $" · 평균 거리 {avgDist:0.##}m · 평균 각도차 {avgAngle:0.#}도" : "")
                    + $" (lightRange {range:0.##}m, 교전 밴드 {range - 0.5f:0.##}~{range + 0.4f:0.##}m)");
            }

            if (foe != null && foeHp != null && foeHp.IsDead)
            {
                kills++;
                c.Report.Info("combat", "KILL", $"처치 {kills}/{wantKills} — {foe.name}");
                yield return LootCorpse(foe, c);
            }
            else if (Time.realtimeSinceStartup >= deadline)
            {
                c.Report.Warn("combat", "TIMEOUT", $"제한 {budget:0}초 안에 처치 실패 (타격 {swings}회)");
                break;
            }
            else break;
        }

        c.Report.Info("combat", "OK", $"처치 {kills} · 타격 {swings} · 회피 {dodges} · 체력 {PlayerHpRatio() * 100f:0}%");
        if (swings > 0 && kills == 0)
            c.Report.Warn("combat", "NO_KILL", $"{swings}회 때렸는데 한 마리도 못 잡음 — 데미지/히트박스 확인");
    }

    /// <summary>시체 파밍 — 시체는 LootContainer + InteractableObject('시체 뒤지기')로 바뀐다.</summary>
    static IEnumerator LootCorpse(EnemyController foe, QaContext c)
    {
        if (foe == null) yield break;
        var box = foe.GetComponent<LootContainer>();
        if (box == null)
        {
            c.Report.Warn("combat", "NO_CORPSE_LOOT", $"{foe.name} 시체에 LootContainer가 없음 — 전리품 회수 불가");
            yield break;
        }

        bool reached = false;
        yield return c.Bot.MoveTo(foe.transform.position, 1.4f, 12f, r => reached = r);
        if (!reached)
        {
            c.Report.Warn("combat", "CORPSE_UNREACHABLE", "시체까지 도달 실패 — 전리품 유실");
            c.Bot.NoteUnreachable();
            yield break;
        }

        c.Bot.Tap(KeyCode.E);
        yield return c.Bot.WaitSec(1.0f);
        int taken = c.Bot.TakeAllFrom(box);
        yield return c.Bot.CloseUi("combat");

        c.Report.Info("combat", "CORPSE", $"시체 수색 — {taken}개 회수");
        if (taken == 0) c.Report.Warn("combat", "CORPSE_EMPTY", "시체가 비어 있음 — enemyDropChance/드랍 테이블 확인");
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

            // 이동 중 씬이 바뀌었다. 사망·탈출이면 중단, **건물 출입이면 목표만 다시 잡는다**.
            // (예전엔 무조건 break라, 상자로 가다 건물 입구를 밟으면 그 사이클이 통째로 끝났다)
            string nowScene = SceneManager.GetActiveScene().name;
            if (nowScene != raidScene)
            {
                if (!IsRaidScene(nowScene))
                { c.Report.Warn("explore", "SCENE_LEFT", $"레이드를 벗어남 → {nowScene} (사망/탈출?) — 탐색 중단"); break; }

                c.Report.Info("explore", "SCENE_CHANGED", $"{raidScene} → {nowScene} — 이 씬의 상자로 목표 재선정");
                crates = new List<LootContainer>(Object.FindObjectsByType<LootContainer>(FindObjectsSortMode.None));
                c.Report.Metric($"[{nowScene}] 상자 수", crates.Count);
                continue;
            }
            if (target == null) { c.Report.Warn("explore", "CRATE_GONE", "이동 중 상자가 사라짐"); continue; }
            if (!reached) { c.Report.Warn("explore", "UNREACHABLE", $"상자 '{target.ContainerName}' 도달 실패(길막힘?)"); c.Bot.NoteUnreachable(); continue; }

            c.Bot.Tap(KeyCode.E);
            yield return c.Bot.WaitSec(1.2f);
            if (target == null) { c.Report.Warn("explore", "CRATE_GONE", "수색 중 상자가 사라짐"); continue; }
            opened++;

            int taken = c.Bot.TakeAllFrom(target);
            c.Report.Info("explore", "CRATE", $"'{target.ContainerName}' 수색 — {taken}개 회수");
            yield return c.Bot.CloseUi("explore");

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

        // 건물 내부엔 탈출구가 없다 — 먼저 밖으로 나가야 한다(사람도 그렇게 한다).
        yield return LeaveBuildingIfInside(c);
        player = TopDownPlayer.Instance;
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
        // 도착 판정을 그 오브젝트의 실제 상호작용 반경에 맞춘다 —
        // 고정값(1.4m)을 쓰면 interactRange가 더 좁은 탈출구에서는 '도착했는데 범위 밖'이 된다.
        float arrive = Mathf.Clamp(best.InteractRange * 0.6f, 0.8f, 1.4f);
        // 이동 시간은 **거리에 맞춰야** 한다. 고정 배분(budget×0.6)이면 먼 탈출구는 무조건 실패한다
        // (2026-07-28: 직선 173.7m를 54초 안에 — 우회까지 하면 불가능 → EXIT_UNREACHABLE).
        float moveBudget = Mathf.Max(budget * 0.6f, bestD / 2.2f + 20f);
        yield return c.Bot.MoveTo(best.transform.position, arrive, moveBudget, r => reached = r);
        if (!reached)
        {
            yield return c.Bot.Blocked("extract", "EXIT_UNREACHABLE",
                $"탈출구 '{best.name}'까지 도달 실패 (직선거리 {bestD:0.#}m)", "MoveTo 반복 시도");
            yield break;
        }

        // ── 탈출 상호작용 ──────────────────────────────────────────────
        // E를 한 번 누르고 기다리면 안 된다. InteractionSystem이 입력을 **버리는 조건이 둘** 있다:
        //   ① UI가 열려 있으면 타겟 해제 + return  → 프롬프트조차 안 뜬다
        //   ② TopDownPlayer.CurrentState != Idle(공격·구르기 중)이면 E 무시
        // 봇은 교전 직후 탈출구로 오므로 ②에 걸려 입력이 통째로 사라진다(2026-07-28 EXTRACT_TIMEOUT).
        // 게다가 탈출은 즉시 전환이 아니라 exitWaitTime 동안 **버텨야** 하고,
        // interactRange×2 밖으로 나가면 취소된다 → 그 동안 움직이면 안 된다.
        yield return c.Bot.CloseUi("extract");                 // ① 해소
        GameInput.VSetMove(Vector2.zero);                      // 카운트다운 중 이탈 방지

        bool left = false;
        float deadline = Time.realtimeSinceStartup + budget * 0.4f;
        int taps = 0;
        bool sawPrompt = false;

        while (Time.realtimeSinceStartup < deadline)
        {
            if (SceneManager.GetActiveScene().name != raidScene) { left = true; break; }

            var pl = TopDownPlayer.Instance;
            bool idle = pl == null || pl.CurrentState == TopDownPlayer.CombatState.Idle;   // ② 해소: Idle일 때만
            bool uiOpen = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();
            if (uiOpen) yield return c.Bot.CloseUi("extract");

            if (idle && !uiOpen)
            {
                sawPrompt = true;
                c.Bot.Tap(KeyCode.E);
                taps++;
            }
            yield return c.Bot.WaitSec(0.5f);                  // 카운트다운을 버티며 재시도
        }

        if (!left)
        {
            string why = !sawPrompt
                ? "플레이어가 Idle 상태가 된 적이 없어 E가 한 번도 전달되지 않음(공격·구르기 상태 고착 의심)"
                : $"E {taps}회 전달했는데 씬 전환 없음 — 탈출구 배선(targetScene)·대기시간·취소거리 확인";
            yield return c.Bot.Blocked("extract", "EXTRACT_TIMEOUT", $"탈출 실패 — {why}", $"E {taps}회 · 정지 유지");
        }
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
