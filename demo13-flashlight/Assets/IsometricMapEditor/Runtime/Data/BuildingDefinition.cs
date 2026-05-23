using UnityEngine;
using System.Collections.Generic;

namespace IsometricMapEditor
{
    [CreateAssetMenu(menuName = "Isometric Map/Building Definition")]
    public class BuildingDefinition : ScriptableObject
    {
        public string buildingId;
        public string displayName;
        public Vector2Int footprint = new(2, 2);
        public int sortingOffset;
        public bool isEnterable;
        public string interiorMapId;
        public Vector2Int entryCell;
        public BuildingVariantSet variantSet;

        [Header("Prefab")]
        [Tooltip("The prefab to instantiate when this building is placed on the map.")]
        public GameObject prefab;

        [Header("Editor Preview")]
        [Tooltip("Optional icon sprite shown in the palette. If null, uses prefab preview.")]
        public Sprite icon;

        public bool IsMultiTile => footprint.x > 1 || footprint.y > 1;

        public List<Vector2Int> GetOccupiedCells(Vector2Int origin)
        {
            var cells = new List<Vector2Int>();
            for (int x = 0; x < footprint.x; x++)
                for (int y = 0; y < footprint.y; y++)
                    cells.Add(new Vector2Int(origin.x + x, origin.y + y));
            return cells;
        }

        public int GetFrontSortingOrder(Vector2Int origin)
        {
            int frontX = origin.x + footprint.x - 1;
            int frontY = origin.y + footprint.y - 1;
            return IsometricGrid.GetSortingOrder(new Vector2Int(frontX, frontY));
        }
    }
}
