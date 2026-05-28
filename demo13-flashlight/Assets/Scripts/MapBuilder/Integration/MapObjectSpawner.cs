using System.Collections.Generic;
using UnityEngine;

namespace IsometricMapEditor
{
    /// <summary>
    /// MapData의 PlacedMapObject를 런타임 GameObject로 변환.
    /// InteractableObject 타입이면 해당 컴포넌트를 자동 부착 및 설정.
    /// LootContainer/ItemDrop → ItemSpawnPoint 자동 부착.
    /// EnemySpawn → EnemyController 생성.
    /// 맵 스폰 프로파일이 있으면 MapSpawnController 자동 부착.
    /// </summary>
    public class MapObjectSpawner : MonoBehaviour
    {
        Transform _root;
        readonly List<GameObject> _spawnedObjects = new();

        public void Initialize(Transform parent)
        {
            _root = parent;
        }

        public void SpawnMapObjects(List<PlacedMapObject> mapObjects, GridSettings gridSettings)
        {
            if (mapObjects == null) return;

            foreach (var obj in mapObjects)
            {
                var go = SpawnSingle(obj, gridSettings);
                if (go != null)
                    _spawnedObjects.Add(go);
            }

            // 맵에 LootContainer/ItemDrop이 있으면 MapSpawnController 자동 생성
            TryCreateMapSpawnController();

            Debug.Log($"[MapObjectSpawner] Spawned {_spawnedObjects.Count} map objects");
        }

        GameObject SpawnSingle(PlacedMapObject obj, GridSettings gridSettings)
        {
            Vector3 worldPos = obj.GetWorldPosition(gridSettings);

            var go = new GameObject($"MapObj_{obj.objectType}_{obj.instanceId}");
            go.transform.SetParent(_root);
            go.transform.position = worldPos;
            // Y회전을 기본 아이소메트릭 Root 회전에 곱함
            go.transform.rotation = Quaternion.Euler(35.264f, 45f, 0);
            if (Mathf.Abs(obj.yRotation) > 0.01f)
                go.transform.rotation = Quaternion.Euler(0, obj.yRotation, 0) * go.transform.rotation;

            // InteractableObject 타입이면 컴포넌트 부착
            if (obj.IsInteractable)
            {
                var interactable = go.AddComponent<InteractableObject>();
                int interactTypeInt = MapBuilderManager.MapObjectTypeToInteractType(obj.objectType);
                string prompt = !string.IsNullOrEmpty(obj.promptText) ? obj.promptText : obj.objectType.ToString();
                float range = obj.interactRange > 0 ? obj.interactRange : 2f;

                interactable.Configure(
                    (InteractableObject.InteractType)interactTypeInt,
                    prompt,
                    range
                );
            }

            // 타입별 추가 처리
            switch (obj.objectType)
            {
                case MapObjectType.SpawnPoint:
                    SetupSpawnPoint(go, obj);
                    break;

                case MapObjectType.LootContainer:
                    SetupLootContainer(go, obj);
                    break;

                case MapObjectType.ItemDrop:
                    SetupItemDrop(go, obj);
                    break;

                case MapObjectType.EnemySpawn:
                    SetupEnemySpawn(go, obj);
                    break;

                case MapObjectType.NPC:
                    SetupNPC(go, obj);
                    break;

                case MapObjectType.Door:
                    SetupDoor(go, obj);
                    break;
            }

            return go;
        }

