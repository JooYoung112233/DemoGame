using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// QA 시나리오 정의 — **사용자가 짜는 플레이 시퀀스**. (docs/qa.md)
///
/// 로드 우선순위 (앞이 이기고, 재컴파일 없이 교체 가능):
///   1) `persistentDataPath/qa-scenario.json`   ← 현장에서 고쳐 쓰는 자리
///   2) `Resources/QA/qa-scenario.json`         ← 리포지토리 기본본
///   3) 코드 내장 기본 시나리오(Default)         ← 둘 다 없을 때
///
/// 한 사이클 = 정비(인벤·상점·퀘스트) → 모험(레이드) → 귀환(정산). 이걸 cycles회 반복하며
/// 사이클별 지표를 쌓아 **밸런스 추세**를 본다.
/// </summary>
[System.Serializable]
public class QaStepDef
{
    /// <summary>동작 이름. QaSteps에 등록된 op (예: raid.enter, shop.buy).</summary>
    public string op;
    /// <summary>이 스텝에 허용할 시간(초). 0이면 스텝 기본값.</summary>
    public float budgetSec;
    /// <summary>개수형 파라미터(상자 수·퀘스트 수·구매 개수 등).</summary>
    public int count;
    /// <summary>자유 문자열(지역 id·카테고리·상점 id 등). "auto"면 자동 선택.</summary>
    public string param;
    /// <summary>비율형 파라미터(소지금 사용률 등, 0~1).</summary>
    public float ratio;
    /// <summary>배회(무작위 이동) 섞기 — 자유도.</summary>
    public bool wander;
}

[System.Serializable]
public class QaScenarioDef
{
    public string name = "표준 순환";
    /// <summary>난수 시드 — 같은 시드면 같은 플레이(재현 가능). 0이면 매번 랜덤.</summary>
    public int seed = 20260711;
    /// <summary>반복 사이클 수. 밸런스 추세는 사이클이 여럿이어야 보인다.</summary>
    public int cycles = 3;
    /// <summary>사이클 하나의 스텝 시퀀스.</summary>
    public QaStepDef[] steps;

    public static QaScenarioDef Load()
    {
        // 1) 현장 오버라이드
        try
        {
            string p = System.IO.Path.Combine(Application.persistentDataPath, "qa-scenario.json");
            if (System.IO.File.Exists(p))
            {
                var s = JsonUtility.FromJson<QaScenarioDef>(System.IO.File.ReadAllText(p));
                if (s != null && s.steps != null && s.steps.Length > 0)
                {
                    Debug.Log($"[QA] 시나리오 로드(외부): {p}");
                    return s;
                }
            }
        }
        catch (System.Exception e) { Debug.LogWarning($"[QA] 외부 시나리오 로드 실패: {e.Message}"); }

        // 2) 리포지토리 기본본
        var ta = Resources.Load<TextAsset>("QA/qa-scenario");
        if (ta != null)
        {
            var s = JsonUtility.FromJson<QaScenarioDef>(ta.text);
            if (s != null && s.steps != null && s.steps.Length > 0)
            {
                Debug.Log("[QA] 시나리오 로드(Resources/QA/qa-scenario)");
                return s;
            }
        }

        Debug.Log("[QA] 시나리오 로드(내장 기본)");
        return Default();
    }

    /// <summary>내장 기본 — 정비 → 모험 → 귀환 순환.</summary>
    public static QaScenarioDef Default()
    {
        return new QaScenarioDef
        {
            name = "표준 순환(내장)",
            seed = 20260711,
            cycles = 3,
            steps = new[]
            {
                new QaStepDef { op = "cycle.begin" },
                new QaStepDef { op = "safehouse.ensure",  budgetSec = 25 },
                new QaStepDef { op = "inventory.organize", budgetSec = 15 },
                new QaStepDef { op = "shop.sell",          ratio = 0.7f },            // 잡템 70% 처분
                new QaStepDef { op = "shop.buy",           param = "Medical", ratio = 0.4f, count = 2 },
                new QaStepDef { op = "quest.accept",       count = 2 },
                new QaStepDef { op = "raid.enter",         param = "auto", budgetSec = 25 },
                new QaStepDef { op = "raid.explore",       budgetSec = 150, count = 6, wander = true },
                new QaStepDef { op = "raid.extract",       budgetSec = 90 },
                new QaStepDef { op = "settle.verify",      budgetSec = 15 },
                new QaStepDef { op = "cycle.end" },
            },
        };
    }

    /// <summary>현재 시나리오를 JSON으로 내보낸다(사용자가 고쳐 쓸 템플릿 생성용).</summary>
    public string ToJson() => JsonUtility.ToJson(this, true);
}
