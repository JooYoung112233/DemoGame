#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// "지역1(Zone1)" — **폐상가 도심 그레이박스**(좀보이드式). (docs/world-map.md §7-5)
///   • 똑같은 격자 = 거부 → **블록마다 성격 다르게**: 작은 점포(폭·깊이 제각각+공터) / 큰 건물 / 광장(주차장·공원).
///   • 좌표 = 월드 XY, 1u=1m.
///
/// ★ 2026-07-11 v2 — **크기 1/2 + 도로 위계 컨셉**(사용자: "맵이 너무 밀도가 없다").
///   구: 316×330(≈10만 m²)에 도로가 전부 6~11m로 비슷 → 넓고 밋밋한 격자.
///   신: **160×168**(≈2.7만 m², 선형 1/2) + 도로를 4단계로 구분해 '길의 성격'을 만든다.
///
///   ┌ 도로 위계 ──────────────────────────────────────────────────────┐
///   │ 4차선 간선 12m — 십자 1쌍(세로 x102~114 · 가로 y114~126). 맵의 척추.       │
///   │                  랜드마크 4채의 문이 전부 이 십자를 향한다(길 찾기 쉬움).    │
///   │ 2차선 지선  6m — 순환도로(맵 둘레) + 블록 분할(세로 x60~66 · 가로 y72~78). │
///   │ 골목        3m — 블록 안 점포 사이(Shops).                                │
///   │ 실개골목    2m — 밀집 아케이드(약국 심부)만.                              │
///   └────────────────────────────────────────────────────────────────┘
///
///   블록 = 3열 × 3행 = 9개(구 30개). 큰 빈 블록을 없애고 점포를 촘촘히.
///   스폰5 = 순환도로 둘레 / 탈출 = 중앙 교차로(고정) + 순환도로 네 코너(풀).
///
/// 메뉴: Tools ▸ TopDown ▸ 빌드 ▸ 지역1
/// </summary>
public static class Zone1GreyboxLayout
{
    const string ScenePath = "Assets/Scenes/Zone1.unity";

    // ── 맵 규격 (160 × 168) ──
    const float FX0 = 8f, FY0 = 8f, FX1 = 168f, FY1 = 176f;
    const float TUT_OX = 16f, TUT_OY = 16f;   // 튜토(폐상가) 44×56 = C0R0에 정확히 들어맞음

    // ── 도로 위계(폭) ──
    public const float RoadArterial  = 12f;   // 4차선 간선
    public const float RoadCollector = 6f;    // 2차선 지선 · 순환도로
    public const float Alley         = 3f;    // 골목(점포 사이)
    public const float AlleyTight    = 2f;    // 실개골목(밀집 아케이드)

    // 블록 격자 — 사이 간격이 곧 도로. C0|지선|C1|간선|C2, R0|지선|R1|간선|R2. 둘레는 순환도로.
    static readonly float[] CX0 = {  16f,  66f, 114f };
    static readonly float[] CX1 = {  60f, 102f, 160f };
    static readonly float[] RY0 = {  16f,  78f, 126f };
    static readonly float[] RY1 = {  72f, 114f, 168f };

    // 점포 크기 풀 — 맵이 절반이 됐으므로 점포도 잘게(밀도↑, 골목 수↑).
    static readonly float[] WS = { 7f, 9f, 11f, 14f, 17f };
    static readonly float[] DS = { 8f, 10f, 12f, 15f };

