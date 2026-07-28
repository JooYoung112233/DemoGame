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
    // ── 스윙 프리셋(바라보는 방향 기준 상대 각도) ──
    //   콤보 3연타가 서로 다른 궤적을 그리도록: 위→아래 / 아래→위 / 크게 횡베기.
    static readonly float[] ComboFrom = {  78f, -72f,  105f };
    static readonly float[] ComboTo   = { -42f,  55f, -105f };
    const float IdleAngle    = -38f;    // 평상시 — 몸 옆에 내려둠
    const float ChargeAngle  = 128f;    // 강공 차징 — 뒤 대각으로 치켜듦
    const float HeavyToAngle = -88f;    // 강공 릴리스 끝각(크고 빠른 호)

    [SerializeField] float bladeLength = 1.05f;
    [SerializeField] float bladeWidth  = 0.12f;
    [SerializeField] float gripOffset  = 0.30f;   // 몸통에서 손잡이까지

    Transform      _pivot;
    SpriteRenderer _blade, _guard;
    TrailRenderer  _trail;

    float _facingDeg;
    float _from, _to, _dur, _t = 1f;   // _t >= 1 = 스윙 끝
    bool  _charging;
    float _chargePct;
    float _restAngle = IdleAngle;

    public bool IsSwinging => _t < 1f;

    /// <summary>owner 밑에 칼을 만들어 붙인다. 이미 있으면 그걸 돌려준다.</summary>
    public static MeleeWeaponVisual Attach(Transform owner, Color bladeColor, int sortingOrder,
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
        w.Build(bladeColor, sortingOrder);
        return w;
    }

    void Build(Color bladeColor, int sortingOrder)
    {
        _pivot = new GameObject("Pivot").transform;
        _pivot.SetParent(transform, false);

        // 손잡이/가드(어두운 짧은 막대) — 칼끝 방향이 한눈에 읽히게.
        _guard = MakePart("Guard", new Color(0.25f, 0.22f, 0.20f), sortingOrder,
                          gripOffset * 0.5f, 0.22f, bladeWidth * 2.2f);
        // 칼날
        _blade = MakePart("Blade", bladeColor, sortingOrder + 1,
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
        _trail.sortingOrder = sortingOrder;   // 칼날 아래로 깔린다
        var sh = Shader.Find("Sprites/Default");
        if (sh != null) _trail.material = new Material(sh);
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(bladeColor, 1f) },
            new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
        _trail.colorGradient = grad;

        Apply(IdleAngle);
    }

    SpriteRenderer MakePart(string name, Color color, int order, float offX, float len, float thick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_pivot, false);
        go.transform.localPosition = new Vector3(offX, 0f, 0f);
        go.transform.localScale    = new Vector3(len, thick, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PlaceholderSprite.Square;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

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
        _facingDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    /// <summary>약공 콤보 스윙. step 0/1/2 마다 궤적이 다르다.</summary>
    public void Swing(int step, float duration)
    {
        int i = Mathf.Clamp(step, 0, ComboFrom.Length - 1);
        StartSwing(ComboFrom[i], ComboTo[i], Mathf.Max(0.08f, duration * 0.75f));
    }

    /// <summary>강공 릴리스 — 차징하던 대각에서 크고 빠르게 벤다.</summary>
    public void SwingHeavy(float duration, bool full)
    {
        _charging = false;
        StartSwing(ChargeAngle, HeavyToAngle - (full ? 20f : 0f), Mathf.Max(0.08f, duration * 0.6f));
    }

    /// <summary>강공 차징 — 뒤 대각으로 치켜들고, 꽉 찰수록 미세하게 떤다.</summary>
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
        _pivot.rotation = Quaternion.Euler(0f, 0f, _facingDeg + offsetDeg);
    }
}
