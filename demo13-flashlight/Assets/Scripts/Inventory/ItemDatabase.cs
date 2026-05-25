using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 전체 아이템 DB. Resources/Items/ (하위 폴더 포함) 에서 자동 로드.
/// 싱글톤 — ItemDatabase.Get("bandage") 형태로 조회.
/// </summary>
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    Dictionary<string, ItemData> db = new Dictionary<string, ItemData>();
    ItemData[] allItems;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        if (FindFirstObjectByType<ItemDatabase>(FindObjectsInactive.Include) != null) return;

        var go = new GameObject("[ItemDatabase]");
        go.AddComponent<ItemDatabase>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAll();
    }

    void LoadAll()
    {
        allItems = Resources.LoadAll<ItemData>("Items");
        db.Clear();

        for (int i = 0; i < allItems.Length; i++)
        {
            var item = allItems[i];
            if (string.IsNullOrEmpty(item.itemId))
            {
                Debug.LogWarning($"[ItemDatabase] itemId가 비어있는 SO: {item.name}");
                continue;
            }

            if (db.ContainsKey(item.itemId))
            {
                Debug.LogWarning($"[ItemDatabase] 중복 itemId: {item.itemId}");
                continue;
            }

            db[item.itemId] = item;
        }

        Debug.Log($"[ItemDatabase] {db.Count}개 아이템 로드 완료");
    }

    /// <summary>ID로 아이템 데이터 조회</summary>
    public static ItemData Get(string itemId)
    {
        if (Instance == null || string.IsNullOrEmpty(itemId)) return null;
        Instance.db.TryGetValue(itemId, out var data);
        return data;
    }

    /// <summary>전체 아이템 배열</summary>
    public static ItemData[] GetAll()
    {
        return Instance != null ? Instance.allItems : new ItemData[0];
    }

    /// <summary>지역 전용 아이템 (primaryRegionId 일치)</summary>
    public static List<ItemData> GetByPrimaryRegion(string regionId)
    {
        var result = new List<ItemData>();
        if (Instance == null || string.IsNullOrEmpty(regionId)) return result;

        for (int i = 0; i < Instance.allItems.Length; i++)
        {
            var item = Instance.allItems[i];
            if (item.primaryRegionId == regionId)
                result.Add(item);
        }
        return result;
    }

    /// <summary>카테고리로 필터링</summary>
    public static List<ItemData> GetByCategory(ItemCategory category)
    {
        var result = new List<ItemData>();
        if (Instance == null) return result;

        for (int i = 0; i < Instance.allItems.Length; i++)
        {
            if (Instance.allItems[i].category == category)
                result.Add(Instance.allItems[i]);
        }
        return result;
    }
}
