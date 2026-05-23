using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP Post Processing 프리셋 시스템.
/// 프리셋(Mood)을 선택하면 낮/밤 값이 자동 설정됨.
/// DayNightCycle 연동으로 부드럽게 전환.
/// 전환 시 "이상현상 펄스 + 글리치" 효과 재생.
/// </summary>
public class PostProcessController : MonoBehaviour
{
    public enum Mood
    {
        NightCity,      // 네온 불빛 도시 밤거리
        DesolateRuin,   // 폐허, 손전등 하나만 의지
        Anomaly,        // 이상현상, 뒤틀린 세계
    }

    [Header("Preset")]
    [SerializeField] Mood dayMood = Mood.NightCity;
    [SerializeField] Mood nightMood = Mood.DesolateRuin;

    [Header("Transition")]
    [SerializeField] float transitionSpeed = 2f;
    [SerializeField] float maxTransitionDuration = 3f;  // 전환 최대 소요 시간(초)

    [Header("Anomaly Pulse (전환 시 이상현상)")]
    [SerializeField] float pulseDuration = 2.0f;
    [SerializeField] float pulseChromatic = 1.0f;
    [SerializeField] float pulseVignette = 0.85f;
    [SerializeField] float pulseBloom = 6f;
    [SerializeField] float pulseGrain = 0.5f;
    [SerializeField] float pulseSaturation = -90f;

    [Header("Glitch (지지직)")]
    [SerializeField] float glitchFrequency = 12f;       // 초당 글리치 횟수
    [SerializeField] float glitchChromaShake = 0.8f;     // 색수차 랜덤 흔들림
    [SerializeField] float glitchExposureKick = 1.5f;    // 노출 깜빡임 강도
    [SerializeField] float glitchDistortionRange = 0.6f; // 렌즈 왜곡 범위

    // ── 런타임 캐시 ──
    DayNightCycle dayNight;
    Volume volume;
    Vignette vignette;
    ChromaticAberration chromaticAberration;
    FilmGrain filmGrain;
    Bloom bloom;
    ColorAdjustments colorAdjustments;
    LensDistortion lensDistortion;

    float targetNight;
    float currentNight;
    MoodData data;

    // ── 펄스 ──
    float pulseTimer;
    bool isPulsing;
    float glitchSeed;

    // ════════════════════════════════════════
    //  Mood Data
    // ════════════════════════════════════════

    struct MoodData
    {
        // Vignette
        public float dayVignette, nightVignette;
        // Chromatic Aberration
        public float dayChromaticAberration, nightChromaticAberration;
        // Film Grain
        public float dayGrain, nightGrain;
        public float grainResponse;
        // Bloom
        public float dayBloomIntensity, nightBloomIntensity;
        public float dayBloomThreshold, nightBloomThreshold;
        public float bloomScatter;
        // Color Adjustments
        public float dayContrast, nightContrast;
        public float daySaturation, nightSaturation;
        public float dayExposure, nightExposure;
    }

    static MoodData GetMoodData(Mood mood)
    {
        switch (mood)
        {
            // ── 1. Night City: 네온 도시 밤거리 ──
            case Mood.NightCity:
                return new MoodData
                {
                    dayVignette = 0.28f,        nightVignette = 0.4f,
                    dayChromaticAberration = 0.04f, nightChromaticAberration = 0.08f,
                    dayGrain = 0.04f,           nightGrain = 0.12f,
                    grainResponse = 0.5f,
                    dayBloomIntensity = 0.6f,   nightBloomIntensity = 1.2f,
                    dayBloomThreshold = 1.2f,   nightBloomThreshold = 0.8f,
                    bloomScatter = 0.7f,
                    dayContrast = 12f,          nightContrast = 20f,
                    daySaturation = -10f,       nightSaturation = -20f,
                    dayExposure = 0.1f,         nightExposure = 0f,
                };

            // ── 2. Desolate Ruin: 폐허, 극한의 어둠 ──
            case Mood.DesolateRuin:
                return new MoodData
                {
                    dayVignette = 0.25f,        nightVignette = 0.55f,
                    dayChromaticAberration = 0.03f, nightChromaticAberration = 0.1f,
                    dayGrain = 0.03f,           nightGrain = 0.15f,
                    grainResponse = 0.5f,
                    dayBloomIntensity = 0.2f,   nightBloomIntensity = 2.0f,
                    dayBloomThreshold = 1.8f,   nightBloomThreshold = 0.5f,
                    bloomScatter = 0.8f,
                    dayContrast = 10f,          nightContrast = 35f,
                    daySaturation = -15f,       nightSaturation = -50f,
                    dayExposure = 0f,           nightExposure = -0.5f,
                };

            // ── 3. Anomaly: 이상현상, 뒤틀린 세계 ──
            case Mood.Anomaly:
                return new MoodData
                {
                    dayVignette = 0.35f,        nightVignette = 0.6f,
                    dayChromaticAberration = 0.2f, nightChromaticAberration = 0.4f,
                    dayGrain = 0.08f,           nightGrain = 0.2f,
                    grainResponse = 0.7f,
                    dayBloomIntensity = 0.8f,   nightBloomIntensity = 3.0f,
                    dayBloomThreshold = 1.0f,   nightBloomThreshold = 0.3f,
                    bloomScatter = 0.9f,
                    dayContrast = 15f,          nightContrast = 40f,
                    daySaturation = -30f,       nightSaturation = -60f,
                    dayExposure = 0.2f,         nightExposure = -0.3f,
                };

            default:
                return GetMoodData(Mood.NightCity);
        }
    }

