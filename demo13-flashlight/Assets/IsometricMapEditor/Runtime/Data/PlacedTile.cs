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

        [Tooltip("Override the definition's default material for this instance. Leave null to use default.")]
        public Material materialOverride;

        /// <summary>
        /// Returns materialOverride if set, otherwise falls back to definition's material.
        /// </summary>
        public Material EffectiveMaterial =>
            materialOverride != null ? materialOverride
            : tileDefinition != null ? tileDefinition.material
            : null;
    }
}
