using UnityEngine;

/// <summary>
/// 카메라 추적. GameSettings 연결 시 실시간 반영.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 8f;
    [SerializeField] float cameraAngle = 55f;
    [SerializeField] float cameraDistance = 20f;
    [SerializeField] float cameraYaw = 0f;
    [SerializeField] float orthoSize = 7f;

    [Header("Data")]
    [SerializeField] GameSettings gameSettings;

    Vector3 offset;
    Camera cam;

    float Angle => gameSettings != null ? gameSettings.cameraAngle : cameraAngle;
    float Distance => gameSettings != null ? gameSettings.cameraDistance : cameraDistance;
    float Yaw => gameSettings != null ? gameSettings.cameraYaw : cameraYaw;
    float Smooth => gameSettings != null ? gameSettings.cameraSmoothSpeed : smoothSpeed;
    float OrthoSize => gameSettings != null ? gameSettings.orthoSize : orthoSize;

    void Start()
    {
        cam = GetComponent<Camera>();
        UpdateOffset();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // GameSettings 변경 시 실시간 반영
        UpdateOffset();

        if (cam != null)
            cam.orthographicSize = OrthoSize;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, Smooth * Time.deltaTime);
    }

    void UpdateOffset()
    {
        Quaternion rot = Quaternion.Euler(Angle, Yaw, 0);
        offset = rot * new Vector3(0, 0, -Distance);
        transform.rotation = rot;
    }
}
