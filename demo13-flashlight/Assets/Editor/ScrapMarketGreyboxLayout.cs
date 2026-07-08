#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// "폐상가 (낮) — 첫 출격" 레벨 레이아웃 v6를 그레이박스 프리팹(gb_*)으로 배치한다.
/// (docs/level-scrapmarket.md §1 — MQ-001 첫 낮 레이드 / 스토리 동선 그대로 / 일방통행 선형)
///
/// **이 레이아웃은 두 경로로 쓰인다:**
///   • 단독 씬(메뉴): Tools ▸ TopDown ▸ Map ▸ Build ScrapMarket Greybox Layout → ScrapMarket_GB.unity
///   • **Zone1 통합**: Zone1GreyboxLayout이 `Place(map, ox, oy)`로 **좌하단(SW)에 그대로 배치**(v7 유기적 대각 버전 폐기).
///       → 사용자 피드백 "대각선 너무 많다 / 잘 만든 튜토 그대로 좌하단" (2026-06-06). 직교 v6를 오프셋만 줘서 재사용.
///
/// v6 요지(파밍을 '들어가는 건물' 안 + 창고도 건물):
///   • 파밍 = 출입문(gb_door)으로 들어가는 폐쇄 '건물' 3채(폐상점·차고·창고), 각 서벽 1칸 출입구로만 길과 연결되는 막다른 공간.
///   • 창고 = 막다른 건물(셔터 1입구). 민이 부재 = 흔적/쪽지(gb_note)만. 동선: 철문→[폐상점]→[차고]→골목 공터(밴딧·차)→[창고]→맨홀.
///   • 길 = 서측 세로 3m 골목(직선/직교). 맵 44×56.
///
/// 조밀화 원리: 길/건물 내부/출입구를 제외한 전 내부를 큰 gb_wall 블록으로 채운다(walkable 구멍 금지).
///
/// 좌표계 = XY 평면, Z=0. 1 unit = 1m. **모든 배치는 (OX,OY) 오프셋이 더해진다**(단독=0,0 / Zone1=좌하단).
/// </summary>
public static class ScrapMarketGreyboxLayout
{
    const string SceneDir   = "Assets/Scenes";
    const string ScenePath  = SceneDir + "/ScrapMarket_GB.unity";
    const string PrefabRoot = "Props2D/Prefabs/";   // Resources.Load 기준 경로

    // 맵 규모(문서 §1.2). 바닥/주석용. v6: 44 × 56 유지.
    const float MapW = 44f;
    const float MapH = 56f;

    // 배치 오프셋(좌하단 통합용). Place() 시작에서 설정. 단독 빌드=0,0.
    static float OX = 0f, OY = 0f;

    // 이 레이아웃이 쓰는 gb_* 프리팹(하나라도 없으면 빌드 시 팔레트 자동 생성).
    static readonly string[] RequiredPrefabIds =
    {
        "gb_floor", "gb_wall", "gb_barricade", "gb_door", "gb_crate",
        "gb_shelf", "gb_note", "gb_spawn", "gb_exit", "gb_enemy",
    };

    [MenuItem("Tools/TopDown/개발/고철시장 그레이박스(단독)")]
    public static void Build()
    {
        // ── 빈 씬 새로 시작(멱등: 같은 경로로 저장하면 기존 씬 덮어씀) ──
        var scene = EditorSceneBuildUtil.NewDetachedScene(out var prevActive);  // 현재 씬 유지(폴더에만 생성)
        var map = new GameObject("Map");

        // ── 그레이박스 팔레트(gb_*) 보장 + 레이드 매니저(단독 씬일 때만) ──
        EnsureGreyboxPalette();
        new GameObject("RaidManager").AddComponent<RaidManager>();

        // ── v6 레이아웃 배치(오프셋 0) ──
        int placed = Place(map, 0f, 0f);

        // ── 저장 ──
        Selection.activeObject = null;
        bool saved = EditorSceneBuildUtil.SaveAndClose(scene, ScenePath, prevActive);  // 저장 후 닫기(현재 씬 유지)

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
                  "  • 동선: 철문 진입 → [폐상점] → [차고] → 골목 공터(밴딧·차) → [창고: 흔적·쪽지] → 맨홀(골목 끝).\n" +
                  "  • 각 건물은 서벽 출입문 1칸으로만 연결되는 막다른 폐쇄 공간(안에서 루팅). 맵 44×56.\n" +
                  "  • ※ Zone1엔 ScrapMarketGreyboxLayout.Place(map, 좌하단)로 그대로 통합됨.");

