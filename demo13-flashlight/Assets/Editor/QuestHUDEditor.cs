using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestHUD))]
public class QuestHUDEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var ui = (QuestHUD)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI Generation", EditorStyles.boldLabel);
        if (ui.IsGenerated)
        {
            EditorGUILayout.HelpBox("UI structure exists.", MessageType.Info);
            if (GUILayout.Button("Clear Generated UI"))
            {
                Undo.RegisterFullObjectHierarchyUndo(ui.gameObject, "Clear QuestHUD");
                ui.ClearGeneratedUI();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No UI generated.", MessageType.Warning);
            if (GUILayout.Button("Generate UI Structure"))
            {
                Undo.RegisterFullObjectHierarchyUndo(ui.gameObject, "Generate QuestHUD");
                ui.GenerateUI();
                EditorUtility.SetDirty(ui);
            }
        }
    }
}
