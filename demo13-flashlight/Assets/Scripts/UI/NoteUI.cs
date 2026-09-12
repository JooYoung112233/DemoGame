using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 쪽지/문서 읽기 UI — 일반 게임의 노트 읽기처럼 **화면 전체**에 띄운다.
/// (NPC 대화 DialogueUI(하단바)·내레이션 NarrationUI(하단바)와 별개. 이건 전체 오버레이.)
///
/// 흐름: InteractableObject(Note 타입) 상호작용 → NoteUI.Instance.Show(내용) →
///       어두운 전체 배경 + 가운데 '종이' 패널에 제목/본문 표시 →
///       E / Esc / Space / 클릭 으로 닫기.
///
/// 싱글톤 — GameBootstrap.EnsureSingleton / SystemsSceneBuilder 로 등록(NarrationUI와 동일 방식).
/// UIManager.IsAnyUIOpen()에 포함되어, 열려 있는 동안 플레이어 입력이 차단된다.
/// 프로시저럴 uGUI(씬 배치 불필요) — Awake에서 Canvas 자동 생성.
/// </summary>
public class NoteUI : MonoBehaviour
{
    public static NoteUI Instance { get; private set; }

    public bool IsShowing => isShowing;
    public bool IsGenerated => canvas != null;

    bool isShowing;
    int  openFrame = -1;          // 연 프레임(같은 프레임 입력으로 즉시 닫힘 방지)
    Coroutine fadeCo;

    // uGUI (프리팹 베이크 시 직렬화 보존)
    [SerializeField] Canvas canvas;
    [SerializeField] CanvasGroup group;            // 페이드용
    [SerializeField] GameObject panelRoot;
    [SerializeField] Text titleText;
    [SerializeField] Text bodyText;
    [SerializeField] Text closeHint;
    Font koreanFont;   // 런타임 동적 OS 폰트 — 직렬화 안 함(Instantiate 후 재바인딩)

    // 설정
    const int   SortingOrder   = 110;   // DialogueUI(100)·Narration(90) 위
    const float PaperWidth      = 760f;  // 세로 종이 느낌
    const float PaperHeight     = 1000f;
    const float FadeTime        = 0.12f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        koreanFont = LoadKoreanFont();
        if (!IsGenerated) GenerateUI();   // 폴백: 프리팹 없이 코드로 생성
        else ApplyFonts();                // 프리팹 인스턴스: 동적 폰트 재바인딩
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>NoteUI 인스턴스 보장 — Systems 씬에 없으면 런타임 자동 생성. 첫 쪽지 읽을 때 호출.</summary>
    public static NoteUI Ensure()
    {
        if (Instance == null)
        {
            // 프리팹 우선(Instantiate가 Awake로 Instance 세팅), 없으면 코드 생성 폴백.
            var prefab = Resources.Load<GameObject>("UI/NoteUI");
            GameObject go = prefab != null ? Instantiate(prefab) : new GameObject("[NoteUI]");
            go.name = "[NoteUI]";
            if (prefab == null) go.AddComponent<NoteUI>();
            DontDestroyOnLoad(go);
        }
        return Instance;
    }

    // ─────────────────────────────────────────────────────────────────
    // 공개 API
    // ─────────────────────────────────────────────────────────────────

    /// <summary>쪽지 내용을 전체 화면으로 표시.</summary>
    public void Show(string content, string title = null)
    {
        if (!IsGenerated) GenerateUI();

        titleText.text = string.IsNullOrEmpty(title) ? "쪽지" : title;
        bodyText.text  = content ?? "";

        isShowing = true;
        openFrame = Time.frameCount;
        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        panelRoot.SetActive(true);

        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(Fade(0f, 1f));
    }

    public void Close()
    {
        if (!isShowing) return;
        isShowing = false;
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(Fade(group != null ? group.alpha : 1f, 0f, deactivateAtEnd: true));
    }

    void Update()
    {
        if (!isShowing) return;
        if (Time.frameCount == openFrame) return;   // 연 프레임의 키 입력 무시

        if (GameInput.GetKeyDown(KeyCode.Escape) || GameInput.GetKeyDown(KeyCode.E) ||
            GameInput.GetKeyDown(KeyCode.Space)  || GameInput.GetMouseButtonDown(0))
        {
            Close();
        }
    }

