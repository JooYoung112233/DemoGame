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

            for (int x = 0; x <= settings.mapWidth; x++)
            {
                Vector2 start = IsometricGrid.GridToWorld(new Vector2Int(x, 0), settings);
                Vector2 end = IsometricGrid.GridToWorld(new Vector2Int(x, settings.mapHeight), settings);

                float hw = settings.TileWorldWidth * 0.5f;
                float hh = settings.TileWorldHeight * 0.5f;

                Vector3 startPos = new(start.x - hw, start.y, 0);
                Vector3 endPos = new(end.x - hw, end.y, 0);
                Handles.DrawLine(startPos, endPos);
            }

            for (int y = 0; y <= settings.mapHeight; y++)
            {
                Vector2 start = IsometricGrid.GridToWorld(new Vector2Int(0, y), settings);
                Vector2 end = IsometricGrid.GridToWorld(new Vector2Int(settings.mapWidth, y), settings);

                float hw = settings.TileWorldWidth * 0.5f;
                float hh = settings.TileWorldHeight * 0.5f;

                Vector3 startPos = new(start.x - hw, start.y, 0);
                Vector3 endPos = new(end.x - hw, end.y, 0);
                Handles.DrawLine(startPos, endPos);
            }

            DrawDiamondGrid(settings);
        }

        static void DrawDiamondGrid(GridSettings settings)
        {
            int maxCells = settings.mapWidth * settings.mapHeight;
            if (maxCells > 10000) return;

            Handles.color = new Color(1, 1, 1, 0.08f);

            for (int x = 0; x < settings.mapWidth; x++)
            {
                for (int y = 0; y < settings.mapHeight; y++)
                {
                    Vector3[] corners = IsometricGrid.GetCellWorldCorners(new Vector2Int(x, y), settings);
                    Handles.DrawLine(corners[0], corners[1]);
                    Handles.DrawLine(corners[1], corners[2]);
                    Handles.DrawLine(corners[2], corners[3]);
                    Handles.DrawLine(corners[3], corners[0]);
                }
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

            Vector2 worldPos = IsometricGrid.GridToWorld(cell, settings);
            Handles.color = new Color(1, 1, 1, 0.5f);
            Vector3[] corners = IsometricGrid.GetCellWorldCorners(cell, settings);
            Handles.DrawAAConvexPolygon(corners);
        }
    }
}
