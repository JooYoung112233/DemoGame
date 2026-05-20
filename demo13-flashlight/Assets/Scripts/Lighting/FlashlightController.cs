using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FlashlightController : MonoBehaviour
{
    [Header("Flashlight Settings")]
    [SerializeField] Light2D coneLight;
    [SerializeField] Light2D ambientGlow;
    [SerializeField] float coneAngle = 90f;
    [SerializeField] float coneRange = 7f;
    [SerializeField] float glowRange = 2f;
    [SerializeField] float flickerIntensity = 0.05f;
    [SerializeField] float flickerSpeed = 8f;

    [Header("Battery")]
    [SerializeField] float maxBattery = 90f;
    [SerializeField] float drainRate = 1f;

    float currentBattery;
    bool isOn = true;
    float baseIntensity;
    float flickerTimer;

    public float BatteryPercent => currentBattery / maxBattery;
    public bool IsOn => isOn;

    void Awake()
    {
        currentBattery = maxBattery;
        if (coneLight != null)
            baseIntensity = coneLight.intensity;
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
                SetFlashlightActive(false);
            }

            ApplyFlicker();
        }

        UpdateLightParams();
    }

    void Toggle()
    {
        if (currentBattery <= 0) return;
        isOn = !isOn;
        SetFlashlightActive(isOn);
    }

    void SetFlashlightActive(bool active)
    {
        isOn = active;
        if (coneLight != null) coneLight.enabled = active;
    }

    void ApplyFlicker()
    {
        if (coneLight == null) return;
        flickerTimer += Time.deltaTime * flickerSpeed;
        float flicker = Mathf.PerlinNoise(flickerTimer, 0f) * flickerIntensity;

        float batteryDim = BatteryPercent < 0.2f ? 0.5f + BatteryPercent * 2.5f : 1f;
        coneLight.intensity = (baseIntensity + flicker) * batteryDim;
    }

    void UpdateLightParams()
    {
        if (coneLight != null)
        {
            coneLight.pointLightOuterAngle = coneAngle;
            coneLight.pointLightOuterRadius = coneRange;
        }
        if (ambientGlow != null)
        {
            ambientGlow.pointLightOuterRadius = glowRange;
            ambientGlow.enabled = true;
        }
    }

    public void AddBattery(float amount)
    {
        currentBattery = Mathf.Min(currentBattery + amount, maxBattery);
    }
}
