using UnityEngine;

public enum EventRewardType
{
    Item,
    Currency,
    Heal,
    Affinity,
    Trust,
}

public enum EventPenaltyType
{
    LoseItem,
    LoseCurrency,
    Damage,
    ConsumeItem,   // 2026-09-12 — 특정 아이템 소모(itemId × amount). 모자라면 그 선택지는 아무것도 적용되지 않는다.
}

[System.Serializable]
public class EventReward
{
    public EventRewardType type;
    [Tooltip("Item일 때. 'a|b|c'로 적으면 그중 하나를 무작위로 준다.")]
    public string itemId;
    public int amount;
    [Tooltip("Affinity/Trust 대상 NPC")]
    public string npcId;
}

[System.Serializable]
public class EventPenalty
{
    public EventPenaltyType type;
    public int amount;
    [Tooltip("ConsumeItem일 때 소모할 아이템 id")]
    public string itemId;
}

[System.Serializable]
public class EventChoice
{
    [Tooltip("선택지 텍스트")]
    public string text;
    [TextArea(1, 3)]
    public string resultText;
    public EventReward[] rewards;
    public EventPenalty[] penalties;
}

[System.Serializable]
public class EventCondition
{
    [Tooltip("최소 생존 시간 (초)")]
    public float minRaidTime;
    [Tooltip("특정 지역에서만")]
    public string requiredRegion;
    [Tooltip("최소 획득 아이템 수")]
    public int minLootCount;
    [Tooltip("밤에만 발생")]
    public bool nightOnly;
}

[CreateAssetMenu(fileName = "NewPostRaidEvent", menuName = "Dev Tools/Content/Post Raid Event")]
public class PostRaidEventData : ScriptableObject
{
    [Header("기본 정보")]
    public string eventId;
    public string title;
    [TextArea(2, 5)]
    public string description;

    [Header("선택지")]
    public EventChoice[] choices;

    [Header("발생 조건")]
    public EventCondition conditions;

    [Header("설정")]
    [Tooltip("선택 가중치 (높을수록 자주)")]
    public float weight = 1f;
    [Tooltip("true면 1회만 발생")]
    public bool oneShot;
}
