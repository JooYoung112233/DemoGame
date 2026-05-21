using UnityEngine;

/// <summary>
/// 적 스폰 영역. 씬에서 Gizmo로 영역 표시.
/// </summary>
public class SpawnZone : MonoBehaviour
{
    [SerializeField] Vector3 size = new Vector3(4, 0, 4);
    [SerializeField] int enemyCount = 2;
    [SerializeField] Color gizmoColor = new Color(1f, 0.3f, 0.3f, 0.3f);

    public Vector3 Size => size;
    public int EnemyCount => enemyCount;

    public void Setup(Vector3 zoneSize, int count)
    {
        size = zoneSize;
        enemyCount = count;
    }

    /// <summary>영역 내 랜덤 위치 반환</summary>
    public Vector3 GetRandomPoint()
    {
        Vector3 pos = transform.position;
        pos.x += Random.Range(-size.x * 0.5f, size.x * 0.5f);
        pos.z += Random.Range(-size.z * 0.5f, size.z * 0.5f);
        pos.y = 0;
        return pos;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(transform.position + Vector3.up * 0.1f,
            new Vector3(size.x, 0.2f, size.z));

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.8f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.1f,
            new Vector3(size.x, 0.2f, size.z));
    }
}
