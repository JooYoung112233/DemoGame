using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 바닥 데칼 스캐터 브러시 — 씬 뷰에서 드래그로 금/얼룩/물때/이끼 데칼을 흩뿌린다(터레인 디테일 페인팅 느낌).
/// ② 자유 페인트: 브러시 반경 안에 랜덤 위치·회전·스케일·색 지터로 데칼 인스턴스 생성.
/// ③ 가장자리 자동: 선택한 SpriteRenderer(벽/구조물)의 외곽선을 따라 데칼을 흩뿌려 경계를 자연스럽게.
/// 데칼은 SpriteRenderer + 지정 머티리얼(BRB/DecalPixel 권장)로, "FloorDecals" 부모 아래 생성. Undo 지원.
/// 메뉴: Tools ▸ TopDown ▸ Map ▸ Decal Scatter Brush
/// </summary>
public class DecalScatterBrush : EditorWindow
{
    [SerializeField] Material decalMaterial;
    [SerializeField] List<Sprite> sprites = new();
    [SerializeField] string sortingLayer = "Default";
    [SerializeField] int sortingOrder = 1;
    [SerializeField] float radius = 1.5f;
    [SerializeField] float spacing = 0.4f;        // 드래그 중 최소 간격
    [SerializeField] int perStamp = 3;            // 한 번 찍을 때 개수
    [SerializeField] Vector2 scaleRange = new(0.5f, 1.2f);
    [SerializeField] bool randomRotation = true;
    [SerializeField, Range(0f, 0.8f)] float colorJitter = 0.15f;
    [SerializeField] Color tint = Color.white;
    [SerializeField] Transform parent;
    [SerializeField] bool eraseMode = false;
    [SerializeField] bool brushActive = false;

    // ③ 가장자리
    [SerializeField, Range(0f, 1f)] float edgeInset = 0.15f;   // 안쪽으로 당기는 정도
    [SerializeField] float edgeDensity = 0.45f;                // 가장자리 점 간격

    Vector3 _lastPaintPos;
    bool _hasLast;
    Vector2 _scroll;

    [MenuItem("Tools/TopDown/Map/Decal Scatter Brush")]
    static void Open() => GetWindow<DecalScatterBrush>("Decal Scatter");

    void OnEnable() => SceneView.duringSceneGui += OnScene;
    void OnDisable() => SceneView.duringSceneGui -= OnScene;

