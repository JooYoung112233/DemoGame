using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라디오 UI — 은신처 시설. 전체화면 레트로 무전 콘솔.
/// 상단 주파수 튜닝 바(채널 눈금 + ◀▶) / 좌측 도움 문서·힌트 / 우측 수신 로그.
/// 발전기 레벨 ≥ 1 필요. HideoutModuleManager "radio" 레벨에 따라 채널 해금.
/// 설계: docs/safehouse-intel.md §3 / §7-A.
/// </summary>
public class RadioUI : MonoBehaviour
{
    static RadioUI instance;
    static GameObject uiRoot;
    static bool isShowing;
    public static bool IsShowing => isShowing;

    // ── 한글 폰트 로더 ──
    static Font _kr;
    static Font KR
    {
        get
        {
            if (_kr == null)
                _kr = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Dotum", "Arial" }, 16);
            return _kr != null ? _kr : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    // ── 색 팔레트 (Tarkov 웜그레이/올리브 톤 — UITheme 기반) ──
    static readonly Color Panel   = new Color(UITheme.Panel.r,    UITheme.Panel.g,    UITheme.Panel.b,    0.98f);
    static readonly Color Panel2  = new Color(UITheme.PanelAlt.r, UITheme.PanelAlt.g, UITheme.PanelAlt.b, 0.98f);
    static readonly Color Gold    = UITheme.Gold;
    static readonly Color BodyTxt = UITheme.TextBright;
    static readonly Color DimTxt  = UITheme.TextMuted;
    static readonly Color Line    = UITheme.Divider;
    static readonly Color BtnCol  = UITheme.Cell;
    static readonly Color Danger  = UITheme.Danger;
    static readonly Color Good     = UITheme.Positive;
    static readonly Color Warn     = UITheme.Negative;

    // ── 채널 정의 (기존 값 재사용) ──
    static readonly string[] ChannelNames = { "소식 채널", "시장 정보", "구조 신호", "세계관 단편" };
    static readonly string[] ChannelDesc = {
        "근처 지역 동향과 이벤트 정보",
        "시세 변동, 희귀 물자 출현 정보",
        "조난 신호 → 파견 힌트",
        "이 세계의 단서와 기록 조각"
    };
    static readonly int[] ChannelUnlockLevel = { 1, 1, 2, 3 };

    // ── 주파수 다이얼 정의 ──
    // FM 밴드 88.0~108.0 MHz. 각 채널을 밴드 위에 분배 — 손잡이를 끌어 ±tolerance 안에 들면 동조(lock).
    const float FreqMin = 88.0f;
    const float FreqMax = 108.0f;
    const float FreqTolerance = 0.5f;   // 채널 동조 허용 범위 (±)
    // 채널별 실제 주파수(눈금 + 동조 판정용). ChannelNames 순서와 1:1.
    static readonly float[] ChannelFreq = { 89.1f, 94.5f, 100.3f, 106.7f };

    // 채널별 도움 문서·힌트 (좌측 패널 플레이스홀더)
    static readonly string[][] ChannelHints = {
        new[] {
            "· 동향 보고는 지역1 위주로 갱신됩니다.",
            "· 약탈자 무리 목격담 → 레이드 위험도 참고.",
            "· '이벤트' 키워드 = 한시적 보급/조우.",
            "· 갱신 주기: 귀환 시마다 재추첨.",
        },
        new[] {
            "· 시세 상승 품목은 다음 거래에 반영.",
            "· '희귀 물자 출현' = 특정 지역 루팅 기회.",
            "· 회로기판/연료 시세를 주시할 것.",
            "· 블랙마켓 정보와 교차 검증 권장.",
        },
        new[] {
            "· 조난 좌표는 파견 보드와 연동됩니다.",
            "· '지하/옥상' 언급 = 인텔 게이트 단서.",
            "· 구조 신호는 노이즈가 심합니다.",
            "· 수신 강도가 높을수록 신뢰도 ↑.",
        },
        new[] {
            "· 세계관 조각은 아카이브에 누적 기록.",
            "· 군 통신/실험 기록 = 사건 단서.",
            "· '노을' '알파' '게이트' = 핵심 키워드.",
            "· 일부 방송은 특정 조건에서만 잡힘.",
        },
    };

    // ── 인스턴스 상태 ──
    int radioLevel;
    float currentFreq = FreqMin; // 다이얼 현재 주파수 (MHz)
    int tunedChannel = -1;       // 현재 동조된 채널 (-1 = 동조 안 됨, 노이즈)
    string receivedLog = "";

    Text logText;
    Text hintHeaderText;
    Text noiseText;
    Text freqReadout;            // 상단 현재 주파수 숫자 표시
    Slider freqSlider;           // 주파수 다이얼 (드래그로 조절)
    Button receiveBtn;           // 동조 상태에 따라 활성/비활성
    Text receiveBtnLbl;
    RectTransform tuneRowRT;     // 주파수 눈금 행 (채널 틱 마커)
    RectTransform hintListRT;    // 좌측 힌트 리스트

    // ── 외부 API (보존) ──

    public static void Show()
    {
        if (isShowing) { Hide(); return; }

        var hm = HideoutModuleManager.Instance;
        int radioLv = hm != null ? hm.GetLevel("radio") : 0;
        if (radioLv <= 0)
        {
            ToastManager.Show("라디오 미건설 (라디오 타일 클릭 → 건설)", ToastManager.ToastType.Warning);
            return;
        }

        // 발전기 전력 전제
        if (hm == null || !hm.GeneratorPowered)
        {
            ToastManager.Show("발전기 전력 필요", ToastManager.ToastType.Warning);
            return;
        }

        Open(radioLv);
    }

    /// <summary>레벨 게이트를 건너뛰고 강제로 연다 (F1 미리보기용). 임의 레벨 3으로 빌드.</summary>
    public static void ShowPreview()
    {
        if (isShowing) { Hide(); return; }
        Open(3);
    }

    static void Open(int radioLv)
    {
        if (instance == null)
        {
            var go = new GameObject("RadioUI");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<RadioUI>();
        }
        instance.BuildUI(radioLv);
        isShowing = true;
    }

    public static void Hide()
    {
        isShowing = false;
        if (uiRoot != null) { Destroy(uiRoot); uiRoot = null; }
    }

    void Update()
    {
        if (!isShowing) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Hide();
    }

    // ── UI 빌드 ──

    void BuildUI(int radioLv)
    {
        radioLevel = radioLv;
        // 첫 빌드 시 다이얼을 해금된 첫 채널 주파수로 맞춰 둔다.
        currentFreq = ChannelFreq[FirstUnlockedChannel()];

        if (uiRoot != null) Destroy(uiRoot);

        uiRoot = new GameObject("RadioUI_Canvas");
        uiRoot.transform.SetParent(transform, false);
        var canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        var scaler = uiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        uiRoot.AddComponent<GraphicRaycaster>();

        // 루트 = 화면 전체 딤 배경
        var root = MakeStretch(uiRoot.transform, "Root", Vector2.zero, Vector2.zero);
        root.AddComponent<Image>().color = new Color(UITheme.Backdrop.r, UITheme.Backdrop.g, UITheme.Backdrop.b, 0.96f);
        var rootRT = root.GetComponent<RectTransform>();

        BuildHeader(rootRT);
        BuildTuningBar(rootRT);
        BuildBody(rootRT);
        RefreshChannelView();
    }

    // ── 레이아웃 상수 (한 곳에서 관리 — 밴드가 겹치지 않게) ──
    const float HeaderH  = 64f;   // 상단 헤더바 높이
    const float TuneBarH = 168f;  // 주파수 튜닝 바 높이
    const float SideMargin = 40f; // 좌/우 화면 여백
    const float BottomMargin = 40f;

    // 상단 헤더바 (전폭) — 제목 + 닫기 ✕ + 아래 구분선
    void BuildHeader(RectTransform parent)
    {
        // 화면 상단에 폭 전체로 붙는 헤더. 좌우 0 여백(전폭).
        var header = MakeAnchored(parent, "Header", new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0.5f, 1f), new Vector2(0, -HeaderH / 2f), new Vector2(0, HeaderH));
        header.AddComponent<Image>().color = Panel2;
        var headerRT = header.GetComponent<RectTransform>();

        // 제목
        var title = MakeLabel(headerRT, "Title", "라디오", 30, Gold, TextAnchor.MiddleLeft, true);
        var tRT = title.rectTransform;
        tRT.anchorMin = new Vector2(0, 0); tRT.anchorMax = new Vector2(0.7f, 1);
        tRT.offsetMin = new Vector2(SideMargin, 0); tRT.offsetMax = Vector2.zero;

        // 닫기 ✕ (48x40, 우측 정렬)
        var closeGO = MakeAnchored(headerRT, "Close", new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(1, 0.5f), new Vector2(-SideMargin, 0), new Vector2(48, 40));
        closeGO.AddComponent<Image>().color = Danger;
        var closeBtn = closeGO.AddComponent<Button>();
        closeBtn.targetGraphic = closeGO.GetComponent<Image>();
        closeBtn.onClick.AddListener(Hide);
        var cx = MakeLabel(closeGO.GetComponent<RectTransform>(), "X", "✕", 22, BodyTxt, TextAnchor.MiddleCenter, true);
        StretchFull(cx.rectTransform);

        // 헤더 아래 1px 구분선
        var divider = MakeAnchored(parent, "HeaderDivider", new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0.5f, 1f), new Vector2(0, -HeaderH), new Vector2(0, 1));
        divider.AddComponent<Image>().color = Line;
    }

