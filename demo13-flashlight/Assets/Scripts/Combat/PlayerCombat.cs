using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] float attackDamage = 25f;
    [SerializeField] float attackRange = 2f;
    [SerializeField] float attackCooldown = 0.8f;
    [SerializeField] float attackAngle = 90f;

    [Header("References")]
    [SerializeField] IsometricSpriteAnimator animator;
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

        // 이동 방향으로 스프라이트 방향 설정
        if (animator != null && playerController != null)
            animator.SetDirection(playerController.FacingDirection);

        if (Input.GetMouseButtonDown(0) && attackTimer <= 0 && !isAttacking)
        {
            DoAttack();
        }

        // 공격 중이 아니면 이동 애니메이션
        if (!isAttacking && animator != null)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            bool moving = Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f;
            animator.Play(moving ? "walk" : "idle");
        }
    }

    void DoAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        animator?.PlayOneShot("attack", () => { isAttacking = false; });

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

            // 각도 체크
            Vector3 toEnemy = (col.transform.position - transform.position);
            toEnemy.y = 0;
            float angle = Vector3.Angle(facing, toEnemy);
            if (angle <= attackAngle * 0.5f)
            {
                enemyHealth.TakeDamage(attackDamage);
            }
        }
    }

    void OnDamaged(float amount)
    {
        if (animator != null && !isAttacking)
            animator.PlayOneShot("gethit", () => { });
    }

    void OnDeath()
    {
        animator?.PlayOneShot("death");
        if (playerController != null) playerController.enabled = false;
    }
}
