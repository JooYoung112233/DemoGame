using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace IsometricMapEditor.Editor
{
    /// <summary>
    /// Bakes BuildingWorkshopData into permanent scene objects or prefabs.
    /// Creates structure: Root > Floor, Walls, Roof (with RoofController), Props
    /// </summary>
    public static class WorkshopBaker
    {
        private const string BakedPrefix = "__Baked_Workshop_";

        public static GameObject BakeToScene(BuildingWorkshopData data)
        {
            if (data == null)
            {
                Debug.LogError("[WorkshopBaker] No workshop data.");
                return null;
            }

            // Remove previous bake
            ClearBaked(data);
            WorkshopPreviewManager.ClearPreview();

            GridSettings gs = data.GetGridSettings();
            GameObject root = new GameObject(BakedPrefix + data.buildingName + "__");

            // Floor
            if (data.floorTiles.Count > 0)
            {
                GameObject floorRoot = new GameObject("Floor");
                floorRoot.transform.SetParent(root.transform, false);

                foreach (PlacedTile tile in data.floorTiles)
                {
                    if (tile.tileDefinition == null || tile.tileDefinition.sprite == null) continue;

                    Vector3 worldPos = IsometricGrid.GridToWorld(tile.gridPosition, gs);
                    GameObject go = new GameObject("Floor_" + tile.gridPosition.x + "_" + tile.gridPosition.y);
                    go.transform.SetParent(floorRoot.transform);
                    go.transform.position = worldPos;
                    go.transform.rotation = Quaternion.Euler(90, 0, 0);
                    go.transform.localScale = IsometricGrid.GetTileScale(tile.tileDefinition.sprite, gs);

                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = tile.tileDefinition.sprite;
                    sr.sortingOrder = IsometricGrid.GetSortingOrder(tile.gridPosition);
                    Material mat = tile.EffectiveMaterial;
                    if (mat != null) sr.sharedMaterial = mat;
                }
            }

            // Walls
            if (data.wallTiles.Count > 0)
            {
                GameObject wallRoot = new GameObject("Walls");
                wallRoot.transform.SetParent(root.transform, false);

                foreach (PlacedTile tile in data.wallTiles)
                {
                    if (tile.tileDefinition == null || !tile.tileDefinition.IsWall) continue;
                    WallBuilder.CreateWallCube(tile, gs, wallRoot.transform, editorPreview: false);
                }
            }

            // Roof (with RoofController)
            if (data.roofTiles.Count > 0)
            {
                GameObject roofRoot = new GameObject("Roof");
                roofRoot.transform.SetParent(root.transform, false);
                roofRoot.AddComponent<RoofController>();

                foreach (PlacedTile tile in data.roofTiles)
                {
                    if (tile.tileDefinition == null || tile.tileDefinition.sprite == null) continue;

                    Vector3 worldPos = IsometricGrid.GridToWorld(tile.gridPosition, gs);
                    worldPos.y += 2.2f;

                    GameObject go = new GameObject("Roof_" + tile.gridPosition.x + "_" + tile.gridPosition.y);
                    go.transform.SetParent(roofRoot.transform);
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
            if (data.props.Count > 0)
            {
                GameObject propRoot = new GameObject("Props");
                propRoot.transform.SetParent(root.transform, false);

                foreach (PlacedProp prop in data.props)
                {
                    if (prop.propDefinition == null || prop.propDefinition.prefab == null) continue;

                    Vector3 worldPos = IsometricGrid.GridToWorld(prop.gridPosition, gs);
                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prop.propDefinition.prefab);
                    go.name = "Prop_" + prop.propDefinition.displayName;
                    go.transform.SetParent(propRoot.transform);
                    go.transform.position = worldPos;

                    if (prop.freePlace)
                    {
                        go.transform.rotation = Quaternion.Euler(0, prop.yRotation, 0);
                        go.transform.localScale = Vector3.one * prop.scale;
                    }
                }
            }

            Undo.RegisterCreatedObjectUndo(root, "Bake Workshop Building");
            EditorSceneManager.MarkSceneDirty(root.scene);

            int total = data.floorTiles.Count + data.wallTiles.Count + data.roofTiles.Count + data.props.Count;
            Debug.Log("[WorkshopBaker] Baked \"" + data.buildingName + "\" (" + total + " objects)");
            return root;
        }

        public static void SaveAsPrefab(BuildingWorkshopData data)
        {
            if (data == null)
            {
                Debug.LogError("[WorkshopBaker] No workshop data.");
                return;
            }

            string defaultFolder = "Assets/IsometricMapEditor/Prefabs/Buildings";
            EnsureFolderExists(defaultFolder);

            string path = EditorUtility.SaveFilePanel("Save Building Prefab",
                defaultFolder, data.buildingName, "prefab");
            if (string.IsNullOrEmpty(path)) return;

            // Convert absolute to relative
            string dataPath = Application.dataPath;
            if (path.StartsWith(dataPath))
                path = "Assets" + path.Substring(dataPath.Length);

            GameObject root = BakeToScene(data);
            if (root == null) return;

            PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.UserAction);
            AssetDatabase.Refresh();

            Debug.Log("[WorkshopBaker] Prefab saved: " + path);
        }

        public static void ClearBaked(BuildingWorkshopData data)
        {
            if (data == null) return;
            string target = BakedPrefix + data.buildingName + "__";
            foreach (GameObject rootGO in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (rootGO.name == target)
                {
                    Undo.DestroyObjectImmediate(rootGO);
                }
            }
        }

        private static void EnsureFolderExists(string folderPath)
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
