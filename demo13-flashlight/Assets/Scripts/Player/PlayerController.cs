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

    public bool IsCrouching => isCrouching;
    public Vector3 FacingDirection { get; private set; } = Vector3.forward;

    void Awake()
    {
        mainCam = Camera.main;
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveDir = new Vector3(h, 0, v).normalized;

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
