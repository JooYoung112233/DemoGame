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
    SpriteRenderer _srcSR, _dstSR;
    // 라이브 동기화 캐시 — 씬에서 SpriteRenderer를 편집하면 그림자가 따라가게(변경 시에만 갱신)
    Sprite _cSprite; bool _cFX, _cFY; SpriteDrawMode _cDraw; Vector2 _cSize; int _cOrder, _cLayer; float _cCutoff;

    void OnEnable()
    {
        if (!gameObject.scene.IsValid()) return;
        Build();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorSync;
        UnityEditor.EditorApplication.update += EditorSync;   // 매 에디터 틱 라이브 동기화(Update보다 확실)
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorSync;
#endif
        Cleanup();
    }

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

    /// <summary>그림자 자식을 강제로 다시 만든다(맵 저장 후 DontSave 자식이 떨어졌을 때 등).</summary>
    public void Rebuild() { if (gameObject.scene.IsValid()) Build(); }

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
            _srcSR = srcSR;
            _dstSR = _go.AddComponent<SpriteRenderer>();
            _dstSR.sharedMaterial = _mat;
            ApplyFromSource();   // sprite/flip/draw/size/sorting/cutout 복사 + 시그니처 캐시
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

    // ── 라이브 동기화: 씬에서 SpriteRenderer 편집 → 그림자 자식이 따라가게(에디트 모드) ──

    void ApplyFromSource()
    {
        if (_srcSR == null || _dstSR == null) return;
        _dstSR.sprite = _srcSR.sprite;
        _dstSR.flipX = _srcSR.flipX; _dstSR.flipY = _srcSR.flipY;
        _dstSR.drawMode = _srcSR.drawMode;
        if (_srcSR.drawMode != SpriteDrawMode.Simple) { _dstSR.tileMode = _srcSR.tileMode; _dstSR.size = _srcSR.size; }
        _dstSR.sortingLayerID = _srcSR.sortingLayerID;
        _dstSR.sortingOrder = _srcSR.sortingOrder - 2;   // 본체 뒤
        CopyCutout(_srcSR.sharedMaterial);
        CacheSignature();
    }

    void CacheSignature()
    {
        _cSprite = _srcSR.sprite; _cFX = _srcSR.flipX; _cFY = _srcSR.flipY; _cDraw = _srcSR.drawMode;
        _cSize = _srcSR.size; _cOrder = _srcSR.sortingOrder; _cLayer = _srcSR.sortingLayerID;
        _cCutoff = (_srcSR.sharedMaterial != null && _srcSR.sharedMaterial.HasProperty(IdCutoff))
            ? _srcSR.sharedMaterial.GetFloat(IdCutoff) : -1f;
    }

    bool Changed()
    {
        float cut = (_srcSR.sharedMaterial != null && _srcSR.sharedMaterial.HasProperty(IdCutoff))
            ? _srcSR.sharedMaterial.GetFloat(IdCutoff) : -1f;
        return _cSprite != _srcSR.sprite || _cFX != _srcSR.flipX || _cFY != _srcSR.flipY
            || _cDraw != _srcSR.drawMode || _cSize != _srcSR.size
            || _cOrder != _srcSR.sortingOrder || _cLayer != _srcSR.sortingLayerID || _cCutoff != cut;
    }

#if UNITY_EDITOR
    // 에디트 모드에서 매 틱 호출 — 베이스 SpriteRenderer가 바뀌면 그림자 자식 재동기화(변경 시에만, 런타임 비용 0).
    void EditorSync()
    {
        if (this == null) { UnityEditor.EditorApplication.update -= EditorSync; return; }
        if (Application.isPlaying) return;
        if (!isActiveAndEnabled || !gameObject.scene.IsValid()) return;
        if (_srcSR == null) _srcSR = GetComponent<SpriteRenderer>();
        if (_srcSR == null) return;                   // 메시 기반 등은 라이브 동기화 생략
        if (_go == null || _dstSR == null)            // 자식 없음(스프라이트 나중 지정 등) → 재생성
        {
            if (_srcSR.sprite != null) { Cleanup(); Build(); }
            return;
        }
        if (_srcSR.sprite == null) { Cleanup(); return; }
        if (Changed()) ApplyFromSource();
    }
#endif

    void OnDestroy()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorSync;
#endif
        Cleanup();
    }

    void Cleanup()
    {
        if (_go != null) DestroySafe(_go);
        if (_mat != null) DestroySafe(_mat);
        _go = null; _mat = null; _dstSR = null;
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
