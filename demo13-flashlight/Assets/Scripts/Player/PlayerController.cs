using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float crouchSpeedMultiplier = 0.4f;

    [Header("References")]
    [SerializeField] Transform flashlightPivot;

    Camera mainCam;
    Rigidbody2D rb;
    Vector2 moveInput;
    bool isCrouching;

    public bool IsCrouching => isCrouching;
    public Vector2 FacingDirection { get; private set; } = Vector2.right;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
    }

    void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
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
