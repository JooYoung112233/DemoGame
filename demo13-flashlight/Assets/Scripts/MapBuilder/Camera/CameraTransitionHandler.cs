using UnityEngine;

namespace TopDownMapEditor
{
    public class CameraTransitionHandler : MonoBehaviour
    {
        public float transitionDuration = 0.5f;

        TopDownCameraController cameraController;
        bool _isTransitioning;
        Vector3 _targetPosition;
        Bounds _targetBounds;
        float _transitionElapsed;
        Vector3 _startPosition;

        public void Initialize(TopDownCameraController camera)
        {
            cameraController = camera;
        }

        public void TransitionToInterior(InteriorMapData interior)
        {
            if (cameraController == null) return;

            Bounds bounds = TopDownGrid.GetMapWorldBounds(interior.gridSettings);
            Vector3 center = bounds.center;
            // Offset camera along its backward direction to look at center from above
            center = center - cameraController.transform.forward * 20f;

            _startPosition = cameraController.transform.position;
            _targetPosition = center;
            _targetBounds = bounds;
            _transitionElapsed = 0f;
            _isTransitioning = true;
        }

        public void TransitionToExterior(GridSettings exteriorSettings)
        {
            if (cameraController == null) return;

            Bounds bounds = TopDownGrid.GetMapWorldBounds(exteriorSettings);
            cameraController.SetBounds(bounds);
        }

        void Update()
        {
            if (!_isTransitioning) return;

            _transitionElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_transitionElapsed / transitionDuration);
            t = t * t * (3f - 2f * t);

            cameraController.transform.position = Vector3.Lerp(_startPosition, _targetPosition, t);

            if (t >= 1f)
            {
                _isTransitioning = false;
                cameraController.SetBounds(_targetBounds);
            }
        }
    }
}
