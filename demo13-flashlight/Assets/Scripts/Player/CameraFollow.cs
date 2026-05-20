using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 8f;
    [SerializeField] float cameraAngle = 30f;
    [SerializeField] float cameraDistance = 20f;

    Vector3 offset;

    void Start()
    {
        float rad = cameraAngle * Mathf.Deg2Rad;
        offset = new Vector3(0, Mathf.Sin(rad) * cameraDistance, -Mathf.Cos(rad) * cameraDistance);

        // 각도 고정 (한 번만 설정)
        transform.rotation = Quaternion.Euler(cameraAngle, 0, 0);
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
