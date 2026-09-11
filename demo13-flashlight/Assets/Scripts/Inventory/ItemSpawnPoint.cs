using UnityEngine;

/// <summary>
/// 루팅 앵커 — "여기에 루팅이 놓일 수 있다"를 표시한다.
/// Ground/Container는 MapSpawnController(예산)가 루팅 표(RegionLootCatalog)로 채운다 — 스스로 스폰하지 않는다.
/// Fixed만 자기 고정 아이템을 한 번 놓는다(열쇠 등).
/// 2026-09-11 정리(docs/region-loot.md §루팅 정리 결정): 자체 스폰(SpawnTable·지역 루트)·재스폰·linkedContainer 제거 —
/// 컨트롤러가 있으면 늘 무시됐고, 없으면 루팅이 0개가 되는 폴백만 남아 있었다.
/// </summary>
public class ItemSpawnPoint : MonoBehaviour
{
    public enum SpawnType
    {
        Ground,     // 바닥에 WorldItem 배치(컨트롤러가)
        Container,  // 같은 오브젝트(또는 부모·자식)의 LootContainer를 채움(컨트롤러가)
        Fixed,      // 항상 같은 고정 아이템(스스로)
    }

    [Header("스폰 설정")]
    [Tooltip("스폰 타입")]
    [SerializeField] SpawnType spawnType = SpawnType.Ground;

    /// <summary>외부에서 스폰 타입 조회</summary>
    public SpawnType Type => spawnType;

    [Tooltip("고정 아이템 (Fixed용)")]
    [SerializeField] ItemData fixedItem;

    [Tooltip("고정 아이템 수량")]
    [SerializeField] int fixedCount = 1;

    /// <summary>MapSpawnController가 수집했는지. Ground/Container인데 false면 채워 줄 컨트롤러가 없는 씬이다.</summary>
    [HideInInspector] public bool managedByController;

    bool _spawned;

    void Start()
    {
        if (spawnType != SpawnType.Fixed)
        {
            if (!managedByController)
                Debug.LogWarning($"[ItemSpawnPoint] MapSpawnController가 없는 씬 — '{name}'은 채워지지 않는다", this);
            return;
        }
        if (_spawned || fixedItem == null) return;
        _spawned = true;
        WorldItem.Drop(new ItemInstance(fixedItem, fixedCount), transform.position, this);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = spawnType switch
        {
            SpawnType.Ground => new Color(0, 1, 0, 0.5f),
            SpawnType.Container => new Color(1, 0.5f, 0, 0.5f),
            SpawnType.Fixed => new Color(0, 0.5f, 1, 0.5f),
            _ => Color.white,
        };
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawIcon(transform.position, "d_Prefab Icon", true);
    }
}
