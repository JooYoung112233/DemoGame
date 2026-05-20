using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 8f;
    [SerializeField] float distance = 15f;
    [SerializeField] float angle = 55f;

    void LateUpdate()
    {
        if (target == null) return;

        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(0, Mathf.Sin(rad) * distance, -Mathf.Cos(rad) * distance);
        Vector3 desired = target.position + offset;

        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position);
    }
}
