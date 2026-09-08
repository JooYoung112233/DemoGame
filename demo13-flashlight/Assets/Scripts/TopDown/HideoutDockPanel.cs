using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 은신처 시설 UI의 **도킹 프레임** — 화면 우측 또는 하단에만 붙어 캐릭터를 가리지 않는다.
///
/// 2026-09-08 결정: 캐릭터가 시설에서 자세를 잡는 것이 이 화면의 요점이므로 그 위에 패널을
/// 덮으면 의미가 없다. 전체화면 패널 금지 — 우측 도킹(요리·작업대·창고 등) 또는
/// 하단 바(침대 = 시간 표기).
///
/// ⚠️ 지금은 **프레임**이다. 기존 시설 UI(HideoutUI/RadioUI/DispatchUI)는 전체화면 전제로
/// 만들어져 있어, 그 내용을 이 안으로 옮기는 작업이 남았다. 그때까지는 패널의 '열기'가
/// 기존 전체화면 UI를 띄운다.
///
/// 설계: docs/hideout-3d.md
/// </summary>
public class HideoutDockPanel : MonoBehaviour
{
    public static HideoutDockPanel Instance { get; private set; }

    const int RefW = 1920, RefH = 1080;

    GameObject _root;
    RectTransform _panel;
    Text _title, _body;
    Button _openBtn, _closeBtn, _upgradeBtn, _useBtn;
    Text _statusText;
    System.Action _onOpen;
    string _module;

    // ── 기존 패널 입양 ──
    RectTransform _content;                 // 입양한 패널이 들어갈 자리
    DockedLayout.Session _session;          // 들고 있는 패널 + 되돌릴 값 전부 (DockedLayout 참조)

    public bool IsOpen => _root != null && _root.activeSelf;
    public bool HasAdopted => _session != null;


    // ── 시설별 도크 크기 ────────────────────────────────────────────
    // 실측(2026-09-08, 1920 기준): 제작 520×480 / 침대 720×460 / 파견 조작부 360×964
    //                              라디오 540+1276 2열 / 창고 643+556+556 3열
    //
    // ⚠️ 도크를 좁게(560) 잡으면 1920 기준으로 짠 패널이 통째로 줄어들어 글씨를 못 읽는다.
    //    캐릭터만 안 가리면 되므로 **화면의 43% 정도까지 넉넉히 준다.** 나머지 57%에 방을 담고,
    //    카메라가 그만큼 방을 왼쪽으로 민다(ResolvedBias가 이 값에서 정확히 계산한다).

    public const float RefWidth  = 1920f;
    public const float RefHeight = 1080f;
    public const float RightMargin  = 32f;   // 도크와 화면 오른쪽 사이
    public const float BottomMargin = 28f;

    public static Vector2 RightDockSize(string moduleKey) => moduleKey switch
    {
        "stash" => new Vector2(880f, 960f),   // 3열이라 한 열(643)이 그대로 들어가야 한다
        _       => new Vector2(840f, 960f),
    };

    /// <summary>하단 바 높이. 침대는 수면창(720×460)이 들어가야 해서 기본 바보다 높다.</summary>
    public static float BottomDockHeight(string moduleKey) => moduleKey switch
    {
        "bed" => 380f,
        _     => 220f,
    };

    /// <summary>제목 + 안내 + 시설 바가 쓰는 화면 상단 띠 높이.</summary>
    public const float TopBandHeight = 180f;

