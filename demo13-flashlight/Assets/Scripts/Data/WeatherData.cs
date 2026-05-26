using UnityEngine;

/// <summary>
/// 낮/밤 라이팅 + 날씨 설정. ScriptableObject.
/// Weather Editor에서 편집, DayNightCycle/RainController가 런타임에 참조.
/// </summary>
[CreateAssetMenu(fileName = "WeatherData", menuName = "Demo/Weather Data")]
public class WeatherData : ScriptableObject
{
    // ===== 낮 =====
    [Header("DAY - Directional Light")]
    public float dayIntensity = 1f;
    public Color dayLightColor = new Color(1f, 0.95f, 0.9f);
    public Vector3 daySunAngle = new Vector3(50, -30, 0);

    [Header("DAY - Ambient")]
    public Color dayAmbientColor = new Color(0.35f, 0.35f, 0.4f);
    public float dayAmbientIntensity = 1f;

    [Header("DAY - Fog")]
    public bool dayFog = false;
    public Color dayFogColor = new Color(0.6f, 0.6f, 0.65f);
    public float dayFogDensity = 0.01f;

    // ===== 밤 =====
    [Header("NIGHT - Directional Light")]
    public float nightIntensity = 0f;
    public Color nightLightColor = Color.black;
    public Vector3 nightSunAngle = new Vector3(50, -30, 0);

    [Header("NIGHT - Ambient")]
    public Color nightAmbientColor = new Color(0.04f, 0.04f, 0.06f);
    public float nightAmbientIntensity = 0.2f;

    [Header("NIGHT - Fog")]
    public bool nightFog = true;
    public Color nightFogColor = new Color(0.02f, 0.02f, 0.03f);
    public float nightFogDensity = 0.04f;

    // ===== 날씨 =====
    public enum WeatherType { Clear, Rain, HeavyRain, Fog }

    [Header("WEATHER")]
    public WeatherType weather = WeatherType.Clear;

    [Header("RAIN")]
    [Range(0, 1)] public float rainIntensity = 0.6f;
    public Color rainTint = new Color(0.7f, 0.75f, 0.85f);
    [Range(0, 1)] public float groundWetness = 0.7f;
    [Range(0, 1)] public float rippleSpeed = 0.5f;
    [Range(0, 1)] public float rippleDensity = 0.5f;
    public float rainParticleRate = 300f;
    public float rainSpeed = 12f;
    public Vector3 rainWindDirection = new Vector3(0.3f, 0, 0.1f);

    [Header("RAIN FOG OVERRIDE")]
    public bool rainOverrideFog = true;
    public Color rainFogColor = new Color(0.15f, 0.17f, 0.22f);
    public float rainFogDensity = 0.06f;

    // ===== 계산 =====
    public bool IsRaining => weather == WeatherType.Rain || weather == WeatherType.HeavyRain;
    public float EffectiveWetness => IsRaining ? groundWetness : 0f;
    public float EffectiveRainRate => weather == WeatherType.HeavyRain ? rainParticleRate * 2f : rainParticleRate;
}
