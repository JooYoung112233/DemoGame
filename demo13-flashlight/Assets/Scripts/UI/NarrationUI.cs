using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 내레이션(독백) UI. NPC 대화(DialogueUI)와 별도.
/// 화면 하단에 반투명 패널로 주인공 내면 독백을 표시.
/// </summary>
public class NarrationUI : MonoBehaviour
{
    public static NarrationUI Instance { get; private set; }

    public bool IsShowing => isShowing;
    public bool IsGenerated => canvas != null;

    bool isShowing;
    string[] currentLines;
    int lineIndex;
    System.Action onComplete;

    // 타이핑
    Coroutine typingCoroutine;
    bool isTyping;
    string fullLineText;

    // uGUI
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject panelRoot;
    [SerializeField] Text narrationText;
    [SerializeField] Text continueHint;
    [SerializeField] Image panelBg;

    // 설정
    const float TYPING_SPEED = 0.03f;
    const float PANEL_WIDTH = 900f;
    const float PANEL_HEIGHT = 100f;
    const float PANEL_BOTTOM_MARGIN = 60f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (!IsGenerated) GenerateUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void GenerateUI()
    {
        // Canvas (자식 오브젝트로 생성)
        var canvasGO = new GameObject("NarrationUI_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; // DialogueUI(100)보다 아래

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        // 패널
        panelRoot = new GameObject("NarrationPanel");
        panelRoot.transform.SetParent(canvas.transform, false);
        var panelRT = panelRoot.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0f);
        panelRT.anchorMax = new Vector2(0.5f, 0f);
        panelRT.pivot = new Vector2(0.5f, 0f);
        panelRT.sizeDelta = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);
        panelRT.anchoredPosition = new Vector2(0, PANEL_BOTTOM_MARGIN);

        panelBg = panelRoot.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.7f);

        // 내레이션 텍스트
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(panelRoot.transform, false);
        narrationText = textGO.AddComponent<Text>();
        narrationText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        narrationText.fontSize = 22;
        narrationText.color = new Color(0.85f, 0.85f, 0.8f);
        narrationText.fontStyle = FontStyle.Italic;
        narrationText.alignment = TextAnchor.MiddleCenter;
        narrationText.horizontalOverflow = HorizontalWrapMode.Wrap;
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(30, 10);
        textRT.offsetMax = new Vector2(-30, -25);

        // 계속 힌트
        var hintGO = new GameObject("ContinueHint");
        hintGO.transform.SetParent(panelRoot.transform, false);
        continueHint = hintGO.AddComponent<Text>();
        continueHint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        continueHint.fontSize = 14;
        continueHint.color = new Color(0.5f, 0.5f, 0.5f);
        continueHint.alignment = TextAnchor.LowerRight;
        continueHint.text = "클릭하여 계속";
        var hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = Vector2.zero;
        hintRT.anchorMax = Vector2.one;
        hintRT.offsetMin = new Vector2(0, 5);
        hintRT.offsetMax = new Vector2(-15, 0);

        panelRoot.SetActive(false);
    }

    public void BindEvents()
    {
        // 현재 바인딩할 이벤트 없음 — 일관성을 위해 유지
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — 현재 GenerateUI를 1회 실행해 프리팹화할 계층을 만든다.</summary>
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
        panelRoot = null;
        narrationText = null;
        continueHint = null;
        panelBg = null;
    }

    void Update()
    {
        if (!isShowing) return;

        if (GameInput.GetMouseButtonDown(0) || GameInput.GetKeyDown(KeyCode.Space) || GameInput.GetKeyDown(KeyCode.Return))
        {
            if (isTyping)
            {
                // 타이핑 중이면 즉시 완성
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                narrationText.text = fullLineText;
                isTyping = false;
                continueHint.gameObject.SetActive(true);
            }
            else
            {
                // 다음 줄로
                lineIndex++;
                if (lineIndex < currentLines.Length)
                    ShowLine(currentLines[lineIndex]);
                else
                    Close();
            }
        }
    }

    /// <summary>
    /// 내레이션 시작. 여러 줄을 순서대로 표시.
    /// </summary>
    public void Show(string[] lines, System.Action onComplete = null)
    {
        if (lines == null || lines.Length == 0) return;

        this.currentLines = lines;
        this.lineIndex = 0;
        this.onComplete = onComplete;

        isShowing = true;
        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        panelRoot.SetActive(true);

        // 플레이어 입력 차단
        if (UIManager.Instance != null)
        {
            // UIManager가 IsAnyPanelOpen 체크 시 이 패널도 포함되도록
        }

        ShowLine(currentLines[0]);
    }

    /// <summary>
    /// 한 줄짜리 간단한 내레이션.
    /// </summary>
    public void Show(string line, System.Action onComplete = null)
    {
        Show(new string[] { line }, onComplete);
    }

    void ShowLine(string text)
    {
        fullLineText = text;
        continueHint.gameObject.SetActive(false);
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLine(text));
    }

    IEnumerator TypeLine(string text)
    {
        isTyping = true;
        narrationText.text = "";
        float speed = GameTuning.Instance != null ? GameTuning.Instance.narrationTypingSpeed : TYPING_SPEED;
        foreach (char c in text)
        {
            narrationText.text += c;
            yield return new WaitForSecondsRealtime(speed);
        }
        isTyping = false;
        continueHint.gameObject.SetActive(true);
    }

    void Close()
    {
        isShowing = false;
        panelRoot.SetActive(false);
        currentLines = null;

        var callback = onComplete;
        onComplete = null;
        callback?.Invoke();
    }

    /// <summary>외부에서 강제로 내레이션을 닫는다(모달 UI 진입 시 잔류 방지).
    /// onComplete는 **호출한다**(스킵 처리) — 삼켜버리면 이 콜백을 기다리는 StoryPlayer의
    /// 씬 재생이 영구히 멈춰 다음 노드/플래그가 죽는다(전체 검수 2026-07-07 잔여분).
    /// 시각 표시만 치우고 스토리 진행은 완료로 간주.</summary>
    public void Dismiss()
    {
        if (!isShowing) return;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        isShowing = false;
        if (panelRoot != null) panelRoot.SetActive(false);
        currentLines = null;

        var callback = onComplete;
        onComplete = null;
        callback?.Invoke();
    }
}
