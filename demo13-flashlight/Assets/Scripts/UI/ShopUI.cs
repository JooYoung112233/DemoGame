using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 UI — Tarkov 스타일 3-컬럼 전체화면 모달.
/// 좌: 상인 재고(구매 선택) / 중: 선택 아이템 프리뷰 + 거래 버튼 / 우: 내 가방(판매 선택).
/// 탭 없음. 좌측 클릭 = 구매 대상, 우측 클릭 = 판매 대상.
/// </summary>
public class ShopUI : MonoBehaviour
{
    // ── 공개 API ────────────────────────────────────────────
    public static ShopUI Instance { get; private set; }
    public bool IsShowing   => panel != null && panel.activeSelf;
    public bool IsGenerated => canvas != null;

    // ── 상수 ────────────────────────────────────────────────
    const int   CELL_SIZE   = 72;   // 인벤/GridPanel과 동일 — 가로 7칸 통일
    const int   CELL_GAP    = 2;
    const float COL_L_RATIO = 0.35f;   // 좌 컬럼 너비 비율
    const float COL_R_RATIO = 0.35f;   // 우 컬럼 너비 비율

    // Tarkov 거래창 톤 — 파란끼 제거, 어두운 웜그레이/올리브 팔레트 + 탄(tan) 강조.
    static readonly Color C_BG          = new Color(0.085f, 0.082f, 0.072f, 0.99f);  // 거의 검정(웜)
    static readonly Color C_PANEL       = new Color(0.135f, 0.130f, 0.115f, 1.00f);  // 패널 배경
    static readonly Color C_CELL        = new Color(0.175f, 0.168f, 0.148f, 1.00f);  // 셀 배경
    static readonly Color C_CELL_SEL    = new Color(0.40f,  0.34f,  0.18f,  1.00f);  // 선택(탄)
    static readonly Color C_CELL_HOVER  = new Color(0.24f,  0.23f,  0.20f,  1.00f);
    static readonly Color C_HEADER      = new Color(0.155f, 0.150f, 0.130f, 1.00f);
    static readonly Color C_GOLD        = new Color(0.86f,  0.74f,  0.42f,  1.00f);  // 가격(탄/골드)
    static readonly Color C_BUY_BTN     = new Color(0.20f,  0.34f,  0.16f,  1.00f);  // 올리브 그린
    static readonly Color C_SELL_BTN    = new Color(0.46f,  0.28f,  0.10f,  1.00f);
    static readonly Color C_CLOSE       = new Color(0.40f,  0.14f,  0.12f,  1.00f);
    static readonly Color C_MUTED       = new Color(0.54f,  0.51f,  0.44f,  1.00f);  // 웜 뮤트
    static readonly Color C_DIM         = new Color(0.115f, 0.110f, 0.098f, 1.00f);  // 슬롯/그리드 배경
    static readonly Color C_SLOT        = new Color(0.225f, 0.215f, 0.195f, 1.00f);  // 빈 슬롯(인벤처럼 또렷이)
    static readonly Color C_GRIDLINE    = new Color(0.05f,  0.048f, 0.042f, 1.00f);  // 셀 사이 그리드선
    static readonly Color C_INK         = new Color(0.15f,  0.12f,  0.09f,  1.00f);  // btn(밝은 종이) 위 어두운 글자

    // ── 영속 스켈레톤(프리팹 베이크 시 직렬화 보존) / 런타임 상태 ─────
    // [SerializeField] = GenerateUI/Build* 로 만들어지는 영속 골격(프리팹에 굳음).
    // 직렬화 안 함: 동적 셀/행/트레이 엔트리, 선택·런타임 상태, 런타임 추가 컴포넌트.
    [SerializeField] Canvas          canvas;
    [SerializeField] GameObject      panel;

    // 상단 바
    [SerializeField] Text            traderNameText;
    [SerializeField] Text            trustText;
    [SerializeField] Text            balanceText;
    [SerializeField] Button          closeBtn;             // 닫기(✕) — 정적 골격 버튼

    // 좌: 상인 재고 (footprint 격자 — 인벤/창고와 동일 렌더)
    [SerializeField] RectTransform   stockContent;
    [SerializeField] RectTransform   stockSlotRoot;   // 슬롯 그리드(루트 — 동적 셀은 런타임 생성)
    [SerializeField] RectTransform   stockItemRoot;   // 배치 아이템 오버레이(루트 — 동적 셀은 런타임 생성)
    [SerializeField] Text            stockEmptyText;

    // 중: 구매 박스(상) / 판매 박스(하)
    [SerializeField] Image           buyIcon;
    [SerializeField] Text            buyName;
    [SerializeField] Text            buyInfo;
    [SerializeField] Button          actionBuyBtn;
    [SerializeField] Text            actionBuyLabel;
    [SerializeField] GameObject      buyEmpty;
    int             buyQty = 1;        // 다중 구매 수량(런타임 상태 — 직렬화 안 함)
    [SerializeField] Text            buyQtyText;
    [SerializeField] GameObject      buyQtyRow;
    [SerializeField] Button          buyMinusBtn;          // 수량 − (정적 골격 버튼)
    [SerializeField] Button          buyPlusBtn;           // 수량 + (정적 골격 버튼)

    [SerializeField] Image           sellIcon;
    [SerializeField] Text            sellName;
    [SerializeField] Text            sellInfo;
    [SerializeField] Button          actionSellBtn;
    [SerializeField] Text            actionSellLabel;
    [SerializeField] GameObject      sellEmpty;

    [SerializeField] Text            tradeResultText;

    // 우: 내 가방
    [SerializeField] RectTransform   invSlotRoot;    // 슬롯 그리드 루트 (동적 셀은 런타임 생성)
    [SerializeField] RectTransform   invItemRoot;    // 배치된 아이템 오버레이 루트 (동적 셀은 런타임 생성)

    // 선택 상태
    ItemData        selectedStock;      // 상인 재고에서 선택한 아이템
    InventoryGrid.PlacedItem selectedInv;  // 인벤토리에서 선택한 아이템

    ShopData        shop;
    InventoryGrid   stashGrid;

    // ── 판매 트레이 (다중 판매) ──────────────────────────────
    InventoryGrid   sellTray;        // 트레이 데이터(런타임) — 직렬화 안 함
    [SerializeField] RectTransform   sellTraySlotRoot, sellTrayItemRoot;
    [SerializeField] Button          sellTrayBtn;
    [SerializeField] Text            sellTrayBtnLabel;
    [SerializeField] Button          returnTrayBtn;        // 트레이 되돌리기 (정적 골격 버튼)

    // 드래그 격자(인벤 동일) — 창고/트레이/가방팝업
    GridDragManager dragMgr;
    GridPanel       stashPanel, trayPanel, bagPanel;

    // 가방 이동 팝업 (영속 골격 — BuildBagPopup이 GenerateUI 체인에서 생성)
    [SerializeField] GameObject      bagPopup;
    [SerializeField] Text            bagPopupTitle;
    ItemInstance    bagPopupInst;   // 런타임 상태 — 직렬화 안 함
    [SerializeField] GameObject      bagSortBtn;
    [SerializeField] Button          bagCloseBtn;          // 가방 팝업 닫기(✕) (정적 골격 버튼)

    // 우클릭 컨텍스트 메뉴 (열기/자세히/버리기)
    GameObject      shopMenu;
    RectTransform   shopMenuPanel;
    Text            shopMenuName;

    // ── 상점 내 컨테이너 열기 (가방 안 아이템 판매) ───────────
    ItemInstance    shopOpenBag;        // null = 루트 창고, 아니면 그 가방 내부 (런타임 상태)
    [SerializeField] Text            stashHeaderText;    // 창고/가방 제목
    [SerializeField] GameObject      stashBackBtn;       // '← 뒤로'

    /// <summary>현재 창고 패널이 보여주는 격자 (루트 창고 또는 열린 가방 내부).</summary>
    InventoryGrid CurrentStashGrid => shopOpenBag != null ? shopOpenBag.ContainerGrid : stashGrid;

    // ── 재고 수량(품절) — 세션 런타임. 재입고 시스템은 추후. shopId → (item → 남은 수량) ──
    static readonly Dictionary<string, Dictionary<ItemData, int>> stockBank
        = new Dictionary<string, Dictionary<ItemData, int>>();

    Dictionary<ItemData, int> EnsureStockBank()
    {
        if (shop == null) return null;
        if (!stockBank.TryGetValue(shop.shopId, out var bank))
        {
            bank = new Dictionary<ItemData, int>();
            if (shop.stock != null)
                foreach (var it in shop.stock)
                    if (it != null && !bank.ContainsKey(it)) bank[it] = Mathf.Max(0, shop.defaultStock);
            stockBank[shop.shopId] = bank;
        }
        return bank;
    }

    /// <summary>현재 상점에서 item의 남은 재고. defaultStock ≤ 0 = 무제한(int.MaxValue).</summary>
    int StockRemaining(ItemData item)
    {
        if (item == null || shop == null) return 0;
        if (shop.defaultStock <= 0) return int.MaxValue;   // 무제한
        var bank = EnsureStockBank();
        return bank != null && bank.TryGetValue(item, out int n) ? n : shop.defaultStock;
    }

    /// <summary>구매분만큼 재고 차감(무제한이면 무시).</summary>
    void DecrementStock(ItemData item, int qty)
    {
        if (item == null || shop == null || shop.defaultStock <= 0 || qty <= 0) return;
        var bank = EnsureStockBank();
        if (bank == null) return;
        int cur = bank.TryGetValue(item, out int n) ? n : shop.defaultStock;
        bank[item] = Mathf.Max(0, cur - qty);
    }

    // ── 위탁(consignment) — 그레이박스 ────────────────────────
    // 위탁 = 직접 판매보다 높은 정산(1.5배)이지만 시간이 걸린다(그레이박스 30초).
    // 슬롯 3개는 상점 공용(static) 런타임 상태 — 세이브하지 않는다.
    const int   CONSIGN_SLOTS    = 3;
    const float CONSIGN_DURATION = 30f;   // 그레이박스 정산 시간(초, unscaledTime)

    class ConsignSlot
    {
        public ItemData item;       // null = 비었음
        public string   itemName;   // 표시용
        public int      payout;     // 정산 시 지급될 스크랩(위탁가)
        public float    readyTime;  // Time.unscaledTime 기준 정산 완료 시각
        public bool IsEmpty => item == null;
        public bool IsReady => item != null && Time.unscaledTime >= readyTime;
    }

    static readonly ConsignSlot[] consignSlots =
    {
        new ConsignSlot(), new ConsignSlot(), new ConsignSlot()
    };

    enum ShopTab { Trade, Consign, Wanted }
    ShopTab         currentTab = ShopTab.Trade;
    bool            consignMode => currentTab == ShopTab.Consign;  // 위탁 슬롯 폴링용
    [SerializeField] GameObject      tabBar;               // 탭 버튼 컨테이너 (전부 숨길 때 사용)
    [SerializeField] GameObject      tradeBody;            // 구매/판매 3컬럼 본문
    [SerializeField] GameObject      consignBody;          // 위탁 본문
    [SerializeField] GameObject      wantedBody;           // 수배 본문
    [SerializeField] Text            tabTradeLabel;
    [SerializeField] Text            tabConsignLabel;
    [SerializeField] Text            tabWantedLabel;
    [SerializeField] Image           tabTradeBg;
    [SerializeField] Image           tabConsignBg;
    [SerializeField] Image           tabWantedBg;
    [SerializeField] RectTransform   consignSellList;      // 좌: 가방 판매가능 아이템 목록 루트(동적 행은 런타임)
    [SerializeField] Text            consignSellEmpty;
    [SerializeField] RectTransform   wantedList;           // 수배 행 목록 루트(동적 행은 런타임)
    [SerializeField] Text            wantedEmpty;
    // 위탁 슬롯 3개 = 고정 골격(BuildConsignView가 생성). 동적 셀 아님 → 직렬화.
    [SerializeField] Image[]  consignSlotBg     = new Image[CONSIGN_SLOTS];
    [SerializeField] Text[]   consignSlotTitle  = new Text[CONSIGN_SLOTS];
    [SerializeField] Text[]   consignSlotStatus = new Text[CONSIGN_SLOTS];
    [SerializeField] Button[] consignSlotBtn    = new Button[CONSIGN_SLOTS];

    static readonly Color C_TAB_ON   = new Color(0.34f, 0.30f, 0.18f, 1f);   // 활성 탭(탄)
    static readonly Color C_TAB_OFF  = new Color(0.135f, 0.130f, 0.115f, 1f);
    static readonly Color C_CONSIGN  = new Color(0.30f, 0.20f, 0.42f, 1f);  // 정산중
    static readonly Color C_READY    = new Color(0.16f, 0.40f, 0.20f, 1f);  // 정산완료

