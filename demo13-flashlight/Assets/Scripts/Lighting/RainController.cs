using UnityEngine;

/// <summary>
/// 비 날씨 컨트롤러.
/// WeatherData 기반으로 비 파티클 + 바닥 Wetness 셰이더 제어.
/// R키로 비 토글 (디버그).
/// </summary>
public class RainController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] WeatherData weatherData;

    [Header("Rain Particle")]
    [SerializeField] ParticleSystem rainParticle;

    [Header("Wetness")]
    [SerializeField] float wetnessTransitionSpeed = 0.3f;

    float currentWetness;
    float targetWetness;

    // 글로벌 셰이더 프로퍼티
    static readonly int WetnessID = Shader.PropertyToID("_Wetness");
    static readonly int RippleSpeedID = Shader.PropertyToID("_RippleSpeed");
    static readonly int RippleDensityID = Shader.PropertyToID("_RippleDensity");

    bool isRaining;

    public bool IsRaining => isRaining;
    public float Wetness => currentWetness;

    void Start()
    {
        if (weatherData != null)
        {
            isRaining = weatherData.IsRaining;
            targetWetness = weatherData.EffectiveWetness;
            currentWetness = targetWetness; // 즉시 적용
        }

        UpdateParticle();
        ApplyWetness();
    }

    void Update()
    {
        // R키 디버그 토글
        if (GameInput.GetKeyDown(KeyCode.R))
        {
            isRaining = !isRaining;
            Debug.Log($"[Rain] R키: 비 {(isRaining ? "ON" : "OFF")}");
        }

        // WeatherData에서 타겟 갱신
        if (weatherData != null && !GameInput.GetKey(KeyCode.R))
        {
            isRaining = weatherData.IsRaining;
        }

        targetWetness = isRaining ? (weatherData != null ? weatherData.groundWetness : 0.7f) : 0f;

        // 부드러운 전환
        currentWetness = Mathf.MoveTowards(currentWetness, targetWetness,
            wetnessTransitionSpeed * Time.deltaTime);

        ApplyWetness();
        UpdateParticle();
        UpdateFog();
    }

    void ApplyWetness()
    {
        // 모든 WetFloor 셰이더 머테리얼에 글로벌 적용
        Shader.SetGlobalFloat(WetnessID, currentWetness);

        if (weatherData != null)
        {
            Shader.SetGlobalFloat(RippleSpeedID, weatherData.rippleSpeed * 3f);
            Shader.SetGlobalFloat(RippleDensityID, weatherData.rippleDensity * 15f + 3f);
        }
    }

    void UpdateParticle()
    {
        if (rainParticle == null) return;

        var emission = rainParticle.emission;

        if (isRaining && currentWetness > 0.01f)
        {
            if (!rainParticle.isPlaying) rainParticle.Play();

            float rate = weatherData != null ? weatherData.EffectiveRainRate : 300f;
            emission.rateOverTime = rate * currentWetness;

            var main = rainParticle.main;
            main.startSpeed = weatherData != null ? weatherData.rainSpeed : 12f;

            // 바람
            var velocityOverLifetime = rainParticle.velocityOverLifetime;
            if (weatherData != null)
            {
                velocityOverLifetime.enabled = true;
                velocityOverLifetime.x = weatherData.rainWindDirection.x * 3f;
                velocityOverLifetime.z = weatherData.rainWindDirection.z * 3f;
            }

            // 비 색상
            if (weatherData != null)
            {
                main.startColor = new Color(
                    weatherData.rainTint.r, weatherData.rainTint.g,
                    weatherData.rainTint.b, 0.3f);
            }
        }
        else
        {
            emission.rateOverTime = 0;
            if (rainParticle.isPlaying && rainParticle.particleCount == 0)
                rainParticle.Stop();
        }
    }

    void UpdateFog()
    {
        if (weatherData == null) return;
        if (!weatherData.rainOverrideFog) return;

        if (isRaining && currentWetness > 0.1f)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor,
                weatherData.rainFogColor, currentWetness);
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity,
                weatherData.rainFogDensity, currentWetness);
        }
    }

    /// <summary>에디터에서 호출: 비 파티클 시스템 생성</summary>
    public static ParticleSystem CreateRainParticle(Transform parent)
    {
        var rainGO = new GameObject("RainParticle");
        rainGO.transform.SetParent(parent);
        rainGO.transform.localPosition = new Vector3(0, 15f, 0);

        var ps = rainGO.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.maxParticles = 2000;
        main.startLifetime = 1.5f;
        main.startSpeed = 12f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startColor = new Color(0.7f, 0.75f, 0.85f, 0.3f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.2f;

        var emission = ps.emission;
        emission.rateOverTime = 0; // RainController가 제어

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20f, 0.1f, 20f);

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.x = 1f;
        velocityOverLifetime.z = 0.3f;

        // 크기 감소
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.Linear(0, 1, 1, 0.3f));

        // 머테리얼
        var renderer = rainGO.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 8f;
        renderer.velocityScale = 0.1f;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.name = "RainDropMat";
        mat.SetColor("_BaseColor", new Color(0.7f, 0.75f, 0.85f, 0.25f));
        // Transparent + Additive
        mat.SetFloat("_Surface", 1);
        renderer.sharedMaterial = mat;

        return ps;
    }
}
