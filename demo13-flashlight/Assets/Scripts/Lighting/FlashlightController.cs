using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Flashlight Settings")]
    [SerializeField] Light spotLight;
    [SerializeField] Light ambientGlow;
    [SerializeField] float flickerIntensity = 0.1f;
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
        if (spotLight != null)
            baseIntensity = spotLight.intensity;
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
    }

    void ApplyFlicker()
    {
        if (spotLight == null) return;
        flickerTimer += Time.deltaTime * flickerSpeed;
        float flicker = Mathf.PerlinNoise(flickerTimer, 0f) * flickerIntensity;
        float batteryDim = BatteryPercent < 0.2f ? 0.5f + BatteryPercent * 2.5f : 1f;
        spotLight.intensity = (baseIntensity + flicker) * batteryDim;
    }

    public void AddBattery(float amount)
    {
        currentBattery = Mathf.Min(currentBattery + amount, maxBattery);
    }
}
