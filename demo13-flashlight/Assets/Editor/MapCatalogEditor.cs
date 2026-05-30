using UnityEngine;
using UnityEditor;
using IsometricMapEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 맵 빌더 카탈로그 에디터.
/// 타일/벽/프롭/건물 에셋을 생성하고 카탈로그에 자동 등록.
/// 목록에서 "편집" 클릭 시 폼에 값이 로드되어 인라인 수정 가능.
/// 메뉴: Tools > Dev Tools > Map > Catalog Editor
/// </summary>
public class MapCatalogEditor : EditorWindow
{
    const string CATALOG_PATH = "Assets/Resources/MapBuilder/MapBuilderCatalog.asset";
    const string ASSET_ROOT = "Assets/Resources/MapBuilder";

    MapBuilderCatalog catalog;
    SerializedObject so;
    Vector2 scrollPos;

    int tab; // 0=Tiles, 1=Walls, 2=Props, 3=Buildings, 4=MapObjects
    string[] tabNames = { "Tiles", "Walls", "Props", "Buildings", "Objects" };

    // 편집 모드: -1이면 새로 생성, 0 이상이면 해당 인덱스의 기존 에셋 수정
    int editingIndex = -1;

    // 목록 접기/펼치기 + 검색
    bool listFoldout = true;
    string searchFilter = "";

    // 에셋 생성/수정 폼
    bool showCreateForm;
    string newId = "";
    string newName = "";
    Material newMaterial;
    Sprite newSprite;
    GameObject newPrefab;
    Sprite newIcon;
    Vector2Int newSize = Vector2Int.one;
    Vector2Int newFootprint = new(2, 2);
    float newWallHeight = 2.4f;
    float newWallThickness = 0.08f;
    bool newBlocksWalk;
    bool newEnterable;
    bool newOccludesInterior;

    // 빛 차폐(그림자 박스) 폼 (Prop/Wall/Building 공용)
    bool newCastsShadow;
    int newShadowPreset = 1; // 0=직선,1=대각선(기본),2=ㅅ자,3=V자,4=직접편집
    List<ShadowBox> newShadowBoxes = new();
    static readonly string[] SHADOW_PRESET_NAMES = { "직선", "대각선", "ㅅ자(Peak)", "V자(Valley)", "직접 편집" };
    Texture2D newBuildingTexture;
    float newBuildingScale = 5f;
    int newSortingOffset;

    // MapObject fields
    MapObjectType newObjectType;
    int newVisualMode; // 0=Sphere, 1=TextureQuad, 2=Invisible, 3=EffectPrefab
    Texture2D newVisualTexture;
    GameObject newEffectPrefab;
    float newVisualScale = 1f;
    float newInteractRange = 2f;
    string newPromptText = "";

    static readonly Color HEADER_BG = new(0.2f, 0.2f, 0.25f, 1f);

    // ===== Scene Preview =====
    GameObject _preview;
    bool _previewDirty;  // form → scene 동기화 필요 여부

    [MenuItem("Tools/Dev Tools/Map/Catalog Editor")]
    static void Open()
    {
        var win = GetWindow<MapCatalogEditor>("Map Catalog");
        win.minSize = new Vector2(500, 600);
        win.LoadCatalog();
        Debug.Log($"[MapCatalog] Editor opened. Tabs: {win.tabNames.Length} ({string.Join(", ", win.tabNames)})");
    }

    void OnEnable()
    {
        LoadCatalog();
        EditorApplication.update += OnEditorUpdate;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        DestroyPreview();
        EditorApplication.update -= OnEditorUpdate;
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void LoadCatalog()
    {
        catalog = AssetDatabase.LoadAssetAtPath<MapBuilderCatalog>(CATALOG_PATH);
        if (catalog != null)
        {
            so = new SerializedObject(catalog);
            // nextId가 0이면 기존 항목 수 기반으로 초기화
            if (catalog.nextId <= 0)
            {
                int total = catalog.tiles.Length + catalog.walls.Length
                          + catalog.props.Length + catalog.buildings.Length
                          + catalog.mapObjectDefs.Length;
                catalog.nextId = total + 1;
                EditorUtility.SetDirty(catalog);
            }
        }
    }

    void OnGUI()
    {
        if (catalog == null)
        {
            EditorGUILayout.HelpBox("MapBuilderCatalog이 없습니다.\nTools > Dev Tools > Map > Open Map Builder를 먼저 실행하세요.", MessageType.Warning);
            if (GUILayout.Button("카탈로그 생성", GUILayout.Height(30)))
            {
                MapBuilderSetup.OpenMapBuilder();
                LoadCatalog();
            }
            return;
        }

        so.Update();

        // 탭
        int prevTab = tab;
        tab = GUILayout.Toolbar(tab, tabNames, GUILayout.Height(28));
        if (tab != prevTab) { editingIndex = -1; ResetForm(); DestroyPreview(); searchFilter = ""; listFoldout = true; }
        EditorGUILayout.Space(4);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        switch (tab)
        {
            case 0: DrawTileList(false); break;
            case 1: DrawTileList(true); break;
            case 2: DrawPropList(); break;
            case 3: DrawBuildingList(); break;
            case 4: DrawMapObjectList(); break;
        }

        EditorGUILayout.Space(8);
        DrawCreateForm();

        EditorGUILayout.EndScrollView();

        if (so.hasModifiedProperties)
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }
    }

    // ===== 타일/벽 목록 =====

    void DrawTileList(bool wallMode)
    {
        var arr = wallMode ? catalog.walls : catalog.tiles;
        string label = wallMode ? "Walls" : "Tiles";

        listFoldout = EditorGUILayout.Foldout(listFoldout, $"등록된 {label}: {arr.Length}개", true, EditorStyles.foldoutHeader);
        if (!listFoldout) return;

        // 검색 바
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        if (!string.IsNullOrEmpty(searchFilter) && GUILayout.Button("✕", GUILayout.Width(22)))
            searchFilter = "";
        EditorGUILayout.EndHorizontal();

        string filter = searchFilter?.ToLowerInvariant() ?? "";

        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == null) continue;
            // 검색 필터 적용
            if (!string.IsNullOrEmpty(filter) && !arr[i].tileId.ToLowerInvariant().Contains(filter))
                continue;

            bool isEditing = editingIndex == i;
            var boxStyle = isEditing ? "selectionRect" : "box";
            EditorGUILayout.BeginHorizontal(boxStyle);

