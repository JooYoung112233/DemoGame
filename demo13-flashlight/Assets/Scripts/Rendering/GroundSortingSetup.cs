using UnityEngine;

/// <summary>
/// 바닥 Quad(MeshRenderer)를 Ground 소팅 레이어에 고정.
/// 항상 건물/캐릭터 뒤에 렌더링됨.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class GroundSortingSetup : MonoBehaviour
{
    [SerializeField] string sortingLayerName = "Ground";
    [SerializeField] int sortingOrder = 0;

    void Awake()
    {
        var mr = GetComponent<MeshRenderer>();
        mr.sortingLayerName = sortingLayerName;
        mr.sortingOrder = sortingOrder;
    }
}
