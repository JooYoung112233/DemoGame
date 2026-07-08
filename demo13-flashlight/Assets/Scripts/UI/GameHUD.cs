using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 메인 게임 HUD (Canvas/uGUI).
/// 부상 아이콘 + 하이드아웃 나가기 버튼.
/// HP/스태미너/돈/배터리는 캐릭터 패널·상점 등 해당 UI에서만 표시.
/// **가시성**: 게임플레이 씬이 로드돼 있을 때만 표시.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] Color barBgColor = new Color(0.115f, 0.110f, 0.098f, 0.85f);

    // 레퍼런스
    Health health;
    PlayerMedicalSystem medical;

    // uGUI 요소
    [Header("uGUI References (auto-filled by GenerateUI)")]
    [SerializeField] Canvas canvas;
    [SerializeField] CanvasScaler scaler;
    [SerializeField] RectTransform injuryPanel;
    [SerializeField] Text[] injuryIcons;
    [SerializeField] Text survivalWarnText;   // 수분/포만감 위험 경고(상단 중앙)

    // 하이드아웃 나가기 버튼
    [SerializeField] GameObject hideoutExitBtnGO;

    public bool IsGenerated => canvas != null;

    void Awake()
    {
        if (!IsGenerated) GenerateUI();
        WireEvents();   // onClick은 프리팹에 직렬화 안 됨(§6.5) — 프리팹/코드 양쪽 경로에서 재부착
        SceneManager.sceneLoaded += OnSceneLoadedHUD;
        SceneManager.sceneUnloaded += OnSceneUnloadedHUD;
        ApplyVisibility();   // 부팅 시점엔 게임플레이 씬 없음 → 숨김
    }

    /// <summary>정적 버튼 onClick 재부착 — 하이드아웃 나가기(우상단). 빌더에서만 붙이면 프리팹 경로에서 무반응.</summary>
    void WireEvents()
    {
        if (hideoutExitBtnGO == null) return;
        var btn = hideoutExitBtnGO.GetComponent<UnityEngine.UI.Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnHideoutExitClicked);
    }

    void OnSceneLoadedHUD(Scene scene, LoadSceneMode mode) => ApplyVisibility();
    void OnSceneUnloadedHUD(Scene scene) => ApplyVisibility();

    /// <summary>게임플레이 씬이 하나라도 로드돼 있으면 HUD 표시, 아니면 숨김(타이틀/Systems 단독).</summary>
    void ApplyVisibility()
    {
        if (canvas == null) return;
        bool inGameplay = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && SystemsScene.IsGameplayScene(s)) { inGameplay = true; break; }
        }
        canvas.enabled = inGameplay;
    }

    void Update()
    {
        if (health == null) FindPlayer();
        if (health == null) return;

        UpdateInjuryIcons();
        UpdateSurvivalWarning();
        SyncHideoutExitButton();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedHUD;
        SceneManager.sceneUnloaded -= OnSceneUnloadedHUD;
    }

    void FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return;
        health = go.GetComponent<Health>();
        medical = go.GetComponent<PlayerMedicalSystem>();
    }

    #region UI 빌드

    public void GenerateUI()
    {
        // 이미 Canvas가 있으면 스킵
        canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            // 기존 요소 재연결
            RebindExisting();
            return;
        }

        // Canvas 생성
        var canvasGO = new GameObject("HUD_Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var canvasRT = canvasGO.GetComponent<RectTransform>();

        // ── 앵커 패널 (좌하단) ──
        var anchor = CreatePanel("Anchor_BottomLeft", canvasRT);
        var anchorRT = anchor.GetComponent<RectTransform>();
        anchorRT.anchorMin = new Vector2(0, 0);
        anchorRT.anchorMax = new Vector2(0, 0);
        anchorRT.pivot = new Vector2(0, 0);
        anchorRT.anchoredPosition = new Vector2(30, 25);
        anchorRT.sizeDelta = new Vector2(300, 120);

        // ── 부상 아이콘 패널 ──
        var injGO = CreatePanel("InjuryIcons", anchorRT);
        injuryPanel = injGO.GetComponent<RectTransform>();
        injuryPanel.anchorMin = new Vector2(0, 1);
        injuryPanel.anchorMax = new Vector2(0, 1);
        injuryPanel.pivot = new Vector2(0, 0);
        injuryPanel.anchoredPosition = new Vector2(0, 5);
        injuryPanel.sizeDelta = new Vector2(250, 24);

        // 부상 아이콘 (최대 8개 미리 생성)
        injuryIcons = new Text[8];
        for (int i = 0; i < injuryIcons.Length; i++)
        {
            var iconGO = new GameObject($"Injury_{i}");
            iconGO.transform.SetParent(injuryPanel, false);

            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0, 0.5f);
            iconRT.anchorMax = new Vector2(0, 0.5f);
            iconRT.pivot = new Vector2(0, 0.5f);
            iconRT.anchoredPosition = new Vector2(i * 24, 0);
            iconRT.sizeDelta = new Vector2(22, 22);

            var txt = iconGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 16;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = "";

            injuryIcons[i] = txt;
            iconGO.SetActive(false);
        }

        // ── 하이드아웃 나가기 버튼 (우상단, Hideout일 때만 표시) ──
        BuildHideoutExitButton(canvasRT);

        // ── 생존 위험 경고 (상단 중앙) ──
        BuildSurvivalWarning(canvasRT);
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — GenerateUI를 1회 실행해 프리팹화할 영속 계층을 만든다.
    /// (부상 아이콘 8개·생존 경고·나가기 버튼 = 영속 스켈레톤은 베이크 대상. 부상 표시는 런타임에 토글만.)</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        GenerateUI();
    }
