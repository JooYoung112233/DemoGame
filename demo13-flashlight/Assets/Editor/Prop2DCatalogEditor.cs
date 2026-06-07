using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 탑다운 2D 프롭 카탈로그 에디터.
/// Prop2DDefinition 에셋 등록/편집/미리보기 + 콜라이더(막힘) 시각 편집.
/// 편의기능: 검색, 스프라이트 일괄 등록, 씬 클릭 배치.
/// 메뉴: Tools ▸ TopDown ▸ Map ▸ Prop Catalog
/// </summary>
public class Prop2DCatalogEditor : EditorWindow
{
    const string PropFolder = "Assets/Resources/Props2D";

    [MenuItem("Tools/TopDown/맵/프롭 카탈로그")]
    static void Open() => GetWindow<Prop2DCatalogEditor>("2D Prop Catalog");

    List<Prop2DDefinition> _props = new();
    Prop2DDefinition _selected;
    Prop2DDefinition _draft; // '＋ 새 항목' 시 만드는 미저장 드래프트(이미지/머티리얼 등록 후 '생성 & 등록')
    Vector2 _listScroll, _formScroll;
    string _search = "";
    bool _placeMode;
    bool _resizeMode;             // 씬에서 선택 프롭을 에지 드래그로 리사이즈(앵커=반대쪽)
    float _snap = 1f;
    bool _ceilingXray;            // 천장 반투명(에디터 화면 전용 — 저장/플레이 영향 없음)
    float _ceilingXrayAlpha = 0.3f;
    GameObject _ghost;          // 씬 배치 고스트 미리보기(HideAndDontSave)
    Prop2DDefinition _ghostDef; // 고스트가 만들어진 정의
    Vector3 _lastPaintPos;      // 드래그 연속 배치 간격 기준
    Material _batchMaterial;     // 일괄 등록 공통 머티리얼
    SpriteDrawMode _batchDrawMode = SpriteDrawMode.Simple; // 일괄 등록 공통 Draw Mode
    string _batchSortingLayer = "Default"; // 일괄 등록 공통 Sorting Layer
    int _batchSortingOffset;     // 일괄 등록 공통 Order in Layer
    bool _batchCastShadow;       // 일괄 등록 공통 그림자
    Prop2DDefinition.ShadowCast _batchShadowCasting = Prop2DDefinition.ShadowCast.CastShadow;
    string _batchGroup = "";     // 일괄 등록 공통 분류(컬렉션)
    string _groupFilter = null;  // 팔레트 분류 필터. null=전체, ""=(미분류), 그 외=분류명
    [SerializeField] Prop2DDefinition.Category _tab = Prop2DDefinition.Category.Prop; // 활성 탭도 리로드 후 유지
    [SerializeField] TabState[] _tabStates; // 탭별 마지막 값(일괄 등록 설정 + 선택). 도메인 리로드/창 유지 시 보존.
    static readonly string[] TabNames = { "바닥", "벽", "프롭", "오브젝트", "데칼", "천장" };
    // 섹션 접기/펴기 (목록 + 폼)
    bool _foldList = true, _foldVisual = true, _foldCollider = true, _foldShadow, _foldBreak, _foldAged, _foldFunction, _foldPreview = true;
    bool _foldCeiling = true;   // 천장 섹션(카테고리=천장일 때만 표시)
    bool _foldLight;            // 발광 섹션
    bool _foldBatch;            // 일괄 등록 설정 접이식(기본 접힘)
    GUIStyle _cellNameStyle;    // 팔레트 셀 이름 스타일(지연 생성)
    GUIStyle _sectionText, _bannerText; // 섹션 헤더/배너 글자 스타일(지연 생성)
    GUIStyle _bigButton, _barLabel;     // 최상단 바 버튼/라벨 스타일(지연 생성)
    // 섹션 강조색 (어두운 스킨에서 흰 글씨와 대비되는 톤)
    static readonly Color ColIdentity = new(0.20f, 0.31f, 0.46f);
    static readonly Color ColVisual   = new(0.16f, 0.37f, 0.40f);
    static readonly Color ColCollider = new(0.45f, 0.27f, 0.17f);
    static readonly Color ColShadow   = new(0.32f, 0.24f, 0.45f);
    static readonly Color ColBreak    = new(0.46f, 0.20f, 0.20f);
    static readonly Color ColAged     = new(0.40f, 0.30f, 0.16f);
    static readonly Color ColFunction = new(0.21f, 0.40f, 0.24f);
    static readonly Color ColPreview  = new(0.27f, 0.27f, 0.30f);
    static readonly Color ColBatch    = new(0.34f, 0.30f, 0.15f);
    static readonly Color ColCeiling  = new(0.18f, 0.30f, 0.36f);
    static readonly Color ColLight    = new(0.44f, 0.40f, 0.16f);   // 발광(따뜻한 노랑)

    void OnEnable()
    {
        GameLayers.EnsureSortingLayer("Ceiling"); // 천장 최상단 레이어 보장/복구(0 uniqueID 깨짐 수리)
        EnsureTabStates();
        LoadTabState(_tab);   // 마지막 탭 값 복원(도메인 리로드/재오픈 후)
        Refresh();
        SceneView.duringSceneGui += OnSceneGUI;
        // 저장 시 천장 반투명이 씬/프리팹에 구워지지 않게 가드.
        EditorSceneManager.sceneSaving += OnSceneSavingGuard;
        EditorSceneManager.sceneSaved += OnSceneSavedGuard;
    }

    void OnDisable()
    {
        SaveTabState(_tab);   // 현재 탭 값 저장(리로드/닫기 전)
        SetCeilingAlpha(1f);  // 닫기/리로드 전 천장 불투명 복원(반투명이 남지 않게)
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorSceneManager.sceneSaving -= OnSceneSavingGuard;
        EditorSceneManager.sceneSaved -= OnSceneSavedGuard;
        CancelDraft();  // 미저장 드래프트 정리
        DestroyGhost(); // 고스트 미리보기 정리
    }

    // 씬 저장 직전 천장 불투명 복원 → 저장 후 반투명 재적용(반투명이 파일에 구워지는 것 방지).
    void OnSceneSavingGuard(Scene scene, string path) { if (_ceilingXray) SetCeilingAlpha(1f); }
    void OnSceneSavedGuard(Scene scene) { if (_ceilingXray) SetCeilingAlpha(_ceilingXrayAlpha); }

    /// <summary>열린 씬의 모든 천장(SortingLayer=Ceiling) 스프라이트 알파를 a로 설정(에디터 화면 전용).
    /// 값이 같으면 건너뛰어 불필요한 더티 방지.</summary>
    void SetCeilingAlpha(float a)
    {
        foreach (var sr in FindCeilingRenderers())
        {
            if (Mathf.Approximately(sr.color.a, a)) continue;
            var c = sr.color; c.a = a; sr.color = c;
        }
        SceneView.RepaintAll();
    }

    /// <summary>열린 씬의 천장 SpriteRenderer(SortingLayer=Ceiling, 고스트 제외) 열거.</summary>
    static IEnumerable<SpriteRenderer> FindCeilingRenderers()
    {
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            if (sr != null && sr.sortingLayerName == "Ceiling"
                && !sr.gameObject.hideFlags.HasFlag(HideFlags.HideAndDontSave))
                yield return sr;
    }

