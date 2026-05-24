using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 지역 선택 UI (Canvas/uGUI).
/// 지도판(MapBoard) 상호작용 시 표시.
/// 지역 선택 후 해당 씬으로 전환.
/// UIManager 자식으로 배치.
/// </summary>
public class MapSelectUI : MonoBehaviour
{
    public bool IsShowing => isShowing;

    bool isShowing;

    // uGUI
    Canvas canvas;
    GameObject panelRoot;
    Image dimBg;
    Text titleText;
    Button[] regionButtons;
    Text selectedInfoText;
    Button confirmBtn;
    Button cancelBtn;
    Text confirmText;

    // 지역 데이터 (더미)
    static readonly RegionInfo[] regions = new RegionInfo[]
    {
        new RegionInfo("폐상가 거리",   "InGameScene", "default", "street",      "난이도: ★☆☆\n파밍 위주의 초보자 지역.\n적 출현: 낮음"),
        new RegionInfo("붕괴 아파트",   "InGameScene", "default", "apartment",   "난이도: ★★☆\n밀폐 공간. 귀중품 다수.\n적 출현: 보통"),
        new RegionInfo("지하 상가",     "InGameScene", "default", "underground", "난이도: ★★★\n시야 제한. 고급 자원.\n적 출현: 높음"),
        new RegionInfo("???",          "",            "",        "",             "난이도: ???\n아직 정찰되지 않은 지역."),
    };

    int selectedRegion = -1;
    Text[] regionBtnTexts; // 버튼 텍스트 캐시 (시간 업데이트용)

    struct RegionInfo
    {
        public string name;
        public string sceneName;
        public string spawnId;
        public string regionId;   // RegionTimeManager 연동용
        public string description;

        public RegionInfo(string name, string scene, string spawn, string regionId, string desc)
        {
            this.name = name;
            sceneName = scene;
            spawnId = spawn;
            this.regionId = regionId;
            description = desc;
        }

        public bool IsLocked => string.IsNullOrEmpty(sceneName);
    }

    void Awake()
    {
        BuildUI();
    }

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

