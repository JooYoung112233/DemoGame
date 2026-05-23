using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP Post Processing을 낮/밤에 따라 자동 전환.
/// 씬에 Global Volume이 있어야 동작.
/// 밤: 비네트 강화, 색수차 강화, 그레인 추가, 차가운 컬러 그레이딩
/// 낮: 약한 비네트, 따뜻한 톤
/// </summary>
public class PostProcessController : MonoBehaviour
{
    [Header("Day/Night Link")]
    DayNightCycle dayNight;

    [Header("Transition")]
    [SerializeField] float transitionSpeed = 2f;

    [Header("Vignette")]
    [SerializeField] float dayVignette = 0.2f;
    [SerializeField] float nightVignette = 0.45f;

    [Header("Chromatic Aberration")]
    [SerializeField] float dayChromaticAberration = 0f;
    [SerializeField] float nightChromaticAberration = 0.15f;

    [Header("Film Grain")]
    [SerializeField] float dayGrain = 0f;
    [SerializeField] float nightGrain = 0.35f;

    [Header("Bloom")]
    [SerializeField] float dayBloomIntensity = 0.3f;
    [SerializeField] float nightBloomIntensity = 1.2f;
    [SerializeField] float dayBloomThreshold = 1.5f;
    [SerializeField] float nightBloomThreshold = 0.8f;

    [Header("Color Grading")]
    [SerializeField] float dayTemperature = 10f;     // 약간 따뜻
    [SerializeField] float nightTemperature = -20f;   // 차가운 블루톤
    [SerializeField] float dayContrast = 5f;
    [SerializeField] float nightContrast = 20f;
    [SerializeField] float daySaturation = 0f;
    [SerializeField] float nightSaturation = -25f;

    // Volume references
    Volume volume;
    Vignette vignette;
    ChromaticAberration chromaticAberration;
    FilmGrain filmGrain;
    Bloom bloom;
    ColorAdjustments colorAdjustments;

    float targetNight;
    float currentNight;

    void Start()
    {
        // Volume 찾기
        volume = FindFirstObjectByType<Volume>();
        if (volume == null)
        {
            // 없으면 자동 생성
            var go = new GameObject("PostProcessVolume");
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        // Override 컴포넌트 가져오기 (없으면 추가)
        EnsureOverride(out vignette);
        EnsureOverride(out chromaticAberration);
        EnsureOverride(out filmGrain);
        EnsureOverride(out bloom);
        EnsureOverride(out colorAdjustments);

        // DayNightCycle 구독
        dayNight = FindFirstObjectByType<DayNightCycle>();
        if (dayNight != null)
        {
            dayNight.OnPhaseChanged += OnPhaseChanged;
            targetNight = dayNight.IsNight ? 1f : 0f;
            currentNight = targetNight;
        }
        else
        {
            targetNight = 1f;
            currentNight = 1f;
        }

        ApplyValues(currentNight);
    }

    void OnDestroy()
    {
        if (dayNight != null)
            dayNight.OnPhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(bool isNight)
    {
        targetNight = isNight ? 1f : 0f;
    }

    void Update()
    {
        if (Mathf.Approximately(currentNight, targetNight)) return;

        currentNight = Mathf.MoveTowards(currentNight, targetNight,
            transitionSpeed * Time.deltaTime);
        ApplyValues(currentNight);
    }

    void ApplyValues(float t)
    {
        // Vignette
        if (vignette != null)
        {
            vignette.intensity.overrideState = true;
            vignette.intensity.value = Mathf.Lerp(dayVignette, nightVignette, t);
        }

        // Chromatic Aberration
        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.overrideState = true;
            chromaticAberration.intensity.value = Mathf.Lerp(dayChromaticAberration, nightChromaticAberration, t);
        }

        // Film Grain
        if (filmGrain != null)
        {
            filmGrain.type.overrideState = true;
            filmGrain.type.value = FilmGrainLookup.Medium3;
            filmGrain.intensity.overrideState = true;
            filmGrain.intensity.value = Mathf.Lerp(dayGrain, nightGrain, t);
            filmGrain.response.overrideState = true;
            filmGrain.response.value = 0.8f;
        }

        // Bloom
        if (bloom != null)
        {
            bloom.intensity.overrideState = true;
            bloom.intensity.value = Mathf.Lerp(dayBloomIntensity, nightBloomIntensity, t);
            bloom.threshold.overrideState = true;
            bloom.threshold.value = Mathf.Lerp(dayBloomThreshold, nightBloomThreshold, t);
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.7f;
        }

        // Color Adjustments (Color Grading)
        if (colorAdjustments != null)
        {
            colorAdjustments.colorFilter.overrideState = true;
            colorAdjustments.colorFilter.value = Color.white;
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.postExposure.value = 0f;

            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.contrast.value = Mathf.Lerp(dayContrast, nightContrast, t);

            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = Mathf.Lerp(daySaturation, nightSaturation, t);
        }
    }

    void EnsureOverride<T>(out T component) where T : VolumeComponent
    {
        if (!volume.profile.TryGet(out component))
        {
            component = volume.profile.Add<T>(true);
        }
    }
}
