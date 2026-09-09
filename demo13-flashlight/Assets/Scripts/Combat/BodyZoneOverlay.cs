using UnityEngine;

/// <summary>
/// **테스트용** 부위 표시 — 타르코프처럼 몸을 부위별로 나눠 보여준다.
/// (2026-07-29 사용자: "몸에 부위별 나눴잖아, 타르코프처럼 그걸 표시하는 건 어때, 테스트용으로.
///  다리 노려지는지 이런 거 볼 수 있을지도")
///
/// **눈으로 확인할 수단이 없으면 부위 판정은 있는지 없는지도 모른다** — 다리를 노렸는데
/// 정말 다리가 맞았는지 로그로만 보면 감이 안 온다. 그래서 판정과 같은 기하(`BodyZones`)로
/// 그린다. 표시가 판정과 다른 식을 쓰면 "보이는 것과 맞는 것"이 어긋나 더 나쁘다.
///
/// 나중에 통째로 뺄 것이므로 **다른 코드가 이걸 참조하지 않는다**(Hurtbox가 있으면 알려줄 뿐).
/// F1 디버그에서 켜고 끈다.
/// </summary>
public class BodyZoneOverlay : MonoBehaviour
{
    /// <summary>전역 on/off — F1 디버그 토글. 기본 꺼짐(평소엔 보일 이유가 없다).</summary>
    public static bool Enabled;

    static readonly BodyPartType[] Parts =
    {
        BodyPartType.Head, BodyPartType.Torso, BodyPartType.Arms,
        BodyPartType.LeftLeg, BodyPartType.RightLeg,
    };

    Collider _body;
    UnitInjuries _inj;
    SpriteRenderer[] _cells;      // Arms는 좌우 2칸이라 총 6칸
    float[] _flash;
    BodyPartType[] _cellPart;

    /// <summary>owner(허트박스를 가진 캐릭터)에 붙인다.</summary>
    public static BodyZoneOverlay Attach(Transform owner)
    {
        if (owner == null) return null;
        var existing = owner.GetComponentInChildren<BodyZoneOverlay>(true);
        if (existing != null) return existing;
        var go = new GameObject("BodyZoneOverlay");
        go.transform.SetParent(owner, false);
        // F1 부위 오버레이는 허트박스를 화면에 그대로 겹쳐 보여 주는 것이 목적이다.
        // 쿼터뷰에서 눕혀 두면 칸이 찌그러져 "다리를 노렸는지"를 확인할 수가 없다.
        Billboard.Attach(go.transform);
        return go.AddComponent<BodyZoneOverlay>();
    }

    void Awake()
    {
        var hb = GetComponentInParent<Hurtbox>();
        _body = hb != null ? hb.GetComponent<Collider>() : GetComponentInParent<Collider>();
        _inj = GetComponentInParent<UnitInjuries>();   // 없으면(플레이어) 부상 색은 안 쓴다

        _cells = new SpriteRenderer[6];
        _flash = new float[6];
        _cellPart = new BodyPartType[6];
        for (int i = 0; i < 6; i++)
        {
            var go = new GameObject("Zone" + i);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSprite.Square;
            sr.sortingOrder = 30;                    // 몸통 위, 총알(40) 아래
            _cells[i] = sr;
            _cellPart[i] = i < 5 ? Parts[i] : BodyPartType.Arms;   // 6번째 = 반대쪽 팔
        }
    }

    /// <summary>그 부위를 잠깐 밝힌다(피격 표시).</summary>
    public void FlashPart(BodyPartType part)
    {
        for (int i = 0; i < _cells.Length; i++)
            if (_cellPart[i] == part) _flash[i] = 1f;
    }

    void LateUpdate()
    {
        bool show = Enabled && _body != null && _body.enabled;
        for (int i = 0; i < _cells.Length; i++)
        {
            if (_flash[i] > 0f) _flash[i] = Mathf.Max(0f, _flash[i] - Time.deltaTime * 2.2f);
            if (_cells[i] == null) continue;
            if (_cells[i].enabled != show) _cells[i].enabled = show;
        }
        if (!show) return;

        var b = _body.bounds;
        // 부모 스케일을 빼고 월드 기준으로 그린다 — 몸통이 눌려 있어도 구획은 몸에 맞아야 한다.
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        for (int i = 0; i < _cells.Length; i++)
        {
            var part = _cellPart[i];
            Rect r = BodyZones.ZoneRect(b, part);
            if (i == 5)   // 반대쪽 팔 — 오른쪽 띠
                r = new Rect(b.max.x - r.width, r.y, r.width, r.height);

            var t = _cells[i].transform;
            t.position   = new Vector3(r.x + r.width * 0.5f, r.y + r.height * 0.5f, 0f);
            t.localScale = new Vector3(Mathf.Max(0.01f, r.width), Mathf.Max(0.01f, r.height), 1f);
            // 부상한 부위는 짙게 — 다리를 부순 게 먹혔는지 눈으로 확인할 수 있어야 한다.
            Color c = BaseColor(part);
            int lv = _inj != null ? _inj.Level(part) : 0;
            if (lv > 0) c = new Color(c.r * 0.5f, c.g * 0.35f, c.b * 0.35f,
                                      Mathf.Min(0.75f, c.a + 0.18f * lv));
            _cells[i].color = Color.Lerp(c, Color.white, _flash[i] * 0.85f);
        }
    }

    static Color BaseColor(BodyPartType p)
    {
        switch (p)
        {
            case BodyPartType.Head:  return new Color(0.90f, 0.25f, 0.22f, 0.30f);   // 붉게 = 위험
            case BodyPartType.Torso: return new Color(0.85f, 0.70f, 0.25f, 0.24f);
            case BodyPartType.Arms:  return new Color(0.35f, 0.65f, 0.85f, 0.22f);
            default:                 return new Color(0.40f, 0.80f, 0.45f, 0.22f);   // 다리
        }
    }
}
