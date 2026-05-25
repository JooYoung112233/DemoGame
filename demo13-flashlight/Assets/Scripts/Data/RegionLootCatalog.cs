using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지역별 드롭 테이블. tools/region_loot.csv → Resources/region_loot.txt 로 복사 후 로드.
/// SpawnTable SO 없이 ItemSpawnPoint·디버그에서 사용.
/// </summary>
public static class RegionLootCatalog
{
    public struct LootEntry
    {
        public string itemId;
        public float weight;
        public int minCount;
        public int maxCount;
    }

    public struct LootPool
    {
        public string regionId;
        public RegionLootTier tier;
        public int rollCount;
        public LootEntry[] entries;
    }

    static Dictionary<string, LootPool> pools;
    static bool loaded;

    public static string GetActiveRegionId()
    {
        if (RegionTimeManager.Instance != null && !string.IsNullOrEmpty(RegionTimeManager.Instance.ActiveRegionId))
            return RegionTimeManager.Instance.ActiveRegionId;
        return "scrap_market";
    }

    public static bool IsNightInRegion(string regionId)
    {
        if (RegionTimeManager.Instance == null || string.IsNullOrEmpty(regionId))
            return false;
        var rt = RegionTimeManager.Instance.GetRegion(regionId);
        return rt != null && rt.isNight;
    }

    public static RegionLootTier ResolveTier(RegionLootTier baseTier, string regionId)
    {
        bool night = IsNightInRegion(regionId);
        return baseTier switch
        {
            RegionLootTier.GroundDay => night ? RegionLootTier.GroundNight : RegionLootTier.GroundDay,
            RegionLootTier.GroundNight => RegionLootTier.GroundNight,
            RegionLootTier.ContainerDay => night ? RegionLootTier.ContainerNight : RegionLootTier.ContainerDay,
            RegionLootTier.ContainerNight => RegionLootTier.ContainerNight,
            _ => baseTier,
        };
    }

    public static RegionLootTier TierForSpawnType(ItemSpawnPoint.SpawnType spawnType, bool forceNight = false)
    {
        bool night = forceNight || IsNightInRegion(GetActiveRegionId());
        return spawnType switch
        {
            ItemSpawnPoint.SpawnType.Ground => night ? RegionLootTier.GroundNight : RegionLootTier.GroundDay,
            ItemSpawnPoint.SpawnType.Container => night ? RegionLootTier.ContainerNight : RegionLootTier.ContainerDay,
            _ => night ? RegionLootTier.ContainerNight : RegionLootTier.ContainerDay,
        };
    }

    static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        pools = new Dictionary<string, LootPool>();

        var text = Resources.Load<TextAsset>("region_loot");
        if (text == null)
        {
            Debug.LogWarning("[RegionLootCatalog] Resources/region_loot.txt 없음 — 내장 폴백 없음.");
            return;
        }

        ParseCsv(text.text);
        Debug.Log($"[RegionLootCatalog] {pools.Count}개 풀 로드");
    }

    static void ParseCsv(string csv)
    {
        var lines = csv.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

            var p = line.Split(',');
            if (p.Length < 7) continue;

            string region = p[0].Trim();
            string tierStr = p[1].Trim();
            int rollCount = int.Parse(p[2]);
            string itemId = p[3].Trim();
            float weight = float.Parse(p[4]);
            int minC = int.Parse(p[5]);
            int maxC = int.Parse(p[6]);

            if (!System.Enum.TryParse<RegionLootTier>(ToTierEnumName(tierStr), true, out var tier))
                continue;

            string key = PoolKey(region, tier);
            if (!pools.TryGetValue(key, out var pool))
            {
                pool = new LootPool
                {
                    regionId = region,
                    tier = tier,
                    rollCount = rollCount,
                    entries = System.Array.Empty<LootEntry>(),
                };
            }

            var list = new List<LootEntry>(pool.entries);
            list.Add(new LootEntry { itemId = itemId, weight = weight, minCount = minC, maxCount = maxC });
            pool.entries = list.ToArray();
            pool.rollCount = rollCount;
            pools[key] = pool;
        }
    }

    static string ToTierEnumName(string csvTier)
    {
        // container_day → ContainerDay
        var parts = csvTier.Split('_');
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            sb.Append(char.ToUpper(parts[i][0]));
            if (parts[i].Length > 1)
                sb.Append(parts[i].Substring(1));
        }
        return sb.ToString();
    }

    static string PoolKey(string regionId, RegionLootTier tier) => $"{regionId}|{tier}";

    public static bool TryGetPool(string regionId, RegionLootTier tier, out LootPool pool)
    {
        EnsureLoaded();
        return pools.TryGetValue(PoolKey(regionId, tier), out pool);
    }

    /// <summary>지역·티어에서 아이템 인스턴스 배열 생성</summary>
    public static ItemInstance[] Roll(string regionId, RegionLootTier tier)
    {
        EnsureLoaded();
        if (!TryGetPool(regionId, tier, out var p) || p.entries == null || p.entries.Length == 0)
        {
            Debug.LogWarning($"[RegionLootCatalog] 풀 없음: {regionId} / {tier}");
            return new ItemInstance[0];
        }
        float totalWeight = 0f;
        for (int i = 0; i < p.entries.Length; i++)
            totalWeight += p.entries[i].weight;

        if (totalWeight <= 0f) return new ItemInstance[0];

        var results = new List<ItemInstance>();
        for (int r = 0; r < p.rollCount; r++)
        {
            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < p.entries.Length; i++)
            {
                cumulative += p.entries[i].weight;
                if (roll > cumulative) continue;

                var e = p.entries[i];
                var data = ItemDatabase.Get(e.itemId);
                if (data == null)
                {
                    Debug.LogWarning($"[RegionLootCatalog] 아이템 없음: {e.itemId}");
                    break;
                }

                int count = Random.Range(e.minCount, e.maxCount + 1);
                results.Add(new ItemInstance(data, count));
                break;
            }
        }

        return results.ToArray();
    }

    /// <summary>활성 지역 + 스폰 타입으로 롤</summary>
    public static ItemInstance[] RollForActiveRegion(RegionLootTier tier, string overrideRegionId = null)
    {
        string region = string.IsNullOrEmpty(overrideRegionId) ? GetActiveRegionId() : overrideRegionId;
        tier = ResolveTier(tier, region);
        return Roll(region, tier);
    }
}
