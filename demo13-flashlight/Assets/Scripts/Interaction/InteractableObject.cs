using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 상호작용 가능한 오브젝트 기본 컴포넌트.
/// 씬에 배치 후 타입/프롬프트/범위 설정.
/// 정적 리스트로 관리 — FindObjectsOfType 불필요.
/// </summary>
public class InteractableObject : MonoBehaviour, IInteractable
{
    #region 정적 등록

    public static readonly List<InteractableObject> All = new List<InteractableObject>();

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    #endregion

    #region 열거형

    public enum InteractType
    {
        Generic,        // 범용 (이벤트만 발생)
        ExitPoint,      // 탈출구 (필드→안전가옥) / 진입구 (안전가옥→필드)
        Container,      // 상자/서랍 (루팅)
        NPC,            // NPC (대화)
        Note,           // 쪽지/문서 (읽기)
        Pickup,         // 바닥 아이템 (줍기)
        Bed,            // 침대 (휴식/저장)
        Workbench,      // 작업대 (무기 제작/수리)
        MapBoard,       // 지도판 (출전 선택) — 값 8 유지 (기존 씬 직렬화)
        MedicalBench,   // 의료대 (일회용 치료템 제작)
        CookingBench,   // 조리대 (버프 음식 제작)
        Door,           // 문 (잠금/열쇠/조건부)
    }

    #endregion

    #region 필드

    [Header("── 상호작용 기본 ──")]
    [Tooltip("오브젝트 종류. 타입에 따라 아래 세부 설정이 달라짐")]
    [SerializeField] InteractType type = InteractType.Generic;

    [Tooltip("플레이어에게 표시될 프롬프트 텍스트 (예: 조사하기, 줍기, 탈출하기)")]
    [SerializeField] string promptText = "조사하기";

    [Tooltip("플레이어가 이 거리 안에 있어야 상호작용 가능 (단위: 미터, XZ 평면)")]
    [SerializeField] float interactRange = 2f;

    [Tooltip("true면 한 번만 사용 가능 (줍기, 쪽지 등). false면 반복 사용")]
    [SerializeField] bool oneShot = false;

    [Header("── 비주얼 ──")]
    [Tooltip("범위 내 진입 시 스프라이트에 적용되는 하이라이트 색")]
    [SerializeField] Color highlightColor = new Color(1f, 1f, 0.5f, 1f);

    [Header("── 탈출구/진입구 (ExitPoint 전용) ──")]
    [Tooltip("이동할 씬 이름 (Build Settings에 등록된 이름과 동일해야 함)")]
    [SerializeField] string targetScene;

    [Tooltip("도착 씬에서 플레이어가 스폰될 SpawnPoint의 ID")]
    [SerializeField] string spawnPointId;

    [Tooltip("탈출 대기 시간(초). 0이면 즉시 전환, 8이면 8초 대기 후 전환")]
    [SerializeField] float exitWaitTime = 0f;

    [Header("── 쪽지 (Note 전용) ──")]
    [Tooltip("읽을 때 표시될 쪽지 내용")]
    [SerializeField] [TextArea(3, 10)] string noteContent;

    [Tooltip("이 쪽지에 연결된 스토리 씬 ID (비어있으면 noteContent를 직접 표시)")]
    [SerializeField] string noteStorySceneId;

    [Header("── NPC (스토리 연동) ──")]
    [Tooltip("스토리 트리거에 사용할 NPC ID (pawnshop, merchant 등)")]
    [SerializeField] string storyNpcId;

    [Header("── 줍기 (Pickup 전용) ──")]
    [Tooltip("인벤토리에 추가될 아이템 ID (ItemDatabase 기준)")]
    [SerializeField] string itemId;

    [Tooltip("줍는 수량")]
    [SerializeField] int itemCount = 1;

    bool used;
    SpriteRenderer spriteRenderer;
    Color originalColor = Color.white;
    bool isHighlighted;

    #endregion

    #region 프로퍼티 (IInteractable)

    public string PromptText => promptText;
    public bool CanInteract => !used || !oneShot;
    public float InteractRange => interactRange;
    public InteractType Type => type;
    public string TargetScene => targetScene;
    public string NoteContent => noteContent;
    public string ItemId => itemId;
    public int ItemCount => itemCount;

    /// <summary>상호작용 시 외부에서 구독 가능한 이벤트</summary>
    // TODO(TopDownPlayer): public event System.Action<PlayerController> OnInteracted;
    public event System.Action<GameObject> OnInteracted;

    #endregion

