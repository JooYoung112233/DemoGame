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

        /// <summary>건물 instanceId → GameObject 딕셔너리 (PropManager 연동용)</summary>
        public Dictionary<string, GameObject> BuildingObjects => _buildingObjects;

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

            // 내부 투명 전환: BuildingInterior 자동 부착
            if (def.occludesInterior)
                SetupBuildingInterior(go, def, settings);

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

        /// <summary>
        /// BuildingInterior 자동 부착 — 플레이어 진입 시 벽 페이드 + 내부 프랍 표시.
        /// 프리팹 내 자식 구조를 분석해 벽/프랍을 자동 분류.
        /// </summary>
        static void SetupBuildingInterior(GameObject buildingGo, BuildingDefinition def, GridSettings settings)
        {
            // 이미 프리팹에 BuildingInterior가 있으면 스킵
            if (buildingGo.GetComponentInChildren<BuildingInterior>() != null) return;

            // 트리거 영역 생성
            var triggerGo = new GameObject("InteriorTrigger");
            triggerGo.transform.SetParent(buildingGo.transform, false);

            // 건물 footprint 기반 트리거 크기 계산
            float tileSize = settings.tileSize;
            float width = def.footprint.x * tileSize;
            float depth = def.footprint.y * tileSize;

            var box = triggerGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0, 1.5f, 0);
            box.size = new Vector3(width, 3f, depth);

            var interior = triggerGo.AddComponent<BuildingInterior>();

            // ── 벽 렌더러 자동 수집 ──
            // 이름에 "Wall", "Roof", "Ceiling", "Front", "Facade" 포함된 자식
            var wallRenderers = new System.Collections.Generic.List<Renderer>();
            var propRoot = (Transform)null;

            foreach (Transform child in buildingGo.transform)
            {
                string name = child.name.ToLower();

                // 트리거 자체는 건너뛰기
                if (child == triggerGo.transform) continue;

                // 내부 프랍 루트 탐색
                if (name.Contains("interior") || name.Contains("props")
                    || name.Contains("furniture") || name.Contains("inside"))
                {
                    propRoot = child;
                    continue;
                }

                // 벽/천장/지붕 → 페이드 대상
                if (name.Contains("wall") || name.Contains("roof") || name.Contains("ceiling")
                    || name.Contains("front") || name.Contains("facade") || name.Contains("top"))
                {
                    var renderers = child.GetComponentsInChildren<Renderer>();
                    wallRenderers.AddRange(renderers);
                }
            }

            // 벽 렌더러가 없으면 전체 렌더러를 대상으로 (프리팹 구조가 단순한 경우)
            if (wallRenderers.Count == 0)
            {
                // 건물 루트의 직접 렌더러만 (자식 프랍은 제외)
                var rootRenderer = buildingGo.GetComponent<Renderer>();
                if (rootRenderer != null)
                    wallRenderers.Add(rootRenderer);
            }

            // 리플렉션으로 SerializeField 설정
            SetField(interior, "fadeRenderers", wallRenderers.ToArray());

            if (propRoot != null)
                SetField(interior, "interiorPropRoot", propRoot);

            // 캐시 재구축
            interior.RebuildPropCache();
        }

        static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(target, value);
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
