using UnityEngine;

/// <summary>
/// 그레이박스 맨손 비주얼 — **장갑 낀 주먹 둘, 때리면 앞으로 정권찌르기.**
/// (2026-07-29 사용자: "맨손이면 맨손 표시로 약간 장갑처럼 보여주고, 때릴 때 앞으로 정권찌르기 같이")
///
/// `MeleeWeaponVisual`·`GunVisual`과 같은 규약 — 스파인이 들어오면 교체할 임시 연출이라
/// **판정에는 일절 관여하지 않는다.** 히트박스는 `AttackPerformer`가 그대로 담당한다.
///
/// 구조: owner ─ Pivot(바라보는 각) ─ HandL / HandR
///   두 손은 Pivot의 +X(전방) 기준 좌우로 벌려 두고, 지르는 손만 +X로 밀어낸다.
///   주먹은 **번갈아** 나간다 — 같은 손만 나가면 원투가 아니라 한 손 반복으로 보인다.
/// </summary>
public class FistVisual : MonoBehaviour
{
    const float RestX     = 0.26f;   // 평상시 몸 앞
    const float RestSide  = 0.20f;   // 좌우로 벌린 폭
    const float ReachX    = 0.86f;   // 정권이 뻗는 끝
    const float WindBackX = 0.08f;   // 지르기 전 살짝 당김(강공만)

    Transform _pivot;
    SpriteRenderer _l, _r;

    float _facingDeg;
    bool  _rightTurn = true;         // 이번에 나갈 손
    float _t = 1f, _dur = 0.18f;     // _t >= 1 = 지르기 끝
    float _reach = ReachX;
    float _wind;

    public bool IsPunching => _t < 1f;

    /// <summary>owner 밑에 주먹을 만들어 붙인다. 이미 있으면 그걸 돌려준다.</summary>
    public static FistVisual Attach(Transform owner, Color glove, int sortingOrder)
    {
        if (owner == null) return null;
        var existing = owner.GetComponentInChildren<FistVisual>(true);
        if (existing != null) return existing;

        var root = new GameObject("FistVisual");
        root.transform.SetParent(owner, false);
        root.transform.localPosition = Vector3.zero;
        var f = root.AddComponent<FistVisual>();
        f.Build(glove, sortingOrder);
        return f;
    }

    void Build(Color glove, int order)
    {
        _pivot = new GameObject("Pivot").transform;
        _pivot.SetParent(transform, false);
        _l = MakeHand("HandL", glove, order,  RestSide);
        _r = MakeHand("HandR", glove, order,  -RestSide);
        Apply();
    }

    SpriteRenderer MakeHand(string name, Color color, int order, float side)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_pivot, false);
        go.transform.localPosition = new Vector3(RestX, side, 0f);
        go.transform.localScale    = new Vector3(0.19f, 0.17f, 1f);   // 주먹 = 작고 도톰한 사각
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PlaceholderSprite.Square;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    /// <summary>표시 on/off — 시야콘 밖에서 **주먹만 어둠에 떠 있는** 것을 막는다.</summary>
    public void SetVisible(bool v)
    {
        if (_l != null) _l.enabled = v;
        if (_r != null) _r.enabled = v;
    }

    public void SetFacing(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        _facingDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    /// <summary>약공 — 정권찌르기 1회(손 번갈아).</summary>
    public void Punch(float duration) => Start(duration, ReachX, 0f);

    /// <summary>강공 — 더 깊게 지르고, 지르기 전에 살짝 당긴다.</summary>
    public void PunchHeavy(float duration, bool full)
        => Start(duration, ReachX + (full ? 0.20f : 0.10f), WindBackX);

    void Start(float duration, float reach, float wind)
    {
        _dur = Mathf.Max(0.08f, duration * 0.8f);
        _reach = reach;
        _wind = wind;
        _t = 0f;
        _rightTurn = !_rightTurn;
    }

    /// <summary>지르기 끝 — 두 손 모두 평상 자세로.</summary>
    public void Rest() { _t = 1f; }

    void LateUpdate()
    {
        if (_t < 1f) _t = Mathf.Min(1f, _t + Time.deltaTime / _dur);
        Apply();
    }

    void Apply()
    {
        if (_pivot == null) return;
        _pivot.localRotation = Quaternion.Euler(0f, 0f, _facingDeg);

        // 0→0.35 뻗고 0.35→1 되돌아온다. 뻗는 쪽이 빨라야 '지른다'로 읽힌다.
        float x = RestX;
        if (_t < 1f)
        {
            const float outAt = 0.35f;
            if (_t < outAt)
            {
                float k = _t / outAt;
                float e = 1f - (1f - k) * (1f - k);                  // ease-out — 초반이 빠르다
                x = Mathf.Lerp(RestX - _wind, _reach, e);
            }
            else
            {
                float k = (_t - outAt) / (1f - outAt);
                x = Mathf.Lerp(_reach, RestX, k * k);                // ease-in — 천천히 회수
            }
        }

        // 지르는 손만 나간다. 반대 손은 살짝 뒤로 빠져 몸을 튼 것처럼 보이게.
        float other = RestX - (x - RestX) * 0.18f;
        if (_rightTurn)
        {
            SetHand(_r, x,     -RestSide * (1f - 0.55f * Mathf.InverseLerp(RestX, _reach, x)));
            SetHand(_l, other,  RestSide);
        }
        else
        {
            SetHand(_l, x,      RestSide * (1f - 0.55f * Mathf.InverseLerp(RestX, _reach, x)));
            SetHand(_r, other, -RestSide);
        }
    }

    void SetHand(SpriteRenderer sr, float x, float side)
    {
        if (sr == null) return;
        sr.transform.localPosition = new Vector3(x, side, 0f);
    }
}
