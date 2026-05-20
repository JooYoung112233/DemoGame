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
        Rect bounds;
        bool hasBounds;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        void LateUpdate()
        {
            if (followTarget != null)
            {
                Vector3 target = followTarget.position;
                target.z = transform.position.z;
                transform.position = Vector3.Lerp(transform.position, target, followSmooth * Time.deltaTime);
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
            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y -= 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1;

            transform.position += panSpeed * cam.orthographicSize * 0.1f * Time.deltaTime * move.normalized;
        }

        void HandleEdgePan()
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 move = Vector3.zero;

            if (mousePos.x < edgePanMargin) move.x -= 1;
            if (mousePos.x > Screen.width - edgePanMargin) move.x += 1;
            if (mousePos.y < edgePanMargin) move.y -= 1;
            if (mousePos.y > Screen.height - edgePanMargin) move.y += 1;

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
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, bounds.xMin, bounds.xMax);
            pos.y = Mathf.Clamp(pos.y, bounds.yMin, bounds.yMax);
            transform.position = pos;
        }

        public void SetBounds(Rect newBounds)
        {
            bounds = newBounds;
            hasBounds = true;
        }

        public void ClearBounds() => hasBounds = false;
    }
}
