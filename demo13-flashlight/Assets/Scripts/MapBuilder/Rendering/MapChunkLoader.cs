using UnityEngine;
using System.Collections.Generic;

namespace TopDownMapEditor
{
    public class MapChunkLoader : MonoBehaviour
    {
        public int chunkSize = 16;
        public int loadRadius = 2;

        MapData mapData;
        TileRenderer tileRenderer;
        readonly HashSet<Vector2Int> _loadedChunks = new();
        Vector2Int _lastChunk = new(-999, -999);

        public void Initialize(MapData map, TileRenderer renderer)
        {
            mapData = map;
            tileRenderer = renderer;
        }

        public void UpdateChunks(Vector3 playerWorldPos)
        {
            if (mapData == null) return;

            Vector2Int playerGrid = TopDownGrid.WorldToGrid(playerWorldPos, mapData.gridSettings);
            Vector2Int currentChunk = new(playerGrid.x / chunkSize, playerGrid.y / chunkSize);

            if (currentChunk == _lastChunk) return;
            _lastChunk = currentChunk;

            var neededChunks = new HashSet<Vector2Int>();
            for (int cx = -loadRadius; cx <= loadRadius; cx++)
            {
                for (int cy = -loadRadius; cy <= loadRadius; cy++)
                {
                    neededChunks.Add(new Vector2Int(currentChunk.x + cx, currentChunk.y + cy));
                }
            }

            var toUnload = new List<Vector2Int>();
            foreach (var chunk in _loadedChunks)
                if (!neededChunks.Contains(chunk))
                    toUnload.Add(chunk);

            foreach (var chunk in toUnload)
            {
                UnloadChunk(chunk);
                _loadedChunks.Remove(chunk);
            }

            foreach (var chunk in neededChunks)
            {
                if (!_loadedChunks.Contains(chunk))
                {
                    LoadChunk(chunk);
                    _loadedChunks.Add(chunk);
                }
            }
        }

        void LoadChunk(Vector2Int chunkCoord)
        {
            int startX = chunkCoord.x * chunkSize;
            int startY = chunkCoord.y * chunkSize;

            foreach (var layer in mapData.layers)
            {
                foreach (var tile in layer.tiles)
                {
                    if (tile.gridPosition.x >= startX && tile.gridPosition.x < startX + chunkSize &&
                        tile.gridPosition.y >= startY && tile.gridPosition.y < startY + chunkSize)
                    {
                        tileRenderer.RenderSingleTile(tile, layer, mapData.gridSettings);
                    }
                }
            }
        }

        void UnloadChunk(Vector2Int chunkCoord)
        {
            int startX = chunkCoord.x * chunkSize;
            int startY = chunkCoord.y * chunkSize;

            for (int x = startX; x < startX + chunkSize; x++)
                for (int y = startY; y < startY + chunkSize; y++)
                    for (int lv = 0; lv <= TileRenderer.MAX_LEVEL; lv++)
                        tileRenderer.RemoveTileObject(new Vector2Int(x, y), lv);
        }

        public void ClearAll()
        {
            _loadedChunks.Clear();
            _lastChunk = new Vector2Int(-999, -999);
        }
    }
}
