using System.Collections.Generic;
using UnityEngine;

namespace IsometricMapEditor
{
    /// <summary>
    /// A collection of zones (MapData assets) that make up a world/region.
    /// Use this to organize multiple map zones and switch between them in the editor.
    /// </summary>
    [CreateAssetMenu(menuName = "Isometric Map/Zone Collection")]
    public class ZoneCollection : ScriptableObject
    {
        public string worldName = "New World";

        [System.Serializable]
        public class ZoneEntry
        {
            public string zoneName;
            public MapData mapData;
        }

        public List<ZoneEntry> zones = new List<ZoneEntry>();

        /// <summary>
        /// Add a new zone with the given name and MapData.
        /// </summary>
        public ZoneEntry AddZone(string zoneName, MapData mapData)
        {
            var entry = new ZoneEntry
            {
                zoneName = zoneName,
                mapData = mapData
            };
            zones.Add(entry);
            return entry;
        }

        /// <summary>
        /// Remove a zone by index.
        /// </summary>
        public void RemoveZone(int index)
        {
            if (index >= 0 && index < zones.Count)
                zones.RemoveAt(index);
        }

        /// <summary>
        /// Get the next available zone number for naming.
        /// </summary>
        public int GetNextZoneNumber()
        {
            return zones.Count + 1;
        }
    }
}
