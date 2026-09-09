using UnityEngine;

/// <summary>
/// 그레이박스 근접무기 비주얼 — **칼이 실제로 휘둘러진다.**
/// (2026-07-11 사용자: "때릴 때 표기가 없는데 칼 스프라이트 하나 만들어서 휘두르기 가능할까?
///  콤보는 각도를 살짝씩 바꿔 3연타 느낌, 강공은 기 모을 때 대각에 뒀다가 놓으면 빠르게, 테일 이펙트")
///
/// 스파인 애니메이션이 들어오면 통째로 교체할 임시 연출이다. 그래서 **판정에는 일절 관여하지 않는다** —
/// 히트박스는 `AttackPerformer`가 그대로 담당하고, 이 컴포넌트는 보여주기만 한다.
/// (연출이 판정을 건드리기 시작하면 "보이는 것과 맞는 것"이 어긋나 디버깅이 지옥이 된다.)
///
/// 구조: owner ─ Pivot(회전) ─ Blade(스프라이트) + Tip(TrailRenderer)
///   Pivot의 Z회전 = 바라보는 각 + 스윙 오프셋. 칼날은 Pivot의 +X 방향으로 뻗는다.
/// </summary>
public class MeleeWeaponVisual : MonoBehaviour
{
    // ── 스윙 프리셋 (바라보는 방향 기준 상대 각도, +가 좌측/CCW) ──
    //   2026-07-11 사용자 결정: **모든 스윙은 우측 → 좌측 한 방향**으로 통일.
    //   콤보별 궤적 변화는 뺐다("콤보는 일단 빼줘") — 궤적이 매번 달라 타이밍이 안 읽혔다.
    const float SwingFrom    = -72f;    // 약공 시작 — 오른쪽
    const float SwingTo      =  72f;    // 약공 끝 — 왼쪽
    const float IdleAngle    = -38f;    // 평상시 — 오른쪽에 내려둠
    const float ChargeAngle  = -122f;   // 강공 차징 — 오른쪽 **뒤로 더 당겨** 힘을 모으는 자세
    const float HeavyToAngle =   82f;   // 강공 릴리스 끝각 — 좌측으로 크게 뿌린다

    [SerializeField] float bladeLength = 1.05f;
    [SerializeField] float bladeWidth  = 0.12f;
    [SerializeField] float gripOffset  = 0.30f;   // 몸통에서 손잡이까지

    Transform      _pivot;
    /// <summary>칼을 쥔 손 높이(m). <see cref="GreyboxLimbs.Height"/> 몸의 허리~가슴 사이.</summary>
    const float HandHeight = 1.05f;

    MeshRenderer _blade, _guard;
    TrailRenderer  _trail;

    float _facingDeg;
    float _from, _to, _dur, _t = 1f;   // _t >= 1 = 스윙 끝
    bool  _charging;
    float _chargePct;
    float _restAngle = IdleAngle;

    public bool IsSwinging => _t < 1f;

    /// <summary>owner 밑에 칼을 만들어 붙인다. 이미 있으면 그걸 돌려준다.</summary>
    public static MeleeWeaponVisual Attach(Transform owner, Color bladeColor,
                                           float length = 1.05f)
    {
        if (owner == null) return null;
        var existing = owner.GetComponentInChildren<MeleeWeaponVisual>(true);
        if (existing != null) return existing;

        var root = new GameObject("Weapon");
        root.transform.SetParent(owner, false);
        root.transform.localPosition = Vector3.zero;
        var w = root.AddComponent<MeleeWeaponVisual>();
        w.bladeLength = length;
        w.Build(bladeColor);
        return w;
    }

    void Build(Color bladeColor)
    {
        _pivot = new GameObject("Pivot").transform;
        _pivot.SetParent(transform, false);
        // 손 높이. 예전엔 0(=발밑)이었다 — 2D에선 몸이 원점 중심이라 맞았지만
        // 3D에서 몸이 발밑 기준으로 서면서 칼이 바닥을 긁게 됐다.
        _pivot.localPosition = new Vector3(0f, HandHeight, 0f);

        // 손잡이/가드(어두운 짧은 막대) — 칼끝 방향이 한눈에 읽히게.
        _guard = MakePart("Guard", new Color(0.25f, 0.22f, 0.20f),
                          gripOffset * 0.5f, 0.22f, bladeWidth * 2.2f);
        // 칼날
        _blade = MakePart("Blade", bladeColor,
                          gripOffset + bladeLength * 0.5f, bladeLength, bladeWidth);

        // 칼끝 궤적(테일)
        var tip = new GameObject("Tip");
        tip.transform.SetParent(_pivot, false);
        tip.transform.localPosition = new Vector3(gripOffset + bladeLength, 0f, 0f);
        _trail = tip.AddComponent<TrailRenderer>();
        _trail.time = 0.16f;
        _trail.widthCurve = AnimationCurve.Linear(0f, bladeWidth * 1.5f, 1f, 0f);
        _trail.minVertexDistance = 0.02f;
        _trail.numCapVertices = 2;
        _trail.autodestruct = false;
        _trail.emitting = false;
        // 궤적은 깊이로 정렬된다 — 2D 시절의 sortingOrder 배선은 제거했다(3D에선 의미 없다).
        // ⚠️ `Sprites/Default`는 빌트인 파이프라인 셰이더다. URP-3D로 넘어온 뒤에도 그걸 쓰면
        //    칼 궤적이 **분홍색 에러 머티리얼**로 나올 수 있다. URP 파티클 언릿을 먼저 찾는다
        //    (버텍스 컬러를 받아야 그라디언트 페이드가 산다).
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
              ?? Shader.Find("Universal Render Pipeline/Unlit")
              ?? Shader.Find("Sprites/Default");
        if (sh != null) _trail.material = new Material(sh);
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(bladeColor, 1f) },
            new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
        _trail.colorGradient = grad;

