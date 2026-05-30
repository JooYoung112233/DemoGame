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

        public MapData EditingMap { get; private set; }
        public ToolMode CurrentTool { get; private set; } = ToolMode.Tile;
        public TileDefinition SelectedTile { get; private set; }
        public TileDefinition SelectedWall { get; private set; }
        public PropDefinition SelectedProp { get; private set; }
        public BuildingDefinition SelectedBuilding { get; private set; }
        public MapObjectType SelectedObjectType { get; private set; } = MapObjectType.SpawnPoint;
        // Eraser hover
        readonly List<(Renderer rend, Color origColor)> _eraseHoverRenderers = new();
        string _eraseHoverInstanceId;
        public string EraseHoverInfo { get; private set; }

        // 정렬 조절 대상 (마지막으로 가리킨 프랍/건물). UI 위로 마우스가 가도 유지됨.
        public string SortTargetId { get; private set; }
        public string SortTargetLabel { get; private set; }
        public bool HasSortTarget => !string.IsNullOrEmpty(SortTargetId);
        public int SortTargetOffset
        {
            get
            {
                if (EditingMap == null || string.IsNullOrEmpty(SortTargetId)) return 0;
                foreach (var p in EditingMap.props) if (p.instanceId == SortTargetId) return p.sortingOffsetOverride;
                foreach (var b in EditingMap.buildings) if (b.instanceId == SortTargetId) return b.sortingOffsetOverride;
                return 0;
            }
        }

        // Resize mode
        public bool ResizeMode { get; set; }
        readonly List<(Renderer rend, Color origColor)> _resizeHoverRenderers = new();
        string _resizeHoverInstanceId;
        public string ResizeHoverInfo { get; private set; }

        // Move mode
        public bool MoveMode { get; private set; }
        bool _moveGrabbed; // 오브젝트를 집었는가
        string _moveInstanceId;
        int _moveType = -1; // 0=tile, 1=wall, 2=prop, 3=building, 4=mapObject
        int _moveWallRotation;
        readonly List<(Renderer rend, Color origColor)> _moveHoverRenderers = new();
        string _moveHoverInstanceId;
        public string MoveHoverInfo { get; private set; }

        // Visibility toggles
        public bool ShowTiles { get; set; } = true;
        public bool ShowWalls { get; set; } = true;
        public bool ShowProps { get; set; } = true;
        public bool ShowBuildings { get; set; } = true;
        public bool ShowMapObjects { get; set; } = true;

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
        public bool SnapToGrid { get; set; }

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
            NewMap(defaultMapWidth, defaultMapHeight);
        }

        void SetupScene()
        {
            // Camera
            var camGo = new GameObject("MapBuilderCamera");
            BuilderCamera = camGo.AddComponent<MapBuilderCamera>();
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

        public void NewMap(int width, int height)
        {
            EditingMap = ScriptableObject.CreateInstance<MapData>();
            EditingMap.mapName = "NewMap";
            EditingMap.mapId = System.Guid.NewGuid().ToString("N")[..8];
            EditingMap.gridSettings = new GridSettings
            {
                tileSize = 1f,
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

        public void RotateSelection(float delta)
        {
            CurrentRotation = ((CurrentRotation + delta) % 24f + 24f) % 24f;
            UI.RefreshStatus();

            // 이동 모드에서 집은 오브젝트의 각도도 실시간 갱신
            if (_moveGrabbed) ApplyMoveRotation();
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
                    PlaceWall(cell);
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
                    flipX = false
                };

                EditingMap.PlaceTile(tile, "Ground");
                _tileRenderer.RenderSingleTile(tile, layer, EditingMap.gridSettings);
            }
        }

        void PlaceWall(Vector2Int cell)
        {
            if (SelectedWall == null) return;
            SaveUndoSnapshot();

            int wallRot = Mathf.RoundToInt(CurrentRotation);
            var tile = new PlacedTile
            {
                gridPosition = cell,
                tileDefinitionId = SelectedWall.tileId,
                tileDefinition = SelectedWall,
                rotation = wallRot,
                flipX = false
            };

            EditingMap.PlaceTile(tile, "Walls");

            string key = $"{cell.x}_{cell.y}_E{wallRot}";
            if (_wallObjects.TryGetValue(key, out var old))
            {
                Destroy(old);
                _wallObjects.Remove(key);
            }

            var wallGo = WallBuilder.CreateWallCube(tile, EditingMap.gridSettings, _wallRoot);
            if (wallGo != null)
                _wallObjects[key] = wallGo;
        }

        void PlaceProp(Vector3 worldPos)
        {
            if (SelectedProp == null || SelectedProp.prefab == null) return;
            SaveUndoSnapshot();

            var cell = IsometricGrid.WorldToGrid(worldPos, EditingMap.gridSettings);
            bool free = !SnapToGrid;
            Vector3 finalPos = free ? worldPos : IsometricGrid.GridToWorld(cell, EditingMap.gridSettings);

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
                parentBuildingId = SelectedParentBuildingId
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
                scale = 1f
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
                    // Only erase tiles (not walls, not objects)
                    EditingMap.RemoveNonWallTilesAt(cell);
                    _tileRenderer.RemoveTileObject(cell);
                    break;
                case ToolMode.Eraser:
                    // Erase the nearest object of ANY type at that cell
                    EraseNearestAtCell(cell);
                    break;
                case ToolMode.Wall:
                    int eraseWallRot = Mathf.RoundToInt(CurrentRotation);
                    string key = $"{cell.x}_{cell.y}_E{eraseWallRot}";
                    EditingMap.RemoveWallEdge(cell, eraseWallRot);
                    if (_wallObjects.TryGetValue(key, out var wallGo))
                    {
                        Destroy(wallGo);
                        _wallObjects.Remove(key);
                    }
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
            for (int r = 0; r < 4; r++)
            {
                string key = $"{cell.x}_{cell.y}_E{r}";
                if (_wallObjects.TryGetValue(key, out var go))
                {
                    Destroy(go);
                    _wallObjects.Remove(key);
                }
            }
        }

        void RemoveNearestProp(Vector3 worldPos)
        {
            float bestDist = 1.5f;
            PlacedProp bestProp = null;
            foreach (var p in EditingMap.props)
            {
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

            // Check tiles (non-wall) at cell
            foreach (var layer in EditingMap.layers)
            {
                for (int i = 0; i < layer.tiles.Count; i++)
                {
                    var t = layer.tiles[i];
                    if (t.gridPosition != cell) continue;
                    if (t.tileDefinition != null && t.tileDefinition.IsWall) continue;
                    // Tiles are exactly at cell center, distance = 0
                    if (0f < bestDist) { bestDist = 0f; bestType = 0; }
                }
            }

            // Check walls at cell
            for (int r = 0; r < 4; r++)
            {
                string key = $"{cell.x}_{cell.y}_E{r}";
                if (_wallObjects.ContainsKey(key))
                {
                    if (0f < bestDist) { bestDist = 0f; bestType = 1; bestRotation = r; }
                }
            }

            // Check props
            for (int i = 0; i < EditingMap.props.Count; i++)
            {
                var p = EditingMap.props[i];
                float dist = Vector3.Distance(p.GetWorldPosition(EditingMap.gridSettings), cellWorld);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestType = 2;
                    bestIndex = i;
                }
            }

            // Check buildings
            for (int i = 0; i < EditingMap.buildings.Count; i++)
            {
                var b = EditingMap.buildings[i];
                if (b.buildingDefinition == null) continue;
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

            // Check map objects
            for (int i = 0; i < EditingMap.mapObjects.Count; i++)
            {
                var obj = EditingMap.mapObjects[i];
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

            // Execute erase for nearest
            switch (bestType)
            {
                case 0:
                    EditingMap.RemoveNonWallTilesAt(cell);
                    _tileRenderer.RemoveTileObject(cell);
                    break;
                case 1:
                    string wKey = $"{cell.x}_{cell.y}_E{bestRotation}";
                    EditingMap.RemoveWallEdge(cell, bestRotation);
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

            // Props
            for (int i = 0; i < EditingMap.props.Count; i++)
            {
                var p = EditingMap.props[i];
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
            // Buildings
            for (int i = 0; i < EditingMap.buildings.Count; i++)
            {
                var b = EditingMap.buildings[i];
                if (b.buildingDefinition == null) continue;
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
            // Map Objects
            for (int i = 0; i < EditingMap.mapObjects.Count; i++)
            {
                var obj = EditingMap.mapObjects[i];
                float dist = Vector3.Distance(obj.GetWorldPosition(EditingMap.gridSettings), cellWorld);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestType = 4; bestIndex = i;
                    targetId = obj.instanceId;
                    info = $"{obj.objectType}: {obj.label}";
                }
            }
            // Walls
            for (int r = 0; r < 4; r++)
            {
                string wKey = $"{cell.x}_{cell.y}_E{r}";
                if (_wallObjects.ContainsKey(wKey) && 0f < bestDist)
                {
                    bestDist = 0f; bestType = 1; bestRotation = r;
                    targetId = wKey;
                    info = "벽";
                }
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
                    _moveHoverRenderers.Add((r, r.material.color));
                    r.material.color = new Color(0.2f, 1f, 0.3f, 0.9f);
                }
            }
        }

        public void ClearMoveHover()
        {
            foreach (var (rend, orig) in _moveHoverRenderers)
            {
                if (rend != null && rend.material != null)
                    rend.material.color = orig;
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
                    if (p != null) CurrentRotation = p.yRotation / 15f;
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
                    _moveWallRotation = 0;
                    foreach (var layer in EditingMap.layers)
                        foreach (var t in layer.tiles)
                            if (t.gridPosition == cell && t.tileDefinition != null && t.tileDefinition.IsWall)
                            { _moveWallRotation = t.rotation; break; }
                    CurrentRotation = _moveWallRotation;
                    break;
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
                float yOffset = _moveType == 1
                    ? go.transform.position.y  // 벽은 Y축 유지
                    : (_moveType == 4 && go.transform.childCount > 0 ? go.transform.position.y : 0f);
                go.transform.position = new Vector3(finalPos.x, _moveType == 1 ? yOffset : 0f, finalPos.z);

                // 벽은 높이 보정
                if (_moveType == 1)
                {
                    var wallChild = go.transform.Find("WallVisual") ?? (go.transform.childCount > 0 ? go.transform.GetChild(0) : null);
                    if (wallChild != null)
                        go.transform.position = new Vector3(finalPos.x, wallChild.localScale.y * 0.5f, finalPos.z);
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
                case 2: _propObjects.TryGetValue(_moveInstanceId, out go); break;
                case 3: _buildingObjects.TryGetValue(_moveInstanceId, out go); break;
                case 4: _objectMarkers.TryGetValue(_moveInstanceId, out go); break;
            }
            if (go == null) return;

            // 프리팹 원본 회전에 yRotation을 곱함
            switch (_moveType)
            {
                case 2:
                    var p = EditingMap.props.Find(x => x.instanceId == _moveInstanceId);
                    if (p?.propDefinition?.prefab != null)
                    {
                        var baseRot = p.propDefinition.prefab.transform.rotation;
                        go.transform.rotation = Quaternion.Euler(0, yRot, 0) * baseRot;
                    }
                    else go.transform.rotation = Quaternion.Euler(0, yRot, 0);
                    break;
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
                    // 원래 벽 데이터 찾아서 제거
                    PlacedTile wallTile = null;
                    string layerName = null;
                    foreach (var layer in EditingMap.layers)
                    {
                        for (int i = layer.tiles.Count - 1; i >= 0; i--)
                        {
                            var t = layer.tiles[i];
                            if (t.tileDefinition != null && t.tileDefinition.IsWall)
                            {
                                string wKey = $"{t.gridPosition.x}_{t.gridPosition.y}_E{t.rotation}";
                                if (wKey == _moveInstanceId)
                                {
                                    wallTile = t;
                                    layerName = layer.layerName;
                                    layer.tiles.RemoveAt(i);
                                    break;
                                }
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
                        int newRot = Mathf.RoundToInt(CurrentRotation) % 4;
                        wallTile.gridPosition = cell;
                        wallTile.rotation = newRot;
                        EditingMap.PlaceTile(wallTile, layerName ?? "Walls");
                        var wallGo = WallBuilder.CreateWallCube(wallTile, EditingMap.gridSettings, _wallRoot);
                        if (wallGo != null)
                        {
                            string newKey = $"{cell.x}_{cell.y}_E{newRot}";
                            _wallObjects[newKey] = wallGo;
                        }
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
        /// 현재 마우스가 가리키는(호버) 프랍/건물의 정렬 오프셋을 미세조정한다. ([ / ] 키)
        /// 스프라이트는 sortingOrder 가 즉시 먹고, 불투명 3D 메시는 깊이 정렬이라 효과가 없을 수 있다.
        /// </summary>
        public void NudgeHoverSortOffset(int delta)
        {
            if (EditingMap == null || string.IsNullOrEmpty(SortTargetId)) return;

            // 프랍
            for (int i = 0; i < EditingMap.props.Count; i++)
            {
                var p = EditingMap.props[i];
                if (p.instanceId != SortTargetId) continue;

                p.sortingOffsetOverride += delta;

                if (_propObjects.TryGetValue(p.instanceId, out var old) && old != null)
                    Destroy(old);
                _propObjects.Remove(p.instanceId);
                SpawnPropVisual(p);

                UI?.RefreshStatus();
                return;
            }

            // 건물
            for (int i = 0; i < EditingMap.buildings.Count; i++)
            {
                var b = EditingMap.buildings[i];
                if (b.instanceId != SortTargetId) continue;

                b.sortingOffsetOverride += delta;

                if (_buildingObjects.TryGetValue(b.instanceId, out var old) && old != null)
                    Destroy(old);
                _buildingObjects.Remove(b.instanceId);
                SpawnBuildingVisual(b);

                UI?.RefreshStatus();
                return;
            }
        }

        /// <summary>정렬 조절 대상 해제 (X 버튼)</summary>
        public void ClearSortTarget()
        {
            SortTargetId = null;
            SortTargetLabel = null;
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

            // Check tiles
            foreach (var layer in EditingMap.layers)
            {
                for (int i = 0; i < layer.tiles.Count; i++)
                {
                    var t = layer.tiles[i];
                    if (t.gridPosition != cell) continue;
                    if (t.tileDefinition != null && t.tileDefinition.IsWall) continue;
                    if (0f < bestDist) { bestDist = 0f; bestType = 0; info = $"타일: {t.tileDefinitionId}"; }
                }
            }

            // Check walls
            for (int r = 0; r < 4; r++)
            {
                string key = $"{cell.x}_{cell.y}_E{r}";
                if (_wallObjects.ContainsKey(key))
                {
                    if (0f < bestDist)
                    {
                        bestDist = 0f; bestType = 1; bestRotation = r;
                        // find wall tile definition
                        foreach (var layer in EditingMap.layers)
                            foreach (var t in layer.tiles)
                                if (t.gridPosition == cell && t.rotation == r && t.tileDefinition != null && t.tileDefinition.IsWall)
                                    info = $"벽: {t.tileDefinitionId}";
                        if (info == null) info = "벽";
                    }
                }
            }

            // Check props
            for (int i = 0; i < EditingMap.props.Count; i++)
            {
                var p = EditingMap.props[i];
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

            // Check buildings
            for (int i = 0; i < EditingMap.buildings.Count; i++)
            {
                var b = EditingMap.buildings[i];
                if (b.buildingDefinition == null) continue;
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

            // Check map objects
            for (int i = 0; i < EditingMap.mapObjects.Count; i++)
            {
                var obj = EditingMap.mapObjects[i];
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

            // Determine the target instanceId for highlighting
            string targetId = null;
            switch (bestType)
            {
                case 2: targetId = EditingMap.props[bestIndex].instanceId; break;
                case 3: targetId = EditingMap.buildings[bestIndex].instanceId; break;
                case 4: targetId = EditingMap.mapObjects[bestIndex].instanceId; break;
            }

            // 프랍/건물을 가리켰으면 정렬 조절 대상으로 고정 (UI 클릭하러 가도 유지)
            if (bestType == 2 || bestType == 3)
            {
                if (SortTargetId != targetId)
                {
                    SortTargetId = targetId;
                    SortTargetLabel = info;
                    UI?.RefreshStatus();
                }
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
                string wKey = $"{cell.x}_{cell.y}_E{bestRotation}";
                _wallObjects.TryGetValue(wKey, out targetGo);
            }

            if (targetGo != null)
            {
                var renderers = targetGo.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    if (r.material == null) continue;
                    _eraseHoverRenderers.Add((r, r.material.color));
                    r.material.color = new Color(1f, 0.25f, 0.25f, 0.9f);
                }
            }
        }

        public void ClearEraseHover()
        {
            foreach (var (rend, orig) in _eraseHoverRenderers)
            {
                if (rend != null && rend.material != null)
                    rend.material.color = orig;
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

            // Props
            for (int i = 0; i < EditingMap.props.Count; i++)
            {
                var p = EditingMap.props[i];
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

            // Buildings
            for (int i = 0; i < EditingMap.buildings.Count; i++)
            {
                var b = EditingMap.buildings[i];
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

            // MapObject markers (sphere markers have no meaningful scale, skip for now)

            if (targetId == _resizeHoverInstanceId) { ResizeHoverInfo = info; return; }

            ClearResizeHover();
            _resizeHoverInstanceId = targetId;
            ResizeHoverInfo = info;

            // Highlight with blue tint
            GameObject targetGo = null;
            if (targetType == 0 && targetId != null) _propObjects.TryGetValue(targetId, out targetGo);
            else if (targetType == 1 && targetId != null) _buildingObjects.TryGetValue(targetId, out targetGo);

            if (targetGo != null)
            {
                var renderers = targetGo.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    if (r.material == null) continue;
                    _resizeHoverRenderers.Add((r, r.material.color));
                    r.material.color = new Color(0.3f, 0.6f, 1f, 0.9f);
                }
            }
        }

        public void ClearResizeHover()
        {
            foreach (var (rend, orig) in _resizeHoverRenderers)
            {
                if (rend != null && rend.material != null)
                    rend.material.color = orig;
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
            if (prop.propDefinition?.prefab == null) return;

            var go = Instantiate(prop.propDefinition.prefab, _propRoot);
            go.name = $"Prop_{prop.propDefinitionId}_{prop.instanceId}";
            go.transform.position = prop.GetWorldPosition(EditingMap.gridSettings);
            // Y회전을 프리팹 Root 회전에 곱함 (Root의 카메라 맞춤 회전 유지)
            if (Mathf.Abs(prop.yRotation) > 0.01f)
                go.transform.rotation = Quaternion.Euler(0, prop.yRotation, 0) * go.transform.rotation;
            go.transform.localScale = Vector3.one * prop.scale;

            // 스프라이트 정렬 순서 (바닥보다 위에) + 인스턴스별 미세조정
            int sortOrder = IsometricGrid.GetSortingOrder(prop.gridPosition, IsometricGrid.OBJECT_SORT_BASE)
                            + prop.propDefinition.sortingOffset
                            + prop.sortingOffsetOverride;
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
                sr.sortingOrder = sortOrder;

            // 접지 위치 마커
            AttachPlacedGroundMarker(go);

            // 빛 차폐 그림자 프록시 박스 (벽/컨테이너 등). prop의 회전/스케일을 상속.
            ShadowProxyBuilder.Build(prop.propDefinition, go.transform, editorPreview: !Application.isPlaying);

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

            // 접지 위치 마커
            float markerScale = Mathf.Max(def.footprint.x, def.footprint.y);
            AttachPlacedGroundMarker(go, markerScale);

            _buildingObjects[building.instanceId] = go;
        }

        void SpawnObjectMarker(PlacedMapObject obj)
        {
            GameObject go;
            Vector3 pos = obj.GetWorldPosition(EditingMap.gridSettings);
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
                    if (SelectedProp?.prefab == null) return;
                    _placementGhost = Instantiate(SelectedProp.prefab);
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
                        else
                        {
                            gm.color = new Color(gm.color.r, gm.color.g, gm.color.b, 0.45f);
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

            float yRot = CurrentRotation * 15f;

            switch (CurrentTool)
            {
                case ToolMode.Prop:
                    _placementGhost.transform.position = finalPos;
                    if (Mathf.Abs(yRot) > 0.01f && SelectedProp?.prefab != null)
                        _placementGhost.transform.rotation = Quaternion.Euler(0, yRot, 0) * SelectedProp.prefab.transform.rotation;
                    else if (SelectedProp?.prefab != null)
                        _placementGhost.transform.rotation = SelectedProp.prefab.transform.rotation;
                    break;
                case ToolMode.Building:
                    _placementGhost.transform.position = finalPos;
                    if (SelectedBuilding?.prefab != null)
                        _placementGhost.transform.rotation = Quaternion.Euler(0, yRot, 0) * SelectedBuilding.prefab.transform.rotation;
                    else
                        _placementGhost.transform.rotation = Quaternion.Euler(0, yRot, 0);
                    break;
                case ToolMode.Wall:
                    int wallRot = Mathf.RoundToInt(CurrentRotation) % 4;
                    Vector3 cellCenter = IsometricGrid.GridToWorld(cell, EditingMap.gridSettings);
                    float halfTile = EditingMap.gridSettings.tileSize * 0.5f;
                    Vector3 edgeOff = wallRot switch
                    {
                        0 => new Vector3(0, 0, halfTile),
                        1 => new Vector3(halfTile, 0, 0),
                        2 => new Vector3(0, 0, -halfTile),
                        3 => new Vector3(-halfTile, 0, 0),
                        _ => Vector3.zero
                    };
                    float wh = SelectedWall?.wallHeight ?? 2.4f;
                    _placementGhost.transform.position = cellCenter + edgeOff + new Vector3(0, wh * 0.5f, 0);
                    // 벽 방향에 따라 큐브 스케일 변경
                    var wallChild = _placementGhost.transform.childCount > 0
                        ? _placementGhost.transform.GetChild(0) : _placementGhost.transform;
                    float wt = SelectedWall?.wallThickness ?? 0.08f;
                    wallChild.localScale = (wallRot == 0 || wallRot == 2)
                        ? new Vector3(EditingMap.gridSettings.tileSize, wh, wt)
                        : new Vector3(wt, wh, EditingMap.gridSettings.tileSize);
                    break;
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
                        string key = $"{tile.gridPosition.x}_{tile.gridPosition.y}_E{tile.rotation}";
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
                            pos.y -= 0.05f; // 3D 벽이 깊이 테스트에서 이기도록 바닥을 살짝 내림
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
                    if (prop.propDefinition?.prefab == null) continue;

                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prop.propDefinition.prefab, propRoot.transform);
                    if (go == null) continue;

                    go.name = $"Prop_{prop.propDefinitionId}_{prop.instanceId}";
                    go.transform.position = prop.GetWorldPosition(EditingMap.gridSettings);
                    if (Mathf.Abs(prop.yRotation) > 0.01f)
                        go.transform.rotation = Quaternion.Euler(0, prop.yRotation, 0) * go.transform.rotation;
                    go.transform.localScale = Vector3.one * prop.scale;

                    int sortOrder = IsometricGrid.GetSortingOrder(prop.gridPosition, IsometricGrid.OBJECT_SORT_BASE)
                                    + prop.propDefinition.sortingOffset
                                    + prop.sortingOffsetOverride;
                    foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
                        sr.sortingOrder = sortOrder;

                    ShadowProxyBuilder.Build(prop.propDefinition, go.transform, editorPreview: false);
                }

                // ── Buildings ──
                var buildRoot = new GameObject("Buildings");
                buildRoot.transform.SetParent(root.transform);

                foreach (var building in EditingMap.buildings)
                {
                    var def = building.buildingDefinition;
                    if (def == null) continue;

                    Vector3 worldPos = building.GetWorldPosition(EditingMap.gridSettings);
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

                    int sortOrder = IsometricGrid.GetSortingOrder(building.gridPosition, IsometricGrid.OBJECT_SORT_BASE)
                                    + def.sortingOffset
                                    + building.sortingOffsetOverride;
                    foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
                        sr.sortingOrder = sortOrder;
                    foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                        mr.sortingOrder = sortOrder;
                }

                // ── MapObjects ──
                var objRoot = new GameObject("MapObjects");
                objRoot.transform.SetParent(root.transform);

                foreach (var obj in EditingMap.mapObjects)
                {
                    Vector3 pos = obj.freePlace ? obj.worldPosition
                        : IsometricGrid.GridToWorld(obj.gridPosition, EditingMap.gridSettings);

                    var go = new GameObject($"MapObj_{obj.objectType}_{obj.instanceId}");
                    go.transform.SetParent(objRoot.transform);
                    go.transform.position = pos;
                    if (Mathf.Abs(obj.yRotation) > 0.01f)
                        go.transform.rotation = Quaternion.Euler(0, obj.yRotation, 0);

                    // 이펙트 프리팹 모드면 프리팹 배치
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

                // ── 맵 데이터 컴포넌트 부착 ──
                var mapRef = root.AddComponent<MapDataReference>();
                mapRef.mapName = EditingMap.mapName;
                mapRef.mapId = EditingMap.mapId;
                mapRef.jsonFileName = filename;
                mapRef.gridSettings = EditingMap.gridSettings;

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
