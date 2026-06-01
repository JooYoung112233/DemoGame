using UnityEngine;

namespace TopDownMapEditor
{
    public class MapRuntimeBootstrapper : MonoBehaviour
    {
        [SerializeField] MapData mapData;

        MapLoader mapLoader;
        BuildingRenderer buildingRenderer;
        RoofController roofController;
        BuildingStateManager stateManager;
        PropManager propManager;
        HarvestableManager harvestableManager;
        MapObjectSpawner mapObjectSpawner;
        WalkabilityMap walkabilityMap;
        EscapePointManager escapeManager;
        InteriorMapLoader interiorLoader;
        InteriorTransitionManager interiorTransition;
        MapDebugOverlay debugOverlay;
        TopDownCameraController cameraController;

        public void SetMapData(MapData data)
        {
            mapData = data;
        }

        void Start()
        {
            // bake된 맵 사용 — 런타임 자동 생성 비활성화
        }

        void Bootstrap()
        {
            mapLoader = CreateChild<MapLoader>("MapLoader");
            mapLoader.LoadMap(mapData);

            buildingRenderer = CreateChild<BuildingRenderer>("BuildingRenderer");
            buildingRenderer.Initialize(transform);
            buildingRenderer.RenderBuildings(mapData);

            roofController = CreateChild<RoofController>("RoofController");
            roofController.Initialize(buildingRenderer);

            if (mapData.buildings.Exists(b => b.buildingDefinition?.variantSet != null))
            {
                stateManager = CreateChild<BuildingStateManager>("StateManager");
                stateManager.Initialize(mapData, buildingRenderer);
            }

            propManager = CreateChild<PropManager>("PropManager");
            propManager.Initialize(transform);
            propManager.SetBuildingObjects(buildingRenderer.BuildingObjects);
            propManager.SpawnProps(mapData.props, mapData.gridSettings);

            harvestableManager = CreateChild<HarvestableManager>("HarvestableManager");
            harvestableManager.Initialize(transform);
            harvestableManager.SpawnHarvestables(mapData.harvestables, mapData.gridSettings);

            // Map objects (SpawnPoint, InteractableObject 등)
            if (mapData.mapObjects.Count > 0)
            {
                mapObjectSpawner = CreateChild<MapObjectSpawner>("MapObjectSpawner");
                mapObjectSpawner.Initialize(transform);
                mapObjectSpawner.SetBuildingObjects(buildingRenderer.BuildingObjects);
                mapObjectSpawner.SpawnMapObjects(mapData.mapObjects, mapData.gridSettings);
            }

            if (mapData.walkability != null)
            {
                walkabilityMap = CreateChild<WalkabilityMap>("WalkabilityMap");
                walkabilityMap.Initialize(mapData.walkability, mapData.gridSettings);
            }

            interiorLoader = CreateChild<InteriorMapLoader>("InteriorLoader");

            interiorTransition = CreateChild<InteriorTransitionManager>("InteriorTransition");
            interiorTransition.Initialize(interiorLoader, buildingRenderer, roofController);

            debugOverlay = CreateChild<MapDebugOverlay>("DebugOverlay");
            debugOverlay.Initialize(mapData);

            cameraController = FindAnyObjectByType<TopDownCameraController>();
            if (cameraController != null)
            {
                Bounds bounds = TopDownGrid.GetMapWorldBounds(mapData.gridSettings);
                cameraController.SetBounds(bounds);
            }

            Debug.Log($"[MapBootstrapper] Map '{mapData.mapName}' loaded. " +
                      $"Grid: {mapData.gridSettings.mapWidth}x{mapData.gridSettings.mapHeight}, " +
                      $"Buildings: {mapData.buildings.Count}, Props: {mapData.props.Count}");
        }

        T CreateChild<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            return go.AddComponent<T>();
        }
    }
}
