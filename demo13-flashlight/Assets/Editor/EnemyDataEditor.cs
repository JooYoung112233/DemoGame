using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 적 타입 관리 에디터 윈도우.
/// 적 타입 생성/편집/삭제/복제 + 프리셋 + 밸런스 비교.
/// </summary>
public class EnemyDataEditor : EditorWindow
{
    const string DATA_FOLDER = "Assets/Settings/EnemyData";
    const string COMBAT_DATA_PATH = "Assets/Settings/CombatData.asset";

    List<EnemyData> enemyList = new List<EnemyData>();
    int selectedIndex = -1;
    Vector2 listScrollPos;
    Vector2 detailScrollPos;

    bool showVisual = true;
    bool showCombat = true;
    bool showMovement = true;
    bool showDetection = true;
    bool showAI = true;
    bool showReward = true;
    bool showBalance = true;
    bool showPrefab = true;

    CombatData combatData;

    [MenuItem("Tools/Enemy Data Editor")]
    static void Open()
    {
        var win = GetWindow<EnemyDataEditor>("Enemy Data");
        win.minSize = new Vector2(650, 500);
        win.RefreshList();
    }

    void OnEnable()
    {
        RefreshList();
        combatData = AssetDatabase.LoadAssetAtPath<CombatData>(COMBAT_DATA_PATH);
    }

    void OnFocus() => RefreshList();

    void RefreshList()
    {
        enemyList.Clear();

        if (!AssetDatabase.IsValidFolder(DATA_FOLDER))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");
            AssetDatabase.CreateFolder("Assets/Settings", "EnemyData");
        }

