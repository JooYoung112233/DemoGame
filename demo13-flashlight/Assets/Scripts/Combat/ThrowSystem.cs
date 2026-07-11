using UnityEngine;

/// <summary>
/// 투척물 시스템 (1차 = 돌, 유인 전용). (docs/combat.md 투척물 2026-07-10)
///   • G키(돌 보유·안전구역 아님·모달 UI 없음) → 조준 모드: 사거리 원 + 커서 착탄 마커(사거리 밖=경계로 클램프).
///   • 좌클릭 → 착탄 지점으로 돌 투척(짧은 비행) → 착탄 시 PlayerNoise.Pulse(소음) → 반경 내 적 조사 이동. 데미지 0.
///   • 우클릭/ESC → 취소. 던지면 돌 1개 소모.
/// 자가 부트스트랩(PlayerNoise 패턴, DontDestroyOnLoad). 조준값은 GameTuning + traits.
/// </summary>
public class ThrowSystem : MonoBehaviour
{
    public static ThrowSystem Instance { get; private set; }

    /// <summary>투척물 아이템 id(1차 = 돌). ItemData.isThrowable 아이템 중 이 id를 던진다.</summary>
    public const string ThrowItemId = "stone";

    /// <summary>조준 중인가(다른 시스템이 좌클릭 공격을 막을 때 참조).</summary>
    public bool IsAiming { get; private set; }

    TopDownPlayer player;
    PlayerInventory inv;
    string _aimItemId = ThrowItemId;   // 현재 조준 중인 투척물 id (G=기본 돌, 퀵슬롯=지정 아이템)

    Transform rangeRing, marker;
    SpriteRenderer rangeSr, markerSr;
    static Sprite _ringSprite, _markerSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;
        if (Instance != null) return;
        var go = new GameObject("[ThrowSystem]");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<ThrowSystem>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildAimVisuals();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (player == null || inv == null)
        {
            player = TopDownPlayer.Instance;
            inv = player != null ? player.GetComponent<PlayerInventory>() : null;
        }
        if (player == null || inv == null) { if (IsAiming) CancelAim(); return; }

        bool safe  = UIManager.Instance != null && UIManager.Instance.IsSafehouse;
        bool modal = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();

        if (!IsAiming)
        {
            // 조준 진입: G키(기본 돌). 퀵슬롯 사용도 TryEnterAim로 진입(게이트/보유 체크는 그 안에서).
            if (GameInput.GetKeyDown(KeyCode.G)) TryEnterAim(ThrowItemId);
            return;
        }

        // 조준 중 — 안전구역 진입/모달 오픈/ESC/우클릭이면 취소.
        if (safe || modal || GameInput.GetKeyDown(KeyCode.Escape) || GameInput.GetMouseButtonDown(1))
        {
            CancelAim();
            return;
        }

        Vector2 land = ClampedLandingPoint();
        UpdateAimVisuals(land);

