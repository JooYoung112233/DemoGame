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
            def.baseSprite = (Sprite)EditorGUILayout.ObjectField("Base Sprite", def.baseSprite, typeof(Sprite), false);
            def.roofSprite = (Sprite)EditorGUILayout.ObjectField("Roof Sprite", def.roofSprite, typeof(Sprite), false);
            def.footprint = EditorGUILayout.Vector2IntField("Footprint", def.footprint);
            def.sortingOffset = EditorGUILayout.IntField("Sorting Offset", def.sortingOffset);

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

            if (def.baseSprite != null)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                DrawPreview("Base", def.baseSprite);
                if (def.roofSprite != null)
                    DrawPreview("Roof", def.roofSprite);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Multi-tile: {def.IsMultiTile}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Cells: {def.footprint.x * def.footprint.y}", EditorStyles.miniLabel);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(target);
        }

        void DrawPreview(string label, Sprite sprite)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(label, EditorStyles.centeredGreyMiniLabel);
            Rect rect = GUILayoutUtility.GetRect(96, 64, GUILayout.ExpandWidth(false));
            Texture2D tex = sprite.texture;
            Rect texRect = sprite.textureRect;
            Rect uv = new(texRect.x / tex.width, texRect.y / tex.height,
                          texRect.width / tex.width, texRect.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv);
            EditorGUILayout.EndVertical();
        }
    }
}
