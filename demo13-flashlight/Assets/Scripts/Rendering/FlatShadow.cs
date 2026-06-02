using UnityEngine;
using UnityEngine.Rendering.Universal; // Light2D

/// <summary>
/// 순수 2D(정사영) 모듈에 그림자를 붙인다. 월드 = XY 평면.
/// 두 종류를 독립 토글로 지원:
///   ① 정적 발밑 그림자(groundShadow) — 이미지 바로 밑에 고정. 빛과 무관하게 항상 같은 자리.
///   ② 동적 투영 그림자(projectedShadow) — Light2D 반대쪽으로 늘어나는 반응형.
/// 그림자는 SpriteRenderer/MeshRenderer를 복제한 별도 자식으로 그린다(셰이더 한 패스 제약 때문).
/// 셰이더: BRB/WallPixel (_SHADOW_MODE) — 정적·동적 그림자 모두 사용(URP 2D 검증됨)
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class FlatShadow : MonoBehaviour
{
    /// <summary>동적 투영 그림자의 방향 기준.</summary>
    public enum DirMode
    {
        Player,        // FlatShadowDirector.Source(보통 플레이어) — 늦게 스폰돼도 자동 연결
        NearestLight,  // 씬에서 가장 가까운 Point Light2D
        SpecificLight, // lightSource로 지정한 Light2D
        Manual,        // manualDirection 고정(월드 기준)
    }

    [Header("Light / 방향 (2D)")]
    [Tooltip("동적 투영 그림자 방향 기준. Player=플레이어 따라(권장), NearestLight=최근접 점광, SpecificLight=지정 라이트, Manual=고정 방향.")]
    public DirMode directionMode = DirMode.Player;
    [Tooltip("SpecificLight 모드에서 사용할 Light2D.")]
    public Light2D lightSource;
    [Tooltip("Manual 모드 방향(XY, 빛 반대쪽). 소스가 아직 없을 때 임시 폴백으로도 쓰임.")]
    public Vector2 manualDirection = new Vector2(0f, -1f);
    [Tooltip("Player 모드 거리 감쇠 기준 반경(가까울수록 길어짐).")]
    public float lightRange = 5f;

    [Header("① 정적 발밑 그림자 (이미지 밑 고정)")]
    [Tooltip("이미지 발밑에 항상 깔리는 그림자. 빛과 무관하게 고정.")]
    public bool groundShadow = true;
    [Range(0f, 1f)] public float groundStrength = 0.6f;
    [Tooltip("발밑 그림자가 화면 아래(앞)로 깔리는 길이. 0이면 스프라이트 뒤에 가려 안 보임.")]
    public float groundLength = 0.35f;
    [Tooltip("발밑 그림자 추가 위치 오프셋(월드 단위).")]
    public Vector2 groundOffset = Vector2.zero;

    [Header("② 동적 투영 그림자 (빛 따라 늘어남)")]
    [Tooltip("Light2D 반대쪽으로 늘어나는 반응형 그림자.")]
    public bool projectedShadow = true;
    [Range(0f, 1f)] public float strength = 0.55f;
    [Range(0f, 1f)] public float tipFade = 0.35f;
    public float maxLength = 1.4f;
    [Tooltip("점광이 가까울수록 그림자가 길어지는 배수.")]
    public float proximityStretch = 1.5f;
    [Tooltip("빛에서 멀수록 그림자가 옅어지는 정도(0=거리무관 일정, 1=멀면 거의 사라짐).")]
    [Range(0f, 1f)] public float distanceFade = 0.5f;
    [Tooltip("(레거시) 현재는 그림자가 항상 WallPixel(_SHADOW_MODE)로 통일돼서 효과 없음.")]
    public bool baseContact = false;

    [Header("공통")]
    public Color shadowColor = Color.black;
    [Tooltip("켜면 빛 위/아래에 따라 앵커를 자동 반전 — 단, 수평선 넘을 때 '탁' 뒤집힘(세로 미러). 부드럽게 하려면 OFF 권장(대칭 프롭은 차이 거의 없음).")]
    public bool autoFlipV = false;
    [Tooltip("스프라이트 발밑 방향 보정(아트가 위아래 반대면 체크). autoFlipV OFF일 때 고정 사용.")]
    public bool flipV = false;

    static readonly int IdMainTex = Shader.PropertyToID("_MainTex");
    static readonly int IdShadowColor = Shader.PropertyToID("_ShadowColor");
    static readonly int IdStrength = Shader.PropertyToID("_ShadowStrength");
    static readonly int IdTipFade = Shader.PropertyToID("_TipFade");
    static readonly int IdFlipV = Shader.PropertyToID("_FlipV");
    static readonly int IdShadowFlipV = Shader.PropertyToID("_ShadowFlipV");
    static readonly int IdDir = Shader.PropertyToID("_ShadowDirWS");
    static readonly int IdLength = Shader.PropertyToID("_ShadowLength");
    static readonly int IdShadowHeight = Shader.PropertyToID("_ShadowHeight");
    static readonly int IdContactStrength = Shader.PropertyToID("_ContactStrength");
    static readonly int IdContactHeight = Shader.PropertyToID("_ContactHeight");
    static readonly int IdCutoff = Shader.PropertyToID("_Cutoff");
    static readonly int IdWhiteThreshold = Shader.PropertyToID("_WhiteThreshold");
    static readonly int IdWhiteSoftness = Shader.PropertyToID("_WhiteSoftness");

    // 정적 발밑
    GameObject _groundGo; Material _groundMat;
    // 동적 투영
    GameObject _projGo; Material _projMat;

    void OnEnable()
    {
        if (!gameObject.scene.IsValid()) return;
        BuildShadows();
    }

    void OnDisable() => CleanupAll();

    void OnValidate()
    {
        if (!isActiveAndEnabled || !gameObject.scene.IsValid()) return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += DeferredRebuild;
#endif
    }

#if UNITY_EDITOR
    void DeferredRebuild()
    {
        if (this == null) return;
        if (!isActiveAndEnabled || !gameObject.scene.IsValid()) return;
        CleanupAll();
        BuildShadows();
    }
#endif

    void BuildShadows()
    {
        // 재컴파일/중복 대비: 추적 중인 것 + 이름으로 남은 고아 그림자 자식 제거 후 새로 생성
        if (_groundGo != null || _projGo != null) CleanupAll();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c != null && (c.name == "ShadowGround" || c.name == "ShadowProjected"))
                DestroySafe(c.gameObject);
        }

        var srcSR = GetComponent<SpriteRenderer>();
        var srcMR = GetComponent<MeshRenderer>();
        var srcMF = GetComponent<MeshFilter>();
        Material srcMat = srcSR != null ? srcSR.sharedMaterial
                        : srcMR != null ? srcMR.sharedMaterial : null;

        if (srcSR == null && (srcMF == null || srcMR == null))
        {
            Debug.LogWarning("[FlatShadow] SpriteRenderer나 MeshFilter+MeshRenderer가 필요함.", this);
            return;
        }

        // ① 정적 발밑 그림자 — 실루엣을 통째로 아래(화면 앞)로 offset해서 발밑에 깔리게.
        //   shear 대신 offset → 불투명 픽셀이 통째로 내려가 벽/프롭 아래로 확실히 삐져나옴(텍스처 여백 무관).
        //   셰이더는 URP 2D에서 검증된 WallPixel(_SHADOW_MODE) 사용(ShadowProjector는 2D에서 안 그려짐).
        if (groundShadow)
        {
            _groundMat = MakeMaterial("BRB/WallPixel", true);
            Vector3 gpos = new Vector3(groundOffset.x, groundOffset.y - Mathf.Max(0.02f, groundLength), 0f);
            _groundGo = MakeChild("ShadowGround", -2, gpos, srcSR, srcMR, srcMF, srcMat, _groundMat);
            if (_groundMat != null)
            {
                _groundMat.SetFloat(IdStrength, groundStrength);
                _groundMat.SetFloat(IdTipFade, 1f); // 균일(끝 안 흐림)
                if (_groundMat.HasProperty(IdContactStrength)) _groundMat.SetFloat(IdContactStrength, 0f);
                _groundMat.SetVector(IdDir, Vector4.zero); // shear 없음 = 정적, offset만
                _groundMat.SetFloat(IdLength, 0f);
                if (_groundMat.HasProperty(IdShadowHeight)) _groundMat.SetFloat(IdShadowHeight, 0f); // offset 모드(붕괴 안 함)
            }
        }

        // ② 동적 투영 그림자 — 항상 WallPixel(_SHADOW_MODE) 사용(URP 2D 검증됨).
        if (projectedShadow)
        {
            _projMat = MakeMaterial("BRB/WallPixel", true);
            _projGo = MakeChild("ShadowProjected", -1, Vector3.zero, srcSR, srcMR, srcMF, srcMat, _projMat);
            if (_projMat != null)
            {
                _projMat.SetFloat(IdStrength, strength);
                _projMat.SetFloat(IdTipFade, tipFade);
                if (_projMat.HasProperty(IdContactStrength)) _projMat.SetFloat(IdContactStrength, 0f); // 발밑은 정적이 담당
                // 발밑 피벗 투영 활성화 → 어느 방향이든 일정 길이로 부드럽게(플립/스냅 없음)
                _projMat.SetFloat(IdShadowHeight, ComputeWorldHeight(srcSR, srcMR));
            }
        }
    }

    Material MakeMaterial(string shaderName, bool shadowKeyword)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null) { Debug.LogWarning($"[FlatShadow] {shaderName} 못 찾음."); return null; }
        var m = new Material(shader);
        if (shadowKeyword) m.EnableKeyword("_SHADOW_MODE");
        m.SetColor(IdShadowColor, shadowColor);
        if (m.HasProperty(IdFlipV)) m.SetFloat(IdFlipV, flipV ? 1f : 0f);
        if (m.HasProperty(IdShadowFlipV)) m.SetFloat(IdShadowFlipV, flipV ? 1f : 0f);
        return m;
    }

    GameObject MakeChild(string name, int sortDelta, Vector3 localPos,
        SpriteRenderer srcSR, MeshRenderer srcMR, MeshFilter srcMF, Material srcMat, Material mat)
    {
        if (mat == null) return null;
        var go = new GameObject(name);
        go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        Texture tex = null;
        if (srcSR != null)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = srcSR.sprite;
            sr.flipX = srcSR.flipX;
            sr.flipY = srcSR.flipY;
            sr.drawMode = srcSR.drawMode;
            if (srcSR.drawMode != SpriteDrawMode.Simple)
            {
                sr.tileMode = srcSR.tileMode;
                sr.size = srcSR.size;
            }
            sr.sortingLayerID = srcSR.sortingLayerID;
            sr.sortingOrder = srcSR.sortingOrder + sortDelta;
            sr.sharedMaterial = mat;
        }
        else
        {
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = srcMF.sharedMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingLayerID = srcMR.sortingLayerID;
            mr.sortingOrder = srcMR.sortingOrder + sortDelta;
            mr.sharedMaterial = mat;
            if (srcMat != null && srcMat.HasProperty(IdMainTex))
                tex = srcMat.GetTexture(IdMainTex);
        }

        if (tex != null && mat.HasProperty(IdMainTex))
            mat.SetTexture(IdMainTex, tex);

        // 실루엣 일치: 컷오프 + 문 흰색뚫기 복사
        if (srcMat != null)
        {
            if (srcMat.HasProperty(IdCutoff) && mat.HasProperty(IdCutoff))
                mat.SetFloat(IdCutoff, srcMat.GetFloat(IdCutoff));
            if (srcMat.IsKeywordEnabled("_WHITE_CUTOUT"))
            {
                mat.EnableKeyword("_WHITE_CUTOUT");
                if (srcMat.HasProperty(IdWhiteThreshold)) mat.SetFloat(IdWhiteThreshold, srcMat.GetFloat(IdWhiteThreshold));
                if (srcMat.HasProperty(IdWhiteSoftness)) mat.SetFloat(IdWhiteSoftness, srcMat.GetFloat(IdWhiteSoftness));
            }
        }
        return go;
    }

    void LateUpdate()
    {
        // 동적 투영만 매 프레임 갱신. 정적 발밑은 건드리지 않음.
        if (_projMat == null) return;

        Vector2 dir = manualDirection;
        float lengthScale = 1f;
        float strengthScale = 1f;

        // 방향 기준 위치/반경 결정
        bool havePos = false;
        Vector3 srcPos = Vector3.zero;
        float srcRange = lightRange;

        switch (directionMode)
        {
            case DirMode.Manual:
                break; // dir = manualDirection 고정
            case DirMode.SpecificLight:
                if (lightSource != null) { srcPos = lightSource.transform.position; srcRange = Mathf.Max(0.001f, lightSource.pointLightOuterRadius); havePos = true; }
                break;
            case DirMode.NearestLight:
            {
                var lt = ResolveNearestPointLight();
                if (lt != null) { srcPos = lt.transform.position; srcRange = Mathf.Max(0.001f, lt.pointLightOuterRadius); havePos = true; }
                break;
            }
            default: // Player — 매니저가 늦게 생긴 플레이어도 연결
            {
                var s = FlatShadowDirector.Source;
                if (s != null) { srcPos = s.position; srcRange = Mathf.Max(0.001f, lightRange); havePos = true; }
                break;
            }
        }

        if (directionMode != DirMode.Manual)
        {
            if (havePos)
            {
                Vector3 d = transform.position - srcPos;       // 빛→오브젝트 = 그림자 방향
                dir = new Vector2(d.x, d.y);
                float dist = dir.magnitude;
                float t = Mathf.Clamp01(dist / srcRange);       // 0=라이트 위, 1=반경 끝
                lengthScale = Mathf.Lerp(proximityStretch, 1f, t);     // 가까울수록 길게
                strengthScale = Mathf.Lerp(1f, 1f - distanceFade, t);  // 멀수록 옅게
            }
            else dir = manualDirection; // 소스 아직 없음(플레이어 미스폰) → 임시
        }

        if (dir.sqrMagnitude < 1e-5f) dir = new Vector2(0f, -1f);
        dir.Normalize();

        // 라이트가 위쪽(그림자가 아래로 뻗음 dir.y<0)이면 위/아래 앵커를 뒤집어 자연스럽게.
        if (autoFlipV)
        {
            float fv = (dir.y < 0f) ? 1f : 0f;
            if (_projMat.HasProperty(IdFlipV)) _projMat.SetFloat(IdFlipV, fv);
            if (_projMat.HasProperty(IdShadowFlipV)) _projMat.SetFloat(IdShadowFlipV, fv);
        }

        _projMat.SetVector(IdDir, new Vector4(dir.x, dir.y, 0f, 0f));
        _projMat.SetFloat(IdLength, maxLength * lengthScale);
        _projMat.SetFloat(IdStrength, strength * strengthScale); // 거리에 따라 진하기 조절
    }

    // 발밑 투영용 스프라이트 월드 높이(세로 크기)
    float ComputeWorldHeight(SpriteRenderer sr, MeshRenderer mr)
    {
        if (sr != null)
        {
            float h = (sr.drawMode != SpriteDrawMode.Simple)
                ? sr.size.y
                : (sr.sprite != null ? sr.sprite.bounds.size.y : 1f);
            return Mathf.Max(0.01f, h * Mathf.Abs(transform.lossyScale.y));
        }
        if (mr != null) return Mathf.Max(0.01f, mr.bounds.size.y);
        return 1f;
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

    void OnDestroy() => CleanupAll();

    void CleanupAll()
    {
        DestroySafe(_groundGo); DestroySafe(_groundMat); _groundGo = null; _groundMat = null;
        DestroySafe(_projGo);   DestroySafe(_projMat);   _projGo = null;   _projMat = null;
    }

    static void DestroySafe(Object o)
    {
        if (o == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) { DestroyImmediate(o); return; }
#endif
        Destroy(o);
    }

#if UNITY_EDITOR
    const string DefKey = "FlatShadow.default.";

    // 컴포넌트 추가/Reset 시 저장된 기본값 자동 적용
    void Reset()
    {
        LoadDefaults(true); // 저장된 기본값 적용(없으면 코드 기본값) + 그림자 생성
    }

    /// <summary>현재 값을 에디터 기본값으로 저장(EditorPrefs).</summary>
    public void SaveAsDefault()
    {
        UnityEditor.EditorPrefs.SetBool(DefKey + "set", true);
        UnityEditor.EditorPrefs.SetBool(DefKey + "groundShadow", groundShadow);
        UnityEditor.EditorPrefs.SetFloat(DefKey + "groundStrength", groundStrength);
        UnityEditor.EditorPrefs.SetFloat(DefKey + "groundLength", groundLength);
        UnityEditor.EditorPrefs.SetFloat(DefKey + "groundOffX", groundOffset.x);
        UnityEditor.EditorPrefs.SetFloat(DefKey + "groundOffY", groundOffset.y);
        UnityEditor.EditorPrefs.SetBool(DefKey + "projectedShadow", projectedShadow);
        UnityEditor.EditorPrefs.SetFloat(DefKey + "strength", strength);
        UnityEditor.EditorPrefs.SetFloat(DefKey + "tipFade", tipFade);
        UnityEditor.EditorPrefs.SetFloat(DefKey + "maxLength", maxLength);
        UnityEditor.EditorPrefs.SetFloat(DefKey + "proximityStretch", proximityStretch);
        UnityEditor.EditorPrefs.SetFloat(DefKey + "distanceFade", distanceFade);
        UnityEditor.EditorPrefs.SetBool(DefKey + "baseContact", baseContact);
        UnityEditor.EditorPrefs.SetInt(DefKey + "dirMode", (int)directionMode);
        UnityEditor.EditorPrefs.SetFloat(DefKey + "lightRange", lightRange);
        UnityEditor.EditorPrefs.SetBool(DefKey + "autoFlipV", autoFlipV);
        UnityEditor.EditorPrefs.SetBool(DefKey + "flipV", flipV);
        UnityEditor.EditorPrefs.SetFloat(DefKey + "colR", shadowColor.r);
        UnityEditor.EditorPrefs.SetFloat(DefKey + "colG", shadowColor.g);
        UnityEditor.EditorPrefs.SetFloat(DefKey + "colB", shadowColor.b);
        UnityEditor.EditorPrefs.SetFloat(DefKey + "colA", shadowColor.a);
        Debug.Log("[FlatShadow] 현재값을 기본값으로 저장했습니다.");
    }

    /// <summary>저장된 기본값을 불러와 적용. 없으면 하드코드 기본값.</summary>
    public void LoadDefaults(bool rebuild = true)
    {
        if (UnityEditor.EditorPrefs.GetBool(DefKey + "set", false))
        {
            groundShadow = UnityEditor.EditorPrefs.GetBool(DefKey + "groundShadow", groundShadow);
            groundStrength = UnityEditor.EditorPrefs.GetFloat(DefKey + "groundStrength", groundStrength);
            groundLength = UnityEditor.EditorPrefs.GetFloat(DefKey + "groundLength", groundLength);
            groundOffset = new Vector2(
                UnityEditor.EditorPrefs.GetFloat(DefKey + "groundOffX", groundOffset.x),
                UnityEditor.EditorPrefs.GetFloat(DefKey + "groundOffY", groundOffset.y));
            projectedShadow = UnityEditor.EditorPrefs.GetBool(DefKey + "projectedShadow", projectedShadow);
            strength = UnityEditor.EditorPrefs.GetFloat(DefKey + "strength", strength);
            tipFade = UnityEditor.EditorPrefs.GetFloat(DefKey + "tipFade", tipFade);
            maxLength = UnityEditor.EditorPrefs.GetFloat(DefKey + "maxLength", maxLength);
            proximityStretch = UnityEditor.EditorPrefs.GetFloat(DefKey + "proximityStretch", proximityStretch);
            distanceFade = UnityEditor.EditorPrefs.GetFloat(DefKey + "distanceFade", distanceFade);
            baseContact = UnityEditor.EditorPrefs.GetBool(DefKey + "baseContact", baseContact);
            directionMode = (DirMode)UnityEditor.EditorPrefs.GetInt(DefKey + "dirMode", (int)directionMode);
            lightRange = UnityEditor.EditorPrefs.GetFloat(DefKey + "lightRange", lightRange);
            autoFlipV = UnityEditor.EditorPrefs.GetBool(DefKey + "autoFlipV", autoFlipV);
            flipV = UnityEditor.EditorPrefs.GetBool(DefKey + "flipV", flipV);
            shadowColor = new Color(
                UnityEditor.EditorPrefs.GetFloat(DefKey + "colR", 0f),
                UnityEditor.EditorPrefs.GetFloat(DefKey + "colG", 0f),
                UnityEditor.EditorPrefs.GetFloat(DefKey + "colB", 0f),
                UnityEditor.EditorPrefs.GetFloat(DefKey + "colA", 1f));
        }
        else ResetHardDefaults();

        if (rebuild && isActiveAndEnabled && gameObject.scene.IsValid())
        {
            CleanupAll();
            BuildShadows();
        }
    }

    /// <summary>코드 하드코드 기본값으로(EditorPrefs 무시).</summary>
    public void ResetHardDefaults()
    {
        groundShadow = true; groundStrength = 0.6f; groundLength = 0.35f; groundOffset = Vector2.zero;
        projectedShadow = true; strength = 0.55f; tipFade = 0.35f; maxLength = 1.4f; proximityStretch = 1.5f;
        distanceFade = 0.5f; baseContact = false; directionMode = DirMode.Player; lightRange = 5f;
        autoFlipV = false; flipV = false; shadowColor = Color.black;
    }

    public static void ClearDefault() => UnityEditor.EditorPrefs.DeleteKey(DefKey + "set");
#endif
}
