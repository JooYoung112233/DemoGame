using UnityEngine;

/// <summary>
/// **테스트용** 체력 바. `Health`를 가진 아무 오브젝트에나 붙는다.
/// (2026-07-29 사용자: "hp 바도 하나 추가해줘, 적이랑 플레이어, 나중에 뺄 수 있게 테스트용으로")
///
/// 나중에 통째로 빼야 하므로 **다른 코드가 이걸 참조하지 않는다** — 스스로 Health를 찾아 그리기만 한다.
/// 이 컴포넌트를 지우면 그걸로 끝이고, 게임 로직엔 아무 흔적도 남지 않는다.
///
/// 적 부위 부상(`UnitInjuries`)이 있어도 **전체 HP 바는 계속 필요하다** —
/// "어디를 다쳤나"와 "얼마나 남았나"는 다른 정보다.
/// </summary>
public class TestHealthBar : MonoBehaviour
{
    const float W = 0.86f, H = 0.09f;

    Health _health;
    Transform _fill;
    SpriteRenderer _fillSr, _bgSr;
    bool _hideWhenFull;

    /// <summary>owner 밑에 바를 만들어 붙인다. 이미 있으면 그걸 돌려준다.</summary>
    public static TestHealthBar Attach(Transform owner, float yOffset, bool hideWhenFull = false)
    {
        if (owner == null) return null;
        var existing = owner.GetComponentInChildren<TestHealthBar>(true);
        if (existing != null) return existing;

        var root = new GameObject("TestHealthBar");
        root.transform.SetParent(owner, false);
        root.transform.localPosition = new Vector3(0f, yOffset, 0f);
        Billboard.Attach(root.transform);
        var b = root.AddComponent<TestHealthBar>();
        b._hideWhenFull = hideWhenFull;
        b.Build();
        return b;
    }

    void Build()
    {
        // 부모 스케일 보정 — 몸통이 (0.6, 0.9)라도 바는 제 비율을 지킨다.
        Vector3 ls = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, Mathf.Abs(ls.x)),
                                           1f / Mathf.Max(0.0001f, Mathf.Abs(ls.y)), 1f);

        _bgSr   = MakePart("BG",   new Color(0.08f, 0.08f, 0.08f, 0.85f), 60, W, H);
        _fillSr = MakePart("Fill", Color.green, 61, W, H);
        _fill = _fillSr.transform;
    }

    SpriteRenderer MakePart(string name, Color color, int order, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(w, h, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PlaceholderSprite.Square;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    void LateUpdate()
    {
        if (_health == null) _health = GetComponentInParent<Health>();
        if (_health == null || _fill == null) return;

        float pct = Mathf.Clamp01(_health.Percent);
        bool show = !_health.IsDead && (!_hideWhenFull || pct < 0.995f);
        if (_bgSr.enabled != show) { _bgSr.enabled = show; _fillSr.enabled = show; }
        if (!show) return;

        // 왼쪽 고정으로 줄어들게 — 가운데서 줄면 얼마나 남았는지 눈으로 못 읽는다.
        _fill.localScale    = new Vector3(W * pct, H, 1f);
        _fill.localPosition = new Vector3(-W * (1f - pct) * 0.5f, 0f, 0f);
        _fillSr.color = Color.Lerp(new Color(0.85f, 0.18f, 0.15f), new Color(0.35f, 0.78f, 0.30f), pct);

        // 부모가 좌우 반전(flipX)돼도 바는 안 뒤집히게 — 부모 스케일이 음수면 되돌린다.
        Vector3 ls = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        float sx = 1f / Mathf.Max(0.0001f, Mathf.Abs(ls.x));
        transform.localScale = new Vector3(ls.x < 0f ? -sx : sx,
                                           1f / Mathf.Max(0.0001f, Mathf.Abs(ls.y)), 1f);
    }
}
