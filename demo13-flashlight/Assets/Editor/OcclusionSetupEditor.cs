using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 벽 가림 처리(Occlusion) 통합 셋업 에디터.
/// 메뉴: Tools > Dev Tools > Visual > Occlusion Setup
///
/// 기능:
/// 1. 선택한 플레이어/적에 WallOcclusionOutline 자동 부착
/// 2. 선택한 건물에 BuildingInterior 자동 부착
/// 3. 씬 전체 일괄 적용
/// </summary>
public class OcclusionSetupEditor : EditorWindow
{
    enum SetupMode { Unit, Building }

    SetupMode mode = SetupMode.Unit;
    Vector2 scrollPos;

    // ── 유닛 설정 ──
    Color playerOutlineColor = new Color(1f, 1f, 1f, 0.7f);
    Color enemyOutlineColor = new Color(1f, 0.3f, 0.2f, 0.7f);
    float outlineWidth = 2f;
    LayerMask wallLayer = ~0;

    // ── 건물 설정 ──
    float fadeAlpha = 0.15f;
    float fadeSpeed = 5f;

    [MenuItem("Tools/Dev Tools/Visual/Occlusion Setup")]
    static void Open()
    {
        var window = GetWindow<OcclusionSetupEditor>("Occlusion Setup");
        window.minSize = new Vector2(400, 500);
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("벽 가림 처리 (Occlusion) 셋업", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        mode = (SetupMode)GUILayout.Toolbar((int)mode, new[] { "유닛 (플레이어/적)", "건물" });
        EditorGUILayout.Space(10);

        if (mode == SetupMode.Unit)
            DrawUnitSection();
        else
            DrawBuildingSection();

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        DrawBatchSection();

        EditorGUILayout.EndScrollView();
    }

    // ══════════════════════════════════════
    // 유닛 섹션
    // ══════════════════════════════════════

    void DrawUnitSection()
    {
        EditorGUILayout.LabelField("아웃라인 설정", EditorStyles.boldLabel);
        playerOutlineColor = EditorGUILayout.ColorField("플레이어 아웃라인 색", playerOutlineColor);
        enemyOutlineColor = EditorGUILayout.ColorField("적 아웃라인 색", enemyOutlineColor);
        outlineWidth = EditorGUILayout.Slider("아웃라인 두께", outlineWidth, 0.5f, 6f);
        wallLayer = EditorGUILayout.MaskField("벽 레이어", wallLayer, UnityEditorInternal.InternalEditorUtility.layers);

        EditorGUILayout.Space(10);

        // 선택된 오브젝트 정보
        var selected = Selection.gameObjects;
        int playerCount = 0, enemyCount = 0;
        foreach (var go in selected)
        {
            if (go.GetComponent<PlayerController>() != null) playerCount++;
            else if (go.GetComponent<EnemyController>() != null) enemyCount++;
        }

        EditorGUILayout.HelpBox(
            $"선택된 오브젝트: {selected.Length}개\n" +
            $"  플레이어: {playerCount}개\n" +
            $"  적: {enemyCount}개",
            playerCount + enemyCount > 0 ? MessageType.Info : MessageType.Warning);

        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
        if (GUILayout.Button("선택 유닛에 아웃라인 적용", GUILayout.Height(30)))
            ApplyOutlineToSelected();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(1f, 0.6f, 0.3f);
        if (GUILayout.Button("선택 유닛에서 아웃라인 제거", GUILayout.Height(25)))
            RemoveOutlineFromSelected();
        GUI.backgroundColor = Color.white;
    }

    void ApplyOutlineToSelected()
    {
        int count = 0;
        foreach (var go in Selection.gameObjects)
        {
            bool isPlayer = go.GetComponent<PlayerController>() != null;
            bool isEnemy = go.GetComponent<EnemyController>() != null;

            if (!isPlayer && !isEnemy) continue;

            Undo.RecordObject(go, "Add WallOcclusionOutline");

            var outline = go.GetComponent<WallOcclusionOutline>();
            if (outline == null)
                outline = Undo.AddComponent<WallOcclusionOutline>(go);

            var so = new SerializedObject(outline);
            so.FindProperty("outlineColor").colorValue = isPlayer ? playerOutlineColor : enemyOutlineColor;
            so.FindProperty("outlineWidth").floatValue = outlineWidth;
            so.FindProperty("wallLayerMask").intValue = wallLayer;
            so.FindProperty("autoExcludeOwnLayer").boolValue = true;
            so.FindProperty("centerOffset").vector3Value = new Vector3(0, 0.5f, 0);
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(go);
            count++;
        }

        if (count > 0)
            Debug.Log($"<color=green>[Occlusion Setup]</color> {count}개 유닛에 아웃라인 적용 완료");
        else
            Debug.LogWarning("[Occlusion Setup] 선택된 오브젝트 중 PlayerController/EnemyController가 없습니다");
    }

    void RemoveOutlineFromSelected()
    {
        int count = 0;
        foreach (var go in Selection.gameObjects)
        {
            var outline = go.GetComponent<WallOcclusionOutline>();
            if (outline == null) continue;

            // 자동 생성된 Quad 제거
            var quad = go.transform.Find("OcclusionOutlineQuad");
            if (quad == null)
            {
                // Root 아래에 있을 수도 있음
                var root = go.transform.Find("Root");
                if (root != null) quad = root.Find("OcclusionOutlineQuad");
            }
            if (quad != null) Undo.DestroyObjectImmediate(quad.gameObject);

            Undo.DestroyObjectImmediate(outline);
            count++;
        }

        if (count > 0)
            Debug.Log($"<color=orange>[Occlusion Setup]</color> {count}개 유닛에서 아웃라인 제거");
    }

    // ══════════════════════════════════════
    // 건물 섹션
    // ══════════════════════════════════════

    void DrawBuildingSection()
    {
        EditorGUILayout.LabelField("건물 내부 진입 페이드 설정", EditorStyles.boldLabel);
        fadeAlpha = EditorGUILayout.Slider("목표 알파", fadeAlpha, 0f, 0.5f);
        fadeSpeed = EditorGUILayout.Slider("페이드 속도", fadeSpeed, 1f, 15f);

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "사용법:\n" +
            "1. 건물 루트 오브젝트 (또는 벽 부모) 선택\n" +
            "2. '건물에 인테리어 적용' 클릭\n" +
            "3. → Box Collider (Trigger) + BuildingInterior 자동 추가\n" +
            "4. → 자식 MeshRenderer(CityWall/CityBuilding 셰이더) 자동 수집\n" +
            "5. Inspector에서 트리거 크기, fadeRenderers 조정",
            MessageType.Info);

        EditorGUILayout.Space(5);

        var selected = Selection.gameObjects;
        EditorGUILayout.HelpBox($"선택된 오브젝트: {selected.Length}개", MessageType.None);

        GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
        if (GUILayout.Button("선택 건물에 인테리어 적용", GUILayout.Height(30)))
            ApplyBuildingInteriorToSelected();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(1f, 0.6f, 0.3f);
        if (GUILayout.Button("선택 건물에서 인테리어 제거", GUILayout.Height(25)))
            RemoveBuildingInteriorFromSelected();
        GUI.backgroundColor = Color.white;
    }

