using UnityEngine;

/// <summary>
/// 몸의 부위 — **어디를 맞았나**에만 쓴다.
/// (원래 `Medical/BodyPart.cs`에 있었으나 2026-09-09 부위별 의료 폐기로 여기로 이관)
/// </summary>
public enum BodyPartType
{
    Head,       // 머리 — 데미지 배율 최대
    Torso,      // 몸통 — 기준
    Arms,       // 양팔
    LeftLeg,    // 왼다리
    RightLeg,   // 오른다리
}

/// <summary>
/// 어디를 맞았나 — 피격 지점을 몸의 **부위**로 옮긴다. (docs/combat.md "부위 피격")
///
/// 2026-07-29 사용자 결정: **근접도 총과 똑같이 조준한 곳이 맞는다.**
///   탑다운이지만 캐릭터는 선 사람으로 그려지므로(근-오버헤드 투영) 스프라이트 안에서의
///   **위아래가 곧 몸의 높이**다. 위를 노리면 머리, 아래를 노리면 다리 — 조준점만으로 읽힌다.
///   조준점이 없는 쪽(적의 공격)은 가중 랜덤으로 떨어진다.
///
/// 부위는 **피격 판정과 데미지 배율**에만 쓴다 — 플레이어 부위별 치료(출혈/골절/통증)는
/// 2026-09-09 RPG화 결정으로 폐기됐고, 적 부위 부상(`UnitInjuries`)만 남았다.
/// </summary>
public static class BodyZones
{
    // 몸을 세로로 나눈 비율(0=발끝, 1=정수리). 실루엣이 사람으로 읽히는 최소한의 구분.
    const float HeadTop  = 1.00f, HeadBottom = 0.78f;
    const float LegTop   = 0.34f;
    const float ArmEdge  = 0.32f;   // 중심에서 이만큼 벗어나면 팔(몸통 높이대에서만)

    /// <summary>부위별 데미지 배율. 머리는 크게, 팔다리는 덜 아프다.
    /// (2026-07-29 사용자: "적도 같이 — 헤드샷 보너스")</summary>
    public static float DamageMult(BodyPartType part)
    {
        switch (part)
        {
            case BodyPartType.Head:  return 2.0f;
            case BodyPartType.Torso: return 1.0f;
            case BodyPartType.Arms:  return 0.8f;
            default:                 return 0.75f;   // 다리
        }
    }

    public static string Ko(BodyPartType part)
    {
        switch (part)
        {
            case BodyPartType.Head:     return "머리";
            case BodyPartType.Torso:    return "몸통";
            case BodyPartType.Arms:     return "팔";
            case BodyPartType.LeftLeg:  return "왼다리";
            default:                    return "오른다리";
        }
    }

    /// <summary>피격 지점 → 부위. bounds는 대상 몸통의 월드 경계(허트박스 콜라이더).
    /// 경계 밖이면 가장 가까운 쪽으로 눌러 읽는다(빗맞아도 부위는 정해져야 한다).
    ///
    /// ⚠️ 3D 전환(2026-09-08): 2D에선 화면 y가 곧 키였지만, 3D에서 **키는 월드 Y**이고
    ///    좌우는 XZ 평면 위의 한 축이다. 그래서 피격 지점을 평면 <c>Vector2</c>로 받으면
    ///    머리·다리를 가를 수 없다 — 월드 <c>Vector3</c>와 좌우 기준축을 함께 받는다.
    /// </summary>
    /// <param name="lateralAxis">좌우를 가를 수평 축(보통 대상의 오른쪽 또는 카메라 오른쪽).
    /// 0이면 월드 X를 쓴다.</param>
    public static BodyPartType FromPoint(Bounds bounds, Vector3 worldPoint, Vector3 lateralAxis)
    {
        float h = Mathf.Max(0.0001f, bounds.size.y);
        float y01 = Mathf.Clamp01((worldPoint.y - bounds.min.y) / h);

        // 좌우 — 기준축에 투영해 몸 폭으로 정규화한다.
        Vector3 axis = lateralAxis; axis.y = 0f;
        if (axis.sqrMagnitude < 0.000001f) axis = Vector3.right;
        axis.Normalize();
        // 축 방향 몸 반폭 — AABB를 단위축에 투영한 길이.
        float halfW = Mathf.Abs(bounds.extents.x * axis.x) + Mathf.Abs(bounds.extents.z * axis.z);
        if (halfW < 0.01f) halfW = Mathf.Max(bounds.extents.x, bounds.extents.z);


        float lateral = Vector3.Dot(worldPoint - bounds.center, axis);
        float x01 = Mathf.Clamp01(lateral / (2f * halfW) + 0.5f);

        if (y01 >= HeadBottom) return BodyPartType.Head;
        if (y01 < LegTop) return x01 < 0.5f ? BodyPartType.LeftLeg : BodyPartType.RightLeg;
        // 몸통 높이대 — 가장자리는 팔.
        return Mathf.Abs(x01 - 0.5f) > ArmEdge ? BodyPartType.Arms : BodyPartType.Torso;
    }

    /// <summary>조준점이 없을 때(적의 공격 등) — 가중 랜덤.
    /// 몸통이 가장 잘 맞고 머리는 드물다. 밸런스가 예측 가능해야 하므로 고정 표를 쓴다.</summary>
    public static BodyPartType Random()
    {
        float r = UnityEngine.Random.value;
        if (r < 0.45f) return BodyPartType.Torso;
        if (r < 0.65f) return BodyPartType.Arms;
        if (r < 0.80f) return BodyPartType.LeftLeg;
        if (r < 0.92f) return BodyPartType.RightLeg;
        return BodyPartType.Head;                      // 8%
    }

    /// <summary>부위의 월드 사각형(표시용). BodyZoneOverlay가 쓴다.</summary>
    public static Rect ZoneRect(Bounds b, BodyPartType part)
    {
        float x0 = b.min.x, y0 = b.min.y, w = b.size.x, h = b.size.y;
        switch (part)
        {
            case BodyPartType.Head:
                return new Rect(x0 + w * 0.28f, y0 + h * HeadBottom, w * 0.44f, h * (HeadTop - HeadBottom));
            case BodyPartType.Torso:
                return new Rect(x0 + w * (0.5f - ArmEdge), y0 + h * LegTop,
                                w * ArmEdge * 2f, h * (HeadBottom - LegTop));
            case BodyPartType.Arms:   // 좌우 두 쪽이지만 표시는 왼쪽 띠 하나로 대표(오버레이가 두 번 그린다)
                return new Rect(x0, y0 + h * LegTop, w * (0.5f - ArmEdge), h * (HeadBottom - LegTop));
            case BodyPartType.LeftLeg:
                return new Rect(x0, y0, w * 0.5f, h * LegTop);
            default:
                return new Rect(x0 + w * 0.5f, y0, w * 0.5f, h * LegTop);
        }
    }
}