    IEnumerator Fade(float from, float to, bool deactivateAtEnd = false)
    {
        if (group == null) { if (deactivateAtEnd) panelRoot.SetActive(false); yield break; }
        float t = 0f;
        group.alpha = from;
        while (t < FadeTime)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / FadeTime);
            yield return null;
        }
        group.alpha = to;
        if (deactivateAtEnd) panelRoot.SetActive(false);
        fadeCo = null;
    }

    // ─────────────────────────────────────────────────────────────────
    // UI 생성 (프로시저럴)
    // ─────────────────────────────────────────────────────────────────

    public void GenerateUI()
    {
        var canvasGO = new GameObject("NoteUI_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        UITheme.ConfigureCanvasScale(scaler);
        canvasGO.AddComponent<GraphicRaycaster>();

        // 페이드 + 입력 차단(레이캐스트)용 그룹 = panelRoot.
        panelRoot = new GameObject("NotePanelRoot");
        panelRoot.transform.SetParent(canvas.transform, false);
        var rootRT = panelRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero; rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero; rootRT.offsetMax = Vector2.zero;
        group = panelRoot.AddComponent<CanvasGroup>();

        // 어두운 전체 배경(클릭 차단).
        var dim = new GameObject("Dim");
        dim.transform.SetParent(panelRoot.transform, false);
        var dimRT = dim.AddComponent<RectTransform>();
        dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = Vector2.zero; dimRT.offsetMax = Vector2.zero;
        var dimImg = dim.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.82f);
        dimImg.raycastTarget = true;

        // 가운데 '종이' 패널.
        var paper = new GameObject("Paper");
        paper.transform.SetParent(panelRoot.transform, false);
        var paperRT = paper.AddComponent<RectTransform>();
        paperRT.anchorMin = new Vector2(0.5f, 0.5f);
        paperRT.anchorMax = new Vector2(0.5f, 0.5f);
        paperRT.pivot     = new Vector2(0.5f, 0.5f);
        paperRT.sizeDelta = new Vector2(PaperWidth, PaperHeight);
        paperRT.anchoredPosition = Vector2.zero;
        var paperImg = paper.AddComponent<Image>();
        paperImg.color = new Color(0.93f, 0.90f, 0.78f, 1f);   // 종이색(크림)

        // 종이 안쪽 테두리(살짝 어두운 액자).
        var inner = new GameObject("InnerBorder");
        inner.transform.SetParent(paper.transform, false);
        var innerRT = inner.AddComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero; innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(18, 18); innerRT.offsetMax = new Vector2(-18, -18);
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = new Color(0.88f, 0.84f, 0.70f, 1f);

        // 제목.
        titleText = MakeText(paper.transform, "Title", 36, FontStyle.Bold,
            new Color(0.15f, 0.12f, 0.08f), TextAnchor.UpperCenter);
        var tRT = (RectTransform)titleText.transform;
        tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.offsetMin = new Vector2(50, -110); tRT.offsetMax = new Vector2(-50, -45);

        // 제목 밑줄.
        var rule = new GameObject("Rule");
        rule.transform.SetParent(paper.transform, false);
        var ruleRT = rule.AddComponent<RectTransform>();
        ruleRT.anchorMin = new Vector2(0, 1); ruleRT.anchorMax = new Vector2(1, 1);
        ruleRT.pivot = new Vector2(0.5f, 1f);
        ruleRT.sizeDelta = new Vector2(-100, 2);
        ruleRT.anchoredPosition = new Vector2(0, -120);
        rule.AddComponent<Image>().color = new Color(0.5f, 0.42f, 0.28f, 1f);

        // 본문.
        bodyText = MakeText(paper.transform, "Body", 30, FontStyle.Normal,
            new Color(0.12f, 0.10f, 0.07f), TextAnchor.UpperLeft);
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.lineSpacing = 1.15f;
        var bRT = (RectTransform)bodyText.transform;
        bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
        bRT.offsetMin = new Vector2(55, 70); bRT.offsetMax = new Vector2(-55, -140);

        // 닫기 힌트.
        closeHint = MakeText(panelRoot.transform, "CloseHint", 22, FontStyle.Italic,
            new Color(0.85f, 0.85f, 0.85f), TextAnchor.LowerCenter);
        closeHint.text = "[E] / [Esc] 닫기";
        var hRT = (RectTransform)closeHint.transform;
        hRT.anchorMin = new Vector2(0.5f, 0f); hRT.anchorMax = new Vector2(0.5f, 0f);
        hRT.pivot = new Vector2(0.5f, 0f);
        hRT.sizeDelta = new Vector2(600, 40);
        hRT.anchoredPosition = new Vector2(0, 36);

        panelRoot.SetActive(false);
    }

    /// <summary>프리팹 인스턴스화 시 동적 OS 폰트를 직렬화된 Text 참조에 재바인딩.</summary>
    void ApplyFonts()
    {
        var f = koreanFont != null ? koreanFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (titleText) titleText.font = f;
        if (bodyText)  bodyText.font  = f;
        if (closeHint) closeHint.font = f;
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — GenerateUI를 1회 실행해 프리팹화할 계층을 만든다.</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        koreanFont = LoadKoreanFont();
        GenerateUI();
    }
#endif

    Text MakeText(Transform parent, string name, int size, FontStyle style, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.font = koreanFont != null ? koreanFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = anchor;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    /// <summary>한글 렌더 가능한 동적 OS 폰트(맑은 고딕 등). 빌트인 LegacyRuntime은 ASCII만이라 폴백.</summary>
    static Font LoadKoreanFont()
    {
        var f = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Dotum", "Batang", "Arial" }, 32);
        return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
