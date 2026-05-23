using UnityEngine;
using UnityEditor;
using System.IO;

namespace IsometricMapEditor.Editor
{
    /// <summary>
    /// Generates prefabs from sprites for buildings and props.
    /// Creates a vertical Quad (MeshRenderer) with material, saved as .prefab.
    /// Building prefabs get BuildingGlow (root) + BuildingSortingSetup (quad child).
    /// Prop prefabs get BuildingSortingSetup (quad child) only.
    /// </summary>
    public static class PrefabGenerator
    {
        const string DefaultPrefabFolder = "Assets/IsometricMapEditor/Prefabs";

        /// <summary>
        /// Create a building prefab from a sprite.
        /// Structure: Root(BuildingGlow) → Quad child(MeshFilter+MeshRenderer+BuildingSortingSetup)
        /// </summary>
        public static GameObject CreateBuildingPrefab(Sprite sprite, Material material, string outputFolder = null)
        {
            if (sprite == null) return null;

            if (string.IsNullOrEmpty(outputFolder))
                outputFolder = DefaultPrefabFolder + "/Buildings";

            EnsureFolderExists(outputFolder);

            string safeName = SanitizeFileName(sprite.name);
            string prefabPath = $"{outputFolder}/{safeName}.prefab";

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            // Calculate size from sprite
            float ppu = sprite.pixelsPerUnit;
            float w = sprite.rect.width / ppu;
            float h = sprite.rect.height / ppu;

            // Create Quad child
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = safeName;

            // Remove default collider
            var col = quad.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            quad.transform.localScale = new Vector3(w, h, 1f);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localRotation = Quaternion.Euler(26, 42, 0); // match isometric camera (26, 42, 0)

            // Apply material
            var renderer = quad.GetComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
            else
                renderer.sharedMaterial = CreateFallbackMaterial(sprite, outputFolder, safeName);

            // Add BuildingSortingSetup to quad child
            quad.AddComponent<BuildingSortingSetup>();

            // Set Unity Layer
            int buildingLayer = LayerMask.NameToLayer("Building");
            if (buildingLayer >= 0)
                quad.layer = buildingLayer;

            // Create root parent
            var root = new GameObject(safeName);
            if (buildingLayer >= 0)
                root.layer = buildingLayer;
            quad.transform.SetParent(root.transform);

            // Add BuildingGlow to root — auto-collects child MeshRenderers
            root.AddComponent<BuildingGlow>();

            // Add BoxCollider on root for blocking movement (size adjustable per prefab)
            root.AddComponent<BoxCollider>();

            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            return prefab;
        }

        /// <summary>
        /// Create a prop prefab from a sprite.
        /// Structure: Root → Quad child(MeshFilter+MeshRenderer+BuildingSortingSetup)
        /// Props don't get BuildingGlow (no window glow needed).
        /// </summary>
        public static GameObject CreatePropPrefab(Sprite sprite, Material material, string outputFolder = null)
        {
            if (sprite == null) return null;

            if (string.IsNullOrEmpty(outputFolder))
                outputFolder = DefaultPrefabFolder + "/Props";

            EnsureFolderExists(outputFolder);

            string safeName = SanitizeFileName(sprite.name);
            string prefabPath = $"{outputFolder}/{safeName}.prefab";

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            float ppu = sprite.pixelsPerUnit;
            float w = sprite.rect.width / ppu;
            float h = sprite.rect.height / ppu;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = safeName;

            var col = quad.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            quad.transform.localScale = new Vector3(w, h, 1f);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localRotation = Quaternion.Euler(26, 42, 0); // match isometric camera (26, 42, 0)

            var renderer = quad.GetComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
            else
                renderer.sharedMaterial = CreateFallbackMaterial(sprite, outputFolder, safeName);

            // Add BuildingSortingSetup to quad child
            quad.AddComponent<BuildingSortingSetup>();

            // Set Unity Layer
            int propLayer = LayerMask.NameToLayer("Building");
            if (propLayer >= 0)
                quad.layer = propLayer;

            var root = new GameObject(safeName);
            if (propLayer >= 0)
                root.layer = propLayer;
            quad.transform.SetParent(root.transform);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            return prefab;
        }

        /// <summary>
        /// Legacy: kept for backward compat. Calls CreateBuildingPrefab with no material.
        /// </summary>
        public static GameObject CreateSpritePrefab(Sprite sprite, string outputFolder = null, bool vertical = true)
        {
            return CreateBuildingPrefab(sprite, null, outputFolder);
        }

