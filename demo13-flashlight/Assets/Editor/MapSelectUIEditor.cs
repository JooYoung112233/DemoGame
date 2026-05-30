using UnityEngine;
using UnityEditor;

/// <summary>
/// MapSelectUI 커스텀 인스펙터.
/// Generate UI / Clear UI 버튼으로 에디터 타임에 Canvas 생성/삭제.
/// </summary>
[CustomEditor(typeof(MapSelectUI))]
public class MapSelectUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var ui = (MapSelectUI)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("UI 생성 도구", EditorStyles.boldLabel);

        bool generated = ui.IsGenerated;

        EditorGUILayout.HelpBox(
            generated ? "UI가 생성되어 있습니다." : "UI가 아직 생성되지 않았습니다.",
            generated ? MessageType.Info : MessageType.Warning);

        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
        using (new EditorGUI.DisabledScope(generated))
        {
            if (GUILayout.Button("Generate UI", GUILayout.Height(30)))
            {
                var t = ui;
                EditorApplication.delayCall += () =>
                {
                    if (t == null) return;
                    Undo.RegisterCompleteObjectUndo(t, "Generate MapSelectUI");
                    t.GenerateUI();
                    EditorUtility.SetDirty(t);
                    ForceInspectorRefresh(t.gameObject);
                    Debug.Log("<color=green>[MapSelectUI]</color> UI 생성 완료");
                };
                GUIUtility.ExitGUI();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(3);

        GUI.backgroundColor = new Color(1f, 0.4f, 0.3f);
        using (new EditorGUI.DisabledScope(!generated))
        {
            if (GUILayout.Button("Clear UI", GUILayout.Height(28)))
            {
                var t = ui;
                EditorApplication.delayCall += () =>
                {
                    if (t == null) return;
                    Undo.RegisterCompleteObjectUndo(t, "Clear MapSelectUI");
                    t.ClearGeneratedUI();
                    EditorUtility.SetDirty(t);
                    ForceInspectorRefresh(t.gameObject);
                    Debug.Log("<color=orange>[MapSelectUI]</color> UI 제거 완료");
                };
                GUIUtility.ExitGUI();
            }
        }
        GUI.backgroundColor = Color.white;
    }

    static void ForceInspectorRefresh(GameObject go)
    {
        Selection.activeGameObject = null;
        EditorApplication.delayCall += () => Selection.activeGameObject = go;
    }
}
