using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// StatDB 통합 에디터 윈도우.
/// 플레이어 스탯 + 유닛 스탯 관리. 키 기반 조회.
/// </summary>
public class StatDBEditor : EditorWindow
{
    const string DB_PATH = "Assets/Resources/Data/StatDB.asset";

    StatDB db;
    SerializedObject so;
    Vector2 leftScroll, rightScroll;

    // 탭
    int mainTab; // 0=Player, 1=Units
    string[] mainTabNames = { "PLAYER", "UNITS" };

    // 유닛 패널
    int selectedUnit = -1;
    string searchFilter = "";

    // 유닛 상세 섹션 토글
    bool showVisual = true;
    bool showCombat = true;
    bool showGroggy = false;
    bool showMovement = true;
    bool showDetection = false;
    bool showAI = false;
    bool showReward = false;

    // 플레이어 섹션 토글
    bool showPMove = true;
    bool showPSprint = true;
    bool showPLight = true;
    bool showPHeavy = false;
    bool showPDodge = false;
    bool showPStamina = true;

    [MenuItem("Tools/Dev Tools/Data/Stat DB Editor")]
    static void Open()
    {
        var win = GetWindow<StatDBEditor>("Stat DB");
        win.minSize = new Vector2(650, 500);
        win.LoadOrCreate();
    }

    void OnEnable() => LoadOrCreate();
    void OnFocus() => LoadOrCreate();

