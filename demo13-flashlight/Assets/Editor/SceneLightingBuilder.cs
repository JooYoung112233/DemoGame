using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 탑다운 2D 씬 조명 셋업기. (Tools ▸ TopDown 2D ▸ Setup Scene Lighting)
/// 어두운 Global Light2D를 현재 씬에 추가/갱신 → 플레이어 PlayerLight와 대비되어
/// "주변만 밝고 나머지는 어둠" (Darkwood 느낌)이 됨.
/// 모든 Sorting Layer를 비추도록 설정해 스프라이트가 라이트를 받게 한다.
/// </summary>
public static class SceneLightingBuilder
{
    [MenuItem("Tools/TopDown 2D/Setup Scene Lighting")]
    public static void SetupSceneLighting()
    {
        // 기존 Global Light2D 탐색 (중복 생성 방지)
        var all = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        Light2D global = null;
        foreach (var l in all)
            if (l.lightType == Light2D.LightType.Global) { global = l; break; }

        if (global == null)
        {
            var go = new GameObject("Global Light 2D (Dark)");
            global = go.AddComponent<Light2D>();
            global.lightType = Light2D.LightType.Global;
            Undo.RegisterCreatedObjectUndo(go, "Create Global Light");
        }

        global.intensity = 0.22f;                       // 어둠 (이미지처럼)
        global.color = new Color(0.45f, 0.5f, 0.68f);   // 차가운 밤색
        ApplyAllSortingLayers(global);
        EditorUtility.SetDirty(global);

        // 씬에 이미 스폰된 플레이어 라이트도 모든 레이어 비추게 보정
        foreach (var l in all)
            if (l.lightType == Light2D.LightType.Point)
                ApplyAllSortingLayers(l);

        // DayNightCycle 컴포넌트 보장 + globalLight 연결 (인스펙터 낮/밤 버튼 바로 사용)
        var dn = Object.FindFirstObjectByType<DayNightCycle>();
        if (dn == null) dn = global.gameObject.AddComponent<DayNightCycle>();
        var so = new SerializedObject(dn);
        var glProp = so.FindProperty("globalLight");
        if (glProp != null) { glProp.objectReferenceValue = global; so.ApplyModifiedPropertiesWithoutUndo(); }
        EditorUtility.SetDirty(dn);

        Debug.Log("[SceneLighting] 어두운 Global Light2D + DayNightCycle 설정 (전 Sorting Layer 타겟). 인스펙터에서 낮/밤 전환 가능.");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Scene Lighting",
                "어두운 Global Light2D 설정 완료 (intensity 0.22).\n\n" +
                "• 플레이어 주변만 PlayerLight로 밝아집니다.\n" +
                "• 너무 어둡/밝으면 Global Light2D의 Intensity를 조정하세요.\n" +
                "• 모든 Sorting Layer를 비추도록 설정됨.", "확인");
    }

    /// <summary>Light2D가 모든 Sorting Layer를 비추도록 설정 (스프라이트가 라이트를 받게).</summary>
    public static void ApplyAllSortingLayers(Light2D light)
    {
        if (light == null) return;
        var so = new SerializedObject(light);
        var prop = so.FindProperty("m_ApplyToSortingLayers");
        if (prop == null) return;
        prop.ClearArray();
        var layers = SortingLayer.layers;
        for (int i = 0; i < layers.Length; i++)
        {
            prop.InsertArrayElementAtIndex(i);
            prop.GetArrayElementAtIndex(i).intValue = layers[i].id;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
