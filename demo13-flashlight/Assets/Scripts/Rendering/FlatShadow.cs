using UnityEngine;
using UnityEngine.Rendering.Universal; // Light2D

/// <summary>
/// 순수 2D(정사영) 탑다운 모듈에 방향성 투영 그림자를 붙인다. 월드 = XY 평면.
/// 모듈에 SpriteRenderer 또는 MeshRenderer(+MeshFilter 쿼드)가 있으면 자동 감지해
/// 자식으로 그림자(같은 스프라이트/메시 + BRB/ShadowProjector 머티리얼)를 만들고,
/// 2D 점광(Light2D) 위치에 따라 빛 반대쪽(XY)으로 늘려 그린다.
/// 라이트가 사방 어디에 있어도 매 프레임 방향이 갱신된다.
/// 깊이는 sortingOrder로 모듈 뒤에 깔린다(정사영이라 Z 위치는 화면에 안 보임).
/// 셰이더: BRB/ShadowProjector
/// </summary>
[DisallowMultipleComponent]
public class FlatShadow : MonoBehaviour
{
    [Header("Light (2D)")]
    [Tooltip("그림자를 드리울 2D 점광(Light2D). 비우면 씬에서 가장 가까운 Point Light2D 자동 사용.")]
    public Light2D lightSource;
    [Tooltip("라이트 대신 고정 방향(태양)을 쓸 때 체크. 방향은 ManualDirection.")]
    public bool useManualDirection = false;
    [Tooltip("고정 그림자 방향(XY, 빛 반대쪽).")]
    public Vector2 manualDirection = new Vector2(0f, -1f);

    [Header("Shadow Look")]
    [Range(0f, 1f)] public float strength = 0.55f;
    [Range(0f, 1f)] public float tipFade = 0.35f;
    [Tooltip("그림자 최대 길이(월드 단위).")]
    public float maxLength = 1.4f;
    [Tooltip("점광이 가까울수록 그림자가 길어지는 배수(거리 0에서 적용).")]
    public float proximityStretch = 1.5f;
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
    Material _shadowMat;

    void Start()
    {
        BuildShadow();
    }

    void BuildShadow()
    {
        var shader = Shader.Find("BRB/ShadowProjector");
        if (shader == null)
        {
            Debug.LogWarning("[FlatShadow] BRB/ShadowProjector 셰이더를 찾을 수 없음.");
            return;
        }
        _shadowMat = new Material(shader);

        _shadowGo = new GameObject("Shadow");
        _shadowGo.transform.SetParent(transform, false);
        _shadowGo.transform.localPosition = Vector3.zero;
        _shadowGo.transform.localRotation = Quaternion.identity;
        _shadowGo.transform.localScale = Vector3.one;

        Texture tex = null;
        var srcSR = GetComponent<SpriteRenderer>();
        var srcMR = GetComponent<MeshRenderer>();
        var srcMF = GetComponent<MeshFilter>();

        if (srcSR != null)
        {
            // 스프라이트 모듈: 같은 스프라이트로 그림자 SpriteRenderer 생성
            // ※ 스프라이트 Mesh Type = Full Rect 권장(Tight면 uv.y가 지오메트리와 안 맞아 shear가 틀어짐)
            var sr = _shadowGo.AddComponent<SpriteRenderer>();
            sr.sprite = srcSR.sprite;
            sr.flipX = srcSR.flipX;
            sr.flipY = srcSR.flipY;
            sr.sortingLayerID = srcSR.sortingLayerID;
            sr.sortingOrder = srcSR.sortingOrder - 1; // 모듈 뒤
            sr.sharedMaterial = _shadowMat;
            // SpriteRenderer는 스프라이트 텍스처를 _MainTex로 자동 바인딩
        }
        else if (srcMF != null && srcMR != null)
        {
            var mf = _shadowGo.AddComponent<MeshFilter>();
            mf.sharedMesh = srcMF.sharedMesh;
            var mr = _shadowGo.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingLayerID = srcMR.sortingLayerID;
            mr.sortingOrder = srcMR.sortingOrder - 1;
            mr.sharedMaterial = _shadowMat;

            var sm = srcMR.sharedMaterial;
            if (sm != null && sm.HasProperty(IdMainTex))
                tex = sm.GetTexture(IdMainTex);
        }
        else
        {
            Debug.LogWarning("[FlatShadow] SpriteRenderer나 MeshFilter+MeshRenderer가 필요함.", this);
            Destroy(_shadowGo);
            _shadowGo = null;
            return;
        }

        if (tex != null && _shadowMat.HasProperty(IdMainTex))
            _shadowMat.SetTexture(IdMainTex, tex);

        _shadowMat.SetColor(IdShadowColor, shadowColor);
        _shadowMat.SetFloat(IdStrength, strength);
        _shadowMat.SetFloat(IdTipFade, tipFade);
        _shadowMat.SetFloat(IdFlipV, flipV ? 1f : 0f);
    }

    void LateUpdate()
    {
        if (_shadowMat == null) return;

        // 순수 2D: 월드 = XY 평면. 그림자는 빛 반대 방향(빛→오브젝트)으로 XY에서 뻗는다.
        Vector2 dir;
        float lengthScale = 1f;

        if (useManualDirection)
        {
            dir = manualDirection;
        }
        else
        {
            Light2D lt = lightSource != null ? lightSource : ResolveNearestPointLight();
            if (lt == null)
            {
                dir = manualDirection;
            }
            else
            {
                Vector3 d = transform.position - lt.transform.position;
                dir = new Vector2(d.x, d.y);
                float dist = dir.magnitude;
                float range = Mathf.Max(0.001f, lt.pointLightOuterRadius);
                // 가까울수록(dist 작을수록) 길게
                lengthScale = Mathf.Lerp(proximityStretch, 1f, Mathf.Clamp01(dist / range));
            }
        }

        if (dir.sqrMagnitude < 1e-5f) dir = new Vector2(0f, -1f);
        dir.Normalize();

        _shadowMat.SetVector(IdDir, new Vector4(dir.x, dir.y, 0f, 0f));
        _shadowMat.SetFloat(IdLength, maxLength * lengthScale);
    }

    Light2D _cached;
    Light2D ResolveNearestPointLight()
    {
        // 가장 가까운 Point Light2D
        float best = float.MaxValue;
        Light2D found = null;
        foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (l.lightType != Light2D.LightType.Point) continue;
            float dsq = (l.transform.position - transform.position).sqrMagnitude;
            if (dsq < best) { best = dsq; found = l; }
        }
        _cached = found;
        return found;
    }

    void OnDestroy()
    {
        if (_shadowMat != null) Destroy(_shadowMat);
    }
}
