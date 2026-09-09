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

        // ⚠️ 태양은 **게임플레이 씬**에 있고 이 컴포넌트는 Systems 씬에 있다(영구 상주).
        //    씬을 갈아끼우면 캐시된 태양이 파괴되고 새 씬의 태양은 구운 값(=낮) 그대로다 —
        //    밤에 레이드에 들어가도 T를 한 번 누르기 전까지 대낮이었다. 로드마다 다시 건다.
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
        => UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
    {
        directionalLight = null;   // 새 씬의 태양을 다시 찾게 한다(옛 태양은 이미 파괴됨)
        globalLight = null;
        ApplyLighting();
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

        if (GameInput.GetKeyDown(KeyCode.T))
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
        // ── 3D 태양(주 조명) ──────────────────────────────────────────
        // 2026-09-08: 주·부가 뒤집혔다. 예전엔 Light2D 글로벌이 주 조명이고 3D는 "레거시,
        // 있을 때만"이었는데, 3D 전환으로 **디렉셔널이 주 조명**이다.
        // 씬에 지정이 없으면 찾는다 — 태양은 Systems(PlayerRig)에 있고 맵 씬에는 없다.
        if (directionalLight == null) directionalLight = FindSun();
        if (weatherData == null) weatherData = LoadWeather();

        if (directionalLight != null)
        {
            if (weatherData != null)
            {
                directionalLight.intensity = isNight ? weatherData.nightIntensity : weatherData.dayIntensity;
                directionalLight.color     = isNight ? weatherData.nightLightColor : weatherData.dayLightColor;
                directionalLight.transform.rotation = Quaternion.Euler(
                    isNight ? weatherData.nightSunAngle : weatherData.daySunAngle);

                ApplyAmbient(isNight ? weatherData.nightAmbientColor : weatherData.dayAmbientColor);

                bool useFog = isNight ? weatherData.nightFog : weatherData.dayFog;
                RenderSettings.fog = useFog;
                if (useFog)
                {
                    RenderSettings.fogColor   = isNight ? weatherData.nightFogColor : weatherData.dayFogColor;
                    RenderSettings.fogDensity = isNight ? weatherData.nightFogDensity : weatherData.dayFogDensity;
                }
            }
            else
            {
                directionalLight.intensity = isNight ? nightIntensity : dayIntensity;
                directionalLight.color     = isNight ? nightColor : dayColor;
            }
        }

        // ── 2D 글로벌 라이트 (남아 있는 2D 씬용) ──────────────────────
        // 3D 씬엔 없다. 찾지 못해도 조용히 넘어간다.
        if (globalLight == null) globalLight = FindGlobalLight();
        if (globalLight != null)
        {
            globalLight.intensity = isNight ? nightGlobalIntensity : dayGlobalIntensity;
            globalLight.color     = isNight ? nightGlobalColor : dayGlobalColor;
        }
    }

    /// <summary>WeatherData를 Resources에서 찾는다(StatDB·GameTuning과 같은 자리).
    ///
    /// ⚠️ 인스펙터 참조에만 기대면 안 된다. Systems 씬의 <c>weatherData</c>가 비어 있어
    ///    (<c>fileID: 0</c>) **포그·앰비언트·태양 각도 분기 전체가 런타임에 한 번도 안 돌았다.**
    ///    씬을 다시 구울 때마다 손으로 끼우는 참조는 이렇게 조용히 빠진다.</summary>
    static WeatherData LoadWeather()
    {
        var w = Resources.Load<WeatherData>("Data/WeatherData");
        if (w == null) Debug.LogWarning("[DayNight] Resources/Data/WeatherData 없음 — 인스펙터 폴백 값으로 돈다.");
        return w;
    }

    /// <summary>WeatherData의 앰비언트 색 <b>하나</b>를 3색 앰비언트(Trilight)에 편다.
    ///
    /// ⚠️ <c>RenderSettings.ambientLight</c>는 <c>ambientMode</c>가 Flat일 때만 먹는다.
    ///    맵 빌더(<see cref="Lighting3D"/>)가 Trilight로 굽기 때문에, 그냥 대입하면
    ///    **낮밤을 아무리 토글해도 앰비언트가 안 변한다** — 실제로 낮·밤 측정값이
    ///    같은 RGBA(0.212,0.227,0.259)로 찍혔다.
    ///
    /// WeatherData는 앰비언트를 색 하나로만 들고 있어 위/옆/아래를 여기서 만든다.
    /// 하늘은 밝고 바닥은 어둡다 — 그 비율만 지키면 한 값으로도 입체감이 산다.</summary>
    static void ApplyAmbient(Color c)
    {
        // ⚠️ 알파까지 곱하면 안 된다. Color 곱셈은 a도 함께 곱해 0.55 같은 값이 남는데,
        //    앰비언트 알파는 안 쓰이지만 인스펙터에서 보면 "왜 반투명이지" 하게 된다.
        Color Scale(float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        // ⚠️ `ambientLight`는 별개 슬롯이 아니라 Trilight의 **하늘색과 같은 값**이다.
        //    뒤에 대입하면 방금 넣은 sky를 덮어쓴다(실제로 sky가 1.25배 안 먹었다). 먼저 넣는다.
        RenderSettings.ambientLight        = Scale(1.25f);
        RenderSettings.ambientSkyColor     = Scale(1.25f);
        RenderSettings.ambientEquatorColor = Scale(1f);
        RenderSettings.ambientGroundColor  = Scale(0.55f);
    }

    /// <summary>이 씬에서 낮밤이 몰 태양을 고른다.
    ///
    /// ⚠️ 예전엔 "먼저 찾은 디렉셔널"을 그냥 썼다. 맵마다 태양을 굽게 되면서 태양이 둘이 됐고
    ///    (맵 + PlayerRig에 남아 있던 것) **화면을 실제로 밝히는 쪽은 대낮에 멈춰 있었다.**
    ///    값을 재도 몰리는 쪽만 정상으로 보여 한참 안 드러났다. 그래서 지금은
    ///    <see cref="SunLight"/> 꼬리표를 우선하고, 후보가 여럿이면 경고한다.</summary>
    Light FindSun()
    {
        Light marked = null, plain = null;
        int candidates = 0;

        foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;

            var mark = l.GetComponent<SunLight>();
            if (mark != null && !mark.followDayNight) continue;   // 고정 조명 씬(은신처 등)

            candidates++;
            if (mark != null) { if (marked == null) marked = l; }
            else if (plain == null) plain = l;
        }

        var sun = marked != null ? marked : plain;
        if (candidates > 1)
            Debug.LogWarning($"[DayNight] 낮밤이 몰 수 있는 태양이 {candidates}개다 — '{sun?.name}'만 몰고 " +
                             "나머지는 구운 값에 멈춘다. 맵 씬에 태양은 하나만 두는 게 맞다.", sun);
        return sun;
    }
}
