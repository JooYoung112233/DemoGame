using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 안전가옥 가구 창고 컴포넌트.
/// 각 가구 오브젝트에 하나씩 부착. FurnitureData SO를 참조.
/// 전체 가구 목록은 static으로 씬 전환 후에도 유지.
///
/// 사용법:
/// 1. InteractableObject(Container) + SafehouseStorage를 같은 오브젝트에 부착
/// 2. Inspector에서 furnitureData에 FurnitureData SO 할당
/// 3. 플레이어가 상호작용하면 해당 가구의 격자가 UI에 표시됨
/// </summary>
public class SafehouseStorage : MonoBehaviour
{
    [Header("가구 설정")]
    [Tooltip("이 가구의 데이터 (ScriptableObject). 비어있으면 기본 범용 상자 생성")]
    [SerializeField] FurnitureData furnitureData;

    [Tooltip("이 오브젝트에 연결된 가구 인스턴스 UID. -1이면 자동 할당")]
    [SerializeField] int furnitureUid = -1;

    // ── static 가구 목록 (전 씬 공유) ──
    static List<FurnitureInstance> allFurniture = new List<FurnitureInstance>();
    static bool initialized = false;

    // 이 오브젝트에 연결된 가구 인스턴스
    FurnitureInstance linkedInstance;

    /// <summary>연결된 가구 인스턴스</summary>
    public FurnitureInstance LinkedInstance => linkedInstance;

    /// <summary>전체 보유 가구 목록</summary>
    public static IReadOnlyList<FurnitureInstance> AllFurniture => allFurniture;

    // ── 하위 호환 프로퍼티 ──

    /// <summary>이 가구의 격자 (기존 코드 호환)</summary>
    public InventoryGrid Grid => linkedInstance?.grid;

    /// <summary>이 가구의 표시 이름 (기존 코드 호환)</summary>
    public string StorageName
    {
        get
        {
            if (linkedInstance != null) return linkedInstance.DisplayName;
            if (furnitureData != null) return furnitureData.displayName;
            return "창고";
        }
    }

    void Awake()
    {
        EnsureInitialized();
        LinkOrCreate();
    }

    /// <summary>런타임 배치 시 FurnitureData 할당 (부트스트랩·에디터 스크립트)</summary>
    public void AssignFurnitureData(FurnitureData data)
    {
        furnitureData = data;
        furnitureUid = -1;
        EnsureInitialized();
        LinkOrCreate();
    }

    void OnDestroy()
    {
        linkedInstance = null;
    }

    /// <summary>가구 열기 — CharacterPanelUI에 격자 표시</summary>
    public void Open()
    {
        if (linkedInstance == null)
        {
            Debug.LogWarning("[SafehouseStorage] linkedInstance가 null");
            return;
        }

        string catInfo = furnitureData != null ? furnitureData.AllowedCategorySummary : "전체";
        Debug.Log($"[SafehouseStorage] {StorageName} 열기 ({linkedInstance.grid.width}x{linkedInstance.grid.height}, " +
                  $"아이템 {linkedInstance.grid.ItemCount}개, 허용: {catInfo})");

        if (UIManager.Instance != null)
            UIManager.Instance.ShowCharacterPanelWithStorage(this);
    }

    // ── 가구 링크 / 생성 ──

    void LinkOrCreate()
    {
        // UID로 기존 인스턴스 찾기
        if (furnitureUid >= 0)
        {
            linkedInstance = FindByUid(furnitureUid);
            if (linkedInstance != null) return;
        }

        // 새 인스턴스 생성
        if (furnitureData != null)
        {
            linkedInstance = AddFurniture(furnitureData);
            furnitureUid = linkedInstance.uid;
        }
        else
        {
            // furnitureData 없으면 기본 범용 상자
            var defaultData = CreateDefaultData();
            linkedInstance = AddFurniture(defaultData);
            furnitureUid = linkedInstance.uid;
        }
    }

