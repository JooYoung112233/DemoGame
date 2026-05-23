using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace IsometricMapEditor.Editor
{
    /// <summary>
    /// Bakes BuildingWorkshopData into permanent scene objects or prefabs.
    /// Uses QuadFactory (mesh-based) — same rendering pipeline as the map editor.
    /// Structure: Root(BuildingGlow, BoxCollider) > Floor, Walls, Roof(RoofController), Props
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

            ClearBaked(data);
            WorkshopPreviewManager.ClearPreview();

            GridSettings gs = data.GetGridSettings();
            GameObject root = new GameObject(BakedPrefix + data.buildingName + "__");

            // Floor — QuadFactory mesh
            if (data.floorTiles.Count > 0)
            {
                GameObject floorRoot = new GameObject("Floor");
                floorRoot.transform.SetParent(root.transform, false);

                foreach (PlacedTile tile in data.floorTiles)
                {
                    if (tile.tileDefinition == null || tile.tileDefinition.sprite == null) continue;

                    Vector3 worldPos = IsometricGrid.GridToWorld(tile.gridPosition, gs);
                    QuadFactory.CreateFloorQuad(
                        "Floor_" + tile.gridPosition.x + "_" + tile.gridPosition.y,
                        worldPos, tile, gs, floorRoot.transform, false);
                }
            }

            // Walls — WallBuilder mesh
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

            // Roof — QuadFactory mesh + RoofController
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

                    QuadFactory.CreateFloorQuad(
                        "Roof_" + tile.gridPosition.x + "_" + tile.gridPosition.y,
                        worldPos, tile, gs, roofRoot.transform, false);
                }
            }

            // Buildings — prefab instantiation
            if (data.buildings.Count > 0)
            {
                GameObject buildingRoot = new GameObject("Buildings");
                buildingRoot.transform.SetParent(root.transform, false);

                foreach (PlacedBuilding building in data.buildings)
                {
                    if (building.buildingDefinition == null || building.buildingDefinition.prefab == null) continue;

                    Vector3 worldPos = building.freePlace
                        ? building.worldPosition
                        : IsometricGrid.GridToWorld(building.gridPosition, gs);

                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(building.buildingDefinition.prefab);
                    go.name = "Building_" + building.buildingDefinition.displayName;
                    go.transform.SetParent(buildingRoot.transform);
                    go.transform.position = worldPos;
                    go.transform.rotation = Quaternion.Euler(0, building.yRotation, 0);
                    go.transform.localScale = Vector3.one * building.scale;
                }
            }

            // Props — prefab instantiation
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

            int total = data.floorTiles.Count + data.wallTiles.Count + data.roofTiles.Count + data.buildings.Count + data.props.Count;
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

            string dataPath = Application.dataPath;
            if (path.StartsWith(dataPath))
                path = "Assets" + path.Substring(dataPath.Length);

            GameObject root = BakeToScene(data);
            if (root == null) return;

            // ── Building components (same as manual prefab) ──

            // BuildingGlow on root
            if (root.GetComponent<BuildingGlow>() == null)
                root.AddComponent<BuildingGlow>();

            // BoxCollider on root
            if (root.GetComponent<BoxCollider>() == null)
                root.AddComponent<BoxCollider>();

            // Layer = "Building" recursive
            int buildingLayer = LayerMask.NameToLayer("Building");
            if (buildingLayer >= 0)
                SetLayerRecursive(root.transform, buildingLayer);

            // BuildingSortingSetup on all MeshRenderers
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
            {
                if (mr.GetComponent<BuildingSortingSetup>() == null)
                    mr.gameObject.AddComponent<BuildingSortingSetup>();
            }

            // Save prefab
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.UserAction);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();

            // ── Auto-link to BuildingDefinition ──
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (data.outputDefinition != null && prefab != null)
            {
                // Sync existing definition
                Undo.RecordObject(data.outputDefinition, "Sync Prefab");
                data.outputDefinition.prefab = prefab;
                data.outputDefinition.displayName = data.buildingName;
                data.outputDefinition.footprint = data.GetFootprint();
                EditorUtility.SetDirty(data.outputDefinition);
            }
            else if (data.outputDefinition == null && prefab != null)
            {
                // Auto-create BuildingDefinition
                string defFolder = "Assets/IsometricMapEditor/MapData/Sprites/AutoGen/Buildings";
                EnsureFolderExists(defFolder);
                string defPath = AssetDatabase.GenerateUniqueAssetPath($"{defFolder}/{data.buildingName}.asset");

                var def = ScriptableObject.CreateInstance<BuildingDefinition>();
                def.buildingId = data.buildingName.ToLower().Replace(" ", "_");
                def.displayName = data.buildingName;
                def.prefab = prefab;
                def.icon = data.externalIcon;
                def.footprint = data.GetFootprint();

                AssetDatabase.CreateAsset(def, defPath);
                data.outputDefinition = def;
                EditorUtility.SetDirty(data);
                Debug.Log($"[WorkshopBaker] Auto-created BuildingDefinition: {defPath}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[WorkshopBaker] Prefab saved: " + path);
        }

        public static void ClearBaked(BuildingWorkshopData data)
        {
            if (data == null) return;
            string target = BakedPrefix + data.buildingName + "__";
            foreach (GameObject rootGO in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (rootGO.name == target)
                    Undo.DestroyObjectImmediate(rootGO);
            }
        }

        static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursive(t.GetChild(i), layer);
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
