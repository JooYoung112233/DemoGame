using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 탑다운 2D 프롭 카탈로그 에디터.
/// Prop2DDefinition 에셋 등록/편집/미리보기 + 콜라이더(막힘) 시각 편집.
/// 편의기능: 검색, 스프라이트 일괄 등록, 씬 클릭 배치.
/// 메뉴: Tools ▸ TopDown ▸ Map ▸ Prop Catalog
/// </summary>
public class Prop2DCatalogEditor : EditorWindow
{
    const string PropFolder = "Assets/Resources/Props2D";

    [MenuItem("Tools/TopDown/Map/Prop Catalog")]
    static void Open() => GetWindow<Prop2DCatalogEditor>("2D Prop Catalog");

    List<Prop2DDefinition> _props = new();
    Prop2DDefinition _selected;
    Prop2DDefinition _draft; // '＋ 새 항목' 시 만드는 미저장 드래프트(이미지/머티리얼 등록 후 '생성 & 등록')
    Vector2 _listScroll, _formScroll;
    string _search = "";
    bool _placeMode;
    float _snap = 1f;
    GameObject _ghost;          // 씬 배치 고스트 미리보기(HideAndDontSave)
    Prop2DDefinition _ghostDef; // 고스트가 만들어진 정의
    Vector3 _lastPaintPos;      // 드래그 연속 배치 간격 기준
    Material _batchMaterial;     // 일괄 등록 공통 머티리얼
    SpriteDrawMode _batchDrawMode = SpriteDrawMode.Simple; // 일괄 등록 공통 Draw Mode
    string _batchSortingLayer = "Default"; // 일괄 등록 공통 Sorting Layer
    Prop2DDefinition.Category _tab = Prop2DDefinition.Category.Prop;
    static readonly string[] TabNames = { "바닥", "벽", "프롭", "오브젝트" };
    // 섹션 접기/펴기 (목록 + 폼)
    bool _foldList = true, _foldVisual = true, _foldCollider = true, _foldShadow, _foldFunction, _foldPreview = true;
    bool _foldBatch;            // 일괄 등록 설정 접이식(기본 접힘)
    GUIStyle _cellNameStyle;    // 팔레트 셀 이름 스타일(지연 생성)

