using UnityEngine;
using System.Collections.Generic;

namespace IsometricMapEditor
{
    public class InteriorMapLoader : MonoBehaviour
    {
        [SerializeField] List<InteriorMapData> availableInteriors = new();

        readonly Dictionary<string, InteriorMapData> _loadedInteriors = new();
        readonly Dictionary<string, GameObject> _interiorRoots = new();

        public InteriorMapData LoadInterior(string interiorId)
        {
            if (_loadedInteriors.TryGetValue(interiorId, out var cached))
                return cached;

            var data = availableInteriors.Find(i => i.interiorId == interiorId);
            if (data == null)
            {
                Debug.LogWarning($"[InteriorMapLoader] Interior '{interiorId}' not found.");
                return null;
            }

            var root = new GameObject($"Interior_{interiorId}");
            root.transform.SetParent(transform);

            foreach (var layer in data.layers)
            {
                if (!layer.isVisible) continue;
                foreach (var tile in layer.tiles)
                {
                    if (tile.tileDefinition == null || tile.tileDefinition.sprite == null) continue;

                    Vector3 worldPos = IsometricGrid.GridToWorld(tile.gridPosition, data.gridSettings);
                    var go = new GameObject($"Tile_{tile.gridPosition.x}_{tile.gridPosition.y}");
                    go.transform.SetParent(root.transform);
                    go.transform.position = worldPos;
                    go.transform.rotation = Quaternion.Euler(90, 0, 0);

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = tile.tileDefinition.sprite;
                    sr.sortingOrder = IsometricGrid.GetSortingOrder(tile.gridPosition, layer.sortingLayerOffset);
                }
            }

            _loadedInteriors[interiorId] = data;
            _interiorRoots[interiorId] = root;
            return data;
        }

        public void UnloadInterior(string interiorId)
        {
            if (_interiorRoots.TryGetValue(interiorId, out var root))
            {
                Destroy(root);
                _interiorRoots.Remove(interiorId);
            }
            _loadedInteriors.Remove(interiorId);
        }

        public void UnloadAll()
        {
            foreach (var root in _interiorRoots.Values)
                if (root != null) Destroy(root);
            _interiorRoots.Clear();
            _loadedInteriors.Clear();
        }

        public InteriorMapData GetLoadedInterior(string interiorId)
        {
            return _loadedInteriors.GetValueOrDefault(interiorId);
        }
    }
}
