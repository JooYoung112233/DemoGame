using UnityEngine;

namespace TopDownMapEditor
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

        /// <summary>층 인덱스. 0=1층 ... 월드 Y = level * GridSettings.levelHeight.</summary>
        public int level;

        /// <summary>이 층의 바닥 월드 Y 오프셋. 비주얼 배치 시 GetWorldPosition에 더한다(거리 탐색은 평면 유지).</summary>
        public float ElevationY(GridSettings settings) => level * (settings != null ? settings.levelHeight : 0f);

        public Vector3 GetWorldPosition(GridSettings settings)
        {
            return freePlace ? worldPosition : TopDownGrid.GridToWorld(gridPosition, settings);
        }
    }
}