        /// <summary>SpawnPoint 컴포넌트 설정</summary>
        void SetupSpawnPoint(GameObject go, PlacedMapObject obj)
        {
            var sp = go.AddComponent<SpawnPoint>();
            string spawnId = !string.IsNullOrEmpty(obj.customData) ? obj.customData : (obj.label ?? "default");
            var pointIdField = typeof(SpawnPoint).GetField("pointId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (pointIdField != null)
                pointIdField.SetValue(sp, spawnId);
        }

        /// <summary>LootContainer + ItemSpawnPoint 설정</summary>
        void SetupLootContainer(GameObject go, PlacedMapObject obj)
        {
            // LootContainer 컴포넌트
            var lc = go.AddComponent<LootContainer>();
            SetPrivateField(lc, "gridWidth", obj.containerGridWidth > 0 ? obj.containerGridWidth : 4);
            SetPrivateField(lc, "gridHeight", obj.containerGridHeight > 0 ? obj.containerGridHeight : 5);
            if (!string.IsNullOrEmpty(obj.containerName))
                SetPrivateField(lc, "containerName", obj.containerName);

            // ItemSpawnPoint 컴포넌트 (Container 타입)
            var isp = go.AddComponent<ItemSpawnPoint>();
            SetPrivateField(isp, "spawnType", ItemSpawnPoint.SpawnType.Container);
            SetPrivateField(isp, "useRegionLoot", obj.useRegionLoot);
            SetPrivateField(isp, "linkedContainer", lc);

            // 고정 아이템이 지정되어 있으면 Fixed 타입으로 전환
            if (!string.IsNullOrEmpty(obj.fixedItemId))
            {
                SetPrivateField(isp, "spawnType", ItemSpawnPoint.SpawnType.Fixed);
                var itemData = LoadItemData(obj.fixedItemId);
                if (itemData != null)
                {
                    SetPrivateField(isp, "fixedItem", itemData);
                    SetPrivateField(isp, "fixedCount", obj.fixedItemCount > 0 ? obj.fixedItemCount : 1);
                }
            }

            // 시각적 표시 (간단한 큐브)
            AddContainerVisual(go);
        }

        /// <summary>바닥 아이템 스폰 포인트 설정</summary>
        void SetupItemDrop(GameObject go, PlacedMapObject obj)
        {
            var isp = go.AddComponent<ItemSpawnPoint>();

            if (!string.IsNullOrEmpty(obj.fixedItemId))
            {
                // 고정 아이템 지정 시 Fixed
                SetPrivateField(isp, "spawnType", ItemSpawnPoint.SpawnType.Fixed);
                var itemData = LoadItemData(obj.fixedItemId);
                if (itemData != null)
                {
                    SetPrivateField(isp, "fixedItem", itemData);
                    SetPrivateField(isp, "fixedCount", obj.fixedItemCount > 0 ? obj.fixedItemCount : 1);
                }
            }
            else
            {
                // 지역 루트 기반 바닥 스폰
                SetPrivateField(isp, "spawnType", ItemSpawnPoint.SpawnType.Ground);
                SetPrivateField(isp, "useRegionLoot", obj.useRegionLoot);
            }
        }

        /// <summary>적 스폰 포인트 설정</summary>
        void SetupEnemySpawn(GameObject go, PlacedMapObject obj)
        {
            // EnemySpawn 마커에 유닛 키와 수량 저장
            // customData에 unitKey 기록 (기존 호환)
            if (!string.IsNullOrEmpty(obj.enemyUnitKey))
                obj.customData = obj.enemyUnitKey;

            // 런타임에 EnemySpawnManager가 이 마커를 읽어 적을 생성
            // (EnemySpawnManager 구현은 별도)
        }

        /// <summary>문 + DoorController 설정</summary>
        void SetupDoor(GameObject go, PlacedMapObject obj)
        {
            var door = go.AddComponent<DoorController>();
            SetPrivateField(door, "lockType", (DoorController.LockType)obj.doorLockType);

            if (obj.doorLockType == 1) // Key
            {
                SetPrivateField(door, "requiredKeyId", obj.doorKeyId ?? "");
                SetPrivateField(door, "consumeKey", obj.doorConsumeKey);
            }
            else if (obj.doorLockType == 2) // Quest
            {
                SetPrivateField(door, "requiredQuestId", obj.doorQuestId ?? "");
            }

            // 물리 차단 콜라이더 (통과 방지)
            var blocker = go.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0, 0.5f, 0);
            blocker.size = new Vector3(1f, 1f, 0.2f);
            blocker.isTrigger = false;
            SetPrivateField(door, "doorCollider", blocker);

            // 문 비주얼 (간단한 큐브)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "DoorVisual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0, 0.5f, 0);
            visual.transform.localScale = new Vector3(0.8f, 1f, 0.12f);
            SetPrivateField(door, "doorVisual", visual);

            // 비주얼 콜라이더 제거 (blocker가 대신)
            var visualCol = visual.GetComponent<Collider>();
            if (visualCol != null) Destroy(visualCol);

            // 잠금 타입에 따른 비주얼 색상
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader.name == "Hidden/InternalErrorShader")
                    mat = new Material(Shader.Find("Standard"));

