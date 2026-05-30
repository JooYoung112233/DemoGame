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

        /// <summary>이 인스턴스에만 적용되는 추가 정렬 오프셋 ([ / ] 키로 미세조정)</summary>
        public int sortingOffsetOverride;

        public Vector3 GetWorldPosition(GridSettings settings)
        {
            return freePlace ? worldPosition : IsometricGrid.GridToWorld(gridPosition, settings);
        }
    }
}
