using UnityEngine;

namespace IsometricMapEditor
{
    [System.Serializable]
    public class PlacedTile
    {
        public Vector2Int gridPosition;
        public string tileDefinitionId;
        public TileDefinition tileDefinition;
        public int rotation;
        public bool flipX;
    }
}
