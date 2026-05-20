using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] float dayDuration = 120f;
    [SerializeField] float nightDuration = 300f;

    [Header("Lighting")]
    [SerializeField] Light2D globalLight;
    [SerializeField] float dayIntensity = 1f;
    [SerializeField] float nightIntensity = 0.05f;
    [SerializeField] Color dayColor = Color.white;
    [SerializeField] Color nightColor = new Color(0.05f, 0.05f, 0.15f);
    [SerializeField] float transitionDuration = 3f;

    [Header("References")]
    [SerializeField] FlashlightController flashlight;

    float currentTime;
    bool isNight;
    float transitionProgress;
    bool isTransitioning;

    public bool IsNight => isNight;
    public float TimeRemaining => isNight
        ? nightDuration - currentTime
        : dayDuration - currentTime;
    public float TimePercent => isNight
        ? currentTime / nightDuration
        : currentTime / dayDuration;

    void Update()
    {
        if (isTransitioning)
        {
            UpdateTransition();
            return;
        }

        currentTime += Time.deltaTime;

        float maxTime = isNight ? nightDuration : dayDuration;
        if (currentTime >= maxTime)
        {
            if (!isNight)
                StartTransition(true);
        }

        if (Input.GetKeyDown(KeyCode.T))
            StartTransition(!isNight);

        UpdateUI();
    }

    void StartTransition(bool toNight)
    {
        isTransitioning = true;
        transitionProgress = 0f;
        isNight = toNight;
        currentTime = 0f;
    }

    void UpdateTransition()
    {
        transitionProgress += Time.deltaTime / transitionDuration;
        float t = Mathf.SmoothStep(0, 1, transitionProgress);

        if (isNight)
        {
            globalLight.intensity = Mathf.Lerp(dayIntensity, nightIntensity, t);
            globalLight.color = Color.Lerp(dayColor, nightColor, t);
        }
        else
        {
            globalLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, t);
            globalLight.color = Color.Lerp(nightColor, dayColor, t);
        }

        if (transitionProgress >= 1f)
        {
            isTransitioning = false;
        }
    }

    void UpdateUI()
    {
        // UI는 별도 HUD에서 처리
    }
}
