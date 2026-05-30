using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameHUD))]
public class GameHUDEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var ui = (GameHUD)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI Generation", EditorStyles.boldLabel);
        if (ui.IsGenerated)
        {
            EditorGUILayout.HelpBox("UI structure exists.", MessageType.Info);
            if (GUILayout.Button("Clear Generated UI"))
            {
                var target = ui;
                EditorApplication.delayCall += () =>
                {
                    if (target == null) return;
                    Undo.RegisterFullObjectHierarchyUndo(target.gameObject, "Clear GameHUD");
                    target.ClearGeneratedUI();
                };
                GUIUtility.ExitGUI();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No UI generated.", MessageType.Warning);
            if (GUILayout.Button("Generate UI Structure"))
            {
                var target = ui;
                EditorApplication.delayCall += () =>
                {
                    if (target == null) return;
                    Undo.RegisterFullObjectHierarchyUndo(target.gameObject, "Generate GameHUD");
                    target.GenerateUI();
                    EditorUtility.SetDirty(target);
                };
                GUIUtility.ExitGUI();
            }
        }
    }
}
