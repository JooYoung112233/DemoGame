# Unity 아이소메트릭 쿼터뷰 맵툴 설계 계획

> 작성일: 2026-05-20
> 형태: Unity Editor Extension
> 예상 기간: 14~22주 (1인 개발 기준)

---

## 1. 개요

아이소 쿼터뷰 게임용 올인원 맵 에디터. Unity Editor Extension으로 제작하며, 다음 기능을 포함한다.

| 카테고리 | 핵심 기능 |
|----------|-----------|
| 기본 배치 | 타일/건물/도로 배치 (커스텀 타일 크기) |
| 건물 상태 | 낮↔밤, 시즌, 파괴 등 상태별 비주얼 전환 |
| 인테리어 | 건물 진입 시 지붕 제거, 별도 내부 맵 전환 |
| 내부 연결 | 건물간 터널/스카이워크 연결 |
| Props | 가로등, 나무 등 오브젝트 배치 |
| 파밍 요소 | 확률/조건부 노출 (시간, 시즌, 퀘스트 등) |
| Walkability | 이동 가능/불가 영역 페인팅 |
| 탈출구 | 인테리어 내 탈출 포인트 (문, 창문, 비밀통로) |
| 연동 | 에디터 ↔ 인게임 맵 로드 자동 연동 |

---

## 2. 프로젝트 구조

