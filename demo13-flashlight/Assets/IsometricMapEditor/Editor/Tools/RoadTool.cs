using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public static class RoadTool
    {
        static RoadDefinition activeRoad;
        static bool isActive;

        public static bool IsActive => isActive;
        public static RoadDefinition ActiveRoad => activeRoad;

        public static void Toggle() => isActive = !isActive;
        public static void SetActive(bool active) => isActive = active;
        public static void SetRoad(RoadDefinition road) => activeRoad = road;

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
            if (!isActive || map == null || activeRoad == null) return;

            Event e = Event.current;
            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            {
                if (e.button == 0)
                {
                    Vector3 mouseWorld = GetMouseWorldOnXZPlane(e);
                    Vector2Int cell = IsometricGrid.WorldToGrid(mouseWorld, map.gridSettings);

                    if (map.gridSettings.IsInBounds(cell))
                    {
                        Undo.RecordObject(map, "Paint Road");
                        PlaceRoad(map, cell);
                        UpdateNeighborRoads(map, cell);
                        EditorUtility.SetDirty(map);
                        sceneView.Repaint();
                    }
                    e.Use();
                }
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        static void PlaceRoad(MapData map, Vector2Int cell)
        {
            int bitmask = CalculateBitmask(map, cell);
            var tile = new PlacedTile
            {
                gridPosition = cell,
                tileDefinitionId = activeRoad.roadId
            };
            map.PlaceTile(tile, "Roads");
        }

        static void UpdateNeighborRoads(MapData map, Vector2Int cell)
        {
            Vector2Int[] neighbors = {
                cell + Vector2Int.up,
                cell + Vector2Int.down,
                cell + Vector2Int.left,
                cell + Vector2Int.right
            };

            foreach (var neighbor in neighbors)
            {
                if (!map.gridSettings.IsInBounds(neighbor)) continue;
                var existingTile = map.GetTileAt(neighbor);
                if (existingTile != null && existingTile.tileDefinitionId == activeRoad.roadId)
                {
                    int newBitmask = CalculateBitmask(map, neighbor);
                    existingTile.rotation = newBitmask;
                }
            }
        }

        static int CalculateBitmask(MapData map, Vector2Int cell)
        {
            int mask = 0;
            if (IsRoad(map, cell + Vector2Int.up)) mask |= 1;
            if (IsRoad(map, cell + Vector2Int.right)) mask |= 2;
            if (IsRoad(map, cell + Vector2Int.down)) mask |= 4;
            if (IsRoad(map, cell + Vector2Int.left)) mask |= 8;
            return mask;
        }

        static bool IsRoad(MapData map, Vector2Int cell)
        {
            if (!map.gridSettings.IsInBounds(cell)) return false;
            var tile = map.GetTileAt(cell);
            return tile != null && tile.tileDefinitionId == activeRoad?.roadId;
        }
    }
}
