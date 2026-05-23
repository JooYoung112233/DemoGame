using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace IsometricMapEditor.Editor
{
    /// <summary>
    /// Building Workshop: a separate editor window for constructing individual buildings.
    /// Like the map editor but focused on one building. Features:
    /// - Floor/Wall/Roof layers
    /// - Roof ON/OFF toggle
    /// - Bake to prefab or place into map
    /// - State survives OFF/recompile via EditorStateCache
    /// </summary>
    public class BuildingWorkshopWindow : EditorWindow
    {
        private static BuildingWorkshopData activeData;
        private static bool workshopEnabled;
        private static WorkshopTool currentTool = WorkshopTool.Floor;
        private static TileDefinition selectedTile;
        private static PropDefinition selectedProp;
        private static int brushRotation;
        private static Material brushMaterial;

        private Vector2 paletteScroll;
        private Vector2 inspectorScroll;
        private string searchFilter = "";
        private bool showInspector = true;
        private bool stateRestored;

        private TileDefinition[] cachedTiles;
        private PropDefinition[] cachedProps;
        private BuildingPreset[] cachedPresets;

        public enum WorkshopTool { Floor, Wall, Roof, Prop, Erase }

        public static BuildingWorkshopData ActiveData
        {
            get { return activeData; }
        }

        public static bool WorkshopEnabled
        {
            get { return workshopEnabled; }
        }

        [MenuItem("Tools/Building Workshop")]
        public static void ShowWindow()
        {
            BuildingWorkshopWindow window = GetWindow<BuildingWorkshopWindow>("Building Workshop");
            window.minSize = new Vector2(340, 500);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            RefreshAssets();
            RestoreState();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SaveState();
        }

        private void RestoreState()
        {
            if (!stateRestored)
            {
                BuildingWorkshopData cached = EditorStateCache.LoadCachedWorkshopData();
                if (cached != null)
                {
                    activeData = cached;
                    workshopEnabled = EditorStateCache.LoadCachedWorkshopEnabled();
                }
                stateRestored = true;
            }
        }

        private void SaveState()
        {
            EditorStateCache.SaveWorkshopState(activeData, workshopEnabled);
        }

        private void RefreshAssets()
        {
            cachedTiles = LoadAll<TileDefinition>("t:TileDefinition");
            cachedProps = LoadAll<PropDefinition>("t:PropDefinition");
            cachedPresets = LoadAll<BuildingPreset>("t:BuildingPreset");
        }

        private static T[] LoadAll<T>(string filter) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets(filter);
            List<T> list = new List<T>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) list.Add(asset);
            }
            return list.ToArray();
        }

        // ─── GUI ────────────────────────────────────────────────────────

        private void OnGUI()
        {
            DrawHeader();

            if (activeData == null)
            {
                EditorGUILayout.HelpBox("Select or create a BuildingWorkshopData asset.", MessageType.Info);
                if (GUILayout.Button("Create New Workshop Data"))
                    CreateNewWorkshopData();
                return;
            }

            if (!workshopEnabled)
            {
                EditorGUILayout.HelpBox("Workshop is OFF. Toggle ON to start building.", MessageType.Info);
                return;
            }

            DrawToolbar();
            DrawPresetBar();
            DrawBuildingSettings();
            DrawRoofToggle();

            EditorGUILayout.Space(4);
            DrawPalette();

            DrawSeparator();
            DrawShortcutHelp();
        }

        private void DrawHeader()
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

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Toggle(currentTool == WorkshopTool.Floor, "Floor", EditorStyles.toolbarButton))
                currentTool = WorkshopTool.Floor;
            if (GUILayout.Toggle(currentTool == WorkshopTool.Wall, "Wall", EditorStyles.toolbarButton))
                currentTool = WorkshopTool.Wall;
            if (GUILayout.Toggle(currentTool == WorkshopTool.Roof, "Roof", EditorStyles.toolbarButton))
                currentTool = WorkshopTool.Roof;
            if (GUILayout.Toggle(currentTool == WorkshopTool.Prop, "Prop", EditorStyles.toolbarButton))
                currentTool = WorkshopTool.Prop;
            if (GUILayout.Toggle(currentTool == WorkshopTool.Erase, "Erase", EditorStyles.toolbarButton))
                currentTool = WorkshopTool.Erase;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Bake", EditorStyles.toolbarButton))
                WorkshopBaker.BakeToScene(activeData);
            if (GUILayout.Button("Prefab", EditorStyles.toolbarButton))
                WorkshopBaker.SaveAsPrefab(activeData);

            EditorGUILayout.EndHorizontal();

            // Wall edge selector
            if (currentTool == WorkshopTool.Wall)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                EditorGUILayout.LabelField("Edge:", GUILayout.Width(34));
                string[] edgeLabels = new string[] { "N", "E", "S", "W" };
                brushRotation = GUILayout.Toolbar(brushRotation, edgeLabels,
                    EditorStyles.toolbarButton, GUILayout.Width(120));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("R = rotate edge", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            // Material override row
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Mat:", GUILayout.Width(30));
            brushMaterial = (Material)EditorGUILayout.ObjectField(
                brushMaterial, typeof(Material), false, GUILayout.Width(160));
            if (brushMaterial != null)
            {
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(42)))
                    brushMaterial = null;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPresetBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Preset:", GUILayout.Width(46));

            if (cachedPresets != null && cachedPresets.Length > 0)
            {
                string[] names = new string[cachedPresets.Length + 1];
                names[0] = "-- Load --";
                for (int i = 0; i < cachedPresets.Length; i++)
                {
                    names[i + 1] = cachedPresets[i].presetName;
                    if (string.IsNullOrEmpty(names[i + 1]))
                        names[i + 1] = cachedPresets[i].name;
                }

                int sel = EditorGUILayout.Popup(0, names, EditorStyles.toolbarPopup, GUILayout.Width(120));
                if (sel > 0)
                {
                    BuildingPreset preset = cachedPresets[sel - 1];
                    if (EditorUtility.DisplayDialog("Load Building Preset",
                        "\"" + preset.presetName + "\" 프리셋을 현재 워크숍에 덮어쓰시겠습니까?",
                        "덮어쓰기", "취소"))
                    {
                        Undo.RecordObject(activeData, "Load Building Preset");
                        preset.ApplyTo(activeData);
                        EditorUtility.SetDirty(activeData);
                        WorkshopPreviewManager.InvalidateTracking();
                        SceneView.RepaintAll();
                        Debug.Log("[Workshop] Preset \"" + preset.presetName + "\" loaded.");
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("(없음)", EditorStyles.miniLabel, GUILayout.Width(120));
            }

            if (GUILayout.Button("Save New", EditorStyles.toolbarButton, GUILayout.Width(68)))
            {
                string folder = "Assets/IsometricMapEditor/Presets/Buildings";
                EnsureFolderExists(folder);
                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Building Preset", activeData.buildingName + "_Preset", "asset",
                    "Save as building preset", folder);
                if (!string.IsNullOrEmpty(path))
                {
                    BuildingPreset preset = ScriptableObject.CreateInstance<BuildingPreset>();
                    preset.CaptureFrom(activeData);
                    preset.presetName = System.IO.Path.GetFileNameWithoutExtension(path);
                    AssetDatabase.CreateAsset(preset, path);
                    AssetDatabase.SaveAssets();
                    RefreshAssets();
                    Debug.Log("[Workshop] Preset saved: " + path);
                }
            }

            if (cachedPresets != null && cachedPresets.Length > 0)
            {
                if (GUILayout.Button("Overwrite", EditorStyles.toolbarButton, GUILayout.Width(68)))
                {
                    GenericMenu menu = new GenericMenu();
                    for (int i = 0; i < cachedPresets.Length; i++)
                    {
                        BuildingPreset p = cachedPresets[i];
                        string label = p.presetName;
                        if (string.IsNullOrEmpty(label)) label = p.name;
                        menu.AddItem(new GUIContent(label), false, () =>
                        {
                            if (EditorUtility.DisplayDialog("Overwrite Preset",
                                "\"" + p.presetName + "\" 프리셋에 현재 데이터를 덮어쓰시겠습니까?",
                                "덮어쓰기", "취소"))
                            {
                                Undo.RecordObject(p, "Overwrite Building Preset");
                                p.CaptureFrom(activeData);
                                EditorUtility.SetDirty(p);
                                AssetDatabase.SaveAssets();
                                Debug.Log("[Workshop] Preset \"" + p.presetName + "\" overwritten.");
                            }
                        });
                    }
                    menu.ShowAsContext();
                }
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("↻", EditorStyles.toolbarButton, GUILayout.Width(22)))
                RefreshAssets();

            EditorGUILayout.EndHorizontal();
        }

        private static void EnsureFolderExists(string folderPath)
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

        private void DrawBuildingSettings()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", GUILayout.Width(42));
            activeData.buildingName = EditorGUILayout.TextField(activeData.buildingName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("W", GUILayout.Width(16));
            activeData.gridWidth = EditorGUILayout.IntField(activeData.gridWidth, GUILayout.Width(40));
            EditorGUILayout.LabelField("H", GUILayout.Width(14));
            activeData.gridHeight = EditorGUILayout.IntField(activeData.gridHeight, GUILayout.Width(40));
            EditorGUILayout.LabelField("TileSize", GUILayout.Width(52));
            activeData.tileSize = EditorGUILayout.FloatField(activeData.tileSize, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            // External appearance (what gets placed on the map)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Prefab:", GUILayout.Width(42));
            activeData.externalPrefab = (GameObject)EditorGUILayout.ObjectField(
                activeData.externalPrefab, typeof(GameObject), false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Icon:", GUILayout.Width(42));
            activeData.externalIcon = (Sprite)EditorGUILayout.ObjectField(
                activeData.externalIcon, typeof(Sprite), false, GUILayout.Height(48));
            if (activeData.externalIcon != null)
            {
                Rect previewRect = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48));
                Texture2D tex = activeData.externalIcon.texture;
                Rect texRect = activeData.externalIcon.textureRect;
                Rect uv = new(texRect.x / tex.width, texRect.y / tex.height,
                              texRect.width / tex.width, texRect.height / tex.height);
                GUI.DrawTextureWithTexCoords(previewRect, tex, uv);
            }
            EditorGUILayout.EndHorizontal();

            // Output BuildingDefinition link
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Output:", GUILayout.Width(42));
            activeData.outputDefinition = (BuildingDefinition)EditorGUILayout.ObjectField(
                activeData.outputDefinition, typeof(BuildingDefinition), false);
            EditorGUILayout.EndHorizontal();

            if (activeData.outputDefinition == null)
            {
                if (GUILayout.Button("Generate BuildingDefinition"))
                    GenerateBuildingDefinition();
            }
            else
            {
                if (GUILayout.Button("Sync to BuildingDefinition"))
                    SyncToBuildingDefinition();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(activeData);
                SceneView.RepaintAll();
            }
        }

        void GenerateBuildingDefinition()
        {
            string folder = "Assets/IsometricMapEditor/MapData/Sprites/AutoGen/Buildings";
            EnsureFolderExists(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{activeData.buildingName}.asset");

            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.buildingId = activeData.buildingName.ToLower().Replace(" ", "_");
            def.displayName = activeData.buildingName;
            def.icon = activeData.externalIcon;
            def.prefab = activeData.externalPrefab;
            def.footprint = activeData.GetFootprint();

            AssetDatabase.CreateAsset(def, path);
            AssetDatabase.SaveAssets();

            activeData.outputDefinition = def;
            EditorUtility.SetDirty(activeData);
            Debug.Log($"[Workshop] BuildingDefinition created: {path}");
        }

        void SyncToBuildingDefinition()
        {
            var def = activeData.outputDefinition;
            Undo.RecordObject(def, "Sync Workshop to BuildingDefinition");
            def.displayName = activeData.buildingName;
            def.icon = activeData.externalIcon;
            def.prefab = activeData.externalPrefab;
            def.footprint = activeData.GetFootprint();
            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Workshop] BuildingDefinition '{def.displayName}' synced.");
        }

        private void DrawRoofToggle()
        {
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("Roof:", GUILayout.Width(36));

            EditorGUI.BeginChangeCheck();
            activeData.roofVisible = GUILayout.Toggle(activeData.roofVisible,
                activeData.roofVisible ? "Visible" : "Hidden",
                EditorStyles.miniButton, GUILayout.Width(60));
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(activeData);
                WorkshopPreviewManager.InvalidateTracking();
                SceneView.RepaintAll();
            }

            EditorGUILayout.LabelField(
                "Floor:" + activeData.floorTiles.Count +
                " Wall:" + activeData.wallTiles.Count +
                " Roof:" + activeData.roofTiles.Count +
                " Prop:" + activeData.props.Count,
                EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPalette()
        {
            searchFilter = EditorGUILayout.TextField("Search", searchFilter);

            paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll);

            if (currentTool == WorkshopTool.Prop)
                DrawPropPalette();
            else if (currentTool != WorkshopTool.Erase)
                DrawTilePalette();
            else
                EditorGUILayout.HelpBox("LClick = erase floor/wall, RClick = erase prop\nRoof erased when Roof tool + Erase", MessageType.Info);

            EditorGUILayout.EndScrollView();
        }

        private void DrawTilePalette()
        {
            if (cachedTiles == null || cachedTiles.Length == 0)
            {
                EditorGUILayout.HelpBox("No tiles found. Use Sync in Map Editor.", MessageType.Info);
                return;
            }

            int columns = Mathf.Max(1, (int)(position.width / 76));
            int col = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (TileDefinition tile in cachedTiles)
            {
                if (tile == null) continue;
                if (!string.IsNullOrEmpty(searchFilter)
                    && !tile.tileId.ToLower().Contains(searchFilter.ToLower()))
                    continue;

                // Wall tool only shows wall tiles, Floor/Roof show non-wall tiles
                if (currentTool == WorkshopTool.Wall && !tile.IsWall) continue;
                if ((currentTool == WorkshopTool.Floor || currentTool == WorkshopTool.Roof) && tile.IsWall) continue;

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

        private void DrawPropPalette()
        {
            if (cachedProps == null || cachedProps.Length == 0)
            {
                EditorGUILayout.HelpBox("No props found.", MessageType.Info);
                return;
            }

            foreach (PropDefinition prop in cachedProps)
            {
                if (prop == null) continue;
                if (!string.IsNullOrEmpty(searchFilter)
                    && !prop.displayName.ToLower().Contains(searchFilter.ToLower()))
                    continue;

                bool isSelected = selectedProp == prop;
                EditorGUILayout.BeginHorizontal(isSelected ? "selectionRect" : "box");

                if (prop.icon != null)
                {
                    Rect rect = GUILayoutUtility.GetRect(36, 36, GUILayout.Width(36));
                    DrawSpritePreview(rect, prop.icon);
                }

                EditorGUILayout.LabelField(prop.displayName, EditorStyles.boldLabel);

                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    selectedProp = prop;
                    selectedTile = null;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private bool showHelp = true;

        private void DrawShortcutHelp()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.foldout);
            headerStyle.richText = true;
            showHelp = EditorGUILayout.Foldout(showHelp, "<b>도구 & 단축키 가이드</b>", true, headerStyle);
            if (!showHelp) return;

            GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
            style.richText = true;
            style.wordWrap = true;
            style.padding = new RectOffset(8, 8, 1, 1);

            Color bg = new Color(0.18f, 0.18f, 0.22f, 1f);
            Rect rect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(rect, bg);

            EditorGUILayout.LabelField("<color=#FFD700><b>── 도구 ──</b></color>", style);

            EditorGUILayout.LabelField(
                "<color=#7fef7f><b>Floor</b></color>  " +
                "바닥 타일 배치. 건물의 기본 바닥면 구성.\n" +
                "  <color=#aaa>LClick/LDrag</color> 배치  " +
                "<color=#aaa>RClick</color> 해당 셀 삭제",
                style, GUILayout.Height(30));

            EditorGUILayout.LabelField(
                "<color=#ffaf5f><b>Wall</b></color>  " +
                "벽 타일 배치. 셀의 4면(N/E/S/W)에 개별 설치.\n" +
                "  <color=#aaa>R</color> 방향 순환(N→E→S→W)  " +
                "팔레트에 Wall 카테고리 타일만 표시됨.",
                style, GUILayout.Height(30));

            EditorGUILayout.LabelField(
                "<color=#bf7fff><b>Roof</b></color>  " +
                "지붕 타일 배치. 별도 Roof 레이어에 저장됨.\n" +
                "  Bake 시 'Roof' 부모 오브젝트에 RoofController 컴포넌트 자동 부착.\n" +
                "  상단 Roof: Visible/Hidden 토글로 지붕 보이기/숨기기.",
                style, GUILayout.Height(36));

            EditorGUILayout.LabelField(
                "<color=#7fefbf><b>Prop</b></color>  " +
                "소품 배치. 건물 내부 가구, 장식 등.\n" +
                "  <color=#aaa>LClick</color> 배치  " +
                "<color=#aaa>RClick</color> 삭제",
                style, GUILayout.Height(30));

            EditorGUILayout.LabelField(
                "<color=#ff7f7f><b>Erase</b></color>  " +
                "현재 도구 컨텍스트에 따라 삭제.\n" +
                "  Floor 모드→바닥/벽 삭제, Roof→지붕 삭제, Prop→소품 삭제.",
                style, GUILayout.Height(24));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("<color=#FFD700><b>── 액션 버튼 ──</b></color>", style);

            EditorGUILayout.LabelField(
                "<color=#5fdf5f><b>Bake</b></color>  " +
                "건물을 씬에 영구 오브젝트로 생성. Floor/Walls/Roof/Props 구조.\n" +
                "<color=#5fafff><b>Prefab</b></color>  " +
                "Bake 후 .prefab 파일로 저장. 맵에 반복 배치 가능.\n" +
                "<color=#ef8fef><b>Preset</b></color>  " +
                "건물 디자인을 프리셋으로 저장/불러오기. 건물 템플릿 재사용.",
                style, GUILayout.Height(42));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("<color=#FFD700><b>── 일반 ──</b></color>", style);

            EditorGUILayout.LabelField(
                "<color=#ccc><b>ON/OFF</b></color>  " +
                "워크숍 토글. OFF 해도 작업 내용 유지(캐시).\n" +
                "<color=#ccc><b>Mat</b></color>  " +
                "배치 시 머티리얼 오버라이드. Clear로 초기화.\n" +
                "<color=#ccc><b>Roof: Visible/Hidden</b></color>  " +
                "지붕 레이어 표시/숨기기. 건물 내부 작업 시 Hidden으로.",
                style, GUILayout.Height(42));

            EditorGUILayout.EndVertical();
        }

        private void DrawSeparator()
        {
            Rect rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
        }

        private static void DrawSpritePreview(Rect rect, Sprite sprite)
        {
            Texture2D tex = sprite.texture;
            Rect texRect = sprite.textureRect;
            Rect uv = new Rect(texRect.x / tex.width, texRect.y / tex.height,
                texRect.width / tex.width, texRect.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv);
        }

        // ─── Scene GUI ──────────────────────────────────────────────────

        private void OnSceneGUI(SceneView sceneView)
        {
            if (activeData == null || !workshopEnabled) return;

            GridSettings gs = activeData.GetGridSettings();

            // Draw workshop grid (offset so it doesn't overlap with map grid)
            DrawWorkshopGrid(gs);

            Event e = Event.current;

            if (currentTool != WorkshopTool.Erase || true)
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
                HandleWorkshopClick(e, sceneView, false);
            }
            else if (e.type == EventType.MouseDown && e.button == 1 && !e.alt)
            {
                HandleWorkshopClick(e, sceneView, true);
            }

            DrawWorkshopCursor(e, gs);
        }

        private void HandleWorkshopClick(Event e, SceneView sceneView, bool rightClick)
        {
            GridSettings gs = activeData.GetGridSettings();
            Vector3 mouseWorld = GetMouseWorldOnXZ(e);
            Vector2Int cell = IsometricGrid.WorldToGrid(mouseWorld, gs);

            if (!activeData.IsInBounds(cell)) return;

            Undo.RecordObject(activeData, "Workshop Edit");

            if (currentTool == WorkshopTool.Erase || rightClick)
            {
                // Erase based on current tool context
                if (currentTool == WorkshopTool.Roof)
                    activeData.RemoveRoofAt(cell);
                else if (currentTool == WorkshopTool.Wall)
                    activeData.RemoveWallAt(cell, brushRotation);
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
                    case WorkshopTool.Floor:
                        if (selectedTile != null)
                        {
                            PlacedTile ft = new PlacedTile();
                            ft.gridPosition = cell;
                            ft.tileDefinitionId = selectedTile.tileId;
                            ft.tileDefinition = selectedTile;
                            ft.materialOverride = brushMaterial;
                            activeData.PlaceFloorTile(ft);
                        }
                        break;

                    case WorkshopTool.Wall:
                        if (selectedTile != null && selectedTile.IsWall)
                        {
                            PlacedTile wt = new PlacedTile();
                            wt.gridPosition = cell;
                            wt.tileDefinitionId = selectedTile.tileId;
                            wt.tileDefinition = selectedTile;
                            wt.rotation = brushRotation;
                            wt.materialOverride = brushMaterial;
                            activeData.PlaceWallTile(wt);
                        }
                        break;

                    case WorkshopTool.Roof:
                        if (selectedTile != null)
                        {
                            PlacedTile rt = new PlacedTile();
                            rt.gridPosition = cell;
                            rt.tileDefinitionId = selectedTile.tileId;
                            rt.tileDefinition = selectedTile;
                            rt.materialOverride = brushMaterial;
                            activeData.PlaceRoofTile(rt);
                        }
                        break;

                    case WorkshopTool.Prop:
                        if (selectedProp != null)
                        {
                            PlacedProp pp = new PlacedProp();
                            pp.instanceId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
                            pp.gridPosition = cell;
                            pp.propDefinitionId = selectedProp.propId;
                            pp.propDefinition = selectedProp;
                            pp.freePlace = false;
                            pp.scale = 1f;
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

        private void DrawWorkshopGrid(GridSettings gs)
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

            // Label
            Handles.Label(new Vector3(-0.5f, 0, gs.mapHeight * gs.tileSize + 0.3f),
                "Workshop: " + (activeData != null ? activeData.buildingName : ""),
                EditorStyles.whiteBoldLabel);
        }

        private void DrawWorkshopCursor(Event e, GridSettings gs)
        {
            Vector3 mouseWorld = GetMouseWorldOnXZ(e);
            Vector2Int cell = IsometricGrid.WorldToGrid(mouseWorld, gs);

            if (!activeData.IsInBounds(cell)) return;

            Color color;
            if (currentTool == WorkshopTool.Erase)
                color = new Color(1, 0, 0, 0.4f);
            else if (currentTool == WorkshopTool.Roof)
                color = new Color(0.8f, 0.5f, 1f, 0.4f);
            else if (currentTool == WorkshopTool.Wall)
                color = new Color(1f, 0.7f, 0.2f, 0.4f);
            else
                color = new Color(0, 1, 0, 0.4f);

            Vector3[] corners = IsometricGrid.GetCellWorldCorners(cell, gs);
            Handles.color = color;
            Handles.DrawAAConvexPolygon(corners);

            // Wall edge indicator
            if (currentTool == WorkshopTool.Wall)
            {
                float halfTile = gs.tileSize * 0.5f;
                Vector3 center = IsometricGrid.GridToWorld(cell, gs);
                Vector3 edgeOff;
                Vector3 lineDir;
                string[] edgeNames = new string[] { "N", "E", "S", "W" };

                switch (brushRotation)
                {
                    case 0: edgeOff = new Vector3(0, 0, halfTile); break;
                    case 1: edgeOff = new Vector3(halfTile, 0, 0); break;
                    case 2: edgeOff = new Vector3(0, 0, -halfTile); break;
                    default: edgeOff = new Vector3(-halfTile, 0, 0); break;
                }

                if (brushRotation == 0 || brushRotation == 2)
                    lineDir = Vector3.right;
                else
                    lineDir = Vector3.forward;

                Handles.color = new Color(1, 0.4f, 0, 0.9f);
                Vector3 edgeCenter = center + edgeOff;
                Vector3 p1 = edgeCenter - lineDir * halfTile;
                Vector3 p2 = edgeCenter + lineDir * halfTile;
                float thickness = 0.08f;
                Handles.DrawAAConvexPolygon(
                    p1 + Vector3.up * thickness, p2 + Vector3.up * thickness,
                    p2 - Vector3.up * thickness, p1 - Vector3.up * thickness);

                Handles.Label(edgeCenter + Vector3.up * 0.3f,
                    "Wall: " + edgeNames[brushRotation], EditorStyles.whiteBoldLabel);
            }

            Handles.color = Color.white;
            SceneView.RepaintAll();
        }

        private static Vector3 GetMouseWorldOnXZ(Event e)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane xzPlane = new Plane(Vector3.up, Vector3.zero);
            float dist;
            if (xzPlane.Raycast(ray, out dist))
                return ray.GetPoint(dist);
            return Vector3.zero;
        }

        // ─── Create ─────────────────────────────────────────────────────

        private void CreateNewWorkshopData()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Workshop Data", "NewBuilding", "asset", "Save workshop data");
            if (string.IsNullOrEmpty(path)) return;

            BuildingWorkshopData data = CreateInstance<BuildingWorkshopData>();
            data.buildingName = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
            activeData = data;
            workshopEnabled = true;
            SaveState();
        }
    }
}
