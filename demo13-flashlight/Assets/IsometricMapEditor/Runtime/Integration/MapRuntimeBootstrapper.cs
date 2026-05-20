using UnityEngine;

namespace IsometricMapEditor
{
    public class MapRuntimeBootstrapper : MonoBehaviour
    {
        [SerializeField] MapData mapData;
        [SerializeField] MapLoadMode loadMode = MapLoadMode.ScriptableObject;

        MapLoader mapLoader;
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
            mapLoader = GetComponentInChildren<MapLoader>();
            if (mapLoader == null)
            {
                var loaderGO = new GameObject("MapLoader");
                loaderGO.transform.SetParent(transform);
                mapLoader = loaderGO.AddComponent<MapLoader>();
            }

            mapLoader.LoadMap(mapData);

            cameraController = FindAnyObjectByType<IsometricCameraController>();
            if (cameraController != null)
            {
                Rect bounds = IsometricGrid.GetMapWorldBounds(mapData.gridSettings);
                cameraController.SetBounds(bounds);
            }

            Debug.Log($"[MapBootstrapper] Map '{mapData.mapName}' loaded. " +
                      $"Grid: {mapData.gridSettings.mapWidth}x{mapData.gridSettings.mapHeight}, " +
                      $"Tile: {mapData.gridSettings.tileWidth}x{mapData.gridSettings.tileHeight}");
        }
    }
}
