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

    [Header("시야 라이트 (부채꼴, 앞 방향)")]
    [Tooltip("PlayerLight(Light2D) Transform. 마우스 방향으로 회전 + 앞으로 오프셋")]
    [SerializeField] Transform lightPivot;
    [Tooltip("라이트 원점을 앞으로 미는 거리 (본인 주변 어둡게, 앞을 밝게)")]
    [SerializeField] float lightForwardOffset = 0.45f;
    [Tooltip("라이트 콘 회전 보정(도). 콘 방향이 안 맞으면 조정 (-90/90/0/180)")]
    [SerializeField] float lightAngleOffset = -90f;

    [Header("전투")]
    [Tooltip("공격 판정 대상 레이어 (적). 비워두면 EnemyController 컴포넌트로 판별)")]
    [SerializeField] LayerMask enemyMask = ~0;

    [Header("공격 데이터 (비우면 StatDB 기반 자동 생성)")]
    [Tooltip("약공격 콤보 체인 (연속 공격)")]
    [SerializeField] AttackComboData lightCombo;
    [SerializeField] AttackData heavyAttack;
    [SerializeField] AttackData heavyFullAttack;

    [Header("기본값 (StatDB 없을 때)")]
    [SerializeField] float fallbackMoveSpeed = 5f;

    // ── 퍼블릭 API ───────────────────────────────────────────────────
    public Vector2 FacingDirection { get; private set; } = Vector2.down;
    /// <summary>콘 라이트(시야) Transform — 그림자 방향 등에서 실제 비추는 방향 참조용.</summary>
    public Transform LightPivot => lightPivot;
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

    float MoveSpd        => (Stat != null ? Stat.moveSpeed : fallbackMoveSpeed) * WeaponMoveMult;
    float SprintMult     => Stat.sprintSpeedMultiplier;
    float SprintCost     => Stat.sprintStaminaCost;
    float SprintMinStam  => Stat.sprintMinStamina;
    float MaxStam        => Stat.maxStamina;
    float StamRegen      => Stat.staminaRegen;
    float StamRegenDelay => Stat.staminaRegenDelay;
    float ExhaustDur     => Stat.exhaustionDuration;
    float LightRange     => Stat.lightRange;
    float LightCooldown  => Stat.lightCooldown;
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
    float _lightCooldownTimer;
    float _attackStateTimer;
    bool  _comboBuffered;   // 다음 단계 선입력 예약
    float _comboBufferTimer;

    // 강공격
    float _chargeTimer;
    float _heavyCooldownTimer;

    // 구르기
    float _dodgeTimer;
    float _dodgeCooldownTimer;
    float _dodgeInvTimer;
    Vector2 _dodgeDir;

    // 히트박스 / 허트박스
    AttackPerformer _performer;
    Hurtbox _hurtbox;

    // 장착 무기 (null=맨손, 인스펙터 기본값 사용)
    WeaponData _weapon;
    AttackComboData CurrentLightCombo => (_weapon != null && _weapon.lightCombo != null) ? _weapon.lightCombo : lightCombo;
    AttackData CurrentHeavy           => (_weapon != null && _weapon.heavyAttack != null) ? _weapon.heavyAttack : heavyAttack;
    AttackData CurrentHeavyFull       => (_weapon != null && _weapon.heavyFullAttack != null) ? _weapon.heavyFullAttack : heavyFullAttack;
    float WeaponMoveMult  => _weapon != null ? _weapon.moveSpeedMult : 1f;
    float WeaponStamMult  => _weapon != null ? _weapon.staminaCostMult : 1f;

    /// <summary>무기 장착 반영 (PlayerEquipment가 호출). null=맨손.</summary>
    public void SetWeapon(WeaponData weapon) => _weapon = weapon;

    // ── Unity 생명주기 ───────────────────────────────────────────────

    void Awake()
    {
        // PlayerRig(카메라+플레이어+라이트 한 세트)의 루트를 보존/중복제거
        if (Instance != null && Instance != this) { Destroy(transform.root.gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);

        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType       = RigidbodyType2D.Dynamic;       // 벽에 막히려면 Dynamic (Kinematic이면 뚫음)
        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 빠른 이동·대시 터널링 방지
        _rb.interpolation  = RigidbodyInterpolation2D.Interpolate;        // 부드러운 이동

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _cam = Camera.main;

        _stamina = MaxStam;

        // 공격 판정기 (적 레이어 타격)
        _performer = GetComponent<AttackPerformer>();
        if (_performer == null) _performer = gameObject.AddComponent<AttackPerformer>();
        _performer.Configure(enemyMask, () => FacingDirection);

        _hurtbox = GetComponentInChildren<Hurtbox>();

        // 장비 컴포넌트 보장 (무기 장착)
        if (GetComponent<PlayerEquipment>() == null) gameObject.AddComponent<PlayerEquipment>();

        // 피격 화면 연출 보장 (위험 비례)
        if (GetComponent<PlayerHitReaction>() == null) gameObject.AddComponent<PlayerHitReaction>();

        EnsureDefaultAttacks();
    }

    /// <summary>인스펙터에 데이터가 비어있으면 StatDB 수치로 프레임 기반 기본 생성.</summary>
    void EnsureDefaultAttacks()
    {
        if (lightCombo == null || lightCombo.StepCount == 0)
        {
            lightCombo = ScriptableObject.CreateInstance<AttackComboData>();
            lightCombo.comboId = "light";
            lightCombo.steps = new System.Collections.Generic.List<AttackData>
            {
                MakeAttack("light1", 6, Stat.lightDamage,       Stat.lightGroggy,       LightRange,        2, 3),
                MakeAttack("light2", 6, Stat.lightCombo2Damage, Stat.lightCombo2Groggy, LightRange,        2, 3),
                MakeAttack("light3", 8, Stat.lightCombo3Damage, Stat.lightCombo3Groggy, LightRange * 1.1f, 3, 5),
            };
        }
        if (heavyAttack == null)
            heavyAttack = MakeAttack("heavy", 7, Stat.heavyDamage, Stat.heavyGroggy, HeavyRange, 3, 5, hitstop: 0.05f);
        if (heavyFullAttack == null)
            heavyFullAttack = MakeAttack("heavyFull", 9, Stat.heavyFullDamage, Stat.heavyFullGroggy, HeavyRange * 1.2f, 4, 6, hitstop: 0.06f);
    }

    static AttackData MakeAttack(string id, int frames, float dmg, float grog, float range, int hitStart, int hitEnd, float hitstop = 0f)
    {
        var a = ScriptableObject.CreateInstance<AttackData>();
        a.attackId = id; a.fps = 12; a.totalFrames = frames;
        a.cancelFromFrame = Mathf.Max(1, hitEnd);
        a.damage = dmg; a.groggy = grog;
        a.hitstop = hitstop > 0f; a.hitstopDuration = hitstop > 0f ? hitstop : 0.05f;
        a.windows = new System.Collections.Generic.List<HitWindow>
        {
            new HitWindow
            {
                label = "hit", startFrame = hitStart, endFrame = hitEnd, shape = HitboxShape.Box,
                offset = new Vector2(range * 0.5f, 0f),
                boxSize = new Vector2(range, range * 0.75f),
            }
        };
        return a;
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
        UpdateVisionLight();

        bool uiOpen = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();
        if (!uiOpen) HandleCombatInput();

        UpdateCombatTimers();
        UpdateStamina();
        UpdateSprint(uiOpen);

        // 구르기 무적 → 허트박스 비활성 (피격 안 됨)
        if (_hurtbox != null) _hurtbox.SetActive(!IsInvincible);
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

    void UpdateVisionLight()
    {
        if (lightPivot == null) return;
        // 마우스 방향으로 콘 회전
        float angle = Mathf.Atan2(FacingDirection.y, FacingDirection.x) * Mathf.Rad2Deg;
        lightPivot.localRotation = Quaternion.Euler(0f, 0f, angle + lightAngleOffset);
        // 콘 원점을 앞으로 → 본인 주변은 어둑, 앞이 부채꼴로 밝음 (다크우드식)
        lightPivot.localPosition = (Vector3)(FacingDirection * lightForwardOffset);
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

        // 약공격 (좌클릭) — 콤보 체인
        if (Input.GetMouseButtonDown(0))
        {
            if (_state == CombatState.Idle && _lightCooldownTimer <= 0f)
            {
                StartLightCombo();
                return;
            }
            if (_state == CombatState.LightAttack)
            {
                // 다음 단계 선입력 예약 (캔슬 가능 시점에 발동)
                _comboBuffered = true;
                _comboBufferTimer = CurrentLightCombo != null ? CurrentLightCombo.bufferTime : 0.25f;
            }
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

    void StartLightCombo()
    {
        _comboStep = 0;
        PerformComboStep();
    }

    void AdvanceCombo()
    {
        _comboStep++;
        PerformComboStep();
    }

    void PerformComboStep()
    {
        var combo = CurrentLightCombo;
        var atk = combo != null ? combo.GetStep(_comboStep) : null;
        if (atk == null) { EndLightCombo(); return; }

        float cost = (_comboStep >= 2 ? Stat.lightCombo3StaminaCost : Stat.lightStaminaCost) * WeaponStamMult;
        if (!ConsumeStamina(cost)) { EndLightCombo(); return; }

        _state = CombatState.LightAttack;
        _attackStateTimer = atk.Duration;
        _performer.Perform(atk);
        _comboBuffered = false;
    }

    void EndLightCombo()
    {
        _comboStep = 0;
        _comboBuffered = false;
        _lightCooldownTimer = LightCooldown;
        if (_state == CombatState.LightAttack) _state = CombatState.Idle;
    }

    void DoHeavyAttack()
    {
        bool full = _chargeTimer >= HeavyChargeTime;
        float cost = (full ? Stat.heavyFullStaminaCost : Stat.heavyStaminaCost) * WeaponStamMult;
        if (!ConsumeStamina(cost)) { _state = CombatState.Idle; return; }

        _state = CombatState.HeavyRelease;
        var atk = full ? CurrentHeavyFull : CurrentHeavy;
        _attackStateTimer = atk != null ? atk.Duration : 0.25f;
        _heavyCooldownTimer = HeavyCooldown;

        _performer.Perform(atk);
    }

    void TryDodge()
    {
        if (!ConsumeStamina(DodgeCost)) return;
        _dodgeDir = IsMoving ? MoveDirection.normalized : FacingDirection;
        _state = CombatState.Dodge;
        _dodgeTimer = DodgeDur;
        _dodgeInvTimer = DodgeInvDur;
        _dodgeCooldownTimer = DodgeCooldown;
        _performer.Cancel();           // 진행 중 공격 취소
        _comboStep = 0;                // 콤보 끊김
        _comboBuffered = false;
    }

    // ── 타이머 / 상태 ────────────────────────────────────────────────

    void UpdateCombatTimers()
    {
        float dt = Time.deltaTime;
        if (_lightCooldownTimer > 0) _lightCooldownTimer -= dt;
        if (_heavyCooldownTimer > 0) _heavyCooldownTimer -= dt;
        if (_dodgeCooldownTimer > 0) _dodgeCooldownTimer -= dt;
        if (_dodgeInvTimer > 0)      _dodgeInvTimer -= dt;

        // 선입력 버퍼 만료
        if (_comboBuffered) { _comboBufferTimer -= dt; if (_comboBufferTimer <= 0f) _comboBuffered = false; }

        switch (_state)
        {
            case CombatState.LightAttack:
                // 캔슬 가능 시점 + 선입력 → 다음 콤보 단계
                int lastStep = (CurrentLightCombo != null ? CurrentLightCombo.StepCount : 0) - 1;
                if (_comboBuffered && _performer.CanCancel && _comboStep < lastStep)
                {
                    AdvanceCombo();
                    break;
                }
                _attackStateTimer -= dt;
                if (_attackStateTimer <= 0) EndLightCombo();
                break;
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
        // 맵툴 씬에선 플레이어 자동 스폰 안 함 — 맵 편집/미리보기 전용.
        if (MapToolScene.IsActive) return;
        if (Instance != null) return;
        if (FindFirstObjectByType<TopDownPlayer>(FindObjectsInactive.Include) != null) return;

        // PlayerRig = 카메라 + 플레이어 + 라이트 한 세트 프리팹 (DontDestroyOnLoad)
        var prefab = Resources.Load<GameObject>("PlayerRig");
        if (prefab != null)
        {
            var go = Instantiate(prefab);
            go.name = "PlayerRig";
            var sp = FindFirstObjectByType<SpawnPoint>();
            if (sp != null) go.transform.position = sp.transform.position;
        }
        else
        {
            Debug.LogWarning("[TopDownPlayer] Resources/PlayerRig 프리팹 없음. " +
                             "'Tools > TopDown 2D > Build Player Prefab'으로 생성하세요.");
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
