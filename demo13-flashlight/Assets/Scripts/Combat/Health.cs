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

    void Awake()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHp = Mathf.Max(0, currentHp - amount);
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
}
