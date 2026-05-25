using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public class ConnectionEditorWindow : EditorWindow
    {
        MapData mapData;
        Vector2 scrollPos;
        Vector2 graphOffset;

        [MenuItem("Window/Isometric Map/Connection Editor")]
        public static void ShowWindow()
        {
            GetWindow<ConnectionEditorWindow>("Connection Editor");
        }

        void OnGUI()
        {
            mapData = (MapData)EditorGUILayout.ObjectField(
                "Map Data", mapData, typeof(MapData), false);

            if (mapData == null)
            {
                EditorGUILayout.HelpBox("Select a MapData to view connections.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Interior Connections", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            for (int i = 0; i < mapData.interiorConnections.Count; i++)
            {
                var conn = mapData.interiorConnections[i];
                EditorGUILayout.BeginVertical("box");
                conn.connectionId = EditorGUILayout.TextField("ID", conn.connectionId);
                conn.label = EditorGUILayout.TextField("Label", conn.label);
                conn.type = (InteriorConnectionType)EditorGUILayout.EnumPopup("Type", conn.type);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("From", EditorStyles.boldLabel);
                conn.fromMapId = EditorGUILayout.TextField("Map ID", conn.fromMapId);
                conn.fromConnectionPointId = EditorGUILayout.TextField("Point ID", conn.fromConnectionPointId);
                EditorGUILayout.EndVertical();

                EditorGUILayout.LabelField("↔", GUILayout.Width(20));

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("To", EditorStyles.boldLabel);
                conn.toMapId = EditorGUILayout.TextField("Map ID", conn.toMapId);
                conn.toConnectionPointId = EditorGUILayout.TextField("Point ID", conn.toConnectionPointId);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Remove"))
                {
                    Undo.RecordObject(mapData, "Remove Connection");
                    mapData.interiorConnections.RemoveAt(i);
                    EditorUtility.SetDirty(mapData);
                    break;
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Add Connection"))
            {
                Undo.RecordObject(mapData, "Add Connection");
                mapData.interiorConnections.Add(new InteriorConnection
                {
                    connectionId = System.Guid.NewGuid().ToString("N")[..8]
                });
                EditorUtility.SetDirty(mapData);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Enterable Buildings", EditorStyles.boldLabel);
            foreach (var building in mapData.buildings)
            {
                if (building.buildingDefinition == null || !building.buildingDefinition.isEnterable) continue;
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(building.buildingDefinition.displayName);
                EditorGUILayout.LabelField($"→ {building.buildingDefinition.interiorMapId}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (GUI.changed)
                EditorUtility.SetDirty(mapData);
        }
    }
}
