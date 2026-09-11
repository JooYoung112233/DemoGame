using UnityEngine;

/// <summary>
/// 씬 전환 시 플레이어 스폰 위치 마커.
/// 씬에 빈 오브젝트로 배치, pointId로 식별.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("이 스폰포인트의 고유 ID. InteractableObject의 spawnPointId와 일치해야 전환 시 여기로 이동")]
    [SerializeField] string pointId = "default";

    public string PointId => pointId;

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0.5f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0.5f, 1f);
        Gizmos.DrawSphere(transform.position, 0.15f);

#if UNITY_EDITOR
        var style = new GUIStyle();
        style.normal.textColor = Color.green;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"Spawn: {pointId}", style);
#endif
    }
}
