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
    Button _openBtn, _closeBtn;
    System.Action _onOpen;

    public bool IsOpen => _root != null && _root.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Build();
        Hide();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>시설 패널을 연다. <paramref name="dock"/>에 따라 우측/하단에 붙는다.</summary>
    public void Show(string title, string body, HideoutFacilityAnchor.Dock dock, System.Action onOpen)
    {
        if (_root == null) Build();
        _onOpen = onOpen;

        _title.text = title;
        _body.text = body;
        _openBtn.gameObject.SetActive(onOpen != null);

        // 도킹 위치 — 캐릭터가 밀려난 반대쪽에 붙는다.
        if (dock == HideoutFacilityAnchor.Dock.Bottom)
        {
            _panel.anchorMin = new Vector2(0.06f, 0f);
            _panel.anchorMax = new Vector2(0.94f, 0f);
            _panel.pivot     = new Vector2(0.5f, 0f);
            _panel.sizeDelta = new Vector2(0f, 220f);
            _panel.anchoredPosition = new Vector2(0f, 28f);
        }
        else   // Right
        {
            _panel.anchorMin = new Vector2(1f, 0.5f);
            _panel.anchorMax = new Vector2(1f, 0.5f);
            _panel.pivot     = new Vector2(1f, 0.5f);
            _panel.sizeDelta = new Vector2(560f, 720f);
            _panel.anchoredPosition = new Vector2(-32f, 0f);
        }

        _root.SetActive(true);
    }

    public void Hide()
    {
        if (_root != null) _root.SetActive(false);
        _onOpen = null;
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
                            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -24f), new Vector2(-56f, 56f));
        _body    = MakeText(panelGO.transform, "Body", 26, TextAnchor.UpperLeft,
                            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28f, 96f), new Vector2(-56f, -190f));
        _body.color = new Color(0.78f, 0.78f, 0.74f);

        _openBtn  = MakeButton(panelGO.transform, "Open",  "열기",  new Vector2(0f, 0f), new Vector2(28f, 24f),  new Vector2(200f, 56f));
        _closeBtn = MakeButton(panelGO.transform, "Close", "닫기", new Vector2(1f, 0f), new Vector2(-28f, 24f), new Vector2(160f, 56f));

        _openBtn.onClick.AddListener(() => { _onOpen?.Invoke(); });
        _closeBtn.onClick.AddListener(Hide);
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
