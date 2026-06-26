using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 전체화면 "위성/탑다운 전술 지도" — 레이드 중 N(또는 F1 디버그) 키로 보는 지역 전체 맵.
/// 타르코프식: 실제 지형(존 박스) 위에 마커 — 현재 위치(●)·탈출구(◆)·통로 주석(◇/!).
/// 데이터는 RaidMapManager(발견 존) + InteractableObject(탈출구) + PassageMarker(통로 주석)에서 읽는다.
/// 데이터가 없으면 그레이박스 더미(격자 + 더미 구역 + 플레이어 + 탈출 2개)로 레이아웃을 시연한다(graceful).
///
/// ※ NavigationHUD(상시 나침반 + M 홀드 미니맵)와는 별개 컴포넌트. 이 클래스는 NavigationHUD를 건드리지 않는다.
///   M 키는 NavigationHUD가, F1은 디버그 창(DebugTestUI)이 쓰므로 충돌 방지로 여기선 N 토글만 쓴다.
///   (최초 개방은 F1 디버그 패널의 "예시 UI 미리보기" 버튼/HideoutUI 경유 → 이후 N으로 토글.)
///
/// 자체 self-spawn 싱글톤(DontDestroyOnLoad), 프로시저럴 uGUI(씬 배치 불필요).
/// 참고 디자인: docs/world-map.md §9-A
/// </summary>
public class RaidMapUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────
    //  싱글톤 (self-spawn)
    // ─────────────────────────────────────────────────────────────────

    static RaidMapUI _instance;

    static RaidMapUI Ensure()
    {
        if (_instance == null)
        {
            // 프리팹 우선 — Instantiate가 Awake를 돌려 _instance 설정(폰트 재바인딩 포함).
            // 프리팹이 없으면(미베이크) 코드 생성으로 폴백.
            var prefab = Resources.Load<GameObject>("UI/RaidMapUI");
            GameObject go = prefab != null ? Instantiate(prefab) : new GameObject("[RaidMapUI]");
            go.name = "[RaidMapUI]";
            if (prefab == null) go.AddComponent<RaidMapUI>(); // 폴백: Awake가 BuildUI
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    // ─────────────────────────────────────────────────────────────────
    //  공개 API
    // ─────────────────────────────────────────────────────────────────

    public static bool IsShowing => _instance != null && _instance._showing;

    public static void Show()
    {
        var ui = Ensure();
        ui.Open();
    }

    public static void Hide()
    {
        if (_instance != null) _instance.Close();
    }

    public static void Toggle()
    {
        if (IsShowing) Hide();
        else Show();
    }

    /// <summary>그레이박스/디자인 미리보기 진입점(=Show).</summary>
    public static void ShowPreview() => Show();

    // ─────────────────────────────────────────────────────────────────
    //  한글 폰트 로더 (규약: 모든 Text.font에 사용)
    // ─────────────────────────────────────────────────────────────────

    static Font _kr;
    static Font KR
    {
        get
        {
            if (_kr == null)
                _kr = Font.CreateDynamicFontFromOSFont(
                    new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Dotum", "Arial" }, 16);
            return _kr != null ? _kr : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  색 팔레트 (어두운 전술 톤 — 공통 스펙)
    // ─────────────────────────────────────────────────────────────────

    static readonly Color ColDim     = UITheme.Backdrop;
    static readonly Color ColPanel   = UITheme.Panel;
    static readonly Color ColPanel2  = UITheme.PanelAlt;
    static readonly Color ColGold    = UITheme.Gold;
    static readonly Color ColText    = UITheme.TextBright;
    static readonly Color ColMuted   = UITheme.TextMuted;
    static readonly Color ColLine    = UITheme.Divider;
    static readonly Color ColBtn     = UITheme.Cell;
    static readonly Color ColDanger  = UITheme.Danger;
    static readonly Color ColGood     = UITheme.Positive;
    static readonly Color ColWarn     = UITheme.Negative;

    // 맵 전용 색
    static readonly Color ColMapBg    = new Color(0.06f, 0.08f, 0.09f, 1f);   // 위성 톤 어두운 배경
    static readonly Color ColGrid     = new Color(0.30f, 0.45f, 0.45f, 0.14f); // 격자선
    static readonly Color ColZoneFill = new Color(0.22f, 0.40f, 0.52f, 0.32f); // 구역 박스
    static readonly Color ColZoneLine = new Color(0.45f, 0.70f, 0.85f, 0.55f); // 구역 외곽
    static readonly Color ColPlayer   = new Color(1f, 0.85f, 0.20f);           // 플레이어 ●
    static readonly Color ColExit     = new Color(0.30f, 1f, 0.45f);           // 탈출구 ◆
    static readonly Color ColPoi      = new Color(0.55f, 0.75f, 1f);           // POI ◇

    // ─────────────────────────────────────────────────────────────────
    //  설정
    // ─────────────────────────────────────────────────────────────────

    const int   SortingOrder = 130;          // NoteUI(110) 위, Toast(200) 아래
    const KeyCode ToggleKey  = KeyCode.N;     // M=NavigationHUD, F1=DebugTestUI → 충돌 피해 N만 사용
    const float GridStep     = 56f;           // 격자 칸 픽셀
    const float MapPad       = 40f;           // 본문 좌우 여백

    // ─────────────────────────────────────────────────────────────────
    //  상태 / 참조
    // ─────────────────────────────────────────────────────────────────

    bool _showing;
    int  _openFrame = -1;

    // ── 영속 스켈레톤(프리팹 베이크 시 직렬화 보존) ──
    [SerializeField] Canvas      _canvas;
    [SerializeField] GameObject  _rootGO;          // 전체 루트(딤)
    [SerializeField] RectTransform _mapArea;       // 마커가 배치되는 좌표계(중앙 앵커)
    // 정적 라벨(폰트 재바인딩 대상) — 마커는 동적이라 직렬화하지 않음.
    [SerializeField] Text _titleText;
    [SerializeField] Text _closeText;
    [SerializeField] Text _legendHead;
    [SerializeField] Text _legendRow0;
    [SerializeField] Text _legendRow1;
    [SerializeField] Text _legendRow2;

    Rect _worldBounds;            // 맵 영역에 매핑되는 월드 사각

    bool IsGenerated => _canvas != null;

    readonly List<GameObject> _markerPool = new List<GameObject>();
    readonly List<GameObject> _gridPool   = new List<GameObject>();
    RectTransform _playerMarker;
    bool _usingDummy;

    // ─────────────────────────────────────────────────────────────────
    //  라이프사이클
    // ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        if (!IsGenerated) BuildUI();   // 폴백: 프리팹 없이 코드로 생성
        else ApplyFonts();             // 프리팹 인스턴스: 동적 폰트 재바인딩
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    void Update()
    {
        // N 토글. 다른 UI가 떠 있으면 그 위에 겹쳐 열지 않음(닫기는 항상 허용).
        if (GameInput.GetKeyDown(ToggleKey))
        {
            if (_showing) Close();
            else if (UIManager.Instance == null || !UIManager.Instance.IsAnyUIOpen()) Open();
        }

        if (!_showing) return;

        // 연 프레임의 키 입력 무시(즉시 닫힘 방지).
        if (Time.frameCount == _openFrame) return;

        if (GameInput.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        // 실 데이터 모드일 때만 플레이어 마커를 따라가게 갱신.
        if (!_usingDummy) UpdatePlayerMarker();
    }

    // ─────────────────────────────────────────────────────────────────
    //  열기 / 닫기
    // ─────────────────────────────────────────────────────────────────

    void Open()
    {
        if (_rootGO == null) BuildUI();
        _showing = true;
        _openFrame = Time.frameCount;
        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (_canvas != null && !_canvas.gameObject.activeSelf) _canvas.gameObject.SetActive(true);
        _rootGO.SetActive(true);
        Rebuild();
    }

    void Close()
    {
        _showing = false;
        if (_rootGO != null) _rootGO.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────
    //  맵 그리기
    // ─────────────────────────────────────────────────────────────────

    void Rebuild()
    {
        ClearPool(_markerPool);
        ClearPool(_gridPool);
        _playerMarker = null;
        _usingDummy = false;

        var m = RaidMapManager.InstanceIfExists;
        var zones = m != null ? m.Zones : null;
        int discoveredCount = 0;
        if (zones != null)
            for (int i = 0; i < zones.Count; i++)
                if (zones[i].discovered) discoveredCount++;

        if (discoveredCount == 0)
        {
            // 데이터 없음 → 그레이박스 더미로 레이아웃 시연.
            _usingDummy = true;
            DrawGrid();
            DrawDummy();
            return;
        }

        ComputeWorldBounds(zones);
        DrawGrid();
        DrawRealData(zones);
    }

    void DrawGrid()
    {
        // mapArea 크기 기준으로 격자 라인을 채운다.
        float w = _mapArea.rect.width;
        float h = _mapArea.rect.height;
        if (w < 1f || h < 1f) { w = 1600f; h = 820f; } // 첫 프레임 레이아웃 전 폴백

        int cols = Mathf.CeilToInt(w / GridStep);
        int rows = Mathf.CeilToInt(h / GridStep);

        for (int c = 0; c <= cols; c++)
        {
            float x = -w * 0.5f + c * GridStep;
            var line = MakeMapImage(_gridPool, "GridV", null, ColGrid);
            line.sizeDelta = new Vector2(1f, h);
            line.anchoredPosition = new Vector2(x, 0f);
        }
        for (int r = 0; r <= rows; r++)
        {
            float y = -h * 0.5f + r * GridStep;
            var line = MakeMapImage(_gridPool, "GridH", null, ColGrid);
            line.sizeDelta = new Vector2(w, 1f);
            line.anchoredPosition = new Vector2(0f, y);
        }
    }

    // ── 실제 데이터 ──

    void DrawRealData(IReadOnlyList<MapZone> zones)
    {
        // 발견한 구역 박스 + 라벨.
        for (int i = 0; i < zones.Count; i++)
        {
            var z = zones[i];
            if (!z.discovered) continue;

            var box = MakeMapImage(_markerPool, "Zone_" + z.id, null, ColZoneFill);
            box.anchoredPosition = WorldToMap(z.Center);
            box.sizeDelta = WorldSizeToMap(z.bounds.size);
            AddOutline(box, ColZoneLine);

            var lbl = MakeMapText(_markerPool, "ZoneLbl_" + z.id,
                string.IsNullOrEmpty(z.displayName) ? z.id : z.displayName, 15, ColText);
            lbl.rectTransform.anchoredPosition = WorldToMap(z.Center);
        }

        // 탈출구 ◆ — 발견 구역 안에 있는 것만.
        var all = InteractableObject.All;
        for (int i = 0; i < all.Count; i++)
        {
            var io = all[i];
            if (io == null || io.Type != InteractableObject.InteractType.ExitPoint) continue;
            Vector2 wp = io.transform.position;
            if (!InsideDiscovered(zones, wp)) continue;
            PlaceDiamond(WorldToMap(wp), 20f, ColExit, "탈출구");
        }

        // 통로 주석(잠김/막힘/일방통행) — '알게 된' 것만. POI/인텔 느낌의 ◇/!.
        var passages = PassageMarker.All;
        for (int i = 0; i < passages.Count; i++)
        {
            var pm = passages[i];
            if (pm == null || !pm.Known) continue;
            if (pm.State == PassageState.Open) continue;
            PlacePoi(WorldToMap(pm.Position), PassageColor(pm.State), PassageLabel(pm.State));
        }

        // 플레이어 ● — 월드 위치를 매 프레임 갱신.
        _playerMarker = PlaceDot(Vector2.zero, 18f, ColPlayer, "");
        UpdatePlayerMarker();
    }

    void UpdatePlayerMarker()
    {
        if (_playerMarker == null) return;
        var p = TopDownPlayer.Instance;
        if (p == null) return;
        _playerMarker.anchoredPosition = WorldToMap((Vector2)p.transform.position);
    }

    // ── 그레이박스 더미 ──

    void DrawDummy()
    {
        // 더미 구역 박스 몇 개(레이아웃 시연).
        DummyZone(new Vector2(-440f, 150f), new Vector2(360f, 240f), "공장 구역");
        DummyZone(new Vector2(120f,  220f), new Vector2(320f, 200f), "주거 블록");
        DummyZone(new Vector2(-120f, -190f), new Vector2(420f, 260f), "지하 통로");
        DummyZone(new Vector2(460f, -120f), new Vector2(300f, 300f), "항만 창고");

        // 더미 통로/인텔 마커.
        PlacePoi(new Vector2(-260f, -40f), ColDanger, "잠김");
        PlacePoi(new Vector2(300f, 40f),  ColWarn,   "막힘");

        // 탈출 마커 2개 ◆.
        PlaceDiamond(new Vector2(-560f, -260f), 22f, ColExit, "탈출구 A");
        PlaceDiamond(new Vector2(600f,  280f),  22f, ColExit, "탈출구 B");

        // 플레이어 ● (더미는 중앙 고정).
        _playerMarker = PlaceDot(new Vector2(40f, -30f), 18f, ColPlayer, "현재 위치");
    }

    void DummyZone(Vector2 center, Vector2 size, string name)
    {
        var box = MakeMapImage(_markerPool, "DummyZone", null, ColZoneFill);
        box.anchoredPosition = center;
        box.sizeDelta = size;
        AddOutline(box, ColZoneLine);

        var lbl = MakeMapText(_markerPool, "DummyZoneLbl", name, 15, ColText);
        lbl.rectTransform.anchoredPosition = center;
    }

    // ─────────────────────────────────────────────────────────────────
    //  마커 헬퍼
    // ─────────────────────────────────────────────────────────────────

    RectTransform PlaceDot(Vector2 pos, float size, Color color, string label)
    {
        var dot = MakeMapImage(_markerPool, "Dot", PlaceholderSprite.Circle, color);
        dot.sizeDelta = new Vector2(size, size);
        dot.anchoredPosition = pos;
        if (!string.IsNullOrEmpty(label)) MarkerLabel(pos, size, label, color);
        return dot;
    }

    void PlaceDiamond(Vector2 pos, float size, Color color, string label)
    {
        var d = MakeMapImage(_markerPool, "Diamond", PlaceholderSprite.Square, color);
        d.sizeDelta = new Vector2(size, size);
        d.anchoredPosition = pos;
        d.localRotation = Quaternion.Euler(0f, 0f, 45f); // 마름모 ◆
        if (!string.IsNullOrEmpty(label)) MarkerLabel(pos, size, label, color);
    }

    void PlacePoi(Vector2 pos, Color color, string label)
    {
        // 외곽 마름모(◇) + 중앙 '!'.
        var d = MakeMapImage(_markerPool, "Poi", PlaceholderSprite.Square, new Color(color.r, color.g, color.b, 0.35f));
        d.sizeDelta = new Vector2(18f, 18f);
        d.anchoredPosition = pos;
        d.localRotation = Quaternion.Euler(0f, 0f, 45f);
        AddOutline(d, color);

        var bang = MakeMapText(_markerPool, "Poi!", "!", 16, color);
        bang.fontStyle = FontStyle.Bold;
        bang.rectTransform.sizeDelta = new Vector2(18f, 18f);
        bang.rectTransform.anchoredPosition = pos;

        if (!string.IsNullOrEmpty(label)) MarkerLabel(pos, 18f, label, color);
    }

    void MarkerLabel(Vector2 pos, float size, string text, Color color)
    {
        var lbl = MakeMapText(_markerPool, "MarkerLbl", text, 13, color);
        lbl.rectTransform.sizeDelta = new Vector2(140f, 18f);
        lbl.rectTransform.anchoredPosition = pos + new Vector2(0f, size * 0.5f + 11f);
    }

    static Color PassageColor(PassageState s) => s switch
    {
        PassageState.Locked  => ColDanger,                       // 빨강 자물쇠
        PassageState.Blocked => ColWarn,                         // 주황 잔해
        PassageState.OneWay  => new Color(0.4f, 0.8f, 1f),       // 하늘 일방통행
        _                    => ColPoi,
    };

    static string PassageLabel(PassageState s) => s switch
    {
        PassageState.Locked  => "잠김",
        PassageState.Blocked => "막힘",
        PassageState.OneWay  => "일방통행",
        _                    => "통로",
    };

    bool InsideDiscovered(IReadOnlyList<MapZone> zones, Vector2 wp)
    {
        for (int i = 0; i < zones.Count; i++)
            if (zones[i].discovered && zones[i].Contains(wp)) return true;
        return false;
    }

    // ─────────────────────────────────────────────────────────────────
    //  월드↔맵 좌표 변환
    // ─────────────────────────────────────────────────────────────────

    void ComputeWorldBounds(IReadOnlyList<MapZone> zones)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        int count = 0;
        if (zones != null)
            for (int i = 0; i < zones.Count; i++)
            {
                if (!zones[i].discovered) continue;
                var b = zones[i].bounds;
                minX = Mathf.Min(minX, b.xMin); minY = Mathf.Min(minY, b.yMin);
                maxX = Mathf.Max(maxX, b.xMax); maxY = Mathf.Max(maxY, b.yMax);
                count++;
            }

        if (count == 0)
        {
            Vector2 c = TopDownPlayer.Instance != null ? (Vector2)TopDownPlayer.Instance.transform.position : Vector2.zero;
            _worldBounds = new Rect(c.x - 20f, c.y - 20f, 40f, 40f);
            return;
        }

        float padX = (maxX - minX) * 0.10f + 2f;
        float padY = (maxY - minY) * 0.10f + 2f;
        _worldBounds = new Rect(minX - padX, minY - padY, (maxX - minX) + padX * 2f, (maxY - minY) + padY * 2f);
    }

    float MapScale()
    {
        float w = _mapArea.rect.width;
        float h = _mapArea.rect.height;
        if (w < 1f || h < 1f) { w = 1600f; h = 820f; }
        float bw = Mathf.Max(_worldBounds.width, 0.01f);
        float bh = Mathf.Max(_worldBounds.height, 0.01f);
        return Mathf.Min(w / bw, h / bh) * 0.92f;
    }

    Vector2 WorldToMap(Vector2 w) => (w - _worldBounds.center) * MapScale();
    Vector2 WorldSizeToMap(Vector2 s) => s * MapScale();

    // ─────────────────────────────────────────────────────────────────
    //  UI 빌드 (전체화면 공통 스펙)
    // ─────────────────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("RaidMapUI_Canvas");
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = SortingOrder;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // 루트 = 화면 전체 딤 배경(클릭 차단).
        _rootGO = new GameObject("Root");
        _rootGO.transform.SetParent(canvasGO.transform, false);
        var rootRT = _rootGO.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero; rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero; rootRT.offsetMax = Vector2.zero;
        var dim = _rootGO.AddComponent<Image>();
        dim.color = ColDim;
        dim.raycastTarget = true;

        BuildHeader(rootRT);
        BuildBody(rootRT);
        BuildLegend(rootRT);

        _rootGO.SetActive(false);
    }

    /// <summary>프리팹 인스턴스화 시 동적 OS 폰트를 직렬화된 정적 Text 참조에 재바인딩.</summary>
    void ApplyFonts()
    {
        var f = KR;
        if (_titleText)  _titleText.font  = f;
        if (_closeText)  _closeText.font  = f;
        if (_legendHead) _legendHead.font = f;
        if (_legendRow0) _legendRow0.font = f;
        if (_legendRow1) _legendRow1.font = f;
        if (_legendRow2) _legendRow2.font = f;
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — BuildUI를 1회 실행해 프리팹화할 계층을 만든다.</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        BuildUI();
    }
#endif

    void BuildHeader(RectTransform root)
    {
        // 상단 헤더바 (전폭, 높이 64).
        var bar = MakeImage(root, "Header", ColPanel2);
        bar.anchorMin = new Vector2(0f, 1f); bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.sizeDelta = new Vector2(0f, 64f);
        bar.anchoredPosition = Vector2.zero;

        // 제목.
        var title = MakeText(bar, "Title", "지역 지도", 26, FontStyle.Bold, ColText, TextAnchor.MiddleLeft);
        _titleText = title;
        var tRT = title.rectTransform;
        tRT.anchorMin = new Vector2(0f, 0f); tRT.anchorMax = new Vector2(1f, 1f);
        tRT.offsetMin = new Vector2(28f, 0f); tRT.offsetMax = new Vector2(-120f, 0f);

        // 닫기 ✕ 버튼 (48x40, 우측 끝).
        var closeGO = new GameObject("CloseBtn");
        closeGO.transform.SetParent(bar, false);
        var cRT = closeGO.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(1f, 0.5f); cRT.anchorMax = new Vector2(1f, 0.5f);
        cRT.pivot = new Vector2(1f, 0.5f);
        cRT.sizeDelta = new Vector2(48f, 40f);
        cRT.anchoredPosition = new Vector2(-16f, 0f);
        var cImg = closeGO.AddComponent<Image>();
        cImg.color = ColDanger;
        var cBtn = closeGO.AddComponent<Button>();
        cBtn.onClick.AddListener(Close);
        var cTxt = MakeText(cRT, "X", "✕", 22, FontStyle.Bold, ColText, TextAnchor.MiddleCenter);
        _closeText = cTxt;
        var xRT = cTxt.rectTransform;
        xRT.anchorMin = Vector2.zero; xRT.anchorMax = Vector2.one;
        xRT.offsetMin = Vector2.zero; xRT.offsetMax = Vector2.zero;

        // 헤더 아래 1px 구분선.
        var line = MakeImage(root, "HeaderLine", ColLine);
        line.anchorMin = new Vector2(0f, 1f); line.anchorMax = new Vector2(1f, 1f);
        line.pivot = new Vector2(0.5f, 1f);
        line.sizeDelta = new Vector2(0f, 1f);
        line.anchoredPosition = new Vector2(0f, -64f);
    }

    void BuildBody(RectTransform root)
    {
        // 본문 = 헤더 아래부터 화면 하단까지(좌우 여백 MapPad).
        var bodyPanel = MakeImage(root, "MapPanel", ColPanel2);
        bodyPanel.anchorMin = new Vector2(0f, 0f); bodyPanel.anchorMax = new Vector2(1f, 1f);
        bodyPanel.offsetMin = new Vector2(MapPad, MapPad);
        bodyPanel.offsetMax = new Vector2(-MapPad, -(64f + 14f));

        // 위성 톤 맵 영역(어두운 배경) — 마커 좌표계의 부모.
        var areaGO = new GameObject("MapArea");
        areaGO.transform.SetParent(bodyPanel, false);
        _mapArea = areaGO.AddComponent<RectTransform>();
        _mapArea.anchorMin = Vector2.zero; _mapArea.anchorMax = Vector2.one;
        _mapArea.offsetMin = new Vector2(8f, 8f); _mapArea.offsetMax = new Vector2(-8f, -8f);
        var areaImg = areaGO.AddComponent<Image>();
        areaImg.color = ColMapBg;
        // 맵 밖으로 마커가 새지 않도록 클립.
        areaGO.AddComponent<RectMask2D>();
    }

    void BuildLegend(RectTransform root)
    {
        // 우상단 작은 범례 패널(본문 영역 안 우상단).
        var legend = MakeImage(root, "Legend", ColPanel);
        legend.anchorMin = new Vector2(1f, 1f); legend.anchorMax = new Vector2(1f, 1f);
        legend.pivot = new Vector2(1f, 1f);
        legend.sizeDelta = new Vector2(200f, 130f);
        legend.anchoredPosition = new Vector2(-(MapPad + 14f), -(64f + 24f));
        AddOutline(legend, ColLine);

        var head = MakeText(legend, "LegendHead", "범례", 14, FontStyle.Bold, ColGold, TextAnchor.UpperLeft);
        _legendHead = head;
        var hRT = head.rectTransform;
        hRT.anchorMin = new Vector2(0f, 1f); hRT.anchorMax = new Vector2(1f, 1f);
        hRT.offsetMin = new Vector2(12f, -26f); hRT.offsetMax = new Vector2(-12f, -6f);

        _legendRow0 = LegendRow(legend, 0, ColPlayer, "● 현재 위치");
        _legendRow1 = LegendRow(legend, 1, ColExit,   "◆ 탈출구");
        _legendRow2 = LegendRow(legend, 2, ColPoi,    "◇ POI / 인텔");
    }

    Text LegendRow(RectTransform parent, int index, Color color, string text)
    {
        var row = MakeText(parent, "LegendRow" + index, text, 13, FontStyle.Normal, color, TextAnchor.MiddleLeft);
        var rt = row.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        float top = -34f - index * 24f;
        rt.offsetMin = new Vector2(14f, top - 22f); rt.offsetMax = new Vector2(-10f, top);
        return row;
    }

    // ─────────────────────────────────────────────────────────────────
    //  공통 빌드 헬퍼
    // ─────────────────────────────────────────────────────────────────

    RectTransform MakeImage(RectTransform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        return rt;
    }

    Text MakeText(RectTransform parent, string name, string content, int size, FontStyle style, Color color, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.font = KR;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.8f);
        sh.effectDistance = new Vector2(1f, -1f);
        return t;
    }

    /// <summary>RectTransform 외곽선(Outline 컴포넌트가 아닌 4변 1px 라인으로 — Image엔 Outline이 안 먹음).</summary>
    void AddOutline(RectTransform target, Color color)
    {
        // 상/하/좌/우 1px 라인을 자식으로.
        AddEdge(target, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -1f), new Vector2(0f, 0f), color); // top
        AddEdge(target, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), color);  // bottom
        AddEdge(target, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f), color);  // left
        AddEdge(target, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1f, 0f), new Vector2(0f, 0f), color); // right
    }

    void AddEdge(RectTransform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, Color color)
    {
        var go = new GameObject("Edge");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = offMin; rt.offsetMax = offMax;
        // 가로/세로 1px 처리: 앵커가 한 축으로 0폭이면 sizeDelta로 1px 보정.
        if (aMin.x == aMax.x) rt.sizeDelta = new Vector2(1f, rt.sizeDelta.y);
        if (aMin.y == aMax.y) rt.sizeDelta = new Vector2(rt.sizeDelta.x, 1f);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    // 맵 영역 자식(중앙 앵커) — 마커/격자/구역 박스용.
    RectTransform MakeMapImage(List<GameObject> pool, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_mapArea, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        var img = go.AddComponent<Image>();
        if (sprite != null) img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        pool.Add(go);
        return rt;
    }

    Text MakeMapText(List<GameObject> pool, string name, string content, int size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_mapArea, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.font = KR;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(140f, 18f);
        var sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sh.effectDistance = new Vector2(1f, -1f);
        pool.Add(go);
        return t;
    }

    void ClearPool(List<GameObject> pool)
    {
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null) Destroy(pool[i]);
        pool.Clear();
    }
}
