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

    Camera _cam;
    Transform _target;

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

        Vector3 pivot = _target.position + Vector3.up * pivotHeight;
        Vector3 vp = _cam.WorldToViewportPoint(pivot);

        // 플레이어가 화면 밖이거나 카메라 뒤면 뚫지 않는다.
        if (vp.z <= 0f || vp.x < -0.2f || vp.x > 1.2f || vp.y < -0.2f || vp.y > 1.2f)
        {
            Shader.SetGlobalFloat(CutEnabledId, 0f);
            return;
        }

        // 셰이더의 SV_POSITION은 위에서 아래로 세므로 y를 뒤집는다.
        float depth = -_cam.worldToCameraMatrix.MultiplyPoint(pivot).z;
        Shader.SetGlobalVector(CutCenterId, new Vector4(vp.x, 1f - vp.y, depth, 0f));
        Shader.SetGlobalFloat(CutRadiusId, radius);
        Shader.SetGlobalFloat(CutSoftId, softEdge);
        Shader.SetGlobalFloat(CutEnabledId, 1f);
    }

    void OnDisable() => Shader.SetGlobalFloat(CutEnabledId, 0f);
}
