using UnityEngine;

/// <summary>
/// 비주얼(SpriteRenderer) 없는 기능 마커를 씬에서 보이게/선택 가능하게 하는 기즈모.
/// Prop2D에서 'noVisual'(투명 마커)로 배치된 오브젝트에 부착된다(SpawnPoint는 자체 기즈모 있음).
/// </summary>
public class Prop2DMarker : MonoBehaviour
{
    public Color color = new Color(0.3f, 0.9f, 1f, 0.9f);
    [Tooltip("기즈모 옆 라벨(기능 이름 등).")]
    public string label;

    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(label))
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.4f, label);
#endif
    }
}
