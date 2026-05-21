using UnityEngine;

/// <summary>
/// 플레이어 이동 + 손전등 방향 제어.
/// 기본: 이동 방향 = 바라보는 방향 = 손전등 방향
/// Ctrl: 이동은 WASD, 바라보는 방향/손전등은 마우스 (좀보이드 스타일)
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;

    [Header("References")]
    [SerializeField] Transform flashlightPivot;
    [SerializeField] CombatData combatData;

    Camera mainCam;
    CharacterController cc;
    Vector3 moveDir;

    // 카메라 기준 이동 축
    Vector3 camForward;
    Vector3 camRight;
    Vector3 lastMoveDir = Vector3.forward;

    float MoveSpeed => combatData != null ? combatData.player.moveSpeed : moveSpeed;

    public bool IsAiming { get; private set; }
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
        moveDir = (camForward * v + camRight * h).normalized;

        // 마지막 이동 방향 기억
        if (moveDir.sqrMagnitude > 0.01f)
            lastMoveDir = moveDir;

        // Ctrl = 에이밍 모드 (방향/이동 분리)
        IsAiming = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (IsAiming)
            UpdateMouseFacing(); // 마우스 방향
        else
            FacingDirection = lastMoveDir; // 이동 방향

        // 손전등 방향 갱신
        if (flashlightPivot != null && FacingDirection.sqrMagnitude > 0.01f)
            flashlightPivot.rotation = Quaternion.LookRotation(FacingDirection);
    }

    void FixedUpdate()
    {
        float speed = MoveSpeed;
        if (cc != null)
            cc.Move(moveDir * speed * Time.fixedDeltaTime);
        else
            transform.position += moveDir * speed * Time.fixedDeltaTime;
    }

    void UpdateMouseFacing()
    {
        if (mainCam == null) return;
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float dist))
        {
            Vector3 hitPoint = ray.GetPoint(dist);
            Vector3 dir = hitPoint - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
                FacingDirection = dir.normalized;
        }
    }
}
