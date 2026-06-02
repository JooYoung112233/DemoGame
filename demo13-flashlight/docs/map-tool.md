# 맵 도구 (탑다운 2D)

> **현 상태 = 진실.** 2026-06-02 아이소메트릭 맵빌더(WallBuilder/PropQuadBuilder/MapBuilderManager 등) **전면 폐기**.
> 탑다운 2D는 Unity 네이티브 **Tilemap** + **Prop2D 카탈로그** + 씬 빌더 에디터로 구성.
> 렌더 전제는 [`rendering.md`](rendering.md), 전환 배경은 [`topdown-migration.md`](topdown-migration.md).

## 구성

| 도구 | 형태 | 역할 |
|---|---|---|
| `GameSceneBuilder` | 에디터 메뉴 (`Tools ▸ TopDown ▸ Build ▸ InGame/Safehouse Scene`) | InGame/Safehouse 씬 골격 자동 생성 — 2D 카메라(CustomAxis 정렬) + CameraFollow, EventSystem, 글로벌 Light2D, Grid + Floor/Walls Tilemap(+TilemapCollider2D/CompositeCollider2D/Static Rigidbody2D), SpawnPoint |
| `MapTool2DSceneBuilder` | 에디터 (`Tools ▸ TopDown ▸ Build ▸ Map Tool Scene`) | 탑다운 맵 제작용 씬 셋업 보조 |
| `Prop2DCatalogEditor` | EditorWindow (`Tools ▸ TopDown ▸ Map ▸ Prop Catalog`) | Prop2D 정의 목록·생성·삭제 + 콜라이더 시각 미리보기. 에셋은 `Assets/Resources/Props2D/` |

> 전체 에디터 메뉴 인덱스는 [`tooling.md`](tooling.md) 참고 (2026-06-02 `Tools/TopDown/` 단일 루트로 통합).

## 맵 제작 흐름

1. **씬 골격**: `GameSceneBuilder`로 InGame/Safehouse 씬 생성.
2. **바닥/벽**: Unity **Tilemap**으로 페인트. 바닥 = Floor 타일맵, 벽 = Walls 타일맵(TilemapCollider2D + CompositeCollider2D로 이동 차단).
3. **프롭**: `Prop2DCatalogEditor`로 정의 등록 후 씬에 배치. 각 프롭은 `SpriteRenderer + Collider2D`.
   - 콜라이더 모드(프롭마다 선택): **None**(장식) / **Box**(네모) / **Polygon**(나무·바위, 스프라이트 physics shape 외곽선 자동) / **Composite**(각진 구조물, 박스 2~3개 합성).
4. **막힘 규칙**: walkability 그리드/NavMesh 없음 — **비-트리거 Collider2D**가 곧 이동 차단.

## 좌표 / 정렬

- 월드 = **XY 평면**, 깊이 = **sortingOrder**. 카메라 정렬축 `(0,1,0)`(Y 낮을수록 앞).
- 싱글톤/플레이어는 씬에 배치하지 않고 코드로 자동 스폰(`TopDownPlayer.Bootstrap` 등). 씬에는 SpawnPoint만.

## 핵심 결정 로그