    void OnEnable()
    {
        Refresh();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        CancelDraft();  // 미저장 드래프트 정리
        DestroyGhost(); // 고스트 미리보기 정리
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

    void OnGUI()
    {
        // 창에 포커스가 있을 때 ESC = 배치 취소 (씬뷰 포커스가 아닐 때 대비)
        if (_placeMode && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            _placeMode = false;
            DestroyGhost();
            Event.current.Use();
        }

        // ── 최상단 바: 맵 생성 / 맵 저장 / 스냅 / 배치 상태 ──
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("＋ 맵 생성", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            var t = MapRoot(true);
            Selection.activeGameObject = t != null ? t.gameObject : null;
        }
        if (GUILayout.Button("맵 저장(프리팹)", EditorStyles.toolbarButton, GUILayout.Width(110)))
            SaveMapPrefab();
        GUILayout.Space(10);
        GUILayout.Label("스냅", GUILayout.Width(30));
        _snap = EditorGUILayout.FloatField(_snap, GUILayout.Width(40));
        GUILayout.FlexibleSpace();
        if (_placeMode)
            GUILayout.Label($"● 배치: {(_selected != null ? _selected.displayName : "")}  (좌클릭/드래그=배치, ESC=취소)",
                EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

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
            _tab = (Prop2DDefinition.Category)newTab;
            _selected = _props.FirstOrDefault(p => p != null && p.category == _tab); // 새 탭의 첫 항목(없으면 null)
        }

        int count = _props.Count(p => p != null && p.category == _tab);
        // 목록 접기/펴기 — 접으면 폼만 크게 본다. (탭은 항상 보임)
        _foldList = EditorGUILayout.Foldout(_foldList, $"{TabNames[(int)_tab]} 팔레트 ({count})", true);
        if (!_foldList) { EditorGUILayout.EndVertical(); return; }

        // 헤더: 배치 토글 + 새 항목 + 새로고침
        EditorGUILayout.BeginHorizontal();
        var prevBg = GUI.backgroundColor;
        if (_placeMode) GUI.backgroundColor = new Color(0.4f, 1f, 0.55f);
        bool wasPlacing = _placeMode;
        _placeMode = GUILayout.Toggle(_placeMode,
            _placeMode ? "● 배치 중 (ESC 종료)" : "배치 모드", "Button", GUILayout.Height(22));
        if (_placeMode && !wasPlacing) SceneView.lastActiveSceneView?.Focus(); // ESC·클릭이 씬뷰로 가도록
        GUI.backgroundColor = prevBg;
        if (GUILayout.Button("＋ 새 항목", GUILayout.Width(90), GUILayout.Height(22))) CreateNewProp();
        if (GUILayout.Button("↻", GUILayout.Width(26), GUILayout.Height(22))) Refresh();
        EditorGUILayout.EndHorizontal();

        _search = EditorGUILayout.TextField("검색", _search);

        // 일괄 등록 (접이식)
        _foldBatch = EditorGUILayout.Foldout(_foldBatch, "일괄 등록 (선택 스프라이트 → 항목 생성)", true);
        if (_foldBatch)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _batchMaterial = (Material)EditorGUILayout.ObjectField("머티리얼", _batchMaterial, typeof(Material), false);
            _batchSortingLayer = SortingLayerPopup("Sorting Layer", _batchSortingLayer);
            _batchDrawMode = (SpriteDrawMode)EditorGUILayout.EnumPopup("Draw Mode", _batchDrawMode);
            if (GUILayout.Button("선택 스프라이트 일괄 등록")) BatchImportSelectedSprites();
            EditorGUILayout.EndVertical();
        }

        // 팔레트 그리드 (썸네일 — 클릭=선택/브러시, 우클릭=메뉴)
        var items = _props.Where(p => p != null && p.category == _tab &&
                (string.IsNullOrEmpty(_search) ||
                 (p.displayName ?? p.propId).IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0))
            .ToList();
        EditorGUILayout.Space(2);
        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Height(190));
        DrawPaletteGrid(items);
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

        EditorGUILayout.LabelField(creating
                ? "🆕 새 항목 만들기 — 이미지·머티리얼 등 채우고 아래 '생성 & 등록'"
                : $"✏ 편집 중: {(string.IsNullOrEmpty(def.displayName) ? def.propId : def.displayName)}",
            EditorStyles.boldLabel);
        _formScroll = EditorGUILayout.BeginScrollView(_formScroll);
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
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

        EditorGUILayout.Space(6);
        _foldVisual = EditorGUILayout.Foldout(_foldVisual, "Visual (스프라이트)", true);
        if (_foldVisual)
        {
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
        }

        EditorGUILayout.Space(6);
        _foldCollider = EditorGUILayout.Foldout(_foldCollider, "Collider (막힘 영역)", true);
        if (_foldCollider)
        {
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
        }

        EditorGUILayout.Space(6);
        _foldShadow = EditorGUILayout.Foldout(_foldShadow, "Shadow (투영 그림자)", true);
        if (_foldShadow)
        {
            def.castShadow = EditorGUILayout.Toggle("그림자 드리움", def.castShadow);
            if (def.castShadow)
            {
                EditorGUILayout.HelpBox("배치 시 ShadowCaster2D 자동 부착 → Light2D(플레이어 등)가 실제 캐스트 그림자를 빛 반대편에 그림. " +
                    "방향·길이는 라이트 위치가 결정. 벽·프롭 기본 ON. (그림자가 보이려면 씬 Light2D에 Shadows 켜야 함.)",
                    MessageType.None);
            }
        }

