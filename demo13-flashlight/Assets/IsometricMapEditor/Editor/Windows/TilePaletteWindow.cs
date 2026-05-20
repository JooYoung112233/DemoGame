using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public class TilePaletteWindow : EditorWindow
    {
        Vector2 scrollPos;
        string searchFilter = "";
        TileCategory categoryFilter = (TileCategory)(-1);
        static TileDefinition selectedTile;
        static BuildingDefinition selectedBuilding;
        static PropDefinition selectedProp;

        public static TileDefinition SelectedTile => selectedTile;
        public static BuildingDefinition SelectedBuilding => selectedBuilding;
        public static PropDefinition SelectedProp => selectedProp;

        public enum PaletteMode { Tiles, Buildings, Props }
        PaletteMode mode = PaletteMode.Tiles;

        [MenuItem("Window/Isometric Map/Tile Palette")]
        public static void ShowWindow()
        {
            GetWindow<TilePaletteWindow>("Tile Palette");
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(mode == PaletteMode.Tiles, "Tiles", EditorStyles.toolbarButton))
                mode = PaletteMode.Tiles;
            if (GUILayout.Toggle(mode == PaletteMode.Buildings, "Buildings", EditorStyles.toolbarButton))
                mode = PaletteMode.Buildings;
            if (GUILayout.Toggle(mode == PaletteMode.Props, "Props", EditorStyles.toolbarButton))
                mode = PaletteMode.Props;
            EditorGUILayout.EndHorizontal();

            searchFilter = EditorGUILayout.TextField("Search", searchFilter);

            if (mode == PaletteMode.Tiles)
                categoryFilter = (TileCategory)EditorGUILayout.EnumFlagsField("Category", categoryFilter);

            EditorGUILayout.Space(4);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            switch (mode)
            {
                case PaletteMode.Tiles: DrawTilePalette(); break;
                case PaletteMode.Buildings: DrawBuildingPalette(); break;
                case PaletteMode.Props: DrawPropPalette(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawTilePalette()
        {
            string[] guids = AssetDatabase.FindAssets("t:TileDefinition");
            int columns = Mathf.Max(1, (int)(position.width / 80));
            int col = 0;

            EditorGUILayout.BeginHorizontal();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var tile = AssetDatabase.LoadAssetAtPath<TileDefinition>(path);
                if (tile == null) continue;
                if (!MatchesFilter(tile.tileId, tile.category)) continue;

                bool isSelected = selectedTile == tile;
                var style = isSelected ? "selectionRect" : "button";

                if (GUILayout.Button(GUIContent.none, style, GUILayout.Width(72), GUILayout.Height(72)))
                {
                    selectedTile = tile;
                    selectedBuilding = null;
                    selectedProp = null;
                }

                var rect = GUILayoutUtility.GetLastRect();
                if (tile.sprite != null)
                    DrawSpritePreview(rect, tile.sprite);
                else
                    EditorGUI.LabelField(rect, tile.tileId, EditorStyles.centeredGreyMiniLabel);

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
            string[] guids = AssetDatabase.FindAssets("t:BuildingDefinition");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var building = AssetDatabase.LoadAssetAtPath<BuildingDefinition>(path);
                if (building == null) continue;
                if (!string.IsNullOrEmpty(searchFilter) &&
                    !building.displayName.ToLower().Contains(searchFilter.ToLower())) continue;

                bool isSelected = selectedBuilding == building;
                EditorGUILayout.BeginHorizontal(isSelected ? "selectionRect" : "box");

                if (building.baseSprite != null)
                {
                    var rect = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48));
                    DrawSpritePreview(rect, building.baseSprite);
                }

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(building.displayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"{building.footprint.x}x{building.footprint.y}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    selectedBuilding = building;
                    selectedTile = null;
                    selectedProp = null;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawPropPalette()
        {
            string[] guids = AssetDatabase.FindAssets("t:PropDefinition");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prop = AssetDatabase.LoadAssetAtPath<PropDefinition>(path);
                if (prop == null) continue;
                if (!string.IsNullOrEmpty(searchFilter) &&
                    !prop.displayName.ToLower().Contains(searchFilter.ToLower())) continue;

                bool isSelected = selectedProp == prop;
                EditorGUILayout.BeginHorizontal(isSelected ? "selectionRect" : "box");

                if (prop.sprite != null)
                {
                    var rect = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48));
                    DrawSpritePreview(rect, prop.sprite);
                }

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(prop.displayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"{prop.footprint.x}x{prop.footprint.y}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    selectedProp = prop;
                    selectedTile = null;
                    selectedBuilding = null;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        bool MatchesFilter(string id, TileCategory cat)
        {
            if (!string.IsNullOrEmpty(searchFilter) && !id.ToLower().Contains(searchFilter.ToLower()))
                return false;
            if ((int)categoryFilter != -1 && (categoryFilter & cat) == 0)
                return false;
            return true;
        }

        static void DrawSpritePreview(Rect rect, Sprite sprite)
        {
            Texture2D tex = sprite.texture;
            Rect texRect = sprite.textureRect;
            Rect uv = new(texRect.x / tex.width, texRect.y / tex.height,
                          texRect.width / tex.width, texRect.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv);
        }
    }
}
