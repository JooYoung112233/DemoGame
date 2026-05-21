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
    [SerializeField] SkeletonAnimController animController;

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
            animController?.Play("walk");
            return;
        }

        Vector3 dir = patrolTarget - transform.position;
        dir.y = 0;

        if (dir.magnitude < 0.5f)
        {
            patrolTimer += Time.deltaTime;
            animController?.Play("idle");

            if (patrolTimer >= patrolWaitTime)
            {
                patrolTarget = GetRandomPatrolPoint();
                patrolTimer = 0f;
            }
        }
        else
        {
            Move(dir.normalized, patrolSpeed);
            animController?.Play("walk");
            animController?.SetDirection(dir.normalized);
        }
    }

    void UpdateChase()
    {
        if (player == null || (playerHealth != null && playerHealth.IsDead))
        {
            state = State.Patrol;
            animController?.Play("idle");
            return;
        }

        float dist = DistToPlayer();

        if (dist > loseRange)
        {
            state = State.Patrol;
            patrolTarget = GetRandomPatrolPoint();
            animController?.Play("idle");
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
        animController?.Play("walk");
        animController?.SetDirection(dir);
    }

    void UpdateAttack()
    {
        if (animController != null && animController.IsAnimComplete)
        {
            state = State.Chase;
            animController?.Play("walk");
        }
    }

    void UpdateHit()
    {
        hitTimer -= Time.deltaTime;
        if (hitTimer <= 0 || (animController != null && animController.IsAnimComplete))
        {
            state = State.Chase;
            animController?.Play("walk");
        }
    }

    void DoAttack()
    {
        attackTimer = attackCooldown;

        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        animController?.SetDirection(dir.normalized);

        animController?.PlayOneShot("attack", () =>
        {
            if (player != null && DistToPlayer() <= attackRange * 1.5f)
                playerHealth?.TakeDamage(attackDamage);
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
        hitTimer = 0.3f;
        animController?.PlayOneShot("gethit");
        state = State.Chase;
    }

    void OnDeath()
    {
        state = State.Dead;
        animController?.PlayOneShot("death");
        if (cc != null) cc.enabled = false;
        Destroy(gameObject, 3f);
    }

    void Move(Vector3 dir, float speed)
    {
        if (cc != null)
            cc.Move(dir * speed * Time.deltaTime);
        else
            transform.position += dir * speed * Time.deltaTime;
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