        if (!Application.isBatchMode && !ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("ScrapMarket Greybox",
                $"{ScenePath} 생성 완료(v6).\n\nMap 루트 하위에 그레이박스 {placed}개 배치.\n" +
                "철문 → [폐상점] → [차고] → 골목 공터(밴딧·차) → [창고: 흔적·쪽지] → 맨홀.\n\n" +
                "맵 콘텐츠만 담긴 씬입니다(팔레트 gb_*는 없으면 자동 생성). Systems 부트 씬이 카메라/조명/매니저/플레이어를 공급합니다.",
                "확인");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  v6 레이아웃을 map 루트 하위에 (ox,oy) 오프셋으로 배치(단독·Zone1 통합 공용). 반환 = 배치 수.
    //  ※ 팔레트 보장/RaidManager/저장은 호출자 책임(단독 Build()만 수행).
    // ─────────────────────────────────────────────────────────────────────
    public static int Place(GameObject map, float ox = 0f, float oy = 0f, bool eastCorridor = false)
    {
        OX = ox; OY = oy;
        int placed = 0;

        // ── 바닥 ──
        placed += Floor(map, "Floor", 22f, 28f, MapW, MapH);

        // ── 외곽 벽(v3 그대로). 북벽 갭 X8~18 = 약국 통로(바리케이드 뒤) ──
        placed += Wall(map, "Wall_S",   22f,  0.5f, 44f, 1f);
        placed += Wall(map, "Wall_W",    0.5f, 28f,  1f, 56f);
        if (eastCorridor)   // 차고(2번째 블럭) 동측 통로 = 동벽을 Y27~32 갭으로 분할
        {
            placed += Wall(map, "Wall_E_S", 43.5f, 13.5f, 1f, 27f);  // Y0~27
            placed += Wall(map, "Wall_E_N", 43.5f, 44f,   1f, 24f);  // Y32~56
        }
        else placed += Wall(map, "Wall_E", 43.5f, 28f, 1f, 56f);
        placed += Wall(map, "Wall_N_W",  4f,  55.5f, 8f, 1f);   // X0~8
        placed += Wall(map, "Wall_N_E", 31f,  55.5f,26f, 1f);   // X18~44

        // ── 건물 블록(조밀화) — 길/건물내부/출입구 채널 제외 전 내부를 채움(walkable 구멍 금지) ──
        placed += Wall(map, "Bldg_S_base", 22f,  1.5f, 42f,  1f);  // 남단 토대 X1~43 Y1~2
        placed += Wall(map, "Bldg_W",       2.5f,28.5f, 3f, 53f);  // 서측 띠(전 높이) X1~4 Y2~55
        if (eastCorridor)   // 차고 동측 통로(갭 Y27~32) — 동측 대블록 분할
        {
            placed += Wall(map, "Bldg_E_S", 29.5f, 14.5f, 27f, 25f);  // X16~43 Y2~27
            placed += Wall(map, "Bldg_E_N", 29.5f, 43.5f, 27f, 23f);  // X16~43 Y32~55
        }
        else placed += Wall(map, "Bldg_E", 29.5f, 28.5f, 27f, 53f);   // 동측 대블록 X16~43 Y2~55
        // 길↔건물 벽띠 X7~8 (출입구 3칸 Y9~10·30~31·47~48 만큼 끊김)
        placed += Wall(map, "Bldg_door_a",  7.5f, 7f,   1f,  4f);  // X7~8 Y5~9
        placed += Wall(map, "Bldg_door_b",  7.5f,12f,   1f,  4f);  // X7~8 Y10~14
        placed += Wall(map, "Bldg_door_c",  7.5f,28f,   1f,  4f);  // X7~8 Y26~30
        placed += Wall(map, "Bldg_door_d",  7.5f,33f,   1f,  4f);  // X7~8 Y31~35
        placed += Wall(map, "Bldg_door_e",  7.5f,45.5f, 1f,  3f);  // X7~8 Y44~47
        placed += Wall(map, "Bldg_door_f",  7.5f,49f,   1f,  2f);  // X7~8 Y48~50
        // 건물 사이 막이 / 밴딧 공터 옆 (X7~16). 밴딧 공터는 Y36~43(차고 '뒤', 창고 직전).
        placed += Wall(map, "Bldg_z1",     11.5f, 3.5f, 9f,  3f);  // X7~16 Y2~5 (폐상점 남)
        placed += Wall(map, "Bldg_z2",     11.5f,20f,   9f, 12f);  // X7~16 Y14~26 (폐상점 북~차고 남)
        placed += Wall(map, "Bldg_z3",     11.5f,35.5f, 9f,  1f);  // X7~16 Y35~36 (차고 북~공터 남)
        placed += Wall(map, "Bldg_z4",     13f,  39.5f, 6f,  7f);  // X10~16 Y36~43 (밴딧 공터 동측)
        placed += Wall(map, "Bldg_z5",     11.5f,43.5f, 9f,  1f);  // X7~16 Y43~44 (공터 북~창고 남)
        placed += Wall(map, "Bldg_z6",     11.5f,50.5f, 9f,  1f);  // X7~16 Y50~51 (창고 북~맨홀공터 남)
        placed += Wall(map, "Bldg_z7",     15f,  53f,   2f,  4f);  // X14~16 Y51~55 (맨홀 공터 동측)

        // 1. 녹슨 철문 / 진입 (Spawn) — 남단. 길 ST(X4~7) 하단.
        placed += Marker(map, "gb_spawn", "Gate_Spawn", 6f, 3f);

        // 2. 폐상점 (실내 파밍 #1) — 방 X8~16 Y5~14. 서벽 출입문(Shop_Door Y9~10). 선반×2+박스×1.
        placed += Marker(map, "gb_door",  "Shop_Door",    7.5f, 9.5f);
        placed += Marker(map, "gb_shelf", "Shop_Shelf_1", 15f, 12f);
        placed += Marker(map, "gb_shelf", "Shop_Shelf_2", 15f, 7f);
        placed += Marker(map, "gb_crate", "Shop_Crate_1", 11f, 11f);

        // 3. 차고 (실내 파밍 #2) — 방 X8~16 Y26~35. 서벽 출입문(Garage_Door Y30~31). 선반×1+박스×2.
        placed += Marker(map, "gb_door",  "Garage_Door",    7.5f, 30.5f);
        placed += Marker(map, "gb_shelf", "Garage_Shelf_1", 15f, 33f);
        placed += Marker(map, "gb_crate", "Garage_Crate_1", 11f, 28f);
        placed += Marker(map, "gb_crate", "Garage_Crate_2", 14f, 28f);

        // 4. 골목 공터 (밴딧 + 야외 차) — PLAZA X4~10 Y36~43. 두 건물 파밍 '후' 첫 전투(창고 직전).
        placed += Marker(map, "gb_enemy", "Bandit_Corner", 6f, 40f);
        placed += EnemyZone(map, "Bandit_SpawnZone", 6f, 40f, 5f, 6f, "bandit_melee", 3);  // 런타임 적 3기
        placed += Marker(map, "gb_crate", "Road_Car_1",    8f, 38f);

        // 5. 창고 (메인 목표 — 막다름) — 방 X8~16 Y44~50. 셔터(Y47~48) 1입구. 민이 부재 = 흔적/쪽지.
        placed += Marker(map, "gb_door",  "Warehouse_Shutter",    7.5f, 47.5f);
        placed += Note(map, "Warehouse_Mini_Note",  11f, 46f, "창고 안 흔적",
            "셔터 아래쪽에 작은 손자국이 말라붙어 있다.\n안에서 누가 긁은 것 같은 자국도 보인다.\n\n……민이, 여기 있었구나. 지금은 어디 있지?");
        placed += Note(map, "Warehouse_Knock_Note", 14f, 48f, "벽에 적힌 낙서",
            "세 번 두드리면 대답해.\n한 번이면 숨고.\n두 번이면 울고.\n네 번이면 절대 열지 마.");

        // 6. 맨홀 탈출 (EXIT) — 창고 옆 골목 공터(CT X4~14 Y51~55). 추출구.
        placed += Exit(map, "Manhole_Exit", 9f, 53f, "Safehouse", "raid_return", 5f);

        // 통합 연결점 (철거 가능 바리케이드). §2.
        placed += Barricade(map, "Pharmacy_Barricade_N", 11f, 54.5f, 6f, 1f);  // 맨홀 공터 북단(X8~14)→약국/Zone1
        placed += Barricade(map, "Basement_Block",       15f, 47f,   1f, 4f);  // 창고 동측 안쪽→밤 지하창고(루디)

        // 7. 탐색 의뢰 POI 존 (게시판 탐색 의뢰 ReachPoint 대상 — QuestPoiZone). 각 존은 맵 지형지물에 얹음.
        //    poi_farm_sweep(3곳=파밍 건물 3채)·collapsed_shop·warehouse_noise·signal_source는 같은 건물에 겹쳐도 무방(다른 poiId).
        //    ⚠ poi_anomaly_edge/poi_deep_seal(짙은 현상)은 이 튜토 안전맵이 아니라 현상 레이드 소속 → 여기 미배치(그 맵 생기면 추가).
        placed += Poi(map, "POI_Warehouse", "poi_warehouse_noise", "창고 인근",    12f, 47f, 4f, 4f);  // 창고
        placed += Poi(map, "POI_Shop",      "poi_collapsed_shop",  "무너진 상점",  12f, 9f,  4f, 4f);  // 폐상점
        placed += Poi(map, "POI_North",     "poi_north_road",      "북쪽 진입로",  12f, 54f, 5f, 2f);  // 북벽 갭(약국 통로)
        placed += Poi(map, "POI_Signal",    "poi_signal_source",   "신호원",      12f, 30f, 4f, 4f);  // 차고(신호 추적 placeholder)
        // 근방 정밀 수색(BD-18) = 파밍 포인트 3곳 → 3개 존
        placed += Poi(map, "POI_Farm_1",    "poi_farm_sweep",      "수색 지점 1", 12f, 9f,  3f, 3f);
        placed += Poi(map, "POI_Farm_2",    "poi_farm_sweep",      "수색 지점 2", 12f, 30f, 3f, 3f);
        placed += Poi(map, "POI_Farm_3",    "poi_farm_sweep",      "수색 지점 3",  6f, 40f, 3f, 3f);  // 밴딧 공터

        return placed;
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
    // 배치 헬퍼 (모두 PrefabUtility.InstantiatePrefab → gb_* 링크 유지, (OX,OY) 오프셋 적용)
    // ─────────────────────────────────────────────────────────────────────

    static int Wall(GameObject parent, string name, float cx, float cy, float lenX, float thickY)
        => Bar(parent, "gb_wall", name, cx, cy, lenX, thickY);

    static int Barricade(GameObject parent, string name, float cx, float cy, float lenX, float thickY)
        => Bar(parent, "gb_barricade", name, cx, cy, lenX, thickY);

    static int Bar(GameObject parent, string prefabId, string name, float cx, float cy, float lenX, float thickY)
    {
        var go = Spawn(prefabId, name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(cx + OX, cy + OY, 0f);
        go.transform.localScale    = new Vector3(lenX, thickY, 1f);
        CounterScaleLabel(go);
        return 1;
    }

    /// <summary>탐색 의뢰 POI 존 배치 — 프리팹 없이 GameObject + BoxCollider2D(trigger) + QuestPoiZone.
    /// (cx,cy)=중심, (w,h)=크기. poiId는 의뢰 SO의 ReachPoint targetId와 일치.</summary>
    static int Poi(GameObject parent, string name, string poiId, string displayName, float cx, float cy, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = new Vector3(cx + OX, cy + OY, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(w, h);
        var z = go.AddComponent<QuestPoiZone>();
        z.poiId = poiId;
        z.displayName = displayName;
        return 1;
    }

    static int Floor(GameObject parent, string name, float cx, float cy, float w, float h)
    {
        var go = Spawn("gb_floor", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(cx + OX, cy + OY, 0f);
        go.transform.localScale    = new Vector3(w, h, 1f);
        CounterScaleLabel(go);
        return 1;
    }

    static int Marker(GameObject parent, string prefabId, string name, float x, float y)
    {
        var go = Spawn(prefabId, name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x + OX, y + OY, 0f);
        return 1;
    }

    /// <summary>적 스폰 존(SpawnZone): 영역(폭 w·높이 h)·유닛키·마릿수 설정. 런타임 EnemySpawner가 읽어 적 생성.</summary>
    static int EnemyZone(GameObject parent, string name, float cx, float cy, float w, float h, string unitKey, int count)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = new Vector3(cx + OX, cy + OY, 0f);
        go.AddComponent<SpawnZone>().Setup(new Vector3(w, 0f, h), count, unitKey);
        return 1;
    }

    /// <summary>쪽지(gb_note): 좌표 배치 + InteractableObject에 내용/제목 주입(읽으면 NoteUI 전체화면).</summary>
    static int Note(GameObject parent, string name, float x, float y, string title, string content)
    {
        var go = Spawn("gb_note", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x + OX, y + OY, 0f);
        var io = go.GetComponentInChildren<InteractableObject>();
        if (io != null) io.SetNote(content, title, "읽기");
        return 1;
    }

    /// <summary>탈출구(ExitPoint): 좌표 배치 + targetScene/spawnPointId/대기 설정(추출 = 상호작용 후 wait초).</summary>
    static int Exit(GameObject parent, string name, float x, float y, string targetScene, string spawnId, float wait)
    {
        var go = Spawn("gb_exit", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x + OX, y + OY, 0f);
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
