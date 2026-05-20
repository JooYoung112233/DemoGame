using UnityEngine;

namespace IsometricMapEditor
{
    [CreateAssetMenu(menuName = "Isometric Map/Prop Definition")]
    public class PropDefinition : ScriptableObject
    {
        public string propId;
        public string displayName;
        public Sprite sprite;
        public Vector2Int footprint = new(1, 1);
        public bool blocksWalkability;
        public bool hasVariants;
        public BuildingVariantSet variants;
        public int sortingOffset;
    }
}
