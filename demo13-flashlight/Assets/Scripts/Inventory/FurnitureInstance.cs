using UnityEngine;

/// <summary>
/// 가구 런타임 인스턴스.
/// FurnitureData(SO) 참조 + 자체 InventoryGrid + 카테고리 필터.
/// SafehouseStorage의 static 목록에서 관리됨.
/// </summary>
[System.Serializable]
public class FurnitureInstance
{
    static int nextUid = 1;

    /// <summary>고유 인스턴스 ID (런타임 생성)</summary>
    public int uid;

    /// <summary>가구 데이터 (SO 참조)</summary>
    public FurnitureData data;

    /// <summary>이 가구의 인벤토리 격자</summary>
    public InventoryGrid grid;

    public FurnitureInstance(FurnitureData data)
    {
        uid = nextUid++;
        this.data = data;
        grid = new InventoryGrid(data.gridWidth, data.gridHeight);

        // 격자에 카테고리 필터 설정
        if (!data.IsUniversal)
            grid.AcceptFilter = (item) => data.AcceptsItem(item);
    }

    /// <summary>표시 이름</summary>
    public string DisplayName => data != null ? data.displayName : "???";

    /// <summary>해당 아이템을 이 가구에 보관 가능한지</summary>
    public bool AcceptsItem(ItemInstance item) => data != null && data.AcceptsItem(item);

    /// <summary>에디터 도메인 리로드 시 UID 리셋</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        nextUid = 1;
    }
}
