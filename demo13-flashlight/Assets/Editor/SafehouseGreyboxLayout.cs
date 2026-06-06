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
/// 배치(ScrapMarket과 동일):
///   • XY 평면, Z=0, 1u=1m. 맵 26(W)×16(H), 중심 (13,8). 골목 + 건물(은신처/전당포/잠금건물 = 벽으로 둘러싼 방 + 문).
///   • 벽 = gb_wall을 (lenX, thickY, 1)로 스케일한 바. 바닥 = gb_floor 1개 전체 스케일.
///   • NPC/스폰 = 스케일 1 마커. 스폰은 2개만(레이드 지역 아님): default(최초 시작) / raid_return(레이드 귀환 — 성공·실패·사망 공통).
///   • 멱등: 같은 경로로 저장하면 덮어씀. 프리팹은 InstantiatePrefab(링크 유지).
///
/// 메뉴: Tools ▸ TopDown ▸ Map ▸ Build Safehouse Greybox Layout
/// </summary>
public static class SafehouseGreyboxLayout
{
    const string ScenePath  = "Assets/Scenes/Safehouse.unity";
    const string PrefabRoot = "Props2D/Prefabs/";

    const float MapW = 26f, MapH = 16f;   // 골목 + 건물(은신처/전당포/잠금건물). 컴팩트하되 건물 들어갈 정도

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

        // ── 바닥 + 외곽 (26×16). 동측 = 우측 세로 방호벽(게이트, 개구부) → 폐도시. ──
        n += Floor(map, "Floor", 13f, 8f, MapW, MapH);
        n += Wall(map, "Wall_S", 13f,  0.5f, 26f, 1f);
        n += Wall(map, "Wall_N", 13f, 15.5f, 26f, 1f);
        n += Wall(map, "Wall_W",  0.5f, 8f,   1f,16f);
        n += Barricade(map, "Gate_Wall_N", 25f, 12.25f, 1f, 5.5f); // 우측 세로 방호벽 상 X24.5~25.5 Y9.5~15
        n += Barricade(map, "Gate_Wall_S", 25f,  3.75f, 1f, 5.5f); // 우측 세로 방호벽 하 X24.5~25.5 Y1~6.5
        //  개구부 = X24.5 Y6.5~9.5 → 폐도시. 출격은 게시판으로, 게이트는 경계 비주얼.

        // ── 은신처(서측 건물) — 벽 + 동측 문(→Hideout 씬) ──
        n += Wall(map, "Hideout_N",  3.5f,11.5f, 5f, 1f);
        n += Wall(map, "Hideout_S",  3.5f, 4.5f, 5f, 1f);
        n += Wall(map, "Hideout_E1", 6.5f, 6f,   1f, 3f);
        n += Wall(map, "Hideout_E2", 6.5f,10f,   1f, 3f);   // 문 갭 Y7.5~8.5
        n += Exit (map, "Hideout_Entrance", 6.5f, 8f, "Hideout", "default");

        // ── 북측 잠금 건물(수리점/의료소) — 벽 + 셔터(막힘) ──
        n += Wall(map, "Repair_W", 7.5f,13.5f, 1f, 4f);  n += Wall(map, "Repair_E", 12.5f,13.5f, 1f, 4f);
        n += Barricade(map, "Repair_Shutter", 10f, 11.5f, 4f, 1f);   // 수리점🔒
        n += Wall(map, "Med_W",   13.5f,13.5f, 1f, 4f);  n += Wall(map, "Med_E",   18.5f,13.5f, 1f, 4f);
        n += Barricade(map, "Med_Shutter",    16f, 11.5f, 4f, 1f);   // 의료소🔒

        // ── 전당포(동측 건물, 게이트 안쪽) — 벽 + 서측 문, 강무진 안 ──
        n += Wall(map, "Pawn_N",  20f, 11.5f, 5f, 1f);   // X17.5~22.5 Y11~12
        n += Wall(map, "Pawn_S",  20f,  4.5f, 5f, 1f);   // X17.5~22.5 Y4~5
        n += Wall(map, "Pawn_E",  22f,  8f,   1f, 7f);   // 동벽 X21.5~22.5 Y4.5~11.5
        n += Wall(map, "Pawn_W1", 17.5f,6f,   1f, 3f);
        n += Wall(map, "Pawn_W2", 17.5f,10f,  1f, 3f);   // 문 갭 Y7.5~8.5
        n += Npc (map, "NPC_Pawnshop", "pawnshop", 20f, 8f);  // 강무진 — 방 안

        // ── 마당(중앙) — 스폰 2개(레이드 지역 아님) + 관리인 + 회수꾼 + 게시판 ──
        n += Spawn(map, "default",     9f, 8f);   // ① 최초 게임 시작
        n += Spawn(map, "raid_return",10f, 8f);   // ② 레이드 귀환(성공/실패/사망 공통)
        n += Npc(map, "NPC_Warden",   "district_warden",   13f, 8f);   // 구역 관리인 — 마당 중앙
        n += Npc(map, "NPC_Veteran",  "veteran_scavenger", 15f, 8f);   // 베테랑 회수꾼 — 전당포 앞
        n += Marker(map, "gb_mapboard","Board_Quest",      15f, 5.5f); // 게시판(의뢰). ※코드는 MapBoard→MapSelectUI

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
                  "  • 십자 허브: 중앙 스폰(집) + 시설(침대/작업대/의료대/조리대) + 우측 지도판(출전)/회수꾼 + 하단 전당포 NPC + 좌측 관리인.\n" +
                  "  • 잠금 건물/방호벽 = 바리케이드(막힘). NPC는 gb_npc(빈 npcId) — NPC Maker로 NPCData 연결 필요(전당포=ShopData).\n" +
                  "  • Systems 씬을 열고 Play하면 GameBoot이 이 Safehouse를 로드 → 지도판으로 출전.");

        if (!Application.isBatchMode && !ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("Safehouse Greybox",
                $"{ScenePath} 생성 완료.\n\nMap 루트 하위 그레이박스 {n}개 — 중앙 스폰 + 시설 + 지도판(출전) + NPC(전당포/회수꾼/관리인).\n" +
                "NPC는 빈 마커이니 NPC Maker로 NPCData(전당포 ShopData) 연결하세요.\n" +
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
    static int Wall(GameObject parent, string name, float cx, float cy, float lenX, float thickY)
        => Bar(parent, "gb_wall", name, cx, cy, lenX, thickY);
    static int Barricade(GameObject parent, string name, float cx, float cy, float lenX, float thickY)
        => Bar(parent, "gb_barricade", name, cx, cy, lenX, thickY);

    static int Bar(GameObject parent, string prefabId, string name, float cx, float cy, float lenX, float thickY)
    {
        var go = Inst(prefabId, name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(cx, cy, 0f);
        go.transform.localScale    = new Vector3(lenX, thickY, 1f);
        CounterScaleLabel(go);
        return 1;
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
