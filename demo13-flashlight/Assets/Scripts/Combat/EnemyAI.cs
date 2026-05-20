using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    enum State { Patrol, Chase, Attack, Hit, Dead }

    [Header("Detection")]
    [SerializeField] float detectRange = 8f;
    [SerializeField] float attackRange = 1.5f;
    [SerializeField] float loseRange = 12f;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 2.5f;
    [SerializeField] float patrolSpeed = 1.2f;
    [SerializeField] float patrolRadius = 5f;
    [SerializeField] float patrolWaitTime = 2f;

    [Header("Combat")]
    [SerializeField] float attackDamage = 15f;
    [SerializeField] float attackCooldown = 1.5f;

    [Header("References")]
    [SerializeField] IsometricSpriteAnimator animator;

    State state = State.Patrol;
    Transform player;
    Health health;
    Health playerHealth;
    CharacterController cc;

    Vector3 spawnPos;
    Vector3 patrolTarget;
    float patrolTimer;
    float attackTimer;
    float hitTimer;

    void Awake()
    {
        health = GetComponent<Health>();
        cc = GetComponent<CharacterController>();
    }

    void Start()
    {
        spawnPos = transform.position;
        patrolTarget = GetRandomPatrolPoint();

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            playerHealth = playerGO.GetComponent<Health>();
        }

        if (health != null)
        {
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
        }
    }

    void Update()
    {
        if (state == State.Dead) return;

        attackTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase: UpdateChase(); break;
            case State.Attack: UpdateAttack(); break;
            case State.Hit: UpdateHit(); break;
        }
    }

    void UpdatePatrol()
    {
        if (player != null && DistToPlayer() < detectRange)
        {
            state = State.Chase;
            animator?.Play("walk");
            return;
        }

        Vector3 dir = patrolTarget - transform.position;
        dir.y = 0;

        if (dir.magnitude < 0.5f)
        {
            patrolTimer += Time.deltaTime;
            animator?.Play("idle");
            animator?.SetDirection(dir.magnitude > 0.01f ? dir : Vector3.forward);

            if (patrolTimer >= patrolWaitTime)
            {
                patrolTarget = GetRandomPatrolPoint();
                patrolTimer = 0f;
            }
        }
        else
        {
            Move(dir.normalized, patrolSpeed);
            animator?.Play("walk");
            animator?.SetDirection(dir.normalized);
        }
    }

    void UpdateChase()
    {
        if (player == null || playerHealth != null && playerHealth.IsDead)
        {
            state = State.Patrol;
            animator?.Play("idle");
            return;
        }

        float dist = DistToPlayer();

        if (dist > loseRange)
        {
            state = State.Patrol;
            patrolTarget = GetRandomPatrolPoint();
            animator?.Play("idle");
            return;
        }

        if (dist <= attackRange && attackTimer <= 0)
        {
            state = State.Attack;
            DoAttack();
            return;
        }

        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        dir.Normalize();

        Move(dir, moveSpeed);
        animator?.Play("walk");
        animator?.SetDirection(dir);
    }

    void UpdateAttack()
    {
        if (animator != null && animator.IsAnimComplete)
        {
            state = State.Chase;
            animator?.Play("walk");
        }
    }

    void UpdateHit()
    {
        hitTimer -= Time.deltaTime;
        if (hitTimer <= 0 || (animator != null && animator.IsAnimComplete))
        {
            state = State.Chase;
            animator?.Play("walk");
        }
    }

    void DoAttack()
    {
        attackTimer = attackCooldown;
        animator?.PlayOneShot("attack", () =>
        {
            // 공격 애니메이션 중간에 데미지 적용
            if (player != null && DistToPlayer() <= attackRange * 1.5f)
                playerHealth?.TakeDamage(attackDamage);
        });

        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        animator?.SetDirection(dir.normalized);
    }

    void OnDamaged(float amount)
    {
        if (state == State.Dead) return;
        state = State.Hit;
        hitTimer = 0.3f;
        animator?.PlayOneShot("gethit");

        // 피격 시 플레이어 방향으로 전환
        if (player != null)
        {
            Vector3 dir = (player.position - transform.position);
            dir.y = 0;
            animator?.SetDirection(dir.normalized);

            // 아직 감지 못했으면 추적 시작
            state = State.Chase;
        }
    }

    void OnDeath()
    {
        state = State.Dead;
        animator?.PlayOneShot("death");
        if (cc != null) cc.enabled = false;

        // 3초 후 오브젝트 제거
        Destroy(gameObject, 3f);
    }

    void Move(Vector3 dir, float speed)
    {
        Vector3 move = dir * speed * Time.deltaTime;
        if (cc != null)
            cc.Move(move);
        else
            transform.position += move;
    }

    float DistToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector3.Distance(transform.position, player.position);
    }

    Vector3 GetRandomPatrolPoint()
    {
        Vector2 rnd = Random.insideUnitCircle * patrolRadius;
        return spawnPos + new Vector3(rnd.x, 0, rnd.y);
    }
}
