using UnityEngine;

public class WallOcclusionOutline : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] MeshRenderer outlineRenderer;
    [SerializeField] Color outlineColor = new Color(1f, 1f, 1f, 0.4f);

    Camera mainCam;
    Material outlineMat;

    void Start()
    {
        mainCam = Camera.main;
        if (outlineRenderer != null)
        {
            outlineMat = outlineRenderer.material;
            outlineRenderer.enabled = false;
        }
    }

    void LateUpdate()
    {
        if (mainCam == null || player == null || outlineRenderer == null) return;

        Vector3 camPos = mainCam.transform.position;
        Vector3 playerPos = player.position + Vector3.up * 0.5f;
        Vector3 dir = playerPos - camPos;
        float dist = dir.magnitude;

        bool occluded = false;
        var hits = Physics.RaycastAll(camPos, dir.normalized, dist);
        foreach (var hit in hits)
        {
            if (hit.transform.IsChildOf(player)) continue;
            if (hit.collider.gameObject.name == "ShadowCaster" ||
                hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
            {
                occluded = true;
                break;
            }
        }

        outlineRenderer.enabled = occluded;
    }
}
