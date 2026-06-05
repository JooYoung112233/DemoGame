#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// "상가 골목 (낮)" 레벨 레이아웃 v1을 그레이박스 프리팹(gb_*)으로 테스트 씬에 자동 배치한다.
/// (docs/level-scrapmarket.md §1 — Z0~Z5 구역/동선/리스크리워드/막다른=철거가능 바리케이드/퀘스트 앵커)
///
/// 생성물: Assets/Scenes/ScrapMarket_GB.unity — '맵 콘텐츠'만 담는 씬.
///   카메라/조명/EventSystem/매니저/플레이어는 넣지 않는다(= Systems 부트 씬이 additive로 공급).
///   모든 그레이박스는 단일 'Map' 루트 GameObject 하위에 들어간다
///   (MapTool2D 규약과 동일 — 카탈로그 '맵 저장'으로 그대로 프리팹화 가능).
///
/// 배치 규칙(작업 지시 그대로):
///   • 좌표계 = XY 평면, Z=0. 1 unit = 1m. 맵 84(W) × 64(H). 남(Y=0)=입구, 북(Y=64)=깊은 곳.
///   • 벽/바리케이드 = gb_wall/gb_barricade 인스턴스를 '바(bar)'로 스케일.
///       transform.localScale = (lengthX, thicknessY, 1), 위치 = 바의 '중심'. 두께 ~1.
///       (BoxCollider2D는 transform 스케일을 따라가므로 충돌이 비주얼과 일치)
///   • 바닥 = gb_floor 1개를 84×64 전체로 스케일(중심 (42,32), scale (84,64,1)). 정렬 최하단.
///   • 마커(spawn/exit/enemy/npc)·상자/선반/문 = 스케일 1로 해당 좌표에 배치.
///   • 멱등: 씬이 있으면 덮어쓴다.
///   • 인스턴스는 PrefabUtility.InstantiatePrefab으로 만들어 gb_* 프리팹과 링크 유지.
///
/// 메뉴: Tools ▸ TopDown ▸ Map ▸ Build ScrapMarket Greybox Layout
/// 배치모드(executeMethod): ScrapMarketGreyboxLayout.Build
/// </summary>
public static class ScrapMarketGreyboxLayout
{
    const string SceneDir   = "Assets/Scenes";
    const string ScenePath  = SceneDir + "/ScrapMarket_GB.unity";
    const string PrefabRoot = "Props2D/Prefabs/";   // Resources.Load 기준 경로

    // 맵 규모(문서 §1.2). 바닥/주석용.
    const float MapW = 84f;
    const float MapH = 64f;