```
Assets/IsometricMapEditor/
│
├── Editor/                                  # 에디터 전용 (빌드 제외)
│   ├── IsometricMapEditor.Editor.asmdef
│   │
│   ├── Windows/
│   │   ├── MapEditorWindow.cs               # 메인 에디터 창 (UI Toolkit)
│   │   ├── InteriorEditorWindow.cs          # 인테리어 맵 편집 창
│   │   ├── BuildingVariantEditorWindow.cs   # 상태 변환 편집
│   │   ├── ConnectionEditorWindow.cs        # 건물간 연결 그래프 뷰
│   │   └── TilePaletteWindow.cs             # 타일/건물 브러시 팔레트
│   │
│   ├── SceneView/
│   │   ├── MapSceneOverlay.cs               # Scene View 오버레이 툴바
│   │   ├── IsometricGridGizmoDrawer.cs      # 다이아몬드 그리드 시각화
│   │   ├── SceneViewInputHandler.cs         # 마우스→그리드 좌표 변환 + 입력
│   │   └── ConnectionGizmoDrawer.cs         # 진입점/연결 기즈모
│   │
│   ├── Inspectors/
│   │   ├── MapDataInspector.cs              # MapData 커스텀 에디터
│   │   ├── BuildingDefinitionInspector.cs   # 풋프린트 에디터, 변환 섹션
│   │   ├── InteriorMapDataInspector.cs      # 인테리어 맵 에디터
│   │   └── ConnectionPointDrawer.cs         # ConnectionPoint 프로퍼티 드로어
│   │
│   ├── Tools/
│   │   ├── PaintTool.cs                     # EditorTool: 타일 페인트
│   │   ├── EraseTool.cs                     # EditorTool: 지우기
│   │   ├── SelectTool.cs                    # EditorTool: 선택/이동
│   │   ├── ConnectionTool.cs                # EditorTool: 진입/출구 연결
│   │   ├── RoadTool.cs                      # EditorTool: 도로 (자동 연결)
│   │   ├── PropPlaceTool.cs                 # EditorTool: 오브젝트 배치
│   │   └── WalkabilityPaintTool.cs          # EditorTool: 이동 가능/불가 페인팅
│   │
│   └── Utilities/
│       ├── MapExporter.cs                   # SO → JSON 익스포트
│       ├── MapValidator.cs                  # 누락 연결, 겹침 검증
│       ├── UndoHelper.cs                    # Undo/Redo 통합
│       ├── MapSceneGenerator.cs             # 맵 데이터 → Unity Scene 자동 생성
│       ├── LivePreviewManager.cs            # 에디터 수정 → Scene 즉시 반영
│       └── BuildPreprocessor.cs             # 빌드 전 자동 검증/패키징
│
├── Runtime/                                 # 게임에 포함
│   ├── IsometricMapEditor.Runtime.asmdef
│   │
│   ├── Core/
│   │   ├── IsometricGrid.cs                 # 좌표 변환 (Grid ↔ World)
│   │   └── GridSettings.cs                  # 커스텀 타일 크기 설정
│   │
│   ├── Data/
│   │   ├── MapData.cs                       # SO: 전체 맵 데이터
│   │   ├── MapLayer.cs                      # 레이어 (Ground, Objects, Roof)
│   │   ├── TileDefinition.cs                # SO: 타일 타입 정의
│   │   ├── PlacedTile.cs                    # 배치된 타일 인스턴스
│   │   ├── BuildingDefinition.cs            # SO: 건물 템플릿
│   │   ├── PlacedBuilding.cs                # 배치된 건물 인스턴스
│   │   ├── BuildingVariantSet.cs            # SO: 상태 변환 세트
│   │   ├── BuildingVariant.cs               # 개별 상태 (스프라이트, 머테리얼, 라이트)
│   │   ├── RoadDefinition.cs                # SO: 도로 (오토타일링)
│   │   ├── PropDefinition.cs                # SO: 배치 오브젝트 (가로등, 나무 등)
│   │   ├── PlacedProp.cs                    # 배치된 오브젝트 인스턴스
│   │   ├── HarvestableDefinition.cs         # SO: 파밍 요소 (확률, 조건, 드롭)
│   │   ├── PlacedHarvestable.cs             # 배치된 파밍 요소 인스턴스
│   │   ├── SpawnCondition.cs                # 노출 조건 (확률, 시간, 퀘스트 등)
│   │   ├── InteriorMapData.cs               # SO: 인테리어 맵
│   │   ├── ConnectionPoint.cs               # 진입/출구 포인트
│   │   ├── InteriorConnection.cs            # 건물간 내부 연결
│   │   └── WalkabilityData.cs               # 셀별 이동 가능 여부 맵
│   │
│   ├── Rendering/
│   │   ├── IsometricSortingManager.cs       # 뎁스 소팅 (row+col 기반)
│   │   ├── TileRenderer.cs                  # 타일 GO 생성/풀링
│   │   ├── BuildingRenderer.cs              # 건물 스프라이트 + 상태 변환
│   │   ├── RoofController.cs                # 지붕 show/hide
│   │   └── MapChunkLoader.cs               # 청크 기반 로딩 (대형 맵)
│   │
│   ├── State/
│   │   ├── BuildingStateManager.cs          # 런타임 상태 전환 관리
│   │   ├── BuildingStateTransition.cs       # 전환 이펙트 (CrossFade 등)
│   │   └── StateConditionEvaluator.cs       # 조건 평가 (시간, 시즌, 데미지)
│   │
│   ├── Interior/
│   │   ├── InteriorTransitionManager.cs     # 외부→내부 전환 오케스트레이션
│   │   ├── InteriorMapLoader.cs             # 인테리어 맵 로드/언로드
│   │   ├── InteriorConnectionResolver.cs    # 건물간 터널 해석
│   │   └── RoofHideController.cs            # 근접 시 지붕 페이드
│   │
│   ├── Props/
│   │   ├── PropManager.cs                   # 배치 오브젝트 관리
│   │   ├── HarvestableManager.cs            # 파밍 요소 스폰/관리
│   │   └── SpawnConditionEvaluator.cs       # 확률/조건부 노출 평가
│   │
│   ├── Navigation/
│   │   ├── WalkabilityMap.cs                # 이동 가능 영역 런타임 데이터
│   │   ├── EscapePointManager.cs            # 탈출구 관리
│   │   └── PathfindingHelper.cs             # A* 경로 탐색 유틸
│   │
│   ├── Camera/
│   │   ├── IsometricCameraController.cs     # 팬/줌/팔로우
│   │   └── CameraTransitionHandler.cs       # 외부↔내부 카메라 전환
│   │
│   ├── Loading/
│   │   ├── MapLoader.cs                     # 맵 데이터 로드
│   │   ├── MapSerializer.cs                 # JSON 직렬화
│   │   └── MapDataConverter.cs              # SO→런타임 포맷 변환
│   │
│   └── Integration/
│       ├── MapRuntimeBootstrapper.cs        # 맵 로드→렌더링→시스템 초기화 원스톱
│       ├── MapSceneLinker.cs                # 맵 데이터 ↔ Unity Scene 1:1 연결
│       ├── EditorPlaySync.cs                # 에디터 수정 → Play 모드 즉시 반영
│       ├── MapBuildPipeline.cs              # 빌드 시 맵 데이터 자동 패키징
│       └── MapDebugOverlay.cs               # 런타임 디버그 (그리드/walkability 시각화)
│
└── Tests/
    ├── Editor/
    │   ├── IsometricMapEditor.Editor.Tests.asmdef
    │   ├── IsometricGridTests.cs            # 좌표 변환 round-trip
    │   └── MapExporterTests.cs              # 익스포트 검증
    └── Runtime/
        ├── IsometricMapEditor.Runtime.Tests.asmdef
        ├── BuildingStateTests.cs            # 상태 전환 로직
        └── SpawnConditionTests.cs           # 조건부 노출 로직
```

