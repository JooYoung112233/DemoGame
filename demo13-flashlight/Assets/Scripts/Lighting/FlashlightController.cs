using UnityEngine;

/// <summary>
/// 손전등 컨트롤러.
/// 3D Spot 라이트 기반. 배터리 소모, F키 토글, 깜빡임.
/// TopDownPlayer 자식 flashlightPivot에 Light와 함께 붙임.
/// ⚠️ 2D의 `pointLightInner/OuterAngle`·`Radius`는 3D에서 `innerSpotAngle`·`spotAngle`·`range`다.
///    `falloffIntensity`(거리 끝처리)는 3D에 대응이 없어 버린다 — 감쇠는 range가 맡는다.
/// </summary>
public class FlashlightController : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] Light spotLight;
    [SerializeField] float flickerIntensity = 0.08f;
    [SerializeField] float flickerSpeed = 8f;

    [Header("주변 조명 (Point Light)")]
    [SerializeField] Light ambientGlow;
    [SerializeField] float glowIntensity = 0.6f;
    [SerializeField] Color glowColor = new Color(0.6f, 0.65f, 0.8f);
    [SerializeField] bool glowAlwaysOn = true;

    // ── 무드(따뜻한 손전등) — 레퍼런스: 어두운 밤 + 따뜻한 부드러운 빛 + 밝은 코어 ──
    [Header("무드 라이트 (따뜻한·부드러운)")]
    [Tooltip("켜면 아래 값으로 spot/glow 라이트를 덮어씀. Awake 1회 적용.")]
    [SerializeField] bool applyMoodConfig = true;
    [Header("· 콘(spot)")]
    [SerializeField] Color spotColor = new Color(1f, 0.85f, 0.55f);   // 따뜻한 백열
    [SerializeField] float spotIntensity = 1.0f;
    [Tooltip("이 각도까지 풀 밝기, 여기서 outer까지 페이드. 작을수록 옆면이 부드러움(중심만 밝고 가장자리로 흐려짐).")]
    [SerializeField] float spotInnerAngle = 10f;                       // ★ 옆면 하드라인 제거(긴 falloff)
    [SerializeField] float spotOuterAngle = 55f;
    [SerializeField] float spotOuterRadius = 9f;
    [Header("· 코어(발치 주변광)")]
    [SerializeField] Color coreColor = new Color(1f, 0.8f, 0.5f);     // 캐릭터 감싸는 따뜻한 풀
    [SerializeField] float coreIntensity = 0.95f;
    [Tooltip("캐릭터 주변광 반경. 콘 시작점을 감싸 빔이 자연스럽게 번져 나오게.")]
    [SerializeField] float coreRadius = 3.0f;                          // ★ 코어 키워 origin 감싸기

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

        if (applyMoodConfig) ApplyMoodConfig();

        if (spotLight != null)
            baseIntensity = spotLight.intensity;

        if (ambientGlow != null)
        {
            if (!applyMoodConfig)   // 무드 미사용 시 기존(차가운) 글로우 유지
            {
                ambientGlow.color = glowColor;
                ambientGlow.intensity = glowIntensity;
            }
            baseGlowIntensity = ambientGlow.intensity;
        }
    }

    /// <summary>따뜻하고 부드러운 무드 라이트 값을 spot/glow 라이트에 적용(프리팹 흰색 하드 콘 대체).</summary>
    void ApplyMoodConfig()
    {
        if (spotLight != null)
        {
            spotLight.color = spotColor;
            spotLight.intensity = spotIntensity;
            spotLight.type = LightType.Spot;
            spotLight.innerSpotAngle = spotInnerAngle;
            spotLight.spotAngle = spotOuterAngle;
            spotLight.shadows = LightShadows.None;   // 손전등 그림자는 비싸고 쿼터뷰에선 잘 안 보인다
            spotLight.range = spotOuterRadius;
        }
        if (ambientGlow != null)
        {
            ambientGlow.color = coreColor;
            ambientGlow.intensity = coreIntensity;
            ambientGlow.type = LightType.Point;
            ambientGlow.shadows = LightShadows.None;
            ambientGlow.range = coreRadius;
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

        if (GameInput.GetKeyDown(KeyCode.F)) Toggle();

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
