using UnityEngine;

/// <summary>
/// 문 컨트롤러.
/// 잠금 해제 방식: 열쇠 아이템, 퀘스트 조건, 스위치, 또는 잠금 없음.
/// 물리 차단: 콜라이더로 통과 방지, 열리면 비활성화.
/// InteractableObject(Door) + 이 컴포넌트를 함께 부착.
/// </summary>
public class DoorController : MonoBehaviour
{
    public enum LockType
    {
        None,           // 잠금 없음 (바로 열림)
        Key,            // 열쇠 아이템 필요
        Quest,          // 특정 퀘스트 완료 필요
        Switch,         // 다른 오브젝트(스위치/레버)로 열림
    }

    public enum DoorState
    {
        Locked,
        Unlocked,
        Open,
    }

    [Header("잠금 설정")]
    [SerializeField] LockType lockType = LockType.Key;

    [Header("열쇠 (Key 타입)")]
    [Tooltip("필요한 열쇠의 ItemDatabase ID")]
    [SerializeField] string requiredKeyId;
    [Tooltip("열쇠 사용 시 소모할지")]
    [SerializeField] bool consumeKey = true;

    [Header("퀘스트 (Quest 타입)")]
    [Tooltip("완료되어야 하는 퀘스트 ID")]
    [SerializeField] string requiredQuestId;

    [Header("문 물리")]
    [Tooltip("통과 차단용 콜라이더 (문 자체 또는 별도 오브젝트)")]
    [SerializeField] Collider doorCollider;
    [Tooltip("열릴 때 비활성화할 비주얼 오브젝트 (문짝 메쉬/스프라이트)")]
    [SerializeField] GameObject doorVisual;

    [Header("연결")]
    [Tooltip("문 열릴 때 활성화할 오브젝트 (방 내부, 숨겨진 통로 등)")]
    [SerializeField] GameObject[] activateOnOpen;
    [Tooltip("문 열릴 때 비활성화할 오브젝트")]
    [SerializeField] GameObject[] deactivateOnOpen;

    [Header("메시지")]
    [SerializeField] string lockedMessage = "잠겨 있다...";
    [SerializeField] string needKeyMessage = "열쇠가 필요하다";
    [SerializeField] string needQuestMessage = "아직 열 수 없다";
    [SerializeField] string openMessage = "문을 열었다";

    DoorState state = DoorState.Locked;
    InteractableObject interactable;

    public DoorState State => state;
    public bool IsOpen => state == DoorState.Open;
    public LockType Lock => lockType;

    /// <summary>외부에서 문 열림 감지</summary>
    public event System.Action<DoorController> OnDoorOpened;

    void Awake()
    {
        interactable = GetComponent<InteractableObject>();

        // 잠금 없음이면 바로 Unlocked
        if (lockType == LockType.None)
            state = DoorState.Unlocked;

        // 콜라이더 자동 탐색
        if (doorCollider == null)
        {
            doorCollider = GetComponent<Collider>();
            // trigger 아닌 콜라이더 우선
            var cols = GetComponents<Collider>();
            foreach (var c in cols)
            {
                if (!c.isTrigger)
                {
                    doorCollider = c;
                    break;
                }
            }
        }
    }

    /// <summary>플레이어가 문을 열려고 시도</summary>
    public void TryOpen(PlayerController player)
    {
        if (state == DoorState.Open)
        {
            Debug.Log("[Door] 이미 열려 있음");
            return;
        }

        // 잠금 해제 시도
        if (state == DoorState.Locked)
        {
            if (!TryUnlock(player))
                return; // 잠금 해제 실패
        }

        // Unlocked → Open
        Open();
    }

    /// <summary>잠금 해제 시도. 성공하면 true.</summary>
    bool TryUnlock(PlayerController player)
    {
        switch (lockType)
        {
            case LockType.None:
                state = DoorState.Unlocked;
                return true;

            case LockType.Key:
                return TryUnlockWithKey(player);

            case LockType.Quest:
                return TryUnlockWithQuest();

            case LockType.Switch:
                // 스위치는 외부에서 Unlock() 호출
                ShowMessage(lockedMessage);
                return false;

            default:
                return false;
        }
    }

