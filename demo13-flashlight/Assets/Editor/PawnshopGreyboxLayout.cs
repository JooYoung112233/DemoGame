#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 전당포 '실내' 그레이박스 씬(Pawnshop.unity) — 안전가옥 전당포 건물 입구(BuildingEntrance 트리거)로 진입하는 별도 실내.
///   (docs/safehouse.md '건물 전환 모델' / story-script S-002)
///
/// 하이드아웃과 달리 **캐릭터가 걸어다니는 일반 실내**(HideoutController 안 붙임). 작은 점포 방 —
/// 바닥+벽 4면, 진입 스폰(default), 강무진(pawnshop) NPC + 카운터, 그리고 **출구 BuildingEntrance 트리거**
/// (밟으면 Safehouse/from_pawnshop으로 자동 복귀, 페이드+캐릭터 유지).
///
/// 콘텐츠 전용(Systems가 카메라/조명/매니저/플레이어 공급). 빌드세팅에 Pawnshop 자동 등록.
/// 메뉴: Tools ▸ TopDown ▸ 빌드 ▸ 전당포
/// </summary>
public static class PawnshopGreyboxLayout
{
    const string ScenePath  = "Assets/Scenes/Pawnshop.unity";
    const string PrefabRoot = "Props2D/Prefabs/";
    const float MapW = 12f, MapH = 9f;   // 아담한 점포(전당포 5×4 거래 위주 — 통로 여유 포함 12×9)

    static readonly string[] RequiredPrefabIds =
    {
        "gb_floor", "gb_wall", "gb_spawn", "gb_npc",
    };

    [MenuItem("Tools/TopDown/빌드/전당포", priority = -97)]
    public static void Build()
    {
        var scene = EditorSceneBuildUtil.NewDetachedScene(out var prevActive);  // 현재 씬 유지(폴더에만 생성)
        var map = new GameObject("Map");
        int n = 0;

        EnsureGreyboxPalette();

        // 바닥 + 외곽 벽 4면(작은 방)
        n += Floor(map, "Floor", 6f, 4.5f, MapW, MapH);
        n += Wall(map, "Wall_S", 6f,  0.5f, 12f, 1f);
        n += Wall(map, "Wall_N", 6f,  8.5f, 12f, 1f);
        n += Wall(map, "Wall_W", 0.5f,4.5f,  1f, 9f);
        n += Wall(map, "Wall_E",11.5f,4.5f,  1f, 9f);

        // 진입 스폰(안전가옥 전당포 입구→여기). 문 앞(남측 가운데).
        n += Spawn(map, "default", 6f, 2.5f);

        // 강무진(전당포 주인, npcId=pawnshop) + 카운터 — 점포 안쪽.
        n += Npc (map, "NPC_Pawnshop", "pawnshop", 6f, 6.5f);
        n += Wall(map, "Pawn_Counter", 6f, 5.5f, 6f, 0.8f, "전당포 카운터");

        // 출구 트리거 — 밟으면 Safehouse/from_pawnshop으로 복귀(페이드+additive, 캐릭터 유지).
        n += Entrance(map, "Exit_ToSafehouse", 6f, 1.6f, 1.3f, 1f, "Safehouse", "from_pawnshop", isExit: true);

        // ── 저장(덮어쓰기) + 빌드세팅 등록 ──
        Selection.activeObject = null;
        if (!EditorSceneBuildUtil.SaveAndClose(scene, ScenePath, prevActive))
        {
            Debug.LogError("[PawnshopGB] 씬 저장 실패: " + ScenePath);
            return;
        }
        AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=cyan>[PawnshopGB]</color> 생성 완료: {ScenePath} — 그레이박스 {n}개(바닥/벽/스폰/강무진/카운터/출구트리거). 빌드세팅 등록.\n" +
                  "  • 안전가옥 전당포 건물 입구(BuildingEntrance→Pawnshop)로 진입, 출구 트리거(→Safehouse/from_pawnshop)로 복귀. 캐릭터 유지(일반 실내).");
        if (!Application.isBatchMode && !ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("Pawnshop Greybox",
                $"{ScenePath} 생성 + 빌드세팅 등록 완료.\n\n작은 점포 방(바닥+벽4) + 진입 스폰 + 강무진(pawnshop)+카운터 + 출구 BuildingEntrance 트리거(→Safehouse/from_pawnshop).\n" +
                "안전가옥 전당포 건물 입구가 여기로 트리거 전환합니다(캐릭터 유지).", "확인");
    }

    static void EnsureGreyboxPalette()
    {
        foreach (var id in RequiredPrefabIds)
            if (Resources.Load<GameObject>(PrefabRoot + id) == null) { GreyboxPaletteBuilder.Generate(); return; }
    }

    static void AddToBuildSettings(string scenePath)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == scenePath) return;
        list.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ── 헬퍼 (Safehouse/HideoutGreyboxLayout과 동일 기법) ──
    static int Wall(GameObject parent, string name, float cx, float cy, float lenX, float thickY, string label = null)
    {
        var go = Inst("gb_wall", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(cx, cy, 0f);
        go.transform.localScale    = new Vector3(lenX, thickY, 1f);
        CounterScaleLabel(go);
        if (!string.IsNullOrEmpty(label)) SetLabel(go, label);
        return 1;
    }

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
            var data = AssetDatabase.LoadAssetAtPath<NPCData>($"Assets/Resources/Data/NPC/{storyNpcId}.asset");
            var dp = so.FindProperty("npcData");
            if (dp != null && data != null) dp.objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return 1;
    }

    /// <summary>BuildingEntrance(트리거 전환) 마커 — gb_spawn 시각 박스 재사용 + BoxCollider2D(trigger) + BuildingEntrance.
    /// gb_exit/gb_spawn 프리팹엔 콜라이더가 없으므로 트리거 콜라이더를 직접 부착한다.</summary>
    static int Entrance(GameObject parent, string name, float x, float y, float w, float h,
        string targetScene, string spawnId, bool isExit)
    {
        // 시각 박스는 gb_exit(파란 '탈출' 마커) 재사용.
        var go = Inst("gb_exit", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);

        // 기존 gb_exit는 InteractableObject(ExitPoint, E키)를 갖는다 — 트리거 전환과 중복되니 제거.
        var oldIo = go.GetComponentInChildren<InteractableObject>();
        if (oldIo != null) Object.DestroyImmediate(oldIo);

        // 트리거 콜라이더 + BuildingEntrance.
        var box = go.GetComponent<BoxCollider2D>();
        if (box == null) box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(w, h);

        var be = go.GetComponent<BuildingEntrance>();
        if (be == null) be = go.AddComponent<BuildingEntrance>();
        be.Configure(targetScene, spawnId, isExit);

        SetLabel(go, isExit ? "출구" : "입구");
        return 1;
    }

    static GameObject Inst(string prefabId, string name, GameObject parent)
    {
        var prefab = Resources.Load<GameObject>(PrefabRoot + prefabId);
        if (prefab == null) { Debug.LogError($"[PawnshopGB] 프리팹 로드 실패: Resources/{PrefabRoot}{prefabId}"); return null; }
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
