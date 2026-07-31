using UnityEngine;

/// <summary>
/// 그레이박스 몸 — **머리·몸통·팔·다리가 실루엣으로 나뉜다.**
/// (2026-07-29 사용자: "적 팔다리 구분도 안 돼, 그냥 사각형이랑 좀 분리 좀")
///
/// 통짜 사각형이면 부위 조준이 **어디를 노리는 건지 알 수가 없다** — 판정은 위/아래/좌우로
/// 나뉘어 있는데 눈에는 네모 하나뿐이라, 다리를 노렸는지 몸통을 노렸는지 감이 안 온다.
///
/// 칸 비율은 **`BodyZones`와 같은 상수**에서 나온다. 표시가 판정과 다른 식을 쓰면
/// "보이는 것과 맞는 것"이 어긋나 없느니만 못하다(오버레이와 같은 원칙).
///
/// 1×1 로컬 박스 안에 그리므로, 부모(몸통 트랜스폼)의 스케일이 그대로 먹는다 —
/// `ApplyUnitLook`이 적 크기를 바꿔도 팔다리 비율이 안 깨진다.
/// </summary>
public class GreyboxLimbs : MonoBehaviour
{
    SpriteRenderer[] _parts;

    /// <summary>owner(몸통 스프라이트 트랜스폼) 밑에 팔다리를 만든다. 이미 있으면 그걸 돌려준다.</summary>
    public static GreyboxLimbs Attach(Transform owner, Color body, int sortingOrder)
    {
        if (owner == null) return null;
        var existing = owner.GetComponentInChildren<GreyboxLimbs>(true);
        if (existing != null) return existing;

        var root = new GameObject("Limbs");
        root.transform.SetParent(owner, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localScale = Vector3.one;
        var g = root.AddComponent<GreyboxLimbs>();
        g.Build(body, sortingOrder);
        return g;
    }

    void Build(Color body, int order)
    {
        // BodyZones의 세로 구획과 같은 값 — 여기서 다른 숫자를 쓰면 표시가 거짓말을 한다.
        const float headBottom = 0.78f, legTop = 0.34f, armEdge = 0.32f;

        Color head  = Mul(body, 1.18f);   // 머리는 밝게 — 제일 먼저 눈에 들어와야 한다
        Color torso = body;
        Color limb  = Mul(body, 0.78f);   // 팔다리는 어둡게 — 구획이 눈에 나뉜다

        _parts = new SpriteRenderer[6];
        // 로컬 1×1 박스: x -0.5~0.5, y -0.5~0.5 (y01 0=발끝, 1=정수리)
        _parts[0] = Cell("Head",  head,  order + 2, 0f,            Mid(headBottom, 1f),     0.44f, 1f - headBottom);
        _parts[1] = Cell("Torso", torso, order + 1, 0f,            Mid(legTop, headBottom), armEdge * 2f, headBottom - legTop);
        _parts[2] = Cell("ArmL",  limb,  order,     -(0.25f + armEdge * 0.5f), Mid(legTop, headBottom), 0.5f - armEdge, headBottom - legTop);
        _parts[3] = Cell("ArmR",  limb,  order,      (0.25f + armEdge * 0.5f), Mid(legTop, headBottom), 0.5f - armEdge, headBottom - legTop);
        _parts[4] = Cell("LegL",  limb,  order,     -0.24f,        Mid(0f, legTop),         0.44f, legTop);
        _parts[5] = Cell("LegR",  limb,  order,      0.24f,        Mid(0f, legTop),         0.44f, legTop);
    }

    static float Mid(float a, float b) => (a + b) * 0.5f - 0.5f;   // y01 구간의 중앙 → 로컬 y
    static Color Mul(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);

    SpriteRenderer Cell(string name, Color c, int order, float x, float y, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(x, y, 0f);
        go.transform.localScale = new Vector3(Mathf.Max(0.02f, w * 0.94f), Mathf.Max(0.02f, h * 0.92f), 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PlaceholderSprite.Square;
        sr.color = c;
        sr.sortingOrder = order;
        return sr;
    }

    /// <summary>몸 전체 색을 갈아 준다(피격 플래시·시체 어둡게 등).</summary>
    public void SetTint(Color body)
    {
        if (_parts == null) return;
        _parts[0].color = Mul(body, 1.18f);
        _parts[1].color = body;
        for (int i = 2; i < 6; i++) _parts[i].color = Mul(body, 0.78f);
    }

    public void SetVisible(bool v)
    {
        if (_parts == null) return;
        for (int i = 0; i < _parts.Length; i++) if (_parts[i] != null) _parts[i].enabled = v;
    }
}