### 어셈블리 분리 (4개 asmdef)

| 어셈블리 | 참조 | 플랫폼 |
|----------|------|--------|
| `IsometricMapEditor.Runtime` | (없음) | Any |
| `IsometricMapEditor.Editor` | Runtime | Editor Only |
| `IsometricMapEditor.Runtime.Tests` | Runtime | Any (Test) |
| `IsometricMapEditor.Editor.Tests` | Runtime, Editor | Editor Only (Test) |

Runtime 어셈블리는 절대 UnityEditor를 참조하지 않음. 빌드 시 에디터 코드 자동 제외.

---

## 3. 핵심 데이터 모델

### 3.1 좌표 시스템 (커스텀 타일 크기)

```csharp
[System.Serializable]
public class GridSettings
{
    public int tileWidth = 128;       // 사용자 설정 가능
    public int tileHeight = 64;       // 사용자 설정 가능 (보통 width/2)
    public Vector2 originOffset;      // 그리드 원점 월드 좌표
    public int mapWidth = 64;         // 그리드 열 수
    public int mapHeight = 64;        // 그리드 행 수
}
```

```csharp
public static class IsometricGrid
{
    // Grid → World 변환
    public static Vector2 GridToWorld(Vector2Int cell, GridSettings s)
    {
        float x = (cell.x - cell.y) * (s.tileWidth * 0.5f);
        float y = (cell.x + cell.y) * (s.tileHeight * 0.5f);
        return new Vector2(x, -y) + s.originOffset;
    }

    // World → Grid 변환
    public static Vector2Int WorldToGrid(Vector2 worldPos, GridSettings s)
    {
        Vector2 local = worldPos - s.originOffset;
        local.y = -local.y;
        float col = (local.x / (s.tileWidth * 0.5f) + local.y / (s.tileHeight * 0.5f)) * 0.5f;
        float row = (local.y / (s.tileHeight * 0.5f) - local.x / (s.tileWidth * 0.5f)) * 0.5f;
        return new Vector2Int(Mathf.RoundToInt(col), Mathf.RoundToInt(row));
    }

    // 소팅 순서 (back-to-front)
    public static int GetSortingOrder(Vector2Int cell, int layer = 0)
    {
        return (cell.x + cell.y) * 10 + layer;
    }
}
```

### 3.2 건물 상태 전환 시스템

파괴뿐 아니라 낮/밤, 시즌 등 **범용 상태 전환 시스템**.