    // ───────────────── 창 UI ─────────────────
    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField("데칼 스캐터 브러시", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("씬 뷰에서 좌클릭 드래그 = 데칼 흩뿌리기. " +
            "브러시 켜면 클릭 선택이 막힘(끄면 일반 편집). Alt+드래그는 카메라 조작.", MessageType.Info);

        brushActive = EditorGUILayout.ToggleLeft(brushActive ? "■ 브러시 ON (씬에서 칠하기)" : "□ 브러시 OFF", brushActive);
        eraseMode = EditorGUILayout.ToggleLeft(eraseMode ? "지우개 모드 (브러시 안 데칼 삭제)" : "그리기 모드", eraseMode);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("데칼 에셋", EditorStyles.miniBoldLabel);
        decalMaterial = (Material)EditorGUILayout.ObjectField("머티리얼(DecalPixel)", decalMaterial, typeof(Material), false);
        if (decalMaterial == null)
            EditorGUILayout.HelpBox("BRB/DecalPixel 머티리얼 지정 권장(블렌드 프리셋·마스크 모드 적용). 비우면 기본 스프라이트 머티리얼.", MessageType.None);

        EditorGUILayout.LabelField("스프라이트 변형(랜덤 선택)");
        int del = -1;
        for (int i = 0; i < sprites.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            sprites[i] = (Sprite)EditorGUILayout.ObjectField(sprites[i], typeof(Sprite), false);
            if (GUILayout.Button("-", GUILayout.Width(22))) del = i;
            EditorGUILayout.EndHorizontal();
        }
        if (del >= 0) sprites.RemoveAt(del);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ 빈 칸")) sprites.Add(null);
        if (GUILayout.Button("선택한 스프라이트 추가")) AddSelectedSprites();
        if (GUILayout.Button("비우기")) sprites.Clear();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("브러시", EditorStyles.miniBoldLabel);
        radius = EditorGUILayout.Slider("반경", radius, 0.1f, 8f);
        spacing = EditorGUILayout.Slider("드래그 간격", spacing, 0.05f, 4f);
        perStamp = EditorGUILayout.IntSlider("한 번에 개수", perStamp, 1, 20);
        scaleRange = EditorGUILayout.Vector2Field("스케일 범위(min,max)", scaleRange);
        randomRotation = EditorGUILayout.Toggle("랜덤 회전", randomRotation);
        colorJitter = EditorGUILayout.Slider("색 지터(어둡게)", colorJitter, 0f, 0.8f);
        tint = EditorGUILayout.ColorField("틴트(정점색)", tint);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("배치", EditorStyles.miniBoldLabel);
        sortingLayer = EditorGUILayout.TextField("Sorting Layer", sortingLayer);
        sortingOrder = EditorGUILayout.IntField("Order (바닥 위·프롭 아래)", sortingOrder);
        parent = (Transform)EditorGUILayout.ObjectField("부모(비우면 FloorDecals)", parent, typeof(Transform), true);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("③ 가장자리 자동 디테일", EditorStyles.miniBoldLabel);
        edgeInset = EditorGUILayout.Slider("안쪽 당김", edgeInset, 0f, 1f);
        edgeDensity = EditorGUILayout.Slider("가장자리 간격", edgeDensity, 0.1f, 3f);
        using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0 || sprites.Count == 0))
        {
            if (GUILayout.Button($"선택 {Selection.gameObjects.Length}개 가장자리에 흩뿌리기", GUILayout.Height(28)))
                ScatterEdges();
        }
        EditorGUILayout.HelpBox("선택한 SpriteRenderer(벽/구조물)의 외곽 둘레를 따라 데칼을 흩뿌립니다(이끼·잔해·금).", MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    void AddSelectedSprites()
    {
        foreach (var o in Selection.objects)
        {
            if (o is Sprite sp) { if (!sprites.Contains(sp)) sprites.Add(sp); }
            else if (o is Texture2D tex)
            {
                string path = AssetDatabase.GetAssetPath(tex);
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (sub is Sprite s && !sprites.Contains(s)) sprites.Add(s);
            }
        }
    }

    // ───────────────── 씬 뷰 페인트 ─────────────────
    void OnScene(SceneView sv)
    {
        if (!brushActive) return;
        var e = Event.current;
        int id = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(id);   // 클릭 선택 차단

        Vector3 wp = ProjectToZ0(HandleUtility.GUIPointToWorldRay(e.mousePosition));

        Handles.color = eraseMode ? new Color(0.9f, 0.3f, 0.3f, 0.9f) : new Color(0.3f, 0.9f, 0.45f, 0.9f);
        Handles.DrawWireDisc(wp, Vector3.forward, radius);
        sv.Repaint();

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && !e.alt)
        {
            if (eraseMode) EraseAt(wp);
            else if (!_hasLast || Vector3.Distance(wp, _lastPaintPos) >= spacing)
            {
                Stamp(wp);
                _lastPaintPos = wp; _hasLast = true;
            }
            e.Use();
        }
        else if (e.type == EventType.MouseUp) _hasLast = false;
    }

    static Vector3 ProjectToZ0(Ray r)
    {
        if (Mathf.Abs(r.direction.z) > 1e-5f)
        {
            float t = -r.origin.z / r.direction.z;
            Vector3 p = r.origin + r.direction * t;
            return new Vector3(p.x, p.y, 0f);
        }
        return new Vector3(r.origin.x, r.origin.y, 0f);
    }

    void Stamp(Vector3 center)
    {
        if (!HasSprites()) return;
        var par = EnsureParent();
        for (int i = 0; i < perStamp; i++)
        {
            Vector2 off = Random.insideUnitCircle * radius;
            CreateDecal(center + new Vector3(off.x, off.y, 0f), par);
        }
    }

    void EraseAt(Vector3 center)
    {
        SpriteRenderer[] all = parent != null
            ? parent.GetComponentsInChildren<SpriteRenderer>()
            : Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        foreach (var sr in all)
        {
            if (sr == null || !sr.name.StartsWith("Decal_")) continue;
            if (Vector3.Distance(sr.transform.position, center) <= radius)
                Undo.DestroyObjectImmediate(sr.gameObject);
        }
    }

    // ───────────────── ③ 가장자리 ─────────────────
    void ScatterEdges()
    {
        if (!HasSprites()) return;
        var par = EnsureParent();
        foreach (var go in Selection.gameObjects)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;
            Bounds b = sr.bounds;
            float per = 2f * (b.size.x + b.size.y);
            int n = Mathf.Max(4, Mathf.RoundToInt(per / Mathf.Max(0.05f, edgeDensity)));
            float inset = edgeInset * 0.5f * Mathf.Min(b.size.x, b.size.y);
            for (int i = 0; i < n; i++)
            {
                Vector3 p = PerimeterPoint(b, Random.value);
                Vector3 toCenter = (b.center - p); toCenter.z = 0f;
                if (toCenter.sqrMagnitude > 1e-4f) p += toCenter.normalized * inset;
                p += (Vector3)(Random.insideUnitCircle * edgeDensity * 0.4f);
                p.z = 0f;
                CreateDecal(p, par);
            }
        }
    }

    static Vector3 PerimeterPoint(Bounds b, float t01)
    {
        float per = 2f * (b.size.x + b.size.y);
        float d = t01 * per;
        Vector3 min = b.min, max = b.max;
        if (d < b.size.x) return new Vector3(min.x + d, min.y, 0f);
        d -= b.size.x;
        if (d < b.size.y) return new Vector3(max.x, min.y + d, 0f);
        d -= b.size.y;
        if (d < b.size.x) return new Vector3(max.x - d, max.y, 0f);
        d -= b.size.y;
        return new Vector3(min.x, max.y - d, 0f);
    }

    // ───────────────── 공통 ─────────────────
    bool HasSprites()
    {
        foreach (var s in sprites) if (s != null) return true;
        return false;
    }

    void CreateDecal(Vector3 pos, Transform par)
    {
        Sprite sp = null;
        for (int tries = 0; tries < 8 && sp == null; tries++)
            sp = sprites[Random.Range(0, sprites.Count)];
        if (sp == null) return;

        var go = new GameObject("Decal_" + sp.name);
        Undo.RegisterCreatedObjectUndo(go, "Scatter Decal");
        go.transform.SetParent(par, true);
        go.transform.position = pos;
        if (randomRotation) go.transform.rotation = Quaternion.Euler(0f, 0f, Random.value * 360f);
        float s = Random.Range(Mathf.Min(scaleRange.x, scaleRange.y), Mathf.Max(scaleRange.x, scaleRange.y));
        go.transform.localScale = new Vector3(s, s, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        if (decalMaterial != null) sr.sharedMaterial = decalMaterial;
        if (!string.IsNullOrEmpty(sortingLayer)) sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder;

        float j = colorJitter > 0f ? 1f - Random.value * colorJitter : 1f;
        sr.color = new Color(tint.r * j, tint.g * j, tint.b * j, tint.a);
    }

    Transform EnsureParent()
    {
        if (parent != null) return parent;
        var go = GameObject.Find("FloorDecals");
        if (go == null)
        {
            go = new GameObject("FloorDecals");
            Undo.RegisterCreatedObjectUndo(go, "Create FloorDecals");
        }
        parent = go.transform;
        return parent;
    }
}
