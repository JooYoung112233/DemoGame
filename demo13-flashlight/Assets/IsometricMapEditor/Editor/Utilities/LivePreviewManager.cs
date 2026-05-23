using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    [InitializeOnLoad]
    public static class LivePreviewManager
    {
        static MapData lastTrackedMap;
        static int lastTileCount;
        static int lastBuildingCount;
        static int lastPropCount;

        static LivePreviewManager()
        {
            EditorApplication.update += OnEditorUpdate;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        /// <summary>
        /// Force re-check on next update (called after domain reload to restore preview).
        /// </summary>
        public static void InvalidateTracking()
        {
            lastTrackedMap = null;
            lastTileCount = -1;
            lastBuildingCount = -1;
            lastPropCount = -1;
        }

        static void OnEditorUpdate()
        {
            var map = MapEditorWindow.ActiveMap;

            // Map removed → clear preview
            if (map == null)
            {
                if (lastTrackedMap != null)
                {
                    ClearPreview();
                    lastTrackedMap = null;
                    lastTileCount = 0;
                    lastBuildingCount = 0;
                    lastPropCount = 0;
                }
                return;
            }

            // Editor OFF → keep existing preview, just stop updating
            if (!MapEditorWindow.EditorEnabled)
                return;

            int tileCount = 0;
            foreach (var layer in map.layers)
                tileCount += layer.tiles.Count;

            if (map != lastTrackedMap || tileCount != lastTileCount
                || map.buildings.Count != lastBuildingCount || map.props.Count != lastPropCount)
            {
                lastTrackedMap = map;
                lastTileCount = tileCount;
                lastBuildingCount = map.buildings.Count;
                lastPropCount = map.props.Count;
                RefreshScenePreview(map);
            }
        }

        static void OnUndoRedo()
        {
            var map = MapEditorWindow.ActiveMap;
            if (map != null && MapEditorWindow.EditorEnabled)
                RefreshScenePreview(map);
        }

        static void RefreshScenePreview(MapData map)
        {
            ClearPreview();

            // All preview objects go under the map's root parent
            var mapRoot = MapEditorWindow.GetOrCreateMapRoot();

            var layerRoots = new Dictionary<string, Transform>();

            foreach (var layer in map.layers)
            {
                if (!layer.isVisible) continue;
                if (layer.tiles.Count == 0) continue;

                var layerRoot = GetOrCreateLayerRoot(layer.layerName, layerRoots, mapRoot);
                int unityLayer = layer.unityLayer >= 0 ? layer.unityLayer : 0;

                foreach (var tile in layer.tiles)
                {
                    if (tile.tileDefinition == null) continue;

                    // Wall → 3D cube
                    if (tile.tileDefinition.IsWall)
                    {
                        var wallGO = WallBuilder.CreateWallCube(tile, map.gridSettings, layerRoot, editorPreview: true);
                        if (wallGO != null) SetLayerRecursive(wallGO.transform, unityLayer);
                        continue;
                    }

                    if (tile.tileDefinition.sprite == null) continue;

                    Vector3 worldPos = IsometricGrid.GridToWorld(tile.gridPosition, map.gridSettings);
                    var tileGO = QuadFactory.CreateFloorQuad(
                        $"Tile_{tile.gridPosition.x}_{tile.gridPosition.y}",
                        worldPos, tile, map.gridSettings, layerRoot, true);
                    if (tileGO != null) tileGO.layer = unityLayer;
                }
            }

            // Buildings (prefab instantiation)
            if (map.buildings.Count > 0)
            {
                var bParent = MapEditorWindow.GetBuildingsParent();
                var bRoot = CreatePreviewGroup("__Preview_Buildings__", bParent != null ? bParent : mapRoot);
                foreach (var building in map.buildings)
                {
                    if (building.buildingDefinition == null) continue;
                    var def = building.buildingDefinition;
                    if (def.prefab == null) continue;

                    Vector3 worldPos = building.GetWorldPosition(map.gridSettings);
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(def.prefab, bRoot);
                    go.name = $"Building_{def.displayName}_{building.instanceId}";
                    go.hideFlags = HideFlags.DontSave;
                    go.transform.position = worldPos;
                    go.transform.rotation = Quaternion.Euler(0, building.yRotation, 0);
                    go.transform.localScale = Vector3.one * building.scale;
                    SetHideFlagsRecursive(go.transform);
                }
            }

            // Props (prefab instantiation)
            if (map.props.Count > 0)
            {
                var pParent = MapEditorWindow.GetPropsParent();
                var pRoot = CreatePreviewGroup("__Preview_Props__", pParent != null ? pParent : mapRoot);
                foreach (var prop in map.props)
                {
                    if (prop.propDefinition == null) continue;
                    var def = prop.propDefinition;
                    if (def.prefab == null) continue;

                    Vector3 worldPos = prop.freePlace
                        ? prop.worldPosition
                        : IsometricGrid.GridToWorld(prop.gridPosition, map.gridSettings);

                    var go = (GameObject)PrefabUtility.InstantiatePrefab(def.prefab, pRoot);
                    go.name = $"Prop_{def.displayName}_{prop.instanceId}";
                    go.hideFlags = HideFlags.DontSave;
                    go.transform.position = worldPos;
                    go.transform.rotation = Quaternion.Euler(0, prop.yRotation, 0);
                    go.transform.localScale = Vector3.one * prop.scale;
                    SetHideFlagsRecursive(go.transform);
                }
            }

            SceneView.RepaintAll();
        }

        static Transform GetOrCreateLayerRoot(string layerName, Dictionary<string, Transform> cache, Transform mapRoot)
        {
            if (cache.TryGetValue(layerName, out var existing))
                return existing;

            var parent = MapEditorWindow.GetLayerParent(layerName);
            var root = CreatePreviewGroup($"__Preview_{layerName}__", parent != null ? parent : mapRoot);
            cache[layerName] = root;
            return root;
        }

        static Transform CreatePreviewGroup(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.hideFlags = HideFlags.DontSave;
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go.transform;
        }

        public static void ClearPreview()
        {
            ClearByPrefix("__Preview_");
            // legacy cleanup
            var old = GameObject.Find("__MapEditorPreview__");
            if (old != null) Object.DestroyImmediate(old);
        }

        static void ClearByPrefix(string prefix)
        {
            var toDestroy = new List<GameObject>();
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                CollectPreviewObjects(root.transform, prefix, toDestroy);
            foreach (var go in toDestroy)
                Object.DestroyImmediate(go);
        }

        static void CollectPreviewObjects(Transform t, string prefix, List<GameObject> list)
        {
            if (t.name.StartsWith(prefix) && (t.gameObject.hideFlags & HideFlags.DontSave) != 0)
            {
                list.Add(t.gameObject);
                return;
            }
            for (int i = t.childCount - 1; i >= 0; i--)
                CollectPreviewObjects(t.GetChild(i), prefix, list);
        }

        static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursive(t.GetChild(i), layer);
        }

        static void SetHideFlagsRecursive(Transform t)
        {
            t.gameObject.hideFlags = HideFlags.DontSave;
            for (int i = 0; i < t.childCount; i++)
                SetHideFlagsRecursive(t.GetChild(i));
        }
    }
}
