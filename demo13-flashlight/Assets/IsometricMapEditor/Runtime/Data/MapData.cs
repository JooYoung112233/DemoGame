using System.Collections.Generic;
using UnityEngine;

namespace IsometricMapEditor
{
    [CreateAssetMenu(menuName = "Isometric Map/Map Data")]
    public class MapData : ScriptableObject
    {
        public string mapName;
        public string mapId;
        public GridSettings gridSettings = new();
        public List<MapLayer> layers = new();

        readonly Dictionary<Vector2Int, PlacedTile> _occupancyCache = new();
        bool _cacheDirty = true;

        void OnEnable() => _cacheDirty = true;

        public void MarkDirty() => _cacheDirty = true;

        void RebuildCache()
        {
            _occupancyCache.Clear();
            foreach (var layer in layers)
                foreach (var tile in layer.tiles)
                    _occupancyCache[tile.gridPosition] = tile;
            _cacheDirty = false;
        }

        public PlacedTile GetTileAt(Vector2Int pos)
        {
            if (_cacheDirty) RebuildCache();
            return _occupancyCache.GetValueOrDefault(pos);
        }

        public bool IsCellOccupied(Vector2Int pos)
        {
            if (_cacheDirty) RebuildCache();
            return _occupancyCache.ContainsKey(pos);
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
            layer.tiles.RemoveAll(t => t.gridPosition == tile.gridPosition);
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

        public void RemoveTileAtAllLayers(Vector2Int pos)
        {
            foreach (var layer in layers)
                layer.tiles.RemoveAll(t => t.gridPosition == pos);
            _cacheDirty = true;
        }

        void OnValidate()
        {
            if (string.IsNullOrEmpty(mapId))
                mapId = System.Guid.NewGuid().ToString("N")[..8];
        }
    }
}
