using UnityEngine;
using System.Collections.Generic;

namespace IsometricMapEditor
{
    /// <summary>
    /// Runtime building renderer — instantiates prefabs from BuildingDefinition.
    /// </summary>
    public class BuildingRenderer : MonoBehaviour
    {
        readonly Dictionary<string, GameObject> _buildingObjects = new();
        Transform _root;

        public void Initialize(Transform parent)
        {
            _root = new GameObject("Buildings").transform;
            _root.SetParent(parent);
        }

        public void RenderBuildings(MapData map)
        {
            ClearAll();
            foreach (var building in map.buildings)
                RenderBuilding(building, map.gridSettings);
        }

        public void RenderBuilding(PlacedBuilding building, GridSettings settings)
        {
            if (building.buildingDefinition == null) return;
            var def = building.buildingDefinition;

            // Resolve prefab: check active variant first, then fallback to definition
            GameObject prefab = GetActivePrefab(building, def);
            if (prefab == null) return;

            Vector3 worldPos = building.GetWorldPosition(settings);

            var go = Instantiate(prefab, worldPos, Quaternion.Euler(0, building.yRotation, 0), _root);
            go.name = $"Building_{building.instanceId}";
            go.transform.localScale = Vector3.one * building.scale;

            _buildingObjects[building.instanceId] = go;
        }

        static GameObject GetActivePrefab(PlacedBuilding building, BuildingDefinition def)
        {
            if (def.variantSet != null && !string.IsNullOrEmpty(building.activeVariantId))
            {
                var variant = def.variantSet.GetVariant(building.activeVariantId);
                if (variant?.prefab != null) return variant.prefab;
            }
            return def.prefab;
        }

        public void RemoveBuilding(string instanceId)
        {
            if (_buildingObjects.TryGetValue(instanceId, out var go))
            {
                Destroy(go);
                _buildingObjects.Remove(instanceId);
            }
        }

        public void ClearAll()
        {
            foreach (var go in _buildingObjects.Values)
                if (go != null) Destroy(go);
            _buildingObjects.Clear();
        }
    }
}
