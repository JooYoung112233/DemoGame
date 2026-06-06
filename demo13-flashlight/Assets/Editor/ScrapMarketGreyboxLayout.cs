#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// "폐상가 (낮) — 첫 출격" 레벨 레이아웃 v6를 그레이박스 프리팹(gb_*)으로 테스트 씬에 자동 배치한다.
/// (docs/level-scrapmarket.md §1 — MQ-001 첫 낮 레이드 / 스토리 동선 그대로 / 일방통행 선형)
///
/// v6 변경 요지(파밍을 '들어가는 건물' 안으로 + 창고도 건물 — docs/level-scrapmarket.md 2026-06-06 결정):
///   • 파밍을 **출입문(gb_door)으로 들어가는 폐쇄 '건물' 안**에 배치(길가 X). 건물 3채:
///       폐상점(선반×2+박스) · 차고(선반+박스×2) · 창고(민이 흔적·쪽지 — 민이 부재). 각 건물 = 벽으로 둘러싸고
///       **출입문 1칸으로만** 길과 연결되는 막다른 공간 → "건물 안에 들어가 루팅" 체감.
///   • **창고도 폐쇄 건물**(셔터 1입구, 막다름). 셔터로 진입 → 민이 흔적·쪽지(민이는 부재, NPC 아님) → 다시 나와
///       **길 따라 조금 더 위로 가면 맨홀**(창고 옆 골목 공터, '창고 막다른 건물 + 길가 맨홀').
///   • 길(street) = 서측 세로 3m 골목. 건물 3채는 길 동측에 매달림. 밴딧·차(야외1)는 골목 공터에.
///   • 동선: 철문 진입 → [폐상점] → [차고] → 골목 공터(밴딧·차) → [창고: 흔적·쪽지] → 맨홀(골목 끝).
///       적 조우는 두 건물(폐상점·차고) 파밍 '뒤', 창고 직전으로 늦춤(스토리 ①②③→④→⑤).
///   • 맵 44×56 유지. v5(통과형 방=길가 루팅)에서 건물화로 개정.
///
/// 생성물: Assets/Scenes/ScrapMarket_GB.unity — '맵 콘텐츠'만 담는 씬.
///   카메라/조명/EventSystem/매니저/플레이어는 넣지 않는다(= Systems 부트 씬이 additive로 공급).
///   모든 그레이박스는 단일 'Map' 루트 GameObject 하위에 들어간다.
///
/// 배치 규칙(v1~v5와 동일 기법):
///   • 좌표계 = XY 평면, Z=0. 1 unit = 1m. 맵 44(W) × 56(H).
///   • 벽/바리케이드/건물블록 = gb_wall/gb_barricade 인스턴스를 '바(bar)'/'블록'으로 스케일.
///       transform.localScale = (lengthX, thicknessY, 1), 위치 = 중심. (BoxCollider2D가 스케일을 따라 충돌=비주얼 일치)
///   • 바닥 = gb_floor 1개를 44×56 전체로 스케일(중심 (22,28)). 정렬 최하단.
///   • 마커(spawn/exit/enemy/npc)·상자/선반/문 = 스케일 1로 좌표 배치.
///   • 멱등: 씬이 있으면 덮어쓴다. 인스턴스는 PrefabUtility.InstantiatePrefab으로 만들어 링크 유지.
///   • 건물 출입문은 **gb_door**로 명시(폐상점=Shop_Door, 차고=Garage_Door, 창고=Warehouse_Shutter).
///       창고/맨홀/차 등 나머지는 전용 스프라이트 없이 **인스턴스 이름**으로 구분(맨홀=gb_exit, 민이 흔적·노크규칙 쪽지=gb_note, 차=gb_crate).
///       ※ 민이 NPC 없음 — 스토리상 민이는 부재, 창고엔 '흔적/쪽지'(손자국·노크규칙)만(= gb_note 오브젝트, InteractType.Note).
///
/// 조밀화 원리(중요): 길/건물 내부/출입구를 제외한 전 내부를 큰 gb_wall 블록으로 채운다.
///   불변식 — ① 블록은 어떤 채널(길·건물내부·출입구·공터)과도 겹치지 않는다.
///            ② 블록 합집합이 채널 외 내부를 빈틈없이 덮는다(walkable 구멍 금지). ③ 블록끼리 겹침 무방.
///   v6 채널 9개(+출입구 3): 길 ST(X4~7 Y2~53)·밴딧공터 PLAZA(X4~10 Y36~43, 차고 뒤)·맨홀공터 CT(X4~14 Y51~55)
///     · 폐상점 X8~16 Y5~14 · 차고 X8~16 Y26~35 · 창고 X8~16 Y44~50
///     · 출입구(X7~8): 폐상점 Y9~10 · 차고 Y30~31 · 창고 Y47~48.
///   → 건물은 서벽(X7~8)의 1칸 출입구로만 길과 연결되는 막다른 공간.
///
/// 메뉴: Tools ▸ TopDown ▸ Map ▸ Build ScrapMarket Greybox Layout
/// 배치모드(executeMethod): ScrapMarketGreyboxLayout.Build
/// </summary>
public static class ScrapMarketGreyboxLayout
{
    const string SceneDir   = "Assets/Scenes";
    const string ScenePath  = SceneDir + "/ScrapMarket_GB.unity";
    const string PrefabRoot = "Props2D/Prefabs/";   // Resources.Load 기준 경로