    #region 유니티 라이프사이클

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    void Start()
    {
        // 씬 배치 Pickup인데 WorldItem이 없으면 (인스펙터 설정) 이름 라벨 생성
        if (type == InteractType.Pickup && GetComponent<WorldItem>() == null && !string.IsNullOrEmpty(itemId))
        {
            var data = ItemDatabase.Get(itemId);
            if (data != null)
            {
                string name = itemCount > 1 ? $"{data.displayName} x{itemCount}" : data.displayName;
                promptText = $"줍기: {name}";
                // 이름 라벨
                CreatePickupLabel(data.displayName, data.RarityColor);
            }
        }
    }

    void CreatePickupLabel(string labelName, Color color)
    {
        var labelGO = new GameObject("PickupLabel");
        labelGO.transform.SetParent(transform, false);
        labelGO.transform.localPosition = new Vector3(0, 0.5f, 0);

        var canvas = labelGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        var canvasRT = labelGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(2f, 0.4f);
        canvasRT.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        var bgGO = new GameObject("Bg");
        bgGO.transform.SetParent(canvasRT, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bgGO.AddComponent<UnityEngine.UI.Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.75f);

        var textGO = new GameObject("Name");
        textGO.transform.SetParent(canvasRT, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(4, 0);
        textRT.offsetMax = new Vector2(-4, 0);

        var txt = textGO.AddComponent<UnityEngine.UI.Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 18;
        txt.fontStyle = FontStyle.Bold;
        txt.color = color;
        txt.text = labelName;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        textGO.AddComponent<UnityEngine.UI.Shadow>().effectColor = Color.black;
        labelGO.AddComponent<WorldLabelBillboard>();
    }

    #endregion

    #region 상호작용 실행

    // TODO(TopDownPlayer): public void Interact(PlayerController player)
    public void Interact(GameObject playerGO)
    {
        if (!CanInteract) return;

        if (oneShot) used = true;

        // 이벤트 발행
        OnInteracted?.Invoke(playerGO);

        // 타입별 기본 처리
        switch (type)
        {
            case InteractType.ExitPoint:
                HandleExit(playerGO);
                break;
            case InteractType.Note:
                HandleNote(playerGO);
                break;
            case InteractType.Pickup:
                HandlePickup(playerGO);
                break;
            case InteractType.Container:
                HandleContainer(playerGO);
                break;
            case InteractType.Workbench:
                HandleCrafting(CraftingStation.Workbench);
                break;
            case InteractType.MedicalBench:
                HandleCrafting(CraftingStation.MedicalBench);
                break;
            case InteractType.CookingBench:
                HandleCrafting(CraftingStation.CookingBench);
                break;
            case InteractType.MapBoard:
                HandleMapBoard(playerGO);
                break;
            case InteractType.NPC:
                HandleNPC(playerGO);
                break;
            case InteractType.Bed:
                HandleBed(playerGO);
                break;
            case InteractType.Door:
                HandleDoor(playerGO);
                break;
            default:
                Debug.Log($"[Interact] {type}: {promptText}");
                break;
        }
    }

    // TODO(TopDownPlayer): void HandleExit(PlayerController player)
    void HandleExit(GameObject playerGO)
    {
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("[ExitPoint] targetScene이 비어있음");
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[ExitPoint] SceneTransitionManager가 씬에 없습니다.");
            return;
        }

        // RaidManager에 탈출 성공 알림
        if (RaidManager.Instance != null)
            RaidManager.Instance.OnExtractSuccess();

        if (exitWaitTime > 0)
        {
            Debug.Log($"[ExitPoint] 탈출 대기 {exitWaitTime}초...");
            SceneTransitionManager.Instance.TransitionWithDelay(targetScene, spawnPointId, exitWaitTime, transform, interactRange * 2f);
        }
        else
        {
            SceneTransitionManager.Instance.TransitionTo(targetScene, spawnPointId);
        }
    }

