using UnityEngine;
using UnityEngine.EventSystems;

namespace IsometricMapEditor
{
    public class MapBuilderInput : MonoBehaviour
    {
        MapBuilderManager _manager;
        MapBuilderCamera _camera;
        Vector2Int _lastGridCell = new(-1, -1);
        bool _isDragging;

        public Vector2Int CurrentGridCell => _lastGridCell;
        public Vector3 CurrentWorldPos { get; private set; }

        public void Initialize(MapBuilderManager manager, MapBuilderCamera camera)
        {
            _manager = manager;
            _camera = camera;
        }

        void Update()
        {
            if (_manager == null || _camera == null) return;
            if (IsPointerOverUI()) return;

            UpdateMousePosition();
            HandleMouseInput();
            HandleKeyboardShortcuts();
        }

        void UpdateMousePosition()
        {
            CurrentWorldPos = _camera.ScreenToWorldXZ(Input.mousePosition);
            var grid = _manager.EditingMap?.gridSettings;
            if (grid != null)
            {
                _lastGridCell = IsometricGrid.WorldToGrid(CurrentWorldPos, grid);
                _manager.GridOverlay.SetHighlightCell(_lastGridCell, grid.IsInBounds(_lastGridCell));
            }
        }

        void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = true;
                _manager.OnPointerDown(_lastGridCell, CurrentWorldPos);
            }

            if (Input.GetMouseButton(0) && _isDragging)
            {
                _manager.OnPointerDrag(_lastGridCell, CurrentWorldPos);
            }

            if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
            }

            if (Input.GetMouseButtonDown(1))
            {
                _manager.OnEraseAt(_lastGridCell, CurrentWorldPos);
            }
        }

        void HandleKeyboardShortcuts()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) _manager.SetToolMode(ToolMode.Tile);
            if (Input.GetKeyDown(KeyCode.Alpha2)) _manager.SetToolMode(ToolMode.Wall);
            if (Input.GetKeyDown(KeyCode.Alpha3)) _manager.SetToolMode(ToolMode.Prop);
            if (Input.GetKeyDown(KeyCode.Alpha4)) _manager.SetToolMode(ToolMode.MapObject);
            if (Input.GetKeyDown(KeyCode.Alpha5)) _manager.SetToolMode(ToolMode.Eraser);

            if (Input.GetKeyDown(KeyCode.Q)) _manager.RotateSelection(-1);
            if (Input.GetKeyDown(KeyCode.E)) _manager.RotateSelection(1);

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
                _manager.UI.ShowSaveDialog();
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L))
                _manager.UI.ShowLoadDialog();
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.N))
                _manager.UI.ShowNewMapDialog();
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
                _manager.Undo();
        }

        bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