    static FurnitureData CreateDefaultData()
    {
        var data = ScriptableObject.CreateInstance<FurnitureData>();
        data.furnitureId = "box_default";
        data.displayName = "일반 상자";
        data.gridWidth = 4;
        data.gridHeight = 4;
        data.allowedCategories = new ItemCategory[0]; // 범용
        return data;
    }

    // ── static 관리 API ──

    static void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        // 첫 실행 시 빈 목록 (이미 new로 초기화됨)
    }

    /// <summary>가구 추가 (구매). 인스턴스 반환.</summary>
    public static FurnitureInstance AddFurniture(FurnitureData data)
    {
        EnsureInitialized();
        var instance = new FurnitureInstance(data);
        allFurniture.Add(instance);
        Debug.Log($"[SafehouseStorage] 가구 추가: {data.displayName} (uid={instance.uid}, {data.gridWidth}x{data.gridHeight})");
        return instance;
    }

    /// <summary>가구 제거 (판매/파괴)</summary>
    public static bool RemoveFurniture(int uid)
    {
        for (int i = 0; i < allFurniture.Count; i++)
        {
            if (allFurniture[i].uid == uid)
            {
                Debug.Log($"[SafehouseStorage] 가구 제거: {allFurniture[i].DisplayName} (uid={uid})");
                allFurniture.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>UID로 가구 인스턴스 찾기</summary>
    public static FurnitureInstance FindByUid(int uid)
    {
        for (int i = 0; i < allFurniture.Count; i++)
            if (allFurniture[i].uid == uid) return allFurniture[i];
        return null;
    }

    /// <summary>타입별 가구 목록</summary>
    public static List<FurnitureInstance> GetByType(string furnitureId)
    {
        var result = new List<FurnitureInstance>();
        for (int i = 0; i < allFurniture.Count; i++)
            if (allFurniture[i].data != null && allFurniture[i].data.furnitureId == furnitureId)
                result.Add(allFurniture[i]);
        return result;
    }

    // ── 전체 가구 검색 유틸 ──

    /// <summary>모든 가구에서 특정 아이템의 총 수량</summary>
    public static int CountItemAcrossAll(string itemId)
    {
        int total = 0;
        for (int i = 0; i < allFurniture.Count; i++)
            total += allFurniture[i].grid.CountItem(itemId);
        return total;
    }

    /// <summary>모든 가구에서 아이템 소비 (재료 차감 등)</summary>
    public static bool ConsumeItemAcrossAll(string itemId, int count)
    {
        int remaining = count;
        for (int i = 0; i < allFurniture.Count && remaining > 0; i++)
        {
            int has = allFurniture[i].grid.CountItem(itemId);
            if (has <= 0) continue;
            int consume = Mathf.Min(has, remaining);
            allFurniture[i].grid.ConsumeItem(itemId, consume);
            remaining -= consume;
        }
        return remaining <= 0;
    }

    /// <summary>아이템을 수용 가능한 최적 가구에 자동 배치</summary>
    public static bool AutoPlaceInBestFurniture(ItemInstance item)
    {
        if (item == null || item.data == null) return false;

        // 1순위: 전용 가구 (카테고리 일치)
        for (int i = 0; i < allFurniture.Count; i++)
        {
            if (allFurniture[i].data.IsUniversal) continue;
            if (allFurniture[i].AcceptsItem(item) && allFurniture[i].grid.TryAutoPlace(item))
                return true;
        }

        // 2순위: 범용 가구
        for (int i = 0; i < allFurniture.Count; i++)
        {
            if (!allFurniture[i].data.IsUniversal) continue;
            if (allFurniture[i].grid.TryAutoPlace(item))
                return true;
        }

        return false;
    }

    // ── 에디터 도메인 리로드 대응 ──

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        allFurniture = new List<FurnitureInstance>();
        initialized = false;
    }
}
