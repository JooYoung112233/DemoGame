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
        HierarchyFolder.Persist(gameObject);
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

    // ── 기본 구역 (2026-09-12) ──
    // 설계(navigation.md §3.1): 모든 레이드 맵은 미니맵 구역을 기본 1개 가진다. 옛 GameSceneBuilder만 이걸 깔았고
    // 3D 맵 빌더(Zone1 등)는 빠뜨려 지도·나침반이 비어 있었다 → 구역이 하나도 없는 레이드 맵이면 런타임에 1개 깐다.

    public void RequestDefaultZone(UnityEngine.SceneManagement.Scene scene)
    {
        StartCoroutine(CreateDefaultZoneNextFrame(scene));
    }

    System.Collections.IEnumerator CreateDefaultZoneNextFrame(UnityEngine.SceneManagement.Scene scene)
    {
        yield return null;   // 씬에 배치된 구역(OnEnable 등록)과 NavGrid 런타임 배치가 끝난 뒤에 본다
        if (!scene.IsValid() || !scene.isLoaded || _zones.Count > 0) yield break;
        var grid = NavGrid.Instance;
        if (grid == null) yield break;

        Rect r = grid.PlanBounds;
        var go = new GameObject("MapZone_All");
        go.SetActive(false);   // Configure 뒤에 켜야 OnEnable 등록에 크기가 반영된다
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.SetParent(HierarchyFolder.RuntimeFolder(scene, "Map"), false);
        go.transform.position = Plan3D.ToWorld(r.center, 0f);
        go.AddComponent<MapZoneVolume>().Configure("MapZone_All", "지역 전체", r.size);
        go.SetActive(true);
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

    // ── 세이브 영속 (2026-07-11 — 그동안 세션 내에서만 유지되던 스텁 해소) ──────
    //   "안전가옥 지도판 누적"(navigation.md §3.3 결정)이 실제로 저장되게 한다.
    //   확률로 막힌 통로는 매 레이드 랜덤이라 저장하지 않는다(설계 주석 유지).

    /// <summary>발견 존 + 알게 된 통로를 지역별로 직렬화.</summary>
    public RaidMapSaveData GetSaveData()
    {
        var d = new RaidMapSaveData();
        foreach (var kv in _discoveredByRegion)
            d.discovered.Add(new RaidMapRegionEntry { regionId = kv.Key, ids = new List<string>(kv.Value) });
        foreach (var kv in _knownPassagesByRegion)
            d.passages.Add(new RaidMapRegionEntry { regionId = kv.Key, ids = new List<string>(kv.Value) });
        return d;
    }

    /// <summary>세이브 복원. null이면 전부 비움(새 게임).</summary>
    public void LoadSaveData(RaidMapSaveData d)
    {
        _discoveredByRegion.Clear();
        _knownPassagesByRegion.Clear();
        if (d != null)
        {
            if (d.discovered != null)
                foreach (var e in d.discovered)
                    if (!string.IsNullOrEmpty(e.regionId))
                        _discoveredByRegion[e.regionId] = new HashSet<string>(e.ids ?? new List<string>());
            if (d.passages != null)
                foreach (var e in d.passages)
                    if (!string.IsNullOrEmpty(e.regionId))
                        _knownPassagesByRegion[e.regionId] = new HashSet<string>(e.ids ?? new List<string>());
        }
        // 현재 씬에 이미 등록된 존이 있으면 복원된 지식으로 표시를 갱신.
        OnMapChanged?.Invoke();
    }

    /// <summary>새 게임 리셋 — 지도 지식 비움.</summary>
    public void ResetForNewGame() => LoadSaveData(null);

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
