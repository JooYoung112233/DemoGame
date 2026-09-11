#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Pose = HideoutFacilityAnchor.Pose;
using Dock = HideoutFacilityAnchor.Dock;

/// <summary>
/// 은신처(컨테이너) — **3D 쿼터뷰 버전**. 2D `HideoutGreyboxLayout`의 3D 대응.
///
/// 좌표 규약(docs/3d-migration.md): 월드는 **XZ 평면**, 위쪽 +Y, 1u = 1m.
/// 2D 시절 좌표 (x, y)를 그대로 (x, 0, y)로 옮겼다 — 시설 배치가 2D판과 1:1로 대응한다.
///
/// 방 = 14 × 9 m 컨테이너 내부.
/// ★ **천장을 만들지 않는다** — pitch 55° 쿼터뷰에서 지붕이 있으면 내부가 아예 안 보인다.
///   컨테이너 "안"이라는 느낌은 4면 벽 + 어두운 조명으로 낸다.
///
/// 메뉴: Tools ▸ TopDown ▸ 빌드 ▸ 은신처 (3D)
/// </summary>
public static class Hideout3DLayout
{
    const string ScenePath = "Assets/Scenes/Hideout.unity";   // 3D 은신처가 곧 정식 Hideout이다(2026-09-09)

    // 방 규격 — 2D판(MapW 14, MapH 9)과 동일
    const float RoomW = 14f, RoomD = 9f;
    const float WallH = 2.6f;    // 컨테이너 내부 높이(실측 약 2.4~2.7m)
    const float WallT = 0.3f;

    [MenuItem("Tools/TopDown/빌드/은신처 (3D)", priority = -98)]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var map = new GameObject("Map");
        int n = 0;

        // ── 재질 팔레트 (텍스처 없는 단색 — 3D 아트 규약) ──
        var matFloor = Mat(new Color(0.28f, 0.28f, 0.30f), 0.05f);
        var matWall  = Mat(new Color(0.38f, 0.40f, 0.38f), 0.08f);   // 골판 금속 컨테이너
        var matRib   = Mat(new Color(0.33f, 0.35f, 0.33f), 0.10f);

        // ── 배경판 ── 방이 14×9m뿐이라 줌인하면 카메라가 방 밖(검은 공백)을 잡는다.
        // 컨테이너 바깥은 어차피 보일 일이 없으니 넓고 어두운 판을 깔아 공백을 없앤다.
        var matVoid = Mat(new Color(0.055f, 0.055f, 0.065f), 0f);
        n += Deco(map, "Backdrop", new Vector3(RoomW * .5f, -0.35f, RoomD * .5f),
                  new Vector3(70f, 0.2f, 70f), matVoid);

        // ── 바닥 ──
        n += Box(map, "Floor", new Vector3(RoomW * .5f, -0.05f, RoomD * .5f),
                 new Vector3(RoomW, 0.1f, RoomD), matFloor);

        // ── 외벽 4면 (천장 없음) ──
        float cx = RoomW * .5f, cz = RoomD * .5f, hy = WallH * .5f;
        n += Box(map, "Wall_S", new Vector3(cx, hy, -WallT * .5f),          new Vector3(RoomW + WallT * 2f, WallH, WallT), matWall);
        n += Box(map, "Wall_N", new Vector3(cx, hy, RoomD + WallT * .5f),   new Vector3(RoomW + WallT * 2f, WallH, WallT), matWall);
        n += Box(map, "Wall_W", new Vector3(-WallT * .5f, hy, cz),          new Vector3(WallT, WallH, RoomD), matWall);
        n += Box(map, "Wall_E", new Vector3(RoomW + WallT * .5f, hy, cz),   new Vector3(WallT, WallH, RoomD), matWall);

        // 골판 리브 — 컨테이너 느낌. 남·북 벽에 세로 홈을 얕게 덧붙인다(콜라이더 없음).
        for (float x = 0.6f; x < RoomW; x += 1.2f)
        {
            n += Deco(map, "Rib_S", new Vector3(x, hy, 0.02f),          new Vector3(0.18f, WallH * 0.92f, 0.10f), matRib);
            n += Deco(map, "Rib_N", new Vector3(x, hy, RoomD - 0.02f),  new Vector3(0.18f, WallH * 0.92f, 0.10f), matRib);
        }

