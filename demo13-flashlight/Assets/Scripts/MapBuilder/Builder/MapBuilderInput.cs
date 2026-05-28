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

            if (IsPointerOverUI())
            {
                _manager.ClearEraseHover();
                return;
            }

            UpdateMousePosition();
            UpdateEraseHover();
            HandleMouseInput();
            HandleKeyboardShortcuts();
        }

        void UpdateEraseHover()
        {
            if (_manager.ResizeMode)
            {
                _manager.ClearEraseHover();
                _manager.UpdateResizeHover(_lastGridCell, CurrentWorldPos);
            }
            else if (_manager.CurrentTool == ToolMode.Eraser)
            {
                _manager.ClearResizeHover();
                _manager.UpdateEraseHover(_lastGridCell, CurrentWorldPos);
            }
            else
            {
                _manager.ClearEraseHover();
                _manager.ClearResizeHover();
            }
        }

        void UpdateMousePosition()
        {
            CurrentWorldPos = _camera.ScreenToWorldXZ(Input.mousePosition);
            var grid = _manager.EditingMap?.gridSettings;
            if (grid != null)
            {
                _lastGridCell = IsometricGrid.WorldToGrid(CurrentWorldPos, grid);
                bool inBounds = grid.IsInBounds(_lastGridCell);

                if (!_manager.SnapToGrid)
                    _manager.GridOverlay.SetFreeHighlight(CurrentWorldPos, _lastGridCell, inBounds);
                else
                    _manager.GridOverlay.SetHighlightCell(_lastGridCell, inBounds);
            }
        }

        void HandleMouseInput()
        {
            // Resize mode: scroll wheel adjusts scale
            if (_manager.ResizeMode)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.01f)
                    _manager.AdjustResizeScale(scroll * 0.1f);

                // ESC exits resize mode
                if (Input.GetKeyDown(KeyCode.Escape))
                    _manager.ToggleResizeMode();
                return; // block placement in resize mode
            }

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
            if (Input.GetKeyDown(KeyCode.Alpha4)) _manager.SetToolMode(ToolMode.Building);
            if (Input.GetKeyDown(KeyCode.Alpha5)) _manager.SetToolMode(ToolMode.MapObject);
            if (Input.GetKeyDown(KeyCode.Alpha6)) _manager.SetToolMode(ToolMode.Eraser);

            if (Input.GetKeyDown(KeyCode.Q)) _manager.RotateSelection(-1);
            if (Input.GetKeyDown(KeyCode.E)) _manager.RotateSelection(1);
            if (Input.GetKeyDown(KeyCode.G)) _manager.ToggleSnapToGrid();
            if (Input.GetKeyDown(KeyCode.R)) _manager.ToggleResizeMode();

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
                _manager.UI.ShowSaveDialog();
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L))
                _manager.UI.ShowLoadDialog();
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.N))
                _manager.UI.ShowNewMapDialog();
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
                _manager.Undo();

            if (Input.GetKeyDown(KeyCode.F1)) _manager.UI.ToggleHelp();

            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
                _manager.DeleteSelectedObject();
        }

        bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
