using UnityEngine;
using UnityEditor;
using System.IO;

public class CombatDemoSetup : EditorWindow
{
    const string SKELETON_PATH = "Assets/PixelArtStudio/SkeletonsPack/CommonSoldier/Textures";
    static readonly string[] DIRS = { "Bot", "LeftBot", "Left", "LeftTop", "Top", "RightTop", "Right", "RightBot" };

    [MenuItem("Tools/Setup Combat Demo (Skeleton)")]
    static void Setup()
    {
        if (!EditorUtility.DisplayDialog("Combat Demo Setup",
            "해골 스프라이트로 전투 데모를 셋업합니다.\n" +
            "(먼저 'Setup Flashlight Prototype Scene'을 실행해주세요)\n\n계속?",
            "Yes", "Cancel"))
            return;

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null)
        {
            EditorUtility.DisplayDialog("Error", "Player를 찾을 수 없습니다.\n먼저 Flashlight Prototype Scene을 셋업하세요.", "OK");
            return;
        }

        // ===== 플레이어 스프라이트 교체 =====
        SetupPlayerSprite(playerGO);

        // ===== 플레이어 전투 컴포넌트 =====
        SetupPlayerCombat(playerGO);

        // ===== 적 3마리 스폰 =====
        SpawnEnemies(3);

