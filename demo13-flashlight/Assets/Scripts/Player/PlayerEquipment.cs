using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 장비 관리 (타르코프식 슬롯).
/// 8 슬롯: Head/Armor/Rig/Backpack/PrimaryWeapon/SecondaryWeapon/Melee/Special(특수창: 시계·측정기 등).
/// 가방 장착 → PlayerInventory 격자 크기 변경.
/// 장비 무게 → PlayerInventory 총 무게에 합산.
/// </summary>
public class PlayerEquipment : MonoBehaviour
{
    TopDownPlayer player;
    PlayerInventory inventory;

    Dictionary<EquipSlot, ItemData> slots = new Dictionary<EquipSlot, ItemData>();
    // 슬롯별 실제 인스턴스(무기 부착물 등 인스턴스 상태 보존용). 장착 직후 SetSlotInstance로 기록.
    Dictionary<EquipSlot, ItemInstance> slotInstances = new Dictionary<EquipSlot, ItemInstance>();

    /// <summary>장비 변경 이벤트 (UI 구독)</summary>
    public event System.Action<EquipSlot, ItemData> OnEquipChanged;

    /// <summary>슬롯에 장착된 실제 인스턴스(부착물 포함). 데이터만으로 장착됐으면 null.</summary>
    public ItemInstance GetSlotInstance(EquipSlot slot)
    {
        ItemInstance inst;
        return slotInstances.TryGetValue(slot, out inst) ? inst : null;
    }

    /// <summary>장착 직후 실제 인스턴스를 슬롯에 연결(부착물 보존). 데이터 불일치면 무시.</summary>
    public void SetSlotInstance(EquipSlot slot, ItemInstance inst)
    {
        if (inst != null && GetSlot(slot) == inst.data) slotInstances[slot] = inst;
    }

    // ── 세이브: 장착 주무기 부착물 ────────────────────────────────────
    /// <summary>장착 주무기의 부착물 itemId[4] (없으면 null). SaveManager용.</summary>
    public string[] GetEquippedWeaponAttachments()
    {
        var inst = GetSlotInstance(EquipSlot.PrimaryWeapon);
        return (inst != null && inst.HasAnyAttachment) ? inst.attachments : null;
    }

    /// <summary>로드 후 장착 주무기에 부착물 복원(인스턴스 없으면 생성).</summary>
    public void SetEquippedWeaponAttachments(string[] att)
    {
        if (att == null || att.Length == 0) return;   // JsonUtility는 null→빈배열 → 길이로 가드
        var data = GetSlot(EquipSlot.PrimaryWeapon);
        if (data == null) return;
        var inst = GetSlotInstance(EquipSlot.PrimaryWeapon);
        if (inst == null) { inst = new ItemInstance(data); slotInstances[EquipSlot.PrimaryWeapon] = inst; }
        inst.attachments = (string[])att.Clone();
    }

    // ── 무기 부착물 집계 보정 (장착 주무기 기준) ──────────────────────
    public float WeaponPartMoveMult    => PartMult(EquipSlot.PrimaryWeapon, p => p.partMoveSpeedMult);
    public float WeaponPartStaminaMult => PartMult(EquipSlot.PrimaryWeapon, p => p.partStaminaMult);
    // 총기용(2026-07-29). 손잡이·소염기 = 반동↓, 조준경 = 유효사거리+.
    // (소염기의 '총성 반경↓'는 소음 시스템 폐기 2026-09-09로 사라졌다.)
    public float WeaponPartRecoilMult  => PartMult(EquipSlot.PrimaryWeapon, p => p.partRecoilMult);

    /// <summary>부착물 사거리 가산 합(곱이 아니라 합이다 — 조준경은 더해 준다).</summary>
    public float WeaponPartRangeBonus
    {
        get
        {
            var inst = GetSlotInstance(EquipSlot.PrimaryWeapon);
            if (inst == null || inst.attachments == null) return 0f;
            float sum = 0f;
            for (int i = 0; i < inst.attachments.Length; i++)
            {
                if (string.IsNullOrEmpty(inst.attachments[i])) continue;
                var d = ItemDatabase.Get(inst.attachments[i]);
                if (d != null) sum += d.partRangeBonus;
            }
            return sum;
        }
    }

