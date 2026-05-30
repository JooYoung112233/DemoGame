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
                var t = mgr;
                EditorApplication.delayCall += () =>
                {
                    if (t == null) return;
                    Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Clear ScreenEffect UI");
                    t.ClearGeneratedUI();
                    EditorUtility.SetDirty(t);
                    ForceInspectorRefresh(t.gameObject);
                };
                GUIUtility.ExitGUI();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No UI generated. Click to create.", MessageType.Warning);
            if (GUILayout.Button("Generate UI Structure"))
            {
                var t = mgr;
                EditorApplication.delayCall += () =>
                {
                    if (t == null) return;
                    Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Generate ScreenEffect UI");
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
