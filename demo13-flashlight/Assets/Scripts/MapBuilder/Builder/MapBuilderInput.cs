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
                _manager.ClearMoveHover();
                return;
            }

            UpdateMousePosition();
            UpdateHoverModes();
            _manager.UpdateGhostPosition(_lastGridCell, CurrentWorldPos);
            HandleMouseInput();
            HandleKeyboardShortcuts();
        }

        void UpdateHoverModes()
        {
            if (_manager.ResizeMode)
            {
                _manager.ClearEraseHover();
                _manager.ClearMoveHover();
                _manager.UpdateResizeHover(_lastGridCell, CurrentWorldPos);
            }
            else if (_manager.CurrentTool == ToolMode.Move)
            {
                _manager.ClearEraseHover();
                _manager.ClearResizeHover();
                _manager.UpdateMoveHover(_lastGridCell, CurrentWorldPos);
                // 집은 상태면 마우스 따라 이동
                if (_manager.IsMovingObject)
                    _manager.UpdateMovePosition(_lastGridCell, CurrentWorldPos);
            }
            else if (_manager.CurrentTool == ToolMode.Eraser)
            {
                _manager.ClearResizeHover();
                _manager.ClearMoveHover();
                _manager.UpdateEraseHover(_lastGridCell, CurrentWorldPos);
            }
            else
            {
                _manager.ClearEraseHover();
                _manager.ClearResizeHover();
                _manager.ClearMoveHover();
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

                // 브러시 미리보기는 타일 모드에서만 WxH, 그 외엔 1x1
                if (_manager.CurrentTool == ToolMode.Tile)
                    _manager.GridOverlay.SetBrushSize(_manager.BrushWidth, _manager.BrushHeight);
                else
                    _manager.GridOverlay.SetBrushSize(1, 1);

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

                if (Input.GetKeyDown(KeyCode.Escape))
                    _manager.ToggleResizeMode();
                return;
            }

            // Move mode: 클릭으로 집기/놓기, ESC로 취소
            if (_manager.CurrentTool == ToolMode.Move)
            {
                if (Input.GetMouseButtonDown(0))
                    _manager.OnMoveClick(_lastGridCell, CurrentWorldPos);

                if (Input.GetKeyDown(KeyCode.Escape) && _manager.IsMovingObject)
                    _manager.CancelMove();

                // 우클릭으로 이동 취소
                if (Input.GetMouseButtonDown(1) && _manager.IsMovingObject)
                    _manager.CancelMove();

                return;
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
            if (Input.GetKeyDown(KeyCode.Alpha7)) _manager.SetToolMode(ToolMode.Move);

            // Q/E: ±15° (1스텝), Shift+Q/E: ±1° (미세 조절)
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float rotStep = shift ? (1f / 15f) : 1f; // 1/15 스텝 = 1도
            if (Input.GetKeyDown(KeyCode.Q)) _manager.RotateSelection(-rotStep);
            if (Input.GetKeyDown(KeyCode.E)) _manager.RotateSelection(rotStep);
            if (Input.GetKeyDown(KeyCode.G)) _manager.ToggleSnapToGrid();
            if (Input.GetKeyDown(KeyCode.R)) _manager.ToggleResizeMode();

            // [ / ] : 지우개 모드에서 가리키는 프랍의 정렬 순서 미세조정
            if (Input.GetKeyDown(KeyCode.LeftBracket)) _manager.NudgeHoverSortOffset(-1);
            if (Input.GetKeyDown(KeyCode.RightBracket)) _manager.NudgeHoverSortOffset(+1);

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