    void LoadOrCreate()
    {
        db = AssetDatabase.LoadAssetAtPath<StatDB>(DB_PATH);
        if (db == null)
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Data");
            db = ScriptableObject.CreateInstance<StatDB>();
            AssetDatabase.CreateAsset(db, DB_PATH);
            AssetDatabase.SaveAssets();
        }
        so = new SerializedObject(db);
        StatDB.SetInstance(db);
    }

    void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            int lastSlash = path.LastIndexOf('/');
            string parent = path.Substring(0, lastSlash);
            string folder = path.Substring(lastSlash + 1);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    void OnGUI()
    {
        if (db == null) { LoadOrCreate(); return; }
        so.Update();

        // 헤더
        EditorGUILayout.Space(6);
        DrawHeader("STAT DB");
        EditorGUILayout.Space(4);

        // 메인 탭
        mainTab = GUILayout.Toolbar(mainTab, mainTabNames, GUILayout.Height(26));
        EditorGUILayout.Space(4);

        if (mainTab == 0)
            DrawPlayerTab();
        else
            DrawUnitsTab();

        so.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(db);
    }

    // ================================================================
    //  Player 탭
    // ================================================================
    void DrawPlayerTab()
    {
        rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

        var playerProp = so.FindProperty("playerStat");

        // HP
        EditorGUILayout.LabelField("HEALTH", EditorStyles.boldLabel);
        DrawProp(playerProp, "maxHp", "Max HP");
        EditorGUILayout.Space(4);

        // 이동
        showPMove = EditorGUILayout.BeginFoldoutHeaderGroup(showPMove, "MOVEMENT");
        if (showPMove)
        {
            DrawProp(playerProp, "moveSpeed", "Move Speed");
            DrawProp(playerProp, "sprintSpeedMultiplier", "Sprint Multiplier");
            DrawProp(playerProp, "crouchSpeedMultiplier", "Crouch Multiplier");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // 달리기
        showPSprint = EditorGUILayout.BeginFoldoutHeaderGroup(showPSprint, "SPRINT");
        if (showPSprint)
        {
            DrawProp(playerProp, "sprintStaminaCost", "Stamina Cost/s");
            DrawProp(playerProp, "sprintMinStamina", "Min Stamina to Sprint");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // 약공격
        showPLight = EditorGUILayout.BeginFoldoutHeaderGroup(showPLight, "LIGHT ATTACK");
        if (showPLight)
        {
            DrawProp(playerProp, "lightDamage", "Damage");
            DrawProp(playerProp, "lightRange", "Range");
            DrawProp(playerProp, "lightStaminaCost", "Stamina Cost");
            DrawProp(playerProp, "lightGroggy", "Groggy");
            DrawProp(playerProp, "lightCooldown", "Cooldown");
            DrawProp(playerProp, "lightComboMax", "Max Combo");
            DrawProp(playerProp, "lightComboWindow", "Combo Window");
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Combo 2", EditorStyles.miniLabel);
            DrawProp(playerProp, "lightCombo2Damage", "  Damage");
            DrawProp(playerProp, "lightCombo2Groggy", "  Groggy");
            EditorGUILayout.LabelField("Combo 3", EditorStyles.miniLabel);
            DrawProp(playerProp, "lightCombo3Damage", "  Damage");
            DrawProp(playerProp, "lightCombo3Groggy", "  Groggy");
            DrawProp(playerProp, "lightCombo3StaminaCost", "  Stamina Cost");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // 강공격
        showPHeavy = EditorGUILayout.BeginFoldoutHeaderGroup(showPHeavy, "HEAVY ATTACK");
        if (showPHeavy)
        {
            DrawProp(playerProp, "heavyDamage", "Damage");
            DrawProp(playerProp, "heavyRange", "Range");
            DrawProp(playerProp, "heavyStaminaCost", "Stamina Cost");
            DrawProp(playerProp, "heavyGroggy", "Groggy");
            DrawProp(playerProp, "heavyChargeTime", "Min Charge Time");
            DrawProp(playerProp, "heavyMaxCharge", "Max Charge Time");
            DrawProp(playerProp, "heavyFullDamage", "Full Charge Damage");
            DrawProp(playerProp, "heavyFullStaminaCost", "Full Charge Stamina");
            DrawProp(playerProp, "heavyFullGroggy", "Full Charge Groggy");
            DrawProp(playerProp, "heavyCooldown", "Cooldown");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // 구르기
        showPDodge = EditorGUILayout.BeginFoldoutHeaderGroup(showPDodge, "DODGE");
        if (showPDodge)
        {
            DrawProp(playerProp, "dodgeStaminaCost", "Stamina Cost");
            DrawProp(playerProp, "dodgeDistance", "Distance");
            DrawProp(playerProp, "dodgeDuration", "Duration");
            DrawProp(playerProp, "dodgeInvincibleDuration", "I-Frame Duration");
            DrawProp(playerProp, "dodgeCooldown", "Cooldown");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // 스태미너
        showPStamina = EditorGUILayout.BeginFoldoutHeaderGroup(showPStamina, "STAMINA");
        if (showPStamina)
        {
            DrawProp(playerProp, "maxStamina", "Max Stamina");
            DrawProp(playerProp, "staminaRegen", "Regen / sec");
            DrawProp(playerProp, "staminaRegenDelay", "Regen Delay");
            DrawProp(playerProp, "exhaustionDuration", "Exhaustion Duration");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.EndScrollView();
    }

    // ================================================================
    //  Units 탭
    // ================================================================
    void DrawUnitsTab()
    {
        EditorGUILayout.BeginHorizontal();

        // === 좌측: 유닛 리스트 ===
        EditorGUILayout.BeginVertical(GUILayout.Width(200));
        DrawUnitList();
        EditorGUILayout.EndVertical();

        // 구분선
        GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));

        // === 우측: 유닛 상세 ===
        EditorGUILayout.BeginVertical();
        DrawUnitDetail();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    void DrawUnitList()
    {
        EditorGUILayout.LabelField("UNIT LIST", EditorStyles.boldLabel);

        // 검색
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);

        leftScroll = EditorGUILayout.BeginScrollView(leftScroll);

        var units = db.units;
        for (int i = 0; i < units.Count; i++)
        {
            var u = units[i];
            if (!string.IsNullOrEmpty(searchFilter) &&
                !u.id.ToLower().Contains(searchFilter.ToLower()) &&
                !u.displayName.ToLower().Contains(searchFilter.ToLower()))
                continue;

            EditorGUILayout.BeginHorizontal();

            // 색상 인디케이터
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = u.tintColor;
            GUILayout.Box("", GUILayout.Width(12), GUILayout.Height(18));
            GUI.backgroundColor = prevBg;

            // 선택 버튼
            bool selected = (selectedUnit == i);
            var style = selected ? EditorStyles.boldLabel : EditorStyles.label;
            if (GUILayout.Button($"{u.id}", style))
                selectedUnit = i;

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);

        // 추가/삭제 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ New"))
        {
            Undo.RecordObject(db, "Add Unit");
            var newUnit = new UnitStatData { id = $"unit_{units.Count}", displayName = $"Unit {units.Count}" };
            units.Add(newUnit);
            selectedUnit = units.Count - 1;
            db.RefreshCache();
        }
        if (GUILayout.Button("Duplicate") && selectedUnit >= 0 && selectedUnit < units.Count)
        {
            Undo.RecordObject(db, "Duplicate Unit");
            var src = units[selectedUnit];
            var json = JsonUtility.ToJson(src);
            var copy = JsonUtility.FromJson<UnitStatData>(json);
            copy.id = src.id + "_copy";
            copy.displayName = src.displayName + " Copy";
            units.Add(copy);
            selectedUnit = units.Count - 1;
            db.RefreshCache();
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Delete") && selectedUnit >= 0 && selectedUnit < units.Count)
        {
            if (EditorUtility.DisplayDialog("Delete", $"'{units[selectedUnit].id}' 삭제?", "Delete", "Cancel"))
            {
                Undo.RecordObject(db, "Delete Unit");
                units.RemoveAt(selectedUnit);
                selectedUnit = Mathf.Min(selectedUnit, units.Count - 1);
                db.RefreshCache();
            }
        }

        EditorGUILayout.Space(4);

        // 프리셋 추가
        EditorGUILayout.LabelField("ADD PRESET", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Melee", EditorStyles.miniButton))
            AddPreset(UnitStatData.PresetMelee());
        if (GUILayout.Button("Ranged", EditorStyles.miniButton))
            AddPreset(UnitStatData.PresetRanged());
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Tank", EditorStyles.miniButton))
            AddPreset(UnitStatData.PresetTank());
        if (GUILayout.Button("Boss", EditorStyles.miniButton))
            AddPreset(UnitStatData.PresetBoss());
        EditorGUILayout.EndHorizontal();
    }

    void AddPreset(UnitStatData preset)
    {
        Undo.RecordObject(db, "Add Preset");
        // 중복 ID 방지
        int suffix = 0;
        string baseId = preset.id;
        while (db.GetUnit(preset.id) != null)
        {
            suffix++;
            preset.id = $"{baseId}_{suffix}";
        }
        db.units.Add(preset);
        selectedUnit = db.units.Count - 1;
        db.RefreshCache();
    }

    void DrawUnitDetail()
    {
        if (selectedUnit < 0 || selectedUnit >= db.units.Count)
        {
            EditorGUILayout.HelpBox("좌측에서 유닛을 선택하세요.", MessageType.Info);
            return;
        }

        rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

        var unitsProp = so.FindProperty("units");
        var unitProp = unitsProp.GetArrayElementAtIndex(selectedUnit);
        var unit = db.units[selectedUnit];

        // ID & 이름
        EditorGUILayout.LabelField("IDENTITY", EditorStyles.boldLabel);
        var idProp = unitProp.FindPropertyRelative("id");
        string prevId = idProp.stringValue;
        EditorGUILayout.PropertyField(idProp, new GUIContent("ID (Key)"));
        if (idProp.stringValue != prevId) db.RefreshCache();
        EditorGUILayout.PropertyField(unitProp.FindPropertyRelative("displayName"), new GUIContent("Display Name"));

        EditorGUILayout.Space(4);

        // 비주얼
        showVisual = EditorGUILayout.BeginFoldoutHeaderGroup(showVisual, "VISUAL");
        if (showVisual)
        {
            EditorGUI.indentLevel++;
            DrawUProp(unitProp, "scale", "Scale");
            DrawUProp(unitProp, "tintColor", "Tint Color");
            DrawUProp(unitProp, "shadowColor", "Shadow Color");
            DrawUProp(unitProp, "useGlow", "Use Glow");
            if (unit.useGlow)
            {
                DrawUProp(unitProp, "glowColor", "  Glow Color");
                DrawUProp(unitProp, "glowIntensity", "  Glow Intensity");
                DrawUProp(unitProp, "glowRange", "  Glow Range");
            }
            DrawUProp(unitProp, "useTrailParticle", "Use Trail");
            if (unit.useTrailParticle)
                DrawUProp(unitProp, "trailColor", "  Trail Color");

            DrawUProp(unitProp, "generatedPrefab", "Generated Prefab");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // 전투
        showCombat = EditorGUILayout.BeginFoldoutHeaderGroup(showCombat, "COMBAT");
        if (showCombat)
        {
            EditorGUI.indentLevel++;
            DrawUProp(unitProp, "maxHp", "Max HP");
            DrawUProp(unitProp, "attackDamage", "Attack Damage");
            DrawUProp(unitProp, "attackRange", "Attack Range");
            DrawUProp(unitProp, "attackSpeed", "Attack Speed (hits/s)");
            DrawUProp(unitProp, "attackWindup", "Attack Windup");
            DrawUProp(unitProp, "canBeCancelled", "Can Be Cancelled");

            // 계산 표시
            EditorGUILayout.Space(2);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("DPS", unit.DPS);
            EditorGUILayout.FloatField("Attack Cooldown", unit.AttackCooldown);
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // 그로기
        showGroggy = EditorGUILayout.BeginFoldoutHeaderGroup(showGroggy, "GROGGY");
        if (showGroggy)
        {
            EditorGUI.indentLevel++;
            DrawUProp(unitProp, "maxGroggy", "Max Groggy");
            DrawUProp(unitProp, "groggyDecay", "Decay / sec");
            DrawUProp(unitProp, "groggyStunDuration", "Stun Duration");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // 이동
        showMovement = EditorGUILayout.BeginFoldoutHeaderGroup(showMovement, "MOVEMENT");
        if (showMovement)
        {
            EditorGUI.indentLevel++;
            DrawUProp(unitProp, "moveSpeed", "Move Speed");
            DrawUProp(unitProp, "patrolSpeed", "Patrol Speed");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // 감지
        showDetection = EditorGUILayout.BeginFoldoutHeaderGroup(showDetection, "DETECTION");
        if (showDetection)
        {
            EditorGUI.indentLevel++;
            DrawUProp(unitProp, "detectRange", "Detect Range");
            DrawUProp(unitProp, "loseRange", "Lose Range");
            DrawUProp(unitProp, "patrolRadius", "Patrol Radius");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // AI
        showAI = EditorGUILayout.BeginFoldoutHeaderGroup(showAI, "AI BEHAVIOR");
        if (showAI)
        {
            EditorGUI.indentLevel++;
            DrawUProp(unitProp, "patrolWaitTime", "Patrol Wait Time");
            DrawUProp(unitProp, "hitStunDuration", "Hit Stun Duration");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // 보상
        showReward = EditorGUILayout.BeginFoldoutHeaderGroup(showReward, "REWARDS");
        if (showReward)
        {
            EditorGUI.indentLevel++;
            DrawUProp(unitProp, "expReward", "EXP Reward");
            DrawUProp(unitProp, "goldReward", "Gold Reward");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.EndScrollView();
    }

    // ================================================================
    //  UI 헬퍼
    // ================================================================
    void DrawHeader(string text)
    {
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField(text, style);
    }

    void DrawProp(SerializedProperty parent, string name, string label)
    {
        var prop = parent.FindPropertyRelative(name);
        if (prop != null)
            EditorGUILayout.PropertyField(prop, new GUIContent(label));
    }

    void DrawUProp(SerializedProperty unitProp, string name, string label)
    {
        var prop = unitProp.FindPropertyRelative(name);
        if (prop != null)
            EditorGUILayout.PropertyField(prop, new GUIContent(label));
    }
}
