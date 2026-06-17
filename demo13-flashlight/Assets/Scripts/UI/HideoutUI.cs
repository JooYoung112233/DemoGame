using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하이드아웃 시설 **단일 패널**(타르코프식). 시설 타일을 클릭하면 그 시설 1개만 보여준다:
///   • 미건설(Lv0) → 건설 UI(필요 스크랩+재료)
///   • 건설됨(Lv1~) → 업그레이드 UI(필요 재화) + 그 시설의 기능 버튼(제작/창고/라디오/파견/휴식)
///   • 최대 레벨 → MAX + 기능 버튼만
/// HideoutModuleManager로 건설/업글. 기능 버튼은 각 UI(CraftingUI/RadioUI/…)로 연결.
/// 진입: InteractableObject(시설 타일) 클릭 → HideoutUI.Show(moduleKey).
/// </summary>
public class HideoutUI : MonoBehaviour
{
    public static HideoutUI Instance { get; private set; }
    public bool IsShowing => panel != null && panel.activeSelf;
    public bool IsGenerated => canvas != null;

    Canvas canvas;
    GameObject panel;
    RectTransform listContent;
    Text titleText;
    Text scrapText;
    Font font;

    string currentModule = "workbench";

    void Awake() { if (Instance == null) Instance = this; }

    /// <summary>해당 시설 패널을 연다(온디맨드 생성). 시설 타일 클릭 → 여기로.</summary>
    public static HideoutUI Show(string module = "workbench")
    {
        if (Instance == null)
        {
            var go = new GameObject("HideoutUI");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<HideoutUI>();
        }
        Instance.currentModule = string.IsNullOrEmpty(module) ? "workbench" : module;
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

    void OnUpgraded(string m, int lv) { if (m == currentModule) Refresh(); }

    void Refresh()
    {
        int bal = CurrencyManager.Instance != null ? CurrencyManager.Instance.Balance : 0;
        if (scrapText != null) scrapText.text = $"◈ {bal:N0}";
        if (titleText != null) titleText.text = HideoutModuleManager.DisplayName(currentModule);
        BuildFacility();
    }

    // ═══════════════════════════════════════════
    //  시설 1개 패널 본문
    // ═══════════════════════════════════════════
    void BuildFacility()
    {
        for (int i = listContent.childCount - 1; i >= 0; i--)
            Destroy(listContent.GetChild(i).gameObject);

        var mgr = HideoutModuleManager.Instance;
        if (mgr == null) { Row("HideoutModuleManager 없음", 16, new Color(1, 0.6f, 0.6f)); return; }

        string m = currentModule;
        int lv = mgr.GetLevel(m);

        // 현재 레벨
        var lvRow = Row($"현재: Lv {lv} / {HideoutModuleManager.MaxLevel}", 18, Color.white);
        lvRow.fontStyle = FontStyle.Bold;

        // ── 건설/업그레이드 ──
        var cost = mgr.NextCost(m);
        if (cost == null)
        {
            Row("최대 레벨", 16, new Color(0.55f, 1f, 0.6f));
        }
        else
        {
            Row(lv == 0 ? "── 건설 비용 ──" : $"── Lv{lv + 1} 업그레이드 비용 ──", 15, new Color(0.8f, 0.82f, 0.9f));

            // 스크랩
            int bal = CurrencyManager.Instance != null ? CurrencyManager.Instance.Balance : 0;
            bool scrapOk = bal >= cost.Value.scrap;
            Row($"스크랩  ◈ {cost.Value.scrap:N0}   (보유 {bal:N0})", 15,
                scrapOk ? new Color(0.8f, 0.9f, 0.8f) : new Color(1f, 0.55f, 0.5f));

            // 재료 (가방 + 창고 합산)
            foreach (var (id, qty) in cost.Value.mats)
            {
                int owned = HideoutModuleManager.CountMaterial(id);
                bool ok = owned >= qty;
                string nm = MatName(id);
                Row($"{nm}  x{qty}   (보유 {owned})", 15,
                    ok ? new Color(0.8f, 0.9f, 0.8f) : new Color(1f, 0.55f, 0.5f));
            }

            // 건설/업그레이드 버튼
            bool can = mgr.CanUpgrade(m, out _);
            string label = lv == 0 ? "건설" : "업그레이드";
            string mm = m;
            BuildButton(label, can ? new Color(0.28f, 0.6f, 0.36f) : new Color(0.42f, 0.32f, 0.32f), can, () =>
            {
                if (HideoutModuleManager.Instance != null) HideoutModuleManager.Instance.Upgrade(mm);
            });
        }

        // ── 발전기: 전력 ON/OFF 토글(일반 기능 버튼 대신 특수 처리) ──
        if (m == "generator")
        {
            if (lv >= 1)
            {
                Spacer(10);
                Row("── 전력 ──", 15, new Color(0.8f, 0.82f, 0.9f));
                bool powered = mgr.GeneratorPowered;
                Row($"전력: {(powered ? "<color=#73DA73>ON</color>" : "<color=#FF8C73>OFF</color>")}", 18, Color.white)
                    .fontStyle = FontStyle.Bold;
                if (!powered)
                    Row("전력 켜기 = 연료(fuel_can) 1 소비", 13, new Color(0.7f, 0.7f, 0.75f));
                BuildButton(powered ? "전력 끄기" : "전력 켜기",
                    powered ? new Color(0.5f, 0.32f, 0.28f) : new Color(0.28f, 0.5f, 0.4f), true, () =>
                    {
                        var mr = HideoutModuleManager.Instance;
                        if (mr != null && !mr.ToggleGeneratorPower(out var reason))
                            ToastManager.Show(reason, ToastManager.ToastType.Warning);
                        Refresh();
                    });
            }
            return;
        }

        // ── 기능 ──
        string funcLabel = FuncLabel(m);
        if (funcLabel != null)
        {
            Spacer(10);
            Row("── 기능 ──", 15, new Color(0.8f, 0.82f, 0.9f));
            if (lv >= 1)
            {
                string mm = m;
                BuildButton(funcLabel, new Color(0.26f, 0.4f, 0.62f), true, () => UseFacility(mm));
            }
            else
            {
                Row("건설 후 사용 가능", 14, new Color(0.7f, 0.7f, 0.75f));
            }
        }
    }

    /// <summary>시설 기능 버튼 라벨. null이면 기능 없음(발전기 등).</summary>
    static string FuncLabel(string m) => m switch
    {
        "workbench" => "제작 (작업대)",
        "medbench"  => "제작 (의료대)",
        "cooking"   => "제작 (조리대)",
        "stash"     => "창고 열기",
        "radio"     => "라디오 듣기",
        "dispatch"  => "파견 보내기",
        "quarters"  => "휴식",
        _ => null,
    };

    void UseFacility(string m)
    {
        Close();
        switch (m)
        {
            case "workbench": UIManager.Instance?.ShowCrafting(CraftingStation.Workbench); break;
            case "medbench":  UIManager.Instance?.ShowCrafting(CraftingStation.MedicalBench); break;
            case "cooking":   UIManager.Instance?.ShowCrafting(CraftingStation.CookingBench); break;
            case "stash":     UIManager.Instance?.ShowCharacterPanelWithStash(); break;
            case "radio":     RadioUI.Show(); break;
            case "dispatch":  DispatchUI.Show(); break;
            case "quarters":  SleepUI.Show(); break;
        }
    }

    static string MatName(string id)
    {
        var data = ItemDatabase.Get(id);
        return data != null ? data.displayName : id;
    }

    static InventoryGrid PlayerGrid()
    {
        var pgo = GameObject.FindGameObjectWithTag("Player");
        var inv = pgo != null ? pgo.GetComponent<PlayerInventory>() : null;
        return inv != null ? inv.Grid : null;
    }

    // ── 행/버튼 헬퍼 ──
    Text Row(string text, int size, Color color)
    {
        var go = new GameObject("Row", typeof(RectTransform));
        go.transform.SetParent(listContent, false);
        go.AddComponent<LayoutElement>().minHeight = size + 12;
        var t = go.AddComponent<Text>();
        t.font = font; t.fontSize = size; t.alignment = TextAnchor.MiddleLeft; t.color = color; t.text = text;
        t.supportRichText = true;
        return t;
    }

    void Spacer(float h)
    {
        var go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(listContent, false);
        go.AddComponent<LayoutElement>().minHeight = h;
    }

    void BuildButton(string label, Color bg, bool interactable, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button", typeof(RectTransform));
        go.transform.SetParent(listContent, false);
        go.AddComponent<LayoutElement>().minHeight = 54;
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable = interactable;
        btn.onClick.AddListener(onClick);
        var lbl = MakeChild(go.transform, label, 17, TextAnchor.MiddleCenter);
        lbl.fontStyle = FontStyle.Bold;
    }

    // ── UI 스캐폴드 ──
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
        winRT.sizeDelta = new Vector2(640, 560);
        win.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.11f, 0.99f);

        titleText = MakeText(win.transform, "시설", 26, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -18), new Vector2(420, 40));
        titleText.fontStyle = FontStyle.Bold;

