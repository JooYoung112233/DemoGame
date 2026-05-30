using UnityEngine;
using System.Collections.Generic;

namespace IsometricMapEditor
{
    public class PropManager : MonoBehaviour
    {
        readonly Dictionary<string, GameObject> _propObjects = new();
        Transform _root;

        // 건물별 내부 프랍 루트 (BuildingInterior가 관리)
        readonly Dictionary<string, Transform> _buildingInteriorRoots = new();
        Dictionary<string, GameObject> _buildingObjects;

        public void Initialize(Transform parent)
        {
            _root = new GameObject("Props").transform;
            _root.SetParent(parent);
        }

        /// <summary>
        /// BuildingRenderer의 건물 오브젝트 딕셔너리를 연결.
        /// 내부 프랍을 건물 GO 하위에 배치하기 위해 필요.
        /// </summary>
        public void SetBuildingObjects(Dictionary<string, GameObject> buildingObjects)
        {
            _buildingObjects = buildingObjects;
        }

        public void SpawnProps(List<PlacedProp> props, GridSettings settings)
        {
            ClearAll();
            foreach (var prop in props)
                SpawnProp(prop, settings);

            // BuildingInterior 캐시 갱신 (내부 프랍이 추가된 후)
            RebuildBuildingInteriorCaches();
        }

        public void SpawnProp(PlacedProp prop, GridSettings settings)
        {
            if (prop.propDefinition == null || prop.propDefinition.prefab == null) return;

            Vector3 worldPos = prop.GetWorldPosition(settings);

            // 소속 건물이 있으면 건물 하위 Interior 루트에 배치
            Transform parent = _root;
            if (!string.IsNullOrEmpty(prop.parentBuildingId))
            {
                parent = GetOrCreateInteriorRoot(prop.parentBuildingId);
            }

            var go = Instantiate(prop.propDefinition.prefab, worldPos, Quaternion.identity, parent);
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

        /// <summary>
        /// 건물 하위 "Interior" GO를 가져오거나 생성.
        /// BuildingInterior 컴포넌트가 이 루트의 자식 Renderer를 자동 관리.
        /// </summary>
        Transform GetOrCreateInteriorRoot(string buildingId)
        {
            if (_buildingInteriorRoots.TryGetValue(buildingId, out var existing) && existing != null)
                return existing;

            // 건물 GO 탐색
            Transform buildingTransform = null;
            if (_buildingObjects != null && _buildingObjects.TryGetValue(buildingId, out var buildingGo) && buildingGo != null)
            {
                buildingTransform = buildingGo.transform;
            }

            if (buildingTransform == null)
            {
                // 건물을 못 찾으면 Props 루트에 그룹 생성
                var fallbackGo = new GameObject($"Interior_{buildingId}");
                fallbackGo.transform.SetParent(_root);
                _buildingInteriorRoots[buildingId] = fallbackGo.transform;
                Debug.LogWarning($"[PropManager] 건물 '{buildingId}' 를 찾을 수 없음. Props 루트에 그룹 생성.");
                return fallbackGo.transform;
            }

            // 건물 하위에 기존 Interior 루트가 있는지 확인
            for (int i = 0; i < buildingTransform.childCount; i++)
            {
                var child = buildingTransform.GetChild(i);
                if (child.name == "Interior" || child.name.Contains("Interior"))
                {
                    _buildingInteriorRoots[buildingId] = child;
                    return child;
                }
            }

            // 없으면 새로 생성
            var interiorGo = new GameObject("Interior");
            interiorGo.transform.SetParent(buildingTransform, false);
            interiorGo.transform.localPosition = Vector3.zero;
            _buildingInteriorRoots[buildingId] = interiorGo.transform;
            return interiorGo.transform;
        }

        /// <summary>
        /// 모든 건물의 BuildingInterior 컴포넌트에 프랍 캐시 갱신을 알림.
        /// SpawnProps 완료 후 한번 호출.
        /// </summary>
        void RebuildBuildingInteriorCaches()
        {
            if (_buildingObjects == null) return;

            foreach (var kvp in _buildingObjects)
            {
                if (kvp.Value == null) continue;
                var interior = kvp.Value.GetComponentInChildren<BuildingInterior>();
                if (interior != null)
                    interior.RebuildPropCache();
            }
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
            _buildingInteriorRoots.Clear();
        }
    }
}
