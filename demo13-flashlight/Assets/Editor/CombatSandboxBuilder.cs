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
/// - Build Combat Sandbox: 순수 hit 테스트 아레나 — SpawnPoint + 적 3기 + NavGrid + 우회벽 + 약한 전역광.
///   카메라·플레이어 라이트·후처리(Volume)는 PlayerRig(자동 스폰)가 한 세트로 들고 오므로 씬에 안 둠.
/// 메뉴: Tools ▸ TopDown ▸ Build ▸ Enemy Prefab / Combat Sandbox Scene
/// </summary>
public static class CombatSandboxBuilder
{
    const string ENEMY_PREFAB_PATH = "Assets/Resources/Enemy.prefab";
    const string SCENE_PATH        = "Assets/Scenes/CombatSandbox.unity";
    // 플레이어와 동일 스프라이트 재사용(빨간 틴트로 구분)
    const string SPRITE_GUID = "f8bd92d6d061f7143986c16a0ea86602";

    // ══════════════════════════════════════════════════════════
    //  적 프리팹
    // ══════════════════════════════════════════════════════════

    [MenuItem("Tools/TopDown/개발/적 프리팹")]
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

    [MenuItem("Tools/TopDown/개발/전투 샌드박스 씬")]
    public static void BuildSandbox()
    {
        // 적 프리팹 보장
        var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_PREFAB_PATH);
        if (enemyPrefab == null)
        {
            BuildEnemyPrefab();
            enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_PREFAB_PATH);
        }

        var scene = EditorSceneBuildUtil.NewDetachedScene(out var prevActive);  // 현재 씬 유지(폴더에만 생성)

        // ⚠️ 카메라·라이트·후처리(Volume)는 만들지 않는다 — PlayerRig(자동 스폰)가 한 세트로 들고 옴.
        //    (씬에 카메라/Volume을 또 두면 PlayerRig 것과 충돌.)

        // ── 전역 조명: 테스트 가시성용 약한 앰비언트만 ───────
        //    PlayerRig가 플레이어 주변광을 들고 오므로, 여긴 멀리 있는 적도 보이게 약하게만.
        var lightGo = new GameObject("Global Light2D");
        var gl = lightGo.AddComponent<Light2D>();
        gl.lightType = Light2D.LightType.Global;
        gl.intensity = 0.5f;
        gl.color = new Color(0.7f, 0.72f, 0.8f);

        // ── EventSystem (UI 입력) ───────────────────────────
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
        }

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

        // ── 길찾기 격자 + 장애물(우회 테스트용) ──────────────
        var navGo = new GameObject("NavGrid");
        navGo.AddComponent<NavGrid>();   // 기본 60x60 / cell 0.5 / agentRadius 0.35, Start에서 베이크

        // 플레이어(원점)와 적(앞쪽) 사이에 벽 2개 → 적이 우회해야 함
        MakeObstacle(new Vector3(2.2f, 0.9f, 0f), new Vector2(0.5f, 2.6f));
        MakeObstacle(new Vector3(4.5f, -1.2f, 0f), new Vector2(0.5f, 2.6f));

        // ── 저장 ────────────────────────────────────────────
        EnsureFolder("Assets/Scenes");
        EditorSceneBuildUtil.SaveAndClose(scene, SCENE_PATH, prevActive);  // 저장 후 닫기(현재 씬 유지)
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=cyan>[Sandbox]</color> 전투 샌드박스 생성: {SCENE_PATH} — Play 시 PlayerRig 자동 스폰 + 적 3기. 좌클릭=약공, 우클릭홀드=강공, Space=구르기.");
    }

    /// <summary>벽 장애물(비-트리거 BoxCollider2D + 어두운 스프라이트). 길찾기·물리 모두 차단.</summary>
    static void MakeObstacle(Vector3 pos, Vector2 size)
    {
        var go = new GameObject("Obstacle");
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.18f, 0.18f, 0.22f);
        sr.sortingOrder = -1;
        var path = AssetDatabase.GUIDToAssetPath(SPRITE_GUID);
        if (!string.IsNullOrEmpty(path)) sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        go.AddComponent<BoxCollider2D>(); // 스프라이트 bounds 자동 → 스케일로 월드 크기 결정. Default 레이어=장애물
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
