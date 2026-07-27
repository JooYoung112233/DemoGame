using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 단위 아이템 스폰 총괄 컨트롤러.
/// 맵 진입 시 프로파일 기반으로 아이템 예산을 산정하고,
/// 씬의 ItemSpawnPoint들에 아이템을 분배.
///
/// 흐름:
/// 1. 씬의 모든 ItemSpawnPoint 수집
/// 2. Fixed 포인트 → 고정 아이템 먼저 스폰
/// 3. 프로파일 예산 산정 (총량, 희귀도, 카테고리)
/// 4. 예산 내에서 아이템 풀 생성
/// 5. Ground/Container 포인트에 분배
/// </summary>
public class MapSpawnController : MonoBehaviour
{
    [Header("프로파일")]
    [Tooltip("이 맵의 스폰 프로파일")]
    [SerializeField] MapSpawnProfile profile;

    [Tooltip("프로파일 없으면 RegionLootCatalog 폴백 사용")]
    [SerializeField] bool fallbackToRegionLoot = true;

    [Header("지역")]
    [Tooltip("비우면 RegionTimeManager 활성 지역 사용")]
    [SerializeField] string regionIdOverride;

    [Header("건물 내부")]
    [Tooltip("이 씬이 '건물 내부'인가. true면 예산에 GameTuning.interiorLootBudgetMult를 곱한다\n" +
             "(내부는 맵 전체가 아니라 한 채이므로). 루트 테이블 자체는 지역 확률 그대로 사용.")]
    [SerializeField] bool isInterior;

    [Header("디버그")]
    [SerializeField] bool logSpawnDetails = true;

    // 런타임
    bool hasSpawned;
    List<ItemSpawnPoint> groundPoints = new List<ItemSpawnPoint>();
    List<ItemSpawnPoint> containerPoints = new List<ItemSpawnPoint>();
    List<ItemSpawnPoint> fixedPoints = new List<ItemSpawnPoint>();

    // 스폰 결과 통계
    int totalSpawned;
    Dictionary<ItemRarity, int> rarityStats = new Dictionary<ItemRarity, int>();
    Dictionary<ItemCategory, int> categoryStats = new Dictionary<ItemCategory, int>();

    void Awake()
    {
        // Awake에서 스폰 포인트 수집 + managedByController 플래그 설정
        // → ItemSpawnPoint.Start()보다 먼저 실행되어 자체 스폰을 차단
        CollectSpawnPoints();
    }

    void Start()
    {
        if (hasSpawned) return;
        ExecuteSpawn();
    }

    /// <summary>씬의 모든 ItemSpawnPoint 수집 및 분류</summary>
    void CollectSpawnPoints()
    {
        groundPoints.Clear();
        containerPoints.Clear();
        fixedPoints.Clear();

        var allPoints = FindObjectsByType<ItemSpawnPoint>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < allPoints.Length; i++)
        {
            var sp = allPoints[i];

            // 컨트롤러 관리 플래그 설정 (Fixed 제외)
            sp.managedByController = true;

            switch (sp.Type)
            {
                case ItemSpawnPoint.SpawnType.Ground:
                    groundPoints.Add(sp);
                    break;
                case ItemSpawnPoint.SpawnType.Container:
                    containerPoints.Add(sp);
                    break;
                case ItemSpawnPoint.SpawnType.Fixed:
                    fixedPoints.Add(sp);
                    break;
            }
        }

