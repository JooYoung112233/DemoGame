using System.Collections.Generic;

namespace IsometricMapEditor
{
    [System.Serializable]
    public class MapLayer
    {
        public string layerName;
        public int sortingLayerOffset;
        public bool isVisible = true;
        public bool isLocked;
        public List<PlacedTile> tiles = new();
    }
}
