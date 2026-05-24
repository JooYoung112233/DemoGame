using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이어 통합 컨트롤러.
/// 이동, 마우스 조준, 손전등, 전투(콤보/차징/구르기), 스태미너, 비주얼(빌보드/플립/바운스) 일체형.
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region ===== 싱글톤 (DontDestroyOnLoad) =====

    public static PlayerController Instance { get; private set; }

    #endregion

    #region ===== 열거형 & 필드 =====

    // ---------- 전투 상태 ----------
    public enum CombatState { Idle, LightAttack, HeavyCharge, HeavyRelease, Dodge, Exhausted }

    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;

    [Header("Flashlight")]
    [SerializeField] Transform flashlightPivot;
    [SerializeField] float flashlightPitch = 10f;

    [Header("Stamina (기본값, CombatData 없을 때)")]
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float staminaRegenRate = 15f;
    [SerializeField] float staminaRegenDelay = 1.0f;

    [Header("Walk Bounce")]
    [SerializeField] float bounceHeight = 0.035f;
    [SerializeField] float bounceSpeed = 14f;
    [SerializeField] float squashAmount = 0.06f;
    [SerializeField] Transform visualRoot;

    [Header("References")]
    [SerializeField] CombatData combatData;
    [SerializeField] CrosshairUI crosshairUI;

    // --- 자동 검색 레퍼런스 ---
    Camera mainCam;
    NavMeshAgent agent;
    Health health;
    CombatFeedback feedback;
    SkeletonAnimController animController;
    PlayerMedicalSystem medical;
    SpriteRenderer spriteRenderer;
    Renderer[] renderers;
    Color originalColor = Color.white;

    // --- 이동 ---
    Vector3 moveDir;
    Vector3 camForward;
    Vector3 camRight;
    Vector3 lastMoveDir = Vector3.forward;
    bool canMove = true;

    // --- 전투 상태머신 ---
    CombatState state = CombatState.Idle;

    // 약공격 콤보
    int comboStep;
    float comboTimer;
    float lightCooldownTimer;

    // 강공격 차징
    float chargeTimer;
    float heavyCooldownTimer;
    bool heavyFullyCharged;

    // 구르기
    float dodgeTimer;
    float dodgeCooldownTimer;
    Vector3 dodgeDir;
    float dodgeInvTimer;

    // 탈진
    float exhaustionTimer;

    // 공격 이펙트
    float attackFlashTimer;

    // --- 스태미너 ---
    float staminaCurrent;
    float staminaRegenDelayTimer;
    float staminaExhaustionTimer;
    bool isExhausted;

    // --- 비주얼: 원샷 타이머 ---
    float oneShotTimer;
    System.Action oneShotCallback;

    // --- 워크 바운스 ---
    Vector3 bounceBaseLocalPos;
    Vector3 bounceBaseLocalScale;
    float bounceTimer;
    float currentBounce;
    bool wasMoving;
    Vector3 lastPosition;
    float positionDelta;

    // --- 3D 월드 바 ---
    GameObject staminaBarBg, staminaBarFill;
    Material staminaFillMat;
    float staminaBarWidth = 0.7f;
    float staminaBarHeight = 0.06f;

    GameObject chargeBarBg, chargeBarFill;
    Material chargeFillMat;
    float chargeBarWidth = 0.5f;
    float chargeBarHeight = 0.05f;

    #endregion

    #region ===== 프로퍼티: CombatData 접근 =====

    // --- 이동 ---
    float BaseMoveSpeed => combatData != null ? combatData.player.moveSpeed : moveSpeed;
    float MoveSpeed
    {
        get
        {
            float speed = BaseMoveSpeed;
            if (medical != null) speed *= medical.MoveSpeedMultiplier;
            return speed;
        }
    }

    // --- 약공격 ---
    float LightDmg(int step)
    {
        if (combatData == null) return 8f;
        switch (step)
        {
            case 0: return combatData.player.lightDamage;
            case 1: return combatData.player.lightCombo2Damage;
            case 2: return combatData.player.lightCombo3Damage;
            default: return combatData.player.lightDamage;
        }
    }
    float LightGroggy(int step)
    {
        if (combatData == null) return 5f;
        switch (step)
        {
            case 0: return combatData.player.lightGroggy;
            case 1: return combatData.player.lightCombo2Groggy;
            case 2: return combatData.player.lightCombo3Groggy;
            default: return combatData.player.lightGroggy;
        }
    }
    float LightStamina(int step)
    {
        if (combatData == null) return 6f;
        return step >= 2 && combatData != null ? combatData.player.lightCombo3StaminaCost : combatData.player.lightStaminaCost;
    }
    float LightRange => combatData != null ? combatData.player.lightRange : 2f;
    float LightCooldown => combatData != null ? combatData.player.lightCooldown : 0.4f;
    int ComboMax => combatData != null ? combatData.player.lightComboMax : 3;
    float ComboWindow => combatData != null ? combatData.player.lightComboWindow : 0.6f;

    // --- 강공격 ---
    float HeavyDmg => combatData != null ? combatData.player.heavyDamage : 20f;
    float HeavyFullDmg => combatData != null ? combatData.player.heavyFullDamage : 32f;
    float HeavyRange => combatData != null ? combatData.player.heavyRange : 2.5f;
    float HeavyStaminaCost => combatData != null ? combatData.player.heavyStaminaCost : 22f;
    float HeavyFullStaminaCost => combatData != null ? combatData.player.heavyFullStaminaCost : 35f;
    float HeavyGroggyVal => combatData != null ? combatData.player.heavyGroggy : 25f;
    float HeavyFullGroggyVal => combatData != null ? combatData.player.heavyFullGroggy : 45f;
    float HeavyChargeTime => combatData != null ? combatData.player.heavyChargeTime : 0.6f;
    float HeavyMaxCharge => combatData != null ? combatData.player.heavyMaxCharge : 1.5f;
    float HeavyCooldown => combatData != null ? combatData.player.heavyCooldown : 0.8f;

    // --- 구르기 ---
    float DodgeStaminaCost => combatData != null ? combatData.player.dodgeStaminaCost : 15f;
    float DodgeDist => combatData != null ? combatData.player.dodgeDistance : 3f;
    float DodgeDur => combatData != null ? combatData.player.dodgeDuration : 0.3f;
    float DodgeInvDur => combatData != null ? combatData.player.dodgeInvincibleDuration : 0.2f;
    float DodgeCooldown => combatData != null ? combatData.player.dodgeCooldown : 0.5f;

    // --- 스태미너 ---
    float MaxStam => combatData != null ? combatData.player.maxStamina : maxStamina;
    float StamRegenRate => combatData != null ? combatData.player.staminaRegen : staminaRegenRate;
    float StamRegenDelay => combatData != null ? combatData.player.staminaRegenDelay : staminaRegenDelay;
    float ExhaustDur => combatData != null ? combatData.player.exhaustionDuration : 0.8f;

    #endregion

    #region ===== 퍼블릭 API =====

    // 이동/조준
    public Vector3 FacingDirection { get; private set; } = Vector3.forward;
    public Vector3 MouseWorldPos { get; private set; }

    // 전투
    public bool CombatEnabled { get; set; } = true;
    public bool IsInvincible => state == CombatState.Dodge && dodgeInvTimer > 0;
    public bool IsAttacking => state == CombatState.LightAttack || state == CombatState.HeavyRelease;
    public CombatState CurrentState => state;
    public float ChargePercent => state == CombatState.HeavyCharge ? Mathf.Clamp01(chargeTimer / HeavyMaxCharge) : 0f;
    public float GetAttackRange() => LightRange;

    // 스태미너 (HUD용)
    public float StaminaCurrent => staminaCurrent;
    public float StaminaMax => MaxStam;
    public float StaminaPercent => staminaCurrent / MaxStam;
    public bool IsExhausted => isExhausted;

    /// <summary>HUD 표시용 상태 문자열</summary>
    public string GetStateText()
    {
        switch (state)
        {
            case CombatState.LightAttack: return $"Light {comboStep}/{ComboMax}";
            case CombatState.HeavyCharge: return $"Charging {ChargePercent * 100:F0}%";
            case CombatState.HeavyRelease: return "Heavy!";
            case CombatState.Dodge: return "Dodge";
            case CombatState.Exhausted: return "EXHAUSTED";
            default: return "Idle";
        }
    }

    #endregion

    #region ===== Unity 라이프사이클 =====

    /// <summary>어떤 씬에서 Play 해도 Player가 존재하도록 자동 생성.
    /// 씬에 배치된 Player가 있으면 그걸 사용.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;

        // 씬에 이미 Player가 있으면 스킵 (Awake에서 Instance 됨)
        if (FindObjectOfType<PlayerController>(true) != null) return;

        // 없으면 Resources/Player 프리팹에서 생성
        var prefab = Resources.Load<GameObject>("Player");
        if (prefab != null)
        {
            var go = Instantiate(prefab);
            go.name = "Player";

            // SpawnPoint 있으면 거기에, 없으면 원점
            var sp = FindObjectOfType<SpawnPoint>();
            if (sp != null)
                go.transform.position = sp.transform.position;
        }
        else
        {
            Debug.LogWarning("[PlayerController] Resources/Player 프리팹을 찾을 수 없습니다. Assets/Resources/ 에 Player 프리팹을 넣어주세요.");
        }
    }

    void Awake()
    {
        // 싱글톤 + DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 씬 로드 시 카메라 등 재연결
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 자동 레퍼런스 검색
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        feedback = GetComponent<CombatFeedback>();
        medical = GetComponent<PlayerMedicalSystem>();
        animController = GetComponentInChildren<SkeletonAnimController>();
        mainCam = Camera.main;

        // 스프라이트 렌더러
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        renderers = GetComponentsInChildren<Renderer>();

        // NavMeshAgent 설정
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.speed = MoveSpeed;
            agent.acceleration = 100f;
            agent.angularSpeed = 0f;
        }

        // Health 콜백
        if (health != null)
        {
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
        }

        // 스태미너 초기화
        staminaCurrent = MaxStam;

        // 워크 바운스: visualRoot 자동 탐색
        if (visualRoot == null)
        {
            var root = transform.Find("Root");
            visualRoot = root != null ? root : null;
        }
        if (visualRoot != null)
        {
            bounceBaseLocalPos = visualRoot.localPosition;
            bounceBaseLocalScale = visualRoot.localScale;
        }

        lastPosition = transform.position;
    }

    void Start()
    {
        UpdateCameraAxes();
        CreateStaminaBar();
        CreateChargeBar();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬의 카메라 재연결
        mainCam = Camera.main;

        // NavMeshAgent가 새 NavMesh 위에 있도록 보장
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        // 전투 상태 초기화 (씬 전환 중 공격 중이면 리셋)
        state = CombatState.Idle;
        canMove = true;

        // 안전가옥에서는 전투 비활성화
        CombatEnabled = (scene.name != "Safehouse");
    }

    /// <summary>UI가 열려있어서 플레이어 입력을 차단해야 하는지</summary>
    bool IsUIBlocking => UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();

    void Update()
    {
        if (health != null && health.IsDead) return;

        // 카메라 축 갱신
        UpdateCameraAxes();

        // UI가 열려있으면 이동/전투/조준 모두 차단
        if (IsUIBlocking)
        {
            moveDir = Vector3.zero;
            animController?.Play("idle");
            return;
        }

        // 스태미너 업데이트
        UpdateStamina();

        // 이동 입력 수집
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveDir = (camForward * v + camRight * h).normalized;

        if (moveDir.sqrMagnitude > 0.01f)
            lastMoveDir = moveDir;

        // 마우스 방향 갱신
        UpdateMouseFacing();

        // 스프라이트 방향 설정 (마우스 기준)
        if (FacingDirection.sqrMagnitude > 0.01f)
        {
            animController?.SetDirection(FacingDirection);
        }

        // 손전등 방향 갱신
        if (flashlightPivot != null && FacingDirection.sqrMagnitude > 0.01f)
        {
            Quaternion yaw = Quaternion.LookRotation(FacingDirection);
            Quaternion pitch = Quaternion.Euler(flashlightPitch, 0, 0);
            flashlightPivot.rotation = yaw * pitch;
        }

        // 쿨다운 감소 (unscaled — timeScale=0에서도 작동)
        float udt = Time.unscaledDeltaTime;
        lightCooldownTimer -= udt;
        heavyCooldownTimer -= udt;
        dodgeCooldownTimer -= udt;
        attackFlashTimer -= udt;

        // 전투 상태 업데이트
        switch (state)
        {
            case CombatState.Idle: UpdateIdle(); break;
            case CombatState.LightAttack: UpdateLightAttack(); break;
            case CombatState.HeavyCharge: UpdateHeavyCharge(); break;
            case CombatState.HeavyRelease: UpdateHeavyRelease(); break;
            case CombatState.Dodge: UpdateDodge(); break;
            case CombatState.Exhausted: UpdateExhausted(); break;
        }

        // 콤보 윈도우 타임아웃
        if (comboTimer > 0)
        {
            comboTimer -= udt;
            if (comboTimer <= 0) comboStep = 0;
        }

        // 탈진 체크
        if (isExhausted && state != CombatState.Exhausted && state != CombatState.Dodge)
        {
            state = CombatState.Exhausted;
            exhaustionTimer = ExhaustDur;
            SetTint(new Color(0.5f, 0.5f, 0.8f));
        }

        // 시각 피드백 복원
        if (attackFlashTimer <= 0 && state != CombatState.HeavyCharge && state != CombatState.Dodge && state != CombatState.Exhausted)
        {
            RestoreTint();
        }

        // 이동 애니메이션 (Idle 상태일 때만)
        if (state == CombatState.Idle)
        {
            bool moving = Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f;
            string anim = moving ? "walk" : "idle";
            animController?.Play(anim);
        }

        // 이동 처리 (canMove일 때만)
        // unscaledDeltaTime 사용 → 안전가옥(timeScale=0)에서도 이동 가능
        float dt = Time.unscaledDeltaTime;
        if (canMove)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.speed = MoveSpeed;
                agent.Move(moveDir * MoveSpeed * dt);
            }
            else
            {
                transform.position += moveDir * MoveSpeed * dt;
            }
        }
    }

    void LateUpdate()
    {
        // 빌보드: 스프라이트가 카메라를 향하도록
        if (mainCam != null && spriteRenderer != null)
            spriteRenderer.transform.rotation = mainCam.transform.rotation;

        // 스프라이트 좌우 플립 (마우스 방향 기준)
        if (spriteRenderer != null && mainCam != null && FacingDirection.sqrMagnitude > 0.01f)
        {
            Vector3 cr = mainCam.transform.right;
            cr.y = 0;
            spriteRenderer.flipX = Vector3.Dot(FacingDirection, cr.normalized) < 0;
        }

        // 원샷 타이머
        if (oneShotTimer > 0)
        {
            oneShotTimer -= Time.unscaledDeltaTime;
            if (oneShotTimer <= 0)
            {
                oneShotCallback?.Invoke();
                oneShotCallback = null;
            }
        }

        // 3D 바 업데이트
        UpdateStaminaBar3D();
        UpdateChargeBar3D();

        // 워크 바운스
        UpdateWalkBounce();
    }

    #endregion

    #region ===== 이동 =====

    void UpdateCameraAxes()
    {
        if (mainCam == null) return;
        camForward = mainCam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        camRight = mainCam.transform.right;
        camRight.y = 0;
        camRight.Normalize();
    }

    void UpdateMouseFacing()
    {
        if (mainCam == null) return;
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float dist))
        {
            Vector3 hitPoint = ray.GetPoint(dist);
            MouseWorldPos = hitPoint;
            Vector3 dir = hitPoint - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
                FacingDirection = dir.normalized;
        }
    }

    #endregion

    #region ===== 전투 상태 =====

    void UpdateIdle()
    {
        if (!CombatEnabled) return;

        // 구르기 — Space (최우선)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryDodge();
            return;
        }

        // 강공격 — 우클릭 홀드
        if (Input.GetMouseButtonDown(1) && heavyCooldownTimer <= 0)
        {
            if (CanConsumeStamina(HeavyStaminaCost))
            {
                state = CombatState.HeavyCharge;
                chargeTimer = 0;
                heavyFullyCharged = false;
                canMove = false;
                return;
            }
        }

        // 약공격 — 좌클릭
        if (Input.GetMouseButtonDown(0) && lightCooldownTimer <= 0)
        {
            TryLightAttack();
        }
    }

    void UpdateLightAttack()
    {
        // 약공격 중 구르기로 캔슬 가능
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryDodge();
            return;
        }
    }

    void UpdateHeavyCharge()
    {
        chargeTimer += Time.deltaTime;

        // 풀차지 알림
        if (chargeTimer >= HeavyMaxCharge && !heavyFullyCharged)
        {
            heavyFullyCharged = true;
        }

        // 구르기로 캔슬
        if (Input.GetKeyDown(KeyCode.Space))
        {
            canMove = true;
            TryDodge();
            return;
        }

        // 우클릭 뗌 → 발동
        if (Input.GetMouseButtonUp(1))
        {
            if (chargeTimer >= HeavyChargeTime)
            {
                DoHeavyAttack();
            }
            else
            {
                // 차징 부족 → 캔슬
                state = CombatState.Idle;
                canMove = true;
            }
        }
    }

    void UpdateHeavyRelease()
    {
        // 공격 모션 대기 후 Idle 복귀 (DoHeavyAttack에서 Invoke 처리)
    }

    void UpdateDodge()
    {
        float dt = Time.unscaledDeltaTime;
        dodgeTimer -= dt;
        dodgeInvTimer -= dt;

        // 대시 이동
        float speed = DodgeDist / DodgeDur;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.Move(dodgeDir * speed * dt);
        }
        else
        {
            transform.position += dodgeDir * speed * dt;
        }

        // 무적 중 반투명
        if (dodgeInvTimer > 0)
            SetAlpha(0.3f);
        else
            SetAlpha(1f);

        if (dodgeTimer <= 0)
        {
            state = CombatState.Idle;
            canMove = true;
            RestoreTint();
            SetAlpha(1f);
        }
    }

    void UpdateExhausted()
    {
        exhaustionTimer -= Time.unscaledDeltaTime;
        // 탈진 중 깜빡임
        float blink = Mathf.Sin(Time.time * 8f) > 0 ? 0.6f : 1f;
        SetAlpha(blink);

        if (exhaustionTimer <= 0 || !isExhausted)
        {
            state = CombatState.Idle;
            RestoreTint();
            SetAlpha(1f);
        }
    }

    #endregion

    #region ===== 전투 액션 =====

    void TryLightAttack()
    {
        float cost = LightStamina(comboStep);
        if (!ConsumeStamina(cost)) return;

        DoLightAttack();
    }

    void DoLightAttack()
    {
        state = CombatState.LightAttack;
        lightCooldownTimer = LightCooldown;

        int step = comboStep;
        float damage = LightDmg(step);
        float groggy = LightGroggy(step);

        // 콤보 진행
        comboStep = (comboStep + 1) % ComboMax;
        comboTimer = ComboWindow;

        // 마우스 방향으로 갱신
        animController?.SetDirection(FacingDirection);

        // 공격 판정 — 전방 부채꼴 범위
        HitEnemiesInRange(LightRange, damage, groggy, 90f);

        // 시각 피드백 — 흰색 플래시
        attackFlashTimer = 0.1f;
        SetTint(Color.white * 1.5f);

        // 애니메이션
        System.Action onDone = () =>
        {
            if (state == CombatState.LightAttack)
                state = CombatState.Idle;
        };

        if (animController != null)
        {
            animController.PlayOneShot("attack", onDone);
        }
        else
        {
            // 애니 없으면 원샷 타이머로 복귀
            oneShotTimer = LightCooldown;
            oneShotCallback = onDone;
        }
    }

    void DoHeavyAttack()
    {
        bool isFullCharge = chargeTimer >= HeavyMaxCharge;
        float damage = isFullCharge ? HeavyFullDmg : HeavyDmg;
        float groggy = isFullCharge ? HeavyFullGroggyVal : HeavyGroggyVal;
        float cost = isFullCharge ? HeavyFullStaminaCost : HeavyStaminaCost;

        if (!ConsumeStamina(cost))
        {
            state = CombatState.Idle;
            canMove = true;
            RestoreTint();
            return;
        }

        state = CombatState.HeavyRelease;
        heavyCooldownTimer = HeavyCooldown;

        // 강공격 판정 — 더 넓은 범위
        HitEnemiesInRange(HeavyRange, damage, groggy, 120f, true);

        // 시각 피드백 — 빨간 플래시
        attackFlashTimer = 0.2f;
        SetTint(isFullCharge ? new Color(1f, 0f, 0f, 1f) : new Color(1f, 0.4f, 0f, 1f));

        // 이동 복귀
        canMove = true;

        // 콤보 리셋
        comboStep = 0;
        comboTimer = 0;

        // 상태 복귀
        Invoke(nameof(ReturnToIdle), 0.3f);
    }

    void TryDodge()
    {
        if (dodgeCooldownTimer > 0) return;
        if (!ConsumeStamina(DodgeStaminaCost)) return;

        state = CombatState.Dodge;
        dodgeTimer = DodgeDur;
        dodgeInvTimer = DodgeInvDur;
        dodgeCooldownTimer = DodgeCooldown + DodgeDur;

        // 이동 방향 or 마우스 방향
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;

        if (inputDir.sqrMagnitude > 0.01f)
        {
            // 카메라 기준 변환
            dodgeDir = (camForward * v + camRight * h).normalized;
        }
        else
        {
            dodgeDir = FacingDirection;
        }

        // 구르기 중 이동 입력 끊기
        canMove = false;

        // 콤보 리셋
        comboStep = 0;
        comboTimer = 0;
    }

    #endregion

    #region ===== 히트 판정 =====

    void HitEnemiesInRange(float range, float damage, float groggyAmount, float angle, bool isHeavy = false)
    {
        Vector3 forward = FacingDirection;
        var enemies = FindObjectsOfType<EnemyController>();
        bool hitAny = false;

        foreach (var enemy in enemies)
        {
            var hp = enemy.GetComponent<Health>();
            if (hp == null || hp.IsDead) continue;

            Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0;
            float dist = toEnemy.magnitude;

            if (dist > range) continue;

            // 부채꼴 각도 체크
            float angleDiff = Vector3.Angle(forward, toEnemy);
            if (angleDiff > angle * 0.5f) continue;

            // 데미지 적용
            hp.TakeDamage(damage);
            hitAny = true;

            // 데미지 팝업 + 그로기 + 캔슬 — EnemyController 통합 참조
            var ec = enemy.GetComponent<EnemyController>();
            var popupType = isHeavy ? DamagePopup.DamageType.Heavy : DamagePopup.DamageType.Normal;
            if (ec != null && ec.IsStunned)
                popupType = DamagePopup.DamageType.Critical;

            DamagePopup.Create(enemy.transform.position, damage, popupType);

            // 그로기 적용
            if (ec != null)
                ec.AddGroggy(groggyAmount);

            // 강공격이면 적 공격 캔슬 시도
            if (isHeavy && ec != null)
                ec.TryCancelAttack();
        }

        // 공격 돌진 (맞추든 빗나가든 항상)
        if (feedback != null)
        {
            if (isHeavy)
                feedback.DoHeavyLunge(forward);
            else
                feedback.DoLightLunge(forward);
        }
    }

    #endregion

    #region ===== 스태미너 =====

    /// <summary>스태미너가 부족하면 false 반환 (소모하지 않음)</summary>
    bool CanConsumeStamina(float amount) => !isExhausted && staminaCurrent >= amount;

    /// <summary>스태미너 소모. 0이 되면 탈진 상태 진입. 부족하면 false.</summary>
    public bool ConsumeStamina(float amount)
    {
        if (isExhausted) return false;
        if (staminaCurrent < amount) return false;

        staminaCurrent -= amount;
        staminaRegenDelayTimer = StamRegenDelay;

        if (staminaCurrent <= 0)
        {
            staminaCurrent = 0;
            isExhausted = true;
            staminaExhaustionTimer = ExhaustDur;
        }

        return true;
    }

    void UpdateStamina()
    {
        float dt = Time.unscaledDeltaTime;

        // 탈진 상태 처리
        if (isExhausted)
        {
            staminaExhaustionTimer -= dt;
            if (staminaExhaustionTimer <= 0)
            {
                isExhausted = false;
            }
        }

        // 회복 딜레이 후 자동 회복
        if (staminaRegenDelayTimer > 0)
        {
            staminaRegenDelayTimer -= dt;
        }
        else if (staminaCurrent < MaxStam)
        {
            staminaCurrent = Mathf.Min(staminaCurrent + StamRegenRate * dt, MaxStam);

            // 탈진 해제 후 최소 스태미너 확보
            if (isExhausted && staminaCurrent >= MaxStam * 0.2f)
                isExhausted = false;
        }
    }

    #endregion

    #region ===== 워크 바운스 =====

    void UpdateWalkBounce()
    {
        if (visualRoot == null) return;

        // 위치 변화로 이동 감지
        Vector3 currentPos = transform.position;
        positionDelta = (currentPos - lastPosition).sqrMagnitude / Mathf.Max(Time.deltaTime * Time.deltaTime, 0.0001f);
        lastPosition = currentPos;

        bool moving = IsBounceMoving();

        if (moving)
        {
            bounceTimer += Time.deltaTime * bounceSpeed;

            // 절대값 사인 → 바닥에서 위로만 튕김 (통통 느낌)
            float raw = Mathf.Sin(bounceTimer);
            float bounce = Mathf.Abs(raw) * bounceHeight;

            // 착지 순간 (사인파가 0을 지날 때) 살짝 납작
            float squash = 1f;
            float stretch = 1f;
            if (Mathf.Abs(raw) < 0.3f)
            {
                float landPct = 1f - (Mathf.Abs(raw) / 0.3f);
                squash = 1f + squashAmount * landPct;
                stretch = 1f - squashAmount * landPct;
            }
            else
            {
                float airPct = (Mathf.Abs(raw) - 0.3f) / 0.7f;
                squash = 1f - squashAmount * 0.3f * airPct;
                stretch = 1f + squashAmount * 0.3f * airPct;
            }

            currentBounce = bounce;
            visualRoot.localPosition = bounceBaseLocalPos + new Vector3(0, bounce, 0);
            visualRoot.localScale = new Vector3(
                bounceBaseLocalScale.x * squash,
                bounceBaseLocalScale.y * stretch,
                bounceBaseLocalScale.z
            );

            wasMoving = true;
        }
        else
        {
            // 멈출 때 부드럽게 원위치
            if (wasMoving)
            {
                bounceTimer = 0;
                wasMoving = false;
            }

            currentBounce = Mathf.Lerp(currentBounce, 0, Time.deltaTime * 12f);
            visualRoot.localPosition = Vector3.Lerp(
                visualRoot.localPosition,
                bounceBaseLocalPos,
                Time.deltaTime * 12f
            );
            visualRoot.localScale = Vector3.Lerp(
                visualRoot.localScale,
                bounceBaseLocalScale,
                Time.deltaTime * 12f
            );
        }
    }

    bool IsBounceMoving()
    {
        // NavMeshAgent.velocity 체크 (SetDestination 사용 시)
        if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
            return true;

        // 실제 위치 변화 체크 (agent.Move() 사용 시 velocity가 0이므로)
        if (positionDelta > 0.5f)
            return true;

        return false;
    }

    #endregion

    #region ===== 3D 월드 바 =====

    void CreateStaminaBar()
    {
        var container = new GameObject("StaminaBar3D");
        container.transform.SetParent(transform);
        container.transform.localPosition = new Vector3(0, 0.78f, 0);

        Shader sh = FindUnlitShader();

        // 배경
        staminaBarBg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        staminaBarBg.name = "StaminaBar_BG";
        staminaBarBg.transform.SetParent(container.transform);
        staminaBarBg.transform.localPosition = Vector3.zero;
        staminaBarBg.transform.localScale = new Vector3(staminaBarWidth, staminaBarHeight, 1);
        staminaBarBg.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(staminaBarBg.GetComponent<MeshCollider>());
        var bgMat = new Material(sh);
        SetupTransparentMat(bgMat, new Color(0.1f, 0.1f, 0.1f, 0.7f), 3200);
        SetupBarRenderer(staminaBarBg.GetComponent<MeshRenderer>(), bgMat);

        // 채우기
        staminaBarFill = GameObject.CreatePrimitive(PrimitiveType.Quad);
        staminaBarFill.name = "StaminaBar_Fill";
        staminaBarFill.transform.SetParent(container.transform);
        staminaBarFill.transform.localPosition = new Vector3(0, 0, -0.001f);
        staminaBarFill.transform.localScale = new Vector3(staminaBarWidth, staminaBarHeight, 1);
        staminaBarFill.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(staminaBarFill.GetComponent<MeshCollider>());
        staminaFillMat = new Material(sh);
        SetupTransparentMat(staminaFillMat, new Color(0f, 0.85f, 1f, 0.9f), 3201);
        SetupBarRenderer(staminaBarFill.GetComponent<MeshRenderer>(), staminaFillMat);
    }

    void CreateChargeBar()
    {
        var container = new GameObject("ChargeBar3D");
        container.transform.SetParent(transform);
        container.transform.localPosition = new Vector3(0, 0.7f, 0);

        Shader sh = FindUnlitShader();

        // 배경
        chargeBarBg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        chargeBarBg.name = "ChargeBar_BG";
        chargeBarBg.transform.SetParent(container.transform);
        chargeBarBg.transform.localPosition = Vector3.zero;
        chargeBarBg.transform.localScale = new Vector3(chargeBarWidth, chargeBarHeight, 1);
        chargeBarBg.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(chargeBarBg.GetComponent<MeshCollider>());
        var bgMat = new Material(sh);
        SetupTransparentMat(bgMat, new Color(0.15f, 0.1f, 0f, 0.7f), 3202);
        SetupBarRenderer(chargeBarBg.GetComponent<MeshRenderer>(), bgMat);

        // 채우기
        chargeBarFill = GameObject.CreatePrimitive(PrimitiveType.Quad);
        chargeBarFill.name = "ChargeBar_Fill";
        chargeBarFill.transform.SetParent(container.transform);
        chargeBarFill.transform.localPosition = new Vector3(0, 0, -0.001f);
        chargeBarFill.transform.localScale = new Vector3(0, chargeBarHeight, 1);
        chargeBarFill.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(chargeBarFill.GetComponent<MeshCollider>());
        chargeFillMat = new Material(sh);
        SetupTransparentMat(chargeFillMat, new Color(1f, 0.7f, 0.1f, 0.9f), 3203);
        SetupBarRenderer(chargeBarFill.GetComponent<MeshRenderer>(), chargeFillMat);

        // 처음에는 숨김
        chargeBarBg.SetActive(false);
        chargeBarFill.SetActive(false);
    }

    void UpdateStaminaBar3D()
    {
        if (staminaBarBg == null || staminaBarFill == null || mainCam == null) return;

        // 빌보드
        staminaBarBg.transform.parent.rotation = mainCam.transform.rotation;

        // 풀일 때 숨기기
        bool show = StaminaPercent < 0.99f;
        staminaBarBg.SetActive(show);
        staminaBarFill.SetActive(show);
        if (!show) return;

        // 스케일 (왼쪽 정렬)
        float pct = StaminaPercent;
        staminaBarFill.transform.localScale = new Vector3(staminaBarWidth * pct, staminaBarHeight, 1);
        staminaBarFill.transform.localPosition = new Vector3(
            -staminaBarWidth * (1 - pct) * 0.5f, 0, -0.001f);

        // 색상: 탈진 → 파란 깜빡 / 정상 → 주황~청록 보간
        if (isExhausted)
        {
            float blink = Mathf.Sin(Time.time * 8f) > 0 ? 0.8f : 0.3f;
            SetMatColor(staminaFillMat, new Color(0.4f, 0.4f, 0.8f, blink));
        }
        else
        {
            Color c = Color.Lerp(new Color(1f, 0.3f, 0f), new Color(0f, 0.9f, 1f), pct);
            SetMatColor(staminaFillMat, c);
        }
    }

    void UpdateChargeBar3D()
    {
        if (chargeBarBg == null || chargeBarFill == null) return;

        bool show = state == CombatState.HeavyCharge;
        chargeBarBg.SetActive(show);
        chargeBarFill.SetActive(show);
        if (!show) return;

        // 빌보드
        if (mainCam != null)
            chargeBarBg.transform.parent.rotation = mainCam.transform.rotation;

        float pct = ChargePercent;

        // 스케일 (왼쪽 정렬)
        chargeBarFill.transform.localScale = new Vector3(chargeBarWidth * pct, chargeBarHeight, 1);
        chargeBarFill.transform.localPosition = new Vector3(
            -chargeBarWidth * (1 - pct) * 0.5f, 0, -0.001f);

        // 색상: 주황→빨강, 풀차지면 깜빡
        if (heavyFullyCharged)
        {
            float blink = Mathf.Sin(Time.time * 10f) > 0 ? 1f : 0.5f;
            SetMatColor(chargeFillMat, new Color(1f, 0.1f, 0.1f, blink));
        }
        else
        {
            Color c = Color.Lerp(new Color(1f, 0.8f, 0.2f), new Color(1f, 0.3f, 0f), pct);
            SetMatColor(chargeFillMat, c);
        }
    }

    Shader FindUnlitShader()
    {
        // Sprites/Default — 항상 포함, 라이팅 영향 없음, 투명 지원
        Shader s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Color");
        if (s == null) s = Shader.Find("UI/Default");
        return s;
    }

    void SetupTransparentMat(Material mat, Color color, int queue)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }
        else
        {
            mat.color = color;
        }
        mat.renderQueue = queue;
    }

    void SetupBarRenderer(MeshRenderer renderer, Material mat)
    {
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    void SetMatColor(Material mat, Color color)
    {
        if (mat == null) return;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;
    }

    #endregion

    #region ===== 시각 피드백 =====

    void SetTint(Color color)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = color;

        foreach (var r in renderers)
        {
            if (r is SpriteRenderer sr)
                sr.color = color;
        }
    }

    void RestoreTint()
    {
        SetTint(originalColor);
    }

    void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            var c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }
    }

    #endregion

    #region ===== 콜백 =====

    void OnDamaged(float amount)
    {
        if (IsInvincible) return;

        if (state != CombatState.Dodge)
        {
            // 피격 시 차징 캔슬
            if (state == CombatState.HeavyCharge)
            {
                canMove = true;
            }

            state = CombatState.Idle;
            comboStep = 0;
            comboTimer = 0;

            attackFlashTimer = 0.15f;
            SetTint(new Color(1f, 0.3f, 0.3f));

            animController?.PlayOneShot("gethit");

            // 의료 시스템 연동 — 피격 시 부상 판정
            if (medical != null && health != null)
                medical.OnDamageTaken(amount, health.MaxHp);
        }
    }

    void OnDeath()
    {
        state = CombatState.Idle;
        canMove = false;
        animController?.PlayOneShot("death");
    }

    void ReturnToIdle()
    {
        if (state == CombatState.LightAttack || state == CombatState.HeavyRelease)
            state = CombatState.Idle;
    }

    #endregion
}
