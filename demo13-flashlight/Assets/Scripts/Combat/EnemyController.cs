using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 통합 컨트롤러.
/// AI 상태머신 + 그로기 시스템 + HP바 + 그로기바 + 걷기 바운스 + 스프라이트 플립.
/// EnemyAI, GroggySystem, HealthBar3D, WalkBounce, PlayerVisual(플립) 병합.
/// </summary>
public class EnemyController : MonoBehaviour
{
    #region 열거형 & 필드

    public enum State { Patrol, Chase, AttackWindup, Attack, Hit, Stunned, Dead }

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
    [SerializeField] float attackWindup = 0.8f;

    [Header("Groggy")]
    [SerializeField] float maxGroggy = 100f;
    [SerializeField] float groggyDecay = 8f;
    [SerializeField] float groggyStunDuration = 2.0f;

    [Header("Data")]
    [SerializeField] string unitKey; // StatDB 키
    UnitStatData unitStat; // 런타임 캐시

    [Header("References")]
    [SerializeField] SkeletonAnimController animController;

    // AI 상태
    State state = State.Patrol;
    Transform player;
    Health health;
    Health playerHealth;
    PlayerController playerController;
    NavMeshAgent agent;
    CombatFeedback feedback;

    Vector3 spawnPos;
    float patrolTimer;
    float attackTimer;
    float hitTimer;
    float windupTimer;

    // 스프라이트 플립
    SpriteRenderer spriteRenderer;
    Renderer[] renderers;
    Color originalColor = Color.white;
    float windupFlashTimer;
    Camera mainCam;

    // 그로기 상태
    float currentGroggy;
    float stunTimer;
    bool isStunned;

    // HP바
    GameObject hpBarBg;
    GameObject hpBarFill;
    Material hpFillMat;
    float hpBarWidth = 0.8f;
    float hpBarHeight = 0.08f;

    // 그로기바
    GameObject groggyBarBg;
    GameObject groggyBarFill;
    Material groggyFillMat;
    float groggyBarWidth = 0.6f;
    float groggyBarHeight = 0.06f;

    // 걷기 바운스
    [Header("Walk Bounce")]
    [SerializeField] float bounceHeight = 0.035f;
    [SerializeField] float bounceSpeed = 14f;
    [SerializeField] float squashAmount = 0.06f;

    Transform visualRoot;
    Vector3 baseLocalPos;
    Vector3 baseLocalScale;
    float bounceTimer;
    float currentBounce;
    bool wasMoving;
    Vector3 lastPosition;
    float positionDelta;

    #endregion

    #region 프로퍼티 (UnitStatData > 인스펙터 필드값)

    float Damage => unitStat != null ? unitStat.attackDamage : attackDamage;
    float AtkRange => unitStat != null ? unitStat.attackRange : attackRange;
    float AtkCooldown => 1f / Mathf.Max(
        unitStat != null ? unitStat.attackSpeed : attackSpeed, 0.1f);
    float DetectRng => unitStat != null ? unitStat.detectRange : detectRange;
    float LoseRng => unitStat != null ? unitStat.loseRange : loseRange;
    float MoveSpd => unitStat != null ? unitStat.moveSpeed : moveSpeed;
    float PatrolSpd => unitStat != null ? unitStat.patrolSpeed : patrolSpeed;
    float PatrolRad => unitStat != null ? unitStat.patrolRadius : patrolRadius;
    float HitStun => unitStat != null ? unitStat.hitStunDuration : 0.3f;
    float PatrolWait => unitStat != null ? unitStat.patrolWaitTime : patrolWaitTime;
    float Windup => unitStat != null ? unitStat.attackWindup : attackWindup;
    bool CanBeCancelled => unitStat != null ? unitStat.canBeCancelled : true;

    float MaxGroggy => unitStat != null ? unitStat.maxGroggy : maxGroggy;
    float GroggyDecayRate => unitStat != null ? unitStat.groggyDecay : groggyDecay;
    float StunDuration => unitStat != null ? unitStat.groggyStunDuration : groggyStunDuration;

    public State CurrentState => state;