        var guids = AssetDatabase.FindAssets("t:EnemyData", new[] { DATA_FOLDER });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data != null) enemyList.Add(data);
        }

        // 이름순 정렬
        enemyList.Sort((a, b) => string.Compare(a.displayName, b.displayName));

        if (selectedIndex >= enemyList.Count)
            selectedIndex = enemyList.Count - 1;
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        // ===== 왼쪽: 목록 패널 =====
        DrawListPanel();

        // ===== 오른쪽: 상세 패널 =====
        DrawDetailPanel();

        EditorGUILayout.EndHorizontal();
    }

    // ================================================================
    //  왼쪽 패널 — 적 목록
    // ================================================================
    void DrawListPanel()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(200), GUILayout.ExpandHeight(true));

        // 헤더
        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("ENEMY TYPES", headerStyle);
        EditorGUILayout.Space(4);

        // 목록
        listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos);

        for (int i = 0; i < enemyList.Count; i++)
        {
            var enemy = enemyList[i];
            bool isSelected = (i == selectedIndex);

            EditorGUILayout.BeginHorizontal();

            // 색상 인디케이터
            var colorRect = GUILayoutUtility.GetRect(12, 18, GUILayout.Width(12));
            EditorGUI.DrawRect(colorRect, enemy.tintColor);

            // 버튼
            var btnStyle = new GUIStyle(isSelected ? EditorStyles.boldLabel : EditorStyles.label);
            if (isSelected)
                btnStyle.normal.textColor = new Color(0.3f, 0.7f, 1f);

            if (GUILayout.Button(enemy.displayName, btnStyle))
                selectedIndex = i;

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);

        // ===== 버튼 =====
        if (GUILayout.Button("+ New Enemy", GUILayout.Height(24)))
            CreateNewEnemy("New Enemy");

        EditorGUILayout.Space(2);

        // 프리셋 버튼들
        EditorGUILayout.LabelField("Presets:", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Melee", EditorStyles.miniButton))
            CreatePreset_Melee();
        if (GUILayout.Button("Ranged", EditorStyles.miniButton))
            CreatePreset_Ranged();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Tank", EditorStyles.miniButton))
            CreatePreset_Tank();
        if (GUILayout.Button("Boss", EditorStyles.miniButton))
            CreatePreset_Boss();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // 선택된 적 조작 버튼
        GUI.enabled = selectedIndex >= 0 && selectedIndex < enemyList.Count;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Duplicate", EditorStyles.miniButton))
            DuplicateSelected();
        if (GUILayout.Button("Delete", EditorStyles.miniButton))
            DeleteSelected();
        EditorGUILayout.EndHorizontal();

        GUI.enabled = true;

        EditorGUILayout.EndVertical();
    }

    // ================================================================
    //  오른쪽 패널 — 상세 편집
    // ================================================================
    void DrawDetailPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        if (selectedIndex < 0 || selectedIndex >= enemyList.Count)
        {
            EditorGUILayout.HelpBox("왼쪽에서 적 타입을 선택하거나 새로 만드세요.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        var enemy = enemyList[selectedIndex];
        var so = new SerializedObject(enemy);
        so.Update();

        detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos);

        // ===== 헤더 =====
        EditorGUILayout.Space(4);
        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField(enemy.displayName, titleStyle);
        EditorGUILayout.Space(6);

        // ===== 기본 정보 =====
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("displayName"), new GUIContent("Display Name"));
        DrawSlider(so.FindProperty("scale"), "Scale", 0.5f, 5f);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // ===== 비주얼 =====
        showVisual = EditorGUILayout.BeginFoldoutHeaderGroup(showVisual, "VISUAL");
        if (showVisual)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(so.FindProperty("tintColor"), new GUIContent("Body Tint"));
            EditorGUILayout.PropertyField(so.FindProperty("shadowColor"), new GUIContent("Shadow Color"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Glow Light", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(so.FindProperty("useGlow"), new GUIContent("Enable Glow"));
            if (enemy.useGlow)
            {
                EditorGUILayout.PropertyField(so.FindProperty("glowColor"), new GUIContent("  Glow Color"));
                DrawSlider(so.FindProperty("glowIntensity"), "  Glow Intensity", 0.5f, 15);
                DrawSlider(so.FindProperty("glowRange"), "  Glow Range", 0.5f, 10);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Trail Particle", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(so.FindProperty("useTrailParticle"), new GUIContent("Enable Trail"));
            if (enemy.useTrailParticle)
            {
                EditorGUILayout.PropertyField(so.FindProperty("trailColor"), new GUIContent("  Trail Color"));
            }

            // 색상 미리보기 바
            EditorGUILayout.Space(4);
            DrawColorPreview(enemy);

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // ===== 프리팹 =====
        showPrefab = EditorGUILayout.BeginFoldoutHeaderGroup(showPrefab, "PREFAB");
        if (showPrefab)
        {
            EditorGUI.indentLevel++;

            // 현재 프리팹 상태
            if (enemy.generatedPrefab != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Current Prefab:", GUILayout.Width(100));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(enemy.generatedPrefab, typeof(GameObject), false);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("프리팹 미생성. 아래 버튼으로 생성하세요.", MessageType.Info);
            }

            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(
                enemy.generatedPrefab != null ? "Update Prefab" : "Generate Prefab",
                GUILayout.Height(24)))
            {
                so.ApplyModifiedProperties();
                if (enemy.generatedPrefab != null)
                    EnemyPrefabFactory.UpdateExistingPrefab(enemy);
                else
                    EnemyPrefabFactory.GeneratePrefab(enemy);
            }

            if (GUILayout.Button("Preview in Scene", GUILayout.Height(24)))
            {
                so.ApplyModifiedProperties();
                EnemyPrefabFactory.PreviewInScene(enemy);
            }
            EditorGUILayout.EndHorizontal();

            // 모든 프리팹 일괄 생성
            EditorGUILayout.Space(2);
            if (GUILayout.Button("Generate ALL Prefabs", EditorStyles.miniButton))
            {
                so.ApplyModifiedProperties();
                int count = 0;
                foreach (var e in enemyList)
                {
                    if (e.generatedPrefab != null)
                        EnemyPrefabFactory.UpdateExistingPrefab(e);
                    else
                        EnemyPrefabFactory.GeneratePrefab(e);
                    count++;
                }
                Debug.Log($"[PrefabFactory] {count}개 프리팹 일괄 생성/업데이트 완료!");
            }

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // ===== 체력 =====
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("HP", EditorStyles.boldLabel);
        DrawSlider(so.FindProperty("maxHp"), "Max HP", 10, 1000);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // ===== 전투 =====
        showCombat = EditorGUILayout.BeginFoldoutHeaderGroup(showCombat, "COMBAT");
        if (showCombat)
        {
            EditorGUI.indentLevel++;
            DrawSlider(so.FindProperty("attackDamage"), "Attack Damage", 1, 200);
            DrawSlider(so.FindProperty("attackRange"), "Attack Range", 0.5f, 15);
            DrawSlider(so.FindProperty("attackSpeed"), "Attack Speed (hits/s)", 0.1f, 5f);

            EditorGUILayout.Space(2);
            DrawInfoBox(
                $"Cooldown: {enemy.AttackCooldown:F2}s  |  DPS: {enemy.DPS:F1}");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // ===== 이동 =====
        showMovement = EditorGUILayout.BeginFoldoutHeaderGroup(showMovement, "MOVEMENT");
        if (showMovement)
        {
            EditorGUI.indentLevel++;
            DrawSlider(so.FindProperty("moveSpeed"), "Chase Speed", 0.5f, 15);
            DrawSlider(so.FindProperty("patrolSpeed"), "Patrol Speed", 0.5f, 8);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // ===== 감지 =====
        showDetection = EditorGUILayout.BeginFoldoutHeaderGroup(showDetection, "DETECTION");
        if (showDetection)
        {
            EditorGUI.indentLevel++;
            DrawSlider(so.FindProperty("detectRange"), "Detect Range", 1, 30);
            DrawSlider(so.FindProperty("loseRange"), "Lose Range", 2, 40);
            DrawSlider(so.FindProperty("patrolRadius"), "Patrol Radius", 1, 20);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // ===== AI 행동 =====
        showAI = EditorGUILayout.BeginFoldoutHeaderGroup(showAI, "AI BEHAVIOR");
        if (showAI)
        {
            EditorGUI.indentLevel++;
            DrawSlider(so.FindProperty("patrolWaitTime"), "Patrol Wait (s)", 0.5f, 10);
            DrawSlider(so.FindProperty("hitStunDuration"), "Hit Stun (s)", 0f, 2f);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // ===== 보상 =====
        showReward = EditorGUILayout.BeginFoldoutHeaderGroup(showReward, "REWARDS");
        if (showReward)
        {
            EditorGUI.indentLevel++;
            var expProp = so.FindProperty("expReward");
            expProp.intValue = EditorGUILayout.IntSlider("EXP", expProp.intValue, 0, 500);
            var goldProp = so.FindProperty("goldReward");
            goldProp.intValue = EditorGUILayout.IntSlider("Gold", goldProp.intValue, 0, 500);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(8);

        // ===== 밸런스 비교 (vs Player) =====
        showBalance = EditorGUILayout.BeginFoldoutHeaderGroup(showBalance, "BALANCE vs PLAYER");
        if (showBalance)
        {
            DrawBalanceSection(enemy);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(8);

        // ===== 하단 버튼 =====
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Apply to Scene Enemies", GUILayout.Height(26)))
            ApplyToSceneEnemies(enemy);

        if (GUILayout.Button("Sync to CombatData", GUILayout.Height(26)))
            SyncToCombatData(enemy);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();

        so.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(enemy);

        EditorGUILayout.EndVertical();
    }

    // ================================================================
    //  밸런스 비교
    // ================================================================
    void DrawBalanceSection(EnemyData enemy)
    {
        if (combatData == null)
        {
            combatData = AssetDatabase.LoadAssetAtPath<CombatData>(COMBAT_DATA_PATH);
            if (combatData == null)
            {
                EditorGUILayout.HelpBox("CombatData.asset 없음", MessageType.Warning);
                return;
            }
        }

        EditorGUI.indentLevel++;

        float playerDPS = combatData.player.attackDamage * combatData.player.attackSpeed;
        float enemyDPS = enemy.DPS;

        float playerKillTime = enemy.maxHp / Mathf.Max(playerDPS, 0.01f);
        float enemyKillTime = combatData.player.maxHp / Mathf.Max(enemyDPS, 0.01f);

        float playerHitsToKill = Mathf.Ceil(enemy.maxHp / Mathf.Max(combatData.player.attackDamage, 0.1f));
        float enemyHitsToKill = Mathf.Ceil(combatData.player.maxHp / Mathf.Max(enemy.attackDamage, 0.1f));

        DrawProgressBar($"Player kills this enemy: {playerKillTime:F1}s ({playerHitsToKill:F0} hits)",
            playerKillTime / 15f, new Color(0.2f, 0.7f, 0.3f));
        DrawProgressBar($"This enemy kills player: {enemyKillTime:F1}s ({enemyHitsToKill:F0} hits)",
            enemyKillTime / 15f, new Color(0.8f, 0.2f, 0.2f));

        EditorGUILayout.Space(2);

        // 위험도 평가
        float dangerRatio = playerKillTime > 0 ? enemyKillTime / playerKillTime : 0;
        string dangerText;
        Color dangerColor;
        if (dangerRatio > 3f) { dangerText = "Very Easy"; dangerColor = new Color(0.3f, 0.8f, 0.3f); }
        else if (dangerRatio > 1.5f) { dangerText = "Easy"; dangerColor = new Color(0.5f, 0.8f, 0.4f); }
        else if (dangerRatio > 0.8f) { dangerText = "Balanced"; dangerColor = new Color(0.9f, 0.8f, 0.2f); }
        else if (dangerRatio > 0.4f) { dangerText = "Hard"; dangerColor = new Color(0.9f, 0.5f, 0.2f); }
        else { dangerText = "Very Hard"; dangerColor = new Color(0.9f, 0.2f, 0.2f); }

        var diffStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12
        };
        diffStyle.normal.textColor = dangerColor;
        EditorGUILayout.LabelField($"Difficulty: {dangerText}", diffStyle);

        EditorGUI.indentLevel--;
    }

    // ================================================================
    //  생성 / 삭제 / 복제
    // ================================================================
    EnemyData CreateNewEnemy(string name)
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        data.displayName = name;

        string safeName = name.Replace(" ", "_");
        string path = AssetDatabase.GenerateUniqueAssetPath($"{DATA_FOLDER}/{safeName}.asset");
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();

        RefreshList();
        selectedIndex = enemyList.IndexOf(data);
        return data;
    }

    void DuplicateSelected()
    {
        if (selectedIndex < 0 || selectedIndex >= enemyList.Count) return;

        var src = enemyList[selectedIndex];
        var dup = CreateNewEnemy(src.displayName + " Copy");

        // 값 복사 — 비주얼
        dup.tintColor = src.tintColor;
        dup.shadowColor = src.shadowColor;
        dup.useGlow = src.useGlow;
        dup.glowColor = src.glowColor;
        dup.glowIntensity = src.glowIntensity;
        dup.glowRange = src.glowRange;
        dup.useTrailParticle = src.useTrailParticle;
        dup.trailColor = src.trailColor;
        // 스탯
        dup.scale = src.scale;
        dup.maxHp = src.maxHp;
        dup.attackDamage = src.attackDamage;
        dup.attackRange = src.attackRange;
        dup.attackSpeed = src.attackSpeed;
        dup.moveSpeed = src.moveSpeed;
        dup.patrolSpeed = src.patrolSpeed;
        dup.detectRange = src.detectRange;
        dup.loseRange = src.loseRange;
        dup.patrolRadius = src.patrolRadius;
        dup.patrolWaitTime = src.patrolWaitTime;
        dup.hitStunDuration = src.hitStunDuration;
        dup.expReward = src.expReward;
        dup.goldReward = src.goldReward;
        EditorUtility.SetDirty(dup);
        AssetDatabase.SaveAssets();
    }

    void DeleteSelected()
    {
        if (selectedIndex < 0 || selectedIndex >= enemyList.Count) return;

        var enemy = enemyList[selectedIndex];
        if (!EditorUtility.DisplayDialog("Delete Enemy",
            $"'{enemy.displayName}' 를 삭제하시겠습니까?", "Delete", "Cancel"))
            return;

        string path = AssetDatabase.GetAssetPath(enemy);
        AssetDatabase.DeleteAsset(path);
        RefreshList();
    }

    // ================================================================
    //  프리셋
    // ================================================================
    void CreatePreset_Melee()
    {
        var data = CreateNewEnemy("Skeleton Soldier");
        data.tintColor = new Color(1f, 0.7f, 0.7f);
        data.maxHp = 60f;
        data.attackDamage = 15f;
        data.attackRange = 1.5f;
        data.attackSpeed = 0.67f;
        data.moveSpeed = 2.5f;
        data.patrolSpeed = 1.2f;
        data.detectRange = 8f;
        data.loseRange = 12f;
        data.patrolRadius = 5f;
        data.expReward = 10;
        data.goldReward = 5;
        EditorUtility.SetDirty(data);
    }

    void CreatePreset_Ranged()
    {
        var data = CreateNewEnemy("Skeleton Archer");
        data.tintColor = new Color(0.7f, 1f, 0.7f);
        data.shadowColor = new Color(0f, 0.1f, 0f, 0.5f);
        data.scale = 1.8f;
        data.maxHp = 40f;
        data.attackDamage = 20f;
        data.attackRange = 8f;
        data.attackSpeed = 0.5f;
        data.moveSpeed = 2f;
        data.patrolSpeed = 1f;
        data.detectRange = 12f;
        data.loseRange = 16f;
        data.patrolRadius = 3f;
        data.expReward = 15;
        data.goldReward = 8;
        EditorUtility.SetDirty(data);
    }

    void CreatePreset_Tank()
    {
        var data = CreateNewEnemy("Skeleton Guard");
        data.tintColor = new Color(0.7f, 0.7f, 1f);
        data.shadowColor = new Color(0f, 0f, 0.15f, 0.5f);
        data.useGlow = true;
        data.glowColor = new Color(0.4f, 0.4f, 1f);
        data.glowIntensity = 2f;
        data.glowRange = 2f;
        data.scale = 2.5f;
        data.maxHp = 150f;
        data.attackDamage = 25f;
        data.attackRange = 1.8f;
        data.attackSpeed = 0.4f;
        data.moveSpeed = 1.5f;
        data.patrolSpeed = 0.8f;
        data.detectRange = 6f;
        data.loseRange = 10f;
        data.patrolRadius = 3f;
        data.hitStunDuration = 0.15f;
        data.expReward = 25;
        data.goldReward = 15;
        EditorUtility.SetDirty(data);
    }

    void CreatePreset_Boss()
    {
        var data = CreateNewEnemy("Skeleton King");
        data.tintColor = new Color(1f, 0.4f, 1f);
        data.shadowColor = new Color(0.2f, 0f, 0.2f, 0.6f);
        data.useGlow = true;
        data.glowColor = new Color(1f, 0.3f, 1f);
        data.glowIntensity = 5f;
        data.glowRange = 4f;
        data.useTrailParticle = true;
        data.trailColor = new Color(1f, 0.3f, 1f, 0.6f);
        data.scale = 3f;
        data.maxHp = 500f;
        data.attackDamage = 40f;
        data.attackRange = 2.5f;
        data.attackSpeed = 0.8f;
        data.moveSpeed = 3f;
        data.patrolSpeed = 1f;
        data.detectRange = 15f;
        data.loseRange = 20f;
        data.patrolRadius = 4f;
        data.hitStunDuration = 0.1f;
        data.expReward = 100;
        data.goldReward = 50;
        EditorUtility.SetDirty(data);
    }

    // ================================================================
    //  씬 적용 / CombatData 동기화
    // ================================================================
    void ApplyToSceneEnemies(EnemyData enemyData)
    {
        var enemies = Object.FindObjectsOfType<EnemyAI>();
        int count = 0;

        foreach (var ai in enemies)
        {
            // EnemyData가 연결된 적만 업데이트
            var aiSO = new SerializedObject(ai);
            var dataProp = aiSO.FindProperty("enemyData");
            if (dataProp != null && dataProp.objectReferenceValue == enemyData)
            {
                var health = ai.GetComponent<Health>();
                if (health != null)
                {
                    var hso = new SerializedObject(health);
                    hso.FindProperty("maxHp").floatValue = enemyData.maxHp;
                    hso.ApplyModifiedProperties();
                }
                count++;
            }
        }

        Debug.Log($"[Enemy Data] '{enemyData.displayName}' 스탯을 씬 적 {count}마리에 적용!");
    }

    void SyncToCombatData(EnemyData enemyData)
    {
        if (combatData == null)
        {
            combatData = AssetDatabase.LoadAssetAtPath<CombatData>(COMBAT_DATA_PATH);
            if (combatData == null) return;
        }

        combatData.enemy = enemyData.ToCombatEnemyStats();
        EditorUtility.SetDirty(combatData);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Enemy Data] '{enemyData.displayName}' → CombatData.enemy 동기화 완료!");
    }

    // ================================================================
    //  UI 헬퍼
    // ================================================================
    void DrawSlider(SerializedProperty prop, string label, float min, float max)
    {
        if (prop == null) return;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(150));
        prop.floatValue = EditorGUILayout.Slider(prop.floatValue, min, max);
        EditorGUILayout.EndHorizontal();
    }

    void DrawInfoBox(string text)
    {
        var style = new GUIStyle(EditorStyles.helpBox) { richText = true, fontSize = 11 };
        EditorGUILayout.LabelField(text, style, GUILayout.MinHeight(22));
    }

    void DrawColorPreview(EnemyData enemy)
    {
        EditorGUILayout.LabelField("Color Preview", EditorStyles.miniLabel);
        Rect r = EditorGUILayout.GetControlRect(false, 24);

        float segWidth = r.width / 3f;

        // Body tint
        Rect bodyRect = new Rect(r.x, r.y, segWidth - 2, r.height);
        EditorGUI.DrawRect(bodyRect, enemy.tintColor);
        var labelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = GetContrastColor(enemy.tintColor) }
        };
        EditorGUI.LabelField(bodyRect, "Body", labelStyle);

        // Shadow
        Rect shadowRect = new Rect(r.x + segWidth, r.y, segWidth - 2, r.height);
        EditorGUI.DrawRect(shadowRect, enemy.shadowColor);
        labelStyle.normal.textColor = Color.white;
        EditorGUI.LabelField(shadowRect, "Shadow", labelStyle);

        // Glow
        Rect glowRect = new Rect(r.x + segWidth * 2, r.y, segWidth - 2, r.height);
        Color glowPreview = enemy.useGlow ? enemy.glowColor : new Color(0.2f, 0.2f, 0.2f);
        EditorGUI.DrawRect(glowRect, glowPreview);
        labelStyle.normal.textColor = GetContrastColor(glowPreview);
        EditorGUI.LabelField(glowRect, enemy.useGlow ? "Glow" : "No Glow", labelStyle);
    }

    static Color GetContrastColor(Color c)
    {
        float luma = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
        return luma > 0.5f ? Color.black : Color.white;
    }

    void DrawProgressBar(string label, float value, Color color)
    {
        Rect r = EditorGUILayout.GetControlRect(false, 20);
        EditorGUI.DrawRect(r, new Color(0.15f, 0.15f, 0.15f));

        Rect fill = r;
        fill.width *= Mathf.Clamp01(value);
        EditorGUI.DrawRect(fill, color);

        var style = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold,
            fontSize = 10
        };
        EditorGUI.LabelField(r, label, style);
    }
}
