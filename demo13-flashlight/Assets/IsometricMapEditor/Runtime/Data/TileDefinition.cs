using UnityEngine;

namespace IsometricMapEditor
{
    [CreateAssetMenu(menuName = "Isometric Map/Tile Definition")]
    public class TileDefinition : ScriptableObject
    {
        public string tileId;
        public Sprite sprite;
        public TileCategory category;
        public bool isWalkable = true;
        public Vector2Int size = Vector2Int.one;
        public int sortingOffset;

        void OnValidate()
        {
            if (string.IsNullOrEmpty(tileId))
                tileId = name;
        }
    }

    public enum TileCategory
    {
        Ground,
        Road,
        Decoration,
        Wall,
        Water,
        Custom
    }
}
