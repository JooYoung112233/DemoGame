#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 컨테이너 은신처 '실내' 그레이박스 씬(Hideout.unity) — 타르코프식 화면. (docs/safehouse.md 실내 / story-script S-011)
///   박스 침상(수면) + 부서진 작업대 + 시설 슬롯(조리대/의료대). 캐릭터로 걷지 않고 HideoutController가
///   플레이어를 숨기고 카메라를 방에 고정 → 시설을 '클릭'하면 각 UI(제작/휴식 등)가 열림. '나가기' 버튼/ESC로 복귀.
/// 안전가옥(Safehouse)의 은신처 입구(BuildingEntrance 트리거→"Hideout")에서 씬 전환으로 진입, 나가기 → "Safehouse"/default(집 문 앞).
///   진입 = 트리거 자동(밟으면 전환), 퇴장 = HideoutController의 UI '나가기'/ESC(캐릭터 없는 클릭 화면이라 걸어나가는 출구 없음).
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
        "gb_floor", "gb_wall", "gb_spawn",
        "gb_bed", "gb_workbench", "gb_medbench", "gb_cookbench",
    };

    [MenuItem("Tools/TopDown/빌드/은신처", priority = -99)]
    public static void Build()
    {
        var scene = EditorSceneBuildUtil.NewDetachedScene(out var prevActive);  // 현재 씬 유지(폴더에만 생성)
        var map = new GameObject("Map");
        int n = 0;

        EnsureGreyboxPalette();

        // 바닥 + 외곽(컨테이너 = 좁은 금속 박스)
        n += Floor(map, "Floor", 7f, 4.5f, MapW, MapH);
        n += Wall(map, "Wall_S", 7f,  0.5f, 14f, 1f);
        n += Wall(map, "Wall_N", 7f,  8.5f, 14f, 1f);
        n += Wall(map, "Wall_W", 0.5f,4.5f,  1f, 9f);
        n += Wall(map, "Wall_E",13.5f,4.5f,  1f, 9f);

        // 진입 스폰(안전가옥 집 입구 BuildingEntrance→여기/default).
        //   ※ 퇴장은 걸어나가는 출구가 아니라 HideoutController의 UI '나가기'/ESC(캐릭터 없는 클릭 화면이라 트리거 퇴장 불가) →
        //     Safehouse/default(집 문 앞)로 복귀. 그래서 옛 Exit_ToSafehouse(걸어나가는 gb_exit)는 두지 않는다.
        n += Spawn(map, "default", 3f, 4.5f);

        // 시설: 박스 침상 + 부서진 작업대 + 슬롯(조리대/의료대) + 창고 — 클릭하면 각 UI(타르코프식)
        n += Marker(map, "gb_bed",       "Bed_BoxCot",      5f, 6.5f);
        n += Marker(map, "gb_workbench", "Workbench_Broken",9f, 6.5f);
        n += Marker(map, "gb_cookbench", "CookingBench",   11f, 2.5f);
        n += Marker(map, "gb_medbench",  "MedicalBench",    5f, 2.5f);
        n += StashFacility(map, "Stash_창고", 11f, 6.5f);  // 창고 시설(메인 보관함) — 클릭 시 인벤 우측에 창고
        // 라디오(정보 수신) + 파견 보드(NPC 출전) — 클릭 시 각 UI(RadioUI/DispatchUI)
        n += FacilityOverride(map, "Radio_라디오",    7f, 6.5f, "라디오",  new Color(0.30f, 0.45f, 0.72f), InteractableObject.InteractType.Radio);
        n += FacilityOverride(map, "Dispatch_파견",   8f, 2.5f, "파견",    new Color(0.66f, 0.42f, 0.24f), InteractableObject.InteractType.Dispatch);
        // 발전기(전력 ON/OFF) — 라디오/파견의 전력 전제. 클릭 시 HideoutUI("generator")
        n += FacilityOverride(map, "Generator_발전기", 9f, 4.5f, "발전기", new Color(0.62f, 0.55f, 0.30f), InteractableObject.InteractType.Generator);

        // 타르코프식 화면 컨트롤러: 캐릭터 숨김 + 카메라 고정(방 중심 7,4.5 / size 5) + 클릭 상호작용 + 나가기 버튼/ESC.
        var hc = new GameObject("HideoutController").AddComponent<HideoutController>();
        hc.transform.SetParent(map.transform);
        n += 1;

        Selection.activeObject = null;
        if (!EditorSceneBuildUtil.SaveAndClose(scene, ScenePath, prevActive))  // 저장 후 닫기(현재 씬 유지)
        {
            Debug.LogError("[HideoutGB] 씬 저장 실패: " + ScenePath);
            return;
        }
        AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=cyan>[HideoutGB]</color> 생성 완료: {ScenePath} — 그레이박스 {n}개(침상/작업대/슬롯/스폰). 빌드세팅 등록.\n" +
                  "  • 안전가옥 은신처 입구(BuildingEntrance 트리거→Hideout)로 진입, 퇴장은 UI 나가기/ESC(→Safehouse/default). 걸어나가는 출구 없음.");
        if (!Application.isBatchMode && !ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("Hideout Greybox",
                $"{ScenePath} 생성 + 빌드세팅 등록 완료.\n\n박스 침상 + 부서진 작업대 + 조리대/의료대 슬롯 + 진입 스폰(default).\n" +
                "안전가옥 은신처 입구(트리거)가 여기로 전환. 퇴장은 UI '나가기'/ESC(HideoutController).", "확인");
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

    /// <summary>창고 시설 — gb_workbench 구조를 재사용해 색/라벨/타입을 '창고(Stash)'로 덮어쓴다.</summary>
    static int StashFacility(GameObject parent, string name, float x, float y)
        => FacilityOverride(parent, name, x, y, "창고", new Color(0.30f, 0.62f, 0.55f), InteractableObject.InteractType.Stash);

    /// <summary>기존 시설 프리팹(gb_workbench) 구조를 재사용하되 색/라벨/InteractType을 덮어써 새 시설을 추가한다.
    /// (팔레트 재생성 없이 시설 추가 — 창고/라디오/파견 등. 클릭 시 해당 InteractType의 UI가 열림.)</summary>
    static int FacilityOverride(GameObject parent, string name, float x, float y,
        string label, Color color, InteractableObject.InteractType interactType)
    {
        var go = Inst("gb_workbench", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);

        // 색 구분
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = color;

        // 라벨 텍스트 (자식 TextMesh "Label")
        var labelTr = go.transform.Find("Label");
        if (labelTr != null)
        {
            var tm = labelTr.GetComponent<TextMesh>();
            if (tm != null) tm.text = label;
        }

        // InteractableObject → 타입 + 프롬프트
        var io = go.GetComponentInChildren<InteractableObject>();
        if (io != null)
        {
            var so = new SerializedObject(io);
            var t  = so.FindProperty("type");        if (t  != null) t.enumValueIndex = (int)interactType;
            var pt = so.FindProperty("promptText");  if (pt != null) pt.stringValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
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
