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
        HierarchyFolder.Persist(gameObject);
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
        SaveCheckpoints.Instance?.QuestAccepted();
        return true;
    }

    /// <summary>이 (타입,대상)을 필요로 하는 '진행 중이고 미완료'인 목표가 있는가 —
    /// POI 존 등이 진행 피드백(토스트) 여부를 결정할 때 사용.</summary>
    public bool HasActiveObjective(ObjectiveType type, string targetId)
    {
        foreach (var quest in activeQuests)
        {
            if (quest.state != QuestState.Active) continue;
            for (int i = 0; i < quest.data.objectives.Length; i++)
            {
                var obj = quest.data.objectives[i];
                if (obj.type == type && obj.targetId == targetId && !quest.IsObjectiveComplete(i))
                    return true;
            }
        }
        return false;
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

    /// <summary>
    /// 퀘스트 완료 처리.
    /// force=true면 목표 진행도(ReadyToReport)와 무관하게 강제 완료한다 —
    /// 스토리 스크립트(StoryPlayer)가 내러티브상 완료를 확정하는 경우용.
    /// NPC 보고 흐름은 force=false(기본)로 ReadyToReport 게이트를 유지.
    /// </summary>
    public bool CompleteQuest(string questId, bool force = false)
    {
        var quest = activeQuests.Find(q => q.data.questId == questId);
        if (quest == null) return false;
        if (!force && quest.state != QuestState.ReadyToReport) return false;

        quest.state = QuestState.Completed;
        GiveRewards(quest);
        completedQuestIds.Add(questId);
        activeQuests.Remove(quest);
        GrantReputation(quest);   // 평판 적립 (quests-region1 §9.2, 값 SSOT: reputation.csv) — 완료 상태 확정 후
        OnQuestCompleted?.Invoke(quest);
        Debug.Log($"[QuestManager] 퀘스트 완료: {quest.data.title}");
        SaveCheckpoints.Instance?.QuestCompleted();
        return true;
    }

    /// <summary>퀘스트 완료 → 평판 적립 배선 (quests-region1 §9.2).
    /// questId → reputation.csv 액션 키 매핑. 미매핑/값 0(BD 일반·DQ 일반)은 무시.
    /// 루디 납품 계열은 목표 requiredCount(에셋)만큼 배수 — 수량 하드코딩 금지(에셋과 이중 소스 방지).</summary>
    void GrantReputation(QuestInstance quest)
    {
        if (ReputationManager.Instance == null || quest?.data == null) return;
        string questId = quest.data.questId;

        // 루디 납품 계열 — 개당 +1 × 납품 수량(에셋 requiredCount). 토스트 스팸 방지 위해 합산 1회 Add.
        if (questId == "BD-17" || questId == "DQ-006" || questId == "DQ-007")
        {
            int per = ReputationActions.Value("rudi_deliver");
            int count = quest.data.objectives != null && quest.data.objectives.Length > 0
                ? Mathf.Max(1, quest.data.objectives[0].requiredCount) : 1;
            if (per != 0)
                ReputationManager.Instance.Add(per * count, $"루디 납품 ×{count} ({questId})");
            return;
        }

        string actionKey = questId switch
        {
            "MQ-001" => "MQ-001_report",
            "MQ-002" => "MQ-002_done",
            "SQ-001" => "SQ-001_rescue",
            "SQ-002" => "SQ-002_done",
            _ => null,
        };
        // BQ 등급별 반복 적립 (BQ-E01 → BQ-E_done) — ID 포맷 "BQ-{등급대문자}nn" 전제
        if (actionKey == null && questId.StartsWith("BQ-") && questId.Length >= 4)
        {
            actionKey = $"BQ-{questId[3]}_done";
            if (ReputationActions.Value(actionKey) == 0)
                Debug.LogWarning($"[QuestManager] BQ 평판 키 매핑 실패({questId} → {actionKey}) — reputation.csv/ID 포맷 확인");
        }

        if (actionKey != null)
            ReputationManager.Instance.AddByAction(actionKey, $"의뢰 완료: {questId}");
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
                case QuestRewardType.Recipe:
                    // itemId = 해금할 recipeId
                    if (CraftingSystem.Instance != null && !string.IsNullOrEmpty(reward.itemId))
                    {
                        bool ok = CraftingSystem.Instance.UnlockRecipe(reward.itemId);
                        if (ok) ToastManager.Show($"새 레시피 해금: {reward.itemId}", ToastManager.ToastType.Success);
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

    /// <summary>새 게임 — 진행 상태 전체 초기화(활성/완료 퀘스트·플래그). SaveManager.ResetToNewGame용.</summary>
    public void ResetForNewGame()
    {
        activeQuests.Clear();
        completedQuestIds.Clear();
        flags.Clear();
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
