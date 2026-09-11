using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 히트박스 타임라인 에디터 (Tools ▸ TopDown ▸ Combat ▸ Attack Editor).
/// 프레임 단위로 히트박스를 배치/편집. 연속 공격(AttackComboData)은 단계별 탭으로 편집.
/// - 프레임 그리드: 칸 = 1프레임. 윈도우를 startFrame~endFrame 막대로. 스크러버로 프레임 이동.
/// - 2D 프리뷰: facing=오른쪽. 현재 프레임 활성 윈도우 진하게. 중심 핸들 드래그로 offset.
/// - 콤보: steps 탭 [1타][2타]… 으로 단계 전환, 캔슬 프레임 마커 표시.
/// </summary>
// 밸런스·컨트롤 패널의 "전투" 탭에 임베드되는 공격(AttackData) 에디터.
// (별도 창 폐지 — 모든 컨트롤·밸런스는 밸런스·컨트롤 단일 패널에서.)
public class AttackDataEditorWindow
{
    enum Mode { Single, Combo }
    Mode mode = Mode.Single;

    AttackData single;
    AttackComboData combo;
    int comboStep = 0;

    int selectedWindow = -1;
    int scrubFrame = 1;
    bool draggingCenter;
    Vector2 scroll;

    const float PPU = 60f;

    System.Action _repaint;

    /// <summary>밸런스·컨트롤 패널의 "전투" 탭에서 호출. repaint=호스트 패널의 Repaint.</summary>
    public void Draw(System.Action repaint)
    {
        _repaint = repaint;
        scroll = EditorGUILayout.BeginScrollView(scroll);

        mode = (Mode)GUILayout.Toolbar((int)mode, new[] { "단일 공격", "콤보 (연속 공격)" });
        EditorGUILayout.Space();

        AttackData current = null;

        if (mode == Mode.Single)
        {
            single = (AttackData)EditorGUILayout.ObjectField("Attack Data", single, typeof(AttackData), false);
            if (single == null)
            {
                EditorGUILayout.HelpBox("AttackData를 지정하거나 새로 생성하세요.", MessageType.Info);
                if (GUILayout.Button("새 AttackData 생성")) single = CreateAsset<AttackData>("Attack_New");
            }
            current = single;
        }
        else
        {
            combo = (AttackComboData)EditorGUILayout.ObjectField("Combo Data", combo, typeof(AttackComboData), false);
            if (combo == null)
            {
                EditorGUILayout.HelpBox("AttackComboData를 지정하거나 새로 생성하세요.", MessageType.Info);
                if (GUILayout.Button("새 Combo 생성")) combo = CreateAsset<AttackComboData>("Combo_New");
            }
            else current = DrawComboStepBar();
        }

        if (current != null)
        {
            EditorGUI.BeginChangeCheck();
            DrawAttackEditor(current);
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(current);
        }

        EditorGUILayout.EndScrollView();
    }

    // ── 콤보 단계 탭 ──────────────────────────────────────────
    AttackData DrawComboStepBar()
    {
        EditorGUI.BeginChangeCheck();
        combo.comboId = EditorGUILayout.TextField("Combo Id", combo.comboId);
        combo.bufferTime = EditorGUILayout.Slider("선입력 버퍼(초)", combo.bufferTime, 0f, 0.6f);
        if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(combo);

        EditorGUILayout.LabelField("단계", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < combo.StepCount; i++)
        {
            bool on = i == comboStep;
            if (GUILayout.Toggle(on, $"{i + 1}타", "Button", GUILayout.Width(50)) && !on)
                comboStep = i;
        }
        if (GUILayout.Button("+ 단계", GUILayout.Width(60)))
        {
            var a = CreateAsset<AttackData>($"Attack_{combo.comboId}_{combo.StepCount + 1}");
            if (a != null) { combo.steps.Add(a); comboStep = combo.StepCount - 1; EditorUtility.SetDirty(combo); }
        }
        EditorGUILayout.EndHorizontal();

        comboStep = Mathf.Clamp(comboStep, 0, Mathf.Max(0, combo.StepCount - 1));
        var step = combo.GetStep(comboStep);

        EditorGUILayout.BeginHorizontal();
        combo.steps[comboStep] = (AttackData)EditorGUILayout.ObjectField($"{comboStep + 1}타 데이터", step, typeof(AttackData), false);
        if (combo.StepCount > 1 && GUILayout.Button("이 단계 제거", GUILayout.Width(90)))
        {
            combo.steps.RemoveAt(comboStep);
            EditorUtility.SetDirty(combo);
            comboStep = Mathf.Clamp(comboStep, 0, combo.StepCount - 1);
            EditorGUILayout.EndHorizontal();
            return combo.GetStep(comboStep);
        }
        EditorGUILayout.EndHorizontal();

        return combo.GetStep(comboStep);
    }

