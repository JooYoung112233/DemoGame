using UnityEngine;
using System.Collections.Generic;

namespace IsometricMapEditor
{
    public class BuildingStateManager : MonoBehaviour
    {
        MapData mapData;
        BuildingRenderer buildingRenderer;
        StateConditionEvaluator conditionEvaluator;
        readonly Dictionary<string, string> _currentStates = new();

        public void Initialize(MapData map, BuildingRenderer renderer)
        {
            mapData = map;
            buildingRenderer = renderer;
            conditionEvaluator = new StateConditionEvaluator();

            foreach (var building in mapData.buildings)
            {
                string initialState = building.activeVariantId;
                if (string.IsNullOrEmpty(initialState) && building.buildingDefinition?.variantSet != null)
                {
                    var def = building.buildingDefinition.variantSet.GetDefault();
                    if (def != null) initialState = def.variantId;
                }
                _currentStates[building.instanceId] = initialState ?? "";
            }
        }

        void Update()
        {
            if (mapData == null) return;

            foreach (var building in mapData.buildings)
            {
                if (building.buildingDefinition?.variantSet == null) continue;

                var variantSet = building.buildingDefinition.variantSet;
                string currentState = _currentStates.GetValueOrDefault(building.instanceId, "");

                foreach (var rule in variantSet.transitionRules)
                {
                    if (!rule.autoTransition) continue;
                    if (rule.fromVariantId != currentState) continue;

                    if (conditionEvaluator.Evaluate(rule.conditionType, rule.conditionParam))
                    {
                        TransitionTo(building, rule.toVariantId);
                        break;
                    }
                }
            }
        }

        public void TransitionTo(PlacedBuilding building, string variantId)
        {
            _currentStates[building.instanceId] = variantId;
            building.activeVariantId = variantId;
            buildingRenderer.UpdateBuildingVariant(building, building.buildingDefinition);
        }

        public string GetCurrentState(string instanceId)
        {
            return _currentStates.GetValueOrDefault(instanceId, "");
        }
    }
}
