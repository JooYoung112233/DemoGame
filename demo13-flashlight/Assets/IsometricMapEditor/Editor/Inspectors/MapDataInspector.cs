using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    [CustomEditor(typeof(MapData))]
    public class MapDataInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var mapData = (MapData)target;

            EditorGUILayout.LabelField("Map Info", EditorStyles.boldLabel);
            mapData.mapName = EditorGUILayout.TextField("Name", mapData.mapName);
            EditorGUILayout.LabelField("ID", mapData.mapId);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            mapData.gridSettings.tileSize = EditorGUILayout.Slider("Tile Size", mapData.gridSettings.tileSize, 0.1f, 10f);
            mapData.gridSettings.mapWidth = EditorGUILayout.IntSlider("Map Width", mapData.gridSettings.mapWidth, 4, 256);
            mapData.gridSettings.mapHeight = EditorGUILayout.IntSlider("Map Height", mapData.gridSettings.mapHeight, 4, 256);
            mapData.gridSettings.originOffset = EditorGUILayout.Vector3Field("Origin Offset", mapData.gridSettings.originOffset);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(target);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
            foreach (var layer in mapData.layers)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(layer.layerName, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{layer.tiles.Count} tiles", EditorStyles.miniLabel);
                layer.isVisible = EditorGUILayout.Toggle("Visible", layer.isVisible, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }

            int totalTiles = 0;
            foreach (var l in mapData.layers) totalTiles += l.tiles.Count;
            EditorGUILayout.LabelField($"Total: {totalTiles} tiles across {mapData.layers.Count} layers", EditorStyles.miniLabel);

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Open in Map Editor"))
                MapEditorWindow.ShowWindow();
        }
    }
}
