using System;
using UnityEngine;
using UnityEngine.UI;

namespace IsometricMapEditor
{
    public class MapBuilderUI : MonoBehaviour
    {
        MapBuilderManager _manager;
        Canvas _canvas;
        RectTransform _root;

        // Toolbar
        Button[] _toolButtons;
        Text _rotationText;

        // Palette
        RectTransform _palettePanel;
        RectTransform _paletteContent;
        ScrollRect _paletteScroll;

        // Snap toggle
        Text _snapText;
        Button _snapButton;

        // Brush size (Tile tool)
        Text _brushWText;
        Text _brushHText;
        RectTransform _brushGroup;

        // 정렬 조절 그룹
        RectTransform _sortGroup;
        Text _sortLabel;
        Text _sortValue;

        // Hover tooltip (eraser preview)
        RectTransform _hoverTooltip;
        Text _hoverTooltipText;

        // Filter toggles
        RectTransform _filterPanel;
        Button[] _filterButtons;

        // Spawn Config
        RectTransform _spawnConfigPanel;

        // Building visibility list (Feature B)
        RectTransform _buildingListPanel;
        RectTransform _buildingListContent;
        Button _interiorViewBtn;
        Button _wallHideBtn;

        // Help panel
        RectTransform _helpPanel;

        // Dialog
        RectTransform _dialogPanel;
        InputField _dialogInput;
        Text _dialogTitle;
        Action<string> _dialogCallback;
        RectTransform _fileListContent;

        // New Map Dialog
        RectTransform _newMapPanel;
        InputField _widthInput;
        InputField _heightInput;
        InputField _tileSizeInput;

        static readonly Color BG = new(0.12f, 0.12f, 0.15f, 0.95f);
        static readonly Color BTN_NORMAL = new(0.25f, 0.25f, 0.3f, 1f);
        static readonly Color BTN_ACTIVE = new(0.3f, 0.6f, 1f, 1f);
        static readonly Color BTN_HOVER = new(0.35f, 0.35f, 0.4f, 1f);
        static readonly Color PANEL_BG = new(0.15f, 0.15f, 0.18f, 0.95f);

        static Font _cachedFont;
        static bool _fontResolved;
        static Font DefaultFont
        {
            get
            {
                if (_fontResolved) return _cachedFont;
                _fontResolved = true;
                _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_cachedFont == null)
                    _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (_cachedFont == null)
                    _cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
                return _cachedFont;
            }
        }

        public void Initialize(MapBuilderManager manager)
        {
            _manager = manager;
            BuildUI();
        }

        void BuildUI()
        {
            var canvasGo = new GameObject("MapBuilderCanvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            _root = canvasGo.GetComponent<RectTransform>();

            BuildTopToolbar();
            BuildPalettePanel();
            BuildFilterPanel();
            BuildSpawnConfigPanel();
            BuildBuildingListPanel();
            BuildHoverTooltip();
            BuildSaveLoadDialog();
            BuildNewMapDialog();
            BuildHelpPanel();
        }

        // ======== TOP TOOLBAR ========

        void BuildTopToolbar()
        {
            var bar = CreatePanel(_root, "Toolbar", BG);
            SetAnchors(bar, new Vector2(0, 1), new Vector2(1, 1));
            bar.offsetMin = new Vector2(0, -48);
            bar.offsetMax = Vector2.zero;

            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 4, 4);
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            string[] toolNames = { "1.Tile", "2.Wall", "3.Prop", "4.Building", "5.Object", "6.Del", "7.Move" };
            ToolMode[] modes = { ToolMode.Tile, ToolMode.Wall, ToolMode.Prop, ToolMode.Building, ToolMode.MapObject, ToolMode.Eraser, ToolMode.Move };
            _toolButtons = new Button[toolNames.Length];

            for (int i = 0; i < toolNames.Length; i++)
            {
                int idx = i;
                var btn = CreateButton(bar, toolNames[i], 80, () => _manager.SetToolMode(modes[idx]));
                _toolButtons[i] = btn;
            }

            CreateSpacer(bar, 16);
            CreateButton(bar, "New", 50, () => ShowNewMapDialog());
            CreateButton(bar, "Save", 50, () => ShowSaveDialog());
            CreateButton(bar, "Load", 50, () => ShowLoadDialog());
            // Undo는 버튼 전용. (플레이모드에서 Ctrl+Z는 Unity 에디터 undo와 충돌해 키 단축키 제거)
            CreateButton(bar, "Undo", 55, () => _manager.Undo());

            CreateSpacer(bar, 16);
            _rotationText = CreateLabel(bar, "Rot: 0°", 65);

            // Snap/Free toggle button
            _snapButton = CreateButton(bar, "Free", 60, () =>
            {
                _manager.ToggleSnapToGrid();
                RefreshStatus();
            });
            _snapText = _snapButton.GetComponentInChildren<Text>();

            // ── 브러시 크기 (타일 모드) ──
            CreateSpacer(bar, 10);
            var brushGo = new GameObject("BrushGroup");
            brushGo.transform.SetParent(bar, false);
            _brushGroup = brushGo.AddComponent<RectTransform>();
            var brushLayout = brushGo.AddComponent<HorizontalLayoutGroup>();
            brushLayout.spacing = 2;
            brushLayout.childForceExpandWidth = false;
            brushLayout.childForceExpandHeight = true;
            brushLayout.childAlignment = TextAnchor.MiddleCenter;
            var brushLe = brushGo.AddComponent<LayoutElement>();
            brushLe.preferredWidth = 178;

            CreateLabel(_brushGroup, "Brush", 42);
            CreateButton(_brushGroup, "-", 22, () => _manager.SetBrushSize(_manager.BrushWidth - 1, _manager.BrushHeight));
            _brushWText = CreateLabel(_brushGroup, "1", 16);
            CreateButton(_brushGroup, "+", 22, () => _manager.SetBrushSize(_manager.BrushWidth + 1, _manager.BrushHeight));
            CreateLabel(_brushGroup, "x", 10);
            CreateButton(_brushGroup, "-", 22, () => _manager.SetBrushSize(_manager.BrushWidth, _manager.BrushHeight - 1));
            _brushHText = CreateLabel(_brushGroup, "1", 16);
            CreateButton(_brushGroup, "+", 22, () => _manager.SetBrushSize(_manager.BrushWidth, _manager.BrushHeight + 1));

            // ── 정렬 조절 (지우개 모드에서 프랍/건물을 가리키면 표시) ──
            CreateSpacer(bar, 10);
            var sortGo = new GameObject("SortGroup");
            sortGo.transform.SetParent(bar, false);
            _sortGroup = sortGo.AddComponent<RectTransform>();
            var sortLayout = sortGo.AddComponent<HorizontalLayoutGroup>();
            sortLayout.spacing = 3;
            sortLayout.childForceExpandWidth = false;
            sortLayout.childForceExpandHeight = true;
            sortLayout.childAlignment = TextAnchor.MiddleCenter;
            var sortLe = sortGo.AddComponent<LayoutElement>();
            sortLe.preferredWidth = 230;

            _sortLabel = CreateLabel(_sortGroup, "정렬", 90);
            _sortLabel.alignment = TextAnchor.MiddleRight;
            CreateButton(_sortGroup, "-", 24, () => _manager.NudgeHoverSortOffset(-1));
            _sortValue = CreateLabel(_sortGroup, "0", 28);
            CreateButton(_sortGroup, "+", 24, () => _manager.NudgeHoverSortOffset(+1));
            CreateButton(_sortGroup, "X", 22, () => _manager.ClearSortTarget());
        }

        // ======== PALETTE PANEL ========

        void BuildPalettePanel()
        {
            _palettePanel = CreatePanel(_root, "Palette", PANEL_BG);
            SetAnchors(_palettePanel, new Vector2(0, 0), new Vector2(0, 1));
            _palettePanel.offsetMin = new Vector2(0, 0);
            _palettePanel.offsetMax = new Vector2(180, -48);

            var titleBar = CreatePanel(_palettePanel, "PaletteTitle", new Color(0.1f, 0.1f, 0.13f, 1f));
            SetAnchors(titleBar, new Vector2(0, 1), new Vector2(1, 1));
            titleBar.offsetMin = new Vector2(0, -30);
            titleBar.offsetMax = Vector2.zero;

            var titleTextGo = new GameObject("TitleText");
            titleTextGo.transform.SetParent(titleBar, false);
            var titleRt = titleTextGo.AddComponent<RectTransform>();
            SetAnchors(titleRt, Vector2.zero, Vector2.one);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            var titleLabel = titleTextGo.AddComponent<Text>();
            titleLabel.text = "  Palette";
            titleLabel.font = DefaultFont;
            titleLabel.fontSize = 14;
            titleLabel.color = Color.white;
            titleLabel.alignment = TextAnchor.MiddleLeft;

            var scrollArea = CreatePanel(_palettePanel, "ScrollArea", Color.clear);
            SetAnchors(scrollArea, Vector2.zero, Vector2.one);
            scrollArea.offsetMin = new Vector2(5, 5);
            scrollArea.offsetMax = new Vector2(-5, -35);

            var scrollGo = scrollArea.gameObject;
            _paletteScroll = scrollGo.AddComponent<ScrollRect>();
            _paletteScroll.horizontal = false;
            _paletteScroll.vertical = true;
            scrollGo.AddComponent<RectMask2D>();

            _paletteContent = new GameObject("Content").AddComponent<RectTransform>();
            _paletteContent.SetParent(scrollArea, false);
            SetAnchors(_paletteContent, new Vector2(0, 1), new Vector2(1, 1));
            _paletteContent.pivot = new Vector2(0.5f, 1f);
            _paletteContent.offsetMin = Vector2.zero;
            _paletteContent.offsetMax = Vector2.zero;

            var vlg = _paletteContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(2, 2, 2, 2);
            vlg.spacing = 3;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = _paletteContent.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _paletteScroll.content = _paletteContent;
        }

