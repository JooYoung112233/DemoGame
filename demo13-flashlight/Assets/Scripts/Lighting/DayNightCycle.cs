using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] float dayDuration = 120f;
    [SerializeField] float nightDuration = 300f;

    [Header("Lighting")]
    [SerializeField] Light directionalLight;
    [SerializeField] float dayIntensity = 1f;
    [SerializeField] float nightIntensity = 0f;
    [SerializeField] Color dayColor = new Color(1f, 0.95f, 0.9f);
    [SerializeField] Color nightColor = Color.black;

    [Header("References")]
    [SerializeField] FlashlightController flashlight;

    float currentTime;
    bool isNight = false;

    public bool IsNight => isNight;
    public float TimeRemaining => isNight
        ? nightDuration - currentTime
        : dayDuration - currentTime;

    public event System.Action<bool> OnPhaseChanged;

    void Start()
    {
        // 에디터 설정 유지 — 시작 시 라이팅 덮어쓰지 않음
    }

    void Update()
    {
        currentTime += Time.deltaTime;
        float maxTime = isNight ? nightDuration : dayDuration;
        if (currentTime >= maxTime && !isNight)
            SetPhase(true);

        if (Input.GetKeyDown(KeyCode.T))
            SetPhase(!isNight);
    }

    void SetPhase(bool toNight)
    {
        isNight = toNight;
        currentTime = 0f;
        ApplyLighting();
        OnPhaseChanged?.Invoke(isNight);
    }

    void ApplyLighting()
    {
        if (directionalLight == null) return;
        directionalLight.intensity = isNight ? nightIntensity : dayIntensity;
        directionalLight.color = isNight ? nightColor : dayColor;
    }
}
