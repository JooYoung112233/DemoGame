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
                var target = mgr;
                EditorApplication.delayCall += () =>
                {
                    if (target == null) return;
                    Undo.RegisterFullObjectHierarchyUndo(target.gameObject, "Clear ScreenEffect UI");
                    target.ClearGeneratedUI();
                };
                GUIUtility.ExitGUI();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No UI generated. Click to create.", MessageType.Warning);
            if (GUILayout.Button("Generate UI Structure"))
            {
                var target = mgr;
                EditorApplication.delayCall += () =>
                {
                    if (target == null) return;
                    Undo.RegisterFullObjectHierarchyUndo(target.gameObject, "Generate ScreenEffect UI");
                    target.GenerateUI();
                    EditorUtility.SetDirty(target);
                };
                GUIUtility.ExitGUI();
            }
        }
    }
}
