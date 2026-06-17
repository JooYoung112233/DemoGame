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
    /// Floor=바닥(보통 콜라이더 없음), Wall=벽(막힘), Prop=장식/오브젝트, Object=스폰/상호작용 마커,
    /// Decal=바닥/벽 위에 얹는 장식 오버레이(핏자국·그을음·균열·낙서 — 콜라이더·그림자 없음, 바닥보다 위에 그림).</summary>
    public enum Category { Floor, Wall, Prop, Object, Decal }

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
    [Tooltip("카탈로그 탭 분류(바닥/벽/프롭/오브젝트/데칼).")]
    public Category category = Category.Prop;
    [Tooltip("분류(컬렉션/테마) — 예: '전당포', '안전구역'. 같은 분류끼리 묶어 보고 필터링하는 정리용 태그. " +
             "카테고리를 가로질러 쓸 수 있음(전당포 테마에 바닥/벽/프롭/데칼 모두). 런타임 동작 영향 없음. 비우면 (미분류).")]
    public string group = "";

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

    [Header("Shadow (그림자)")]
    [Tooltip("켜면 ShadowCaster2D 부착 → Light2D가 실제 캐스트 그림자를 드리움(빛 반대편). 벽/프롭 권장. 방향·길이는 라이트가 결정.")]
    public bool castShadow = false;

    /// <summary>ShadowCaster2D Casting Option 미러(URP enum과 1:1).</summary>
    public enum ShadowCast { CastShadow, CastAndSelfShadow, SelfShadow, NoShadow }
    [Tooltip("Casting Option — CastShadow=드리우기만(기본·완전차단), CastAndSelfShadow=자기+드리우기, SelfShadow=자기만, NoShadow=그림자 없음(빛 완전 통과).")]
    public ShadowCast shadowCasting = ShadowCast.CastShadow;
    [Range(0f, 1f)]
    [Tooltip("Alpha Cutoff — 스프라이트 알파가 이 값보다 낮은 부분은 그림자를 안 만듦 = 그쪽으로 빛 통과. " +
             "창문/반투명 부분 빛 새게 하려면 값을 높여(예 0.5~0.8). 0.1=거의 다 막음(불투명만 통과 안 됨).")]
    public float shadowAlphaCutoff = 0.1f;

    [Header("Breakable (파괴 가능)")]
    [Tooltip("켜면 Breakable 부착 — 때리면 단계별로 부서지고 마지막 단계에 파괴. " +
             "손상 비주얼은 오버레이(BRB/DamageOverlay)라 베이스 셰이더와 무관.")]
    public bool breakable = false;
    [Tooltip("손상 단계 수. 마지막 단계 = 파괴(예: 3이면 1→2→3).")]
    [Min(1)] public int breakStages = 3;
    [Tooltip("총 내구도(데미지 합). '때려서 부수기' 시 타수 ≈ breakHp / 공격 데미지(약공 ~8). 예: 24면 약 3대.")]
    [Min(0.01f)] public float breakHp = 24f;
    [Tooltip("true=파괴 시 오브젝트 제거. false=잔해로 남김(콜라이더 끔).")]
    public bool breakDestroy = true;
    [Tooltip("breakDestroy=false일 때 남길 잔해 스프라이트(선택).")]
    public Sprite breakRubbleSprite;

    [Tooltip("켜면 Health+Hurtbox 자동 부착(Destructible 레이어) → 플레이어 근접 공격으로 직접 부숨. " +
             "끄면 비주얼·로직만(트랩 등에서 Breakable.Hit() 호출).")]
    public bool breakHittable = true;

    [Tooltip("손상 오버레이 세기.")]
    [Range(0f, 2f)] public float breakOverlayIntensity = 1f;
    [Tooltip("균열 색(어두움).")]
    public Color breakCrackColor = new(0.02f, 0.02f, 0.03f, 1f);
    [Tooltip("그을음/때 색.")]
    public Color breakGrimeColor = new(0.15f, 0.13f, 0.10f, 1f);

    [Tooltip("켜면 파괴 시 아이템 드랍.")]
    public bool breakDrop = false;
    [Tooltip("드랍 고정 아이템 ID(비우면 지역 루트만). Resources/Items에서 검색.")]
    public string breakDropItemId = "";
    [Min(1)] public int breakDropCount = 1;
    [Tooltip("켜면 지역 루트 테이블(Ground)로 추가 드랍.")]
    public bool breakDropRegionLoot = false;

    [Header("Aged / Weathering (낡음·녹·폐허, 이미지 0장)")]
    [Tooltip("켜면 Weathered 부착 — 부서지지 않아도 항상 녹·이끼·그을음으로 낡아 보이게(절차적). 베이스 셰이더 무관.")]
    public bool weathered = false;
    [Tooltip("낡음 정도(0=새것, 1=폐허).")]
    [Range(0f, 1f)] public float weatherAmount = 0.5f;
    [Tooltip("풍화 색 — 녹=주황갈, 이끼=초록, 그을음=검정, 물때=갈색.")]
    public Color weatherTint = new(0.32f, 0.22f, 0.12f, 1f);

    [Header("Light (발광 — 램프·창문·네온 등)")]
    [Tooltip("켜면 Light2D 자식 부착 → 이 프롭이 실제로 주변을 비춤(텍스쳐 쿠키). " +
             "아트에 베이크된 발광과 별개의 동적 조명(그림자도 드리움).")]
    public bool emitsLight = false;

    /// <summary>발광 쿠키 모양(LightCookieGenerator 산출물).</summary>
    public enum LightShape { Radial, Cone }
    [Tooltip("Radial=원형 풀(전구·램프·창문), Cone=부채꼴(스탠드·스포트).")]
    public LightShape lightShape = LightShape.Radial;
    [Tooltip("빛 색(따뜻한 노랑 등). 흰 쿠키를 이 색으로 틴트.")]
    public Color lightColor = new(1f, 0.85f, 0.55f, 1f);
    [Tooltip("빛 세기(HDR — 1 이상이면 bloom 잘 걸림).")]
    [Min(0f)] public float lightIntensity = 1.3f;
    [Tooltip("빛 크기(월드 단위 — Radial=반경, Cone=길이 대략).")]
    [Min(0.1f)] public float lightRadius = 2.5f;
    [Tooltip("빛 원점 오프셋(로컬). 램프 head 등 발광부 위치로.")]
    public Vector2 lightOffset = Vector2.zero;
    [Tooltip("Cone일 때 빛 방향(도). +X=0, 위=90, 아래=-90.")]
    public float lightAngle = -90f;
    [Tooltip("켜면 ShadowCaster2D가 있는 벽/프롭 너머를 가림(그림자 드리움).")]
    public bool lightCastsShadows = true;
    [Tooltip("켜면 밤에만 발광(낮엔 꺼짐, DayNightCycle 연동).")]
    public bool lightNightOnly = false;

    // ── 기능(Function) — 오브젝트(스폰/탈출/지도판)·수색 가능 프롭 등 ──
    /// <summary>배치 시 부여할 게임플레이 기능. None=장식/막힘만.</summary>
    public enum Function { None, SpawnPoint, Interactable, LootContainer, ItemDrop, NPC, Door, Trigger }

    [Header("Function (기능)")]
    [Tooltip("배치 시 자동 부여할 기능. None=장식/막힘. SpawnPoint=플레이어 스폰. " +
             "Interactable=상호작용(탈출/지도판/침대 등 종류 선택). LootContainer=수색 가능 컨테이너.")]
    public Function function = Function.None;
    [Tooltip("Interactable 종류 — 탈출(ExitPoint)/지도판(MapBoard)/침대/작업대/NPC/문/범용(Generic) 등.")]
    public InteractableObject.InteractType interactType = InteractableObject.InteractType.Generic;
    [Tooltip("상호작용 프롬프트(비우면 종류 이름).")]
    public string functionPrompt = "";
    [Tooltip("상호작용 인식 범위(m).")]
    public float interactRange = 1.5f;
    [Tooltip("SpawnPoint면 pointId(씬 전환 도착 지점 식별). 비우면 default.")]
    public string spawnPointId = "default";
    [Tooltip("LootContainer 격자 가로/세로.")]
    public int lootGridWidth = 4;
    public int lootGridHeight = 5;
    [Tooltip("LootContainer가 지역 루트 테이블을 쓸지.")]
    public bool lootUseRegionLoot = true;

    [Tooltip("투명 마커 — SpriteRenderer 없이 기능만(스폰/트리거 등). 씬에선 기즈모로 표시.")]
    public bool noVisual = false;

    [Header("ItemDrop (바닥 아이템)")]
    [Tooltip("바닥에 스폰할 고정 아이템 ID. 비우면 지역 루트(Ground)로 스폰.")]
    public string itemId = "";
    [Tooltip("고정 아이템 수량.")]
    public int itemCount = 1;

    [Header("NPC")]
    [Tooltip("NPCData id (Resources/Data/NPC/{id}). 비우면 빈 NPC.")]
    public string npcId = "";

    [Header("Door")]
    public DoorController.LockType doorLockType = DoorController.LockType.None;
    [Tooltip("Key 잠금일 때 필요 열쇠 아이템 ID.")]
    public string doorKeyId = "";
    [Tooltip("Quest 잠금일 때 필요 퀘스트 ID.")]
    public string doorQuestId = "";

    [Header("Trigger (영역 → 씬 전환)")]
    [Tooltip("전환할 씬 이름.")]
    public string triggerTargetScene = "";
    [Tooltip("도착 씬 SpawnPoint ID.")]
    public string triggerTargetSpawnId = "";
    [Tooltip("트리거 영역 크기(월드 단위).")]
    public Vector2 triggerSize = new(2f, 2f);
    [Tooltip("전환 영역 중심 오프셋(월드 단위). 입구가 아래쪽(80° 틸트 밑둥)이면 Y를 음수로 내려 입구에 맞춤.")]
    public Vector2 triggerOffset = Vector2.zero;
    [Tooltip("들어오는 즉시 전환할지.")]
    public bool triggerAutoEnter = true;
}
