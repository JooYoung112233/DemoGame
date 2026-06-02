using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 탑다운 2D 프롭 카탈로그 항목. 스프라이트 + 막힘(Collider2D) 설정을 정의한다.
///
/// 막힘 규칙: 비-트리거 Collider2D가 곧 "못 가는 곳"(동적 Rigidbody2D 플레이어가 부딪힘).
/// 콜라이더가 없거나(None) isTrigger면 "지나갈 수 있는" 장식/이벤트.
///
/// 콜라이더 모드(프롭마다 선택):
///   None      — 콜라이더 없음(바닥 장식, 통과 가능).
///   Box       — 스프라이트 크기에 맞춘 BoxCollider2D 1개(배율·오프셋 조정 가능).
///   Polygon   — 스프라이트 외곽선(physics shape)을 따라 PolygonCollider2D 자동 생성
///               (각진/오목/구멍/다중 path 자동 — 나무·바위 등).
///   Composite — 박스 여러 개를 직접 배치(각진 구조물을 2~3개 박스로 합성).
/// </summary>
[CreateAssetMenu(menuName = "TopDown 2D/Prop Definition", fileName = "prop_")]
public class Prop2DDefinition : ScriptableObject
{
    public enum ColliderMode { None, Box, Polygon, Composite }

    /// <summary>카탈로그 탭 분류. 데이터는 동일(스프라이트+콜라이더)하고 탭 정리·기본값에만 쓰인다.
    /// Floor=바닥(보통 콜라이더 없음), Wall=벽(막힘), Prop=장식/오브젝트, Object=스폰/상호작용 마커.</summary>
    public enum Category { Floor, Wall, Prop, Object }

    /// <summary>로컬 단위 박스 하나(Composite 모드). 월드 단위 = 픽셀/PixelsPerUnit.</summary>
    [System.Serializable]
    public struct ColliderBox
    {
        public Vector2 center;
        public Vector2 size;
        public ColliderBox(Vector2 c, Vector2 s) { center = c; size = s; }
    }

    [Header("Identity")]
    public string propId;
    public string displayName;
    [Tooltip("카탈로그 탭 분류(바닥/벽/프롭/오브젝트).")]
    public Category category = Category.Prop;

    [Header("Visual")]
    public Sprite sprite;
    [Tooltip("스프라이트 머티리얼(선택). 비우면 SpriteRenderer 기본.")]
    public Material material;
    [Tooltip("Sorting Layer 이름 (SpriteRenderer.sortingLayerName). 예: Ground/Wall/Object.")]
    public string sortingLayer = "Default";
    [Tooltip("Order in Layer — 같은 Sorting Layer 내 앞뒤. 클수록 앞에 그려짐.")]
    public int sortingOffset;

    [Header("Draw Mode (벽 타일링 등)")]
    [Tooltip("Simple=원본 1장. Tiled=Size만큼 스프라이트 반복(벽을 길게). Sliced=9-slice. " +
             "Tiled/Sliced는 스프라이트 임포트 Mesh Type=Full Rect 필요.")]
    public SpriteDrawMode drawMode = SpriteDrawMode.Simple;
    [Tooltip("Tiled/Sliced일 때 렌더 크기(월드 단위). 예: 벽 길이×두께.")]
    public Vector2 tiledSize = new(1f, 1f);
    [Tooltip("Tiled일 때 반복 방식. Continuous=가장자리 잘림, Adaptive=정수배로 늘려 안 잘림.")]
    public SpriteTileMode tileMode = SpriteTileMode.Continuous;

    [Header("Auto Prefab")]
    [Tooltip("카탈로그가 자동 생성/갱신하는 프리팹(SpriteRenderer+Collider2D). 씬 배치 시 이걸 인스턴스화.")]
    public GameObject prefab;

    [Header("Collider (막힘 영역)")]
    public ColliderMode colliderMode = ColliderMode.Box;
    [Tooltip("true면 통과 가능한 트리거(이벤트/감지용). false면 물리적으로 막음.")]
    public bool isTrigger = false;

    [Header("Box 모드")]
    [Tooltip("스프라이트 크기 대비 배율(1,1=그림 크기 그대로).")]
    public Vector2 boxSizeScale = Vector2.one;
    [Tooltip("박스 중심 오프셋(로컬 단위). 스프라이트 중심 기준.")]
    public Vector2 boxOffset = Vector2.zero;

    [Header("Composite 모드")]
    [Tooltip("박스 여러 개(로컬 단위). 각진/오목 모양을 박스 2~3개로 합성.")]
    public List<ColliderBox> compositeBoxes = new();

    [Header("Shadow (투영 그림자)")]
    [Tooltip("켜면 FlatShadow로 그림자(정적 발밑 + 동적 투영)를 드리움. 벽/프롭에 권장.")]
    public bool castShadow = false;
    [Tooltip("동적 투영 방향 기준. Player=플레이어 따라(권장), NearestLight=최근접 점광, Manual=고정 방향.")]
    public FlatShadow.DirMode shadowDirMode = FlatShadow.DirMode.Player;
    [Tooltip("(레거시) 그림자는 항상 WallPixel(_SHADOW_MODE)로 통일돼서 현재 효과 없음.")]
    public bool shadowBaseContact = true;
    [Tooltip("그림자 최대 길이(월드 단위).")]
    public float shadowMaxLength = 1.4f;
    [Range(0f, 1f)]
    [Tooltip("투영 그림자 진하기.")]
    public float shadowStrength = 0.55f;
}
