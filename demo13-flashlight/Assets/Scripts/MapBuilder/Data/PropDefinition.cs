using UnityEngine;

namespace IsometricMapEditor
{
    /// <summary>
    /// 빛 차폐용 그림자 프록시 박스 하나의 사양.
    /// 비주얼(2D 스프라이트)은 납작해서 그림자가 안 나오므로,
    /// 렌더되지 않는(ShadowsOnly) 얇은 3D 박스를 함께 배치해 플래시라이트 빛을 막는다.
    /// 대각선/ㅅ/V자 벽은 yaw로 비스듬히 눕혀서 스프라이트 라인에 맞춘다.
    /// </summary>
    [System.Serializable]
    public struct ShadowBox
    {
        [Tooltip("박스 크기 (X=벽 길이/폭, Y=높이, Z=두께). 대각선은 길이를 길게·두께를 얇게.")]
        public Vector3 size;
        [Tooltip("프롭 기준 로컬 위치 오프셋. Y는 보통 높이의 절반.")]
        public Vector3 offset;
        [Tooltip("박스 자체의 Y회전(도). 대각선/ㅅ/V자 벽을 스프라이트에 맞춰 눕힐 때 사용.")]
        public float yaw;
    }

    [CreateAssetMenu(menuName = "Isometric Map/Prop Definition")]
    public class PropDefinition : ScriptableObject
    {
        public string propId;
        public string displayName;
        public Vector2Int footprint = new(1, 1);
        public bool blocksWalkability;
        public bool hasVariants;
        public BuildingVariantSet variants;
        public int sortingOffset;

        [Header("Prefab")]
        [Tooltip("The prefab to instantiate when this prop is placed on the map.")]
        public GameObject prefab;

        [Header("Light Occlusion (빛 차폐 / 벽)")]
        [Tooltip("체크하면 이 프롭이 플래시라이트 빛을 막는 그림자 전용 박스를 함께 배치한다. (솔리드 벽/컨테이너용. 철창 펜스 등 see-through는 끈다)")]
        public bool castsShadow;
        [Tooltip("그림자 차폐 박스 목록. 대각선 벽=1개(yaw 45), ㅅ/V자=2개 조합.")]
        public ShadowBox[] shadowBoxes;

        [Header("Editor Preview")]
        [Tooltip("Optional icon sprite shown in the palette. If null, uses prefab preview.")]
        public Sprite icon;
    }
}
