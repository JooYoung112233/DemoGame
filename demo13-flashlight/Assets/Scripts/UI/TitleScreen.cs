using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 시작 화면(타이틀/메인 메뉴). 부팅 시 GameBoot가 호출(showTitleOnBoot).
/// 버튼: 새 게임 / 이어하기(세이브 있을 때만) / 종료.
/// 타이틀은 **세이브 상태 + 씬 전환**만 제어 — 실제 새게임/로드 분기는 Safehouse 씬의 GameStartHandler가 처리
/// (세이브 있으면 로드, 없으면 프롤로그 S-000). 절차적 uGUI(legacy Text), ToastManager 캔버스 관례.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    public static TitleScreen Instance { get; private set; }

    bool IsGenerated => canvas != null;

    [SerializeField] Canvas canvas;
    [SerializeField] Button newGameBtn;
    [SerializeField] Button continueBtn;
    [SerializeField] Button quitBtn;
    Font font;

    /// <summary>타이틀을 띄운다(없으면 생성). 부팅/타이틀복귀에서 호출.</summary>
    public static TitleScreen Show()
    {
        if (Instance == null)
        {
            // 프리팹 우선(Instantiate가 Awake로 Instance 세팅), 없으면 코드 생성 폴백.
            var prefab = Resources.Load<GameObject>("UI/TitleScreen");
            GameObject go = prefab != null ? Instantiate(prefab) : new GameObject("TitleScreen");
            go.name = "TitleScreen";
            DontDestroyOnLoad(go);
            if (prefab == null) Instance = go.AddComponent<TitleScreen>();   // 폴백: Awake가 BuildUI
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
        if (newGameBtn  != null) { newGameBtn.onClick.RemoveAllListeners();  newGameBtn.onClick.AddListener(OnNewGame); }
        if (continueBtn != null) { continueBtn.onClick.RemoveAllListeners(); continueBtn.onClick.AddListener(OnContinue); }
        if (quitBtn     != null) { quitBtn.onClick.RemoveAllListeners();     quitBtn.onClick.AddListener(OnQuit); }
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void BuildUI()
    {
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGO = new GameObject("Title_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // 모든 UI 위
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // 배경(어두운 풀스크린)
        var bg = MakeRect("BG", canvasGO.transform);
        Stretch(bg);
        bg.gameObject.AddComponent<Image>().color = UITheme.Backdrop;

        // 타이틀
        var title = MakeText("Title", canvasGO.transform, "다녀올게", 96, FontStyle.Bold,
            UITheme.TextBright);
        Anchor(title, new Vector2(0.5f, 0.5f), new Vector2(0, 240), new Vector2(900, 140));

        // 부제
        var sub = MakeText("Subtitle", canvasGO.transform, "— 안전가옥에서 다시 돌아오기까지 —", 28,
            FontStyle.Italic, UITheme.TextMuted);
        Anchor(sub, new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(900, 50));

        // 버튼들
        MakeButton("새 게임", new Vector2(0, 0), OnNewGame, out newGameBtn);
        MakeButton("이어하기", new Vector2(0, -80), OnContinue, out continueBtn);
        MakeButton("종료", new Vector2(0, -160), OnQuit, out quitBtn);

        // 버전 표기
        var ver = MakeText("Version", canvasGO.transform, "프로토타입 v0.1", 20, FontStyle.Normal,
            UITheme.TextDim);
        Anchor(ver, new Vector2(1f, 0f), new Vector2(-110, 30), new Vector2(200, 30));
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
        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (v && canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        if (canvas != null) canvas.gameObject.SetActive(v);
        if (v && continueBtn != null)
            continueBtn.interactable = SaveManager.Instance != null && SaveManager.Instance.HasSave();
    }

    void Hide() => SetVisible(false);

    // ── 버튼 동작 ───────────────────────────────────────────────

    void OnNewGame()
    {
        // 새 게임 = 기존 세이브 삭제 → Safehouse 진입 시 GameStartHandler가 프롤로그(S-000) 재생
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave())
            SaveManager.Instance.DeleteSave();
        GameStartHandler.ResetSession();   // 안전가옥 진입 시 시작 분기 1회 재실행
        Debug.Log("[Title] 새 게임 → Safehouse");
        Hide();
        GoToSafehouse();
    }

    void OnContinue()
    {
        // 이어하기 = 세이브 보존 → Safehouse 진입 시 GameStartHandler가 로드
        GameStartHandler.ResetSession();   // 안전가옥 진입 시 Load() 1회 재실행
        Debug.Log("[Title] 이어하기 → Safehouse");
        Hide();
        GoToSafehouse();
    }

    void OnQuit()
    {
        Debug.Log("[Title] 종료");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void GoToSafehouse()
    {
        if (SceneTransitionManager.Instance != null)
            // 즉시 검게 덮은 채 로드 → 셋업 → reveal. 클릭 순간 화면이 검어져 HUD/타이틀 깜빡임 없음.
            SceneTransitionManager.Instance.TransitionTo("Safehouse", "default", instantCover: true);
        else
            Debug.LogWarning("[Title] SceneTransitionManager 없음 — Safehouse 전환 불가(씬 미빌드?)");
    }

    // ── uGUI 헬퍼 ───────────────────────────────────────────────

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

    static void Anchor(Text t, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }

    Text MakeText(string name, Transform parent, string content, int size, FontStyle style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    void MakeButton(string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick, out Button button)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(360, 64);

        var img = go.AddComponent<Image>();
        img.color = UITheme.Cell;

        button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = UITheme.CellHover;
        colors.pressedColor = UITheme.CellPressed;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        button.colors = colors;
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        var label_t = MakeText("Label", go.transform, label, 30, FontStyle.Bold,
            UITheme.TextBright);
        Stretch(label_t.rectTransform);
    }
}