        scrapText = MakeText(win.transform, "◈ 0", 20, TextAnchor.MiddleRight,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-96, -20), new Vector2(220, 34));
        scrapText.color = new Color(1f, 0.85f, 0.3f);

        var closeGO = new GameObject("Close", typeof(RectTransform));
        closeGO.transform.SetParent(win.transform, false);
        var cRT = closeGO.GetComponent<RectTransform>();
        cRT.anchorMin = cRT.anchorMax = cRT.pivot = new Vector2(1, 1);
        cRT.anchoredPosition = new Vector2(-44, -20); cRT.sizeDelta = new Vector2(44, 34);
        closeGO.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f);
        closeGO.AddComponent<Button>().onClick.AddListener(Close);
        MakeChild(closeGO.transform, "✕", 18, TextAnchor.MiddleCenter);

        // 본문 목록(시설 1개 분량)
        var area = NewRect("Body", win.transform, new Vector2(0, 0), new Vector2(1, 1));
        var areaRT = area.GetComponent<RectTransform>();
        areaRT.offsetMin = new Vector2(20, 20); areaRT.offsetMax = new Vector2(-20, -68);
        area.AddComponent<Image>().color = new Color(0, 0, 0, 0.25f);

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(area.transform, false);
        listContent = content.GetComponent<RectTransform>();
        listContent.anchorMin = new Vector2(0, 0); listContent.anchorMax = new Vector2(1, 1);
        listContent.offsetMin = new Vector2(12, 12); listContent.offsetMax = new Vector2(-12, -12);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6; vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
        vlg.childAlignment = TextAnchor.UpperLeft;

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
