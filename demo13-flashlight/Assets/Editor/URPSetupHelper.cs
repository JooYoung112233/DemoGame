using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class URPSetupHelper : EditorWindow
{
    [MenuItem("Tools/Setup URP 3D Renderer")]
    static void SetupURP()
    {
        EnsureSettingsFolder();

        // 3D Forward Renderer (2D Renderer가 아님!)
        var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
        AssetDatabase.CreateAsset(rendererData, "Assets/Settings/ForwardRenderer.asset");

        var urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
        // 그림자 설정
        var so = new SerializedObject(urpAsset);
        var mainLightShadow = so.FindProperty("m_MainLightRenderingMode");
        if (mainLightShadow != null) mainLightShadow.intValue = 1; // PerPixel
        var mainLightShadowEnabled = so.FindProperty("m_MainLightShadowsSupported");
        if (mainLightShadowEnabled != null) mainLightShadowEnabled.boolValue = true;
        var additionalLightShadow = so.FindProperty("m_AdditionalLightShadowsSupported");
        if (additionalLightShadow != null) additionalLightShadow.boolValue = true;
        var additionalLightMode = so.FindProperty("m_AdditionalLightsRenderingMode");
        if (additionalLightMode != null) additionalLightMode.intValue = 1; // PerPixel
        var shadowDist = so.FindProperty("m_MainLightShadowmapResolution");
        if (shadowDist != null) shadowDist.intValue = 2048;
        var shadowDistance = so.FindProperty("m_ShadowDistance");
        if (shadowDistance != null) shadowDistance.floatValue = 30f;
        so.ApplyModifiedProperties();

        AssetDatabase.CreateAsset(urpAsset, "Assets/Settings/URP-3D.asset");

        GraphicsSettings.defaultRenderPipeline = urpAsset;
        QualitySettings.renderPipeline = urpAsset;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[URP Setup] 3D Forward Renderer + Shadow 설정 완료!");
        EditorUtility.DisplayDialog("URP 3D Setup Complete",
            "URP 3D Forward Renderer 설정 완료!\nShadow: Main + Additional Light 모두 활성화\n\nNext: Tools > Setup Flashlight Prototype Scene",
            "OK");
    }

    [InitializeOnLoadMethod]
    static void EnsureSettingsFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");
    }
}
