using UnityEngine;
using UnityEditor;

/// <summary>
/// DayNightCycle 인스펙터에 낮/밤 전환 버튼 추가.
/// 에디터(씬 편집)·플레이(인게임) 둘 다 즉시 적용 — 글로벌 Light2D가 바로 바뀌어 미리보기 가능.
/// 낮/밤 수치(intensity/color)는 기본 인스펙터에서 조절.
/// </summary>
[CustomEditor(typeof(DayNightCycle))]
public class DayNightCycleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var dn = (DayNightCycle)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("낮/밤 전환 (에디터·런타임 공용)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("현재 상태", dn.IsNight ? "🌙 밤" : "☀ 낮");

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = dn.IsNight ? Color.white : new Color(1f, 0.9f, 0.5f);
        if (GUILayout.Button("☀ 낮으로", GUILayout.Height(30))) Apply(dn, false);
        GUI.backgroundColor = dn.IsNight ? new Color(0.5f, 0.6f, 1f) : Color.white;
        if (GUILayout.Button("🌙 밤으로", GUILayout.Height(30))) Apply(dn, true);
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("⟳ 토글")) Apply(dn, !dn.IsNight);

        EditorGUILayout.HelpBox(
            "버튼은 씬(에디터)·플레이 둘 다 즉시 적용 → 글로벌 Light2D가 바로 바뀝니다.\n" +
            "수치는 위 인스펙터(Day/Night Global Intensity·Color)에서 조절 후 버튼으로 확인.\n" +
            "런타임에선 T키로도 전환됩니다.", MessageType.Info);
    }

    void Apply(DayNightCycle dn, bool night)
    {
        dn.SetNight(night);
        if (!Application.isPlaying)
        {
            // 글로벌 라이트(다른 오브젝트)까지 변경되므로 씬 전체 dirty + 뷰 갱신
            EditorUtility.SetDirty(dn);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(dn.gameObject.scene);
            SceneView.RepaintAll();
        }
        Repaint();
    }
}
