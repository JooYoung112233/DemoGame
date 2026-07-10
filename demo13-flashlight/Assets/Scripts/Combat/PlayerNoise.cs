using UnityEngine;

/// <summary>
/// 플레이어 소음 발생기 + 발밑 링 VFX. (docs/combat.md 소음 시스템 2026-07-10)
///   • 매 프레임 이동 상태(웅크림/걷기/달리기/정지)로 지속 소음 반경 계산 → NoiseSystem에 등록.
///     반경 = GameTuning 행동별 값 × 특성 `move_noise`(고양이걸음 −30% / 무거운발 +25%).
///   • 발밑에 반투명 원(반경 = 소음 반경)을 그려 "지금 내 소음 범위"를 시각화(월드).
///   • 타격·문 등 순간 펄스는 `PlayerNoise.Pulse(...)` 정적 헬퍼로 발생(파문 VFX 동반).
/// 자가 부트스트랩(부팅 시 생성, DontDestroyOnLoad). 안전가옥에선 무음(면제).
/// </summary>
public class PlayerNoise : MonoBehaviour
{
    public static PlayerNoise Instance { get; private set; }

    TopDownPlayer player;
    Transform ring;                 // 발밑 링(월드 스프라이트)
    SpriteRenderer ringSr;
    static Sprite _discSprite;

    float _shownRadius;             // 표시용 스무딩된 반경
    float _lastPulseRadius;         // UI 레벨에 잠깐 반영할 최근 펄스
    float _pulseFade;

