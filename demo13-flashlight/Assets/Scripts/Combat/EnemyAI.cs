using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 AI 상태머신 + NavMeshAgent 경로탐색.
/// CombatData 연결 시 실시간 스탯 반영.
/// </summary>
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
    [SerializeField] float attackSpeed = 0.67f;

    [Header("Data")]
    [SerializeField] EnemyData enemyData;
    [SerializeField] CombatData combatData;

    [Header("References")]
    [SerializeField] SkeletonAnimController animController;

    State state = State.Patrol;
    Transform player;
    Health health;
    Health playerHealth;
    NavMeshAgent agent;

    Vector3 spawnPos;
    float patrolTimer;
    float attackTimer;
    float hitTimer;

    // 우선순위: EnemyData > CombatData > 인스펙터 필드값
    float Damage => enemyData != null ? enemyData.attackDamage :
        combatData != null ? combatData.enemy.attackDamage : attackDamage;
    float AtkRange => enemyData != null ? enemyData.attackRange :
        combatData != null ? combatData.enemy.attackRange : attackRange;
    float AtkCooldown => 1f / Mathf.Max(
        enemyData != null ? enemyData.attackSpeed :
        combatData != null ? combatData.enemy.attackSpeed : attackSpeed, 0.1f);
    float DetectRng => enemyData != null ? enemyData.detectRange :
        combatData != null ? combatData.enemy.detectRange : detectRange;
    float LoseRng => enemyData != null ? enemyData.loseRange :
        combatData != null ? combatData.enemy.loseRange : loseRange;
    float MoveSpd => enemyData != null ? enemyData.moveSpeed :
        combatData != null ? combatData.enemy.moveSpeed : moveSpeed;
    float PatrolSpd => enemyData != null ? enemyData.patrolSpeed :
        combatData != null ? combatData.enemy.patrolSpeed : patrolSpeed;
    float PatrolRad => enemyData != null ? enemyData.patrolRadius :
        combatData != null ? combatData.enemy.patrolRadius : patrolRadius;
    float HitStun => enemyData != null ? enemyData.hitStunDuration : 0.3f;
    float PatrolWait => enemyData != null ? enemyData.patrolWaitTime : patrolWaitTime;

    void Awake()
    {
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.speed = MoveSpd;
            agent.acceleration = 50f;
            agent.angularSpeed = 0f;
            agent.stoppingDistance = 0.3f;
        }
    }

    void Start()
    {
        spawnPos = transform.position;

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

        // 첫 순찰 지점
        SetPatrolTarget();
    }

    void Update()
    {
        if (state == State.Dead) return;
        attackTimer -= Time.deltaTime;

        // 이동 방향에 따라 애니메이션 방향 설정
        if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 vel = agent.velocity;
            vel.y = 0;
            if (vel.sqrMagnitude > 0.01f)
                animController?.SetDirection(vel.normalized);
        }

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
        if (player != null && DistToPlayer() < DetectRng)
        {
            state = State.Chase;
            return;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = PatrolSpd;

            // 도착 체크
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                patrolTimer += Time.deltaTime;
                animController?.Play("idle");

                if (patrolTimer >= PatrolWait)
                    SetPatrolTarget();
            }
            else
            {
                animController?.Play("walk");
            }
        }
    }

    void UpdateChase()
    {
        if (player == null || (playerHealth != null && playerHealth.IsDead))
        {
            state = State.Patrol;
            StopAgent();
            animController?.Play("idle");
            return;
        }

        float dist = DistToPlayer();

        if (dist > LoseRng)
        {
            state = State.Patrol;
            SetPatrolTarget();
            return;
        }

        if (dist <= AtkRange && attackTimer <= 0)
        {
            state = State.Attack;
            StopAgent();
            DoAttack();
            return;
        }

        // NavMesh 경로탐색으로 플레이어 추격
        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = MoveSpd;
            agent.stoppingDistance = AtkRange * 0.8f;
            agent.SetDestination(player.position);
        }
        animController?.Play("walk");
    }

    void UpdateAttack()
    {
        if (animController != null && animController.IsAnimComplete)
        {
            state = State.Chase;
        }
    }

    void UpdateHit()
    {
        hitTimer -= Time.deltaTime;
        if (hitTimer <= 0 || (animController != null && animController.IsAnimComplete))
        {
            state = State.Chase;
        }
    }

    void DoAttack()
    {
        attackTimer = AtkCooldown;

        // 플레이어 방향으로 회전
        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        animController?.SetDirection(dir.normalized);

        animController?.PlayOneShot("attack", () =>
        {
            if (player != null && DistToPlayer() <= AtkRange * 1.5f)
                playerHealth?.TakeDamage(Damage);
        });
    }

    void OnDamaged(float amount)
    {
        if (state == State.Dead) return;

        if (player != null)
        {
            Vector3 dir = (player.position - transform.position);
            dir.y = 0;
            animController?.SetDirection(dir.normalized);
        }

        state = State.Hit;
        hitTimer = HitStun;
        StopAgent();
        animController?.PlayOneShot("gethit");
    }

    void OnDeath()
    {
        state = State.Dead;
        StopAgent();
        if (agent != null) agent.enabled = false;
        animController?.PlayOneShot("death");
        Destroy(gameObject, 3f);
    }

    void SetPatrolTarget()
    {
        patrolTimer = 0f;
        if (agent != null && agent.isOnNavMesh)
        {
            Vector2 rnd = Random.insideUnitCircle * PatrolRad;
            Vector3 target = spawnPos + new Vector3(rnd.x, 0, rnd.y);

            // NavMesh 위의 유효 위치로 보정
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, PatrolRad, NavMesh.AllAreas))
            {
                agent.stoppingDistance = 0.3f;
                agent.SetDestination(hit.position);
            }
        }
    }

    void StopAgent()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    float DistToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector3.Distance(transform.position, player.position);
    }
}
