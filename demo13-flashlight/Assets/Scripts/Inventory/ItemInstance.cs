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

    /// <summary>무기 부착물(파츠) — 종류별 itemId. 길이 4 = [Scope, Muzzle, Magazine, Grip]. (무기 인스턴스에 귀속, 타르코프식)</summary>
    public string[] attachments;

    /// <summary>남은 탄. **탄창 인스턴스**엔 그 탄창에 든 탄, **총기 인스턴스**엔 장착 탄창에 남은 탄.
    ///
    /// 2026-07-29: 탄창을 아이템으로 만들면서(타르코프식) 탄창은 '탄을 담은 개별 물건'이 됐다.
    ///   총기가 탄창 인스턴스를 **품게** 하면 자기참조 타입이 되어 유니티 직렬화가 못 다룬다
    ///   → 총기와 탄창이 **같은 두 필드를 나눠 쓴다**. 장전은 이 값을 서로 옮기는 일이다.
    ///   그래서 반쯤 쓴 탄창을 빼도 남은 탄이 그대로 따라 나온다.</summary>
    public int ammoCount;

    /// <summary>담긴 탄의 itemId(탄종). ammoCount가 0이면 의미 없다.</summary>
    public string ammoItemId;

    /// <summary>이 인스턴스가 담을 수 있는 최대 탄 수. 탄창=제 용량 / 총기=장착 탄창의 용량(없으면 0).</summary>
    public int AmmoCapacity
    {
        get
        {
            if (data == null) return 0;
            if (data.IsMagazine) return data.magCapacity;
            var mag = LoadedMagazineData;
            return mag != null ? mag.magCapacity : 0;
        }
    }

    /// <summary>총기에 물려 있는 탄창의 ItemData(없으면 null). 부착 슬롯 [2]=Magazine.</summary>
    public ItemData LoadedMagazineData
    {
        get
        {
            string id = GetAttachment(WeaponPartType.Magazine);
            if (string.IsNullOrEmpty(id)) return null;
            var d = ItemDatabase.Get(id);
            return (d != null && d.IsMagazine) ? d : null;
        }
    }

    /// <summary>보관함 내부 격자(컨테이너 아이템만, 인스턴스 귀속). 첫 접근 시 internalW×H로 생성.</summary>
    [System.NonSerialized] InventoryGrid _containerGrid;

    /// <summary>이 인스턴스가 보관함인지.</summary>
    public bool IsContainer => data != null && data.IsContainer;

    /// <summary>보관함 내부 격자(컨테이너가 아니면 null). 지연 생성.</summary>
    public InventoryGrid ContainerGrid
    {
        get
        {
            if (data == null || !data.IsContainer) return null;
            if (_containerGrid == null)
                _containerGrid = new InventoryGrid(data.ContainerGridWidth, data.ContainerGridHeight);
            return _containerGrid;
        }
    }

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

    // ── 무기 부착물 ──────────────────────────────────────────────────
    /// <summary>종류 t에 부착된 파츠 itemId (없으면 null).</summary>
    public string GetAttachment(WeaponPartType t)
    {
        int i = (int)t - 1;
        return (attachments != null && i >= 0 && i < attachments.Length) ? attachments[i] : null;
    }

    /// <summary>종류 t에 파츠 itemId 부착(null/"" = 해제).</summary>
    public void SetAttachment(WeaponPartType t, string itemId)
    {
        int i = (int)t - 1;
        if (i < 0 || i > 3) return;
        if (attachments == null) attachments = new string[4];
        attachments[i] = string.IsNullOrEmpty(itemId) ? null : itemId;
    }

    /// <summary>부착물이 하나라도 있는지.</summary>
    public bool HasAnyAttachment =>
        attachments != null && System.Array.Exists(attachments, a => !string.IsNullOrEmpty(a));

    /// <summary>부착물 무게 합(파츠 ItemData 조회).</summary>
    public float AttachmentWeight
    {
        get
        {
            if (attachments == null) return 0f;
            float w = 0f;
            for (int i = 0; i < attachments.Length; i++)
            {
                if (string.IsNullOrEmpty(attachments[i])) continue;
                var d = ItemDatabase.Get(attachments[i]);
                if (d != null) w += d.weight;
            }
            return w;
        }
    }

    /// <summary>총 무게 (부착물 + 보관함 내용물 포함)</summary>
    public float TotalWeight
    {
        get
        {
            float w = (data != null ? data.weight * stackCount : 0f) + AttachmentWeight;
            if (_containerGrid != null)
                foreach (var p in _containerGrid.GetAll())
                    if (p.item != null) w += p.item.TotalWeight;
            return w;
        }
    }

    /// <summary>표시용 이름 (스택이면 수량 포함)</summary>
    public string DisplayName
    {
        get
        {
            if (data == null) return "???";
            // 탄창은 **몇 발 들었는지**가 이름의 일부다 — 안 보이면 채웠는지 아닌지 알 수가 없다.
            if (data.IsMagazine) return $"{data.displayName} [{ammoCount}/{data.magCapacity}]";
            if (stackCount > 1)
                return $"{data.displayName} x{stackCount}";
            return data.displayName;
        }
    }

    public override string ToString() => $"[{uid}] {DisplayName}";
}
