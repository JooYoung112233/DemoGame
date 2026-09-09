using UnityEngine;

/// <summary>
/// 그레이박스 몸 — **머리·몸통·팔·다리가 실루엣으로 나뉜다.**
/// (2026-07-29 사용자: "적 팔다리 구분도 안 돼, 그냥 사각형이랑 좀 분리 좀")
///
/// 통짜 사각형이면 부위 조준이 **어디를 노리는 건지 알 수가 없다** — 판정은 위/아래로
/// 나뉘어 있는데 눈에는 네모 하나뿐이라, 다리를 노렸는지 몸통을 노렸는지 감이 안 온다.
///
/// 칸 비율은 **<see cref="BodyZones"/>와 같은 상수**에서 나온다. 표시가 판정과 다른 식을 쓰면
/// "보이는 것과 맞는 것"이 어긋나 없느니만 못하다(오버레이와 같은 원칙).
///
/// <b>2026-09-09 3D화.</b> 스프라이트 6장 → 상자 6개. 같이 고친 것이 하나 더 있다:
/// 예전 몸은 <b>원점을 중심으로</b> 그려져(y −0.5~+0.5) 키가 1m였고 <b>절반이 바닥에 묻혀</b>
/// 있었다. 정작 콜라이더는 발밑부터 1.8m 캡슐이라 판정과 표시가 따로 놀았다.
/// 지금은 y01 0=발끝 / 1=정수리를 그대로 <see cref="Height"/>에 걸어 **땅을 딛고 선다.**
///
/// 설계: docs/3d-migration.md Stage 4
/// </summary>
public class GreyboxLimbs : MonoBehaviour
{
    /// <summary>기본 키(m). 적 캡슐 콜라이더(height 1.8)와 맞춘 값 — 여기가 어긋나면
    /// 다시 "맞는 자리와 보이는 자리가 다른" 상태로 돌아간다.</summary>
    public const float Height = 1.8f;

    /// <summary>부위별 두께(로컬). 쿼터뷰에선 옆·앞 두께가 있어야 사람으로 읽힌다 —
    /// 두께 0인 판은 각도에 따라 선으로 사라진다.</summary>
    const float HeadDepth = 0.42f, TorsoDepth = 0.44f, ArmDepth = 0.24f, LegDepth = 0.28f;

    MeshRenderer[] _parts;
    Color _body = Color.white;

    /// <summary>어깨 폭(m). 로컬 x/z 1.0이 이 값이 된다.</summary>
    public const float Width = 0.62f;

    /// <summary>owner(보통 캐릭터 루트) 밑에 몸을 만든다. 이미 있으면 그걸 돌려준다.
    ///
    /// ⚠️ <b>루트에 붙인다.</b> 예전엔 몸통 스프라이트 앵커 밑에 붙였는데, 그 앵커는
    ///    x만 눌린 비균등 스케일(0.8, 1, 1)을 들고 있어서 3D로 오자마자 <b>폭보다 두꺼운</b>
    ///    몸이 나왔다. 여기서 균등하게 잡으면 그 부류의 문제가 통째로 없어진다.
    ///    크기 조절은 <see cref="SetScale"/>로 명시적으로 받는다.</summary>
    public static GreyboxLimbs Attach(Transform owner, Color body, int _unusedSortingOrder = 0)
    {
        if (owner == null) return null;
        var existing = owner.GetComponentInChildren<GreyboxLimbs>(true);
        if (existing != null) return existing;

        var root = new GameObject("Limbs");
        root.transform.SetParent(owner, false);
        root.transform.localPosition = Vector3.zero;   // 발끝 = 캐릭터 원점 = 지면
        root.transform.localScale = new Vector3(Width, Height, Width);
        var g = root.AddComponent<GreyboxLimbs>();
        g.Build(body);
        return g;
    }

    /// <summary>덩치 배율(1 = 기본). <c>UnitStatData.scale</c>을 몸에만 먹인다 —
    /// 루트에 곱하면 콜라이더까지 커져 물리가 통째로 바뀐다.</summary>
    public void SetScale(float mul)
    {
        mul = Mathf.Clamp(mul, 0.3f, 4f);
        transform.localScale = new Vector3(Width * mul, Height * mul, Width * mul);
    }

