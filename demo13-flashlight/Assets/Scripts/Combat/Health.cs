using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] float maxHp = 100f;
    float currentHp;
    bool isDead;

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

        // 플레이어 무적 체크 (구르기 중) — silent 데미지는 무적 무시 안 함
        if (!silent)
        {
            // TODO(TopDownPlayer): var playerCombat = GetComponent<PlayerController>();
            // TODO(TopDownPlayer): if (playerCombat != null && playerCombat.IsInvincible) return;
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
