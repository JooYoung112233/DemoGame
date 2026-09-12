using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 튜토리얼 프롬프트 UI.
/// 화면 중앙 하단에 짧은 안내 텍스트를 표시하고 자동 소멸.
/// 같은 ID의 프롬프트는 세션 내 1회만 표시.
/// </summary>
public class TutorialPrompt : MonoBehaviour
{
    public static TutorialPrompt Instance { get; private set; }

    HashSet<string> shownPrompts = new HashSet<string>();

    // uGUI
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject promptRoot;
    [SerializeField] Text promptText;
    [SerializeField] Image bgImage;
    [SerializeField] CanvasGroup canvasGroup;

    Coroutine activeCoroutine;

    // H키 튜토리얼 패널
    [SerializeField] GameObject tutorialPanel;
    [SerializeField] Text tutorialPanelText;
    [SerializeField] Button startTutorialBtn;
    [SerializeField] Button closeTutorialBtn;
    bool tutorialPanelOpen;

    const float DEFAULT_DURATION = 4f;
    const float FADE_DURATION = 0.5f;

    public bool IsGenerated => canvas != null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (!IsGenerated) GenerateUI();
        BindEvents();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (GameInput.GetKeyDown(KeyCode.H))
        {
            ToggleTutorialPanel();
        }

