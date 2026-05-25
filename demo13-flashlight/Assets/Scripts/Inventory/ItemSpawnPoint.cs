using UnityEngine;

/// <summary>
/// 아이템 스폰 포인트.
/// 씬에 배치. 게임 시작/진입 시 SpawnTable에 따라 아이템 생성.
/// </summary>
public class ItemSpawnPoint : MonoBehaviour
{
    public enum SpawnType
    {
        Ground,     // 바닥에 WorldItem 배치
        Container,  // 루팅 상자 내부 격자에 채우기
        Fixed,      // 항상 같은 고정 아이템
    }

    [Header("스폰 설정")]
    [Tooltip("스폰 타입")]
    [SerializeField] SpawnType spawnType = SpawnType.Ground;

    [Tooltip("스폰 테이블 (Ground/Container용). 비우고 지역 루트 사용 가능")]
    [SerializeField] SpawnTable spawnTable;

    [Tooltip("true면 RegionLootCatalog에서 현재 지역 테이블 사용")]
    [SerializeField] bool useRegionLoot = true;

    [Tooltip("비우면 RegionTimeManager 활성 지역. 씬 고정 테스트용")]
    [SerializeField] string regionIdOverride;

    [Tooltip("고정 아이템 (Fixed용)")]
    [SerializeField] ItemData fixedItem;

    [Tooltip("고정 아이템 수량")]
    [SerializeField] int fixedCount = 1;

    [Header("옵션")]
    [Tooltip("이미 스폰했는지")]
    [SerializeField] bool hasSpawned;

    [Tooltip("재스폰 여부")]
    [SerializeField] bool respawn;

    [Tooltip("재스폰 대기 시간 (초)")]
    [SerializeField] float respawnTime = 300f;

    [Header("Container 전용")]
    [Tooltip("연결된 루팅 상자 (Container 타입일 때)")]
    [SerializeField] LootContainer linkedContainer;

    float respawnTimer;

    void Start()
    {
        if (!hasSpawned)
            DoSpawn();
    }

    void Update()
    {
        if (hasSpawned && respawn)
        {
            respawnTimer += Time.deltaTime;
            if (respawnTimer >= respawnTime)
            {
                hasSpawned = false;
                respawnTimer = 0f;
                DoSpawn();
            }
        }
    }

    void DoSpawn()
    {
        hasSpawned = true;

        switch (spawnType)
        {
            case SpawnType.Ground:
                SpawnGround();
                break;
            case SpawnType.Container:
                SpawnContainer();
                break;
            case SpawnType.Fixed:
                SpawnFixed();
                break;
        }
    }

    void SpawnGround()
    {
        var items = RollLoot();
        if (items == null || items.Length == 0) return;
        float offset = 0f;

        for (int i = 0; i < items.Length; i++)
        {
            Vector3 pos = transform.position + new Vector3(
                Random.Range(-0.3f, 0.3f), 0,
                Random.Range(-0.3f, 0.3f) + offset);
            WorldItem.Drop(items[i], pos);
            offset += 0.2f;
        }
    }

    void SpawnContainer()
    {
        if (linkedContainer == null) return;

        var items = RollLoot();
        if (items == null || items.Length == 0) return;
        for (int i = 0; i < items.Length; i++)
        {
            linkedContainer.Grid.TryAutoPlace(items[i]);
        }
    }

    void SpawnFixed()
    {
        if (fixedItem == null) return;
        var item = new ItemInstance(fixedItem, fixedCount);
        WorldItem.Drop(item, transform.position);
    }

    ItemInstance[] RollLoot()
    {
        if (spawnTable != null)
            return spawnTable.Roll();

        if (!useRegionLoot) return null;

        var tier = RegionLootCatalog.TierForSpawnType(spawnType);
        return RegionLootCatalog.RollForActiveRegion(tier, regionIdOverride);
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