        if (GameInput.GetMouseButtonDown(0))
            Throw(land);
    }

    void EnterAim() { IsAiming = true; SetVisualsActive(true); }

    void CancelAim() { IsAiming = false; SetVisualsActive(false); }

    /// <summary>조준 모드 진입 시도(G키·퀵슬롯 공용). itemId=null이면 기본 투척물(돌). 성공하면 true.</summary>
    public bool TryEnterAim(string itemId = null)
    {
        if (IsAiming) return false;
        if (player == null || inv == null)
        {
            player = TopDownPlayer.Instance;
            inv = player != null ? player.GetComponent<PlayerInventory>() : null;
        }
        if (player == null || inv == null) return false;

        bool safe  = UIManager.Instance != null && UIManager.Instance.IsSafehouse;
        bool modal = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();
        if (safe || modal) return false;

        string id = string.IsNullOrEmpty(itemId) ? ThrowItemId : itemId;
        if (inv.CountItemAll(id) <= 0) return false;

        _aimItemId = id;
        EnterAim();
        return true;
    }

    /// <summary>플레이어 기준 커서 방향 착탄점 — 사거리 밖이면 원 경계로 클램프.</summary>
    Vector2 ClampedLandingPoint()
    {
        var gt = GameTuning.Instance;
        float range = gt != null ? gt.throwRange : 8f;
        Vector2 origin = player.transform.position;
        Vector2 d = (Vector2)player.MouseWorldPos - origin;
        if (d.sqrMagnitude > range * range) d = d.normalized * range;
        return origin + d;
    }

    void Throw(Vector2 land)
    {
        // 투척물 1개 소모 실패(방금 소진 등) → 조준만 종료.
        if (!inv.ConsumeItemAll(_aimItemId, 1)) { CancelAim(); return; }

        var gt = GameTuning.Instance;
        float speed  = gt != null ? gt.throwSpeed       : 10f;
        float noiseR = gt != null ? gt.throwNoiseRadius : 9f;
        // 비행 시간 = 거리/속도(0.15~1.0s 클램프) → 거리와 무관하게 일정 속도(멀수록 오래 = 눈에 보이는 포물선).
        float flight = Mathf.Clamp(Vector2.Distance(player.transform.position, land) / Mathf.Max(1f, speed), 0.15f, 1.0f);
        SpawnStone(player.transform.position, land, flight, noiseR);
        CancelAim();
    }

    static void SpawnStone(Vector2 from, Vector2 to, float flight, float noiseRadius)
    {
        var go = new GameObject("ThrownStone");
        go.transform.position = from;
        go.transform.localScale = Vector3.one * 0.28f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MarkerSprite();
        sr.color = new Color(0.72f, 0.68f, 0.60f, 1f);
        sr.sortingOrder = 60;
        go.AddComponent<ThrownStone>().Init(from, to, flight, noiseRadius);
    }

    // ── 조준 비주얼 (사거리 원 + 착탄 마커) ───────────────────────────
    void BuildAimVisuals()
    {
        var ringGo = new GameObject("ThrowRange");
        ringGo.transform.SetParent(transform, false);   // ThrowSystem(DontDestroyOnLoad) 자식 → 씬 전환에도 영속
        rangeRing = ringGo.transform;
        rangeSr = ringGo.AddComponent<SpriteRenderer>();
        rangeSr.sprite = RingSprite();
        rangeSr.color = new Color(0.55f, 0.9f, 1f, 0.35f);
        rangeSr.sortingOrder = -5;   // 바닥 위, 캐릭터 아래(소음 링과 동일 층)
        ringGo.SetActive(false);

        var markGo = new GameObject("ThrowMarker");
        markGo.transform.SetParent(transform, false);   // 영속(씬 전환 안전)
        marker = markGo.transform;
        markGo.transform.localScale = Vector3.one * 0.7f;
        markerSr = markGo.AddComponent<SpriteRenderer>();
        markerSr.sprite = MarkerSprite();
        markerSr.color = new Color(1f, 0.85f, 0.3f, 0.95f);
        markerSr.sortingOrder = 55;
        markGo.SetActive(false);
    }

    void SetVisualsActive(bool on)
    {
        if (rangeRing != null && rangeRing.gameObject.activeSelf != on) rangeRing.gameObject.SetActive(on);
        if (marker != null && marker.gameObject.activeSelf != on) marker.gameObject.SetActive(on);
    }

    void UpdateAimVisuals(Vector2 land)
    {
        var gt = GameTuning.Instance;
        float range = gt != null ? gt.throwRange : 8f;
        if (rangeRing != null)
        {
            rangeRing.position = player.transform.position;
            rangeRing.localScale = Vector3.one * (range * 2f);   // 스프라이트 지름 1 기준 → 반경×2
        }
        if (marker != null) marker.position = land;
    }

    // ── 절차적 스프라이트 ─────────────────────────────────────────────
    /// <summary>사거리 원 — 얇은 외곽 링.</summary>
    static Sprite RingSprite()
    {
        if (_ringSprite != null) return _ringSprite;
        const int R = 64;
        var tex = new Texture2D(R * 2, R * 2, TextureFormat.RGBA32, false);
        var px = new Color[R * 2 * R * 2];
        for (int y = 0; y < R * 2; y++)
            for (int x = 0; x < R * 2; x++)
            {
                float d = Mathf.Sqrt((x - R) * (x - R) + (y - R) * (y - R)) / R;
                float a = (d > 0.90f && d <= 1f) ? 1f : 0f;   // 외곽 링 밴드만
                px[y * R * 2 + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px); tex.Apply();
        _ringSprite = Sprite.Create(tex, new Rect(0, 0, R * 2, R * 2), new Vector2(0.5f, 0.5f), R * 2);
        return _ringSprite;
    }

    /// <summary>착탄 마커/돌 — 레티클(중앙 점 + 외곽 링).</summary>
    static Sprite MarkerSprite()
    {
        if (_markerSprite != null) return _markerSprite;
        const int R = 32;
        var tex = new Texture2D(R * 2, R * 2, TextureFormat.RGBA32, false);
        var px = new Color[R * 2 * R * 2];
        for (int y = 0; y < R * 2; y++)
            for (int x = 0; x < R * 2; x++)
            {
                float d = Mathf.Sqrt((x - R) * (x - R) + (y - R) * (y - R)) / R;
                float a = (d < 0.30f || (d > 0.72f && d <= 1f)) ? 1f : 0f;
                px[y * R * 2 + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px); tex.Apply();
        _markerSprite = Sprite.Create(tex, new Rect(0, 0, R * 2, R * 2), new Vector2(0.5f, 0.5f), R * 2);
        return _markerSprite;
    }
}

/// <summary>던져진 돌 — from→to로 포물선 비행 후 착탄 시 소음 펄스 발생.</summary>
public class ThrownStone : MonoBehaviour
{
    Vector2 _from, _to;
    float _dur, _t, _noiseRadius;

    public void Init(Vector2 from, Vector2 to, float dur, float noiseRadius)
    {
        _from = from; _to = to; _dur = Mathf.Max(0.05f, dur); _noiseRadius = noiseRadius;
    }

    void Update()
    {
        _t += Time.deltaTime / _dur;
        if (_t >= 1f)
        {
            transform.position = _to;
            PlayerNoise.Pulse(_to, _noiseRadius);   // 착탄 소음 → 반경 내 적 조사
            Destroy(gameObject);
            return;
        }
        // 수평 보간 + 작은 포물선 호(연출)
        Vector2 flat = Vector2.Lerp(_from, _to, _t);
        float hop = Mathf.Sin(_t * Mathf.PI) * 0.6f;
        transform.position = new Vector3(flat.x, flat.y + hop, 0f);
    }
}
