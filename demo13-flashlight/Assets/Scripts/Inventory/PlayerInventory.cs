using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 인벤토리 컴포넌트.
/// Player에 부착. InventoryGrid를 소유하고 아이템 사용 로직 처리.
/// 가방 미장착 = 주머니(pocketWidth×pocketHeight). 가방 장착 시 가방 크기로 확장.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("주머니 (가방 미장착 시)")]
    [SerializeField] int pocketWidth = 0;
    [SerializeField] int pocketHeight = 0;

    [Header("무게")]
    [SerializeField] float maxWeight = 30f;

    public InventoryGrid Grid { get; private set; }
    public float MaxWeight => maxWeight;

    /// <summary>인벤토리 아이템 + 장비 무게 합산</summary>
    public float CurrentWeight
    {
        get
        {
            float w = Grid != null ? Grid.TotalWeight : 0f;
            if (equipment != null) w += equipment.TotalEquipWeight;
            return w;
        }
    }

    public bool IsOverweight => CurrentWeight > maxWeight;

    // 캐시
    Health health;
    PlayerMedicalSystem medical;
    FlashlightController flashlight;
    PlayerEquipment equipment;

    void Awake()
    {
        Grid = new InventoryGrid(pocketWidth, pocketHeight);

        health = GetComponent<Health>();
        medical = GetComponent<PlayerMedicalSystem>();
        flashlight = GetComponentInChildren<FlashlightController>();
        equipment = GetComponent<PlayerEquipment>();
    }

    /// <summary>가방 장착/해제 시 호출. 격자 크기를 가방에 맞게 변경.</summary>
    public void OnBackpackChanged(ItemData backpack)
    {
        int newW, newH;
        if (backpack != null && backpack.containerWidth > 0 && backpack.containerHeight > 0)
        {
            newW = backpack.containerWidth;
            newH = backpack.containerHeight;
        }
        else
        {
            newW = pocketWidth;
            newH = pocketHeight;
        }

        if (Grid.width == newW && Grid.height == newH) return;

        var overflow = Grid.Resize(newW, newH);

        // 넘치는 아이템은 메인 창고로 이동, 그래도 안 되면 월드 드롭
        if (overflow.Count > 0)
        {
            var stash = MainStash.Ensure();
            var stashGrid = stash != null ? stash.GetGrid() : null;

            for (int i = 0; i < overflow.Count; i++)
            {
                bool placed = false;
                if (stashGrid != null)
                    placed = stashGrid.TryAutoPlace(overflow[i]);

                if (!placed)
                {
                    WorldItem.Drop(overflow[i], transform.position + transform.right * 0.5f);
                    Debug.LogWarning($"[Inventory] 가방 축소 — {overflow[i].DisplayName} 월드 드롭");
                }
                else
                {
                    Debug.Log($"[Inventory] 가방 축소 — {overflow[i].DisplayName} 창고로 이동");
                }
            }
        }
    }

    /// <summary>가방 장착 여부</summary>
    public bool HasBackpack => Grid != null && Grid.width > 0 && Grid.height > 0;

    /// <summary>아이템 줍기 시도. 성공하면 true.</summary>
    public bool TryPickup(ItemInstance item)
    {
        if (item == null || item.data == null) return false;
        if (!HasBackpack)
        {
            Debug.Log("[Inventory] 가방 미장착 — 아이템 줍기 불가");
            return false;
        }
        return Grid.TryAutoPlace(item);
    }

    /// <summary>아이템 사용 (우클릭)</summary>
    public bool UseItem(InventoryGrid.PlacedItem placed)
    {
        if (placed == null) return false;
        var item = placed.item;
        if (item.data == null) return false;

        // 장비 아이템 → 장착(토글)
        if (item.data.equipSlot != EquipSlot.None)
        {
            if (equipment == null) equipment = GetComponent<PlayerEquipment>();
            return equipment != null && equipment.Equip(item.data);
        }

        // 무기(equipSlot=None인 구형 무기) → 장착(토글)
        if (item.data.category == ItemCategory.Weapon)
        {
            if (equipment == null) equipment = GetComponent<PlayerEquipment>();
            return equipment != null && equipment.EquipWeapon(item.data);
        }

        if (!item.data.isUsable) return false;

        bool used = false;

        if (CraftingSystem.Instance != null && CraftingSystem.Instance.TryUnlockFromItem(item.data.itemId))
            used = true;

        if (!used)
        switch (item.data.useEffect)
        {
            case ItemUseEffect.HealHP:
                if (health != null && health.CurrentHp < health.MaxHp)
                {
                    health.Heal(item.data.effectValue);
                    used = true;
                }
                break;

            case ItemUseEffect.HealInjury:
                if (medical != null && medical.HasAnyInjury && item.data.medicalData != null)
                {
                    var parts = medical.GetAllParts();
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (!parts[i].IsInjured) continue;
                        for (int j = 0; j < parts[i].injuries.Count; j++)
                        {
                            if (item.data.medicalData.CanTreat(parts[i].injuries[j].type))
                            {
                                medical.StartHealing(parts[i].partType, parts[i].injuries[j].type, item.data.medicalData);
                                used = true;
                                goto doneHeal;
                            }
                        }
                    }
                    doneHeal:;
                }
                break;

            case ItemUseEffect.AddBattery:
                if (flashlight != null)
                {
                    flashlight.AddBattery(item.data.effectValue);
                    used = true;
                }
                break;

            case ItemUseEffect.Food:
            {
                var survival = SurvivalStats.Get();
                if (survival != null)
                {
                    survival.AddSatiety(item.data.effectValue);
                    survival.AddWater(item.data.effectValue * 0.5f);
                }
                used = true;
                break;
            }
        }

        if (used)
        {
            if (item.HasDurability)
            {
                item.durability -= item.data.durabilityCostPerUse;
                if (item.durability <= 0f)
                {
                    Grid.Remove(placed);
                    Debug.Log($"[Inventory] {item.data.displayName} 내구도 소진 → 파괴");
                }
                else
                {
                    Grid.NotifyChanged();
                    Debug.Log($"[Inventory] {item.data.displayName} 사용 (내구도: {item.durability:F0}/{item.data.maxDurability:F0})");
                }
            }
            else
            {
                item.stackCount--;
                if (item.stackCount <= 0)
                    Grid.Remove(placed);
                else
                    Grid.NotifyChanged();

                Debug.Log($"[Inventory] {item.data.displayName} 사용");
            }
        }

        return used;
    }

    /// <summary>아이템 버리기 (월드에 드롭)</summary>
    public void DropItem(InventoryGrid.PlacedItem placed)
    {
        if (placed == null) return;

        Grid.Remove(placed);
        WorldItem.Drop(placed.item, transform.position + transform.right * 0.5f);
        Debug.Log($"[Inventory] {placed.item.DisplayName} 드롭");
    }
}
