using UnityEngine;

namespace IsometricMapEditor
{
    [System.Serializable]
    public class PlacedHarvestable
    {
        public string instanceId;
        public Vector2Int gridPosition;
        public string harvestableDefinitionId;
        public HarvestableDefinition harvestableDefinition;
        public string overrideSpawnCondition;
    }
}
