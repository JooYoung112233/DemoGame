using UnityEngine;
using UnityEngine.Serialization;   // FormerlySerializedAs — 필드 이름을 바꿔도 기존 에셋 값 보존

/// <summary>
/// 아이템 카테고리.
/// </summary>
public enum ItemCategory
{
    Weapon,      // 무기
    Medical,     // 의료 (붕대, 부목, 진통제, 구급상자)
    Consumable,  // 소비 (음식, 음료, 건전지)
    Material,    // 재료 (고철, 나사, 천 조각)
    Valuable,    // 귀중품 (전당포 판매용)
    Key,         // 열쇠
    Misc,        // 기타
}

public enum EquipSlot
{
    None = 0,
    Head,
    Armor,
    Rig,
    Backpack,
    PrimaryWeapon,
    SecondaryWeapon,
    Melee,
    Special,   // 시계/측정기 등 특수 장비 (좌측 특수창) ※ 끝에 추가(직렬화 인덱스 보존)
}

/// <summary>
/// 무기 부착물(파츠) 종류. 무기 1정당 종류별 1개 부착.
/// </summary>
public enum WeaponPartType
{
    None = 0,
    Scope,      // 조준경 — 사거리/명중
    Muzzle,     // 소염기 — 반동/소음
    Magazine,   // 탄창 — 장탄수(총기)
    Grip,       // 손잡이 — 핸들링(이속/스태미너/흔들림)
}

/// <summary>
/// 아이템 희귀도.
/// </summary>
public enum ItemRarity
{
    Common,    // 흰색
    Uncommon,  // 녹색
    Rare,      // 파랑
    Epic,      // 보라
    Legendary, // 금색
}

/// <summary>
/// 아이템 사용 시 효과 타입.
/// </summary>
public enum ItemUseEffect
{
    None,
    HealHP,          // HP 회복
    HealInjury,      // [폐기 2026-09-09] 부위별 치료 제거 → HealHP로 흡수. enum 인덱스 보존용 예약 슬롯
    RestoreStamina,  // [미사용/deprecated] 스태미너 회복 — 2026-05-30 제거. enum 인덱스 보존용으로만 유지(삭제 시 AddBattery/Food SO 깨짐)
    AddBattery,      // 배터리 충전
    Food,            // 포만감 (향후)
}

