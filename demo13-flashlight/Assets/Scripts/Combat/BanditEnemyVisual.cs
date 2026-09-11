using UnityEngine;

/// <summary>Bandit model presentation. AI and damage remain owned by EnemyController.</summary>
public sealed class BanditEnemyVisual : MonoBehaviour
{
    static readonly int Speed = Animator.StringToHash("Speed");
    static readonly int Locomotion = Animator.StringToHash("Base Layer.Locomotion");
    static readonly int PatrolLocomotion = Animator.StringToHash("Base Layer.PatrolLocomotion");
    static readonly int Walk = Animator.StringToHash("Base Layer.SwordWalk");
    static readonly int Slash = Animator.StringToHash("Base Layer.SwordSlash");
    EnemyController owner;
    Rigidbody body;
    Transform view;
    Animator animator;
    Renderer[] meshes;
    Color[][] baseColors;
    MaterialPropertyBlock block;
    Vector2 facing = Vector2.down;
    int motion, holdLayer;
    /// <summary>걷기·달리기 클립이 원래 몇 m/s용인가(보폭 실측 기준). 발 미끄러짐 보정용.</summary>
    const float WalkClipSpeed = 0.34f, RunClipSpeed = 0.52f;
    // Bandit_BatAttack: ready -> backswing -> contact -> recovery.
    const float WindupEndPhase = .30f, ContactPhase = .385f;
    float windupLength, attackLength, attackTime, impactTime;
    bool winding, attacking, dead;
    bool combatReady;
    Transform[] windupBones;
    Vector3[] windupPositions;
    Quaternion[] windupRotations;
    float deathTime;

    // 총기 밴딧(2026-09-11) — 시안 리그(BanditFirearmPreview)를 AI 상태로 구동한다.
    BanditFirearmPreview gun;
    float gunDraw, gunClock, sinceShot = 10f;
    LineRenderer aimLine;
    Material aimMat;
    static int aimMask = -1;
    /// <summary>꺼내기/넣기 시간(초). 시안은 1.2초 — 교전에선 굼떠 보여 절반으로. (GameTuning)</summary>
    static float GunDrawTime => GameTuning.Instance != null ? GameTuning.Instance.enemyGunDrawTime : .6f;
    /// <summary>총 든 걷기 클립의 보폭 속도(m/s) — 발 미끄러짐 보정 기준.</summary>
    const float GunWalkClipSpeed = .9f;

    /// <summary>총을 다 꺼냈나(근접 모델은 항상 true). EnemyController가 조준 시작 조건으로 읽는다.</summary>
    public bool GunReady => gun == null || gunDraw >= 1f;
    public Vector3 MuzzlePosition => gun != null && gun.Muzzle != null ? gun.Muzzle.position : transform.position + Vector3.up;
    public void NotifyShot() { sinceShot = 0f; }

    public bool Initialize(EnemyController enemy, float scale, string model = "Characters/Bandit01")
    {
        var prefab = Resources.Load<GameObject>(model);
        if (prefab == null) return false;
        owner = enemy; body = GetComponent<Rigidbody>();
        block = new MaterialPropertyBlock();
        view = Instantiate(prefab, transform, false).transform;
        view.name = "Bandit3D"; view.localScale = Vector3.one * scale;
        animator = view.GetComponentInChildren<Animator>();
        if (animator == null) { Destroy(view.gameObject); return false; }
        animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        gun = view.GetComponent<BanditFirearmPreview>();
        if (gun != null)
        {
            // 컨트롤러 없는 리그 — 자세는 Sample()로 직접 잡는다. 시안 자체 Update(자동 연출·가짜 트레이서)는 끈다.
            gun.autoPreview = false; gun.target = null; gun.enabled = false;
            gun.Initialize();
            animator.enabled = false;
            holdLayer = -1;
            CreateAimLine();
        }
        else holdLayer = animator.GetLayerIndex("SwordHold");
        foreach (Transform t in view.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
        meshes = view.GetComponentsInChildren<Renderer>(true);

        // FBX에 저작된 머티리얼을 그대로 쓰고 그림자 설정만 맞춘다.
        //   (피격 플래시 등은 MaterialPropertyBlock으로 쓰므로 공유 에셋을 오염시키지 않는다.)
        foreach (var r in meshes)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
        }

        baseColors = new Color[meshes.Length][];
        for (int i = 0; i < meshes.Length; i++)
        {
            var materials = meshes[i].sharedMaterials; baseColors[i] = new Color[materials.Length];
            for (int j = 0; j < materials.Length; j++)
                baseColors[i][j] = materials[j].HasProperty("_BaseColor") ? materials[j].GetColor("_BaseColor") : Color.white;
        }
        SetFacing(facing); return true;
    }

