using UnityEngine;

/// <summary>
/// 탑다운 2D 플레이어 — WASD 8방향 이동 + 마우스 조준 + 근접 전투(약/강공격, 구르기) + 스태미너.
/// Rigidbody2D 기반. NavMesh 없음. 스탯은 StatDB.playerStat에서 로드.
/// 게임 로직(인벤/의료/퀘스트 등)은 별도 컴포넌트로 부착.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class TopDownPlayer : MonoBehaviour
{
    // ── 싱글톤 ──────────────────────────────────────────────────────
    public static TopDownPlayer Instance { get; private set; }

    public enum CombatState { Idle, LightAttack, HeavyCharge, HeavyRelease, Dodge, Exhausted }

    // ── 인스펙터 ─────────────────────────────────────────────────────
    [Header("비주얼")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] bool flipByMouse = true;

    [Header("손전등")]
    [SerializeField] Transform flashlightPivot; // 자식 오브젝트 — Light2D 붙음

    [Header("전투")]
    [Tooltip("공격 판정 대상 레이어 (적). 비워두면 EnemyController 컴포넌트로 판별)")]
    [SerializeField] LayerMask enemyMask = ~0;

    [Header("기본값 (StatDB 없을 때)")]
    [SerializeField] float fallbackMoveSpeed = 5f;

    // ── 퍼블릭 API ───────────────────────────────────────────────────
    public Vector2 FacingDirection { get; private set; } = Vector2.down;
    public Vector2 MoveDirection   { get; private set; }
    public Vector3 MouseWorldPos   { get; private set; }
    public bool    IsSprinting     { get; private set; }
    public bool    IsMoving => MoveDirection.sqrMagnitude > 0.01f;
    public bool    CanMove  { get; set; } = true;

    // 전투 상태
    public CombatState CurrentState => _state;
    public bool  CombatEnabled { get; set; } = true;
    public bool  IsInvincible  => _state == CombatState.Dodge && _dodgeInvTimer > 0f;
    public bool  IsAttacking   => _state == CombatState.LightAttack || _state == CombatState.HeavyRelease;
    public float ChargePercent => _state == CombatState.HeavyCharge ? Mathf.Clamp01(_chargeTimer / HeavyMaxCharge) : 0f;

    // 스태미너
    public float StaminaCurrent => _stamina;
    public float StaminaMax     => MaxStam;
    public float StaminaPercent => _stamina / MaxStam;
    public bool  IsExhausted    => _exhausted;

    // ── 스탯 접근 (StatDB.playerStat → 폴백) ─────────────────────────
    PlayerStatData _stat;
    PlayerStatData Stat => _stat ??= (StatDB.Instance != null ? StatDB.Instance.playerStat : null) ?? new PlayerStatData();

    float MoveSpd        => Stat != null ? Stat.moveSpeed : fallbackMoveSpeed;
    float SprintMult     => Stat.sprintSpeedMultiplier;
    float SprintCost     => Stat.sprintStaminaCost;
    float SprintMinStam  => Stat.sprintMinStamina;
    float MaxStam        => Stat.maxStamina;
    float StamRegen      => Stat.staminaRegen;
    float StamRegenDelay => Stat.staminaRegenDelay;
    float ExhaustDur     => Stat.exhaustionDuration;
    float LightRange     => Stat.lightRange;
    float LightCooldown  => Stat.lightCooldown;
    int   ComboMax       => Stat.lightComboMax;
    float ComboWindow    => Stat.lightComboWindow;
    float HeavyRange     => Stat.heavyRange;
    float HeavyChargeTime=> Stat.heavyChargeTime;
    float HeavyMaxCharge => Stat.heavyMaxCharge;
    float HeavyCooldown  => Stat.heavyCooldown;
    float DodgeDist      => Stat.dodgeDistance;
    float DodgeDur       => Stat.dodgeDuration;
    float DodgeInvDur    => Stat.dodgeInvincibleDuration;
    float DodgeCooldown  => Stat.dodgeCooldown;
    float DodgeCost      => Stat.dodgeStaminaCost;

    // ── 내부 상태 ─────────────────────────────────────────────────────
    Rigidbody2D _rb;
    Camera      _cam;
    CombatState _state = CombatState.Idle;

    // 스태미너
    float _stamina;
    float _regenDelayTimer;
    float _exhaustTimer;
    bool  _exhausted;

    // 약공격 콤보
    int   _comboStep;
    float _comboTimer;
    float _lightCooldownTimer;
    float _attackStateTimer;

    // 강공격
    float _chargeTimer;
    float _heavyCooldownTimer;

    // 구르기
    float _dodgeTimer;
    float _dodgeCooldownTimer;
    float _dodgeInvTimer;
    Vector2 _dodgeDir;

    // ── Unity 생명주기 ───────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _cam = Camera.main;

        _stamina = MaxStam;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (_cam == null) _cam = Camera.main;

        UpdateMouseFacing();
        UpdateFlip();
        UpdateFlashlight();

        bool uiOpen = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();
        if (!uiOpen) HandleCombatInput();

        UpdateCombatTimers();
        UpdateStamina();
        UpdateSprint(uiOpen);
    }

    void FixedUpdate()
    {
        // 구르기 중 — 대시 속도 적용
        if (_state == CombatState.Dodge)
        {
            _rb.linearVelocity = _dodgeDir * (DodgeDist / DodgeDur);
            return;
        }

        // 공격/차징 중 — 이동 정지 (제자리 공격)
        if (_state == CombatState.LightAttack || _state == CombatState.HeavyRelease)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        if (!CanMove) { _rb.linearVelocity = Vector2.zero; return; }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        MoveDirection = new Vector2(h, v);
        if (MoveDirection.sqrMagnitude > 1f) MoveDirection.Normalize();

        float speed = MoveSpd * (IsSprinting ? SprintMult : 1f);
        if (_state == CombatState.HeavyCharge) speed *= 0.4f; // 차징 중 감속
        _rb.linearVelocity = MoveDirection * speed;
    }

    // ── 마우스 / 비주얼 ──────────────────────────────────────────────

    void UpdateMouseFacing()
    {
        if (_cam == null) return;
        Vector3 m = _cam.ScreenToWorldPoint(Input.mousePosition);
        m.z = transform.position.z;
        Vector2 dir = (Vector2)(m - transform.position);
        if (dir.sqrMagnitude > 0.01f)
        {
            FacingDirection = dir.normalized;
            MouseWorldPos   = m;
        }
    }

    void UpdateFlip()
    {
        if (!flipByMouse || spriteRenderer == null) return;
        if (Mathf.Abs(FacingDirection.x) > 0.01f)
            spriteRenderer.flipX = FacingDirection.x < 0f;
    }

    void UpdateFlashlight()
    {
        if (flashlightPivot == null) return;
        float angle = Mathf.Atan2(FacingDirection.y, FacingDirection.x) * Mathf.Rad2Deg;
        flashlightPivot.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    void UpdateSprint(bool uiOpen)
    {
        IsSprinting = !uiOpen && !_exhausted && _state == CombatState.Idle
                      && Input.GetKey(KeyCode.LeftShift) && IsMoving
                      && _stamina > SprintMinStam;

        if (IsSprinting)
        {
            _stamina -= SprintCost * Time.deltaTime;
            _regenDelayTimer = StamRegenDelay;
            if (_stamina <= 0f) { _stamina = 0f; EnterExhausted(); }
        }
    }

    // ── 전투 입력 ────────────────────────────────────────────────────

    void HandleCombatInput()
    {
        if (!CombatEnabled || _exhausted) return;

        // 구르기 (Space)
        if (Input.GetKeyDown(KeyCode.Space) && _state != CombatState.Dodge && _dodgeCooldownTimer <= 0f)
        {
            TryDodge();
            return;
        }

        if (_state != CombatState.Idle && _state != CombatState.HeavyCharge) return;

        // 약공격 (좌클릭)
        if (Input.GetMouseButtonDown(0) && _lightCooldownTimer <= 0f && _state == CombatState.Idle)
        {
            DoLightAttack();
            return;
        }

        // 강공격 (우클릭 차징 → 떼면 발동)
        if (Input.GetMouseButtonDown(1) && _heavyCooldownTimer <= 0f && _state == CombatState.Idle)
        {
            _state = CombatState.HeavyCharge;
            _chargeTimer = 0f;
        }
        if (_state == CombatState.HeavyCharge)
        {
            _chargeTimer += Time.deltaTime;
            if (Input.GetMouseButtonUp(1))
                DoHeavyAttack();
        }
    }

    void DoLightAttack()
    {
        float cost = _comboStep >= 2 ? Stat.lightCombo3StaminaCost : Stat.lightStaminaCost;
        if (!ConsumeStamina(cost)) return;

        float dmg = _comboStep switch
        {
            0 => Stat.lightDamage,
            1 => Stat.lightCombo2Damage,
            _ => Stat.lightCombo3Damage,
        };
        float groggy = _comboStep switch
        {
            0 => Stat.lightGroggy,
            1 => Stat.lightCombo2Groggy,
            _ => Stat.lightCombo3Groggy,
        };

        _state = CombatState.LightAttack;
        _attackStateTimer = 0.18f;
        _lightCooldownTimer = LightCooldown;

        DealDamageInArc(LightRange, dmg, groggy);

        _comboStep = (_comboStep + 1) % Mathf.Max(1, ComboMax);
        _comboTimer = ComboWindow;
    }

    void DoHeavyAttack()
    {
        bool full = _chargeTimer >= HeavyChargeTime;
        float cost = full ? Stat.heavyFullStaminaCost : Stat.heavyStaminaCost;
        if (!ConsumeStamina(cost)) { _state = CombatState.Idle; return; }

        float dmg    = full ? Stat.heavyFullDamage : Stat.heavyDamage;
        float groggy = full ? Stat.heavyFullGroggy : Stat.heavyGroggy;

        _state = CombatState.HeavyRelease;
        _attackStateTimer = 0.25f;
        _heavyCooldownTimer = HeavyCooldown;

        DealDamageInArc(HeavyRange, dmg, groggy);
    }

    void TryDodge()
    {
        if (!ConsumeStamina(DodgeCost)) return;
        _dodgeDir = IsMoving ? MoveDirection.normalized : FacingDirection;
        _state = CombatState.Dodge;
        _dodgeTimer = DodgeDur;
        _dodgeInvTimer = DodgeInvDur;
        _dodgeCooldownTimer = DodgeCooldown;
    }

    /// <summary>FacingDirection 방향 부채꼴(원형 근사)로 적에게 데미지 + 그로기.</summary>
    void DealDamageInArc(float range, float damage, float groggy)
    {
        Vector2 center = (Vector2)transform.position + FacingDirection * (range * 0.5f);
        var hits = Physics2D.OverlapCircleAll(center, range * 0.6f, enemyMask);
        foreach (var col in hits)
        {
            var enemy = col.GetComponentInParent<EnemyController>();
            if (enemy == null || enemy.IsDead) continue;
            enemy.TakeHit(damage, groggy, FacingDirection);
        }
    }

    // ── 타이머 / 상태 ────────────────────────────────────────────────

    void UpdateCombatTimers()
    {
        float dt = Time.deltaTime;
        if (_lightCooldownTimer > 0) _lightCooldownTimer -= dt;
        if (_heavyCooldownTimer > 0) _heavyCooldownTimer -= dt;
        if (_dodgeCooldownTimer > 0) _dodgeCooldownTimer -= dt;
        if (_dodgeInvTimer > 0)      _dodgeInvTimer -= dt;

        // 콤보 윈도우 만료 → 콤보 리셋
        if (_comboTimer > 0) { _comboTimer -= dt; if (_comboTimer <= 0) _comboStep = 0; }

        switch (_state)
        {
            case CombatState.LightAttack:
            case CombatState.HeavyRelease:
                _attackStateTimer -= dt;
                if (_attackStateTimer <= 0) _state = CombatState.Idle;
                break;
            case CombatState.Dodge:
                _dodgeTimer -= dt;
                if (_dodgeTimer <= 0) _state = CombatState.Idle;
                break;
        }
    }

    void UpdateStamina()
    {
        // 탈진 회복
        if (_exhausted)
        {
            _exhaustTimer -= Time.deltaTime;
            if (_exhaustTimer <= 0f) _exhausted = false;
        }

        if (_regenDelayTimer > 0f) { _regenDelayTimer -= Time.deltaTime; return; }

        if (_stamina < MaxStam && !IsSprinting)
            _stamina = Mathf.Min(MaxStam, _stamina + StamRegen * Time.deltaTime);
    }

    void EnterExhausted()
    {
        _exhausted = true;
        _exhaustTimer = ExhaustDur;
        if (_state != CombatState.Dodge) _state = CombatState.Idle;
    }

    /// <summary>스태미너 소모. 부족하면 false (탈진 진입 가능).</summary>
    public bool ConsumeStamina(float amount)
    {
        if (_exhausted) return false;
        if (_stamina < amount) return false;
        _stamina -= amount;
        _regenDelayTimer = StamRegenDelay;
        if (_stamina <= 0f) { _stamina = 0f; EnterExhausted(); }
        return true;
    }

    // ── 외부 API ─────────────────────────────────────────────────────
    public void SetCanMove(bool value) => CanMove = value;
    public void Teleport(Vector2 worldPos) => transform.position = worldPos;
    public float GetAttackRange() => LightRange;

    // ── 자동 스폰 ───────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        if (FindFirstObjectByType<TopDownPlayer>(FindObjectsInactive.Include) != null) return;

        var prefab = Resources.Load<GameObject>("TopDownPlayer");
        if (prefab != null)
        {
            var go = Instantiate(prefab);
            go.name = "TopDownPlayer";
            var sp = FindFirstObjectByType<SpawnPoint>();
            if (sp != null) go.transform.position = sp.transform.position;
        }
        else
        {
            Debug.LogWarning("[TopDownPlayer] Resources/TopDownPlayer 프리팹 없음. 씬에 직접 배치하거나 프리팹 생성 필요.");
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
        Vector2 c = (Vector2)transform.position + FacingDirection * 1f;
        Gizmos.DrawWireSphere(c, 1.2f);
    }
#endif
}
