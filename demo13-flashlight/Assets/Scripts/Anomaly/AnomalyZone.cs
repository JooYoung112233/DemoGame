using System.Collections.Generic;
using UnityEngine;

/// <summary>짙은현상 구간의 생명주기 상태.</summary>
public enum AnomalyState { Dormant, Telegraph, Active, Waning, Collapsing, Cooldown }

/// <summary>
/// 짙은현상 구간 1개 (Phase 1 기반). 위치+반경으로 구역을 정의하고 생명주기 상태머신을 돌린다.
/// 잠복 → 징조 → 활성 → 쇠퇴(종료 경고) → 붕괴(미회수 증발) → 쿨다운 → 잠복.
/// 스폰/연출/마커 시스템은 이벤트(OnTelegraph/OnActivate/OnWaning/OnCollapse/OnDormant)를 구독.
/// 타이밍은 GameTuning에서 읽음(없으면 기본값). 트리거는 AnomalyManager가 랜덤으로.
/// 설계: docs/anomaly.md
/// </summary>
public class AnomalyZone : MonoBehaviour
{
    [Header("구역")]
    [Tooltip("현상 반경(m). 루트/몬스터 스폰·플레이어 판정 범위.")]
    [SerializeField] float radius = 6f;
    [Tooltip("붕괴 후 다시 잠복까지 쿨다운(초).")]
    [SerializeField] float cooldown = 60f;

    public AnomalyState State { get; private set; } = AnomalyState.Dormant;
    public Vector2 Center => transform.position;
    public float Radius => radius;
    public bool CanTrigger => State == AnomalyState.Dormant;
    /// <summary>루트 회수 가능한 단계(활성+쇠퇴).</summary>
    public bool IsLootablePhase => State == AnomalyState.Active || State == AnomalyState.Waning;

    public float PhaseElapsed { get; private set; }
    public float PhaseDuration { get; private set; }
    /// <summary>활성+쇠퇴가 끝나(붕괴 시작)까지 남은 초 — 경고/HUD용.</summary>
    public float TimeLeft { get; private set; }

    // ── 이벤트 (스폰/연출/마커가 구독) ──
    public event System.Action<AnomalyZone> OnTelegraph;   // 징조 시작 (안개 모임)
    public event System.Action<AnomalyZone> OnActivate;    // 활성 — 스폰 시작
    public event System.Action<AnomalyZone> OnWaning;      // 종료 경고 시작
    public event System.Action<AnomalyZone> OnCollapse;    // 붕괴 — 미회수/몬스터 증발 시작
    public event System.Action<AnomalyZone> OnDormant;     // 잠복 복귀

    // ── 전역 레지스트리 ──
    static readonly List<AnomalyZone> _all = new List<AnomalyZone>();
    public static IReadOnlyList<AnomalyZone> All => _all;

    void OnEnable()
    {
        _all.Add(this);
        // 매니저 없으면 런타임에 자동 보장(존만 배치해도 동작)
        if (Application.isPlaying && AnomalyManager.Instance == null)
            new GameObject("[AnomalyManager]").AddComponent<AnomalyManager>();
    }
    void OnDisable() => _all.Remove(this);

    GameTuning T => GameTuning.Instance;
    float ActiveDur  => T != null ? T.anomalyActiveDuration : 180f;
    float TeleDur    => T != null ? T.anomalyTelegraph : 8f;
    float WarnTime   => T != null ? Mathf.Min(T.anomalyWarning, T.anomalyActiveDuration) : 30f;
    float CollapseDur => T != null ? T.anomalyCollapse : 5f;

    /// <summary>잠복 중일 때 현상 발생 시작(AnomalyManager가 호출).</summary>
    public void Trigger()
    {
        if (CanTrigger) EnterState(AnomalyState.Telegraph);
    }

    void EnterState(AnomalyState s)
    {
        State = s;
        PhaseElapsed = 0f;
        switch (s)
        {
            case AnomalyState.Telegraph:
                PhaseDuration = TeleDur; OnTelegraph?.Invoke(this);
                Debug.Log($"[Anomaly] {name} 징조(텔레그래프)"); break;
            case AnomalyState.Active:
                PhaseDuration = Mathf.Max(0.1f, ActiveDur - WarnTime); OnActivate?.Invoke(this);
                Debug.Log($"[Anomaly] {name} 활성 — 스폰"); break;
            case AnomalyState.Waning:
                PhaseDuration = WarnTime; OnWaning?.Invoke(this);
                Debug.Log($"[Anomaly] {name} 종료 경고"); break;
            case AnomalyState.Collapsing:
                PhaseDuration = CollapseDur; OnCollapse?.Invoke(this);
                Debug.Log($"[Anomaly] {name} 붕괴 — 미회수/몬스터 증발"); break;
            case AnomalyState.Cooldown:
                PhaseDuration = cooldown; break;
            case AnomalyState.Dormant:
                PhaseDuration = 0f; TimeLeft = 0f; OnDormant?.Invoke(this); break;
        }
    }

    void Update()
    {
        if (State == AnomalyState.Dormant) return;
        PhaseElapsed += Time.deltaTime;

        if (State == AnomalyState.Active)      TimeLeft = (PhaseDuration - PhaseElapsed) + WarnTime;
        else if (State == AnomalyState.Waning) TimeLeft = PhaseDuration - PhaseElapsed;

        if (PhaseElapsed < PhaseDuration) return;

        switch (State)
        {
            case AnomalyState.Telegraph:  EnterState(AnomalyState.Active); break;
            case AnomalyState.Active:     EnterState(AnomalyState.Waning); break;
            case AnomalyState.Waning:     EnterState(AnomalyState.Collapsing); break;
            case AnomalyState.Collapsing: EnterState(AnomalyState.Cooldown); break;
            case AnomalyState.Cooldown:   EnterState(AnomalyState.Dormant); break;
        }
    }

    // ── 스폰 배치용 (Phase 2) ──
    public bool Contains(Vector2 p) => ((Vector2)transform.position - p).sqrMagnitude <= radius * radius;

    /// <summary>구역 내 랜덤 점. edgeBias 0=균일, 1=가장자리 쏠림(몬스터 가장자리 등장용).</summary>
    public Vector2 RandomPoint(float edgeBias = 0f)
    {
        float t = Mathf.Lerp(Mathf.Sqrt(Random.value), 1f, Mathf.Clamp01(edgeBias));
        float a = Random.value * Mathf.PI * 2f;
        return (Vector2)transform.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (radius * t);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = GizmoColor();
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    Color GizmoColor()
    {
        switch (State)
        {
            case AnomalyState.Active:     return new Color(0.7f, 0.2f, 0.9f, 0.5f);
            case AnomalyState.Waning:     return new Color(1f, 0.5f, 0.1f, 0.6f);
            case AnomalyState.Collapsing: return new Color(1f, 0.2f, 0.2f, 0.6f);
            case AnomalyState.Telegraph:  return new Color(0.5f, 0.4f, 0.8f, 0.4f);
            default:                      return new Color(0.4f, 0.4f, 0.5f, 0.3f);
        }
    }
#endif
}