    // 주파수 튜닝 바 (헤더 아래) — 라디오 다이얼: 슬라이더 손잡이를 끌어 주파수를 맞춘다.
    void BuildTuningBar(RectTransform parent)
    {
        const float barHeight = 160f;
        var bar = MakeAnchored(parent, "TuneBar", new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0.5f, 1f), new Vector2(0, -(65 + barHeight / 2f)), new Vector2(0, barHeight));
        bar.AddComponent<Image>().color = Panel;
        var barRT = bar.GetComponent<RectTransform>();
        barRT.offsetMin = new Vector2(40, barRT.offsetMin.y);
        barRT.offsetMax = new Vector2(-40, barRT.offsetMax.y);

        // 섹션 라벨 (좌측 상단)
        var lbl = MakeLabel(barRT, "TuneLabel", "주파수 튜닝", 16, DimTxt, TextAnchor.MiddleLeft, true);
        var lRT = lbl.rectTransform;
        lRT.anchorMin = new Vector2(0, 1); lRT.anchorMax = new Vector2(0.4f, 1);
        lRT.pivot = new Vector2(0, 1);
        lRT.anchoredPosition = new Vector2(20, -8); lRT.sizeDelta = new Vector2(0, 24);

        // 현재 주파수 숫자 (중앙 상단, 크게 — 드래그하면 실시간 갱신)
        freqReadout = MakeLabel(barRT, "FreqReadout", "", 30, Gold, TextAnchor.MiddleCenter, true);
        var frRT = freqReadout.rectTransform;
        frRT.anchorMin = new Vector2(0.4f, 1); frRT.anchorMax = new Vector2(0.6f, 1);
        frRT.pivot = new Vector2(0.5f, 1);
        frRT.anchoredPosition = new Vector2(0, -4); frRT.sizeDelta = new Vector2(0, 34);

