using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 8f;
    [SerializeField] float cameraAngle = 30f;
    [SerializeField] float cameraDistance = 20f;

    Vector3 offset;

    [SerializeField] float cameraYaw = 45f;

    void Start()
    {
        Quaternion rot = Quaternion.Euler(cameraAngle, cameraYaw, 0);
        offset = rot * new Vector3(0, 0, -cameraDistance);

        transform.rotation = rot;
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
