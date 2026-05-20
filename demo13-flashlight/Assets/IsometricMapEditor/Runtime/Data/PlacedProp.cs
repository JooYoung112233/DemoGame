using UnityEngine;

namespace IsometricMapEditor
{
    [System.Serializable]
    public class PlacedProp
    {
        public string instanceId;
        public Vector2Int gridPosition;
        public string propDefinitionId;
        public PropDefinition propDefinition;
        public int rotation;
        public string activeVariantId;
    }
}
