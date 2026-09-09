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
///   │ 아치 통로 3.6m — 블록 띠를 뚫는 유일한 안뜰 진입로(블록당 1~2개).           │
///   └────────────────────────────────────────────────────────────────┘
///
/// ★ 2026-07-28 v3 — **생성 단위 재정의: 필지(lot) → 둘레형 블록(perimeter block)**.
///   구: 블록을 필지 격자로 잘게 쪼개 필지마다 네모를 세우고 그 사이를 골목 격자로 채웠다.
///       그래서 무엇을 조절해도 **네모의 크기·간격만** 바뀌었다 — 올리면 다닥다닥, 내리면 텅 빔.
///   신: 건물이 블록 **테두리를 두께 8~12m 띠**로 두르고, 안쪽은 **안뜰**이 된다.
///       길은 블록 **사이**에만 있고, 띠를 뚫는 건 **아치 1~2개**뿐. → `PerimeterBlock`
///       (docs/level-scrapmarket.md "★생성 단위 재정의")
///
///   블록 = 3열 × 3행 = 9개. 블록 하나가 곧 **정체성 단위**(이름·안뜰 성격·위험도).
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
    // (구 Alley 3m / AlleyTight 2m 폐기 — 2026-07-28. 블록 **안**에는 길을 두지 않는다.
    //  블록 내부 통행은 아치(ArchGap 3.6m) → 안뜰뿐. 길은 블록 사이에만 있다.)

    /// <summary>건물 한 채의 최대 한 변(m) — **아트 리소스 제약**(2026-07-11 사용자).
    /// 이보다 크면 랜드마크라도 단지로 쪼갠다. 랜드마크의 정체성은 크기가 아니라
    /// 라벨·전용 내부 씬·주변 위험도가 만든다.</summary>
    public const float MaxSpan = 13f;

    /// <summary>붕괴 필지 비율 — "계획도시"가 아니라 **정부가 무너진 뒤의 도시**로 보이게 하는 값.
    /// 이 비율만큼의 필지가 온전한 사각형 대신 **무너진 잔해 덩어리 몇 개**가 된다.</summary>
    const float RuinRatio = 0.22f;

    // 블록 격자 — 사이 간격이 곧 도로. C0|지선|C1|간선|C2, R0|지선|R1|간선|R2. 둘레는 순환도로.
    static readonly float[] CX0 = {  16f,  66f, 114f };
    static readonly float[] CX1 = {  60f, 102f, 160f };
    static readonly float[] RY0 = {  16f,  78f, 126f };
    static readonly float[] RY1 = {  72f, 114f, 168f };

    // (구 점포 크기 풀 WS/DS는 폐기 — 크기를 뽑아 쓰면 블록 끝에 자투리가 남아 빈 땅이 됐다.
    //  지금은 Shops가 블록을 남김없이 분할하고 필지 크기를 가중치로 흔든다.)

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
        // 튜토의 **출입구 두 곳**도 비워 둘 곳으로 등록 — 안 하면 지선 갓길 차량이 갭 앞을 덮는다.
        MarkGap(32f, 67f, 32f, 78f);        // 北 갭(월드 x30~34) → 가로 지선 → 약국 아케이드 南 갭
        MarkGap(55f, 45.5f, 67f, 45.5f);    // 東 통로(월드 y43~48) → 세로 지선

        // ── 블록 9개, 성격 전부 다르게 ── (2026-07-28: 전부 **둘레형 블록**)
        //   C0R0=튜토 / C0R1=약국 아케이드 / C0R2=공원 안뜰(분식집)
        //   C1R0=폐아파트 / C1R1=상점가 안뜰(유니크 4채) / C1R2=식물원 돔
        //   C2R0=유리타워 / C2R1=주차 안뜰(경찰서) / C2R2=무너진 상가
        //   ※ 랜드마크·유니크 건물은 **띠의 한 조각**으로 편입한다 — 블록 밖에 따로 세우지 않는다.
        //     (예전엔 경찰서·분식집을 블록 루프보다 먼저 세워 광장이 그 자리를 피하게 했는데,
        //      그러면 건물이 블록과 따로 놀고 서로 겹칠 위험만 계속 남았다.)
        for (int r = 0; r < RY0.Length; r++)
        for (int c = 0; c < CX0.Length; c++)
        {
            if (c == 0 && r == 0) continue;  // 튜토 자리(위에서 Place로 배치됨)
            float ax0 = CX0[c], ay0 = RY0[r], ax1 = CX1[c], ay1 = RY1[r];
            string p = $"B{c}{r}";
            int seed = H(c + 1, r + 1);

            if      (c == 0 && r == 1) n += BuildPharmacyArcade(map);                       // 약국·상가 심부(상세)
            else if (c == 0 && r == 2) n += BuildParkBlock(map, ax0, ay0, ax1, ay1);        // 공원 안뜰 + 분식집
            else if (c == 1 && r == 0) n += BuildAptBlock(map, ax0, ay0, ax1, ay1);         // 폐아파트(띠 東=세로 간선)
            else if (c == 1 && r == 2) n += BuildGreenhouseDome(map);                       // 식물원 돔(상세)
            else if (c == 2 && r == 0) n += BuildTowerBlock(map, ax0, ay0, ax1, ay1);       // 유리타워(띠 西=세로 간선)
            else if (c == 2 && r == 1) n += BuildLotBlock(map, ax0, ay0, ax1, ay1);         // 주차 안뜰 + 경찰서
            else if (c == 2 && r == 2) n += BuildCollapsedMall(map, ax0, ay0, ax1, ay1);    // 무너진 상가(전용 내부)
            else if (c == 1 && r == 1) n += BuildUniqueRow(map);                            // 상점가 안뜰(유니크 4채)
            else                       n += PerimeterBlock(map, p, ax0, ay0, ax1, ay1, seed);
        }


        // ── 열쇠 사슬 마커 — 해당 건물이 면한 간선 위에 ──
        //   (라벨은 랜드마크 문 앞에서 `BandSpot.label`이 낸다 — 여기 또 적으면 같은 쪽지가 둘이 된다.)
        n += GreyboxBuild.Marker(map, "gb_crate", "key_apt_admin", 108f, 36f);
        n += GreyboxBuild.Marker(map, "gb_door",  "Pent_Gate(key_apt_admin)", 108f, 48f);
        n += GreyboxBuild.Marker(map, "gb_crate", "key_tower_card", 108f, 60f);
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
        n += EnemyZone(map, "EZ_Dome_T", 92f, 157f, 8f, 8f, "bandit_tank", 1);

        // ④ 유리 R&D 타워(C2R0) — 강함·고급 루트(최심부)
        n += Scatter(map, "SZ_Tower", 116f, 18f, 158f, 70f, 6, 6, 44);
        n += EnemyZone(map, "EZ_Tower", 140f, 40f, 16f, 14f, "bandit_melee_1", 2);
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
        //   차량이 실제 비율(폭 2m대)로 얇아진 만큼 **간격을 좁혀** 도로가 비어 보이지 않게 한다.
        n += ObstacleRun(map, "OB_ArtV", true,  102f, 114f,  12f, 172f, 10f, 0.55f, 201);  // 세로 간선
        n += ObstacleRun(map, "OB_ArtH", false, 114f, 126f,  12f, 164f, 10f, 0.55f, 202);  // 가로 간선
        n += ObstacleRun(map, "OB_ColV", true,   60f,  66f,  12f, 172f, 15f, 0.45f, 203);  // 세로 지선
        n += ObstacleRun(map, "OB_ColH", false,  72f,  78f,  12f, 164f, 15f, 0.45f, 204);  // 가로 지선
        n += ObstacleRun(map, "OB_RingS", false, 10f,  16f,  20f, 156f, 18f, 0.42f, 205);  // 순환 남
        n += ObstacleRun(map, "OB_RingN", false,168f, 174f,  20f, 156f, 18f, 0.42f, 206);  // 순환 북
        n += ObstacleRun(map, "OB_RingW", true,   10f, 16f,  20f, 164f, 18f, 0.42f, 207);  // 순환 서
        n += ObstacleRun(map, "OB_RingE", true,  160f,166f,  20f, 164f, 18f, 0.42f, 208);  // 순환 동
        // 간선 중앙분리대(화단·가드레일) — 12m 도로에 축이 생겨 '큰길'로 읽히고, 넘나들 때 동선이 꺾인다.
        n += Median(map, "MD_V", true,  108f,  20f, 166f, 9f);
        n += Median(map, "MD_H", false, 120f,  20f, 158f, 9f);

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

        // ── 길목(교차로) — 도로가 만나는 자리마다 작은 무리 ──
        //   (2026-07-29 사용자: "길가가 좀 비지 않게 밀도 조금만 더 올려주고, 길목마다")
        //   교차로는 길을 외우는 기준점이다. 아무것도 없으면 사방이 똑같은 길이라 방향 감각이 안 생긴다.
        n += Junctions(map, 4141);

        // ── 거리 프랍 (마지막 — 건물·차량·길목이 다 등록된 뒤라야 정면에 붙는다) ──
        //   길에 표정을 주고(길 외우기), 작은 엄폐를 깔고, 일부는 뒤질 수 있게 한다.
        //   2026-07-29: 300 → 460으로 올려 길가가 비어 보이지 않게. 뒤질 수 있는 비율은
        //   3개당 1 → 5개당 1로 낮춰 **루팅 앵커 총량은 그대로**(예산이 묽어지지 않게).
        n += StreetProps(map, 460, 5, 4242);

        // ── ★ 적 무리 상한 (맨 마지막 — 존이 다 놓인 뒤라야 겹침을 볼 수 있다) ──
        TrimEnemyPacks(map);

        GreyboxBuild.EndScene(scene, ScenePath, n, "지역1 Zone1(160×168 · 도로 위계 4단계 · 장애물/프랍)");
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
        if (go == null) return 1;

        // 3D 그레이박스의 gb_exit는 **상자만** 세운다(기능은 호출부 책임 — Greybox3D 주석).
        //   이걸 빠뜨려서 Exit_Fixed·PX_* 4곳이 상호작용 없는 장식으로 남았고,
        //   지역1의 실제 탈출구는 맨홀 하나뿐이었다(고정1+랜덤2 설계가 통째로 죽어 있었다).
        var io = go.GetComponentInChildren<InteractableObject>();
        if (io == null && GreyboxBuild.Use3D)
        {
            io = go.gameObject.AddComponent<InteractableObject>();
            io.Configure(InteractableObject.InteractType.ExitPoint, "탈출하기", 2.0f);
        }
        if (io != null)
        {
            var so = new SerializedObject(io);
            var ts = so.FindProperty("targetScene");   if (ts != null) ts.stringValue = "Safehouse";
            // 귀환 스폰은 안전구역 1곳으로 통합됨 — 맨홀과 같은 raid_return.
            var sp = so.FindProperty("spawnPointId");  if (sp != null) sp.stringValue = "raid_return";
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
            float nx, ny;                                          // 벽에서 바깥으로 나가는 방향
            switch (rnd.Next(4))
            {
                case 0:  x = Mathf.Lerp(r.xMin, r.xMax, (float)rnd.NextDouble()); y = r.yMin - off; nx = 0f; ny = -1f; break;  // 남면
                case 1:  x = Mathf.Lerp(r.xMin, r.xMax, (float)rnd.NextDouble()); y = r.yMax + off; nx = 0f; ny =  1f; break;  // 북면
                case 2:  x = r.xMin - off; y = Mathf.Lerp(r.yMin, r.yMax, (float)rnd.NextDouble()); nx = -1f; ny = 0f; break;  // 서면
                default: x = r.xMax + off; y = Mathf.Lerp(r.yMin, r.yMax, (float)rnd.NextDouble()); nx =  1f; ny = 0f; break;  // 동면
            }
            if (x < x0 || x > x1 || y < y0 || y > y1) continue;   // 이 구역 밖으로 튀면 버림
            if (IsIndoors(x, y)) continue;                        // 옆 건물/잔해에 파묻히면 버림
            // 2026-07-28: 벽 **바로 앞**이 비어 있어도 그게 사각형 둘 사이의 실틈(0.9m)이면
            //   사람이 못 들어가 **영영 못 줍는 루트**가 된다(노점 골목에서 실제로 발생).
            //   한 발 더 바깥이 막혀 있으면 그건 틈이지 길이 아니다.
            if (IsIndoors(x + nx * 1.1f, y + ny * 1.1f)) continue;
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
            var go = GreyboxBuild.Point(map, $"{prefix}_G{i}", x, y);
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

        var go = GreyboxBuild.Point(map, name, cx, cy);
        go.AddComponent<SpawnZone>().Setup(GreyboxBuild.PlanSize(w, h), count, unitKey);
        return 1;
    }

    /// <summary>겹쳐 놓인 적 존을 잘라 **한 자리에 몰리는 마릿수**에 상한을 건다.
    ///
    /// 구역 존(손배치, 난이도 곡선)과 건물 안뜰 존(절차 생성)이 서로를 모르고 놓이는 바람에
    /// 같은 자리에 4~6마리가 쌓였다(실측: 폐아파트 5 · 유니크열 5(중장 포함) · 무너진상가 4).
    /// 시야 7m·추적 11m라 그 안에 들어서면 **전원이 동시에** 달려든다 — 곡선이 아니라 사고다.
    /// 안뜰의 '일반+중장' 짝(가중치 1+2=3)은 의도된 조합이라 상한 4 안에 그대로 들어간다.
    ///
    /// 나중에 놓인 것(=구역 존)을 먼저 살린다 — 블록·안뜰이 앞서 만들어지므로, 역순 처리가
    /// 곧 "손으로 배치한 곡선 우선"이 된다. 수치는 GameTuning(enemyPackRadius/enemyPackMaxWeight).</summary>
    static void TrimEnemyPacks(GameObject map)
    {
        var gt = GameTuning.Instance;
        float radius = gt != null ? gt.enemyPackRadius : 12f;
        int maxWeight = gt != null ? gt.enemyPackMaxWeight : 4;
        if (radius <= 0f || maxWeight <= 0) return;

        var zones = new System.Collections.Generic.List<SpawnZone>(map.GetComponentsInChildren<SpawnZone>(true));
        zones.Reverse();   // 늦게 놓인 것부터 = 구역 존 우선

        int Weight(SpawnZone z) => z.UnitKey == "bandit_tank" ? 2 : 1;
        var kept = new System.Collections.Generic.List<SpawnZone>();
        int trimmed = 0, dropped = 0;

        foreach (var z in zones)
        {
            int used = 0;
            foreach (var k in kept)
                if (Vector3.Distance(k.transform.position, z.transform.position) <= radius)
                    used += k.EnemyCount * Weight(k);

            int room = maxWeight - used;
            int allow = room / Weight(z);                 // 중장은 자리를 2 차지한다
            if (allow >= z.EnemyCount) { kept.Add(z); continue; }

            if (allow <= 0) { trimmed += z.EnemyCount; dropped++; Object.DestroyImmediate(z.gameObject); continue; }
            trimmed += z.EnemyCount - allow;
            z.Setup(z.Size, allow, z.UnitKey);
            kept.Add(z);
        }

        if (trimmed > 0)
            Debug.Log($"[Zone1] 적 무리 상한(반경 {radius}m · 가중치 {maxWeight}): {trimmed}마리 감축, 존 {dropped}개 제거 → 존 {kept.Count}개");
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
        // 갭 3개를 '비워 둘 곳'으로 등록 — 안 하면 도로 잔해가 아케이드 입구를 막는다(MarkGap 주석 참조).
        MarkGap(31f, 74f, 31f, 83f);    // 南 갭 x28~34 (튜토에서 올라옴)
        MarkGap(43f, 109f, 43f, 118f);  // 北 갭 x40~46 (가로 간선)
        MarkGap(55f, 95f, 64f, 95f);    // 東 갭 y92~98 (세로 지선)

        // 약국(앵커) — 방 x38~58 y80~98, 서문(스파인 향). 껍데기만 — 내부는 Int_Pharmacy 씬.
        n += SolidBuilding(m, "Pharmacy", 38f, 80f, 58f, 98f, 'W', 87f, "Int_Pharmacy", "from_pharmacy");
        // 메모는 **문 밖 스파인**에(건물 안에 두면 껍데기 원칙 위반 — 외부에서 보이면 안 된다).
        n += GreyboxBuild.Note(m, "Pharmacy_Note", 32f, 84f, "약국 카운터 메모",
            "처방 약은 약장(MedCabinet) 안. 카운터 밑 열쇠(key_pharmacy)로 연다.");

        // 서측 열 — **노점 골목**(2026-07-28). 튜토 바로 옆이 또 네모 격자면 첫 두 구역이 같아 보인다.
        //   3~5m 좌판이 실틈(0.9m)을 두고 늘어서고 가운데 통로는 2m — 맵에서 가장 잔 텍스처.
        //   좌판은 들어가는 건물이 아니라 **뒤지는 물건**이라 여기만 걷는 속도가 확 느려진다.
        n += StallAlley(m, "AW", X0 + 2f, Y0 + 2f, 28f, Y1 - 2f, H(101, 7));

        // 약국 위쪽은 절차 점포 대신 **골목 점포**(유니크) — 아케이드 최심부의 곁가지.
        n += UniqueShop(m, "AlleyShop", "골목 점포 ★", "Int_AlleyShop", "from_alleyshop",
                        40f, 99f, 52f, 110f, 'N', 45f, 0, "bandit_melee_1",
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
        // ★ 돔의 **유일한** 입구. 등록 안 하면 간선 갓길 버스가 여길 막아 돔 전체가 못 가는 땅이 된다.
        MarkGap(84f, 122f, 84f, 132f);

        // 중앙 금고실(돔 코어) — 문 = 金庫(key_dome_code 잠금). 최고 보상. 내부는 Int_Dome 씬.
        n += SolidBuilding(m, "DomeCore", 76f, 140f, 96f, 158f, 'S', 84f, "Int_Dome", "from_dome");

        // 온실 화단(벤치=선반) + 고가 루팅
        n += GreyboxBuild.Marker(m, "gb_shelf", "GH_Bed1", 70f, 132f);
        n += GreyboxBuild.Marker(m, "gb_shelf", "GH_Bed2", 74f, 136f);
        n += GreyboxBuild.Marker(m, "gb_shelf", "GH_Bed3", 98f, 134f);
        n += GreyboxBuild.Marker(m, "gb_shelf", "GH_Bed4", 71f, 163f);
        n += GreyboxBuild.Marker(m, "gb_crate", "GH_Crate1", 69f, 152f);
        n += GreyboxBuild.Marker(m, "gb_crate", "GH_Crate2", 99f, 163f);
        // 습지 침수(이동 제약) = 바리케이드 패치 — 돔 주위를 도는 동선을 만든다.
        //   2026-07-28: 침수대가 외벽에 너무 붙어 **0.5m 슬롯**을 만들었고, 그 안에 상자가 생겨
        //   영영 못 줍는 루트가 됐다(연결성 시뮬레이션으로 확인). 벽에서 2m 이상 띄운다.
        n += GreyboxBuild.Barricade(m, "GH_Flood_W", 72.5f, 148f, 4f, 14f);   // x70.5~74.5 (서벽 x68에서 2.5m)
        n += GreyboxBuild.Barricade(m, "GH_Flood_E", 96f, 146f, 4f, 12f);     // x94~98  (동벽 x100에서 2m)
        n += GreyboxBuild.Note(m, "GH_Label", 70f, 129f, "식물원 돔 ★★★★★ (시그니처)",
            "중앙 금고 key_dome_code = 최고 보상. 온실 화단·습지 침수(이동 제약). 최고 위험.");
        return n;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ★ 둘레형 블록(perimeter block) — 2026-07-28 재작성
    //    (docs/level-scrapmarket.md "★생성 단위 재정의: 필지(lot) → 둘레형 블록")
    //
    //  구(Shops): 블록을 필지 격자로 쪼개 필지마다 네모를 세웠다. 그래서 무엇을 조절해도
    //    **네모의 크기·간격만** 바뀌었다 — 밀도를 올리면 다닥다닥, 내리면 텅 빔.
    //    둘 사이에 답이 없었던 건 값이 아니라 **생성 단위**가 틀렸기 때문이다.
    //
    //  신(PerimeterBlock): 생성 단위가 **블록 하나**다.
    //    · 건물이 블록 테두리를 두께 8~12m 띠로 두르고, 그 띠를 8~13m 조각으로 끊어 각각 한 채
    //    · 모서리 조각은 두 방향으로 팔을 뻗어 **ㄱ자** — 정사각형을 '피하는' 게 아니라 안 생긴다
    //    · 띠를 뚫는 것은 **아치(통로) 1~2개**뿐. 블록 안에 골목 격자를 두지 않는다
    //    · 안쪽은 **안뜰** = 콘텐츠 공간(프랍·루트·적). 에워싸여 있어 교전이 갇힌 싸움이 되고
    //      "들어갈까" 하는 판단이 생긴다. **여백이 '장소'가 된다.**
    //    · 블록 = 정체성 단위 — 안뜰 이름표(쪽지)·성격·위험도를 블록 단위로 준다
    //    · 유니크/랜드마크 건물은 **띠의 한 조각**으로 편입한다(BandSpot)
    //
    //  ※ 문서의 "띠를 3~6조각"은 MaxSpan(13m, 아트 리소스 상한)과 양립하지 않는다 —
    //    둘레 180m를 6조각 내면 조각 하나가 30m다. **MaxSpan이 상위 제약**이므로
    //    조각 길이를 8~13m로 잡고 개수는 변 길이에 맡긴다(블록당 8~16채).
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>둘레 띠 두께 범위(m). 변마다 달라서 안뜰이 정사각형이 되지 않는다.</summary>
    const float BandMin = 8f, BandMax = 12f;
    /// <summary>안뜰 최소 한 변 — 이보다 좁으면 '마당'이 아니라 그냥 틈이다.</summary>
    const float CourtMin = 11f;
    /// <summary>아치(통로) 폭 — 골목이 아니라 '건물 밑을 지나는 통로'로 읽히는 폭.</summary>
    const float ArchGap = 3.6f;
    /// <summary>띠 조각 목표 길이. MaxSpan(13) 이하라야 단지 분할이 안 일어나 띠가 갈라지지 않는다.</summary>
    const float SegLen = 11f;

    /// <summary>띠에 심는 '이름 있는 한 채'(랜드마크·유니크 점포).
    /// 유니크 건물을 블록 밖에 따로 세우지 않고 **띠의 한 조각으로 편입**한다(확정 설계 ③).</summary>
    struct BandSpot
    {
        public char   side;        // 앉힐 변 'S','N','W','E'
        public float  at;          // 그 변에서의 위치(0~1). wholeSide면 무시
        public bool   wholeSide;   // 변 전체를 이 한 채(단지)로 — 랜드마크용
        public bool   inward;      // 문이 **안뜰**을 향한다(아치를 지나 들어가야 열리는 집)
        public string id, label, scene, note;
        public int    enemies;
        public string enemyKey;
    }

    static char Opp(char s) => s == 'S' ? 'N' : s == 'N' ? 'S' : s == 'W' ? 'E' : 'W';

    static float BandT(int seed, int k) => BandMin + (H(seed, k) % 100) * (BandMax - BandMin) * 0.01f;

    /// <summary>모서리 팔 길이 — 0(사각 모서리) 또는 3~9.8m(ㄱ자).</summary>
    static float Arm(int seed, int k)
    {
        int v = H(seed, k) % 100;
        return v < 32 ? 0f : 3f + (v - 32) * 0.1f;
    }

    /// <summary>마주 보는 두 변 두께의 합이 커서 안뜰이 사라지지 않게 눌러 준다.</summary>
    static void Squeeze(ref float a, ref float b, float span)
    {
        float over = a + b - (span - CourtMin);
        if (over <= 0f) return;
        a = Mathf.Max(5f, a - over * 0.5f);
        b = Mathf.Max(5f, b - over * 0.5f);
    }

    /// <summary>한 변에 붙은 두 모서리 팔이 변을 다 먹지 않게(최소 minRun은 남긴다).</summary>
    static void ClampArms(ref float a, ref float b, float avail, float minRun)
    {
        float over = a + b - (avail - minRun);
        if (over <= 0f) return;
        float t = a + b;
        if (t <= 0.001f) { a = b = 0f; return; }
        a = Mathf.Max(0f, a - over * (a / t));
        b = Mathf.Max(0f, b - over * (b / t));
        if (a < 2.5f) a = 0f;
        if (b < 2.5f) b = 0f;
    }

    static bool HasWholeSide(BandSpot[] spots, char side)
    {
        if (spots == null) return false;
        for (int i = 0; i < spots.Length; i++)
            if (spots[i].wholeSide && spots[i].side == side) return true;
        return false;
    }

    /// <summary>둘레형 블록 — 블록 테두리를 건물 띠가 두르고 안쪽은 안뜰이 된다.</summary>
    /// <param name="spots">띠에 편입할 이름 있는 건물들(랜드마크·유니크 점포). null이면 전부 절차 생성.</param>
    /// <param name="courtKind">안뜰 성격(0 야적장 / 1 주차 / 2 뒷골목 창고 / 3 무너진 안뜰 / 4 공원). -1=해시</param>
    /// <param name="arches">안뜰 진입 통로 개수. -1=블록 크기에 따라 1~2</param>
    static int PerimeterBlock(GameObject m, string p, float x0, float y0, float x1, float y1, int seed,
                              BandSpot[] spots = null, int courtKind = -1, int courtEnemies = -1,
                              string yardLabel = null, string yardNote = null, int arches = -1)
    {
        float bw = x1 - x0, bh = y1 - y0;
        if (bw < 5f || bh < 5f) return 0;

        // 둘레를 두를 수 없는 자투리 → 연립 띠(길을 마주 보는 연속 벽).
        if (bw < BandMin * 2f + CourtMin || bh < BandMin * 2f + CourtMin)
            return RowTerrace(m, p, x0, y0, x1, y1, seed);

        float tS = BandT(seed, 1), tN = BandT(seed, 2), tW = BandT(seed, 3), tE = BandT(seed, 4);
        Squeeze(ref tS, ref tN, bh);
        Squeeze(ref tW, ref tE, bw);

        // 모서리 ㄱ자 팔 — 있으면 L자, 0이면 사각 모서리.
        float swV = Arm(seed, 11), swH = Arm(seed, 12);
        float seV = Arm(seed, 13), seH = Arm(seed, 14);
        float nwV = Arm(seed, 15), nwH = Arm(seed, 16);
        float neV = Arm(seed, 17), neH = Arm(seed, 18);
        ClampArms(ref swH, ref seH, bw - tW - tE, 12f);   // 남변
        ClampArms(ref nwH, ref neH, bw - tW - tE, 12f);   // 북변
        ClampArms(ref swV, ref nwV, bh - tS - tN, 12f);   // 서변
        ClampArms(ref seV, ref neV, bh - tS - tN, 12f);   // 동변

        int n = 0;
        n += CornerL(m, $"{p}SW", x0, y0, tW, tS,  1f,  1f, swV, swH, H(seed, 31));
        n += CornerL(m, $"{p}SE", x1, y0, tE, tS, -1f,  1f, seV, seH, H(seed, 32));
        n += CornerL(m, $"{p}NW", x0, y1, tW, tN,  1f, -1f, nwV, nwH, H(seed, 33));
        n += CornerL(m, $"{p}NE", x1, y1, tE, tN, -1f, -1f, neV, neH, H(seed, 34));

        // 모서리가 먹고 남은 구간 = 변 하나의 길이.
        float sA0 = x0 + tW + swH, sA1 = x1 - tE - seH;
        float nA0 = x0 + tW + nwH, nA1 = x1 - tE - neH;
        float wA0 = y0 + tS + swV, wA1 = y1 - tN - nwV;
        float eA0 = y0 + tS + seV, eA1 = y1 - tN - neV;

        // ── 아치 — 띠를 뚫는 유일한 정규 진입로. 긴 변부터 고른다(짧은 변은 조각이 안 남는다).
        if (arches < 0) arches = (bw + bh > 84f) ? 2 : 1;
        char[] sc = { 'S', 'N', 'W', 'E' };
        float[] sl = { sA1 - sA0, nA1 - nA0, wA1 - wA0, eA1 - eA0 };
        for (int a = 0; a < 4; a++)
        for (int b = a + 1; b < 4; b++)
            if (sl[b] > sl[a])
            {
                float f = sl[a]; sl[a] = sl[b]; sl[b] = f;
                char cc = sc[a]; sc[a] = sc[b]; sc[b] = cc;
            }
        var archSide = new System.Collections.Generic.List<char>();
        for (int a = 0; a < 4 && archSide.Count < arches; a++)
        {
            if (sl[a] < ArchGap + 4f) break;
            if (HasWholeSide(spots, sc[a])) continue;   // 랜드마크가 통째로 쓰는 변은 안 뚫는다
            archSide.Add(sc[a]);
        }
        if (archSide.Count == 0)
        {
            // 통로가 하나도 안 나오면 안뜰이 **영영 못 가는 죽은 땅**이 된다 — 가장 긴 변에 강제로.
            for (int a = 0; a < 4; a++) if (sl[a] >= ArchGap + 2f) { archSide.Add(sc[a]); break; }
            if (archSide.Count == 0)
                Debug.LogWarning($"[Zone1] {p}: 안뜰 진입로를 못 뚫었습니다(변이 전부 너무 짧음).");
        }

        // 붕괴 구간 — 블록당 최대 한 곳. 띠에 '무너져 뚫린 틈'이 생겨 정규 통로 말고 다른 길이 열린다.
        char ruinSide = (H(seed, 61) % 100 < (int)(RuinRatio * 190f)) ? sc[H(seed, 62) % 4] : '\0';

        n += BandRun(m, $"{p}S", 'S', sA0, sA1, y0, y0 + tS, true,  H(seed, 21), archSide.Contains('S'), spots, ruinSide == 'S');
        n += BandRun(m, $"{p}N", 'N', nA0, nA1, y1, y1 - tN, true,  H(seed, 22), archSide.Contains('N'), spots, ruinSide == 'N');
        n += BandRun(m, $"{p}W", 'W', wA0, wA1, x0, x0 + tW, false, H(seed, 23), archSide.Contains('W'), spots, ruinSide == 'W');
        n += BandRun(m, $"{p}E", 'E', eA0, eA1, x1, x1 - tE, false, H(seed, 24), archSide.Contains('E'), spots, ruinSide == 'E');

        // 안뜰 — 여기서부터가 '장소'다.
        n += Courtyard(m, p, x0 + tW, y0 + tS, x1 - tE, y1 - tN, courtKind, courtEnemies, seed, yardLabel, yardNote);
        return n;
    }

    /// <summary>모서리 한 채 — 코너 사각형 + 두 팔(있으면) = **ㄱ자**.
    /// (cx,cy)=블록 바깥 꼭짓점, (sx,sy)=안쪽 방향(±1).
    /// 모서리 집은 길 **둘**을 면하므로 절반 가까이는 문을 낸다 — 교차로마다 들어갈 데가 생긴다.</summary>
    static int CornerL(GameObject m, string name, float cx, float cy, float tx, float ty,
                       float sx, float sy, float armV, float armH, int seed)
    {
        float bx0 = Mathf.Min(cx, cx + sx * tx), bx1 = Mathf.Max(cx, cx + sx * tx);
        float by0 = Mathf.Min(cy, cy + sy * ty), by1 = Mathf.Max(cy, cy + sy * ty);
        uint h = (uint)H(seed, 55);

        int n;
        if (h % 100u < 42u)
        {
            char side = ((h >> 7) % 2u == 0u) ? (sx > 0f ? 'W' : 'E') : (sy > 0f ? 'S' : 'N');
            float dAt = (side == 'S' || side == 'N') ? (bx0 + bx1) * 0.5f - 1f : (by0 + by1) * 0.5f - 1f;
            n = BandBuilding(m, name, bx0, by0, bx1, by1, side, dAt);
        }
        else n = Fill(m, name, cx, cy, cx + sx * tx, cy + sy * ty);

        if (armV >= 2.5f) n += Fill(m, $"{name}v", cx, cy + sy * ty, cx + sx * tx, cy + sy * (ty + armV));
        if (armH >= 2.5f) n += Fill(m, $"{name}h", cx + sx * tx, cy, cx + sx * (tx + armH), cy + sy * ty);
        return n;
    }

    static int Fill(GameObject m, string name, float ax, float ay, float bx, float by)
        => BandMass(m, name, Mathf.Min(ax, bx), Mathf.Min(ay, by), Mathf.Max(ax, bx), Mathf.Max(ay, by));

    /// <summary>띠 조각 덩어리 — MaxSpan을 넘으면 **틈 없이** 쪼갠다.
    /// `SolidMass`는 조각 사이에 1.4m 실개틈을 두는데, 띠에 그걸 쓰면 사방이 뚫려
    /// "아치로만 안뜰에 들어간다"는 규칙이 통째로 무너진다.</summary>
    static int BandMass(GameObject m, string name, float x0, float y0, float x1, float y1)
    {
        float w = x1 - x0, d = y1 - y0;
        if (w < 1.2f || d < 1.2f) return 0;
        int cols = Mathf.Max(1, Mathf.CeilToInt(w / MaxSpan - 0.001f));
        int rows = Mathf.Max(1, Mathf.CeilToInt(d / MaxSpan - 0.001f));
        float pw = w / cols, pd = d / rows;
        int n = 0;
        for (int i = 0; i < rows; i++)
        for (int j = 0; j < cols; j++)
        {
            float px = x0 + j * pw, py = y0 + i * pd;
            n += GreyboxBuild.Wall(m, cols * rows == 1 ? name : $"{name}_{i}{j}",
                                   px + pw * 0.5f, py + pd * 0.5f, pw, pd);
            MarkBuilding(px, py, px + pw, py + pd);
        }
        return n;
    }

    /// <summary>띠 조각 + 문. scene을 주면 전용 내부, force면 잠금 없이 Int_Generic,
    /// 아니면 `GameTuning.buildingEnterRatio` 필터를 거친다.</summary>
    static int BandBuilding(GameObject m, string name, float x0, float y0, float x1, float y1,
                            char side, float doorAt, string scene = null, bool force = false)
    {
        // 3D — 별도 실내 씬 없이 **같은 맵에서 걸어 들어간다.**
        // 작은 창고·헛간은 방으로 만들 수 없으니(사람이 낀다) 막힌 덩어리로 둔다.
        if (GreyboxBuild.Use3D)
        {
            if (!Greybox3D.CanBeRoom(x0, y0, x1, y1)) return BandMass(m, name, x0, y0, x1, y1);

            DoorPad(x0, y0, x1, y1, side, doorAt, out float ex, out float ey, out float _, out float _2);
            float gapAt = (side == 'S' || side == 'N') ? ex - 1.2f : ey - 1.2f;
            MarkBuilding(x0, y0, x1, y1);
            return Greybox3D.Room(m, name, x0, y0, x1, y1, side, gapAt);
        }

        int n = BandMass(m, name, x0, y0, x1, y1);
        if (n == 0) return 0;
        DoorPad(x0, y0, x1, y1, side, doorAt, out float ex2, out float ey2, out float tw, out float th);
        if (!string.IsNullOrEmpty(scene)) n += Enter(m, $"{name}_Door", ex2, ey2, scene, "default", tw, th);
        else if (force)                   n += Enter(m, $"{name}_Door", ex2, ey2, "Int_Generic", "default", tw, th);
        else                              n += EnterGeneric(m, $"{name}_Door", ex2, ey2, tw, th);
        return n;
    }

    /// <summary>아치(통로) 자리를 '비워 둘 곳'으로 등록 — 프랍·도로 잔해가 통로를 막으면
    /// 안뜰이 통째로 못 가는 땅이 된다. 통로 깊이를 따라 3점을 찍는다.</summary>
    static void MarkArchAt(float ac, float dOut, float dIn, bool horizontal)
    {
        for (int k = 0; k <= 2; k++)
        {
            float d = Mathf.Lerp(dOut, dIn, k * 0.5f);
            KeepOut(horizontal ? ac : d, horizontal ? d : ac);
        }
    }

    /// <summary>손으로 그린 외벽의 **갭(출입구)** 도 비워 둘 곳으로 등록한다. A→B는 갭을 지나는 통행선.
    ///
    /// 2026-07-28: 갭은 '문'이 아니라 '벽의 빈칸'이라 여태 `_keepOut`에 없었다. 그 결과
    ///   간선 갓길에 붙은 버스(9.4m) 두 대가 **식물원 돔의 유일한 입구(南 갭 x80~88)를 통째로 막아**
    ///   시그니처 랜드마크가 도달 불가였다. 그레이박스 배치를 그대로 돌려 연결성을 검사해 찾았다.</summary>
    static void MarkGap(float ax, float ay, float bx, float by)
    {
        for (int k = 0; k <= 2; k++)
            KeepOut(Mathf.Lerp(ax, bx, k * 0.5f), Mathf.Lerp(ay, by, k * 0.5f));
    }

    /// <summary>띠의 한 변 — 진행축 [a0,a1]을 8~13m 조각으로 **틈 없이** 채운다.
    /// dOut=길을 면한 깊이, dIn=안뜰을 면한 깊이(부호 있음).</summary>
    static int BandRun(GameObject m, string p, char side, float a0, float a1,
                       float dOut, float dIn, bool horizontal, int seed,
                       bool arch, BandSpot[] spots, bool allowRuin)
    {
        float L = a1 - a0;
        if (L < 4f) return 0;
        char inSide = Opp(side);

        // 변 하나를 통째로 쓰는 랜드마크(단지)인가.
        if (spots != null)
            for (int s = 0; s < spots.Length; s++)
                if (spots[s].wholeSide && spots[s].side == side)
                    return LandmarkRun(m, spots[s], a0, a1, dOut, dIn, horizontal, side, seed);

        float gap = arch ? ArchGap : 0f;
        float usable = L - gap;
        if (usable < 4.5f)
        {
            if (arch) MarkArchAt((a0 + a1) * 0.5f, dOut, dIn, horizontal);   // 변 전체가 통로
            return 0;
        }

        int cnt = Mathf.Max(1, Mathf.RoundToInt(usable / SegLen));
        while (cnt > 1 && usable / cnt < 6.5f) cnt--;
        int archAt = arch ? (cnt >= 2 ? 1 + (H(seed, 71) % (cnt - 1)) : (H(seed, 72) % 2)) : -1;

        float[] wt = LotWeights(cnt, seed * 19 + 7);
        var st = new float[cnt];
        var ln = new float[cnt];
        {
            float a = a0;
            for (int i = 0; i < cnt; i++)
            {
                if (i == archAt) a += gap;
                st[i] = a; ln[i] = usable * wt[i]; a += ln[i];
            }
        }
        if (arch)
            MarkArchAt(archAt <= 0    ? a0 + gap * 0.5f
                     : archAt >= cnt  ? a1 - gap * 0.5f
                                      : st[archAt] - gap * 0.5f, dOut, dIn, horizontal);

        // 이름 있는 건물 배정 — 요청 위치에 가장 가까운 조각.
        var spotOf = new int[cnt];
        for (int i = 0; i < cnt; i++) spotOf[i] = -1;
        if (spots != null)
            for (int s = 0; s < spots.Length; s++)
            {
                if (spots[s].side != side || spots[s].wholeSide) continue;
                float target = Mathf.Lerp(a0, a1, Mathf.Clamp01(spots[s].at));
                int best = -1; float bd = float.MaxValue;
                for (int i = 0; i < cnt; i++)
                {
                    if (spotOf[i] >= 0) continue;
                    float d = Mathf.Abs(st[i] + ln[i] * 0.5f - target);
                    if (d < bd) { bd = d; best = i; }
                }
                if (best >= 0) spotOf[best] = s;
            }

        int ruinAt = allowRuin ? (int)((uint)H(seed, 85) % (uint)cnt) : -1;

        int n = 0;
        float depth = Mathf.Abs(dIn - dOut);
        float sgn   = Mathf.Sign(dIn - dOut);
        for (int i = 0; i < cnt; i++)
        {
            uint h = (uint)H(seed * 31 + i * 7, 5);

            // 안쪽(안뜰) 면을 들쭉날쭉하게 — 안뜰 윤곽이 사각형이 아니게 된다.
            float shrink = ((h >> 3) % 100u < 45u) ? 1.0f + ((h >> 9) % 20u) * 0.09f : 0f;
            // 앞마당(setback) — 5채 중 1채는 길에서 물러난다(정면선이 일직선이 아니게).
            float back = ((h >> 17) % 100u < 20u) ? 1.2f + ((h >> 21) % 10u) * 0.12f : 0f;
            if (depth - shrink - back < 5.5f) { shrink = 0f; back = 0f; }

            float lo = Mathf.Min(dOut + sgn * back, dIn - sgn * shrink);
            float hi = Mathf.Max(dOut + sgn * back, dIn - sgn * shrink);

            float bx0 = horizontal ? st[i]         : lo;
            float by0 = horizontal ? lo            : st[i];
            float bx1 = horizontal ? st[i] + ln[i] : hi;
            float by1 = horizontal ? hi            : st[i] + ln[i];
            string name = $"{p}{i}";

            if (spotOf[i] >= 0) { n += BandLandmark(m, spots[spotOf[i]], bx0, by0, bx1, by1, side, inSide); continue; }
            // 다른 건물이 선점한 자리면 비워 둔다 — 겹쳐 지으면 서로 뚫고 나온다.
            if (IsIndoors((bx0 + bx1) * 0.5f, (by0 + by1) * 0.5f)) continue;
            if (i == ruinAt) { n += RuinLot(m, name, bx0, by0, bx1, by1, h); continue; }

            if ((h % 100u) >= 48u) { n += BandMass(m, name, bx0, by0, bx1, by1); continue; }
            // 문의 30%는 **안뜰**을 향한다 — 아치를 지나 들어가야만 열리는 집.
            char ds = ((h >> 5) % 100u) < 30u ? inSide : side;
            float dAt = (ds == 'S' || ds == 'N') ? (bx0 + bx1) * 0.5f - 1f : (by0 + by1) * 0.5f - 1f;
            n += BandBuilding(m, name, bx0, by0, bx1, by1, ds, dAt);
        }
        return n;
    }

    /// <summary>띠 조각 하나를 차지하는 이름 있는 건물.</summary>
    static int BandLandmark(GameObject m, BandSpot sp, float x0, float y0, float x1, float y1,
                            char outward, char inward)
    {
        char side = sp.inward ? inward : outward;
        float dAt = (side == 'S' || side == 'N') ? (x0 + x1) * 0.5f - 1f : (y0 + y1) * 0.5f - 1f;
        int n = BandBuilding(m, sp.id, x0, y0, x1, y1, side, dAt, sp.scene, true);
        return n + LandmarkTrim(m, sp, x0, y0, x1, y1, side, dAt);
    }

    /// <summary>랜드마크의 '정체성' — 문 앞 이름표(쪽지) + 건물 앞 적.
    /// **크기가 아니라 이게** 랜드마크를 만든다(아트 리소스 상한 13m 아래서도 성립하게).</summary>
    static int LandmarkTrim(GameObject m, BandSpot sp, float x0, float y0, float x1, float y1,
                            char side, float dAt)
    {
        DoorPad(x0, y0, x1, y1, side, dAt, out float ex, out float ey, out _, out _);
        float ox = side == 'W' ? -1f : side == 'E' ? 1f : 0f;
        float oy = side == 'S' ? -1f : side == 'N' ? 1f : 0f;
        int n = 0;
        if (!string.IsNullOrEmpty(sp.label))
            n += GreyboxBuild.Note(m, $"{sp.id}_Label", ex + ox * 2.6f, ey + oy * 2.6f,
                                   sp.label, sp.note ?? sp.label);
        // 건물 **앞**(문 바깥)에 적 존 — 안에 두면 껍데기에서 스폰돼 걸어 나온다.
        if (sp.enemies > 0)
            n += EnemyZone(m, $"EZ_{sp.id}", ex + ox * 3.4f, ey + oy * 3.4f, 7f, 7f,
                           string.IsNullOrEmpty(sp.enemyKey) ? "bandit_melee_1" : sp.enemyKey, sp.enemies);
        return n;
    }

    /// <summary>변 하나를 통째로 쓰는 랜드마크 — **여러 동이 붙은 단지**로 짓는다.
    /// MaxSpan 때문에 한 채로는 못 세우지만, 틈 없이 붙이고 동마다 안뜰 쪽 깊이를 흔들면
    /// 실루엣이 들쭉날쭉해져 '큰 시설'로 읽힌다. 안뜰은 그 시설의 뒷마당이 된다.</summary>
    static int LandmarkRun(GameObject m, BandSpot sp, float a0, float a1, float dOut, float dIn,
                           bool horizontal, char outward, int seed)
    {
        char side = sp.inward ? Opp(outward) : outward;
        float L = a1 - a0;
        if (L < 5f) return 0;

        int wings = Mathf.Clamp(Mathf.RoundToInt(L / 16f), 1, 4);
        float[] wt = LotWeights(wings, seed * 29 + 3);
        float sgn = Mathf.Sign(dIn - dOut), depth = Mathf.Abs(dIn - dOut);
        int doorWing = wings / 2;

        int n = 0;
        float a = a0, dx0 = 0f, dy0 = 0f, dx1 = 0f, dy1 = 0f;
        for (int i = 0; i < wings; i++)
        {
            float len = L * wt[i];
            uint h = (uint)H(seed * 13 + i, 9);
            float shrink = (i == doorWing) ? 0f : ((h % 100u) < 60u ? 0.8f + (h % 18u) * 0.11f : 0f);
            if (depth - shrink < 6f) shrink = 0f;
            float lo = Mathf.Min(dOut, dIn - sgn * shrink), hi = Mathf.Max(dOut, dIn - sgn * shrink);

            float bx0 = horizontal ? a       : lo, by0 = horizontal ? lo : a;
            float bx1 = horizontal ? a + len : hi, by1 = horizontal ? hi : a + len;
            n += BandMass(m, wings == 1 ? sp.id : $"{sp.id}_w{i}", bx0, by0, bx1, by1);
            if (i == doorWing) { dx0 = bx0; dy0 = by0; dx1 = bx1; dy1 = by1; }
            a += len;
        }
        if (dx1 - dx0 < 1f || dy1 - dy0 < 1f) return n;

        float dAt = (side == 'S' || side == 'N') ? (dx0 + dx1) * 0.5f - 1f : (dy0 + dy1) * 0.5f - 1f;
        DoorPad(dx0, dy0, dx1, dy1, side, dAt, out float ex, out float ey, out float tw, out float th);
        n += Enter(m, $"{sp.id}_Door", ex, ey,
                   string.IsNullOrEmpty(sp.scene) ? "Int_Generic" : sp.scene, "default", tw, th);
        return n + LandmarkTrim(m, sp, dx0, dy0, dx1, dy1, side, dAt);
    }

    /// <summary>연립 띠 — 둘레를 두를 수 없는 자투리를 **길을 마주 보는 연속 벽**으로 채운다.
    /// 옛 필지 격자와 달리 조각 사이에 틈이 없다: 자투리가 '빈 땅'이 아니라 '벽'이 된다.
    /// 길면 통로 하나를 뚫어 관통 동선을 남긴다.</summary>
    static int RowTerrace(GameObject m, string p, float x0, float y0, float x1, float y1, int seed)
    {
        float bw = x1 - x0, bh = y1 - y0;
        if (bw < 5f || bh < 5f) return 0;
        bool horizontal = bw >= bh;
        float L = horizontal ? bw : bh, D = horizontal ? bh : bw;

        bool arch = L > 30f;
        float gap = arch ? ArchGap : 0f;
        float usable = L - gap;
        int cnt = Mathf.Max(1, Mathf.RoundToInt(usable / SegLen));
        while (cnt > 1 && usable / cnt < 6f) cnt--;
        int archAt = (arch && cnt >= 2) ? 1 + (H(seed, 41) % (cnt - 1)) : -1;
        if (archAt < 0) { arch = false; gap = 0f; usable = L; }

        float[] wt = LotWeights(cnt, seed * 23 + 9);
        int ruinAt = (H(seed, 43) % 100 < 35) ? (int)((uint)H(seed, 44) % (uint)cnt) : -1;

        int n = 0;
        float a = horizontal ? x0 : y0;
        for (int i = 0; i < cnt; i++)
        {
            if (i == archAt)
            {
                MarkArchAt(a + gap * 0.5f, horizontal ? y0 : x0, horizontal ? y1 : x1, horizontal);
                a += gap;
            }
            float len = usable * wt[i];
            uint h = (uint)H(seed * 37 + i * 5, 11);
            // 뒷면만 들쭉날쭉하게 — 앞(길)은 정렬, 뒤는 어긋난다.
            float back = ((h >> 3) % 100u < 42u && D > 9f) ? 1.0f + ((h >> 9) % 18u) * 0.09f : 0f;
            float lo = horizontal ? y0 : x0;
            float hi = (horizontal ? y1 : x1) - back;
            if (hi - lo < 5f) hi = horizontal ? y1 : x1;

            float bx0 = horizontal ? a       : lo, by0 = horizontal ? lo : a;
            float bx1 = horizontal ? a + len : hi, by1 = horizontal ? hi : a + len;
            string name = $"{p}{i}";
            a += len;

            if (IsIndoors((bx0 + bx1) * 0.5f, (by0 + by1) * 0.5f)) continue;
            if (i == ruinAt) { n += RuinLot(m, name, bx0, by0, bx1, by1, h); continue; }
            if ((h % 100u) >= 45u) { n += BandMass(m, name, bx0, by0, bx1, by1); continue; }

            char ds = horizontal ? ((h >> 5) % 2u == 0u ? 'S' : 'N') : ((h >> 5) % 2u == 0u ? 'W' : 'E');
            float dAt = (ds == 'S' || ds == 'N') ? (bx0 + bx1) * 0.5f - 1f : (by0 + by1) * 0.5f - 1f;
            n += BandBuilding(m, name, bx0, by0, bx1, by1, ds, dAt);
        }
        return n;
    }

    // ── 안뜰(courtyard) ──────────────────────────────────────────────────

    /// <summary>안뜰 안에서 **비어 있고 통로를 막지 않는** 좌표를 뽑는다.</summary>
    static bool CourtSpot(System.Random rnd, float x0, float y0, float x1, float y1, float pad,
                          out float x, out float y)
    {
        for (int t = 0; t < 20; t++)
        {
            x = Mathf.Lerp(x0 + pad, x1 - pad, (float)rnd.NextDouble());
            y = Mathf.Lerp(y0 + pad, y1 - pad, (float)rnd.NextDouble());
            if (IsIndoors(x, y)) continue;
            if (NearKeepOut(x, y, 3.2f)) continue;   // 아치 앞은 비워 둔다(막히면 안뜰이 죽는다)
            return true;
        }
        x = y = 0f;
        return false;
    }

    /// <summary>안뜰 상자 = 루팅 앵커. 내용은 예산제(MapSpawnController)가 채운다.</summary>
    static int CrateAnchor(GameObject m, string name, float x, float y)
    {
        if (IsIndoors(x, y)) return 0;
        if (GreyboxBuild.Marker(m, "gb_crate", name, x, y) == 0) return 0;
        var t = FindChild(m.transform, name);
        if (t == null) return 0;
        var lc = t.GetComponentInChildren<LootContainer>();
        var sp = t.gameObject.AddComponent<ItemSpawnPoint>();
        SetSpawnType(sp, 1);   // Container
        if (lc != null)
        {
            var so = new SerializedObject(sp);
            var lk = so.FindProperty("linkedContainer");
            if (lk != null) { lk.objectReferenceValue = lc; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
        return 1;
    }

    /// <summary>안뜰 — 띠가 에워싼 블록 안쪽. **여백이 아니라 '장소'** 다(확정 설계 ②).
    /// 프랍·루트·적이 여기 있으면 "들어갈까" 하는 판단이 생기고, 교전이 갇힌 싸움이 된다.
    /// 루트는 **위험만큼** 준다 — 적이 있는 안뜰이 더 두둑해야 들어갈 이유가 생긴다.</summary>
    static int Courtyard(GameObject m, string p, float x0, float y0, float x1, float y1,
                         int kind, int enemies, int seed, string label, string note)
    {
        float w = x1 - x0, h = y1 - y0;
        if (w < 7f || h < 7f) return 0;
        if (kind < 0)    kind = H(seed, 91) % 4;
        if (enemies < 0) enemies = (H(seed, 92) % 100 < 55) ? 1 + H(seed, 93) % 2 : 0;

        var rnd = new System.Random(seed ^ 0x5F3A);
        int n = 0;

        // 이름표 — 들어와야 읽힌다. 블록마다 다른 이름 = 길을 외우는 기준점.
        if (!string.IsNullOrEmpty(label))
        {
            float nx = x0 + 2.2f, ny = y0 + 2.2f;
            n += GreyboxBuild.Note(m, $"{p}_Yard", nx, ny, label, note ?? label);
            KeepOut(nx, ny);
        }

        switch (kind)
        {
            case 1:   // 주차 안뜰 — 버려진 차. 시야는 트이되 몸은 숨는다.
            {
                int rows = Mathf.Max(1, Mathf.FloorToInt((h - 5f) / 7f));
                for (int r = 0; r < rows; r++)
                for (int c = 0; c < 8; c++)
                {
                    float cx = x0 + 3.4f + c * 6.4f, cy = y0 + 3.6f + r * 7f;
                    if (cx > x1 - 3f || cy > y1 - 3f) continue;
                    uint hh = (uint)H(seed + r * 41, c * 13 + 7);
                    if (hh % 4u == 0u) continue;                      // 군데군데 빈 자리(주차 구획)
                    if (IsIndoors(cx, cy) || NearKeepOut(cx, cy, 3.4f)) continue;
                    bool truck = hh % 5u == 0u;
                    float len = truck ? 6.8f : 4.4f, wid = truck ? 2.3f : 1.9f;
                    n += GreyboxBuild.Car(m, $"{p}_car{r}{c}", cx, cy, len, wid);
                    MarkBuilding(cx - len * 0.5f, cy - wid * 0.5f, cx + len * 0.5f, cy + wid * 0.5f);
                }
                break;
            }
            case 2:   // 뒷골목 창고 — 안뜰 **안**에 또 건물. 들어와서 한 번 더 돌아야 한다.
            {
                int sheds = 1 + H(seed, 97) % 2;
                for (int i = 0; i < sheds; i++)
                {
                    float sw = 6f + (H(seed + i * 7, 3) % 30) * 0.1f;
                    float sd = 5.5f + (H(seed + i * 7, 4) % 30) * 0.1f;
                    if (w - sw < 8f || h - sd < 8f) break;            // 돌아 걸을 여유가 없으면 포기
                    float sx = x0 + 3.5f + (w - sw - 7f) * ((H(seed + i * 11, 5) % 100) / 100f);
                    float sy = y0 + 3.5f + (h - sd - 7f) * ((H(seed + i * 11, 6) % 100) / 100f);
                    if (IsIndoors(sx + sw * 0.5f, sy + sd * 0.5f)) continue;
                    if (NearKeepOut(sx + sw * 0.5f, sy + sd * 0.5f, 5f)) continue;
                    n += BandBuilding(m, $"{p}_shed{i}", sx, sy, sx + sw, sy + sd, 'S', sx + sw * 0.5f - 1f);
                }
                break;
            }
            case 3:   // 무너진 안뜰 — 잔해 더미. 사이로 걸어 들어갈 틈이 생긴다.
            {
                float mx = (x0 + x1) * 0.5f, my = (y0 + y1) * 0.5f;
                n += RuinLot(m, $"{p}_ruin", mx - w * 0.27f, my - h * 0.27f,
                                             mx + w * 0.27f, my + h * 0.27f, (uint)H(seed, 55));
                break;
            }
            case 4:   // 공원 안뜰 — 화단·벤치. 개활감은 **여기**가 준다(옛 '광장 블록'의 자리).
            {
                for (int i = 0; i < 12; i++)
                {
                    if (!CourtSpot(rnd, x0, y0, x1, y1, 2.6f, out float px, out float py)) continue;
                    bool bed = i % 3 == 0;
                    float sx = bed ? 4.6f : 1.0f, sy = bed ? 2.0f : 1.0f;
                    n += GreyboxBuild.Prop(m, $"{p}_pk{i}", px, py, sx, sy);
                    MarkBuilding(px - sx * 0.5f, py - sy * 0.5f, px + sx * 0.5f, py + sy * 0.5f);
                }
                break;
            }
            default:  // 0 = 야적장 — 팔레트·드럼통 더미.
            {
                for (int i = 0; i < 9; i++)
                {
                    if (!CourtSpot(rnd, x0, y0, x1, y1, 2.4f, out float px, out float py)) continue;
                    float sx = 1.4f + (float)rnd.NextDouble() * 1.7f;
                    float sy = 1.2f + (float)rnd.NextDouble() * 1.5f;
                    n += GreyboxBuild.Prop(m, $"{p}_st{i}", px, py, sx, sy);
                    MarkBuilding(px - sx * 0.5f, py - sy * 0.5f, px + sx * 0.5f, py + sy * 0.5f);
                }
                break;
            }
        }

        // 루트 — 위험 곡선과 짝. 적이 있는 안뜰이 더 두둑하다.
        int crates = (kind == 0 || kind == 2) ? 3 : 2;
        if (enemies > 0) crates++;
        for (int i = 0; i < crates; i++)
        {
            if (!CourtSpot(rnd, x0, y0, x1, y1, 2.2f, out float px, out float py)) continue;
            n += CrateAnchor(m, $"{p}_yc{i}", px, py);
        }

        if (enemies > 0)
        {
            float zw = Mathf.Min(w - 4f, 12f), zh = Mathf.Min(h - 4f, 12f);
            // 험한 안뜰엔 큰 놈이 하나 낀다 — 에워싸인 공간에서의 강공은 훨씬 무섭다.
            bool tank = enemies >= 2 && H(seed, 94) % 100 < 35;
            n += EnemyZone(m, $"EZ_{p}_yard", (x0 + x1) * 0.5f, (y0 + y1) * 0.5f, zw, zh,
                           "bandit_melee_1", tank ? enemies - 1 : enemies);
            if (tank)
                n += EnemyZone(m, $"EZ_{p}_yardT", (x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 7f, 7f, "bandit_tank", 1);
        }
        return n;
    }

    /// <summary>간선 중앙분리대 — 화단/가드레일 토막을 일정 간격으로. 12m 도로에 **축**이 생겨
    /// 큰길로 읽히고, 반대편으로 넘어갈 때 동선이 한 번 꺾인다(=넓지만 직선 질주는 못 함).
    /// 토막 사이는 비워 두므로 통행은 막히지 않는다.</summary>
    static int Median(GameObject m, string p, bool vertical, float center, float from, float to, float step)
    {
        int n = 0, i = 0;
        for (float t = from + step * 0.5f; t < to; t += step, i++)
        {
            float x = vertical ? center : t, y = vertical ? t : center;
            if (NearKeepOut(x, y, 4f)) continue;
            if (IsIndoors(x, y)) continue;
            float sx = vertical ? 1.1f : 4.2f, sy = vertical ? 4.2f : 1.1f;
            n += GreyboxBuild.Prop(m, $"{p}_{i}", x, y, sx, sy);
            MarkBuilding(x - sx * 0.5f, y - sy * 0.5f, x + sx * 0.5f, y + sy * 0.5f);
        }
        return n;
    }

    /// <summary>길목(교차로) — 도로가 만나는 자리마다 작은 무리를 놓는다.
    ///
    /// (2026-07-29 사용자: "길가가 좀 비지 않게 밀도 조금만 더 올려주고, 길목마다")
    /// 교차로는 **길을 외우는 기준점**이다. 여기가 비면 사방이 똑같은 길이라 어디쯤인지 감이 안 온다.
    /// 네 귀퉁이에 각각 다른 것을 놓아(전복 차·잔해·드럼통 무리) 교차로마다 인상이 달라지게 한다.</summary>
    static int Junctions(GameObject m, int seed)
    {
        float[] xs = { 13f, 63f, 108f, 163f };   // 순환 서 · 세로 지선 · 세로 간선 · 순환 동
        float[] ys = { 13f, 75f, 120f, 171f };   // 순환 남 · 가로 지선 · 가로 간선 · 순환 북
        int n = 0, k = 0;
        for (int i = 0; i < xs.Length; i++)
        for (int j = 0; j < ys.Length; j++)
        {
            // 스폰·탈출이 앉은 교차로(순환 네 코너 · 중앙 교차점)는 통째로 건너뛴다.
            //   귀퉁이 넷을 다 채우면 구석에 있는 탈출구가 갇힌다(PX_SE에서 실제로 발생).
            //   거긴 이미 그 자체로 기준점이라 표식이 더 필요하지도 않다.
            if (NearKeepOut(xs[i], ys[j], 11f)) { k += 4; continue; }

            for (int q = 0; q < 4; q++, k++)
            {
                uint h = (uint)H(seed + k * 13, q * 7 + 3);
                float off = 5.5f + (h % 30u) * 0.12f;                 // 교차점에서 5.5~9.1m
                float cx = xs[i] + ((q & 1) == 0 ? -off : off);
                float cy = ys[j] + ((q & 2) == 0 ? -off : off);
                if (cx < FX0 + 3f || cx > FX1 - 3f || cy < FY0 + 3f || cy > FY1 - 3f) continue;
                if (IsIndoors(cx, cy)) continue;
                if (NearKeepOut(cx, cy, 5f)) continue;                // 스폰·탈출·문 앞은 비워 둔다
                if ((h >> 5) % 100u < 22u) continue;                  // 네 귀퉁이가 다 차면 그것대로 답답하다

                string name = $"JX{i}{j}_{q}";
                switch ((h >> 9) % 3u)
                {
                    case 0:   // 전복 차 — 교차로에서 제일 눈에 띄는 표식
                    {
                        bool along = ((h >> 12) & 1u) == 0u;
                        float w = along ? 4.4f : 1.9f, d = along ? 1.9f : 4.4f;
                        n += GreyboxBuild.Car(m, name, cx, cy, w, d);
                        MarkBuilding(cx - w * 0.5f, cy - d * 0.5f, cx + w * 0.5f, cy + d * 0.5f);
                        break;
                    }
                    case 1:   // 무너져 내린 잔해 — 길 폭이 들쭉날쭉해진다
                    {
                        float w = 2.2f + ((h >> 14) % 20u) * 0.13f, d = 1.8f + ((h >> 19) % 18u) * 0.12f;
                        n += GreyboxBuild.Wall(m, name, cx, cy, w, d);
                        MarkBuilding(cx - w * 0.5f, cy - d * 0.5f, cx + w * 0.5f, cy + d * 0.5f);
                        break;
                    }
                    default:  // 드럼통·자재 무리 3개 — 작은 엄폐가 뭉쳐 있어 교전선이 꺾인다
                    {
                        for (int t = 0; t < 3; t++)
                        {
                            uint ht = (uint)H(seed + k * 13 + t * 5, 17);
                            float px = cx + ((ht % 40u) * 0.1f - 2f), py = cy + (((ht >> 6) % 40u) * 0.1f - 2f);
                            if (IsIndoors(px, py) || NearKeepOut(px, py, 4f)) continue;
                            float s = 0.8f + ((ht >> 12) % 12u) * 0.09f;
                            n += GreyboxBuild.Prop(m, $"{name}_{t}", px, py, s, s);
                            MarkBuilding(px - s * 0.5f, py - s * 0.5f, px + s * 0.5f, py + s * 0.5f);
                        }
                        break;
                    }
                }
            }
        }
        return n;
    }

    /// <summary>거리 프랍 — 건물 정면을 따라 자판기·드럼통·쓰레기통·전신주 따위를 붙인다.
    ///
    /// (2026-07-11 사용자: "중간에 프랍도 좀 넣고 유저가 탐험할 맛 나는 맵을 좀 만들어보라고")
    /// 프랍은 세 가지를 동시에 한다:
    ///   ① **길의 표정** — 다 똑같이 생긴 골목에 랜드마크가 생겨 길을 외울 수 있다
    ///   ② **엄폐** — 작지만 몸을 가릴 수 있어 골목 교전이 단조롭지 않다
    ///   ③ **탐험 보상** — 일부는 루팅 앵커라 "구석을 뒤질 이유"가 된다
    /// 건물 사각형의 면을 따라 붙이므로 도로 한복판에 뜨지 않는다.</summary>
    static int StreetProps(GameObject m, int count, int lootEvery, int seed)
    {
        int n = 0;
        if (_buildingRects.Count == 0) return 0;
        var rnd = new System.Random(seed);

        for (int i = 0; i < count; i++)
        {
            var r = _buildingRects[rnd.Next(_buildingRects.Count)];
            float off = 0.8f + (float)rnd.NextDouble() * 0.5f;
            float x, y;
            switch (rnd.Next(4))
            {
                case 0:  x = Mathf.Lerp(r.xMin, r.xMax, (float)rnd.NextDouble()); y = r.yMin - off; break;
                case 1:  x = Mathf.Lerp(r.xMin, r.xMax, (float)rnd.NextDouble()); y = r.yMax + off; break;
                case 2:  x = r.xMin - off; y = Mathf.Lerp(r.yMin, r.yMax, (float)rnd.NextDouble());  break;
                default: x = r.xMax + off; y = Mathf.Lerp(r.yMin, r.yMax, (float)rnd.NextDouble());  break;
            }
            if (x < FX0 + 3f || x > FX1 - 3f || y < FY0 + 3f || y > FY1 - 3f) continue;
            if (IsIndoors(x, y)) continue;                     // 옆 건물에 파묻히면 버림
            if (NearKeepOut(x, y, 3f)) continue;               // 스폰·탈출·입구 앞은 비워 둔다

            // 6개 중 1개는 **길로 흘러내린 잔해**(회색 = 무너진 건물의 일부).
            //   길 폭이 들쭉날쭉해지며 "계획도시"가 아니라 "무너진 도시"로 읽힌다.
            if (i % 9 == 4)
            {
                float rw = 1.8f + (float)rnd.NextDouble() * 2.2f;
                float rh = 1.4f + (float)rnd.NextDouble() * 1.8f;
                if (GreyboxBuild.Wall(m, $"RB_{i}", x, y, rw, rh) == 0) continue;
                MarkBuilding(x - rw * 0.5f, y - rh * 0.5f, x + rw * 0.5f, y + rh * 0.5f);
                n++;
                continue;
            }

            int kind = rnd.Next(4);
            float sx = kind == 0 ? 1.0f : kind == 1 ? 0.8f : kind == 2 ? 1.6f : 0.6f;   // 자판기/드럼통/벤치/전신주
            float sy = kind == 2 ? 0.7f : sx;
            string name = $"SP_{i}";
            if (GreyboxBuild.Prop(m, name, x, y, sx, sy) == 0) continue;
            MarkBuilding(x - sx * 0.5f, y - sy * 0.5f, x + sx * 0.5f, y + sy * 0.5f);
            n++;

            // 일부는 뒤질 수 있는 프랍 — "구석을 살펴볼 이유".
            if (lootEvery > 0 && i % lootEvery == 0)
            {
                var t = FindChild(m.transform, name);
                if (t == null) continue;
                var go = t.gameObject;
                var lc = go.GetComponent<LootContainer>();
                if (lc == null) lc = go.AddComponent<LootContainer>();
                lc.Setup("길가 잡동사니", 2, 2);
                var io = go.GetComponent<InteractableObject>();
                if (io == null) io = go.AddComponent<InteractableObject>();
                io.Configure(InteractableObject.InteractType.Container, "뒤지기", 1.8f);
                var sp = go.GetComponent<ItemSpawnPoint>();
                if (sp == null) sp = go.AddComponent<ItemSpawnPoint>();
                SetSpawnType(sp, 1);
                var so = new SerializedObject(sp);
                var lk = so.FindProperty("linkedContainer");
                if (lk != null) { lk.objectReferenceValue = lc; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
        }
        return n;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ★ 블록 유형 — 2026-07-28
    //  사용자: "11시 1시 3시 중앙 5시 6시가 똑같은 스타일인데 2~3개 정도는 다른 스타일로"
    //
    //  블록이 정체성 단위라면 **블록의 형태 자체**가 정체성이어야 한다. 둘레형 하나로만 채우면
    //  이름표만 다르고 걷는 느낌은 전부 같다 — 링을 돌고, 아치로 들어가고, 안뜰을 훑는다.
    //    · 둘레형   `PerimeterBlock` — 테두리가 두꺼운 띠, 안쪽이 안뜰 (공원 · 상점가 · 유리타워)
    //    · 판상 단지 `SlabRows`      — 긴 동이 나란히. 테두리가 아니라 **줄무늬** (폐아파트)
    //    · 울타리 야적장 `FencedYard` — 테두리가 **얇은 선**(담장)이라 안이 다 보인다 (경찰서 압수장)
    //    · 붕괴장   `RubbleField`    — 형태 자체가 없다. 잔해 사이가 곧 길 (무너진 상가)
    //    · 노점 골목 `StallAlley`    — 3~5m 좌판이 통로 양쪽에. 맵에서 가장 잔 텍스처 (약국 아케이드)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>판상 단지 — 긴 동 2~4개가 나란히 서고 **그 사이가 단지 안길**이 된다.
    ///
    /// 둘레형과 정반대 실루엣이다. 길에서 단지 안이 훤히 들여다보이지만, 들어가면 동과 동 사이에
    /// 갇혀 **앞뒤로만** 움직이게 된다(둘레형은 사방이 막히고 안이 넓다 — 정확히 반대의 압박).
    /// 동 길이를 서로 어긋나게 잡아야 '줄무늬 3개'가 아니라 '단지'로 읽힌다.</summary>
    static int SlabRows(GameObject m, string p, float x0, float y0, float x1, float y1, int seed,
                        BandSpot lm, string yardLabel, string yardNote)
    {
        float bw = x1 - x0, bh = y1 - y0;
        if (bw < 22f || bh < 26f) return PerimeterBlock(m, p, x0, y0, x1, y1, seed);

        const float Gap = 5.2f;                 // 동 사이 = 단지 안길(주차·놀이터가 여기 들어간다)
        int slabs = Mathf.Clamp(Mathf.RoundToInt(bw / 14f), 2, 4);
        float sw = (bw - Gap * (slabs - 1)) / slabs;
        while (slabs > 2 && sw < 7f) { slabs--; sw = (bw - Gap * (slabs - 1)) / slabs; }
        int lmIndex = lm.side == 'W' ? 0 : slabs - 1;   // 랜드마크 동 = 간선을 면한 쪽

        int n = 0;
        for (int i = 0; i < slabs; i++)
        {
            uint h = (uint)H(seed * 17 + i * 5, 7);
            float sx = x0 + i * (sw + Gap);
            // 동 길이를 흔든다 — 다 같으면 그냥 줄무늬다.
            float sy0 = y0 + 1.5f + (h % 5u) * 1.5f;
            float sy1 = y1 - 1.5f - ((h >> 4) % 5u) * 1.5f;
            if (sy1 - sy0 < 14f) { sy0 = y0 + 2f; sy1 = y1 - 2f; }

            bool isLm = i == lmIndex && !string.IsNullOrEmpty(lm.id);
            n += BandMass(m, isLm ? lm.id : $"{p}_s{i}", sx, sy0, sx + sw, sy1);

            // 동 출입구 — 46m짜리 동에 문 하나면 단지가 죽는다. 16m마다 하나씩, 안길을 향해.
            int doors = Mathf.Max(1, Mathf.FloorToInt((sy1 - sy0) / 16f));
            for (int d = 0; d < doors; d++)
            {
                float dAt = Mathf.Lerp(sy0, sy1, (d + 0.5f) / doors) - 1f;
                char side = i == 0 ? 'E' : i == slabs - 1 ? 'W' : ((d % 2 == 0) ? 'W' : 'E');
                DoorPad(sx, sy0, sx + sw, sy1, side, dAt, out float ex, out float ey, out float tw, out float th);
                n += EnterGeneric(m, $"{p}_s{i}d{d}", ex, ey, tw, th);
            }
            // 랜드마크 동은 **간선을 향한 정문**을 따로 갖는다(길에서 바로 보이고 바로 들어간다).
            if (isLm)
            {
                float dAt = (sy0 + sy1) * 0.5f - 1f;
                DoorPad(sx, sy0, sx + sw, sy1, lm.side, dAt, out float ex, out float ey, out float tw, out float th);
                n += Enter(m, $"{lm.id}_Door", ex, ey,
                           string.IsNullOrEmpty(lm.scene) ? "Int_Generic" : lm.scene, "default", tw, th);
                n += LandmarkTrim(m, lm, sx, sy0, sx + sw, sy1, lm.side, dAt);
            }
        }

        // ── 안길 채우기 — 주차 열 / 놀이터 / 루트. 여기가 이 블록의 '안뜰'이다.
        var rnd = new System.Random(seed ^ 0x11AB);
        for (int g = 0; g < slabs - 1; g++)
        {
            float gx = x0 + (g + 1) * sw + g * Gap;      // 안길 좌측 경계
            float cx = gx + Gap * 0.5f;
            bool play = g == (H(seed, 71) % Mathf.Max(1, slabs - 1));   // 안길 하나는 놀이터

            for (float cy = y0 + 5f; cy < y1 - 4f; cy += 7.5f)
            {
                uint h = (uint)H(seed + g * 37, (int)cy);
                if (h % 4u == 0u) continue;
                if (IsIndoors(cx, cy) || NearKeepOut(cx, cy, 3.2f)) continue;
                if (play && h % 3u == 0u)
                {
                    n += GreyboxBuild.Prop(m, $"{p}_pl{g}{(int)cy}", cx, cy, 2.6f, 1.6f);
                    MarkBuilding(cx - 1.3f, cy - 0.8f, cx + 1.3f, cy + 0.8f);
                }
                else
                {
                    n += GreyboxBuild.Car(m, $"{p}_car{g}{(int)cy}", cx, cy, 1.9f, 4.4f);   // 세로 주차
                    MarkBuilding(cx - 0.95f, cy - 2.2f, cx + 0.95f, cy + 2.2f);
                }
            }
            // 안길마다 상자 하나 — 동 사이를 끝까지 걸어 볼 이유.
            if (CourtSpot(rnd, gx, y0 + 3f, gx + Gap, y1 - 3f, 1.2f, out float px, out float py))
                n += CrateAnchor(m, $"{p}_gc{g}", px, py);
        }

        // 단지 이름표 + 적 — 가운데 안길에.
        float mx = x0 + bw * 0.5f, my = y0 + bh * 0.5f;
        if (!string.IsNullOrEmpty(yardLabel))
        {
            float ny = y0 + 4f;
            n += GreyboxBuild.Note(m, $"{p}_Yard", mx, ny, yardLabel, yardNote ?? yardLabel);
            KeepOut(mx, ny);
        }
        n += EnemyZone(m, $"EZ_{p}_yard", mx, my, Mathf.Min(bw - 6f, 14f), Mathf.Min(bh - 6f, 16f),
                       "bandit_melee_1", 2);
        return n;
    }

    /// <summary>울타리 야적장 — 한 변만 건물이고 나머지 세 변은 **담장(두께 0.7m)**.
    ///
    /// 테두리가 '선'이라 밖에서 안이 다 보인다 — 들어가기 전에 **보고 판단**할 수 있는 유일한 블록.
    /// 대신 문은 두 곳뿐이라, 안에서 걸리면 되돌아 나가는 거리가 길다.
    /// 안은 컨테이너 열·창고·압수 차량 — 직선 엄폐물이 줄지어 있어 교전 결이 또 다르다.</summary>
    static int FencedYard(GameObject m, string p, float x0, float y0, float x1, float y1, int seed,
                          BandSpot lm, string yardLabel, string yardNote)
    {
        float bw = x1 - x0, bh = y1 - y0;
        if (bw < 24f || bh < 24f) return PerimeterBlock(m, p, x0, y0, x1, y1, seed);

        int n = 0;
        float depth = 12f;                                  // 건물 변의 두께
        float ix0 = x0, iy0 = y0, ix1 = x1, iy1 = y1;       // 야적장(담장 안) 영역
        switch (lm.side)
        {
            case 'W': ix0 = x0 + depth; break;
            case 'E': ix1 = x1 - depth; break;
            case 'S': iy0 = y0 + depth; break;
            default:  iy1 = y1 - depth; break;
        }

        // ── 건물 변 — 랜드마크 + 그 변을 마저 채우는 보통 건물 1~2채.
        {
            bool vertical = lm.side == 'W' || lm.side == 'E';
            float a0 = vertical ? y0 : x0, a1 = vertical ? y1 : x1;
            float d0 = lm.side == 'W' ? x0 : lm.side == 'E' ? x1 : lm.side == 'S' ? y0 : y1;
            float d1 = lm.side == 'W' ? x0 + depth : lm.side == 'E' ? x1 - depth
                     : lm.side == 'S' ? y0 + depth : y1 - depth;
            n += BandRun(m, $"{p}F", lm.side, a0, a1, d0, d1, !vertical, H(seed, 51), false,
                         new[] { lm }, false);
        }

        // ── 담장 세 면 + 문 2개. 문은 서로 다른 변에 나야 '가로지르는 동선'이 생긴다.
        int gateA = H(seed, 61) % 3, gateB = (gateA + 1 + H(seed, 62) % 2) % 3;
        int side = 0;
        if (lm.side != 'S') { n += Fence(m, $"{p}fS", ix0, iy0, ix1, iy0, side == gateA || side == gateB, H(seed, 71)); side++; }
        if (lm.side != 'N') { n += Fence(m, $"{p}fN", ix0, iy1, ix1, iy1, side == gateA || side == gateB, H(seed, 72)); side++; }
        if (lm.side != 'W') { n += Fence(m, $"{p}fW", ix0, iy0, ix0, iy1, side == gateA || side == gateB, H(seed, 73)); side++; }
        if (lm.side != 'E') { n += Fence(m, $"{p}fE", ix1, iy0, ix1, iy1, side == gateA || side == gateB, H(seed, 74)); }

        // ── 야적장 안 — 컨테이너 열(긴 직선 엄폐) + 창고 1~2채 + 압수 차량.
        var rnd = new System.Random(seed ^ 0x2C0D);
        float yw = ix1 - ix0, yh = iy1 - iy0;
        if (!string.IsNullOrEmpty(yardLabel))
        {
            float nx = ix0 + 3f, ny = iy0 + 3f;
            n += GreyboxBuild.Note(m, $"{p}_Yard", nx, ny, yardLabel, yardNote ?? yardLabel);
            KeepOut(nx, ny);
        }

        int rows = Mathf.Max(1, Mathf.FloorToInt((yh - 6f) / 8f));
        for (int r = 0; r < rows; r++)
        {
            float cy = iy0 + 5f + r * 8f;
            if (cy > iy1 - 4f) break;
            for (float cx = ix0 + 4f; cx < ix1 - 4f; cx += 9f)
            {
                uint h = (uint)H(seed + r * 29, (int)cx);
                if (h % 5u == 0u) continue;                       // 빈 자리(하역 통로)
                bool box = h % 3u != 0u;                          // 컨테이너 / 압수 차량
                float w = box ? 7.5f : 4.4f, d = box ? 2.6f : 1.9f;
                if (cx + w * 0.5f > ix1 - 2f) break;
                if (IsIndoors(cx, cy) || NearKeepOut(cx, cy, 3.6f)) continue;
                n += box ? GreyboxBuild.Prop(m, $"{p}_ct{r}{(int)cx}", cx, cy, w, d)
                         : GreyboxBuild.Car(m, $"{p}_iv{r}{(int)cx}", cx, cy, w, d);
                MarkBuilding(cx - w * 0.5f, cy - d * 0.5f, cx + w * 0.5f, cy + d * 0.5f);
            }
        }

        // 창고 1~2채 — 야적장 안에도 들어갈 데가 있어야 훑을 맛이 난다.
        int sheds = 1 + H(seed, 81) % 2;
        for (int i = 0; i < sheds; i++)
        {
            float sw = 8f + (H(seed + i * 13, 3) % 30) * 0.1f;
            float sd = 6.5f + (H(seed + i * 13, 4) % 25) * 0.1f;
            if (yw - sw < 10f || yh - sd < 10f) break;
            float sx = ix0 + 3f + (yw - sw - 6f) * ((H(seed + i * 19, 5) % 100) / 100f);
            float sy = iy0 + 3f + (yh - sd - 6f) * ((H(seed + i * 19, 6) % 100) / 100f);
            if (IsIndoors(sx + sw * 0.5f, sy + sd * 0.5f)) continue;
            if (NearKeepOut(sx + sw * 0.5f, sy + sd * 0.5f, 5.5f)) continue;
            n += BandBuilding(m, $"{p}_shed{i}", sx, sy, sx + sw, sy + sd, 'S', sx + sw * 0.5f - 1f, null, true);
        }

        for (int i = 0; i < 4; i++)
            if (CourtSpot(rnd, ix0, iy0, ix1, iy1, 2.2f, out float px, out float py))
                n += CrateAnchor(m, $"{p}_yc{i}", px, py);

        n += EnemyZone(m, $"EZ_{p}_yard", (ix0 + ix1) * 0.5f, (iy0 + iy1) * 0.5f,
                       Mathf.Min(yw - 4f, 13f), Mathf.Min(yh - 4f, 13f), "bandit_melee_1", 2);
        n += EnemyZone(m, $"EZ_{p}_yardT", (ix0 + ix1) * 0.5f, (iy0 + iy1) * 0.5f, 7f, 7f, "bandit_tank", 1);
        return n;
    }

    /// <summary>담장 한 줄 — 얇은 벽 + (선택) 문 갭. A→B는 축 정렬이어야 한다.</summary>
    static int Fence(GameObject m, string name, float ax, float ay, float bx, float by, bool gate, int seed)
    {
        const float T = 0.7f, Gate = 4.4f;
        bool horiz = Mathf.Abs(bx - ax) > Mathf.Abs(by - ay);
        float L = horiz ? bx - ax : by - ay;
        if (L < 4f) return 0;
        if (!gate || L < Gate + 6f) return FenceSeg(m, name, ax, ay, horiz, 0f, L, T);

        float g0 = 3f + (H(seed, 5) % 100) * 0.01f * (L - Gate - 6f);
        int n = FenceSeg(m, $"{name}a", ax, ay, horiz, 0f, g0, T)
              + FenceSeg(m, $"{name}b", ax, ay, horiz, g0 + Gate, L, T);
        float gc = g0 + Gate * 0.5f;
        // 문 앞뒤를 비워 둔다 — 담장 문이 잔해로 막히면 야적장이 통째로 죽는다.
        for (int k = -1; k <= 1; k++)
            KeepOut(horiz ? ax + gc : ax + k * 2.2f, horiz ? ay + k * 2.2f : ay + gc);
        return n;
    }

    static int FenceSeg(GameObject m, string name, float ax, float ay, bool horiz, float t0, float t1, float T)
    {
        if (t1 - t0 < 1f) return 0;
        float cx = horiz ? ax + (t0 + t1) * 0.5f : ax;
        float cy = horiz ? ay : ay + (t0 + t1) * 0.5f;
        float w = horiz ? t1 - t0 : T, h = horiz ? T : t1 - t0;
        if (IsIndoors(cx, cy)) return 0;
        int n = GreyboxBuild.Wall(m, name, cx, cy, w, h);
        MarkBuilding(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
        return n;
    }

    /// <summary>붕괴장 — 블록의 **형태가 없다**. 살아남은 벽 조각 몇과 잔해 더미뿐이라
    /// 길이 정해져 있지 않고, 어디로든 갈 수 있지만 어디도 곧게 가지 못한다.
    /// 링·줄무늬·담장과 달리 **실루엣에 직선이 거의 없어** 멀리서도 한눈에 구분된다.</summary>
    static int RubbleField(GameObject m, string p, float x0, float y0, float x1, float y1, int seed,
                           BandSpot lm, string yardLabel, string yardNote)
    {
        float bw = x1 - x0, bh = y1 - y0;
        if (bw < 20f || bh < 20f) return PerimeterBlock(m, p, x0, y0, x1, y1, seed);
        int n = 0;

        // ── 살아남은 한 채(랜드마크) — 길을 면한 변에 붙인다. 폐허 속 유일한 온전한 건물.
        {
            float lw = Mathf.Min(12.5f, bw * 0.35f), ld = Mathf.Min(11.5f, bh * 0.32f);
            float bx0, by0, bx1, by1;
            switch (lm.side)
            {
                case 'W': bx0 = x0; bx1 = x0 + ld; by0 = y0 + (bh - lw) * 0.5f; by1 = by0 + lw; break;
                case 'E': bx1 = x1; bx0 = x1 - ld; by0 = y0 + (bh - lw) * 0.5f; by1 = by0 + lw; break;
                case 'N': by1 = y1; by0 = y1 - ld; bx0 = x0 + (bw - lw) * 0.5f; bx1 = bx0 + lw; break;
                default:  by0 = y0; by1 = y0 + ld; bx0 = x0 + (bw - lw) * 0.5f; bx1 = bx0 + lw; break;
            }
            float dAt = (lm.side == 'S' || lm.side == 'N') ? (bx0 + bx1) * 0.5f - 1f : (by0 + by1) * 0.5f - 1f;
            n += BandBuilding(m, lm.id, bx0, by0, bx1, by1, lm.side, dAt, lm.scene, true);
            n += LandmarkTrim(m, lm, bx0, by0, bx1, by1, lm.side, dAt);
        }

        // ── 무너지고 남은 벽 조각 2개 — ㄱ자 형태로 남아 '건물이었던 것'임을 알린다.
        for (int i = 0; i < 2; i++)
        {
            uint h = (uint)H(seed * 7 + i * 31, 3);
            float fx = x0 + 4f + (bw - 20f) * ((h % 100u) / 100f);
            float fy = y0 + 4f + (bh - 20f) * (((h >> 7) % 100u) / 100f);
            float armA = 7f + (h % 6u), armB = 6f + ((h >> 3) % 6u);
            if (IsIndoors(fx, fy) || NearKeepOut(fx, fy, 5f)) continue;
            n += FenceSeg(m, $"{p}_frag{i}a", fx, fy, true,  0f, armA, 1.1f);
            n += FenceSeg(m, $"{p}_frag{i}b", fx, fy, false, 0f, armB, 1.1f);
        }

        // ── 잔해 더미 — 흔든 격자 위에. 사이가 곧 길이라 통로가 저절로 구불거린다.
        var rnd = new System.Random(seed ^ 0x3D0F);
        for (float gy = y0 + 4f; gy < y1 - 3f; gy += 7f)
        for (float gx = x0 + 4f; gx < x1 - 3f; gx += 7.5f)
        {
            uint h = (uint)H(seed + (int)gx * 13, (int)gy);
            if (h % 5u == 0u) continue;                                  // 빈칸 = 넓게 트인 길
            float cx = gx + ((h >> 3) % 30u) * 0.1f - 1.5f;
            float cy = gy + ((h >> 9) % 30u) * 0.1f - 1.5f;
            float w = 2.6f + ((h >> 14) % 40u) * 0.11f;                  // 2.6~7.0
            float d = 2.2f + ((h >> 20) % 40u) * 0.10f;
            if (IsIndoors(cx, cy) || NearKeepOut(cx, cy, 4f)) continue;
            if (cx - w * 0.5f < x0 || cx + w * 0.5f > x1) continue;
            if (cy - d * 0.5f < y0 || cy + d * 0.5f > y1) continue;
            n += GreyboxBuild.Wall(m, $"{p}_rb{(int)gx}_{(int)gy}", cx, cy, w, d);
            MarkBuilding(cx - w * 0.5f, cy - d * 0.5f, cx + w * 0.5f, cy + d * 0.5f);
        }

        // 파묻힌 차 몇 대 + 루트. 폐허는 위험한 만큼 두둑해야 들어갈 이유가 생긴다.
        for (int i = 0; i < 4; i++)
            if (CourtSpot(rnd, x0, y0, x1, y1, 3f, out float px, out float py))
            {
                n += GreyboxBuild.Car(m, $"{p}_wr{i}", px, py, 4.4f, 1.9f);
                MarkBuilding(px - 2.2f, py - 0.95f, px + 2.2f, py + 0.95f);
            }
        if (!string.IsNullOrEmpty(yardLabel))
        {
            if (CourtSpot(rnd, x0, y0, x1, y1, 3f, out float nx, out float ny))
            {
                n += GreyboxBuild.Note(m, $"{p}_Yard", nx, ny, yardLabel, yardNote ?? yardLabel);
                KeepOut(nx, ny);
            }
        }
        for (int i = 0; i < 5; i++)
            if (CourtSpot(rnd, x0, y0, x1, y1, 2.5f, out float px, out float py))
                n += CrateAnchor(m, $"{p}_rc{i}", px, py);

        n += EnemyZone(m, $"EZ_{p}_field", (x0 + x1) * 0.5f, (y0 + y1) * 0.5f,
                       Mathf.Min(bw - 6f, 14f), Mathf.Min(bh - 6f, 14f), "bandit_melee_1", 2);
        n += EnemyZone(m, $"EZ_{p}_fieldT", x0 + bw * 0.7f, y0 + bh * 0.35f, 8f, 8f, "bandit_tank", 1);
        return n;
    }

    /// <summary>노점 골목 — 3~5m 좌판이 통로 양쪽에 촘촘히. 맵에서 **가장 잔 텍스처**다.
    /// 좌판은 들어가는 건물이 아니라 **뒤지는 물건**이라, 여기만 걷는 속도가 확 느려진다.</summary>
    static int StallAlley(GameObject m, string p, float x0, float y0, float x1, float y1, int seed)
    {
        float bw = x1 - x0, bh = y1 - y0;
        if (bw < 6f || bh < 10f) return RowTerrace(m, p, x0, y0, x1, y1, seed);

        bool vertical = bh >= bw;                       // 통로가 뻗는 방향
        float across = vertical ? bw : bh;              // 통로에 수직인 폭
        float along = vertical ? bh : bw;
        const float Aisle = 2.0f;                       // 좌판 사이 통로(맵에서 가장 좁다)
        float rowD = (across - Aisle) * 0.5f;
        if (rowD < 2.2f) return RowTerrace(m, p, x0, y0, x1, y1, seed);

        // 좌판 칸 경계는 **양쪽 줄이 공유**한다. 그래야 4칸마다 비우는 자리가 통로로 맞물려
        //   가운데 통로가 밖과 이어진다. (칸을 줄마다 따로 잡았더니 두 줄이 벽이 되고
        //   통로 양 끝은 아케이드 외벽이라 **들어갈 수 없는 복도**가 됐다 — 시뮬레이션이 잡음.)
        var st = new System.Collections.Generic.List<float>();
        var ln = new System.Collections.Generic.List<float>();
        {
            float a = (vertical ? y0 : x0) + 0.8f;
            float aEnd = (vertical ? y1 : x1) - 0.8f;
            for (int i = 0; a < aEnd - 2.5f; i++)
            {
                float len = 3.0f + ((uint)H(seed * 23, i * 7 + 3) % 22u) * 0.1f;   // 3.0~5.1m
                if (a + len > aEnd) len = aEnd - a;
                if (len < 2.2f) break;
                st.Add(a); ln.Add(len);
                a += len + 0.9f;                                                   // 좌판 사이 실틈
            }
        }

        int n = 0;
        for (int side = 0; side < 2; side++)
        {
            float d0 = side == 0 ? (vertical ? x0 : y0) : (vertical ? x0 + rowD + Aisle : y0 + rowD + Aisle);
            float d1 = d0 + rowD;
            for (int i = 0; i < st.Count; i++)
            {
                if (i % 4 == 3) continue;                        // ★ 가로지르는 통로 — 양쪽 줄이 같이 비운다
                uint h = (uint)H(seed * 23 + side * 101, i * 7 + 3);
                if ((h >> 11) % 100u < 10u) continue;            // 비어 있는 자리(장사 접은 칸)

                float dep = d1 - (((h >> 5) % 100u < 35u) ? 0.7f : 0f);   // 좌판 깊이도 흔들린다
                string name = $"{p}_st{side}{i}";
                float bx0 = vertical ? d0 : st[i], by0 = vertical ? st[i] : d0;
                float bx1 = vertical ? dep : st[i] + ln[i], by1 = vertical ? st[i] + ln[i] : dep;
                if (IsIndoors((bx0 + bx1) * 0.5f, (by0 + by1) * 0.5f)) continue;

                n += BandMass(m, name, bx0, by0, bx1, by1);
                // 좌판 4개 중 1개는 뒤질 수 있다 — 골목을 끝까지 훑을 이유.
                if (i % 4 == side) n += Searchable(m, name, "좌판", "좌판 뒤지기", 2, 2, Mathf.Max(ln[i], rowD));
            }
        }
        return n;
    }

    /// <summary>이미 세운 오브젝트를 **뒤질 수 있는 것**으로 만든다(별도 상자를 옆에 놓지 않는다).
    /// 예산제(MapSpawnController)가 채우도록 ItemSpawnPoint(Container)를 링크해 둔다.</summary>
    static int Searchable(GameObject m, string objName, string label, string prompt, int w, int h, float size)
    {
        var t = FindChild(m.transform, objName);
        if (t == null) return 0;
        var go = t.gameObject;

        var lc = go.GetComponent<LootContainer>();
        if (lc == null) lc = go.AddComponent<LootContainer>();   // ??는 Unity 가짜 null을 통과시켜 못 씀
        lc.Setup(label, w, h);

        var io = go.GetComponent<InteractableObject>();
        if (io == null) io = go.AddComponent<InteractableObject>();
        io.Configure(InteractableObject.InteractType.Container, prompt, size * 0.5f + 1.6f);

        var sp = go.GetComponent<ItemSpawnPoint>();
        if (sp == null) sp = go.AddComponent<ItemSpawnPoint>();
        SetSpawnType(sp, 1);   // Container
        var so = new SerializedObject(sp);
        var lk = so.FindProperty("linkedContainer");
        if (lk != null) { lk.objectReferenceValue = lc; so.ApplyModifiedPropertiesWithoutUndo(); }
        return 1;
    }

    /// <summary>합이 1인 가중치 — 필지 크기를 흔들어 균일 격자처럼 보이지 않게 한다.</summary>
    static float[] LotWeights(int count, int seed)
    {
        var w = new float[count];
        float sum = 0f;
        for (int i = 0; i < count; i++) { w[i] = 0.78f + (H(seed, i) % 100) * 0.0045f; sum += w[i]; }
        for (int i = 0; i < count; i++) w[i] /= sum;
        return w;
    }

    /// <summary>랜드마크 블록 — 랜드마크가 **띠의 한 변을 통째로** 차지하고(길을 면한 변),
    /// 나머지 세 변은 보통 건물, 안쪽은 그 시설의 **뒷마당(안뜰)** 이 된다.
    ///
    /// 2026-07-28 재작성. 예전엔 블록의 58%를 통짜로 먹고 남은 자리에 점포 격자를 깔았다 —
    ///   "큰 덩어리 + 큰 공백"이라 랜드마크가 **크기로만** 존재했고, 아트 리소스 상한(13m)과도
    ///   계속 싸웠다. 이제 크기가 아니라 **길을 면한 한 변 전체 + 이름표 + 전용 내부 씬 + 뒷마당**이
    ///   랜드마크를 만든다(확정 설계 ③ "유니크 건물도 블록의 한 조각으로 편입").</summary>
    static int LandmarkBlock(GameObject m, string id, float x0, float y0, float x1, float y1, char side,
                             string scene, string label, string note, int enemies,
                             int courtKind, string yardLabel, string yardNote)
    {
        var spot = new BandSpot
        {
            side = side, wholeSide = true, id = id, label = label, note = note,
            scene = scene, enemies = enemies, enemyKey = "bandit_melee_1"
        };
        return PerimeterBlock(m, id, x0, y0, x1, y1, H((int)x0 + 3, (int)y0 + 9),
                              new[] { spot }, courtKind, -1, yardLabel, yardNote);
    }

    /// <summary>C1R0 = 폐아파트 **판상 단지**(둘레형 아님). 긴 동 3개가 나란히, 사이가 단지 안길.
    /// 동측 동이 간선을 면하고 정문을 갖는다.</summary>
    static int BuildAptBlock(GameObject m, float x0, float y0, float x1, float y1)
        => SlabRows(m, "Apt", x0, y0, x1, y1, H((int)x0 + 3, (int)y0 + 9),
                    new BandSpot
                    {
                        side = 'E', id = "Apt", label = "폐아파트 ★★",
                        note = "세로 간선 西. key_apt_admin → 펜트 key_tower_card. 수직 다층 후속.",
                        enemies = 2, enemyKey = "bandit_melee_1",
                    },
                    "아파트 단지 안길 ★★",
                    "동과 동 사이. 주민들이 버리고 간 차가 그대로다. 앞뒤로밖에 못 움직인다.");

    /// <summary>C2R0 = 유리타워 블록. 서측 띠 전체가 타워 단지(문 = 세로 간선), 안쪽은 하역장.</summary>
    static int BuildTowerBlock(GameObject m, float x0, float y0, float x1, float y1)
        => LandmarkBlock(m, "Tower", x0, y0, x1, y1, 'W', null,
                         "유리타워 ★★★★",
                         "동측. key_tower_card로 상층 R&D → key_dome_code. 카드키·수직 후속.",
                         3, 0,
                         "타워 하역장 ★★★",
                         "적재 팔레트가 그대로 남았다. 사방이 막혀 있어 여기서 붙으면 피할 데가 없다.");

    /// <summary>C2R2 = **붕괴장**(둘레형 아님). 블록 전체가 잔해밭이고, 살아남은 한 채가
    /// 가로 간선을 면한다 — 그 안이 `Int_CollapsedMall`.</summary>
    static int BuildCollapsedMall(GameObject m, float x0, float y0, float x1, float y1)
        => RubbleField(m, "CollapsedMall", x0, y0, x1, y1, H((int)x0 + 3, (int)y0 + 9),
                       new BandSpot
                       {
                           side = 'S', id = "CollapsedMall", label = "무너진 상가 ★★★",
                           scene = "Int_CollapsedMall",
                           note = "SQ-001 갇힌 생존자. 잔해 미로 최심부. 문 = 가로 간선(南).",
                           enemies = 2, enemyKey = "bandit_melee_1",
                       },
                       "상가 붕괴장 ★★★",
                       "상층이 통째로 무너져 내렸다. 길이 정해져 있지 않고, 어디로도 곧게 가지 못한다.");

    /// <summary>C0R2 = 공원 안뜰 블록(분식집이 남측 띠에 편입).</summary>
    static int BuildParkBlock(GameObject m, float x0, float y0, float x1, float y1)
    {
        var spots = new[]
        {
            new BandSpot { side = 'S', at = 0.55f, id = "Diner", label = "분식집 ★",
                           scene = "Int_Diner", enemies = 0,
                           note = "캔푸드·물. 주방 뒤 창고. 공원 옆이라 조용하다." },
        };
        return PerimeterBlock(m, "Park", x0, y0, x1, y1, H((int)x0 + 5, (int)y1), spots, 4, 0,
                              "공원 안뜰 ★",
                              "블록 한가운데 남은 공원. 조용한 대신 가져갈 것도 적다.");
    }

    /// <summary>C2R1 = **울타리 야적장**(둘레형 아님). 서측 한 변만 건물(경찰서 — 문 = 세로 간선)이고
    /// 나머지 세 면은 담장이다. 밖에서 안이 다 보이지만 들어가는 문은 둘뿐.</summary>
    static int BuildLotBlock(GameObject m, float x0, float y0, float x1, float y1)
        => FencedYard(m, "Lot", x0, y0, x1, y1, H((int)x0 + 5, (int)y1),
                      new BandSpot
                      {
                          side = 'W', at = 0.5f, id = "Police", label = "경찰서 ★★★★",
                          scene = "Int_Police", enemies = 3, enemyKey = "bandit_melee_1",
                          note = "무기고 뒷문은 잠겨 있다(열쇠). 로비 압수품 대장에 **보석상 금고 번호**가 적혀 있다.",
                      },
                      "경찰 압수 야적장 ★★",
                      "컨테이너와 압수 차량이 줄지어 있다. 담장 너머로 다 보이는 만큼, 안에서도 다 보인다.");

    /// <summary>**문 = 입구.** gb_door 하나가 표시이자 진입 트리거(BuildingEntrance)다.
    /// 밟으면 내부 씬으로 전환(페이드+캐릭터 유지). 복귀 스폰은 Zone1의 from_&lt;건물&gt;.</summary>
    static int Enter(GameObject m, string name, float x, float y, string targetScene, string spawnId = "default",
                     float tw = 2f, float th = 1f)
    {
        // 3D — 건물은 같은 맵에서 걸어 들어간다. 씬 전환 문을 세우지 않는다.
        // (2026-09-08 결정: 별도 실내 씬 폐기)
        if (GreyboxBuild.Use3D) return 0;
        if (GreyboxBuild.Marker(m, "gb_door", name, x, y) == 0) return 0;
        KeepOut(x, y);   // 건물 입구 앞도 비워 둔다(문이 잔해로 막히면 못 들어간다)
        var t = FindChild(m.transform, name);
        if (t == null) return 0;
        var go = t.gameObject;

        // **문 = 표시 + 상호작용 진입**. E로만 들어간다(밟기 아님) → 지나가다 실수로 안 들어간다.
        var io = go.GetComponentInChildren<InteractableObject>();
        if (io == null) io = go.AddComponent<InteractableObject>();
        io.Configure(InteractableObject.InteractType.Door, "들어가기", 2.0f);

        // 진입 몸통. 2D는 BoxCollider2D(trigger) + BuildingEntrance,
        // 3D는 BoxCollider + InteractableObject.ExitPoint로 씬 전환을 직접 건다
        // (BuildingEntrance는 BoxCollider2D 전제라 3D 맵에서 동작하지 않는다).
        if (GreyboxBuild.Use3D)
        {
            var box3 = go.GetComponent<BoxCollider>();
            if (box3 == null) box3 = go.AddComponent<BoxCollider>();
            box3.isTrigger = false;                       // 문은 막는 몸 — E로 통과한다
            box3.size = new Vector3(tw, 2.4f, th);
            box3.center = new Vector3(0f, 1.2f, 0f);

            io.Configure(InteractableObject.InteractType.ExitPoint, "들어가기", 2.0f);
            var so3 = new SerializedObject(io);
            var ts3 = so3.FindProperty("targetScene");   if (ts3 != null) ts3.stringValue = targetScene;
            var sp3 = so3.FindProperty("spawnPointId");  if (sp3 != null) sp3.stringValue = spawnId;
            so3.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            var box = go.GetComponent<BoxCollider2D>();
            if (box == null) box = go.AddComponent<BoxCollider2D>();   // ??는 Unity 가짜 null을 통과시켜 못 씀
            box.isTrigger = true;
            box.size = new Vector2(tw, th);

            var be = go.GetComponent<BuildingEntrance>();
            if (be == null) be = go.AddComponent<BuildingEntrance>();
            be.Configure(targetScene, spawnId, false, new Vector2(tw, th));
            be.SetRequireInteract(true);
        }

        // 문 앞 DoorController(팔레트 기본)는 진입과 이중이라 제거 — 문 하나가 한 가지 일만 하게.
        var dc = go.GetComponent<DoorController>();
        if (dc != null) Object.DestroyImmediate(dc);
        return 1;
    }

    /// <summary>**문 = 입구.** 정면선(facade) 한가운데에 놓이는 문의 중심·크기.
    ///
    /// 2026-07-11 (사용자: "입구랑 문은 왜 따로임? 그냥 문 = 입구면 되는 거 아닌가"):
    /// 예전엔 벽에 '문 표시'(gb_door)를 두고 그 앞에 '입구 발판'(gb_enter)을 따로 깔았다 —
    /// 한 개념에 오브젝트 2개라 화면만 지저분했다. 이제 **문 자체가 트리거**다.
    /// 문을 정면선 위에 두고 두께 1.4m를 주면 절반이 길 쪽으로 나오므로 밟을 수 있다
    /// (나머지 절반은 솔리드 안이지만 트리거라 무해).</summary>
    static void DoorPad(float x0, float y0, float x1, float y1, char side, float doorAt,
                        out float ex, out float ey, out float tw, out float th)
    {
        const float gap = 2.2f, depth = 1.4f;
        switch (side)
        {
            case 'S': ex = doorAt + 1f; ey = y0; tw = gap;   th = depth; break;
            case 'N': ex = doorAt + 1f; ey = y1; tw = gap;   th = depth; break;
            case 'W': ex = x0; ey = doorAt + 1f; tw = depth; th = gap;   break;
            default:  ex = x1; ey = doorAt + 1f; tw = depth; th = gap;   break;   // 'E'
        }
    }

    /// <summary>**속이 찬** 건물 한 채 — 덩어리 + 정면 문 표시 + 그 앞 진입 발판(+선택: 전용 내부 씬).
    ///
    /// 2026-07-11 (사용자: "중간중간 빈 공간이 너무 커, 도로도 아닌 게"):
    /// 건물을 속 빈 링으로 그리면 **위에서 내부가 다 보여 거대한 공백**이 된다. 그런데 이 게임의
    /// 건물은 문에서 곧바로 내부 씬으로 전환하므로 **껍데기 안쪽은 플레이어가 영영 못 가는 죽은 땅**이다.
    /// → 통째로 채운다. 공백이 사라지고 도시의 '살'이 생기며, 음영은 도로·골목만 남는다.</summary>
    static int SolidBuilding(GameObject m, string name, float x0, float y0, float x1, float y1,
                             char side, float doorAt, string scene = null, string returnSpawn = null)
    {
        // ★ 리소스 제약(2026-07-11 사용자: "우리 리소스 중에 너무 큰 건물은 불가능하거든, 없애자.
        //   랜드마크지만 자잘하게 갈 수도 있잖아") — 한 채가 MaxSpan을 넘으면 **단지로 쪼갠다**.
        //   랜드마크의 정체성은 '큰 덩어리'가 아니라 라벨·전용 내부 씬·주변 위험도가 만든다.
        if (x1 - x0 > MaxSpan || y1 - y0 > MaxSpan)
            return SolidCluster(m, name, x0, y0, x1, y1, side, doorAt, scene, returnSpawn);

        // ── 3D: 걸어 들어가는 방 ──────────────────────────────────────
        // 별도 실내 씬으로 넘기지 않고 **같은 맵 안에서** 들어간다(2026-09-08 결정).
        // 발자국이 너무 작으면 사람이 낄 상자라 방으로 만들지 않고 막힌 덩어리로 둔다.
        if (GreyboxBuild.Use3D)
        {
            MarkBuilding(x0, y0, x1, y1);
            if (Greybox3D.CanBeRoom(x0, y0, x1, y1))
            {
                DoorPad(x0, y0, x1, y1, side, doorAt, out float rex, out float rey, out float rtw, out float rth);
                // 문 갭 시작 좌표 = 문 면을 따라가는 축의 값에서 폭의 절반을 뺀 것.
                float gapAt = (side == 'S' || side == 'N') ? rex - 1.2f : rey - 1.2f;
                return Greybox3D.Room(m, name, x0, y0, x1, y1, side, gapAt);
            }
            return GreyboxBuild.Wall(m, name, (x0 + x1) * 0.5f, (y0 + y1) * 0.5f, x1 - x0, y1 - y0);
        }

        int n = GreyboxBuild.Wall(m, name, (x0 + x1) * 0.5f, (y0 + y1) * 0.5f, x1 - x0, y1 - y0);
        MarkBuilding(x0, y0, x1, y1);
        if (string.IsNullOrEmpty(scene) && !EnterableHere(name)) return n;   // 문 없는 덩어리

        // **문 = 입구.** 오브젝트 하나(gb_door)가 표시이자 트리거다.
        DoorPad(x0, y0, x1, y1, side, doorAt, out float ex, out float ey, out float tw, out float th);
        if (string.IsNullOrEmpty(scene))
            n += EnterGeneric(m, $"{name}_Door", ex, ey, tw, th);
        else
        {
            n += Enter(m, $"{name}_Door", ex, ey, scene, "default", tw, th);
            if (!string.IsNullOrEmpty(returnSpawn))
            {
                float rx = ex + (side == 'W' ? -2.4f : side == 'E' ? 2.4f : 0f);
                float ry = ey + (side == 'S' ? -2.4f : side == 'N' ? 2.4f : 0f);
                // 복귀 스폰 마커는 두지 않는다 — BuildingReturn이 **들어온 문 앞**으로 되돌린다(2026-07-11).
            }
        }
        return n;
    }

    /// <summary>큰 건물을 **단지(여러 채)** 로 쪼갠다 — 외부 대형 건물 리소스를 못 구하기 때문.
    /// 조각 사이는 1.4m 실개틈(사람 하나 지날 폭)이라 단지 안쪽도 걸어 다닐 수 있다.
    /// 문·전용 내부 씬은 **도로를 면한 조각 하나**가 갖는다.</summary>
    static int SolidCluster(GameObject m, string name, float x0, float y0, float x1, float y1,
                            char side, float doorAt, string scene, string returnSpawn)
    {
        const float Gap = 1.4f;
        int cols = Mathf.Max(1, Mathf.CeilToInt((x1 - x0) / MaxSpan));
        int rows = Mathf.Max(1, Mathf.CeilToInt((y1 - y0) / MaxSpan));
        float pw = ((x1 - x0) - Gap * (cols - 1)) / cols;
        float pd = ((y1 - y0) - Gap * (rows - 1)) / rows;

        // 문을 가질 조각 = side가 향하는 가장자리 + doorAt이 속한 열/행.
        int di = side == 'S' ? 0 : side == 'N' ? rows - 1
               : Mathf.Clamp(Mathf.FloorToInt((doorAt - y0) / (pd + Gap)), 0, rows - 1);
        int dj = side == 'W' ? 0 : side == 'E' ? cols - 1
               : Mathf.Clamp(Mathf.FloorToInt((doorAt - x0) / (pw + Gap)), 0, cols - 1);

        int n = 0;
        for (int i = 0; i < rows; i++)
        for (int j = 0; j < cols; j++)
        {
            float px = x0 + j * (pw + Gap), py = y0 + i * (pd + Gap);
            string pn = $"{name}_{i}{j}";
            if (i == di && j == dj)
            {
                float da = (side == 'S' || side == 'N') ? px + Mathf.Max(1f, pw * 0.5f - 1f)
                                                        : py + Mathf.Max(1f, pd * 0.5f - 1f);
                n += SolidBuilding(m, pn, px, py, px + pw, py + pd, side, da, scene, returnSpawn);
            }
            else
            {
                n += GreyboxBuild.Wall(m, pn, px + pw * 0.5f, py + pd * 0.5f, pw, pd);
                MarkBuilding(px, py, px + pw, py + pd);
            }
        }
        return n;
    }

    // (구 SolidMass 폐기 — 2026-07-28. 유일한 호출자였던 Shops가 사라졌다.
    //  띠 조각은 조각 사이에 틈이 없어야 하므로 BandMass가 대신한다.)

    /// <summary>무너진 필지 — 온전한 사각형 대신 **잔해 덩어리 2~4개**를 흩어 놓는다.
    ///
    /// (2026-07-11 사용자: "너무 계획도시보단 그래도 정부가 약간 무너진 느낌이잖아")
    /// 격자를 깨는 가장 정직한 수단이다. 사각형 실루엣이 무너지고, 덩어리 사이로
    /// **걸어 들어갈 수 있는 틈**이 생겨 탐험 거리가 늘어난다.</summary>
    static int RuinLot(GameObject m, string name, float x0, float y0, float x1, float y1, uint h)
    {
        int n = 0;
        int chunks = 2 + (int)(h % 3u);           // 2~4 덩어리
        float w = x1 - x0, d = y1 - y0;
        for (int k = 0; k < chunks; k++)
        {
            uint hk = (uint)H((int)(h & 0xFFFF) + k * 61, k * 17 + 3);
            float cw = w * (0.32f + (hk % 30u) * 0.012f);       // 필지의 32~68%
            float cd = d * (0.30f + ((hk >> 5) % 30u) * 0.012f);
            float cx = x0 + cw * 0.5f + (w - cw) * (((hk >> 10) % 100u) / 100f);
            float cy = y0 + cd * 0.5f + (d - cd) * (((hk >> 17) % 100u) / 100f);
            n += GreyboxBuild.Wall(m, $"{name}_r{k}", cx, cy, cw, cd);
            MarkBuilding(cx - cw * 0.5f, cy - cd * 0.5f, cx + cw * 0.5f, cy + cd * 0.5f);
        }
        return n;
    }

    /// <summary>이름 해시로 '들어갈 수 있는 집'인지 결정 — GameTuning.buildingEnterRatio와 같은 규칙.</summary>
    static bool EnterableHere(string name)
    {
        float ratio = GameTuning.Instance != null ? GameTuning.Instance.buildingEnterRatio : 1f;
        if (ratio >= 1f) return true;
        if (ratio <= 0f) return false;
        uint h = 2166136261u;
        for (int i = 0; i < name.Length; i++) { h ^= name[i]; h *= 16777619u; }
        return (h % 1000u) / 1000f < ratio;
    }

    /// <summary>공용 내부(Int_Generic)로 들어가는 진입 트리거. 복귀는 `__back__`(들어온 문 앞).
    /// 전용 내부가 만들어진 건물은 Enter()로 개별 지정하고, 나머지 절차 생성 건물이 이걸 쓴다.
    ///
    /// **진입 가능 비율은 `GameTuning.buildingEnterRatio` 노브**(1=전부, 0.5=절반).
    /// "건물을 더 열지"는 QA 플레이 결과로 판단 — 값만 바꾸고 이 빌더를 다시 돌리면 반영된다.
    /// 선택은 이름 해시 기반이라 **결정론적**(같은 값이면 같은 건물이 열림).</summary>
    static int EnterGeneric(GameObject m, string name, float x, float y, float tw = 2f, float th = 1f)
    {
        if (GreyboxBuild.Use3D) return 0;   // 3D — 씬 전환 문 없음(같은 맵에서 들어간다)
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

    // (구 PlazaBlock/Plaza/PlazaCrate 폐기 — 2026-07-28.
    //  개활지를 "블록의 앞 38%"로 잘라 놓으면 그건 결국 길에 면한 **빈 땅**이었다.
    //  지금은 개활감을 **안뜰**이 준다: 둘레는 건물이 두르고 그 안이 공원/주차장이다.
    //  트여 있지만 에워싸여 있어, 들어가는 순간 '장소'가 된다. → BuildParkBlock / BuildLotBlock)

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
            // ★ 차량은 **실제 차 비율**로. 2026-07-11 사용자 지적: "차량은 크기 일정하게, 정사각형은 아닌 것 같다".
            //   구: 두께를 도로 폭의 75~99%로 잡아서 4~7m 길이에 5~6.5m 두께 → **거의 정사각형**이었다.
            //   신: 차종별 고정 치수(길이 × 폭). 폭은 항상 2m 남짓 = 한눈에 '차'로 읽힌다.
            int kind = (int)((h >> 1) % 3);                        // 0 승용 / 1 트럭 / 2 버스
            float carLen = kind == 0 ? 4.4f : kind == 1 ? 6.8f : 9.4f;
            float carWid = kind == 0 ? 1.9f : kind == 1 ? 2.3f : 2.6f;

            // 30%는 **사고 차량** — 도로를 가로질러 누워 실제 병목을 만든다(지그재그의 핵심).
            bool crashed = ((h >> 6) % 10u) < 3u;
            float len   = crashed ? carWid : carLen;               // 도로 진행축 길이
            float thick = crashed ? Mathf.Min(carLen, maxBlock) : carWid;   // 도로 폭축
            if (len > (b1 - t)) len = b1 - t;
            if (len < 1.5f) break;

            float ca = left ? a0 + thick * 0.5f : a1 - thick * 0.5f;   // 도로 한쪽 갓길에 붙임
            float cb = t + len * 0.5f;
            // 스폰·탈출·건물 입구 근처는 건너뛴다 — 거기 잔해가 깔리면 스폰 즉시 끼거나 문이 막힌다.
            float cx0 = vertical ? ca : cb, cy0 = vertical ? cb : ca;
            if (NearKeepOut(cx0, cy0, len * 0.5f + 3.5f)) continue;
            string name = $"{p}_{i}";
            // 세로 도로면 장애물의 '길이'가 y축, 두께가 x축.
            // 차량(파랑) — **막힘(주황)과 색을 분리**했다. 주황은 인터랙션이 있는 것만.
            n += vertical ? GreyboxBuild.Car(m, name, ca, cb, thick, len)
                          : GreyboxBuild.Car(m, name, cb, ca, len, thick);
            // 잔해 자리를 등록 → 루트 앵커가 잔해 속에 박히지 않고, 오히려 **잔해에 붙어** 생긴다.
            float hw = (vertical ? thick : len) * 0.5f, hh = (vertical ? len : thick) * 0.5f;
            MarkBuilding(cx0 - hw, cy0 - hh, cx0 + hw, cy0 + hh);

            // 3대 중 1대는 **트렁크가 열린 차** — 도로 파밍을 바닥에 뿌리는 대신 잔해에 붙인다.
            //   (사용자: "도로에 부서진 자동차 트렁크나 상자 같은 거 … 유기적인 방향으로")
            if (i % 3 == 1) n += Trunk(m, name, Mathf.Max(len, thick));
        }
        return n;
    }

    /// <summary>C1R1 = **상점가 안뜰 블록**. 네 가게(보석상·컴퓨터가게·철물점·세탁소)의 문이
    /// 전부 **안뜰**을 향한다 — 길에서 보면 그냥 벽이고, 아치를 지나야 비로소 상점가가 열린다.
    ///
    /// 2026-07-28 재작성. 예전엔 블록 한가운데에 십자 골목을 내고 4채를 마주 세웠는데,
    ///   그건 "블록 안에 길을 두지 않는다"는 규칙과 정면으로 어긋났고, 남는 자리는 또 점포 격자였다.
    ///   지금은 네 채가 곧 띠의 네 조각이다. 들어가는 길이 둘뿐이라 안에서 붙으면 갇힌 싸움이 된다.</summary>
    static int BuildUniqueRow(GameObject m)
    {
        var spots = new[]
        {
            new BandSpot { side = 'S', at = 0.5f, inward = true, id = "Jewelry", label = "보석상 ★★★★",
                           scene = "Int_Jewelry", enemies = 2,
                           note = "금고는 **비밀번호**. 번호는 다른 데서 알아내야 한다(경찰 압수품 대장). 최고가 루트." },
            new BandSpot { side = 'E', at = 0.5f, inward = true, id = "Electronics", label = "컴퓨터가게 ★★",
                           scene = "Int_Electronics", enemies = 2,
                           note = "배터리·전선·전자부품. 라디오/발전기 업그레이드 재료." },
            new BandSpot { side = 'N', at = 0.5f, inward = true, id = "Hardware", label = "철물점 ★★",
                           scene = "Int_Hardware", enemies = 1,
                           note = "공구·부품·못. 제작·수리 재료." },
            new BandSpot { side = 'W', at = 0.5f, inward = true, id = "Laundry", label = "세탁소 ★",
                           scene = "Int_Laundry", enemies = 0,
                           note = "천·의류. 방한·붕대 재료." },
        };
        return PerimeterBlock(m, "UR", 66f, 78f, 102f, 114f, H(311, 5), spots, 0, 2,
                              "상점가 안뜰 ★★★",
                              "네 가게의 문이 전부 이 안뜰을 향한다. 들어오는 길은 아치 둘뿐 — 나가는 길도 둘뿐이다.",
                              2);
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
        int n = SolidBuilding(m, id, x0, y0, x1, y1, side, doorAt, scene, returnSpawn);
        DoorPad(x0, y0, x1, y1, side, doorAt, out float ex, out float ey, out _, out _);
        float rx = ex + (side == 'W' ? -2.2f : side == 'E' ? 2.2f : 0f);
        float ry = ey + (side == 'S' ? -2.2f : side == 'N' ? 2.2f : 0f);
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

        // 솔리드 콜라이더(통행 차단). 2D는 BoxCollider2D, 3D는 BoxCollider다.
        // ⚠️ 3D 상자에 2D 콜라이더를 붙이면 AddComponent가 null을 돌려주고 바로 다음 줄에서
        //    NullReference가 난다(실내 빌더에서도 같은 자리에 걸렸다).
        if (GreyboxBuild.Use3D)
        {
            var box3 = go.GetComponent<BoxCollider>();
            if (box3 == null) box3 = go.AddComponent<BoxCollider>();
            box3.isTrigger = false;
            box3.size = Vector3.one;   // 부모 스케일(w, 높이, h)이 곱해진다
        }
        else
        {
            var box = go.GetComponent<BoxCollider2D>();
            if (box == null) box = go.AddComponent<BoxCollider2D>();   // ??는 Unity 가짜 null을 통과시켜 못 씀
            box.isTrigger = false;
            box.size = Vector2.one;   // 부모 스케일(w,h)이 곱해진다
        }

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
