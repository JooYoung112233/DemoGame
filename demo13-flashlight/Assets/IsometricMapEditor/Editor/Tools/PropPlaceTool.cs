using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public static class PropPlaceTool
    {
        static bool isActive;
        static bool freeMode = true;
        static float placementRotation;
        static float placementScale = 1f;

        public static bool IsActive => isActive;
        public static bool FreeMode { get => freeMode; set => freeMode = value; }
        public static float PlacementRotation { get => placementRotation; set => placementRotation = value; }
        public static float PlacementScale { get => placementScale; set => placementScale = Mathf.Max(0.1f, value); }

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

            var selectedProp = MapEditorWindow.SelectedProp;
            if (selectedProp == null) return;

            Event e = Event.current;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            // Q/E = rotate 15°, +/- = scale
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Q) { placementRotation = (placementRotation - 15f) % 360f; e.Use(); sceneView.Repaint(); }
                if (e.keyCode == KeyCode.E) { placementRotation = (placementRotation + 15f) % 360f; e.Use(); sceneView.Repaint(); }
                if (e.keyCode == KeyCode.Equals || e.keyCode == KeyCode.KeypadPlus) { placementScale = Mathf.Min(5f, placementScale + 0.1f); e.Use(); sceneView.Repaint(); }
                if (e.keyCode == KeyCode.Minus || e.keyCode == KeyCode.KeypadMinus) { placementScale = Mathf.Max(0.1f, placementScale - 0.1f); e.Use(); sceneView.Repaint(); }
            }

            Vector3 mouseWorld = GetMouseWorldOnXZPlane(e);

            if (freeMode)
                HandleFreeMode(e, sceneView, map, selectedProp, mouseWorld);
            else
                HandleGridMode(e, sceneView, map, selectedProp, mouseWorld);
        }

        static void HandleFreeMode(Event e, SceneView sceneView, MapData map, PropDefinition selectedProp, Vector3 mouseWorld)
        {
            // Preview cursor with rotation/scale
            float size = map.gridSettings.tileSize * 0.3f * placementScale;
            Handles.color = new Color(0, 1, 0.5f, 0.8f);
            Handles.DrawWireDisc(mouseWorld, Vector3.up, size);
            Vector3 forward = Quaternion.Euler(0, placementRotation, 0) * Vector3.forward * size;
            Handles.DrawLine(mouseWorld, mouseWorld + forward);
            Handles.Label(mouseWorld + Vector3.up * 0.3f,
                $"{selectedProp.displayName}\nRot:{placementRotation:F0}° Scale:{placementScale:F1}x",
                EditorStyles.whiteBoldLabel);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Undo.RecordObject(map, "Place Prop (Free)");
                map.props.Add(new PlacedProp
                {
                    instanceId = System.Guid.NewGuid().ToString("N")[..8],
                    gridPosition = IsometricGrid.WorldToGrid(mouseWorld, map.gridSettings),
                    propDefinitionId = selectedProp.propId,
                    propDefinition = selectedProp,
                    freePlace = true,
                    worldPosition = mouseWorld,
                    yRotation = placementRotation,
                    scale = placementScale
                });
                EditorUtility.SetDirty(map);
                sceneView.Repaint();
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                RemoveNearestProp(map, mouseWorld, sceneView);
                e.Use();
            }

            sceneView.Repaint();
        }

        static void HandleGridMode(Event e, SceneView sceneView, MapData map, PropDefinition selectedProp, Vector3 mouseWorld)
        {
            Vector2Int cell = IsometricGrid.WorldToGrid(mouseWorld, map.gridSettings);

            if (map.gridSettings.IsInBounds(cell))
            {
                Vector3[] corners = IsometricGrid.GetCellWorldCorners(cell, map.gridSettings);
                Handles.color = new Color(0, 1, 0, 0.5f);
                Handles.DrawAAConvexPolygon(corners);

                Vector3 labelPos = IsometricGrid.GridToWorld(cell, map.gridSettings) + Vector3.up * 0.3f;
                string label = $"{selectedProp.displayName}\nRot:{placementRotation:F0}° Scale:{placementScale:F1}x";
                Handles.Label(labelPos, label, EditorStyles.whiteBoldLabel);
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
                        propDefinition = selectedProp,
                        freePlace = false,
                        yRotation = placementRotation,
                        scale = placementScale
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
                var toRemove = map.props.FindIndex(p => !p.freePlace && p.gridPosition == cell);
                if (toRemove >= 0)
                {
                    Undo.RecordObject(map, "Remove Prop");
                    map.props.RemoveAt(toRemove);
                    EditorUtility.SetDirty(map);
                    sceneView.Repaint();
                }
                e.Use();
            }
        }

        static void RemoveNearestProp(MapData map, Vector3 mouseWorld, SceneView sceneView)
        {
            float bestDist = float.MaxValue;
            int bestIdx = -1;

            for (int i = 0; i < map.props.Count; i++)
            {
                var p = map.props[i];
                Vector3 pos = p.GetWorldPosition(map.gridSettings);
                float dist = Vector3.Distance(pos, mouseWorld);
                if (dist < bestDist && dist < map.gridSettings.tileSize * 1.5f)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            if (bestIdx >= 0)
            {
                Undo.RecordObject(map, "Remove Prop");
                map.props.RemoveAt(bestIdx);
                EditorUtility.SetDirty(map);
                sceneView.Repaint();
            }
        }
    }
}
