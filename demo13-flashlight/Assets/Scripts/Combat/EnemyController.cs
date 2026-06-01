using UnityEngine;

/// <summary>
/// 탑다운 2D 적 컨트롤러.
/// Rigidbody2D 기반 이동 + 상태머신 (Patrol/Chase/AttackWindup/Attack/Hit/Stunned/Dead).
/// NavMesh 없음. StatDB에서 스탯 로드.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyController : MonoBehaviour
{
    // ─────────────────────── 열거형 ────────────────────────────────
    public enum State { Patrol, Chase, AttackWindup, Attack, Hit, Stunned, Dead }

    // ─────────────────────── 인스펙터 ──────────────────────────────
    [Header("감지")]
    [SerializeField] float detectRange  = 8f;
    [SerializeField] float attackRange  = 1.2f;
    [SerializeField] float loseRange    = 12f;

    [Header("이동")]
    [SerializeField] float moveSpeed    = 3f;
    [SerializeField] float patrolRadius = 4f;
    [SerializeField] float patrolWaitMin = 1f;
    [SerializeField] float patrolWaitMax = 3f;

    [Header("공격")]
    [SerializeField] float attackDamage  = 10f;
    [SerializeField] float attackWindup  = 0.4f;
    [SerializeField] float attackCooldown = 1.2f;

    [Header("그로기 (누적 → 스턴)")]
    [SerializeField] float maxGroggy     = 100f;
    [SerializeField] float groggyDecay   = 20f;
    [SerializeField] float stunDuration  = 1.8f;

    [Header("StatDB 키 (비어있으면 인스펙터 값 사용)")]
    [SerializeField] string unitKey;

    [Header("비주얼")]
    [SerializeField] SpriteRenderer spriteRenderer;

    // ─────────────────────── 내부 상태 ─────────────────────────────
    Rigidbody2D _rb;
    Health      _health;
    State       _state = State.Patrol;

    Vector2 _patrolTarget;
    float   _patrolWaitTimer;
    bool    _patrolWaiting;

    float _attackTimer;
    float _windupTimer;
    float _stunTimer;
    float _hitTimer;
    float _groggy;

    Transform _playerTransform;

    Color _originalColor;
    float _flashTimer;

    // ─────────────────────── 프로퍼티 ──────────────────────────────
    public State CurrentState => _state;
    public bool  IsDead       => _state == State.Dead;

    // ─────────────────────── Unity ─────────────────────────────────
    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;

        _health = GetComponent<Health>();
        if (_health != null) _health.OnDied += OnDied;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            _originalColor = spriteRenderer.color;
    }

    void Start()
    {
        LoadStats();
        PickPatrolTarget();
    }

    void OnDestroy()
    {
        if (_health != null) _health.OnDied -= OnDied;
    }

    void Update()
    {
        if (_state == State.Dead) return;
        FindPlayer();
        UpdateGroggy();
        UpdateColorFlash();

        switch (_state)
        {
            case State.Patrol:       UpdatePatrol();       break;
            case State.Chase:        UpdateChase();        break;
            case State.AttackWindup: UpdateAttackWindup(); break;
            case State.Attack:       UpdateAttack();       break;
            case State.Hit:          UpdateHit();          break;
            case State.Stunned:      UpdateStunned();      break;
        }
    }

    // ─────────────────────── 상태별 업데이트 ───────────────────────

    void UpdatePatrol()
    {
        if (_playerTransform != null && DistToPlayer() <= detectRange)
        {
            SetState(State.Chase);
            return;
        }

        if (_patrolWaiting)
        {
            _patrolWaitTimer -= Time.deltaTime;
            if (_patrolWaitTimer <= 0f) { _patrolWaiting = false; PickPatrolTarget(); }
            StopMove();
            return;
        }

        if (((Vector2)transform.position - _patrolTarget).magnitude < 0.25f)
        {
            StopMove();
            _patrolWaiting = true;
            _patrolWaitTimer = Random.Range(patrolWaitMin, patrolWaitMax);
        }
        else
        {
            MoveToward(_patrolTarget);
        }
    }

    void UpdateChase()
    {
        if (_playerTransform == null || DistToPlayer() > loseRange)
        {
            SetState(State.Patrol);
            return;
        }
        if (DistToPlayer() <= attackRange && _attackTimer <= 0f)
        {
            SetState(State.AttackWindup);
            return;
        }
        MoveToward(_playerTransform.position);
        _attackTimer -= Time.deltaTime;
    }

    void UpdateAttackWindup()
    {
        StopMove();
        _windupTimer -= Time.deltaTime;
        if (_windupTimer <= 0f) SetState(State.Attack);
    }

    void UpdateAttack()
    {
        StopMove();
        if (_playerTransform != null)
        {
            var h = _playerTransform.GetComponent<Health>();
            if (h != null) h.TakeDamage(attackDamage);
        }
        _attackTimer = attackCooldown;
        SetState(State.Chase);
    }

    void UpdateHit()
    {
        _hitTimer -= Time.deltaTime;
        if (_hitTimer <= 0f) SetState(State.Chase);
    }

    void UpdateStunned()
    {
        StopMove();
        _stunTimer -= Time.deltaTime;
        if (_stunTimer <= 0f) SetState(State.Chase);
    }

    // ─────────────────────── 공개 API ──────────────────────────────

    /// <summary>플레이어 공격이 명중했을 때 호출.</summary>
    public void TakeHit(float damage, float groggyAmount, Vector2 knockbackDir = default)
    {
        if (_state == State.Dead) return;

        if (_health != null) _health.TakeDamage(damage);

        _groggy += groggyAmount;
        if (_groggy >= maxGroggy)
        {
            _groggy = 0f;
            ApplyStun();
            return;
        }

        if (knockbackDir != Vector2.zero)
            _rb.AddForce(knockbackDir.normalized * 4f, ForceMode2D.Impulse);

        FlashRed();
        SetState(State.Hit);
        _hitTimer = 0.15f;
    }

    // ─────────────────────── 내부 ──────────────────────────────────

    void OnDied()
    {
        SetState(State.Dead);
        StopMove();
        _rb.simulated = false;
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.35f, 0.35f, 0.35f);
        Debug.Log($"[Enemy] {gameObject.name} 사망");
    }

    void SetState(State next)
    {
        _state = next;
        if (next == State.AttackWindup) _windupTimer = attackWindup;
        if (next == State.Patrol)       PickPatrolTarget();
    }

    void MoveToward(Vector2 target)
    {
        Vector2 dir = ((Vector2)target - (Vector2)transform.position).normalized;
        _rb.linearVelocity = dir * moveSpeed;
        if (spriteRenderer != null && Mathf.Abs(dir.x) > 0.01f)
            spriteRenderer.flipX = dir.x < 0f;
    }

    void StopMove() => _rb.linearVelocity = Vector2.zero;

    void PickPatrolTarget()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist  = Random.Range(patrolRadius * 0.3f, patrolRadius);
        _patrolTarget = (Vector2)transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
    }

    void FindPlayer()
    {
        if (_playerTransform != null) return;
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) _playerTransform = go.transform;
    }

    float DistToPlayer()
    {
        if (_playerTransform == null) return float.MaxValue;
        return Vector2.Distance(transform.position, _playerTransform.position);
    }

    void LoadStats()
    {
        if (string.IsNullOrEmpty(unitKey)) return;
        var stat = StatDB.Instance?.GetUnit(unitKey);
        if (stat == null) return;

        moveSpeed     = stat.moveSpeed;
        detectRange   = stat.detectionRange;
        attackRange   = stat.attackRange;
        attackDamage  = stat.attackDamage;
        attackCooldown= stat.attackCooldown;
        maxGroggy     = stat.groggyMax;
        groggyDecay   = stat.groggyDecay;
        stunDuration  = stat.stunDuration;

        if (_health != null) { _health.SetMaxHp(stat.maxHp); _health.FullHeal(); }
    }

    void ApplyStun()
    {
        SetState(State.Stunned);
        _stunTimer = stunDuration;
        FlashWhite();
    }

    void UpdateGroggy()
    {
        if (_groggy > 0f)
            _groggy = Mathf.Max(0f, _groggy - groggyDecay * Time.deltaTime);
    }

    void FlashRed()  { if (spriteRenderer) spriteRenderer.color = new Color(1f, 0.3f, 0.3f); _flashTimer = 0.12f; }
    void FlashWhite(){ if (spriteRenderer) spriteRenderer.color = Color.white;                _flashTimer = 0.25f; }

    void UpdateColorFlash()
    {
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f && spriteRenderer != null)
                spriteRenderer.color = _originalColor;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
