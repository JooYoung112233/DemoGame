using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Flashlight Settings")]
    [SerializeField] Light spotLight;
    [SerializeField] float flickerIntensity = 0.1f;
    [SerializeField] float flickerSpeed = 8f;

    [Header("Ambient Glow (주변 라이트)")]
    [SerializeField] Light ambientGlow;
    [SerializeField] float glowRange = 10f;
    [SerializeField] float glowIntensity = 2.5f;
    [SerializeField] Color glowColor = new Color(0.6f, 0.65f, 0.8f);
    [SerializeField] bool glowAlwaysOn = true; // 손전등 꺼도 유지

    [Header("Battery")]
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

        // 에디터 설정 유지 — Light 컴포넌트 값을 덮어쓰지 않고 읽기만
        if (spotLight != null)
            baseIntensity = spotLight.intensity;

        if (ambientGlow != null)
        {
            ambientGlow.range = glowRange;
            ambientGlow.intensity = glowIntensity;
            ambientGlow.color = glowColor;
            baseGlowIntensity = glowIntensity;
        }
    }

    void Start()
    {
        // 낮/밤 전환 구독 — 낮이면 자동 OFF
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
        if (!isNight && isOn)
        {
            // 아침 → 손전등 자동 OFF
            SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            Toggle();

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

        // 주변 라이트: glowAlwaysOn이면 손전등 꺼도 유지
        if (ambientGlow != null && !glowAlwaysOn)
            ambientGlow.enabled = active;
    }

    void ApplyFlicker()
    {
        flickerTimer += Time.deltaTime * flickerSpeed;
        float flicker = Mathf.PerlinNoise(flickerTimer, 0f) * flickerIntensity;
        float batteryDim = BatteryPercent < 0.2f ? 0.5f + BatteryPercent * 2.5f : 1f;

        if (spotLight != null)
            spotLight.intensity = (baseIntensity + flicker) * batteryDim;

        // 주변 라이트도 배터리 연동 (미세 플리커)
        if (ambientGlow != null && !glowAlwaysOn)
            ambientGlow.intensity = baseGlowIntensity * batteryDim;
    }

    public void AddBattery(float amount)
    {
        currentBattery = Mathf.Min(currentBattery + amount, maxBattery);
    }
}
