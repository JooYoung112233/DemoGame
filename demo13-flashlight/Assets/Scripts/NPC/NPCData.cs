using UnityEngine;

[System.Serializable]
public class DialogueCondition
{
    public int minAffinity;
    public int minTrust;
    public int minFear;
    public string requiredQuest;
    public string requiredFlag;
}

[System.Serializable]
public class DialogueEntry
{
    public string id;
    [TextArea(1, 3)]
    public string[] lines;
    public DialogueCondition conditions;
    public int priority;
}

[System.Serializable]
public class DialogueChoice
{
    public string text;
    public int affinityChange;
    public int trustChange;
    public int fearChange;
    [Tooltip("전역 평판(ReputationManager) 변동")]
    public int reputationChange;
    [TextArea(1, 3)]
    public string[] resultLines;
    [Tooltip("선택 시 수주할 퀘스트 ID")]
    public string triggerQuest;
    public string setFlag;
    [Tooltip("선택 시 이 NPC의 상점(거래) 열기")]
    public bool openShop;
}

[System.Serializable]
public class EventDialogue
{
    public string id;
    public DialogueCondition triggerCondition;
    [TextArea(1, 3)]
    public string[] npcLines;
    public DialogueChoice[] choices;
    [Tooltip("true면 1회만 발생")]
    public bool oneShot;
}

[CreateAssetMenu(fileName = "NewNPC", menuName = "Dev Tools/Content/NPC Data")]
public class NPCData : ScriptableObject
{
    [Header("기본 정보")]
    public string npcId;
    public string displayName;
    public string role;

    [Header("관계 초기값 (첫 대면 시 1회 시드)")]
    [Range(0, 100)] public int initialAffinity = 20;
    [Range(0, 100)] public int initialTrust = 10;
    [Range(0, 100)] public int initialFear = 0;

    [Header("대화")]
    public DialogueEntry[] defaultDialogues;
    public EventDialogue[] eventDialogues;

    [Header("상점 (거래)")]
    [Tooltip("이 NPC가 상점이면 ShopData 할당 (전당포 등). 대화 선택지 openShop으로 진입")]
    public ShopData shopData;

    [Header("퀘스트")]
    [Tooltip("이 NPC가 제공 가능한 퀘스트 목록")]
    public QuestData[] availableQuests;

    [Header("상점 (선택)")]
    public ItemData[] shopInventory;
}
