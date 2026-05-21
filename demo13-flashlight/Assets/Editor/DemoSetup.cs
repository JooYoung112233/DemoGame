using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.AI.Navigation;
using UnityEditor;

/// <summary>
/// 전투 데모 원클릭 셋업.
/// Tools > Setup Demo (All-in-One) 하나로 URP + 씬 + 전투 모두 생성.
/// </summary>
public class DemoSetup : EditorWindow
{
    const float TILE_SIZE = 1f;
    const string SKELETON_PREFAB = "Assets/PixelArtStudio/SkeletonsPack/CommonSoldier/Prefabs/CommonSoldier.prefab";

    [MenuItem("Tools/Setup Demo (All-in-One)")]
    static void Setup()
    {
        if (!EditorUtility.DisplayDialog("Demo Setup (All-in-One)",
            "URP 설정 + 손전등 씬 + 해골 전투 데모를 한번에 셋업합니다.\n" +
            "(기존 씬 오브젝트를 먼저 삭제해주세요)\n\n계속?",
            "Yes", "Cancel"))
            return;

        // 프리팹 확인
        var skeletonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SKELETON_PREFAB);
        if (skeletonPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", $"해골 프리팹 없음: {SKELETON_PREFAB}", "OK");
            return;
        }

        // ===== Step 1: URP 설정 =====
        SetupURP();

        // ===== Step 2: 씬 구성 =====
        var dirLight = CreateDirectionalLight();
        var playerGO = CreatePlayer(skeletonPrefab);
        var flashCtrl = SetupFlashlight(playerGO);
        var cam = SetupCamera(playerGO);
        CreateAtmosphere();
        CreateDustParticles(playerGO);
        var dnCycle = CreateDayNightCycle(dirLight, flashCtrl);
        CreateHUD(dnCycle, flashCtrl);
        SetupRenderSettings();
        CreateBuilding();
        CreateNeonSign(dnCycle);
        CreateRoof(playerGO);

        // ===== Step 3: Data 생성 =====
        var combatData = GetOrCreateCombatData();
        var gameSettings = GetOrCreateGameSettings();

        // ===== Step 4: NavMesh 베이킹 =====
        BakeNavMesh();

        // ===== Step 5: 전투 셋업 (NavMeshAgent) =====
        SetupPlayerCombat(playerGO, combatData, gameSettings);
        SpawnEnemiesFromZones(skeletonPrefab, combatData, gameSettings);

        // ===== Step 6: 크로스헤어 + 게임매니저 =====
        SetupCrosshairAndManager(playerGO, combatData);

