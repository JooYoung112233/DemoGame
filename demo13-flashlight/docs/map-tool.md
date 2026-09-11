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
| 2026-06-03 | **카탈로그 탭별 상태 캐싱.** 탭(바닥/벽/프롭/오브젝트/데칼)마다 **일괄 등록 설정(머티리얼·Sorting Layer·Order·Draw Mode·그림자·Casting·분류) + 선택 항목**을 따로 기억(`TabState[]`, `[SerializeField]`라 도메인 리로드/창 유지 후에도 보존). 탭 전환 시 떠나는 탭 저장→새 탭 복원. 활성 탭(`_tab`)도 리로드 후 유지. 분류 필터·검색은 캐싱 안 함(가림 방지 위해 탭 전환·일괄등록 후 리셋 유지). | 탭마다 자주 쓰는 머티리얼/정렬/분류가 달라 탭 전환 때마다 다시 세팅하던 불편 제거. |
| 2026-06-03 | **카탈로그 폼 가독성 정비.** 섹션(Identity/Visual/Collider/Shadow/Function/Preview·일괄등록)마다 **색상 헤더 바**(고유 강조색 + 흰 굵은 큰 글씨 + ▼/▶ 토글 + 왼쪽 강조 바) + **본문 들여쓰기**. 상단 "편집/생성" 배너도 색 바 + 큰 글씨(생성=초록/편집=파랑). 주요 버튼 색 강조(생성=초록/저장=파랑/삭제=빨강). **최상단 바**(맵 생성/맵 저장)는 얇은 toolbar 버튼이라 안 보여서 → 큰 색 버튼(맵 생성=초록/맵 저장=파랑, 28px 굵게) + 하단 구분선으로 교체. 기능 동일, 시각 계층만 정리. |
| 2026-06-03 | **분류(컬렉션) 개념 추가 — 카테고리 하위 테마 묶음.** `Prop2DDefinition.group`(자유 텍스트, 예: '전당포', '안전구역'). **카테고리(탭)를 가로지르는** 정리용 태그(런타임 영향 없음) — 한 테마에 바닥/벽/프롭/데칼 모두 포함 가능. 카탈로그: 폼·일괄 등록에 분류 입력 + 기존 분류 ▾ 선택, 팔레트에 **분류 필터**(＜전체＞/(미분류)/각 분류), ＜전체＞일 땐 **분류별 헤더로 묶어** 표시. 새 항목 생성 시 현재 필터 분류 자동 적용. | 옛 카탈로그엔 카테고리(탭)만 있어 같은 테마(전당포 등) 에셋이 흩어짐 → 테마 큐레이션·필터 위해 2단계 분류 도입. |
| 2026-06-04 | **건물 개념 변경 — 천장 시스템 폐기 → 건물=Trigger 전환.** "천장 없음 + 건물은 무조건 전환" 확정. **천장(Ceiling) 탭·카테고리·`CeilingFader`·천장 반투명 토글·천장 트리거/정렬레이어 시딩·`GreyboxPaletteBuilder` 천장 항목** 전부 삭제. 건물 = 일반 프롭 + **`Function.Trigger`**(영역→씬 전환, 입구 밟으면 자동). Trigger에 **`triggerOffset`** 추가 — 80° 틸트 밑둥/입구로 전환 영역을 내릴 수 있게. 기존 천장 에셋(ceiling_0001/0002, gb_roof)은 Prop 카테고리로 전환(삭제는 사용자 재량). `AttachColliderAutoFitUnder`의 `isTrigger` 제외는 유지(건물 전환 영역 보호). 상세 [`rendering.md`](rendering.md). |
| 2026-06-04 | **씬 "크기조절" 모드 — 피벗(반대쪽) 고정 앵커 리사이즈.** Unity 스케일 툴은 피벗(보통 중심) 기준이라 양쪽이 같이 커짐 → 한쪽 고정 스트레치가 안 됨. 카탈로그에 **"크기조절" 토글**(배치 모드와 상호배타) 추가: 씬에서 프롭 선택 → 4개 가장자리 핸들 드래그 = **반대쪽 에지 고정**하고 그 축만 늘림(드래그 후 위치 보정으로 고정쪽 제자리). **Tiled/Sliced=`SpriteRenderer.size`, 그 외=`localScale`** 변경(콜라이더는 `ColliderAutoFit`이 자동 추종). Shift=중심 고정 대칭. ESC 종료. 회전 0 가정. |
| 2026-06-04 | **일괄 등록 ID = 인덱스 증가(이름 충돌 버그 수정).** 기존 `{prefix}_{스프라이트이름}`은 이름이 같은 스프라이트끼리 ID가 겹쳐 전부 건너뛰어 **0개 등록(무반응)** 됐음 → `{prefix}_{0001..}` 자동 증가로 항상 고유(표시 이름만 스프라이트 이름). 같은 카테고리에 **이미 등록된 스프라이트는 건너뜀**(재실행 중복 방지) + 결과 팝업 피드백. 기존 이름-기반 에셋/프리팹은 메뉴 **`Tools▸TopDown▸Map▸Prop ID 인덱스로 정규화`** 로 일괄 개명(propId+.asset+.prefab, GUID/참조 보존). |
| 2026-06-04 | **천장 반투명(에디터) 토글 + 스냅 설명.** 상단 바에 **"천장 반투명"** 토글(+알파 슬라이더) — 맵 편집 중 천장이 바닥을 가릴 때 켜면 천장 스프라이트를 반투명(에디터 화면 전용). 저장 가드(`sceneSaving`/`맵 저장`/창 닫기 시 불투명 복원→재적용)로 **씬·프리팹엔 안 구워짐**, 플레이엔 영향 없음(`CeilingFader.Awake`가 알파 1 리셋). **스냅 = 배치 격자 크기**(배치 모드 격자 스냅·드래그 연속배치 간격·고스트 칸, 0=자유) — 라벨에 툴팁. |
| 2026-06-03 | **천장(Ceiling) 탭 추가 + 컷어웨이.** `Category`에 `Ceiling` 추가(enum 끝), 탭 `…/데칼/천장`, ID `ceiling_`. 기본값: **콜라이더 None·그림자 OFF·정렬 `Ceiling`(최상단)**. 빌더가 천장 프롭에 트리거(footprint) + `CeilingFader` 자동 부착 → 플레이어 건물 진입 시 지붕 알파 페이드아웃(건물 단위 `ceilingGroupId` 그룹). 폼에 천장 전용 섹션(그룹 ID·숨김 알파·페이드 속도). 시스템 상세는 [`rendering.md`](rendering.md). |
| 2026-06-03 | **데칼(Decal) 탭 추가.** `Prop2DDefinition.Category`에 `Decal` 추가(enum 끝에 → 기존 직렬화 인덱스 보존), 카탈로그 탭 `바닥/벽/프롭/오브젝트/데칼`. ID 접두어 `decal_`(자동). 기본값: **콜라이더 None(통과)·그림자 OFF** — 바닥/벽 위에 얹는 평면 장식 오버레이(핏자국·그을음·균열·낙서). 정렬은 자동 오프셋 안 박음(양수 Order는 같은 레이어 캐릭터까지 덮음) — 데칼 전용 Sorting Layer를 바닥보다 위·엔티티보다 아래로 두고 Visual/일괄 설정에서 지정. |
| 2026-06-03 | **프롭 기능(Function) 부여 — 옛 MapObject 기능 복구.** `Prop2DDefinition.function`(모든 프롭에): None / **SpawnPoint** / **Interactable**(InteractType: ExitPoint/MapBoard/Bed/Workbench 등) / **LootContainer**(수색) / **ItemDrop**(바닥 아이템, ItemSpawnPoint Fixed/Ground) / **NPC**(NPCController+npcData) / **Door**(DoorController, 프롭 콜라이더=문) / **Trigger**(영역→씬전환, 새 `MapTriggerZone2D`). **투명 마커(noVisual)**=SpriteRenderer 없이 기능만 + `Prop2DMarker` 기즈모(SpawnPoint는 자체 기즈모). `Prop2DBuilder.ApplyFunction`이 배치/프리팹 빌드 시 컴포넌트 자동 부착(private 필드 reflection). 오브젝트 탭 새 항목은 기본 Interactable. | 옛 `MapObjectSpawner`(클린 슬레이트로 삭제) 기능을 Prop2D로 통합 — "오브젝트=기능"을 모든 프롭에 부여 가능하게. |
| 2026-06-03 | **씬 편집 자동연동 — 그림자·콜라이더가 SpriteRenderer를 따라가게.** 맵툴 배치본 말고 씬에서 직접 수정한 벽도 반영. ① `GroundShadow2D`에 에디터 `Update()` 라이브 동기화(스프라이트·flip·Tiled size·정렬·Cutoff 변경 감지 시 그림자 자식 재동기화, 변경 시에만·런타임 0). ② 새 `ColliderAutoFit`(`[ExecuteAlways]`) — BoxCollider2D를 SpriteRenderer(Tiled size 또는 sprite.bounds)×scale+offset에 지속 맞춤. ③ 메뉴 `Tools▸TopDown▸Map▸선택에 Collider Auto-Fit/GroundShadow 부착`(손제작 벽 일괄 부착). ④ **맵 저장 시 ColliderAutoFit 자동 부착**(SaveMapPrefab이 맵 하위 SpriteRenderer+BoxCollider2D 프롭에 일괄, 없을 때만) — 매번 메뉴 안 눌러도 됨. 동기화는 `EditorApplication.update`(매 에디터 틱)로 — `[ExecuteAlways]` `Update()`가 에디트 모드에서 띄엄띄엄 불려 안 따라오던 것 해결. Polygon/Composite는 대상 아님(Box 전용). | 벽을 씬에서 세세히 수정(스프라이트/길이) 시 발밑 그림자·콜라이더가 빌드 시점에 고정돼 안 따라오던 문제 해결. |
| 2026-06-03 | **바닥 불규칙화 3종(터레인 페인팅 느낌).** ① `FloorPixel` 타일 반복 깨기(`_BREAKUP_ON` — 월드 좌표 노이즈로 명암 변주, 격자감 제거, 자동). ② **데칼 스캐터 브러시**(`DecalScatterBrush`, Tools▸TopDown▸Map) — 씬뷰 좌클릭 드래그로 데칼 흩뿌리기(반경·간격·개수·스케일·회전·색 지터, `FloorDecals` 부모, Undo, 지우개). ③ **가장자리 자동 디테일** — 선택 SpriteRenderer(벽/구조물) 외곽 둘레에 데칼 흩뿌림(이끼·잔해·금). 데칼은 `BRB/DecalPixel` 머티리얼 사용. | 통짜로 그린 참조 맵의 불규칙 바닥을 Tilemap에서도. 스플랫맵(다중 텍스처 블렌드)은 무거워서 제외. |
| 2026-06-04 | **맵 저장 시 발밑 그림자/오버레이 사라지던 버그 수정.** `GroundShadow2D`·`Weathered`·`Breakable`의 자식(발밑 그림자/풍화/손상 오버레이)은 `HideFlags.DontSave`라 `SaveAsPrefabAssetAndConnect`가 저장하며 떼어내는데, 인스턴스 재연결 직후 에디터에서 자동 재생성이 안 돼 **사라져 보였음**. → 세 컴포넌트에 `public Rebuild()/RebuildOverlay()` 추가 + `SaveMapPrefab`이 저장 후 `EditorApplication.delayCall`로 맵 하위 전부 강제 재생성. (그림자는 의도대로 프리팹엔 안 굽힘 — 런타임/로드 시 재생성, 에디터 표시만 복원.) | 저장하면 그림자가 사라진다는 사용자 보고. |
