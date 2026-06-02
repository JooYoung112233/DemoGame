using UnityEngine;

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
    HealInjury,      // 부상 치료 (MedicalItemData 연동)
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

    [Header("가격 (루디 기준)")]
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

    [Header("내구도 (구급상자 등 다회 사용 아이템)")]
    [Tooltip("true면 스택 대신 내구도 소모 방식")]
    public bool hasDurability;

    [Tooltip("최대 내구도 (예: 300)")]
    [Min(1)]
    public float maxDurability = 100f;

    [Tooltip("1회 사용 시 내구도 소모량 (예: 50)")]
    [Min(1)]
    public float durabilityCostPerUse = 50f;

    [Header("의료 연동 (HealInjury 전용)")]
    [Tooltip("치료 아이템 SO (Medical 카테고리일 때)")]
    public MedicalItemData medicalData;

    [Header("무기 연동 (Weapon 전용)")]
    [Tooltip("무기 전투 데이터 SO (Weapon 카테고리일 때). 장착 시 콤보·스탯 교체")]
    public WeaponData weaponData;

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
