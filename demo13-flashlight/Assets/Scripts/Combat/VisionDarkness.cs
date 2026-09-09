using UnityEngine;

/// <summary>
/// FOV 시야콘 **밖을 어둡게 덮는 오버레이**. (docs/rendering.md 가시성 / dev-roadmap "FOV 어둠 오버레이")
///
/// 왜 필요한가: `PlayerVision`이 시야 밖 적의 렌더러를 끄는데, 주변이 밝으면 **적이 그냥 사라진 것처럼**
/// 보여 버그로 읽힌다(사용자 지적). 안 비추는 영역을 실제로 어둡게 만들면 "저긴 안 보이는 구역"으로 읽힌다.
///
/// 방식: 플레이어 중심 부채꼴 메시를 매 프레임 생성해 **어두운 반투명으로 덮는다.**
///   • 콘 안(정면 ±fov/2) → 시야 사거리까지 밝음
///   • 근접 반경 안 → 각도 무관 밝음(바로 옆 기척)
///   • 그 외 → 어둡게
/// 판정 수치를 `PlayerVision`과 **동일한 GameTuning 필드**에서 읽으므로 시각과 판정이 절대 어긋나지 않는다.
///
/// URP 2D Light2D를 건드리지 않는 독립 오버레이라 기존 조명 셋업(글로벌 라이트·낮밤)과 충돌하지 않는다.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisionDarkness : MonoBehaviour
{
    public static VisionDarkness Instance { get; private set; }

    const int Segments = 96;          // 부채꼴 해상도(높을수록 경계가 매끈)
    const float OuterRadius = 60f;    // 화면을 덮고도 남는 바깥 반경

    /// <summary>부채꼴을 지면 위로 띄우는 높이(m).
    ///
    /// ⚠️ 이 오버레이는 **바닥만 덮는다** — 벽·서 있는 프롭은 어두워지지 않는다. 2D에선
    ///    화면 전체를 덮었지만 3D에서 같은 효과를 내려면 후처리가 필요하다(Stage 2 잔여).
    ///    지금은 "저 바닥은 안 보이는 구역"이라는 신호 + `PlayerVision`의 적 은폐 조합으로 읽힌다.</summary>
    const float GroundY = 0.05f;

    Mesh _mesh;
    MeshRenderer _mr;
    Vector3[] _verts;
    int[] _tris;
    Color[] _colors;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;
        if (Instance != null) return;
        if (FindFirstObjectByType<VisionDarkness>(FindObjectsInactive.Include) != null) return;

        var go = new GameObject("[VisionDarkness]", typeof(MeshFilter), typeof(MeshRenderer));
        DontDestroyOnLoad(go);
        go.AddComponent<VisionDarkness>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _mesh = new Mesh { name = "VisionDarknessMesh" };
        _mesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = _mesh;

        _mr = GetComponent<MeshRenderer>();
        _mr.sharedMaterial = BuildMaterial();
        // 3D에선 `sortingOrder`가 의미 없다 — 순서는 셰이더의 Queue/ZTest가 정한다.
        _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _mr.receiveShadows = false;

        AllocBuffers();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>Unlit 반투명 — 조명 영향을 받지 않아야 '어둠'이 흔들리지 않는다.
    ///
    /// ⚠️ 예전엔 `Sprites/Default`였다. URP-3D에선 그게 빌트인 파이프라인 셰이더라
    ///    **분홍 에러 머티리얼**이 되고, URP 기본 Unlit은 정점 색을 안 읽어 부채꼴의
    ///    밝음↔어둠 그라디언트가 통째로 사라진다. 그래서 전용 셰이더를 쓴다.</summary>
    static Material BuildMaterial()
    {
        var sh = Shader.Find("BRB/VisionDarkness");
        if (sh == null)
        {
            Debug.LogWarning("[VisionDarkness] BRB/VisionDarkness 셰이더 없음 — 어둠 오버레이가 안 보일 수 있다.");
            sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        }
        return new Material(sh) { name = "VisionDarknessMat" };
    }

    void AllocBuffers()
    {
        // 세그먼트마다 사각형(내측 2 + 외측 2) → 삼각형 2개
        _verts = new Vector3[Segments * 4];
        _colors = new Color[Segments * 4];
        _tris = new int[Segments * 6];
        for (int s = 0; s < Segments; s++)
        {
            int v = s * 4, t = s * 6;
            _tris[t + 0] = v + 0; _tris[t + 1] = v + 2; _tris[t + 2] = v + 1;
            _tris[t + 3] = v + 1; _tris[t + 4] = v + 2; _tris[t + 5] = v + 3;
        }
    }

    void LateUpdate()
    {
        var p = TopDownPlayer.Instance;
        var gt = GameTuning.Instance;

        // 시야 비활성 / 플레이어 없음 / 안전구역 → 어둠 없음
        bool on = p != null && (gt == null || gt.visionEnabled)
                  && (UIManager.Instance == null || !UIManager.Instance.IsSafehouse);
        float alpha = gt != null ? gt.visionDarkAlpha : 0.72f;
        if (!on || alpha <= 0.001f)
        {
            if (_mr.enabled) _mr.enabled = false;
            return;
        }
        if (!_mr.enabled) _mr.enabled = true;

        // ★ PlayerVision과 **같은 값**을 읽는다 — 시각과 판정이 어긋나면 그게 더 큰 버그.
        float fovDeg = gt != null ? gt.visionFovDegrees : 150f;
        float range  = gt != null ? gt.visionRange : 9f;
        float near   = gt != null ? gt.visionNearRadius : 2.2f;

        // ⚠️ 예전엔 `new Vector3(c.x, c.y, 0f)`였다 — 2D에선 맞았지만 3D에선 부채꼴이
        //    **원점 평면에 세워진 검은 벽**이 된다. 지면 바로 위에 깐다.
        Vector3 c = p.transform.position;
        transform.position = new Vector3(c.x, c.y + GroundY, c.z);

        Vector2 facing = p.FacingDirection.sqrMagnitude > 0.0001f ? p.FacingDirection.normalized : Vector2.down;
        float faceDeg = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        float half = fovDeg * 0.5f;
        float soft = Mathf.Min(12f, half * 0.35f);   // 콘 경계 부드럽게(계단 방지)

        var dark = new Color(0f, 0f, 0f, alpha);
        var clear = new Color(0f, 0f, 0f, 0f);

        for (int s = 0; s < Segments; s++)
        {
            float a0 = s * 360f / Segments;
            float a1 = (s + 1) * 360f / Segments;
            SetEdge(s * 4 + 0, s * 4 + 1, a0, faceDeg, half, soft, near, range, dark, clear);
            SetEdge(s * 4 + 2, s * 4 + 3, a1, faceDeg, half, soft, near, range, dark, clear);
        }

        _mesh.Clear();
        _mesh.vertices = _verts;
        _mesh.colors = _colors;
        _mesh.triangles = _tris;
        _mesh.RecalculateBounds();
    }

    /// <summary>한 각도의 내측(밝음 경계)·외측(완전 어둠) 정점 2개를 만든다.</summary>
    void SetEdge(int viInner, int viOuter, float angDeg, float faceDeg, float half, float soft,
                 float near, float range, Color dark, Color clear)
    {
        float diff = Mathf.Abs(Mathf.DeltaAngle(faceDeg, angDeg));

        // 콘 안이면 시야 사거리까지, 밖이면 근접 반경까지 밝다.
        float inner = diff <= half ? range : near;

        // 경계에서 급격히 끊기지 않게 half~half+soft 구간을 보간.
        if (diff > half && diff < half + soft)
            inner = Mathf.Lerp(range, near, (diff - half) / soft);

        // ⚠️ 평면 각도를 **XZ**에 편다. 2D 시절의 (cos, sin, 0)을 그대로 두면 부채꼴이
        //    수직면에 서서 바닥을 전혀 덮지 못한다. 평면 방향 (x, y)는 월드 (x, _, z)다.
        float rad = angDeg * Mathf.Deg2Rad;
        var dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));

        _verts[viInner] = dir * inner;   _colors[viInner] = clear;   // 밝은 쪽 = 투명
        _verts[viOuter] = dir * OuterRadius; _colors[viOuter] = dark; // 바깥 = 어둠
    }
}
