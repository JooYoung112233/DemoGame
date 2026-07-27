#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// "지역1(Zone1)" — **폐상가 도심 그레이박스**(좀보이드式). (docs/world-map.md §7-5)
///   • 똑같은 격자 = 거부 → **블록마다 성격 다르게**: 작은 점포(폭·깊이 제각각+공터) / 큰 건물(다실 분할 = 백화점·주거동·돔) / 광장(주차장).
///   • 간선도로 격자(세로 x64/120/200/276, 가로 y44~55 차고큰길·76·168·256·324, ~6~11m)로 블록 분할. 튜토는 SW.
///   • 5 랜드마크 = 큰 건물 + 간선 교차점 열쇠/라벨/적/탈출 마커(밀도·위치 검토용; 내부 정밀배치 후속).
///   • 좌표 = 월드 XY, 1u=1m.
/// 메뉴: Tools ▸ TopDown ▸ 맵 ▸ 1구역 그레이박스
/// </summary>
public static class Zone1GreyboxLayout
{
    const string ScenePath = "Assets/Scenes/Zone1.unity";
    const float FX0 = 14f, FY0 = 14f, FX1 = 330f, FY1 = 344f;
    const float TUT_OX = 20f, TUT_OY = 20f;

    static readonly float[] CX0 = { 16f, 70f, 126f, 206f, 282f };
    static readonly float[] CX1 = { 64f, 120f, 200f, 276f, 328f };
    static readonly float[] RY0 = { 16f, 55f, 82f, 176f, 264f, 332f };
    static readonly float[] RY1 = { 44f, 76f, 168f, 256f, 324f, 342f };

    // 점포 크기 풀(제각각).
    static readonly float[] WS = { 8f, 10f, 12f, 15f, 18f, 23f };
    static readonly float[] DS = { 9f, 11f, 13f, 16f, 20f };

