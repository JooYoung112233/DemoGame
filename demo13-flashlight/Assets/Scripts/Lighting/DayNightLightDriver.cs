using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 낮 ↔ 밤 **시각적 ambient** 전환을 부드럽게(lerp) 구동.
/// - 게임플레이 위협 모델은 불변(위협 = 짙은 현상 구역, 낮/밤 아님). 이 컴포넌트는 **분위기 조명만** 담당.
/// - 글로벌 Light2D(앰비언트)의 intensity/color를 WeatherData 낮/밤 ambient 값 사이로 보간.
/// - 자체 클럭으로 day→dusk→night→dawn 곡선(전환 구간만 smoothstep, 나머지는 hold).
/// - 기존 DayNightCycle/RegionTimeManager는 건드리지 않음(루트 night 플래그 등은 그대로).
/// 사용: 씬의 빈 GameObject에 부착 → globalLight(글로벌 Light2D) + weather(WeatherData) 지정.
/// </summary>
[DisallowMultipleComponent]
public class DayNightLightDriver : MonoBehaviour
{
    [Header("타겟")]
    [Tooltip("앰비언트로 쓰는 글로벌 Light2D. 비우면 씬에서 자동 탐색.")]
    public Light2D globalLight;
    [Tooltip("낮/밤 조명 파라미터 SO (Assets/Settings/WeatherData)")]
    public WeatherData weather;

    [Header("사이클")]
    [Tooltip("낮+밤 1주기 길이(초). 0이면 자동진행 off(수동 phase01만).")]
    public float cycleSeconds = 600f;
    [Tooltip("새벽/황혼 전환 비율(주기 중 전환에 쓰는 폭, 0.02~0.49)")]
    [Range(0.02f, 0.49f)] public float transitionRatio = 0.15f;
    [Tooltip("시작 위상 (0=낮 시작, 0.5=밤 중심)")]
    [Range(0f, 1f)] public float startCycle = 0f;
    public bool autoAdvance = true;

    [Header("현재 상태 (0=낮 → 1=밤)")]
    [Range(0f, 1f)] public float phase01;

    float clock;

    void Reset() { AutoFind(); }
    void OnValidate() { if (globalLight == null) AutoFind(); }

    void AutoFind()
    {
        if (globalLight != null) return;
        foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (l.lightType == Light2D.LightType.Global) { globalLight = l; break; }
        }
    }

    void OnEnable()
    {
        if (globalLight == null) AutoFind();
        clock = Mathf.Clamp01(startCycle) * Mathf.Max(0.01f, cycleSeconds);
        phase01 = ComputePhase(clock);
        Apply(phase01);
    }

    void Update()
    {
        if (autoAdvance && cycleSeconds > 0.01f)
        {
            clock += Time.deltaTime;
            if (clock >= cycleSeconds) clock -= cycleSeconds;
            phase01 = ComputePhase(clock);
        }
        Apply(phase01);
    }

    /// 클럭(초) → 위상(0 낮 ~ 1 밤). 밤은 주기 중앙(u=0.5)에 위치, 전환만 부드럽게.
    float ComputePhase(float c)
    {
        float u = Mathf.Repeat(c / Mathf.Max(0.01f, cycleSeconds), 1f); // 0..1
        float tr = Mathf.Clamp(transitionRatio, 0.02f, 0.49f);
        if (u < 0.5f)
        {
            float a = 0.5f - tr;                  // 낮 hold 끝
            if (u <= a) return 0f;
            return Mathf.SmoothStep(0f, 1f, (u - a) / tr);   // 황혼: 낮→밤
        }
        else
        {
            float b = 0.5f + tr;                  // 밤 hold 끝
            if (u <= b) return 1f;
            return 1f - Mathf.SmoothStep(0f, 1f, (u - b) / tr); // 새벽: 밤→낮
        }
    }

    void Apply(float p)
    {
        if (globalLight == null || weather == null) return;
        globalLight.intensity = Mathf.Lerp(weather.dayAmbientIntensity, weather.nightAmbientIntensity, p);
        globalLight.color = Color.Lerp(weather.dayAmbientColor, weather.nightAmbientColor, p);
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/TopDown/맵/낮밤 라이트 드라이버 부착")]
    static void AddToScene()
    {
        var go = new GameObject("DayNightLightDriver");
        var d = go.AddComponent<DayNightLightDriver>();
        d.AutoFind();
        var w = UnityEditor.AssetDatabase.LoadAssetAtPath<WeatherData>("Assets/Settings/WeatherData.asset");
        if (w != null) d.weather = w;
        UnityEditor.Selection.activeGameObject = go;
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Add DayNightLightDriver");
    }
#endif
}
