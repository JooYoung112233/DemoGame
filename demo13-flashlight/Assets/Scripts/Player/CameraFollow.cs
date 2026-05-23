using UnityEngine;

/// <summary>
/// 카메라 추적. 에디터에서 설정한 위치/회전/orthoSize를 그대로 유지.
/// Play 시 플레이어와의 오프셋만 계산해서 따라감.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 8f;

    Vector3 offset;
    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        // target 없으면 Player 태그로 자동 탐색
        if (target == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
                target = playerGO.transform;
        }

        // 에디터에서 설정한 현재 위치 기준으로 오프셋 계산
        if (target != null)
            offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
