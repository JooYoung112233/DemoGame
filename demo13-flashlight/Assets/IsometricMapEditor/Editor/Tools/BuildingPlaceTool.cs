using UnityEngine;
using UnityEditor;

namespace IsometricMapEditor.Editor
{
    public static class BuildingPlaceTool
    {
        private static bool _isActive;
        private static bool _freeMode = true;
        private static float _placementRotation;
        private static float _placementScale = 1f;

        public static bool IsActive
        {
            get { return _isActive; }
        }

        public static bool FreeMode
        {
            get { return _freeMode; }
            set { _freeMode = value; }
        }

        public static float PlacementRotation
        {
            get { return _placementRotation; }
            set { _placementRotation = value; }
        }

        public static float PlacementScale
        {
            get { return _placementScale; }
            set { _placementScale = Mathf.Max(0.1f, value); }
        }

        public static void SetActive(bool active)
        {
            _isActive = active;
        }

        private static Vector3 GetMouseWorldOnXZPlane(Event e)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane xzPlane = new Plane(Vector3.up, Vector3.zero);
            float dist;
            if (xzPlane.Raycast(ray, out dist))
                return ray.GetPoint(dist);
            return Vector3.zero;
        }

        public static void OnSceneGUI(SceneView sceneView, MapData map)
        {
            if (!_isActive || map == null) return;

            BuildingDefinition selectedBuilding = MapEditorWindow.SelectedBuilding;
            if (selectedBuilding == null) return;

            Event e = Event.current;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Q)
                {
                    _placementRotation = (_placementRotation - 15f) % 360f;
                    e.Use();
                    sceneView.Repaint();
                }
                if (e.keyCode == KeyCode.E)
                {
                    _placementRotation = (_placementRotation + 15f) % 360f;
                    e.Use();
                    sceneView.Repaint();
                }
                if (e.keyCode == KeyCode.Equals || e.keyCode == KeyCode.KeypadPlus)
                {
                    _placementScale = Mathf.Min(5f, _placementScale + 0.1f);
                    e.Use();
                    sceneView.Repaint();
                }
                if (e.keyCode == KeyCode.Minus || e.keyCode == KeyCode.KeypadMinus)
                {
                    _placementScale = Mathf.Max(0.1f, _placementScale - 0.1f);
                    e.Use();
                    sceneView.Repaint();
                }
            }

            Vector3 mouseWorld = GetMouseWorldOnXZPlane(e);

            if (_freeMode)
                HandleFreeMode(e, sceneView, map, selectedBuilding, mouseWorld);
            else
                HandleGridMode(e, sceneView, map, selectedBuilding, mouseWorld);
        }

        private static void HandleFreeMode(Event e, SceneView sceneView, MapData map,
            BuildingDefinition def, Vector3 mouseWorld)
        {
            float size = map.gridSettings.tileSize * 0.4f * _placementScale;
            Handles.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            Handles.DrawWireDisc(mouseWorld, Vector3.up, size);
            Vector3 forward = Quaternion.Euler(0, _placementRotation, 0) * Vector3.forward * size;
            Handles.DrawLine(mouseWorld, mouseWorld + forward);

            string label = def.displayName + "\nRot:" + _placementRotation.ToString("F0")
                + " Scale:" + _placementScale.ToString("F1") + "x";
            Handles.Label(mouseWorld + Vector3.up * 0.4f, label, EditorStyles.whiteBoldLabel);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Undo.RecordObject(map, "Place Building (Free)");
                PlacedBuilding building = new PlacedBuilding();
                building.instanceId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
                building.gridPosition = IsometricGrid.WorldToGrid(mouseWorld, map.gridSettings);
                building.buildingDefinitionId = def.buildingId;
                building.buildingDefinition = def;

                building.freePlace = true;
                building.worldPosition = mouseWorld;
                building.yRotation = _placementRotation;
                building.scale = _placementScale;
                map.buildings.Add(building);
                EditorUtility.SetDirty(map);
                sceneView.Repaint();
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                RemoveNearestBuilding(map, mouseWorld, sceneView);
                e.Use();
            }

            sceneView.Repaint();
        }

        private static void HandleGridMode(Event e, SceneView sceneView, MapData map,
            BuildingDefinition def, Vector3 mouseWorld)
        {
            Vector2Int cell = IsometricGrid.WorldToGrid(mouseWorld, map.gridSettings);

            if (map.gridSettings.IsInBounds(cell))
            {
                Vector3[] corners = IsometricGrid.GetCellWorldCorners(cell, map.gridSettings);
                Handles.color = new Color(0.2f, 0.6f, 1f, 0.5f);
                Handles.DrawAAConvexPolygon(corners);

                if (def.footprint.x > 1 || def.footprint.y > 1)
                {
                    Handles.color = new Color(0.2f, 0.6f, 1f, 0.25f);
                    for (int dx = 0; dx < def.footprint.x; dx++)
                    {
                        for (int dy = 0; dy < def.footprint.y; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            Vector2Int fc = new Vector2Int(cell.x + dx, cell.y + dy);
                            if (map.gridSettings.IsInBounds(fc))
                            {
                                Vector3[] fc_corners = IsometricGrid.GetCellWorldCorners(fc, map.gridSettings);
                                Handles.DrawAAConvexPolygon(fc_corners);
                            }
                        }
                    }
                }

                // Show rotation/scale info label
                Vector3 labelPos = IsometricGrid.GridToWorld(cell, map.gridSettings) + Vector3.up * 0.4f;
                string label = def.displayName + "\nRot:" + _placementRotation.ToString("F0")
                    + " Scale:" + _placementScale.ToString("F1") + "x";
                Handles.Label(labelPos, label, EditorStyles.whiteBoldLabel);
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (map.gridSettings.IsInBounds(cell))
                {
                    Undo.RecordObject(map, "Place Building");
                    PlacedBuilding building = new PlacedBuilding();
                    building.instanceId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
                    building.gridPosition = cell;
                    building.buildingDefinitionId = def.buildingId;
                    building.buildingDefinition = def;
    
                    building.freePlace = false;
                    building.yRotation = _placementRotation;
                    building.scale = _placementScale;
                    map.buildings.Add(building);
                    EditorUtility.SetDirty(map);
                    sceneView.Repaint();
                }
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                int toRemove = -1;
                for (int i = 0; i < map.buildings.Count; i++)
                {
                    if (!map.buildings[i].freePlace && map.buildings[i].gridPosition == cell)
                    {
                        toRemove = i;
                        break;
                    }
                }
                if (toRemove >= 0)
                {
                    Undo.RecordObject(map, "Remove Building");
                    map.buildings.RemoveAt(toRemove);
                    EditorUtility.SetDirty(map);
                    sceneView.Repaint();
                }
                e.Use();
            }
        }

        private static void RemoveNearestBuilding(MapData map, Vector3 mouseWorld, SceneView sceneView)
        {
            float bestDist = float.MaxValue;
            int bestIdx = -1;

            for (int i = 0; i < map.buildings.Count; i++)
            {
                PlacedBuilding b = map.buildings[i];
                Vector3 pos = b.GetWorldPosition(map.gridSettings);
                float dist = Vector3.Distance(pos, mouseWorld);
                if (dist < bestDist && dist < map.gridSettings.tileSize * 2f)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            if (bestIdx >= 0)
            {
                Undo.RecordObject(map, "Remove Building");
                map.buildings.RemoveAt(bestIdx);
                EditorUtility.SetDirty(map);
                sceneView.Repaint();
            }
        }
    }
}