    /// <summary>도크와 상단 띠를 뺀, **방을 담을 수 있는 화면 영역**(정규화 0~1, y는 아래가 0).
    /// 카메라 프레이밍은 여기서 나온다 — 편향과 줌을 따로 손으로 맞추면 도크 크기를 바꿀 때마다
    /// 방이 UI 밑으로 들어가거나 화면 밖으로 잘린다.</summary>
    public static Rect FreeScreenRect(string moduleKey, HideoutFacilityAnchor.Dock dock)
    {
        float top = TopBandHeight / RefHeight;

        if (dock == HideoutFacilityAnchor.Dock.Bottom)
        {
            float b = (BottomDockHeight(moduleKey) + BottomMargin) / RefHeight;
            return new Rect(0f, b, 1f, Mathf.Max(1f - top - b, 0.1f));
        }

        // 도크가 없으면(대기 상태) 상단 띠만 피하고 화면 전체를 쓴다.
        float r = dock == HideoutFacilityAnchor.Dock.None
                ? 0f
                : (RightDockSize(moduleKey).x + RightMargin) / RefWidth;
        return new Rect(0f, 0f, Mathf.Max(1f - r, 0.1f), Mathf.Max(1f - top, 0.1f));
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Build();
        Hide();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>시설 패널을 연다. <paramref name="dock"/>에 따라 우측/하단에 붙는다.</summary>
    public void Show(string title, string body, HideoutFacilityAnchor.Dock dock, System.Action onOpen,
                     string moduleKey = null)
    {
        if (_root == null) Build();
        _onOpen = onOpen;
        _module = moduleKey;

        _title.text = title;
        _body.text = body;
        _openBtn.gameObject.SetActive(onOpen != null);
        RefreshModule();

        // 도킹 위치 — 캐릭터가 밀려난 반대쪽에 붙는다.
        if (dock == HideoutFacilityAnchor.Dock.Bottom)
        {
            _panel.anchorMin = new Vector2(0.06f, 0f);
            _panel.anchorMax = new Vector2(0.94f, 0f);
            _panel.pivot     = new Vector2(0.5f, 0f);
            _panel.sizeDelta = new Vector2(0f, BottomDockHeight(_module));
            _panel.anchoredPosition = new Vector2(0f, 28f);
        }
        else   // Right
        {
            _panel.anchorMin = new Vector2(1f, 0.5f);
            _panel.anchorMax = new Vector2(1f, 0.5f);
            _panel.pivot     = new Vector2(1f, 0.5f);
            _panel.sizeDelta = RightDockSize(_module);
            _panel.anchoredPosition = new Vector2(-32f, 0f);
        }

        // 입양 자리 — 위쪽 88px만 제목(좌)·닫기 X(우)에게 내주고 나머지는 전부 내용이 쓴다.
        // 우측·하단 도크가 같은 규칙이다.
        _content.offsetMin = new Vector2(16f, 16f);
        _content.offsetMax = new Vector2(-16f, -88f);

        _root.SetActive(true);

        // ESC로 닫히도록 UI 매니저의 **열린 순서 스택**에 등록한다.
        if (UIManager.Instance != null)
            UIManager.Instance.PushUI("HideoutDock", () => IsOpen, Hide);
    }

    public void Hide()
    {
        Release();
        if (_root != null) _root.SetActive(false);
        _onOpen = null;
        _module = null;
        if (UIManager.Instance != null) UIManager.Instance.PopUI("HideoutDock");
    }

    /// <summary>남의 패널을 도킹 안으로 끌어들인다 — 전체화면 UI를 덮어쓰지 않고 **자리만** 옮긴다.
    /// 각 UI가 구조가 제각각이라 개별 접근자를 만들지 않고, 방금 켜진 Canvas의 내용을 통째로 받는다.</summary>
    public bool Adopt(RectTransform panel)
    {
        if (panel == null || _content == null) return false;
        Release();

        // 도크 크기가 바뀐 직후라 _content의 rect가 아직 갱신 전일 수 있다.
        // 재배치는 폭 하나로 결정되므로, 여기서 강제로 한 번 계산시킨다.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        var area = _content.rect.size;

        _session = DockedLayout.Prepare(panel, _module, area);
        var placed = _session.Placed;
        if (placed == null) { _session = null; return false; }

        placed.SetParent(_content, false);

        // 도크 자리를 **꽉 채운다.** 제 크기로 가운데 두면 배경만 넓고 내용이 갑갑해 보인다.
        // 스크롤이든 아니든 자리는 똑같이 채우고, 세로로 넘치는 몫만 스크롤이 흡수한다.
        placed.anchorMin = Vector2.zero;
        placed.anchorMax = Vector2.one;
        placed.pivot     = new Vector2(0.5f, 0.5f);
        placed.offsetMin = Vector2.zero;
        placed.offsetMax = Vector2.zero;
        placed.localScale = Vector3.one;

        // 입양하면 설명·상태·열기 버튼은 그 패널이 대신한다.
        _body.gameObject.SetActive(false);
        _statusText.gameObject.SetActive(false);
        _upgradeBtn.gameObject.SetActive(false);
        _openBtn.gameObject.SetActive(false);
        _useBtn.gameObject.SetActive(false);
        return true;
    }

    /// <summary>들고 있던 패널을 원래 자리·원래 모양으로 돌려준다.
    /// ⚠️ 재배치한 것까지 전부 되돌려야 한다 — 안 그러면 은신처 밖에서 그 UI가 찌그러진 채 뜬다.</summary>
    public void Release()
    {
        if (_session == null) return;

        var root = _session.Root;
        _session.Restore();
        _session = null;

        // ⚠️ 되돌려만 놓고 끄지 않으면, 도크를 닫은 뒤에도 그 패널이 전체화면으로
        // 화면에 남는다(=캐릭터를 덮는다). 게다가 계속 켜져 있어서 다음에 같은
        // 시설을 열 때 '새로 켜진 패널'로 안 잡혀 입양이 실패한다.
        if (root != null) root.gameObject.SetActive(false);

        if (_body != null) _body.gameObject.SetActive(true);
        if (_statusText != null) _statusText.gameObject.SetActive(true);
    }

    /// <summary>시설 상태(레벨·다음 비용)를 패널 안에 직접 보여준다.
    /// 전체화면 HideoutUI로 넘어가지 않고 여기서 건설·업그레이드까지 끝난다.</summary>
    void RefreshModule()
    {
        var mm = HideoutModuleManager.Instance;
        if (string.IsNullOrEmpty(_module) || mm == null)
        {
            _statusText.text = "";
            _upgradeBtn.gameObject.SetActive(false);
            return;
        }

        int lv = mm.GetLevel(_module);
        string state = lv <= 0 ? "<미건설>" : "Lv " + lv + (mm.IsMaxed(_module) ? " (최대)" : "");
        string cost = "";
        var next = mm.NextCost(_module);
        if (next.HasValue)
        {
            cost = "\n다음 단계: 스크랩 " + next.Value.scrap;
            if (next.Value.mats != null)
                foreach (var m in next.Value.mats)
                    cost += "  " + m.id + " ×" + m.qty;
        }
        _statusText.text = state + cost;

        bool can = mm.CanUpgrade(_module, out string reason);
        _upgradeBtn.gameObject.SetActive(!mm.IsMaxed(_module));
        _upgradeBtn.interactable = can;
        var lbl = _upgradeBtn.GetComponentInChildren<Text>();
        if (lbl != null) lbl.text = lv <= 0 ? "건설" : "업그레이드";
        if (!can && !string.IsNullOrEmpty(reason)) _statusText.text += "\n\n" + reason;

        // 기능 버튼 — 건설(Lv≥1)된 시설만 뜬다. 라벨은 시설마다 다르다.
        string useLabel = UseLabelFor(_module);
        bool built = lv >= 1 && !string.IsNullOrEmpty(useLabel);
        _useBtn.gameObject.SetActive(built);
        var ul = _useBtn.GetComponentInChildren<Text>();
        if (ul != null) ul.text = useLabel;
    }

    /// <summary>시설별 기능 버튼 이름. 빈 문자열이면 기능 없음.</summary>
    static string UseLabelFor(string key) => key switch
    {
        "workbench" => "제작·수리",
        "cooking"   => "요리",
        "medical"   => "조제",
        "stash"     => "창고 열기",
        "radio"     => "청취",
        "dispatch"  => "파견",
        "bed"       => "휴식",
        "generator" => "전력 전환",
        _           => "",
    };

    /// <summary>시설 기능 실행 — 레시피·스테이션 배선.
    /// 제작 3종은 <see cref="CraftingStation"/> enum으로 갈라지고, 나머지는 각자의 UI를 쓴다.
    /// 여기서 여는 UI들은 아직 전체화면이라, 열릴 때 도킹으로 입양된다(Adopt).</summary>
    void UseFacility()
    {
        var um = UIManager.Instance;

        // 발전기는 UI가 아니라 토글이다 - 입양할 패널이 없다.
        if (_module == "generator")
        {
            var gm = HideoutModuleManager.Instance;
            if (gm == null) return;
            bool ok = gm.ToggleGeneratorPower(out string why);
            ToastManager.Show(ok ? (gm.GeneratorPowered ? "전력 켬" : "전력 끔") : why,
                              ok ? ToastManager.ToastType.Info : ToastManager.ToastType.Warning);
            RefreshModule();
            return;
        }

        // ⚠️ 여기서 여는 UI는 전부 전체화면 전제라, 그냥 띄우면 캐릭터를 덮는다.
        // 열기 직전 화면을 찍어두고 새로 켜진 패널을 도크 안으로 끌어들인다.
        var before = HideoutDiorama.ActivePanels();

        switch (_module)
        {
            case "workbench": if (um != null) um.ShowCrafting(CraftingStation.Workbench);    break;
            case "cooking":   if (um != null) um.ShowCrafting(CraftingStation.CookingBench); break;
            case "medical":   if (um != null) um.ShowCrafting(CraftingStation.MedicalBench); break;
            case "stash":     if (um != null) um.ShowCharacterPanelWithStash();              break;
            case "radio":     RadioUI.Show();     break;
            case "dispatch":  DispatchUI.Show();  break;
            case "bed":       SleepUI.Show();     break;
            default: return;
        }

        var opened = HideoutDiorama.FindNewPanel(before);
        if (opened != null) Adopt(opened);          // Adopt가 기능 버튼까지 정리한다
    }

    // ── uGUI 코드 생성 ──────────────────────────────────────────────
    void Build()
    {
        _root = new GameObject("HideoutDockPanel");
        _root.transform.SetParent(transform, false);

        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefW, RefH);
        scaler.matchWidthOrHeight = 0.5f;
        _root.AddComponent<GraphicRaycaster>();

        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(_root.transform, false);
        _panel = panelGO.AddComponent<RectTransform>();
        var bg = panelGO.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.07f, 0.085f, 0.93f);