    /// <summary>예비동작 중인지 (외부에서 판별용)</summary>
    public bool IsInWindup => state == State.AttackWindup;

    /// <summary>그로기 스턴 중인지</summary>
    public bool IsStunned => isStunned;

    /// <summary>그로기 게이지 비율 (0~1)</summary>
    public float GroggyPercent => currentGroggy / MaxGroggy;

    #endregion

    #region 유니티 라이프사이클

    /// <summary>외부에서 유닛 키 지정 (스폰 시스템 등)</summary>
    public void SetUnitKey(string key)
    {
        unitKey = key;
        if (StatDB.Instance != null)
            unitStat = StatDB.Instance.GetUnit(unitKey);
    }

    void Awake()
    {
        // StatDB에서 유닛 스탯 로드
        if (!string.IsNullOrEmpty(unitKey) && StatDB.Instance != null)
            unitStat = StatDB.Instance.GetUnit(unitKey);

        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        feedback = GetComponent<CombatFeedback>();

        if (animController == null)
            animController = GetComponentInChildren<SkeletonAnimController>();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        renderers = GetComponentsInChildren<Renderer>();
        mainCam = Camera.main;

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.speed = MoveSpd;
            agent.acceleration = 50f;
            agent.angularSpeed = 0f;
            agent.stoppingDistance = 0.3f;
        }

        // 걷기 바운스용 Root 자식 탐색
        var root = transform.Find("Root");
        visualRoot = root != null ? root : null;

        if (visualRoot != null)
        {
            baseLocalPos = visualRoot.localPosition;
            baseLocalScale = visualRoot.localScale;
        }

