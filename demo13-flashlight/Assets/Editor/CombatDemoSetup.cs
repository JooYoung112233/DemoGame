using UnityEngine;
using UnityEditor;

public class CombatDemoSetup : EditorWindow
{
    const string PREFAB_PATH = "Assets/PixelArtStudio/SkeletonsPack/CommonSoldier/Prefabs/CommonSoldier.prefab";

    [MenuItem("Tools/Setup Combat Demo (Skeleton)")]
    static void Setup()
    {
        if (!EditorUtility.DisplayDialog("Combat Demo Setup",
            "해골 프리팹으로 전투 데모를 셋업합니다.\n" +
            "(먼저 'Setup Flashlight Prototype Scene'을 실행해주세요)\n\n계속?",
            "Yes", "Cancel"))
            return;

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null)
        {
            EditorUtility.DisplayDialog("Error", "Player를 찾을 수 없습니다.\n먼저 Flashlight Prototype Scene을 셋업하세요.", "OK");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Error", $"프리팹 없음: {PREFAB_PATH}", "OK");
            return;
        }

        // ===== 플레이어 스프라이트 교체 =====
        SetupPlayerSprite(playerGO, prefab);

        // ===== 플레이어 전투 =====
        SetupPlayerCombat(playerGO);

        // ===== 적 3마리 =====
        SpawnEnemies(prefab);

        Debug.Log("[Combat Demo] 해골 전투 데모 셋업 완료!");
    }

    static void SetupPlayerSprite(GameObject playerGO, GameObject prefab)
    {
        // 기존 PlayerSprite 제거
        var oldSprite = playerGO.transform.Find("PlayerSprite");
        if (oldSprite != null) Object.DestroyImmediate(oldSprite.gameObject);

        // 기존 BillboardSprite 제거 (있으면)
        var oldBillboard = playerGO.GetComponentInChildren<BillboardSprite>();
        if (oldBillboard != null) Object.DestroyImmediate(oldBillboard);

        // 프리팹 인스턴스 생성 → 플레이어 자식으로
        var skeleton = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        skeleton.name = "SkeletonSprite";
        skeleton.transform.SetParent(playerGO.transform);
        skeleton.transform.localPosition = new Vector3(0, 0.1f, 0);
        skeleton.transform.localScale = Vector3.one * 2f;

        // 플레이어 색상 구분: SpriteRenderer에 푸른 틴트
        var renderers = skeleton.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            if (r.gameObject.name == "shadow") continue;
            r.color = new Color(0.7f, 0.8f, 1f, 1f);
        }

        // SkeletonAnimController 추가
        var animCtrl = skeleton.GetComponent<SkeletonAnimController>();
        if (animCtrl == null) animCtrl = skeleton.AddComponent<SkeletonAnimController>();
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

        var animCtrl = playerGO.GetComponentInChildren<SkeletonAnimController>();
        var playerCtrl = playerGO.GetComponent<PlayerController>();
        combatSO.FindProperty("animController").objectReferenceValue = animCtrl;
        combatSO.FindProperty("playerController").objectReferenceValue = playerCtrl;
        combatSO.ApplyModifiedProperties();

        // HP바
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

    static void SpawnEnemies(GameObject prefab)
    {
        // 기존 적 제거
        var oldEnemies = GameObject.Find("Enemies");
        if (oldEnemies != null) Object.DestroyImmediate(oldEnemies);

        var enemyRoot = new GameObject("Enemies");
        Undo.RegisterCreatedObjectUndo(enemyRoot, "Create Enemies");

        Vector3[] positions = {
            new Vector3(3, 0, 5),
            new Vector3(8, 0, 7),
            new Vector3(10, 0, 3),
        };

        for (int i = 0; i < positions.Length; i++)
            CreateEnemy(enemyRoot, prefab, $"Skeleton_{i}", positions[i]);
    }

    static void CreateEnemy(GameObject parent, GameObject prefab, string name, Vector3 position)
    {
        var enemyGO = new GameObject(name);
        enemyGO.transform.SetParent(parent.transform);
        enemyGO.transform.position = position;

        // CharacterController
        var cc = enemyGO.AddComponent<CharacterController>();
        cc.radius = 0.3f;
        cc.height = 0.1f;
        cc.center = new Vector3(0, 0.05f, 0);

        // 프리팹 인스턴스
        var skeleton = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        skeleton.name = "SkeletonSprite";
        skeleton.transform.SetParent(enemyGO.transform);
        skeleton.transform.localPosition = new Vector3(0, 0.1f, 0);
        skeleton.transform.localScale = Vector3.one * 2f;

        // 적 색상: 붉은 틴트
        var renderers = skeleton.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            if (r.gameObject.name == "shadow") continue;
            r.color = new Color(1f, 0.7f, 0.7f, 1f);
        }

        // SkeletonAnimController
        var animCtrl = skeleton.GetComponent<SkeletonAnimController>();
        if (animCtrl == null) animCtrl = skeleton.AddComponent<SkeletonAnimController>();

        // Health
        var health = enemyGO.AddComponent<Health>();
        var healthSO = new SerializedObject(health);
        healthSO.FindProperty("maxHp").floatValue = 60f;
        healthSO.ApplyModifiedProperties();

        // EnemyAI
        var ai = enemyGO.AddComponent<EnemyAI>();
        var aiSO = new SerializedObject(ai);
        aiSO.FindProperty("animController").objectReferenceValue = animCtrl;
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
}
