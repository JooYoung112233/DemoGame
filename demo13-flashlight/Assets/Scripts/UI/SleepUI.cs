using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 수면 UI — 침대(InteractType.Bed) 클릭 시 연다(온디맨드 생성).
/// 수면 시간 선택(4h/8h) → HP·스태미너 회복 + 수분/포만감 차감. 현재 상태 표시.
/// 차감/회복 수치는 SurvivalStats·Health·TopDownPlayer에 적용.
/// </summary>
public class SleepUI : MonoBehaviour
{
    public static SleepUI Instance { get; private set; }
    public bool IsShowing => panel != null && panel.activeSelf;
    public bool IsGenerated => canvas != null;

    struct Option { public string label; public float hours; public float hpPct; public float water; public float satiety; }

    // 수면 시간별 회복/차감 (그레이박스 기본값 — 8h ≈ 각 -25)
    static readonly Option[] Options =
    {
        new Option { label = "4시간 수면", hours = 4f, hpPct = 0.40f, water = 18f, satiety = 18f },
        new Option { label = "8시간 수면", hours = 8f, hpPct = 1.00f, water = 38f, satiety = 38f },
    };

    Canvas canvas;
    GameObject panel;
    Text statusText;
    Font font;
    Image fadeOverlay;   // 폴백 페이드용(ScreenEffectManager 부재 시)
    bool sleeping;        // 휴식 처리 중복 방지

    void Awake() { if (Instance == null) Instance = this; }

    public static SleepUI Show()
    {
        if (Instance == null)
        {
            var go = new GameObject("SleepUI");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<SleepUI>();
        }
        Instance.Open();
        return Instance;
    }

    public void Open()
    {
        if (!IsGenerated) GenerateUI();
        panel.SetActive(true);
        RefreshStatus();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    void RefreshStatus()
    {
        if (statusText == null) return;
        var go = GameObject.FindGameObjectWithTag("Player");
        var health = go != null ? go.GetComponent<Health>() : null;
        var s = SurvivalStats.Get();
        string hp = health != null ? $"{health.CurrentHp:F0}/{health.MaxHp:F0}" : "-";
        string water = s != null ? $"{s.Water:F0}/100" : "-";
        string food = s != null ? $"{s.Satiety:F0}/100" : "-";
        statusText.text = $"현재 상태    HP {hp}    수분 {water}    포만감 {food}";
    }

    void DoSleep(Option o)
    {
        if (sleeping) return;
        sleeping = true;
        StartCoroutine(SleepRoutine(o));
    }

    IEnumerator SleepRoutine(Option o)
    {
        const float fadeDur = 0.4f;

        // ── 페이드 아웃 ──
        var sem = ScreenEffectManager.Instance;
        if (sem != null && sem.IsGenerated)
        {
            bool done = false;
            sem.FadeOut(fadeDur, () => done = true);
            while (!done) yield return null;
        }
        else
        {
            yield return Fade(0f, 1f, fadeDur); // 폴백
        }

        // ── 수면 처리(회복/차감/스토리/세이브) ──
        var go = GameObject.FindGameObjectWithTag("Player");
        var health = go != null ? go.GetComponent<Health>() : null;
        if (health != null) health.Heal(health.MaxHp * o.hpPct);
        TopDownPlayer.Instance?.RefillStamina();
        SurvivalStats.Get()?.Consume(o.water, o.satiety);

        // 시간 경과 — 모든 지역 시계를 수면 시간만큼 진행(밤→낮 등).
        if (RegionTimeManager.Instance != null) RegionTimeManager.Instance.AdvanceAllRegions(o.hours);

        // 침대 휴식 스토리 트리거 유지
        if (StoryTriggerManager.Instance != null) StoryTriggerManager.Instance.OnBedRest();
        // 침대 수면 = 수동 저장 체크포인트(안전가옥 시설이므로 항상 Commit).
        // SaveCheckpoints가 없으면 기존 직접 저장으로 폴백.
        if (SaveCheckpoints.Instance != null) SaveCheckpoints.Instance.BedSleepSave();
        else if (SaveManager.Instance != null) SaveManager.Instance.AutoSave();

        RefreshStatus();

        // ── 페이드 인 ──
        if (sem != null && sem.IsGenerated)
        {
            bool done = false;
            sem.FadeIn(fadeDur, () => done = true);
            while (!done) yield return null;
        }
        else
        {
            yield return Fade(1f, 0f, fadeDur); // 폴백
        }

        // ── 휴식 완료 표시(회복 요약) ──
        ToastManager.Show(
            $"{o.hours:F0}시간 휴식 — HP +{o.hpPct * 100:F0}%, 스태미너 회복 (시간 경과)",
            ToastManager.ToastType.Success);

        sleeping = false;
        Close();
    }

    // ScreenEffectManager 부재 시 자체 검은 오버레이 페이드(0→1→0)
    IEnumerator Fade(float from, float to, float duration)
    {
        EnsureFadeOverlay();
        var fadeGO = fadeOverlay.gameObject;
        fadeGO.SetActive(true);
        fadeOverlay.raycastTarget = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            fadeOverlay.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        fadeOverlay.color = new Color(0f, 0f, 0f, to);

        if (to <= 0f)
        {
            fadeOverlay.raycastTarget = false;
            fadeGO.SetActive(false);
        }
    }

    void EnsureFadeOverlay()
    {
        if (fadeOverlay != null) return;
        // 패널 위(자식)로 전체 화면을 덮는 검은 이미지. 같은 캔버스 내 마지막 자식 = 최상단.
        var fadeGO = NewRect("Fade", panel.transform, Vector2.zero, Vector2.one);
        fadeOverlay = fadeGO.AddComponent<Image>();
        fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        fadeGO.SetActive(false);
    }

    // ── UI 빌드 ───────────────────────────────────────────
    void GenerateUI()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGO = new GameObject("Sleep_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 56;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        panel = NewRect("Panel", canvasGO.transform, Vector2.zero, Vector2.one);
        panel.AddComponent<Image>().color = new Color(0.02f, 0.02f, 0.05f, 0.96f);

        var win = NewRect("Window", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var winRT = win.GetComponent<RectTransform>();
        winRT.sizeDelta = new Vector2(720, 460);
        win.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 1f);

        var title = MakeText(win.transform, "수면", 28, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -18), new Vector2(360, 40));
        title.fontStyle = FontStyle.Bold;

        var closeGO = NewRect("Close", win.transform, new Vector2(1, 1), new Vector2(1, 1));
        var cRT = closeGO.GetComponent<RectTransform>();
        cRT.pivot = new Vector2(1, 1); cRT.anchoredPosition = new Vector2(-20, -18); cRT.sizeDelta = new Vector2(44, 34);
        closeGO.AddComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f);
        closeGO.AddComponent<Button>().onClick.AddListener(Close);
        MakeChild(closeGO.transform, "✕", 18, TextAnchor.MiddleCenter);