        lastPosition = transform.position;
    }

    void Start()
    {
        spawnPos = transform.position;

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            playerHealth = playerGO.GetComponent<Health>();
            playerController = playerGO.GetComponent<PlayerController>();
        }

        if (health != null)
        {
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
        }

        // 원본 색상 저장
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // HP바 & 그로기바 생성
        CreateHPBar();
        CreateGroggyBar();

        SetPatrolTarget();
    }

    void Update()
    {
        if (state == State.Dead) return;
        attackTimer -= Time.deltaTime;

        // 이동 방향에 따라 애니메이션 방향 + 스프라이트 플립
        if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 vel = agent.velocity;
            vel.y = 0;
            if (vel.sqrMagnitude > 0.01f)
            {
                Vector3 dir = vel.normalized;
                animController?.SetDirection(dir);
                FlipSprite(dir);
            }
        }

        // AI 상태 머신
        switch (state)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase: UpdateChase(); break;
            case State.AttackWindup: UpdateAttackWindup(); break;
            case State.Attack: UpdateAttack(); break;
            case State.Hit: UpdateHit(); break;
            case State.Stunned: UpdateStunned(); break;
        }

        // 그로기 업데이트
        UpdateGroggy();

        // 그로기바 업데이트
        UpdateGroggyBar();
    }

    void LateUpdate()
    {
        if (state == State.Dead) return;

        // HP바 빌보드 & 표시
        UpdateHPBar();

        // 걷기 바운스
        UpdateWalkBounce();
    }

    #endregion

    #region AI 상태 업데이트

    void UpdatePatrol()
    {
        if (player != null && DistToPlayer() < DetectRng)
        {
            state = State.Chase;

            // 스토리: 첫 전투 조우 트리거
            if (StoryTriggerManager.Instance != null)
                StoryTriggerManager.Instance.OnFirstCombatEncounter();

            return;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = PatrolSpd;

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

        // 공격 시작 → 예비동작으로
        if (dist <= AtkRange && attackTimer <= 0)
        {
            state = State.AttackWindup;
            windupTimer = Windup;
            StopAgent();

            // 플레이어 방향으로 회전
            FacePlayer();

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

    void UpdateAttackWindup()
    {
        windupTimer -= Time.deltaTime;

        // 예비동작 시각 피드백 — 빨간색 깜빡
        windupFlashTimer += Time.deltaTime;
        float flash = Mathf.Sin(windupFlashTimer * 15f);
        if (flash > 0)
            SetTint(new Color(1f, 0.2f, 0.2f));
        else
            RestoreTint();

        // 예비동작 완료 → 실제 공격
        if (windupTimer <= 0)
        {
            RestoreTint();
            DoAttack();
        }
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
            RestoreTint();
            state = State.Chase;
        }
    }

    void UpdateStunned()
    {
        // 그로기 시스템이 스턴 해제를 관리
        // 시각 피드백 — 노란색 깜빡
        float blink = Mathf.Sin(Time.time * 6f);
        if (blink > 0)
            SetTint(new Color(1f, 1f, 0.2f));
        else
            SetTint(new Color(0.6f, 0.6f, 0.1f));

        animController?.Play("idle");
    }

    #endregion

    #region 액션 (DoAttack, TryCancelAttack)

    void DoAttack()
    {
        state = State.Attack;
        attackTimer = AtkCooldown;
        windupFlashTimer = 0;

        FacePlayer();

        // 공격 돌진
        if (feedback != null && player != null)
        {
            Vector3 dir = (player.position - transform.position);
            dir.y = 0;
            feedback.DoLightLunge(dir.normalized);
        }

        animController?.PlayOneShot("attack", () =>
        {
            // 공격 판정은 모션 끝에 실행
            if (player != null && DistToPlayer() <= AtkRange * 1.5f)
            {
                // 플레이어 무적 체크
                if (playerController != null && playerController.IsInvincible)
                    return;

                playerHealth?.TakeDamage(Damage);
                DamagePopup.Create(player.position, Damage, DamagePopup.DamageType.Normal);
            }
        });

        // 애니 없으면 직접 판정
        if (animController == null)
        {
            if (player != null && DistToPlayer() <= AtkRange * 1.5f)
            {
                if (playerController == null || !playerController.IsInvincible)
                {
                    playerHealth?.TakeDamage(Damage);
                    DamagePopup.Create(player.position, Damage, DamagePopup.DamageType.Normal);
                }
            }
            state = State.Chase;
        }
    }

    /// <summary>강공격에 의한 공격 캔슬. 예비동작 중에만 성공.</summary>
    public void TryCancelAttack()
    {
        if (!CanBeCancelled) return;

        if (state == State.AttackWindup)
        {
            state = State.Hit;
            hitTimer = HitStun * 1.5f; // 캔슬 성공 시 더 긴 경직
            StopAgent();
            RestoreTint();
            SetTint(new Color(1f, 1f, 0.5f)); // 노란 플래시로 캔슬 표시
            animController?.PlayOneShot("gethit");
        }
    }

    #endregion

    #region 그로기 시스템

    /// <summary>그로기 수치 추가. 가득 차면 스턴 발동.</summary>
    public void AddGroggy(float amount)
    {
        if (isStunned) return;

        currentGroggy = Mathf.Min(currentGroggy + amount, MaxGroggy);

        if (currentGroggy >= MaxGroggy)
        {
            isStunned = true;
            stunTimer = StunDuration;
            OnGroggyTriggered();
        }
    }

    void UpdateGroggy()
    {
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0)
            {
                isStunned = false;
                currentGroggy = 0;
                OnGroggyRecovered();
            }
        }
        else if (currentGroggy > 0)
        {
            // 그로기 자연 감소
            currentGroggy = Mathf.Max(0, currentGroggy - GroggyDecayRate * Time.deltaTime);
        }
    }

    #endregion

    #region HP바 비주얼

    void CreateHPBar()
    {
        // HP바 컨테이너 — hierarchy의 HealthBar 자식 위치 사용
        var hpContainer = new GameObject("HealthBar");
        hpContainer.transform.SetParent(transform);
        // HealthBar3D는 hierarchy의 localPosition을 그대로 사용
        hpContainer.transform.localPosition = new Vector3(0, 0.85f, 0);

        Shader unlitShader = FindUnlitShader();

        // 배경
        hpBarBg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        hpBarBg.name = "HPBar_BG";
        hpBarBg.transform.SetParent(hpContainer.transform);
        hpBarBg.transform.localPosition = Vector3.zero;
        hpBarBg.transform.localScale = new Vector3(hpBarWidth, hpBarHeight, 1);
        hpBarBg.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(hpBarBg.GetComponent<MeshCollider>());
        var bgMat = new Material(unlitShader);
        SetupTransparentMat(bgMat, new Color(0.1f, 0.1f, 0.1f, 0.8f), 3200);
        SetupBarRenderer(hpBarBg.GetComponent<MeshRenderer>(), bgMat);

        // 체력바
        hpBarFill = GameObject.CreatePrimitive(PrimitiveType.Quad);
        hpBarFill.name = "HPBar_Fill";
        hpBarFill.transform.SetParent(hpContainer.transform);
        hpBarFill.transform.localPosition = new Vector3(0, 0, -0.001f);
        hpBarFill.transform.localScale = new Vector3(hpBarWidth, hpBarHeight, 1);
        hpBarFill.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(hpBarFill.GetComponent<MeshCollider>());
        hpFillMat = new Material(unlitShader);
        SetupTransparentMat(hpFillMat, Color.green, 3201);
        SetupBarRenderer(hpBarFill.GetComponent<MeshRenderer>(), hpFillMat);
    }

    void UpdateHPBar()
    {
        if (mainCam == null || health == null) return;

        // 빌보드 — 회전만 (위치는 localPosition 그대로 부모를 따라감)
        if (hpBarBg != null)
            hpBarBg.transform.parent.rotation = mainCam.transform.rotation;

        // HP 풀일 때 숨기기
        bool show = health.Percent < 0.99f;
        if (hpBarBg != null) hpBarBg.SetActive(show);
        if (hpBarFill != null) hpBarFill.SetActive(show);

        if (!show) return;

        // 스케일 조절 (왼쪽 정렬)
        float pct = health.Percent;
        hpBarFill.transform.localScale = new Vector3(hpBarWidth * pct, hpBarHeight, 1);
        hpBarFill.transform.localPosition = new Vector3(
            -hpBarWidth * (1 - pct) * 0.5f, 0, -0.001f
        );

        // 색상 보간
        Color c = Color.Lerp(Color.red, Color.green, pct);
        SetMatColor(hpFillMat, c);
    }

    #endregion

    #region 그로기바 비주얼

    void CreateGroggyBar()
    {
        Vector3 offset = new Vector3(0, 1.0f, 0);

        var container = new GameObject("GroggyBar");
        container.transform.SetParent(transform);
        container.transform.localPosition = offset;

        Shader unlitShader = FindUnlitShader();

        // 배경
        groggyBarBg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        groggyBarBg.name = "GroggyBar_BG";
        groggyBarBg.transform.SetParent(container.transform);
        groggyBarBg.transform.localPosition = Vector3.zero;
        groggyBarBg.transform.localScale = new Vector3(groggyBarWidth, groggyBarHeight, 1);
        groggyBarBg.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(groggyBarBg.GetComponent<MeshCollider>());
        var bgMat = new Material(unlitShader);
        SetupTransparentMat(bgMat, new Color(0.15f, 0.15f, 0.15f, 0.7f), 3202);
        SetupBarRenderer(groggyBarBg.GetComponent<MeshRenderer>(), bgMat);

        // 채우기
        groggyBarFill = GameObject.CreatePrimitive(PrimitiveType.Quad);
        groggyBarFill.name = "GroggyBar_Fill";
        groggyBarFill.transform.SetParent(container.transform);
        groggyBarFill.transform.localPosition = new Vector3(0, 0, -0.001f);
        groggyBarFill.transform.localScale = new Vector3(0, groggyBarHeight, 1);
        groggyBarFill.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(groggyBarFill.GetComponent<MeshCollider>());
        groggyFillMat = new Material(unlitShader);
        SetupTransparentMat(groggyFillMat, new Color(1f, 0.6f, 0f, 0.9f), 3203);
        SetupBarRenderer(groggyBarFill.GetComponent<MeshRenderer>(), groggyFillMat);

        // 처음에는 숨김
        groggyBarBg.SetActive(false);
        groggyBarFill.SetActive(false);
    }

    void UpdateGroggyBar()
    {
        if (groggyBarBg == null || groggyBarFill == null) return;

        bool show = currentGroggy > 0.1f || isStunned;
        groggyBarBg.SetActive(show);
        groggyBarFill.SetActive(show);

        if (!show) return;

        // 빌보드
        if (mainCam != null)
            groggyBarBg.transform.parent.rotation = mainCam.transform.rotation;

        float pct = currentGroggy / MaxGroggy;
        groggyBarFill.transform.localScale = new Vector3(groggyBarWidth * pct, groggyBarHeight, 1);
        groggyBarFill.transform.localPosition = new Vector3(
            -groggyBarWidth * (1 - pct) * 0.5f, 0, -0.001f);

        // 스턴 상태면 빨간색으로 깜빡
        if (isStunned)
        {
            float blink = Mathf.Sin(Time.time * 10f) > 0 ? 1f : 0.4f;
            SetMatColor(groggyFillMat, new Color(1f, 0.1f, 0.1f, blink));
        }
        else
        {
            SetMatColor(groggyFillMat, new Color(1f, 0.6f, 0f, 0.9f));
        }
    }

    #endregion

    #region 걷기 바운스

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
            visualRoot.localPosition = baseLocalPos + new Vector3(0, bounce, 0);
            visualRoot.localScale = new Vector3(
                baseLocalScale.x * squash,
                baseLocalScale.y * stretch,
                baseLocalScale.z
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
                baseLocalPos,
                Time.deltaTime * 12f
            );
            visualRoot.localScale = Vector3.Lerp(
                visualRoot.localScale,
                baseLocalScale,
                Time.deltaTime * 12f
            );
        }
    }

    bool IsBounceMoving()
    {
        // NavMeshAgent.velocity 체크 (SetDestination 사용 시)
        if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
            return true;

        // 실제 위치 변화 체크
        if (positionDelta > 0.5f)
            return true;

        return false;
    }

    #endregion

    #region 비주얼 헬퍼

    void FlipSprite(Vector3 worldDir)
    {
        if (spriteRenderer == null || mainCam == null) return;
        Vector3 camRight = mainCam.transform.right;
        camRight.y = 0;
        spriteRenderer.flipX = Vector3.Dot(worldDir, camRight.normalized) < 0;
    }

    void FacePlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude < 0.01f) return;
        dir.Normalize();
        animController?.SetDirection(dir);
        FlipSprite(dir);
    }

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

    #region 콜백

    void OnDamaged(float amount)
    {
        if (state == State.Dead) return;

        FacePlayer();

        // 예비동작 중 피격 → 일반 Hit로 전환
        if (state == State.AttackWindup)
        {
            windupFlashTimer = 0;
        }

        state = State.Hit;
        hitTimer = HitStun;
        StopAgent();

        // 피격 플래시
        SetTint(new Color(1f, 0.5f, 0.5f));

        animController?.PlayOneShot("gethit");
    }

    void OnDeath()
    {
        state = State.Dead;
        StopAgent();
        if (agent != null) agent.enabled = false;
        RestoreTint();
        animController?.PlayOneShot("death");

        // HP바 숨기기
        if (hpBarBg != null) hpBarBg.SetActive(false);
        if (hpBarFill != null) hpBarFill.SetActive(false);

        // 퀘스트 목표 갱신
        if (QuestManager.Instance != null && !string.IsNullOrEmpty(unitKey))
            QuestManager.Instance.UpdateObjective(ObjectiveType.KillEnemy, unitKey, 1);

        Destroy(gameObject, 3f);
    }

    void OnGroggyTriggered()
    {
        state = State.Stunned;
        StopAgent();
    }

    void OnGroggyRecovered()
    {
        if (state == State.Stunned)
        {
            RestoreTint();
            state = State.Chase;
        }
    }

    #endregion

    #region 유틸리티 헬퍼

    void SetPatrolTarget()
    {
        patrolTimer = 0f;
        if (agent != null && agent.isOnNavMesh)
        {
            Vector2 rnd = Random.insideUnitCircle * PatrolRad;
            Vector3 target = spawnPos + new Vector3(rnd.x, 0, rnd.y);

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

    #endregion
}