        // ── 시설 ── 2D판과 같은 (x, y) → (x, 0, y)
        // 시설 — 클릭하면 캐릭터가 그 앞으로 가서 자세를 잡는다(디오라마).
        // dock = UI가 붙을 자리. 카메라가 반대쪽으로 밀어 캐릭터를 안 가린다.
        //   전부 Right(우측 패널). 침대도 하단 바를 쓰다가 통일했다 — 수면창(720×460)이 커서
        //   하단 바를 높이면 방이 다른 시설보다 작게 잡혀 화면 크기가 시설마다 들쭉날쭉했다.
        // 북쪽 벽 줄 (z=7.7) — 침대 · 라디오 · 작업대 · 창고
        n += Facility(map, "Bed_BoxCot",       new Vector3(2.6f,  0f, 7.7f), new Vector3(2.2f, 0.55f, 1.1f), new Color(0.45f, 0.38f, 0.32f), "bed",       Pose.Lie,   Dock.Right);
        n += Facility(map, "Radio_라디오",      new Vector3(5.6f,  0f, 7.9f), new Vector3(0.8f, 0.6f,  0.6f), new Color(0.30f, 0.45f, 0.72f), "radio",     Pose.Stand, Dock.Right);
        n += Facility(map, "Workbench_Broken", new Vector3(8.4f,  0f, 7.8f), new Vector3(1.9f, 0.95f, 0.8f), new Color(0.50f, 0.45f, 0.38f), "workbench", Pose.Stand, Dock.Right);
        n += Facility(map, "Stash_창고",        new Vector3(11.6f, 0f, 7.8f), new Vector3(1.3f, 1.4f,  0.8f), new Color(0.30f, 0.62f, 0.55f), "stash",     Pose.Stand, Dock.Right);

        // 남쪽 벽 줄 (z=1.3) — 의료대 · 조리대 · 파견 보드
        n += Facility(map, "MedicalBench",     new Vector3(3.0f,  0f, 1.3f), new Vector3(1.7f, 0.9f,  0.8f), new Color(0.70f, 0.70f, 0.72f), "medical",   Pose.Stand, Dock.Right);
        n += Facility(map, "CookingBench",     new Vector3(6.6f,  0f, 1.3f), new Vector3(1.7f, 0.9f,  0.8f), new Color(0.60f, 0.45f, 0.30f), "cooking",   Pose.Stand, Dock.Right);
        n += Facility(map, "Dispatch_파견",     new Vector3(10.2f, 0f, 1.15f),new Vector3(1.2f, 1.6f,  0.3f), new Color(0.66f, 0.42f, 0.24f), "dispatch",  Pose.Stand, Dock.Right);

        // 동쪽 벽 — 발전기
        n += Facility(map, "Generator_발전기",  new Vector3(12.9f, 0f, 4.5f), new Vector3(1.0f, 1.0f,  1.0f), new Color(0.62f, 0.55f, 0.30f), "generator", Pose.Stand, Dock.Right);

        // 대기 의자 — 방 가운데. 여기만 캐릭터가 소품 "위"에 있으므로 서기 오프셋 0.
        n += Facility(map, "Chair_Idle", new Vector3(6.6f, 0f, 4.5f), new Vector3(0.6f, 0.85f, 0.6f), new Color(0.42f, 0.34f, 0.28f), "idle", Pose.Sit, Dock.None);

