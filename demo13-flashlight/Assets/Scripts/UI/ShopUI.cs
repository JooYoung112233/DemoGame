using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점(전당포) 거래 UI. 코드 생성(Canvas/uGUI). UIManager가 ShowShop으로 연다.
/// 참조 레이아웃(docs 04 상점): 상단 = 상인 정보(이름 + 신뢰도) + 잔액 + 닫기 /
/// 탭 = 구매 / 판매 / 위탁 / 본문 = 칸(그리드). 한 화면에 한 탭만 표시.
/// </summary>
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }
    public bool IsShowing => panel != null && panel.activeSelf;
    public bool IsGenerated => canvas != null;

    Canvas canvas;
    GameObject panel;
    Text titleText, rudiText, traderNameText, trustText;
    RectTransform gridContent;
    Text emptyHint;
    Button[] tabButtons;
    Image[] tabBgs;
    Text[] tabTexts;
    int currentTab;   // 0 구매, 1 판매, 2 위탁

    static readonly string[] TABS = { "구매", "판매", "위탁" };

    ShopData shop;
    PlayerInventory inv;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void Open(ShopData shopData, PlayerInventory inventory)
    {
        if (shopData == null) return;
        if (!IsGenerated) GenerateUI();
        shop = shopData;
        inv = inventory;
        panel.SetActive(true);
        SelectTab(0);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    // ── 탭 ────────────────────────────────────────────────
    void SelectTab(int i)
    {
        currentTab = i;
        if (tabBgs != null)
            for (int t = 0; t < tabBgs.Length; t++)
                if (tabBgs[t] != null)
                    tabBgs[t].color = (t == i) ? new Color(0.30f, 0.42f, 0.62f) : new Color(0.14f, 0.14f, 0.2f);
        Refresh();
    }

    // ── 갱신 ──────────────────────────────────────────────
    void Refresh()
    {
        if (titleText != null) titleText.text = shop != null ? shop.shopName : "상점";
        if (traderNameText != null) traderNameText.text = shop != null ? shop.shopName : "상인";
        UpdateRudi();
        UpdateTrust();
        BuildGrid();
    }

    void UpdateRudi()
    {
        int bal = CurrencyManager.Instance != null ? CurrencyManager.Instance.Balance : 0;
        if (rudiText != null) rudiText.text = $"◈ {bal:N0}";
    }

    void UpdateTrust()
    {
        if (trustText == null) return;
        if (ReputationManager.Instance != null)
            trustText.text = $"신뢰도 {ReputationManager.Instance.Reputation}";
        else
            trustText.text = "신뢰도 -";
    }

    void BuildGrid()
    {
        ClearChildren(gridContent);
        int count = 0;

        if (currentTab == 0)            // 구매 (상점 재고)
        {
            if (shop != null && shop.stock != null)
                foreach (var it in shop.stock)
                {
                    if (it == null) continue;
                    int price = shop.BuyPrice(it);
                    if (price <= 0) continue;
                    var captured = it;
                    AddCell(gridContent, it, "", price, () => Buy(captured));
                    count++;
                }
        }
        else if (currentTab == 1)       // 판매 (내 가방)
        {
            if (inv != null && inv.Grid != null && shop != null)
                foreach (var p in inv.Grid.GetAll())
                {
                    if (p.item?.data == null) continue;
                    int unit = shop.SellPrice(p.item.data);
                    if (unit <= 0) continue;
                    int total = unit * p.item.stackCount;
                    var captured = p;
                    string cnt = p.item.stackCount > 1 ? $"x{p.item.stackCount}" : "";
                    AddCell(gridContent, p.item.data, cnt, total, () => Sell(captured));
                    count++;
                }
        }
        // currentTab == 2 (위탁): 준비 중 — 칸 없음

        if (emptyHint != null)
        {
            emptyHint.gameObject.SetActive(count == 0);
            emptyHint.text =
                currentTab == 2 ? "위탁 거래는 준비 중입니다."
              : currentTab == 1 ? "팔 수 있는 물건이 없습니다.\n(가방에 판매 가능한 아이템이 없음)"
              :                   "판매 중인 물건이 없습니다.";
        }
    }

    // ── 거래 ──────────────────────────────────────────────
    void Sell(InventoryGrid.PlacedItem p)
    {
        if (p?.item?.data == null || shop == null) return;
        int total = shop.SellPrice(p.item.data) * p.item.stackCount;
        if (total <= 0) return;
        CurrencyManager.Instance?.Add(total, $"판매: {p.item.DisplayName}");
        inv.Grid.Remove(p);
        Refresh();
    }

    void Buy(ItemData it)
    {
        if (it == null || shop == null) return;
        int price = shop.BuyPrice(it);
        if (price <= 0) return;
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.Spend(price, $"구매: {it.displayName}"))
        {
            ToastManager.Show("스크랩이 부족하다", ToastManager.ToastType.Warning);
            return;
        }
        if (!inv.TryPickup(new ItemInstance(it, 1)))
        {
            CurrencyManager.Instance.Add(price, "환불(공간 부족)");
            ToastManager.Show("인벤토리 공간 부족", ToastManager.ToastType.Warning);
        }
        Refresh();
    }

    // ── UI 빌드 ────────────────────────────────────────────
    void GenerateUI()
    {
        var canvasGO = new GameObject("Shop_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // 불투명 배경 (전체화면 모달)
        panel = CreateRect("Panel", canvasGO.transform, Vector2.zero, Vector2.one);
        panel.AddComponent<Image>().color = new Color(0.02f, 0.02f, 0.04f, 0.97f);

        // 중앙 창
        var win = CreateRect("Window", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var winRT = win.GetComponent<RectTransform>();
        winRT.sizeDelta = new Vector2(1040, 700);
        win.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.13f, 1f);

        // ── 상단 바: 타이틀 + 잔액 + 닫기 ──
        titleText = MakeText(win.transform, "전당포", 26, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -14), new Vector2(420, 38));
        titleText.fontStyle = FontStyle.Bold;

        rudiText = MakeText(win.transform, "◈ 0", 22, TextAnchor.MiddleRight,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-92, -16), new Vector2(240, 34));
        rudiText.color = new Color(1f, 0.85f, 0.3f);

        MakeButton(win.transform, "✕", new Vector2(1, 1), new Vector2(-40, -16),
            new Vector2(44, 34), new Color(0.5f, 0.2f, 0.2f), Close);

        // ── 상인 정보 행: 초상화 + 이름 + 신뢰도 ──
        var portrait = CreateRect("Portrait", win.transform, new Vector2(0, 1), new Vector2(0, 1));
        var pRT = portrait.GetComponent<RectTransform>();
        pRT.pivot = new Vector2(0, 1);
        pRT.anchoredPosition = new Vector2(24, -62);
        pRT.sizeDelta = new Vector2(72, 72);
        portrait.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.26f);
        MakeChildText(portrait.transform, "상\n인", 14, TextAnchor.MiddleCenter).color = new Color(0.6f, 0.62f, 0.7f);

        traderNameText = MakeText(win.transform, "상인", 18, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(108, -64), new Vector2(420, 26));
        traderNameText.fontStyle = FontStyle.Bold;

        trustText = MakeText(win.transform, "신뢰도 -", 14, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(108, -92), new Vector2(420, 24));
        trustText.color = new Color(0.55f, 0.85f, 0.6f);

        // ── 탭: 구매 / 판매 / 위탁 ──
        tabButtons = new Button[TABS.Length];
        tabBgs = new Image[TABS.Length];
        tabTexts = new Text[TABS.Length];
        float tabW = 150f, tabH = 38f, tabY = -150f;
        for (int i = 0; i < TABS.Length; i++)
        {
            var tab = CreateRect($"Tab_{i}", win.transform, new Vector2(0, 1), new Vector2(0, 1));
            var trt = tab.GetComponent<RectTransform>();
            trt.pivot = new Vector2(0, 1);
            trt.anchoredPosition = new Vector2(24 + i * (tabW + 4), tabY);
            trt.sizeDelta = new Vector2(tabW, tabH);
            tabBgs[i] = tab.AddComponent<Image>();
            tabBgs[i].color = new Color(0.14f, 0.14f, 0.2f);
            tabButtons[i] = tab.AddComponent<Button>();
            tabButtons[i].targetGraphic = tabBgs[i];
            int idx = i;
            tabButtons[i].onClick.AddListener(() => SelectTab(idx));
            tabTexts[i] = MakeChildText(tab.transform, TABS[i], 16, TextAnchor.MiddleCenter);
            tabTexts[i].fontStyle = FontStyle.Bold;
        }

        // ── 본문: 칸(그리드) 스크롤 ──
        var area = CreateRect("Scroll", win.transform, new Vector2(0, 0), new Vector2(1, 1));
        var areaRT = area.GetComponent<RectTransform>();
        areaRT.offsetMin = new Vector2(24, 24);
        areaRT.offsetMax = new Vector2(-24, -196);
        var sr = area.AddComponent<ScrollRect>();
        sr.horizontal = false;
        area.AddComponent<Image>().color = new Color(0, 0, 0, 0.25f);
        area.AddComponent<Mask>().showMaskGraphic = true;

        var content = new GameObject("Content");
        content.transform.SetParent(area.transform, false);
        gridContent = content.AddComponent<RectTransform>();
        gridContent.anchorMin = new Vector2(0, 1);
        gridContent.anchorMax = new Vector2(1, 1);
        gridContent.pivot = new Vector2(0.5f, 1);
        gridContent.offsetMin = Vector2.zero;
        gridContent.offsetMax = Vector2.zero;
        var glg = content.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(92, 92);
        glg.spacing = new Vector2(8, 8);
        glg.padding = new RectOffset(10, 10, 10, 10);
        glg.childAlignment = TextAnchor.UpperLeft;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = gridContent;

        // 빈 목록 안내 (가운데)
        emptyHint = MakeText(area.transform, "", 16, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(600, 80));
        emptyHint.color = new Color(0.55f, 0.58f, 0.66f);
        emptyHint.gameObject.SetActive(false);

        panel.SetActive(false);
    }

    /// <summary>정사각 칸. 아이콘(없으면 이름) + 수량(우상단) + 가격(하단). 셀 전체 클릭 = 거래.</summary>
    void AddCell(RectTransform parent, ItemData data, string topRight, int price, System.Action onClick)
    {
        if (parent == null) return;
        var cell = new GameObject("Cell", typeof(RectTransform));
        cell.transform.SetParent(parent, false);
        var bg = cell.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.16f, 0.21f, 1f);
        var btn = cell.AddComponent<Button>();
        btn.targetGraphic = bg;
        var cb = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.36f, 0.48f);
        cb.pressedColor = new Color(0.22f, 0.27f, 0.38f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick?.Invoke());

        if (data != null && data.icon != null)
        {
            var icon = CellChild("Icon", cell.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(6, 20), new Vector2(-6, -6));
            var img = icon.AddComponent<Image>();
            img.sprite = data.icon; img.preserveAspect = true;
        }
        else
        {
            var nameGO = CellChild("Name", cell.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(3, 18), new Vector2(-3, -3));
            var nt = CellText(nameGO, data != null ? data.displayName : "?", 12, TextAnchor.MiddleCenter, new Color(0.9f, 0.9f, 0.95f));
            nt.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        if (!string.IsNullOrEmpty(topRight))
        {
            var cntGO = CellChild("Cnt", cell.transform, new Vector2(0.35f, 0.72f), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-3, -2));
            CellText(cntGO, topRight, 12, TextAnchor.UpperRight, Color.white).fontStyle = FontStyle.Bold;
        }

        var priceGO = CellChild("Price", cell.transform, new Vector2(0, 0), new Vector2(1, 0.22f), new Vector2(2, 1), new Vector2(-2, 0));
        CellText(priceGO, $"◈{price:N0}", 12, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f));
    }

    static GameObject CellChild(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = offMin; rt.offsetMax = offMax;
        return go;
    }

    static Text CellText(GameObject go, string text, int size, TextAnchor anchor, Color color)
    {
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.alignment = anchor; t.color = color; t.text = text;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    // ── UI 헬퍼 ────────────────────────────────────────────
    static GameObject CreateRect(string name, Transform parent, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }

    static Text MakeText(Transform parent, string text, int size, TextAnchor anchor,
        Vector2 aMin, Vector2 aMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = aMax;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.alignment = anchor; t.color = Color.white;
        t.text = text;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        return t;
    }

    static Text MakeChildText(Transform parent, string text, int size, TextAnchor anchor)
    {
        var go = new GameObject("T");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.alignment = anchor; t.color = Color.white;
        t.text = text;
        return t;
    }

    GameObject MakeButton(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color color, System.Action onClick)
    {
        var go = new GameObject("Button");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.AddComponent<Image>().color = color;
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());
        MakeChildText(go.transform, label, 18, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
        return go;
    }

    static void ClearChildren(RectTransform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }
}
