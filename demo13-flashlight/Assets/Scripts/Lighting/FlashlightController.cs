using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 2D 손전등 컨트롤러.
/// Light2D(Spot) 기반. 배터리 소모, F키 토글, 깜빡임.
/// TopDownPlayer 자식 flashlightPivot에 Light2D와 함께 붙임.
/// </summary>
public class FlashlightController : MonoBehaviour
{
    [Header("Light2D")]
    [SerializeField] Light2D spotLight;
    [SerializeField] float flickerIntensity = 0.08f;
    [SerializeField] float flickerSpeed = 8f;

    [Header("주변 조명 (Point Light)")]
    [SerializeField] Light2D ambientGlow;
    [SerializeField] float glowIntensity = 0.6f;
    [SerializeField] Color glowColor = new Color(0.6f, 0.65f, 0.8f);
    [SerializeField] bool glowAlwaysOn = true;

    [Header("배터리")]
    [SerializeField] float maxBattery = 90f;
    [SerializeField] float drainRate = 1f;

    DayNightCycle dayNight;
    float currentBattery;
    bool isOn = true;
    float baseIntensity;
    float baseGlowIntensity;
    float flickerTimer;

    public float BatteryPercent => currentBattery / maxBattery;
    public bool IsOn => isOn;

    void Awake()
    {
        currentBattery = maxBattery;

        if (spotLight != null)
            baseIntensity = spotLight.intensity;

        if (ambientGlow != null)
        {
            ambientGlow.color = glowColor;
            ambientGlow.intensity = glowIntensity;
            baseGlowIntensity = glowIntensity;
        }
    }

    void Start()
    {
        dayNight = FindFirstObjectByType<DayNightCycle>();
        if (dayNight != null)
            dayNight.OnPhaseChanged += OnPhaseChanged;
    }

    void OnDestroy()
    {
        if (dayNight != null)
            dayNight.OnPhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(bool isNight)
    {
        if (!isNight && isOn) SetActive(false);
    }

    void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen()) return;

        if (Input.GetKeyDown(KeyCode.F)) Toggle();

        if (isOn && currentBattery > 0)
        {
            currentBattery -= drainRate * Time.deltaTime;
            if (currentBattery <= 0)
            {
                currentBattery = 0;
                SetActive(false);
            }
            ApplyFlicker();
        }
    }

    void Toggle()
    {
        if (currentBattery <= 0) return;
        isOn = !isOn;
        SetActive(isOn);
    }

    void SetActive(bool active)
    {
        isOn = active;
        if (spotLight != null) spotLight.enabled = active;
        if (ambientGlow != null && !glowAlwaysOn) ambientGlow.enabled = active;
    }

    void ApplyFlicker()
    {
        flickerTimer += Time.deltaTime * flickerSpeed;
        float flicker = Mathf.PerlinNoise(flickerTimer, 0f) * flickerIntensity;
        float batteryDim = BatteryPercent < 0.2f ? 0.5f + BatteryPercent * 2.5f : 1f;

        if (spotLight != null)
            spotLight.intensity = (baseIntensity + flicker) * batteryDim;

        if (ambientGlow != null && !glowAlwaysOn)
            ambientGlow.intensity = baseGlowIntensity * batteryDim;
    }

    public void AddBattery(float amount)
    {
        currentBattery = Mathf.Clamp(currentBattery + amount, 0f, maxBattery);
    }
}