            // 스프라이트 미리보기
            if (arr[i].sprite != null)
            {
                var rect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32));
                EditorGUI.DrawPreviewTexture(rect, arr[i].sprite.texture);
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(arr[i].tileId, EditorStyles.boldLabel);

            string info = wallMode
                ? $"높이: {arr[i].wallHeight}m  두께: {arr[i].wallThickness}m"
                : $"카테고리: {arr[i].category}  크기: {arr[i].size.x}x{arr[i].size.y}  이동: {(arr[i].isWalkable ? "O" : "X")}";
            EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button(isEditing ? "편집중" : "편집", GUILayout.Width(50)))
            {
                if (isEditing) { editingIndex = -1; ResetForm(); }
                else LoadTileToForm(i, wallMode);
            }
            if (GUILayout.Button("Select", GUILayout.Width(50)))
                Selection.activeObject = arr[i];
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                if (editingIndex == i) editingIndex = -1;
                else if (editingIndex > i) editingIndex--;
                RemoveFromArray(wallMode ? "walls" : "tiles", i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    void LoadTileToForm(int index, bool wallMode)
    {
        var t = wallMode ? catalog.walls[index] : catalog.tiles[index];
        editingIndex = index;
        showCreateForm = true;
        newId = t.tileId;
        newName = "";
        newSprite = t.sprite;
        newMaterial = t.material;
        newSize = t.size;
        newBlocksWalk = !t.isWalkable;
        newWallHeight = t.wallHeight;
        newWallThickness = t.wallThickness;
        newCastsShadow = t.castsShadow;
        newShadowBoxes = t.shadowBoxes != null ? new List<ShadowBox>(t.shadowBoxes) : new List<ShadowBox>();
    }

    void SaveEditedTile(bool wallMode)
    {
        var arr = wallMode ? catalog.walls : catalog.tiles;
        if (editingIndex < 0 || editingIndex >= arr.Length) return;
        var tile = arr[editingIndex];
        if (tile == null) return;

        Undo.RecordObject(tile, "Edit Tile");

        tile.tileId = newId;
        tile.sprite = newSprite;
        tile.material = newMaterial;
        tile.size = newSize;
        tile.isWalkable = !newBlocksWalk;

        if (wallMode)
        {
            tile.wallHeight = newWallHeight;
            tile.wallThickness = newWallThickness;
        }

        EditorUtility.SetDirty(tile);
        AssetDatabase.SaveAssets();

        editingIndex = -1;
        ResetForm();
        Repaint();

        Debug.Log($"<color=yellow>[Catalog]</color> {(wallMode ? "Wall" : "Tile")} 수정 완료: {tile.tileId}");
    }

    // ===== 프롭 목록 =====

    void DrawPropList()
    {
        listFoldout = EditorGUILayout.Foldout(listFoldout, $"등록된 Props: {catalog.props.Length}개", true, EditorStyles.foldoutHeader);
        if (!listFoldout) return;

        // 검색 바
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        if (!string.IsNullOrEmpty(searchFilter) && GUILayout.Button("✕", GUILayout.Width(22)))
            searchFilter = "";
        EditorGUILayout.EndHorizontal();

        string filter = searchFilter?.ToLowerInvariant() ?? "";

        for (int i = 0; i < catalog.props.Length; i++)
        {
            if (catalog.props[i] == null) continue;
            var p = catalog.props[i];
            // 검색 필터 적용
            string searchTarget = (p.displayName ?? p.propId).ToLowerInvariant() + " " + p.propId.ToLowerInvariant();
            if (!string.IsNullOrEmpty(filter) && !searchTarget.Contains(filter))
                continue;
            bool isEditing = editingIndex == i;
            var boxStyle = isEditing ? "selectionRect" : "box";
            EditorGUILayout.BeginHorizontal(boxStyle);

            if (p.icon != null)
            {
                var rect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32));
                EditorGUI.DrawPreviewTexture(rect, p.icon.texture);
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(p.displayName ?? p.propId, EditorStyles.boldLabel);
            int shadowCount = p.castsShadow && p.shadowBoxes != null ? p.shadowBoxes.Length : 0;
        string info = $"풋프린트: {p.footprint.x}x{p.footprint.y}  차단: {(p.blocksWalkability ? "O" : "X")}  프리팹: {(p.prefab != null ? "O" : "X")}  빛차폐: {(p.castsShadow ? $"O({shadowCount})" : "X")}";
            EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button(isEditing ? "편집중" : "편집", GUILayout.Width(50)))
            {
                if (isEditing) { editingIndex = -1; ResetForm(); }
                else LoadPropToForm(i);
            }
            if (GUILayout.Button("Select", GUILayout.Width(50)))
                Selection.activeObject = p;
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                if (editingIndex == i) editingIndex = -1;
                else if (editingIndex > i) editingIndex--;
                RemoveFromArray("props", i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    void LoadPropToForm(int index)
    {
        var p = catalog.props[index];
        editingIndex = index;
        showCreateForm = true;
        newId = p.propId;
        newName = p.displayName ?? "";
        newPrefab = p.prefab;
        newIcon = p.icon;
        newFootprint = p.footprint;
        newBlocksWalk = p.blocksWalkability;
        newCastsShadow = p.castsShadow;
        newShadowBoxes = p.shadowBoxes != null ? new List<ShadowBox>(p.shadowBoxes) : new List<ShadowBox>();
        newShadowPreset = SHADOW_PRESET_NAMES.Length - 1; // 기존 값 로드 → 직접 편집
    }

    void SaveEditedProp()
    {
        if (editingIndex < 0 || editingIndex >= catalog.props.Length) return;
        var prop = catalog.props[editingIndex];
        if (prop == null) return;

        Undo.RecordObject(prop, "Edit Prop");

        prop.propId = newId;
        prop.displayName = string.IsNullOrEmpty(newName) ? newId : newName;
        prop.prefab = newPrefab;
        prop.icon = newIcon;
        prop.footprint = newFootprint;
        prop.blocksWalkability = newBlocksWalk;
        prop.castsShadow = newCastsShadow;
        prop.shadowBoxes = newCastsShadow ? newShadowBoxes.ToArray() : new ShadowBox[0];

        EditorUtility.SetDirty(prop);
        AssetDatabase.SaveAssets();

        editingIndex = -1;
        ResetForm();
        Repaint();

        Debug.Log($"<color=yellow>[Catalog]</color> Prop 수정 완료: {prop.propId}");
    }

    // ===== 건물 목록 =====

    void DrawBuildingList()
    {
        listFoldout = EditorGUILayout.Foldout(listFoldout, $"등록된 Buildings: {catalog.buildings.Length}개", true, EditorStyles.foldoutHeader);
        if (!listFoldout) return;

        // 검색 바
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        if (!string.IsNullOrEmpty(searchFilter) && GUILayout.Button("✕", GUILayout.Width(22)))
            searchFilter = "";
        EditorGUILayout.EndHorizontal();

        string filter = searchFilter?.ToLowerInvariant() ?? "";

        for (int i = 0; i < catalog.buildings.Length; i++)
        {
            if (catalog.buildings[i] == null) continue;
            var b = catalog.buildings[i];
            // 검색 필터 적용
            string searchTarget = (b.displayName ?? b.buildingId).ToLowerInvariant() + " " + b.buildingId.ToLowerInvariant();
            if (!string.IsNullOrEmpty(filter) && !searchTarget.Contains(filter))
                continue;
            bool isEditing = editingIndex == i;

            var boxStyle = isEditing ? "selectionRect" : "box";
            EditorGUILayout.BeginHorizontal(boxStyle);

            Texture buildingTex = null;
            if (b.prefab != null)
            {
                var r = b.prefab.GetComponentInChildren<Renderer>();
                if (r != null && r.sharedMaterial != null)
                    buildingTex = r.sharedMaterial.mainTexture;
            }
            if (buildingTex != null)
            {
                var rect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32));
                EditorGUI.DrawPreviewTexture(rect, buildingTex);
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(b.displayName ?? b.buildingId, EditorStyles.boldLabel);
            string info = $"풋프린트: {b.footprint.x}x{b.footprint.y}  진입: {(b.isEnterable ? "O" : "X")}  프리팹: {(b.prefab != null ? "O" : "X")}"
                + (b.occludesInterior ? "  투명" : "");
            EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button(isEditing ? "편집중" : "편집", GUILayout.Width(50)))
            {
                if (isEditing) { editingIndex = -1; ResetForm(); }
                else LoadBuildingToForm(i);
            }
            if (GUILayout.Button("Select", GUILayout.Width(50)))
                Selection.activeObject = b;
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                if (editingIndex == i) editingIndex = -1;
                else if (editingIndex > i) editingIndex--;
                RemoveFromArray("buildings", i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    void LoadBuildingToForm(int index)
    {
        var b = catalog.buildings[index];
        editingIndex = index;
        showCreateForm = true;
        newId = b.buildingId;
        newName = b.displayName ?? "";
        newFootprint = b.footprint;
        newEnterable = b.isEnterable;
        newOccludesInterior = b.occludesInterior;
        newSortingOffset = b.sortingOffset;
        newMaterial = b.materialPreset;

        newBuildingTexture = null;
        newBuildingScale = 5f;
        if (b.prefab != null)
        {
            var r = b.prefab.GetComponentInChildren<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                newBuildingTexture = r.sharedMaterial.mainTexture as Texture2D;
                // materialPreset이 비어있으면 실제 프리팹 머티리얼로 폼을 채워서
                // 편집 후 저장 시 머티리얼 연결이 풀리지 않도록 한다.
                if (newMaterial == null)
                    newMaterial = r.sharedMaterial;
            }

            var visual = b.prefab.transform.Find("Visual");
            if (visual != null)
            {
                float sx = visual.localScale.x;
                float sy = visual.localScale.y;
                newBuildingScale = Mathf.Max(sx, sy);
            }
        }
    }

    void SaveEditedBuilding()
    {
        if (editingIndex < 0 || editingIndex >= catalog.buildings.Length) return;
        var building = catalog.buildings[editingIndex];
        if (building == null) return;

        Undo.RecordObject(building, "Edit Building");

        building.buildingId = newId;
        building.displayName = string.IsNullOrEmpty(newName) ? newId : newName;
        building.footprint = newFootprint;
        building.isEnterable = newEnterable;
        building.occludesInterior = newOccludesInterior;
        building.sortingOffset = newSortingOffset;
        building.materialPreset = newMaterial;

        bool needsRebuild = false;
        if (building.prefab != null)
        {
            var visual = building.prefab.transform.Find("Visual");
            if (visual != null)
            {
                var r = visual.GetComponent<Renderer>();
                var currentTex = r != null && r.sharedMaterial != null ? r.sharedMaterial.mainTexture as Texture2D : null;
                float currentScale = Mathf.Max(visual.localScale.x, visual.localScale.y);
                if (currentTex != newBuildingTexture || Mathf.Abs(currentScale - newBuildingScale) > 0.01f)
                    needsRebuild = true;
            }
        }
        else if (newBuildingTexture != null)
        {
            needsRebuild = true;
        }

        if (needsRebuild)
            RebuildBuildingPrefab(building);

        EditorUtility.SetDirty(building);
        AssetDatabase.SaveAssets();

        editingIndex = -1;
        ResetForm();
        Repaint();

        Debug.Log($"<color=yellow>[Catalog]</color> Building 수정 완료: {building.buildingId}");
    }

    void RebuildBuildingPrefab(BuildingDefinition building)
    {
        string prefabPath = building.prefab != null
            ? AssetDatabase.GetAssetPath(building.prefab)
            : null;

        var root = new GameObject(building.buildingId);
        root.transform.rotation = Quaternion.Euler(35.264f, 45f, 0);
        var quadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadObj.name = "Visual";
        quadObj.transform.SetParent(root.transform);
        quadObj.transform.localPosition = Vector3.zero;
        quadObj.transform.localRotation = Quaternion.identity;

        float sx = newBuildingScale, sy = newBuildingScale;
        if (newBuildingTexture != null && newBuildingTexture.width > 0 && newBuildingTexture.height > 0)
        {
            float a = (float)newBuildingTexture.width / newBuildingTexture.height;
            if (a > 1f) sy = newBuildingScale / a; else sx = newBuildingScale * a;
        }
        quadObj.transform.localScale = new Vector3(sx, sy, 1f);

        var meshCol = quadObj.GetComponent<MeshCollider>();
        if (meshCol) DestroyImmediate(meshCol);

        var renderer = quadObj.GetComponent<MeshRenderer>();
        Material mat;
        if (newMaterial != null)
        {
            mat = new Material(newMaterial);
            if (newBuildingTexture) mat.SetTexture("_MainTex", newBuildingTexture);
        }
        else
        {
            Material existingMat = null;
            if (building.prefab != null)
            {
                var existR = building.prefab.GetComponentInChildren<Renderer>();
                if (existR != null) existingMat = existR.sharedMaterial;
            }

            if (existingMat != null)
            {
                mat = existingMat;
                if (newBuildingTexture) mat.SetTexture("_MainTex", newBuildingTexture);
                EditorUtility.SetDirty(mat);
            }
            else
            {
                var shader = Shader.Find("InkCity/CityBuilding") ?? Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                if (newBuildingTexture) mat.SetTexture("_MainTex", newBuildingTexture);
                mat.SetFloat("_Cutoff", 0.5f);
                mat.EnableKeyword("_MAGENTA_CLIP"); mat.SetFloat("_MagentaClip", 1f);
                mat.EnableKeyword("_GLOW_ON"); mat.SetFloat("_GlowToggle", 1f);
                mat.EnableKeyword("_HEIGHTFADE_ON"); mat.SetFloat("_HeightFadeToggle", 1f);

                mat.name = $"Mat_{building.buildingId}";
                EnsureFolder("Assets/Materials/Buildings");
                string matPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/Materials/Buildings/{mat.name}.mat");
                AssetDatabase.CreateAsset(mat, matPath);
                mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            }
        }
        renderer.sharedMaterial = mat;

        var box = root.AddComponent<BoxCollider>();
        box.size = new Vector3(sx * 0.9f, sy * 0.9f, 0.3f);
        box.center = new Vector3(0, sy * 0.5f, 0);

        if (string.IsNullOrEmpty(prefabPath))
        {
            EnsureFolder("Assets/Prefab/Buildings");
            prefabPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/Prefab/Buildings/{building.buildingId}.prefab");
        }
        building.prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);

        Debug.Log($"<color=green>[Catalog]</color> Building prefab 갱신: {prefabPath}");
    }

    // ===== 맵 오브젝트 목록 =====

    static readonly string[] VISUAL_MODE_NAMES = { "구체", "텍스처Quad", "투명", "이펙트" };

    void DrawMapObjectList()
    {
        listFoldout = EditorGUILayout.Foldout(listFoldout, $"등록된 MapObjects: {catalog.mapObjectDefs.Length}개", true, EditorStyles.foldoutHeader);
        if (!listFoldout) return;

        // 검색 바
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        if (!string.IsNullOrEmpty(searchFilter) && GUILayout.Button("✕", GUILayout.Width(22)))
            searchFilter = "";
        EditorGUILayout.EndHorizontal();

        string filter = searchFilter?.ToLowerInvariant() ?? "";

        for (int i = 0; i < catalog.mapObjectDefs.Length; i++)
        {
            if (catalog.mapObjectDefs[i] == null) continue;
            var d = catalog.mapObjectDefs[i];
            // 검색 필터 적용
            string searchTarget = (d.displayName ?? d.objectId).ToLowerInvariant() + " " + d.objectId.ToLowerInvariant();
            if (!string.IsNullOrEmpty(filter) && !searchTarget.Contains(filter))
                continue;
            bool isEditing = editingIndex == i;
            var boxStyle = isEditing ? "selectionRect" : "box";
            EditorGUILayout.BeginHorizontal(boxStyle);

            // 텍스처 프리뷰
            if (d.visualMode == 1 && d.visualTexture != null)
            {
                var rect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32));
                EditorGUI.DrawPreviewTexture(rect, d.visualTexture);
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(d.displayName ?? d.objectId, EditorStyles.boldLabel);
            string info = $"타입: {d.objectType}  비주얼: {VISUAL_MODE_NAMES[Mathf.Clamp(d.visualMode, 0, 3)]}  스케일: {d.visualScale:F1}";
            EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button(isEditing ? "편집중" : "편집", GUILayout.Width(50)))
            {
                if (isEditing) { editingIndex = -1; ResetForm(); }
                else LoadMapObjectToForm(i);
            }
            if (GUILayout.Button("Select", GUILayout.Width(50)))
                Selection.activeObject = d;
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                if (editingIndex == i) editingIndex = -1;
                else if (editingIndex > i) editingIndex--;
                RemoveFromArray("mapObjectDefs", i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    void LoadMapObjectToForm(int index)
    {
        var d = catalog.mapObjectDefs[index];
        editingIndex = index;
        showCreateForm = true;
        newId = d.objectId;
        newName = d.displayName ?? "";
        newObjectType = d.objectType;
        newVisualMode = d.visualMode;
        newVisualTexture = d.visualTexture;
        newEffectPrefab = d.effectPrefab;
        newVisualScale = d.visualScale;
        newInteractRange = d.interactRange;
        newPromptText = d.promptText ?? "";
    }

    void SaveEditedMapObject()
    {
        if (editingIndex < 0 || editingIndex >= catalog.mapObjectDefs.Length) return;
        var def = catalog.mapObjectDefs[editingIndex];
        if (def == null) return;

        Undo.RecordObject(def, "Edit MapObject");

        def.objectId = newId;
        def.displayName = string.IsNullOrEmpty(newName) ? newId : newName;
        def.objectType = newObjectType;
        def.visualMode = newVisualMode;
        def.visualTexture = newVisualTexture;
        def.effectPrefab = newEffectPrefab;
        def.visualScale = newVisualScale;
        def.interactRange = newInteractRange;
        def.promptText = newPromptText;

        EditorUtility.SetDirty(def);
        AssetDatabase.SaveAssets();

        editingIndex = -1;
        ResetForm();
        Repaint();

        Debug.Log($"<color=yellow>[Catalog]</color> MapObject 수정 완료: {def.objectId}");
    }

    // ===== 폼 리셋 =====

    void ResetForm()
    {
        DestroyPreview();
        newId = "";
        newName = "";
        newSprite = null;
        newMaterial = null;
        newPrefab = null;
        newIcon = null;
        newSize = Vector2Int.one;
        newFootprint = new Vector2Int(2, 2);
        newWallHeight = 2.4f;
        newWallThickness = 0.08f;
        newBlocksWalk = false;
        newEnterable = false;
        newOccludesInterior = false;
        newCastsShadow = false;
        newShadowBoxes = new List<ShadowBox>();
        newShadowPreset = 0;
        newBuildingTexture = null;
        newBuildingScale = 5f;
        newSortingOffset = 0;
        newObjectType = MapObjectType.SpawnPoint;
        newVisualMode = 0;
        newVisualTexture = null;
        newEffectPrefab = null;
        newVisualScale = 1f;
        newInteractRange = 2f;
        newPromptText = "";
    }

    // ===== Scene Preview 시스템 =====

    void OnEditorUpdate()
    {
        if (_preview == null) return;
        SyncSceneToForm();
    }

    void OnSceneGUI(SceneView sv)
    {
        if (_preview == null) return;
        // Scene View에 안내 라벨 표시
        Handles.BeginGUI();
        var style = new GUIStyle(EditorStyles.helpBox) { fontSize = 12, fontStyle = FontStyle.Bold };
        style.normal.textColor = Color.yellow;
        GUILayout.BeginArea(new Rect(10, 10, 300, 28));
        GUILayout.Label($"[카탈로그 미리보기] {newId}", style);
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    /// <summary>현재 탭/폼 상태에 맞는 프리뷰 오브젝트를 씬에 생성하거나 갱신한다.</summary>
    void SpawnOrUpdatePreview()
    {
        if (_preview == null)
        {
            _preview = new GameObject($"__CatalogPreview_{tab}__");
            _preview.hideFlags = HideFlags.DontSave;
            _preview.tag = "EditorOnly";
            _preview.transform.position = Vector3.zero;
        }

        UpdatePreviewVisual();

        // 씬 뷰 카메라를 프리뷰에 포커스
        Selection.activeGameObject = _preview;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();
    }

    void UpdatePreviewVisual()
    {
        if (_preview == null) return;

        // 자식 비주얼 모두 제거 후 재생성
        for (int i = _preview.transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(_preview.transform.GetChild(i).gameObject);

        // 기존 컴포넌트 제거 (SpriteRenderer 등)
        foreach (var c in _preview.GetComponents<Component>())
        {
            if (c is Transform) continue;
            DestroyImmediate(c);
        }

        // Root 회전 초기화
        // 바닥에 깔리는 것: 타일(0), Object TextureQuad(tab4+mode1) → (90,0,0)
        // 3D 직립: 벽(1) → identity (큐브가 Y축 직립)
        // 카메라 향: 프랍/건물/Object 기타 → (35.264,45,0)
        if (tab == 0 || (tab == 4 && newVisualMode == 1))
            _preview.transform.rotation = Quaternion.Euler(90, 0, 0);
        else if (tab == 1)
            _preview.transform.rotation = Quaternion.identity;
        else
            _preview.transform.rotation = Quaternion.Euler(35.264f, 45f, 0);
        _preview.transform.localScale = Vector3.one;

        switch (tab)
        {
            case 0: BuildTilePreview(); break;
            case 1: BuildWallPreview(); break;
            case 2: BuildPropPreview(); break;
            case 3: BuildBuildingPreview(); break;
            case 4: BuildObjectPreview(); break;
        }

        // ShadowProxy 미리보기 (프랍만 — 벽/건물은 3D 메시라 자체 차폐)
        if (newCastsShadow && newShadowBoxes.Count > 0 && tab == 2)
            ShadowProxyBuilder.Build(newShadowBoxes.ToArray(), _preview.transform, editorPreview: true);

        Selection.activeGameObject = _preview;
    }

    void BuildTilePreview()
    {
        if (newSprite == null) return;
        var sr = _preview.AddComponent<SpriteRenderer>();
        sr.sprite = newSprite;
        // root rotation은 UpdatePreviewVisual에서 Euler(90,0,0)으로 설정됨
        _preview.transform.localScale = new Vector3(newSize.x, newSize.y, 1f);
    }

    void BuildWallPreview()
    {
        float height = newWallHeight;
        float thickness = newWallThickness;

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "WallVisual";
        cube.hideFlags = HideFlags.DontSave;
        cube.transform.SetParent(_preview.transform, false);
        cube.transform.localPosition = new Vector3(0, height * 0.5f, 0);
        cube.transform.localScale = new Vector3(1f, height, thickness);

        var col = cube.GetComponent<Collider>();
        if (col) DestroyImmediate(col);

        var renderer = cube.GetComponent<Renderer>();
        Material mat = null;
        if (newMaterial != null)
        {
            mat = new Material(newMaterial);
        }
        else if (newSprite != null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.mainTexture = newSprite.texture;
        }
        else
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.6f, 0.6f, 0.65f, 1f);
        }
        mat.hideFlags = HideFlags.DontSave;
        renderer.sharedMaterial = mat;
    }

    void BuildPropPreview()
    {
        if (newPrefab != null)
        {
            var child = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab, _preview.transform);
            if (child != null)
            {
                child.hideFlags = HideFlags.DontSave;
                child.transform.localPosition = Vector3.zero;
                child.transform.localRotation = Quaternion.identity;
            }
        }
        else if (newIcon != null)
        {
            var sr = _preview.AddComponent<SpriteRenderer>();
            sr.sprite = newIcon;
            // root rotation은 UpdatePreviewVisual에서 Euler(35.264,45,0) 설정됨
        }
    }

    void BuildBuildingPreview()
    {
        // Root는 UpdatePreviewVisual에서 Euler(35.264,45,0) 설정됨 — 카메라 향
        var quadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadObj.name = "Visual";
        quadObj.hideFlags = HideFlags.DontSave;
        quadObj.transform.SetParent(_preview.transform, false);
        quadObj.transform.localPosition = Vector3.zero;
        quadObj.transform.localRotation = Quaternion.identity;

        float sx = newBuildingScale, sy = newBuildingScale;
        if (newBuildingTexture != null && newBuildingTexture.width > 0 && newBuildingTexture.height > 0)
        {
            float a = (float)newBuildingTexture.width / newBuildingTexture.height;
            if (a > 1f) sy = newBuildingScale / a; else sx = newBuildingScale * a;
        }
        quadObj.transform.localScale = new Vector3(sx, sy, 1f);

        var meshCol = quadObj.GetComponent<MeshCollider>();
        if (meshCol) DestroyImmediate(meshCol);

        var renderer = quadObj.GetComponent<MeshRenderer>();
        Material mat;
        if (newMaterial != null)
        {
            mat = new Material(newMaterial);
            if (newBuildingTexture) mat.SetTexture("_MainTex", newBuildingTexture);
        }
        else
        {
            var shader = Shader.Find("InkCity/CityBuilding") ?? Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(shader);
            if (newBuildingTexture) mat.SetTexture("_MainTex", newBuildingTexture);
            mat.SetFloat("_Cutoff", 0.5f);
            if (mat.HasProperty("_MagentaClip")) { mat.EnableKeyword("_MAGENTA_CLIP"); mat.SetFloat("_MagentaClip", 1f); }
            if (mat.HasProperty("_GlowToggle")) { mat.EnableKeyword("_GLOW_ON"); mat.SetFloat("_GlowToggle", 1f); }
            if (mat.HasProperty("_HeightFadeToggle")) { mat.EnableKeyword("_HEIGHTFADE_ON"); mat.SetFloat("_HeightFadeToggle", 1f); }
        }
        mat.hideFlags = HideFlags.DontSave;
        renderer.sharedMaterial = mat;
    }

    void BuildObjectPreview()
    {
        switch (newVisualMode)
        {
            case 0: // Sphere
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "SphereVisual";
                sphere.hideFlags = HideFlags.DontSave;
                sphere.transform.SetParent(_preview.transform, false);
                sphere.transform.localScale = Vector3.one * newVisualScale;
                var col0 = sphere.GetComponent<Collider>();
                if (col0) DestroyImmediate(col0);
                break;
            case 1: // TextureQuad (바닥 데칼 — root가 Euler(90,0,0)이므로 child는 identity)
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "QuadVisual";
                quad.hideFlags = HideFlags.DontSave;
                quad.transform.SetParent(_preview.transform, false);
                quad.transform.localRotation = Quaternion.identity;
                quad.transform.localScale = Vector3.one * newVisualScale;
                var col1 = quad.GetComponent<MeshCollider>();
                if (col1) DestroyImmediate(col1);
                if (newVisualTexture != null)
                {
                    var r = quad.GetComponent<Renderer>();
                    var m = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
                    m.mainTexture = newVisualTexture;
                    m.hideFlags = HideFlags.DontSave;
                    r.sharedMaterial = m;
                }
                break;
            case 3: // EffectPrefab
                if (newEffectPrefab != null)
                {
                    var fx = (GameObject)PrefabUtility.InstantiatePrefab(newEffectPrefab, _preview.transform);
                    if (fx != null)
                    {
                        fx.hideFlags = HideFlags.DontSave;
                        fx.transform.localPosition = Vector3.zero;
                        fx.transform.localScale = Vector3.one * newVisualScale;
                    }
                }
                break;
        }
    }

    /// <summary>씬에서 수정된 프리뷰 오브젝트의 변경사항을 폼에 반영한다.</summary>
    void SyncSceneToForm()
    {
        if (_preview == null) return;

        bool changed = false;

        switch (tab)
        {
            case 1: // Wall — 자식 큐브에서 높이/두께 역산
            {
                var wallVis = _preview.transform.Find("WallVisual");
                if (wallVis != null)
                {
                    float h = wallVis.localScale.y;
                    float t = wallVis.localScale.z;
                    if (Mathf.Abs(h - newWallHeight) > 0.001f) { newWallHeight = h; changed = true; }
                    if (Mathf.Abs(t - newWallThickness) > 0.001f) { newWallThickness = t; changed = true; }
                }
                break;
            }
            case 3: // Building — 자식 Visual 쿼드에서 스케일 역산
            {
                var visual = _preview.transform.Find("Visual");
                if (visual != null)
                {
                    float sx = visual.localScale.x;
                    float sy = visual.localScale.y;
                    float maxS = Mathf.Max(sx, sy);
                    if (Mathf.Abs(maxS - newBuildingScale) > 0.01f) { newBuildingScale = maxS; changed = true; }
                }
                break;
            }
            case 4: // Object — 자식에서 스케일
            {
                if (_preview.transform.childCount > 0)
                {
                    var child = _preview.transform.GetChild(0);
                    float s = Mathf.Max(child.localScale.x, child.localScale.y);
                    if (Mathf.Abs(s - newVisualScale) > 0.01f) { newVisualScale = s; changed = true; }
                }
                break;
            }
        }

        if (changed) Repaint();
    }

    void DestroyPreview()
    {
        if (_preview != null)
        {
            DestroyImmediate(_preview);
            _preview = null;
            SceneView.RepaintAll();
        }
    }

    /// <summary>프리뷰 GO를 프리팹으로 저장하고 반환한다. 카테고리별 Create에서 사용.</summary>
    GameObject SavePreviewAsPrefab(string prefabFolder, string prefabName)
    {
        if (_preview == null) return null;
        if (string.IsNullOrWhiteSpace(prefabFolder) || string.IsNullOrWhiteSpace(prefabName))
        {
            Debug.LogError("[Catalog] prefabFolder 또는 prefabName이 비어있습니다.");
            return null;
        }

        EnsureFolder(prefabFolder);

        // hideFlags 제거해서 프리팹 저장 가능하게
        _preview.hideFlags = HideFlags.None;
        _preview.name = prefabName;
        foreach (Transform child in _preview.GetComponentsInChildren<Transform>(true))
            child.gameObject.hideFlags = HideFlags.None;

        // 머티리얼 에셋 저장 (인스턴스 머티리얼 → 에셋)
        EnsureFolder("Assets/Materials/Catalog");
        foreach (var r in _preview.GetComponentsInChildren<Renderer>(true))
        {
            if (r.sharedMaterial != null && !AssetDatabase.Contains(r.sharedMaterial))
            {
                var mat = r.sharedMaterial;
                mat.hideFlags = HideFlags.None;
                string matPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/Materials/Catalog/{prefabName}_{mat.name}.mat");
                AssetDatabase.CreateAsset(mat, matPath);
                r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            }
        }

        // 에디터전용 ShadowProxy 제거 (런타임에서 재생성됨)
        var toDestroy = new List<GameObject>();
        foreach (Transform child in _preview.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child.name.StartsWith("ShadowProxy_"))
                toDestroy.Add(child.gameObject);
        }
        foreach (var go in toDestroy)
            DestroyImmediate(go);

        // 프리뷰 위치 리셋 (프리팹은 원점 기준)
        _preview.transform.position = Vector3.zero;

        string path = AssetDatabase.GenerateUniqueAssetPath($"{prefabFolder}/{prefabName}.prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(_preview, path);
        Debug.Log($"<color=green>[Catalog]</color> Prefab 생성: {path}");

        DestroyImmediate(_preview);
        _preview = null;

        return prefab;
    }

    // ===== 생성/수정 폼 =====

    bool IsEditing => editingIndex >= 0;

    string EditingLabel
    {
        get
        {
            if (!IsEditing) return "  + 새 에셋 생성 & 등록";
            switch (tab)
            {
                case 0: return $"  ✏ Tile 수정: {newId}";
                case 1: return $"  ✏ Wall 수정: {newId}";
                case 2: return $"  ✏ Prop 수정: {newName} ({newId})";
                case 3: return $"  ✏ Building 수정: {newName} ({newId})";
                case 4: return $"  ✏ MapObject 수정: {newName} ({newId})";
                default: return "  + 새 에셋 생성 & 등록";
            }
        }
    }

    // ===== Prop 빛 차폐(그림자 박스) 폼 =====
    void DrawShadowSection()
    {
        EditorGUILayout.Space(4);
        newCastsShadow = EditorGUILayout.Toggle(
            new GUIContent("벽(빛 차폐)", "체크 시 플래시라이트 빛을 막는 그림자 전용 박스를 함께 배치 (솔리드 벽/컨테이너용)"),
            newCastsShadow);

        if (!newCastsShadow) return;

        EditorGUI.indentLevel++;

        // 형태 프리셋: 직접 편집(마지막) 외 항목 선택 시 박스를 자동 생성
        int prev = newShadowPreset;
        newShadowPreset = EditorGUILayout.Popup("형태 프리셋", newShadowPreset, SHADOW_PRESET_NAMES);
        bool isCustom = newShadowPreset == SHADOW_PRESET_NAMES.Length - 1;
        if (newShadowPreset != prev && !isCustom)
            newShadowBoxes = new List<ShadowBox>(
                ShadowProxyBuilder.GetPreset((ShadowProxyBuilder.ShadowShape)newShadowPreset));

        // 토글 켰는데 박스가 비어있으면 직선 1개로 초기화
        if (newShadowBoxes.Count == 0 && !isCustom)
            newShadowBoxes = new List<ShadowBox>(
                ShadowProxyBuilder.GetPreset((ShadowProxyBuilder.ShadowShape)newShadowPreset));

        EditorGUILayout.HelpBox("대각선 벽=1박스(yaw 45), ㅅ/V자=2박스. 박스를 직접 수정하면 '직접 편집' 모드가 됩니다.", MessageType.None);

        for (int b = 0; b < newShadowBoxes.Count; b++)
        {
            var box = newShadowBoxes[b];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"박스 {b + 1}", EditorStyles.boldLabel);
            if (GUILayout.Button("삭제", GUILayout.Width(50)))
            {
                newShadowBoxes.RemoveAt(b);
                newShadowPreset = SHADOW_PRESET_NAMES.Length - 1;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            box.size = EditorGUILayout.Vector3Field("크기(길이,높이,두께)", box.size);
            box.offset = EditorGUILayout.Vector3Field("오프셋", box.offset);
            box.yaw = EditorGUILayout.FloatField("Y회전(대각선)", box.yaw);
            if (EditorGUI.EndChangeCheck())
                newShadowPreset = SHADOW_PRESET_NAMES.Length - 1; // 직접 편집으로 전환
            newShadowBoxes[b] = box;
            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("+ 박스 추가"))
        {
            newShadowBoxes.Add(new ShadowBox { size = new Vector3(1f, 2.4f, 0.1f), offset = new Vector3(0, 1.2f, 0) });
            newShadowPreset = SHADOW_PRESET_NAMES.Length - 1;
        }

        EditorGUI.indentLevel--;
    }

    void DrawCreateForm()
    {
        showCreateForm = EditorGUILayout.Foldout(showCreateForm, EditingLabel, true, EditorStyles.foldoutHeader);
        if (!showCreateForm) return;

        EditorGUILayout.BeginVertical("box");

        // ID — 자동 생성 (편집 모드에서는 기존 ID 표시)
        GUI.enabled = false;
        if (IsEditing)
        {
            EditorGUILayout.TextField("ID", newId);
        }
        else
        {
            newId = GenerateNextId();
            EditorGUILayout.TextField("ID", newId);
        }
        GUI.enabled = true;

        // 표시 이름 (Prop/Building만)
        if (tab >= 2)
            newName = EditorGUILayout.TextField("표시 이름", newName);

        // === 프리뷰 버튼 ===
        EditorGUILayout.BeginHorizontal();
        if (_preview != null)
        {
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("● 미리보기 ON", GUILayout.Height(22), GUILayout.Width(130)))
                DestroyPreview();
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("↻ 새로고침", GUILayout.Height(22), GUILayout.Width(80)))
                SpawnOrUpdatePreview();
            if (GUILayout.Button("포커스", GUILayout.Height(22), GUILayout.Width(50)))
            {
                Selection.activeGameObject = _preview;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }
        else
        {
            if (GUILayout.Button("씬 미리보기", GUILayout.Height(22), GUILayout.Width(130)))
                SpawnOrUpdatePreview();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);

        EditorGUI.BeginChangeCheck();

        switch (tab)
        {
            case 0: // Tile
                newSprite = (Sprite)EditorGUILayout.ObjectField("스프라이트", newSprite, typeof(Sprite), false);
                newMaterial = (Material)EditorGUILayout.ObjectField("머티리얼", newMaterial, typeof(Material), false);
                newSize = EditorGUILayout.Vector2IntField("크기 (셀)", newSize);
                newBlocksWalk = !EditorGUILayout.Toggle("이동 가능", !newBlocksWalk);
                if (newSprite != null)
                {
                    var rect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
                    EditorGUI.DrawPreviewTexture(rect, newSprite.texture, null, ScaleMode.ScaleToFit);
                }
                break;

            case 1: // Wall (3D 큐브 — 자체 빛 차폐, ShadowProxy 불필요)
                newSprite = (Sprite)EditorGUILayout.ObjectField("스프라이트", newSprite, typeof(Sprite), false);
                newMaterial = (Material)EditorGUILayout.ObjectField("머티리얼", newMaterial, typeof(Material), false);
                newWallHeight = EditorGUILayout.FloatField("벽 높이", newWallHeight);
                newWallThickness = EditorGUILayout.FloatField("벽 두께", newWallThickness);
                if (newSprite != null)
                {
                    var rect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
                    EditorGUI.DrawPreviewTexture(rect, newSprite.texture, null, ScaleMode.ScaleToFit);
                }
                break;

            case 2: // Prop
                newPrefab = (GameObject)EditorGUILayout.ObjectField("프리팹", newPrefab, typeof(GameObject), false);
                newIcon = (Sprite)EditorGUILayout.ObjectField("아이콘", newIcon, typeof(Sprite), false);
                newFootprint = EditorGUILayout.Vector2IntField("풋프린트", newFootprint);
                newBlocksWalk = EditorGUILayout.Toggle("이동 차단", newBlocksWalk);
                DrawShadowSection();
                break;

            case 3: // Building (3D 쿼드 — 자체 빛 차폐, ShadowProxy 불필요)
                newBuildingTexture = (Texture2D)EditorGUILayout.ObjectField("텍스쳐 (Quad 생성)", newBuildingTexture, typeof(Texture2D), false);
                newMaterial = (Material)EditorGUILayout.ObjectField("머티리얼 (선택)", newMaterial, typeof(Material), false);
                newBuildingScale = EditorGUILayout.FloatField("스케일", newBuildingScale);
                newFootprint = EditorGUILayout.Vector2IntField("풋프린트", newFootprint);
                newSortingOffset = EditorGUILayout.IntField(
                    new GUIContent("이미지 뎁스 (정렬 오프셋)", "값이 클수록 앞에 그려짐. 다른 프랍/바닥에 묻힐 때 +로 올린다. (스프라이트 기반에만 적용)"),
                    newSortingOffset);
                newEnterable = EditorGUILayout.Toggle("진입 가능", newEnterable);
                newOccludesInterior = EditorGUILayout.Toggle("투명 전환 (윗벽/천장)", newOccludesInterior);
                if (newBuildingTexture != null)
                {
                    var rect = GUILayoutUtility.GetRect(80, 80, GUILayout.ExpandWidth(false));
                    EditorGUI.DrawPreviewTexture(rect, newBuildingTexture, null, ScaleMode.ScaleToFit);
                }
                break;

            case 4: // MapObject
                newObjectType = (MapObjectType)EditorGUILayout.EnumPopup("오브젝트 타입", newObjectType);
                newVisualMode = EditorGUILayout.Popup("비주얼 모드", newVisualMode, VISUAL_MODE_NAMES);

                switch (newVisualMode)
                {
                    case 1: // TextureQuad
                        newVisualTexture = (Texture2D)EditorGUILayout.ObjectField("텍스처", newVisualTexture, typeof(Texture2D), false);
                        if (newVisualTexture != null)
                        {
                            var rect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
                            EditorGUI.DrawPreviewTexture(rect, newVisualTexture, null, ScaleMode.ScaleToFit);
                        }
                        break;
                    case 3: // EffectPrefab
                        newEffectPrefab = (GameObject)EditorGUILayout.ObjectField("이펙트 프리팹", newEffectPrefab, typeof(GameObject), false);
                        break;
                }

                newVisualScale = EditorGUILayout.FloatField("스케일", newVisualScale);
                newInteractRange = EditorGUILayout.FloatField("상호작용 범위", newInteractRange);
                newPromptText = EditorGUILayout.TextField("프롬프트 텍스트", newPromptText);
                break;
        }

        // 폼 변경 → 프리뷰 자동 갱신
        if (EditorGUI.EndChangeCheck() && _preview != null)
            SpawnOrUpdatePreview();

        EditorGUILayout.Space(4);

        GUI.enabled = true;

        if (IsEditing)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("수정 저장", GUILayout.Height(30)))
            {
                int capturedTab = tab;
                EditorApplication.delayCall += () =>
                {
                    switch (capturedTab)
                    {
                        case 0: SaveEditedTile(false); break;
                        case 1: SaveEditedTile(true); break;
                        case 2: SaveEditedProp(); break;
                        case 3: SaveEditedBuilding(); break;
                        case 4: SaveEditedMapObject(); break;
                    }
                };
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("편집 취소", GUILayout.Height(30), GUILayout.Width(80)))
            {
                editingIndex = -1;
                ResetForm();
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            if (GUILayout.Button("생성 & 카탈로그에 등록", GUILayout.Height(30)))
            {
                EditorApplication.delayCall += CreateAndRegister;
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.EndVertical();
    }

    void CreateAndRegister()
    {
        // ID 자동 생성 (혹시 비어있으면 재생성)
        if (string.IsNullOrWhiteSpace(newId))
            newId = GenerateNextId();

        switch (tab)
        {
            case 0: CreateTile(false); break;
            case 1: CreateTile(true); break;
            case 2: CreateProp(); break;
            case 3: CreateBuilding(); break;
            case 4: CreateMapObject(); break;
        }

        ConsumeNextId();
        so = new SerializedObject(catalog);
        AssetDatabase.SaveAssets();
        Repaint();

        ResetForm();
    }

    void CreateTile(bool isWall)
    {
        string folder = isWall ? $"{ASSET_ROOT}/Walls" : $"{ASSET_ROOT}/Tiles";
        EnsureFolder(folder);

        // 프리뷰가 있으면 프리팹으로 저장
        if (_preview != null)
        {
            string pfFolder = isWall ? "Assets/Prefab/Walls" : "Assets/Prefab/Tiles";
            SavePreviewAsPrefab(pfFolder, newId);
        }

        var tile = CreateInstance<TileDefinition>();
        tile.tileId = newId;
        tile.sprite = newSprite;
        tile.material = newMaterial;
        tile.category = isWall ? TileCategory.Wall : TileCategory.Ground;
        tile.size = newSize;
        tile.isWalkable = !newBlocksWalk;

        if (isWall)
        {
            tile.wallHeight = newWallHeight;
            tile.wallThickness = newWallThickness;
        }

        string path = $"{folder}/{newId}.asset";
        AssetDatabase.CreateAsset(tile, path);

        if (isWall)
        {
            var list = new List<TileDefinition>(catalog.walls) { tile };
            catalog.walls = list.ToArray();
        }
        else
        {
            var list = new List<TileDefinition>(catalog.tiles) { tile };
            catalog.tiles = list.ToArray();
        }
        EditorUtility.SetDirty(catalog);

        Debug.Log($"<color=cyan>[Catalog]</color> {(isWall ? "Wall" : "Tile")} 생성: {path}");
    }

    void CreateProp()
    {
        string folder = $"{ASSET_ROOT}/Props";
        EnsureFolder(folder);

        // 프리뷰가 있으면 프리팹으로 저장
        GameObject prefab = newPrefab;
        if (_preview != null)
            prefab = SavePreviewAsPrefab("Assets/Prefab/Props", newId);

        var prop = CreateInstance<PropDefinition>();
        prop.propId = newId;
        prop.displayName = string.IsNullOrEmpty(newName) ? newId : newName;
        prop.prefab = prefab;
        prop.icon = newIcon;
        prop.footprint = newFootprint;
        prop.blocksWalkability = newBlocksWalk;
        prop.castsShadow = newCastsShadow;
        prop.shadowBoxes = newCastsShadow ? newShadowBoxes.ToArray() : new ShadowBox[0];

        string path = $"{folder}/{newId}.asset";
        AssetDatabase.CreateAsset(prop, path);

        var list = new List<PropDefinition>(catalog.props) { prop };
        catalog.props = list.ToArray();
        EditorUtility.SetDirty(catalog);

        Debug.Log($"<color=cyan>[Catalog]</color> Prop 생성: {path}");
    }

    void CreateBuilding()
    {
        string folder = $"{ASSET_ROOT}/Buildings";
        EnsureFolder(folder);

        GameObject prefab = null;

        // 프리뷰가 있으면 그대로 프리팹으로 저장
        if (_preview != null)
        {
            prefab = SavePreviewAsPrefab("Assets/Prefab/Buildings", newId);
        }
        else if (newBuildingTexture != null || newMaterial != null)
        {
            // 프리뷰 없이 생성하는 폴백
            var root = new GameObject(newId);
            root.transform.rotation = Quaternion.Euler(35.264f, 45f, 0);
            var quadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadObj.name = "Visual";
            quadObj.transform.SetParent(root.transform);
            quadObj.transform.localPosition = Vector3.zero;
            quadObj.transform.localRotation = Quaternion.identity;

            float sx = newBuildingScale, sy = newBuildingScale;
            if (newBuildingTexture != null && newBuildingTexture.width > 0 && newBuildingTexture.height > 0)
            {
                float a = (float)newBuildingTexture.width / newBuildingTexture.height;
                if (a > 1f) sy = newBuildingScale / a; else sx = newBuildingScale * a;
            }
            quadObj.transform.localScale = new Vector3(sx, sy, 1f);

            var meshCol = quadObj.GetComponent<MeshCollider>();
            if (meshCol) DestroyImmediate(meshCol);

            var renderer = quadObj.GetComponent<MeshRenderer>();
            Material mat;
            if (newMaterial != null)
            {
                mat = new Material(newMaterial);
                if (newBuildingTexture) mat.SetTexture("_MainTex", newBuildingTexture);
            }
            else
            {
                var shader = Shader.Find("InkCity/CityBuilding") ?? Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                if (newBuildingTexture) mat.SetTexture("_MainTex", newBuildingTexture);
                mat.SetFloat("_Cutoff", 0.5f);
                mat.EnableKeyword("_MAGENTA_CLIP"); mat.SetFloat("_MagentaClip", 1f);
                mat.EnableKeyword("_GLOW_ON"); mat.SetFloat("_GlowToggle", 1f);
                mat.EnableKeyword("_HEIGHTFADE_ON"); mat.SetFloat("_HeightFadeToggle", 1f);
            }
            mat.name = $"Mat_{newId}";

            EnsureFolder("Assets/Materials/Buildings");
            string matPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/Materials/Buildings/{mat.name}.mat");
            AssetDatabase.CreateAsset(mat, matPath);
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            EnsureFolder("Assets/Prefab/Buildings");
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/Prefab/Buildings/{newId}.prefab");
            prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);

            Debug.Log($"<color=green>[Catalog]</color> Building prefab 생성: {prefabPath}");
        }

        var building = CreateInstance<BuildingDefinition>();
        building.buildingId = newId;
        building.displayName = string.IsNullOrEmpty(newName) ? newId : newName;
        building.prefab = prefab;
        building.footprint = newFootprint;
        building.isEnterable = newEnterable;
        building.occludesInterior = newOccludesInterior;
        building.sortingOffset = newSortingOffset;
        building.materialPreset = newMaterial;

        string path = $"{folder}/{newId}.asset";
        AssetDatabase.CreateAsset(building, path);

        var list = new List<BuildingDefinition>(catalog.buildings) { building };
        catalog.buildings = list.ToArray();
        EditorUtility.SetDirty(catalog);

        Debug.Log($"<color=cyan>[Catalog]</color> Building 생성: {path}");
    }

    void CreateMapObject()
    {
        string folder = $"{ASSET_ROOT}/MapObjects";
        EnsureFolder(folder);

        // 프리뷰가 있으면 이펙트 프리팹으로 저장
        if (_preview != null && newVisualMode == 3)
        {
            var savedPrefab = SavePreviewAsPrefab("Assets/Prefab/MapObjects", newId);
            if (savedPrefab != null) newEffectPrefab = savedPrefab;
        }
        else
        {
            DestroyPreview();
        }

        var def = CreateInstance<MapObjectDefinition>();
        def.objectId = newId;
        def.displayName = string.IsNullOrEmpty(newName) ? newId : newName;
        def.objectType = newObjectType;
        def.visualMode = newVisualMode;
        def.visualTexture = newVisualTexture;
        def.effectPrefab = newEffectPrefab;
        def.visualScale = newVisualScale;
        def.interactRange = newInteractRange;
        def.promptText = newPromptText;

        string path = $"{folder}/{newId}.asset";
        AssetDatabase.CreateAsset(def, path);

        var list = new List<MapObjectDefinition>(catalog.mapObjectDefs) { def };
        catalog.mapObjectDefs = list.ToArray();
        EditorUtility.SetDirty(catalog);

        Debug.Log($"<color=cyan>[Catalog]</color> MapObject 생성: {path}");
    }

    // ===== 유틸 =====

    void RemoveFromArray(string propName, int index)
    {
        var sp = so.FindProperty(propName);
        sp.DeleteArrayElementAtIndex(index);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(catalog);
    }

    /// <summary>현재 탭 접두어 + 전역 카운터로 ID를 자동 생성한다.</summary>
    string GenerateNextId()
    {
        string prefix = tab switch
        {
            0 => "tile",
            1 => "wall",
            2 => "prop",
            3 => "bld",
            4 => "obj",
            _ => "item"
        };
        return $"{prefix}_{catalog.nextId:D4}";
    }

    /// <summary>전역 ID 카운터를 증가시키고 저장한다.</summary>
    void ConsumeNextId()
    {
        catalog.nextId++;
        EditorUtility.SetDirty(catalog);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path).Replace("\\", "/");
        var folder = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