/// <summary>
/// 아이템 정의 ScriptableObject.
/// 모든 아이템의 원본 데이터. 인스턴스는 ItemInstance로 생성.
/// Assets/Data/Items/ 에 SO 파일로 관리.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Dev Tools/Item/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("고유 ID (코드 참조용, 예: bandage, pipe_weapon)")]
    public string itemId;

    [Tooltip("표시 이름")]
    public string displayName;

    [Tooltip("아이템 설명")]
    [TextArea(2, 5)]
    public string description;

    [Tooltip("인벤토리 아이콘 (격자 크기에 맞게)")]
    public Sprite icon;

    [Header("격자 크기")]
    [Tooltip("인벤토리에서 차지하는 가로 칸 수")]
    [Range(1, 3)]
    public int gridWidth = 1;

    [Tooltip("인벤토리에서 차지하는 세로 칸 수")]
    [Range(1, 3)]
    public int gridHeight = 1;

    [Header("분류")]
    public ItemCategory category;
    public ItemRarity rarity;

    [Header("지역 (월드맵)")]
    [Tooltip("전용 지역 ID (WorldRegionCatalog.regionId). 비우면 공용.")]
    public string primaryRegionId;

    [Header("스택")]
    [Tooltip("최대 스택 수 (1 = 스택 불가)")]
    [Min(1)]
    public int maxStack = 1;

    [Header("무게")]
    [Tooltip("무게 (kg)")]
    [Min(0)]
    public float weight = 0.1f;

    [Header("가격 (스크랩 기준)")]
    [Tooltip("전당포 판매가 (0이면 판매 불가)")]
    [Min(0)]
    public int sellPrice;

    [Tooltip("상점 구매가 (0이면 구매 불가)")]
    [Min(0)]
    public int buyPrice;

    [Header("사용")]
    [Tooltip("우클릭으로 즉시 사용 가능 여부")]
    public bool isUsable;

    [Tooltip("사용 시 효과 타입")]
    public ItemUseEffect useEffect;

    [Tooltip("효과 수치 (HP 회복량, 배터리 충전량 등)")]
    public float effectValue;

    [Tooltip("사용에 걸리는 시간(초). 0이면 즉시. 예: 붕대 5초. 진행 중 ESC로 취소.")]
    [Min(0)]
    public float useTimeSeconds;

    [Header("투척물")]
    [Tooltip("true면 던질 수 있는 투척물(G키 조준→착탄 소음으로 적 유인). 1차=돌. 사거리/소음반경은 GameTuning.")]
    public bool isThrowable;

    [Header("내구도 (구급상자 등 다회 사용 아이템)")]
    [Tooltip("true면 스택 대신 내구도 소모 방식")]
    public bool hasDurability;

    [Tooltip("최대 내구도 (예: 300)")]
    [Min(1)]
    public float maxDurability = 100f;

    [Tooltip("1회 사용 시 내구도 소모량 (예: 50)")]
    [Min(1)]
    public float durabilityCostPerUse = 50f;

    [Header("장비 슬롯")]
    [Tooltip("장착 가능 슬롯 (None이면 장착 불가)")]
    public EquipSlot equipSlot;

    [Tooltip("가방/조끼 장착 시 제공하는 격자 가로 칸")]
    [Min(0)]
    public int containerWidth;

    [Tooltip("가방/조끼 장착 시 제공하는 격자 세로 칸")]
    [Min(0)]
    public int containerHeight;

    [Header("보관함 (컨테이너 아이템 — 타르코프 케이스)")]
    [Tooltip("true면 자기만의 내부 격자를 가진 보관함(창고에 놓고 열어 보관). 외부 footprint=gridWidth/Height")]
    public bool isContainer;
    [Tooltip("내부 격자 가로 칸 (예: 냉장고 8)")]
    [Min(0)]
    public int internalWidth;
    [Tooltip("내부 격자 세로 칸 (예: 냉장고 7)")]
    [Min(0)]
    public int internalHeight;
    [Tooltip("내부에 넣을 수 있는 카테고리(빈 배열=전체=범용 상자). 불일치=배치 불가")]
    public ItemCategory[] allowedCategories;

    /// <summary>이 아이템이 보관함(내부 격자 보유)인지. 명시 보관함 + 가방/조끼(containerWidth>0)도 포함.</summary>
    public bool IsContainer =>
        (isContainer && internalWidth > 0 && internalHeight > 0)
        || (containerWidth > 0 && containerHeight > 0);

    /// <summary>내부 격자 가로(internalWidth 우선, 없으면 가방 containerWidth).</summary>
    public int ContainerGridWidth => internalWidth > 0 ? internalWidth : containerWidth;
    /// <summary>내부 격자 세로(internalHeight 우선, 없으면 가방 containerHeight).</summary>
    public int ContainerGridHeight => internalHeight > 0 ? internalHeight : containerHeight;

    /// <summary>해당 카테고리를 이 보관함에 넣을 수 있는지(빈 allowedCategories=범용).</summary>
    public bool AcceptsCategory(ItemCategory c)
    {
        if (allowedCategories == null || allowedCategories.Length == 0) return true;
        for (int i = 0; i < allowedCategories.Length; i++)
            if (allowedCategories[i] == c) return true;
        return false;
    }

    /// <summary>허용 카테고리 요약 문자열(범용이면 "전체").</summary>
    public string AllowedCategorySummary
    {
        get
        {
            if (allowedCategories == null || allowedCategories.Length == 0) return "전체";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < allowedCategories.Length; i++)
            {
                if (i > 0) sb.Append('/');
                sb.Append(CategoryKo(allowedCategories[i]));
            }
            return sb.ToString();
        }
    }

    static string CategoryKo(ItemCategory c)
    {
        switch (c)
        {
            case ItemCategory.Weapon:     return "무기";
            case ItemCategory.Medical:    return "의료";
            case ItemCategory.Consumable: return "소비";
            case ItemCategory.Material:   return "재료";
            case ItemCategory.Valuable:   return "귀중품";
            case ItemCategory.Key:        return "열쇠";
            default:                      return "기타";
        }
    }

    [Header("무기 연동 (Weapon 전용)")]
    [Tooltip("무기 전투 데이터 SO (Weapon 카테고리일 때). 장착 시 콤보·스탯 교체")]
    public WeaponData weaponData;

    [Header("무기 파츠 (부착물 — Weapon Part)")]
    [Tooltip("이 아이템이 무기 부착물이면 종류(None=부착물 아님). 조준경/소염기/탄창/손잡이")]
    public WeaponPartType weaponPartType;
    [Tooltip("부착 시 이동속도 배율 보정(곱연산, 1=영향없음). 손잡이/경량화 등")]
    public float partMoveSpeedMult = 1f;
    [Tooltip("부착 시 스태미너 소모 배율 보정(곱연산, 1=영향없음). 손잡이 등")]
    public float partStaminaMult = 1f;
    [Tooltip("부착 시 유효사거리 가산(m). 조준경.")]
    public float partRangeBonus = 0f;
    [Tooltip("부착 시 반동/탄퍼짐 배율(곱연산, <1=감소). 소염기/손잡이.")]
    public float partRecoilMult = 1f;

    // 2026-07-29: 구 `partMagBonus`(장탄수 '가산') → `magCapacity`(탄창 자체의 '용량').
    //   탄창을 아이템으로 만들기로 하면서(타르코프식) 탄창이 곧 장탄수의 주인이 됐다.
    //   [FormerlySerializedAs]로 기존 에셋 값(mag_extended=10)이 그대로 넘어온다 —
    //   이름만 바꾸고 어트리뷰트를 안 달면 **조용히 0이 되어 장탄 0짜리 탄창**이 된다.
    [Header("탄창 / 탄약 (총기)")]
    [FormerlySerializedAs("partMagBonus")]
    [Tooltip("탄창 용량(발). weaponPartType=Magazine일 때만 의미 있다.")]
    public int magCapacity = 0;
    [Tooltip("탄창이 받는 구경(예: 9x19). 총기의 caliber와 같아야 장착된다. 비우면 아무거나.")]
    public string magCaliber;
    [Tooltip("이 아이템이 **탄약**이면 구경(예: 9x19). 비어 있으면 탄약이 아니다.")]
    public string ammoCaliber;
    [Tooltip("탄종 데미지 배율(곱연산). 철갑/저위력탄 등.")]
    public float ammoDamageMult = 1f;

    /// <summary>무기 부착물 여부</summary>
    public bool IsWeaponPart => weaponPartType != WeaponPartType.None;

    /// <summary>탄창인지(용량이 있어야 쓸 수 있는 탄창이다).</summary>
    public bool IsMagazine => weaponPartType == WeaponPartType.Magazine && magCapacity > 0;

    /// <summary>탄약 아이템인지.</summary>
    public bool IsAmmo => !string.IsNullOrEmpty(ammoCaliber);

    [Header("바닥 드롭")]
    [Tooltip("월드에 떨어졌을 때 사용할 프리팹 (없으면 기본 큐브)")]
    public GameObject worldDropPrefab;

    /// <summary>희귀도에 해당하는 색상</summary>
    public Color RarityColor
    {
        get
        {
            switch (rarity)
            {
                case ItemRarity.Common:    return Color.white;
                case ItemRarity.Uncommon:  return new Color(0.3f, 0.9f, 0.3f);
                case ItemRarity.Rare:      return new Color(0.3f, 0.5f, 1f);
                case ItemRarity.Epic:      return new Color(0.7f, 0.3f, 1f);
                case ItemRarity.Legendary: return new Color(1f, 0.85f, 0.2f);
                default: return Color.white;
            }
        }
    }
}
