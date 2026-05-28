using UnityEngine;

namespace IsometricMapEditor
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

        // ── NPC 설정 (NPC 전용) ──

        /// <summary>NPCData.npcId (Resources/Data/NPC/{npcId} 로드용)</summary>
        public string npcId;

        /// <summary>NPC 표시 이름 (NPCData가 없을 때 폴백)</summary>
        public string npcDisplayName;

        public Vector3 GetWorldPosition(GridSettings settings)
        {
            return freePlace ? worldPosition : IsometricGrid.GridToWorld(gridPosition, settings);
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
