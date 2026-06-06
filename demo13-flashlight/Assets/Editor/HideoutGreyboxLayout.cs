#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 컨테이너 은신처 '실내' 그레이박스 씬(Hideout.unity). (docs/safehouse.md 실내 / story-script S-011)
///   박스 침상(수면) + 부서진 작업대 + 시설 슬롯(조리대/의료대) + 안전가옥으로 나가는 출구.
/// 안전가옥(Safehouse)의 은신처 입구(ExitPoint→"Hideout")에서 씬 전환으로 진입, 출구로 복귀(→"Safehouse"/from_hideout).
/// 콘텐츠 전용(Systems가 카메라/조명/매니저/플레이어 공급). 빌드세팅에 Hideout 자동 등록.
///
/// 메뉴: Tools ▸ TopDown ▸ Map ▸ Build Hideout Greybox Layout
/// </summary>
public static class HideoutGreyboxLayout
{
    const string ScenePath  = "Assets/Scenes/Hideout.unity";
    const string PrefabRoot = "Props2D/Prefabs/";
    const float MapW = 14f, MapH = 9f;

    static readonly string[] RequiredPrefabIds =
    {
        "gb_floor", "gb_wall", "gb_spawn", "gb_exit",
        "gb_bed", "gb_workbench", "gb_medbench", "gb_cookbench",
    };

    [MenuItem("Tools/TopDown/Map/Build Hideout Greybox Layout")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var map = new GameObject("Map");
        int n = 0;

        EnsureGreyboxPalette();

        // 바닥 + 외곽(컨테이너 = 좁은 금속 박스)
        n += Floor(map, "Floor", 7f, 4.5f, MapW, MapH);
        n += Wall(map, "Wall_S", 7f,  0.5f, 14f, 1f);
        n += Wall(map, "Wall_N", 7f,  8.5f, 14f, 1f);
        n += Wall(map, "Wall_W", 0.5f,4.5f,  1f, 9f);
        n += Wall(map, "Wall_E",13.5f,4.5f,  1f, 9f);

        // 진입 스폰(안전가옥→여기) + 출구(여기→안전가옥/from_hideout)
        n += Spawn(map, "default", 3f, 4.5f);
        n += Exit (map, "Exit_ToSafehouse", 1.5f, 4.5f, "Safehouse", "from_hideout");

        // 시설: 박스 침상 + 부서진 작업대 + 슬롯(조리대/의료대)
        n += Marker(map, "gb_bed",       "Bed_BoxCot",      5f, 6.5f);
        n += Marker(map, "gb_workbench", "Workbench_Broken",9f, 6.5f);
        n += Marker(map, "gb_cookbench", "CookingBench",   11f, 2.5f);
        n += Marker(map, "gb_medbench",  "MedicalBench",    5f, 2.5f);

        Selection.activeObject = null;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            Debug.LogError("[HideoutGB] 씬 저장 실패: " + ScenePath);
            return;
        }
        AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=cyan>[HideoutGB]</color> 생성 완료: {ScenePath} — 그레이박스 {n}개(침상/작업대/슬롯/스폰/출구). 빌드세팅 등록.\n" +
                  "  • 안전가옥 은신처 입구(ExitPoint→Hideout)로 진입, 출구로 복귀(→Safehouse/from_hideout).");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Hideout Greybox",
                $"{ScenePath} 생성 + 빌드세팅 등록 완료.\n\n박스 침상 + 부서진 작업대 + 조리대/의료대 슬롯 + 진입 스폰 + 출구(→Safehouse).\n" +
                "안전가옥 그레이박스의 은신처 입구가 여기로 씬 전환합니다.", "확인");
    }

    static void EnsureGreyboxPalette()
    {
        foreach (var id in RequiredPrefabIds)
            if (Resources.Load<GameObject>(PrefabRoot + id) == null) { GreyboxPaletteBuilder.Generate(); return; }
    }

    static void AddToBuildSettings(string scenePath)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == scenePath) return;
        list.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ── 헬퍼 (SafehouseGreyboxLayout과 동일) ──
    static int Wall(GameObject parent, string name, float cx, float cy, float lenX, float thickY)
    {
        var go = Inst("gb_wall", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(cx, cy, 0f);
        go.transform.localScale    = new Vector3(lenX, thickY, 1f);
        CounterScaleLabel(go);
        return 1;
    }

    static int Floor(GameObject parent, string name, float cx, float cy, float w, float h)
    {
        var go = Inst("gb_floor", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(cx, cy, 0f);
        go.transform.localScale    = new Vector3(w, h, 1f);
        CounterScaleLabel(go);
        return 1;
    }

    static int Marker(GameObject parent, string prefabId, string name, float x, float y)
    {
        var go = Inst(prefabId, name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);
        return 1;
    }

    static int Spawn(GameObject parent, string pointId, float x, float y)
    {
        var go = Inst("gb_spawn", "Spawn_" + pointId, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);
        var sp = go.GetComponentInChildren<SpawnPoint>();
        if (sp != null)
        {
            var so = new SerializedObject(sp);
            var p = so.FindProperty("pointId");
            if (p != null) { p.stringValue = pointId; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
        return 1;
    }

    static int Exit(GameObject parent, string name, float x, float y, string targetScene, string spawnId)
    {
        var go = Inst("gb_exit", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);
        var io = go.GetComponentInChildren<InteractableObject>();
        if (io != null)
        {
            var so = new SerializedObject(io);
            var ts = so.FindProperty("targetScene");   if (ts != null) ts.stringValue = targetScene;
            var sp = so.FindProperty("spawnPointId");   if (sp != null) sp.stringValue = spawnId;
            var ew = so.FindProperty("exitWaitTime");   if (ew != null) ew.floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return 1;
    }

    static GameObject Inst(string prefabId, string name, GameObject parent)
    {
        var prefab = Resources.Load<GameObject>(PrefabRoot + prefabId);
        if (prefab == null) { Debug.LogError($"[HideoutGB] 프리팹 로드 실패: Resources/{PrefabRoot}{prefabId}"); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        go.name = name;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;
        return go;
    }

    static void CounterScaleLabel(GameObject go)
    {
        var label = go.transform.Find("Label");
        if (label == null) return;
        var s = go.transform.localScale;
        float ix = Mathf.Approximately(s.x, 0f) ? 1f : 1f / s.x;
        float iy = Mathf.Approximately(s.y, 0f) ? 1f : 1f / s.y;
        label.localScale = new Vector3(ix, iy, 1f);
    }
}
#endif
