using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MedicalHUD))]
public class MedicalHUDEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var ui = (MedicalHUD)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI Generation", EditorStyles.boldLabel);
        if (ui.IsGenerated)
        {
            EditorGUILayout.HelpBox("UI structure exists.", MessageType.Info);
            if (GUILayout.Button("Clear Generated UI"))
            {
                Undo.RegisterFullObjectHierarchyUndo(ui.gameObject, "Clear MedicalHUD");
                ui.ClearGeneratedUI();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No UI generated.", MessageType.Warning);
            if (GUILayout.Button("Generate UI Structure"))
            {
                Undo.RegisterFullObjectHierarchyUndo(ui.gameObject, "Generate MedicalHUD");
                ui.GenerateUI();
                EditorUtility.SetDirty(ui);
            }
        }
    }
}
