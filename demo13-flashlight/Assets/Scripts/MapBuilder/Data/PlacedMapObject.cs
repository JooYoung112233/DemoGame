using UnityEngine;

namespace TopDownMapEditor
{
    public enum MapObjectType
    {
        // 기본 맵 오브젝트
        SpawnPoint,       // 플레이어 스폰 지점
        EscapePoint,      // 탈출구 (→ InteractType.ExitPoint)
        LootContainer,    // 루팅 상자 (→ InteractType.Container)
        EnemySpawn,       // 적 스폰 지점
        ItemDrop,         // 바닥 아이템 (→ InteractType.Pickup)
        Trigger,          // 트리거 영역
        Custom,           // 커스텀

        // InteractableObject 확장
        NPC,              // NPC (→ InteractType.NPC)
        Note,             // 쪽지/문서 (→ InteractType.Note)
        Bed,              // 침대 (→ InteractType.Bed)
        Workbench,        // 작업대 (→ InteractType.Workbench)
        MapBoard,         // 지도판 (→ InteractType.MapBoard)
        MedicalBench,     // 의료대 (→ InteractType.MedicalBench)
        CookingBench,     // 조리대 (→ InteractType.CookingBench)
        GenericInteract,  // 범용 상호작용 (→ InteractType.Generic)
        Door,             // 문 (→ InteractType.Door) — 잠금/열쇠/퀘스트/스위치
        Stairs,           // 계단 — 층(level) 이동 트리거
    }

    [System.Serializable]
    public class PlacedMapObject
    {
        public string instanceId;
        public MapObjectType objectType;
        public Vector2Int gridPosition;
        public bool freePlace;
        public Vector3 worldPosition;
        public float yRotation;
        public string label;
        public string customData;

        // InteractableObject 설정
        public float interactRange = 2f;
        public string promptText;

        // ── 스폰 설정 (LootContainer / ItemDrop) ──

        /// <summary>ItemSpawnPoint.SpawnType 매핑 (0=Ground, 1=Container, 2=Fixed)</summary>
        public int spawnPointType;

        /// <summary>지역 루트 테이블 사용 여부</summary>
        public bool useRegionLoot = true;

        /// <summary>루팅 상자 격자 가로 (Container 전용)</summary>
        public int containerGridWidth = 4;

        /// <summary>루팅 상자 격자 세로 (Container 전용)</summary>
        public int containerGridHeight = 5;

        /// <summary>루팅 상자 이름 (Container 전용)</summary>
        public string containerName;

        /// <summary>적 유닛 키 (EnemySpawn 전용)</summary>
        public string enemyUnitKey;

        /// <summary>적 스폰 수 (EnemySpawn 전용)</summary>
        public int enemyCount = 1;

        /// <summary>고정 아이템 ID (Fixed 전용)</summary>
        public string fixedItemId;

        /// <summary>고정 아이템 수량 (Fixed 전용)</summary>
        public int fixedItemCount = 1;

        // ── 문 설정 (Door 전용) ──

        /// <summary>잠금 타입 (0=None, 1=Key, 2=Quest, 3=Switch)</summary>
        public int doorLockType;

        /// <summary>필요 열쇠 아이템 ID (Key 타입 전용)</summary>
        public string doorKeyId;

        /// <summary>열쇠 사용 시 소모 여부</summary>
        public bool doorConsumeKey = true;

        /// <summary>필요 퀘스트 ID (Quest 타입 전용)</summary>
        public string doorQuestId;

        // ── 비주얼 모드 ──

        /// <summary>비주얼 모드 (0=Sphere, 1=TextureQuad, 2=Invisible, 3=EffectPrefab)</summary>
        public int visualMode;

        /// <summary>텍스처 Quad용 텍스처 경로 (Resources 상대 경로)</summary>
        public string visualTexturePath;

        /// <summary>이펙트 프리팹 경로 (Resources 상대 경로)</summary>
        public string effectPrefabPath;

        /// <summary>비주얼 스케일</summary>
        public float visualScale = 1f;

        // ── 트리거 설정 (Trigger 전용) ──

        /// <summary>트리거 모드 (0=SceneTransition, 1=LocalTeleport, 2=StoryTrigger, 3=CustomEvent)</summary>
        public int triggerMode;

        /// <summary>전환 대상 씬 이름 (SceneTransition)</summary>
        public string triggerTargetScene;

        /// <summary>전환 대상 스폰 포인트 ID (SceneTransition)</summary>
        public string triggerTargetSpawnId;

        /// <summary>자동 입장 여부 (true면 E키 없이 즉시)</summary>
        public bool triggerAutoEnter;

        /// <summary>전환 지연 시간 (0이면 즉시)</summary>
        public float triggerDelay;

        /// <summary>1회만 발동</summary>
        public bool triggerOneShot;

        /// <summary>트리거 콜라이더 크기</summary>
        public float triggerSizeX = 1.5f;
        public float triggerSizeY = 2f;
        public float triggerSizeZ = 1.5f;

        /// <summary>텔레포트 대상 좌표 (LocalTeleport)</summary>
        public float teleportX, teleportY, teleportZ;
        public float teleportYRot;

        /// <summary>스토리 씬 ID (StoryTrigger)</summary>
        public string triggerStorySceneId;

        // ── NPC 설정 (NPC 전용) ──

        /// <summary>NPCData.npcId (Resources/Data/NPC/{npcId} 로드용)</summary>
        public string npcId;

        /// <summary>NPC 표시 이름 (NPCData가 없을 때 폴백)</summary>
        public string npcDisplayName;

        // ── 건물 소속 ──

        /// <summary>소속 건물 instanceId. 비어있으면 외부 오브젝트 (항상 표시)</summary>
        public string parentBuildingId;

        /// <summary>층 인덱스. 0=1층 ... 월드 Y = level * GridSettings.levelHeight.</summary>
        public int level;

        /// <summary>이 층의 바닥 월드 Y 오프셋. 비주얼 배치 시 GetWorldPosition에 더한다(거리 탐색은 평면 유지).</summary>
        public float ElevationY(GridSettings settings) => level * (settings != null ? settings.levelHeight : 0f);

        public Vector3 GetWorldPosition(GridSettings settings)
        {
            return freePlace ? worldPosition : TopDownGrid.GridToWorld(gridPosition, settings);
        }

        /// <summary>이 타입이 InteractableObject로 변환 가능한지</summary>
        public bool IsInteractable => objectType switch
        {
            MapObjectType.EscapePoint => true,
            MapObjectType.LootContainer => true,
            MapObjectType.ItemDrop => true,
            MapObjectType.NPC => true,
            MapObjectType.Note => true,
            MapObjectType.Bed => true,
            MapObjectType.Workbench => true,
            MapObjectType.MapBoard => true,
            MapObjectType.MedicalBench => true,
            MapObjectType.CookingBench => true,
            MapObjectType.GenericInteract => true,
            MapObjectType.Door => true,
            _ => false
        };
    }
}
