using UnityEngine;

[RequireComponent(typeof(InteractableObject))]
public class NPCController : MonoBehaviour
{
    [SerializeField] NPCData npcData;

    public NPCData Data => npcData;

    public void Talk(PlayerController player)
    {
        if (npcData == null)
        {
            Debug.LogWarning("[NPCController] NPCData가 없음");
            return;
        }

        if (DialogueUI.Instance != null)
            DialogueUI.Instance.StartDialogue(npcData, player);
    }
}
