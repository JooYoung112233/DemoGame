using UnityEngine;

/// <summary>
/// 적 타겟팅 시 아웃라인 표시.
/// 사거리 안: 빨간 링 + 스프라이트 밝게
/// 사거리 밖: 노란 링
/// </summary>
public class EnemyOutline : MonoBehaviour
{
    [SerializeField] Color inRangeRingColor = new Color(1f, 0.2f, 0.2f, 0.9f);
    [SerializeField] Color outOfRangeRingColor = new Color(1f, 1f, 0.3f, 0.5f);
    [SerializeField] float ringRadius = 0.6f;
    [SerializeField] int ringSegments = 32;

    SpriteRenderer[] spriteRenderers;
    Color[] originalColors;
    LineRenderer ringLine;

    void Start()
    {
        // 스프라이트 렌더러 캐시
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;

        CreateRing();
        SetHighlight(false);
    }

    void CreateRing()
    {
        var ringGO = new GameObject("TargetRing");
        ringGO.transform.SetParent(transform);
        ringGO.transform.localPosition = new Vector3(0, 0.05f, 0);

        ringLine = ringGO.AddComponent<LineRenderer>();
        ringLine.useWorldSpace = false;
        ringLine.loop = true;
        ringLine.positionCount = ringSegments;
        ringLine.startWidth = 0.06f;
        ringLine.endWidth = 0.06f;
        ringLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ringLine.receiveShadows = false;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", inRangeRingColor);
        ringLine.sharedMaterial = mat;

        for (int i = 0; i < ringSegments; i++)
        {
            float angle = i * 360f / ringSegments * Mathf.Deg2Rad;
            ringLine.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * ringRadius, 0, Mathf.Sin(angle) * ringRadius));
        }

        ringGO.SetActive(false);
    }

    public void SetHighlight(bool highlight, bool inRange = false)
    {
        // 링 표시
        if (ringLine != null)
        {
            ringLine.gameObject.SetActive(highlight);
            if (highlight)
            {
                Color c = inRange ? inRangeRingColor : outOfRangeRingColor;
                ringLine.startColor = c;
                ringLine.endColor = c;
                if (ringLine.sharedMaterial != null)
                    ringLine.sharedMaterial.SetColor("_BaseColor", c);
            }
        }

        // 스프라이트 색상 변경
        if (spriteRenderers == null) return;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            if (spriteRenderers[i].gameObject.name == "shadow") continue;

            if (highlight && inRange)
            {
                // 사거리 내: 밝게
                Color orig = originalColors[i];
                spriteRenderers[i].color = new Color(
                    Mathf.Min(orig.r * 1.6f, 1f),
                    Mathf.Min(orig.g * 1.6f, 1f),
                    Mathf.Min(orig.b * 1.6f, 1f),
                    orig.a);
            }
            else
            {
                spriteRenderers[i].color = originalColors[i];
            }
        }
    }
}
