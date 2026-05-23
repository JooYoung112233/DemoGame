using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    [InitializeOnLoad]
    public static class SceneViewInputHandler
    {
        static SceneViewInputHandler()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            var map = MapEditorWindow.ActiveMap;
            if (map == null || !MapEditorWindow.EditorEnabled) return;

            if (WalkabilityPaintTool.IsActive)
                WalkabilityPaintTool.OnSceneGUI(sceneView, map);

            if (RoadTool.IsActive)
                RoadTool.OnSceneGUI(sceneView, map);

            if (PropPlaceTool.IsActive)
                PropPlaceTool.OnSceneGUI(sceneView, map);

            if (BuildingPlaceTool.IsActive)
                BuildingPlaceTool.OnSceneGUI(sceneView, map);

            if (ConnectionTool.IsActive)
                ConnectionTool.OnSceneGUI(sceneView, map);

            DrawCursorInfo(sceneView, map);
        }

        static Vector3 GetMouseWorldOnXZPlane(Event e)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane xzPlane = new Plane(Vector3.up, Vector3.zero);
            if (xzPlane.Raycast(ray, out float dist))
                return ray.GetPoint(dist);
            return Vector3.zero;
        }

        static void DrawCursorInfo(SceneView sceneView, MapData map)
        {
            Event e = Event.current;
            if (e.type != EventType.Repaint) return;

            Vector3 mouseWorld = GetMouseWorldOnXZPlane(e);
            Vector2Int cell = IsometricGrid.WorldToGrid(mouseWorld, map.gridSettings);

            if (!map.gridSettings.IsInBounds(cell)) return;

            Handles.BeginGUI();
            var rect = new Rect(10, sceneView.position.height - 60, 300, 20);

            string info = $"Grid: ({cell.x}, {cell.y})";

            if (map.walkability != null)
            {
                var walkType = map.walkability.GetCell(cell);
                info += $" | Walk: {walkType}";
            }

            var tile = map.GetTileAt(cell);
            if (tile != null)
                info += $" | Tile: {tile.tileDefinitionId}";

            GUI.Label(rect, info, EditorStyles.helpBox);
            Handles.EndGUI();
        }
    }
}
