using UnityEngine;
using System;

namespace IsometricMapEditor
{
    public class InteriorTransitionManager : MonoBehaviour
    {
        public float fadeDuration = 0.5f;

        InteriorMapLoader mapLoader;
        BuildingRenderer buildingRenderer;
        RoofController roofController;

        bool _isTransitioning;
        string _currentInteriorId;

        public bool IsInInterior => !string.IsNullOrEmpty(_currentInteriorId);
        public string CurrentInteriorId => _currentInteriorId;

        public event Action<string> OnEnterInterior;
        public event Action OnExitInterior;

        public void Initialize(InteriorMapLoader loader, BuildingRenderer renderer, RoofController roof)
        {
            mapLoader = loader;
            buildingRenderer = renderer;
            roofController = roof;
        }

        public void EnterBuilding(PlacedBuilding building)
        {
            if (_isTransitioning) return;
            if (building.buildingDefinition == null || !building.buildingDefinition.isEnterable) return;

            string interiorId = building.buildingDefinition.interiorMapId;
            if (string.IsNullOrEmpty(interiorId)) return;

            _isTransitioning = true;
            roofController.HideRoof(building.instanceId);

            var interiorMap = mapLoader.LoadInterior(interiorId);
            if (interiorMap != null)
            {
                _currentInteriorId = interiorId;
                OnEnterInterior?.Invoke(interiorId);
            }

            _isTransitioning = false;
        }

        public void ExitToExterior()
        {
            if (_isTransitioning || !IsInInterior) return;

            _isTransitioning = true;
            mapLoader.UnloadInterior(_currentInteriorId);
            _currentInteriorId = null;
            OnExitInterior?.Invoke();
            _isTransitioning = false;
        }

        public void UseEscapePoint(EscapePoint escape)
        {
            if (_isTransitioning) return;

            if (!string.IsNullOrEmpty(escape.targetMapId))
            {
                if (IsInInterior)
                    mapLoader.UnloadInterior(_currentInteriorId);

                var target = mapLoader.LoadInterior(escape.targetMapId);
                if (target != null)
                {
                    _currentInteriorId = escape.targetMapId;
                    OnEnterInterior?.Invoke(escape.targetMapId);
                }
                else
                {
                    _currentInteriorId = null;
                    OnExitInterior?.Invoke();
                }
            }
            else
            {
                ExitToExterior();
            }
        }
    }
}
