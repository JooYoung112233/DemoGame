using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 장비 관리 (타르코프식 슬롯).
/// 7 슬롯: Head/Armor/Rig/Backpack/PrimaryWeapon/SecondaryWeapon/Melee.
/// 가방 장착 → PlayerInventory 격자 크기 변경.
/// 장비 무게 → PlayerInventory 총 무게에 합산.
/// </summary>
public class PlayerEquipment : MonoBehaviour
{
    TopDownPlayer player;
    PlayerInventory inventory;

    Dictionary<EquipSlot, ItemData> slots = new Dictionary<EquipSlot, ItemData>();

    /// <summary>장비 변경 이벤트 (UI 구독)</summary>
    public event System.Action<EquipSlot, ItemData> OnEquipChanged;

    /// <summary>현재 장착된 무기 (PrimaryWeapon 슬롯, 하위 호환)</summary>
    public ItemData EquippedWeapon => GetSlot(EquipSlot.PrimaryWeapon);

    public ItemData GetSlot(EquipSlot slot)
    {
        ItemData d;
        return slots.TryGetValue(slot, out d) ? d : null;
    }

    public IEnumerable<KeyValuePair<EquipSlot, ItemData>> GetAllEquipped() => slots;

    /// <summary>장비 총 무게 (kg)</summary>
    public float TotalEquipWeight
    {
        get
        {
            float w = 0f;
            foreach (var kv in slots)
                if (kv.Value != null) w += kv.Value.weight;
            return w;
        }
    }

    void Awake()
    {
        player = GetComponent<TopDownPlayer>();
        inventory = GetComponent<PlayerInventory>();
    }

    /// <summary>아이템을 해당 슬롯에 장착. 같은 슬롯 재장착 = 해제(토글).</summary>
    public bool Equip(ItemData item)
    {
        if (item == null || item.equipSlot == EquipSlot.None) return false;
        var slot = item.equipSlot;

        // 같은 아이템 다시 → 해제
        if (GetSlot(slot) == item) { Unequip(slot); return true; }

        // 기존 장착 해제
        if (slots.ContainsKey(slot)) Unequip(slot);

        slots[slot] = item;

        // 무기 → 전투 시스템 반영
        if (slot == EquipSlot.PrimaryWeapon && player != null)
            player.SetWeapon(item.weaponData);

        // 가방 → 인벤토리 크기 변경
        if (slot == EquipSlot.Backpack && inventory != null)
            inventory.OnBackpackChanged(item);

        OnEquipChanged?.Invoke(slot, item);
        Debug.Log($"[Equip] {slot} 장착: {item.displayName}");
        return true;
    }

    /// <summary>하위 호환: 무기 장착 API</summary>
    public bool EquipWeapon(ItemData item)
    {
        if (item == null || item.category != ItemCategory.Weapon) return false;
        // equipSlot이 None인 무기도 PrimaryWeapon으로 취급
        if (item.equipSlot == EquipSlot.None)
        {
            if (GetSlot(EquipSlot.PrimaryWeapon) == item) { Unequip(EquipSlot.PrimaryWeapon); return true; }
            if (slots.ContainsKey(EquipSlot.PrimaryWeapon)) Unequip(EquipSlot.PrimaryWeapon);
            slots[EquipSlot.PrimaryWeapon] = item;
            if (player != null) player.SetWeapon(item.weaponData);
            OnEquipChanged?.Invoke(EquipSlot.PrimaryWeapon, item);
            Debug.Log($"[Equip] 무기 장착: {item.displayName}");
            return true;
        }
        return Equip(item);
    }

    public void Unequip(EquipSlot slot)
    {
        ItemData prev;
        if (!slots.TryGetValue(slot, out prev)) return;
        slots.Remove(slot);

        if (slot == EquipSlot.PrimaryWeapon && player != null)
            player.SetWeapon(null);

        if (slot == EquipSlot.Backpack && inventory != null)
            inventory.OnBackpackChanged(null);

        OnEquipChanged?.Invoke(slot, null);
        Debug.Log($"[Equip] {slot} 해제" + (prev != null ? $": {prev.displayName}" : ""));
    }

    [System.Obsolete("Use Unequip(EquipSlot) instead")]
    public void Unequip()
    {
        Unequip(EquipSlot.PrimaryWeapon);
    }

    // ── 세이브/로드 ──
    public string GetSaveData()
    {
        var parts = new List<string>();
        foreach (var kv in slots)
            if (kv.Value != null)
                parts.Add($"{(int)kv.Key}:{kv.Value.itemId}");
        return string.Join("|", parts);
    }

    public void LoadSaveData(string data)
    {
        slots.Clear();
        if (string.IsNullOrEmpty(data))
        {
            if (player != null) player.SetWeapon(null);
            if (inventory != null) inventory.OnBackpackChanged(null);
            return;
        }

        // 하위 호환: 구 포맷(itemId만) → PrimaryWeapon
        if (!data.Contains(":"))
        {
            var item = ItemDatabase.Get(data);
            if (item != null) EquipWeapon(item);
            return;
        }

        foreach (var part in data.Split('|'))
        {
            var kv = part.Split(':');
            if (kv.Length != 2) continue;
            int slotInt;
            if (!int.TryParse(kv[0], out slotInt)) continue;
            var item = ItemDatabase.Get(kv[1]);
            if (item != null)
            {
                var slot = (EquipSlot)slotInt;
                slots[slot] = item;
                if (slot == EquipSlot.PrimaryWeapon && player != null)
                    player.SetWeapon(item.weaponData);
            }
        }

        // 가방 로드 후 인벤 크기 반영
        if (inventory != null)
            inventory.OnBackpackChanged(GetSlot(EquipSlot.Backpack));
    }
}
