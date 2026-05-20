using UnityEngine;

namespace IsometricMapEditor
{
    public static class IsometricGrid
    {
        /// <summary>
        /// Grid cell → World position on XZ plane (Y=0).
        /// Tiles laid out on a flat XZ grid, camera provides the isometric look.
        /// </summary>
        public static Vector3 GridToWorld(Vector2Int cell, GridSettings settings)
        {
            float x = cell.x * settings.tileSize;
            float z = cell.y * settings.tileSize;
            return new Vector3(x, 0f, z) + settings.originOffset;
        }

        /// <summary>
        /// World position → nearest grid cell (ignores Y height).
        /// </summary>
        public static Vector2Int WorldToGrid(Vector3 worldPos, GridSettings settings)
        {
            Vector3 local = worldPos - settings.originOffset;
            int col = Mathf.RoundToInt(local.x / settings.tileSize);
            int row = Mathf.RoundToInt(local.z / settings.tileSize);
            return new Vector2Int(col, row);
        }

        /// <summary>
        /// Sorting order for sprite rendering. Higher row+col = rendered later (in front).
        /// Floor parameter reserved for future multi-floor support.
        /// </summary>
        public static int GetSortingOrder(Vector2Int cell, int layerOffset = 0, int floor = 0)
        {
            return floor * 1000 + (cell.x + cell.y) * 10 + layerOffset;
        }

        /// <summary>
        /// Returns 4 corners of a cell on the XZ plane (Y=0).
        /// </summary>
        public static Vector3[] GetCellWorldCorners(Vector2Int cell, GridSettings settings)
        {
            Vector3 center = GridToWorld(cell, settings);
            float half = settings.tileSize * 0.5f;
            return new Vector3[]
            {
                new(center.x - half, 0, center.z + half), // top-left
                new(center.x + half, 0, center.z + half), // top-right
                new(center.x + half, 0, center.z - half), // bottom-right
                new(center.x - half, 0, center.z - half), // bottom-left
            };
        }

        /// <summary>
        /// Axis-aligned bounding box of the entire map on XZ plane.
        /// </summary>
        public static Bounds GetMapWorldBounds(GridSettings settings)
        {
            Vector3 min = GridToWorld(Vector2Int.zero, settings);
            Vector3 max = GridToWorld(new Vector2Int(settings.mapWidth - 1, settings.mapHeight - 1), settings);
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = new(
                Mathf.Abs(max.x - min.x) + settings.tileSize,
                0.1f,
                Mathf.Abs(max.z - min.z) + settings.tileSize
            );
            return new Bounds(center, size);
        }
    }
}
