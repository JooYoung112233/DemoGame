using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightCycle : MonoBehaviour
{
    [Header("2D Global Light (탑다운 주 조명)")]
    [Tooltip("비우면 씬의 Global Light2D 자동 탐색")]
    [SerializeField] Light2D globalLight;
    [SerializeField] float dayGlobalIntensity = 1f;
    [SerializeField] float nightGlobalIntensity = 0.18f;
    [SerializeField] Color dayGlobalColor = new Color(1f, 0.97f, 0.9f);
    [SerializeField] Color nightGlobalColor = new Color(0.35f, 0.42f, 0.62f);

    [Header("3D Lighting (레거시 — 2D에선 보통 미사용)")]
    [SerializeField] Light directionalLight;
    [SerializeField] float dayIntensity = 1f;
    [SerializeField] float nightIntensity = 0f;
    [SerializeField] Color dayColor = new Color(1f, 0.95f, 0.9f);
    [SerializeField] Color nightColor = Color.black;

    [Header("Data")]
    [SerializeField] WeatherData weatherData;

    bool isNight = false;
    bool lastSyncedNight = false;
    bool useStandalone = false; // RegionTimeManager 없을 때 독립 모드

    public bool IsNight => isNight;
    public float TimeRemaining
    {
        get
        {
            if (useStandalone) return 0;
            if (RegionTimeManager.Instance == null) return 0;
            var rt = RegionTimeManager.Instance.GetRegion(RegionTimeManager.Instance.ActiveRegionId);
            if (rt == null) return 0;
            float max = rt.isNight ? RegionTimeManager.Instance.NightDuration : RegionTimeManager.Instance.DayDuration;
            return max - rt.elapsed;
        }
    }

    public event System.Action<bool> OnPhaseChanged;

    /// <summary>낮/밤 직접 설정 (에디터 버튼·런타임 공용). 조명 즉시 적용 + 이벤트 + 지역시간 동기화.</summary>
    public void SetNight(bool night)
    {
        isNight = night;
        lastSyncedNight = night;
        ApplyLighting();
        OnPhaseChanged?.Invoke(isNight);

        // RegionTimeManager 연동 모드면 지역 시간도 맞춤
        if (Application.isPlaying && RegionTimeManager.Instance != null)
        {
            var regionId = RegionTimeManager.Instance.ActiveRegionId;
            if (!string.IsNullOrEmpty(regionId))
            {
                var rt = RegionTimeManager.Instance.GetRegion(regionId);
                if (rt != null) { rt.isNight = night; rt.elapsed = 0f; }
            }
        }
    }

    /// <summary>낮↔밤 토글.</summary>
    public void ToggleDayNight() => SetNight(!isNight);

    /// <summary>씬의 Global Light2D 자동 탐색.</summary>
    Light2D FindGlobalLight()
    {
        var all = FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var l in all)
            if (l.lightType == Light2D.LightType.Global) return l;
        return null;
    }

    void Start()
    {
        // RegionTimeManager가 없거나 ActiveRegionId가 비어있으면 독립 모드
        if (RegionTimeManager.Instance == null ||
            string.IsNullOrEmpty(RegionTimeManager.Instance.ActiveRegionId))
        {
            useStandalone = true;
            // 시작 시 현재 상태 적용
            ApplyLighting();
        }
        else
        {
            SyncFromRegionTime();
        }
    }

    void Update()
    {
        if (!useStandalone)
        {
            // RegionTimeManager 연결 확인 (런타임 중 ActiveRegionId 비게 될 수 있음)
            if (RegionTimeManager.Instance == null ||
                string.IsNullOrEmpty(RegionTimeManager.Instance.ActiveRegionId))
            {
                useStandalone = true;
            }
            else
            {
                SyncFromRegionTime();
            }
        }

        if (Input.GetKeyDown(KeyCode.T))
            ForceToggle();
    }

    /// <summary>RegionTimeManager의 활성 지역 시간에 동기화</summary>
    void SyncFromRegionTime()
    {
        if (RegionTimeManager.Instance == null) return;
        var regionId = RegionTimeManager.Instance.ActiveRegionId;
        if (string.IsNullOrEmpty(regionId)) return;

        var rt = RegionTimeManager.Instance.GetRegion(regionId);
        if (rt == null) return;

        isNight = rt.isNight;

        // 페이즈가 바뀌었으면 이벤트 발행 + 라이팅 적용
        if (isNight != lastSyncedNight)
        {
            lastSyncedNight = isNight;
            ApplyLighting();
            OnPhaseChanged?.Invoke(isNight);
        }
    }

    /// <summary>디버그용 강제 토글 (T키)</summary>
    void ForceToggle()
    {
        if (useStandalone)
        {
            // 독립 모드: 직접 토글
            SetNight(!isNight);
            Debug.Log($"[DayNight] T키: {(isNight ? "밤" : "낮")} 전환");
            return;
        }

        // RegionTimeManager 연동 모드
        if (RegionTimeManager.Instance == null) return;
        var regionId = RegionTimeManager.Instance.ActiveRegionId;
        if (string.IsNullOrEmpty(regionId)) return;

        var rt = RegionTimeManager.Instance.GetRegion(regionId);
        if (rt == null) return;

        rt.isNight = !rt.isNight;
        rt.elapsed = 0f;
    }

    void ApplyLighting()
    {
        // ── 2D 글로벌 라이트 (탑다운 주 조명) ──
        if (globalLight == null) globalLight = FindGlobalLight();
        if (globalLight != null)
        {
            globalLight.intensity = isNight ? nightGlobalIntensity : dayGlobalIntensity;
            globalLight.color     = isNight ? nightGlobalColor : dayGlobalColor;
        }

        // ── 3D 라이트 (레거시, 있을 때만) ──
        if (directionalLight == null) return;

        if (weatherData != null)
        {
            // WeatherData에서 읽기
            directionalLight.intensity = isNight ? weatherData.nightIntensity : weatherData.dayIntensity;
            directionalLight.color = isNight ? weatherData.nightLightColor : weatherData.dayLightColor;
            directionalLight.transform.rotation = Quaternion.Euler(
                isNight ? weatherData.nightSunAngle : weatherData.daySunAngle);

            // 앰비언트
            RenderSettings.ambientLight = isNight ? weatherData.nightAmbientColor : weatherData.dayAmbientColor;

            // 안개
            bool useFog = isNight ? weatherData.nightFog : weatherData.dayFog;
            RenderSettings.fog = useFog;
            if (useFog)
            {
                RenderSettings.fogColor = isNight ? weatherData.nightFogColor : weatherData.dayFogColor;
                RenderSettings.fogDensity = isNight ? weatherData.nightFogDensity : weatherData.dayFogDensity;
            }
        }
        else
        {
            // 폴백: 인스펙터 값 사용
            directionalLight.intensity = isNight ? nightIntensity : dayIntensity;
            directionalLight.color = isNight ? nightColor : dayColor;
        }
    }
}