    /// <summary>UI용 소음 레벨(0~1) = max(지속, 최근 펄스) / noiseUiMax.</summary>
    public float Level01
    {
        get
        {
            var gt = GameTuning.Instance;
            float uiMax = gt != null ? gt.noiseUiMax : 14f;
            float r = Mathf.Max(NoiseSystem.PlayerSustainedRadius, _lastPulseRadius * _pulseFade);
            return uiMax > 0f ? Mathf.Clamp01(r / uiMax) : 0f;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;
        if (Instance != null) return;
        var go = new GameObject("[PlayerNoise]");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<PlayerNoise>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildRing();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (player == null) player = TopDownPlayer.Instance;
        var gt = GameTuning.Instance;

        // 안전가옥 허브는 무음(무게 페널티와 동일 면제).
        bool active = player != null && (UIManager.Instance == null || !UIManager.Instance.IsSafehouse);

        float target = 0f;
        if (active)
        {
            float baseR;
            if (!player.IsMoving)        baseR = gt != null ? gt.noiseIdle   : 0f;
            else if (player.IsCrouching) baseR = gt != null ? gt.noiseCrouch : 1.5f;
            else if (player.IsSprinting) baseR = gt != null ? gt.noiseRun    : 11f;
            else                         baseR = gt != null ? gt.noiseWalk   : 5f;
            target = baseR * TraitManager.Mod("move_noise");
        }

        NoiseSystem.SetPlayerSustained(player != null ? (Vector2)player.transform.position : Vector2.zero, target);

        // 펄스 UI 페이드
        if (_pulseFade > 0f)
        {
            float dur = gt != null ? gt.noisePulseDuration : 0.6f;
            _pulseFade -= Time.deltaTime / Mathf.Max(0.1f, dur);
            if (_pulseFade < 0f) _pulseFade = 0f;
        }

        UpdateRing(active, target);
    }

    /// <summary>순간 소음 펄스 — 타격/문 등. NoiseSystem 등록 + 파문 VFX. type 없이 반경만.</summary>
    public static void Pulse(Vector2 pos, float radius)
    {
        if (radius <= 0f) return;
        var gt = GameTuning.Instance;
        NoiseSystem.ReportPulse(pos, radius, gt != null ? gt.noisePulseDuration : 0.6f);
        if (Instance != null) { Instance._lastPulseRadius = radius; Instance._pulseFade = 1f; }
        SpawnRipple(pos, radius);
    }

    /// <summary>타격 소음(플레이어 위치). TopDownPlayer 공격 시작에서 호출.</summary>
    public static void AttackNoise()
    {
        var p = TopDownPlayer.Instance; var gt = GameTuning.Instance;
        if (p == null) return;
        Pulse(p.transform.position, (gt != null ? gt.noiseAttack : 14f) * TraitManager.Mod("move_noise"));
    }

    /// <summary>문/셔터 소음(문 위치).</summary>
    public static void DoorNoise(Vector2 pos)
    {
        var gt = GameTuning.Instance;
        Pulse(pos, gt != null ? gt.noiseDoor : 8f);
    }

    // ── VFX ────────────────────────────────────────────────
    void BuildRing()
    {
        var go = new GameObject("NoiseRing");
        ring = go.transform;
        ringSr = go.AddComponent<SpriteRenderer>();
        ringSr.sprite = DiscSprite();
        ringSr.color = new Color(1f, 0.9f, 0.4f, 0f);
        ringSr.sortingOrder = -5;   // 바닥 위, 캐릭터 아래
        go.SetActive(false);
    }

    void UpdateRing(bool active, float radius)
    {
        if (ring == null) return;
        _shownRadius = Mathf.Lerp(_shownRadius, radius, Time.deltaTime * 10f);
        bool show = active && _shownRadius > 0.2f && player != null;
        if (ring.gameObject.activeSelf != show) ring.gameObject.SetActive(show);
        if (!show) return;
        ring.position = player.transform.position;
        ring.localScale = Vector3.one * (_shownRadius * 2f);   // 스프라이트 지름 1 기준 → 반경×2
        float a = 0.10f + 0.12f * Level01;                     // 시끄러울수록 진하게
        ringSr.color = new Color(1f, 0.9f, 0.4f, a);
    }

    static void SpawnRipple(Vector2 pos, float radius)
    {
        var go = new GameObject("NoiseRipple");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DiscSprite();
        sr.color = new Color(1f, 0.85f, 0.35f, 0.5f);
        sr.sortingOrder = -4;
        go.AddComponent<NoiseRipple>().Init(radius);
    }

    static Sprite DiscSprite()
    {
        if (_discSprite != null) return _discSprite;
        const int R = 64;
        var tex = new Texture2D(R * 2, R * 2, TextureFormat.RGBA32, false);
        var px = new Color[R * 2 * R * 2];
        for (int y = 0; y < R * 2; y++)
            for (int x = 0; x < R * 2; x++)
            {
                float d = Mathf.Sqrt((x - R) * (x - R) + (y - R) * (y - R)) / R;
                // 가장자리 링 강조 + 안쪽 옅게(도넛형 느낌)
                float a = d > 1f ? 0f : Mathf.Lerp(0.25f, 1f, Mathf.SmoothStep(0f, 1f, d));
                px[y * R * 2 + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px); tex.Apply();
        _discSprite = Sprite.Create(tex, new Rect(0, 0, R * 2, R * 2), new Vector2(0.5f, 0.5f), R * 2);
        return _discSprite;
    }
}

/// <summary>파문 링 — 잠깐 커지며 사라진다.</summary>
public class NoiseRipple : MonoBehaviour
{
    float _radius, _t;
    SpriteRenderer _sr;

    public void Init(float radius) { _radius = radius; _sr = GetComponent<SpriteRenderer>(); }

    void Update()
    {
        _t += Time.deltaTime * 2.2f;   // ~0.45s 수명
        if (_t >= 1f) { Destroy(gameObject); return; }
        float scale = Mathf.Lerp(_radius * 0.4f, _radius * 2f, _t);
        transform.localScale = Vector3.one * scale;
        if (_sr != null) _sr.color = new Color(1f, 0.85f, 0.35f, 0.5f * (1f - _t));
    }
}
