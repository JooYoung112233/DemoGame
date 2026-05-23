using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public static class MapSceneGenerator
    {
        const string BakedRootPrefix = "__Baked_Map_";

        // ─── Bake: generate persistent objects in current scene ───────────

        /// <summary>
        /// Bake MapData into permanent GameObjects in the current scene.
        /// Replaces any previous bake for this map.
        /// </summary>
        public static GameObject BakeToScene(MapData map)
        {
            if (map == null)
            {
                Debug.LogError("[MapSceneGenerator] No map data.");
                return null;
            }

            // Remove previous bake for this map
            ClearBakedMap(map);

            // Hide preview so baked objects are the only visible ones
            LivePreviewManager.ClearPreview();

            var root = new GameObject($"{BakedRootPrefix}{map.mapName}__");
            // Use auto-created map root, or manual parentRoot override
            var mapRoot = MapEditorWindow.GetOrCreateMapRoot();
            if (mapRoot != null)
                root.transform.SetParent(mapRoot, false);

            // MapRuntimeBootstrapper 제거 — bake된 프리팹은 런타임 간섭 없이 그대로 사용

            BakeTiles(map, root.transform);
            BakeBuildings(map, root.transform);
            BakeProps(map, root.transform);

            Undo.RegisterCreatedObjectUndo(root, "Bake Map");
            EditorSceneManager.MarkSceneDirty(root.scene);

            Debug.Log($"[MapSceneGenerator] Baked \"{map.mapName}\" into scene ({root.transform.childCount} groups)");
            return root;
        }

        /// <summary>
        /// Remove previously baked objects for this map from the scene.
        /// </summary>
        public static void ClearBakedMap(MapData map)
        {
            if (map == null) return;
            string target = $"{BakedRootPrefix}{map.mapName}__";
            foreach (var rootGO in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                FindAndDestroy(rootGO.transform, target);
        }

        static void FindAndDestroy(Transform t, string name)
        {
            if (t.name == name)
            {
                Undo.DestroyObjectImmediate(t.gameObject);
                return;
            }
            for (int i = t.childCount - 1; i >= 0; i--)
                FindAndDestroy(t.GetChild(i), name);
        }

        // ─── Save as Prefab ──────────────────────────────────────────────

        public static void SaveAsPrefab(MapData map)
        {
            if (map == null)
            {
                Debug.LogError("[MapSceneGenerator] No map data.");
                return;
            }

            string defaultFolder = "Assets/IsometricMapEditor/Prefabs";
            EnsureFolderExists(defaultFolder);

            string path = EditorUtility.SaveFilePanel("Save Map Prefab", defaultFolder, map.mapName, "prefab");
            if (string.IsNullOrEmpty(path)) return;
            path = "Assets" + path[Application.dataPath.Length..];

            // Bake into scene temporarily
            var root = BakeToScene(map);
            if (root == null) return;

            // MapData 참조 저장 — Load Prefab 시 복원용
            var link = root.AddComponent<MapPrefabLink>();
            link.sourceMapData = map;

            // Save as prefab
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.UserAction);
            AssetDatabase.Refresh();

            // 임시 씬 오브젝트 제거 — 프리팹만 남김
            Object.DestroyImmediate(root);

            Debug.Log($"[MapSceneGenerator] Prefab saved to {path}");
        }

        // ─── Load from Prefab ───────────────────────────────────────────

        /// <summary>
        /// 프리팹에서 MapData를 찾아 맵 에디터에 로드.
        /// MapPrefabLink 컴포넌트에 저장된 참조를 사용.
        /// </summary>
        public static MapData LoadFromPrefab()
        {
            string path = EditorUtility.OpenFilePanel("Load Map Prefab", "Assets/IsometricMapEditor", "prefab");
            if (string.IsNullOrEmpty(path)) return null;

            string dataPath = Application.dataPath;
            if (path.StartsWith(dataPath))
                path = "Assets" + path[dataPath.Length..];

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError("[MapSceneGenerator] Failed to load prefab at: " + path);
                return null;
            }

            // 1. MapPrefabLink가 있으면 MapData 직접 반환
            var link = prefab.GetComponent<MapPrefabLink>();
            if (link != null && link.sourceMapData != null)
            {
                Debug.Log($"[MapSceneGenerator] Loaded MapData \"{link.sourceMapData.mapName}\" from prefab link.");
                return link.sourceMapData;
            }

            // 2. MapPrefabLink 없는 기존 프리팹 → 씬에 배치
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(instance, "Load Map Prefab");
            Debug.Log($"[MapSceneGenerator] Placed prefab \"{prefab.name}\" into scene.");

            return null;
        }

        // ─── Shared generation logic ─────────────────────────────────────

        static void BakeTiles(MapData map, Transform root)
        {
            foreach (var layer in map.layers)
            {
                if (layer.tiles.Count == 0) continue;

                var layerRoot = new GameObject(layer.layerName);
                layerRoot.transform.SetParent(root, false);
                int unityLayer = layer.unityLayer >= 0 ? layer.unityLayer : 0;

                foreach (var tile in layer.tiles)
                {
                    if (tile.tileDefinition == null) continue;

                    if (tile.tileDefinition.IsWall)
                    {
                        var wallGO = WallBuilder.CreateWallCube(tile, map.gridSettings, layerRoot.transform, editorPreview: false);
                        if (wallGO != null) SetLayerRecursive(wallGO.transform, unityLayer);
                        continue;
                    }

                    if (tile.tileDefinition.sprite == null) continue;

                    Vector3 worldPos = IsometricGrid.GridToWorld(tile.gridPosition, map.gridSettings);
                    var tileGO = QuadFactory.CreateFloorQuad(
                        $"Tile_{tile.gridPosition.x}_{tile.gridPosition.y}",
                        worldPos, tile, map.gridSettings, layerRoot.transform, false);
                    if (tileGO != null) tileGO.layer = unityLayer;
                }
            }
        }

        static void BakeBuildings(MapData map, Transform root)
        {
            if (map.buildings.Count == 0) return;

            var buildingsRoot = new GameObject("Buildings");
            buildingsRoot.transform.SetParent(root, false);

            foreach (var building in map.buildings)
            {
                if (building.buildingDefinition == null) continue;
                var def = building.buildingDefinition;
                if (def.prefab == null) continue;

                Vector3 worldPos = building.GetWorldPosition(map.gridSettings);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(def.prefab, buildingsRoot.transform);
                go.name = $"Building_{def.displayName}_{building.instanceId}";
                go.transform.position = worldPos;
                go.transform.rotation = Quaternion.Euler(0, building.yRotation, 0);
                go.transform.localScale = Vector3.one * building.scale;
            }
        }

        static void BakeProps(MapData map, Transform root)
        {
            if (map.props.Count == 0) return;

            var propsRoot = new GameObject("Props");
            propsRoot.transform.SetParent(root, false);

            foreach (var prop in map.props)
            {
                if (prop.propDefinition == null) continue;
                var def = prop.propDefinition;
                if (def.prefab == null) continue;

                Vector3 worldPos = prop.freePlace
                    ? prop.worldPosition
                    : IsometricGrid.GridToWorld(prop.gridPosition, map.gridSettings);

                var go = (GameObject)PrefabUtility.InstantiatePrefab(def.prefab, propsRoot.transform);
                go.name = $"Prop_{def.displayName}_{prop.instanceId}";
                go.transform.position = worldPos;
                go.transform.rotation = Quaternion.Euler(0, prop.yRotation, 0);
                go.transform.localScale = Vector3.one * prop.scale;
            }
        }

        // ─── Utility ─────────────────────────────────────────────────────

        static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursive(t.GetChild(i), layer);
        }

        static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
