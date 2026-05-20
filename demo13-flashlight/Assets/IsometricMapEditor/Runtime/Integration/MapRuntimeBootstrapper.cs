using UnityEngine;

namespace IsometricMapEditor
{
    public class MapRuntimeBootstrapper : MonoBehaviour
    {
        [SerializeField] MapData mapData;
        [SerializeField] MapLoadMode loadMode = MapLoadMode.ScriptableObject;

        MapLoader mapLoader;
        BuildingRenderer buildingRenderer;
        RoofController roofController;
        BuildingStateManager stateManager;
        PropManager propManager;
        HarvestableManager harvestableManager;
        WalkabilityMap walkabilityMap;
        EscapePointManager escapeManager;
        InteriorMapLoader interiorLoader;
        InteriorTransitionManager interiorTransition;
        MapDebugOverlay debugOverlay;
        IsometricCameraController cameraController;

        public enum MapLoadMode { ScriptableObject, JSON, Addressable }

        public void SetMapData(MapData data)
        {
            mapData = data;
        }

        void Start()
        {
            if (mapData == null)
            {
                Debug.LogWarning("[MapBootstrapper] No MapData assigned.");
                return;
            }

            Bootstrap();
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
            propManager.SpawnProps(mapData.props, mapData.gridSettings);

            harvestableManager = CreateChild<HarvestableManager>("HarvestableManager");
            harvestableManager.Initialize(transform);
            harvestableManager.SpawnHarvestables(mapData.harvestables, mapData.gridSettings);

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

            cameraController = FindAnyObjectByType<IsometricCameraController>();
            if (cameraController != null)
            {
                Rect bounds = IsometricGrid.GetMapWorldBounds(mapData.gridSettings);
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
