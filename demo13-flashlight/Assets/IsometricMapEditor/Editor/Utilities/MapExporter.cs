using UnityEngine;
using UnityEditor;
using System.IO;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public static class MapExporter
    {
        public static void ExportToJson(MapData map, string path = null)
        {
            if (map == null)
            {
                Debug.LogError("[MapExporter] No map data to export.");
                return;
            }

            if (string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFilePanel("Export Map", Application.dataPath, map.mapName, "json");
                if (string.IsNullOrEmpty(path)) return;
            }

            string json = MapSerializer.Serialize(map);
            File.WriteAllText(path, json);
            Debug.Log($"[MapExporter] Exported '{map.mapName}' to {path}");
        }

        public static void ExportToStreamingAssets(MapData map)
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Maps");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, $"{map.mapId}.json");
            ExportToJson(map, path);
            AssetDatabase.Refresh();
        }

        public static void ExportAllMaps()
        {
            string[] guids = AssetDatabase.FindAssets("t:MapData");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var map = AssetDatabase.LoadAssetAtPath<MapData>(assetPath);
                if (map != null)
                    ExportToStreamingAssets(map);
            }
        }
    }
}
