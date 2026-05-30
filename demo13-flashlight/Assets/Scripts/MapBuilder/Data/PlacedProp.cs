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

        /// <summary>좌우 반전(미러). 빌보드 스프라이트의 X축 플립.</summary>
        public bool flipX;

        /// <summary>벽 부착 모드. true면 빌보드를 끄고 벽면에 납작하게 세운다(yRotation으로 향하는 벽 결정).</summary>
        public bool wallMounted;

        /// <summary>벽 부착 시 바닥에서 띄우는 높이(m). wallMounted일 때만 적용.</summary>
        public float mountHeight;

        /// <summary>소속 건물 instanceId. 비어있으면 외부 프랍 (항상 표시)</summary>
        public string parentBuildingId;

        /// <summary>이 인스턴스에만 적용되는 추가 정렬 오프셋 ([ / ] 키로 미세조정)</summary>
        public int sortingOffsetOverride;

        /// <summary>층 인덱스. 0=1층 ... 월드 Y = level * GridSettings.levelHeight.</summary>
        public int level;

        /// <summary>이 층의 바닥 월드 Y 오프셋. 비주얼 배치 시 GetWorldPosition에 더한다(거리 탐색은 평면 유지).</summary>
        public float ElevationY(GridSettings settings) => level * (settings != null ? settings.levelHeight : 0f);

        /// <summary>이 인스턴스에만 적용되는 추가 접지 오프셋(맵툴 Move 모드에서 XYZ 미세조정).
        /// 월드 좌표 그대로 더한다(+X 우, +Y 위, +Z 뒤). PropDefinition 기본값 위에 누적된다.</summary>
        public Vector3 groundOffsetOverride;

        /// <summary>접지 미세 보정 월드 오프셋. 정의값(카탈로그 기본) + 인스턴스 오버라이드(맵툴 조정).
        /// 비주얼 배치 지점에서만 더한다(거리/임계값 탐색은 평면 유지).</summary>
        public Vector3 GroundOffsetVec =>
            (propDefinition != null ? propDefinition.GroundOffsetVec : Vector3.zero) + groundOffsetOverride;

        public Vector3 GetWorldPosition(GridSettings settings)
        {
            return freePlace ? worldPosition : IsometricGrid.GridToWorld(gridPosition, settings);
        }
    }
}
