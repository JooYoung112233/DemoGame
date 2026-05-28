using UnityEngine;

namespace IsometricMapEditor
{
    [CreateAssetMenu(menuName = "Isometric Map/Map Builder Catalog")]
    public class MapBuilderCatalog : ScriptableObject
    {
        [Header("Tiles")]
        public TileDefinition[] tiles = new TileDefinition[0];

        [Header("Wall Tiles")]
        public TileDefinition[] walls = new TileDefinition[0];

        [Header("Props")]
        public PropDefinition[] props = new PropDefinition[0];

        [Header("Buildings")]
        public BuildingDefinition[] buildings = new BuildingDefinition[0];

        public TileDefinition GetTile(string id)
        {
            foreach (var t in tiles)
                if (t != null && t.tileId == id) return t;
            foreach (var w in walls)
                if (w != null && w.tileId == id) return w;
            return null;
        }

        public PropDefinition GetProp(string id)
        {
            foreach (var p in props)
                if (p != null && p.propId == id) return p;
            return null;
        }

        public BuildingDefinition GetBuilding(string id)
        {
            foreach (var b in buildings)
                if (b != null && b.buildingId == id) return b;
            return null;
        }
    }
}
