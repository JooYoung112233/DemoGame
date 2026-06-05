#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// "폐상가 (낮) — 첫 출격" 레벨 레이아웃 v3를 그레이박스 프리팹(gb_*)으로 테스트 씬에 자동 배치한다.
/// (docs/level-scrapmarket.md §1 — MQ-001 첫 낮 레이드 / 스토리 동선 그대로 / 일방통행 선형 / 좁은 4m 골목 / 조밀 건물블록)
///
/// v3 변경 요지(스토리 반영 — story-script.md S-004~S-006):
///   • 동선을 **스토리 그대로** 1열 선형으로 재구성:
///       녹슨 철문 진입(남) → 폐상점(첫 파밍) → 버려진 도로(자동차·박스) → 밴딧 코너(첫 전투)
///       → 창고(셔터문 통과 · 민이 흔적 · 노크 규칙 · 쪽지 = MQ-001 핵심) → 맨홀 탈출(창고 앞).
///       복도는 좌우로 꺾여(zig-zag) 시야 코너를 만든다(밴딧 = 코너 조우).
///   • v2의 추상 구역명(편의점/셔터상가/잔해/뒷골목) 폐기 → 스토리 지명으로 교체:
///       편의점 → **폐상점**(일반 폐상점, 선반/서랍). 북단탈출 → **맨홀**(창고 앞 임시 탈출구).
///   • 창고 = 메인 목표. 셔터(gb_door) 강제 통과 → 안에 민이(gb_npc, 동생 복선 SQ-001) + 쪽지(gb_crate).
///   • 통합 연결점(철거 가능 바리케이드 = 연결점) 2곳:
///       북단 = 약국 방향(SQ-002 "폐상가 북쪽 무너진 약국") / 창고 인근 = 밤 전용 지하창고(루디) 입구(낮엔 막힘).
///   • 맵 = 44(W) × 56(H)로 압축. 좁고 단순한 첫 지역(스토리 "좁고 단순한 첫 지역").
///
/// 생성물: Assets/Scenes/ScrapMarket_GB.unity — '맵 콘텐츠'만 담는 씬.
///   카메라/조명/EventSystem/매니저/플레이어는 넣지 않는다(= Systems 부트 씬이 additive로 공급).
///   모든 그레이박스는 단일 'Map' 루트 GameObject 하위에 들어간다
///   (MapTool2D 규약과 동일 — 카탈로그 '맵 저장'으로 그대로 프리팹화 가능).
///
/// 배치 규칙(v1/v2와 동일 기법):
///   • 좌표계 = XY 평면, Z=0. 1 unit = 1m. 맵 44(W) × 56(H).
///   • 벽/바리케이드/건물블록 = gb_wall/gb_barricade 인스턴스를 '바(bar)'/'블록'으로 스케일.
///       transform.localScale = (lengthX, thicknessY, 1), 위치 = 중심. (BoxCollider2D가 스케일을 따라 충돌=비주얼 일치)
///   • 바닥 = gb_floor 1개를 44×56 전체로 스케일(중심 (22,28), scale (44,56,1)). 정렬 최하단.
///   • 마커(spawn/exit/enemy/npc)·상자/선반/문 = 스케일 1로 해당 좌표에 배치.
///   • 멱등: 씬이 있으면 덮어쓴다.
///   • 인스턴스는 PrefabUtility.InstantiatePrefab으로 만들어 gb_* 프리팹과 링크 유지.
///   • 그레이박스 프리팹 종류는 11개(floor/roof/wall/barricade/door/crate/shelf/spawn/exit/enemy/npc)뿐이라
///       창고/맨홀/민이 등은 전용 스프라이트 없이 **인스턴스 이름**으로 구분한다
///       (맨홀=gb_exit, 셔터=gb_door, 민이=gb_npc, 쪽지/박스=gb_crate).
///
/// 메뉴: Tools ▸ TopDown ▸ Map ▸ Build ScrapMarket Greybox Layout
/// 배치모드(executeMethod): ScrapMarketGreyboxLayout.Build
/// </summary>
public static class ScrapMarketGreyboxLayout
{
    const string SceneDir   = "Assets/Scenes";
    const string ScenePath  = SceneDir + "/ScrapMarket_GB.unity";
    const string PrefabRoot = "Props2D/Prefabs/";   // Resources.Load 기준 경로

