using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class URPSetupHelper : EditorWindow
{
    [MenuItem("Tools/Setup URP 2D Renderer")]
    static void SetupURP()
    {
        // Create 2D Renderer Data
        var renderer2D = ScriptableObject.CreateInstance<Renderer2DData>();
        AssetDatabase.CreateAsset(renderer2D, "Assets/Settings/Renderer2D.asset");

        // Create URP Asset with 2D Renderer
        var urpAsset = UniversalRenderPipelineAsset.Create(renderer2D);
        AssetDatabase.CreateAsset(urpAsset, "Assets/Settings/URP-2D.asset");

        // Assign to Graphics Settings
        GraphicsSettings.defaultRenderPipeline = urpAsset;
        QualitySettings.renderPipeline = urpAsset;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[URP Setup] 2D Renderer configured! Graphics pipeline set to URP 2D.");
        EditorUtility.DisplayDialog("URP 2D Setup Complete",
            "URP 2D Renderer has been configured.\n\nNext: Tools > Setup Flashlight Prototype Scene",
            "OK");
    }

    [InitializeOnLoadMethod]
    static void EnsureSettingsFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");
    }
}
