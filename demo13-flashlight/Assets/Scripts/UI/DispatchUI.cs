using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 파견 보드 UI — 은신처 시설. NPC를 랜드마크에 파견해 인텔(정보)을 수집.
/// 전체화면 "위성/탑다운 지역맵 + 좌측 파란 정보 패널" 레이아웃.
///  - 우측: 위성맵 그레이박스(어두운 격자/지형 배경 + 파견지 마커 버튼).
///  - 좌측: 파견 구성 정보 패널 — ①누굴 보내는가(슬롯) ②목적지 정보(보상/성공률)
///          ③결과 로그 ④맨 아래 "파견 보내기" 버튼.
/// 파견 보드 레벨 ≥ 1 필요. HideoutModuleManager "dispatch" 레벨에 따라 슬롯/목적지 해금.
/// 설계: docs/safehouse-intel.md §2-A, §7-B.
/// </summary>
public class DispatchUI : MonoBehaviour
{
    static DispatchUI instance;
    static GameObject uiRoot;
    static bool isShowing;
    public static bool IsShowing => isShowing;

    // ── 한글 폰트 로더 (LegacyRuntime는 ASCII만이라 한글이 안 나옴) ──
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

    // ── 색 팔레트(전술 톤) ──
    static readonly Color ColDim       = new Color(0.05f, 0.06f, 0.08f, 0.96f);
    static readonly Color ColPanel     = new Color(0.11f, 0.12f, 0.15f, 0.98f);
    static readonly Color ColPanel2    = new Color(0.07f, 0.08f, 0.11f, 0.98f);
    static readonly Color ColPanelBlue = new Color(0.10f, 0.16f, 0.26f, 0.96f); // 좌측 정보 패널(파란톤)
    static readonly Color ColGold      = new Color(0.95f, 0.85f, 0.30f);
    static readonly Color ColText      = new Color(0.93f, 0.93f, 0.92f);
    static readonly Color ColTextDim   = new Color(0.55f, 0.58f, 0.66f);
    static readonly Color ColLine      = new Color(1f, 1f, 1f, 0.12f);
    static readonly Color ColBtn       = new Color(0.16f, 0.18f, 0.24f, 0.95f);
    static readonly Color ColDanger    = new Color(0.5f, 0.22f, 0.22f);
    static readonly Color ColGood       = new Color(0.45f, 0.85f, 0.45f);
    static readonly Color ColWarn       = new Color(1f, 0.55f, 0.45f);

    // 파견 슬롯 (그레이박스: 최대 2)
    static readonly string[] SlotLabels = { "파견 슬롯 1", "파견 슬롯 2" };
    // 슬롯에 배정된 인력(플레이스홀더 — §2-A 등급/특기/상태)
    static readonly string[] SlotPersonnel = { "노을 (숙련 · 정찰)", "지원 인력 (견습 · 운반)" };

    // 목적지 (그레이박스 더미 — 실제는 탐험 해금 기반)
    static readonly string[] Destinations = { "약국 폐허", "경찰서 잔해", "학교 지하", "아파트 단지" };
    static readonly int[]    DestUnlockLevel = { 1, 1, 2, 3 };
    // 맵 위 마커 좌표(맵 영역 RectTransform 기준, pivot 0,1 / 좌상단 원점 — x:+오른쪽, y:-아래쪽)
    static readonly Vector2[] DestMapPos = {
        new Vector2(0.22f, -0.30f), // 약국
        new Vector2(0.62f, -0.22f), // 경찰서
        new Vector2(0.40f, -0.58f), // 학교
        new Vector2(0.75f, -0.66f), // 아파트
    };
    // 목적지별 예상 인텔/보상·성공률(플레이스홀더)
    static readonly string[] DestIntel = {
        "의료용품 은닉처 좌표 · 잠긴 금고 위치",
        "무기고 해제 코드 · 순찰 경로 기록",
        "지하 대피소 입구 · 물자 캐시 좌표",
        "옥상 보급 드롭 · 생존자 은신처 위치",
    };
    static readonly int[] DestSuccess = { 82, 68, 55, 47 }; // 예상 성공률(%)

    struct SlotState
    {
        public bool dispatched;
        public int destinationIdx;
        public float returnTime;
        public string result;
    }

    SlotState[] slots = new SlotState[2];
    int selectedSlot;
    int selectedDest;
    int dispatchLevel = 1;
    Text logText;
    string logContent = "";

    // 상단 탭 뷰 모드 (파견 / 고용)
    enum ViewMode { Dispatch, Hire }
    ViewMode viewMode = ViewMode.Dispatch;

