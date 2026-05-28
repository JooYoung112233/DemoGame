using System.Collections.Generic;
using UnityEngine;

namespace IsometricMapEditor
{
    /// <summary>
    /// A snapshot/template of a MapData that can be saved and loaded as a preset.
    /// Used for region templates: e.g. "Village", "Forest", "Desert" presets
    /// that can be applied to any MapData to quickly set up a region.
    /// </summary>
    [CreateAssetMenu(menuName = "Isometric Map/Map Preset")]
    public class MapPreset : ScriptableObject
    {
        [Header("Preset Info")]
        public string presetName;
        [TextArea(2, 4)]
        public string description;

        [Header("Grid")]
        public GridSettings gridSettings = new GridSettings();

        [Header("Data")]
        public List<PresetLayer> layers = new List<PresetLayer>();
        public List<PlacedBuilding> buildings = new List<PlacedBuilding>();
        public List<PlacedProp> props = new List<PlacedProp>();

        [Header("Walkability")]
        public WalkabilityData walkability;

        /// <summary>
        /// Capture the current state of a MapData into this preset.
        /// </summary>
        public void CaptureFrom(MapData source)
        {
            if (source == null) return;

            presetName = source.mapName;
            gridSettings = new GridSettings(source.gridSettings.tileSize,
                source.gridSettings.mapWidth, source.gridSettings.mapHeight);
            gridSettings.originOffset = source.gridSettings.originOffset;

            // Deep-copy layers
            layers.Clear();
            foreach (MapLayer srcLayer in source.layers)
            {
                PresetLayer pl = new PresetLayer();
                pl.layerName = srcLayer.layerName;
                pl.sortingLayerOffset = srcLayer.sortingLayerOffset;
                pl.isVisible = srcLayer.isVisible;
                pl.isLocked = srcLayer.isLocked;
                pl.tiles = new List<PlacedTile>();
                foreach (PlacedTile t in srcLayer.tiles)
                {
                    PlacedTile copy = new PlacedTile();
                    copy.gridPosition = t.gridPosition;
                    copy.tileDefinitionId = t.tileDefinitionId;
                    copy.tileDefinition = t.tileDefinition;
                    copy.rotation = t.rotation;
                    copy.flipX = t.flipX;
                    copy.materialOverride = t.materialOverride;
                    pl.tiles.Add(copy);
                }
                layers.Add(pl);
            }

            // Deep-copy buildings
            buildings.Clear();
            foreach (PlacedBuilding b in source.buildings)
            {
                PlacedBuilding copy = new PlacedBuilding();
                copy.instanceId = b.instanceId;
                copy.gridPosition = b.gridPosition;
                copy.buildingDefinitionId = b.buildingDefinitionId;
                copy.buildingDefinition = b.buildingDefinition;
                copy.rotation = b.rotation;
                copy.activeVariantId = b.activeVariantId;

                copy.freePlace = b.freePlace;
                copy.worldPosition = b.worldPosition;
                copy.yRotation = b.yRotation;
                copy.scale = b.scale;
                buildings.Add(copy);
            }

            // Deep-copy props
            props.Clear();
            foreach (PlacedProp p in source.props)
            {
                PlacedProp copy = new PlacedProp();
                copy.instanceId = p.instanceId;
                copy.gridPosition = p.gridPosition;
                copy.propDefinitionId = p.propDefinitionId;
                copy.propDefinition = p.propDefinition;
                copy.rotation = p.rotation;
                copy.activeVariantId = p.activeVariantId;
                copy.freePlace = p.freePlace;
                copy.worldPosition = p.worldPosition;
                copy.yRotation = p.yRotation;
                copy.scale = p.scale;
                props.Add(copy);
            }

            // Walkability
            if (source.walkability != null)
            {
                walkability = new WalkabilityData(source.gridSettings.mapWidth, source.gridSettings.mapHeight);
                walkability.CopyFrom(source.walkability);
            }
            else
            {
                walkability = null;
            }
        }

        /// <summary>
        /// Apply this preset onto a target MapData, overwriting all its content.
        /// </summary>
        public void ApplyTo(MapData target)
        {
            if (target == null) return;

            target.gridSettings.tileSize = gridSettings.tileSize;
            target.gridSettings.mapWidth = gridSettings.mapWidth;
            target.gridSettings.mapHeight = gridSettings.mapHeight;
            target.gridSettings.originOffset = gridSettings.originOffset;

            // Deep-copy layers
            target.layers.Clear();
            foreach (PresetLayer pl in layers)
            {
                MapLayer ml = new MapLayer();
                ml.layerName = pl.layerName;
                ml.sortingLayerOffset = pl.sortingLayerOffset;
                ml.isVisible = pl.isVisible;
                ml.isLocked = pl.isLocked;
                ml.tiles = new List<PlacedTile>();
                foreach (PlacedTile t in pl.tiles)
                {
                    PlacedTile copy = new PlacedTile();
                    copy.gridPosition = t.gridPosition;
                    copy.tileDefinitionId = t.tileDefinitionId;
                    copy.tileDefinition = t.tileDefinition;
                    copy.rotation = t.rotation;
                    copy.flipX = t.flipX;
                    copy.materialOverride = t.materialOverride;
                    ml.tiles.Add(copy);
                }
                target.layers.Add(ml);
            }

            // Deep-copy buildings
            target.buildings.Clear();
            foreach (PlacedBuilding b in buildings)
            {
                PlacedBuilding copy = new PlacedBuilding();
                copy.instanceId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
                copy.gridPosition = b.gridPosition;
                copy.buildingDefinitionId = b.buildingDefinitionId;
                copy.buildingDefinition = b.buildingDefinition;
                copy.rotation = b.rotation;
                copy.activeVariantId = b.activeVariantId;

                copy.freePlace = b.freePlace;
                copy.worldPosition = b.worldPosition;
                copy.yRotation = b.yRotation;
                copy.scale = b.scale;
                target.buildings.Add(copy);
            }

            // Deep-copy props
            target.props.Clear();
            foreach (PlacedProp p in props)
            {
                PlacedProp copy = new PlacedProp();
                copy.instanceId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
                copy.gridPosition = p.gridPosition;
                copy.propDefinitionId = p.propDefinitionId;
                copy.propDefinition = p.propDefinition;
                copy.rotation = p.rotation;
                copy.activeVariantId = p.activeVariantId;
                copy.freePlace = p.freePlace;
                copy.worldPosition = p.worldPosition;
                copy.yRotation = p.yRotation;
                copy.scale = p.scale;
                target.props.Add(copy);
            }

            // Walkability
            if (walkability != null)
            {
                target.walkability = new WalkabilityData(gridSettings.mapWidth, gridSettings.mapHeight);
                target.walkability.CopyFrom(walkability);
            }

            target.MarkDirty();
        }
    }

    /// <summary>
    /// Serializable copy of MapLayer for preset storage.
    /// </summary>
    [System.Serializable]
    public class PresetLayer
    {
        public string layerName;
        public int sortingLayerOffset;
        public bool isVisible = true;
        public bool isLocked;
        public List<PlacedTile> tiles = new List<PlacedTile>();
    }
}