        Apply(IdleAngle);
    }

    /// <summary>칼 조각 하나(3D 상자). 두께는 y·z 양쪽에 준다 — 두께 0인 판은
    /// 쿼터뷰에서 각도에 따라 선으로 사라진다.</summary>
    MeshRenderer MakePart(string name, Color color, float offX, float len, float thick)
        => GreyboxMesh.Box(_pivot, name, new Vector3(offX, 0f, 0f),
                           new Vector3(len, thick, thick), color, castShadow: false);

    /// <summary>표시 on/off — FOV 시야콘 밖에서 **칼만 어둠에 떠 있는** 것을 막는다.
    /// (칼은 몸통 스프라이트의 자식이 아니라 루트의 자식이라, 몸통을 꺼도 자동으로 안 꺼진다.)</summary>
    public void SetVisible(bool v)
    {
        if (_blade != null) _blade.enabled = v;
        if (_guard != null) _guard.enabled = v;
        if (_trail != null) { _trail.enabled = v; if (!v) _trail.Clear(); }
    }

    /// <summary>매 프레임 소유자가 바라보는 방향을 알려준다.</summary>
    public void SetFacing(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        // ⚠️ 평면 방향(x=월드X, y=월드Z)을 **Unity yaw**로 옮긴다. yaw θ는 로컬 +X를
        //    월드 (cos θ, 0, −sin θ)로 보내므로, 칼이 dir을 향하려면 θ = atan2(−dir.y, dir.x)다.
        //    2D의 atan2(dir.y, dir.x)를 그대로 쓰면 칼이 좌우로 뒤집혀 나간다.
        _facingDeg = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg;
    }

    /// <summary>약공 스윙 — 우측에서 좌측으로.</summary>
    public void Swing(float duration)
        => StartSwing(SwingFrom, SwingTo, Mathf.Max(0.08f, duration * 0.75f));

    /// <summary>강공 릴리스 — 뒤로 당겨 뒀던 자세에서 **좌측으로 빠르게** 뿌린다.
    /// 릴리스가 약공보다 짧다 = 같은 거리를 더 빨리 지난다 = 눈에 '빠르게' 읽힌다.</summary>
    public void SwingHeavy(float duration, bool full)
    {
        _charging = false;
        StartSwing(ChargeAngle, HeavyToAngle + (full ? 22f : 0f), Mathf.Max(0.07f, duration * 0.5f));
    }

    /// <summary>강공 차징 — 오른쪽 뒤로 당겨 들고, 꽉 찰수록 미세하게 떤다.</summary>
    public void Charge(float pct)
    {
        _charging = true;
        _chargePct = Mathf.Clamp01(pct);
        _t = 1f;   // 스윙 중단
    }

    /// <summary>평상시 자세로.</summary>
    public void Rest()
    {
        _charging = false;
        _restAngle = IdleAngle;
    }

    void StartSwing(float from, float to, float dur)
    {
        _charging = false;
        _from = from; _to = to; _dur = dur; _t = 0f;
        _restAngle = to;                      // 벤 자세로 잠깐 남았다가 Rest()로 돌아온다
        if (_trail != null) { _trail.Clear(); _trail.emitting = true; }
    }

    void LateUpdate()
    {
        float offset;

        if (_t < 1f)
        {
            _t += Time.deltaTime / Mathf.Max(0.01f, _dur);
            // ease-out cubic — 시작이 가장 빠르다(칼이 '터지듯' 나가는 느낌).
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(_t), 3f);
            offset = Mathf.LerpAngle(_from, _to, e);
            if (_t >= 1f && _trail != null) _trail.emitting = false;
        }
        else if (_charging)
        {
            // 꽉 찰수록 떨림 폭 증가 — "곧 터진다"가 눈에 보이게.
            float tremble = Mathf.Sin(Time.time * 34f) * (1.2f + _chargePct * 3.5f);
            offset = ChargeAngle + tremble;
        }
        else
        {
            offset = Mathf.LerpAngle(_restAngle, IdleAngle, Time.deltaTime * 9f);
            _restAngle = offset;
        }

        Apply(offset);
    }

    void Apply(float offsetDeg)
    {
        if (_pivot == null) return;
        // ⚠️ 2D에선 Z축 회전이었다. 3D 쿼터뷰에서 스윙은 **XZ 평면**을 도는 것이라 Y축이다.
        //    Z축으로 두면 칼이 수직면에서 돌아 위아래로 까딱거리기만 한다.
        _pivot.rotation = Quaternion.Euler(0f, _facingDeg + offsetDeg, 0f);
    }
}
