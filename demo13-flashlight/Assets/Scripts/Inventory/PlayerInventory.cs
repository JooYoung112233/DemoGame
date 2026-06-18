using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 인벤토리 컴포넌트.
/// Player에 부착. InventoryGrid를 소유하고 아이템 사용 로직 처리.
/// 가방 미장착 = 주머니(pocketWidth×pocketHeight). 가방 장착 시 가방 크기로 확장.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("무게")]
    [SerializeField] float maxWeight = 30f;

    public InventoryGrid Grid { get; private set; }          // 가방(백팩) — 장착 시 그 크기, 미장착 0×0
    public InventoryGrid PocketsGrid { get; private set; }   // 주머니 4칸 (고정, 항상 존재)
    public InventoryGrid SecureGrid { get; private set; }    // 보안 컨테이너 3×3 (고정, 항상 존재)
    public float MaxWeight => maxWeight;

    // 고정 컨테이너 크기 (가로 기준)
    const int PocketW = 4, PocketH = 1;
    const int SecureW = 3, SecureH = 3;

    /// <summary>인벤토리 아이템 + 장비 무게 합산</summary>
    public float CurrentWeight
    {
        get
        {
            float w = Grid != null ? Grid.TotalWeight : 0f;
            if (PocketsGrid != null) w += PocketsGrid.TotalWeight;
            if (SecureGrid != null) w += SecureGrid.TotalWeight;
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
        // 다중 컨테이너: 가방(미장착=0×0) + 고정 주머니 4칸 + 고정 보안 3×3.
        Grid = new InventoryGrid(0, 0);
        PocketsGrid = new InventoryGrid(PocketW, PocketH);
        SecureGrid = new InventoryGrid(SecureW, SecureH);

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
            newW = 0;   // 가방 미장착 = 가방 격자 0×0 (주머니는 별도 PocketsGrid 전담)
            newH = 0;
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
        if (HasBackpack && Grid.TryAutoPlace(item)) return true;   // 가방 우선
        if (PocketsGrid != null && PocketsGrid.TryAutoPlace(item)) return true;  // 주머니 차선
        Debug.Log("[Inventory] 공간 부족 (가방·주머니)");
        return false;
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
            var g = GridOf(placed) ?? Grid;   // placed가 속한 격자(가방/주머니/보안)에서 처리
            if (item.HasDurability)
            {
                item.durability -= item.data.durabilityCostPerUse;
                if (item.durability <= 0f)
                {
                    g.Remove(placed);
                    Debug.Log($"[Inventory] {item.data.displayName} 내구도 소진 → 파괴");
                }
                else
                {
                    g.NotifyChanged();
                    Debug.Log($"[Inventory] {item.data.displayName} 사용 (내구도: {item.durability:F0}/{item.data.maxDurability:F0})");
                }
            }
            else
            {
                item.stackCount--;
                if (item.stackCount <= 0)
                    g.Remove(placed);
                else
                    g.NotifyChanged();

                Debug.Log($"[Inventory] {item.data.displayName} 사용");
            }
        }

        return used;
    }

    /// <summary>아이템 버리기 (월드에 드롭)</summary>
    public void DropItem(InventoryGrid.PlacedItem placed)
    {
        if (placed == null) return;

        (GridOf(placed) ?? Grid).Remove(placed);
        WorldItem.Drop(placed.item, transform.position + transform.right * 0.5f);
        Debug.Log($"[Inventory] {placed.item.DisplayName} 드롭");
    }

    // ── 다중 컨테이너 집계 API (가방 + 주머니 + 보안) ───────────────
    InventoryGrid[] AllGrids => new[] { Grid, PocketsGrid, SecureGrid };

    /// <summary>placed가 속한 격자(가방/주머니/보안) 반환.</summary>
    public InventoryGrid GridOf(InventoryGrid.PlacedItem placed)
    {
        if (placed == null) return null;
        foreach (var g in AllGrids)
            if (g != null && g.GetAll().Contains(placed)) return g;
        return null;
    }

    /// <summary>가방+주머니+보안 합산 아이템 수.</summary>
    public int CountItemAll(string itemId)
    {
        int n = 0;
        foreach (var g in AllGrids) if (g != null) n += g.CountItem(itemId);
        return n;
    }

    /// <summary>전 컨테이너에서 아이템 소비(부족하면 false, 소비 안 함).</summary>
    public bool ConsumeItemAll(string itemId, int count)
    {
        if (CountItemAll(itemId) < count) return false;
        int remaining = count;
        foreach (var g in AllGrids)
        {
            if (g == null || remaining <= 0) continue;
            int take = Mathf.Min(g.CountItem(itemId), remaining);
            if (take > 0) { g.ConsumeItem(itemId, take); remaining -= take; }
        }
        return remaining <= 0;
    }

    /// <summary>가방→주머니→보안 순으로 자동 배치 시도.</summary>
    public bool TryAutoPlaceAnywhere(ItemInstance item)
    {
        if (item == null) return false;
        if (HasBackpack && Grid.TryAutoPlace(item)) return true;
        if (PocketsGrid != null && PocketsGrid.TryAutoPlace(item)) return true;
        if (SecureGrid != null && SecureGrid.TryAutoPlace(item)) return true;
        return false;
    }

    /// <summary>아무 컨테이너에든 배치 가능한지(공간 체크).</summary>
    public bool CanPlaceAnywhere(ItemInstance item)
    {
        if (item == null) return false;
        foreach (var g in AllGrids) if (g != null && g.CanAutoPlace(item)) return true;
        return false;
    }
}
