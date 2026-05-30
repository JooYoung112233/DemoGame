using System.Collections.Generic;
using UnityEngine;

namespace IsometricMapEditor
{
    public class TileRenderer : MonoBehaviour
    {
        /// <summary>지원하는 최대 층 인덱스 (0~MAX_LEVEL). MapBuilderManager.MAX_LEVEL과 일치.</summary>
        public const int MAX_LEVEL = 9;

        // 키 = (x, y, level). z 성분에 층 인덱스를 담아 같은 칸의 여러 층을 구분한다.
        readonly Dictionary<Vector3Int, GameObject> _tileObjects = new();
        readonly Queue<GameObject> _pool = new();

        static Vector3Int Key(Vector2Int pos, int level) => new(pos.x, pos.y, level);

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
            RemoveTileObject(tile.gridPosition, tile.level);
            // 벽은 3D 큐브(WallBuilder)로 그리므로 평면 스프라이트로 중복 렌더하지 않는다.
            if (tile.tileDefinition != null && tile.tileDefinition.sprite != null
                && !tile.tileDefinition.IsWall)
                RenderTile(tile, layer, settings);
        }

        public void RemoveTileObject(Vector2Int pos, int level = 0)
        {
            var key = Key(pos, level);
            if (!_tileObjects.TryGetValue(key, out var go)) return;
            go.SetActive(false);
            _pool.Enqueue(go);
            _tileObjects.Remove(key);
        }

        static void SetupBillboard(GameObject go)
        {
            // Rotate sprite to face isometric camera (lying on XZ plane tilted toward camera)
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
        }

        void RenderTile(PlacedTile tile, MapLayer layer, GridSettings settings)
        {
            var go = GetOrCreateTileObject(tile.gridPosition, tile.level);
            Vector3 worldPos = IsometricGrid.GridToWorld(tile.gridPosition, settings);
            // 바닥 타일 Y = 해당 층 바닥 높이 (0층=0). 벽 밑동도 같은 높이라 바닥에 딱 맞음.
            worldPos.y = tile.level * settings.levelHeight;
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
            _tileObjects[Key(tile.gridPosition, tile.level)] = go;
        }

        GameObject GetOrCreateTileObject(Vector2Int pos, int level)
        {
            var key = Key(pos, level);
            if (_tileObjects.TryGetValue(key, out var existing))
                return existing;

            if (_pool.Count > 0)
            {
                var pooled = _pool.Dequeue();
                pooled.name = $"Tile_{pos.x}_{pos.y}_L{level}";
                return pooled;
            }

            var go = new GameObject($"Tile_{pos.x}_{pos.y}_L{level}");
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Ground";
            return go;
        }

        /// <summary>
        /// 층 컷어웨이: currentLevel보다 위층의 바닥 타일을 숨겨 현재 편집 층이 잘 보이게 한다.
        /// enabled=false면 모든 층 타일을 다시 보이게 한다.
        /// </summary>
        public void ApplyLevelCutaway(int currentLevel, bool enabled)
        {
            foreach (var kvp in _tileObjects)
            {
                bool show = !enabled || kvp.Key.z <= currentLevel;
                if (kvp.Value != null && kvp.Value.activeSelf != show)
                    kvp.Value.SetActive(show);
            }
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