        if (tutorialPanelOpen && GameInput.GetKeyDown(KeyCode.Escape))
        {
            CloseTutorialPanel();
        }
    }

    public void GenerateUI()
    {
        // Canvas on child object
        var canvasGO = new GameObject("TutorialPrompt_Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 85;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        UITheme.ConfigureCanvasScale(scaler);

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── 프롬프트 루트 ──
        promptRoot = new GameObject("PromptPanel");
        promptRoot.transform.SetParent(canvas.transform, false);
        var rt = promptRoot.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(500f, 50f);
        rt.anchoredPosition = new Vector2(0, 180f);

        bgImage = promptRoot.AddComponent<Image>();
        bgImage.color = new Color(UITheme.Panel.r, UITheme.Panel.g, UITheme.Panel.b, 0.8f);

        canvasGroup = promptRoot.AddComponent<CanvasGroup>();

        // 텍스트
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(promptRoot.transform, false);
        promptText = textGO.AddComponent<Text>();
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 20;
        promptText.color = UITheme.Gold;
        promptText.alignment = TextAnchor.MiddleCenter;
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(10, 5);
        textRT.offsetMax = new Vector2(-10, -5);

        promptRoot.SetActive(false);

        // ── 튜토리얼 패널 ──
        tutorialPanel = new GameObject("TutorialPanel");
        tutorialPanel.transform.SetParent(canvas.transform, false);

        var panelRT = tutorialPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(520f, 420f);

        var bg = tutorialPanel.AddComponent<Image>();
        bg.color = new Color(UITheme.Panel.r, UITheme.Panel.g, UITheme.Panel.b, 0.94f);

        // 조작법 텍스트 (상단 영역)
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(tutorialPanel.transform, false);
        tutorialPanelText = contentGO.AddComponent<Text>();
        tutorialPanelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tutorialPanelText.fontSize = 18;
        tutorialPanelText.color = UITheme.TextBright;
        tutorialPanelText.alignment = TextAnchor.UpperLeft;
        tutorialPanelText.supportRichText = true;
        tutorialPanelText.lineSpacing = 1.3f;

        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(24, 80);
        contentRT.offsetMax = new Vector2(-24, -20);

        // ── 하단 버튼 영역 ──

        // 튜토리얼 시작 버튼
        var startBtnGO = new GameObject("StartBtn");
        startBtnGO.transform.SetParent(tutorialPanel.transform, false);
        var startBtnRT = startBtnGO.AddComponent<RectTransform>();
        startBtnRT.anchorMin = new Vector2(0, 0);
        startBtnRT.anchorMax = new Vector2(0.5f, 0);
        startBtnRT.pivot = new Vector2(0, 0);
        startBtnRT.offsetMin = new Vector2(24, 16);
        startBtnRT.offsetMax = new Vector2(-8, 60);

        var startBtnImg = startBtnGO.AddComponent<Image>();
        startBtnImg.color = UITheme.Buy;

        startTutorialBtn = startBtnGO.AddComponent<Button>();
        startTutorialBtn.targetGraphic = startBtnImg;
        var startColors = startTutorialBtn.colors;
        startColors.highlightedColor = UITheme.BuyHi;
        startColors.pressedColor = UITheme.Buy;
        startTutorialBtn.colors = startColors;

        MakeBtnText(startBtnGO.transform, "▶ 튜토리얼 시작");

        // 닫기 버튼
        var closeBtnGO = new GameObject("CloseBtn");
        closeBtnGO.transform.SetParent(tutorialPanel.transform, false);
        var closeBtnRT = closeBtnGO.AddComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(0.5f, 0);
        closeBtnRT.anchorMax = new Vector2(1, 0);
        closeBtnRT.pivot = new Vector2(1, 0);
        closeBtnRT.offsetMin = new Vector2(8, 16);
        closeBtnRT.offsetMax = new Vector2(-24, 60);

        var closeBtnImg = closeBtnGO.AddComponent<Image>();
        closeBtnImg.color = UITheme.Danger;

        closeTutorialBtn = closeBtnGO.AddComponent<Button>();
        closeTutorialBtn.targetGraphic = closeBtnImg;
        var closeColors = closeTutorialBtn.colors;
        closeColors.highlightedColor = new Color(0.52f, 0.20f, 0.16f);
        closeColors.pressedColor = new Color(0.30f, 0.10f, 0.09f);
        closeTutorialBtn.colors = closeColors;

        MakeBtnText(closeBtnGO.transform, "닫기 [H]");

        tutorialPanel.SetActive(false);
    }

    public void BindEvents()
    {
        if (startTutorialBtn != null) startTutorialBtn.onClick.AddListener(OnStartTutorialClicked);
        if (closeTutorialBtn != null) closeTutorialBtn.onClick.AddListener(CloseTutorialPanel);
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — GenerateUI를 1회 실행해 프리팹화할 계층을 만든다.</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        GenerateUI();
    }
#endif

    public void ClearGeneratedUI()
    {
        if (canvas != null)
        {
            if (Application.isPlaying)
                Destroy(canvas.gameObject);
            else
                DestroyImmediate(canvas.gameObject);
        }

        canvas = null;
        promptRoot = null;
        promptText = null;
        bgImage = null;
        canvasGroup = null;
        tutorialPanel = null;
        tutorialPanelText = null;
        startTutorialBtn = null;
        closeTutorialBtn = null;
    }

    // ════════════════════════════════════════
    //  Show / Fade
    // ════════════════════════════════════════

    /// <summary>
    /// 튜토리얼 프롬프트 표시. id가 있으면 세션 내 1회만.
    /// </summary>
    public void Show(string text, float duration = DEFAULT_DURATION, string id = null)
    {
        if (!string.IsNullOrEmpty(id))
        {
            if (shownPrompts.Contains(id)) return;
            shownPrompts.Add(id);
        }

        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(ShowRoutine(text, duration));
    }

    IEnumerator ShowRoutine(string text, float duration)
    {
        promptText.text = text;
        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        promptRoot.SetActive(true);

        // 페이드 인
        float t = 0;
        while (t < FADE_DURATION)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / FADE_DURATION);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 대기
        yield return new WaitForSecondsRealtime(duration);

        // 페이드 아웃
        t = 0;
        while (t < FADE_DURATION)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(t / FADE_DURATION);
            yield return null;
        }

        promptRoot.SetActive(false);
        activeCoroutine = null;
    }

    /// <summary>
    /// 특정 프롬프트가 이미 표시되었는지 확인.
    /// </summary>
    public bool HasShown(string id)
    {
        return shownPrompts.Contains(id);
    }

    /// <summary>
    /// 세이브/로드용: 표시된 프롬프트 ID 목록.
    /// </summary>
    public HashSet<string> GetShownIds() => new HashSet<string>(shownPrompts);
    public void SetShownIds(HashSet<string> ids) { shownPrompts = ids ?? new HashSet<string>(); }

    // ════════════════════════════════════════
    //  H키 튜토리얼 패널
    // ════════════════════════════════════════

    void ToggleTutorialPanel()
    {
        if (tutorialPanelOpen)
        {
            CloseTutorialPanel();
        }
        else
        {
            OpenTutorialPanel();
        }
    }

    void OpenTutorialPanel()
    {
        // 현재 상황에 맞는 튜토리얼 내용 구성
        string content = BuildTutorialContent();
        tutorialPanelText.text = content;
        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        tutorialPanel.SetActive(true);
        tutorialPanelOpen = true;
    }

    void CloseTutorialPanel()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
        tutorialPanelOpen = false;
    }

    string BuildTutorialContent()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<color=#FFD700><b>◈ 조작법</b></color>");
        sb.AppendLine();

        // StoryLocale에서 가져오기 (있으면), 없으면 하드코딩 폴백
        var loc = StoryLocale.Instance;

        sb.AppendLine(GetTutText(loc, "TUT_MOVE", "WASD — 이동"));
        sb.AppendLine(GetTutText(loc, "TUT_PICKUP", "E — 줍기 / 상호작용"));
        sb.AppendLine(GetTutText(loc, "TUT_INVENTORY", "Tab — 인벤토리"));
        sb.AppendLine(GetTutText(loc, "TUT_SEARCH", "E — 수색"));
        sb.AppendLine(GetTutText(loc, "TUT_COMBAT", "좌클릭 — 약공격 / 우클릭(홀드) — 강공격 / Space — 구르기"));
        sb.AppendLine(GetTutText(loc, "TUT_FLASHLIGHT", "F — 손전등 ON/OFF"));
        sb.AppendLine(GetTutText(loc, "TUT_REST", "침대에서 휴식 → HP 회복"));

        return sb.ToString();
    }

    string GetTutText(StoryLocale loc, string key, string fallback)
    {
        if (loc != null && loc.HasKey(key))
            return $"  {loc.Get(key)}";
        return $"  {fallback}";
    }

    void OnStartTutorialClicked()
    {
        CloseTutorialPanel();

        // 프롤로그부터 재생
        if (StoryPlayer.Instance != null)
        {
            StoryPlayer.Instance.PlayScene("S-000", () =>
            {
                // 프롤로그 후 H키 안내
                Show("H — 조작법 보기", 5f, null);
            });
        }
    }

    void MakeBtnText(Transform parent, string label)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 16;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
    }
}
