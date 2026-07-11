using UnityEngine;

/// <summary>
/// 플레이어 소음 발생기. (docs/combat.md 소음 시스템)
///   • 매 프레임 이동 상태(웅크림/걷기/달리기/정지)로 지속 소음 반경 계산 → NoiseSystem에 등록.
///     반경 = GameTuning 행동별 값 × 특성 `move_noise`(고양이걸음 −30% / 무거운발 +25%).
///   • 타격 적중·문·돌 착탄 등 순간 펄스는 `PlayerNoise.Pulse(...)` 정적 헬퍼.
/// 시각화는 **HUD 귀 아이콘(NoiseHUD)만** — 월드 원형 VFX(발밑 링·파문)는 제거(2026-07-11 사용자 요청).
/// 자가 부트스트랩(부팅 시 생성, DontDestroyOnLoad). 안전가옥에선 무음(면제).
/// </summary>
public class PlayerNoise : MonoBehaviour
{
    public static PlayerNoise Instance { get; private set; }

    TopDownPlayer player;
    float _lastPulseRadius;   // UI 레벨에 잠깐 반영할 최근 펄스
    float _pulseFade;

    /// <summary>UI용 소음 레벨(0~1) = max(지속, 최근 펄스) / noiseUiMax.</summary>
    public float Level01
    {
        get
        {
            var gt = GameTuning.Instance;
            float uiMax = gt != null ? gt.noiseUiMax : 14f;
            float r = Mathf.Max(NoiseSystem.PlayerSustainedRadius, _lastPulseRadius * _pulseFade);
            return uiMax > 0f ? Mathf.Clamp01(r / uiMax) : 0f;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;
        if (Instance != null) return;
        var go = new GameObject("[PlayerNoise]");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<PlayerNoise>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (player == null) player = TopDownPlayer.Instance;
        var gt = GameTuning.Instance;

        // 안전가옥 허브는 무음(무게 페널티와 동일 면제).
        bool active = player != null && (UIManager.Instance == null || !UIManager.Instance.IsSafehouse);

        float target = 0f;
        if (active)
        {
            float baseR;
            if (!player.IsMoving)        baseR = gt != null ? gt.noiseIdle   : 0f;
            else if (player.IsCrouching) baseR = gt != null ? gt.noiseCrouch : 1.5f;
            else if (player.IsSprinting) baseR = gt != null ? gt.noiseRun    : 11f;
            else                         baseR = gt != null ? gt.noiseWalk   : 5f;
            target = baseR * TraitManager.Mod("move_noise");
        }

        NoiseSystem.SetPlayerSustained(player != null ? (Vector2)player.transform.position : Vector2.zero, target);

        // 펄스 UI 페이드
        if (_pulseFade > 0f)
        {
            float dur = gt != null ? gt.noisePulseDuration : 0.6f;
            _pulseFade -= Time.deltaTime / Mathf.Max(0.1f, dur);
            if (_pulseFade < 0f) _pulseFade = 0f;
        }
    }

    /// <summary>순간 소음 펄스 — 타격 적중/문/돌 착탄 등. NoiseSystem 등록 + HUD 레벨 반영(월드 VFX 없음).</summary>
    public static void Pulse(Vector2 pos, float radius)
    {
        if (radius <= 0f) return;
        var gt = GameTuning.Instance;
        NoiseSystem.ReportPulse(pos, radius, gt != null ? gt.noisePulseDuration : 0.6f);
        if (Instance != null) { Instance._lastPulseRadius = radius; Instance._pulseFade = 1f; }
    }

    /// <summary>타격 소음(플레이어 위치) — F1 테스트/직접 호출용. (실전 타격 소음은 AttackPerformer 적중에서 Pulse.)</summary>
    public static void AttackNoise()
    {
        var p = TopDownPlayer.Instance; var gt = GameTuning.Instance;
        if (p == null) return;
        Pulse(p.transform.position, (gt != null ? gt.noiseAttack : 14f) * TraitManager.Mod("move_noise"));
    }

    /// <summary>문/셔터 소음(문 위치).</summary>
    public static void DoorNoise(Vector2 pos)
    {
        var gt = GameTuning.Instance;
        Pulse(pos, gt != null ? gt.noiseDoor : 8f);
    }
}
