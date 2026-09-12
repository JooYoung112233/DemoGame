using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 메뉴 (1차: 볼륨 3종 / 해상도·창모드 / 키 안내 보기 전용 — docs/dev-roadmap.md 2026-07-10).
/// PauseMenu의 '설정' 버튼으로 열린다(일시정지 위 레이어). 저장 = GameSettings(PlayerPrefs).
/// 온디맨드 싱글턴 + 프리팹 폴백(PauseMenu와 동일 관례). ESC 닫기는 UIManager.CloseTopmost가 처리.
/// 키 리바인딩은 2차(신 Input System rebinding 별도).
/// </summary>
public class SettingsUI : MonoBehaviour
{
    public static SettingsUI Instance { get; private set; }
    public static bool IsShowing => Instance != null && Instance.canvas != null && Instance.canvas.gameObject.activeSelf;

    [SerializeField] Canvas canvas;
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Text masterValue;
    [SerializeField] Text bgmValue;
    [SerializeField] Text sfxValue;
    [SerializeField] Text resolutionLabel;
    [SerializeField] Text screenModeLabel;
    [SerializeField] Button resPrevBtn;
    [SerializeField] Button resNextBtn;
    [SerializeField] Button modePrevBtn;
    [SerializeField] Button modeNextBtn;
    [SerializeField] Button closeBtn;

    Font font;
    bool IsGenerated => canvas != null;

    // 해상도 후보(중복 제거, 런타임 구성)와 창모드 목록.
    readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    int resIndex;
    static readonly (FullScreenMode mode, string label)[] ScreenModes =
    {
        (FullScreenMode.FullScreenWindow,    "전체화면"),
        (FullScreenMode.ExclusiveFullScreen, "전체화면 (독점)"),
        (FullScreenMode.Windowed,            "창모드"),
    };
    int modeIndex;

    public static SettingsUI Show()
    {
        if (Instance == null)
        {
            var prefab = Resources.Load<GameObject>("UI/SettingsUI");
            GameObject go = prefab != null ? Instantiate(prefab) : new GameObject("SettingsUI");
            go.name = "SettingsUI";
            DontDestroyOnLoad(go);
            if (prefab == null) Instance = go.AddComponent<SettingsUI>();   // 폴백: Awake가 BuildUI
            EnsureEventSystem();   // 현재 진입은 PauseMenu 경유지만, 직접 Show() 경로(타이틀 등) 대비
        }
        Instance.Open();
        return Instance;
    }

