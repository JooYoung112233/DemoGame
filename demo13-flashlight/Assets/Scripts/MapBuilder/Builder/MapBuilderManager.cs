using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IsometricMapEditor
{
    public enum ToolMode
    {
        Tile,
        Wall,
        Prop,
        Building,
        MapObject,
        Eraser,
        Move
    }

    public class MapBuilderManager : MonoBehaviour
    {
        [Header("Catalog")]
        public MapBuilderCatalog catalog;

        [Header("Map Settings")]
        public int defaultMapWidth = 64;
        public int defaultMapHeight = 64;
        public float defaultTileSize = 1f;

        public MapData EditingMap { get; private set; }
        public ToolMode CurrentTool { get; private set; } = ToolMode.Tile;
        public TileDefinition SelectedTile { get; private set; }
        public TileDefinition SelectedWall { get; private set; }
        public PropDefinition SelectedProp { get; private set; }
        public BuildingDefinition SelectedBuilding { get; private set; }
        public MapObjectType SelectedObjectType { get; private set; } = MapObjectType.SpawnPoint;
        // Eraser hover
        readonly List<(Renderer rend, string prop, Color origColor)> _eraseHoverRenderers = new();

        // 셰이더마다 색 프로퍼티 이름이 달라서(_Color/_BaseColor/_Tint) 안전하게 찾는 헬퍼
        static string GetColorPropName(Material m)
        {
            if (m == null) return null;
            if (m.HasProperty("_BaseColor")) return "_BaseColor";
            if (m.HasProperty("_Color")) return "_Color";
            if (m.HasProperty("_Tint")) return "_Tint";
            return null;
        }
        string _eraseHoverInstanceId;
        public string EraseHoverInfo { get; private set; }

        // 그림 순서(정렬) 조절 대상:
        //  - 이동(Move) 모드에서 프랍/건물을 집은 상태  → 그 오브젝트
        //  - 배치(Prop/Building) 모드             → 다음에 놓을 오브젝트(_placementSortOffset)
        int _placementSortOffset; // 배치 모드에서 다음 프랍/건물에 적용할 정렬 오프셋

        bool IsMoveSortTarget => _moveGrabbed && (_moveType == 2 || _moveType == 3);
        bool IsPlacementSortTarget => CurrentTool == ToolMode.Prop || CurrentTool == ToolMode.Building;

        public bool HasSortTarget => IsMoveSortTarget || IsPlacementSortTarget;

        public string SortTargetLabel
        {
            get
            {
                if (IsMoveSortTarget) return "그림순서 (이동 중)";
                if (IsPlacementSortTarget) return "그림순서 (배치)";
                return null;
            }
        }

        public int SortTargetOffset
        {
            get
            {
                if (_moveGrabbed && _moveType == 2)
                {
                    var p = EditingMap?.props.Find(x => x.instanceId == _moveInstanceId);
                    if (p != null) return p.sortingOffsetOverride;
                }
                if (_moveGrabbed && _moveType == 3)
                {
                    var b = EditingMap?.buildings.Find(x => x.instanceId == _moveInstanceId);
                    if (b != null) return b.sortingOffsetOverride;
                }
                return _placementSortOffset;
            }
        }

        // Resize mode
        public bool ResizeMode { get; set; }
        readonly List<(Renderer rend, string prop, Color origColor)> _resizeHoverRenderers = new();
        string _resizeHoverInstanceId;
        public string ResizeHoverInfo { get; private set; }

        // Move mode
        public bool MoveMode { get; private set; }
        bool _moveGrabbed; // 오브젝트를 집었는가
        string _moveInstanceId;
        int _moveType = -1; // 0=tile, 1=wall, 2=prop, 3=building, 4=mapObject
        int _moveWallRotation;
        readonly List<(Renderer rend, string prop, Color origColor)> _moveHoverRenderers = new();
        string _moveHoverInstanceId;
        public string MoveHoverInfo { get; private set; }

        // Visibility toggles
        public bool ShowTiles { get; set; } = true;
        public bool ShowWalls { get; set; } = true;
        public bool ShowProps { get; set; } = true;
        public bool ShowBuildings { get; set; } = true;
        public bool ShowMapObjects { get; set; } = true;

        // 개별 건물 숨김 (에디터 편집 전용 — 저장 데이터엔 영향 없음. 내부에 프랍 배치할 때 사용)
        readonly HashSet<string> _hiddenBuildingIds = new();
        public bool InteriorViewActive { get; private set; }

        /// <summary>모든 벽을 숨기는 편집용 토글. 벽으로 직접 쌓은 구조물 내부에 프랍을 놓을 때 사용.
        /// (프리팹 건물의 "내부 보기"와 별개 — 이쪽은 Wall 도구로 깐 개별 벽 전체 대상.)</summary>
        public bool WallsHidden { get; private set; }

        // Spawn config properties (for MapObject placement)
        public bool SpawnUseRegionLoot { get; set; } = true;
        public int SpawnContainerWidth { get; set; } = 4;
        public int SpawnContainerHeight { get; set; } = 5;
        public string SpawnContainerName { get; set; } = "";
        public string SpawnEnemyUnitKey { get; set; } = "";
        public int SpawnEnemyCount { get; set; } = 1;
        public string SpawnFixedItemId { get; set; } = "";
        public int SpawnFixedItemCount { get; set; } = 1;
        public int SpawnDoorLockType { get; set; } = 0;
        public string SpawnDoorKeyId { get; set; } = "";
        public bool SpawnDoorConsumeKey { get; set; } = true;
        public string SpawnDoorQuestId { get; set; } = "";
        public string SpawnNpcId { get; set; } = "";
        public string SpawnNpcDisplayName { get; set; } = "";
        public int SpawnVisualMode { get; set; }
        public string SpawnVisualTexturePath { get; set; } = "";
        public string SpawnEffectPrefabPath { get; set; } = "";
        public float SpawnVisualScale { get; set; } = 1f;

        // Trigger config properties (for Trigger placement)
        public int SpawnTriggerMode { get; set; }              // 0=SceneTransition, 1=LocalTeleport, 2=StoryTrigger, 3=CustomEvent
        public string SpawnTriggerTargetScene { get; set; } = "";
        public string SpawnTriggerTargetSpawnId { get; set; } = "";
        public bool SpawnTriggerAutoEnter { get; set; }
        public float SpawnTriggerDelay { get; set; }
        public bool SpawnTriggerOneShot { get; set; }
        public float SpawnTriggerSizeX { get; set; } = 1.5f;
        public float SpawnTriggerSizeY { get; set; } = 2f;
        public float SpawnTriggerSizeZ { get; set; } = 1.5f;
        public float SpawnTeleportX { get; set; }
        public float SpawnTeleportY { get; set; }
        public float SpawnTeleportZ { get; set; }
        public float SpawnTeleportYRot { get; set; }
        public string SpawnTriggerStorySceneId { get; set; } = "";
        public string SpawnTriggerCustomData { get; set; } = "";

        // 건물 소속 (Prop + MapObject 공통)
        public string SelectedParentBuildingId { get; set; } = "";

        public float CurrentRotation { get; private set; }
        public bool CurrentFlipX { get; private set; }
        /// <summary>다음에 놓을 프롭을 벽 부착 모드로 둘지. (B 키로 토글)</summary>
        public bool CurrentWallMount { get; private set; }
        /// <summary>벽 부착 시 바닥에서 띄울 높이(m). (PageUp/Down으로 조절)</summary>
        public float CurrentMountHeight { get; private set; } = 1.2f;
        /// <summary>다음에 놓을 프롭의 인스턴스별 접지 오프셋(XYZ, 넘패드로 조절). 배치 시 groundOffsetOverride로 박힌다.</summary>
        public Vector3 CurrentGroundOffset { get; private set; }
        public bool SnapToGrid { get; set; }

        /// <summary>현재 편집 중인 층(level). 0=1층. 새로 놓는 타일/벽은 이 층에 배치된다. (PageUp/Down 또는 UI로 변경)</summary>
        public int CurrentLevel { get; private set; }
        public const int MAX_LEVEL = 9;

        public void SetCurrentLevel(int level)
        {
            CurrentLevel = Mathf.Clamp(level, 0, MAX_LEVEL);
            ApplyLevelCutaway();
            UI?.RefreshAll();
        }

        /// <summary>편집 층을 delta만큼 올리거나 내린다. (UI/키 입력 공통 진입점)</summary>
        public void ChangeLevel(int delta) => SetCurrentLevel(CurrentLevel + delta);

        // 타일 브러시 크기 (1~5). 클릭한 칸을 중심으로 WxH 범위에 배치.
        public int BrushWidth { get; private set; } = 1;
        public int BrushHeight { get; private set; } = 1;
        public const int MAX_BRUSH = 5;

        public void SetBrushSize(int w, int h)
        {
            BrushWidth = Mathf.Clamp(w, 1, MAX_BRUSH);
            BrushHeight = Mathf.Clamp(h, 1, MAX_BRUSH);
            UI?.RefreshStatus();
        }

        /// <summary>브러시 범위 셀 목록. anchor를 중심으로 BrushWidth x BrushHeight.</summary>
        public IEnumerable<Vector2Int> GetBrushCells(Vector2Int anchor)
        {
            int offX = (BrushWidth - 1) / 2;
            int offY = (BrushHeight - 1) / 2;
            for (int dx = 0; dx < BrushWidth; dx++)
                for (int dy = 0; dy < BrushHeight; dy++)
                    yield return new Vector2Int(anchor.x - offX + dx, anchor.y - offY + dy);
        }

        public MapBuilderUI UI { get; private set; }
        public MapBuilderGridOverlay GridOverlay { get; private set; }
        public MapBuilderCamera BuilderCamera { get; private set; }

        TileRenderer _tileRenderer;
        Transform _wallRoot;
        Transform _propRoot;
        Transform _buildingRoot;
        Transform _objectRoot;
        readonly Dictionary<string, GameObject> _wallObjects = new();
        readonly Dictionary<string, GameObject> _propObjects = new();
        readonly Dictionary<string, GameObject> _buildingObjects = new();
        readonly Dictionary<string, GameObject> _objectMarkers = new();
        readonly List<string> _undoSnapshots = new();
        const int MAX_UNDO = 30;
        Vector2Int _lastDragCell = new(-1, -1);

        // 배치 미리보기 (고스트)
        GameObject _placementGhost;

        void Start()
        {
            if (catalog == null)
            {
                catalog = Resources.Load<MapBuilderCatalog>("MapBuilder/MapBuilderCatalog");
                if (catalog == null)
                    Debug.LogWarning("[MapBuilder] Catalog이 없습니다. Tools > Dev Tools > Map > Open Map Builder로 생성하세요.");
            }

            SetupScene();
            NewMap(defaultMapWidth, defaultMapHeight, defaultTileSize);
        }

        void SetupScene()
        {
            // Camera
            var camGo = new GameObject("MapBuilderCamera");
            BuilderCamera = camGo.AddComponent<MapBuilderCamera>();
            BuilderCamera.manager = this;
            camGo.AddComponent<AudioListener>();
            BuilderCamera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            BuilderCamera.GetComponent<Camera>().backgroundColor = new Color(0.15f, 0.15f, 0.18f);

            // Grid overlay (on camera so OnRenderObject fires)
            GridOverlay = camGo.AddComponent<MapBuilderGridOverlay>();

            // Tile renderer
            var tileGo = new GameObject("TileRoot");
            tileGo.transform.SetParent(transform);
            _tileRenderer = tileGo.AddComponent<TileRenderer>();

            // Wall root
            _wallRoot = new GameObject("WallRoot").transform;
            _wallRoot.SetParent(transform);

            // Prop root
            _propRoot = new GameObject("PropRoot").transform;
            _propRoot.SetParent(transform);

            // Building root
            _buildingRoot = new GameObject("BuildingRoot").transform;
            _buildingRoot.SetParent(transform);

            // Object marker root
            _objectRoot = new GameObject("ObjectRoot").transform;
            _objectRoot.SetParent(transform);

            // Input
            var input = gameObject.AddComponent<MapBuilderInput>();
            input.Initialize(this, BuilderCamera);

            // UI
            UI = gameObject.AddComponent<MapBuilderUI>();
            UI.Initialize(this);

            // 기존 게임 UI/HUD 캔버스 + 게임 카메라 비활성화 (빌더 것 제외).
            // GameBootstrap/UIManager/PlayerController 싱글톤이 DontDestroyOnLoad로
            // 넘어오거나 늦게(Start) 캔버스를 만들 수 있어 매 프레임 정리한다.
            HideExternalObjects(true);

            // EventSystem
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Light
            if (FindFirstObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("DirectionalLight");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
        }

        /// <summary>
        /// 빌더 것을 제외한 모든 외부 Canvas/Camera 를 비활성화한다.
        /// DontDestroyOnLoad 싱글톤(GameHUD/QuestHUD/PlayerController 카메라 등)이
        /// 맵 빌더 씬에 남거나 늦게 생성되는 것을 매 프레임 정리한다.
        /// </summary>
        void HideExternalObjects(bool log = false)
        {
            // --- Canvas ---
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas == null) continue;
                if (canvas.gameObject.name == "MapBuilderCanvas") continue;
                if (canvas.transform.IsChildOf(transform)) continue;
                // Canvas 컴포넌트 자체를 끈다 (GameObject는 살려 싱글톤 로직 깨지지 않게)
                if (canvas.enabled)
                {
                    canvas.enabled = false;
                    if (log) Debug.Log($"[MapBuilder] Disabled external Canvas: {canvas.name}");
                }
            }

            // --- Camera (빌더 카메라 외 전부 끔) ---
            var builderCam = BuilderCamera != null ? BuilderCamera.GetComponent<Camera>() : null;
            var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var cam in cameras)
            {
                if (cam == null || cam == builderCam) continue;
                if (cam.transform.IsChildOf(transform)) continue;
                if (cam.enabled)
                {
                    cam.enabled = false;
                    if (log) Debug.Log($"[MapBuilder] Disabled external Camera: {cam.name}");
                }
            }
        }

        // 싱글톤들이 늦게(Start/이후 프레임) 캔버스·카메라를 켤 수 있어 계속 정리한다.
        void LateUpdate()
        {
            HideExternalObjects();
        }

        public void NewMap(int width, int height, float tileSize = 1f)
        {
            EditingMap = ScriptableObject.CreateInstance<MapData>();
            EditingMap.mapName = "NewMap";
            EditingMap.mapId = System.Guid.NewGuid().ToString("N")[..8];
            EditingMap.gridSettings = new GridSettings
            {
                tileSize = tileSize <= 0f ? 1f : tileSize,
                originOffset = Vector3.zero,
                mapWidth = width,
                mapHeight = height
            };
            EditingMap.InitializeWalkability();

            GridOverlay.gridSettings = EditingMap.gridSettings;
            ClearAllVisuals();

            Vector3 center = IsometricGrid.GridToWorld(
                new Vector2Int(width / 2, height / 2), EditingMap.gridSettings);
            BuilderCamera.SetFocusPoint(center);

            _undoSnapshots.Clear();
            UI.RefreshAll();
        }

        public void SetToolMode(ToolMode mode)
        {
            if (MoveMode && _moveGrabbed) CancelMove();
            ClearMoveHover();
            CurrentTool = mode;
            CurrentRotation = 0;
            CurrentFlipX = false;
            CurrentWallMount = false;
            MoveMode = mode == ToolMode.Move;
            RebuildGhost();
            UI.RefreshToolbar();
        }

        public void SelectTile(TileDefinition tile)
        {
            SelectedTile = tile;
            CurrentTool = ToolMode.Tile;
            UI.RefreshToolbar();
        }

        public void SelectWall(TileDefinition wall)
        {
            SelectedWall = wall;
            CurrentTool = ToolMode.Wall;
            CurrentRotation = 0;
            RebuildGhost();
            UI.RefreshToolbar();
        }

        public void SelectProp(PropDefinition prop)
        {
            SelectedProp = prop;
            CurrentTool = ToolMode.Prop;
            // 벽 부착 가능한 프롭이면 기본값을 켜고 정의된 높이를 채운다.
            CurrentWallMount = prop != null && prop.wallMountable;
            if (prop != null && prop.wallMountable && prop.defaultMountHeight > 0f)
                CurrentMountHeight = prop.defaultMountHeight;
            RebuildGhost();
            UI.RefreshToolbar();
        }

        public void SelectBuilding(BuildingDefinition building)
        {
            SelectedBuilding = building;
            CurrentTool = ToolMode.Building;
            RebuildGhost();
            UI.RefreshToolbar();
        }

        public MapObjectDefinition SelectedObjectDef { get; private set; }

        public void SelectObjectType(MapObjectType type)
        {
            SelectedObjectType = type;
            SelectedObjectDef = null;
            CurrentTool = ToolMode.MapObject;
            UI.RefreshToolbar();
        }

        public void SelectObjectDef(MapObjectDefinition def)
        {
            if (def == null) return;
            SelectedObjectDef = def;
            SelectedObjectType = def.objectType;
            SpawnVisualMode = def.visualMode;
            SpawnVisualTexturePath = def.visualTexture != null ? def.visualTexture.name : "";
            SpawnEffectPrefabPath = def.effectPrefab != null ? def.effectPrefab.name : "";
            SpawnVisualScale = def.visualScale;
            CurrentTool = ToolMode.MapObject;
            UI.RefreshToolbar();
        }

        public void SetTileVisibility(bool v) { ShowTiles = v; if (_tileRenderer) _tileRenderer.gameObject.SetActive(v); UI.RefreshAll(); }
        public void SetWallVisibility(bool v) { ShowWalls = v; if (_wallRoot) _wallRoot.gameObject.SetActive(v); UI.RefreshAll(); }
        public void SetPropVisibility(bool v) { ShowProps = v; if (_propRoot) _propRoot.gameObject.SetActive(v); UI.RefreshAll(); }
        public void SetBuildingVisibility(bool v) { ShowBuildings = v; if (_buildingRoot) _buildingRoot.gameObject.SetActive(v); UI.RefreshAll(); }
        public void SetMapObjectVisibility(bool v) { ShowMapObjects = v; if (_objectRoot) _objectRoot.gameObject.SetActive(v); UI.RefreshAll(); }

        // ── 개별 건물 숨김 (내부 프랍 배치용) ──

        /// <summary>현재 맵에 배치된 건물 목록 (UI 리스트용).</summary>
        public IReadOnlyList<PlacedBuilding> PlacedBuildings =>
            EditingMap != null ? EditingMap.buildings : System.Array.Empty<PlacedBuilding>();

        public bool IsBuildingHidden(string instanceId) => _hiddenBuildingIds.Contains(instanceId);

        /// <summary>특정 건물 인스턴스를 숨기거나 표시한다. (에디터 편집 전용)</summary>
        public void SetBuildingHidden(string instanceId, bool hidden)
        {
            if (string.IsNullOrEmpty(instanceId)) return;
            if (hidden) _hiddenBuildingIds.Add(instanceId);
            else _hiddenBuildingIds.Remove(instanceId);
            if (_buildingObjects.TryGetValue(instanceId, out var go) && go != null)
                go.SetActive(!hidden);
            UI.RefreshStatus();
        }

        /// <summary>리렌더 후 숨김 상태 재적용 (RebuildAllVisuals 끝에서 호출).</summary>
        void ApplyBuildingHiddenState()
        {
            foreach (var id in _hiddenBuildingIds)
                if (_buildingObjects.TryGetValue(id, out var go) && go != null)
                    go.SetActive(false);
        }

        /// <summary>층 컷어웨이 활성 여부. 켜면 현재 편집 층보다 위층 오브젝트를 숨겨 아래층이 잘 보인다. (V 키/UI로 토글)</summary>
        public bool LevelCutawayEnabled { get; private set; } = true;

        public void ToggleLevelCutaway()
        {
            LevelCutawayEnabled = !LevelCutawayEnabled;
            ApplyLevelCutaway();
            UI?.RefreshStatus();
        }

        /// <summary>
        /// 현재 편집 층(CurrentLevel)보다 위층의 타일/벽/프롭/건물/오브젝트를 숨긴다.
        /// (컷어웨이 비활성 시 모두 표시) 층 변경·리렌더·토글 시 호출.
        /// </summary>
        void ApplyLevelCutaway()
        {
            if (EditingMap == null) return;
            bool on = LevelCutawayEnabled;

            // 바닥 타일 (TileRenderer가 층별 키 보유)
            _tileRenderer.ApplyLevelCutaway(CurrentLevel, on);

            // 벽 (IsWall 타일)
            foreach (var layer in EditingMap.layers)
            {
                foreach (var tile in layer.tiles)
                {
                    if (tile.tileDefinition == null || !tile.tileDefinition.IsWall) continue;
                    if (_wallObjects.TryGetValue(WallKeyFor(tile), out var go) && go != null)
                    {
                        // 전체 벽 숨김 토글이 켜져 있으면 무조건 숨김, 아니면 층 컷어웨이 규칙.
                        bool show = !WallsHidden && (!on || tile.level <= CurrentLevel);
                        if (go.activeSelf != show) go.SetActive(show);
                    }
                }
            }

            // 프롭
            foreach (var p in EditingMap.props)
                if (_propObjects.TryGetValue(p.instanceId, out var go) && go != null)
                {
                    bool show = !on || p.level <= CurrentLevel;
                    if (go.activeSelf != show) go.SetActive(show);
                }

            // 건물 (내부 보기 등으로 따로 숨긴 건 계속 숨김 유지)
            foreach (var b in EditingMap.buildings)
                if (_buildingObjects.TryGetValue(b.instanceId, out var go) && go != null)
                {
                    bool show = (!on || b.level <= CurrentLevel) && !_hiddenBuildingIds.Contains(b.instanceId);
                    if (go.activeSelf != show) go.SetActive(show);
                }

            // 맵 오브젝트 마커
            foreach (var o in EditingMap.mapObjects)
                if (_objectMarkers.TryGetValue(o.instanceId, out var go) && go != null)
                {
                    bool show = !on || o.level <= CurrentLevel;
                    if (go.activeSelf != show) go.SetActive(show);
                }
        }

        /// <summary>"내부 보기" 토글 — occludesInterior(윗벽/천장) 파트를 한 번에 켜고 끈다.</summary>
        public void ToggleInteriorView()
        {
            InteriorViewActive = !InteriorViewActive;
            if (EditingMap != null)
            {
                foreach (var b in EditingMap.buildings)
                {
                    if (b.buildingDefinition != null && b.buildingDefinition.occludesInterior)
                        SetBuildingHidden(b.instanceId, InteriorViewActive);
                }
            }
            UI.RefreshStatus();
        }

        /// <summary>모든 건물을 다시 표시한다.</summary>
        public void ShowAllBuildings()
        {
            InteriorViewActive = false;
            var ids = new List<string>(_hiddenBuildingIds);
            foreach (var id in ids)
                SetBuildingHidden(id, false);
            UI.RefreshStatus();
        }

        /// <summary>모든 벽 숨김 토글. 벽으로 쌓은 구조물 내부에 프랍을 놓을 때 사용.
        /// 벽 가시성의 단일 출처인 ApplyLevelCutaway를 다시 돌려 (층 컷어웨이와 AND) 반영.</summary>
        public void ToggleWallsHidden()
        {
            WallsHidden = !WallsHidden;
            ApplyLevelCutaway();
            UI?.RefreshStatus();
        }

        public void RotateSelection(float delta)
        {
            CurrentRotation = ((CurrentRotation + delta) % 24f + 24f) % 24f;
            UI.RefreshStatus();

            // 이동 모드에서 집은 오브젝트의 각도도 실시간 갱신
            if (_moveGrabbed) ApplyMoveRotation();
        }

        /// <summary>프롭(빌보드 스프라이트)의 좌우 반전 토글. 배치 고스트/이동 중 오브젝트에 즉시 반영.</summary>
        public void ToggleFlipX()
        {
            CurrentFlipX = !CurrentFlipX;

            // 배치 고스트가 프롭이면 반영
            if (CurrentTool == ToolMode.Prop)
                PropQuadBuilder.ApplyFlip(_placementGhost, CurrentFlipX);

            // 이동 모드에서 집은 프롭이면 실시간 반영
            if (_moveGrabbed && _moveType == 2
                && _propObjects.TryGetValue(_moveInstanceId, out var moveGo))
                PropQuadBuilder.ApplyFlip(moveGo, CurrentFlipX);

            UI.RefreshStatus();
        }

        /// <summary>프롭 벽 부착 모드 토글. 고스트/이동 중 프롭에 즉시 반영.</summary>
        public void ToggleWallMount()
        {
            if (CurrentTool != ToolMode.Prop) return;
            CurrentWallMount = !CurrentWallMount;
            RefreshPropGhostMount();

            // 이동 모드에서 집은 프롭이면 데이터+비주얼 즉시 반영
            if (_moveGrabbed && _moveType == 2)
            {
                var p = EditingMap.props.Find(x => x.instanceId == _moveInstanceId);
                if (p != null) { p.wallMounted = CurrentWallMount; p.mountHeight = CurrentMountHeight; }
                ApplyMoveRotation();
                ApplyMovePropMountHeight();
            }
            UI.RefreshStatus();
        }

        /// <summary>벽 부착 높이 조절(±). 벽 부착 모드일 때만 의미 있음.</summary>
        public void AdjustMountHeight(float delta)
        {
            if (CurrentTool != ToolMode.Prop) return;
            CurrentMountHeight = Mathf.Clamp(CurrentMountHeight + delta, 0f, 10f);
            RefreshPropGhostMount();

            if (_moveGrabbed && _moveType == 2)
            {
                var p = EditingMap.props.Find(x => x.instanceId == _moveInstanceId);
                if (p != null) p.mountHeight = CurrentMountHeight;
                ApplyMovePropMountHeight();
            }
            UI.RefreshStatus();
        }

        /// <summary>지금 접지 오프셋(XYZ)을 보여줄 대상값을 반환. Move로 프롭을 집었으면 그 인스턴스,
        /// 아니면 프롭 배치 모드의 CurrentGroundOffset. 둘 다 아니면 null.</summary>
        public Vector3? ActiveGroundOffset
        {
            get
            {
                if (_moveGrabbed && _moveType == 2)
                {
                    var p = EditingMap.props.Find(x => x.instanceId == _moveInstanceId);
                    return p != null ? p.groundOffsetOverride : (Vector3?)null;
                }
                if (CurrentTool == ToolMode.Prop && SelectedProp != null) return CurrentGroundOffset;
                return null;
            }
        }

        /// <summary>벽 부착 높이를 조절하는 컨텍스트인가. 집은 프롭이 벽 부착 상태이거나,
        /// 프롭 배치 모드에서 벽 부착이 켜진 경우. PageUp/Down 라우팅에 사용(높이 vs 깊이Z).</summary>
        public bool IsWallMountContext
        {
            get
            {
                if (_moveGrabbed && _moveType == 2)
                {
                    var p = EditingMap.props.Find(x => x.instanceId == _moveInstanceId);
                    return p != null && p.wallMounted;
                }
                return CurrentTool == ToolMode.Prop && SelectedProp != null && CurrentWallMount;
            }
        }

        /// <summary>접지 오프셋(XYZ) 누적 조정. Move로 프롭을 집었으면 그 인스턴스에, 아니면 다음 배치용
        /// CurrentGroundOffset에 적용(고스트에 즉시 반영). 데이터+비주얼 즉시 반영.</summary>
        public void AdjustGroundOffset(Vector3 delta)
        {
            if (_moveGrabbed && _moveType == 2)
            {
                var p = EditingMap.props.Find(x => x.instanceId == _moveInstanceId);
                if (p == null) return;
                p.groundOffsetOverride += delta;
                ApplyMovePropMountHeight();
                UI.RefreshStatus();
                return;
            }
            if (CurrentTool != ToolMode.Prop || SelectedProp == null) return;
            CurrentGroundOffset += delta;
            UI.RefreshStatus();
        }

        /// <summary>접지 오프셋을 0으로 리셋. 집은 프롭이 있으면 그 인스턴스, 아니면 배치용 CurrentGroundOffset.</summary>
        public void ResetGroundOffset()
        {
            if (_moveGrabbed && _moveType == 2)
            {
                var p = EditingMap.props.Find(x => x.instanceId == _moveInstanceId);
                if (p == null) return;
                p.groundOffsetOverride = Vector3.zero;
                ApplyMovePropMountHeight();
                UI.RefreshStatus();
                return;
            }
            if (CurrentTool != ToolMode.Prop) return;
            CurrentGroundOffset = Vector3.zero;
            UI.RefreshStatus();
        }

        /// <summary>배치 고스트가 프롭이면 벽 부착 회전을 다시 적용 (위치는 UpdateGhostPosition이 갱신).</summary>
        void RefreshPropGhostMount()
        {
            if (CurrentTool == ToolMode.Prop && _placementGhost != null)
            {
                float yRot = CurrentRotation * 15f;
                _placementGhost.transform.rotation = PropQuadBuilder.RootRotation(yRot, CurrentWallMount);
            }
        }

        /// <summary>이동 중 집은 프롭의 월드 Y를 벽 부착 높이에 맞춰 갱신.</summary>
        void ApplyMovePropMountHeight()
        {
            if (!_moveGrabbed || _moveType != 2) return;
            if (!_propObjects.TryGetValue(_moveInstanceId, out var go) || go == null) return;
            var p = EditingMap.props.Find(x => x.instanceId == _moveInstanceId);
            if (p == null) return;
            Vector3 basePos = p.GetWorldPosition(EditingMap.gridSettings);
            basePos.y += p.ElevationY(EditingMap.gridSettings);
            if (p.wallMounted) basePos += Vector3.up * p.mountHeight;
            // 스프라이트 프롭은 sortingOrder로 정렬 — 시선축 위치 오프셋 없이 바닥에 그대로(띄움 방지).
            go.transform.position = basePos;
            // 접지 보정은 root가 아니라 내부 Content(이미지+콜라이더)에 적용.
            PropQuadBuilder.ApplyGroundOffset(go, p.GroundOffsetVec);
        }

        // --- Placement ---

        public void OnPointerDown(Vector2Int cell, Vector3 worldPos)
        {
            _lastDragCell = new Vector2Int(-1, -1);
            PlaceAt(cell, worldPos);
        }

        public void OnPointerDrag(Vector2Int cell, Vector3 worldPos)
        {
            if (cell == _lastDragCell) return;
            PlaceAt(cell, worldPos);
        }

        void PlaceAt(Vector2Int cell, Vector3 worldPos)
        {
            if (EditingMap == null) return;
            if (!EditingMap.gridSettings.IsInBounds(cell)) return;

            _lastDragCell = cell;

            switch (CurrentTool)
            {
                case ToolMode.Tile:
                    PlaceTile(cell);
                    break;
                case ToolMode.Wall:
                    PlaceWall(cell, worldPos);
                    break;
                case ToolMode.Prop:
                    PlaceProp(worldPos);
                    break;
                case ToolMode.Building:
                    PlaceBuildingAt(cell, worldPos);
                    break;
                case ToolMode.MapObject:
                    PlaceMapObject(cell, worldPos);
                    break;
                case ToolMode.Eraser:
                    EraseAt(cell);
                    break;
            }
        }

        void PlaceTile(Vector2Int cell)
        {
            if (SelectedTile == null) return;
            SaveUndoSnapshot();

            var layer = EditingMap.GetOrCreateLayer("Ground");
            int rot = Mathf.RoundToInt(CurrentRotation);

            foreach (var c in GetBrushCells(cell))
            {
                if (!EditingMap.gridSettings.IsInBounds(c)) continue;

                var tile = new PlacedTile
                {
                    gridPosition = c,
                    tileDefinitionId = SelectedTile.tileId,
                    tileDefinition = SelectedTile,
                    rotation = rot,
                    flipX = false,
                    level = CurrentLevel
                };

                EditingMap.PlaceTile(tile, "Ground");
                _tileRenderer.RenderSingleTile(tile, layer, EditingMap.gridSettings);
            }
        }

        void PlaceWall(Vector2Int cell, Vector3 worldPos)
        {
            if (SelectedWall == null) return;
            SaveUndoSnapshot();

            bool free = !SnapToGrid;
            PlacedTile tile;
            string key;

            if (free)
            {
                // 자유 배치: 커서 월드 위치 + 자유 Y회전. 모서리 스냅/중복 제거 안 함.
                tile = new PlacedTile
                {
                    gridPosition = cell,
                    tileDefinitionId = SelectedWall.tileId,
                    tileDefinition = SelectedWall,
                    rotation = 0,
                    flipX = false,
                    freePlace = true,
                    worldPosition = worldPos,
                    yRotation = CurrentRotation * 15f,
                    id = System.Guid.NewGuid().ToString("N"),
                    level = CurrentLevel
                };
                key = WallKeyFor(tile);
            }
            else
            {
                // 스냅 배치: 동서남북(N/E/S/W) 4방향. CurrentRotation(15° 단위)을 90°로 양자화.
                int wallRot = Mathf.RoundToInt(CurrentRotation / 6f) % 4;
                tile = new PlacedTile
                {
                    gridPosition = cell,
                    tileDefinitionId = SelectedWall.tileId,
                    tileDefinition = SelectedWall,
                    rotation = wallRot,
                    flipX = false,
                    level = CurrentLevel
                };
                key = WallKeyFor(tile);
                // 같은 모서리에 이미 벽이 있으면 교체
                if (_wallObjects.TryGetValue(key, out var old))
                {
                    Destroy(old);
                    _wallObjects.Remove(key);
                }
            }

            EditingMap.PlaceTile(tile, "Walls");

            var wallGo = WallBuilder.CreateWallCube(tile, EditingMap.gridSettings, _wallRoot);
            if (wallGo != null)
                _wallObjects[key] = wallGo;
        }

        /// <summary>벽 GameObject 딕셔너리 키. 자유 배치는 고유 id, 스냅은 "x_y_L{level}_Er".</summary>
        static string WallKeyFor(PlacedTile t)
            => t.freePlace ? $"F{t.id}" : $"{t.gridPosition.x}_{t.gridPosition.y}_L{t.level}_E{t.rotation}";

        /// <summary>
        /// 주어진 셀들에 이미 겹쳐 있는 프랍/건물 중 가장 앞(최대 sortOrder)을 찾아,
        /// 새 오브젝트가 그 바로 앞에 오도록 필요한 override 값을 돌려준다. (겹치는 게 없으면 0)
        /// → 프리뷰에서 "앞"으로 보이던 게 설치 후에도 "앞"이 되도록 z-fighting 타이를 깬다.
        /// </summary>
        int ComputeFrontSortOverride(IList<Vector2Int> cells, int defSortingOffset, Vector2Int anchorCell)
        {
            if (EditingMap == null || cells == null || cells.Count == 0) return 0;
            var cellSet = new HashSet<Vector2Int>(cells);
            int baseSort = IsometricGrid.GetSortingOrder(anchorCell, IsometricGrid.OBJECT_SORT_BASE) + defSortingOffset;
            int maxOther = int.MinValue;

            foreach (var p in EditingMap.props)
            {
                if (p.level != CurrentLevel) continue;
                if (!cellSet.Contains(p.gridPosition)) continue;
                int s = IsometricGrid.GetSortingOrder(p.gridPosition, IsometricGrid.OBJECT_SORT_BASE)
                        + (p.propDefinition != null ? p.propDefinition.sortingOffset : 0) + p.sortingOffsetOverride;
                if (s > maxOther) maxOther = s;
            }
            foreach (var b in EditingMap.buildings)
            {
                if (b.buildingDefinition == null) continue;
                if (b.level != CurrentLevel) continue;
                bool overlap = false;
                foreach (var c in b.buildingDefinition.GetOccupiedCells(b.gridPosition))
                    if (cellSet.Contains(c)) { overlap = true; break; }
                if (!overlap) continue;
                int s = IsometricGrid.GetSortingOrder(b.gridPosition, IsometricGrid.OBJECT_SORT_BASE)
                        + b.buildingDefinition.sortingOffset + b.sortingOffsetOverride;
                if (s > maxOther) maxOther = s;
            }

            if (maxOther == int.MinValue) return 0; // 겹치는 것 없음
            return Mathf.Max(0, (maxOther + 1) - baseSort);
        }

        void PlaceProp(Vector3 worldPos)
        {
            if (SelectedProp == null || SelectedProp.sprite == null) return;
            SaveUndoSnapshot();

            var cell = IsometricGrid.WorldToGrid(worldPos, EditingMap.gridSettings);
            bool free = !SnapToGrid;
            Vector3 finalPos = free ? worldPos : IsometricGrid.GridToWorld(cell, EditingMap.gridSettings);

            // 겹친 것 앞으로 보내는 기본 오프셋 + 사용자 상대 보정
            int autoFront = ComputeFrontSortOverride(new[] { cell }, SelectedProp.sortingOffset, cell);

            var prop = new PlacedProp
            {
                instanceId = System.Guid.NewGuid().ToString("N")[..8],
                gridPosition = cell,
                propDefinitionId = SelectedProp.propId,
                propDefinition = SelectedProp,
                rotation = Mathf.RoundToInt(CurrentRotation),
                freePlace = free,
                worldPosition = finalPos,
                yRotation = CurrentRotation * 15f,
                scale = 1f,
                flipX = CurrentFlipX,
                wallMounted = CurrentWallMount,
                mountHeight = CurrentWallMount ? CurrentMountHeight : 0f,
                groundOffsetOverride = CurrentGroundOffset,
                sortingOffsetOverride = autoFront + _placementSortOffset,
                parentBuildingId = SelectedParentBuildingId,
                level = CurrentLevel
            };

            EditingMap.props.Add(prop);
            SpawnPropVisual(prop);
            UI.RefreshAll();
        }

        void PlaceBuilding(Vector2Int cell)
        {
            PlaceBuildingAt(cell, IsometricGrid.GridToWorld(cell, EditingMap.gridSettings));
        }

        void PlaceBuildingAt(Vector2Int cell, Vector3 worldPos)
        {
            if (SelectedBuilding == null) return;

            // 풋프린트 범위 체크
            var occupied = SelectedBuilding.GetOccupiedCells(cell);
            foreach (var c in occupied)
                if (!EditingMap.gridSettings.IsInBounds(c)) return;

            SaveUndoSnapshot();

            bool free = !SnapToGrid;
            var building = new PlacedBuilding
            {
                instanceId = System.Guid.NewGuid().ToString("N")[..8],
                gridPosition = cell,
                buildingDefinitionId = SelectedBuilding.buildingId,
                buildingDefinition = SelectedBuilding,
                rotation = Mathf.RoundToInt(CurrentRotation),
                freePlace = free,
                worldPosition = free ? worldPos : IsometricGrid.GridToWorld(cell, EditingMap.gridSettings),
                yRotation = CurrentRotation * 15f,
                scale = 1f,
                sortingOffsetOverride = ComputeFrontSortOverride(occupied, SelectedBuilding.sortingOffset, cell) + _placementSortOffset,
                level = CurrentLevel
            };

            EditingMap.buildings.Add(building);
            SpawnBuildingVisual(building);
            UI.RefreshAll();
        }

        void PlaceMapObject(Vector2Int cell, Vector3 worldPos)
        {
            SaveUndoSnapshot();

            bool free = !SnapToGrid;
            int objRot = Mathf.RoundToInt(CurrentRotation);
            var obj = new PlacedMapObject
            {
                instanceId = System.Guid.NewGuid().ToString("N")[..8],
                objectType = SelectedObjectType,
                gridPosition = cell,
                freePlace = free,
                worldPosition = free ? worldPos : IsometricGrid.GridToWorld(cell, EditingMap.gridSettings),
                yRotation = objRot * 15f,
                label = SelectedObjectType.ToString(),
                interactRange = GetDefaultInteractRange(SelectedObjectType),
                promptText = GetDefaultPromptText(SelectedObjectType),
                // Spawn config
                spawnPointType = 0,
                useRegionLoot = SpawnUseRegionLoot,
                containerGridWidth = SpawnContainerWidth,
                containerGridHeight = SpawnContainerHeight,
                containerName = SpawnContainerName,
                enemyUnitKey = SpawnEnemyUnitKey,
                enemyCount = SpawnEnemyCount,
                fixedItemId = SpawnFixedItemId,
                fixedItemCount = SpawnFixedItemCount,
                doorLockType = SpawnDoorLockType,
                doorKeyId = SpawnDoorKeyId,
                doorConsumeKey = SpawnDoorConsumeKey,
                doorQuestId = SpawnDoorQuestId,
                npcId = SpawnNpcId,
                npcDisplayName = SpawnNpcDisplayName,
                // Visual mode
                visualMode = SpawnVisualMode,
                visualTexturePath = SpawnVisualTexturePath,
                effectPrefabPath = SpawnEffectPrefabPath,
                visualScale = SpawnVisualScale,
                // Trigger config
                triggerMode = SpawnTriggerMode,
                triggerTargetScene = SpawnTriggerTargetScene,
                triggerTargetSpawnId = SpawnTriggerTargetSpawnId,
                triggerAutoEnter = SpawnTriggerAutoEnter,
                triggerDelay = SpawnTriggerDelay,
                triggerOneShot = SpawnTriggerOneShot,
                triggerSizeX = SpawnTriggerSizeX,
                triggerSizeY = SpawnTriggerSizeY,
                triggerSizeZ = SpawnTriggerSizeZ,
                teleportX = SpawnTeleportX,
                teleportY = SpawnTeleportY,
                teleportZ = SpawnTeleportZ,
                teleportYRot = SpawnTeleportYRot,
                triggerStorySceneId = SpawnTriggerStorySceneId,
                parentBuildingId = SelectedParentBuildingId,
                level = CurrentLevel,
            };

            // Type-specific label
            switch (SelectedObjectType)
            {
                case MapObjectType.NPC:
                    obj.customData = SpawnNpcId;
                    obj.label = !string.IsNullOrEmpty(SpawnNpcDisplayName) ? SpawnNpcDisplayName
                        : (!string.IsNullOrEmpty(SpawnNpcId) ? $"NPC:{SpawnNpcId}" : "NPC");
                    break;
                case MapObjectType.Door:
                    string[] lockNames = { "열림", "열쇠", "퀘스트", "스위치" };
                    obj.label = $"Door ({lockNames[Mathf.Clamp(SpawnDoorLockType, 0, 3)]})";
                    break;
                case MapObjectType.LootContainer:
                    obj.label = !string.IsNullOrEmpty(SpawnContainerName) ? SpawnContainerName : "LootContainer";
                    break;
                case MapObjectType.EnemySpawn:
                    obj.label = !string.IsNullOrEmpty(SpawnEnemyUnitKey) ? SpawnEnemyUnitKey : "EnemySpawn";
                    break;
                case MapObjectType.Trigger:
                    string[] triggerModeNames = { "씬전환", "로컬이동", "스토리", "커스텀" };
                    obj.label = $"Trigger ({triggerModeNames[Mathf.Clamp(SpawnTriggerMode, 0, 3)]})";
                    if (SpawnTriggerMode == 3) // CustomEvent
                        obj.customData = SpawnTriggerCustomData;
                    break;
            }

            EditingMap.mapObjects.Add(obj);
            SpawnObjectMarker(obj);
            UI.RefreshAll();
        }

        // --- Erase ---

        public void OnEraseAt(Vector2Int cell, Vector3 worldPos)
        {
            if (EditingMap == null) return;
            EraseAt(cell);
        }

        void EraseAt(Vector2Int cell)
        {
            if (EditingMap == null) return;
            ClearEraseHover();
            SaveUndoSnapshot();

            switch (CurrentTool)
            {
                case ToolMode.Tile:
                    // Only erase tiles (not walls, not objects) — 현재 층만
                    EditingMap.RemoveNonWallTilesAt(cell, CurrentLevel);
                    _tileRenderer.RemoveTileObject(cell, CurrentLevel);
                    break;
                case ToolMode.Eraser:
                    // Erase the nearest object of ANY type at that cell
                    EraseNearestAtCell(cell);
                    break;
                case ToolMode.Wall:
                    // 스냅 모서리 벽(양자화된 방향) 제거 — 현재 층만
                    int eraseWallRot = Mathf.RoundToInt(CurrentRotation / 6f) % 4;
                    string key = $"{cell.x}_{cell.y}_L{CurrentLevel}_E{eraseWallRot}";
                    EditingMap.RemoveWallEdge(cell, eraseWallRot, CurrentLevel);
                    if (_wallObjects.TryGetValue(key, out var wallGo))
                    {
                        Destroy(wallGo);
                        _wallObjects.Remove(key);
                    }
                    // 이 셀에 배치된 자유 벽도 함께 제거
                    RemoveFreeWallsAtCell(cell);
                    break;
                case ToolMode.Prop:
                    RemoveNearestProp(IsometricGrid.GridToWorld(cell, EditingMap.gridSettings));
                    break;
                case ToolMode.Building:
                    RemoveBuildingAtCell(cell);
                    break;
                case ToolMode.MapObject:
                    RemoveMapObjectsAtCell(cell);
                    break;
            }
        }

        void RemoveWallsAtCell(Vector2Int cell)
        {
            // 모든 층의 스냅 벽 제거
            for (int lv = 0; lv <= MAX_LEVEL; lv++)
                for (int r = 0; r < 4; r++)
                {
                    string key = $"{cell.x}_{cell.y}_L{lv}_E{r}";
                    if (_wallObjects.TryGetValue(key, out var go))
                    {
                        Destroy(go);
                        _wallObjects.Remove(key);
                    }
                }
            RemoveFreeWallsAtCell(cell);
        }

        /// <summary>해당 셀에 배치된(gridPosition 기준) 자유 배치 벽을 데이터+비주얼에서 제거.</summary>
        void RemoveFreeWallsAtCell(Vector2Int cell)
        {
            var ids = new List<string>();
            foreach (var layer in EditingMap.layers)
                foreach (var t in layer.tiles)
                    if (t.freePlace && t.gridPosition == cell
                        && t.tileDefinition != null && t.tileDefinition.IsWall
                        && !string.IsNullOrEmpty(t.id))
                        ids.Add(t.id);

            foreach (var id in ids)
            {
                foreach (var layer in EditingMap.layers)
                    layer.tiles.RemoveAll(t => t.freePlace && t.id == id);
                string fk = $"F{id}";
                if (_wallObjects.TryGetValue(fk, out var go))
                {
                    Destroy(go);
                    _wallObjects.Remove(fk);
                }
            }
        }

        void RemoveNearestProp(Vector3 worldPos)
        {
            float bestDist = 1.5f;
            PlacedProp bestProp = null;
            foreach (var p in EditingMap.props)
            {
                if (p.level != CurrentLevel) continue;
                float dist = Vector3.Distance(p.GetWorldPosition(EditingMap.gridSettings), worldPos);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestProp = p;
                }
            }

            if (bestProp == null) return;
            EditingMap.props.Remove(bestProp);
            if (_propObjects.TryGetValue(bestProp.instanceId, out var go))
            {
                Destroy(go);
                _propObjects.Remove(bestProp.instanceId);
            }
        }

        void RemoveBuildingAtCell(Vector2Int cell)
        {
            for (int i = EditingMap.buildings.Count - 1; i >= 0; i--)
            {
                var b = EditingMap.buildings[i];
                var def = b.buildingDefinition;
                if (def == null) continue;
                if (b.level != CurrentLevel) continue;

                var occupied = def.GetOccupiedCells(b.gridPosition);
                if (occupied.Contains(cell))
                {
                    if (_buildingObjects.TryGetValue(b.instanceId, out var go))
                    {
                        Destroy(go);
                        _buildingObjects.Remove(b.instanceId);
                    }
                    EditingMap.buildings.RemoveAt(i);
                }
            }
        }

        void RemoveMapObjectsAtCell(Vector2Int cell)
        {
            for (int i = EditingMap.mapObjects.Count - 1; i >= 0; i--)
            {
                var obj = EditingMap.mapObjects[i];
                if (obj.level != CurrentLevel) continue;
                if (obj.gridPosition == cell)
                {
                    if (_objectMarkers.TryGetValue(obj.instanceId, out var go))
                    {
                        Destroy(go);
                        _objectMarkers.Remove(obj.instanceId);
                    }
                    EditingMap.mapObjects.RemoveAt(i);
                }
            }
        }

        void EraseNearestAtCell(Vector2Int cell)
        {
            Vector3 cellWorld = IsometricGrid.GridToWorld(cell, EditingMap.gridSettings);
            float bestDist = float.MaxValue;
            int bestType = -1; // 0=tile, 1=wall, 2=prop, 3=building, 4=mapObject
            int bestIndex = -1;
            int bestRotation = 0;

            // 타일은 바닥 레이어 → 다른 모든 것(벽/프랍/건물/오브젝트)이 없을 때만 지운다.
            // 여기선 존재 여부만 기록하고, 우선순위 판정은 맨 마지막에.
            bool hasTile = false;
            foreach (var layer in EditingMap.layers)
            {
                for (int i = 0; i < layer.tiles.Count; i++)
                {
                    var t = layer.tiles[i];
                    if (t.gridPosition != cell || t.level != CurrentLevel) continue;
                    if (t.tileDefinition != null && t.tileDefinition.IsWall) continue;
                    hasTile = true;
                }
            }

            // Check walls at cell (현재 층)
            for (int r = 0; r < 4; r++)
            {
                string key = $"{cell.x}_{cell.y}_L{CurrentLevel}_E{r}";
                if (_wallObjects.ContainsKey(key))
                {
                    if (0f < bestDist) { bestDist = 0f; bestType = 1; bestRotation = r; }
                }
            }

            // Check props (현재 층)
            for (int i = 0; i < EditingMap.props.Count; i++)
            {
                var p = EditingMap.props[i];
                if (p.level != CurrentLevel) continue;
                float dist = Vector3.Distance(p.GetWorldPosition(EditingMap.gridSettings), cellWorld);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestType = 2;
                    bestIndex = i;
                }
            }

            // Check buildings (현재 층)
            for (int i = 0; i < EditingMap.buildings.Count; i++)
            {
                var b = EditingMap.buildings[i];
                if (b.buildingDefinition == null) continue;
                if (b.level != CurrentLevel) continue;
                var occupied = b.buildingDefinition.GetOccupiedCells(b.gridPosition);
                if (occupied.Contains(cell))
                {
                    float dist = Vector3.Distance(b.GetWorldPosition(EditingMap.gridSettings), cellWorld);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestType = 3;
                        bestIndex = i;
                    }
                }
            }

            // Check map objects (현재 층)
            for (int i = 0; i < EditingMap.mapObjects.Count; i++)
            {
                var obj = EditingMap.mapObjects[i];
                if (obj.level != CurrentLevel) continue;
                if (obj.gridPosition == cell)
                {
                    float dist = Vector3.Distance(obj.GetWorldPosition(EditingMap.gridSettings), cellWorld);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestType = 4;
                        bestIndex = i;
                    }
                }
            }

            // 다른 대상이 없을 때만 타일을 지운다 (타일 = 최하위 우선순위)
            if (bestType == -1 && hasTile) bestType = 0;

            // Execute erase for nearest
            switch (bestType)
            {
                case 0:
                    EditingMap.RemoveNonWallTilesAt(cell, CurrentLevel);
                    _tileRenderer.RemoveTileObject(cell, CurrentLevel);
                    break;
                case 1:
                    string wKey = $"{cell.x}_{cell.y}_L{CurrentLevel}_E{bestRotation}";
                    EditingMap.RemoveWallEdge(cell, bestRotation, CurrentLevel);
                    if (_wallObjects.TryGetValue(wKey, out var wallGo))
                    {
                        Destroy(wallGo);
                        _wallObjects.Remove(wKey);
                    }
                    break;
                case 2:
                    var prop = EditingMap.props[bestIndex];
                    EditingMap.props.RemoveAt(bestIndex);
                    if (_propObjects.TryGetValue(prop.instanceId, out var propGo))
                    {
                        Destroy(propGo);
                        _propObjects.Remove(prop.instanceId);
                    }
                    break;
                case 3:
                    var building = EditingMap.buildings[bestIndex];
                    if (_buildingObjects.TryGetValue(building.instanceId, out var buildGo))
                    {
                        Destroy(buildGo);
                        _buildingObjects.Remove(building.instanceId);
                    }
                    EditingMap.buildings.RemoveAt(bestIndex);
                    break;
                case 4:
                    var mObj = EditingMap.mapObjects[bestIndex];
                    if (_objectMarkers.TryGetValue(mObj.instanceId, out var markerGo))
                    {
                        Destroy(markerGo);
                        _objectMarkers.Remove(mObj.instanceId);
                    }
                    EditingMap.mapObjects.RemoveAt(bestIndex);
                    break;
            }

            UI.RefreshAll();
        }

        // --- Move Mode ---

        public void UpdateMoveHover(Vector2Int cell, Vector3 worldPos)
        {
            if (EditingMap == null) { ClearMoveHover(); return; }
            if (_moveGrabbed) return; // 이미 집은 상태면 호버 불필요

            Vector3 cellWorld = IsometricGrid.GridToWorld(cell, EditingMap.gridSettings);
            float bestDist = float.MaxValue;
            int bestType = -1;
            int bestIndex = -1;
            int bestRotation = 0;
            string info = null;
            string targetId = null;

            // Props (현재 층)
            for (int i = 0; i < EditingMap.props.Count; i++)
            {
                var p = EditingMap.props[i];
                if (p.level != CurrentLevel) continue;
                float dist = Vector3.Distance(p.GetWorldPosition(EditingMap.gridSettings), cellWorld);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestType = 2; bestIndex = i;
                    targetId = p.instanceId;
                    string name = p.propDefinition != null ? (p.propDefinition.displayName ?? p.propDefinitionId) : p.propDefinitionId;
                    info = $"프롭: {name}";
                }
            }
            // Buildings (현재 층)
            for (int i = 0; i < EditingMap.buildings.Count; i++)
            {
                var b = EditingMap.buildings[i];
                if (b.buildingDefinition == null) continue;
                if (b.level != CurrentLevel) continue;
                var occupied = b.buildingDefinition.GetOccupiedCells(b.gridPosition);
                if (occupied.Contains(cell))
                {
                    float dist = Vector3.Distance(b.GetWorldPosition(EditingMap.gridSettings), cellWorld);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestType = 3; bestIndex = i;
                        targetId = b.instanceId;
                        info = $"건물: {(b.buildingDefinition.displayName ?? b.buildingDefinitionId)}";
                    }
                }
            }
            // Map Objects (현재 층)
            for (int i = 0; i < EditingMap.mapObjects.Count; i++)
            {
                var obj = EditingMap.mapObjects[i];
                if (obj.level != CurrentLevel) continue;
                float dist = Vector3.Distance(obj.GetWorldPosition(EditingMap.gridSettings), cellWorld);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestType = 4; bestIndex = i;
                    targetId = obj.instanceId;
                    info = $"{obj.objectType}: {obj.label}";
                }
            }
            // Walls (스냅 모서리) — 현재 층
            for (int r = 0; r < 4; r++)
            {
                string wKey = $"{cell.x}_{cell.y}_L{CurrentLevel}_E{r}";
                if (_wallObjects.ContainsKey(wKey) && 0f < bestDist)
                {
                    bestDist = 0f; bestType = 1; bestRotation = r;
                    targetId = wKey;
                    info = "벽";
                }
            }
            // 자유 배치 벽 (커서 근처 월드 거리)
            var mFreeWall = FindFreeWallNear(worldPos, EditingMap.gridSettings.tileSize * 0.6f, out var mFreeKey);
            if (mFreeWall != null && bestDist > 0f)
            {
                bestDist = 0f; bestType = 1; bestRotation = 0;
                targetId = mFreeKey;
                info = "벽(자유)";
            }

            if (targetId == _moveHoverInstanceId) { MoveHoverInfo = info; return; }
            ClearMoveHover();
            _moveHoverInstanceId = targetId;
            _moveType = bestType;
            MoveHoverInfo = info;

            // 초록색 하이라이트
            GameObject targetGo = null;
            if (bestType == 1) _wallObjects.TryGetValue(targetId, out targetGo);
            else if (bestType == 2 && targetId != null) _propObjects.TryGetValue(targetId, out targetGo);
            else if (bestType == 3 && targetId != null) _buildingObjects.TryGetValue(targetId, out targetGo);
            else if (bestType == 4 && targetId != null) _objectMarkers.TryGetValue(targetId, out targetGo);

            if (targetGo != null)
            {
                foreach (var r in targetGo.GetComponentsInChildren<Renderer>())
                {
                    if (r.material == null) continue;
                    string prop = GetColorPropName(r.material);
                    if (prop == null) continue;
                    _moveHoverRenderers.Add((r, prop, r.material.GetColor(prop)));
                    r.material.SetColor(prop, new Color(0.2f, 1f, 0.3f, 0.9f));
                }
            }
        }

        public void ClearMoveHover()
        {
            foreach (var (rend, prop, orig) in _moveHoverRenderers)
            {
                if (rend != null && rend.material != null && rend.material.HasProperty(prop))
                    rend.material.SetColor(prop, orig);
            }
            _moveHoverRenderers.Clear();
            _moveHoverInstanceId = null;
            MoveHoverInfo = null;
        }

        /// <summary>이동 모드에서 클릭 — 집기 또는 놓기</summary>
        public void OnMoveClick(Vector2Int cell, Vector3 worldPos)
        {
            if (_moveGrabbed)
            {
                // 놓기: 현재 위치에 데이터 확정
                DropMove(cell, worldPos);
            }
            else if (!string.IsNullOrEmpty(_moveHoverInstanceId))
            {
                // 집기
                GrabMove(cell);
            }
        }

        void GrabMove(Vector2Int cell)
        {
            SaveUndoSnapshot();
            _moveGrabbed = true;
            _moveInstanceId = _moveHoverInstanceId;

            // 집은 오브젝트의 현재 각도를 CurrentRotation에 반영
            switch (_moveType)
            {
                case 2: // Prop
                    var p = EditingMap.props.Find(x => x.instanceId == _moveInstanceId);
                    if (p != null)
                    {
                        CurrentRotation = p.yRotation / 15f;
                        CurrentFlipX = p.flipX;
                        CurrentWallMount = p.wallMounted;
                        if (p.wallMounted && p.mountHeight > 0f) CurrentMountHeight = p.mountHeight;
                    }
                    break;
                case 3: // Building
                    var b = EditingMap.buildings.Find(x => x.instanceId == _moveInstanceId);
                    if (b != null) CurrentRotation = b.yRotation / 15f;
                    break;
                case 4: // MapObject
                    var o = EditingMap.mapObjects.Find(x => x.instanceId == _moveInstanceId);
                    if (o != null) CurrentRotation = o.yRotation / 15f;
                    break;
                case 1: // Wall
                {
                    var wt = FindWallTileByKey(_moveInstanceId);
                    if (wt != null && wt.freePlace)
                    {
                        // 자유 벽: 자유 Y회전 복원 (15° 단위 CurrentRotation으로 환산)
                        _moveWallRotation = 0;
                        CurrentRotation = wt.yRotation / 15f;
                    }
                    else
                    {
                        // 스냅 벽: 모서리 회전(0~3)을 90°(6스텝) 단위 CurrentRotation으로 환산
                        _moveWallRotation = wt != null ? wt.rotation : 0;
                        CurrentRotation = _moveWallRotation * 6f;
                    }
                    break;
                }
            }

            ClearMoveHover();
            MoveHoverInfo = "이동 중… (클릭: 놓기, ESC: 취소, Q/E: 회전)";
            UI.RefreshStatus();
        }

        /// <summary>이동 중 매 프레임 마우스 위치로 오브젝트 비주얼 이동</summary>
        public void UpdateMovePosition(Vector2Int cell, Vector3 worldPos)
        {
            if (!_moveGrabbed) return;

            Vector3 finalPos = SnapToGrid
                ? IsometricGrid.GridToWorld(cell, EditingMap.gridSettings)
                : worldPos;

            GameObject go = null;
            switch (_moveType)
            {
                case 1: _wallObjects.TryGetValue(_moveInstanceId, out go); break;
                case 2: _propObjects.TryGetValue(_moveInstanceId, out go); break;
                case 3: _buildingObjects.TryGetValue(_moveInstanceId, out go); break;
                case 4: _objectMarkers.TryGetValue(_moveInstanceId, out go); break;
            }

            if (go != null)
            {
                // 이동 중에도 해당 항목의 층(level) 높이를 유지한다.
                float levelElev = 0f;
                switch (_moveType)
                {
                    case 2: { var mp = EditingMap.props.Find(x => x.instanceId == _moveInstanceId); if (mp != null) levelElev = mp.ElevationY(EditingMap.gridSettings); break; }
                    case 3: { var mb = EditingMap.buildings.Find(x => x.instanceId == _moveInstanceId); if (mb != null) levelElev = mb.ElevationY(EditingMap.gridSettings); break; }
                    case 4: { var mo = EditingMap.mapObjects.Find(x => x.instanceId == _moveInstanceId); if (mo != null) levelElev = mo.ElevationY(EditingMap.gridSettings); break; }
                }
                float yOffset = _moveType == 1
                    ? go.transform.position.y  // 벽은 Y축 유지
                    : (_moveType == 4 && go.transform.childCount > 0 ? go.transform.position.y : levelElev);
                // 벽 부착 프롭은 드래그 중에도 높이 유지 + 층 높이
                float propY = levelElev;
                Vector3 propOffset = Vector3.zero; // 프롭 접지 보정(내부 Content에 적용)
                if (_moveType == 2)
                {
                    var mp = EditingMap.props.Find(x => x.instanceId == _moveInstanceId);
                    if (mp != null)
                    {
                        if (mp.wallMounted) propY += mp.mountHeight;
                        propOffset = mp.GroundOffsetVec;
                    }
                }
                go.transform.position = new Vector3(finalPos.x, _moveType == 1 ? yOffset : propY, finalPos.z);
                // 접지 보정은 root가 아니라 내부 Content(이미지+콜라이더)에 적용 — root는 셀 바닥점 유지.
                if (_moveType == 2) PropQuadBuilder.ApplyGroundOffset(go, propOffset);

                // 벽은 높이 보정
                if (_moveType == 1)
                {
                    var wallChild = go.transform.Find("WallCube") ?? go.transform.Find("WallVisual")
                        ?? (go.transform.childCount > 0 ? go.transform.GetChild(0) : null);
                    if (wallChild != null)
                    {
                        var wtTile = FindWallTileByKey(_moveInstanceId);
                        float wallLevelY = wtTile != null ? wtTile.level * EditingMap.gridSettings.levelHeight : 0f;
                        go.transform.position = new Vector3(finalPos.x, wallLevelY + wallChild.localScale.y * 0.5f, finalPos.z);
                    }
                }

                // 프랍/건물: 그림 순서(정렬)를 실시간 반영 — sortingOrder + 시선축 깊이 오프셋
                if (_moveType == 2 || _moveType == 3)
                {
                    int defOffset = 0, overrideOffset = 0;
                    if (_moveType == 2)
                    {
                        var p = EditingMap.props.Find(x => x.instanceId == _moveInstanceId);
                        if (p != null) { defOffset = p.propDefinition != null ? p.propDefinition.sortingOffset : 0; overrideOffset = p.sortingOffsetOverride; }
                    }
                    else
                    {
                        var b = EditingMap.buildings.Find(x => x.instanceId == _moveInstanceId);
                        if (b != null) { defOffset = b.buildingDefinition != null ? b.buildingDefinition.sortingOffset : 0; overrideOffset = b.sortingOffsetOverride; }
                    }
                    int sortOrder = IsometricGrid.GetSortingOrder(cell, IsometricGrid.OBJECT_SORT_BASE) + defOffset + overrideOffset;
                    foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>()) sr.sortingOrder = sortOrder;
                    foreach (var mr in go.GetComponentsInChildren<MeshRenderer>()) mr.sortingOrder = sortOrder;
                    // 불투명 메시(건물)만 시선축 깊이 오프셋. 스프라이트 프롭은 sortingOrder로 정렬돼 오프셋 불필요(띄움 방지).
                    if (_moveType == 3)
                        go.transform.position += IsometricGrid.SortDepthOffset(sortOrder);
                }
            }
        }

        void ApplyMoveRotation()
        {
            if (!_moveGrabbed) return;
            float yRot = CurrentRotation * 15f;

            GameObject go = null;
            switch (_moveType)
            {
                case 1: _wallObjects.TryGetValue(_moveInstanceId, out go); break;
                case 2: _propObjects.TryGetValue(_moveInstanceId, out go); break;
                case 3: _buildingObjects.TryGetValue(_moveInstanceId, out go); break;
                case 4: _objectMarkers.TryGetValue(_moveInstanceId, out go); break;
            }
            if (go == null) return;

            // 프리팹 원본 회전에 yRotation을 곱함
            switch (_moveType)
            {
                case 1:
                {
                    // 자유 벽만 이동 중 회전 반영 (스냅 벽은 모서리 고정).
                    var wt = FindWallTileByKey(_moveInstanceId);
                    if (wt != null && wt.freePlace)
                        go.transform.rotation = Quaternion.Euler(0, yRot, 0);
                    break;
                }
                case 2:
                {
                    // 프롭 회전: 벽 부착이면 빌보드 끄고 yaw만, 아니면 빌보드×yaw.
                    var mp = EditingMap.props.Find(x => x.instanceId == _moveInstanceId);
                    bool mounted = mp != null ? mp.wallMounted : CurrentWallMount;
                    go.transform.rotation = PropQuadBuilder.RootRotation(yRot, mounted);
                    break;
                }
                case 3:
                    var b = EditingMap.buildings.Find(x => x.instanceId == _moveInstanceId);
                    if (b?.buildingDefinition?.prefab != null)
                    {
                        var baseRot = b.buildingDefinition.prefab.transform.rotation;
                        go.transform.rotation = Quaternion.Euler(0, yRot, 0) * baseRot;
                    }
                    else go.transform.rotation = Quaternion.Euler(0, yRot, 0);
                    break;
                case 4:
                    go.transform.rotation = Quaternion.Euler(0, yRot, 0);
                    break;
            }
        }

        void DropMove(Vector2Int cell, Vector3 worldPos)
        {
            Vector3 finalPos = SnapToGrid
                ? IsometricGrid.GridToWorld(cell, EditingMap.gridSettings)
                : worldPos;

            float yRot = CurrentRotation * 15f;

            switch (_moveType)
            {
                case 2: // Prop
                {
                    var p = EditingMap.props.Find(x => x.instanceId == _moveInstanceId);
                    if (p != null)
                    {
                        p.gridPosition = cell;
                        p.worldPosition = finalPos;
                        p.freePlace = !SnapToGrid;
                        p.yRotation = yRot;
                        p.rotation = Mathf.RoundToInt(CurrentRotation);
                        p.flipX = CurrentFlipX;
                        p.wallMounted = CurrentWallMount;
                        p.mountHeight = CurrentWallMount ? CurrentMountHeight : 0f;
                    }
                    break;
                }
                case 3: // Building
                {
                    var b = EditingMap.buildings.Find(x => x.instanceId == _moveInstanceId);
                    if (b != null)
                    {
                        b.gridPosition = cell;
                        b.worldPosition = finalPos;
                        b.freePlace = !SnapToGrid;
                        b.yRotation = yRot;
                        b.rotation = Mathf.RoundToInt(CurrentRotation);
                    }
                    break;
                }
                case 4: // MapObject
                {
                    var o = EditingMap.mapObjects.Find(x => x.instanceId == _moveInstanceId);
                    if (o != null)
                    {
                        o.gridPosition = cell;
                        o.worldPosition = finalPos;
                        o.freePlace = !SnapToGrid;
                        o.yRotation = yRot;
                    }
                    break;
                }
                case 1: // Wall — 원래 위치의 벽 데이터 삭제 후 새 위치에 재배치
                {
                    // 원래 벽 데이터 찾아서 제거 (스냅/자유 키 모두 지원)
                    PlacedTile wallTile = null;
                    string layerName = null;
                    foreach (var layer in EditingMap.layers)
                    {
                        for (int i = layer.tiles.Count - 1; i >= 0; i--)
                        {
                            var t = layer.tiles[i];
                            if (t.tileDefinition != null && t.tileDefinition.IsWall
                                && WallKeyFor(t) == _moveInstanceId)
                            {
                                wallTile = t;
                                layerName = layer.layerName;
                                layer.tiles.RemoveAt(i);
                                break;
                            }
                        }
                        if (wallTile != null) break;
                    }

                    // 비주얼 제거
                    if (_wallObjects.TryGetValue(_moveInstanceId, out var oldGo))
                    {
                        Destroy(oldGo);
                        _wallObjects.Remove(_moveInstanceId);
                    }

                    if (wallTile != null)
                    {
                        if (!SnapToGrid)
                        {
                            // 자유 배치: 월드 위치 + 자유 Y회전
                            wallTile.freePlace = true;
                            wallTile.gridPosition = cell;
                            wallTile.worldPosition = finalPos;
                            wallTile.yRotation = yRot;
                            wallTile.rotation = 0;
                            if (string.IsNullOrEmpty(wallTile.id))
                                wallTile.id = System.Guid.NewGuid().ToString("N");
                        }
                        else
                        {
                            // 스냅 배치: 셀 모서리 (동서남북)
                            wallTile.freePlace = false;
                            wallTile.gridPosition = cell;
                            wallTile.rotation = Mathf.RoundToInt(CurrentRotation / 6f) % 4;
                        }
                        EditingMap.PlaceTile(wallTile, layerName ?? "Walls");
                        var wallGo = WallBuilder.CreateWallCube(wallTile, EditingMap.gridSettings, _wallRoot);
                        if (wallGo != null)
                            _wallObjects[WallKeyFor(wallTile)] = wallGo;
                    }
                    break;
                }
            }

            _moveGrabbed = false;
            _moveInstanceId = null;
            _moveType = -1;
            MoveHoverInfo = null;
            UI.RefreshAll();
        }

        public void CancelMove()
        {
            if (!_moveGrabbed) return;
            // Undo로 복원
            Undo();
            _moveGrabbed = false;
            _moveInstanceId = null;
            _moveType = -1;
            MoveHoverInfo = null;
        }

        public bool IsMovingObject => _moveGrabbed;

        // --- Hierarchy Actions ---

        public void SelectPlacedObject(string instanceId)
        {
            if (EditingMap == null) return;

            // Search buildings
            foreach (var b in EditingMap.buildings)
            {
                if (b.instanceId == instanceId)
                {
                    BuilderCamera.SetFocusPoint(b.GetWorldPosition(EditingMap.gridSettings));
                    return;
                }
            }
            // Search props
            foreach (var p in EditingMap.props)
            {
                if (p.instanceId == instanceId)
                {
                    BuilderCamera.SetFocusPoint(p.GetWorldPosition(EditingMap.gridSettings));
                    return;
                }
            }
            // Search map objects
            foreach (var o in EditingMap.mapObjects)
            {
                if (o.instanceId == instanceId)
                {
                    BuilderCamera.SetFocusPoint(o.GetWorldPosition(EditingMap.gridSettings));
                    return;
                }
            }
        }

        public void DeletePlacedObject(string instanceId)
        {
            if (EditingMap == null) return;
            SaveUndoSnapshot();

            // Search buildings
            for (int i = EditingMap.buildings.Count - 1; i >= 0; i--)
            {
                if (EditingMap.buildings[i].instanceId == instanceId)
                {
                    if (_buildingObjects.TryGetValue(instanceId, out var go))
                    {
                        Destroy(go);
                        _buildingObjects.Remove(instanceId);
                    }
                    EditingMap.buildings.RemoveAt(i);
                    UI.RefreshAll();
                    return;
                }
            }
            // Search props
            for (int i = EditingMap.props.Count - 1; i >= 0; i--)
            {
                if (EditingMap.props[i].instanceId == instanceId)
                {
                    if (_propObjects.TryGetValue(instanceId, out var go))
                    {
                        Destroy(go);
                        _propObjects.Remove(instanceId);
                    }
                    EditingMap.props.RemoveAt(i);
                    UI.RefreshAll();
                    return;
                }
            }
            // Search map objects
            for (int i = EditingMap.mapObjects.Count - 1; i >= 0; i--)
            {
                if (EditingMap.mapObjects[i].instanceId == instanceId)
                {
                    if (_objectMarkers.TryGetValue(instanceId, out var go))
                    {
                        Destroy(go);
                        _objectMarkers.Remove(instanceId);
                    }
                    EditingMap.mapObjects.RemoveAt(i);
                    UI.RefreshAll();
                    return;
                }
            }
        }

        /// <summary>선택된 오브젝트 삭제 (Delete/Backspace 키)</summary>
        public void DeleteSelectedObject()
        {
            if (EditingMap == null) return;
            // 현재 호버 중인 대상 삭제
            if (!string.IsNullOrEmpty(_eraseHoverInstanceId))
            {
                string id = _eraseHoverInstanceId;
                ClearEraseHover();
                DeletePlacedObject(id);
                return;
            }
            // Fallback: delete last MapObject
            if (EditingMap.mapObjects.Count > 0)
            {
                var last = EditingMap.mapObjects[^1];
                DeletePlacedObject(last.instanceId);
            }
        }

        /// <summary>
        /// 그림 순서(정렬 오프셋)를 조절한다. ([ / ] 키, 화면 -/+ 버튼)
        ///  - 이동 모드에서 프랍/건물을 집은 상태: 그 오브젝트의 오프셋을 즉시 변경(미리보기 반영)
        ///  - 배치(Prop/Building) 모드: 다음에 놓을 오브젝트의 오프셋(_placementSortOffset)을 변경
        /// 불투명 3D 메시는 시선축 깊이 오프셋으로 변환되어 같은 셀에 겹친 조각의 앞뒤가 바뀐다.
        /// </summary>
        public void NudgeHoverSortOffset(int delta)
        {
            if (EditingMap == null) return;

            // 1) 이동 모드: 집은 프랍/건물의 오프셋을 직접 변경
            if (_moveGrabbed && _moveType == 2)
            {
                var p = EditingMap.props.Find(x => x.instanceId == _moveInstanceId);
                if (p != null)
                {
                    p.sortingOffsetOverride += delta;
                    UI?.RefreshStatus();
                }
                return;
            }
            if (_moveGrabbed && _moveType == 3)
            {
                var b = EditingMap.buildings.Find(x => x.instanceId == _moveInstanceId);
                if (b != null)
                {
                    b.sortingOffsetOverride += delta;
                    UI?.RefreshStatus();
                }
                return;
            }

            // 2) 배치 모드: 다음에 놓을 오브젝트의 오프셋만 조정
            if (IsPlacementSortTarget)
            {
                _placementSortOffset += delta;
                UI?.RefreshStatus();
            }
        }

        /// <summary>배치용 정렬 오프셋 초기화 (X 버튼)</summary>
        public void ClearSortTarget()
        {
            _placementSortOffset = 0;
            UI?.RefreshStatus();
        }

        // --- Eraser Hover ---

        public void UpdateEraseHover(Vector2Int cell, Vector3 worldPos)
        {
            if (EditingMap == null) { ClearEraseHover(); return; }

            Vector3 cellWorld = IsometricGrid.GridToWorld(cell, EditingMap.gridSettings);
            float bestDist = float.MaxValue;
            int bestType = -1; // 0=tile, 1=wall, 2=prop, 3=building, 4=mapObject
            int bestIndex = -1;
            int bestRotation = 0;
            string info = null;

            // 타일은 바닥 레이어 → 최하위 우선순위. 존재 여부/표시명만 기록.
            bool hasTile = false;
            string tileInfo = null;
            foreach (var layer in EditingMap.layers)
            {
                for (int i = 0; i < layer.tiles.Count; i++)
                {
                    var t = layer.tiles[i];
                    if (t.gridPosition != cell || t.level != CurrentLevel) continue;
                    if (t.tileDefinition != null && t.tileDefinition.IsWall) continue;
                    hasTile = true;
                    if (tileInfo == null) tileInfo = $"타일: {t.tileDefinitionId}";
                }
            }

            // Check walls — 현재 층
            for (int r = 0; r < 4; r++)
            {
                string key = $"{cell.x}_{cell.y}_L{CurrentLevel}_E{r}";
                if (_wallObjects.ContainsKey(key))
                {
                    if (0f < bestDist)
                    {
                        bestDist = 0f; bestType = 1; bestRotation = r;
                        // find wall tile definition
                        foreach (var layer in EditingMap.layers)
                            foreach (var t in layer.tiles)
                                if (t.gridPosition == cell && t.level == CurrentLevel && t.rotation == r && t.tileDefinition != null && t.tileDefinition.IsWall)
                                    info = $"벽: {t.tileDefinitionId}";
                        if (info == null) info = "벽";
                    }
                }
            }

            // Check props (현재 층)
            for (int i = 0; i < EditingMap.props.Count; i++)
            {
                var p = EditingMap.props[i];
                if (p.level != CurrentLevel) continue;
                float dist = Vector3.Distance(p.GetWorldPosition(EditingMap.gridSettings), cellWorld);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestType = 2;
                    bestIndex = i;
                    string name = p.propDefinition != null ? (p.propDefinition.displayName ?? p.propDefinitionId) : p.propDefinitionId;
                    info = $"프롭: {name}";
                }
            }

            // Check buildings (현재 층)
            for (int i = 0; i < EditingMap.buildings.Count; i++)
            {
                var b = EditingMap.buildings[i];
                if (b.buildingDefinition == null) continue;
                if (b.level != CurrentLevel) continue;
                var occupied = b.buildingDefinition.GetOccupiedCells(b.gridPosition);
                if (occupied.Contains(cell))
                {
                    float dist = Vector3.Distance(b.GetWorldPosition(EditingMap.gridSettings), cellWorld);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestType = 3;
                        bestIndex = i;
                        string name = b.buildingDefinition.displayName ?? b.buildingDefinitionId;
                        info = $"건물: {name}";
                    }
                }
            }

            // Check map objects (현재 층)
            for (int i = 0; i < EditingMap.mapObjects.Count; i++)
            {
                var obj = EditingMap.mapObjects[i];
                if (obj.level != CurrentLevel) continue;
                if (obj.gridPosition == cell)
                {
                    float dist = Vector3.Distance(obj.GetWorldPosition(EditingMap.gridSettings), cellWorld);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestType = 4;
                        bestIndex = i;
                        info = $"{obj.objectType}: {obj.label}";
                    }
                }
            }

            // 다른 대상이 없을 때만 타일을 하이라이트 (타일 = 최하위 우선순위)
            if (bestType == -1 && hasTile) { bestType = 0; info = tileInfo; }

            // Determine the target instanceId for highlighting
            string targetId = null;
            switch (bestType)
            {
                case 2: targetId = EditingMap.props[bestIndex].instanceId; break;
                case 3: targetId = EditingMap.buildings[bestIndex].instanceId; break;
                case 4: targetId = EditingMap.mapObjects[bestIndex].instanceId; break;
            }

            // Skip if same target
            if (targetId == _eraseHoverInstanceId && EraseHoverInfo == info) { EraseHoverInfo = info; return; }

            ClearEraseHover();
            EraseHoverInfo = info;
            _eraseHoverInstanceId = targetId;

            // Highlight 3D object with red tint
            GameObject targetGo = null;
            if (targetId != null)
            {
                if (bestType == 2) _propObjects.TryGetValue(targetId, out targetGo);
                else if (bestType == 3) _buildingObjects.TryGetValue(targetId, out targetGo);
                else if (bestType == 4) _objectMarkers.TryGetValue(targetId, out targetGo);
            }
            else if (bestType == 1)
            {
                string wKey = $"{cell.x}_{cell.y}_L{CurrentLevel}_E{bestRotation}";
                _wallObjects.TryGetValue(wKey, out targetGo);
            }

            if (targetGo != null)
            {
                var renderers = targetGo.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    if (r.material == null) continue;
                    string prop = GetColorPropName(r.material);
                    if (prop == null) continue; // 색 프로퍼티 없는 셰이더는 건너뜀
                    _eraseHoverRenderers.Add((r, prop, r.material.GetColor(prop)));
                    r.material.SetColor(prop, new Color(1f, 0.25f, 0.25f, 0.9f));
                }
            }
        }

        public void ClearEraseHover()
        {
            foreach (var (rend, prop, orig) in _eraseHoverRenderers)
            {
                if (rend != null && rend.material != null && rend.material.HasProperty(prop))
                    rend.material.SetColor(prop, orig);
            }
            _eraseHoverRenderers.Clear();
            _eraseHoverInstanceId = null;
            EraseHoverInfo = null;
        }

        // --- Resize Mode ---

        public void ToggleResizeMode()
        {
            ResizeMode = !ResizeMode;
            if (!ResizeMode) ClearResizeHover();
            UI.RefreshAll();
        }

        public void UpdateResizeHover(Vector2Int cell, Vector3 worldPos)
        {
            if (EditingMap == null) { ClearResizeHover(); return; }

            Vector3 cellWorld = IsometricGrid.GridToWorld(cell, EditingMap.gridSettings);
            float bestDist = 2f;
            string targetId = null;
            string info = null;
            int targetType = -1; // 0=prop, 1=building, 2=mapObject

            // Props (현재 층)
            for (int i = 0; i < EditingMap.props.Count; i++)
            {
                var p = EditingMap.props[i];
                if (p.level != CurrentLevel) continue;
                float dist = Vector3.Distance(p.GetWorldPosition(EditingMap.gridSettings), cellWorld);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    targetId = p.instanceId;
                    string name = p.propDefinition != null ? (p.propDefinition.displayName ?? p.propDefinitionId) : p.propDefinitionId;
                    info = $"프롭: {name}  (x{p.scale:F2})";
                    targetType = 0;
                }
            }

            // Buildings (현재 층)
            for (int i = 0; i < EditingMap.buildings.Count; i++)
            {
                var b = EditingMap.buildings[i];
                if (b.level != CurrentLevel) continue;
                float dist = Vector3.Distance(b.GetWorldPosition(EditingMap.gridSettings), cellWorld);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    targetId = b.instanceId;
                    string name = b.buildingDefinition != null ? (b.buildingDefinition.displayName ?? b.buildingDefinitionId) : b.buildingDefinitionId;
                    info = $"건물: {name}  (x{b.scale:F2})";
                    targetType = 1;
                }
            }

            // Walls (호버한 셀의 모서리 벽) — 비율 배율로 크기 조절 (현재 층)
            for (int r = 0; r < 4; r++)
            {
                string wKey = $"{cell.x}_{cell.y}_L{CurrentLevel}_E{r}";
                if (_wallObjects.ContainsKey(wKey) && 0f < bestDist)
                {
                    bestDist = 0f;
                    targetId = wKey;
                    var wt = FindWallTile(cell, r, CurrentLevel);
                    float ws = wt != null ? (wt.wallScale <= 0f ? 1f : wt.wallScale) : 1f;
                    info = $"벽  (x{ws:F2})";
                    targetType = 3;
                }
            }

            // 자유 배치 벽 — 커서 근처(월드 거리)로 검출
            var freeWall = FindFreeWallNear(worldPos, EditingMap.gridSettings.tileSize * 0.6f, out var freeKey);
            if (freeWall != null && bestDist > 0f)
            {
                bestDist = 0f;
                targetId = freeKey;
                float ws = freeWall.wallScale <= 0f ? 1f : freeWall.wallScale;
                info = $"벽(자유)  (x{ws:F2})";
                targetType = 3;
            }

            // MapObject markers (sphere markers have no meaningful scale, skip for now)

            if (targetId == _resizeHoverInstanceId) { ResizeHoverInfo = info; return; }

            ClearResizeHover();
            _resizeHoverInstanceId = targetId;
            ResizeHoverInfo = info;

            // Highlight with blue tint
            GameObject targetGo = null;
            if (targetType == 0 && targetId != null) _propObjects.TryGetValue(targetId, out targetGo);
            else if (targetType == 1 && targetId != null) _buildingObjects.TryGetValue(targetId, out targetGo);
            else if (targetType == 3 && targetId != null) _wallObjects.TryGetValue(targetId, out targetGo);

            if (targetGo != null)
            {
                var renderers = targetGo.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    if (r.material == null) continue;
                    string prop = GetColorPropName(r.material);
                    if (prop == null) continue;
                    _resizeHoverRenderers.Add((r, prop, r.material.GetColor(prop)));
                    r.material.SetColor(prop, new Color(0.3f, 0.6f, 1f, 0.9f));
                }
            }
        }

        public void ClearResizeHover()
        {
            foreach (var (rend, prop, orig) in _resizeHoverRenderers)
            {
                if (rend != null && rend.material != null && rend.material.HasProperty(prop))
                    rend.material.SetColor(prop, orig);
            }
            _resizeHoverRenderers.Clear();
            _resizeHoverInstanceId = null;
            ResizeHoverInfo = null;
        }

        public void AdjustResizeScale(float delta)
        {
            if (EditingMap == null || string.IsNullOrEmpty(_resizeHoverInstanceId)) return;

            SaveUndoSnapshot();

            // Props
            foreach (var p in EditingMap.props)
            {
                if (p.instanceId == _resizeHoverInstanceId)
                {
                    p.scale = Mathf.Max(0.1f, p.scale + delta);
                    if (_propObjects.TryGetValue(p.instanceId, out var go))
                        go.transform.localScale = Vector3.one * p.scale;
                    ResizeHoverInfo = $"프롭: {(p.propDefinition?.displayName ?? p.propDefinitionId)}  (x{p.scale:F2})";
                    return;
                }
            }

            // Buildings
            foreach (var b in EditingMap.buildings)
            {
                if (b.instanceId == _resizeHoverInstanceId)
                {
                    b.scale = Mathf.Max(0.1f, b.scale + delta);
                    if (_buildingObjects.TryGetValue(b.instanceId, out var go))
                        go.transform.localScale = Vector3.one * b.scale;
                    ResizeHoverInfo = $"건물: {(b.buildingDefinition?.displayName ?? b.buildingDefinitionId)}  (x{b.scale:F2})";
                    return;
                }
            }

            // Walls — 비율 배율(길이+높이, 두께 제외)로 크기 조절
            if (_wallObjects.TryGetValue(_resizeHoverInstanceId, out var wallGo))
            {
                var wt = FindWallTileByKey(_resizeHoverInstanceId);
                if (wt != null)
                {
                    float cur = wt.wallScale <= 0f ? 1f : wt.wallScale;
                    wt.wallScale = Mathf.Max(0.1f, cur + delta);
                    ApplyWallScaleToGo(wallGo, wt);
                    ResizeHoverInfo = (wt.freePlace ? "벽(자유)" : "벽") + $"  (x{wt.wallScale:F2})";
                }
            }
        }

        /// <summary>벽 키 "x_y_Er" → (셀, 회전) 파싱.</summary>
        // 스냅 벽 키 형식: "x_y_L{level}_E{rot}"
        static (Vector2Int cell, int rot, int level) ParseWallKey(string key)
        {
            var parts = key.Split('_');
            int x = int.Parse(parts[0]);
            int y = int.Parse(parts[1]);
            int level = int.Parse(parts[2].Substring(1)); // 'L' 제거
            int rot = int.Parse(parts[3].Substring(1));    // 'E' 제거
            return (new Vector2Int(x, y), rot, level);
        }

        /// <summary>벽 딕셔너리 키로 PlacedTile을 찾는다. 자유 벽("Fid")은 id로, 스냅 벽은 셀+회전으로.</summary>
        PlacedTile FindWallTileByKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (key.Length > 0 && key[0] == 'F')
            {
                string id = key.Substring(1);
                foreach (var layer in EditingMap.layers)
                    foreach (var t in layer.tiles)
                        if (t.freePlace && t.id == id
                            && t.tileDefinition != null && t.tileDefinition.IsWall)
                            return t;
                return null;
            }
            var (cell, rot, level) = ParseWallKey(key);
            return FindWallTile(cell, rot, level);
        }

        /// <summary>커서 월드 위치(XZ)에서 maxDist 안의 가장 가까운 자유 배치 벽을 찾는다.</summary>
        PlacedTile FindFreeWallNear(Vector3 worldPos, float maxDist, out string key)
        {
            key = null;
            PlacedTile best = null;
            float bestD = maxDist;
            var cursor = new Vector2(worldPos.x, worldPos.z);
            foreach (var layer in EditingMap.layers)
                foreach (var t in layer.tiles)
                {
                    if (!t.freePlace || t.tileDefinition == null || !t.tileDefinition.IsWall) continue;
                    float d = Vector2.Distance(cursor, new Vector2(t.worldPosition.x, t.worldPosition.z));
                    if (d < bestD) { bestD = d; best = t; key = WallKeyFor(t); }
                }
            return best;
        }

        /// <summary>주어진 셀+회전+층의 (스냅) 벽 PlacedTile을 모든 레이어에서 찾는다.</summary>
        PlacedTile FindWallTile(Vector2Int cell, int rot, int level = 0)
        {
            foreach (var layer in EditingMap.layers)
                foreach (var t in layer.tiles)
                    if (!t.freePlace && t.gridPosition == cell && t.rotation == rot && t.level == level
                        && t.tileDefinition != null && t.tileDefinition.IsWall)
                        return t;
            return null;
        }

        /// <summary>벽 GameObject에 wallScale을 즉시 반영 (자식 큐브 스케일 + 바닥 정렬 Y). WallBuilder와 동일 공식.</summary>
        void ApplyWallScaleToGo(GameObject go, PlacedTile tile)
        {
            var def = tile.tileDefinition;
            if (def == null || go == null) return;

            float floorY = tile.level * EditingMap.gridSettings.levelHeight;
            float tileSize = EditingMap.gridSettings.tileSize;
            float thickness = def.wallThickness;
            float s = tile.wallScale <= 0f ? 1f : tile.wallScale;
            float scaledHeight = def.wallHeight * s;
            float baseLength = def.wallLength > 0f ? def.wallLength : tileSize;
            float scaledLength = baseLength * s;

            var child = go.transform.Find("WallCube")
                ?? (go.transform.childCount > 0 ? go.transform.GetChild(0) : null);
            if (child != null)
                child.localScale = (tile.freePlace || tile.rotation == 0 || tile.rotation == 2)
                    ? new Vector3(scaledLength, scaledHeight, thickness)
                    : new Vector3(thickness, scaledHeight, scaledLength);

            Vector3 p = go.transform.position;
            go.transform.position = new Vector3(p.x, floorY + scaledHeight * 0.5f, p.z);
        }

        /// <summary>MapObjectType → InteractableObject.InteractType 매핑</summary>
        public static int MapObjectTypeToInteractType(MapObjectType type) => type switch
        {
            MapObjectType.EscapePoint => 1,      // InteractType.ExitPoint
            MapObjectType.LootContainer => 2,     // InteractType.Container
            MapObjectType.NPC => 3,               // InteractType.NPC
            MapObjectType.Note => 4,              // InteractType.Note
            MapObjectType.ItemDrop => 5,           // InteractType.Pickup
            MapObjectType.Bed => 6,               // InteractType.Bed
            MapObjectType.Workbench => 7,          // InteractType.Workbench
            MapObjectType.MapBoard => 8,           // InteractType.MapBoard
            MapObjectType.MedicalBench => 9,       // InteractType.MedicalBench
            MapObjectType.CookingBench => 10,      // InteractType.CookingBench
            MapObjectType.Door => 11,              // InteractType.Door
            MapObjectType.GenericInteract => 0,    // InteractType.Generic
            _ => 0
        };

        public void ToggleSnapToGrid()
        {
            SnapToGrid = !SnapToGrid;
            UI.RefreshStatus();
        }

        // --- Visuals ---

        void SpawnPropVisual(PlacedProp prop)
        {
            var def = prop.propDefinition;
            if (def == null) return;
            // 프롭은 빌보드 쿼드 전용. 스프라이트가 없으면 비주얼이 없으므로 스킵.
            if (!PropQuadBuilder.UsesQuad(def)) return;

            // 스프라이트 프롭은 빌보드 쿼드로 생성. (에디터 미리보기에선 nav 콜라이더 불필요)
            var go = PropQuadBuilder.Build(def, EditingMap.gridSettings, addNavCollider: false);
            go.transform.SetParent(_propRoot);
            go.name = $"Prop_{prop.propDefinitionId}_{prop.instanceId}";
            Vector3 basePos = prop.GetWorldPosition(EditingMap.gridSettings);
            basePos.y += prop.ElevationY(EditingMap.gridSettings);
            if (prop.wallMounted) basePos += Vector3.up * prop.mountHeight;
            go.transform.position = basePos;
            // 회전: 벽 부착이면 빌보드 끄고 yaw만, 아니면 빌보드×yaw.
            go.transform.rotation = PropQuadBuilder.RootRotation(prop.yRotation, prop.wallMounted);
            go.transform.localScale = Vector3.one * prop.scale;
            PropQuadBuilder.ApplyFlip(go, prop.flipX);
            PropQuadBuilder.ApplyGroundOffset(go, prop.GroundOffsetVec); // 접지 보정 — 내부 Content(이미지+콜라이더)만

            // 스프라이트 정렬 순서 (바닥보다 위에) + 인스턴스별 미세조정
            int sortOrder = IsometricGrid.GetSortingOrder(prop.gridPosition, IsometricGrid.OBJECT_SORT_BASE)
                            + prop.propDefinition.sortingOffset
                            + prop.sortingOffsetOverride;
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
                sr.sortingOrder = sortOrder;
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                mr.sortingOrder = sortOrder;

            // 스프라이트 프롭은 sr.sortingOrder로 정렬되므로 시선축 위치 오프셋을 쓰지 않는다.
            // (오프셋은 셀에 비례한 +Y 성분이 있어 root를 띄운다 → 원근 씬뷰/접지 마커가 떠 보임.
            //  런타임 PropManager도 오프셋 없이 바닥에 정확히 붙음. 불투명 메시(건물)만 오프셋 사용.)

            // 접지 위치 마커
            AttachPlacedGroundMarker(go);

            // 쿼드(스프라이트) 프롭은 빛 차폐를 쓰지 않는다 (ShadowProxy 없음).

            // 건물 소속 표시 (에디터 라벨)
            if (!string.IsNullOrEmpty(prop.parentBuildingId))
            {
                var labelGo = new GameObject("BuildingLabel");
                labelGo.transform.SetParent(go.transform);
                labelGo.transform.localPosition = Vector3.up * 0.8f;
                var tm = labelGo.AddComponent<TextMesh>();
                tm.text = $"[건물:{prop.parentBuildingId}]";
                tm.fontSize = 20;
                tm.characterSize = 0.06f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = new Color(0.3f, 0.8f, 1f, 0.7f);
            }

            _propObjects[prop.instanceId] = go;
        }

        void SpawnBuildingVisual(PlacedBuilding building)
        {
            var def = building.buildingDefinition;
            if (def == null) return;

            Vector3 worldPos = building.GetWorldPosition(EditingMap.gridSettings);
            worldPos.y += building.ElevationY(EditingMap.gridSettings);
            GameObject go;

            if (def.prefab != null)
            {
                go = Instantiate(def.prefab, _buildingRoot);
                go.transform.position = worldPos;
                // Y회전을 프리팹 Root 회전에 곱함 (Root의 카메라 맞춤 회전 유지)
                if (Mathf.Abs(building.yRotation) > 0.01f)
                    go.transform.rotation = Quaternion.Euler(0, building.yRotation, 0) * go.transform.rotation;
            }
            else
            {
                // 프리팹 없으면 풋프린트 크기의 큐브 박스로 표시
                go = new GameObject($"Building_{def.buildingId}_{building.instanceId}");
                go.transform.SetParent(_buildingRoot);
                go.transform.position = worldPos;
                if (Mathf.Abs(building.yRotation) > 0.01f)
                    go.transform.rotation = Quaternion.Euler(0, building.yRotation, 0);

                float ts = EditingMap.gridSettings.tileSize;
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(go.transform, false);
                cube.transform.localPosition = new Vector3(
                    (def.footprint.x - 1) * ts * 0.5f,
                    1.2f,
                    (def.footprint.y - 1) * ts * 0.5f);
                cube.transform.localScale = new Vector3(
                    def.footprint.x * ts * 0.95f,
                    2.4f,
                    def.footprint.y * ts * 0.95f);

                var collider = cube.GetComponent<Collider>();
                if (collider != null) Destroy(collider);

                var renderer = cube.GetComponent<Renderer>();
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader.name == "Hidden/InternalErrorShader")
                    mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.4f, 0.5f, 0.7f, 0.8f);
                renderer.sharedMaterial = mat;

                // 라벨
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(go.transform);
                labelGo.transform.localPosition = new Vector3(
                    (def.footprint.x - 1) * ts * 0.5f,
                    2.8f,
                    (def.footprint.y - 1) * ts * 0.5f);

                var tm = labelGo.AddComponent<TextMesh>();
                tm.text = $"{def.displayName}\n{def.footprint.x}x{def.footprint.y}";
                tm.fontSize = 24;
                tm.characterSize = 0.06f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = Color.white;
            }

            go.name = $"Building_{def.buildingId}_{building.instanceId}";

            // 정렬 순서 (SpriteRenderer + MeshRenderer 모두)
            // = 그리드 기본 + 정의별 기본 오프셋(건물끼리 order) + 인스턴스 미세조정
            int sortOrder = IsometricGrid.GetSortingOrder(building.gridPosition, IsometricGrid.OBJECT_SORT_BASE)
                            + def.sortingOffset
                            + building.sortingOffsetOverride;
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
                sr.sortingOrder = sortOrder;
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                mr.sortingOrder = sortOrder;

            // 불투명 메시(CityBuilding 등)는 sortingOrder를 무시하고 깊이로 정렬되므로,
            // sortingOrder를 시선축 깊이 오프셋으로 변환해 같은 셀에 겹친 바닥/벽/천장의 앞뒤를 확정한다.
            go.transform.position = worldPos + IsometricGrid.SortDepthOffset(sortOrder);

            // 접지 위치 마커
            float markerScale = Mathf.Max(def.footprint.x, def.footprint.y);
            AttachPlacedGroundMarker(go, markerScale);

            _buildingObjects[building.instanceId] = go;
        }

        void SpawnObjectMarker(PlacedMapObject obj)
        {
            GameObject go;
            Vector3 pos = obj.GetWorldPosition(EditingMap.gridSettings);
            pos.y += obj.ElevationY(EditingMap.gridSettings);
            float scale = obj.visualScale > 0.01f ? obj.visualScale : 1f;

            switch (obj.visualMode)
            {
                case 1: // TextureQuad
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    // 바닥에 깔리는 텍스처 쿼드 — 타일과 동일하게 XZ 평면에 눕힘
                    go.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                    go.transform.localScale = Vector3.one * scale;
                    var meshCol = go.GetComponent<MeshCollider>();
                    if (meshCol) Destroy(meshCol);
                    var quadRenderer = go.GetComponent<Renderer>();
                    var quadMat = new Material(Shader.Find("InkCity/CityBuilding") ?? Shader.Find("Universal Render Pipeline/Unlit"));
                    Texture2D quadTex = null;
                    if (catalog != null)
                    {
                        var def = catalog.GetMapObjectDefByType(obj.objectType);
                        if (def != null && def.visualTexture != null) quadTex = def.visualTexture;
                    }
                    if (quadTex == null && !string.IsNullOrEmpty(obj.visualTexturePath))
                        quadTex = Resources.Load<Texture2D>(obj.visualTexturePath);
                    if (quadTex != null)
                    {
                        quadMat.mainTexture = quadTex;
                        float aspect = (float)quadTex.width / quadTex.height;
                        go.transform.localScale = new Vector3(scale * (aspect > 1 ? 1 : aspect), scale * (aspect > 1 ? 1f / aspect : 1), 1f);
                    }
                    quadMat.SetFloat("_Cutoff", 0.5f);
                    quadMat.EnableKeyword("_MAGENTA_CLIP");
                    quadMat.SetFloat("_MagentaClip", 1f);
                    quadRenderer.sharedMaterial = quadMat;
                    go.transform.position = pos;
                    break;
                }

                case 2: // Invisible
                {
                    go = new GameObject();
                    go.transform.position = pos + Vector3.up * 0.5f;
                    var boxCol = go.AddComponent<BoxCollider>();
                    boxCol.size = Vector3.one * 0.4f;
                    boxCol.isTrigger = true;
                    break;
                }

                case 3: // EffectPrefab
                {
                    go = new GameObject();
                    go.transform.position = pos;
                    GameObject effectSrc = null;
                    if (catalog != null)
                    {
                        var def = catalog.GetMapObjectDefByType(obj.objectType);
                        if (def != null && def.effectPrefab != null) effectSrc = def.effectPrefab;
                    }
                    if (effectSrc == null && !string.IsNullOrEmpty(obj.effectPrefabPath))
                        effectSrc = Resources.Load<GameObject>(obj.effectPrefabPath);
                    if (effectSrc != null)
                    {
                        var effect = Instantiate(effectSrc, go.transform);
                        effect.transform.localPosition = Vector3.zero;
                        effect.transform.localScale = Vector3.one * scale;
                    }
                    break;
                }

                default: // 0 = Sphere
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.position = pos + Vector3.up * 0.5f;
                    go.transform.localScale = Vector3.one * 0.4f;
                    var col = go.GetComponent<Collider>();
                    if (col != null) Destroy(col);
                    var renderer = go.GetComponent<Renderer>();
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    if (mat.shader.name == "Hidden/InternalErrorShader")
                        mat = new Material(Shader.Find("Standard"));
                    mat.color = GetObjectTypeColor(obj.objectType);
                    renderer.sharedMaterial = mat;
                    break;
                }
            }

            go.name = $"Marker_{obj.objectType}_{obj.instanceId}";
            go.transform.SetParent(_objectRoot);

            // Apply Y rotation
            if (Mathf.Abs(obj.yRotation) > 0.01f)
                go.transform.rotation = Quaternion.Euler(0, obj.yRotation, 0) * go.transform.rotation;

            // Label (always show for all modes)
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = obj.visualMode == 1 ? Vector3.up * (scale * 0.6f) : Vector3.up * 1f;

            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = string.IsNullOrEmpty(obj.label) ? obj.objectType.ToString() : obj.label;
            tm.fontSize = 24;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = GetObjectTypeColor(obj.objectType);

            _objectMarkers[obj.instanceId] = go;
        }

        static float GetDefaultInteractRange(MapObjectType type) => type switch
        {
            MapObjectType.EscapePoint => 3f,
            MapObjectType.Door => 2f,
            _ => 2f
        };

        static string GetDefaultPromptText(MapObjectType type) => type switch
        {
            MapObjectType.EscapePoint => "탈출",
            MapObjectType.LootContainer => "뒤지기",
            MapObjectType.NPC => "대화하기",
            MapObjectType.Note => "읽기",
            MapObjectType.ItemDrop => "줍기",
            MapObjectType.Bed => "쉬기",
            MapObjectType.Workbench => "제작하기",
            MapObjectType.MapBoard => "출전 준비",
            MapObjectType.MedicalBench => "치료품 제작",
            MapObjectType.CookingBench => "요리하기",
            MapObjectType.Door => "문 열기",
            MapObjectType.GenericInteract => "조사하기",
            _ => ""
        };

        public static Color GetObjectTypeColor(MapObjectType type) => type switch
        {
            MapObjectType.SpawnPoint => Color.green,
            MapObjectType.EscapePoint => Color.cyan,
            MapObjectType.LootContainer => Color.yellow,
            MapObjectType.EnemySpawn => Color.red,
            MapObjectType.ItemDrop => new Color(1f, 0.6f, 0f),
            MapObjectType.Trigger => Color.magenta,
            MapObjectType.Custom => Color.gray,
            MapObjectType.NPC => new Color(0.5f, 0.8f, 1f),
            MapObjectType.Note => new Color(1f, 1f, 0.6f),
            MapObjectType.Bed => new Color(0.6f, 0.4f, 0.8f),
            MapObjectType.Workbench => new Color(0.8f, 0.6f, 0.3f),
            MapObjectType.MapBoard => new Color(0.3f, 0.8f, 0.6f),
            MapObjectType.MedicalBench => new Color(1f, 0.4f, 0.4f),
            MapObjectType.CookingBench => new Color(1f, 0.7f, 0.3f),
            MapObjectType.GenericInteract => new Color(0.7f, 0.7f, 0.7f),
            MapObjectType.Door => new Color(0.6f, 0.4f, 0.25f),
            _ => Color.white
        };

        // --- 배치 고스트 프리뷰 ---

        /// <summary>현재 도구+선택에 맞는 고스트를 생성한다. 이미 있으면 제거 후 재생성.</summary>
        void RebuildGhost()
        {
            DestroyGhost();

            switch (CurrentTool)
            {
                case ToolMode.Prop:
                    if (SelectedProp == null || !PropQuadBuilder.UsesQuad(SelectedProp)) return;
                    _placementGhost = PropQuadBuilder.Build(SelectedProp, EditingMap?.gridSettings, addNavCollider: false);
                    PropQuadBuilder.ApplyFlip(_placementGhost, CurrentFlipX);
                    break;
                case ToolMode.Building:
                    if (SelectedBuilding == null) return;
                    if (SelectedBuilding.prefab != null)
                        _placementGhost = Instantiate(SelectedBuilding.prefab);
                    else
                    {
                        _placementGhost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        float ts = EditingMap?.gridSettings.tileSize ?? 1f;
                        _placementGhost.transform.localScale = new Vector3(
                            SelectedBuilding.footprint.x * ts * 0.95f, 2.4f,
                            SelectedBuilding.footprint.y * ts * 0.95f);
                    }
                    break;
                case ToolMode.Wall:
                    if (SelectedWall == null) return;
                    var wallTile = new PlacedTile
                    {
                        gridPosition = Vector2Int.zero,
                        tileDefinition = SelectedWall,
                        rotation = 0,
                    };
                    var gs = EditingMap?.gridSettings ?? new GridSettings { tileSize = 1f };
                    _placementGhost = WallBuilder.CreateWallCube(wallTile, gs, null);
                    break;
                default:
                    return;
            }

            if (_placementGhost == null) return;
            _placementGhost.name = "__PlacementGhost__";

            // 반투명 처리
            ApplyGhostMaterial(_placementGhost);

            // 콜라이더 제거 (레이캐스트 방해 방지)
            foreach (var col in _placementGhost.GetComponentsInChildren<Collider>())
                Destroy(col);

            // 그라운드 마커 추가
            AttachGroundMarker(_placementGhost, CurrentTool == ToolMode.Building
                ? Mathf.Max(SelectedBuilding?.footprint.x ?? 1, SelectedBuilding?.footprint.y ?? 1)
                : 1f);

            _placementGhost.SetActive(false);
        }

        void ApplyGhostMaterial(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r is SpriteRenderer sr)
                {
                    // SpriteRenderer: color.a로 반투명
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.4f);
                }
                else
                {
                    // MeshRenderer: 원본 머티리얼 복제 후 투명 처리 (텍스처 유지)
                    var origMats = r.sharedMaterials;
                    var ghostMats = new Material[origMats.Length];
                    for (int i = 0; i < origMats.Length; i++)
                    {
                        var src = origMats[i];
                        var gm = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));

                        // URP 투명 설정
                        gm.SetFloat("_Surface", 1); // Transparent
                        gm.SetOverrideTag("RenderType", "Transparent");
                        gm.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        gm.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        gm.SetInt("_ZWrite", 0);
                        gm.renderQueue = 3000;
                        gm.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

                        // 알파값 조절 (원본 색상 유지, 반투명)
                        if (gm.HasProperty("_BaseColor"))
                        {
                            var c = gm.GetColor("_BaseColor");
                            gm.SetColor("_BaseColor", new Color(c.r, c.g, c.b, 0.45f));
                        }
                        else if (gm.HasProperty("_Color"))
                        {
                            var c = gm.GetColor("_Color");
                            gm.SetColor("_Color", new Color(c.r, c.g, c.b, 0.45f));
                        }
                        else if (gm.HasProperty("_Tint"))
                        {
                            var c = gm.GetColor("_Tint");
                            gm.SetColor("_Tint", new Color(c.r, c.g, c.b, 0.45f));
                        }

                        // Alpha도 별도 프로퍼티가 있으면 설정
                        if (gm.HasProperty("_Alpha"))
                            gm.SetFloat("_Alpha", 0.45f);

                        ghostMats[i] = gm;
                    }
                    r.sharedMaterials = ghostMats;
                }
            }
        }

        /// <summary>고스트 위치/회전을 마우스 커서에 맞춰 갱신한다.</summary>
        public void UpdateGhostPosition(Vector2Int cell, Vector3 worldPos)
        {
            if (_placementGhost == null) return;
            if (EditingMap == null) return;

            bool inBounds = EditingMap.gridSettings.IsInBounds(cell);
            _placementGhost.SetActive(inBounds);
            if (!inBounds) return;

            Vector3 finalPos = SnapToGrid
                ? IsometricGrid.GridToWorld(cell, EditingMap.gridSettings)
                : worldPos;

            // 현재 층 높이 (Stage 2: 타일/벽뿐 아니라 프롭/건물/오브젝트도 층을 가진다.)
            float levelY = CurrentLevel * EditingMap.gridSettings.levelHeight;

            float yRot = CurrentRotation * 15f;

            switch (CurrentTool)
            {
                case ToolMode.Prop:
                {
                    // 회전: 벽 부착이면 빌보드 끄고 yaw만, 아니면 빌보드×yaw.
                    _placementGhost.transform.rotation = PropQuadBuilder.RootRotation(yRot, CurrentWallMount);
                    Vector3 propPos = finalPos + new Vector3(0, levelY, 0);
                    if (CurrentWallMount) propPos += Vector3.up * CurrentMountHeight;
                    // 스프라이트 프롭은 시선축 오프셋 없이 바닥에 그대로 (놓을 때와 동일, 띄움 방지).
                    _placementGhost.transform.position = propPos;
                    // 접지 보정은 root가 아니라 내부 Content에 적용 — 미리보기도 박을 때와 동일하게.
                    // 정의값(카탈로그 기본) + 배치용 인스턴스 오프셋(J/L·I/K·U/O로 조절).
                    Vector3 ghostOffset = (SelectedProp != null ? SelectedProp.GroundOffsetVec : Vector3.zero) + CurrentGroundOffset;
                    PropQuadBuilder.ApplyGroundOffset(_placementGhost, ghostOffset);
                    break;
                }
                case ToolMode.Building:
                {
                    if (SelectedBuilding?.prefab != null)
                        _placementGhost.transform.rotation = Quaternion.Euler(0, yRot, 0) * SelectedBuilding.prefab.transform.rotation;
                    else
                        _placementGhost.transform.rotation = Quaternion.Euler(0, yRot, 0);
                    int bDef = SelectedBuilding != null ? SelectedBuilding.sortingOffset : 0;
                    int bAuto = SelectedBuilding != null
                        ? ComputeFrontSortOverride(SelectedBuilding.GetOccupiedCells(cell), bDef, cell) : 0;
                    int bSort = IsometricGrid.GetSortingOrder(cell, IsometricGrid.OBJECT_SORT_BASE)
                                + bDef + bAuto + _placementSortOffset;
                    _placementGhost.transform.position = finalPos + new Vector3(0, levelY, 0) + IsometricGrid.SortDepthOffset(bSort);
                    break;
                }
                case ToolMode.Wall:
                {
                    float wh = SelectedWall?.wallHeight ?? 2.4f;
                    float wt = SelectedWall?.wallThickness ?? 0.08f;
                    float ts = EditingMap.gridSettings.tileSize;
                    // 벽 길이: 정의값(>0)이면 그걸, 없으면 타일 한 칸. (모서리 오프셋엔 ts 그대로 사용)
                    float wlen = (SelectedWall != null && SelectedWall.wallLength > 0f)
                        ? SelectedWall.wallLength : ts;
                    var wallChild = _placementGhost.transform.childCount > 0
                        ? _placementGhost.transform.GetChild(0) : _placementGhost.transform;

                    if (!SnapToGrid)
                    {
                        // 자유 배치 미리보기: 커서 월드 위치 + 자유 Y회전, 길이는 로컬 X축.
                        _placementGhost.transform.position = worldPos + new Vector3(0, levelY + wh * 0.5f, 0);
                        _placementGhost.transform.rotation = Quaternion.Euler(0, yRot, 0);
                        wallChild.localScale = new Vector3(wlen, wh, wt);
                    }
                    else
                    {
                        // 스냅 미리보기: 동서남북 모서리. CurrentRotation을 90°로 양자화.
                        int wallRot = Mathf.RoundToInt(CurrentRotation / 6f) % 4;
                        Vector3 cellCenter = IsometricGrid.GridToWorld(cell, EditingMap.gridSettings);
                        float halfTile = ts * 0.5f;
                        Vector3 edgeOff = wallRot switch
                        {
                            0 => new Vector3(0, 0, halfTile),
                            1 => new Vector3(halfTile, 0, 0),
                            2 => new Vector3(0, 0, -halfTile),
                            3 => new Vector3(-halfTile, 0, 0),
                            _ => Vector3.zero
                        };
                        // 긴 벽도 그리드 라인에 끝이 맞도록 길이축 보정 (WallBuilder와 동일 공식)
                        float gAlign = WallBuilder.GridAlignShift(wlen, ts);
                        Vector3 alignOff = (wallRot == 0 || wallRot == 2)
                            ? new Vector3(gAlign, 0, 0)
                            : new Vector3(0, 0, gAlign);
                        _placementGhost.transform.position = cellCenter + edgeOff + alignOff + new Vector3(0, levelY + wh * 0.5f, 0);
                        _placementGhost.transform.rotation = Quaternion.identity;
                        wallChild.localScale = (wallRot == 0 || wallRot == 2)
                            ? new Vector3(wlen, wh, wt)
                            : new Vector3(wt, wh, wlen);
                    }
                    break;
                }
            }

            // 고스트 접지 마커는 부모(아이소 틸트/yaw)를 따라 매 프레임 기울어진다 →
            // 배치된 초록 마커처럼 항상 바닥에 평평하게(월드 기준) 다시 고정한다.
            // (월드 회전·Y를 매 프레임 덮어써서 부모 회전과 무관하게 평면 유지.)
            var ghostMarker = _placementGhost.transform.Find("GroundMarker");
            if (ghostMarker != null)
            {
                var gp = _placementGhost.transform.position;
                ghostMarker.position = new Vector3(gp.x, levelY + 0.01f, gp.z);
                ghostMarker.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        void DestroyGhost()
        {
            if (_placementGhost != null)
            {
                Destroy(_placementGhost);
                _placementGhost = null;
            }
        }

        /// <summary>배치 모드 취소 (ESC) — 현재 선택/고스트를 해제해 더 이상 배치되지 않게 한다.</summary>
        public void CancelPlacement()
        {
            DestroyGhost();
            SelectedTile = null;
            SelectedWall = null;
            SelectedProp = null;
            SelectedBuilding = null;
            _placementSortOffset = 0;
            CurrentRotation = 0;
            CurrentFlipX = false;
            CurrentWallMount = false;
            UI.RefreshToolbar();
        }

        /// <summary>현재 배치 가능한 대상이 선택돼 있는지 (ESC 취소 대상 판단용)</summary>
        public bool HasActivePlacement =>
            (CurrentTool == ToolMode.Tile && SelectedTile != null)
            || (CurrentTool == ToolMode.Wall && SelectedWall != null)
            || (CurrentTool == ToolMode.Prop && SelectedProp != null)
            || (CurrentTool == ToolMode.Building && SelectedBuilding != null)
            || CurrentTool == ToolMode.MapObject;

        /// <summary>접지 위치 표시용 링(circle) 마커를 GO 아래에 추가</summary>
        static void AttachGroundMarker(GameObject parent, float radiusScale = 1f)
        {
            var markerGo = new GameObject("GroundMarker");
            markerGo.transform.SetParent(parent.transform, false);
            // 월드 기준 바닥에 위치 (parent의 로컬 기준)
            markerGo.transform.localPosition = Vector3.zero;
            // XZ 평면에 눕힘
            markerGo.transform.localRotation = Quaternion.Euler(90, 0, 0);

            var lr = markerGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.widthMultiplier = 0.03f;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(1f, 1f, 0f, 0.8f);
            lr.sharedMaterial = mat;

            // 원형 포인트 생성
            int segments = 24;
            float radius = 0.35f * radiusScale;
            lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0));
            }

            lr.sortingOrder = 9999; // 항상 맨 위에 표시
        }

        /// <summary>배치된 프롭/건물 비주얼 아래에도 접지 마커를 붙인다.</summary>
        static void AttachPlacedGroundMarker(GameObject go, float radiusScale = 1f)
        {
            var markerGo = new GameObject("GroundMarker");
            markerGo.transform.SetParent(go.transform, false);
            // 부모가 회전되어 있어도 월드 기준으로 바닥에 깔기
            markerGo.transform.position = new Vector3(go.transform.position.x, 0.01f, go.transform.position.z);
            markerGo.transform.rotation = Quaternion.Euler(90, 0, 0);

            var lr = markerGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.widthMultiplier = 0.025f;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(0f, 1f, 0.5f, 0.6f);
            lr.sharedMaterial = mat;

            int segments = 24;
            float radius = 0.3f * radiusScale;
            lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0));
            }

            lr.sortingOrder = 9999;
        }

        void ClearAllVisuals()
        {
            DestroyGhost();
            _tileRenderer.ClearAll();

            foreach (var kvp in _wallObjects)
                if (kvp.Value != null) Destroy(kvp.Value);
            _wallObjects.Clear();

            foreach (var kvp in _propObjects)
                if (kvp.Value != null) Destroy(kvp.Value);
            _propObjects.Clear();

            foreach (var kvp in _buildingObjects)
                if (kvp.Value != null) Destroy(kvp.Value);
            _buildingObjects.Clear();

            foreach (var kvp in _objectMarkers)
                if (kvp.Value != null) Destroy(kvp.Value);
            _objectMarkers.Clear();
        }

        void RebuildAllVisuals()
        {
            ClearAllVisuals();
            if (EditingMap == null) return;

            _tileRenderer.RenderMap(EditingMap);

            foreach (var layer in EditingMap.layers)
            {
                foreach (var tile in layer.tiles)
                {
                    if (tile.tileDefinition != null && tile.tileDefinition.IsWall)
                    {
                        string key = WallKeyFor(tile);
                        var wallGo = WallBuilder.CreateWallCube(tile, EditingMap.gridSettings, _wallRoot);
                        if (wallGo != null)
                            _wallObjects[key] = wallGo;
                    }
                }
            }

            foreach (var prop in EditingMap.props)
                SpawnPropVisual(prop);

            foreach (var building in EditingMap.buildings)
                SpawnBuildingVisual(building);

            foreach (var obj in EditingMap.mapObjects)
                SpawnObjectMarker(obj);

            // 숨김 처리된 건물 상태 재적용 (편집 중 내부 보기 유지)
            ApplyBuildingHiddenState();

            // 층 컷어웨이 재적용 (위층 숨김)
            ApplyLevelCutaway();
        }

        // --- Save / Load ---

        const string MAP_SAVE_FOLDER = "Assets/Maps";

        public string GetSavePath()
        {
#if UNITY_EDITOR
            return Path.Combine(Application.dataPath, "../", MAP_SAVE_FOLDER).Replace("\\", "/");
#else
            return Path.Combine(Application.persistentDataPath, "Maps");
#endif
        }

        void EnsureSaveFolder()
        {
            string dir = GetSavePath();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public void SaveMap(string filename)
        {
            EnsureSaveFolder();

            string dir = GetSavePath();
            string path = Path.Combine(dir, filename + ".json");
            string json = MapSerializer.Serialize(EditingMap);
            File.WriteAllText(path, json);
            Debug.Log($"[MapBuilder] JSON Saved: {path}");

#if UNITY_EDITOR
            // JSON을 AssetDatabase에 반영
            AssetDatabase.Refresh();

            // 프리팹 생성
            SaveMapPrefab(filename);
#endif
        }

#if UNITY_EDITOR
        void SaveMapPrefab(string filename)
        {
            string prefabDir = $"{MAP_SAVE_FOLDER}/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabDir))
            {
                if (!AssetDatabase.IsValidFolder(MAP_SAVE_FOLDER))
                    AssetDatabase.CreateFolder("Assets", "Maps");
                AssetDatabase.CreateFolder(MAP_SAVE_FOLDER, "Prefabs");
            }

            // 루트 오브젝트 생성
            var root = new GameObject($"Map_{filename}");

            try
            {
                // ── Tiles ──
                var tileRoot = new GameObject("Tiles");
                tileRoot.transform.SetParent(root.transform);

                foreach (var layer in EditingMap.layers)
                {
                    var layerGo = new GameObject(layer.layerName);
                    layerGo.transform.SetParent(tileRoot.transform);

                    foreach (var tile in layer.tiles)
                    {
                        if (tile.tileDefinition == null) continue;

                        if (tile.tileDefinition.IsWall)
                        {
                            var wallGo = WallBuilder.CreateWallCube(tile, EditingMap.gridSettings, layerGo.transform);
                            if (wallGo != null)
                                wallGo.name = $"Wall_{tile.tileDefinitionId}_{tile.gridPosition.x}_{tile.gridPosition.y}";
                        }
                        else if (tile.tileDefinition.sprite != null)
                        {
                            var tileGo = new GameObject($"Tile_{tile.tileDefinitionId}_{tile.gridPosition.x}_{tile.gridPosition.y}");
                            tileGo.transform.SetParent(layerGo.transform);

                            Vector3 pos = IsometricGrid.GridToWorld(tile.gridPosition, EditingMap.gridSettings);
                            // 바닥 타일 Y = 해당 층 바닥 높이 (0층=0)
                            pos.y = tile.level * EditingMap.gridSettings.levelHeight;
                            tileGo.transform.position = pos;
                            tileGo.transform.rotation = Quaternion.Euler(90, 0, 0); // XZ 평면에 눕힘
                            tileGo.transform.localScale = IsometricGrid.GetTileScale(tile.tileDefinition.sprite, EditingMap.gridSettings);

                            var sr = tileGo.AddComponent<SpriteRenderer>();
                            sr.sprite = tile.tileDefinition.sprite;
                            sr.flipX = tile.flipX;
                            sr.sortingOrder = IsometricGrid.GetSortingOrder(tile.gridPosition, layer.sortingLayerOffset)
                                              + tile.tileDefinition.sortingOffset;

                            var mat = tile.EffectiveMaterial;
                            if (mat != null)
                                sr.sharedMaterial = mat;
                        }
                    }
                }

                // ── Props ──
                var propRoot = new GameObject("Props");
                propRoot.transform.SetParent(root.transform);

                foreach (var prop in EditingMap.props)
                {
                    var pdef = prop.propDefinition;
                    if (pdef == null) continue;
                    // 프롭은 빌보드 쿼드 전용. 스프라이트 없으면 스킵.
                    if (!PropQuadBuilder.UsesQuad(pdef)) continue;

                    // 스프라이트 프롭은 빌보드 쿼드(+nav 박스)로 베이크.
                    GameObject go = PropQuadBuilder.Build(pdef, EditingMap.gridSettings, addNavCollider: true);
                    if (go == null) continue;
                    go.transform.SetParent(propRoot.transform);

                    go.name = $"Prop_{prop.propDefinitionId}_{prop.instanceId}";
                    Vector3 basePos = prop.GetWorldPosition(EditingMap.gridSettings);
                    basePos.y += prop.ElevationY(EditingMap.gridSettings);
                    if (prop.wallMounted) basePos += Vector3.up * prop.mountHeight;
                    go.transform.position = basePos;
                    go.transform.rotation = PropQuadBuilder.RootRotation(prop.yRotation, prop.wallMounted);
                    go.transform.localScale = Vector3.one * prop.scale;
                    PropQuadBuilder.ApplyFlip(go, prop.flipX);
                    PropQuadBuilder.ApplyGroundOffset(go, prop.GroundOffsetVec); // 접지 보정 — 내부 Content(이미지+콜라이더)만

                    int sortOrder = IsometricGrid.GetSortingOrder(prop.gridPosition, IsometricGrid.OBJECT_SORT_BASE)
                                    + pdef.sortingOffset
                                    + prop.sortingOffsetOverride;
                    foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
                        sr.sortingOrder = sortOrder;
                    foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                        mr.sortingOrder = sortOrder;
                    // 스프라이트 프롭은 sortingOrder로 정렬 — 시선축 위치 오프셋 없이 바닥에 그대로(런타임과 동일).

                    // 쿼드(스프라이트) 프롭은 빛 차폐를 쓰지 않는다.
                }

                // ── Buildings ──
                var buildRoot = new GameObject("Buildings");
                buildRoot.transform.SetParent(root.transform);

                // 내부 오브젝트(parentBuildingId) 라우팅용 instanceId→GameObject
                var bakedBuildings = new System.Collections.Generic.Dictionary<string, GameObject>();

                foreach (var building in EditingMap.buildings)
                {
                    var def = building.buildingDefinition;
                    if (def == null) continue;

                    Vector3 worldPos = building.GetWorldPosition(EditingMap.gridSettings);
                    worldPos.y += building.ElevationY(EditingMap.gridSettings);
                    GameObject go;

                    if (def.prefab != null)
                    {
                        go = (GameObject)PrefabUtility.InstantiatePrefab(def.prefab, buildRoot.transform);
                        if (go == null) continue;
                        go.transform.position = worldPos;
                        if (Mathf.Abs(building.yRotation) > 0.01f)
                            go.transform.rotation = Quaternion.Euler(0, building.yRotation, 0) * go.transform.rotation;
                    }
                    else
                    {
                        go = new GameObject($"Building_{def.buildingId}_{building.instanceId}");
                        go.transform.SetParent(buildRoot.transform);
                        go.transform.position = worldPos;
                        if (Mathf.Abs(building.yRotation) > 0.01f)
                            go.transform.rotation = Quaternion.Euler(0, building.yRotation, 0);
                    }

                    go.name = $"Building_{building.buildingDefinitionId}_{building.instanceId}";
                    go.transform.localScale = Vector3.one * building.scale;
                    if (!string.IsNullOrEmpty(building.instanceId))
                        bakedBuildings[building.instanceId] = go;

                    int sortOrder = IsometricGrid.GetSortingOrder(building.gridPosition, IsometricGrid.OBJECT_SORT_BASE)
                                    + def.sortingOffset
                                    + building.sortingOffsetOverride;
                    foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
                        sr.sortingOrder = sortOrder;
                    foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                        mr.sortingOrder = sortOrder;
                    go.transform.position = worldPos + IsometricGrid.SortDepthOffset(sortOrder);
                }

                // ── MapObjects (SpawnPoint, MapBoard 등 — 기능 컴포넌트 포함) ──
                var objRoot = new GameObject("MapObjects");
                objRoot.transform.SetParent(root.transform);

                // 런타임 스폰과 동일한 MapObjectSpawner로 InteractableObject/SpawnPoint/LootContainer 등
                // 기능 컴포넌트를 그대로 부착해 프리팹에 직렬화한다.
                // (이전엔 빈 GameObject만 만들어 스폰포인트/지도판 등의 기능이 저장 안 됐음.)
                var bakeSpawnerGo = new GameObject("__BakeMapObjectSpawner");
                try
                {
                    var bakeSpawner = bakeSpawnerGo.AddComponent<MapObjectSpawner>();
                    bakeSpawner.Initialize(objRoot.transform);
                    bakeSpawner.SetBuildingObjects(bakedBuildings);

                    foreach (var obj in EditingMap.mapObjects)
                    {
                        var go = bakeSpawner.SpawnSingle(obj, EditingMap.gridSettings);
                        if (go == null) continue;

                        // 이펙트 프리팹 비주얼(visualMode==3)은 스포너가 다루지 않으므로 여기서 부착.
                        if (obj.visualMode == 3)
                        {
                            GameObject effectSrc = null;
                            var moDef = catalog?.GetMapObjectDefByType(obj.objectType);
                            if (moDef != null && moDef.effectPrefab != null)
                                effectSrc = moDef.effectPrefab;
                            if (effectSrc == null && !string.IsNullOrEmpty(obj.effectPrefabPath))
                                effectSrc = Resources.Load<GameObject>(obj.effectPrefabPath);
                            if (effectSrc != null)
                            {
                                var effect = (GameObject)PrefabUtility.InstantiatePrefab(effectSrc, go.transform);
                                if (effect != null)
                                {
                                    effect.transform.localPosition = Vector3.zero;
                                    float s = obj.visualScale > 0.01f ? obj.visualScale : 1f;
                                    effect.transform.localScale = Vector3.one * s;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    DestroyImmediate(bakeSpawnerGo);
                }

                // ── 맵 데이터 컴포넌트 부착 ──
                var mapRef = root.AddComponent<MapDataReference>();
                mapRef.mapName = EditingMap.mapName;
                mapRef.mapId = EditingMap.mapId;
                mapRef.jsonFileName = filename;
                mapRef.gridSettings = EditingMap.gridSettings;

                // 생성된 비-에셋 메시/머티리얼(벽 큐브, 건물 폴백 등)을 디스크 에셋으로 영속화.
                // 안 하면 SaveAsPrefabAsset이 메모리상 메시/머티리얼을 직렬화하지 못해
                // 프리팹을 다시 열 때 참조가 null이 되어 벽이 사라진다.
                PersistGeneratedAssets(root, prefabDir, filename);

                // 프리팹 저장
                string prefabPath = $"{prefabDir}/Map_{filename}.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[MapBuilder] Prefab Saved: {prefabPath}");
            }
            finally
            {
                DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 프리팹 베이크 시 코드로 생성된(=에셋이 아닌) 메시/머티리얼을 디스크 에셋으로 저장하고
        /// 계층 내 참조를 그 에셋으로 바꾼다. 이렇게 해야 SaveAsPrefabAsset이 정상 직렬화한다.
        ///   - 벽 큐브: WallBuilder의 공유 절차적 메시(_uprightBox) + 런타임 머티리얼.
        ///   - 건물 폴백 큐브 등: new Material(...)로 만든 런타임 머티리얼.
        /// 동일 인스턴스는 한 번만 저장하고 재사용(레퍼런스로 디듀프).
        /// 에셋은 {prefabDir}/Map_{filename}_Assets/ 폴더에 모은다(프리팹 삭제 시 함께 정리하기 쉬움).
        /// </summary>
        void PersistGeneratedAssets(GameObject root, string prefabDir, string filename)
        {
            string assetDir = $"{prefabDir}/Map_{filename}_Assets";
            // 이전 베이크 잔여물 제거(참조 깨짐 방지 + 깔끔한 재생성)
            if (AssetDatabase.IsValidFolder(assetDir))
                AssetDatabase.DeleteAsset(assetDir);
            AssetDatabase.CreateFolder(prefabDir, $"Map_{filename}_Assets");

            var meshMap = new System.Collections.Generic.Dictionary<Mesh, Mesh>();
            var matMap = new System.Collections.Generic.Dictionary<Material, Material>();
            int meshIdx = 0, matIdx = 0;

            // ── 메시 ──
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh;
                if (mesh == null || AssetDatabase.Contains(mesh)) continue;
                if (!meshMap.TryGetValue(mesh, out var saved))
                {
                    // 원본(런타임 공유 싱글톤)을 소비하지 않도록 복사본을 저장한다.
                    saved = Object.Instantiate(mesh);
                    saved.name = $"{mesh.name}_{meshIdx++}";
                    AssetDatabase.CreateAsset(saved, $"{assetDir}/{saved.name}.asset");
                    meshMap[mesh] = saved;
                }
                mf.sharedMesh = saved;
            }

            // ── 머티리얼 ──
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = rend.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null || AssetDatabase.Contains(mat)) continue;
                    if (!matMap.TryGetValue(mat, out var saved))
                    {
                        saved = new Material(mat) { name = $"{mat.name}_{matIdx++}" };
                        AssetDatabase.CreateAsset(saved, $"{assetDir}/{saved.name}.mat");
                        matMap[mat] = saved;
                    }
                    mats[i] = saved;
                    changed = true;
                }
                if (changed) rend.sharedMaterials = mats;
            }

            AssetDatabase.SaveAssets();
        }
#endif

        public void LoadMap(string filename)
        {
            string path = Path.Combine(GetSavePath(), filename + ".json");
            if (!File.Exists(path))
            {
                Debug.LogError($"[MapBuilder] File not found: {path}");
                return;
            }

            string json = File.ReadAllText(path);
            EditingMap = ScriptableObject.CreateInstance<MapData>();
            MapSerializer.Deserialize(json, EditingMap);

            ResolveReferences();

            GridOverlay.gridSettings = EditingMap.gridSettings;
            RebuildAllVisuals();

            Vector3 center = IsometricGrid.GridToWorld(
                new Vector2Int(EditingMap.gridSettings.mapWidth / 2,
                               EditingMap.gridSettings.mapHeight / 2),
                EditingMap.gridSettings);
            BuilderCamera.SetFocusPoint(center);

            _undoSnapshots.Clear();
            UI.RefreshAll();
            Debug.Log($"[MapBuilder] Loaded: {path}");
        }

        void ResolveReferences()
        {
            if (catalog == null) return;

            foreach (var layer in EditingMap.layers)
            {
                foreach (var tile in layer.tiles)
                {
                    if (tile.tileDefinition == null && !string.IsNullOrEmpty(tile.tileDefinitionId))
                        tile.tileDefinition = catalog.GetTile(tile.tileDefinitionId);
                }
            }

            foreach (var prop in EditingMap.props)
            {
                if (prop.propDefinition == null && !string.IsNullOrEmpty(prop.propDefinitionId))
                    prop.propDefinition = catalog.GetProp(prop.propDefinitionId);
            }

            foreach (var building in EditingMap.buildings)
            {
                if (building.buildingDefinition == null && !string.IsNullOrEmpty(building.buildingDefinitionId))
                    building.buildingDefinition = catalog.GetBuilding(building.buildingDefinitionId);
            }
        }

        public string[] GetSavedMapFiles()
        {
            string dir = GetSavePath();
            if (!Directory.Exists(dir)) return new string[0];

            var files = Directory.GetFiles(dir, "*.json");
            var names = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                names[i] = Path.GetFileNameWithoutExtension(files[i]);
            return names;
        }

        // --- Undo ---

        void SaveUndoSnapshot()
        {
            string snapshot = MapSerializer.Serialize(EditingMap);
            _undoSnapshots.Add(snapshot);
            if (_undoSnapshots.Count > MAX_UNDO)
                _undoSnapshots.RemoveAt(0);
        }

        public void Undo()
        {
            if (_undoSnapshots.Count == 0) return;

            string snapshot = _undoSnapshots[^1];
            _undoSnapshots.RemoveAt(_undoSnapshots.Count - 1);

            MapSerializer.Deserialize(snapshot, EditingMap);
            ResolveReferences();
            RebuildAllVisuals();
        }
    }
}
