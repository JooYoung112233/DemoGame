using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 통합 캐릭터 패널 (Tab 키).
/// 우측 고정 패널 + 상단 탭(인벤토리/의료/정보).
/// 루팅 상자 열면 좌측에 상자 격자 추가.
/// UIManager 자식으로 배치.
/// </summary>
public class CharacterPanelUI : MonoBehaviour
{
    public bool IsShowing => isShowing;
    public bool IsGenerated => canvas != null;

    bool isShowing;
    int currentTab; // 0=인벤토리, 1=의료, 2=정보

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

    /// <summary>현재 열린 좌측 격자 (상자 또는 창고)</summary>
    InventoryGrid LeftGrid =>
        openContainer != null ? openContainer.Grid
        : openStorage != null ? openStorage.Grid
        : null;

    // ── uGUI 요소 (구조적 요소만 SerializeField) ──
    [SerializeField] Canvas canvas;
    [SerializeField] RectTransform canvasRT; // 캔버스 RectTransform (고스트 부모)
    [SerializeField] GameObject panelRoot;

    // 우측 메인 패널
    [SerializeField] RectTransform rightPanel;
    [SerializeField] Image rightPanelBg;
    [SerializeField] Button[] tabButtons;
    [SerializeField] Text[] tabTexts;
    [SerializeField] Image[] tabBgs;
    [SerializeField] GameObject[] tabContents; // 각 탭의 콘텐츠 루트

    // 좌측 상자 패널
    [SerializeField] RectTransform leftPanel;
    [SerializeField] Image leftPanelBg;
    [SerializeField] Text leftTitleText;
    [SerializeField] GameObject leftPanelRoot; // 숨김/표시용

    // ── 인벤토리 탭 ──
    [SerializeField] RectTransform invGridRoot;
    Image[,] invSlotImages;
    Image[] invItemImages;      // 배치된 아이템 아이콘들
    [SerializeField] Text invWeightText;

    // ── 의료 탭 ──
    [SerializeField] Text[] medPartTexts;
    [SerializeField] Text medDebuffText;
    [SerializeField] Text medHealingText;

    // ── 정보 탭 ──
    [SerializeField] Text infoText;

    // ── 좌측 상자 격자 ──
    [SerializeField] RectTransform containerGridRoot;
    Image[,] containerSlotImages;

    // 설정
    static readonly int CELL_SIZE = 48;
    static readonly int CELL_GAP = 2;
    static readonly float PANEL_WIDTH = 300f;
    static readonly string[] TAB_NAMES = { "인벤토리", "의료", "정보" };
    static readonly string[] PART_NAMES = { "머 리", "몸 통", "양 팔", "왼다리", "오른다리" };

    // ── 드래그 앤 드롭 ──
    bool isDragging;
    ItemInstance dragItem;
    InventoryGrid dragSourceGrid;
    int dragOrigX, dragOrigY;
    bool dragOrigRotated;
    bool dragRotated;
    GameObject ghostGO;
    RectTransform ghostRT;
    GameObject highlightGO;
    RectTransform highlightRT;
    Image highlightImage;

