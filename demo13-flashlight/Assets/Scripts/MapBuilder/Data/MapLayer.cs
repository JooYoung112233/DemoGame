using System.Collections.Generic;
using UnityEngine;

namespace TopDownMapEditor
{
    [System.Serializable]
    public class MapLayer
    {
        public string layerName;
        public int sortingLayerOffset;
        public bool isVisible = true;
        public bool isLocked;
        public List<PlacedTile> tiles = new();

        [Tooltip("Unity Layer index assigned to objects in this layer. -1 = Default.")]
        public int unityLayer = -1;
    }
}
