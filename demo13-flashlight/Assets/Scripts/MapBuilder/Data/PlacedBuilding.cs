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

        [Header("Free Placement")]
        public bool freePlace;
        public Vector3 worldPosition;
        public float yRotation;
        public float scale = 1f;

        public Vector3 GetWorldPosition(GridSettings settings)
        {
            return freePlace ? worldPosition : IsometricGrid.GridToWorld(gridPosition, settings);
        }
    }
}
