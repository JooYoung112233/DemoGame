using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 플레이어 이동 (NavMeshAgent) + 손전등 방향 제어.
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
    NavMeshAgent agent;
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
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.speed = MoveSpeed;
            agent.acceleration = 100f;  // 즉각 반응
            agent.angularSpeed = 0f;    // 회전은 직접 제어
        }
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
            UpdateMouseFacing();
        else
            FacingDirection = lastMoveDir;

        // 손전등 방향 갱신
        if (flashlightPivot != null && FacingDirection.sqrMagnitude > 0.01f)
            flashlightPivot.rotation = Quaternion.LookRotation(FacingDirection);

        // NavMeshAgent 이동
        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = MoveSpeed;
            agent.Move(moveDir * MoveSpeed * Time.deltaTime);
        }
        else
        {
            // 폴백: NavMesh 없을 때 직접 이동
            transform.position += moveDir * MoveSpeed * Time.deltaTime;
        }
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
