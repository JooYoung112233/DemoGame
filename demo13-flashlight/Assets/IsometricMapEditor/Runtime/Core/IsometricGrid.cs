using UnityEngine;

namespace IsometricMapEditor
{
    public static class IsometricGrid
    {
        public static Vector2 GridToWorld(Vector2Int cell, GridSettings settings)
        {
            float w = settings.TileWorldWidth;
            float h = settings.TileWorldHeight;
            float x = (cell.x - cell.y) * (w * 0.5f);
            float y = (cell.x + cell.y) * (h * 0.5f);
            return new Vector2(x, -y) + settings.originOffset;
        }

        public static Vector2Int WorldToGrid(Vector2 worldPos, GridSettings settings)
        {
            float w = settings.TileWorldWidth;
            float h = settings.TileWorldHeight;
            Vector2 local = worldPos - settings.originOffset;
            local.y = -local.y;
            float col = (local.x / (w * 0.5f) + local.y / (h * 0.5f)) * 0.5f;
            float row = (local.y / (h * 0.5f) - local.x / (w * 0.5f)) * 0.5f;
            return new Vector2Int(Mathf.RoundToInt(col), Mathf.RoundToInt(row));
        }

        public static int GetSortingOrder(Vector2Int cell, int layerOffset = 0)
        {
            return (cell.x + cell.y) * 10 + layerOffset;
        }

        public static Vector3[] GetCellWorldCorners(Vector2Int cell, GridSettings settings)
        {
            float w = settings.TileWorldWidth;
            float h = settings.TileWorldHeight;
            Vector2 center = GridToWorld(cell, settings);
            float hw = w * 0.5f;
            float hh = h * 0.5f;
            return new Vector3[]
            {
                new(center.x,      center.y + hh, 0), // top
                new(center.x + hw, center.y,      0), // right
                new(center.x,      center.y - hh, 0), // bottom
                new(center.x - hw, center.y,      0), // left
            };
        }

        public static Rect GetMapWorldBounds(GridSettings settings)
        {
            Vector2 topLeft = GridToWorld(new Vector2Int(0, 0), settings);
            Vector2 topRight = GridToWorld(new Vector2Int(settings.mapWidth - 1, 0), settings);
            Vector2 bottomLeft = GridToWorld(new Vector2Int(0, settings.mapHeight - 1), settings);
            Vector2 bottomRight = GridToWorld(new Vector2Int(settings.mapWidth - 1, settings.mapHeight - 1), settings);

            float minX = Mathf.Min(topLeft.x, bottomLeft.x, topRight.x, bottomRight.x);
            float maxX = Mathf.Max(topLeft.x, bottomLeft.x, topRight.x, bottomRight.x);
            float minY = Mathf.Min(topLeft.y, bottomLeft.y, topRight.y, bottomRight.y);
            float maxY = Mathf.Max(topLeft.y, bottomLeft.y, topRight.y, bottomRight.y);

            float hw = settings.TileWorldWidth * 0.5f;
            float hh = settings.TileWorldHeight * 0.5f;

            return new Rect(minX - hw, minY - hh, (maxX - minX) + hw * 2, (maxY - minY) + hh * 2);
        }
    }
}
