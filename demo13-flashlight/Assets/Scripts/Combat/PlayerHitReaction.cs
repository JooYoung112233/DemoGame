using UnityEngine;

/// <summary>
/// 플레이어 피격 화면 연출 — 위험 비례.
/// 평소(체력 여유): 비네트 붉은 펄스 + 약한 셰이크(절제).
/// 저체력/부상(출혈·골절 등): 빨강 풀스크린 플래시 + 색수차 펄스 + 강한 셰이크(풀세트).
/// 화면 전체 연출은 *플레이어 피격에만* (적 피격은 엔티티 연출만).
/// TopDownPlayer.Awake가 자동 부착. 전역 연출은 ScreenEffectManager(GameBootstrap 싱글톤) 사용.
/// </summary>
[RequireComponent(typeof(Health))]
public class PlayerHitReaction : MonoBehaviour
{
    [Header("위험 판정")]
    [Tooltip("이 체력 비율 미만이면 위험(풀세트)")]
    [SerializeField] float lowHpThreshold = 0.35f;

    [Header("평소 (절제)")]
    [SerializeField] float calmVignette    = 0.32f;
    [SerializeField] float calmVignetteDur = 0.40f;
    [SerializeField] float calmShake       = 0.05f;
    [SerializeField] float calmShakeDur    = 0.14f;

    [Header("위험 (풀세트)")]
    [SerializeField] Color dangerFlashColor   = new Color(0.5f, 0f, 0f, 1f);
    [SerializeField] float dangerFlashDur     = 0.20f;
    [SerializeField] float dangerVignette     = 0.50f;
    [SerializeField] float dangerVignetteDur  = 0.50f;
    [SerializeField] float dangerChromatic    = 0.80f;
    [SerializeField] float dangerChromaticDur = 0.50f;
    [SerializeField] float dangerShake        = 0.16f;
    [SerializeField] float dangerShakeDur     = 0.25f;

    static readonly Color RedVignette = new Color(0.6f, 0f, 0f, 1f);

    Health              _health;
    TopDownPlayer       _player;

    void Awake()
    {
        _health  = GetComponent<Health>();
        _player  = GetComponent<TopDownPlayer>();
    }

    void OnEnable()  { if (_health != null) _health.OnDamaged += OnDamaged; }
    void OnDisable() { if (_health != null) _health.OnDamaged -= OnDamaged; }

    void OnDamaged(float amount)
    {
        if (_player != null && _player.IsInvincible) return;
        if (_health != null && _health.IsDead) return;

        var sem = ScreenEffectManager.Instance;

        bool danger = _health != null && _health.Percent < lowHpThreshold;

        if (danger)
        {
            if (sem != null)
            {
                sem.Flash(dangerFlashColor, dangerFlashDur);
                sem.VignettePulse(dangerVignette, dangerVignetteDur, RedVignette);
                sem.ChromaticPulse(dangerChromatic, dangerChromaticDur);
                sem.ScreenShake(dangerShake, dangerShakeDur);   // CameraFollow로 위임됨
            }
            else CameraFollow.Instance?.Shake(dangerShake, dangerShakeDur);
        }
        else
        {
            if (sem != null)
            {
                sem.VignettePulse(calmVignette, calmVignetteDur, RedVignette);
                sem.ScreenShake(calmShake, calmShakeDur);
            }
            else CameraFollow.Instance?.Shake(calmShake, calmShakeDur);
        }
    }
}