#endif

    void BuildSurvivalWarning(RectTransform canvasRT)
    {
        var go = new GameObject("SurvivalWarn");
        go.transform.SetParent(canvasRT, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -64);
        rt.sizeDelta = new Vector2(640, 36);

        survivalWarnText = go.AddComponent<Text>();
        survivalWarnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        survivalWarnText.fontSize = 20;
        survivalWarnText.fontStyle = FontStyle.Bold;
        survivalWarnText.alignment = TextAnchor.MiddleCenter;
        survivalWarnText.horizontalOverflow = HorizontalWrapMode.Overflow;
        survivalWarnText.text = "";
        go.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.8f);
        go.SetActive(false);
    }

    void UpdateSurvivalWarning()
    {
        if (survivalWarnText == null) return;
        var s = SurvivalStats.Get();
        if (s == null) { survivalWarnText.gameObject.SetActive(false); return; }

        // 위험한(낮은) 쪽 메시지 모음
        string msg = "";
        bool critical = false;
        if (s.Water <= 0f)      { msg += "⚠ 탈수! ";    critical = true; }
        else if (s.Water <= 20f) msg += "⚠ 수분 부족 ";
        if (s.Satiety <= 0f)    { msg += "⚠ 굶주림! ";  critical = true; }
        else if (s.Satiety <= 20f) msg += "⚠ 허기 ";

        if (string.IsNullOrEmpty(msg))
        {
            survivalWarnText.gameObject.SetActive(false);
            return;
        }
        survivalWarnText.gameObject.SetActive(true);
        survivalWarnText.text = msg.TrimEnd();
        if (critical)
        {
            float blink = Mathf.PingPong(Time.unscaledTime * 3f, 1f);
            survivalWarnText.color = Color.Lerp(new Color(1f, 0.5f, 0.2f), new Color(1f, 0.15f, 0.15f), blink);
        }
        else
        {
            survivalWarnText.color = new Color(1f, 0.82f, 0.3f);
        }
    }

    void BuildHideoutExitButton(RectTransform canvasRT)
    {
        hideoutExitBtnGO = new GameObject("HideoutExitBtn");
        hideoutExitBtnGO.transform.SetParent(canvasRT, false);
        var rt = hideoutExitBtnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-30, -25);
        rt.sizeDelta = new Vector2(140, 40);

        var bg = hideoutExitBtnGO.AddComponent<Image>();
        bg.color = UITheme.Danger;

        var btn = hideoutExitBtnGO.AddComponent<Button>();
        btn.targetGraphic = bg;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.52f, 0.20f, 0.16f);
        colors.pressedColor = new Color(0.30f, 0.10f, 0.09f);
        btn.colors = colors;
        btn.onClick.AddListener(OnHideoutExitClicked);

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(hideoutExitBtnGO.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        var txt = txtGO.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 16;
        txt.fontStyle = FontStyle.Bold;
        txt.color = UITheme.TextBright;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = "나가기";

        txtGO.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.7f);

        hideoutExitBtnGO.SetActive(false);
    }

    void SyncHideoutExitButton()
    {
        if (hideoutExitBtnGO == null) return;
        hideoutExitBtnGO.SetActive(HideoutController.IsActive);
    }

    void OnHideoutExitClicked()
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo("Safehouse", "default");
    }

    public void ClearGeneratedUI()
    {
        var hudCanvas = transform.Find("HUD_Canvas");
        if (hudCanvas != null)
        {
            if (Application.isPlaying)
                Destroy(hudCanvas.gameObject);
            else
                DestroyImmediate(hudCanvas.gameObject);
        }

        canvas = null;
        scaler = null;
        injuryPanel = null;
        injuryIcons = null;
        hideoutExitBtnGO = null;
    }

    void RebindExisting()
    {
        injuryPanel = FindChild<RectTransform>("InjuryIcons");
        if (injuryPanel != null)
            injuryIcons = injuryPanel.GetComponentsInChildren<Text>(true);
    }

    T FindChild<T>(string childName) where T : Component
    {
        var all = GetComponentsInChildren<T>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i].gameObject.name == childName)
                return all[i];
        return null;
    }

    GameObject CreatePanel(string name, RectTransform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    #endregion

    #region 업데이트

    void UpdateInjuryIcons()
    {
        if (injuryIcons == null || medical == null) return;

        // 모두 숨김
        for (int i = 0; i < injuryIcons.Length; i++)
            if (injuryIcons[i] != null)
                injuryIcons[i].gameObject.SetActive(false);

        if (!medical.HasAnyInjury) return;

        int idx = 0;
        var parts = medical.GetAllParts();
        for (int i = 0; i < parts.Length && idx < injuryIcons.Length; i++)
        {
            for (int j = 0; j < parts[i].injuries.Count && idx < injuryIcons.Length; j++)
            {
                var inj = parts[i].injuries[j];
                var icon = injuryIcons[idx];
                icon.gameObject.SetActive(true);
                icon.text = GetInjurySymbol(inj.type);
                icon.color = GetInjuryColor(inj.type, inj.severity);
                idx++;
            }
        }
    }

    string GetInjurySymbol(InjuryType type)
    {
        switch (type)
        {
            case InjuryType.Bleeding: return "●";
            case InjuryType.Fracture: return "✕";
            case InjuryType.Pain: return "◆";
            default: return "?";
        }
    }

    Color GetInjuryColor(InjuryType type, float severity)
    {
        Color c;
        switch (type)
        {
            case InjuryType.Bleeding: c = new Color(1f, 0.15f, 0.15f); break;
            case InjuryType.Fracture: c = new Color(1f, 0.6f, 0.1f); break;
            case InjuryType.Pain: c = new Color(0.8f, 0.7f, 1f); break;
            default: c = Color.white; break;
        }

        // 심각하면 깜빡임
        if (severity > 0.7f)
        {
            float blink = Mathf.PingPong(Time.unscaledTime * 3f, 1f);
            c.a = 0.6f + blink * 0.4f;
        }

        return c;
    }

    #endregion
}
