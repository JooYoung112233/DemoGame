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

            // 도움말이 열려 있으면 아무 키/클릭으로 닫고, 다른 입력은 무시.
            // (도움말 패널이 전체 화면 레이캐스트를 막아 아래 IsPointerOverUI에서 막히기 전에 처리)
            if (_manager.UI.IsHelpOpen)
            {
                if (Input.anyKeyDown) _manager.UI.CloseHelp();
                return;
            }

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
            // 벽 + 스냅 모드일 땐 동서남북(N/E/S/W) 90°씩 회전 (6스텝 = 90°).
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool wallSnap = _manager.CurrentTool == ToolMode.Wall && _manager.SnapToGrid;
            float rotStep = wallSnap ? 6f : (shift ? (1f / 15f) : 1f); // 6스텝=90°, 1/15스텝=1도
            if (Input.GetKeyDown(KeyCode.Q)) _manager.RotateSelection(-rotStep);
            if (Input.GetKeyDown(KeyCode.E)) _manager.RotateSelection(rotStep);
            if (Input.GetKeyDown(KeyCode.F)) _manager.ToggleFlipX();
            if (Input.GetKeyDown(KeyCode.B)) _manager.ToggleWallMount();
            if (Input.GetKeyDown(KeyCode.G)) _manager.ToggleSnapToGrid();
            if (Input.GetKeyDown(KeyCode.R)) _manager.ToggleResizeMode();

            // [ / ] : 지우개 모드에서 가리키는 프랍의 정렬 순서 미세조정
            if (Input.GetKeyDown(KeyCode.LeftBracket)) _manager.NudgeHoverSortOffset(-1);
            if (Input.GetKeyDown(KeyCode.RightBracket)) _manager.NudgeHoverSortOffset(+1);

            // 프롭 접지 오프셋(XYZ) 미세조정 — "방향키 = 화면 이동" 규약.
            // 배치 중(고스트) 또는 Move(7)로 집은 상태에서만 동작.
            //   ←/→ = 좌우(X), ↑/↓ = 위아래(Y, 높이), PageUp/PageDown = 깊이(Z), Home = 리셋.
            //   Shift = 미세(0.01). 방향키는 이 상태에서 카메라 패닝 대신 프롭을 민다(카메라는 WASD).
            // 예외: 벽 부착 컨텍스트에서는 PageUp/Down이 Z 오프셋 대신 벽 부착 높이를 조절.
            float offStep = shift ? 0.01f : 0.05f;
            if (_manager.ActiveGroundOffset.HasValue)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow)) _manager.AdjustGroundOffset(new Vector3(-offStep, 0f, 0f));
                if (Input.GetKeyDown(KeyCode.RightArrow)) _manager.AdjustGroundOffset(new Vector3(offStep, 0f, 0f));
                if (Input.GetKeyDown(KeyCode.UpArrow)) _manager.AdjustGroundOffset(new Vector3(0f, offStep, 0f));
                if (Input.GetKeyDown(KeyCode.DownArrow)) _manager.AdjustGroundOffset(new Vector3(0f, -offStep, 0f));
                if (Input.GetKeyDown(KeyCode.Home)) _manager.ResetGroundOffset();
            }

            // PageUp/PageDown: 벽 부착 컨텍스트면 부착 높이, 아니면 깊이(Z) 오프셋.
            if (_manager.IsWallMountContext)
            {
                float heightStep = shift ? 0.02f : 0.1f;
                if (Input.GetKeyDown(KeyCode.PageUp)) _manager.AdjustMountHeight(heightStep);
                if (Input.GetKeyDown(KeyCode.PageDown)) _manager.AdjustMountHeight(-heightStep);
            }
            else if (_manager.ActiveGroundOffset.HasValue)
            {
                if (Input.GetKeyDown(KeyCode.PageUp)) _manager.AdjustGroundOffset(new Vector3(0f, 0f, offStep));
                if (Input.GetKeyDown(KeyCode.PageDown)) _manager.AdjustGroundOffset(new Vector3(0f, 0f, -offStep));
            }

            // , / . : 편집 층(level) 내리기/올리기 (Zomboid식 층 쌓기)
            if (Input.GetKeyDown(KeyCode.Period)) _manager.ChangeLevel(+1);
            if (Input.GetKeyDown(KeyCode.Comma)) _manager.ChangeLevel(-1);
            // V : 층 컷어웨이(위층 숨김) 토글
            if (Input.GetKeyDown(KeyCode.V)) _manager.ToggleLevelCutaway();

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
                _manager.UI.ShowSaveDialog();
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L))
                _manager.UI.ShowLoadDialog();
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.N))
                _manager.UI.ShowNewMapDialog();
            // Undo는 상단 UI의 "Undo" 버튼으로. (플레이모드에서 Ctrl+Z는 Unity 에디터 undo와 충돌)

            if (Input.GetKeyDown(KeyCode.F1)) _manager.UI.ToggleHelp();

            // ESC: 배치 모드 취소 (리사이즈/이동 모드는 각자 ESC 처리가 따로 있음)
            if (Input.GetKeyDown(KeyCode.Escape) && !_manager.ResizeMode
                && _manager.CurrentTool != ToolMode.Move && _manager.HasActivePlacement)
                _manager.CancelPlacement();

            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
                _manager.DeleteSelectedObject();
        }

        bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