| 날짜 | 결정 |
|---|---|
| 2026-06-02 | 아이소 맵빌더(커스텀 EditorWindow + 3D 큐브 벽 + 빌보드 프롭 + JSON 베이크) 전면 폐기. |
| 2026-06-02 | 탑다운 맵 = Tilemap(바닥/벽) + Prop2D 카탈로그(프롭). 막힘 = 비-트리거 Collider2D 규칙. |
| 2026-06-02 | Prop2D 콜라이더 전략 = "프롭마다 선택"(None/Box/Polygon/Composite). |
| 2026-06-03 | **Prop 카탈로그 UX 정비.** 탭(바닥/벽/프롭/오브젝트, ID 접두어 `floor_/wall_/prop_/object_` 자동) + 세로 레이아웃(목록 위·스크롤/접기, 폼 아래·섹션 접기). **새 항목 = 드래프트 방식**(이미지·머티리얼 등록 후 '생성 & 등록', 즉시 빈 에셋 X). 항목 행에 썸네일/정보/편집·복제·X. |
| 2026-06-03 | **카탈로그 생성 시 프리팹 자동 생성.** `Prop2DDefinition.prefab`(SpriteRenderer+Collider2D)을 `Resources/Props2D/Prefabs/{id}.prefab`로 생성/갱신(생성·저장 시). 씬 배치는 그 프리팹을 `InstantiatePrefab`(링크 유지), 없으면 즉석 빌드. 배치는 **Play가 아니라 Scene 뷰(에디트 모드)** — `SceneView.duringSceneGui` 클릭. |
| 2026-06-03 | **벽 Tiled draw mode 지원.** `Prop2DDefinition`에 `drawMode`(Simple/Tiled/Sliced)·`tiledSize`·`tileMode` 추가. `Prop2DBuilder`가 `SpriteRenderer.drawMode/size/tileMode` 적용, Box 콜라이더도 Tiled면 `tiledSize`에 맞춤(벽 길이 전체). 카탈로그에 "스프라이트 Full Rect로 설정" 버튼(Tiled 정상 렌더용 임포트 수정). |
| 2026-06-03 | **맵툴 씬에선 게임 HUD/플레이어 자동 스폰 스킵.** 씬 이름에 "MapTool" 포함 시(`MapToolScene.IsActive`) `UIManager`(체력/재화 HUD)·`TopDownPlayer` 자동 생성 안 함 — 맵 편집/미리보기 전용. |
| 2026-06-03 | **카탈로그 UX 전면 정비 + 배치 페인트.** 목록 → **썸네일 팔레트 그리드**(클릭=브러시 선택, 우클릭=메뉴 복제/삭제). 단일 **"배치 모드" 토글**(브러시=선택 프롭) — 켜면 씬뷰 좌클릭/드래그=배치(고스트 미리보기), **ESC=취소**(창·씬뷰 양쪽 처리 + 진입 시 씬뷰 포커스). 삭제는 씬에서 직접(우클릭 지우개 제거). 최상단 바: **맵 생성/맵 저장**·스냅. 새 항목=드래프트(이미지·머티리얼 등록 후 생성&등록). 폼 섹션 접기. |
| 2026-06-03 | **맵 저장(배치→프리팹) + 맵 부모.** `맵 생성`이 씬에 `Map` 루트 생성, 배치 프롭이 그 하위로. `맵 저장`이 `Map`(또는 선택)을 `Assets/Maps/`에 프리팹으로(`SaveAsPrefabAssetAndConnect`, 중첩 프리팹 보존). |
| 2026-06-03 | **프롭 기능(Function) 부여 — 옛 MapObject 기능 복구.** `Prop2DDefinition.function`(모든 프롭에): None / **SpawnPoint** / **Interactable**(InteractType: ExitPoint/MapBoard/Bed/Workbench 등) / **LootContainer**(수색) / **ItemDrop**(바닥 아이템, ItemSpawnPoint Fixed/Ground) / **NPC**(NPCController+npcData) / **Door**(DoorController, 프롭 콜라이더=문) / **Trigger**(영역→씬전환, 새 `MapTriggerZone2D`). **투명 마커(noVisual)**=SpriteRenderer 없이 기능만 + `Prop2DMarker` 기즈모(SpawnPoint는 자체 기즈모). `Prop2DBuilder.ApplyFunction`이 배치/프리팹 빌드 시 컴포넌트 자동 부착(private 필드 reflection). 오브젝트 탭 새 항목은 기본 Interactable. | 옛 `MapObjectSpawner`(클린 슬레이트로 삭제) 기능을 Prop2D로 통합 — "오브젝트=기능"을 모든 프롭에 부여 가능하게. |
