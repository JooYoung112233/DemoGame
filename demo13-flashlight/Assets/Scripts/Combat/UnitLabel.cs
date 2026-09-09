using UnityEngine;

/// <summary>
/// 그레이박스 식별 라벨 — 머리 위 월드 텍스트("적" → 죽으면 "시체").
/// 아트가 붙으면 통째로 제거할 임시 가시성 장치 (docs/rendering.md 2026-07-11).
///
/// 부착 규칙 두 가지가 중요하다.
/// 1) **폰트를 명시 지정한다.** 스크립트로 AddComponent한 TextMesh는 font가 null이라
///    (인스펙터로 붙일 때와 달리) 한 글자도 안 그려진다. EnemySpeechBubble과 같은 동적폰트 사용.
/// 2) **스프라이트의 자식으로 붙이되, 숨김은 수동이다.** PlayerVision(FOV)은 SpriteRenderer
///    '컴포넌트'만 끄므로 자식 MeshRenderer는 자동으로 안 꺼진다 — 안 그러면 시야 밖 적의
///    "적" 글자만 어둠 속에 떠서 위치가 그대로 노출된다. EnemyController가 SetVisible로 같이 끈다.
/// </summary>
public class UnitLabel : MonoBehaviour
{
    public static readonly Color EnemyColor  = new Color(1f, 0.85f, 0.85f);
    public static readonly Color CorpseColor = new Color(0.70f, 0.70f, 0.74f);

    const float WorldSize = 0.13f;   // 부모 스케일과 무관한 월드 기준 글자 크기
    // 2026-07-29 사용자: "적들 이름이 머리 위가 아니라 몸 중앙에 뜨게".
    //   머리 위는 HP/그로기 바(0.7·0.85)와 겹쳐 셋이 층층이 쌓여 시끄러웠다.
    //   몸 중앙이면 "저게 뭔지"와 "저게 어디 있는지"가 한 점에서 읽힌다.
    const float BodyY     = 0f;      // 몸 중앙(부모 로컬)

    TextMesh     _tm;
    MeshRenderer _mr;

    void Awake()
    {
        if (_tm == null) _tm = GetComponent<TextMesh>();
        if (_mr == null) _mr = GetComponent<MeshRenderer>();
    }

    /// <summary>parent 머리 위에 라벨 생성. 부모가 눌린 스케일이어도 글자 비율은 정상.
    ///
    /// <paramref name="localY"/>는 부모 로컬 기준 높이다. 예전 기본값 0은 "원점 = 몸 중앙"인
    /// 2D 시절 전제였는데, 3D에선 원점이 <b>발밑</b>이라 그대로 두면 이름표가 발치에 깔린다.</summary>
    public static UnitLabel Attach(Transform parent, string text, Color color, int sortingOrder, float localY = BodyY)
    {
        if (parent == null) return null;

        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        go.transform.localPosition = new Vector3(0f, localY, 0f);
        Billboard.Attach(go.transform);   // 쿼터뷰에서 눕혀 두면 글자가 찌그러져 안 읽힌다

        // 부모 스케일 보정 — 예: 몸체가 (0.8, 1.0)이어도 글자는 정사각 비율로.
        Vector3 ls = parent.lossyScale;
        go.transform.localScale = new Vector3(
            WorldSize / Mathf.Max(0.0001f, Mathf.Abs(ls.x)),
            WorldSize / Mathf.Max(0.0001f, Mathf.Abs(ls.y)), 1f);

        var tm = go.AddComponent<TextMesh>();
        tm.text          = text;
        tm.anchor        = TextAnchor.MiddleCenter;   // 몸 중앙에 두므로 글자도 중앙 정렬
        tm.alignment     = TextAlignment.Center;
        tm.fontSize      = 48;
        tm.characterSize = 1f;
        tm.fontStyle     = FontStyle.Bold;
        tm.color         = color;

        var mr = go.GetComponent<MeshRenderer>();

        // ★ 폰트 명시 — 없으면 머티리얼이 비어 아무것도 안 그려진다(한글 포함).
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null)
        {
            tm.font = f;
            if (mr != null) mr.sharedMaterial = f.material;
        }

        if (mr != null)
        {
            mr.sortingOrder      = sortingOrder;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = false;
        }

        var label = go.AddComponent<UnitLabel>();
        label._tm = tm;
        label._mr = mr;
        return label;
    }

    public void Set(string text, Color color)
    {
        if (_tm == null) _tm = GetComponent<TextMesh>();
        if (_tm == null) return;
        _tm.text  = text;
        _tm.color = color;
    }

    public void SetVisible(bool v)
    {
        if (_mr == null) _mr = GetComponent<MeshRenderer>();
        if (_mr != null) _mr.enabled = v;
    }
}