        // ── 나가기 ── 2D판과 같은 HideoutController가 화면 UI('나가기' 버튼)·ESC 확인창·
        //    씬 복귀를 담당한다. 디오라마가 있으면 캐릭터 숨김·카메라 고정·2D 클릭은 스스로 양보한다.
        //    ★ 걸어나가는 출구는 두지 않는다 — 클릭 화면이라 트리거 퇴장이 성립하지 않는다.
        var hcGO = new GameObject("HideoutController");
        hcGO.transform.SetParent(map.transform, false);
        var hc = hcGO.AddComponent<HideoutController>();
        {
            var so = new SerializedObject(hc);
            var sc = so.FindProperty("safehouseScene");
            var sp = so.FindProperty("safehouseSpawn");
            if (sc != null) sc.stringValue = "Safehouse";   // 3D 마을이 정식 Safehouse다
            if (sp != null) sp.stringValue = "from_hideout";
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        n++;

        // ── 도킹 UI ── 캐릭터를 가리지 않는 우측/하단 패널
        var dockGO = new GameObject("HideoutDockPanel");
        dockGO.transform.SetParent(map.transform, false);
        dockGO.AddComponent<HideoutDockPanel>();
        n++;

        // ── 디오라마 오케스트레이터 ── 시설 클릭 → 캐릭터 이동·자세 → 카메라 → UI
        var dio = new GameObject("HideoutDiorama");
        dio.transform.SetParent(map.transform, false);
        dio.AddComponent<HideoutDiorama>();
        n++;

        // ── 진입 스폰 (2D판과 동일 위치) ──
        var spawn = new GameObject("default");
        spawn.transform.SetParent(map.transform, false);
        spawn.transform.position = new Vector3(3f, 0f, 4.5f);
        var spc = spawn.AddComponent<SpawnPoint>();
        // 이름과 pointId를 함께 맞춘다 — 여긴 하나뿐이라 기본값과 우연히 같지만,
        // 이름만 믿는 습관이 마을에서 스폰 전부를 "default"로 만들었다.
        {
            var spSo = new SerializedObject(spc);
            spSo.FindProperty("pointId").stringValue = "default";
            spSo.ApplyModifiedPropertiesWithoutUndo();
        }
        n++;

        // ── 조명 ── 컨테이너 내부: 약한 천창 + 매달린 전구 한 개
        // 천창(디렉셔널)과 앰비언트는 Lighting3D의 Hideout 프리셋이 갖고 있다.
        // 전구는 이 방만의 소품이라 여기 남긴다 — 다른 씬이 따라 할 값이 아니다.
        Lighting3D.Apply(map, Lighting3D.Preset.Hideout);

        var bulbGO = new GameObject("Bulb");
        bulbGO.transform.SetParent(map.transform, false);
        bulbGO.transform.position = new Vector3(7f, 2.3f, 4.5f);
        var bulb = bulbGO.AddComponent<Light>();
        bulb.type = LightType.Point;
        bulb.range = 11f;
        bulb.intensity = 3.2f;
        bulb.color = new Color(1f, 0.82f, 0.58f);      // 백열 전구
        bulb.shadows = LightShadows.Soft;
        n += 2;

        SceneHierarchyOrganizer.Organize(scene);   // 규약대로 기능별 폴더에 묶는다(docs/architecture.md §씬 하이어라키 규약)
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=cyan>[Hideout3D]</color> 생성 완료: {ScenePath} — 오브젝트 {n}개. " +
                  "천장 없음(쿼터뷰에서 내부가 보이도록). 시설 좌표는 2D판과 1:1.");
    }

    /// <summary>빌드 세팅에 씬 등록 — 없으면 SceneManager.LoadScene("...")이 실패한다.
    /// (2D 빌더는 하는데 3D 빌더가 빠뜨려 마을↔은신처 전환이 통째로 안 됐다.)</summary>
    static void AddToBuildSettings(string scenePath)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == scenePath) { s.enabled = true; EditorBuildSettings.scenes = list.ToArray(); return; }
        list.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────
    /// <summary>moduleKey → 기존 상호작용 타입. 2D판 FacilityOverride/Marker와 같은 배선.</summary>
    static InteractableObject.InteractType InteractTypeFor(string key) => key switch
    {
        "bed"       => InteractableObject.InteractType.Bed,
        "workbench" => InteractableObject.InteractType.Workbench,
        "stash"     => InteractableObject.InteractType.Stash,
        "radio"     => InteractableObject.InteractType.Radio,
        "cooking"   => InteractableObject.InteractType.CookingBench,
        "medical"   => InteractableObject.InteractType.MedicalBench,
        "dispatch"  => InteractableObject.InteractType.Dispatch,
        "generator" => InteractableObject.InteractType.Generator,
        _           => InteractableObject.InteractType.Generic,
    };

    static Material Mat(Color c, float smooth)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        return m;
    }

    static int Box(GameObject parent, string name, Vector3 center, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return 1;
    }

    /// <summary>순수 장식 — 콜라이더를 떼서 이동·판정에 관여하지 않게 한다.</summary>
    static int Deco(GameObject parent, string name, Vector3 center, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return 1;
    }

    /// <summary>시설 — 바닥에 놓이도록 y를 높이 절반만큼 띄우고, 캐릭터 자세 앵커를 붙인다.</summary>
    static int Facility(GameObject parent, string name, Vector3 floorPos, Vector3 size, Color c,
                        string moduleKey, Pose pose, Dock dock)
    {
        var center = new Vector3(floorPos.x, size.y * .5f, floorPos.z);
        Box(parent, name, center, size, Mat(c, 0.06f));
        var go = parent.transform.Find(name).gameObject;

        // 캐릭터가 설 자리 — 방 중앙에서 접근한다고 보고 그 방향으로 띄운다.
        // 대기 의자(idle)만 예외: 앉는 자리라 소품 위치 그대로.
        var toCenter = new Vector3(RoomW * .5f - floorPos.x, 0f, RoomD * .5f - floorPos.z);
        toCenter = toCenter.sqrMagnitude > 0.0001f ? toCenter.normalized : Vector3.forward;
        float standDist = moduleKey == "idle" ? 0f : 1.15f;
        var standGO = new GameObject("Stand");
        standGO.transform.SetParent(go.transform, true);
        standGO.transform.position = new Vector3(floorPos.x, 0f, floorPos.z) + toCenter * standDist;

        // 기존 UI 배선 — moduleKey에 맞는 상호작용 타입을 붙인다.
        var io = go.AddComponent<InteractableObject>();
        var so = new SerializedObject(io);
        var tp = so.FindProperty("interactType");
        if (tp != null) { tp.enumValueIndex = (int)InteractTypeFor(moduleKey); so.ApplyModifiedPropertiesWithoutUndo(); }

        var a = go.AddComponent<HideoutFacilityAnchor>();
        a.moduleKey = moduleKey;
        a.standPoint = standGO.transform;
        a.pose = pose;
        a.dock = dock;

        // 머리 위 역할 라벨 — 클릭 화면이라 E 프롬프트가 성립하지 않는다.
        // 무엇을 누를 수 있는지 한눈에 보여야 한다.
        if (moduleKey != "idle") go.AddComponent<FacilityLabel>();
        return 2;
    }
}
#endif