    /// <summary>위탁가 = 직접 판매가(sellRate 기반)의 1.5배(올림). 판매 불가(0)면 0.</summary>
    int ConsignPrice(ItemData item)
    {
        if (item == null || shop == null) return 0;
        int sell = shop.SellPrice(item);
        if (sell <= 0) return 0;
        return Mathf.CeilToInt(sell * 1.5f);
    }
    // ── Awake ────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        WireEvents();   // onClick은 프리팹에 직렬화 안 됨 → 정적 골격 버튼 리스너를 양쪽 경로에서 재부착
        // GridPanel(창고/트레이)은 plain 객체라 직렬화 안 됨 → 프리팹 인스턴스(GenerateUI 스킵)는 여기서 셋업.
        // 코드생성 경로는 GenerateUI→SetupDragGrids가 처리(canvas==null이면 GenerateUI 전이므로 건너뜀).
        if (IsGenerated) SetupDragGrids();
    }

    /// <summary>
    /// 정적 골격 버튼(탭/구매·판매/트레이/수량/뒤로/닫기 등)의 onClick 재부착.
    /// 프리팹 인스턴스는 GenerateUI를 스킵하므로(빌드 1회) 직렬화된 버튼 ref에 리스너를 다시 건다.
    /// 동적 셀/행/컨텍스트 메뉴 버튼은 Refresh* 시 매번 다시 그리며 자체적으로 재부착하므로 여기서 건드리지 않는다.
    /// RemoveAllListeners로 코드생성 경로의 중복 부착도 방지.
    /// </summary>
    void WireEvents()
    {
        // 탭 (거래/위탁/수배) — 버튼은 탭 배경 Image와 같은 GameObject에 있음.
        Button tabTradeBtn   = tabTradeBg   != null ? tabTradeBg.GetComponent<Button>()   : null;
        Button tabConsignBtn = tabConsignBg != null ? tabConsignBg.GetComponent<Button>() : null;
        Button tabWantedBtn  = tabWantedBg  != null ? tabWantedBg.GetComponent<Button>()  : null;
        if (tabTradeBtn   != null) { tabTradeBtn.onClick.RemoveAllListeners();   tabTradeBtn.onClick.AddListener(() => SetTab(ShopTab.Trade)); }
        if (tabConsignBtn != null) { tabConsignBtn.onClick.RemoveAllListeners(); tabConsignBtn.onClick.AddListener(() => SetTab(ShopTab.Consign)); }
        if (tabWantedBtn  != null) { tabWantedBtn.onClick.RemoveAllListeners();  tabWantedBtn.onClick.AddListener(() => SetTab(ShopTab.Wanted)); }

        // 닫기(✕)
        if (closeBtn != null) { closeBtn.onClick.RemoveAllListeners(); closeBtn.onClick.AddListener(Close); }

        // 구매/판매 액션
        if (actionBuyBtn  != null) { actionBuyBtn.onClick.RemoveAllListeners();  actionBuyBtn.onClick.AddListener(() => { if (selectedStock != null) Buy(selectedStock, buyQty); }); }
        if (actionSellBtn != null) { actionSellBtn.onClick.RemoveAllListeners(); actionSellBtn.onClick.AddListener(() => { if (selectedInv != null) Sell(selectedInv); }); }

        // 구매 수량 스테퍼 (− / +)
        if (buyMinusBtn != null) { buyMinusBtn.onClick.RemoveAllListeners(); buyMinusBtn.onClick.AddListener(() => { buyQty = Mathf.Max(1, buyQty - 1); RefreshBuyBox(); }); }
        if (buyPlusBtn  != null) { buyPlusBtn.onClick.RemoveAllListeners();  buyPlusBtn.onClick.AddListener(() => { buyQty += 1; RefreshBuyBox(); }); }

        // 판매 트레이 (판매 / 되돌리기)
        if (sellTrayBtn   != null) { sellTrayBtn.onClick.RemoveAllListeners();   sellTrayBtn.onClick.AddListener(SellTrayAll); }
        if (returnTrayBtn != null) { returnTrayBtn.onClick.RemoveAllListeners(); returnTrayBtn.onClick.AddListener(() => ReturnTrayAll(true)); }

        // 창고 '← 뒤로' — GameObject로 직렬화되므로 같은 GO의 Button을 집는다.
        Button backBtn = stashBackBtn != null ? stashBackBtn.GetComponent<Button>() : null;
        if (backBtn != null) { backBtn.onClick.RemoveAllListeners(); backBtn.onClick.AddListener(CloseBag); }

        // 가방 팝업 (정렬 / 닫기)
        Button sortBtn = bagSortBtn != null ? bagSortBtn.GetComponent<Button>() : null;
        if (sortBtn != null) { sortBtn.onClick.RemoveAllListeners(); sortBtn.onClick.AddListener(SortBagPopup); }
        if (bagCloseBtn != null) { bagCloseBtn.onClick.RemoveAllListeners(); bagCloseBtn.onClick.AddListener(CloseBagPopup); }

        // 위탁 슬롯 3개 (정산 수령) — 인덱스별 동일 핸들러.
        if (consignSlotBtn != null)
            for (int i = 0; i < consignSlotBtn.Length; i++)
            {
                int idx = i;
                var b = consignSlotBtn[i];
                if (b != null) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => CollectConsign(idx)); }
            }
    }

    // ── 공개 메서드 ──────────────────────────────────────────
    public void Open(ShopData shopData)
    {
        if (shopData == null) return;
        if (!IsGenerated) GenerateUI();
        shop = shopData;
        var stash = MainStash.Ensure();
        stashGrid = stash != null ? stash.GetGrid() : null;
        EnsureSellTray();
        shopOpenBag = null;
        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        panel.SetActive(true);
        ClearSelection();
        UpdateTabVisibility();
        SetTab(ShopTab.Trade);   // 항상 거래 뷰로 시작
        RefreshAll();
    }

    /// <summary>탭(위탁/수배) 가시성 갱신. 둘 다 없으면 탭 바 전체를 숨긴다.</summary>
    void UpdateTabVisibility()
    {
        bool consign = shop != null && shop.allowConsignment;   // 위탁 = 전당포만
        bool wanted  = shop != null && shop.HasWanted;
        if (tabConsignBg != null) tabConsignBg.gameObject.SetActive(consign);
        if (tabWantedBg  != null) tabWantedBg.gameObject.SetActive(wanted);
        // 위탁/수배가 하나라도 있어야 탭 바(거래 탭 포함)를 보인다.
        bool showBar = consign || wanted;
        if (tabBar != null) tabBar.SetActive(showBar);
    }

    // ── 위탁 슬롯 완료 체크 (MonoBehaviour Update) ────────────
    float consignPollTimer;
    void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        if (!consignMode) return;
        // 매 프레임 전부 다시 그릴 필요 없음 — 0.5초마다 상태 갱신(타이머 표시·완료 전환)
        consignPollTimer -= Time.unscaledDeltaTime;
        if (consignPollTimer <= 0f)
        {
            consignPollTimer = 0.5f;
            RefreshConsignSlots();
        }
    }

    public void Close()
    {
        ReturnTrayAll(false);   // 트레이에 남은 물건은 창고로 되돌림(분실 방지)
        shopOpenBag = null;
        CloseBagPopup();
        HideShopMenu();
        if (panel != null) panel.SetActive(false);
        ClearSelection();
    }

    // ── 선택 ─────────────────────────────────────────────────
    void ClearSelection()
    {
        selectedStock = null;
        selectedInv   = null;
    }

    // 좌(상인 재고) 선택 = 구매 박스 / 우(창고) 선택 = 판매 박스 — 서로 독립(둘 다 동시 선택 가능).
    void SelectStock(ItemData item)
    {
        selectedStock = item;
        buyQty = 1;
        RefreshBuyBox();   // 재고 격자는 다시 그리지 않음(전체 반짝 방지) — 선택은 구매 박스로 표시
    }

    // ── 거래 ─────────────────────────────────────────────────
    void Buy(ItemData item, int qty = 1)
    {
        if (item == null || shop == null) return;
        int price = shop.BuyPrice(item);
        if (price <= 0) return;
        qty = Mathf.Max(1, qty);

        int remain = StockRemaining(item);
        if (remain <= 0)
        {
            ToastManager.Show("품절된 물건", ToastManager.ToastType.Warning);
            ShowTradeResult("품절되었습니다.", new Color(1f, 0.6f, 0.3f));
            return;
        }
        qty = Mathf.Min(qty, remain);

        int bought = 0;
        for (int i = 0; i < qty; i++)
        {
            if (CurrencyManager.Instance == null || !CurrencyManager.Instance.Spend(price, $"구매: {item.displayName}"))
            {
                if (bought == 0) { ToastManager.Show("스크랩이 부족하다", ToastManager.ToastType.Warning); ShowTradeResult("잔액이 부족합니다.", new Color(1f, 0.4f, 0.3f)); }
                break;
            }
            if (stashGrid == null || !stashGrid.TryAutoPlace(new ItemInstance(item, 1)))
            {
                CurrencyManager.Instance.Add(price, "환불(공간 부족)");
                if (bought == 0) { ToastManager.Show("창고 공간 부족", ToastManager.ToastType.Warning); ShowTradeResult("창고 공간 부족.", new Color(1f, 0.4f, 0.3f)); }
                else ToastManager.Show("창고 공간 부족 — 일부만 구매", ToastManager.ToastType.Warning);
                break;
            }
            bought++;
        }

        if (bought > 0)
        {
            DecrementStock(item, bought);
            ShowTradeResult($"{item.displayName} ×{bought} 구매 완료. (-◈{(price * bought):N0})", new Color(0.4f, 1f, 0.5f));
            buyQty = 1;
            RefreshAll();   // 재고 수량 배지/품절 표시까지 갱신(구매=재고 변동, 갱신 정당)
            SaveCheckpoints.Instance?.InventoryChanged();   // 거래 = 자동 세이브(안전구역 커밋)
        }
    }

    void Sell(InventoryGrid.PlacedItem placed)
    {
        if (placed?.item?.data == null || shop == null) return;

        // 컨테이너(가방/보관함)는 내용물째 팔리지 않게 — 비운 뒤에만 판매.
        if (placed.item.IsContainer)
        {
            var cg = placed.item.ContainerGrid;
            if (cg != null && cg.GetAll().Count > 0)
            {
                ToastManager.Show("가방·보관함은 내용물을 비운 뒤 판매하세요", ToastManager.ToastType.Warning);
                ShowTradeResult("내용물을 비우고 판매하세요.", new Color(1f, 0.6f, 0.3f));
                return;
            }
        }

        int total = shop.SellPrice(placed.item.data) * placed.item.stackCount;
        if (total <= 0) return;
        string name = placed.item.DisplayName;
        CurrencyManager.Instance?.Add(total, $"판매: {name}");
        stashGrid.Remove(placed);
        selectedInv = null;
        ShowTradeResult($"{name} 판매 완료. (+◈{total:N0})", new Color(0.4f, 1f, 0.5f));
        RefreshAll();
        SaveCheckpoints.Instance?.InventoryChanged();
    }

    void ShowTradeResult(string msg, Color col)
    {
        if (tradeResultText == null) return;
        tradeResultText.text  = msg;
        tradeResultText.color = col;
    }

    // ── 탭 전환 (거래/위탁/수배) ──────────────────────────────
    void SetTab(ShopTab tab)
    {
        currentTab = tab;
        if (tradeBody   != null) tradeBody.SetActive(tab == ShopTab.Trade);
        if (consignBody != null) consignBody.SetActive(tab == ShopTab.Consign);
        if (wantedBody  != null) wantedBody.SetActive(tab == ShopTab.Wanted);
        if (tabTradeBg   != null) tabTradeBg.color   = tab == ShopTab.Trade   ? C_TAB_ON : C_TAB_OFF;
        if (tabConsignBg != null) tabConsignBg.color = tab == ShopTab.Consign ? C_TAB_ON : C_TAB_OFF;
        if (tabWantedBg  != null) tabWantedBg.color  = tab == ShopTab.Wanted  ? C_TAB_ON : C_TAB_OFF;
        if (tab == ShopTab.Consign) RefreshConsign();
        if (tab == ShopTab.Wanted)  RefreshWanted();
    }

    /// <summary>가방의 판매가능 아이템 1개를 빈 위탁 슬롯에 올린다(그리드에서 1개 제거).</summary>
    void Consign(InventoryGrid.PlacedItem placed)
    {
        if (placed?.item?.data == null || shop == null || stashGrid == null) return;
        int slotIdx = FindEmptyConsignSlot();
        if (slotIdx < 0)
        {
            ToastManager.Show("빈 위탁 슬롯이 없다", ToastManager.ToastType.Warning);
            return;
        }
        var data  = placed.item.data;
        int price = ConsignPrice(data);
        if (price <= 0) return;

        // 그리드에서 1개 제거 (스택이면 수량 1 감소, 마지막이면 PlacedItem 제거)
        if (placed.item.stackCount > 1)
        {
            placed.item.stackCount -= 1;
            stashGrid.NotifyChanged();
        }
        else
        {
            stashGrid.Remove(placed);
        }

        var slot       = consignSlots[slotIdx];
        slot.item      = data;
        slot.itemName  = data.displayName;
        slot.payout    = price;
        slot.readyTime = Time.unscaledTime + CONSIGN_DURATION;

        ToastManager.Show($"{data.displayName} 위탁 (정산가 ◈{price:N0})", ToastManager.ToastType.Info);
        RefreshConsign();
    }

    /// <summary>정산완료 슬롯 수령 → 스크랩 지급 + 슬롯 비움.</summary>
    void CollectConsign(int slotIdx)
    {
        if (slotIdx < 0 || slotIdx >= CONSIGN_SLOTS) return;
        var slot = consignSlots[slotIdx];
        if (slot.IsEmpty || !slot.IsReady) return;

        int payout = slot.payout;
        // 슬롯을 먼저 비우고 커밋 — 지급 후 커밋 시점에 슬롯이 남아 있으면
        // 강제종료→재로드로 같은 정산을 무한 재수령할 수 있다
        slot.item      = null;
        slot.itemName  = null;
        slot.payout    = 0;
        slot.readyTime = 0f;
        CurrencyManager.Instance?.Add(payout, "위탁 정산");
        SaveCheckpoints.Instance?.InventoryChanged();
        RefreshConsign();
    }

    int FindEmptyConsignSlot()
    {
        for (int i = 0; i < CONSIGN_SLOTS; i++)
            if (consignSlots[i].IsEmpty) return i;
        return -1;
    }

    // ── 위탁 슬롯 세이브/로드 (전당포 위탁을 영속화 — '시스템적') ────────
    [System.Serializable]
    public class ConsignSave
    {
        public string itemId;
        public string itemName;
        public int    payout;
        public float  remaining;   // 정산까지 남은 초(저장 시점 기준)
    }

    public static List<ConsignSave> GetConsignSave()
    {
        var list = new List<ConsignSave>();
        foreach (var s in consignSlots)
        {
            if (s == null || s.IsEmpty) continue;
            list.Add(new ConsignSave
            {
                itemId    = s.item != null ? s.item.itemId : null,
                itemName  = s.itemName,
                payout    = s.payout,
                remaining = Mathf.Max(0f, s.readyTime - Time.unscaledTime),
            });
        }
        return list;
    }

    public static void LoadConsignSave(List<ConsignSave> data)
    {
        // 전부 비우고 복원
        foreach (var s in consignSlots) { s.item = null; s.itemName = null; s.payout = 0; s.readyTime = 0f; }
        if (data == null) return;
        int idx = 0;
        foreach (var d in data)
        {
            if (idx >= consignSlots.Length) break;
            var s = consignSlots[idx++];
            s.item      = !string.IsNullOrEmpty(d.itemId) ? ItemDatabase.Get(d.itemId) : null;
            s.itemName  = d.itemName;
            s.payout    = d.payout;
            s.readyTime = Time.unscaledTime + Mathf.Max(0f, d.remaining);
        }
    }

    // ── 판매 트레이 세이브/로드 (상점 열림 중 커밋 시 트레이 물건 보존) ────────
    // 트레이는 런타임 전용 격자라, 물건이 트레이에 올라간 상태에서 다른 거래(구매 등)가
    // 커밋(InventoryChanged)을 일으키면 세이브엔 그 물건이 창고에도 트레이에도 없는 상태로
    // 기록된다 — 그 시점 크래시/강제종료 시 아이템 유실. 커밋에 트레이 내용을 포함하고
    // 로드 시 창고로 반환해서 막는다(위탁 슬롯 영속화와 같은 패턴).
    public static List<GridItemEntry> GetSellTraySave()
    {
        var tray = Instance != null ? Instance.sellTray : null;
        if (tray == null || tray.GetAll().Count == 0) return null;
        return tray.GetSaveData();
    }

    public static void LoadSellTraySave(List<GridItemEntry> data)
    {
        // 런타임 트레이는 항상 비운다 — 세션 중 로드 시 이전 트레이 잔여물이 팔리는 것(복제) 방지
        Instance?.sellTray?.Clear();
        if (data == null || data.Count == 0) return;
        var stash = MainStash.Ensure()?.GetGrid();
        foreach (var e in data)
        {
            var inst = InventoryGrid.InstanceFromEntry(e);
            if (inst == null) continue;
            if (stash == null || !stash.TryAutoPlace(inst))
                Debug.LogWarning($"[Save] 판매 트레이 복구 실패(창고 공간 부족): {e.itemId} ×{e.count}");
        }
    }

    // ── 전체 갱신 ────────────────────────────────────────────
    void RefreshAll()
    {
        RefreshTopBar();
        RefreshStockCells();
        RefreshBuyBox();
        RefreshSellTray();
        RefreshInvGrid();
    }

    /// <summary>거래 후 갱신 — 재고 격자는 손대지 않아 전체 반짝을 막는다(잔액·창고·트레이만).</summary>
    void RefreshTrade()
    {
        RefreshTopBar();
        RefreshSellTray();
        RefreshInvGrid();
    }

    void RefreshTopBar()
    {
        if (traderNameText != null) traderNameText.text = shop != null ? shop.shopName : "상인";
        if (trustText != null)
        {
            if (ReputationManager.Instance != null)
            {
                var rm = ReputationManager.Instance;
                trustText.text = $"평판 {rm.Reputation} [{rm.TierName} {rm.Tier}]";
            }
            else
            {
                trustText.text = "평판 -";
            }
        }
        UpdateBalance();
    }

    void UpdateBalance()
    {
        if (balanceText == null) return;
        int bal = CurrencyManager.Instance != null ? CurrencyManager.Instance.Balance : 0;
        balanceText.text = $"◈ {bal:N0}";
    }

    // ── 좌: 상인 재고 그리드 (footprint — 창고/인벤과 동일) ──────
    void RefreshStockCells()
    {
        if (stockSlotRoot == null || stockItemRoot == null) return;
        ClearChildren(stockSlotRoot);
        ClearChildren(stockItemRoot);

        if (shop == null || shop.stock == null)
        {
            SetActive(stockEmptyText, true, "판매 중인 물건이 없습니다.");
            return;
        }

        int cellTotal = CELL_SIZE + CELL_GAP;
        float availW = stockContent != null ? stockContent.rect.width : 0f;
        int cols = availW > 0f ? Mathf.Clamp(Mathf.FloorToInt((availW - 16) / cellTotal), 3, 8) : 8;   // 창고와 동일 8칸

        var repTier = ReputationManager.Instance != null ? ReputationManager.Instance.Tier : ReputationTier.F;

        // 구매 가능 재고를 가상 격자에 footprint 자동 배치
        var vgrid = new InventoryGrid(cols, 80);
        var entries = new List<StockEntry>();
        foreach (var item in shop.stock)
        {
            if (item == null) continue;
            int price = shop.BuyPrice(item);
            if (price <= 0) continue;
            var inst = new ItemInstance(item, 1);
            if (!vgrid.TryAutoPlace(inst)) continue;
            InventoryGrid.PlacedItem placed = null;
            foreach (var pl in vgrid.GetAll()) if (pl.item == inst) { placed = pl; break; }
            if (placed == null) continue;
            entries.Add(new StockEntry { p = placed, data = item, price = price, locked = repTier < RarityToRepTier(item.rarity) });
        }

        SetActive(stockEmptyText, entries.Count == 0, "판매 중인 물건이 없습니다.");
        if (entries.Count == 0) return;

        int usedRows = 1;
        foreach (var e in entries) usedRows = Mathf.Max(usedRows, e.p.gridY + e.p.EffectiveHeight);
        // 아이템이 적어도 영역(뷰포트)을 가득 채우는 빈 슬롯 격자 — 1줄이어도 전체 격자.
        float areaH = (stockContent != null && stockContent.parent is RectTransform pr) ? pr.rect.height : 0f;
        int fillRows = areaH > 0f ? Mathf.Max(1, Mathf.FloorToInt((areaH - 12) / cellTotal)) : 8;
        int rows = Mathf.Max(usedRows, fillRows);

        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                var slot = MakeRect("Slot", stockSlotRoot.transform);
                var srt  = slot.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(0, 1); srt.pivot = new Vector2(0, 1);
                srt.anchoredPosition = new Vector2(c * cellTotal, -r * cellTotal);
                srt.sizeDelta = new Vector2(CELL_SIZE, CELL_SIZE);
                var slotImg = slot.AddComponent<Image>();
                slotImg.color = UITheme.PanelAlt;   // 인벤과 동일
                UISkin.Cell(slotImg);   // 시안: storage_box 빈 격자 셀(폴백: 색 유지)
            }

        if (stockContent != null)
            stockContent.sizeDelta = new Vector2(stockContent.sizeDelta.x, rows * cellTotal + 16);

        foreach (var e in entries)
        {
            var captured = e.data;
            int remain = StockRemaining(e.data);
            AddStockGridCell(stockItemRoot, e.p, e.data, e.price, e.locked, remain, selectedStock == e.data, () => SelectStock(captured));
        }
    }

    class StockEntry { public InventoryGrid.PlacedItem p; public ItemData data; public int price; public bool locked; }

    void AddStockGridCell(RectTransform parent, InventoryGrid.PlacedItem placed, ItemData data, int price, bool locked, int remaining, bool selected, System.Action onClick)
    {
        if (parent == null || placed == null || data == null) return;   // 안전 가드(NRE 방지)
        bool soldOut = remaining <= 0;

        int cellTotal = CELL_SIZE + CELL_GAP;
        int cw = placed.EffectiveWidth, ch = placed.EffectiveHeight;
        float pw = cw * CELL_SIZE + (cw - 1) * CELL_GAP;
        float ph = ch * CELL_SIZE + (ch - 1) * CELL_GAP;

        var cell = MakeRect("StockItem", parent.transform);
        var crt  = cell.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(0, 1); crt.pivot = new Vector2(0, 1);
        crt.anchoredPosition = new Vector2(placed.gridX * cellTotal, -placed.gridY * cellTotal);
        crt.sizeDelta = new Vector2(pw, ph);

        var baseCol = UITheme.RarityBg(data.rarity);   // 인벤과 동일한 희귀도 배경
        var bg = cell.AddComponent<Image>();
        bg.color = selected
            ? new Color(baseCol.r + 0.18f, baseCol.g + 0.18f, baseCol.b + 0.18f, 0.98f)
            : baseCol;

        if (data.icon != null)
        {
            var ic = MakeRect("Icon", cell.transform); var irt = ic.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 0); irt.anchorMax = new Vector2(1, 1);
            irt.offsetMin = new Vector2(4, 4); irt.offsetMax = new Vector2(-4, -4);
            var im = ic.AddComponent<Image>(); im.sprite = data.icon; im.preserveAspect = true;
        }
        else
        {
            var ng = MakeRect("Name", cell.transform); var nrt = ng.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0); nrt.anchorMax = new Vector2(1, 1);
            nrt.offsetMin = new Vector2(2, 2); nrt.offsetMax = new Vector2(-2, -2);
            var nt = AddText(ng, data.displayName, 11, TextAnchor.MiddleCenter, Color.white);
            if (nt != null) nt.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        // 남은 수량 배지 (우하단) — 무제한/잠김/품절 아닐 때만
        if (!locked && !soldOut && remaining < int.MaxValue)
        {
            var qg = MakeRect("Qty", cell.transform); var qrt = qg.GetComponent<RectTransform>();
            qrt.anchorMin = new Vector2(1, 0); qrt.anchorMax = new Vector2(1, 0); qrt.pivot = new Vector2(1, 0);
            qrt.anchoredPosition = new Vector2(-2, 2); qrt.sizeDelta = new Vector2(34, 16);
            var qt = AddText(qg, $"x{remaining}", 11, TextAnchor.LowerRight, new Color(0.95f, 0.92f, 0.72f));
            if (qt != null) { qt.fontStyle = FontStyle.Bold; qg.AddComponent<Shadow>().effectColor = Color.black; }
        }

        // 오버레이(잠김 / 품절) — 클릭 불가
        if (locked || soldOut)
        {
            var lk = MakeRect("Overlay", cell.transform); var lrt = lk.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            lk.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            var lt = AddText(lk, locked ? "잠김" : "품절", 11, TextAnchor.MiddleCenter,
                locked ? new Color(0.7f, 0.6f, 0.4f) : new Color(0.92f, 0.5f, 0.42f));
            if (lt != null) lt.fontStyle = FontStyle.Bold;
            return;
        }

        var btn = cell.AddComponent<Button>();
        btn.targetGraphic = bg;
        var cb = btn.colors;
        cb.normalColor = bg.color;
        cb.highlightedColor = new Color(bg.color.r + 0.08f, bg.color.g + 0.10f, bg.color.b + 0.18f, 0.95f);
        cb.pressedColor = new Color(bg.color.r - 0.04f, bg.color.g - 0.04f, bg.color.b - 0.04f, 0.95f);
        cb.selectedColor = bg.color;
        btn.colors = cb;
        btn.onClick.AddListener(() =>
        {
            onClick?.Invoke();
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        });
    }

    // 희귀도별 해금 평판 등급 — GameTuning(Control Panel)에서 조정. 에셋 없으면 기본값 폴백.
    static ReputationTier RarityToRepTier(ItemRarity rarity)
    {
        var t = GameTuning.Instance;
        switch (rarity)
        {
            case ItemRarity.Rare:      return t != null ? t.shopTierRare      : ReputationTier.D;
            case ItemRarity.Epic:      return t != null ? t.shopTierEpic      : ReputationTier.B;
            case ItemRarity.Legendary: return t != null ? t.shopTierLegendary : ReputationTier.A;
            default:                   return ReputationTier.F;   // Common/Uncommon = 항상 해금
        }
    }

    void AddLockedStockCell(RectTransform parent, ItemData data, ReputationTier requiredTier)
    {
        var cell   = MakeRect("LockedCell", parent.transform);
        var bg = cell.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.06f, 0.08f, 0.8f);

        var rarBar = MakeRect("RarBar", cell.transform);
        var rarRT  = rarBar.GetComponent<RectTransform>();
        rarRT.anchorMin = new Vector2(0, 0); rarRT.anchorMax = new Vector2(0, 1);
        rarRT.offsetMin = Vector2.zero;      rarRT.offsetMax  = new Vector2(3, 0);
        rarBar.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f);

        var lockGO = MakeRect("Lock", cell.transform);
        var lockRT = lockGO.GetComponent<RectTransform>();
        lockRT.anchorMin = new Vector2(0, 0.2f); lockRT.anchorMax = new Vector2(1, 1);
        lockRT.offsetMin = new Vector2(4, 2);     lockRT.offsetMax  = new Vector2(-4, -2);
        var lt = AddText(lockGO, "잠김", 11, TextAnchor.MiddleCenter, new Color(0.45f, 0.45f, 0.5f));
        lt.fontStyle = FontStyle.Bold;

        var tierGO = MakeRect("Tier", cell.transform);
        var tierRT = tierGO.GetComponent<RectTransform>();
        tierRT.anchorMin = new Vector2(0, 0); tierRT.anchorMax = new Vector2(1, 0.26f);
        tierRT.offsetMin = Vector2.zero;       tierRT.offsetMax  = Vector2.zero;
        tierGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.45f);
        AddText(tierGO.GetComponent<RectTransform>(), $"평판 {requiredTier}", 9, TextAnchor.MiddleCenter, new Color(0.6f, 0.5f, 0.3f));
    }

    void AddStockCell(RectTransform parent, ItemData data, int price, bool selected, System.Action onClick)
    {
        var cell   = MakeRect("StockCell", parent.transform);
        var cellRT = cell.GetComponent<RectTransform>();
        // GridLayoutGroup handles size; set via GLG cell size

        var bg = cell.AddComponent<Image>();
        bg.color = selected ? C_CELL_SEL : C_CELL;

        var btn = cell.AddComponent<Button>();
        btn.targetGraphic = bg;
        var cb = btn.colors;
        cb.normalColor      = selected ? C_CELL_SEL : C_CELL;
        cb.highlightedColor = C_CELL_HOVER;
        cb.pressedColor     = new Color(0.28f, 0.24f, 0.14f);
        cb.selectedColor    = cb.normalColor;   // EventSystem '선택 유지' 반짝 제거(선택 표시는 bg로)
        btn.colors = cb;
        btn.onClick.AddListener(() =>
        {
            onClick?.Invoke();
            // 클릭 후 포커스 해제 — 셀 전체가 잔상처럼 반짝이는 현상 방지.
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        });

        // 희귀도 왼쪽 테두리 (2px)
        var rarBar = MakeRect("RarBar", cell.transform);
        var rarRT  = rarBar.GetComponent<RectTransform>();
        rarRT.anchorMin = new Vector2(0, 0); rarRT.anchorMax = new Vector2(0, 1);
        rarRT.offsetMin = Vector2.zero;      rarRT.offsetMax  = new Vector2(3, 0);
        rarBar.AddComponent<Image>().color = data != null ? data.RarityColor : Color.gray;

        // 아이콘 또는 이름
        if (data != null && data.icon != null)
        {
            var iconGO = MakeRect("Icon", cell.transform);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0, 0.26f); iconRT.anchorMax = new Vector2(1, 1);
            iconRT.offsetMin = new Vector2(5, 2);     iconRT.offsetMax  = new Vector2(-5, -2);
            iconGO.AddComponent<Image>().sprite = data.icon;
            iconGO.GetComponent<Image>().preserveAspect = true;
        }
        else
        {
            var nameGO = MakeRect("Name", cell.transform);
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.26f); nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = new Vector2(4, 2);     nameRT.offsetMax  = new Vector2(-4, -2);
            var nt = AddText(nameGO, data != null ? data.displayName : "?", 10, TextAnchor.MiddleCenter, Color.white);
            nt.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        // 가격 하단 바
        var priceBg = MakeRect("PriceBg", cell.transform);
        var pbRT    = priceBg.GetComponent<RectTransform>();
        pbRT.anchorMin = new Vector2(0, 0); pbRT.anchorMax = new Vector2(1, 0.26f);
        pbRT.offsetMin = Vector2.zero;       pbRT.offsetMax  = Vector2.zero;
        priceBg.AddComponent<Image>().color = new Color(0, 0, 0, 0.45f);
        AddText(priceBg.GetComponent<RectTransform>(), $"◈{price:N0}", 9, TextAnchor.MiddleCenter, C_GOLD);
    }

    // ── 중·상: 구매(BUY) 박스 — 좌 상인 재고 선택을 표시 ─────────
    void RefreshBuyBox()
    {
        var  data   = selectedStock;
        int  remain = data != null ? StockRemaining(data) : 0;
        bool has    = data != null && shop != null && shop.BuyPrice(data) > 0 && remain > 0;
        int  price  = has ? shop.BuyPrice(data) : 0;

        if (buyEmpty != null) buyEmpty.SetActive(!has);

        if (buyIcon != null)
        {
            buyIcon.sprite  = has ? data.icon : null;
            buyIcon.enabled = has && data.icon != null;
            buyIcon.color   = Color.white;
        }
        if (buyName != null)
        {
            buyName.text  = has ? data.displayName : "";
            buyName.color = has ? data.RarityColor : C_MUTED;
        }
        if (buyInfo != null)
        {
            string stockTxt = (has && remain < int.MaxValue) ? $" · 재고 {remain}" : "";
            buyInfo.text = has
                ? $"{RarityLabel(data.rarity)} · {data.category} · {data.gridWidth}×{data.gridHeight} · {data.weight:F2}kg{stockTxt}"
                : "";
        }
        if (actionBuyBtn != null) actionBuyBtn.gameObject.SetActive(has);
        if (buyQtyRow != null) buyQtyRow.SetActive(has);
        if (has)
        {
            int qtyMax = remain == int.MaxValue ? 99 : remain;
            buyQty = Mathf.Clamp(buyQty, 1, Mathf.Max(1, qtyMax));
            if (buyQtyText != null) buyQtyText.text = buyQty.ToString();
            if (actionBuyLabel != null)
                actionBuyLabel.text = buyQty > 1 ? $"구매 ×{buyQty}  ◈{(price * buyQty):N0}" : $"구매  ◈{price:N0}";
        }

        UpdateBalance();
    }

    // ── 중·하: 판매(SELL) 박스 — 우 창고 선택을 표시 ────────────
    void RefreshSellBox()
    {
        var placed = selectedInv;
        var data   = placed?.item?.data;
        bool has   = data != null && shop != null && shop.SellPrice(data) > 0;
        int  each  = has ? shop.SellPrice(data) : 0;
        int  total = has ? each * placed.item.stackCount : 0;

        if (sellEmpty != null) sellEmpty.SetActive(!has);

        if (sellIcon != null)
        {
            sellIcon.sprite  = has ? data.icon : null;
            sellIcon.enabled = has && data.icon != null;
            sellIcon.color   = Color.white;
        }
        if (sellName != null)
        {
            sellName.text  = has
                ? (placed.item.stackCount > 1 ? $"{data.displayName} (x{placed.item.stackCount})" : data.displayName)
                : "";
            sellName.color = has ? data.RarityColor : C_MUTED;
        }
        if (sellInfo != null)
        {
            sellInfo.text = has
                ? $"{RarityLabel(data.rarity)} · {data.category} · 개당 ◈{each:N0}"
                : "";
        }
        if (actionSellBtn != null) actionSellBtn.gameObject.SetActive(has);
        if (has && actionSellLabel != null) actionSellLabel.text = $"판매  ◈{total:N0}";
    }

    // ── 우: 창고 그리드 (드래그 격자 = 인벤 동일) ──────────────
    void RefreshInvGrid()
    {
        // 제목 + 뒤로 버튼 (가방 내부면 가방명/뒤로)
        if (stashHeaderText != null)
            stashHeaderText.text = shopOpenBag != null ? $"📦 {shopOpenBag.data.displayName}" : "창고";
        if (stashBackBtn != null) stashBackBtn.SetActive(shopOpenBag != null);

        if (stashPanel == null) return;
        stashPanel.grid = CurrentStashGrid;
        stashPanel.Refresh();
    }

    // ── 판매 트레이 + 컨테이너 열기 ───────────────────────────
    void OnStashClick(InventoryGrid.PlacedItem placed)
    {
        if (placed?.item?.data == null) return;
        var item = placed.item;

        // 내용물 있는 컨테이너 → 열어서 내부 보기(통째 판매 방지)
        if (item.IsContainer && item.ContainerGrid != null && item.ContainerGrid.GetAll().Count > 0 && item != shopOpenBag)
        {
            shopOpenBag = item;
            RefreshInvGrid();
            return;
        }

        // 판매 불가 품목 거르기
        if (shop == null || shop.SellPrice(item.data) <= 0)
        {
            ToastManager.Show("여기선 팔 수 없는 물건", ToastManager.ToastType.Warning);
            return;
        }

        // 트레이에 담기 (현재 격자에서 빼서 트레이로)
        var from = CurrentStashGrid;
        from.Remove(placed);
        EnsureSellTray();
        if (!sellTray.TryAutoPlace(item))
        {
            from.TryPlace(item, placed.gridX, placed.gridY, placed.rotated);   // 복원
            ToastManager.Show("판매 트레이가 가득 참", ToastManager.ToastType.Warning);
            return;
        }
        RefreshTrade();
    }

    void CloseBag()
    {
        shopOpenBag = null;
        RefreshInvGrid();
    }

    /// <summary>트레이 아이템 클릭 = 창고로 되돌림.</summary>
    void RemoveFromTray(InventoryGrid.PlacedItem placed)
    {
        if (placed?.item == null || sellTray == null) return;
        var item = placed.item;
        sellTray.Remove(placed);
        if (stashGrid == null || !stashGrid.TryAutoPlace(item))
        {
            sellTray.TryPlace(item, placed.gridX, placed.gridY, placed.rotated);
            ToastManager.Show("창고 공간 부족", ToastManager.ToastType.Warning);
            return;
        }
        RefreshTrade();
    }

    void ReturnTrayAll(bool refresh)
    {
        if (sellTray == null) return;
        var items = new List<InventoryGrid.PlacedItem>(sellTray.GetAll());
        foreach (var p in items)
        {
            if (p?.item == null) continue;
            sellTray.Remove(p);
            if (stashGrid == null || !stashGrid.TryAutoPlace(p.item))
                MainStash.Ensure()?.GetGrid()?.TryAutoPlace(p.item);
        }
        if (refresh) RefreshTrade();
    }

    int SellTrayTotal()
    {
        int total = 0;
        if (sellTray != null && shop != null)
            foreach (var p in sellTray.GetAll())
                if (p?.item?.data != null) total += shop.SellPrice(p.item.data) * p.item.stackCount;
        return total;
    }

    void SellTrayAll()
    {
        if (sellTray == null || shop == null) return;
        var items = new List<InventoryGrid.PlacedItem>(sellTray.GetAll());
        if (items.Count == 0) { ToastManager.Show("판매 트레이가 비어 있다", ToastManager.ToastType.Info); return; }
        int total = SellTrayTotal();
        foreach (var p in items)
            if (p?.item != null) sellTray.Remove(p);
        CurrencyManager.Instance?.Add(total, "트레이 판매");
        ShowTradeResult($"{items.Count}종 판매 완료. (+◈{total:N0})", new Color(0.4f, 1f, 0.5f));
        RefreshTrade();
        SaveCheckpoints.Instance?.InventoryChanged();
    }

    // ── 드래그 격자 셋업(창고/트레이 = 인벤 동일 동작) ─────────
    void SetupDragGrids()
    {
        if (dragMgr == null) dragMgr = gameObject.AddComponent<GridDragManager>();
        dragMgr.Init(canvas != null ? canvas.GetComponent<RectTransform>() : null);
        dragMgr.onChanged = OnDragChanged;
        dragMgr.Clear();

        stashPanel = new GridPanel { slotRoot = invSlotRoot, itemRoot = invItemRoot, hitRoot = invSlotRoot };
        trayPanel  = new GridPanel { slotRoot = sellTraySlotRoot, itemRoot = sellTrayItemRoot, hitRoot = sellTraySlotRoot };

        // 창고(stash) 스크롤 = 일반 탭 창고와 동일 동작: 휠 빠르게 + 드래그-스크롤 버그 차단 + 스크롤바.
        // invSlotRoot이 [SerializeField]라 프리팹/코드 두 경로 모두 부모 ScrollRect를 런타임에 찾아 세팅(재베이크 불필요).
        // includeInactive=true: SetupDragGrids는 Awake(상점 패널 비활성)에 돌 수 있어, 비활성이면 GetComponentInParent가 null을 반환함.
        var stashSR = invSlotRoot != null ? invSlotRoot.GetComponentInParent<ScrollRect>(true) : null;
        if (stashSR != null)
        {
            stashSR.horizontal = false; stashSR.vertical = true;
            stashSR.scrollSensitivity = 40f;
            stashSR.movementType = ScrollRect.MovementType.Clamped;
            // 아이템을 들고 격자를 끌 때 창고가 멋대로 스크롤되던 버그 차단(휠은 통과).
            var content = invSlotRoot.parent as RectTransform;
            if (content != null && content.GetComponent<ScrollDragBlocker>() == null)
            {
                var blk = content.gameObject.AddComponent<ScrollDragBlocker>();
                blk.targetScroll = stashSR;
            }
            EnsureStashScrollbar(stashSR);
        }
        stashPanel.scrollRect = stashSR;

        stashPanel.onDrop = (item, src, x, y) =>
        {
            var g = CurrentStashGrid;
            bool ok = g != null && (g.TryPlace(item, x, y, false) || g.TryAutoPlace(item));
            if (ok) stashPanel.Refresh();
            return ok;
        };
        stashPanel.onRightClick = p => ShowShopMenu(p, stashGrid);

        trayPanel.canAccept = it => shop != null && it?.data != null && shop.SellPrice(it.data) > 0
            && !(it.IsContainer && it.ContainerGrid != null && it.ContainerGrid.GetAll().Count > 0);
        trayPanel.onDrop = (item, src, x, y) =>
        {
            EnsureSellTray();
            bool ok = sellTray.TryPlace(item, x, y, false) || sellTray.TryAutoPlace(item);
            if (ok) trayPanel.Refresh();
            return ok;
        };

        // Ctrl+클릭 = 창고 → 판매 트레이로 즉시 이동
        stashPanel.onCtrlClick = placed =>
        {
            var it = placed?.item;
            if (it?.data == null || shop == null || shop.SellPrice(it.data) <= 0) { ToastManager.Show("팔 수 없는 물건", ToastManager.ToastType.Warning); return; }
            if (it.IsContainer && it.ContainerGrid != null && it.ContainerGrid.GetAll().Count > 0) { ToastManager.Show("가방은 비우고 판매", ToastManager.ToastType.Warning); return; }
            var from = CurrentStashGrid;
            from.Remove(placed);
            EnsureSellTray();
            if (!sellTray.TryAutoPlace(it)) from.TryPlace(it, placed.gridX, placed.gridY, placed.rotated);
            RefreshTrade();
        };
        // Ctrl+클릭 = 판매 트레이 → 창고로 즉시 이동
        trayPanel.onCtrlClick = placed =>
        {
            var it = placed?.item;
            if (it == null || sellTray == null) return;
            sellTray.Remove(placed);
            if (stashGrid == null || !stashGrid.TryAutoPlace(it)) sellTray.TryPlace(it, placed.gridX, placed.gridY, placed.rotated);
            RefreshTrade();
        };
        trayPanel.onRightClick = p => ShowShopMenu(p, sellTray);

        // 가방 이동 팝업 패널
        BuildBagPopup();
        bagPanel.onDrop = (item, src, x, y) =>
        {
            var g = bagPanel.grid;
            bool ok = g != null && (g.TryPlace(item, x, y, false) || g.TryAutoPlace(item));
            if (ok) bagPanel.Refresh();
            return ok;
        };
        bagPanel.canAccept = it => bagPopupInst != null && it?.data != null
            && bagPopupInst.data.AcceptsCategory(it.data.category)
            && !(it.IsContainer && it.data.equipSlot != EquipSlot.None);   // 가방 안 가방 방지
        bagPanel.onCtrlClick = placed =>   // 가방 → 판매 트레이
        {
            var it = placed?.item;
            if (it?.data == null || bagPanel.grid == null) return;
            if (shop == null || shop.SellPrice(it.data) <= 0) { ToastManager.Show("팔 수 없는 물건", ToastManager.ToastType.Warning); return; }
            bagPanel.grid.Remove(placed);
            EnsureSellTray();
            if (!sellTray.TryAutoPlace(it)) bagPanel.grid.TryPlace(it, placed.gridX, placed.gridY, placed.rotated);
            bagPanel.Refresh();
            RefreshTrade();
        };
        bagPanel.onRightClick = p => ShowShopMenu(p, bagPanel.grid);

        dragMgr.Register(stashPanel);
        dragMgr.Register(trayPanel);
        dragMgr.Register(bagPanel);
    }

    void OnDragChanged()
    {
        RefreshTopBar();
        if (sellTrayBtnLabel != null) sellTrayBtnLabel.text = $"판매  ◈{SellTrayTotal():N0}";
    }

    void OnStashRightClick(InventoryGrid.PlacedItem placed)
    {
        if (placed?.item == null) return;
        var it = placed.item;
        // 컨테이너 → 열기/닫기(인플레이스). (완전한 열기/자세히/버리기 메뉴는 다음 단계)
        if (it.IsContainer && it.ContainerGrid != null)
        {
            shopOpenBag = (shopOpenBag == it) ? null : it;
            RefreshInvGrid();
            return;
        }
        // 일반 아이템 → 자세히 팝업
        ItemDetailUI.Show(it);
    }

    // ── 가방 이동 팝업 ────────────────────────────────────────
    const float BAG_HEADER_H = 30f, BAG_PAD = 8f;

    void BuildBagPopup()
    {
        bagPopup = MakeRect("BagPopup", canvas.transform);
        var rt = bagPopup.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(220, 200); rt.anchoredPosition = new Vector2(140, 0);
        var bagPopupImg = bagPopup.AddComponent<Image>();
        bagPopupImg.color = UITheme.Panel;
        UISkin.Panel(bagPopupImg);   // 시안: box 프레임(어두운 팝업 — 위 텍스트는 밝게 유지)

        // 헤더(드래그 핸들) — 인벤 컨테이너 팝업과 동일
        var header = MakeRect("Header", bagPopup.transform);
        SetAnchors(header, new Vector2(0, 1), new Vector2(1, 1));
        var hRT = header.GetComponent<RectTransform>(); hRT.pivot = new Vector2(0.5f, 1); hRT.sizeDelta = new Vector2(0, BAG_HEADER_H);
        header.AddComponent<Image>().color = UITheme.Header;
        header.AddComponent<DraggableWindow>().target = rt;

        bagPopupTitle = AddText(hRT, "가방", 13, TextAnchor.MiddleLeft, UITheme.Gold);
        bagPopupTitle.fontStyle = FontStyle.Bold;
        var btRT = bagPopupTitle.GetComponent<RectTransform>(); btRT.offsetMin = new Vector2(10, 0); btRT.offsetMax = new Vector2(-92, 0);

        bagSortBtn = MakeBagHeaderBtn(hRT, "정렬", -36, UITheme.Accent, SortBagPopup, 50);
        var bagCloseGO = MakeBagHeaderBtn(hRT, "✕", -6, UITheme.Negative, CloseBagPopup, 24);
        bagCloseBtn = bagCloseGO != null ? bagCloseGO.GetComponent<Button>() : null;

        // 격자(헤더 아래 직접 배치 — 마스크/내부패널 없음). GridHost가 Refresh의 content 리사이즈를 흡수.
        var host = MakeRect("GridHost", bagPopup.transform);
        var hostRT = host.GetComponent<RectTransform>();
        hostRT.anchorMin = new Vector2(0, 1); hostRT.anchorMax = new Vector2(0, 1); hostRT.pivot = new Vector2(0, 1);
        hostRT.anchoredPosition = new Vector2(BAG_PAD, -(BAG_HEADER_H + BAG_PAD));
        var slot = MakeRect("SlotRoot", host.transform);
        var slotRT = slot.GetComponent<RectTransform>(); slotRT.anchorMin = new Vector2(0, 1); slotRT.anchorMax = new Vector2(0, 1); slotRT.pivot = new Vector2(0, 1);
        var item = MakeRect("ItemRoot", host.transform);
        var itemRT = item.GetComponent<RectTransform>(); itemRT.anchorMin = new Vector2(0, 1); itemRT.anchorMax = new Vector2(0, 1); itemRT.pivot = new Vector2(0, 1);

        bagPanel = new GridPanel { slotRoot = slotRT, itemRoot = itemRT, hitRoot = slotRT };
        bagPopup.SetActive(false);
    }

    /// <summary>가방 팝업 헤더 버튼(정렬/닫기) — 인벤 MakePopupHeaderButton과 동일.</summary>
    GameObject MakeBagHeaderBtn(RectTransform header, string label, float xFromRight, Color col,
                                UnityEngine.Events.UnityAction onClick, float width)
    {
        var btn = MakeRect($"Btn_{label}", header);
        btn.anchorMin = btn.anchorMax = btn.pivot = new Vector2(1, 1);
        btn.anchoredPosition = new Vector2(xFromRight, -4);
        btn.sizeDelta = new Vector2(width, 22);
        var btnImg = btn.gameObject.AddComponent<Image>();
        btnImg.color = col;
        UISkin.ButtonPrimary(btnImg);   // 시안: btn(밝은 종이) — 시맨틱 색은 tint로 유지, 글자 어둡게
        btn.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
        AddText(btn, label, 12, TextAnchor.MiddleCenter, C_INK);
        return btn.gameObject;
    }

    void OpenBagPopup(ItemInstance inst)
    {
        if (inst == null || !inst.IsContainer || bagPopup == null) return;
        bagPopupInst = inst;
        var g = inst.ContainerGrid;
        bagPanel.grid = g;

        // 제목: 이름 + 허용 카테고리(인벤과 동일)
        if (bagPopupTitle != null)
        {
            string title = inst.data.displayName;
            if (inst.data.allowedCategories != null && inst.data.allowedCategories.Length > 0)
                title += $"  <size=10><color=#88AACC>({inst.data.AllowedCategorySummary})</color></size>";
            bagPopupTitle.text = title;
        }
        // 가방(장비형) = 정렬 제외, 보관함(equipSlot None) = 정렬 노출
        if (bagSortBtn != null) bagSortBtn.SetActive(inst.data.equipSlot == EquipSlot.None);

        // 팝업 크기를 격자에 맞춤(인벤과 동일)
        if (g != null)
        {
            int cellTotal = GridPanel.CellTotal;
            float gw = g.width * cellTotal - GridPanel.GAP;
            float gh = g.height * cellTotal - GridPanel.GAP;
            var rt = bagPopup.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(gw + BAG_PAD * 2, gh + BAG_HEADER_H + BAG_PAD * 2);
        }

        bagPopup.SetActive(true);
        bagPopup.transform.SetAsLastSibling();
        bagPanel.Refresh();
    }

    void SortBagPopup()
    {
        if (bagPopupInst == null || bagPopupInst.ContainerGrid == null) return;
        bagPopupInst.ContainerGrid.AutoSort();
        bagPanel.Refresh();
    }

    void CloseBagPopup()
    {
        bagPopupInst = null;
        if (bagPanel != null) bagPanel.grid = null;
        if (bagPopup != null) bagPopup.SetActive(false);
    }

    // ── 우클릭 컨텍스트 메뉴 (열기/자세히/버리기) ──────────────
    void EnsureShopMenu()
    {
        if (shopMenu != null) return;
        shopMenu = MakeRect("ShopMenu", canvas.transform);
        SetAnchors(shopMenu, Vector2.zero, Vector2.one);
        shopMenu.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);   // 백드롭(밖 클릭 차단/닫기)
        shopMenu.AddComponent<Button>().onClick.AddListener(HideShopMenu);

        var panel = MakeRect("MenuPanel", shopMenu.transform);
        shopMenuPanel = panel.GetComponent<RectTransform>();
        shopMenuPanel.anchorMin = shopMenuPanel.anchorMax = new Vector2(0.5f, 0.5f); shopMenuPanel.pivot = new Vector2(0, 1);
        shopMenuPanel.sizeDelta = new Vector2(150, 100);
        var menuPanelImg = panel.AddComponent<Image>();
        menuPanelImg.color = UITheme.Panel;
        UISkin.Panel(menuPanelImg);   // 시안: box 프레임(어두운 메뉴 — 위 텍스트는 밝게 유지)

        // 아이템 이름 헤더(인벤 CtxName과 동일)
        var nameRT = MakeRect("CtxName", shopMenuPanel);
        nameRT.anchorMin = new Vector2(0, 1); nameRT.anchorMax = new Vector2(1, 1); nameRT.pivot = new Vector2(0, 1);
        nameRT.anchoredPosition = new Vector2(8, -4); nameRT.sizeDelta = new Vector2(-16, 22);
        shopMenuName = nameRT.gameObject.AddComponent<Text>();
        shopMenuName.font = UITheme.Font; shopMenuName.fontSize = 13; shopMenuName.fontStyle = FontStyle.Bold;
        shopMenuName.alignment = TextAnchor.MiddleLeft; shopMenuName.horizontalOverflow = HorizontalWrapMode.Overflow;
        nameRT.gameObject.AddComponent<Shadow>().effectColor = Color.black;

        shopMenu.SetActive(false);
    }

    void ShowShopMenu(InventoryGrid.PlacedItem placed, InventoryGrid grid)
    {
        if (placed?.item?.data == null || grid == null) return;
        EnsureShopMenu();
        if (dragMgr != null) dragMgr.suspended = true;

        var it = placed.item;
        shopMenuName.text = it.DisplayName;
        shopMenuName.color = it.data.RarityColor;

        // 기존 버튼 제거(이름 헤더 제외)
        for (int i = shopMenuPanel.childCount - 1; i >= 0; i--)
        {
            var ch = shopMenuPanel.GetChild(i);
            if (ch.name != "CtxName") Destroy(ch.gameObject);
        }

        float y = -28f;
        if (it.IsContainer && it.ContainerGrid != null)
        { AddMenuBtn(y, "열기", UITheme.AccentBright, () => { HideShopMenu(); OpenBagPopup(it); }); y -= 26; }
        AddMenuBtn(y, "자세히", UITheme.TextBright, () => { HideShopMenu(); ItemDetailUI.Show(it); }); y -= 26;
        AddMenuBtn(y, "버리기", UITheme.Negative, () => { HideShopMenu(); grid.Remove(placed); RefreshAll(); ToastManager.Show("버렸다", ToastManager.ToastType.Info); }); y -= 26;

        float totalH = Mathf.Abs(y) + 8f;
        shopMenuPanel.sizeDelta = new Vector2(150, totalH);

        var canvasRT = canvas.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, GameInput.mousePosition, null, out var lp))
            shopMenuPanel.anchoredPosition = lp;
        // 화면 밖 보정(인벤과 동일)
        Vector2 cs = canvasRT.rect.size; Vector2 pos = shopMenuPanel.anchoredPosition;
        if (pos.x + 150 > cs.x / 2f) pos.x -= 150;
        if (pos.y - totalH < -cs.y / 2f) pos.y += totalH;
        shopMenuPanel.anchoredPosition = pos;

        shopMenu.SetActive(true);
        shopMenu.transform.SetAsLastSibling();
    }

    void HideShopMenu()
    {
        if (shopMenu != null) shopMenu.SetActive(false);
        if (dragMgr != null) dragMgr.suspended = false;
    }

    void AddMenuBtn(float y, string label, Color textColor, UnityEngine.Events.UnityAction onClick)
    {
        var rt = MakeRect($"M_{label}", shopMenuPanel);   // RectTransform 반환 오버로드
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(4, y); rt.sizeDelta = new Vector2(-8, 24);
        var img = rt.gameObject.AddComponent<Image>(); img.color = UITheme.Cell;
        UISkin.Cell(img);   // 시안: storage_box 격자 셀(폴백: 색 유지)
        var btn = rt.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
        var cb = btn.colors; cb.highlightedColor = UITheme.CellHover; cb.pressedColor = UITheme.CellPressed; btn.colors = cb;
        btn.onClick.AddListener(onClick);
        var t = AddText(rt, label, 13, TextAnchor.MiddleLeft, textColor);
        var trt = t.GetComponent<RectTransform>(); trt.offsetMin = new Vector2(8, 0); trt.offsetMax = new Vector2(-4, 0);
    }

    // ── 판매 트레이 렌더 ──────────────────────────────────────
    /// <summary>트레이 박스(TrayArea) 크기에 맞춰 격자 칸수를 계산 — 영역을 꽉 채운다.</summary>
    Vector2Int ComputeTraySize()
    {
        int cols = 6, rows = 4;
        var host = sellTraySlotRoot != null ? sellTraySlotRoot.parent as RectTransform : null;   // TrayHost
        var box  = host != null ? host.parent as RectTransform : null;                            // TrayArea
        if (box != null && box.rect.width > 4f)
        {
            int ct = GridPanel.CellTotal;
            cols = Mathf.Clamp(Mathf.FloorToInt((box.rect.width  - 12f) / ct), 4, 14);
            rows = Mathf.Clamp(Mathf.FloorToInt((box.rect.height - 12f) / ct), 3, 12);
        }
        return new Vector2Int(cols, rows);
    }

    void EnsureSellTray()
    {
        if (sellTray != null) return;
        var s = ComputeTraySize();
        sellTray = new InventoryGrid(s.x, s.y);
    }

    void RefreshSellTray()
    {
        if (trayPanel == null) return;
        EnsureSellTray();
        // 비어 있으면 레이아웃 확정 크기로 맞춤(첫 프레임 rect=0 보정 + 영역 꽉 채우기)
        if (sellTray.GetAll().Count == 0)
        {
            var s = ComputeTraySize();
            if (s.x != sellTray.width || s.y != sellTray.height) sellTray = new InventoryGrid(s.x, s.y);
        }
        trayPanel.grid = sellTray;
        trayPanel.Refresh();
        if (sellTrayBtnLabel != null) sellTrayBtnLabel.text = $"판매  ◈{SellTrayTotal():N0}";
    }

    // ── 위탁 뷰 갱신 ──────────────────────────────────────────
    void RefreshConsign()
    {
        RefreshConsignSellList();
        RefreshConsignSlots();
    }

    void RefreshConsignSellList()
    {
        if (consignSellList == null) return;
        ClearChildren(consignSellList);

        int count = 0;
        if (stashGrid != null && shop != null)
        {
            foreach (var placed in stashGrid.GetAll())
            {
                if (placed?.item?.data == null) continue;
                int price = ConsignPrice(placed.item.data);
                if (price <= 0) continue;   // 판매 불가 아이템은 위탁 불가
                var captured = placed;
                AddConsignSellRow(consignSellList, placed, price, () => Consign(captured));
                count++;
            }
        }
        SetActive(consignSellEmpty, count == 0);
    }

    void AddConsignSellRow(RectTransform parent, InventoryGrid.PlacedItem placed, int price, System.Action onClick)
    {
        var data = placed.item.data;
        var row  = MakeRect("Row", parent.transform);
        var rowRT = row.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 40);
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 40;
        le.minHeight       = 40;

        var bg = row.AddComponent<Image>();
        bg.color = C_CELL;
        var btn = row.AddComponent<Button>();
        btn.targetGraphic = bg;
        var cb = btn.colors;
        cb.normalColor      = C_CELL;
        cb.highlightedColor = C_CELL_HOVER;
        cb.pressedColor     = new Color(0.28f, 0.24f, 0.14f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick?.Invoke());

        // 희귀도 좌 막대
        var rarBar = MakeRect("RarBar", row.transform);
        var rarRT  = rarBar.GetComponent<RectTransform>();
        rarRT.anchorMin = new Vector2(0, 0); rarRT.anchorMax = new Vector2(0, 1);
        rarRT.offsetMin = Vector2.zero;      rarRT.offsetMax = new Vector2(3, 0);
        rarBar.AddComponent<Image>().color = data.RarityColor;

        // 이름 (좌)
        var nameGO = MakeRect("Name", row.transform);
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0); nameRT.anchorMax = new Vector2(0.65f, 1);
        nameRT.offsetMin = new Vector2(10, 0); nameRT.offsetMax = Vector2.zero;
        string label = placed.item.stackCount > 1 ? $"{data.displayName} (x{placed.item.stackCount})" : data.displayName;
        AddText(nameGO, label, 13, TextAnchor.MiddleLeft, Color.white);

        // 위탁가 (우)
        var priceGO = MakeRect("Price", row.transform);
        var priceRT = priceGO.GetComponent<RectTransform>();
        priceRT.anchorMin = new Vector2(0.65f, 0); priceRT.anchorMax = new Vector2(1, 1);
        priceRT.offsetMin = Vector2.zero; priceRT.offsetMax = new Vector2(-10, 0);
        AddText(priceGO, $"위탁 ◈{price:N0}", 13, TextAnchor.MiddleRight, C_GOLD);
    }

    void RefreshConsignSlots()
    {
        for (int i = 0; i < CONSIGN_SLOTS; i++)
        {
            var slot  = consignSlots[i];
            var bg    = consignSlotBg[i];
            var title = consignSlotTitle[i];
            var stat  = consignSlotStatus[i];
            if (bg == null || title == null || stat == null) continue;

            if (slot.IsEmpty)
            {
                bg.color    = C_DIM;
                title.text  = "비어 있음";
                title.color = C_MUTED;
                stat.text   = "가방에서 물건을 선택해 올리세요.";
                stat.color  = C_MUTED;
            }
            else if (slot.IsReady)
            {
                bg.color    = C_READY;
                title.text  = slot.itemName;
                title.color = Color.white;
                stat.text   = $"정산 완료 — 클릭해 수령  (+◈{slot.payout:N0})";
                stat.color  = new Color(0.5f, 1f, 0.6f);
            }
            else
            {
                int remain = Mathf.CeilToInt(slot.readyTime - Time.unscaledTime);
                if (remain < 0) remain = 0;
                bg.color    = C_CONSIGN;
                title.text  = slot.itemName;
                title.color = Color.white;
                stat.text   = $"정산 중… {remain}초 남음  (◈{slot.payout:N0})";
                stat.color  = new Color(0.75f, 0.7f, 0.85f);
            }
        }
    }

    // ── 수배 남은 수량 = 런타임 전용(에셋 미수정·세이브 안 함, 위탁 슬롯과 동일 원칙). key=shopId:index ──
    static readonly Dictionary<string, int> wantedRemainRuntime = new Dictionary<string, int>();

    int WantedRemaining(int index)
    {
        if (shop == null || shop.wanted == null || index < 0 || index >= shop.wanted.Count) return 0;
        string key = shop.shopId + ":" + index;
        if (!wantedRemainRuntime.TryGetValue(key, out int r))
        {
            r = Mathf.Max(0, shop.wanted[index].remaining);   // SO의 remaining = 초기값(런타임에 복사)
            wantedRemainRuntime[key] = r;
        }
        return r;
    }

    void SpendWanted(int index)
    {
        if (shop == null) return;
        string key = shop.shopId + ":" + index;
        wantedRemainRuntime[key] = Mathf.Max(0, WantedRemaining(index) - 1);
    }

    // ── 수배 뷰 갱신 ──────────────────────────────────────────
    void RefreshWanted()
    {
        if (wantedList == null) return;
        ClearChildren(wantedList);

        int count = 0;
        if (shop != null && shop.wanted != null)
        {
            for (int i = 0; i < shop.wanted.Count; i++)
            {
                var w = shop.wanted[i];
                if (w == null || w.item == null) continue;
                int remain = WantedRemaining(i);
                if (remain <= 0) continue;
                int price = shop.WantedPrice(w);
                if (price <= 0) continue;
                int idx = i;   // 클로저 캡처
                AddWantedRow(wantedList, w, price, remain, () => SellWanted(idx));
                count++;
            }
        }
        SetActive(wantedEmpty, count == 0);
    }

    /// <summary>가방에서 지정 ItemData와 같은 첫 PlacedItem을 찾는다(없으면 null).</summary>
    InventoryGrid.PlacedItem FindInStash(ItemData data)
    {
        if (stashGrid == null || data == null) return null;
        foreach (var placed in stashGrid.GetAll())
        {
            if (placed?.item?.data == data) return placed;
        }
        return null;
    }

    /// <summary>수배 항목(인덱스)을 1개 매입: 가방에서 1개 제거 + 스크랩 지급 + 런타임 남은수량 감소.</summary>
    void SellWanted(int index)
    {
        if (shop == null || stashGrid == null || shop.wanted == null) return;
        if (index < 0 || index >= shop.wanted.Count) return;
        var w = shop.wanted[index];
        if (w == null || w.item == null) return;
        if (WantedRemaining(index) <= 0) return;

        var placed = FindInStash(w.item);
        if (placed == null)
        {
            ToastManager.Show("가방에 해당 물건이 없다", ToastManager.ToastType.Warning);
            return;
        }
        int price = shop.WantedPrice(w);
        if (price <= 0) return;

        // 1개 제거 (스택이면 수량 감소, 마지막이면 PlacedItem 제거)
        if (placed.item.stackCount > 1)
        {
            placed.item.stackCount -= 1;
            stashGrid.NotifyChanged();
        }
        else
        {
            stashGrid.Remove(placed);
        }

        string name = w.item.displayName;
        CurrencyManager.Instance?.Add(price, $"수배 매입: {name}");
        SaveCheckpoints.Instance?.InventoryChanged();
        SpendWanted(index);   // 런타임 남은수량 감소(에셋 미수정)
        // TODO 평판: 수배 매입 시 평판 보너스 (현재는 그레이박스 — 프리미엄가만)

        // 수배 뷰에선 tradeBody(결과 텍스트)가 비활성 → 토스트로 피드백.
        ToastManager.Show($"{name} 수배 매입 (+◈{price:N0})", ToastManager.ToastType.Success);
        UpdateBalance();
        RefreshWanted();
    }

    void AddWantedRow(RectTransform parent, ShopData.WantedItem w, int price, int remaining, System.Action onSell)
    {
        var data = w.item;
        var row  = MakeRect("Row", parent.transform);
        var rowRT = row.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 44);
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 44;
        le.minHeight       = 44;
        row.AddComponent<Image>().color = C_CELL;

        // 희귀도 좌 막대
        var rarBar = MakeRect("RarBar", row.transform);
        var rarRT  = rarBar.GetComponent<RectTransform>();
        rarRT.anchorMin = new Vector2(0, 0); rarRT.anchorMax = new Vector2(0, 1);
        rarRT.offsetMin = Vector2.zero;      rarRT.offsetMax = new Vector2(3, 0);
        rarBar.AddComponent<Image>().color = data.RarityColor;

        bool has = FindInStash(data) != null;

        // 이름 (좌)
        var nameGO = MakeRect("Name", row.transform);
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0); nameRT.anchorMax = new Vector2(0.42f, 1);
        nameRT.offsetMin = new Vector2(10, 0); nameRT.offsetMax = Vector2.zero;
        AddText(nameGO, data.displayName, 14, TextAnchor.MiddleLeft, has ? Color.white : C_MUTED);

        // 매입가 (중)
        var priceGO = MakeRect("Price", row.transform);
        var priceRT = priceGO.GetComponent<RectTransform>();
        priceRT.anchorMin = new Vector2(0.42f, 0); priceRT.anchorMax = new Vector2(0.66f, 1);
        priceRT.offsetMin = Vector2.zero; priceRT.offsetMax = Vector2.zero;
        AddText(priceGO, $"매입가 ◈{price:N0}", 13, TextAnchor.MiddleCenter, C_GOLD);

        // 남은 수량 (중우)
        var remGO = MakeRect("Remaining", row.transform);
        var remRT = remGO.GetComponent<RectTransform>();
        remRT.anchorMin = new Vector2(0.66f, 0); remRT.anchorMax = new Vector2(0.84f, 1);
        remRT.offsetMin = Vector2.zero; remRT.offsetMax = Vector2.zero;
        AddText(remGO, $"남은 {remaining}개", 13, TextAnchor.MiddleCenter, C_MUTED);

        // 팔기 버튼 (우)
        var btnGO = MakeRect("SellBtn", row.transform);
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.84f, 0.5f); btnRT.anchorMax = new Vector2(0.84f, 0.5f);
        btnRT.pivot     = new Vector2(0, 0.5f);
        btnRT.anchoredPosition = new Vector2(6, 0);
        btnRT.sizeDelta = new Vector2(64, 34);
        var btnBg = btnGO.AddComponent<Image>();
        btnBg.color = has ? C_SELL_BTN : C_DIM;
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        btn.interactable  = has;
        if (has) btn.onClick.AddListener(() => onSell?.Invoke());
        AddText(btnGO.GetComponent<RectTransform>(), "팔기", 14, TextAnchor.MiddleCenter,
                has ? Color.white : C_MUTED);
    }

    void AddInvCell(RectTransform parent, InventoryGrid.PlacedItem placed, bool selected, System.Action onClick)
    {
        var data   = placed.item.data;
        int cw     = placed.EffectiveWidth;
        int ch     = placed.EffectiveHeight;
        float pw   = cw * CELL_SIZE + (cw - 1) * CELL_GAP;
        float ph   = ch * CELL_SIZE + (ch - 1) * CELL_GAP;
        float px   = placed.gridX * (CELL_SIZE + CELL_GAP);
        float py   = -placed.gridY * (CELL_SIZE + CELL_GAP);

        var cell   = MakeRect("Item", parent.transform);
        var cellRT = cell.GetComponent<RectTransform>();
        cellRT.anchorMin        = new Vector2(0, 1);
        cellRT.anchorMax        = new Vector2(0, 1);
        cellRT.pivot            = new Vector2(0, 1);
        cellRT.anchoredPosition = new Vector2(px, py);
        cellRT.sizeDelta        = new Vector2(pw, ph);

        // 배경 = 희귀도 색 (좌측 막대 제거, 배경으로 희귀도 표시)
        var bg = cell.AddComponent<Image>();
        Color rarCol = data.RarityColor;
        bg.color = selected
            ? new Color(rarCol.r * 0.55f + 0.25f, rarCol.g * 0.55f + 0.30f, rarCol.b * 0.55f + 0.40f, 0.92f)
            : new Color(rarCol.r * 0.40f + 0.06f, rarCol.g * 0.40f + 0.06f, rarCol.b * 0.40f + 0.06f, 0.92f);

        var btn = cell.AddComponent<Button>();
        btn.targetGraphic = bg;
        var cb = btn.colors;
        cb.normalColor      = bg.color;
        cb.highlightedColor = new Color(bg.color.r + 0.08f, bg.color.g + 0.10f, bg.color.b + 0.18f, 0.95f);
        cb.pressedColor     = new Color(bg.color.r - 0.04f, bg.color.g - 0.04f, bg.color.b - 0.04f, 0.95f);
        cb.selectedColor    = bg.color;   // '선택 유지' 반짝 제거
        btn.colors = cb;
        btn.onClick.AddListener(() =>
        {
            onClick?.Invoke();
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        });

        // 아이콘
        if (data.icon != null)
        {
            var iconGO = MakeRect("Icon", cell.transform);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0, 0); iconRT.anchorMax = new Vector2(1, 1);
            iconRT.offsetMin = new Vector2(2, placed.item.HasDurability ? 6 : 2);
            iconRT.offsetMax = new Vector2(-2, -2);
            iconGO.AddComponent<Image>().sprite = data.icon;
            iconGO.GetComponent<Image>().preserveAspect = true;
        }
        else
        {
            var nameGO = MakeRect("Name", cell.transform);
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.15f); nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = new Vector2(3, 2);     nameRT.offsetMax  = new Vector2(-3, -2);
            var nt = AddText(nameGO, data.displayName, 9, TextAnchor.MiddleCenter, Color.white);
            nt.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        // 수량 (우상단)
        if (placed.item.stackCount > 1)
        {
            var cntGO = MakeRect("StackCnt", cell.transform);
            var cntRT = cntGO.GetComponent<RectTransform>();
            cntRT.anchorMin = new Vector2(0.4f, 0.65f); cntRT.anchorMax = new Vector2(1, 1);
            cntRT.offsetMin = Vector2.zero; cntRT.offsetMax = new Vector2(-2, -2);
            var ct = AddText(cntGO, $"x{placed.item.stackCount}", 9, TextAnchor.UpperRight, Color.white);
            ct.fontStyle = FontStyle.Bold;
        }

        // 내구도 바 (하단)
        if (placed.item.HasDurability)
        {
            var durBg   = MakeRect("DurBg", cell.transform);
            var durBgRT = durBg.GetComponent<RectTransform>();
            durBgRT.anchorMin = new Vector2(0, 0); durBgRT.anchorMax = new Vector2(1, 0);
            durBgRT.offsetMin = new Vector2(2, 2); durBgRT.offsetMax  = new Vector2(-2, 7);
            durBg.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);

            var durFill   = MakeRect("DurFill", durBg.transform);
            var durFillRT = durFill.GetComponent<RectTransform>();
            float ratio = Mathf.Clamp01(placed.item.DurabilityRatio);
            durFillRT.anchorMin = new Vector2(0, 0); durFillRT.anchorMax = new Vector2(ratio, 1);
            durFillRT.offsetMin = Vector2.zero;       durFillRT.offsetMax  = Vector2.zero;
            Color durCol = Color.Lerp(new Color(0.9f, 0.2f, 0.1f), new Color(0.2f, 0.85f, 0.3f), ratio);
            durFill.AddComponent<Image>().color = durCol;
        }
    }

    // ── UI 빌드 ────────────────────────────────────────────
