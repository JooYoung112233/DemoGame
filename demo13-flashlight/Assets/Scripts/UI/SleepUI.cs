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

    // 수면 시간별 회복/차감.
    // 수치는 GameTuning(sleep4h*/sleep8h*) 경유 — 에셋 없으면 아래 기본값으로 폴백(동일 값).
    static Option[] BuildOptions()
    {
        var t = GameTuning.Instance;
        float h4Hp = t != null ? t.sleep4hHpPct   : 0.40f;
        float h4W  = t != null ? t.sleep4hWater    : 18f;
        float h4S  = t != null ? t.sleep4hSatiety  : 18f;
        float h8Hp = t != null ? t.sleep8hHpPct    : 1.00f;
        float h8W  = t != null ? t.sleep8hWater     : 38f;
        float h8S  = t != null ? t.sleep8hSatiety   : 38f;
        return new[]
        {
            new Option { label = "4시간 수면", hours = 4f, hpPct = h4Hp, water = h4W, satiety = h4S },
            new Option { label = "8시간 수면", hours = 8f, hpPct = h8Hp, water = h8W, satiety = h8S },
        };
    }

    // uGUI (프리팹 베이크 시 직렬화 보존)
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject panel;
    [SerializeField] Text statusText;
    [SerializeField] Button closeBtn;        // 닫기 버튼 — onClick 재부착용 ref
    [SerializeField] Button[] optionBtns;    // 수면 옵션 버튼(인덱스=BuildOptions 순서) — onClick 재부착용 ref
    Font font;            // 빌트인 폰트 — 직렬화 안 함(코드 생성 시점에만 사용)
    Image fadeOverlay;   // 폴백 페이드용(ScreenEffectManager 부재 시) — 런타임 생성
    bool sleeping;        // 휴식 처리 중복 방지

    void Awake()
    {
        if (Instance == null) Instance = this;
        // 정적 텍스트는 빌트인 폰트가 프리팹에 직렬화돼 무해하나, font "필드"는 미직렬화 —
        // 프리팹 경로에서 null로 남는다(HideoutUI 빈 패널 버그와 동일 계열). 방어적으로 항상 보장.
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        WireEvents();   // onClick은 프리팹에 직렬화 안 됨 → 양쪽 경로(프리팹/폴백)에서 항상 재부착
    }

    /// <summary>정적 버튼 onClick 재부착. 프리팹 인스턴스는 GenerateUI를 스킵하므로
    /// 직렬화된 버튼 ref에 리스너를 다시 건다. 옵션 버튼은 인덱스로 BuildOptions 데이터를 재매핑.</summary>
    void WireEvents()
    {
        if (closeBtn != null) { closeBtn.onClick.RemoveAllListeners(); closeBtn.onClick.AddListener(Close); }
        if (optionBtns != null)
        {
            var options = BuildOptions();
            for (int i = 0; i < optionBtns.Length; i++)
            {
                if (optionBtns[i] == null || i >= options.Length) continue;
                var captured = options[i];
                optionBtns[i].onClick.RemoveAllListeners();
                optionBtns[i].onClick.AddListener(() => DoSleep(captured));

                // 라벨/상세 텍스트도 현재 GameTuning 값으로 갱신 — 프리팹엔 베이크 시점 수치가
                // 굳어 있어 튜닝을 바꿔도 표시가 안 따라오던 문제(전체 검수 2026-07-07 잔여분).
                var texts = optionBtns[i].GetComponentsInChildren<Text>(true);
                if (texts.Length >= 1) texts[0].text = captured.label;
                if (texts.Length >= 2) texts[1].text =
                    $"HP +{captured.hpPct * 100:F0}%   스태미너 회복\n수분 -{captured.water:F0}   포만감 -{captured.satiety:F0}";
            }
        }
    }

    public static SleepUI Show()
    {
        if (Instance == null)
        {
            // 프리팹 우선(Instantiate가 Awake로 Instance 세팅), 없으면 코드 생성 폴백.
            var prefab = Resources.Load<GameObject>("UI/SleepUI");
            GameObject go = prefab != null ? Instantiate(prefab) : new GameObject("SleepUI");
            go.name = "SleepUI";
            if (prefab == null) go.AddComponent<SleepUI>();   // 폴백: Open()이 GenerateUI
            DontDestroyOnLoad(go);
        }
        Instance.Open();
        return Instance;
    }

    public void Open()
    {
        if (!IsGenerated) GenerateUI();
        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
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

        // sleeping 고착 방지(전체 검수 2026-07-07 잔여분): 코루틴이 예외/오브젝트 파괴로 중단되거나
        // Fade 콜백이 안 와도 플래그가 반드시 풀리도록 try/finally + 콜백 대기 타임아웃.
        try
        {

        // ── 페이드 아웃 ──
        var sem = ScreenEffectManager.Instance;
        if (sem != null && sem.IsGenerated)
        {
            bool done = false;
            sem.FadeOut(fadeDur, () => done = true);
            float wait = 0f;
            while (!done && (wait += Time.unscaledDeltaTime) < fadeDur + 2f) yield return null;   // 콜백 유실 대비 상한
        }
        else
        {
            yield return Fade(0f, 1f, fadeDur); // 폴백
        }

        // ── 수면 처리(회복/차감/스토리/세이브) ──
        var go = GameObject.FindGameObjectWithTag("Player");
        var health = go != null ? go.GetComponent<Health>() : null;
        // 특성 sleep_recovery(빠른 회복 +0.30 / 악몽 −0.30) — HP 회복량만 스케일(물·포만은 소모라 제외).
        if (health != null) health.Heal(health.MaxHp * o.hpPct * TraitManager.Mod("sleep_recovery"));
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
            float wait = 0f;
            while (!done && (wait += Time.unscaledDeltaTime) < fadeDur + 2f) yield return null;   // 콜백 유실 대비 상한
        }
        else
        {
            yield return Fade(1f, 0f, fadeDur); // 폴백
        }

        // ── 휴식 완료 표시(회복 요약) ──
        ToastManager.Show(
            $"{o.hours:F0}시간 휴식 — HP +{o.hpPct * 100:F0}%, 스태미너 회복 (시간 경과)",
            ToastManager.ToastType.Success);

        Close();

        }
        finally
        {
            sleeping = false;   // 어떤 경로로 끝나든(정상/예외/중단) 반드시 해제
        }
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
        panel.AddComponent<Image>().color = UITheme.Backdrop;

        var win = NewRect("Window", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var winRT = win.GetComponent<RectTransform>();
        winRT.sizeDelta = new Vector2(720, 460);
        win.AddComponent<Image>().color = UITheme.Panel;

        var title = MakeText(win.transform, "수면", 28, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -18), new Vector2(360, 40));
        title.fontStyle = FontStyle.Bold;

        var closeGO = NewRect("Close", win.transform, new Vector2(1, 1), new Vector2(1, 1));
        var cRT = closeGO.GetComponent<RectTransform>();
        cRT.pivot = new Vector2(1, 1); cRT.anchoredPosition = new Vector2(-20, -18); cRT.sizeDelta = new Vector2(44, 34);
        closeGO.AddComponent<Image>().color = UITheme.Danger;
        closeBtn = closeGO.AddComponent<Button>();
        closeBtn.onClick.AddListener(Close);
        MakeChild(closeGO.transform, "✕", 18, TextAnchor.MiddleCenter);

        statusText = MakeText(win.transform, "현재 상태", 16, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -66), new Vector2(660, 28));
        statusText.color = UITheme.TextMuted;

        // 수면 시간 옵션 버튼
        var options = BuildOptions();
        optionBtns = new Button[options.Length];   // 베이크/폴백 시 ref 채움 → WireEvents가 인덱스로 재부착
        float btnW = 320f, btnH = 150f, gap = 24f;
        float totalW = btnW * options.Length + gap * (options.Length - 1);
        float startX = -totalW / 2f + btnW / 2f;
        for (int i = 0; i < options.Length; i++)
        {
            var o = options[i];
            var btnGO = NewRect($"Opt_{i}", win.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var brt = btnGO.GetComponent<RectTransform>();
            brt.anchoredPosition = new Vector2(startX + i * (btnW + gap), -20);
            brt.sizeDelta = new Vector2(btnW, btnH);
            var img = btnGO.AddComponent<Image>();
            img.color = UITheme.Cell;
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            optionBtns[i] = btn;
            var captured = o;
            btn.onClick.AddListener(() => DoSleep(captured));

            var label = MakeChild(btnGO.transform, o.label, 22, TextAnchor.UpperCenter);
            label.fontStyle = FontStyle.Bold;
            label.rectTransform.offsetMin = new Vector2(8, 8);
            label.rectTransform.offsetMax = new Vector2(-8, -14);

            var detail = MakeChild(btnGO.transform,
                $"HP +{o.hpPct * 100:F0}%   스태미너 회복\n수분 -{o.water:F0}   포만감 -{o.satiety:F0}",
                15, TextAnchor.LowerCenter);
            detail.color = UITheme.TextMuted;
            detail.rectTransform.offsetMin = new Vector2(8, 14);
            detail.rectTransform.offsetMax = new Vector2(-8, -8);
        }

        panel.SetActive(false);
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — GenerateUI를 1회 실행해 프리팹화할 계층을 만든다.</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        GenerateUI();   // GenerateUI 첫 줄에서 빌트인 font 설정
    }
#endif

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