    [MenuItem("Tools/TopDown/빌드/지역1", priority = -98)]
    public static void Build()
    {
        var map = GreyboxBuild.BeginScene(out var scene);
        _buildingRects.Clear();   // 건물 자리 등록 초기화(빌드마다 새로) — Scatter가 실내를 피하는 근거
        _keepOut.Clear();         // 장애물 금지 지점(스폰·탈출·입구)
        int n = 0;

        n += GreyboxBuild.Floor(map, "Floor", (FX0+FX1)*0.5f, (FY0+FY1)*0.5f, FX1-FX0, FY1-FY0);
        n += GreyboxBuild.WallSeg(map, "Edge_S", FX0, FY0, FX1, FY0+2f);
        n += GreyboxBuild.WallSeg(map, "Edge_N_HanRiver", FX0, FY1-2f, FX1, FY1);
        n += GreyboxBuild.WallSeg(map, "Edge_W", FX0, FY0, FX0+2f, FY1);
        n += GreyboxBuild.WallSeg(map, "Edge_E", FX1-2f, FY0, FX1, FY1);

        // 튜토(SW = C0R0, 44×56이 블록에 정확히 맞음).
        //   北 갭(월드 x24~34) → 가로 지선 → 약국 아케이드 / 東 갭(월드 y43~48) → 세로 지선(x60~66).
        n += ScrapMarketGreyboxLayout.Place(map, TUT_OX, TUT_OY, true);

        // ── 개활 블록에 얹는 유니크 2채 (주차장 안 경찰서 · 공원 안 분식집) ──
        //   **블록 루프보다 먼저** 세운다 — 그래야 Plaza가 이 자리를 알고 키오스크·상자를 피한다
        //   (블록이 절반으로 작아진 뒤로 "건물 안에 상자"가 쉽게 재발한다).
        n += UniqueShop(map, "Police", "경찰서 ★★★★", "Int_Police", "from_police",
                        116f, 95f, 134f, 111f, 'W', 101f, 3, "bandit_melee_1",
                        "무기고 뒷문은 잠겨 있다(열쇠). 로비 압수품 대장에 **보석상 금고 번호**가 적혀 있다.");
        n += UniqueShop(map, "Diner", "분식집 ★", "Int_Diner", "from_diner",
                        38f, 130f, 52f, 143f, 'S', 43f, 0, "bandit_melee_1",
                        "캔푸드·물. 주방 뒤 창고. 공원 옆이라 조용하다.");

        // ── 블록 9개, 성격 전부 다르게 ──
        //   C0R0=튜토 / C0R1=약국 아케이드 / C0R2=공원
        //   C1R0=폐아파트 / C1R1=점포 밀집 / C1R2=식물원 돔
        //   C2R0=유리타워 / C2R1=주차장 / C2R2=무너진 상가
        //   ※ 랜드마크 4채(아파트·타워·돔·상가)의 문은 전부 **간선 십자**를 향한다.
        for (int r = 0; r < RY0.Length; r++)
        for (int c = 0; c < CX0.Length; c++)
        {
            if (c == 0 && r == 0) continue;  // 튜토 자리(위에서 Place로 배치됨)
            float ax0 = CX0[c], ay0 = RY0[r], ax1 = CX1[c], ay1 = RY1[r];
            string p = $"B{c}{r}";
            int seed = H(c + 1, r + 1);

            if      (c == 0 && r == 1) n += BuildPharmacyArcade(map);                       // 약국·상가 심부(상세)
            else if (c == 0 && r == 2) n += Plaza(map, "Park", ax0, ay0, ax1, ay1);         // 공원
            else if (c == 1 && r == 0) n += Big(map, "Apt",  ax0, ay0, ax1, ay1, 'E');      // 폐아파트(문=세로 간선)
            else if (c == 1 && r == 2) n += BuildGreenhouseDome(map);                       // 식물원 돔(상세)
            else if (c == 2 && r == 0) n += Big(map, "Tower", ax0, ay0, ax1, ay1, 'W');     // 유리타워(문=세로 간선)
            else if (c == 2 && r == 1) n += Plaza(map, "Lot", ax0, ay0, ax1, ay1);          // 주차장
            else if (c == 2 && r == 2) n += BuildCollapsedMall(map, ax0, ay0, ax1, ay1);    // 무너진 상가(전용 내부)
            else if (c == 1 && r == 1) n += BuildUniqueRow(map);                            // 유니크 상점가 4채
            else                       n += Shops(map, p, ax0, ay0, ax1, ay1, Alley, seed); // 점포 밀집
        }


        // ── 랜드마크 라벨/열쇠 사슬 — 해당 건물이 면한 간선·지선 위에 ──
        n += GreyboxBuild.Note(map, "AP_Label", 108f, 24f, "폐아파트 ★★", "세로 간선 西. key_apt_admin → 펜트 key_tower_card. 수직 다층 후속.");
        n += GreyboxBuild.Marker(map, "gb_crate", "key_apt_admin", 108f, 36f);
        n += GreyboxBuild.Marker(map, "gb_door",  "Pent_Gate(key_apt_admin)", 108f, 48f);
        n += GreyboxBuild.Marker(map, "gb_crate", "key_tower_card", 108f, 60f);
        n += GreyboxBuild.Note(map, "TW_Label", 132f, 75f, "유리타워 ★★★★", "동측. key_tower_card로 상층 R&D → key_dome_code. 카드키·수직 후속.");
        n += GreyboxBuild.Marker(map, "gb_door",  "RnD_Gate(key_tower_card)", 144f, 75f);
        n += GreyboxBuild.Marker(map, "gb_crate", "key_dome_code", 154f, 75f);
        n += GreyboxBuild.Note(map, "RV_Label", 88f, 171f, "강변 부두 ★★★", "最北 한강 경계(순환도로 北). 부두 창고. 탈출 밀집.");

        // ── 레이드 스폰 5(매 판 랜덤 1곳) — **순환도로 둘레**에 분산 ──
        //   pointId를 오브젝트명과 같게 주입해야 SpawnPoint가 실제로 식별된다(주입 없으면 프리팹 기본값 "default").
        n += Spawn5(map, "SP1_S",  110f,  13f);   // 남 순환
        n += Spawn5(map, "SP2_N",  110f, 171f);   // 북 순환
        n += Spawn5(map, "SP3_W",   13f,  96f);   // 서 순환
        n += Spawn5(map, "SP4_E",  163f,  96f);   // 동 순환
        n += Spawn5(map, "SP5_NE", 163f, 150f);   // 북동 순환
        // ── 탈출: 고정 1(중앙 교차로) + 풀 4(순환도로 네 코너 — 스폰별 2개 매치로 맵 횡단 유도) ──
        //   ExitPoint 설정(targetScene/spawnPointId/대기)을 주입해야 실제 탈출로 동작한다.
        n += ExitPt(map, "Exit_Fixed", 108f, 120f);  // 고정 = 간선 십자 교차점
        n += ExitPt(map, "PX_SW",  13f,  13f);
        n += ExitPt(map, "PX_SE", 163f,  13f);
        n += ExitPt(map, "PX_NW",  13f, 171f);
        n += ExitPt(map, "PX_NE", 163f, 171f);
        // 매치(스폰→풀 2, 고정 제외): SP1_S→{NW,NE} · SP2_N→{SW,SE} · SP3_W→{SE,NE} · SP4_E→{SW,NW} · SP5_NE→{SW,NW}
        // → 런타임 랜덤스폰 + 매치 탈출 활성 = RaidSpawnDirector(아래 배치). 매치표는 디렉터가 동일하게 보유.
        n += Director(map);

        // ── 루팅 예산제(MapSpawnController + ItemSpawnPoint 앵커) + 적 밀도 ──
        //   예산은 맵 전체 총량 → 앵커를 많이 심은 구역에 루트가 몰린다 = 보상 곡선.
        //   난이도는 SpawnZone 유닛키/마릿수로 = 위험 곡선. (결정 2026-07-11: 안쪽으로 갈수록 위험·보상 ↑)
        n += Controller(map);

        // ① 약국 아케이드(C0R1) — 약함·잡템
        n += Scatter(map, "SZ_Arcade", 18f, 80f, 58f, 112f, 4, 3, 11);
        n += EnemyZone(map, "EZ_Arcade", 38f, 96f, 14f, 16f, "bandit_melee_1", 2);

        // ② 폐아파트(C1R0) — 중
        n += Scatter(map, "SZ_Apt", 68f, 18f, 100f, 70f, 5, 4, 22);
        n += EnemyZone(map, "EZ_Apt", 84f, 44f, 16f, 18f, "bandit_melee_1", 3);

        // ③ 식물원 돔·습지(C1R2) — 중상
        n += Scatter(map, "SZ_Dome", 68f, 128f, 100f, 166f, 5, 5, 33);
        n += EnemyZone(map, "EZ_Dome", 84f, 146f, 14f, 14f, "bandit_melee_1", 2);
        n += EnemyZone(map, "EZ_Dome_R", 92f, 157f, 8f, 8f, "bandit_ranged", 1);

        // ④ 유리 R&D 타워(C2R0) — 강함·고급 루트(최심부)
        n += Scatter(map, "SZ_Tower", 116f, 18f, 158f, 70f, 6, 6, 44);
        n += EnemyZone(map, "EZ_Tower", 140f, 40f, 16f, 14f, "bandit_ranged", 2);
        n += EnemyZone(map, "EZ_Tower_T", 150f, 52f, 8f, 8f, "bandit_tank", 1);

        // ⑤ 무너진 상가(C2R2) — 중상
        n += Scatter(map, "SZ_Mall", 116f, 128f, 158f, 166f, 4, 3, 77);
        n += EnemyZone(map, "EZ_Mall", 137f, 146f, 16f, 16f, "bandit_melee_1", 2);

        // ⑥ 주차장·공원(개활지) — 낮은 밀도, 적 약간
        n += Scatter(map, "SZ_Lot", 116f, 80f, 158f, 112f, 3, 2, 55);
        n += EnemyZone(map, "EZ_Lot", 137f, 96f, 16f, 16f, "bandit_melee_1", 2);
        n += Scatter(map, "SZ_Park", 18f, 128f, 58f, 166f, 3, 2, 66);

        // ⑦ 도로 파밍은 **바닥에 뿌리지 않는다** — 잔해(부서진 차)의 트렁크에 붙는다.
        //   아래 ObstacleRun이 3대 중 1대를 트렁크로 만든다(사용자: "도로 중앙에 말고 … 유기적으로").

        // ── ★ 도로 장애물 (마지막 — 스폰·탈출·입구가 다 등록된 뒤라야 그 자리를 피한다) ──
        //   "넓은 길 = 빠르지만 직선으로는 못 달린다". 좌우 번갈아 붙여 통행선을 지그재그로.
        //   간선(12m)은 크게 물어 뜯고, 지선(6m)·순환(6m)은 한 대씩만 — 폭에 비례해 압박.
        n += ObstacleRun(map, "OB_ArtV", true,  102f, 114f,  12f, 172f, 16f, 0.55f, 201);  // 세로 간선
        n += ObstacleRun(map, "OB_ArtH", false, 114f, 126f,  12f, 164f, 16f, 0.55f, 202);  // 가로 간선
        n += ObstacleRun(map, "OB_ColV", true,   60f,  66f,  12f, 172f, 24f, 0.45f, 203);  // 세로 지선
        n += ObstacleRun(map, "OB_ColH", false,  72f,  78f,  12f, 164f, 24f, 0.45f, 204);  // 가로 지선
        n += ObstacleRun(map, "OB_RingS", false, 10f,  16f,  20f, 156f, 30f, 0.42f, 205);  // 순환 남
        n += ObstacleRun(map, "OB_RingN", false,168f, 174f,  20f, 156f, 30f, 0.42f, 206);  // 순환 북
        n += ObstacleRun(map, "OB_RingW", true,   10f, 16f,  20f, 164f, 30f, 0.42f, 207);  // 순환 서
        n += ObstacleRun(map, "OB_RingE", true,  160f,166f,  20f, 164f, 30f, 0.42f, 208);  // 순환 동

        // ── 길을 아예 끊는 것들 = "못 가는 곳 / 돌아가야 하는 곳" ──
        //   ① 영구: 세로 간선 중간의 무너진 고가 — **척추가 끊긴다.** 남↔북은 지선이나 순환으로 우회.
        n += Blocker(map, "BLK_Overpass", 108f, 66f, 12f, 5f,
                     BlockedPassage.Mode.Permanent, "무너진 고가도로");
        //   ② 치울 수 있는 지름길 3곳 — 치우면 빨라지지만 **소음**으로 적이 몰린다(대가 있는 지름길).
        n += Blocker(map, "BLK_ColV", 63f, 100f, 6f, 3.5f,
                     BlockedPassage.Mode.Clearable, "쌓인 폐자재");
        n += Blocker(map, "BLK_ColH", 92f, 75f, 3.5f, 6f,
                     BlockedPassage.Mode.Clearable, "전복된 트럭");
        n += Blocker(map, "BLK_ArtH", 60f, 120f, 3.5f, 12f,
                     BlockedPassage.Mode.Clearable, "사고 차량 더미");
        //   ③ 밤에만 지나갈 수 있는 골목 — 낮엔 우회(짙은현상 게이트의 전신).
        n += Blocker(map, "BLK_NightAlley", 163f, 130f, 6f, 3.5f,
                     BlockedPassage.Mode.NightOnly, "잠긴 셔터");

        GreyboxBuild.EndScene(scene, ScenePath, n, "지역1 Zone1(160×168 · 도로 위계 4단계 · 장애물/차단)");
        AddToBuildSettings(ScenePath);   // 등록 안 하면 TransitionTo("Zone1")이 LoadSceneAsync에서 실패
        AssetDatabase.SaveAssets();
    }

