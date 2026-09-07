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
    [Header("3D 캐릭터 표시")]
    [SerializeField] GameObject character3DPrefab;
    [SerializeField] RuntimeAnimatorController character3DController;
    [SerializeField] Shader character3DShader;
    [SerializeField, Min(.01f)] float character3DScale = .65f;
    ChibiPlayerVisual _character3D;
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
    float MaxStam        => Stat.maxStamina * TraitManager.Mod("stamina_max");
    float StamRegen      => Stat.staminaRegen * TraitManager.Mod("stamina_regen");
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
    float DodgeInvDur    => Stat.dodgeInvincibleDuration * TraitManager.Mod("dodge_iframe");
    float DodgeCooldown  => Stat.dodgeCooldown;
    float DodgeCost      => Stat.dodgeStaminaCost * TraitManager.Mod("dodge_stamina_cost");
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
    PlayerGun _gun;          // 총기 사격/조준/장전 (근접 상태기계와 분리)
    Hurtbox _hurtbox;

    // 장착 무기 (null=맨손, 인스펙터 기본값 사용)
    WeaponData _weapon;
    AttackComboData CurrentLightCombo => (_weapon != null && _weapon.lightCombo != null) ? _weapon.lightCombo : lightCombo;
    AttackData CurrentHeavy           => (_weapon != null && _weapon.heavyAttack != null) ? _weapon.heavyAttack : heavyAttack;
    AttackData CurrentHeavyFull       => (_weapon != null && _weapon.heavyFullAttack != null) ? _weapon.heavyFullAttack : heavyFullAttack;
    PlayerEquipment _equip;
    PlayerEquipment Equip => _equip != null ? _equip : (_equip = GetComponent<PlayerEquipment>());
    PlayerInventory _inv;
    PlayerInventory Inv => _inv != null ? _inv : (_inv = GetComponent<PlayerInventory>());
    // 무게 페널티는 안전가옥(Safehouse 허브)만 면제 — 레이드/테스트 맵에선 항상 적용.
    //   (구: StaminaInfinite 기준은 '활성 지역 없음'이라 F1 직행·지역 미활성 레이드에서 페널티가 꺼지는 버그 → IsSafehouse로 교체)
    bool WeightPenaltyActive => UIManager.Instance == null || !UIManager.Instance.IsSafehouse;
    float OverweightMoveMult => (Inv != null && WeightPenaltyActive) ? Inv.OverweightMoveMult : 1f;
    bool OverweightSprintBlocked => Inv != null && WeightPenaltyActive && Inv.SprintBlocked;
    // 무기 보정 × 부착물(파츠) 집계 보정
    // 조준 중엔 느려진다 — 정밀 사격의 대가(2026-07-29).
    float AdsMoveMult     => (_gun != null && _gun.IsAiming && _weapon != null && _weapon.isRanged)
                             ? _weapon.adsMoveMult : 1f;
    float WeaponMoveMult  => (_weapon != null ? _weapon.moveSpeedMult : 1f)
                             * (Equip != null ? Equip.WeaponPartMoveMult : 1f) * AdsMoveMult;
    float WeaponStamMult  => (_weapon != null ? _weapon.staminaCostMult : 1f) * (Equip != null ? Equip.WeaponPartStaminaMult : 1f);

    /// <summary>무기 장착 반영 (PlayerEquipment가 호출). null=맨손.</summary>
    public void SetWeapon(WeaponData weapon) => _weapon = weapon;

    /// <summary>지금 장착한 무기 데이터(맨손이면 null).</summary>
    public WeaponData CurrentWeapon => _weapon;

    /// <summary>총을 들고 있나 — 켜져 있으면 좌클릭이 '휘두르기'가 아니라 '사격'이다.</summary>
    public bool IsRangedEquipped => _weapon != null && _weapon.isRanged;

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
        if (character3DPrefab != null)
        {
            var visual = gameObject.AddComponent<ChibiPlayerVisual>();
            if (visual.Initialize(character3DPrefab, character3DController, character3DShader, character3DScale))
            {
                _character3D = visual;
                if (spriteRenderer != null) spriteRenderer.enabled = false;
            }
            else
            {
                Destroy(visual);
                Debug.LogWarning("[TopDownPlayer] 3D character setup failed; keeping the previous visual.", this);
            }
        }
        EnsureGreyboxBody();   // ★ 스파인 탐색 **뒤에** — 앞에 두면 스파인이 있어도 네모를 덧그린다
        TestHealthBar.Attach(transform, 0.72f);   // 테스트용 — 이 한 줄만 지우면 흔적이 안 남는다
        BodyZoneOverlay.Attach(transform);        // 테스트용 부위 표시(F1에서 켠다, 기본 꺼짐)
        _cam = Camera.main;

        _stamina = MaxStam;

        // 공격 판정기 (적 레이어 타격)
        _performer = GetComponent<AttackPerformer>();
        if (_performer == null) _performer = gameObject.AddComponent<AttackPerformer>();
        _gun = GetComponent<PlayerGun>();
        if (_gun == null) _gun = gameObject.AddComponent<PlayerGun>();
        _performer.Configure(enemyMask, () => FacingDirection);
        // 근접도 **조준한 곳**이 맞는다(2026-07-29 사용자). 마우스 월드 좌표가 곧 부위가 된다.
        _performer.AimPoint = () => MouseWorldPos;

        // 그레이박스 칼 — 스파인이 들어오면 통째로 교체. **판정엔 관여하지 않는다**(연출 전용).
        // 손에 들리는 것 3종 — 무엇을 보여줄지는 **장착 아이템**이 정한다(UpdateWeaponVisual).
        //   2026-07-29 이전엔 칼만 무조건 붙어서 **맨손이어도 칼이 보였다**.
        _weaponVis = MeleeWeaponVisual.Attach(transform, new Color(0.85f, 0.88f, 0.95f), 6, 1.05f);
        _fistVis   = FistVisual.Attach(transform, new Color(0.42f, 0.36f, 0.30f), 6);          // 장갑 낀 주먹
        _gunVis    = GunVisual.Attach(transform, new Color(0.46f, 0.47f, 0.50f), 6, 0.62f);    // 총

        _hurtbox = GetComponentInChildren<Hurtbox>();

        // 장비 컴포넌트 보장 (무기 장착)
        if (GetComponent<PlayerEquipment>() == null) gameObject.AddComponent<PlayerEquipment>();

        // 피격 화면 연출 보장 (위험 비례)
        if (GetComponent<PlayerHitReaction>() == null) gameObject.AddComponent<PlayerHitReaction>();

        EnsureDefaultAttacks();
    }

    /// <summary>플레이어 콜라이더 반경 — 히트박스를 몸 밖으로 밀어내는 기준.</summary>
    const float BodyRadius = 0.3f;

    /// <summary>인스펙터에 데이터가 비어있으면 StatDB 수치로 프레임 기반 기본 생성.</summary>
    void EnsureDefaultAttacks()
    {
        if (lightCombo == null || lightCombo.StepCount == 0)
        {
            lightCombo = ScriptableObject.CreateInstance<AttackComboData>();
            lightCombo.comboId = "light";
            lightCombo.steps = new System.Collections.Generic.List<AttackData>
            {
                // 2026-07-11: 약공에도 짧은 히트스탑 부여 — 예전엔 hitstop:0이라 약공은 히트스탑·셰이크·
                //   줌펀치가 **전부 미발동**해서 "때려도 아무 반응이 없는" 느낌이었다. 3타는 조금 더 길게.
                MakeAttack("light1", 6, Stat.lightDamage,       Stat.lightGroggy,       LightRange,        2, 3, hitstop: 0.03f),
                MakeAttack("light2", 6, Stat.lightCombo2Damage, Stat.lightCombo2Groggy, LightRange,        2, 3, hitstop: 0.035f),
                MakeAttack("light3", 8, Stat.lightCombo3Damage, Stat.lightCombo3Groggy, LightRange * 1.1f, 3, 5, hitstop: 0.05f),
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
                // 2026-07-11: 리치(range)와 스윙 폭을 분리.
                //   구: offset=range*0.5, boxSize=(range, range*0.75)
                //     → ① 사거리를 키우면 폭까지 같이 커져 '몸통 폭의 몇 배짜리 장판'이 됐고
                //        ② 박스 근접변이 플레이어 원점에 딱 붙어 **옆(90°)·뒤에 붙은 적까지** 정면 판정에 들어왔다.
                //   신: 몸 반경만큼 앞으로 밀어내고, 폭은 무기 스윙 폭으로 고정(사거리와 독립).
                offset = new Vector2(BodyRadius + range * 0.5f, 0f),
                boxSize = new Vector2(range, Mathf.Max(0.7f, range * 0.55f)),
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

        GameInput.Tick();   // 패드/마우스 마지막 사용 디바이스 갱신 (조준 소스 전환)
        _uiOpen = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();

        // UI/대화 열림 → 조준·전투 입력 정지 (이동은 FixedUpdate에서 정지)
        if (!_uiOpen)
        {
            if (GameInput.GetKeyDown(KeyCode.C)) _crouching = !_crouching;   // 앉기 토글
            UpdateMouseFacing();
            UpdateFlip();
            UpdateVisionLight();
            HandleCombatInput();
        }

        UpdateWeaponVisual();   // UI 열림 여부와 무관 — 칼이 허공에 굳어 있지 않게
        UpdateCombatTimers();
        UpdateStamina();
        UpdateSprint(_uiOpen);
        UpdateCharacter3D();

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
            // 2026-07-11 구르기 감각 개선: 등속(=순간이동처럼 뚝 끊김) → **강한 시작 + 부드러운 착지**.
            //   진행도 t에 따라 속도 배율 1.35→0.5로 감쇠(평균≈0.93이라 이동거리는 거의 그대로).
            float dodgeT = DodgeDur > 0f ? Mathf.Clamp01(1f - _dodgeTimer / DodgeDur) : 1f;
            float dodgeMul = Mathf.Lerp(1.35f, 0.5f, dodgeT);
            _rb.linearVelocity = _dodgeDir * (DodgeDist / DodgeDur) * dodgeMul;
            return;
        }

        // 공격/차징 중 — 이동 정지 (제자리 공격)
        if (_state == CombatState.LightAttack || _state == CombatState.HeavyRelease)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        if (!CanMove) { _rb.linearVelocity = Vector2.zero; return; }

        float h = GameInput.GetAxisRaw("Horizontal");
        float v = GameInput.GetAxisRaw("Vertical");
        MoveDirection = new Vector2(h, v);
        if (MoveDirection.sqrMagnitude > 1f) MoveDirection.Normalize();

        float speed = MoveSpd * (IsSprinting ? SprintMult : 1f);
        if (_crouching) speed *= Stat.crouchSpeedMultiplier;  // 앉아 이동 = 감속
        if (_state == CombatState.HeavyCharge) speed *= 0.4f; // 차징 중 감속
        speed *= OverweightMoveMult;                          // 무게 초과 페널티(과적 −15% / 심각 −30%)

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
        // 게임패드 조준 — 카메라 불필요하므로 _cam 가드 위에서 처리.
        if (GameInput.PadActive)
        {
            Vector2 aim = GameInput.AimStick;
            if (aim.sqrMagnitude > 0.01f)
            {
                // 오른쪽 스틱을 밀면 그 방향을 바라본다.
                FacingDirection = aim.normalized;
                MouseWorldPos   = transform.position + (Vector3)(FacingDirection * 3f);
            }
            else if (MoveDirection.sqrMagnitude > 0.01f)
            {
                // 오른쪽 스틱 미입력 → 이동 방향으로 바라본다(트윈스틱 폴백, facing 고착 방지).
                FacingDirection = MoveDirection.normalized;
                MouseWorldPos   = transform.position + (Vector3)(FacingDirection * 3f);
            }
            // 조준·이동 둘 다 미입력이면 마지막 방향 유지.
            return;
        }

        if (_cam == null) return;

        // 마우스 조준 — 커서 월드좌표를 향한다.
        Vector3 m = _cam.ScreenToWorldPoint(GameInput.mousePosition);
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
        if (_character3D != null) return;
        if (!flipByMouse) return;
        if (Mathf.Abs(FacingDirection.x) <= 0.01f) return;
        bool faceLeft = FacingDirection.x < 0f;
        if (flipInvert) faceLeft = !faceLeft;   // 스프라이트 기본 방향이 반대일 때 보정

        if (spriteRenderer != null)
            spriteRenderer.flipX = faceLeft;
    }

    void UpdateCharacter3D()
    {
        if (_character3D == null) return;
        float speed = _rb != null ? _rb.linearVelocity.magnitude : 0f;
        bool allowed = !_uiOpen && CanMove && !IsAttacking;
        bool running = IsSprinting || _state == CombatState.Dodge;
        string motion = allowed && speed > .05f ? (running ? "run" : "walk") : "idle";
        float cadence = MotionSpeed(motion);
        if (animCadenceMatchesSpeed && motion != "idle")
            cadence *= Mathf.Clamp(speed / Mathf.Max(.1f, MoveSpd), .5f, 1.8f);
        _character3D.UpdateMotion(FacingDirection, speed, running, allowed, cadence, Time.deltaTime);
    }

    MeleeWeaponVisual _weaponVis;
    FistVisual _fistVis;
    GunVisual  _gunVis;

    /// <summary>주인공 그레이박스 몸통 — **스프라이트도 스파인도 없으면** 네모를 만들어 준다.
    ///
    /// (2026-07-29 사용자: "지금 주인공 없으니 네모 스프라이트 적당한 크기로 만들어서 해주라, 적들처럼")
    /// PlayerRig 프리팹엔 몸통 스프라이트가 없어서 여태 **주인공이 안 보였다**(손/칼만 떠 있었다).
    /// 적의 런타임 그레이박스와 같은 방식이라, 스파인이 붙으면 이 분기는 저절로 안 탄다.
    /// 크기는 콜라이더(0.6×0.9)에 맞춘다 — 보이는 것과 맞는 것이 어긋나면 안 된다.</summary>
    void EnsureGreyboxBody()
    {
        if (_character3D != null) return;
        if (spriteRenderer != null && spriteRenderer.sprite != null) return;

        var go = new GameObject("GreyboxBody");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = new Vector3(0.62f, 0.9f, 1f);      // 콜라이더 0.6×0.9에 맞춤

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PlaceholderSprite.Square;
        sr.color = new Color(0.42f, 0.58f, 0.72f);                   // 청회색 — 붉은 적과 확실히 구분
        sr.sortingOrder = 5;                                          // 손/무기(6)보다 뒤
        spriteRenderer = sr;

        // 어느 쪽을 보는지 알 수 있게 코 하나. 네모만 있으면 방향이 안 읽힌다.
        var nose = new GameObject("Facing");
        nose.transform.SetParent(go.transform, false);
        nose.transform.localPosition = new Vector3(0.34f, 0f, 0f);   // 몸통 로컬 = 오른쪽
        nose.transform.localScale = new Vector3(0.28f, 0.34f, 1f);
        var nsr = nose.AddComponent<SpriteRenderer>();
        nsr.sprite = PlaceholderSprite.Square;
        nsr.color = new Color(0.78f, 0.86f, 0.94f);
        nsr.sortingOrder = 5;
    }

    /// <summary>손에 뭐가 들려 있나. **장착 아이템이 정한다** — 맨손이면 주먹이 보여야지 칼이 보이면 안 된다.
    /// 근접 무기는 WeaponData가 없는 것들이 많아(구형 무기) 카테고리로 판단한다.</summary>
    enum HandVisual { Fist, Melee, Gun }
    HandVisual InHand
    {
        get
        {
            var it = Equip != null ? Equip.EquippedWeapon : null;
            if (it == null) return HandVisual.Fist;
            if (it.weaponData != null && it.weaponData.isRanged) return HandVisual.Gun;
            return it.category == ItemCategory.Weapon ? HandVisual.Melee : HandVisual.Fist;
        }
    }

    /// <summary>칼 비주얼 갱신 — 바라보는 각 + 상태별 자세(차징/평상시). 스윙은 공격 시점에 1회 호출.</summary>
    void UpdateWeaponVisual()
    {
        // 셋 중 **하나만** 보인다. 안 그러면 총 쏘는데 칼이 같이 떠 있는 식이 된다.
        var hand = InHand;

        if (_weaponVis != null)
        {
            _weaponVis.SetVisible(hand == HandVisual.Melee);
            if (hand == HandVisual.Melee)
            {
                _weaponVis.SetFacing(FacingDirection);
                if (_state == CombatState.HeavyCharge) _weaponVis.Charge(ChargePercent);
                else if (!_weaponVis.IsSwinging && _state != CombatState.HeavyRelease
                         && _state != CombatState.LightAttack) _weaponVis.Rest();
            }
        }

        if (_fistVis != null)
        {
            _fistVis.SetVisible(hand == HandVisual.Fist && _character3D == null);
            if (hand == HandVisual.Fist)
            {
                _fistVis.SetFacing(FacingDirection);
                if (!_fistVis.IsPunching && _state != CombatState.HeavyRelease
                    && _state != CombatState.LightAttack) _fistVis.Rest();
            }
        }

        if (_gunVis != null)
        {
            _gunVis.SetVisible(hand == HandVisual.Gun);
            if (hand == HandVisual.Gun)
            {
                _gunVis.SetFacing(FacingDirection);
                _gunVis.SetState(_gun != null && _gun.IsAiming,
                                 _gun != null && _gun.IsReloading,
                                 _gun != null ? _gun.ReloadProgress : 0f);
            }
        }
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
                          && !ChannelBusy                                  // 아이템 사용(채널) 중 달리기 금지
                          && !OverweightSprintBlocked                      // 과적(100%+) 시 스프린트 불가(레이드만)
                          && GameInput.GetKey(KeyCode.LeftShift) && IsMoving
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

    /// <summary>아이템 사용(채널) 진행 중 — 이 동안 구르기/공격/달리기를 막는다.</summary>
    bool ChannelBusy => UseActionManager.Instance != null && UseActionManager.Instance.IsBusy;

    /// <summary>약공 콤보 사용 여부 — 2026-07-11 기본 OFF(사용자 결정). Control Panel에서 되살릴 수 있다.</summary>
    static bool ComboOn => GameTuning.Instance != null && GameTuning.Instance.comboEnabled;

    void HandleCombatInput()
    {
        if (ChannelBusy) return;   // 아이템 사용 중 — 구르기·공격 금지 (취소는 ESC)
        // 투척 조준 중 — 좌클릭=투척 / 우클릭=취소가 ThrowSystem에서 처리되므로 공격 입력 차단(중복 발동 방지).
        if (ThrowSystem.Instance != null && ThrowSystem.Instance.IsAiming) return;
        if (!CombatEnabled || _exhausted) return;

        // 구르기 (Space) — 총을 들고 있어도 구를 수 있다.
        if (GameInput.GetKeyDown(KeyCode.Space) && _state != CombatState.Dodge && _dodgeCooldownTimer <= 0f)
        {
            TryDodge();
            return;
        }

        // ★ 총기(2026-07-29) — 좌클릭=사격 / 우클릭=조준 / R=장전.
        //   근접 상태기계는 아예 안 탄다. 총기 분기를 아래 콤보·차징 코드에 섞으면
        //   애써 잡아 놓은 선입력·차징 타이밍이 같이 흔들린다. 입력만 갈라 준다.
        if (IsRangedEquipped)
        {
            if (_gun != null) _gun.HandleInput(false);
            return;
        }

        // ★ 약공 선입력 예약 — **반드시 아래 얼리 리턴보다 먼저.**
        //   (2026-07-11 버그픽스) 예전엔 이 분기가 `_state != Idle && != HeavyCharge → return` 뒤에 있어
        //   _state==LightAttack이면 도달 자체가 불가능했다 → _comboBuffered가 영원히 false →
        //   3타 콤보가 한 번도 발동 못 하고 매번 1타 + 쿨다운. "때려도 씹힌다"의 정체.
        if (ComboOn && _state == CombatState.LightAttack && GameInput.GetMouseButtonDown(0))
        {
            _comboBuffered = true;
            _comboBufferTimer = CurrentLightCombo != null ? CurrentLightCombo.bufferTime : 0.25f;
            return;
        }

        if (_state != CombatState.Idle && _state != CombatState.HeavyCharge) return;

        // 약공격 (좌클릭) — 콤보 1타 시작
        if (GameInput.GetMouseButtonDown(0))
        {
            if (_state == CombatState.Idle && _lightCooldownTimer <= 0f)
            {
                StartLightCombo();
                return;
            }
        }

        // 강공격 (우클릭 차징 → 떼면 발동)
        if (GameInput.GetMouseButtonDown(1) && _heavyCooldownTimer <= 0f && _state == CombatState.Idle)
        {
            _state = CombatState.HeavyCharge;
            _chargeTimer = 0f;
        }
        if (_state == CombatState.HeavyCharge)
        {
            _chargeTimer += Time.deltaTime;
            // 2026-07-11: 버튼을 떼는 순간을 놓쳐도(UI 열림·투척 조준·탈진 등으로 이 블록을 못 탄 프레임에
            //   뗀 경우) 차징에 **영구 고착**되던 문제 → GetMouseButton(1)이 이미 풀렸으면 즉시 발동시키고,
            //   최대 차징의 2배를 넘기면 강제 종료한다.
            if (GameInput.GetMouseButtonUp(1) || !GameInput.GetMouseButton(1))
                DoHeavyAttack();
            else if (_chargeTimer > HeavyMaxCharge * 2f)
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
        // 손에 든 것에 맞는 동작 — 칼은 휘두르고, 맨손은 정권으로 지른다.
        if (InHand == HandVisual.Melee) _weaponVis?.Swing(atk.Duration);   // 우 → 좌 한 방향
        else                            _fistVis?.Punch(atk.Duration);
        // 소음은 스윙이 아니라 '적중' 시에만 발생(AttackPerformer.ScanWindow) — 2026-07-11 변경.
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
        if (InHand == HandVisual.Melee) _weaponVis?.SwingHeavy(_attackStateTimer, full);   // 치켜든 대각에서 크고 빠르게
        else                            _fistVis?.PunchHeavy(_attackStateTimer, full);    // 더 깊은 정권
        // 강공도 적중 시에만 소음(AttackPerformer.ScanWindow).
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
                // 2026-07-11: 콤보는 GameTuning.comboEnabled로 게이트(기본 OFF — 사용자 결정).
                //   데이터는 그대로 두고 '진행'만 막는다 → 켜면 그대로 되살아난다.
                int lastStep = ComboOn ? (CurrentLightCombo != null ? CurrentLightCombo.StepCount : 0) - 1 : 0;
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
        {
            float regen = StamRegen * (Inv != null ? Inv.StaminaRegenMult : 1f);   // 심각 과적 시 회복 절반
            _stamina = Mathf.Min(MaxStam, _stamina + regen * Time.deltaTime);
        }
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
