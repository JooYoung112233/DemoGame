using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 루팅 표 — **무엇이·몇 개씩** 나오는지의 유일한 출처(2026-09-11 사용자 결정 "예산 + 지역 표",
/// docs/region-loot.md §루팅 정리 결정). 얼마나·어디에(예산)는 MapSpawnController가 정한다.
///
/// 같은 형식(region,source,roll_count,item_id,weight,min,max)의 두 파일을 읽는다:
///   • Resources/region_loot.txt — 지역 × 시간대 일반 표(container_day/night · ground_day/night · *_anomaly).
///     원본 tools/region_loot.csv, 컨트롤 패널 '지역 루트' 탭이 편집한다.
///   • Resources/loot_tables.txt — 지역 × **상자 종류** 표(junk·trunk·stall·crate·register·safe·ground·corpse·int_*).
///     2026-09-11 신설. 컨트롤 패널 CSV 탭에서 편집한다.
/// 찾는 순서(TryFindPool): 종류_night(밤) → 종류 → 일반 표(바닥이면 ground_*, 그 밖엔 container_*) → 같은 순서로 scrap_market.
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
        public string source;
        public int rollCount;
        public LootEntry[] entries;
    }

    /// <summary>표가 없는 지역·종류는 이 지역의 표를 쓴다.</summary>
    public const string FallbackRegion = "scrap_market";

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

    static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        pools = new Dictionary<string, LootPool>();

        foreach (var file in new[] { "region_loot", "loot_tables" })
        {
            var text = Resources.Load<TextAsset>(file);
            if (text == null)
            {
                Debug.LogWarning($"[RegionLootCatalog] Resources/{file}.txt 없음");
                continue;
            }
            ParseCsv(text.text);
        }
        Debug.Log($"[RegionLootCatalog] {pools.Count}개 표 로드");
    }

    static void ParseCsv(string csv)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var lines = csv.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

            var p = line.Split(',');
            if (p.Length < 7) continue;

            string region = p[0].Trim();
            string source = p[1].Trim().ToLowerInvariant();
            if (region.Length == 0 || source.Length == 0) continue;
            if (!int.TryParse(p[2].Trim(), out int rollCount)) continue;
            string itemId = p[3].Trim();
            if (!float.TryParse(p[4].Trim(), System.Globalization.NumberStyles.Float, inv, out float weight)) continue;
            if (!int.TryParse(p[5].Trim(), out int minC) || !int.TryParse(p[6].Trim(), out int maxC)) continue;

            string key = PoolKey(region, source);
            if (!pools.TryGetValue(key, out var pool))
                pool = new LootPool { regionId = region, source = source, entries = System.Array.Empty<LootEntry>() };

            var list = new List<LootEntry>(pool.entries);
            list.Add(new LootEntry { itemId = itemId, weight = weight, minCount = minC, maxCount = maxC });
            pool.entries = list.ToArray();
            pool.rollCount = rollCount;   // 같은 표 = 같은 roll_count(파일 규약). 마지막 값 채택
            pools[key] = pool;
        }
    }

    /// <summary>ContainerDay → container_day (region_loot.txt의 tier 컬럼 이름).</summary>
    public static string SourceName(RegionLootTier tier)
    {
        var s = tier.ToString();
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsUpper(c) && i > 0) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    static string PoolKey(string regionId, string source) => $"{regionId}|{source}";

    public static bool TryGetPool(string regionId, string source, out LootPool pool)
    {
        EnsureLoaded();
        return pools.TryGetValue(PoolKey(regionId, source), out pool);
    }

    public static bool TryGetPool(string regionId, RegionLootTier tier, out LootPool pool)
        => TryGetPool(regionId, SourceName(tier), out pool);

    /// <summary>상자 종류·바닥·시체의 표를 찾는다 — **종류 표를 먼저**(지역 → scrap_market, 밤이면 종류_night부터),
    /// 없을 때만 일반 표(지역 → scrap_market). 종류 표가 우선이라야 다른 지역 건물의 계산대·금고도 고철이 나온다.</summary>
    public static bool TryFindPool(string regionId, string source, bool night, out LootPool pool)
    {
        EnsureLoaded();
        source = string.IsNullOrEmpty(source) ? "crate" : source.ToLowerInvariant();
        string generic = source == "ground" ? (night ? "ground_night" : "ground_day")
                                            : (night ? "container_night" : "container_day");
        var regions = new[] { regionId, FallbackRegion };
        foreach (var r in regions)
        {
            if (string.IsNullOrEmpty(r)) continue;
            if (night && TryGetPool(r, source + "_night", out pool) && pool.entries.Length > 0) return true;
            if (TryGetPool(r, source, out pool) && pool.entries.Length > 0) return true;
        }
        foreach (var r in regions)
            if (!string.IsNullOrEmpty(r) && TryGetPool(r, generic, out pool) && pool.entries.Length > 0) return true;
        pool = default;
        return false;
    }

    /// <summary>표에서 한 번 뽑기(가중치). 전역 노브(lootChanceMult 꽝 · valuableWeightMult) 적용. 꽝이면 null.</summary>
    public static ItemInstance RollOnce(LootPool p)
    {
        if (p.entries == null || p.entries.Length == 0) return null;
        var gt = GameTuning.Instance;
        float chanceMult = gt != null ? gt.lootChanceMult : 1f;
        float valuableMult = gt != null ? gt.valuableWeightMult : 1f;
        if (chanceMult < 1f && Random.value >= chanceMult) return null;

        float total = 0f;
        var eff = new float[p.entries.Length];
        for (int i = 0; i < p.entries.Length; i++)
        {
            float w = p.entries[i].weight;
            if (valuableMult != 1f)
            {
                var d = ItemDatabase.Get(p.entries[i].itemId);
                if (d != null && d.category == ItemCategory.Valuable) w *= valuableMult;
            }
            eff[i] = w;
            total += w;
        }
        if (total <= 0f) return null;

        float roll = Random.Range(0f, total), cum = 0f;
        for (int i = 0; i < p.entries.Length; i++)
        {
            cum += eff[i];
            if (roll > cum) continue;
            var e = p.entries[i];
            var data = ItemDatabase.Get(e.itemId);
            if (data == null)
            {
                Debug.LogWarning($"[RegionLootCatalog] 아이템 없음: {e.itemId} ({p.regionId}/{p.source})");
                return null;
            }
            return new ItemInstance(data, Random.Range(e.minCount, Mathf.Max(e.minCount, e.maxCount) + 1));
        }
        return null;
    }

    /// <summary>표 하나를 roll_count번 뽑는다.</summary>
    static ItemInstance[] RollPool(LootPool p)
    {
        var results = new List<ItemInstance>();
        for (int r = 0; r < p.rollCount; r++)
        {
            var it = RollOnce(p);
            if (it != null) results.Add(it);
        }
        return results.ToArray();
    }

    /// <summary>지역·일반 티어에서 아이템 배열(에디터 시뮬 등 — 옛 API).</summary>
    public static ItemInstance[] Roll(string regionId, RegionLootTier tier)
    {
        if (!TryGetPool(regionId, tier, out var p) || p.entries == null || p.entries.Length == 0)
        {
            Debug.LogWarning($"[RegionLootCatalog] 표 없음: {regionId} / {SourceName(tier)}");
            return new ItemInstance[0];
        }
        return RollPool(p);
    }

    /// <summary>지역·종류(corpse·crate …)의 표를 찾아 roll_count번 뽑는다(찾는 순서는 TryFindPool).</summary>
    public static ItemInstance[] RollSource(string regionId, string source, bool night)
        => TryFindPool(regionId, source, night, out var p) ? RollPool(p) : new ItemInstance[0];

    /// <summary>활성 지역 + 일반 티어로 롤</summary>
    public static ItemInstance[] RollForActiveRegion(RegionLootTier tier, string overrideRegionId = null)
    {
        string region = string.IsNullOrEmpty(overrideRegionId) ? GetActiveRegionId() : overrideRegionId;
        tier = ResolveTier(tier, region);
        return Roll(region, tier);
    }
}