    /// <summary>씬을 빌드세팅에 등록(이미 있으면 무시). ScrapMarketGreyboxLayout과 동일 패턴.</summary>
    static void AddToBuildSettings(string scenePath)
    {
        var cur = EditorBuildSettings.scenes;
        foreach (var s in cur) if (s.path == scenePath) return;
        var arr = new EditorBuildSettingsScene[cur.Length + 1];
        System.Array.Copy(cur, arr, cur.Length);
        arr[cur.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = arr;
        Debug.Log($"[Zone1] 빌드세팅 등록: {scenePath}");
    }

    // ── 스폰/탈출/디렉터 (마커 → 실제 동작하는 컴포넌트) ────────────────────

    /// <summary>레이드 스폰 후보(gb_spawn = SpawnPoint). pointId를 오브젝트명과 동일하게 주입.</summary>
    static int Spawn5(GameObject map, string name, float x, float y)
    {
        if (GreyboxBuild.Marker(map, "gb_spawn", name, x, y) == 0) return 0;
        KeepOut(x, y);   // 스폰 자리엔 도로 잔해를 깔지 않는다(스폰하자마자 끼는 사고 방지)
        var go = FindChild(map.transform, name);
        var sp = go != null ? go.GetComponentInChildren<SpawnPoint>() : null;
        if (sp != null)
        {
            var so = new SerializedObject(sp);
            var p = so.FindProperty("pointId");
            if (p != null) { p.stringValue = name; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
        return 1;
    }

    /// <summary>탈출구(gb_exit = InteractableObject/ExitPoint). 안전가옥 복귀로 설정.</summary>
    static int ExitPt(GameObject map, string name, float x, float y)
    {
        if (GreyboxBuild.Marker(map, "gb_exit", name, x, y) == 0) return 0;
        KeepOut(x, y);   // 탈출구 자리도 비워 둔다
        var go = FindChild(map.transform, name);
        var io = go != null ? go.GetComponentInChildren<InteractableObject>() : null;
        if (io != null)
        {
            var so = new SerializedObject(io);
            var ts = so.FindProperty("targetScene");   if (ts != null) ts.stringValue = "Safehouse";
            var sp = so.FindProperty("spawnPointId");  if (sp != null) sp.stringValue = "default";
            var ew = so.FindProperty("exitWaitTime");  if (ew != null) ew.floatValue = 5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return 1;
    }

    // ── 루팅 예산제 / 적 밀도 ────────────────────────────────────────────

    /// <summary>루팅 예산 분배기(MapSpawnController) — 지역1 프로파일 + region_loot 지역 지정.</summary>
    static int Controller(GameObject map)
    {
        var go = new GameObject("MapSpawnController");
        go.transform.SetParent(map.transform, false);
        var c = go.AddComponent<MapSpawnController>();

        var profile = AssetDatabase.LoadAssetAtPath<MapSpawnProfile>("Assets/Resources/Data/MapSpawn/scrap_market.asset");
        if (profile == null) Debug.LogWarning("[Zone1] MapSpawnProfile 'Zone1.asset' 로드 실패 — 예산제 비활성(앵커는 자체 폴백 스폰).");

        var so = new SerializedObject(c);
        var p  = so.FindProperty("profile");           if (p  != null) p.objectReferenceValue = profile;
        var r  = so.FindProperty("regionIdOverride");  if (r  != null) r.stringValue = "scrap_market";   // region_loot 지역1
        var fb = so.FindProperty("fallbackToRegionLoot"); if (fb != null) fb.boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        return 1;
    }

    /// <summary>구역에 루트 앵커를 뿌린다 — 바닥 ground개 + 상자 crate개(상자엔 Container 앵커 부착).
    /// 앵커 수 = 그 구역의 보상 비중(예산이 앵커에 분배되므로). 시드로 결정론적 배치.</summary>
    // ── 건물 자리 등록 (2026-07-11) ──────────────────────────────────────
    //   건물 모델이 "껍데기 + 별도 내부 씬"으로 바뀌면서, **외부 맵의 루트가 건물 안에 놓이면 안 된다**
    //   (사용자 지적: "건물 안에 상자가 보인다 — 건물 시스템이 안 맞는다").
    //   외부 루트 = 길·골목·공터에만. 건물 내부 파밍은 Int_* 씬이 담당한다.
    //   → 건물을 지을 때마다 바닥 사각형을 등록해 두고, Scatter가 그 안을 피한다.
    static readonly System.Collections.Generic.List<Rect> _buildingRects = new System.Collections.Generic.List<Rect>();

    /// <summary>건물 바닥 사각형 등록(외곽 벽 포함). 여유 0.6m를 둬 벽에 붙은 루트도 배제.</summary>
    static void MarkBuilding(float x0, float y0, float x1, float y1)
    {
        const float pad = 0.6f;
        _buildingRects.Add(new Rect(x0 - pad, y0 - pad, (x1 - x0) + pad * 2f, (y1 - y0) + pad * 2f));
    }

    /// <summary>존 중심을 가장 가까운 실외로 옮기고, 건물에 걸치면 크기를 줄인다.
    /// (적이 건물 껍데기 안에서 스폰되는 것을 막는다 — 실내 전투는 Int_* 씬 담당.)</summary>
    static void SnapOutdoors(ref float cx, ref float cy, ref float w, ref float h)
    {
        if (IsIndoors(cx, cy))
        {
            // 나선 탐색 — 가까운 실외 지점으로.
            bool found = false;
            for (float r = 3f; r <= 40f && !found; r += 3f)
            {
                for (int a = 0; a < 12; a++)
                {
                    float t = a * Mathf.PI * 2f / 12f;
                    float nx = cx + Mathf.Cos(t) * r, ny = cy + Mathf.Sin(t) * r;
                    if (IsIndoors(nx, ny)) continue;
                    cx = nx; cy = ny; found = true; break;
                }
            }
            if (!found) { w = h = 0f; return; }   // 못 찾으면 존을 0으로(스폰 안 함)
        }

        // 존 네 모서리가 건물에 걸리면 걸치지 않을 때까지 축소.
        for (int i = 0; i < 6; i++)
        {
            float hx = w * 0.5f, hy = h * 0.5f;
            bool ok = !IsIndoors(cx - hx, cy - hy) && !IsIndoors(cx + hx, cy - hy)
                   && !IsIndoors(cx - hx, cy + hy) && !IsIndoors(cx + hx, cy + hy);
            if (ok) return;
            w *= 0.7f; h *= 0.7f;
            if (w < 2f || h < 2f) { w = Mathf.Max(w, 2f); h = Mathf.Max(h, 2f); return; }
        }
    }

    static bool IsIndoors(float x, float y)
    {
        for (int i = 0; i < _buildingRects.Count; i++)
            if (_buildingRects[i].Contains(new Vector2(x, y))) return true;
        return false;
    }

    /// <summary>영역 안에서 **건물 밖** 좌표를 뽑는다. 실패(꽉 찬 블록)하면 false → 그 앵커는 생략.</summary>
    static bool PickOutdoorPoint(System.Random rnd, float x0, float y0, float x1, float y1,
                                 out float x, out float y)
    {
        for (int t = 0; t < 24; t++)
        {
            x = Mathf.Lerp(x0, x1, (float)rnd.NextDouble());
            y = Mathf.Lerp(y0, y1, (float)rnd.NextDouble());
            if (!IsIndoors(x, y)) return true;
        }
        x = y = 0f;
        return false;
    }

    /// <summary>건물·잔해 **벽에 붙는** 좌표를 뽑는다 — 도로 한복판에 상자가 둥둥 뜨는 걸 막는다.
    /// (2026-07-11 사용자: "도로 중앙에 말고 건물에 붙어서나 이런 유기적인 방향으로")
    ///
    /// 등록된 사각형(건물·도로 잔해) 중 이 구역과 겹치는 걸 하나 골라, **한 면 바깥 0.6~1.3m**를 잡는다.
    /// 실제 벽면과의 거리는 여기에 등록 여유 0.6m가 더해져 1.2~1.9m — 벽에 기댄 잔해처럼 읽힌다.
    /// 붙일 데가 아예 없는 개활지(광장·공원)면 무작위 실외로 폴백한다.</summary>
    static readonly System.Collections.Generic.List<Rect> _hugBuf = new System.Collections.Generic.List<Rect>();
    static bool PickHuggingPoint(System.Random rnd, float x0, float y0, float x1, float y1,
                                 out float x, out float y)
    {
        var area = Rect.MinMaxRect(x0, y0, x1, y1);
        _hugBuf.Clear();
        for (int i = 0; i < _buildingRects.Count; i++)
            if (_buildingRects[i].Overlaps(area)) _hugBuf.Add(_buildingRects[i]);

        for (int t = 0; t < 24 && _hugBuf.Count > 0; t++)
        {
            var r = _hugBuf[rnd.Next(_hugBuf.Count)];
            float off = 0.6f + (float)rnd.NextDouble() * 0.7f;
            switch (rnd.Next(4))
            {
                case 0:  x = Mathf.Lerp(r.xMin, r.xMax, (float)rnd.NextDouble()); y = r.yMin - off; break;  // 남면
                case 1:  x = Mathf.Lerp(r.xMin, r.xMax, (float)rnd.NextDouble()); y = r.yMax + off; break;  // 북면
                case 2:  x = r.xMin - off; y = Mathf.Lerp(r.yMin, r.yMax, (float)rnd.NextDouble());  break;  // 서면
                default: x = r.xMax + off; y = Mathf.Lerp(r.yMin, r.yMax, (float)rnd.NextDouble());  break;  // 동면
            }
            if (x < x0 || x > x1 || y < y0 || y > y1) continue;   // 이 구역 밖으로 튀면 버림
            if (IsIndoors(x, y)) continue;                        // 옆 건물/잔해에 파묻히면 버림
            return true;
        }
        return PickOutdoorPoint(rnd, x0, y0, x1, y1, out x, out y);   // 개활지 폴백
    }

    static int Scatter(GameObject map, string prefix, float x0, float y0, float x1, float y1, int ground, int crate, int seed)
    {
        int n = 0;
        var rnd = new System.Random(seed);
        for (int i = 0; i < ground; i++)
        {
            if (!PickHuggingPoint(rnd, x0, y0, x1, y1, out float x, out float y)) continue;
            var go = new GameObject($"{prefix}_G{i}");
            go.transform.SetParent(map.transform, false);
            go.transform.localPosition = new Vector3(x, y, 0f);
            SetSpawnType(go.AddComponent<ItemSpawnPoint>(), 0);   // Ground
            n++;
        }
        for (int i = 0; i < crate; i++)
        {
            if (!PickHuggingPoint(rnd, x0, y0, x1, y1, out float x, out float y)) continue;
            string name = $"{prefix}_C{i}";
            if (GreyboxBuild.Marker(map, "gb_crate", name, x, y) == 0) continue;
            var go = FindChild(map.transform, name);
            if (go == null) continue;
            var lc = go.GetComponentInChildren<LootContainer>();
            var sp = go.gameObject.AddComponent<ItemSpawnPoint>();
            SetSpawnType(sp, 1);                                   // Container
            if (lc != null)
            {
                var so = new SerializedObject(sp);
                var lk = so.FindProperty("linkedContainer");
                if (lk != null) { lk.objectReferenceValue = lc; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
            n++;
        }
        return n;
    }

    /// <summary>ItemSpawnPoint.spawnType(private) 주입. 0=Ground 1=Container 2=Fixed.</summary>
    static void SetSpawnType(ItemSpawnPoint sp, int type)
    {
        var so = new SerializedObject(sp);
        var t = so.FindProperty("spawnType");
        if (t != null) { t.enumValueIndex = type; so.ApplyModifiedPropertiesWithoutUndo(); }
    }

    /// <summary>적 스폰 존(SpawnZone) — 런타임 EnemySpawner가 읽어 생성. 난이도 곡선용.</summary>
    static int EnemyZone(GameObject map, string name, float cx, float cy, float w, float h, string unitKey, int count)
    {
        // 2026-07-11: 적 존이 **건물 안**에 있으면 적이 껍데기 안에서 스폰됐다가 문으로 걸어 나온다
        //   (사용자 지적: "적이 건물 안에 있는데 건물 밖으로 나가진다"). 건물 내부 전투는 Int_* 씬 담당.
        //   → 중심을 실외로 스냅하고, 건물에 걸치지 않게 크기를 줄인다.
        SnapOutdoors(ref cx, ref cy, ref w, ref h);

        var go = new GameObject(name);
        go.transform.SetParent(map.transform, false);
        go.transform.localPosition = new Vector3(cx, cy, 0f);
        go.AddComponent<SpawnZone>().Setup(new Vector3(w, 0f, h), count, unitKey);
        return 1;
    }

    /// <summary>랜덤 스폰 + 매치 탈출 활성 디렉터(런타임).</summary>
    static int Director(GameObject map)
    {
        var go = new GameObject("RaidSpawnDirector");
        go.transform.SetParent(map.transform, false);
        go.AddComponent<RaidSpawnDirector>();
        return 1;
    }

    static Transform FindChild(Transform t, string n)
    {
        if (t.name == n) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var r = FindChild(t.GetChild(i), n);
            if (r != null) return r;
        }
        return null;
    }

    /// <summary>
    /// 약국·상가 심부(상세) — C0R1(x16~60 y78~114). 맵에서 **가장 촘촘한** 구역(실개골목 2m).
    ///   동선: 튜토 北 갭(x24~34) → 지선 → 남 갭 → 세로 스파인(x28~34) → 북 갭(→ 가로 간선) / 동 갭(→ 세로 지선).
    ///   약국 미니퍼즐: key_pharmacy(카운터) → MedCabinet(잠금) → SQ-002. 의료·생필품 루팅.
    /// </summary>
    static int BuildPharmacyArcade(GameObject m)
    {
        const float X0 = 16f, Y0 = 78f, X1 = 60f, Y1 = 114f;
        int n = 0;
        // 경계 — 남 갭 x28~34(튜토에서 올라옴) · 북 갭 x40~46(가로 간선) · 동 갭 y92~98(세로 지선) · 서벽 폐쇄
        n += GreyboxBuild.WallSeg(m, "PA_S_a", X0, Y0, 28f, Y0 + 2f);
        n += GreyboxBuild.WallSeg(m, "PA_S_b", 34f, Y0, X1, Y0 + 2f);
        n += GreyboxBuild.WallSeg(m, "PA_N_a", X0, Y1 - 2f, 40f, Y1);
        n += GreyboxBuild.WallSeg(m, "PA_N_b", 46f, Y1 - 2f, X1, Y1);
        n += GreyboxBuild.WallSeg(m, "PA_W",   X0, Y0, X0 + 2f, Y1);
        n += GreyboxBuild.WallSeg(m, "PA_E_a", X1 - 2f, Y0, X1, 92f);
        n += GreyboxBuild.WallSeg(m, "PA_E_b", X1 - 2f, 98f, X1, Y1);

        // 약국(앵커) — 방 x38~58 y80~98, 서문(스파인 향). 껍데기만 — 내부는 Int_Pharmacy 씬.
        n += GreyboxBuild.Building(m, "Pharmacy", 38f, 80f, 58f, 98f, 'W', 87f, "gb_door", "Pharmacy_Door");
        MarkBuilding(38f, 80f, 58f, 98f);
        n += Enter(m, "Pharmacy_Enter", 38.5f, 88f, "Int_Pharmacy", "default", 1f, 2f);   // 서문 **문간**(벽 x38~39, 갭 y87~89)
        n += ReturnSpawn(m, "from_pharmacy", 35.5f, 88f);                                  // 내부에서 나오면 문 앞
        // 메모는 **문 밖 스파인**에(건물 안에 두면 껍데기 원칙 위반 — 외부에서 보이면 안 된다).
        n += GreyboxBuild.Note(m, "Pharmacy_Note", 32f, 84f, "약국 카운터 메모",
            "처방 약은 약장(MedCabinet) 안. 카운터 밑 열쇠(key_pharmacy)로 연다.");

        // 빽빽한 점포 — **실개골목 2m**(맵에서 가장 좁음). 스파인 x28~34는 비워 둔다.
        n += Shops(m, "AW", X0 + 2f, Y0 + 2f, 28f, Y1 - 2f, AlleyTight, H(101, 7));   // 서측 열

        // 약국 위쪽은 절차 점포 대신 **골목 점포**(유니크) — 아케이드 최심부의 곁가지.
        n += UniqueShop(m, "AlleyShop", "골목 점포 ★", "Int_AlleyShop", "from_alleyshop",
                        40f, 100f, 52f, 111f, 'N', 45f, 0, "bandit_melee_1",
                        "잡템 1~2. 약국 곁가지 — 실개골목 끝.");

        n += GreyboxBuild.Note(m, "PD_Label", 31f, 110f, "약국·상가 심부 ★★",
            "튜토 北. 약장 미니퍼즐(key_pharmacy→약장→SQ-002) + 의료·생필품. 실개골목 2m = 최고 밀도.");
        return n;
    }

    /// <summary>
    /// ⑤ 식물원 돔(상세) — C2R3(x126~200 y176~256). 시그니처·최고보상·최고위험.
    ///   대형 유리 온실: 중앙 금고실(돔 코어, 문=key_dome_code 잠금 → 최고 보상) + 온실 화단(벤치) + 습지 침수(이동 제약).
    ///   진입 = 南 갭 x148~160(식물원 간선). 빽빽한 점포와 대비되는 '개방형 온실' 톤.
    /// </summary>
    static int BuildGreenhouseDome(GameObject m)
    {
        const float X0 = 66f, Y0 = 126f, X1 = 102f, Y1 = 168f;
        int n = 0;
        // 외곽 — 남 갭 x80~88 = 가로 간선에서 진입(온실 정문)
        n += GreyboxBuild.WallSeg(m, "GH_S_a", X0, Y0, 80f, Y0 + 2f);
        n += GreyboxBuild.WallSeg(m, "GH_S_b", 88f, Y0, X1, Y0 + 2f);
        n += GreyboxBuild.WallSeg(m, "GH_N",   X0, Y1 - 2f, X1, Y1);
        n += GreyboxBuild.WallSeg(m, "GH_W",   X0, Y0, X0 + 2f, Y1);
        n += GreyboxBuild.WallSeg(m, "GH_E",   X1 - 2f, Y0, X1, Y1);

        // 중앙 금고실(돔 코어) — 문 = 金庫(key_dome_code 잠금). 최고 보상. 내부는 Int_Dome 씬.
        n += GreyboxBuild.Building(m, "DomeCore", 76f, 140f, 96f, 158f, 'S', 84f, "gb_door", "Dome_Vault(key_dome_code)");
        MarkBuilding(76f, 140f, 96f, 158f);
        n += Enter(m, "Dome_Enter", 85f, 140.5f, "Int_Dome", "default", 2f, 1f);   // 금고문 **문간**(남벽 y140~141, 갭 x84~86)
        n += ReturnSpawn(m, "from_dome", 85f, 136f);

        // 온실 화단(벤치=선반) + 고가 루팅
        n += GreyboxBuild.Marker(m, "gb_shelf", "GH_Bed1", 70f, 132f);
        n += GreyboxBuild.Marker(m, "gb_shelf", "GH_Bed2", 74f, 136f);
        n += GreyboxBuild.Marker(m, "gb_shelf", "GH_Bed3", 98f, 134f);
        n += GreyboxBuild.Marker(m, "gb_shelf", "GH_Bed4", 71f, 163f);
        n += GreyboxBuild.Marker(m, "gb_crate", "GH_Crate1", 69f, 152f);
        n += GreyboxBuild.Marker(m, "gb_crate", "GH_Crate2", 99f, 163f);
        // 습지 침수(이동 제약) = 바리케이드 패치 — 돔 주위를 도는 동선을 만든다.
        n += GreyboxBuild.Barricade(m, "GH_Flood_W", 71f, 148f, 5f, 14f);
        n += GreyboxBuild.Barricade(m, "GH_Flood_E", 99f, 146f, 5f, 12f);
        n += GreyboxBuild.Note(m, "GH_Label", 70f, 129f, "식물원 돔 ★★★★★ (시그니처)",
            "중앙 금고 key_dome_code = 최고 보상. 온실 화단·습지 침수(이동 제약). 최고 위험.");
        return n;
    }

    /// <summary>작은 점포 블록: 폭·깊이 제각각(해시) + 가끔 빈 칸(공터). 문=골목 향.</summary>
    static int Shops(GameObject m, string p, float x0, float y0, float x1, float y1, float street, int seed)
    {
        int n = 0, i = 0;
        float y = y0;
        while (y1 - y >= 8f)
        {
            float d = DS[H(seed, i) % DS.Length];
            if (y + d > y1) d = y1 - y;
            if (d < 7f) break;
            float x = x0; int j = 0;
            while (x1 - x >= 8f)
            {
                float w = WS[H(seed * 31 + i, j) % WS.Length];
                if (x + w > x1) w = x1 - x;
                bool open = (H(seed + i * 13, j * 7 + 1) % 11) == 0;  // ~9% 빈 칸
                if (!open && w >= 7f && d >= 7f)
                {
                    char side = ((i + j) % 2 == 0) ? 'S' : 'N';
                    float doorX = x + Mathf.Max(1f, w * 0.5f - 1f);
                    n += GreyboxBuild.Building(m, $"{p}_{i}_{j}", x, y, x + w, y + d, side,
                                               doorX, "gb_door", $"{p}_{i}_{j}_D");
                    MarkBuilding(x, y, x + w, y + d);
                    // 2026-07-11: 절차 생성 점포도 **전부 들어갈 수 있게** — 공용 내부(Int_Generic)로 진입.
                    //   복귀는 고정 스폰이 아니라 '들어온 문 앞'(BuildingReturn) → 한 채를 돌려 써도 제자리로 나온다.
                    //   발판 위치는 EnterAtDoor가 Building()의 (side, doorAt) 규약대로 계산 → 건물에 정확히 붙는다.
                    n += EnterAtDoor(m, $"{p}_{i}_{j}_Enter", x, y, x + w, y + d, side, doorX);
                }
                x += w + street; j++;
            }
            y += d + street; i++;
        }
        return n;
    }

    /// <summary>큰 건물(랜드마크): **껍데기(외벽+문)만** — 내부는 별도 씬(전당포식 전환, 2026-07-11).
    /// 예전엔 내부 십자 칸막이를 그려 위에서 내부가 다 보였음 → 제거.</summary>
    static int Big(GameObject m, string p, float x0, float y0, float x1, float y1, char side,
                   string targetScene = null, string returnSpawn = null)
    {
        float bx0 = x0 + 2f, by0 = y0 + 2f, bx1 = x1 - 2f, by1 = y1 - 2f;
        if (bx1 - bx0 < 12f || by1 - by0 < 12f) return Shops(m, p, x0, y0, x1, y1, Alley, H((int)x0, (int)y0));
        int n = 0;
        float doorAt = (side == 'S' || side == 'N') ? (bx0 + bx1) * 0.5f - 1f : (by0 + by1) * 0.5f - 1f;
        n += GreyboxBuild.Building(m, p, bx0, by0, bx1, by1, side, doorAt, "gb_door", $"{p}_D");
        // 2026-07-11: 내부 십자 칸막이 제거 — 건물 = 껍데기(외벽+문)뿐. 내부는 별도 씬(전당포식 전환).
        // 2026-07-11: 대형 건물도 진입 가능("입구 발판을 건물에 붙여줘 전부다"). 문간에 발판을 채운다.
        //   targetScene을 주면 **전용 내부 씬**, 안 주면 공용 내부(Int_Generic).
        if (string.IsNullOrEmpty(targetScene))
            n += EnterAtDoor(m, $"{p}_Enter", bx0, by0, bx1, by1, side, doorAt);
        else
        {
            n += EnterAtDoorTo(m, $"{p}_Enter", bx0, by0, bx1, by1, side, doorAt, targetScene);
            if (!string.IsNullOrEmpty(returnSpawn))
            {
                DoorPad(bx0, by0, bx1, by1, side, doorAt, out float ex, out float ey, out _, out _);
                // 복귀 자리 = 문간에서 바깥으로 2.5m(발판과 안 겹치게).
                float rx = ex + (side == 'W' ? -2.5f : side == 'E' ? 2.5f : 0f);
                float ry = ey + (side == 'S' ? -2.5f : side == 'N' ? 2.5f : 0f);
                n += ReturnSpawn(m, returnSpawn, rx, ry);
            }
        }
        MarkBuilding(bx0, by0, bx1, by1);
        return n;
    }

    /// <summary>무너진 상가(랜드마크) — 껍데기 + **전용 내부 씬** `Int_CollapsedMall`. 문은 남쪽(가로 간선 향).</summary>
    static int BuildCollapsedMall(GameObject m, float x0, float y0, float x1, float y1)
    {
        int n = Big(m, "CollapsedMall", x0, y0, x1, y1, 'S', "Int_CollapsedMall", "from_collapsed");
        n += GreyboxBuild.Note(m, "CM_Label", (x0 + x1) * 0.5f, y0 - 4f, "무너진 상가 ★★★",
            "SQ-001 갇힌 생존자. 잔해 미로 최심부. 문 = 가로 간선(南).");
        return n;
    }

    /// <summary>건물 문에 진입 트리거(BuildingEntrance) — 내부 씬이 있는 건물만.
    /// 밟으면 내부 씬으로 전환(페이드+캐릭터 유지). 복귀 스폰은 Zone1의 from_&lt;건물&gt;.</summary>
    static int Enter(GameObject m, string name, float x, float y, string targetScene, string spawnId = "default",
                     float tw = 2f, float th = 1f)
    {
        if (GreyboxBuild.Marker(m, "gb_enter", name, x, y) == 0) return 0;
        KeepOut(x, y);   // 건물 입구 앞도 비워 둔다(문이 잔해로 막히면 못 들어간다)
        var t = FindChild(m.transform, name);
        if (t == null) return 0;
        var go = t.gameObject;

        var io = go.GetComponentInChildren<InteractableObject>();   // gb_exit의 E키 ExitPoint는 중복 → 제거
        if (io != null) Object.DestroyImmediate(io);

        var box = go.GetComponent<BoxCollider2D>();
        if (box == null) box = go.AddComponent<BoxCollider2D>();   // ??는 Unity 가짜 null을 통과시켜 못 씀
        box.isTrigger = true;
        box.size = new Vector2(tw, th);

        var be = go.GetComponent<BuildingEntrance>();
        if (be == null) be = go.AddComponent<BuildingEntrance>();
        // 크기를 함께 넘겨야 한다 — 안 넘기면 Awake가 기본 1.3×1.0으로 덮어써서
        //   **문 갭(2m)보다 좁은 발판**이 되고, 옆으로 비껴 들어가 빈 껍데기 안에 갇힌다.
        be.Configure(targetScene, spawnId, false, new Vector2(tw, th));
        return 1;
    }

    /// <summary>입구 발판을 **문간(문 갭 그 자리)** 에 정확히 채운다. `GreyboxBuild.Building`의
    /// (side, doorAt) 규약대로 계산 — 벽 두께 1m × 문 갭 2m를 발판이 꽉 메운다.
    ///
    /// 2026-07-11 수정: 예전엔 문 **바깥 0.9m**에 1.3m짜리 발판을 뒀다. 문 갭은 2m라
    /// **발판 옆으로 비껴 들어가면 빈 껍데기 안에 갇혔다**(사용자 보고: "벽 안에 입구 있으면 어떻게 나가니").
    /// 문간을 꽉 채우면 ① 발판을 안 밟고 통과하는 게 불가능하고 ② 건물 외벽을 따라 지나가도 안 밟힌다.
    /// </summary>
    static int EnterAtDoor(GameObject m, string name, float x0, float y0, float x1, float y1,
                           char side, float doorAt)
    {
        DoorPad(x0, y0, x1, y1, side, doorAt, out float ex, out float ey, out float tw, out float th);
        return EnterGeneric(m, name, ex, ey, tw, th);
    }

    /// <summary>EnterAtDoor의 '전용 내부 씬' 판 — 공용(Int_Generic) 대신 지정 씬으로 들어간다.</summary>
    static int EnterAtDoorTo(GameObject m, string name, float x0, float y0, float x1, float y1,
                             char side, float doorAt, string targetScene, string spawnId = "default")
    {
        DoorPad(x0, y0, x1, y1, side, doorAt, out float ex, out float ey, out float tw, out float th);
        return Enter(m, name, ex, ey, targetScene, spawnId, tw, th);
    }

    /// <summary>`GreyboxBuild.Building`의 (side, doorAt) 규약 → 문간 발판의 중심·크기.</summary>
    static void DoorPad(float x0, float y0, float x1, float y1, char side, float doorAt,
                        out float ex, out float ey, out float tw, out float th)
    {
        const float gap = 2f, t = 1f;   // Building(): 문 갭 2m, 벽 두께 1m
        switch (side)
        {
            case 'S': ex = doorAt + gap * 0.5f; ey = y0 + t * 0.5f;       tw = gap; th = t;   break;
            case 'N': ex = doorAt + gap * 0.5f; ey = y1 - t * 0.5f;       tw = gap; th = t;   break;
            case 'W': ex = x0 + t * 0.5f;       ey = doorAt + gap * 0.5f; tw = t;   th = gap; break;
            default:  ex = x1 - t * 0.5f;       ey = doorAt + gap * 0.5f; tw = t;   th = gap; break;   // 'E'
        }
    }

    /// <summary>공용 내부(Int_Generic)로 들어가는 진입 트리거. 복귀는 `__back__`(들어온 문 앞).
    /// 전용 내부가 만들어진 건물은 Enter()로 개별 지정하고, 나머지 절차 생성 건물이 이걸 쓴다.
    ///
    /// **진입 가능 비율은 `GameTuning.buildingEnterRatio` 노브**(1=전부, 0.5=절반).
    /// "건물을 더 열지"는 QA 플레이 결과로 판단 — 값만 바꾸고 이 빌더를 다시 돌리면 반영된다.
    /// 선택은 이름 해시 기반이라 **결정론적**(같은 값이면 같은 건물이 열림).</summary>
    static int EnterGeneric(GameObject m, string name, float x, float y, float tw = 2f, float th = 1f)
    {
        float ratio = GameTuning.Instance != null ? GameTuning.Instance.buildingEnterRatio : 1f;
        if (ratio < 1f)
        {
            if (ratio <= 0f) return 0;
            // 이름 해시 → 0~1 균등. ratio 미만인 건물만 연다.
            uint h = 2166136261u;
            for (int i = 0; i < name.Length; i++) { h ^= name[i]; h *= 16777619u; }
            if ((h % 1000u) / 1000f >= ratio) return 0;
        }
        // 진입 스폰은 내부 씬의 "default". `__back__`은 **나올 때** 쓰는 값이라(내부 씬 출구가 보유)
        //   여기 넣으면 매번 "스폰 __back__ 미발견" 경고만 찍힌다.
        return Enter(m, name, x, y, "Int_Generic", "default", tw, th);
    }

    /// <summary>내부에서 돌아왔을 때 서는 자리(from_&lt;건물&gt;). 건물 문 앞.</summary>
    static int ReturnSpawn(GameObject m, string pointId, float x, float y)
    {
        string name = $"Ret_{pointId}";
        if (GreyboxBuild.Marker(m, "gb_spawn", name, x, y) == 0) return 0;
        var t = FindChild(m.transform, name);
        var sp = t != null ? t.GetComponentInChildren<SpawnPoint>() : null;
        if (sp != null)
        {
            var so = new SerializedObject(sp);
            var p = so.FindProperty("pointId");
            if (p != null) { p.stringValue = pointId; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
        return 1;
    }

    /// <summary>광장/주차장: 거의 빈 공간 + 모서리 작은 구조물 2 + 상자 몇(변화용).</summary>
    static int Plaza(GameObject m, string p, float x0, float y0, float x1, float y1)
    {
        int n = 0;
        n += GreyboxBuild.Building(m, $"{p}_k1", x0 + 3f, y0 + 3f, x0 + 17f, y0 + 14f, 'S', x0 + 9f, "gb_door", $"{p}_k1D");
        n += GreyboxBuild.Building(m, $"{p}_k2", x1 - 18f, y1 - 15f, x1 - 3f, y1 - 3f, 'N', x1 - 13f, "gb_door", $"{p}_k2D");
        MarkBuilding(x0 + 3f, y0 + 3f, x0 + 17f, y0 + 14f);
        MarkBuilding(x1 - 18f, y1 - 15f, x1 - 3f, y1 - 3f);
        // 광장 키오스크 2채도 진입 가능.
        n += EnterAtDoor(m, $"{p}_k1_Enter", x0 + 3f, y0 + 3f, x0 + 17f, y0 + 14f, (char)83, x0 + 9f);
        n += EnterAtDoor(m, $"{p}_k2_Enter", x1 - 18f, y1 - 15f, x1 - 3f, y1 - 3f, (char)78, x1 - 13f);
        // 상자는 **개활지 바닥에만** — 키오스크 껍데기 안에 들어가면 "건물 안에 상자" 불일치가 재발한다.
        //   (블록이 절반으로 작아지면서 예전 오프셋이 키오스크 안으로 들어갔다.)
        n += PlazaCrate(m, $"{p}_c1", (x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
        n += PlazaCrate(m, $"{p}_c2", x0 + 8f, y1 - 8f);
        return n;
    }

    /// <summary>개활지 상자 — 건물 자리면 배치하지 않는다(건물 = 껍데기 원칙).</summary>
    static int PlazaCrate(GameObject m, string name, float x, float y)
    {
        if (IsIndoors(x, y)) { Debug.LogWarning($"[Zone1] {name} 위치가 건물 안 → 생략"); return 0; }
        return GreyboxBuild.Marker(m, "gb_crate", name, x, y);
    }

    // ── 도로 장애물 (2026-07-11) ─────────────────────────────────────────
    //   사용자: "도로의 장애물 개념". 12m 간선이 그냥 뻥 뚫린 복도면 넓기만 하고 심심하다.
    //   → 버려진 차·전복 트럭·콘크리트 잔해를 **좌우 번갈아** 놓아 통행선을 지그재그로 만든다.
    //     · 시야가 끊긴다(모퉁이마다 조우 가능성)
    //     · 엄폐가 생긴다(원거리 적 상대 가능)
    //     · 넓은 길 = 빠른 길이지만 **직선으로는 못 달린다**
    //   남는 통행 폭은 GameTuning.roadMinPassWidth 아래로 내려가지 않는다(끼임 방지).

    /// <summary>도로 한 줄에 장애물을 깐다. vertical=true면 세로 도로(폭=x, 진행=y).
    /// spacing 간격마다 좌/우 번갈아 도로 폭의 blockFrac만큼 막는다.</summary>
    /// <summary>장애물 금지 지점 — 스폰·탈출·건물 입구. 여기 잔해가 깔리면 스폰 즉시 끼거나 문이 막힌다.</summary>
    static readonly System.Collections.Generic.List<Vector2> _keepOut = new System.Collections.Generic.List<Vector2>();
    static void KeepOut(float x, float y) => _keepOut.Add(new Vector2(x, y));
    static bool NearKeepOut(float x, float y, float r)
    {
        for (int i = 0; i < _keepOut.Count; i++)
            if ((_keepOut[i] - new Vector2(x, y)).sqrMagnitude < r * r) return true;
        return false;
    }

    static int ObstacleRun(GameObject m, string p, bool vertical,
                           float a0, float a1, float b0, float b1,
                           float spacing, float blockFrac, int seed)
    {
        float density = GameTuning.Instance != null ? GameTuning.Instance.roadObstacleDensity : 1f;
        if (density <= 0.01f) return 0;
        float minPass = GameTuning.Instance != null ? GameTuning.Instance.roadMinPassWidth : 3f;

        float width = a1 - a0;                                   // 도로 폭
        float maxBlock = Mathf.Max(0f, width - minPass);          // 최소 통행 폭은 반드시 남긴다
        float block = Mathf.Min(width * blockFrac, maxBlock);
        if (block < 1f) return 0;

        float step = Mathf.Max(6f, spacing / Mathf.Max(0.2f, density));
        int n = 0, i = 0;
        for (float t = b0 + step * 0.5f; t < b1; t += step, i++)
        {
            uint h = (uint)H(seed, i);
            bool left = (h & 1u) == 0;                            // 좌우 번갈이 + 해시로 흔들기
            float len  = 3.5f + (h >> 1) % 4 * 1.2f;              // 차 1대 ~ 트럭
            float thick = Mathf.Max(1.6f, block * (0.75f + ((h >> 4) % 3) * 0.12f));
            thick = Mathf.Min(thick, maxBlock);
            if (len > (b1 - t)) len = b1 - t;
            if (len < 2f) break;

            float ca = left ? a0 + thick * 0.5f : a1 - thick * 0.5f;   // 도로 한쪽에 붙임
            float cb = t + len * 0.5f;
            // 스폰·탈출·건물 입구 근처는 건너뛴다 — 거기 잔해가 깔리면 스폰 즉시 끼거나 문이 막힌다.
            float cx0 = vertical ? ca : cb, cy0 = vertical ? cb : ca;
            if (NearKeepOut(cx0, cy0, len * 0.5f + 3.5f)) continue;
            string name = $"{p}_{i}";
            // 세로 도로면 장애물의 '길이'가 y축, 두께가 x축.
            n += vertical ? GreyboxBuild.Barricade(m, name, ca, cb, thick, len)
                          : GreyboxBuild.Barricade(m, name, cb, ca, len, thick);
            // 잔해 자리를 등록 → 루트 앵커가 잔해 속에 박히지 않고, 오히려 **잔해에 붙어** 생긴다.
            float hw = (vertical ? thick : len) * 0.5f, hh = (vertical ? len : thick) * 0.5f;
            MarkBuilding(cx0 - hw, cy0 - hh, cx0 + hw, cy0 + hh);

            // 3대 중 1대는 **트렁크가 열린 차** — 도로 파밍을 바닥에 뿌리는 대신 잔해에 붙인다.
            //   (사용자: "도로에 부서진 자동차 트렁크나 상자 같은 거 … 유기적인 방향으로")
            if (i % 3 == 1) n += Trunk(m, name, Mathf.Max(len, thick));
        }
        return n;
    }

    /// <summary>C1R1 = **유니크 상점가**. 4채가 중앙 십자 골목(폭 4m)을 마주 본다.
    ///   보석상 / 컴퓨터가게 (남열) · 철물점 / 세탁소 (북열).
    /// 골목은 남쪽으로 열려 가로 지선과 이어진다 — 안쪽으로 들어갈수록 시야가 좁아지는 구조.</summary>
    static int BuildUniqueRow(GameObject m)
    {
        int n = 0;
        // 문은 전부 중앙 세로 골목(x83~87)을 향한다 → 한 골목에서 4채를 다 볼 수 있다.
        n += UniqueShop(m, "Jewelry", "보석상 ★★★★", "Int_Jewelry", "from_jewelry",
                        68f, 82f, 83f, 95f, 'E', 87f, 2, "bandit_melee_1",
                        "금고는 **비밀번호**. 번호는 다른 데서 알아내야 한다(경찰 압수품 대장). 최고가 루트.");
        n += UniqueShop(m, "Electronics", "컴퓨터가게 ★★", "Int_Electronics", "from_electronics",
                        87f, 82f, 100f, 95f, 'W', 87f, 2, "bandit_melee_1",
                        "배터리·전선·전자부품. 라디오/발전기 업그레이드 재료.");
        n += UniqueShop(m, "Hardware", "철물점 ★★", "Int_Hardware", "from_hardware",
                        68f, 99f, 83f, 112f, 'E', 104f, 1, "bandit_melee_1",
                        "공구·부품·못. 제작·수리 재료.");
        n += UniqueShop(m, "Laundry", "세탁소 ★", "Int_Laundry", "from_laundry",
                        87f, 99f, 100f, 112f, 'W', 104f, 0, "bandit_melee_1",
                        "천·의류. 방한·붕대 재료.");
        return n;
    }

    /// <summary>유니크 상점 1채 — 껍데기 + 문간 진입(전용 내부 씬) + 복귀 스폰 + 라벨 + **건물 앞 적 존**.
    ///
    /// (2026-07-11 사용자: "랜드마크 아니어도 뭔가 유니크한 건물들 — 보석상·철물점·컴퓨터가게 등등
    ///  파밍의 재미를 올리고, 거기서 밴딧 나올 확률도 높여서 도전하면 더 좋은 물품이라는 동기부여")
    ///
    /// 보상은 **내부 씬 쪽**에서 준다(`InteriorBuild.Controller`의 budgetMult·lootRegion) —
    /// 여기서는 위험(적 수)과 접근성만 다룬다. 위험/보상을 한 곳에 몰아넣으면 조절이 안 된다.</summary>
    static int UniqueShop(GameObject m, string id, string label, string scene, string returnSpawn,
                          float x0, float y0, float x1, float y1, char side, float doorAt,
                          int enemies, string enemyKey = "bandit_melee_1", string note = null)
    {
        int n = 0;
        n += GreyboxBuild.Building(m, id, x0, y0, x1, y1, side, doorAt, "gb_door", $"{id}_Door");
        MarkBuilding(x0, y0, x1, y1);
        n += EnterAtDoorTo(m, $"{id}_Enter", x0, y0, x1, y1, side, doorAt, scene);

        DoorPad(x0, y0, x1, y1, side, doorAt, out float ex, out float ey, out _, out _);
        float rx = ex + (side == 'W' ? -2.5f : side == 'E' ? 2.5f : 0f);
        float ry = ey + (side == 'S' ? -2.5f : side == 'N' ? 2.5f : 0f);
        n += ReturnSpawn(m, returnSpawn, rx, ry);
        n += GreyboxBuild.Note(m, $"{id}_Label", (x0 + x1) * 0.5f, y1 + 1.5f, label,
                               note ?? $"{label} — 유니크 점포. 내부 파밍 전용 씬.");

        // 건물 **앞**(문 바깥)에 적 존 — 안에 두면 껍데기에서 스폰돼 걸어 나온다(SnapOutdoors가 한 번 더 보정).
        if (enemies > 0)
            n += EnemyZone(m, $"EZ_{id}", rx, ry, 7f, 7f, enemyKey, enemies);
        return n;
    }

    /// <summary>도로 잔해에 **트렁크 루팅**을 붙인다 — 부서진 차를 뒤지는 감각.
    /// 별도 상자를 옆에 놓지 않고 잔해 오브젝트 자체를 컨테이너로 만든다(도로가 어질러 보이지 않게).
    /// 예산제(MapSpawnController)가 채우므로 `ItemSpawnPoint(Container)`를 링크해 둔다.</summary>
    static int Trunk(GameObject m, string obstacleName, float size)
    {
        var t = FindChild(m.transform, obstacleName);
        if (t == null) return 0;
        var go = t.gameObject;

        var lc = go.GetComponent<LootContainer>();
        if (lc == null) lc = go.AddComponent<LootContainer>();   // ??는 Unity 가짜 null을 통과시켜 못 씀
        lc.Setup("자동차 트렁크", 3, 2);

        var io = go.GetComponent<InteractableObject>();
        if (io == null) io = go.AddComponent<InteractableObject>();
        io.Configure(InteractableObject.InteractType.Container, "트렁크 뒤지기", size * 0.5f + 1.6f);

        var sp = go.GetComponent<ItemSpawnPoint>();
        if (sp == null) sp = go.AddComponent<ItemSpawnPoint>();
        SetSpawnType(sp, 1);   // Container
        var so = new SerializedObject(sp);
        var lk = so.FindProperty("linkedContainer");
        if (lk != null) { lk.objectReferenceValue = lc; so.ApplyModifiedPropertiesWithoutUndo(); }
        return 1;
    }

    /// <summary>막힌 통로(BlockedPassage) — 도로를 **가로질러** 완전히 막는 잔해.
    /// mode에 따라 영구 차단 / 치울 수 있음 / 열쇠 / 밤에만.</summary>
    static int Blocker(GameObject m, string name, float cx, float cy, float w, float h,
                       BlockedPassage.Mode mode, string label, string itemId = null)
    {
        if (GreyboxBuild.Barricade(m, name, cx, cy, w, h) == 0) return 0;
        var t = FindChild(m.transform, name);
        if (t == null) return 0;
        var go = t.gameObject;

        // 솔리드 콜라이더(통행 차단). gb_barricade에 이미 있으면 크기만 맞춘다.
        var box = go.GetComponent<BoxCollider2D>();
        if (box == null) box = go.AddComponent<BoxCollider2D>();   // ??는 Unity 가짜 null을 통과시켜 못 씀
        box.isTrigger = false;
        box.size = Vector2.one;   // 부모 스케일(w,h)이 곱해진다

        var io = go.GetComponent<InteractableObject>();
        if (io == null) io = go.AddComponent<InteractableObject>();
        // 상호작용 반경은 **크기에 비례**해야 한다 — 12m 잔해에 고정 2.2m를 주면
        //   플레이어가 가장자리에 서 있을 때 중심까지 6m라 E가 아예 안 먹는다.
        io.Configure(InteractableObject.InteractType.Passage, label, Mathf.Max(w, h) * 0.5f + 1.8f);

        var bp = go.GetComponent<BlockedPassage>();
        if (bp == null) bp = go.AddComponent<BlockedPassage>();
        bp.Configure(mode, label, itemId);

        MarkBuilding(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
        return 1;
    }

    /// <summary>결정적 해시(인덱스 → 의사난수). Math.random 없이 재현 가능한 변동.</summary>
    static int H(int a, int b)
    {
        unchecked
        {
            uint h = (uint)(a * 374761393 + b * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (int)((h ^ (h >> 16)) & 0x7fffffff);
        }
    }
}
#endif
