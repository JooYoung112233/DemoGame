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

    Canvas canvas;
    Font font;
    float _prevTimeScale = 1f;

    public static PauseMenu Show()
    {
        if (Instance == null)
        {
            var go = new GameObject("PauseMenu");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<PauseMenu>();
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

        var canvasGO = new GameObject("Pause_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600; // 모든 UI 위
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // 어두운 반투명 오버레이(뒤 게임 보이되 흐리게)
        var dim = MakeRect("Dim", canvasGO.transform);
        Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        // 제목
        var title = MakeText("Title", canvasGO.transform, "일시정지", 64, FontStyle.Bold,
            new Color(0.92f, 0.92f, 0.95f));
        Anchor(title, new Vector2(0.5f, 0.5f), new Vector2(0, 180), new Vector2(700, 100));

        MakeButton("계속하기", new Vector2(0, 60), OnResume);
        MakeButton("설정", new Vector2(0, -20), OnSettings);
        MakeButton("게임 종료", new Vector2(0, -100), OnQuit);
    }

    void SetVisible(bool v)
    {
        if (canvas != null) canvas.gameObject.SetActive(v);
        if (v) { _prevTimeScale = Time.timeScale; Time.timeScale = 0f; }   // 일시정지
        else   { Time.timeScale = _prevTimeScale; }                        // 원복(안전가옥=0, 레이드=1)
    }

    public void Hide() => SetVisible(false);

    // ── 버튼 ─────────────────────────────────────────────
    void OnResume() => Hide();

    void OnSettings() => ToastManager.Show("설정 — 준비 중", ToastManager.ToastType.Info);

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

    void MakeButton(string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(360, 64);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.17f, 0.21f, 1f);

        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.65f, 0.8f, 1f, 1f);
        colors.pressedColor = new Color(0.4f, 0.55f, 0.8f, 1f);
        button.colors = colors;
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        var t = MakeText("Label", go.transform, label, 28, FontStyle.Bold, new Color(0.9f, 0.92f, 0.96f));
        Stretch(t.rectTransform);
    }
}