    // TODO(TopDownPlayer): void HandleNote(PlayerController player)
    void HandleNote(GameObject playerGO)
    {
        if (StoryTriggerManager.Instance != null)
        {
            StoryTriggerManager.Instance.OnNoteRead(noteContent, noteStorySceneId);
        }
        else if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.ShowStoryDialogue("", new[] { noteContent }, null);
        }
        else
        {
            Debug.Log($"[Note] {noteContent}");
        }
    }

    // TODO(TopDownPlayer): void HandleBed(PlayerController player)
    void HandleBed(GameObject playerGO)
    {
        if (StoryTriggerManager.Instance != null)
        {
            StoryTriggerManager.Instance.OnBedRest();
        }
        else
        {
            // 폴백: HP만 회복
            var health = playerGO.GetComponent<Health>();
            if (health != null) health.Heal(health.MaxHp);
            Debug.Log("[Bed] 휴식 완료 (HP 회복)");
        }
    }

    // TODO(TopDownPlayer): void HandlePickup(PlayerController player)
    void HandlePickup(GameObject playerGO)
    {
        // WorldItem이 있으면 인벤토리 연동
        var worldItem = GetComponent<WorldItem>();
        if (worldItem != null)
        {
            if (worldItem.TryPickup(playerGO))
                return; // 성공 시 WorldItem이 Destroy 처리
            else
                return; // 공간 부족
        }

        // WorldItem 없으면 ItemDatabase에서 생성
        var data = ItemDatabase.Get(itemId);
        if (data != null)
        {
            var inventory = playerGO.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                var item = new ItemInstance(data, itemCount);
                if (inventory.TryPickup(item))
                {
                    // RaidManager에 루트 기록
                    if (RaidManager.Instance != null)
                        RaidManager.Instance.TrackLoot(item);

                    // 퀘스트 목표 갱신
                    if (QuestManager.Instance != null)
                        QuestManager.Instance.UpdateObjective(ObjectiveType.CollectItem, data.itemId, itemCount);

                    Debug.Log($"[Pickup] {data.displayName} x{itemCount} 획득");

                    // 스토리 트리거: 첫 파밍, 루디 획득
                    if (StoryTriggerManager.Instance != null)
                    {
                        StoryTriggerManager.Instance.OnItemLooted();
                        if (data.itemId == "rudi_shard" || data.itemId == "rudi")
                            StoryTriggerManager.Instance.OnRudiPickup();
                    }

                    if (oneShot) gameObject.SetActive(false);
                }
                else
                {
                    Debug.Log("[Pickup] 인벤토리 공간 부족");
                }
            }
        }
        else
        {
            Debug.Log($"[Pickup] {itemId} x{itemCount} (ItemDatabase 미등록)");
            if (oneShot) gameObject.SetActive(false);
        }
    }

    // TODO(TopDownPlayer): void HandleContainer(PlayerController player)
    void HandleContainer(GameObject playerGO)
    {
        // 안전가옥 창고 우선 체크
        var storage = GetComponent<SafehouseStorage>();
        if (storage != null)
        {
            storage.Open();
            return;
        }

        // 일반 루팅 상자
        var container = GetComponent<LootContainer>();
        if (container != null)
        {
            container.Open(playerGO);
            if (UIManager.Instance != null)
                UIManager.Instance.ShowCharacterPanelWithContainer(container);
        }
        else
        {
            Debug.Log($"[Container] {promptText} (LootContainer/SafehouseStorage 없음)");
        }
    }

    void HandleCrafting(CraftingStation station)
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowCrafting(station);
        else
            Debug.LogWarning($"[{station}] UIManager가 없습니다.");
    }

    // TODO(TopDownPlayer): void HandleNPC(PlayerController player)
    void HandleNPC(GameObject playerGO)
    {
        var npc = GetComponent<NPCController>();
        if (npc != null)
            npc.Talk(playerGO);
        else
            Debug.Log($"[NPC] {promptText} (NPCController 없음)");
    }

    // TODO(TopDownPlayer): void HandleDoor(PlayerController player)
    void HandleDoor(GameObject playerGO)
    {
        var door = GetComponent<DoorController>();
        if (door != null)
            door.TryOpen(playerGO);
        else
            Debug.Log($"[Door] {promptText} (DoorController 없음)");
    }

    // TODO(TopDownPlayer): void HandleMapBoard(PlayerController player)
    void HandleMapBoard(GameObject playerGO)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMapSelect();
        }
        else
        {
            Debug.LogWarning("[MapBoard] UIManager가 없습니다.");
        }
    }

    #endregion

    #region 외부 설정

    /// <summary>런타임/부트스트랩에서 타입·프롬프트 설정</summary>
    public void Configure(InteractType interactType, string prompt, float range = 1.5f, bool once = false)
    {
        type = interactType;
        promptText = prompt;
        interactRange = range;
        oneShot = once;
    }

    /// <summary>코드에서 Pickup 타입으로 설정 (WorldItem.Drop에서 사용)</summary>
    public void SetupAsPickup(string id, int count, string prompt)
    {
        type = InteractType.Pickup;
        itemId = id;
        itemCount = count;
        promptText = prompt;
        oneShot = true;
        interactRange = 1.5f;
    }

    #endregion

    #region 하이라이트

    public void SetHighlight(bool on)
    {
        isHighlighted = on;
        if (spriteRenderer == null) return;
        spriteRenderer.color = on ? highlightColor : originalColor;
    }

    #endregion

    #region 기즈모

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }

    #endregion
}