        // 버튼 텍스트에 낮/밤 아이콘 실시간 갱신
        UpdateRegionTimeDisplay();
    }

    void BuildUI()
    {
        // Canvas
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

        // ── 루트 ──
        panelRoot = new GameObject("PanelRoot");
        panelRoot.transform.SetParent(canvasRT, false);
        var rootRT = panelRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        dimBg = panelRoot.AddComponent<Image>();
        dimBg.color = new Color(0, 0, 0, 0.75f);

        // ── 중앙 패널 ──
        var panel = CreateRect(panelRoot.transform, "CenterPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520, 380));
        var panelImg = panel.gameObject.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        // 제목
        titleText = MakeText(panel, "Title", "◈ 출전 지역 선택 ◈",
            new Vector2(0, -15), new Vector2(480, 35), 22, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
        titleText.fontStyle = FontStyle.Bold;

        // 구분선
        MakeLine(panel, -50);

        // ── 지역 버튼들 (좌측) ──
        regionButtons = new Button[regions.Length];
        regionBtnTexts = new Text[regions.Length];
        for (int i = 0; i < regions.Length; i++)
        {
            int idx = i; // 클로저 캡처용

            var btnGO = new GameObject($"Region_{i}");
            btnGO.transform.SetParent(panel, false);

            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0, 1);
            btnRT.anchorMax = new Vector2(0, 1);
            btnRT.pivot = new Vector2(0, 1);
            btnRT.anchoredPosition = new Vector2(20, -65 - i * 55);
            btnRT.sizeDelta = new Vector2(220, 45);

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = regions[i].IsLocked ? new Color(0.2f, 0.2f, 0.2f, 0.8f) : new Color(0.15f, 0.2f, 0.3f, 0.9f);

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            if (regions[i].IsLocked)
            {
                btn.interactable = false;
            }
            else
            {
                btn.onClick.AddListener(() => SelectRegion(idx));

                // 호버 색상
                var colors = btn.colors;
                colors.highlightedColor = new Color(0.2f, 0.35f, 0.55f);
                colors.pressedColor = new Color(0.15f, 0.25f, 0.45f);
                btn.colors = colors;
            }

            // 버튼 텍스트
            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(10, 0);
            txtRT.offsetMax = Vector2.zero;

            var txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 16;
            txt.fontStyle = FontStyle.Bold;
            txt.color = regions[i].IsLocked ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
            txt.text = regions[i].name;
            txt.alignment = TextAnchor.MiddleLeft;

            regionButtons[i] = btn;
            regionBtnTexts[i] = txt;
        }

        // ── 우측: 지역 정보 ──
        var infoPanel = CreateRect(panel, "InfoPanel",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(240, 220));
        infoPanel.pivot = new Vector2(1, 1);
        infoPanel.anchoredPosition = new Vector2(-20, -60);
        var infoBg = infoPanel.gameObject.AddComponent<Image>();
        infoBg.color = new Color(0.05f, 0.05f, 0.08f, 0.8f);

        selectedInfoText = MakeText(infoPanel, "InfoText", "지역을 선택하세요",
            new Vector2(10, -10), new Vector2(220, 200), 14, new Color(0.8f, 0.85f, 0.9f), TextAnchor.UpperLeft);

        // ── 하단 버튼 ──
        // 출전 버튼
        var confirmGO = new GameObject("ConfirmBtn");
        confirmGO.transform.SetParent(panel, false);
        var confirmRT = confirmGO.AddComponent<RectTransform>();
        confirmRT.anchorMin = new Vector2(1, 0);
        confirmRT.anchorMax = new Vector2(1, 0);
        confirmRT.pivot = new Vector2(1, 0);
        confirmRT.anchoredPosition = new Vector2(-20, 20);
        confirmRT.sizeDelta = new Vector2(140, 40);

        var confirmImg = confirmGO.AddComponent<Image>();
        confirmImg.color = new Color(0.15f, 0.5f, 0.2f);

        confirmBtn = confirmGO.AddComponent<Button>();
        confirmBtn.targetGraphic = confirmImg;
        confirmBtn.onClick.AddListener(OnConfirm);
        confirmBtn.interactable = false;

        var cColors = confirmBtn.colors;
        cColors.highlightedColor = new Color(0.2f, 0.7f, 0.3f);
        cColors.pressedColor = new Color(0.1f, 0.4f, 0.15f);
        cColors.disabledColor = new Color(0.2f, 0.2f, 0.2f);
        confirmBtn.colors = cColors;

        confirmText = MakeChildText(confirmGO.transform, "출전", 16, Color.white);

        // 취소 버튼
        var cancelGO = new GameObject("CancelBtn");
        cancelGO.transform.SetParent(panel, false);
        var cancelRT = cancelGO.AddComponent<RectTransform>();
        cancelRT.anchorMin = new Vector2(0, 0);
        cancelRT.anchorMax = new Vector2(0, 0);
        cancelRT.pivot = new Vector2(0, 0);
        cancelRT.anchoredPosition = new Vector2(20, 20);
        cancelRT.sizeDelta = new Vector2(100, 40);

        var cancelImg = cancelGO.AddComponent<Image>();
        cancelImg.color = new Color(0.4f, 0.15f, 0.15f);

        cancelBtn = cancelGO.AddComponent<Button>();
        cancelBtn.targetGraphic = cancelImg;
        cancelBtn.onClick.AddListener(Hide);

        var xColors = cancelBtn.colors;
        xColors.highlightedColor = new Color(0.6f, 0.2f, 0.2f);
        xColors.pressedColor = new Color(0.3f, 0.1f, 0.1f);
        cancelBtn.colors = xColors;

        MakeChildText(cancelGO.transform, "취소", 14, Color.white);

        // ESC 안내
        MakeText(panel, "EscHint", "[ESC] 닫기",
            new Vector2(140, 20), new Vector2(200, 30), 12, new Color(0.5f, 0.5f, 0.5f), TextAnchor.MiddleLeft);

        panelRoot.SetActive(false);
    }

    void SelectRegion(int idx)
    {
        selectedRegion = idx;
        UpdateSelection();
    }

    void UpdateSelection()
    {
        // 버튼 하이라이트
        for (int i = 0; i < regionButtons.Length; i++)
        {
            if (regionButtons[i] == null) continue;
            var img = regionButtons[i].GetComponent<Image>();
            if (i == selectedRegion)
                img.color = new Color(0.2f, 0.35f, 0.55f, 0.95f);
            else if (regions[i].IsLocked)
                img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            else
                img.color = new Color(0.15f, 0.2f, 0.3f, 0.9f);
        }

        // 정보 패널
        if (selectedRegion >= 0 && selectedRegion < regions.Length)
        {
            var r = regions[selectedRegion];
            string timeInfo = GetRegionTimeText(r.regionId);
            selectedInfoText.text = $"<b>{r.name}</b>\n{timeInfo}\n\n{r.description}";
            confirmBtn.interactable = !r.IsLocked;
        }
        else
        {
            selectedInfoText.text = "지역을 선택하세요";
            confirmBtn.interactable = false;
        }
    }

    void OnConfirm()
    {
        if (selectedRegion < 0 || selectedRegion >= regions.Length) return;
        var r = regions[selectedRegion];
        if (r.IsLocked) return;

        Hide();

        // 지역 시간 매니저에 활성 지역 설정
        if (RegionTimeManager.Instance != null)
            RegionTimeManager.Instance.ActiveRegionId = r.regionId;

        // 안전가옥(timeScale=0)에서 출전 → 시간 복구
        Time.timeScale = 1f;

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(r.sceneName, r.spawnId);
    }

    #region 시간 표시

    /// <summary>버튼 텍스트에 낮/밤 아이콘 실시간 갱신</summary>
    void UpdateRegionTimeDisplay()
    {
        if (regionBtnTexts == null || RegionTimeManager.Instance == null) return;

        for (int i = 0; i < regions.Length; i++)
        {
            if (regionBtnTexts[i] == null) continue;
            var r = regions[i];
            if (r.IsLocked)
            {
                regionBtnTexts[i].text = r.name;
                continue;
            }

            var rt = RegionTimeManager.Instance.GetRegion(r.regionId);
            if (rt == null) continue;

            string icon = rt.isNight ? "☾" : "☀";
            Color iconColor = rt.isNight ? new Color(0.5f, 0.6f, 1f) : new Color(1f, 0.9f, 0.3f);

            // 남은 시간
            float remaining = rt.isNight
                ? RegionTimeManager.Instance.NightDuration - rt.elapsed
                : RegionTimeManager.Instance.DayDuration - rt.elapsed;
            int min = (int)(remaining / 60);
            int sec = (int)(remaining % 60);

            regionBtnTexts[i].text = $"{icon} {r.name}  {min}:{sec:D2}";
        }

        // 선택된 지역 정보 패널도 실시간 갱신
        if (selectedRegion >= 0 && selectedRegion < regions.Length)
        {
            var r = regions[selectedRegion];
            string timeInfo = GetRegionTimeText(r.regionId);
            selectedInfoText.text = $"<b>{r.name}</b>\n{timeInfo}\n\n{r.description}";
        }
    }

    /// <summary>지역 시간 정보 문자열 생성</summary>
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
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
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
        rt.sizeDelta = new Vector2(480, 1);
        var img = go.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.2f);
    }

    #endregion
}
