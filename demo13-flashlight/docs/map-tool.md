# 아이소메트릭 맵툴 기획 결정 기록

## 현재 상태

### 에디터 형태
- **Unity Editor Extension** (EditorWindow + EditorTool + SceneView 오버레이)
- 별도 앱이 아닌 Unity 에디터 내장 방식

### 좌표 시스템 (3D 공간 + 2D 스프라이트, 좀보이드 스타일)
- **3D 공간**: 타일은 XZ 평면에 배치, Y축 = 높이
- 카메라: 3D 오쏘그래픽, `Euler(35.264, 45, 0)` — 정석 아이소메트릭 (arctan(1/√2), 3축 120° 등각)
- **카메라 향 오브젝트** (NPC, 적, 인터랙터블 등) Root: `Quaternion.Euler(35.264, 45, 0)` (카메라와 동일)
- **바닥에 깔리는 것** (타일, 하베스터블 스프라이트, TextureQuad 맵오브젝트): `Quaternion.Euler(90, 0, 0)` (XZ 평면에 눕힘)
- **프랍/빌딩**: 프리팹 자체에 Root 회전이 세팅됨. 바닥 쿼드 프리팹은 `(90,0,0)`, 서있는 오브젝트 프리팹은 `(35.264,45,0)`
- **벽**: 수직으로 세워서 동서남북 배치 (변경 안 함)
- Y회전(Q/E 15도 단위): 프리팹 Root 회전에 **곱셈** (덮어쓰기 X) — `Euler(0, yRot, 0) * existingRotation`
- `float tileSize = 1f` (단일 크기, tileWidth/tileHeight 제거)
- GridToWorld: `Vector3(cell.x * tileSize, 0, cell.y * tileSize) + originOffset`
- WorldToGrid: XZ 좌표에서 역산
- 소팅: `(cell.x + cell.y) * 10 + layerOffset + floor * 1000`
- Scene View 마우스: XZ 평면(Y=0) ray-plane intersection
- 층(floor) 시스템: API에 예약됨, 추후 구현 예정

### 건물 상태 전환
- **범용 상태 시스템** (BuildingVariantSet)
- 낮/밤, 시즌, 파괴 단계, 커스텀 등 모든 상태 전환 통합
- Props(가로등 등)에도 동일 시스템 재사용

### 인테리어 시스템
- 건물별 **별도 ScriptableObject**로 내부 맵 관리
- 외부 맵과 독립된 좌표계
- 진입/출구 ConnectionPoint로 연결
- 인테리어에도 Props/Harvestable/Walkability 동일 적용
- 건물간 내부 연결 (터널, 스카이워크) 지원

### 배치 오브젝트 (Props)
- 가로등, 나무, 표지판 등 배치 가능
- footprint(차지 셀 수), blocksWalkability(자동 이동불가) 설정

### 파밍 요소 (Harvestable)
- 확률/조건부 노출 시스템 (SpawnCondition)
- 조건 타입: Always, Probability, TimeOfDay, Season, Weather, QuestComplete, PlayerLevel, Custom
- 드롭 테이블 (아이템ID, 수량 범위, 확률)
- 리스폰 설정 (시간 기반 / 맵 리로드 / 1회성)

### 이동 가능 영역 (Walkability)
- 셀 단위 페인팅 방식
- 타입: Walkable, Blocked, SlowZone, Hazard, TriggerZone

### 탈출구 시스템
- 인테리어 내 탈출 포인트 설정
- 타입: Door, Window, SecretPassage, Emergency
- 열쇠 필요, 숨겨진 통로, 조건부 사용 가능

### 에디터-인게임 연동
- LivePreviewManager: 에디터 수정 → Scene View 즉시 반영
- EditorPlaySync: Play 모드 진입 시 현재 맵 자동 로드
- MapRuntimeBootstrapper: 원스톱 런타임 초기화
- MapSceneGenerator: SO → Unity Scene 자동 생성 (Phase 9)
- BuildPreprocessor: 빌드 전 자동 검증 (Phase 9)