```csharp
[CreateAssetMenu(menuName = "Isometric Map/Building Variant Set")]
public class BuildingVariantSet : ScriptableObject
{
    public string variantSetId;
    public string defaultVariantId;
    public List<BuildingVariant> variants;
    public List<VariantTransitionRule> transitionRules;
}

[System.Serializable]
public class BuildingVariant
{
    public string variantId;              // "day", "night", "winter", "damaged_1"
    public string displayName;
    public Sprite baseSprite;             // 건물 본체 교체
    public Sprite roofSprite;             // 지붕 교체
    public Material materialOverride;     // 후처리/셰이더
    public Color tintColor = Color.white;
    public List<VariantAddon> addons;     // 추가 오브젝트 (라이트, 파티클)
    public AudioClip ambientSound;
    public float transitionDuration = 0.5f;
    public TransitionType transitionType; // Instant, CrossFade, Dissolve
}

[System.Serializable]
public class VariantAddon
{
    public GameObject prefab;             // 라이트, 파티클 등
    public Vector3 localPosition;
    public bool activeInThisVariant;
}

[System.Serializable]
public class VariantTransitionRule
{
    public string fromVariantId;          // "*" = 어떤 상태에서든
    public string toVariantId;
    public ConditionType conditionType;   // TimeOfDay, Season, DamageLevel, ...
    public string conditionParam;         // "18:00", "winter", "0.5"
    public bool autoTransition;           // 자동 전환 여부
}

public enum ConditionType  { TimeOfDay, Season, DamageLevel, Occupancy, Weather, Custom }
public enum TransitionType { Instant, CrossFade, Dissolve, SlideDown }
```

**전환 룰 예시:**
| from | to | 조건 | 자동 |
|------|----|------|------|
| `*` | `night` | TimeOfDay >= 18:00 | O |
| `night` | `day` | TimeOfDay >= 06:00 | O |
| `*` | `winter` | Season == winter | O |
| `*` | `damaged_1` | DamageLevel >= 0.5 | O |

### 3.3 인테리어 시스템

```csharp
[CreateAssetMenu(menuName = "Isometric Map/Interior Map Data")]
public class InteriorMapData : ScriptableObject
{
    public string interiorId;
    public string displayName;
    public GridSettings gridSettings;           // 내부는 별도 그리드 스케일 가능
    public List<MapLayer> layers;
    public List<PlacedTile> tiles;
    public List<PlacedProp> props;              // 내부 오브젝트 (가구, 가로등 등)
    public List<PlacedHarvestable> harvestables; // 내부 파밍 요소
    public WalkabilityData walkability;         // 내부 이동 가능 영역
    public List<ConnectionPoint> connectionPoints;
    public List<EscapePoint> escapePoints;      // 탈출구
    public Rect cameraBounds;                   // 내부 카메라 범위
}

[System.Serializable]
public class ConnectionPoint
{
    public string connectionId;
    public string label;                        // "정문", "뒷문"
    public Vector2Int gridPosition;
    public ConnectionDirection direction;
    public ConnectionPointType type;            // Entry, Exit, Bidirectional
    public string targetMapId;                  // 연결 대상 맵 ID
    public string targetConnectionId;           // 연결 대상 포인트 ID
    public Sprite doorSprite;
    public bool showIndicator = true;
}

[System.Serializable]
public class InteriorConnection
{
    public string connectionId;
    public string sourceBuildingId;
    public string sourceConnectionPointId;
    public string targetBuildingId;
    public string targetConnectionPointId;
    public string transitionType;               // "tunnel", "skywalk", "underground"
    public float transitionDuration;
}
```

### 3.4 배치 오브젝트 (Props)

외부/내부 맵 모두에 가로등, 나무, 표지판, 장식물 등 배치.

```csharp
[CreateAssetMenu(menuName = "Isometric Map/Prop Definition")]
public class PropDefinition : ScriptableObject
{
    public string propId;
    public string displayName;
    public Sprite sprite;
    public Vector2Int footprint;              // 차지하는 셀 수
    public bool blocksWalkability;            // 배치 시 자동 이동 불가 처리
    public bool hasVariants;                  // 상태 전환 가능 (가로등 on/off)
    public BuildingVariantSet variants;       // 재사용: 낮=꺼짐, 밤=켜짐
    public int sortingOffset;
}

[System.Serializable]
public class PlacedProp
{
    public Vector2Int gridPosition;
    public string propDefinitionId;
    public int rotation;
    public string instanceId;
}
```

### 3.5 파밍 요소 (Harvestable)

확률/조건에 따라 노출 여부가 달라지는 채집 가능 오브젝트.

