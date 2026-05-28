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

        // Snap label
        Text _snapText;

        // Hierarchy
        RectTransform _hierarchyPanel;
        RectTransform _hierarchyContent;
        ScrollRect _hierarchyScroll;

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
            BuildHierarchyPanel();
            BuildSaveLoadDialog();
            BuildNewMapDialog();
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

            string[] toolNames = { "1.Tile", "2.Wall", "3.Prop", "4.Building", "5.Object", "6.Eraser" };
            ToolMode[] modes = { ToolMode.Tile, ToolMode.Wall, ToolMode.Prop, ToolMode.Building, ToolMode.MapObject, ToolMode.Eraser };
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

            CreateSpacer(bar, 16);
            _rotationText = CreateLabel(bar, "Rot: N", 55);
            _snapText = CreateLabel(bar, "Free", 50);
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
                    AddPaletteHeader("Props (Free Place)");
                    foreach (var prop in _manager.catalog.props)
                    {
                        if (prop == null) continue;
                        var p = prop;
                        bool active = _manager.SelectedProp == p;
                        AddPaletteItem(prop.displayName ?? prop.propId, prop.icon, active, () => _manager.SelectProp(p));
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
                            AddPaletteItem(label, building.icon, active, () => _manager.SelectBuilding(b));
                        }
                    }
                    break;

                case ToolMode.MapObject:
                    AddPaletteHeader("Map Objects");
                    foreach (MapObjectType type in Enum.GetValues(typeof(MapObjectType)))
                    {
                        var t = type;
                        bool active = _manager.SelectedObjectType == t;
                        AddPaletteItem(type.ToString(), null, active, () => _manager.SelectObjectType(t));
                    }
                    break;

                case ToolMode.Eraser:
                    AddPaletteHeader("Eraser");
                    AddPaletteLabel("Right-click or\nLMB to erase at cursor");
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

        // ======== HIERARCHY PANEL ========

        void BuildHierarchyPanel()
        {
            _hierarchyPanel = CreatePanel(_root, "Hierarchy", PANEL_BG);
            SetAnchors(_hierarchyPanel, new Vector2(1, 0), new Vector2(1, 1));
            _hierarchyPanel.offsetMin = new Vector2(-200, 0);
            _hierarchyPanel.offsetMax = new Vector2(0, -48);

            var titleBar = CreatePanel(_hierarchyPanel, "HierarchyTitle", new Color(0.1f, 0.1f, 0.13f, 1f));
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
            titleLabel.text = "  Hierarchy";
            titleLabel.font = DefaultFont;
            titleLabel.fontSize = 14;
            titleLabel.color = Color.white;
            titleLabel.alignment = TextAnchor.MiddleLeft;

            var scrollArea = CreatePanel(_hierarchyPanel, "ScrollArea", Color.clear);
            SetAnchors(scrollArea, Vector2.zero, Vector2.one);
            scrollArea.offsetMin = new Vector2(5, 5);
            scrollArea.offsetMax = new Vector2(-5, -35);

            var scrollGo = scrollArea.gameObject;
            _hierarchyScroll = scrollGo.AddComponent<ScrollRect>();
            _hierarchyScroll.horizontal = false;
            _hierarchyScroll.vertical = true;
            scrollGo.AddComponent<RectMask2D>();

            _hierarchyContent = new GameObject("Content").AddComponent<RectTransform>();
            _hierarchyContent.SetParent(scrollArea, false);
            SetAnchors(_hierarchyContent, new Vector2(0, 1), new Vector2(1, 1));
            _hierarchyContent.pivot = new Vector2(0.5f, 1f);
            _hierarchyContent.offsetMin = Vector2.zero;
            _hierarchyContent.offsetMax = Vector2.zero;

            var vlg = _hierarchyContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(2, 2, 2, 2);
            vlg.spacing = 2;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = _hierarchyContent.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _hierarchyScroll.content = _hierarchyContent;
        }

        void PopulateHierarchy()
        {
            if (_hierarchyContent == null) return;

            foreach (Transform child in _hierarchyContent)
                Destroy(child.gameObject);

            if (_manager.EditingMap == null) return;

            // Buildings
            if (_manager.EditingMap.buildings.Count > 0)
            {
                AddHierarchyHeader("Buildings");
                foreach (var b in _manager.EditingMap.buildings)
                {
                    string label = b.buildingDefinition != null
                        ? b.buildingDefinition.displayName ?? b.buildingDefinitionId
                        : b.buildingDefinitionId;
                    AddHierarchyItem($"{label} [{b.instanceId}]", b.instanceId);
                }
            }

            // Props
            if (_manager.EditingMap.props.Count > 0)
            {
                AddHierarchyHeader("Props");
                foreach (var p in _manager.EditingMap.props)
                {
                    string label = p.propDefinition != null
                        ? p.propDefinition.displayName ?? p.propDefinitionId
                        : p.propDefinitionId;
                    AddHierarchyItem($"{label} [{p.instanceId}]", p.instanceId);
                }
            }

            // Map Objects
            if (_manager.EditingMap.mapObjects.Count > 0)
            {
                AddHierarchyHeader("Map Objects");
                foreach (var o in _manager.EditingMap.mapObjects)
                {
                    string label = string.IsNullOrEmpty(o.label) ? o.objectType.ToString() : o.label;
                    AddHierarchyItem($"{label} [{o.instanceId}]", o.instanceId);
                }
            }
        }

        void AddHierarchyHeader(string text)
        {
            var go = new GameObject("Header");
            go.transform.SetParent(_hierarchyContent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 22;

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = DefaultFont;
            txt.fontSize = 12;
            txt.fontStyle = FontStyle.Bold;
            txt.color = new Color(0.8f, 0.8f, 0.8f);
            txt.alignment = TextAnchor.MiddleLeft;
        }

        void AddHierarchyItem(string label, string instanceId)
        {
            var go = new GameObject(label);
            go.transform.SetParent(_hierarchyContent, false);

            var img = go.AddComponent<Image>();
            img.color = BTN_NORMAL;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 26;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 2, 1, 1);
            layout.spacing = 2;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // Clickable label (select + focus camera)
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelTxt = labelGo.AddComponent<Text>();
            labelTxt.text = label;
            labelTxt.font = DefaultFont;
            labelTxt.fontSize = 11;
            labelTxt.color = Color.white;
            labelTxt.alignment = TextAnchor.MiddleLeft;
            var labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1;

            // Add a transparent Image as raycast target for the button
            var labelImg = labelGo.AddComponent<Image>();
            labelImg.color = Color.clear;

            var labelBtn = labelGo.AddComponent<Button>();
            labelBtn.targetGraphic = labelImg;
            var labelColors = labelBtn.colors;
            labelColors.normalColor = Color.clear;
            labelColors.highlightedColor = new Color(1, 1, 1, 0.1f);
            labelColors.pressedColor = new Color(1, 1, 1, 0.2f);
            labelBtn.colors = labelColors;
            string selectId = instanceId;
            labelBtn.onClick.AddListener(() => _manager.SelectPlacedObject(selectId));

            // Delete button
            string deleteId = instanceId;
            var delBtn = CreateButton(go.GetComponent<RectTransform>(), "X", 20, () => _manager.DeletePlacedObject(deleteId));
            var delLe = delBtn.GetComponent<LayoutElement>();
            delLe.preferredWidth = 20;
            delLe.preferredHeight = 22;

            // Make delete button red-ish
            var delImg = delBtn.GetComponent<Image>();
            delImg.color = new Color(0.5f, 0.2f, 0.2f, 1f);
            var delColors = delBtn.colors;
            delColors.normalColor = new Color(0.5f, 0.2f, 0.2f, 1f);
            delColors.highlightedColor = new Color(0.7f, 0.25f, 0.25f, 1f);
            delColors.pressedColor = new Color(0.9f, 0.3f, 0.3f, 1f);
            delBtn.colors = delColors;
        }

        // ======== SAVE/LOAD DIALOG ========

        void BuildSaveLoadDialog()
        {
            _dialogPanel = CreatePanel(_root, "Dialog", new Color(0, 0, 0, 0.7f));
            SetAnchors(_dialogPanel, Vector2.zero, Vector2.one);
            _dialogPanel.offsetMin = Vector2.zero;
            _dialogPanel.offsetMax = Vector2.zero;

            var center = CreatePanel(_dialogPanel, "DialogCenter", PANEL_BG);
            center.anchorMin = new Vector2(0.3f, 0.2f);
            center.anchorMax = new Vector2(0.7f, 0.8f);
            center.offsetMin = Vector2.zero;
            center.offsetMax = Vector2.zero;

            var vlg = center.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(15, 15, 15, 15);
            vlg.spacing = 10;
            vlg.childForceExpandHeight = false;

            _dialogTitle = CreateTextObj(center, "Title", "Save Map", 18, FontStyle.Bold);
            var titleLe = _dialogTitle.gameObject.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 30;

            // Filename input
            var inputRow = CreateRow(center, 35);
            CreateTextObj(inputRow, "Label", "Filename:", 14, FontStyle.Normal);
            _dialogInput = CreateInputField(inputRow, "map_name");

            // File list (for load)
            var listArea = CreatePanel(center, "FileList", new Color(0.1f, 0.1f, 0.12f, 1f));
            var listLe = listArea.gameObject.AddComponent<LayoutElement>();
            listLe.flexibleHeight = 1;

            var listScroll = listArea.gameObject.AddComponent<ScrollRect>();
            listScroll.horizontal = false;
            listArea.gameObject.AddComponent<RectMask2D>();

            _fileListContent = new GameObject("Content").AddComponent<RectTransform>();
            _fileListContent.SetParent(listArea, false);
            SetAnchors(_fileListContent, new Vector2(0, 1), new Vector2(1, 1));
            _fileListContent.pivot = new Vector2(0.5f, 1f);

            var contentVlg = _fileListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            contentVlg.padding = new RectOffset(5, 5, 5, 5);
            contentVlg.spacing = 3;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;

            var contentCsf = _fileListContent.gameObject.AddComponent<ContentSizeFitter>();
            contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            listScroll.content = _fileListContent;

            // Buttons row
            var btnRow = CreateRow(center, 35);
            CreateButton(btnRow, "OK", 100, OnDialogOK);
            CreateButton(btnRow, "Cancel", 100, () => _dialogPanel.gameObject.SetActive(false));

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

            var btnRow = CreateRow(center, 35);
            CreateButton(btnRow, "Create", 100, () =>
            {
                int.TryParse(_widthInput.text, out int w);
                int.TryParse(_heightInput.text, out int h);
                w = Mathf.Clamp(w, 4, 512);
                h = Mathf.Clamp(h, 4, 512);
                _manager.NewMap(w, h);
                _newMapPanel.gameObject.SetActive(false);
            });
            CreateButton(btnRow, "Cancel", 100, () => _newMapPanel.gameObject.SetActive(false));

            _newMapPanel.gameObject.SetActive(false);
        }

        public void ShowNewMapDialog()
        {
            _widthInput.text = "32";
            _heightInput.text = "32";
            _newMapPanel.gameObject.SetActive(true);
        }

        // ======== REFRESH ========

        public void RefreshAll()
        {
            RefreshToolbar();
            PopulatePalette();
            PopulateHierarchy();
            RefreshStatus();
        }

        public void RefreshToolbar()
        {
            if (_toolButtons == null) return;
            ToolMode[] modes = { ToolMode.Tile, ToolMode.Wall, ToolMode.Prop, ToolMode.Building, ToolMode.MapObject, ToolMode.Eraser };
            for (int i = 0; i < _toolButtons.Length && i < modes.Length; i++)
            {
                if (_toolButtons[i] == null) continue;
                var img = _toolButtons[i].GetComponent<Image>();
                img.color = _manager.CurrentTool == modes[i] ? BTN_ACTIVE : BTN_NORMAL;
            }

            PopulatePalette();
            RefreshStatus();
        }

        public void RefreshStatus()
        {
            if (_rotationText != null)
            {
                string[] dirs = { "N", "E", "S", "W" };
                int rot = Mathf.Clamp(_manager.CurrentRotation, 0, 3);
                _rotationText.text = $"Rot: {dirs[rot]}";
            }

            if (_snapText != null)
            {
                _snapText.text = _manager.SnapToGrid ? "Snap" : "Free";
            }
        }

        void Update()
        {
            if (_manager == null) return;
            RefreshStatus();
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
