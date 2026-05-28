using UnityEngine;

namespace IsometricMapEditor
{
    public class MapDebugOverlay : MonoBehaviour
    {
        public bool showGrid;
        public bool showWalkability;
        public bool showConnections;
        public KeyCode toggleKey = KeyCode.F1;

        MapData mapData;
        bool _isVisible;

        public void Initialize(MapData map)
        {
            mapData = map;
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                _isVisible = !_isVisible;
        }

        void OnDrawGizmos()
        {
            if (!_isVisible || mapData == null) return;

            if (showGrid) DrawGrid();
            if (showWalkability) DrawWalkability();
            if (showConnections) DrawConnections();
        }

        void DrawGrid()
        {
            var settings = mapData.gridSettings;
            Gizmos.color = new Color(1, 1, 1, 0.2f);

            for (int x = 0; x <= settings.mapWidth; x++)
            {
                for (int y = 0; y <= settings.mapHeight; y++)
                {
                    Vector3[] corners = IsometricGrid.GetCellWorldCorners(new Vector2Int(x, y), settings);
                    for (int i = 0; i < 4; i++)
                        Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
                }
            }
        }

        void DrawWalkability()
        {
            if (mapData.walkability == null) return;

            var settings = mapData.gridSettings;
            for (int x = 0; x < settings.mapWidth; x++)
            {
                for (int y = 0; y < settings.mapHeight; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var type = mapData.walkability.GetCell(pos);
                    if (type == WalkableType.Walkable) continue;

                    Gizmos.color = type switch
                    {
                        WalkableType.Blocked => new Color(1, 0, 0, 0.4f),
                        WalkableType.SlowZone => new Color(1, 1, 0, 0.4f),
                        WalkableType.Hazard => new Color(1, 0.5f, 0, 0.4f),
                        WalkableType.TriggerZone => new Color(0, 0, 1, 0.4f),
                        _ => Color.clear
                    };

                    Vector3 world = IsometricGrid.GridToWorld(pos, settings);
                    Gizmos.DrawCube(world, new Vector3(0.3f, 0.05f, 0.3f));
                }
            }
        }

        void DrawConnections()
        {
            foreach (var building in mapData.buildings)
            {
                if (building.buildingDefinition == null || !building.buildingDefinition.isEnterable) continue;

                Vector3 world = IsometricGrid.GridToWorld(building.gridPosition, mapData.gridSettings);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(world, 0.3f);
            }
        }
    }
}
