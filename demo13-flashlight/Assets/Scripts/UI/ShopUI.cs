using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점(전당포) 거래 UI. 코드 생성(Canvas/uGUI). UIManager가 ShowShop으로 연다.
/// 좌: 팔기(플레이어 인벤, sellPrice>0) / 우: 사기(상점 stock). 상단: 스크랩 잔액.
/// </summary>
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }
    public bool IsShowing => panel != null && panel.activeSelf;
    public bool IsGenerated => canvas != null;

    Canvas canvas;
    GameObject panel;
    Text titleText, rudiText;
    RectTransform sellContent, buyContent;

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
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    // ── 갱신 ──────────────────────────────────────────────
    void Refresh()
    {
        if (titleText != null) titleText.text = shop != null ? shop.shopName : "상점";
        UpdateRudi();
        BuildSellList();
        BuildBuyList();
    }

    void UpdateRudi()
    {
        int bal = CurrencyManager.Instance != null ? CurrencyManager.Instance.Balance : 0;
        if (rudiText != null) rudiText.text = $"◈ {bal:N0}";
    }

    void BuildSellList()
    {
        ClearChildren(sellContent);
        if (inv == null || inv.Grid == null || shop == null) return;
        foreach (var p in inv.Grid.GetAll())
        {
            if (p.item?.data == null) continue;
            int unit = shop.SellPrice(p.item.data);
            if (unit <= 0) continue;
            int total = unit * p.item.stackCount;
            var captured = p;
            string cnt = p.item.stackCount > 1 ? $"x{p.item.stackCount}" : "";
            AddCell(sellContent, p.item.data, cnt, total, () => Sell(captured));
        }
    }

    void BuildBuyList()
    {
        ClearChildren(buyContent);
        if (shop == null || shop.stock == null) return;
        foreach (var it in shop.stock)
        {
            if (it == null) continue;
            int price = shop.BuyPrice(it);
            if (price <= 0) continue;
            var captured = it;
            AddCell(buyContent, it, "", price, () => Buy(captured));
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

        // 어두운 배경
        panel = CreateRect("Panel", canvasGO.transform, Vector2.zero, Vector2.one);
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.75f);

        // 중앙 창
        var win = CreateRect("Window", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var winRT = win.GetComponent<RectTransform>();
        winRT.sizeDelta = new Vector2(1000, 640);
        win.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.13f, 0.98f);

        // 상단 바: 타이틀 + 스크랩 + 닫기
        titleText = MakeText(win.transform, "전당포", 28, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -16), new Vector2(400, 40));
        titleText.fontStyle = FontStyle.Bold;

        rudiText = MakeText(win.transform, "◈ 0", 22, TextAnchor.MiddleRight,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-90, -18), new Vector2(220, 36));
        rudiText.color = new Color(1f, 0.85f, 0.3f);

        var closeBtn = MakeButton(win.transform, "✕", new Vector2(1, 1), new Vector2(-40, -18),
            new Vector2(48, 36), new Color(0.5f, 0.2f, 0.2f), Close);

        // 좌: 팔기 / 우: 사기
        MakeColumnLabel(win.transform, "팔기 (내 가방)", 0f);
        MakeColumnLabel(win.transform, "사기 (상점)", 0.5f);
        sellContent = MakeScrollColumn(win.transform, 0f);
        buyContent  = MakeScrollColumn(win.transform, 0.5f);

        panel.SetActive(false);
    }

    /// <summary>타르코프식 정사각 칸. 아이콘(없으면 이름) + 수량(우상단) + 가격(하단). 셀 전체 클릭 = 거래.</summary>
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

        // 아이콘 또는 이름(아이콘 없을 때)
        if (data != null && data.icon != null)
        {
            var icon = CellChild("Icon", cell.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(6, 18), new Vector2(-6, -6));
            var img = icon.AddComponent<Image>();
            img.sprite = data.icon; img.preserveAspect = true;
        }
        else
        {
            var nameGO = CellChild("Name", cell.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(3, 16), new Vector2(-3, -3));
            var nt = CellText(nameGO, data != null ? data.displayName : "?", 12, TextAnchor.MiddleCenter, new Color(0.9f, 0.9f, 0.95f));
            nt.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        // 수량(우상단)
        if (!string.IsNullOrEmpty(topRight))
        {
            var cntGO = CellChild("Cnt", cell.transform, new Vector2(0.35f, 0.72f), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-3, -2));
            CellText(cntGO, topRight, 12, TextAnchor.UpperRight, Color.white).fontStyle = FontStyle.Bold;
        }

        // 가격(하단)
        var priceGO = CellChild("Price", cell.transform, new Vector2(0, 0), new Vector2(1, 0.24f), new Vector2(2, 1), new Vector2(-2, 0));
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

    void MakeColumnLabel(Transform win, string text, float xFrac)
    {
        var t = MakeText(win, text, 18, TextAnchor.MiddleLeft,
            new Vector2(xFrac, 1), new Vector2(xFrac, 1), new Vector2(28 + xFrac * 480, -62), new Vector2(440, 30));
        t.fontStyle = FontStyle.Bold;
        t.color = new Color(0.8f, 0.85f, 1f);
    }

    RectTransform MakeScrollColumn(Transform win, float xFrac)
    {
        var area = CreateRect("Scroll", win, new Vector2(xFrac, 0), new Vector2(xFrac + 0.5f, 1));
        var areaRT = area.GetComponent<RectTransform>();
        areaRT.offsetMin = new Vector2(20, 24);
        areaRT.offsetMax = new Vector2(-20, -96);
        var sr = area.AddComponent<ScrollRect>();
        sr.horizontal = false;
        area.AddComponent<Image>().color = new Color(0, 0, 0, 0.25f);
        area.AddComponent<Mask>().showMaskGraphic = true;

        var content = new GameObject("Content");
        content.transform.SetParent(area.transform, false);
        var cRT = content.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0.5f, 1);
        cRT.offsetMin = Vector2.zero; cRT.offsetMax = Vector2.zero;
        var glg = content.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(80, 80);
        glg.spacing = new Vector2(6, 6);
        glg.padding = new RectOffset(6, 6, 6, 6);
        glg.childAlignment = TextAnchor.UpperLeft;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = cRT;
        return cRT;
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
        return t;
    }

    static Text MakeChildText(Transform parent, string text, int size, TextAnchor anchor)
    {
        var go = new GameObject("T");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<LayoutElement>();
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
