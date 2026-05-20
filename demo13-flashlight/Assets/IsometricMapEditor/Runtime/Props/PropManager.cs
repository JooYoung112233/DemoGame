using UnityEngine;
using System.Collections.Generic;

namespace IsometricMapEditor
{
    public class PropManager : MonoBehaviour
    {
        readonly Dictionary<string, GameObject> _propObjects = new();
        Transform _root;

        public void Initialize(Transform parent)
        {
            _root = new GameObject("Props").transform;
            _root.SetParent(parent);
        }

        public void SpawnProps(List<PlacedProp> props, GridSettings settings)
        {
            ClearAll();
            foreach (var prop in props)
                SpawnProp(prop, settings);
        }

        public void SpawnProp(PlacedProp prop, GridSettings settings)
        {
            if (prop.propDefinition == null || prop.propDefinition.sprite == null) return;

            Vector3 worldPos = IsometricGrid.GridToWorld(prop.gridPosition, settings);
            var go = new GameObject($"Prop_{prop.instanceId}");
            go.transform.SetParent(_root);
            go.transform.position = worldPos;
            SetupBillboard(go);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = prop.propDefinition.sprite;
            sr.sortingOrder = IsometricGrid.GetSortingOrder(prop.gridPosition) + prop.propDefinition.sortingOffset;

            _propObjects[prop.instanceId] = go;
        }

        public void RemoveProp(string instanceId)
        {
            if (_propObjects.TryGetValue(instanceId, out var go))
            {
                Destroy(go);
                _propObjects.Remove(instanceId);
            }
        }

        static void SetupBillboard(GameObject go)
        {
            // Rotate sprite to face isometric camera (lying on XZ plane tilted toward camera)
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
        }

        public void ClearAll()
        {
            foreach (var go in _propObjects.Values)
                if (go != null) Destroy(go);
            _propObjects.Clear();
        }
    }
}
