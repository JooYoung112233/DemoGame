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

        /// <summary>층 인덱스. 0=1층 ... 월드 Y = level * GridSettings.levelHeight.</summary>
        public int level;
    }
}
