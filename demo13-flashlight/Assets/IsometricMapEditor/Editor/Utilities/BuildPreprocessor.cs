using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public class BuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            string[] guids = AssetDatabase.FindAssets("t:MapData");
            bool hasErrors = false;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var map = AssetDatabase.LoadAssetAtPath<MapData>(path);
                if (map == null) continue;

                var result = MapValidator.Validate(map);
                MapValidator.LogResults(result, map.mapName);

                if (!result.IsValid)
                    hasErrors = true;
            }

            if (hasErrors)
            {
                throw new BuildFailedException(
                    "[IsometricMapEditor] Build aborted: map validation failed. Check console for details.");
            }

            MapExporter.ExportAllMaps();
            Debug.Log("[BuildPreprocessor] All maps validated and exported.");
        }
    }
}
