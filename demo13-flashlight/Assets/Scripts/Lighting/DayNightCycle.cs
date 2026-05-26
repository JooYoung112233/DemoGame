using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Lighting")]
    [SerializeField] Light directionalLight;
    [SerializeField] float dayIntensity = 1f;
    [SerializeField] float nightIntensity = 0f;
    [SerializeField] Color dayColor = new Color(1f, 0.95f, 0.9f);
    [SerializeField] Color nightColor = Color.black;

    [Header("References")]
    [SerializeField] FlashlightController flashlight;

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
            isNight = !isNight;
            lastSyncedNight = isNight;
            ApplyLighting();
            OnPhaseChanged?.Invoke(isNight);
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
        if (directionalLight == null) return;
        directionalLight.intensity = isNight ? nightIntensity : dayIntensity;
        directionalLight.color = isNight ? nightColor : dayColor;
    }
}
