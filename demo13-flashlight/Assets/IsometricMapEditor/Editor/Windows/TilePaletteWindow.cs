using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    [System.Obsolete("Use MapEditorWindow instead. Palette is now integrated.")]
    public class TilePaletteWindow : EditorWindow
    {
        public static TileDefinition SelectedTile => MapEditorWindow.SelectedBrush;
        public static BuildingDefinition SelectedBuilding => MapEditorWindow.SelectedBuilding;
        public static PropDefinition SelectedProp => MapEditorWindow.SelectedProp;

        [MenuItem("Window/Isometric Map/Tile Palette (Legacy)")]
        public static void ShowWindow()
        {
            MapEditorWindow.ShowWindow();
        }
    }
}
