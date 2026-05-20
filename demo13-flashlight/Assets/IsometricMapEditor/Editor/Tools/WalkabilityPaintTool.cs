using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public static class WalkabilityPaintTool
    {
        static WalkableType paintType = WalkableType.Blocked;
        static bool isActive;

        public static bool IsActive => isActive;
        public static WalkableType PaintType => paintType;

        public static void Toggle() => isActive = !isActive;
        public static void SetActive(bool active) => isActive = active;
        public static void SetPaintType(WalkableType type) => paintType = type;

        static Vector3 GetMouseWorldOnXZPlane(Event e)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane xzPlane = new Plane(Vector3.up, Vector3.zero);
            if (xzPlane.Raycast(ray, out float dist))
                return ray.GetPoint(dist);
            return Vector3.zero;
        }

        public static void OnSceneGUI(SceneView sceneView, MapData map)
        {
            if (!isActive || map == null) return;

            if (map.walkability == null)
            {
                map.InitializeWalkability();
                EditorUtility.SetDirty(map);
            }

            DrawWalkabilityOverlay(map);

            Event e = Event.current;
            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            {
                if (e.button == 0)
                {
                    Vector3 mouseWorld = GetMouseWorldOnXZPlane(e);
                    Vector2Int cell = IsometricGrid.WorldToGrid(mouseWorld, map.gridSettings);

                    if (map.gridSettings.IsInBounds(cell))
                    {
                        Undo.RecordObject(map, "Paint Walkability");
                        map.walkability.SetCell(cell, paintType);
                        EditorUtility.SetDirty(map);
                        sceneView.Repaint();
                    }
                    e.Use();
                }
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        static void DrawWalkabilityOverlay(MapData map)
        {
            if (map.walkability == null) return;
            var settings = map.gridSettings;

            for (int x = 0; x < settings.mapWidth; x++)
            {
                for (int y = 0; y < settings.mapHeight; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var type = map.walkability.GetCell(pos);
                    if (type == WalkableType.Walkable) continue;

                    Color color = type switch
                    {
                        WalkableType.Blocked => new Color(1, 0, 0, 0.3f),
                        WalkableType.SlowZone => new Color(1, 1, 0, 0.3f),
                        WalkableType.Hazard => new Color(1, 0.5f, 0, 0.3f),
                        WalkableType.TriggerZone => new Color(0, 0.5f, 1, 0.3f),
                        _ => Color.clear
                    };

                    Vector3[] corners = IsometricGrid.GetCellWorldCorners(pos, settings);
                    Handles.color = color;
                    Handles.DrawAAConvexPolygon(corners);
                }
            }
        }
    }
}
