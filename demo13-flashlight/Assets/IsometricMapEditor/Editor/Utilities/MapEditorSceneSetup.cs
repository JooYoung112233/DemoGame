using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public static class MapEditorSceneSetup
    {
        [MenuItem("Tools/Isometric Map/Create Editor Scene", priority = 10)]
        public static void CreateEditorScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 카메라: 3D 아이소메트릭용 오쏘그래픽
            var camGO = new GameObject("Main Camera");
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8;
            cam.transform.position = new Vector3(0, 10, -10);
            cam.transform.rotation = Quaternion.Euler(30, 45, 0);
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGO.tag = "MainCamera";
            camGO.AddComponent<IsometricCameraController>();

            // 맵 생성 또는 기존 맵 선택
            MapData mapData = FindOrCreateMap();

            // 부트스트래퍼
            var bootstrapperGO = new GameObject("MapBootstrapper");
            var bootstrapper = bootstrapperGO.AddComponent<MapRuntimeBootstrapper>();
            bootstrapper.SetMapData(mapData);

            // Scene View를 3D 아이소메트릭 뷰로 전환
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.in2DMode = false;
                sceneView.LookAt(Vector3.zero, Quaternion.Euler(30, 45, 0), 15f);
            }

            // 씬 저장
            string scenePath = EditorUtility.SaveFilePanel(
                "Save Map Editor Scene", "Assets", "MapEditorScene", "unity");

            if (!string.IsNullOrEmpty(scenePath))
            {
                scenePath = "Assets" + scenePath[Application.dataPath.Length..];
                EditorSceneManager.SaveScene(scene, scenePath);
            }

            // 맵 에디터 창 자동 열기
            var window = EditorWindow.GetWindow<MapEditorWindow>("Map Editor");
            window.Show();

            Debug.Log("[MapEditorSetup] 맵 에디터 씬 생성 완료. Scene View를 3D 아이소메트릭 뷰로 전환했습니다.");
            Debug.Log("[MapEditorSetup] 1) 에디터 창에서 맵을 선택하세요");
            Debug.Log("[MapEditorSetup] 2) Sprites 폴더에 이미지를 넣고 [Sync] 버튼을 누르세요");
            Debug.Log("[MapEditorSetup] 3) Paint 도구로 Scene View에서 타일을 배치하세요");
        }

        [MenuItem("Tools/Isometric Map/Setup Current Scene", priority = 11)]
        public static void SetupCurrentScene()
        {
            // 카메라 확인/생성
            var cam = Camera.main;
            if (cam == null)
            {
                var camGO = new GameObject("Main Camera");
                cam = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = 8;
            cam.transform.position = new Vector3(0, 10, -10);
            cam.transform.rotation = Quaternion.Euler(30, 45, 0);

            if (cam.GetComponent<IsometricCameraController>() == null)
                cam.gameObject.AddComponent<IsometricCameraController>();

            // 부트스트래퍼 확인/생성
            var bootstrapper = Object.FindFirstObjectByType<MapRuntimeBootstrapper>();
            if (bootstrapper == null)
            {
                var go = new GameObject("MapBootstrapper");
                bootstrapper = go.AddComponent<MapRuntimeBootstrapper>();
            }

            // 기존 맵 있으면 할당
            MapData mapData = FindOrCreateMap();
            bootstrapper.SetMapData(mapData);

            // Scene View 3D 아이소메트릭
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.in2DMode = false;
                sceneView.LookAt(Vector3.zero, Quaternion.Euler(30, 45, 0), 15f);
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            var window = EditorWindow.GetWindow<MapEditorWindow>("Map Editor");
            window.Show();

            Debug.Log("[MapEditorSetup] 현재 씬에 맵 에디터 세팅 완료.");
        }

        [MenuItem("Tools/Isometric Map/Create Sample Tiles", priority = 20)]
        public static void CreateSampleTiles()
        {
            string folder = "Assets/IsometricMapEditor/SampleData";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/IsometricMapEditor", "SampleData");
            }

            CreateTile(folder, "ground_grass", "Grass", TileCategory.Ground);
            CreateTile(folder, "ground_dirt", "Dirt", TileCategory.Ground);
            CreateTile(folder, "ground_stone", "Stone Floor", TileCategory.Ground);
            CreateTile(folder, "wall_brick", "Brick Wall", TileCategory.Wall, false);
            CreateTile(folder, "deco_flower", "Flower", TileCategory.Decoration);
            CreateTile(folder, "water_shallow", "Shallow Water", TileCategory.Water, false);

            var building = ScriptableObject.CreateInstance<BuildingDefinition>();
            building.buildingId = "house_small";
            building.displayName = "Small House";
            building.footprint = new Vector2Int(2, 2);
            building.isEnterable = true;
            building.interiorMapId = "house_small_interior";
            AssetDatabase.CreateAsset(building, $"{folder}/Building_SmallHouse.asset");

            var prop = ScriptableObject.CreateInstance<PropDefinition>();
            prop.propId = "streetlight";
            prop.displayName = "Street Light";
            prop.footprint = new Vector2Int(1, 1);
            prop.blocksWalkability = true;
            AssetDatabase.CreateAsset(prop, $"{folder}/Prop_StreetLight.asset");

            var harvestable = ScriptableObject.CreateInstance<HarvestableDefinition>();
            harvestable.harvestableId = "berry_bush";
            harvestable.displayName = "Berry Bush";
            harvestable.respawnTimeSec = 60f;
            harvestable.dropTable.Add(new DropEntry
            {
                itemId = "berry",
                minAmount = 1,
                maxAmount = 3,
                dropRate = 0.8f
            });
            harvestable.spawnConditions.Add(new SpawnCondition
            {
                type = SpawnConditionType.Probability,
                param = "0.5"
            });
            AssetDatabase.CreateAsset(harvestable, $"{folder}/Harvestable_BerryBush.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MapEditorSetup] 샘플 에셋 생성 완료: {folder}");
            Debug.Log("[MapEditorSetup] 스프라이트는 직접 할당해야 합니다 (각 에셋 Inspector에서)");
        }

        static void CreateTile(string folder, string id, string name, TileCategory category, bool walkable = true)
        {
            var tile = ScriptableObject.CreateInstance<TileDefinition>();
            tile.tileId = id;
            tile.category = category;
            tile.isWalkable = walkable;
            tile.size = Vector2Int.one;
            AssetDatabase.CreateAsset(tile, $"{folder}/Tile_{name.Replace(" ", "")}.asset");
        }

        static MapData FindOrCreateMap()
        {
            string[] guids = AssetDatabase.FindAssets("t:MapData");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<MapData>(path);
            }

            string folder = "Assets/IsometricMapEditor/SampleData";
            if (!AssetDatabase.IsValidFolder("Assets/IsometricMapEditor"))
                AssetDatabase.CreateFolder("Assets", "IsometricMapEditor");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/IsometricMapEditor", "SampleData");

            var map = ScriptableObject.CreateInstance<MapData>();
            map.mapName = "NewMap";
            map.mapId = System.Guid.NewGuid().ToString("N")[..8];
            map.gridSettings = new GridSettings
            {
                tileSize = 1f,
                mapWidth = 16,
                mapHeight = 16,
                originOffset = Vector3.zero
            };
            map.GetOrCreateLayer("Ground", 0);
            map.GetOrCreateLayer("Objects", 10);
            map.GetOrCreateLayer("Roof", 20);

            string mapPath = $"{folder}/Map_Default.asset";
            AssetDatabase.CreateAsset(map, mapPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[MapEditorSetup] 기본 맵 생성: {mapPath}");
            return map;
        }
    }
}