    // ── 공격 1종 편집 ────────────────────────────────────────
    void DrawAttackEditor(AttackData a)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("공격 기본", EditorStyles.boldLabel);
        a.attackId = EditorGUILayout.TextField("Attack Id", a.attackId);
        EditorGUILayout.BeginHorizontal();
        a.fps = Mathf.Max(1, EditorGUILayout.IntField("FPS", a.fps));
        a.totalFrames = Mathf.Max(1, EditorGUILayout.IntField("총 프레임", a.totalFrames));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField($"   = {a.Duration * 1000f:F0}ms", EditorStyles.miniLabel);
        a.cancelFromFrame = EditorGUILayout.IntSlider("캔슬 가능 프레임", a.cancelFromFrame, -1, a.totalFrames);
        a.damage = EditorGUILayout.FloatField("Damage", a.damage);
        a.groggy = EditorGUILayout.FloatField("Groggy", a.groggy);

        scrubFrame = Mathf.Clamp(scrubFrame, 0, a.totalFrames);

        EditorGUILayout.Space();
        DrawFrameTimeline(a);

        EditorGUILayout.Space();
        DrawPreview(a);

        EditorGUILayout.Space();
        DrawWindowList(a);
    }

    // ── 프레임 그리드 타임라인 ───────────────────────────────
    void DrawFrameTimeline(AttackData a)
    {
        EditorGUILayout.LabelField($"프레임 타임라인  (현재 프레임 {scrubFrame}/{a.totalFrames})", EditorStyles.boldLabel);

        Rect r = GUILayoutUtility.GetRect(10, 10000, 70, 70);
        EditorGUI.DrawRect(r, new Color(0.16f, 0.16f, 0.18f));

        int frames = Mathf.Max(1, a.totalFrames);
        float fw = r.width / frames;

        // 프레임 칸 + 번호
        for (int f = 0; f < frames; f++)
        {
            float x = r.x + f * fw;
            EditorGUI.DrawRect(new Rect(x, r.y, 1, r.height), new Color(1, 1, 1, 0.06f));
            if (frames <= 24)
                GUI.Label(new Rect(x + 2, r.yMax - 14, fw, 12), f.ToString(), EditorStyles.miniLabel);
        }

        // 캔슬 프레임 마커
        if (a.cancelFromFrame >= 0)
        {
            float cx = r.x + a.cancelFromFrame * fw;
            EditorGUI.DrawRect(new Rect(cx - 1, r.y, 2, r.height), new Color(0.3f, 1f, 0.5f, 0.7f));
        }

        // 윈도우 막대
        for (int i = 0; i < a.windows.Count; i++)
        {
            var w = a.windows[i];
            float x0 = r.x + w.startFrame * fw;
            float x1 = r.x + (w.endFrame + 1) * fw;
            var bar = new Rect(x0, r.y + 4 + (i % 4) * 13, Mathf.Max(3, x1 - x0), 11);
            bool sel = i == selectedWindow;
            EditorGUI.DrawRect(bar, sel ? new Color(1f, 0.7f, 0.2f) : new Color(0.4f, 0.6f, 1f, 0.85f));
            GUI.Label(new Rect(bar.x + 2, bar.y - 1, 120, 12), w.label, EditorStyles.miniLabel);
            if (Event.current.type == EventType.MouseDown && bar.Contains(Event.current.mousePosition))
            {
                selectedWindow = i; Event.current.Use(); _repaint?.Invoke();
            }
        }

        // 스크러버 (프레임)
        float sx = r.x + (scrubFrame + 0.5f) * fw;
        EditorGUI.DrawRect(new Rect(sx - 1, r.y, 2, r.height), Color.white);

        var e = Event.current;
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && r.Contains(e.mousePosition) && e.button == 0)
        {
            scrubFrame = Mathf.Clamp(Mathf.FloorToInt((e.mousePosition.x - r.x) / fw), 0, frames);
            _repaint?.Invoke();
        }

        scrubFrame = EditorGUILayout.IntSlider("스크러버 (프레임)", scrubFrame, 0, a.totalFrames);
    }

    // ── 2D 프리뷰 ────────────────────────────────────────────
    void DrawPreview(AttackData a)
    {
        EditorGUILayout.LabelField("프리뷰 (facing → 오른쪽)", EditorStyles.boldLabel);
        Rect r = GUILayoutUtility.GetRect(10, 10000, 220, 220);
        EditorGUI.DrawRect(r, new Color(0.1f, 0.11f, 0.13f));
        Vector2 origin = r.center;

        Handles.BeginGUI();
        Handles.color = new Color(1, 1, 1, 0.08f);
        for (float g = -3; g <= 3; g++)
        {
            Handles.DrawLine(W2S(new Vector2(g, -3), origin), W2S(new Vector2(g, 3), origin));
            Handles.DrawLine(W2S(new Vector2(-3, g), origin), W2S(new Vector2(3, g), origin));
        }
        Handles.color = new Color(0.3f, 0.9f, 0.4f);
        Handles.DrawWireDisc(origin, Vector3.forward, 0.35f * PPU);
        Handles.DrawLine(origin, W2S(new Vector2(0.9f, 0), origin));

        for (int i = 0; i < a.windows.Count; i++)
        {
            var w = a.windows[i];
            bool active = w.IsActiveAtFrame(scrubFrame);
            bool sel = i == selectedWindow;
            Color c = active ? new Color(1f, 0.3f, 0.25f) : new Color(0.5f, 0.5f, 0.55f, 0.45f);
            if (sel) c = active ? new Color(1f, 0.8f, 0.2f) : new Color(0.9f, 0.8f, 0.4f, 0.55f);
            Handles.color = c;

            Vector2 centerS = W2S(w.offset, origin);
            if (w.shape == HitboxShape.Box) DrawRotatedBox(centerS, w.boxSize * PPU, w.angle, c);
            else Handles.DrawWireDisc(centerS, Vector3.forward, w.radius * PPU);

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
                    EditorUtility.SetDirty(a); e.Use(); _repaint?.Invoke();
                }
                if (e.type == EventType.MouseUp) draggingCenter = false;
            }
        }
        Handles.EndGUI();
    }

    void DrawRotatedBox(Vector2 center, Vector2 size, float angleDeg, Color c)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 ax = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad)) * (size.x * 0.5f);
        Vector2 ay = new Vector2(-Mathf.Sin(rad), -Mathf.Cos(rad)) * (size.y * 0.5f);
        Vector3[] pts = { center + ax + ay, center + ax - ay, center - ax - ay, center - ax + ay, center + ax + ay };
        Handles.color = c;
        Handles.DrawAAPolyLine(2f, pts);
    }

    Vector2 W2S(Vector2 w, Vector2 origin) => origin + new Vector2(w.x, -w.y) * PPU;
    Vector2 S2W(Vector2 s, Vector2 origin) => new Vector2((s.x - origin.x) / PPU, -(s.y - origin.y) / PPU);

    // ── 윈도우 리스트 ────────────────────────────────────────
    void DrawWindowList(AttackData a)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"히트 윈도우 ({a.windows.Count})", EditorStyles.boldLabel);
        if (GUILayout.Button("+ 추가", GUILayout.Width(60)))
        {
            a.windows.Add(new HitWindow { startFrame = scrubFrame, endFrame = Mathf.Min(a.totalFrames, scrubFrame + 1) });
            selectedWindow = a.windows.Count - 1;
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < a.windows.Count; i++)
        {
            var w = a.windows[i];
            bool sel = i == selectedWindow;
            EditorGUILayout.BeginVertical(sel ? EditorStyles.helpBox : GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(sel, $"#{i}  {w.label}  [F{w.startFrame}~{w.endFrame}]  {w.shape}", EditorStyles.foldoutHeader) != sel)
                selectedWindow = sel ? -1 : i;
            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                a.windows.RemoveAt(i);
                if (selectedWindow >= a.windows.Count) selectedWindow = a.windows.Count - 1;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            if (sel)
            {
                w.label = EditorGUILayout.TextField("Label", w.label);
                w.startFrame = EditorGUILayout.IntSlider("시작 프레임", w.startFrame, 0, a.totalFrames);
                w.endFrame   = EditorGUILayout.IntSlider("끝 프레임",   w.endFrame, w.startFrame, a.totalFrames);
                w.shape  = (HitboxShape)EditorGUILayout.EnumPopup("Shape", w.shape);
                w.offset = EditorGUILayout.Vector2Field("Offset (전방x, 좌y)", w.offset);
                if (w.shape == HitboxShape.Box)
                {
                    w.boxSize = EditorGUILayout.Vector2Field("Box Size", w.boxSize);
                    w.angle = EditorGUILayout.Slider("Angle", w.angle, -180f, 180f);
                }
                else w.radius = EditorGUILayout.FloatField("Radius", w.radius);
                w.damageMult = EditorGUILayout.FloatField("Damage ×", w.damageMult);
                w.groggyMult = EditorGUILayout.FloatField("Groggy ×", w.groggyMult);
            }
            EditorGUILayout.EndVertical();
        }
    }

    static T CreateAsset<T>(string defaultName) where T : ScriptableObject
    {
        string path = EditorUtility.SaveFilePanelInProject("새 " + typeof(T).Name, defaultName, "asset", "저장 위치");
        if (string.IsNullOrEmpty(path)) return null;
        var a = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(a, path);
        AssetDatabase.SaveAssets();
        return a;
    }
}
