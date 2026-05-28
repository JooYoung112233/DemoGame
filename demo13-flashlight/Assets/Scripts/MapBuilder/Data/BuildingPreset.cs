using System.Collections.Generic;
using UnityEngine;

namespace IsometricMapEditor
{
    /// <summary>
    /// A preset/template for BuildingWorkshopData.
    /// Save a building design and load it into any workshop data.
    /// </summary>
    [CreateAssetMenu(menuName = "Isometric Map/Building Preset")]
    public class BuildingPreset : ScriptableObject
    {
        [Header("Preset Info")]
        public string presetName;
        [TextArea(2, 4)]
        public string description;

        [Header("Grid")]
        public int gridWidth = 8;
        public int gridHeight = 8;
        public float tileSize = 1f;

        [Header("Data")]
        public List<PlacedTile> floorTiles = new List<PlacedTile>();
        public List<PlacedTile> wallTiles = new List<PlacedTile>();
        public List<PlacedTile> roofTiles = new List<PlacedTile>();
        public List<PlacedProp> props = new List<PlacedProp>();

        /// <summary>
        /// Capture the current state of a BuildingWorkshopData into this preset.
        /// </summary>
        public void CaptureFrom(BuildingWorkshopData source)
        {
            if (source == null) return;

            presetName = source.buildingName;
            gridWidth = source.gridWidth;
            gridHeight = source.gridHeight;
            tileSize = source.tileSize;

            floorTiles = DeepCopyTiles(source.floorTiles);
            wallTiles = DeepCopyTiles(source.wallTiles);
            roofTiles = DeepCopyTiles(source.roofTiles);
            props = DeepCopyProps(source.props);
        }

        /// <summary>
        /// Apply this preset onto a target BuildingWorkshopData, overwriting all content.
        /// </summary>
        public void ApplyTo(BuildingWorkshopData target)
        {
            if (target == null) return;

            target.buildingName = presetName;
            target.gridWidth = gridWidth;
            target.gridHeight = gridHeight;
            target.tileSize = tileSize;

            target.floorTiles = DeepCopyTiles(floorTiles);
            target.wallTiles = DeepCopyTiles(wallTiles);
            target.roofTiles = DeepCopyTiles(roofTiles);
            target.props = DeepCopyProps(props);
        }

        private static List<PlacedTile> DeepCopyTiles(List<PlacedTile> source)
        {
            List<PlacedTile> result = new List<PlacedTile>();
            foreach (PlacedTile t in source)
            {
                PlacedTile copy = new PlacedTile();
                copy.gridPosition = t.gridPosition;
                copy.tileDefinitionId = t.tileDefinitionId;
                copy.tileDefinition = t.tileDefinition;
                copy.rotation = t.rotation;
                copy.flipX = t.flipX;
                copy.materialOverride = t.materialOverride;
                result.Add(copy);
            }
            return result;
        }

        private static List<PlacedProp> DeepCopyProps(List<PlacedProp> source)
        {
            List<PlacedProp> result = new List<PlacedProp>();
            foreach (PlacedProp p in source)
            {
                PlacedProp copy = new PlacedProp();
                copy.instanceId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
                copy.gridPosition = p.gridPosition;
                copy.propDefinitionId = p.propDefinitionId;
                copy.propDefinition = p.propDefinition;
                copy.rotation = p.rotation;
                copy.freePlace = p.freePlace;
                copy.worldPosition = p.worldPosition;
                copy.yRotation = p.yRotation;
                copy.scale = p.scale;
                result.Add(copy);
            }
            return result;
        }
    }
}