        void PopulatePalette()
        {
            foreach (Transform child in _paletteContent)
                Destroy(child.gameObject);

            if (_manager.catalog == null) return;

            switch (_manager.CurrentTool)
            {
                case ToolMode.Tile:
                    AddPaletteHeader("Tiles");
                    foreach (var tile in _manager.catalog.tiles)
                    {
                        if (tile == null) continue;
                        var t = tile;
                        bool active = _manager.SelectedTile == t;
                        AddPaletteItem(tile.tileId, tile.sprite, active, () => _manager.SelectTile(t));
                    }
                    break;

                case ToolMode.Wall:
                    AddPaletteHeader("Walls (Q/E: Rotate)");
                    foreach (var wall in _manager.catalog.walls)
                    {
                        if (wall == null) continue;
                        var w = wall;
                        bool active = _manager.SelectedWall == w;
                        AddPaletteItem(wall.tileId, wall.sprite, active, () => _manager.SelectWall(w));
                    }
                    break;

                case ToolMode.Prop:
                    AddPaletteHeader("Props (Q/E: 회전, F: 반전, B: 벽부착, PgUp/Dn: 높이)");
                    foreach (var prop in _manager.catalog.props)
                    {
                        if (prop == null) continue;
                        var p = prop;
                        bool active = _manager.SelectedProp == p;
                        AddPaletteItem(prop.displayName ?? prop.propId, prop.sprite, active, () => _manager.SelectProp(p));
                    }
                    break;

                case ToolMode.Building:
                    AddPaletteHeader("Buildings (Footprint)");
                    if (_manager.catalog != null)
                    {
                        foreach (var building in _manager.catalog.buildings)
                        {
                            if (building == null) continue;
                            var b = building;
                            bool active = _manager.SelectedBuilding == b;
                            string label = $"{building.displayName ?? building.buildingId} ({building.footprint.x}x{building.footprint.y})";
                            Texture tex = GetPrefabMainTexture(building.prefab);
                            AddPaletteItem(label, tex, active, () => _manager.SelectBuilding(b));
                        }
                    }
                    break;

                case ToolMode.MapObject:
                    // 카탈로그에 등록된 MapObject 정의
                    if (_manager.catalog != null && _manager.catalog.mapObjectDefs != null && _manager.catalog.mapObjectDefs.Length > 0)
                    {
                        AddPaletteHeader("등록된 오브젝트");
                        foreach (var def in _manager.catalog.mapObjectDefs)
                        {
                            if (def == null) continue;
                            var d = def;
                            bool active = _manager.SelectedObjectDef == d;
                            string label = def.displayName ?? def.objectId;
                            if (def.visualTexture != null)
                                AddPaletteItem(label, (Texture)def.visualTexture, active, () => { _manager.SelectObjectDef(d); RefreshSpawnConfig(); });
                            else
                                AddPaletteItem(label, (Sprite)null, active, () => { _manager.SelectObjectDef(d); RefreshSpawnConfig(); });
                        }
                    }

                    AddPaletteHeader("기본 타입");
                    foreach (MapObjectType type in Enum.GetValues(typeof(MapObjectType)))
                    {
                        var t = type;
                        bool active = _manager.SelectedObjectDef == null && _manager.SelectedObjectType == t;
                        string displayName = GetMapObjectDisplayName(type);
                        AddPaletteItem($"{displayName} ({type})", (Sprite)null, active, () =>
                        {
                            _manager.SelectObjectType(t);
                            RefreshSpawnConfig();
                        });
                    }
                    break;

                case ToolMode.Eraser:
                    AddPaletteHeader("Del");
                    AddPaletteLabel("LMB: 커서 위치 삭제\n우클릭: 다른 모드에서도 삭제\n\n마우스 오버 시\n삭제 대상 빨간색 표시");
                    break;

                case ToolMode.Move:
                    AddPaletteHeader("Move");
                    AddPaletteLabel("LMB: 오브젝트 집기/놓기\nRMB/ESC: 취소\nQ/E: 회전 (집은 상태)\nShift+Q/E: 미세 회전 (1°)\n\n마우스 오버 시\n이동 대상 초록색 표시");
                    break;
            }
        }

        void AddPaletteHeader(string text)
        {
            var go = new GameObject("Header");
            go.transform.SetParent(_paletteContent, false);
            var rt = go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 25;

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = DefaultFont;
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.color = new Color(0.8f, 0.8f, 0.8f);
            txt.alignment = TextAnchor.MiddleLeft;
        }

        Texture GetPrefabMainTexture(GameObject prefab)
        {
            if (prefab == null) return null;
            var r = prefab.GetComponentInChildren<Renderer>();
            if (r == null || r.sharedMaterial == null) return null;
            return r.sharedMaterial.mainTexture;
        }

        void AddPaletteItem(string label, Texture texture, bool active, Action onClick)
        {
            Sprite sprite = null;
            if (texture is Texture2D tex2d)
            {
                sprite = Sprite.Create(tex2d, new Rect(0, 0, tex2d.width, tex2d.height), new Vector2(0.5f, 0.5f));
            }
            AddPaletteItem(label, sprite, active, onClick);
        }

        void AddPaletteItem(string label, Sprite icon, bool active, Action onClick)
        {
            var go = new GameObject(label);
            go.transform.SetParent(_paletteContent, false);

            var img = go.AddComponent<Image>();
            img.color = active ? BTN_ACTIVE : BTN_NORMAL;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 32;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = active ? BTN_ACTIVE : BTN_NORMAL;
            colors.highlightedColor = active ? BTN_ACTIVE : BTN_HOVER;
            colors.pressedColor = BTN_ACTIVE;
            btn.colors = colors;
            btn.onClick.AddListener(() =>
            {
                onClick?.Invoke();
                PopulatePalette();
            });

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(5, 5, 2, 2);
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;

            if (icon != null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(go.transform, false);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                var iconLe = iconGo.AddComponent<LayoutElement>();
                iconLe.preferredWidth = 26;
                iconLe.preferredHeight = 26;
            }

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = DefaultFont;
            txt.fontSize = 12;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;
            var txtLe = txtGo.AddComponent<LayoutElement>();
            txtLe.flexibleWidth = 1;
        }

