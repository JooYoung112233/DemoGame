using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하이드아웃 업그레이드 UI. 모듈 목록 → 각 모듈 현재 Lv · 다음 비용(스크랩+재료) · 효과 + 업그레이드 버튼.
/// HideoutModuleManager로 업글. UIManager.ShowHideout으로 연다.
/// (타르코프식 "시설 아이콘 뷰 → 우측 패널"은 다음 단계 — safehouse.md §하이드아웃 UI. 지금은 목록형.)
/// </summary>
public class HideoutUI : MonoBehaviour
{
    public static HideoutUI Instance { get; private set; }
    public bool IsShowing => panel != null && panel.activeSelf;
    public bool IsGenerated => canvas != null;

    Canvas canvas;
    GameObject panel;
    RectTransform listContent;
    Text rudiText;
    Font font;

    void Awake() { if (Instance == null) Instance = this; }

    /// <summary>없으면 생성 후 연다(온디맨드). UIManager/관리인/시설에서 호출.</summary>
    public static HideoutUI Show()
    {
        if (Instance == null)
        {
            var go = new GameObject("HideoutUI");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<HideoutUI>();
        }
        Instance.Open();
        return Instance;
    }

    public void Open()
    {
        if (!IsGenerated) GenerateUI();
        panel.SetActive(true);
        if (HideoutModuleManager.Instance != null)
        {
            HideoutModuleManager.Instance.OnModuleUpgraded -= OnUpgraded;
            HideoutModuleManager.Instance.OnModuleUpgraded += OnUpgraded;
        }
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        if (HideoutModuleManager.Instance != null)
            HideoutModuleManager.Instance.OnModuleUpgraded -= OnUpgraded;
    }

    void OnUpgraded(string m, int lv) => Refresh();

    void Refresh()
    {
        int bal = CurrencyManager.Instance != null ? CurrencyManager.Instance.Balance : 0;
        if (rudiText != null) rudiText.text = $"◈ {bal:N0}";
        BuildList();
    }

    void BuildList()
    {
        for (int i = listContent.childCount - 1; i >= 0; i--)
            Destroy(listContent.GetChild(i).gameObject);

        var mgr = HideoutModuleManager.Instance;
        if (mgr == null) return;
        foreach (var m in HideoutModuleManager.Modules)
            AddRow(m, mgr);
    }

    void AddRow(string m, HideoutModuleManager mgr)
    {
        int lv = mgr.GetLevel(m);
        var row = new GameObject($"Row_{m}", typeof(RectTransform));
        row.transform.SetParent(listContent, false);
        row.AddComponent<LayoutElement>().minHeight = 60;
        row.AddComponent<Image>().color = new Color(1, 1, 1, 0.05f);
        var hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.padding = new RectOffset(12, 12, 6, 6); hl.spacing = 12;
        hl.childControlWidth = true; hl.childForceExpandWidth = false; hl.childAlignment = TextAnchor.MiddleLeft;

        var nameT = Txt(row.transform, $"{HideoutModuleManager.DisplayName(m)}   Lv {lv}/{HideoutModuleManager.MaxLevel}", 16, TextAnchor.MiddleLeft);
        nameT.GetComponent<LayoutElement>().minWidth = 200;
        nameT.fontStyle = FontStyle.Bold;

        var cost = mgr.NextCost(m);
        string desc;
        if (cost == null) desc = "<color=#88FF99>최대 레벨</color>";
        else
        {
            string mats = "";
            foreach (var (id, qty) in cost.Value.mats) mats += $"  {id} x{qty}";
            desc = $"다음: ◈{cost.Value.scrap:N0} +{mats}";
        }
        var descT = Txt(row.transform, desc, 13, TextAnchor.MiddleLeft);
        descT.supportRichText = true;
        descT.GetComponent<LayoutElement>().flexibleWidth = 1;
        descT.color = new Color(0.82f, 0.84f, 0.9f);

        var btnGO = new GameObject("Up", typeof(RectTransform));
        btnGO.transform.SetParent(row.transform, false);
        btnGO.AddComponent<LayoutElement>().minWidth = 104;
        bool can = mgr.CanUpgrade(m, out _);
        var img = btnGO.AddComponent<Image>();
        img.color = cost == null ? new Color(0.18f, 0.18f, 0.2f)
                  : can ? new Color(0.28f, 0.6f, 0.36f) : new Color(0.42f, 0.32f, 0.32f);
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable = cost != null;
        string mm = m;
        btn.onClick.AddListener(() => { if (HideoutModuleManager.Instance != null) HideoutModuleManager.Instance.Upgrade(mm); });
        Txt(btnGO.transform, cost == null ? "MAX" : (lv == 0 ? "건설" : "업그레이드"), 13, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
    }

    // ── UI 빌드 ──
    void GenerateUI()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGO = new GameObject("Hideout_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        panel = NewRect("Panel", canvasGO.transform, Vector2.zero, Vector2.one);
        panel.AddComponent<Image>().color = new Color(0.02f, 0.02f, 0.04f, 0.9f);

        // 중앙 창
        var win = NewRect("Window", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var winRT = win.GetComponent<RectTransform>();
        winRT.sizeDelta = new Vector2(820, 720);
        win.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.11f, 0.99f);

        var title = MakeText(win.transform, "하이드아웃 — 시설 관리", 26, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -18), new Vector2(500, 40));
        title.fontStyle = FontStyle.Bold;

        rudiText = MakeText(win.transform, "◈ 0", 20, TextAnchor.MiddleRight,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-96, -20), new Vector2(220, 34));
        rudiText.color = new Color(1f, 0.85f, 0.3f);

