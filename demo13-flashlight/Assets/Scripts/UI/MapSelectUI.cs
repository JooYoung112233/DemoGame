using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 지역 선택 UI (전체화면 Canvas/uGUI).
/// 지도판(MapBoard) 상호작용 시 표시.
/// 디자인: 양식화된 일러스트 도시 맵(그레이박스: 어두운 도시 실루엣) 위에
/// 지역/랜드마크 노드 마커 → 마커 선택 시 우측 정보 패널 + 출전.
/// (docs/world-map.md §9-A · 지역 데이터: WorldRegionCatalog)
///
/// Editor-time 사용:
///   Inspector에서 Generate UI / Clear UI 로 Canvas 생성/삭제.
///   런타임: Awake()에서 미생성 시 자동 GenerateUI() + BindEvents().
///
/// 주의: Systems 씬에서 UIManager 자식으로 살고 [SerializeField] 참조를 가진다.
///       Systems 프리팹에 구버전 캔버스가 직렬화돼 있으면 인스펙터에서
///       Clear → Generate 로 재생성해야 한다.
/// </summary>
public class MapSelectUI : MonoBehaviour
{
    public bool IsShowing => isShowing;
    public bool IsGenerated => canvas != null;

    bool isShowing;

    [SerializeField] Canvas canvas;
    [SerializeField] GameObject panelRoot;
    [SerializeField] Image dimBg;
    [SerializeField] Text titleText;
    [SerializeField] Text selectedInfoText;
    [SerializeField] Button confirmBtn;
    [SerializeField] Button cancelBtn;
    [SerializeField] Text confirmText;
    [SerializeField] Button[] regionButtons;   // 맵 위 노드 마커 버튼
    [SerializeField] Text[] regionBtnTexts;     // 마커 하단 라벨(지역명 + 시간)
    [SerializeField] Text infoTimeText;         // 우측 정보 패널 시간대 라인

    static WorldRegionCatalog.RegionDefinition[] Regions => WorldRegionCatalog.All;

    int selectedRegion = -1;

