using UnityEngine;

/// <summary>
/// 적 스폰 영역. 씬에서 Gizmo로 영역 표시.
/// </summary>
public class SpawnZone : MonoBehaviour
{
    [SerializeField] Vector3 size = new Vector3(4, 0, 4);
    [SerializeField] int enemyCount = 2;
    [SerializeField] EnemyData enemyType;
    [SerializeField] Color gizmoColor = new Color(1f, 0.3f, 0.3f, 0.3f);

    public Vector3 Size => size;
    public int EnemyCount => enemyCount;
    public EnemyData EnemyType => enemyType;

    public void Setup(Vector3 zoneSize, int count, EnemyData type = null)
    {
        size = zoneSize;
        enemyCount = count;
        enemyType = type;
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
        Color baseColor = enemyType != null ?
            new Color(enemyType.tintColor.r, enemyType.tintColor.g, enemyType.tintColor.b, 0.3f) :
            gizmoColor;

        Gizmos.color = baseColor;
        Gizmos.DrawCube(transform.position + Vector3.up * 0.1f,
            new Vector3(size.x, 0.2f, size.z));

        Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.8f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.1f,
            new Vector3(size.x, 0.2f, size.z));
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (enemyType != null)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                $"{enemyType.displayName} x{enemyCount}");
        }
    }
#endif
}
