using UnityEditor;
using UnityEngine;

/// <summary>
/// FlatShadow 커스텀 인스펙터 — 기본값 리셋/저장 버튼.
/// 기본값은 EditorPrefs에 저장되어 새 FlatShadow 추가(Reset) 시 자동 적용된다.
/// </summary>
[CustomEditor(typeof(FlatShadow))]
[CanEditMultipleObjects]
public class FlatShadowEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("기본값(Default)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("기본값으로 리셋", GUILayout.Height(24)))
        {
            foreach (FlatShadow t in targets)
            {
                Undo.RecordObject(t, "Reset FlatShadow");
                t.LoadDefaults();          // 저장된 기본값(없으면 하드코드)
                EditorUtility.SetDirty(t);
            }
        }
        if (GUILayout.Button("현재값을 기본값으로 저장", GUILayout.Height(24)))
        {
            ((FlatShadow)target).SaveAsDefault();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("코드 기본값으로", GUILayout.Height(20)))
        {
            foreach (FlatShadow t in targets)
            {
                Undo.RecordObject(t, "Hard Reset FlatShadow");
                t.ResetHardDefaults();
                t.LoadDefaults(); // 즉시 그림자 재생성
                EditorUtility.SetDirty(t);
            }
        }
        if (GUILayout.Button("저장된 기본값 삭제", GUILayout.Height(20)))
            FlatShadow.ClearDefault();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "‘현재값을 기본값으로 저장’ → 이후 새 FlatShadow를 붙이면(Reset) 그 값이 자동 적용됩니다.\n" +
            "‘기본값으로 리셋’ → 저장된 기본값(없으면 코드 기본값)으로 되돌림 + 그림자 재생성.",
            MessageType.None);
    }
}
