using UnityEngine;
using System.Collections.Generic;

namespace IsometricMapEditor
{
    [CreateAssetMenu(menuName = "Isometric Map/Building Definition")]
    public class BuildingDefinition : ScriptableObject
    {
        public string buildingId;
        public string displayName;
        public Vector2Int footprint = new(2, 2);
        public int sortingOffset;
        public bool isEnterable;
        public string interiorMapId;
        public Vector2Int entryCell;
        public BuildingVariantSet variantSet;

        [Header("Prefab")]
        [Tooltip("The prefab to instantiate when this building is placed on the map.")]
        public GameObject prefab;

        [Header("Material Preset")]
        [Tooltip("셰이더 설정 프리셋. 지정하면 프리팹의 텍스처는 유지하고 셰이더 파라미터만 덮어씁니다.")]
        public Material materialPreset;

        [Header("Interior Occlusion (투명 전환)")]
        [Tooltip("플레이어가 건물 내부에 있을 때 이 파트를 투명하게 만든다. (윗벽/아랫벽/천장 등)")]
        public bool occludesInterior;

        [Header("Light Occlusion (빛 차폐)")]
        [Tooltip("체크하면 플래시라이트 빛을 막는 그림자 전용 박스를 함께 배치한다.")]
        public bool castsShadow;
        [Tooltip("그림자 차폐 박스 목록.")]
        public ShadowBox[] shadowBoxes;

        public bool IsMultiTile => footprint.x > 1 || footprint.y > 1;

        public List<Vector2Int> GetOccupiedCells(Vector2Int origin)
        {
            var cells = new List<Vector2Int>();
            for (int x = 0; x < footprint.x; x++)
                for (int y = 0; y < footprint.y; y++)
                    cells.Add(new Vector2Int(origin.x + x, origin.y + y));
            return cells;
        }

        public int GetFrontSortingOrder(Vector2Int origin)
        {
            int frontX = origin.x + footprint.x - 1;
            int frontY = origin.y + footprint.y - 1;
            return IsometricGrid.GetSortingOrder(new Vector2Int(frontX, frontY));
        }
    }
}
