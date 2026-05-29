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

    // Prop 빛 차폐(그림자 박스) 폼
    bool newCastsShadow;
    int newShadowPreset; // 0=직선,1=대각선,2=ㅅ자,3=V자,4=직접편집
    List<ShadowBox> newShadowBoxes = new();
    static readonly string[] SHADOW_PRESET_NAMES = { "직선", "대각선", "ㅅ자(Peak)", "V자(Valley)", "직접 편집" };
    Texture2D newBuildingTexture;
    float newBuildingScale = 3f;

    // MapObject fields
    MapObjectType newObjectType;
    int newVisualMode; // 0=Sphere, 1=TextureQuad, 2=Invisible, 3=EffectPrefab
    Texture2D newVisualTexture;
    GameObject newEffectPrefab;
    float newVisualScale = 1f;
    float newInteractRange = 2f;
    string newPromptText = "";

    static readonly Color HEADER_BG = new(0.2f, 0.2f, 0.25f, 1f);

    [MenuItem("Tools/Dev Tools/Map/Catalog Editor")]
    static void Open()
    {
        var win = GetWindow<MapCatalogEditor>("Map Catalog");
        win.minSize = new Vector2(500, 600);
        win.LoadCatalog();
        Debug.Log($"[MapCatalog] Editor opened. Tabs: {win.tabNames.Length} ({string.Join(", ", win.tabNames)})");
    }

    void OnEnable() => LoadCatalog();

    void LoadCatalog()
    {
        catalog = AssetDatabase.LoadAssetAtPath<MapBuilderCatalog>(CATALOG_PATH);
        if (catalog != null)
            so = new SerializedObject(catalog);
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
        if (tab != prevTab) { editingIndex = -1; ResetForm(); }
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

        EditorGUILayout.LabelField($"등록된 {label}: {arr.Length}개", EditorStyles.boldLabel);

        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == null) continue;
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
        EditorGUILayout.LabelField($"등록된 Props: {catalog.props.Length}개", EditorStyles.boldLabel);

        for (int i = 0; i < catalog.props.Length; i++)
        {
            if (catalog.props[i] == null) continue;
            var p = catalog.props[i];
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
        EditorGUILayout.LabelField($"등록된 Buildings: {catalog.buildings.Length}개", EditorStyles.boldLabel);

        for (int i = 0; i < catalog.buildings.Length; i++)
        {
            if (catalog.buildings[i] == null) continue;
            var b = catalog.buildings[i];
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
            string info = $"풋프린트: {b.footprint.x}x{b.footprint.y}  진입: {(b.isEnterable ? "O" : "X")}  프리팹: {(b.prefab != null ? "O" : "X")}";
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
        newMaterial = b.materialPreset;

        newBuildingTexture = null;
        newBuildingScale = 3f;
        if (b.prefab != null)
        {
            var r = b.prefab.GetComponentInChildren<Renderer>();
            if (r != null && r.sharedMaterial != null)
                newBuildingTexture = r.sharedMaterial.mainTexture as Texture2D;

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
        var quadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadObj.name = "Visual";
        quadObj.transform.SetParent(root.transform);
        quadObj.transform.localPosition = Vector3.zero;
        quadObj.transform.localRotation = Quaternion.Euler(90f, 0, 0);

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
        EditorGUILayout.LabelField($"등록된 MapObjects: {catalog.mapObjectDefs.Length}개", EditorStyles.boldLabel);

        for (int i = 0; i < catalog.mapObjectDefs.Length; i++)
        {
            if (catalog.mapObjectDefs[i] == null) continue;
            var d = catalog.mapObjectDefs[i];
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
        newCastsShadow = false;
        newShadowBoxes = new List<ShadowBox>();
        newShadowPreset = 0;
        newBuildingTexture = null;
        newBuildingScale = 3f;
        newObjectType = MapObjectType.SpawnPoint;
        newVisualMode = 0;
        newVisualTexture = null;
        newEffectPrefab = null;
        newVisualScale = 1f;
        newInteractRange = 2f;
        newPromptText = "";
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

        // ID — 편집 모드에서는 읽기전용
        if (IsEditing)
        {
            GUI.enabled = false;
            EditorGUILayout.TextField("ID", newId);
            GUI.enabled = true;
        }
        else
        {
            newId = EditorGUILayout.TextField("ID", newId);
        }

        // 표시 이름 (Prop/Building만)
        if (tab >= 2)
            newName = EditorGUILayout.TextField("표시 이름", newName);

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

            case 1: // Wall
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

            case 3: // Building
                newBuildingTexture = (Texture2D)EditorGUILayout.ObjectField("텍스쳐 (Quad 생성)", newBuildingTexture, typeof(Texture2D), false);
                newMaterial = (Material)EditorGUILayout.ObjectField("머티리얼 (선택)", newMaterial, typeof(Material), false);
                newBuildingScale = EditorGUILayout.FloatField("스케일", newBuildingScale);
                newFootprint = EditorGUILayout.Vector2IntField("풋프린트", newFootprint);
                newEnterable = EditorGUILayout.Toggle("진입 가능", newEnterable);
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

        EditorGUILayout.Space(4);

        bool valid = !string.IsNullOrEmpty(newId);
        GUI.enabled = valid;

        if (IsEditing)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("수정 저장", GUILayout.Height(30)))
            {
                switch (tab)
                {
                    case 0: SaveEditedTile(false); break;
                    case 1: SaveEditedTile(true); break;
                    case 2: SaveEditedProp(); break;
                    case 3: SaveEditedBuilding(); break;
                    case 4: SaveEditedMapObject(); break;
                }
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
                CreateAndRegister();
            }
        }

        GUI.enabled = true;

        EditorGUILayout.EndVertical();
    }

    void CreateAndRegister()
    {
        switch (tab)
        {
            case 0: CreateTile(false); break;
            case 1: CreateTile(true); break;
            case 2: CreateProp(); break;
            case 3: CreateBuilding(); break;
            case 4: CreateMapObject(); break;
        }

        so = new SerializedObject(catalog);
        AssetDatabase.SaveAssets();
        Repaint();

        ResetForm();
    }

    void CreateTile(bool isWall)
    {
        string folder = isWall ? $"{ASSET_ROOT}/Walls" : $"{ASSET_ROOT}/Tiles";
        EnsureFolder(folder);

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

        var prop = CreateInstance<PropDefinition>();
        prop.propId = newId;
        prop.displayName = string.IsNullOrEmpty(newName) ? newId : newName;
        prop.prefab = newPrefab;
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
        if (newBuildingTexture != null || newMaterial != null)
        {
            var root = new GameObject(newId);
            var quadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadObj.name = "Visual";
            quadObj.transform.SetParent(root.transform);
            quadObj.transform.localPosition = Vector3.zero;
            quadObj.transform.localRotation = Quaternion.Euler(90f, 0, 0);

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

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(sx * 0.9f, sy * 0.9f, 0.3f);
            box.center = new Vector3(0, sy * 0.5f, 0);

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
