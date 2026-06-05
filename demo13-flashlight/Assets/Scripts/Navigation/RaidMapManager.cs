using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 레이드 맵 상태 + 영속 지식 관리 싱글톤(DontDestroyOnLoad).
/// - 현재 레이드(씬)에 등록된 존 목록 (MapZoneVolume가 등록).
/// - 영속: 지역별로 발견한 존 / 알게 된 잠김 통로 → 안전가옥 지도판 누적.
///   ※ 확률로 막힌 통로는 매 레이드 랜덤이라 영속 저장하지 않음(PassageMarker가 이번 판만 관리).
/// 설계: docs/navigation.md §3
/// </summary>
public class RaidMapManager : MonoBehaviour
{
    static RaidMapManager _instance;

    /// <summary>접근 시 없으면 생성(런타임). 에디트 모드에선 생성하지 않음.</summary>
    public static RaidMapManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<RaidMapManager>();
                if (_instance == null && Application.isPlaying)
                {
                    var go = new GameObject("[RaidMapManager]");
                    _instance = go.AddComponent<RaidMapManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    /// <summary>생성 없이 현재 인스턴스만 반환(teardown/OnDisable 안전용).</summary>
    public static RaidMapManager InstanceIfExists => _instance;

    // 현재 레이드(씬)에 등록된 존
    readonly List<MapZone> _zones = new List<MapZone>();
    public IReadOnlyList<MapZone> Zones => _zones;

    // 영속 지식 (지역 id → 발견 존 / 알게 된 통로)
    readonly Dictionary<string, HashSet<string>> _discoveredByRegion = new Dictionary<string, HashSet<string>>();
    readonly Dictionary<string, HashSet<string>> _knownPassagesByRegion = new Dictionary<string, HashSet<string>>();

    /// <summary>맵 상태 변경(존 등록/발견/통로 주석) 시 발생 — HUD가 구독.</summary>
    public event System.Action OnMapChanged;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    /// <summary>현재 지역 id. RegionTimeManager 우선, 없으면 활성 씬 이름.</summary>
    public string CurrentRegionId
    {
        get
        {
            if (RegionTimeManager.Instance != null)
            {
                var id = RegionTimeManager.Instance.ActiveRegionId;
                if (!string.IsNullOrEmpty(id)) return id;
            }
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
    }

    HashSet<string> DiscoveredSet(string region)
    {
        if (!_discoveredByRegion.TryGetValue(region, out var s))
            _discoveredByRegion[region] = s = new HashSet<string>();
        return s;
    }

    HashSet<string> KnownPassageSet(string region)
    {
        if (!_knownPassagesByRegion.TryGetValue(region, out var s))
            _knownPassagesByRegion[region] = s = new HashSet<string>();
        return s;
    }

    // ── 존 등록 (MapZoneVolume) ──

    public void RegisterZone(MapZone zone)
    {
        if (zone == null || _zones.Contains(zone)) return;
        _zones.Add(zone);
        // 영속 복원: 이전에 발견한 존이면 즉시 표시(지도판 누적)
        if (DiscoveredSet(CurrentRegionId).Contains(zone.id))
            zone.discovered = true;
        OnMapChanged?.Invoke();
    }

    public void UnregisterZone(MapZone zone)
    {
        if (zone == null) return;
        if (_zones.Remove(zone))
            OnMapChanged?.Invoke();
    }

    // ── 발견 / 해금 ──

    /// <summary>월드 좌표가 속한 존을 발견 처리(탐색 fog 해제).</summary>
    public void DiscoverAt(Vector2 worldPos)
    {
        bool any = false;
        foreach (var z in _zones)
            if (!z.discovered && z.Contains(worldPos)) { MarkDiscoveredQuiet(z); any = true; }
        if (any) OnMapChanged?.Invoke();
    }

    /// <summary>특정 존 해금(지도 조각 아이템).</summary>
    public void RevealZone(string zoneId)
    {
        bool any = false;
        foreach (var z in _zones)
            if (z.id == zoneId && !z.discovered) { MarkDiscoveredQuiet(z); any = true; }
        if (any) OnMapChanged?.Invoke();
    }

    /// <summary>전체 해금(전체 지도 아이템).</summary>
    public void RevealAll()
    {
        bool any = false;
        foreach (var z in _zones)
            if (!z.discovered) { MarkDiscoveredQuiet(z); any = true; }
        if (any) OnMapChanged?.Invoke();
    }

    void MarkDiscoveredQuiet(MapZone z)
    {
        z.discovered = true;
        DiscoveredSet(CurrentRegionId).Add(z.id); // 영속
    }

    // ── 통로 주석 영속(잠김만; 막힘은 이번 판 한정) ──

    public bool IsPassageKnown(string passageId)
        => !string.IsNullOrEmpty(passageId) && KnownPassageSet(CurrentRegionId).Contains(passageId);

    public void MarkPassageKnown(string passageId)
    {
        if (string.IsNullOrEmpty(passageId)) return;
        if (KnownPassageSet(CurrentRegionId).Add(passageId))
            OnMapChanged?.Invoke();
    }

    // ── 세이브 훅(스텁) ──
    // TODO(SaveManager 연동): _discoveredByRegion / _knownPassagesByRegion 직렬화·복원.
    //   영속 대상은 '고정 요소'(구역 발견 + 잠김 통로)뿐. 확률 막힘은 저장 안 함.
}
