using UnityEngine;

public enum QuestType
{
    Collect,    // 아이템 수집
    Kill,       // 적 처치
    Explore,    // 지점 도달
    Deliver,    // 아이템 전달
}

public enum ObjectiveType
{
    CollectItem,
    KillEnemy,
    ReachPoint,
    TalkToNPC,
}

public enum QuestRewardType
{
    Item,
    Currency,
    Affinity,
    Trust,
}

public enum QuestState
{
    Available,
    Active,
    ReadyToReport,
    Completed,
    Failed,
}

[System.Serializable]
public class QuestObjective
{
    public ObjectiveType type;
    [Tooltip("아이템ID / 적ID / 지점ID / NPC ID")]
    public string targetId;
    public int requiredCount = 1;
    [Tooltip("목표 설명 (예: 붕대 3개 수집)")]
    public string description;
}

[System.Serializable]
public class QuestReward
{
    public QuestRewardType type;
    [Tooltip("Item 보상일 때 아이템 ID")]
    public string itemId;
    public int amount = 1;
    [Tooltip("Affinity/Trust 보상 대상 NPC")]
    public string npcId;
}

[System.Serializable]
public class QuestCondition
{
    [Tooltip("퀘스트 제공 NPC의 최소 호감도")]
    public int requiredAffinity;
    [Tooltip("최소 신뢰도")]
    public int requiredTrust;
    [Tooltip("선행 퀘스트 ID (완료 필수)")]
    public string requiredQuest;
    [Tooltip("범용 플래그")]
    public string requiredFlag;
}

[CreateAssetMenu(fileName = "NewQuest", menuName = "Dev Tools/Content/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("기본 정보")]
    public string questId;
    public string title;
    [TextArea(2, 5)]
    public string description;
    public QuestType questType;

    [Header("NPC")]
    [Tooltip("퀘스트 제공 NPC ID")]
    public string giverNpcId;

    [Header("목표")]
    public QuestObjective[] objectives;

    [Header("보상")]
    public QuestReward[] rewards;

    [Header("해금 조건")]
    public QuestCondition unlockCondition;

    [Header("설정")]
    [Tooltip("수행 지역 (빈값이면 아무 곳)")]
    public string region;
    public bool isRepeatable;
    [Tooltip("레이드 1회 한정 (레이드 NPC 퀘스트)")]
    public bool expiresOnRaid;
}
