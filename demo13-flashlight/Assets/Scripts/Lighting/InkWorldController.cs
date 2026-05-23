using UnityEngine;

public class InkWorldController : MonoBehaviour
{
    [Header("Day/Night Link")]
    [SerializeField] DayNightCycle dayNightCycle;

    [Header("Night Overlay")]
    [SerializeField] Material nightOverlayMaterial;
    [SerializeField] float dayDarkness = 0.05f;
    [SerializeField] float nightDarkness = 0.55f;
    [SerializeField] float dayVignette = 0.1f;
    [SerializeField] float nightVignette = 0.6f;
    [SerializeField] float dayNoise = 0.05f;
    [SerializeField] float nightNoise = 0.25f;

    [Header("Ink Shadows")]
    [SerializeField] Material inkShadowMaterial;
    [SerializeField] float dayShadowOpacity = 0.25f;
    [SerializeField] float nightShadowOpacity = 0.65f;

    [Header("Transition")]
    [SerializeField] float transitionSpeed = 2f;

    float targetNightValue;
    float currentNightValue;

    static readonly int DarknessID = Shader.PropertyToID("_Darkness");
    static readonly int VignettePowerID = Shader.PropertyToID("_VignettePower");
    static readonly int NoiseStrengthID = Shader.PropertyToID("_NoiseStrength");
    static readonly int OpacityID = Shader.PropertyToID("_Opacity");

    void OnEnable()
    {
        if (dayNightCycle != null)
            dayNightCycle.OnPhaseChanged += OnPhaseChanged;
    }

    void OnDisable()
    {
        if (dayNightCycle != null)
            dayNightCycle.OnPhaseChanged -= OnPhaseChanged;
    }

    void Start()
    {
        // 에디터 설정 유지 — 머티리얼 값 덮어쓰지 않음
        // Update에서 transitionSpeed로 자연스럽게 전환
        if (dayNightCycle != null)
            targetNightValue = dayNightCycle.IsNight ? 1f : 0f;
    }

    void Update()
    {
        currentNightValue = Mathf.MoveTowards(currentNightValue, targetNightValue, Time.deltaTime * transitionSpeed);
        ApplyValues(currentNightValue);
    }

    void OnPhaseChanged(bool isNight)
    {
        targetNightValue = isNight ? 1f : 0f;
    }

    void ApplyValues(float t)
    {
        if (nightOverlayMaterial != null)
        {
            nightOverlayMaterial.SetFloat(DarknessID, Mathf.Lerp(dayDarkness, nightDarkness, t));
            nightOverlayMaterial.SetFloat(VignettePowerID, Mathf.Lerp(dayVignette, nightVignette, t));
            nightOverlayMaterial.SetFloat(NoiseStrengthID, Mathf.Lerp(dayNoise, nightNoise, t));
        }

        if (inkShadowMaterial != null)
        {
            inkShadowMaterial.SetFloat(OpacityID, Mathf.Lerp(dayShadowOpacity, nightShadowOpacity, t));
        }
    }
}
