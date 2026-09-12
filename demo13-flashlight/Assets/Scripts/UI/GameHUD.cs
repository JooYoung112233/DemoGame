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

    // uGUI 요소
    [Header("uGUI References (auto-filled by GenerateUI)")]
    [SerializeField] Canvas canvas;
    [SerializeField] CanvasScaler scaler;
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

        UpdateSurvivalWarning();
        UpdateAmmo();
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
    }

    #region UI 빌드

    public void GenerateUI()
    {
        // 이미 Canvas가 있으면 스킵
        canvas = GetComponentInChildren<Canvas>();
        if (canvas != null) return;

        // Canvas 생성
        var canvasGO = new GameObject("HUD_Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        UITheme.ConfigureCanvasScale(scaler);
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

        // ── 하이드아웃 나가기 버튼 (우상단, Hideout일 때만 표시) ──
        BuildHideoutExitButton(canvasRT);

        // ── 생존 위험 경고 (상단 중앙) ──
        BuildSurvivalWarning(canvasRT);
        BuildAmmo(canvasRT);
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

    // ── 탄약 표시 (2026-07-29 총기) ──────────────────────────────────────
    //   우하단. 총을 안 들었으면 아예 안 보인다 — 근접만 쓰는 판에 빈 칸을 남기지 않는다.
    [SerializeField] Text ammoText;

    void BuildAmmo(RectTransform canvasRT)
    {
        var go = new GameObject("AmmoText");
        go.transform.SetParent(canvasRT, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-28, 96);
        rt.sizeDelta = new Vector2(260, 44);

        ammoText = go.AddComponent<Text>();
        ammoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ammoText.fontSize = 26;
        ammoText.fontStyle = FontStyle.Bold;
        ammoText.alignment = TextAnchor.LowerRight;
        ammoText.horizontalOverflow = HorizontalWrapMode.Overflow;
        ammoText.text = "";
        go.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.85f);
        go.SetActive(false);
    }

    void UpdateAmmo()
    {
        // ★ GameHUD는 **프리팹 베이크 대상**이다(UIPrefabBaker). 이미 구워진 프리팹엔 이 위젯이 없어
        //   ammoText가 null이고, GenerateUI는 IsGenerated 가드에 막혀 다시 안 돈다 →
        //   재베이크 전까지 탄약 표시가 **조용히 안 뜬다**. 없으면 여기서 직접 만든다.
        //   (A타입 패널의 "프리팹 없으면 코드 생성으로 폴백"과 같은 규약.)
        if (ammoText == null)
        {
            if (canvas == null) canvas = GetComponentInChildren<Canvas>();
            var rt = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            if (rt == null) return;
            BuildAmmo(rt);
            if (ammoText == null) return;
        }

        var player = TopDownPlayer.Instance;
        var gun = player != null ? player.GetComponent<PlayerGun>() : null;

        bool show = gun != null && player.IsRangedEquipped;
        if (ammoText.gameObject.activeSelf != show) ammoText.gameObject.SetActive(show);
        if (!show) return;

        if (gun.IsReloading)
        {
            ammoText.text = $"장전 {Mathf.RoundToInt(gun.ReloadProgress * 100f)}%";
            ammoText.color = new Color(0.86f, 0.74f, 0.42f);      // 금색 — 지금 못 쏜다
            return;
        }
        if (!gun.HasMagazine)
        {
            ammoText.text = "탄창 없음";
            ammoText.color = new Color(0.85f, 0.32f, 0.28f);
            return;
        }

        int a = gun.Ammo, cap = gun.Capacity;
        ammoText.text = $"{a} / {cap}";
        // 남은 탄이 1/4 아래로 떨어지면 붉게 — 장전할 자리를 고르라는 신호.
        ammoText.color = a <= 0 ? new Color(0.85f, 0.32f, 0.28f)
                       : (cap > 0 && a <= cap * 0.25f) ? new Color(0.88f, 0.55f, 0.25f)
                       : new Color(0.92f, 0.90f, 0.84f);
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
        hideoutExitBtnGO.SetActive(HideoutController.IsActive && !HideoutDiorama.Active);
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
        hideoutExitBtnGO = null;
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

    #endregion
}