    public void SetFacing(Vector2 direction)
    {
        if (dead || attacking || direction.sqrMagnitude < .0001f) return;
        facing = direction.normalized;
        view.localRotation = Quaternion.Euler(0, Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg, 0);
    }
    bool isVisible = true;
    public void SetVisible(bool visible) { isVisible = visible; if (meshes != null) foreach (var r in meshes) r.enabled = visible; }
    public void SetTint(Color color)
    {
        if (meshes == null) return;
        if (dead) color = new Color(.45f,.45f,.45f);
        for (int i = 0; i < meshes.Length; i++) for (int j = 0; j < baseColors[i].Length; j++)
        {
            meshes[i].GetPropertyBlock(block, j);
            block.SetColor("_BaseColor", baseColors[i][j] * color);
            meshes[i].SetPropertyBlock(block, j);
        }
    }
    public void Windup(float duration)
    {
        // Close-range detection can enter windup directly from the one-hand carry.
        // Capture that pose so the handover blends instead of snapping to two hands.
        if (animator != null && (!combatReady || animator.IsInTransition(0)))
        {
            windupBones = animator.GetComponentsInChildren<Transform>();
            windupPositions = new Vector3[windupBones.Length];
            windupRotations = new Quaternion[windupBones.Length];
            for (int i = 0; i < windupBones.Length; i++)
            { windupPositions[i] = windupBones[i].localPosition; windupRotations[i] = windupBones[i].localRotation; }
        }
        else windupBones = null;
        combatReady = true; winding = true; attacking = false;
        windupLength = Mathf.Max(.01f, duration); attackTime = 0;
    }
    public void Attack(float duration, float hitAt)
    {
        winding = false; attacking = true; combatReady = true; windupBones = null; attackTime = 0;
        attackLength = Mathf.Max(.05f, duration); impactTime = Mathf.Clamp(hitAt, .001f, attackLength - .001f);
    }
    // EnemyController supplies the same clock used for damage; never advance it again in LateUpdate.
    public void SetAttackElapsed(float elapsed) { attackTime = Mathf.Max(0, elapsed); }
    public void SetWindupRemaining(float remaining) { attackTime = Mathf.Max(0, windupLength - remaining); }
    public void CancelAttack() { winding = attacking = false; windupBones = null; motion = 0; }
    public void Die()
    {
        CancelAttack(); dead = true; deathTime = 0; SetVisible(true); SetTint(new Color(.45f,.45f,.45f));
        if (aimLine != null) aimLine.enabled = false;
    }

