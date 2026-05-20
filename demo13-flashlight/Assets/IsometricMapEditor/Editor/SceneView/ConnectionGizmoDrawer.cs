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

                Vector2 world = IsometricGrid.GridToWorld(building.gridPosition, map.gridSettings);
                Vector3 pos3 = new(world.x, world.y, 0);

                Handles.color = Color.cyan;
                Handles.DrawWireDisc(pos3, Vector3.forward, 0.25f);

                var entryCell = building.buildingDefinition.entryCell + building.gridPosition;
                Vector2 entryWorld = IsometricGrid.GridToWorld(entryCell, map.gridSettings);
                Handles.color = Color.green;
                Handles.DrawSolidDisc(new Vector3(entryWorld.x, entryWorld.y, 0), Vector3.forward, 0.1f);

                Handles.Label(pos3 + Vector3.up * 0.4f, building.buildingDefinition.displayName,
                    new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.cyan } });
            }

            Handles.color = new Color(0, 1, 0, 0.6f);
            foreach (var conn in map.interiorConnections)
            {
                var from = map.buildings.Find(b => b.buildingDefinition?.interiorMapId == conn.fromMapId);
                var to = map.buildings.Find(b => b.buildingDefinition?.interiorMapId == conn.toMapId);
                if (from == null || to == null) continue;

                Vector2 fromWorld = IsometricGrid.GridToWorld(from.gridPosition, map.gridSettings);
                Vector2 toWorld = IsometricGrid.GridToWorld(to.gridPosition, map.gridSettings);

                Handles.DrawDottedLine(
                    new Vector3(fromWorld.x, fromWorld.y, 0),
                    new Vector3(toWorld.x, toWorld.y, 0), 4f);

                Vector2 mid = (fromWorld + toWorld) * 0.5f;
                Handles.Label(new Vector3(mid.x, mid.y + 0.2f, 0), conn.type.ToString(),
                    EditorStyles.miniLabel);
            }
        }
    }
}
