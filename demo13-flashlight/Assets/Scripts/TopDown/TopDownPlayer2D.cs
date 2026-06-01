using UnityEngine;

/// <summary>
/// 탑다운 2D 플레이어 이동 — WASD(8방향) + 마우스 페이싱(손전등/방향용).
/// 필요 컴포넌트: Rigidbody2D, Collider2D(예: CapsuleCollider2D), SpriteRenderer.
/// 아이소 좌표 변환 없음 — 입력이 곧 화면 방향(탑다운).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TopDownPlayer2D : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] bool flipByMouse = true;

    /// <summary>마우스 기준 바라보는 방향(정규화). 손전등 등에서 참조.</summary>
    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    Rigidbody2D _rb;
    Camera _cam;
    Vector2 _input;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        _cam = Camera.main;
    }

    void Update()
    {
        _input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (_input.sqrMagnitude > 1f) _input.Normalize();

        if (_cam == null) _cam = Camera.main;
        if (_cam != null)
        {
            Vector3 m = _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 d = (Vector2)(m - transform.position);
            if (d.sqrMagnitude > 0.0001f) FacingDirection = d.normalized;
        }

        if (flipByMouse && sprite != null && Mathf.Abs(FacingDirection.x) > 0.01f)
            sprite.flipX = FacingDirection.x < 0f;
    }

    void FixedUpdate()
    {
        _rb.linearVelocity = _input * moveSpeed; // Unity 6: linearVelocity
    }
}