    bool TryUnlockWithKey(PlayerController player)
    {
        if (string.IsNullOrEmpty(requiredKeyId))
        {
            Debug.LogWarning("[Door] requiredKeyId가 비어있음");
            state = DoorState.Unlocked;
            return true;
        }

        var inventory = player.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            ShowMessage(needKeyMessage);
            return false;
        }

        // 열쇠 보유 확인
        var keyItem = inventory.Grid.FindItem(requiredKeyId);
        if (keyItem == null)
        {
            // 열쇠 이름 표시
            var keyData = ItemDatabase.Get(requiredKeyId);
            string keyName = keyData != null ? keyData.displayName : requiredKeyId;
            ShowMessage($"{keyName}(이)가 필요하다");
            return false;
        }

        // 열쇠 소모
        if (consumeKey)
        {
            inventory.Grid.ConsumeItem(requiredKeyId, 1);
            var keyData = ItemDatabase.Get(requiredKeyId);
            string keyName = keyData != null ? keyData.displayName : requiredKeyId;
            Debug.Log($"[Door] {keyName} 사용 (소모됨)");
        }

        state = DoorState.Unlocked;
        return true;
    }

    bool TryUnlockWithQuest()
    {
        if (string.IsNullOrEmpty(requiredQuestId))
        {
            state = DoorState.Unlocked;
            return true;
        }

        // QuestManager 연동
        if (QuestManager.Instance != null && QuestManager.Instance.CompletedQuestIds.Contains(requiredQuestId))
        {
            state = DoorState.Unlocked;
            return true;
        }

        ShowMessage(needQuestMessage);
        return false;
    }

    /// <summary>외부에서 잠금 해제 (스위치, 이벤트 등)</summary>
    public void Unlock()
    {
        if (state == DoorState.Open) return;
        state = DoorState.Unlocked;
        Debug.Log($"[Door] {gameObject.name} 잠금 해제됨");

        // 프롬프트 갱신
        UpdatePrompt();
    }

    /// <summary>문 열기</summary>
    void Open()
    {
        state = DoorState.Open;
        ShowMessage(openMessage);

        // 콜라이더 비활성화 (통과 가능)
        if (doorCollider != null)
            doorCollider.enabled = false;

        // 문 비주얼 숨기기
        if (doorVisual != null)
            doorVisual.SetActive(false);

        // 연결 오브젝트 활성/비활성
        if (activateOnOpen != null)
            foreach (var go in activateOnOpen)
                if (go != null) go.SetActive(true);

        if (deactivateOnOpen != null)
            foreach (var go in deactivateOnOpen)
                if (go != null) go.SetActive(false);

        // 상호작용 비활성화 (다시 닫을 수 없는 문)
        if (interactable != null)
        {
            // oneShot 처리는 InteractableObject에서
        }

        OnDoorOpened?.Invoke(this);
        Debug.Log($"[Door] {gameObject.name} 열림");
    }

    void UpdatePrompt()
    {
        if (interactable == null) return;

        // 리플렉션으로 promptText 갱신
        var field = typeof(InteractableObject).GetField("promptText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            if (state == DoorState.Unlocked)
                field.SetValue(interactable, "문 열기");
            else if (state == DoorState.Locked)
                field.SetValue(interactable, lockedMessage);
        }
    }

    void ShowMessage(string msg)
    {
        // TODO: 화면에 토스트 메시지 표시 (현재는 콘솔만)
        Debug.Log($"[Door] {msg}");
    }

    #region 기즈모

    void OnDrawGizmosSelected()
    {
        Color col = state == DoorState.Open ? Color.green
                  : state == DoorState.Unlocked ? Color.yellow
                  : Color.red;
        Gizmos.color = col;

        if (doorCollider != null)
        {
            Gizmos.matrix = doorCollider.transform.localToWorldMatrix;
            if (doorCollider is BoxCollider box)
                Gizmos.DrawWireCube(box.center, box.size);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(1f, 1f, 0.2f));
        }
    }

    #endregion
}
