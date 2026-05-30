using UnityEngine;

namespace IsometricMapEditor
{
    [CreateAssetMenu(menuName = "Isometric Map/Tile Definition")]
    public class TileDefinition : ScriptableObject
    {
        public string tileId;
        public Sprite sprite;
        public TileCategory category;
        public bool isWalkable = true;
        public Vector2Int size = Vector2Int.one;
        public int sortingOffset;

        [Header("Material")]
        [Tooltip("Default material for this tile. Applied to SpriteRenderer (floor) or MeshRenderer (wall).")]
        public Material material;

        [Header("Wall Settings (category=Wall only)")]
        public float wallHeight = 2f;
        public float wallThickness = 0.08f;

        [Header("Light Occlusion (빛 차폐)")]
        [Tooltip("체크하면 벽이 플래시라이트 빛을 막는 그림자 전용 박스를 함께 배치한다.")]
        public bool castsShadow;
        [Tooltip("그림자 차폐 박스 목록. 커스텀 형태의 빛 차폐가 필요할 때 사용.")]
        public ShadowBox[] shadowBoxes;

        public bool IsWall => category == TileCategory.Wall;

        // Legacy alias so existing code referencing wallMaterial still compiles
        public Material wallMaterial
        {
            get => material;
            set => material = value;
        }

        void OnValidate()
        {
            if (string.IsNullOrEmpty(tileId))
                tileId = name;
        }
    }

    public enum TileCategory
    {
        Ground,
        Road,
        Decoration,
        Wall,
        Water,
        Custom
    }
}
