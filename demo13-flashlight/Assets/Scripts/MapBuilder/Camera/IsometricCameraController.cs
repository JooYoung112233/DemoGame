using UnityEngine;

namespace IsometricMapEditor
{
    public class IsometricCameraController : MonoBehaviour
    {
        [Header("Pan")]
        [SerializeField] float panSpeed = 10f;
        [SerializeField] float edgePanSpeed = 5f;
        [SerializeField] float edgePanMargin = 20f;

        [Header("Zoom")]
        [SerializeField] float zoomSpeed = 5f;
        [SerializeField] float minZoom = 2f;
        [SerializeField] float maxZoom = 20f;

        [Header("Follow")]
        [SerializeField] Transform followTarget;
        [SerializeField] float followSmooth = 5f;

        Camera cam;
        Bounds bounds;
        bool hasBounds;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;

            // Setup 3D orthographic isometric camera
            cam.orthographic = true;
            cam.orthographicSize = 10f;
            transform.rotation = Quaternion.Euler(35.264f, 45f, 0);
        }

        void LateUpdate()
        {
            if (followTarget != null)
            {
                Vector3 target = followTarget.position;
                // Maintain camera offset along its forward direction
                float dist = (transform.position - followTarget.position).magnitude;
                Vector3 desired = target - transform.forward * dist;
                transform.position = Vector3.Lerp(transform.position, desired, followSmooth * Time.deltaTime);
            }
            else
            {
                HandleKeyboardPan();
                HandleEdgePan();
            }

            HandleZoom();

            if (hasBounds) ClampToBounds();
        }

        void HandleKeyboardPan()
        {
            // Pan along camera-local right (X) and world up projected onto XZ (forward flattened)
            Vector3 right = transform.right;
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move += forward;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move -= forward;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move -= right;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += right;

            transform.position += panSpeed * cam.orthographicSize * 0.1f * Time.deltaTime * move.normalized;
        }

        void HandleEdgePan()
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 right = transform.right;
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 move = Vector3.zero;
            if (mousePos.x < edgePanMargin) move -= right;
            if (mousePos.x > Screen.width - edgePanMargin) move += right;
            if (mousePos.y < edgePanMargin) move -= forward;
            if (mousePos.y > Screen.height - edgePanMargin) move += forward;

            transform.position += edgePanSpeed * cam.orthographicSize * 0.1f * Time.deltaTime * move.normalized;
        }

        void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.001f) return;

            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize - scroll * zoomSpeed,
                minZoom, maxZoom
            );
        }

        void ClampToBounds()
        {
            // Clamp the look-at point (where camera ray hits Y=0) within bounds
            Vector3 pos = transform.position;
            // Project camera position onto XZ for clamping
            float clampedX = Mathf.Clamp(pos.x, bounds.min.x, bounds.max.x);
            float clampedZ = Mathf.Clamp(pos.z, bounds.min.z, bounds.max.z);
            pos.x = clampedX;
            pos.z = clampedZ;
            transform.position = pos;
        }

        public void SetBounds(Bounds newBounds)
        {
            bounds = newBounds;
            hasBounds = true;
        }

        public void ClearBounds() => hasBounds = false;
    }
}
