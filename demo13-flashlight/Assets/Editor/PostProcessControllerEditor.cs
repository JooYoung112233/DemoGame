using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PostProcessController))]
public class PostProcessControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PostProcessController controller = (PostProcessController)target;

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Play 없이 현재 Mood 설정을 Volume에 즉시 적용합니다.\n" +
            "씬 뷰에서 포스트프로세싱 미리보기 가능.",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Apply Day Mood", GUILayout.Height(30)))
        {
            controller.EditorApplyMood(false);
            EditorUtility.SetDirty(controller);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Apply Night Mood", GUILayout.Height(30)))
        {
            controller.EditorApplyMood(true);
            EditorUtility.SetDirty(controller);
            SceneView.RepaintAll();
        }

        EditorGUILayout.EndHorizontal();
    }
}