```csharp
[CreateAssetMenu(menuName = "Isometric Map/Harvestable Definition")]
public class HarvestableDefinition : ScriptableObject
{
    public string harvestableId;
    public string displayName;
    public Sprite sprite;
    public Sprite depletedSprite;             // 채집 후 모습

    // 드롭 테이블
    public List<DropEntry> dropTable;

    // 리스폰
    public float respawnTimeSec;              // 0 = 1회성
    public bool respawnOnMapReload;

    // 노출 조건 (모두 충족해야 노출)
    public List<SpawnCondition> spawnConditions;
}

[System.Serializable]
public class DropEntry
{
    public string itemId;
    public int minAmount;
    public int maxAmount;
    public float dropRate;                    // 0.0 ~ 1.0
}

[System.Serializable]
public class SpawnCondition
{
    public SpawnConditionType type;
    public string param;
}

public enum SpawnConditionType
{
    Always,          // 항상 노출
    Probability,     // 확률 기반        param: "0.3" → 30%
    TimeOfDay,       // 시간대           param: "18:00-06:00" → 밤에만
    Season,          // 시즌             param: "winter"
    Weather,         // 날씨             param: "rain"
    QuestComplete,   // 퀘스트 완료      param: "quest_01"
    PlayerLevel,     // 플레이어 레벨    param: ">=10"
    Custom           // 커스텀 플래그    param: "my_flag"
}
```

### 3.6 이동 가능 영역 (Walkability)

셀 단위 이동 가능/불가 설정. 에디터에서 컬러 오버레이로 시각화.

```csharp
[System.Serializable]
public class WalkabilityData
{
    public int width;
    public int height;
    public WalkableType[] cells;              // width * height 배열

    public WalkableType GetCell(Vector2Int pos) => cells[pos.y * width + pos.x];
    public void SetCell(Vector2Int pos, WalkableType type) => cells[pos.y * width + pos.x] = type;
}

public enum WalkableType
{
    Walkable,        // 이동 가능        (초록)
    Blocked,         // 이동 불가        (빨강)
    SlowZone,        // 감속 지역        (노랑)
    Hazard,          // 데미지 지역      (주황)
    TriggerZone      // 이벤트 트리거    (파랑)
}
```

### 3.7 탈출구 시스템

인테리어 맵 내 탈출 포인트.

```csharp
[System.Serializable]
public class EscapePoint
{
    public string escapeId;
    public string label;                      // "비상구", "창문", "지하 탈출구"
    public Vector2Int gridPosition;
    public EscapeType type;
    public string targetMapId;                // 나가면 어디로?
    public string targetConnectionId;
    public bool requiresKey;                  // 열쇠 필요 여부
    public string requiredKeyItemId;
    public bool isHidden;                     // 발견해야 사용 가능
    public List<SpawnCondition> availableConditions;  // 조건부 사용
}

public enum EscapeType
{
    Door,            // 일반 문
    Window,          // 창문 (탈출만 가능, 진입 불가)
    SecretPassage,   // 비밀 통로 (발견 필요)
    Emergency        // 비상구 (항상 표시)
}
```

---

## 4. 맵툴 ↔ 인게임 연동 시스템

### 연동 파이프라인 전체도

```
[맵툴 에디터] ─→ ScriptableObject ─┬─→ [LivePreviewManager] ─→ Scene View 즉시 반영
                                   ├─→ [EditorPlaySync] ─→ Play 모드 자동 로드
                                   ├─→ [MapSceneGenerator] ─→ Unity Scene 자동 생성
                                   ├─→ [MapExporter] ─→ JSON ─→ [MapLoader] ─→ 런타임
                                   └─→ [BuildPreprocessor] ─→ 검증 → 빌드 패키지
```

### 4.1 실시간 프리뷰 (LivePreviewManager)

에디터에서 타일/건물 수정하면 Scene View에 즉시 반영. SO의 `OnValidate`와 `Undo.undoRedoPerformed` 콜백 활용.

### 4.2 Play 모드 연동 (EditorPlaySync)

```csharp
public class EditorPlaySync
{
    // Play 모드 진입 시:
    // 1. 현재 편집 중인 MapData를 임시 JSON으로 익스포트
    // 2. MapRuntimeBootstrapper가 자동 로드
    // 3. 게임 시스템 (카메라, 캐릭터, 상태전환) 자동 초기화
    // Play 모드 종료 시:
    // 4. 임시 데이터 정리
}
```

### 4.3 런타임 부트스트래퍼 (MapRuntimeBootstrapper)

