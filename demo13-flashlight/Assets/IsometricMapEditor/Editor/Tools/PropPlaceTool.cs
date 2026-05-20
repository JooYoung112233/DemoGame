using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public static class PropPlaceTool
    {
        static bool isActive;
        public static bool IsActive => isActive;

        public static void Toggle() => isActive = !isActive;
        public static void SetActive(bool active) => isActive = active;

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

            var selectedProp = TilePaletteWindow.SelectedProp;
            if (selectedProp == null) return;

            Event e = Event.current;
            Vector3 mouseWorld = GetMouseWorldOnXZPlane(e);
            Vector2Int cell = IsometricGrid.WorldToGrid(mouseWorld, map.gridSettings);

            if (map.gridSettings.IsInBounds(cell))
            {
                Vector3[] corners = IsometricGrid.GetCellWorldCorners(cell, map.gridSettings);
                Handles.color = new Color(0, 1, 0, 0.5f);
                Handles.DrawAAConvexPolygon(corners);
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (map.gridSettings.IsInBounds(cell))
                {
                    Undo.RecordObject(map, "Place Prop");
                    map.props.Add(new PlacedProp
                    {
                        instanceId = System.Guid.NewGuid().ToString("N")[..8],
                        gridPosition = cell,
                        propDefinitionId = selectedProp.propId,
                        propDefinition = selectedProp
                    });

                    if (selectedProp.blocksWalkability && map.walkability != null)
                    {
                        for (int dx = 0; dx < selectedProp.footprint.x; dx++)
                            for (int dy = 0; dy < selectedProp.footprint.y; dy++)
                                map.walkability.SetCell(cell + new Vector2Int(dx, dy), WalkableType.Blocked);
                    }

                    EditorUtility.SetDirty(map);
                    sceneView.Repaint();
                }
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                var toRemove = map.props.FindIndex(p => p.gridPosition == cell);
                if (toRemove >= 0)
                {
                    Undo.RecordObject(map, "Remove Prop");
                    map.props.RemoveAt(toRemove);
                    EditorUtility.SetDirty(map);
                    sceneView.Repaint();
                }
                e.Use();
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
    }
}
