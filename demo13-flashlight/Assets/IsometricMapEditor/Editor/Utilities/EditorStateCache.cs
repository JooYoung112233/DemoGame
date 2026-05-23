using UnityEngine;
using UnityEditor;

namespace IsometricMapEditor.Editor
{
    /// <summary>
    /// Persists editor state (active map, enabled toggle, etc.) across domain reloads,
    /// recompilation, and play-mode transitions using EditorPrefs.
    /// All editors (Map, Building Workshop) use this to cache their state so
    /// nothing disappears on OFF/recompile/play mode.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorStateCache
    {
        private const string KEY_MAP_GUID = "IsometricMap_ActiveMapGUID";
        private const string KEY_ENABLED = "IsometricMap_EditorEnabled";
        private const string KEY_LAYER = "IsometricMap_ActiveLayer";
        private const string KEY_TOOL = "IsometricMap_CurrentTool";
        private const string KEY_WORKSHOP_GUID = "IsometricMap_WorkshopDataGUID";
        private const string KEY_WORKSHOP_ENABLED = "IsometricMap_WorkshopEnabled";
        private const string KEY_ZONE_COLLECTION_GUID = "IsometricMap_ZoneCollectionGUID";
        private const string KEY_ZONE_INDEX = "IsometricMap_ActiveZoneIndex";

        static EditorStateCache()
        {
            // Restore state after domain reload
            EditorApplication.delayCall += RestoreAllState;
        }

        // ─── Map Editor State ──────────────────────────────────────────

        public static void SaveMapState(MapData map, bool enabled, string layerName, int tool)
        {
            if (map != null)
            {
                string path = AssetDatabase.GetAssetPath(map);
                string guid = AssetDatabase.AssetPathToGUID(path);
                EditorPrefs.SetString(KEY_MAP_GUID, guid);
            }
            else
            {
                EditorPrefs.DeleteKey(KEY_MAP_GUID);
            }
            EditorPrefs.SetBool(KEY_ENABLED, enabled);
            EditorPrefs.SetString(KEY_LAYER, layerName);
            EditorPrefs.SetInt(KEY_TOOL, tool);
        }

        public static MapData LoadCachedMap()
        {
            string guid = EditorPrefs.GetString(KEY_MAP_GUID, "");
            if (string.IsNullOrEmpty(guid)) return null;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<MapData>(path);
        }

        public static bool LoadCachedEnabled()
        {
            return EditorPrefs.GetBool(KEY_ENABLED, false);
        }

        public static string LoadCachedLayer()
        {
            return EditorPrefs.GetString(KEY_LAYER, "Ground");
        }

        public static int LoadCachedTool()
        {
            return EditorPrefs.GetInt(KEY_TOOL, 0);
        }

        // ─── Building Workshop State ────────────────────────────────────

        public static void SaveWorkshopState(BuildingWorkshopData data, bool enabled)
        {
            if (data != null)
            {
                string path = AssetDatabase.GetAssetPath(data);
                string guid = AssetDatabase.AssetPathToGUID(path);
                EditorPrefs.SetString(KEY_WORKSHOP_GUID, guid);
            }
            else
            {
                EditorPrefs.DeleteKey(KEY_WORKSHOP_GUID);
            }
            EditorPrefs.SetBool(KEY_WORKSHOP_ENABLED, enabled);
        }

        public static BuildingWorkshopData LoadCachedWorkshopData()
        {
            string guid = EditorPrefs.GetString(KEY_WORKSHOP_GUID, "");
            if (string.IsNullOrEmpty(guid)) return null;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<BuildingWorkshopData>(path);
        }

        public static bool LoadCachedWorkshopEnabled()
        {
            return EditorPrefs.GetBool(KEY_WORKSHOP_ENABLED, false);
        }

        // ─── Zone Collection State ──────────────────────────────────────

        public static void SaveZoneState(ZoneCollection collection, int zoneIndex)
        {
            if (collection != null)
            {
                string path = AssetDatabase.GetAssetPath(collection);
                string guid = AssetDatabase.AssetPathToGUID(path);
                EditorPrefs.SetString(KEY_ZONE_COLLECTION_GUID, guid);
            }
            else
            {
                EditorPrefs.DeleteKey(KEY_ZONE_COLLECTION_GUID);
            }
            EditorPrefs.SetInt(KEY_ZONE_INDEX, zoneIndex);
        }

        public static ZoneCollection LoadCachedZoneCollection()
        {
            string guid = EditorPrefs.GetString(KEY_ZONE_COLLECTION_GUID, "");
            if (string.IsNullOrEmpty(guid)) return null;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<ZoneCollection>(path);
        }

        public static int LoadCachedZoneIndex()
        {
            return EditorPrefs.GetInt(KEY_ZONE_INDEX, -1);
        }

        // ─── Restore ────────────────────────────────────────────────────

        private static void RestoreAllState()
        {
            // Map editor will restore its own state in OnEnable
            // LivePreviewManager will detect the restored state and re-render

            // Force LivePreviewManager to re-check by invalidating its tracking
            LivePreviewManager.InvalidateTracking();
        }
    }
}
