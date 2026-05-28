using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 지역 선택 UI (Canvas/uGUI).
/// 지도판(MapBoard) 상호작용 시 표시.
/// 지역 데이터: WorldRegionCatalog (docs/world-map.md).
///
/// Editor-time 사용:
///   Inspector에서 Generate UI / Clear UI 버튼으로 Canvas 생성/삭제.
///   런타임: Awake()에서 미생성 시 자동 GenerateUI() + BindEvents().
/// </summary>
public class MapSelectUI : MonoBehaviour
{
    public bool IsShowing => isShowing;
    public bool IsGenerated => canvas != null;

    const float BtnHeight = 38f;
    const float BtnSpacing = 42f;
    const float ListTop = -58f;
    const float ListLeft = 16f;
    const float BtnWidth = 248f;

    bool isShowing;

    [SerializeField] Canvas canvas;
    [SerializeField] GameObject panelRoot;
    [SerializeField] Image dimBg;
    [SerializeField] Text titleText;
    [SerializeField] Text selectedInfoText;
    [SerializeField] Button confirmBtn;
    [SerializeField] Button cancelBtn;
    [SerializeField] Text confirmText;
    [SerializeField] Button[] regionButtons;
    [SerializeField] Text[] regionBtnTexts;

    static WorldRegionCatalog.RegionDefinition[] Regions => WorldRegionCatalog.All;

    int selectedRegion = -1;

    void Awake()
    {
        if (!IsGenerated) GenerateUI();
        BindEvents();
    }

    // ══════════════════════════════════════
    // UI 생성 / 바인딩 / 제거
    // ══════════════════════════════════════

