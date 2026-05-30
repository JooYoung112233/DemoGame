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

        [Header("Free Placement")]
        public bool freePlace;
        public Vector3 worldPosition;
        public float yRotation;
        public float scale = 1f;

        /// <summary>소속 건물 instanceId. 비어있으면 외부 프랍 (항상 표시)</summary>
        public string parentBuildingId;

        /// <summary>이 인스턴스에만 적용되는 추가 정렬 오프셋 ([ / ] 키로 미세조정)</summary>
        public int sortingOffsetOverride;

        public Vector3 GetWorldPosition(GridSettings settings)
        {
            return freePlace ? worldPosition : IsometricGrid.GridToWorld(gridPosition, settings);
        }
    }
}
