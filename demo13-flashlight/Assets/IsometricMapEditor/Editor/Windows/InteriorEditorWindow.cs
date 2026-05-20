using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public class InteriorEditorWindow : EditorWindow
    {
        InteriorMapData interiorData;
        Vector2 scrollPos;
        int selectedTab;
        readonly string[] tabs = { "Layout", "Entry Points", "Escape Points", "Props", "Walkability" };

        [MenuItem("Window/Isometric Map/Interior Editor")]
        public static void ShowWindow()
        {
            GetWindow<InteriorEditorWindow>("Interior Editor");
        }

        public static void Open(InteriorMapData data)
        {
            var window = GetWindow<InteriorEditorWindow>("Interior Editor");
            window.interiorData = data;
        }

        void OnGUI()
        {
            interiorData = (InteriorMapData)EditorGUILayout.ObjectField(
                "Interior Map", interiorData, typeof(InteriorMapData), false);

            if (interiorData == null)
            {
                EditorGUILayout.HelpBox("Select an InteriorMapData to edit.", MessageType.Info);
                return;
            }

            selectedTab = GUILayout.Toolbar(selectedTab, tabs);
            EditorGUILayout.Space(4);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            switch (selectedTab)
            {
                case 0: DrawLayoutTab(); break;
                case 1: DrawEntryPointsTab(); break;
                case 2: DrawEscapePointsTab(); break;
                case 3: DrawPropsTab(); break;
                case 4: DrawWalkabilityTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawLayoutTab()
        {
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
            interiorData.gridSettings.tileSize = EditorGUILayout.FloatField("Tile Size", interiorData.gridSettings.tileSize);
            interiorData.gridSettings.mapWidth = EditorGUILayout.IntField("Map Width", interiorData.gridSettings.mapWidth);
            interiorData.gridSettings.mapHeight = EditorGUILayout.IntField("Map Height", interiorData.gridSettings.mapHeight);
            interiorData.gridSettings.originOffset = EditorGUILayout.Vector3Field("Origin Offset", interiorData.gridSettings.originOffset);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"Layers: {interiorData.layers.Count}", EditorStyles.boldLabel);
            foreach (var layer in interiorData.layers)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(layer.layerName);
                EditorGUILayout.LabelField($"{layer.tiles.Count} tiles", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawEntryPointsTab()
        {
            EditorGUILayout.LabelField("Entry/Exit Points", EditorStyles.boldLabel);

            for (int i = 0; i < interiorData.entryPoints.Count; i++)
            {
                var cp = interiorData.entryPoints[i];
                EditorGUILayout.BeginVertical("box");
                cp.connectionId = EditorGUILayout.TextField("ID", cp.connectionId);
                cp.label = EditorGUILayout.TextField("Label", cp.label);
                cp.gridPosition = EditorGUILayout.Vector2IntField("Grid Position", cp.gridPosition);
                cp.direction = (ConnectionDirection)EditorGUILayout.EnumPopup("Direction", cp.direction);
                cp.targetMapId = EditorGUILayout.TextField("Target Map ID", cp.targetMapId);
                cp.targetConnectionId = EditorGUILayout.TextField("Target Connection", cp.targetConnectionId);

                if (GUILayout.Button("Remove"))
                {
                    Undo.RecordObject(interiorData, "Remove Entry Point");
                    interiorData.entryPoints.RemoveAt(i);
                    EditorUtility.SetDirty(interiorData);
                    break;
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Add Entry Point"))
            {
                Undo.RecordObject(interiorData, "Add Entry Point");
                interiorData.entryPoints.Add(new ConnectionPoint
                {
                    connectionId = System.Guid.NewGuid().ToString("N")[..8]
                });
                EditorUtility.SetDirty(interiorData);
            }
        }

        void DrawEscapePointsTab()
        {
            EditorGUILayout.LabelField("Escape Points", EditorStyles.boldLabel);

            for (int i = 0; i < interiorData.escapePoints.Count; i++)
            {
                var ep = interiorData.escapePoints[i];
                EditorGUILayout.BeginVertical("box");
                ep.escapeId = EditorGUILayout.TextField("ID", ep.escapeId);
                ep.label = EditorGUILayout.TextField("Label", ep.label);
                ep.gridPosition = EditorGUILayout.Vector2IntField("Position", ep.gridPosition);
                ep.type = (EscapeType)EditorGUILayout.EnumPopup("Type", ep.type);
                ep.targetMapId = EditorGUILayout.TextField("Target Map", ep.targetMapId);
                ep.requiresKey = EditorGUILayout.Toggle("Requires Key", ep.requiresKey);
                if (ep.requiresKey)
                    ep.requiredKeyItemId = EditorGUILayout.TextField("Key Item ID", ep.requiredKeyItemId);
                ep.isHidden = EditorGUILayout.Toggle("Hidden", ep.isHidden);

                if (GUILayout.Button("Remove"))
                {
                    Undo.RecordObject(interiorData, "Remove Escape Point");
                    interiorData.escapePoints.RemoveAt(i);
                    EditorUtility.SetDirty(interiorData);
                    break;
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Add Escape Point"))
            {
                Undo.RecordObject(interiorData, "Add Escape Point");
                interiorData.escapePoints.Add(new EscapePoint
                {
                    escapeId = System.Guid.NewGuid().ToString("N")[..8]
                });
                EditorUtility.SetDirty(interiorData);
            }
        }

        void DrawPropsTab()
        {
            EditorGUILayout.LabelField($"Props: {interiorData.props.Count}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Harvestables: {interiorData.harvestables.Count}");
            EditorGUILayout.HelpBox("Use Scene View tools to place props and harvestables.", MessageType.Info);
        }

        void DrawWalkabilityTab()
        {
            if (interiorData.walkability == null)
            {
                if (GUILayout.Button("Initialize Walkability"))
                {
                    Undo.RecordObject(interiorData, "Init Walkability");
                    interiorData.InitializeWalkability();
                    EditorUtility.SetDirty(interiorData);
                }
                return;
            }

            EditorGUILayout.LabelField($"Size: {interiorData.walkability.width}x{interiorData.walkability.height}");
            EditorGUILayout.HelpBox("Use Scene View Walkability Paint Tool to edit.", MessageType.Info);
        }
    }
}
