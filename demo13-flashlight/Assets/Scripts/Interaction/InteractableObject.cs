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
        Stash,          // 창고 (메인 보관함 — 인벤 우측 열) ※ 끝에 추가(기존 직렬화 인덱스 보존)
        Radio,          // 라디오 (정보 수신 — RadioUI) ※ 끝에 추가
        Dispatch,       // [폐기 2026-09-09] 파견 보드 — 시스템 삭제됨. 뒤 항목 직렬화 인덱스 보존용 예약 슬롯
        Generator,      // 발전기 (전력 ON/OFF — HideoutUI generator) ※ 끝에 추가(직렬화 인덱스 보존)
        Passage,        // 막힌 통로 (철거/열쇠/조건부 — BlockedPassage) ※ 끝에 추가(직렬화 인덱스 보존)
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

    [Tooltip("쪽지 UI 상단 제목 (비어있으면 '쪽지')")]
    [SerializeField] string noteTitle;

    [Tooltip("읽으면 얻는 **지식** 플래그 id (예: code_jewelry_vault). 아이템이 아니라 플래그라 죽어도 안 잃는다.\n" +
             "BlockedPassage(Code)가 이 값을 확인한다. 비우면 그냥 읽는 쪽지.")]
    [SerializeField] string grantsKnowledgeId;

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
        // 탑다운 2D: 카메라 고정이라 빌보드 불필요
    }

    #endregion

    #region 상호작용 실행

    public void Interact(GameObject playerGO)
    {
        if (!CanInteract) return;

        // Pickup은 '성공'해야 소진 — 실패(공간/가방 부족) 시 프롬프트·이름 유지 + 재시도 가능.
        // 그 외 oneShot(쪽지 등)은 즉시 소진.
        if (oneShot && type != InteractType.Pickup) used = true;

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
            // 하이드아웃 시설 = 단일 패널(건설/업그레이드 + 기능). HideoutUI가 레벨 따라 분기.
            case InteractType.Workbench:
                HideoutUI.Show("workbench");
                break;
            case InteractType.MedicalBench:
                HideoutUI.Show("medbench");
                break;
            case InteractType.CookingBench:
                HideoutUI.Show("cooking");
                break;
            case InteractType.MapBoard:
                HandleMapBoard(playerGO);
                break;
            case InteractType.NPC:
                HandleNPC(playerGO);
                break;
            case InteractType.Bed:
                HideoutUI.Show("quarters");
                break;
            case InteractType.Door:
                HandleDoor(playerGO);
                break;
            case InteractType.Passage:
                GetComponent<BlockedPassage>()?.TryPass(playerGO);
                break;
            case InteractType.Stash:
                HideoutUI.Show("stash");
                break;
            case InteractType.Radio:
                HideoutUI.Show("radio");
                break;
            case InteractType.Generator:
                HideoutUI.Show("generator");
                break;
            default:
                Debug.Log($"[Interact] {type}: {promptText}");
                break;
        }
    }

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

    void HandleNote(GameObject playerGO)
    {
        // 2026-07-11: 쪽지가 **지식**(금고 번호 등)을 준다 — 아이템이 아니라 플래그라 죽어도 잃지 않는다.
        //   (docs/level-scrapmarket.md — 보석상 금고 코드는 다른 건물에서 주운 쪽지로 알아낸다)
        if (!string.IsNullOrEmpty(grantsKnowledgeId) && PlayerKnowledge.Learn(grantsKnowledgeId))
            ToastManager.Show($"알아냈다 — {(string.IsNullOrEmpty(noteTitle) ? grantsKnowledgeId : noteTitle)}",
                              ToastManager.ToastType.Info);

        // 스토리 씬에 연결된 쪽지 → 스크립트 씬 재생(StoryPlayer 경유).
        if (!string.IsNullOrEmpty(noteStorySceneId) && StoryTriggerManager.Instance != null)
        {
            StoryTriggerManager.Instance.OnNoteRead(noteContent, noteStorySceneId);
            return;
        }

        // 일반 쪽지 → 전체 화면 노트 UI(없으면 자동 생성).
        NoteUI.Ensure().Show(noteContent, noteTitle);
    }

    void HandleBed(GameObject playerGO)
    {
        // 침대 → 수면 UI(시간 선택 + HP·스태미너 회복 + 수분/포만감 차감).
        // 확인 시 SleepUI가 회복·차감 + StoryTriggerManager.OnBedRest()까지 처리.
        SleepUI.Show();
    }

    void HandlePickup(GameObject playerGO)
    {
        // WorldItem이 있으면 인벤토리 연동
        var worldItem = GetComponent<WorldItem>();
        if (worldItem != null)
        {
            // 바닥에 여러 개 겹쳐 있으면 → 목록 UI로 선택 줍기
            var cluster = WorldItem.GatherNear(playerGO.transform.position, GroundPickupUI.ClusterRadius);
            if (cluster.Count >= 2)
            {
                GroundPickupUI.Show(cluster, playerGO);
                return;
            }

            worldItem.TryPickup(playerGO); // 성공 시 WorldItem이 Destroy / 실패 시 토스트
            return;
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
                        if (data.itemId == "ruby_shard" || data.itemId == "rudi_shard" || data.itemId == "rudi")
                            StoryTriggerManager.Instance.OnRudiPickup();
                    }

                    used = true;                                   // 성공 시에만 소진
                    if (oneShot) gameObject.SetActive(false);
                }
                else
                {
                    // 실패 → used 유지(false). 프롬프트·이름 그대로, 공간 확보 후 재시도 가능.
                    string reason = inventory.HasBackpack ? "인벤토리 공간 부족" : "가방을 장착하세요";
                    Debug.Log($"[Pickup] {reason}");
                    ToastManager.Show(reason, ToastManager.ToastType.Warning);
                }
            }
        }
        else
        {
            Debug.Log($"[Pickup] {itemId} x{itemCount} (ItemDatabase 미등록)");
            used = true;
            if (oneShot) gameObject.SetActive(false);
        }
    }

    void HandleContainer(GameObject playerGO)
    {
        // 안전가옥 창고 우선 체크
        var storage = GetComponent<SafehouseStorage>();
        if (storage != null)
        {
            storage.Open();
            return;
        }

        // 필드 루팅 상자·시체 = 루팅 목록(2026-09-11 docs/region-loot.md §루팅 정리 결정 — 처음 열면 옛 수색 연출로
        //   하나씩 드러나고, 다 드러나면 전부/하나씩). Tab이면 캐릭터 패널 자세히 창. 창고·보관함은 위 SafehouseStorage.
        var container = GetComponent<LootContainer>();
        if (container != null)
        {
            container.Open(playerGO);
            LootListUI.Show(container, playerGO);
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

    void HandleNPC(GameObject playerGO)
    {
        var npc = GetComponent<NPCController>();
        if (npc != null)
            npc.Talk(playerGO);
        else
            Debug.Log($"[NPC] {promptText} (NPCController 없음)");
    }

    void HandleDoor(GameObject playerGO)
    {
        // 2026-07-11 (사용자: "모든 문은 상호작용해야 내부로 들어가도록"):
        //   문 하나가 **표시 + 잠금 + 진입**을 모두 맡는다. 순서가 곧 규칙이다.
        //   ① 잠겨 있으면 먼저 연다(열쇠·비밀번호·철거) → ② 열렸으면 안으로 들어간다.
        var passage = GetComponent<BlockedPassage>();
        if (passage != null && !passage.IsOpen) { passage.TryPass(playerGO); return; }

        var entrance = GetComponent<BuildingEntrance>();
        if (entrance != null) { entrance.Interact(playerGO); return; }

        var door = GetComponent<DoorController>();
        if (door != null)
            door.TryOpen(playerGO);
        else
            Debug.Log($"[Door] {promptText} (DoorController 없음)");
    }

    void HandleStash(GameObject playerGO)
    {
        // 창고 시설 클릭 → 인벤토리 + 우측 메인 창고 표시.
        if (UIManager.Instance != null)
            UIManager.Instance.ShowCharacterPanelWithStash();
        else
            Debug.LogWarning("[Stash] UIManager가 없습니다.");
    }

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

    /// <summary>런타임 프롬프트 교체 — 상태가 변하는 상호작용(막힌 통로 등)용.</summary>
    public void SetPrompt(string prompt)
    {
        if (!string.IsNullOrEmpty(prompt)) promptText = prompt;
    }

    /// <summary>상호작용 가능/불가 토글 — 치운 잔해처럼 '끝난' 오브젝트를 목록에서 빼는 용도.</summary>
    public void SetInteractable(bool on)
    {
        if (on) used = false;
        else  { used = true; oneShot = true; }
    }

    /// <summary>런타임/부트스트랩에서 타입·프롬프트 설정</summary>
    public void Configure(InteractType interactType, string prompt, float range = 1.5f, bool once = false)
    {
        type = interactType;
        promptText = prompt;
        interactRange = range;
        oneShot = once;
    }

    /// <summary>런타임에서 루팅 컨테이너로 구성(적 시체 등). LootContainer는 같은 GO에 별도 부착할 것.</summary>
    public void SetupAsContainer(string prompt)
    {
        type = InteractType.Container;
        promptText = prompt;
        oneShot = false;
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

    /// <summary>코드에서 Note(쪽지) 타입으로 설정 (그레이박스 레이아웃 등에서 쪽지 내용 주입).</summary>
    public void SetNote(string content, string title = "", string prompt = "읽기")
    {
        type = InteractType.Note;
        noteContent = content;
        noteTitle = title;
        noteStorySceneId = "";
        promptText = prompt;
        if (interactRange < 0.5f) interactRange = 1.5f;
    }

    /// <summary>지식(금고 번호 등)을 주는 쪽지로 구성. 읽는 순간 PlayerKnowledge에 기록된다.</summary>
    public void SetKnowledgeNote(string content, string title, string knowledgeId, string prompt = "읽기")
    {
        SetNote(content, title, prompt);
        grantsKnowledgeId = knowledgeId;
    }

    #endregion

    #region 하이라이트

    /// <summary>바라보는 대상임을 색으로 알린다.
    ///
    /// ⚠️ 예전엔 <see cref="SpriteRenderer"/>만 칠했다. 3D로 오면서 NPC·문·컨테이너가 전부
    ///    메시가 됐고, 그 순간 **하이라이트가 통째로 안 보이게 됐다** — "이걸 누를 수 있다"는
    ///    피드백이 사라지면 무엇과 상호작용되는지 알 방법이 없다. 이제 자식 렌더러를 전부 칠한다.
    ///
    /// 원래 색은 <c>sharedMaterial</c>에서 읽는다. <c>material</c>을 읽으면 그 순간
    /// 머티리얼이 인스턴스화돼 공유가 깨지고 오브젝트마다 하나씩 샌다.</summary>
    public void SetHighlight(bool on)
    {
        isHighlighted = on;

        if (spriteRenderer != null)
            spriteRenderer.color = on ? highlightColor : originalColor;

        // ⚠️ **비어 있으면 다시 훑는다.** 한 번 캐시하고 끝내면, 첫 호출이 렌더러가
        //    생기기 전에 들어온 오브젝트는 빈 배열이 굳어 **하이라이트가 영영 죽는다**
        //    — 실제로 마을 NPC 전원이 그 상태였고 원인을 찾는 데 오래 걸렸다.
        //    하이라이트는 바라보는 대상이 바뀔 때만 호출되므로 재조회 비용은 무시할 만하다.
        if (_meshRenderers == null || _meshRenderers.Length == 0) CacheMeshRenderers();
        for (int i = 0; i < _meshRenderers.Length; i++)
            GreyboxMesh.Tint(_meshRenderers[i], on ? highlightColor : _meshBaseColors[i]);
    }

    Renderer[] _meshRenderers;
    Color[]    _meshBaseColors;

    void CacheMeshRenderers()
    {
        var all = GetComponentsInChildren<Renderer>(true);
        var keep = new System.Collections.Generic.List<Renderer>();
        foreach (var r in all)
        {
            if (r is SpriteRenderer) continue;          // 위에서 따로 처리
            if (r is TrailRenderer || r is LineRenderer) continue;
            if (r.GetComponentInParent<Billboard>() != null) continue;   // 이름표·게이지는 칠하지 않는다
            keep.Add(r);
        }
        _meshRenderers   = keep.ToArray();
        _meshBaseColors  = new Color[_meshRenderers.Length];
        for (int i = 0; i < _meshRenderers.Length; i++)
        {
            var m = _meshRenderers[i].sharedMaterial;
            _meshBaseColors[i] = m == null ? Color.white
                               : m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor")
                               : m.HasProperty("_Color")     ? m.GetColor("_Color")
                               : Color.white;
        }
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
