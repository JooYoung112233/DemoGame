using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] float attackDamage = 25f;
    [SerializeField] float attackRange = 2f;
    [SerializeField] float attackCooldown = 0.8f;
    [SerializeField] float attackAngle = 90f;

    [Header("References")]
    [SerializeField] SkeletonAnimController animController;
    [SerializeField] PlayerController playerController;

    float attackTimer;
    bool isAttacking;
    Health health;

    void Awake()
    {
        health = GetComponent<Health>();
        if (health != null)
        {
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
        }
    }

    void Update()
    {
        if (health != null && health.IsDead) return;

        attackTimer -= Time.deltaTime;

        // 방향 설정
        if (animController != null && playerController != null)
            animController.SetDirection(playerController.FacingDirection);

        if (Input.GetMouseButtonDown(0) && attackTimer <= 0 && !isAttacking)
        {
            DoAttack();
        }

        // 공격 중이 아니면 이동 애니메이션
        if (!isAttacking && animController != null)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            bool moving = Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f;
            animController.Play(moving ? "walk" : "idle");
        }
    }

    void DoAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        animController?.PlayOneShot("attack", () => { isAttacking = false; });

        // 부채꼴 범위 내 적 탐색
        Vector3 facing = playerController != null ? playerController.FacingDirection : transform.forward;
        var colliders = Physics.OverlapSphere(transform.position, attackRange);

        foreach (var col in colliders)
        {
            if (col.transform == transform) continue;
            if (col.transform.IsChildOf(transform)) continue;

            var enemyHealth = col.GetComponentInParent<Health>();
            var enemyAI = col.GetComponentInParent<EnemyAI>();
            if (enemyHealth == null || enemyAI == null) continue;

            Vector3 toEnemy = (col.transform.position - transform.position);
            toEnemy.y = 0;
            float angle = Vector3.Angle(facing, toEnemy);
            if (angle <= attackAngle * 0.5f)
                enemyHealth.TakeDamage(attackDamage);
        }
    }

    void OnDamaged(float amount)
    {
        if (animController != null && !isAttacking)
            animController.PlayOneShot("gethit");
    }

    void OnDeath()
    {
        animController?.PlayOneShot("death");
        if (playerController != null) playerController.enabled = false;
    }
}
