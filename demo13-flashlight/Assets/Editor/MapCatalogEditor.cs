using UnityEngine;
using UnityEditor;
using IsometricMapEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 맵 빌더 카탈로그 에디터.
/// 타일/벽/프롭/건물 에셋을 생성하고 카탈로그에 자동 등록.
/// 메뉴: Tools > Dev Tools > Map > Catalog Editor
/// </summary>
public class MapCatalogEditor : EditorWindow
{
    const string CATALOG_PATH = "Assets/Resources/MapBuilder/MapBuilderCatalog.asset";
    const string ASSET_ROOT = "Assets/Resources/MapBuilder";

    MapBuilderCatalog catalog;
    SerializedObject so;
    Vector2 scrollPos;

    int tab; // 0=Tiles, 1=Walls, 2=Props, 3=Buildings
    string[] tabNames = { "Tiles", "Walls", "Props", "Buildings" };

    // 새 에셋 생성 폼
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

    static readonly Color HEADER_BG = new(0.2f, 0.2f, 0.25f, 1f);

    [MenuItem("Tools/Dev Tools/Map/Catalog Editor")]
    static void Open()
    {
        var win = GetWindow<MapCatalogEditor>("Map Catalog");
        win.minSize = new Vector2(500, 600);
        win.LoadCatalog();
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
        tab = GUILayout.Toolbar(tab, tabNames, GUILayout.Height(28));
        EditorGUILayout.Space(4);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        switch (tab)
        {
            case 0: DrawTileList(false); break;
            case 1: DrawTileList(true); break;
            case 2: DrawPropList(); break;
            case 3: DrawBuildingList(); break;
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
            EditorGUILayout.BeginHorizontal("box");

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

            if (GUILayout.Button("Select", GUILayout.Width(50)))
                Selection.activeObject = arr[i];

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                RemoveFromArray(wallMode ? "walls" : "tiles", i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    // ===== 프롭 목록 =====

    void DrawPropList()
    {
        EditorGUILayout.LabelField($"등록된 Props: {catalog.props.Length}개", EditorStyles.boldLabel);

        for (int i = 0; i < catalog.props.Length; i++)
        {
            if (catalog.props[i] == null) continue;
            var p = catalog.props[i];

            EditorGUILayout.BeginHorizontal("box");

            if (p.icon != null)
            {
                var rect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32));
                EditorGUI.DrawPreviewTexture(rect, p.icon.texture);
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(p.displayName ?? p.propId, EditorStyles.boldLabel);
            string info = $"풋프린트: {p.footprint.x}x{p.footprint.y}  차단: {(p.blocksWalkability ? "O" : "X")}  프리팹: {(p.prefab != null ? "O" : "X")}";
            EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Select", GUILayout.Width(50)))
                Selection.activeObject = p;
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                RemoveFromArray("props", i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    // ===== 건물 목록 =====

    void DrawBuildingList()
    {
        EditorGUILayout.LabelField($"등록된 Buildings: {catalog.buildings.Length}개", EditorStyles.boldLabel);

        for (int i = 0; i < catalog.buildings.Length; i++)
        {
            if (catalog.buildings[i] == null) continue;
            var b = catalog.buildings[i];

            EditorGUILayout.BeginHorizontal("box");

            if (b.icon != null)
            {
                var rect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32));
                EditorGUI.DrawPreviewTexture(rect, b.icon.texture);
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(b.displayName ?? b.buildingId, EditorStyles.boldLabel);
            string info = $"풋프린트: {b.footprint.x}x{b.footprint.y}  진입: {(b.isEnterable ? "O" : "X")}  프리팹: {(b.prefab != null ? "O" : "X")}";
            EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Select", GUILayout.Width(50)))
                Selection.activeObject = b;
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                RemoveFromArray("buildings", i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    // ===== 생성 폼 =====

    void DrawCreateForm()
    {
        // 접기/펼치기
        var bgStyle = new GUIStyle(EditorStyles.helpBox);
        showCreateForm = EditorGUILayout.Foldout(showCreateForm, "  + 새 에셋 생성 & 등록", true, EditorStyles.foldoutHeader);
        if (!showCreateForm) return;

        EditorGUILayout.BeginVertical("box");

        newId = EditorGUILayout.TextField("ID", newId);
        newName = EditorGUILayout.TextField("표시 이름", newName);

        switch (tab)
        {
            case 0: // Tile
                newSprite = (Sprite)EditorGUILayout.ObjectField("스프라이트", newSprite, typeof(Sprite), false);
                newMaterial = (Material)EditorGUILayout.ObjectField("머티리얼", newMaterial, typeof(Material), false);
                newSize = EditorGUILayout.Vector2IntField("크기 (셀)", newSize);
                newBlocksWalk = !EditorGUILayout.Toggle("이동 가능", !newBlocksWalk);
                break;

            case 1: // Wall
                newSprite = (Sprite)EditorGUILayout.ObjectField("스프라이트", newSprite, typeof(Sprite), false);
                newMaterial = (Material)EditorGUILayout.ObjectField("머티리얼", newMaterial, typeof(Material), false);
                newWallHeight = EditorGUILayout.FloatField("벽 높이", newWallHeight);
                newWallThickness = EditorGUILayout.FloatField("벽 두께", newWallThickness);
                break;

            case 2: // Prop
                newPrefab = (GameObject)EditorGUILayout.ObjectField("프리팹", newPrefab, typeof(GameObject), false);
                newIcon = (Sprite)EditorGUILayout.ObjectField("아이콘", newIcon, typeof(Sprite), false);
                newFootprint = EditorGUILayout.Vector2IntField("풋프린트", newFootprint);
                newBlocksWalk = EditorGUILayout.Toggle("이동 차단", newBlocksWalk);
                break;

            case 3: // Building
                newPrefab = (GameObject)EditorGUILayout.ObjectField("프리팹 (없으면 큐브 표시)", newPrefab, typeof(GameObject), false);
                newIcon = (Sprite)EditorGUILayout.ObjectField("아이콘", newIcon, typeof(Sprite), false);
                newFootprint = EditorGUILayout.Vector2IntField("풋프린트", newFootprint);
                newEnterable = EditorGUILayout.Toggle("진입 가능", newEnterable);
                break;
        }

        EditorGUILayout.Space(4);

        bool valid = !string.IsNullOrEmpty(newId);
        GUI.enabled = valid;

        if (GUILayout.Button("생성 & 카탈로그에 등록", GUILayout.Height(30)))
        {
            CreateAndRegister();
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
        }

        so = new SerializedObject(catalog);
        AssetDatabase.SaveAssets();
        Repaint();

        // 폼 리셋
        newId = "";
        newName = "";
        newSprite = null;
        newMaterial = null;
        newPrefab = null;
        newIcon = null;
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

        // 카탈로그에 추가
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

        var building = CreateInstance<BuildingDefinition>();
        building.buildingId = newId;
        building.displayName = string.IsNullOrEmpty(newName) ? newId : newName;
        building.prefab = newPrefab;
        building.icon = newIcon;
        building.footprint = newFootprint;
        building.isEnterable = newEnterable;

        string path = $"{folder}/{newId}.asset";
        AssetDatabase.CreateAsset(building, path);

        var list = new List<BuildingDefinition>(catalog.buildings) { building };
        catalog.buildings = list.ToArray();
        EditorUtility.SetDirty(catalog);

        Debug.Log($"<color=cyan>[Catalog]</color> Building 생성: {path}");
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
