using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScreenEffectManager))]
public class ScreenEffectManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var mgr = (ScreenEffectManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI Generation", EditorStyles.boldLabel);

        if (mgr.IsGenerated)
        {
            EditorGUILayout.HelpBox("UI structure exists.", MessageType.Info);
            if (GUILayout.Button("Clear Generated UI"))
            {
                Undo.RegisterFullObjectHierarchyUndo(mgr.gameObject, "Clear ScreenEffect UI");
                mgr.ClearGeneratedUI();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No UI generated. Click to create.", MessageType.Warning);
            if (GUILayout.Button("Generate UI Structure"))
            {
                Undo.RegisterFullObjectHierarchyUndo(mgr.gameObject, "Generate ScreenEffect UI");
                mgr.GenerateUI();
                EditorUtility.SetDirty(mgr);
            }
        }
    }
}
