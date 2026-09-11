using System.Collections.Generic;
using UnityEngine;

/// <summary>밴딧 사망 래그돌 — 쓰러질 때 물리로 무너지고, 멈추면 그 자세로 굳는다
/// (docs/combat.md §배그식 사망 루팅 ⑤, 2026-09-11 사용자 "몸 대신 레그돌 넣어야 할 것 같은데").
///
/// 3D 밴딧 리그(SimpleHero_Rig)의 뼈 11개에 **런타임으로** 강체·콜라이더·관절을 붙인다 — 프리팹엔 아무것도 없다.
/// ⚠️ 뼈에는 FBX 100배 스케일이 걸려 있어(로컬 좌표가 0.001 단위) 크기는 **월드 거리로 재서 로컬로 되돌린다.**
/// 레이어 = <see cref="CorpseLayer"/>: 플레이어·산 적과는 부딪히지 않는다(걸려 넘어지거나 시체를 밀지 않게).
/// 굳으면 콜라이더를 끈다 — 총알·시야 판정에 시체 팔다리가 끼지 않게.</summary>
public sealed class BanditRagdoll : MonoBehaviour
{
    /// <summary>시체 팔다리 레이어(ProjectSettings 레이어 12 "Corpse" — tools/setup_corpse_loot.cs가 이름을 붙인다).</summary>
    public const int CorpseLayer = 12;
    /// <summary>이 안에 안 멈춰도 굳힌다(초).</summary>
    const float SettleMaxTime = 2.5f;
    const float TotalMass = 60f;

    enum Shape { Capsule, Box, Sphere }
    struct Part
    {
        public string bone, parent, child; public float mass; public Shape shape;
        public Part(string b, string p, string c, float m, Shape s) { bone = b; parent = p; child = c; mass = m; shape = s; }
    }
    // 부모가 먼저 오게 — 관절은 이미 만든 부모 강체에 붙는다.
    static readonly Part[] Parts =
    {
        new Part("Hips",       null,         "Chest",     .20f, Shape.Box),
        new Part("Chest",      "Hips",       "Neck",      .20f, Shape.Box),
        new Part("Head",       "Chest",      null,        .10f, Shape.Sphere),
        new Part("UpperArm.L", "Chest",      "Forearm.L", .05f, Shape.Capsule),
        new Part("Forearm.L",  "UpperArm.L", "Hand.L",    .04f, Shape.Capsule),
        new Part("UpperArm.R", "Chest",      "Forearm.R", .05f, Shape.Capsule),
        new Part("Forearm.R",  "UpperArm.R", "Hand.R",    .04f, Shape.Capsule),
        new Part("Thigh.L",    "Hips",       "Shin.L",    .08f, Shape.Capsule),
        new Part("Shin.L",     "Thigh.L",    "Foot.L",    .05f, Shape.Capsule),
        new Part("Thigh.R",    "Hips",       "Shin.R",    .08f, Shape.Capsule),
        new Part("Shin.R",     "Thigh.R",    "Foot.R",    .05f, Shape.Capsule),
    };

    readonly List<Rigidbody> _bodies = new List<Rigidbody>();
    readonly List<Collider> _cols = new List<Collider>();
    float _t;
    bool _frozen;

    /// <summary>엉덩이 뼈 — 몸이 밀려난 자리(표시·라벨을 몸 위에 세울 때).</summary>
    public Transform Hips { get; private set; }
    public bool IsFrozen => _frozen;

    /// <summary>래그돌을 켠다. 뼈를 못 찾으면 null(호출자가 예전 연출로 대신한다).
    /// <paramref name="push"/> = 모든 부위에 줄 초기 속도(m/s).</summary>
    public static BanditRagdoll Activate(Transform view, Vector3 push)
    {
        if (view == null) return null;
        var bones = new Dictionary<string, Transform>();
        foreach (var t in view.GetComponentsInChildren<Transform>(true))
            if (!bones.ContainsKey(t.name)) bones[t.name] = t;
        if (!bones.ContainsKey("Hips") || !bones.ContainsKey("Chest")) return null;

        // 몸 크기(월드) — 부위 두께를 여기에 비례시킨다(탱크는 1.3배 모델).
        float h = 1.6f;
        var smr = view.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null) h = Mathf.Max(0.5f, smr.bounds.size.y);

        // 적 루트 강체가 살아 있으면 자식 뼈를 끌고 다닌다 — 먼저 멈춰 둔다.
        var rootBody = view.GetComponentInParent<Rigidbody>();
        if (rootBody != null)
        {
            if (!rootBody.isKinematic) { rootBody.linearVelocity = Vector3.zero; rootBody.angularVelocity = Vector3.zero; }
            rootBody.isKinematic = true;
        }

