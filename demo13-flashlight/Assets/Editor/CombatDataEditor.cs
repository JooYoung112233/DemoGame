using UnityEngine;
using UnityEditor;

/// <summary>
/// 전투 밸런스 에디터 윈도우.
/// CombatData ScriptableObject를 시각적으로 편집.
/// </summary>
public class CombatDataEditor : EditorWindow
{
    const string DATA_PATH = "Assets/Settings/CombatData.asset";

    CombatData data;
    Vector2 scrollPos;
    bool showPlayer = true;
    bool showEnemy = true;

    [MenuItem("Tools/Combat Data Editor")]
    static void Open()
    {
        var win = GetWindow<CombatDataEditor>("Combat Data");
        win.minSize = new Vector2(350, 500);
        win.LoadOrCreateData();
    }

    void OnEnable() => LoadOrCreateData();

    void LoadOrCreateData()
    {
        data = AssetDatabase.LoadAssetAtPath<CombatData>(DATA_PATH);
        if (data == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");
            data = ScriptableObject.CreateInstance<CombatData>();
            AssetDatabase.CreateAsset(data, DATA_PATH);
            AssetDatabase.SaveAssets();
        }
    }

    void OnGUI()
    {
        if (data == null) { LoadOrCreateData(); return; }

        var so = new SerializedObject(data);
        so.Update();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ===== 헤더 =====
        EditorGUILayout.Space(8);
        DrawHeader("COMBAT DATA EDITOR");
        EditorGUILayout.Space(4);

        // ===== Player =====
        showPlayer = EditorGUILayout.BeginFoldoutHeaderGroup(showPlayer, "PLAYER");
        if (showPlayer)
        {
            EditorGUI.indentLevel++;
            var player = so.FindProperty("player");

            DrawSlider(player.FindPropertyRelative("maxHp"), "Max HP", 50, 500);
            DrawSlider(player.FindPropertyRelative("attackDamage"), "Attack Damage", 1, 100);
            DrawSlider(player.FindPropertyRelative("attackRange"), "Attack Range", 1, 10);
            DrawSlider(player.FindPropertyRelative("attackSpeed"), "Attack Speed (hits/s)", 0.1f, 5f);
            DrawSlider(player.FindPropertyRelative("moveSpeed"), "Move Speed", 1, 15);

            EditorGUILayout.Space(4);
            DrawInfoBox(
                $"Cooldown: {1f / Mathf.Max(data.player.attackSpeed, 0.1f):F2}s\n" +
                $"DPS: {data.player.attackDamage * data.player.attackSpeed:F1}");

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(8);

        // ===== Enemy =====
        showEnemy = EditorGUILayout.BeginFoldoutHeaderGroup(showEnemy, "ENEMY");
        if (showEnemy)
        {
            EditorGUI.indentLevel++;
            var enemy = so.FindProperty("enemy");

            DrawSlider(enemy.FindPropertyRelative("maxHp"), "Max HP", 10, 300);
            DrawSlider(enemy.FindPropertyRelative("attackDamage"), "Attack Damage", 1, 80);
            DrawSlider(enemy.FindPropertyRelative("attackRange"), "Attack Range", 0.5f, 8);
            DrawSlider(enemy.FindPropertyRelative("attackSpeed"), "Attack Speed (hits/s)", 0.1f, 3f);
            DrawSlider(enemy.FindPropertyRelative("moveSpeed"), "Move Speed", 0.5f, 10);
            DrawSlider(enemy.FindPropertyRelative("patrolSpeed"), "Patrol Speed", 0.5f, 5);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Detection", EditorStyles.boldLabel);
            DrawSlider(enemy.FindPropertyRelative("detectRange"), "Detect Range", 2, 20);
            DrawSlider(enemy.FindPropertyRelative("loseRange"), "Lose Range", 5, 30);
            DrawSlider(enemy.FindPropertyRelative("patrolRadius"), "Patrol Radius", 1, 15);

            EditorGUILayout.Space(4);
            DrawInfoBox(
                $"Cooldown: {1f / Mathf.Max(data.enemy.attackSpeed, 0.1f):F2}s\n" +
                $"DPS: {data.enemy.attackDamage * data.enemy.attackSpeed:F1}\n" +
                $"Hits to kill player: {Mathf.Ceil(data.player.maxHp / Mathf.Max(data.enemy.attackDamage, 0.1f))}\n" +
                $"Player hits to kill: {Mathf.Ceil(data.enemy.maxHp / Mathf.Max(data.player.attackDamage, 0.1f))}");

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(12);

        // ===== 버튼 =====
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset to Defaults", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog("Reset", "기본값으로 초기화?", "Yes", "Cancel"))
            {
                data.player = new CombatData.PlayerStats();
                data.enemy = new CombatData.EnemyStats();
                EditorUtility.SetDirty(data);
            }
        }

        if (GUILayout.Button("Apply to Scene", GUILayout.Height(28)))
        {
            ApplyToScene();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // ===== Comparison =====
        DrawComparisonBar();

        EditorGUILayout.EndScrollView();

        so.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(data);
    }

    void DrawHeader(string text)
    {
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField(text, style);
    }

    void DrawSlider(SerializedProperty prop, string label, float min, float max)
    {
        if (prop == null) return;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(160));
        prop.floatValue = EditorGUILayout.Slider(prop.floatValue, min, max);
        EditorGUILayout.EndHorizontal();
    }

    void DrawInfoBox(string text)
    {
        var style = new GUIStyle(EditorStyles.helpBox)
        {
            richText = true,
            fontSize = 11
        };
        EditorGUILayout.LabelField(text, style, GUILayout.MinHeight(50));
    }

    void DrawComparisonBar()
    {
        if (data == null) return;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Balance Overview", EditorStyles.boldLabel);

        // Player DPS vs Enemy HP
        float playerDPS = data.player.attackDamage * data.player.attackSpeed;
        float enemyKillTime = data.enemy.maxHp / Mathf.Max(playerDPS, 0.1f);

        float enemyDPS = data.enemy.attackDamage * data.enemy.attackSpeed;
        float playerKillTime = data.player.maxHp / Mathf.Max(enemyDPS, 0.1f);

        DrawProgressBar($"Enemy kill time: {enemyKillTime:F1}s", enemyKillTime / 10f, Color.green);
        DrawProgressBar($"Player kill time: {playerKillTime:F1}s", playerKillTime / 10f, Color.red);
    }

    void DrawProgressBar(string label, float value, Color color)
    {
        Rect r = EditorGUILayout.GetControlRect(false, 20);
        EditorGUI.DrawRect(r, new Color(0.2f, 0.2f, 0.2f));

        Rect fill = r;
        fill.width *= Mathf.Clamp01(value);
        EditorGUI.DrawRect(fill, color);

        var style = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold
        };
        EditorGUI.LabelField(r, label, style);
    }

    void ApplyToScene()
    {
        // Player
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            var health = playerGO.GetComponent<Health>();
            if (health != null)
            {
                var hso = new SerializedObject(health);
                hso.FindProperty("maxHp").floatValue = data.player.maxHp;
                hso.ApplyModifiedProperties();
            }
        }

        // Enemies
        var enemies = Object.FindObjectsOfType<EnemyAI>();
        foreach (var e in enemies)
        {
            var health = e.GetComponent<Health>();
            if (health != null)
            {
                var hso = new SerializedObject(health);
                hso.FindProperty("maxHp").floatValue = data.enemy.maxHp;
                hso.ApplyModifiedProperties();
            }
        }

        Debug.Log("[Combat Data] 씬에 스탯 적용 완료!");
    }
}