        // 노이즈/동조 상태 텍스트 (우측 상단)
        noiseText = MakeLabel(barRT, "Noise", "", 14, Warn, TextAnchor.MiddleRight, false);
        var nRT = noiseText.rectTransform;
        nRT.anchorMin = new Vector2(0.6f, 1); nRT.anchorMax = new Vector2(1, 1);
        nRT.pivot = new Vector2(1, 1);
        nRT.anchoredPosition = new Vector2(-20, -8); nRT.sizeDelta = new Vector2(0, 24);

        // ── 슬라이더 다이얼 ──
        // 트랙(배경) — 슬라이더가 이 위에 얹힘. 좌/우 여백 64로 손잡이가 끝까지 가도 라벨이 잘리지 않게.
        var slider = MakeAnchored(barRT, "FreqSlider", new Vector2(0, 0.5f), new Vector2(1, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0, -14), new Vector2(0, 20));
        var sliderRT = slider.GetComponent<RectTransform>();
        sliderRT.offsetMin = new Vector2(64, sliderRT.offsetMin.y);
        sliderRT.offsetMax = new Vector2(-64, sliderRT.offsetMax.y);
        var sliderBgImg = slider.AddComponent<Image>();
        sliderBgImg.color = Panel2;
        tuneRowRT = sliderRT;   // 채널 틱 마커의 부모 (슬라이더 폭에 맞춰 배치)

