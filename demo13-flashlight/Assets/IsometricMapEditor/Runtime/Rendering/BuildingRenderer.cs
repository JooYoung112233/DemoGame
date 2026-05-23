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

            // Material Preset 적용: 텍스처는 유지, 셰이더 파라미터만 덮어쓰기
            if (def.materialPreset != null)
                ApplyMaterialPreset(go, def.materialPreset);

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

        /// <summary>
        /// 프리셋 Material에서 셰이더 파라미터만 복사 (텍스처 슬롯은 프리팹 것 유지)
        /// </summary>
        static void ApplyMaterialPreset(GameObject go, Material preset)
        {
            // 텍스처 프로퍼티 이름 — 이 목록의 텍스처는 프리팹 원본 유지
            var textureProps = new[] { "_MainTex", "_GlowMask", "_HeightFadeMask", "_BumpMap" };

            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;

                    // 원본 텍스처 백업
                    var savedTextures = new System.Collections.Generic.Dictionary<string, Texture>();
                    foreach (var prop in textureProps)
                    {
                        if (mats[i].HasProperty(prop))
                            savedTextures[prop] = mats[i].GetTexture(prop);
                    }

                    // 프리셋 기반 새 머티리얼 생성
                    var newMat = new Material(preset);

                    // 백업한 텍스처 복원
                    foreach (var kvp in savedTextures)
                    {
                        if (kvp.Value != null && newMat.HasProperty(kvp.Key))
                            newMat.SetTexture(kvp.Key, kvp.Value);
                    }

                    mats[i] = newMat;
                }
                renderer.sharedMaterials = mats;
            }
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
