using UnityEngine;

/// <summary>
/// 플레이어가 들어오면 씬 전환을 일으키는 트리거 영역. (옛 BuildingEntryTrigger의 2D 대체 — SceneTransition 모드)
/// Prop2D Function=Trigger로 배치 시 자동 부착되며, 트리거 Collider2D가 함께 붙는다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MapTriggerZone2D : MonoBehaviour
{
    [Tooltip("전환할 씬 이름. 비우면 아무것도 안 함.")]
    public string targetScene;
    [Tooltip("도착 씬에서의 SpawnPoint ID.")]
    public string spawnPointId;
    [Tooltip("true면 들어오는 즉시 전환. false면(추후) 프롬프트/키 입력용.")]
    public bool autoEnter = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoEnter || string.IsNullOrEmpty(targetScene)) return;
        if (!other.CompareTag("Player")) return;
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(targetScene, spawnPointId);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.5f);
        var col = GetComponent<Collider2D>();
        if (col != null) Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.bounds.size);
    }
}
