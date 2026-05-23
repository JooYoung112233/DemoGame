using UnityEngine;

public class NeonSign : MonoBehaviour
{
    [SerializeField] Light neonLight;
    [SerializeField] MeshRenderer signRenderer;
    [SerializeField] DayNightCycle dayNight;

    [Header("Neon Settings")]
    [SerializeField] Color neonColor = new Color(1f, 0.2f, 0.3f);
    [SerializeField] float baseIntensity = 8f;
    [SerializeField] float flickerSpeed = 3f;
    [SerializeField] float flickerAmount = 0.3f;

    Material emissiveMat;
    float flickerTimer;
    bool isActive;

    void Start()
    {
        if (signRenderer != null)
            emissiveMat = signRenderer.material;

        if (dayNight != null)
        {
            dayNight.OnPhaseChanged += OnPhaseChanged;
            // 에디터 설정 유지 — 즉시 적용하지 않음
            // Light/Emission 상태는 에디터에서 설정한 그대로 시작
            isActive = dayNight.IsNight;
        }
    }

    void OnDestroy()
    {
        if (dayNight != null)
            dayNight.OnPhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(bool night)
    {
        isActive = night;
        if (neonLight != null) neonLight.enabled = night;
        UpdateEmission(night ? 1f : 0f);
    }

    void Update()
    {
        if (!isActive) return;

        flickerTimer += Time.deltaTime;

        // 불규칙한 깜빡임: 두 개의 Perlin 노이즈 합성
        float n1 = Mathf.PerlinNoise(flickerTimer * flickerSpeed, 0.5f);
        float n2 = Mathf.PerlinNoise(flickerTimer * flickerSpeed * 2.7f, 1.3f);
        float flicker = (n1 * 0.7f + n2 * 0.3f);

        // 간헐적 꺼짐 (5% 확률로 순간 OFF)
        bool momentaryOff = Mathf.PerlinNoise(flickerTimer * 0.8f, 10f) > 0.92f;
        float intensity = momentaryOff ? 0.1f : Mathf.Lerp(1f - flickerAmount, 1f, flicker);

        if (neonLight != null)
            neonLight.intensity = baseIntensity * intensity;

        UpdateEmission(intensity);
    }

    void UpdateEmission(float intensity)
    {
        if (emissiveMat == null) return;
        Color emission = neonColor * intensity * 2f;
        emissiveMat.SetColor("_EmissionColor", emission);
        emissiveMat.SetColor("_BaseColor", Color.Lerp(Color.black, neonColor, intensity));
    }
}