    float PartMult(EquipSlot slot, System.Func<ItemData, float> sel)
    {
        var inst = GetSlotInstance(slot);
        if (inst == null || inst.attachments == null) return 1f;
        float m = 1f;
        for (int i = 0; i < inst.attachments.Length; i++)
        {
            if (string.IsNullOrEmpty(inst.attachments[i])) continue;
            var d = ItemDatabase.Get(inst.attachments[i]);
            if (d != null) m *= sel(d);
        }
        return m;
    }

    /// <summary>현재 장착된 무기 (PrimaryWeapon 슬롯, 하위 호환)</summary>
    public ItemData EquippedWeapon => GetSlot(EquipSlot.PrimaryWeapon);

    public ItemData GetSlot(EquipSlot slot)
    {
        ItemData d;
        return slots.TryGetValue(slot, out d) ? d : null;
    }

    public IEnumerable<KeyValuePair<EquipSlot, ItemData>> GetAllEquipped() => slots;

    public static bool CanEquipWeapon(ItemData item, EquipSlot slot)
        => item != null && item.category == ItemCategory.Weapon
        && (slot == EquipSlot.PrimaryWeapon || slot == EquipSlot.SecondaryWeapon
            || (slot == EquipSlot.Melee && (item.weaponData == null || !item.weaponData.isRanged)));

    void PutWeapon(EquipSlot slot, ItemInstance item)
    {
        slots.Remove(slot);
        slotInstances.Remove(slot);
        if (item != null) { slots[slot] = item.data; slotInstances[slot] = item; }
        if (slot == EquipSlot.PrimaryWeapon && player != null) player.SetWeapon(item?.data.weaponData);
        OnEquipChanged?.Invoke(slot, item?.data);
    }

    /// <summary>무기는 가방/창고 또는 장비 중 한 곳만 소유한다. 교체품은 출발 칸으로 돌아간다.</summary>
    public bool TryEquipWeaponFromGrid(ItemInstance item, InventoryGrid source, EquipSlot slot = EquipSlot.PrimaryWeapon)
    {
        if (!CanEquipWeapon(item?.data, slot) || source == null) return false;
        InventoryGrid.PlacedItem placed = null;
        foreach (var p in source.GetAll()) if (p.item == item) { placed = p; break; }
        if (placed == null) return false;
        var previous = GetSlotInstance(slot) ?? (GetSlot(slot) != null ? new ItemInstance(GetSlot(slot)) : null);
        source.Remove(placed);
        if (previous != null && !source.TryPlace(previous, placed.gridX, placed.gridY))
        {
            source.TryPlace(item, placed.gridX, placed.gridY);
            return false;
        }
        PutWeapon(slot, item);
        return true;
    }

    public bool TryStoreWeapon(EquipSlot slot, InventoryGrid destination)
    {
        var data = GetSlot(slot);
        if (data == null || data.category != ItemCategory.Weapon || destination == null) return false;
        var item = GetSlotInstance(slot) ?? new ItemInstance(data);
        if (!destination.TryAutoPlace(item)) return false;
        PutWeapon(slot, null);
        return true;
    }

