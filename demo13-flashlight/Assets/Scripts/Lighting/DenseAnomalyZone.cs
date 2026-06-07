#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// 짙은 현상 구간 — 트리거 영역에 플레이어가 들어가면 그만큼 현상(fog+어둠)이 켜진다.
/// 전체 맵이 아니라 "구간구간"으로 현상을 지정할 때 사용(골목/건물/특정 구역).
/// 트리거 Collider2D 필요(Box/Polygon). 겹치는 구간이 여러 개면 가장 큰 intensity 적용.
/// DenseAnomalyController가 매 프레임 들어간 구간들의 max를 목표 강도로 반영(부드럽게 lerp).
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class DenseAnomalyZone : MonoBehaviour
{
    [Tooltip("이 구간 안에서의 현상 강도(0~1). 들어가면 화면이 이만큼 안개·어둠.")]
    [Range(0, 1)] public float intensity = 1f;

    int _inside;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _inside++;
        if (_inside == 1) DenseAnomalyController.Instance?.AddZone(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _inside = Mathf.Max(0, _inside - 1);
        if (_inside == 0) DenseAnomalyController.Instance?.RemoveZone(this);
    }

    void OnDisable()
    {
        _inside = 0;
        if (DenseAnomalyController.Instance != null) DenseAnomalyController.Instance.RemoveZone(this);
    }

    void OnDrawGizmos()
    {
        var c = GetComponent<Collider2D>();
        if (c == null) return;
        var b = c.bounds;
        Gizmos.color = new Color(0.55f, 0.35f, 0.85f, 0.16f);
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(0.65f, 0.45f, 0.95f, 0.9f);
        Gizmos.DrawWireCube(b.center, b.size);
    }

#if UNITY_EDITOR
    [MenuItem("Tools/TopDown/Map/Create Anomaly Zone")]
    static void CreateZone()
    {
        var go = new GameObject("AnomalyZone");
        Undo.RegisterCreatedObjectUndo(go, "Create Anomaly Zone");
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(6f, 6f);
        go.AddComponent<DenseAnomalyZone>();
        var sv = SceneView.lastActiveSceneView;
        if (sv != null) { var p = sv.pivot; p.z = 0f; go.transform.position = p; }
        Selection.activeGameObject = go;
    }
#endif
}