    public void GenerateUI()
    {
        int count = Regions.Length;
        float listHeight = count * BtnSpacing + 20f;
        float panelH = Mathf.Max(480f, listHeight + 120f);

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

        panelRoot = new GameObject("PanelRoot");
        panelRoot.transform.SetParent(canvasRT, false);
        var rootRT = panelRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        dimBg = panelRoot.AddComponent<Image>();
        dimBg.color = new Color(0, 0, 0, 0.75f);

        var panel = CreateRect(panelRoot.transform, "CenterPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(620, panelH));
        var panelImg = panel.gameObject.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        titleText = MakeText(panel, "Title", "◈ 밤의 도시 — 출전 지역 ◈",
            new Vector2(0, -12), new Vector2(580, 32), 20, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
        titleText.fontStyle = FontStyle.Bold;

        MakeText(panel, "Subtitle", "지구를 선택하세요 (낮/밤은 지역마다 독립 진행)",
            new Vector2(0, -38), new Vector2(580, 22), 12, new Color(0.55f, 0.6f, 0.7f), TextAnchor.MiddleCenter);

        MakeLine(panel, -52);

        regionButtons = new Button[count];
        regionBtnTexts = new Text[count];
        for (int i = 0; i < count; i++)
        {
            var r = Regions[i];

            var btnGO = new GameObject($"Region_{r.regionId}");
            btnGO.transform.SetParent(panel, false);

            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0, 1);
            btnRT.anchorMax = new Vector2(0, 1);
            btnRT.pivot = new Vector2(0, 1);
            btnRT.anchoredPosition = new Vector2(ListLeft, ListTop - i * BtnSpacing);
            btnRT.sizeDelta = new Vector2(BtnWidth, BtnHeight);

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = WorldRegionCatalog.GetButtonColor(r, false);

            var btn = btnGO.AddComponent<Button>();
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

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(8, 0);
            txtRT.offsetMax = new Vector2(-4, 0);

            var txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.color = r.IsPlayable ? Color.white : new Color(0.45f, 0.45f, 0.5f);
            txt.text = FormatButtonLabel(r);
            txt.alignment = TextAnchor.MiddleLeft;

            regionButtons[i] = btn;
            regionBtnTexts[i] = txt;
        }

        float infoH = Mathf.Min(panelH - 80f, 340f);
        var infoPanel = CreateRect(panel, "InfoPanel",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(300, infoH));
        infoPanel.pivot = new Vector2(1, 1);
        infoPanel.anchoredPosition = new Vector2(-14, -56);
        var infoBg = infoPanel.gameObject.AddComponent<Image>();
        infoBg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

        selectedInfoText = MakeText(infoPanel, "InfoText", "지역을 선택하세요",
            new Vector2(10, -8), new Vector2(280, infoH - 16), 13, new Color(0.8f, 0.85f, 0.9f), TextAnchor.UpperLeft);

        var confirmGO = new GameObject("ConfirmBtn");
        confirmGO.transform.SetParent(panel, false);
        var confirmRT = confirmGO.AddComponent<RectTransform>();
        confirmRT.anchorMin = new Vector2(1, 0);
        confirmRT.anchorMax = new Vector2(1, 0);
        confirmRT.pivot = new Vector2(1, 0);
        confirmRT.anchoredPosition = new Vector2(-14, 16);
        confirmRT.sizeDelta = new Vector2(140, 38);

        var confirmImg = confirmGO.AddComponent<Image>();
        confirmImg.color = new Color(0.15f, 0.5f, 0.2f);

        confirmBtn = confirmGO.AddComponent<Button>();
        confirmBtn.targetGraphic = confirmImg;
        confirmBtn.interactable = false;

        var cColors = confirmBtn.colors;
        cColors.highlightedColor = new Color(0.2f, 0.7f, 0.3f);
        cColors.pressedColor = new Color(0.1f, 0.4f, 0.15f);
        cColors.disabledColor = new Color(0.2f, 0.2f, 0.2f);
        confirmBtn.colors = cColors;

        confirmText = MakeChildText(confirmGO.transform, "출전", 16, Color.white);

        var cancelGO = new GameObject("CancelBtn");
        cancelGO.transform.SetParent(panel, false);
        var cancelRT = cancelGO.AddComponent<RectTransform>();
        cancelRT.anchorMin = new Vector2(0, 0);
        cancelRT.anchorMax = new Vector2(0, 0);
        cancelRT.pivot = new Vector2(0, 0);
        cancelRT.anchoredPosition = new Vector2(16, 16);
        cancelRT.sizeDelta = new Vector2(100, 38);

        var cancelImg = cancelGO.AddComponent<Image>();
        cancelImg.color = new Color(0.4f, 0.15f, 0.15f);

        cancelBtn = cancelGO.AddComponent<Button>();
        cancelBtn.targetGraphic = cancelImg;

        var xColors = cancelBtn.colors;
        xColors.highlightedColor = new Color(0.6f, 0.2f, 0.2f);
        xColors.pressedColor = new Color(0.3f, 0.1f, 0.1f);
        cancelBtn.colors = xColors;

        MakeChildText(cancelGO.transform, "취소", 14, Color.white);

        MakeText(panel, "EscHint", "[ESC] 닫기",
            new Vector2(130, 18), new Vector2(200, 28), 12, new Color(0.5f, 0.5f, 0.5f), TextAnchor.MiddleLeft);

        panelRoot.SetActive(false);
    }

    public void BindEvents()
    {
        if (confirmBtn != null)
            confirmBtn.onClick.AddListener(OnConfirm);

        if (cancelBtn != null)
            cancelBtn.onClick.AddListener(Hide);

        if (regionButtons != null)
        {
            for (int i = 0; i < regionButtons.Length; i++)
            {
                if (regionButtons[i] == null) continue;
                if (!regionButtons[i].interactable) continue;
                int idx = i;
                regionButtons[i].onClick.AddListener(() => SelectRegion(idx));
            }
        }
    }

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
    }

    // ══════════════════════════════════════
    // Show / Hide
    // ══════════════════════════════════════

