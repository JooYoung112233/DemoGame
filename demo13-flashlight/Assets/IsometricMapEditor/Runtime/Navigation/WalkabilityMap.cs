using UnityEngine;

namespace IsometricMapEditor
{
    public class WalkabilityMap : MonoBehaviour
    {
        WalkabilityData _data;
        GridSettings _settings;

        public void Initialize(WalkabilityData data, GridSettings settings)
        {
            _data = data;
            _settings = settings;
        }

        public WalkableType GetCellType(Vector2Int gridPos)
        {
            if (_data == null) return WalkableType.Walkable;
            return _data.GetCell(gridPos);
        }

        public bool IsWalkable(Vector2Int gridPos)
        {
            var type = GetCellType(gridPos);
            return type != WalkableType.Blocked;
        }

        public bool IsWalkableWorld(Vector3 worldPos)
        {
            Vector2Int gridPos = IsometricGrid.WorldToGrid(worldPos, _settings);
            return IsWalkable(gridPos);
        }

        public float GetSpeedMultiplier(Vector2Int gridPos)
        {
            return GetCellType(gridPos) switch
            {
                WalkableType.SlowZone => 0.5f,
                WalkableType.Blocked => 0f,
                _ => 1f
            };
        }
    }
}
