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
/// 배치(# 격자 골목):
///   • XY 평면, Z=0, 1u=1m. 맵 32(W)×22(H), 중심 (16,11). 골목이 #자(가로2+세로2)로 교차 → 3×3 = 9칸.
///     세로골목 X9~12·X20~23, 가로골목 Y7~10·Y15~18. 칸: 상단=수리점/전당포/의료소, 중단=가구점/광장(허브,열림)/블랙마켓, 하단=컨테이너 야드(집).
///   • 상단중앙 = 전당포(강무진), 중앙 = 광장 허브, 하단 = 컨테이너 더미 야드(집·은신처 입구·스폰2 = '레이드장 아래 베이스' 느낌).
///   • 동쪽 = 세로 방호벽 게이트(개구부 Y7~10 = 하단 가로 골목 동쪽 끝) → 폐도시(출전). 잠금건물 입구는 셔터(해금 게이팅).
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

    const float MapW = 32f, MapH = 22f;   // # 격자 골목: 가로2+세로2 골목 → 3×3칸(중앙=광장 허브 / 상단중앙=전당포 / 하단=컨테이너 야드 집)

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

        // ── 바닥(32×22) + 외곽: 남/북/서 벽 + 동쪽 세로 방호벽 게이트(개구부 Y7~10) → 폐도시. ──
        n += Floor(map, "Floor", 16f, 11f, MapW, MapH);
        n += Wall(map, "Wall_S", 16f,  0.5f, 32f, 1f);
        n += Wall(map, "Wall_N", 16f, 21.5f, 32f, 1f);
        n += Wall(map, "Wall_W",  0.5f,11f,   1f,22f);
        n += Barricade(map, "Gate_Wall_N", 31.5f, 15.5f, 1f, 11f, "게이트→폐도시"); // 우측 세로 방호벽 상 Y10~21
        n += Barricade(map, "Gate_Wall_S", 31.5f,  4f,   1f,  6f, "폐도시 방면");   // 우측 세로 방호벽 하 Y1~7
        //  개구부 = X31.5 Y7~10(하단 가로 골목 동쪽 끝) → 폐도시. 출격은 게시판 의뢰로, 게이트는 경계/출구.

        // ── # 격자 9칸: 골목(세로 X9~12·X20~23, 가로 Y7~10·Y15~18) 사이를 건물 블록이 메움. 중앙(B-Mid)만 열린 광장. ──
        // 상단(Y18~21): 수리점 / 전당포(중앙 위) / 의료소
        n += Wall(map, "Block_Repair", 5f, 19.5f, 8f, 3f, "수리점 (잠금)");   // 좌상
        n += Wall(map, "Block_Pawn",  16f,  19.5f, 8f, 3f, "전당포");          // 중상 = 전당포(강무진 안)
        n += Wall(map, "Block_Med",   27f,  19.5f, 8f, 3f, "의료소 (잠금)");   // 우상
        // 중단(Y10~15): 가구점 / [광장=열림] / 블랙마켓
        n += Wall(map, "Block_Furn",   5f, 12.5f, 8f, 5f, "가구점 (잠금)");   // 좌중
        n += Wall(map, "Block_Black", 27f,  12.5f, 8f, 5f, "블랙마켓 (잠금)"); // 우중
        //  (중앙 B-Mid X12~20 Y10~15 = 광장 허브 — 블록 없음)

        // ── 잠금 건물 입구 셔터(해금 시 제거) — 인접 골목 향 ──
        n += Barricade(map, "Repair_Shutter", 5f, 18f, 4f, 1f, "셔터");  // 수리점 → 상단 가로골목
        n += Barricade(map, "Med_Shutter",   27f,  18f, 4f, 1f, "셔터");  // 의료소 → 상단 가로골목
        n += Barricade(map, "Furn_Shutter",   5f, 10f, 4f, 1f, "셔터");  // 가구점 → 하단 가로골목
        n += Barricade(map, "Black_Shutter", 27f,  10f, 4f, 1f, "셔터");  // 블랙마켓 → 하단 가로골목

        // ── 하단 = 컨테이너 더미 야드(집 = '레이드장 아래 베이스'). 은신처 입구 + 스폰 2개 + 컨테이너 블록들. ──
        n += Wall(map, "Container_Home", 16f, 6f, 5f, 2f, "집(은신처)");   // 중앙 야드 = 사는 컨테이너(양옆 1.5 통로)
        n += Exit (map, "Hideout_Entrance", 16f, 4f, "Hideout", "default"); // 집 앞 → 은신처 실내 씬
        n += Spawn(map, "default",     14f, 2.5f);  // ① 최초 게임 시작
        n += Spawn(map, "raid_return", 18f, 2.5f);  // ② 레이드 귀환(성공/실패/사망 공통)
        n += Wall(map, "Container_W1", 4.5f, 2.5f, 5f, 1.6f, "컨테이너");  // 좌 야드 더미
        n += Wall(map, "Container_W2", 4f,   5f,   4f, 1.6f, "컨테이너");
        n += Wall(map, "Container_E1", 27f,  2.5f, 5f, 1.6f, "컨테이너");  // 우 야드 더미
        n += Wall(map, "Container_E2", 27.5f,5f,   4f, 1.6f, "컨테이너");

        // ── NPC + 게시판 (스토리 S-001~007 동선) ──
        n += Npc (map, "NPC_Pawnshop", "pawnshop",          16f, 16.5f);// 전당포 강무진 — 전당포(상단중앙) 앞 가로골목
        n += Npc (map, "NPC_Warden",   "district_warden",   16f, 12.5f);// 구역 관리인 — 중앙 광장 허브
        n += Marker(map, "gb_mapboard","Board_Quest",       27f, 8.5f); // 게시판(의뢰 수령) — 하단 가로골목 동, 게이트 옆 ※코드는 MapBoard→MapSelectUI
        n += Npc (map, "NPC_Veteran",  "veteran_scavenger", 29f, 8.5f); // 베테랑 회수꾼 — 게시판 옆(게이트 앞)

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
                  "  • # 격자(32×22): 골목 가로2(Y7~10·Y15~18)+세로2(X9~12·X20~23) → 3×3 9칸.\n" +
                  "  • 상단=수리점/전당포(중앙위)/의료소 · 중단=가구점/광장(허브)/블랙마켓 · 하단=컨테이너 야드(집·은신처 입구·스폰2).\n" +
                  "  • 동쪽 세로 방호벽 게이트(개구부 Y7~10) → 폐도시. 게시판/회수꾼=게이트 앞. 잠금건물 입구=셔터.\n" +
                  "  • NPC는 storyNpcId로 NPCData 자동연결(SafehouseNpcBuilder 선행). 전당포=ShopData. 시설(침대/작업대)은 은신처 실내(Hideout).\n" +
                  "  • Systems 씬을 열고 Play하면 GameBoot이 이 Safehouse를 로드 → 게시판으로 출전.");

        if (!Application.isBatchMode && !ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("Safehouse Greybox",
                $"{ScenePath} 생성 완료 — # 격자 골목(32×22).\n\nMap 루트 하위 그레이박스 {n}개:\n" +
                "  • 골목 #자(가로2+세로2) → 3×3 9칸\n" +
                "  • 상단중앙 전당포 · 중앙 광장 허브 · 하단 컨테이너 야드(집+스폰2)\n" +
                "  • 동쪽 방호벽 게이트(출전) + 게시판/회수꾼, 잠금건물 4(수리점/의료소/가구점/블랙마켓)\n\n" +
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