    // ════════════════════════════════════════
    //  Lifecycle
    // ════════════════════════════════════════

    void Start()
    {
        // Volume 캐싱만 — 에디터에서 설정한 값 덮어쓰지 않음
        volume = GetComponent<Volume>();
        if (volume == null)
            volume = FindFirstObjectByType<Volume>();
        if (volume == null)
        {
            Debug.LogWarning("[PostProcess] Volume not found. Add a Volume component.");
            return;
        }

        // 컴포넌트 참조 캐싱 — 없으면 생성 (값은 덮어쓰지 않음)
        EnsureOverride(out vignette);
        EnsureOverride(out chromaticAberration);
        EnsureOverride(out filmGrain);
        EnsureOverride(out bloom);
        EnsureOverride(out colorAdjustments);
        EnsureOverride(out lensDistortion);

        // DayNightCycle 구독 (T키 전환 시에만 효과 적용)
        dayNight = FindFirstObjectByType<DayNightCycle>();
        if (dayNight != null)
        {
            dayNight.OnPhaseChanged += OnPhaseChanged;
            bool isNight = dayNight.IsNight;
            targetNight = isNight ? 1f : 0f;
            currentNight = targetNight;
            data = GetMoodData(isNight ? nightMood : dayMood);
        }
        else
        {
            targetNight = 1f;
            currentNight = 1f;
            data = GetMoodData(nightMood);
        }
        // ApplyValues 호출 안 함 → 에디터 설정 그대로 유지
    }

