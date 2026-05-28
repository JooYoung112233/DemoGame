using System.Collections.Generic;
using UnityEngine;

namespace IsometricMapEditor
{
    public class TileRenderer : MonoBehaviour
    {
        readonly Dictionary<Vector2Int, GameObject> _tileObjects = new();
        readonly Queue<GameObject> _pool = new();

        public void RenderMap(MapData mapData)
        {
            ClearAll();

            foreach (var layer in mapData.layers)
            {
                if (!layer.isVisible) continue;

                foreach (var tile in layer.tiles)
                {
                    if (tile.tileDefinition == null || tile.tileDefinition.sprite == null)
                        continue;

                    RenderTile(tile, layer, mapData.gridSettings);
                }
            }
        }

        public void RenderSingleTile(PlacedTile tile, MapLayer layer, GridSettings settings)
        {
            RemoveTileObject(tile.gridPosition);
            if (tile.tileDefinition != null && tile.tileDefinition.sprite != null)
                RenderTile(tile, layer, settings);
        }

        public void RemoveTileObject(Vector2Int pos)
        {
            if (!_tileObjects.TryGetValue(pos, out var go)) return;
            go.SetActive(false);
            _pool.Enqueue(go);
            _tileObjects.Remove(pos);
        }

        static void SetupBillboard(GameObject go)
        {
            // Rotate sprite to face isometric camera (lying on XZ plane tilted toward camera)
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
        }

        void RenderTile(PlacedTile tile, MapLayer layer, GridSettings settings)
        {
            var go = GetOrCreateTileObject(tile.gridPosition);
            Vector3 worldPos = IsometricGrid.GridToWorld(tile.gridPosition, settings);
            go.transform.position = worldPos;
            SetupBillboard(go);
            go.transform.localScale = IsometricGrid.GetTileScale(tile.tileDefinition.sprite, settings);

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = tile.tileDefinition.sprite;
            sr.flipX = tile.flipX;
            sr.sortingOrder = IsometricGrid.GetSortingOrder(tile.gridPosition, layer.sortingLayerOffset)
                              + tile.tileDefinition.sortingOffset;

            // Apply material (per-instance override > definition default)
            var mat = tile.EffectiveMaterial;
            if (mat != null)
                sr.sharedMaterial = mat;

            go.SetActive(true);
            _tileObjects[tile.gridPosition] = go;
        }

        GameObject GetOrCreateTileObject(Vector2Int pos)
        {
            if (_tileObjects.TryGetValue(pos, out var existing))
                return existing;

            if (_pool.Count > 0)
            {
                var pooled = _pool.Dequeue();
                pooled.name = $"Tile_{pos.x}_{pos.y}";
                return pooled;
            }

            var go = new GameObject($"Tile_{pos.x}_{pos.y}");
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Ground";
            return go;
        }

        public void ClearAll()
        {
            foreach (var kvp in _tileObjects)
            {
                kvp.Value.SetActive(false);
                _pool.Enqueue(kvp.Value);
            }
            _tileObjects.Clear();
        }
    }
}
