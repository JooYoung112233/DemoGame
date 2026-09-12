using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일시정지 / 설정 메뉴. ESC를 눌렀는데 열린 UI가 없을 때 UIManager가 띄운다.
/// 버튼: 계속하기 / 설정(준비 중) / 게임 종료. 표시 중 timeScale=0(복귀 시 원복).
/// 절차적 uGUI(legacy Text), TitleScreen과 동일 관례. IsAnyUIOpen()에 포함된다.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    public bool IsShowing => canvas != null && canvas.gameObject.activeSelf;
    bool IsGenerated => canvas != null;

    [SerializeField] Canvas canvas;
    [SerializeField] Button resumeBtn;
    [SerializeField] Button settingsBtn;
    [SerializeField] Button quitBtn;
    Font font;
    float _prevTimeScale = 1f;

    public static PauseMenu Show()
    {
        if (Instance == null)
        {
            // 프리팹 우선(Instantiate가 Awake로 Instance 세팅), 없으면 코드 생성 폴백.
            var prefab = Resources.Load<GameObject>("UI/PauseMenu");
            GameObject go = prefab != null ? Instantiate(prefab) : new GameObject("PauseMenu");
            go.name = "PauseMenu";
            DontDestroyOnLoad(go);
            if (prefab == null) Instance = go.AddComponent<PauseMenu>();   // 폴백: Awake가 BuildUI
            EnsureEventSystem();
        }
        Instance.SetVisible(true);
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (!IsGenerated) BuildUI();   // 폴백: 프리팹 없이 코드로 생성
        WireEvents();                  // onClick은 프리팹에 직렬화 안 됨 → 양쪽 경로에서 항상 재부착
    }

    /// <summary>버튼 onClick 재부착. 프리팹 인스턴스는 BuildUI를 스킵하므로 직렬화된 버튼 ref에 리스너를 다시 건다.</summary>
    void WireEvents()
    {
        if (resumeBtn   != null) { resumeBtn.onClick.RemoveAllListeners();   resumeBtn.onClick.AddListener(OnResume); }
        if (settingsBtn != null) { settingsBtn.onClick.RemoveAllListeners(); settingsBtn.onClick.AddListener(OnSettings); }
        if (quitBtn     != null) { quitBtn.onClick.RemoveAllListeners();     quitBtn.onClick.AddListener(OnQuit); }
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void BuildUI()
    {
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGO = new GameObject("Pause_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600; // 모든 UI 위
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        UITheme.ConfigureCanvasScale(scaler);
        canvasGO.AddComponent<GraphicRaycaster>();

        // 어두운 반투명 오버레이(뒤 게임 보이되 흐리게)
        var dim = MakeRect("Dim", canvasGO.transform);
        Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        var frame = MakeRect("MenuFrame", canvasGO.transform);
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f,.5f);
        frame.sizeDelta = new Vector2(480,500);
        frame.gameObject.AddComponent<Image>().color = UITheme.Panel;

        // 제목
        var title = MakeText("Title", canvasGO.transform, "일시정지", 40, FontStyle.Bold,
            UITheme.TextBright);
        Anchor(title, new Vector2(0.5f, 0.5f), new Vector2(0, 180), new Vector2(700, 100));

        MakeButton("계속하기", new Vector2(0, 60), OnResume, out resumeBtn);
        MakeButton("설정", new Vector2(0, -20), OnSettings, out settingsBtn);
        MakeButton("게임 종료", new Vector2(0, -100), OnQuit, out quitBtn);
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — BuildUI를 1회 실행해 프리팹화할 계층을 만든다(EventSystem 등 런타임 셋업 제외).</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
    }
#endif

    void SetVisible(bool v)
    {
        if (v == IsShowing) return;   // 재진입 가드 — 이미 일시정지 중 Show가 또 오면 _prevTimeScale이 0으로 덮여 원복 불가(전체 검수 2026-07-07)
        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (v && canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        if (canvas != null) canvas.gameObject.SetActive(v);
        if (v) { _prevTimeScale = Time.timeScale; Time.timeScale = 0f; }   // 일시정지
        else   { Time.timeScale = _prevTimeScale; }                        // 원복(안전가옥=0, 레이드=1)
    }

    public void Hide() => SetVisible(false);

    // ── 버튼 ─────────────────────────────────────────────
    void OnResume() => Hide();

    void OnSettings() => SettingsUI.Show();   // 일시정지 위 레이어(sortingOrder 620)로 설정 패널

    void OnQuit()
    {
        Time.timeScale = 1f;
        Debug.Log("[Pause] 게임 종료");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── uGUI 헬퍼 ────────────────────────────────────────
    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
        DontDestroyOnLoad(es);
    }

    static RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void Anchor(Text t, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    Text MakeText(string name, Transform parent, string content, int size, FontStyle style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font; t.text = content; t.fontSize = size; t.fontStyle = style; t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    void MakeButton(string label, Vector2 pos, UnityEngine.Events.UnityAction onClick, out Button button)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(360, 64);

        var img = go.AddComponent<Image>();
        img.color = UITheme.Cell;

        button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = UITheme.CellHover;
        colors.pressedColor = UITheme.CellPressed;
        button.colors = colors;
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        var t = MakeText("Label", go.transform, label, 28, FontStyle.Bold, UITheme.TextBright);
        Stretch(t.rectTransform);
    }
}