    /// <summary>보조/근접 슬롯의 무기를 손으로 꺼내고, 기존 손 무기를 그 슬롯으로 교환한다.</summary>
    public bool TryDrawWeapon(EquipSlot from)
    {
        if (from == EquipSlot.PrimaryWeapon) return false;
        var data = GetSlot(from);
        if (!CanEquipWeapon(data, EquipSlot.PrimaryWeapon)) return false;
        var held = GetSlotInstance(EquipSlot.PrimaryWeapon)
            ?? (EquippedWeapon != null ? new ItemInstance(EquippedWeapon) : null);
        var next = GetSlotInstance(from) ?? new ItemInstance(data);
        // 근접 칸에 총을 넣을 수 없으므로 손의 총은 휴대 격자로 회수한다.
        if (held != null && !CanEquipWeapon(held.data, from))
        {
            if (inventory == null || !inventory.TryAutoPlaceAnywhere(held)) return false;
            held = null;
        }
        PutWeapon(from, held);
        PutWeapon(EquipSlot.PrimaryWeapon, next);
        return true;
    }

    [System.Serializable]
    public class WeaponState
    {
        public EquipSlot slot;
        public List<GridItemEntry> items;
    }

    public List<WeaponState> GetWeaponStates()
    {
        var result = new List<WeaponState>();
        foreach (var kv in slots)
        {
            if (kv.Value == null || kv.Value.category != ItemCategory.Weapon) continue;
            var grid = new InventoryGrid(1, 1);
            grid.TryAutoPlace(GetSlotInstance(kv.Key) ?? new ItemInstance(kv.Value));
            result.Add(new WeaponState { slot = kv.Key, items = grid.GetSaveData() });
        }
        return result;
    }

    public void RestoreWeaponStates(List<WeaponState> saved)
    {
        if (saved == null) return; // 구 세이브는 기존 장비/부착물 로드 유지
        foreach (var state in saved)
        {
            var grid = new InventoryGrid(1, 1);
            grid.LoadSaveData(state.items);
            foreach (var p in grid.GetAll())
                if (CanEquipWeapon(p.item.data, state.slot)) PutWeapon(state.slot, p.item);
        }
    }

    /// <summary>장비 총 무게 (kg)</summary>
    public float TotalEquipWeight
    {
        get
        {
            float w = 0f;
            foreach (var kv in slots)
                if (kv.Value != null) w += kv.Value.weight;
            // 장착 무기의 부착물 무게 합산
            foreach (var kv in slotInstances)
                if (kv.Value != null) w += kv.Value.AttachmentWeight;
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
        if (item.equipSlot == EquipSlot.None) return ToggleAsPrimary(item);
        return Equip(item);
    }

    /// <summary>무기를 주무기 칸에 꺼내 든다(같은 무기면 넣는다) — 전투는 주무기 칸만 읽는다.
    /// 아이템의 equipSlot과 무관하게 주무기로 — 퀵슬롯 무기 전환(2026-09-11, docs/combat.md §무기 구성 결정)이 쓴다.</summary>
    public bool ToggleAsPrimary(ItemData item)
    {
        if (item == null || item.category != ItemCategory.Weapon) return false;
        if (GetSlot(EquipSlot.PrimaryWeapon) == item) { Unequip(EquipSlot.PrimaryWeapon); return true; }
        if (slots.ContainsKey(EquipSlot.PrimaryWeapon)) Unequip(EquipSlot.PrimaryWeapon);
        slots[EquipSlot.PrimaryWeapon] = item;
        if (player != null) player.SetWeapon(item.weaponData);
        OnEquipChanged?.Invoke(EquipSlot.PrimaryWeapon, item);
        Debug.Log($"[Equip] 무기 장착: {item.displayName}");
        return true;
    }

    public void Unequip(EquipSlot slot)
    {
        ItemData prev;
        if (!slots.TryGetValue(slot, out prev)) return;
        slots.Remove(slot);
        slotInstances.Remove(slot);

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

    /// <summary>새 게임 — 모든 슬롯 해제. SaveManager.ResetToNewGame용.
    /// (Unequip이 slots를 수정하므로 키 사본으로 순회.)</summary>
    public void ResetForNewGame()
    {
        foreach (var slot in new List<EquipSlot>(slots.Keys))
            Unequip(slot);
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
        slotInstances.Clear();
        if (player != null) player.SetWeapon(null);
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
