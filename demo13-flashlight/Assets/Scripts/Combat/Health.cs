using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] float maxHp = 100f;
    float currentHp;
    bool isDead;

    /// <summary>피격 무적창 길이(초) — 플레이어 전용. 다중 피격이 한 프레임에 겹치는 것만 막는 짧은 창.</summary>
    const float HurtIFrame = 0.35f;
    float _hurtIFrameUntil;

    // 플레이어 한정 특성 게이팅용(적 Health엔 null). 같은 GO에 TopDownPlayer 존재 = 플레이어.
    TopDownPlayer _tp;
    // 불굴(low_hp_damage_taken) 발동 체력 비율 — placeholder(→ GameTuning 이관 가능).
    const float LowHpThreshold = 0.3f;

    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public float Percent => currentHp / maxHp;
    public bool IsDead => isDead;

    public event System.Action<float> OnDamaged;   // damage amount
    public event System.Action OnDeath;
    /// <summary>OnDeath의 별칭 — 코드 가독성용</summary>
    public event System.Action OnDied { add => OnDeath += value; remove => OnDeath -= value; }

    void Awake()
    {
        currentHp = maxHp;
        _tp = GetComponent<TopDownPlayer>();
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null, false);
    }

    /// <summary>
    /// 데미지 적용.
    /// </summary>
    /// <param name="amount">데미지량</param>
    /// <param name="source">데미지 원인 (null 가능)</param>
    /// <param name="silent">true면 OnDamaged 이벤트 미발행 (출혈 등 DoT용)</param>
    public void TakeDamage(float amount, GameObject source, bool silent)
    {
        if (isDead) return;

        // 플레이어 무적 체크 (구르기 중) — silent 데미지(DoT)는 무적 무시
        if (!silent && _tp != null && _tp.IsInvincible) return;

        // 2026-07-11: **피격 무적창(i-frame)** — 플레이어 한정.
        //   예전엔 피격 후 무적이 전혀 없어(구르기 중만 무적) 적 여럿에게 같은 순간 겹쳐 맞고
        //   "맞는지도 모르게 갈리는" 느낌이 났다. 짧은 창으로 다중 피격만 걸러낸다(DoT는 예외).
        if (!silent && _tp != null && amount > 0f)
        {
            if (Time.time < _hurtIFrameUntil) return;
            _hurtIFrameUntil = Time.time + HurtIFrame;
        }

        // 플레이어 한정 특성 — 받는 피해 보정(유리 어깨 +0.15 / 불굴: 저체력 시 −0.15)
        if (_tp != null && amount > 0f)
        {
            amount *= TraitManager.Mod("damage_taken");
            if (Percent <= LowHpThreshold)
                amount *= TraitManager.Mod("low_hp_damage_taken");
        }

        currentHp = Mathf.Max(0, currentHp - amount);

        if (!silent)
            OnDamaged?.Invoke(amount);

        if (currentHp <= 0)
        {
            isDead = true;
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHp = Mathf.Min(currentHp + amount, maxHp);
    }

    public void FullHeal()
    {
        isDead = false;
        currentHp = maxHp;
    }

    public void SetMaxHp(float newMax)
    {
        maxHp = Mathf.Max(1f, newMax);
        currentHp = Mathf.Min(currentHp, maxHp);
    }
}
