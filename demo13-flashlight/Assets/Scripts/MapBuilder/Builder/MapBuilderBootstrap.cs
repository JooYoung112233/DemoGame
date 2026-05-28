using UnityEngine;

namespace IsometricMapEditor
{
    public class MapBuilderBootstrap : MonoBehaviour
    {
        [Header("Drag your MapBuilderCatalog asset here")]
        public MapBuilderCatalog catalog;

        [Header("Initial Map Size")]
        public int mapWidth = 32;
        public int mapHeight = 32;

        void Awake()
        {
            var existing = FindFirstObjectByType<MapBuilderManager>();
            if (existing != null)
            {
                Debug.LogWarning("[MapBuilder] Manager already exists, skipping bootstrap.");
                return;
            }

            var go = new GameObject("MapBuilderManager");
            var manager = go.AddComponent<MapBuilderManager>();
            manager.catalog = catalog;
            manager.defaultMapWidth = mapWidth;
            manager.defaultMapHeight = mapHeight;
        }
    }
}
