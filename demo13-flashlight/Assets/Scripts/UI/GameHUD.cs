using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 메인 게임 HUD (Canvas/uGUI).
/// HP 바, 스태미너 바, 부상 아이콘.
/// UIManager 자식으로 배치. 에디터 씬 뷰에서도 레이아웃 확인 가능.
/// **가시성**: 게임플레이 씬(안전가옥/레이드)이 로드돼 있을 때만 표시. 타이틀(Systems 단독)에선 숨김 → HP바 깜빡임 없음.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Bar Settings")]
    [SerializeField] float hpBarWidth = 220f;
    [SerializeField] float hpBarHeight = 22f;
    [SerializeField] float staminaBarWidth = 180f;
    [SerializeField] float staminaBarHeight = 12f;

    [Header("Colors")]
    [SerializeField] Color hpHighColor = new Color(0.2f, 0.9f, 0.3f);
    [SerializeField] Color hpMidColor = new Color(0.9f, 0.8f, 0.1f);
    [SerializeField] Color hpLowColor = new Color(0.8f, 0.1f, 0.1f);
    [SerializeField] Color staminaFullColor = new Color(0.3f, 0.9f, 0.9f);
    [SerializeField] Color staminaLowColor = new Color(1f, 0.5f, 0.1f);
    [SerializeField] Color exhaustedColor = new Color(0.3f, 0.4f, 1f);
    [SerializeField] Color barBgColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);

    [Header("Battery Colors")]
    [SerializeField] Color batteryFullColor = new Color(0.3f, 0.9f, 1f);
    [SerializeField] Color batteryLowColor = new Color(1f, 0.3f, 0.1f);
    [SerializeField] float batteryBarWidth = 140f;
    [SerializeField] float batteryBarHeight = 10f;

    // 레퍼런스
    Health health;
    PlayerMedicalSystem medical;
    FlashlightController flashlight;

    // uGUI 요소
    [Header("uGUI References (auto-filled by GenerateUI)")]
    [SerializeField] Canvas canvas;
    [SerializeField] CanvasScaler scaler;
    [SerializeField] RectTransform hpBarBg, hpBarFill;
    [SerializeField] RectTransform stBarBg, stBarFill;
    [SerializeField] RectTransform batBarBg, batBarFill;
    [SerializeField] Text hpText, batText;
    [SerializeField] Image hpFillImage, stFillImage, batFillImage;
    [SerializeField] Text batIcon;
    [SerializeField] RectTransform injuryPanel;
    [SerializeField] Text[] injuryIcons;
    [SerializeField] Text rudiText;

    // 화폐 표시 상태
    bool currencyBound;
    float rudiFlash;

    // 상태
    float prevHpPct = 1f;
    float hpShakeTimer;
    float hpShakeIntensity;
    Vector2 hpBarBasePos;

    public bool IsGenerated => canvas != null;

    void Awake()
    {
        if (!IsGenerated) GenerateUI();
        SceneManager.sceneLoaded += OnSceneLoadedHUD;
        SceneManager.sceneUnloaded += OnSceneUnloadedHUD;
        ApplyVisibility();   // 부팅 시점엔 게임플레이 씬 없음 → 숨김
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
        if (!currencyBound) TryBindCurrency();
        UpdateRudiFlash();

        if (health == null) FindPlayer();
        if (health == null) return;

        // HP 바는 HUD에서 제거됨(캐릭터 패널에서 표시). 스태미너/배터리/부상만 HUD에 유지.
        UpdateStaminaBar();
        UpdateBatteryBar();
        UpdateInjuryIcons();
    }

    void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnBalanceChanged -= OnRudiChanged;
        SceneManager.sceneLoaded -= OnSceneLoadedHUD;
        SceneManager.sceneUnloaded -= OnSceneUnloadedHUD;
    }

    void TryBindCurrency()
    {
        if (CurrencyManager.Instance == null) return;
        CurrencyManager.Instance.OnBalanceChanged -= OnRudiChanged;
        CurrencyManager.Instance.OnBalanceChanged += OnRudiChanged;
        currencyBound = true;
        OnRudiChanged(CurrencyManager.Instance.Balance, 0);
    }

    void OnRudiChanged(int newBalance, int delta)
    {
        if (rudiText != null)
            rudiText.text = $"◈ {newBalance:N0}";
        if (delta != 0)
            rudiFlash = 1f; // 변동 시 반짝임
    }

    void UpdateRudiFlash()
    {
        if (rudiText == null) return;
        if (rudiFlash > 0f)
        {
            rudiFlash -= Time.unscaledDeltaTime * 2f;
            Color baseC = new Color(1f, 0.85f, 0.3f);
            Color flashC = Color.white;
            rudiText.color = Color.Lerp(baseC, flashC, Mathf.Clamp01(rudiFlash));
        }
        else
        {
            rudiText.color = new Color(1f, 0.85f, 0.3f);
        }
    }

    void FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return;
        health = go.GetComponent<Health>();
        medical = go.GetComponent<PlayerMedicalSystem>();
        flashlight = go.GetComponentInChildren<FlashlightController>();
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

        // ── HP 바: HUD에서 제거(2026-06-17). HP는 캐릭터 패널 "01 캐릭터 상태"에서 표시 ──

        // ── 스태미너 바 ──
        BuildBar(anchorRT, "Stamina", 0, staminaBarWidth, staminaBarHeight,
            out stBarBg, out stBarFill, out stFillImage, out _);

        // ── 배터리 바 ──
        float batYOffset = staminaBarHeight + 6;
        BuildBatteryBar(anchorRT, batYOffset);

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

        // ── 스크랩 카운터 (우상단) ──
        BuildRudiCounter(canvasRT);
    }

    void BuildRudiCounter(RectTransform canvasRT)
    {
        var panelGO = new GameObject("Rudi_Panel");
        panelGO.transform.SetParent(canvasRT, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(1, 1);
        panelRT.anchorMax = new Vector2(1, 1);
        panelRT.pivot = new Vector2(1, 1);
        panelRT.anchoredPosition = new Vector2(-30, -25);
        panelRT.sizeDelta = new Vector2(180, 36);

        var bg = panelGO.AddComponent<Image>();
        bg.color = barBgColor;

        var txtGO = new GameObject("Rudi_Text");
        txtGO.transform.SetParent(panelGO.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(10, 0);
        txtRT.offsetMax = new Vector2(-10, 0);

        rudiText = txtGO.AddComponent<Text>();
        rudiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rudiText.fontSize = 18;
        rudiText.fontStyle = FontStyle.Bold;
        rudiText.color = new Color(1f, 0.85f, 0.3f);
        rudiText.alignment = TextAnchor.MiddleRight;
        rudiText.text = "◈ 0";

        var shadow = txtGO.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f);
        shadow.effectDistance = new Vector2(1, -1);
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
        hpBarBg = null;
        hpBarFill = null;
        stBarBg = null;
        stBarFill = null;
        batBarBg = null;
        batBarFill = null;
        hpText = null;
        batText = null;
        hpFillImage = null;
        stFillImage = null;
        batFillImage = null;
        batIcon = null;
        injuryPanel = null;
        injuryIcons = null;
        rudiText = null;
        currencyBound = false;
    }

    void BuildBar(RectTransform parent, string label, float yOffset, float width, float height,
        out RectTransform bg, out RectTransform fill, out Image fillImg, out Text text)
    {
        // 배경
        var bgGO = new GameObject($"{label}_BG");
        bgGO.transform.SetParent(parent, false);
        bg = bgGO.AddComponent<RectTransform>();
        bg.anchorMin = new Vector2(0, 1);
        bg.anchorMax = new Vector2(0, 1);
        bg.pivot = new Vector2(0, 1);
        bg.anchoredPosition = new Vector2(0, -yOffset);
        bg.sizeDelta = new Vector2(width + 4, height + 4);

        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = barBgColor;

        // 채움
        var fillGO = new GameObject($"{label}_Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        fill = fillGO.AddComponent<RectTransform>();
        fill.anchorMin = new Vector2(0, 0);
        fill.anchorMax = new Vector2(0, 1);
        fill.pivot = new Vector2(0, 0.5f);
        fill.offsetMin = new Vector2(2, 2);
        fill.offsetMax = new Vector2(-2, -2);
        fill.sizeDelta = new Vector2(width, 0);

        fillImg = fillGO.AddComponent<Image>();
        fillImg.color = Color.green;

        // 텍스트 (HP만)
        text = null;
        if (label == "HP")
        {
            var txtGO = new GameObject($"{label}_Text");
            txtGO.transform.SetParent(bgGO.transform, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(8, 0);
            txtRT.offsetMax = Vector2.zero;

            text = txtGO.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.text = "HP 100 / 100";

            // 그림자
            var shadow = txtGO.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(1, -1);
        }
    }

    void RebindExisting()
    {
        // 씬에 배치된 Canvas에서 요소 재연결
        hpFillImage = FindChild<Image>("HP_Fill");
        stFillImage = FindChild<Image>("Stamina_Fill");
        hpText = FindChild<Text>("HP_Text");
        hpBarBg = FindChild<RectTransform>("HP_BG");
        stBarBg = FindChild<RectTransform>("Stamina_BG");
        hpBarFill = FindChild<RectTransform>("HP_Fill");
        stBarFill = FindChild<RectTransform>("Stamina_Fill");
        batFillImage = FindChild<Image>("Bat_Fill");
        batBarBg = FindChild<RectTransform>("Bat_BG");
        batBarFill = FindChild<RectTransform>("Bat_Fill");
        batText = FindChild<Text>("Bat_Text");
        batIcon = FindChild<Text>("Bat_Icon");
        injuryPanel = FindChild<RectTransform>("InjuryIcons");
        rudiText = FindChild<Text>("Rudi_Text");

        if (hpBarBg != null)
            hpBarBasePos = hpBarBg.anchoredPosition;

        // 부상 아이콘 재연결
        if (injuryPanel != null)
        {
            injuryIcons = injuryPanel.GetComponentsInChildren<Text>(true);
        }
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

    void UpdateHPBar()
    {
        if (hpFillImage == null || hpBarFill == null) return;

        float pct = health.Percent;

        // 크기
        hpBarFill.sizeDelta = new Vector2(hpBarWidth * pct, 0);

        // 색상
        Color c;
        if (pct > 0.6f) c = Color.Lerp(hpMidColor, hpHighColor, (pct - 0.6f) / 0.4f);
        else if (pct > 0.3f) c = Color.Lerp(hpLowColor, hpMidColor, (pct - 0.3f) / 0.3f);
        else c = hpLowColor;

        // 낮은 HP 깜빡임
        if (pct < 0.25f)
        {
            float blink = Mathf.PingPong(Time.unscaledTime * 3f, 1f);
            c = Color.Lerp(c, new Color(0.9f, 0.1f, 0.1f), blink * 0.5f);
        }

        hpFillImage.color = c;

        // 텍스트
        if (hpText != null)
            hpText.text = $"HP  {health.CurrentHp:F0} / {health.MaxHp:F0}";

        // 피격 감지
        if (pct < prevHpPct - 0.01f)
        {
            hpShakeTimer = 0.3f;
            hpShakeIntensity = (prevHpPct - pct) * 20f;
        }
        prevHpPct = pct;
    }

    void UpdateHPShake()
    {
        if (hpBarBg == null) return;

        if (hpShakeTimer > 0)
        {
            hpShakeTimer -= Time.deltaTime;
            float t = hpShakeTimer / 0.3f;
            float sx = Mathf.Sin(Time.unscaledTime * 60f) * hpShakeIntensity * t;
            float sy = Mathf.Cos(Time.unscaledTime * 45f) * hpShakeIntensity * 0.5f * t;
            hpBarBg.anchoredPosition = hpBarBasePos + new Vector2(sx, sy);
        }
        else
        {
            hpBarBg.anchoredPosition = hpBarBasePos;
        }
    }

    void BuildBatteryBar(RectTransform parent, float yOffset)
    {
        // 아이콘 (손전등 심볼)
        var iconGO = new GameObject("Bat_Icon");
        iconGO.transform.SetParent(parent, false);
        var iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 1);
        iconRT.anchorMax = new Vector2(0, 1);
        iconRT.pivot = new Vector2(0, 1);
        iconRT.anchoredPosition = new Vector2(0, -yOffset - 1);
        iconRT.sizeDelta = new Vector2(16, batteryBarHeight + 4);

        batIcon = iconGO.AddComponent<Text>();
        batIcon.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        batIcon.fontSize = 11;
        batIcon.fontStyle = FontStyle.Bold;
        batIcon.color = batteryFullColor;
        batIcon.text = "⚡";
        batIcon.alignment = TextAnchor.MiddleCenter;

        // 배경
        var bgGO = new GameObject("Bat_BG");
        bgGO.transform.SetParent(parent, false);
        batBarBg = bgGO.AddComponent<RectTransform>();
        batBarBg.anchorMin = new Vector2(0, 1);
        batBarBg.anchorMax = new Vector2(0, 1);
        batBarBg.pivot = new Vector2(0, 1);
        batBarBg.anchoredPosition = new Vector2(18, -yOffset);
        batBarBg.sizeDelta = new Vector2(batteryBarWidth + 4, batteryBarHeight + 4);

        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = barBgColor;

        // 채움
        var fillGO = new GameObject("Bat_Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        batBarFill = fillGO.AddComponent<RectTransform>();
        batBarFill.anchorMin = new Vector2(0, 0);
        batBarFill.anchorMax = new Vector2(0, 1);
        batBarFill.pivot = new Vector2(0, 0.5f);
        batBarFill.offsetMin = new Vector2(2, 2);
        batBarFill.offsetMax = new Vector2(-2, -2);
        batBarFill.sizeDelta = new Vector2(batteryBarWidth, 0);

        batFillImage = fillGO.AddComponent<Image>();
        batFillImage.color = batteryFullColor;

        // 텍스트
        var txtGO = new GameObject("Bat_Text");
        txtGO.transform.SetParent(bgGO.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(4, 0);
        txtRT.offsetMax = Vector2.zero;

        batText = txtGO.AddComponent<Text>();
        batText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        batText.fontSize = 9;
        batText.fontStyle = FontStyle.Bold;
        batText.color = Color.white;
        batText.alignment = TextAnchor.MiddleLeft;
        batText.text = "100%";

        var shadow = txtGO.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f);
        shadow.effectDistance = new Vector2(1, -1);
    }

    void UpdateBatteryBar()
    {
        if (batFillImage == null || batBarFill == null || flashlight == null) return;

        float pct = flashlight.BatteryPercent;

        // 크기
        batBarFill.sizeDelta = new Vector2(batteryBarWidth * pct, 0);

        // 색상
        Color c = Color.Lerp(batteryLowColor, batteryFullColor, pct);

        // 낮은 배터리 깜빡임
        if (pct < 0.15f && pct > 0f)
        {
            float blink = Mathf.PingPong(Time.unscaledTime * 3f, 1f);
            c = Color.Lerp(c, new Color(1f, 0.1f, 0.1f), blink * 0.6f);
        }

        batFillImage.color = c;

        // 아이콘 색상 연동
        if (batIcon != null)
        {
            if (flashlight.IsOn)
                batIcon.color = pct < 0.15f ? c : batteryFullColor;
            else
                batIcon.color = new Color(0.4f, 0.4f, 0.4f);
        }

        // 텍스트
        if (batText != null)
        {
            string state = flashlight.IsOn ? "" : " OFF";
            batText.text = $"{pct * 100:F0}%{state}";
        }
    }

    void UpdateStaminaBar()
    {
        if (stFillImage == null || stBarFill == null) return;
        var p = TopDownPlayer.Instance;
        if (p == null) { if (stBarBg != null) stBarBg.gameObject.SetActive(false); return; }

        float pct = p.StaminaPercent;
        bool show = pct < 0.99f;
        stBarBg.gameObject.SetActive(show);
        if (!show) return;

        stBarFill.sizeDelta = new Vector2(staminaBarWidth * pct, 0);
        Color c;
        if (p.IsExhausted)
        {
            float blink = Mathf.PingPong(Time.unscaledTime * 4f, 1f);
            c = Color.Lerp(exhaustedColor, exhaustedColor * 1.3f, blink);
        }
        else
        {
            c = Color.Lerp(staminaLowColor, staminaFullColor, pct);
        }
        stFillImage.color = c;
    }

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
