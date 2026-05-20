using UnityEngine;

namespace IsometricMapEditor
{
    [System.Serializable]
    public class WalkabilityData
    {
        public int width;
        public int height;
        public WalkableType[] cells;

        public WalkabilityData(int width, int height)
        {
            this.width = width;
            this.height = height;
            cells = new WalkableType[width * height];
        }

        public WalkableType GetCell(Vector2Int pos)
        {
            if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height)
                return WalkableType.Blocked;
            return cells[pos.y * width + pos.x];
        }

        public void SetCell(Vector2Int pos, WalkableType type)
        {
            if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height)
                return;
            cells[pos.y * width + pos.x] = type;
        }
    }

    public enum WalkableType
    {
        Walkable,
        Blocked,
        SlowZone,
        Hazard,
        TriggerZone
    }
}
