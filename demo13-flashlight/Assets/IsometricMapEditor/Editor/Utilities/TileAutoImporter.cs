using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public static class TileAutoImporter
    {
        const string DefaultTileSpritePath = "Assets/IsometricMapEditor/SampleData/Sprites/Tiles";
        const string DefaultBuildingSpritePath = "Assets/IsometricMapEditor/SampleData/Sprites/Buildings";
        const string DefaultPropSpritePath = "Assets/IsometricMapEditor/SampleData/Sprites/Props";
        const string DefaultAssetOutputPath = "Assets/IsometricMapEditor/SampleData/AutoGen";

        public static string TileSpritePath
        {
            get => EditorPrefs.GetString("IsometricMap_TileSpritePath", DefaultTileSpritePath);
            set => EditorPrefs.SetString("IsometricMap_TileSpritePath", value);
        }

        public static string BuildingSpritePath
        {
            get => EditorPrefs.GetString("IsometricMap_BuildingSpritePath", DefaultBuildingSpritePath);
            set => EditorPrefs.SetString("IsometricMap_BuildingSpritePath", value);
        }

        public static string PropSpritePath
        {
            get => EditorPrefs.GetString("IsometricMap_PropSpritePath", DefaultPropSpritePath);
            set => EditorPrefs.SetString("IsometricMap_PropSpritePath", value);
        }

        public static string AssetOutputPath
        {
            get => EditorPrefs.GetString("IsometricMap_AssetOutputPath", DefaultAssetOutputPath);
            set => EditorPrefs.SetString("IsometricMap_AssetOutputPath", value);
        }

        public static int SyncTiles()
        {
            return SyncSpritesToDefinitions<TileDefinition>(
                TileSpritePath,
                AssetOutputPath + "/Tiles",
                "t:TileDefinition",
                (sprite, path) =>
                {
                    var tile = ScriptableObject.CreateInstance<TileDefinition>();
                    tile.tileId = sprite.name;
                    tile.sprite = sprite;
                    tile.category = GuessTileCategory(sprite.name);
                    tile.isWalkable = tile.category != TileCategory.Wall && tile.category != TileCategory.Water;
                    tile.size = Vector2Int.one;
                    AssetDatabase.CreateAsset(tile, path);
                    return tile;
                },
                existing => existing.sprite
            );
        }

        public static int SyncBuildings()
        {
            string prefabFolder = AssetOutputPath + "/Prefabs/Buildings";
            return SyncSpritesToDefinitions<BuildingDefinition>(
                BuildingSpritePath,
                AssetOutputPath + "/Buildings",
                "t:BuildingDefinition",
                (sprite, path) =>
                {
                    var building = ScriptableObject.CreateInstance<BuildingDefinition>();
                    building.buildingId = sprite.name;
                    building.displayName = FormatDisplayName(sprite.name);
                    building.icon = sprite;
                    building.footprint = new Vector2Int(2, 2);
                    // Auto-generate prefab from sprite
                    building.prefab = PrefabGenerator.CreateBuildingPrefab(sprite, null, prefabFolder);
                    AssetDatabase.CreateAsset(building, path);
                    return building;
                },
                existing => existing.icon
            );
        }

        public static int SyncProps()
        {
            string prefabFolder = AssetOutputPath + "/Prefabs/Props";
            return SyncSpritesToDefinitions<PropDefinition>(
                PropSpritePath,
                AssetOutputPath + "/Props",
                "t:PropDefinition",
                (sprite, path) =>
                {
                    var prop = ScriptableObject.CreateInstance<PropDefinition>();
                    prop.propId = sprite.name;
                    prop.displayName = FormatDisplayName(sprite.name);
                    prop.icon = sprite;
                    prop.footprint = Vector2Int.one;
                    // Auto-generate prefab from sprite
                    prop.prefab = PrefabGenerator.CreatePropPrefab(sprite, null, prefabFolder);
                    AssetDatabase.CreateAsset(prop, path);
                    return prop;
                },
                existing => existing.icon
            );
        }

        public static (int tiles, int buildings, int props) SyncAll()
        {
            int t = SyncTiles();
            int b = SyncBuildings();
            int p = SyncProps();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return (t, b, p);
        }

        static int SyncSpritesToDefinitions<T>(
            string spriteFolderPath,
            string outputFolder,
            string assetFilter,
            System.Func<Sprite, string, T> createAsset,
            System.Func<T, Sprite> getSprite
        ) where T : ScriptableObject
        {
            if (!AssetDatabase.IsValidFolder(spriteFolderPath))
                return 0;

            EnsureFolderExists(outputFolder);

            var existingSprites = new HashSet<string>();
            string[] existingGuids = AssetDatabase.FindAssets(assetFilter);
            foreach (string guid in existingGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null) continue;
                var sprite = getSprite(asset);
                if (sprite != null)
                    existingSprites.Add(sprite.name);
            }

            // Only scan the target folder (no subfolders)
            string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { spriteFolderPath });
            int created = 0;

            foreach (string guid in spriteGuids)
            {
                string spritePath = AssetDatabase.GUIDToAssetPath(guid);

                // Skip files in subfolders — only process direct children
                string fileDir = Path.GetDirectoryName(spritePath).Replace('\\', '/');
                if (fileDir != spriteFolderPath)
                    continue;

                var sprites = LoadAllSpritesAtPath(spritePath);

                foreach (var sprite in sprites)
                {
                    // Skip glow / effect sprites
                    if (sprite.name.ToLower().Contains("glow"))
                        continue;

                    if (existingSprites.Contains(sprite.name))
                        continue;

                    string safeName = SanitizeFileName(sprite.name);
                    string assetPath = $"{outputFolder}/{safeName}.asset";

                    if (AssetDatabase.LoadAssetAtPath<T>(assetPath) != null)
                        continue;

                    createAsset(sprite, assetPath);
                    existingSprites.Add(sprite.name);
                    created++;
                }
            }

            return created;
        }

        static List<Sprite> LoadAllSpritesAtPath(string assetPath)
        {
            var result = new List<Sprite>();
            var objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var obj in objects)
            {
                if (obj is Sprite sprite)
                    result.Add(sprite);
            }

            if (result.Count == 0)
            {
                var single = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (single != null)
                    result.Add(single);
            }
            return result;
        }

        static TileCategory GuessTileCategory(string name)
        {
            string lower = name.ToLower();
            if (lower.Contains("wall") || lower.Contains("brick") || lower.Contains("fence"))
                return TileCategory.Wall;
            if (lower.Contains("water") || lower.Contains("lake") || lower.Contains("river"))
                return TileCategory.Water;
            if (lower.Contains("road") || lower.Contains("path") || lower.Contains("bridge"))
                return TileCategory.Road;
            if (lower.Contains("flower") || lower.Contains("deco") || lower.Contains("tree") || lower.Contains("bush"))
                return TileCategory.Decoration;
            return TileCategory.Ground;
        }

        static string FormatDisplayName(string name)
        {
            return name.Replace("_", " ").Replace("-", " ");
        }

        static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

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
