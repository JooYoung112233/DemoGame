using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하이드아웃 시설 **단일 패널**(타르코프식). 시설 타일을 클릭하면 그 시설 1개만 보여준다:
///   • 미건설(Lv0) → 건설 UI(필요 스크랩+재료)
///   • 건설됨(Lv1~) → 업그레이드 UI(필요 재화) + 그 시설의 기능 버튼(제작/창고/라디오/휴식)
///   • 최대 레벨 → MAX + 기능 버튼만
/// HideoutModuleManager로 건설/업글. 기능 버튼은 각 UI(CraftingUI/RadioUI/…)로 연결.
/// 진입: InteractableObject(시설 타일) 클릭 → HideoutUI.Show(moduleKey).
/// </summary>
public class HideoutUI : MonoBehaviour
{
    public static HideoutUI Instance { get; private set; }
    public bool IsShowing => panel != null && panel.activeSelf;
    public bool IsGenerated => canvas != null;

    [SerializeField] Canvas canvas;
    [SerializeField] GameObject panel;
    [SerializeField] RectTransform listContent;
    [SerializeField] Text titleText;
    [SerializeField] Text scrapText;
    [SerializeField] Button closeBtn;   // 정적 프레임 버튼 — onClick은 프리팹에 직렬화 안 됨 → WireEvents에서 재부착
    Font font;   // 빌트인 LegacyRuntime 폰트 — 직렬화/재바인딩 불필요

    string currentModule = "workbench";

    void Awake()
    {
        if (Instance == null) Instance = this;
        // 프리팹 경로는 GenerateUI(font 할당처)를 스킵 → 동적 Row/버튼 라벨의 font가 null이 되어
        // "제목만 나오고 본문이 전부 빈" 패널이 된다(2026-07-07 하이드아웃 버그). 항상 여기서 보장.
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        WireEvents();   // 프리팹 인스턴스는 GenerateUI를 스킵 → 직렬화된 정적 버튼 ref에 리스너 재부착
    }

    /// <summary>정적 프레임 버튼 onClick 재부착. 프리팹 경로는 GenerateUI를 안 타므로 여기서 다시 건다.</summary>
    void WireEvents()
    {
        if (closeBtn != null) { closeBtn.onClick.RemoveAllListeners(); closeBtn.onClick.AddListener(Close); }
    }

    /// <summary>해당 시설 패널을 연다(온디맨드 생성). 시설 타일 클릭 → 여기로.</summary>
    public static HideoutUI Show(string module = "workbench")
    {
        if (Instance == null)
        {
            // 프리팹 우선(Instantiate가 Awake로 Instance 세팅), 없으면 코드 생성 폴백.
            var prefab = Resources.Load<GameObject>("UI/HideoutUI");
            GameObject go = prefab != null ? Instantiate(prefab) : new GameObject("HideoutUI");
            go.name = "HideoutUI";
            if (prefab == null) go.AddComponent<HideoutUI>();
            DontDestroyOnLoad(go);
        }
        Instance.currentModule = string.IsNullOrEmpty(module) ? "workbench" : module;
        Instance.Open();
        return Instance;
    }

    public void Open()
    {
        if (!IsGenerated) GenerateUI();
        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
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
            Row(lv == 0 ? "── 건설 비용 ──" : $"── Lv{lv + 1} 업그레이드 비용 ──", 15, UITheme.TextMuted);

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
            BuildButton(label, can ? UITheme.Buy : new Color(0.30f, 0.26f, 0.22f, 1f), can, () =>
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
                Row("── 전력 ──", 15, UITheme.TextMuted);
                bool powered = mgr.GeneratorPowered;
                Row($"전력: {(powered ? "<color=#73DA73>ON</color>" : "<color=#FF8C73>OFF</color>")}", 18, Color.white)
                    .fontStyle = FontStyle.Bold;
                if (!powered)
                    Row("전력 켜기 = 연료(fuel_can) 1 소비", 13, UITheme.TextMuted);
                BuildButton(powered ? "전력 끄기" : "전력 켜기",
                    powered ? UITheme.Sell : UITheme.Buy, true, () =>
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
            Row("── 기능 ──", 15, UITheme.TextMuted);
            if (lv >= 1)
            {
                string mm = m;
                BuildButton(funcLabel, UITheme.Accent, true, () => UseFacility(mm));
            }
            else
            {
                Row("건설 후 사용 가능", 14, UITheme.TextMuted);
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
        UITheme.ConfigureCanvasScale(scaler);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        panel = NewRect("Panel", canvasGO.transform, Vector2.zero, Vector2.one);
        panel.AddComponent<Image>().color = UITheme.Backdrop;

        // 중앙 창
        var win = NewRect("Window", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var winRT = win.GetComponent<RectTransform>();
        winRT.sizeDelta = new Vector2(640, 560);
        win.AddComponent<Image>().color = UITheme.Panel;

        titleText = MakeText(win.transform, "시설", 26, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -18), new Vector2(420, 40));
        titleText.fontStyle = FontStyle.Bold;

        scrapText = MakeText(win.transform, "◈ 0", 20, TextAnchor.MiddleRight,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-96, -20), new Vector2(220, 34));
        scrapText.color = UITheme.Gold;

        var closeGO = new GameObject("Close", typeof(RectTransform));
        closeGO.transform.SetParent(win.transform, false);
        var cRT = closeGO.GetComponent<RectTransform>();
        cRT.anchorMin = cRT.anchorMax = cRT.pivot = new Vector2(1, 1);
        cRT.anchoredPosition = new Vector2(-44, -20); cRT.sizeDelta = new Vector2(44, 34);
        closeGO.AddComponent<Image>().color = UITheme.Danger;
        closeBtn = closeGO.AddComponent<Button>();   // ref 저장(프리팹 직렬화 대상) — 리스너는 WireEvents에서 부착
        closeBtn.onClick.AddListener(Close);
        MakeChild(closeGO.transform, "✕", 18, TextAnchor.MiddleCenter);

        // 본문 목록(시설 1개 분량)
        var area = NewRect("Body", win.transform, new Vector2(0, 0), new Vector2(1, 1));
        var areaRT = area.GetComponent<RectTransform>();
        areaRT.offsetMin = new Vector2(20, 20); areaRT.offsetMax = new Vector2(-20, -68);
        area.AddComponent<Image>().color = UITheme.PanelAlt;

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

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — GenerateUI를 1회 실행해 프리팹화할 계층을 만든다.</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        GenerateUI();
    }
#endif

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
