using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 루팅 총괄 — **몇 개·어디에**(예산)를 정한다. **무엇이·몇 개씩**은 RegionLootCatalog(루팅 표)가 정한다.
/// 2026-09-11 사용자 결정 "예산 + 지역 표"(docs/region-loot.md §루팅 정리 결정) — 예전엔 아이템 DB 전체에서
/// 카테고리·희귀도로 무작위로 뽑아 루팅 표가 상자에 안 쓰였다(고철 300~1,000이 1~5로, 현금까지 섞여 나왔다).
///
/// 흐름(씬 로드 시 1회):
/// 1. 씬의 ItemSpawnPoint 수집 → Ground / Container(상자) / Fixed(자체 스폰)
/// 2. 예산 = 프로파일(바닥·상자 min~max, 밤 배율) × GameTuning.lootCountMult × 실내 배율 × 이 씬 배율. 단위 = 뽑기 횟수
/// 3. 상자: 섞은 뒤 **계산대·금고·좌판을 먼저**(예산과 무관하게 늘 채움 — 고철은 여기와 시체에서만),
///    나머지는 예산이 닿는 데까지. 상자마다 자기 종류(LootContainer.LootKind) 표에서 roll_count번 뽑는다.
///    예산 밖 상자는 비어 있다 — 섞기 때문에 매 판 다른 상자다.
/// 4. 바닥: 섞은 지점마다 최대 2개씩 'ground' 표에서.
/// </summary>
public class MapSpawnController : MonoBehaviour
{
    [Header("프로파일")]
    [Tooltip("예산(바닥·상자 개수 범위, 밤 배율). 희귀도·카테고리 가중치는 2026-09-11부터 안 쓴다 — 무엇이 나올지는 루팅 표가 정한다.")]
    [SerializeField] MapSpawnProfile profile;

    [Header("지역")]
    [Tooltip("루팅 표 지역. 비우면 RegionTimeManager 활성 지역")]
    [SerializeField] string regionIdOverride;

    [Header("건물 내부")]
    [Tooltip("이 씬이 '건물 내부'인가. true면 예산에 GameTuning.interiorLootBudgetMult를 곱한다\n" +
             "(내부는 맵 전체가 아니라 한 채이므로).")]
    [SerializeField] bool isInterior;

    [Tooltip("이 씬만의 루팅 예산 배율. **유니크 건물**(보석상·경찰서 등)을 일반 점포보다 후하게 만드는 값.\n" +
             "interiorLootBudgetMult 위에 곱해진다. 1=일반, 2.2=보석상급. '위험을 감수하면 더 좋은 물품'의 보상 쪽 축.")]
    [SerializeField] float budgetMult = 1f;

    [Header("디버그")]
    [SerializeField] bool logSpawnDetails = true;

    const int FallbackGroundBudget = 20, FallbackContainerBudget = 30;
    const int MaxPerGroundPoint = 2;

    /// <summary>지금 씬의 루팅 지역 — 시체 루팅이 같은 표를 쓰게(실내 보석상에서 쓰러뜨린 적은 보석상 지역 표).
    /// 씬이 바뀌면 새 컨트롤러가 덮는다.</summary>
    public static string CurrentRegionId { get; private set; }

    // 런타임
    bool hasSpawned;
    readonly List<ItemSpawnPoint> groundPoints = new List<ItemSpawnPoint>();
    readonly List<ItemSpawnPoint> containerPoints = new List<ItemSpawnPoint>();
    readonly List<ItemSpawnPoint> fixedPoints = new List<ItemSpawnPoint>();

    // 스폰 결과 통계
    int totalSpawned;
    readonly Dictionary<ItemRarity, int> rarityStats = new Dictionary<ItemRarity, int>();
    readonly Dictionary<ItemCategory, int> categoryStats = new Dictionary<ItemCategory, int>();

    void Awake()
    {
        // ItemSpawnPoint.Start()보다 먼저 수집해 둔다(앵커는 스스로 스폰하지 않는다 — Fixed만 예외).
        CollectSpawnPoints();
    }

