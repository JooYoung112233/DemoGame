using UnityEngine;

/// <summary>
/// 아이템 런타임 인스턴스.
/// ItemData(SO)를 참조하되, 스택 수/내구도 등 개별 상태를 가짐.
/// </summary>
[System.Serializable]
public class ItemInstance
{
    static int nextUid = 1;

    /// <summary>런타임 고유 ID (저장/비교용)</summary>
    public int uid;

    /// <summary>원본 아이템 데이터</summary>
    public ItemData data;

    /// <summary>현재 스택 수</summary>
    public int stackCount;

    /// <summary>현재 내구도 (hasDurability 아이템은 절대값, 아니면 0~1)</summary>
    public float durability;

    public ItemInstance(ItemData data, int count = 1)
    {
        uid = nextUid++;
        this.data = data;
        stackCount = Mathf.Clamp(count, 1, data != null ? data.maxStack : 1);
        // 내구도형 아이템은 maxDurability로 초기화
        durability = (data != null && data.hasDurability) ? data.maxDurability : 1f;
    }

    /// <summary>내구도형 아이템인지</summary>
    public bool HasDurability => data != null && data.hasDurability;

    /// <summary>내구도 비율 (0~1, UI 게이지용)</summary>
    public float DurabilityRatio => (data != null && data.hasDurability && data.maxDurability > 0)
        ? Mathf.Clamp01(durability / data.maxDurability) : 1f;

    /// <summary>남은 사용 횟수</summary>
    public int RemainingUses => (data != null && data.hasDurability && data.durabilityCostPerUse > 0)
        ? Mathf.FloorToInt(durability / data.durabilityCostPerUse) : 0;

    /// <summary>스택 가능 여부 (같은 아이템 + 여유 공간)</summary>
    public bool CanStackWith(ItemInstance other)
    {
        if (other == null || data == null || other.data == null) return false;
        if (data.itemId != other.data.itemId) return false;
        if (data.maxStack <= 1) return false;
        // 내구도형 아이템은 스택 불가
        if (data.hasDurability) return false;
        return stackCount < data.maxStack;
    }

    /// <summary>스택 시도. 넘치는 수량 반환 (0이면 전부 합쳐짐)</summary>
    public int TryStack(ItemInstance from)
    {
        if (!CanStackWith(from)) return from.stackCount;

        int space = data.maxStack - stackCount;
        int toAdd = Mathf.Min(from.stackCount, space);
        stackCount += toAdd;
        from.stackCount -= toAdd;
        return from.stackCount; // 남은 수량
    }

    /// <summary>스택에서 일부 분리</summary>
    public ItemInstance Split(int count)
    {
        if (count >= stackCount || count <= 0) return null;
        stackCount -= count;
        return new ItemInstance(data, count);
    }

    /// <summary>총 무게</summary>
    public float TotalWeight => data != null ? data.weight * stackCount : 0f;

    /// <summary>표시용 이름 (스택이면 수량 포함)</summary>
    public string DisplayName
    {
        get
        {
            if (data == null) return "???";
            if (stackCount > 1)
                return $"{data.displayName} x{stackCount}";
            return data.displayName;
        }
    }

    public override string ToString() => $"[{uid}] {DisplayName}";
}
