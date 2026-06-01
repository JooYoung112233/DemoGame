using UnityEngine;
using System.Collections.Generic;

namespace TopDownMapEditor
{
    [CreateAssetMenu(menuName = "Top-Down Map/Harvestable Definition")]
    public class HarvestableDefinition : ScriptableObject
    {
        public string harvestableId;
        public string displayName;
        public Sprite sprite;
        public Sprite depletedSprite;
        public List<DropEntry> dropTable = new();
        public float respawnTimeSec;
        public bool respawnOnMapReload;
        public List<SpawnCondition> spawnConditions = new();
        public int sortingOffset;
    }

    [System.Serializable]
    public class DropEntry
    {
        public string itemId;
        public int minAmount = 1;
        public int maxAmount = 1;
        [Range(0f, 1f)]
        public float dropRate = 1f;
    }
}
