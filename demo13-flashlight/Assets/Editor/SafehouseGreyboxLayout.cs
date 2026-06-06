#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 안전가옥 그레이박스 레이아웃 v1을 gb_* 프리팹으로 Safehouse.unity에 배치(지역1=ScrapMarket과 동일 기법).
/// (docs/safehouse.md '십자 허브 골목' — 중앙 집/허브 + 전당포(하) + 지도판/게이트(우) + 시설 + 잠금 건물)
///
/// 생성물: Assets/Scenes/Safehouse.unity — '맵 콘텐츠'만(카메라/조명/매니저/플레이어는 Systems 부트 씬이 공급).
///   ※ 루프가 로드하는 씬에 바로 채워 "빈 안전가옥" 문제를 해소. 모든 그레이박스는 'Map' 루트 하위.
///
/// 배치(십자 허브):
///   • XY 평면, Z=0, 1u=1m. 맵 24(W)×20(H), 중심 (12,10). 십자(+) 골목 — 중앙 집 허브 + 사방 골목(N/S/E/W).
///     모서리 4블록(8.5×8) = 잠금건물(수리점/의료소/블랙마켓)·영구벽으로 십자 사이를 메움. 통행로: 가로 Y8~12, 세로 X9.5~14.5.
///   • 동쪽 = 세로 방호벽 게이트(개구부 Y8~12 = 가로 골목 동쪽 출구) → 폐도시(출전). 곁가지 입구는 셔터/바리케이드(해금 게이팅).
///   • 벽/블록 = gb_wall, 셔터/방호벽 = gb_barricade를 (lenX, thickY, 1)로 스케일. 바닥 = gb_floor 1개 전체 스케일.
///   • NPC/스폰 = 스케일 1 마커. 스폰 2개만(레이드 지역 아님): default(최초 시작) / raid_return(레이드 귀환 — 성공·실패·사망 공통).
///   • 멱등: 같은 경로로 저장하면 덮어씀. 프리팹은 InstantiatePrefab(링크 유지).
///
/// 메뉴: Tools ▸ TopDown ▸ Map ▸ Build Safehouse Greybox Layout
/// </summary>
public static class SafehouseGreyboxLayout
{
    const string ScenePath  = "Assets/Scenes/Safehouse.unity";
    const string PrefabRoot = "Props2D/Prefabs/";

    const float MapW = 24f, MapH = 20f;   // 십자(+) 골목: 중앙 집 허브 + 사방 골목, 모서리 = 건물/영구벽

    static readonly string[] RequiredPrefabIds =
    {
        "gb_floor", "gb_wall", "gb_barricade", "gb_spawn", "gb_npc", "gb_door", "gb_exit",
        "gb_mapboard",
    };

