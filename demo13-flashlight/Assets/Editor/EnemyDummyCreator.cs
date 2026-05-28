using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

/// <summary>
/// 적 더미 프리팹 생성 에디터.
/// Bandit_Weak 1.prefab 구조를 기준으로 생성.
/// 메뉴: Tools > Dev Tools > Spawn > Enemy Dummy Creator
/// </summary>
public class EnemyDummyCreator : EditorWindow
{
    [Header("기본 설정")]
    string enemyName = "EnemyDummy";
    Color bodyColor = new Color(1f, 1f, 1f, 1f);

    // 스탯
    float maxHp = 60f;
    float attackDamage = 15f;
    float attackRange = 1.5f;
    float attackSpeed = 0.67f;
    float moveSpeed = 2.5f;
    float patrolSpeed = 1.2f;
    float detectRange = 8f;
    float loseRange = 12f;
    float patrolRadius = 5f;
    float patrolWaitTime = 2f;
    float attackWindup = 0.8f;

    // 그로기
    float maxGroggy = 100f;
    float groggyDecay = 8f;
    float groggyStunDuration = 2f;

    // HitFeedback
    float flashDuration = 0.12f;
    float punchScale = 1.25f;
    float punchDuration = 0.15f;
    float knockbackDist = 0.15f;
    float knockbackDuration = 0.1f;
    float freezeDuration = 0.05f;

    // AttackLunge
    float lightLungeDist = 0.3f;
    float lightLungeDuration = 0.08f;
    float lightReturnDuration = 0.12f;
    float heavyLungeDist = 0.6f;
    float heavyLungeDuration = 0.06f;
    float heavyReturnDuration = 0.18f;

    // EnemyOutline
    float ringRadius = 0.6f;

    // HealthBar
    float healthBarY = 0.85f;

    // 프리셋
    int presetIndex = 0;
    string[] presetNames = { "커스텀", "밴딧 (약)", "밴딧 (강)", "몬스터", "낮 배회자" };

    // 참조
    string unitKey = ""; // StatDB 유닛 키
    Sprite enemySprite;
    Material spriteMaterial;
    bool savePrefab = true;

    Vector2 scrollPos;
    bool showHitFeedback = false;
    bool showAttackLunge = false;

