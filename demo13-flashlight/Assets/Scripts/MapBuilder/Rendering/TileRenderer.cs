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
                    // 벽은 3D 큐브로만 그린다 (평면 스프라이트 중복 방지)
                    if (tile.tileDefinition.IsWall)
                        continue;

                    RenderTile(tile, layer, mapData.gridSettings);
                }
            }
        }

        public void RenderSingleTile(PlacedTile tile, MapLayer layer, GridSettings settings)
        {
            RemoveTileObject(tile.gridPosition);
            // 벽은 3D 큐브(WallBuilder)로 그리므로 평면 스프라이트로 중복 렌더하지 않는다.
            if (tile.tileDefinition != null && tile.tileDefinition.sprite != null
                && !tile.tileDefinition.IsWall)
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
            // 바닥을 Y=0 보다 살짝 아래로 내려 3D 벽(밑동 Y=0)이 깊이 테스트에서
            // 항상 이기도록 한다. (스프라이트 ZTest LEqual + 벽이 더 가까움 → 벽 우선)
            worldPos.y -= 0.05f;
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