    [MenuItem("Tools/TopDown/Map/Build Safehouse Greybox Layout")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var map = new GameObject("Map");
        int n = 0;

        EnsureGreyboxPalette();

        // ── 바닥(24×20) + 외곽 3면 벽(S/N/W). 동쪽 = 세로 방호벽 게이트(개구부 Y8~12) → 폐도시. ──
        n += Floor(map, "Floor", 12f, 10f, MapW, MapH);
        n += Wall(map, "Wall_S", 12f,  0.5f, 24f, 1f);
        n += Wall(map, "Wall_N", 12f, 19.5f, 24f, 1f);
        n += Wall(map, "Wall_W",  0.5f,10f,   1f,20f);
        n += Barricade(map, "Gate_Wall_N", 23.5f, 16f, 1f, 8f, "게이트");  // 우측 세로 방호벽 상 Y12~20
        n += Barricade(map, "Gate_Wall_S", 23.5f,  4f, 1f, 8f, "폐도시 방면"); // 우측 세로 방호벽 하 Y0~8
        //  개구부 = X23.5 Y8~12(가로 골목 동쪽 끝) → 폐도시. 출격은 게시판 의뢰로, 게이트는 경계/출구 비주얼.

        // ── 모서리 4블록 = 십자 사이를 메우는 벽(잠금 건물 / 영구벽). 통행로: 가로 Y8~12, 세로 X9.5~14.5만. ──
        n += Wall(map, "Block_NW_Repair",   5.25f, 16f, 8.5f, 8f, "수리점\n(잠금)");   // 북서 — 수리점(實·잠금)
        n += Wall(map, "Block_NE_Med",     18.75f, 16f, 8.5f, 8f, "의료소\n(잠금)");   // 북동 — 의료소(잠금)
        n += Wall(map, "Block_SW_Wall",     5.25f,  4f, 8.5f, 8f, "벽\n(막다른)");     // 남서 — 영구벽(막다른)
        n += Wall(map, "Block_SE_Black",   18.75f,  4f, 8.5f, 8f, "블랙마켓\n(잠금)"); // 남동 — 블랙마켓(잠금)

        // ── 잠금 게이팅(〰): 곁가지 골목 입구에 셔터/바리케이드(해금 시 제거) ──
        n += Barricade(map, "Repair_Shutter",  5.25f, 11.75f, 4f, 1f, "셔터");  // 수리점 입구(가로 골목 서쪽 북면)
        n += Barricade(map, "Med_Shutter",    18.75f, 11.75f, 4f, 1f, "셔터");  // 의료소 입구(가로 골목 동쪽 북면)
        n += Barricade(map, "Black_Shutter",  18.75f,  8.25f, 4f, 1f, "셔터");  // 블랙마켓 입구(가로 골목 동쪽 남면)
        n += Barricade(map, "Furniture_Gate",  2f, 10f, 1f, 4f, "가구점\n(잠금)"); // 가구점 — 가로 골목 서쪽 끝 막음

        // ── 중앙 허브(집 = 컨테이너 은신처 입구 + 스폰 2개) ──
        n += Exit (map, "Hideout_Entrance", 12f, 11.5f, "Hideout", "default"); // 집(實) → 은신처 실내 씬
        n += Spawn(map, "default",     11.5f, 9.5f);  // ① 최초 게임 시작
        n += Spawn(map, "raid_return", 12.5f, 9.5f);  // ② 레이드 귀환(성공/실패/사망 공통)

        // ── NPC + 게시판 (스토리 S-001~007 동선) ──
        n += Npc (map, "NPC_Pawnshop", "pawnshop",          12f, 3.5f); // 전당포 강무진 — 집 바로 아래(남 골목)
        n += Marker(map, "gb_mapboard","Board_Quest",       20f, 10f);  // 게시판(의뢰 수령) — 동 골목, 게이트 옆 ※코드는 MapBoard→MapSelectUI
        n += Npc (map, "NPC_Veteran",  "veteran_scavenger", 18f, 10f);  // 베테랑 회수꾼 — 게시판 옆
        n += Npc (map, "NPC_Warden",   "district_warden",   12f, 15f);  // 구역 관리인 — 북 골목

        // ── 저장(덮어쓰기) ──
        Selection.activeObject = null;
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!saved)
        {
            Debug.LogError("[SafehouseGB] 씬 저장 실패: " + ScenePath);
            return;
        }
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=cyan>[SafehouseGB]</color> 생성 완료: {ScenePath} — Map 하위 그레이박스 {n}개.\n" +
                  "  • 십자(+) 허브: 중앙 집(은신처 입구)+스폰2 / 남=전당포 / 동=게시판·회수꾼·게이트(출전) / 북=관리인 / 모서리=잠금건물·영구벽.\n" +
                  "  • 통행로: 가로 골목 Y8~12, 세로 골목 X9.5~14.5. 곁가지 입구는 셔터/바리케이드(해금 게이팅).\n" +
                  "  • NPC는 storyNpcId로 NPCData 자동연결(SafehouseNpcBuilder 선행). 전당포=ShopData. 시설(침대/작업대)은 은신처 실내(Hideout)에 있음.\n" +
                  "  • Systems 씬을 열고 Play하면 GameBoot이 이 Safehouse를 로드 → 게시판으로 출전.");

        if (!Application.isBatchMode && !ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("Safehouse Greybox",
                $"{ScenePath} 생성 완료 — 십자 허브.\n\nMap 루트 하위 그레이박스 {n}개:\n" +
                "  • 중앙 집(은신처 입구)+스폰2  • 남 전당포  • 동 게시판/회수꾼/게이트(출전)  • 북 관리인\n" +
                "  • 모서리 4블록=잠금건물(수리점/의료소/블랙마켓)·영구벽, 셔터로 게이팅\n\n" +
                "NPC는 SafehouseNpcBuilder의 NPCData가 있으면 자동연결(전당포=ShopData).\n" +
                "Systems 부트 씬이 카메라/조명/매니저/플레이어를 공급합니다.", "확인");
    }

    static void EnsureGreyboxPalette()
    {
        foreach (var id in RequiredPrefabIds)
            if (Resources.Load<GameObject>(PrefabRoot + id) == null)
            {
                Debug.Log($"<color=cyan>[SafehouseGB]</color> 프리팹 '{id}' 없음 → GreyboxPaletteBuilder.Generate.");
                GreyboxPaletteBuilder.Generate();
                return;
            }
    }

    // ── 헬퍼 (ScrapMarketGreyboxLayout과 동일 기법) ──
    // label != null 이면 자식 "Label" 텍스트를 교체(큰 블록/셔터를 씬 뷰에서 식별).
    static int Wall(GameObject parent, string name, float cx, float cy, float lenX, float thickY, string label = null)
        => Bar(parent, "gb_wall", name, cx, cy, lenX, thickY, label);
    static int Barricade(GameObject parent, string name, float cx, float cy, float lenX, float thickY, string label = null)
        => Bar(parent, "gb_barricade", name, cx, cy, lenX, thickY, label);

    static int Bar(GameObject parent, string prefabId, string name, float cx, float cy, float lenX, float thickY, string label = null)
    {
        var go = Inst(prefabId, name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(cx, cy, 0f);
        go.transform.localScale    = new Vector3(lenX, thickY, 1f);
        CounterScaleLabel(go);
        if (!string.IsNullOrEmpty(label)) SetLabel(go, label);
        return 1;
    }

    /// <summary>그레이박스 인스턴스의 자식 "Label" TextMesh 텍스트 교체(가독용).</summary>
    static void SetLabel(GameObject go, string text)
    {
        var label = go.transform.Find("Label");
        if (label == null) return;
        var tm = label.GetComponent<TextMesh>();
        if (tm != null) tm.text = text;
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

    /// <summary>스폰 배치 + pointId 직렬화 설정.</summary>
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

    /// <summary>ExitPoint(씬 전환) 마커 — targetScene/spawnPointId/대기 설정. 건물 진입은 즉시(wait 0).</summary>
    static int Exit(GameObject parent, string name, float x, float y, string targetScene, string spawnId)
    {
        var go = Inst("gb_exit", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);
        var io = go.GetComponentInChildren<InteractableObject>();
        if (io != null)
        {
            var so = new SerializedObject(io);
            var ts = so.FindProperty("targetScene");  if (ts != null) ts.stringValue = targetScene;
            var sp = so.FindProperty("spawnPointId");  if (sp != null) sp.stringValue = spawnId;
            var ew = so.FindProperty("exitWaitTime");  if (ew != null) ew.floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return 1;
    }

    /// <summary>NPC 마커 + storyNpcId 직렬화(대화/상점은 NPC Maker로 NPCData 별도 연결).</summary>
    static int Npc(GameObject parent, string name, string storyNpcId, float x, float y)
    {
        var go = Inst("gb_npc", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);
        var npc = go.GetComponentInChildren<NPCController>();
        if (npc != null)
        {
            var so = new SerializedObject(npc);
            var sid = so.FindProperty("storyNpcId");
            if (sid != null) sid.stringValue = storyNpcId;
            // NPCData 자동 연결(SafehouseNpcBuilder로 만들어 둔 게 있으면)
            var data = AssetDatabase.LoadAssetAtPath<NPCData>($"Assets/Resources/Data/NPC/{storyNpcId}.asset");
            var dp = so.FindProperty("npcData");
            if (dp != null && data != null) dp.objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return 1;
    }

    static GameObject Inst(string prefabId, string name, GameObject parent)
    {
        var prefab = Resources.Load<GameObject>(PrefabRoot + prefabId);
        if (prefab == null)
        {
            Debug.LogError($"[SafehouseGB] 프리팹 로드 실패: Resources/{PrefabRoot}{prefabId}");
            return null;
        }
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