    [MenuItem("Tools/TopDown/빌드/지역1", priority = -98)]
    public static void Build()
    {
        var map = GreyboxBuild.BeginScene(out var scene);
        int n = 0;

        n += GreyboxBuild.Floor(map, "Floor", (FX0+FX1)*0.5f, (FY0+FY1)*0.5f, FX1-FX0, FY1-FY0);
        n += GreyboxBuild.WallSeg(map, "Edge_S", FX0, FY0, FX1, FY0+2f);
        n += GreyboxBuild.WallSeg(map, "Edge_N_HanRiver", FX0, FY1-2f, FX1, FY1);
        n += GreyboxBuild.WallSeg(map, "Edge_W", FX0, FY0, FX0+2f, FY1);
        n += GreyboxBuild.WallSeg(map, "Edge_E", FX1-2f, FY0, FX1, FY1);

        // ① 튜토(SW) + 차고 동측 통로 → 세로 간선(x64~70)으로 빠짐.
        n += ScrapMarketGreyboxLayout.Place(map, TUT_OX, TUT_OY, true);

        // ── 블록마다 성격 다르게 채움 ──
        for (int r = 0; r < RY0.Length; r++)
        for (int c = 0; c < CX0.Length; c++)
        {
            if (c == 0 && r <= 1) continue;  // 튜토 자리
            float ax0 = CX0[c], ay0 = RY0[r], ax1 = CX1[c], ay1 = RY1[r];
            string p = $"B{c}{r}";
            int seed = H(c + 1, r + 1);

            if (c == 0 && r == 2)         n += BuildPharmacyArcade(map);          // ② 약국·상가 심부(상세)
            else if (c == 1 && r <= 1)    n += Big(map, p, ax0, ay0, ax1, ay1, 'S'); // ③ 폐아파트 주거동
            else if (c == 4 && r == 0)    n += Big(map, p, ax0, ay0, ax1, ay1, 'W'); // ④ 유리타워
            else if (c == 2 && r == 3)    n += BuildGreenhouseDome(map);          // ⑤ 식물원 돔(상세)
            else if (c == 3 && r == 2)    n += Plaza(map, p, ax0, ay0, ax1, ay1);    // 주차장/공터
            else if (c == 1 && r == 4)    n += Plaza(map, p, ax0, ay0, ax1, ay1);    // 공원
            else                          n += Shops(map, p, ax0, ay0, ax1, ay1, 4f, seed); // 작은 점포(제각각)
        }

        // ── 4 랜드마크 마커(간선도로 위; 약국은 아케이드 내부에 상세 배치됨) ──
        n += GreyboxBuild.Note(map, "AP_Label", 80f, 49f, "폐아파트 ★★", "튜토 차고 東 큰길. key_apt_admin → 펜트 key_tower_card. 수직 다층 후속.");
        n += GreyboxBuild.Marker(map, "gb_crate", "key_apt_admin", 100f, 49f);
        n += GreyboxBuild.Marker(map, "gb_door",  "Pent_Gate(key_apt_admin)", 132f, 49f);
        n += GreyboxBuild.Marker(map, "gb_crate", "key_tower_card", 140f, 49f);
        n += GreyboxBuild.Marker(map, "gb_enemy", "Bandit_Apt", 170f, 49f);
        n += GreyboxBuild.Note(map, "TW_Label", 250f, 49f, "유리타워 ★★★★", "東단. key_tower_card로 상층 R&D → key_dome_code. 카드키·수직 후속.");
        n += GreyboxBuild.Marker(map, "gb_door",  "RnD_Gate(key_tower_card)", 278f, 49f);
        n += GreyboxBuild.Marker(map, "gb_crate", "key_dome_code", 295f, 49f);
        n += GreyboxBuild.Marker(map, "gb_enemy", "Bandit_Tower", 312f, 49f);
        n += GreyboxBuild.Note(map, "RV_Label", 100f, 328f, "강변 부두 ★★★", "最北 한강 경계. 부두 창고. 탈출 밀집.");

        // ── 전용 내부 씬이 있는 랜드마크 진입 (2026-07-11) ──
        //   §1.4c 현행 랜드마크 = 약국(아케이드 내부에 배선됨) / 무너진 상가 / 짙은현상 지하창고(창고 안).
        //   ※ 폐아파트·유리타워는 구버전 랜드마크(level-apartment/tower.md 보존) — 공용 내부로 처리.
        n += GreyboxBuild.Note(map, "CM_Label", 232f, 300f, "무너진 상가 ★★★", "SQ-001 갇힌 생존자. 잔해 미로 최심부.");
        n += Enter(map, "CollapsedMall_Enter", 232f, 296f, "Int_CollapsedMall");
        n += ReturnSpawn(map, "from_collapsed", 232f, 293f);

        // ── 레이드 스폰 5(매 판 랜덤 1곳) — 주변부 거리 분산(간선도로 위) ──
        //   pointId를 오브젝트명과 같게 주입해야 SpawnPoint가 실제로 식별된다(주입 없으면 프리팹 기본값 "default").
        n += Spawn5(map, "SP1_S",  172f, 49f);   // 남(차고 큰길)
        n += Spawn5(map, "SP2_N",  172f, 328f);  // 북(강변 앞)
        n += Spawn5(map, "SP3_W",   67f, 172f);  // 서
        n += Spawn5(map, "SP4_E",  279f, 172f);  // 동
        n += Spawn5(map, "SP5_NE", 203f, 260f);  // 북동 내부
        // ── 탈출: 고정 1(전 스폰 공용) + 풀 4(스폰별 2개 매치 = 먼 코너, 맵 횡단 유도) ──
        //   ExitPoint 설정(targetScene/spawnPointId/대기)을 주입해야 실제 탈출로 동작한다.
        n += ExitPt(map, "Exit_Fixed", 123f, 172f);  // 고정(중앙)
        n += ExitPt(map, "PX_SW", 40f,  49f);
        n += ExitPt(map, "PX_SE", 300f, 49f);
        n += ExitPt(map, "PX_NW", 40f,  328f);
        n += ExitPt(map, "PX_NE", 300f, 328f);
        // 매치(스폰→풀 2, 고정 제외): SP1_S→{NW,NE} · SP2_N→{SW,SE} · SP3_W→{SE,NE} · SP4_E→{SW,NW} · SP5_NE→{SW,NW}
        // → 런타임 랜덤스폰 + 매치 탈출 활성 = RaidSpawnDirector(아래 배치). 매치표는 디렉터가 동일하게 보유.
        n += Director(map);

        // ── 루팅 예산제(MapSpawnController + ItemSpawnPoint 앵커) + 적 밀도 ──
        //   예산은 맵 전체 총량 → 앵커를 많이 심은 구역에 루트가 몰린다 = 보상 곡선.
        //   난이도는 SpawnZone 유닛키/마릿수로 = 위험 곡선. (결정 2026-07-11: 안쪽으로 갈수록 위험·보상 ↑)
        n += Controller(map);

        // ① 상가골목/약국 아케이드(진입부, C0R2) — 약함·잡템
        n += Scatter(map, "SZ_Arcade", 18f, 86f, 62f, 164f, 4, 3, 11);
        n += EnemyZone(map, "EZ_Arcade", 40f, 125f, 20f, 30f, "bandit_melee_1", 2);

        // ② 폐아파트(C1R0~1) — 중
        n += Scatter(map, "SZ_Apt", 72f, 18f, 118f, 74f, 5, 4, 22);
        n += EnemyZone(map, "EZ_Apt", 95f, 46f, 24f, 26f, "bandit_melee_1", 3);

        // ③ 식물원 돔·습지(C2R3) — 중상
        n += Scatter(map, "SZ_Dome", 130f, 180f, 196f, 252f, 5, 5, 33);
        n += EnemyZone(map, "EZ_Dome", 163f, 216f, 30f, 30f, "bandit_melee_1", 2);
        n += EnemyZone(map, "EZ_Dome_R", 178f, 236f, 14f, 14f, "bandit_ranged", 1);

        // ④ 유리 R&D 타워(C4R0) — 강함·고급 루트(최심부)
        n += Scatter(map, "SZ_Tower", 285f, 18f, 325f, 42f, 6, 6, 44);
        n += EnemyZone(map, "EZ_Tower", 305f, 30f, 22f, 14f, "bandit_ranged", 2);
        n += EnemyZone(map, "EZ_Tower_T", 315f, 30f, 10f, 10f, "bandit_tank", 1);

        // ⑤ 주차장·공원(개활지) — 낮은 밀도, 적 약간
        n += Scatter(map, "SZ_Plaza", 210f, 86f, 272f, 164f, 3, 2, 55);
        n += EnemyZone(map, "EZ_Plaza", 240f, 125f, 24f, 24f, "bandit_melee_1", 2);
        n += Scatter(map, "SZ_Park", 74f, 268f, 116f, 320f, 3, 2, 66);

        GreyboxBuild.EndScene(scene, ScenePath, n, "지역1 Zone1(폐상가 도심 — 블록 성격 다양화)");
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
    static int Scatter(GameObject map, string prefix, float x0, float y0, float x1, float y1, int ground, int crate, int seed)
    {
        int n = 0;
        var rnd = new System.Random(seed);
        for (int i = 0; i < ground; i++)
        {
            float x = Mathf.Lerp(x0, x1, (float)rnd.NextDouble());
            float y = Mathf.Lerp(y0, y1, (float)rnd.NextDouble());
            var go = new GameObject($"{prefix}_G{i}");
            go.transform.SetParent(map.transform, false);
            go.transform.localPosition = new Vector3(x, y, 0f);
            SetSpawnType(go.AddComponent<ItemSpawnPoint>(), 0);   // Ground
            n++;
        }
        for (int i = 0; i < crate; i++)
        {
            float x = Mathf.Lerp(x0, x1, (float)rnd.NextDouble());
            float y = Mathf.Lerp(y0, y1, (float)rnd.NextDouble());
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
    /// ② 약국·상가 심부(상세) — C0R2(x16~64 y82~168). 좁은 아케이드 골목 + 약국 앵커.
    ///   동선: (튜토 南 갭 x28~38) → 세로 스파인(x28~38) → 교차골목(y122~128) → 북 출구(x46~52, 식물원 간선) / 동 갭(y122~128 → V간선).
    ///   약국 미니퍼즐: key_pharmacy(카운터) → MedCabinet(잠금) → SQ-002. 의료·생필품 루팅.
    /// </summary>
    static int BuildPharmacyArcade(GameObject m)
    {
        int n = 0;
        // 경계(남 갭 x28~38 튜토 · 북 갭 x46~52 식물원 · 동 갭 y122~128 V간선 · 서벽)
        n += GreyboxBuild.WallSeg(m, "PA_S_a", 16f, 82f, 28f, 84f);
        n += GreyboxBuild.WallSeg(m, "PA_S_b", 38f, 82f, 64f, 84f);
        n += GreyboxBuild.WallSeg(m, "PA_N_a", 16f, 166f, 46f, 168f);
        n += GreyboxBuild.WallSeg(m, "PA_N_b", 52f, 166f, 64f, 168f);
        n += GreyboxBuild.WallSeg(m, "PA_W",   16f, 82f, 18f, 168f);
        n += GreyboxBuild.WallSeg(m, "PA_E_a", 62f, 82f, 64f, 122f);
        n += GreyboxBuild.WallSeg(m, "PA_E_b", 62f, 128f, 64f, 168f);

        // 약국(앵커) — 방 x37~62 y84~116, 서문(스파인 향). key_pharmacy→약장(잠금)→SQ-002.
        //   2026-07-11: 껍데기만 두고 **내부는 Int_Pharmacy 씬**(전당포식 전환). 서문에 진입 트리거 + 복귀 스폰.
        n += GreyboxBuild.Building(m, "Pharmacy", 37f, 84f, 62f, 116f, 'W', 98f, "gb_door", "Pharmacy_Door");
        n += Enter(m, "Pharmacy_Enter", 36.2f, 99f, "Int_Pharmacy");   // 서문 바로 앞(문 갭 y98~100)
        n += ReturnSpawn(m, "from_pharmacy", 34.5f, 99f);              // 내부에서 나오면 문 앞
        n += GreyboxBuild.Marker(m, "gb_crate", "key_pharmacy", 44f, 92f);
        n += GreyboxBuild.Marker(m, "gb_door",  "MedCabinet(key_pharmacy)", 58f, 110f);
        n += GreyboxBuild.Marker(m, "gb_crate", "SQ002_Box", 52f, 110f);
        n += GreyboxBuild.Marker(m, "gb_shelf", "Pharm_Shelf1", 46f, 88f);
        n += GreyboxBuild.Marker(m, "gb_shelf", "Pharm_Shelf2", 56f, 88f);
        n += GreyboxBuild.Note(m, "Pharmacy_Note", 44f, 106f, "약국 카운터 메모",
            "처방 약은 약장(MedCabinet) 안. 카운터 밑 열쇠(key_pharmacy)로 연다.");

        // 빽빽한 점포(스파인 x29~37 개방·약국 제외) — 서측 열 + 동측 하단(약국 아래). 골목 3m.
        n += Shops(m, "AW", 16f, 84f, 29f, 164f, 3f, H(101, 7));   // 서측 점포 열
        n += Shops(m, "AE", 37f, 120f, 62f, 164f, 3f, H(102, 7));  // 동측 하단 점포

        n += GreyboxBuild.Marker(m, "gb_enemy", "Bandit_Pharmacy", 33f, 140f);
        n += GreyboxBuild.Note(m, "PD_Label", 32f, 160f, "약국·상가 심부 ★★",
            "튜토 北. 약장 미니퍼즐(key_pharmacy→약장→SQ-002) + 의료·생필품. 빽빽한 아케이드 골목.");
        return n;
    }

    /// <summary>
    /// ⑤ 식물원 돔(상세) — C2R3(x126~200 y176~256). 시그니처·최고보상·최고위험.
    ///   대형 유리 온실: 중앙 금고실(돔 코어, 문=key_dome_code 잠금 → 최고 보상) + 온실 화단(벤치) + 습지 침수(이동 제약).
    ///   진입 = 南 갭 x148~160(식물원 간선). 빽빽한 점포와 대비되는 '개방형 온실' 톤.
    /// </summary>
    static int BuildGreenhouseDome(GameObject m)
    {
        int n = 0;
        // 외곽(남 갭 x148~160 = 간선 진입)
        n += GreyboxBuild.WallSeg(m, "GH_S_a", 126f, 176f, 148f, 178f);
        n += GreyboxBuild.WallSeg(m, "GH_S_b", 160f, 176f, 200f, 178f);
        n += GreyboxBuild.WallSeg(m, "GH_N",   126f, 254f, 200f, 256f);
        n += GreyboxBuild.WallSeg(m, "GH_W",   126f, 178f, 128f, 254f);
        n += GreyboxBuild.WallSeg(m, "GH_E",   198f, 178f, 200f, 254f);
        // 중앙 금고실(돔 코어) — 문 = 金庫(key_dome_code 잠금). 최고 보상.
        n += GreyboxBuild.Building(m, "DomeCore", 150f, 204f, 178f, 232f, 'S', 162f, "gb_door", "Dome_Vault(key_dome_code)");
        n += GreyboxBuild.Marker(m, "gb_crate", "Dome_Reward", 164f, 222f);
        n += GreyboxBuild.Marker(m, "gb_shelf", "Dome_RareA", 157f, 210f);
        n += GreyboxBuild.Marker(m, "gb_shelf", "Dome_RareB", 171f, 210f);
        // 온실 화단(벤치=선반) + 고가 루팅
        n += GreyboxBuild.Marker(m, "gb_shelf", "GH_Bed1", 136f, 190f);
        n += GreyboxBuild.Marker(m, "gb_shelf", "GH_Bed2", 146f, 190f);
        n += GreyboxBuild.Marker(m, "gb_shelf", "GH_Bed3", 188f, 190f);
        n += GreyboxBuild.Marker(m, "gb_shelf", "GH_Bed4", 138f, 246f);
        n += GreyboxBuild.Marker(m, "gb_crate", "GH_Crate1", 132f, 240f);
        n += GreyboxBuild.Marker(m, "gb_crate", "GH_Crate2", 192f, 246f);
        // 습지 침수(이동 제약) = 바리케이드 패치
        n += GreyboxBuild.Barricade(m, "GH_Flood_W", 134f, 218f, 8f, 22f);
        n += GreyboxBuild.Barricade(m, "GH_Flood_E", 193f, 205f, 8f, 18f);
        // 적(고위험) 2 + 라벨
        n += GreyboxBuild.Marker(m, "gb_enemy", "Bandit_Dome1", 140f, 240f);
        n += GreyboxBuild.Marker(m, "gb_enemy", "Bandit_Dome2", 184f, 226f);
        n += GreyboxBuild.Note(m, "GH_Label", 132f, 182f, "식물원 돔 ★★★★★ (시그니처)",
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
                    // 2026-07-11: 절차 생성 점포도 **전부 들어갈 수 있게** — 공용 내부(Int_Generic)로 진입.
                    //   복귀는 고정 스폰이 아니라 '들어온 문 앞'(BuildingReturn) → 한 채를 돌려 써도 제자리로 나온다.
                    n += EnterGeneric(m, $"{p}_{i}_{j}_Enter", doorX + 1f, (side == 'S' ? y : y + d) + (side == 'S' ? -0.9f : 0.9f));
                }
                x += w + street; j++;
            }
            y += d + street; i++;
        }
        return n;
    }

    /// <summary>큰 건물(랜드마크): **껍데기(외벽+문)만** — 내부는 별도 씬(전당포식 전환, 2026-07-11).
    /// 예전엔 내부 십자 칸막이를 그려 위에서 내부가 다 보였음 → 제거.</summary>
    static int Big(GameObject m, string p, float x0, float y0, float x1, float y1, char side)
    {
        float bx0 = x0 + 2f, by0 = y0 + 2f, bx1 = x1 - 2f, by1 = y1 - 2f;
        if (bx1 - bx0 < 12f || by1 - by0 < 12f) return Shops(m, p, x0, y0, x1, y1, 3f, H((int)x0, (int)y0));
        int n = 0;
        float doorAt = (side == 'S' || side == 'N') ? (bx0 + bx1) * 0.5f - 1f : (by0 + by1) * 0.5f - 1f;
        n += GreyboxBuild.Building(m, p, bx0, by0, bx1, by1, side, doorAt, "gb_door", $"{p}_D");
        // 2026-07-11: 내부 십자 칸막이 제거 — 건물 = 껍데기(외벽+문)뿐. 내부는 별도 씬(전당포식 전환).
        //   문에 BuildingEntrance를 달 건물은 Enter()로 개별 지정한다(내부 씬이 있는 건물만).
        return n;
    }

    /// <summary>건물 문에 진입 트리거(BuildingEntrance) — 내부 씬이 있는 건물만.
    /// 밟으면 내부 씬으로 전환(페이드+캐릭터 유지). 복귀 스폰은 Zone1의 from_&lt;건물&gt;.</summary>
    static int Enter(GameObject m, string name, float x, float y, string targetScene, string spawnId = "default")
    {
        if (GreyboxBuild.Marker(m, "gb_exit", name, x, y) == 0) return 0;
        var t = FindChild(m.transform, name);
        if (t == null) return 0;
        var go = t.gameObject;

        var io = go.GetComponentInChildren<InteractableObject>();   // gb_exit의 E키 ExitPoint는 중복 → 제거
        if (io != null) Object.DestroyImmediate(io);

        var box = go.GetComponent<BoxCollider2D>();
        if (box == null) box = go.AddComponent<BoxCollider2D>();   // ??는 Unity 가짜 null을 통과시켜 못 씀
        box.isTrigger = true;
        box.size = new Vector2(2.2f, 1.4f);

        var be = go.GetComponent<BuildingEntrance>();
        if (be == null) be = go.AddComponent<BuildingEntrance>();
        be.Configure(targetScene, spawnId, false);
        return 1;
    }

    /// <summary>공용 내부(Int_Generic)로 들어가는 진입 트리거. 복귀는 `__back__`(들어온 문 앞).
    /// 전용 내부가 만들어진 건물은 Enter()로 개별 지정하고, 나머지 절차 생성 건물이 이걸 쓴다.</summary>
    static int EnterGeneric(GameObject m, string name, float x, float y)
        => Enter(m, name, x, y, "Int_Generic", BuildingReturn.BackSpawnId);

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
        n += GreyboxBuild.Marker(m, "gb_crate", $"{p}_c1", (x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
        n += GreyboxBuild.Marker(m, "gb_crate", $"{p}_c2", (x0 + x1) * 0.5f + 9f, (y0 + y1) * 0.5f + 7f);
        return n;
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