        Debug.Log("[Combat Demo] 해골 전투 데모 셋업 완료!");
    }

    static void SetupPlayerSprite(GameObject playerGO)
    {
        // 기존 PlayerSprite Quad 찾기
        var oldSprite = playerGO.transform.Find("PlayerSprite");
        if (oldSprite != null)
        {
            // Quad를 재사용
            var renderer = oldSprite.GetComponent<MeshRenderer>();

            // 머테리얼을 Unlit AlphaTest로 설정
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.name = "SkeletonPlayerMat";
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_AlphaClip", 1);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = 2450;
            // 플레이어 색상 구분: 살짝 푸른 틴트
            mat.SetColor("_BaseColor", new Color(0.7f, 0.8f, 1f, 1f));
            renderer.sharedMaterial = mat;

            // 크기 조정
            oldSprite.localScale = new Vector3(1.2f, 1.2f, 1);
            oldSprite.localPosition = new Vector3(0, 0.6f, 0);

            // 애니메이터 추가
            var anim = playerGO.GetComponent<IsometricSpriteAnimator>();
            if (anim == null) anim = playerGO.AddComponent<IsometricSpriteAnimator>();

            var animSO = new SerializedObject(anim);
            animSO.FindProperty("targetRenderer").objectReferenceValue = renderer;
            animSO.FindProperty("frameRate").floatValue = 10f;

            // 8방향 스프라이트 시트 로드
            SetSpriteSheets(animSO, "idleSheets", "Idle", 16);
            SetSpriteSheets(animSO, "walkSheets", "Walk", 8);
            SetSpriteSheets(animSO, "attackSheets", "Attack", 8);
            SetSpriteSheets(animSO, "deathSheets", "Death", 8);
            SetSpriteSheets(animSO, "getHitSheets", "GetHit", 4);
            animSO.ApplyModifiedProperties();
        }
    }

    static void SetupPlayerCombat(GameObject playerGO)
    {
        // Health
        var health = playerGO.GetComponent<Health>();
        if (health == null) health = playerGO.AddComponent<Health>();
        var healthSO = new SerializedObject(health);
        healthSO.FindProperty("maxHp").floatValue = 100f;
        healthSO.ApplyModifiedProperties();

        // PlayerCombat
        var combat = playerGO.GetComponent<PlayerCombat>();
        if (combat == null) combat = playerGO.AddComponent<PlayerCombat>();
        var combatSO = new SerializedObject(combat);
        combatSO.FindProperty("attackDamage").floatValue = 25f;
        combatSO.FindProperty("attackRange").floatValue = 2f;
        combatSO.FindProperty("attackCooldown").floatValue = 0.8f;

        var animator = playerGO.GetComponent<IsometricSpriteAnimator>();
        var playerCtrl = playerGO.GetComponent<PlayerController>();
        combatSO.FindProperty("animator").objectReferenceValue = animator;
        combatSO.FindProperty("playerController").objectReferenceValue = playerCtrl;
        combatSO.ApplyModifiedProperties();

        // HP바
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

    static void SpawnEnemies(int count)
    {
        var enemyRoot = new GameObject("Enemies");
        Undo.RegisterCreatedObjectUndo(enemyRoot, "Create Enemies");

        Vector3[] spawnPositions = {
            new Vector3(3, 0, 5),
            new Vector3(8, 0, 7),
            new Vector3(10, 0, 3),
        };

        for (int i = 0; i < count && i < spawnPositions.Length; i++)
        {
            CreateEnemy(enemyRoot, $"Skeleton_{i}", spawnPositions[i]);
        }
    }

    static void CreateEnemy(GameObject parent, string name, Vector3 position)
    {
        var enemyGO = new GameObject(name);
        enemyGO.transform.SetParent(parent.transform);
        enemyGO.transform.position = position;

        // CharacterController
        var cc = enemyGO.AddComponent<CharacterController>();
        cc.radius = 0.3f;
        cc.height = 0.1f;
        cc.center = new Vector3(0, 0.05f, 0);

        // 스프라이트 Quad
        var spriteGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        spriteGO.name = "Sprite";
        spriteGO.transform.SetParent(enemyGO.transform);
        spriteGO.transform.localPosition = new Vector3(0, 0.6f, 0);
        spriteGO.transform.localScale = new Vector3(1.2f, 1.2f, 1);
        Object.DestroyImmediate(spriteGO.GetComponent<MeshCollider>());

        // 빌보드
        spriteGO.AddComponent<BillboardSprite>();

        // 머테리얼 (적은 붉은 틴트)
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.name = $"SkeletonEnemyMat_{name}";
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_AlphaClip", 1);
        mat.SetFloat("_Cutoff", 0.5f);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.renderQueue = 2450;
        mat.SetColor("_BaseColor", new Color(1f, 0.7f, 0.7f, 1f)); // 붉은 틴트
        spriteGO.GetComponent<MeshRenderer>().sharedMaterial = mat;
        spriteGO.GetComponent<MeshRenderer>().shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        // IsometricSpriteAnimator
        var anim = enemyGO.AddComponent<IsometricSpriteAnimator>();
        var animSO = new SerializedObject(anim);
        animSO.FindProperty("targetRenderer").objectReferenceValue =
            spriteGO.GetComponent<MeshRenderer>();
        animSO.FindProperty("frameRate").floatValue = 10f;

        SetSpriteSheets(animSO, "idleSheets", "Idle", 16);
        SetSpriteSheets(animSO, "walkSheets", "Walk", 8);
        SetSpriteSheets(animSO, "attackSheets", "Attack", 8);
        SetSpriteSheets(animSO, "deathSheets", "Death", 8);
        SetSpriteSheets(animSO, "getHitSheets", "GetHit", 4);
        animSO.ApplyModifiedProperties();

        // Health
        var health = enemyGO.AddComponent<Health>();
        var healthSO = new SerializedObject(health);
        healthSO.FindProperty("maxHp").floatValue = 60f;
        healthSO.ApplyModifiedProperties();

        // EnemyAI
        var ai = enemyGO.AddComponent<EnemyAI>();
        var aiSO = new SerializedObject(ai);
        aiSO.FindProperty("animator").objectReferenceValue = anim;
        aiSO.FindProperty("detectRange").floatValue = 8f;
        aiSO.FindProperty("attackRange").floatValue = 1.5f;
        aiSO.FindProperty("moveSpeed").floatValue = 2.5f;
        aiSO.FindProperty("attackDamage").floatValue = 15f;
        aiSO.ApplyModifiedProperties();

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

    static void SetSpriteSheets(SerializedObject so, string propName, string animFolder, int defaultFrameCount)
    {
        var prop = so.FindProperty(propName);
        prop.arraySize = 8;

        for (int i = 0; i < 8; i++)
        {
            string dir = DIRS[i];
            string texPath = $"{SKELETON_PATH}/{animFolder}/{dir}/{animFolder}.png";

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            var element = prop.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("texture").objectReferenceValue = tex;

            // 프레임 수 자동 계산: 텍스처가 있으면 세로/가로 비율로 추정
            int frames = defaultFrameCount;
            if (tex != null && tex.width > 0)
            {
                // 스프라이트 시트는 세로로 프레임 나열, 각 프레임은 대략 정사각형
                float ratio = (float)tex.height / tex.width;
                frames = Mathf.Max(1, Mathf.RoundToInt(ratio));
            }
            element.FindPropertyRelative("frameCount").intValue = frames;

            if (tex == null)
                Debug.LogWarning($"[Combat Demo] 텍스처 없음: {texPath}");
        }
    }
}