    // 맵 규모(문서 §1.2). 바닥/주석용. v6: 44 × 56 유지.
    const float MapW = 44f;
    const float MapH = 56f;

    // 이 레이아웃이 쓰는 gb_* 프리팹(하나라도 없으면 빌드 시 팔레트 자동 생성).
    static readonly string[] RequiredPrefabIds =
    {
        "gb_floor", "gb_wall", "gb_barricade", "gb_door", "gb_crate",
        "gb_shelf", "gb_note", "gb_spawn", "gb_exit", "gb_enemy",
    };

    [MenuItem("Tools/TopDown/Map/Build ScrapMarket Greybox Layout")]
    public static void Build()
    {
        // ── 빈 씬 새로 시작(멱등: 같은 경로로 저장하면 기존 씬 덮어씀) ──
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var map = new GameObject("Map");
        int placed = 0;

        // ── 그레이박스 팔레트(gb_*) 보장: 없으면 자동 생성(별도 'Generate Greybox Palette' 메뉴 불필요) ──
        EnsureGreyboxPalette();

        // ── 레이드 매니저(타이머 / 사망 / 시간초과 + 루트 추적) — 귀환 정산용 ──
        new GameObject("RaidManager").AddComponent<RaidManager>();

        // ── 바닥 ──
        placed += Floor(map, "Floor", 22f, 28f, MapW, MapH);

        // ── 외곽 벽(v3 그대로). 북벽 갭 X8~18 = 약국 통로(바리케이드 뒤) ──
        placed += Wall(map, "Wall_S",   22f,  0.5f, 44f, 1f);
        placed += Wall(map, "Wall_W",    0.5f, 28f,  1f, 56f);
        placed += Wall(map, "Wall_E",   43.5f, 28f,  1f, 56f);
        placed += Wall(map, "Wall_N_W",  4f,  55.5f, 8f, 1f);   // X0~8
        placed += Wall(map, "Wall_N_E", 31f,  55.5f,26f, 1f);   // X18~44

        // ─────────────────────────────────────────────────────────────────
        //  건물 블록(조밀화) — 길/건물내부/출입구 채널을 제외한 전 내부를 채운다(15블록, 1m 그리드 검증).
        //  서측 띠(X1~4) + 동측 대블록(X16~43) + 길↔건물 벽띠(X7~8, 출입구 3칸만 빔) + 건물 사이 막이(X8~16).
        // ─────────────────────────────────────────────────────────────────
        placed += Wall(map, "Bldg_S_base", 22f,  1.5f, 42f,  1f);  // 남단 토대 X1~43 Y1~2
        placed += Wall(map, "Bldg_W",       2.5f,28.5f, 3f, 53f);  // 서측 띠(전 높이) X1~4 Y2~55
        placed += Wall(map, "Bldg_E",      29.5f,28.5f,27f, 53f);  // 동측 대블록(건물 너머) X16~43 Y2~55
        // 길↔건물 벽띠 X7~8 (출입구 3칸 Y9~10·30~31·47~48 만큼 끊김)
        placed += Wall(map, "Bldg_door_a",  7.5f, 7f,   1f,  4f);  // X7~8 Y5~9
        placed += Wall(map, "Bldg_door_b",  7.5f,12f,   1f,  4f);  // X7~8 Y10~14
        placed += Wall(map, "Bldg_door_c",  7.5f,28f,   1f,  4f);  // X7~8 Y26~30
        placed += Wall(map, "Bldg_door_d",  7.5f,33f,   1f,  4f);  // X7~8 Y31~35
        placed += Wall(map, "Bldg_door_e",  7.5f,45.5f, 1f,  3f);  // X7~8 Y44~47
        placed += Wall(map, "Bldg_door_f",  7.5f,49f,   1f,  2f);  // X7~8 Y48~50
        // 건물 사이 막이 / 밴딧 공터 옆 (X7~16). 밴딧 공터는 Y36~43으로 이동(차고 '뒤', 창고 직전).
        placed += Wall(map, "Bldg_z1",     11.5f, 3.5f, 9f,  3f);  // X7~16 Y2~5 (폐상점 남)
        placed += Wall(map, "Bldg_z2",     11.5f,20f,   9f, 12f);  // X7~16 Y14~26 (폐상점 북~차고 남)
        placed += Wall(map, "Bldg_z3",     11.5f,35.5f, 9f,  1f);  // X7~16 Y35~36 (차고 북~공터 남)
        placed += Wall(map, "Bldg_z4",     13f,  39.5f, 6f,  7f);  // X10~16 Y36~43 (밴딧 공터 동측)
        placed += Wall(map, "Bldg_z5",     11.5f,43.5f, 9f,  1f);  // X7~16 Y43~44 (공터 북~창고 남)
        placed += Wall(map, "Bldg_z6",     11.5f,50.5f, 9f,  1f);  // X7~16 Y50~51 (창고 북~맨홀공터 남)
        placed += Wall(map, "Bldg_z7",     15f,  53f,   2f,  4f);  // X14~16 Y51~55 (맨홀 공터 동측)

        // ─────────────────────────────────────────────────────────────────
        //  1. 녹슨 철문 / 진입 (Spawn) — 남단. 길 ST(X4~7) 하단.
        // ─────────────────────────────────────────────────────────────────
        placed += Marker(map, "gb_spawn", "Gate_Spawn", 6f, 3f);

        // ─────────────────────────────────────────────────────────────────
        //  2. 폐상점 (건물, 실내 파밍 #1) — 방 X8~16 Y5~14. 서벽 출입문(Shop_Door, Y9~10)으로 진입.
        //     안에 선반×2 + 박스×1.
        // ─────────────────────────────────────────────────────────────────
        placed += Marker(map, "gb_door",  "Shop_Door",    7.5f, 9.5f);  // 출입문(길→폐상점)
        placed += Marker(map, "gb_shelf", "Shop_Shelf_1", 15f, 12f);
        placed += Marker(map, "gb_shelf", "Shop_Shelf_2", 15f, 7f);
        placed += Marker(map, "gb_crate", "Shop_Crate_1", 11f, 11f);

        // ─────────────────────────────────────────────────────────────────
        //  3. 차고 (건물, 실내 파밍 #2) — 방 X8~16 Y26~35. 서벽 출입문(Garage_Door, Y30~31)으로 진입.
        //     안에 선반×1 + 박스×2. (전투 '전' 둘째 파밍)
        // ─────────────────────────────────────────────────────────────────
        placed += Marker(map, "gb_door",  "Garage_Door",    7.5f, 30.5f);  // 출입문(길→차고)
        placed += Marker(map, "gb_shelf", "Garage_Shelf_1", 15f, 33f);
        placed += Marker(map, "gb_crate", "Garage_Crate_1", 11f, 28f);
        placed += Marker(map, "gb_crate", "Garage_Crate_2", 14f, 28f);

        // ─────────────────────────────────────────────────────────────────
        //  4. 골목 공터 (밴딧 + 야외 차) — PLAZA X4~10 Y36~43. 두 건물 파밍 '후' 첫 전투(창고 직전).
        //     적 조우를 늦춰 파밍→파밍→전투→창고 순(스토리 S-005 ①②③→④→⑤).
        // ─────────────────────────────────────────────────────────────────
        placed += Marker(map, "gb_enemy", "Bandit_Corner", 6f, 40f);   // 탐색꾼/밴딧 ×1 (폐상점·차고 지난 뒤)
        placed += Marker(map, "gb_crate", "Road_Car_1",    8f, 38f);   // 야외 차 트렁크 ×1(스토리 ③ 다양성)

        // ─────────────────────────────────────────────────────────────────
        //  5. 창고 (건물, 메인 목표 — 막다름) — 방 X8~16 Y44~50. 셔터(Warehouse_Shutter, Y47~48) 1입구.
        //     민이는 '없음' = 흔적만(스토리). 손자국·노크규칙 = 쪽지/단서 오브젝트(gb_note, NPC 아님).
        //     보고 나와 길로 복귀 → 길 따라 조금 더 위 맨홀.
        // ─────────────────────────────────────────────────────────────────
        placed += Marker(map, "gb_door",  "Warehouse_Shutter",    7.5f, 47.5f);  // 셔터(길→창고, 막다름)
        placed += Note(map, "Warehouse_Mini_Note",  11f, 46f, "창고 안 흔적",
            "셔터 아래쪽에 작은 손자국이 말라붙어 있다.\n안에서 누가 긁은 것 같은 자국도 보인다.\n\n……민이, 여기 있었구나. 지금은 어디 있지?");   // 민이 흔적 단서
        placed += Note(map, "Warehouse_Knock_Note", 14f, 48f, "벽에 적힌 낙서",
            "세 번 두드리면 대답해.\n한 번이면 숨고.\n두 번이면 울고.\n네 번이면 절대 열지 마.");   // 노크규칙 쪽지

        // ─────────────────────────────────────────────────────────────────
        //  6. 맨홀 탈출 (EXIT) — 창고 옆 골목 공터(CT X4~14 Y51~55), 길 따라 조금 더 위. 추출구.
        // ─────────────────────────────────────────────────────────────────
        placed += Exit(map, "Manhole_Exit", 9f, 53f, "Safehouse", "raid_return", 5f);

        // ─────────────────────────────────────────────────────────────────
        //  통합 연결점 (철거 가능 바리케이드). §2.
        // ─────────────────────────────────────────────────────────────────
        placed += Barricade(map, "Pharmacy_Barricade_N", 11f, 54.5f, 6f, 1f);  // 맨홀 공터 북단(X8~14)→약국
        placed += Barricade(map, "Basement_Block",       15f, 47f,   1f, 4f);  // 창고 동측 안쪽→밤 지하창고(루디)

        // ── 저장 ──
        Selection.activeObject = null;
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);

