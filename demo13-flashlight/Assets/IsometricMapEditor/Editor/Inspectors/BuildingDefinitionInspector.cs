using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    [CustomEditor(typeof(BuildingDefinition))]
    public class BuildingDefinitionInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var def = (BuildingDefinition)target;

            EditorGUI.BeginChangeCheck();

            def.buildingId = EditorGUILayout.TextField("Building ID", def.buildingId);
            def.displayName = EditorGUILayout.TextField("Display Name", def.displayName);
            def.footprint = EditorGUILayout.Vector2IntField("Footprint", def.footprint);
            def.sortingOffset = EditorGUILayout.IntField("Sorting Offset", def.sortingOffset);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Prefab", EditorStyles.boldLabel);
            def.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", def.prefab, typeof(GameObject), false);
            def.icon = (Sprite)EditorGUILayout.ObjectField("Icon", def.icon, typeof(Sprite), false);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Interior", EditorStyles.boldLabel);
            def.isEnterable = EditorGUILayout.Toggle("Is Enterable", def.isEnterable);
            if (def.isEnterable)
            {
                def.interiorMapId = EditorGUILayout.TextField("Interior Map ID", def.interiorMapId);
                def.entryCell = EditorGUILayout.Vector2IntField("Entry Cell", def.entryCell);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("State Variants", EditorStyles.boldLabel);
            def.variantSet = (BuildingVariantSet)EditorGUILayout.ObjectField(
                "Variant Set", def.variantSet, typeof(BuildingVariantSet), false);

            if (def.variantSet != null && GUILayout.Button("Open Variant Editor"))
                BuildingVariantEditorWindow.Open(def.variantSet);

            // Prefab preview
            if (def.prefab != null)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Prefab Preview", EditorStyles.boldLabel);
                var preview = AssetPreview.GetAssetPreview(def.prefab);
                if (preview != null)
                {
                    Rect rect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(false));
                    GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
                }
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Multi-tile: {def.IsMultiTile}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Cells: {def.footprint.x * def.footprint.y}", EditorStyles.miniLabel);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(target);
        }
    }
}