        var closeGO = new GameObject("Close", typeof(RectTransform));
        closeGO.transform.SetParent(win.transform, false);
        var cRT = closeGO.GetComponent<RectTransform>();
        cRT.anchorMin = cRT.anchorMax = cRT.pivot = new Vector2(1, 1);
        cRT.anchoredPosition = new Vector2(-44, -20); cRT.sizeDelta = new Vector2(44, 34);
        closeGO.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f);
        closeGO.AddComponent<Button>().onClick.AddListener(Close);
        MakeChild(closeGO.transform, "✕", 18, TextAnchor.MiddleCenter);

        // 스크롤 목록
        var area = NewRect("Scroll", win.transform, new Vector2(0, 0), new Vector2(1, 1));
        var areaRT = area.GetComponent<RectTransform>();
        areaRT.offsetMin = new Vector2(20, 20); areaRT.offsetMax = new Vector2(-20, -68);
        var sr = area.AddComponent<ScrollRect>(); sr.horizontal = false;
        area.AddComponent<Image>().color = new Color(0, 0, 0, 0.25f);
        area.AddComponent<Mask>().showMaskGraphic = true;

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(area.transform, false);
        listContent = content.GetComponent<RectTransform>();
        listContent.anchorMin = new Vector2(0, 1); listContent.anchorMax = new Vector2(1, 1);
        listContent.pivot = new Vector2(0.5f, 1); listContent.offsetMin = Vector2.zero; listContent.offsetMax = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6; vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = listContent;

        panel.SetActive(false);
    }

    // ── 헬퍼 ──
    static GameObject NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }

    Text MakeText(Transform parent, string text, int size, TextAnchor anchor,
        Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sizeD)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = aMax; rt.anchoredPosition = pos; rt.sizeDelta = sizeD;
        var t = go.AddComponent<Text>();
        t.font = font; t.fontSize = size; t.alignment = anchor; t.color = Color.white; t.text = text;
        return t;
    }

    Text Txt(Transform parent, string text, int size, TextAnchor anchor)
    {
        var go = new GameObject("T", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>();
        var t = go.AddComponent<Text>();
        t.font = font; t.fontSize = size; t.alignment = anchor; t.color = Color.white; t.text = text;
        return t;
    }

    Text MakeChild(Transform parent, string text, int size, TextAnchor anchor)
    {
        var go = new GameObject("T", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<Text>();
        t.font = font; t.fontSize = size; t.alignment = anchor; t.color = Color.white; t.text = text;
        return t;
    }
}
