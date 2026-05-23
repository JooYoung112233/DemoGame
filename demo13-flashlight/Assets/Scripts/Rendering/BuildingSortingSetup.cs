using UnityEngine;

/// <summary>
/// 건물 Quad(MeshRenderer)의 sorting 속성을 설정해서
/// SpriteRenderer와 같은 정렬 파이프라인에 넣음.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class BuildingSortingSetup : MonoBehaviour
{
    [SerializeField] string sortingLayerName = "Default";
    [SerializeField] int sortingPrecision = 100;
    [SerializeField] int sortingOffset = 0;

    MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sortingLayerName = sortingLayerName;
    }

    void LateUpdate()
    {
        meshRenderer.sortingOrder = -(int)(transform.position.y * sortingPrecision) + sortingOffset;
    }
}