        /// <summary>
        /// Creates a fallback alpha-cutout material when no material is provided.
        /// </summary>
        static Material CreateFallbackMaterial(Sprite sprite, string outputFolder, string safeName)
        {
            string matPath = $"{outputFolder}/{safeName}_Mat.mat";
            var existingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existingMat != null) return existingMat;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                mat = new Material(Shader.Find("Unlit/Transparent Cutout"));

            if (mat.HasProperty("_Cutoff"))
                mat.SetFloat("_Cutoff", 0.5f);
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 0);
                mat.SetFloat("_AlphaClip", 1);
            }

            mat.mainTexture = sprite.texture;

            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", sprite.texture);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);

            var rect = sprite.textureRect;
            float texW = sprite.texture.width;
            float texH = sprite.texture.height;
            var offset = new Vector2(rect.x / texW, rect.y / texH);
            var scale = new Vector2(rect.width / texW, rect.height / texH);
            mat.mainTextureOffset = offset;
            mat.mainTextureScale = scale;
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTextureOffset("_BaseMap", offset);
                mat.SetTextureScale("_BaseMap", scale);
            }

            AssetDatabase.CreateAsset(mat, matPath);
            return mat;
        }

        /// <summary>
        /// Batch: generate building prefabs for all sprites in a folder.
        /// </summary>
        public static int BatchCreateBuildingPrefabs(string spriteFolder, string prefabOutputFolder)
        {
            if (!AssetDatabase.IsValidFolder(spriteFolder)) return 0;

            EnsureFolderExists(prefabOutputFolder);
            int created = 0;

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { spriteFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                string fileDir = Path.GetDirectoryName(path).Replace('\\', '/');
                if (fileDir != spriteFolder)
                    continue;

                var objects = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var obj in objects)
                {
                    if (obj is Sprite sprite)
                    {
                        if (sprite.name.ToLower().Contains("glow"))
                            continue;

                        string safeName = SanitizeFileName(sprite.name);
                        string prefabPath = $"{prefabOutputFolder}/{safeName}.prefab";
                        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                            continue;

                        CreateBuildingPrefab(sprite, null, prefabOutputFolder);
                        created++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return created;
        }

        /// <summary>
        /// Batch: generate prop prefabs for all sprites in a folder.
        /// </summary>
        public static int BatchCreatePropPrefabs(string spriteFolder, string prefabOutputFolder)
        {
            if (!AssetDatabase.IsValidFolder(spriteFolder)) return 0;

            EnsureFolderExists(prefabOutputFolder);
            int created = 0;

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { spriteFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                string fileDir = Path.GetDirectoryName(path).Replace('\\', '/');
                if (fileDir != spriteFolder)
                    continue;

                var objects = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var obj in objects)
                {
                    if (obj is Sprite sprite)
                    {
                        if (sprite.name.ToLower().Contains("glow"))
                            continue;

                        string safeName = SanitizeFileName(sprite.name);
                        string prefabPath = $"{prefabOutputFolder}/{safeName}.prefab";
                        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                            continue;

                        CreatePropPrefab(sprite, null, prefabOutputFolder);
                        created++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return created;
        }

        /// <summary>
        /// Generate prefab from sprite and assign to BuildingDefinition.
        /// </summary>
        public static GameObject GenerateForBuilding(BuildingDefinition def, Sprite sprite, string outputFolder = null)
        {
            if (def == null || sprite == null) return null;

            if (string.IsNullOrEmpty(outputFolder))
                outputFolder = DefaultPrefabFolder + "/Buildings";

            var prefab = CreateBuildingPrefab(sprite, null, outputFolder);
            if (prefab != null)
            {
                Undo.RecordObject(def, "Assign Prefab");
                def.prefab = prefab;
                if (def.icon == null) def.icon = sprite;
                EditorUtility.SetDirty(def);
            }
            return prefab;
        }

        /// <summary>
        /// Generate prefab from sprite and assign to PropDefinition.
        /// </summary>
        public static GameObject GenerateForProp(PropDefinition def, Sprite sprite, string outputFolder = null)
        {
            if (def == null || sprite == null) return null;

            if (string.IsNullOrEmpty(outputFolder))
                outputFolder = DefaultPrefabFolder + "/Props";

            var prefab = CreatePropPrefab(sprite, null, outputFolder);
            if (prefab != null)
            {
                Undo.RecordObject(def, "Assign Prefab");
                def.prefab = prefab;
                if (def.icon == null) def.icon = sprite;
                EditorUtility.SetDirty(def);
            }
            return prefab;
        }

        static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
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
