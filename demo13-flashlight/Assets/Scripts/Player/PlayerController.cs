using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float crouchSpeedMultiplier = 0.4f;

    [Header("References")]
    [SerializeField] Transform flashlightPivot;

    Camera mainCam;
    CharacterController cc;
    Vector3 moveDir;
    bool isCrouching;

    // 카메라 기준 이동 방향 (XZ 평면 투영)
    Vector3 camForward;
    Vector3 camRight;

    public bool IsCrouching => isCrouching;
    public Vector3 FacingDirection { get; private set; } = Vector3.forward;

    void Awake()
    {
        mainCam = Camera.main;
        cc = GetComponent<CharacterController>();
    }

    void Start()
    {
        UpdateCameraAxes();
    }

    void UpdateCameraAxes()
    {
        if (mainCam == null) return;
        // 카메라의 forward/right를 XZ 평면에 투영
        camForward = mainCam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        camRight = mainCam.transform.right;
        camRight.y = 0;
        camRight.Normalize();
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 카메라 기준으로 이동 방향 계산
        moveDir = (camForward * v + camRight * h).normalized;

        isCrouching = Input.GetKey(KeyCode.LeftControl);

        RotateTowardsMouse();
    }

    void FixedUpdate()
    {
        float speed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
        if (cc != null)
            cc.Move(moveDir * speed * Time.fixedDeltaTime);
        else
            transform.position += moveDir * speed * Time.fixedDeltaTime;
    }

    void RotateTowardsMouse()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float dist))
        {
            Vector3 hitPoint = ray.GetPoint(dist);
            Vector3 dir = hitPoint - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                FacingDirection = dir.normalized;
                if (flashlightPivot != null)
                    flashlightPivot.rotation = Quaternion.LookRotation(dir.normalized);
            }
        }
    }
}
