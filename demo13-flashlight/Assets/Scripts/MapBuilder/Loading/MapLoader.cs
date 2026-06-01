using UnityEngine;

namespace TopDownMapEditor
{
    public class MapLoader : MonoBehaviour
    {
        [SerializeField] MapData mapData;

        TileRenderer tileRenderer;
        TopDownSortingManager sortingManager;

        public MapData CurrentMap => mapData;

        public void LoadMap(MapData data)
        {
            mapData = data;
            if (data == null) return;

            EnsureComponents();
            tileRenderer.RenderMap(data);
        }

        public void RefreshRendering()
        {
            if (mapData == null) return;
            EnsureComponents();
            tileRenderer.RenderMap(mapData);
        }

        void EnsureComponents()
        {
            if (tileRenderer == null)
            {
                var tileRoot = new GameObject("TileRoot");
                tileRoot.transform.SetParent(transform);
                tileRenderer = tileRoot.AddComponent<TileRenderer>();
            }

            if (sortingManager == null)
                sortingManager = gameObject.AddComponent<TopDownSortingManager>();
        }

        public void UnloadMap()
        {
            if (tileRenderer != null)
                tileRenderer.ClearAll();
            mapData = null;
        }
    }
}
