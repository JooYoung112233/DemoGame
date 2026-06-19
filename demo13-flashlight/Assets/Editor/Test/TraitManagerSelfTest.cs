#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// TraitManager 자가검증 — 로드/PP/해금·환급/부정상한/모디파이어 합성/세이브 라운드트립 단언.
/// 메뉴 클릭 → Console PASS/FAIL. 진실원: Assets/Resources/Data/Traits/*.asset (traits.md §3).
/// 데이터에서 키·값을 읽어 단언하므로 1차 수치가 바뀌어도 깨지지 않음.
/// </summary>
public static class TraitManagerSelfTest
{
    static int pass, fail;

    [MenuItem("Tools/TopDown/테스트/특성 시스템 자가검증")]
    public static void Run()
    {
        pass = 0; fail = 0;
        var go = new GameObject("__trait_selftest");
        try
        {
            var m = go.AddComponent<TraitManager>();   // Awake → Resources 로드
            Eq("SO 41개 로드", 41, m.AllTraits.Count);
            True("초기 PP 0", m.AvailablePP == 0);

            // ── 양수 퍽: 선행 없는 T1 ──
            var pos = m.AllTraits.FirstOrDefault(
                t => t.tier == TraitTier.T1 && string.IsNullOrEmpty(t.prereqTraitId) && t.ppCost > 0);
            True("T1 무선행 퍽 존재", pos != null);
            if (pos != null)
            {
                True("PP 0일 때 CanUnlock false", !m.CanUnlock(pos.traitId, out _));
                m.GrantPP(10);
                Eq("GrantPP(10)", 10, m.AvailablePP);
                True("CanUnlock true", m.CanUnlock(pos.traitId, out _));
                True("Unlock 성공", m.Unlock(pos.traitId));
                Eq("PP 차감", 10 - pos.ppCost, m.AvailablePP);
                True("IsUnlocked true", m.IsUnlocked(pos.traitId));

                var mul = pos.effects.FirstOrDefault(e => e.op == TraitManager.OP_MUL);
                if (mul != null)
                    True($"GetModifier({mul.effectKey})≈{1f + mul.value}",
                         Mathf.Abs(m.GetModifier(mul.effectKey) - (1f + mul.value)) < 0.001f);
            }

            // ── flag 퍽 → HasFlag ──
            var flagT = m.AllTraits.FirstOrDefault(
                t => t.ppCost > 0 && string.IsNullOrEmpty(t.prereqTraitId)
                     && t.effects.Any(e => e.op == TraitManager.OP_FLAG));
            if (flagT != null)
            {
                m.GrantPP(10);
                m.Unlock(flagT.traitId);
                var fk = flagT.effects.First(e => e.op == TraitManager.OP_FLAG).effectKey;
                True($"HasFlag({fk}) true", m.HasFlag(fk));
            }

            // ── 선행조건 게이트 ──
            var child = m.AllTraits.FirstOrDefault(
                t => !string.IsNullOrEmpty(t.prereqTraitId) && !m.IsUnlocked(t.prereqTraitId));
            if (child != null)
            {
                m.GrantPP(20);
                True("선행 미충족 시 CanUnlock false", !m.CanUnlock(child.traitId, out _));
                if (m.Get(child.prereqTraitId) != null) m.Unlock(child.prereqTraitId);
                True("선행 충족 후 CanUnlock true", m.CanUnlock(child.traitId, out _));
            }

            // ── 부정 특성: 환급 + 상한(3) ──
            var negs = m.AllTraits
                .Where(t => t.tier == TraitTier.Negative && !m.IsUnlocked(t.traitId))
                .Take(4).ToList();
            if (negs.Count >= 1)
            {
                int before = m.AvailablePP;
                int refund = -negs[0].ppCost;   // ppCost 음수 → 환급
                True("부정 CanUnlock true", m.CanUnlock(negs[0].traitId, out _));
                m.Unlock(negs[0].traitId);
                Eq("부정 환급(+PP)", before + refund, m.AvailablePP);
                Eq("NegativeTraitCount 1", 1, m.NegativeTraitCount);
            }
            if (negs.Count >= 4)
            {
                m.Unlock(negs[1].traitId);
                m.Unlock(negs[2].traitId);
                Eq("부정 3개", 3, m.NegativeTraitCount);
                True("4번째 부정 차단(상한)", !m.CanUnlock(negs[3].traitId, out _));
            }

            // ── 세이브 라운드트립 ──
            var save = m.GetSaveData();
            True("세이브에 unlocked 포함", save.unlocked.Count > 0);
            Object.DestroyImmediate(go);          // 첫 인스턴스 제거(Instance 해제)
            var go2 = new GameObject("__trait_selftest2");
            try
            {
                var m2 = go2.AddComponent<TraitManager>();
                m2.LoadSaveData(save);
                Eq("로드 후 PP 복원", save.availablePP, m2.AvailablePP);
                True("로드 후 unlocked 복원", save.unlocked.All(id => m2.IsUnlocked(id)));
            }
            finally { Object.DestroyImmediate(go2); }
        }
        finally
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        string s = $"[TraitManagerSelfTest] {pass} PASS / {fail} FAIL";
        if (fail == 0) Debug.Log("✅ " + s); else Debug.LogError("❌ " + s);
    }

    static void Eq<T>(string label, T expected, T actual)
    {
        if (Equals(expected, actual)) pass++;
        else { fail++; Debug.LogError($"  FAIL [{label}] expected={expected} actual={actual}"); }
    }

    static void True(string label, bool cond)
    {
        if (cond) pass++;
        else { fail++; Debug.LogError($"  FAIL [{label}]"); }
    }
}
#endif
