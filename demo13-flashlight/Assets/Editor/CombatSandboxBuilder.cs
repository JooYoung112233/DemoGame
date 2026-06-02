using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;
using System.IO;

/// <summary>
/// 전투 샌드박스(타격감/히트박스 테스트) 씬 + 적 프리팹 생성기.
/// - Build Enemy Prefab: Resources/Enemy.prefab (바디 스프라이트+Hurtbox+Health+CombatFeedback+EnemyController)
///   → "적을 코드로 스폰하는 곳이 없어 바디 스프라이트가 없던" 공백을 메움.
/// - Build Combat Sandbox: 카메라/조명/Volume/SpawnPoint + 적 3기를 앞에 둔 아레나 씬.
/// 메뉴: Tools ▸ TopDown 2D ▸ Build Enemy Prefab / Tools ▸ BRB ▸ Build Scene ▸ Combat Sandbox
/// </summary>
public static class CombatSandboxBuilder
{
    const string ENEMY_PREFAB_PATH = "Assets/Resources/Enemy.prefab";
    const string SCENE_PATH        = "Assets/Scenes/CombatSandbox.unity";
    const string VOLUME_PROFILE    = "Assets/Settings/SandboxVolume.asset";
    // 플레이어와 동일 스프라이트 재사용(빨간 틴트로 구분)
    const string SPRITE_GUID = "f8bd92d6d061f7143986c16a0ea86602";

    // ══════════════════════════════════════════════════════════
    //  적 프리팹
    // ══════════════════════════════════════════════════════════

    [MenuItem("Tools/TopDown 2D/Build Enemy Prefab")]
    public static void BuildEnemyPrefab()
    {
        EnsureFolder("Assets/Resources");
        var root = CreateEnemyRoot();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, ENEMY_PREFAB_PATH, out bool ok);
        Object.DestroyImmediate(root);

        if (ok)
        {
            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"<color=orange>[Enemy]</color> 프리팹 생성: {ENEMY_PREFAB_PATH}");
        }
        else Debug.LogError("[Enemy] 프리팹 저장 실패");
    }

    static GameObject CreateEnemyRoot()
    {
        var root = new GameObject("Enemy");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) root.layer = enemyLayer;
        else Debug.LogWarning("[Enemy] 'Enemy' 레이어가 없음. TagManager에 Player(6)/Enemy(9) 레이어를 추가하세요.");

        // 물리(바디 콜라이더 — 통과 차단)
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; rb.freezeRotation = true;
        var body = root.AddComponent<CircleCollider2D>();
        body.radius = 0.3f; body.isTrigger = false;

        // 바디 스프라이트(붉은 틴트) — HitFlash 대상 / flip / 틴트
        var spriteGo = new GameObject("EnemySprite");
        spriteGo.transform.SetParent(root.transform, false);
        if (enemyLayer >= 0) spriteGo.layer = enemyLayer;
        var sr = spriteGo.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 0;
        sr.color = new Color(1f, 0.55f, 0.55f);
        var path = AssetDatabase.GUIDToAssetPath(SPRITE_GUID);
        if (!string.IsNullOrEmpty(path)) sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        // 허트박스(trigger, Enemy 레이어 — 플레이어 AttackPerformer가 스캔)
        var hurtGo = new GameObject("Hurtbox");
        hurtGo.transform.SetParent(root.transform, false);
        if (enemyLayer >= 0) hurtGo.layer = enemyLayer;
        var hbCol = hurtGo.AddComponent<BoxCollider2D>();
        hbCol.isTrigger = true; hbCol.size = new Vector2(0.7f, 1.0f);
        hurtGo.AddComponent<Hurtbox>();

        // 게임 로직
        root.AddComponent<Health>();
        root.AddComponent<CombatFeedback>();   // HitFlash 자동 부착
        var enemy = root.AddComponent<EnemyController>();

        // playerMask = Player 레이어
        int playerLayer = LayerMask.NameToLayer("Player");
        var eSo = new SerializedObject(enemy);
        var pm = eSo.FindProperty("playerMask");
        if (pm != null && playerLayer >= 0) pm.intValue = 1 << playerLayer;
        eSo.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    // ══════════════════════════════════════════════════════════
    //  샌드박스 씬
    // ══════════════════════════════════════════════════════════

    [MenuItem("Tools/BRB/Build Scene/Combat Sandbox")]
    public static void BuildSandbox()
    {
        // 적 프리팹 보장
        var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_PREFAB_PATH);
        if (enemyPrefab == null)
        {
            BuildEnemyPrefab();
            enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_PREFAB_PATH);
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── 카메라 ──────────────────────────────────────────
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.06f, 0.08f);
        cam.transparencySortMode = TransparencySortMode.CustomAxis;
        cam.transparencySortAxis = new Vector3(0, 1, 0);
        var camData = cam.GetUniversalAdditionalCameraData();
        if (camData != null) camData.renderPostProcessing = true;
        camGo.AddComponent<CameraFollow>();

        // ── 전역 조명(밝게 — 테스트용) ──────────────────────
        var lightGo = new GameObject("Global Light2D");
        var gl = lightGo.AddComponent<Light2D>();
        gl.lightType = Light2D.LightType.Global;
        gl.intensity = 1f;
        gl.color = Color.white;

        // ── EventSystem ─────────────────────────────────────
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // ── post-process Volume(피격 비네트/색수차) ─────────
        CreateSandboxVolume();

        // ── SpawnPoint(플레이어 자동 스폰) ──────────────────
        var spawnGo = new GameObject("SpawnPoint");
        spawnGo.transform.position = Vector3.zero;
        spawnGo.AddComponent<SpawnPoint>();

        // ── 적 3기(앞쪽에 배치) ─────────────────────────────
        if (enemyPrefab != null)
        {
            for (int i = 0; i < 3; i++)
            {
                var e = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
                e.transform.position = new Vector3(3f + i * 1.5f, (i % 2 == 0) ? 0.5f : -0.5f, 0f);
                e.name = $"Enemy_{i + 1}";
            }
        }
        else Debug.LogWarning("[Sandbox] Enemy.prefab 로드 실패 — 적 미배치");

        // ── 저장 ────────────────────────────────────────────
        EnsureFolder("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=cyan>[Sandbox]</color> 전투 샌드박스 생성: {SCENE_PATH}\n" +
                  "Play 하면 플레이어 자동 스폰 + 적 3기. 좌클릭=약공, 우클릭홀드=강공, Space=구르기.");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Combat Sandbox",
                "전투 샌드박스 씬 생성 완료.\n\n" +
                "Play → 플레이어 자동 스폰, 적 3기.\n" +
                "강공으로 적 때리면 흰 플래시+히트스탑+카메라 셰이크,\n" +
                "적에게 맞으면 화면 연출이 나옵니다.", "확인");
    }

    static void CreateSandboxVolume()
    {
        EnsureFolder("Assets/Settings");

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VOLUME_PROFILE);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VOLUME_PROFILE);

            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0f);     // 평소 0 → PlayerHitReaction이 펄스
            vig.color.Override(new Color(0.6f, 0f, 0f));

            var ca = profile.Add<ChromaticAberration>(true);
            ca.intensity.Override(0f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        var volGo = new GameObject("PostProcess Volume");
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.profile = profile;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path).Replace("\\", "/");
        var folder = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
