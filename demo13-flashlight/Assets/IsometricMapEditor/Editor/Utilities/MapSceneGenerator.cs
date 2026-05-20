using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public static class MapSceneGenerator
    {
        public static void GenerateScene(MapData map, string scenePath = null)
        {
            if (map == null)
            {
                Debug.LogError("[MapSceneGenerator] No map data.");
                return;
            }

            if (string.IsNullOrEmpty(scenePath))
            {
                scenePath = EditorUtility.SaveFilePanel("Save Scene", "Assets", map.mapName, "unity");
                if (string.IsNullOrEmpty(scenePath)) return;
                scenePath = "Assets" + scenePath[Application.dataPath.Length..];
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var mapRoot = new GameObject($"Map_{map.mapName}");

            GenerateTiles(map, mapRoot.transform);
            GenerateBuildings(map, mapRoot.transform);
            GenerateProps(map, mapRoot.transform);

            var bootstrapper = mapRoot.AddComponent<MapRuntimeBootstrapper>();
            bootstrapper.SetMapData(map);

            var cameraGO = new GameObject("Main Camera");
            var cam = cameraGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.transform.position = new Vector3(0, 0, -10);
            cameraGO.AddComponent<IsometricCameraController>();
            cameraGO.tag = "MainCamera";

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[MapSceneGenerator] Scene saved to {scenePath}");
        }

        static void GenerateTiles(MapData map, Transform parent)
        {
            var tilesRoot = new GameObject("Tiles");
            tilesRoot.transform.SetParent(parent);

            foreach (var layer in map.layers)
            {
                var layerRoot = new GameObject(layer.layerName);
                layerRoot.transform.SetParent(tilesRoot.transform);

                foreach (var tile in layer.tiles)
                {
                    if (tile.tileDefinition == null || tile.tileDefinition.sprite == null) continue;

                    Vector2 worldPos = IsometricGrid.GridToWorld(tile.gridPosition, map.gridSettings);
                    var go = new GameObject($"Tile_{tile.gridPosition.x}_{tile.gridPosition.y}");
                    go.transform.SetParent(layerRoot.transform);
                    go.transform.position = new Vector3(worldPos.x, worldPos.y, 0);

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = tile.tileDefinition.sprite;
                    sr.flipX = tile.flipX;
                    sr.sortingOrder = IsometricGrid.GetSortingOrder(tile.gridPosition, layer.sortingLayerOffset)
                                      + tile.tileDefinition.sortingOffset;
                }
            }
        }

        static void GenerateBuildings(MapData map, Transform parent)
        {
            if (map.buildings.Count == 0) return;

            var buildingsRoot = new GameObject("Buildings");
            buildingsRoot.transform.SetParent(parent);

            foreach (var building in map.buildings)
            {
                if (building.buildingDefinition == null) continue;
                var def = building.buildingDefinition;

                Vector2 worldPos = IsometricGrid.GridToWorld(building.gridPosition, map.gridSettings);
                var go = new GameObject($"Building_{def.displayName}_{building.instanceId}");
                go.transform.SetParent(buildingsRoot.transform);
                go.transform.position = new Vector3(worldPos.x, worldPos.y, 0);

                if (def.baseSprite != null)
                {
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = def.baseSprite;
                    sr.sortingOrder = def.GetFrontSortingOrder(building.gridPosition) + def.sortingOffset;
                }

                if (def.roofSprite != null)
                {
                    var roofGO = new GameObject("Roof");
                    roofGO.transform.SetParent(go.transform);
                    roofGO.transform.localPosition = Vector3.zero;
                    var roofSR = roofGO.AddComponent<SpriteRenderer>();
                    roofSR.sprite = def.roofSprite;
                    roofSR.sortingOrder = def.GetFrontSortingOrder(building.gridPosition) + def.sortingOffset + 1;
                }
            }
        }

        static void GenerateProps(MapData map, Transform parent)
        {
            if (map.props.Count == 0) return;

            var propsRoot = new GameObject("Props");
            propsRoot.transform.SetParent(parent);

            foreach (var prop in map.props)
            {
                if (prop.propDefinition == null || prop.propDefinition.sprite == null) continue;

                Vector2 worldPos = IsometricGrid.GridToWorld(prop.gridPosition, map.gridSettings);
                var go = new GameObject($"Prop_{prop.propDefinition.displayName}_{prop.instanceId}");
                go.transform.SetParent(propsRoot.transform);
                go.transform.position = new Vector3(worldPos.x, worldPos.y, 0);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = prop.propDefinition.sprite;
                sr.sortingOrder = IsometricGrid.GetSortingOrder(prop.gridPosition) + prop.propDefinition.sortingOffset;
            }
        }
    }
}
