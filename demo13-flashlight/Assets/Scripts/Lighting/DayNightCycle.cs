using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] float dayDuration = 120f;
    [SerializeField] float nightDuration = 300f;

    [Header("Lighting")]
    [SerializeField] Light directionalLight;
    [SerializeField] float dayIntensity = 1f;
    [SerializeField] float nightIntensity = 0.02f;
    [SerializeField] Color dayColor = new Color(1f, 0.95f, 0.9f);
    [SerializeField] Color nightColor = new Color(0.05f, 0.05f, 0.12f);
    [SerializeField] float transitionDuration = 3f;

    [Header("References")]
    [SerializeField] FlashlightController flashlight;

    float currentTime;
    bool isNight = true;
    float transitionProgress;
    bool isTransitioning;

    public bool IsNight => isNight;
    public float TimeRemaining => isNight
        ? nightDuration - currentTime
        : dayDuration - currentTime;

    void Update()
    {
        if (isTransitioning)
        {
            UpdateTransition();
            return;
        }

        currentTime += Time.deltaTime;
        float maxTime = isNight ? nightDuration : dayDuration;
        if (currentTime >= maxTime && !isNight)
            StartTransition(true);

        if (Input.GetKeyDown(KeyCode.T))
            StartTransition(!isNight);
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

        if (directionalLight != null)
        {
            if (isNight)
            {
                directionalLight.intensity = Mathf.Lerp(dayIntensity, nightIntensity, t);
                directionalLight.color = Color.Lerp(dayColor, nightColor, t);
            }
            else
            {
                directionalLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, t);
                directionalLight.color = Color.Lerp(nightColor, dayColor, t);
            }
        }

        if (transitionProgress >= 1f)
            isTransitioning = false;
    }
}
