using UnityEngine;
using UnityEditor;

/// <summary>
/// InteractableObject 커스텀 인스펙터.
/// - 상호작용 범위를 씬 뷰에서 핸들로 조절 가능
/// - 범위 원 색상: 타입별 구분
/// - 일괄 범위 조절 버튼
/// </summary>
[CustomEditor(typeof(InteractableObject))]
[CanEditMultipleObjects]
public class InteractableObjectEditor : Editor
{
    SerializedProperty typeProp;
    SerializedProperty interactRangeProp;
    SerializedProperty promptTextProp;

    void OnEnable()
    {
        typeProp = serializedObject.FindProperty("type");
        interactRangeProp = serializedObject.FindProperty("interactRange");
        promptTextProp = serializedObject.FindProperty("promptText");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        serializedObject.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Range Tools", EditorStyles.boldLabel);

        // 현재 범위 표시 + 슬라이더
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("범위 조절");
        float newRange = EditorGUILayout.Slider(interactRangeProp.floatValue, 0.5f, 8f);
        if (!Mathf.Approximately(newRange, interactRangeProp.floatValue))
        {
            interactRangeProp.floatValue = newRange;
        }
        EditorGUILayout.EndHorizontal();

        // 프리셋 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("1.5m (좁음)")) interactRangeProp.floatValue = 1.5f;
        if (GUILayout.Button("2.5m (기본)")) interactRangeProp.floatValue = 2.5f;
        if (GUILayout.Button("4m (넓음)")) interactRangeProp.floatValue = 4f;
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();

        // 다중 선택 시 일괄 적용
        if (targets.Length > 1)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox($"{targets.Length}개 선택됨 — 슬라이더/버튼으로 일괄 적용", MessageType.Info);
        }
    }

    void OnSceneGUI()
    {
        var obj = (InteractableObject)target;
        if (obj == null) return;

        // 범위 색상: 타입별 구분
        Color rangeColor = GetTypeColor(obj.Type);
        Handles.color = rangeColor;

        // XZ 평면 원 (Y축 높이에 맞게)
        Vector3 center = obj.transform.position;
        Handles.DrawWireDisc(center, Vector3.up, obj.InteractRange);

        // 반투명 디스크
        Color fillColor = rangeColor;
        fillColor.a = 0.08f;
        Handles.color = fillColor;
        Handles.DrawSolidDisc(center, Vector3.up, obj.InteractRange);

        // 범위 핸들 (드래그로 조절)
        Handles.color = rangeColor;
        EditorGUI.BeginChangeCheck();

        float handleSize = HandleUtility.GetHandleSize(center) * 0.06f;
        Vector3 handlePos = center + Vector3.right * obj.InteractRange;
        Vector3 newHandlePos = Handles.FreeMoveHandle(
            handlePos, handleSize, Vector3.zero, Handles.DotHandleCap);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(obj, "Change Interact Range");
            Vector3 diff = newHandlePos - center;
            diff.y = 0;
            float newRange = Mathf.Max(0.3f, diff.magnitude);

            var so = new SerializedObject(obj);
            var rangeProp = so.FindProperty("interactRange");
            rangeProp.floatValue = newRange;
            so.ApplyModifiedProperties();
        }

        // 라벨
        Handles.color = Color.white;
        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
        labelStyle.normal.textColor = rangeColor;
        labelStyle.fontSize = 11;
        Handles.Label(center + Vector3.up * 0.8f + Vector3.right * 0.2f,
            $"[{obj.Type}] {obj.InteractRange:F1}m", labelStyle);
    }

    static Color GetTypeColor(InteractableObject.InteractType type)
    {
        switch (type)
        {
            case InteractableObject.InteractType.ExitPoint:  return new Color(0.2f, 1f, 0.4f, 0.8f);   // 초록
            case InteractableObject.InteractType.Container:  return new Color(1f, 0.8f, 0.2f, 0.8f);    // 노랑
            case InteractableObject.InteractType.NPC:        return new Color(0.3f, 0.7f, 1f, 0.8f);    // 파랑
            case InteractableObject.InteractType.Note:       return new Color(0.9f, 0.9f, 0.9f, 0.8f);  // 흰색
            case InteractableObject.InteractType.Pickup:     return new Color(1f, 0.5f, 0f, 0.8f);      // 주황
            case InteractableObject.InteractType.MapBoard:   return new Color(0.8f, 0.4f, 1f, 0.8f);    // 보라
            default:                                         return new Color(0f, 1f, 1f, 0.8f);        // 시안
        }
    }
}
