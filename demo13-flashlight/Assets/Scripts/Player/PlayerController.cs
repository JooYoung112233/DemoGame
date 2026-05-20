using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float crouchSpeedMultiplier = 0.4f;

    [Header("References")]
    [SerializeField] Transform flashlightPivot;

    Camera mainCam;
    Rigidbody2D rb;
    Vector2 moveInput;
    bool isCrouching;

    // 쿼터뷰 이동 보정 (화면 기준 WASD → 아이소 좌표)
    static readonly Vector2 ISO_UP = new Vector2(-0.5f, 0.25f).normalized;
    static readonly Vector2 ISO_DOWN = new Vector2(0.5f, -0.25f).normalized;
    static readonly Vector2 ISO_LEFT = new Vector2(-0.5f, -0.25f).normalized;
    static readonly Vector2 ISO_RIGHT = new Vector2(0.5f, 0.25f).normalized;

    public bool IsCrouching => isCrouching;
    public Vector2 FacingDirection { get; private set; } = Vector2.right;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
    }

    void Update()
    {
        // 쿼터뷰 방향으로 입력 변환
        moveInput = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) moveInput += ISO_UP;
        if (Input.GetKey(KeyCode.S)) moveInput += ISO_DOWN;
        if (Input.GetKey(KeyCode.A)) moveInput += ISO_LEFT;
        if (Input.GetKey(KeyCode.D)) moveInput += ISO_RIGHT;
        moveInput = moveInput.normalized;

        isCrouching = Input.GetKey(KeyCode.LeftControl);

        RotateTowardsMouse();
    }

    void FixedUpdate()
    {
        float speed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
        rb.linearVelocity = moveInput * speed;
    }

    void RotateTowardsMouse()
    {
        Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (mouseWorld - transform.position).normalized;
        if (dir.sqrMagnitude > 0.01f)
            FacingDirection = dir;

        if (flashlightPivot != null)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            flashlightPivot.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
