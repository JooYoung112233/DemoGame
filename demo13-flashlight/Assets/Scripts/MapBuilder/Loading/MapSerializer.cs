using UnityEngine;

namespace TopDownMapEditor
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
            public SerializableMapObject[] mapObjects;
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
            public int level;
            public string tileDefinitionId;
            public int rotation;
            public bool flipX;
            public float wallScale = 1f;
            // 자유 배치 벽 (스냅 OFF)
            public bool freePlace;
            public float wpx, wpy, wpz;
            public float yRotation;
            public string id;
        }

        [System.Serializable]
        class SerializableBuilding
        {
            public string instanceId;
            public int x, y;
            public string buildingDefinitionId;
            public int rotation;
            public string activeVariantId;
            public bool freePlace;
            public float worldX, worldY, worldZ;
            public float yRotation;
            public float scale;
            public int level;
        }

        [System.Serializable]
        class SerializableProp
        {
            public string instanceId;
            public int x, y;
            public string propDefinitionId;
            public int rotation;
            public bool freePlace;
            public float worldX, worldY, worldZ;
            public float yRotation;
            public float scale;
            public bool flipX;
            public bool wallMounted;
            public float mountHeight;
            public float groundOffX, groundOffY, groundOffZ;
            public string parentBuildingId;
            public int level;
        }

        [System.Serializable]
        class SerializableHarvestable
        {
            public string instanceId;
            public int x, y;
            public string harvestableDefinitionId;
            public string overrideSpawnCondition;
            public int level;
        }

        [System.Serializable]
        class SerializableMapObject
        {
            public string instanceId;
            public int objectType;
            public int x, y;
            public bool freePlace;
            public float worldX, worldY, worldZ;
            public float yRotation;
            public string label;
            public string customData;
            public float interactRange;
            public string promptText;

            // 스폰 설정
            public int spawnPointType;
            public bool useRegionLoot;
            public int containerGridWidth;
            public int containerGridHeight;
            public string containerName;
            public string enemyUnitKey;
            public int enemyCount;
            public string fixedItemId;
            public int fixedItemCount;

            // 문 설정
            public int doorLockType;
            public string doorKeyId;
            public bool doorConsumeKey;
            public string doorQuestId;

            // 트리거 설정
            public int triggerMode;
            public string triggerTargetScene;
            public string triggerTargetSpawnId;
            public bool triggerAutoEnter;
            public float triggerDelay;
            public bool triggerOneShot;
            public float triggerSizeX;
            public float triggerSizeY;
            public float triggerSizeZ;
            public float teleportX, teleportY, teleportZ;
            public float teleportYRot;
            public string triggerStorySceneId;

            // NPC 설정
            public string npcId;
            public string npcDisplayName;

            // 비주얼 모드
            public int visualMode;
            public string visualTexturePath;
            public string effectPrefabPath;
            public float visualScale;

            // 건물 소속
            public string parentBuildingId;

            // 층
            public int level;
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
                harvestables = new SerializableHarvestable[map.harvestables.Count],
                mapObjects = new SerializableMapObject[map.mapObjects.Count]
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
                        level = tile.level,
                        tileDefinitionId = tile.tileDefinitionId,
                        rotation = tile.rotation,
                        flipX = tile.flipX,
                        wallScale = tile.wallScale,
                        freePlace = tile.freePlace,
                        wpx = tile.worldPosition.x,
                        wpy = tile.worldPosition.y,
                        wpz = tile.worldPosition.z,
                        yRotation = tile.yRotation,
                        id = tile.id
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
                    activeVariantId = b.activeVariantId,
                    freePlace = b.freePlace,
                    worldX = b.worldPosition.x,
                    worldY = b.worldPosition.y,
                    worldZ = b.worldPosition.z,
                    yRotation = b.yRotation,
                    scale = b.scale,
                    level = b.level
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
                    rotation = p.rotation,
                    freePlace = p.freePlace,
                    worldX = p.worldPosition.x,
                    worldY = p.worldPosition.y,
                    worldZ = p.worldPosition.z,
                    yRotation = p.yRotation,
                    scale = p.scale,
                    flipX = p.flipX,
                    wallMounted = p.wallMounted,
                    mountHeight = p.mountHeight,
                    groundOffX = p.groundOffsetOverride.x,
                    groundOffY = p.groundOffsetOverride.y,
                    groundOffZ = p.groundOffsetOverride.z,
                    parentBuildingId = p.parentBuildingId,
                    level = p.level
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
                    overrideSpawnCondition = h.overrideSpawnCondition,
                    level = h.level
                };
            }

            for (int i = 0; i < map.mapObjects.Count; i++)
            {
                var mo = map.mapObjects[i];
                json.mapObjects[i] = new SerializableMapObject
                {
                    instanceId = mo.instanceId,
                    objectType = (int)mo.objectType,
                    x = mo.gridPosition.x,
                    y = mo.gridPosition.y,
                    freePlace = mo.freePlace,
                    worldX = mo.worldPosition.x,
                    worldY = mo.worldPosition.y,
                    worldZ = mo.worldPosition.z,
                    yRotation = mo.yRotation,
                    label = mo.label,
                    customData = mo.customData,
                    interactRange = mo.interactRange,
                    promptText = mo.promptText,
                    // 스폰 설정
                    spawnPointType = mo.spawnPointType,
                    useRegionLoot = mo.useRegionLoot,
                    containerGridWidth = mo.containerGridWidth,
                    containerGridHeight = mo.containerGridHeight,
                    containerName = mo.containerName,
                    enemyUnitKey = mo.enemyUnitKey,
                    enemyCount = mo.enemyCount,
                    fixedItemId = mo.fixedItemId,
                    fixedItemCount = mo.fixedItemCount,
                    // 문 설정
                    doorLockType = mo.doorLockType,
                    doorKeyId = mo.doorKeyId,
                    doorConsumeKey = mo.doorConsumeKey,
                    doorQuestId = mo.doorQuestId,
                    // 트리거 설정
                    triggerMode = mo.triggerMode,
                    triggerTargetScene = mo.triggerTargetScene,
                    triggerTargetSpawnId = mo.triggerTargetSpawnId,
                    triggerAutoEnter = mo.triggerAutoEnter,
                    triggerDelay = mo.triggerDelay,
                    triggerOneShot = mo.triggerOneShot,
                    triggerSizeX = mo.triggerSizeX,
                    triggerSizeY = mo.triggerSizeY,
                    triggerSizeZ = mo.triggerSizeZ,
                    teleportX = mo.teleportX,
                    teleportY = mo.teleportY,
                    teleportZ = mo.teleportZ,
                    teleportYRot = mo.teleportYRot,
                    triggerStorySceneId = mo.triggerStorySceneId,
                    // NPC 설정
                    npcId = mo.npcId,
                    npcDisplayName = mo.npcDisplayName,
                    // 비주얼 모드
                    visualMode = mo.visualMode,
                    visualTexturePath = mo.visualTexturePath,
                    effectPrefabPath = mo.effectPrefabPath,
                    visualScale = mo.visualScale,
                    parentBuildingId = mo.parentBuildingId,
                    level = mo.level
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
                                level = st.level,
                                tileDefinitionId = st.tileDefinitionId,
                                rotation = st.rotation,
                                flipX = st.flipX,
                                wallScale = st.wallScale <= 0f ? 1f : st.wallScale,
                                freePlace = st.freePlace,
                                worldPosition = new Vector3(st.wpx, st.wpy, st.wpz),
                                yRotation = st.yRotation,
                                id = st.id
                            });
                        }
                    }
                    target.layers.Add(layer);
                }
            }

            target.buildings.Clear();
            if (json.buildings != null)
            {
                foreach (var sb in json.buildings)
                {
                    target.buildings.Add(new PlacedBuilding
                    {
                        instanceId = sb.instanceId,
                        gridPosition = new Vector2Int(sb.x, sb.y),
                        buildingDefinitionId = sb.buildingDefinitionId,
                        rotation = sb.rotation,
                        activeVariantId = sb.activeVariantId,
                        freePlace = sb.freePlace,
                        worldPosition = new Vector3(sb.worldX, sb.worldY, sb.worldZ),
                        yRotation = sb.yRotation,
                        scale = sb.scale > 0 ? sb.scale : 1f,
                        level = sb.level
                    });
                }
            }

            target.props.Clear();
            if (json.props != null)
            {
                foreach (var sp in json.props)
                {
                    target.props.Add(new PlacedProp
                    {
                        instanceId = sp.instanceId,
                        gridPosition = new Vector2Int(sp.x, sp.y),
                        propDefinitionId = sp.propDefinitionId,
                        rotation = sp.rotation,
                        freePlace = sp.freePlace,
                        worldPosition = new Vector3(sp.worldX, sp.worldY, sp.worldZ),
                        yRotation = sp.yRotation,
                        scale = sp.scale > 0 ? sp.scale : 1f,
                        flipX = sp.flipX,
                        wallMounted = sp.wallMounted,
                        mountHeight = sp.mountHeight,
                        groundOffsetOverride = new Vector3(sp.groundOffX, sp.groundOffY, sp.groundOffZ),
                        parentBuildingId = sp.parentBuildingId,
                        level = sp.level
                    });
                }
            }

            target.harvestables.Clear();
            if (json.harvestables != null)
            {
                foreach (var sh in json.harvestables)
                {
                    target.harvestables.Add(new PlacedHarvestable
                    {
                        instanceId = sh.instanceId,
                        gridPosition = new Vector2Int(sh.x, sh.y),
                        harvestableDefinitionId = sh.harvestableDefinitionId,
                        overrideSpawnCondition = sh.overrideSpawnCondition,
                        level = sh.level
                    });
                }
            }

            target.mapObjects.Clear();
            if (json.mapObjects != null)
            {
                foreach (var smo in json.mapObjects)
                {
                    target.mapObjects.Add(new PlacedMapObject
                    {
                        instanceId = smo.instanceId,
                        objectType = (MapObjectType)smo.objectType,
                        gridPosition = new Vector2Int(smo.x, smo.y),
                        freePlace = smo.freePlace,
                        worldPosition = new Vector3(smo.worldX, smo.worldY, smo.worldZ),
                        yRotation = smo.yRotation,
                        label = smo.label,
                        customData = smo.customData,
                        interactRange = smo.interactRange > 0 ? smo.interactRange : 2f,
                        promptText = smo.promptText,
                        // 스폰 설정
                        spawnPointType = smo.spawnPointType,
                        useRegionLoot = smo.useRegionLoot,
                        containerGridWidth = smo.containerGridWidth > 0 ? smo.containerGridWidth : 4,
                        containerGridHeight = smo.containerGridHeight > 0 ? smo.containerGridHeight : 5,
                        containerName = smo.containerName,
                        enemyUnitKey = smo.enemyUnitKey,
                        enemyCount = smo.enemyCount > 0 ? smo.enemyCount : 1,
                        fixedItemId = smo.fixedItemId,
                        fixedItemCount = smo.fixedItemCount > 0 ? smo.fixedItemCount : 1,
                        // 문 설정
                        doorLockType = smo.doorLockType,
                        doorKeyId = smo.doorKeyId,
                        doorConsumeKey = smo.doorConsumeKey,
                        doorQuestId = smo.doorQuestId,
                        // NPC 설정
                        // 트리거 설정
                        triggerMode = smo.triggerMode,
                        triggerTargetScene = smo.triggerTargetScene,
                        triggerTargetSpawnId = smo.triggerTargetSpawnId,
                        triggerAutoEnter = smo.triggerAutoEnter,
                        triggerDelay = smo.triggerDelay,
                        triggerOneShot = smo.triggerOneShot,
                        triggerSizeX = smo.triggerSizeX > 0 ? smo.triggerSizeX : 1.5f,
                        triggerSizeY = smo.triggerSizeY > 0 ? smo.triggerSizeY : 2f,
                        triggerSizeZ = smo.triggerSizeZ > 0 ? smo.triggerSizeZ : 1.5f,
                        teleportX = smo.teleportX,
                        teleportY = smo.teleportY,
                        teleportZ = smo.teleportZ,
                        teleportYRot = smo.teleportYRot,
                        triggerStorySceneId = smo.triggerStorySceneId,
                        npcId = smo.npcId,
                        npcDisplayName = smo.npcDisplayName,
                        // 비주얼 모드
                        visualMode = smo.visualMode,
                        visualTexturePath = smo.visualTexturePath,
                        effectPrefabPath = smo.effectPrefabPath,
                        visualScale = smo.visualScale > 0.01f ? smo.visualScale : 1f,
                        parentBuildingId = smo.parentBuildingId,
                        level = smo.level
                    });
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
