using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public class BuildingVariantEditorWindow : EditorWindow
    {
        BuildingVariantSet variantSet;
        Vector2 scrollPos;
        int selectedVariantIndex = -1;

        [MenuItem("Window/Isometric Map/Variant Editor")]
        public static void ShowWindow()
        {
            GetWindow<BuildingVariantEditorWindow>("Variant Editor");
        }

        public static void Open(BuildingVariantSet set)
        {
            var window = GetWindow<BuildingVariantEditorWindow>("Variant Editor");
            window.variantSet = set;
        }

        void OnGUI()
        {
            variantSet = (BuildingVariantSet)EditorGUILayout.ObjectField(
                "Variant Set", variantSet, typeof(BuildingVariantSet), false);

            if (variantSet == null)
            {
                EditorGUILayout.HelpBox("Select a BuildingVariantSet to edit.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4);
            variantSet.defaultVariantId = EditorGUILayout.TextField("Default Variant", variantSet.defaultVariantId);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Variants", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            for (int i = 0; i < variantSet.variants.Count; i++)
            {
                var variant = variantSet.variants[i];
                bool isSelected = i == selectedVariantIndex;

                EditorGUILayout.BeginVertical(isSelected ? "selectionRect" : "box");

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(variant.variantId, EditorStyles.boldLabel))
                    selectedVariantIndex = isSelected ? -1 : i;
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    Undo.RecordObject(variantSet, "Remove Variant");
                    variantSet.variants.RemoveAt(i);
                    EditorUtility.SetDirty(variantSet);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                if (isSelected)
                {
                    EditorGUI.indentLevel++;
                    variant.variantId = EditorGUILayout.TextField("ID", variant.variantId);
                    variant.displayName = EditorGUILayout.TextField("Display Name", variant.displayName);
                    variant.baseSprite = (Sprite)EditorGUILayout.ObjectField(
                        "Base Sprite", variant.baseSprite, typeof(Sprite), false);
                    variant.roofSprite = (Sprite)EditorGUILayout.ObjectField(
                        "Roof Sprite", variant.roofSprite, typeof(Sprite), false);
                    variant.tintColor = EditorGUILayout.ColorField("Tint", variant.tintColor);
                    variant.transitionEffect = (TransitionEffect)EditorGUILayout.EnumPopup(
                        "Transition", variant.transitionEffect);
                    variant.transitionDuration = EditorGUILayout.FloatField(
                        "Duration", variant.transitionDuration);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Add Variant"))
            {
                Undo.RecordObject(variantSet, "Add Variant");
                variantSet.variants.Add(new BuildingVariant
                {
                    variantId = $"variant_{variantSet.variants.Count}",
                    displayName = "New Variant"
                });
                EditorUtility.SetDirty(variantSet);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Transition Rules", EditorStyles.boldLabel);

            for (int i = 0; i < variantSet.transitionRules.Count; i++)
            {
                var rule = variantSet.transitionRules[i];
                EditorGUILayout.BeginHorizontal("box");
                rule.fromVariantId = EditorGUILayout.TextField(rule.fromVariantId, GUILayout.Width(80));
                EditorGUILayout.LabelField("→", GUILayout.Width(20));
                rule.toVariantId = EditorGUILayout.TextField(rule.toVariantId, GUILayout.Width(80));
                rule.conditionType = (TransitionConditionType)EditorGUILayout.EnumPopup(rule.conditionType, GUILayout.Width(100));
                rule.conditionParam = EditorGUILayout.TextField(rule.conditionParam);
                rule.autoTransition = EditorGUILayout.Toggle(rule.autoTransition, GUILayout.Width(20));
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    Undo.RecordObject(variantSet, "Remove Rule");
                    variantSet.transitionRules.RemoveAt(i);
                    EditorUtility.SetDirty(variantSet);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Add Rule"))
            {
                Undo.RecordObject(variantSet, "Add Rule");
                variantSet.transitionRules.Add(new VariantTransitionRule());
                EditorUtility.SetDirty(variantSet);
            }

            EditorGUILayout.EndScrollView();

            if (GUI.changed)
                EditorUtility.SetDirty(variantSet);
        }
    }
}
