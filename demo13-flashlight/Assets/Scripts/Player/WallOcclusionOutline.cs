using UnityEngine;

public class WallOcclusionOutline : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] MeshRenderer outlineRenderer;
    [SerializeField] Color outlineColor = new Color(0.3f, 0.8f, 1f, 0.4f);

    Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (outlineRenderer != null)
            outlineRenderer.enabled = false;
    }

    void LateUpdate()
    {
        if (mainCam == null || player == null || outlineRenderer == null) return;

        // 카메라→플레이어 방향으로 레이캐스트
        Vector3 camPos = mainCam.transform.position;
        Vector3 playerPos = player.position + Vector3.up * 0.5f;
        Vector3 dir = playerPos - camPos;
        float dist = dir.magnitude;

        bool occluded = false;

        // 카메라에서 플레이어까지 모든 충돌 체크
        var hits = Physics.RaycastAll(camPos, dir.normalized, dist - 0.3f);
        foreach (var hit in hits)
        {
            // 플레이어 자신 제외
            if (hit.transform == player) continue;
            if (hit.transform.IsChildOf(player)) continue;

            // 벽(ShadowCaster) 또는 지붕이 가리고 있으면 occluded
            occluded = true;
            break;
        }

        outlineRenderer.enabled = occluded;
    }
}