        statusText = MakeText(win.transform, "현재 상태", 16, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -66), new Vector2(660, 28));
        statusText.color = new Color(0.75f, 0.8f, 0.9f);

        // 수면 시간 옵션 버튼
        float btnW = 320f, btnH = 150f, gap = 24f;
        float totalW = btnW * Options.Length + gap * (Options.Length - 1);
        float startX = -totalW / 2f + btnW / 2f;
        for (int i = 0; i < Options.Length; i++)
        {
            var o = Options[i];
            var btnGO = NewRect($"Opt_{i}", win.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var brt = btnGO.GetComponent<RectTransform>();
            brt.anchoredPosition = new Vector2(startX + i * (btnW + gap), -20);
            brt.sizeDelta = new Vector2(btnW, btnH);
            var img = btnGO.AddComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.3f);
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            var captured = o;
            btn.onClick.AddListener(() => DoSleep(captured));

            var label = MakeChild(btnGO.transform, o.label, 22, TextAnchor.UpperCenter);
            label.fontStyle = FontStyle.Bold;
            label.rectTransform.offsetMin = new Vector2(8, 8);
            label.rectTransform.offsetMax = new Vector2(-8, -14);

            var detail = MakeChild(btnGO.transform,
                $"HP +{o.hpPct * 100:F0}%   스태미너 회복\n수분 -{o.water:F0}   포만감 -{o.satiety:F0}",
                15, TextAnchor.LowerCenter);
            detail.color = new Color(0.8f, 0.85f, 0.92f);
            detail.rectTransform.offsetMin = new Vector2(8, 14);
            detail.rectTransform.offsetMax = new Vector2(-8, -8);
        }

        panel.SetActive(false);
    }

    // ── 헬퍼 ──────────────────────────────────────────────
    static GameObject NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }

    Text MakeText(Transform parent, string text, int size, TextAnchor anchor,
        Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sizeD)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = aMax; rt.anchoredPosition = pos; rt.sizeDelta = sizeD;
        var t = go.AddComponent<Text>();
        t.font = font; t.fontSize = size; t.alignment = anchor; t.color = Color.white; t.text = text;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        return t;
    }

    Text MakeChild(Transform parent, string text, int size, TextAnchor anchor)
    {
        var go = new GameObject("T", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<Text>();
        t.font = font; t.fontSize = size; t.alignment = anchor; t.color = Color.white; t.text = text;
        return t;
    }
}
