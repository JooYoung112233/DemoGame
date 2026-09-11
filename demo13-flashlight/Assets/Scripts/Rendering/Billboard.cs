using UnityEngine;

/// <summary>
/// 월드에 떠 있는 표시물(HP바·이름표·말풍선)을 <b>카메라 쪽으로 세운다.</b>
///
/// 2D에선 필요 없었다 — 카메라가 정면이라 스프라이트가 그냥 화면과 나란했다. 3D 쿼터뷰는
/// 카메라가 55° 내려다보므로, 눕혀 둔 판은 **납작하게 찌그러져** 글자도 게이지도 안 읽힌다.
///
/// 카메라의 회전을 그대로 복사한다(빌보드 중에서도 가장 단순한 형태). 오소 카메라라
/// 위치 기준으로 바라보게 하는 것과 결과가 같고, 캐릭터마다 각도가 조금씩 달라지지 않아
/// 여러 개가 나란히 있을 때 더 안정적이다.
///
/// ⚠️ <b>부모의 스케일을 타지 않게</b> 자기 스케일을 매번 보정하지는 않는다 — 붙이는 쪽에서
///    균등 스케일 노드에 달아라. 여기서 스케일까지 건드리면 게이지 채움(localScale.x로
///    표현한다)을 덮어써 HP바가 항상 가득 찬 것처럼 보인다.
///
/// 설계: docs/3d-migration.md Stage 4
/// </summary>
[DisallowMultipleComponent]
public class Billboard : MonoBehaviour
{
    [Tooltip("비우면 Camera.main. 씬 전환으로 카메라가 바뀌면 자동으로 다시 잡는다.")]
    [SerializeField] Camera cam;

    /// <summary>이 오브젝트(와 자식)를 카메라 쪽으로 세운다. 이미 붙어 있으면 그대로 둔다.</summary>
    public static Billboard Attach(Transform t)
    {
        if (t == null) return null;
        return t.GetComponent<Billboard>() ?? t.gameObject.AddComponent<Billboard>();
    }

    void LateUpdate()
    {
        // LateUpdate여야 한다. 카메라가 같은 프레임에 움직이므로(CameraFollow),
        // Update에서 맞추면 한 프레임 뒤처져 빠르게 움직일 때 표시물이 흔들린다.
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        transform.rotation = cam.transform.rotation;
    }
}
