using UnityEngine;

/// <summary>
/// 플레이어 전투. 크로스헤어로 적을 타겟팅해야 공격 가능.
/// CombatData가 연결되면 실시간으로 스탯 반영.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack (CombatData 없을 때 기본값)")]
    [SerializeField] float attackDamage = 25f;
    [SerializeField] float attackRange = 3f;
    [SerializeField] float attackSpeed = 1.2f; // 초당 공격 횟수

    [Header("References")]
    [SerializeField] SkeletonAnimController animController;
    [SerializeField] PlayerVisual playerVisual;
    [SerializeField] PlayerController playerController;
    [SerializeField] CrosshairUI crosshairUI;
    [SerializeField] CombatData combatData;

    float attackTimer;
    bool isAttacking;
    Health health;

    // CombatData 우선, 없으면 기본값
    float Damage => combatData != null ? combatData.player.attackDamage : attackDamage;
    float Range => combatData != null ? combatData.player.attackRange : attackRange;
    float Speed => combatData != null ? combatData.player.attackSpeed : attackSpeed;
    float Cooldown => 1f / Mathf.Max(Speed, 0.1f);

    public float GetAttackRange() => Range;

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
        if (playerController != null)
        {
            animController?.SetDirection(playerController.FacingDirection);
            playerVisual?.SetDirection(playerController.FacingDirection);
        }

        // 좌클릭 공격 — 크로스헤어 타겟 필수
        if (Input.GetMouseButtonDown(0) && attackTimer <= 0 && !isAttacking)
        {
            if (crosshairUI != null &&
                crosshairUI.TargetedEnemy != null &&
                crosshairUI.IsTargetInRange)
            {
                DoAttack(crosshairUI.TargetedEnemy);
            }
        }

        // 공격 중이 아니면 이동 애니메이션
        if (!isAttacking)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            bool moving = Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f;
            string anim = moving ? "walk" : "idle";
            animController?.Play(anim);
            playerVisual?.Play(anim);
        }
    }

    void DoAttack(EnemyAI target)
    {
        isAttacking = true;
        attackTimer = Cooldown;

        // 타겟 방향으로 회전
        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0;
        if (toTarget.sqrMagnitude > 0.01f)
        {
            animController?.SetDirection(toTarget.normalized);
            playerVisual?.SetDirection(toTarget.normalized);
        }

        // 데미지 즉시 적용 (타겟은 이미 사거리 확인됨)
        var enemyHealth = target.GetComponent<Health>();
        if (enemyHealth != null && !enemyHealth.IsDead)
            enemyHealth.TakeDamage(Damage);

        // 공격 애니메이션
        System.Action onDone = () => { isAttacking = false; };
        if (animController != null)
            animController.PlayOneShot("attack", onDone);
        else if (playerVisual != null)
            playerVisual.PlayOneShot("attack", onDone);
        else
            isAttacking = false;
    }

    void OnDamaged(float amount)
    {
        if (!isAttacking)
        {
            animController?.PlayOneShot("gethit");
            playerVisual?.PlayOneShot("gethit");
        }
    }

    void OnDeath()
    {
        animController?.PlayOneShot("death");
        playerVisual?.PlayOneShot("death");
        if (playerController != null) playerController.enabled = false;
    }
}
