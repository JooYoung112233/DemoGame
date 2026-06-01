using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 탑다운 2D 플레이어 — WASD 8방향 이동 + 마우스 조준 + 2D 손전등.
/// Rigidbody2D 기반. NavMesh 없음.
/// 게임 로직(인벤/의료/퀘스트 등)은 별도 컴포넌트로 부착.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class TopDownPlayer : MonoBehaviour
{
    // ── 싱글톤 ──────────────────────────────────────────────────────
    public static TopDownPlayer Instance { get; private set; }

    // ── 인스펙터 ─────────────────────────────────────────────────────
    [Header("이동")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float sprintMultiplier = 1.6f;

    [Header("비주얼")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] bool flipByMouse = true;

    [Header("손전등")]
    [SerializeField] Transform flashlightPivot; // 자식 오브젝트 — Light2D 붙음

    // ── 퍼블릭 API ───────────────────────────────────────────────────
    public Vector2 FacingDirection { get; private set; } = Vector2.down;
    public Vector2 MoveDirection   { get; private set; }
    public Vector3 MouseWorldPos   { get; private set; }
    public bool    IsSprinting     { get; private set; }
    public bool    IsMoving => MoveDirection.sqrMagnitude > 0.01f;
    public bool    CanMove  { get; set; } = true;

    // ── 내부 ─────────────────────────────────────────────────────────
    Rigidbody2D _rb;
    Camera      _cam;

    // ── Unity 생명주기 ───────────────────────────────────────────────

    void Awake()
    {
        // 싱글톤
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _rb  = GetComponent<Rigidbody2D>();
        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _cam = Camera.main;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (_cam == null) _cam = Camera.main;
        UpdateMouseFacing();
        UpdateFlip();
        UpdateSprint();
        UpdateFlashlight();
    }

    void FixedUpdate()
    {
        if (!CanMove) { _rb.linearVelocity = Vector2.zero; return; }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        MoveDirection = new Vector2(h, v);
        if (MoveDirection.sqrMagnitude > 1f) MoveDirection.Normalize();

        float speed = moveSpeed * (IsSprinting ? sprintMultiplier : 1f);
        _rb.linearVelocity = MoveDirection * speed;
    }

    // ── 내부 메서드 ───────────────────────────────────────────────────

    void UpdateMouseFacing()
    {
        if (_cam == null) return;
        Vector3 m = _cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (Vector2)(m - transform.position);
        if (dir.sqrMagnitude > 0.01f)
        {
            FacingDirection = dir.normalized;
            MouseWorldPos   = m;
        }
    }

    void UpdateFlip()
    {
        if (!flipByMouse || spriteRenderer == null) return;
        if (Mathf.Abs(FacingDirection.x) > 0.01f)
            spriteRenderer.flipX = FacingDirection.x < 0f;
    }

    void UpdateSprint()
    {
        IsSprinting = Input.GetKey(KeyCode.LeftShift) && IsMoving;
    }

    void UpdateFlashlight()
    {
        if (flashlightPivot == null) return;
        float angle = Mathf.Atan2(FacingDirection.y, FacingDirection.x) * Mathf.Rad2Deg;
        flashlightPivot.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    // ── 스태미너 (스텁 — 향후 구현) ────────────────────────────────
    // 스태미너 시스템이 추가될 때까지 UI가 참조할 수 있도록 기본값 제공
    public float StaminaPercent   => 1f;
    public float StaminaCurrent   => 100f;
    public float StaminaMax       => 100f;
    public bool  IsExhausted      => false;

    // ── 외부 API (게임 로직 컴포넌트용) ─────────────────────────────

    /// <summary>외부에서 이동 활성/비활성 (UI 열림, 컷씬 등)</summary>
    public void SetCanMove(bool value) => CanMove = value;

    /// <summary>월드 좌표로 순간이동</summary>
    public void Teleport(Vector2 worldPos) => transform.position = worldPos;

    // ── 자동 스폰 ───────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;

        // 씬에 이미 있으면 스킵
        if (FindFirstObjectByType<TopDownPlayer>(FindObjectsInactive.Include) != null) return;

        var prefab = Resources.Load<GameObject>("TopDownPlayer");
        if (prefab != null)
        {
            var go = Instantiate(prefab);
            go.name = "TopDownPlayer";
            var sp = FindFirstObjectByType<SpawnPoint>();
            if (sp != null) go.transform.position = sp.transform.position;
        }
        else
        {
            Debug.LogWarning("[TopDownPlayer] Resources/TopDownPlayer 프리팹 없음. 씬에 직접 배치하거나 프리팹 생성 필요.");
        }
    }
}