        freqSlider = slider.AddComponent<Slider>();
        freqSlider.direction = Slider.Direction.LeftToRight;
        freqSlider.minValue = FreqMin;
        freqSlider.maxValue = FreqMax;
        freqSlider.wholeNumbers = false;

        // Fill (동조 시 색이 켜지는 띠)
        var fillArea = MakeStretch(sliderRT, "Fill Area", new Vector2(8, 6), new Vector2(8, 6));
        var fillAreaRT = fillArea.GetComponent<RectTransform>();
        var fillGO = MakeAnchored(fillAreaRT, "Fill", new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(0, 0.5f), Vector2.zero, new Vector2(10, 0));
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(Gold.r, Gold.g, Gold.b, 0.30f);
        fillImg.raycastTarget = false;
        freqSlider.fillRect = fillGO.GetComponent<RectTransform>();

        // Handle (드래그하는 손잡이 노브)
        var handleArea = MakeStretch(sliderRT, "Handle Slide Area", new Vector2(8, 0), new Vector2(8, 0));
        var handleAreaRT = handleArea.GetComponent<RectTransform>();
        var handleGO = MakeAnchored(handleAreaRT, "Handle", new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 52));
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Gold;
        freqSlider.handleRect = handleGO.GetComponent<RectTransform>();
        freqSlider.targetGraphic = handleImg;

        // 값 변경 → 주파수 갱신 + 동조 판정
        freqSlider.onValueChanged.AddListener(OnDialChanged);
        freqSlider.SetValueWithoutNotify(currentFreq);

        // ── 채널 틱 마커 + 라벨 (주파수 눈금 위 위치) ──
        int n = ChannelNames.Length;
        for (int i = 0; i < n; i++)
        {
            float t01 = Mathf.InverseLerp(FreqMin, FreqMax, ChannelFreq[i]);
            bool unlocked = radioLevel >= ChannelUnlockLevel[i];

            // 세로 틱 선 (드래그를 막지 않도록 raycast 비활성)
            var tick = MakeAnchored(sliderRT, $"Tick_{i}", new Vector2(t01, 0), new Vector2(t01, 1),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3, 26));
            var tickImg = tick.AddComponent<Image>();
            tickImg.color = unlocked ? Gold : DimTxt;
            tickImg.raycastTarget = false;

            // 라벨 (주파수 + 채널명) — 틱 아래쪽. 클릭하면 그 채널 주파수로 스냅(편의).
            string label = unlocked
                ? $"{ChannelFreq[i]:0.0}\n<size=12>{ChannelNames[i]}</size>"
                : $"{ChannelFreq[i]:0.0}\n<size=12>잠김 Lv{ChannelUnlockLevel[i]}</size>";
            var lblGO = MakeAnchored(sliderRT, $"TickLbl_{i}", new Vector2(t01, 0), new Vector2(t01, 0),
                new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(96, 40));
            var lblImg = lblGO.AddComponent<Image>();
            lblImg.color = new Color(0, 0, 0, 0.001f);   // 거의 투명 — 클릭 영역 확보
            var snapBtn = lblGO.AddComponent<Button>();
            snapBtn.targetGraphic = lblImg;
            float snapFreq = ChannelFreq[i];
            snapBtn.onClick.AddListener(() => { if (freqSlider != null) freqSlider.value = snapFreq; });
            var t = MakeLabel(lblGO.GetComponent<RectTransform>(), "Lbl", label, 15, unlocked ? BodyTxt : DimTxt, TextAnchor.UpperCenter, true);
            StretchFull(t.rectTransform);
        }
    }

    // 슬라이더 값 변경 콜백 — 주파수 표시 + 동조 채널 재계산.
    void OnDialChanged(float value)
    {
        currentFreq = value;
        RefreshChannelView();
    }

    // 현재 주파수에 동조된 채널 인덱스 (±FreqTolerance 안에서 가장 가까운 것). 없으면 -1.
    int FindTunedChannel()
    {
        int best = -1;
        float bestDist = FreqTolerance + 0.0001f;
        for (int i = 0; i < ChannelFreq.Length; i++)
        {
            float d = Mathf.Abs(currentFreq - ChannelFreq[i]);
            if (d <= FreqTolerance && d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    // 본문 (헤더+튜닝바 아래 ~ 화면 하단)
    void BuildBody(RectTransform parent)
    {
        const float topInset = 65 + 160 + 8;   // 헤더 + 튜닝바 + 여백
        var body = MakeStretch(parent, "Body", new Vector2(40, 40), new Vector2(40, topInset));
        var bodyRT = body.GetComponent<RectTransform>();

        // 좌측 패널 (~30%) — 도움 문서·힌트
        var leftBg = MakeAnchored(bodyRT, "LeftPanel", new Vector2(0, 0), new Vector2(0.30f, 1),
            new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        var leftRT = leftBg.GetComponent<RectTransform>();
        leftRT.offsetMin = Vector2.zero; leftRT.offsetMax = new Vector2(-12, 0);
        leftBg.AddComponent<Image>().color = Panel;

        hintHeaderText = MakeLabel(leftRT, "HintHeader", "도움 문서 · 힌트", 18, Gold, TextAnchor.UpperLeft, true);
        var hhRT = hintHeaderText.rectTransform;
        hhRT.anchorMin = new Vector2(0, 1); hhRT.anchorMax = new Vector2(1, 1);
        hhRT.pivot = new Vector2(0, 1);
        hhRT.anchoredPosition = new Vector2(18, -14); hhRT.sizeDelta = new Vector2(-36, 28);

        // 힌트 리스트 컨테이너 (RefreshChannelView에서 내용 채움)
        var hintListGO = MakeStretch(leftRT, "HintList", new Vector2(18, 18), new Vector2(18, 52));
        hintListRT = hintListGO.GetComponent<RectTransform>();

        // 우측 패널 (~70%) — 수신
        var rightBg = MakeAnchored(bodyRT, "RightPanel", new Vector2(0.30f, 0), new Vector2(1, 1),
            new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        var rightRT = rightBg.GetComponent<RectTransform>();
        rightRT.offsetMin = new Vector2(12, 0); rightRT.offsetMax = Vector2.zero;
        rightBg.AddComponent<Image>().color = Panel2;

        var recvHeader = MakeLabel(rightRT, "RecvHeader", "수신", 18, Gold, TextAnchor.UpperLeft, true);
        var rhRT = recvHeader.rectTransform;
        rhRT.anchorMin = new Vector2(0, 1); rhRT.anchorMax = new Vector2(1, 1);
        rhRT.pivot = new Vector2(0, 1);
        rhRT.anchoredPosition = new Vector2(20, -14); rhRT.sizeDelta = new Vector2(-260, 28);

        // 수신 버튼 (우측 상단) — 동조 + 해금 상태일 때만 활성 (RefreshChannelView에서 갱신)
        var recvGO = MakeAnchored(rightRT, "ReceiveBtn", new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(1, 1), new Vector2(-20, -14), new Vector2(200, 44));
        recvGO.AddComponent<Image>().color = BtnCol;
        receiveBtn = recvGO.AddComponent<Button>();
        receiveBtn.targetGraphic = recvGO.GetComponent<Image>();
        receiveBtn.onClick.AddListener(Receive);
        receiveBtnLbl = MakeLabel(recvGO.GetComponent<RectTransform>(), "Lbl", "▶  수신", 18, BodyTxt, TextAnchor.MiddleCenter, true);
        StretchFull(receiveBtnLbl.rectTransform);

        // 로그 배경
        var logBg = MakeStretch(rightRT, "LogBg", new Vector2(20, 20), new Vector2(20, 66));
        logBg.AddComponent<Image>().color = new Color(UITheme.Gridline.r, UITheme.Gridline.g, UITheme.Gridline.b, 0.98f);
        var logBgRT = logBg.GetComponent<RectTransform>();

        var logGO = MakeStretch(logBgRT, "LogText", new Vector2(16, 16), new Vector2(16, 16));
        var ltRT = logGO.GetComponent<RectTransform>();
        logText = logGO.AddComponent<Text>();
        logText.font = KR;
        logText.fontSize = 16;
        logText.color = BodyTxt;
        logText.alignment = TextAnchor.UpperLeft;
        logText.supportRichText = true;
        logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logText.verticalOverflow = VerticalWrapMode.Overflow;
        logText.text = receivedLog.Length > 0 ? receivedLog : "<color=#8A8270>주파수를 맞추고 [수신] 버튼을 누르세요…</color>";
    }

    // ── 채널 뷰 갱신 (주파수 표시 / 동조 판정 / 힌트 / 노이즈 / 수신 버튼) ──
    void RefreshChannelView()
    {
        tunedChannel = FindTunedChannel();
        bool tuned = tunedChannel >= 0;
        bool unlocked = tuned && radioLevel >= ChannelUnlockLevel[tunedChannel];

        // 상단 주파수 숫자 (실시간)
        if (freqReadout != null)
            freqReadout.text = $"{currentFreq:0.0} <size=16>MHz</size>";

        // 틱 라벨 색: 동조된 채널만 강조
        for (int i = 0; i < ChannelNames.Length; i++)
        {
            if (tuneRowRT == null) break;
            bool chUnlocked = radioLevel >= ChannelUnlockLevel[i];
            var lbl = tuneRowRT.Find($"TickLbl_{i}")?.Find("Lbl")?.GetComponent<Text>();
            if (lbl != null)
            {
                if (i == tunedChannel)
                    lbl.color = chUnlocked ? Gold : Warn;
                else
                    lbl.color = chUnlocked ? BodyTxt : DimTxt;
            }
        }

        // 노이즈/동조 연출
        if (noiseText != null)
        {
            if (!tuned)
                noiseText.text = "<color=#FF8C73>··· 치지직 ··· 주파수를 맞추세요</color>";
            else if (!unlocked)
                noiseText.text = $"<color=#FF8C73>✕ 신호 잠김 — Lv{ChannelUnlockLevel[tunedChannel]} 필요</color>";
            else
                noiseText.text = $"<color=#73DA73>● 동조 — {ChannelNames[tunedChannel]}</color>";
        }

        // 수신 버튼 활성/비활성 (동조 + 해금일 때만)
        if (receiveBtn != null)
        {
            receiveBtn.interactable = unlocked;
            var bImg = receiveBtn.targetGraphic as Image;
            if (bImg != null) bImg.color = unlocked ? new Color(Gold.r, Gold.g, Gold.b, 0.85f) : BtnCol;
        }
        if (receiveBtnLbl != null)
        {
            receiveBtnLbl.text = unlocked ? "▶  수신" : (tuned ? "🔒 잠김" : "··· 노이즈 ···");
            receiveBtnLbl.color = unlocked ? new Color(0.08f, 0.08f, 0.06f) : DimTxt;
        }

        // 좌측 힌트 헤더 + 리스트
        string headerTag = tuned ? ChannelNames[tunedChannel] : "비동조";
        if (hintHeaderText != null)
            hintHeaderText.text = $"도움 문서 · 힌트  <size=13><color=#8A8270>[{headerTag}]</color></size>";

        if (hintListRT != null)
        {
            for (int i = hintListRT.childCount - 1; i >= 0; i--)
                Destroy(hintListRT.GetChild(i).gameObject);

            float y = 0f;
            if (!tuned)
            {
                var noise = MakeLabel(hintListRT, "Noise",
                    "주파수가 어느 채널에도 맞지 않았습니다.\n손잡이를 끌어 눈금에 동조하세요.", 14, DimTxt, TextAnchor.UpperLeft, false);
                var noRT = noise.rectTransform;
                noRT.anchorMin = new Vector2(0, 1); noRT.anchorMax = new Vector2(1, 1);
                noRT.pivot = new Vector2(0, 1);
                noRT.anchoredPosition = new Vector2(0, y); noRT.sizeDelta = new Vector2(0, 44);
            }
            else if (!unlocked)
            {
                var locked = MakeLabel(hintListRT, "Locked",
                    "이 채널은 아직 해금되지 않았습니다.\n라디오 모듈을 업그레이드하세요.", 14, Warn, TextAnchor.UpperLeft, false);
                var lkRT = locked.rectTransform;
                lkRT.anchorMin = new Vector2(0, 1); lkRT.anchorMax = new Vector2(1, 1);
                lkRT.pivot = new Vector2(0, 1);
                lkRT.anchoredPosition = new Vector2(0, y); lkRT.sizeDelta = new Vector2(0, 44);
            }
            else
            {
                string[] hints = ChannelHints[Mathf.Clamp(tunedChannel, 0, ChannelHints.Length - 1)];
                for (int i = 0; i < hints.Length; i++)
                {
                    var t = MakeLabel(hintListRT, $"Hint_{i}", hints[i], 14, BodyTxt, TextAnchor.UpperLeft, false);
                    var tRT = t.rectTransform;
                    tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
                    tRT.pivot = new Vector2(0, 1);
                    tRT.anchoredPosition = new Vector2(0, y); tRT.sizeDelta = new Vector2(0, 30);
                    y -= 34f;
                }
            }
        }
    }

    // ── 채널 조작 ──

    int FirstUnlockedChannel()
    {
        for (int i = 0; i < ChannelNames.Length; i++)
            if (radioLevel >= ChannelUnlockLevel[i]) return i;
        return 0;
    }

    // ── 수신 (기존 랜덤 메시지 풀 재사용) ──
    // 동조된 채널이 있고 해금돼 있을 때만 호출 (버튼이 그 외엔 비활성).

    void Receive()
    {
        if (tunedChannel < 0)
        {
            ToastManager.Show("주파수 비동조 — 손잡이를 채널에 맞추세요", ToastManager.ToastType.Warning);
            return;
        }
        if (radioLevel < ChannelUnlockLevel[tunedChannel])
        {
            ToastManager.Show("신호 잠김 — 라디오 업그레이드 필요", ToastManager.ToastType.Warning);
            return;
        }

        // TODO(사운드): 동조 시 채널별 무전/노이즈 SFX 재생. 지금은 UI 표기만.

        string[] messages = {
            "...폐상가 북쪽에 새 약탈자 무리 목격...",
            "...구역3 기상 이상 — 안개 주의...",
            "...떠돌이 상인, 내일 고철시장 방문 예정...",
            "...무전: 누구든... 도움... 좌표 전송 중...",
            "...시장 정보: 회로기판 시세 상승 중...",
            "...구조 신호 포착 — 고철시장 지하...",
            "...여긴... 노을... 아직 살아 있다...",
            "...알파 주파수 잡음... 게이트가 열리고 있다...",
            "...이 주파수를 듣는 이가 있다면 — 밤에 나가지 마라...",
        };
        string msg = messages[Random.Range(0, messages.Length)];
        string entry = $"<color=#F2D94D>[{ChannelNames[tunedChannel]} · {ChannelFreq[tunedChannel]:0.0}]</color> {msg}\n";
        receivedLog = entry + receivedLog;

        if (receivedLog.Length > 4000)
            receivedLog = receivedLog.Substring(0, 4000);

        if (logText != null) logText.text = receivedLog;
        Debug.Log($"[Radio] 수신: {msg}");
    }

    // ── UI 유틸 ──

    /// <summary>네 모서리에 맞춰 늘린 RectTransform. inset = (left,bottom)/(right,top) 여백.</summary>
    GameObject MakeStretch(Transform parent, string name, Vector2 insetMin, Vector2 insetMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = insetMin;                       // left, bottom
        rt.offsetMax = new Vector2(-insetMax.x, -insetMax.y); // right, top
        return go;
    }

    GameObject MakeAnchored(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return go;
    }

    void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    Text MakeLabel(Transform parent, string name, string content, int fontSize, Color color, TextAnchor align, bool bold)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(200, 30);
        var txt = go.AddComponent<Text>();
        txt.font = KR;
        txt.fontSize = fontSize; txt.color = color; txt.text = content;
        txt.alignment = align; txt.supportRichText = true;
        txt.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }
}
