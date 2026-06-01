using UnityEngine;

/// <summary>
/// 순수 2D(정사영) 탑다운 모듈에 방향성 투영 그림자를 붙인다. 월드 = XY 평면.
/// 모듈 GameObject(MeshFilter+MeshRenderer 쿼드)에 추가하면
/// 자식으로 그림자 메시를 생성하고, 라이트 위치에 따라 빛 반대쪽(XY)으로 늘려 그린다.
/// 라이트가 사방 어디에 있어도 매 프레임 방향이 갱신된다.
/// 깊이는 sortingOrder로 모듈 뒤에 깔린다(정사영이라 Z 위치는 화면에 안 보임).
/// 셰이더: BRB/ShadowProjector
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FlatShadow : MonoBehaviour
{
    [Header("Light")]
    [Tooltip("그림자를 만들 광원. 비우면 씬의 메인 라이트를 자동 사용.")]
    public Light lightSource;

    [Header("Shadow Look")]
    [Range(0f, 1f)] public float strength = 0.55f;
    [Range(0f, 1f)] public float tipFade = 0.35f;
    [Tooltip("그림자 최대 길이(월드 단위).")]
    public float maxLength = 1.4f;
    [Tooltip("점광/스팟이 가까울수록 그림자가 길어지는 배수(거리 0에서 적용).")]
    public float angleStretch = 1.5f;
    public Color shadowColor = Color.black;
    [Tooltip("스프라이트 위/아래가 반대로 투영되면 체크.")]
    public bool flipV = false;

    static readonly int IdMainTex = Shader.PropertyToID("_MainTex");
    static readonly int IdShadowColor = Shader.PropertyToID("_ShadowColor");
    static readonly int IdStrength = Shader.PropertyToID("_ShadowStrength");
    static readonly int IdTipFade = Shader.PropertyToID("_TipFade");
    static readonly int IdFlipV = Shader.PropertyToID("_FlipV");
    static readonly int IdDir = Shader.PropertyToID("_ShadowDirWS");
    static readonly int IdLength = Shader.PropertyToID("_ShadowLength");

    GameObject _shadowGo;
    MeshRenderer _shadowRend;
    Material _shadowMat;

    void Start()
    {
        BuildShadow();
    }

    void BuildShadow()
    {
        var srcFilter = GetComponent<MeshFilter>();
        var srcRend = GetComponent<MeshRenderer>();
        if (srcFilter == null || srcRend == null) return;

        _shadowGo = new GameObject("Shadow");
        _shadowGo.transform.SetParent(transform, false);
        // 부모와 동일 위치/회전/스케일, 바닥쪽으로 살짝
        _shadowGo.transform.localPosition = new Vector3(0f, 0f, 0f);
        _shadowGo.transform.localRotation = Quaternion.identity;
        _shadowGo.transform.localScale = Vector3.one;

        var mf = _shadowGo.AddComponent<MeshFilter>();
        mf.sharedMesh = srcFilter.sharedMesh;

        _shadowRend = _shadowGo.AddComponent<MeshRenderer>();
        _shadowRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _shadowRend.receiveShadows = false;

        var shader = Shader.Find("BRB/ShadowProjector");
        _shadowMat = new Material(shader);

        // 모듈 텍스처 그대로 사용 (실루엣 모양 일치)
        var srcMat = srcRend.sharedMaterial;
        if (srcMat != null && srcMat.HasProperty(IdMainTex))
            _shadowMat.SetTexture(IdMainTex, srcMat.GetTexture(IdMainTex));

        _shadowMat.SetColor(IdShadowColor, shadowColor);
        _shadowMat.SetFloat(IdStrength, strength);
        _shadowMat.SetFloat(IdTipFade, tipFade);
        _shadowMat.SetFloat(IdFlipV, flipV ? 1f : 0f);
        _shadowRend.sharedMaterial = _shadowMat;

        // 모듈보다 먼저 그리도록 (셰이더 Queue가 Transparent-10이라 자동이지만 안전하게)
        _shadowRend.sortingOrder = srcRend.sortingOrder - 1;
    }

    void LateUpdate()
    {
        if (_shadowMat == null) return;

        Light lt = lightSource != null ? lightSource : ResolveMainLight();

        // 바닥 평면(XZ) 기준, 빛에서 오브젝트로 향하는 방향 = 그림자가 뻗는 방향
        Vector2 dir;
        float lengthScale = 1f;

        // 순수 2D: 월드 = XY 평면. 그림자는 빛 반대 방향(빛→오브젝트)으로 XY에서 뻗는다.
        if (lt == null)
        {
            dir = new Vector2(0f, -1f); // 기본: 화면 아래쪽
        }
        else if (lt.type == LightType.Directional)
        {
            // 디렉셔널: forward의 XY 성분이 곧 그림자 방향
            Vector3 f = lt.transform.forward;
            dir = new Vector2(f.x, f.y);
        }
        else
        {
            // 점광/스팟: 광원→오브젝트 방향 (XY), 가까울수록 길게(드라마틱)
            Vector3 d = transform.position - lt.transform.position;
            dir = new Vector2(d.x, d.y);
            float dist = dir.magnitude;
            lengthScale = Mathf.Lerp(angleStretch, 1f, Mathf.Clamp01(dist / Mathf.Max(0.001f, lt.range)));
        }

        if (dir.sqrMagnitude < 1e-5f) dir = new Vector2(0f, -1f);
        dir.Normalize();

        _shadowMat.SetVector(IdDir, new Vector4(dir.x, dir.y, 0f, 0f));
        _shadowMat.SetFloat(IdLength, maxLength * lengthScale);
    }

    Light _cachedMain;
    Light ResolveMainLight()
    {
        if (_cachedMain != null) return _cachedMain;
        // 디렉셔널 우선, 없으면 아무 라이트
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional) { _cachedMain = l; break; }
            if (_cachedMain == null) _cachedMain = l;
        }
        return _cachedMain;
    }

    void OnDestroy()
    {
        if (_shadowMat != null) Destroy(_shadowMat);
    }
}
