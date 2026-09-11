using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 프롭(램프·창문·네온 등)에 붙는 동적 발광 보조. Prop2D 카탈로그(Prop2DBuilder)가 부착한다.
/// 실제 빛은 같은 GameObject의 <see cref="Light2D"/>(Sprite 쿠키)가 낸다.
///
/// nightOnly면 <see cref="DayNightCycle"/>의 OnPhaseChanged에 맞춰 밤에만 켜진다(기존 이벤트 패턴).
/// DayNightCycle이 없으면 항상 켜진 상태 유지.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class PropLight2D : MonoBehaviour
{
    [Tooltip("켜면 밤에만 발광(낮엔 꺼짐). DayNightCycle 연동.")]
    public bool nightOnly = false;

    Light2D _light;
    DayNightCycle _dayNight;

    void Awake() => _light = GetComponent<Light2D>();

    void OnEnable()
    {
        if (!nightOnly) return;
        _dayNight = FindFirstObjectByType<DayNightCycle>();
        if (_dayNight != null)
        {
            _dayNight.OnPhaseChanged += Apply;
            Apply(_dayNight.IsNight);
        }
    }

    void OnDisable()
    {
        if (_dayNight != null) _dayNight.OnPhaseChanged -= Apply;
    }

    void Apply(bool isNight)
    {
        if (_light != null) _light.enabled = !nightOnly || isNight;
    }
}