        if (!saved)
        {
            Debug.LogError("[ScrapMarketGB] 씬 저장 실패: " + ScenePath);
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("ScrapMarket Greybox", "씬 저장 실패:\n" + ScenePath, "확인");
            return;
        }

        AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=cyan>[ScrapMarketGB]</color> 생성 완료(v6): {ScenePath} — Map 하위 그레이박스 {placed}개 배치.\n" +
                  "  • v6 = 파밍을 '들어가는 건물' 안으로(폐상점·차고·창고 3채, 출입문 gb_door) + 창고도 막다른 건물.\n" +
                  "  • 동선: 철문 진입 → [폐상점] → [차고] → 골목 공터(밴딧·차) → [창고: 흔적·쪽지] → 맨홀(골목 끝). 적 조우는 두 건물 파밍 뒤(창고 직전).\n" +
                  "  • 각 건물은 서벽 출입문 1칸으로만 연결되는 막다른 폐쇄 공간(안에서 루팅). 맵 44×56.\n" +
                  "  • 바리케이드 = 통합 연결점: 맨홀 공터 북단→약국(SQ-002), 창고 동측→밤 지하창고(루디).\n" +
                  "  • Systems 씬을 additive로 올린 뒤 Play하거나, MapTool 카탈로그 '맵 저장'으로 프리팹화 가능.");

        if (!Application.isBatchMode && !ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("ScrapMarket Greybox",
                $"{ScenePath} 생성 완료(v6).\n\nMap 루트 하위에 그레이박스 {placed}개 배치.\n" +
                "파밍을 '들어가는 건물' 안으로 + 창고도 막다른 건물(셔터 1입구).\n" +
                "철문 → [폐상점] → [차고] → 골목 공터(밴딧·차) → [창고: 흔적·쪽지] → 맨홀.\n\n" +
                "맵 콘텐츠만 담긴 씬입니다(팔레트 gb_*는 없으면 자동 생성). Systems 부트 씬이 카메라/조명/매니저/플레이어를 공급합니다.",
                "확인");
    }

    /// <summary>이 레이아웃이 쓰는 gb_* 프리팹이 하나라도 없으면 팔레트 전체를 자동 생성(별도 메뉴 불필요).</summary>
    static void EnsureGreyboxPalette()
    {
        foreach (var id in RequiredPrefabIds)
        {
            if (Resources.Load<GameObject>(PrefabRoot + id) == null)
            {
                Debug.Log($"<color=cyan>[ScrapMarketGB]</color> 그레이박스 프리팹 '{id}' 없음 → 팔레트 자동 생성(GreyboxPaletteBuilder.Generate).");
                GreyboxPaletteBuilder.Generate();
                return;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // 배치 헬퍼 (모두 PrefabUtility.InstantiatePrefab → gb_* 프리팹 링크 유지)
    // ─────────────────────────────────────────────────────────────────────

    static int Wall(GameObject parent, string name, float cx, float cy, float lenX, float thickY)
        => Bar(parent, "gb_wall", name, cx, cy, lenX, thickY);

    static int Barricade(GameObject parent, string name, float cx, float cy, float lenX, float thickY)
        => Bar(parent, "gb_barricade", name, cx, cy, lenX, thickY);

    static int Bar(GameObject parent, string prefabId, string name, float cx, float cy, float lenX, float thickY)
    {
        var go = Spawn(prefabId, name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(cx, cy, 0f);
        go.transform.localScale    = new Vector3(lenX, thickY, 1f);
        CounterScaleLabel(go);
        return 1;
    }

    static int Floor(GameObject parent, string name, float cx, float cy, float w, float h)
    {
        var go = Spawn("gb_floor", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(cx, cy, 0f);
        go.transform.localScale    = new Vector3(w, h, 1f);
        CounterScaleLabel(go);
        return 1;
    }

    static int Marker(GameObject parent, string prefabId, string name, float x, float y)
    {
        var go = Spawn(prefabId, name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);
        return 1;
    }

    /// <summary>쪽지(gb_note): 좌표 배치 + InteractableObject에 내용/제목 주입(읽으면 NoteUI 전체화면).</summary>
    static int Note(GameObject parent, string name, float x, float y, string title, string content)
    {
        var go = Spawn("gb_note", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);
        var io = go.GetComponentInChildren<InteractableObject>();
        if (io != null) io.SetNote(content, title, "읽기");
        return 1;
    }

    /// <summary>탈출구(ExitPoint): 좌표 배치 + targetScene/spawnPointId/대기 설정(추출 = 상호작용 후 wait초).</summary>
    static int Exit(GameObject parent, string name, float x, float y, string targetScene, string spawnId, float wait)
    {
        var go = Spawn("gb_exit", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);
        var io = go.GetComponentInChildren<InteractableObject>();
        if (io != null)
        {
            var so = new SerializedObject(io);
            var ts = so.FindProperty("targetScene");  if (ts != null) ts.stringValue = targetScene;
            var sp = so.FindProperty("spawnPointId");  if (sp != null) sp.stringValue = spawnId;
            var ew = so.FindProperty("exitWaitTime");  if (ew != null) ew.floatValue = wait;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return 1;
    }

    /// <summary>씬을 빌드세팅에 등록(이미 있으면 무시).</summary>
    static void AddToBuildSettings(string scenePath)
    {
        var cur = EditorBuildSettings.scenes;
        foreach (var s in cur) if (s.path == scenePath) return;
        var arr = new EditorBuildSettingsScene[cur.Length + 1];
        System.Array.Copy(cur, arr, cur.Length);
        arr[cur.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = arr;
    }

    static GameObject Spawn(string prefabId, string name, GameObject parent)
    {
        var prefab = Resources.Load<GameObject>(PrefabRoot + prefabId);
        if (prefab == null)
        {
            Debug.LogError($"[ScrapMarketGB] 프리팹 로드 실패: Resources/{PrefabRoot}{prefabId} " +
                           "(먼저 'Tools/TopDown/Map/Generate Greybox Palette' 실행 필요)");
            return null;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        go.name = name;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;
        return go;
    }

    /// <summary>막대/블록 스케일 시 자식 'Label' 글자 비율 보정(콜라이더 무관, 가독성만).</summary>
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