    [MenuItem("Tools/Dev Tools/Spawn/Enemy Dummy Creator")]
    static void Open()
    {
        var window = GetWindow<EnemyDummyCreator>("Enemy Dummy Creator");
        window.minSize = new Vector2(400, 650);
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("적 더미 프리팹 생성기", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Bandit_Weak 1.prefab 구조 기준", MessageType.None);
        EditorGUILayout.Space(5);

        // ---- 프리셋 ----
        EditorGUI.BeginChangeCheck();
        presetIndex = EditorGUILayout.Popup("프리셋", presetIndex, presetNames);
        if (EditorGUI.EndChangeCheck() && presetIndex > 0)
            ApplyPreset(presetIndex);

        EditorGUILayout.Space(10);

        // ---- 기본 정보 ----
        EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
        enemyName = EditorGUILayout.TextField("이름", enemyName);
        bodyColor = EditorGUILayout.ColorField("스프라이트 색상", bodyColor);
        enemySprite = (Sprite)EditorGUILayout.ObjectField("스프라이트", enemySprite, typeof(Sprite), false);
        spriteMaterial = (Material)EditorGUILayout.ObjectField("스프라이트 머티리얼", spriteMaterial, typeof(Material), false);

        EditorGUILayout.Space(10);

        // ---- 전투 스탯 ----
        EditorGUILayout.LabelField("전투 스탯", EditorStyles.boldLabel);
        maxHp = EditorGUILayout.FloatField("최대 HP", maxHp);
        attackDamage = EditorGUILayout.FloatField("공격력", attackDamage);
        attackRange = EditorGUILayout.FloatField("공격 사거리", attackRange);
        attackSpeed = EditorGUILayout.FloatField("공격 속도 (초당)", attackSpeed);
        attackWindup = EditorGUILayout.FloatField("예비동작 시간", attackWindup);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("이동 / 감지", EditorStyles.boldLabel);
        moveSpeed = EditorGUILayout.FloatField("이동 속도", moveSpeed);
        patrolSpeed = EditorGUILayout.FloatField("순찰 속도", patrolSpeed);
        detectRange = EditorGUILayout.FloatField("감지 범위", detectRange);
        loseRange = EditorGUILayout.FloatField("추격 포기 범위", loseRange);
        patrolRadius = EditorGUILayout.FloatField("순찰 반경", patrolRadius);
        patrolWaitTime = EditorGUILayout.FloatField("순찰 대기 시간", patrolWaitTime);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("그로기", EditorStyles.boldLabel);
        maxGroggy = EditorGUILayout.FloatField("최대 그로기", maxGroggy);
        groggyDecay = EditorGUILayout.FloatField("그로기 감소/초", groggyDecay);
        groggyStunDuration = EditorGUILayout.FloatField("스턴 지속 시간", groggyStunDuration);

        EditorGUILayout.Space(5);

        // ---- HitFeedback (접이식) ----
        showHitFeedback = EditorGUILayout.Foldout(showHitFeedback, "피격 피드백 (HitFeedback)");
        if (showHitFeedback)
        {
            EditorGUI.indentLevel++;
            flashDuration = EditorGUILayout.FloatField("플래시 시간", flashDuration);
            punchScale = EditorGUILayout.FloatField("스케일 펀치 배율", punchScale);
            punchDuration = EditorGUILayout.FloatField("펀치 시간", punchDuration);
            knockbackDist = EditorGUILayout.FloatField("넉백 거리", knockbackDist);
            knockbackDuration = EditorGUILayout.FloatField("넉백 시간", knockbackDuration);
            freezeDuration = EditorGUILayout.FloatField("히트스탑 시간", freezeDuration);
            EditorGUI.indentLevel--;
        }

        // ---- AttackLunge (접이식) ----
        showAttackLunge = EditorGUILayout.Foldout(showAttackLunge, "공격 돌진 (AttackLunge)");
        if (showAttackLunge)
        {
            EditorGUI.indentLevel++;
            lightLungeDist = EditorGUILayout.FloatField("약공 돌진 거리", lightLungeDist);
            lightLungeDuration = EditorGUILayout.FloatField("약공 돌진 시간", lightLungeDuration);
            lightReturnDuration = EditorGUILayout.FloatField("약공 복귀 시간", lightReturnDuration);
            heavyLungeDist = EditorGUILayout.FloatField("강공 돌진 거리", heavyLungeDist);
            heavyLungeDuration = EditorGUILayout.FloatField("강공 돌진 시간", heavyLungeDuration);
            heavyReturnDuration = EditorGUILayout.FloatField("강공 복귀 시간", heavyReturnDuration);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("기타", EditorStyles.boldLabel);
        ringRadius = EditorGUILayout.FloatField("타겟 링 반경", ringRadius);
        healthBarY = EditorGUILayout.FloatField("HP바 Y 위치", healthBarY);

        EditorGUILayout.Space(10);

        // ---- 참조 ----
        EditorGUILayout.LabelField("데이터 참조 (선택)", EditorStyles.boldLabel);
        unitKey = EditorGUILayout.TextField("StatDB Unit Key", unitKey);

        // StatDB에서 키로 불러오기 버튼
        if (!string.IsNullOrEmpty(unitKey) && GUILayout.Button("Load from StatDB"))
        {
            var db = AssetDatabase.LoadAssetAtPath<StatDB>("Assets/Resources/Data/StatDB.asset");
            if (db != null)
            {
                var unit = db.GetUnit(unitKey);
                if (unit != null)
                {
                    enemyName = unit.displayName;
                    maxHp = unit.maxHp;
                    attackDamage = unit.attackDamage;
                    attackRange = unit.attackRange;
                    attackSpeed = unit.attackSpeed;
                    attackWindup = unit.attackWindup;
                    moveSpeed = unit.moveSpeed;
                    patrolSpeed = unit.patrolSpeed;
                    detectRange = unit.detectRange;
                    loseRange = unit.loseRange;
                    patrolRadius = unit.patrolRadius;
                    patrolWaitTime = unit.patrolWaitTime;
                    maxGroggy = unit.maxGroggy;
                    groggyDecay = unit.groggyDecay;
                    groggyStunDuration = unit.groggyStunDuration;
                    bodyColor = unit.tintColor;
                    Debug.Log($"[EnemyDummy] StatDB '{unitKey}' 데이터 로드 완료");
                }
                else Debug.LogWarning($"[EnemyDummy] StatDB에 '{unitKey}' 키 없음");
            }
        }

        savePrefab = EditorGUILayout.Toggle("프리팹으로 저장", savePrefab);

        EditorGUILayout.Space(15);

        // ---- 생성 버튼 ----
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("적 더미 생성", GUILayout.Height(40)))
            CreateEnemyDummy();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    void ApplyPreset(int index)
    {
        switch (index)
        {
            case 1: // 밴딧 (약)
                enemyName = "Bandit_Weak";
                bodyColor = Color.white;
                maxHp = 40f; attackDamage = 10f; attackRange = 1.5f; attackSpeed = 0.8f;
                moveSpeed = 2.5f; patrolSpeed = 1f; detectRange = 7f; loseRange = 11f;
                patrolRadius = 4f; patrolWaitTime = 2f; attackWindup = 0.7f;
                maxGroggy = 80f; groggyDecay = 10f; groggyStunDuration = 2.5f;
                break;
            case 2: // 밴딧 (강)
                enemyName = "Bandit_Strong";
                bodyColor = Color.white;
                maxHp = 100f; attackDamage = 22f; attackRange = 1.8f; attackSpeed = 0.5f;
                moveSpeed = 2f; patrolSpeed = 0.8f; detectRange = 9f; loseRange = 13f;
                patrolRadius = 5f; patrolWaitTime = 2f; attackWindup = 1.0f;
                maxGroggy = 150f; groggyDecay = 6f; groggyStunDuration = 1.5f;
                break;
            case 3: // 몬스터
                enemyName = "Monster";
                bodyColor = Color.white;
                maxHp = 80f; attackDamage = 18f; attackRange = 2f; attackSpeed = 0.6f;
                moveSpeed = 3f; patrolSpeed = 1.5f; detectRange = 10f; loseRange = 14f;
                patrolRadius = 6f; patrolWaitTime = 2f; attackWindup = 0.6f;
                maxGroggy = 120f; groggyDecay = 5f; groggyStunDuration = 1.8f;
                break;
            case 4: // 낮 배회자
                enemyName = "Wanderer_Day";
                bodyColor = Color.white;
                maxHp = 25f; attackDamage = 5f; attackRange = 1.2f; attackSpeed = 0.5f;
                moveSpeed = 1.5f; patrolSpeed = 0.8f; detectRange = 5f; loseRange = 8f;
                patrolRadius = 3f; patrolWaitTime = 2f; attackWindup = 1.0f;
                maxGroggy = 50f; groggyDecay = 12f; groggyStunDuration = 3f;
                break;
        }
    }

    void CreateEnemyDummy()
    {
        // ===== 루트 오브젝트 =====
        var root = new GameObject(enemyName);
        root.tag = "Untagged";
        Undo.RegisterCreatedObjectUndo(root, "Create Enemy Dummy");

        // CapsuleCollider
        var capsule = root.AddComponent<CapsuleCollider>();
        capsule.radius = 0.25f;
        capsule.height = 0.8f;
        capsule.center = new Vector3(0, 0.4f, 0);

        // NavMeshAgent
        var agent = root.AddComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.acceleration = 50f;
        agent.angularSpeed = 0f;
        agent.radius = 0.3f;
        agent.height = 0.8f;
        agent.stoppingDistance = 0.3f;

        // Health
        var health = root.AddComponent<Health>();
        SetField(health, "maxHp", maxHp);

        // EnemyController (EnemyAI + GroggySystem + HealthBar3D + WalkBounce 통합)
        var enemy = root.AddComponent<EnemyController>();
        SetField(enemy, "detectRange", detectRange);
        SetField(enemy, "attackRange", attackRange);
        SetField(enemy, "loseRange", loseRange);
        SetField(enemy, "moveSpeed", moveSpeed);
        SetField(enemy, "patrolSpeed", patrolSpeed);
        SetField(enemy, "patrolRadius", patrolRadius);
        SetField(enemy, "patrolWaitTime", patrolWaitTime);
        SetField(enemy, "attackDamage", attackDamage);
        SetField(enemy, "attackSpeed", attackSpeed);
        SetField(enemy, "attackWindup", attackWindup);
        SetField(enemy, "maxGroggy", maxGroggy);
        SetField(enemy, "groggyDecay", groggyDecay);
        SetField(enemy, "groggyStunDuration", groggyStunDuration);
        if (!string.IsNullOrEmpty(unitKey))
            SetField(enemy, "unitKey", unitKey);

        // EnemyOutline
        var outline = root.AddComponent<EnemyOutline>();
        SetField(outline, "ringRadius", ringRadius);

        // CombatFeedback (HitFeedback + AttackLunge 통합)
        var cfb = root.AddComponent<CombatFeedback>();
        SetField(cfb, "flashDuration", flashDuration);
        SetField(cfb, "punchScale", punchScale);
        SetField(cfb, "punchDuration", punchDuration);
        SetField(cfb, "knockbackDist", knockbackDist);
        SetField(cfb, "knockbackDuration", knockbackDuration);
        SetField(cfb, "freezeDuration", freezeDuration);
        SetField(cfb, "lightLungeDist", lightLungeDist);
        SetField(cfb, "lightLungeDuration", lightLungeDuration);
        SetField(cfb, "lightReturnDuration", lightReturnDuration);
        SetField(cfb, "heavyLungeDist", heavyLungeDist);
        SetField(cfb, "heavyLungeDuration", heavyLungeDuration);
        SetField(cfb, "heavyReturnDuration", heavyReturnDuration);

        // IsometricDepthSorter
        root.AddComponent<IsometricDepthSorter>();

        // ===== Root (비주얼 컨테이너) =====
        var visualRoot = new GameObject("Root");
        visualRoot.transform.SetParent(root.transform);
        visualRoot.transform.localPosition = new Vector3(0, 0.4f, 0);
        visualRoot.transform.localRotation = Quaternion.Euler(26f, 42f, 0f);
        visualRoot.transform.localScale = Vector3.one;

        // ===== EnemySprite =====
        var spriteObj = new GameObject("EnemySprite");
        spriteObj.transform.SetParent(visualRoot.transform);
        spriteObj.transform.localPosition = Vector3.zero;
        spriteObj.transform.localScale = Vector3.one;

        var sr = spriteObj.AddComponent<SpriteRenderer>();
        sr.color = bodyColor;

        if (enemySprite != null)
            sr.sprite = enemySprite;
        else
        {
            var playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Player/Player.png");
            if (playerSprite != null) sr.sprite = playerSprite;
        }

        if (spriteMaterial != null)
            sr.sharedMaterial = spriteMaterial;

        // HealthBar와 GroggyBar는 EnemyController가 자동 생성

        // ===== shadow =====
        var shadowObj = new GameObject("shadow");
        shadowObj.transform.SetParent(root.transform);
        shadowObj.transform.localPosition = new Vector3(0, 0.02f, 0);
        shadowObj.transform.localRotation = Quaternion.Euler(90f, 0, 0);
        shadowObj.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        var shadowSr = shadowObj.AddComponent<SpriteRenderer>();
        shadowSr.color = new Color(0, 0, 0, 0.35f);
        shadowSr.sortingOrder = -10;

        if (spriteMaterial != null)
            shadowSr.sharedMaterial = spriteMaterial;

        // ===== 프리팹 저장 =====
        if (savePrefab)
        {
            string folderPath = "Assets/Prefab";
            if (!AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.CreateFolder("Assets", "Prefab");

            string prefabPath = $"{folderPath}/{enemyName}.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                if (!EditorUtility.DisplayDialog("프리팹 덮어쓰기",
                    $"'{enemyName}.prefab'이 이미 존재합니다.\n덮어쓰시겠습니까?",
                    "덮어쓰기", "취소"))
                {
                    Selection.activeGameObject = root;
                    return;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"<color=green>프리팹 저장:</color> {prefabPath}");
        }

        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log($"<color=cyan>적 생성:</color> {enemyName} | " +
            $"HP:{maxHp} ATK:{attackDamage} SPD:{moveSpeed} " +
            $"Groggy:{maxGroggy} Windup:{attackWindup}s");
    }

    static void SetField(object target, string fieldName, object value)
    {
        var type = target.GetType();
        var field = type.GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public);

        if (field != null)
        {
            field.SetValue(target, value);
            if (target is Object unityObj)
                EditorUtility.SetDirty(unityObj);
        }
        else
        {
            Debug.LogWarning($"<color=yellow>Field '{fieldName}' not found on {type.Name}</color>");
        }
    }
}
