using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 탑다운 2D 프롭 카탈로그 에디터.
/// Prop2DDefinition 에셋 등록/편집/미리보기 + 콜라이더(막힘) 시각 편집.
/// 편의기능: 검색, 스프라이트 일괄 등록, 씬 클릭 배치.
/// 메뉴: Tools ▸ TopDown 2D ▸ Prop Catalog
/// </summary>
public class Prop2DCatalogEditor : EditorWindow
{
    const string PropFolder = "Assets/Resources/Props2D";

    [MenuItem("Tools/TopDown 2D/Prop Catalog")]
    static void Open() => GetWindow<Prop2DCatalogEditor>("2D Prop Catalog");

    List<Prop2DDefinition> _props = new();
    Prop2DDefinition _selected;
    Vector2 _listScroll, _formScroll;
    string _search = "";
    bool _placeMode;
    float _snap = 1f;
    Prop2DDefinition.Category _tab = Prop2DDefinition.Category.Prop;
    static readonly string[] TabNames = { "바닥", "벽", "프롭", "오브젝트" };

    void OnEnable()
    {
        Refresh();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

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
        EditorGUILayout.BeginHorizontal();
        DrawList();
        DrawDivider();
        DrawForm();
        EditorGUILayout.EndHorizontal();
    }

    // ───────────────────────── 좌측: 목록 ─────────────────────────
    void DrawList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(300));

        // 카테고리 탭
        int newTab = GUILayout.Toolbar((int)_tab, TabNames);
        if (newTab != (int)_tab)
        {
            _tab = (Prop2DDefinition.Category)newTab;
            _selected = _props.FirstOrDefault(p => p != null && p.category == _tab); // 새 탭의 첫 항목(없으면 null)
        }

        int count = _props.Count(p => p != null && p.category == _tab);
        EditorGUILayout.LabelField($"{TabNames[(int)_tab]} ({count})", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("＋ 새 항목")) CreateNewProp();
        if (GUILayout.Button("새로고침", GUILayout.Width(64))) Refresh();
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("선택 스프라이트 일괄 등록"))
            BatchImportSelectedSprites();

        _search = EditorGUILayout.TextField("검색", _search);

        EditorGUILayout.Space(4);
        _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
        Prop2DDefinition toDuplicate = null, toDelete = null;
        foreach (var p in _props)
        {
            if (p == null || p.category != _tab) continue;
            string label = !string.IsNullOrEmpty(p.displayName) ? p.displayName : p.propId;
            if (!string.IsNullOrEmpty(_search) &&
                label.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            bool sel = p == _selected;
            EditorGUILayout.BeginHorizontal(sel ? "selectionRect" : "box");

            // 썸네일
            var thumb = GUILayoutUtility.GetRect(28, 28, GUILayout.Width(28), GUILayout.Height(28));
            if (p.sprite != null && p.sprite.texture != null)
            {
                var trc = p.sprite.textureRect; var tx = p.sprite.texture;
                GUI.DrawTextureWithTexCoords(thumb, tx,
                    new Rect(trc.x / tx.width, trc.y / tx.height, trc.width / tx.width, trc.height / tx.height), true);
            }

            // 이름 + 정보(클릭=선택)
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button(label, sel ? EditorStyles.boldLabel : EditorStyles.label)) _selected = p;
            bool blocks = p.colliderMode != Prop2DDefinition.ColliderMode.None && !p.isTrigger;
            EditorGUILayout.LabelField($"{p.colliderMode}  막힘:{(blocks ? "O" : "X")}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button(sel ? "편집중" : "편집", GUILayout.Width(46))) _selected = p;
            if (GUILayout.Button("복제", GUILayout.Width(38))) toDuplicate = p;
            if (GUILayout.Button("X", GUILayout.Width(22))) toDelete = p;

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        // 루프 밖에서 처리(컬렉션 변경 안전)
        if (toDuplicate != null) { _selected = toDuplicate; DuplicateSelected(); }
        if (toDelete != null) { _selected = toDelete; DeleteSelected(); }
        EditorGUILayout.EndVertical();
    }

    void DrawDivider()
    {
        var r = GUILayoutUtility.GetRect(1, 1, GUILayout.Width(1), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(r, new Color(0, 0, 0, 0.3f));
    }

    // ───────────────────────── 우측: 폼 + 미리보기 ─────────────────────────
    void DrawForm()
    {
        EditorGUILayout.BeginVertical();

        // 씬 배치 도구바
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        _placeMode = GUILayout.Toggle(_placeMode, " 씬에 클릭 배치", "Button", GUILayout.Width(110));
        GUILayout.Label("스냅", GUILayout.Width(30));
        _snap = EditorGUILayout.FloatField(_snap, GUILayout.Width(40));
        GUILayout.Label(_placeMode ? "Scene 뷰에서 좌클릭 → 선택 프롭 배치" : "", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        if (_selected == null)
        {
            EditorGUILayout.HelpBox("왼쪽 목록에서 항목을 클릭하면 여기서 편집합니다. 없으면 '＋ 새 항목'.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        var def = _selected;
        EditorGUILayout.LabelField($"✏ 편집 중: {(string.IsNullOrEmpty(def.displayName) ? def.propId : def.displayName)}",
            EditorStyles.boldLabel);
        _formScroll = EditorGUILayout.BeginScrollView(_formScroll);
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        // ID는 자동 생성(수동 입력 X). 편집 시 읽기전용 표시.
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.TextField("Prop ID (자동)", def.propId);
        def.displayName = EditorGUILayout.TextField("표시 이름", def.displayName);
        def.category = (Prop2DDefinition.Category)EditorGUILayout.EnumPopup("카테고리(탭)", def.category);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        def.sprite = (Sprite)EditorGUILayout.ObjectField("스프라이트", def.sprite, typeof(Sprite), false);
        def.material = (Material)EditorGUILayout.ObjectField("머티리얼(선택)", def.material, typeof(Material), false);
        def.sortingOffset = EditorGUILayout.IntField("정렬 오프셋", def.sortingOffset);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Collider (막힘 영역)", EditorStyles.boldLabel);
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

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(def, "Edit Prop2D");
            EditorUtility.SetDirty(def);
        }

        // 액션 버튼 — 미리보기 위에 둬서 항상 보이게(큰 미리보기에 가려지지 않도록).
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("저장")) AssetDatabase.SaveAssets();
        if (GUILayout.Button("복제", GUILayout.Width(56))) DuplicateSelected();
        if (GUILayout.Button("포커스", GUILayout.Width(56))) EditorGUIUtility.PingObject(_selected);
        if (GUILayout.Button("삭제", GUILayout.Width(56))) DeleteSelected();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("미리보기 (콜라이더 = 빨강)", EditorStyles.boldLabel);
        DrawPreview(def);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
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
        if (!_placeMode || _selected == null) return;
        var e = Event.current;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector3 p = ray.origin;
        if (Mathf.Abs(ray.direction.z) > 1e-5f)
        {
            float t = -ray.origin.z / ray.direction.z; // z=0 평면 교차
            p = ray.origin + ray.direction * t;
        }
        p.z = 0f;
        if (_snap > 0f)
        {
            p.x = Mathf.Round(p.x / _snap) * _snap;
            p.y = Mathf.Round(p.y / _snap) * _snap;
        }

        Handles.color = Color.green;
        Handles.DrawWireCube(p, Vector3.one * (_snap > 0 ? _snap : 1f));

        // 클릭이 오브젝트 선택으로 새지 않도록 가로채기
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            PlaceAt(p);
            e.Use();
        }
        sv.Repaint();
    }

    void PlaceAt(Vector3 pos)
    {
        var go = Prop2DBuilder.Build(_selected);
        go.transform.position = pos;
        var props = GameObject.Find("Props");
        if (props != null) go.transform.SetParent(props.transform);
        Undo.RegisterCreatedObjectUndo(go, "Place Prop2D");
        Selection.activeGameObject = go;
    }

    // ───────────────────────── 에셋 생성/삭제/일괄등록 ─────────────────────────
    void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PropFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "Props2D");
    }

    void CreateNewProp()
    {
        EnsureFolder();
        int n = _props.Count + 1;
        string id;
        do { id = $"prop_{n:0000}"; n++; }
        while (_props.Exists(p => p != null && p.propId == id));

        var def = CreateInstance<Prop2DDefinition>();
        def.propId = id;
        def.displayName = id;
        def.category = _tab;
        def.colliderMode = DefaultCollider(_tab);
        AssetDatabase.CreateAsset(def, $"{PropFolder}/{id}.asset");
        AssetDatabase.SaveAssets();
        Refresh();
        _selected = def;
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
            string id = $"prop_{sp.name}";
            if (existing.Contains(id)) continue;
            var def = CreateInstance<Prop2DDefinition>();
            def.propId = id;
            def.displayName = sp.name;
            def.sprite = sp;
            def.category = _tab;                       // 현재 탭으로 분류
            def.colliderMode = DefaultCollider(_tab);  // 카테고리별 기본 콜라이더
            string assetName = MakeSafe(id);
            AssetDatabase.CreateAsset(def, AssetDatabase.GenerateUniqueAssetPath($"{PropFolder}/{assetName}.asset"));
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

    static string MakeSafe(string s)
    {
        foreach (var c in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s;
    }

    /// <summary>선택 항목을 복제(같은 카테고리 유지). 옛 카탈로그 "복제" 기능.</summary>
    void DuplicateSelected()
    {
        if (_selected == null) return;
        EnsureFolder();
        var copy = Instantiate(_selected);
        copy.propId = _selected.propId + "_copy";
        copy.displayName = (_selected.displayName ?? _selected.propId) + " (복제)";
        string path = AssetDatabase.GenerateUniqueAssetPath($"{PropFolder}/{MakeSafe(copy.propId)}.asset");
        AssetDatabase.CreateAsset(copy, path);
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