                mat.color = obj.doorLockType switch
                {
                    0 => new Color(0.5f, 0.35f, 0.2f),   // None: 갈색
                    1 => new Color(0.7f, 0.3f, 0.3f),     // Key: 붉은 갈색
                    2 => new Color(0.3f, 0.3f, 0.7f),     // Quest: 파란 갈색
                    3 => new Color(0.5f, 0.5f, 0.3f),     // Switch: 노란 갈색
                    _ => new Color(0.5f, 0.35f, 0.2f)
                };
                renderer.sharedMaterial = mat;
            }
        }

        /// <summary>NPC 컨트롤러 설정</summary>
        void SetupNPC(GameObject go, PlacedMapObject obj)
        {
            var npcCtrl = go.AddComponent<NPCController>();

            // npcId 필드 우선, 폴백으로 customData / label 사용
            string npcId = !string.IsNullOrEmpty(obj.npcId) ? obj.npcId
                         : !string.IsNullOrEmpty(obj.customData) ? obj.customData
                         : obj.label;

            if (!string.IsNullOrEmpty(npcId))
            {
                var npcData = Resources.Load<NPCData>($"Data/NPC/{npcId}");
                if (npcData != null)
                    SetPrivateField(npcCtrl, "npcData", npcData);
                else
                    Debug.LogWarning($"[MapObjectSpawner] NPCData '{npcId}' 로드 실패 (Resources/Data/NPC/{npcId})");
            }

            // storyNpcId 설정 (비어있지 않으면)
            if (!string.IsNullOrEmpty(obj.npcDisplayName))
            {
                // npcDisplayName은 NPCData가 없을 때 폴백용이므로 별도 처리 불필요
                // (NPCData SO 자체에 displayName이 있음)
            }
        }

        /// <summary>맵에 스폰 포인트가 있으면 MapSpawnController 자동 생성</summary>
        void TryCreateMapSpawnController()
        {
            // 이미 존재하면 스킵
            if (FindFirstObjectByType<MapSpawnController>() != null) return;

            // ItemSpawnPoint가 있는지 확인
            var spawnPoints = FindObjectsByType<ItemSpawnPoint>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (spawnPoints.Length == 0) return;

            // MapSpawnController 생성
            var controllerGo = new GameObject("MapSpawnController");
            controllerGo.transform.SetParent(_root);
            var controller = controllerGo.AddComponent<MapSpawnController>();

            // 현재 지역에 맞는 프로파일 로드 시도
            string regionId = RegionLootCatalog.GetActiveRegionId();
            if (!string.IsNullOrEmpty(regionId))
            {
                var profile = Resources.Load<MapSpawnProfile>($"Data/MapSpawn/{regionId}");
                if (profile != null)
                    controller.SetProfile(profile);
            }

            Debug.Log($"[MapObjectSpawner] MapSpawnController 자동 생성 (region={regionId})");
        }

        /// <summary>LootContainer 시각적 표시용 큐브 추가</summary>
        void AddContainerVisual(GameObject go)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "ContainerVisual";
            cube.transform.SetParent(go.transform, false);
            cube.transform.localPosition = new Vector3(0, 0.3f, 0);
            cube.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            // 콜라이더는 InteractableObject의 트리거용으로 변환
            var col = cube.GetComponent<Collider>();
            if (col is BoxCollider box)
                box.isTrigger = true;
        }

        /// <summary>리플렉션으로 SerializeField에 값 설정</summary>
        static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(target, value);
        }

        /// <summary>ItemData SO를 Resources에서 로드</summary>
        static ItemData LoadItemData(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            // Items 폴더 내 모든 하위 폴더 검색
            var allItems = Resources.LoadAll<ItemData>("Items");
            foreach (var item in allItems)
            {
                if (item.itemId == itemId)
                    return item;
            }
            return null;
        }

        public void ClearAll()
        {
            foreach (var go in _spawnedObjects)
            {
                if (go != null)
                    Destroy(go);
            }
            _spawnedObjects.Clear();
        }
    }
}
