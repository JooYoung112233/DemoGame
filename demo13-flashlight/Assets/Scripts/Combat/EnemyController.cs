using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 통합 컨트롤러 — 쿼터뷰 3D (Rigidbody 기반, XZ 평면 이동, NavMesh 없음).
/// 평면 수학은 Vector2(x=월드X, y=월드Z) 그대로 두고 물리·트랜스폼 경계에서만 Plan3D로 변환한다.
/// AI 상태머신(순찰/추격/예비동작/공격/피격/스턴/사망) + 그로기 시스템.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class EnemyController : MonoBehaviour
{
    #region 열거형 & 필드

    public enum State { Patrol, Chase, AttackWindup, Attack, Hit, Stunned, Dead, Investigate }

    /// <summary>활성 적 레지스트리 — PlayerVision(FOV 시야콘)이 순회.</summary>
    public static readonly List<EnemyController> All = new List<EnemyController>();
    bool _visionVisible = true;
    UnitLabel _label;   // 머리 위 "적"/"시체" 라벨 (그레이박스용)
    MeleeWeaponVisual _weaponVis;   // 그레이박스 칼 (연출 전용 — 판정은 AttackPerformer)
    GreyboxLimbs _limbs;            // 머리·몸통·팔·다리 실루엣 (부위 조준이 눈에 읽히게)
    bool _nextIsHeavy;              // 이번 공격이 강공인가 (예비동작 진입 시 결정)
    BanditEnemyVisual _banditVisual;
    float _banditAttackElapsed, _banditAttackDuration, _banditHitAt;
    bool _banditPendingHit;
    int  _lightStreak;              // 약공 연속 횟수 — N회 넘으면 강제로 강공

    // 원거리(총기 밴딧, 2026-09-11) — 조준 방향·점사 상태
    Vector2  _aimDir = Vector2.right;
    int      _shotsLeft;
    float    _shotTimer, _rangedAttackElapsed;
    bool     _losOk;
    float    _losCheckAt;
    Collider _playerBody;
    /// <summary>발사 직전 이만큼은 조준을 고정한다 — 고정된 조준선을 보고 옆으로 빠지면 피한다. (GameTuning)</summary>
    static float AimLockTime   => GameTuning.Instance != null ? GameTuning.Instance.enemyAimLockTime : 0.2f;
    /// <summary>마지막 탄 뒤 추격으로 돌아가기까지. (GameTuning)</summary>
    static float RangedRecover => GameTuning.Instance != null ? GameTuning.Instance.enemyRangedRecover : 0.35f;
    static float BulletRangeMult => GameTuning.Instance != null ? GameTuning.Instance.enemyBulletRangeMult : 1.25f;

    [Header("Detection")]
    [SerializeField] float detectRange    = 8f;
    [SerializeField] float attackRange    = 1.2f;
    [SerializeField] float loseRange      = 12f;

    [Header("Movement")]
    [SerializeField] float moveSpeed      = 2.5f;
    [SerializeField] float patrolSpeed    = 1.2f;
    [SerializeField] float patrolRadius   = 5f;
    [SerializeField] float patrolWaitTime = 2f;

    [Header("Combat")]
    [SerializeField] float attackDamage   = 15f;
    [SerializeField] float attackSpeed    = 0.67f;
    [SerializeField] float attackWindup   = 0.8f;

    [Header("Groggy")]
    [SerializeField] float maxGroggy           = 100f;
    [SerializeField] float groggyDecay         = 8f;
    [SerializeField] float groggyStunDuration  = 2.0f;

    [Header("Data")]
    [SerializeField] string unitKey;
    UnitStatData unitStat;

    [Header("Attack Hitbox")]
    [Tooltip("적 공격의 히트박스 타임라인 (비우면 즉시 데미지 폴백)")]
    [SerializeField] AttackData attackData;
    [Tooltip("타격 대상 레이어 (Player)")]
    [SerializeField] LayerMask playerMask = 1 << 6;

    [Header("References")]
    [SerializeField] SkeletonAnimController animController;

    // 런타임
    State          state = State.Patrol;
    Transform      player;
    Health         health;
    Health         playerHealth;
    Rigidbody      _rb;
    CombatFeedback feedback;
    AttackPerformer _performer;
    NavAgent       _nav;          // 격자 A* 길찾기 (추격 시)
    Vector2        _attackDir = Vector2.right;
    SpriteRenderer spriteRenderer;
    Renderer[]     renderers;
    Color          originalColor = Color.white;

    Vector2 spawnPos;
    Vector2 patrolTarget;
    Vector2 investigatePos;       // 소음 조사 지점
    float   investigateLook;      // 도착 후 두리번 타이머
    float   investigateTotal;     // 총 조사 경과(도달 불가 지점 무한 조사 방지 캡)
    GameObject alertMark;         // '?' 조사 표시(머리 위)
    float   patrolTimer;
    float   attackTimer;
    float   hitTimer;
    float   windupTimer;
    float   windupFlashTimer;

    // 그로기
    float currentGroggy;
    float stunTimer;
    bool  isStunned;

    // HP바
    GameObject hpBarBg, hpBarFill;
    Material   hpFillMat;
    const float BAR_W = 0.8f, BAR_H = 0.08f;

    // 그로기바
    GameObject groggyBarBg, groggyBarFill;
    Material   groggyFillMat;
    const float GROG_W = 0.6f, GROG_H = 0.06f;

    #endregion

    #region 프로퍼티

    // ── 부위 부상 디버프(2026-07-29) — 배율을 **여기 한 곳**에서만 곱한다.
    //    스탯을 읽는 자리마다 따로 곱하면 하나 빠뜨려도 아무도 모른다.
    UnitInjuries _injuries;
    UnitInjuries Inj => _injuries != null ? _injuries : (_injuries = GetComponent<UnitInjuries>());
    float InjMove   => Inj != null ? Inj.MoveMult        : 1f;
    float InjWindup => Inj != null ? Inj.WindupMult      : 1f;
    float InjAtk    => Inj != null ? Inj.AttackMult      : 1f;
    float InjDetect => Inj != null ? Inj.DetectMult      : 1f;
    float InjDecay  => Inj != null ? Inj.GroggyDecayMult : 1f;

    float Damage          => (unitStat != null ? unitStat.attackDamage        : attackDamage) * InjAtk;
    float AtkRange        => unitStat != null ? unitStat.attackRange         : attackRange;
    float AtkCooldown     => 1f / Mathf.Max(unitStat != null ? unitStat.attackSpeed : attackSpeed, 0.1f);
    float DetectRng       => (unitStat != null ? unitStat.detectRange : detectRange) * TraitManager.Mod("detect_radius");   // 잠행: 그림자 감지반경 -20%
    float LoseRng         => unitStat != null ? unitStat.loseRange           : loseRange;
    float MoveSpd         => (unitStat != null ? unitStat.moveSpeed          : moveSpeed) * InjMove;
    float PatrolSpd       => (unitStat != null ? unitStat.patrolSpeed        : patrolSpeed) * InjMove;
    float PatrolRad       => unitStat != null ? unitStat.patrolRadius        : patrolRadius;
    float HitStun         => unitStat != null ? unitStat.hitStunDuration     : 0.3f;
    float PatrolWait      => unitStat != null ? unitStat.patrolWaitTime      : patrolWaitTime;
    float Windup          => (unitStat != null ? unitStat.attackWindup       : attackWindup) * InjWindup;
    bool  CanBeCancelled  => unitStat != null ? unitStat.canBeCancelled      : true;
    float MaxGroggy       => unitStat != null ? unitStat.maxGroggy           : maxGroggy;
    float GroggyDecayRate => (unitStat != null ? unitStat.groggyDecay        : groggyDecay) * InjDecay;
    float StunDuration    => unitStat != null ? unitStat.groggyStunDuration  : groggyStunDuration;
    bool  IsRanged        => unitStat != null && unitStat.rangedWeapon != UnitStatData.RangedWeapon.None;
    float PreferredRange  => unitStat != null ? Mathf.Min(unitStat.preferredRange, unitStat.attackRange) : attackRange;

    public State CurrentState  => state;
    public bool  IsInWindup    => state == State.AttackWindup;
    public bool  IsStunned     => isStunned;
    public bool  IsDead        => state == State.Dead;

    // 총기 밴딧 표시(BanditEnemyVisual 총기 모드)가 읽는다
    public Transform Target       => player;
    public Vector2   AimDirection => _aimDir;
    public bool      AimLocked    => IsRanged && state == State.AttackWindup && windupTimer <= AimLockTime;
    public float     FireRange    => AtkRange;
    public float GroggyPercent => currentGroggy / MaxGroggy;

    /// <summary>이 적이 지금 플레이어와 교전 중인가(추격/예비동작/공격).
    /// 이 상태들은 플레이어를 탐지했을 때만 진입하므로 player가 살아있고 비어있지 않을 때만 true.
    /// (CombatStateTracker의 전투 감지용 읽기 전용 프로퍼티)</summary>
    public bool IsEngagingPlayer =>
        player != null &&
        (playerHealth == null || !playerHealth.IsDead) &&
        (state == State.Chase || state == State.AttackWindup || state == State.Attack);

    #endregion

    #region 유니티 생명주기

    public void SetUnitKey(string key)
    {
        unitKey = key;
        if (StatDB.Instance != null) unitStat = StatDB.Instance.GetUnit(unitKey);
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    /// <summary>FOV 시야콘이 호출 — 시야 밖이면 렌더러만 숨김(AI·충돌은 유지). 사망/시야 무관 처리는 호출부.</summary>
    public void SetVisionVisible(bool v)
    {
        if (_visionVisible == v) return;
        _visionVisible = v;
        if (spriteRenderer != null) spriteRenderer.enabled = v;
        // 팔다리가 있으면 **그쪽이 몸**이다 — 통짜 네모는 계속 꺼 둔다.
        //   안 그러면 시야에 들어오는 순간 네모가 다시 켜져 팔다리를 덮는다.
        if (_limbs != null)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            _limbs.SetVisible(v);
        }
        // 라벨은 스프라이트의 자식이지만 MeshRenderer라 위 한 줄로는 안 꺼진다 —
        // 안 끄면 시야 밖 적의 "적" 글자만 어둠 속에 떠서 위치가 노출된다.
        if (_label != null) _label.SetVisible(v);
        if (_weaponVis != null) _weaponVis.SetVisible(v);   // 칼도 루트의 자식이라 같이 꺼줘야 한다
        if (_banditVisual != null)
        {
            _banditVisual.SetVisible(v);
            if (spriteRenderer != null) spriteRenderer.enabled = false;
        }
        if (!v)
        {
            // ⚠️ **머리 위 표시물을 전부** 내린다. 예전엔 HP바만 껐는데, 몸(3D 상자)이
            //    사라진 자리에 그로기바·말풍선 같은 **납작한 2D 스프라이트만 남아 떠 있었다.**
            //    화면에는 "적이 아직 2D로 나온다"로 보이고, 동시에 시야 밖 적의 위치가
            //    그대로 노출된다 — 시야 콘의 의미가 사라진다.
            if (hpBarBg != null) hpBarBg.SetActive(false);
            if (hpBarFill != null) hpBarFill.SetActive(false);
            if (groggyBarBg != null) groggyBarBg.SetActive(false);
            if (groggyBarFill != null) groggyBarFill.SetActive(false);
            if (alertMark != null) alertMark.SetActive(false);
        }
        // 말풍선도 시야를 따른다 — 몸이 안 보이는데 말만 떠 있으면 그게 곧 위치 표시다.
        var bubble = GetComponent<EnemySpeechBubble>();
        if (bubble != null) bubble.SetVisionVisible(v);

        // 다시 보이면 UpdateHPBar/UpdateGroggyBar가 다음 프레임에 필요 시 재표시.
    }

    void Awake()
    {
        if (!string.IsNullOrEmpty(unitKey) && StatDB.Instance != null)
            unitStat = StatDB.Instance.GetUnit(unitKey);

        _rb                = GetComponent<Rigidbody>();
        _rb.isKinematic    = false;                        // 벽에 막히려면 Dynamic
        _rb.useGravity     = false;                        // 평면 이동 — 낙하는 아직 다루지 않는다
        _rb.constraints    = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // 질량이 기본값 1로 남아 있었다 — 플레이어(70)가 스치기만 해도 적이 날아갔고,
        // 특히 구르기(3m/0.3s = 10m/s)로 파고들면 볼링처럼 밀려났다. 적이 **길을 막는 것**은
        // 전투의 기본 압박이므로 밀리면 안 된다. 이동은 속도를 직접 넣어 굴리므로(SetVelocity)
        // 질량을 키워도 적 자신의 발걸음은 그대로다 — 충돌 반응만 단단해진다.
        _rb.mass = 300f;

        health    = GetComponent<Health>();
        feedback  = GetComponent<CombatFeedback>();

        _nav = GetComponent<NavAgent>();
        if (_nav == null) _nav = gameObject.AddComponent<NavAgent>();

        _performer = GetComponent<AttackPerformer>();
        if (_performer == null) _performer = gameObject.AddComponent<AttackPerformer>();
        _performer.Configure(playerMask, () => _attackDir);

        if (animController == null)
            animController = GetComponentInChildren<SkeletonAnimController>();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        renderers      = GetComponentsInChildren<Renderer>();

        // 그레이박스 식별 라벨("적" → 사망 시 "시체"). 프리팹 적이든 런타임 생성이든 여기 한 곳에서 보장.
        _label = GetComponentInChildren<UnitLabel>(true);
        if (_label == null && spriteRenderer != null)
            _label = UnitLabel.Attach(transform, "적", UnitLabel.EnemyColor, 0, LABEL_Y);

        // 그레이박스 칼 — 플레이어와 같은 연출을 적도 쓴다(예비동작이 눈에 보여야 캔슬을 노릴 수 있다).
        _weaponVis = MeleeWeaponVisual.Attach(transform, new Color(0.80f, 0.70f, 0.66f), 0.95f);

        // 팔다리 실루엣(2026-07-29) — 통짜 사각형이면 부위를 어디로 노리는 건지 알 수가 없다.
        //   몸통 스프라이트 밑에 붙여 ApplyUnitLook의 크기 조절이 그대로 먹게 한다.
        if (spriteRenderer != null)
        {
            // 몸은 **루트**에 붙인다(앵커가 아니라) — 앵커의 비균등 스케일을 타면
            // 3D에서 폭보다 두꺼운 몸이 나온다. 크기는 ApplyUnitLook이 SetScale로 준다.
            _limbs = GreyboxLimbs.Attach(transform, spriteRenderer.color);
            spriteRenderer.enabled = false;   // 통짜 네모는 끈다 — 겹쳐 그리면 구획이 안 보인다
        }

        // 테스트용 부위 표시(F1에서 켠다, 기본 꺼짐) — 다리를 노렸는지 눈으로 확인할 유일한 수단.
        BodyZoneOverlay.Attach(transform);

        // 부위 부상(2026-07-29) — 다리를 부수면 못 쫓아오고, 팔을 부수면 느리게 때린다.
        _injuries = GetComponent<UnitInjuries>();
        if (_injuries == null) _injuries = gameObject.AddComponent<UnitInjuries>();

        // 적 은신 + 머리 위 말풍선 (가시성 실험) — 자동 부착
        if (GetComponent<EnemySpeechBubble>() == null)
            gameObject.AddComponent<EnemySpeechBubble>();
    }

    void Start()
    {
        spawnPos = Plan3D.ToPlan(transform.position);

        AcquirePlayer();

        if (health != null)
        {
            if (unitStat != null && unitStat.maxHp > 0f)   // 유닛 스탯의 최대 HP 적용(없으면 Health 인스펙터 기본값)
            {
                health.SetMaxHp(unitStat.maxHp);
                // 부상 임계치를 그 유닛의 HP에 맞춘다 — HP 110짜리 탱커와 40짜리 밴딧이 같으면 안 된다.
                if (Inj != null) Inj.Setup(unitStat.maxHp);
                health.FullHeal();
            }
            health.OnDamaged += OnDamaged;
            health.OnDeath   += OnDeath;
        }

        ApplyUnitLook();   // 크기·색으로 종류를 구분 — 여태 StatDB의 scale/tintColor가 **한 번도 안 쓰였다**
        if (!string.IsNullOrEmpty(unitKey) && unitKey.StartsWith("bandit_", System.StringComparison.Ordinal))
        {
            var visual = gameObject.AddComponent<BanditEnemyVisual>();
            float scale = unitStat != null ? Mathf.Clamp(unitStat.scale / 2f, .5f, 3f) : 1f;
            // 총기 밴딧은 같은 몸에 총 본이 추가된 리그를 쓴다(Resources/Characters/BanditPistol·BanditRifle).
            string model = !IsRanged ? "Characters/Bandit01"
                : unitStat.rangedWeapon == UnitStatData.RangedWeapon.Rifle ? "Characters/BanditRifle" : "Characters/BanditPistol";
            if (visual.Initialize(this, scale, model))
            {
                _banditVisual = visual;
                if (_limbs != null) { Destroy(_limbs.gameObject); _limbs = null; }
                if (_weaponVis != null) { Destroy(_weaponVis.gameObject); _weaponVis = null; }
                if (animController != null) { animController.enabled = false; animController = null; }
                if (spriteRenderer != null) spriteRenderer.enabled = false;
                _banditVisual.SetVisible(_visionVisible);
            }
            else Destroy(visual);
        }

        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        // 표시물은 **모델이 정해진 뒤** 올린다 — ApplyUnitLook 시점엔 아직 그레이박스 팔다리라
        //   그때 잰 높이로 두면 3D 밴딧(특히 중장 2.36m)에선 다시 몸에 묻힌다.
        if (_label != null)
        {
            var lp = _label.transform.localPosition;
            _label.transform.localPosition = new Vector3(lp.x, HeadTop() + LABEL_GAP, lp.z);
        }
        CreateHPBar();
        CreateGroggyBar();
        SetPatrolTarget();
    }

    /// <summary>플레이어 참조 확보. 2026-07-11: 예전엔 Start에서 **한 번만** 찾았다 —
    /// 그때 플레이어가 아직 없었거나(씬 로드 순서) 이후 재생성되면 그 적은 탐지·추격·공격을
    /// **영영 한 번도 안 했다**("공격범위 안인데 안 때리는 개체가 있다"의 한 갈래).
    /// 이제 비어 있으면 주기적으로 다시 찾는다(찾을 때까지만 도는 저비용 루프).</summary>
    void AcquirePlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return;
        player       = go.transform;
        playerHealth = go.GetComponent<Health>();
        // 사선 확인 대상 = 플레이어의 **솔리드 몸**(트리거 허트박스가 아니라)
        _playerBody = null;
        foreach (var c in go.GetComponents<Collider>())
            if (!c.isTrigger) { _playerBody = c; break; }
    }

    float _reacquireAt;

    void Update()
    {
        if (state == State.Dead) return;
        attackTimer -= Time.deltaTime;

        if (player == null && Time.time >= _reacquireAt)   // 참조 유실 복구(0.5초 간격)
        {
            _reacquireAt = Time.time + 0.5f;
            AcquirePlayer();
        }

        switch (state)
        {
            case State.Patrol:       UpdatePatrol();       break;
            case State.Investigate:  UpdateInvestigate();  break;
            case State.Chase:        UpdateChase();        break;
            case State.AttackWindup: UpdateAttackWindup(); break;
            case State.Attack:       UpdateAttack();       break;
            case State.Hit:          UpdateHit();          break;
            case State.Stunned:      UpdateStunned();      break;
        }

        UpdateWeaponVisual();
        UpdateGroggy();
        UpdateGroggyBar();
        UpdateHPBar();
        UpdateInjuryBadge();
    }

    #endregion

    #region AI 상태

    void UpdatePatrol()
    {
        if (player != null && DistToPlayer() < DetectRng)
        {
            state = State.Chase;
            StoryTriggerManager.Instance?.OnFirstCombatEncounter();
            return;
        }

        // 유인(던진 물건 착탄) — 반경 안이면 그 지점으로 조사하러 이동. 소음 시스템은 2026-09-09 폐기.
        if (Distraction.TrySense(Plan3D.ToPlan(transform.position), out var src))
        {
            EnterInvestigate(src);
            return;
        }

        Vector2 toTarget = patrolTarget - Plan3D.ToPlan(transform.position);
        if (toTarget.magnitude < 0.4f)
        {
            SetVelocity(Vector2.zero);
            animController?.Play("idle");
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= PatrolWait) SetPatrolTarget();
        }
        else
        {
            Vector2 dir = toTarget.normalized;
            SetVelocity(dir * PatrolSpd);
            FlipSprite(dir);
            animController?.Play("walk");
        }
    }

    void EnterInvestigate(Vector2 pos)
    {
        investigatePos = pos;
        investigateLook = 0f;
        investigateTotal = 0f;
        state = State.Investigate;
        ShowAlertMark(true);
    }

    /// <summary>소음 조사 — 소리 지점으로 이동 → 두리번 → 순찰 복귀. 도중 시야로 플레이어 발견 시 추격.</summary>
    void UpdateInvestigate()
    {
        // 시야로 실제 발견 → 추격 전환(우선).
        if (player != null && DistToPlayer() < DetectRng)
        {
            ShowAlertMark(false);
            state = State.Chase;
            StoryTriggerManager.Instance?.OnFirstCombatEncounter();
            return;
        }

        // 더 가까운/새 유인이 생기면 지점 갱신 + 총 조사시간 리셋(계속 던지면 계속 따라옴).
        if (Distraction.TrySense(Plan3D.ToPlan(transform.position), out var src))
        {
            investigatePos = src;
            investigateTotal = 0f;
        }

        // 총 조사시간 캡 — 도달 불가 지점(벽 뒤)에서 영영 못 벗어나는 것 방지.
        investigateTotal += Time.deltaTime;
        float look = GameTuning.Instance != null ? GameTuning.Instance.noiseInvestigateLook : 2.5f;
        if (investigateTotal >= look * 4f)   // 도착 못 해도 이만큼 지나면 포기
        {
            ShowAlertMark(false);
            state = State.Patrol;
            SetPatrolTarget();
            return;
        }

        Vector2 to = investigatePos - Plan3D.ToPlan(transform.position);
        if (to.magnitude > 0.6f)
        {
            Vector2 d;
            if (_nav != null)
            {
                _nav.SetDestinationPlan(investigatePos);
                d = _nav.DesiredDirection;
                if (d.sqrMagnitude < 0.0001f) d = to.normalized;
            }
            else d = to.normalized;

            SetVelocity(d * PatrolSpd);
            FlipSprite(d);
            animController?.Play("walk");
        }
        else
        {
            // 도착 → 두리번(대기).
            SetVelocity(Vector2.zero);
            _nav?.Stop();
            animController?.Play("idle");
            investigateLook += Time.deltaTime;
            if (investigateLook >= look)   // look = 위에서 계산됨
            {
                ShowAlertMark(false);
                state = State.Patrol;
                SetPatrolTarget();
            }
        }
    }

    void UpdateChase()
    {
        if (player == null || (playerHealth != null && playerHealth.IsDead))
        {
            state = State.Patrol;
            _nav?.Stop();
            SetVelocity(Vector2.zero);
            animController?.Play("idle");
            return;
        }

        float dist = DistToPlayer();

        if (dist > LoseRng) { state = State.Patrol; _nav?.Stop(); SetPatrolTarget(); return; }

        if (IsRanged) { UpdateRangedChase(dist); return; }

        if (dist <= AtkRange && attackTimer <= 0)
        {
            // ── 강공 판단 (2026-07-11) ──
            //   확률만 두면 한 판 내내 강공이 한 번도 안 나오는 경우가 생긴다 →
            //   약공을 N회 연속 낸 뒤엔 **반드시** 강공(패턴을 읽을 수 있게).
            var gt = GameTuning.Instance;
            float hChance = gt != null ? gt.enemyHeavyChance : 0.3f;
            int   force   = gt != null ? gt.enemyHeavyForceAfter : 3;
            _nextIsHeavy = CanBeCancelled ? (_lightStreak >= force || Random.value < hChance)
                                          : true;   // 캔슬 불가 유닛(중장) = 항상 큰 공격

            state = State.AttackWindup;
            _nav?.Stop();
            windupTimer = Windup * (_nextIsHeavy ? (gt != null ? gt.enemyHeavyWindupMult : 1.9f) : 1f);
            windupFlashTimer = 0;
            SetVelocity(Vector2.zero);
            FacePlayer();
            _banditVisual?.Windup(windupTimer);
            return;
        }

        // 2026-07-11: **정지거리(standoff)** — 공격 쿨다운 중엔 더 파고들지 않는다.
        //   예전엔 쿨다운 동안에도 계속 전속력으로 밀고 들어와 거리 0이 되도록 겹쳤다(양쪽 Dynamic RB라
        //   플레이어가 물리적으로 떠밀림) → 조준·거리감이 무의미. 이제 사거리의 0.85배 안쪽이면 멈추고 대기.
        float standoff = AtkRange * 0.85f;
        if (dist <= standoff)
        {
            _nav?.Stop();
            SetVelocity(Vector2.zero);
            FacePlayer();
            animController?.Play("idle");
            return;
        }

        MoveTowardPlayer(dist);
    }

    /// <summary>플레이어 쪽으로 걷는다(길찾기 + 동료 분리). 근접·총기 추격 공용.</summary>
    void MoveTowardPlayer(float dist)
    {
        // 길찾기 방향(벽 우회). 경로 없으면 직진 폴백.
        Vector2 d;
        if (_nav != null)
        {
            _nav.SetDestination(player.position);
            d = _nav.DesiredDirection;
            if (d.sqrMagnitude < 0.0001f)
                d = (Plan3D.ToPlan(player.position) - Plan3D.ToPlan(transform.position)).normalized;
        }
        else
        {
            d = (Plan3D.ToPlan(player.position) - Plan3D.ToPlan(transform.position)).normalized;
        }

        // 2026-07-11: 적끼리 **분리(separation)** — 예전엔 반발이 없어 3마리가 한 점에 겹쳐
        //   한 덩어리로 밀려들었다. 근처 동료에게서 밀어내는 성분을 섞어 대열이 퍼지게 한다.
        //
        // ★ 단, **접근 중일 때만.** 사거리 근처에선 0으로 죽인다 —
        //   안 그러면 플레이어 옆에 모인 둘이 서로 밀어내며 사거리 밖을 맴돌아
        //   **아무도 공격하지 않는다**(2026-07-11 사용자 보고: "공격범위 들어와도 안 때리는 경우가 있다").
        //   합성 벡터도 1로 클램프 — 분리 성분이 추격 방향을 이기지 못하게.
        float sepW = Mathf.InverseLerp(AtkRange, AtkRange * 2f, dist) * 0.6f;
        if (sepW > 0.001f)
            d = (d + Vector2.ClampMagnitude(Separation(), 1f) * sepW).normalized;

        SetVelocity(d * MoveSpd);
        FlipSprite(d);
        animController?.Play("walk");
    }

    /// <summary>근처 동료로부터 밀어내는 방향(정규화 전 합). 겹침 방지용 — 반경 안에서 거리 반비례.</summary>
    Vector2 Separation()
    {
        const float R = 1.1f;
        Vector2 me = Plan3D.ToPlan(transform.position), push = Vector2.zero;
        for (int i = 0; i < All.Count; i++)
        {
            var o = All[i];
            if (o == null || o == this || o.state == State.Dead) continue;
            Vector2 diff = me - Plan3D.ToPlan(o.transform.position);
            float dsq = diff.sqrMagnitude;
            if (dsq > R * R || dsq < 0.0001f) continue;
            push += diff.normalized * (1f - Mathf.Sqrt(dsq) / R);
        }
        return push;
    }

    /// <summary>유닛 종류를 **눈으로 구분**되게 — StatDB의 tintColor/scale을 실제로 적용한다.
    ///
    /// 2026-07-11: 이 두 값은 데이터에 있으면서 **한 번도 읽히지 않았다**(모든 적이 같은 크기·같은 붉은색).
    /// scale은 절대값이 아니라 **일반 적(2.0) 기준 배율**로 쓴다 — 루트에 곧바로 곱하면
    /// 콜라이더까지 2배가 돼 물리가 통째로 바뀐다. 몸통·허트박스·칼만 비례 확대한다.</summary>
    void ApplyUnitLook()
    {
        if (unitStat == null) return;

        // 약탈자는 보랏빛 식별색이 우선 — 여기서 덮으면 회수 대상 구분이 사라진다(MakeScavenger는 Start 전에 호출됨).
        if (spriteRenderer != null && !_isScavenger)
        {
            spriteRenderer.color = unitStat.tintColor;
            // ⚠️ 팔다리는 Awake에서 **그 전 색으로** 이미 만들어졌다. 여기서 같이 밀어주지
            //    않으면 StatDB의 종류별 식별색이 화면에 전혀 안 나타난다(전부 같은 색으로 보인다).
            if (_limbs != null) _limbs.SetTint(unitStat.tintColor);
        }

        const float BaseScale = 2f;   // bandit_melee_1 기준
        float mul = Mathf.Clamp(unitStat.scale / BaseScale, 0.5f, 3f);
        // 표시물(HP·그로기·이름표) 높이도 같은 배율을 탄다 — 고정 높이로 두면 큰 유닛에선
        // 바가 **가슴에 묻힌다**(중장 모델 2.30m vs 바 2.02m. 화면에선 바닥에 깔린 것처럼 보인다).
        _visualScale = mul;
        if (_limbs != null) _limbs.SetScale(mul);   // 몸은 배율을 매번 받는다(누적 곱이 아니다)
        if (Mathf.Approximately(mul, 1f)) return;

        if (spriteRenderer != null)
            spriteRenderer.transform.localScale *= mul;

        var body = GetComponent<CapsuleCollider>();
        if (body != null) body.radius *= mul;

        var hurt = GetComponentInChildren<Hurtbox>();
        var hbox = hurt != null ? hurt.GetComponent<BoxCollider>() : null;
        if (hbox != null) hbox.size *= mul;

        if (_weaponVis != null) _weaponVis.transform.localScale = Vector3.one * mul;
    }

    /// <summary>칼 방향/자세 — 예비동작은 UpdateAttackWindup이, 스윙은 DoAttack이 따로 건다.</summary>
    void UpdateWeaponVisual()
    {
        if (_weaponVis == null) return;
        if (player != null && state != State.Dead)
            _weaponVis.SetFacing((Plan3D.ToPlan(player.position) - Plan3D.ToPlan(transform.position)).normalized);
        if (state != State.AttackWindup && state != State.Attack && !_weaponVis.IsSwinging)
            _weaponVis.Rest();
    }

    void UpdateAttackWindup()
    {
        if (IsRanged) { UpdateRangedAim(); return; }
        windupTimer -= Time.deltaTime;
        _banditVisual?.SetWindupRemaining(windupTimer);
        SetVelocity(Vector2.zero);
        windupFlashTimer += Time.deltaTime;
        // 강공은 더 진하게/느리게 점멸 — 약공과 구분되어야 피하거나 캔슬을 노릴 수 있다.
        float blinkHz = _nextIsHeavy ? 9f : 15f;
        Color warn = _nextIsHeavy ? new Color(1f, 0.45f, 0f) : new Color(1f, 0.2f, 0.2f);
        SetTint(Mathf.Sin(windupFlashTimer * blinkHz) > 0 ? warn : originalColor);
        // 예비동작 = 칼을 오른쪽 뒤로 당긴 자세 + 떨림. 붉은 점멸만 있을 땐 "뭘 하는지" 안 읽혔다.
        float full = Windup > 0f ? Mathf.Clamp01(1f - windupTimer / Windup) : 1f;
        _weaponVis?.Charge(_nextIsHeavy ? full : full * 0.45f);
        if (windupTimer <= 0) { RestoreTint(); DoAttack(); }
    }

    void UpdateAttack()
    {
        if (IsRanged) { UpdateRangedFire(); return; }
        if (_banditVisual != null)
        {
            SetVelocity(Vector2.zero);
            _banditAttackElapsed += Time.deltaTime;
            _banditVisual.SetAttackElapsed(_banditAttackElapsed);
            if (_banditPendingHit && _banditAttackElapsed >= _banditHitAt)
            {
                _banditPendingHit = false;
                ImmediateMeleeHit();
            }
            if (_banditAttackElapsed >= _banditAttackDuration)
            {
                _banditVisual.CancelAttack(); state = State.Chase;
            }
            return;
        }
        if (animController == null || animController.IsAnimComplete)
            state = State.Chase;
    }

    void UpdateHit()
    {
        hitTimer -= Time.deltaTime;
        SetVelocity(Vector2.zero);
        if (hitTimer <= 0 || (animController != null && animController.IsAnimComplete))
        {
            RestoreTint();
            state = State.Chase;
        }
    }

    void UpdateStunned()
    {
        SetVelocity(Vector2.zero);
        SetTint(Mathf.Sin(Time.time * 6f) > 0 ? new Color(1f, 1f, 0.2f) : new Color(0.6f, 0.6f, 0.1f));
        animController?.Play("idle");
    }

    #endregion

    #region 공격

    void DoAttack()
    {
        if (IsRanged) { BeginRangedFire(); return; }
        state       = State.Attack;
        attackTimer = AtkCooldown;
        windupFlashTimer = 0;
        FacePlayer();

        Vector2 dir = player != null
            ? (Plan3D.ToPlan(player.position) - Plan3D.ToPlan(transform.position)).normalized
            : _attackDir;
        _attackDir = dir;
        if (_banditVisual != null)
        {
            _banditAttackElapsed = 0;
            _banditAttackDuration = attackData != null ? attackData.Duration : .65f;
            _banditHitAt = _banditAttackDuration * .34f;
            if (attackData != null && attackData.windows.Count > 0)
            {
                int first = int.MaxValue;
                foreach (var window in attackData.windows) first = Mathf.Min(first, window.startFrame);
                _banditHitAt = first / Mathf.Max(1f, attackData.fps);
            }
            _banditPendingHit = attackData == null;
            _banditVisual.Attack(_banditAttackDuration, _banditHitAt);
            if (attackData != null) _performer.Perform(attackData);
            if (_nextIsHeavy) _lightStreak = 0; else _lightStreak++;
            return;
        }
        _weaponVis?.SetFacing(dir);
        // 약공/강공 모두 **우 → 좌**. 강공은 뒤로 당겼다가 더 빠르게(SwingHeavy).
        if (_nextIsHeavy) { _weaponVis?.SwingHeavy(0.26f, true); _lightStreak = 0; }
        else              { _weaponVis?.Swing(0.34f);            _lightStreak++;  }

        // 2026-07-11: 공격 런지(전진→원위치 하드 스냅) 제거 — 사용자 피드백 "때릴 때 앞뒤로 움직인다".
        //   Rigidbody 위에서 transform을 직접 되돌리는 연출이라 고무줄처럼 튕겨 보였다.
        //   공격은 제자리에서, 피드백은 '맞았을 때 히트 표기'(HitFlash/팝업)만 남긴다.

        // 히트박스 타임라인이 있으면 프레임 기반 판정, 없으면 즉시 데미지 폴백
        if (attackData != null)
        {
            _performer.Perform(attackData);
            animController?.PlayOneShot("attack");
            if (animController == null) state = State.Chase;
        }
        else
        {
            animController?.PlayOneShot("attack", () => ImmediateMeleeHit());
            if (animController == null)
            {
                ImmediateMeleeHit();
                state = State.Chase;
            }
        }
    }

    /// <summary>AttackData 없을 때의 즉시 근접 판정 폴백.</summary>
    void ImmediateMeleeHit()
    {
        if (player == null) return;

        // 2026-07-11: 판정을 **진입 조건과 일치**시키고 **정면 제한**을 건다.
        //   구: `AtkRange * 1.5f` 원형(360°) → ① 추격 정지 거리보다 1.5배 넓게 맞고
        //       ② 적 뒤로 돌아가도 맞아서 "왜 맞았는지 모르겠다"가 됐다.
        //   신: 사거리 그대로 + 예비동작 때 바라본 방향(_attackDir) 기준 ±60° 안에서만 적중.
        // 강공은 리치도 조금 길다(크게 휘두르니까) — 대신 예비동작이 두 배 가까이 길다.
        float reach = AtkRange * (_nextIsHeavy ? 1.20f : 1.05f);
        if (DistToPlayer() > reach) return;

        Vector2 toPlayer = (Plan3D.ToPlan(player.position) - Plan3D.ToPlan(transform.position)).normalized;
        Vector2 face = _attackDir.sqrMagnitude > 0.0001f ? _attackDir.normalized : toPlayer;
        if (Vector2.Dot(face, toPlayer) < 0.5f) return;   // cos60° — 등 뒤/옆은 빗나감

        float dmgMult = _nextIsHeavy
            ? (GameTuning.Instance != null ? GameTuning.Instance.enemyHeavyDamageMult : 1.9f) : 1f;
        float dmg = Damage * dmgMult;
        playerHealth?.TakeDamage(dmg);
        DamagePopup.Create(player.position, dmg,
            _nextIsHeavy ? DamagePopup.DamageType.Heavy : DamagePopup.DamageType.Normal);
    }

    #region 원거리 (총기 밴딧, 2026-09-11)
    // docs/bandit-firearms.md §레이드 배치 결정 — 조준 경고 후 **날아가는 총알**(보고 피할 수 있다).
    //   근접과 같은 상태(AttackWindup=조준, Attack=발사)를 쓰므로 피격 캔슬·스턴·사망 흐름을 그대로 탄다.

    Vector2 DirToPlayer() => player != null
        ? (Plan3D.ToPlan(player.position) - Plan3D.ToPlan(transform.position)).normalized
        : _aimDir;

    /// <summary>사선이 트이고 사거리 안이면 멈춰 조준, 트였지만 멀면 선호 거리까지만 다가간다.
    /// 벽에 가리면 길찾기로 돌아 들어온다.</summary>
    void UpdateRangedChase(float dist)
    {
        bool los = HasLineOfFire();
        bool gunReady = _banditVisual == null || _banditVisual.GunReady;   // 총을 다 꺼내기 전엔 조준하지 않는다
        if (los && gunReady && dist <= AtkRange && attackTimer <= 0)
        {
            _nextIsHeavy = false;   // 총기엔 강공이 없다 — 조준 중에 맞히면 캔슬된다(근접 약공과 같은 규칙)
            state = State.AttackWindup;
            _nav?.Stop();
            windupTimer = Windup;
            windupFlashTimer = 0;
            SetVelocity(Vector2.zero);
            _aimDir = DirToPlayer();
            FacePlayer();
            return;
        }
        if (los && dist <= PreferredRange)
        {
            _nav?.Stop();
            SetVelocity(Vector2.zero);
            FacePlayer();
            return;
        }
        MoveTowardPlayer(dist);
    }

    /// <summary>조준 경고. 끝 <see cref="AimLockTime"/> 전까지 플레이어를 따라가다 고정된다.</summary>
    void UpdateRangedAim()
    {
        windupTimer -= Time.deltaTime;
        SetVelocity(Vector2.zero);
        if (windupTimer > AimLockTime && player != null) { _aimDir = DirToPlayer(); FacePlayer(); }
        windupFlashTimer += Time.deltaTime;
        SetTint(Mathf.Sin(windupFlashTimer * 15f) > 0 ? new Color(1f, 0.2f, 0.2f) : originalColor);
        if (windupTimer <= 0) { RestoreTint(); DoAttack(); }
    }

    void BeginRangedFire()
    {
        state = State.Attack;
        attackTimer = AtkCooldown;
        windupFlashTimer = 0;
        _shotsLeft = Mathf.Max(1, unitStat.burstCount);
        _shotTimer = 0f;
        _rangedAttackElapsed = 0f;
    }

    void UpdateRangedFire()
    {
        SetVelocity(Vector2.zero);
        _rangedAttackElapsed += Time.deltaTime;
        _shotTimer -= Time.deltaTime;
        if (_shotsLeft > 0)
        {
            if (_shotTimer <= 0f)
            {
                FireShot();
                _shotsLeft--;
                _shotTimer = unitStat.burstInterval;
                _rangedAttackElapsed = 0f;
            }
        }
        else if (_rangedAttackElapsed >= RangedRecover) state = State.Chase;
    }

    static readonly Color EnemyTracer = new Color(1f, 0.55f, 0.2f);

    /// <summary>탄 한 발 — 실제 <see cref="Projectile"/>. 벽·엄폐·다른 적의 몸에 막힌다.</summary>
    void FireShot()
    {
        Vector3 muzzle = _banditVisual != null ? _banditVisual.MuzzlePosition : transform.position + Vector3.up;
        // 탄 높이는 플레이어 허트박스 안(0.6~1.3m)으로 — 총구가 어깨 위면 머리 위로 날아간다.
        float y = transform.position.y + Mathf.Clamp(muzzle.y - transform.position.y, 0.6f, 1.3f);
        float spread = Random.Range(-unitStat.spreadDeg, unitStat.spreadDeg) * Mathf.Deg2Rad;
        float c = Mathf.Cos(spread), s = Mathf.Sin(spread);
        Vector2 dir = new Vector2(_aimDir.x * c - _aimDir.y * s, _aimDir.x * s + _aimDir.y * c);
        Projectile.Spawn(transform, Plan3D.ToPlan(muzzle), dir, unitStat.projectileSpeed, Damage, 0f,
                         AtkRange * BulletRangeMult, EnemyTracer, 0.45f, y);
        _banditVisual?.NotifyShot();
    }

    static readonly RaycastHit[] _allyBuf = new RaycastHit[8];
    static int _enemyMask = -1;

    /// <summary>몸 높이에서 플레이어 몸까지 벽도 동료도 없나. 0.15초 간격으로만 잰다.</summary>
    bool HasLineOfFire()
    {
        if (player == null) return false;
        if (Time.time < _losCheckAt) return _losOk;
        _losCheckAt = Time.time + 0.15f;
        Vector3 from = transform.position + Vector3.up;
        _losOk = _playerBody == null || !AttackPerformer.IsBlockedByWall(from, _playerBody);
        // 벽 판정은 캐릭터를 무시하지만 총알은 다른 적의 몸에 막힌다 — 동료 등 뒤에서 쏘면 점사를 통째로 버린다.
        if (_losOk) _losOk = !AllyInLine(from);
        return _losOk;
    }

    bool AllyInLine(Vector3 from)
    {
        if (_enemyMask == -1)
        {
            int l = LayerMask.NameToLayer("Enemy");
            _enemyMask = l >= 0 ? 1 << l : 0;
        }
        if (_enemyMask == 0) return false;
        Vector3 d = player.position + Vector3.up - from;
        float dist = d.magnitude;
        if (dist < 0.05f) return false;
        int n = Physics.RaycastNonAlloc(from, d / dist, _allyBuf, dist, _enemyMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var c = _allyBuf[i].collider;
            if (c == null || c.transform == transform || c.transform.IsChildOf(transform)) continue;
            return true;
        }
        return false;
    }

    #endregion

    public void TryCancelAttack()
    {
        if (!CanBeCancelled || state != State.AttackWindup) return;
        // 강공은 캔슬되지 않는다 — 길게 예고하는 대신 "확정으로 나간다"가 압박이 된다.
        //   (예비동작이 길다고 공짜로 끊기면 강공이 그냥 손해가 된다.)
        if (_nextIsHeavy) return;
        state    = State.Hit;
        hitTimer = HitStun * 1.5f;
        _cancelBonusUntil = Time.time + 0.05f;   // 직후 OnDamaged가 hitTimer를 덮어쓰지 못하게(아래 참조)
        _performer?.Cancel();                    // 캔슬된 공격의 히트박스가 계속 판정 내는 것 방지
        _banditPendingHit = false;
        _banditVisual?.CancelAttack();
        SetVelocity(Vector2.zero);
        RestoreTint();
        SetTint(new Color(1f, 1f, 0.5f));
        animController?.PlayOneShot("gethit");
    }

    /// <summary>예비동작 캔슬 보너스(1.5배 경직) 보호 창 — 같은 타격의 OnDamaged가 곧바로 덮어쓰는 것을 막는다.
    /// (2026-07-11: TryCancelAttack → TakeDamage → OnDamaged 순서라 보너스가 항상 무효화되던 버그.)</summary>
    float _cancelBonusUntil;

    #endregion

    #region 그로기

    public void AddGroggy(float amount)
    {
        if (isStunned) return;
        currentGroggy = Mathf.Min(currentGroggy + amount, MaxGroggy);
        if (currentGroggy >= MaxGroggy) { isStunned = true; stunTimer = StunDuration; OnGroggyTriggered(); }
    }

    /// <summary>플레이어 근접 공격 명중 — 데미지 + 그로기 + 넉백 + 팝업.
    /// (Health.TakeDamage가 OnDamaged 콜백으로 Hit 상태 전환/연출을 처리)</summary>
    public void TakeHit(float damage, float groggy, Vector2 knockbackDir = default)
    {
        if (state == State.Dead) return;

        // windup 중이면 캔슬 시도 (성공 시 Hit 상태로)
        if (state == State.AttackWindup) TryCancelAttack();

        if (health != null) health.TakeDamage(damage);
        // 머리를 다치면 더 쉽게 무너진다 — 부위마다 다른 이득이 있어야 조준에 선택이 생긴다.
        float groggyTaken = Inj != null ? Inj.GroggyTakenMult : 1f;
        AddGroggy(groggy * TraitManager.Mod("groggy_buildup") * groggyTaken);   // 전투: 냉정한 손 +20%

        DamagePopup.Create(transform.position, damage, DamagePopup.DamageType.Normal);

        if (knockbackDir != Vector2.zero && _rb != null)
            _rb.AddForce(Plan3D.ToWorld(knockbackDir.normalized) * 3f, ForceMode.Impulse);
    }

    void UpdateGroggy()
    {
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0) { isStunned = false; currentGroggy = 0; OnGroggyRecovered(); }
        }
        else if (currentGroggy > 0)
        {
            currentGroggy = Mathf.Max(0, currentGroggy - GroggyDecayRate * Time.deltaTime);
        }
    }

    #endregion

    #region HP바 / 그로기바 (2D SpriteRenderer)

    /// <summary>머리 위 표시물 높이(m). 3D 몸(<see cref="GreyboxLimbs.Height"/> 1.8m) 위로 띄운다.
    /// 예전 값(HP 0.7 / 그로기 0.85 / 라벨 0)은 원점 중심 1m 몸 기준이라, 몸이 제 키로 서자
    /// **가슴에 박히거나 발치에 깔렸다.** 아래에서 위로: HP → 그로기 → 이름표.</summary>
    const float HP_BAR_Y = 2.02f, GROGGY_BAR_Y = 2.18f, LABEL_Y = 2.42f;

    /// <summary>유닛 크기 배율(StatDB.scale 기준). 모델 실측이 실패했을 때만 쓰는 폴백.</summary>
    float _visualScale = 1f;

    /// <summary>표시물을 올릴 기준 높이 = **이 적 모델의 실제 정수리**(루트 기준, m).
    ///
    /// 상수 × 배율로 잡으면 모델이 바뀔 때마다 어긋난다 — 실제로 중장(1.3배)은 모델이 2.36m인데
    /// 바가 2.02에 있어 **가슴에 묻혔고**, 일반 밴딧도 여유가 0.2m뿐이라 머리에 달라붙어 보였다.
    /// 렌더러 바운즈에서 정수리를 직접 재고, 못 재면 예전 상수로 물러선다.</summary>
    float HeadTop()
    {
        float top = float.MinValue;
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            // 표시물 자신(바·이름표)은 기준에서 뺀다 — 안 그러면 매 프레임 위로 밀려 올라간다.
            if (r is SpriteRenderer) continue;
            if (r.GetComponentInParent<UnitLabel>() != null) continue;
            // 3D 모델로 교체되며 Destroy된 그레이박스 팔다리는 **그 프레임 끝까지 살아 있다** —
            // 같은 Start 안에서 재면 옛 몸이 섞여 들어온다.
            if (r.GetComponentInParent<GreyboxLimbs>() != null) continue;
            if (r.bounds.max.y > top) top = r.bounds.max.y;
        }
        if (top <= float.MinValue * 0.5f) return GreyboxLimbs.Height * _visualScale;   // 폴백
        return Mathf.Max(0.5f, top - transform.position.y);
    }

    /// <summary>정수리 위 여유(m). 아래에서 위로: HP → 그로기 → 이름표.
    /// 0.2m로는 **머리에 달라붙어** 보였다(사용자 지적) — 애니메이션 포즈에 따라 정수리가
    /// 2~3cm씩 오르내리므로 그만큼도 잡아먹힌다. 쿼터뷰에서 확실히 떠 보이게 0.45부터.</summary>
    const float HP_GAP = 0.45f, GROGGY_GAP = 0.62f, LABEL_GAP = 0.88f;

    void CreateHPBar()
    {
        var c = new GameObject("HPBar");
        c.transform.SetParent(transform);
        c.transform.localPosition = new Vector3(0, HeadTop() + HP_GAP, 0);
        Billboard.Attach(c.transform);   // 쿼터뷰에서 눕혀 두면 게이지가 안 읽힌다
        hpBarBg   = MakeBar("HPBar_BG",   c.transform, new Color(0.1f, 0.1f, 0.1f, 0.8f), 0, BAR_W, BAR_H);
        hpBarFill = MakeBar("HPBar_Fill", c.transform, Color.green, 1, BAR_W, BAR_H);
        hpFillMat = hpBarFill.GetComponent<SpriteRenderer>().material;
    }

    /// <summary>이름표에 부상 표시를 붙인다 — "적 [다]" 처럼. F1 오버레이를 안 켜도
    /// **다리를 부순 게 먹혔는지** 바로 보여야 조준이 의미가 있다.</summary>
    string _badgeShown = "";
    void UpdateInjuryBadge()
    {
        if (_label == null || Inj == null || state == State.Dead) return;
        string badge = Inj.Badge();
        if (badge == _badgeShown) return;                 // 매 프레임 TextMesh를 건드리지 않는다
        _badgeShown = badge;
        _label.Set("적" + badge, UnitLabel.EnemyColor);
    }

    void UpdateHPBar()
    {
        if (health == null || hpBarBg == null) return;
        bool show = _visionVisible && health.Percent < 0.99f && state != State.Dead;
        hpBarBg.SetActive(show); hpBarFill.SetActive(show);
        if (!show) return;
        float pct = health.Percent;
        hpBarFill.transform.localScale    = new Vector3(BAR_W * pct, BAR_H, 1);
        hpBarFill.transform.localPosition = new Vector3(-BAR_W * (1 - pct) * 0.5f, 0, 0);
        if (hpFillMat != null) hpFillMat.color = Color.Lerp(Color.red, Color.green, pct);
    }

    void CreateGroggyBar()
    {
        var c = new GameObject("GroggyBar");
        c.transform.SetParent(transform);
        c.transform.localPosition = new Vector3(0, HeadTop() + GROGGY_GAP, 0);
        Billboard.Attach(c.transform);
        groggyBarBg   = MakeBar("GroggyBar_BG",   c.transform, new Color(0.15f, 0.15f, 0.15f, 0.7f), 0, GROG_W, GROG_H);
        groggyBarFill = MakeBar("GroggyBar_Fill",  c.transform, new Color(1f, 0.6f, 0f, 0.9f), 1, 0, GROG_H);
        groggyFillMat = groggyBarFill.GetComponent<SpriteRenderer>().material;
        groggyBarBg.SetActive(false); groggyBarFill.SetActive(false);
    }

    void UpdateGroggyBar()
    {
        if (groggyBarBg == null) return;
        bool show = currentGroggy > 0.1f || isStunned;
        groggyBarBg.SetActive(show); groggyBarFill.SetActive(show);
        if (!show) return;
        float pct = currentGroggy / MaxGroggy;
        groggyBarFill.transform.localScale    = new Vector3(GROG_W * pct, GROG_H, 1);
        groggyBarFill.transform.localPosition = new Vector3(-GROG_W * (1 - pct) * 0.5f, 0, 0);
        if (groggyFillMat != null)
            groggyFillMat.color = isStunned
                ? new Color(1f, 0.1f, 0.1f, Mathf.Sin(Time.time * 10f) > 0 ? 1f : 0.4f)
                : new Color(1f, 0.6f, 0f, 0.9f);
    }

    static Sprite _whiteSprite;
    static Sprite WhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;
        var t = new Texture2D(1, 1); t.SetPixel(0, 0, Color.white); t.Apply();
        return _whiteSprite = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }

    static GameObject MakeBar(string name, Transform parent, Color color, int order, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale    = new Vector3(w, h, 1);
        go.transform.localPosition = Vector3.zero;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = WhiteSprite();
        sr.color        = color;
        sr.sortingOrder = 100 + order;
        sr.material     = new Material(sr.material); // 인스턴스 머티리얼 (색 변경용)
        return go;
    }

    #endregion

    #region 콜백

    void OnDamaged(float amount)
    {
        if (state == State.Dead) return;

        // 2026-07-11: 그로기 스턴 중엔 상태를 갈아엎지 않는다(연출만).
        //   예전엔 스턴 중 피격이 무조건 state=Hit → Hit 종료 시 Chase로 복귀해서
        //   **애써 그로기 채워 스턴 걸어놔도 첫 타격에 바로 풀리고 반격당했다**(그로기 보상이 없었음).
        if (isStunned)
        {
            SetTint(new Color(1f, 0.5f, 0.5f));
            return;
        }

        FacePlayer();
        if (state == State.AttackWindup) windupFlashTimer = 0;
        ShowAlertMark(false);   // 조사 중 피격 시 '?' 잔류 방지
        state    = State.Hit;
        // 방금 예비동작 캔슬로 1.5배 경직을 받았다면 그 값을 유지(덮어쓰기 금지).
        if (_banditVisual != null) { _banditPendingHit = false; _banditVisual.CancelAttack(); _performer?.Cancel(); }
        if (Time.time >= _cancelBonusUntil) hitTimer = HitStun;
        SetVelocity(Vector2.zero);
        SetTint(new Color(1f, 0.5f, 0.5f));
        animController?.PlayOneShot("gethit");
    }

    void OnDeath()
    {
        state         = State.Dead;
        _banditPendingHit = false;
        _banditVisual?.Die(DeathPush());
        _performer?.Cancel();   // 죽는 순간 진행 중이던 공격 판정이 계속 나가는 것 방지(2026-07-11)
        _rb.detectCollisions = false;
        RestoreTint();
        animController?.PlayOneShot("death");
        if (hpBarBg   != null) hpBarBg.SetActive(false);
        if (hpBarFill != null) hpBarFill.SetActive(false);
        if (groggyBarBg   != null) groggyBarBg.SetActive(false);    // 시체 잔존화로 바가 영구 남는 것 방지
        if (groggyBarFill != null) groggyBarFill.SetActive(false);
        ShowAlertMark(false);   // 조사 표시 제거
        _nav?.Stop();   // 추격 중 사망 시 A* 리패스 잔류 방지(시체가 파괴되지 않으므로)

        if (QuestManager.Instance != null && !string.IsNullOrEmpty(unitKey))
            QuestManager.Instance.UpdateObjective(ObjectiveType.KillEnemy, unitKey, 1);

        // 킬 XP 누적 (레이드 중에만 — RaidManager가 종료 시 정산. traits.md §2)
        if (RaidManager.Instance != null && unitStat != null)
            RaidManager.Instance.TrackKillXp(unitStat.expReward);

        BecomeCorpse();

        if (_weaponVis != null) _weaponVis.SetVisible(false);   // 시체가 칼을 들고 서 있지 않게

        // 2026-07-11: "적이 죽으면 시체" — 머리 위 라벨을 교체하고 몸체를 어둡게 해
        //   살아있는(붉은) 적과 한눈에 구분되게 한다. 이름표만 바뀌면 여전히 헷갈린다.
        if (_label != null) _label.Set("시체", UnitLabel.CorpseColor);
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            var dark = new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, c.a);
            spriteRenderer.color = dark;
            if (_limbs != null) _limbs.SetTint(dark);   // 팔다리가 몸이므로 그쪽도 어둡게
        }

        SetVisionVisible(true);   // 시야 밖 사망 대비 — All 해제 후엔 PlayerVision이 다시 켜주지 않음(시체 = 항상 보이는 월드 오브젝트)
        enabled = false;          // AI 종료 + All 등록 해제(OnDisable) — 시야/전투 판정 대상에서 제외. GO는 시체로 유지.
    }

    // ── 약탈자(회수 루프, docs/raid.md) — ScavengerLoot가 스폰 직후 지정. 시체 루팅에 이 물품이 추가됨. ──
    bool _isScavenger;
    List<ItemInstance> _scavengerLoot;

    /// <summary>이 적을 '약탈자'로 지정 — 플레이어가 잃은 물품 loot을 지녀, 시체를 뒤지면 회수된다.
    /// 특징(식별): 몸을 보랏빛으로 틴트(HUD 표식 없음 — 관찰로 구분).</summary>
    public void MakeScavenger(List<ItemInstance> loot)
    {
        _isScavenger = true;
        _scavengerLoot = loot;
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.62f, 0.45f, 0.85f);   // 약탈자 = 보랏빛(붉은 일반 적과 구분)
    }

    /// <summary>시체 = 루팅 컨테이너 전환(docs/combat.md 2026-07-10 — 옛 즉시 바닥 드랍을 대체).
    /// GO를 파괴하지 않고 그 자리에 유지(레이드 씬 언로드 시 함께 정리 = "레이드 종료까지").
    /// 드랍 테이블을 시체 인벤에 굴려 넣고, E 상호작용(Container)으로 뒤진다.</summary>
    void BecomeCorpse()
    {
        var container = gameObject.AddComponent<LootContainer>();
        string label = (unitStat != null && !string.IsNullOrEmpty(unitStat.displayName)) ? unitStat.displayName : "적";
        if (_isScavenger) label = "약탈자 " + label;

        var items = RollLoot();
        AddCash(items);   // 현금 — 배그식 사망 루팅 ④ (docs/economy.md §적 현금 드랍)
        // 약탈자 = 플레이어가 잃은 물품을 지님 → 시체 루팅으로 회수.
        if (_isScavenger && _scavengerLoot != null) items.AddRange(_scavengerLoot);
        var overflow = container.SetupAutoSize($"{label} 시체", items);   // 격자 크기 = 내용물에 맞춤(4열, 2~6행)
        foreach (var it in overflow) DropOne(it);                          // 그래도 넘치는 것만 바닥에

        var io = gameObject.AddComponent<InteractableObject>();
        io.SetupAsContainer("시체 뒤지기");

        // 배그식 사망 루팅 ①② — 희귀도 빛기둥(비면 "빈 시체") + 열면 빠른 루팅 목록(InteractableObject가 이걸 보고 고른다)
        gameObject.AddComponent<CorpseMarker>().Init(container, _label);
    }

    /// <summary>현금 — 유닛별 cashMin~cashMax(◈). 현금 아이템 1개 = ◈1(스택 수가 곧 금액).</summary>
    void AddCash(List<ItemInstance> items)
    {
        if (unitStat == null || unitStat.cashMax <= 0) return;
        var cash = ItemDatabase.Get(CashWallet.ItemId);
        if (cash == null) return;
        int amount = Random.Range(unitStat.cashMin, Mathf.Max(unitStat.cashMin, unitStat.cashMax) + 1);
        if (amount > 0) items.Add(new ItemInstance(cash, amount));
    }

    /// <summary>래그돌이 쓰러질 방향·세기(속도). 맞은 방향 정보가 없어 "플레이어 → 적" 쪽으로 밀어 넘어뜨린다.</summary>
    Vector3 DeathPush()
    {
        Vector3 d = player != null ? transform.position - player.position : -transform.forward;
        d.y = 0f;
        return (d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward) * 2.5f + Vector3.up * 1.2f;
    }

    /// <summary>전리품 롤 — 적별 전용 드랍 테이블(unitStat.drops) 우선, 비면 지상 티어 region 루트 폴백.
    /// GameTuning.enemyDropChance로 전역 게이트(실패 = 빈손 시체 — 뒤질 수는 있음).
    /// 추가로 corpseBagChance 확률로 가방(컨테이너 아이템)이 통째로 — 안에 지역 루트 1~2개(타르코프식, 가방째 회수 가능).</summary>
    List<ItemInstance> RollLoot()
    {
        var items = new List<ItemInstance>();

        float gate = GameTuning.Instance != null ? GameTuning.Instance.enemyDropChance : 1f;
        if (Random.value <= gate)
        {
            // 적별 전용 테이블이 있으면 그것만 사용(밸런스 에디터에서 편집).
            if (unitStat != null && unitStat.drops != null && unitStat.drops.Count > 0)
            {
                foreach (var d in unitStat.drops)
                {
                    if (d == null || string.IsNullOrEmpty(d.itemId)) continue;
                    if (Random.value > d.chance) continue;
                    var data = ItemDatabase.Get(d.itemId);
                    if (data == null) continue;
                    int qty = Random.Range(d.minQty, Mathf.Max(d.minQty, d.maxQty) + 1);
                    if (qty > 0) items.Add(new ItemInstance(data, qty));
                }
            }
            else
            {
                // 폴백: 지역(Ground) 루트
                var loot = RegionLootCatalog.RollForActiveRegion(RegionLootTier.GroundDay);
                if (loot != null)
                    for (int i = 0; i < loot.Length; i++)
                        if (loot[i] != null) items.Add(loot[i]);
            }
        }

        TryAddBag(items);   // 가방은 드랍 게이트와 별개 롤
        return items;
    }

    /// <summary>corpseBagChance 확률로 시체에 가방 아이템을 넣는다 — 가방 내부엔 지역 루트 1~2개.</summary>
    void TryAddBag(List<ItemInstance> items)
    {
        float chance = GameTuning.Instance != null ? GameTuning.Instance.corpseBagChance : 0.3f;
        if (Random.value > chance) return;

        var bagData = PickRandomBag();
        if (bagData == null) return;

        var bag = new ItemInstance(bagData, 1);
        var inner = bag.ContainerGrid;
        if (inner != null)
        {
            var loot = RegionLootCatalog.RollForActiveRegion(RegionLootTier.GroundDay);
            int put = 0, max = Random.Range(1, 3);   // 1~2개
            if (loot != null)
                for (int i = 0; i < loot.Length && put < max; i++)
                    if (loot[i] != null && inner.TryAutoPlace(loot[i])) put++;
        }
        items.Add(bag);
    }

    static ItemData PickRandomBag()
    {
        var all = ItemDatabase.GetAll();
        ItemData pick = null;
        int seen = 0;
        for (int i = 0; i < all.Length; i++)
        {
            var d = all[i];
            if (d == null || d.equipSlot != EquipSlot.Backpack || !d.IsContainer) continue;
            seen++;
            if (Random.Range(0, seen) == 0) pick = d;   // 저수지 샘플링 — 목록 생성 없이 균등 랜덤
        }
        return pick;
    }

    void DropOne(ItemInstance item)
    {
        Vector2 r = Random.insideUnitCircle * 0.6f;
        WorldItem.Drop(item, transform.position + new Vector3(r.x, r.y, 0f), this);
    }

    void OnGroggyTriggered()
    {
        ShowAlertMark(false); state = State.Stunned; SetVelocity(Vector2.zero);
        if (_banditVisual != null) { _banditPendingHit = false; _banditVisual.CancelAttack(); _performer?.Cancel(); }
    }

    void OnGroggyRecovered()
    {
        if (state == State.Stunned) { RestoreTint(); state = State.Chase; }
    }

    #endregion

    #region 유틸리티

    void SetPatrolTarget()
    {
        patrolTimer  = 0f;
        patrolTarget = spawnPos + Random.insideUnitCircle * PatrolRad;
    }

    /// <summary>머리 위 '?' 조사 표시(소음 들었을 때). 지연 생성.</summary>
    void ShowAlertMark(bool on)
    {
        if (on && alertMark == null)
        {
            alertMark = new GameObject("AlertMark");
            alertMark.transform.SetParent(transform, false);
            alertMark.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            var tm = alertMark.AddComponent<TextMesh>();
            tm.text = "?"; tm.fontSize = 48; tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.LowerCenter; tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.85f, 0.3f);
            var mr = alertMark.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 132;   // 말풍선 층 근처
            // 카메라를 향하게 — 적 몸에 붙어 같이 돌면 대각선 쿼터뷰(2026-09-11)에서 비스듬히 누워 보인다.
            Billboard.Attach(alertMark.transform);
        }
        if (alertMark != null) alertMark.SetActive(on);
    }

    /// <summary>평면 속도 적용 — 세로(Y) 성분은 건드리지 않는다(중력·단차용으로 남겨 둔다).</summary>
    void SetVelocity(Vector2 v)
    {
        if (_rb == null) return;
        _rb.linearVelocity = new Vector3(v.x, _rb.linearVelocity.y, v.y);
    }

    float DistToPlayer()
        => player == null ? float.MaxValue : Vector2.Distance(Plan3D.ToPlan(transform.position), Plan3D.ToPlan(player.position));

    void FacePlayer()
    {
        if (player == null) return;
        Vector2 dir = (Plan3D.ToPlan(player.position) - Plan3D.ToPlan(transform.position)).normalized;
        animController?.SetDirection(dir);
        FlipSprite(dir);
    }

    void FlipSprite(Vector2 dir)
    {
        _banditVisual?.SetFacing(dir);
        // 3D 몸은 좌우 뒤집기가 아니라 회전이다 — 쿼터뷰에선 앞뒤도 보인다.
        if (_limbs != null) _limbs.SetFacing(dir);
        if (spriteRenderer != null && Mathf.Abs(dir.x) > 0.01f)
            spriteRenderer.flipX = dir.x < 0f;
    }

    void SetTint(Color c)
    {
        _banditVisual?.SetTint(c == originalColor ? (_isScavenger ? new Color(.75f,.6f,1f) : Color.white) : c);
        if (spriteRenderer != null) spriteRenderer.color = c;
        // 팔다리가 몸을 대신하므로 색도 그쪽으로 — 안 그러면 예비동작 깜빡임이 안 보인다.
        if (_limbs != null) _limbs.SetTint(c);
        foreach (var r in renderers) if (r is SpriteRenderer sr) sr.color = c;
    }

    void RestoreTint() => SetTint(originalColor);

    #endregion
}
