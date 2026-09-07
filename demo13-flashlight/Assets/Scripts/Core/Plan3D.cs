using UnityEngine;

/// <summary>
/// 평면(XZ) ↔ 월드(XYZ) 변환 헬퍼.
///
/// 3D 전환 규약: 월드는 **XZ 평면**이고 위쪽이 +Y다. 그런데 이 게임의 게임플레이 로직
/// (조준 방향·이동 방향·거리 판정·시야 콘 등)은 전부 **평면 위의 2D 문제**다.
/// 그래서 평면 수학은 계속 <see cref="Vector2"/>로 다루고 — x=월드X, y=월드Z —
/// 트랜스폼/물리 경계에서만 이 헬퍼로 변환한다.
///
/// 이렇게 하면 `FacingDirection` 같은 공개 API가 그대로 유지되어
/// 소비자(전투·시야·AI·UI)를 건드리지 않고 좌표계를 바꿀 수 있다.
/// 설계 근거: docs/3d-migration.md
/// </summary>
public static class Plan3D
{
    /// <summary>평면 좌표 → 월드. y(높이)는 0.</summary>
    public static Vector3 ToWorld(Vector2 plan) => new Vector3(plan.x, 0f, plan.y);

    /// <summary>평면 좌표 → 월드, 높이 지정.</summary>
    public static Vector3 ToWorld(Vector2 plan, float height) => new Vector3(plan.x, height, plan.y);

    /// <summary>월드 → 평면 좌표(높이 버림).</summary>
    public static Vector2 ToPlan(Vector3 world) => new Vector2(world.x, world.z);

    /// <summary>두 월드 지점의 평면 거리(높이 무시). 단차가 생겨도 거리 판정이 흔들리지 않는다.</summary>
    public static float PlanDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x, dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>평면 방향을 바라보는 회전(Y축 yaw). 방향이 0이면 <paramref name="fallback"/>.</summary>
    public static Quaternion LookRotation(Vector2 planDir, Quaternion fallback)
    {
        if (planDir.sqrMagnitude < 0.000001f) return fallback;
        return Quaternion.LookRotation(ToWorld(planDir).normalized, Vector3.up);
    }

    /// <summary>화면 좌표에서 쏜 광선이 <paramref name="groundY"/> 높이의 수평면과 만나는 지점.
    /// 오소 쿼터뷰에서 마우스 조준은 이 방식이어야 한다 — ScreenToWorldPoint는 3D에서 의미가 없다.</summary>
    public static bool ScreenToGround(Camera cam, Vector3 screenPos, float groundY, out Vector3 world)
    {
        world = default;
        if (cam == null) return false;
        var ray = cam.ScreenPointToRay(screenPos);
        var plane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
        if (!plane.Raycast(ray, out float enter)) return false;
        world = ray.GetPoint(enter);
        return true;
    }
}
