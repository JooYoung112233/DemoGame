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
            if (prop.propDefinition == null || prop.propDefinition.prefab == null) return;

            Vector3 worldPos = prop.GetWorldPosition(settings);
            var go = Instantiate(prop.propDefinition.prefab, worldPos, Quaternion.identity, _root);
            go.name = $"Prop_{prop.instanceId}";

            if (prop.freePlace)
            {
                // Y회전을 프리팹 Root 회전에 곱함 (Root의 카메라 맞춤 회전 유지)
                if (Mathf.Abs(prop.yRotation) > 0.01f)
                    go.transform.rotation = Quaternion.Euler(0, prop.yRotation, 0) * go.transform.rotation;
                go.transform.localScale = Vector3.one * prop.scale;
            }

            // 빛 차폐 그림자 프록시 박스 (벽/컨테이너 등). 런타임은 ShadowsOnly.
            ShadowProxyBuilder.Build(prop.propDefinition, go.transform, editorPreview: false);

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

        public void ClearAll()
        {
            foreach (var go in _propObjects.Values)
                if (go != null) Destroy(go);
            _propObjects.Clear();
        }
    }
}
