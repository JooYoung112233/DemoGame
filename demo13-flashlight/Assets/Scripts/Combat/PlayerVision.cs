using UnityEngine;

/// <summary>
/// 좀보이드식 FOV 시야콘 — 플레이어 정면 부채꼴 + 근접 360° 밖의 적은 **렌더러만 숨김**(AI·충돌은 유지).
/// 매 프레임 `EnemyController.All`을 순회해 가시성 판정 후 `SetVisionVisible` 호출.
///
/// 판정: 사거리 안 && (근접 반경 안 || 콘 각도 안) && (LOS 켜졌으면 벽에 안 막힘).
/// 튜닝: GameTuning.vision*(각도/사거리/근접/LOS/enable). 플레이어 없으면 전부 보임.
/// 부팅 시 자가 생성(DontDestroyOnLoad). 안전구역엔 적이 없어 자연히 무영향.
/// </summary>
public class PlayerVision : MonoBehaviour
{
    public static PlayerVision Instance { get; private set; }

    int _occluderMask;   // LOS 레이캐스트 대상(=Player/Enemy/IgnoreRaycast 제외)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;
        if (Instance != null) return;
        var go = new GameObject("[PlayerVision]");
        DontDestroyOnLoad(go);
        go.AddComponent<PlayerVision>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        int player = LayerMask.NameToLayer("Player");
        int enemy = LayerMask.NameToLayer("Enemy");
        int mask = ~0;                       // 전체
        if (player >= 0) mask &= ~(1 << player);
        if (enemy >= 0) mask &= ~(1 << enemy);
        mask &= ~(1 << 2);                    // IgnoreRaycast
        _occluderMask = mask;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // 이동/마우스 방향 갱신 뒤에 판정 → LateUpdate.
    void LateUpdate()
    {
        var list = EnemyController.All;
        if (list.Count == 0) return;

        var p = TopDownPlayer.Instance;
        var gt = GameTuning.Instance;
        bool on = (gt == null || gt.visionEnabled) && p != null;

        // 시야 비활성/플레이어 없음 → 전부 보이게
        if (!on)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null) list[i].SetVisionVisible(true);
            return;
        }

        float fovDeg   = gt != null ? gt.visionFovDegrees : 150f;
        float range    = gt != null ? gt.visionRange : 9f;
        float near     = gt != null ? gt.visionNearRadius : 2.2f;
        bool los       = gt == null || gt.visionLineOfSight;

        Vector2 eye = Plan3D.ToPlan(p.transform.position);
        Vector2 facing = p.FacingDirection.sqrMagnitude > 0.0001f ? p.FacingDirection.normalized : Vector2.down;
        float cosHalf = Mathf.Cos(fovDeg * 0.5f * Mathf.Deg2Rad);
        float range2 = range * range;
        float near2 = near * near;

        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e == null) continue;

            if (e == null) continue;
            e.SetVisionVisible(Test(eye, facing, cosHalf, range2, near2, los, Plan3D.ToPlan(e.transform.position)));
        }
    }

    /// <summary>한 지점이 시야에 들어오는지 — 적 판정과 **같은 계산**을 한 곳에 모은 것.</summary>
    bool Test(Vector2 eye, Vector2 facing, float cosHalf, float range2, float near2, bool los, Vector2 target)
    {
        Vector2 to = target - eye;
        float d2 = to.sqrMagnitude;

        bool visible;
        if (d2 > range2) visible = false;                 // 사거리 밖
        else if (d2 <= near2) visible = true;             // 근접 360°
        else
        {
            // 콘 각도: dot(facing, dir) >= cos(half)
            float dot = Vector2.Dot(facing, to.normalized);
            visible = dot >= cosHalf;
        }

        // LOS: 벽(솔리드)에 막히면 안 보임
        if (visible && los && d2 > near2)
            visible = !BlockedByWall(eye, target);

        return visible;
    }

    /// <summary>이 월드 좌표가 지금 플레이어 눈에 보이는가 — **적 판정과 완전히 같은 규칙**.
    ///
    /// QA 지각(QaPerception)이 "봇이 무엇을 발견했는가"를 정할 때 쓴다. 규칙을 따로 구현하면
    /// QA가 게임과 다른 것을 검증하게 되므로, 반드시 이 하나만 쓴다.
    /// 시야 시스템이 꺼져 있으면(visionEnabled=false) 사거리 안이면 전부 보이는 것으로 본다.</summary>
    /// ⚠️ 인자는 **월드 좌표**다. 예전 Vector2 시그니처를 그대로 두면 호출부의 Vector3가
    ///    암묵 변환되어 (x, 높이)로 읽힌다 — 컴파일은 되고 판정만 조용히 틀린다.
    public static bool CanSee(Vector3 world)
    {
        var inst = Instance;
        var p = TopDownPlayer.Instance;
        if (inst == null || p == null) return true;      // 판정 불가 — 막지 않는다

        var gt = GameTuning.Instance;
        float range = gt != null ? gt.visionRange : 9f;
        Vector2 eye = Plan3D.ToPlan(p.transform.position);

        if (gt != null && !gt.visionEnabled)
            return (Plan3D.ToPlan(world) - eye).sqrMagnitude <= range * range;

        float fovDeg = gt != null ? gt.visionFovDegrees : 150f;
        float near   = gt != null ? gt.visionNearRadius : 2.2f;
        bool los     = gt == null || gt.visionLineOfSight;
        Vector2 facing = p.FacingDirection.sqrMagnitude > 0.0001f ? p.FacingDirection.normalized : Vector2.down;

        return inst.Test(eye, facing, Mathf.Cos(fovDeg * 0.5f * Mathf.Deg2Rad),
                         range * range, near * near, los, Plan3D.ToPlan(world));
    }

    /// <summary>시야 차단 검사를 쏘는 높이 — 눈높이. 지면에서 쏘면 턱마다 막힌다.</summary>
    const float EyeY = 1.5f;

    bool BlockedByWall(Vector2 a, Vector2 b)
    {
        float y = TopDownPlayer.Instance != null ? TopDownPlayer.Instance.transform.position.y : 0f;
        return Physics.Linecast(Plan3D.ToWorld(a, y + EyeY), Plan3D.ToWorld(b, y + EyeY),
                                _occluderMask, QueryTriggerInteraction.Ignore);
    }
}
