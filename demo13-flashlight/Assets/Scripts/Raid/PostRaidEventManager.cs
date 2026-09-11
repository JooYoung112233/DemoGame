using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PostRaidEventManager : MonoBehaviour
{
    public static PostRaidEventManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("이벤트 발생 기본 확률 (0~1)")]
    [SerializeField] float baseChance = 0.4f;

    [Header("Events")]
    [Tooltip("등록된 포스트 레이드 이벤트 목록")]
    [SerializeField] PostRaidEventData[] allEvents;

    HashSet<string> completedOneShots = new HashSet<string>();
    Queue<string> recentEvents = new Queue<string>();
    const int RECENT_EXCLUDE_COUNT = 2;

    public event System.Action<PostRaidEventData> OnEventTriggered;
    public event System.Action OnNoEvent;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        HierarchyFolder.Persist(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public PostRaidEventData TryGetEvent()
    {
        if (Random.value > baseChance)
        {
            OnNoEvent?.Invoke();
            return null;
        }

        var eligible = GetEligibleEvents();
        if (eligible.Count == 0)
        {
            OnNoEvent?.Invoke();
            return null;
        }

        var selected = WeightedRandom(eligible);

        recentEvents.Enqueue(selected.eventId);
        if (recentEvents.Count > RECENT_EXCLUDE_COUNT)
            recentEvents.Dequeue();

        if (selected.oneShot)
            completedOneShots.Add(selected.eventId);

        OnEventTriggered?.Invoke(selected);
        return selected;
    }

    List<PostRaidEventData> GetEligibleEvents()
    {
        var result = new List<PostRaidEventData>();
        if (allEvents == null) return result;

        foreach (var evt in allEvents)
        {
            if (evt == null) continue;
            if (evt.oneShot && completedOneShots.Contains(evt.eventId)) continue;
            if (recentEvents.Contains(evt.eventId)) continue;
            if (!CheckCondition(evt.conditions)) continue;
            result.Add(evt);
        }
        return result;
    }

    bool CheckCondition(EventCondition cond)
    {
        if (cond == null) return true;

        if (cond.minRaidTime > 0 && RaidManager.Instance != null)
        {
            if (RaidManager.Instance.ElapsedTime < cond.minRaidTime) return false;
        }

        if (cond.minLootCount > 0 && RaidManager.Instance != null)
        {
            if (RaidManager.Instance.LootedItems.Count < cond.minLootCount) return false;
        }

        // 밤/지역 조건 — 탈출 시점 값을 RaidManager가 기록해 둔다(이 호출은 귀환 뒤라 지금 시각·씬이 아니다).
        if (cond.nightOnly && !RaidManager.LastExtractWasNight) return false;

        if (!string.IsNullOrEmpty(cond.requiredRegion)
            && !string.Equals(cond.requiredRegion, RaidManager.LastExtractRegionId, System.StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    PostRaidEventData WeightedRandom(List<PostRaidEventData> pool)
    {
        float totalWeight = pool.Sum(e => e.weight);
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var evt in pool)
        {
            cumulative += evt.weight;
            if (roll <= cumulative) return evt;
        }
        return pool[pool.Count - 1];
    }
}