    void LateUpdate()
    {
        if (animator == null) return;
        float dt = Time.deltaTime;
        if (dead)
        {
            animator.speed = 0; deathTime += dt;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(deathTime / .35f));
            view.localRotation = Quaternion.Euler(0, Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg, 0) * Quaternion.Euler(0,0,85*t);
            view.localPosition = new Vector3(0,.28f*t,0); return;
        }
        if (gun != null) { UpdateGun(dt); return; }
        if (owner.CurrentState == EnemyController.State.Hit || owner.IsStunned) CancelAttack();
        if (winding || attacking)
        {
            float phase = winding ? Mathf.Lerp(0,WindupEndPhase,Mathf.Clamp01(attackTime/windupLength))
                : attackTime <= impactTime ? Mathf.Lerp(WindupEndPhase,ContactPhase,attackTime/impactTime)
                : Mathf.Lerp(ContactPhase,1,Mathf.Clamp01((attackTime-impactTime)/(attackLength-impactTime)));
            if (holdLayer >= 0) animator.SetLayerWeight(holdLayer,0);
            animator.speed = 0; animator.Play(Slash,0,phase); animator.Update(0);
            if (winding && windupBones != null)
            {
                float blend = Mathf.SmoothStep(0, 1, Mathf.Clamp01(attackTime / Mathf.Min(.22f, windupLength)));
                for (int i = 0; i < windupBones.Length; i++)
                {
                    var bone = windupBones[i];
                    bone.localPosition = Vector3.Lerp(windupPositions[i], bone.localPosition, blend);
                    bone.localRotation = Quaternion.Slerp(windupRotations[i], bone.localRotation, blend);
                }
                if (blend >= 1) windupBones = null;
            }
            motion = Slash; return;
        }
        Vector2 velocity = new Vector2(body.linearVelocity.x, body.linearVelocity.z);
        float speed = velocity.magnitude;
        bool moving = speed > .08f, running = speed > 1.7f;
        if (moving) SetFacing(velocity);
        if (owner.IsEngagingPlayer) combatReady = true;
        else if (owner.CurrentState == EnemyController.State.Patrol || owner.CurrentState == EnemyController.State.Investigate) combatReady = false;
        int next = !combatReady ? PatrolLocomotion : moving && !running ? Walk : Locomotion;
        // 발 미끄러짐 — 클립은 0.3~0.5m/s용인데 몸은 1.9~2.5m/s로 나간다.
        //   예전 분모(2.5/1.2)는 "설정 이동속도"라 전속이면 늘 배속 1 = 클립 고유 속도였다.
        //   실제 보폭 기준으로 나누고, 다리가 뭉개지지 않게 상한을 둔다(플레이어와 같은 규칙).
        animator.speed = moving ? Mathf.Clamp(speed / (running ? RunClipSpeed : WalkClipSpeed), .65f, 2.4f) : 1;
        if (holdLayer >= 0) animator.SetLayerWeight(holdLayer,next == Locomotion ? 1 : 0);
        animator.SetFloat(Speed,running ? 2 : moving && !combatReady ? 1 : 0,.1f,dt);
        if (motion != next)
        {
            bool handover = next == PatrolLocomotion || motion == PatrolLocomotion;
            animator.CrossFadeInFixedTime(next, handover ? .28f : .10f); motion = next;
        }
    }

    /// <summary>총기 모드 — 교전이면 총을 꺼내 들고(플레이어를 본다), 순찰이면 넣는다.
    /// 조준(AttackWindup)·사격(Attack) 중엔 AI의 조준 방향을 본다.</summary>
    void UpdateGun(float dt)
    {
        var st = owner.CurrentState;
        bool aiming = st == EnemyController.State.AttackWindup || st == EnemyController.State.Attack;
        bool engaged = aiming || st == EnemyController.State.Chase || st == EnemyController.State.Hit
                    || st == EnemyController.State.Stunned;
        gunDraw = Mathf.MoveTowards(gunDraw, engaged ? 1f : 0f, dt / GunDrawTime);
        sinceShot += dt;

        Vector2 velocity = new Vector2(body.linearVelocity.x, body.linearVelocity.z);
        float speed = velocity.magnitude;
        bool moving = speed > .08f;
        Vector2 look = Vector2.zero;
        if (aiming) look = owner.AimDirection;
        else if (engaged && owner.Target != null) look = Plan3D.ToPlan(owner.Target.position) - Plan3D.ToPlan(transform.position);
        else if (moving) look = velocity;
        if (look.sqrMagnitude > .0001f)
        {
            facing = look.normalized;
            view.localRotation = Quaternion.Euler(0, Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg, 0);
        }

        gunClock += dt * (moving ? Mathf.Clamp(speed / GunWalkClipSpeed, .65f, 2.2f) : 1f);
        // 시야 밖에서 총을 넣고 순찰 중이면 자세를 안 잡는다 — 클립 샘플링이 적마다 매 프레임 돈다.
        //   보이게 되는 순간(또는 교전) 다시 잡으므로 화면엔 차이가 없다.
        if (isVisible || engaged || gunDraw > 0f) gun.Sample(gunClock, gunDraw, moving, sinceShot);
        UpdateAimLine(st);
    }

    void CreateAimLine()
    {
        // 셰이더가 빌드에서 빠지면 new Material(null)이 던져 EnemyController.Start 전체가 멈춘다 — 조준선만 포기한다.
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) return;
        var go = new GameObject("AimLine");
        go.transform.SetParent(transform, false);
        aimLine = go.AddComponent<LineRenderer>();
        aimMat = new Material(shader);
        aimLine.sharedMaterial = aimMat;
        aimLine.positionCount = 2;
        aimLine.useWorldSpace = true;
        aimLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        aimLine.receiveShadows = false;
        aimLine.enabled = false;
    }

    /// <summary>조준선 — 조준 중에만. 고정(발사 직전)되면 굵어지고 노랗게 깜빡인다.
    /// 시야(FOV) 밖이어도 보인다: "어디서 겨누고 있다"는 경고가 핵심이라서.</summary>
    void UpdateAimLine(EnemyController.State st)
    {
        if (aimLine == null) return;
        bool on = !dead && st == EnemyController.State.AttackWindup;
        aimLine.enabled = on;
        if (!on) return;
        bool locked = owner.AimLocked;
        // 고정 전에도 화면에서 읽혀야 한다 — 0.025m였을 땐 1280×720 쿼터뷰에서 1~2px이라 안 보였다(2026-09-11 촬영).
        float w = locked ? .07f : .045f;
        aimLine.startWidth = w; aimLine.endWidth = w * .6f;
        aimMat.SetColor("_BaseColor", locked && Mathf.Sin(Time.time * 45f) > 0 ? new Color(1f, .9f, .3f) : new Color(1f, .12f, .08f));

        Vector3 from = MuzzlePosition;
        var a = owner.AimDirection;
        var dir = new Vector3(a.x, 0f, a.y);
        float len = owner.FireRange;
        if (aimMask == -1) aimMask = ~(1 << LayerMask.NameToLayer("Enemy") | 1 << LayerMask.NameToLayer("Player"));
        if (Physics.Raycast(from, dir, out var hit, len, aimMask, QueryTriggerInteraction.Ignore)) len = hit.distance;   // 벽에서 끊는다
        aimLine.SetPosition(0, from);
        aimLine.SetPosition(1, from + dir * len);
    }

    void OnDestroy() { if (aimMat != null) Destroy(aimMat); }
}
