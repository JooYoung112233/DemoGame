using UnityEngine;

/// <summary>
/// 건물 셰이더(CityBuilding)의 Window Glow를 낮/밤에 따라 제어.
/// 밤: _GlowIntensity → nightIntensity (켜짐)
/// 낮: _GlowIntensity → 0 (꺼짐)
/// DayNightCycle 자동 탐색. 페이드 전환.
/// </summary>
public class BuildingGlow : MonoBehaviour
{
    [Header("Glow Settings")]
    [SerializeField] float nightIntensity = 1.5f;
    [SerializeField] float fadeSpeed = 3f;

    [Header("Renderers (비우면 자식 자동 수집)")]
    [SerializeField] MeshRenderer[] buildingRenderers;

    DayNightCycle dayNight;
    MaterialPropertyBlock propBlock;
    float targetIntensity;
    float currentIntensity;

    static readonly int GlowIntensityID = Shader.PropertyToID("_GlowIntensity");

    void Start()
    {
        propBlock = new MaterialPropertyBlock();

        // 렌더러 미지정 시 자식에서 자동 수집
        if (buildingRenderers == null || buildingRenderers.Length == 0)
            buildingRenderers = GetComponentsInChildren<MeshRenderer>();

        // DayNightCycle 자동 탐색 + 이벤트만 등록
        dayNight = FindFirstObjectByType<DayNightCycle>();

        if (dayNight != null)
        {
            dayNight.OnPhaseChanged += OnPhaseChanged;
            // 에디터 설정 유지 — 즉시 적용하지 않고 타겟만 설정
            // Update에서 fadeSpeed로 자연스럽게 전환
            targetIntensity = dayNight.IsNight ? nightIntensity : 0f;
        }
        else
        {
            targetIntensity = nightIntensity;
        }

        // 낮 시작이면 glow 즉시 꺼짐, 밤이면 즉시 켜짐
        currentIntensity = targetIntensity;
        ApplyGlow();
    }

    void OnDestroy()
    {
        if (dayNight != null)
            dayNight.OnPhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(bool isNight)
    {
        targetIntensity = isNight ? nightIntensity : 0f;
    }

    void Update()
    {
        if (Mathf.Approximately(currentIntensity, targetIntensity)) return;

        currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, fadeSpeed * Time.deltaTime);
        ApplyGlow();
    }

    void ApplyGlow()
    {
        foreach (var r in buildingRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(propBlock);
            propBlock.SetFloat(GlowIntensityID, currentIntensity);
            r.SetPropertyBlock(propBlock);
        }
    }
}
