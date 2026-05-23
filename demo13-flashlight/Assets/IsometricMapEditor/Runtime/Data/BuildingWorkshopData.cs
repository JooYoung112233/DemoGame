using System.Collections.Generic;
using UnityEngine;

namespace IsometricMapEditor
{
    /// <summary>
    /// Data asset for constructing a single building in the Building Workshop.
    /// Works like a mini MapData but focused on one building structure.
    /// </summary>
    [CreateAssetMenu(menuName = "Isometric Map/Building Workshop Data")]
    public class BuildingWorkshopData : ScriptableObject
    {
        public string buildingName = "New Building";

        [Header("Grid")]
        public int gridWidth = 8;
        public int gridHeight = 8;
        public float tileSize = 1f;

        [Header("Layers")]
        public List<PlacedTile> floorTiles = new List<PlacedTile>();
        public List<PlacedTile> wallTiles = new List<PlacedTile>();
        public List<PlacedProp> props = new List<PlacedProp>();

        [Header("Roof")]
        public List<PlacedTile> roofTiles = new List<PlacedTile>();
        public bool roofVisible = true;

        [Header("External Appearance")]
        [Tooltip("Icon sprite shown in the map editor palette.")]
        public Sprite externalIcon;
        [Tooltip("The prefab instantiated when this building is placed on the map.")]
        public GameObject externalPrefab;

        [Header("Output")]
        public BuildingDefinition outputDefinition;

        public GridSettings GetGridSettings()
        {
            GridSettings gs = new GridSettings();
            gs.mapWidth = gridWidth;
            gs.mapHeight = gridHeight;
            gs.tileSize = tileSize;
            return gs;
        }

        public bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
        }

        public void PlaceFloorTile(PlacedTile tile)
        {
            // Replace existing at same position (non-wall)
            for (int i = floorTiles.Count - 1; i >= 0; i--)
            {
                if (floorTiles[i].gridPosition == tile.gridPosition)
                {
                    floorTiles.RemoveAt(i);
                    break;
                }
            }
            floorTiles.Add(tile);
        }

        public void PlaceWallTile(PlacedTile tile)
        {
            // Allow multiple walls per cell (one per edge/rotation)
            for (int i = wallTiles.Count - 1; i >= 0; i--)
            {
                if (wallTiles[i].gridPosition == tile.gridPosition
                    && wallTiles[i].rotation == tile.rotation)
                {
                    wallTiles.RemoveAt(i);
                    break;
                }
            }
            wallTiles.Add(tile);
        }

        public void PlaceRoofTile(PlacedTile tile)
        {
            for (int i = roofTiles.Count - 1; i >= 0; i--)
            {
                if (roofTiles[i].gridPosition == tile.gridPosition)
                {
                    roofTiles.RemoveAt(i);
                    break;
                }
            }
            roofTiles.Add(tile);
        }

        public void RemoveTileAt(Vector2Int pos)
        {
            floorTiles.RemoveAll(t => t.gridPosition == pos);
            wallTiles.RemoveAll(t => t.gridPosition == pos);
        }

        public void RemoveWallAt(Vector2Int pos, int rotation)
        {
            wallTiles.RemoveAll(t => t.gridPosition == pos && t.rotation == rotation);
        }

        public void RemoveRoofAt(Vector2Int pos)
        {
            roofTiles.RemoveAll(t => t.gridPosition == pos);
        }

        public void RemovePropAt(Vector2Int pos)
        {
            props.RemoveAll(p => p.gridPosition == pos);
        }

        /// <summary>
        /// Get the bounding box of all placed tiles (for prefab export).
        /// </summary>
        public Vector2Int GetFootprint()
        {
            int maxX = 1;
            int maxY = 1;
            foreach (var t in floorTiles)
            {
                if (t.gridPosition.x + 1 > maxX) maxX = t.gridPosition.x + 1;
                if (t.gridPosition.y + 1 > maxY) maxY = t.gridPosition.y + 1;
            }
            foreach (var t in wallTiles)
            {
                if (t.gridPosition.x + 1 > maxX) maxX = t.gridPosition.x + 1;
                if (t.gridPosition.y + 1 > maxY) maxY = t.gridPosition.y + 1;
            }
            return new Vector2Int(maxX, maxY);
        }
    }
}
