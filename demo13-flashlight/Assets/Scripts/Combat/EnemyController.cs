using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 통합 컨트롤러 — 탑다운 2D (Rigidbody2D 기반, NavMesh 없음).
/// AI 상태머신(순찰/추격/예비동작/공격/피격/스턴/사망) + 그로기 시스템.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyController : MonoBehaviour
{
    #region 열거형 & 필드

    public enum State { Patrol, Chase, AttackWindup, Attack, Hit, Stunned, Dead }

    /// <summary>활성 적 레지스트리 — PlayerVision(FOV 시야콘)이 순회.</summary>
    public static readonly List<EnemyController> All = new List<EnemyController>();
    bool _visionVisible = true;

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
    Rigidbody2D    _rb;
    CombatFeedback feedback;
    AttackPerformer _performer;
    NavAgent       _nav;          // 격자 A* 길찾기 (추격 시)
    Vector2        _attackDir = Vector2.right;
    SpriteRenderer spriteRenderer;
    Renderer[]     renderers;
    Color          originalColor = Color.white;

    Vector2 spawnPos;
    Vector2 patrolTarget;
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

    float Damage          => unitStat != null ? unitStat.attackDamage        : attackDamage;
    float AtkRange        => unitStat != null ? unitStat.attackRange         : attackRange;
    float AtkCooldown     => 1f / Mathf.Max(unitStat != null ? unitStat.attackSpeed : attackSpeed, 0.1f);
    float DetectRng       => unitStat != null ? unitStat.detectRange         : detectRange;
    float LoseRng         => unitStat != null ? unitStat.loseRange           : loseRange;
    float MoveSpd         => unitStat != null ? unitStat.moveSpeed           : moveSpeed;
    float PatrolSpd       => unitStat != null ? unitStat.patrolSpeed         : patrolSpeed;
    float PatrolRad       => unitStat != null ? unitStat.patrolRadius        : patrolRadius;
    float HitStun         => unitStat != null ? unitStat.hitStunDuration     : 0.3f;
    float PatrolWait      => unitStat != null ? unitStat.patrolWaitTime      : patrolWaitTime;
    float Windup          => unitStat != null ? unitStat.attackWindup        : attackWindup;
    bool  CanBeCancelled  => unitStat != null ? unitStat.canBeCancelled      : true;
    float MaxGroggy       => unitStat != null ? unitStat.maxGroggy           : maxGroggy;
    float GroggyDecayRate => unitStat != null ? unitStat.groggyDecay         : groggyDecay;
    float StunDuration    => unitStat != null ? unitStat.groggyStunDuration  : groggyStunDuration;

    public State CurrentState  => state;
    public bool  IsInWindup    => state == State.AttackWindup;
    public bool  IsStunned     => isStunned;
    public bool  IsDead        => state == State.Dead;
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
        if (!v)
        {
            if (hpBarBg != null) hpBarBg.SetActive(false);
            if (hpBarFill != null) hpBarFill.SetActive(false);
        }
        // 다시 보이면 UpdateHPBar가 다음 프레임에 필요 시 재표시.
    }

    void Awake()
    {
        if (!string.IsNullOrEmpty(unitKey) && StatDB.Instance != null)
            unitStat = StatDB.Instance.GetUnit(unitKey);

        _rb                = GetComponent<Rigidbody2D>();
        _rb.bodyType       = RigidbodyType2D.Dynamic;       // 벽에 막히려면 Dynamic
        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

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

        // 적 은신 + 머리 위 말풍선 (가시성 실험) — 자동 부착
        if (GetComponent<EnemySpeechBubble>() == null)
            gameObject.AddComponent<EnemySpeechBubble>();
    }

    void Start()
    {
        spawnPos = transform.position;

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player       = playerGO.transform;
            playerHealth = playerGO.GetComponent<Health>();
        }

        if (health != null)
        {
            if (unitStat != null && unitStat.maxHp > 0f)   // 유닛 스탯의 최대 HP 적용(없으면 Health 인스펙터 기본값)
            {
                health.SetMaxHp(unitStat.maxHp);
                health.FullHeal();
            }
            health.OnDamaged += OnDamaged;
            health.OnDeath   += OnDeath;
        }

        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        CreateHPBar();
        CreateGroggyBar();
        SetPatrolTarget();
    }

    void Update()
    {
        if (state == State.Dead) return;
        attackTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Patrol:       UpdatePatrol();       break;
            case State.Chase:        UpdateChase();        break;
            case State.AttackWindup: UpdateAttackWindup(); break;
            case State.Attack:       UpdateAttack();       break;
            case State.Hit:          UpdateHit();          break;
            case State.Stunned:      UpdateStunned();      break;
        }

        UpdateGroggy();
        UpdateGroggyBar();
        UpdateHPBar();
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

        Vector2 toTarget = patrolTarget - (Vector2)transform.position;
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

        if (dist <= AtkRange && attackTimer <= 0)
        {
            state = State.AttackWindup;
            _nav?.Stop();
            windupTimer = Windup;
            windupFlashTimer = 0;
            SetVelocity(Vector2.zero);
            FacePlayer();
            return;
        }

        // 길찾기 방향(벽 우회). 경로 없으면 직진 폴백.
        Vector2 d;
        if (_nav != null)
        {
            _nav.SetDestination(player.position);
            d = _nav.DesiredDirection;
            if (d.sqrMagnitude < 0.0001f)
                d = ((Vector2)player.position - (Vector2)transform.position).normalized;
        }
        else
        {
            d = ((Vector2)player.position - (Vector2)transform.position).normalized;
        }

        SetVelocity(d * MoveSpd);
        FlipSprite(d);
        animController?.Play("walk");
    }

    void UpdateAttackWindup()
    {
        windupTimer -= Time.deltaTime;
        SetVelocity(Vector2.zero);
        windupFlashTimer += Time.deltaTime;
        SetTint(Mathf.Sin(windupFlashTimer * 15f) > 0 ? new Color(1f, 0.2f, 0.2f) : originalColor);
        if (windupTimer <= 0) { RestoreTint(); DoAttack(); }
    }

    void UpdateAttack()
    {
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
        state       = State.Attack;
        attackTimer = AtkCooldown;
        windupFlashTimer = 0;
        FacePlayer();

        Vector2 dir = player != null
            ? ((Vector2)player.position - (Vector2)transform.position).normalized
            : _attackDir;
        _attackDir = dir;

        if (feedback != null) feedback.DoLightLunge(dir);

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
        if (player != null && DistToPlayer() <= AtkRange * 1.5f)
        {
            playerHealth?.TakeDamage(Damage);
            DamagePopup.Create(player.position, Damage, DamagePopup.DamageType.Normal);
        }
    }

    public void TryCancelAttack()
    {
        if (!CanBeCancelled || state != State.AttackWindup) return;
        state    = State.Hit;
        hitTimer = HitStun * 1.5f;
        SetVelocity(Vector2.zero);
        RestoreTint();
        SetTint(new Color(1f, 1f, 0.5f));
        animController?.PlayOneShot("gethit");
    }

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
        AddGroggy(groggy);

        DamagePopup.Create(transform.position, damage, DamagePopup.DamageType.Normal);

        if (knockbackDir != Vector2.zero && _rb != null)
            _rb.AddForce(knockbackDir.normalized * 3f, ForceMode2D.Impulse);
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

    void CreateHPBar()
    {
        var c = new GameObject("HPBar");
        c.transform.SetParent(transform);
        c.transform.localPosition = new Vector3(0, 0.7f, 0);
        hpBarBg   = MakeBar("HPBar_BG",   c.transform, new Color(0.1f, 0.1f, 0.1f, 0.8f), 0, BAR_W, BAR_H);
        hpBarFill = MakeBar("HPBar_Fill", c.transform, Color.green, 1, BAR_W, BAR_H);
        hpFillMat = hpBarFill.GetComponent<SpriteRenderer>().material;
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
        c.transform.localPosition = new Vector3(0, 0.85f, 0);
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
        FacePlayer();
        if (state == State.AttackWindup) windupFlashTimer = 0;
        state    = State.Hit;
        hitTimer = HitStun;
        SetVelocity(Vector2.zero);
        SetTint(new Color(1f, 0.5f, 0.5f));
        animController?.PlayOneShot("gethit");
    }

    void OnDeath()
    {
        state         = State.Dead;
        _rb.simulated = false;
        RestoreTint();
        animController?.PlayOneShot("death");
        if (hpBarBg   != null) hpBarBg.SetActive(false);
        if (hpBarFill != null) hpBarFill.SetActive(false);

        if (QuestManager.Instance != null && !string.IsNullOrEmpty(unitKey))
            QuestManager.Instance.UpdateObjective(ObjectiveType.KillEnemy, unitKey, 1);

        DropLoot();

        Destroy(gameObject, 3f);
    }

    /// <summary>전리품 드랍 — 적별 전용 드랍 테이블(unitStat.drops) 우선, 비면 지상 티어 region 루트 폴백.
    /// GameTuning.enemyDropChance로 전역 게이트.</summary>
    void DropLoot()
    {
        float gate = GameTuning.Instance != null ? GameTuning.Instance.enemyDropChance : 1f;
        if (Random.value > gate) return;

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
                if (qty > 0) DropOne(new ItemInstance(data, qty));
            }
            return;
        }

        // 폴백: 지역(Ground) 루트
        var loot = RegionLootCatalog.RollForActiveRegion(RegionLootTier.GroundDay);
        if (loot == null) return;
        for (int i = 0; i < loot.Length; i++)
            if (loot[i] != null) DropOne(loot[i]);
    }

    void DropOne(ItemInstance item)
    {
        Vector2 r = Random.insideUnitCircle * 0.6f;
        WorldItem.Drop(item, transform.position + new Vector3(r.x, r.y, 0f));
    }

    void OnGroggyTriggered() { state = State.Stunned; SetVelocity(Vector2.zero); }

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

    void SetVelocity(Vector2 v) { if (_rb != null) _rb.linearVelocity = v; }

    float DistToPlayer()
        => player == null ? float.MaxValue : Vector2.Distance(transform.position, player.position);

    void FacePlayer()
    {
        if (player == null) return;
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        animController?.SetDirection(dir);
        FlipSprite(dir);
    }

    void FlipSprite(Vector2 dir)
    {
        if (spriteRenderer != null && Mathf.Abs(dir.x) > 0.01f)
            spriteRenderer.flipX = dir.x < 0f;
    }

    void SetTint(Color c)
    {
        if (spriteRenderer != null) spriteRenderer.color = c;
        foreach (var r in renderers) if (r is SpriteRenderer sr) sr.color = c;
    }

    void RestoreTint() => SetTint(originalColor);

    #endregion
}