    void ApplyBuildingInteriorToSelected()
    {
        int count = 0;
        foreach (var go in Selection.gameObjects)
        {
            Undo.RecordObject(go, "Add BuildingInterior");

            // BuildingInterior 추가
            var interior = go.GetComponent<BuildingInterior>();
            if (interior == null)
                interior = Undo.AddComponent<BuildingInterior>(go);

            // Box Collider (Trigger) 확인/추가
            var boxCol = go.GetComponent<BoxCollider>();
            if (boxCol == null)
            {
                boxCol = Undo.AddComponent<BoxCollider>(go);
                // 건물 자식 Renderer들의 바운드로 크기 자동 산정
                var bounds = CalculateBounds(go);
                boxCol.center = go.transform.InverseTransformPoint(bounds.center);
                boxCol.size = go.transform.InverseTransformVector(bounds.size);
            }
            boxCol.isTrigger = true;

            // 자식 Renderer 중 CityWall/CityBuilding 셰이더 사용하는 것 수집
            var wallRenderers = CollectWallRenderers(go);

            var so = new SerializedObject(interior);
            var fadeArr = so.FindProperty("fadeRenderers");
            fadeArr.arraySize = wallRenderers.Count;
            for (int i = 0; i < wallRenderers.Count; i++)
                fadeArr.GetArrayElementAtIndex(i).objectReferenceValue = wallRenderers[i];

            so.FindProperty("fadeAlpha").floatValue = fadeAlpha;
            so.FindProperty("fadeSpeed").floatValue = fadeSpeed;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(go);
            count++;

            Debug.Log($"<color=cyan>[Occlusion Setup]</color> {go.name}: BuildingInterior 적용 " +
                      $"(페이드 대상 {wallRenderers.Count}개 Renderer)");
        }

        if (count == 0)
            Debug.LogWarning("[Occlusion Setup] 선택된 오브젝트가 없습니다");
    }

