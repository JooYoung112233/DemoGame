using System.Collections.Generic;
using UnityEngine;

namespace TopDownMapEditor
{
    [CreateAssetMenu(menuName = "Top-Down Map/Map Data")]
    public class MapData : ScriptableObject
    {
        public string mapName;
        public string mapId;
        public GridSettings gridSettings = new();
        public List<MapLayer> layers = new();
        public List<PlacedBuilding> buildings = new();
        public List<PlacedProp> props = new();
        public List<PlacedHarvestable> harvestables = new();
        public List<PlacedMapObject> mapObjects = new();
        public List<InteriorConnection> interiorConnections = new();
        public WalkabilityData walkability;

        public void InitializeWalkability()
        {
            walkability = new WalkabilityData(gridSettings.mapWidth, gridSettings.mapHeight);
        }

        // 키 = (x, y, level). z 성분에 층 인덱스를 담아 같은 칸의 여러 층을 구분한다.
        readonly Dictionary<Vector3Int, PlacedTile> _occupancyCache = new();
        bool _cacheDirty = true;

        static Vector3Int CacheKey(Vector2Int pos, int level) => new(pos.x, pos.y, level);

        void OnEnable() => _cacheDirty = true;

        public void MarkDirty() => _cacheDirty = true;

        void RebuildCache()
        {
            _occupancyCache.Clear();
            foreach (var layer in layers)
                foreach (var tile in layer.tiles)
                    _occupancyCache[CacheKey(tile.gridPosition, tile.level)] = tile;
            _cacheDirty = false;
        }

        public PlacedTile GetTileAt(Vector2Int pos, int level = 0)
        {
            if (_cacheDirty) RebuildCache();
            return _occupancyCache.GetValueOrDefault(CacheKey(pos, level));
        }

        public bool IsCellOccupied(Vector2Int pos, int level = 0)
        {
            if (_cacheDirty) RebuildCache();
            return _occupancyCache.ContainsKey(CacheKey(pos, level));
        }

        public MapLayer GetOrCreateLayer(string layerName, int sortingOffset = 0)
        {
            var layer = layers.Find(l => l.layerName == layerName);
            if (layer != null) return layer;

            layer = new MapLayer
            {
                layerName = layerName,
                sortingLayerOffset = sortingOffset
            };
            layers.Add(layer);
            _cacheDirty = true;
            return layer;
        }

        public void PlaceTile(PlacedTile tile, string layerName)
        {
            var layer = GetOrCreateLayer(layerName);

            if (tile.tileDefinition != null && tile.tileDefinition.IsWall)
            {
                if (tile.freePlace)
                {
                    // 자유 배치 벽: id로만 식별. 같은 id가 있으면 교체(이동 시), 없으면 그냥 추가(여러 개 공존 허용).
                    if (!string.IsNullOrEmpty(tile.id))
                        layer.tiles.RemoveAll(t =>
                            t.tileDefinition != null && t.tileDefinition.IsWall
                            && t.freePlace && t.id == tile.id);
                }
                else
                {
                    // 스냅 벽: 셀+모서리+층당 1개 — 같은 층 같은 모서리의 스냅 벽만 교체 (자유 배치 벽은 보존)
                    layer.tiles.RemoveAll(t =>
                        t.gridPosition == tile.gridPosition && t.level == tile.level
                        && t.tileDefinition != null && t.tileDefinition.IsWall
                        && !t.freePlace && t.rotation == tile.rotation);
                }
            }
            else
            {
                // Regular tiles: 셀+층당 1개 — 같은 층의 비-벽 타일만 교체
                layer.tiles.RemoveAll(t =>
                    t.gridPosition == tile.gridPosition && t.level == tile.level
                    && (t.tileDefinition == null || !t.tileDefinition.IsWall));
            }

            layer.tiles.Add(tile);
            _cacheDirty = true;
        }

        public bool RemoveTileAt(Vector2Int pos, string layerName)
        {
            var layer = layers.Find(l => l.layerName == layerName);
            if (layer == null) return false;

            int removed = layer.tiles.RemoveAll(t => t.gridPosition == pos);
            if (removed > 0) _cacheDirty = true;
            return removed > 0;
        }

        /// <summary>
        /// Remove a specific wall edge at the given cell/rotation across all layers.
        /// </summary>
        public bool RemoveWallEdge(Vector2Int pos, int rotation, int level = 0)
        {
            bool any = false;
            foreach (var layer in layers)
            {
                int removed = layer.tiles.RemoveAll(t =>
                    t.gridPosition == pos && t.level == level
                    && t.tileDefinition != null && t.tileDefinition.IsWall
                    && t.rotation == rotation);
                if (removed > 0) any = true;
            }
            if (any) _cacheDirty = true;
            return any;
        }

        public void RemoveTileAtAllLayers(Vector2Int pos)
        {
            foreach (var layer in layers)
                layer.tiles.RemoveAll(t => t.gridPosition == pos);
            _cacheDirty = true;
        }

        /// <summary>
        /// Remove only non-wall tiles at the given position (ground/floor tiles only).
        /// Walls are preserved.
        /// </summary>
        public void RemoveNonWallTilesAt(Vector2Int pos, int level = 0)
        {
            foreach (var layer in layers)
                layer.tiles.RemoveAll(t =>
                    t.gridPosition == pos && t.level == level
                    && (t.tileDefinition == null || !t.tileDefinition.IsWall));
            _cacheDirty = true;
        }

        /// <summary>
        /// Repaint: replace existing tiles at this position with the new definition.
        /// Only affects cells that already have tiles — empty cells are skipped.
        /// Searches all layers, replaces in-place keeping the same layer.
        /// </summary>
        public bool RepaintTile(Vector2Int pos, TileDefinition newDef, Material materialOverride)
        {
            bool any = false;
            foreach (var layer in layers)
            {
                for (int i = 0; i < layer.tiles.Count; i++)
                {
                    var t = layer.tiles[i];
                    if (t.gridPosition != pos) continue;

                    // Skip walls if new tile is not a wall (and vice versa)
                    bool oldIsWall = t.tileDefinition != null && t.tileDefinition.IsWall;
                    bool newIsWall = newDef != null && newDef.IsWall;
                    if (oldIsWall != newIsWall) continue;

                    // Replace definition, keep position and rotation
                    t.tileDefinitionId = newDef.tileId;
                    t.tileDefinition = newDef;
                    t.materialOverride = materialOverride;
                    any = true;
                }
            }
            if (any) _cacheDirty = true;
            return any;
        }

        void OnValidate()
        {
            if (string.IsNullOrEmpty(mapId))
                mapId = System.Guid.NewGuid().ToString("N")[..8];
        }
    }
}
