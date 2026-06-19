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

    Canvas canvas;
    Button continueBtn;
    Font font;

    /// <summary>타이틀을 띄운다(없으면 생성). 부팅/타이틀복귀에서 호출.</summary>
    public static TitleScreen Show()
    {
        if (Instance == null)
        {
            var go = new GameObject("TitleScreen");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<TitleScreen>();
            Instance.BuildUI();
        }
        Instance.SetVisible(true);
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void BuildUI()
    {
        EnsureEventSystem();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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
        MakeButton("새 게임", new Vector2(0, 0), OnNewGame, out _);
        MakeButton("이어하기", new Vector2(0, -80), OnContinue, out continueBtn);
        MakeButton("종료", new Vector2(0, -160), OnQuit, out _);

        // 버전 표기
        var ver = MakeText("Version", canvasGO.transform, "프로토타입 v0.1", 20, FontStyle.Normal,
            UITheme.TextDim);
        Anchor(ver, new Vector2(1f, 0f), new Vector2(-110, 30), new Vector2(200, 30));
    }

    void SetVisible(bool v)
    {
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
        var es = new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));
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
