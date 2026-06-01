using UnityEditor;
using UnityEngine;

/// <summary>
/// 히트박스 타임라인 에디터 (Tools ▸ TopDown Combat ▸ Attack Editor).
/// AttackData의 각 HitWindow를 프레임(정규화 0~1) 타임라인 + 2D 탑다운 프리뷰로 편집.
/// - 타임라인: 스크러버로 시점 이동, 윈도우 막대 드래그로 시작/끝 조절
/// - 프리뷰: facing=오른쪽 기준 캐릭터 위에 히트박스(Box/Circle) 표시. 현재 시점 활성 윈도우는 진하게.
///   중심 핸들 드래그로 offset 이동.
/// </summary>
public class AttackDataEditorWindow : EditorWindow
{
    AttackData data;
    int selected = -1;
    float scrub = 0.3f;
    bool draggingCenter;
    int draggingEdge = -1; // 0=start, 1=end
    Vector2 scroll;

    const float PreviewPPU = 60f; // pixels per world unit

    [MenuItem("Tools/TopDown Combat/Attack Editor")]
    static void Open() => GetWindow<AttackDataEditorWindow>("Attack Editor").minSize = new Vector2(420, 600);

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        data = (AttackData)EditorGUILayout.ObjectField("Attack Data", data, typeof(AttackData), false);
        if (data == null)
        {
            EditorGUILayout.HelpBox("편집할 AttackData를 지정하거나 새로 생성하세요.", MessageType.Info);
            if (GUILayout.Button("새 AttackData 생성")) CreateNew();
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("공격 기본", EditorStyles.boldLabel);
        data.attackId = EditorGUILayout.TextField("Attack Id", data.attackId);
        data.duration = Mathf.Max(0.05f, EditorGUILayout.FloatField("Duration (초)", data.duration));
        data.damage   = EditorGUILayout.FloatField("Damage", data.damage);
        data.groggy   = EditorGUILayout.FloatField("Groggy", data.groggy);

        EditorGUILayout.Space();
        DrawTimeline();

        EditorGUILayout.Space();
        DrawPreview();

        EditorGUILayout.Space();
        DrawWindowInspector();

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(data);

        EditorGUILayout.EndScrollView();
    }

    // ── 타임라인 ──────────────────────────────────────────────
    void DrawTimeline()
    {
        EditorGUILayout.LabelField($"타임라인  (시점 {scrub:F2} → {scrub * data.duration * 1000f:F0}ms)", EditorStyles.boldLabel);

        Rect r = GUILayoutUtility.GetRect(10, 10000, 60, 60);
        EditorGUI.DrawRect(r, new Color(0.16f, 0.16f, 0.18f));

        // 윈도우 막대
        for (int i = 0; i < data.windows.Count; i++)
        {
            var w = data.windows[i];
            float x0 = Mathf.Lerp(r.x, r.xMax, w.startNorm);
            float x1 = Mathf.Lerp(r.x, r.xMax, w.endNorm);
            var bar = new Rect(x0, r.y + 8 + i * 14 % (r.height - 16), Mathf.Max(2, x1 - x0), 10);
            bool sel = i == selected;
            EditorGUI.DrawRect(bar, sel ? new Color(1f, 0.7f, 0.2f) : new Color(0.4f, 0.6f, 1f, 0.8f));
            if (Event.current.type == EventType.MouseDown && bar.Contains(Event.current.mousePosition))
            {
                selected = i;
                Event.current.Use();
                Repaint();
            }
        }

        // 스크러버 라인
        float sx = Mathf.Lerp(r.x, r.xMax, scrub);
        EditorGUI.DrawRect(new Rect(sx - 1, r.y, 2, r.height), Color.white);

        // 스크러버 드래그
        var e = Event.current;
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && r.Contains(e.mousePosition) && e.button == 0)
        {
            // 윈도우 막대를 안 눌렀을 때만 스크럽
            if (e.type == EventType.MouseDrag || draggingEdge < 0)
            {
                scrub = Mathf.Clamp01((e.mousePosition.x - r.x) / r.width);
                Repaint();
            }
        }