    void Start()
    {
        if (hasSpawned) return;
        ExecuteSpawn();

        // 지도 구역이 없는 레이드 맵이면 기본 구역 1개(navigation.md §3.1) — 지도·나침반이 비지 않게.
        var map = RaidMapManager.Instance;
        if (map != null) map.RequestDefaultZone(gameObject.scene);
    }

    /// <summary>씬의 모든 ItemSpawnPoint 수집 및 분류</summary>
    void CollectSpawnPoints()
    {
        groundPoints.Clear();
        containerPoints.Clear();
        fixedPoints.Clear();

        var allPoints = FindObjectsByType<ItemSpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < allPoints.Length; i++)
        {
            var sp = allPoints[i];
            sp.managedByController = true;
            switch (sp.Type)
            {
                case ItemSpawnPoint.SpawnType.Ground:    groundPoints.Add(sp); break;
                case ItemSpawnPoint.SpawnType.Container: containerPoints.Add(sp); break;
                case ItemSpawnPoint.SpawnType.Fixed:     fixedPoints.Add(sp); break;
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

        string regionId = string.IsNullOrEmpty(regionIdOverride) ? RegionLootCatalog.GetActiveRegionId() : regionIdOverride;
        CurrentRegionId = regionId;
        bool isNight = RegionLootCatalog.IsNightInRegion(regionId);

        int groundBudget, containerBudget;
        if (profile != null)
        {
            groundBudget = profile.GetGroundBudget(isNight);
            containerBudget = profile.GetContainerBudget(isNight);
        }
        else
        {
            groundBudget = FallbackGroundBudget;
            containerBudget = FallbackContainerBudget;
            Debug.LogWarning($"[MapSpawnController] 프로파일 없음 — 기본 예산(바닥 {groundBudget}·상자 {containerBudget})으로 채운다");
        }

        var gt = GameTuning.Instance;
        float mult = gt != null ? gt.lootCountMult : 1f;
        // 2026-07-11: 내부 씬은 '한 채'라 맵 전체 예산을 그대로 쓰면 과다 — interiorLootBudgetMult로 줄인다.
        if (isInterior) mult *= gt != null ? gt.interiorLootBudgetMult : 0.25f;
        // 유니크 건물 가산 — 이 씬만 더 후하게(보석상·경찰서).
        if (budgetMult > 0f) mult *= budgetMult;
        groundBudget = Mathf.Max(0, Mathf.RoundToInt(groundBudget * mult));
        containerBudget = Mathf.Max(0, Mathf.RoundToInt(containerBudget * mult));

        if (logSpawnDetails)
            Debug.Log($"[MapSpawnController] 예산(뽑기): Ground={groundBudget}, Container={containerBudget} " +
                      $"(region={regionId}, night={isNight}, interior={isInterior})");

        FillContainers(regionId, isNight, containerBudget);
        FillGround(regionId, isNight, groundBudget);

        if (logSpawnDetails) LogStats();
    }

    /// <summary>고철이 나오는 오브젝트 — 계산대·금고·좌판. 예산과 무관하게 늘 채운다.
    /// (2026-09-11 검증: 좌판을 보통 상자와 같이 섞었더니 Zone1 상자 117개 중 예산 42뽑기에 밀려 좌판 4개가 전부 비었다 → 고철 0)</summary>
    static bool IsSpecial(string kind) => kind == "register" || kind == "safe" || kind == "stall";

    /// <summary>상자 채우기 — 고철 오브젝트(계산대·금고·좌판) 먼저(늘), 나머지는 섞은 순서로 예산이 닿는 데까지.</summary>
    void FillContainers(string regionId, bool night, int budget)
    {
        var special = new List<LootContainer>();
        var normal = new List<LootContainer>();
        for (int i = 0; i < containerPoints.Count; i++)
        {
            var p = containerPoints[i];
            var lc = p.GetComponent<LootContainer>();
            if (lc == null) lc = p.GetComponentInChildren<LootContainer>();
            if (lc == null && p.transform.parent != null) lc = p.transform.parent.GetComponent<LootContainer>();
            if (lc == null)
            {
                if (logSpawnDetails) Debug.LogWarning($"[MapSpawnController] 상자 앵커에 LootContainer 없음: {p.name}", p);
                continue;
            }
            var list = IsSpecial(lc.LootKind) ? special : normal;
            if (!list.Contains(lc)) list.Add(lc);
        }
        Shuffle(normal);

        int left = budget;
        foreach (var c in special) FillOne(c, regionId, night, ref left, true);
        foreach (var c in normal)
        {
            if (left <= 0) break;
            FillOne(c, regionId, night, ref left, false);
        }
    }

    void FillOne(LootContainer c, string regionId, bool night, ref int left, bool ignoreBudget)
    {
        if (!RegionLootCatalog.TryFindPool(regionId, c.LootKind, night, out var pool))
        {
            Debug.LogWarning($"[MapSpawnController] 루팅 표 없음: {regionId}/{c.LootKind}", c);
            return;
        }
        int rolls = Mathf.Max(1, pool.rollCount);
        for (int i = 0; i < rolls; i++)
        {
            if (!ignoreBudget)
            {
                if (left <= 0) return;
                left--;
            }
            var it = RegionLootCatalog.RollOnce(pool);
            if (it != null && c.AddItem(it)) Track(it);
        }
    }

    /// <summary>바닥 — 섞은 지점마다 최대 2개씩(예전엔 마지막 지점에 남은 걸 전부 몰아 15개 더미가 생겼다).</summary>
    void FillGround(string regionId, bool night, int budget)
    {
        if (groundPoints.Count == 0 || budget <= 0) return;
        if (!RegionLootCatalog.TryFindPool(regionId, "ground", night, out var pool))
        {
            Debug.LogWarning($"[MapSpawnController] 바닥 루팅 표 없음: {regionId}");
            return;
        }
        var pts = new List<ItemSpawnPoint>(groundPoints);
        Shuffle(pts);
        int left = budget;
        for (int pass = 0; pass < MaxPerGroundPoint && left > 0; pass++)
            for (int p = 0; p < pts.Count && left > 0; p++)
            {
                left--;
                var it = RegionLootCatalog.RollOnce(pool);
                if (it == null) continue;
                Vector3 pos = pts[p].transform.position + new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f));
                WorldItem.Drop(it, pos, this);
                Track(it);
            }
    }

    // ── 유틸리티 ──

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void Track(ItemInstance it)
    {
        totalSpawned++;
        if (it?.data == null) return;
        rarityStats.TryGetValue(it.data.rarity, out int r); rarityStats[it.data.rarity] = r + 1;
        categoryStats.TryGetValue(it.data.category, out int c); categoryStats[it.data.category] = c + 1;
    }

    void LogStats()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[MapSpawnController] 스폰 완료: 총 {totalSpawned}개");
        sb.Append("  희귀도: ");
        foreach (var kv in rarityStats) sb.Append($"{kv.Key}={kv.Value} ");
        sb.AppendLine();
        sb.Append("  카테고리: ");
        foreach (var kv in categoryStats) sb.Append($"{kv.Key}={kv.Value} ");
        Debug.Log(sb.ToString());
    }

    // ── 외부 API ──

    /// <summary>수동 리스폰 — 기존 내용물 위에 **더한다**(비우지 않는다).</summary>
    public void Respawn()
    {
        hasSpawned = false;
        CollectSpawnPoints();
        ExecuteSpawn();
    }

    /// <summary>런타임 프로파일 교체</summary>
    public void SetProfile(MapSpawnProfile newProfile) => profile = newProfile;

    /// <summary>스폰 통계 반환</summary>
    public (int total, Dictionary<ItemRarity, int> rarity, Dictionary<ItemCategory, int> category) GetStats()
        => (totalSpawned, new Dictionary<ItemRarity, int>(rarityStats), new Dictionary<ItemCategory, int>(categoryStats));
}
