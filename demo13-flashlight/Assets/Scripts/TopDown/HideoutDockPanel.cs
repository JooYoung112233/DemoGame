using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 은신처 시설 UI의 **도킹 프레임** — 화면 우측 또는 하단에만 붙어 캐릭터를 가리지 않는다.
///
/// 2026-09-08 결정: 캐릭터가 시설에서 자세를 잡는 것이 이 화면의 요점이므로 그 위에 패널을
/// 덮으면 의미가 없다. 전체화면 패널 금지 — 우측 도킹(요리·작업대·창고 등) 또는
/// 하단 바(침대 = 시간 표기).
///
/// ⚠️ 지금은 **프레임**이다. 기존 시설 UI(HideoutUI/RadioUI)는 전체화면 전제로
/// 만들어져 있어, 그 내용을 이 안으로 옮기는 작업이 남았다. 그때까지는 패널의 '열기'가
/// 기존 전체화면 UI를 띄운다.
///
/// 설계: docs/hideout-3d.md
/// </summary>
public partial class HideoutDockPanel : MonoBehaviour
{
    public static HideoutDockPanel Instance { get; private set; }

    const int RefW = 1920, RefH = 1080;

    GameObject _root;
    RectTransform _panel;
    Image _dockDim;
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

        _dockDim.gameObject.SetActive(dock == HideoutFacilityAnchor.Dock.Right);
        _dockDim.rectTransform.sizeDelta = new Vector2(RightDockSize(_module).x + RightMargin + 24f, 0f);
        _dockDim.color = new Color(0f, 0f, 0f, Tune != null ? Tune.hideoutDockDimAlpha : .32f);

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

}
