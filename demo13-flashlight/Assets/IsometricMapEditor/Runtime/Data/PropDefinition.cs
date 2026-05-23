using UnityEngine;

namespace IsometricMapEditor
{
    [CreateAssetMenu(menuName = "Isometric Map/Prop Definition")]
    public class PropDefinition : ScriptableObject
    {
        public string propId;
        public string displayName;
        public Vector2Int footprint = new(1, 1);
        public bool blocksWalkability;
        public bool hasVariants;
        public BuildingVariantSet variants;
        public int sortingOffset;

        [Header("Prefab")]
        [Tooltip("The prefab to instantiate when this prop is placed on the map.")]
        public GameObject prefab;

        [Header("Editor Preview")]
        [Tooltip("Optional icon sprite shown in the palette. If null, uses prefab preview.")]
        public Sprite icon;
    }
}
