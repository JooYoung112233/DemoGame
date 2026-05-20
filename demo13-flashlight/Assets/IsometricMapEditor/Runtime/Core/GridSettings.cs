using UnityEngine;

namespace IsometricMapEditor
{
    [System.Serializable]
    public class GridSettings
    {
        [Tooltip("Tile width in pixels (horizontal span of diamond)")]
        public int tileWidth = 128;

        [Tooltip("Tile height in pixels (vertical span of diamond, typically width/2)")]
        public int tileHeight = 64;

        [Tooltip("World-space origin offset for grid (0,0)")]
        public Vector2 originOffset;

        [Tooltip("Number of grid columns")]
        public int mapWidth = 64;

        [Tooltip("Number of grid rows")]
        public int mapHeight = 64;

        public float TileWorldWidth => tileWidth / 100f;
        public float TileWorldHeight => tileHeight / 100f;

        public GridSettings() { }

        public GridSettings(int tileWidth, int tileHeight, int mapWidth, int mapHeight)
        {
            this.tileWidth = tileWidth;
            this.tileHeight = tileHeight;
            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;
        }

        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < mapWidth && cell.y >= 0 && cell.y < mapHeight;
        }
    }
}
