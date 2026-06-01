using UnityEngine;

namespace TopDownMapEditor
{
    public class MapBuilderCamera : MonoBehaviour
    {
        [Header("Movement")]
        public float panSpeed = 15f;
        public float fastPanMultiplier = 2.5f;

        [Header("Zoom")]
        public float zoomSpeed = 5f;
        public float minZoom = 1f;
        public float maxZoom = 120f;

        [Header("View Angle (탑다운)")]
        // 기본값은 중앙 설정(TopDownGrid)에서 — iso→탑다운 전환의 단일 출처.
        public float cameraAngleX = TopDownGrid.CameraPitch;
        public float cameraAngleY = TopDownGrid.CameraYaw;
        public float initialDistance = 20f;

        [HideInInspector] public MapBuilderManager manager;

        Camera _cam;
        float _currentDistance;
        Vector3 _focusPoint;
        bool _middleDragging;
        Vector3 _lastMousePos;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = gameObject.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = initialDistance;
            _cam.nearClipPlane = -100f;
            _cam.farClipPlane = 500f;
            _currentDistance = initialDistance;
        }

        public void SetFocusPoint(Vector3 point)
        {
            _focusPoint = point;
            UpdateCameraPosition();
        }

        void Update()
        {
            HandleKeyboardPan();
            HandleMiddleMousePan();
            HandleScrollZoom();
            UpdateCameraPosition();
        }

        void HandleKeyboardPan()
        {
            // 프롭 접지 오프셋 조정 중이면 방향키는 프롭을 밀므로 카메라 패닝에서 제외(WASD만).
            bool arrowsForOffset = manager != null && manager.ActiveGroundOffset.HasValue;

            float h = 0f, v = 0f;
            if (Input.GetKey(KeyCode.W) || (!arrowsForOffset && Input.GetKey(KeyCode.UpArrow))) v = 1f;
            if (Input.GetKey(KeyCode.S) || (!arrowsForOffset && Input.GetKey(KeyCode.DownArrow))) v = -1f;
            if (Input.GetKey(KeyCode.A) || (!arrowsForOffset && Input.GetKey(KeyCode.LeftArrow))) h = -1f;
            if (Input.GetKey(KeyCode.D) || (!arrowsForOffset && Input.GetKey(KeyCode.RightArrow))) h = 1f;

            if (h == 0f && v == 0f) return;

            float speed = panSpeed;
            if (Input.GetKey(KeyCode.LeftShift)) speed *= fastPanMultiplier;

            // Pan relative to camera's XZ projection
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

            _focusPoint += (right * h + forward * v) * speed * Time.unscaledDeltaTime;
        }

        void HandleMiddleMousePan()
        {
            if (Input.GetMouseButtonDown(2))
            {
                _middleDragging = true;
                _lastMousePos = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(2))
                _middleDragging = false;

            if (_middleDragging)
            {
                Vector3 mouseDelta = Input.mousePosition - _lastMousePos;
                _lastMousePos = Input.mousePosition;

                // 스크린 픽셀 → 월드 단위 변환 (orthographic)
                float worldPerPixel = (_cam.orthographicSize * 2f) / Screen.height;

                // 카메라 로컬 축 기준으로 이동
                Vector3 right = transform.right;
                Vector3 up = Vector3.ProjectOnPlane(transform.up, Vector3.up).normalized;

                _focusPoint -= (right * mouseDelta.x + up * mouseDelta.y) * worldPerPixel;
            }
        }

        void HandleScrollZoom()
        {
            // 리사이즈 모드에서는 스크롤이 크기 조절에 쓰이므로 카메라 줌을 막는다.
            if (manager != null && manager.ResizeMode) return;

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f) return;

            // 줌 단계를 현재 거리에 비례시켜 가까울 땐 미세하게, 멀 땐 크게 움직인다.
            // (넓어진 범위 1~120에서도 스크롤 횟수가 일정하게 느껴지도록)
            float step = zoomSpeed * (_currentDistance / 20f);
            _currentDistance -= scroll * step;
            _currentDistance = Mathf.Clamp(_currentDistance, minZoom, maxZoom);
            _cam.orthographicSize = _currentDistance;
        }

        void UpdateCameraPosition()
        {
            Quaternion rot = Quaternion.Euler(cameraAngleX, cameraAngleY, 0);
            Vector3 offset = rot * (Vector3.back * _currentDistance);
            transform.position = _focusPoint + offset;
            transform.rotation = rot;
        }

        Vector3 GetMouseWorldXZ()
        {
            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            Plane xzPlane = new(Vector3.up, Vector3.zero);
            if (xzPlane.Raycast(ray, out float enter))
                return ray.GetPoint(enter);
            return _focusPoint;
        }

        public Vector3 ScreenToWorldXZ(Vector3 screenPos)
        {
            Ray ray = _cam.ScreenPointToRay(screenPos);
            Plane xzPlane = new(Vector3.up, Vector3.zero);
            if (xzPlane.Raycast(ray, out float enter))
                return ray.GetPoint(enter);
            return Vector3.zero;
        }
    }
}
