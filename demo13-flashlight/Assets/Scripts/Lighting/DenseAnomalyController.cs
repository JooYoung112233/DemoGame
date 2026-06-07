using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 짙은 현상(시간 무관 침식) 컨트롤러 — 낮/밤과 완전 독립.
/// 카메라 앞에 풀스크린 fog 쿼드(BRB/AnomalyFog)를 만들어 화면을 직접 어둡게+안개로 덮는다.
///   → DayNightCycle의 글로벌 Light2D를 건드리지 않으므로 충돌 없음(최종 화면 = 낮밤 × 현상 느낌).
/// 강도(intensity) = 수동/이벤트 + 지역 기본 침식도(WorldRegionCatalog.anomaly) 중 큰 값. 부드럽게 lerp.
/// 싱글톤(DontDestroyOnLoad). Systems 씬에 두거나 미설치 시 자동 부트스트랩.
/// </summary>
public class DenseAnomalyController : MonoBehaviour
{
    public static DenseAnomalyController Instance { get; private set; }

    [Header("강도")]
    [Tooltip("수동/이벤트 강도(0~1). 지역 기본 침식도와 max로 합쳐짐.")]
    [Range(0, 1)] public float intensity = 0f;
    [Tooltip("활성 지역의 WorldRegionCatalog.anomaly를 기본 침식도로 반영.")]
    public bool useRegionBaseline = true;
    [Tooltip("강도 변화 속도(초당).")]
    public float lerpSpeed = 0.8f;

    [Header("Look")]
    [Tooltip("어두운 안개 색.")]
    public Color fogColor = new(0.06f, 0.07f, 0.10f, 1f);
    [Tooltip("안개 최대 불투명도.")]
    [Range(0, 1)] public float hazeStrength = 0.9f;
    [Tooltip("플레이어 주변 클리어 거리(이 안은 안개 옅음).")]
    [Range(0, 1.5f)] public float nearClear = 0.25f;
    [Tooltip("완전 안개까지 거리 — 클수록 더 점진적(넓은 그라데이션).")]
    [Range(0.3f, 3f)] public float farFull = 1.4f;

    [Header("Debug")]
    [Tooltip("테스트용 토글 키(현상 0↔1).")]
    public KeyCode debugKey = KeyCode.G;

    float _current;
    Camera _cam;
    Transform _player;
    GameObject _quad;
    Material _mat;
    static Sprite _sprite;

    // 구간(zone) — 플레이어가 들어간 현상 구역들. 가장 큰 값이 목표 강도에 반영.
    readonly HashSet<DenseAnomalyZone> _zones = new();
    public void AddZone(DenseAnomalyZone z) { if (z != null) _zones.Add(z); }
    public void RemoveZone(DenseAnomalyZone z) { _zones.Remove(z); }

    static readonly int IdDensity = Shader.PropertyToID("_Density");
    static readonly int IdWorldOffset = Shader.PropertyToID("_WorldOffset");
    static readonly int IdFogColor = Shader.PropertyToID("_FogColor");
    static readonly int IdHaze = Shader.PropertyToID("_HazeStrength");
    static readonly int IdNearClear = Shader.PropertyToID("_NearClear");
    static readonly int IdFarFull = Shader.PropertyToID("_FarFull");
    static readonly int IdAspect = Shader.PropertyToID("_Aspect");
    static readonly int IdClearCenter = Shader.PropertyToID("_ClearCenter");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        if (FindFirstObjectByType<DenseAnomalyController>(FindObjectsInactive.Include) != null) return;
        var go = new GameObject("[DenseAnomaly]");
        go.AddComponent<DenseAnomalyController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>현상 강도 설정(이벤트/스토리/지역 진입에서 호출). 0=맑음, 1=완전 침식.</summary>
    public void SetIntensity(float v) => intensity = Mathf.Clamp01(v);

    void LateUpdate()
    {
        EnsureQuad();
        if (_cam == null || _quad == null || _mat == null) return;

        // 목표 강도 = 수동 + 지역 기본 침식도 + 들어간 구간(zone) 중 가장 큰 값
        float target = Mathf.Clamp01(intensity);
        if (useRegionBaseline && RegionTimeManager.Instance != null)
        {
            var def = WorldRegionCatalog.GetById(RegionTimeManager.Instance.ActiveRegionId);
            if (def.HasValue) target = Mathf.Max(target, def.Value.anomaly);
        }
        foreach (var z in _zones)
            if (z != null) target = Mathf.Max(target, z.intensity);

        _current = Mathf.MoveTowards(_current, target, lerpSpeed * Time.unscaledDeltaTime);

        FitQuad();
        _mat.SetFloat(IdDensity, _current);
        _mat.SetColor(IdFogColor, fogColor);
        _mat.SetFloat(IdHaze, hazeStrength);
        _mat.SetFloat(IdNearClear, nearClear);
        _mat.SetFloat(IdFarFull, farFull);
        _mat.SetFloat(IdAspect, _cam.aspect);

        // 클리어 버블 중심 = 플레이어 화면 위치(없으면 화면 중앙) → 내 뷰만 잘 보이게
        Vector2 cc = new(0.5f, 0.5f);
        var pl = GetPlayer();
        if (pl != null)
        {
            Vector3 vp = _cam.WorldToViewportPoint(pl.position);
            cc = new Vector2(vp.x, vp.y);
        }
        _mat.SetVector(IdClearCenter, new Vector4(cc.x, cc.y, 0, 0));

        Vector3 cp = _cam.transform.position;
        _mat.SetVector(IdWorldOffset, new Vector4(cp.x, cp.y, 0, 0));

        if (Input.GetKeyDown(debugKey))
            intensity = intensity > 0.5f ? 0f : 1f;
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
        var shader = Shader.Find("BRB/AnomalyFog");
        if (shader == null) { Debug.LogWarning("[DenseAnomaly] BRB/AnomalyFog 셰이더 못 찾음."); return; }

        _mat = new Material(shader);
        _quad = new GameObject("AnomalyFogQuad") { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
        _quad.transform.SetParent(_cam.transform, false);

        // SpriteRenderer 사용(URP 2D 렌더러 확실 렌더 — MeshRenderer는 2D에서 불확실).
        var sr = _quad.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite();
        sr.sharedMaterial = _mat;
        // 최상단 Sorting Layer + 매우 높은 Order로 월드 스프라이트 위에(UI는 별도 캔버스라 그대로 위).
        var layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) sr.sortingLayerID = layers[layers.Length - 1].id;
        sr.sortingOrder = 30000;
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
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "AnomalyFogTex", hideFlags = HideFlags.DontSave };
        tex.SetPixel(0, 0, Color.white); tex.Apply();
        _sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);  // PPU=1 → 1 유닛(중앙 피벗)
        _sprite.name = "AnomalyFogSprite";
        _sprite.hideFlags = HideFlags.DontSave;
        return _sprite;
    }

    void Cleanup()
    {
        if (_quad != null) Destroy(_quad);
        if (_mat != null) Destroy(_mat);
        _quad = null; _mat = null;
    }

    void OnDestroy() { Cleanup(); if (Instance == this) Instance = null; }
}