맵 로드 → 모든 시스템 초기화를 원스톱으로 처리.

```csharp
public class MapRuntimeBootstrapper : MonoBehaviour
{
    [SerializeField] string mapId;
    [SerializeField] MapLoadMode loadMode;    // SO / JSON / Addressable

    void Start()
    {
        // 1. 맵 데이터 로드                   (MapLoader)
        // 2. 타일/건물 렌더링 초기화           (TileRenderer, BuildingRenderer)
        // 3. 소팅 적용                         (IsometricSortingManager)
        // 4. Walkability 맵 로드              (WalkabilityMap)
        // 5. Props/Harvestable 스폰           (PropManager, HarvestableManager)
        // 6. 건물 상태 전환 시작               (BuildingStateManager)
        // 7. 인테리어 연결 해석               (InteriorConnectionResolver)
        // 8. 카메라 바운드 설정               (IsometricCameraController)
        // 9. 청크 로더 초기화                 (MapChunkLoader)
    }
}

public enum MapLoadMode { ScriptableObject, JSON, Addressable }
```

### 4.4 Scene 자동 생성 (MapSceneGenerator)

맵 데이터 → Unity Scene을 버튼 하나로 생성. 증분 업데이트도 지원.

### 4.5 Scene 링크 (MapSceneLinker)

MapData SO ↔ Unity Scene 1:1 매핑 관리. 맵 데이터 변경 시 Scene 자동 dirty.

### 4.6 빌드 파이프라인 (BuildPreprocessor)

```csharp
public class BuildPreprocessor : IPreprocessBuildWithReport
{
    public void OnPreprocessBuild(BuildReport report)
    {
        // 1. 모든 MapData SO 수집
        // 2. MapValidator로 전체 검증
        //    - 누락된 연결, 탈출구 없는 인테리어, 겹침
        // 3. 검증 실패 → 빌드 중단 + 에러 리포트
        // 4. 통과 → JSON 익스포트 → StreamingAssets 또는 Addressable
    }
}
```

### 4.7 런타임 디버그 오버레이 (MapDebugOverlay)

Play 모드에서 F1으로 토글. 표시 항목:
- 그리드 라인
- Walkability 컬러 오버레이
- 건물 풋프린트
- 연결 포인트 아이콘
- 파밍 요소 스폰 확률
- 현재 건물 상태 정보
- 청크 경계선

---

## 5. 에디터 UI 구성

### 메인 에디터 창

```
+------------------------------------------------------------------+
| [File] [Edit] [View] [Tools]              Map: "CityBlock_01"    |
+----------+---------------------------+--------------------------+
| TOOLS    |       SCENE VIEW          |  PROPERTIES (컨텍스트)    |
| [Paint]  |    (Unity Scene View)     |  건물 이름: Tavern       |
| [Erase]  |                           |  풋프린트: 2x3           |
| [Select] |                           |  상태: 4개               |
| [Road]   |                           |  인테리어: 연결됨         |
| [Props]  |                           |  Walkability: O          |
| [Walk]   |                           |                          |
| [Connect]|                           |                          |
+----------+                           +--------------------------+
| LAYERS   |                           | 상태 미리보기            |
| [v] Ground                           | [Day] [Night]            |
| [v] Objects                          | [Winter] [Damaged]       |
| [v] Props                            |                          |
| [ ] Roof                             |                          |
| [ ] Walkability                      |                          |
+----------+---------------------------+--------------------------+
| Grid 64x64 | Tile: 128x64 | Cursor: (12, 5) | Sync: Live       |
+------------------------------------------------------------------+
```

### 상태 변환 에디터
- 모든 상태를 나란히 미리보기
- 전환 룰 시각적 편집 (from → to, 조건, 자동 여부)

### 인테리어 에디터
- 별도 탭으로 열림 (Preview Scene 사용)
- 진입/출구/탈출구 기즈모 표시
- Props, Harvestable, Walkability 동일 적용
- "전환 테스트" 버튼

### 연결 그래프 에디터
- 건물을 노드, 연결을 엣지로 시각화
- 드래그로 건물간 터널/스카이워크 연결

---

## 6. 런타임 시스템