        EditorGUILayout.Space();
        scrub = EditorGUILayout.Slider("스크러버", scrub, 0f, 1f);
    }

    // ── 프리뷰 (2D 탑다운, facing=오른쪽) ────────────────────
    void DrawPreview()
    {
        EditorGUILayout.LabelField("프리뷰 (facing → 오른쪽)", EditorStyles.boldLabel);
        Rect r = GUILayoutUtility.GetRect(10, 10000, 240, 240);
        EditorGUI.DrawRect(r, new Color(0.1f, 0.11f, 0.13f));

        Vector2 origin = r.center; // 캐릭터 위치
        // 그리드
        Handles.BeginGUI();
        Handles.color = new Color(1, 1, 1, 0.08f);
        for (float gx = -3; gx <= 3; gx++)
            Handles.DrawLine(W2S(new Vector2(gx, -3), origin), W2S(new Vector2(gx, 3), origin));
        for (float gy = -3; gy <= 3; gy++)
            Handles.DrawLine(W2S(new Vector2(-3, gy), origin), W2S(new Vector2(3, gy), origin));

        // 캐릭터 + facing 화살표
        Handles.color = new Color(0.3f, 0.9f, 0.4f);
        Handles.DrawWireDisc(origin, Vector3.forward, 0.35f * PreviewPPU);
        Handles.color = Color.green;
        Handles.DrawLine(origin, W2S(new Vector2(0.9f, 0), origin));

        // 윈도우 박스/원
        for (int i = 0; i < data.windows.Count; i++)
        {
            var w = data.windows[i];
            bool active = w.IsActiveAt(scrub);
            bool sel = i == selected;
            Color c = active ? new Color(1f, 0.3f, 0.25f) : new Color(0.5f, 0.5f, 0.55f, 0.5f);
            if (sel) c = active ? new Color(1f, 0.8f, 0.2f) : new Color(0.9f, 0.8f, 0.4f, 0.6f);
            Handles.color = c;

            Vector2 centerW = w.offset; // facing=+x 기준 (프리뷰는 오른쪽이 전방)
            Vector2 centerS = W2S(centerW, origin);

            if (w.shape == HitboxShape.Box)
            {
                DrawRotatedBox(centerS, w.boxSize * PreviewPPU, w.angle, c);
            }
            else
            {
                Handles.DrawWireDisc(centerS, Vector3.forward, w.radius * PreviewPPU);
            }

            // 중심 핸들 (선택된 것만 드래그)
            if (sel)
            {
                var hr = new Rect(centerS.x - 5, centerS.y - 5, 10, 10);
                EditorGUIUtility.AddCursorRect(hr, MouseCursor.MoveArrow);
                EditorGUI.DrawRect(hr, Color.yellow);

                var e = Event.current;
                if (e.type == EventType.MouseDown && hr.Contains(e.mousePosition)) { draggingCenter = true; e.Use(); }
                if (draggingCenter && e.type == EventType.MouseDrag)
                {
                    w.offset = S2W(e.mousePosition, origin);
                    EditorUtility.SetDirty(data);
                    e.Use(); Repaint();
                }
                if (e.type == EventType.MouseUp) draggingCenter = false;
            }
        }
        Handles.EndGUI();
    }

    void DrawRotatedBox(Vector2 center, Vector2 size, float angleDeg, Color c)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 ax = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * (size.x * 0.5f);
        Vector2 ay = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad)) * (size.y * 0.5f);
        // 스크린 y축은 아래로 증가 → ay 반전
        ay.y = -ay.y; ax.y = -ax.y;
        Vector3[] pts =
        {
            center + ax + ay, center + ax - ay, center - ax - ay, center - ax + ay, center + ax + ay
        };
        Handles.color = c;
        Handles.DrawAAPolyLine(2f, pts);
    }

    // 월드(전방x, 좌y) → 스크린. 화면 y 아래증가라 -y.
    Vector2 W2S(Vector2 w, Vector2 origin) => origin + new Vector2(w.x, -w.y) * PreviewPPU;
    Vector2 S2W(Vector2 s, Vector2 origin) => new Vector2((s.x - origin.x) / PreviewPPU, -(s.y - origin.y) / PreviewPPU);

    // ── 윈도우 속성 ──────────────────────────────────────────
    void DrawWindowInspector()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"히트 윈도우 ({data.windows.Count})", EditorStyles.boldLabel);
        if (GUILayout.Button("+ 추가", GUILayout.Width(60)))
        {
            data.windows.Add(new HitWindow());
            selected = data.windows.Count - 1;
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < data.windows.Count; i++)
        {
            var w = data.windows[i];
            bool sel = i == selected;
            EditorGUILayout.BeginVertical(sel ? EditorStyles.helpBox : GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(sel, $"#{i}  {w.label}  [{w.startNorm:F2}~{w.endNorm:F2}]  {w.shape}", EditorStyles.foldoutHeader) != sel)
                selected = sel ? -1 : i;
            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                data.windows.RemoveAt(i);
                if (selected >= data.windows.Count) selected = data.windows.Count - 1;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            if (sel)
            {
                w.label = EditorGUILayout.TextField("Label", w.label);
                EditorGUILayout.MinMaxSlider(new GUIContent($"활성 구간 {w.startNorm:F2}~{w.endNorm:F2}"),
                    ref w.startNorm, ref w.endNorm, 0f, 1f);
                w.shape = (HitboxShape)EditorGUILayout.EnumPopup("Shape", w.shape);
                w.offset = EditorGUILayout.Vector2Field("Offset (전방x, 좌y)", w.offset);
                if (w.shape == HitboxShape.Box)
                {
                    w.boxSize = EditorGUILayout.Vector2Field("Box Size", w.boxSize);
                    w.angle = EditorGUILayout.Slider("Angle", w.angle, -180f, 180f);
                }
                else
                {
                    w.radius = EditorGUILayout.FloatField("Radius", w.radius);
                }
                w.damageMult = EditorGUILayout.FloatField("Damage ×", w.damageMult);
                w.groggyMult = EditorGUILayout.FloatField("Groggy ×", w.groggyMult);
            }
            EditorGUILayout.EndVertical();
        }
    }

    void CreateNew()
    {
        var a = ScriptableObject.CreateInstance<AttackData>();
        string path = EditorUtility.SaveFilePanelInProject("새 AttackData", "Attack_New", "asset", "저장 위치");
        if (string.IsNullOrEmpty(path)) return;
        AssetDatabase.CreateAsset(a, path);
        AssetDatabase.SaveAssets();
        data = a;
    }
}