    void Refresh()
    {
        _props = AssetDatabase.FindAssets("t:Prop2DDefinition")
            .Select(g => AssetDatabase.LoadAssetAtPath<Prop2DDefinition>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(p => p != null)
            .OrderBy(p => p.displayName + p.propId)
            .ToList();
        // 선택은 항상 현재 탭 항목으로 유지(탭과 무관한 항목이 폼에 뜨던 버그 방지).
        if (_selected == null || !_props.Contains(_selected) || _selected.category != _tab)
            _selected = _props.FirstOrDefault(p => p.category == _tab);
    }

    // ── 탭별 마지막 값 캐시 ──
    /// <summary>탭(카테고리)마다 기억하는 일괄 등록 설정 + 선택 항목.
    /// 탭마다 자주 쓰는 머티리얼/정렬/분류가 달라서, 탭을 바꿔도 각 탭의 마지막 값이 유지된다.</summary>
    [System.Serializable]
    class TabState
    {
        public Material batchMaterial;
        public SpriteDrawMode batchDrawMode = SpriteDrawMode.Simple;
        public string batchSortingLayer = "Default";
        public int batchSortingOffset;
        public bool batchCastShadow;
        public Prop2DDefinition.ShadowCast batchShadowCasting = Prop2DDefinition.ShadowCast.CastShadow;
        public string batchGroup = "";
        public Prop2DDefinition selected;
    }

    /// <summary>캐시 배열 보장(탭 개수 변경에도 기존 값 보존).</summary>
    void EnsureTabStates()
    {
        if (_tabStates != null && _tabStates.Length == TabNames.Length) return;
        var old = _tabStates;
        _tabStates = new TabState[TabNames.Length];
        for (int i = 0; i < _tabStates.Length; i++)
            _tabStates[i] = (old != null && i < old.Length && old[i] != null) ? old[i] : new TabState();
    }

    /// <summary>현재 활성 값(일괄 설정+선택)을 해당 탭 캐시에 저장.</summary>
    void SaveTabState(Prop2DDefinition.Category cat)
    {
        EnsureTabStates();
        var s = _tabStates[(int)cat];
        s.batchMaterial = _batchMaterial;
        s.batchDrawMode = _batchDrawMode;
        s.batchSortingLayer = _batchSortingLayer;
        s.batchSortingOffset = _batchSortingOffset;
        s.batchCastShadow = _batchCastShadow;
        s.batchShadowCasting = _batchShadowCasting;
        s.batchGroup = _batchGroup;
        s.selected = _selected;
    }

    /// <summary>탭 캐시 값을 활성 필드로 복원.</summary>
    void LoadTabState(Prop2DDefinition.Category cat)
    {
        EnsureTabStates();
        var s = _tabStates[(int)cat];
        _batchMaterial = s.batchMaterial;
        _batchDrawMode = s.batchDrawMode;
        _batchSortingLayer = string.IsNullOrEmpty(s.batchSortingLayer) ? "Default" : s.batchSortingLayer;
        _batchSortingOffset = s.batchSortingOffset;
        _batchCastShadow = s.batchCastShadow;
        _batchShadowCasting = s.batchShadowCasting;
        _batchGroup = s.batchGroup ?? "";
        _selected = s.selected;

        // 천장 탭은 최상단 'Ceiling' 정렬 레이어가 기본(미설정 시) — 보이게 시딩 + 레이어 보장.
        if (cat == Prop2DDefinition.Category.Ceiling && _batchSortingLayer == "Default")
        {
            GameLayers.EnsureSortingLayer("Ceiling");
            _batchSortingLayer = "Ceiling";
        }
    }

    void OnGUI()
    {
        // 창에 포커스가 있을 때 ESC = 배치/크기조절 취소 (씬뷰 포커스가 아닐 때 대비)
        if ((_placeMode || _resizeMode) && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            _placeMode = false;
            _resizeMode = false;
            DestroyGhost();
            Event.current.Use();
        }

        // ── 최상단 바: 맵 생성 / 맵 저장 / 스냅 / 배치 상태 (큰 색 버튼으로 잘 보이게) ──
        EditorGUILayout.Space(3);
        EditorGUILayout.BeginHorizontal();
        var prevTopBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.40f, 0.85f, 0.50f); // 맵 생성 = 초록
        if (GUILayout.Button("＋ 맵 생성", BigButton, GUILayout.Width(120)))
        {
            var t = MapRoot(true);
            Selection.activeGameObject = t != null ? t.gameObject : null;
        }
        GUI.backgroundColor = new Color(0.45f, 0.70f, 1f); // 맵 저장 = 파랑
        if (GUILayout.Button("맵 저장 (프리팹)", BigButton, GUILayout.Width(150)))
            SaveMapPrefab();
        GUI.backgroundColor = prevTopBg;
        GUILayout.Space(14);
        GUILayout.Label(new GUIContent("스냅",
            "배치 격자 크기(월드 단위). 배치 모드에서 프롭이 이 격자에 맞춰 놓이고, 드래그 연속배치 간격·고스트 칸 크기도 이 값. 0=자유 배치."),
            BarLabel, GUILayout.Width(36));
        _snap = EditorGUILayout.FloatField(_snap, GUILayout.Width(50), GUILayout.Height(20));

        // 천장 반투명(에디터 화면만) — 바닥/배치가 천장에 가릴 때.
        GUILayout.Space(14);
        var prevXrayBg = GUI.backgroundColor;
        if (_ceilingXray) GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
        bool xrayNow = GUILayout.Toggle(_ceilingXray, new GUIContent("천장 반투명",
            "맵 편집 중 천장을 반투명하게(에디터 화면만 — 저장/플레이엔 영향 없음). 천장에 바닥이 가릴 때 켜기."),
            "Button", GUILayout.Height(22), GUILayout.Width(86));
        GUI.backgroundColor = prevXrayBg;
        if (xrayNow != _ceilingXray) { _ceilingXray = xrayNow; SetCeilingAlpha(_ceilingXray ? _ceilingXrayAlpha : 1f); }
        if (_ceilingXray)
        {
            float a = EditorGUILayout.Slider(_ceilingXrayAlpha, 0f, 1f, GUILayout.Width(90));
            if (!Mathf.Approximately(a, _ceilingXrayAlpha)) { _ceilingXrayAlpha = a; SetCeilingAlpha(a); }
        }

        GUILayout.FlexibleSpace();
        if (_placeMode)
            GUILayout.Label($"● 배치: {(_selected != null ? _selected.displayName : "")}  (좌클릭/드래그=배치, ESC=취소)",
                BarLabel);
        EditorGUILayout.EndHorizontal();
        // 구분선 — 맵 바 / 카탈로그 분리
        var topSep = GUILayoutUtility.GetRect(1, 3, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(topSep, new Color(0f, 0f, 0f, 0.35f));
        EditorGUILayout.Space(2);

        // 세로 레이아웃: 목록(위) → 구분선 → 편집 폼(아래). (가로 분할 시 목록이 좁아 버튼이 잘림)
        DrawList();
        var sep = GUILayoutUtility.GetRect(1, 2, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(sep, new Color(0, 0, 0, 0.3f));
        DrawForm();
    }

    // ───────────────────────── 위: 목록 ─────────────────────────
    void DrawList()
    {
        EditorGUILayout.BeginVertical();

        // 카테고리 탭
        int newTab = GUILayout.Toolbar((int)_tab, TabNames);
        if (newTab != (int)_tab)
        {
            SaveTabState(_tab);                          // 떠나는 탭의 일괄 설정+선택 저장
            _tab = (Prop2DDefinition.Category)newTab;
            LoadTabState(_tab);                          // 새 탭의 마지막 일괄 설정+선택 복원
            // 복원된 선택이 유효하지 않으면 새 탭 첫 항목(없으면 null)
            if (_selected == null || !_props.Contains(_selected) || _selected.category != _tab)
                _selected = _props.FirstOrDefault(p => p != null && p.category == _tab);
            _groupFilter = null; // 분류 필터는 탭 단위 — 탭 바꾸면 '전체'로 리셋(이전 필터가 새 탭 항목 가리는 것 방지)
        }

        int count = _props.Count(p => p != null && p.category == _tab);
        // 목록 접기/펴기 — 접으면 폼만 크게 본다. (탭은 항상 보임)
        _foldList = EditorGUILayout.Foldout(_foldList, $"{TabNames[(int)_tab]} 팔레트 ({count})", true);
        if (!_foldList) { EditorGUILayout.EndVertical(); return; }

        // 헤더: 배치 토글 + 크기조절 토글 + 새 항목 + 새로고침
        EditorGUILayout.BeginHorizontal();
        var prevBg = GUI.backgroundColor;
        if (_placeMode) GUI.backgroundColor = new Color(0.4f, 1f, 0.55f);
        bool wasPlacing = _placeMode;
        _placeMode = GUILayout.Toggle(_placeMode,
            _placeMode ? "● 배치 중 (ESC 종료)" : "배치 모드", "Button", GUILayout.Height(22));
        if (_placeMode && !wasPlacing) { _resizeMode = false; SceneView.lastActiveSceneView?.Focus(); }
        GUI.backgroundColor = prevBg;

        if (_resizeMode) GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
        bool wasResizing = _resizeMode;
        _resizeMode = GUILayout.Toggle(_resizeMode,
            _resizeMode ? "● 크기조절 (ESC)" : "크기조절", "Button", GUILayout.Height(22));
        if (_resizeMode && !wasResizing) { _placeMode = false; DestroyGhost(); SceneView.lastActiveSceneView?.Focus(); }
        GUI.backgroundColor = prevBg;

        if (GUILayout.Button("＋ 새 항목", GUILayout.Width(90), GUILayout.Height(22))) CreateNewProp();
        if (GUILayout.Button("↻", GUILayout.Width(26), GUILayout.Height(22))) Refresh();
        EditorGUILayout.EndHorizontal();
        if (_resizeMode)
            EditorGUILayout.HelpBox("씬에서 프롭 선택 → 가장자리 핸들 드래그 = 반대쪽(피벗) 고정하고 그 방향만 늘림. " +
                "Tiled는 size, 그 외는 Scale 변경. (Shift=양쪽 대칭)", MessageType.Info);

        _search = EditorGUILayout.TextField("검색", _search);

        // 분류(컬렉션) 필터 — 현재 탭에 존재하는 분류 목록.
        {
            var groups = GroupsInTab(_tab);
            var opts = new List<string> { "＜전체＞", "(미분류)" };
            opts.AddRange(groups);
            int cur = _groupFilter == null ? 0 : (_groupFilter.Length == 0 ? 1 : opts.IndexOf(_groupFilter));
            if (cur < 0) cur = 0; // 다른 탭으로 옮겨 분류가 사라졌으면 전체로
            int sel = EditorGUILayout.Popup("분류 필터", cur, opts.ToArray());
            _groupFilter = sel == 0 ? null : (sel == 1 ? "" : opts[sel]);
        }

        // 일괄 등록 (접이식)
        _foldBatch = SectionHeader("일괄 등록 · 선택 스프라이트 → 항목 생성", _foldBatch, ColBatch);
        if (_foldBatch)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _batchMaterial = (Material)EditorGUILayout.ObjectField("머티리얼", _batchMaterial, typeof(Material), false);
            _batchSortingLayer = SortingLayerPopup("Sorting Layer", _batchSortingLayer);
            _batchSortingOffset = EditorGUILayout.IntField("Order in Layer", _batchSortingOffset);
            _batchDrawMode = (SpriteDrawMode)EditorGUILayout.EnumPopup("Draw Mode", _batchDrawMode);
            _batchCastShadow = EditorGUILayout.Toggle("그림자 드리움", _batchCastShadow);
            if (_batchCastShadow)
                _batchShadowCasting = (Prop2DDefinition.ShadowCast)
                    EditorGUILayout.EnumPopup("Casting Option", _batchShadowCasting);
            EditorGUILayout.BeginHorizontal();
            _batchGroup = EditorGUILayout.TextField("분류(컬렉션)", _batchGroup);
            if (GUILayout.Button("▾", EditorStyles.miniButton, GUILayout.Width(22)))
                ShowGroupMenu(g => _batchGroup = g, _batchGroup);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("선택 스프라이트 일괄 등록")) BatchImportSelectedSprites();
            EditorGUILayout.EndVertical();
        }

        // 팔레트 그리드 (썸네일 — 클릭=선택/브러시, 우클릭=메뉴)
        var items = _props.Where(p => p != null && p.category == _tab &&
                (string.IsNullOrEmpty(_search) ||
                 (p.displayName ?? p.propId).IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0) &&
                (_groupFilter == null || (p.group ?? "") == _groupFilter))
            .ToList();

        // 안전망: 필터/검색으로 가려진 항목이 있으면 안내 + 한 번에 해제 ('안 보인다' 방지)
        int tabTotal = _props.Count(p => p != null && p.category == _tab);
        if (items.Count < tabTotal)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"⚠ 필터/검색으로 {tabTotal - items.Count}개 숨김 (이 탭 전체 {tabTotal}개)", EditorStyles.miniLabel);
            if (GUILayout.Button("전체 보기", GUILayout.Width(80))) { _groupFilter = null; _search = ""; }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(2);
        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Height(210));
        if (items.Count == 0)
            EditorGUILayout.LabelField("항목 없음 — '＋ 새 항목' 또는 일괄 등록으로 추가", EditorStyles.miniLabel);
        else if (_groupFilter == null)
        {
            // ＜전체＞ — 분류별 헤더로 묶어서 표시 ((미분류)는 맨 아래)
            foreach (var grp in items
                         .GroupBy(p => string.IsNullOrEmpty(p.group) ? "(미분류)" : p.group)
                         .OrderBy(g => g.Key == "(미분류)" ? 1 : 0).ThenBy(g => g.Key))
            {
                EditorGUILayout.LabelField($"▸ {grp.Key}  ({grp.Count()})", EditorStyles.boldLabel);
                DrawPaletteGrid(grp.ToList());
            }
        }
        else DrawPaletteGrid(items);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.LabelField("클릭=선택(브러시) · 우클릭=메뉴(복제/삭제) · 편집은 아래 폼", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    // ── 팔레트 그리드 ──
    void DrawPaletteGrid(List<Prop2DDefinition> items)
    {
        if (items.Count == 0)
        {
            EditorGUILayout.LabelField("항목 없음 — '＋ 새 항목' 또는 일괄 등록으로 추가", EditorStyles.miniLabel);
            return;
        }
        const float cell = 62f;
        float avail = EditorGUIUtility.currentViewWidth - 26f;
        int cols = Mathf.Max(1, Mathf.FloorToInt(avail / cell));
        int i = 0;
        while (i < items.Count)
        {
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < cols && i < items.Count; c++, i++)
                DrawPaletteCell(items[i], cell - 4f);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }

    void DrawPaletteCell(Prop2DDefinition p, float size)
    {
        var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
        bool sel = p == _selected;

        EditorGUI.DrawRect(rect, sel ? new Color(0.22f, 0.42f, 0.85f) : new Color(0.2f, 0.2f, 0.2f));
        // 썸네일(종횡비 맞춤)
        var inner = new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 16);
        if (p.sprite != null && p.sprite.texture != null)
        {
            var bs = p.sprite.bounds.size;
            float aspect = bs.y > 0.0001f ? bs.x / bs.y : 1f;
            float w = inner.width, h = inner.height;
            if (aspect >= 1f) h = inner.width / Mathf.Max(0.01f, aspect); else w = inner.height * aspect;
            var fit = new Rect(inner.x + (inner.width - w) * 0.5f, inner.y + (inner.height - h) * 0.5f, w, h);
            var trc = p.sprite.textureRect; var tx = p.sprite.texture;
            GUI.DrawTextureWithTexCoords(fit, tx,
                new Rect(trc.x / tx.width, trc.y / tx.height, trc.width / tx.width, trc.height / tx.height), true);
        }
        GUI.Label(new Rect(rect.x + 1, rect.yMax - 13, rect.width - 2, 12),
            p.displayName ?? p.propId, CellNameStyle);
        if (_placeMode && sel) DrawRectOutline(rect, new Color(0.4f, 1f, 0.5f)); // 브러시 표시

        var e = Event.current;
        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            if (e.button == 0) { _selected = p; GUI.FocusControl(null); e.Use(); Repaint(); }
            else if (e.button == 1) { ShowCellMenu(p); e.Use(); }
        }
    }

    GUIStyle CellNameStyle => _cellNameStyle ??= new GUIStyle(EditorStyles.miniLabel)
    {
        alignment = TextAnchor.MiddleCenter,
        fontSize = 9,
        clipping = TextClipping.Clip,
        normal = { textColor = Color.white }
    };

    void ShowCellMenu(Prop2DDefinition p)
    {
        var m = new GenericMenu();
        m.AddItem(new GUIContent("배치 (브러시)"), false, () => { _selected = p; _placeMode = true; SceneView.lastActiveSceneView?.Focus(); });
        m.AddItem(new GUIContent("편집 (선택)"), false, () => { _selected = p; });
        m.AddItem(new GUIContent("복제"), false, () => { _selected = p; DuplicateSelected(); });
        m.AddItem(new GUIContent("삭제"), false, () => { _selected = p; DeleteSelected(); });
        m.AddSeparator("");
        m.AddItem(new GUIContent("프로젝트에서 보기"), false, () => EditorGUIUtility.PingObject(p));
        m.ShowAsContext();
    }

    // ── 가독성: 색상 섹션 헤더 ──
    GUIStyle SectionText => _sectionText ??= new GUIStyle(EditorStyles.label)
    {
        fontSize = 12, fontStyle = FontStyle.Bold,
        alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white }
    };
    GUIStyle BannerText => _bannerText ??= new GUIStyle(EditorStyles.label)
    {
        fontSize = 13, fontStyle = FontStyle.Bold,
        alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white }
    };
    GUIStyle BigButton => _bigButton ??= new GUIStyle(GUI.skin.button)
    { fontSize = 12, fontStyle = FontStyle.Bold, fixedHeight = 28 };
    GUIStyle BarLabel => _barLabel ??= new GUIStyle(EditorStyles.label)
    { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fixedHeight = 28 };

    /// <summary>색상 바 섹션 헤더. collapsible=true면 클릭 토글(▼/▶), 반환값=접힘 상태.
    /// collapsible=false면 항상 펼친 제목 바(반환값 무시).</summary>
    bool SectionHeader(string title, bool fold, Color accent, bool collapsible = true)
    {
        EditorGUILayout.Space(4);
        var rect = GUILayoutUtility.GetRect(0, 23, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, accent);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), accent * 1.6f); // 왼쪽 강조 바
        string arrow = collapsible ? (fold ? "▼  " : "▶  ") : "";
        GUI.Label(new Rect(rect.x + 9, rect.y, rect.width - 12, rect.height), arrow + title, SectionText);
        if (collapsible)
        {
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            { fold = !fold; e.Use(); Repaint(); }
        }
        return fold;
    }

    // ───────────────────────── 우측: 폼 + 미리보기 ─────────────────────────
    void DrawForm()
    {
        EditorGUILayout.BeginVertical();

        var def = _draft != null ? _draft : _selected;
        bool creating = _draft != null;
        if (def == null)
        {
            EditorGUILayout.HelpBox("왼쪽 목록에서 항목을 클릭하면 여기서 편집합니다. 새로 만들려면 '＋ 새 항목'.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        // 상단 배너 — 현재 편집 대상(큰 글씨 + 색 바). 생성=초록, 편집=파랑.
        {
            var brect = GUILayoutUtility.GetRect(0, 26, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(brect, creating ? new Color(0.18f, 0.42f, 0.26f) : new Color(0.20f, 0.31f, 0.46f));
            string banner = creating
                ? "🆕  새 항목 만들기 — 채우고 아래 '생성 & 등록'"
                : $"✏  편집 중: {(string.IsNullOrEmpty(def.displayName) ? def.propId : def.displayName)}";
            GUI.Label(new Rect(brect.x + 9, brect.y, brect.width - 12, brect.height), banner, BannerText);
        }
        _formScroll = EditorGUILayout.BeginScrollView(_formScroll);
        EditorGUI.BeginChangeCheck();

        SectionHeader("Identity · 기본 정보", true, ColIdentity, collapsible: false);
        EditorGUI.indentLevel++;
        // ID는 자동 생성(수동 입력 X). 편집 시 읽기전용 표시.
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.TextField("Prop ID (자동)", def.propId);
        def.displayName = EditorGUILayout.TextField("표시 이름", def.displayName);
        var newCat = (Prop2DDefinition.Category)EditorGUILayout.EnumPopup("카테고리(탭)", def.category);
        if (newCat != def.category)
        {
            def.category = newCat;
            // 드래프트(새 항목)면 ID 접두어를 카테고리에 맞춰 갱신 (Wall→wall_0001 등)
            if (creating) def.propId = NextAutoId(newCat);
        }

        // 분류(컬렉션) — 직접 입력 + 기존 분류 선택(▾). 예: 전당포, 안전구역.
        EditorGUILayout.BeginHorizontal();
        def.group = EditorGUILayout.TextField(
            new GUIContent("분류(컬렉션)", "같은 분류끼리 묶어 보기·필터. 예: 전당포, 안전구역. 카테고리 가로질러 사용 가능."),
            def.group ?? "");
        if (GUILayout.Button("▾", EditorStyles.miniButton, GUILayout.Width(22)))
            ShowGroupMenu(g => { def.group = g; EditorUtility.SetDirty(def); }, def.group);
        EditorGUILayout.EndHorizontal();
        EditorGUI.indentLevel--;

        _foldVisual = SectionHeader("Visual · 스프라이트", _foldVisual, ColVisual);
        if (_foldVisual)
        {
            EditorGUI.indentLevel++;
            def.sprite = (Sprite)EditorGUILayout.ObjectField("스프라이트", def.sprite, typeof(Sprite), false);
            def.material = (Material)EditorGUILayout.ObjectField("머티리얼(선택)", def.material, typeof(Material), false);
            def.sortingLayer = SortingLayerPopup("Sorting Layer", def.sortingLayer);
            def.sortingOffset = EditorGUILayout.IntField("Order in Layer", def.sortingOffset);

            var newDraw = (SpriteDrawMode)EditorGUILayout.EnumPopup("Draw Mode", def.drawMode);
            if (newDraw != def.drawMode)
            {
                def.drawMode = newDraw;
                // Tiled/Sliced로 전환 시 크기 기본값을 스프라이트 크기로 자동 채움(미설정/기본값일 때).
                if (newDraw != SpriteDrawMode.Simple && def.sprite != null &&
                    (def.tiledSize == Vector2.zero || def.tiledSize == Vector2.one))
                    def.tiledSize = def.sprite.bounds.size;
            }
            if (def.drawMode != SpriteDrawMode.Simple)
            {
                // 안전망: 크기가 0이면 스프라이트 크기(없으면 1)로.
                if (def.tiledSize == Vector2.zero)
                    def.tiledSize = def.sprite != null ? (Vector2)def.sprite.bounds.size : Vector2.one;

                def.tiledSize = EditorGUILayout.Vector2Field("크기(Tiled, 월드단위)", def.tiledSize);
                if (def.drawMode == SpriteDrawMode.Tiled)
                    def.tileMode = (SpriteTileMode)EditorGUILayout.EnumPopup("Tile Mode", def.tileMode);
                if (def.sprite != null && GUILayout.Button("크기를 스프라이트 1장 크기로 리셋"))
                    def.tiledSize = def.sprite.bounds.size;
                EditorGUILayout.HelpBox("Tiled/Sliced는 스프라이트 임포트 Mesh Type = Full Rect 필요.", MessageType.Info);
                if (def.sprite != null && GUILayout.Button("스프라이트 Full Rect로 설정"))
                    SetSpriteFullRect(def.sprite);
            }
            EditorGUI.indentLevel--;
        }

        _foldCollider = SectionHeader("Collider · 막힘 영역", _foldCollider, ColCollider);
        if (_foldCollider)
        {
            EditorGUI.indentLevel++;
            def.colliderMode = (Prop2DDefinition.ColliderMode)
                EditorGUILayout.EnumPopup("모드", def.colliderMode);
            def.isTrigger = EditorGUILayout.Toggle("트리거(통과 가능)", def.isTrigger);

            switch (def.colliderMode)
            {
                case Prop2DDefinition.ColliderMode.Box:
                    def.boxSizeScale = EditorGUILayout.Vector2Field("크기 배율(1=그림크기)", def.boxSizeScale);
                    def.boxOffset = EditorGUILayout.Vector2Field("중심 오프셋", def.boxOffset);
                    break;
                case Prop2DDefinition.ColliderMode.Polygon:
                    EditorGUILayout.HelpBox("스프라이트 외곽선(physics shape)을 따라 자동 생성됩니다. " +
                        "Sprite Editor의 Custom Physics Shape로 외곽을 다듬을 수 있습니다.", MessageType.Info);
                    break;
                case Prop2DDefinition.ColliderMode.Composite:
                    DrawCompositeList(def);
                    break;
            }
            EditorGUI.indentLevel--;
        }

        _foldShadow = SectionHeader("Shadow · 투영 그림자", _foldShadow, ColShadow);
        if (_foldShadow)
        {
            EditorGUI.indentLevel++;
            def.castShadow = EditorGUILayout.Toggle("그림자 드리움", def.castShadow);
            if (def.castShadow)
            {
                def.shadowCasting = (Prop2DDefinition.ShadowCast)
                    EditorGUILayout.EnumPopup("Casting Option", def.shadowCasting);
                def.shadowAlphaCutoff = EditorGUILayout.Slider("Alpha Cutoff (빛 통과)", def.shadowAlphaCutoff, 0f, 1f);
                EditorGUILayout.HelpBox("ShadowCaster2D(Light2D가 빛 반대편에 그림자). 빛 차단량은 라이트의 Shadow Intensity(전역). " +
                    "Alpha Cutoff↑ = 스프라이트의 반투명/낮은 알파 부분(창문 등)으로 빛 통과. NoShadow=완전 통과. " +
                    "그림자가 보이려면 씬 Light2D Shadows ON.",
                    MessageType.None);
            }
            EditorGUI.indentLevel--;
        }

        _foldBreak = SectionHeader("Breakable · 파괴", _foldBreak, ColBreak);
        if (_foldBreak)
        {
            EditorGUI.indentLevel++;
            def.breakable = EditorGUILayout.Toggle("파괴 가능", def.breakable);
            if (def.breakable)
            {
                def.breakStages = EditorGUILayout.IntSlider("단계 수 (마지막=파괴)", def.breakStages, 1, 6);
                def.breakHp = EditorGUILayout.FloatField("내구도(HP)", Mathf.Max(0.01f, def.breakHp));
                def.breakDestroy = EditorGUILayout.Toggle("파괴 시 제거 (끄면 잔해)", def.breakDestroy);
                if (!def.breakDestroy)
                    def.breakRubbleSprite = (Sprite)EditorGUILayout.ObjectField("잔해 스프라이트", def.breakRubbleSprite, typeof(Sprite), false);

                EditorGUILayout.Space(2);
                bool wasHittable = def.breakHittable;
                def.breakHittable = EditorGUILayout.Toggle("때려서 부수기 (Health+Hurtbox)", def.breakHittable);
                if (def.breakHittable && !wasHittable) GameLayers.EnsureDestructible();  // 레이어 보장
                if (def.breakHittable)
                    EditorGUILayout.HelpBox("Hurtbox(트리거)를 'Destructible' 레이어에 붙여 플레이어 근접 공격으로 직접 부숨. " +
                        "레이어는 자동 생성되고, PlayerRig를 다시 빌드하면 공격 마스크에 포함됨.", MessageType.None);

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("오버레이 (균열/그을음)", EditorStyles.miniBoldLabel);
                def.breakOverlayIntensity = EditorGUILayout.Slider("세기", def.breakOverlayIntensity, 0f, 2f);
                def.breakCrackColor = EditorGUILayout.ColorField("균열 색", def.breakCrackColor);
                def.breakGrimeColor = EditorGUILayout.ColorField("그을음 색", def.breakGrimeColor);

                EditorGUILayout.Space(2);
                def.breakDrop = EditorGUILayout.Toggle("부수면 드랍", def.breakDrop);
                if (def.breakDrop)
                {
                    EditorGUI.indentLevel++;
                    def.breakDropItemId = EditorGUILayout.TextField("고정 아이템 ID", def.breakDropItemId);
                    def.breakDropCount = Mathf.Max(1, EditorGUILayout.IntField("수량", def.breakDropCount));
                    def.breakDropRegionLoot = EditorGUILayout.Toggle("지역 루트 추가", def.breakDropRegionLoot);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.HelpBox("때리면 균열/그을음 오버레이(BRB/DamageOverlay)가 단계별로 진해지고 마지막에 파편 튀며 파괴. " +
                    "베이스 셰이더 무관(오버레이). '때려서 부수기' OFF면 비주얼만 — Breakable.Hit()/ApplyDamage()로 수동 구동.",
                    MessageType.None);
            }
            EditorGUI.indentLevel--;
        }

        _foldAged = SectionHeader("Aged · 낡음/녹/폐허 (이미지 0장)", _foldAged, ColAged);
        if (_foldAged)
        {
            EditorGUI.indentLevel++;
            def.weathered = EditorGUILayout.Toggle("낡음/녹 적용", def.weathered);
            if (def.weathered)
            {
                def.weatherAmount = EditorGUILayout.Slider("낡음 정도", def.weatherAmount, 0f, 1f);
                def.weatherTint = EditorGUILayout.ColorField("풍화 색 (녹=주황갈/이끼=초록/그을음=검정)", def.weatherTint);
                EditorGUILayout.HelpBox("Weathered 부착 — 부서지지 않아도 항상 절차적 녹·그을음으로 낡아 보임(이미지 0장). " +
                    "색만 바꿔 녹/이끼/그을음/물때. Breakable과 같이 켜면 '원래 낡았는데 부서짐'.", MessageType.None);
            }
            EditorGUI.indentLevel--;
        }

        _foldLight = SectionHeader("Light · 발광 (램프/창문/네온)", _foldLight, ColLight);
        if (_foldLight)
        {
            EditorGUI.indentLevel++;
            def.emitsLight = EditorGUILayout.Toggle(
                new GUIContent("발광", "Light2D(Sprite 쿠키) 자식 부착 → 이 프롭이 실제로 주변을 비춤(동적 조명)."), def.emitsLight);
            if (def.emitsLight)
            {
                def.lightShape = (Prop2DDefinition.LightShape)EditorGUILayout.EnumPopup(
                    new GUIContent("모양", "Radial=원형 풀(전구/램프/창문), Cone=부채꼴(스탠드/스포트)."), def.lightShape);
                def.lightColor = EditorGUILayout.ColorField(
                    new GUIContent("빛 색", "흰 쿠키를 이 색으로 틴트(따뜻한 노랑 등)."), def.lightColor);
                def.lightIntensity = EditorGUILayout.FloatField(
                    new GUIContent("세기", "HDR — 1 이상이면 bloom 잘 걸림."), def.lightIntensity);
                def.lightRadius = EditorGUILayout.FloatField(
                    new GUIContent("크기", "월드 단위 — Radial=반경, Cone=길이 대략."), def.lightRadius);
                def.lightOffset = EditorGUILayout.Vector2Field(
                    new GUIContent("원점 오프셋", "발광부(램프 head 등) 위치로 빛 원점 이동."), def.lightOffset);
                if (def.lightShape == Prop2DDefinition.LightShape.Cone)
                    def.lightAngle = EditorGUILayout.FloatField(
                        new GUIContent("방향(도)", "+X=0, 위=90, 아래=-90."), def.lightAngle);
                def.lightCastsShadows = EditorGUILayout.Toggle(
                    new GUIContent("그림자 드리움", "ShadowCaster2D 있는 벽/프롭 너머를 가림."), def.lightCastsShadows);
                def.lightNightOnly = EditorGUILayout.Toggle(
                    new GUIContent("밤에만", "낮엔 꺼짐(DayNightCycle 연동). PropLight2D 부착."), def.lightNightOnly);
                EditorGUILayout.HelpBox("쿠키는 Tools▸TopDown▸Map▸Generate Light Cookies로 생성(Resources/LightCookies). " +
                    "아트에 베이크된 발광과 별개의 동적 조명 — 실제로 주변을 비추고 그림자 드리움.", MessageType.None);
            }
            EditorGUI.indentLevel--;
        }

        // 천장 컷어웨이 — 카테고리=천장일 때만.
        if (def.category == Prop2DDefinition.Category.Ceiling)
        {
            _foldCeiling = SectionHeader("Ceiling · 천장 컷어웨이", _foldCeiling, ColCeiling);
            if (_foldCeiling)
            {
                EditorGUI.indentLevel++;
                def.ceilingGroupId = EditorGUILayout.TextField(
                    new GUIContent("건물 그룹 ID", "같은 ID 천장 조각들이 한 건물로 묶여 함께 페이드. 비우면 조각 단독."),
                    def.ceilingGroupId ?? "");
                def.ceilingHiddenAlpha = EditorGUILayout.Slider(
                    new GUIContent("숨김 알파", "플레이어가 안에 있을 때 지붕 알파(0=투명, 0.3=반투명 유지)."),
                    def.ceilingHiddenAlpha, 0f, 1f);
                def.ceilingFadeSpeed = EditorGUILayout.FloatField(
                    new GUIContent("페이드 속도", "알파/초. 클수록 빠르게 사라짐/복귀."), def.ceilingFadeSpeed);

                EditorGUILayout.Space(2);
                def.ceilingTriggerSize = EditorGUILayout.Vector2Field(
                    new GUIContent("트리거 크기(0=자동)", "컷어웨이 감지 영역 크기. (0,0)=스프라이트 footprint. " +
                        "80° 틸트로 밑둥(앞면)이 길면 세로를 키워 입구까지 덮으면 진입 즉시 페이드."), def.ceilingTriggerSize);
                def.ceilingTriggerOffset = EditorGUILayout.Vector2Field(
                    new GUIContent("트리거 오프셋", "감지 영역 중심 이동. 입구가 아래쪽이면 Y를 음수로 내려 밑둥/입구를 덮음."),
                    def.ceilingTriggerOffset);

                if (def.sortingLayer != "Ceiling")
                    EditorGUILayout.HelpBox("천장은 최상단 정렬 권장 — Visual의 Sorting Layer를 'Ceiling'으로.", MessageType.Warning);
                EditorGUILayout.HelpBox("배치 시 빌더가 트리거 + CeilingFader 자동 부착. 플레이어 진입 → 부드럽게 페이드아웃.\n" +
                    "입구(밑둥)에서 바로 사라지게 하려면 트리거 크기 세로↑ + 오프셋 Y↓(또는 씬에서 배치본의 BoxCollider2D를 직접 늘려도 됨).", MessageType.None);
                EditorGUI.indentLevel--;
            }
        }

        _foldFunction = SectionHeader("Function · 기능 (스폰/탈출/지도판/수색 등)", _foldFunction, ColFunction);
        if (_foldFunction)
        {
            EditorGUI.indentLevel++;
            def.function = (Prop2DDefinition.Function)EditorGUILayout.EnumPopup("기능", def.function);
            if (def.function != Prop2DDefinition.Function.None)
                def.noVisual = EditorGUILayout.Toggle("투명 마커(비주얼 없음)", def.noVisual);
            switch (def.function)
            {
                case Prop2DDefinition.Function.None:
                    EditorGUILayout.HelpBox("기능 없음 — 장식/막힘 전용 프롭.", MessageType.None);
                    break;
                case Prop2DDefinition.Function.SpawnPoint:
                    def.spawnPointId = EditorGUILayout.TextField("Spawn Point ID", def.spawnPointId);
                    EditorGUILayout.HelpBox("씬 전환 도착 지점. 보통 '투명 마커'로 사용.", MessageType.None);
                    break;
                case Prop2DDefinition.Function.Interactable:
                    def.interactType = (InteractableObject.InteractType)
                        EditorGUILayout.EnumPopup("상호작용 종류", def.interactType);
                    def.functionPrompt = EditorGUILayout.TextField("프롬프트(비우면 기본)", def.functionPrompt);
                    def.interactRange = EditorGUILayout.FloatField("인식 범위(m)", def.interactRange);
                    EditorGUILayout.HelpBox("탈출구=ExitPoint, 지도판=MapBoard, 침대/작업대 등. (NPC/문은 전용 기능 사용)", MessageType.None);
                    break;
                case Prop2DDefinition.Function.LootContainer:
                    def.lootGridWidth = EditorGUILayout.IntField("격자 가로", def.lootGridWidth);
                    def.lootGridHeight = EditorGUILayout.IntField("격자 세로", def.lootGridHeight);
                    def.lootUseRegionLoot = EditorGUILayout.Toggle("지역 루트 사용", def.lootUseRegionLoot);
                    def.functionPrompt = EditorGUILayout.TextField("프롬프트(비우면 '수색')", def.functionPrompt);
                    def.interactRange = EditorGUILayout.FloatField("인식 범위(m)", def.interactRange);
                    EditorGUILayout.HelpBox("수색 가능 컨테이너 — LootContainer+상호작용 자동 부착. 일반 프랍에도 부여 가능.", MessageType.None);
                    break;
                case Prop2DDefinition.Function.ItemDrop:
                    def.itemId = EditorGUILayout.TextField("아이템 ID(비우면 지역루트)", def.itemId);
                    def.itemCount = EditorGUILayout.IntField("수량", def.itemCount);
                    EditorGUILayout.HelpBox("바닥에 아이템 스폰(ItemSpawnPoint). ID 지정=Fixed, 비우면 지역 루트 Ground.", MessageType.None);
                    break;
                case Prop2DDefinition.Function.NPC:
                    def.npcId = EditorGUILayout.TextField("NPC ID (Resources/Data/NPC/)", def.npcId);
                    def.functionPrompt = EditorGUILayout.TextField("프롬프트(비우면 '대화')", def.functionPrompt);
                    def.interactRange = EditorGUILayout.FloatField("인식 범위(m)", def.interactRange);
                    EditorGUILayout.HelpBox("NPCController + 상호작용(NPC) 부착. npcId로 NPCData 자동 로드.", MessageType.None);
                    break;
                case Prop2DDefinition.Function.Door:
                    def.doorLockType = (DoorController.LockType)EditorGUILayout.EnumPopup("잠금", def.doorLockType);
                    if (def.doorLockType == DoorController.LockType.Key)
                        def.doorKeyId = EditorGUILayout.TextField("필요 열쇠 ID", def.doorKeyId);
                    else if (def.doorLockType == DoorController.LockType.Quest)
                        def.doorQuestId = EditorGUILayout.TextField("필요 퀘스트 ID", def.doorQuestId);
                    def.functionPrompt = EditorGUILayout.TextField("프롬프트(비우면 '문')", def.functionPrompt);
                    def.interactRange = EditorGUILayout.FloatField("인식 범위(m)", def.interactRange);
                    EditorGUILayout.HelpBox("DoorController + 상호작용(Door). 프롭의 콜라이더가 문 차단/판정에 쓰임.", MessageType.None);
                    break;
                case Prop2DDefinition.Function.Trigger:
                    def.triggerTargetScene = EditorGUILayout.TextField("전환 씬", def.triggerTargetScene);
                    def.triggerTargetSpawnId = EditorGUILayout.TextField("도착 Spawn ID", def.triggerTargetSpawnId);
                    def.triggerSize = EditorGUILayout.Vector2Field("영역 크기", def.triggerSize);
                    def.triggerAutoEnter = EditorGUILayout.Toggle("즉시 전환", def.triggerAutoEnter);
                    EditorGUILayout.HelpBox("플레이어가 영역에 들어오면 씬 전환(MapTriggerZone2D). 보통 '투명 마커'.", MessageType.None);
                    break;
            }
            EditorGUI.indentLevel--;
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(def, "Edit Prop2D");
            EditorUtility.SetDirty(def);
        }

        // 액션 버튼 — 미리보기 위에 둬서 항상 보이게(큰 미리보기에 가려지지 않도록).
        // 파괴/생성 동작은 플래그로 모았다가 레이아웃 종료 후 실행(드로잉 중 def 파괴 방지).
        bool doCommit = false, doCancel = false, doDelete = false;
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        var prevBtnBg = GUI.backgroundColor;
        if (creating)
        {
            GUI.backgroundColor = new Color(0.40f, 0.85f, 0.50f); // 생성=초록 강조
            if (GUILayout.Button("생성 & 등록", GUILayout.Height(26))) doCommit = true;
            GUI.backgroundColor = prevBtnBg;
            if (GUILayout.Button("취소", GUILayout.Width(60), GUILayout.Height(26))) doCancel = true;
        }
        else
        {
            GUI.backgroundColor = new Color(0.45f, 0.70f, 1f); // 저장=파랑 강조
            if (GUILayout.Button("저장", GUILayout.Height(24))) { SaveOrUpdatePrefab(_selected); AssetDatabase.SaveAssets(); }
            GUI.backgroundColor = prevBtnBg;
            if (GUILayout.Button("복제", GUILayout.Width(56), GUILayout.Height(24))) DuplicateSelected();
            if (GUILayout.Button("포커스", GUILayout.Width(56), GUILayout.Height(24))) EditorGUIUtility.PingObject(_selected);
            GUI.backgroundColor = new Color(1f, 0.50f, 0.45f); // 삭제=빨강
            if (GUILayout.Button("삭제", GUILayout.Width(56), GUILayout.Height(24))) doDelete = true;
            GUI.backgroundColor = prevBtnBg;
        }
        EditorGUILayout.EndHorizontal();

        _foldPreview = SectionHeader("Preview · 미리보기 (콜라이더 = 빨강)", _foldPreview, ColPreview);
        if (_foldPreview) DrawPreview(def);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        if (doCommit) CommitDraft();
        else if (doCancel) CancelDraft();
        else if (doDelete) DeleteSelected();
    }

    void DrawCompositeList(Prop2DDefinition def)
    {
        def.compositeBoxes ??= new List<Prop2DDefinition.ColliderBox>();
        EditorGUILayout.LabelField($"박스 {def.compositeBoxes.Count}개", EditorStyles.miniBoldLabel);

        int removeAt = -1;
        for (int i = 0; i < def.compositeBoxes.Count; i++)
        {
            var box = def.compositeBoxes[i];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"#{i}", GUILayout.Width(28));
            if (GUILayout.Button("삭제", GUILayout.Width(50))) removeAt = i;
            EditorGUILayout.EndHorizontal();
            box.center = EditorGUILayout.Vector2Field("중심", box.center);
            box.size = EditorGUILayout.Vector2Field("크기", box.size);
            def.compositeBoxes[i] = box;
            EditorGUILayout.EndVertical();
        }
        if (removeAt >= 0) { def.compositeBoxes.RemoveAt(removeAt); EditorUtility.SetDirty(def); }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("＋ 박스 추가"))
        { def.compositeBoxes.Add(new Prop2DDefinition.ColliderBox(Vector2.zero, Vector2.one)); EditorUtility.SetDirty(def); }
        if (GUILayout.Button("＋ 스프라이트 크기 박스") && def.sprite != null)
        {
            var b = def.sprite.bounds;
            def.compositeBoxes.Add(new Prop2DDefinition.ColliderBox(b.center, b.size));
            EditorUtility.SetDirty(def);
        }
        EditorGUILayout.EndHorizontal();
    }

    // ───────────────────────── 미리보기 + 콜라이더 오버레이 ─────────────────────────
    void DrawPreview(Prop2DDefinition def)
    {
        const float MaxSize = 260f;
        Bounds b = def.sprite != null ? def.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);

        float aspect = b.size.y > 0.0001f ? b.size.x / b.size.y : 1f;
        float w = MaxSize, h = MaxSize;
        if (aspect >= 1f) h = MaxSize / aspect; else w = MaxSize * aspect;

        Rect outer = GUILayoutUtility.GetRect(MaxSize, MaxSize);
        Rect img = new Rect(outer.x + (outer.width - w) * 0.5f, outer.y, w, h);
        EditorGUI.DrawRect(outer, new Color(0.15f, 0.15f, 0.15f, 1f));

        if (def.sprite != null && def.sprite.texture != null)
        {
            var tr = def.sprite.textureRect;
            var tex = def.sprite.texture;
            var tc = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            GUI.DrawTextureWithTexCoords(img, tex, tc, true);
        }

        var fill = new Color(1f, 0.2f, 0.2f, 0.22f);
        var line = new Color(1f, 0.25f, 0.25f, 0.9f);

        Vector2 W2G(Vector2 wpt)
        {
            float nx = b.size.x > 0.0001f ? (wpt.x - b.min.x) / b.size.x : 0.5f;
            float ny = b.size.y > 0.0001f ? (wpt.y - b.min.y) / b.size.y : 0.5f;
            return new Vector2(img.x + nx * img.width, img.y + (1f - ny) * img.height);
        }
        void DrawBox(Vector2 center, Vector2 size)
        {
            Vector2 g1 = W2G(center - size * 0.5f);
            Vector2 g2 = W2G(center + size * 0.5f);
            var r = Rect.MinMaxRect(Mathf.Min(g1.x, g2.x), Mathf.Min(g1.y, g2.y),
                                    Mathf.Max(g1.x, g2.x), Mathf.Max(g1.y, g2.y));
            EditorGUI.DrawRect(r, fill);
            DrawRectOutline(r, line);
        }

        switch (def.colliderMode)
        {
            case Prop2DDefinition.ColliderMode.Box:
                if (def.sprite != null)
                    DrawBox((Vector2)b.center + def.boxOffset, Vector2.Scale(b.size, def.boxSizeScale));
                break;
            case Prop2DDefinition.ColliderMode.Composite:
                if (def.compositeBoxes != null)
                    foreach (var cb in def.compositeBoxes)
                        DrawBox(cb.center, cb.size == Vector2.zero ? Vector2.one : cb.size);
                break;
            case Prop2DDefinition.ColliderMode.Polygon:
                DrawPolygon(def.sprite, W2G, line);
                break;
        }
    }

    static void DrawRectOutline(Rect r, Color c)
    {
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1), c);
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1, r.width, 1), c);
        EditorGUI.DrawRect(new Rect(r.x, r.y, 1, r.height), c);
        EditorGUI.DrawRect(new Rect(r.xMax - 1, r.y, 1, r.height), c);
    }

    static void DrawPolygon(Sprite sprite, System.Func<Vector2, Vector2> w2g, Color c)
    {
        if (sprite == null) return;
        int n = sprite.GetPhysicsShapeCount();
        if (n == 0) return;
        Handles.color = c;
        var pts = new List<Vector2>();
        for (int s = 0; s < n; s++)
        {
            sprite.GetPhysicsShape(s, pts);
            if (pts.Count < 2) continue;
            for (int i = 0; i < pts.Count; i++)
                Handles.DrawLine(w2g(pts[i]), w2g(pts[(i + 1) % pts.Count]));
        }
    }

    // ───────────────────────── 씬 클릭 배치 ─────────────────────────
    void OnSceneGUI(SceneView sv)
    {
        var e = Event.current;

        // 크기조절 모드: 선택 프롭을 가장자리 핸들로 앵커(반대쪽 고정) 리사이즈.
        if (_resizeMode)
        {
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            { _resizeMode = false; e.Use(); Repaint(); return; }
            DrawResizeHandles();
            sv.Repaint();
            return;
        }

        if (!_placeMode || _selected == null) { DestroyGhost(); return; }

        // ESC = 배치 모드 취소
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            _placeMode = false;
            DestroyGhost();
            e.Use();
            Repaint();
            return;
        }

        // 커서 → z=0 평면 월드좌표 + 스냅
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector3 p = ray.origin;
        if (Mathf.Abs(ray.direction.z) > 1e-5f)
            p = ray.origin + ray.direction * (-ray.origin.z / ray.direction.z);
        p.z = 0f;
        if (_snap > 0f)
        {
            p.x = Mathf.Round(p.x / _snap) * _snap;
            p.y = Mathf.Round(p.y / _snap) * _snap;
        }

        UpdateGhost(p);                                   // 실제 스프라이트 고스트 미리보기
        Handles.color = new Color(0.3f, 1f, 0.4f, 0.9f);
        Handles.DrawWireCube(p, Vector3.one * (_snap > 0 ? _snap : 1f));

        // 클릭이 씬 선택으로 새지 않도록 가로채기
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        // 좌클릭/드래그 = 배치(연속). (삭제는 씬에서 직접 — 우클릭은 가로채지 않음)
        if (!e.alt && e.button == 0 && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
        {
            float spacing = Mathf.Max(0.05f, _snap);
            if (e.type == EventType.MouseDown || Vector3.Distance(p, _lastPaintPos) >= spacing)
            {
                PlaceAt(p);
                _lastPaintPos = p;
            }
            e.Use();
        }
        sv.Repaint();
    }

    // ── 크기조절 (앵커=반대쪽 고정 에지 드래그) ──
    void DrawResizeHandles()
    {
        var go = Selection.activeGameObject;
        var sr = go != null ? go.GetComponent<SpriteRenderer>() : null;
        if (sr == null || sr.sprite == null)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(8, 8, 300, 24), EditorStyles.helpBox);
            GUILayout.Label("크기조절: 씬에서 프롭(SpriteRenderer)을 선택하세요.", EditorStyles.miniLabel);
            GUILayout.EndArea();
            Handles.EndGUI();
            return;
        }

        Bounds b = sr.bounds;                       // 월드 AABB(피벗·스케일 반영)
        float hs = HandleUtility.GetHandleSize(b.center) * 0.13f;
        bool sym = Event.current.shift;             // Shift=양쪽 대칭(중심 고정)

        // 4 에지 — 드래그하면 반대쪽(또는 중심) 고정하고 그 축만 늘림.
        DrawEdge(sr, new Vector3(b.max.x, b.center.y, 0f), Vector3.right, 0, true,  hs, sym);
        DrawEdge(sr, new Vector3(b.min.x, b.center.y, 0f), Vector3.right, 0, false, hs, sym);
        DrawEdge(sr, new Vector3(b.center.x, b.max.y, 0f), Vector3.up,    1, true,  hs, sym);
        DrawEdge(sr, new Vector3(b.center.x, b.min.y, 0f), Vector3.up,    1, false, hs, sym);

        Handles.color = new Color(0.3f, 0.8f, 1f, 0.9f);
        Handles.DrawWireCube(b.center, new Vector3(b.size.x, b.size.y, 0.001f));
    }

    void DrawEdge(SpriteRenderer sr, Vector3 pos, Vector3 dir, int axis, bool maxEdge, float hs, bool sym)
    {
        Handles.color = new Color(0.3f, 0.95f, 1f, 1f);
        EditorGUI.BeginChangeCheck();
        Vector3 np = Handles.Slider(pos, dir, hs, Handles.CubeHandleCap, 0f);
        if (EditorGUI.EndChangeCheck())
            ResizeEdge(sr, axis, maxEdge, np[axis], sym);
    }

    /// <summary>axis(0=x,1=y)의 한 에지를 newWorldCoord로 옮기되 반대 에지(또는 중심)를 고정한 채 리사이즈.
    /// Tiled/Sliced=sr.size 변경, 그 외=transform.localScale. sym=양쪽 대칭.</summary>
    void ResizeEdge(SpriteRenderer sr, int axis, bool maxEdge, float newWorldCoord, bool sym)
    {
        var t = sr.transform;
        Bounds b = sr.bounds;
        float oldSize = b.size[axis];
        if (oldSize < 1e-4f) return;

        float anchor, newSize;
        if (sym)                       // 중심 고정 — 양쪽 대칭
        {
            anchor = b.center[axis];
            newSize = Mathf.Max(0.05f, Mathf.Abs(newWorldCoord - anchor) * 2f);
        }
        else                           // 반대 에지 고정(피벗)
        {
            anchor = maxEdge ? b.min[axis] : b.max[axis];
            newSize = Mathf.Max(0.05f, maxEdge ? (newWorldCoord - anchor) : (anchor - newWorldCoord));
        }
        float factor = newSize / oldSize;

        Undo.RecordObject(t, "Resize Prop");
        if (sr.drawMode != SpriteDrawMode.Simple)
        {
            Undo.RecordObject(sr, "Resize Prop");
            var sz = sr.size; sz[axis] = Mathf.Max(0.05f, sz[axis] * factor); sr.size = sz;
        }
        else
        {
            var ls = t.localScale; ls[axis] *= factor; t.localScale = ls;
        }

        // 고정 좌표 복원(반대 에지 또는 중심이 제자리에 있도록 위치 보정).
        Bounds nb = sr.bounds;
        float newAnchor = sym ? nb.center[axis] : (maxEdge ? nb.min[axis] : nb.max[axis]);
        var p = t.position; p[axis] += anchor - newAnchor; t.position = p;

        EditorUtility.SetDirty(t);
        EditorUtility.SetDirty(sr);
    }

    // ── 고스트 미리보기 ──
    void UpdateGhost(Vector3 pos)
    {
        if (_ghost == null || _ghostDef != _selected)
        {
            DestroyGhost();
            _ghost = Prop2DBuilder.Build(_selected);
            _ghost.name = "[Prop2D Ghost]";
            _ghost.hideFlags = HideFlags.HideAndDontSave;
            foreach (var c in _ghost.GetComponentsInChildren<Collider2D>())
                Object.DestroyImmediate(c);                 // 고스트는 물리 없음
            foreach (var sr in _ghost.GetComponentsInChildren<SpriteRenderer>())
            { var col = sr.color; col.a = 0.5f; sr.color = col; }  // 반투명
            _ghostDef = _selected;
        }
        _ghost.transform.position = pos;
    }

    void DestroyGhost()
    {
        if (_ghost != null) Object.DestroyImmediate(_ghost);
        _ghost = null;
        _ghostDef = null;
    }

    void PlaceAt(Vector3 pos)
    {
        // 프리팹이 있으면 인스턴스화(링크 유지), 없으면 즉석 빌드.
        GameObject go = _selected.prefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(_selected.prefab)
            : Prop2DBuilder.Build(_selected);
        go.transform.position = pos;
        go.transform.SetParent(MapRoot(true), true);  // 맵 부모 하위로
        Undo.RegisterCreatedObjectUndo(go, "Place Prop2D");
        Selection.activeGameObject = go;
    }

    /// <summary>맵 부모 오브젝트. "Map"(없으면 기존 "Props" 호환) 사용, create면 없을 때 "Map" 생성.</summary>
    Transform MapRoot(bool create)
    {
        var go = GameObject.Find("Map");
        if (go == null) go = GameObject.Find("Props"); // 기존 맵툴 씬 호환
        if (go == null && create)
        {
            go = new GameObject("Map");
            Undo.RegisterCreatedObjectUndo(go, "Create Map Root");
        }
        return go != null ? go.transform : null;
    }

    // ───────────────────────── 에셋/프리팹 생성·갱신 ─────────────────────────
    const string PrefabFolder = PropFolder + "/Prefabs";

    void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PropFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "Props2D");
    }

    /// <summary>정의로부터 프리팹(SpriteRenderer+Collider2D)을 생성/갱신하고 def.prefab에 연결.</summary>
    void SaveOrUpdatePrefab(Prop2DDefinition def)
    {
        if (def == null) return;
        EnsureFolder();
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder(PropFolder, "Prefabs");

        if (def.emitsLight) LightCookieGenerator.EnsureCookies();  // 발광 쿠키 없으면 먼저 생성
        var temp = Prop2DBuilder.Build(def);           // 임시 GO 조립
        string path = $"{PrefabFolder}/{MakeSafe(def.propId)}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);                 // 씬에서 정리
        def.prefab = prefab;
        EditorUtility.SetDirty(def);
        AssetDatabase.SaveAssets();
    }

    /// <summary>씬에 깔아둔 배치(맵)를 프리팹으로 저장.
    /// 저장 루트 = 선택된 오브젝트(있으면) 또는 'Map'/'Props'. 프리팹 인스턴스 중첩 그대로 보존.</summary>
    void SaveMapPrefab()
    {
        var root = Selection.activeGameObject;
        if (root == null) { var t = MapRoot(false); root = t != null ? t.gameObject : null; }
        if (root == null)
        {
            EditorUtility.DisplayDialog("맵 저장",
                "저장할 루트가 없습니다. 씬에서 맵 루트(예: 'Props' 또는 'Map')를 선택하거나, 맵툴 씬을 사용하세요.", "확인");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Maps"))
            AssetDatabase.CreateFolder("Assets", "Maps");

        string path = EditorUtility.SaveFilePanelInProject(
            "맵 프리팹 저장", root.name, "prefab", "배치를 프리팹으로 저장", "Assets/Maps");
        if (string.IsNullOrEmpty(path)) return;

        // 씬 편집 자동연동: BoxCollider2D 있는 프롭에 ColliderAutoFit 자동 부착(없을 때만) → 저장본에 포함.
        int autoFit = AttachColliderAutoFitUnder(root);
        if (autoFit > 0) Debug.Log($"[Prop Catalog] ColliderAutoFit {autoFit}개 자동 부착(맵 저장).");

        // 천장 반투명이 켜져 있으면 저장 전 불투명 복원(프리팹에 반투명 구워지지 않게), 저장 후 재적용.
        bool wasXray = _ceilingXray;
        if (wasXray) SetCeilingAlpha(1f);
        // 씬 오브젝트를 프리팹으로 저장하고 인스턴스로 연결(중첩 프리팹 참조 유지).
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.UserAction);
        AssetDatabase.SaveAssets();
        if (wasXray) SetCeilingAlpha(_ceilingXrayAlpha);
        Debug.Log($"[Prop Catalog] 맵 프리팹 저장: {path} (루트 '{root.name}')");

        // 저장 시 DontSave 런타임 자식(발밑 그림자·손상/풍화 오버레이)이 떨어져 에디터에서 사라짐 → 강제 재생성.
        var savedRoot = root;
        EditorApplication.delayCall += () =>
        {
            if (savedRoot == null) return;
            foreach (var g in savedRoot.GetComponentsInChildren<GroundShadow2D>(true)) if (g != null) g.Rebuild();
            foreach (var w in savedRoot.GetComponentsInChildren<Weathered>(true)) if (w != null) w.Rebuild();
            foreach (var b in savedRoot.GetComponentsInChildren<Breakable>(true)) if (b != null) b.RebuildOverlay();
        };
    }

    /// <summary>root 하위에서 SpriteRenderer + BoxCollider2D를 가진 프롭에 ColliderAutoFit을 부착(없을 때만).
    /// 씬에서 스프라이트/Tiled 크기를 바꾸면 콜라이더가 따라가게 — 맵 저장 시 일괄 적용.</summary>
    static int AttachColliderAutoFitUnder(GameObject root)
    {
        if (root == null) return 0;
        int n = 0;
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null || sr.sprite == null) continue;
            var go = sr.gameObject;
            var box = go.GetComponent<BoxCollider2D>();
            if (box == null) continue;                                  // Box 전용
            if (box.isTrigger) continue;                                // 트리거(천장 컷어웨이/씬전환 등)는 의도된 영역 — 자동맞춤 제외
            if (go.GetComponent<CeilingFader>() != null) continue;      // 천장 컷어웨이 트리거 보호
            if (go.GetComponent<ColliderAutoFit>() != null) continue;   // 이미 있음

            // 현재 콜라이더를 보존하도록 scale/offset 역산(이후 스프라이트 변경엔 비례 추종).
            Vector2 prevSize = box.size, prevOffset = box.offset;
            Vector2 baseSize, center;
            if (sr.drawMode != SpriteDrawMode.Simple)
            {
                baseSize = sr.size;
                var sb = sr.sprite.bounds;
                center = new Vector2(
                    sb.size.x > 1e-5f ? baseSize.x * (sb.center.x / sb.size.x) : 0f,
                    sb.size.y > 1e-5f ? baseSize.y * (sb.center.y / sb.size.y) : 0f);
            }
            else
            {
                var b = sr.sprite.bounds;
                baseSize = b.size;
                center = b.center;
            }

            var fit = go.AddComponent<ColliderAutoFit>();   // OnEnable이 기본값(1,0)으로 한 번 맞춤 → 아래서 보정
            fit.sizeScale = new Vector2(
                baseSize.x > 1e-5f ? prevSize.x / baseSize.x : 1f,
                baseSize.y > 1e-5f ? prevSize.y / baseSize.y : 1f);
            fit.offset = prevOffset - center;
            fit.Refit();                                    // 보존 값으로 즉시 재맞춤(원래 콜라이더 복원)
            n++;
        }
        return n;
    }

    /// <summary>스프라이트 임포트 Mesh Type을 Full Rect로 (Tiled/Sliced 정상 렌더용).</summary>
    static void SetSpriteFullRect(Sprite sprite)
    {
        string path = AssetDatabase.GetAssetPath(sprite);
        if (AssetImporter.GetAtPath(path) is not TextureImporter imp) return;
        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(settings);
        imp.SaveAndReimport();
    }

    /// <summary>'＋ 새 항목' — 바로 에셋을 만들지 않고 미저장 드래프트를 시작한다.
    /// 폼에서 이미지·머티리얼 등 등록 후 '생성 & 등록'으로 실제 에셋 생성.</summary>
    void CreateNewProp()
    {
        CancelDraft(); // 기존 드래프트 정리
        _draft = CreateInstance<Prop2DDefinition>();
        _draft.propId = NextAutoId(_tab);
        _draft.displayName = _draft.propId;
        _draft.category = _tab;
        // 분류 필터 중이면 그 분류로 시작(연속 등록 편하게). 전체/미분류면 빈 분류.
        if (!string.IsNullOrEmpty(_groupFilter)) _draft.group = _groupFilter;
        _draft.colliderMode = DefaultCollider(_tab);
        _draft.castShadow = DefaultCastShadow(_tab); // 벽·프롭은 그림자 기본 ON
        // 오브젝트 탭은 기능 항목이 기본 — Interactable로 시작하고 Function 섹션을 펼쳐 보여줌.
        if (_tab == Prop2DDefinition.Category.Object)
        {
            _draft.function = Prop2DDefinition.Function.Interactable;
            _foldFunction = true;
        }
        // 천장 탭은 최상단 'Ceiling' 정렬 레이어 기본 + 천장 섹션 펼침.
        if (_tab == Prop2DDefinition.Category.Ceiling)
        {
            GameLayers.EnsureSortingLayer("Ceiling");
            _draft.sortingLayer = "Ceiling";
            _foldCeiling = true;
        }
        _selected = null;
    }

    /// <summary>드래프트를 실제 에셋으로 저장·등록.</summary>
    void CommitDraft()
    {
        if (_draft == null) return;
        EnsureFolder();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{PropFolder}/{MakeSafe(_draft.propId)}.asset");
        AssetDatabase.CreateAsset(_draft, path);
        SaveOrUpdatePrefab(_draft);   // SO + 프리팹 동시 생성
        AssetDatabase.SaveAssets();
        var created = _draft;
        _draft = null;
        Refresh();
        _selected = created;
    }

    /// <summary>드래프트 취소(미저장 인스턴스 폐기).</summary>
    void CancelDraft()
    {
        if (_draft != null) { Object.DestroyImmediate(_draft); _draft = null; }
    }

    /// <summary>카테고리별 접두어. floor_/wall_/prop_/object_/decal_.</summary>
    static string CatPrefix(Prop2DDefinition.Category c) => c.ToString().ToLowerInvariant();

    /// <summary>등록된 모든 분류(컬렉션) — 카테고리 가로지름. 폼/일괄 ▾ 드롭다운용.</summary>
    List<string> AllGroups() => _props
        .Where(p => p != null && !string.IsNullOrEmpty(p.group))
        .Select(p => p.group).Distinct().OrderBy(s => s).ToList();

    /// <summary>해당 카테고리 탭에 존재하는 분류 목록. 팔레트 분류 필터용.</summary>
    List<string> GroupsInTab(Prop2DDefinition.Category cat) => _props
        .Where(p => p != null && p.category == cat && !string.IsNullOrEmpty(p.group))
        .Select(p => p.group).Distinct().OrderBy(s => s).ToList();

    /// <summary>기존 분류 선택 메뉴(▾). 선택 시 setter로 값 전달. current=현재 체크 표시용.</summary>
    void ShowGroupMenu(System.Action<string> setter, string current)
    {
        var groups = AllGroups();
        var menu = new GenericMenu();
        if (groups.Count == 0)
            menu.AddDisabledItem(new GUIContent("(등록된 분류 없음 — 직접 입력)"));
        else
            foreach (var g in groups)
            {
                string gg = g; // 클로저 캡처
                menu.AddItem(new GUIContent(gg), (current ?? "") == gg, () => { setter(gg); Repaint(); });
            }
        menu.ShowAsContext();
    }

    /// <summary>다음 자동 ID — 카테고리 접두어 + 4자리 번호 (wall_0001, floor_0001 …).</summary>
    string NextAutoId(Prop2DDefinition.Category cat)
    {
        string prefix = CatPrefix(cat);
        int n = 1;
        string id;
        do { id = $"{prefix}_{n:0000}"; n++; }
        while (_props.Exists(p => p != null && p.propId == id));
        return id;
    }

    /// <summary>Project에서 선택한 스프라이트(또는 텍스처의 스프라이트)마다 Prop 정의를 자동 생성.</summary>
    void BatchImportSelectedSprites()
    {
        var sprites = new List<Sprite>();
        foreach (var obj in Selection.objects)
        {
            if (obj is Sprite s) sprites.Add(s);
            else if (obj is Texture2D tex)
            {
                string path = AssetDatabase.GetAssetPath(tex);
                sprites.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>());
            }
        }
        sprites = sprites.Distinct().ToList();
        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("일괄 등록", "Project에서 스프라이트(또는 스프라이트 텍스처)를 선택한 뒤 누르세요.", "확인");
            return;
        }

        EnsureFolder();
        string prefix = CatPrefix(_tab);
        // ID는 스프라이트 이름이 아니라 '인덱스 증가'로 항상 고유 (이름이 같아도 충돌 X — 이게 무반응 버그 원인이었음).
        var usedIdx = new HashSet<int>();
        var rx = new System.Text.RegularExpressions.Regex(
            $@"^{System.Text.RegularExpressions.Regex.Escape(prefix)}_(\d+)$");
        foreach (var p in _props)
        {
            if (p == null) continue;
            var m = rx.Match(p.propId ?? "");
            if (m.Success) usedIdx.Add(int.Parse(m.Groups[1].Value));
        }
        // 같은 카테고리에 이미 등록된 '스프라이트'는 건너뜀(재등록 시 중복 방지).
        var existingSprites = new HashSet<Sprite>(
            _props.Where(p => p != null && p.category == _tab && p.sprite != null).Select(p => p.sprite));

        int next = 1, made = 0, skipped = 0;
        foreach (var sp in sprites)
        {
            if (existingSprites.Contains(sp)) { skipped++; continue; } // 이미 이 스프라이트로 등록됨
            while (usedIdx.Contains(next)) next++;
            string id = $"{prefix}_{next:0000}";
            usedIdx.Add(next);
            existingSprites.Add(sp);

            var def = CreateInstance<Prop2DDefinition>();
            def.propId = id;
            def.displayName = sp.name;                 // 표시 이름은 스프라이트 이름(중복 무방)
            def.sprite = sp;
            def.category = _tab;                       // 현재 탭으로 분류
            def.colliderMode = DefaultCollider(_tab);  // 카테고리별 기본 콜라이더
            def.castShadow = DefaultCastShadow(_tab);  // 벽·프롭은 그림자 기본 ON
            def.material = _batchMaterial;             // 일괄 설정 머티리얼
            def.sortingLayer = _batchSortingLayer;     // 일괄 설정 Sorting Layer
            def.sortingOffset = _batchSortingOffset;   // 일괄 설정 Order in Layer
            def.drawMode = _batchDrawMode;             // 일괄 설정 Draw Mode
            def.castShadow = _batchCastShadow;         // 일괄 설정 그림자
            def.shadowCasting = _batchShadowCasting;   // 일괄 설정 Casting Option
            def.group = _batchGroup;                   // 일괄 설정 분류(컬렉션)
            if (_batchDrawMode != SpriteDrawMode.Simple)
                def.tiledSize = sp.bounds.size;        // Tiled 기본 크기 = 스프라이트 1장 (이후 조절)
            AssetDatabase.CreateAsset(def, AssetDatabase.GenerateUniqueAssetPath($"{PropFolder}/{MakeSafe(id)}.asset"));
            SaveOrUpdatePrefab(def);                   // 프리팹도 생성
            made++;
        }
        AssetDatabase.SaveAssets();
        Refresh();
        _groupFilter = null;   // 방금 등록한 항목이 분류 필터에 가려지지 않게 '전체'로
        _search = "";          // 검색어도 초기화(가려짐 방지)
        string summary = $"신규 {made}개 등록"
            + (skipped > 0 ? $" · {skipped}개는 이미 등록된 스프라이트(건너뜀)" : "");
        Debug.Log($"[Prop Catalog] 일괄 등록 — {summary} (선택 {sprites.Count}개).");
        EditorUtility.DisplayDialog("일괄 등록", summary, "확인");
    }

    /// <summary>기존 Prop2D 정의 중 ID가 인덱스 형식({prefix}_숫자)이 아닌 것을 인덱스로 정규화.
    /// propId + .asset 파일명 + 연결 프리팹(.prefab) 파일명을 함께 변경(GUID/참조 보존).</summary>
    [MenuItem("Tools/TopDown/맵/프롭 ID 정규화")]
    static void NormalizePropIds()
    {
        var all = AssetDatabase.FindAssets("t:Prop2DDefinition")
            .Select(g => AssetDatabase.LoadAssetAtPath<Prop2DDefinition>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(p => p != null).ToList();
        if (all.Count == 0) { EditorUtility.DisplayDialog("정규화", "Prop2D 정의가 없습니다.", "확인"); return; }

        var rx = new System.Text.RegularExpressions.Regex(@"^(.+)_(\d+)$");
        // 카테고리(접두어)별로 이미 쓰인 인덱스 수집 → 충돌 없는 번호 배정.
        var used = new Dictionary<string, HashSet<int>>();
        foreach (var p in all)
        {
            var m = rx.Match(p.propId ?? "");
            if (!m.Success) continue;
            string pre = m.Groups[1].Value;
            if (!used.TryGetValue(pre, out var s)) used[pre] = s = new HashSet<int>();
            s.Add(int.Parse(m.Groups[2].Value));
        }

        int renamed = 0;
        var log = new System.Text.StringBuilder();
        foreach (var p in all)
        {
            if (rx.IsMatch(p.propId ?? "")) continue;   // 이미 {prefix}_{숫자} → 유지

            string prefix = CatPrefix(p.category);
            if (!used.TryGetValue(prefix, out var s)) used[prefix] = s = new HashSet<int>();
            int n = 1; while (s.Contains(n)) n++;
            s.Add(n);
            string newId = $"{prefix}_{n:0000}";
            string oldId = p.propId;

            p.propId = newId;
            EditorUtility.SetDirty(p);

            // .asset 파일명 변경 (GUID 유지 → 참조 보존)
            string assetPath = AssetDatabase.GetAssetPath(p);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string err = AssetDatabase.RenameAsset(assetPath, newId);
                if (!string.IsNullOrEmpty(err)) Debug.LogWarning($"[정규화] '{oldId}' asset 이름변경 실패: {err}");
            }
            // 연결 프리팹도 같은 이름으로(있으면). RenameAsset이 GUID 유지하므로 def.prefab 참조는 그대로 유효.
            if (p.prefab != null)
            {
                string pfp = AssetDatabase.GetAssetPath(p.prefab);
                if (!string.IsNullOrEmpty(pfp)) AssetDatabase.RenameAsset(pfp, newId);
            }
            log.AppendLine($"{oldId} → {newId}");
            renamed++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Prop Catalog] Prop ID 정규화 {renamed}개:\n{log}");
        EditorUtility.DisplayDialog("정규화",
            renamed > 0 ? $"{renamed}개를 인덱스 형식으로 변경했습니다.\n(목록은 Console)" : "이미 모두 인덱스 형식입니다.", "확인");
    }

    /// <summary>카테고리별 기본 콜라이더: 바닥·데칼·천장=없음(통과), 그 외=Box(막힘).
    /// (천장은 막힘 없음 — 컷어웨이 트리거는 빌더가 따로 부착.)</summary>
    static Prop2DDefinition.ColliderMode DefaultCollider(Prop2DDefinition.Category cat)
        => (cat == Prop2DDefinition.Category.Floor
            || cat == Prop2DDefinition.Category.Decal
            || cat == Prop2DDefinition.Category.Ceiling)
            ? Prop2DDefinition.ColliderMode.None
            : Prop2DDefinition.ColliderMode.Box;

    /// <summary>카테고리별 기본 그림자: 벽·프롭=ON(발밑 고정 + 투영). 바닥/데칼/오브젝트마커=OFF.</summary>
    static bool DefaultCastShadow(Prop2DDefinition.Category cat)
        => cat == Prop2DDefinition.Category.Wall || cat == Prop2DDefinition.Category.Prop;

    static string MakeSafe(string s)
    {
        foreach (var c in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s;
    }

    /// <summary>프로젝트 Sorting Layer 드롭다운. (Tags & Layers에서 정의한 레이어 목록)</summary>
    static string SortingLayerPopup(string label, string current)
    {
        var names = SortingLayer.layers.Select(l => l.name).ToArray();
        if (names.Length == 0) return current;
        int idx = System.Array.IndexOf(names, current);
        if (idx < 0) idx = System.Array.IndexOf(names, "Default");
        if (idx < 0) idx = 0;
        idx = EditorGUILayout.Popup(label, idx, names);
        return names[idx];
    }

    /// <summary>선택 항목을 복제(같은 카테고리 유지). 옛 카탈로그 "복제" 기능.</summary>
    void DuplicateSelected()
    {
        if (_selected == null) return;
        EnsureFolder();
        var copy = Instantiate(_selected);
        copy.propId = _selected.propId + "_copy";
        copy.displayName = (_selected.displayName ?? _selected.propId) + " (복제)";
        copy.prefab = null; // 원본 프리팹 공유 방지 — 아래에서 새로 생성
        string path = AssetDatabase.GenerateUniqueAssetPath($"{PropFolder}/{MakeSafe(copy.propId)}.asset");
        AssetDatabase.CreateAsset(copy, path);
        SaveOrUpdatePrefab(copy); // 복제본 전용 프리팹 생성
        AssetDatabase.SaveAssets();
        Refresh();
        _selected = copy;
    }

    void DeleteSelected()
    {
        if (_selected == null) return;
        if (!EditorUtility.DisplayDialog("삭제", $"'{_selected.displayName}' 프롭 정의를 삭제할까요?", "삭제", "취소"))
            return;
        string path = AssetDatabase.GetAssetPath(_selected);
        _selected = null;
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.SaveAssets();
        Refresh();
    }
}
