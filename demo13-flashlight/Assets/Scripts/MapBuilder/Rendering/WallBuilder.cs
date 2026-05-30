using UnityEngine;

namespace IsometricMapEditor
{
    /// <summary>
    /// Builds wall cubes on the edges of a tile cell.
    /// rotation 0 = North (+Z), 1 = East (+X), 2 = South (-Z), 3 = West (-X)
    /// </summary>
    public static class WallBuilder
    {
        public static GameObject CreateWallCube(
            PlacedTile tile, GridSettings settings,
            Transform parent = null, bool editorPreview = false)
        {
            var def = tile.tileDefinition;
            if (def == null || !def.IsWall) return null;

            float height = def.wallHeight;
            float thickness = def.wallThickness;
            float tileSize = settings.tileSize;
            float halfTile = tileSize * 0.5f;

            Vector3 cellCenter = IsometricGrid.GridToWorld(tile.gridPosition, settings);

            // Offset wall to the edge of the cell based on rotation
            // 0=North(+Z), 1=East(+X), 2=South(-Z), 3=West(-X)
            Vector3 edgeOffset = tile.rotation switch
            {
                0 => new Vector3(0, 0, halfTile),    // North edge
                1 => new Vector3(halfTile, 0, 0),    // East edge
                2 => new Vector3(0, 0, -halfTile),   // South edge
                3 => new Vector3(-halfTile, 0, 0),   // West edge
                _ => Vector3.zero
            };

            Vector3 worldPos = cellCenter + edgeOffset;

            var go = new GameObject($"Wall_{tile.gridPosition.x}_{tile.gridPosition.y}_E{tile.rotation}");
            if (editorPreview)
                go.hideFlags = HideFlags.DontSave;
            if (parent != null)
                go.transform.SetParent(parent);

            go.transform.position = worldPos + new Vector3(0, height * 0.5f, 0);

            // Main cube
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (editorPreview)
                cube.hideFlags = HideFlags.DontSave;
            cube.transform.SetParent(go.transform, false);

            // North/South edges: wall extends along X axis
            // East/West edges: wall extends along Z axis
            Vector3 scale = (tile.rotation == 0 || tile.rotation == 2)
                ? new Vector3(tileSize, height, thickness)
                : new Vector3(thickness, height, tileSize);

            cube.transform.localScale = scale;

            // Apply material: per-instance override > definition material > sprite fallback
            var renderer = cube.GetComponent<Renderer>();
            Material mat = tile.EffectiveMaterial;

            if (mat != null)
            {
                renderer.sharedMaterial = mat;
            }
            else if (def.sprite != null)
            {
                var fallback = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (fallback.shader == null || fallback.shader.name == "Hidden/InternalErrorShader")
                    fallback = new Material(Shader.Find("Standard"));
                fallback.mainTexture = def.sprite.texture;
                if (fallback.HasProperty("_BaseMap"))
                    fallback.SetTexture("_BaseMap", def.sprite.texture);
                if (fallback.HasProperty("_BaseColor"))
                    fallback.SetColor("_BaseColor", Color.white);
                renderer.sharedMaterial = fallback;
                if (editorPreview)
                    fallback.hideFlags = HideFlags.DontSave;
            }

            // Remove collider in editor preview
            if (editorPreview)
            {
                var collider = cube.GetComponent<Collider>();
                if (collider != null) Object.DestroyImmediate(collider);
            }

            // 빛 차폐 그림자 프록시 (커스텀 ShadowBox가 있을 때)
            if (def.castsShadow && def.shadowBoxes != null && def.shadowBoxes.Length > 0)
                ShadowProxyBuilder.Build(def.shadowBoxes, go.transform, editorPreview);

            return go;
        }
    }
}