        if (logSpawnDetails)
            Debug.Log($"[MapSpawnController] 스폰 포인트 수집: Ground={groundPoints.Count}, Container={containerPoints.Count}, Fixed={fixedPoints.Count}");
    }

    /// <summary>전체 스폰 실행</summary>
    void ExecuteSpawn()
    {
        hasSpawned = true;
        totalSpawned = 0;
        rarityStats.Clear();
        categoryStats.Clear();

        string regionId = string.IsNullOrEmpty(regionIdOverride)
            ? RegionLootCatalog.GetActiveRegionId()
            : regionIdOverride;

        bool isNight = RegionLootCatalog.IsNightInRegion(regionId);

        // 1. Fixed 아이템 먼저 (프로파일 예산과 무관)
        SpawnFixedItems();

        // 프로파일 없으면 폴백
        if (profile == null)
        {
            if (fallbackToRegionLoot)
                FallbackRegionLootSpawn(regionId, isNight);
            else if (logSpawnDetails)
                Debug.LogWarning("[MapSpawnController] 프로파일 없음, 폴백 비활성");
            return;
        }

        // 2. 예산 산정
        int groundBudget = profile.GetGroundBudget(isNight);
        int containerBudget = profile.GetContainerBudget(isNight);

        // 2026-07-11: **내부 씬은 '한 채'라 맵 전체 예산을 그대로 쓰면 과다**(내부가 12개면 12맵치가 뿌려짐).
        //   GameTuning.interiorLootBudgetMult로 줄인다 — QA가 "파밍을 늘릴지"를 판단해 조절하는 노브.
        if (isInterior)
        {
            float im = GameTuning.Instance != null ? GameTuning.Instance.interiorLootBudgetMult : 0.25f;
            groundBudget    = Mathf.Max(1, Mathf.RoundToInt(groundBudget * im));
            containerBudget = Mathf.Max(1, Mathf.RoundToInt(containerBudget * im));
        }

        if (logSpawnDetails)
            Debug.Log($"[MapSpawnController] 예산: Ground={groundBudget}, Container={containerBudget} " +
                      $"(night={isNight}, interior={isInterior})");

        // 3. 아이템 풀 생성
        var groundItems = GenerateItemPool(groundBudget, regionId, isNight);
        var containerItems = GenerateItemPool(containerBudget, regionId, isNight);

        // 4. 분배
        DistributeToPoints(groundItems, groundPoints, ItemSpawnPoint.SpawnType.Ground);
        DistributeToContainers(containerItems, containerPoints);

        // 5. 통계 로그
        if (logSpawnDetails)
            LogStats();
    }

    /// <summary>Fixed 포인트는 각자 자체 스폰</summary>
    void SpawnFixedItems()
    {
        for (int i = 0; i < fixedPoints.Count; i++)
        {
            // ItemSpawnPoint.DoSpawn()이 Start에서 호출되므로,
            // 여기서는 별도 처리 불필요 (Fixed는 자체 처리)
        }
    }

    /// <summary>
    /// 프로파일 기반으로 아이템 인스턴스 풀 생성.
    /// 카테고리 → 희귀도 → 해당 조건의 아이템 중 랜덤 선택.
    /// </summary>
    List<ItemInstance> GenerateItemPool(int budget, string regionId, bool isNight)
    {
        var result = new List<ItemInstance>(budget);
        if (budget <= 0) return result;

        float[] rarityWeights = profile.GetRarityWeights(isNight);
        float[] categoryWeights = profile.GetCategoryWeights();

        // ItemDatabase에서 지역 + 카테고리별 아이템 캐시
        var itemsByCategory = BuildCategoryCache(regionId);

        for (int i = 0; i < budget; i++)
        {
            // 카테고리 선택
            var category = (ItemCategory)WeightedRandom(categoryWeights);

            // 해당 카테고리에 아이템이 없으면 다른 카테고리 시도
            if (!itemsByCategory.ContainsKey(category) || itemsByCategory[category].Count == 0)
            {
                category = FindFallbackCategory(itemsByCategory, categoryWeights);
                if (!itemsByCategory.ContainsKey(category)) continue;
            }

            var candidates = itemsByCategory[category];

            // 희귀도 선택
            var rarity = (ItemRarity)WeightedRandom(rarityWeights);

            // 해당 희귀도 아이템 필터링
            var filtered = FilterByRarity(candidates, rarity);

            // 정확한 희귀도가 없으면 가장 가까운 것으로
            if (filtered.Count == 0)
                filtered = FindClosestRarity(candidates, rarity);

            if (filtered.Count == 0) continue;

            // 랜덤 선택
            var chosen = filtered[Random.Range(0, filtered.Count)];
            int count = chosen.maxStack > 1
                ? Random.Range(1, Mathf.Min(chosen.maxStack, 5) + 1)
                : 1;

            result.Add(new ItemInstance(chosen, count));

            // 통계
            TrackStats(chosen);
        }

        return result;
    }

    /// <summary>지역 기반 카테고리별 아이템 캐시 구축</summary>
    Dictionary<ItemCategory, List<ItemData>> BuildCategoryCache(string regionId)
    {
        var cache = new Dictionary<ItemCategory, List<ItemData>>();

        var allItems = ItemDatabase.GetAll();
        if (allItems == null) return cache;

        foreach (var item in allItems)
        {
            // 지역 필터: 공용 아이템 + 해당 지역 전용 아이템만
            if (!string.IsNullOrEmpty(item.primaryRegionId) && item.primaryRegionId != regionId)
                continue;

            // 열쇠/스토리 아이템은 Fixed로만 스폰 (예산 풀에서 제외)
            if (item.category == ItemCategory.Key) continue;

            if (!cache.ContainsKey(item.category))
                cache[item.category] = new List<ItemData>();

            cache[item.category].Add(item);
        }

        return cache;
    }

    List<ItemData> FilterByRarity(List<ItemData> items, ItemRarity target)
    {
        var result = new List<ItemData>();
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].rarity == target)
                result.Add(items[i]);
        }
        return result;
    }

    /// <summary>정확한 희귀도가 없으면 가장 가까운 낮은 등급으로</summary>
    List<ItemData> FindClosestRarity(List<ItemData> items, ItemRarity target)
    {
        // 낮은 등급부터 올라가며 찾기
        for (int delta = 1; delta <= 4; delta++)
        {
            int lower = (int)target - delta;
            int higher = (int)target + delta;

            if (lower >= 0)
            {
                var result = FilterByRarity(items, (ItemRarity)lower);
                if (result.Count > 0) return result;
            }
            if (higher <= 4)
            {
                var result = FilterByRarity(items, (ItemRarity)higher);
                if (result.Count > 0) return result;
            }
        }
        return new List<ItemData>();
    }

    ItemCategory FindFallbackCategory(Dictionary<ItemCategory, List<ItemData>> cache, float[] weights)
    {
        // 가중치 순으로 비어있지 않은 카테고리 반환
        var sorted = new List<(ItemCategory cat, float w)>();
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] > 0 && cache.ContainsKey((ItemCategory)i) && cache[(ItemCategory)i].Count > 0)
                sorted.Add(((ItemCategory)i, weights[i]));
        }

        if (sorted.Count == 0) return ItemCategory.Misc;

        float total = 0;
        for (int i = 0; i < sorted.Count; i++) total += sorted[i].w;
        float roll = Random.Range(0f, total);
        float cum = 0;
        for (int i = 0; i < sorted.Count; i++)
        {
            cum += sorted[i].w;
            if (roll <= cum) return sorted[i].cat;
        }
        return sorted[sorted.Count - 1].cat;
    }

    /// <summary>Ground 아이템을 스폰 포인트들에 균등 분배</summary>
    void DistributeToPoints(List<ItemInstance> items, List<ItemSpawnPoint> points,
                            ItemSpawnPoint.SpawnType type)
    {
        if (points.Count == 0 || items.Count == 0) return;

        // 셔플
        ShuffleList(items);

        int perPoint = Mathf.Max(1, items.Count / points.Count);
        int idx = 0;

        for (int p = 0; p < points.Count && idx < items.Count; p++)
        {
            // 마지막 포인트는 남은 아이템 전부
            int count = (p == points.Count - 1)
                ? items.Count - idx
                : Mathf.Min(perPoint, items.Count - idx);

            for (int i = 0; i < count && idx < items.Count; i++, idx++)
            {
                Vector3 pos = points[p].transform.position + new Vector3(
                    Random.Range(-0.4f, 0.4f), 0f,
                    Random.Range(-0.4f, 0.4f));
                WorldItem.Drop(items[idx], pos);
                totalSpawned++;
            }
        }
    }

    /// <summary>Container 아이템을 상자들에 분배</summary>
    void DistributeToContainers(List<ItemInstance> items, List<ItemSpawnPoint> points)
    {
        if (points.Count == 0 || items.Count == 0) return;

        ShuffleList(items);

        // 각 상자에 연결된 LootContainer를 찾아서 배치
        var containers = new List<LootContainer>();
        for (int i = 0; i < points.Count; i++)
        {
            var lc = points[i].GetComponent<LootContainer>();
            if (lc == null) lc = points[i].GetComponentInChildren<LootContainer>();
            if (lc == null)
            {
                // 같은 GO 또는 부모에서 탐색
                var parent = points[i].transform.parent;
                if (parent != null) lc = parent.GetComponent<LootContainer>();
            }
            if (lc != null) containers.Add(lc);
        }

        if (containers.Count == 0)
        {
            // 컨테이너 없으면 바닥에 스폰
            DistributeToPoints(items, points, ItemSpawnPoint.SpawnType.Ground);
            return;
        }

        int idx = 0;
        int perContainer = Mathf.Max(1, items.Count / containers.Count);

        for (int c = 0; c < containers.Count && idx < items.Count; c++)
        {
            int count = (c == containers.Count - 1)
                ? items.Count - idx
                : Mathf.Min(perContainer, items.Count - idx);

            for (int i = 0; i < count && idx < items.Count; i++, idx++)
            {
                if (containers[c].Grid.TryAutoPlace(items[idx]))
                    totalSpawned++;
            }
        }
    }

    /// <summary>프로파일 없을 때 기존 RegionLootCatalog 폴백</summary>
    void FallbackRegionLootSpawn(string regionId, bool isNight)
    {
        if (logSpawnDetails)
            Debug.Log($"[MapSpawnController] 프로파일 없음 → RegionLootCatalog 폴백 ({regionId})");

        // 각 스폰 포인트가 자체 Start()에서 스폰하도록 내버려둠
        // (ItemSpawnPoint.useRegionLoot = true 상태)
    }

    // ── 유틸리티 ──

    /// <summary>가중치 배열에서 랜덤 인덱스 선택</summary>
    static int WeightedRandom(float[] weights)
    {
        float total = 0f;
        for (int i = 0; i < weights.Length; i++) total += weights[i];
        if (total <= 0f) return 0;

        float roll = Random.Range(0f, total);
        float cum = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cum += weights[i];
            if (roll <= cum) return i;
        }
        return weights.Length - 1;
    }

    static void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    void TrackStats(ItemData item)
    {
        if (!rarityStats.ContainsKey(item.rarity))
            rarityStats[item.rarity] = 0;
        rarityStats[item.rarity]++;

        if (!categoryStats.ContainsKey(item.category))
            categoryStats[item.category] = 0;
        categoryStats[item.category]++;
    }

    void LogStats()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[MapSpawnController] 스폰 완료: 총 {totalSpawned}개");

        sb.Append("  희귀도: ");
        foreach (var kv in rarityStats)
            sb.Append($"{kv.Key}={kv.Value} ");
        sb.AppendLine();

        sb.Append("  카테고리: ");
        foreach (var kv in categoryStats)
            sb.Append($"{kv.Key}={kv.Value} ");

        Debug.Log(sb.ToString());
    }

    // ── 외부 API ──

    /// <summary>수동 리스폰 (맵 재입장 등)</summary>
    public void Respawn()
    {
        hasSpawned = false;
        CollectSpawnPoints();
        ExecuteSpawn();
    }

    /// <summary>런타임 프로파일 교체</summary>
    public void SetProfile(MapSpawnProfile newProfile)
    {
        profile = newProfile;
    }

    /// <summary>스폰 통계 반환</summary>
    public (int total, Dictionary<ItemRarity, int> rarity, Dictionary<ItemCategory, int> category) GetStats()
    {
        return (totalSpawned, new Dictionary<ItemRarity, int>(rarityStats), new Dictionary<ItemCategory, int>(categoryStats));
    }
}
