using UnityEngine;
using UnityEditor;

/// <summary>
/// 게임 설정 에디터 윈도우.
/// 카메라, 스폰존 등을 슬라이더로 조절.
/// </summary>
public class GameSettingsEditor : EditorWindow
{
    const string DATA_PATH = "Assets/Settings/GameSettings.asset";

    GameSettings data;
    Vector2 scrollPos;
    bool showCamera = true;
    bool showSpawn = true;

    [MenuItem("Tools/Game Settings Editor")]
    static void Open()
    {
        var win = GetWindow<GameSettingsEditor>("Game Settings");
        win.minSize = new Vector2(380, 450);
        win.LoadOrCreateData();
    }

    void OnEnable() => LoadOrCreateData();

    void LoadOrCreateData()
    {
        data = AssetDatabase.LoadAssetAtPath<GameSettings>(DATA_PATH);
        if (data == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");
            data = ScriptableObject.CreateInstance<GameSettings>();
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

        EditorGUILayout.Space(8);
        DrawHeader("GAME SETTINGS");
        EditorGUILayout.Space(4);

        // ===== Camera =====
        showCamera = EditorGUILayout.BeginFoldoutHeaderGroup(showCamera, "CAMERA");
        if (showCamera)
        {
            EditorGUI.indentLevel++;
            DrawSlider(so.FindProperty("orthoSize"), "Ortho Size", 3, 20);
            DrawSlider(so.FindProperty("cameraAngle"), "Angle (X)", 10, 80);
            DrawSlider(so.FindProperty("cameraDistance"), "Distance", 5, 50);
            DrawSlider(so.FindProperty("cameraYaw"), "Yaw (Y)", -45, 45);
            DrawSlider(so.FindProperty("cameraSmoothSpeed"), "Smooth Speed", 1, 20);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Apply to Scene Camera"))
                ApplyCameraToScene();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(8);

        // ===== Spawn Zones =====
        showSpawn = EditorGUILayout.BeginFoldoutHeaderGroup(showSpawn, "SPAWN ZONES");
        if (showSpawn)
        {
            EditorGUI.indentLevel++;
            var zones = so.FindProperty("spawnZones");

            for (int i = 0; i < zones.arraySize; i++)
            {
                EditorGUILayout.BeginVertical("box");
                var zone = zones.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Zone {i}", EditorStyles.boldLabel, GUILayout.Width(60));
                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    zones.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(zone.FindPropertyRelative("position"), new GUIContent("Position"));
                EditorGUILayout.PropertyField(zone.FindPropertyRelative("size"), new GUIContent("Size"));

                var countProp = zone.FindPropertyRelative("enemyCount");
                countProp.intValue = EditorGUILayout.IntSlider("Enemy Count", countProp.intValue, 1, 10);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Zone", GUILayout.Height(24)))
            {
                zones.InsertArrayElementAtIndex(zones.arraySize);
                var newZone = zones.GetArrayElementAtIndex(zones.arraySize - 1);
                newZone.FindPropertyRelative("position").vector3Value = new Vector3(6, 0, 5);
                newZone.FindPropertyRelative("size").vector3Value = new Vector3(4, 0, 4);
                newZone.FindPropertyRelative("enemyCount").intValue = 2;
            }
            if (GUILayout.Button("Apply Zones to Scene", GUILayout.Height(24)))
            {
                so.ApplyModifiedProperties();
                ApplySpawnZonesToScene();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(12);

        // ===== Info =====
        int totalEnemies = 0;
        if (data.spawnZones != null)
            foreach (var z in data.spawnZones) totalEnemies += z.enemyCount;

        DrawInfoBox(
            $"Total spawn zones: {(data.spawnZones != null ? data.spawnZones.Length : 0)}\n" +
            $"Total enemies: {totalEnemies}\n" +
            $"Camera: {data.orthoSize:F1} ortho, {data.cameraAngle:F0}° angle");

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Reset to Defaults", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog("Reset", "기본값으로 초기화?", "Yes", "Cancel"))
            {
                data.orthoSize = 7f;
                data.cameraAngle = 55f;
                data.cameraDistance = 20f;
                data.cameraYaw = 0f;
                data.cameraSmoothSpeed = 8f;
                data.spawnZones = new GameSettings.SpawnZoneData[]
                {
                    new GameSettings.SpawnZoneData { position = new Vector3(3, 0, 7), size = new Vector3(4, 0, 4), enemyCount = 2 },
                    new GameSettings.SpawnZoneData { position = new Vector3(10, 0, 4), size = new Vector3(3, 0, 3), enemyCount = 1 },
                };
                EditorUtility.SetDirty(data);
            }
        }

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
        EditorGUILayout.LabelField(label, GUILayout.Width(140));
        prop.floatValue = EditorGUILayout.Slider(prop.floatValue, min, max);
        EditorGUILayout.EndHorizontal();
    }

    void DrawInfoBox(string text)
    {
        var style = new GUIStyle(EditorStyles.helpBox) { richText = true, fontSize = 11 };
        EditorGUILayout.LabelField(text, style, GUILayout.MinHeight(44));
    }

    void ApplyCameraToScene()
    {
        var cam = Camera.main;
        if (cam == null) { Debug.LogWarning("Main Camera not found"); return; }

        cam.orthographicSize = data.orthoSize;
        Quaternion rot = Quaternion.Euler(data.cameraAngle, data.cameraYaw, 0);
        cam.transform.rotation = rot;

        var follow = cam.GetComponent<CameraFollow>();
        if (follow != null)
        {
            var followSO = new SerializedObject(follow);
            followSO.FindProperty("gameSettings").objectReferenceValue = data;
            followSO.FindProperty("orthoSize").floatValue = data.orthoSize;
            followSO.FindProperty("cameraAngle").floatValue = data.cameraAngle;
            followSO.FindProperty("cameraDistance").floatValue = data.cameraDistance;
            followSO.FindProperty("cameraYaw").floatValue = data.cameraYaw;
            followSO.FindProperty("smoothSpeed").floatValue = data.cameraSmoothSpeed;
            followSO.ApplyModifiedProperties();
        }

        Debug.Log("[Game Settings] 카메라 설정 적용 완료!");
    }

    void ApplySpawnZonesToScene()
    {
        // 기존 스폰존 제거
        var oldZones = GameObject.Find("SpawnZones");
        if (oldZones != null) Object.DestroyImmediate(oldZones);

        var root = new GameObject("SpawnZones");
        Undo.RegisterCreatedObjectUndo(root, "Create SpawnZones");

        if (data.spawnZones == null) return;

        for (int i = 0; i < data.spawnZones.Length; i++)
        {
            var zd = data.spawnZones[i];
            var go = new GameObject($"SpawnZone_{i}");
            go.transform.SetParent(root.transform);
            go.transform.position = zd.position;

            var zone = go.AddComponent<SpawnZone>();
            var zoneSO = new SerializedObject(zone);
            zoneSO.FindProperty("size").vector3Value = zd.size;
            zoneSO.FindProperty("enemyCount").intValue = zd.enemyCount;
            zoneSO.ApplyModifiedProperties();
        }

        Debug.Log($"[Game Settings] 스폰존 {data.spawnZones.Length}개 생성!");
    }
}
