using UnityEngine;

namespace TopDownMapEditor
{
    [System.Serializable]
    public class PlacedTile
    {
        public Vector2Int gridPosition;
        public string tileDefinitionId;
        public TileDefinition tileDefinition;
        public int rotation;
        public bool flipX;

        [Tooltip("층 인덱스. 0=1층(바닥), 1=2층 ... 월드 Y = level * GridSettings.levelHeight.")]
        public int level;

        [Tooltip("벽 전용: 이미지 비율을 유지한 채 크기를 키우는 배율 (길이+높이, 두께 제외). 1=정의 기본 크기.")]
        public float wallScale = 1f;

        [Header("벽 자유 배치 (스냅 OFF)")]
        [Tooltip("true면 셀 모서리 스냅 대신 worldPosition/yRotation으로 자유 배치된 벽.")]
        public bool freePlace;

        [Tooltip("자유 배치 벽의 월드 위치(밑동 기준 XZ). freePlace=true일 때만 사용.")]
        public Vector3 worldPosition;

        [Tooltip("자유 배치 벽의 Y축 회전(도). freePlace=true일 때만 사용.")]
        public float yRotation;

        [Tooltip("자유 배치 벽의 고유 식별자. _wallObjects 딕셔너리 키로 사용.")]
        public string id;

        [Tooltip("Override the definition's default material for this instance. Leave null to use default.")]
        public Material materialOverride;

        /// <summary>
        /// Returns materialOverride if set, otherwise falls back to definition's material.
        /// </summary>
        public Material EffectiveMaterial =>
            materialOverride != null ? materialOverride
            : tileDefinition != null ? tileDefinition.material
            : null;
    }
}