    public void Show()
    {
        isShowing = true;
        selectedRegion = -1;
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

        if (Input.GetKeyDown(KeyCode.Escape))
            Hide();

        UpdateRegionTimeDisplay();
    }

    // ══════════════════════════════════════
    // 선택 / 확인
    // ══════════════════════════════════════

    static string FormatButtonLabel(in WorldRegionCatalog.RegionDefinition r)
    {
        string lockMark = r.IsPlayable ? "" : "🔒 ";
        return $"{lockMark}{r.displayName}";
    }

    void SelectRegion(int idx)
    {
        selectedRegion = idx;
        UpdateSelection();
    }

    void UpdateSelection()
    {
        for (int i = 0; i < regionButtons.Length; i++)
        {
            if (regionButtons[i] == null) continue;
            var r = Regions[i];
            var img = regionButtons[i].GetComponent<Image>();
            img.color = WorldRegionCatalog.GetButtonColor(r, i == selectedRegion);
        }

        if (selectedRegion >= 0 && selectedRegion < Regions.Length)
        {
            var r = Regions[selectedRegion];
            string timeInfo = GetRegionTimeText(r.regionId);
            string desc = WorldRegionCatalog.BuildDescription(r);

            // 잠금 지역이면 잠금 안내 추가
            if (!r.IsPlayable)
            {
                string lockMsg = StoryLocale.Instance != null
                    ? StoryLocale.Instance.Get("UI_MAP_LOCKED")
                    : "아직 갈 수 없습니다.";
                desc = $"<color=#FF6666>🔒 {lockMsg}</color>\n\n{desc}";
            }

            selectedInfoText.text = $"<b><color=#{ColorToHex(r.accentColor)}>{r.displayName}</color></b>\n{timeInfo}\n\n{desc}";
            confirmBtn.interactable = r.IsPlayable;
        }
        else
        {
            selectedInfoText.text = "지역을 선택하세요";
            confirmBtn.interactable = false;
        }
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

            string icon = rt.isNight ? "☾" : "☀";
            float remaining = rt.isNight
                ? RegionTimeManager.Instance.NightDuration - rt.elapsed
                : RegionTimeManager.Instance.DayDuration - rt.elapsed;
            int min = (int)(remaining / 60);
            int sec = (int)(remaining % 60);

            regionBtnTexts[i].text = $"{FormatButtonLabel(r)}  {icon}{min}:{sec:D2}";
        }

        if (selectedRegion >= 0 && selectedRegion < Regions.Length)
        {
            var r = Regions[selectedRegion];
            string timeInfo = GetRegionTimeText(r.regionId);
            selectedInfoText.text = $"<b><color=#{ColorToHex(r.accentColor)}>{r.displayName}</color></b>\n{timeInfo}\n\n{WorldRegionCatalog.BuildDescription(r)}";
        }
    }

    string GetRegionTimeText(string regionId)
    {
        if (string.IsNullOrEmpty(regionId) || RegionTimeManager.Instance == null)
            return "";

        var rt = RegionTimeManager.Instance.GetRegion(regionId);
        if (rt == null) return "";

        string phase = rt.isNight ? "☾ 밤" : "☀ 낮";
        float remaining = rt.isNight
            ? RegionTimeManager.Instance.NightDuration - rt.elapsed
            : RegionTimeManager.Instance.DayDuration - rt.elapsed;
        int min = (int)(remaining / 60);
        int sec = (int)(remaining % 60);
        string nextPhase = rt.isNight ? "일출" : "일몰";

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
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.color = color;
        txt.text = content;
        txt.alignment = align;
        txt.supportRichText = true;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

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
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.color = color;
        txt.text = content;
        txt.alignment = TextAnchor.MiddleCenter;

        return txt;
    }

    void MakeLine(Transform parent, float yPos)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, yPos);
        rt.sizeDelta = new Vector2(580, 1);
        var img = go.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.2f);
    }

    #endregion
}
