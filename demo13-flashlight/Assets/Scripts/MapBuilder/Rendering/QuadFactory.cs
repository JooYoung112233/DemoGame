using UnityEngine;

namespace IsometricMapEditor
{
    /// <summary>
    /// Factory for creating 3D Quad meshes (MeshRenderer) from sprites.
    /// All objects use depth buffer instead of sorting order for proper 3D rendering.
    /// </summary>
    public static class QuadFactory
    {
        /// <summary>
        /// Create a floor tile as a horizontal Quad lying on XZ plane.
        /// </summary>
        public static GameObject CreateFloorQuad(string name, Vector3 worldPos, PlacedTile tile,
            GridSettings settings, Transform parent, bool editorPreview)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            if (editorPreview)
                go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(parent);

            go.transform.position = worldPos + new Vector3(0, 0.001f, 0);
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
            go.transform.localScale = new Vector3(
                tile.flipX ? -settings.tileSize : settings.tileSize,
                settings.tileSize, 1f);

            if (editorPreview)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sortingLayerName = "Ground";
            ApplyMaterial(renderer, tile.tileDefinition.sprite, tile.EffectiveMaterial, false, editorPreview);
            return go;
        }

        /// <summary>
        /// Create a vertical Quad for buildings/props standing upright.
        /// </summary>
        public static GameObject CreateVerticalQuad(string name, Vector3 worldPos, Sprite sprite,
            Vector2 size, float yRotation, Material overrideMat, Transform parent, bool editorPreview)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            if (editorPreview)
                go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(parent);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0, yRotation, 0);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            if (editorPreview)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }

            var renderer = go.GetComponent<MeshRenderer>();
            ApplyMaterial(renderer, sprite, overrideMat, true, editorPreview);
            return go;
        }

        static void ApplyMaterial(MeshRenderer renderer, Sprite sprite, Material overrideMat,
            bool needsAlphaClip, bool editorPreview)
        {
            if (sprite == null && overrideMat != null)
            {
                renderer.sharedMaterial = overrideMat;
                return;
            }
            if (sprite == null) return;

            Material mat;
            if (overrideMat != null)
            {
                // Bake: 유저가 설정한 머티리얼을 그대로 사용 (셰이더 설정 보존)
                // Editor preview: 에셋 원본 보호를 위해 복사본 사용
                if (!editorPreview)
                {
                    renderer.sharedMaterial = overrideMat;
                    return;
                }
                mat = new Material(overrideMat);
            }
            else if (needsAlphaClip)
            {
                // Vertical quads need alpha cutout so transparent parts aren't visible
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                    mat = new Material(Shader.Find("Unlit/Transparent Cutout"));

                if (mat.HasProperty("_Cutoff"))
                    mat.SetFloat("_Cutoff", 0.5f);
                if (mat.HasProperty("_Surface"))
                {
                    mat.SetFloat("_Surface", 0);
                    mat.SetFloat("_AlphaClip", 1);
                }
            }
            else
            {
                // Floor quads: standard lit material
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                    mat = new Material(Shader.Find("Standard"));
            }

            mat.mainTexture = sprite.texture;

            // URP shaders use _BaseMap instead of _MainTex
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", sprite.texture);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);

            // Handle sprite atlas sub-regions
            var rect = sprite.textureRect;
            float texW = sprite.texture.width;
            float texH = sprite.texture.height;
            var offset = new Vector2(rect.x / texW, rect.y / texH);
            var scale = new Vector2(rect.width / texW, rect.height / texH);
            mat.mainTextureOffset = offset;
            mat.mainTextureScale = scale;
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTextureOffset("_BaseMap", offset);
                mat.SetTextureScale("_BaseMap", scale);
            }

            if (editorPreview)
                mat.hideFlags = HideFlags.DontSave;

            renderer.sharedMaterial = mat;
        }
    }
}
