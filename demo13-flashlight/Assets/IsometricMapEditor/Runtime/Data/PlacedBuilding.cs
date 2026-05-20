using UnityEngine;

namespace IsometricMapEditor
{
    [System.Serializable]
    public class PlacedBuilding
    {
        public string instanceId;
        public Vector2Int gridPosition;
        public string buildingDefinitionId;
        public BuildingDefinition buildingDefinition;
        public int rotation;
        public string activeVariantId;
        public bool roofVisible = true;
    }
}
