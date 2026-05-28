using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using IsometricMapEditor;
using System.IO;

/// <summary>
/// 맵 빌더 초기 세팅 자동화.
/// 카탈로그 SO 생성 + 기존 타일 에셋 자동 등록 + 전용 씬 생성.
/// 메뉴: Tools > Dev Tools > Map > Open Map Builder
/// </summary>
public static class MapBuilderSetup
{
    const string CATALOG_PATH = "Assets/Resources/MapBuilder/MapBuilderCatalog.asset";
    const string SCENE_DIR = "Assets/Scenes";
    const string SCENE_PATH = "Assets/Scenes/MapBuilder.unity";

    [MenuItem("Tools/Dev Tools/Map/Open Map Builder")]
    public static void OpenMapBuilder()
    {
        // 1. 카탈로그 확보 (없으면 생성 + 자동 등록)
        var catalog = AssetDatabase.LoadAssetAtPath<MapBuilderCatalog>(CATALOG_PATH);
        if (catalog == null)
        {
            catalog = CreateCatalog();
            Debug.Log($"<color=cyan>[MapBuilder]</color> 카탈로그 생성: {CATALOG_PATH}");
        }

        // 2. 씬 확보 (없으면 생성)
        if (!File.Exists(SCENE_PATH))
        {
            CreateMapBuilderScene(catalog);
            Debug.Log($"<color=cyan>[MapBuilder]</color> 씬 생성: {SCENE_PATH}");
        }

        // 3. 씬 열기
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(SCENE_PATH);

            // Bootstrap에 카탈로그가 빠져있으면 채워주기
            var bootstrap = Object.FindFirstObjectByType<MapBuilderBootstrap>();
            if (bootstrap != null && bootstrap.catalog == null)
            {
                bootstrap.catalog = catalog;
                EditorUtility.SetDirty(bootstrap);
                EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
            }

            Debug.Log("<color=cyan>[MapBuilder]</color> 맵 빌더 씬 열림. Play 버튼을 누르면 에디터가 시작됩니다.");
            EditorUtility.DisplayDialog("Map Builder",
                "맵 빌더 씬이 열렸습니다.\n\n" +
                "▶ Play 버튼을 누르면 맵 에디터가 시작됩니다.\n\n" +
                "조작법:\n" +
                "  1~5: 도구 선택 (Tile/Wall/Prop/Object/Eraser)\n" +
                "  좌클릭: 배치  |  우클릭: 삭제\n" +
                "  Q/E: 회전\n" +
                "  WASD: 카메라 이동  |  마우스휠: 줌\n" +
                "  Ctrl+S: 저장  |  Ctrl+L: 불러오기\n" +
                "  Ctrl+N: 새 맵  |  Ctrl+Z: 되돌리기",
                "확인");
        }
    }

    static MapBuilderCatalog CreateCatalog()
    {
        EnsureFolder("Assets/Resources/MapBuilder");

        var catalog = ScriptableObject.CreateInstance<MapBuilderCatalog>();

        // 기존 타일 에셋 자동 탐색 & 등록
        var tileGuids = AssetDatabase.FindAssets("t:TileDefinition", new[] { "Assets/Resources/MapBuilder" });
        var tileList = new System.Collections.Generic.List<TileDefinition>();
        var wallList = new System.Collections.Generic.List<TileDefinition>();

        foreach (var guid in tileGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var tile = AssetDatabase.LoadAssetAtPath<TileDefinition>(path);
            if (tile == null) continue;

            if (tile.category == TileCategory.Wall)
                wallList.Add(tile);
            else
                tileList.Add(tile);
        }

        catalog.tiles = tileList.ToArray();
        catalog.walls = wallList.ToArray();

        // Props
        var propGuids = AssetDatabase.FindAssets("t:PropDefinition", new[] { "Assets/Resources/MapBuilder" });
        var propList = new System.Collections.Generic.List<PropDefinition>();
        foreach (var guid in propGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prop = AssetDatabase.LoadAssetAtPath<PropDefinition>(path);
            if (prop != null) propList.Add(prop);
        }
        catalog.props = propList.ToArray();

        // Buildings
        var buildingGuids = AssetDatabase.FindAssets("t:BuildingDefinition", new[] { "Assets/Resources/MapBuilder" });
        var buildingList = new System.Collections.Generic.List<BuildingDefinition>();
        foreach (var guid in buildingGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var building = AssetDatabase.LoadAssetAtPath<BuildingDefinition>(path);
            if (building != null) buildingList.Add(building);
        }
        catalog.buildings = buildingList.ToArray();

        AssetDatabase.CreateAsset(catalog, CATALOG_PATH);
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=cyan>[MapBuilder]</color> 카탈로그에 등록: " +
                  $"타일 {catalog.tiles.Length}개, 벽 {catalog.walls.Length}개, 프롭 {catalog.props.Length}개");

        return catalog;
    }

    static void CreateMapBuilderScene(MapBuilderCatalog catalog)
    {
        EnsureFolder(SCENE_DIR);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Bootstrap
        var bootstrapGo = new GameObject("MapBuilderBootstrap");
        var bootstrap = bootstrapGo.AddComponent<MapBuilderBootstrap>();
        bootstrap.catalog = catalog;
        bootstrap.mapWidth = 32;
        bootstrap.mapHeight = 32;

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
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
