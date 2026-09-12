using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 쿼터뷰 3D 프롭 카탈로그 항목 — 맵에 세울 물건 하나의 정의.
///
/// 2D판(Prop2DDefinition — 2026-09-12 삭제)은 스프라이트 + `Collider2D` + 정렬 순서가 중심이었다.
/// 3D에서는 그 셋 중 **정렬 순서가 통째로 사라지고**(깊이는 카메라가 푼다), 대신
/// **높이**가 들어온다 — 무엇이 시야를 막고 무엇을 넘어갈 수 있는지가 높이로 갈린다.
///
/// <b>모델이 없어도 선다.</b> <see cref="mesh"/>를 비워 두면 <see cref="size"/> 크기의
/// 상자로 대신 세운다. 맵을 먼저 짓고 모델을 나중에 끼우기 위한 것이다 — 마을·은신처를
/// 그렇게 지었고, 프랍 63종이 나오기 전에 레이드 맵을 세우려면 이 길밖에 없다.
///
/// 막힘 규칙(2D와 같다): 비-트리거 콜라이더 = 못 가는 곳. 없거나 트리거면 지나갈 수 있다.
///
/// 설계: docs/3d-migration.md Stage 3, docs/prop-catalog.md
/// </summary>
[CreateAssetMenu(menuName = "TopDown 3D/Prop Definition", fileName = "p3_")]
public class Prop3DDefinition : ScriptableObject
{
    /// <summary>콜라이더 모양.
    /// None=통과(바닥 장식) / Box=크기에 맞춘 상자(대부분) / Mesh=메시 그대로(볼록 근사) /
    /// Capsule=사람·기둥처럼 둥근 것 / Composite=상자 여러 개(ㄱ자·오목한 구조물).</summary>
    public enum ColliderMode { None, Box, Mesh, Capsule, Composite }

    /// <summary>카탈로그 탭 분류. 2D판과 같은 뜻이되 Decal이 빠졌다 —
    /// 3D에선 바닥 얼룩을 별도 분류로 둘 이유가 없고 Prop으로 충분하다.</summary>
    public enum Category { Floor, Wall, Prop, Object, Cover }

    /// <summary>로컬 단위 상자 하나(Composite 모드).</summary>
    [System.Serializable]
    public struct ColliderBox
    {
        public Vector3 center;
        public Vector3 size;
        public ColliderBox(Vector3 c, Vector3 s) { center = c; size = s; }
    }

    [Header("Identity")]
    public string propId;
    public string displayName;
    [Tooltip("카탈로그 탭 분류. Cover=엄폐물 — 3D에서 새로 생긴 분류(높이로 몸을 가리는 것).")]
    public Category category = Category.Prop;
    [Tooltip("컬렉션/테마 태그(예: '전당포', '고철시장'). 정리용이며 런타임 동작에 영향 없음.")]
    public string group = "";

    [Header("Visual")]
    [Tooltip("표시할 메시. **비우면 size 크기의 상자로 대신 세운다**(그레이박스).")]
    public Mesh mesh;
    [Tooltip("머티리얼. 비우면 greyColor로 즉석 생성한 것을 쓴다.")]
    public Material material;
    [Tooltip("메시가 없을 때(그레이박스) 쓸 색.")]
    public Color greyColor = new(0.45f, 0.43f, 0.40f);

    [Header("Footprint (크기)")]
    [Tooltip("가로·높이·세로(m). 그레이박스 크기이자 Box 콜라이더 기본 크기. " +
             "메시가 있으면 표시 크기는 메시가 정하고 이 값은 콜라이더에만 쓰인다.")]
    public Vector3 size = new(1f, 1f, 1f);
    [Tooltip("바닥에서 띄울 높이(m). 천장 등 매달린 것에 쓴다. 0=바닥에 놓임.")]
    public float groundOffset = 0f;

    [Header("Collider (막힘)")]
    public ColliderMode colliderMode = ColliderMode.Box;
    [Tooltip("true면 통과 가능한 트리거(감지·이벤트용). false면 물리적으로 막는다.")]
    public bool isTrigger = false;
    [Tooltip("Box 모드 — size 대비 배율(1,1,1=크기 그대로).")]
    public Vector3 boxSizeScale = Vector3.one;
    [Tooltip("콜라이더 중심 오프셋(로컬 m).")]
    public Vector3 boxOffset = Vector3.zero;
    [Tooltip("Composite 모드 — 상자 여러 개.")]
    public List<ColliderBox> compositeBoxes = new();

    [Header("Cover (엄폐)")]
    [Tooltip("이 프롭 뒤에 숨을 수 있는가. 켜면 시야·사격 판정에서 엄폐물로 쓴다. " +
             "높이가 낮으면 앉아야 가려지는 반엄폐, 키를 넘으면 완전 엄폐.")]
    public bool isCover = false;

    [Header("Shadow")]
    [Tooltip("그림자를 드리우는가. 3D는 메시 렌더러가 직접 드리운다(2D의 ShadowCaster2D 불필요). " +
             "바닥 장식처럼 그림자가 어색한 것만 끈다.")]
    public bool castShadow = true;

    [Header("Breakable (파괴 가능)")]
    [Tooltip("켜면 Breakable 부착 — 때리면 단계별로 부서진다.")]
    public bool breakable = false;
    [Min(1)] public int breakStages = 3;
    [Min(0.01f)] public float breakHp = 24f;
    [Tooltip("true=파괴 시 제거, false=잔해로 남김(콜라이더 끔).")]
    public bool breakDestroy = true;
    [Tooltip("켜면 Health+Hurtbox 부착 → 근접 공격으로 직접 부술 수 있다.")]
    public bool breakHittable = true;

    [Header("Light (발광 — 램프·모닥불·네온)")]
    [Tooltip("켜면 Point 라이트를 붙인다.")]
    public bool emitsLight = false;
    public Color lightColor = new(1f, 0.88f, 0.68f);
    [Min(0f)] public float lightRange = 8f;
    [Min(0f)] public float lightIntensity = 1.6f;
    [Tooltip("라이트를 놓을 높이(m, 프롭 바닥 기준).")]
    public float lightHeight = 1.2f;

    /// <summary>메시가 없어 그레이박스로 서는가 — 카탈로그 화면에서 '모델 대기'로 보여준다.</summary>
    public bool IsGreybox => mesh == null;
}
