using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace IsometricMapEditor.Editor
{
    /// <summary>
    /// Building Workshop: map editor style window for constructing individual buildings.
    /// Creates building prefabs that can be placed in the map editor.
    /// Layout mirrors MapEditorWindow for consistency.
    /// </summary>
    public class BuildingWorkshopWindow : EditorWindow
    {
        // ─── State ──────────────────────────────────────────────────────
        static BuildingWorkshopData activeData;
        static bool workshopEnabled;
        static WorkshopTool currentTool = WorkshopTool.Paint;
        static TileDefinition selectedTile;
        static PropDefinition selectedProp;
        static int brushRotation;
        static Material brushMaterial;
        bool stateRestored;

        Vector2 paletteScroll;
        Vector2 inspectorScroll;
        string searchFilter = "";

        TileDefinition[] cachedTiles;
        BuildingDefinition[] cachedBuildings;
        PropDefinition[] cachedProps;
        BuildingPreset[] cachedPresets;

        static BuildingDefinition selectedBuilding;

        public enum WorkshopTool { Paint, Wall, Roof, Del, Build, Prop }

        public static BuildingWorkshopData ActiveData => activeData;
        public static bool WorkshopEnabled => workshopEnabled;

        [MenuItem("Tools/Building Workshop")]
        public static void ShowWindow()
        {
            var window = GetWindow<BuildingWorkshopWindow>("Building Workshop");
            window.minSize = new Vector2(340, 500);
        }

        // ─── Lifecycle ──────────────────────────────────────────────────

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            RefreshAssets();
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
                var cached = EditorStateCache.LoadCachedWorkshopData();
                if (cached != null)
                {
                    activeData = cached;
                    workshopEnabled = EditorStateCache.LoadCachedWorkshopEnabled();
                }
                stateRestored = true;
            }
        }

        void SaveState()
        {
            EditorStateCache.SaveWorkshopState(activeData, workshopEnabled);
        }

        void RefreshAssets()
        {
            cachedTiles = LoadAll<TileDefinition>("t:TileDefinition");
            cachedBuildings = LoadAll<BuildingDefinition>("t:BuildingDefinition");
            cachedProps = LoadAll<PropDefinition>("t:PropDefinition");
            cachedPresets = LoadAll<BuildingPreset>("t:BuildingPreset");
        }

        static T[] LoadAll<T>(string filter) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets(filter);
            var list = new List<T>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) list.Add(asset);
            }
            return list.ToArray();
        }

        // ─── Main GUI ───────────────────────────────────────────────────

        void OnGUI()
        {
            DrawHeader();

            if (activeData == null)
            {
                EditorGUILayout.HelpBox("Select or create a BuildingWorkshopData asset.", MessageType.Info);
                if (GUILayout.Button("Create New", GUILayout.Height(24)))
                    CreateNewWorkshopData();
                return;
            }

            if (!workshopEnabled)
            {
                EditorGUILayout.HelpBox("Workshop is OFF. Toggle ON to start building.", MessageType.Info);
                return;
            }

            DrawToolbar();
            DrawToolOptions();
            EditorGUILayout.Space(2);

            // Top: settings + output
            float totalHeight = position.height - 110;
            float topHeight = Mathf.Min(140, totalHeight * 0.25f);

            EditorGUILayout.BeginVertical(GUILayout.Height(topHeight));
            DrawBuildingSettings();
            EditorGUILayout.EndVertical();

            DrawSeparator();

            // Bottom: palette
            EditorGUILayout.BeginVertical();
            DrawPalette();
            EditorGUILayout.EndVertical();
        }

        // ─── Header (ON/OFF + Data selector) ────────────────────────────

        void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            bool newEnabled = GUILayout.Toggle(workshopEnabled,
                workshopEnabled ? "ON" : "OFF", EditorStyles.toolbarButton, GUILayout.Width(36));
            if (EditorGUI.EndChangeCheck())
            {
                workshopEnabled = newEnabled;
                SaveState();
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            activeData = (BuildingWorkshopData)EditorGUILayout.ObjectField(
                activeData, typeof(BuildingWorkshopData), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (activeData != null && !workshopEnabled)
                    workshopEnabled = true;
                SaveState();
                WorkshopPreviewManager.InvalidateTracking();
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
        }

        // ─── Toolbar (Paint Wall Roof Del Prop | Bake Prefab) ───────────

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Toggle(currentTool == WorkshopTool.Paint, "Paint", EditorStyles.toolbarButton))
                currentTool = WorkshopTool.Paint;
            if (GUILayout.Toggle(currentTool == WorkshopTool.Wall, "Wall", EditorStyles.toolbarButton))
                currentTool = WorkshopTool.Wall;
            if (GUILayout.Toggle(currentTool == WorkshopTool.Roof, "Roof", EditorStyles.toolbarButton))
                currentTool = WorkshopTool.Roof;
            if (GUILayout.Toggle(currentTool == WorkshopTool.Del, "Del", EditorStyles.toolbarButton))
                currentTool = WorkshopTool.Del;
            if (GUILayout.Toggle(currentTool == WorkshopTool.Build, "Build", EditorStyles.toolbarButton))
                currentTool = WorkshopTool.Build;
            if (GUILayout.Toggle(currentTool == WorkshopTool.Prop, "Prop", EditorStyles.toolbarButton))
                currentTool = WorkshopTool.Prop;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Bake", EditorStyles.toolbarButton))
                WorkshopBaker.BakeToScene(activeData);
            if (GUILayout.Button("Prefab", EditorStyles.toolbarButton))
                WorkshopBaker.SaveAsPrefab(activeData);

            EditorGUILayout.EndHorizontal();
        }

        // ─── Tool Options (preset, material, wall edge) ─────────────────

        void DrawToolOptions()
        {
            // Preset bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Preset:", GUILayout.Width(46));

            if (cachedPresets != null && cachedPresets.Length > 0)
            {
                string[] names = new string[cachedPresets.Length + 1];
                names[0] = "(load)";
                for (int i = 0; i < cachedPresets.Length; i++)
                    names[i + 1] = string.IsNullOrEmpty(cachedPresets[i].presetName)
                        ? cachedPresets[i].name : cachedPresets[i].presetName;

                int sel = EditorGUILayout.Popup(0, names, EditorStyles.toolbarPopup, GUILayout.Width(100));
                if (sel > 0)
                {
                    var preset = cachedPresets[sel - 1];
                    if (EditorUtility.DisplayDialog("Load Preset",
                        $"\"{preset.presetName}\" 로드?", "OK", "Cancel"))
                    {
                        Undo.RecordObject(activeData, "Load Preset");
                        preset.ApplyTo(activeData);
                        EditorUtility.SetDirty(activeData);
                        WorkshopPreviewManager.InvalidateTracking();
                        SceneView.RepaintAll();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("(none)", EditorStyles.miniLabel, GUILayout.Width(100));
            }

            if (GUILayout.Button("Save New", EditorStyles.toolbarButton, GUILayout.Width(65)))
                SaveNewPreset();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("↻", EditorStyles.toolbarButton, GUILayout.Width(22)))
                RefreshAssets();
            EditorGUILayout.EndHorizontal();

            // Wall edge selector
            if (currentTool == WorkshopTool.Wall)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                EditorGUILayout.LabelField("Edge:", GUILayout.Width(34));
                brushRotation = GUILayout.Toolbar(brushRotation,
                    new[] { "N", "E", "S", "W" }, EditorStyles.toolbarButton, GUILayout.Width(120));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("R = rotate", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            // Material override
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Mat:", GUILayout.Width(30));
            brushMaterial = (Material)EditorGUILayout.ObjectField(
                brushMaterial, typeof(Material), false, GUILayout.Width(160));
            if (brushMaterial != null && GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(42)))
                brushMaterial = null;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // ─── Building Settings (compact, map-editor style) ──────────────

        void DrawBuildingSettings()
        {
            EditorGUI.BeginChangeCheck();

            // Row 1: Name
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name", GUILayout.Width(38));
            activeData.buildingName = EditorGUILayout.TextField(activeData.buildingName);
            EditorGUILayout.EndHorizontal();

            // Row 2: W H TileSize + Roof toggle
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("W", GUILayout.Width(14));
            activeData.gridWidth = EditorGUILayout.IntField(activeData.gridWidth, GUILayout.Width(36));
            EditorGUILayout.LabelField("H", GUILayout.Width(14));
            activeData.gridHeight = EditorGUILayout.IntField(activeData.gridHeight, GUILayout.Width(36));
            EditorGUILayout.LabelField("Size", GUILayout.Width(28));
            activeData.tileSize = EditorGUILayout.FloatField(activeData.tileSize, GUILayout.Width(36));

            GUILayout.Space(8);
            activeData.roofVisible = GUILayout.Toggle(activeData.roofVisible,
                activeData.roofVisible ? "Roof:ON" : "Roof:OFF",
                EditorStyles.miniButton, GUILayout.Width(60));

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                $"F:{activeData.floorTiles.Count} W:{activeData.wallTiles.Count} R:{activeData.roofTiles.Count} B:{activeData.buildings.Count} P:{activeData.props.Count}",
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // Row 3: Output BuildingDefinition
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Output", GUILayout.Width(42));
            activeData.outputDefinition = (BuildingDefinition)EditorGUILayout.ObjectField(
                activeData.outputDefinition, typeof(BuildingDefinition), false);
            EditorGUILayout.EndHorizontal();

            // Row 4: External appearance
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Prefab", GUILayout.Width(42));
            activeData.externalPrefab = (GameObject)EditorGUILayout.ObjectField(
                activeData.externalPrefab, typeof(GameObject), false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Icon", GUILayout.Width(42));
            activeData.externalIcon = (Sprite)EditorGUILayout.ObjectField(
                activeData.externalIcon, typeof(Sprite), false, GUILayout.Height(18));
            if (activeData.externalIcon != null)
            {
                Rect previewRect = GUILayoutUtility.GetRect(36, 36, GUILayout.Width(36));
                DrawSpritePreview(previewRect, activeData.externalIcon);
            }
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(activeData);
                WorkshopPreviewManager.InvalidateTracking();
                SceneView.RepaintAll();
            }

            // ── Quick Prefab: sprite 1장 → 프리팹 즉시 생성 ──
            if (activeData.externalIcon != null && activeData.externalPrefab == null)
            {
                EditorGUILayout.Space(2);
                if (GUILayout.Button("Generate Prefab from Sprite", GUILayout.Height(24)))
                {
                    string folder = TileAutoImporter.AssetOutputPath + "/Prefabs/Buildings";
                    var prefab = PrefabGenerator.CreateBuildingPrefab(
                        activeData.externalIcon, null, folder);
                    if (prefab != null)
                    {
                        activeData.externalPrefab = prefab;
                        EditorUtility.SetDirty(activeData);

                        // Auto-create or sync BuildingDefinition
                        if (activeData.outputDefinition == null)
                        {
                            string defFolder = TileAutoImporter.AssetOutputPath + "/Buildings";
                            EnsureFolderExists(defFolder);
                            string defPath = AssetDatabase.GenerateUniqueAssetPath(
                                $"{defFolder}/{activeData.buildingName}.asset");

                            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
                            def.buildingId = activeData.buildingName.ToLower().Replace(" ", "_");
                            def.displayName = activeData.buildingName;
                            def.prefab = prefab;
                            def.icon = activeData.externalIcon;
                            def.footprint = activeData.GetFootprint();
                            AssetDatabase.CreateAsset(def, defPath);

                            activeData.outputDefinition = def;
                            EditorUtility.SetDirty(activeData);
                            Debug.Log($"[Workshop] BuildingDefinition auto-created: {defPath}");
                        }
                        else
                        {
                            Undo.RecordObject(activeData.outputDefinition, "Sync Prefab");
                            activeData.outputDefinition.prefab = prefab;
                            activeData.outputDefinition.icon = activeData.externalIcon;
                            EditorUtility.SetDirty(activeData.outputDefinition);
                        }

                        AssetDatabase.SaveAssets();
                        Debug.Log($"[Workshop] Prefab generated: {prefab.name}");
                    }
                }
            }
        }

        // ─── Palette (auto-sync with tool) ──────────────────────────────

        void DrawPalette()
        {
            searchFilter = EditorGUILayout.TextField("Search", searchFilter);
            paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll);

            if (currentTool == WorkshopTool.Build)
                DrawBuildingPalette();
            else if (currentTool == WorkshopTool.Prop)
                DrawPropPalette();
            else if (currentTool != WorkshopTool.Del)
                DrawTilePalette();
            else
                EditorGUILayout.HelpBox("LClick = delete tile/wall/building\nRClick = delete prop", MessageType.Info);

            EditorGUILayout.EndScrollView();
        }

        void DrawTilePalette()
        {
            if (cachedTiles == null || cachedTiles.Length == 0)
            {
                EditorGUILayout.HelpBox("No tiles found. Use Sync in Map Editor.", MessageType.Info);
                return;
            }

            int columns = Mathf.Max(1, (int)(position.width / 76));
            int col = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (var tile in cachedTiles)
            {
                if (tile == null) continue;
                if (!string.IsNullOrEmpty(searchFilter)
                    && !tile.tileId.ToLower().Contains(searchFilter.ToLower()))
                    continue;

                // Filter: Wall tool → wall tiles only, Paint/Roof → non-wall
                if (currentTool == WorkshopTool.Wall && !tile.IsWall) continue;
                if ((currentTool == WorkshopTool.Paint || currentTool == WorkshopTool.Roof) && tile.IsWall) continue;

                bool isSelected = selectedTile == tile;
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
                    selectedTile = tile;
                    selectedProp = null;
                }

                EditorGUILayout.LabelField(tile.tileId, EditorStyles.miniLabel,
                    GUILayout.Width(64), GUILayout.Height(14));
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

        void DrawPropPalette()
        {
            if (cachedProps == null || cachedProps.Length == 0)
            {
                EditorGUILayout.HelpBox("No props found.", MessageType.Info);
                return;
            }

            int columns = Mathf.Max(1, (int)(position.width / 76));
            int col = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (var prop in cachedProps)
            {
                if (prop == null) continue;
                if (!string.IsNullOrEmpty(searchFilter)
                    && !prop.displayName.ToLower().Contains(searchFilter.ToLower()))
                    continue;

                bool isSelected = selectedProp == prop;
                EditorGUILayout.BeginVertical(GUILayout.Width(68));
                Rect btnRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64));

                if (isSelected)
                {
                    EditorGUI.DrawRect(btnRect, new Color(0.2f, 0.6f, 1f, 0.3f));
                    Handles.color = Color.cyan;
                    Handles.DrawSolidRectangleWithOutline(btnRect, Color.clear, Color.cyan);
                }

                if (prop.icon != null)
                    DrawSpritePreview(btnRect, prop.icon);
                else
                {
                    // Prefab preview fallback
                    var preview = prop.prefab != null ? AssetPreview.GetAssetPreview(prop.prefab) : null;
                    if (preview != null)
                        GUI.DrawTexture(btnRect, preview, ScaleMode.ScaleToFit);
                    else
                        EditorGUI.LabelField(btnRect, "?", EditorStyles.centeredGreyMiniLabel);
                }

                if (GUI.Button(btnRect, GUIContent.none, GUIStyle.none))
                {
                    selectedProp = prop;
                    selectedTile = null;
                }

                EditorGUILayout.LabelField(prop.displayName, EditorStyles.miniLabel,
                    GUILayout.Width(64), GUILayout.Height(14));
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

            void DrawBuildingPalette()
        {
            if (cachedBuildings == null || cachedBuildings.Length == 0)
            {
                EditorGUILayout.HelpBox("No buildings found. Create BuildingDefinition first.", MessageType.Info);
                return;
            }

            int columns = Mathf.Max(1, (int)(position.width / 76));
            int col = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (var building in cachedBuildings)
            {
                if (building == null) continue;
                if (!string.IsNullOrEmpty(searchFilter)
                    && !building.displayName.ToLower().Contains(searchFilter.ToLower()))
                    continue;

                bool isSelected = selectedBuilding == building;
                EditorGUILayout.BeginVertical(GUILayout.Width(68));
                Rect btnRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64));

                if (isSelected)
                {
                    EditorGUI.DrawRect(btnRect, new Color(0.2f, 0.6f, 1f, 0.3f));
                    Handles.color = Color.cyan;
                    Handles.DrawSolidRectangleWithOutline(btnRect, Color.clear, Color.cyan);
                }

                if (building.icon != null)
                    DrawSpritePreview(btnRect, building.icon);
                else
                {
                    var preview = building.prefab != null ? AssetPreview.GetAssetPreview(building.prefab) : null;
                    if (preview != null)
                        GUI.DrawTexture(btnRect, preview, ScaleMode.ScaleToFit);
                    else
                        EditorGUI.LabelField(btnRect, "?", EditorStyles.centeredGreyMiniLabel);
                }

                if (GUI.Button(btnRect, GUIContent.none, GUIStyle.none))
                {
                    selectedBuilding = building;
                    selectedTile = null;
                    selectedProp = null;
                }

                EditorGUILayout.LabelField(building.displayName, EditorStyles.miniLabel,
                    GUILayout.Width(64), GUILayout.Height(14));
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

        // ─── Helpers ────────────────────────────────────────────────────

        void DrawSeparator()
        {
            Rect rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
        }

        static void DrawSpritePreview(Rect rect, Sprite sprite)
        {
            Texture2D tex = sprite.texture;
            Rect texRect = sprite.textureRect;
            Rect uv = new Rect(texRect.x / tex.width, texRect.y / tex.height,
                texRect.width / tex.width, texRect.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv);
        }

        void SaveNewPreset()
        {
            string folder = "Assets/IsometricMapEditor/Presets/Buildings";
            EnsureFolderExists(folder);
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Building Preset", activeData.buildingName + "_Preset", "asset",
                "Save as building preset", folder);
            if (!string.IsNullOrEmpty(path))
            {
                var preset = ScriptableObject.CreateInstance<BuildingPreset>();
                preset.CaptureFrom(activeData);
                preset.presetName = System.IO.Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(preset, path);
                AssetDatabase.SaveAssets();
                RefreshAssets();
            }
        }

        void CreateNewWorkshopData()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Workshop Data", "NewBuilding", "asset", "Save workshop data");
            if (string.IsNullOrEmpty(path)) return;

            var data = CreateInstance<BuildingWorkshopData>();
            data.buildingName = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
            activeData = data;
            workshopEnabled = true;
            SaveState();
        }

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

        // ─── Scene GUI ──────────────────────────────────────────────────

        void OnSceneGUI(SceneView sceneView)
        {
            if (activeData == null || !workshopEnabled) return;

            GridSettings gs = activeData.GetGridSettings();
            DrawWorkshopGrid(gs);

            Event e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            // R key for wall edge rotation
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R
                && !e.alt && !e.control && !e.shift)
            {
                brushRotation = (brushRotation + 1) % 4;
                e.Use();
                sceneView.Repaint();
                Repaint();
            }

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                && e.button == 0 && !e.alt)
            {
                HandleClick(e, false);
            }
            else if (e.type == EventType.MouseDown && e.button == 1 && !e.alt)
            {
                HandleClick(e, true);
            }

            DrawCursor(e, gs);
        }

        void HandleClick(Event e, bool rightClick)
        {
            GridSettings gs = activeData.GetGridSettings();
            Vector3 mouseWorld = GetMouseWorldOnXZ(e);
            Vector2Int cell = IsometricGrid.WorldToGrid(mouseWorld, gs);

            if (!activeData.IsInBounds(cell)) return;

            Undo.RecordObject(activeData, "Workshop Edit");

            if (currentTool == WorkshopTool.Del || rightClick)
            {
                if (currentTool == WorkshopTool.Roof)
                    activeData.RemoveRoofAt(cell);
                else if (currentTool == WorkshopTool.Wall)
                    activeData.RemoveWallAt(cell, brushRotation);
                else if (currentTool == WorkshopTool.Build)
                    activeData.RemoveBuildingAt(cell);
                else if (currentTool == WorkshopTool.Prop)
                    activeData.RemovePropAt(cell);
                else
                {
                    activeData.RemoveTileAt(cell);
                    activeData.RemoveRoofAt(cell);
                }
            }
            else
            {
                switch (currentTool)
                {
                    case WorkshopTool.Paint:
                        if (selectedTile != null)
                        {
                            var ft = new PlacedTile
                            {
                                gridPosition = cell,
                                tileDefinitionId = selectedTile.tileId,
                                tileDefinition = selectedTile,
                                materialOverride = brushMaterial
                            };
                            activeData.PlaceFloorTile(ft);
                        }
                        break;

                    case WorkshopTool.Wall:
                        if (selectedTile != null && selectedTile.IsWall)
                        {
                            var wt = new PlacedTile
                            {
                                gridPosition = cell,
                                tileDefinitionId = selectedTile.tileId,
                                tileDefinition = selectedTile,
                                rotation = brushRotation,
                                materialOverride = brushMaterial
                            };
                            activeData.PlaceWallTile(wt);
                        }
                        break;

                    case WorkshopTool.Roof:
                        if (selectedTile != null)
                        {
                            var rt = new PlacedTile
                            {
                                gridPosition = cell,
                                tileDefinitionId = selectedTile.tileId,
                                tileDefinition = selectedTile,
                                materialOverride = brushMaterial
                            };
                            activeData.PlaceRoofTile(rt);
                        }
                        break;

                    case WorkshopTool.Build:
                        if (selectedBuilding != null && selectedBuilding.prefab != null)
                        {
                            var pb = new PlacedBuilding
                            {
                                instanceId = System.Guid.NewGuid().ToString("N")[..8],
                                gridPosition = cell,
                                buildingDefinitionId = selectedBuilding.buildingId,
                                buildingDefinition = selectedBuilding,
                                scale = 1f
                            };
                            activeData.PlaceBuilding(pb);
                        }
                        break;

                    case WorkshopTool.Prop:
                        if (selectedProp != null)
                        {
                            var pp = new PlacedProp
                            {
                                instanceId = System.Guid.NewGuid().ToString("N")[..8],
                                gridPosition = cell,
                                propDefinitionId = selectedProp.propId,
                                propDefinition = selectedProp,
                                freePlace = false,
                                scale = 1f
                            };
                            activeData.props.Add(pp);
                        }
                        break;
                }
            }

            EditorUtility.SetDirty(activeData);
            WorkshopPreviewManager.InvalidateTracking();
            e.Use();
            SceneView.RepaintAll();
            Repaint();
        }

        void DrawWorkshopGrid(GridSettings gs)
        {
            Handles.color = new Color(0.4f, 0.7f, 1f, 0.3f);
            for (int x = 0; x <= gs.mapWidth; x++)
            {
                Vector3 start = new Vector3(x * gs.tileSize, 0, 0);
                Vector3 end = new Vector3(x * gs.tileSize, 0, gs.mapHeight * gs.tileSize);
                Handles.DrawLine(start, end);
            }
            for (int y = 0; y <= gs.mapHeight; y++)
            {
                Vector3 start = new Vector3(0, 0, y * gs.tileSize);
                Vector3 end = new Vector3(gs.mapWidth * gs.tileSize, 0, y * gs.tileSize);
                Handles.DrawLine(start, end);
            }

            Handles.Label(new Vector3(-0.5f, 0, gs.mapHeight * gs.tileSize + 0.3f),
                "Workshop: " + (activeData != null ? activeData.buildingName : ""),
                EditorStyles.whiteBoldLabel);
        }

        void DrawCursor(Event e, GridSettings gs)
        {
            Vector3 mouseWorld = GetMouseWorldOnXZ(e);
            Vector2Int cell = IsometricGrid.WorldToGrid(mouseWorld, gs);

            if (!activeData.IsInBounds(cell)) return;

            Color color = currentTool switch
            {
                WorkshopTool.Del => new Color(1, 0, 0, 0.4f),
                WorkshopTool.Roof => new Color(0.8f, 0.5f, 1f, 0.4f),
                WorkshopTool.Wall => new Color(1f, 0.7f, 0.2f, 0.4f),
                _ => new Color(0, 1, 0, 0.4f)
            };

            Vector3[] corners = IsometricGrid.GetCellWorldCorners(cell, gs);
            Handles.color = color;
            Handles.DrawAAConvexPolygon(corners);

            // Wall edge indicator
            if (currentTool == WorkshopTool.Wall)
            {
                float halfTile = gs.tileSize * 0.5f;
                Vector3 center = IsometricGrid.GridToWorld(cell, gs);
                Vector3 edgeOff = brushRotation switch
                {
                    0 => new Vector3(0, 0, halfTile),
                    1 => new Vector3(halfTile, 0, 0),
                    2 => new Vector3(0, 0, -halfTile),
                    _ => new Vector3(-halfTile, 0, 0)
                };
                Vector3 lineDir = (brushRotation == 0 || brushRotation == 2)
                    ? Vector3.right : Vector3.forward;

                Handles.color = new Color(1, 0.4f, 0, 0.9f);
                Vector3 edgeCenter = center + edgeOff;
                Vector3 p1 = edgeCenter - lineDir * halfTile;
                Vector3 p2 = edgeCenter + lineDir * halfTile;
                float thickness = 0.08f;
                Handles.DrawAAConvexPolygon(
                    p1 + Vector3.up * thickness, p2 + Vector3.up * thickness,
                    p2 - Vector3.up * thickness, p1 - Vector3.up * thickness);

                string[] edgeNames = { "N", "E", "S", "W" };
                Handles.Label(edgeCenter + Vector3.up * 0.3f,
                    "Wall: " + edgeNames[brushRotation], EditorStyles.whiteBoldLabel);
            }

            Handles.color = Color.white;
            SceneView.RepaintAll();
        }

        static Vector3 GetMouseWorldOnXZ(Event e)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane xzPlane = new Plane(Vector3.up, Vector3.zero);
            if (xzPlane.Raycast(ray, out float dist))
                return ray.GetPoint(dist);
            return Vector3.zero;
        }
    }
}
