using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    List<QuestInstance> activeQuests = new List<QuestInstance>();
    HashSet<string> completedQuestIds = new HashSet<string>();
    Dictionary<string, bool> flags = new Dictionary<string, bool>();

    public List<QuestInstance> ActiveQuests => activeQuests;
    public HashSet<string> CompletedQuestIds => completedQuestIds;

    public event System.Action<QuestInstance, int> OnObjectiveUpdated;
    public event System.Action<QuestInstance> OnQuestCompleted;
    public event System.Action<QuestInstance> OnQuestAccepted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool AcceptQuest(QuestData data)
    {
        if (data == null) return false;
        if (activeQuests.Any(q => q.data.questId == data.questId)) return false;
        if (!data.isRepeatable && completedQuestIds.Contains(data.questId)) return false;

        var instance = new QuestInstance(data, Time.time);
        activeQuests.Add(instance);
        OnQuestAccepted?.Invoke(instance);
        Debug.Log($"[QuestManager] 퀘스트 수주: {data.title}");
        return true;
    }

    public void UpdateObjective(ObjectiveType type, string targetId, int count = 1)
    {
        for (int q = activeQuests.Count - 1; q >= 0; q--)
        {
            var quest = activeQuests[q];
            if (quest.state != QuestState.Active) continue;

            for (int i = 0; i < quest.data.objectives.Length; i++)
            {
                var obj = quest.data.objectives[i];
                if (obj.type != type || obj.targetId != targetId) continue;
                if (quest.IsObjectiveComplete(i)) continue;

                quest.progress[i] = Mathf.Min(quest.progress[i] + count, obj.requiredCount);
                OnObjectiveUpdated?.Invoke(quest, i);
                Debug.Log($"[QuestManager] 목표 진행: {obj.description} ({quest.progress[i]}/{obj.requiredCount})");
            }

            if (quest.AreAllObjectivesComplete() && quest.state == QuestState.Active)
            {
                quest.state = QuestState.ReadyToReport;
                Debug.Log($"[QuestManager] 퀘스트 완료 가능: {quest.data.title}");
            }
        }
    }

    public bool CompleteQuest(string questId)
    {
        var quest = activeQuests.Find(q => q.data.questId == questId);
        if (quest == null || quest.state != QuestState.ReadyToReport) return false;

        quest.state = QuestState.Completed;
        GiveRewards(quest);
        completedQuestIds.Add(questId);
        activeQuests.Remove(quest);
        OnQuestCompleted?.Invoke(quest);
        Debug.Log($"[QuestManager] 퀘스트 완료: {quest.data.title}");
        return true;
    }

    void GiveRewards(QuestInstance quest)
    {
        foreach (var reward in quest.data.rewards)
        {
            switch (reward.type)
            {
                case QuestRewardType.Item:
                    var itemData = ItemDatabase.Get(reward.itemId);
                    if (itemData != null)
                    {
                        var player = GameObject.FindGameObjectWithTag("Player");
                        if (player != null)
                        {
                            var inv = player.GetComponent<PlayerInventory>();
                            if (inv != null)
                            {
                                var item = new ItemInstance(itemData, reward.amount);
                                inv.TryPickup(item);
                            }
                        }
                    }
                    break;
                case QuestRewardType.Currency:
                    if (CurrencyManager.Instance != null)
                        CurrencyManager.Instance.Add(reward.amount, $"퀘스트: {quest.data.title}");
                    break;
                case QuestRewardType.Affinity:
                case QuestRewardType.Trust:
                    if (NPCRelationshipManager.Instance != null)
                    {
                        var rel = NPCRelationshipManager.Instance.GetRelationship(reward.npcId);
                        if (rel != null)
                        {
                            if (reward.type == QuestRewardType.Affinity)
                                rel.affinity = Mathf.Min(100, rel.affinity + reward.amount);
                            else
                                rel.trust = Mathf.Min(100, rel.trust + reward.amount);
                        }
                    }
                    break;
            }
        }
    }

    public void FailRaidQuests()
    {
        for (int i = activeQuests.Count - 1; i >= 0; i--)
        {
            if (activeQuests[i].data.expiresOnRaid)
            {
                Debug.Log($"[QuestManager] 레이드 퀘스트 실패: {activeQuests[i].data.title}");
                activeQuests[i].state = QuestState.Failed;
                activeQuests.RemoveAt(i);
            }
        }
    }

    public List<QuestData> GetAvailableQuests(string npcId, QuestData[] allQuests)
    {
        var available = new List<QuestData>();
        foreach (var q in allQuests)
        {
            if (q.giverNpcId != npcId) continue;
            if (activeQuests.Any(a => a.data.questId == q.questId)) continue;
            if (!q.isRepeatable && completedQuestIds.Contains(q.questId)) continue;
            if (!CheckCondition(q.unlockCondition, npcId)) continue;
            available.Add(q);
        }
        return available;
    }

    public QuestInstance GetReportableQuest(string npcId)
    {
        return activeQuests.Find(q =>
            q.state == QuestState.ReadyToReport &&
            q.data.giverNpcId == npcId);
    }

    bool CheckCondition(QuestCondition cond, string npcId)
    {
        if (cond == null) return true;

        if (!string.IsNullOrEmpty(cond.requiredQuest) && !completedQuestIds.Contains(cond.requiredQuest))
            return false;

        if (!string.IsNullOrEmpty(cond.requiredFlag) && (!flags.ContainsKey(cond.requiredFlag) || !flags[cond.requiredFlag]))
            return false;

        if (cond.requiredAffinity > 0 || cond.requiredTrust > 0)
        {
            if (NPCRelationshipManager.Instance != null)
            {
                var rel = NPCRelationshipManager.Instance.GetRelationship(npcId);
                if (rel == null) return false;
                if (rel.affinity < cond.requiredAffinity) return false;
                if (rel.trust < cond.requiredTrust) return false;
            }
        }

        return true;
    }

    public void SetFlag(string flag, bool value = true)
    {
        flags[flag] = value;
    }

    public bool GetFlag(string flag)
    {
        return flags.ContainsKey(flag) && flags[flag];
    }

    /// <summary>
    /// 세이브용: 모든 플래그 키 목록 반환.
    /// </summary>
    public IEnumerable<string> GetAllFlagKeys()
    {
        return flags.Keys;
    }

    /// <summary>
    /// 세이브용: 모든 플래그를 딕셔너리로 반환.
    /// </summary>
    public Dictionary<string, bool> GetAllFlags()
    {
        return new Dictionary<string, bool>(flags);
    }
}
