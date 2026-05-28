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

        public void SelectObjectType(MapObjectType type)
        {
            SelectedObjectType = type;
            CurrentTool = ToolMode.MapObject;
            UI.RefreshToolbar();
        }

        public void RotateSelection(int direction)
        {
            CurrentRotation = (CurrentRotation + direction + 4) % 4;
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
            if (!EditingMap.gridSettings.IsInBounds(cell) && CurrentTool != ToolMode.Prop) return;

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
                yRotation = CurrentRotation * 90f,
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
                yRotation = CurrentRotation * 90f,
                scale = 1f
            };

            EditingMap.buildings.Add(building);
            SpawnBuildingVisual(building);
            UI.RefreshAll();
        }

        void PlaceMapObject(Vector2Int cell, Vector3 worldPos)
        {
            SaveUndoSnapshot();

            var obj = new PlacedMapObject
            {
                instanceId = System.Guid.NewGuid().ToString("N")[..8],
                objectType = SelectedObjectType,
                gridPosition = cell,
                freePlace = false,
                worldPosition = IsometricGrid.GridToWorld(cell, EditingMap.gridSettings),
                yRotation = CurrentRotation * 90f,
                label = SelectedObjectType.ToString()
            };

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
            go.transform.rotation = Quaternion.Euler(0, prop.yRotation, 0);
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
                go.transform.rotation = Quaternion.Euler(0, building.rotation * 90f, 0);
            }
            else
            {
                // 프리팹 없으면 풋프린트 크기의 큐브 박스로 표시
                go = new GameObject($"Building_{def.buildingId}_{building.instanceId}");
                go.transform.SetParent(_buildingRoot);
                go.transform.position = worldPos;

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
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"Marker_{obj.objectType}_{obj.instanceId}";
            go.transform.SetParent(_objectRoot);
            go.transform.position = obj.GetWorldPosition(EditingMap.gridSettings) + Vector3.up * 0.5f;
            go.transform.localScale = Vector3.one * 0.4f;

            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            var renderer = go.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat.shader.name == "Hidden/InternalErrorShader")
                mat = new Material(Shader.Find("Standard"));

            mat.color = GetObjectTypeColor(obj.objectType);
            renderer.sharedMaterial = mat;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = Vector3.up * 1f;

            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = string.IsNullOrEmpty(obj.label) ? obj.objectType.ToString() : obj.label;
            tm.fontSize = 24;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = GetObjectTypeColor(obj.objectType);

            _objectMarkers[obj.instanceId] = go;
        }

        static Color GetObjectTypeColor(MapObjectType type) => type switch
        {
            MapObjectType.SpawnPoint => Color.green,
            MapObjectType.EscapePoint => Color.cyan,
            MapObjectType.LootContainer => new Color(1f, 0.7f, 0f),
            MapObjectType.EnemySpawn => Color.red,
            MapObjectType.ItemDrop => new Color(0.8f, 0.5f, 1f),
            MapObjectType.Trigger => Color.yellow,
            MapObjectType.Custom => Color.white,
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
