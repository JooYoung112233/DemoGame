using UnityEngine;
using System.Collections.Generic;

namespace IsometricMapEditor
{
    public class RoofController : MonoBehaviour
    {
        BuildingRenderer buildingRenderer;
        readonly Dictionary<string, float> _roofAlpha = new();

        public void Initialize(BuildingRenderer renderer)
        {
            buildingRenderer = renderer;
        }

        public void ShowRoof(string instanceId)
        {
            _roofAlpha[instanceId] = 1f;
            buildingRenderer.SetRoofVisible(instanceId, true);
        }

        public void HideRoof(string instanceId)
        {
            _roofAlpha[instanceId] = 0f;
            buildingRenderer.SetRoofVisible(instanceId, false);
        }

        public void ToggleRoof(string instanceId)
        {
            float current = _roofAlpha.GetValueOrDefault(instanceId, 1f);
            if (current > 0.5f)
                HideRoof(instanceId);
            else
                ShowRoof(instanceId);
        }
    }
}