    // ─────────────────────────────────────────────────────────────────
    // 공개 API (보존)
    // ─────────────────────────────────────────────────────────────────

    public static void Show()
    {
        if (isShowing) { Hide(); return; }

        var hm = HideoutModuleManager.Instance;
        int lv = hm != null ? hm.GetLevel("dispatch") : 0;
        if (lv <= 0)
        {
            ToastManager.Show("파견 보드 미건설 (파견 타일 클릭 → 건설)", ToastManager.ToastType.Warning);
            return;
        }

        // 발전기 전력 전제
        if (hm == null || !hm.GeneratorPowered)
        {
            ToastManager.Show("발전기 전력 필요", ToastManager.ToastType.Warning);
            return;
        }

        Open(lv);
    }

    /// <summary>레벨 게이트를 건너뛰고 강제로 연다(레벨 2 가정). 미리보기/디자인 확인용.</summary>
    public static void ShowPreview()
    {
        if (isShowing) Hide();

        var hm = HideoutModuleManager.Instance;
        int lv = hm != null ? hm.GetLevel("dispatch") : 0;
        if (lv < 2) lv = 2; // 게이트 무시 — 레벨 2 가정(슬롯 2 + 목적지 일부 해금)
        Open(lv);
    }

    static void Open(int lv)
    {
        if (instance == null)
        {
            var go = new GameObject("DispatchUI");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<DispatchUI>();
        }
        instance.dispatchLevel = lv;
        instance.BuildUI(lv);
        isShowing = true;
    }

    public static void Hide()
    {
        isShowing = false;
        if (uiRoot != null) { Destroy(uiRoot); uiRoot = null; }
    }

    // ─────────────────────────────────────────────────────────────────

    void Update()
    {
        if (!isShowing) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Hide(); return; }

