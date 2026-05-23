using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace IsometricMapEditor.Editor
{
    /// <summary>
    /// Live preview for the Building Workshop. Renders floor/wall/roof/prop
    /// as DontSave objects in the scene. Survives OFF toggle (keeps objects visible).
    /// After domain reload, restores from BuildingWorkshopData.
    /// </summary>
    [InitializeOnLoad]
    public static class WorkshopPreviewManager
    {
        private static BuildingWorkshopData lastData;
        private static int lastFloorCount;
        private static int lastWallCount;
        private static int lastRoofCount;
        private static int lastPropCount;
        private static bool lastRoofVisible;
        private static bool _dirty = true;

        static WorkshopPreviewManager()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        public static void InvalidateTracking()
        {
            _dirty = true;
        }

        private static void OnEditorUpdate()
        {
            BuildingWorkshopData data = BuildingWorkshopWindow.ActiveData;

            if (data == null)
            {
                if (lastData != null)
                {
                    ClearPreview();
                    lastData = null;
                    _dirty = false;
                }
                return;
            }

            // Workshop OFF → keep existing preview
            if (!BuildingWorkshopWindow.WorkshopEnabled)
                return;

            int floorCount = data.floorTiles.Count;
            int wallCount = data.wallTiles.Count;
            int roofCount = data.roofTiles.Count;
            int propCount = data.props.Count;
            bool roofVis = data.roofVisible;

            if (_dirty || data != lastData
                || floorCount != lastFloorCount || wallCount != lastWallCount
                || roofCount != lastRoofCount || propCount != lastPropCount
                || roofVis != lastRoofVisible)
            {
                lastData = data;
                lastFloorCount = floorCount;
                lastWallCount = wallCount;
                lastRoofCount = roofCount;
                lastPropCount = propCount;
                lastRoofVisible = roofVis;
                _dirty = false;
                RefreshPreview(data);
            }
        }

        private static void RefreshPreview(BuildingWorkshopData data)
        {
            ClearPreview();

            GridSettings gs = data.GetGridSettings();
            Transform root = CreateGroup("__Workshop_Preview__");

            // Floor tiles
            foreach (PlacedTile tile in data.floorTiles)
            {
                if (tile.tileDefinition == null || tile.tileDefinition.sprite == null) continue;

                Vector3 worldPos = IsometricGrid.GridToWorld(tile.gridPosition, gs);
                GameObject go = new GameObject("WF_" + tile.gridPosition.x + "_" + tile.gridPosition.y);
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(root);
                go.transform.position = worldPos;
                go.transform.rotation = Quaternion.Euler(90, 0, 0);
                go.transform.localScale = IsometricGrid.GetTileScale(tile.tileDefinition.sprite, gs);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = tile.tileDefinition.sprite;
                sr.sortingOrder = IsometricGrid.GetSortingOrder(tile.gridPosition);

                Material mat = tile.EffectiveMaterial;
                if (mat != null) sr.sharedMaterial = mat;
            }

            // Wall tiles
            foreach (PlacedTile tile in data.wallTiles)
            {
                if (tile.tileDefinition == null || !tile.tileDefinition.IsWall) continue;
                WallBuilder.CreateWallCube(tile, gs, root, editorPreview: true);
            }

            // Roof tiles (under Roof parent with RoofController)
            if (data.roofTiles.Count > 0)
            {
                GameObject roofParent = new GameObject("Roof");
                roofParent.hideFlags = HideFlags.DontSave;
                roofParent.transform.SetParent(root);

                // Add RoofController component
                RoofController roofCtrl = roofParent.AddComponent<RoofController>();

                if (!data.roofVisible)
                    roofParent.SetActive(false);

                foreach (PlacedTile tile in data.roofTiles)
                {
                    if (tile.tileDefinition == null || tile.tileDefinition.sprite == null) continue;

                    Vector3 worldPos = IsometricGrid.GridToWorld(tile.gridPosition, gs);
                    // Roof is elevated above walls
                    worldPos.y += 2.2f;

                    GameObject go = new GameObject("WR_" + tile.gridPosition.x + "_" + tile.gridPosition.y);
                    go.hideFlags = HideFlags.DontSave;
                    go.transform.SetParent(roofParent.transform);
                    go.transform.position = worldPos;
                    go.transform.rotation = Quaternion.Euler(90, 0, 0);
                    go.transform.localScale = IsometricGrid.GetTileScale(tile.tileDefinition.sprite, gs);

                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = tile.tileDefinition.sprite;
                    sr.sortingOrder = IsometricGrid.GetSortingOrder(tile.gridPosition) + 100;

                    Material mat = tile.EffectiveMaterial;
                    if (mat != null) sr.sharedMaterial = mat;
                }
            }

            // Props
            foreach (PlacedProp prop in data.props)
            {
                if (prop.propDefinition == null || prop.propDefinition.prefab == null) continue;

                Vector3 worldPos = IsometricGrid.GridToWorld(prop.gridPosition, gs);
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prop.propDefinition.prefab);
                go.name = "WP_" + prop.propDefinition.displayName;
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(root);
                go.transform.position = worldPos;

                if (prop.freePlace)
                {
                    go.transform.rotation = Quaternion.Euler(0, prop.yRotation, 0);
                    go.transform.localScale = Vector3.one * prop.scale;
                }
            }

            SceneView.RepaintAll();
        }

        private static Transform CreateGroup(string name)
        {
            GameObject go = new GameObject(name);
            go.hideFlags = HideFlags.DontSave;
            return go.transform;
        }

        public static void ClearPreview()
        {
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == "__Workshop_Preview__"
                    && (root.hideFlags & HideFlags.DontSave) != 0)
                {
                    toDestroy.Add(root);
                }
                // Also check children
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = root.transform.GetChild(i);
                    if (child.name == "__Workshop_Preview__"
                        && (child.gameObject.hideFlags & HideFlags.DontSave) != 0)
                    {
                        toDestroy.Add(child.gameObject);
                    }
                }
            }
            foreach (GameObject go in toDestroy)
                Object.DestroyImmediate(go);
        }
    }
}
