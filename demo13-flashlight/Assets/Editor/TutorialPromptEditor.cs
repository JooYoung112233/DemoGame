using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TutorialPrompt))]
public class TutorialPromptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var ui = (TutorialPrompt)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI Generation", EditorStyles.boldLabel);
        if (ui.IsGenerated)
        {
            EditorGUILayout.HelpBox("UI structure exists.", MessageType.Info);
            if (GUILayout.Button("Clear Generated UI"))
            {
                var t = ui;
                EditorApplication.delayCall += () =>
                {
                    if (t == null) return;
                    Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Clear TutorialPrompt");
                    t.ClearGeneratedUI();
                    EditorUtility.SetDirty(t);
                    ForceInspectorRefresh(t.gameObject);
                };
                GUIUtility.ExitGUI();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No UI generated.", MessageType.Warning);
            if (GUILayout.Button("Generate UI Structure"))
            {
                var t = ui;
                EditorApplication.delayCall += () =>
                {
                    if (t == null) return;
                    Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Generate TutorialPrompt");
                    t.GenerateUI();
                    EditorUtility.SetDirty(t);
                    ForceInspectorRefresh(t.gameObject);
                };
                GUIUtility.ExitGUI();
            }
        }
    }

    static void ForceInspectorRefresh(GameObject go)
    {
        Selection.activeGameObject = null;
        EditorApplication.delayCall += () => Selection.activeGameObject = go;
    }
}
