using UnityEngine;

/// <summary>
/// 지역별 독립 시간 관리 (DontDestroyOnLoad 싱글톤).
/// 안전가옥(timeScale=0)에서도 unscaledDeltaTime으로 계속 흐름.
/// 각 지역은 고유한 낮/밤 사이클을 가짐.
/// </summary>
public class RegionTimeManager : MonoBehaviour
{
    public static RegionTimeManager Instance { get; private set; }

    [Header("Cycle Settings (전 지역 공통)")]
    [SerializeField] float dayDuration = 120f;
    [SerializeField] float nightDuration = 300f;

    /// <summary>지역 하나의 시간 상태</summary>
    [System.Serializable]
    public class RegionTime
    {
        public string regionId;
        public string displayName;
        public float elapsed;   // 현재 페이즈 내 경과 시간
        public bool isNight;

        public RegionTime(string id, string name, float startOffset, float dayDur, float nightDur)
        {
            regionId = id;
            displayName = name;
            isNight = false;
            elapsed = startOffset % dayDur;
        }
    }

    RegionTime[] regions;

    /// <summary>현재 플레이어가 들어간 지역 ID (null이면 안전가옥)</summary>
    public string ActiveRegionId { get; set; }

    public float DayDuration => dayDuration;
    public float NightDuration => nightDuration;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        if (FindFirstObjectByType<RegionTimeManager>(FindObjectsInactive.Include) != null) return;

        var go = new GameObject("[RegionTimeManager]");
        go.AddComponent<RegionTimeManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitRegions();
    }

    void InitRegions()
    {
        var defs = WorldRegionCatalog.All;
        regions = new RegionTime[defs.Length];

        for (int i = 0; i < defs.Length; i++)
        {
            var d = defs[i];
            regions[i] = new RegionTime(d.regionId, d.displayName, d.timeOffsetSeconds, dayDuration, nightDuration);
            SimulateTime(regions[i], d.timeOffsetSeconds);
        }
    }

    /// <summary>오프셋 시간만큼 시뮬레이션 (초기화용)</summary>
    void SimulateTime(RegionTime rt, float totalSeconds)
    {
        float remaining = totalSeconds;
        while (remaining > 0)
        {
            float phaseDur = rt.isNight ? nightDuration : dayDuration;
            float left = phaseDur - rt.elapsed;
            if (remaining >= left)
            {
                remaining -= left;
                rt.elapsed = 0;
                rt.isNight = !rt.isNight;
            }
            else
            {
                rt.elapsed += remaining;
                remaining = 0;
            }
        }
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        for (int i = 0; i < regions.Length; i++)
        {
            var rt = regions[i];
            rt.elapsed += dt;

            float maxTime = rt.isNight ? nightDuration : dayDuration;
            if (rt.elapsed >= maxTime)
            {
                rt.elapsed -= maxTime;
                rt.isNight = !rt.isNight;
            }
        }
    }

    public RegionTime GetRegion(string regionId)
    {
        if (regions == null) return null;
        for (int i = 0; i < regions.Length; i++)
            if (regions[i].regionId == regionId)
                return regions[i];
        return null;
    }

    public RegionTime GetRegionByIndex(int index)
    {
        if (regions == null || index < 0 || index >= regions.Length) return null;
        return regions[index];
    }

    public int RegionCount => regions != null ? regions.Length : 0;

    public float GetTimeRemaining(string regionId)
    {
        var rt = GetRegion(regionId);
        if (rt == null) return 0;
        float maxTime = rt.isNight ? nightDuration : dayDuration;
        return maxTime - rt.elapsed;
    }

    public float GetPhaseProgress(string regionId)
    {
        var rt = GetRegion(regionId);
        if (rt == null) return 0;
        float maxTime = rt.isNight ? nightDuration : dayDuration;
        return rt.elapsed / maxTime;
    }
}
