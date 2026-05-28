using UnityEngine;

/// <summary>
/// 안전가옥 가구 데이터 (ScriptableObject).
/// 각 가구는 카테고리별 전용 격자 창고.
/// 예: 냉장고(음식), 무기거치대(무기), 일반상자(범용).
/// </summary>
[CreateAssetMenu(fileName = "NewFurniture", menuName = "Dev Tools/Item/Furniture Data")]
public class FurnitureData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("고유 ID (예: box_basic, fridge, weapon_rack)")]
    public string furnitureId;

    [Tooltip("UI 표시명")]
    public string displayName;

    [TextArea(2, 4)]
    [Tooltip("설명")]
    public string description;

    [Tooltip("가구 아이콘 (상점 UI용)")]
    public Sprite icon;

    [Tooltip("가구 스프라이트 (월드 배치용)")]
    public Sprite worldSprite;

    [Header("격자 설정")]
    [Tooltip("격자 가로 칸 수")]
    public int gridWidth = 4;
    [Tooltip("격자 세로 칸 수")]
    public int gridHeight = 4;

    [Header("카테고리 필터")]
    [Tooltip("이 가구에 보관 가능한 아이템 카테고리. 비어있으면 전체 허용 (범용)")]
    public ItemCategory[] allowedCategories;

    [Header("구매 비용")]
    [Tooltip("루디 가격")]
    public int buyPriceRudy;
    [Tooltip("필요 재료")]
    public MaterialCost[] materialCost;

    [System.Serializable]
    public struct MaterialCost
    {
        [Tooltip("ItemDatabase 아이템 ID")]
        public string itemId;
        [Tooltip("필요 수량")]
        public int count;
    }

    /// <summary>범용 가구인지 (카테고리 제한 없음)</summary>
    public bool IsUniversal => allowedCategories == null || allowedCategories.Length == 0;

    /// <summary>해당 카테고리의 아이템을 수용하는지</summary>
    public bool AcceptsCategory(ItemCategory category)
    {
        if (IsUniversal) return true;
        for (int i = 0; i < allowedCategories.Length; i++)
            if (allowedCategories[i] == category) return true;
        return false;
    }

    /// <summary>해당 아이템을 수용하는지</summary>
    public bool AcceptsItem(ItemInstance item)
    {
        if (item == null || item.data == null) return false;
        return AcceptsCategory(item.data.category);
    }

    /// <summary>허용 카테고리 요약 문자열</summary>
    public string AllowedCategorySummary
    {
        get
        {
            if (IsUniversal) return "전체";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < allowedCategories.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(CategoryName(allowedCategories[i]));
            }
            return sb.ToString();
        }
    }

    static string CategoryName(ItemCategory cat)
    {
        switch (cat)
        {
            case ItemCategory.Weapon: return "무기";
            case ItemCategory.Medical: return "의료";
            case ItemCategory.Consumable: return "소비";
            case ItemCategory.Material: return "재료";
            case ItemCategory.Valuable: return "귀중품";
            case ItemCategory.Key: return "열쇠";
            case ItemCategory.Misc: return "기타";
            default: return cat.ToString();
        }
    }
}
