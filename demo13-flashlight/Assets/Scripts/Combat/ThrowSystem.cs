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
        // ⚠️ Vector3 → Vector2 암묵 변환은 (x, **높이**)를 읽는다. 컴파일도 되고 에러도 없지만
        //    착탄점 계산이 통째로 틀린 평면에서 돈다. 평면 좌표는 반드시 Plan3D.ToPlan을 거친다.
        Vector2 origin = Plan3D.ToPlan(player.transform.position);
        Vector2 d = Plan3D.ToPlan(player.MouseWorldPos) - origin;
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
        Vector2 origin = Plan3D.ToPlan(player.transform.position);
        float flight = Mathf.Clamp(Vector2.Distance(origin, land) / Mathf.Max(1f, speed), 0.15f, 1.0f);
        SpawnStone(origin, land, flight, noiseR);
        CancelAim();
    }

    static void SpawnStone(Vector2 from, Vector2 to, float flight, float noiseRadius)
    {
        // ⚠️ `transform.position = from`(Vector2)은 3D에서 **z 대신 y로 들어간다** —
        //    돌이 지면이 아니라 공중/지하로 날아간다. 평면→월드 변환은 Plan3D를 거친다.
        var go = new GameObject("ThrownStone");
        go.transform.position = Plan3D.ToWorld(from, ThrownStone.CarryY);
        GreyboxMesh.Box(go.transform, "Visual", Vector3.zero,
                        Vector3.one * 0.16f, new Color(0.72f, 0.68f, 0.60f, 1f), castShadow: false);
        go.AddComponent<ThrownStone>().Init(from, to, flight, noiseRadius);
    }

    // ── 조준 비주얼 (사거리 원 + 착탄 마커) ───────────────────────────

    /// <summary>지면 표식을 띄우는 높이(m). 0이면 바닥과 z-fighting으로 지글거린다.</summary>
    const float GroundDecalY = 0.03f;

    /// <summary>지면에 눕히는 회전. 스프라이트는 기본적으로 XY 평면(수직)이라
    /// 그대로 두면 조준 원과 착탄 표식이 **벽처럼 서 있다** — 실제로 그 상태였다.
    /// x축 +90°로 눕혀야 바닥 데칼로 읽힌다.</summary>
    static readonly Quaternion GroundDecal = Quaternion.Euler(90f, 0f, 0f);

    void BuildAimVisuals()
    {
        var ringGo = new GameObject("ThrowRange");
        ringGo.transform.SetParent(transform, false);   // ThrowSystem(DontDestroyOnLoad) 자식 → 씬 전환에도 영속
        rangeRing = ringGo.transform;
        rangeSr = ringGo.AddComponent<SpriteRenderer>();
        rangeSr.sprite = RingSprite();
        rangeSr.color = new Color(0.55f, 0.9f, 1f, 0.35f);
        rangeSr.sortingOrder = -5;   // 바닥 위, 캐릭터 아래(소음 링과 동일 층)
        ringGo.transform.rotation = GroundDecal;
        ringGo.SetActive(false);

        var markGo = new GameObject("ThrowMarker");
        markGo.transform.SetParent(transform, false);   // 영속(씬 전환 안전)
        marker = markGo.transform;
        markGo.transform.localScale = Vector3.one * 0.7f;
        markerSr = markGo.AddComponent<SpriteRenderer>();
        markerSr.sprite = MarkerSprite();
        markerSr.color = new Color(1f, 0.85f, 0.3f, 0.95f);
        markerSr.sortingOrder = 55;
        markGo.transform.rotation = GroundDecal;
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
            // 지면 바로 위에 깐다. z-fighting을 피할 만큼만 띄운다.
            var p = player.transform.position;
            rangeRing.position = new Vector3(p.x, p.y + GroundDecalY, p.z);
            rangeRing.localScale = Vector3.one * (range * 2f);   // 스프라이트 지름 1 기준 → 반경×2
        }
        if (marker != null) marker.position = Plan3D.ToWorld(land, GroundDecalY);
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
    /// <summary>손을 떠난 돌이 나는 기본 높이(m). 0이면 지면을 긁는다.</summary>
    public const float CarryY = 1.0f;
    /// <summary>포물선 호의 최고 추가 높이(m).</summary>
    const float ArcHeight = 1.2f;

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
            transform.position = Plan3D.ToWorld(_to);
            PlayerNoise.Pulse(_to, _noiseRadius);   // 착탄 소음 → 반경 내 적 조사
            Destroy(gameObject);
            return;
        }
        // 수평 보간 + 포물선 호. ⚠️ 2D에선 호가 화면 y였다 — 3D에서 그대로 두면
        //    돌이 **옆으로 휘어 날아간다.** 호는 높이(y), 이동은 평면(xz)이다.
        Vector2 flat = Vector2.Lerp(_from, _to, _t);
        float hop = Mathf.Sin(_t * Mathf.PI) * ArcHeight;
        transform.position = Plan3D.ToWorld(flat, CarryY + hop);
    }
}