        Debug.Log("[Demo Setup] 전투 데모 올인원 셋업 완료!");
    }

    // ================================================================
    //  URP
    // ================================================================
    static void SetupURP()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");

        // 기존 에셋이 있으면 재사용
        var existingRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/ForwardRenderer.asset");
        var existingURP = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/URP-3D.asset");

        if (existingURP != null)
        {
            GraphicsSettings.defaultRenderPipeline = existingURP;
            QualitySettings.renderPipeline = existingURP;
            return;
        }

        var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
        AssetDatabase.CreateAsset(rendererData, "Assets/Settings/ForwardRenderer.asset");

        var urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
        var so = new SerializedObject(urpAsset);
        SetPropertyInt(so, "m_MainLightRenderingMode", 1);
        SetPropertyBool(so, "m_MainLightShadowsSupported", true);
        SetPropertyBool(so, "m_AdditionalLightShadowsSupported", true);
        SetPropertyInt(so, "m_AdditionalLightsRenderingMode", 1);
        SetPropertyInt(so, "m_MainLightShadowmapResolution", 2048);
        SetPropertyFloat(so, "m_ShadowDistance", 30f);
        so.ApplyModifiedProperties();

        AssetDatabase.CreateAsset(urpAsset, "Assets/Settings/URP-3D.asset");
        GraphicsSettings.defaultRenderPipeline = urpAsset;
        QualitySettings.renderPipeline = urpAsset;
        AssetDatabase.SaveAssets();
    }

    // ================================================================
    //  Directional Light
    // ================================================================
    static Light CreateDirectionalLight()
    {
        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0f;
        light.color = Color.black;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.8f;
        go.transform.rotation = Quaternion.Euler(50, -30, 0);
        Undo.RegisterCreatedObjectUndo(go, "Create DirLight");
        return light;
    }

    // ================================================================
    //  Player (해골 프리팹 사용)
    // ================================================================
    static GameObject CreatePlayer(GameObject skeletonPrefab)
    {
        var playerGO = new GameObject("Player");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(6, 0, 4);

        var playerAgent = playerGO.AddComponent<NavMeshAgent>();
        playerAgent.radius = 0.3f;
        playerAgent.height = 1.5f;
        playerAgent.baseOffset = 0f;
        playerAgent.speed = 5f;
        playerAgent.acceleration = 100f;
        playerAgent.angularSpeed = 0f;
        playerAgent.updateRotation = false;
        playerAgent.updateUpAxis = false;

        playerGO.AddComponent<PlayerController>();
        Undo.RegisterCreatedObjectUndo(playerGO, "Create Player");

        // 해골 프리팹 인스턴스 → 플레이어 자식
        var skeleton = (GameObject)PrefabUtility.InstantiatePrefab(skeletonPrefab);
        skeleton.name = "SkeletonSprite";
        skeleton.transform.SetParent(playerGO.transform);
        skeleton.transform.localPosition = new Vector3(0, 0.1f, 0);
        skeleton.transform.localScale = Vector3.one * 2f;

        // 플레이어 색상: 푸른 틴트
        var renderers = skeleton.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            if (r.gameObject.name == "shadow") continue;
            r.color = new Color(0.7f, 0.8f, 1f, 1f);
        }

        // SkeletonAnimController
        var animCtrl = skeleton.GetComponent<SkeletonAnimController>();
        if (animCtrl == null) animCtrl = skeleton.AddComponent<SkeletonAnimController>();

        return playerGO;
    }

    // ================================================================
    //  Flashlight
    // ================================================================
    static FlashlightController SetupFlashlight(GameObject playerGO)
    {
        var flashPivot = new GameObject("FlashlightPivot");
        flashPivot.transform.SetParent(playerGO.transform);
        flashPivot.transform.localPosition = new Vector3(0, 0.6f, 0);

        var spotGO = new GameObject("SpotLight");
        spotGO.transform.SetParent(flashPivot.transform);
        spotGO.transform.localPosition = Vector3.zero;
        var spot = spotGO.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.spotAngle = 90f;
        spot.innerSpotAngle = 50f;
        spot.range = 25f;
        spot.intensity = 15f;
        spot.color = new Color(1f, 0.93f, 0.75f);
        spot.shadows = LightShadows.Hard;
        spot.shadowStrength = 0.95f;
        spot.shadowNearPlane = 0.1f;

        var glowGO = new GameObject("AmbientGlow");
        glowGO.transform.SetParent(playerGO.transform);
        glowGO.transform.localPosition = new Vector3(0, 0.5f, 0);
        var glow = glowGO.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = 6f;
        glow.intensity = 2.5f;
        glow.color = new Color(0.6f, 0.65f, 0.8f);
        glow.shadows = LightShadows.Soft;

        var flashCtrl = playerGO.AddComponent<FlashlightController>();
        var flashSO = new SerializedObject(flashCtrl);
        flashSO.FindProperty("spotLight").objectReferenceValue = spot;
        flashSO.FindProperty("ambientGlow").objectReferenceValue = glow;
        flashSO.ApplyModifiedProperties();

        var playerCtrl = playerGO.GetComponent<PlayerController>();
        var playerSO = new SerializedObject(playerCtrl);
        playerSO.FindProperty("flashlightPivot").objectReferenceValue = flashPivot.transform;
        playerSO.ApplyModifiedProperties();

        return flashCtrl;
    }

    // ================================================================
    //  Camera
    // ================================================================
    static Camera SetupCamera(GameObject playerGO)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            cam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 7;
        cam.backgroundColor = Color.black;
        cam.transform.position = new Vector3(6, 10, 4 - 7);
        cam.transform.rotation = Quaternion.Euler(55, 0, 0);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 50f;

        var camData = cam.gameObject.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        var camFollow = cam.gameObject.GetComponent<CameraFollow>();
        if (camFollow == null) camFollow = cam.gameObject.AddComponent<CameraFollow>();
        var camSO = new SerializedObject(camFollow);
        camSO.FindProperty("target").objectReferenceValue = playerGO.transform;
        camSO.ApplyModifiedProperties();

        return cam;
    }

    // ================================================================
    //  CombatData
    // ================================================================
    static CombatData GetOrCreateCombatData()
    {
        const string path = "Assets/Settings/CombatData.asset";
        var data = AssetDatabase.LoadAssetAtPath<CombatData>(path);
        if (data == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");
            data = ScriptableObject.CreateInstance<CombatData>();
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
        }
        return data;
    }

    // ================================================================
    //  GameSettings
    // ================================================================
    static GameSettings GetOrCreateGameSettings()
    {
        const string path = "Assets/Settings/GameSettings.asset";
        var data = AssetDatabase.LoadAssetAtPath<GameSettings>(path);
        if (data == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");
            data = ScriptableObject.CreateInstance<GameSettings>();
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
        }
        return data;
    }

    // ================================================================
    //  NavMesh
    // ================================================================
    static void BakeNavMesh()
    {
        // 기존 NavMesh 오브젝트 제거
        var oldNav = GameObject.Find("NavMesh");
        if (oldNav != null) Object.DestroyImmediate(oldNav);

        // NavMesh용 바닥 콜라이더 (비주얼 타일과 별도)
        var navGO = new GameObject("NavMesh");
        Undo.RegisterCreatedObjectUndo(navGO, "Create NavMesh");

        // 넓은 바닥 평면 (건물 안팎 모두 커버)
        var ground = new GameObject("NavGround");
        ground.transform.SetParent(navGO.transform);
        ground.transform.position = new Vector3(6, -0.01f, 5);
        var groundCol = ground.AddComponent<BoxCollider>();
        groundCol.size = new Vector3(28, 0.02f, 24);
        // MeshRenderer 없음 → 안 보임, 콜라이더만

        // NavMeshSurface
        var surface = navGO.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

        // 에이전트 설정
        var settings = surface.GetBuildSettings();
        settings.agentRadius = 0.3f;
        settings.agentHeight = 1.5f;
        settings.agentSlope = 45f;
        settings.agentClimb = 0.3f;
        // BuildSettings는 struct이므로 다시 설정 필요
        // NavMeshSurface의 overrideVoxelSize 등으로 제어
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.1f;

        surface.BuildNavMesh();
        Debug.Log("[NavMesh] 베이킹 완료!");
    }

    // ================================================================
    //  Crosshair + GameManager
    // ================================================================
    static void SetupCrosshairAndManager(GameObject playerGO, CombatData combatData)
    {
        var old = GameObject.Find("GameManager");
        if (old != null) Object.DestroyImmediate(old);

        var managerGO = new GameObject("GameManager");
        Undo.RegisterCreatedObjectUndo(managerGO, "Create GameManager");

        // CrosshairUI
        var crosshair = managerGO.AddComponent<CrosshairUI>();

        // PlayerCombat에 CrosshairUI 연결
        var combat = playerGO.GetComponent<PlayerCombat>();
        if (combat != null)
        {
            var combatSO = new SerializedObject(combat);
            combatSO.FindProperty("crosshairUI").objectReferenceValue = crosshair;
            combatSO.ApplyModifiedProperties();
        }
    }

    // ================================================================
    //  Player Combat
    // ================================================================
    static void SetupPlayerCombat(GameObject playerGO, CombatData combatData, GameSettings gameSettings)
    {
        // Health
        var health = playerGO.GetComponent<Health>();
        if (health == null) health = playerGO.AddComponent<Health>();
        var healthSO = new SerializedObject(health);
        healthSO.FindProperty("maxHp").floatValue = combatData.player.maxHp;
        healthSO.ApplyModifiedProperties();

        // PlayerCombat
        var combat = playerGO.GetComponent<PlayerCombat>();
        if (combat == null) combat = playerGO.AddComponent<PlayerCombat>();
        var combatSO = new SerializedObject(combat);
        combatSO.FindProperty("attackDamage").floatValue = combatData.player.attackDamage;
        combatSO.FindProperty("attackRange").floatValue = combatData.player.attackRange;
        combatSO.FindProperty("attackSpeed").floatValue = combatData.player.attackSpeed;
        combatSO.FindProperty("combatData").objectReferenceValue = combatData;

        var animCtrl = playerGO.GetComponentInChildren<SkeletonAnimController>();
        var playerCtrl = playerGO.GetComponent<PlayerController>();
        combatSO.FindProperty("animController").objectReferenceValue = animCtrl;
        combatSO.FindProperty("playerController").objectReferenceValue = playerCtrl;
        combatSO.ApplyModifiedProperties();

        // PlayerController에 CombatData 연결
        var playerSO = new SerializedObject(playerCtrl);
        playerSO.FindProperty("combatData").objectReferenceValue = combatData;
        playerSO.ApplyModifiedProperties();

        // CameraFollow에 GameSettings 연결
        var cam = Camera.main;
        if (cam != null)
        {
            var camFollow = cam.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                var cfSO = new SerializedObject(camFollow);
                cfSO.FindProperty("gameSettings").objectReferenceValue = gameSettings;
                cfSO.ApplyModifiedProperties();
            }
        }

        // HP Bar
        var existingBar = playerGO.transform.Find("PlayerHPBar");
        if (existingBar != null) Object.DestroyImmediate(existingBar.gameObject);

        var hpBarGO = new GameObject("PlayerHPBar");
        hpBarGO.transform.SetParent(playerGO.transform);
        hpBarGO.transform.localPosition = new Vector3(0, 1.5f, 0);
        var hpBar = hpBarGO.AddComponent<HealthBar3D>();
        var hpBarSO = new SerializedObject(hpBar);
        hpBarSO.FindProperty("health").objectReferenceValue = health;
        hpBarSO.FindProperty("offset").vector3Value = new Vector3(0, 1.5f, 0);
        hpBarSO.FindProperty("fullColor").colorValue = Color.green;
        hpBarSO.FindProperty("lowColor").colorValue = Color.red;
        hpBarSO.ApplyModifiedProperties();
    }

    // ================================================================
    //  Enemies
    // ================================================================
    static void SpawnEnemiesFromZones(GameObject prefab, CombatData combatData, GameSettings gameSettings)
    {
        var oldEnemies = GameObject.Find("Enemies");
        if (oldEnemies != null) Object.DestroyImmediate(oldEnemies);

        var oldZones = GameObject.Find("SpawnZones");
        if (oldZones != null) Object.DestroyImmediate(oldZones);

        var enemyRoot = new GameObject("Enemies");
        Undo.RegisterCreatedObjectUndo(enemyRoot, "Create Enemies");

        var zonesRoot = new GameObject("SpawnZones");
        Undo.RegisterCreatedObjectUndo(zonesRoot, "Create SpawnZones");

        if (gameSettings.spawnZones == null || gameSettings.spawnZones.Length == 0)
        {
            // 기본 스폰존 사용
            gameSettings.spawnZones = new GameSettings.SpawnZoneData[]
            {
                new GameSettings.SpawnZoneData { position = new Vector3(3, 0, 7), size = new Vector3(4, 0, 4), enemyCount = 2 },
                new GameSettings.SpawnZoneData { position = new Vector3(10, 0, 4), size = new Vector3(3, 0, 3), enemyCount = 1 },
            };
            EditorUtility.SetDirty(gameSettings);
        }

        int enemyIdx = 0;
        for (int z = 0; z < gameSettings.spawnZones.Length; z++)
        {
            var zoneData = gameSettings.spawnZones[z];

            // SpawnZone 오브젝트 생성
            var zoneGO = new GameObject($"SpawnZone_{z}");
            zoneGO.transform.SetParent(zonesRoot.transform);
            zoneGO.transform.position = zoneData.position;
            var zone = zoneGO.AddComponent<SpawnZone>();
            var zoneSO = new SerializedObject(zone);
            zoneSO.FindProperty("size").vector3Value = zoneData.size;
            zoneSO.FindProperty("enemyCount").intValue = zoneData.enemyCount;
            if (zoneData.enemyType != null)
                zoneSO.FindProperty("enemyType").objectReferenceValue = zoneData.enemyType;
            zoneSO.ApplyModifiedProperties();

            // 존 내 적 스폰
            for (int e = 0; e < zoneData.enemyCount; e++)
            {
                Vector3 spawnPos = zoneData.position + new Vector3(
                    Random.Range(-zoneData.size.x * 0.5f, zoneData.size.x * 0.5f),
                    0,
                    Random.Range(-zoneData.size.z * 0.5f, zoneData.size.z * 0.5f)
                );
                CreateEnemy(enemyRoot, prefab, $"Skeleton_{enemyIdx}", spawnPos, combatData, zoneData.enemyType);
                enemyIdx++;
            }
        }

        Debug.Log($"[Spawn] {gameSettings.spawnZones.Length}개 존에서 적 {enemyIdx}마리 생성!");
    }

    static void CreateEnemy(GameObject parent, GameObject prefab, string name, Vector3 position,
        CombatData combatData, EnemyData enemyData = null)
    {
        var enemyGO = new GameObject(name);
        enemyGO.transform.SetParent(parent.transform);
        enemyGO.transform.position = position;

        // 스탯 결정: EnemyData 우선, 없으면 CombatData 폴백
        float spd = enemyData != null ? enemyData.moveSpeed : combatData.enemy.moveSpeed;
        float hp = enemyData != null ? enemyData.maxHp : combatData.enemy.maxHp;
        float scale = enemyData != null ? enemyData.scale : 2f;
        Color tint = enemyData != null ? enemyData.tintColor : new Color(1f, 0.7f, 0.7f, 1f);

        // NavMeshAgent
        var agent = enemyGO.AddComponent<NavMeshAgent>();
        agent.radius = 0.3f;
        agent.height = 1.5f;
        agent.baseOffset = 0f;
        agent.speed = spd;
        agent.acceleration = 50f;
        agent.angularSpeed = 0f;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.stoppingDistance = 0.3f;

        var skeleton = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        skeleton.name = "SkeletonSprite";
        skeleton.transform.SetParent(enemyGO.transform);
        skeleton.transform.localPosition = new Vector3(0, 0.1f, 0);
        skeleton.transform.localScale = Vector3.one * scale;

        // 적 색상: EnemyData의 tintColor 사용
        var renderers = skeleton.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            if (r.gameObject.name == "shadow") continue;
            r.color = tint;
        }

        var animCtrl = skeleton.GetComponent<SkeletonAnimController>();
        if (animCtrl == null) animCtrl = skeleton.AddComponent<SkeletonAnimController>();

        var health = enemyGO.AddComponent<Health>();
        var healthSO = new SerializedObject(health);
        healthSO.FindProperty("maxHp").floatValue = hp;
        healthSO.ApplyModifiedProperties();

        var ai = enemyGO.AddComponent<EnemyAI>();
        var aiSO = new SerializedObject(ai);
        aiSO.FindProperty("animController").objectReferenceValue = animCtrl;
        aiSO.FindProperty("combatData").objectReferenceValue = combatData;
        if (enemyData != null)
            aiSO.FindProperty("enemyData").objectReferenceValue = enemyData;
        aiSO.FindProperty("detectRange").floatValue = enemyData != null ? enemyData.detectRange : combatData.enemy.detectRange;
        aiSO.FindProperty("attackRange").floatValue = enemyData != null ? enemyData.attackRange : combatData.enemy.attackRange;
        aiSO.FindProperty("moveSpeed").floatValue = spd;
        aiSO.FindProperty("attackDamage").floatValue = enemyData != null ? enemyData.attackDamage : combatData.enemy.attackDamage;
        aiSO.FindProperty("attackSpeed").floatValue = enemyData != null ? enemyData.attackSpeed : combatData.enemy.attackSpeed;
        aiSO.ApplyModifiedProperties();

        // EnemyOutline
        enemyGO.AddComponent<EnemyOutline>();

        // HP바
        var hpBarGO = new GameObject("HPBar");
        hpBarGO.transform.SetParent(enemyGO.transform);
        hpBarGO.transform.localPosition = new Vector3(0, 1.5f, 0);
        var hpBar = hpBarGO.AddComponent<HealthBar3D>();
        var hpBarSO = new SerializedObject(hpBar);
        hpBarSO.FindProperty("health").objectReferenceValue = health;
        hpBarSO.FindProperty("offset").vector3Value = new Vector3(0, 1.5f, 0);
        hpBarSO.FindProperty("fullColor").colorValue = new Color(1f, 0.3f, 0.3f);
        hpBarSO.FindProperty("lowColor").colorValue = new Color(0.5f, 0f, 0f);
        hpBarSO.ApplyModifiedProperties();
    }

    // ================================================================
    //  Atmosphere / PostProcessing
    // ================================================================
    static void CreateAtmosphere()
    {
        var volumeGO = new GameObject("PostProcessVolume");
        var volume = volumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1;
        Undo.RegisterCreatedObjectUndo(volumeGO, "Create PostProcess");

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = profile;

        var vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(0.35f);
        vignette.smoothness.Override(0.3f);
        vignette.color.Override(Color.black);

        var colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.active = true;
        colorAdj.saturation.Override(-30f);
        colorAdj.contrast.Override(15f);
        colorAdj.colorFilter.Override(new Color(0.85f, 0.9f, 1f));

        var bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.threshold.Override(0.8f);
        bloom.intensity.Override(0.5f);
        bloom.scatter.Override(0.6f);
        bloom.tint.Override(new Color(0.9f, 0.85f, 0.7f));

        var grain = profile.Add<FilmGrain>();
        grain.active = true;
        grain.type.Override(FilmGrainLookup.Medium3);
        grain.intensity.Override(0.4f);
        grain.response.Override(0.6f);

        var lgg = profile.Add<LiftGammaGain>();
        lgg.active = true;
        lgg.lift.Override(new Vector4(-0.05f, -0.05f, -0.02f, 0f));
        lgg.gamma.Override(new Vector4(0f, -0.02f, 0.02f, 0f));
        lgg.gain.Override(new Vector4(0.05f, 0.02f, -0.03f, 0f));
    }

    static void CreateDustParticles(GameObject player)
    {
        var dustGO = new GameObject("DustParticles");
        dustGO.transform.SetParent(player.transform);
        dustGO.transform.localPosition = new Vector3(0, 2f, 0);
        Undo.RegisterCreatedObjectUndo(dustGO, "Create Dust");

        var ps = dustGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.maxParticles = 80;
        main.startLifetime = 6f;
        main.startSpeed = 0.05f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = new Color(0.7f, 0.65f, 0.55f, 0.3f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.01f;

        var emission = ps.emission;
        emission.rateOverTime = 12f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(8f, 3f, 8f);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] {
                new GradientColorKey(new Color(0.7f, 0.65f, 0.55f), 0f),
                new GradientColorKey(new Color(0.7f, 0.65f, 0.55f), 1f)
            },
            new[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.3f, 0.3f),
                new GradientAlphaKey(0.3f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var renderer = dustGO.GetComponent<ParticleSystemRenderer>();
        var dustMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        dustMat.SetColor("_BaseColor", new Color(0.7f, 0.65f, 0.55f, 0.3f));
        renderer.sharedMaterial = dustMat;
    }

    // ================================================================
    //  DayNight / HUD
    // ================================================================
    static DayNightCycle CreateDayNightCycle(Light dirLight, FlashlightController flashCtrl)
    {
        var go = new GameObject("DayNightCycle");
        var dnCycle = go.AddComponent<DayNightCycle>();
        var so = new SerializedObject(dnCycle);
        so.FindProperty("directionalLight").objectReferenceValue = dirLight;
        so.FindProperty("flashlight").objectReferenceValue = flashCtrl;
        so.ApplyModifiedProperties();
        return dnCycle;
    }

    static void CreateHUD(DayNightCycle dnCycle, FlashlightController flashCtrl)
    {
        var go = new GameObject("PrototypeHUD");
        var hud = go.AddComponent<PrototypeHUD>();
        var so = new SerializedObject(hud);
        so.FindProperty("dayNight").objectReferenceValue = dnCycle;
        so.FindProperty("flashlight").objectReferenceValue = flashCtrl;
        so.ApplyModifiedProperties();
    }

    // ================================================================
    //  Render Settings
    // ================================================================
    static void SetupRenderSettings()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.04f, 0.04f, 0.06f);
        RenderSettings.ambientSkyColor = new Color(0.03f, 0.03f, 0.05f);
        RenderSettings.ambientEquatorColor = new Color(0.02f, 0.02f, 0.03f);
        RenderSettings.ambientGroundColor = Color.black;
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.skybox = null;
        RenderSettings.subtractiveShadowColor = Color.black;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.03f);
        RenderSettings.fogDensity = 0.04f;
    }

    // ================================================================
    //  Building (Floor + Walls)
    // ================================================================
    static void CreateBuilding()
    {
        var root = new GameObject("Building");
        Undo.RegisterCreatedObjectUndo(root, "Create Building");

        int bw = 12, bh = 10;
        float wallHeight = 2.5f;

        Material floorMat = CreateTextureMaterial(CreateConcreteFloorTex(), "FloorMat");
        Material roadMat = CreateTextureMaterial(CreateAsphaltTex(), "RoadMat");
        Material wallMat = CreateTextureMaterial(CreateBrickWallTex(), "WallMat");

        // 바깥 바닥 (도로)
        var outsideParent = new GameObject("OutsideGround");
        outsideParent.transform.SetParent(root.transform);
        for (int x = -4; x <= bw + 4; x++)
            for (int z = -4; z <= bh + 4; z++)
            {
                if (x >= 1 && x < bw && z >= 1 && z < bh) continue;
                CreateFloorTile(outsideParent, $"R_{x}_{z}", x, z, roadMat);
            }

        // 건물 내부 바닥
        var floorParent = new GameObject("Floor");
        floorParent.transform.SetParent(root.transform);
        for (int x = 1; x < bw; x++)
            for (int z = 1; z < bh; z++)
                CreateFloorTile(floorParent, $"F_{x}_{z}", x, z, floorMat);

        // 벽
        var wallParent = new GameObject("Walls");
        wallParent.transform.SetParent(root.transform);

        for (int x = 0; x <= bw; x++)
            CreateWall(wallParent, x, bh, wallHeight, wallMat, WallDir.ZWall);
        for (int x = 0; x <= bw; x++)
        {
            if (x >= 5 && x <= 7) continue;
            CreateWall(wallParent, x, 0, wallHeight, wallMat, WallDir.ZWall);
        }
        for (int z = 0; z <= bh; z++)
            CreateWall(wallParent, 0, z, wallHeight, wallMat, WallDir.XWall);
        for (int z = 0; z <= bh; z++)
            CreateWall(wallParent, bw, z, wallHeight, wallMat, WallDir.XWall);
    }

    // ================================================================
    //  Neon Sign
    // ================================================================
    static void CreateNeonSign(DayNightCycle dnCycle)
    {
        var signGO = new GameObject("NeonSign");
        signGO.transform.position = new Vector3(6.5f, 2.8f, 0.6f);
        Undo.RegisterCreatedObjectUndo(signGO, "Create NeonSign");

        var signQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        signQuad.name = "SignFace";
        signQuad.transform.SetParent(signGO.transform);
        signQuad.transform.localPosition = Vector3.zero;
        signQuad.transform.localScale = new Vector3(2.5f, 0.6f, 1);
        Object.DestroyImmediate(signQuad.GetComponent<MeshCollider>());

        var neonMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        neonMat.name = "NeonMat";
        Color neonColor = new Color(1f, 0.15f, 0.25f);
        neonMat.SetColor("_BaseColor", neonColor);
        neonMat.SetColor("_EmissionColor", neonColor * 2f);
        neonMat.EnableKeyword("_EMISSION");
        neonMat.SetFloat("_Smoothness", 0.9f);
        signQuad.GetComponent<MeshRenderer>().sharedMaterial = neonMat;
        signQuad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        var textQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        textQuad.name = "SignText";
        textQuad.transform.SetParent(signGO.transform);
        textQuad.transform.localPosition = new Vector3(0, 0, -0.01f);
        textQuad.transform.localScale = new Vector3(2f, 0.4f, 1);
        Object.DestroyImmediate(textQuad.GetComponent<MeshCollider>());
        var textMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        textMat.name = "NeonTextMat";
        textMat.mainTexture = CreateNeonTexture();
        textMat.SetColor("_EmissionColor", Color.white * 3f);
        textMat.EnableKeyword("_EMISSION");
        textMat.SetFloat("_Surface", 1);
        textMat.SetFloat("_AlphaClip", 1);
        textMat.SetFloat("_Cutoff", 0.5f);
        textMat.EnableKeyword("_ALPHATEST_ON");
        textMat.renderQueue = 2460;
        textQuad.GetComponent<MeshRenderer>().sharedMaterial = textMat;
        textQuad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        var neonLightGO = new GameObject("NeonLight");
        neonLightGO.transform.SetParent(signGO.transform);
        neonLightGO.transform.localPosition = new Vector3(0, -0.5f, -1.5f);
        var neonLight = neonLightGO.AddComponent<Light>();
        neonLight.type = LightType.Point;
        neonLight.range = 8f;
        neonLight.intensity = 8f;
        neonLight.color = new Color(1f, 0.2f, 0.3f);
        neonLight.shadows = LightShadows.Soft;

        var neonComp = signGO.AddComponent<NeonSign>();
        var neonSO = new SerializedObject(neonComp);
        neonSO.FindProperty("neonLight").objectReferenceValue = neonLight;
        neonSO.FindProperty("signRenderer").objectReferenceValue = signQuad.GetComponent<MeshRenderer>();
        neonSO.FindProperty("dayNight").objectReferenceValue = dnCycle;
        neonSO.FindProperty("neonColor").colorValue = new Color(1f, 0.15f, 0.25f);
        neonSO.FindProperty("baseIntensity").floatValue = 8f;
        neonSO.ApplyModifiedProperties();
    }

    // ================================================================
    //  Roof
    // ================================================================
    static void CreateRoof(GameObject player)
    {
        int bw = 12, bh = 10;
        float roofY = 2.7f;

        var roofParent = new GameObject("Roof");
        Undo.RegisterCreatedObjectUndo(roofParent, "Create Roof");

        var roofMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        roofMat.name = "RoofMat";
        Color roofColor = new Color(0.15f, 0.13f, 0.12f, 1f);
        roofMat.SetColor("_BaseColor", roofColor);
        roofMat.SetFloat("_Surface", 1);
        roofMat.SetFloat("_Blend", 0);
        roofMat.SetOverrideTag("RenderType", "Transparent");
        roofMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        roofMat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        roofMat.SetFloat("_ZWrite", 0);
        roofMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        roofMat.renderQueue = 3000;
        roofMat.SetFloat("_Smoothness", 0.2f);

        for (int x = 0; x <= bw; x++)
            for (int z = 0; z <= bh; z++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.name = $"Roof_{x}_{z}";
                tile.transform.SetParent(roofParent.transform);
                tile.transform.position = new Vector3(x + 0.5f, roofY, z + 0.5f);
                tile.transform.rotation = Quaternion.Euler(90, 0, 0);
                tile.transform.localScale = new Vector3(TILE_SIZE + 0.02f, TILE_SIZE + 0.02f, 1);
                tile.GetComponent<MeshRenderer>().sharedMaterial = roofMat;
                tile.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                tile.GetComponent<MeshRenderer>().receiveShadows = false;
                Object.DestroyImmediate(tile.GetComponent<MeshCollider>());
            }

        var roofCtrl = roofParent.AddComponent<RoofController>();
        var roofSO = new SerializedObject(roofCtrl);
        roofSO.FindProperty("player").objectReferenceValue = player.transform;
        roofSO.FindProperty("roofObject").objectReferenceValue = roofParent;
        roofSO.FindProperty("buildingMin").vector3Value = new Vector3(0.5f, 0, 0.5f);
        roofSO.FindProperty("buildingMax").vector3Value = new Vector3(bw + 0.5f, 0, bh + 0.5f);
        roofSO.ApplyModifiedProperties();
    }

    // ================================================================
    //  Helpers
    // ================================================================
    enum WallDir { XWall, ZWall }

    static void CreateFloorTile(GameObject parent, string name, int x, int z, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(parent.transform);
        float tileOverlap = TILE_SIZE + 0.02f;
        go.transform.position = new Vector3(x + 0.5f, 0, z + 0.5f);
        go.transform.rotation = Quaternion.Euler(90, 0, 0);
        go.transform.localScale = new Vector3(tileOverlap, tileOverlap, 1);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        go.GetComponent<MeshRenderer>().receiveShadows = true;
        Object.DestroyImmediate(go.GetComponent<MeshCollider>());
    }

    static void CreateWall(GameObject parent, int x, int z, float height, Material mat, WallDir dir)
    {
        float wallOverlap = TILE_SIZE + 0.05f;
        float wallThickness = 0.15f;

        var wallGO = new GameObject($"W_{x}_{z}");
        wallGO.transform.SetParent(parent.transform);
        wallGO.transform.position = new Vector3(x + 0.5f, height * 0.5f, z + 0.5f);

        var wallCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallCube.name = "Visual";
        wallCube.transform.SetParent(wallGO.transform);
        wallCube.transform.localPosition = Vector3.zero;
        wallCube.transform.localScale = new Vector3(
            dir == WallDir.ZWall ? wallOverlap : wallThickness,
            height,
            dir == WallDir.XWall ? wallOverlap : wallThickness
        );
        wallCube.GetComponent<MeshRenderer>().sharedMaterial = mat;
        wallCube.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
        wallCube.GetComponent<MeshRenderer>().receiveShadows = true;
    }

    // ================================================================
    //  Textures
    // ================================================================
    static Texture2D CreateNeonTexture()
    {
        int w = 128, h = 32;
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Point };
        Color clear = new Color(0, 0, 0, 0);
        Color glow = Color.white;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, clear);

        int[,] letters = {
            {12,8},{13,8},{14,8},{15,8},{16,8},{17,8},{18,8},{19,8},{20,8},
            {11,9},{12,9},{20,9},{21,9},{10,10},{11,10},{21,10},{22,10},
            {10,12},{11,12},{21,12},{22,12},{10,14},{11,14},{21,14},{22,14},
            {10,16},{11,16},{21,16},{22,16},{10,18},{11,18},{21,18},{22,18},
            {10,20},{11,20},{21,20},{22,20},{11,21},{12,21},{20,21},{21,21},
            {12,22},{13,22},{14,22},{15,22},{16,22},{17,22},{18,22},{19,22},{20,22},
            {28,8},{29,8},{30,8},{31,8},{32,8},{33,8},{34,8},{35,8},{36,8},
            {28,9},{29,9},{36,9},{37,9},{28,10},{29,10},{37,10},{38,10},
            {28,12},{29,12},{37,12},{38,12},{28,14},{29,14},{36,14},{37,14},
            {28,15},{29,15},{30,15},{31,15},{32,15},{33,15},{34,15},{35,15},{36,15},
            {28,18},{29,18},{28,20},{29,20},{28,22},{29,22},
            {44,8},{45,8},{46,8},{47,8},{48,8},{49,8},{50,8},{51,8},{52,8},{53,8},{54,8},
            {44,9},{45,9},{44,10},{45,10},{44,12},{45,12},
            {44,14},{45,14},{46,14},{47,14},{48,14},{49,14},{50,14},
            {44,16},{45,16},{44,18},{45,18},{44,20},{45,20},
            {44,22},{45,22},{46,22},{47,22},{48,22},{49,22},{50,22},{51,22},{52,22},{53,22},{54,22},
            {60,8},{61,8},{72,8},{73,8},{60,10},{61,10},{62,10},{72,10},{73,10},
            {60,12},{61,12},{63,12},{64,12},{72,12},{73,12},
            {60,14},{61,14},{65,14},{66,14},{72,14},{73,14},
            {60,16},{61,16},{67,16},{68,16},{72,16},{73,16},
            {60,18},{61,18},{69,18},{70,18},{72,18},{73,18},
            {60,20},{61,20},{71,20},{72,20},{73,20},{60,22},{61,22},{72,22},{73,22},
        };

        for (int i = 0; i < letters.GetLength(0); i++)
        {
            int px = letters[i, 0], py = letters[i, 1];
            if (px >= 0 && px < w && py >= 0 && py < h)
            {
                tex.SetPixel(px, py, glow);
                if (px + 1 < w) tex.SetPixel(px + 1, py, glow);
                if (py + 1 < h) tex.SetPixel(px, py + 1, glow);
            }
        }
        tex.Apply();
        return tex;
    }

    static Material CreateTextureMaterial(Texture2D tex, string name)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = name;
        mat.mainTexture = tex;
        mat.SetFloat("_Smoothness", 0.1f);
        mat.SetFloat("_Cull", 0);
        mat.SetColor("_EmissionColor", Color.black);
        mat.SetFloat("_EnvironmentReflections", 0f);
        mat.SetFloat("_SpecularHighlights", 0f);
        mat.DisableKeyword("_EMISSION");
        mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        return mat;
    }

    static Texture2D CreateAsphaltTex()
    {
        int s = 64;
        var tex = new Texture2D(s, s) { filterMode = FilterMode.Point };
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.06f;
                float v = 0.12f + n;
                tex.SetPixel(x, y, new Color(v, v, v + 0.005f));
            }
        tex.Apply();
        return tex;
    }

    static Texture2D CreateConcreteFloorTex()
    {
        int s = 64;
        var tex = new Texture2D(s, s) { filterMode = FilterMode.Point };
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.2f + 50, y * 0.2f + 50) * 0.06f;
                float v = 0.22f + n;
                bool grid = (x % 32 == 0 || y % 32 == 0);
                Color c = grid ? new Color(v - 0.04f, v - 0.04f, v - 0.03f)
                               : new Color(v, v - 0.01f, v - 0.02f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return tex;
    }

    static Texture2D CreateBrickWallTex()
    {
        int w = 64, h = 64;
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        Color brickBase = new Color(0.32f, 0.22f, 0.18f);
        Color mortarColor = new Color(0.18f, 0.16f, 0.14f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int brickRow = y / 8;
                int offset = (brickRow % 2 == 0) ? 0 : 8;
                int brickCol = (x + offset) % 16;
                bool mortar = (y % 8 == 0) || (brickCol == 0);
                float n = Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.06f;
                Color c = mortar ? mortarColor
                    : new Color(brickBase.r + n, brickBase.g + n * 0.7f, brickBase.b + n * 0.4f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return tex;
    }

    // SerializedObject helpers
    static void SetPropertyInt(SerializedObject so, string name, int val)
    { var p = so.FindProperty(name); if (p != null) p.intValue = val; }
    static void SetPropertyFloat(SerializedObject so, string name, float val)
    { var p = so.FindProperty(name); if (p != null) p.floatValue = val; }
    static void SetPropertyBool(SerializedObject so, string name, bool val)
    { var p = so.FindProperty(name); if (p != null) p.boolValue = val; }
}