    void Build(Color body)
    {
        _body = body;

        // BodyZones의 세로 구획과 같은 값 — 여기서 다른 숫자를 쓰면 표시가 거짓말을 한다.
        const float headBottom = 0.78f, legTop = 0.34f, armEdge = 0.32f;

        _parts = new MeshRenderer[6];
        // 로컬 상자: x −0.5~0.5(폭), **y 0~1(0=발끝, 1=정수리)**, z −0.5~0.5(두께)
        _parts[0] = Cell("Head",  HeadColor(body),  0f,                        Mid(headBottom, 1f),     0.44f,        1f - headBottom, HeadDepth);
        _parts[1] = Cell("Torso", body,             0f,                        Mid(legTop, headBottom), armEdge * 2f, headBottom - legTop, TorsoDepth);
        _parts[2] = Cell("ArmL",  LimbColor(body), -(0.25f + armEdge * 0.5f),  Mid(legTop, headBottom), 0.5f - armEdge, headBottom - legTop, ArmDepth);
        _parts[3] = Cell("ArmR",  LimbColor(body),  (0.25f + armEdge * 0.5f),  Mid(legTop, headBottom), 0.5f - armEdge, headBottom - legTop, ArmDepth);
        _parts[4] = Cell("LegL",  LimbColor(body), -0.24f,                     Mid(0f, legTop),         0.44f,        legTop,          LegDepth);
        _parts[5] = Cell("LegR",  LimbColor(body),  0.24f,                     Mid(0f, legTop),         0.44f,        legTop,          LegDepth);
    }

    /// <summary>y01 구간의 중앙. 예전엔 −0.5를 빼 원점 기준으로 만들었는데, 그게 몸을
    /// 바닥에 묻던 원인이었다. 지금은 0=발끝 그대로 쓴다.</summary>
    static float Mid(float a, float b) => (a + b) * 0.5f;

    static Color HeadColor(Color c) => Mul(c, 1.18f);   // 머리는 밝게 — 제일 먼저 눈에 들어와야 한다
    static Color LimbColor(Color c) => Mul(c, 0.78f);   // 팔다리는 어둡게 — 구획이 눈에 나뉜다
    static Color Mul(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);

    MeshRenderer Cell(string name, Color c, float x, float y, float w, float h, float d)
        => GreyboxMesh.Box(transform, name, new Vector3(x, y, 0f),
                           new Vector3(Mathf.Max(0.02f, w * 0.94f), Mathf.Max(0.02f, h * 0.92f), d), c);

    /// <summary>몸 전체 색을 갈아 준다(피격 플래시·시체 어둡게 등).</summary>
    public void SetTint(Color body)
    {
        if (_parts == null) return;
        _body = body;
        GreyboxMesh.Tint(_parts[0], HeadColor(body));
        GreyboxMesh.Tint(_parts[1], body);
        for (int i = 2; i < 6; i++) GreyboxMesh.Tint(_parts[i], LimbColor(body));
    }

    /// <summary>현재 몸 색(피격 플래시가 원래 색으로 되돌릴 때 쓴다).</summary>
    public Color BodyColor => _body;

    /// <summary>몸을 진행 방향으로 돌린다(평면 방향, x=월드 X / y=월드 Z).
    ///
    /// 2D에선 <c>spriteRenderer.flipX</c> 하나로 좌우만 뒤집으면 됐지만, 3D 쿼터뷰에선
    /// 앞뒤도 보이므로 **회전**이어야 한다. 돌리는 것은 <b>몸통뿐</b>이다 —
    /// 라벨·HP바·말풍선은 앵커 밑에 있어서 같이 돌면 글자가 뒤집혀 읽힌다.</summary>
    public void SetFacing(Vector2 planDir)
    {
        if (planDir.sqrMagnitude < 0.0001f) return;
        // 로컬 회전이다. 부모(앵커)가 회전하지 않는다는 전제이며, 지금 구조가 그렇다.
        transform.localRotation = Quaternion.Euler(0f, Mathf.Atan2(planDir.x, planDir.y) * Mathf.Rad2Deg, 0f);
    }

    public void SetVisible(bool v)
    {
        if (_parts == null) return;
        for (int i = 0; i < _parts.Length; i++) if (_parts[i] != null) _parts[i].enabled = v;
    }
}
