using UnityEngine;

/// <summary>
/// 플레이어 인벤토리 컴포넌트.
/// Player에 부착. InventoryGrid를 소유하고 아이템 사용 로직 처리.
/// PlayerController가 있는 GO에 자동 부착됨.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerInventory : MonoBehaviour
{
    [Header("가방 설정")]
    [Tooltip("가방 격자 가로 칸 수")]
    [SerializeField] int bagWidth = 5;
    [Tooltip("가방 격자 세로 칸 수")]
    [SerializeField] int bagHeight = 8;
    [Tooltip("최대 무게 (kg)")]
    [SerializeField] float maxWeight = 30f;

    public InventoryGrid Grid { get; private set; }
    public float MaxWeight => maxWeight;
    public float CurrentWeight => Grid != null ? Grid.TotalWeight : 0f;
    public bool IsOverweight => CurrentWeight > maxWeight;

    // 캐시
    Health health;
    PlayerMedicalSystem medical;
    FlashlightController flashlight;
    PlayerController playerCtrl;

    void Awake()
    {
        Grid = new InventoryGrid(bagWidth, bagHeight);

        health = GetComponent<Health>();
        medical = GetComponent<PlayerMedicalSystem>();
        flashlight = GetComponentInChildren<FlashlightController>();
        playerCtrl = GetComponent<PlayerController>();
    }

    /// <summary>아이템 줍기 시도. 성공하면 true.</summary>
    public bool TryPickup(ItemInstance item)
    {
        if (item == null || item.data == null) return false;

        // 무게 체크 (초과해도 주울 수는 있되 이동속도 페널티)
        return Grid.TryAutoPlace(item);
    }

    /// <summary>아이템 사용 (우클릭)</summary>
    public bool UseItem(InventoryGrid.PlacedItem placed)
    {
        if (placed == null) return false;
        var item = placed.item;
        if (item.data == null || !item.data.isUsable) return false;

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
                // MedicalItemData 연동 — 향후 부위 선택 UI 필요
                // 현재는 가장 심한 부상 자동 치료
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

            case ItemUseEffect.RestoreStamina:
                if (playerCtrl != null)
                {
                    // 스태미너 직접 회복 — PlayerController에 메서드 필요
                    // 임시: ConsumeStamina 음수로는 안되니까 TODO
                    used = true;
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
                // 향후 포만감 시스템
                used = true;
                break;
        }

        // 사용 성공 시 소모 처리
        if (used)
        {
            if (item.HasDurability)
            {
                // 내구도형: 내구도 감소
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
                // 일회성: 스택 감소
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

        // WorldItem 생성
        WorldItem.Drop(placed.item, transform.position + transform.forward * 0.5f);

        Debug.Log($"[Inventory] {placed.item.DisplayName} 드롭");
    }
}
