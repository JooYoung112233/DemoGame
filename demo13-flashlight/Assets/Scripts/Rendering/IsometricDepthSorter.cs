using UnityEngine;

/// <summary>
/// Y좌표 기반으로 sortingOrder를 동적 갱신.
/// SpriteRenderer, MeshRenderer 모두 지원.
/// </summary>
public class IsometricDepthSorter : MonoBehaviour
{
    [SerializeField] int sortingPrecision = 100;
    [SerializeField] int sortingOffset = 0;

    Renderer cachedRenderer;

    void Awake()
    {
        cachedRenderer = GetComponentInChildren<Renderer>();
    }

    void LateUpdate()
    {
        if (cachedRenderer == null) return;
        cachedRenderer.sortingOrder = -(int)(transform.position.y * sortingPrecision) + sortingOffset;
    }
}
