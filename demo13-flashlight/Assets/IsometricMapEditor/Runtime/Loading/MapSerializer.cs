using UnityEngine;

namespace IsometricMapEditor
{
    public static class MapSerializer
    {
        [System.Serializable]
        class MapDataJson
        {
            public string mapName;
            public string mapId;
            public GridSettings gridSettings;
            public SerializableLayer[] layers;
            public SerializableBuilding[] buildings;
            public SerializableProp[] props;
            public SerializableHarvestable[] harvestables;
            public int[] walkabilityCells;
            public int walkabilityWidth;
            public int walkabilityHeight;
        }

        [System.Serializable]
        class SerializableLayer
        {
            public string layerName;
            public int sortingLayerOffset;
            public bool isVisible;
            public SerializableTile[] tiles;
        }

        [System.Serializable]
        class SerializableTile
        {
            public int x, y;
            public string tileDefinitionId;
            public int rotation;
            public bool flipX;
        }

        [System.Serializable]
        class SerializableBuilding
        {
            public string instanceId;
            public int x, y;
            public string buildingDefinitionId;
            public int rotation;
            public string activeVariantId;
        }

        [System.Serializable]
        class SerializableProp
        {
            public string instanceId;
            public int x, y;
            public string propDefinitionId;
            public int rotation;
        }

        [System.Serializable]
        class SerializableHarvestable
        {
            public string instanceId;
            public int x, y;
            public string harvestableDefinitionId;
            public string overrideSpawnCondition;
        }

        public static string Serialize(MapData map)
        {
            var json = new MapDataJson
            {
                mapName = map.mapName,
                mapId = map.mapId,
                gridSettings = map.gridSettings,
                layers = new SerializableLayer[map.layers.Count],
                buildings = new SerializableBuilding[map.buildings.Count],
                props = new SerializableProp[map.props.Count],
                harvestables = new SerializableHarvestable[map.harvestables.Count]
            };

            for (int i = 0; i < map.layers.Count; i++)
            {
                var layer = map.layers[i];
                var sl = new SerializableLayer
                {
                    layerName = layer.layerName,
                    sortingLayerOffset = layer.sortingLayerOffset,
                    isVisible = layer.isVisible,
                    tiles = new SerializableTile[layer.tiles.Count]
                };
                for (int j = 0; j < layer.tiles.Count; j++)
                {
                    var tile = layer.tiles[j];
                    sl.tiles[j] = new SerializableTile
                    {
                        x = tile.gridPosition.x,
                        y = tile.gridPosition.y,
                        tileDefinitionId = tile.tileDefinitionId,
                        rotation = tile.rotation,
                        flipX = tile.flipX
                    };
                }
                json.layers[i] = sl;
            }

            for (int i = 0; i < map.buildings.Count; i++)
            {
                var b = map.buildings[i];
                json.buildings[i] = new SerializableBuilding
                {
                    instanceId = b.instanceId,
                    x = b.gridPosition.x,
                    y = b.gridPosition.y,
                    buildingDefinitionId = b.buildingDefinitionId,
                    rotation = b.rotation,
                    activeVariantId = b.activeVariantId
                };
            }

            for (int i = 0; i < map.props.Count; i++)
            {
                var p = map.props[i];
                json.props[i] = new SerializableProp
                {
                    instanceId = p.instanceId,
                    x = p.gridPosition.x,
                    y = p.gridPosition.y,
                    propDefinitionId = p.propDefinitionId,
                    rotation = p.rotation
                };
            }

            for (int i = 0; i < map.harvestables.Count; i++)
            {
                var h = map.harvestables[i];
                json.harvestables[i] = new SerializableHarvestable
                {
                    instanceId = h.instanceId,
                    x = h.gridPosition.x,
                    y = h.gridPosition.y,
                    harvestableDefinitionId = h.harvestableDefinitionId,
                    overrideSpawnCondition = h.overrideSpawnCondition
                };
            }

            if (map.walkability != null)
            {
                json.walkabilityWidth = map.walkability.width;
                json.walkabilityHeight = map.walkability.height;
                json.walkabilityCells = new int[map.walkability.cells.Length];
                for (int i = 0; i < map.walkability.cells.Length; i++)
                    json.walkabilityCells[i] = (int)map.walkability.cells[i];
            }

            return JsonUtility.ToJson(json, true);
        }

        public static void Deserialize(string jsonString, MapData target)
        {
            var json = JsonUtility.FromJson<MapDataJson>(jsonString);
            target.mapName = json.mapName;
            target.mapId = json.mapId;
            target.gridSettings = json.gridSettings;

            target.layers.Clear();
            if (json.layers != null)
            {
                foreach (var sl in json.layers)
                {
                    var layer = new MapLayer
                    {
                        layerName = sl.layerName,
                        sortingLayerOffset = sl.sortingLayerOffset,
                        isVisible = sl.isVisible
                    };
                    if (sl.tiles != null)
                    {
                        foreach (var st in sl.tiles)
                        {
                            layer.tiles.Add(new PlacedTile
                            {
                                gridPosition = new Vector2Int(st.x, st.y),
                                tileDefinitionId = st.tileDefinitionId,
                                rotation = st.rotation,
                                flipX = st.flipX
                            });
                        }
                    }
                    target.layers.Add(layer);
                }
            }

            if (json.walkabilityCells != null && json.walkabilityWidth > 0)
            {
                target.walkability = new WalkabilityData(json.walkabilityWidth, json.walkabilityHeight);
                for (int i = 0; i < json.walkabilityCells.Length; i++)
                    target.walkability.cells[i] = (WalkableType)json.walkabilityCells[i];
            }

            target.MarkDirty();
        }
    }
}
