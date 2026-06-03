using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점(전당포 등) 데이터. NPC가 참조.
/// 플레이어는 stock에서 사고(buyPrice×buyRate), 가진 아이템을 팔 수 있다(sellPrice×sellRate).
/// 전당포는 sellRate를 후려쳐 차익을 둔다.
/// </summary>
[CreateAssetMenu(menuName = "BRB/Shop Data", fileName = "Shop_")]
public class ShopData : ScriptableObject
{
    public string shopId = "pawnshop";
    public string shopName = "전당포";

    [Tooltip("이 상점에서 살 수 있는 아이템 목록")]
    public List<ItemData> stock = new List<ItemData>();

    [Header("가격 배율")]
    [Tooltip("구매가 배율 (ItemData.buyPrice 기준)")]
    [Range(0.5f, 2f)] public float buyRate = 1f;
    [Tooltip("판매가 배율 (ItemData.sellPrice 기준). 전당포는 후려침")]
    [Range(0.1f, 1f)] public float sellRate = 0.6f;

    /// <summary>플레이어가 이 아이템을 살 때 가격 (0이면 구매 불가).</summary>
    public int BuyPrice(ItemData item)
    {
        if (item == null || item.buyPrice <= 0) return 0;
        return Mathf.Max(1, Mathf.RoundToInt(item.buyPrice * buyRate));
    }

    /// <summary>플레이어가 이 아이템을 팔 때 받는 가격 (0이면 판매 불가).</summary>
    public int SellPrice(ItemData item)
    {
        if (item == null || item.sellPrice <= 0) return 0;
        return Mathf.Max(1, Mathf.RoundToInt(item.sellPrice * sellRate));
    }
}