        _title   = MakeText(panelGO.transform, "Title", 40, TextAnchor.UpperLeft,
                            // ⚠️ 상단 stretch에서 offMax.y는 위쪽 바깥으로 나가는 값이다.
                            // +56이면 제목이 패널 밖으로 떠서 씬 헤더와 겹친다. 안쪽(-)으로 잡는다.
                            // 우상단 X 자리(-90)를 비워 둔다.
                            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -84f), new Vector2(-90f, -24f));
        _body    = MakeText(panelGO.transform, "Body", 26, TextAnchor.UpperLeft,
                            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28f, 96f), new Vector2(-56f, -190f));
        _body.color = new Color(0.78f, 0.78f, 0.74f);

        _openBtn  = MakeButton(panelGO.transform, "Open",  "열기",  new Vector2(0f, 0f), new Vector2(28f, 24f),  new Vector2(200f, 56f));
        // 닫기 = 우측 상단 X. 아래에 두면 내용이 쓸 세로 자리를 100px 가까이 잡아먹는다.
        _closeBtn = MakeButton(panelGO.transform, "Close", "✕", new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(52f, 52f));

        _statusText = MakeText(panelGO.transform, "Status", 24, TextAnchor.LowerLeft,
                               new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(28f, 170f), new Vector2(-56f, 340f));
        _statusText.color = new Color(0.86f, 0.82f, 0.60f);

        _upgradeBtn = MakeButton(panelGO.transform, "Upgrade", "업그레이드",
                                 new Vector2(0f, 0f), new Vector2(28f, 92f), new Vector2(260f, 56f));

        // 기능 버튼 - 건설 버튼 오른쪽. 시설을 실제로 쓰는 입구(제작/요리/창고/휴식).
        _useBtn = MakeButton(panelGO.transform, "Use", "사용",
                             new Vector2(0f, 0f), new Vector2(300f, 92f), new Vector2(232f, 56f));
        _useBtn.gameObject.SetActive(false);

        // 입양 자리 — 제목 아래, 버튼 위
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(panelGO.transform, false);
        _content = contentGO.AddComponent<RectTransform>();
        _content.anchorMin = new Vector2(0f, 0f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.offsetMin = new Vector2(16f, 92f);
        _content.offsetMax = new Vector2(-16f, -80f);
        // 입양하는 UI들은 1920 폭 전체화면 전제로 만들어져 있어, 도크에 넣으면
        // 자식들이 밖으로 삐져나온다. 도크 경계에서 잘라 화면이 지저분해지지 않게.
        contentGO.AddComponent<RectMask2D>();

        _openBtn.onClick.AddListener(() => { _onOpen?.Invoke(); });
        _closeBtn.onClick.AddListener(Hide);
        _useBtn.onClick.AddListener(UseFacility);
        _upgradeBtn.onClick.AddListener(() =>
        {
            var mm = HideoutModuleManager.Instance;
            if (mm == null || string.IsNullOrEmpty(_module)) return;
            if (mm.Upgrade(_module)) RefreshModule();
            else { mm.CanUpgrade(_module, out string why); ToastManager.Show(why, ToastManager.ToastType.Warning); }
        });
    }

    static Text MakeText(Transform parent, string name, int size, TextAnchor anchor,
                         Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = new Vector2(offMin.x, offMin.y);
        rt.offsetMax = new Vector2(offMax.x, offMax.y);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }

    static Button MakeButton(Transform parent, string name, string label,
                             Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.25f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var t = MakeText(go.transform, "Label", 28, TextAnchor.MiddleCenter,
                         Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        t.text = label;
        return btn;
    }
}
