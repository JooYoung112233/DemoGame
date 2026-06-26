using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 통합 캐릭터 패널 (Tab 키). 타르코프식 3열 레이아웃.
/// 좌=캐릭터/장비 상태, 중=가방(인벤토리 격자), 우=창고/파밍.
/// UIManager 자식으로 배치.
/// </summary>
public class CharacterPanelUI : MonoBehaviour
{
    public bool IsShowing => isShowing;
    public bool IsGenerated => canvas != null;

    bool isShowing;

    // 레퍼런스 (자동 탐색)
    PlayerInventory playerInventory;
    PlayerMedicalSystem medical;
    Health health;
    GameObject playerGO; // 플레이어 GO 참조 (컴포넌트는 GetComponent로 접근)
    FlashlightController flashlight;

    // 루팅 중인 상자 또는 창고
    LootContainer openContainer;
    SafehouseStorage openStorage; // 안전가옥 창고 (LootContainer와 별도)
    FurnitureInstance openFurniture; // 현재 열린 가구 인스턴스 (카테고리 필터용)
    InventoryGrid openStashGrid;  // 메인 창고(MainStash) — 안전구역 인벤 우측 열
    ItemInstance openContainerItem; // 현재 열린 보관함/가방(컨테이너 아이템) — 독립 팝업에 내부 격자 표시
    // 컨테이너 팝업(이동 가능 창)
    GameObject containerPopupGO;
    RectTransform containerPopupRT;
    RectTransform popupGridRoot;
    Text containerPopupTitle;
    GameObject containerPopupSortBtn;
    Image[,] popupSlotImages;

    /// <summary>현재 열린 우측 격자 (상자 > 가구창고 > 메인창고)</summary>
    InventoryGrid LeftGrid =>
        openContainer != null ? openContainer.Grid
        : openStorage != null ? openStorage.Grid
        : openStashGrid;

    // ── uGUI 요소 (구조적 요소만 SerializeField) ──
    [SerializeField] Canvas canvas;
    [SerializeField] RectTransform canvasRT; // 캔버스 RectTransform (고스트 부모)
    [SerializeField] GameObject panelRoot;

    // 우측 메인 패널
    [SerializeField] RectTransform rightPanel;
    [SerializeField] Image rightPanelBg;

    // 좌측 상자 패널
    [SerializeField] RectTransform leftPanel;
    [SerializeField] Image leftPanelBg;
    [SerializeField] Text leftTitleText;
    [SerializeField] GameObject leftPanelRoot; // 숨김/표시용
    [SerializeField] GameObject leftPlaceholder; // 우측 열 빈칸 안내 (창고/상자 미오픈 시)

    // ── 중앙 패널: 무기파츠(예약) / 가방 / 주머니4 / 보안3×3 (위→아래) ──
    [SerializeField] RectTransform midContentRoot; // 세로 스택 콘텐츠 루트 (헤더 아래)
    [SerializeField] RectTransform weaponBox;      // 무기 파츠 예약 공간 (장착 시 표시 예정)
    [SerializeField] RectTransform invGridRoot;    // 가방(백팩) 격자 루트
    [SerializeField] RectTransform pocketsGridRoot;// 주머니 4칸 격자 루트 (고정)
    [SerializeField] RectTransform secureGridRoot; // 보안 컨테이너 3×3 격자 루트 (고정)
    [SerializeField] Text pocketsHeaderText;
    [SerializeField] Text secureHeaderText;
    [SerializeField] Text invWeightText;

    // ── 좌측 캐릭터/장비 패널 (타르코프식 3열 좌측) ──
    [SerializeField] RectTransform charPanel;
    [SerializeField] Text charHpText;
    [SerializeField] Text charStaminaText;
    [SerializeField] Text charWaterText;
    [SerializeField] Text charFoodText;
    [SerializeField] Text charWeightText;
    [SerializeField] Text[] charBodyTexts;

    // ── 장비 슬롯 UI ──
    PlayerEquipment playerEquipment;
    Dictionary<EquipSlot, Image> equipSlotBgs;
    Dictionary<EquipSlot, Image> equipSlotIcons;
    Dictionary<EquipSlot, Text> equipSlotLabels;

    // ── 좌측 상자 격자 ──
    [SerializeField] RectTransform containerGridRoot;
    Image[,] containerSlotImages;

    // 설정
    static readonly int CELL_SIZE = 48;
    static readonly int CELL_GAP = 2;
    static readonly float PANEL_WIDTH = 360f;
    static readonly string[] PART_NAMES = { "머 리", "몸 통", "양 팔", "왼다리", "오른다리" };

    // 중앙 패널 세로 스택 레이아웃
    const float MID_INNER_W = 520f;   // 중앙 콘텐츠 가용 폭 (격자 가로 정렬 기준)
    const float WEAPON_BOX_H = 92f;   // 무기 파츠 예약 공간 높이
    const float SECTION_HDR_H = 22f;  // 섹션 헤더 높이
    const float SECTION_GAP = 10f;    // 섹션 간 간격

    // ── 드래그 앤 드롭 ──
    bool isDragging;
    ItemInstance dragItem;
    InventoryGrid dragSourceGrid;
    int dragOrigX, dragOrigY;
    bool dragOrigRotated;
    bool dragRotated;
    // 픽셀 잡기 오프셋: 아이템 좌상단 → 잡은 지점(커서)까지의 캔버스 픽셀 거리.
    // 고스트는 이 값만큼 커서를 자유 추적(칸 점프 없음), 배치는 좌상단을 가까운 칸에 스냅.
    Vector2 grabPixelOffset;
    GameObject ghostGO;
    RectTransform ghostRT;
    GameObject highlightGO;
    RectTransform highlightRT;
    Image highlightImage;

    // ── 가방 미장착 안내 ──
    RectTransform invPlaceholder; // 중앙열: 가방 미장착 시 안내
    Text bagHeaderText;        // "가방" 헤더 텍스트 (동적 갱신용)

    // ── 우클릭 컨텍스트 메뉴 ──
    GameObject contextMenuGO;
    RectTransform contextMenuRT;
    InventoryGrid.PlacedItem contextTarget;
    InventoryGrid contextTargetGrid;
    Text contextItemNameText;

    // ── 확인 팝업(버리기/폐기/제거 등 파괴적 동작 전 한 번 더 묻기) ──
    GameObject confirmGO;
    Text confirmText;
    System.Action confirmYes;

    // ── 아이템 선택(클릭) → 착용 버튼 + 좌측 슬롯 하이라이트 ──
    ItemInstance selectedItem;        // 현재 선택된 아이템(클릭 시)
    InventoryGrid selectedGrid;       // 선택 아이템이 속한 격자
    readonly HashSet<EquipSlot> highlightSlots = new HashSet<EquipSlot>();  // 하이라이트할 좌측 슬롯
    Vector2 dragStartMouse;           // 드래그 시작 시 마우스 위치(클릭 판정용)
    const float CLICK_MOVE_THRESHOLD = 6f;  // 이 거리 미만 이동이면 클릭(선택)으로 간주

    // ── 수색 연출 (루팅 상자 전용) ──
    bool isSearching;
    float searchTimer;
    float searchDelay;
    System.Collections.Generic.Queue<InventoryGrid.PlacedItem> searchQueue;
    InventoryGrid.PlacedItem searchingItem;
    System.Collections.Generic.HashSet<int> revealedUids;
    InventoryGrid lastSearchedGrid;
    int totalSearchItems;
    int revealedCount;
    bool leftPanelSearchEnabled; // true=루팅상자(수색), false=창고(즉시)
    [SerializeField] Text searchStatusText; // "수색 중... 3/7"

    void Awake()
    {
        if (!IsGenerated) GenerateUI();
        BindEvents();
    }

    void Update()
    {
        if (!isShowing) return;

        // 아이템 상세 팝업이 떠 있으면 패널 입력(드래그/클릭/Esc) 차단 — 팝업이 자체 처리.
        if (ItemDetailUI.IsShowing) return;

        if (GameInput.GetKeyDown(KeyCode.Escape))
        {
            if (contextMenuGO != null && contextMenuGO.activeSelf)
            {
                HideContextMenu();
                return;
            }
            // 사용 채널 진행 중이면 먼저 취소
            if (UseActionManager.Instance != null && UseActionManager.Instance.IsBusy)
            {
                UseActionManager.Instance.Cancel();
                return;
            }
            // 컨테이너 팝업 열려 있으면 먼저 닫기(인벤 패널보다 우선)
            if (openContainerItem != null && containerPopupGO != null && containerPopupGO.activeSelf)
            {
                CloseContainerPopup();
                return;
            }
            if (isDragging)
                CancelDrag();
            else
                Hide();
            return;
        }

        UpdateInventoryTab();
        UpdateCharacterPanel();

        if (LeftGrid != null)
            UpdateContainerGrid();

        HandleDragAndDrop();
    }

    #region Show / Hide

    public void Show()
    {
        FindRefs();
        isShowing = true;
        if (panelRoot != null)
            panelRoot.SetActive(true);
        RefreshInventoryGrid();

        // 안전구역(안전가옥/하이드아웃)이면 우측에 메인 창고를 항상 표시.
        // 레이드 중에는 표시 안 함(상자에 다가가 E로 파밍할 때만 우측이 채워짐).
        if (IsSafeArea() && openContainer == null && openStorage == null)
            ShowStash();

        SyncLeftPlaceholder();
    }

    /// <summary>레이드(지역 활성) 중이 아니면 안전구역으로 간주.</summary>
    bool IsSafeArea()
    {
        return RegionTimeManager.Instance == null
            || string.IsNullOrEmpty(RegionTimeManager.Instance.ActiveRegionId);
    }

    /// <summary>우측 열에 메인 창고 표시.</summary>
    void ShowStash()
    {
        var stash = MainStash.Ensure();
        if (stash == null) return;
        openStashGrid = stash.GetGrid();
        ShowLeftPanel(openStashGrid, "창고", false);   // 수색 연출 없이 즉시 공개
    }

    public void Hide()
    {
        HideContextMenu();
        if (isDragging) CancelDrag();
        ClearSelection();
        isShowing = false;
        if (panelRoot != null)
            panelRoot.SetActive(false);
        CloseContainer();
    }

    /// <summary>창고 시설 클릭 → 인벤 + 우측 메인 창고 강제 표시 (지역 무관).</summary>
    public void ShowWithStash()
    {
        openContainer = null;
        openStorage = null;
        openFurniture = null;
        Show();          // 안전구역이면 Show 안에서 이미 창고 표시
        ShowStash();     // 명시적으로 한 번 더 보장
    }

    public void ShowWithContainer(LootContainer container)
    {
        openContainer = container;
        openStorage = null;
        openFurniture = null;
        Show();
        // 이미 수색 완료된 상자는 즉시 공개
        bool needSearch = !container.HasBeenSearched;
        ShowLeftPanel(container.Grid, container.ContainerName, needSearch);
    }

    public void ShowWithStorage(SafehouseStorage storage)
    {
        openStorage = storage;
        openFurniture = storage.LinkedInstance;
        openContainer = null;
        Show();

        // 제목에 허용 카테고리 표시
        string title = storage.StorageName;
        if (openFurniture != null && openFurniture.data != null && !openFurniture.data.IsUniversal)
            title += $" <size=11><color=#88AACC>({openFurniture.data.AllowedCategorySummary})</color></size>";

        ShowLeftPanel(storage.Grid, title, false); // 수색 연출 X (내 창고)
    }

    /// <summary>보관함/가방 열기 → 내부 격자 셀 크기에 맞는 독립 이동 팝업 표시.</summary>
    public void OpenContainerItem(ItemInstance inst)
    {
        if (inst == null || !inst.IsContainer) return;
        Show();
        openContainerItem = inst;
        EnsureContainerPopup();

        string title = inst.data.displayName;
        if (inst.data.allowedCategories != null && inst.data.allowedCategories.Length > 0)
            title += $"  <size=10><color=#88AACC>({inst.data.AllowedCategorySummary})</color></size>";
        containerPopupTitle.text = title;

        // 가방/조끼(장비형 컨테이너)는 정렬 버튼 제외.
        bool showSort = inst.data.equipSlot == EquipSlot.None;
        if (containerPopupSortBtn != null) containerPopupSortBtn.SetActive(showSort);

        RefreshContainerPopup();
        containerPopupGO.SetActive(true);
        containerPopupGO.transform.SetAsLastSibling();
    }

    /// <summary>컨테이너 팝업 닫기(X).</summary>
    void CloseContainerPopup()
    {
        openContainerItem = null;
        if (containerPopupGO != null) containerPopupGO.SetActive(false);
        if (selectedGrid != null && !IsPlayerGrid(selectedGrid)) ClearSelection();
    }

    /// <summary>현재 열린 좌측 격자(가구)가 dragItem을 받을 수 있는지(카테고리 게이트).</summary>
    bool LeftGridAccepts(ItemInstance item)
    {
        if (item == null || item.data == null) return false;
        if (openFurniture != null) return openFurniture.AcceptsItem(item);
        return true;   // 루팅 상자 / 메인 창고 = 제한 없음
    }

    // ── 컨테이너 팝업(이동 가능 창) ──────────────────────────────────
    const float POPUP_HEADER_H = 30f;
    const float POPUP_PAD = 8f;

    void EnsureContainerPopup()
    {
        if (containerPopupGO != null) return;

        containerPopupGO = new GameObject("ContainerPopup", typeof(RectTransform), typeof(Image));
        containerPopupGO.transform.SetParent(canvasRT, false);
        containerPopupRT = containerPopupGO.GetComponent<RectTransform>();
        containerPopupRT.anchorMin = containerPopupRT.anchorMax = new Vector2(0.5f, 0.5f);
        containerPopupRT.pivot = new Vector2(0.5f, 0.5f);
        containerPopupRT.sizeDelta = new Vector2(220, 200);
        containerPopupRT.anchoredPosition = new Vector2(160, 0);
        containerPopupGO.GetComponent<Image>().color = UITheme.Panel;

        // 헤더(드래그 핸들)
        var header = new GameObject("Header", typeof(RectTransform), typeof(Image), typeof(DraggableWindow));
        header.transform.SetParent(containerPopupRT, false);
        var hRT = header.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1); hRT.pivot = new Vector2(0.5f, 1);
        hRT.anchoredPosition = Vector2.zero;
        hRT.sizeDelta = new Vector2(0, POPUP_HEADER_H);
        header.GetComponent<Image>().color = UITheme.Header;
        header.GetComponent<DraggableWindow>().target = containerPopupRT;

