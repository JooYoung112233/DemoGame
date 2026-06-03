using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점(전당포) 거래 UI. 코드 생성(Canvas/uGUI). UIManager가 ShowShop으로 연다.
/// 좌: 팔기(플레이어 인벤, sellPrice>0) / 우: 사기(상점 stock). 상단: 루디 잔액.
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
            string label = p.item.stackCount > 1 ? $"{p.item.DisplayName} x{p.item.stackCount}" : p.item.DisplayName;
            AddRow(sellContent, label, total, "팔기", new Color(0.3f, 0.7f, 0.4f), () => Sell(captured));
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
            AddRow(buyContent, it.displayName, price, "사기", new Color(0.8f, 0.7f, 0.3f), () => Buy(captured));
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
            ToastManager.Show("루디가 부족하다", ToastManager.ToastType.Warning);
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

        // 상단 바: 타이틀 + 루디 + 닫기
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

    void AddRow(RectTransform parent, string name, int price, string btnLabel, Color btnColor, System.Action onClick)
    {
        if (parent == null) return;
        var row = new GameObject("Row");
        row.transform.SetParent(parent, false);
        var rt = row.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 40);
        var le = row.AddComponent<LayoutElement>();
        le.minHeight = 40;
        row.AddComponent<Image>().color = new Color(1, 1, 1, 0.04f);
        var hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.padding = new RectOffset(8, 8, 4, 4);
        hl.spacing = 6;
        hl.childControlWidth = true; hl.childForceExpandWidth = false;
        hl.childAlignment = TextAnchor.MiddleLeft;

        var nameT = MakeChildText(row.transform, name, 16, TextAnchor.MiddleLeft);
        nameT.GetComponent<LayoutElement>().flexibleWidth = 1;

        var priceT = MakeChildText(row.transform, $"◈ {price:N0}", 16, TextAnchor.MiddleRight);
        priceT.color = new Color(1f, 0.85f, 0.3f);
        priceT.GetComponent<LayoutElement>().minWidth = 90;

        var btnGO = new GameObject("Btn");
        btnGO.transform.SetParent(row.transform, false);
        btnGO.AddComponent<RectTransform>();
        var btnLE = btnGO.AddComponent<LayoutElement>(); btnLE.minWidth = 64;
        btnGO.AddComponent<Image>().color = btnColor;
        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());
        var bt = MakeChildText(btnGO.transform, btnLabel, 15, TextAnchor.MiddleCenter);
        bt.fontStyle = FontStyle.Bold;
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
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4; vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
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
