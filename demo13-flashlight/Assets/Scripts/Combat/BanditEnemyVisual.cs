using UnityEngine;

/// <summary>Bandit model presentation. AI and damage remain owned by EnemyController.</summary>
public sealed class BanditEnemyVisual : MonoBehaviour
{
    static readonly int Speed = Animator.StringToHash("Speed");
    static readonly int Locomotion = Animator.StringToHash("Base Layer.Locomotion");
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
    readonly System.Collections.Generic.List<Material> _owned = new System.Collections.Generic.List<Material>();   // 새로 만든 머티리얼 — 파괴 시 정리
    /// <summary>걷기·달리기 클립이 원래 몇 m/s용인가(보폭 실측 기준). 발 미끄러짐 보정용.</summary>
    const float WalkClipSpeed = 0.34f, RunClipSpeed = 0.52f;
    float windupLength, attackLength, attackTime, impactTime;
    bool winding, attacking, dead;
    float deathTime;

    public bool Initialize(EnemyController enemy, float scale)
    {
        var prefab = Resources.Load<GameObject>("Characters/Bandit01");
        if (prefab == null) return false;
        owner = enemy; body = GetComponent<Rigidbody>();
        block = new MaterialPropertyBlock();
        view = Instantiate(prefab, transform, false).transform;
        view.name = "Bandit3D"; view.localScale = Vector3.one * scale;
        animator = view.GetComponentInChildren<Animator>();
        if (animator == null) { Destroy(view.gameObject); return false; }
        animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        holdLayer = animator.GetLayerIndex("SwordHold");
        foreach (Transform t in view.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
        meshes = view.GetComponentsInChildren<Renderer>(true);

        // 카툰 룩 — 플레이어(ChibiPlayerVisual)와 **같은 셰이더**로 갈아끼운다.
        //   적만 사실적 음영이면 한 화면에서 룩이 갈려, 카툰 외곽선의 의미가 사라진다.
        //   프리팹 머티리얼은 색만 가져오고 인스턴스를 새로 만든다(원본 에셋 오염 방지).
        ToonMaterial.ApplyTo(view.gameObject, "Bandit", _owned);

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
    public void SetVisible(bool visible) { if (meshes != null) foreach (var r in meshes) r.enabled = visible; }
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
    public void Windup(float duration) { winding = true; attacking = false; windupLength = Mathf.Max(.01f, duration); attackTime = 0; }
    public void Attack(float duration, float hitAt)
    {
        winding = false; attacking = true; attackTime = 0;
        attackLength = Mathf.Max(.05f, duration); impactTime = Mathf.Clamp(hitAt, .001f, attackLength - .001f);
    }
    public void CancelAttack() { winding = attacking = false; motion = 0; }
    public void Die() { CancelAttack(); dead = true; deathTime = 0; SetVisible(true); SetTint(new Color(.45f,.45f,.45f)); }

    void OnDestroy()
    {
        foreach (var m in _owned) if (m != null) Destroy(m);
        _owned.Clear();
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
        if (owner.CurrentState == EnemyController.State.Hit || owner.IsStunned) CancelAttack();
        if (winding || attacking)
        {
            attackTime += dt;
            float phase = winding ? Mathf.Lerp(0,.40f,Mathf.Clamp01(attackTime/windupLength))
                : attackTime <= impactTime ? Mathf.Lerp(.40f,.56f,attackTime/impactTime)
                : Mathf.Lerp(.56f,1,Mathf.Clamp01((attackTime-impactTime)/(attackLength-impactTime)));
            if (holdLayer >= 0) animator.SetLayerWeight(holdLayer,0);
            animator.speed = 0; animator.Play(Slash,0,phase); animator.Update(0);
            motion = Slash; return;
        }
        Vector2 velocity = new Vector2(body.linearVelocity.x, body.linearVelocity.z);
        float speed = velocity.magnitude;
        bool moving = speed > .08f, running = speed > 1.7f;
        if (moving) SetFacing(velocity);
        int next = moving && !running ? Walk : Locomotion;
        // 발 미끄러짐 — 클립은 0.3~0.5m/s용인데 몸은 1.9~2.5m/s로 나간다.
        //   예전 분모(2.5/1.2)는 "설정 이동속도"라 전속이면 늘 배속 1 = 클립 고유 속도였다.
        //   실제 보폭 기준으로 나누고, 다리가 뭉개지지 않게 상한을 둔다(플레이어와 같은 규칙).
        animator.speed = moving ? Mathf.Clamp(speed / (running ? RunClipSpeed : WalkClipSpeed), .65f, 2.4f) : 1;
        if (holdLayer >= 0) animator.SetLayerWeight(holdLayer,next == Locomotion ? 1 : 0);
        animator.SetFloat(Speed,running ? 2 : 0,.1f,dt);
        if (motion != next) { animator.CrossFadeInFixedTime(next,.10f); motion = next; }
    }
}
