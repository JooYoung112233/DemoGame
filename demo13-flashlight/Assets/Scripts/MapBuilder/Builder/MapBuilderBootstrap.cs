using UnityEngine;

namespace IsometricMapEditor
{
    public class MapBuilderBootstrap : MonoBehaviour
    {
        [Header("Drag your MapBuilderCatalog asset here")]
        public MapBuilderCatalog catalog;

        [Header("Initial Map Size")]
        public int mapWidth = 64;
        public int mapHeight = 64;
        [Tooltip("타일 한 칸의 월드 크기(m). 작게 하면 그리드가 촘촘해짐.")]
        public float tileSize = 1f;

        void Awake()
        {
            var existing = FindFirstObjectByType<MapBuilderManager>();
            if (existing != null)
            {
                Debug.LogWarning("[MapBuilder] Manager already exists, skipping bootstrap.");
                return;
            }

            // Inspector에 카탈로그가 안 물려있으면 Resources에서 자동 로드
            if (catalog == null)
                catalog = Resources.Load<MapBuilderCatalog>("MapBuilder/MapBuilderCatalog");

            var go = new GameObject("MapBuilderManager");
            var manager = go.AddComponent<MapBuilderManager>();
            manager.catalog = catalog;
            manager.defaultMapWidth = mapWidth;
            manager.defaultMapHeight = mapHeight;
            manager.defaultTileSize = tileSize;
        }
    }
}
