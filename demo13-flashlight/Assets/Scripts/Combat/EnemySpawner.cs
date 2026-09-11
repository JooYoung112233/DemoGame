using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 레이드 씬의 SpawnZone들을 읽어 적을 런타임 스폰. 부팅 시 자가 생성(DontDestroyOnLoad),
/// 게임플레이 씬이 additive 로드될 때마다 그 씬의 SpawnZone만 처리(씬당 1회).
/// 안전구역(안전가옥/은신처/전당포)엔 SpawnZone이 없으므로 아무것도 안 함.
///
/// 각 존: round(enemyCount × GameTuning.enemySpawnCountMult) 마리를 존 영역(2D) 랜덤 위치에 스폰.
/// 유닛 프리팹(UnitStatData.generatedPrefab)이 있으면 그걸, 없으면 런타임 그레이박스 적을 생성.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    readonly HashSet<Scene> _processed = new HashSet<Scene>();   // 처리된 씬 — 중복 스폰 방지 (Scene은 값 비교)

    /// <summary>부팅 시 자가 생성 (Systems 씬 유무와 무관하게 항상 동작 — 맵툴 씬 제외).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;
        if (Instance != null) return;
        var go = new GameObject("[EnemySpawner]");
        DontDestroyOnLoad(go);
        go.AddComponent<EnemySpawner>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void Start()
    {
        // 레이드 씬에서 바로 Play한 경우(이미 로드됨 → sceneLoaded 못 받음) 활성 씬 1회 처리.
        ProcessScene(SceneManager.GetActiveScene());
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ProcessScene(scene);

    // 언로드 시 가드 해제 → 같은 레이드 재입장(핸들 재사용 가능) 시 재스폰 보장.
    void OnSceneUnloaded(Scene scene) => _processed.Remove(scene);

    void ProcessScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        if (!SystemsScene.IsGameplayScene(scene)) return;   // Systems/맵툴 제외 (안전구역은 존이 없어 자연히 0마리)
        if (!_processed.Add(scene)) return;           // 씬당 1회
        SpawnInScene(scene);
    }

    void SpawnInScene(Scene scene)
    {
        var zones = FindObjectsByType<SpawnZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        float mult = GameTuning.Instance != null ? GameTuning.Instance.enemySpawnCountMult : 1f;

        int total = 0, zoneCount = 0;
        var spawned = new List<EnemyController>();
        Transform folder = null;   // [Runtime]/Enemies — 첫 스폰 때 만든다(적 없는 씬엔 폴더도 없음)
        for (int i = 0; i < zones.Length; i++)
        {
            var z = zones[i];
            if (z == null || z.gameObject.scene != scene) continue;   // 방금 로드된 씬의 존만
            zoneCount++;
            int count = Mathf.Max(0, Mathf.RoundToInt(z.EnemyCount * mult));
            for (int n = 0; n < count; n++)
            {
                var e = SpawnOne(z);
                if (e == null) continue;
                SceneManager.MoveGameObjectToScene(e, scene);   // 레이드 씬과 함께 언로드되도록
                if (folder == null) folder = HierarchyFolder.RuntimeFolder(scene, "Enemies");
                e.transform.SetParent(folder, true);            // 하이어라키 정리 — 씬 루트에 수십 마리가 평평하게 깔리지 않게
                var ec = e.GetComponent<EnemyController>();
                if (ec != null) spawned.Add(ec);
                total++;
            }
        }

        // 약탈자 회수(docs/raid.md) — 지난 사망에서 잃은 물품이 있으면 이 레이드 적 몇 마리에 분배(한 번의 기회).
        if (ScavengerLoot.HasPending) ScavengerLoot.Distribute(spawned);

        if (total > 0)
            Debug.Log($"[EnemySpawner] '{scene.name}' 적 {total}기 스폰 (존 {zoneCount}, 배율 {mult:0.##})");
    }

    GameObject SpawnOne(SpawnZone zone)
    {
        Vector3 pos = zone.GetRandomPoint();
        string key = string.IsNullOrEmpty(zone.UnitKey) ? "bandit_melee_1" : zone.UnitKey;   // 2026-07-11: StatDB 실존 키(구 "bandit_melee"는 없어서 인스펙터 폴백으로 샘)

        var unit = StatDB.Instance != null ? StatDB.Instance.GetUnit(key) : null;

        GameObject go = (unit != null && unit.generatedPrefab != null)
            ? Instantiate(unit.generatedPrefab, pos, Quaternion.identity)
            : BuildRuntimeEnemy(pos);
        if (go == null) return null;

        var ec = go.GetComponent<EnemyController>();
        if (ec == null) ec = go.AddComponent<EnemyController>();
        ec.SetUnitKey(key);   // unitStat은 지연 조회라 스폰 직후 지정으로 충분(Update 전)

        go.name = $"Enemy_{key}";
        return go;
    }

    /// <summary>프리팹이 없을 때의 그레이박스 적 (CombatSandboxBuilder.CreateEnemyRoot와 동일 구성).</summary>
    static GameObject BuildRuntimeEnemy(Vector3 pos)
    {
        var root = new GameObject("Enemy");
        root.transform.position = pos;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) root.layer = enemyLayer;

        // 물리(바디 콜라이더 — 통과 차단)
        var rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        var body = root.AddComponent<CapsuleCollider>();
        body.radius = 0.3f; body.height = 1.8f; body.center = new Vector3(0f, 0.9f, 0f); body.isTrigger = false;

        // 바디 스프라이트(붉은 틴트)
        var spriteGo = new GameObject("EnemySprite");
        spriteGo.transform.SetParent(root.transform, false);
        if (enemyLayer >= 0) spriteGo.layer = enemyLayer;
        var sr = spriteGo.AddComponent<SpriteRenderer>();
        sr.sprite = PlaceholderSprite.Square;
        sr.color = new Color(1f, 0.55f, 0.55f);
        sr.sortingOrder = 3;
        spriteGo.transform.localScale = new Vector3(0.8f, 1.0f, 1f);

        // 머리 위 "적"/"시체" 라벨은 EnemyController.Awake가 UnitLabel로 붙인다(프리팹 적도 동일 경로).

        // ⚠️ **맞을 몸(Health)을 허트박스보다 먼저 붙인다.** `AddComponent`는 그 자리에서
        //    `Awake()`를 부르므로, 순서가 뒤집히면 `Hurtbox.Awake`가 루트에서 Health를
        //    못 찾는다. 실제로 그 상태였고 **스폰된 적 전부가 때려도 데미지가 0**이었다.
        //    (Hurtbox 쪽도 지연 해석으로 고쳤지만, 원인을 여기서도 없애 둔다.)
        root.AddComponent<Health>();
        root.AddComponent<CombatFeedback>();

        // 허트박스(trigger, Enemy 레이어 — 플레이어 AttackPerformer가 스캔)
        var hurtGo = new GameObject("Hurtbox");
        hurtGo.transform.SetParent(root.transform, false);
        if (enemyLayer >= 0) hurtGo.layer = enemyLayer;
        var hb = hurtGo.AddComponent<BoxCollider>();
        // 허트박스는 **몸 전체를 덮는 상자** — 부위 판정이 높이로 갈리므로 키만큼 세워야 한다.
        hb.isTrigger = true; hb.size = new Vector3(0.7f, 1.6f, 0.7f); hb.center = new Vector3(0f, 0.85f, 0f);
        hurtGo.AddComponent<Hurtbox>();

        // 게임 로직 (EnemyController.playerMask 기본 1<<6 = Player)
        root.AddComponent<EnemyController>();
        return root;
    }
}
