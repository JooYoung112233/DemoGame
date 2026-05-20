using UnityEngine;
using System.Collections.Generic;

namespace IsometricMapEditor
{
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
            Vector3 worldPos = IsometricGrid.GridToWorld(building.gridPosition, settings);

            var go = new GameObject($"Building_{building.instanceId}");
            go.transform.SetParent(_root);
            go.transform.position = worldPos;
            SetupBillboard(go);

            var baseSR = go.AddComponent<SpriteRenderer>();
            baseSR.sprite = GetActiveSprite(building, def);
            baseSR.sortingOrder = def.GetFrontSortingOrder(building.gridPosition) + def.sortingOffset;

            if (def.roofSprite != null)
            {
                var roofGO = new GameObject("Roof");
                roofGO.transform.SetParent(go.transform);
                roofGO.transform.localPosition = Vector3.zero;
                var roofSR = roofGO.AddComponent<SpriteRenderer>();
                roofSR.sprite = GetActiveRoofSprite(building, def);
                roofSR.sortingOrder = baseSR.sortingOrder + 1;
                roofSR.enabled = building.roofVisible;
            }

            _buildingObjects[building.instanceId] = go;
        }

        Sprite GetActiveSprite(PlacedBuilding building, BuildingDefinition def)
        {
            if (def.variantSet != null && !string.IsNullOrEmpty(building.activeVariantId))
            {
                var variant = def.variantSet.GetVariant(building.activeVariantId);
                if (variant?.baseSprite != null) return variant.baseSprite;
            }
            return def.baseSprite;
        }

        Sprite GetActiveRoofSprite(PlacedBuilding building, BuildingDefinition def)
        {
            if (def.variantSet != null && !string.IsNullOrEmpty(building.activeVariantId))
            {
                var variant = def.variantSet.GetVariant(building.activeVariantId);
                if (variant?.roofSprite != null) return variant.roofSprite;
            }
            return def.roofSprite;
        }

        public void SetRoofVisible(string instanceId, bool visible)
        {
            if (!_buildingObjects.TryGetValue(instanceId, out var go)) return;
            var roofTransform = go.transform.Find("Roof");
            if (roofTransform != null)
            {
                var sr = roofTransform.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = visible;
            }
        }

        public void UpdateBuildingVariant(PlacedBuilding building, BuildingDefinition def)
        {
            if (!_buildingObjects.TryGetValue(building.instanceId, out var go)) return;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = GetActiveSprite(building, def);

            var roofTransform = go.transform.Find("Roof");
            if (roofTransform != null)
            {
                var roofSR = roofTransform.GetComponent<SpriteRenderer>();
                if (roofSR != null) roofSR.sprite = GetActiveRoofSprite(building, def);
            }
        }

        static void SetupBillboard(GameObject go)
        {
            // Rotate sprite to face isometric camera (lying on XZ plane tilted toward camera)
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
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