### 렌더링/소팅
- Sorting Order = `(cell.x + cell.y) * 10 + layerOffset + customOffset`
- 멀티타일 건물: 풋프린트 최전면 셀 기준
- 건물 = base SpriteRenderer + roof SpriteRenderer 분리

### 건물 진입 플로우
```
1. 근접    → RoofHideController: 지붕 페이드 아웃
2. 트리거  → 화면 전환 (페이드)
3. 로드    → InteriorMapLoader: 인테리어 맵 로드
4. 배치    → 플레이어를 ConnectionPoint 위치로 텔레포트
5. 카메라  → 바운드 변경
6. 완료    → 화면 전환 인
```

### 상태 전환 런타임
- `BuildingStateManager`: 매 프레임 자동 전환 룰 체크
- 조건 충족 시 CrossFade/Dissolve 등으로 스프라이트 전환
- 추가 오브젝트 (라이트 등) 활성/비활성

### 성능 (대형 맵)
- 16x16 청크 단위 로딩/언로딩
- 오브젝트 풀링
- SpriteAtlas로 드로우콜 최소화
- 256x256+ 맵 스케일 대응

---

## 7. 구현 페이즈

### Phase 1: 코어 그리드 + 타일 페인팅 + 연동 기반 (MVP) ── 2~3주

| 항목 | 내용 |
|------|------|
| 핵심 | IsometricGrid, GridSettings, MapData, TileDefinition |
| 에디터 | MapEditorWindow (기본), 그리드 기즈모, PaintTool/EraseTool |
| 렌더링 | TileRenderer, IsometricSortingManager, IsometricCameraController |
| 연동 | MapRuntimeBootstrapper, LivePreviewManager, EditorPlaySync |
| 결과물 | **에디터에서 맵 만들고 Play 누르면 아이소 맵 즉시 보임** |

### Phase 2: 건물 + 멀티타일 ── 1~2주

| 항목 | 내용 |
|------|------|
| 데이터 | BuildingDefinition, PlacedBuilding |
| 에디터 | 풋프린트 에디터, 건물 팔레트 |
| 렌더링 | BuildingRenderer, RoofController (기본 show/hide) |
| 결과물 | **건물 배치, 풋프린트 겹침 차단, 지붕 분리 렌더링** |

### Phase 3: 건물 상태 전환 ── 1~2주

| 항목 | 내용 |
|------|------|
| 데이터 | BuildingVariantSet, BuildingVariant, VariantTransitionRule |
| 런타임 | BuildingStateManager, StateConditionEvaluator |
| 에디터 | BuildingVariantEditorWindow |
| 결과물 | **낮/밤/시즌/파괴 상태 전환 동작** |

### Phase 4: 도로 오토타일링 ── 1주

| 항목 | 내용 |
|------|------|
| 데이터 | RoadDefinition (4비트 이웃 비트마스크 스프라이트) |
| 에디터 | RoadTool |
| 결과물 | **도로 페인트 → 자동 코너/교차로 스프라이트 전환** |

### Phase 5: Walkability + 이동 불가 영역 ── 1주

| 항목 | 내용 |
|------|------|
| 데이터 | WalkabilityData |
| 에디터 | WalkabilityPaintTool, 컬러 오버레이 (초록/빨강/노랑) |
| 런타임 | WalkabilityMap, PathfindingHelper |
| 결과물 | **맵 위에 이동 가능 영역 페인트, 런타임 경로 차단** |

### Phase 6: 배치 오브젝트 + 파밍 요소 ── 2주

| 항목 | 내용 |
|------|------|
| Props | PropDefinition, PlacedProp, PropPlaceTool, PropManager |
| 파밍 | HarvestableDefinition, PlacedHarvestable, SpawnCondition |
| 런타임 | HarvestableManager, SpawnConditionEvaluator |
| 에디터 | 오브젝트 팔레트, 파밍 조건 설정 UI |
| 결과물 | **가로등/나무 배치, 파밍 요소 확률/조건 설정, 조건부 노출** |

### Phase 7: 인테리어 맵 + 진입/탈출 시스템 ── 2~3주

