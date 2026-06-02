using UnityEngine;

/// <summary>
/// 정적 발밑(접지) 그림자 — 물체 바로 밑에 항상 깔리는 어두운 실루엣. 빛과 무관(고정).
/// ShadowCaster2D(동적 캐스트 그림자)와 함께 쓰면: 빛 없는 곳에서도 접지감 유지 + 빛 있으면 캐스트 그림자.
/// 자식으로 SpriteRenderer/MeshRenderer를 복제해 BRB/WallPixel(_SHADOW_MODE, shear 0)로 어둡게.
/// 그림자 자식은 HideFlags.DontSave(런타임 전용).
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class GroundShadow2D : MonoBehaviour
{
    [Tooltip("그림자 색.")]
    public Color shadowColor = Color.black;
    [Range(0f, 1f)] public float strength = 0.45f;
    [Tooltip("발밑으로 살짝 내릴 거리(아래 offset). 발 밑에 깔리게.")]
    public float offsetY = -0.08f;

    static readonly int IdMainTex = Shader.PropertyToID("_MainTex");
    static readonly int IdShadowColor = Shader.PropertyToID("_ShadowColor");
    static readonly int IdStrength = Shader.PropertyToID("_ShadowStrength");
    static readonly int IdTipFade = Shader.PropertyToID("_TipFade");
    static readonly int IdContactStrength = Shader.PropertyToID("_ContactStrength");
    static readonly int IdDir = Shader.PropertyToID("_ShadowDirWS");
    static readonly int IdLength = Shader.PropertyToID("_ShadowLength");
    static readonly int IdHeight = Shader.PropertyToID("_ShadowHeight");
    static readonly int IdCutoff = Shader.PropertyToID("_Cutoff");

    GameObject _go;
    Material _mat;

    void OnEnable()
    {
        if (!gameObject.scene.IsValid()) return;
        Build();
    }

    void OnDisable() => Cleanup();

    void OnValidate()
    {
        if (!isActiveAndEnabled || !gameObject.scene.IsValid()) return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || !isActiveAndEnabled || !gameObject.scene.IsValid()) return;
            Cleanup(); Build();
        };
#endif
    }

    void Build()
    {
        // 재컴파일/중복 대비: 기존 고아 자식 제거
        if (_go != null) Cleanup();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c != null && c.name == "GroundShadow") DestroySafe(c.gameObject);
        }

        var shader = Shader.Find("BRB/WallPixel");
        if (shader == null) { Debug.LogWarning("[GroundShadow2D] BRB/WallPixel 못 찾음."); return; }
        _mat = new Material(shader);
        _mat.EnableKeyword("_SHADOW_MODE");
        _mat.SetColor(IdShadowColor, shadowColor);
        _mat.SetFloat(IdStrength, strength);
        _mat.SetFloat(IdTipFade, 1f);                       // 균일
        if (_mat.HasProperty(IdContactStrength)) _mat.SetFloat(IdContactStrength, 0f);
        _mat.SetVector(IdDir, Vector4.zero);                // shear 없음 = 정적
        _mat.SetFloat(IdLength, 0f);
        if (_mat.HasProperty(IdHeight)) _mat.SetFloat(IdHeight, 0f);

        _go = new GameObject("GroundShadow");
        _go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
        _go.transform.SetParent(transform, false);
        _go.transform.localPosition = new Vector3(0f, offsetY, 0f);

        var srcSR = GetComponent<SpriteRenderer>();
        var srcMR = GetComponent<MeshRenderer>();
        var srcMF = GetComponent<MeshFilter>();

        if (srcSR != null)
        {
            var sr = _go.AddComponent<SpriteRenderer>();
            sr.sprite = srcSR.sprite;
            sr.flipX = srcSR.flipX; sr.flipY = srcSR.flipY;
            sr.drawMode = srcSR.drawMode;
            if (srcSR.drawMode != SpriteDrawMode.Simple) { sr.tileMode = srcSR.tileMode; sr.size = srcSR.size; }
            sr.sortingLayerID = srcSR.sortingLayerID;
            sr.sortingOrder = srcSR.sortingOrder - 2;       // 본체 뒤(캐스트 그림자보다도 아래)
            sr.sharedMaterial = _mat;
            CopyCutout(srcSR.sharedMaterial);
        }
        else if (srcMF != null && srcMR != null)
        {
            var mf = _go.AddComponent<MeshFilter>(); mf.sharedMesh = srcMF.sharedMesh;
            var mr = _go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingLayerID = srcMR.sortingLayerID;
            mr.sortingOrder = srcMR.sortingOrder - 2;
            mr.sharedMaterial = _mat;
            var sm = srcMR.sharedMaterial;
            if (sm != null && sm.HasProperty(IdMainTex)) _mat.SetTexture(IdMainTex, sm.GetTexture(IdMainTex));
            CopyCutout(sm);
        }
        else { DestroySafe(_go); _go = null; }
    }

    void CopyCutout(Material src)
    {
        if (src != null && src.HasProperty(IdCutoff) && _mat.HasProperty(IdCutoff))
            _mat.SetFloat(IdCutoff, src.GetFloat(IdCutoff));
    }

    void OnDestroy() => Cleanup();

    void Cleanup()
    {
        if (_go != null) DestroySafe(_go);
        if (_mat != null) DestroySafe(_mat);
        _go = null; _mat = null;
    }

    static void DestroySafe(Object o)
    {
        if (o == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) { DestroyImmediate(o); return; }
#endif
        Destroy(o);
    }
}