    void OnDestroy()
    {
        if (dayNight != null)
            dayNight.OnPhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(bool isNight)
    {
        targetNight = isNight ? 1f : 0f;
        data = GetMoodData(isNight ? nightMood : dayMood);

        // 이상현상 펄스 시작
        isPulsing = true;
        pulseTimer = pulseDuration;
    }

    void Update()
    {
        bool moved = false;

        if (!Mathf.Approximately(currentNight, targetNight))
        {
            // maxTransitionDuration 이내에 완료되도록 최소 속도 보장
            float minSpeed = (maxTransitionDuration > 0f) ? 1f / maxTransitionDuration : transitionSpeed;
            float speed = Mathf.Max(transitionSpeed, minSpeed);
            currentNight = Mathf.MoveTowards(currentNight, targetNight,
                speed * Time.deltaTime);
            moved = true;
        }

        // 이상현상 펄스 처리
        if (isPulsing)
        {
            pulseTimer -= Time.deltaTime;
            if (pulseTimer <= 0f)
            {
                isPulsing = false;
                pulseTimer = 0f;
            }
            moved = true;
        }

        if (moved)
            ApplyValues(currentNight);
    }

    /// <summary>
    /// 런타임에 Mood 변경 (UI나 이벤트에서 호출 가능)
    /// </summary>
    public void SetMood(Mood newMood)
    {
        dayMood = newMood;
        nightMood = newMood;
        data = GetMoodData(newMood);
        ApplyValues(currentNight);
    }

    // ════════════════════════════════════════
    //  Apply
    // ════════════════════════════════════════

    void ApplyValues(float t)
    {
        // ── 펄스 강도 (smooth envelope) ──
        float p = 0f;
        float glitch = 0f;   // 글리치 랜덤 노이즈 0~1
        if (isPulsing && pulseDuration > 0f)
        {
            float ratio = pulseTimer / pulseDuration;           // 1→0
            p = ratio * ratio;                                   // EaseIn curve

            // 글리치: 펄스 중 랜덤 지지직 (고주파 노이즈)
            glitchSeed += Time.deltaTime * glitchFrequency;
            float noise = Mathf.PerlinNoise(glitchSeed * 6.7f, glitchSeed * 3.1f);
            // 간헐적으로 강한 글리치 (30% 확률로 풀 킥)
            glitch = noise > 0.55f ? Mathf.InverseLerp(0.55f, 1f, noise) : 0f;
            glitch *= p;  // 펄스 감쇠에 따라 글리치도 약해짐
        }

        if (vignette != null)
        {
            vignette.intensity.overrideState = true;
            float baseVal = Mathf.Lerp(data.dayVignette, data.nightVignette, t);
            // 글리치 시 비네트 급격히 조임
            float glitchVignette = Mathf.Lerp(0f, 0.15f, glitch);
            vignette.intensity.value = Mathf.Lerp(baseVal, pulseVignette, p) + glitchVignette;
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.overrideState = true;
            float baseVal = Mathf.Lerp(data.dayChromaticAberration, data.nightChromaticAberration, t);
            // 글리치: 색수차 랜덤 스파이크 (지지직!)
            float glitchCA = glitch * glitchChromaShake;
            chromaticAberration.intensity.value = Mathf.Lerp(baseVal, pulseChromatic, p) + glitchCA;
        }

        if (filmGrain != null)
        {
            filmGrain.type.overrideState = true;
            // 글리치 중 그레인 타입 랜덤 변경 (다른 텍스처 = 지지직)
            filmGrain.type.value = glitch > 0.5f ? FilmGrainLookup.Large01 : FilmGrainLookup.Medium3;
            filmGrain.intensity.overrideState = true;
            float baseVal = Mathf.Lerp(data.dayGrain, data.nightGrain, t);
            // 글리치 시 그레인 대폭 증가
            float glitchGrain = glitch * 0.4f;
            filmGrain.intensity.value = Mathf.Lerp(baseVal, pulseGrain, p) + glitchGrain;
            filmGrain.response.overrideState = true;
            filmGrain.response.value = Mathf.Lerp(data.grainResponse, 1f, glitch);
        }

        if (bloom != null)
        {
            bloom.intensity.overrideState = true;
            float baseVal = Mathf.Lerp(data.dayBloomIntensity, data.nightBloomIntensity, t);
            // 글리치 시 블룸 번쩍임
            float glitchBloom = glitch * 3f;
            bloom.intensity.value = Mathf.Lerp(baseVal, pulseBloom, p) + glitchBloom;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = Mathf.Lerp(data.dayBloomThreshold, data.nightBloomThreshold, t);
            bloom.scatter.overrideState = true;
            bloom.scatter.value = data.bloomScatter;
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.overrideState = true;
            float baseExposure = Mathf.Lerp(data.dayExposure, data.nightExposure, t);
            // 글리치: 노출 깜빡 (밝았다 어두웠다)
            float glitchExp = (glitch > 0.3f)
                ? Mathf.Sin(glitchSeed * 31.4f) * glitchExposureKick * glitch
                : 0f;
            colorAdjustments.postExposure.value = baseExposure + glitchExp;

            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.contrast.value = Mathf.Lerp(data.dayContrast, data.nightContrast, t);
            colorAdjustments.saturation.overrideState = true;
            float baseSat = Mathf.Lerp(data.daySaturation, data.nightSaturation, t);
            colorAdjustments.saturation.value = Mathf.Lerp(baseSat, pulseSaturation, p);

            colorAdjustments.colorFilter.overrideState = true;
            // 펄스: 보라 틴트, 글리치: 랜덤 색 깜빡임
            Color pulseColor = new Color(0.85f, 0.7f, 0.9f);
            Color glitchColor = glitch > 0.6f
                ? new Color(
                    0.6f + Mathf.Sin(glitchSeed * 17f) * 0.4f,
                    0.4f + Mathf.Sin(glitchSeed * 23f) * 0.3f,
                    0.7f + Mathf.Sin(glitchSeed * 11f) * 0.3f)
                : Color.white;
            Color tint = Color.Lerp(Color.white, pulseColor, p * 0.5f);
            tint = Color.Lerp(tint, glitchColor, glitch * 0.6f);
            colorAdjustments.colorFilter.value = tint;
        }

        if (lensDistortion != null)
        {
            lensDistortion.intensity.overrideState = true;
            // 기본 펄스 왜곡 + 글리치 랜덤 방향 왜곡
            float baseDist = Mathf.Lerp(0f, -0.5f, p);
            float glitchDist = glitch * glitchDistortionRange
                               * Mathf.Sin(glitchSeed * 19f);
            lensDistortion.intensity.value = baseDist + glitchDist;
        }
    }

    void EnsureOverride<T>(out T component) where T : VolumeComponent
    {
        if (!volume.profile.TryGet(out component))
        {
            component = volume.profile.Add<T>(true);
        }
    }

    // ════════════════════════════════════════
    //  Editor Preview (에디터에서 적용 버튼용)
    // ════════════════════════════════════════

    /// <summary>
    /// 에디터에서 호출: 현재 Mood를 Volume에 즉시 적용 (Play 안 해도 됨)
    /// </summary>
    public void EditorApplyMood(bool asNight)
    {
        volume = GetComponent<Volume>();
        if (volume == null)
            volume = FindFirstObjectByType<Volume>();
        if (volume == null)
        {
            Debug.LogWarning("[PostProcess] Volume not found!");
            return;
        }
        if (volume.profile == null)
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

        EnsureOverride(out vignette);
        EnsureOverride(out chromaticAberration);
        EnsureOverride(out filmGrain);
        EnsureOverride(out bloom);
        EnsureOverride(out colorAdjustments);
        EnsureOverride(out lensDistortion);

        isPulsing = false;
        currentNight = asNight ? 1f : 0f;
        data = GetMoodData(asNight ? nightMood : dayMood);
        ApplyValues(currentNight);
        Debug.Log($"[PostProcess] Editor applied: {(asNight ? nightMood : dayMood)} ({(asNight ? "Night" : "Day")})");
    }
}