    // ── 한글 폰트 로더 ──────────────────────────
    static Font _kr;
    static Font KR
    {
        get
        {
            if (_kr == null)
                _kr = Font.CreateDynamicFontFromOSFont(
                    new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Dotum", "Arial" }, 16);
            return _kr != null ? _kr : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    // ── 색 팔레트(웜/올리브 전술 톤 — UITheme 공유) ────────────────
    static readonly Color DimBg = UITheme.Backdrop;
    static readonly Color Panel = UITheme.Panel;
    static readonly Color Panel2 = UITheme.PanelAlt;
    static readonly Color Gold = UITheme.Gold;
    static readonly Color BodyText = UITheme.TextBright;
    static readonly Color FaintText = UITheme.TextMuted;
    static readonly Color Line = UITheme.Divider;
    static readonly Color BtnCol = UITheme.Cell;
    static readonly Color Danger = UITheme.Danger;
    static readonly Color Good = UITheme.Positive;
    static readonly Color Warn = UITheme.Negative;

    const float HeaderH = 64f;
    const float InfoPanelW = 380f;

    void Awake()
    {
        if (!IsGenerated) GenerateUI();   // 폴백: 프리팹 없이 코드로 생성
        else ApplyFonts();                // 프리팹 인스턴스: 동적 폰트 재바인딩
        BindEvents();
    }

    // ══════════════════════════════════════
    // UI 생성 / 바인딩 / 제거
    // ══════════════════════════════════════

    public void GenerateUI()
    {
        int count = Regions.Length;

        var canvasGO = new GameObject("MapSelect_Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        var canvasRT = canvasGO.GetComponent<RectTransform>();

        // ── 루트: 전체화면 딤 배경 ──
        panelRoot = new GameObject("PanelRoot");
        panelRoot.transform.SetParent(canvasRT, false);
        var rootRT = panelRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        dimBg = panelRoot.AddComponent<Image>();
        dimBg.color = DimBg;

        // ── 상단 헤더바 ──
        BuildHeader(rootRT);

        // ── 본문 영역(헤더 아래 ~ 화면 하단) ──
        var body = CreateStretch(panelRoot.transform, "Body");
        body.offsetMin = new Vector2(40, 40);     // 좌·하 여백
        body.offsetMax = new Vector2(-40, -(HeaderH + 16)); // 우·상 여백(헤더 아래)

        // 우측 정보 패널
        BuildInfoPanel(body);

        // 좌측 일러스트 맵 영역(우측 패널 폭 + 간격만큼 비움)
        var mapArea = CreateStretch(body, "MapArea");
        mapArea.offsetMin = Vector2.zero;
        mapArea.offsetMax = new Vector2(-(InfoPanelW + 24), 0);
        BuildMapBackdrop(mapArea);

        // ── 노드 마커 배치 ──
        regionButtons = new Button[count];
        regionBtnTexts = new Text[count];

        for (int i = 0; i < count; i++)
            BuildRegionMarker(mapArea, i);

        panelRoot.SetActive(false);
    }

    void BuildHeader(Transform parent)
    {
        var header = CreateRect(parent, "Header", new Vector2(0, 1), new Vector2(1, 1), Vector2.zero);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(0, HeaderH);
        var hbg = header.gameObject.AddComponent<Image>();
        hbg.color = Panel2;

        titleText = MakeText(header, "Title", "출전 지역 — 다녀올게",
            new Vector2(28, 0), new Vector2(900, HeaderH), 26, Gold, TextAnchor.MiddleLeft);
        var ttRT = titleText.rectTransform;
        ttRT.anchorMin = new Vector2(0, 0);
        ttRT.anchorMax = new Vector2(0, 1);
        ttRT.pivot = new Vector2(0, 0.5f);
        ttRT.anchoredPosition = new Vector2(28, 0);
        titleText.fontStyle = FontStyle.Bold;

        // 닫기 ✕ 버튼 (우측 끝, 48x40)
        var closeRT = CreateRect(header, "CloseBtn", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(48, 40));
        closeRT.pivot = new Vector2(1, 0.5f);
        closeRT.anchoredPosition = new Vector2(-12, 0);
        var closeImg = closeRT.gameObject.AddComponent<Image>();
        closeImg.color = Danger;
        cancelBtn = closeRT.gameObject.AddComponent<Button>();
        cancelBtn.targetGraphic = closeImg;
        var xColors = cancelBtn.colors;
        xColors.highlightedColor = new Color(0.65f, 0.28f, 0.28f);
        xColors.pressedColor = new Color(0.35f, 0.14f, 0.14f);
        cancelBtn.colors = xColors;
        MakeChildText(closeRT, "✕", 22, BodyText);

        // 헤더 아래 1px 구분선
        var lineRT = CreateRect(parent, "HeaderLine", new Vector2(0, 1), new Vector2(1, 1), Vector2.zero);
        lineRT.pivot = new Vector2(0.5f, 1f);
        lineRT.anchoredPosition = new Vector2(0, -HeaderH);
        lineRT.sizeDelta = new Vector2(0, 1);
        lineRT.gameObject.AddComponent<Image>().color = Line;
    }

    void BuildMapBackdrop(Transform mapArea)
    {
        // 맵 배경 패널
        var bg = mapArea.gameObject.AddComponent<Image>();
        bg.color = Panel2;

        // 어두운 도시 실루엣(그레이박스) — 하단에 빌딩 블록 몇 개
        var skyline = CreateStretch(mapArea, "Skyline");
        skyline.anchorMin = new Vector2(0, 0);
        skyline.anchorMax = new Vector2(1, 0);
        skyline.pivot = new Vector2(0.5f, 0);
        skyline.anchoredPosition = Vector2.zero;
        skyline.sizeDelta = new Vector2(0, 220);

        // 빌딩 실루엣 블록(절차적)
        float[] widths = { 70, 110, 60, 140, 90, 120, 80, 100, 130, 70 };
        float[] heights = { 90, 150, 70, 200, 120, 170, 100, 140, 190, 80 };
        float x = 12f;
        for (int i = 0; i < widths.Length; i++)
        {
            var b = CreateRect(skyline, $"Bldg{i}", new Vector2(0, 0), new Vector2(0, 0), new Vector2(widths[i], heights[i]));
            b.pivot = new Vector2(0, 0);
            b.anchoredPosition = new Vector2(x, 0);
            var img = b.gameObject.AddComponent<Image>();
            float shade = 0.10f + (i % 3) * 0.025f;
            img.color = new Color(shade + 0.03f, shade + 0.01f, shade, 0.9f);
            x += widths[i] + 18f;
        }

        // 안내 라벨
        var hint = MakeText(mapArea, "MapHint", "도시 구역 — 마커를 선택해 정찰/출전 지역을 고르세요",
            new Vector2(16, -12), new Vector2(640, 26), 14, FaintText, TextAnchor.UpperLeft);
        hint.fontStyle = FontStyle.Italic;
    }

    void BuildRegionMarker(Transform mapArea, int i)
    {
        var r = Regions[i];

        // 노드 위치(절차적 양식화 배치) — 좌상~우하로 흩어진 도시 노드
        Vector2[] anchors =
        {
            new Vector2(0.16f, 0.66f),
            new Vector2(0.38f, 0.42f),
            new Vector2(0.56f, 0.70f),
            new Vector2(0.72f, 0.40f),
            new Vector2(0.86f, 0.62f),
        };
        Vector2 a = i < anchors.Length ? anchors[i] : new Vector2(0.5f, 0.5f);

        var btnRT = CreateRect(mapArea, $"Marker_{r.regionId}", a, a, new Vector2(56, 56));
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = Vector2.zero;

        var btnImg = btnRT.gameObject.AddComponent<Image>();
        btnImg.color = WorldRegionCatalog.GetButtonColor(r, false);

        var btn = btnRT.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        if (!r.IsPlayable)
        {
            btn.interactable = false;
        }
        else
        {
            var colors = btn.colors;
            colors.highlightedColor = WorldRegionCatalog.GetButtonColor(r, true);
            colors.pressedColor = WorldRegionCatalog.GetButtonColor(r, true) * 0.85f;
            btn.colors = colors;
        }

        // 마커 아이콘(잠금/난이도)
        var icon = MakeChildText(btnRT, r.IsPlayable ? "◈" : "🔒", 22,
            r.IsPlayable ? BodyText : UITheme.TextDim);
        icon.name = "Icon";

        // 마커 하단 지역명 라벨(+ 시간; UpdateRegionTimeDisplay가 갱신)
        var label = MakeText(btnRT, "Label", FormatButtonLabel(r),
            new Vector2(0, 0), new Vector2(170, 36), 13,
            r.IsPlayable ? BodyText : FaintText, TextAnchor.UpperCenter);
        var labelRT = label.rectTransform;
        labelRT.anchorMin = new Vector2(0.5f, 0);
        labelRT.anchorMax = new Vector2(0.5f, 0);
        labelRT.pivot = new Vector2(0.5f, 1f);
        labelRT.anchoredPosition = new Vector2(0, -6);
        label.fontStyle = FontStyle.Bold;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;

        regionButtons[i] = btn;
        regionBtnTexts[i] = label;
    }

    void BuildInfoPanel(Transform body)
    {
        var infoPanelRT = CreateRect(body, "InfoPanel", new Vector2(1, 0), new Vector2(1, 1), Vector2.zero);
        infoPanelRT.pivot = new Vector2(1, 0.5f);
        infoPanelRT.anchorMin = new Vector2(1, 0);
        infoPanelRT.anchorMax = new Vector2(1, 1);
        infoPanelRT.offsetMin = new Vector2(-InfoPanelW, 0);
        infoPanelRT.offsetMax = new Vector2(0, 0);
        var bg = infoPanelRT.gameObject.AddComponent<Image>();
        bg.color = Panel;

        // 제목 라인(지역명) — 상단 좌우 스트레치, 높이 34
        var titleRT = CreateRect(infoPanelRT, "InfoTitle", new Vector2(0, 1), new Vector2(1, 1), Vector2.zero);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.offsetMin = new Vector2(20, -54);  // y: 1 - 20(top margin) - 34(height)
        titleRT.offsetMax = new Vector2(-20, -20);
        var infoTitle = titleRT.gameObject.AddComponent<Text>();
        ApplyTextStyle(infoTitle, 20, Gold, TextAnchor.UpperLeft);
        infoTitle.fontStyle = FontStyle.Bold;
        infoTitle.text = "지역 정보";
        infoTitle.name = "InfoTitleText";

        // 시간대 라인
        infoTimeText = MakeText(infoPanelRT, "InfoTime", "",
            new Vector2(20, -58), new Vector2(InfoPanelW - 40, 24), 14, FaintText, TextAnchor.UpperLeft);

        MakeLine(infoPanelRT, -88, InfoPanelW - 40);

        // 설명 본문(스크롤 없이 길게)
        selectedInfoText = MakeText(infoPanelRT, "InfoText", "지역을 선택하세요",
            new Vector2(20, -100), new Vector2(InfoPanelW - 40, 600), 15, BodyText, TextAnchor.UpperLeft);

        // 하단 출전 버튼 (좌우 스트레치, 하단 기준 — offset이 rect를 완전 정의)
        var confirmRT = CreateRect(infoPanelRT, "ConfirmBtn", new Vector2(0, 0), new Vector2(1, 0), Vector2.zero);
        confirmRT.pivot = new Vector2(0.5f, 0);
        confirmRT.offsetMin = new Vector2(20, 72);
        confirmRT.offsetMax = new Vector2(-20, 72 + 52);
        var confirmImg = confirmRT.gameObject.AddComponent<Image>();
        confirmImg.color = new Color(Good.r * 0.35f, Good.g * 0.35f, Good.b * 0.35f, 0.95f);
        confirmBtn = confirmRT.gameObject.AddComponent<Button>();
        confirmBtn.targetGraphic = confirmImg;
        confirmBtn.interactable = false;
        var cColors = confirmBtn.colors;
        cColors.highlightedColor = new Color(Good.r * 0.5f, Good.g * 0.5f, Good.b * 0.5f, 1f);
        cColors.pressedColor = new Color(Good.r * 0.28f, Good.g * 0.28f, Good.b * 0.28f, 1f);
        cColors.disabledColor = new Color(0.22f, 0.21f, 0.18f, 0.9f);
        confirmBtn.colors = cColors;
        confirmText = MakeChildText(confirmRT, "출전", 18, BodyText);
        confirmText.fontStyle = FontStyle.Bold;

        // 하단 취소 버튼
        var cancel2RT = CreateRect(infoPanelRT, "CancelBtn2", new Vector2(0, 0), new Vector2(1, 0), Vector2.zero);
        cancel2RT.pivot = new Vector2(0.5f, 0);
        cancel2RT.offsetMin = new Vector2(20, 16);
        cancel2RT.offsetMax = new Vector2(-20, 16 + 44);
        var cancel2Img = cancel2RT.gameObject.AddComponent<Image>();
        cancel2Img.color = BtnCol;
        var cancel2Btn = cancel2RT.gameObject.AddComponent<Button>();
        cancel2Btn.targetGraphic = cancel2Img;
        var c2Colors = cancel2Btn.colors;
        c2Colors.highlightedColor = UITheme.CellHover;
        c2Colors.pressedColor = UITheme.CellPressed;
        cancel2Btn.colors = c2Colors;
        MakeChildText(cancel2RT, "취소  [ESC]", 15, FaintText);
        cancel2Btn.onClick.AddListener(Hide);
    }

    /// <summary>
    /// 정적 버튼 onClick 재부착. onClick 리스너는 프리팹에 직렬화되지 않으므로
    /// (코드 생성 / 프리팹 인스턴스) 양쪽 경로에서 Awake가 호출한다.
    /// 멱등(idempotent): GenerateUI 직후·재호출에도 중복되지 않도록 RemoveAllListeners 후 AddListener.
    /// </summary>
    public void BindEvents()
    {
        if (confirmBtn != null)
        {
            confirmBtn.onClick.RemoveAllListeners();
            confirmBtn.onClick.AddListener(OnConfirm);
        }

        if (cancelBtn != null)
        {
            cancelBtn.onClick.RemoveAllListeners();
            cancelBtn.onClick.AddListener(Hide);
        }

        if (regionButtons != null)
        {
            for (int i = 0; i < regionButtons.Length; i++)
            {
                if (regionButtons[i] == null) continue;
                regionButtons[i].onClick.RemoveAllListeners();
                if (!regionButtons[i].interactable) continue;
                int idx = i;
                regionButtons[i].onClick.AddListener(() => SelectRegion(idx));
            }
        }
    }

    /// <summary>프리팹 인스턴스화 시 동적 OS 폰트를 직렬화된 Text 참조에 재바인딩.</summary>
    void ApplyFonts()
    {
        var f = KR;
        if (titleText)        titleText.font = f;
        if (selectedInfoText) selectedInfoText.font = f;
        if (confirmText)      confirmText.font = f;
        if (infoTimeText)     infoTimeText.font = f;
        if (regionBtnTexts != null)
            foreach (var t in regionBtnTexts)
                if (t) t.font = f;
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
        var child = transform.Find("MapSelect_Canvas");
        if (child != null)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        canvas = null;
        panelRoot = null;
        dimBg = null;
        titleText = null;
        selectedInfoText = null;
        confirmBtn = null;
        cancelBtn = null;
        confirmText = null;
        regionButtons = null;
        regionBtnTexts = null;
        infoTimeText = null;
    }

    // ══════════════════════════════════════
    // Show / Hide
    // ══════════════════════════════════════

    public void Show()
    {
        isShowing = true;
        selectedRegion = -1;
        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        if (panelRoot != null)
            panelRoot.SetActive(true);
        UpdateSelection();
    }

    public void Hide()
    {
        isShowing = false;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void Update()
    {
        if (!isShowing) return;

        if (GameInput.GetKeyDown(KeyCode.Escape))
            Hide();

        UpdateRegionTimeDisplay();
    }

    // ══════════════════════════════════════
    // 선택 / 확인
    // ══════════════════════════════════════

    static string FormatButtonLabel(in WorldRegionCatalog.RegionDefinition r)
    {
        return r.displayName;
    }

    void SelectRegion(int idx)
    {
        selectedRegion = idx;
        UpdateSelection();
    }

    void UpdateSelection()
    {
        if (regionButtons == null) return;

        for (int i = 0; i < regionButtons.Length; i++)
        {
            if (regionButtons[i] == null) continue;
            var r = Regions[i];
            var img = regionButtons[i].GetComponent<Image>();
            if (img != null)
                img.color = WorldRegionCatalog.GetButtonColor(r, i == selectedRegion);
        }

        if (selectedRegion >= 0 && selectedRegion < Regions.Length)
        {
            RenderInfoPanel(Regions[selectedRegion]);
            confirmBtn.interactable = Regions[selectedRegion].IsPlayable;
        }
        else
        {
            if (infoTimeText != null) infoTimeText.text = "";
            if (selectedInfoText != null)
                selectedInfoText.text = "<color=#8A8170>맵에서 지역 마커를 선택하세요.</color>";
            if (confirmBtn != null) confirmBtn.interactable = false;
        }
    }

    void RenderInfoPanel(in WorldRegionCatalog.RegionDefinition r)
    {
        string timeInfo = GetRegionTimeText(r.regionId);
        string desc = WorldRegionCatalog.BuildDescription(r);

        if (!r.IsPlayable)
        {
            string lockMsg = StoryLocale.Instance != null
                ? StoryLocale.Instance.Get("UI_MAP_LOCKED")
                : "아직 갈 수 없습니다.";
            desc = $"<color=#FF8C74>🔒 {lockMsg}</color>\n\n{desc}";
        }

        if (infoTimeText != null)
            infoTimeText.text = timeInfo;

        if (selectedInfoText != null)
            selectedInfoText.text =
                $"<b><color=#{ColorToHex(r.accentColor)}>{r.displayName}</color></b>\n{desc}";
    }

    static string ColorToHex(Color c)
    {
        return ColorUtility.ToHtmlStringRGB(new Color(
            Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b)));
    }

    void OnConfirm()
    {
        if (selectedRegion < 0 || selectedRegion >= Regions.Length) return;
        var r = Regions[selectedRegion];
        if (!r.IsPlayable) return;

        Hide();

        // 밤 레이드인지 체크
        bool isNight = false;
        if (RegionTimeManager.Instance != null)
        {
            var rt = RegionTimeManager.Instance.GetRegion(r.regionId);
            if (rt != null) isNight = rt.isNight;
        }

        // 밤 첫 출전 시 스토리 씬 재생 후 전환
        if (isNight && StoryTriggerManager.Instance != null)
        {
            StoryTriggerManager.Instance.OnNightGateSelected(() =>
            {
                DoTransition(r);
            });
        }
        else
        {
            DoTransition(r);
        }
    }

    void DoTransition(WorldRegionCatalog.RegionDefinition r)
    {
        if (RegionTimeManager.Instance != null)
            RegionTimeManager.Instance.ActiveRegionId = r.regionId;

        Time.timeScale = 1f;

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(r.sceneName, r.spawnId);
    }

    #region 시간 표시

    void UpdateRegionTimeDisplay()
    {
        if (regionBtnTexts == null || RegionTimeManager.Instance == null) return;

        for (int i = 0; i < Regions.Length; i++)
        {
            if (regionBtnTexts[i] == null) continue;
            var r = Regions[i];

            var rt = RegionTimeManager.Instance.GetRegion(r.regionId);
            if (rt == null)
            {
                regionBtnTexts[i].text = FormatButtonLabel(r);
                continue;
            }

            string icon = rt.isNight ? "●" : "○";   // ● 짙은 현상 / ○ 안전 구간
            float remaining = rt.isNight
                ? RegionTimeManager.Instance.NightDuration - rt.elapsed
                : RegionTimeManager.Instance.DayDuration - rt.elapsed;
            int min = (int)(remaining / 60);
            int sec = (int)(remaining % 60);

            regionBtnTexts[i].text = $"{FormatButtonLabel(r)}\n{icon} {min}:{sec:D2}";
        }

        // 선택된 지역의 정보 패널 시간 라인 갱신
        if (selectedRegion >= 0 && selectedRegion < Regions.Length && infoTimeText != null)
            infoTimeText.text = GetRegionTimeText(Regions[selectedRegion].regionId);
    }

    string GetRegionTimeText(string regionId)
    {
        if (string.IsNullOrEmpty(regionId) || RegionTimeManager.Instance == null)
            return "";

        var rt = RegionTimeManager.Instance.GetRegion(regionId);
        if (rt == null) return "";

        string phase = rt.isNight ? "● 짙은 현상" : "○ 안전 구간";
        float remaining = rt.isNight
            ? RegionTimeManager.Instance.NightDuration - rt.elapsed
            : RegionTimeManager.Instance.DayDuration - rt.elapsed;
        int min = (int)(remaining / 60);
        int sec = (int)(remaining % 60);
        string nextPhase = rt.isNight ? "현상 걷힘" : "현상 짙어짐";

        return $"<color={(rt.isNight ? "#8899FF" : "#FFE844")}>{phase}</color>  |  {nextPhase}까지 {min}:{sec:D2}";
    }

    #endregion

    #region 유틸

    RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = size;
        return rt;
    }

    /// <summary>부모를 꽉 채우는 RectTransform 생성(offset은 호출측 조정).</summary>
    RectTransform CreateStretch(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    void ApplyTextStyle(Text txt, int fontSize, Color color, TextAnchor align)
    {
        txt.font = KR;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = align;
        txt.supportRichText = true;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
    }

    Text MakeText(Transform parent, string name, string content,
        Vector2 pos, Vector2 size, int fontSize, Color color, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var txt = go.AddComponent<Text>();
        ApplyTextStyle(txt, fontSize, color, align);
        txt.text = content;
        return txt;
    }

    Text MakeChildText(Transform parent, string content, int fontSize, Color color)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var txt = go.AddComponent<Text>();
        txt.font = KR;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.color = color;
        txt.text = content;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.supportRichText = true;
        return txt;
    }

    void MakeLine(Transform parent, float yPos, float width)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, yPos);
        rt.sizeDelta = new Vector2(width, 1);
        go.AddComponent<Image>().color = Line;
    }

    #endregion
}
