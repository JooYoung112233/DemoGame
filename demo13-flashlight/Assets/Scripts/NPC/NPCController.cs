using UnityEngine;

[RequireComponent(typeof(InteractableObject))]
public class NPCController : MonoBehaviour
{
    [SerializeField] NPCData npcData;

    public NPCData Data => npcData;

    [Tooltip("스토리 트리거에 사용할 NPC ID (pawnshop, merchant 등). 비어있으면 npcData.npcId 사용")]
    [SerializeField] string storyNpcId;

    NPCQuestMarker questMarker;

    void Start()
    {
        // 퀘스트 마커 자동 부착
        questMarker = GetComponent<NPCQuestMarker>();
        if (questMarker == null)
            questMarker = gameObject.AddComponent<NPCQuestMarker>();
    }

    public void Talk(PlayerController player)
    {
        if (npcData == null)
        {
            Debug.LogWarning("[NPCController] NPCData가 없음");
            return;
        }

        // 스토리 씬 우선 체크 (첫 대면 등)
        string npcId = !string.IsNullOrEmpty(storyNpcId) ? storyNpcId : npcData.npcId;
        if (StoryTriggerManager.Instance != null &&
            StoryTriggerManager.Instance.TryPlayNPCStoryScene(npcId, () =>
            {
                // 스토리 씬 완료 후 마커 갱신
                if (questMarker != null)
                    questMarker.RefreshState();
            }))
        {
            return; // 스토리 씬이 재생 중이므로 일반 대화 스킵
        }

        if (DialogueUI.Instance != null)
            DialogueUI.Instance.StartDialogue(npcData, player);

        // 대화 후 마커 갱신 (퀘스트 수주 등으로 상태 변경 가능)
        if (questMarker != null)
            StartCoroutine(DelayedMarkerRefresh());
    }

    System.Collections.IEnumerator DelayedMarkerRefresh()
    {
        // DialogueUI가 닫힐 때까지 대기
        yield return null;
        while (DialogueUI.Instance != null && DialogueUI.Instance.IsShowing)
            yield return null;

        yield return null; // 1프레임 여유
        if (questMarker != null)
            questMarker.RefreshState();
    }
}
