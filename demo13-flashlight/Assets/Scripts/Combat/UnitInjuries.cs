using UnityEngine;

/// <summary>
/// 적의 **부위 부상** — 한 부위에 피해가 쌓이면 그 부위가 망가진다. (docs/combat.md "부위 피격")
///
/// (2026-07-29 사용자: "적 부위 부상 디버프도 넣어줘")
/// 여태 부위는 **데미지 배율**뿐이라, 다리를 노려도 "조금 덜 아프다" 말고는 아무 일도 안 났다.
/// 그래서 머리만 노리는 게 항상 정답이었다. 부위마다 **다른 이득**이 있어야 조준에 선택이 생긴다.
///   · 다리 → 못 쫓아온다 (도망칠 수 있다)
///   · 팔   → 느리게 때리고 덜 아프다 (맞고 버틸 수 있다)
///   · 머리 → 잘 못 찾고 쉽게 무너진다 (기습이 이어진다)
///   · 몸통 → 회복이 느려 그로기가 누적된다 (몰아칠 수 있다)
///
/// 플레이어의 `PlayerMedicalSystem`과 **같은 부위 enum**을 쓰되 구조는 따로다 —
/// 적은 치료·붕대 개념이 없고 한 판 안에서만 유효하므로, 부상 상태를 단순 누적으로 둔다.
/// </summary>
public class UnitInjuries : MonoBehaviour
{
    /// <summary>한 부위가 "부상"이 되는 누적 피해 = 최대 HP × 이 비율.</summary>
    const float InjureRatio = 0.22f;
    /// <summary>"중상"(효과 2배) 비율.</summary>
    const float SevereRatio = 0.45f;

    readonly float[] _dmg = new float[5];
    float _injureAt = 9f, _severeAt = 18f;

    /// <summary>최대 HP를 알려 주면 임계치를 그에 맞춘다(HP 110짜리 탱커와 40짜리 밴딧이 같으면 안 된다).</summary>
    public void Setup(float maxHp)
    {
        float hp = Mathf.Max(1f, maxHp);
        _injureAt = hp * InjureRatio;
        _severeAt = hp * SevereRatio;
    }

    public void Add(BodyPartType part, float damage)
    {
        int i = (int)part;
        if (i < 0 || i >= _dmg.Length || damage <= 0f) return;
        _dmg[i] += damage;
    }

    /// <summary>0=멀쩡 1=부상 2=중상.</summary>
    public int Level(BodyPartType part)
    {
        int i = (int)part;
        if (i < 0 || i >= _dmg.Length) return 0;
        if (_dmg[i] >= _severeAt) return 2;
        return _dmg[i] >= _injureAt ? 1 : 0;
    }

    public bool Injured(BodyPartType part) => Level(part) > 0;

    int LegLevels => Level(BodyPartType.LeftLeg) + Level(BodyPartType.RightLeg);

    // ── 디버프 배율 (EnemyController의 스탯 프로퍼티에 곱해진다) ──────────
    //   한 다리 부상이면 절뚝, 양다리 중상이면 거의 못 움직인다.
    public float MoveMult        => Mathf.Max(0.28f, 1f - 0.18f * LegLevels);
    /// <summary>공격 예비동작 배율(>1 = 느려짐). 팔이 망가지면 크게 휘두르지 못한다.</summary>
    public float WindupMult      => 1f + 0.22f * Level(BodyPartType.Arms);
    /// <summary>공격력 배율. 팔 부상.</summary>
    public float AttackMult      => Mathf.Max(0.45f, 1f - 0.2f * Level(BodyPartType.Arms));
    /// <summary>탐지 반경 배율. 머리를 맞으면 잘 못 찾는다 — 기습이 이어진다.</summary>
    public float DetectMult      => Mathf.Max(0.4f, 1f - 0.25f * Level(BodyPartType.Head));
    /// <summary>그로기 회복 배율(&lt;1 = 느리게 빠짐). 몸통이 망가지면 숨을 못 고른다.</summary>
    public float GroggyDecayMult => Mathf.Max(0.35f, 1f - 0.3f * Level(BodyPartType.Torso));
    /// <summary>받는 그로기 배율(&gt;1 = 쉽게 무너짐). 머리 부상.</summary>
    public float GroggyTakenMult => 1f + 0.25f * Level(BodyPartType.Head);

    /// <summary>이름표에 붙일 짧은 표시(F1 오버레이를 안 켜도 보이게). 멀쩡하면 빈 문자열.</summary>
    public string Badge()
    {
        string s = "";
        if (Injured(BodyPartType.Head))  s += "머";
        if (Injured(BodyPartType.Arms))  s += "팔";
        if (LegLevels > 0)               s += "다";
        if (Injured(BodyPartType.Torso)) s += "몸";
        return s.Length == 0 ? "" : $" [{s}]";
    }
}
