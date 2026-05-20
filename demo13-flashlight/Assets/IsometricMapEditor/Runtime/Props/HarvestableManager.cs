using UnityEngine;
using System.Collections.Generic;

namespace IsometricMapEditor
{
    public class HarvestableManager : MonoBehaviour
    {
        readonly Dictionary<string, GameObject> _harvestableObjects = new();
        readonly Dictionary<string, bool> _depletedState = new();
        readonly Dictionary<string, float> _respawnTimers = new();
        SpawnConditionEvaluator _conditionEvaluator;
        Transform _root;
        GridSettings _settings;
        List<PlacedHarvestable> _harvestables;

        public void Initialize(Transform parent)
        {
            _root = new GameObject("Harvestables").transform;
            _root.SetParent(parent);
            _conditionEvaluator = new SpawnConditionEvaluator();
        }

        public void SpawnHarvestables(List<PlacedHarvestable> harvestables, GridSettings settings)
        {
            ClearAll();
            _settings = settings;
            _harvestables = harvestables;

            foreach (var h in harvestables)
            {
                if (h.harvestableDefinition == null) continue;
                if (!_conditionEvaluator.EvaluateAll(h.harvestableDefinition.spawnConditions))
                    continue;
                SpawnHarvestable(h);
            }
        }

        void SpawnHarvestable(PlacedHarvestable h)
        {
            if (h.harvestableDefinition.sprite == null) return;

            Vector2 worldPos = IsometricGrid.GridToWorld(h.gridPosition, _settings);
            var go = new GameObject($"Harvestable_{h.instanceId}");
            go.transform.SetParent(_root);
            go.transform.position = new Vector3(worldPos.x, worldPos.y, 0);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = h.harvestableDefinition.sprite;
            sr.sortingOrder = IsometricGrid.GetSortingOrder(h.gridPosition) + h.harvestableDefinition.sortingOffset;

            _harvestableObjects[h.instanceId] = go;
            _depletedState[h.instanceId] = false;
        }

        public List<DropEntry> Harvest(string instanceId)
        {
            var h = _harvestables?.Find(x => x.instanceId == instanceId);
            if (h?.harvestableDefinition == null) return null;
            if (_depletedState.GetValueOrDefault(instanceId)) return null;

            _depletedState[instanceId] = true;

            if (_harvestableObjects.TryGetValue(instanceId, out var go))
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null && h.harvestableDefinition.depletedSprite != null)
                    sr.sprite = h.harvestableDefinition.depletedSprite;
            }

            if (h.harvestableDefinition.respawnTimeSec > 0)
                _respawnTimers[instanceId] = h.harvestableDefinition.respawnTimeSec;

            var drops = new List<DropEntry>();
            foreach (var entry in h.harvestableDefinition.dropTable)
            {
                if (Random.value <= entry.dropRate)
                    drops.Add(entry);
            }
            return drops;
        }

        void Update()
        {
            if (_harvestables == null) return;

            var toRespawn = new List<string>();
            var keys = new List<string>(_respawnTimers.Keys);
            foreach (var key in keys)
            {
                _respawnTimers[key] -= Time.deltaTime;
                if (_respawnTimers[key] <= 0)
                    toRespawn.Add(key);
            }

            foreach (var id in toRespawn)
            {
                _respawnTimers.Remove(id);
                _depletedState[id] = false;

                var h = _harvestables.Find(x => x.instanceId == id);
                if (h?.harvestableDefinition == null) continue;

                if (_harvestableObjects.TryGetValue(id, out var go))
                {
                    var sr = go.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sprite = h.harvestableDefinition.sprite;
                }
            }
        }

        public void ClearAll()
        {
            foreach (var go in _harvestableObjects.Values)
                if (go != null) Destroy(go);
            _harvestableObjects.Clear();
            _depletedState.Clear();
            _respawnTimers.Clear();
        }
    }
}