    // 맵 규모(문서 §1.2). 바닥/주석용. v3: 44 × 56.
    const float MapW = 44f;
    const float MapH = 56f;

    [MenuItem("Tools/TopDown/Map/Build ScrapMarket Greybox Layout")]
    public static void Build()
    {
        // ── 빈 씬 새로 시작(멱등: 같은 경로로 저장하면 기존 씬 덮어씀) ──
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 모든 그레이박스의 단일 부모.
        var map = new GameObject("Map");

        int placed = 0;

        // ── 바닥: 전체 44×56 커버, 정렬 최하단(프리팹이 이미 Ground/-10) ──
        placed += Floor(map, "Floor", 22f, 28f, MapW, MapH);

        // ─────────────────────────────────────────────────────────────────
        //  외곽 벽(전체 둘레, 두께 1). 북벽은 약국 통로(X8~18)만큼 끊어 두 토막.
        //  → 그 갭은 Pharmacy_Barricade_N(철거 가능)으로 막아, 치우면 북쪽 약국/심부로 개방.
        // ─────────────────────────────────────────────────────────────────
        placed += Wall(map, "Wall_S",   22f,  0.5f, 44f, 1f);   // 남(철문 진입쪽)
        placed += Wall(map, "Wall_W",    0.5f, 28f,  1f, 56f);  // 서
        placed += Wall(map, "Wall_E",   43.5f, 28f,  1f, 56f);  // 동
        placed += Wall(map, "Wall_N_W",  4f,  55.5f, 8f, 1f);   // 북 서측 (X0~8)
        placed += Wall(map, "Wall_N_E", 31f,  55.5f,26f, 1f);   // 북 동측 (X18~44). 사이 X8~18 = 약국 통로(바리케이드)

        // ─────────────────────────────────────────────────────────────────
        //  건물 블록(조밀화) — 복도/방을 제외한 모든 내부 공간을 큰 gb_wall로 채운다.
        //  복도는 이 블록들 사이의 '음(陰)의 공간'으로 4m 폭으로 비워진다.
        //  (각 블록은 걷는 복도/방 사각형과 겹치지 않도록 클립함. 블록끼리 겹침은 무방.)
        //  채널 = C1(스폰) → 폐상점방 → C2(도로) → C3(동측↑) → C4(서향) → 진입공터(맨홀) → 창고방.
        // ─────────────────────────────────────────────────────────────────
        placed += Wall(map, "Bldg_S_base",  22f,  1.5f, 42f,  1f);  // 남단 토대 X1~43 Y1~2
        placed += Wall(map, "Bldg_W_low",    2.5f,14f,   3f, 24f);  // 서측 띠(스폰·폐상점 좌) X1~4 Y2~26
        placed += Wall(map, "Bldg_SE_low",  25.5f, 9f,  35f, 14f);  // 남단 동측 대블록 X8~43 Y2~16
        placed += Wall(map, "Bldg_E_mid1",  30.5f,19f,  25f,  6f);  // 폐상점 우측 X18~43 Y16~22
        placed += Wall(map, "Bldg_E_strip", 40.5f,32f,   5f, 20f);  // 동측 끝 띠(C3 우) X38~43 Y22~42
        placed += Wall(map, "Bldg_core",    17.5f,32f,  33f, 12f);  // 중앙 코어 대블록(도로 위~C4 아래) X1~34 Y26~38
        placed += Wall(map, "Bldg_W_f",      5.5f,40f,   9f,  4f);  // C4 서측 마감(하) X1~10 Y38~42
        placed += Wall(map, "Bldg_W_h",      5.5f,45f,   9f,  6f);  // 진입공터 서벽 X1~10 Y42~48
        placed += Wall(map, "Bldg_E_h",     29.5f,45f,  27f,  6f);  // 진입공터 동측 대블록 X16~43 Y42~48
        placed += Wall(map, "Bldg_W_ware",   4.5f,51f,   7f,  6f);  // 창고 서벽 X1~8 Y48~54
        placed += Wall(map, "Bldg_E_ware",  32.5f,51f,  21f,  6f);  // 창고 동측 대블록 X22~43 Y48~54
        placed += Wall(map, "Bldg_N_cap_W",  4.5f,54.5f, 7f,  1f);  // 북단 마감 서측 X1~8 Y54~55
        placed += Wall(map, "Bldg_N_cap_E", 30.5f,54.5f,25f,  1f);  // 북단 마감 동측 X18~43 Y54~55 (사이 = 약국 통로)

        // ─────────────────────────────────────────────────────────────────
        //  1. 녹슨 철문 / 진입 (Spawn) — 남단. 전당포(안전가옥)에서 도착. C1 하단.
        // ─────────────────────────────────────────────────────────────────
        placed += Marker(map, "gb_spawn", "Gate_Spawn", 6f, 3f);

        // ─────────────────────────────────────────────────────────────────
        //  2. 폐상점 (첫 파밍 방) — 복도가 방을 관통(C1 남 진입 → 북동 빠짐). 방 X4~18 Y16~26.
        //     일반 폐상점(편의점 아님): 선반 ×2 + 박스(서랍) ×2.
        // ─────────────────────────────────────────────────────────────────
        placed += Marker(map, "gb_shelf", "Shop_Shelf_1", 6f, 21f);
        placed += Marker(map, "gb_shelf", "Shop_Shelf_2", 6f, 18f);
        placed += Marker(map, "gb_crate", "Shop_Crate_1", 10f, 20f);
        placed += Marker(map, "gb_crate", "Shop_Crate_2", 14f, 18f);

        // ─────────────────────────────────────────────────────────────────
        //  3. 버려진 도로 (자동차·박스 잡동사니) — C2 가로 구간(X8~38 Y22~26). 길가 박스 ×2.
        // ─────────────────────────────────────────────────────────────────
        placed += Marker(map, "gb_crate", "Road_Box_1", 16f, 24f);
        placed += Marker(map, "gb_crate", "Road_Box_2", 26f, 24f);

        // ─────────────────────────────────────────────────────────────────
        //  4. 밴딧 조우 (첫 전투) — C2(가로)↔C3(세로) 동남 코너를 돈 직후. 시야 코너 조우.
        // ─────────────────────────────────────────────────────────────────
        placed += Marker(map, "gb_enemy", "Bandit_Corner", 36f, 30f);

        // ─────────────────────────────────────────────────────────────────
        //  5. 창고 (메인 목표) — 셔터(반쯤 내려간 문) 강제 통과 → 방 X8~22 Y48~54.
        //     안: 민이(gb_npc, 동생 복선 SQ-001) + 쪽지/단서 박스(gb_crate). 손자국·노크규칙이 사는 곳(스토리).
        //     셔터문 = 진입공터(APPR)→창고 입구(X10~16 경계, Y48). 강제 통과 도어웨이.
        // ─────────────────────────────────────────────────────────────────
        placed += Marker(map, "gb_door",  "Warehouse_Shutter",    13f, 48f);  // 반쯤 내려간 셔터(강제 통과)
        placed += Marker(map, "gb_npc",   "Warehouse_Mini_NPC",   12f, 51f);  // 민이(아이, 생존자 — 동생 복선)
        placed += Marker(map, "gb_crate", "Warehouse_Note_Crate", 18f, 52f);  // 쪽지/노크규칙 단서

        // ─────────────────────────────────────────────────────────────────
        //  6. 맨홀 탈출 (EXIT) — 창고 '바로 앞'(진입공터 APPR X10~16 Y40~48). 레이드 추출구.
        // ─────────────────────────────────────────────────────────────────
        placed += Marker(map, "gb_exit", "Manhole_Exit", 13f, 45f);

        // ─────────────────────────────────────────────────────────────────
        //  통합 연결점 (철거 가능 바리케이드 = 연결점). §2.
        //   • Pharmacy_Barricade_N — 창고 북단 통로(X8~18 Y54)를 막음. 철거 시 북쪽 약국/심부 폐상가 개방(SQ-002).
        //   • Basement_Block       — 창고 동측 안쪽. 밤 전용 지하창고(루디) 입구를 막음. 낮 데모에선 봉쇄.
        //  (남단 스폰 = 전당포/안전가옥 진입 게이트 → 바리케이드 없음, 입구 그대로.)
        // ─────────────────────────────────────────────────────────────────
        placed += Barricade(map, "Pharmacy_Barricade_N", 13f, 54f, 10f, 1f);  // 북 약국 통로 막음(철거→북 개방)
        placed += Barricade(map, "Basement_Block",       20f, 50f,  1f, 4f);  // 밤 지하창고(루디) 입구 봉쇄

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

        AssetDatabase.SaveAssets();

        Debug.Log($"<color=cyan>[ScrapMarketGB]</color> 생성 완료(v3): {ScenePath} — Map 하위 그레이박스 {placed}개 배치.\n" +
                  "  • v3 = 스토리(MQ-001 첫 출격) 동선 그대로: 철문 진입 → 폐상점 → 버려진 도로 → 밴딧 코너 → 창고(민이·노크규칙·쪽지) → 맨홀 탈출.\n" +
                  "  • 일방통행 선형(분기 없음), 4m 좁은 골목, 조밀 건물블록, 맵 44×56. 좁고 단순한 첫 지역.\n" +
                  "  • 벽/건물블록/바리케이드는 gb_wall/gb_barricade를 바로 스케일(콜라이더 동반). 바닥은 44×56 전체.\n" +
                  "  • 바리케이드 = 통합 연결점: 북단→약국(SQ-002), 창고 인근→밤 지하창고(루디). 둘 다 철거 가능.\n" +
                  "  • Systems 씬을 additive로 올린 뒤 Play하거나, MapTool 카탈로그 '맵 저장'으로 프리팹화 가능.");

        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("ScrapMarket Greybox",
                $"{ScenePath} 생성 완료(v3).\n\nMap 루트 하위에 그레이박스 {placed}개 배치.\n" +
                "스토리(MQ-001) 선형 동선(4m 골목, 조밀 건물블록, 44×56).\n" +
                "철문 진입 → 폐상점 → 버려진 도로 → 밴딧 코너 → 창고(민이·셔터·쪽지) → 맨홀 탈출.\n\n" +
                "맵 콘텐츠만 담긴 씬입니다. Systems 부트 씬이 카메라/조명/매니저/플레이어를 공급합니다.",
                "확인");
    }

