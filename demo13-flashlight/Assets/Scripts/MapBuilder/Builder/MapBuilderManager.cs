using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace IsometricMapEditor
{
    public enum ToolMode
    {
        Tile,
        Wall,
        Prop,
        Building,
        MapObject,
        Eraser
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

        // Resize mode
        public bool ResizeMode { get; set; }
        readonly List<(Renderer rend, Color origColor)> _resizeHoverRenderers = new();
        string _resizeHoverInstanceId;
        public string ResizeHoverInfo { get; private set; }

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

        public int CurrentRotation { get; private set; }
        public bool SnapToGrid { get; set; }

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

            // 기존 게임 UI/HUD 모두 비활성화
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                canvas.gameObject.SetActive(false);

            // UI
            UI = gameObject.AddComponent<MapBuilderUI>();
            UI.Initialize(this);

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
            CurrentTool = mode;
            CurrentRotation = 0;
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
            UI.RefreshToolbar();
        }

        public void SelectProp(PropDefinition prop)
        {
            SelectedProp = prop;
            CurrentTool = ToolMode.Prop;
            UI.RefreshToolbar();
        }

        public void SelectBuilding(BuildingDefinition building)
        {
            SelectedBuilding = building;
            CurrentTool = ToolMode.Building;
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

        public void RotateSelection(int direction)
        {
            CurrentRotation = (CurrentRotation + direction + 24) % 24;
            UI.RefreshStatus();
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

            var tile = new PlacedTile
            {
                gridPosition = cell,
                tileDefinitionId = SelectedTile.tileId,
                tileDefinition = SelectedTile,
                rotation = CurrentRotation,
                flipX = false
            };

            EditingMap.PlaceTile(tile, "Ground");
            var layer = EditingMap.GetOrCreateLayer("Ground");
            _tileRenderer.RenderSingleTile(tile, layer, EditingMap.gridSettings);
        }

        void PlaceWall(Vector2Int cell)
        {
            if (SelectedWall == null) return;
            SaveUndoSnapshot();

            var tile = new PlacedTile
            {
                gridPosition = cell,
                tileDefinitionId = SelectedWall.tileId,
                tileDefinition = SelectedWall,
                rotation = CurrentRotation,
                flipX = false
            };

            EditingMap.PlaceTile(tile, "Walls");

            string key = $"{cell.x}_{cell.y}_E{CurrentRotation}";
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
                rotation = CurrentRotation,
                freePlace = free,
                worldPosition = finalPos,
                yRotation = CurrentRotation * 15f,
                scale = 1f
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
                rotation = CurrentRotation,
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
            var obj = new PlacedMapObject
            {
                instanceId = System.Guid.NewGuid().ToString("N")[..8],
                objectType = SelectedObjectType,
                gridPosition = cell,
                freePlace = free,
                worldPosition = free ? worldPos : IsometricGrid.GridToWorld(cell, EditingMap.gridSettings),
                yRotation = CurrentRotation * 15f,
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
                    string key = $"{cell.x}_{cell.y}_E{CurrentRotation}";
                    EditingMap.RemoveWallEdge(cell, CurrentRotation);
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
                    go.transform.localRotation = Quaternion.Euler(35.264f, 45f, 0);
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

        void ClearAllVisuals()
        {
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

        public string GetSavePath() => Path.Combine(Application.persistentDataPath, "Maps");

        public void SaveMap(string filename)
        {
            string dir = GetSavePath();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, filename + ".json");
            string json = MapSerializer.Serialize(EditingMap);
            File.WriteAllText(path, json);
            Debug.Log($"[MapBuilder] Saved: {path}");
        }

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
