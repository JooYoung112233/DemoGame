using UnityEngine;
using System.Collections.Generic;

namespace IsometricMapEditor
{
    public class RoofHideController : MonoBehaviour
    {
        public float hideDistance = 2f;
        public float fadeDuration = 0.3f;

        MapData mapData;
        BuildingRenderer buildingRenderer;
        Transform playerTransform;
        readonly Dictionary<string, float> _roofAlphaTargets = new();

        public void Initialize(MapData map, BuildingRenderer renderer, Transform player)
        {
            mapData = map;
            buildingRenderer = renderer;
            playerTransform = player;
        }

        void Update()
        {
            if (mapData == null || playerTransform == null) return;

            Vector3 playerPos = playerTransform.position;

            foreach (var building in mapData.buildings)
            {
                if (building.buildingDefinition == null) continue;
                if (building.buildingDefinition.roofSprite == null) continue;

                Vector3 buildingWorld = IsometricGrid.GridToWorld(building.gridPosition, mapData.gridSettings);
                // Distance on XZ plane only (ignore Y height)
                float dist = Vector2.Distance(
                    new Vector2(playerPos.x, playerPos.z),
                    new Vector2(buildingWorld.x, buildingWorld.z));

                bool shouldHide = dist < hideDistance;
                _roofAlphaTargets[building.instanceId] = shouldHide ? 0f : 1f;
                buildingRenderer.SetRoofVisible(building.instanceId, !shouldHide);
            }
        }
    }
}