    public static void Hide()
    {
        if (Instance != null) Instance.SetVisible(false);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (!IsGenerated) BuildUI();
        WireEvents();   // onClick/onValueChanged는 프리팹에 직렬화 안 됨 → 항상 재부착
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void WireEvents()
    {
        WireSlider(masterSlider, v => { GameSettings.MasterVolume = v; UpdateValueLabel(masterValue, v); });
        WireSlider(bgmSlider,    v => { GameSettings.BgmVolume    = v; UpdateValueLabel(bgmValue, v); });
        WireSlider(sfxSlider,    v => { GameSettings.SfxVolume    = v; UpdateValueLabel(sfxValue, v); });
        WireButton(resPrevBtn,  () => CycleResolution(-1));
        WireButton(resNextBtn,  () => CycleResolution(+1));
        WireButton(modePrevBtn, () => CycleScreenMode(-1));
        WireButton(modeNextBtn, () => CycleScreenMode(+1));
        WireButton(closeBtn,    () => SetVisible(false));
    }

    static void WireSlider(Slider s, UnityEngine.Events.UnityAction<float> onChanged)
    {
        if (s == null) return;
        s.onValueChanged.RemoveAllListeners();
        s.onValueChanged.AddListener(onChanged);
    }

    static void WireButton(Button b, UnityEngine.Events.UnityAction onClick)
    {
        if (b == null) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(onClick);
    }

    void Open()
    {
        // 표시 직전 현재 값으로 동기화(SetValueWithoutNotify — 콜백 없이 값만, 라벨은 아래서 명시 갱신).
        BuildResolutionList();
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
        if (bgmSlider != null)    bgmSlider.SetValueWithoutNotify(GameSettings.BgmVolume);
        if (sfxSlider != null)    sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
        UpdateValueLabel(masterValue, GameSettings.MasterVolume);
        UpdateValueLabel(bgmValue, GameSettings.BgmVolume);
        UpdateValueLabel(sfxValue, GameSettings.SfxVolume);
        UpdateScreenLabels();
        SetVisible(true);
    }

    void SetVisible(bool v)
    {
        if (canvas != null) canvas.gameObject.SetActive(v);
        if (!v) GameSettings.Flush();   // 닫을 때 디스크 flush(강제 종료 시 볼륨 변경 유실 방지)
    }

    static void UpdateValueLabel(Text t, float v)
    {
        if (t != null) t.text = Mathf.RoundToInt(v * 100f) + "%";
    }

    // ── 해상도 / 창모드 ──────────────────────────────────
    void BuildResolutionList()
    {
        resolutions.Clear();
        foreach (var r in Screen.resolutions)
        {
            var wh = new Vector2Int(r.width, r.height);
            if (!resolutions.Contains(wh)) resolutions.Add(wh);
        }
        // 에디터/이상 환경 폴백: 목록이 비면 흔한 해상도 3종.
        if (resolutions.Count == 0)
        {
            resolutions.Add(new Vector2Int(1280, 720));
            resolutions.Add(new Vector2Int(1920, 1080));
            resolutions.Add(new Vector2Int(2560, 1440));
        }

        // 현재 인덱스 = 저장값 우선, 없으면 현재 화면. 목록에 없으면(모니터 교체 등) 최대 해상도로 폴백.
        var saved = GameSettings.SavedResolution;
        var cur = saved.x > 0 ? saved : new Vector2Int(Screen.width, Screen.height);
        resIndex = resolutions.FindIndex(r => r == cur);
        if (resIndex < 0) resIndex = resolutions.Count - 1;

        var mode = GameSettings.SavedScreenMode;
        modeIndex = 0;
        for (int i = 0; i < ScreenModes.Length; i++)
            if (ScreenModes[i].mode == mode) { modeIndex = i; break; }
    }

    void CycleResolution(int dir)
    {
        if (resolutions.Count == 0) return;
        resIndex = (resIndex + dir + resolutions.Count) % resolutions.Count;
        ApplyScreen();
    }

    void CycleScreenMode(int dir)
    {
        modeIndex = (modeIndex + dir + ScreenModes.Length) % ScreenModes.Length;
        ApplyScreen();
    }

    void ApplyScreen()
    {
        var r = resolutions[resIndex];
        GameSettings.ApplyResolution(r.x, r.y, ScreenModes[modeIndex].mode);
        UpdateScreenLabels();
    }

    void UpdateScreenLabels()
    {
        if (resolutionLabel != null && resolutions.Count > 0)
            resolutionLabel.text = $"{resolutions[resIndex].x} × {resolutions[resIndex].y}";
        if (screenModeLabel != null)
            screenModeLabel.text = ScreenModes[modeIndex].label;
    }

    // ── UI 생성 (절차적 uGUI — PauseMenu 관례) ────────────
    void BuildUI()
    {
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGO = new GameObject("Settings_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 620;   // PauseMenu(600) 위
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        UITheme.ConfigureCanvasScale(scaler);
        canvasGO.AddComponent<GraphicRaycaster>();

        // 어두운 오버레이 + 중앙 패널
        var dim = MakeRect("Dim", canvasGO.transform);
        Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        var panel = MakeRect("Panel", canvasGO.transform);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(800, 920);
        var panelImg = panel.gameObject.AddComponent<Image>();
        panelImg.color = UITheme.Panel;

        MakeText("Title", panel, "설정", 44, FontStyle.Bold, UITheme.TextBright,
            new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(400, 60));

        float y = -130f;

        // ── 소리 ──
        MakeText("Sec_Sound", panel, "소리", 28, FontStyle.Bold, UITheme.TextDim,
            new Vector2(0f, 1f), new Vector2(70, y), new Vector2(200, 40), TextAnchor.MiddleLeft);
        y -= 55f;
        MakeVolumeRow(panel, "마스터", ref y, out masterSlider, out masterValue);
        MakeVolumeRow(panel, "배경음", ref y, out bgmSlider, out bgmValue);
        MakeVolumeRow(panel, "효과음", ref y, out sfxSlider, out sfxValue);

        y -= 20f;

        // ── 화면 ──
        MakeText("Sec_Screen", panel, "화면", 28, FontStyle.Bold, UITheme.TextDim,
            new Vector2(0f, 1f), new Vector2(70, y), new Vector2(200, 40), TextAnchor.MiddleLeft);
        y -= 55f;
        MakeCycleRow(panel, "해상도", ref y, out resPrevBtn, out resolutionLabel, out resNextBtn);
        MakeCycleRow(panel, "창모드", ref y, out modePrevBtn, out screenModeLabel, out modeNextBtn);
        MakeText("ScreenNote", panel, "변경 사항은 자동으로 저장됩니다", 18, FontStyle.Normal, UITheme.TextMuted,
            new Vector2(0f, 1f), new Vector2(90, y), new Vector2(500, 30), TextAnchor.MiddleLeft);
        y -= 50f;

        // ── 조작 (보기 전용 — 리바인딩 2차) ──
        MakeText("Sec_Keys", panel, "조작", 28, FontStyle.Bold, UITheme.TextDim,
            new Vector2(0f, 1f), new Vector2(70, y), new Vector2(200, 40), TextAnchor.MiddleLeft);
        y -= 45f;
        MakeText("Keys", panel,
            "이동 W A S D   ·   달리기 Shift   ·   웅크리기 C\n" +
            "상호작용 E   ·   조준 마우스 오른쪽   ·   퀵슬롯 1~6\n" +
            "인벤토리 Tab   ·   퀘스트 J   ·   닫기 / 메뉴 Esc",
            20, FontStyle.Normal, UITheme.TextBright,
            new Vector2(0f, 1f), new Vector2(90, y - 35), new Vector2(600, 110), TextAnchor.UpperLeft);

        // 닫기
        MakeButton(panel, "닫기", new Vector2(0, 55), new Vector2(0.5f, 0f), out closeBtn);
    }

    void MakeVolumeRow(RectTransform panel, string label, ref float y, out Slider slider, out Text valueText)
    {
        MakeText($"Lbl_{label}", panel, label, 24, FontStyle.Normal, UITheme.TextBright,
            new Vector2(0f, 1f), new Vector2(110, y + 20), new Vector2(160, 40), TextAnchor.MiddleLeft);
        slider = MakeSlider(panel, $"Sld_{label}", new Vector2(440, y), new Vector2(320, 24));
        valueText = MakeText($"Val_{label}", panel, "100%", 22, FontStyle.Normal, UITheme.TextDim,
            new Vector2(0f, 1f), new Vector2(620, y + 20), new Vector2(100, 40), TextAnchor.MiddleRight);
        y -= 55f;
    }

    void MakeCycleRow(RectTransform panel, string label, ref float y,
        out Button prevBtn, out Text valueLabel, out Button nextBtn)
    {
        MakeText($"Lbl_{label}", panel, label, 24, FontStyle.Normal, UITheme.TextBright,
            new Vector2(0f, 1f), new Vector2(110, y + 20), new Vector2(160, 40), TextAnchor.MiddleLeft);
        prevBtn = MakeArrowButton(panel, $"Prev_{label}", "◀", new Vector2(310, y));
        valueLabel = MakeText($"Val_{label}", panel, "-", 24, FontStyle.Normal, UITheme.TextBright,
            new Vector2(0f, 1f), new Vector2(475, y), new Vector2(250, 40), TextAnchor.MiddleCenter);
        valueLabel.rectTransform.pivot = new Vector2(.5f, .5f);
        nextBtn = MakeArrowButton(panel, $"Next_{label}", "▶", new Vector2(640, y));
        y -= 55f;
    }

    Slider MakeSlider(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        var root = MakeRect(name, parent);
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = pos;
        root.sizeDelta = size;

        var bg = MakeRect("Background", root);
        Stretch(bg);
        bg.offsetMin = new Vector2(0, 8); bg.offsetMax = new Vector2(0, -8);
        bg.gameObject.AddComponent<Image>().color = UITheme.Cell;

        var fillArea = MakeRect("Fill Area", root);
        Stretch(fillArea);
        fillArea.offsetMin = new Vector2(4, 8); fillArea.offsetMax = new Vector2(-4, -8);
        var fill = MakeRect("Fill", fillArea);
        Stretch(fill);
        var fillImg = fill.gameObject.AddComponent<Image>();
        fillImg.color = UITheme.Accent;

        var handleArea = MakeRect("Handle Slide Area", root);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(8, 0); handleArea.offsetMax = new Vector2(-8, 0);
        var handle = MakeRect("Handle", handleArea);
        handle.sizeDelta = new Vector2(18, 0);
        var handleImg = handle.gameObject.AddComponent<Image>();
        handleImg.color = UITheme.TextBright;

        var slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImg;
        slider.minValue = 0f; slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    Button MakeArrowButton(RectTransform parent, string name, string glyph, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(52, 40);

        var img = go.AddComponent<Image>();
        img.color = UITheme.Cell;
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = UITheme.CellHover;
        colors.pressedColor = UITheme.CellPressed;
        btn.colors = colors;
        btn.targetGraphic = img;

        var t = MakeText("Label", rt, glyph, 22, FontStyle.Bold, UITheme.TextBright,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 40));
        Stretch(t.rectTransform);
        return btn;
    }

    void MakeButton(RectTransform parent, string label, Vector2 pos, Vector2 anchor, out Button button)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(260, 56);

        var img = go.AddComponent<Image>();
        img.color = UITheme.Cell;
        button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = UITheme.CellHover;
        colors.pressedColor = UITheme.CellPressed;
        button.colors = colors;
        button.targetGraphic = img;

        var t = MakeText("Label", rt, label, 26, FontStyle.Bold, UITheme.TextBright,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 56));
        Stretch(t.rectTransform);
    }

    Text MakeText(string name, Transform parent, string content, int size, FontStyle style, Color color,
        Vector2 anchor, Vector2 pos, Vector2 sizeDelta, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(anchor.x, anchor.y);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<Text>();
        t.font = font; t.text = content; t.fontSize = size; t.fontStyle = style; t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
        DontDestroyOnLoad(es);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — BuildUI 1회 실행(프리팹화). 런타임 셋업 제외.</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
        canvas.gameObject.SetActive(false);   // 베이크 기본 = 닫힘(Show가 켬)
    }
#endif
}
