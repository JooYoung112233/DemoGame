using UnityEngine;

/// <summary>
/// 탑다운 2D 손전등 조준 — Light2D(스팟 콘)가 붙은 오브젝트를 마우스 방향으로 회전.
/// Light2D는 따로 부착(인스펙터). 이 스크립트는 transform 회전만 담당.
/// </summary>
public class TopDownFlashlight2D : MonoBehaviour
{
    [Tooltip("콘 기본 방향 보정 (스팟이 로컬 +X로 쏘면 0, +Y면 -90)")]
    [SerializeField] float angleOffset = 0f;

    Camera _cam;

    void Awake() => _cam = Camera.main;

    void Update()
    {
        if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }

        Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (Vector2)(mouseWorld - transform.position);
        if (dir.sqrMagnitude < 0.0001f) return;

        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang + angleOffset);
    }
}