    // ── 우클릭 컨텍스트 메뉴 ──
    GameObject contextMenuGO;
    RectTransform contextMenuRT;
    InventoryGrid.PlacedItem contextTarget;
    InventoryGrid contextTargetGrid;
    Text contextItemNameText;

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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (contextMenuGO != null && contextMenuGO.activeSelf)
            {
                HideContextMenu();
                return;
            }
            if (isDragging)
                CancelDrag();
            else
                Hide();
            return;
        }

        // 탭 내용 실시간 갱신
        switch (currentTab)
        {
            case 0: UpdateInventoryTab(); break;
            case 1: UpdateMedicalTab(); break;
            case 2: UpdateInfoTab(); break;
        }

        // 좌측 격자 갱신 (상자 또는 창고)
        if (LeftGrid != null)
            UpdateContainerGrid();

        // 드래그 앤 드롭 (인벤토리 탭일 때만)
        if (currentTab == 0)
            HandleDragAndDrop();
    }

    #region Show / Hide

    public void Show()
    {
        FindRefs();
        isShowing = true;
        if (panelRoot != null)
            panelRoot.SetActive(true);
        SelectTab(currentTab);
        RefreshInventoryGrid();
    }

    public void Hide()
    {
        HideContextMenu();
        if (isDragging) CancelDrag();
        isShowing = false;
        if (panelRoot != null)
            panelRoot.SetActive(false);
        CloseContainer();
    }

    public void ShowWithContainer(LootContainer container)
    {
        openContainer = container;
        openStorage = null;
        openFurniture = null;
        Show();
        SelectTab(0);
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
        SelectTab(0);

        // 제목에 허용 카테고리 표시
        string title = storage.StorageName;
        if (openFurniture != null && openFurniture.data != null && !openFurniture.data.IsUniversal)
            title += $" <size=11><color=#88AACC>({openFurniture.data.AllowedCategorySummary})</color></size>";

        ShowLeftPanel(storage.Grid, title, false); // 수색 연출 X (내 창고)
    }

    public void CloseContainer()
    {
        StopSearch(false);
        if (openContainer != null)
        {
            openContainer.Close();
            openContainer = null;
        }
        openStorage = null;
        openFurniture = null;
        if (leftPanelRoot != null)
            leftPanelRoot.SetActive(false);
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

        playerGO = null;
        health = null;
        medical = null;
        playerInventory = null;
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

        // 반투명 배경 (클릭 차단)
        var dimBg = panelRoot.AddComponent<Image>();
        dimBg.color = new Color(0, 0, 0, 0.4f);

        // ── 우측 메인 패널 ──
        BuildRightPanel(panelRoot.transform);

        // ── 좌측 상자 패널 ──
        BuildLeftPanel(panelRoot.transform);

        panelRoot.SetActive(false);
    }

    /// <summary>탭 버튼 onClick 리스너 연결</summary>
    public void BindEvents()
    {
        if (tabButtons == null) return;
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] == null) continue;
            int idx = i;
            tabButtons[i].onClick.RemoveAllListeners();
            tabButtons[i].onClick.AddListener(() => SelectTab(idx));
        }
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
        tabButtons = null;
        tabTexts = null;
        tabBgs = null;
        tabContents = null;
        leftPanel = null;
        leftPanelBg = null;
        leftTitleText = null;
        leftPanelRoot = null;
        invGridRoot = null;
        invWeightText = null;
        medPartTexts = null;
        medDebuffText = null;
        medHealingText = null;
        infoText = null;
        containerGridRoot = null;
        searchStatusText = null;
    }

    void BuildRightPanel(Transform parent)
    {
        var go = new GameObject("RightPanel");
        go.transform.SetParent(parent, false);
        rightPanel = go.AddComponent<RectTransform>();
        rightPanel.anchorMin = new Vector2(1, 0);
        rightPanel.anchorMax = new Vector2(1, 1);
        rightPanel.pivot = new Vector2(1, 0.5f);
        rightPanel.anchoredPosition = new Vector2(-15, 0);
        rightPanel.sizeDelta = new Vector2(PANEL_WIDTH, -30);

        rightPanelBg = go.AddComponent<Image>();
        rightPanelBg.color = new Color(0.06f, 0.06f, 0.1f, 0.95f);

        // ── 상단 탭 버튼 ──
        tabButtons = new Button[TAB_NAMES.Length];
        tabTexts = new Text[TAB_NAMES.Length];
        tabBgs = new Image[TAB_NAMES.Length];

        float tabW = PANEL_WIDTH / TAB_NAMES.Length;
        for (int i = 0; i < TAB_NAMES.Length; i++)
        {
            var tabGO = new GameObject($"Tab_{i}");
            tabGO.transform.SetParent(rightPanel, false);

            var tabRT = tabGO.AddComponent<RectTransform>();
            tabRT.anchorMin = new Vector2(0, 1);
            tabRT.anchorMax = new Vector2(0, 1);
            tabRT.pivot = new Vector2(0, 1);
            tabRT.anchoredPosition = new Vector2(tabW * i, 0);
            tabRT.sizeDelta = new Vector2(tabW, 32);

            tabBgs[i] = tabGO.AddComponent<Image>();
            tabBgs[i].color = new Color(0.12f, 0.12f, 0.18f);

            tabButtons[i] = tabGO.AddComponent<Button>();
            tabButtons[i].targetGraphic = tabBgs[i];

            tabTexts[i] = MakeChildText(tabGO.transform, TAB_NAMES[i], 14, Color.white);
        }

        // ── 탭 콘텐츠 영역 ──
        tabContents = new GameObject[TAB_NAMES.Length];
        for (int i = 0; i < TAB_NAMES.Length; i++)
        {
            var contentGO = new GameObject($"Content_{i}");
            contentGO.transform.SetParent(rightPanel, false);
            var cRT = contentGO.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 0);
            cRT.anchorMax = new Vector2(1, 1);
            cRT.offsetMin = new Vector2(8, 8);
            cRT.offsetMax = new Vector2(-8, -40);
            tabContents[i] = contentGO;
        }

        BuildInventoryTabContent(tabContents[0].transform);
        BuildMedicalTabContent(tabContents[1].transform);
        BuildInfoTabContent(tabContents[2].transform);
    }

    void BuildLeftPanel(Transform parent)
    {
        leftPanelRoot = new GameObject("LeftPanel");
        leftPanelRoot.transform.SetParent(parent, false);
        leftPanel = leftPanelRoot.AddComponent<RectTransform>();
        leftPanel.anchorMin = new Vector2(1, 0);
        leftPanel.anchorMax = new Vector2(1, 1);
        leftPanel.pivot = new Vector2(1, 0.5f);
        leftPanel.anchoredPosition = new Vector2(-15 - PANEL_WIDTH - 10, 0);
        leftPanel.sizeDelta = new Vector2(PANEL_WIDTH, -30);

        leftPanelBg = leftPanelRoot.AddComponent<Image>();
        leftPanelBg.color = new Color(0.06f, 0.06f, 0.1f, 0.95f);

        // 제목
        leftTitleText = MakeText(leftPanel, "LeftTitle", "상자",
            new Vector2(10, -8), new Vector2(PANEL_WIDTH - 20, 28), 16, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
        leftTitleText.fontStyle = FontStyle.Bold;

        // 수색 상태 텍스트
        searchStatusText = MakeText(leftPanel, "SearchStatus", "",
            new Vector2(10, -32), new Vector2(PANEL_WIDTH - 20, 18), 12, new Color(0.6f, 0.8f, 1f), TextAnchor.MiddleCenter);

        // 격자 루트
        var gridGO = new GameObject("ContainerGrid");
        gridGO.transform.SetParent(leftPanel, false);
        containerGridRoot = gridGO.AddComponent<RectTransform>();
        containerGridRoot.anchorMin = new Vector2(0, 1);
        containerGridRoot.anchorMax = new Vector2(0, 1);
        containerGridRoot.pivot = new Vector2(0, 1);
        containerGridRoot.anchoredPosition = new Vector2(10, -54);
        containerGridRoot.sizeDelta = new Vector2(PANEL_WIDTH - 20, 400);

        leftPanelRoot.SetActive(false);
    }

    #endregion

    #region 인벤토리 탭 빌드

    void BuildInventoryTabContent(Transform parent)
    {
        // 격자 루트
        var gridGO = new GameObject("InvGrid");
        gridGO.transform.SetParent(parent, false);
        invGridRoot = gridGO.AddComponent<RectTransform>();
        invGridRoot.anchorMin = new Vector2(0, 1);
        invGridRoot.anchorMax = new Vector2(0, 1);
        invGridRoot.pivot = new Vector2(0, 1);
        invGridRoot.anchoredPosition = new Vector2(0, 0);
        invGridRoot.sizeDelta = new Vector2(280, 450);

        // 무게 텍스트
        invWeightText = MakeText(parent, "Weight", "무게: 0 / 30 kg",
            new Vector2(0, -420), new Vector2(280, 24), 13, new Color(0.7f, 0.8f, 0.9f), TextAnchor.MiddleLeft);
    }

    void RefreshInventoryGrid()
    {
        if (invGridRoot == null) return;

        // 기존 자식 제거
        for (int i = invGridRoot.childCount - 1; i >= 0; i--)
            Destroy(invGridRoot.GetChild(i).gameObject);

        if (playerInventory == null || playerInventory.Grid == null) return;

        var grid = playerInventory.Grid;
        int cellTotal = CELL_SIZE + CELL_GAP;

        // 빈 칸 슬롯 생성
        invSlotImages = new Image[grid.width, grid.height];
        for (int gy = 0; gy < grid.height; gy++)
        {
            for (int gx = 0; gx < grid.width; gx++)
            {
                var slotGO = new GameObject($"Slot_{gx}_{gy}");
                slotGO.transform.SetParent(invGridRoot, false);
                var rt = slotGO.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(gx * cellTotal, -gy * cellTotal);
                rt.sizeDelta = new Vector2(CELL_SIZE, CELL_SIZE);

                var img = slotGO.AddComponent<Image>();
                img.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);
                invSlotImages[gx, gy] = img;
            }
        }

        // 배치된 아이템 표시
        RefreshInventoryItems(grid, invGridRoot);
    }

    void RefreshInventoryItems(InventoryGrid grid, RectTransform gridRoot)
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

            // 이름 텍스트 (아이콘 없으면 이름 표시)
            if (p.item.data.icon == null)
            {
                var nameText = MakeChildText(itemGO.transform, p.item.data.displayName, 11, Color.white);
                nameText.alignment = TextAnchor.MiddleCenter;
            }

            // 내구도 바 (hasDurability 아이템)
            if (p.item.HasDurability)
            {
                float itemW = w * CELL_SIZE + (w - 1) * CELL_GAP;

                // 배경 바
                var durBgGO = new GameObject("DurBg");
                durBgGO.transform.SetParent(itemGO.transform, false);
                var durBgRT = durBgGO.AddComponent<RectTransform>();
                durBgRT.anchorMin = new Vector2(0, 0);
                durBgRT.anchorMax = new Vector2(1, 0);
                durBgRT.pivot = new Vector2(0, 0);
                durBgRT.anchoredPosition = new Vector2(2, 2);
                durBgRT.sizeDelta = new Vector2(-4, 6);
                var durBgImg = durBgGO.AddComponent<Image>();
                durBgImg.color = new Color(0, 0, 0, 0.6f);

                // 채움 바
                var durFillGO = new GameObject("DurFill");
                durFillGO.transform.SetParent(durBgGO.transform, false);
                var durFillRT = durFillGO.AddComponent<RectTransform>();
                durFillRT.anchorMin = Vector2.zero;
                durFillRT.anchorMax = new Vector2(p.item.DurabilityRatio, 1f);
                durFillRT.offsetMin = Vector2.zero;
                durFillRT.offsetMax = Vector2.zero;
                var durFillImg = durFillGO.AddComponent<Image>();
                float ratio = p.item.DurabilityRatio;
                durFillImg.color = ratio > 0.5f ? new Color(0.3f, 0.9f, 0.4f)
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
                    if (gx < grid.width && gy < grid.height && invSlotImages != null)
                        invSlotImages[gx, gy].color = new Color(0.1f, 0.1f, 0.15f, 0.4f);
        }
    }

    void UpdateInventoryTab()
    {
        if (playerInventory == null) return;

        if (invWeightText != null)
        {
            float cur = playerInventory.CurrentWeight;
            float max = playerInventory.MaxWeight;
            Color wc = cur > max ? new Color(1f, 0.3f, 0.3f) : new Color(0.7f, 0.8f, 0.9f);
            invWeightText.color = wc;
            invWeightText.text = $"무게: {cur:F1} / {max:F0} kg  |  아이템: {playerInventory.Grid.ItemCount}개";
        }
    }

    #endregion

    #region 의료 탭 빌드

    void BuildMedicalTabContent(Transform parent)
    {
        medPartTexts = new Text[5];
        for (int i = 0; i < 5; i++)
        {
            medPartTexts[i] = MakeText(parent, $"Part_{i}", $"{PART_NAMES[i]}: 정상",
                new Vector2(5, -i * 30), new Vector2(270, 26), 15, new Color(0.4f, 1f, 0.5f), TextAnchor.MiddleLeft);
            medPartTexts[i].fontStyle = FontStyle.Bold;
        }

        medHealingText = MakeText(parent, "Healing", "",
            new Vector2(5, -165), new Vector2(270, 22), 14, new Color(0.4f, 1f, 0.8f), TextAnchor.MiddleLeft);

        medDebuffText = MakeText(parent, "Debuffs", "",
            new Vector2(5, -195), new Vector2(270, 80), 13, new Color(1f, 0.6f, 0.4f), TextAnchor.UpperLeft);
    }

    void UpdateMedicalTab()
    {
        if (medical == null || medPartTexts == null) return;

        var parts = medical.GetAllParts();
        for (int i = 0; i < parts.Length && i < medPartTexts.Length; i++)
        {
            if (parts[i].IsInjured)
            {
                string summary = "";
                if (parts[i].HasInjury(InjuryType.Bleeding)) summary += "출혈 ";
                if (parts[i].HasInjury(InjuryType.Fracture)) summary += "골절 ";
                if (parts[i].HasInjury(InjuryType.Pain)) summary += "통증 ";
                medPartTexts[i].text = $"{PART_NAMES[i]}: {summary.TrimEnd()}";

                if (parts[i].HasInjury(InjuryType.Fracture))
                    medPartTexts[i].color = new Color(1f, 0.2f, 0.2f);
                else if (parts[i].HasInjury(InjuryType.Bleeding))
                    medPartTexts[i].color = new Color(1f, 0.5f, 0.2f);
                else
                    medPartTexts[i].color = new Color(1f, 1f, 0.3f);
            }
            else
            {
                medPartTexts[i].text = $"{PART_NAMES[i]}: 정상";
                medPartTexts[i].color = new Color(0.4f, 1f, 0.5f);
            }
        }

        // 치료 진행
        if (medical.IsHealing)
            medHealingText.text = $"치료 중... {medical.HealProgress * 100:F0}%";
        else
            medHealingText.text = "";

        // 디버프
        string debuffs = "";
        if (medical.MoveSpeedMultiplier < 1f)
            debuffs += $"이동속도: {medical.MoveSpeedMultiplier * 100:F0}%\n";
        if (medical.AttackSpeedMultiplier < 1f)
            debuffs += $"공격속도: {medical.AttackSpeedMultiplier * 100:F0}%\n";
        if (medical.StaminaRegenMultiplier < 1f)
            debuffs += $"스태미너회복: {medical.StaminaRegenMultiplier * 100:F0}%";
        medDebuffText.text = debuffs;
    }

    #endregion

    #region 정보 탭 빌드

    void BuildInfoTabContent(Transform parent)
    {
        infoText = MakeText(parent, "Info", "",
            new Vector2(5, 0), new Vector2(270, 400), 14, new Color(0.8f, 0.85f, 0.9f), TextAnchor.UpperLeft);
    }

    void UpdateInfoTab()
    {
        if (infoText == null) return;

        string info = "";
        if (health != null)
            info += $"HP: {health.CurrentHp:F0} / {health.MaxHp:F0}\n";
        //     info += $"스태미너: {player.StaminaCurrent:F0} / {player.StaminaMax:F0}\n";
        if (flashlight != null)
            info += $"배터리: {flashlight.BatteryPercent * 100:F0}%  {(flashlight.IsOn ? "ON" : "OFF")}\n";

        info += "\n";

        if (playerInventory != null)
        {
            info += $"가방: {playerInventory.Grid.ItemCount}개\n";
            info += $"무게: {playerInventory.CurrentWeight:F1} / {playerInventory.MaxWeight:F0} kg\n";

            // 총 판매가치
            int totalValue = 0;
            var allItems = playerInventory.Grid.GetAll();
            for (int i = 0; i < allItems.Count; i++)
            {
                if (allItems[i].item.data != null)
                    totalValue += allItems[i].item.data.sellPrice * allItems[i].item.stackCount;
            }
            if (totalValue > 0)
                info += $"판매가치: {totalValue} 루디\n";
        }

        info += "\n";

        //     info += $"전투: {(player.CombatEnabled ? "활성" : "비활성")}\n";
        if (medical != null)
            info += $"부상: {(medical.HasAnyInjury ? "있음" : "없음")}\n";

        infoText.text = info;
    }

    #endregion

    #region 좌측 상자

    void ShowLeftPanel(InventoryGrid grid, string title, bool withSearch = false)
    {
        if (leftPanelRoot == null || grid == null) return;

        leftPanelRoot.SetActive(true);
        leftTitleText.text = title;
        leftPanelSearchEnabled = withSearch;

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
                img.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);
                containerSlotImages[gx, gy] = img;
            }
        }

        // 아이템 표시 (인벤토리와 동일 패턴)
        RefreshContainerItems(grid, containerGridRoot);
    }

    void RefreshContainerItems(InventoryGrid grid, RectTransform gridRoot)
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
            bool revealed = !leftPanelSearchEnabled
                || (revealedUids != null && revealedUids.Contains(p.item.uid));
            bool currentlySearching = leftPanelSearchEnabled
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
                bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
                var qTxt = MakeChildText(itemGO.transform, "?", 18, new Color(0.3f, 0.3f, 0.4f));
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
                var searchTxt = MakeChildText(itemGO.transform, "...", 14, new Color(1f, 0.85f, 0.4f));
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
                barFill.AddComponent<Image>().color = new Color(1f, 0.75f, 0.2f);
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
        float baseDelay;
        switch (rarity)
        {
            case ItemRarity.Common:    baseDelay = 0.4f; break;
            case ItemRarity.Uncommon:  baseDelay = 0.6f; break;
            case ItemRarity.Rare:      baseDelay = 0.9f; break;
            case ItemRarity.Epic:      baseDelay = 1.3f; break;
            case ItemRarity.Legendary: baseDelay = 1.8f; break;
            default: baseDelay = 0.5f; break;
        }
        float mult = GameTuning.Instance != null ? GameTuning.Instance.searchSpeedMult : 1f;
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
        if (isDragging)
        {
            UpdateGhostPosition();
            UpdateHighlight();

            if (Input.GetKeyDown(KeyCode.R))
                ToggleDragRotation();

            if (Input.GetMouseButtonDown(0))
                TryPlaceDragged();

            if (Input.GetMouseButtonDown(1))
                CancelDrag();
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (contextMenuGO != null && contextMenuGO.activeSelf)
                    HideContextMenu();
                else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    TryQuickTransfer();
                else
                    TryPickupItem();
            }

            // 우클릭 컨텍스트 메뉴
            if (Input.GetMouseButtonDown(1))
                TryShowContextMenu();
        }
    }

    // ── 픽업 ──

    void TryPickupItem()
    {
        // 플레이어 인벤토리 격자
        if (playerInventory != null && playerInventory.Grid != null)
        {
            int gx, gy;
            if (ScreenToGridCell(invGridRoot, playerInventory.Grid, out gx, out gy))
            {
                var placed = playerInventory.Grid.GetAt(gx, gy);
                if (placed != null)
                {
                    StartDrag(placed, playerInventory.Grid);
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

                    StartDrag(placed, leftGrid);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Ctrl+클릭 퀵 이동.
    /// 플레이어 격자 아이템 → 좌측(상자/창고)로, 좌측 아이템 → 플레이어 격자로.
    /// </summary>
    void TryQuickTransfer()
    {
        var playerGrid = playerInventory != null ? playerInventory.Grid : null;
        var leftGrid = LeftGrid;

        // 플레이어 격자 클릭 → 좌측으로 이동
        if (playerGrid != null)
        {
            int gx, gy;
            if (ScreenToGridCell(invGridRoot, playerGrid, out gx, out gy))
            {
                var placed = playerGrid.GetAt(gx, gy);
                if (placed != null && leftGrid != null)
                {
                    var item = placed.item;
                    playerGrid.Remove(placed);
                    if (!leftGrid.TryAutoPlace(item))
                    {
                        // 실패 → 원래 위치 복원
                        playerGrid.TryPlace(item, placed.gridX, placed.gridY, placed.rotated);
                    }
                    RefreshAllGrids();
                }
                return;
            }
        }

        // 좌측 격자 클릭 → 플레이어로 이동
        if (leftGrid != null)
        {
            int gx, gy;
            if (ScreenToGridCell(containerGridRoot, leftGrid, out gx, out gy))
            {
                var placed = leftGrid.GetAt(gx, gy);
                if (placed != null && playerGrid != null)
                {
                    // 수색 모드: 공개된 아이템만
                    if (leftPanelSearchEnabled && revealedUids != null
                        && !revealedUids.Contains(placed.item.uid))
                        return;

                    var item = placed.item;
                    leftGrid.Remove(placed);
                    if (!playerGrid.TryAutoPlace(item))
                    {
                        // 실패 → 원래 위치 복원
                        leftGrid.TryPlace(item, placed.gridX, placed.gridY, placed.rotated);
                    }
                    RefreshAllGrids();
                }
                return;
            }
        }
    }

    void StartDrag(InventoryGrid.PlacedItem placed, InventoryGrid sourceGrid)
    {
        isDragging = true;
        dragItem = placed.item;
        dragSourceGrid = sourceGrid;
        dragOrigX = placed.gridX;
        dragOrigY = placed.gridY;
        dragOrigRotated = placed.rotated;
        dragRotated = placed.rotated;

        sourceGrid.Remove(placed);
        CreateGhost();
        RefreshAllGrids();
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
        ghostRT.pivot = new Vector2(0.5f, 0.5f);

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
            canvasRT, Input.mousePosition, null, out localPos);
        ghostRT.anchoredPosition = localPos;
    }

    // ── 회전 ──

    void ToggleDragRotation()
    {
        if (dragItem == null || dragItem.data == null) return;
        if (dragItem.data.gridWidth == dragItem.data.gridHeight) return; // 정사각형 무의미
        dragRotated = !dragRotated;
        UpdateGhostSize();
    }

    // ── 하이라이트 ──

    void UpdateHighlight()
    {
        InventoryGrid hoverGrid = null;
        RectTransform hoverGridRoot = null;
        int cellX = -1, cellY = -1;

        // 플레이어 인벤토리 위인지
        if (playerInventory != null && playerInventory.Grid != null)
        {
            int gx, gy;
            if (ScreenToGridCell(invGridRoot, playerInventory.Grid, out gx, out gy))
            {
                hoverGrid = playerInventory.Grid;
                hoverGridRoot = invGridRoot;
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

        // 하이라이트 생성/재배치
        EnsureHighlight(hoverGridRoot);
        highlightGO.SetActive(true);

        int w = dragRotated ? dragItem.data.gridHeight : dragItem.data.gridWidth;
        int h = dragRotated ? dragItem.data.gridWidth : dragItem.data.gridHeight;

        // 카테고리 필터 체크 (가구 창고에 놓을 때)
        bool categoryOk = true;
        if (openFurniture != null && hoverGrid == LeftGrid)
            categoryOk = openFurniture.AcceptsItem(dragItem);

        bool canPlace = categoryOk && hoverGrid.CanPlace(dragItem, cellX, cellY, dragRotated);

        int cellTotal = CELL_SIZE + CELL_GAP;
        highlightRT.anchoredPosition = new Vector2(cellX * cellTotal, -cellY * cellTotal);
        highlightRT.sizeDelta = new Vector2(
            w * CELL_SIZE + (w - 1) * CELL_GAP,
            h * CELL_SIZE + (h - 1) * CELL_GAP);
        highlightImage.color = canPlace
            ? new Color(0.2f, 0.8f, 0.3f, 0.35f)
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
        // 플레이어 인벤토리에 배치 시도
        if (playerInventory != null && playerInventory.Grid != null)
        {
            int gx, gy;
            if (ScreenToGridCell(invGridRoot, playerInventory.Grid, out gx, out gy))
            {
                if (TryPlaceInGrid(playerInventory.Grid, gx, gy))
                    return;
                return; // 격자 위 클릭은 항상 소비 (실패해도)
            }
        }

        // 좌측 격자에 배치 시도 (루팅 상자 / 창고)
        var leftG2 = LeftGrid;
        if (leftG2 != null)
        {
            int gx, gy;
            if (ScreenToGridCell(containerGridRoot, leftG2, out gx, out gy))
            {
                if (TryPlaceInGrid(leftG2, gx, gy))
                    return;
                return;
            }
        }

        // 격자 밖 클릭 → 월드 드롭
        DropDraggedToWorld();
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

        // 스왑 시도: 대상 아이템을 빼고, 새 아이템 배치, 이전 아이템을 드래그로
        var oldItem = target.item;
        bool oldRotated = target.rotated;
        int oldX = target.gridX;
        int oldY = target.gridY;
        grid.Remove(target);

        if (grid.CanPlace(dragItem, x, y, dragRotated))
        {
            grid.TryPlace(dragItem, x, y, dragRotated);
            // 이전 아이템을 새 드래그 대상으로
            dragItem = oldItem;
            dragRotated = oldRotated;
            dragSourceGrid = grid;
            dragOrigX = oldX;
            dragOrigY = oldY;
            dragOrigRotated = oldRotated;
            // 고스트 갱신
            if (ghostGO != null) Destroy(ghostGO);
            CreateGhost();
            RefreshAllGrids();
            return false; // 계속 드래그
        }
        else
        {
            // 배치 불가 → 기존 아이템 복원
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
        if (dragItem != null && playerGO != null)
        {
            WorldItem.Drop(dragItem, playerGO.transform.position + playerGO.transform.right * 0.8f);
            Debug.Log($"[Inventory] {dragItem.DisplayName} 월드 드롭");
        }
        EndDrag();
    }

    // ── 우클릭 컨텍스트 메뉴 ──

    void TryShowContextMenu()
    {
        HideContextMenu();

        // 플레이어 인벤토리
        if (playerInventory != null && playerInventory.Grid != null)
        {
            int gx, gy;
            if (ScreenToGridCell(invGridRoot, playerInventory.Grid, out gx, out gy))
            {
                var placed = playerInventory.Grid.GetAt(gx, gy);
                if (placed != null && placed.item.data != null)
                {
                    ShowContextMenu(placed, playerInventory.Grid);
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
            canvasRT, Input.mousePosition, null, out localPos);
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
        bool isPlayerGrid = (grid == playerInventory?.Grid);

        // 사용 (isUsable + 플레이어 인벤토리만)
        if (data.isUsable && isPlayerGrid)
        {
            AddContextButton("사용", new Color(0.3f, 0.9f, 0.4f), y, () =>
            {
                playerInventory.UseItem(contextTarget);
                HideContextMenu();
                RefreshAllGrids();
            });
            y -= 26f;
        }

        // 검사 (항상)
        AddContextButton("검사", new Color(0.7f, 0.85f, 1f), y, () =>
        {
            ShowItemInspect(contextTarget.item);
            HideContextMenu();
        });
        y -= 26f;

        // 버리기 (플레이어 인벤토리만)
        if (isPlayerGrid)
        {
            AddContextButton("버리기", new Color(1f, 0.5f, 0.3f), y, () =>
            {
                var item = contextTarget.item;
                grid.Remove(contextTarget);
                if (playerGO != null)
                    WorldItem.Drop(item, playerGO.transform.position + playerGO.transform.right * 0.8f);
                HideContextMenu();
                RefreshAllGrids();
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

    void HideContextMenu()
    {
        if (contextMenuGO != null)
            contextMenuGO.SetActive(false);
        contextTarget = null;
        contextTargetGrid = null;
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
        bgImg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

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
        btnImg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        var colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.35f, 0.5f);
        colors.pressedColor = new Color(0.15f, 0.25f, 0.4f);
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

    /// <summary>아이템 검사 (상세 정보 표시)</summary>
    void ShowItemInspect(ItemInstance item)
    {
        if (item == null || item.data == null) return;
        var d = item.data;

        // 정보 탭으로 전환하여 상세 표시 대신, 간단한 팝업 로그
        // TODO: 전용 검사 패널 UI (향후)
        string info = $"<color=#{ColorUtility.ToHtmlStringRGB(d.RarityColor)}><b>{d.displayName}</b></color>\n";
        info += $"<size=11>{d.description}</size>\n\n";
        info += $"카테고리: {GetCategoryName(d.category)}\n";
        info += $"크기: {d.gridWidth}x{d.gridHeight}  무게: {d.weight:F1}kg\n";

        if (d.sellPrice > 0)
            info += $"판매가: {d.sellPrice} 루디\n";
        if (d.buyPrice > 0)
            info += $"구매가: {d.buyPrice} 루디\n";

        if (item.HasDurability)
            info += $"내구도: {item.durability:F0}/{d.maxDurability:F0}\n";
        if (item.stackCount > 1)
            info += $"수량: {item.stackCount}/{d.maxStack}\n";

        if (d.isUsable)
        {
            string effectName = d.useEffect.ToString();
            info += $"\n<color=#88CC88>사용 가능: {effectName} ({d.effectValue})</color>\n";
        }

        Debug.Log($"[검사] {d.displayName}\n{info}");

        // infoText가 있으면 정보 탭에 표시
        if (infoText != null)
        {
            SelectTab(2); // 정보 탭
            infoText.text = info;
        }
    }

    static string GetCategoryName(ItemCategory cat)
    {
        switch (cat)
        {
            case ItemCategory.Weapon: return "무기";
            case ItemCategory.Medical: return "의료";
            case ItemCategory.Consumable: return "소비";
            case ItemCategory.Material: return "재료";
            case ItemCategory.Valuable: return "귀중품";
            case ItemCategory.Key: return "열쇠";
            case ItemCategory.Misc: return "기타";
            default: return cat.ToString();
        }
    }

    // ── 좌표 변환 ──

    bool ScreenToGridCell(RectTransform gridRoot, InventoryGrid grid, out int gx, out int gy)
    {
        gx = gy = -1;
        if (gridRoot == null || grid == null) return false;

        Vector2 localPos;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridRoot, Input.mousePosition, null, out localPos))
            return false;

        int cellTotal = CELL_SIZE + CELL_GAP;
        gx = Mathf.FloorToInt(localPos.x / cellTotal);
        gy = Mathf.FloorToInt(-localPos.y / cellTotal);

        if (gx < 0 || gx >= grid.width || gy < 0 || gy >= grid.height)
            return false;

        return true;
    }

    // ── 전체 격자 새로고침 ──

    void RefreshAllGrids()
    {
        RefreshInventoryGrid();
        var leftGrid = LeftGrid;
        if (leftGrid != null && leftPanelRoot != null && leftPanelRoot.activeSelf)
            RefreshLeftGrid(leftGrid);
    }

    #endregion

    #region 탭 전환

    void SelectTab(int idx)
    {
        currentTab = idx;

        for (int i = 0; i < tabContents.Length; i++)
        {
            tabContents[i].SetActive(i == idx);
            tabBgs[i].color = (i == idx)
                ? new Color(0.2f, 0.35f, 0.55f)
                : new Color(0.12f, 0.12f, 0.18f);
            tabTexts[i].color = (i == idx)
                ? Color.white
                : new Color(0.5f, 0.5f, 0.6f);
        }

        // 인벤토리 탭 선택 시 격자 새로고침
        if (idx == 0)
            RefreshInventoryGrid();
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
