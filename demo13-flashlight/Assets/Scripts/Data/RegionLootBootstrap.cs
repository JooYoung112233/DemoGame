using UnityEngine;

/// <summary>
/// 씬에 ItemSpawnPoint가 없을 때 활성 지역 루트를 바닥에 소량 스폰 (데모).
/// </summary>
public class RegionLootBootstrap : MonoBehaviour
{
    [SerializeField] bool spawnOnStart = true;
    [SerializeField] int groundRolls = 1;
    [SerializeField] int containerRolls = 1;
    [SerializeField] float scatterRadius = 2f;

    void Start()
    {
        if (!spawnOnStart) return;
        if (FindObjectsByType<ItemSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        Vector3 origin = player != null ? player.transform.position : Vector3.zero;
        string region = RegionLootCatalog.GetActiveRegionId();

        DropTier(origin, region, RegionLootTier.GroundDay, groundRolls, scatterRadius);
        DropTier(origin + Vector3.right * 1.5f, region, RegionLootTier.ContainerDay, containerRolls, scatterRadius);
    }

    static void DropTier(Vector3 origin, string region, RegionLootTier tier, int times, float radius)
    {
        tier = RegionLootCatalog.ResolveTier(tier, region);
        for (int t = 0; t < times; t++)
        {
            var items = RegionLootCatalog.Roll(region, tier);
            for (int i = 0; i < items.Length; i++)
            {
                Vector3 pos = origin + new Vector3(
                    Random.Range(-radius, radius), 0f,
                    Random.Range(-radius, radius) + i * 0.25f);
                WorldItem.Drop(items[i], pos);
            }
        }
    }
}
