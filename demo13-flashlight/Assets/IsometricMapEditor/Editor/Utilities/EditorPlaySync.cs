using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    [InitializeOnLoad]
    public static class EditorPlaySync
    {
        const string MapDataPathKey = "IsometricMapEditor_ActiveMapPath";

        static EditorPlaySync()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                var map = MapEditorWindow.ActiveMap;
                if (map != null)
                {
                    string path = AssetDatabase.GetAssetPath(map);
                    EditorPrefs.SetString(MapDataPathKey, path);
                }
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                string path = EditorPrefs.GetString(MapDataPathKey, "");
                if (string.IsNullOrEmpty(path)) return;

                var mapData = AssetDatabase.LoadAssetAtPath<MapData>(path);
                if (mapData == null) return;

                var bootstrapper = Object.FindFirstObjectByType<MapRuntimeBootstrapper>();
                if (bootstrapper != null) return;

                var go = new GameObject("MapRuntimeBootstrapper (Auto)");
                var boot = go.AddComponent<MapRuntimeBootstrapper>();
                boot.SetMapData(mapData);
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                LivePreviewManager.ClearPreview();
            }
        }
    }
}
