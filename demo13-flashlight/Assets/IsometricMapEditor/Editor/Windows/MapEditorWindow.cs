using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public class MapEditorWindow : EditorWindow
    {
        static MapData activeMap;
        static TileDefinition selectedBrush;
        static string activeLayerName = "Ground";
        static EditorToolMode currentTool = EditorToolMode.Paint;

        Vector2 tilePaletteScroll;
        Vector2 layerScroll;
        TileDefinition[] allTiles;

        public static MapData ActiveMap => activeMap;
        public static TileDefinition SelectedBrush => selectedBrush;
        public static string ActiveLayerName => activeLayerName;
        public static EditorToolMode CurrentTool => currentTool;

        public enum EditorToolMode { Paint, Erase, Select, Building, Walkability, Road, Prop, Connection }

        [MenuItem("Tools/Isometric Map Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<MapEditorWindow>("Map Editor");
            window.minSize = new Vector2(300, 400);
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            RefreshTilePalette();
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void RefreshTilePalette()
        {
            string[] guids = AssetDatabase.FindAssets("t:TileDefinition");
            allTiles = new TileDefinition[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                allTiles[i] = AssetDatabase.LoadAssetAtPath<TileDefinition>(path);
            }
        }

        void OnGUI()
        {
            DrawMapSelector();
            if (activeMap == null)
            {
                EditorGUILayout.HelpBox("Select or create a MapData asset to begin editing.", MessageType.Info);
                if (GUILayout.Button("Create New Map"))
                    CreateNewMap();
                return;
            }

            DrawToolbar();
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            DrawLayerPanel();
            EditorGUILayout.Space(8);
            DrawTilePalette();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            DrawGridSettings();
            DrawStatusBar();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        void DrawMapSelector()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            activeMap = (MapData)EditorGUILayout.ObjectField("Map", activeMap, typeof(MapData), false);
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
            EditorGUILayout.EndHorizontal();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Toggle(currentTool == EditorToolMode.Paint, "Paint", EditorStyles.toolbarButton))
                SetTool(EditorToolMode.Paint);
            if (GUILayout.Toggle(currentTool == EditorToolMode.Erase, "Erase", EditorStyles.toolbarButton))
                SetTool(EditorToolMode.Erase);
            if (GUILayout.Toggle(currentTool == EditorToolMode.Select, "Select", EditorStyles.toolbarButton))
                SetTool(EditorToolMode.Select);
            if (GUILayout.Toggle(currentTool == EditorToolMode.Building, "Build", EditorStyles.toolbarButton))
                SetTool(EditorToolMode.Building);
            if (GUILayout.Toggle(currentTool == EditorToolMode.Walkability, "Walk", EditorStyles.toolbarButton))
                SetTool(EditorToolMode.Walkability);
            if (GUILayout.Toggle(currentTool == EditorToolMode.Prop, "Prop", EditorStyles.toolbarButton))
                SetTool(EditorToolMode.Prop);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Export", EditorStyles.toolbarButton))
                MapExporter.ExportToJson(activeMap);
            if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
            {
                var result = MapValidator.Validate(activeMap);
                MapValidator.LogResults(result, activeMap.mapName);
            }
            if (GUILayout.Button("Scene", EditorStyles.toolbarButton))
                MapSceneGenerator.GenerateScene(activeMap);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                RefreshTilePalette();

            EditorGUILayout.EndHorizontal();
        }

        void DrawLayerPanel()
        {
            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
            layerScroll = EditorGUILayout.BeginScrollView(layerScroll, GUILayout.Height(120));

            if (activeMap.layers.Count == 0)
            {
                if (GUILayout.Button("+ Add Ground Layer"))
                    activeMap.GetOrCreateLayer("Ground", 0);
            }

            foreach (var layer in activeMap.layers)
            {
                EditorGUILayout.BeginHorizontal();
                layer.isVisible = EditorGUILayout.Toggle(layer.isVisible, GUILayout.Width(20));

                bool isActive = activeLayerName == layer.layerName;
                var style = isActive ? EditorStyles.boldLabel : EditorStyles.label;
                if (GUILayout.Button(layer.layerName, style))
                    activeLayerName = layer.layerName;

                EditorGUILayout.LabelField($"({layer.tiles.Count})", GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Layer"))
            {
                string newName = $"Layer_{activeMap.layers.Count}";
                activeMap.GetOrCreateLayer(newName, activeMap.layers.Count * 10);
                EditorUtility.SetDirty(activeMap);
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawTilePalette()
        {
            EditorGUILayout.LabelField("Tile Palette", EditorStyles.boldLabel);

            if (allTiles == null || allTiles.Length == 0)
            {
                EditorGUILayout.HelpBox("No TileDefinition assets found.\nCreate via Assets > Create > Isometric Map > Tile Definition", MessageType.Warning);
                return;
            }

            tilePaletteScroll = EditorGUILayout.BeginScrollView(tilePaletteScroll);

            int columns = Mathf.Max(1, (int)((position.width * 0.4f) / 68));
            int col = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (var tile in allTiles)
            {
                if (tile == null) continue;

                bool isSelected = selectedBrush == tile;
                var btnStyle = isSelected ? "LargeButtonMid" : "LargeButton";

                EditorGUILayout.BeginVertical(GUILayout.Width(64));
                if (tile.sprite != null)
                {
                    Rect texRect = tile.sprite.textureRect;
                    Texture2D tex = tile.sprite.texture;
                    Rect uv = new(
                        texRect.x / tex.width, texRect.y / tex.height,
                        texRect.width / tex.width, texRect.height / tex.height
                    );

                    if (GUILayout.Button("", GUILayout.Width(60), GUILayout.Height(30)))
                        selectedBrush = tile;

                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    GUI.DrawTextureWithTexCoords(lastRect, tex, uv);

                    if (isSelected)
                    {
                        Handles.color = Color.cyan;
                        Handles.DrawSolidRectangleWithOutline(lastRect, Color.clear, Color.cyan);
                    }
                }
                else
                {
                    if (GUILayout.Button("?", GUILayout.Width(60), GUILayout.Height(30)))
                        selectedBrush = tile;
                }

                EditorGUILayout.LabelField(tile.name, EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.EndVertical();

                col++;
                if (col >= columns)
                {
                    col = 0;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        void SetTool(EditorToolMode tool)
        {
            currentTool = tool;
            WalkabilityPaintTool.SetActive(tool == EditorToolMode.Walkability);
            PropPlaceTool.SetActive(tool == EditorToolMode.Prop);
            ConnectionTool.SetActive(tool == EditorToolMode.Connection);
        }

        void DrawGridSettings()
        {
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            activeMap.gridSettings.tileWidth = EditorGUILayout.IntField("Tile Width", activeMap.gridSettings.tileWidth);
            activeMap.gridSettings.tileHeight = EditorGUILayout.IntField("Tile Height", activeMap.gridSettings.tileHeight);
            activeMap.gridSettings.mapWidth = EditorGUILayout.IntField("Map Width", activeMap.gridSettings.mapWidth);
            activeMap.gridSettings.mapHeight = EditorGUILayout.IntField("Map Height", activeMap.gridSettings.mapHeight);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(activeMap);
                SceneView.RepaintAll();
            }
        }

        void DrawStatusBar()
        {
            EditorGUILayout.Space(8);
            var gs = activeMap.gridSettings;
            EditorGUILayout.LabelField(
                $"Grid {gs.mapWidth}x{gs.mapHeight} | Tile {gs.tileWidth}x{gs.tileHeight} | Tool: {currentTool} | Layer: {activeLayerName}",
                EditorStyles.miniLabel
            );
        }

        void CreateNewMap()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Map", "NewMap", "asset", "Save map data");
            if (string.IsNullOrEmpty(path)) return;

            var map = CreateInstance<MapData>();
            map.mapName = System.IO.Path.GetFileNameWithoutExtension(path);
            map.mapId = System.Guid.NewGuid().ToString("N")[..8];
            map.GetOrCreateLayer("Ground", 0);

            AssetDatabase.CreateAsset(map, path);
            AssetDatabase.SaveAssets();
            activeMap = map;
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (activeMap == null) return;

            IsometricGridGizmoDrawer.DrawGrid(activeMap.gridSettings);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                HandleSceneClick(e, sceneView);
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && !e.alt)
            {
                HandleSceneClick(e, sceneView);
            }

            if (currentTool != EditorToolMode.Select)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            DrawCursorHighlight(e);
        }

        void HandleSceneClick(Event e, SceneView sceneView)
        {
            Vector2 mouseWorld = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            Vector2Int cell = IsometricGrid.WorldToGrid(mouseWorld, activeMap.gridSettings);

            if (!activeMap.gridSettings.IsInBounds(cell)) return;

            switch (currentTool)
            {
                case EditorToolMode.Paint:
                    if (selectedBrush == null) return;
                    Undo.RecordObject(activeMap, "Paint Tile");
                    var tile = new PlacedTile
                    {
                        gridPosition = cell,
                        tileDefinitionId = selectedBrush.tileId,
                        tileDefinition = selectedBrush
                    };
                    activeMap.PlaceTile(tile, activeLayerName);
                    EditorUtility.SetDirty(activeMap);
                    break;

                case EditorToolMode.Erase:
                    Undo.RecordObject(activeMap, "Erase Tile");
                    activeMap.RemoveTileAtAllLayers(cell);
                    EditorUtility.SetDirty(activeMap);
                    break;

                case EditorToolMode.Building:
                    var selBuilding = TilePaletteWindow.SelectedBuilding;
                    if (selBuilding == null) return;
                    Undo.RecordObject(activeMap, "Place Building");
                    activeMap.buildings.Add(new PlacedBuilding
                    {
                        instanceId = System.Guid.NewGuid().ToString("N")[..8],
                        gridPosition = cell,
                        buildingDefinitionId = selBuilding.buildingId,
                        buildingDefinition = selBuilding,
                        roofVisible = true
                    });
                    EditorUtility.SetDirty(activeMap);
                    break;
            }

            e.Use();
            SceneView.RepaintAll();
            Repaint();
        }

        void DrawCursorHighlight(Event e)
        {
            Vector2 mouseWorld = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            Vector2Int cell = IsometricGrid.WorldToGrid(mouseWorld, activeMap.gridSettings);

            if (!activeMap.gridSettings.IsInBounds(cell)) return;

            Vector3[] corners = IsometricGrid.GetCellWorldCorners(cell, activeMap.gridSettings);
            Color color = currentTool switch
            {
                EditorToolMode.Paint => new Color(0, 1, 0, 0.4f),
                EditorToolMode.Erase => new Color(1, 0, 0, 0.4f),
                _ => new Color(1, 1, 0, 0.3f)
            };

            Handles.color = color;
            Handles.DrawAAConvexPolygon(corners);
            Handles.color = Color.white;

            SceneView.RepaintAll();
        }
    }
}