    [MenuItem("Tools/TopDown/Map/Build ScrapMarket Greybox Layout")]
    public static void Build()
    {
        // ── 빈 씬 새로 시작(멱등: 같은 경로로 저장하면 기존 씬 덮어씀) ──
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 모든 그레이박스의 단일 부모.
        var map = new GameObject("Map");

        int placed = 0;

        // ── 바닥: 전체 84×64 커버, 정렬 최하단(프리팹이 이미 Ground/-10) ──
        placed += Floor(map, "Floor", 42f, 32f, MapW, MapH);

        // ── 외곽 벽(전체 둘레) ──
        placed += Wall(map, "Wall_S", 42f, 0.5f, 84f, 1f);   // 남
        placed += Wall(map, "Wall_N", 42f, 63.5f, 84f, 1f);  // 북
        placed += Wall(map, "Wall_W", 0.5f, 32f, 1f, 64f);   // 서
        placed += Wall(map, "Wall_E", 83.5f, 32f, 1f, 64f);  // 동

        // ── Z0 진입 광장(남단, 개방) ──
        placed += Marker(map, "gb_spawn", "Spawn", 42f, 8f);       // S1 스폰(남중앙)
        placed += Marker(map, "gb_exit",  "Exit",  12f, 6f);       // E0 안전 탈출(남서)

        // ── Z2 편의점(서중앙 방, X6~32 Y26~44) — 남쪽에 골목 향한 입구 갭 ──
        placed += Wall(map, "Z2_Wall_N",  19f, 44f, 26f, 1f);     // 북
        placed += Wall(map, "Z2_Wall_S1", 11f, 26f, 10f, 1f);     // 남(서쪽 토막) — 갭 X16~22
        placed += Wall(map, "Z2_Wall_S2", 27f, 26f, 10f, 1f);     // 남(동쪽 토막)
        placed += Wall(map, "Z2_Wall_W",   6f, 35f, 1f, 18f);     // 서
        placed += Wall(map, "Z2_Wall_E",  32f, 35f, 1f, 18f);     // 동
        placed += Marker(map, "gb_crate", "Z2_Crate_1", 10f, 40f);
        placed += Marker(map, "gb_crate", "Z2_Crate_2", 14f, 40f);
        placed += Marker(map, "gb_crate", "Z2_Crate_3", 10f, 31f);
        placed += Marker(map, "gb_crate", "Z2_Crate_4", 28f, 40f);

        // ── Z3 셔터 상가(동중앙 방, X52~78 Y18~36) — 서쪽에 골목 향한 갭+셔터문 ──
        placed += Wall(map, "Z3_Wall_N",  65f, 36f, 26f, 1f);     // 북
        placed += Wall(map, "Z3_Wall_S",  65f, 18f, 26f, 1f);     // 남
        placed += Wall(map, "Z3_Wall_E",  78f, 27f, 1f, 18f);     // 동
        placed += Wall(map, "Z3_Wall_W1", 52f, 21f, 1f, 6f);      // 서(남쪽 토막) — 갭 Y24~30
        placed += Wall(map, "Z3_Wall_W2", 52f, 33f, 1f, 6f);      // 서(북쪽 토막)
        placed += Marker(map, "gb_door",  "Z3_Door",  52f, 27f);  // 셔터문(서쪽 갭)
        placed += Marker(map, "gb_shelf", "Z3_Shelf_1", 60f, 32f);
        placed += Marker(map, "gb_shelf", "Z3_Shelf_2", 70f, 32f);

        // ── Z4 잔해 건물(북서 막다른, X6~30 Y48~62) — 남쪽 좁은 입구, 철거가능 바리케이드로 막힘 ──
        placed += Wall(map, "Z4_Wall_N",  18f, 62f, 24f, 1f);     // 북
        placed += Wall(map, "Z4_Wall_S1", 10f, 48f, 8f, 1f);      // 남(서쪽 토막) — 갭 X14~22
        placed += Wall(map, "Z4_Wall_S2", 26f, 48f, 8f, 1f);      // 남(동쪽 토막)
        placed += Wall(map, "Z4_Wall_W",   6f, 55f, 1f, 14f);     // 서
        placed += Wall(map, "Z4_Wall_E",  30f, 55f, 1f, 14f);     // 동
        placed += Barricade(map, "Z4_Barricade", 18f, 48f, 8f, 1f); // 입구 갭 막음(철거→북 폐아파트 연결)
        placed += Marker(map, "gb_crate", "Z4_Crate", 12f, 58f);    // MQ-001 쪽지 보관
        placed += Marker(map, "gb_enemy", "Z4_Enemy", 24f, 55f);

        // ── Z5 뒷골목 고보상(북동 막다른, X52~78 Y46~62) — 남쪽 갭(협상꾼 게이트), 동쪽 끝 바리케이드(통합 링크) ──
        placed += Wall(map, "Z5_Wall_N",  65f, 62f, 26f, 1f);     // 북
        placed += Wall(map, "Z5_Wall_S1", 57f, 46f, 10f, 1f);     // 남(서쪽 토막) — 갭 X62~68
        placed += Wall(map, "Z5_Wall_S2", 73f, 46f, 10f, 1f);     // 남(동쪽 토막)
        placed += Wall(map, "Z5_Wall_W",  52f, 54f, 1f, 16f);     // 서
        placed += Barricade(map, "Z5_Barricade_E", 78f, 54f, 1f, 16f); // 동쪽 끝 막음(철거→유리타워 연결)
        placed += Marker(map, "gb_npc",   "Z5_NPC", 65f, 48f);    // 밴딧 협상꾼(게이트)
        placed += Marker(map, "gb_crate", "Z5_Crate_1", 58f, 58f);
        placed += Marker(map, "gb_crate", "Z5_Crate_2", 65f, 58f);
        placed += Marker(map, "gb_crate", "Z5_Crate_3", 72f, 58f);
        placed += Marker(map, "gb_enemy", "Z5_Enemy", 70f, 52f);

        // ── Z1 메인 골목: 노점/벤더 NPC(중앙 골목) ──
        placed += Marker(map, "gb_npc", "Z1_Vendor", 42f, 30f);

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

        Debug.Log($"<color=cyan>[ScrapMarketGB]</color> 생성 완료: {ScenePath} — Map 하위 그레이박스 {placed}개 배치.\n" +
                  "  • 맵 콘텐츠만(카메라/조명/매니저/플레이어 없음 → Systems 씬이 additive 공급).\n" +
                  "  • 벽/바리케이드는 gb_wall/gb_barricade를 바로 스케일(콜라이더 동반). 바닥은 84×64 전체 스케일.\n" +
                  "  • Systems 씬을 additive로 올린 뒤 Play하거나, MapTool 카탈로그 '맵 저장'으로 프리팹화 가능.");

        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("ScrapMarket Greybox",
                $"{ScenePath} 생성 완료.\n\nMap 루트 하위에 그레이박스 {placed}개 배치(Z0~Z5 + 외곽벽 + 골목).\n\n" +
                "맵 콘텐츠만 담긴 씬입니다. Systems 부트 씬이 카메라/조명/매니저/플레이어를 공급합니다.",
                "확인");
    }

    // ─────────────────────────────────────────────────────────────────────
    // 배치 헬퍼 (모두 PrefabUtility.InstantiatePrefab → gb_* 프리팹 링크 유지)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>벽 바: gb_wall을 (lengthX, thicknessY, 1)로 스케일, 위치=중심.</summary>
    static int Wall(GameObject parent, string name, float cx, float cy, float lenX, float thickY)
        => Bar(parent, "gb_wall", name, cx, cy, lenX, thickY);

    /// <summary>바리케이드 바: gb_barricade를 스케일(철거 가능 → 통합 연결점 표시).</summary>
    static int Barricade(GameObject parent, string name, float cx, float cy, float lenX, float thickY)
        => Bar(parent, "gb_barricade", name, cx, cy, lenX, thickY);

    /// <summary>막대형(벽/바리케이드) 인스턴스 생성: 중심 위치 + (lenX, thickY, 1) 스케일.</summary>
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
    /// 막대형으로 부모를 스케일하면 자식 'Label' TextMesh 글자가 늘어난다.
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