| 항목 | 내용 |
|------|------|
| 데이터 | InteriorMapData, ConnectionPoint, EscapePoint |
| 에디터 | InteriorEditorWindow, ConnectionTool |
| 런타임 | InteriorTransitionManager, RoofHideController, EscapePointManager |
| 카메라 | CameraTransitionHandler |
| 결과물 | **건물 진입→지붕 사라짐→내부 맵, 탈출구, 이동 불가 적용** |

### Phase 8: 건물간 내부 연결 ── 1~2주

| 항목 | 내용 |
|------|------|
| 데이터 | InteriorConnection |
| 런타임 | InteriorConnectionResolver |
| 에디터 | ConnectionEditorWindow (그래프 뷰) |
| 결과물 | **터널/스카이워크로 건물 내부끼리 이동** |

### Phase 9: 익스포트 + 빌드 파이프라인 + 최적화 ── 2~3주

| 항목 | 내용 |
|------|------|
| 익스포트 | MapExporter (SO→JSON) |
| 검증 | MapValidator (누락 연결, 탈출구 없는 인테리어 경고) |
| 연동 | MapSceneGenerator, MapSceneLinker, BuildPreprocessor, MapBuildPipeline |
| 성능 | MapChunkLoader, 오브젝트 풀링 |
| 디버그 | MapDebugOverlay (F1 토글) |
| 결과물 | **에디터→빌드 원스톱, 빌드 전 자동 검증, 대형 맵 최적화** |

### Phase 10: 샘플 + 문서 ── 1주

- 샘플 맵/건물/타일/파밍 요소
- 가이드 문서

### 총 예상: 14~22주 (1인 개발 기준)

---

## 8. 기술 결정 요약

| 결정 | 선택 | 이유 |
|------|------|------|
| 에디터 UI | UI Toolkit | 모던 레이아웃, 스타일링 가능 |
| 데이터 (에디터) | ScriptableObject | Undo, Inspector, 에셋 참조 네이티브 지원 |
| 데이터 (익스포트) | JSON | 디버깅 편리, 버전 마이그레이션 가능 |
| 좌표 계산 | Static 유틸리티 클래스 | 에디터/런타임 공유, MonoBehaviour 오버헤드 없음 |
| 소팅 | 공식 기반 `(x+y)*10+layer` | 결정적, 동적 소팅 불필요 |
| 인테리어 편집 | 별도 EditorWindow + Preview Scene | 외부/내부 격리, 깔끔한 UX |
| 지붕 숨김 | 건물별 별도 SpriteRenderer | 독립적 알파 제어, 셰이더 복잡도 없음 |
| 대형 맵 | 청크 로딩 + 풀링 | 256x256+ 스케일 가능 |
| 어셈블리 분리 | 4개 asmdef | Editor/Runtime 경계 강제 |
| 맵 로드 모드 | SO/JSON/Addressable 선택 | 개발 중 SO 직접, 빌드 시 JSON/Addressable |

---

## 9. 검증 체크리스트

| # | 페이즈 | 검증 항목 |
|---|--------|-----------|
| 1 | Phase 1 | 에디터에서 맵 생성 → 타일 페인트 → Play 모드 아이소 렌더링 확인 |
| 2 | Phase 1 | 에디터 수정 → Scene View 즉시 반영 (LivePreview) |
| 3 | Phase 1 | Play 버튼 → MapRuntimeBootstrapper 자동 로드 확인 |
| 4 | Phase 2 | 건물 배치 → 풋프린트 겹침 차단 → 소팅 정확성 |
| 5 | Phase 3 | 상태 전환 룰 설정 → Play에서 시간 변경 시 자동 전환 |
| 6 | Phase 5 | Walkability 페인트 → 캐릭터 Blocked 셀 진입 불가 |
| 7 | Phase 6 | 파밍 요소 배치 → 확률/조건 설정 → 조건부 노출/비노출 |
| 8 | Phase 7 | 건물 진입 → 지붕 페이드 → 인테리어 로드 → 탈출구 나가기 |
| 9 | Phase 7 | 인테리어 내 Walkability/파밍 요소 동작 확인 |
| 10 | Phase 9 | 빌드 시 BuildPreprocessor 검증 → JSON 패키징 → 맵 로드 |
| 11 | 전체 | 단위 테스트: 좌표 round-trip, 소팅, 상태 전환, SpawnCondition |