#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — GenerateUI를 1회 실행해 프리팹화할 계층을 만든다.</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        GenerateUI();
    }
#endif

    void GenerateUI()
    {
        // Canvas
        var canvasGO = new GameObject("Shop_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // 전체화면 배경
        panel = MakeRect("Panel", canvasGO.transform);
        SetAnchors(panel, Vector2.zero, Vector2.one);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = C_BG;
        UISkin.StoragePanel(panelImg);   // 시안: storage.png 어두운 프레임(폴백: 색 유지) — 위 텍스트는 밝게

        // ── 최상단 바 (높이 72) ──────────────────────────────
        BuildTopBar(panel.transform);

        // ── 본문 3컬럼 (거래 뷰) ────────────────────────────
        var body = MakeRect("Body", panel.transform);
        SetAnchors(body, Vector2.zero, Vector2.one);
        var bodyRT = body.GetComponent<RectTransform>();
        bodyRT.offsetMin = new Vector2(0,    0);
        bodyRT.offsetMax = new Vector2(0, -72);
        tradeBody = body;

        BuildLeftColumn(body.transform);
        BuildCenterColumn(body.transform);
        BuildRightColumn(body.transform);

        SetupDragGrids();   // 창고/트레이를 드래그 격자로

        // ── 위탁 뷰 (거래 뷰와 같은 영역, 토글로 전환) ──────
        var cbody = MakeRect("ConsignBody", panel.transform);
        SetAnchors(cbody, Vector2.zero, Vector2.one);
        var cbodyRT = cbody.GetComponent<RectTransform>();
        cbodyRT.offsetMin = new Vector2(0,    0);
        cbodyRT.offsetMax = new Vector2(0, -72);
        consignBody = cbody;
        BuildConsignView(cbody.transform);
        cbody.SetActive(false);

        // ── 수배 뷰 (거래 뷰와 같은 영역, 탭으로 전환) ──────
        var wbody = MakeRect("WantedBody", panel.transform);
        SetAnchors(wbody, Vector2.zero, Vector2.one);
        var wbodyRT = wbody.GetComponent<RectTransform>();
        wbodyRT.offsetMin = new Vector2(0,    0);
        wbodyRT.offsetMax = new Vector2(0, -72);
        wantedBody = wbody;
        BuildWantedView(wbody.transform);
        wbody.SetActive(false);

        // 코드생성 폴백 경로: 빌드가 직접 AddListener 했지만 단일 출처로 다시 부착(RemoveAllListeners로 중복 방지).
        WireEvents();

        panel.SetActive(false);
    }

    // ── 수배 뷰 빌드 ──────────────────────────────────────────
    void BuildWantedView(Transform parent)
    {
        var box = MakeRect("WantedBox", parent);
        SetAnchors(box, new Vector2(0, 0), new Vector2(1, 1));
        var boxRT = box.GetComponent<RectTransform>();
        boxRT.offsetMin = new Vector2(8, 8);
        boxRT.offsetMax = new Vector2(-8, -8);
        box.AddComponent<Image>().color = C_PANEL;

        var header = MakeRect("Header", box.transform);
        SetAnchors(header, new Vector2(0, 1), new Vector2(1, 1));
        var hRT = header.GetComponent<RectTransform>();
        hRT.pivot     = new Vector2(0.5f, 1);
        hRT.sizeDelta = new Vector2(0, 36);
        header.AddComponent<Image>().color = C_HEADER;
        var hTextGO = MakeRect("HeaderText", header.transform);
        var hTextRT = hTextGO.GetComponent<RectTransform>();
        hTextRT.anchorMin = Vector2.zero; hTextRT.anchorMax = Vector2.one;
        hTextRT.offsetMin = new Vector2(12, 0); hTextRT.offsetMax = Vector2.zero;
        var hText = hTextGO.AddComponent<Text>();
        ConfigureText(hText, "수배 — 지정 물품 고가 매입", 15, TextAnchor.MiddleLeft, Color.white);
        hText.fontStyle = FontStyle.Bold;

        BuildScrollArea(box.transform, out wantedList, 36);
        var vlg = wantedList.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding       = new RectOffset(8, 8, 8, 8);
        vlg.spacing       = 4;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        var csf = wantedList.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        wantedEmpty = MakeRect("EmptyHint", box.transform).AddComponent<Text>();
        ConfigureText(wantedEmpty, "현재 수배 중인 물품이 없습니다.", 13, TextAnchor.MiddleCenter, C_MUTED);
        SetAnchors(wantedEmpty.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var weRT = wantedEmpty.GetComponent<RectTransform>();
        weRT.sizeDelta = new Vector2(300, 60);
        weRT.anchoredPosition = Vector2.zero;
        wantedEmpty.horizontalOverflow = HorizontalWrapMode.Wrap;
        wantedEmpty.gameObject.SetActive(false);
    }

    // ── 위탁 뷰 빌드 ──────────────────────────────────────────
    void BuildConsignView(Transform parent)
    {
        // 좌: 가방 판매가능 아이템 목록
        var left = MakeRect("ConsignLeft", parent);
        SetAnchors(left, new Vector2(0, 0), new Vector2(0.5f, 1));
        var leftRT = left.GetComponent<RectTransform>();
        leftRT.offsetMin = new Vector2(8, 8);
        leftRT.offsetMax = new Vector2(-4, -8);
        left.AddComponent<Image>().color = C_PANEL;

        var lHeader = MakeRect("Header", left.transform);
        SetAnchors(lHeader, new Vector2(0, 1), new Vector2(1, 1));
        var lhRT = lHeader.GetComponent<RectTransform>();
        lhRT.pivot     = new Vector2(0.5f, 1);
        lhRT.sizeDelta = new Vector2(0, 36);
        lHeader.AddComponent<Image>().color = C_HEADER;
        var lhTextGO = MakeRect("HeaderText", lHeader.transform);
        var lhTextRT = lhTextGO.GetComponent<RectTransform>();
        lhTextRT.anchorMin = Vector2.zero; lhTextRT.anchorMax = Vector2.one;
        lhTextRT.offsetMin = new Vector2(12, 0); lhTextRT.offsetMax = Vector2.zero;
        var lhText = lhTextGO.AddComponent<Text>();
        ConfigureText(lhText, "위탁할 물건 (가방)", 15, TextAnchor.MiddleLeft, Color.white);
        lhText.fontStyle = FontStyle.Bold;

        BuildScrollArea(left.transform, out consignSellList, 36);
        var glg = consignSellList.gameObject.AddComponent<VerticalLayoutGroup>();
        glg.padding       = new RectOffset(8, 8, 8, 8);
        glg.spacing       = 4;
        glg.childAlignment = TextAnchor.UpperLeft;
        glg.childControlWidth  = true;
        glg.childControlHeight = false;
        glg.childForceExpandWidth  = true;
        glg.childForceExpandHeight = false;
        var csf = consignSellList.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        consignSellEmpty = MakeRect("EmptyHint", left.transform).AddComponent<Text>();
        ConfigureText(consignSellEmpty, "위탁할 수 있는 물건이 없습니다.", 13, TextAnchor.MiddleCenter, C_MUTED);
        SetAnchors(consignSellEmpty.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var ceRT = consignSellEmpty.GetComponent<RectTransform>();
        ceRT.sizeDelta = new Vector2(260, 60);
        ceRT.anchoredPosition = Vector2.zero;
        consignSellEmpty.horizontalOverflow = HorizontalWrapMode.Wrap;
        consignSellEmpty.gameObject.SetActive(false);

        // 우: 위탁 슬롯 3개
        var right = MakeRect("ConsignRight", parent);
        SetAnchors(right, new Vector2(0.5f, 0), new Vector2(1, 1));
        var rightRT = right.GetComponent<RectTransform>();
        rightRT.offsetMin = new Vector2(4, 8);
        rightRT.offsetMax = new Vector2(-8, -8);
        right.AddComponent<Image>().color = C_PANEL;

        var rHeader = MakeRect("Header", right.transform);
        SetAnchors(rHeader, new Vector2(0, 1), new Vector2(1, 1));
        var rhRT = rHeader.GetComponent<RectTransform>();
        rhRT.pivot     = new Vector2(0.5f, 1);
        rhRT.sizeDelta = new Vector2(0, 36);
        rHeader.AddComponent<Image>().color = C_HEADER;
        var rhTextGO = MakeRect("HeaderText", rHeader.transform);
        var rhTextRT = rhTextGO.GetComponent<RectTransform>();
        rhTextRT.anchorMin = Vector2.zero; rhTextRT.anchorMax = Vector2.one;
        rhTextRT.offsetMin = new Vector2(12, 0); rhTextRT.offsetMax = Vector2.zero;
        var rhText = rhTextGO.AddComponent<Text>();
        ConfigureText(rhText, "위탁 슬롯 (정산가 = 판매가 ×1.5)", 15, TextAnchor.MiddleLeft, Color.white);
        rhText.fontStyle = FontStyle.Bold;

        // 슬롯 3개 (세로 배치)
        float slotH = 96f, gap = 12f, topPad = 48f;
        for (int i = 0; i < CONSIGN_SLOTS; i++)
        {
            int idx = i;
            var slot = MakeRect($"Slot{i}", right.transform);
            var slotRT = slot.GetComponent<RectTransform>();
            slotRT.anchorMin        = new Vector2(0, 1);
            slotRT.anchorMax        = new Vector2(1, 1);
            slotRT.pivot            = new Vector2(0.5f, 1);
            slotRT.offsetMin        = new Vector2(12, 0);
            slotRT.offsetMax        = new Vector2(-12, 0);
            slotRT.anchoredPosition = new Vector2(0, -(topPad + i * (slotH + gap)));
            slotRT.sizeDelta        = new Vector2(-24, slotH);

            var bg = slot.AddComponent<Image>();
            bg.color = C_DIM;
            consignSlotBg[idx] = bg;

            var btn = slot.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => CollectConsign(idx));
            consignSlotBtn[idx] = btn;

            var titleGO = MakeRect("Title", slot.transform);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 0.5f); titleRT.anchorMax = new Vector2(1, 1);
            titleRT.offsetMin = new Vector2(12, 0);    titleRT.offsetMax = new Vector2(-12, -6);
            var tt = titleGO.AddComponent<Text>();
            ConfigureText(tt, "비어 있음", 16, TextAnchor.LowerLeft, Color.white);
            tt.fontStyle = FontStyle.Bold;
            consignSlotTitle[idx] = tt;

            var statusGO = MakeRect("Status", slot.transform);
            var statusRT = statusGO.GetComponent<RectTransform>();
            statusRT.anchorMin = new Vector2(0, 0); statusRT.anchorMax = new Vector2(1, 0.5f);
            statusRT.offsetMin = new Vector2(12, 6); statusRT.offsetMax = new Vector2(-12, 0);
            var st = statusGO.AddComponent<Text>();
            ConfigureText(st, "가방에서 물건을 선택해 올리세요.", 12, TextAnchor.UpperLeft, C_MUTED);
            consignSlotStatus[idx] = st;
        }
    }

    // ── 상단 바 ──────────────────────────────────────────────
    void BuildTopBar(Transform parent)
    {
        var bar = MakeRect("TopBar", parent);
        SetAnchors(bar, new Vector2(0, 1), new Vector2(1, 1));
        var barRT = bar.GetComponent<RectTransform>();
        barRT.pivot      = new Vector2(0.5f, 1);
        barRT.offsetMin  = Vector2.zero;
        barRT.offsetMax  = Vector2.zero;
        barRT.sizeDelta  = new Vector2(0, 72);
        bar.AddComponent<Image>().color = C_HEADER;

        // 좌: 초상화 (56x56)
        var portrait   = MakeRect("Portrait", bar.transform);
        var portraitRT = portrait.GetComponent<RectTransform>();
        portraitRT.anchorMin        = new Vector2(0, 0.5f);
        portraitRT.anchorMax        = new Vector2(0, 0.5f);
        portraitRT.pivot            = new Vector2(0, 0.5f);
        portraitRT.anchoredPosition = new Vector2(16, 0);
        portraitRT.sizeDelta        = new Vector2(56, 56);
        portrait.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.26f);
        AddText(portrait.GetComponent<RectTransform>(), "상\n인", 11, TextAnchor.MiddleCenter, C_MUTED);

        // 상인 이름
        var nameGO = MakeRect("TraderName", bar.transform);
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin        = new Vector2(0, 0.5f);
        nameRT.anchorMax        = new Vector2(0, 0.5f);
        nameRT.pivot            = new Vector2(0, 0.5f);
        nameRT.anchoredPosition = new Vector2(82, 10);
        nameRT.sizeDelta        = new Vector2(340, 28);
        traderNameText = nameGO.AddComponent<Text>();
        traderNameText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        traderNameText.fontSize  = 22;
        traderNameText.fontStyle = FontStyle.Bold;
        traderNameText.color     = Color.white;
        traderNameText.text      = "상인";
        traderNameText.alignment = TextAnchor.MiddleLeft;

        // 신뢰도
        var trustGO = MakeRect("Trust", bar.transform);
        var trustRT = trustGO.GetComponent<RectTransform>();
        trustRT.anchorMin        = new Vector2(0, 0.5f);
        trustRT.anchorMax        = new Vector2(0, 0.5f);
        trustRT.pivot            = new Vector2(0, 0.5f);
        trustRT.anchoredPosition = new Vector2(82, -13);
        trustRT.sizeDelta        = new Vector2(240, 20);
        trustText = trustGO.AddComponent<Text>();
        trustText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        trustText.fontSize  = 13;
        trustText.color     = new Color(0.55f, 0.85f, 0.6f);
        trustText.text      = "신뢰도 -";
        trustText.alignment = TextAnchor.MiddleLeft;

        // 잔액 (우측)
        var balGO = MakeRect("Balance", bar.transform);
        var balRT = balGO.GetComponent<RectTransform>();
        balRT.anchorMin        = new Vector2(1, 0.5f);
        balRT.anchorMax        = new Vector2(1, 0.5f);
        balRT.pivot            = new Vector2(1, 0.5f);
        balRT.anchoredPosition = new Vector2(-88, 0);
        balRT.sizeDelta        = new Vector2(220, 36);
        balanceText = balGO.AddComponent<Text>();
        balanceText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        balanceText.fontSize  = 22;
        balanceText.fontStyle = FontStyle.Bold;
        balanceText.color     = C_GOLD;
        balanceText.text      = "◈ 0";
        balanceText.alignment = TextAnchor.MiddleRight;

        // 닫기 버튼
        var closeGO = MakeRect("CloseBtn", bar.transform);
        var closeRT  = closeGO.GetComponent<RectTransform>();
        closeRT.anchorMin        = new Vector2(1, 0.5f);
        closeRT.anchorMax        = new Vector2(1, 0.5f);
        closeRT.pivot            = new Vector2(1, 0.5f);
        closeRT.anchoredPosition = new Vector2(-14, 0);
        closeRT.sizeDelta        = new Vector2(44, 44);
        var closeImg = closeGO.AddComponent<Image>();
        closeImg.color = C_CLOSE;
        UISkin.ButtonPrimary(closeImg);   // 시안: btn(밝은 종이) — 글자 어둡게
        var cb = closeGO.AddComponent<Button>();
        cb.targetGraphic = closeImg;
        cb.onClick.AddListener(Close);
        closeBtn = cb;
        var closeTxtGO = new GameObject("Text");
        closeTxtGO.transform.SetParent(closeGO.transform, false);
        var closeTxtRT = closeTxtGO.AddComponent<RectTransform>();
        closeTxtRT.anchorMin = Vector2.zero; closeTxtRT.anchorMax = Vector2.one;
        closeTxtRT.offsetMin = Vector2.zero; closeTxtRT.offsetMax = Vector2.zero;
        var closeTxt = closeTxtGO.AddComponent<Text>();
        ConfigureText(closeTxt, "✕", 22, TextAnchor.MiddleCenter, C_INK);
        closeTxt.fontStyle = FontStyle.Bold;

        // ── 탭: 거래 / 위탁 / 수배 (중앙) ────────────────────
        // 탭 바 컨테이너 — 위탁/수배가 둘 다 없으면 통째로 숨긴다.
        var tabBarGO = MakeRect("TabBar", bar.transform);
        SetAnchors(tabBarGO, Vector2.zero, Vector2.one);
        var tabBarRT = tabBarGO.GetComponent<RectTransform>();
        tabBarRT.offsetMin = Vector2.zero; tabBarRT.offsetMax = Vector2.zero;
        tabBar = tabBarGO;
        BuildTab(tabBarGO.transform, "거래", -150, out tabTradeBg,   out tabTradeLabel,   () => SetTab(ShopTab.Trade));
        BuildTab(tabBarGO.transform, "위탁",    0, out tabConsignBg, out tabConsignLabel, () => SetTab(ShopTab.Consign));
        BuildTab(tabBarGO.transform, "수배",  150, out tabWantedBg,  out tabWantedLabel,  () => SetTab(ShopTab.Wanted));
    }

    void BuildTab(Transform parent, string label, float centerOffsetX, out Image bg, out Text txt, System.Action onClick)
    {
        var tab   = MakeRect("Tab_" + label, parent);
        var tabRT = tab.GetComponent<RectTransform>();
        tabRT.anchorMin        = new Vector2(0.5f, 0.5f);
        tabRT.anchorMax        = new Vector2(0.5f, 0.5f);
        tabRT.pivot            = new Vector2(0.5f, 0.5f);
        tabRT.anchoredPosition = new Vector2(centerOffsetX, 0);
        tabRT.sizeDelta        = new Vector2(150, 40);
        bg = tab.AddComponent<Image>();
        bg.color = C_TAB_OFF;
        UISkin.TabOff(bg);   // 시안: storage_btn 스프라이트(런타임 C_TAB_ON/OFF 색 스왑이 tint로 활성/비활성 구분). 폴백: 색 유지
        var btn = tab.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(() => onClick?.Invoke());
        txt = AddText(tab.GetComponent<RectTransform>(), label, 16, TextAnchor.MiddleCenter, Color.white);
        txt.fontStyle = FontStyle.Bold;
    }

    // ── 좌 컬럼: 상인 재고 ───────────────────────────────────
    void BuildLeftColumn(Transform parent)
    {
        var col = MakeRect("ColLeft", parent);
        SetAnchors(col, new Vector2(0, 0), new Vector2(COL_L_RATIO, 1));
        var colRT = col.GetComponent<RectTransform>();
        colRT.offsetMin = new Vector2(8, 8);
        colRT.offsetMax = new Vector2(-4, -8);
        col.AddComponent<Image>().color = C_PANEL;

        // 헤더
        var header = MakeRect("Header", col.transform);
        SetAnchors(header, new Vector2(0, 1), new Vector2(1, 1));
        var hRT = header.GetComponent<RectTransform>();
        hRT.pivot    = new Vector2(0.5f, 1);
        hRT.sizeDelta = new Vector2(0, 36);
        header.AddComponent<Image>().color = UITheme.Panel;   // 인벤처럼 스트립 없이 카드에 녹임
        var hText = AddText(header.GetComponent<RectTransform>(), "상인 재고", 16, TextAnchor.MiddleCenter, UITheme.Gold);
        hText.fontStyle = FontStyle.Bold;

        // 스크롤
        BuildScrollArea(col.transform, out stockContent, 36);

        // 빈 목록 힌트
        stockEmptyText = MakeRect("EmptyHint", col.transform).AddComponent<Text>();
        ConfigureText(stockEmptyText, "판매 중인 물건이 없습니다.", 13, TextAnchor.MiddleCenter, C_MUTED);
        SetAnchors(stockEmptyText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var stRT = stockEmptyText.GetComponent<RectTransform>();
        stRT.sizeDelta = new Vector2(240, 60);
        stRT.anchoredPosition = Vector2.zero;
        stockEmptyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        stockEmptyText.gameObject.SetActive(false);

        // footprint 격자 — 창고(invSlotRoot/invItemRoot)와 동일 구조. 슬롯 루트 + 아이템 오버레이.
        var sSlotGO = MakeRect("StockSlotRoot", stockContent.transform);
        stockSlotRoot = sSlotGO.GetComponent<RectTransform>();
        stockSlotRoot.anchorMin = new Vector2(0, 1); stockSlotRoot.anchorMax = new Vector2(0, 1);
        stockSlotRoot.pivot = new Vector2(0, 1); stockSlotRoot.anchoredPosition = new Vector2(8, -8);

        var sItemGO = MakeRect("StockItemRoot", stockContent.transform);
        stockItemRoot = sItemGO.GetComponent<RectTransform>();
        stockItemRoot.anchorMin = new Vector2(0, 1); stockItemRoot.anchorMax = new Vector2(0, 1);
        stockItemRoot.pivot = new Vector2(0, 1); stockItemRoot.anchoredPosition = new Vector2(8, -8);

        // 격자 배경 = 패널색(인벤과 동일) — 격자선이 과하게 진하지 않게.
        var stockAreaImg = stockContent.parent != null ? stockContent.parent.GetComponent<Image>() : null;
        if (stockAreaImg != null) stockAreaImg.color = UITheme.Panel;
    }

    // ── 중 컬럼: 구매 박스(상) / 거래 결과(중) / 판매 박스(하) ──
    void BuildCenterColumn(Transform parent)
    {
        float leftEnd    = COL_L_RATIO;
        float rightStart = 1f - COL_R_RATIO;
        var col = MakeRect("ColCenter", parent);
        SetAnchors(col, new Vector2(leftEnd, 0), new Vector2(rightStart, 1));
        var colRT = col.GetComponent<RectTransform>();
        colRT.offsetMin = new Vector2(4, 8);
        colRT.offsetMax = new Vector2(-4, -8);
        // 컬럼 자체 배경은 없음 — 두 박스가 각각 카드(패널)로 보이게 한다.

        // 구매 박스 (상단 ~52%)
        BuildDealBox(col.transform, isBuy: true,  aMin: new Vector2(0, 0.52f), aMax: new Vector2(1, 1));
        // 판매 트레이 (하단 ~48%) — 여러 개 담아 한 번에 판매
        BuildSellTray(col.transform, new Vector2(0, 0), new Vector2(1, 0.48f));

        // 중간 거래 결과 스트립
        var mid = MakeRect("TradeStrip", col.transform);
        SetAnchors(mid, new Vector2(0, 0.48f), new Vector2(1, 0.52f));
        var midRT = mid.GetComponent<RectTransform>();
        midRT.offsetMin = new Vector2(2, 0); midRT.offsetMax = new Vector2(-2, 0);
        tradeResultText = mid.AddComponent<Text>();
        ConfigureText(tradeResultText, "", 13, TextAnchor.MiddleCenter, new Color(0.5f, 0.9f, 0.6f));
        tradeResultText.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    /// <summary>구매/판매 공용 거래 박스 — 헤더 + 아이콘 + 이름/정보 + 액션 버튼 + 빈 힌트.</summary>
    void BuildDealBox(Transform parent, bool isBuy, Vector2 aMin, Vector2 aMax)
    {
        var box   = MakeRect(isBuy ? "BuyBox" : "SellBox", parent);
        SetAnchors(box, aMin, aMax);
        var boxRT = box.GetComponent<RectTransform>();
        boxRT.offsetMin = new Vector2(2, isBuy ? 2 : 4);
        boxRT.offsetMax = new Vector2(-2, isBuy ? -4 : -2);
        box.AddComponent<Image>().color = C_PANEL;

        // 헤더 (구매=올리브 / 판매=갈색)
        var header = MakeRect("Header", box.transform);
        SetAnchors(header, new Vector2(0, 1), new Vector2(1, 1));
        var hRT = header.GetComponent<RectTransform>();
        hRT.pivot     = new Vector2(0.5f, 1);
        hRT.sizeDelta = new Vector2(0, 30);
        header.AddComponent<Image>().color = isBuy ? C_BUY_BTN : C_SELL_BTN;
        var hTextGO = MakeRect("HeaderText", header.transform);
        var hTextRT = hTextGO.GetComponent<RectTransform>();
        hTextRT.anchorMin = Vector2.zero; hTextRT.anchorMax = Vector2.one;
        hTextRT.offsetMin = new Vector2(12, 0); hTextRT.offsetMax = Vector2.zero;
        var hText = hTextGO.AddComponent<Text>();
        ConfigureText(hText, isBuy ? "구매  BUY" : "판매  SELL", 14, TextAnchor.MiddleLeft, Color.white);
        hText.fontStyle = FontStyle.Bold;

        // 아이콘 (좌)
        var iconHolder = MakeRect("Icon", box.transform);
        var iconRT = iconHolder.GetComponent<RectTransform>();
        iconRT.anchorMin        = new Vector2(0, 1);
        iconRT.anchorMax        = new Vector2(0, 1);
        iconRT.pivot            = new Vector2(0, 1);
        iconRT.anchoredPosition = new Vector2(12, -40);
        iconRT.sizeDelta        = new Vector2(72, 72);
        var iconBg = MakeRect("IconBg", iconHolder.transform);
        SetAnchors(iconBg.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        iconBg.AddComponent<Image>().color = C_DIM;
        var icon = iconHolder.AddComponent<Image>();
        icon.color = Color.white; icon.enabled = false; icon.preserveAspect = true;

        // 이름 (아이콘 우측 상단)
        var nameGO = MakeRect("Name", box.transform);
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin        = new Vector2(0, 1);
        nameRT.anchorMax        = new Vector2(1, 1);
        nameRT.pivot            = new Vector2(0, 1);
        nameRT.anchoredPosition = new Vector2(96, -42);
        nameRT.sizeDelta        = new Vector2(-108, 28);
        var nameTxt = nameGO.AddComponent<Text>();
        ConfigureText(nameTxt, "", 16, TextAnchor.UpperLeft, Color.white);
        nameTxt.fontStyle = FontStyle.Bold;
        nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

        // 정보 (아이콘 우측 하단)
        var infoGO = MakeRect("Info", box.transform);
        var infoRT = infoGO.GetComponent<RectTransform>();
        infoRT.anchorMin        = new Vector2(0, 1);
        infoRT.anchorMax        = new Vector2(1, 1);
        infoRT.pivot            = new Vector2(0, 1);
        infoRT.anchoredPosition = new Vector2(96, -74);
        infoRT.sizeDelta        = new Vector2(-108, 40);
        var infoTxt = infoGO.AddComponent<Text>();
        ConfigureText(infoTxt, "", 12, TextAnchor.UpperLeft, new Color(0.75f, 0.73f, 0.66f));
        infoTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

        // 액션 버튼 (하단)
        var btnGO = MakeRect("ActionBtn", box.transform);
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin        = new Vector2(0, 0);
        btnRT.anchorMax        = new Vector2(1, 0);
        btnRT.pivot            = new Vector2(0.5f, 0);
        btnRT.anchoredPosition = new Vector2(0, 12);
        btnRT.sizeDelta        = new Vector2(-24, 42);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = isBuy ? C_BUY_BTN : C_SELL_BTN;
        UISkin.ButtonPrimary(btnImg);   // 시안: btn(밝은 종이) — 시맨틱 색은 Button tint로 유지, 글자 어둡게
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var cb = btn.colors;
        cb.normalColor      = isBuy ? C_BUY_BTN : C_SELL_BTN;
        cb.highlightedColor = isBuy ? new Color(0.28f, 0.46f, 0.22f) : new Color(0.60f, 0.38f, 0.14f);
        cb.pressedColor     = isBuy ? new Color(0.12f, 0.26f, 0.10f) : new Color(0.30f, 0.18f, 0.06f);
        btn.colors = cb;
        var btnLabel = AddText(btnGO.GetComponent<RectTransform>(), isBuy ? "구매" : "판매", 16, TextAnchor.MiddleCenter, C_INK);
        btnLabel.fontStyle = FontStyle.Bold;

        // 빈 힌트 (선택 전 안내)
        var emptyGO = MakeRect("Empty", box.transform);
        var emptyRT = emptyGO.GetComponent<RectTransform>();
        emptyRT.anchorMin = new Vector2(0, 0); emptyRT.anchorMax = new Vector2(1, 1);
        emptyRT.offsetMin = new Vector2(12, 60); emptyRT.offsetMax = new Vector2(-12, -34);
        var emptyTxt = emptyGO.AddComponent<Text>();
        ConfigureText(emptyTxt,
            isBuy ? "왼쪽 상인 재고에서\n구매할 물건을 선택하세요."
                  : "오른쪽 창고에서\n판매할 물건을 선택하세요.",
            13, TextAnchor.MiddleCenter, C_MUTED);
        emptyTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

        // 필드 바인딩 + 클릭
        if (isBuy)
        {
            buyIcon = icon; buyName = nameTxt; buyInfo = infoTxt;
            actionBuyBtn = btn; actionBuyLabel = btnLabel; buyEmpty = emptyGO;

            // 수량 스테퍼 (− [n] +) — 액션 버튼 위
            buyQtyRow = MakeRect("BuyQtyRow", box.transform);
            var qrRT = buyQtyRow.GetComponent<RectTransform>();
            qrRT.anchorMin = new Vector2(0.5f, 0); qrRT.anchorMax = new Vector2(0.5f, 0); qrRT.pivot = new Vector2(0.5f, 0);
            qrRT.anchoredPosition = new Vector2(0, 58);
            qrRT.sizeDelta = new Vector2(180, 30);
            buyMinusBtn = MakeStepBtn(buyQtyRow.transform, "−", -66, () => { buyQty = Mathf.Max(1, buyQty - 1); RefreshBuyBox(); });
            buyPlusBtn  = MakeStepBtn(buyQtyRow.transform, "+",  66, () => { buyQty += 1; RefreshBuyBox(); });
            var qtGO = MakeRect("Qty", buyQtyRow.transform);
            var qtRT = qtGO.GetComponent<RectTransform>();
            qtRT.anchorMin = qtRT.anchorMax = qtRT.pivot = new Vector2(0.5f, 0.5f);
            qtRT.anchoredPosition = Vector2.zero; qtRT.sizeDelta = new Vector2(90, 28);
            buyQtyText = qtGO.AddComponent<Text>();
            ConfigureText(buyQtyText, "1", 16, TextAnchor.MiddleCenter, Color.white);
            buyQtyText.fontStyle = FontStyle.Bold;
            buyQtyRow.SetActive(false);

            btn.onClick.AddListener(() => { if (selectedStock != null) Buy(selectedStock, buyQty); });
        }
        else
        {
            sellIcon = icon; sellName = nameTxt; sellInfo = infoTxt;
            actionSellBtn = btn; actionSellLabel = btnLabel; sellEmpty = emptyGO;
            btn.onClick.AddListener(() => { if (selectedInv != null) Sell(selectedInv); });
        }
        btn.gameObject.SetActive(false);
    }

    /// <summary>판매 트레이 박스 — 헤더 + 트레이 격자 + 되돌리기/판매 버튼.</summary>
    void BuildSellTray(Transform parent, Vector2 aMin, Vector2 aMax)
    {
        var box = MakeRect("SellTrayBox", parent);
        SetAnchors(box, aMin, aMax);
        var boxRT = box.GetComponent<RectTransform>();
        boxRT.offsetMin = new Vector2(2, 4); boxRT.offsetMax = new Vector2(-2, -2);
        box.AddComponent<Image>().color = C_PANEL;

        // 헤더
        var header = MakeRect("Header", box.transform);
        SetAnchors(header, new Vector2(0, 1), new Vector2(1, 1));
        var hRT = header.GetComponent<RectTransform>(); hRT.pivot = new Vector2(0.5f, 1); hRT.sizeDelta = new Vector2(0, 30);
        header.AddComponent<Image>().color = C_SELL_BTN;
        var ht = AddText(header.GetComponent<RectTransform>(), "판매 트레이  (창고 아이템 클릭 = 담기)", 13, TextAnchor.MiddleLeft, Color.white);
        ht.fontStyle = FontStyle.Bold;
        var htRT = ht.GetComponent<RectTransform>(); htRT.offsetMin = new Vector2(12, 0);

        // 트레이 격자 영역
        var area = MakeRect("TrayArea", box.transform);
        SetAnchors(area, Vector2.zero, Vector2.one);
        var areaRT = area.GetComponent<RectTransform>();
        areaRT.offsetMin = new Vector2(8, 52); areaRT.offsetMax = new Vector2(-8, -34);
        area.AddComponent<Image>().color = UITheme.Panel;   // 인벤처럼 격자가 패널 위에 바로
        area.AddComponent<RectMask2D>();

        // GridHost: GridPanel.Refresh의 content 리사이즈를 흡수(stretch된 area 변형 방지)
        var trayHost = MakeRect("TrayHost", area.transform);
        var thRT = trayHost.GetComponent<RectTransform>();
        thRT.anchorMin = new Vector2(0, 1); thRT.anchorMax = new Vector2(0, 1); thRT.pivot = new Vector2(0, 1);
        thRT.anchoredPosition = new Vector2(6, -6);

        var sSlot = MakeRect("TraySlotRoot", trayHost.transform);
        sellTraySlotRoot = sSlot.GetComponent<RectTransform>();
        sellTraySlotRoot.anchorMin = new Vector2(0, 1); sellTraySlotRoot.anchorMax = new Vector2(0, 1); sellTraySlotRoot.pivot = new Vector2(0, 1);
        var sItem = MakeRect("TrayItemRoot", trayHost.transform);
        sellTrayItemRoot = sItem.GetComponent<RectTransform>();
        sellTrayItemRoot.anchorMin = new Vector2(0, 1); sellTrayItemRoot.anchorMax = new Vector2(0, 1); sellTrayItemRoot.pivot = new Vector2(0, 1);

        // 되돌리기 (좌하단)
        var ret = MakeRect("ReturnBtn", box.transform);
        var retRT = ret.GetComponent<RectTransform>();
        retRT.anchorMin = new Vector2(0, 0); retRT.anchorMax = new Vector2(0, 0); retRT.pivot = new Vector2(0, 0);
        retRT.anchoredPosition = new Vector2(10, 10); retRT.sizeDelta = new Vector2(96, 34);
        var retImg = ret.AddComponent<Image>(); retImg.color = C_CELL;
        UISkin.ButtonPrimary(retImg);   // 시안: btn(밝은 종이) — 글자 어둡게
        var retBtn = ret.AddComponent<Button>(); retBtn.targetGraphic = retImg;
        retBtn.onClick.AddListener(() => ReturnTrayAll(true));
        returnTrayBtn = retBtn;
        var rtxt = AddText(ret.GetComponent<RectTransform>(), "되돌리기", 13, TextAnchor.MiddleCenter, C_INK);
        rtxt.fontStyle = FontStyle.Bold;

        // 판매 (우하단, 나머지 폭)
        var sellGO = MakeRect("TraySellBtn", box.transform);
        var sgRT = sellGO.GetComponent<RectTransform>();
        sgRT.anchorMin = new Vector2(0, 0); sgRT.anchorMax = new Vector2(1, 0); sgRT.pivot = new Vector2(0.5f, 0);
        sgRT.offsetMin = new Vector2(114, 10); sgRT.offsetMax = new Vector2(-10, 44);
        var sellImg = sellGO.AddComponent<Image>(); sellImg.color = C_SELL_BTN;
        UISkin.ButtonPrimary(sellImg);   // 시안: btn(밝은 종이) — 시맨틱 색은 tint로 유지, 글자 어둡게
        sellTrayBtn = sellGO.AddComponent<Button>(); sellTrayBtn.targetGraphic = sellImg;
        sellTrayBtn.onClick.AddListener(SellTrayAll);
        sellTrayBtnLabel = AddText(sellGO.GetComponent<RectTransform>(), "판매  ◈0", 15, TextAnchor.MiddleCenter, C_INK);
        sellTrayBtnLabel.fontStyle = FontStyle.Bold;
    }

    Button MakeStepBtn(Transform parent, string label, float posX, System.Action onClick)
    {
        var go = MakeRect("Step_" + label, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(posX, 0); rt.sizeDelta = new Vector2(34, 28);
        var img = go.AddComponent<Image>(); img.color = C_CELL;
        UISkin.ButtonPrimary(img);   // 시안: btn(밝은 종이) — 글자 어둡게
        var b = go.AddComponent<Button>(); b.targetGraphic = img;
        b.onClick.AddListener(() => onClick?.Invoke());
        var t = AddText(go.GetComponent<RectTransform>(), label, 20, TextAnchor.MiddleCenter, C_INK);
        t.fontStyle = FontStyle.Bold;
        return b;
    }

    // ── 우 컬럼: 내 가방 ──────────────────────────────────────
    void BuildRightColumn(Transform parent)
    {
        var col = MakeRect("ColRight", parent);
        SetAnchors(col, new Vector2(1f - COL_R_RATIO, 0), new Vector2(1, 1));
        var colRT = col.GetComponent<RectTransform>();
        colRT.offsetMin = new Vector2(4,  8);
        colRT.offsetMax = new Vector2(-8, -8);
        col.AddComponent<Image>().color = C_PANEL;

        // 헤더
        var header = MakeRect("Header", col.transform);
        SetAnchors(header, new Vector2(0, 1), new Vector2(1, 1));
        var hRT = header.GetComponent<RectTransform>();
        hRT.pivot     = new Vector2(0.5f, 1);
        hRT.sizeDelta = new Vector2(0, 36);
        header.AddComponent<Image>().color = UITheme.Panel;   // 인벤처럼 스트립 없이 카드에 녹임
        var hTextGO = MakeRect("HeaderText", header.transform);
        var hTextRT2 = hTextGO.GetComponent<RectTransform>();
        hTextRT2.anchorMin = Vector2.zero; hTextRT2.anchorMax = Vector2.one;
        hTextRT2.offsetMin = new Vector2(12, 0); hTextRT2.offsetMax = new Vector2(-12, 0);
        var hText = hTextGO.AddComponent<Text>();
        ConfigureText(hText, "창고", 16, TextAnchor.MiddleCenter, UITheme.Gold);
        hText.fontStyle = FontStyle.Bold;
        stashHeaderText = hText;

        // '← 뒤로' (가방 내부 볼 때만 표시)
        stashBackBtn = MakeRect("BackBtn", header.transform);
        var bkRT = stashBackBtn.GetComponent<RectTransform>();
        bkRT.anchorMin = new Vector2(1, 0.5f); bkRT.anchorMax = new Vector2(1, 0.5f); bkRT.pivot = new Vector2(1, 0.5f);
        bkRT.anchoredPosition = new Vector2(-8, 0); bkRT.sizeDelta = new Vector2(72, 26);
        var bkImg = stashBackBtn.AddComponent<Image>();
        bkImg.color = C_CELL;
        UISkin.ButtonPrimary(bkImg);   // 시안: btn(밝은 종이) — 글자 어둡게
        stashBackBtn.AddComponent<Button>().onClick.AddListener(CloseBag);
        var bkTxt = AddText(stashBackBtn.GetComponent<RectTransform>(), "← 뒤로", 12, TextAnchor.MiddleCenter, C_INK);
        bkTxt.fontStyle = FontStyle.Bold;
        stashBackBtn.SetActive(false);

        // 스크롤 영역 — 내부에 invSlotRoot + invItemRoot
        var scrollArea = MakeRect("ScrollArea", col.transform);
        SetAnchors(scrollArea, Vector2.zero, Vector2.one);
        var scrollAreaRT = scrollArea.GetComponent<RectTransform>();
        scrollAreaRT.offsetMin = new Vector2(8,  8);
        scrollAreaRT.offsetMax = new Vector2(-8, -44);
        var sr = scrollArea.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Clamped;   // 바운스 없이 격자 안에서만 스크롤
        scrollArea.AddComponent<Image>().color = UITheme.Panel;   // 인벤처럼 격자가 패널 위에 바로
        var mask = scrollArea.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        var contentGO = MakeRect("InvContent", scrollArea.transform);
        var content   = contentGO.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(0, 1);
        content.pivot     = new Vector2(0, 1);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        sr.content = content;

        // 슬롯 그리드 루트
        var slotRootGO = MakeRect("SlotRoot", content);
        var slotRoot   = slotRootGO.GetComponent<RectTransform>();
        slotRoot.anchorMin        = new Vector2(0, 1);
        slotRoot.anchorMax        = new Vector2(0, 1);
        slotRoot.pivot            = new Vector2(0, 1);
        slotRoot.anchoredPosition = new Vector2(8, -8);
        invSlotRoot = slotRoot;

        // 아이템 오버레이 루트 (슬롯 루트와 동일 위치)
        var itemRootGO = MakeRect("ItemRoot", content);
        var itemRoot   = itemRootGO.GetComponent<RectTransform>();
        itemRoot.anchorMin        = new Vector2(0, 1);
        itemRoot.anchorMax        = new Vector2(0, 1);
        itemRoot.pivot            = new Vector2(0, 1);
        itemRoot.anchoredPosition = new Vector2(8, -8);
        invItemRoot = itemRoot;

        // content 크기를 그리드에 맞게 — 실제 크기는 RefreshInvGrid 에서 설정
    }

    /// <summary>창고 세로 스크롤바를 런타임 생성(없을 때만) — 일반 탭 창고처럼 항상 보이게. 프리팹/코드 두 경로 공용.</summary>
    void EnsureStashScrollbar(ScrollRect sr)
    {
        if (sr == null || sr.verticalScrollbar != null) return;
        var area = sr.GetComponent<RectTransform>();
        var parentRT = area != null ? area.parent as RectTransform : null;
        if (parentRT == null) return;

        // 스크롤 영역 우측을 비워 스크롤바 자리 확보
        area.offsetMax = new Vector2(-18f, area.offsetMax.y);

        var sbGO = new GameObject("StashScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGO.transform.SetParent(parentRT, false);
        var sbRT = sbGO.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0); sbRT.anchorMax = new Vector2(1, 1); sbRT.pivot = new Vector2(1, 0.5f);
        sbRT.offsetMin = new Vector2(-14f, area.offsetMin.y);
        sbRT.offsetMax = new Vector2(-4f,  area.offsetMax.y);
        sbGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.3f);   // 트랙

        var slide = new GameObject("SlidingArea", typeof(RectTransform));
        slide.transform.SetParent(sbGO.transform, false);
        var slRT = slide.GetComponent<RectTransform>();
        slRT.anchorMin = Vector2.zero; slRT.anchorMax = Vector2.one;
        slRT.offsetMin = Vector2.zero; slRT.offsetMax = Vector2.zero;

        var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.transform.SetParent(slide.transform, false);
        var hRT = handleGO.GetComponent<RectTransform>();
        hRT.anchorMin = Vector2.zero; hRT.anchorMax = Vector2.one;
        hRT.offsetMin = Vector2.zero; hRT.offsetMax = Vector2.zero;
        handleGO.GetComponent<Image>().color = UITheme.Divider;

        var sb = sbGO.GetComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;
        sb.handleRect = hRT;
        sb.targetGraphic = handleGO.GetComponent<Image>();

        sr.verticalScrollbar = sb;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;   // 항상 표시
    }

    // ── 스크롤 영역 헬퍼 ──────────────────────────────────────
    void BuildScrollArea(Transform parent, out RectTransform contentRT, float topOffset)
    {
        var area = MakeRect("ScrollArea", parent);
        SetAnchors(area, Vector2.zero, Vector2.one);
        var areaRT = area.GetComponent<RectTransform>();
        areaRT.offsetMin = new Vector2(0, 0);
        areaRT.offsetMax = new Vector2(0, -topOffset);
        var sr = area.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Clamped;   // 바운스 없이 격자 안에서만 스크롤
        area.AddComponent<Image>().color = new Color(0, 0, 0, 0.15f);
        var mask = area.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        var content = MakeRect("Content", area.transform);
        var cRT = content.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 1);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot     = new Vector2(0.5f, 1);
        cRT.offsetMin = Vector2.zero;
        cRT.offsetMax = Vector2.zero;
        sr.content    = cRT;
        contentRT     = cRT;
    }

    // ── 유틸 ─────────────────────────────────────────────────
    static string RarityLabel(ItemRarity r) => r switch
    {
        ItemRarity.Common     => "일반",
        ItemRarity.Uncommon   => "비범",
        ItemRarity.Rare       => "희귀",
        ItemRarity.Epic       => "영웅",
        ItemRarity.Legendary  => "전설",
        _                     => ""
    };

    static GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static RectTransform MakeRect(string name, RectTransform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    Text AddText(GameObject go, string text, int size, TextAnchor anchor, Color color)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var t = go.GetComponent<Text>() ?? go.AddComponent<Text>();
        ConfigureText(t, text, size, anchor, color);
        return t;
    }

    Text AddText(RectTransform parent, string text, int size, TextAnchor anchor, Color color)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<Text>();
        ConfigureText(t, text, size, anchor, color);
        return t;
    }

    void ConfigureText(Text t, string text, int size, TextAnchor anchor, Color color)
    {
        if (t == null) return;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = size;
        t.alignment = anchor;
        t.color     = color;
        t.text      = text;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
    }

    static void SetActive(Text t, bool active, string msg = null)
    {
        if (t == null) return;
        t.gameObject.SetActive(active);
        if (msg != null) t.text = msg;
    }

    static void ClearChildren(RectTransform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }
}
