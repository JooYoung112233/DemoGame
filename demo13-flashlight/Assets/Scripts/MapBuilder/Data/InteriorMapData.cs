using UnityEngine;
using System.Collections.Generic;

namespace TopDownMapEditor
{
    [CreateAssetMenu(menuName = "Top-Down Map/Interior Map Data")]
    public class InteriorMapData : ScriptableObject
    {
        public string interiorId;
        public string displayName;
        public GridSettings gridSettings = new();
        public List<MapLayer> layers = new();
        public List<ConnectionPoint> entryPoints = new();
        public List<EscapePoint> escapePoints = new();
        public List<PlacedProp> props = new();
        public List<PlacedHarvestable> harvestables = new();
        public WalkabilityData walkability;

        public void InitializeWalkability()
        {
            walkability = new WalkabilityData(gridSettings.mapWidth, gridSettings.mapHeight);
        }
    }
}