        // 제목
        containerPopupTitle = MakeText(hRT, "Title", "",
            Vector2.zero, new Vector2(0, POPUP_HEADER_H), 13, UITheme.Gold, TextAnchor.MiddleLeft);
        var tRT = containerPopupTitle.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0, 0); tRT.anchorMax = new Vector2(1, 1);
        tRT.offsetMin = new Vector2(10, 0); tRT.offsetMax = new Vector2(-92, 0);
        containerPopupTitle.fontStyle = FontStyle.Bold;

        // 정렬 / 닫기 버튼 (헤더 우측)
        containerPopupSortBtn = MakePopupHeaderButton(hRT, "정렬", -36, UITheme.Accent, SortContainerPopup, 50);
        MakePopupHeaderButton(hRT, "✕", -6, UITheme.Negative, CloseContainerPopup, 24);

        // 격자 루트(헤더 아래)
        var gridGO = new GameObject("PopupGrid", typeof(RectTransform));
        gridGO.transform.SetParent(containerPopupRT, false);
        popupGridRoot = gridGO.GetComponent<RectTransform>();
        popupGridRoot.anchorMin = new Vector2(0, 1); popupGridRoot.anchorMax = new Vector2(0, 1);
        popupGridRoot.pivot = new Vector2(0, 1);
        popupGridRoot.anchoredPosition = new Vector2(POPUP_PAD, -(POPUP_HEADER_H + POPUP_PAD));

        containerPopupGO.SetActive(false);
    }

    GameObject MakePopupHeaderButton(RectTransform header, string label, float xFromRight, Color col,
                                     UnityEngine.Events.UnityAction onClick, float width)
    {
        var btn = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(header, false);
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(xFromRight, -4);
        rt.sizeDelta = new Vector2(width, 22);
        btn.GetComponent<Image>().color = col;
        btn.GetComponent<Button>().onClick.AddListener(onClick);
        MakeChildText(btn.transform, label, 12, UITheme.TextBright);
        return btn;
    }

    /// <summary>팝업 격자 슬롯·아이템 재그림 + 팝업 창 크기를 격자에 맞춤.</summary>
    void RefreshContainerPopup()
    {
        if (openContainerItem == null || popupGridRoot == null) return;
        var grid = openContainerItem.ContainerGrid;
        if (grid == null) return;

        for (int i = popupGridRoot.childCount - 1; i >= 0; i--)
            Destroy(popupGridRoot.GetChild(i).gameObject);

        int cellTotal = CELL_SIZE + CELL_GAP;
        float gw = grid.width * cellTotal - CELL_GAP;
        float gh = grid.height * cellTotal - CELL_GAP;
        popupGridRoot.sizeDelta = new Vector2(gw, gh);
        containerPopupRT.sizeDelta = new Vector2(gw + POPUP_PAD * 2, gh + POPUP_HEADER_H + POPUP_PAD * 2);

        popupSlotImages = new Image[grid.width, grid.height];
        for (int gy = 0; gy < grid.height; gy++)
            for (int gx = 0; gx < grid.width; gx++)
            {
                var slotGO = new GameObject($"PSlot_{gx}_{gy}", typeof(RectTransform), typeof(Image));
                slotGO.transform.SetParent(popupGridRoot, false);
                var rt = slotGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(gx * cellTotal, -gy * cellTotal);
                rt.sizeDelta = new Vector2(CELL_SIZE, CELL_SIZE);
                slotGO.GetComponent<Image>().color = UITheme.PanelAlt;
                popupSlotImages[gx, gy] = slotGO.GetComponent<Image>();
            }

        RefreshContainerItems(grid, popupGridRoot, false);   // 수색 무관(항상 공개)
    }

    void SortContainerPopup()
    {
        if (openContainerItem == null) return;
        SortGrid(openContainerItem.ContainerGrid);
        RefreshContainerPopup();
    }

    /// <summary>마우스가 컨테이너 팝업 격자 위인지 + 셀 반환.</summary>
    bool ContainerPopupAtMouse(out InventoryGrid grid, out int gx, out int gy)
    {
        grid = null; gx = gy = -1;
        if (openContainerItem == null || containerPopupGO == null || !containerPopupGO.activeSelf) return false;
        var g = openContainerItem.ContainerGrid;
        if (g == null || popupGridRoot == null) return false;
        if (ScreenToGridCell(popupGridRoot, g, out gx, out gy)) { grid = g; return true; }
        return false;
    }

    /// <summary>마우스가 컨테이너 팝업 창(헤더·여백 포함) 위인지 — 뒤 격자로 클릭이 새지 않게.</summary>
    bool PointerOverContainerPopup()
    {
        return containerPopupGO != null && containerPopupGO.activeSelf
            && IsMouseOverRect(containerPopupRT);
    }

    /// <summary>장착형 컨테이너(가방·조끼)인지 — 이런 것끼리는 중첩 금지(가방 안 가방 방지).</summary>
    static bool IsWearableContainer(ItemData d) => d != null && d.IsContainer && d.equipSlot != EquipSlot.None;

    /// <summary>dragged를 container 내부에 넣을 수 있는지(카테고리 일치 + 가방 안 가방 방지).</summary>
    bool CanInsertIntoContainer(ItemInstance dragged, ItemInstance container)
    {
        if (dragged == null || container == null || dragged == container) return false;
        if (dragged.data == null || container.data == null || !container.IsContainer) return false;
        if (!container.data.AcceptsCategory(dragged.data.category)) return false;
        if (IsWearableContainer(dragged.data) && IsWearableContainer(container.data)) return false; // 가방 안 가방 방지
        return true;
    }

    public void CloseContainer()
    {
        StopSearch(false);
        openContainerItem = null;
        if (containerPopupGO != null) containerPopupGO.SetActive(false);
        if (openContainer != null)
        {
            openContainer.Close();
            openContainer = null;
        }
        openStorage = null;
        openFurniture = null;
        openStashGrid = null;
        // 좌측 격자가 닫히면 그 격자를 가리키던 선택은 무효 → 해제
        if (selectedGrid != null && !IsPlayerGrid(selectedGrid))
            ClearSelection();
        if (leftPanelRoot != null)
            leftPanelRoot.SetActive(false);
        SyncLeftPlaceholder();
    }

    #endregion

    #region 레퍼런스

    /// <summary>씬 전환 시 레퍼런스 초기화 (DontDestroyOnLoad이므로 필요)</summary>
    public void ResetRefs()
    {
        StopSearch(false);
        revealedUids = null;
        lastSearchedGrid = null;
        openContainer = null;
        openStorage = null;
        openFurniture = null;
        openStashGrid = null;
        openContainerItem = null;
        if (containerPopupGO != null) containerPopupGO.SetActive(false);
        ClearSelection();

        playerGO = null;
        health = null;
        medical = null;
        playerInventory = null;
        playerEquipment = null;
        flashlight = null;
    }

    void FindRefs()
    {
        if (playerGO != null) return;
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return;
        playerGO = go;
        health = go.GetComponent<Health>();
        medical = go.GetComponent<PlayerMedicalSystem>();
        playerInventory = go.GetComponent<PlayerInventory>();
        playerEquipment = go.GetComponent<PlayerEquipment>();
        flashlight = go.GetComponentInChildren<FlashlightController>();
    }

    #endregion

    #region UI 빌드

    public void GenerateUI()
    {
        // Canvas
        var canvasGO = new GameObject("CharPanel_Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        canvasRT = canvasGO.GetComponent<RectTransform>();

        // ── 전체 루트 ──
        panelRoot = new GameObject("PanelRoot");
        panelRoot.transform.SetParent(canvasRT, false);
        var rootRT = panelRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        // 불투명 배경 (전체화면 모달 — 게임 화면 완전히 가림)
        var dimBg = panelRoot.AddComponent<Image>();
        dimBg.color = UITheme.Backdrop;

        // ── 타르코프식 3열: 좌=캐릭터/장비 · 중=내 가방(탭) · 우=창고/파밍 ──
        BuildRightPanel(panelRoot.transform);      // 중앙 = 내 가방
        BuildLeftPanel(panelRoot.transform);       // 우측 = 창고/파밍
        BuildCharacterPanel(panelRoot.transform);  // 좌측 = 캐릭터/장비

        // 우측 상단 닫기(X) 버튼
        var closeGO = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGO.transform.SetParent(panelRoot.transform, false);
        var cRT = closeGO.GetComponent<RectTransform>();
        cRT.anchorMin = cRT.anchorMax = cRT.pivot = new Vector2(1, 1);
        cRT.anchoredPosition = new Vector2(-18, -18);
        cRT.sizeDelta = new Vector2(36, 36);
        closeGO.GetComponent<Image>().color = UITheme.Negative;
        var cBtn = closeGO.GetComponent<Button>();
        var ccol = cBtn.colors; ccol.highlightedColor = UITheme.CellHover; ccol.pressedColor = UITheme.CellPressed; cBtn.colors = ccol;
        cBtn.onClick.AddListener(Hide);
        MakeChildText(closeGO.transform, "✕", 18, UITheme.TextBright);

        panelRoot.SetActive(false);
    }

    public void BindEvents()
    {
    }

    /// <summary>생성된 UI 구조 제거 및 모든 직렬화 레퍼런스 초기화</summary>
    public void ClearGeneratedUI()
    {
        // CharPanel_Canvas 자식 찾아서 제거
        var canvasTr = transform.Find("CharPanel_Canvas");
        if (canvasTr != null)
        {
            if (Application.isPlaying)
                Destroy(canvasTr.gameObject);
            else
                DestroyImmediate(canvasTr.gameObject);
        }

        canvas = null;
        canvasRT = null;
        panelRoot = null;
        rightPanel = null;
        rightPanelBg = null;
        leftPanel = null;
        leftPanelBg = null;
        leftTitleText = null;
        leftPanelRoot = null;
        leftPlaceholder = null;
        midContentRoot = null;
        weaponBox = null;
        invGridRoot = null;
        pocketsGridRoot = null;
        secureGridRoot = null;
        pocketsHeaderText = null;
        secureHeaderText = null;
        invWeightText = null;
        containerGridRoot = null;
        searchStatusText = null;
        charPanel = null;
        charHpText = null;
        charStaminaText = null;
        charWaterText = null;
        charFoodText = null;
        charWeightText = null;
        charBodyTexts = null;
        equipSlotBgs = null;
        equipSlotIcons = null;
        equipSlotLabels = null;
        invPlaceholder = null;
        bagHeaderText = null;
        selectedItem = null;
        selectedGrid = null;
        highlightSlots.Clear();
    }

    void BuildRightPanel(Transform parent)
    {
        var go = new GameObject("RightPanel");
        go.transform.SetParent(parent, false);
        rightPanel = go.AddComponent<RectTransform>();
        rightPanel.anchorMin = new Vector2(0.355f, 0.07f);
        rightPanel.anchorMax = new Vector2(0.645f, 0.93f);
        rightPanel.offsetMin = Vector2.zero;
        rightPanel.offsetMax = Vector2.zero;

        rightPanelBg = go.AddComponent<Image>();
        rightPanelBg.color = UITheme.Panel;

        // 상단 패널 제목
        var title = MakeText(rightPanel, "MidTitle", "장비 / 소지품",
            new Vector2(0, -8), new Vector2(MID_INNER_W, 26), 16, UITheme.TextBright, TextAnchor.MiddleCenter);
        title.fontStyle = FontStyle.Bold;
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = titleRT.anchorMax = titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0, -8);

        // ── 세로 스택 콘텐츠 루트 (제목 아래, 가로 중앙 고정폭) ──
        var contentGO = new GameObject("MidContent", typeof(RectTransform));
        contentGO.transform.SetParent(rightPanel, false);
        midContentRoot = contentGO.GetComponent<RectTransform>();
        midContentRoot.anchorMin = midContentRoot.anchorMax = midContentRoot.pivot = new Vector2(0.5f, 1f);
        midContentRoot.anchoredPosition = new Vector2(0, -40);
        midContentRoot.sizeDelta = new Vector2(MID_INNER_W, 980);

        // 무기 파츠 예약 공간 (장착 시 채워질 자리 — 지금은 안내만)
        weaponBox = MakeSection(midContentRoot, "WeaponPartsBox", MID_INNER_W, WEAPON_BOX_H,
            UITheme.PanelAlt);
        var wpTxt = MakeChildText(weaponBox, "무기 파츠\n(무기 장착 시 표시)", 12, UITheme.TextMuted);
        wpTxt.alignment = TextAnchor.MiddleCenter;

        // 가방 헤더 + 격자 루트
        bagHeaderText = MakeStackHeader(midContentRoot, "BagHeader", "가방", UITheme.AccentBright);
        invGridRoot = MakeGridRoot(midContentRoot, "InvGrid");

        // 가방 미장착 안내 (가방 격자 위치에 오버레이; 위치/크기는 레이아웃에서 지정)
        invPlaceholder = MakeSection(midContentRoot, "InvPlaceholder", MID_INNER_W, 54,
            UITheme.PanelAlt);
        var phTxt = MakeChildText(invPlaceholder, "가방 미장착 — 가방을 장착하면 격자가 열립니다",
            12, UITheme.TextMuted);
        phTxt.alignment = TextAnchor.MiddleCenter;
        invPlaceholder.gameObject.SetActive(false);

        // 주머니 헤더 + 격자 루트 (고정 4칸)
        pocketsHeaderText = MakeStackHeader(midContentRoot, "PocketsHeader", "주머니", UITheme.AccentBright);
        pocketsGridRoot = MakeGridRoot(midContentRoot, "PocketsGrid");

        // 보안 컨테이너 헤더 + 격자 루트 (고정 3×3, 레이드 사망에도 보존)
        secureHeaderText = MakeStackHeader(midContentRoot, "SecureHeader", "보안 컨테이너", UITheme.AccentBright);
        secureGridRoot = MakeGridRoot(midContentRoot, "SecureGrid");

        // 무게 텍스트 (스택 맨 아래; 위치는 레이아웃에서)
        invWeightText = MakeText(midContentRoot, "Weight", "무게: 0 / 30 kg",
            new Vector2(0, 0), new Vector2(MID_INNER_W, 22), 13, UITheme.TextMuted, TextAnchor.MiddleLeft);
    }

    /// <summary>중앙 스택용 고정폭 박스 섹션 생성 (배경 Image 포함).</summary>
    RectTransform MakeSection(RectTransform parent, string name, float w, float h, Color bg)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = bg;
        return rt;
    }

    /// <summary>중앙 스택용 섹션 헤더 텍스트(가로 전체, 좌측 정렬).</summary>
    Text MakeStackHeader(RectTransform parent, string name, string label, Color color)
    {
        var txt = MakeText(parent, name, label,
            new Vector2(2, 0), new Vector2(MID_INNER_W - 4, SECTION_HDR_H), 13, color, TextAnchor.LowerLeft);
        txt.fontStyle = FontStyle.Bold;
        return txt;
    }

    /// <summary>격자 루트(pivot 0,1 — ScreenToGridCell 히트테스트 기준) 생성.</summary>
    RectTransform MakeGridRoot(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(100, 100);
        return rt;
    }

    void BuildLeftPanel(Transform parent)
    {
        leftPanelRoot = new GameObject("LeftPanel");
        leftPanelRoot.transform.SetParent(parent, false);
        leftPanel = leftPanelRoot.AddComponent<RectTransform>();
        leftPanel.anchorMin = new Vector2(0.655f, 0.07f);   // 우측 열 = 창고/파밍 (가로 11칸 수용 위해 확장)
        leftPanel.anchorMax = new Vector2(0.99f, 0.93f);
        leftPanel.offsetMin = Vector2.zero;
        leftPanel.offsetMax = Vector2.zero;

        leftPanelBg = leftPanelRoot.AddComponent<Image>();
        leftPanelBg.color = UITheme.Panel;

        // 제목
        leftTitleText = MakeText(leftPanel, "LeftTitle", "상자",
            new Vector2(10, -8), new Vector2(PANEL_WIDTH - 20, 28), 16, UITheme.Gold, TextAnchor.MiddleCenter);
        leftTitleText.fontStyle = FontStyle.Bold;

        // 수색 상태 텍스트
        searchStatusText = MakeText(leftPanel, "SearchStatus", "",
            new Vector2(10, -32), new Vector2(PANEL_WIDTH - 20, 18), 12, UITheme.AccentBright, TextAnchor.MiddleCenter);

        // 정렬 버튼 (창고/상자 내 자동 정렬)
        var sortGO = new GameObject("SortBtn", typeof(RectTransform));
        sortGO.transform.SetParent(leftPanel, false);
        var sortRT = sortGO.GetComponent<RectTransform>();
        sortRT.anchorMin = sortRT.anchorMax = sortRT.pivot = new Vector2(1, 1);
        sortRT.anchoredPosition = new Vector2(-8, -6);
        sortRT.sizeDelta = new Vector2(56, 24);
        sortGO.AddComponent<Image>().color = UITheme.Accent;
        sortGO.AddComponent<Button>().onClick.AddListener(SortLeftGrid);
        MakeChildText(sortGO.transform, "정렬", 13, UITheme.TextBright);

        // ── 스크롤 뷰포트 (헤더 아래 영역) + 격자 content (창고 30~100줄 대응) ──
        var viewportGO = new GameObject("LeftViewport", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect));
        viewportGO.transform.SetParent(leftPanel, false);
        var vpRT = viewportGO.GetComponent<RectTransform>();
        vpRT.anchorMin = new Vector2(0, 0); vpRT.anchorMax = new Vector2(1, 1);
        vpRT.offsetMin = new Vector2(8, 8); vpRT.offsetMax = new Vector2(-22, -52);  // 우측 스크롤바 공간

        // 격자 루트 = 스크롤 content. pivot(0,1) 유지 → ScreenToGridCell 히트테스트 정상.
        var gridGO = new GameObject("ContainerGrid");
        gridGO.transform.SetParent(viewportGO.transform, false);
        containerGridRoot = gridGO.AddComponent<RectTransform>();
        containerGridRoot.anchorMin = new Vector2(0, 1);
        containerGridRoot.anchorMax = new Vector2(0, 1);
        containerGridRoot.pivot = new Vector2(0, 1);
        containerGridRoot.anchoredPosition = Vector2.zero;
        containerGridRoot.sizeDelta = new Vector2(PANEL_WIDTH - 20, 400);

        var leftScroll = viewportGO.GetComponent<ScrollRect>();
        leftScroll.horizontal = false; leftScroll.vertical = true;
        leftScroll.scrollSensitivity = 28f;
        leftScroll.movementType = ScrollRect.MovementType.Clamped;
        leftScroll.viewport = vpRT;
        leftScroll.content = containerGridRoot;

        // 세로 스크롤바 (우측, 위치 표기)
        var sbGO = new GameObject("LeftScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGO.transform.SetParent(leftPanel, false);
        var sbRT = sbGO.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0); sbRT.anchorMax = new Vector2(1, 1);
        sbRT.offsetMin = new Vector2(-16, 8); sbRT.offsetMax = new Vector2(-6, -52);
        sbGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.3f);   // 트랙

        var slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
        slidingArea.transform.SetParent(sbGO.transform, false);
        var saRT = slidingArea.GetComponent<RectTransform>();
        saRT.anchorMin = Vector2.zero; saRT.anchorMax = Vector2.one;
        saRT.offsetMin = Vector2.zero; saRT.offsetMax = Vector2.zero;

        var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.transform.SetParent(slidingArea.transform, false);
        var hRT = handleGO.GetComponent<RectTransform>();
        hRT.anchorMin = Vector2.zero; hRT.anchorMax = Vector2.one;
        hRT.offsetMin = Vector2.zero; hRT.offsetMax = Vector2.zero;
        handleGO.GetComponent<Image>().color = UITheme.Divider;

        var sb = sbGO.GetComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;
        sb.handleRect = hRT;
        sb.targetGraphic = handleGO.GetComponent<Image>();

        leftScroll.verticalScrollbar = sb;
        leftScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // content 영역의 드래그가 ScrollRect 스크롤을 흔들지 않도록 차단(휠/스크롤바로만 스크롤).
        var blocker = gridGO.AddComponent<ScrollDragBlocker>();
        blocker.targetScroll = leftScroll;

        leftPanelRoot.SetActive(false);

        // ── 우측 열 placeholder (창고/상자 미오픈 시 빈 칸 대신 안내) ──
        leftPlaceholder = new GameObject("LeftPlaceholder", typeof(RectTransform));
        leftPlaceholder.transform.SetParent(parent, false);
        var phRT = leftPlaceholder.GetComponent<RectTransform>();
        phRT.anchorMin = new Vector2(0.655f, 0.07f);  // leftPanel과 동일 영역
        phRT.anchorMax = new Vector2(0.99f, 0.93f);
        phRT.offsetMin = Vector2.zero;
        phRT.offsetMax = Vector2.zero;
        leftPlaceholder.AddComponent<Image>().color = UITheme.PanelAlt;
        var phTxt = MakeChildText(leftPlaceholder.transform,
            "파밍\n\n상자에 다가가 [E]\n수색하면 여기에 표시됩니다",
            14, UITheme.TextMuted);
        phTxt.alignment = TextAnchor.MiddleCenter;
    }

    /// <summary>우측 열 표시 상태 동기화: 창고/상자 열려있지 않으면 placeholder를 보인다.</summary>
    void SyncLeftPlaceholder()
    {
        if (leftPlaceholder == null) return;
        bool panelOpen = leftPanelRoot != null && leftPanelRoot.activeSelf;
        leftPlaceholder.SetActive(!panelOpen);
    }

    void BuildCharacterPanel(Transform parent)
    {
        var go = new GameObject("CharacterPanel");
        go.transform.SetParent(parent, false);
        charPanel = go.AddComponent<RectTransform>();
        charPanel.anchorMin = new Vector2(0.035f, 0.07f);
        charPanel.anchorMax = new Vector2(0.325f, 0.93f);
        charPanel.offsetMin = Vector2.zero;
        charPanel.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = UITheme.Panel;

        var title = MakeText(charPanel, "CharTitle", "캐릭터 상태",
            new Vector2(10, -8), new Vector2(PANEL_WIDTH - 20, 28), 16, UITheme.TextBright, TextAnchor.MiddleCenter);
        title.fontStyle = FontStyle.Bold;

        // ── 스탯 텍스트 ──
        float y = -48f;
        charHpText = MakeText(charPanel, "CharHp", "HP: 100 / 100",
            new Vector2(14, y), new Vector2(PANEL_WIDTH - 28, 22), 14, new Color(0.4f, 1f, 0.5f), TextAnchor.MiddleLeft);
        charHpText.fontStyle = FontStyle.Bold;
        y -= 24f;

        charStaminaText = MakeText(charPanel, "CharStamina", "스태미너: 100 / 100",
            new Vector2(14, y), new Vector2(PANEL_WIDTH - 28, 20), 12, new Color(0.4f, 0.85f, 0.95f), TextAnchor.MiddleLeft);
        y -= 22f;

        charWaterText = MakeText(charPanel, "CharWater", "수분: 100 / 100",
            new Vector2(14, y), new Vector2(PANEL_WIDTH - 28, 20), 12, new Color(0.4f, 0.7f, 1f), TextAnchor.MiddleLeft);
        y -= 22f;

        charFoodText = MakeText(charPanel, "CharFood", "포만감: 100 / 100",
            new Vector2(14, y), new Vector2(PANEL_WIDTH - 28, 20), 12, new Color(0.95f, 0.75f, 0.45f), TextAnchor.MiddleLeft);
        y -= 22f;

        charWeightText = MakeText(charPanel, "CharWeight", "무게: 0.0 / 30 kg",
            new Vector2(14, y), new Vector2(PANEL_WIDTH - 28, 20), 12, UITheme.TextMuted, TextAnchor.MiddleLeft);
        y -= 30f;

        // ── 장비 슬롯 (타르코프식) ──
        MakeText(charPanel, "EquipHdr", "── 장비 ──",
            new Vector2(14, y), new Vector2(PANEL_WIDTH - 28, 22), 13, UITheme.TextMuted, TextAnchor.MiddleCenter);
        y -= 28f;

        equipSlotBgs = new Dictionary<EquipSlot, Image>();
        equipSlotIcons = new Dictionary<EquipSlot, Image>();
        equipSlotLabels = new Dictionary<EquipSlot, Text>();

        // 슬롯 레이아웃: 3열 상단(Head/Armor/Rig), 3열 중단(Weapon1/Backpack/Weapon2), 1열 하단(Melee)
        float slotSize = 64f;
        float slotGap = 6f;
        float totalW = slotSize * 3 + slotGap * 2;
        float startX = (PANEL_WIDTH - totalW) * 0.5f;

        // 1행: Head / Armor / Rig
        BuildEquipSlot(charPanel, EquipSlot.Head, "헬멧", startX, y, slotSize);
        BuildEquipSlot(charPanel, EquipSlot.Armor, "방탄복", startX + slotSize + slotGap, y, slotSize);
        BuildEquipSlot(charPanel, EquipSlot.Rig, "조끼", startX + (slotSize + slotGap) * 2, y, slotSize);
        y -= slotSize + slotGap;

        // 2행: PrimaryWeapon / Backpack / SecondaryWeapon
        BuildEquipSlot(charPanel, EquipSlot.PrimaryWeapon, "주무기", startX, y, slotSize);
        BuildEquipSlot(charPanel, EquipSlot.Backpack, "가방", startX + slotSize + slotGap, y, slotSize);
        BuildEquipSlot(charPanel, EquipSlot.SecondaryWeapon, "보조", startX + (slotSize + slotGap) * 2, y, slotSize);
        y -= slotSize + slotGap;

        // 3행: 근접 + 특수창(시계/측정기 등) — 가운데 정렬 2칸
        float pairW = slotSize * 2 + slotGap;
        float pairX = (PANEL_WIDTH - pairW) * 0.5f;
        BuildEquipSlot(charPanel, EquipSlot.Melee, "근접", pairX, y, slotSize);
        BuildEquipSlot(charPanel, EquipSlot.Special, "특수창", pairX + slotSize + slotGap, y, slotSize);
        y -= slotSize + 10f;

        // ── 부위 상태 ──
        MakeText(charPanel, "BodyHdr", "── 부위 상태 ──",
            new Vector2(14, y), new Vector2(PANEL_WIDTH - 28, 22), 13, UITheme.TextMuted, TextAnchor.MiddleLeft);
        y -= 26f;

        charBodyTexts = new Text[PART_NAMES.Length];
        for (int i = 0; i < PART_NAMES.Length; i++)
        {
            charBodyTexts[i] = MakeText(charPanel, $"Body_{i}", $"{PART_NAMES[i]}: 정상",
                new Vector2(22, y), new Vector2(PANEL_WIDTH - 40, 20), 12, new Color(0.4f, 1f, 0.5f), TextAnchor.MiddleLeft);
            y -= 22f;
        }
    }

    void BuildEquipSlot(RectTransform parent, EquipSlot slot, string label, float x, float y, float size)
    {
        var go = new GameObject($"Slot_{slot}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(size, size);

        var bg = go.AddComponent<Image>();
        bg.color = UITheme.Cell;
        equipSlotBgs[slot] = bg;

        // 아이콘 (장착 시 표시)
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(go.transform, false);
        var iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = new Vector2(4, 12);
        iconRT.offsetMax = new Vector2(-4, -4);
        var icon = iconGO.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.enabled = false;
        equipSlotIcons[slot] = icon;

        // 슬롯 라벨 (하단)
        var lblTxt = MakeText(rt, $"Lbl_{slot}", label,
            new Vector2(0, 2), new Vector2(size, 14), 10, UITheme.TextMuted, TextAnchor.LowerCenter);
        lblTxt.alignment = TextAnchor.LowerCenter;
        var lblRT = lblTxt.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0, 0);
        lblRT.anchorMax = new Vector2(1, 0);
        lblRT.pivot = new Vector2(0.5f, 0);
        lblRT.anchoredPosition = new Vector2(0, 2);
        lblRT.sizeDelta = new Vector2(0, 14);
        equipSlotLabels[slot] = lblTxt;

        // 클릭 → 해제
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        var colors = btn.colors;
        colors.normalColor = bg.color;
        colors.highlightedColor = UITheme.CellHover;
        colors.pressedColor = UITheme.CellPressed;
        btn.colors = colors;
        var capturedSlot = slot;
        btn.onClick.AddListener(() => OnEquipSlotClicked(capturedSlot));
    }

    void OnEquipSlotClicked(EquipSlot slot)
    {
        if (playerEquipment == null) return;
        if (playerEquipment.GetSlot(slot) == null) return;

        // 좌클릭만으로는 해제하지 않는다(실수 방지). Ctrl+좌클릭일 때만 해제.
        // 일반 해제/제거는 우클릭 메뉴(착용해제/제거)로.
        if (!(GameInput.GetKey(KeyCode.LeftControl) || GameInput.GetKey(KeyCode.RightControl)))
            return;

        UnequipToInventory(slot);
    }

    /// <summary>해당 슬롯의 착용 아이템을 인벤(가방/주머니/보안)으로 해제. 부착물 보존 위해 실제 인스턴스 회수.</summary>
    void UnequipToInventory(EquipSlot slot)
    {
        if (playerEquipment == null) return;
        var equipped = playerEquipment.GetSlot(slot);
        if (equipped == null) return;

        var inst = playerEquipment.GetSlotInstance(slot) ?? new ItemInstance(equipped, 1);

        // 가방: 휴대 격자 내용물을 가방 안으로 먼저 담고(함께 보관), 해제 후 가방을 주머니/창고로.
        if (slot == EquipSlot.Backpack && playerInventory != null)
        {
            playerInventory.TransferBagToContainer(inst);
            playerEquipment.Unequip(slot);   // 휴대 격자 0×0
            // 가방은 창고로 우선 복귀(레이드면 인벤 우선). 창고 가득이면 주머니·보안.
            if (!StoreItemPreferStash(inst))
                ToastManager.Show("가방 둘 공간이 없다 (창고·주머니 가득)", ToastManager.ToastType.Warning);
            RefreshAllGrids();
            return;
        }

        if (playerInventory != null && playerInventory.TryAutoPlaceAnywhere(inst))
        {
            playerEquipment.Unequip(slot);
            RefreshAllGrids();
        }
        else
        {
            ToastManager.Show("인벤토리 공간 부족", ToastManager.ToastType.Warning);
        }
    }

    /// <summary>메인 창고에 자동 배치 시도.</summary>
    bool TryPlaceInStash(ItemInstance inst)
    {
        var stash = MainStash.Instance != null ? MainStash.Instance : MainStash.Ensure();
        return stash != null && inst != null && stash.GetGrid().TryAutoPlace(inst);
    }

    /// <summary>보관 위치 배치 — 안전구역=창고 우선→인벤, 레이드(창고 없음)=인벤 우선→창고. 실패 시 false.</summary>
    bool StoreItemPreferStash(ItemInstance item)
    {
        if (item == null) return false;
        if (IsSafeArea())
            return TryPlaceInStash(item)
                || (playerInventory != null && playerInventory.TryAutoPlaceAnywhere(item));
        return (playerInventory != null && playerInventory.TryAutoPlaceAnywhere(item))
            || TryPlaceInStash(item);
    }

    /// <summary>착용 아이템을 완전히 제거(해제 후 레이드=바닥 산포 / 안전구역=인벤·창고 복귀).</summary>
    void RemoveEquipped(EquipSlot slot)
    {
        if (playerEquipment == null) return;
        var equipped = playerEquipment.GetSlot(slot);
        if (equipped == null) return;

        var inst = playerEquipment.GetSlotInstance(slot) ?? new ItemInstance(equipped, 1);
        // 가방이면 휴대 내용물을 가방에 먼저 담아 함께 처리.
        if (slot == EquipSlot.Backpack && playerInventory != null)
            playerInventory.TransferBagToContainer(inst);
        playerEquipment.Unequip(slot);
        DropOrReturnItem(inst);
        RefreshAllGrids();
    }

    void UpdateCharacterPanel()
    {
        if (charHpText != null && health != null)
        {
            charHpText.text = $"HP: {health.CurrentHp:F0} / {health.MaxHp:F0}";
            float hpPct = health.MaxHp > 0 ? health.CurrentHp / health.MaxHp : 0f;
            charHpText.color = hpPct > 0.6f ? new Color(0.4f, 1f, 0.5f)
                : hpPct > 0.3f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.35f, 0.3f);
        }
        if (charStaminaText != null)
        {
            var p = TopDownPlayer.Instance;
            if (p != null) charStaminaText.text = $"스태미너: {p.StaminaPercent * 100f:F0} / 100";
        }
        if (charWaterText != null || charFoodText != null)
        {
            var s = SurvivalStats.Get();
            if (s != null)
            {
                if (charWaterText != null)
                {
                    charWaterText.text = $"수분: {s.Water:F0} / 100";
                    charWaterText.color = s.Water <= 0f ? new Color(1f, 0.35f, 0.3f)
                        : s.Water <= 20f ? new Color(1f, 0.85f, 0.3f) : new Color(0.4f, 0.7f, 1f);
                }
                if (charFoodText != null)
                {
                    charFoodText.text = $"포만감: {s.Satiety:F0} / 100";
                    charFoodText.color = s.Satiety <= 0f ? new Color(1f, 0.35f, 0.3f)
                        : s.Satiety <= 20f ? new Color(1f, 0.85f, 0.3f) : new Color(0.95f, 0.75f, 0.45f);
                }
            }
        }

        if (charWeightText != null && playerInventory != null)
            charWeightText.text = $"무게: {playerInventory.CurrentWeight:F1} / {playerInventory.MaxWeight:F0} kg";

        // 장비 슬롯 시각 갱신
        UpdateEquipSlots();

        // 부위별 의료 상태
        if (charBodyTexts != null && medical != null)
        {
            var parts = medical.GetAllParts();
            for (int i = 0; i < parts.Length && i < charBodyTexts.Length; i++)
            {
                if (charBodyTexts[i] == null) continue;
                if (parts[i].IsInjured)
                {
                    string st = "";
                    if (parts[i].HasInjury(InjuryType.Bleeding)) st += "출혈 ";
                    if (parts[i].HasInjury(InjuryType.Fracture)) st += "골절 ";
                    if (parts[i].HasInjury(InjuryType.Pain)) st += "통증 ";
                    charBodyTexts[i].text = $"{PART_NAMES[i]}: {st.TrimEnd()}";
                    charBodyTexts[i].color = parts[i].HasInjury(InjuryType.Fracture) ? new Color(1f, 0.2f, 0.2f)
                        : parts[i].HasInjury(InjuryType.Bleeding) ? new Color(1f, 0.5f, 0.2f)
                        : new Color(1f, 1f, 0.3f);
                }
                else
                {
                    charBodyTexts[i].text = $"{PART_NAMES[i]}: 정상";
                    charBodyTexts[i].color = new Color(0.4f, 1f, 0.5f);
                }
            }
        }
    }

    void UpdateEquipSlots()
    {
        if (equipSlotBgs == null || equipSlotIcons == null) return;

        foreach (var kv in equipSlotBgs)
        {
            var slot = kv.Key;
            var bg = kv.Value;
            Image icon;
            Text lbl;
            equipSlotIcons.TryGetValue(slot, out icon);
            equipSlotLabels.TryGetValue(slot, out lbl);

            var equipped = playerEquipment != null ? playerEquipment.GetSlot(slot) : null;
            bool hasItem = equipped != null;

            // 배경색: 장착됨이면 밝게. 선택 아이템이 들어갈 수 있는 슬롯이면 하이라이트.
            bool highlighted = highlightSlots.Count > 0 && highlightSlots.Contains(slot);
            Color bgColor = highlighted
                ? UITheme.Accent
                : hasItem
                    ? UITheme.CellHover
                    : UITheme.Cell;
            bg.color = bgColor;
            // 버튼 ColorTint가 normalColor로 되돌리지 않도록 동기화
            var slotBtn = bg.GetComponent<Button>();
            if (slotBtn != null)
            {
                var c = slotBtn.colors;
                c.normalColor = bgColor;
                slotBtn.colors = c;
            }

            // 아이콘
            if (icon != null)
            {
                if (hasItem && equipped.icon != null)
                {
                    icon.sprite = equipped.icon;
                    icon.color = Color.white;
                    icon.enabled = true;
                }
                else
                {
                    icon.enabled = false;
                }
            }

            // 라벨: 장착됨이면 아이템 이름, 아니면 슬롯 이름
            if (lbl != null)
            {
                if (hasItem)
                {
                    lbl.text = equipped.displayName;
                    lbl.color = equipped.RarityColor;
                }
                else
                {
                    lbl.color = UITheme.TextMuted;
                    // 기본 라벨은 빌드 시 설정된 것 유지
                }
            }
        }
    }

    #endregion

    #region 인벤토리 탭 빌드

    /// <summary>중앙 패널 전체 갱신: 가방·주머니·보안 격자를 렌더하고 세로 스택 위치를 잡는다.</summary>
    void RefreshInventoryGrid()
    {
        if (midContentRoot == null || playerInventory == null) return;

        var bag = playerInventory.Grid;
        bool hasBackpack = bag != null && bag.width > 0 && bag.height > 0;

        // 가방 헤더
        if (bagHeaderText != null)
        {
            var bp = playerEquipment != null ? playerEquipment.GetSlot(EquipSlot.Backpack) : null;
            bagHeaderText.text = hasBackpack ? (bp != null ? $"가방 — {bp.displayName}" : "가방") : "가방 (미장착)";
            bagHeaderText.color = hasBackpack ? UITheme.AccentBright : UITheme.TextDim;
        }

        // ── 격자 렌더 ──
        RenderPlayerGrid(bag, invGridRoot);
        RenderPlayerGrid(playerInventory.PocketsGrid, pocketsGridRoot);
        RenderPlayerGrid(playerInventory.SecureGrid, secureGridRoot);

        // 가방 미장착 시 격자 숨기고 안내 박스 표시
        if (invGridRoot != null) invGridRoot.gameObject.SetActive(hasBackpack);
        if (invPlaceholder != null) invPlaceholder.gameObject.SetActive(!hasBackpack);

        RefreshWeaponParts();
        LayoutMiddleStack(hasBackpack, bag);
    }

    static readonly WeaponPartType[] PartOrder = { WeaponPartType.Scope, WeaponPartType.Muzzle, WeaponPartType.Magazine, WeaponPartType.Grip };
    static readonly string[] PartLabels = { "조준경", "소염기", "탄창", "손잡이" };

    /// <summary>무기 파츠 예약 공간에 장착 무기의 4개 파츠 슬롯 렌더(부착=아이콘+클릭 분리 / 빈칸=종류 라벨).</summary>
    void RefreshWeaponParts()
    {
        if (weaponBox == null) return;
        for (int i = weaponBox.childCount - 1; i >= 0; i--)
            Destroy(weaponBox.GetChild(i).gameObject);

        var wpn = playerEquipment != null ? playerEquipment.GetSlotInstance(EquipSlot.PrimaryWeapon) : null;

        MakeText(weaponBox, "WPHdr", wpn != null ? $"무기 파츠 — {wpn.data.displayName}" : "무기 파츠",
            new Vector2(8, -4), new Vector2(MID_INNER_W - 16, 18), 12,
            wpn != null ? new Color(0.85f, 0.8f, 0.6f) : UITheme.TextMuted, TextAnchor.MiddleLeft);

        if (wpn == null)
        {
            var t = MakeText(weaponBox, "WPNone", "무기를 장착하면 파츠 슬롯이 열립니다",
                new Vector2(8, -26), new Vector2(MID_INNER_W - 16, 40), 11, new Color(0.4f, 0.45f, 0.55f), TextAnchor.MiddleCenter);
            return;
        }

        const float cell = 50f, gap = 8f, startX = 10f;
        for (int i = 0; i < PartOrder.Length; i++)
        {
            var type = PartOrder[i];
            string attId = wpn.GetAttachment(type);
            var attData = string.IsNullOrEmpty(attId) ? null : ItemDatabase.Get(attId);

            var cellGO = new GameObject($"Part_{type}", typeof(RectTransform), typeof(Image), typeof(Button));
            cellGO.transform.SetParent(weaponBox, false);
            var rt = cellGO.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(cell, cell);
            rt.anchoredPosition = new Vector2(startX + i * (cell + gap), -26);
            cellGO.GetComponent<Image>().color = attData != null
                ? GetRarityBgColor(attData.rarity)
                : new Color(0.13f, 0.13f, 0.17f, 0.9f);

            if (attData != null)
            {
                var capType = type;
                cellGO.GetComponent<Button>().onClick.AddListener(() => DetachPart(capType));
                if (attData.icon != null)
                {
                    var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconGO.transform.SetParent(cellGO.transform, false);
                    var icRT = iconGO.GetComponent<RectTransform>();
                    icRT.anchorMin = Vector2.zero; icRT.anchorMax = Vector2.one;
                    icRT.offsetMin = new Vector2(4, 4); icRT.offsetMax = new Vector2(-4, -4);
                    var ic = iconGO.GetComponent<Image>();
                    ic.sprite = attData.icon; ic.preserveAspect = true; ic.raycastTarget = false;
                }
                else
                {
                    var nm = MakeChildText(cellGO.transform, attData.displayName, 10, attData.RarityColor);
                    nm.alignment = TextAnchor.MiddleCenter;
                    nm.raycastTarget = false;
                }
            }
            else
            {
                var t = MakeChildText(cellGO.transform, PartLabels[i], 10, new Color(0.5f, 0.55f, 0.62f));
                t.alignment = TextAnchor.MiddleCenter;
                t.raycastTarget = false;
            }
        }
    }

    /// <summary>드래그 중인 파츠가 weaponBox의 같은 종류 슬롯 위에 떨어졌으면 부착. 처리했으면 true(제스처 소비).</summary>
    bool TryAttachDraggedToPartSlot()
    {
        if (weaponBox == null) return false;
        var wpn = playerEquipment != null ? playerEquipment.GetSlotInstance(EquipSlot.PrimaryWeapon) : null;
        if (wpn == null) return false;

        var type = dragItem.data.weaponPartType;
        var cell = weaponBox.Find($"Part_{type}") as RectTransform;
        if (cell == null) return false;
        if (!RectTransformUtility.RectangleContainsScreenPoint(cell, GameInput.mousePosition, null)) return false;

        if (!string.IsNullOrEmpty(wpn.GetAttachment(type)))
        {
            ToastManager.Show("이미 부착됨 — 먼저 분리", ToastManager.ToastType.Warning);
            CancelDrag();   // 원위치 복귀
            return true;
        }

        wpn.SetAttachment(type, dragItem.data.itemId);
        if (dragItem.stackCount > 1)
        {
            dragItem.stackCount--;
            ReturnItemToInventory(dragItem);   // 남은 스택 복귀
        }
        ToastManager.Show($"{dragItem.data.displayName} 부착", ToastManager.ToastType.Info);
        EndDrag();   // 고스트 제거 + RefreshAllGrids
        return true;
    }

    /// <summary>파츠를 장착 무기에 부착(컨텍스트 메뉴 "부착"에서 호출). 같은 종류 이미 있으면 거부.</summary>
    void AttachPartFromGrid(InventoryGrid.PlacedItem placed, InventoryGrid grid)
    {
        if (placed == null || placed.item == null || placed.item.data == null) return;
        var pdata = placed.item.data;
        if (!pdata.IsWeaponPart) return;
        var wpn = playerEquipment != null ? playerEquipment.GetSlotInstance(EquipSlot.PrimaryWeapon) : null;
        if (wpn == null) { ToastManager.Show("무기를 먼저 장착하세요", ToastManager.ToastType.Warning); return; }
        if (!string.IsNullOrEmpty(wpn.GetAttachment(pdata.weaponPartType)))
        { ToastManager.Show("이미 부착됨 — 먼저 분리", ToastManager.ToastType.Warning); return; }

        wpn.SetAttachment(pdata.weaponPartType, pdata.itemId);
        if (placed.item.stackCount > 1) placed.item.stackCount--; else grid.Remove(placed);
        ToastManager.Show($"{pdata.displayName} 부착", ToastManager.ToastType.Info);
        RefreshAllGrids();
    }

    /// <summary>장착 무기에서 파츠 분리 → 인벤(없으면 창고) 회수.</summary>
    void DetachPart(WeaponPartType type)
    {
        var wpn = playerEquipment != null ? playerEquipment.GetSlotInstance(EquipSlot.PrimaryWeapon) : null;
        if (wpn == null) return;
        string id = wpn.GetAttachment(type);
        if (string.IsNullOrEmpty(id)) return;
        var d = ItemDatabase.Get(id);
        if (d == null) { wpn.SetAttachment(type, null); RefreshAllGrids(); return; }

        var part = new ItemInstance(d, 1);
        if (playerInventory != null && playerInventory.TryAutoPlaceAnywhere(part))
        {
            wpn.SetAttachment(type, null);
            ToastManager.Show($"{d.displayName} 분리", ToastManager.ToastType.Info);
            RefreshAllGrids();
        }
        else ToastManager.Show("공간 부족 — 분리 불가", ToastManager.ToastType.Warning);
    }

    /// <summary>세로 스택(무기파츠 → 가방 → 주머니 → 보안 → 무게) 위치를 위에서부터 잡는다.</summary>
    void LayoutMiddleStack(bool hasBackpack, InventoryGrid bag)
    {
        int cellTotal = CELL_SIZE + CELL_GAP;
        float y = 0f;

        // 무기 파츠 예약 공간
        if (weaponBox != null)
        {
            weaponBox.anchoredPosition = new Vector2((MID_INNER_W - weaponBox.sizeDelta.x) * 0.5f, y);
            y -= weaponBox.sizeDelta.y + SECTION_GAP;
        }

        // 가방
        y = PlaceHeader(bagHeaderText, y);
        if (hasBackpack)
        {
            float bagW = bag.width * cellTotal;
            CenterGrid(invGridRoot, bagW, y);
            y -= bag.height * cellTotal + SECTION_GAP;
        }
        else if (invPlaceholder != null)
        {
            invPlaceholder.anchoredPosition = new Vector2((MID_INNER_W - invPlaceholder.sizeDelta.x) * 0.5f, y);
            y -= invPlaceholder.sizeDelta.y + SECTION_GAP;
        }

        // 주머니 (고정 4칸)
        y = PlaceHeader(pocketsHeaderText, y);
        var pockets = playerInventory.PocketsGrid;
        if (pockets != null)
        {
            CenterGrid(pocketsGridRoot, pockets.width * cellTotal, y);
            y -= pockets.height * cellTotal + SECTION_GAP;
        }

        // 보안 컨테이너 (고정 3×3)
        y = PlaceHeader(secureHeaderText, y);
        var secure = playerInventory.SecureGrid;
        if (secure != null)
        {
            CenterGrid(secureGridRoot, secure.width * cellTotal, y);
            y -= secure.height * cellTotal + SECTION_GAP;
        }

        // 무게
        if (invWeightText != null)
        {
            var wRT = invWeightText.GetComponent<RectTransform>();
            wRT.anchoredPosition = new Vector2(2, y);
        }
    }

    /// <summary>헤더를 y에 배치하고 그 아래 y를 반환.</summary>
    float PlaceHeader(Text header, float y)
    {
        if (header != null)
            header.GetComponent<RectTransform>().anchoredPosition = new Vector2(2, y);
        return y - SECTION_HDR_H;
    }

    /// <summary>격자 루트를 가로 중앙(MID_INNER_W 기준) + 세로 y에 배치.</summary>
    void CenterGrid(RectTransform gridRoot, float gridW, float y)
    {
        if (gridRoot == null) return;
        gridRoot.anchoredPosition = new Vector2((MID_INNER_W - gridW) * 0.5f, y);
    }

    /// <summary>플레이어 격자 하나(가방/주머니/보안)를 gridRoot 자식으로 렌더: 빈 칸 + 아이템 + 점유색.</summary>
    void RenderPlayerGrid(InventoryGrid grid, RectTransform gridRoot)
    {
        if (gridRoot == null) return;

        for (int i = gridRoot.childCount - 1; i >= 0; i--)
            Destroy(gridRoot.GetChild(i).gameObject);

        if (grid == null || grid.width <= 0 || grid.height <= 0)
        {
            gridRoot.sizeDelta = new Vector2(0, 0);
            return;
        }

        int cellTotal = CELL_SIZE + CELL_GAP;
        gridRoot.sizeDelta = new Vector2(grid.width * cellTotal, grid.height * cellTotal);

        var slotImages = new Image[grid.width, grid.height];
        for (int gy = 0; gy < grid.height; gy++)
        {
            for (int gx = 0; gx < grid.width; gx++)
            {
                var slotGO = new GameObject($"Slot_{gx}_{gy}");
                slotGO.transform.SetParent(gridRoot, false);
                var rt = slotGO.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(gx * cellTotal, -gy * cellTotal);
                rt.sizeDelta = new Vector2(CELL_SIZE, CELL_SIZE);

                var img = slotGO.AddComponent<Image>();
                img.color = UITheme.PanelAlt;
                slotImages[gx, gy] = img;
            }
        }

        RenderItemsInto(grid, gridRoot, slotImages);
    }

    /// <summary>격자에 배치된 아이템을 gridRoot에 렌더(아이콘/이름/내구도/스택 + 점유칸 색상).</summary>
    void RenderItemsInto(InventoryGrid grid, RectTransform gridRoot, Image[,] slotImages)
    {
        int cellTotal = CELL_SIZE + CELL_GAP;
        var placed = grid.GetAll();

        for (int i = 0; i < placed.Count; i++)
        {
            var p = placed[i];
            if (p.item.data == null) continue;

            int w = p.EffectiveWidth;
            int h = p.EffectiveHeight;

            // 아이템 배경
            var itemGO = new GameObject($"Item_{p.item.uid}");
            itemGO.transform.SetParent(gridRoot, false);
            var rt = itemGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(
                p.gridX * cellTotal,
                -p.gridY * cellTotal);
            rt.sizeDelta = new Vector2(
                w * CELL_SIZE + (w - 1) * CELL_GAP,
                h * CELL_SIZE + (h - 1) * CELL_GAP);

            var bg = itemGO.AddComponent<Image>();
            bg.color = GetRarityBgColor(p.item.data.rarity);

            // 아이콘 (있으면)
            if (p.item.data.icon != null)
            {
                var iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(itemGO.transform, false);
                var iconRT = iconGO.AddComponent<RectTransform>();
                iconRT.anchorMin = Vector2.zero;
                iconRT.anchorMax = Vector2.one;
                iconRT.offsetMin = new Vector2(4, 4);
                iconRT.offsetMax = new Vector2(-4, -4);
                var iconImg = iconGO.AddComponent<Image>();
                iconImg.sprite = p.item.data.icon;
                iconImg.preserveAspect = true;
            }
            else
            {
                var nameText = MakeChildText(itemGO.transform, p.item.data.displayName, 11, Color.white);
                nameText.alignment = TextAnchor.MiddleCenter;
            }

            // 내구도 바 (hasDurability 아이템)
            if (p.item.HasDurability)
            {
                var durBgGO = new GameObject("DurBg");
                durBgGO.transform.SetParent(itemGO.transform, false);
                var durBgRT = durBgGO.AddComponent<RectTransform>();
                durBgRT.anchorMin = new Vector2(0, 0);
                durBgRT.anchorMax = new Vector2(1, 0);
                durBgRT.pivot = new Vector2(0, 0);
                durBgRT.anchoredPosition = new Vector2(2, 2);
                durBgRT.sizeDelta = new Vector2(-4, 6);
                durBgGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);

                var durFillGO = new GameObject("DurFill");
                durFillGO.transform.SetParent(durBgGO.transform, false);
                var durFillRT = durFillGO.AddComponent<RectTransform>();
                durFillRT.anchorMin = Vector2.zero;
                durFillRT.anchorMax = new Vector2(p.item.DurabilityRatio, 1f);
                durFillRT.offsetMin = Vector2.zero;
                durFillRT.offsetMax = Vector2.zero;
                float ratio = p.item.DurabilityRatio;
                durFillGO.AddComponent<Image>().color = ratio > 0.5f ? new Color(0.3f, 0.9f, 0.4f)
                    : ratio > 0.2f ? new Color(0.9f, 0.8f, 0.2f)
                    : new Color(0.9f, 0.2f, 0.2f);
            }
            // 스택 수 (1 초과일 때, 내구도 아이템 아닌 경우)
            else if (p.item.stackCount > 1)
            {
                var stackGO = new GameObject("Stack");
                stackGO.transform.SetParent(itemGO.transform, false);
                var stackRT = stackGO.AddComponent<RectTransform>();
                stackRT.anchorMin = new Vector2(1, 0);
                stackRT.anchorMax = new Vector2(1, 0);
                stackRT.pivot = new Vector2(1, 0);
                stackRT.anchoredPosition = new Vector2(-2, 2);
                stackRT.sizeDelta = new Vector2(30, 16);

                var stackTxt = stackGO.AddComponent<Text>();
                stackTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                stackTxt.fontSize = 11;
                stackTxt.fontStyle = FontStyle.Bold;
                stackTxt.color = Color.white;
                stackTxt.text = $"x{p.item.stackCount}";
                stackTxt.alignment = TextAnchor.LowerRight;

                var shadow = stackGO.AddComponent<Shadow>();
                shadow.effectColor = Color.black;
                shadow.effectDistance = new Vector2(1, -1);
            }

            // 점유 칸 색상 변경
            for (int gx = p.gridX; gx < p.gridX + w; gx++)
                for (int gy = p.gridY; gy < p.gridY + h; gy++)
                    if (gx < grid.width && gy < grid.height && slotImages != null)
                        slotImages[gx, gy].color = UITheme.Gridline;
        }
    }

    void UpdateInventoryTab()
    {
        if (playerInventory == null) return;

        if (invWeightText != null)
        {
            float cur = playerInventory.CurrentWeight;
            float max = playerInventory.MaxWeight;
            Color wc = cur > max ? UITheme.Negative : UITheme.TextMuted;
            invWeightText.color = wc;
            int itemCount = (playerInventory.Grid != null ? playerInventory.Grid.ItemCount : 0)
                + (playerInventory.PocketsGrid != null ? playerInventory.PocketsGrid.ItemCount : 0)
                + (playerInventory.SecureGrid != null ? playerInventory.SecureGrid.ItemCount : 0);
            invWeightText.text = $"무게: {cur:F1} / {max:F0} kg  |  아이템: {itemCount}개";
        }
    }

    #endregion

    #region 좌측 상자

    void ShowLeftPanel(InventoryGrid grid, string title, bool withSearch = false)
    {
        if (leftPanelRoot == null || grid == null) return;

        leftPanelRoot.SetActive(true);
        leftTitleText.text = title;
        leftPanelSearchEnabled = withSearch;
        SyncLeftPlaceholder();   // 패널 열렸으니 안내 숨김

        RefreshLeftGrid(grid);

        if (withSearch)
            StartSearch(grid);
        else
            StopSearch(true); // 창고: 전부 즉시 공개
    }

    void RefreshLeftGrid(InventoryGrid grid)
    {
        if (containerGridRoot == null || grid == null) return;

        // 기존 자식 제거
        for (int i = containerGridRoot.childCount - 1; i >= 0; i--)
            Destroy(containerGridRoot.GetChild(i).gameObject);
        int cellTotal = CELL_SIZE + CELL_GAP;

        // 스크롤 content 높이를 격자 줄 수에 맞춤 (창고 30~100줄)
        containerGridRoot.sizeDelta = new Vector2(grid.width * cellTotal, grid.height * cellTotal + 4);

        containerSlotImages = new Image[grid.width, grid.height];
        for (int gy = 0; gy < grid.height; gy++)
        {
            for (int gx = 0; gx < grid.width; gx++)
            {
                var slotGO = new GameObject($"CSlot_{gx}_{gy}");
                slotGO.transform.SetParent(containerGridRoot, false);
                var rt = slotGO.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(gx * cellTotal, -gy * cellTotal);
                rt.sizeDelta = new Vector2(CELL_SIZE, CELL_SIZE);

                var img = slotGO.AddComponent<Image>();
                img.color = UITheme.PanelAlt;
                containerSlotImages[gx, gy] = img;
            }
        }

        // 아이템 표시 (인벤토리와 동일 패턴)
        RefreshContainerItems(grid, containerGridRoot);
    }

    /// <summary>정렬 버튼 → 현재 열린 창고/상자 격자를 자동 정렬 후 다시 그림.</summary>
    void SortLeftGrid()
    {
        var grid = LeftGrid;
        if (grid == null) return;
        // 수색 중인 루팅 상자는 정렬 금지(공개 전 위치 흔들림 방지)
        if (leftPanelSearchEnabled && isSearching) return;
        SortGrid(grid);
        RefreshLeftGrid(grid);
    }

    /// <summary>격자 내 아이템을 비우고 큰 것→카테고리→이름 순으로 재배치(촘촘히 패킹).</summary>
    void SortGrid(InventoryGrid grid)
    {
        var placed = grid.GetAll();   // 복사본
        var items = new System.Collections.Generic.List<ItemInstance>();
        foreach (var p in placed) { items.Add(p.item); grid.Remove(p); }

        items.Sort((a, b) =>
        {
            int sa = a.data.gridWidth * a.data.gridHeight;
            int sb = b.data.gridWidth * b.data.gridHeight;
            if (sb != sa) return sb - sa;                       // 큰 것 먼저(패킹 효율)
            int ca = (int)a.data.category, cb = (int)b.data.category;
            if (ca != cb) return ca - cb;                       // 카테고리
            return string.Compare(a.data.displayName, b.data.displayName, System.StringComparison.Ordinal);
        });

        foreach (var it in items)
            grid.TryAutoPlace(it);   // 같은 격자에서 뺀 것이라 공간은 충분
    }

    void RefreshContainerItems(InventoryGrid grid, RectTransform gridRoot, bool applySearch = true)
    {
        int cellTotal = CELL_SIZE + CELL_GAP;
        var placed = grid.GetAll();

        for (int i = 0; i < placed.Count; i++)
        {
            var p = placed[i];
            if (p.item.data == null) continue;

            int w = p.EffectiveWidth;
            int h = p.EffectiveHeight;
            float itemW = w * CELL_SIZE + (w - 1) * CELL_GAP;
            float itemH = h * CELL_SIZE + (h - 1) * CELL_GAP;

            // ── 수색 상태 판별 ──
            bool revealed = !applySearch || !leftPanelSearchEnabled
                || (revealedUids != null && revealedUids.Contains(p.item.uid));
            bool currentlySearching = applySearch && leftPanelSearchEnabled
                && searchingItem != null && searchingItem.item.uid == p.item.uid;

            // 공통 배경 GO
            var itemGO = new GameObject($"CItem_{p.item.uid}");
            itemGO.transform.SetParent(gridRoot, false);
            var rt = itemGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(p.gridX * cellTotal, -p.gridY * cellTotal);
            rt.sizeDelta = new Vector2(itemW, itemH);

            var bg = itemGO.AddComponent<Image>();

            // ── 미공개: 어두운 슬롯 + "?" ──
            if (!revealed && !currentlySearching)
            {
                bg.color = UITheme.PanelAlt;
                var qTxt = MakeChildText(itemGO.transform, "?", 18, UITheme.TextDim);
                qTxt.fontStyle = FontStyle.Bold;
                continue;
            }

            // ── 수색 중: 펄스 배경 + 프로그레스 바 ──
            if (currentlySearching)
            {
                // 펄스: 밝기가 시간에 따라 변함
                float pulse = 0.5f + 0.15f * Mathf.Sin(Time.unscaledTime * 4f);
                bg.color = new Color(pulse * 0.4f, pulse * 0.35f, pulse * 0.2f, 0.95f);

                // "수색 중" 텍스트
                var searchTxt = MakeChildText(itemGO.transform, "...", 14, UITheme.Gold);
                searchTxt.fontStyle = FontStyle.Bold;

                // 하단 프로그레스 바
                float progress = searchDelay > 0 ? 1f - (searchTimer / searchDelay) : 1f;

                var barBg = new GameObject("BarBg");
                barBg.transform.SetParent(itemGO.transform, false);
                var barBgRT = barBg.AddComponent<RectTransform>();
                barBgRT.anchorMin = new Vector2(0, 0);
                barBgRT.anchorMax = new Vector2(1, 0);
                barBgRT.pivot = new Vector2(0, 0);
                barBgRT.anchoredPosition = new Vector2(2, 2);
                barBgRT.sizeDelta = new Vector2(-4, 5);
                barBg.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);

                var barFill = new GameObject("BarFill");
                barFill.transform.SetParent(barBg.transform, false);
                var barFillRT = barFill.AddComponent<RectTransform>();
                barFillRT.anchorMin = Vector2.zero;
                barFillRT.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
                barFillRT.offsetMin = Vector2.zero;
                barFillRT.offsetMax = Vector2.zero;
                barFill.AddComponent<Image>().color = UITheme.Gold;
                continue;
            }

            // ── 공개됨: 정상 렌더링 ──
            bg.color = GetRarityBgColor(p.item.data.rarity);

            if (p.item.data.icon != null)
            {
                var iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(itemGO.transform, false);
                var iconRT = iconGO.AddComponent<RectTransform>();
                iconRT.anchorMin = Vector2.zero;
                iconRT.anchorMax = Vector2.one;
                iconRT.offsetMin = new Vector2(4, 4);
                iconRT.offsetMax = new Vector2(-4, -4);
                var iconImg = iconGO.AddComponent<Image>();
                iconImg.sprite = p.item.data.icon;
                iconImg.preserveAspect = true;
            }
            else
            {
                MakeChildText(itemGO.transform, p.item.data.displayName, 11, Color.white);
            }

            // 내구도 바 또는 스택 수
            if (p.item.HasDurability)
            {
                var durBgGO = new GameObject("DurBg");
                durBgGO.transform.SetParent(itemGO.transform, false);
                var durBgRT = durBgGO.AddComponent<RectTransform>();
                durBgRT.anchorMin = new Vector2(0, 0);
                durBgRT.anchorMax = new Vector2(1, 0);
                durBgRT.pivot = new Vector2(0, 0);
                durBgRT.anchoredPosition = new Vector2(2, 2);
                durBgRT.sizeDelta = new Vector2(-4, 6);
                durBgGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);

                var durFillGO = new GameObject("DurFill");
                durFillGO.transform.SetParent(durBgGO.transform, false);
                var durFillRT = durFillGO.AddComponent<RectTransform>();
                durFillRT.anchorMin = Vector2.zero;
                durFillRT.anchorMax = new Vector2(p.item.DurabilityRatio, 1f);
                durFillRT.offsetMin = Vector2.zero;
                durFillRT.offsetMax = Vector2.zero;
                float ratio = p.item.DurabilityRatio;
                durFillGO.AddComponent<Image>().color = ratio > 0.5f ? new Color(0.3f, 0.9f, 0.4f)
                    : ratio > 0.2f ? new Color(0.9f, 0.8f, 0.2f)
                    : new Color(0.9f, 0.2f, 0.2f);
            }
            else if (p.item.stackCount > 1)
            {
                var stackGO = new GameObject("Stack");
                stackGO.transform.SetParent(itemGO.transform, false);
                var stackRT = stackGO.AddComponent<RectTransform>();
                stackRT.anchorMin = new Vector2(1, 0);
                stackRT.anchorMax = new Vector2(1, 0);
                stackRT.pivot = new Vector2(1, 0);
                stackRT.anchoredPosition = new Vector2(-2, 2);
                stackRT.sizeDelta = new Vector2(30, 16);

                var stackTxt = stackGO.AddComponent<Text>();
                stackTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                stackTxt.fontSize = 11;
                stackTxt.fontStyle = FontStyle.Bold;
                stackTxt.color = Color.white;
                stackTxt.text = $"x{p.item.stackCount}";
                stackTxt.alignment = TextAnchor.LowerRight;
            }
        }
    }

    void UpdateContainerGrid()
    {
        if (isSearching)
            UpdateSearch();
    }

    // ── 수색 연출 로직 ──

    void StartSearch(InventoryGrid grid)
    {
        StopSearch(false);
        lastSearchedGrid = grid;

        var all = grid.GetAll();
        if (all.Count == 0) return;

        // 레어도 낮은 것부터 (Common → Legendary)
        all.Sort((a, b) => ((int)a.item.data.rarity).CompareTo((int)b.item.data.rarity));

        revealedUids = new System.Collections.Generic.HashSet<int>();
        searchQueue = new System.Collections.Generic.Queue<InventoryGrid.PlacedItem>();
        for (int i = 0; i < all.Count; i++)
            searchQueue.Enqueue(all[i]);

        totalSearchItems = all.Count;
        revealedCount = 0;
        isSearching = true;
        searchingItem = null;

        AdvanceSearch();
        UpdateSearchStatusText();
    }

    void StopSearch(bool revealAll)
    {
        isSearching = false;
        searchingItem = null;
        searchTimer = 0f;
        searchDelay = 0f;

        if (revealAll && lastSearchedGrid != null)
        {
            // 전부 공개 처리
            if (revealedUids == null)
                revealedUids = new System.Collections.Generic.HashSet<int>();
            var all = lastSearchedGrid.GetAll();
            for (int i = 0; i < all.Count; i++)
                revealedUids.Add(all[i].item.uid);
            revealedCount = all.Count;
            totalSearchItems = all.Count;
        }

        // 수색 완료 플래그 → 재오픈 시 즉시 공개
        MarkContainerSearched();

        if (searchQueue != null) searchQueue.Clear();
        UpdateSearchStatusText();
    }

    void MarkContainerSearched()
    {
        if (openContainer != null)
            openContainer.HasBeenSearched = true;
    }

    void AdvanceSearch()
    {
        if (searchQueue == null || searchQueue.Count == 0)
        {
            // 모든 아이템 수색 완료
            isSearching = false;
            searchingItem = null;
            MarkContainerSearched();
            UpdateSearchStatusText();
            RefreshLeftGrid(lastSearchedGrid);
            return;
        }

        searchingItem = searchQueue.Dequeue();
        searchDelay = GetSearchDelay(searchingItem.item.data.rarity);
        searchTimer = searchDelay;
    }

    void UpdateSearch()
    {
        if (!isSearching || searchingItem == null) return;

        searchTimer -= Time.unscaledDeltaTime;

        if (searchTimer <= 0f)
        {
            // 현재 아이템 공개
            if (revealedUids == null)
                revealedUids = new System.Collections.Generic.HashSet<int>();
            revealedUids.Add(searchingItem.item.uid);
            revealedCount++;

            // UI 전체 갱신 후 다음 아이템으로
            AdvanceSearch();
            RefreshLeftGrid(lastSearchedGrid);
            UpdateSearchStatusText();
            return;
        }

        // ── 프로그레스 바·펄스 경량 갱신 (Destroy 없이) ──
        UpdateSearchAnimation();
    }

    /// <summary>수색 중 아이템의 펄스/프로그레스 바만 경량 갱신</summary>
    void UpdateSearchAnimation()
    {
        if (containerGridRoot == null || searchingItem == null) return;

        // searchingItem에 해당하는 GO 찾기
        string targetName = $"CItem_{searchingItem.item.uid}";
        Transform itemTr = containerGridRoot.Find(targetName);
        if (itemTr == null) return;

        // 배경 펄스 색상
        var bg = itemTr.GetComponent<Image>();
        if (bg != null)
        {
            float pulse = 0.5f + 0.15f * Mathf.Sin(Time.unscaledTime * 4f);
            bg.color = new Color(pulse * 0.4f, pulse * 0.35f, pulse * 0.2f, 0.95f);
        }

        // 프로그레스 바 채움 갱신
        Transform barBgTr = itemTr.Find("BarBg");
        if (barBgTr != null)
        {
            Transform barFillTr = barBgTr.Find("BarFill");
            if (barFillTr != null)
            {
                float progress = searchDelay > 0 ? 1f - (searchTimer / searchDelay) : 1f;
                var fillRT = barFillTr.GetComponent<RectTransform>();
                if (fillRT != null)
                    fillRT.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            }
        }
    }

    float GetSearchDelay(ItemRarity rarity)
    {
        var gt = GameTuning.Instance;
        float baseDelay;
        switch (rarity)
        {
            case ItemRarity.Common:    baseDelay = gt != null ? gt.searchSecCommon : 0.4f; break;
            case ItemRarity.Uncommon:  baseDelay = gt != null ? gt.searchSecUncommon : 0.6f; break;
            case ItemRarity.Rare:      baseDelay = gt != null ? gt.searchSecRare : 0.9f; break;
            case ItemRarity.Epic:      baseDelay = gt != null ? gt.searchSecEpic : 1.3f; break;
            case ItemRarity.Legendary: baseDelay = gt != null ? gt.searchSecLegendary : 1.8f; break;
            default: baseDelay = 0.5f; break;
        }
        float mult = gt != null ? gt.searchSpeedMult : 1f;
        return (baseDelay + Random.Range(-0.1f, 0.1f)) * mult;
    }

    void UpdateSearchStatusText()
    {
        if (searchStatusText == null) return;
        if (!leftPanelSearchEnabled || totalSearchItems == 0)
        {
            searchStatusText.text = "";
            return;
        }
        if (isSearching)
            searchStatusText.text = $"수색 중... {revealedCount}/{totalSearchItems}";
        else if (revealedCount >= totalSearchItems)
            searchStatusText.text = $"수색 완료 ({totalSearchItems}개)";
        else
            searchStatusText.text = "";
    }

    #endregion

    #region 드래그 앤 드롭

    void HandleDragAndDrop()
    {
        // 확인 팝업이 떠 있는 동안엔 패널 수동 입력 정지(뒤 격자 클릭/픽업 방지). 팝업 버튼은 EventSystem으로 동작.
        if (confirmGO != null && confirmGO.activeSelf)
            return;

        if (isDragging)
        {
            // 1제스처 드래그: 누른 상태로 끌고, 떼면 놓는다.
            UpdateGhostPosition();
            UpdateHighlight();

            if (GameInput.GetKeyDown(KeyCode.R))
                ToggleDragRotation();

            // 우클릭은 드래그 취소(원위치 복귀)
            if (GameInput.GetMouseButtonDown(1))
            {
                CancelDrag();
                return;
            }

            // 마우스 버튼을 떼는 순간 = 놓기
            if (GameInput.GetMouseButtonUp(0))
                TryPlaceDragged();
        }
        else
        {
            if (GameInput.GetMouseButtonDown(0))
            {
                if (contextMenuGO != null && contextMenuGO.activeSelf)
                {
                    // 메뉴 '밖'을 클릭했을 때만 닫는다. 메뉴 버튼 클릭은 닫지 말고
                    // 버튼 onClick(=마우스 업)이 발동하도록 둬야 한다(다운에서 닫으면 클릭이 씹힘).
                    if (!IsMouseOverRect(contextMenuRT))
                        HideContextMenu();
                }
                else if (GameInput.GetKey(KeyCode.LeftControl) || GameInput.GetKey(KeyCode.RightControl))
                    TryQuickTransfer();
                else
                    TryPickupItem(); // 아이템 칸이면 StartDrag → 이후 떼면 놓기
            }

            // 우클릭 컨텍스트 메뉴
            if (GameInput.GetMouseButtonDown(1))
                TryShowContextMenu();

            // Del 키: 마우스 위 내 아이템 버리기 (드래그 버리기 대체 수단)
            if (GameInput.GetKeyDown(KeyCode.Delete))
                TryDiscardItemUnderMouse();
        }
    }

    /// <summary>마우스 아래 플레이어 아이템을 버린다(= 우클릭 버리기와 동일: 레이드=바닥 산포 / 안전구역=인벤 복귀).</summary>
    void TryDiscardItemUnderMouse()
    {
        if (playerInventory == null) return;
        InventoryGrid grid; RectTransform root; int gx, gy;
        if (!PlayerGridAtMouse(out grid, out root, out gx, out gy)) return;
        var placed = grid.GetAt(gx, gy);
        if (placed == null || placed.item.data == null) return;
        var item = placed.item;
        grid.Remove(placed);
        DropOrReturnItem(item);
        RefreshAllGrids();
    }

    // ── 픽업 ──

    void TryPickupItem()
    {
        // 컨테이너 팝업 격자 (최상단)
        {
            InventoryGrid cg; int cgx, cgy;
            if (ContainerPopupAtMouse(out cg, out cgx, out cgy))
            {
                var placed = cg.GetAt(cgx, cgy);
                if (placed != null) StartDrag(placed, cg, popupGridRoot);
                return;
            }
            if (PointerOverContainerPopup()) return;   // 팝업 창(헤더·여백) 위 = 뒤 격자로 안 샘
        }

        // 플레이어 격자 (가방/주머니/보안)
        {
            InventoryGrid pGrid; RectTransform pRoot; int gx, gy;
            if (PlayerGridAtMouse(out pGrid, out pRoot, out gx, out gy))
            {
                var placed = pGrid.GetAt(gx, gy);
                if (placed != null)
                {
                    StartDrag(placed, pGrid, pRoot);
                    return;
                }
            }
        }

        // 좌측 격자 (루팅 상자 / 창고)
        var leftGrid = LeftGrid;
        if (leftGrid != null)
        {
            int gx, gy;
            if (ScreenToGridCell(containerGridRoot, leftGrid, out gx, out gy))
            {
                var placed = leftGrid.GetAt(gx, gy);
                if (placed != null)
                {
                    // 수색 모드: 공개된 아이템만 드래그 가능
                    if (leftPanelSearchEnabled && revealedUids != null
                        && !revealedUids.Contains(placed.item.uid))
                        return;

                    StartDrag(placed, leftGrid, containerGridRoot);
                    return;
                }
            }
        }

        // 격자의 빈 칸/패널 여백을 클릭 = 선택 해제.
        ClearSelection();
    }

    /// <summary>마우스가 해당 RectTransform 위에 있는지(슬롯 히트테스트).</summary>
    bool IsMouseOverRect(RectTransform rt)
    {
        if (rt == null || !rt.gameObject.activeInHierarchy) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, GameInput.mousePosition, null);
    }

    /// <summary>
    /// Ctrl+클릭 스마트 이동(통일 규칙).
    /// 장착 가능 아이템 → 자동 착용. 그 외 → 다른 컨테이너로 이동(가방↔창고/보안 컨테이너).
    /// </summary>
    void TryQuickTransfer()
    {
        var leftGrid = LeftGrid;
        bool popupOpen = openContainerItem != null && containerPopupGO != null && containerPopupGO.activeSelf;
        var containerGrid = popupOpen ? openContainerItem.ContainerGrid : null;

        // 0) 컨테이너 팝업 격자 클릭 → 밖으로 빼기(가방→주머니→보안→창고)
        {
            InventoryGrid cg; int cgx, cgy;
            if (ContainerPopupAtMouse(out cg, out cgx, out cgy))
            {
                var placed = cg.GetAt(cgx, cgy);
                if (placed != null && playerInventory != null)
                {
                    var item = placed.item;
                    cg.Remove(placed);
                    if (!StoreItemPreferStash(item))   // 창고 우선 → 인벤 (창고 없으면 인벤)
                        cg.TryPlace(item, placed.gridX, placed.gridY, placed.rotated);   // 복원
                    RefreshAllGrids();
                }
                return;
            }
            if (PointerOverContainerPopup()) return;
        }

        // 1) 플레이어 격자(가방/주머니/보안) 클릭
        {
            InventoryGrid pGrid; RectTransform pRoot; int gx, gy;
            if (PlayerGridAtMouse(out pGrid, out pRoot, out gx, out gy))
            {
                var placed = pGrid.GetAt(gx, gy);
                if (placed != null)
                {
                    var item = placed.item;
                    // 컨테이너 팝업이 열려 있으면 그 안으로 투입 우선
                    if (popupOpen && CanInsertIntoContainer(item, openContainerItem))
                    {
                        pGrid.Remove(placed);
                        if (!containerGrid.TryAutoPlace(item))
                            pGrid.TryPlace(item, placed.gridX, placed.gridY, placed.rotated);
                        RefreshAllGrids();
                        return;
                    }
                    // 장착 가능 → 자동 착용
                    if (IsEquippable(item.data)) { EquipFromGrid(item, pGrid); return; }
                    // 그 외 → 좌측(상자/창고)로 이동
                    if (leftGrid != null)
                    {
                        pGrid.Remove(placed);
                        if (!leftGrid.TryAutoPlace(item))
                            pGrid.TryPlace(item, placed.gridX, placed.gridY, placed.rotated);
                        RefreshAllGrids();
                    }
                }
                return;
            }
        }

        // 2) 좌측 격자(창고/상자) 클릭
        if (leftGrid != null)
        {
            int gx, gy;
            if (ScreenToGridCell(containerGridRoot, leftGrid, out gx, out gy))
            {
                var placed = leftGrid.GetAt(gx, gy);
                if (placed != null && playerInventory != null)
                {
                    // 수색 모드: 공개된 아이템만
                    if (leftPanelSearchEnabled && revealedUids != null
                        && !revealedUids.Contains(placed.item.uid))
                        return;

                    var item = placed.item;
                    // 컨테이너 팝업 열림 → 그 안으로 투입(가방 열고 창고아이템 Ctrl+클릭 = 가방으로)
                    if (popupOpen && CanInsertIntoContainer(item, openContainerItem))
                    {
                        leftGrid.Remove(placed);
                        if (!containerGrid.TryAutoPlace(item))
                            leftGrid.TryPlace(item, placed.gridX, placed.gridY, placed.rotated);
                        RefreshAllGrids();
                        return;
                    }
                    // 장착 가능 → 자동 착용
                    if (IsEquippable(item.data)) { EquipFromGrid(item, leftGrid); return; }
                    // 그 외 → 플레이어(가방→주머니→보안)로 이동
                    leftGrid.Remove(placed);
                    if (!playerInventory.TryAutoPlaceAnywhere(item))
                        leftGrid.TryPlace(item, placed.gridX, placed.gridY, placed.rotated);
                    RefreshAllGrids();
                }
                return;
            }
        }
    }

    void StartDrag(InventoryGrid.PlacedItem placed, InventoryGrid sourceGrid, RectTransform sourceRoot)
    {
        isDragging = true;
        dragItem = placed.item;
        dragSourceGrid = sourceGrid;
        dragOrigX = placed.gridX;
        dragOrigY = placed.gridY;
        dragOrigRotated = placed.rotated;
        dragRotated = placed.rotated;
        dragStartMouse = GameInput.mousePosition;
        // 픽셀 잡기 오프셋 = 커서(격자 로컬) − 아이템 좌상단(격자 로컬). 잡은 지점이 커서에 고정된다.
        grabPixelOffset = Vector2.zero;
        if (sourceRoot != null)
        {
            Vector2 cursorLocal;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    sourceRoot, GameInput.mousePosition, null, out cursorLocal))
            {
                int cellTotal = CELL_SIZE + CELL_GAP;
                Vector2 itemTopLeft = new Vector2(placed.gridX * cellTotal, -placed.gridY * cellTotal);
                grabPixelOffset = cursorLocal - itemTopLeft;
            }
        }

        sourceGrid.Remove(placed);
        // 드래그 시작 = 이전 선택 해제(선택 하이라이트와 드래그 하이라이트 충돌 방지).
        ClearSelection();
        CreateGhost();
        RefreshAllGrids();
        // 들었을 때 즉시 좌측 장비 슬롯 하이라이트(가방=Backpack 포함 모든 타입).
        // RefreshAllGrids(→ValidateSelection→ClearSelection) 이후에 세팅해야 살아남는다.
        SetDragHighlightSlots();
    }

    // ── 고스트 비주얼 ──

    void CreateGhost()
    {
        if (ghostGO != null) Destroy(ghostGO);
        if (dragItem == null || dragItem.data == null) return;

        ghostGO = new GameObject("DragGhost");
        ghostGO.transform.SetParent(canvasRT, false);
        ghostRT = ghostGO.AddComponent<RectTransform>();
        ghostRT.anchorMin = new Vector2(0.5f, 0.5f);
        ghostRT.anchorMax = new Vector2(0.5f, 0.5f);
        // pivot=좌상단 → 고스트 좌상단이 커서를 따라오고, 배치 하이라이트 셀(커서가 가리키는 좌상단 칸)과 정렬된다.
        ghostRT.pivot = new Vector2(0f, 1f);
        // (0,0) 깜빡임 방지: 위치를 세팅하기 전엔 숨겨둔다.
        ghostGO.SetActive(false);

        var img = ghostGO.AddComponent<Image>();
        var c = GetRarityBgColor(dragItem.data.rarity);
        img.color = new Color(c.r, c.g, c.b, 0.7f);
        img.raycastTarget = false;

        // 아이콘 또는 이름
        if (dragItem.data.icon != null)
        {
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(ghostGO.transform, false);
            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = new Vector2(4, 4);
            iconRT.offsetMax = new Vector2(-4, -4);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.sprite = dragItem.data.icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }
        else
        {
            var txt = MakeChildText(ghostGO.transform, dragItem.data.displayName, 11, Color.white);
            txt.raycastTarget = false;
        }

        UpdateGhostSize();

        // 생성 즉시 현재 커서 위치로 이동시킨 뒤 표시 → 첫 프레임부터 커서를 따라온다((0,0) 안 보임).
        UpdateGhostPosition();
        ghostGO.SetActive(true);
    }

    void UpdateGhostSize()
    {
        if (ghostRT == null || dragItem == null || dragItem.data == null) return;
        int w = dragRotated ? dragItem.data.gridHeight : dragItem.data.gridWidth;
        int h = dragRotated ? dragItem.data.gridWidth : dragItem.data.gridHeight;
        ghostRT.sizeDelta = new Vector2(
            w * CELL_SIZE + (w - 1) * CELL_GAP,
            h * CELL_SIZE + (h - 1) * CELL_GAP);
    }

    void UpdateGhostPosition()
    {
        if (ghostGO == null || canvasRT == null) return;
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, GameInput.mousePosition, null, out localPos);
        // 잡은 지점이 커서에 고정되도록 고스트 좌상단을 픽셀 오프셋만큼 당긴다(칸 점프 없이 자유 추적).
        ghostRT.anchoredPosition = localPos - grabPixelOffset;
    }

    // ── 회전 ──

    void ToggleDragRotation()
    {
        if (dragItem == null || dragItem.data == null) return;
        if (dragItem.data.gridWidth == dragItem.data.gridHeight) return; // 정사각형 무의미
        dragRotated = !dragRotated;
        // 회전 후 새 풋프린트 픽셀 범위로 잡기 오프셋 클램프(아이템 밖으로 안 벗어나게).
        int cellTotal = CELL_SIZE + CELL_GAP;
        int nw = dragRotated ? dragItem.data.gridHeight : dragItem.data.gridWidth;
        int nh = dragRotated ? dragItem.data.gridWidth : dragItem.data.gridHeight;
        grabPixelOffset.x = Mathf.Clamp(grabPixelOffset.x, 0f, nw * cellTotal);
        grabPixelOffset.y = Mathf.Clamp(grabPixelOffset.y, -nh * cellTotal, 0f);
        UpdateGhostSize();
        UpdateGhostPosition();
    }

    // ── 하이라이트 ──

    void UpdateHighlight()
    {
        InventoryGrid hoverGrid = null;
        RectTransform hoverGridRoot = null;
        int cellX = -1, cellY = -1;

        // 컨테이너 팝업 격자 위인지 (최상단)
        {
            InventoryGrid cg; int cgx, cgy;
            if (ContainerPopupAtMouse(out cg, out cgx, out cgy))
            {
                hoverGrid = cg; hoverGridRoot = popupGridRoot; cellX = cgx; cellY = cgy;
            }
        }

        // 플레이어 격자 위인지 (가방/주머니/보안)
        if (hoverGrid == null)
        {
            InventoryGrid pGrid; RectTransform pRoot; int gx, gy;
            if (PlayerGridAtMouse(out pGrid, out pRoot, out gx, out gy))
            {
                hoverGrid = pGrid;
                hoverGridRoot = pRoot;
                cellX = gx;
                cellY = gy;
            }
        }

        // 좌측 격자 위인지 (루팅 상자 / 창고)
        var leftG = LeftGrid;
        if (hoverGrid == null && leftG != null)
        {
            int gx, gy;
            if (ScreenToGridCell(containerGridRoot, leftG, out gx, out gy))
            {
                hoverGrid = leftG;
                hoverGridRoot = containerGridRoot;
                cellX = gx;
                cellY = gy;
            }
        }

        if (hoverGrid == null || cellX < 0)
        {
            if (highlightGO != null) highlightGO.SetActive(false);
            return;
        }

        // 커서가 가리키는 격자 안에서, 픽셀 오프셋 기준 아이템 원점 셀(가까운 칸 스냅)을 계산.
        GhostOriginCell(hoverGridRoot, out cellX, out cellY);

        // 하이라이트 생성/재배치
        EnsureHighlight(hoverGridRoot);
        highlightGO.SetActive(true);

        int w = dragRotated ? dragItem.data.gridHeight : dragItem.data.gridWidth;
        int h = dragRotated ? dragItem.data.gridWidth : dragItem.data.gridHeight;

        // 카테고리 필터 체크 (가구 창고/컨테이너 팝업에 놓을 때)
        bool categoryOk = true;
        if (hoverGridRoot == popupGridRoot && openContainerItem != null)
            categoryOk = CanInsertIntoContainer(dragItem, openContainerItem);
        else if (hoverGrid == LeftGrid)
            categoryOk = LeftGridAccepts(dragItem);

        bool canPlace = categoryOk && hoverGrid.CanPlace(dragItem, cellX, cellY, dragRotated);

        // 컨테이너 위 호버 = '안에 넣기' 표시(파랑). 빈칸 아닐 때만 검사.
        bool insertable = false;
        if (!canPlace)
        {
            var hover = hoverGrid.GetAt(cellX, cellY);
            insertable = hover != null && hover.item != dragItem && hover.item.IsContainer
                && CanInsertIntoContainer(dragItem, hover.item)
                && hover.item.ContainerGrid.CanAutoPlace(dragItem);
        }

        int cellTotal = CELL_SIZE + CELL_GAP;
        highlightRT.anchoredPosition = new Vector2(cellX * cellTotal, -cellY * cellTotal);
        highlightRT.sizeDelta = new Vector2(
            w * CELL_SIZE + (w - 1) * CELL_GAP,
            h * CELL_SIZE + (h - 1) * CELL_GAP);
        highlightImage.color = insertable ? new Color(0.3f, 0.55f, 1f, 0.45f)   // 파랑 = 컨테이너에 넣기
            : canPlace ? new Color(0.2f, 0.8f, 0.3f, 0.35f)
            : new Color(0.9f, 0.2f, 0.2f, 0.35f);
    }

    void EnsureHighlight(RectTransform gridRoot)
    {
        if (highlightGO == null)
        {
            highlightGO = new GameObject("DragHighlight");
            highlightRT = highlightGO.AddComponent<RectTransform>();
            highlightRT.anchorMin = new Vector2(0, 1);
            highlightRT.anchorMax = new Vector2(0, 1);
            highlightRT.pivot = new Vector2(0, 1);
            highlightImage = highlightGO.AddComponent<Image>();
            highlightImage.raycastTarget = false;
        }

        if (highlightGO.transform.parent != gridRoot)
            highlightGO.transform.SetParent(gridRoot, false);

        // 최상단에 표시
        highlightGO.transform.SetAsLastSibling();
    }

    // ── 배치 ──

    void TryPlaceDragged()
    {
        // 거의 안 움직였으면 = 클릭 → 이동이 아니라 "선택"으로 처리(원위치 복귀 + 선택 표시).
        if (((Vector2)GameInput.mousePosition - dragStartMouse).magnitude < CLICK_MOVE_THRESHOLD)
        {
            var clickedItem = dragItem;
            var clickedGrid = dragSourceGrid;
            // 원위치 복귀
            if (clickedGrid != null && clickedItem != null
                && !clickedGrid.TryPlace(clickedItem, dragOrigX, dragOrigY, dragOrigRotated))
                clickedGrid.TryAutoPlace(clickedItem);
            EndDrag();
            SelectItem(clickedItem, clickedGrid);
            return;
        }

        // 실제 드래그 이동 → 선택 해제(슬롯 하이라이트 정리는 EndDrag에서)
        ClearSelection();

        // (a) 좌측 장비 슬롯 위에 놓음 → 착용 (드래그-투-슬롯)
        EquipSlot dropSlot;
        if (EquipSlotAtMouse(out dropSlot))
        {
            if (dragItem != null && IsEquippable(dragItem.data))
            {
                var item = dragItem;
                var src = dragSourceGrid;
                // EquipFromGrid는 격자에서 아이템을 찾아 빼지만, 드래그 중엔 이미 제거된 상태다.
                // → 임시로 출발 격자에 되돌려 넣고 동일 경로로 장착(실패 시 그대로 남음).
                bool restored = false;
                if (src != null)
                    restored = src.TryPlace(item, dragOrigX, dragOrigY, dragOrigRotated) || src.TryAutoPlace(item);
                // 드래그 상태 종료(고스트/하이라이트 제거) 후 장착 처리.
                EndDrag();
                if (restored)
                    EquipFromGrid(item, src, dropSlot);
                else
                    ReturnItemToInventory(item);
                return;
            }
            // 장착 불가 아이템을 슬롯에 떨굼 → 원위치 복귀
            CancelDrag();
            return;
        }

        // (a-2) 무기 파츠 슬롯 위에 놓음 → 부착 (드래그-투-파츠슬롯)
        if (dragItem != null && dragItem.data != null && dragItem.data.IsWeaponPart
            && TryAttachDraggedToPartSlot())
            return;

        // (a-3) 컨테이너 팝업 격자에 배치 (최상단)
        {
            InventoryGrid cg; int cgx, cgy;
            if (ContainerPopupAtMouse(out cg, out cgx, out cgy))
            {
                if (openContainerItem != null && !CanInsertIntoContainer(dragItem, openContainerItem))
                {
                    bool bagInBag = IsWearableContainer(dragItem.data) && IsWearableContainer(openContainerItem.data);
                    ToastManager.Show(bagInBag ? "가방 안에 가방은 넣을 수 없다" : "이 보관함에 넣을 수 없는 종류다",
                                      ToastManager.ToastType.Warning);
                    CancelDrag();
                    return;
                }
                int ox, oy; GhostOriginCell(popupGridRoot, out ox, out oy);
                TryPlaceInGrid(cg, ox, oy);
                EnsureDragEnded();
                RefreshContainerPopup();
                return;
            }
            if (PointerOverContainerPopup()) { CancelDrag(); return; }   // 팝업 창 위(격자 밖)에 떨굼 = 원위치
        }

        // (b) 플레이어 격자(가방/주머니/보안)에 배치 시도
        {
            InventoryGrid pGrid; RectTransform pRoot; int gx, gy;
            if (PlayerGridAtMouse(out pGrid, out pRoot, out gx, out gy))
            {
                int ox, oy; GhostOriginCell(pRoot, out ox, out oy);
                TryPlaceInGrid(pGrid, ox, oy);
                EnsureDragEnded();   // 실패(스택 일부 등)해도 제스처 종료 — 떠다니지 않게
                return;
            }
        }

        // (c) 좌측 격자에 배치 시도 (루팅 상자 / 창고)
        var leftG2 = LeftGrid;
        if (leftG2 != null)
        {
            int gx, gy;
            if (ScreenToGridCell(containerGridRoot, leftG2, out gx, out gy))
            {
                // 보관함/가구 카테고리 게이트 — 불일치면 배치 거부(원위치 복귀).
                if (!LeftGridAccepts(dragItem))
                {
                    ToastManager.Show("이 보관함에 넣을 수 없는 종류다", ToastManager.ToastType.Warning);
                    CancelDrag();
                    return;
                }
                int ox, oy; GhostOriginCell(containerGridRoot, out ox, out oy);
                TryPlaceInGrid(leftG2, ox, oy);
                EnsureDragEnded();   // 컨테이너 간 이동도 한 제스처로 종료
                return;
            }
        }

        // (d) 격자 밖 클릭 → 월드 드롭(안전구역은 인벤/창고 복귀)
        DropDraggedToWorld();
    }

    /// <summary>아직 드래그가 끝나지 않았으면(부분 실패 등) 출발지로 되돌리고 제스처를 종료.</summary>
    void EnsureDragEnded()
    {
        if (!isDragging) return;
        // 배치/스왑이 완료되지 못해 dragItem이 아직 손에 있으면 출발지로 복귀시킨다.
        CancelDrag();
    }

    /// <summary>마우스 아래의 좌측 장비 슬롯을 찾는다.</summary>
    bool EquipSlotAtMouse(out EquipSlot slot)
    {
        slot = EquipSlot.None;
        if (equipSlotBgs == null) return false;
        foreach (var kv in equipSlotBgs)
        {
            if (kv.Value == null) continue;
            if (IsMouseOverRect(kv.Value.rectTransform))
            {
                slot = kv.Key;
                return true;
            }
        }
        return false;
    }

    bool TryPlaceInGrid(InventoryGrid grid, int x, int y)
    {
        // 빈 칸이면 직접 배치
        if (grid.CanPlace(dragItem, x, y, dragRotated))
        {
            grid.TryPlace(dragItem, x, y, dragRotated);
            EndDrag();
            return true;
        }

        // 이미 있는 칸 → 스택 또는 스왑
        var target = grid.GetAt(x, y);
        if (target == null) return false;

        // 스택 시도
        if (target.item.CanStackWith(dragItem))
        {
            int remaining = target.item.TryStack(dragItem);
            if (remaining <= 0)
            {
                grid.NotifyChanged();
                EndDrag();
                return true;
            }
            // 일부만 스택됨 — 계속 드래그
            grid.NotifyChanged();
            RefreshAllGrids();
            return false;
        }

        // 컨테이너(보관함/가방) 위에 떨굼 → 열지 않고 그 안으로 넣기(스왑 대신).
        // 카테고리 불일치 또는 내부 꽉참 → false 반환(스왑 X) → 호출부 EnsureDragEnded가 원위치 복구.
        if (target.item.IsContainer && target.item != dragItem)
        {
            if (!CanInsertIntoContainer(dragItem, target.item))
                return false;   // 카테고리 불일치/가방안가방 → 원상복구(스왑 X)
            if (target.item.ContainerGrid.TryAutoPlace(dragItem))
            {
                grid.NotifyChanged();
                EndDrag();
                return true;
            }
            return false;       // 내부 꽉참 → 원상복구
        }

        // 스왑 시도: 대상 아이템을 빼고, 새 아이템 배치, 이전 아이템은 드래그 시작 위치로.
        // (1제스처 드래그이므로 떼는 즉시 스왑 완료 — 들고 다니지 않는다.)
        var oldItem = target.item;
        bool oldRotated = target.rotated;
        int oldX = target.gridX;
        int oldY = target.gridY;
        var sourceGrid = dragSourceGrid; // dragItem이 빠져나온 격자 (그 칸은 현재 비어있음)
        grid.Remove(target);

        if (grid.CanPlace(dragItem, x, y, dragRotated))
        {
            grid.TryPlace(dragItem, x, y, dragRotated);

            // 빠진 아이템을 드래그 출발지로 되돌린다.
            bool placedOld = false;
            if (sourceGrid != null)
            {
                placedOld = sourceGrid.TryPlace(oldItem, dragOrigX, dragOrigY, dragOrigRotated)
                            || sourceGrid.TryAutoPlace(oldItem);
            }
            if (!placedOld)
                placedOld = grid.TryAutoPlace(oldItem);
            if (!placedOld)
                ReturnItemToInventory(oldItem);   // 스왑 오버플로도 바닥 X

            EndDrag();
            return true; // 스왑 완료
        }
        else
        {
            // 배치 불가 → 기존 아이템 복원 (드래그 아이템은 호출부에서 원위치)
            grid.TryPlace(oldItem, oldX, oldY, oldRotated);
            return false;
        }
    }

    // ── 드래그 종료 ──

    void EndDrag()
    {
        isDragging = false;
        dragItem = null;
        dragSourceGrid = null;

        if (ghostGO != null) { Destroy(ghostGO); ghostGO = null; ghostRT = null; }
        if (highlightGO != null) { Destroy(highlightGO); highlightGO = null; highlightRT = null; highlightImage = null; }

        // 드래그용 좌측 슬롯 하이라이트 정리(이후 SelectItem이 다시 켤 수 있음).
        highlightSlots.Clear();

        RefreshAllGrids();
    }

    void CancelDrag()
    {
        // 원래 위치로 복귀
        if (dragSourceGrid != null && dragItem != null)
        {
            if (!dragSourceGrid.TryPlace(dragItem, dragOrigX, dragOrigY, dragOrigRotated))
            {
                // 원래 자리 점유됨 (이론상 불가능하지만 안전장치)
                if (!dragSourceGrid.TryAutoPlace(dragItem))
                {
                    // 공간 부족 → 월드 드롭
                    DropDraggedToWorld();
                    return;
                }
            }
        }
        EndDrag();
    }

    void DropDraggedToWorld()
    {
        // 드래그로는 바닥에 못 버림(요청) — 격자 밖에 놓으면 인벤/창고로 되돌림.
        // 바닥 버리기는 우클릭 '버리기' 또는 Del 키로만.
        ReturnItemToInventory(dragItem);
        EndDrag();
    }

    /// <summary>아이템을 가방(없으면 창고)로 되돌린다 — 드래그-아웃/스왑오버플로용(바닥 X).</summary>
    void ReturnItemToInventory(ItemInstance item)
    {
        if (item == null) return;
        if (playerInventory != null && playerInventory.TryAutoPlaceAnywhere(item)) return;
        var stash = MainStash.Instance != null ? MainStash.Instance.GetGrid() : null;
        if (stash != null && stash.TryAutoPlace(item)) return;
        ToastManager.Show("공간이 없다", ToastManager.ToastType.Warning);
    }

    /// <summary>아이템을 바닥에 떨군다 — 단 안전구역에선 바닥 금지(가방→창고로 되돌림). 레이드에선 흩어지게 드롭.</summary>
    void DropOrReturnItem(ItemInstance item)
    {
        if (item == null) return;

        if (IsSafeArea())
        {
            // 안전구역: 바닥에 못 버림 → 가방/주머니/보안, 안 되면 창고로 되돌림
            if (playerInventory != null && playerInventory.TryAutoPlaceAnywhere(item)) return;
            var stash = MainStash.Instance != null ? MainStash.Instance.GetGrid() : null;
            if (stash != null && stash.TryAutoPlace(item)) return;
            ToastManager.Show("안전구역에선 바닥에 버릴 수 없다 (공간 부족)", ToastManager.ToastType.Warning);
            return;
        }

        // 레이드: 플레이어 주변에 흩어지게 드롭
        if (playerGO != null)
            WorldItem.Drop(item, ScatterPos());
    }

    /// <summary>플레이어 주변 반경 내 랜덤 위치(바닥 드롭 산포용).</summary>
    Vector3 ScatterPos()
    {
        Vector3 basePos = playerGO != null ? playerGO.transform.position : Vector3.zero;
        Vector2 r = Random.insideUnitCircle * 0.8f;
        return basePos + new Vector3(r.x, r.y, 0f);
    }

    // ── 우클릭 컨텍스트 메뉴 ──

    void TryShowContextMenu()
    {
        HideContextMenu();

        // 컨테이너 팝업 격자 (최상단)
        {
            InventoryGrid cg; int cgx, cgy;
            if (ContainerPopupAtMouse(out cg, out cgx, out cgy))
            {
                var placed = cg.GetAt(cgx, cgy);
                if (placed != null && placed.item.data != null)
                    ShowContextMenu(placed, cg);
                return;   // 팝업 위에선 다른 격자로 흘리지 않음
            }
            if (PointerOverContainerPopup()) return;
        }

        // 좌측 장비 슬롯 위 (착용 아이템) → 착용해제/자세히/제거
        {
            EquipSlot eqSlot;
            if (EquipSlotAtMouse(out eqSlot) && playerEquipment != null)
            {
                var equipped = playerEquipment.GetSlot(eqSlot);
                if (equipped != null)
                {
                    ShowEquipContextMenu(eqSlot, equipped);
                    return;
                }
            }
        }

        // 플레이어 격자 (가방/주머니/보안)
        {
            InventoryGrid pGrid; RectTransform pRoot; int gx, gy;
            if (PlayerGridAtMouse(out pGrid, out pRoot, out gx, out gy))
            {
                var placed = pGrid.GetAt(gx, gy);
                if (placed != null && placed.item.data != null)
                {
                    ShowContextMenu(placed, pGrid);
                    return;
                }
            }
        }

        // 좌측 격자 (공개된 아이템만)
        var leftGrid = LeftGrid;
        if (leftGrid != null)
        {
            int gx, gy;
            if (ScreenToGridCell(containerGridRoot, leftGrid, out gx, out gy))
            {
                var placed = leftGrid.GetAt(gx, gy);
                if (placed != null && placed.item.data != null)
                {
                    if (leftPanelSearchEnabled && revealedUids != null
                        && !revealedUids.Contains(placed.item.uid))
                        return;
                    ShowContextMenu(placed, leftGrid);
                    return;
                }
            }
        }
    }

    void ShowContextMenu(InventoryGrid.PlacedItem placed, InventoryGrid grid)
    {
        contextTarget = placed;
        contextTargetGrid = grid;
        var data = placed.item.data;

        EnsureContextMenu();
        contextMenuGO.SetActive(true);

        // 마우스 위치에 메뉴 배치
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, GameInput.mousePosition, null, out localPos);
        contextMenuRT.anchoredPosition = localPos;

        // 아이템 이름
        contextItemNameText.text = placed.item.DisplayName;
        contextItemNameText.color = data.RarityColor;

        // 기존 버튼 제거 (이름 텍스트 + 배경 제외)
        for (int i = contextMenuRT.childCount - 1; i >= 0; i--)
        {
            var child = contextMenuRT.GetChild(i);
            if (child.name != "CtxBg" && child.name != "CtxName")
                Destroy(child.gameObject);
        }

        float y = -28f;
        bool isPlayerGrid = IsPlayerGrid(grid);
        // 안전 창고(메인 창고/가구 창고) — 수색 중 루팅 상자는 제외
        bool isSafeStorage = !isPlayerGrid && grid == LeftGrid && !leftPanelSearchEnabled;

        // 열기 — 보관함(컨테이너 아이템) → 좌측에 내부 격자
        if (data.IsContainer)
        {
            var capInst = placed.item;
            AddContextButton("열기", UITheme.AccentBright, y, () =>
            {
                HideContextMenu();
                OpenContainerItem(capInst);
            });
            y -= 26f;
        }

        // 장착 — 모든 장착 가능 타입(가방·헬멧·방어구·리그·무기·근접·특수창)을 어느 격자에서든 노출.
        if (IsEquippable(data))
        {
            // 선택 상태도 같이 잡아 좌측 슬롯 하이라이트 일관성 유지
            SelectItem(placed.item, grid);
            var capItem = placed.item;
            var capGrid = grid;
            AddContextButton("착용", UITheme.AccentBright, y, () =>
            {
                EquipFromGrid(capItem, capGrid);
                HideContextMenu();
            });
            y -= 26f;
        }

        // 부착 (무기 파츠 + 내 소지품) → 장착 무기에 부착
        if (data.IsWeaponPart && isPlayerGrid)
        {
            var capPlaced = contextTarget;
            var capGrid2 = grid;
            AddContextButton("부착", UITheme.AccentBright, y, () =>
            {
                AttachPartFromGrid(capPlaced, capGrid2);
                HideContextMenu();
            });
            y -= 26f;
        }

        // 사용/먹기 (isUsable + 내 소지품 또는 안전 창고). 먹을거=먹기, 그 외=사용
        if (data.isUsable && (isPlayerGrid || isSafeStorage))
        {
            string useLabel = data.category == ItemCategory.Consumable ? "먹기" : "사용";
            var capGrid = grid;
            AddContextButton(useLabel, UITheme.Positive, y, () =>
            {
                playerInventory.UseItem(contextTarget, capGrid);   // 지정 격자에서 소모(창고 사용 지원)
                HideContextMenu();
                RefreshAllGrids();
            });
            y -= 26f;
        }

        // 퀵슬롯 등록/해제 (사용 가능 + 내 소지품) — 1~6 숫자키로 즉시 사용
        if (data.isUsable && isPlayerGrid)
        {
            var capId = data.itemId;
            AddContextButton("퀵슬롯", UITheme.AccentBright, y, () =>
            {
                if (QuickSlotBar.Instance != null) QuickSlotBar.Instance.Assign(capId);
                HideContextMenu();
            });
            y -= 26f;
        }

        // 자세히 (항상) — 아이템 1개 상세 팝업
        AddContextButton("자세히", UITheme.TextBright, y, () =>
        {
            ItemDetailUI.Show(contextTarget.item);
            HideContextMenu();
        });
        y -= 26f;

        // 버리기 (내 소지품) — 레이드=바닥 산포 / 안전구역=인벤·창고 복귀
        if (isPlayerGrid)
        {
            AddContextButton("버리기", UITheme.Negative, y, () =>
            {
                var capPlaced = contextTarget;
                var capGrid = grid;
                var item = capPlaced.item;
                HideContextMenu();
                ShowConfirm($"'{item.DisplayName}'을(를) 버릴까요?", () =>
                {
                    capGrid.Remove(capPlaced);
                    DropOrReturnItem(item);
                    RefreshAllGrids();
                });
            });
            y -= 26f;
        }
        // 폐기 (안전 창고) — 영구 삭제
        else if (isSafeStorage)
        {
            AddContextButton("폐기", UITheme.Negative, y, () =>
            {
                var capPlaced = contextTarget;
                var capGrid = grid;
                var nm = capPlaced.item.DisplayName;
                HideContextMenu();
                ShowConfirm($"'{nm}'을(를) 영구 폐기할까요?\n되돌릴 수 없습니다.", () =>
                {
                    capGrid.Remove(capPlaced);
                    RefreshAllGrids();
                    ToastManager.Show("아이템 폐기됨", ToastManager.ToastType.Info);
                });
            });
            y -= 26f;
        }

        // 메뉴 높이 조정
        float totalH = Mathf.Abs(y) + 8f;
        contextMenuRT.sizeDelta = new Vector2(150, totalH);

        // 화면 밖으로 나가지 않도록 보정
        Vector2 canvasSize = canvasRT.rect.size;
        Vector2 pos = contextMenuRT.anchoredPosition;
        if (pos.x + 150 > canvasSize.x / 2f) pos.x -= 150;
        if (pos.y - totalH < -canvasSize.y / 2f) pos.y += totalH;
        contextMenuRT.anchoredPosition = pos;
    }

    /// <summary>착용 슬롯 아이템 우클릭 메뉴 — 착용해제 / 자세히 / 제거(확인 팝업).</summary>
    void ShowEquipContextMenu(EquipSlot slot, ItemData data)
    {
        contextTarget = null;        // 격자 대상 아님(슬롯 기반)
        contextTargetGrid = null;

        EnsureContextMenu();
        contextMenuGO.SetActive(true);

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, GameInput.mousePosition, null, out localPos);
        contextMenuRT.anchoredPosition = localPos;

        var inst = playerEquipment.GetSlotInstance(slot) ?? new ItemInstance(data, 1);
        contextItemNameText.text = inst.DisplayName;
        contextItemNameText.color = data.RarityColor;

        // 기존 버튼 제거 (이름/배경 제외)
        for (int i = contextMenuRT.childCount - 1; i >= 0; i--)
        {
            var child = contextMenuRT.GetChild(i);
            if (child.name != "CtxBg" && child.name != "CtxName")
                Destroy(child.gameObject);
        }

        float y = -28f;
        var capSlot = slot;
        var capInst = inst;
        var capName = data.displayName;

        AddContextButton("착용해제", UITheme.AccentBright, y, () =>
        {
            UnequipToInventory(capSlot);
            HideContextMenu();
        });
        y -= 26f;

        AddContextButton("자세히", UITheme.TextBright, y, () =>
        {
            ItemDetailUI.Show(capInst);
            HideContextMenu();
        });
        y -= 26f;

        AddContextButton("제거", UITheme.Negative, y, () =>
        {
            HideContextMenu();
            ShowConfirm($"'{capName}'을(를) 제거할까요?", () => RemoveEquipped(capSlot));
        });
        y -= 26f;

        // 메뉴 높이 + 화면 밖 보정
        float totalH = Mathf.Abs(y) + 8f;
        contextMenuRT.sizeDelta = new Vector2(150, totalH);
        Vector2 canvasSize = canvasRT.rect.size;
        Vector2 pos = contextMenuRT.anchoredPosition;
        if (pos.x + 150 > canvasSize.x / 2f) pos.x -= 150;
        if (pos.y - totalH < -canvasSize.y / 2f) pos.y += totalH;
        contextMenuRT.anchoredPosition = pos;
    }

    void HideContextMenu()
    {
        if (contextMenuGO != null)
            contextMenuGO.SetActive(false);
        contextTarget = null;
        contextTargetGrid = null;
    }

    // ── 확인 팝업 ──

    /// <summary>파괴적 동작(버리기/폐기/제거) 전 [예/아니오] 모달. 예 → onYes 실행.</summary>
    void ShowConfirm(string message, System.Action onYes)
    {
        EnsureConfirm();
        confirmYes = onYes;
        confirmText.text = message;
        confirmGO.SetActive(true);
        confirmGO.transform.SetAsLastSibling();   // 항상 최상단(컨텍스트 메뉴/고스트 위)
    }

    void HideConfirm()
    {
        if (confirmGO != null) confirmGO.SetActive(false);
        confirmYes = null;
    }

    void EnsureConfirm()
    {
        if (confirmGO != null) return;

        // 전체 화면 반투명 백드롭(뒤 클릭 차단: raycastTarget=true)
        confirmGO = new GameObject("ConfirmPopup");
        confirmGO.transform.SetParent(canvasRT, false);
        var backRT = confirmGO.AddComponent<RectTransform>();
        backRT.anchorMin = Vector2.zero;
        backRT.anchorMax = Vector2.one;
        backRT.offsetMin = Vector2.zero;
        backRT.offsetMax = Vector2.zero;
        var backImg = confirmGO.AddComponent<Image>();
        backImg.color = new Color(0f, 0f, 0f, 0.6f);
        backImg.raycastTarget = true;

        // 중앙 패널
        var panelGO = new GameObject("ConfirmPanel");
        panelGO.transform.SetParent(confirmGO.transform, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(360, 150);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = UITheme.Panel;

        // 메시지
        var msgGO = new GameObject("Msg");
        msgGO.transform.SetParent(panelRT, false);
        var msgRT = msgGO.AddComponent<RectTransform>();
        msgRT.anchorMin = new Vector2(0, 1);
        msgRT.anchorMax = new Vector2(1, 1);
        msgRT.pivot = new Vector2(0.5f, 1);
        msgRT.anchoredPosition = new Vector2(0, -16);
        msgRT.sizeDelta = new Vector2(-24, 72);
        confirmText = msgGO.AddComponent<Text>();
        confirmText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        confirmText.fontSize = 14;
        confirmText.color = UITheme.TextBright;
        confirmText.alignment = TextAnchor.MiddleCenter;
        confirmText.horizontalOverflow = HorizontalWrapMode.Wrap;
        confirmText.verticalOverflow = VerticalWrapMode.Overflow;

        // 버튼 2개 (아니오 / 예)
        MakeConfirmButton(panelRT, "아니오", UITheme.TextBright, new Vector2(-85, 24), () => HideConfirm());
        MakeConfirmButton(panelRT, "예", UITheme.Negative, new Vector2(85, 24), () =>
        {
            var act = confirmYes;
            HideConfirm();
            act?.Invoke();
        });

        confirmGO.SetActive(false);
    }

    void MakeConfirmButton(RectTransform parent, string label, Color textColor, Vector2 anchoredPos, System.Action onClick)
    {
        var btnGO = new GameObject($"Confirm_{label}");
        btnGO.transform.SetParent(parent, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0f);
        btnRT.anchorMax = new Vector2(0.5f, 0f);
        btnRT.pivot = new Vector2(0.5f, 0f);
        btnRT.anchoredPosition = anchoredPos;
        btnRT.sizeDelta = new Vector2(130, 32);

        var img = btnGO.AddComponent<Image>();
        img.color = UITheme.Cell;
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = UITheme.CellHover;
        colors.pressedColor = UITheme.CellPressed;
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick());

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(btnGO.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;
        var txt = txtGO.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 13;
        txt.fontStyle = FontStyle.Bold;
        txt.color = textColor;
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
    }

    void EnsureContextMenu()
    {
        if (contextMenuGO != null) return;

        contextMenuGO = new GameObject("ContextMenu");
        contextMenuGO.transform.SetParent(canvasRT, false);
        contextMenuRT = contextMenuGO.AddComponent<RectTransform>();
        contextMenuRT.anchorMin = new Vector2(0.5f, 0.5f);
        contextMenuRT.anchorMax = new Vector2(0.5f, 0.5f);
        contextMenuRT.pivot = new Vector2(0, 1);
        contextMenuRT.sizeDelta = new Vector2(150, 100);

        // 배경
        var bgGO = new GameObject("CtxBg");
        bgGO.transform.SetParent(contextMenuRT, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = UITheme.Panel;

        // 아이템 이름
        var nameGO = new GameObject("CtxName");
        nameGO.transform.SetParent(contextMenuRT, false);
        var nameRT = nameGO.AddComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 1);
        nameRT.anchorMax = new Vector2(1, 1);
        nameRT.pivot = new Vector2(0, 1);
        nameRT.anchoredPosition = new Vector2(8, -4);
        nameRT.sizeDelta = new Vector2(-16, 22);
        contextItemNameText = nameGO.AddComponent<Text>();
        contextItemNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        contextItemNameText.fontSize = 13;
        contextItemNameText.fontStyle = FontStyle.Bold;
        contextItemNameText.alignment = TextAnchor.MiddleLeft;
        contextItemNameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        nameGO.AddComponent<Shadow>().effectColor = Color.black;

        // 최상단에 표시
        contextMenuGO.transform.SetAsLastSibling();
        contextMenuGO.SetActive(false);
    }

    void AddContextButton(string label, Color textColor, float yPos, System.Action onClick)
    {
        var btnGO = new GameObject($"Ctx_{label}");
        btnGO.transform.SetParent(contextMenuRT, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0, 1);
        btnRT.anchorMax = new Vector2(1, 1);
        btnRT.pivot = new Vector2(0, 1);
        btnRT.anchoredPosition = new Vector2(4, yPos);
        btnRT.sizeDelta = new Vector2(-8, 24);

        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = UITheme.Cell;

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        var colors = btn.colors;
        colors.highlightedColor = UITheme.CellHover;
        colors.pressedColor = UITheme.CellPressed;
        btn.colors = colors;

        btn.onClick.AddListener(() => onClick());

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(btnGO.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(8, 0);
        txtRT.offsetMax = new Vector2(-4, 0);

        var txt = txtGO.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 13;
        txt.color = textColor;
        txt.text = label;
        txt.alignment = TextAnchor.MiddleLeft;
    }

    // (구) ShowItemInspect/GetCategoryName 제거 — "자세히"는 ItemDetailUI 팝업이 대체.

    // ── 좌표 변환 ──

    bool ScreenToGridCell(RectTransform gridRoot, InventoryGrid grid, out int gx, out int gy)
    {
        gx = gy = -1;
        if (gridRoot == null || grid == null) return false;
        if (!gridRoot.gameObject.activeInHierarchy) return false;   // 숨긴 격자(가방 미장착 등)는 히트 제외

        Vector2 localPos;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridRoot, GameInput.mousePosition, null, out localPos))
            return false;

        int cellTotal = CELL_SIZE + CELL_GAP;
        gx = Mathf.FloorToInt(localPos.x / cellTotal);
        gy = Mathf.FloorToInt(-localPos.y / cellTotal);

        if (gx < 0 || gx >= grid.width || gy < 0 || gy >= grid.height)
            return false;

        return true;
    }

    /// <summary>마우스 아래의 플레이어 격자(가방→주머니→보안 순)와 셀을 찾는다.</summary>
    /// <summary>드래그 중 아이템 좌상단(= 커서 − 픽셀 잡기 오프셋)이 놓일 격자 칸을 가까운 칸으로 스냅해 반환.</summary>
    void GhostOriginCell(RectTransform gridRoot, out int gx, out int gy)
    {
        gx = gy = 0;
        if (gridRoot == null) return;
        Vector2 localPos;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gridRoot, GameInput.mousePosition, null, out localPos))
            return;
        Vector2 topLeft = localPos - grabPixelOffset;   // 아이템 좌상단 로컬 위치
        int cellTotal = CELL_SIZE + CELL_GAP;
        gx = Mathf.RoundToInt(topLeft.x / cellTotal);
        gy = Mathf.RoundToInt(-topLeft.y / cellTotal);
    }

    bool PlayerGridAtMouse(out InventoryGrid grid, out RectTransform root, out int gx, out int gy)
    {
        grid = null; root = null; gx = gy = -1;
        if (playerInventory == null) return false;

        if (ScreenToGridCell(invGridRoot, playerInventory.Grid, out gx, out gy))
        { grid = playerInventory.Grid; root = invGridRoot; return true; }
        if (ScreenToGridCell(pocketsGridRoot, playerInventory.PocketsGrid, out gx, out gy))
        { grid = playerInventory.PocketsGrid; root = pocketsGridRoot; return true; }
        if (ScreenToGridCell(secureGridRoot, playerInventory.SecureGrid, out gx, out gy))
        { grid = playerInventory.SecureGrid; root = secureGridRoot; return true; }
        return false;
    }

    /// <summary>해당 격자가 플레이어 소지 격자(가방/주머니/보안)인지.</summary>
    bool IsPlayerGrid(InventoryGrid g)
    {
        return playerInventory != null && g != null
            && (g == playerInventory.Grid || g == playerInventory.PocketsGrid || g == playerInventory.SecureGrid);
    }

    // ── 전체 격자 새로고침 ──

    void RefreshAllGrids()
    {
        RefreshInventoryGrid();
        var leftGrid = LeftGrid;
        if (leftGrid != null && leftPanelRoot != null && leftPanelRoot.activeSelf)
            RefreshLeftGrid(leftGrid);

        // 컨테이너 팝업도 갱신(팝업 안/밖 드래그 반영).
        if (openContainerItem != null && containerPopupGO != null && containerPopupGO.activeSelf)
            RefreshContainerPopup();

        // 선택 아이템이 더 이상 격자에 없으면 선택 해제(장착/이동/제거 후 꼬임 방지).
        ValidateSelection();
    }

    #endregion

    #region 아이템 선택 / 착용

    /// <summary>장착 가능한 아이템인지(가방·헬멧·방어구·리그·무기·근접·특수창 등).</summary>
    static bool IsEquippable(ItemData data)
    {
        if (data == null) return false;
        return data.equipSlot != EquipSlot.None || data.category == ItemCategory.Weapon;
    }

    /// <summary>아이템이 들어갈 좌측 장비 슬롯(들)을 반환. 일반 슬롯은 1개, 무기는 주/보조 둘 다.</summary>
    static void GetTargetSlots(ItemData data, HashSet<EquipSlot> outSlots)
    {
        outSlots.Clear();
        if (data == null) return;

        if (data.equipSlot != EquipSlot.None)
        {
            outSlots.Add(data.equipSlot);
            // 무기는 주무기/보조 어느 쪽에도 들어갈 수 있게 둘 다 표시
            if (data.category == ItemCategory.Weapon
                && (data.equipSlot == EquipSlot.PrimaryWeapon || data.equipSlot == EquipSlot.SecondaryWeapon))
            {
                outSlots.Add(EquipSlot.PrimaryWeapon);
                outSlots.Add(EquipSlot.SecondaryWeapon);
            }
        }
        else if (data.category == ItemCategory.Weapon)
        {
            // 구형 무기(equipSlot=None) → PrimaryWeapon 취급
            outSlots.Add(EquipSlot.PrimaryWeapon);
        }
    }

    /// <summary>아이템 클릭 선택. 장착 가능하면 좌측 장비 슬롯 배경 하이라이트.</summary>
    void SelectItem(ItemInstance item, InventoryGrid grid)
    {
        if (item == null || item.data == null) { ClearSelection(); return; }
        selectedItem = item;
        selectedGrid = grid;

        // 장착 가능한 아이템만 좌측 슬롯 하이라이트(헬멧→Head, 가방→Backpack, 무기→Primary/Secondary…).
        if (IsEquippable(item.data))
            GetTargetSlots(item.data, highlightSlots);
        else
            highlightSlots.Clear();
    }

    void ClearSelection()
    {
        selectedItem = null;
        selectedGrid = null;
        highlightSlots.Clear();
    }

    /// <summary>드래그 중인 아이템에 맞춰 좌측 장비 슬롯 하이라이트를 갱신(픽업 즉시 강조).</summary>
    void SetDragHighlightSlots()
    {
        if (isDragging && dragItem != null && dragItem.data != null && IsEquippable(dragItem.data))
            GetTargetSlots(dragItem.data, highlightSlots);
        else
            highlightSlots.Clear();
    }

    /// <summary>선택 아이템이 여전히 유효(해당 격자에 존재)한지 확인. 아니면 해제.</summary>
    void ValidateSelection()
    {
        if (selectedItem == null) return;
        bool stillThere = selectedGrid != null && GridContains(selectedGrid, selectedItem);
        if (!stillThere)
            ClearSelection();
    }

    static bool GridContains(InventoryGrid grid, ItemInstance item)
    {
        if (grid == null || item == null) return false;
        var all = grid.GetAll();
        for (int i = 0; i < all.Count; i++)
            if (all[i].item == item) return true;
        return false;
    }

    /// <summary>아이템을 격자에서 빼서 지정(또는 기본) 슬롯에 장착. 무기/장비 통일 처리. 실패 시 원복.</summary>
    void EquipFromGrid(ItemInstance item, InventoryGrid grid, EquipSlot forcedSlot = EquipSlot.None)
    {
        if (item == null || item.data == null || playerEquipment == null) return;
        if (!IsEquippable(item.data))
        {
            ToastManager.Show("장착할 수 없는 아이템", ToastManager.ToastType.Warning);
            return;
        }

        // 드래그-투-슬롯 가드: 지정 슬롯이 이 아이템의 유효 슬롯이 아니면 거절(엉뚱한 슬롯에 못 꽂게).
        if (forcedSlot != EquipSlot.None)
        {
            var valid = new HashSet<EquipSlot>();
            GetTargetSlots(item.data, valid);
            if (!valid.Contains(forcedSlot))
            {
                ToastManager.Show("이 슬롯에는 넣을 수 없다", ToastManager.ToastType.Warning);
                // 격자에서 빼지 않았으므로 별도 복원 불필요.
                ClearSelection();
                HideContextMenu();
                RefreshAllGrids();
                return;
            }
        }

        // PlayerEquipment가 실제로 사용하는 슬롯(무기 equipSlot=None → PrimaryWeapon).
        EquipSlot apiSlot = ApiEquipSlot(item.data);

        // 같은 ItemData가 그 슬롯에 이미 장착됨 = 토글 해제(아이템은 격자에 그대로 남겨둠).
        if (playerEquipment.GetSlot(apiSlot) == item.data)
        {
            playerEquipment.Unequip(apiSlot);
            ClearSelection();
            HideContextMenu();
            RefreshAllGrids();
            return;
        }

        // 격자에서 들어낼 위치 기억(실패 시 원복용).
        InventoryGrid.PlacedItem placed = FindPlaced(grid, item);
        int origX = placed != null ? placed.gridX : -1;
        int origY = placed != null ? placed.gridY : -1;
        bool origRot = placed != null ? placed.rotated : false;
        if (placed != null) grid.Remove(placed);

        // 교체로 빠질 기존 장비(스왑 복원용) — 인스턴스로 잡아 부착물 보존.
        ItemData prev = playerEquipment.GetSlot(apiSlot);
        ItemInstance prevInst = playerEquipment.GetSlotInstance(apiSlot);

        // 가방 교체: 기존 가방의 휴대 내용물을 그 가방 인스턴스에 먼저 담아 함께 회수(창고로 흩어지지 않게).
        if (apiSlot == EquipSlot.Backpack && prevInst != null && playerInventory != null)
            playerInventory.TransferBagToContainer(prevInst);

        bool ok = item.data.category == ItemCategory.Weapon
            ? playerEquipment.EquipWeapon(item.data)
            : playerEquipment.Equip(item.data);

        if (!ok)
        {
            // 장착 실패 → 격자 원위치 복원(또는 자동 배치)
            if (placed != null && !grid.TryPlace(item, origX, origY, origRot))
                grid.TryAutoPlace(item);
            ToastManager.Show("장착 실패", ToastManager.ToastType.Warning);
        }
        else
        {
            playerEquipment.SetSlotInstance(apiSlot, item);   // 실제 인스턴스(부착물) 연결
            // 새 가방의 보관 내용물 → 휴대 격자(가방 안 짐이 따라옴).
            if (apiSlot == EquipSlot.Backpack && playerInventory != null)
                playerInventory.TransferContainerToBag(item);
            if (prev != null && prev != item.data)
                // 교체로 빠진 기존 장비를 인벤(없으면 창고)로 회수 — 부착물 보존, 바닥 X.
                ReturnItemToInventory(prevInst ?? new ItemInstance(prev, 1));
        }

        ClearSelection();
        HideContextMenu();
        RefreshAllGrids();
    }

    /// <summary>PlayerEquipment가 실제로 장착할 슬롯(무기 equipSlot=None → PrimaryWeapon).</summary>
    static EquipSlot ApiEquipSlot(ItemData data)
    {
        if (data == null) return EquipSlot.None;
        if (data.equipSlot != EquipSlot.None) return data.equipSlot;
        if (data.category == ItemCategory.Weapon) return EquipSlot.PrimaryWeapon;
        return EquipSlot.None;
    }

    static InventoryGrid.PlacedItem FindPlaced(InventoryGrid grid, ItemInstance item)
    {
        if (grid == null || item == null) return null;
        var all = grid.GetAll();
        for (int i = 0; i < all.Count; i++)
            if (all[i].item == item) return all[i];
        return null;
    }

    #endregion

    #region 유틸

    Color GetRarityBgColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:    return new Color(0.25f, 0.25f, 0.3f, 0.9f);
            case ItemRarity.Uncommon:  return new Color(0.15f, 0.3f, 0.15f, 0.9f);
            case ItemRarity.Rare:      return new Color(0.15f, 0.2f, 0.4f, 0.9f);
            case ItemRarity.Epic:      return new Color(0.25f, 0.15f, 0.35f, 0.9f);
            case ItemRarity.Legendary: return new Color(0.35f, 0.3f, 0.1f, 0.9f);
            default: return new Color(0.2f, 0.2f, 0.25f, 0.9f);
        }
    }

    Text MakeText(Transform parent, string name, string content,
        Vector2 pos, Vector2 size, int fontSize, Color color, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.color = color;
        txt.text = content;
        txt.alignment = align;
        txt.supportRichText = true;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }

    Text MakeChildText(Transform parent, string content, int fontSize, Color color)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.color = color;
        txt.text = content;
        txt.alignment = TextAnchor.MiddleCenter;
        return txt;
    }

    #endregion
}

/// <summary>
/// 스크롤뷰 content에 부착. 아이템 클릭/드래그가 부모 ScrollRect 스크롤을 유발하지 않도록
/// 드래그 이벤트를 소비(아무것도 안 함)한다. 스크롤은 휠(IScrollHandler 전달)과 스크롤바로만.
/// CharacterPanelUI의 격자 클릭/드래그는 Input 폴링으로 별도 처리되므로 막아도 무방.
/// </summary>
public class ScrollDragBlocker : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    public ScrollRect targetScroll;

    // 드래그 이벤트를 여기서 받아 소비 → 부모 ScrollRect로 전파되지 않음(스크롤 점프 방지).
    public void OnBeginDrag(PointerEventData e) { }
    public void OnDrag(PointerEventData e) { }
    public void OnEndDrag(PointerEventData e) { }

    // 휠 스크롤은 부모 ScrollRect로 전달해 정상 동작 유지.
    public void OnScroll(PointerEventData e)
    {
        if (targetScroll != null)
            targetScroll.OnScroll(e);
    }
}
