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
            // 시작 오프셋으로 지역마다 시간 다르게
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
        if (FindObjectOfType<RegionTimeManager>(true) != null) return;

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
        // 지역별 시작 시간 오프셋 (서로 다른 시간대)
        regions = new RegionTime[]
        {
            new RegionTime("street",    "폐상가 거리", 0f,      dayDuration, nightDuration),
            new RegionTime("apartment", "붕괴 아파트", 60f,     dayDuration, nightDuration),
            new RegionTime("underground","지하 상가",  200f,    dayDuration, nightDuration),
        };

        // 시작 오프셋 적용: 큰 오프셋이면 이미 밤일 수 있음
        for (int i = 0; i < regions.Length; i++)
        {
            float offset = 0f;
            switch (i)
            {
                case 0: offset = 0f; break;
                case 1: offset = 60f; break;
                case 2: offset = 200f; break;
            }
            // 오프셋만큼 시간을 시뮬레이션
            SimulateTime(regions[i], offset);
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
        // unscaledDeltaTime → timeScale=0(안전가옥)에서도 시간 흐름
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

    /// <summary>지역 ID로 시간 정보 가져오기</summary>
    public RegionTime GetRegion(string regionId)
    {
        if (regions == null) return null;
        for (int i = 0; i < regions.Length; i++)
            if (regions[i].regionId == regionId)
                return regions[i];
        return null;
    }

    /// <summary>지역 인덱스로 시간 정보 가져오기</summary>
    public RegionTime GetRegionByIndex(int index)
    {
        if (regions == null || index < 0 || index >= regions.Length) return null;
        return regions[index];
    }

    /// <summary>등록된 지역 수</summary>
    public int RegionCount => regions != null ? regions.Length : 0;

    /// <summary>현재 페이즈의 남은 시간</summary>
    public float GetTimeRemaining(string regionId)
    {
        var rt = GetRegion(regionId);
        if (rt == null) return 0;
        float maxTime = rt.isNight ? nightDuration : dayDuration;
        return maxTime - rt.elapsed;
    }

    /// <summary>현재 페이즈 진행률 (0~1)</summary>
    public float GetPhaseProgress(string regionId)
    {
        var rt = GetRegion(regionId);
        if (rt == null) return 0;
        float maxTime = rt.isNight ? nightDuration : dayDuration;
        return rt.elapsed / maxTime;
    }
}