    void RemoveBuildingInteriorFromSelected()
    {
        int count = 0;
        foreach (var go in Selection.gameObjects)
        {
            var interior = go.GetComponent<BuildingInterior>();
            if (interior != null)
            {
                Undo.DestroyObjectImmediate(interior);
                count++;
            }
        }

        if (count > 0)
            Debug.Log($"<color=orange>[Occlusion Setup]</color> {count}개 건물에서 인테리어 제거");
    }

    /// <summary>자식 중 CityWall / CityBuilding 셰이더를 사용하는 Renderer 수집</summary>
    List<Renderer> CollectWallRenderers(GameObject root)
    {
        var result = new List<Renderer>();
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r.sharedMaterial == null) continue;
            var shaderName = r.sharedMaterial.shader.name;
            if (shaderName == "InkCity/CityWall" || shaderName == "InkCity/CityBuilding")
                result.Add(r);
        }
        return result;
    }

    /// <summary>자식 Renderer 전체의 합산 Bounds</summary>
    Bounds CalculateBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.one * 2f);

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        // 약간 여유
        bounds.Expand(0.5f);
        return bounds;
    }

    // ══════════════════════════════════════
    // 일괄 적용 섹션
    // ══════════════════════════════════════

    void DrawBatchSection()
    {
        EditorGUILayout.LabelField("씬 일괄 적용", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "현재 씬의 모든 플레이어/적에 아웃라인을 일괄 적용합니다.\n" +
            "이미 적용된 유닛은 스킵됩니다.",
            MessageType.None);

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button("씬 전체 유닛 적용", GUILayout.Height(28)))
            BatchApplyUnits();

        GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
        if (GUILayout.Button("씬 전체 건물 적용", GUILayout.Height(28)))
            BatchApplyBuildings();

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(1f, 0.4f, 0.3f);
        if (GUILayout.Button("씬 전체 제거 (유닛+건물)", GUILayout.Height(25)))
            BatchRemoveAll();
        GUI.backgroundColor = Color.white;
    }

    void BatchApplyUnits()
    {
        int count = 0;

        // 플레이어
        var players = FindObjectsOfType<PlayerController>(true);
        foreach (var p in players)
        {
            if (p.GetComponent<WallOcclusionOutline>() != null) continue;
            var outline = Undo.AddComponent<WallOcclusionOutline>(p.gameObject);
            var so = new SerializedObject(outline);
            so.FindProperty("outlineColor").colorValue = playerOutlineColor;
            so.FindProperty("outlineWidth").floatValue = outlineWidth;
            so.FindProperty("wallLayerMask").intValue = wallLayer;
            so.ApplyModifiedProperties();
            count++;
        }

        // 적
        var enemies = FindObjectsOfType<EnemyController>(true);
        foreach (var e in enemies)
        {
            if (e.GetComponent<WallOcclusionOutline>() != null) continue;
            var outline = Undo.AddComponent<WallOcclusionOutline>(e.gameObject);
            var so = new SerializedObject(outline);
            so.FindProperty("outlineColor").colorValue = enemyOutlineColor;
            so.FindProperty("outlineWidth").floatValue = outlineWidth;
            so.FindProperty("wallLayerMask").intValue = wallLayer;
            so.ApplyModifiedProperties();
            count++;
        }

        Debug.Log($"<color=green>[Occlusion Setup]</color> 씬 전체: {count}개 유닛에 아웃라인 적용 " +
                  $"(플레이어 {players.Length}, 적 {enemies.Length})");
    }

    void BatchApplyBuildings()
    {
        // CityWall / CityBuilding 셰이더를 쓰는 Renderer의 최상위 부모를 건물로 간주
        var allRenderers = FindObjectsOfType<Renderer>(true);
        var buildingRoots = new HashSet<GameObject>();

        foreach (var r in allRenderers)
        {
            if (r.sharedMaterial == null) continue;
            var shaderName = r.sharedMaterial.shader.name;
            if (shaderName != "InkCity/CityWall" && shaderName != "InkCity/CityBuilding") continue;

            // 최상위 부모 (루트) 찾기 — 단, 씬 루트는 제외
            var root = r.transform;
            while (root.parent != null && root.parent.GetComponent<Renderer>() == null
                   && root.parent.name != root.parent.gameObject.scene.name)
            {
                // 건물 루트: 부모에 Renderer가 없는 첫 번째 레벨
                if (root.parent.GetComponent<BuildingInterior>() != null)
                { root = root.parent; break; }
                root = root.parent;
            }

            buildingRoots.Add(root.gameObject);
        }

        int count = 0;
        foreach (var bldg in buildingRoots)
        {
            if (bldg.GetComponent<BuildingInterior>() != null) continue;

            // 선택 시뮬레이션
            var oldSelection = Selection.gameObjects;
            Selection.activeGameObject = bldg;

            var interior = Undo.AddComponent<BuildingInterior>(bldg);
            var wallRends = CollectWallRenderers(bldg);

            var boxCol = bldg.GetComponent<BoxCollider>();
            if (boxCol == null)
            {
                boxCol = Undo.AddComponent<BoxCollider>(bldg);
                var bounds = CalculateBounds(bldg);
                boxCol.center = bldg.transform.InverseTransformPoint(bounds.center);
                boxCol.size = bldg.transform.InverseTransformVector(bounds.size);
            }
            boxCol.isTrigger = true;

            var so = new SerializedObject(interior);
            var fadeArr = so.FindProperty("fadeRenderers");
            fadeArr.arraySize = wallRends.Count;
            for (int i = 0; i < wallRends.Count; i++)
                fadeArr.GetArrayElementAtIndex(i).objectReferenceValue = wallRends[i];
            so.FindProperty("fadeAlpha").floatValue = fadeAlpha;
            so.FindProperty("fadeSpeed").floatValue = fadeSpeed;
            so.ApplyModifiedProperties();

            Selection.objects = oldSelection;
            count++;
        }

        Debug.Log($"<color=cyan>[Occlusion Setup]</color> 씬 전체: {count}개 건물에 인테리어 적용");
    }

    void BatchRemoveAll()
    {
        int unitCount = 0, buildingCount = 0;

        // 유닛 아웃라인 제거
        var outlines = FindObjectsOfType<WallOcclusionOutline>(true);
        foreach (var o in outlines)
        {
            var quad = o.transform.Find("OcclusionOutlineQuad");
            if (quad == null)
            {
                var root = o.transform.Find("Root");
                if (root != null) quad = root.Find("OcclusionOutlineQuad");
            }
            if (quad != null) Undo.DestroyObjectImmediate(quad.gameObject);
            Undo.DestroyObjectImmediate(o);
            unitCount++;
        }

        // 건물 인테리어 제거
        var interiors = FindObjectsOfType<BuildingInterior>(true);
        foreach (var bi in interiors)
        {
            Undo.DestroyObjectImmediate(bi);
            buildingCount++;
        }

        Debug.Log($"<color=red>[Occlusion Setup]</color> 전체 제거: 유닛 {unitCount}, 건물 {buildingCount}");
    }
}
