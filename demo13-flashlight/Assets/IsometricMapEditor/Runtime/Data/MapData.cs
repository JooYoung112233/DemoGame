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
        public List<PlacedBuilding> buildings = new();
        public List<PlacedProp> props = new();
        public List<PlacedHarvestable> harvestables = new();
        public List<InteriorConnection> interiorConnections = new();
        public WalkabilityData walkability;

        public void InitializeWalkability()
        {
            walkability = new WalkabilityData(gridSettings.mapWidth, gridSettings.mapHeight);
        }

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

            if (tile.tileDefinition != null && tile.tileDefinition.IsWall)
            {
                // Walls: one per edge (rotation) per cell — replace only the same edge
                layer.tiles.RemoveAll(t =>
                    t.gridPosition == tile.gridPosition
                    && t.tileDefinition != null && t.tileDefinition.IsWall
                    && t.rotation == tile.rotation);
            }
            else
            {
                // Regular tiles: one per cell — replace existing non-wall tile
                layer.tiles.RemoveAll(t =>
                    t.gridPosition == tile.gridPosition
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
        public bool RemoveWallEdge(Vector2Int pos, int rotation)
        {
            bool any = false;
            foreach (var layer in layers)
            {
                int removed = layer.tiles.RemoveAll(t =>
                    t.gridPosition == pos
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
