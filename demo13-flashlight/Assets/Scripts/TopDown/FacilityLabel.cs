using UnityEngine;

/// <summary>
/// 소품 머리 위 역할 라벨 — "침대", "작업대" 처럼 **그 소품이 무엇인지**를 항상 띄운다.
///
/// 은신처는 걸어다니지 않는 클릭 화면이라 다가가서 E를 누르는 프롬프트가 성립하지 않는다.
/// 무엇을 누를 수 있는지가 한눈에 보여야 하므로 라벨을 상시 표시한다.
///
/// 한글은 프로젝트 표준 `LegacyRuntime.ttf`(동적 폰트) + <see cref="TextMesh"/>를 쓴다.
/// 쿼터뷰라 라벨은 **카메라를 향해 세운다**(빌보드) — 안 그러면 바닥에 누워 안 읽힌다.
/// 설계: docs/hideout-3d.md
/// </summary>
public class FacilityLabel : MonoBehaviour
{
    [Tooltip("표시할 이름. 비우면 HideoutFacilityAnchor.moduleKey에서 유추.")]
    public string label;

    [Tooltip("소품 위 높이(m). 소품 높이 위에 더해진다.")]
    public float heightOffset = 0.30f;

    [Tooltip("글자 크기(월드 단위).")]
    public float worldSize = 0.035f;   // 월드 높이 ≈ fontSize(64) × 이 값 × 0.1 ≈ 0.22m

    public Color color = new Color(0.94f, 0.92f, 0.86f);

    Transform _label;
    Camera _cam;

    void Start()
    {
        if (string.IsNullOrEmpty(label))
        {
            var a = GetComponent<HideoutFacilityAnchor>();
            label = a != null ? KoreanFor(a.moduleKey) : name;
        }
        if (string.IsNullOrEmpty(label)) { enabled = false; return; }

        // 소품 윗면 높이를 렌더러 바운즈에서 구한다 — 소품마다 높이가 달라서.
        float top = 1f;
        var r = GetComponentInChildren<Renderer>();
        if (r != null) top = r.bounds.max.y - transform.position.y;

        var go = new GameObject("Label");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, top + heightOffset, 0f);
        _label = go.transform;

        var tm = go.AddComponent<TextMesh>();
        tm.text = label;
        tm.fontSize = 64;
        tm.characterSize = worldSize;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null) { tm.font = font; go.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
        go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.GetComponent<MeshRenderer>().receiveShadows = false;
    }

    void LateUpdate()
    {
        if (_label == null) return;
        if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }
        // 카메라를 정면으로 바라보게 — 쿼터뷰에서 바닥에 눕지 않도록.
        _label.rotation = _cam.transform.rotation;
    }

    /// <summary>moduleKey → 한글 이름. 2D판 시설 이름과 맞춘다.</summary>
    public static string KoreanFor(string key) => key switch
    {
        "bed"       => "침대",
        "workbench" => "작업대",
        "stash"     => "창고",
        "radio"     => "라디오",
        "cooking"   => "조리대",
        "medical"   => "의료대",
        "dispatch"  => "",          // 폐기된 파견 시스템: 배경 프랍만 유지
        "generator" => "발전기",
        "idle"      => "",          // 대기 의자는 라벨 없음
        _           => key,
    };
}
