using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    [CustomEditor(typeof(TileDefinition))]
    public class TileDefinitionInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var tileDef = (TileDefinition)target;

            EditorGUI.BeginChangeCheck();

            tileDef.tileId = EditorGUILayout.TextField("Tile ID", tileDef.tileId);
            tileDef.sprite = (Sprite)EditorGUILayout.ObjectField("Sprite", tileDef.sprite, typeof(Sprite), false);
            tileDef.category = (TileCategory)EditorGUILayout.EnumPopup("Category", tileDef.category);
            tileDef.isWalkable = EditorGUILayout.Toggle("Walkable", tileDef.isWalkable);
            tileDef.size = EditorGUILayout.Vector2IntField("Size (cells)", tileDef.size);
            tileDef.sortingOffset = EditorGUILayout.IntField("Sorting Offset", tileDef.sortingOffset);

            if (tileDef.sprite != null)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                Rect previewRect = GUILayoutUtility.GetRect(128, 64, GUILayout.ExpandWidth(false));

                Texture2D tex = tileDef.sprite.texture;
                Rect texRect = tileDef.sprite.textureRect;
                Rect uv = new(
                    texRect.x / tex.width, texRect.y / tex.height,
                    texRect.width / tex.width, texRect.height / tex.height
                );
                GUI.DrawTextureWithTexCoords(previewRect, tex, uv);
            }

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(target);
        }
    }
}
