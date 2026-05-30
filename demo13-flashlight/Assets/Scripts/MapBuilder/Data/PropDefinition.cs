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
        [Tooltip("프롭 비주얼 크기(타일 단위, 소수 가능). 1=한 타일 폭, 0.5=반 타일. 스프라이트 폭을 이 값에 맞춰 스케일하고 nav 박스 크기로도 쓴다. 그리드 점유 검사엔 사용 안 함(프롭은 자유 배치).")]
        public Vector2 footprint = new(1f, 1f);
        public bool blocksWalkability;
        public bool hasVariants;
        public BuildingVariantSet variants;
        public int sortingOffset;

        [Header("Visual (Quad)")]
        [Tooltip("프롭 비주얼 스프라이트. 런타임에 카메라를 바라보는 빌보드 쿼드로 생성된다.")]
        public Sprite sprite;

        [Tooltip("쿼드에 적용할 머티리얼(선택). 비우면 SpriteRenderer 기본 머티리얼을 쓴다.")]
        public Material material;

        [Tooltip("접지 미세 조정 Y — 프롭을 순수 수직(월드 Y)으로 내릴 양. " +
                 "양수면 아래로 내리고 음수면 위로 올린다. 스프라이트 하단에 투명 여백/그림자가 있어 " +
                 "떠 보일 때 이 값을 +로 조금씩 올려 바닥(초록 원) 안에 앉힌다.")]
        public float groundOffset;

        [Tooltip("접지 미세 조정 Z — 프롭을 월드 Z축으로 밀 양. 양수=+Z. 바닥 평면에서 앞뒤 위치를 맞출 때.")]
        public float groundOffsetZ;

        /// <summary>접지 미세 보정 월드 오프셋. Y는 아래로(−), Z는 그대로(+). 비주얼 배치 지점에서만 더한다.</summary>
        public Vector3 GroundOffsetVec => new Vector3(0f, -groundOffset, groundOffsetZ);

        /// <summary>스프라이트 기반(쿼드) 프롭인지 여부.</summary>
        public bool UsesQuad => sprite != null;

        [Header("Wall Mount (벽 부착 — 포스터/액자/스위치 등)")]
        [Tooltip("체크하면 벽에 거는 장식으로 취급한다. 배치 시 빌보드를 끄고 벽면에 납작하게 세우며, 기본 높이(defaultMountHeight)만큼 띄운다. (Q/E로 향하는 벽 방향, PageUp/Down으로 높이 조절)")]
        public bool wallMountable;

        [Tooltip("벽 부착 시 바닥에서 띄울 기본 높이(m). 배치 후 PageUp/Down으로 인스턴스별 미세 조정.")]
        public float defaultMountHeight = 1.2f;

        [Header("Light Occlusion (빛 차폐 / 벽) — 쿼드 프롭은 사용 안 함")]
        [Tooltip("체크하면 이 프롭이 플래시라이트 빛을 막는 그림자 전용 박스를 함께 배치한다. (솔리드 벽/컨테이너용. 철창 펜스 등 see-through는 끈다)")]
        public bool castsShadow;
        [Tooltip("그림자 차폐 박스 목록. 대각선 벽=1개(yaw 45), ㅅ/V자=2개 조합.")]
        public ShadowBox[] shadowBoxes;
    }
}
