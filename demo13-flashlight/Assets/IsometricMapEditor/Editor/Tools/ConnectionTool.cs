using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public static class ConnectionTool
    {
        static bool isActive;
        static PlacedBuilding selectedBuilding;

        public static bool IsActive => isActive;
        public static void Toggle() => isActive = !isActive;
        public static void SetActive(bool active) => isActive = active;

        public static void OnSceneGUI(SceneView sceneView, MapData map)
        {
            if (!isActive || map == null) return;

            DrawConnectionGizmos(map);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Vector2 mouseWorld = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
                Vector2Int cell = IsometricGrid.WorldToGrid(mouseWorld, map.gridSettings);

                var building = FindBuildingAt(map, cell);
                if (building != null)
                {
                    if (selectedBuilding == null)
                    {
                        selectedBuilding = building;
                    }
                    else if (selectedBuilding != building)
                    {
                        Undo.RecordObject(map, "Create Connection");
                        map.interiorConnections.Add(new InteriorConnection
                        {
                            connectionId = System.Guid.NewGuid().ToString("N")[..8],
                            label = $"{selectedBuilding.buildingDefinition?.displayName} ↔ {building.buildingDefinition?.displayName}",
                            type = InteriorConnectionType.Door,
                            fromMapId = selectedBuilding.buildingDefinition?.interiorMapId ?? "",
                            toMapId = building.buildingDefinition?.interiorMapId ?? ""
                        });
                        EditorUtility.SetDirty(map);
                        selectedBuilding = null;
                    }
                    e.Use();
                }

                sceneView.Repaint();
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                selectedBuilding = null;
                e.Use();
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        static void DrawConnectionGizmos(MapData map)
        {
            foreach (var building in map.buildings)
            {
                if (building.buildingDefinition == null || !building.buildingDefinition.isEnterable) continue;

                Vector2 world = IsometricGrid.GridToWorld(building.gridPosition, map.gridSettings);
                bool isSelected = selectedBuilding == building;
                Handles.color = isSelected ? Color.yellow : Color.cyan;
                Handles.DrawWireDisc(new Vector3(world.x, world.y, 0), Vector3.forward, 0.3f);
            }

            foreach (var conn in map.interiorConnections)
            {
                var fromBuilding = map.buildings.Find(b =>
                    b.buildingDefinition?.interiorMapId == conn.fromMapId);
                var toBuilding = map.buildings.Find(b =>
                    b.buildingDefinition?.interiorMapId == conn.toMapId);

                if (fromBuilding == null || toBuilding == null) continue;

                Vector2 fromWorld = IsometricGrid.GridToWorld(fromBuilding.gridPosition, map.gridSettings);
                Vector2 toWorld = IsometricGrid.GridToWorld(toBuilding.gridPosition, map.gridSettings);

                Handles.color = Color.green;
                Handles.DrawLine(
                    new Vector3(fromWorld.x, fromWorld.y, 0),
                    new Vector3(toWorld.x, toWorld.y, 0));
            }
        }

        static PlacedBuilding FindBuildingAt(MapData map, Vector2Int cell)
        {
            foreach (var building in map.buildings)
            {
                if (building.buildingDefinition == null) continue;
                var cells = building.buildingDefinition.GetOccupiedCells(building.gridPosition);
                if (cells.Contains(cell)) return building;
            }
            return null;
        }
    }
}
