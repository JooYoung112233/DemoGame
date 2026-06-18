using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 생존 스탯 — 수분(Water) / 포만감(Satiety). 각 0~100, 시작 100.
/// 레이드(지역 활성) 중에만 시간에 따라 감소(안전구역=정지).
/// 둘 중 하나라도 0이면 페널티: 서서히 HP 감소. 회복: 음식=포만감, 음료/수면=조정.
/// 플레이어 GO에 부착(없으면 자동 추가). SaveManager가 영속화.
///
/// 기본 차감 속도(튜닝 가능): 수분 ≈ 18분, 포만감 ≈ 36분(레이드 실시간)에 0.
/// (기획 의도 "수분 6h / 포만감 12h(게임시간)"의 그레이박스 매핑 — 추후 GameTuning 외부화.)
/// </summary>
public class SurvivalStats : MonoBehaviour
{
    public const float Max = 100f;

    [SerializeField] float water = 100f;
    [SerializeField] float satiety = 100f;

    [Header("레이드 중 초당 감소량")]
    [SerializeField] float waterDrainPerSec = 100f / (18f * 60f);    // 수분 ≈18분에 0
    [SerializeField] float satietyDrainPerSec = 100f / (36f * 60f);  // 포만감 ≈36분에 0

    [Header("0일 때 페널티 (빈 스탯 1개당 초당 HP 감소)")]
    [SerializeField] float starveHpPerSec = 0.6f;

    public float Water => water;
    public float Satiety => satiety;
    public float WaterPercent => Mathf.Clamp01(water / Max);
    public float SatietyPercent => Mathf.Clamp01(satiety / Max);
    public bool IsCritical => water <= 0f || satiety <= 0f;
    public bool IsLow => water <= 20f || satiety <= 20f;

    Health _health;

    void Awake() { _health = GetComponent<Health>(); }

    void Update()
    {
        if (!InRaid()) return;

        float dt = Time.deltaTime;
        water = Mathf.Max(0f, water - waterDrainPerSec * dt);
        satiety = Mathf.Max(0f, satiety - satietyDrainPerSec * dt);

        int empty = (water <= 0f ? 1 : 0) + (satiety <= 0f ? 1 : 0);
        if (empty > 0 && _health != null && !_health.IsDead)
            _health.TakeDamage(starveHpPerSec * empty * dt, null, true);  // silent(DoT)
    }

    static bool InRaid()
    {
        return RegionTimeManager.Instance != null
            && !string.IsNullOrEmpty(RegionTimeManager.Instance.ActiveRegionId);
    }

    public void AddWater(float v)   { water = Mathf.Clamp(water + v, 0f, Max); }
    public void AddSatiety(float v) { satiety = Mathf.Clamp(satiety + v, 0f, Max); }

    /// <summary>수면 등 — 수분/포만감 차감(0 미만 클램프).</summary>
    public void Consume(float waterCost, float satietyCost)
    {
        water = Mathf.Max(0f, water - waterCost);
        satiety = Mathf.Max(0f, satiety - satietyCost);
    }

    // ── 자동 부착 ──────────────────────────────────────────
    /// <summary>플레이어의 SurvivalStats 반환(없으면 부착). 씬 로드/세이브/UI에서 사용.</summary>
    public static SurvivalStats Get()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return null;
        var s = go.GetComponent<SurvivalStats>();
        if (s == null) s = go.AddComponent<SurvivalStats>();
        return s;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureHook()
    {
        // 플레이어가 로드되면 컴포넌트가 항상 존재하도록(레이드 중 차감이 UI와 무관하게 돌도록).
        SceneManager.sceneLoaded += (scene, mode) => Get();
    }

    // ── 세이브/로드 ────────────────────────────────────────
    public SurvivalSaveData GetSaveData() => new SurvivalSaveData { water = water, satiety = satiety };

    public void LoadSaveData(SurvivalSaveData d)
    {
        if (d == null) return;
        water = Mathf.Clamp(d.water, 0f, Max);
        satiety = Mathf.Clamp(d.satiety, 0f, Max);
    }
}

[System.Serializable]
public class SurvivalSaveData
{
    public float water = 100f;
    public float satiety = 100f;
}
