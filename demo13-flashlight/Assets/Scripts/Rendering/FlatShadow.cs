using UnityEngine;
using UnityEngine.Rendering.Universal; // Light2D

/// <summary>
/// 순수 2D(정사영) 모듈에 방향성 투영 그림자를 붙인다. 월드 = XY 평면.
/// 모듈에 SpriteRenderer 또는 MeshRenderer(+MeshFilter 쿼드)가 있으면 자동 감지해
/// 자식으로 그림자(같은 스프라이트/메시 + 그림자 머티리얼)를 만들고,
/// 2D 라이트(Light2D) 위치에 따라 빛 반대쪽(XY)으로 늘려 그린다.
/// baseContact를 켜면 BRB/WallShadow를 써서 밑동에 상시 접지 그림자를 추가(벽/건물용).
/// 깊이는 sortingOrder로 모듈 뒤에 깔린다.
/// 셰이더: BRB/ShadowProjector (기본) / BRB/WallShadow (baseContact)
/// </summary>
[DisallowMultipleComponent]
public class FlatShadow : MonoBehaviour
{
    [Header("Light (2D)")]
    [Tooltip("그림자를 드리울 광원(Light2D). 비우면 씬에서 가장 가까운 Point Light2D 자동 사용.")]
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

    [Header("Base Contact (벽/건물 밑동 상시 그림자)")]
    [Tooltip("켜면 BRB/WallShadow 사용 — 밑동에 항상 깔리는 접지 그림자 추가.")]
    public bool baseContact = false;
    [Range(0f, 1f)] public float contactStrength = 0.5f;
    [Range(0.01f, 0.6f)] public float contactHeight = 0.15f;

    static readonly int IdMainTex = Shader.PropertyToID("_MainTex");
    static readonly int IdShadowColor = Shader.PropertyToID("_ShadowColor");
    static readonly int IdStrength = Shader.PropertyToID("_ShadowStrength");
    static readonly int IdTipFade = Shader.PropertyToID("_TipFade");
    static readonly int IdFlipV = Shader.PropertyToID("_FlipV");
    static readonly int IdShadowFlipV = Shader.PropertyToID("_ShadowFlipV");
    static readonly int IdDir = Shader.PropertyToID("_ShadowDirWS");
    static readonly int IdLength = Shader.PropertyToID("_ShadowLength");
    static readonly int IdContactStrength = Shader.PropertyToID("_ContactStrength");
    static readonly int IdContactHeight = Shader.PropertyToID("_ContactHeight");

    GameObject _shadowGo;
    Material _shadowMat;

    void Start()
    {
        BuildShadow();
    }

    void BuildShadow()
    {
        // 벽/건물(baseContact)은 WallPixel을 _SHADOW_MODE로 재사용(셰이더 통합),
        // 일반 모듈은 전용 ShadowProjector.
        string shaderName = baseContact ? "BRB/WallPixel" : "BRB/ShadowProjector";
        var shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogWarning($"[FlatShadow] {shaderName} 셰이더를 찾을 수 없음.");
            return;
        }
        _shadowMat = new Material(shader);
        if (baseContact)
            _shadowMat.EnableKeyword("_SHADOW_MODE"); // WallPixel을 그림자로 동작시킴

        _shadowGo = new GameObject("Shadow");
        _shadowGo.transform.SetParent(transform, false);
        _shadowGo.transform.localPosition = Vector3.zero;
        _shadowGo.transform.localRotation = Quaternion.identity;
        _shadowGo.transform.localScale = Vector3.one;

        Texture tex = null;
        Material srcMat = null;
        var srcSR = GetComponent<SpriteRenderer>();
        var srcMR = GetComponent<MeshRenderer>();
        var srcMF = GetComponent<MeshFilter>();

        if (srcSR != null)
        {
            // 스프라이트 모듈 — Mesh Type = Full Rect 권장
            var sr = _shadowGo.AddComponent<SpriteRenderer>();
            sr.sprite = srcSR.sprite;
            sr.flipX = srcSR.flipX;
            sr.flipY = srcSR.flipY;
            sr.sortingLayerID = srcSR.sortingLayerID;
            sr.sortingOrder = srcSR.sortingOrder - 1; // 모듈 뒤
            sr.sharedMaterial = _shadowMat;
            srcMat = srcSR.sharedMaterial;
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

            srcMat = srcMR.sharedMaterial;
            if (srcMat != null && srcMat.HasProperty(IdMainTex))
                tex = srcMat.GetTexture(IdMainTex);
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

        // 원본 벽의 실루엣 설정을 그림자에 맞춤 (컷오프 + 문 흰색뚫기)
        if (baseContact && srcMat != null)
        {
            int idCutoff = Shader.PropertyToID("_Cutoff");
            if (srcMat.HasProperty(idCutoff) && _shadowMat.HasProperty(idCutoff))
                _shadowMat.SetFloat(idCutoff, srcMat.GetFloat(idCutoff));

            if (srcMat.IsKeywordEnabled("_WHITE_CUTOUT"))
            {
                _shadowMat.EnableKeyword("_WHITE_CUTOUT");
                int idWT = Shader.PropertyToID("_WhiteThreshold");
                int idWS = Shader.PropertyToID("_WhiteSoftness");
                if (srcMat.HasProperty(idWT)) _shadowMat.SetFloat(idWT, srcMat.GetFloat(idWT));
                if (srcMat.HasProperty(idWS)) _shadowMat.SetFloat(idWS, srcMat.GetFloat(idWS));
            }
        }

        _shadowMat.SetColor(IdShadowColor, shadowColor);
        _shadowMat.SetFloat(IdStrength, strength);
        _shadowMat.SetFloat(IdTipFade, tipFade);
        // flip 프로퍼티 이름이 셰이더마다 다름 (_FlipV / _ShadowFlipV)
        if (_shadowMat.HasProperty(IdFlipV)) _shadowMat.SetFloat(IdFlipV, flipV ? 1f : 0f);
        if (_shadowMat.HasProperty(IdShadowFlipV)) _shadowMat.SetFloat(IdShadowFlipV, flipV ? 1f : 0f);

        if (baseContact)
        {
            if (_shadowMat.HasProperty(IdContactStrength)) _shadowMat.SetFloat(IdContactStrength, contactStrength);
            if (_shadowMat.HasProperty(IdContactHeight)) _shadowMat.SetFloat(IdContactHeight, contactHeight);
        }
    }

    void LateUpdate()
    {
        if (_shadowMat == null) return;

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
                lengthScale = Mathf.Lerp(proximityStretch, 1f, Mathf.Clamp01(dist / range));
            }
        }

        if (dir.sqrMagnitude < 1e-5f) dir = new Vector2(0f, -1f);
        dir.Normalize();

        _shadowMat.SetVector(IdDir, new Vector4(dir.x, dir.y, 0f, 0f));
        _shadowMat.SetFloat(IdLength, maxLength * lengthScale);
    }

    Light2D ResolveNearestPointLight()
    {
        float best = float.MaxValue;
        Light2D found = null;
        foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (l.lightType != Light2D.LightType.Point) continue;
            float dsq = (l.transform.position - transform.position).sqrMagnitude;
            if (dsq < best) { best = dsq; found = l; }
        }
        return found;
    }

    void OnDestroy()
    {
        if (_shadowMat != null) Destroy(_shadowMat);
    }
}