        EditorGUILayout.Space(6);
        _foldFunction = EditorGUILayout.Foldout(_foldFunction, "Function (기능 — 스폰/탈출/지도판/수색 등)", true);
        if (_foldFunction)
        {
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
        if (creating)
        {
            if (GUILayout.Button("생성 & 등록", GUILayout.Height(24))) doCommit = true;
            if (GUILayout.Button("취소", GUILayout.Width(60), GUILayout.Height(24))) doCancel = true;
        }
        else
        {
            if (GUILayout.Button("저장")) { SaveOrUpdatePrefab(_selected); AssetDatabase.SaveAssets(); }
            if (GUILayout.Button("복제", GUILayout.Width(56))) DuplicateSelected();
            if (GUILayout.Button("포커스", GUILayout.Width(56))) EditorGUIUtility.PingObject(_selected);
            if (GUILayout.Button("삭제", GUILayout.Width(56))) doDelete = true;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        _foldPreview = EditorGUILayout.Foldout(_foldPreview, "미리보기 (콜라이더 = 빨강)", true);
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
        if (!_placeMode || _selected == null) { DestroyGhost(); return; }
        var e = Event.current;

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

        // 씬 오브젝트를 프리팹으로 저장하고 인스턴스로 연결(중첩 프리팹 참조 유지).
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.UserAction);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Prop Catalog] 맵 프리팹 저장: {path} (루트 '{root.name}')");
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
        _draft.colliderMode = DefaultCollider(_tab);
        _draft.castShadow = DefaultCastShadow(_tab); // 벽·프롭은 그림자 기본 ON
        // 오브젝트 탭은 기능 항목이 기본 — Interactable로 시작하고 Function 섹션을 펼쳐 보여줌.
        if (_tab == Prop2DDefinition.Category.Object)
        {
            _draft.function = Prop2DDefinition.Function.Interactable;
            _foldFunction = true;
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

    /// <summary>카테고리별 접두어. floor_/wall_/prop_/object_.</summary>
    static string CatPrefix(Prop2DDefinition.Category c) => c.ToString().ToLowerInvariant();

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
        var existing = new HashSet<string>(_props.Where(p => p != null).Select(p => p.propId));
        int made = 0;
        foreach (var sp in sprites)
        {
            string id = $"{CatPrefix(_tab)}_{sp.name}";  // 카테고리 접두어 (wall_*, floor_* …)
            if (existing.Contains(id)) continue;
            var def = CreateInstance<Prop2DDefinition>();
            def.propId = id;
            def.displayName = sp.name;
            def.sprite = sp;
            def.category = _tab;                       // 현재 탭으로 분류
            def.colliderMode = DefaultCollider(_tab);  // 카테고리별 기본 콜라이더
            def.castShadow = DefaultCastShadow(_tab);  // 벽·프롭은 그림자 기본 ON
            def.material = _batchMaterial;             // 일괄 설정 머티리얼
            def.sortingLayer = _batchSortingLayer;     // 일괄 설정 Sorting Layer
            def.drawMode = _batchDrawMode;             // 일괄 설정 Draw Mode
            if (_batchDrawMode != SpriteDrawMode.Simple)
                def.tiledSize = sp.bounds.size;        // Tiled 기본 크기 = 스프라이트 1장 (이후 조절)
            string assetName = MakeSafe(id);
            AssetDatabase.CreateAsset(def, AssetDatabase.GenerateUniqueAssetPath($"{PropFolder}/{assetName}.asset"));
            SaveOrUpdatePrefab(def);                   // 프리팹도 생성
            existing.Add(id);
            made++;
        }
        AssetDatabase.SaveAssets();
        Refresh();
        Debug.Log($"[Prop Catalog] 스프라이트 {sprites.Count}개 중 {made}개 신규 등록.");
    }

    /// <summary>카테고리별 기본 콜라이더: 바닥=없음(통과), 벽=Box(막힘), 그 외=Box.</summary>
    static Prop2DDefinition.ColliderMode DefaultCollider(Prop2DDefinition.Category cat)
        => cat == Prop2DDefinition.Category.Floor
            ? Prop2DDefinition.ColliderMode.None
            : Prop2DDefinition.ColliderMode.Box;

    /// <summary>카테고리별 기본 그림자: 벽·프롭=ON(발밑 고정 + 투영). 바닥/오브젝트마커=OFF.</summary>
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
