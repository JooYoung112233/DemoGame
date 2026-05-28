using UnityEngine;

namespace IsometricMapEditor
{
    /// <summary>
    /// MapObject 비주얼 프리셋 정의.
    /// 카탈로그에 등록해두면 배치 시 비주얼 설정이 자동 적용됨.
    /// </summary>
    [CreateAssetMenu(menuName = "Isometric Map/MapObject Definition")]
    public class MapObjectDefinition : ScriptableObject
    {
        public string objectId;
        public string displayName;
        public MapObjectType objectType;

        [Header("비주얼")]
        [Tooltip("0=구체(기본), 1=텍스처Quad, 2=투명(콜라이더만), 3=이펙트프리팹")]
        public int visualMode;

        [Tooltip("텍스처Quad 모드용 텍스처")]
        public Texture2D visualTexture;

        [Tooltip("이펙트프리팹 모드용 프리팹")]
        public GameObject effectPrefab;

        [Tooltip("비주얼 스케일")]
        public float visualScale = 1f;

        [Header("기본 설정")]
        public float interactRange = 2f;
        public string promptText;
    }
}
