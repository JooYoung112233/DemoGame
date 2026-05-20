using UnityEngine;

public class RoofController : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] GameObject roofObject;
    [SerializeField] Vector3 buildingMin = new Vector3(0, 0, 0);
    [SerializeField] Vector3 buildingMax = new Vector3(12, 0, 10);
    [SerializeField] float fadeSpeed = 8f;

    MeshRenderer[] roofRenderers;
    float currentAlpha = 1f;
    bool playerInside;

    void Start()
    {
        if (roofObject != null)
            roofRenderers = roofObject.GetComponentsInChildren<MeshRenderer>();
    }

    void Update()
    {
        if (player == null || roofRenderers == null) return;

        Vector3 p = player.position;
        playerInside = p.x > buildingMin.x && p.x < buildingMax.x
                    && p.z > buildingMin.z && p.z < buildingMax.z;

        float target = playerInside ? 0f : 1f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, target, fadeSpeed * Time.deltaTime);

        foreach (var r in roofRenderers)
        {
            var c = r.material.color;
            c.a = currentAlpha;
            r.material.color = c;
            r.enabled = currentAlpha > 0.01f;
        }
    }
}
