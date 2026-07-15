using UnityEngine;

/// <summary>
/// 짙은 현상 "볼류메트릭 헤이즈" 프로토 프리뷰 (BRB/AnomalyHazePrototype 셰이더).
/// 기존 DenseAnomalyController 를 건드리지 않는 독립 실험용 — 씬의 아무 GameObject에 붙이면
/// 카메라 앞에 풀스크린 안개 쿼드를 만들어 인스펙터 슬라이더로 바로 튜닝(에디트/플레이 모두).
///
/// 사용법:
///   1) 씬에 빈 GameObject → 이 컴포넌트 추가 (또는 카메라에 직접 추가).
///   2) Intensity 슬라이더를 움직여 강도 확인. Look 값(색·워프·샤프트 등)으로 느낌 튜닝.
///   3) 플레이 중 G키(기본)로 0↔1 토글.
///   4) 느낌 확정되면 값들을 DenseAnomalyController/AnomalyFog로 이식(또는 이 셰이더로 교체).
///
/// ⚠️ 프로토용. 플레이 중 DenseAnomalyController(현상 강도>0)와 동시에 켜지면 이중으로 겹칠 수 있음
///    — 비교 시엔 둘 중 하나만 활성화.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class AnomalyHazePrototype : MonoBehaviour
{
    [Header("강도")]
    [Range(0, 1)] public float intensity = 0.7f;
    [Tooltip("강도 변화 속도(초당). 0이면 즉시.")]
    public float lerpSpeed = 0.8f;

    [Header("Look — 색 (병든 초록 프리셋)")]
    public Color fogColor = new(0.035f, 0.070f, 0.045f, 1f);   // 어두운 병든 초록-검정
    [Tooltip("빛결/샤프트에 스미는 병든 빛색(독성 초록).")]
    public Color lightColor = new(0.30f, 0.52f, 0.26f, 1f);    // 독성 형광 초록

    [Header("Look — 안개")]
    [Range(0, 1)] public float hazeStrength = 0.94f;   // 최대 불투명
    [Range(0, 1)] public float coverage = 0.6f;        // 덮는 정도(하한) — 병든 톤이라 살짝 짙게
    [Range(0.5f, 8f)] public float scale = 2.6f;       // 결 크기
    [Range(0, 1)] public float drift = 0.02f;          // 흐름 속도 — 느리게(끈적한 침식감)
    [Range(0, 2f)] public float warp = 1.0f;           // 뭉게짐 — 살짝 더 유동적
    [Range(0, 1)] public float depthBias = 0.6f;       // 멀수록 짙게

    [Header("Look — 빛")]
    [Range(0, 1)] public float shaftStrength = 0.35f;  // 광선결
    [Range(0, 360)] public float shaftAngle = 115f;    // 광선 방향
    [Range(0, 1)] public float topLight = 0.25f;       // 상단 소프트광

    [Header("Look — 플레이어 클리어")]
    [Range(0, 1.5f)] public float nearClear = 0.28f;   // 클리어 거리
    [Range(0.3f, 3f)] public float farFull = 1.5f;     // 완전안개 거리
    [Tooltip("플레이어(Tag=Player) 위치를 클리어 중심으로. 없으면 화면 중앙.")]
    public bool centerOnPlayer = true;

    [Header("Debug")]
    public KeyCode toggleKey = KeyCode.G;

    float _current;
    Camera _cam;
    Transform _player;
    GameObject _quad;
    Material _mat;
    static Sprite _sprite;

    static readonly int IdFogColor = Shader.PropertyToID("_FogColor");
    static readonly int IdLightColor = Shader.PropertyToID("_LightColor");
    static readonly int IdDensity = Shader.PropertyToID("_Density");
    static readonly int IdHaze = Shader.PropertyToID("_HazeStrength");
    static readonly int IdCoverage = Shader.PropertyToID("_Coverage");
    static readonly int IdScale = Shader.PropertyToID("_Scale");
    static readonly int IdSpeed = Shader.PropertyToID("_Speed");
    static readonly int IdWarp = Shader.PropertyToID("_Warp");
    static readonly int IdShaft = Shader.PropertyToID("_ShaftStrength");
    static readonly int IdShaftAngle = Shader.PropertyToID("_ShaftAngle");
    static readonly int IdDepthBias = Shader.PropertyToID("_DepthBias");
    static readonly int IdTopLight = Shader.PropertyToID("_TopLight");
    static readonly int IdNearClear = Shader.PropertyToID("_NearClear");
    static readonly int IdFarFull = Shader.PropertyToID("_FarFull");
    static readonly int IdClearCenter = Shader.PropertyToID("_ClearCenter");
    static readonly int IdAspect = Shader.PropertyToID("_Aspect");
    static readonly int IdWorldOffset = Shader.PropertyToID("_WorldOffset");

    void OnEnable() { _current = intensity; }
    void OnDisable() => Cleanup();
    void OnDestroy() => Cleanup();

    void LateUpdate()
    {
        EnsureQuad();
        if (_cam == null || _quad == null || _mat == null) return;

        if (Application.isPlaying && GameInput.GetKeyDown(toggleKey))
            intensity = intensity > 0.5f ? 0f : 1f;

        float dt = Application.isPlaying ? Time.unscaledDeltaTime : 0.016f;
        _current = lerpSpeed > 0f ? Mathf.MoveTowards(_current, intensity, lerpSpeed * dt) : intensity;

        FitQuad();

        _mat.SetColor(IdFogColor, fogColor);
        _mat.SetColor(IdLightColor, lightColor);
        _mat.SetFloat(IdDensity, _current);
        _mat.SetFloat(IdHaze, hazeStrength);
        _mat.SetFloat(IdCoverage, coverage);
        _mat.SetFloat(IdScale, scale);
        _mat.SetFloat(IdSpeed, drift);
        _mat.SetFloat(IdWarp, warp);
        _mat.SetFloat(IdShaft, shaftStrength);
        _mat.SetFloat(IdShaftAngle, shaftAngle);
        _mat.SetFloat(IdDepthBias, depthBias);
        _mat.SetFloat(IdTopLight, topLight);
        _mat.SetFloat(IdNearClear, nearClear);
        _mat.SetFloat(IdFarFull, farFull);
        _mat.SetFloat(IdAspect, _cam.aspect);

        Vector2 cc = new(0.5f, 0.5f);
        var pl = centerOnPlayer ? GetPlayer() : null;
        if (pl != null) { Vector3 vp = _cam.WorldToViewportPoint(pl.position); cc = new Vector2(vp.x, vp.y); }
        _mat.SetVector(IdClearCenter, new Vector4(cc.x, cc.y, 0, 0));

        Vector3 cp = _cam.transform.position;
        _mat.SetVector(IdWorldOffset, new Vector4(cp.x, cp.y, 0, 0));

#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.SceneView.RepaintAll(); // 에디트 모드에서도 애니메이션 미리보기
#endif
    }

    Transform GetPlayer()
    {
        if (_player != null) return _player;
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) _player = go.transform;
        return _player;
    }

    void EnsureQuad()
    {
        if (_cam == null || !_cam.isActiveAndEnabled) _cam = Camera.main;
        if (_cam == null) return;
        if (_quad != null && _quad.transform.parent == _cam.transform) return;

        Cleanup();
        var shader = Shader.Find("BRB/AnomalyHazePrototype");
        if (shader == null) { Debug.LogWarning("[AnomalyHazeProto] BRB/AnomalyHazePrototype 셰이더 못 찾음."); return; }

        _mat = new Material(shader) { hideFlags = HideFlags.DontSave };
        _quad = new GameObject("AnomalyHazeProtoQuad") { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
        _quad.transform.SetParent(_cam.transform, false);

        var sr = _quad.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite();
        sr.sharedMaterial = _mat;
        var layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) sr.sortingLayerID = layers[layers.Length - 1].id;
        sr.sortingOrder = 30050; // 프로덕션 안개(30000)보다 살짝 위 — 프로토가 항상 보이게
    }

    void FitQuad()
    {
        if (_cam == null || _quad == null) return;
        float h, w;
        if (_cam.orthographic) { h = _cam.orthographicSize * 2f; w = h * _cam.aspect; }
        else { float dist = _cam.nearClipPlane + 0.1f; h = 2f * dist * Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad); w = h * _cam.aspect; }
        _quad.transform.localPosition = new Vector3(0f, 0f, _cam.nearClipPlane + 0.05f);
        _quad.transform.localRotation = Quaternion.identity;
        _quad.transform.localScale = new Vector3(w, h, 1f);
    }

    static Sprite GetSprite()
    {
        if (_sprite != null) return _sprite;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "HazeProtoTex", hideFlags = HideFlags.DontSave };
        tex.SetPixel(0, 0, Color.white); tex.Apply();
        _sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        _sprite.name = "HazeProtoSprite";
        _sprite.hideFlags = HideFlags.DontSave;
        return _sprite;
    }

    void Cleanup()
    {
        if (_quad != null) { if (Application.isPlaying) Destroy(_quad); else DestroyImmediate(_quad); }
        if (_mat != null) { if (Application.isPlaying) Destroy(_mat); else DestroyImmediate(_mat); }
        _quad = null; _mat = null;
    }
}
