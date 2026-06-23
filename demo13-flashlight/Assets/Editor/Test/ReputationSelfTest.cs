#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// I01 ReputationManager 자가검증 — 순수 등급 로직(임계값·자릿세 할인·다음등급·행동값) 단언.
/// PlayMode 불필요(ReputationTiers/ReputationActions는 MonoBehaviour 비의존). 메뉴 클릭 → Console에 PASS/FAIL 집계.
/// 진실원 대조: tools/balance/reputation_tiers.csv · reputation.csv.
/// </summary>
public static class ReputationSelfTest
{
    static int pass, fail;

    // 메뉴 폐지 — 밸런스·컨트롤 패널 ▸ 도구·검증 탭에서 호출.
    public static void Run()
    {
        pass = 0; fail = 0;

        // ── 등급 경계 (reputation_tiers.csv: F0-9 / E10-29 / D30-59 / C60-99 / B100-149 / A150+) ──
        Eq("F@0", ReputationTier.F, ReputationTiers.GetTier(0));
        Eq("F@9", ReputationTier.F, ReputationTiers.GetTier(9));
        Eq("E@10", ReputationTier.E, ReputationTiers.GetTier(10));
        Eq("E@29", ReputationTier.E, ReputationTiers.GetTier(29));
        Eq("D@30", ReputationTier.D, ReputationTiers.GetTier(30));
        Eq("D@59", ReputationTier.D, ReputationTiers.GetTier(59));
        Eq("C@60", ReputationTier.C, ReputationTiers.GetTier(60));
        Eq("C@99", ReputationTier.C, ReputationTiers.GetTier(99));
        Eq("B@100", ReputationTier.B, ReputationTiers.GetTier(100));
        Eq("B@149", ReputationTier.B, ReputationTiers.GetTier(149));
        Eq("A@150", ReputationTier.A, ReputationTiers.GetTier(150));
        Eq("A@9999", ReputationTier.A, ReputationTiers.GetTier(9999));

        // ── 자릿세 할인 (F0 E0 D20 C40 B60 A80) ──
        Eq("rent@5(F)", 0, ReputationTiers.RentDiscountPercent(5));
        Eq("rent@15(E)", 0, ReputationTiers.RentDiscountPercent(15));
        Eq("rent@45(D)", 20, ReputationTiers.RentDiscountPercent(45));
        Eq("rent@80(C)", 40, ReputationTiers.RentDiscountPercent(80));
        Eq("rent@120(B)", 60, ReputationTiers.RentDiscountPercent(120));
        Eq("rent@200(A)", 80, ReputationTiers.RentDiscountPercent(200));

        // ── 다음 등급까지 ──
        Eq("toNext@0", 10, ReputationTiers.ToNextTier(0));    // → E(10)
        Eq("toNext@5", 5, ReputationTiers.ToNextTier(5));     // → E(10)
        Eq("toNext@100(B→A)", 50, ReputationTiers.ToNextTier(100)); // → A(150)
        Eq("toNext@150(A)", 0, ReputationTiers.ToNextTier(150));    // 최고 등급

        // ── 행동값 (reputation.csv) ──
        Eq("act MQ-001_report", 10, ReputationActions.Value("MQ-001_report"));
        Eq("act MQ-002_done", 15, ReputationActions.Value("MQ-002_done"));
        Eq("act BQ-A_done", 5, ReputationActions.Value("BQ-A_done"));
        Eq("act BQ-E_done", 1, ReputationActions.Value("BQ-E_done"));
        Eq("act BD_general(=0)", 0, ReputationActions.Value("BD_general"));
        Eq("act unknown(=0)", 0, ReputationActions.Value("nonexistent_key"));

        // ── 매니저 래퍼 라운드트립 (등급 비교차 → 토스트 미발생, 안전) ──
        var go = new GameObject("__rep_selftest");
        try
        {
            var mgr = go.AddComponent<ReputationManager>();
            int evRep = -1, evDelta = 0;
            mgr.OnReputationChanged += (r, d) => { evRep = r; evDelta = d; };
            mgr.Add(5, "selftest");                       // 0→5, F 유지
            Eq("mgr.Reputation after +5", 5, mgr.Reputation);
            Eq("mgr.OnReputationChanged rep", 5, evRep);
            Eq("mgr.OnReputationChanged delta", 5, evDelta);
            Eq("mgr.Tier@5", ReputationTier.F, mgr.Tier);
            Eq("mgr.Add(-99) clamps≥0", 0, ClampedAdd(mgr, -99));
            mgr.LoadSaveData(72);
            Eq("mgr.LoadSaveData(72)", 72, mgr.GetSaveData());
            Eq("mgr.Tier@72(C)", ReputationTier.C, mgr.Tier);
            mgr.AddByAction("BD_general", "noop");        // 0값 → 변동 없음
            Eq("mgr.AddByAction(BD_general) noop", 72, mgr.Reputation);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }

        string summary = $"[ReputationSelfTest] {pass} PASS / {fail} FAIL";
        if (fail == 0) Debug.Log("✅ " + summary);
        else Debug.LogError("❌ " + summary);
    }

    static int ClampedAdd(ReputationManager mgr, int delta) { mgr.Add(delta, "clamp"); return mgr.Reputation; }

    static void Eq<T>(string label, T expected, T actual)
    {
        if (Equals(expected, actual)) { pass++; }
        else { fail++; Debug.LogError($"  FAIL [{label}] expected={expected} actual={actual}"); }
    }
}
#endif
