using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public static class IsometricGridGizmoDrawer
    {
        public static void DrawGrid(GridSettings settings)
        {
            Handles.color = new Color(1, 1, 1, 0.12f);

            float halfSize = settings.tileSize * 0.5f;

            for (int x = 0; x <= settings.mapWidth; x++)
            {
                Vector3 start = IsometricGrid.GridToWorld(new Vector2Int(x, 0), settings);
                Vector3 end = IsometricGrid.GridToWorld(new Vector2Int(x, settings.mapHeight), settings);

                Vector3 startPos = new(start.x - halfSize, 0, start.z - halfSize);
                Vector3 endPos = new(end.x - halfSize, 0, end.z - halfSize);
                Handles.DrawLine(startPos, endPos);
            }

            for (int y = 0; y <= settings.mapHeight; y++)
            {
                Vector3 start = IsometricGrid.GridToWorld(new Vector2Int(0, y), settings);
                Vector3 end = IsometricGrid.GridToWorld(new Vector2Int(settings.mapWidth, y), settings);

                Vector3 startPos = new(start.x - halfSize, 0, start.z - halfSize);
                Vector3 endPos = new(end.x - halfSize, 0, end.z - halfSize);
                Handles.DrawLine(startPos, endPos);
            }

        }

        public static void DrawCellHighlight(Vector2Int cell, GridSettings settings, Color color)
        {
            Vector3[] corners = IsometricGrid.GetCellWorldCorners(cell, settings);
            Handles.color = color;
            Handles.DrawAAConvexPolygon(corners);
        }

        public static void DrawTilePreview(Vector2Int cell, GridSettings settings, TileDefinition tileDef)
        {
            if (tileDef == null || tileDef.sprite == null) return;

            Handles.color = new Color(1, 1, 1, 0.5f);
            Vector3[] corners = IsometricGrid.GetCellWorldCorners(cell, settings);
            Handles.DrawAAConvexPolygon(corners);
        }
    }
}
