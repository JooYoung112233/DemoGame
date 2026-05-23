using UnityEngine;

/// <summary>
/// Scene Setup 에디터의 전체 설정을 저장하는 프리셋.
/// Assets/Settings/Presets/ 폴더에 저장.
/// </summary>
[CreateAssetMenu(fileName = "ScenePreset", menuName = "Demo/Scene Setup Preset")]
public class SceneSetupPreset : ScriptableObject
{
    [Header("Camera")]
    public float orthoSize = 7f;
    public float camAngle = 55f;
    public float camDistance = 20f;
    public float camYaw = 0f;
    public float camSmooth = 8f;

    [Header("Lighting")]
    public bool startAsNight = true;
    public float dayIntensity = 1f;
    public Color dayColor = new Color(1f, 0.95f, 0.9f);
    public float ambientBrightness = 0.04f;

    [Header("Player")]
    public int playerTypeIndex; // 0=Skeleton, 1=Sprite
    public float playerScale = 2f;
    public Color playerTint = new Color(0.7f, 0.8f, 1f, 1f);
    public Vector3 playerSpawn = new Vector3(6, 0, 4);

    [Header("Flashlight")]
    public bool addFlashlight = true;
    public float spotAngle = 90f;
    public float spotRange = 25f;
    public float spotIntensity = 15f;

    [Header("Post Process")]
    public bool addPostProcess = true;
    public bool addFog = true;

    [Header("Optimization")]
    public bool addViewCulling = true;
    public float cullingPadding = 5f;
}