        // 파견 귀환 체크
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].dispatched && Time.unscaledTime >= slots[i].returnTime && string.IsNullOrEmpty(slots[i].result))
            {
                slots[i].result = GenerateResult(slots[i].destinationIdx);
                string entry = $"<color=#44CC88>[슬롯{i + 1}]</color> {Destinations[slots[i].destinationIdx]} 귀환: {slots[i].result}\n";
                logContent = entry + logContent;
                if (logContent.Length > 2000) logContent = logContent.Substring(0, 2000);
                if (logText != null) logText.text = logContent;
                ToastManager.Show($"파견 귀환! 슬롯{i + 1}", ToastManager.ToastType.Success);
                // 슬롯 상태 텍스트 갱신
                BuildUI(dispatchLevel);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // UI 빌드 — 전체화면 위성맵 + 좌측 정보 패널
    // ─────────────────────────────────────────────────────────────────

    void BuildUI(int dispatchLv)
    {
        if (uiRoot != null) Destroy(uiRoot);

        uiRoot = new GameObject("DispatchUI_Canvas");
        uiRoot.transform.SetParent(transform, false);
        var canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        var scaler = uiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        uiRoot.AddComponent<GraphicRaycaster>();

        // 전체화면 딤 배경(화면 꽉 채움)
        var dim = MakeStretch(uiRoot.transform, "Dim");
        dim.AddComponent<Image>().color = ColDim;

        // ── 상단 헤더바 (전폭, 높이 64) ──
        var header = MakeRect(uiRoot.transform, "Header", new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, 0), new Vector2(0, 64));
        header.GetComponent<RectTransform>().offsetMin = new Vector2(0, -64);
        header.GetComponent<RectTransform>().offsetMax = new Vector2(0, 0);
        header.AddComponent<Image>().color = ColPanel2;

        MakeLabel(header.transform, "Title", "파견 보드", 28, ColGold, TextAnchor.MiddleLeft,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(32, 0), new Vector2(-120, 0), FontStyle.Bold);

        // ── 상단 탭 (파견 / 고용) — 제목 우측 ──
        BuildTab(header.transform, "파견", ViewMode.Dispatch, 220);
        BuildTab(header.transform, "고용", ViewMode.Hire, 332);

        // 헤더 아래 1px 구분선
        var sep = MakeRect(uiRoot.transform, "HeaderSep", new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, 0), new Vector2(0, 1));
        sep.GetComponent<RectTransform>().offsetMin = new Vector2(0, -65);
        sep.GetComponent<RectTransform>().offsetMax = new Vector2(0, -64);
        sep.AddComponent<Image>().color = ColLine;

        // 닫기 ✕ (48x40)
        var closeGO = MakeRect(header.transform, "Close", new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-32, 0), new Vector2(48, 40));
        closeGO.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
        closeGO.AddComponent<Image>().color = ColDanger;
        var closeBtn = closeGO.AddComponent<Button>();
        closeBtn.targetGraphic = closeGO.GetComponent<Image>();
        closeBtn.onClick.AddListener(Hide);
        MakeLabel(closeGO.transform, "X", "✕", 22, ColText, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);

        // ── 본문 컨테이너 (헤더 아래 ~ 화면 하단, 좌우 여백 40) ──
        var body = MakeStretch(uiRoot.transform, "Body");
        var bodyRT = body.GetComponent<RectTransform>();
        bodyRT.offsetMin = new Vector2(40, 28);
        bodyRT.offsetMax = new Vector2(-40, -88); // 헤더(65) + 여백

        if (viewMode == ViewMode.Hire)
        {
            BuildHireView(bodyRT);
        }
        else
        {
            BuildLeftPanel(bodyRT, dispatchLv);
            BuildMapArea(bodyRT, dispatchLv);
        }
    }

    // ── 상단 탭 버튼 (활성 = 골드 밑줄 강조) ──
    void BuildTab(Transform parent, string label, ViewMode mode, float x)
    {
        bool active = viewMode == mode;
        var tabGO = MakeRect(parent, $"Tab_{mode}", new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(x, 0), new Vector2(96, 40));
        tabGO.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
        tabGO.AddComponent<Image>().color = active ? new Color(0.18f, 0.22f, 0.30f, 0.98f) : ColBtn;
        var btn = tabGO.AddComponent<Button>();
        btn.targetGraphic = tabGO.GetComponent<Image>();
        var m = mode;
        btn.onClick.AddListener(() => SelectTab(m));
        MakeLabel(tabGO.transform, "Label", label, 16, active ? ColGold : ColTextDim, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);
        if (active)
        {
            var under = MakeRect(tabGO.transform, "Underline", new Vector2(0, 0), new Vector2(1, 0),
                Vector2.zero, new Vector2(0, 3));
            var urt = under.GetComponent<RectTransform>();
            urt.pivot = new Vector2(0.5f, 0f);
            urt.offsetMin = new Vector2(0, 0); urt.offsetMax = new Vector2(0, 3);
            under.AddComponent<Image>().color = ColGold;
        }
    }

    void SelectTab(ViewMode mode)
    {
        if (viewMode == mode) return;
        viewMode = mode;
        if (mode == ViewMode.Hire)
            DispatchRoster.Instance.RefreshPool(); // 고용 페이지 열 때 풀 갱신
        BuildUI(dispatchLevel);
    }

    // ── 좌측 정보 패널 (파란톤, 폭 360) ──
    void BuildLeftPanel(RectTransform body, int dispatchLv)
    {
        const float W = 360f;
        var panel = MakeRect(body, "LeftPanel", new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(0, 0), new Vector2(W, 0));
        var prt = panel.GetComponent<RectTransform>();
        prt.pivot = new Vector2(0, 0.5f);
        prt.anchoredPosition = new Vector2(0, 0);
        prt.offsetMin = new Vector2(0, 0);
        prt.offsetMax = new Vector2(W, 0);
        panel.AddComponent<Image>().color = ColPanelBlue;

        float pad = 16f;
        float y = -pad;
        float innerW = W - pad * 2;

        // ① 누굴 보내는가 — 파견 슬롯/인력
        y = SectionHeader(prt, "① 파견 인력", pad, y, innerW);
        int maxSlots = dispatchLv >= 2 ? 2 : 1;
        for (int i = 0; i < SlotLabels.Length; i++)
        {
            bool unlocked = i < maxSlots;
            bool isSel = unlocked && i == selectedSlot;
            string status = !unlocked ? "잠김"
                : slots[i].dispatched ? (string.IsNullOrEmpty(slots[i].result) ? "파견 중..." : "귀환 완료")
                : "대기";
            Color statusCol = !unlocked ? ColTextDim
                : slots[i].dispatched ? (string.IsNullOrEmpty(slots[i].result) ? ColWarn : ColGood)
                : ColTextDim;

            var slotGO = MakeRect(prt, $"Slot_{i}", new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(pad, y), new Vector2(innerW, 52));
            slotGO.AddComponent<Image>().color = isSel ? new Color(0.18f, 0.30f, 0.46f, 0.98f)
                                                       : (unlocked ? ColBtn : ColPanel2);

            // 슬롯 제목 + 상태
            string title = unlocked ? SlotLabels[i] : $"{SlotLabels[i]} [Lv2 필요]";
            MakeLabel(slotGO.transform, "Title", title, 15, unlocked ? ColText : ColTextDim, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -6), new Vector2(-70, -26), FontStyle.Bold);
            MakeLabel(slotGO.transform, "Status", status, 12, statusCol, TextAnchor.UpperRight,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -6), new Vector2(-10, -26), FontStyle.Bold);
            // 인력(등급/특기) — 계약된 roster 인력이 있으면 우선 표기(가벼운 연결)
            string who = unlocked ? GetSlotPersonnel(i) : "—";
            MakeLabel(slotGO.transform, "Who", who, 12, ColTextDim, TextAnchor.LowerLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(10, 6), new Vector2(-10, 26), FontStyle.Normal);

            if (unlocked && !slots[i].dispatched)
            {
                var btn = slotGO.AddComponent<Button>();
                btn.targetGraphic = slotGO.GetComponent<Image>();
                int idx = i;
                btn.onClick.AddListener(() => SelectSlot(idx));
            }
            else if (unlocked && slots[i].dispatched && !string.IsNullOrEmpty(slots[i].result))
            {
                var btn = slotGO.AddComponent<Button>();
                btn.targetGraphic = slotGO.GetComponent<Image>();
                int idx = i;
                btn.onClick.AddListener(() => CollectResult(idx));
            }

            y -= 58f;
        }
        y -= 8f;

        // ② 선택한 목적지 정보
        y = SectionHeader(prt, "② 목적지 정보", pad, y, innerW);
        var destBox = MakeRect(prt, "DestBox", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(pad, y), new Vector2(innerW, 108));
        destBox.AddComponent<Image>().color = ColPanel2;
        bool destUnlocked = dispatchLv >= DestUnlockLevel[selectedDest];
        string dName = Destinations[selectedDest];
        MakeLabel(destBox.transform, "DName", dName, 16, destUnlocked ? ColGold : ColTextDim, TextAnchor.UpperLeft,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -8), new Vector2(-10, -32), FontStyle.Bold);
        MakeLabel(destBox.transform, "DIntel", $"예상 인텔: {DestIntel[selectedDest]}", 12, ColText, TextAnchor.UpperLeft,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -34), new Vector2(-10, -78), FontStyle.Normal);
        int sr = DestSuccess[selectedDest];
        Color srCol = sr >= 70 ? ColGood : sr >= 50 ? ColGold : ColWarn;
        MakeLabel(destBox.transform, "DRate", $"예상 성공률: <color=#{ToHex(srCol)}>{sr}%</color>", 13, ColText, TextAnchor.LowerLeft,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(10, 8), new Vector2(-10, 28), FontStyle.Bold);
        y -= 116f;

        // ③ 결과 로그
        y = SectionHeader(prt, "③ 파견 결과", pad, y, innerW);
        // 로그 박스 = 남은 공간(파견 버튼 영역 위까지)
        var logBg = MakeRect(prt, "LogBg", new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(0, 0), new Vector2(0, 0));
        var logRT = logBg.GetComponent<RectTransform>();
        logRT.pivot = new Vector2(0.5f, 0.5f);
        logRT.offsetMin = new Vector2(pad, 64);          // 하단: 파견 버튼(48) + 여백
        logRT.offsetMax = new Vector2(-pad, y);          // 상단: 섹션 헤더 아래
        logBg.AddComponent<Image>().color = ColPanel2;

        var logGO = new GameObject("LogText");
        logGO.transform.SetParent(logRT, false);
        var ltRT = logGO.AddComponent<RectTransform>();
        ltRT.anchorMin = Vector2.zero; ltRT.anchorMax = Vector2.one;
        ltRT.offsetMin = new Vector2(10, 8); ltRT.offsetMax = new Vector2(-10, -8);
        logText = logGO.AddComponent<Text>();
        logText.font = KR;
        logText.fontSize = 12;
        logText.color = ColText;
        logText.alignment = TextAnchor.UpperLeft;
        logText.supportRichText = true;
        logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logText.verticalOverflow = VerticalWrapMode.Overflow;
        logText.text = logContent.Length > 0 ? logContent : "<color=#556677>아직 파견 기록 없음</color>";

        // ④ 파견 보내기 버튼 (맨 아래)
        var sendGO = MakeRect(prt, "SendBtn", new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 0), new Vector2(0, 48));
        var srt = sendGO.GetComponent<RectTransform>();
        srt.pivot = new Vector2(0.5f, 0f);
        srt.offsetMin = new Vector2(pad, 8);
        srt.offsetMax = new Vector2(-pad, 56);
        sendGO.AddComponent<Image>().color = new Color(0.22f, 0.40f, 0.30f, 0.98f);
        var sendBtn = sendGO.AddComponent<Button>();
        sendBtn.targetGraphic = sendGO.GetComponent<Image>();
        sendBtn.onClick.AddListener(DispatchSelected);
        MakeLabel(sendGO.transform, "Label", "파견 보내기", 16, ColText, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);
    }

    // ─────────────────────────────────────────────────────────────────
    // 고용 뷰 — 좌: 회수꾼(랜덤) 풀 / 우: 내 파티(계약됨)
    // ─────────────────────────────────────────────────────────────────
    void BuildHireView(RectTransform body)
    {
        var roster = DispatchRoster.Instance;
        const float Gap = 16f;

        // ── 좌측: 회수꾼 (랜덤) 풀 ──
        var leftPanel = MakeRect(body, "HirePool", new Vector2(0, 0), new Vector2(0.5f, 1),
            Vector2.zero, Vector2.zero);
        var lrt = leftPanel.GetComponent<RectTransform>();
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.offsetMin = new Vector2(0, 0); lrt.offsetMax = new Vector2(-Gap / 2f, 0);
        leftPanel.AddComponent<Image>().color = ColPanel;

        float pad = 16f;
        var lprt = leftPanel.GetComponent<RectTransform>();
        // 헤더 + 새로고침 버튼
        SectionHeaderFull(lprt, "회수꾼 (랜덤)", pad);
        var refreshGO = MakeRect(lprt, "Refresh", new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-pad - 96, -10), new Vector2(96, 28));
        refreshGO.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        refreshGO.AddComponent<Image>().color = ColBtn;
        var refreshBtn = refreshGO.AddComponent<Button>();
        refreshBtn.targetGraphic = refreshGO.GetComponent<Image>();
        refreshBtn.onClick.AddListener(RefreshHirePool);
        MakeLabel(refreshGO.transform, "L", "↻ 새로고침", 12, ColText, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);

        float y = -48f;
        var pool = roster.Pool;
        if (pool.Count == 0)
        {
            MakeLabel(lprt, "Empty", "후보 없음 — 새로고침", 13, ColTextDim, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(pad, y - 30), new Vector2(-pad, y), FontStyle.Normal);
        }
        for (int i = 0; i < pool.Count; i++)
        {
            var u = pool[i];
            int cost = DispatchRoster.HireCost(u.grade);
            var rowGO = MakeRect(lprt, $"Cand_{i}", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, y), new Vector2(0, 64));
            var rowRT = rowGO.GetComponent<RectTransform>();
            rowRT.pivot = new Vector2(0.5f, 1f);
            rowRT.offsetMin = new Vector2(pad, y - 64); rowRT.offsetMax = new Vector2(-pad, y);
            rowGO.AddComponent<Image>().color = ColPanel2;

            MakeLabel(rowGO.transform, "Name", u.name, 16, ColText, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -8), new Vector2(-110, -30), FontStyle.Bold);
            MakeLabel(rowGO.transform, "Spec",
                $"{DispatchRoster.GradeName(u.grade)} · {DispatchRoster.SpecialtyName(u.specialty)}",
                13, ColTextDim, TextAnchor.LowerLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(12, 8), new Vector2(-110, 30), FontStyle.Normal);

            // 계약 버튼 (비용 표기)
            var hireGO = MakeRect(rowGO.transform, "Hire", new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-12, 0), new Vector2(92, 44));
            hireGO.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
            hireGO.AddComponent<Image>().color = new Color(0.22f, 0.40f, 0.30f, 0.98f);
            var hireBtn = hireGO.AddComponent<Button>();
            hireBtn.targetGraphic = hireGO.GetComponent<Image>();
            int idx = i;
            hireBtn.onClick.AddListener(() => HireCandidate(idx));
            MakeLabel(hireGO.transform, "C", $"계약\n◈{cost:N0}", 12, ColText, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);

            y -= 72f;
        }

        // ── 우측: 내 파티 (계약됨) ──
        var rightPanel = MakeRect(body, "HireRoster", new Vector2(0.5f, 0), new Vector2(1, 1),
            Vector2.zero, Vector2.zero);
        var rrt = rightPanel.GetComponent<RectTransform>();
        rrt.pivot = new Vector2(0.5f, 0.5f);
        rrt.offsetMin = new Vector2(Gap / 2f, 0); rrt.offsetMax = new Vector2(0, 0);
        rightPanel.AddComponent<Image>().color = ColPanelBlue;
        var rprt = rightPanel.GetComponent<RectTransform>();

        SectionHeaderFull(rprt, "내 파티 (계약됨)", pad);
        float ry = -48f;
        var party = roster.Roster;
        if (party.Count == 0)
        {
            MakeLabel(rprt, "Empty", "계약된 인력 없음 — 좌측에서 회수꾼을 계약하세요", 13, ColTextDim, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(pad, ry - 40), new Vector2(-pad, ry), FontStyle.Normal);
        }
        for (int i = 0; i < party.Count; i++)
        {
            var u = party[i];
            var rowGO = MakeRect(rprt, $"Party_{i}", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, ry), new Vector2(0, 56));
            var rowRT = rowGO.GetComponent<RectTransform>();
            rowRT.pivot = new Vector2(0.5f, 1f);
            rowRT.offsetMin = new Vector2(pad, ry - 56); rowRT.offsetMax = new Vector2(-pad, ry);
            rowGO.AddComponent<Image>().color = ColPanel2;

            MakeLabel(rowGO.transform, "Name", u.name, 16, ColGold, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(12, 0), new Vector2(0, 0), FontStyle.Bold);
            MakeLabel(rowGO.transform, "Spec",
                $"{DispatchRoster.GradeName(u.grade)} · {DispatchRoster.SpecialtyName(u.specialty)}",
                13, ColText, TextAnchor.MiddleRight,
                new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-12, 0), FontStyle.Normal);

            ry -= 64f;
        }
    }

    void RefreshHirePool()
    {
        DispatchRoster.Instance.RefreshPool();
        ToastManager.Show("회수꾼 후보 갱신", ToastManager.ToastType.Info);
        BuildUI(dispatchLevel);
    }

    void HireCandidate(int poolIndex)
    {
        var (ok, reason) = DispatchRoster.Instance.Hire(poolIndex);
        ToastManager.Show(reason, ok ? ToastManager.ToastType.Success : ToastManager.ToastType.Warning);
        if (ok) BuildUI(dispatchLevel);
    }

    /// <summary>패널 전폭 섹션 헤더(좌상단 제목 + 아래 라인). 고용 뷰 전용.</summary>
    void SectionHeaderFull(RectTransform parent, string title, float pad)
    {
        MakeLabel(parent, $"HSec_{title}", title, 16, ColGold, TextAnchor.UpperLeft,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(pad, -34), new Vector2(-pad, -10), FontStyle.Bold);
        var line = MakeRect(parent, $"HSecLine_{title}", new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -40), new Vector2(0, 1));
        var lrt = line.GetComponent<RectTransform>();
        lrt.pivot = new Vector2(0.5f, 1f);
        lrt.offsetMin = new Vector2(pad, -41); lrt.offsetMax = new Vector2(-pad, -40);
        line.AddComponent<Image>().color = ColLine;
    }

    // ── 우측 위성/탑다운 지역 맵 영역 ──
    void BuildMapArea(RectTransform body, int dispatchLv)
    {
        const float LeftW = 360f;
        const float Gap = 16f;
        var map = MakeRect(body, "MapArea", new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(0, 0), new Vector2(0, 0));
        var mrt = map.GetComponent<RectTransform>();
        mrt.pivot = new Vector2(0.5f, 0.5f);
        mrt.offsetMin = new Vector2(LeftW + Gap, 0);
        mrt.offsetMax = new Vector2(0, 0);
        map.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.09f, 0.98f); // 어두운 지형 배경

        // 격자 그레이박스(가로/세로 라인 몇 개) — 위성맵 느낌
        const int Cols = 8, Rows = 6;
        for (int c = 1; c < Cols; c++)
        {
            var v = MakeRect(mrt, $"GridV_{c}", new Vector2((float)c / Cols, 0), new Vector2((float)c / Cols, 1),
                Vector2.zero, new Vector2(1, 0));
            var vrt = v.GetComponent<RectTransform>();
            vrt.pivot = new Vector2(0.5f, 0.5f);
            vrt.offsetMin = new Vector2(-0.5f, 0); vrt.offsetMax = new Vector2(0.5f, 0);
            v.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
        }
        for (int r = 1; r < Rows; r++)
        {
            var h = MakeRect(mrt, $"GridH_{r}", new Vector2(0, (float)r / Rows), new Vector2(1, (float)r / Rows),
                Vector2.zero, new Vector2(0, 1));
            var hrt = h.GetComponent<RectTransform>();
            hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.offsetMin = new Vector2(0, -0.5f); hrt.offsetMax = new Vector2(0, 0.5f);
            h.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
        }

        // 지형 블록 그레이박스(건물 윤곽 느낌의 어두운 사각형 몇 개)
        AddTerrainBlock(mrt, new Vector2(0.12f, -0.18f), new Vector2(180, 130), new Color(0.13f, 0.15f, 0.14f, 0.9f));
        AddTerrainBlock(mrt, new Vector2(0.55f, -0.12f), new Vector2(220, 150), new Color(0.12f, 0.14f, 0.16f, 0.9f));
        AddTerrainBlock(mrt, new Vector2(0.32f, -0.5f), new Vector2(160, 170), new Color(0.14f, 0.13f, 0.12f, 0.9f));
        AddTerrainBlock(mrt, new Vector2(0.68f, -0.55f), new Vector2(200, 160), new Color(0.11f, 0.13f, 0.15f, 0.9f));

        // 맵 라벨(좌상단)
        MakeLabel(mrt, "MapLabel", "지역 위성 정찰도 (그레이박스)", 13, ColTextDim, TextAnchor.UpperLeft,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -8), new Vector2(-12, -30), FontStyle.Normal);

        // 파견지 마커 버튼들
        for (int i = 0; i < Destinations.Length; i++)
        {
            bool unlocked = dispatchLv >= DestUnlockLevel[i];
            bool isSel = i == selectedDest;

            var markerGO = MakeRect(mrt, $"Marker_{i}", new Vector2(DestMapPos[i].x, 1 + DestMapPos[i].y),
                new Vector2(DestMapPos[i].x, 1 + DestMapPos[i].y), Vector2.zero, new Vector2(150, 54));
            var markRT = markerGO.GetComponent<RectTransform>();
            markRT.pivot = new Vector2(0.5f, 0.5f);
            markRT.anchoredPosition = Vector2.zero;

            Color mcol = !unlocked ? new Color(0.12f, 0.12f, 0.14f, 0.92f)
                : isSel ? new Color(0.20f, 0.34f, 0.50f, 0.98f)
                : new Color(0.16f, 0.20f, 0.28f, 0.95f);
            markerGO.AddComponent<Image>().color = mcol;

            // 선택 시 골드 테두리(작은 상단 바)
            if (isSel)
            {
                var topbar = MakeRect(markRT, "Sel", new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 3));
                var tbrt = topbar.GetComponent<RectTransform>();
                tbrt.pivot = new Vector2(0.5f, 1f);
                tbrt.offsetMin = new Vector2(0, -3); tbrt.offsetMax = new Vector2(0, 0);
                topbar.AddComponent<Image>().color = ColGold;
            }

            string label = unlocked ? Destinations[i] : $"[Lv{DestUnlockLevel[i]}]";
            MakeLabel(markRT, "Label", label, 13, unlocked ? ColText : ColTextDim, TextAnchor.UpperCenter,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(4, -6), new Vector2(-4, -26), FontStyle.Bold);
            string sub = unlocked ? $"성공 {DestSuccess[i]}%" : "미해금";
            MakeLabel(markRT, "Sub", sub, 11, unlocked ? ColTextDim : ColTextDim, TextAnchor.LowerCenter,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(4, 6), new Vector2(-4, 24), FontStyle.Normal);

            if (unlocked)
            {
                var btn = markerGO.AddComponent<Button>();
                btn.targetGraphic = markerGO.GetComponent<Image>();
                int d = i;
                btn.onClick.AddListener(() => SelectDest(d));
            }
        }
    }

    void AddTerrainBlock(RectTransform parent, Vector2 anchorNorm, Vector2 size, Color color)
    {
        var go = MakeRect(parent, "Terrain", new Vector2(anchorNorm.x, 1 + anchorNorm.y),
            new Vector2(anchorNorm.x, 1 + anchorNorm.y), Vector2.zero, size);
        var rt = go.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        go.AddComponent<Image>().color = color;
    }

    /// <summary>섹션 헤더 텍스트 + 아래 라인. 다음 콘텐츠 시작 y를 반환.</summary>
    float SectionHeader(RectTransform parent, string title, float pad, float y, float innerW)
    {
        MakeLabel(parent, $"Sec_{title}", title, 14, ColGold, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(pad, y - 22), new Vector2(pad + innerW, y), FontStyle.Bold);
        var line = MakeRect(parent, $"SecLine_{title}", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(pad, y - 24), new Vector2(innerW, 1));
        line.AddComponent<Image>().color = ColLine;
        return y - 32f;
    }

    // ─────────────────────────────────────────────────────────────────
    // 로직 (보존)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>슬롯 i의 인력 표기. 계약된 roster 인력이 있으면 그 인력을, 없으면 플레이스홀더.</summary>
    string GetSlotPersonnel(int i)
    {
        var roster = DispatchRoster.Instance.Roster;
        if (i < roster.Count)
        {
            var u = roster[i];
            return $"{u.name} ({DispatchRoster.GradeName(u.grade)} · {DispatchRoster.SpecialtyName(u.specialty)})";
        }
        return SlotPersonnel[i];
    }

    void SelectSlot(int idx)
    {
        selectedSlot = idx;
        ToastManager.Show($"{SlotLabels[idx]} 선택", ToastManager.ToastType.Info);
        BuildUI(dispatchLevel);
    }

    void SelectDest(int idx)
    {
        selectedDest = idx;
        ToastManager.Show($"목적지: {Destinations[idx]}", ToastManager.ToastType.Info);
        BuildUI(dispatchLevel);
    }

    void DispatchSelected()
    {
        if (slots[selectedSlot].dispatched)
        {
            ToastManager.Show("이미 파견 중", ToastManager.ToastType.Warning);
            return;
        }
        if (dispatchLevel < DestUnlockLevel[selectedDest])
        {
            ToastManager.Show($"목적지 미해금 (Lv{DestUnlockLevel[selectedDest]} 필요)", ToastManager.ToastType.Warning);
            return;
        }

        slots[selectedSlot].dispatched = true;
        slots[selectedSlot].destinationIdx = selectedDest;
        slots[selectedSlot].returnTime = Time.unscaledTime + 30f; // 그레이박스: 30초
        slots[selectedSlot].result = null;

        string entry = $"<color=#AABB44>[파견]</color> 슬롯{selectedSlot + 1} → {Destinations[selectedDest]} (30초)\n";
        logContent = entry + logContent;
        if (logText != null) logText.text = logContent;

        ToastManager.Show($"파견 출발: {Destinations[selectedDest]}", ToastManager.ToastType.Info);
        Debug.Log($"[Dispatch] 슬롯{selectedSlot + 1} → {Destinations[selectedDest]}");

        BuildUI(dispatchLevel);
    }

    void CollectResult(int idx)
    {
        if (!slots[idx].dispatched || string.IsNullOrEmpty(slots[idx].result)) return;

        ToastManager.Show($"인텔 수집: {slots[idx].result}", ToastManager.ToastType.Success);
        slots[idx].dispatched = false;
        slots[idx].result = null;

        BuildUI(dispatchLevel);
    }

    string GenerateResult(int destIdx)
    {
        string[][] intelPool = {
            new[] { "약품 창고 뒤편에 잠긴 금고 발견", "약국 지하 통로 확인", "의료용품 은닉처 좌표 획득" },
            new[] { "경찰서 무기고 잠금 해제 코드 확보", "증거 보관실 위치 파악", "순찰 경로 기록 입수" },
            new[] { "학교 지하 대피소 입구 발견", "교사 일지에서 대피 경로 확인", "지하 물자 캐시 좌표" },
            new[] { "아파트 옥상 보급 드롭 확인", "생존자 은신처 위치 파악", "비상 사다리 경로 발견" },
        };
        var pool = intelPool[Mathf.Clamp(destIdx, 0, intelPool.Length - 1)];
        return pool[Random.Range(0, pool.Length)];
    }

    // ─────────────────────────────────────────────────────────────────
    // UI 유틸
    // ─────────────────────────────────────────────────────────────────

    static string ToHex(Color c)
    {
        return ColorUtility.ToHtmlStringRGB(c);
    }

    /// <summary>부모를 가득 채우는 stretch RectTransform.</summary>
    GameObject MakeStretch(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }

    GameObject MakeRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return go;
    }

    /// <summary>앵커 + offset 방식 텍스트(부모 영역 안에서 자동 배치).</summary>
    Text MakeLabel(Transform parent, string name, string content, int fontSize, Color color, TextAnchor align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, FontStyle style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        var txt = go.AddComponent<Text>();
        txt.font = KR;
        txt.fontSize = fontSize; txt.color = color; txt.text = content;
        txt.alignment = align; txt.supportRichText = true;
        txt.fontStyle = style;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }
}