    // ─────────────────────────────────────────────────────────────────────
    // 배치 헬퍼 (모두 PrefabUtility.InstantiatePrefab → gb_* 프리팹 링크 유지)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>벽 바/블록: gb_wall을 (lengthX, thicknessY, 1)로 스케일, 위치=중심.</summary>
    static int Wall(GameObject parent, string name, float cx, float cy, float lenX, float thickY)
        => Bar(parent, "gb_wall", name, cx, cy, lenX, thickY);

    /// <summary>바리케이드 바: gb_barricade를 스케일(철거 가능 → 통합 연결점 표시).</summary>
    static int Barricade(GameObject parent, string name, float cx, float cy, float lenX, float thickY)
        => Bar(parent, "gb_barricade", name, cx, cy, lenX, thickY);

    /// <summary>막대/블록형(벽/바리케이드) 인스턴스 생성: 중심 위치 + (lenX, thickY, 1) 스케일.</summary>
    static int Bar(GameObject parent, string prefabId, string name, float cx, float cy, float lenX, float thickY)
    {
        var go = Spawn(prefabId, name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(cx, cy, 0f);
        go.transform.localScale    = new Vector3(lenX, thickY, 1f);
        CounterScaleLabel(go);  // 라벨 글자는 늘어나지 않게(콜라이더는 영향 없음 — 가독성만)
        return 1;
    }

    /// <summary>바닥: gb_floor를 전체 영역으로 스케일. 정렬은 프리팹값(Ground/-10) 그대로 최하단.</summary>
    static int Floor(GameObject parent, string name, float cx, float cy, float w, float h)
    {
        var go = Spawn("gb_floor", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(cx, cy, 0f);
        go.transform.localScale    = new Vector3(w, h, 1f);
        CounterScaleLabel(go);
        return 1;
    }

    /// <summary>마커/오브젝트(스폰/탈출/적/NPC/상자/선반/문): 스케일 1로 좌표 배치.</summary>
    static int Marker(GameObject parent, string prefabId, string name, float x, float y)
    {
        var go = Spawn(prefabId, name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);
        // 스케일 1 유지(라벨 보정 불필요).
        return 1;
    }

    /// <summary>Resources에서 gb_* 프리팹 로드 → PrefabUtility.InstantiatePrefab으로 링크 인스턴스 생성.</summary>
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

    /// <summary>
    /// 막대/블록형으로 부모를 스케일하면 자식 'Label' TextMesh 글자가 늘어난다.
    /// 라벨 로컬 스케일을 부모 역수로 보정해 글자 비율을 유지(콜라이더/박스 충돌엔 무관, 가독성만).
    /// </summary>
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
