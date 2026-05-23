using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public class MapEditorWindow : EditorWindow
    {
        static MapData activeMap;
        static TileDefinition selectedBrush;
        static BuildingDefinition selectedBuilding;
        static PropDefinition selectedProp;
        static string activeLayerName = "Ground";
        static EditorToolMode currentTool = EditorToolMode.Paint;
        static bool editorEnabled;
        static Transform parentRoot;
        static readonly Dictionary<string, Transform> layerParents = new();
        static Transform buildingsParent;
        static Transform propsParent;

        Vector2 layerScroll;
        Vector2 paletteScroll;
        Vector2 inspectorScroll;
        string searchFilter = "";
        TileCategory categoryFilter = (TileCategory)(-1);
        bool showSettings;
        bool showInspector = true;
        string newAssetName = "";
        string newLayerName = "";
        static int brushRotation;
        static int brushSize = 1;
        static Material brushMaterialOverride;
        bool stateRestored;

        TileDefinition[] cachedTiles;
        BuildingDefinition[] cachedBuildings;
        PropDefinition[] cachedProps;
        MapPreset[] cachedPresets;

        // Zone management (제거됨 — 프리팹 기반 워크플로우)

        public enum PaletteTab { Tiles, Buildings, Props }
        PaletteTab paletteTab = PaletteTab.Tiles;

        public static MapData ActiveMap => activeMap;
        public static bool EditorEnabled => editorEnabled;
        public static Transform ParentRoot => parentRoot;
        public static TileDefinition SelectedBrush => selectedBrush;
        public static BuildingDefinition SelectedBuilding => selectedBuilding;
        public static PropDefinition SelectedProp => selectedProp;
        public static string ActiveLayerName => activeLayerName;
        public static EditorToolMode CurrentTool => currentTool;

        public static Transform GetLayerParent(string layerName)
        {
            if (layerParents.TryGetValue(layerName, out var t) && t != null)
                return t;
            return GetOrCreateMapRoot();
        }

        public static Transform GetBuildingsParent() => buildingsParent != null ? buildingsParent : GetOrCreateMapRoot();
        public static Transform GetPropsParent() => propsParent != null ? propsParent : GetOrCreateMapRoot();

        /// <summary>
        /// Auto-creates a root parent for the current map: "Map_[mapName]".
        /// Each map/region gets its own root so they can be managed independently.
        /// If parentRoot is manually set, uses that instead.
        /// </summary>
        public static Transform GetOrCreateMapRoot()
        {
            // Manual override takes priority
            if (parentRoot != null) return parentRoot;
            if (activeMap == null) return null;

            string rootName = "Map_" + activeMap.mapName;

            // Find existing root in scene
            foreach (var rootGO in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (rootGO.name == rootName)
                    return rootGO.transform;
            }

            // Create new root (permanent, not DontSave)
            var go = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(go, "Create Map Root");
            return go.transform;
        }

        public enum EditorToolMode { Paint, Repaint, Erase, Select, Building, Walkability, Road, Prop, Connection }
        // Note: Select and Walkability are hidden from toolbar but kept for backward compat
        public enum EraseTarget { All, TilesOnly, BuildingsOnly, PropsOnly }
        EraseTarget eraseTarget = EraseTarget.All;

        [MenuItem("Tools/Isometric Map Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<MapEditorWindow>("Map Editor");
            window.minSize = new Vector2(340, 500);
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            RefreshAllAssets();
            RestoreState();
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SaveState();
        }

        void RestoreState()
        {
            if (!stateRestored)
            {
                MapData cached = EditorStateCache.LoadCachedMap();
                if (cached != null && activeMap == null)
                {
                    activeMap = cached;
                    editorEnabled = EditorStateCache.LoadCachedEnabled();
                    activeLayerName = EditorStateCache.LoadCachedLayer();
                    currentTool = (EditorToolMode)EditorStateCache.LoadCachedTool();
                    LivePreviewManager.InvalidateTracking();
                }
                stateRestored = true;
            }
        }

        void SaveState()
        {
            EditorStateCache.SaveMapState(activeMap, editorEnabled, activeLayerName, (int)currentTool);
        }

        void RefreshAllAssets()
        {
            cachedTiles = LoadAll<TileDefinition>("t:TileDefinition");
            cachedBuildings = LoadAll<BuildingDefinition>("t:BuildingDefinition");
            cachedProps = LoadAll<PropDefinition>("t:PropDefinition");
            cachedPresets = LoadAll<MapPreset>("t:MapPreset");
        }

        static T[] LoadAll<T>(string filter) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets(filter);
            var list = new List<T>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) list.Add(asset);
            }
            return list.ToArray();
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

            if (!editorEnabled)
            {
                EditorGUILayout.HelpBox("Map Editor is disabled. Toggle the switch above to start editing.", MessageType.Info);
                return;
            }

            DrawToolbar();
            DrawPresetBar();
            DrawToolOptions();
            EditorGUILayout.Space(2);

            float totalHeight = position.height - 110;
            float topHeight = Mathf.Min(160, totalHeight * 0.25f);

            EditorGUILayout.BeginVertical(GUILayout.Height(topHeight));
            DrawLayerPanel();
            DrawGridSettings();
            EditorGUILayout.EndVertical();

            DrawSeparator();

            EditorGUILayout.BeginVertical();
            DrawPalette();
            EditorGUILayout.EndVertical();

            DrawStatusBar();
        }

        // ─── Zone Management (제거됨 — 프리팹 기반 워크플로우) ─────────

        // (Zone 메서드 전체 제거됨)

        // ─── Map Selector ───────────────────────────────────────────────

        void DrawMapSelector()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            bool newEnabled = GUILayout.Toggle(editorEnabled, editorEnabled ? "ON" : "OFF", EditorStyles.toolbarButton, GUILayout.Width(36));
            if (EditorGUI.EndChangeCheck())
            {
                editorEnabled = newEnabled;
                if (!editorEnabled)
                {
                    // OFF: 프리뷰만 제거
                    LivePreviewManager.ClearPreview();
                }
                else
                {
                    // ON: 프리뷰 복원
                    LivePreviewManager.InvalidateTracking();
                }
                SaveState();
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            activeMap = (MapData)EditorGUILayout.ObjectField(activeMap, typeof(MapData), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (activeMap != null && !editorEnabled)
                    editorEnabled = true;
                SaveState();
                LivePreviewManager.InvalidateTracking();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("⚙", EditorStyles.toolbarButton, GUILayout.Width(24)))
                showSettings = !showSettings;

            EditorGUILayout.EndHorizontal();

            if (showSettings)
                DrawSettings();
        }

        void DrawSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Hierarchy Parents", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Root", GUILayout.Width(42));
            parentRoot = (Transform)EditorGUILayout.ObjectField(parentRoot, typeof(Transform), true);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "Root: 수동 지정 (비워두면 'Map_[맵이름]' 자동 생성)\n" +
                "지역별 맵마다 자동으로 별도 루트 부모가 생성됩니다.\n" +
                "레이어별 부모: Layers 패널에서 각 레이어 옆에 지정\n" +
                "Buildings/Props 부모: 각 탭 상단에서 지정",
                MessageType.None);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Sprite Folders", EditorStyles.miniLabel);
            DrawFolderField("Tiles", TileAutoImporter.TileSpritePath, v => TileAutoImporter.TileSpritePath = v);
            DrawFolderField("Buildings", TileAutoImporter.BuildingSpritePath, v => TileAutoImporter.BuildingSpritePath = v);
            DrawFolderField("Props", TileAutoImporter.PropSpritePath, v => TileAutoImporter.PropSpritePath = v);
            DrawFolderField("Output", TileAutoImporter.AssetOutputPath, v => TileAutoImporter.AssetOutputPath = v);
            EditorGUILayout.HelpBox(
                "Tiles/Buildings/Props: 여기에 이미지를 넣고 Sync 하면 자동으로 에셋 생성\n" +
                "Output: 자동 생성된 에셋(.asset)이 저장되는 폴더",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Folders", GUILayout.Height(20)))
            {
                EnsureFolderExists(TileAutoImporter.TileSpritePath);
                EnsureFolderExists(TileAutoImporter.BuildingSpritePath);
                EnsureFolderExists(TileAutoImporter.PropSpritePath);
                EnsureFolderExists(TileAutoImporter.AssetOutputPath);
                AssetDatabase.Refresh();
            }
            if (GUILayout.Button("Reset to Default", GUILayout.Height(20)))
            {
                EditorPrefs.DeleteKey("IsometricMap_TileSpritePath");
                EditorPrefs.DeleteKey("IsometricMap_BuildingSpritePath");
                EditorPrefs.DeleteKey("IsometricMap_PropSpritePath");
                EditorPrefs.DeleteKey("IsometricMap_AssetOutputPath");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ─── Toolbar ────────────────────────────────────────────────────

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Toggle(currentTool == EditorToolMode.Paint, "Paint", EditorStyles.toolbarButton))
                SetTool(EditorToolMode.Paint);
            if (GUILayout.Toggle(currentTool == EditorToolMode.Repaint, "Repaint", EditorStyles.toolbarButton))
                SetTool(EditorToolMode.Repaint);
            if (GUILayout.Toggle(currentTool == EditorToolMode.Erase, "Del", EditorStyles.toolbarButton))
                SetTool(EditorToolMode.Erase);
            if (GUILayout.Toggle(currentTool == EditorToolMode.Building, "Build", EditorStyles.toolbarButton))
                SetTool(EditorToolMode.Building);
            if (GUILayout.Toggle(currentTool == EditorToolMode.Prop, "Prop", EditorStyles.toolbarButton))
                SetTool(EditorToolMode.Prop);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Load", EditorStyles.toolbarButton))
            {
                var loaded = MapSceneGenerator.LoadFromPrefab();
                if (loaded != null)
                {
                    activeMap = loaded;
                    editorEnabled = true;
                    activeLayerName = "Ground";
                    if (activeMap.layers.Count > 0)
                        activeLayerName = activeMap.layers[0].layerName;
                    SaveState();
                    LivePreviewManager.InvalidateTracking();
                    SceneView.RepaintAll();
                    Repaint();
                }
            }
            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                MapSceneGenerator.SaveAsPrefab(activeMap);

            GUILayout.Space(4);

            if (GUILayout.Button("Export", EditorStyles.toolbarButton))
                MapExporter.ExportToJson(activeMap);
            if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
            {
                var result = MapValidator.Validate(activeMap);
                MapValidator.LogResults(result, activeMap.mapName);
            }

            EditorGUILayout.EndHorizontal();
        }

        // ─── Preset Bar ─────────────────────────────────────────────────

        void DrawPresetBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Preset:", GUILayout.Width(46));

            // Dropdown to load a preset
            if (cachedPresets != null && cachedPresets.Length > 0)
            {
                string[] presetNames = new string[cachedPresets.Length + 1];
                presetNames[0] = "-- Load Preset --";
                for (int i = 0; i < cachedPresets.Length; i++)
                {
                    presetNames[i + 1] = cachedPresets[i].presetName;
                    if (string.IsNullOrEmpty(presetNames[i + 1]))
                        presetNames[i + 1] = cachedPresets[i].name;
                }

                int selected = EditorGUILayout.Popup(0, presetNames, EditorStyles.toolbarPopup, GUILayout.Width(140));
                if (selected > 0)
                {
                    var preset = cachedPresets[selected - 1];
                    if (EditorUtility.DisplayDialog("Load Preset",
                        "\"" + preset.presetName + "\" 프리셋을 현재 맵에 덮어쓰시겠습니까?\n기존 데이터는 모두 교체됩니다.",
                        "덮어쓰기", "취소"))
                    {
                        Undo.RecordObject(activeMap, "Load Preset");
                        preset.ApplyTo(activeMap);
                        EditorUtility.SetDirty(activeMap);
                        LivePreviewManager.InvalidateTracking();
                        SceneView.RepaintAll();
                        Debug.Log("[MapEditor] Preset \"" + preset.presetName + "\" loaded into \"" + activeMap.mapName + "\"");
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("(없음)", EditorStyles.miniLabel, GUILayout.Width(140));
            }

            // Save to new preset
            if (GUILayout.Button("Save New", EditorStyles.toolbarButton, GUILayout.Width(68)))
            {
                SaveMapAsNewPreset();
            }

            // Overwrite existing preset
            if (cachedPresets != null && cachedPresets.Length > 0)
            {
                if (GUILayout.Button("Overwrite", EditorStyles.toolbarButton, GUILayout.Width(68)))
                {
                    ShowOverwritePresetMenu();
                }
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("↻", EditorStyles.toolbarButton, GUILayout.Width(22)))
                RefreshAllAssets();

            EditorGUILayout.EndHorizontal();
        }

        void SaveMapAsNewPreset()
        {
            string defaultFolder = "Assets/IsometricMapEditor/Presets";
            EnsureFolderExists(defaultFolder);

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Map Preset", activeMap.mapName + "_Preset", "asset",
                "Save current map as preset", defaultFolder);
            if (string.IsNullOrEmpty(path)) return;

            var preset = ScriptableObject.CreateInstance<MapPreset>();
            preset.CaptureFrom(activeMap);
            preset.presetName = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            RefreshAllAssets();
            Debug.Log("[MapEditor] Preset saved: " + path);
        }

        void ShowOverwritePresetMenu()
        {
            var menu = new GenericMenu();
            for (int i = 0; i < cachedPresets.Length; i++)
            {
                var preset = cachedPresets[i];
                string label = preset.presetName;
                if (string.IsNullOrEmpty(label)) label = preset.name;
                menu.AddItem(new GUIContent(label), false, () =>
                {
                    if (EditorUtility.DisplayDialog("Overwrite Preset",
                        "\"" + preset.presetName + "\" 프리셋에 현재 맵 데이터를 덮어쓰시겠습니까?",
                        "덮어쓰기", "취소"))
                    {
                        Undo.RecordObject(preset, "Overwrite Preset");
                        preset.CaptureFrom(activeMap);
                        EditorUtility.SetDirty(preset);
                        AssetDatabase.SaveAssets();
                        Debug.Log("[MapEditor] Preset \"" + preset.presetName + "\" overwritten.");
                    }
                });
            }
            menu.ShowAsContext();
        }

        void SetTool(EditorToolMode tool)
        {
            currentTool = tool;
            RoadTool.SetActive(tool == EditorToolMode.Road);
            WalkabilityPaintTool.SetActive(tool == EditorToolMode.Walkability);
            PropPlaceTool.SetActive(tool == EditorToolMode.Prop);
            BuildingPlaceTool.SetActive(tool == EditorToolMode.Building);
            ConnectionTool.SetActive(tool == EditorToolMode.Connection);
            SaveState();
        }

        void DrawToolOptions()
        {
            switch (currentTool)
            {
                case EditorToolMode.Paint:
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    EditorGUILayout.LabelField("Brush:", GUILayout.Width(38));
                    string[] sizeLabels = { "1×1", "2×2", "3×3", "4×4", "5×5" };
                    int sizeIdx = Mathf.Clamp(brushSize - 1, 0, sizeLabels.Length - 1);
                    int newIdx = GUILayout.Toolbar(sizeIdx, sizeLabels, EditorStyles.toolbarButton, GUILayout.Width(250));
                    brushSize = newIdx + 1;
                    if (selectedBrush != null && selectedBrush.IsWall)
                    {
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("Edge:", GUILayout.Width(34));
                        string[] edgeLabels = { "N", "E", "S", "W" };
                        brushRotation = GUILayout.Toolbar(brushRotation, edgeLabels, EditorStyles.toolbarButton, GUILayout.Width(120));
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("[ ] = size, R = rotate", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();

                    // Material override row
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    EditorGUILayout.LabelField("Mat:", GUILayout.Width(30));
                    brushMaterialOverride = (Material)EditorGUILayout.ObjectField(
                        brushMaterialOverride, typeof(Material), false, GUILayout.Width(160));
                    if (brushMaterialOverride != null)
                    {
                        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(42)))
                            brushMaterialOverride = null;
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("tile material override", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                    break;

                case EditorToolMode.Repaint:
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    EditorGUILayout.LabelField("Brush:", GUILayout.Width(38));
                    string[] repaintSizeLabels = { "1×1", "2×2", "3×3", "4×4", "5×5" };
                    int repaintSizeIdx = Mathf.Clamp(brushSize - 1, 0, repaintSizeLabels.Length - 1);
                    int newRepaintIdx = GUILayout.Toolbar(repaintSizeIdx, repaintSizeLabels, EditorStyles.toolbarButton, GUILayout.Width(250));
                    brushSize = newRepaintIdx + 1;
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("[ ] = size — 기존 타일만 교체", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                    break;

                case EditorToolMode.Erase:
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    EditorGUILayout.LabelField("Target:", GUILayout.Width(45));
                    string[] eraseTargetLabels = { "All", "Tiles", "Buildings", "Props" };
                    int etIdx = (int)eraseTarget;
                    int newEtIdx = GUILayout.Toolbar(etIdx, eraseTargetLabels, EditorStyles.toolbarButton, GUILayout.Width(220));
                    eraseTarget = (EraseTarget)newEtIdx;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    EditorGUILayout.LabelField("Brush:", GUILayout.Width(38));
                    string[] eraseSizeLabels = { "1×1", "2×2", "3×3", "4×4", "5×5" };
                    int eraseSizeIdx = Mathf.Clamp(brushSize - 1, 0, eraseSizeLabels.Length - 1);
                    int newEraseIdx = GUILayout.Toolbar(eraseSizeIdx, eraseSizeLabels, EditorStyles.toolbarButton, GUILayout.Width(250));
                    brushSize = newEraseIdx + 1;
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("[ ] = size", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                    break;

                case EditorToolMode.Prop:
                    DrawFreePlaceToolbar("Prop",
                        PropPlaceTool.FreeMode, v => PropPlaceTool.FreeMode = v,
                        PropPlaceTool.PlacementRotation, v => PropPlaceTool.PlacementRotation = v,
                        PropPlaceTool.PlacementScale, v => PropPlaceTool.PlacementScale = v);
                    break;

                case EditorToolMode.Building:
                    DrawFreePlaceToolbar("Build",
                        BuildingPlaceTool.FreeMode, v => BuildingPlaceTool.FreeMode = v,
                        BuildingPlaceTool.PlacementRotation, v => BuildingPlaceTool.PlacementRotation = v,
                        BuildingPlaceTool.PlacementScale, v => BuildingPlaceTool.PlacementScale = v);
                    break;
            }
        }

        void DrawFreePlaceToolbar(string label,
            bool freeMode, System.Action<bool> setFree,
            float rotation, System.Action<float> setRotation,
            float scale, System.Action<float> setScale)
        {
            // Row 1: Free/Grid toggle
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Mode:", GUILayout.Width(38));
            int modeIdx = freeMode ? 0 : 1;
            int newModeIdx = GUILayout.Toolbar(modeIdx, new[] { "Free", "Grid" }, EditorStyles.toolbarButton, GUILayout.Width(100));
            if (newModeIdx != modeIdx) setFree(newModeIdx == 0);

            GUILayout.Space(12);

            EditorGUILayout.LabelField("Rot:", GUILayout.Width(28));
            float newRot = EditorGUILayout.FloatField(rotation, GUILayout.Width(50));
            if (newRot != rotation) setRotation(newRot);

            GUILayout.Space(8);

            EditorGUILayout.LabelField("Scale:", GUILayout.Width(38));
            float newScale = EditorGUILayout.Slider(scale, 0.1f, 5f, GUILayout.Width(120));
            if (!Mathf.Approximately(newScale, scale)) setScale(newScale);

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Q/E=rotate, +/-=scale", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // Row 2: Hints
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(
                freeMode ? "LClick=place, RClick=remove nearest" : "LClick=place on cell, RClick=remove",
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        // ─── Layer Panel ─────────────────────────────────────────────────

        int renamingLayerIndex = -1;
        string renamingLayerValue = "";

        void DrawLayerPanel()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
            if (GUILayout.Button("Sync Unity Layers", EditorStyles.miniButton, GUILayout.Width(110)))
                SyncUnityLayers();
            EditorGUILayout.EndHorizontal();

            if (activeMap.layers.Count == 0)
            {
                if (GUILayout.Button("+ Add Ground Layer"))
                {
                    activeMap.GetOrCreateLayer("Ground", 0);
                    EnsureUnityLayerExists("Ground");
                }
            }

            for (int i = 0; i < activeMap.layers.Count; i++)
            {
                var layer = activeMap.layers[i];
                bool isActive = activeLayerName == layer.layerName;

                EditorGUILayout.BeginHorizontal();

                // Visibility toggle
                layer.isVisible = EditorGUILayout.Toggle(layer.isVisible, GUILayout.Width(20));

                // Layer name (editable via double-click)
                if (renamingLayerIndex == i)
                {
                    renamingLayerValue = EditorGUILayout.TextField(renamingLayerValue, GUILayout.Width(80));
                    if (GUILayout.Button("✓", GUILayout.Width(20)))
                    {
                        if (!string.IsNullOrWhiteSpace(renamingLayerValue))
                        {
                            Undo.RecordObject(activeMap, "Rename Layer");
                            if (activeLayerName == layer.layerName)
                                activeLayerName = renamingLayerValue.Trim();
                            layer.layerName = renamingLayerValue.Trim();
                            EditorUtility.SetDirty(activeMap);
                        }
                        renamingLayerIndex = -1;
                    }
                }
                else
                {
                    var style = isActive ? EditorStyles.boldLabel : EditorStyles.label;
                    if (GUILayout.Button(layer.layerName, style, GUILayout.Width(80)))
                        activeLayerName = layer.layerName;

                    // Double-click to rename
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2
                        && lastRect.Contains(Event.current.mousePosition))
                    {
                        renamingLayerIndex = i;
                        renamingLayerValue = layer.layerName;
                        Event.current.Use();
                    }
                }

                EditorGUILayout.LabelField($"({layer.tiles.Count})", GUILayout.Width(30));

                // Unity Layer dropdown
                int currentUnityLayer = Mathf.Max(0, layer.unityLayer < 0 ? 0 : layer.unityLayer);
                int newUnityLayer = EditorGUILayout.LayerField(currentUnityLayer, GUILayout.Width(70));
                if (newUnityLayer != currentUnityLayer)
                {
                    layer.unityLayer = newUnityLayer;
                    EditorUtility.SetDirty(activeMap);
                }

                // Delete layer
                if (activeMap.layers.Count > 1)
                {
                    if (GUILayout.Button("×", GUILayout.Width(18), GUILayout.Height(18)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Layer",
                            $"'{layer.layerName}' 레이어와 {layer.tiles.Count}개의 타일을 삭제합니까?",
                            "삭제", "취소"))
                        {
                            Undo.RecordObject(activeMap, "Delete Layer");
                            activeMap.layers.RemoveAt(i);
                            if (activeLayerName == layer.layerName && activeMap.layers.Count > 0)
                                activeLayerName = activeMap.layers[0].layerName;
                            EditorUtility.SetDirty(activeMap);
                            LivePreviewManager.InvalidateTracking();
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            // Add layer
            EditorGUILayout.BeginHorizontal();
            newLayerName = EditorGUILayout.TextField(newLayerName, GUILayout.Height(18));
            if (GUILayout.Button("+", GUILayout.Width(24), GUILayout.Height(18)))
            {
                string name = string.IsNullOrWhiteSpace(newLayerName)
                    ? $"Layer_{activeMap.layers.Count}"
                    : newLayerName.Trim();
                activeMap.GetOrCreateLayer(name, activeMap.layers.Count * 10);
                EnsureUnityLayerExists(name);
                EditorUtility.SetDirty(activeMap);
                newLayerName = "";
            }
            EditorGUILayout.EndHorizontal();
        }

        void SyncUnityLayers()
        {
            int created = 0;
            foreach (var layer in activeMap.layers)
            {
                if (EnsureUnityLayerExists(layer.layerName))
                {
                    layer.unityLayer = LayerMask.NameToLayer(layer.layerName);
                    created++;
                }
                else
                {
                    int existing = LayerMask.NameToLayer(layer.layerName);
                    if (existing >= 0)
                        layer.unityLayer = existing;
                }
            }
            EditorUtility.SetDirty(activeMap);
            if (created > 0)
                Debug.Log($"[MapEditor] {created} Unity Layer(s) created and synced.");
            else
                Debug.Log("[MapEditor] All layers already exist in Unity. Assignments updated.");
        }

        static bool EnsureUnityLayerExists(string layerName)
        {
            if (LayerMask.NameToLayer(layerName) >= 0) return false;

            var tagManager = new UnityEditor.SerializedObject(
                AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            var layersProp = tagManager.FindProperty("layers");

            // Find first empty user layer slot (8-31)
            for (int i = 8; i < 32; i++)
            {
                var slot = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[MapEditor] Unity Layer '{layerName}' created at slot {i}.");
                    return true;
                }
            }

            Debug.LogWarning($"[MapEditor] No empty Unity Layer slots. Cannot create '{layerName}'.");
            return false;
        }

        // ─── Grid Settings ───────────────────────────────────────────────

        void DrawGridSettings()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("W", GUILayout.Width(16));
            activeMap.gridSettings.mapWidth = EditorGUILayout.IntField(activeMap.gridSettings.mapWidth, GUILayout.Width(40));
            EditorGUILayout.LabelField("H", GUILayout.Width(14));
            activeMap.gridSettings.mapHeight = EditorGUILayout.IntField(activeMap.gridSettings.mapHeight, GUILayout.Width(40));
            EditorGUILayout.LabelField("Size", GUILayout.Width(30));
            activeMap.gridSettings.tileSize = EditorGUILayout.FloatField(activeMap.gridSettings.tileSize, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(activeMap);
                SceneView.RepaintAll();
            }
        }

        // ─── Palette (Integrated) ────────────────────────────────────────

        void DrawPalette()
        {
            // Auto-sync palette tab with current tool
            if (currentTool == EditorToolMode.Building)
                paletteTab = PaletteTab.Buildings;
            else if (currentTool == EditorToolMode.Prop)
                paletteTab = PaletteTab.Props;
            else
                paletteTab = PaletteTab.Tiles;

            // Header with Sync button
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string paletteName = paletteTab == PaletteTab.Tiles ? "Tiles"
                : paletteTab == PaletteTab.Buildings ? "Buildings" : "Props";
            EditorGUILayout.LabelField(paletteName, EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Sync", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                var (t, b, p) = TileAutoImporter.SyncAll();
                if (t + b + p > 0)
                    Debug.Log($"[AutoImport] Created: {t} tiles, {b} buildings, {p} props");
                else
                    Debug.Log("[AutoImport] All sprites already imported.");
                RefreshAllAssets();
            }
            if (GUILayout.Button("↻", EditorStyles.toolbarButton, GUILayout.Width(22)))
                RefreshAllAssets();

            EditorGUILayout.EndHorizontal();

            // Search
            searchFilter = EditorGUILayout.TextField("Search", searchFilter);
            if (paletteTab == PaletteTab.Tiles)
                categoryFilter = (TileCategory)EditorGUILayout.EnumFlagsField("Category", categoryFilter);

            EditorGUILayout.Space(2);
            paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll);

            switch (paletteTab)
            {
                case PaletteTab.Tiles: DrawTilePalette(); break;
                case PaletteTab.Buildings: DrawBuildingPalette(); break;
                case PaletteTab.Props: DrawPropPalette(); break;
            }

            EditorGUILayout.EndScrollView();

            DrawSeparator();
            DrawAssetInspector();
        }

        bool showHelp = true;

        void DrawShortcutHelp()
        {
            var headerStyle = new GUIStyle(EditorStyles.foldout) { richText = true };
            showHelp = EditorGUILayout.Foldout(showHelp, "<b>도구 & 단축키 가이드</b>", true, headerStyle);
            if (!showHelp) return;

            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                richText = true,
                wordWrap = true,
                padding = new RectOffset(8, 8, 1, 1)
            };

            Color bg = new Color(0.18f, 0.18f, 0.22f, 1f);
            var rect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(rect, bg);

            // ── Tools ──
            EditorGUILayout.LabelField("<color=#FFD700><b>── 도구 (Tools) ──</b></color>", style);

            EditorGUILayout.LabelField(
                "<color=#7fef7f><b>Paint</b></color>  " +
                "선택한 타일을 빈 셀/기존 셀에 배치. 벽 타일은 N/E/S/W 방향 지정 가능.\n" +
                "  <color=#aaa>LClick/LDrag</color> 배치  " +
                "<color=#aaa>[  ]</color> 브러시 크기 1~5  " +
                "<color=#aaa>R</color> 벽 방향 순환(N→E→S→W)",
                style, GUILayout.Height(30));

            EditorGUILayout.LabelField(
                "<color=#5fbfff><b>Repaint</b></color>  " +
                "이미 타일이 깔린 셀만 선택한 타일로 교체. 빈 셀은 무시됨.",
                style, GUILayout.Height(18));

            EditorGUILayout.LabelField(
                "<color=#ff7f7f><b>Del</b></color>  " +
                "셀의 타일/건물/소품 삭제. Target 필터로 대상 선택 가능.\n" +
                "  <color=#aaa>LClick/LDrag</color> 삭제  " +
                "<color=#aaa>[  ]</color> 브러시 크기",
                style, GUILayout.Height(30));

            EditorGUILayout.LabelField(
                "<color=#7fb3ff><b>Build</b></color>  " +
                "건물 프리팹을 배치. Free/Grid 모드 전환 가능.\n" +
                "  <color=#aaa>LClick</color> 배치  " +
                "<color=#aaa>RClick</color> 삭제  " +
                "<color=#aaa>Q/E</color> 회전 ±15°  " +
                "<color=#aaa>+/-</color> 스케일 ±0.1",
                style, GUILayout.Height(30));

            EditorGUILayout.LabelField(
                "<color=#7fefbf><b>Prop</b></color>  " +
                "소품 프리팹을 배치. Free/Grid 모드.\n" +
                "  <color=#aaa>LClick</color> 배치  " +
                "<color=#aaa>RClick</color> 삭제  " +
                "<color=#aaa>Q/E</color> 회전  " +
                "<color=#aaa>+/-</color> 스케일",
                style, GUILayout.Height(30));

            EditorGUILayout.Space(4);

            // ── Action Buttons ──
            EditorGUILayout.LabelField("<color=#FFD700><b>── 액션 버튼 ──</b></color>", style);

            EditorGUILayout.LabelField(
                "<color=#5fafff><b>Load</b></color>  " +
                "프리팹에서 MapData를 불러와 편집 모드 진입.\n" +
                "<color=#5fdf5f><b>Save</b></color>  " +
                "현재 맵을 .prefab 파일로 저장. 씬에 직접 배치해서 사용.",
                style, GUILayout.Height(30));

            EditorGUILayout.LabelField(
                "<color=#dfdf5f><b>Export</b></color>  " +
                "맵 데이터를 JSON 파일로 내보내기.",
                style, GUILayout.Height(18));

            EditorGUILayout.LabelField(
                "<color=#dfaf5f><b>Validate</b></color>  " +
                "맵 데이터 검증. 누락된 정의, 범위 밖 좌표 등을 콘솔에 리포트.",
                style, GUILayout.Height(18));

            EditorGUILayout.Space(4);

            // ── Palette ──
            EditorGUILayout.LabelField("<color=#FFD700><b>── 팔레트 ──</b></color>", style);

            EditorGUILayout.LabelField(
                "도구에 따라 자동 전환: Paint/Repaint/Del → Tiles, Build → Buildings, Prop → Props.\n" +
                "건물과 소품은 <b>프리팹</b> 기반. BuildingDefinition/PropDefinition에 prefab을 지정.",
                style, GUILayout.Height(30));

            EditorGUILayout.Space(4);

            EditorGUILayout.Space(4);

            // ── General ──
            EditorGUILayout.LabelField("<color=#FFD700><b>── 일반 ──</b></color>", style);

            EditorGUILayout.LabelField(
                "<color=#ccc><b>ON/OFF</b></color>  " +
                "에디터 토글. OFF 해도 배치된 오브젝트는 유지됨(캐시). 리컴파일/플레이모드 후 자동 복원.\n" +
                "<color=#ccc><b>Map Root</b></color>  " +
                "맵마다 'Map_[이름]' 루트 부모가 자동 생성됨. 지역별 독립 관리.\n" +
                "<color=#ccc><b>Settings(⚙)</b></color>  " +
                "루트 부모 수동 지정, 스프라이트 폴더 경로 설정, 폴더 자동 생성.",
                style, GUILayout.Height(42));

            EditorGUILayout.EndVertical();
        }

        void DrawAssetInspector()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            showInspector = EditorGUILayout.Foldout(showInspector, "Inspector / Create", true, EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();

            if (!showInspector) return;

            inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll, GUILayout.MaxHeight(200));

            switch (paletteTab)
            {
                case PaletteTab.Tiles:
                    DrawTileInspector();
                    break;
                case PaletteTab.Buildings:
                    DrawBuildingInspector();
                    DrawBuildingCreator();
                    break;
                case PaletteTab.Props:
                    DrawPropInspector();
                    DrawPropCreator();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawTileInspector()
        {
            if (selectedBrush == null) { EditorGUILayout.HelpBox("Select a tile", MessageType.None); return; }

            EditorGUI.BeginChangeCheck();
            selectedBrush.tileId = EditorGUILayout.TextField("ID", selectedBrush.tileId);
            selectedBrush.sprite = (Sprite)EditorGUILayout.ObjectField("Sprite", selectedBrush.sprite, typeof(Sprite), false);
            selectedBrush.category = (TileCategory)EditorGUILayout.EnumPopup("Category", selectedBrush.category);
            selectedBrush.isWalkable = EditorGUILayout.Toggle("Walkable", selectedBrush.isWalkable);
            selectedBrush.size = EditorGUILayout.Vector2IntField("Size", selectedBrush.size);
            selectedBrush.sortingOffset = EditorGUILayout.IntField("Sorting Offset", selectedBrush.sortingOffset);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Material", EditorStyles.miniLabel);
            selectedBrush.material = (Material)EditorGUILayout.ObjectField("Default Material", selectedBrush.material, typeof(Material), false);

            if (selectedBrush.IsWall)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Wall", EditorStyles.miniLabel);
                selectedBrush.wallHeight = EditorGUILayout.FloatField("Height", selectedBrush.wallHeight);
                selectedBrush.wallThickness = EditorGUILayout.FloatField("Thickness", selectedBrush.wallThickness);
            }

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(selectedBrush);

            if (GUILayout.Button("Delete Tile", GUILayout.Height(18)))
                if (DeleteAssetWithConfirm(selectedBrush)) selectedBrush = null;
        }

        void DrawBuildingInspector()
        {
            if (selectedBuilding == null) { EditorGUILayout.HelpBox("Select a building or create new", MessageType.None); return; }

            EditorGUILayout.LabelField(selectedBuilding.displayName, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            selectedBuilding.buildingId = EditorGUILayout.TextField("ID", selectedBuilding.buildingId);
            selectedBuilding.displayName = EditorGUILayout.TextField("Name", selectedBuilding.displayName);
            selectedBuilding.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", selectedBuilding.prefab, typeof(GameObject), false);
            selectedBuilding.icon = (Sprite)EditorGUILayout.ObjectField("Icon", selectedBuilding.icon, typeof(Sprite), false);
            selectedBuilding.footprint = EditorGUILayout.Vector2IntField("Footprint", selectedBuilding.footprint);
            selectedBuilding.isEnterable = EditorGUILayout.Toggle("Enterable", selectedBuilding.isEnterable);
            if (selectedBuilding.isEnterable)
            {
                selectedBuilding.interiorMapId = EditorGUILayout.TextField("Interior Map ID", selectedBuilding.interiorMapId);
                selectedBuilding.entryCell = EditorGUILayout.Vector2IntField("Entry Cell", selectedBuilding.entryCell);
            }
            selectedBuilding.sortingOffset = EditorGUILayout.IntField("Sorting Offset", selectedBuilding.sortingOffset);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(selectedBuilding);

            // Generate Prefab from Icon sprite
            if (selectedBuilding.prefab == null && selectedBuilding.icon != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("No prefab assigned. Click below to auto-generate from icon sprite.", MessageType.Info);
                if (GUILayout.Button("Generate Prefab from Icon", GUILayout.Height(24)))
                {
                    string folder = TileAutoImporter.AssetOutputPath + "/Prefabs/Buildings";
                    var prefab = PrefabGenerator.GenerateForBuilding(selectedBuilding, selectedBuilding.icon, folder);
                    if (prefab != null)
                    {
                        AssetDatabase.SaveAssets();
                        LivePreviewManager.InvalidateTracking();
                        Debug.Log($"[MapEditor] Generated prefab for building '{selectedBuilding.displayName}'");
                    }
                }
            }
            else if (selectedBuilding.prefab == null)
            {
                EditorGUILayout.HelpBox("Assign an Icon sprite first, then you can auto-generate a prefab.", MessageType.Warning);
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Delete Building", GUILayout.Height(18)))
                if (DeleteAssetWithConfirm(selectedBuilding)) selectedBuilding = null;
        }

        void DrawPropInspector()
        {
            if (selectedProp == null) { EditorGUILayout.HelpBox("Select a prop or create new", MessageType.None); return; }

            EditorGUILayout.LabelField(selectedProp.displayName, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            selectedProp.propId = EditorGUILayout.TextField("ID", selectedProp.propId);
            selectedProp.displayName = EditorGUILayout.TextField("Name", selectedProp.displayName);
            selectedProp.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", selectedProp.prefab, typeof(GameObject), false);
            selectedProp.icon = (Sprite)EditorGUILayout.ObjectField("Icon", selectedProp.icon, typeof(Sprite), false);
            selectedProp.footprint = EditorGUILayout.Vector2IntField("Footprint", selectedProp.footprint);
            selectedProp.blocksWalkability = EditorGUILayout.Toggle("Blocks Walk", selectedProp.blocksWalkability);
            selectedProp.sortingOffset = EditorGUILayout.IntField("Sorting Offset", selectedProp.sortingOffset);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(selectedProp);

            // Generate Prefab from Icon sprite
            if (selectedProp.prefab == null && selectedProp.icon != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("No prefab assigned. Click below to auto-generate from icon sprite.", MessageType.Info);
                if (GUILayout.Button("Generate Prefab from Icon", GUILayout.Height(24)))
                {
                    string folder = TileAutoImporter.AssetOutputPath + "/Prefabs/Props";
                    var prefab = PrefabGenerator.GenerateForProp(selectedProp, selectedProp.icon, folder);
                    if (prefab != null)
                    {
                        AssetDatabase.SaveAssets();
                        LivePreviewManager.InvalidateTracking();
                        Debug.Log($"[MapEditor] Generated prefab for prop '{selectedProp.displayName}'");
                    }
                }
            }
            else if (selectedProp.prefab == null)
            {
                EditorGUILayout.HelpBox("Assign an Icon sprite first, then you can auto-generate a prefab.", MessageType.Warning);
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Delete Prop", GUILayout.Height(18)))
                if (DeleteAssetWithConfirm(selectedProp)) selectedProp = null;
        }

        void DrawBuildingCreator()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("New Building", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            newAssetName = EditorGUILayout.TextField(newAssetName);
            if (GUILayout.Button("+", GUILayout.Width(24)) && !string.IsNullOrWhiteSpace(newAssetName))
            {
                string folder = TileAutoImporter.AssetOutputPath + "/Buildings";
                EnsureFolderExists(folder);
                string path = $"{folder}/{newAssetName}.asset";
                if (AssetDatabase.LoadAssetAtPath<BuildingDefinition>(path) == null)
                {
                    var building = CreateInstance<BuildingDefinition>();
                    building.buildingId = newAssetName.ToLower().Replace(" ", "_");
                    building.displayName = newAssetName;
                    building.footprint = new Vector2Int(2, 2);
                    AssetDatabase.CreateAsset(building, path);
                    AssetDatabase.SaveAssets();
                    RefreshAllAssets();
                    selectedBuilding = building;
                    selectedBrush = null;
                    selectedProp = null;
                    newAssetName = "";
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawPropCreator()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("New Prop", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            newAssetName = EditorGUILayout.TextField(newAssetName);
            if (GUILayout.Button("+", GUILayout.Width(24)) && !string.IsNullOrWhiteSpace(newAssetName))
            {
                string folder = TileAutoImporter.AssetOutputPath + "/Props";
                EnsureFolderExists(folder);
                string path = $"{folder}/{newAssetName}.asset";
                if (AssetDatabase.LoadAssetAtPath<PropDefinition>(path) == null)
                {
                    var prop = CreateInstance<PropDefinition>();
                    prop.propId = newAssetName.ToLower().Replace(" ", "_");
                    prop.displayName = newAssetName;
                    prop.footprint = Vector2Int.one;
                    AssetDatabase.CreateAsset(prop, path);
                    AssetDatabase.SaveAssets();
                    RefreshAllAssets();
                    selectedProp = prop;
                    selectedBrush = null;
                    selectedBuilding = null;
                    newAssetName = "";
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawFolderField(string label, string currentPath, System.Action<string> setter)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(60));

            string edited = EditorGUILayout.TextField(currentPath);
            if (edited != currentPath)
                setter(edited);

            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                string abs = EditorUtility.OpenFolderPanel($"Select {label} Folder", "Assets", "");
                if (!string.IsNullOrEmpty(abs))
                {
                    string dataPath = Application.dataPath;
                    if (abs.StartsWith(dataPath))
                        setter("Assets" + abs[dataPath.Length..]);
                    else
                        Debug.LogWarning($"[MapEditor] Folder must be inside the Assets directory.");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // ─── Tile Palette ────────────────────────────────────────────────

        void DrawTilePalette()
        {
            if (cachedTiles == null || cachedTiles.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No tiles found.\n" +
                    "1) Put sprite images in the Tiles folder\n" +
                    "2) Click [Sync] to auto-import",
                    MessageType.Info);
                return;
            }

            int columns = Mathf.Max(1, (int)(position.width / 76));
            int col = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (var tile in cachedTiles)
            {
                if (tile == null) continue;
                if (!MatchesTileFilter(tile.tileId, tile.category)) continue;

                bool isSelected = selectedBrush == tile;

                EditorGUILayout.BeginVertical(GUILayout.Width(68));
                Rect btnRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64));

                if (isSelected)
                {
                    EditorGUI.DrawRect(btnRect, new Color(0.2f, 0.6f, 1f, 0.3f));
                    Handles.color = Color.cyan;
                    Handles.DrawSolidRectangleWithOutline(btnRect, Color.clear, Color.cyan);
                }

                if (tile.sprite != null)
                    DrawSpritePreview(btnRect, tile.sprite);
                else
                    EditorGUI.LabelField(btnRect, "?", EditorStyles.centeredGreyMiniLabel);

                if (GUI.Button(btnRect, GUIContent.none, GUIStyle.none))
                {
                    selectedBrush = tile;
                    selectedBuilding = null;
                    selectedProp = null;
                    if (currentTool != EditorToolMode.Paint && currentTool != EditorToolMode.Erase)
                        SetTool(EditorToolMode.Paint);
                }

                EditorGUILayout.LabelField(tile.tileId, EditorStyles.miniLabel, GUILayout.Width(64), GUILayout.Height(14));
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
        }

        // ─── Building Palette ────────────────────────────────────────────

        void DrawBuildingPalette()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Parent", GUILayout.Width(40));
            buildingsParent = (Transform)EditorGUILayout.ObjectField(buildingsParent, typeof(Transform), true);
            EditorGUILayout.EndHorizontal();

            if (cachedBuildings == null || cachedBuildings.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No buildings found.\n" +
                    "1) Put sprite images in the Buildings folder\n" +
                    "2) Click [Sync] to auto-import",
                    MessageType.Info);
                return;
            }

            foreach (var building in cachedBuildings)
            {
                if (building == null) continue;
                if (!MatchesSearch(building.displayName)) continue;

                bool isSelected = selectedBuilding == building;
                EditorGUILayout.BeginHorizontal(isSelected ? "selectionRect" : "box");

                {
                    var rect = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48));
                    if (building.icon != null)
                        DrawSpritePreview(rect, building.icon);
                    else if (building.prefab != null)
                    {
                        var preview = AssetPreview.GetAssetPreview(building.prefab);
                        if (preview != null) GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
                        else EditorGUI.LabelField(rect, "?", EditorStyles.centeredGreyMiniLabel);
                    }
                    else
                        EditorGUI.LabelField(rect, "?", EditorStyles.centeredGreyMiniLabel);
                }

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(building.displayName, EditorStyles.boldLabel);
                string bInfo = $"{building.footprint.x}x{building.footprint.y}";
                if (building.prefab == null) bInfo += " (no prefab)";
                EditorGUILayout.LabelField(bInfo, EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    selectedBuilding = building;
                    selectedBrush = null;
                    selectedProp = null;
                    SetTool(EditorToolMode.Building);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        // ─── Prop Palette ────────────────────────────────────────────────

        void DrawPropPalette()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Parent", GUILayout.Width(40));
            propsParent = (Transform)EditorGUILayout.ObjectField(propsParent, typeof(Transform), true);
            EditorGUILayout.EndHorizontal();

            if (cachedProps == null || cachedProps.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No props found.\n" +
                    "1) Put sprite images in the Props folder\n" +
                    "2) Click [Sync] to auto-import",
                    MessageType.Info);
                return;
            }

            foreach (var prop in cachedProps)
            {
                if (prop == null) continue;
                if (!MatchesSearch(prop.displayName)) continue;

                bool isSelected = selectedProp == prop;
                EditorGUILayout.BeginHorizontal(isSelected ? "selectionRect" : "box");

                {
                    var rect = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48));
                    if (prop.icon != null)
                        DrawSpritePreview(rect, prop.icon);
                    else if (prop.prefab != null)
                    {
                        var preview = AssetPreview.GetAssetPreview(prop.prefab);
                        if (preview != null) GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
                        else EditorGUI.LabelField(rect, "?", EditorStyles.centeredGreyMiniLabel);
                    }
                    else
                        EditorGUI.LabelField(rect, "?", EditorStyles.centeredGreyMiniLabel);
                }

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(prop.displayName, EditorStyles.boldLabel);
                string pInfo = $"{prop.footprint.x}x{prop.footprint.y}";
                if (prop.prefab == null) pInfo += " (no prefab)";
                EditorGUILayout.LabelField(pInfo, EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    selectedProp = prop;
                    selectedBrush = null;
                    selectedBuilding = null;
                    SetTool(EditorToolMode.Prop);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        // ─── Filters ─────────────────────────────────────────────────────

        bool MatchesTileFilter(string id, TileCategory cat)
        {
            if (!string.IsNullOrEmpty(searchFilter) && !id.ToLower().Contains(searchFilter.ToLower()))
                return false;
            if ((int)categoryFilter != -1 && (categoryFilter & cat) == 0)
                return false;
            return true;
        }

        bool MatchesSearch(string name)
        {
            return string.IsNullOrEmpty(searchFilter) || name.ToLower().Contains(searchFilter.ToLower());
        }

        // ─── Drawing Helpers ──────────────────────────────────────────────

        static void DrawSpritePreview(Rect rect, Sprite sprite)
        {
            Texture2D tex = sprite.texture;
            Rect texRect = sprite.textureRect;
            Rect uv = new(texRect.x / tex.width, texRect.y / tex.height,
                          texRect.width / tex.width, texRect.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv);
        }

        void DrawSeparator()
        {
            var rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
        }

        void DrawStatusBar()
        {
            var gs = activeMap.gridSettings;
            EditorGUILayout.LabelField(
                $"Grid {gs.mapWidth}x{gs.mapHeight} | Tile {gs.tileSize} | {currentTool} | {activeLayerName}",
                EditorStyles.miniLabel
            );
        }

        // ─── Map Creation ─────────────────────────────────────────────────

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

        // ─── Scene GUI (Paint/Erase/Build on Scene View) ─────────────────

        void OnSceneGUI(SceneView sceneView)
        {
            if (activeMap == null || !editorEnabled) return;

            IsometricGridGizmoDrawer.DrawGrid(activeMap.gridSettings);

            Event e = Event.current;

            // MUST be called before event handling to prevent Scene View from consuming mouse events
            if (currentTool != EditorToolMode.Select)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            // R key cycles brush rotation (0→1→2→3) for wall edge direction
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R && !e.alt && !e.control && !e.shift)
            {
                brushRotation = (brushRotation + 1) % 4;
                e.Use();
                sceneView.Repaint();
                Repaint();
            }

            // [ / ] keys to decrease / increase brush size
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.LeftBracket && !e.alt && !e.control)
            {
                brushSize = Mathf.Max(1, brushSize - 1);
                e.Use();
                sceneView.Repaint();
                Repaint();
            }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.RightBracket && !e.alt && !e.control)
            {
                brushSize = Mathf.Min(5, brushSize + 1);
                e.Use();
                sceneView.Repaint();
                Repaint();
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
                HandleSceneClick(e, sceneView);
            else if (e.type == EventType.MouseDrag && e.button == 0 && !e.alt)
                HandleSceneClick(e, sceneView);

            DrawCursorHighlight(e);
        }

        Vector3 GetMouseWorldOnXZPlane(Event e)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane xzPlane = new Plane(Vector3.up, Vector3.zero);
            if (xzPlane.Raycast(ray, out float dist))
                return ray.GetPoint(dist);
            return Vector3.zero;
        }

        /// <summary>
        /// Returns all cells covered by the current brush size, centered on the given cell.
        /// </summary>
        List<Vector2Int> GetBrushCells(Vector2Int center)
        {
            var cells = new List<Vector2Int>();
            int half = brushSize / 2;
            for (int dx = 0; dx < brushSize; dx++)
            {
                for (int dy = 0; dy < brushSize; dy++)
                {
                    var c = new Vector2Int(center.x + dx - half, center.y + dy - half);
                    if (activeMap.gridSettings.IsInBounds(c))
                        cells.Add(c);
                }
            }
            return cells;
        }

        /// <summary>
        /// Auto-sync walkability map from tile definitions.
        /// If a tile has isWalkable=false → Blocked, isWalkable=true → Walkable.
        /// Creates walkability data if it doesn't exist.
        /// </summary>
        void SyncWalkabilityForCells(List<Vector2Int> cells)
        {
            if (activeMap == null) return;

            // Auto-create walkability if not initialized
            if (activeMap.walkability == null)
                activeMap.InitializeWalkability();

            foreach (var c in cells)
            {
                var tile = activeMap.GetTileAt(c);
                if (tile != null && tile.tileDefinition != null)
                {
                    activeMap.walkability.SetCell(c,
                        tile.tileDefinition.isWalkable ? WalkableType.Walkable : WalkableType.Blocked);
                }
            }
        }

        void HandleSceneClick(Event e, SceneView sceneView)
        {
            Vector3 mouseWorld = GetMouseWorldOnXZPlane(e);
            Vector2Int cell = IsometricGrid.WorldToGrid(mouseWorld, activeMap.gridSettings);

            if (!activeMap.gridSettings.IsInBounds(cell)) return;

            switch (currentTool)
            {
                case EditorToolMode.Paint:
                    if (selectedBrush == null) return;
                    Undo.RecordObject(activeMap, "Paint Tile");
                    foreach (var c in GetBrushCells(cell))
                    {
                        var tile = new PlacedTile
                        {
                            gridPosition = c,
                            tileDefinitionId = selectedBrush.tileId,
                            tileDefinition = selectedBrush,
                            rotation = brushRotation,
                            materialOverride = brushMaterialOverride
                        };
                        activeMap.PlaceTile(tile, activeLayerName);
                    }
                    SyncWalkabilityForCells(GetBrushCells(cell));
                    activeMap.MarkDirty();
                    LivePreviewManager.InvalidateTracking();
                    EditorUtility.SetDirty(activeMap);
                    break;

                case EditorToolMode.Repaint:
                    if (selectedBrush == null) return;
                    Undo.RecordObject(activeMap, "Repaint Tile");
                    foreach (var c in GetBrushCells(cell))
                    {
                        activeMap.RepaintTile(c, selectedBrush, null);
                    }
                    SyncWalkabilityForCells(GetBrushCells(cell));
                    activeMap.MarkDirty();
                    LivePreviewManager.InvalidateTracking();
                    EditorUtility.SetDirty(activeMap);
                    break;

                case EditorToolMode.Erase:
                    Undo.RecordObject(activeMap, "Erase");
                    var eraseCells = GetBrushCells(cell);
                    var eraseSet = new System.Collections.Generic.HashSet<Vector2Int>(eraseCells);

                    // Remove tiles (when target is All or TilesOnly)
                    if (eraseTarget == EraseTarget.All || eraseTarget == EraseTarget.TilesOnly)
                    {
                        foreach (var c in eraseCells)
                            activeMap.RemoveTileAtAllLayers(c);

                        // Clear walkability for erased cells
                        if (activeMap.walkability != null)
                            foreach (var c in eraseCells)
                                activeMap.walkability.SetCell(c, WalkableType.Walkable);
                    }

                    // Remove buildings (when target is All or BuildingsOnly)
                    if (eraseTarget == EraseTarget.All || eraseTarget == EraseTarget.BuildingsOnly)
                    {
                        for (int i = activeMap.buildings.Count - 1; i >= 0; i--)
                        {
                            var b = activeMap.buildings[i];
                            if (b.freePlace)
                            {
                                var bCell = IsometricGrid.WorldToGrid(b.worldPosition, activeMap.gridSettings);
                                if (eraseSet.Contains(bCell))
                                    activeMap.buildings.RemoveAt(i);
                            }
                            else
                            {
                                bool hit = false;
                                if (b.buildingDefinition != null)
                                {
                                    foreach (var oc in b.buildingDefinition.GetOccupiedCells(b.gridPosition))
                                        if (eraseSet.Contains(oc)) { hit = true; break; }
                                }
                                else
                                {
                                    hit = eraseSet.Contains(b.gridPosition);
                                }
                                if (hit) activeMap.buildings.RemoveAt(i);
                            }
                        }
                    }

                    // Remove props (when target is All or PropsOnly)
                    if (eraseTarget == EraseTarget.All || eraseTarget == EraseTarget.PropsOnly)
                    {
                        for (int i = activeMap.props.Count - 1; i >= 0; i--)
                        {
                            var p = activeMap.props[i];
                            var pCell = p.freePlace
                                ? IsometricGrid.WorldToGrid(p.worldPosition, activeMap.gridSettings)
                                : p.gridPosition;
                            if (eraseSet.Contains(pCell))
                                activeMap.props.RemoveAt(i);
                        }
                    }

                    activeMap.MarkDirty();
                    LivePreviewManager.InvalidateTracking();
                    EditorUtility.SetDirty(activeMap);
                    break;

                // Building is handled by BuildingPlaceTool via SceneViewInputHandler
            }

            e.Use();
            SceneView.RepaintAll();
            Repaint();
        }

        void DrawCursorHighlight(Event e)
        {
            Vector3 mouseWorld = GetMouseWorldOnXZPlane(e);
            Vector2Int cell = IsometricGrid.WorldToGrid(mouseWorld, activeMap.gridSettings);

            if (!activeMap.gridSettings.IsInBounds(cell)) return;

            Color color = currentTool switch
            {
                EditorToolMode.Paint => new Color(0, 1, 0, 0.4f),
                EditorToolMode.Repaint => new Color(0, 0.7f, 1f, 0.4f),
                EditorToolMode.Erase => new Color(1, 0, 0, 0.4f),
                _ => new Color(1, 1, 0, 0.3f)
            };

            // Draw highlight for each cell in brush area
            bool useBrush = currentTool == EditorToolMode.Paint || currentTool == EditorToolMode.Repaint || currentTool == EditorToolMode.Erase;
            if (useBrush && brushSize > 1)
            {
                foreach (var c in GetBrushCells(cell))
                {
                    Vector3[] corners = IsometricGrid.GetCellWorldCorners(c, activeMap.gridSettings);
                    Handles.color = color;
                    Handles.DrawAAConvexPolygon(corners);
                }

                // Draw outer border for visibility
                Handles.color = new Color(color.r, color.g, color.b, 0.9f);
                foreach (var c in GetBrushCells(cell))
                {
                    Vector3[] corners = IsometricGrid.GetCellWorldCorners(c, activeMap.gridSettings);
                    Handles.DrawPolyLine(corners[0], corners[1], corners[2], corners[3], corners[0]);
                }

                // Brush size label
                Vector3 labelPos = IsometricGrid.GridToWorld(cell, activeMap.gridSettings);
                Handles.Label(labelPos + Vector3.up * 0.5f, $"{brushSize}×{brushSize}", EditorStyles.whiteBoldLabel);
            }
            else
            {
                Vector3[] corners = IsometricGrid.GetCellWorldCorners(cell, activeMap.gridSettings);
                Handles.color = color;
                Handles.DrawAAConvexPolygon(corners);
            }

            // Show wall edge indicator on every cell in brush area
            if (currentTool == EditorToolMode.Paint && selectedBrush != null && selectedBrush.IsWall)
            {
                float halfTile = activeMap.gridSettings.tileSize * 0.5f;
                float thickness = 0.08f;

                // Edge offset + line direction per rotation
                // 0=N(+Z), 1=E(+X), 2=S(-Z), 3=W(-X)
                Vector3 edgeOff = brushRotation switch
                {
                    0 => new Vector3(0, 0, halfTile),
                    1 => new Vector3(halfTile, 0, 0),
                    2 => new Vector3(0, 0, -halfTile),
                    _ => new Vector3(-halfTile, 0, 0)
                };
                Vector3 lineDir = (brushRotation == 0 || brushRotation == 2) ? Vector3.right : Vector3.forward;

                string[] edgeNames = { "N", "E", "S", "W" };

                Handles.color = new Color(1, 0.4f, 0, 0.9f);
                foreach (var c in GetBrushCells(cell))
                {
                    Vector3 center = IsometricGrid.GridToWorld(c, activeMap.gridSettings);
                    Vector3 edgeCenter = center + edgeOff;

                    // Thick edge line
                    Vector3 p1 = edgeCenter - lineDir * halfTile;
                    Vector3 p2 = edgeCenter + lineDir * halfTile;
                    Handles.DrawAAConvexPolygon(
                        p1 + Vector3.up * thickness, p2 + Vector3.up * thickness,
                        p2 - Vector3.up * thickness, p1 - Vector3.up * thickness);
                }

                Vector3 labelPos = IsometricGrid.GridToWorld(cell, activeMap.gridSettings) + edgeOff;
                Handles.Label(labelPos + Vector3.up * 0.4f, $"Wall: {edgeNames[brushRotation]}", EditorStyles.whiteBoldLabel);
            }

            // Erase mode: draw building/prop footprint outlines so user knows where to click
            if (currentTool == EditorToolMode.Erase &&
                (eraseTarget == EraseTarget.All || eraseTarget == EraseTarget.BuildingsOnly || eraseTarget == EraseTarget.PropsOnly))
            {
                // Buildings
                if (eraseTarget != EraseTarget.PropsOnly)
                {
                    foreach (var b in activeMap.buildings)
                    {
                        if (b.buildingDefinition == null) continue;
                        var occupiedCells = b.freePlace
                            ? new List<Vector2Int> { IsometricGrid.WorldToGrid(b.worldPosition, activeMap.gridSettings) }
                            : b.buildingDefinition.GetOccupiedCells(b.gridPosition);

                        // Check if mouse is hovering over this building
                        bool hovered = false;
                        var brushCells = GetBrushCells(cell);
                        var brushSet = new System.Collections.Generic.HashSet<Vector2Int>(brushCells);
                        foreach (var oc in occupiedCells)
                            if (brushSet.Contains(oc)) { hovered = true; break; }

                        Color bColor = hovered
                            ? new Color(1f, 0.2f, 0.2f, 0.5f)   // red = will be deleted
                            : new Color(1f, 0.6f, 0f, 0.25f);    // orange outline
                        Color borderColor = hovered
                            ? new Color(1f, 0f, 0f, 0.9f)
                            : new Color(1f, 0.6f, 0f, 0.6f);

                        foreach (var oc in occupiedCells)
                        {
                            if (!activeMap.gridSettings.IsInBounds(oc)) continue;
                            Vector3[] corners = IsometricGrid.GetCellWorldCorners(oc, activeMap.gridSettings);
                            Handles.color = bColor;
                            Handles.DrawAAConvexPolygon(corners);
                            Handles.color = borderColor;
                            Handles.DrawPolyLine(corners[0], corners[1], corners[2], corners[3], corners[0]);
                        }

                        // Label with building name
                        Vector3 bPos = b.freePlace
                            ? b.worldPosition
                            : IsometricGrid.GridToWorld(b.gridPosition, activeMap.gridSettings);
                        Handles.Label(bPos + Vector3.up * 0.3f, b.buildingDefinition.displayName,
                            hovered ? EditorStyles.whiteBoldLabel : EditorStyles.whiteLabel);
                    }
                }

                // Props
                if (eraseTarget != EraseTarget.BuildingsOnly)
                {
                    foreach (var p in activeMap.props)
                    {
                        if (p.propDefinition == null) continue;
                        var pCell = p.freePlace
                            ? IsometricGrid.WorldToGrid(p.worldPosition, activeMap.gridSettings)
                            : p.gridPosition;
                        if (!activeMap.gridSettings.IsInBounds(pCell)) continue;

                        var brushCells2 = GetBrushCells(cell);
                        bool pHovered = brushCells2.Contains(pCell);

                        Vector3[] pCorners = IsometricGrid.GetCellWorldCorners(pCell, activeMap.gridSettings);
                        Handles.color = pHovered
                            ? new Color(1f, 0.2f, 0.2f, 0.5f)
                            : new Color(0.2f, 0.8f, 1f, 0.25f);
                        Handles.DrawAAConvexPolygon(pCorners);
                        Handles.color = pHovered
                            ? new Color(1f, 0f, 0f, 0.9f)
                            : new Color(0.2f, 0.8f, 1f, 0.6f);
                        Handles.DrawPolyLine(pCorners[0], pCorners[1], pCorners[2], pCorners[3], pCorners[0]);

                        Vector3 pPos = IsometricGrid.GridToWorld(pCell, activeMap.gridSettings);
                        Handles.Label(pPos + Vector3.up * 0.2f, p.propDefinition.displayName,
                            pHovered ? EditorStyles.whiteBoldLabel : EditorStyles.whiteLabel);
                    }
                }
            }

            Handles.color = Color.white;

            SceneView.RepaintAll();
        }

        // ─── Asset Delete ─────────────────────────────────────────────────

        bool DeleteAssetWithConfirm(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path)) return false;
            if (!EditorUtility.DisplayDialog("Delete Asset", $"Delete \"{asset.name}\"?\n{path}", "Delete", "Cancel"))
                return false;

            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            RefreshAllAssets();
            return true;
        }

        // ─── Folder Utility ───────────────────────────────────────────────

        static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