        IgnoreLayersOnce();
        var rd = view.gameObject.AddComponent<BanditRagdoll>();
        rd.Hips = bones["Hips"];
        var made = new Dictionary<string, Rigidbody>();

        foreach (var p in Parts)
        {
            if (!bones.TryGetValue(p.bone, out var b)) continue;
            Transform child = p.child != null && bones.TryGetValue(p.child, out var c) ? c : null;
            b.gameObject.layer = CorpseLayer;
            float s = Mathf.Max(1e-5f, b.lossyScale.x);   // 월드 길이 → 이 뼈의 로컬 길이

            Collider col;
            if (p.shape == Shape.Sphere || (p.shape == Shape.Capsule && child == null))
            {
                var sc = b.gameObject.AddComponent<SphereCollider>();
                bool head = p.shape == Shape.Sphere;
                sc.radius = (head ? 0.13f : 0.05f) * h / s;
                if (head) sc.center = b.InverseTransformVector(Vector3.up * 0.10f * h);
                col = sc;
            }
            else if (p.shape == Shape.Capsule)
            {
                var cc = b.gameObject.AddComponent<CapsuleCollider>();
                Vector3 local = b.InverseTransformPoint(child.position);
                int axis = MaxAxis(local);
                cc.direction = axis;
                cc.center = local * 0.5f;
                cc.height = Mathf.Abs(local[axis]);
                cc.radius = 0.045f * h / s;
                col = cc;
            }
            else
            {
                var bc = b.gameObject.AddComponent<BoxCollider>();
                Vector3 local = child != null ? b.InverseTransformPoint(child.position)
                                              : b.InverseTransformVector(Vector3.up * 0.2f * h);
                int axis = MaxAxis(local);
                var size = Vector3.one * (0.24f * h / s);
                size[axis] = Mathf.Max(Mathf.Abs(local[axis]), 0.05f * h / s);
                bc.center = local * 0.5f;
                bc.size = size;
                col = bc;
            }

            var rb = b.gameObject.AddComponent<Rigidbody>();
            rb.mass = TotalMass * p.mass;
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.6f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            made[p.bone] = rb;
            rd._bodies.Add(rb);
            rd._cols.Add(col);

            if (p.parent != null && made.TryGetValue(p.parent, out var parentBody))
            {
                var j = b.gameObject.AddComponent<CharacterJoint>();
                j.connectedBody = parentBody;
                j.enableProjection = true;   // 관절이 늘어나 팔다리가 떨어져 보이는 것 방지
            }
        }

        // 한 몸의 부위끼리는 서로 밀지 않는다 — 겹친 채 시작하면 튕겨 폭발한다(치비 비율이라 겹침이 크다).
        for (int i = 0; i < rd._cols.Count; i++)
            for (int k = i + 1; k < rd._cols.Count; k++)
                Physics.IgnoreCollision(rd._cols[i], rd._cols[k], true);

        foreach (var rb in rd._bodies) rb.AddForce(push, ForceMode.VelocityChange);
        return rd;
    }

    static int MaxAxis(Vector3 v)
    {
        float x = Mathf.Abs(v.x), y = Mathf.Abs(v.y), z = Mathf.Abs(v.z);
        return x >= y && x >= z ? 0 : y >= z ? 1 : 2;
    }

    static bool _layersSet;
    /// <summary>시체 팔다리는 플레이어·적과 부딪히지 않는다(런타임 1회 — 충돌 매트릭스가 전부 켜져 있어서).</summary>
    static void IgnoreLayersOnce()
    {
        if (_layersSet) return;
        _layersSet = true;
        int player = LayerMask.NameToLayer("Player"), enemy = LayerMask.NameToLayer("Enemy");
        if (player >= 0) Physics.IgnoreLayerCollision(CorpseLayer, player, true);
        if (enemy >= 0) Physics.IgnoreLayerCollision(CorpseLayer, enemy, true);
    }

    void FixedUpdate()
    {
        if (_frozen) return;
        _t += Time.fixedDeltaTime;
        bool settled = _t > 0.6f;
        if (settled)
            foreach (var rb in _bodies)
                if (rb != null && !rb.IsSleeping() && rb.linearVelocity.sqrMagnitude > 0.02f) { settled = false; break; }
        if (settled || _t >= SettleMaxTime) Freeze();
    }

    /// <summary>그 자세로 굳힌다 — 강체는 멈추고 콜라이더는 끈다(총알·시야에 안 걸리게).</summary>
    void Freeze()
    {
        _frozen = true;
        foreach (var rb in _bodies)
        {
            if (rb == null) continue;
            if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            rb.isKinematic = true;
        }
        foreach (var c in _cols) if (c != null) c.enabled = false;
    }
}
