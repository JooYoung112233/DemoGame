using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    [InitializeOnLoad]
    public static class ConnectionGizmoDrawer
    {
        static bool showConnections;

        static ConnectionGizmoDrawer()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public static void SetVisible(bool visible) => showConnections = visible;

        static void OnSceneGUI(SceneView sceneView)
        {
            if (!showConnections) return;

            var map = MapEditorWindow.ActiveMap;
            if (map == null) return;

            foreach (var building in map.buildings)
            {
                if (building.buildingDefinition == null) continue;
                if (!building.buildingDefinition.isEnterable) continue;

                Vector3 pos3 = IsometricGrid.GridToWorld(building.gridPosition, map.gridSettings);

                Handles.color = Color.cyan;
                Handles.DrawWireDisc(pos3, Vector3.up, 0.25f);

                var entryCell = building.buildingDefinition.entryCell + building.gridPosition;
                Vector3 entryWorld = IsometricGrid.GridToWorld(entryCell, map.gridSettings);
                Handles.color = Color.green;
                Handles.DrawSolidDisc(entryWorld, Vector3.up, 0.1f);

                Handles.Label(pos3 + Vector3.up * 0.4f, building.buildingDefinition.displayName,
                    new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.cyan } });
            }

            Handles.color = new Color(0, 1, 0, 0.6f);
            foreach (var conn in map.interiorConnections)
            {
                var from = map.buildings.Find(b => b.buildingDefinition?.interiorMapId == conn.fromMapId);
                var to = map.buildings.Find(b => b.buildingDefinition?.interiorMapId == conn.toMapId);
                if (from == null || to == null) continue;

                Vector3 fromWorld = IsometricGrid.GridToWorld(from.gridPosition, map.gridSettings);
                Vector3 toWorld = IsometricGrid.GridToWorld(to.gridPosition, map.gridSettings);

                Handles.DrawDottedLine(fromWorld, toWorld, 4f);

                Vector3 mid = (fromWorld + toWorld) * 0.5f;
                Handles.Label(mid + Vector3.up * 0.2f, conn.type.ToString(),
                    EditorStyles.miniLabel);
            }
        }
    }
}