### 데이터 형식
- 에디터: ScriptableObject (Undo, Inspector 네이티브 지원)
- 익스포트: JSON (디버깅 편리, 버전 마이그레이션)

### 구현 순서
1. 코어 그리드 + 타일 페인팅 + 연동 기반 (MVP) ← **완료**
2. 건물 + 멀티타일 ← **완료**
3. 건물 상태 전환 ← **완료**
4. 도로 오토타일링 ← **완료**
5. Walkability + 이동 불가 영역 ← **완료**
6. 배치 오브젝트 + 파밍 요소 ← **완료**
7. 인테리어 맵 + 진입/탈출 ← **완료**
8. 건물간 내부 연결 ← **완료**
9. 익스포트 + 빌드 파이프라인 ← **완료**
10. 샘플 + 문서 ← **완료**

---

## 변경 로그

| 날짜 | 변경 내용 |
|------|-----------|
| 2025-05-20 | 초기 기획 결정 기록. Phase 1 구현 시작. |
| 2026-05-20 | 전체 Phase 1~9 구현 완료. 좌표 시스템을 2D→3D로 전면 리팩터 (좀보이드 스타일: 3D 공간 + 2D 빌보드 스프라이트). tileWidth/tileHeight → float tileSize. Vector2→Vector3. Rect→Bounds. Scene View mouse: ray-plane intersection on XZ. 카메라: 3D orthographic Euler(30,45,0). 층(floor) API 예약. |
| 2026-05-29 | 건물 조립 방식 결정: 건물은 단일 프리팹이 아니라 **바닥/윗벽/아랫벽/천장** 등의 파트를 Quad로 개별 배치하여 조립. 각 파트는 Quad + InkCity/CityBuilding 셰이더 자동 적용. 맵툴 런타임 UI에 Free 배치 모드(G키), R키 리사이즈 모드(스크롤 크기조절) 추가. 하이어라키 패널 제거, 삭제 모드 호버 프리뷰(빨간 하이라이트+툴팁) 추가. |
| 2026-05-29 | **아이소메트릭 카메라/Root 각도 통일**: 정석 아이소메트릭 `Euler(35.264, 45, 0)` 적용. 변경 파일: MapBuilderCamera(에디터), IsometricCameraController(인게임), SpawnCreator(Enemy/Interactable/NPC Root 3곳), MapBuilderManager SpawnObjectMarker TextureQuad, HarvestableManager SetupBillboard, MapObjectSpawner SpawnSingle. PropManager/MapBuilderManager SpawnPropVisual/SpawnBuildingVisual: Y회전을 replace→additive(곱셈)로 수정. MapSerializer SerializableBuilding에 freePlace/worldPosition/yRotation/scale 필드 추가. 바닥타일(90,0,0)과 벽은 제외. |
| 2026-05-29 | MapObject 비주얼 모드 4종 결정: **TextureQuad**(이미지), **Sphere**(색상 구체, 기본), **Invisible**(콜라이더만), **EffectPrefab**(파티클/이펙트 프리팹). 회전 단위 15도(Q/E)로 세분화. F1 도움말 오버레이 추가. 카탈로그 에디터 전 탭 인라인 편집 기능 추가. BuildingDefinition에서 icon 필드 제거(프리팹 텍스처로 대체). |
| 2026-05-29 | **2D 스프라이트 구조물 + 그림자 프록시 박스 방식 확정** (안전가옥 펜스/벽/컨테이너). 맵툴에서 구조물 배치 시: 비주얼은 TextureQuad(2D 아이소 스프라이트), 빛 차폐는 얇은 3D 박스(ShadowsOnly)를 함께 배치. **철창 펜스는 그림자 캐스터 없음**(see-through, 빛 통과). 솔리드 벽/컨테이너만 그림자 박스. 상세는 `rendering.md` 참조. |
| 2026-05-29 | **프롭 제작 단계에 "벽(빛 차폐)" 기능 구현.** 카탈로그 에디터 Props 탭에서 벽 체크 → 그림자 차폐 박스 부착. 형태 프리셋(직선/대각선/ㅅ자/V자) + 박스별 size/offset/yaw 인라인 편집. 대각선·ㅅ·V자 벽은 박스 yaw(각도)로 처리. 에디터 프리뷰는 반투명 주황 슬랩, 런타임은 ShadowsOnly. 신규: `ShadowProxyBuilder.cs`, `PropDefinition.ShadowBox/castsShadow/shadowBoxes`. 수정: `MapBuilderManager.SpawnPropVisual`, `PropManager.SpawnProp`, `MapCatalogEditor`. |
| 2026-05-30 | **벽/건물 빛 차폐 확장.** TileDefinition(벽)에 `castsShadow`+`shadowBoxes` 추가, WallBuilder에서 ShadowProxy 자동 배치. BuildingDefinition에 `occludesInterior`(투명 전환: 윗벽/천장 파트용) + `castsShadow`+`shadowBoxes` 추가. ShadowProxyBuilder에 범용 오버로드 `Build(ShadowBox[], Transform, bool)` 추가. 카탈로그 에디터 Walls/Buildings 탭에 빛차폐·투명 UI 반영. |
| 2026-05-30 | **카탈로그 에디터 씬 미리보기 시스템 추가.** 생성 폼에서 "씬 미리보기" 버튼 → 씬에 프리뷰 GO 생성. 폼 수정 시 프리뷰 자동 갱신(양방향 동기화). 씬에서 직접 Transform/스케일 수정하면 폼에 반영. "생성 & 카탈로그에 등록" 클릭 시 프리뷰를 프리팹으로 저장 후 SO에 연결. 전 카테고리(Tile/Wall/Prop/Building/Object) 지원. |
| 2026-05-30 | **건물 파트 표시/숨김 토글 (내부 프랍 배치용).** 질문: 건물 안에 프랍을 설치하려면 건물 벽/천장을 투명하게 하는 기능이 필요 — 리스트에서 벽 1개씩 껐다 켰다? 결정: ①리스트에는 **배치된 모든 건물**을 나열(개별 인스턴스 단위). ②토글 = **완전히 숨김**(SetActive false, 에디터 전용 — 저장 데이터에 영향 없음). ③**"내부 보기" 버튼** 추가: `occludesInterior`인 모든 파트(윗벽/천장)를 한번에 껐다 켰다. + "전체 보기" 버튼으로 모든 숨김 해제. 신규 UI: MapBuilderUI 우측 "건물 표시" 패널(BuildBuildingListPanel/PopulateBuildingList/AddBuildingListItem). 매니저 API: `_hiddenBuildingIds`, `InteriorViewActive`, `PlacedBuildings`, `IsBuildingHidden`, `SetBuildingHidden`, `ApplyBuildingHiddenState`(RebuildAllVisuals 말미 호출), `ToggleInteriorView`, `ShowAllBuildings`. |
| 2026-05-30 | **프롭을 스프라이트 기반 빌보드 쿼드로 전환.** 질문: 프롭 비주얼 생성 방식 / 일괄 배치 방식 / nav 콜라이더 형태. 결정: ①프롭은 `PropDefinition.sprite`만 지정하면 런타임에 카메라 향(`Quaternion.LookRotation(-IsoViewDir)`) 빌보드 쿼드로 자동 생성 — **프리팹 불필요**(prefab은 sprite 없을 때만 쓰는 레거시 옵션). ②**빛 차폐 안 함** — 쿼드 프롭은 ShadowProxy를 생성하지 않음(프리팹 프롭에만 유지). ③이동 차단은 `blocksWalkability`일 때 풋프린트 크기의 **축 정렬 BoxCollider**(NavBlocker 자식)만 부착. ④카탈로그 에디터 Props 탭에 **스프라이트 일괄 등록** 추가: 드래그&드롭 또는 프로젝트 선택 → 스프라이트명=propId로 PropDefinition 에셋 일괄 생성(공통 풋프린트/이동차단 설정). 신규: `PropQuadBuilder.cs`. 수정: `PropDefinition`(sprite/UsesQuad 추가), `MapBuilderManager`(SpawnPropVisual/RebuildGhost/UpdateGhostPosition/UpdateMovePosition/export), `PropManager.SpawnProp`, `MapBuilderUI`(팔레트 아이콘 sprite 폴백), `MapCatalogEditor`(폼 sprite 필드 + 일괄 등록). |
| 2026-05-30 | **프롭 프리팹 기능 완전 제거 + 쿼드 머티리얼 지원.** 결정: ①프롭은 이제 **스프라이트/쿼드 전용** — `PropDefinition.prefab` 및 레거시 프리팹 분기 전부 삭제(PropManager/MapBuilderManager의 SpawnPropVisual·RebuildGhost·UpdateGhost/Move·export, MapCatalogEditor 폼/배치/프리뷰). 빛 차폐(castsShadow/shadowBoxes)도 프롭에서 미사용(필드는 잔존하나 항상 비움). ②쿼드에 **머티리얼(선택) 지정** 가능: `PropDefinition.material` → `PropQuadBuilder`가 SpriteRenderer.sharedMaterial로 적용. 카탈로그 폼·일괄 생성(`batchMaterial`) 양쪽 지원. ③카탈로그 에디터 등록 Props 리스트는 **최대 6행만 표시 후 내부 스크롤**(`propListScroll`, 지연 삭제 패턴). ④**프롭 아이콘(icon) 필드 제거** — 팔레트/리스트 썸네일은 `sprite`를 직접 사용(`PropDefinition.icon` 및 MapCatalogEditor/MapBuilderUI의 icon 분기 전부 삭제). UX 수정: Load/Save 다이얼로그 파일 리스트 항목 글씨 가로 잘림 해결(`_fileListContent` offsetMin/Max=0). |
| 2026-05-30 | **프롭 빌보드 수직화 + 건물 토글 라벨 버그 수정.** ①프롭이 뒤로 눕던 문제 수정: 풀 빌보드(`LookRotation(-IsoViewDir, up)`)는 카메라 피치(35°)만큼 스프라이트가 뒤로 기울어 바닥에 눕는 것처럼 보였다 → **Y축(수평) 빌보드**로 변경(`PropQuadBuilder.BillboardRotation`이 시선 방향의 Y를 0으로). 이제 프롭은 바닥에 수직으로 서서 카메라를 수평으로만 바라봄(회전 Q/E와 결합해도 항상 수직 유지). ②건물 표시 리스트 토글 버튼 라벨이 **반대로** 표기되던 버그 수정: 보이는 건물이 "표시"로 나와 프랍 배치 시 상태가 바뀐 것처럼 보였다 → 버튼은 "클릭 시 동작"을 표시(보임→"숨김", 숨김→"표시"). 프랍 배치는 건물 숨김 상태(`_hiddenBuildingIds`)를 건드리지 않으므로 리스트 상태는 안정적. |
| 2026-05-30 | **프롭 좌우 반전(미러/플립) 기능 추가.** 질문: 프롭에 회전 말고 좌우 반전도 필요. 결정: **F 키**로 프롭 스프라이트 좌우 반전 토글(회전 Q/E와 별개). 구현: 빌보드 SpriteRenderer의 `flipX` 사용 → `PropQuadBuilder.ApplyFlip(root, flipX)` 헬퍼. 신규 데이터 `PlacedProp.flipX`(저장/로드·프리셋 복사 포함). 매니저 상태 `CurrentFlipX` + `ToggleFlipX()`(배치 고스트·이동 중 프롭에 즉시 반영, 도구 전환/선택 해제 시 false 리셋). 배치/이동/베이크(export)/런타임 스폰 전 경로에 적용. UI: 상태바에 `⇄Flip` 표시, Props 팔레트 헤더·F1 도움말에 단축키 안내. 수정: `PlacedProp`, `PropQuadBuilder`, `PropManager`, `MapBuilderManager`, `MapBuilderInput`, `MapBuilderUI`, `MapSerializer`, `MapPreset`, `BuildingPreset`. |
