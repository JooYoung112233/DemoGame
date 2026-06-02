using UnityEngine;

/// <summary>
/// 플레이어 장비(무기) 관리. 장착 무기를 TopDownPlayer 전투에 반영.
/// 인벤토리에서 무기 우클릭 → EquipWeapon. 맨손 = Unequip.
/// 세이브: 장착 무기 itemId 영속화 (SaveManager).
/// </summary>
public class PlayerEquipment : MonoBehaviour
{
    TopDownPlayer player;

    /// <summary>현재 장착된 무기 아이템 (null=맨손)</summary>
    public ItemData EquippedWeapon { get; private set; }

    /// <summary>장착 변경 이벤트 (HUD 등 구독)</summary>
    public event System.Action<ItemData> OnWeaponChanged;

    void Awake()
    {
        player = GetComponent<TopDownPlayer>();
    }

    /// <summary>무기 장착. Weapon 카테고리만 허용. 같은 무기 재장착 시 해제(토글).</summary>
    public bool EquipWeapon(ItemData item)
    {
        if (item == null || item.category != ItemCategory.Weapon) return false;

        // 같은 무기 다시 누르면 해제
        if (EquippedWeapon == item) { Unequip(); return true; }

        EquippedWeapon = item;
        if (player != null) player.SetWeapon(item.weaponData);
        OnWeaponChanged?.Invoke(item);
        Debug.Log($"[Equip] 무기 장착: {item.displayName}");
        return true;
    }

    public void Unequip()
    {
        if (EquippedWeapon == null) return;
        EquippedWeapon = null;
        if (player != null) player.SetWeapon(null);
        OnWeaponChanged?.Invoke(null);
        Debug.Log("[Equip] 맨손");
    }

    // ── 세이브/로드 ──
    public string GetSaveData() => EquippedWeapon != null ? EquippedWeapon.itemId : "";

    public void LoadSaveData(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) { Unequip(); return; }
        var data = ItemDatabase.Get(itemId);
        if (data != null) EquipWeapon(data);
    }
}
