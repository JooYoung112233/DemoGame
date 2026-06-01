using UnityEngine;

namespace TopDownMapEditor
{
    [System.Serializable]
    public class GridSettings
    {
        [Tooltip("Tile width in world units")]
        public float tileSize = 1f;

        [Tooltip("World-space origin offset")]
        public Vector3 originOffset;

        [Tooltip("Number of grid columns")]
        public int mapWidth = 64;

        [Tooltip("Number of grid rows")]
        public int mapHeight = 64;

        [Tooltip("층(level) 하나의 높이(월드 단위). 2층 = level 1 = Y가 levelHeight만큼 올라감.")]
        public float levelHeight = 3f;

        public GridSettings() { }

        public GridSettings(float tileSize, int mapWidth, int mapHeight)
        {
            this.tileSize = tileSize;
            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;
        }

        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < mapWidth && cell.y >= 0 && cell.y < mapHeight;
        }
    }
}