        void AddPaletteLabel(string text)
        {
            var go = new GameObject("Info");
            go.transform.SetParent(_paletteContent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 50;

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = DefaultFont;
            txt.fontSize = 12;
            txt.color = new Color(0.6f, 0.6f, 0.6f);
            txt.alignment = TextAnchor.MiddleCenter;
        }

        // ======== FILTER PANEL ========

        void BuildFilterPanel()
        {
            _filterPanel = CreatePanel(_root, "FilterPanel", new Color(0.12f, 0.12f, 0.15f, 0.9f));
            SetAnchors(_filterPanel, new Vector2(0, 1), new Vector2(0, 1));
            _filterPanel.pivot = new Vector2(0, 1);
            _filterPanel.anchoredPosition = new Vector2(185, -52);
            _filterPanel.sizeDelta = new Vector2(200, 30);

            var layout = _filterPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 2, 2);
            layout.spacing = 3;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            string[] labels = { "T", "W", "P", "B", "O" };
            _filterButtons = new Button[5];
            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                var btnGo = new GameObject(labels[i]);
                btnGo.transform.SetParent(_filterPanel, false);
                var btnImg = btnGo.AddComponent<Image>();
                var btnLe = btnGo.AddComponent<LayoutElement>();
                btnLe.preferredWidth = 32;

                var txtGo = new GameObject("Text");
                txtGo.transform.SetParent(btnGo.transform, false);
                var txtRt = txtGo.AddComponent<RectTransform>();
                SetAnchors(txtRt, Vector2.zero, Vector2.one);
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
                var txt = txtGo.AddComponent<Text>();
                txt.text = labels[i];
                txt.font = DefaultFont;
                txt.fontSize = 13;
                txt.fontStyle = FontStyle.Bold;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;

                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                _filterButtons[i] = btn;
                btn.onClick.AddListener(() =>
                {
                    switch (idx)
                    {
                        case 0: _manager.SetTileVisibility(!_manager.ShowTiles); break;
                        case 1: _manager.SetWallVisibility(!_manager.ShowWalls); break;
                        case 2: _manager.SetPropVisibility(!_manager.ShowProps); break;
                        case 3: _manager.SetBuildingVisibility(!_manager.ShowBuildings); break;
                        case 4: _manager.SetMapObjectVisibility(!_manager.ShowMapObjects); break;
                    }
                });
            }
            RefreshFilterToggles();
        }

        void RefreshFilterToggles()
        {
            if (_filterButtons == null) return;
            bool[] states = { _manager.ShowTiles, _manager.ShowWalls, _manager.ShowProps, _manager.ShowBuildings, _manager.ShowMapObjects };
            for (int i = 0; i < _filterButtons.Length; i++)
            {
                if (_filterButtons[i] == null) continue;
                var img = _filterButtons[i].GetComponent<Image>();
                img.color = states[i] ? new Color(0.2f, 0.7f, 0.3f, 1f) : new Color(0.7f, 0.2f, 0.2f, 1f);
            }
        }

        // ======== SPAWN CONFIG PANEL ========

        void BuildSpawnConfigPanel()
        {
            _spawnConfigPanel = CreatePanel(_root, "SpawnConfig", PANEL_BG);
            SetAnchors(_spawnConfigPanel, new Vector2(0, 0), new Vector2(0, 0));
            _spawnConfigPanel.pivot = new Vector2(0, 0);
            _spawnConfigPanel.anchoredPosition = new Vector2(185, 0);
            _spawnConfigPanel.sizeDelta = new Vector2(250, 0);
            _spawnConfigPanel.gameObject.SetActive(false);
        }

        void RefreshSpawnConfig()
        {
            if (_spawnConfigPanel == null) return;

            foreach (Transform child in _spawnConfigPanel)
                Destroy(child.gameObject);

            bool showMapObj = _manager.CurrentTool == ToolMode.MapObject;
            bool showProp = _manager.CurrentTool == ToolMode.Prop;
            bool show = showMapObj || showProp;

            _spawnConfigPanel.gameObject.SetActive(show);
            if (!show) return;

            var vlg = _spawnConfigPanel.gameObject.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = _spawnConfigPanel.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(8, 8, 8, 8);
                vlg.spacing = 4;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
            }

            var csf = _spawnConfigPanel.gameObject.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = _spawnConfigPanel.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Prop 모드면 추가 설정 없음
            if (showProp) return;

            // ── Visual mode selection (all MapObject types) ──
            AddConfigHeader("비주얼 모드");
            string[] visualModeNames = { "구체", "텍스처", "투명", "이펙트" };
            var modeRow = new GameObject("VisualModeRow");
            modeRow.transform.SetParent(_spawnConfigPanel, false);
            var modeLayout = modeRow.AddComponent<HorizontalLayoutGroup>();
            modeLayout.spacing = 3;
            modeLayout.childForceExpandWidth = true;
            modeLayout.childForceExpandHeight = true;
            var modeLe = modeRow.AddComponent<LayoutElement>();
            modeLe.preferredHeight = 28;

            for (int i = 0; i < 4; i++)
            {
                int modeIdx = i;
                bool isActive = _manager.SpawnVisualMode == modeIdx;
                var btnGo = new GameObject(visualModeNames[i]);
                btnGo.transform.SetParent(modeRow.transform, false);
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = isActive ? BTN_ACTIVE : BTN_NORMAL;

                var txtGo = new GameObject("Text");
                txtGo.transform.SetParent(btnGo.transform, false);
                var txtRt = txtGo.AddComponent<RectTransform>();
                SetAnchors(txtRt, Vector2.zero, Vector2.one);
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
                var txt = txtGo.AddComponent<Text>();
                txt.text = visualModeNames[modeIdx];
                txt.font = DefaultFont;
                txt.fontSize = 11;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;

                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                btn.onClick.AddListener(() => { _manager.SpawnVisualMode = modeIdx; RefreshSpawnConfig(); });
            }

            // Visual-mode-specific fields
            if (_manager.SpawnVisualMode == 1) // TextureQuad
            {
                AddConfigInput("텍스처 경로:", _manager.SpawnVisualTexturePath, v => _manager.SpawnVisualTexturePath = v);
            }
            else if (_manager.SpawnVisualMode == 3) // EffectPrefab
            {
                AddConfigInput("이펙트 경로:", _manager.SpawnEffectPrefabPath, v => _manager.SpawnEffectPrefabPath = v);
            }

            // Scale (for TextureQuad and EffectPrefab)
            if (_manager.SpawnVisualMode == 1 || _manager.SpawnVisualMode == 3)
            {
                AddConfigInput("스케일:", _manager.SpawnVisualScale.ToString("F1"), v =>
                {
                    if (float.TryParse(v, out float f)) _manager.SpawnVisualScale = Mathf.Max(0.1f, f);
                });
            }

            // ── Type-specific config ──
            switch (_manager.SelectedObjectType)
            {
                case MapObjectType.LootContainer:
                    AddConfigHeader("루팅 상자 설정");
                    AddConfigInput("이름:", _manager.SpawnContainerName, v => _manager.SpawnContainerName = v);
                    AddConfigIntRow("가로 칸:", _manager.SpawnContainerWidth, 1, 8, v => _manager.SpawnContainerWidth = v);
                    AddConfigIntRow("세로 칸:", _manager.SpawnContainerHeight, 1, 8, v => _manager.SpawnContainerHeight = v);
                    AddConfigToggle("지역 루트:", _manager.SpawnUseRegionLoot, v => _manager.SpawnUseRegionLoot = v);
                    AddConfigInput("고정 아이템 ID:", _manager.SpawnFixedItemId, v => _manager.SpawnFixedItemId = v);
                    if (!string.IsNullOrEmpty(_manager.SpawnFixedItemId))
                        AddConfigIntRow("수량:", _manager.SpawnFixedItemCount, 1, 99, v => _manager.SpawnFixedItemCount = v);
                    break;

                case MapObjectType.ItemDrop:
                    AddConfigHeader("바닥 아이템 설정");
                    AddConfigToggle("지역 루트:", _manager.SpawnUseRegionLoot, v => _manager.SpawnUseRegionLoot = v);
                    AddConfigInput("고정 아이템 ID:", _manager.SpawnFixedItemId, v => _manager.SpawnFixedItemId = v);
                    if (!string.IsNullOrEmpty(_manager.SpawnFixedItemId))
                        AddConfigIntRow("수량:", _manager.SpawnFixedItemCount, 1, 99, v => _manager.SpawnFixedItemCount = v);
                    break;

                case MapObjectType.EnemySpawn:
                    AddConfigHeader("적 스폰 설정");
                    AddConfigInput("유닛 키:", _manager.SpawnEnemyUnitKey, v => _manager.SpawnEnemyUnitKey = v);
                    AddConfigIntRow("수량:", _manager.SpawnEnemyCount, 1, 10, v => _manager.SpawnEnemyCount = v);
                    break;

                case MapObjectType.Door:
                    AddConfigHeader("문 설정");
                    AddConfigDoorLockButtons();
                    break;

                case MapObjectType.Trigger:
                    AddConfigTriggerPanel();
                    break;

                case MapObjectType.NPC:
                    AddConfigHeader("NPC 설정");
                    AddConfigInput("NPC ID:", _manager.SpawnNpcId, v => _manager.SpawnNpcId = v);
                    AddConfigInput("표시 이름:", _manager.SpawnNpcDisplayName, v => _manager.SpawnNpcDisplayName = v);
                    // 카탈로그 NPC 빠른 선택
                    if (_manager.catalog != null && _manager.catalog.npcs != null && _manager.catalog.npcs.Length > 0)
                    {
                        AddConfigHeader("카탈로그 NPC");
                        foreach (var npc in _manager.catalog.npcs)
                        {
                            if (npc == null) continue;
                            bool isActive = _manager.SpawnNpcId == npc.npcId;
                            string btnLabel = !string.IsNullOrEmpty(npc.displayName) ? $"{npc.displayName} ({npc.npcId})" : npc.npcId;

                            var row = new GameObject("NPC_" + npc.npcId);
                            row.transform.SetParent(_spawnConfigPanel, false);
                            var rowImg = row.AddComponent<Image>();
                            rowImg.color = isActive ? BTN_ACTIVE : BTN_NORMAL;
                            var rowLe = row.AddComponent<LayoutElement>();
                            rowLe.preferredHeight = 26;

                            var rowTxtGo = new GameObject("Text");
                            rowTxtGo.transform.SetParent(row.transform, false);
                            var rowRt = rowTxtGo.AddComponent<RectTransform>();
                            SetAnchors(rowRt, Vector2.zero, Vector2.one);
                            rowRt.offsetMin = new Vector2(6, 0);
                            rowRt.offsetMax = new Vector2(-6, 0);
                            var rowTxt = rowTxtGo.AddComponent<Text>();
                            rowTxt.text = btnLabel;
                            rowTxt.font = DefaultFont;
                            rowTxt.fontSize = 11;
                            rowTxt.color = isActive ? Color.black : Color.white;
                            rowTxt.alignment = TextAnchor.MiddleLeft;

                            var npcRef = npc;
                            var btn = row.AddComponent<Button>();
                            btn.targetGraphic = rowImg;
                            btn.onClick.AddListener(() =>
                            {
                                _manager.SpawnNpcId = npcRef.npcId;
                                _manager.SpawnNpcDisplayName = npcRef.displayName ?? "";
                                RefreshSpawnConfig();
                            });
                        }
                    }
                    break;
            }
        }

        // ======== BUILDING VISIBILITY LIST (Feature B) ========

        void BuildBuildingListPanel()
        {
            _buildingListPanel = CreatePanel(_root, "BuildingList", PANEL_BG);
            SetAnchors(_buildingListPanel, new Vector2(1, 0), new Vector2(1, 1));
            _buildingListPanel.pivot = new Vector2(1, 1);
            _buildingListPanel.offsetMin = new Vector2(-190, 0);
            _buildingListPanel.offsetMax = new Vector2(0, -48);

            // Title bar
            var titleBar = CreatePanel(_buildingListPanel, "Title", new Color(0.1f, 0.1f, 0.13f, 1f));
            SetAnchors(titleBar, new Vector2(0, 1), new Vector2(1, 1));
            titleBar.offsetMin = new Vector2(0, -30);
            titleBar.offsetMax = Vector2.zero;

            var titleTextGo = new GameObject("TitleText");
            titleTextGo.transform.SetParent(titleBar, false);
            var titleRt = titleTextGo.AddComponent<RectTransform>();
            SetAnchors(titleRt, Vector2.zero, Vector2.one);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            var titleLabel = titleTextGo.AddComponent<Text>();
            titleLabel.text = "  건물 표시";
            titleLabel.font = DefaultFont;
            titleLabel.fontSize = 14;
            titleLabel.color = Color.white;
            titleLabel.alignment = TextAnchor.MiddleLeft;

            // Action buttons row (내부 보기 / 전체 보기)
            var btnRow = CreatePanel(_buildingListPanel, "Actions", Color.clear);
            SetAnchors(btnRow, new Vector2(0, 1), new Vector2(1, 1));
            btnRow.offsetMin = new Vector2(4, -64);
            btnRow.offsetMax = new Vector2(-4, -32);
            var btnLayout = btnRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            btnLayout.spacing = 4;
            btnLayout.childForceExpandWidth = true;
            btnLayout.childForceExpandHeight = true;
            _interiorViewBtn = CreateButton(btnRow, "내부 보기", 60, () => { _manager.ToggleInteriorView(); PopulateBuildingList(); });
            CreateButton(btnRow, "전체 보기", 60, () => { _manager.ShowAllBuildings(); PopulateBuildingList(); });
            // 벽으로 직접 쌓은 구조물 내부에 프랍을 놓도록 전체 벽 숨김 토글.
            _wallHideBtn = CreateButton(btnRow, "벽 숨김", 60, () => { _manager.ToggleWallsHidden(); PopulateBuildingList(); });

            // Scroll area
            var scrollArea = CreatePanel(_buildingListPanel, "ScrollArea", Color.clear);
            SetAnchors(scrollArea, Vector2.zero, Vector2.one);
            scrollArea.offsetMin = new Vector2(5, 5);
            scrollArea.offsetMax = new Vector2(-5, -66);

            var scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scrollArea.gameObject.AddComponent<RectMask2D>();

            _buildingListContent = new GameObject("Content").AddComponent<RectTransform>();
            _buildingListContent.SetParent(scrollArea, false);
            SetAnchors(_buildingListContent, new Vector2(0, 1), new Vector2(1, 1));
            _buildingListContent.pivot = new Vector2(0.5f, 1f);
            _buildingListContent.offsetMin = Vector2.zero;
            _buildingListContent.offsetMax = Vector2.zero;

            var vlg = _buildingListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(2, 2, 2, 2);
            vlg.spacing = 3;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = _buildingListContent.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = _buildingListContent;

            PopulateBuildingList();
        }

        public void PopulateBuildingList()
        {
            if (_buildingListContent == null) return;
            foreach (Transform child in _buildingListContent)
                Destroy(child.gameObject);

            // 내부 보기 버튼 활성 상태 색
            if (_interiorViewBtn != null)
            {
                var img = _interiorViewBtn.GetComponent<Image>();
                if (img != null)
                    img.color = _manager.InteriorViewActive ? BTN_ACTIVE : BTN_NORMAL;
            }

            // 벽 숨김 버튼 활성 상태 색
            if (_wallHideBtn != null)
            {
                var img = _wallHideBtn.GetComponent<Image>();
                if (img != null)
                    img.color = _manager.WallsHidden ? BTN_ACTIVE : BTN_NORMAL;
            }

            var buildings = _manager.PlacedBuildings;
            if (buildings == null || buildings.Count == 0)
            {
                var infoGo = new GameObject("Info");
                infoGo.transform.SetParent(_buildingListContent, false);
                var le = infoGo.AddComponent<LayoutElement>();
                le.preferredHeight = 24;
                var txt = infoGo.AddComponent<Text>();
                txt.text = "  배치된 건물 없음";
                txt.font = DefaultFont;
                txt.fontSize = 11;
                txt.color = new Color(0.5f, 0.5f, 0.5f);
                txt.alignment = TextAnchor.MiddleLeft;
                return;
            }

            foreach (var b in buildings)
            {
                if (b == null) continue;
                AddBuildingListItem(b);
            }
        }

        void AddBuildingListItem(PlacedBuilding b)
        {
            string displayName = b.buildingDefinition != null
                ? b.buildingDefinition.displayName
                : b.buildingDefinitionId;
            if (string.IsNullOrEmpty(displayName))
                displayName = b.instanceId;
            string shortName = displayName.Length > 14 ? displayName[..12] + ".." : displayName;

            bool hidden = _manager.IsBuildingHidden(b.instanceId);

            var row = new GameObject("BuildingRow");
            row.transform.SetParent(_buildingListContent, false);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 3;
            rowLayout.childForceExpandHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 26;

            // 이름 라벨
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var labelTxt = labelGo.AddComponent<Text>();
            labelTxt.text = shortName;
            labelTxt.font = DefaultFont;
            labelTxt.fontSize = 11;
            labelTxt.color = hidden ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
            labelTxt.alignment = TextAnchor.MiddleLeft;
            var labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1;

            // 숨김/표시 토글 버튼
            string bid = b.instanceId;
            var btnGo = new GameObject("Toggle");
            btnGo.transform.SetParent(row.transform, false);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = hidden ? new Color(0.5f, 0.2f, 0.2f) : new Color(0.2f, 0.7f, 0.3f);
            var btnLe = btnGo.AddComponent<LayoutElement>();
            btnLe.preferredWidth = 52;

            var btnTxtGo = new GameObject("Text");
            btnTxtGo.transform.SetParent(btnGo.transform, false);
            var btnRt = btnTxtGo.AddComponent<RectTransform>();
            SetAnchors(btnRt, Vector2.zero, Vector2.one);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
            var btnTxt = btnTxtGo.AddComponent<Text>();
            // 버튼은 "클릭하면 할 동작"을 표시: 보이는 건물 → "숨김", 숨긴 건물 → "표시"
            btnTxt.text = hidden ? "표시" : "숨김";
            btnTxt.font = DefaultFont;
            btnTxt.fontSize = 11;
            btnTxt.color = Color.white;
            btnTxt.alignment = TextAnchor.MiddleCenter;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() =>
            {
                _manager.SetBuildingHidden(bid, !_manager.IsBuildingHidden(bid));
                PopulateBuildingList();
            });
        }

        void AddConfigDoorLockButtons()
        {
            string[] lockNames = { "없음", "열쇠", "퀘스트", "스위치" };
            var lockRow = new GameObject("LockRow");
            lockRow.transform.SetParent(_spawnConfigPanel, false);
            var lockLayout = lockRow.AddComponent<HorizontalLayoutGroup>();
            lockLayout.spacing = 3;
            lockLayout.childForceExpandWidth = true;
            lockLayout.childForceExpandHeight = true;
            var lockLe = lockRow.AddComponent<LayoutElement>();
            lockLe.preferredHeight = 28;

            for (int i = 0; i < 4; i++)
            {
                int lockIdx = i;
                bool isActive = _manager.SpawnDoorLockType == lockIdx;
                var btnGo = new GameObject(lockNames[i]);
                btnGo.transform.SetParent(lockRow.transform, false);
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = isActive ? BTN_ACTIVE : BTN_NORMAL;

                var txtGo = new GameObject("Text");
                txtGo.transform.SetParent(btnGo.transform, false);
                var txtRt = txtGo.AddComponent<RectTransform>();
                SetAnchors(txtRt, Vector2.zero, Vector2.one);
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
                var txt = txtGo.AddComponent<Text>();
                txt.text = lockNames[lockIdx];
                txt.font = DefaultFont;
                txt.fontSize = 11;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;

                var btn = btnGo.AddComponent<Button>();
                btn.onClick.AddListener(() => { _manager.SpawnDoorLockType = lockIdx; RefreshSpawnConfig(); });
            }

            switch (_manager.SpawnDoorLockType)
            {
                case 1: // Key
                    AddConfigInput("열쇠 ID:", _manager.SpawnDoorKeyId, v => _manager.SpawnDoorKeyId = v);
                    AddConfigToggle("열쇠 소모:", _manager.SpawnDoorConsumeKey, v => _manager.SpawnDoorConsumeKey = v);
                    break;
                case 2: // Quest
                    AddConfigInput("퀘스트 ID:", _manager.SpawnDoorQuestId, v => _manager.SpawnDoorQuestId = v);
                    break;
            }
        }

        void AddConfigTriggerPanel()
        {
            AddConfigHeader("트리거 설정");

            // Mode buttons
            string[] modeNames = { "씬전환", "로컬이동", "스토리", "커스텀" };
            var modeRow = new GameObject("TriggerModeRow");
            modeRow.transform.SetParent(_spawnConfigPanel, false);
            var modeLayout = modeRow.AddComponent<HorizontalLayoutGroup>();
            modeLayout.spacing = 3;
            modeLayout.childForceExpandWidth = true;
            modeLayout.childForceExpandHeight = true;
            var modeLe = modeRow.AddComponent<LayoutElement>();
            modeLe.preferredHeight = 28;

            for (int i = 0; i < 4; i++)
            {
                int modeIdx = i;
                bool isActive = _manager.SpawnTriggerMode == modeIdx;
                var btnGo = new GameObject(modeNames[i]);
                btnGo.transform.SetParent(modeRow.transform, false);
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = isActive ? BTN_ACTIVE : BTN_NORMAL;

                var txtGo = new GameObject("Text");
                txtGo.transform.SetParent(btnGo.transform, false);
                var txtRt = txtGo.AddComponent<RectTransform>();
                SetAnchors(txtRt, Vector2.zero, Vector2.one);
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
                var txt = txtGo.AddComponent<Text>();
                txt.text = modeNames[modeIdx];
                txt.font = DefaultFont;
                txt.fontSize = 11;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;

                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                btn.onClick.AddListener(() => { _manager.SpawnTriggerMode = modeIdx; RefreshSpawnConfig(); });
            }

            // Common trigger settings
            AddConfigToggle("자동 입장:", _manager.SpawnTriggerAutoEnter, v => _manager.SpawnTriggerAutoEnter = v);
            AddConfigToggle("1회 발동:", _manager.SpawnTriggerOneShot, v => _manager.SpawnTriggerOneShot = v);
            AddConfigInput("지연 시간:", _manager.SpawnTriggerDelay.ToString("F1"), v =>
            {
                if (float.TryParse(v, out float f)) _manager.SpawnTriggerDelay = Mathf.Max(0f, f);
            });

            // Collider size
            AddConfigHeader("콜라이더 크기");
            AddConfigInput("X:", _manager.SpawnTriggerSizeX.ToString("F1"), v =>
            {
                if (float.TryParse(v, out float f)) _manager.SpawnTriggerSizeX = Mathf.Max(0.1f, f);
            });
            AddConfigInput("Y:", _manager.SpawnTriggerSizeY.ToString("F1"), v =>
            {
                if (float.TryParse(v, out float f)) _manager.SpawnTriggerSizeY = Mathf.Max(0.1f, f);
            });
            AddConfigInput("Z:", _manager.SpawnTriggerSizeZ.ToString("F1"), v =>
            {
                if (float.TryParse(v, out float f)) _manager.SpawnTriggerSizeZ = Mathf.Max(0.1f, f);
            });

            // Mode-specific fields
            switch (_manager.SpawnTriggerMode)
            {
                case 0: // SceneTransition
                    AddConfigHeader("씬 전환");
                    AddConfigInput("대상 씬:", _manager.SpawnTriggerTargetScene, v => _manager.SpawnTriggerTargetScene = v);
                    AddConfigInput("스폰 ID:", _manager.SpawnTriggerTargetSpawnId, v => _manager.SpawnTriggerTargetSpawnId = v);
                    break;

                case 1: // LocalTeleport
                    AddConfigHeader("로컬 텔레포트");
                    AddConfigInput("X:", _manager.SpawnTeleportX.ToString("F1"), v =>
                    {
                        if (float.TryParse(v, out float f)) _manager.SpawnTeleportX = f;
                    });
                    AddConfigInput("Y:", _manager.SpawnTeleportY.ToString("F1"), v =>
                    {
                        if (float.TryParse(v, out float f)) _manager.SpawnTeleportY = f;
                    });
                    AddConfigInput("Z:", _manager.SpawnTeleportZ.ToString("F1"), v =>
                    {
                        if (float.TryParse(v, out float f)) _manager.SpawnTeleportZ = f;
                    });
                    AddConfigInput("Y 회전:", _manager.SpawnTeleportYRot.ToString("F0"), v =>
                    {
                        if (float.TryParse(v, out float f)) _manager.SpawnTeleportYRot = f;
                    });
                    break;

                case 2: // StoryTrigger
                    AddConfigHeader("스토리 트리거");
                    AddConfigInput("스토리 씬 ID:", _manager.SpawnTriggerStorySceneId, v => _manager.SpawnTriggerStorySceneId = v);
                    break;

                case 3: // CustomEvent
                    AddConfigHeader("커스텀 이벤트");
                    AddConfigInput("커스텀 데이터:", _manager.SpawnTriggerCustomData, v => _manager.SpawnTriggerCustomData = v);
                    break;
            }

            // Condition settings (shared with door)
            AddConfigHeader("조건");
            AddConfigInput("필요 아이템:", _manager.SpawnDoorKeyId, v => _manager.SpawnDoorKeyId = v);
            AddConfigInput("필요 퀘스트:", _manager.SpawnDoorQuestId, v => _manager.SpawnDoorQuestId = v);
        }

        void AddConfigHeader(string text)
        {
            var go = new GameObject("ConfigHeader");
            go.transform.SetParent(_spawnConfigPanel, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 22;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.25f, 1f);

            // Text는 Image와 같은 GO에 둘 수 없음 (둘 다 Graphic)
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            SetAnchors(txtRt, Vector2.zero, Vector2.one);
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.text = $"  {text}";
            txt.font = DefaultFont;
            txt.fontSize = 12;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;
        }

        void AddConfigInput(string label, string value, Action<string> onChange)
        {
            var row = new GameObject("InputRow");
            row.transform.SetParent(_spawnConfigPanel, false);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 4;
            rowLayout.childForceExpandHeight = true;
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 24;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var labelTxt = labelGo.AddComponent<Text>();
            labelTxt.text = label;
            labelTxt.font = DefaultFont;
            labelTxt.fontSize = 11;
            labelTxt.color = Color.white;
            labelTxt.alignment = TextAnchor.MiddleLeft;
            var labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.preferredWidth = 90;

            var inputGo = new GameObject("Input");
            inputGo.transform.SetParent(row.transform, false);
            var inputImg = inputGo.AddComponent<Image>();
            inputImg.color = new Color(0.2f, 0.2f, 0.22f, 1f);
            var inputLe = inputGo.AddComponent<LayoutElement>();
            inputLe.flexibleWidth = 1;

            var inputTxtGo = new GameObject("Text");
            inputTxtGo.transform.SetParent(inputGo.transform, false);
            var inputRt = inputTxtGo.AddComponent<RectTransform>();
            SetAnchors(inputRt, Vector2.zero, Vector2.one);
            inputRt.offsetMin = new Vector2(4, 0);
            inputRt.offsetMax = new Vector2(-4, 0);
            var inputTxt = inputTxtGo.AddComponent<Text>();
            inputTxt.font = DefaultFont;
            inputTxt.fontSize = 11;
            inputTxt.color = Color.white;
            inputTxt.alignment = TextAnchor.MiddleLeft;
            inputTxt.supportRichText = false;

            var input = inputGo.AddComponent<InputField>();
            input.textComponent = inputTxt;
            input.text = value ?? "";
            input.onEndEdit.AddListener(v => onChange?.Invoke(v));
        }

        void AddConfigIntRow(string label, int value, int min, int max, Action<int> onChange)
        {
            var row = new GameObject("IntRow");
            row.transform.SetParent(_spawnConfigPanel, false);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 4;
            rowLayout.childForceExpandHeight = true;
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 24;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var labelTxt = labelGo.AddComponent<Text>();
            labelTxt.text = label;
            labelTxt.font = DefaultFont;
            labelTxt.fontSize = 11;
            labelTxt.color = Color.white;
            var labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.preferredWidth = 90;

            int current = value;
            var valGo = new GameObject("Value");
            valGo.transform.SetParent(row.transform, false);
            var valTxt = valGo.AddComponent<Text>();
            valTxt.text = current.ToString();
            valTxt.font = DefaultFont;
            valTxt.fontSize = 12;
            valTxt.color = Color.white;
            valTxt.alignment = TextAnchor.MiddleCenter;
            var valLe = valGo.AddComponent<LayoutElement>();
            valLe.preferredWidth = 30;

            CreateSmallButton(row.transform, "-", () => { current = Mathf.Max(min, current - 1); valTxt.text = current.ToString(); onChange?.Invoke(current); });
            CreateSmallButton(row.transform, "+", () => { current = Mathf.Min(max, current + 1); valTxt.text = current.ToString(); onChange?.Invoke(current); });
        }

        void AddConfigToggle(string label, bool value, Action<bool> onChange)
        {
            var row = new GameObject("ToggleRow");
            row.transform.SetParent(_spawnConfigPanel, false);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 4;
            rowLayout.childForceExpandHeight = true;
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 24;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var labelTxt = labelGo.AddComponent<Text>();
            labelTxt.text = label;
            labelTxt.font = DefaultFont;
            labelTxt.fontSize = 11;
            labelTxt.color = Color.white;
            var labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.preferredWidth = 90;

            bool current = value;
            var btnGo = new GameObject("Toggle");
            btnGo.transform.SetParent(row.transform, false);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = current ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.5f, 0.2f, 0.2f);
            var btnLe = btnGo.AddComponent<LayoutElement>();
            btnLe.preferredWidth = 40;

            var btnTxtGo = new GameObject("Text");
            btnTxtGo.transform.SetParent(btnGo.transform, false);
            var btnRt = btnTxtGo.AddComponent<RectTransform>();
            SetAnchors(btnRt, Vector2.zero, Vector2.one);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
            var btnTxt = btnTxtGo.AddComponent<Text>();
            btnTxt.text = current ? "ON" : "OFF";
            btnTxt.font = DefaultFont;
            btnTxt.fontSize = 11;
            btnTxt.color = Color.white;
            btnTxt.alignment = TextAnchor.MiddleCenter;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() =>
            {
                current = !current;
                btnImg.color = current ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.5f, 0.2f, 0.2f);
                btnTxt.text = current ? "ON" : "OFF";
                onChange?.Invoke(current);
            });
        }

        void CreateSmallButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = BTN_NORMAL;
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 24;

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            SetAnchors(txtRt, Vector2.zero, Vector2.one);
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = DefaultFont;
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        // ── 건물 소속 선택기 (Prop + MapObject 공통) ──

        void BuildParentBuildingSelector()
        {
            if (_manager.EditingMap == null) return;
            var buildings = _manager.EditingMap.buildings;

            AddConfigHeader("건물 소속 (내부 프랍)");

            // "없음" + 건물 목록 버튼 행
            var row = new GameObject("BuildingRow");
            row.transform.SetParent(_spawnConfigPanel, false);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 3;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 26;

            // "없음" 버튼
            bool noneSelected = string.IsNullOrEmpty(_manager.SelectedParentBuildingId);
            var noneBtnGo = new GameObject("None");
            noneBtnGo.transform.SetParent(row.transform, false);
            var noneImg = noneBtnGo.AddComponent<Image>();
            noneImg.color = noneSelected ? new Color(0.3f, 0.6f, 1f, 1f) : BTN_NORMAL;
            var noneLe = noneBtnGo.AddComponent<LayoutElement>();
            noneLe.preferredWidth = 45;

            var noneTxtGo = new GameObject("Text");
            noneTxtGo.transform.SetParent(noneBtnGo.transform, false);
            var noneRt = noneTxtGo.AddComponent<RectTransform>();
            SetAnchors(noneRt, Vector2.zero, Vector2.one);
            noneRt.offsetMin = Vector2.zero;
            noneRt.offsetMax = Vector2.zero;
            var noneTxt = noneTxtGo.AddComponent<Text>();
            noneTxt.text = "없음";
            noneTxt.font = DefaultFont;
            noneTxt.fontSize = 11;
            noneTxt.color = Color.white;
            noneTxt.alignment = TextAnchor.MiddleCenter;

            var noneBtn = noneBtnGo.AddComponent<Button>();
            noneBtn.targetGraphic = noneImg;
            noneBtn.onClick.AddListener(() =>
            {
                _manager.SelectedParentBuildingId = "";
                RefreshSpawnConfig();
            });

            if (buildings.Count == 0)
            {
                // 건물이 없으면 안내 텍스트
                var infoGo = new GameObject("Info");
                infoGo.transform.SetParent(_spawnConfigPanel, false);
                var infoLe = infoGo.AddComponent<LayoutElement>();
                infoLe.preferredHeight = 20;
                var infoTxt = infoGo.AddComponent<Text>();
                infoTxt.text = "  (맵에 건물을 먼저 배치하세요)";
                infoTxt.font = DefaultFont;
                infoTxt.fontSize = 10;
                infoTxt.color = new Color(0.5f, 0.5f, 0.5f);
                infoTxt.alignment = TextAnchor.MiddleLeft;
                return;
            }

            // 건물 목록 (스크롤 가능한 버튼 리스트)
            foreach (var building in buildings)
            {
                bool isSelected = _manager.SelectedParentBuildingId == building.instanceId;
                string displayName = building.buildingDefinition != null
                    ? building.buildingDefinition.displayName
                    : building.buildingDefinitionId;
                if (string.IsNullOrEmpty(displayName))
                    displayName = building.instanceId;

                // 건물 이름을 축약 (너무 길면)
                string shortName = displayName.Length > 12
                    ? displayName[..10] + ".."
                    : displayName;

                var bRow = new GameObject("Building");
                bRow.transform.SetParent(_spawnConfigPanel, false);
                var bRowLayout = bRow.AddComponent<HorizontalLayoutGroup>();
                bRowLayout.spacing = 4;
                bRowLayout.childForceExpandHeight = true;
                var bRowLe = bRow.AddComponent<LayoutElement>();
                bRowLe.preferredHeight = 24;

                var bBtnGo = new GameObject("Btn");
                bBtnGo.transform.SetParent(bRow.transform, false);
                var bImg = bBtnGo.AddComponent<Image>();
                bImg.color = isSelected ? new Color(0.3f, 0.6f, 1f, 1f) : BTN_NORMAL;
                var bBtnLe = bBtnGo.AddComponent<LayoutElement>();
                bBtnLe.flexibleWidth = 1;

                var bTxtGo = new GameObject("Text");
                bTxtGo.transform.SetParent(bBtnGo.transform, false);
                var bTxtRt = bTxtGo.AddComponent<RectTransform>();
                SetAnchors(bTxtRt, Vector2.zero, Vector2.one);
                bTxtRt.offsetMin = new Vector2(4, 0);
                bTxtRt.offsetMax = new Vector2(-4, 0);
                var bTxt = bTxtGo.AddComponent<Text>();
                bTxt.text = $"{shortName} [{building.instanceId}]";
                bTxt.font = DefaultFont;
                bTxt.fontSize = 10;
                bTxt.color = Color.white;
                bTxt.alignment = TextAnchor.MiddleLeft;

                string bid = building.instanceId;
                var bBtn = bBtnGo.AddComponent<Button>();
                bBtn.targetGraphic = bImg;
                bBtn.onClick.AddListener(() =>
                {
                    _manager.SelectedParentBuildingId = bid;
                    RefreshSpawnConfig();
                });
            }
        }

        static string GetMapObjectDisplayName(MapObjectType type) => type switch
        {
            MapObjectType.SpawnPoint => "스폰 포인트",
            MapObjectType.EscapePoint => "탈출구",
            MapObjectType.LootContainer => "루팅 상자",
            MapObjectType.EnemySpawn => "적 스폰",
            MapObjectType.ItemDrop => "바닥 아이템",
            MapObjectType.Trigger => "트리거",
            MapObjectType.Custom => "커스텀",
            MapObjectType.NPC => "NPC",
            MapObjectType.Note => "쪽지",
            MapObjectType.Bed => "침대",
            MapObjectType.Workbench => "작업대",
            MapObjectType.MapBoard => "지도판",
            MapObjectType.MedicalBench => "의료대",
            MapObjectType.CookingBench => "조리대",
            MapObjectType.GenericInteract => "상호작용",
            MapObjectType.Door => "문",
            _ => type.ToString()
        };

        // ======== HOVER TOOLTIP (Eraser Preview) ========

        void BuildHoverTooltip()
        {
            var go = new GameObject("HoverTooltip");
            go.transform.SetParent(_root, false);
            _hoverTooltip = go.AddComponent<RectTransform>();
            _hoverTooltip.pivot = new Vector2(0, 1);
            _hoverTooltip.sizeDelta = new Vector2(200, 28);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.8f, 0.15f, 0.15f, 0.85f);
            bg.raycastTarget = false;

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            SetAnchors(txtRt, Vector2.zero, Vector2.one);
            txtRt.offsetMin = new Vector2(8, 2);
            txtRt.offsetMax = new Vector2(-8, -2);
            _hoverTooltipText = txtGo.AddComponent<Text>();
            _hoverTooltipText.font = DefaultFont;
            _hoverTooltipText.fontSize = 12;
            _hoverTooltipText.fontStyle = FontStyle.Bold;
            _hoverTooltipText.color = Color.white;
            _hoverTooltipText.alignment = TextAnchor.MiddleLeft;
            _hoverTooltipText.raycastTarget = false;

            var csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 4, 4);
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // ContentSizeFitter needs a LayoutElement on the text child
            var txtLe = txtGo.AddComponent<LayoutElement>();
            txtLe.preferredHeight = 20;

            _hoverTooltip.gameObject.SetActive(false);
        }

        void UpdateHoverTooltip()
        {
            string info = null;
            Color bgColor = default;

            if (_manager.ResizeMode && !string.IsNullOrEmpty(_manager.ResizeHoverInfo))
            {
                info = $"크기: {_manager.ResizeHoverInfo}  [스크롤: 조절]";
                bgColor = new Color(0.15f, 0.4f, 0.8f, 0.85f);
            }
            else if (_manager.CurrentTool == ToolMode.Move && !string.IsNullOrEmpty(_manager.MoveHoverInfo))
            {
                info = _manager.IsMovingObject
                    ? $"이동: {_manager.MoveHoverInfo}"
                    : $"이동: {_manager.MoveHoverInfo}  [클릭: 집기]";
                bgColor = new Color(0.1f, 0.6f, 0.2f, 0.85f);
            }
            else if (!string.IsNullOrEmpty(_manager.EraseHoverInfo))
            {
                info = $"삭제: {_manager.EraseHoverInfo}";
                bgColor = new Color(0.8f, 0.15f, 0.15f, 0.85f);
            }

            if (string.IsNullOrEmpty(info))
            {
                _hoverTooltip.gameObject.SetActive(false);
                return;
            }

            _hoverTooltip.gameObject.SetActive(true);
            _hoverTooltipText.text = info;
            _hoverTooltip.GetComponent<Image>().color = bgColor;

            // Follow mouse position (convert screen to canvas space)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _root, Input.mousePosition, null, out var localPos);
            _hoverTooltip.anchoredPosition = localPos + new Vector2(15, 10);
        }

        // ======== SAVE/LOAD DIALOG ========

        void BuildSaveLoadDialog()
        {
            _dialogPanel = CreatePanel(_root, "Dialog", new Color(0, 0, 0, 0.7f));
            SetAnchors(_dialogPanel, Vector2.zero, Vector2.one);
            _dialogPanel.offsetMin = Vector2.zero;
            _dialogPanel.offsetMax = Vector2.zero;

            // 고정 크기 + 화면 중앙 (분수 앵커는 화면비에 따라 잘려서 픽셀 고정)
            var center = CreatePanel(_dialogPanel, "DialogCenter", PANEL_BG);
            center.anchorMin = new Vector2(0.5f, 0.5f);
            center.anchorMax = new Vector2(0.5f, 0.5f);
            center.pivot = new Vector2(0.5f, 0.5f);
            center.sizeDelta = new Vector2(480, 560);
            center.anchoredPosition = Vector2.zero;

            var vlg = center.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 14, 14);
            vlg.spacing = 8;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            _dialogTitle = CreateTextObj(center, "Title", "Save Map", 17, FontStyle.Bold);
            var titleLe = _dialogTitle.gameObject.GetComponent<LayoutElement>();
            titleLe.preferredHeight = 26;

            // Filename input (작게)
            var inputRow = CreateRow(center, 26);
            CreateTextObj(inputRow, "Label", "Filename:", 13, FontStyle.Normal);
            _dialogInput = CreateInputField(inputRow, "map_name");

            // File list (for load) — 남는 공간 전부 차지해서 많이 보이게
            var listArea = CreatePanel(center, "FileList", new Color(0.1f, 0.1f, 0.12f, 1f));
            var listLe = listArea.gameObject.AddComponent<LayoutElement>();
            listLe.flexibleHeight = 1;
            listLe.minHeight = 320;

            var listScroll = listArea.gameObject.AddComponent<ScrollRect>();
            listScroll.horizontal = false;
            listArea.gameObject.AddComponent<RectMask2D>();

            _fileListContent = new GameObject("Content").AddComponent<RectTransform>();
            _fileListContent.SetParent(listArea, false);
            SetAnchors(_fileListContent, new Vector2(0, 1), new Vector2(1, 1));
            _fileListContent.pivot = new Vector2(0.5f, 1f);
            _fileListContent.offsetMin = Vector2.zero;
            _fileListContent.offsetMax = Vector2.zero;

            var contentVlg = _fileListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            contentVlg.padding = new RectOffset(5, 5, 5, 5);
            contentVlg.spacing = 3;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;

            var contentCsf = _fileListContent.gameObject.AddComponent<ContentSizeFitter>();
            contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            listScroll.content = _fileListContent;

            // Buttons row (작게, 가운데 정렬)
            var btnRow = CreateRow(center, 30);
            var btnHlg = btnRow.GetComponent<HorizontalLayoutGroup>();
            if (btnHlg != null) btnHlg.childAlignment = TextAnchor.MiddleCenter;
            CreateButton(btnRow, "OK", 90, OnDialogOK);
            CreateButton(btnRow, "Cancel", 90, () => _dialogPanel.gameObject.SetActive(false));

            _dialogPanel.gameObject.SetActive(false);
        }

        public void ShowSaveDialog()
        {
            _dialogTitle.text = "Save Map";
            _dialogInput.text = _manager.EditingMap?.mapName ?? "NewMap";
            PopulateFileList(false);
            _dialogCallback = (filename) => _manager.SaveMap(filename);
            _dialogPanel.gameObject.SetActive(true);
        }

        public void ShowLoadDialog()
        {
            _dialogTitle.text = "Load Map";
            _dialogInput.text = "";
            PopulateFileList(true);
            _dialogCallback = (filename) => _manager.LoadMap(filename);
            _dialogPanel.gameObject.SetActive(true);
        }

        void PopulateFileList(bool clickToSelect)
        {
            foreach (Transform child in _fileListContent)
                Destroy(child.gameObject);

            var files = _manager.GetSavedMapFiles();
            foreach (var f in files)
            {
                string fname = f;
                var go = new GameObject(fname);
                go.transform.SetParent(_fileListContent, false);

                var img = go.AddComponent<Image>();
                img.color = BTN_NORMAL;

                var le = go.AddComponent<LayoutElement>();
                le.preferredHeight = 28;

                var txt = new GameObject("Label").AddComponent<Text>();
                txt.transform.SetParent(go.transform, false);
                var txtRt = txt.GetComponent<RectTransform>();
                SetAnchors(txtRt, Vector2.zero, Vector2.one);
                txtRt.offsetMin = new Vector2(10, 0);
                txtRt.offsetMax = Vector2.zero;
                txt.text = fname;
                txt.font = DefaultFont;
                txt.fontSize = 13;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleLeft;

                var btn = go.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    _dialogInput.text = fname;
                    if (clickToSelect)
                    {
                        // Double-click style: just set the name, OK to confirm
                    }
                });
            }
        }

        void OnDialogOK()
        {
            string filename = _dialogInput.text?.Trim();
            if (string.IsNullOrEmpty(filename)) return;

            _dialogCallback?.Invoke(filename);
            _dialogPanel.gameObject.SetActive(false);
        }

        // ======== NEW MAP DIALOG ========

        void BuildNewMapDialog()
        {
            _newMapPanel = CreatePanel(_root, "NewMapDialog", new Color(0, 0, 0, 0.7f));
            SetAnchors(_newMapPanel, Vector2.zero, Vector2.one);

            var center = CreatePanel(_newMapPanel, "Center", PANEL_BG);
            center.anchorMin = new Vector2(0.35f, 0.35f);
            center.anchorMax = new Vector2(0.65f, 0.65f);
            center.offsetMin = Vector2.zero;
            center.offsetMax = Vector2.zero;

            var vlg = center.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 12;
            vlg.childForceExpandHeight = false;

            CreateTextObj(center, "Title", "New Map", 18, FontStyle.Bold);

            var wRow = CreateRow(center, 30);
            CreateTextObj(wRow, "WLabel", "Width:", 14, FontStyle.Normal);
            _widthInput = CreateInputField(wRow, "64");
            _widthInput.contentType = InputField.ContentType.IntegerNumber;

            var hRow = CreateRow(center, 30);
            CreateTextObj(hRow, "HLabel", "Height:", 14, FontStyle.Normal);
            _heightInput = CreateInputField(hRow, "64");
            _heightInput.contentType = InputField.ContentType.IntegerNumber;

            var tRow = CreateRow(center, 30);
            CreateTextObj(tRow, "TLabel", "Tile Size:", 14, FontStyle.Normal);
            _tileSizeInput = CreateInputField(tRow, "1");
            _tileSizeInput.contentType = InputField.ContentType.DecimalNumber;

            var btnRow = CreateRow(center, 35);
            CreateButton(btnRow, "Create", 100, () =>
            {
                int.TryParse(_widthInput.text, out int w);
                int.TryParse(_heightInput.text, out int h);
                float.TryParse(_tileSizeInput.text, out float ts);
                w = Mathf.Clamp(w, 4, 512);
                h = Mathf.Clamp(h, 4, 512);
                ts = Mathf.Clamp(ts <= 0f ? 1f : ts, 0.1f, 10f);
                _manager.NewMap(w, h, ts);
                _newMapPanel.gameObject.SetActive(false);
            });
            CreateButton(btnRow, "Cancel", 100, () => _newMapPanel.gameObject.SetActive(false));

            _newMapPanel.gameObject.SetActive(false);
        }

        public void ShowNewMapDialog()
        {
            _widthInput.text = "32";
            _heightInput.text = "32";
            // 현재 맵의 타일 크기를 기본값으로 (없으면 1)
            float curTs = _manager.EditingMap?.gridSettings != null
                ? _manager.EditingMap.gridSettings.tileSize : 1f;
            _tileSizeInput.text = curTs.ToString("0.##");
            _newMapPanel.gameObject.SetActive(true);
        }

        // ======== HELP PANEL ========

        void BuildHelpPanel()
        {
            _helpPanel = CreatePanel(_root, "HelpPanel", new Color(0, 0, 0, 0.7f));
            SetAnchors(_helpPanel, Vector2.zero, Vector2.one);
            _helpPanel.offsetMin = Vector2.zero;
            _helpPanel.offsetMax = Vector2.zero;

            var center = CreatePanel(_helpPanel, "HelpCenter", new Color(0.12f, 0.12f, 0.15f, 0.95f));
            center.anchorMin = new Vector2(0.5f, 0.5f);
            center.anchorMax = new Vector2(0.5f, 0.5f);
            center.pivot = new Vector2(0.5f, 0.5f);
            center.sizeDelta = new Vector2(500, 480);

            var vlg = center.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 15, 15);
            vlg.spacing = 6;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            string helpText =
                "=== 맵툴 도움말 (F1) ===\n\n" +
                "[도구 선택]\n" +
                "1 - 타일    2 - 벽     3 - 프롭\n" +
                "4 - 빌딩    5 - 오브젝트  6 - 지우개  7 - 이동\n\n" +
                "[배치/조작]\n" +
                "LMB - 배치/드래그    RMB - 지우기\n" +
                "Q/E - 회전 (15°)    Shift+Q/E - 미세 회전 (1°)\n" +
                "F - 프롭 좌우 반전(미러)\n" +
                "B - 프롭 벽 부착 모드 토글 (포스터/액자 등, 빌보드 끄고 벽면 고정)\n" +
                "접지 오프셋 XYZ — 방향키로 화면 이동 (배치 중 또는 이동(7)으로 집은 상태)\n" +
                "   ↳ ←/→=좌우(X)  ↑/↓=위아래(Y)  PageUp/Down=깊이(Z)  Home=리셋  (Shift: 미세)\n" +
                "   ↳ 벽 부착 상태에선 PageUp/Down이 부착 높이 조절\n" +
                "G - Snap/Free 전환\n" +
                "R - 리사이즈 모드    스크롤 - 크기 조절(R모드)\n" +
                "   ↳ 프롭·건물은 균등 배율, 벽은 이미지 비율(길이+높이) 배율\n" +
                "대괄호 [ 키 / ] 키 - 그림순서(앞뒤) 조절  ( [ =뒤 , ] =앞 )\n" +
                "   ↳ 프랍/빌딩 배치 중, 또는 이동(7)으로 집은 상태에서\n" +
                ", / . - 편집 층(level) 내리기/올리기 (1층=L0, Zomboid식 쌓기)\n" +
                "V - 층 컷어웨이 토글 (현재 층보다 위층 숨김)\n" +
                "Delete - 호버 대상 삭제\n\n" +
                "[이동 모드 (7)]\n" +
                "LMB - 집기/놓기    RMB/ESC - 취소\n" +
                "Q/E - 집은 상태에서 회전\n" +
                "접지 오프셋 XYZ — 방향키 ←/→=X ↑/↓=Y, PageUp/Down=Z(깊이), Home=리셋\n\n" +
                "[파일]\n" +
                "Ctrl+S - 저장    Ctrl+L - 불러오기\n" +
                "Ctrl+N - 새 맵   되돌리기 - 상단 [Undo] 버튼 (Ctrl+Z는 에디터 undo와 충돌해 제거)\n\n" +
                "[표시]\n" +
                "F1 - 이 도움말 토글\n" +
                "ESC - 배치 취소 / 리사이즈·이동 모드 종료";

            var txtGo = new GameObject("HelpText");
            txtGo.transform.SetParent(center, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            SetAnchors(txtRt, Vector2.zero, Vector2.one);
            txtRt.offsetMin = new Vector2(20, 15);
            txtRt.offsetMax = new Vector2(-20, -15);
            var txt = txtGo.AddComponent<Text>();
            txt.text = helpText;
            txt.font = DefaultFont;
            txt.fontSize = 13;
            txt.color = Color.white;
            txt.alignment = TextAnchor.UpperLeft;
            txt.lineSpacing = 1.2f;

            // Close hint at bottom
            var closeGo = new GameObject("CloseHint");
            closeGo.transform.SetParent(center, false);
            var closeRt = closeGo.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0, 0);
            closeRt.anchorMax = new Vector2(1, 0);
            closeRt.pivot = new Vector2(0.5f, 0);
            closeRt.offsetMin = new Vector2(0, 5);
            closeRt.offsetMax = new Vector2(0, 25);
            var closeTxt = closeGo.AddComponent<Text>();
            closeTxt.text = "F1 또는 아무 키를 눌러 닫기";
            closeTxt.font = DefaultFont;
            closeTxt.fontSize = 11;
            closeTxt.color = new Color(0.6f, 0.6f, 0.6f);
            closeTxt.alignment = TextAnchor.MiddleCenter;

            _helpPanel.gameObject.SetActive(false);
        }

        public void ToggleHelp()
        {
            if (_helpPanel == null) return;
            _helpPanel.gameObject.SetActive(!_helpPanel.gameObject.activeSelf);
        }

        /// <summary>도움말 패널이 현재 열려 있는지</summary>
        public bool IsHelpOpen => _helpPanel != null && _helpPanel.gameObject.activeSelf;

        public void CloseHelp()
        {
            if (_helpPanel != null) _helpPanel.gameObject.SetActive(false);
        }

        // ======== REFRESH ========

        public void RefreshAll()
        {
            RefreshToolbar();
            PopulatePalette();
            RefreshFilterToggles();
            RefreshSpawnConfig();
            PopulateBuildingList();
            RefreshStatus();
        }

        public void RefreshToolbar()
        {
            if (_toolButtons == null) return;
            ToolMode[] modes = { ToolMode.Tile, ToolMode.Wall, ToolMode.Prop, ToolMode.Building, ToolMode.MapObject, ToolMode.Eraser, ToolMode.Move };
            for (int i = 0; i < _toolButtons.Length && i < modes.Length; i++)
            {
                if (_toolButtons[i] == null) continue;
                var img = _toolButtons[i].GetComponent<Image>();
                img.color = _manager.CurrentTool == modes[i] ? BTN_ACTIVE : BTN_NORMAL;
            }

            PopulatePalette();
            RefreshSpawnConfig();
            RefreshFilterToggles();
            RefreshStatus();
        }

        public void RefreshStatus()
        {
            if (_rotationText != null)
            {
                float angle = _manager.CurrentRotation * 15f;
                // 정수면 소수점 생략, 아니면 1자리
                string rot = Mathf.Approximately(angle % 1f, 0f)
                    ? $"Rot: {(int)angle}°"
                    : $"Rot: {angle:F1}°";
                // 프롭 좌우 반전(F) 상태 표시
                if (_manager.CurrentFlipX) rot += "  ⇄Flip";
                // 벽 부착(B) 상태 + 높이 표시
                if (_manager.CurrentWallMount) rot += $"  ▣Wall h={_manager.CurrentMountHeight:F2}";
                // 현재 편집 층 표시 (, / . 로 변경). 1층 = L0
                rot += $"  ⌂L{_manager.CurrentLevel}";
                // 층 컷어웨이(V) 상태 표시
                if (_manager.LevelCutawayEnabled) rot += " ✂";
                // 프롭 배치 중이거나 Move로 집었으면 접지 오프셋(XYZ) 표시 (방향키/PageUp·Down/Home으로 조정)
                var off = _manager.ActiveGroundOffset;
                if (off.HasValue)
                    rot += $"  ⊹Off({off.Value.x:F2},{off.Value.y:F2},{off.Value.z:F2})";
                _rotationText.text = rot;
            }

            if (_snapButton != null)
            {
                var img = _snapButton.GetComponent<Image>();
                if (_manager.ResizeMode)
                    img.color = new Color(0.3f, 0.5f, 0.9f, 1f);
                else
                    img.color = _manager.SnapToGrid ? new Color(0.2f, 0.7f, 0.3f, 1f) : new Color(0.7f, 0.4f, 0.2f, 1f);

                // 항상 스냅 상태(Snap/Free)를 표시. (Move 모드여도 스냅 토글 버튼임)
                _snapText.text = _manager.ResizeMode ? "Resize"
                    : (_manager.SnapToGrid ? "Snap" : "Free");
            }

            // 브러시 크기 표시 (타일 모드에서만 노출)
            if (_brushGroup != null)
            {
                bool showBrush = _manager.CurrentTool == ToolMode.Tile;
                if (_brushGroup.gameObject.activeSelf != showBrush)
                    _brushGroup.gameObject.SetActive(showBrush);
                if (showBrush)
                {
                    if (_brushWText != null) _brushWText.text = _manager.BrushWidth.ToString();
                    if (_brushHText != null) _brushHText.text = _manager.BrushHeight.ToString();
                }
            }

            // 정렬 조절 UI: 프랍/건물을 가리켜 정렬 대상이 잡혔을 때만 노출
            if (_sortGroup != null)
            {
                bool showSort = _manager.HasSortTarget;
                if (_sortGroup.gameObject.activeSelf != showSort)
                    _sortGroup.gameObject.SetActive(showSort);
                if (showSort)
                {
                    if (_sortLabel != null) _sortLabel.text = _manager.SortTargetLabel ?? "정렬";
                    if (_sortValue != null) _sortValue.text = _manager.SortTargetOffset.ToString();
                }
            }
        }

        void Update()
        {
            if (_manager == null) return;
            RefreshStatus();
            UpdateHoverTooltip();
        }

        // ======== HELPERS ========

        static RectTransform CreatePanel(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
        }

        static Button CreateButton(RectTransform parent, string label, float width, Action onClick)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = BTN_NORMAL;

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 34;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = BTN_NORMAL;
            colors.highlightedColor = BTN_HOVER;
            colors.pressedColor = BTN_ACTIVE;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            SetAnchors(txtRt, Vector2.zero, Vector2.one);
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = DefaultFont;
            txt.fontSize = 14;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            return btn;
        }

        static Text CreateLabel(RectTransform parent, string text, float width)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = DefaultFont;
            txt.fontSize = 13;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            return txt;
        }

        static void CreateSpacer(RectTransform parent, float width)
        {
            var go = new GameObject("Spacer");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
        }

        static Text CreateTextObj(RectTransform parent, string name, string text, int fontSize, FontStyle style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = DefaultFont;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = fontSize + 10;

            return txt;
        }

        static RectTransform CreateRow(RectTransform parent, float height)
        {
            var go = new GameObject("Row");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;

            return rt;
        }

        static InputField CreateInputField(RectTransform parent, string placeholder)
        {
            var go = new GameObject("InputField");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.23f, 1f);

            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 28;

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            SetAnchors(txtRt, Vector2.zero, Vector2.one);
            txtRt.offsetMin = new Vector2(8, 2);
            txtRt.offsetMax = new Vector2(-8, -2);

            var txt = txtGo.AddComponent<Text>();
            txt.font = DefaultFont;
            txt.fontSize = 14;
            txt.color = Color.white;
            txt.supportRichText = false;

            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(go.transform, false);
            var phRt = phGo.AddComponent<RectTransform>();
            SetAnchors(phRt, Vector2.zero, Vector2.one);
            phRt.offsetMin = new Vector2(8, 2);
            phRt.offsetMax = new Vector2(-8, -2);

            var ph = phGo.AddComponent<Text>();
            ph.text = placeholder;
            ph.font = DefaultFont;
            ph.fontSize = 14;
            ph.fontStyle = FontStyle.Italic;
            ph.color = new Color(0.5f, 0.5f, 0.5f);

            var input = go.AddComponent<InputField>();
            input.textComponent = txt;
            input.placeholder = ph;

            return input;
        }
    }
}
