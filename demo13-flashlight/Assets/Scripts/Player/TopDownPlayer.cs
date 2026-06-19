using UnityEngine;
using Spine.Unity;

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
    [Tooltip("Spine 캐릭터(있으면 SpriteRenderer 대신 이걸로 좌우 플립 + 이동/전투 애니 구동)")]
    [SerializeField] SkeletonAnimation skeletonAnimation;
    [SerializeField] bool flipByMouse = true;
    [Tooltip("좌우 미러 부호 반전. 캐릭터가 마우스와 반대로 보이면 토글.")]
    [SerializeField] bool flipInvert = true;

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

    [Header("이동 감각 (걷는 느낌)")]
    [Tooltip("켜면 걷기/달리기 애니 속도를 실제 이동속도에 비례시킴. 끄면 애니는 고유 속도(1x)로 재생되어 이동속도와 분리됨(기본). ※ 가속/감속 수치는 StatDB ▸ Player Stat ▸ 이동 감각.")]
    [SerializeField] bool animCadenceMatchesSpeed = false;

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
    /// <summary>앉기(C 토글) — 속도 감소 + sit/sit_walk 애니.</summary>
    public bool  IsCrouching    => _crouching;
    bool _crouching;

    /// <summary>스태미너 완전 회복 + 탈진 해제 (수면 등에서 호출).</summary>
    public void RefillStamina() { _stamina = MaxStam; _exhausted = false; }

    // ── 스탯 접근 (StatDB.playerStat → 폴백) ─────────────────────────
    PlayerStatData _stat;
    PlayerStatData Stat => _stat ??= (StatDB.Instance != null ? StatDB.Instance.playerStat : null) ?? new PlayerStatData();

    float MoveSpd        => (Stat != null ? Stat.moveSpeed : fallbackMoveSpeed) * WeaponMoveMult;
    float SprintMult     => Stat.sprintSpeedMultiplier;
    float SprintCost     => Stat.sprintStaminaCost;
    float SprintMinStam  => Stat.sprintMinStamina;
    float MotionSpeed(string key) => Stat != null ? MotionStat.SpeedOf(Stat.motions, key, 1f) : 1f;
    float RunMaxDistance => Stat != null ? MotionStat.DistanceOf(Stat.motions, "run", 0f) : 0f;
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
    float DodgeDist      => Stat != null ? MotionStat.DistanceOf(Stat.motions, "roll", Stat.dodgeDistance) : 3f;
    float DodgeDur       => Stat.dodgeDuration;
    float DodgeInvDur    => Stat.dodgeInvincibleDuration;
    float DodgeCooldown  => Stat.dodgeCooldown;
    float DodgeCost      => Stat.dodgeStaminaCost;
    float MoveAccel      => Stat.moveAccel;
    float MoveDecel      => Stat.moveDecel;

    // ── 내부 상태 ─────────────────────────────────────────────────────
    Rigidbody2D _rb;
    Camera      _cam;
    CombatState _state = CombatState.Idle;

    // 스태미너
    float _stamina;
    float _regenDelayTimer;
    float _exhaustTimer;
    float _runDist;   // 현재 연속 달린 누적 거리(m) — RunMaxDistance 게이트용
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
    PlayerEquipment _equip;
    PlayerEquipment Equip => _equip != null ? _equip : (_equip = GetComponent<PlayerEquipment>());
    // 무기 보정 × 부착물(파츠) 집계 보정
    float WeaponMoveMult  => (_weapon != null ? _weapon.moveSpeedMult : 1f) * (Equip != null ? Equip.WeaponPartMoveMult : 1f);
    float WeaponStamMult  => (_weapon != null ? _weapon.staminaCostMult : 1f) * (Equip != null ? Equip.WeaponPartStaminaMult : 1f);

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
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
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

    bool _uiOpen;   // 대화/UI 열림 — 이동·조준·전투 입력 전면 봉쇄용

    void Update()
    {
        if (_cam == null) _cam = Camera.main;

        _uiOpen = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();

        // UI/대화 열림 → 조준·전투 입력 정지 (이동은 FixedUpdate에서 정지)
        if (!_uiOpen)
        {
            if (Input.GetKeyDown(KeyCode.C)) _crouching = !_crouching;   // 앉기 토글
            UpdateMouseFacing();
            UpdateFlip();
            UpdateVisionLight();
            HandleCombatInput();
        }

        UpdateCombatTimers();
        UpdateStamina();
        UpdateSprint(_uiOpen);
        UpdateSkeletonAnimation();

        // 구르기 무적 → 허트박스 비활성 (피격 안 됨)
        if (_hurtbox != null) _hurtbox.SetActive(!IsInvincible);
    }

    void FixedUpdate()
    {
        // UI/대화 열림 → 이동 전면 정지
        if (_uiOpen) { _rb.linearVelocity = Vector2.zero; MoveDirection = Vector2.zero; return; }

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
        if (_crouching) speed *= Stat.crouchSpeedMultiplier;  // 앉아 이동 = 감속
        if (_state == CombatState.HeavyCharge) speed *= 0.4f; // 차징 중 감속

        // 가속/감속 램프 — 즉속도(미끄럼)가 아니라 살짝 차오르고/잦아드는 무게감.
        Vector2 target = MoveDirection * speed;
        bool wantMove = MoveDirection.sqrMagnitude > 0.01f;
        float rate = wantMove ? MoveAccel : MoveDecel;
        if (rate <= 0f) _rb.linearVelocity = target;   // 0=즉시(기존 동작)
        else            _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, target, rate * Time.fixedDeltaTime);
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
        if (!flipByMouse) return;
        if (Mathf.Abs(FacingDirection.x) <= 0.01f) return;
        bool faceLeft = FacingDirection.x < 0f;
        if (flipInvert) faceLeft = !faceLeft;   // 스켈레톤 기본 방향이 반대일 때 보정

        // Spine 우선 — 스켈레톤 X스케일 부호로 미러링
        if (skeletonAnimation != null && skeletonAnimation.Skeleton != null)
            skeletonAnimation.Skeleton.ScaleX = faceLeft ? -1f : 1f;
        else if (spriteRenderer != null)
            spriteRenderer.flipX = faceLeft;
    }

    // ── Spine 애니메이션 구동 ────────────────────────────────────────
    Spine.AnimationState _spineState;
    string _curAnim = "<init>";
    string _oneShotAnim;   // 끝까지 1회 보장할 비루프 애니(roll/attack) — 재생 중엔 루프 애니로 안 끊음

    /// <summary>이동/전투 상태에 맞춰 Spine 트랙0 애니를 전환. 정지 시 idle 재생(없으면 셋업 포즈).
    /// 모든 애니는 스켈레톤에 실제로 존재할 때만 재생 — 없는 이름을 넣어 경고나는 일 방지(점진 도입).</summary>
    void UpdateSkeletonAnimation()
    {
        if (skeletonAnimation == null) return;
        if (_spineState == null)
        {
            _spineState = skeletonAnimation.AnimationState;
            if (_spineState == null) return;
        }

        string target; bool loop; string motionKey;   // motionKey = 논리 모션(애니 속도 조회용)
        switch (_state)
        {
            case CombatState.Dodge:
                target = FirstAnim("roll", "dodge"); loop = false; motionKey = "roll"; break;
            case CombatState.LightAttack:
            case CombatState.HeavyRelease:
                target = FirstAnim("attack", "attack1", "attack_1"); loop = false; motionKey = "attack"; break;
            default:
                if (_crouching)
                {
                    target = IsMoving ? FirstAnim("sit_walk", "sit") : FirstAnim("sit");      // 앉기
                    motionKey = IsMoving ? "crouch_walk" : "crouch";
                }
                else if (IsMoving)
                {
                    target = IsSprinting ? FirstAnim("run", "walk") : FirstAnim("walk", "run"); // 이동
                    motionKey = IsSprinting ? "run" : "walk";
                }
                else
                {
                    target = IdleAnim();                                                       // 정지
                    motionKey = "idle";
                }
                loop = true;
                break;
        }

        // 전용 애니(공격/구르기/이동)가 스켈레톤에 없으면 idle로 폴백(루프), idle도 없으면 셋업 포즈.
        if (string.IsNullOrEmpty(target)) { target = IdleAnim(); loop = true; }

        // 원샷 애니(roll/attack) 1회 보장: 새 target이 루프 애니(idle/이동)인데 현재 원샷이 아직 안 끝났으면 유지.
        // (Dodge 상태가 dodgeDuration에 끝나 Idle로 돌아가도 구르기 모션이 중간에 idle로 잘리지 않게.)
        // 단 새 target이 또 다른 원샷(loop=false: 공격/재구르기)이면 잠금을 넘어 교체 허용.
        if (loop && !string.IsNullOrEmpty(_oneShotAnim))
        {
            var cur = _spineState.GetCurrent(0);
            if (cur != null && cur.Animation != null && cur.Animation.Name == _oneShotAnim && !cur.IsComplete)
                return;            // 원샷 재생 중 → 루프 애니로 끊지 않음
            _oneShotAnim = null;   // 완료(또는 트랙 교체) → 잠금 해제
        }

        // 애니 재생 속도 — 걷기/달리기는 이동속도 비례(토글) × 모션별 애니 속도(모든 모션).
        {
            float ts = 1f;
            if ((motionKey == "walk" || motionKey == "run") && animCadenceMatchesSpeed && _rb != null)
                ts = Mathf.Clamp(_rb.linearVelocity.magnitude / Mathf.Max(0.1f, MoveSpd), 0.5f, 1.8f);
            ts *= MotionSpeed(motionKey);   // 모션별 애니 속도(idle/walk/run/attack/roll …)
            skeletonAnimation.timeScale = ts;
        }

        if (target == _curAnim) return;
        _curAnim = target;
        if (string.IsNullOrEmpty(target)) { _spineState.SetEmptyAnimation(0, 0.12f); _oneShotAnim = null; }
        else
        {
            _spineState.SetAnimation(0, target, loop);
            _oneShotAnim = loop ? null : target;   // 비루프(roll/attack)면 끝까지 보장 대상으로 잠금
        }
    }

    /// <summary>idle 애니 이름(흔한 표기 변형 자동 탐색). 없으면 null=셋업 포즈.</summary>
    string IdleAnim() => FirstAnim("idle", "Idle", "idle_loop", "idle1", "Idle_Loop");

    /// <summary>우선순위 목록 중 스켈레톤에 실제 존재하는 첫 애니 이름. 없으면 null.</summary>
    string FirstAnim(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
            if (HasAnim(names[i])) return names[i];
        return null;
    }

    bool HasAnim(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        var data = skeletonAnimation != null && skeletonAnimation.Skeleton != null
            ? skeletonAnimation.Skeleton.Data : null;
        return data != null && data.FindAnimation(name) != null;
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
        bool wantSprint = !uiOpen && !_exhausted && !_crouching && _state == CombatState.Idle
                          && Input.GetKey(KeyCode.LeftShift) && IsMoving
                          && _stamina > SprintMinStam;

        // 달리기 거리 제한: RunMaxDistance>0이면 그 거리만큼 달리면 끊김(멈추면 회복).
        float maxDist = RunMaxDistance;
        if (maxDist > 0f && _runDist >= maxDist) wantSprint = false;

        IsSprinting = wantSprint;

        if (IsSprinting)
        {
            if (!StaminaInfinite)   // 안전구역: 무소모
            {
                _stamina -= SprintCost * Time.deltaTime;
                _regenDelayTimer = StamRegenDelay;
                if (_stamina <= 0f) { _stamina = 0f; EnterExhausted(); }
            }
            if (maxDist > 0f && _rb != null)
                _runDist += _rb.linearVelocity.magnitude * Time.deltaTime;
        }
        else if (_runDist > 0f)
        {
            // 안 달리면 거리 예산 회복(달리기 속도의 2배로 빠르게 차오름).
            _runDist = Mathf.Max(0f, _runDist - MoveSpd * 2f * Time.deltaTime);
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

    /// <summary>안전구역(레이드 아님 = 안전가옥/은신처)에선 스태미너 무한 — 소모·탈진 없음.</summary>
    bool StaminaInfinite =>
        RegionTimeManager.Instance == null
        || string.IsNullOrEmpty(RegionTimeManager.Instance.ActiveRegionId);

    void UpdateStamina()
    {
        // 안전구역: 스태미너 항상 가득 + 탈진 해제.
        if (StaminaInfinite)
        {
            _stamina = MaxStam;
            _exhausted = false;
            _exhaustTimer = 0f;
            return;
        }

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
        if (StaminaInfinite) return true;   // 안전구역: 무소모(행동 제한 없음)
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
        // Systems 씬이 PlayerRig를 배치 공급하면 코드 스폰 폴백을 건너뛴다.
        if (SystemsScene.ProvidesSystems) return;
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
