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
    const int   CELL_SIZE   = 48;
    const int   CELL_GAP    = 2;
    const float COL_L_RATIO = 0.35f;   // 좌 컬럼 너비 비율
    const float COL_R_RATIO = 0.35f;   // 우 컬럼 너비 비율

    static readonly Color C_BG          = new Color(0.02f, 0.02f, 0.04f, 0.98f);
    static readonly Color C_PANEL       = new Color(0.06f, 0.06f, 0.10f, 1.00f);
    static readonly Color C_CELL        = new Color(0.10f, 0.10f, 0.14f, 1.00f);
    static readonly Color C_CELL_SEL    = new Color(0.20f, 0.32f, 0.48f, 1.00f);
    static readonly Color C_CELL_HOVER  = new Color(0.16f, 0.22f, 0.32f, 1.00f);
    static readonly Color C_HEADER      = new Color(0.12f, 0.12f, 0.18f, 1.00f);
    static readonly Color C_GOLD        = new Color(1.00f, 0.85f, 0.30f, 1.00f);
    static readonly Color C_BUY_BTN     = new Color(0.12f, 0.42f, 0.18f, 1.00f);
    static readonly Color C_SELL_BTN    = new Color(0.52f, 0.26f, 0.06f, 1.00f);
    static readonly Color C_CLOSE       = new Color(0.42f, 0.12f, 0.12f, 1.00f);
    static readonly Color C_MUTED       = new Color(0.50f, 0.52f, 0.60f, 1.00f);
    static readonly Color C_DIM         = new Color(0.08f, 0.08f, 0.12f, 1.00f);

    // ── 런타임 상태 ─────────────────────────────────────────
    Canvas          canvas;
    GameObject      panel;

    // 상단 바
    Text            traderNameText;
    Text            trustText;
    Text            balanceText;

    // 좌: 상인 재고
    RectTransform   stockContent;
    Text            stockEmptyText;

    // 중: 프리뷰
    Image           previewIcon;
    Text            previewName;
    Text            previewRarity;
    Text            previewDesc;
    Text            previewDetails;
    Button          actionBuyBtn;
    Button          actionSellBtn;
    Text            actionBuyLabel;
    Text            actionSellLabel;
    Text            tradeResultText;

    // 우: 내 가방
    RectTransform   invSlotRoot;    // 슬롯 그리드 (InventoryGrid 크기)
    RectTransform   invItemRoot;    // 배치된 아이템 오버레이

    // 선택 상태
    ItemData        selectedStock;      // 상인 재고에서 선택한 아이템
    InventoryGrid.PlacedItem selectedInv;  // 인벤토리에서 선택한 아이템

    ShopData        shop;
    InventoryGrid   stashGrid;

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

    bool            consignMode;          // true면 본문이 위탁 뷰
    GameObject      tradeBody;            // 구매/판매 3컬럼 본문
    GameObject      consignBody;          // 위탁 본문
    Text            tabTradeLabel;
    Text            tabConsignLabel;
    Image           tabTradeBg;
    Image           tabConsignBg;
    RectTransform   consignSellList;      // 좌: 가방 판매가능 아이템 목록
    Text            consignSellEmpty;
    readonly Image[] consignSlotBg     = new Image[CONSIGN_SLOTS];
    readonly Text[]  consignSlotTitle  = new Text[CONSIGN_SLOTS];
    readonly Text[]  consignSlotStatus = new Text[CONSIGN_SLOTS];
    readonly Button[] consignSlotBtn   = new Button[CONSIGN_SLOTS];

    static readonly Color C_TAB_ON   = new Color(0.18f, 0.30f, 0.46f, 1f);
    static readonly Color C_TAB_OFF  = new Color(0.10f, 0.10f, 0.16f, 1f);
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
    }

    // ── 공개 메서드 ──────────────────────────────────────────
    public void Open(ShopData shopData)
    {
        if (shopData == null) return;
        if (!IsGenerated) GenerateUI();
        shop = shopData;
        var stash = MainStash.Ensure();
        stashGrid = stash != null ? stash.GetGrid() : null;
        panel.SetActive(true);
        ClearSelection();
        SetConsignMode(false);
        RefreshAll();
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
        if (panel != null) panel.SetActive(false);
        ClearSelection();
    }

    // ── 선택 ─────────────────────────────────────────────────
    void ClearSelection()
    {
        selectedStock = null;
        selectedInv   = null;
    }

    void SelectStock(ItemData item)
    {
        selectedStock = item;
        selectedInv   = null;
        RefreshPreview();
        RefreshStockCells();
        RefreshInvCells();
    }

    void SelectInv(InventoryGrid.PlacedItem placed)
    {
        selectedInv   = placed;
        selectedStock = null;
        RefreshPreview();
        RefreshStockCells();
        RefreshInvCells();
    }

    // ── 거래 ─────────────────────────────────────────────────
    void Buy(ItemData item)
    {
        if (item == null || shop == null) return;
        int price = shop.BuyPrice(item);
        if (price <= 0) return;
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.Spend(price, $"구매: {item.displayName}"))
        {
            ToastManager.Show("스크랩이 부족하다", ToastManager.ToastType.Warning);
            ShowTradeResult("잔액이 부족합니다.", new Color(1f, 0.4f, 0.3f));
            return;
        }
        if (stashGrid == null || !stashGrid.TryAutoPlace(new ItemInstance(item, 1)))
        {
            CurrencyManager.Instance.Add(price, "환불(공간 부족)");
            ToastManager.Show("창고 공간 부족", ToastManager.ToastType.Warning);
            ShowTradeResult("창고 공간 부족.", new Color(1f, 0.4f, 0.3f));
            return;
        }
        ShowTradeResult($"{item.displayName} 구매 완료. (-◈{price:N0})", new Color(0.4f, 1f, 0.5f));
        RefreshAll();
    }

    void Sell(InventoryGrid.PlacedItem placed)
    {
        if (placed?.item?.data == null || shop == null) return;
        int total = shop.SellPrice(placed.item.data) * placed.item.stackCount;
        if (total <= 0) return;
        string name = placed.item.DisplayName;
        CurrencyManager.Instance?.Add(total, $"판매: {name}");
        stashGrid.Remove(placed);
        selectedInv = null;
        ShowTradeResult($"{name} 판매 완료. (+◈{total:N0})", new Color(0.4f, 1f, 0.5f));
        RefreshAll();
    }

    void ShowTradeResult(string msg, Color col)
    {
        if (tradeResultText == null) return;
        tradeResultText.text  = msg;
        tradeResultText.color = col;
    }

    // ── 위탁 ─────────────────────────────────────────────────
    void SetConsignMode(bool on)
    {
        consignMode = on;
        if (tradeBody   != null) tradeBody.SetActive(!on);
        if (consignBody != null) consignBody.SetActive(on);
        if (tabTradeBg   != null) tabTradeBg.color   = on ? C_TAB_OFF : C_TAB_ON;
        if (tabConsignBg != null) tabConsignBg.color = on ? C_TAB_ON  : C_TAB_OFF;
        if (on) RefreshConsign();
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

        CurrencyManager.Instance?.Add(slot.payout, "위탁 정산");
        slot.item      = null;
        slot.itemName  = null;
        slot.payout    = 0;
        slot.readyTime = 0f;
        RefreshConsign();
    }

    int FindEmptyConsignSlot()
    {
        for (int i = 0; i < CONSIGN_SLOTS; i++)
            if (consignSlots[i].IsEmpty) return i;
        return -1;
    }

    // ── 전체 갱신 ────────────────────────────────────────────
    void RefreshAll()
    {
        RefreshTopBar();
        RefreshStockCells();
        RefreshPreview();
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

    // ── 좌: 상인 재고 그리드 ─────────────────────────────────
    void RefreshStockCells()
    {
        ClearChildren(stockContent);
        if (shop == null || shop.stock == null)
        {
            SetActive(stockEmptyText, true, "판매 중인 물건이 없습니다.");
            return;
        }

        var repTier = ReputationManager.Instance != null ? ReputationManager.Instance.Tier : ReputationTier.F;
        int count = 0;
        foreach (var item in shop.stock)
        {
            if (item == null) continue;
            int price = shop.BuyPrice(item);
            if (price <= 0) continue;

            var requiredTier = RarityToRepTier(item.rarity);
            if (repTier < requiredTier)
            {
                AddLockedStockCell(stockContent, item, requiredTier);
                count++;
                continue;
            }

            bool selected = (selectedStock == item);
            var captured  = item;
            AddStockCell(stockContent, item, price, selected, () => SelectStock(captured));
            count++;
        }
        SetActive(stockEmptyText, count == 0, "판매 중인 물건이 없습니다.");
    }

    static ReputationTier RarityToRepTier(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common   => ReputationTier.F,
        ItemRarity.Uncommon => ReputationTier.F,
        ItemRarity.Rare     => ReputationTier.D,
        ItemRarity.Epic     => ReputationTier.B,
        ItemRarity.Legendary => ReputationTier.A,
        _ => ReputationTier.F,
    };

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
        cb.pressedColor     = new Color(0.12f, 0.18f, 0.28f);
        cb.selectedColor    = C_CELL_SEL;
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick?.Invoke());

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

    // ── 중: 프리뷰 ────────────────────────────────────────────
    void RefreshPreview()
    {
        ItemData  data  = null;
        int       price = 0;
        bool      isBuy  = false;
        bool      isSell = false;

        if (selectedStock != null && shop != null)
        {
            data   = selectedStock;
            price  = shop.BuyPrice(data);
            isBuy  = true;
        }
        else if (selectedInv?.item?.data != null && shop != null)
        {
            data   = selectedInv.item.data;
            price  = shop.SellPrice(data) * (selectedInv.item.stackCount);
            isSell = true;
        }

        // 아이콘
        if (previewIcon != null)
        {
            previewIcon.sprite  = data?.icon;
            previewIcon.enabled = data?.icon != null;
            previewIcon.color   = Color.white;
        }

        // 이름 + 희귀도
        if (previewName   != null) previewName.text   = data != null ? data.displayName    : "아이템 선택";
        if (previewRarity != null)
        {
            previewRarity.text  = data != null ? RarityLabel(data.rarity)  : "";
            previewRarity.color = data != null ? data.RarityColor : C_MUTED;
        }
        if (previewDesc    != null) previewDesc.text    = data != null ? data.description    : "좌측에서 구매할 아이템을,\n우측에서 판매할 아이템을 선택하세요.";

        // 세부 정보
        if (previewDetails != null)
        {
            if (data != null)
            {
                int sellRef = shop != null ? shop.SellPrice(data) : 0;
                int buyRef  = shop != null ? shop.BuyPrice(data)  : 0;
                previewDetails.text =
                    $"분류: {data.category}\n" +
                    $"크기: {data.gridWidth}×{data.gridHeight}\n" +
                    $"무게: {data.weight:F2} kg\n" +
                    $"구매가: ◈{buyRef:N0}\n" +
                    $"판매가: ◈{sellRef:N0}";
                if (data.maxDurability > 0)
                    previewDetails.text += $"\n내구도: {data.maxDurability}";
                if (data.maxStack > 1 && selectedInv != null)
                    previewDetails.text += $"\n수량: {selectedInv.item.stackCount}/{data.maxStack}";
            }
            else
            {
                previewDetails.text = "";
            }
        }

        // 버튼 표시
        if (actionBuyBtn  != null) actionBuyBtn.gameObject.SetActive(isBuy);
        if (actionSellBtn != null) actionSellBtn.gameObject.SetActive(isSell);

        if (isBuy && actionBuyLabel != null)
            actionBuyLabel.text  = $"구매  ◈{price:N0}";
        if (isSell && actionSellLabel != null)
            actionSellLabel.text = $"판매  ◈{price:N0}";

        // 잔액 갱신
        UpdateBalance();
    }

    // ── 우: 인벤토리 그리드 ───────────────────────────────────
    void RefreshInvGrid()
    {
        if (invSlotRoot == null || invItemRoot == null) return;

        // 슬롯 재생성
        ClearChildren(invSlotRoot);
        ClearChildren(invItemRoot);

        if (stashGrid == null) return;

        int cols = stashGrid.width;
        int rows = stashGrid.height;

        // 슬롯 그리드
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var slot   = MakeRect("Slot", invSlotRoot.transform);
                var slotRT = slot.GetComponent<RectTransform>();
                float x = c * (CELL_SIZE + CELL_GAP);
                float y = -r * (CELL_SIZE + CELL_GAP);
                slotRT.anchorMin = new Vector2(0, 1);
                slotRT.anchorMax = new Vector2(0, 1);
                slotRT.pivot     = new Vector2(0, 1);
                slotRT.anchoredPosition = new Vector2(x, y);
                slotRT.sizeDelta        = new Vector2(CELL_SIZE, CELL_SIZE);
                slot.AddComponent<Image>().color = C_DIM;
            }
        }

        // 배치 아이템 오버레이
        foreach (var placed in stashGrid.GetAll())
        {
            if (placed?.item?.data == null) continue;
            bool sel = (selectedInv != null && selectedInv.item.uid == placed.item.uid);
            var captured = placed;
            AddInvCell(invItemRoot, placed, sel, () => SelectInv(captured));
        }
    }

    void RefreshInvCells()
    {
        // 아이템 오버레이만 다시 그림 (슬롯 재생성 없음)
        ClearChildren(invItemRoot);
        if (stashGrid == null) return;

        foreach (var placed in stashGrid.GetAll())
        {
            if (placed?.item?.data == null) continue;
            bool sel = (selectedInv != null && selectedInv.item.uid == placed.item.uid);
            var captured = placed;
            AddInvCell(invItemRoot, placed, sel, () => SelectInv(captured));
        }
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
        cb.pressedColor     = new Color(0.12f, 0.18f, 0.28f);
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

        // 배경 (희귀도)
        var bg = cell.AddComponent<Image>();
        Color rarCol = data.RarityColor;
        bg.color = selected
            ? new Color(rarCol.r * 0.6f + 0.2f, rarCol.g * 0.6f + 0.3f, rarCol.b * 0.6f + 0.5f, 0.85f)
            : new Color(rarCol.r * 0.25f, rarCol.g * 0.25f, rarCol.b * 0.25f, 0.85f);

        // 테두리 색 (좌 2px)
        var border   = MakeRect("Border", cell.transform);
        var borderRT = border.GetComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0, 0); borderRT.anchorMax = new Vector2(0, 1);
        borderRT.offsetMin = Vector2.zero;       borderRT.offsetMax  = new Vector2(2, 0);
        border.AddComponent<Image>().color = rarCol;

        var btn = cell.AddComponent<Button>();
        btn.targetGraphic = bg;
        var cb = btn.colors;
        cb.normalColor      = bg.color;
        cb.highlightedColor = new Color(bg.color.r + 0.08f, bg.color.g + 0.10f, bg.color.b + 0.18f, 0.95f);
        cb.pressedColor     = new Color(bg.color.r - 0.04f, bg.color.g - 0.04f, bg.color.b - 0.04f, 0.95f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick?.Invoke());

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
        panel.AddComponent<Image>().color = C_BG;

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

        // ── 위탁 뷰 (거래 뷰와 같은 영역, 토글로 전환) ──────
        var cbody = MakeRect("ConsignBody", panel.transform);
        SetAnchors(cbody, Vector2.zero, Vector2.one);
        var cbodyRT = cbody.GetComponent<RectTransform>();
        cbodyRT.offsetMin = new Vector2(0,    0);
        cbodyRT.offsetMax = new Vector2(0, -72);
        consignBody = cbody;
        BuildConsignView(cbody.transform);
        cbody.SetActive(false);

        panel.SetActive(false);
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
        var closeBtn = MakeRect("CloseBtn", bar.transform);
        var closeRT  = closeBtn.GetComponent<RectTransform>();
        closeRT.anchorMin        = new Vector2(1, 0.5f);
        closeRT.anchorMax        = new Vector2(1, 0.5f);
        closeRT.pivot            = new Vector2(1, 0.5f);
        closeRT.anchoredPosition = new Vector2(-14, 0);
        closeRT.sizeDelta        = new Vector2(44, 44);
        closeBtn.AddComponent<Image>().color = C_CLOSE;
        var cb = closeBtn.AddComponent<Button>();
        cb.targetGraphic = closeBtn.GetComponent<Image>();
        cb.onClick.AddListener(Close);
        var closeTxtGO = new GameObject("Text");
        closeTxtGO.transform.SetParent(closeBtn.transform, false);
        var closeTxtRT = closeTxtGO.AddComponent<RectTransform>();
        closeTxtRT.anchorMin = Vector2.zero; closeTxtRT.anchorMax = Vector2.one;
        closeTxtRT.offsetMin = Vector2.zero; closeTxtRT.offsetMax = Vector2.zero;
        var closeTxt = closeTxtGO.AddComponent<Text>();
        ConfigureText(closeTxt, "✕", 22, TextAnchor.MiddleCenter, Color.white);
        closeTxt.fontStyle = FontStyle.Bold;

        // ── 탭: 거래 / 위탁 (중앙) ──────────────────────────
        BuildTab(bar.transform, "거래",  -84, out tabTradeBg,   out tabTradeLabel,   () => SetConsignMode(false));
        BuildTab(bar.transform, "위탁",   84, out tabConsignBg, out tabConsignLabel, () => SetConsignMode(true));
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
        header.AddComponent<Image>().color = C_HEADER;
        var hText = AddText(header.GetComponent<RectTransform>(), "상인 재고", 15, TextAnchor.MiddleLeft, Color.white);
        hText.fontStyle = FontStyle.Bold;
        if (hText != null)
        {
            var hTextRT = hText.GetComponent<RectTransform>();
            if (hTextRT != null) { hTextRT.offsetMin = new Vector2(12, 0); }
        }

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

        // GLG 설정
        var glg = stockContent.gameObject.AddComponent<GridLayoutGroup>();
        glg.cellSize      = new Vector2(CELL_SIZE * 1.6f, CELL_SIZE * 1.6f);
        glg.spacing       = new Vector2(5, 5);
        glg.padding       = new RectOffset(8, 8, 8, 8);
        glg.childAlignment = TextAnchor.UpperLeft;
        var csf = stockContent.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
    }

    // ── 중 컬럼: 프리뷰 ──────────────────────────────────────
    void BuildCenterColumn(Transform parent)
    {
        float leftEnd = COL_L_RATIO;
        float rightStart = 1f - COL_R_RATIO;
        var col = MakeRect("ColCenter", parent);
        SetAnchors(col, new Vector2(leftEnd, 0), new Vector2(rightStart, 1));
        var colRT = col.GetComponent<RectTransform>();
        colRT.offsetMin = new Vector2(4, 8);
        colRT.offsetMax = new Vector2(-4, -8);
        col.AddComponent<Image>().color = C_PANEL;

        float innerPad = 14f;

        // 아이콘 (정사각, 상단)
        var iconHolder   = MakeRect("IconHolder", col.transform);
        var iconHolderRT = iconHolder.GetComponent<RectTransform>();
        iconHolderRT.anchorMin        = new Vector2(0.5f, 1);
        iconHolderRT.anchorMax        = new Vector2(0.5f, 1);
        iconHolderRT.pivot            = new Vector2(0.5f, 1);
        iconHolderRT.anchoredPosition = new Vector2(0, -innerPad);
        iconHolderRT.sizeDelta        = new Vector2(100, 100);
        // 어두운 배경 이미지 — 자식 GO로 분리
        var iconBg   = MakeRect("IconBg", iconHolder.transform);
        SetAnchors(iconBg.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        iconBg.AddComponent<Image>().color = C_DIM;

        // 실제 아이템 아이콘 이미지
        previewIcon         = iconHolder.AddComponent<Image>();
        previewIcon.color   = Color.white;
        previewIcon.enabled = false;
        previewIcon.preserveAspect = true;

        // 이름
        var nameGO = MakeRect("PreviewName", col.transform);
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin        = new Vector2(0, 1);
        nameRT.anchorMax        = new Vector2(1, 1);
        nameRT.pivot            = new Vector2(0.5f, 1);
        nameRT.anchoredPosition = new Vector2(0, -(innerPad + 106));
        nameRT.sizeDelta        = new Vector2(-innerPad * 2, 30);
        previewName = nameGO.AddComponent<Text>();
        ConfigureText(previewName, "아이템 선택", 18, TextAnchor.MiddleCenter, Color.white);
        previewName.fontStyle = FontStyle.Bold;
        previewName.horizontalOverflow = HorizontalWrapMode.Wrap;

        // 희귀도
        var rarGO = MakeRect("PreviewRarity", col.transform);
        var rarRT = rarGO.GetComponent<RectTransform>();
        rarRT.anchorMin        = new Vector2(0, 1);
        rarRT.anchorMax        = new Vector2(1, 1);
        rarRT.pivot            = new Vector2(0.5f, 1);
        rarRT.anchoredPosition = new Vector2(0, -(innerPad + 140));
        rarRT.sizeDelta        = new Vector2(-innerPad * 2, 20);
        previewRarity = rarGO.AddComponent<Text>();
        ConfigureText(previewRarity, "", 12, TextAnchor.MiddleCenter, C_MUTED);

        // 구분선
        var div1 = MakeRect("Div1", col.transform);
        var d1RT = div1.GetComponent<RectTransform>();
        d1RT.anchorMin        = new Vector2(0, 1);
        d1RT.anchorMax        = new Vector2(1, 1);
        d1RT.pivot            = new Vector2(0.5f, 1);
        d1RT.anchoredPosition = new Vector2(0, -(innerPad + 164));
        d1RT.sizeDelta        = new Vector2(-innerPad * 2, 1);
        div1.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.28f);

        // 설명
        var descGO = MakeRect("PreviewDesc", col.transform);
        var descRT = descGO.GetComponent<RectTransform>();
        descRT.anchorMin        = new Vector2(0, 1);
        descRT.anchorMax        = new Vector2(1, 1);
        descRT.pivot            = new Vector2(0.5f, 1);
        descRT.anchoredPosition = new Vector2(0, -(innerPad + 170));
        descRT.sizeDelta        = new Vector2(-innerPad * 2, 72);
        previewDesc = descGO.AddComponent<Text>();
        ConfigureText(previewDesc, "좌측에서 구매할 아이템을,\n우측에서 판매할 아이템을 선택하세요.", 12, TextAnchor.UpperCenter, C_MUTED);
        previewDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
        previewDesc.verticalOverflow   = VerticalWrapMode.Overflow;

        // 세부 정보
        var detailGO = MakeRect("PreviewDetails", col.transform);
        var detailRT = detailGO.GetComponent<RectTransform>();
        detailRT.anchorMin        = new Vector2(0, 1);
        detailRT.anchorMax        = new Vector2(1, 1);
        detailRT.pivot            = new Vector2(0.5f, 1);
        detailRT.anchoredPosition = new Vector2(0, -(innerPad + 248));
        detailRT.sizeDelta        = new Vector2(-innerPad * 2, 110);
        previewDetails = detailGO.AddComponent<Text>();
        ConfigureText(previewDetails, "", 12, TextAnchor.UpperLeft, new Color(0.75f, 0.77f, 0.83f));
        previewDetails.horizontalOverflow = HorizontalWrapMode.Wrap;
        previewDetails.verticalOverflow   = VerticalWrapMode.Overflow;

        // ── 하단 액션 버튼 영역 ──
        // 구매 버튼
        var buyGO = MakeRect("BuyBtn", col.transform);
        var buyRT = buyGO.GetComponent<RectTransform>();
        buyRT.anchorMin        = new Vector2(0, 0);
        buyRT.anchorMax        = new Vector2(1, 0);
        buyRT.pivot            = new Vector2(0.5f, 0);
        buyRT.anchoredPosition = new Vector2(0, 72);
        buyRT.sizeDelta        = new Vector2(-innerPad * 2, 44);
        buyGO.AddComponent<Image>().color = C_BUY_BTN;
        actionBuyBtn = buyGO.AddComponent<Button>();
        actionBuyBtn.targetGraphic = buyGO.GetComponent<Image>();
        var buyCB = actionBuyBtn.colors;
        buyCB.normalColor      = C_BUY_BTN;
        buyCB.highlightedColor = new Color(0.20f, 0.62f, 0.28f);
        buyCB.pressedColor     = new Color(0.08f, 0.30f, 0.12f);
        actionBuyBtn.colors = buyCB;
        actionBuyBtn.onClick.AddListener(() => {
            if (selectedStock != null) Buy(selectedStock);
        });
        actionBuyLabel = AddText(buyGO.GetComponent<RectTransform>(), "구매", 17, TextAnchor.MiddleCenter, Color.white);
        actionBuyLabel.fontStyle = FontStyle.Bold;
        actionBuyBtn.gameObject.SetActive(false);

        // 판매 버튼
        var sellGO = MakeRect("SellBtn", col.transform);
        var sellRT = sellGO.GetComponent<RectTransform>();
        sellRT.anchorMin        = new Vector2(0, 0);
        sellRT.anchorMax        = new Vector2(1, 0);
        sellRT.pivot            = new Vector2(0.5f, 0);
        sellRT.anchoredPosition = new Vector2(0, 72);
        sellRT.sizeDelta        = new Vector2(-innerPad * 2, 44);
        sellGO.AddComponent<Image>().color = C_SELL_BTN;
        actionSellBtn = sellGO.AddComponent<Button>();
        actionSellBtn.targetGraphic = sellGO.GetComponent<Image>();
        var sellCB = actionSellBtn.colors;
        sellCB.normalColor      = C_SELL_BTN;
        sellCB.highlightedColor = new Color(0.72f, 0.40f, 0.10f);
        sellCB.pressedColor     = new Color(0.36f, 0.18f, 0.04f);
        actionSellBtn.colors = sellCB;
        actionSellBtn.onClick.AddListener(() => {
            if (selectedInv != null) Sell(selectedInv);
        });
        actionSellLabel = AddText(sellGO.GetComponent<RectTransform>(), "판매", 17, TextAnchor.MiddleCenter, Color.white);
        actionSellLabel.fontStyle = FontStyle.Bold;
        actionSellBtn.gameObject.SetActive(false);

        // 거래 결과 텍스트
        var resultGO = MakeRect("TradeResult", col.transform);
        var resultRT = resultGO.GetComponent<RectTransform>();
        resultRT.anchorMin        = new Vector2(0, 0);
        resultRT.anchorMax        = new Vector2(1, 0);
        resultRT.pivot            = new Vector2(0.5f, 0);
        resultRT.anchoredPosition = new Vector2(0, 20);
        resultRT.sizeDelta        = new Vector2(-innerPad * 2, 44);
        tradeResultText = resultGO.AddComponent<Text>();
        ConfigureText(tradeResultText, "", 12, TextAnchor.MiddleCenter, new Color(0.4f, 1f, 0.5f));
        tradeResultText.horizontalOverflow = HorizontalWrapMode.Wrap;
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
        header.AddComponent<Image>().color = C_HEADER;
        // 헤더 텍스트 — 별도 GO로 만들어 offsetMin으로 좌 패딩 적용
        var hTextGO = MakeRect("HeaderText", header.transform);
        var hTextRT2 = hTextGO.GetComponent<RectTransform>();
        hTextRT2.anchorMin = Vector2.zero; hTextRT2.anchorMax = Vector2.one;
        hTextRT2.offsetMin = new Vector2(12, 0); hTextRT2.offsetMax = Vector2.zero;
        var hText = hTextGO.AddComponent<Text>();
        ConfigureText(hText, "창고", 15, TextAnchor.MiddleLeft, Color.white);
        hText.fontStyle = FontStyle.Bold;

        // 스크롤 영역 — 내부에 invSlotRoot + invItemRoot
        var scrollArea = MakeRect("ScrollArea", col.transform);
        SetAnchors(scrollArea, Vector2.zero, Vector2.one);
        var scrollAreaRT = scrollArea.GetComponent<RectTransform>();
        scrollAreaRT.offsetMin = new Vector2(8,  8);
        scrollAreaRT.offsetMax = new Vector2(-8, -44);
        var sr = scrollArea.AddComponent<ScrollRect>();
        sr.horizontal = false;
        scrollArea.AddComponent<Image>().color = new Color(0, 0, 0, 0.2f);
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
