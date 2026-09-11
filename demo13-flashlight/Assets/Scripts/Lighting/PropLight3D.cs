using UnityEngine;

/// <summary>Existing practical lights use restrained daylight and stronger night illumination.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
public sealed class PropLight3D : MonoBehaviour
{
    [Min(0)] public float dayIntensity = 6f;
    [Min(0)] public float nightIntensity = 18f;

    Light _light;
    DayNightCycle _cycle;

    void Awake() => _light = GetComponent<Light>();

    void OnEnable()
    {
        _cycle = FindFirstObjectByType<DayNightCycle>();
        if (_cycle == null) return; // Preserve authored lighting in isolated scenes.
        _cycle.OnPhaseChanged += Apply;
        // A map can load after the persistent Systems clock has already entered night.
        Apply(_cycle.IsNight);
    }

    void OnDisable()
    {
        if (_cycle != null) _cycle.OnPhaseChanged -= Apply;
        _cycle = null;
    }

    void Apply(bool night)
    {
        if (_light != null) _light.intensity = night ? nightIntensity : dayIntensity;
    }
}
