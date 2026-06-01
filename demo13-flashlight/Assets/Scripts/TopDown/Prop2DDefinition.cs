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
    [Tooltip("정렬 오프셋 — 같은 위치 스프라이트의 앞뒤. 클수록 앞에 그려짐.")]
    public int sortingOffset;

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
}
