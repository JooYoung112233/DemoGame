using UnityEngine;

/// <summary>
/// 카메라 오클루전 — 플레이어를 가리는 건물에 화면 공간 구멍을 뚫는다.
///
/// 이 컴포넌트는 **전역 셰이더 유니폼만 갱신**한다. 실제로 뚫는 일은 `Spike/OccluderFX`
/// 셰이더가 한다. 그래서 건물이 몇 채든 재질별 배선이 필요 없다 — 그 셰이더를 쓰는
/// 모든 오브젝트가 이 값 하나로 같이 동작한다.
///
/// 판정은 셰이더에서 두 조건의 교집합이다:
///   ① 화면상 플레이어 위치에서 반경 안 (월드 거리로 하면 건물 표면이 멀어 안 잘린다)
///   ② 프래그먼트가 플레이어보다 카메라에 가까움 (뒤에 있는 건물까지 뚫리면 안 된다)
///
/// 설계 근거·비교 스크린샷: docs/3d-migration.md (Stage 0)
/// </summary>
[DefaultExecutionOrder(100)]   // 카메라 추적이 끝난 뒤 값을 읽는다
public class CutawayDriver : MonoBehaviour
{
    static readonly int CutCenterId  = Shader.PropertyToID("_CutCenter");
    static readonly int CutRadiusId  = Shader.PropertyToID("_CutRadius");
    static readonly int CutSoftId    = Shader.PropertyToID("_CutSoft");
    static readonly int CutEnabledId = Shader.PropertyToID("_CutEnabled");

    [Tooltip("구멍 반경 — 화면 높이 대비 비율. 픽셀이 아니라서 해상도가 바뀌어도 안 틀어진다.")]
    [SerializeField, Range(0.02f, 0.4f)] float radius = 0.13f;
    [Tooltip("구멍 가장자리를 디더로 풀어주는 폭. 화면 높이 대비 비율.")]
    [SerializeField, Range(0f, 0.2f)] float softEdge = 0.05f;
    [Tooltip("플레이어 발밑에서 이 높이를 구멍 중심으로 삼는다(가슴 높이).")]
    [SerializeField] float pivotHeight = 1.0f;
    [SerializeField] bool enableCutaway = true;
    [Tooltip("가림 판정에서 제외할 레이어(플레이어 자신 등).")]
    [SerializeField] LayerMask blockerMask = ~0;

    Camera _cam;
    Transform _target;
    Renderer[] _renderers;
    readonly RaycastHit[] _hits = new RaycastHit[8];

    void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        if (_target == null)
        {
            var p = TopDownPlayer.Instance;
            if (p != null) _target = p.transform;
        }

        if (!enableCutaway || _cam == null || _target == null)
        {
            Shader.SetGlobalFloat(CutEnabledId, 0f);
            return;
        }

        Vector3 pivot = ResolvePivot();

        // ⚠️ **실제로 가려질 때만** 구멍을 낸다.
        //    이 판정이 없으면 건물 **옆**에 서 있어도 지붕 귀퉁이가 화면상 원 안에 들어오고
        //    카메라 쪽이라는 이유로 잘려나간다 — 가리지도 않는데 지붕이 파인다.
        if (!IsOccluded(pivot))
        {
            Shader.SetGlobalFloat(CutEnabledId, 0f);
            return;
        }

        Vector3 vp = _cam.WorldToViewportPoint(pivot);

        // 플레이어가 화면 밖이거나 카메라 뒤면 뚫지 않는다.
        if (vp.z <= 0f || vp.x < -0.2f || vp.x > 1.2f || vp.y < -0.2f || vp.y > 1.2f)
        {
            Shader.SetGlobalFloat(CutEnabledId, 0f);
            return;
        }

        // ⚠️ **월드 좌표를 넘긴다.** 화면 좌표를 여기서 계산하면 렌더 타겟에 따라 Y가
        //    뒤집혀(게임 뷰 vs RenderTexture) 구멍이 어긋난다. 셰이더가 프래그먼트와
        //    같은 행렬로 투영하게 두면 그 문제가 원천적으로 사라진다.
        Shader.SetGlobalVector(CutCenterId, new Vector4(pivot.x, pivot.y, pivot.z, 0f));
        Shader.SetGlobalFloat(CutRadiusId, radius);
        Shader.SetGlobalFloat(CutSoftId, softEdge);
        Shader.SetGlobalFloat(CutEnabledId, 1f);
    }

    /// <summary>카메라와 플레이어 사이를 실제로 막는 것이 있는가.
    /// 트리거는 무시한다(진입 볼륨·상호작용 영역이 가림으로 잡히면 안 된다).</summary>
    bool IsOccluded(Vector3 pivot)
    {
        Vector3 from = _cam.transform.position;
        Vector3 dir  = pivot - from;
        float dist = dir.magnitude;
        if (dist < 0.01f) return false;
        int n = Physics.RaycastNonAlloc(from, dir / dist, _hits, dist - 0.35f,
                                        blockerMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var c = _hits[i].collider;
            if (c == null) continue;
            if (_target != null && c.transform.IsChildOf(_target)) continue;   // 나 자신
            return true;
        }
        return false;
    }

    /// <summary>구멍 중심 — 캐릭터 렌더러 바운즈의 중앙. 없으면 발밑 + pivotHeight.</summary>
    Vector3 ResolvePivot()
    {
        if (_renderers == null || _renderers.Length == 0)
            _renderers = _target.GetComponentsInChildren<Renderer>();

        bool has = false; Bounds b = default;
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null || !r.enabled) continue;
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        return has ? b.center : _target.position + Vector3.up * pivotHeight;
    }

    void OnDisable() => Shader.SetGlobalFloat(CutEnabledId, 0f);
}
