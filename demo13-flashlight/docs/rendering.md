# Rendering System

## 렌더링 방식 (확정)

하이브리드 2D+3D 구조. 바닥은 2D, 벽/건물은 3D 큐브.

| 요소 | 렌더 방식 | 이유 |
|---|---|---|
| 바닥(Floor) | 2D Plane (Sprite, Rotation 90°) | 평면 타일, 2D 텍스처로 충분 |
| 벽(Wall) | 3D Cube (WallBuilder) | 플래시라이트 빛 차단 + 그림자 캐스팅 필요 |
| 건물(Building) | 3D 프리팹 직접 배치 | 빛 차단 + 입체감 (구 BuildingCubeBuilder 절차 생성 폐기, 2026-05-29) |
| 프랍(Prop) | 2D Sprite | 장식물은 2D로 충분 |
| 지붕(Roof) | 2D Sprite (상위 RoofController) | ON/OFF 토글 필요 |

## 핵심 원칙

- **벽/건물 큐브는 얇게** — 너무 두껍거나 투박하면 어색함. 자연스러운 두께 유지
- 벽 기본 두께: `0.08` (tileSize 기준)
- 건물 큐브 기본: footprint에 맞춘 가로/세로, 높이만 별도 지정
- 플래시라이트가 벽/건물 뒤를 비추지 못하게 하는 것이 핵심 메커니즘
- 입체감은 실제 3D 높이로 구현 (Normal Map 가짜 입체감 X)

## 라이팅

- 3D Light (Spot Light) 사용 — 플래시라이트 (실제 라이트, 가짜 원뿔 메시 아님)
- 3D 큐브가 물리적으로 빛을 차단하여 그림자 자동 생성
- URP 2D Light는 사용하지 않음 (하이브리드 3D 구조이므로)

### 2D 스프라이트 벽 + 그림자 프록시 (안전가옥 등)

천막/펜스/컨테이너처럼 **2D 아이소 스프라이트로 그린 구조물**도 실제 라이트의 빛을 막아야 함.
스프라이트(quad)는 납작해서 그림자가 제대로 안 나오므로 **그림자 전용 3D 박스**를 별도로 둔다.

| 요소 | 비주얼 | 빛 차폐(그림자) |
|------|--------|----------------|
| 바닥 | 정사각 탑다운 텍스처 (90° 눕힘) | - |
| 철창 펜스 | 2D 아이소 스프라이트 | **없음** (see-through, 빛 통과가 자연스러움) |
| 솔리드 벽/컨테이너 | 2D 아이소 스프라이트 | 얇은 3D 박스 `ShadowCastingMode = ShadowsOnly` |
| 오브젝트 프랍 | 2D 아이소 스프라이트 | 필요시 박스 |

- **그림자 박스**: 렌더 안 됨(`ShadowsOnly`), 그림자만 던짐 → 유니티 기본 Cube, 아트 에셋 추가 작업 없음
- **철창 예외**: 철망은 빛이 통과하는 게 현실적 → 그림자 캐스터 두지 않음(또는 별도 레이어로 차폐 제외)
- 비주얼은 전부 2D 스프라이트 유지 → 아트 제작 워크플로 안 바뀜

#### 구현 (맵툴 오브젝트 제작 단계, 2026-05-29)

프롭(Prop) 제작 단계에서 **"벽(빛 차폐)" 체크**로 그림자 프록시 박스를 붙인다.
대각선/ㅅ자/V자 벽도 박스 yaw(각도)로 비스듬히 눕혀 처리.

| 요소 | 위치 | 역할 |
|------|------|------|
| `ShadowBox` (struct) | `Data/PropDefinition.cs` | 박스 1개 사양: `size`(길이/높이/두께), `offset`, `yaw`(대각선 각도) |
| `PropDefinition.castsShadow` / `shadowBoxes[]` | `Data/PropDefinition.cs` | 벽 체크 여부 + 박스 목록 |
| `ShadowProxyBuilder` | `Rendering/ShadowProxyBuilder.cs` | 박스 생성 + 형태 프리셋(`Straight/Diagonal/Peak/Valley`) |
| 호출: 에디터 프리뷰 | `MapBuilderManager.SpawnPropVisual` | 배치 확인용 **반투명 주황 슬랩**(`ShadowCastingMode.On`)으로 표시 |
| 호출: 런타임 | `PropManager.SpawnProp` | **`ShadowCastingMode.ShadowsOnly`** (안 보이고 그림자만) |
| 카탈로그 UI | `MapCatalogEditor` Props 탭 | 벽 체크 + 형태 프리셋 드롭다운 + 박스별 size/offset/yaw 인라인 편집 |

- 박스는 프롭 비주얼의 **자식**으로 붙어 프롭의 `yRotation`/`scale`을 그대로 상속
- 박스 콜라이더는 제거 — 빛 차폐만 담당. 이동 차단은 `blocksWalkability`가 별도 처리
- **대각선 1장**: 박스 1개 `yaw=45`. **ㅅ/V자**: 박스 2개 조합(프리셋 자동 생성, 값 미세조정 가능)
- **정밀도**: 런타임에서 ShadowsOnly(안 보임)이므로 얼추 덮으면 OK. 스프라이트 라인에 정밀 매칭 불필요

## 벽 가림 처리 (Occlusion)

### 개요
플레이어/적이 벽 뒤에 있을 때의 가시성 처리. 두 가지 상황을 구분:

| 상황 | 처리 방식 | 담당 |
|---|---|---|
| 건물 **내부 진입** | 앞면 벽 + 천장 알파 페이드 (내부 보이게) | BuildingInterior.cs |
| 벽 **뒤편 통과** (미진입) | 캐릭터 아웃라인만 벽 위에 표시 | WallOcclusionOutline.cs |

### 건물 내부 진입 (BuildingInterior)
- 건물에 Box Collider (Trigger) 배치
- OnTriggerEnter/Exit로 플레이어 진입 감지
- **벽/천장**: `_Alpha` MaterialPropertyBlock으로 페이드 아웃 (진입 시 투명)
- **내부 프랍**: 반대로 페이드 인 (진입 시 표시, 퇴장 시 숨김)
- CityWall.shader, CityBuilding.shader에 `_Alpha` 프로퍼티 추가
- 목표 알파: 벽 0.15 (약간 보임), 프랍 0→1, 페이드 속도: 5
- `interiorPropRoot`에 내부 프랍 루트 Transform 할당 (자동 탐색 지원)
- 프랍 렌더러 캐시: `RebuildPropCache()` — 동적 추가 시 호출
- `_Alpha` 없는 머티리얼은 `_Color.a` / `_BaseColor.a` 폴백
- BuildingRenderer: `occludesInterior=true`인 BuildingDefinition은 자동으로 BuildingInterior 부착
  - 프리팹 자식 이름 분석: "Wall/Roof/Front" → 벽, "Interior/Props/Furniture" → 프랍

#### 맵 빌더 연동 (건물 소속)
- PlacedProp / PlacedMapObject에 `parentBuildingId` 필드 추가
- 맵빌더 UI: Prop/MapObject 배치 시 **건물 소속** 패널 표시 (없음 / 건물 목록 선택)
- 선택한 건물이 있으면 배치하는 프랍/오브젝트가 해당 건물의 내부 프랍으로 등록
- 런타임 흐름:
  1. BuildingRenderer가 건물 프리팹 인스턴스화
  2. PropManager/MapObjectSpawner가 `parentBuildingId`가 있는 항목을 건물 GO 하위 "Interior" 루트에 배치
  3. BuildingInterior가 Interior 하위 모든 Renderer 자동 수집 → 진입/퇴장 시 페이드 관리

### 벽 뒤 아웃라인 (WallOcclusionOutline)
- 카메라→유닛 레이캐스트로 벽 차단 감지
- OcclusionOutline.shader: 2-pass 스텐실 기반 외곽선만 렌더링
  - Pass 1: 법선 방향으로 정점 확대 (ZTest Greater — 벽 뒤에서만)
  - Pass 2: 원본 크기로 내부 스텐실 클리어 (외곽선만 남김)
- 플레이어: 흰색 아웃라인
- 적: 빨간색 아웃라인 (적 컨트롤러에서 색상 설정)
- wallLayerMask로 감지 대상 제한

### 기획 결정
- 날짜: 2026-05-26
- 질문: 벽 뒤 캐릭터 처리 방식?
- 결정: 건물 진입 = 앞벽+천장 알파 페이드, 벽 뒤 = 타르코프 스타일 아웃라인만. 적도 아웃라인.
- 근거: 단순 알파로 벽 전체를 투명하면 내부가 노출돼 어색함. 상황 분리가 자연스러움.

---

## 변경 로그

| 날짜 | 질문 | 결정 | 근거 |
|---|---|---|---|
| 2026-05-23 | 건물도 큐브로 렌더링할까? 3D 쓸 필요 없나? | 벽/건물 = 3D 큐브, 바닥 = 2D Plane 하이브리드 확정 | 플래시라이트 빛 차단 + 입체감에 3D 필요. 단, 큐브가 두껍거나 어색하지 않게 조절 |
| 2026-05-26 | 벽 뒤 캐릭터 가시성 처리 방식? | 건물 진입 = 앞벽+천장 알파 페이드 (트리거), 벽 뒤 = 아웃라인만 (타르코프 스타일). 기존 단색 실루엣 → 2-pass 스텐실 외곽선으로 교체. | 내부 구현 전제 — 단순 알파는 내부 노출 문제. 상황별 분리 처리. |
| 2026-05-29 | 2D 스프라이트 벽도 실제 라이트 빛을 막아야 하는데 quad로 되나? | **하이브리드: 비주얼=2D 아이소 스프라이트, 빛 차폐=얇은 3D 박스(ShadowsOnly).** 철창 펜스는 see-through라 그림자 캐스터 없음(빛 통과), 솔리드 벽/컨테이너만 그림자 박스. | 실제 스포트라이트 사용 → 차폐엔 3D 그림자 캐스터 필요. 납작 quad는 그림자 부적합. 박스는 유니티 기본 큐브라 아트 작업 없음. 아트는 2D 유지. |
| 2026-05-29 | 위 결정(그림자 프록시)을 맵툴 오브젝트 제작 단계에서 어떻게 켜나? 대각선/ㅅ/V자 벽은? | **프롭 제작 시 "벽(빛 차폐)" 체크 → 그림자 박스 부착.** 박스마다 `yaw`로 각도를 줘 대각선 벽 처리, ㅅ/V자는 박스 2개 프리셋. 에디터는 반투명 슬랩으로 보이게, 런타임은 ShadowsOnly. (구현: `ShadowProxyBuilder`, `PropDefinition.castsShadow/shadowBoxes`) | 결정만 있고 미구현이던 차폐 시스템을 실제 워크플로에 연결. 박스 각도/조합으로 임의 형태 벽 차폐 가능. |
| 2026-05-30 | 건물 뒤로 가거나 밖으로 나올 때 내부 프랍이 한번에 꺼져야 함 | **BuildingInterior 통합**: 벽 페이드 아웃 + 내부 프랍 페이드 인을 하나의 트리거로. interiorPropRoot 하위 전체 Renderer 관리. BuildingRenderer가 occludesInterior=true일 때 자동 부착. | 벽만 투명해지면 빈 건물 내부가 보여 어색함. 프랍을 함께 관리해야 완성도 있는 건물 진입 연출. |
| 2026-05-30 | 맵툴에서 우측/앞쪽 3D 벽이 바닥 타일에 묻히는 정렬 문제, 수동 조절 가능한가? | **(둘 다 적용) ①자동 깊이 정렬: 바닥 스프라이트는 Transparent 큐, 벽은 Opaque 3D 메시라 sortingOrder가 안 먹음 → 바닥 타일 Y를 -0.05 내려 벽 밑동(Y=0)이 깊이 테스트에서 이기게 함. 또한 벽 타일을 TileRenderer가 평면 스프라이트로 중복 렌더하던 것 제거(벽은 WallBuilder 3D 큐브로만). ②수동 미세조정: 프랍 인스턴스별 `sortingOffsetOverride` 추가, 지우개 모드에서 `[`/`]` 키로 ±1. | 벽=Opaque/바닥=Transparent 혼합이라 sortingOrder 단일 기준으로 안 풀림. 깊이버퍼 기반이 근본 해결, 예외는 프랍 수동 오프셋으로 보정. |
| 2026-05-30 | 맵툴에서 건물끼리 order 지정 + 프랍/건물이 다른 오브젝트에 묻힐 때 맵툴에서 조절 | **①정의별 기본 order**: `BuildingDefinition.sortingOffset`(이미 존재하나 미적용이던 것)을 SpawnBuildingVisual·프리팹 저장에 적용 → 카탈로그 에셋에서 건물 종류별 기본 정렬 지정. **②인스턴스별 미세조정**: `PlacedBuilding.sortingOffsetOverride` 추가, 프랍과 동일하게 지우개 모드에서 `[`/`]` 키로 ±1. (프랍은 2026-05-30 선행 결정) **주의**: 불투명 3D 메시(벽/일부 건물)는 sortingOrder가 안 먹고 깊이로 정렬되므로 이 오프셋은 스프라이트 기반에만 시각 효과. 레이어 전환 UI는 아직 없음(타일→Ground, 벽→Walls 자동). | sortingOrder 단일 체계로는 Opaque 메시를 못 옮기지만, 스프라이트 프랍/건물의 앞뒤 보정은 인스턴스 오프셋으로 충분. 정의별 기본값으로 "건물끼리 order"를, 인스턴스 오프셋으로 예외 케이스를 커버. |
| 2026-05-30 | 빌딩 카탈로그 에디터에서 이미지 뎁스를 직접 조절하고 싶다 | 빌딩 등록/수정 폼(`MapCatalogEditor`)에 **"이미지 뎁스 (정렬 오프셋)"** 정수 필드 노출 → `BuildingDefinition.sortingOffset`을 로드/저장. 추가로 편집 시 `materialPreset`이 비어있으면 프리팹 실제 머티리얼로 폼을 채우는 폴백 + 신규 생성 시 materialPreset 저장(머티리얼 None으로 풀리던 버그 수정). | sortingOffset 필드는 존재했으나 폼에 없어 인스펙터로만 조절 가능했음. 카탈로그 단계에서 종류별 기본 뎁스를 바로 지정할 수 있게 함. |
| 2026-05-30 | 건물 바닥이 일체형이라 위에 프랍을 놓으면 어색함 → 건물을 타일+벽으로 조립할까? | **진입 가능한 건물 = 타일 바닥 + wall 조각 조립으로 구성** (일체형 프리팹은 배경/장식용으로만). 바닥은 기존 타일 시스템, 벽은 WallBuilder 3D 큐브(셀 단위)로 쌓음. | 바닥/벽이 셀 단위라 그리드 정렬·walkability·편집 자유도 확보. 또 프랍이 어느 벽보다 앞/뒤인지 셀 좌표로 결정돼 깊이 정렬이 깔끔해짐(일체형은 "통짜 vs 프랍"이라 끼우기 애매). 진입 시 앞벽+천장 페이드도 벽이 셀 단위라 연동 쉬움. |
| 2026-05-30 | 바닥 위/건물 안에 놓은 프랍이 물리적으로 "뒤로 가는" 것처럼 보여 문제되지 않나? | **문제 없음.** 수직 Y-빌보드라 스프라이트 발이 셀 원점에 박혀 서 있을 뿐(위치 이동 없음). 정렬은 프랍↔프랍↔타일은 셀 `(x+y)` 기준 자동, 프랍↔벽(3D)은 깊이버퍼 기준(바닥 -0.05). 건물 내부 앞벽/뒷벽 겹침만 진입 연출 작업 시 인스턴스 `sortingOffsetOverride`로 보정. | 이전 "뒤로 눕던" 버그는 풀 빌보드 카메라 피치 문제였고 수직 빌보드로 이미 해결. 착시일 뿐 실제 셀 이동 아님. 조립형 벽(셀 단위)이라 앞/뒤 판정도 좌표로 명확. |
| 2026-05-30 | 벽에 프랍을 붙일(걸) 수 있나? (포스터/액자/스위치 등) | **두 종류 구분.** ①벽 옆에 세우기(선반/통/상자) = 깊이버퍼로 바로 됨, 추가 작업 없음. ②벽에 걸기(포스터/액자) = **구현 완료(아래 참조)**. 향후 뷰를 탑다운으로 전환할 가능성은 여전히 열어둠(전환 시 벽부착 자체가 불필요해짐). | ②는 빌보드가 카메라 따라 돌아 벽에서 뜬 것처럼 보임 + 높이 없음 → 벽 부착 모드로 해결. |
| 2026-05-30 | 타일 한 칸 크기가 너무 크고, 그리드 칸 수/칸 크기를 설정할 데가 없음 | **New Map 다이얼로그에 "Tile Size" 입력 추가** (기존 Width/Height = 칸 수, 신규 Tile Size = 한 칸 월드 크기 m, 0.1~10 클램프). `NewMap(w,h,tileSize=1)`로 시그니처 확장, 부트스트랩에도 `tileSize` 노출(`MapBuilderBootstrap`/`defaultTileSize`)해 최초 맵에도 적용. 맵은 1회만 만들면 되므로 별도 상시 UI 대신 생성 시점에 한 번 정하는 방식. | tileSize가 `NewMap`에 1f로 하드코딩돼 있어 조절 불가였음. GridToWorld·벽·프롭 모두 gridSettings.tileSize를 쓰므로 이 값만 바꾸면 그리드 밀도가 일괄 조정됨. |
| 2026-05-30 | 벽이 바닥에서 붕 떠 보임(건드리지 않은 벽도) + 씬에서 개별 벽 크기를 못 키움 | **①벽 밑면 = y=0 유지**(바닥 -0.05 오프셋은 깊이테스트용일 뿐 위치 기준 아님 — 한때 벽도 -0.05로 내렸다가 0으로 되돌림). 크게 떠 보이는 건 0.05가 아니라 **벽 스프라이트 PNG 아래쪽 투명 여백**이 유력(이미지가 면에 0~1 UV로 늘어나므로 아래 빈 공간만큼 그림이 떠 보임 → 아트에서 여백 제거 필요). **②벽 리사이즈**: 리사이즈(R) 후보에 벽 추가. `PlacedTile.wallScale`(직렬화·프리셋 포함) 추가, R+스크롤로 **이미지 비율 유지(길이+높이 함께, 두께 제외)** 배율 조절. 프롭/건물은 기존대로 균등 배율. | 벽은 PlacedTile이라 균등 scale 개념이 없었음. 두께는 그대로 둬야 벽 느낌이 유지되고, 길이+높이만 비율로 키워야 텍스처 왜곡 없음. 카탈로그 wallHeight는 정의 전체 일괄, wallScale은 인스턴스별. |
| 2026-05-30 | 벽(Wall)에 스프라이트 이미지를 입히면 흰색 슬랩으로만 보임 + 벽 높이를 이미지 크기에 맞추려면? | **①버그수정**: 머티리얼(CityBuilding 등)이 지정되면 그 머티리얼을 쓰되 **벽 스프라이트 텍스처를 베이스맵(_BaseMap/_MainTex)으로 주입**하도록 수정(WallBuilder·카탈로그 프리뷰 둘 다). 머티리얼=셰이더/룩, 스프라이트=실제 이미지. 원본 머티리얼 오염 방지 위해 복제 후 주입. **②사이즈**: 벽 폭은 항상 1타일 고정, 이미지는 면에 0..1 UV로 늘어나 채워짐 → "캔버스 크기"가 아니라 **이미지 종횡비**에 높이를 맞춰야 왜곡이 없음. 카탈로그 Wall 폼에 **"이미지 비율로 높이 맞춤"** 버튼 추가(`높이 = 스프라이트 h/w`). | 머티리얼만 쓰고 스프라이트를 무시해 텍스처가 안 보이던 것. 벽 폭 고정+UV 스트레치라 높이만 비율 맞추면 됨. |
| 2026-05-30 | 벽 길이를 2로, 그리드 칸을 0.5로 했더니 벽이 그리드에 안 맞음 | **긴 스냅 벽의 길이축 그리드 정렬 보정 추가**(`WallBuilder.GridAlignShift`). 원인: 셀 중심이 그리드 라인 사이 0.5칸에 있어, 길이가 **짝수 칸**(예: 2m ÷ 0.5 = 4칸)이면 벽 양 끝이 셀 중심에 걸려 반 칸 어긋남(한 칸·홀수 칸은 우연히 맞았음). 보정: `center - length/2 ≡ 0 (mod tileSize)`가 되도록 길이축(N/S=X, E/W=Z)으로 ±반 칸 시프트. WallBuilder(배치·리빌드·로드)와 고스트 미리보기에 동일 적용. 자유 배치 벽은 보정 안 함(임의 위치). | 스냅 벽은 "셀 모서리 중심" 기준이라 한 칸 길이만 자동 정렬됐음. 임의 길이도 양 끝이 그리드 라인에 떨어지게 보정해야 칸 경계와 맞물림. (주의: R 리사이즈로 길이를 바꾸는 라이브 프리뷰 중에는 X/Z 재정렬이 즉시 반영되지 않고, 저장/리로드 시 정렬됨.) |
| 2026-05-30 | 카탈로그에서 기존 항목을 복붙(전체 복제)하고 싶다 | **각 항목 행에 "복제" 버튼 추가.** 클릭 시 해당 항목을 폼에 그대로 불러오되 `editingIndex=-1`(편집 아님) + `GenerateNextId()`로 새 ID를 잡아 **신규 생성 상태**로 만든다. 사용자가 폼에서 일부만 고치고 "생성 & 등록"을 누르면 별도 에셋이 만들어짐. 모든 탭(타일/벽/프롭/건물/오브젝트) 공통 `DuplicateToForm(i)` 헬퍼. 건물/오브젝트는 편집 로드와 동일하게 프리팹에서 텍스처·스케일을 역산해 채움. | 비슷한 벽/타일을 매번 처음부터 등록하기 번거로움. 한 클릭으로 폼 프리필 → 일부 수정 후 새 항목 생성하는 워크플로가 가장 직관적(클립보드 2단계보다 단순). |
| 2026-05-30 | 카탈로그에서 벽 높이뿐 아니라 길이도 조절하고 싶다 + 다른 탭/항목 편집 누르면 이전 선택 취소 | **①벽 길이 필드 추가**: `TileDefinition.wallLength`(m, **0=타일 한 칸 자동**). WallBuilder·리사이즈(ApplyWallScaleToGo)·고스트·카탈로그 프리뷰 모두 `wallLength>0 ? wallLength : tileSize`로 길이 산출(모서리 스냅 오프셋은 기존 tileSize 유지). "이미지 비율로 높이 맞춤" 버튼은 길이×종횡비로 갱신. 기존 에셋은 0이라 동작 불변. **②선택 취소**: 탭 전환 시 `showCreateForm=false`까지 리셋, 다른 항목 `편집` 클릭(Load*ToForm) 시 `DestroyPreview()`로 이전 씬 미리보기 제거. | 길이가 1타일 고정이라 긴/짧은 벽 종류를 못 만들었음(0=자동으로 하위호환). 편집 대상 전환 시 이전 미리보기/하이라이트가 남아 헷갈리던 UX 정리. |
| 2026-05-30 | 벽은 스냅일 때 동서남북(90°)만, 자유 모드(G로 스냅 OFF)에선 자세히 회전·배치하고 싶다 | **스냅 ON = 벽이 셀 모서리 N/E/S/W 4방향(90° 스텝)으로만 회전.** Q/E가 90°씩 돌고 키는 `x_y_E{rot}`(셀+모서리당 1개). **스냅 OFF = 자유 각도 + 자유 위치.** 마우스 월드 위치에 그대로 놓이고 Q/E=±15°·Shift+Q/E=±1° 자유 회전(yRotation), 각 벽은 GUID `id`로 식별(`F{id}` 키, 한 셀에 여러 개 공존). `PlacedTile`에 `freePlace/worldPosition/yRotation/id` 추가(직렬화·프리셋 포함), WallBuilder가 freePlace면 worldPosition+Euler(0,yRot,0)로 배치. 지우개/이동/리사이즈 모두 자유 벽 지원(자유 벽은 XZ 최근접으로 탐색). | 스냅 벽은 그리드 정렬·벽 1칸 단위라 4방향이 자연스럽고 walkability와도 맞음. 하지만 비스듬한 벽·임의 위치 칸막이가 필요할 때가 있어 스냅을 끄면 완전 자유 배치를 허용. 두 모드를 키(id vs 셀모서리)로 분리해 충돌 없이 공존. |
| 2026-05-30 | 천장을 2층(다층)에도 쓸 것 같다 → 층 시스템으로 일반화 | **천장을 일회성으로 만들지 않고 "층(Level) 시스템"으로 통합.** 방식: **같은 맵에 층을 쌓는 Zomboid식**(별도 인테리어 맵 아님). 기존 `MapData.layers`(`MapLayer`)를 층으로 확장 — 각 층에 **Y 높이(elevation) + levelIndex** 부여. **2층 바닥 = 1층 천장**(별도 천장 타입 불필요, 위층 바닥 타일이 곧 아래층 지붕). 맵툴에 "현재 층" 선택기 → 선택 층의 높이에 타일/벽/프롭 배치·렌더. **가시성**: "방 들어가면 천장 페이드"를 일반화해 **현재 층보다 위 층은 컷어웨이(가림/페이드)**. 층 이동은 계단. **범위: 풀 구현**(기반 + 위층 컷어웨이 + 계단 이동 + 층별 walkability). 이전 "천장=연결 천장셀 플러드필" 결정은 이 층 시스템으로 대체. | 천장이 결국 2층 바닥과 같은 것이라, 천장 단독 시스템은 곧 버려질 코드. 층 개념으로 만들면 천장·2층·옥상이 한 메커니즘으로 풀림. 같은 맵 쌓기를 택한 건 외관+내부 2층이 한 화면에 공존하고(타르코프/좀보이드 느낌), 별도 맵 로드의 끊김이 없기 때문. `layers` 인프라가 이미 있어 Y높이만 얹으면 됨. |
| 2026-05-30 | (대체됨) 천장 단독 시스템 | 직전에 "천장=인접 천장셀 플러드필 방, 진입 시 페이드"로 정했으나, 사용자가 2층에도 쓸 거라 해서 **위 "층 시스템"으로 대체**. 천장은 별도 타입 없이 위층 바닥으로 처리. | 
| 2026-05-30 | 카탈로그 "복제"는 폼 검토 없이 바로 생성되게 + 선택/미리보기 "오버"가 다른 버튼·편집·저장 눌러도 안 빠짐(ESC만 됨) | **①복제 = 즉시 생성**: 행의 "복제" 버튼이 폼에 채우고 검토하던 방식(`DuplicateToForm`) → **그 자리에서 새 고유 ID로 에셋을 만들어 카탈로그에 바로 등록**(`DuplicateNow`, 모든 필드 복사, 스프라이트는 이후 편집에서 교체). GUI 레이아웃 도중 배열 변경 방지로 `EditorApplication.delayCall`+`GUIUtility.ExitGUI()`로 호출. **②선택 오버 해제**: 씬 미리보기를 `Selection.activeGameObject`로 선택시켜 아웃라인이 뜨는데 ESC(유니티 기본 선택해제)로만 사라지던 것 → `DestroyPreview()`가 그 선택까지 해제하도록 수정하고, `Select`/`X`/`복제` 버튼 경로에도 `DestroyPreview()` 연결(편집 전환·저장·탭전환은 기존에 이미 정리). | 비슷한 항목을 빨리 늘리려는데 "복제"가 한 번 더 생성 버튼을 눌러야 해서 안 되는 것처럼 보였음 → 한 클릭 복사가 직관적. 미리보기 선택 아웃라인이 다른 조작에도 남아 헷갈리던 것을 모든 전환 동작에서 정리. (이전 2026-05-30 "복붙(전체 복제)" 결정 대체) |
| 2026-05-30 | ② 벽 부착 프롭 구현 | **"벽 부착(Wall Mount)" 모드 추가.** `PropDefinition.wallMountable`/`defaultMountHeight`(카탈로그 에디터 노출), `PlacedProp.wallMounted`/`mountHeight`(직렬화·프리셋 포함). 렌더: `PropQuadBuilder.RootRotation(yaw, wallMounted)` — 부착이면 **빌보드 끄고 yaw(Q/E)만** 적용해 벽면에 납작 고정, 아니면 기존 빌보드×yaw. 위치는 `mountHeight`만큼 Y로 띄움. 맵툴: **B키 토글**, **PageUp/Down 높이 조절**(Shift 미세), 상태바 `▣Wall h=` 표시. 벽 부착 가능 프롭 선택 시 기본 ON+기본높이. 이동(Move) 모드에서도 회전/높이 유지. | 일체형 빌보드는 벽 장식에 부적합(카메라 추적·바닥 고정). yaw 고정+높이로 액자/포스터/스위치를 벽면에 자연스럽게 붙임. 뷰 전환 시 무력화돼도 데이터는 무해. |
| 2026-05-30 | 카탈로그에 천장을 따로 만들어줘야 하나? | **별도 천장 타입은 만들지 않음** — "2층 바닥 = 1층 천장"이라 위층 바닥 타일이 곧 아래층 천장. 다만 카탈로그/팔레트에서 천장용 타일을 구분할 수 있게 **`TileCategory.Ceiling` 값만 추가**(동작은 일반 바닥 타일과 100% 동일, 위층 L1+에 깔면 됨). 카탈로그 타일 폼에 "카테고리" EnumPopup 노출(Wall은 자동 제외) + Ceiling 선택 시 안내 HelpBox. `CreateTile`/`SaveEditedTile`/`LoadTileToForm`/`ResetForm`에 `newCategory` 배선. | 천장은 별도 타입을 만들면 곧 버려질 코드(층 시스템과 중복). 분류 라벨만 있으면 제작자가 천장용 타일을 골라 쓰기 쉬움. 동작 분기는 없어 유지비 0. |
| 2026-05-30 | 층 시스템 **Stage 1(기반) 구현** | **데이터+렌더+편집 기반 완료(타일·벽만).** `PlacedTile.level`(int, 0=1층) + `GridSettings.levelHeight`(기본 3m) 추가. 월드 Y = `level × levelHeight`. **렌더**: `TileRenderer` 딕셔너리 키를 `Vector2Int`→`Vector3Int(x,y,level)`로, 타일 Y에 층 높이 반영. `WallBuilder.floorY = level×levelHeight`. 프리팹 베이크 타일 Y도 동일. **데이터**: `MapData` 점유 캐시·`PlaceTile` 중복제거·`RemoveNonWallTilesAt`·`RemoveWallEdge` 모두 (cell,level)별로. 스냅 벽 키 `x_y_E{rot}`→`x_y_L{level}_E{rot}`(`WallKeyFor`/`ParseWallKey`). **편집**: `MapBuilderManager.CurrentLevel`(0~9)+`ChangeLevel`, 타일/벽 배치·지우개·이동·리사이즈·고스트가 현재 층 기준. 입력 `,`/`.` = 층 내리기/올리기, 상태바 `⌂L{n}` 표시, F1 도움말 갱신. 직렬화·프리셋에 `level` 포함. | "천장=위층 바닥" 결정의 토대. 렌더러가 이미 tile+settings를 받으므로 elevation을 그 안에서 계산해 시그니처 변경 최소화. Unity 검증 불가 환경이라 검증 가능한 단계로 분할 — Stage 1=기반(타일/벽), Stage 2=위층 컷어웨이+프롭/건물 층, Stage 3=계단/층별 walkability. 프롭·건물·맵오브젝트는 아직 층 미적용(Y=0 유지). |
| 2026-05-30 | 프롭 root 회전을 Y-빌보드 대신 고정 아이소 각도로 (이동 시 바닥 부착) | **프롭 root 회전 = 고정 `Euler(35.264, 45, 0)`**(트루 아이소 카메라와 동일). 쿼드 노멀이 카메라 시선과 정렬돼 스프라이트가 카메라 정면으로 평평하게 보이고, 다른 맵 오브젝트 root 규약과 일치. `PropQuadBuilder.BillboardRotation`을 수평 Y-빌보드 → 고정 아이소 각도로 교체(`RootRotation`·Build·고스트·이동·프리팹베이크·런타임 PropManager 전부 이 경로). **자식(Visual/NavBlocker) 회전은 0,0,0 유지** — root만 각도를 가짐. 이동(Move) 시 프롭은 항상 바닥 평면(Y=층높이)에 붙어 XZ로만 미끄러지며, 뷰상 "벽으로 떠오르는" 것은 아이소 투영 착시(실제론 먼 바닥 셀). | 사용자가 카메라를 따라 도는 Y-빌보드 대신 다른 오브젝트처럼 고정 아이소 각도로 세워 일관된 뷰를 원함. wallMounted(포스터/액자)는 기존대로 yaw만 적용(예외 유지). |
| 2026-05-30 | 층 시스템 **Stage 2(프롭/건물 층 + 위층 컷어웨이) 구현** | **①프롭/건물/맵오브젝트/수확물에 `level` 추가**(직렬화·프리셋 포함, 배치 시 `level=CurrentLevel`). **핵심 패턴**: `GetWorldPosition`은 **평면 유지(층 Y 미포함)**하고, 각 Placed*에 `ElevationY(settings)=level×levelHeight` 헬퍼를 둬 **비주얼 배치 지점에서만** Y에 더한다(에디터 스폰·런타임 스폰·프리팹 베이크·고스트·이동드래그). 이렇게 해야 거리/임계값 탐색(GetWorldPosition vs Y=0 셀월드 비교)이 그대로 동작. **②교차층 격리**: 모든 선택 탐색(지우개·이동호버·리사이즈호버·RemoveNearest*·ComputeFrontSortOverride)에 `if (x.level != CurrentLevel) continue;` 필터 추가 → 다른 층 오브젝트는 선택/삭제 안 됨. **③위층 컷어웨이(V키 토글, 기본 ON)**: `ApplyLevelCutaway()`가 `level > CurrentLevel`인 타일(`TileRenderer.ApplyLevelCutaway`)·벽·프롭·건물·오브젝트를 `SetActive(false)`로 숨김(건물은 내부보기 숨김 상태와 AND). `SetCurrentLevel`·`RebuildAllVisuals`에서 재적용. 상태바 `✂` 표시, F1 도움말 갱신. | Stage 1의 벽 방식(floorY 분리)을 그대로 일반화. 평면 GetWorldPosition+분리 ElevationY가 핵심 — 직접 Y에 층을 넣으면 1.5m 지우개 임계값이 위층(≈3m) 오브젝트를 영영 못 잡음. 컷어웨이는 "방 진입 시 천장 페이드"의 일반화(위층 전체 가림). 페이드 대신 즉시 숨김(단순·명확, 추후 알파 페이드로 교체 가능). |
| 2026-05-30 | 고정 아이소 각도로 바꾼 뒤 프롭이 바닥에서 떠 보임 | **두 가지를 함께 고침.** **①스프라이트 하단 접지(grounding):** `PropQuadBuilder.Build`에서 Visual 자식의 `localPosition.y = -sprite.bounds.min.y × fitScale` → 스프라이트의 시각적 하단을 root 원점(=셀 바닥점)에 올린다. 이 점은 **원점이라 회전 불변** — 어떤 yaw/빌보드 각도에도 바닥에 닿음(센터·바닥 피벗 자동 보정). **②에디터 프롭에서 `SortDepthOffset` 제거:** 그 오프셋은 `-IsoViewDir(=(-1,+1,-1)/√3)` 방향이라 sortOrder(셀 합)에 비례하는 **+Y 성분**이 있어 root를 띄웠다. 직교(ortho) 게임 카메라에선 시선축 이동이라 **화면상 안 보이지만**, 에디터 **원근 씬뷰**·접지 마커에선 떠 보임. 스프라이트 프롭은 `sr.sortingOrder`로 정렬되므로 이 위치 오프셋이 **불필요** → 4개 에디터 경로(SpawnPropVisual·이동·이동높이·고스트·프리팹베이크)에서 제거. **불투명 메시(건물)만 유지**(sortingOrder 무시·깊이버퍼 정렬). 런타임 PropManager는 원래부터 오프셋 없이 바닥에 정확히 붙음. | 1차로 접지만 고쳤으나 root 자체가 `SortDepthOffset`의 +Y로 떠서 잔존 부유. 원근 씬뷰에서 큰 리프트로 보였던 게 핵심 단서. 정렬은 sortingOrder가 전담하므로 위치 오프셋은 프롭에 무해히 제거 가능. 회전은 0,0,0 유지(아이소 룩 불변). |
| 2026-05-30 | (대체됨) 불투명 픽셀 자동 접지 트림 | `VisibleBottomLocalY`로 텍스처 알파를 스캔해 보이는 발치를 자동으로 바닥에 맞추려 했으나, **(1)** 텍스처 Read/Write가 꺼지면 폴백(여백 포함)하고 **(2)** 발치 소프트 그림자를 발치로 오인하는 등 결과가 스프라이트에 따라 달라져 "왜 0.25만큼 떠 있나" 식의 예측 불가가 생김. **→ 자동 트림을 제거하고 아래 "수동 접지 보정"으로 대체.** | 자동 추정이 오히려 혼란을 키움. 예측 가능한 단일 기준(rect 하단)+수동 노브가 사용자가 통제하기 쉬움. |
| 2026-05-30 | 프롭이 바닥에서 떠 보임 — 제작자가 직접 맞추고 싶음 (틸트축 말고 **순수 Y**로) | **프롭별 수동 접지 보정 `PropDefinition.groundOffset`을 root의 월드 Y 드롭으로 적용.** 기준 접지는 rect 하단(`Build`은 그대로, groundOffset 미적용)이고, 보정은 **비주얼 배치 지점에서 root의 월드 Y에서 빼서** 순수 수직으로 내린다(양수=아래). 헬퍼 `PlacedProp.GroundOffsetY`(=def.groundOffset, propDefinition null이면 0) 추가. 적용 경로 6곳: 에디터 스폰(`SpawnPropVisual`)·이동 높이(`ApplyMovePropMountHeight`)·이동 드래그·고스트·프리팹 베이크·런타임 `PropManager.SpawnProp`. 카탈로그 프롭 폼(case 2)에 "접지 보정" FloatField + 직렬화·로드·리셋·생성 배선. (이전 "Build의 localPosition.y에 더하는 방식"은 틸트축으로 비스듬히 움직여 직관 X → 폐기) | 사용자가 "유닛 말고 Y만 내리면 될 것 같다"고 요청. 빌보드/yaw로 root가 기울어 있어 로컬 Y 보정은 비스듬히 움직임 → 월드 Y 드롭이 1:1 직관적. ElevationY와 같은 "평면 GetWorldPosition + 비주얼 지점에서만 Y 보정" 패턴이라 거리/임계값 탐색은 불변. |
| 2026-05-30 | 배치 전 노란 고스트 마커가 "이상하게"(기울어져) 보이고, 박으면 초록 마커는 잘 보임 | **노란 고스트 마커를 바닥에 평평하게 고정.** 원인: 노란 마커(`AttachGroundMarker`)는 **로컬** 회전 `Euler(90,0,0)`인데 아이소 틸트(35.264,45,0) 고스트 root의 자식이라 그 틸트를 상속 → 기울어진 타원으로 떠 보임. 초록 마커(`AttachPlacedGroundMarker`)는 **월드** 위치(y=0.01)·회전으로 바닥에 평평. 고스트는 매 프레임 부모가 회전하므로, `UpdateGhostPosition` 끝에서 `GroundMarker` 자식의 **월드 회전=`Euler(90,0,0)`·Y=`levelY+0.01`**을 매 프레임 덮어써 평면 유지. 색은 노랑(미리보기)·초록(배치) 구분 유지. | 미리보기와 배치 결과가 시각적으로 일치해야 어디에 앉는지 가늠 가능. 부모 틸트 상속이 원인이라 월드 트랜스폼을 매 프레임 재고정하는 게 가장 단순(부모 회전 변화에 강건). |
| 2026-05-31 | 접지 보정(XYZ)을 맵툴 안에서 개별 프롭마다 조정 | **`PlacedProp.groundOffsetOverride`(Vector3) 인스턴스별 접지 오프셋 추가.** `GroundOffsetVec = 정의값(PropDefinition.GroundOffsetVec, 카탈로그 기본) + groundOffsetOverride`로 합산 — 카탈로그는 "프롭 종류 기본값", 맵툴은 "이 인스턴스만" 미세조정. 조정 UX는 기존 per-프롭 조작(회전 Q/E·플립 F)과 동일하게 **키보드**: `J/L`=X∓, `I/K`=Z±(깊이), `U/O`=Y±(높이), `P`=리셋, Shift=미세(0.01 vs 0.05). (넘패드 없는 키보드 위해 IJKL+UO+P 클러스터로 변경.) **두 경로 모두 지원**: ①**배치 중** — 회전/플립처럼 "현재 배치 설정"인 `MapBuilderManager.CurrentGroundOffset`(Vector3)을 넘패드로 조절 → 고스트에 즉시 미리보기되고 놓을 때 새 프롭의 `groundOffsetOverride`로 박힘. ②**Move 모드(7)로 집은 상태** — 집은 프롭(`_moveType==2`)의 override를 직접 누적/리셋 후 `ApplyMovePropMountHeight()`로 재배치. `AdjustGroundOffset`/`ResetGroundOffset`가 집은 프롭 유무로 분기, `ActiveGroundOffset`가 상태바 표시용 현재값(집은 인스턴스 또는 CurrentGroundOffset) 반환. 상태바 `⊹Off(x,y,z)`, F1 도움말 안내. 직렬화(`SerializableProp.groundOffX/Y/Z`)·프리셋(Map/Building) 복사 포함. override Y는 월드 좌표 그대로(+=위), 정의값 Y는 −groundOffset(부호 반대)임에 유의. | "맵툴 안에서 개별 프롭을 하나씩 컨트롤"+"이동 말고 설치할 때도" 요청. 카탈로그 정의값은 종류 기본값, 인스턴스 누적 오버라이드는 자리마다 발치 어긋남 보정. 배치 설정 방식은 회전/플립과 동일 패턴이라 놓기 전에 미리 맞춰 둘 수 있고, Move 경로는 이미 놓은 것 사후 보정. |
| 2026-05-30 | 접지 보정에 Z축 추가 + 프롭 카탈로그에서 벽 부착·이미지 뎁스 옵션 제거 | **①접지 보정 Z축 추가:** `PropDefinition.groundOffsetZ` 신설, 기존 `groundOffset`(Y)와 묶어 `GroundOffsetVec = (0, -groundOffset, groundOffsetZ)`(Y는 아래로 −, Z는 그대로 +) 월드 오프셋으로 노출. `PlacedProp.GroundOffsetY`(float)를 `GroundOffsetVec`(Vector3)로 교체. 적용 6경로(SpawnPropVisual·ApplyMovePropMountHeight·이동 드래그·고스트·프리팹 베이크·런타임 PropManager) 모두 `pos += GroundOffsetVec`로 변경 — Y만 내리던 것을 Y+Z 동시 보정. 카탈로그 프롭 폼에 "접지 보정 (Z)" FloatField + 로드·저장·리셋·생성 배선. **②벽 부착 가능 옵션 제거:** 프롭 폼(case 2)에서 `벽 부착 가능` 토글 + `기본 부착 높이` 필드 삭제, 관련 폼 변수(`newWallMountable`/`newDefaultMountHeight`)와 로드·저장·생성·리셋 배선 제거. (PropDefinition.wallMountable/defaultMountHeight 데이터 필드와 런타임 경로는 유지 — 직렬화·기존 에셋 호환). **③이미지 뎁스(정렬 오프셋) 제거:** 프롭 폼에서 "이미지 뎁스 (정렬 오프셋)" IntField 삭제, 프롭 저장·생성에서 `prop.sortingOffset` 대입 제거(기본 0 유지). `newSortingOffset` 변수는 건물 폼이 계속 쓰므로 보존. | Z 보정은 바닥 평면에서 앞뒤(시선 깊이) 위치를 맞추는 데 필요 — 스프라이트 발치가 셀 중심보다 앞/뒤로 어긋날 때. 벽 부착·이미지 뎁스는 실사용에서 불필요/혼란스러워 제작자 노출만 제거(데이터는 호환 위해 유지). |
| 2026-05-31 | 접지 보정을 root 말고 내부 이미지로, nav 콜라이더도 따라오게 | **오프셋을 root가 아닌 내부 `Content` 컨테이너에 적용해 이미지+콜라이더가 함께 이동.** `PropQuadBuilder.Build` 계층을 `root → Content → (Visual + NavBlocker)`로 바꿔 Visual과 NavBlocker를 Content 자식으로 묶음. `ApplyGroundOffset(root, worldOffset)`는 `Content.localPosition = root.InverseTransformVector(worldOffset)`(회전·스케일 상쇄 → 순수 월드 XYZ, 절대값이라 멱등·비누적)로 설정 → 이미지와 nav 박스가 함께 월드 오프셋만큼 이동. **root는 제자리**(셀 바닥점 = 그린 접지 마커·sortingOrder 정렬 기준 고정). `GroundMarker`는 root 직속이라 오프셋 영향 없음. 적용 6경로(SpawnPropVisual·ApplyMovePropMountHeight·이동 드래그·고스트·프리팹 베이크·런타임 PropManager)가 `pos += GroundOffsetVec` 대신 `position=basePos; ApplyGroundOffset(go, GroundOffsetVec)`로 변경(반드시 root 회전·스케일 확정 후 호출). | "보정은 root 말고 내부 이미지로, nav 막으면 콜라이더도 잘 따라와야 한다" 요청. root를 옮기면 정렬 기준점·접지 마커가 같이 흔들려 어디 앉는지 가늠 불가 → root는 셀에 고정하고 보이는 것(이미지)+막는 것(콜라이더)만 함께 민다. InverseTransformVector로 root 틸트와 무관하게 순수 월드 XYZ 보장. |
| 2026-05-31 | 접지 보정 키가 어렵고 일관성 없음 → "방향키 = 화면 이동" | **IJKL/UO/P 클러스터를 방향키 기반으로 교체.** `←/→`=좌우(X∓), `↑/↓`=위아래(Y, 높이 ±), `PageUp/PageDown`=깊이(Z ±), `Home`=리셋, Shift=미세(0.01 vs 0.05). 화면을 보며 "왼쪽/오른쪽/위/아래"로 직관적으로 미는 멘탈 모델. **방향키는 접지 보정 컨텍스트(`ActiveGroundOffset.HasValue`: 프롭 배치 중 또는 Move로 집은 상태)에서만 프롭을 밀고, 그 외엔 카메라 패닝** — `MapBuilderCamera.HandleKeyboardPan`이 그 컨텍스트일 때 방향키를 패닝에서 제외(WASD 패닝은 항상 유지). **PageUp/Down 라우팅**: 벽 부착 컨텍스트(`IsWallMountContext`: 집은 프롭이 wallMounted거나 배치모드+CurrentWallMount)면 부착 높이(`AdjustMountHeight`), 아니면 Z 오프셋. 상태바 `⊹Off(x,y,z)`·F1 도움말 갱신. | "키가 너무 어려운데 일관성 있게" 요청 → 객관식에서 사용자가 "방향키=화면 이동" 선택. 임의 글자키보다 방향키 공간 매핑이 학습 비용 0. PageUp/Down은 기존 벽 부착 높이 키를 흡수(겹치지 않게 컨텍스트로 분기). 카메라 패닝과의 충돌은 보정 활성 시에만 방향키를 가로채 해소(WASD는 그대로라 카메라도 계속 움직임). |
| 2026-05-31 | 프롭 풋프린트를 소수로 — 작은 소품을 미리 작게 | **`PropDefinition.footprint`을 `Vector2Int`→`Vector2`(소수 허용)로 변경.** 프롭 풋프린트는 그리드 점유 검사엔 안 쓰이고 순수 비주얼(스프라이트 fit-scale = `footprint.x×tileSize`, nav 박스 크기)에만 쓰여 안전. `PropQuadBuilder.Build`의 `Mathf.Max(1, ...)` 정수 클램프가 1 미만을 막아 소수의 의미를 없애므로 `Mathf.Max(0.05f, ...)` float 클램프로 교체(0/음수만 차단). 카탈로그 에디터: 프롭용 `Vector2 newPropFootprint`/`batchFootprint`를 건물용 `Vector2Int newFootprint`와 분리하고 프롭 폼·일괄등록 필드를 `Vector2Field`로(건물 폼은 정수 유지). 기존 정수 풋프린트 에셋은 `{x,y}` YAML 표현이 동일해 값 보존(1→1.0). | "작은 건 미리 작게" — 인스턴스 scale(R+스크롤)과 별개로 카탈로그 기본 크기를 0.3~0.6처럼 줄여두고 싶음. 건물은 정수 그리드 점유라 그대로, 프롭만 자유 크기로 분리. |
| 2026-05-31 | 플레이모드 Ctrl+Z가 Unity 에디터 undo까지 같이 먹음 | **맵툴 Undo를 키 단축키(Ctrl+Z)에서 상단 UI [Undo] 버튼으로 이전.** 플레이모드에서 Ctrl+Z는 Unity 에디터가 전역으로 가로채(씬/오브젝트 undo) 코드로 막을 수 없음 → 맵툴 내 단축키를 제거하고 New/Save/Load 옆에 `Undo` 버튼 추가(`_manager.Undo()`). `MapBuilderInput`의 Ctrl+Z 핸들러 삭제, F1 도움말 갱신. | "맵툴 기능이 아닌 것 같으니 차라리 맵툴(버튼)에 넣자"는 사용자 제안. 에디터 전역 단축키 충돌은 회피 불가 → 입력 채널을 버튼으로 분리하는 게 유일하게 깔끔한 해법. |
| 2026-05-31 | 벽으로 직접 쌓은 구조물 내부에 프랍을 못 놓음(벽에 가림) — "건물 표시"엔 안 뜸 | **전체 벽 숨김 토글 추가.** 기존 "건물 표시" 패널의 "내부 보기"는 프리팹 건물(`PlacedBuilding`, occludesInterior 파트)만 대상 → Wall 도구로 깐 개별 벽은 대상이 아니라 가릴 수 없었음. `MapBuilderManager.WallsHidden` 플래그 + `ToggleWallsHidden()` 추가, 패널에 **"벽 숨김" 버튼**(활성 시 하이라이트). 벽 가시성의 단일 출처는 `ApplyLevelCutaway` — 벽 블록을 `show = !WallsHidden && (!on || tile.level<=CurrentLevel)`로 바꿔 층 컷어웨이와 AND. 토글은 `ApplyLevelCutaway` 재실행으로 반영되고 RebuildAllVisuals 끝에서도 재적용돼 리빌드 후 유지. 천장은 "윗층 바닥"이라 기존 V키 층 컷어웨이로 숨김(중복 기능 안 만듦). | 사용자가 프리팹 건물이 아니라 벽+천장 타일로 방을 직접 만들어 내부 프랍을 놓고 싶어 함. 객관식에서 "전체 벽 숨김 토글" 선택(현재층/방향별 대신 단순·명확). 벽 가시성 규칙을 한 곳(ApplyLevelCutaway)에 모아 컷어웨이와 충돌 없이 AND 결합. |
| 2026-05-31 | 맵오브젝트(스폰포인트·지도판 등)가 프리팹에 기능 없이 저장됨 | **베이크의 MapObjects 루프를 런타임 `MapObjectSpawner.SpawnSingle` 재사용으로 통일.** 원인: `MapRuntimeBootstrapper.Start()`가 비어 있어(=베이크 프리팹 사용, 런타임 자동 생성 비활성) 런타임 스폰 경로를 안 타는데, `SaveMapPrefab`의 MapObjects 루프는 **빈 GameObject(+이펙트 프리팹)만** 만들고 SpawnPoint/InteractableObject(MapBoard 등)/LootContainer/Door/NPC/Trigger **기능 컴포넌트를 붙이지 않아** 베이크 프리팹에 기능이 직렬화되지 않았음. **해결**: `MapObjectSpawner.SpawnSingle`을 public으로 올리고, 베이크에서 임시 `MapObjectSpawner`(`__BakeMapObjectSpawner`)를 만들어 각 오브젝트를 `SpawnSingle`로 빌드 → 런타임과 동일한 컴포넌트가 붙은 채 프리팹에 직렬화. 내부 오브젝트(`parentBuildingId`) 라우팅용으로 건물 베이크 시 `instanceId→GameObject` 딕셔너리를 채워 `SetBuildingObjects`로 전달. 이펙트 프리팹(visualMode==3) 비주얼은 스포너가 안 다루므로 베이크에서 추가 부착(기존 로직 유지). 스포너의 런타임 전용 `Destroy`(Door 비주얼 콜라이더)는 `DestroyObj`(에디터=DestroyImmediate) 헬퍼로 교체. 임시 스포너 GO는 `finally`에서 `DestroyImmediate`. 도어 비주얼의 `new Material`은 직전 단계 `PersistGeneratedAssets`가 에셋화. | "스폰포인트·지도판이 저장 안 됨" — 베이크가 비주얼만 굽고 로직 컴포넌트는 누락. 검증된 런타임 스폰 코드를 단일 출처로 재사용해 베이크=런타임 동작 일치(중복 구현·드리프트 방지). JSON은 Resources 밖(Assets/Maps)이라 런타임 로드 불가 → 프리팹에 굽는 게 유일한 경로. (loot용 MapSpawnController 자동생성은 베이크에서 제외 — 추후 과제.) |
| 2026-05-31 | 벽이 프리팹에 저장 안 됨(베이크 후 다시 열면 사라짐) | **베이크 시 코드로 생성한 비-에셋 메시/머티리얼을 디스크 에셋으로 영속화한 뒤 참조 교체.** 원인: 벽 큐브는 `WallBuilder`의 절차적 공유 메시(`_uprightBox = new Mesh{...}`)와 런타임 머티리얼(`new Material(...)`), 건물 폴백 큐브도 런타임 머티리얼을 쓰는데, 이들은 **에셋이 아니라 메모리상 객체**라 `PrefabUtility.SaveAsPrefabAsset`이 직렬화하지 못함 → 프리팹을 다시 열면 메시/머티리얼 참조가 null이 되어 벽이 안 보임(스프라이트 타일/프롭은 에셋 스프라이트라 멀쩡). **해결**: `SaveMapPrefab`에서 `SaveAsPrefabAsset` **직전에** `PersistGeneratedAssets(root, prefabDir, filename)` 호출 — 계층의 모든 `MeshFilter.sharedMesh`/`Renderer.sharedMaterials`를 훑어 `AssetDatabase.Contains(x)==false`인 것만 `{prefabDir}/Map_{filename}_Assets/` 폴더에 `.asset`/`.mat`으로 저장하고 참조를 그 에셋으로 교체. 동일 인스턴스는 레퍼런스로 디듀프(공유 벽 메시는 1개만 저장). **메시는 원본을 소비하지 않게 `Object.Instantiate` 복사본을 저장**(런타임 싱글톤 `_uprightBox` 보존). 폴더는 매 베이크 시 `DeleteAsset`으로 비우고 재생성. | 절차적 메시/런타임 머티리얼은 디스크 에셋이 아니면 프리팹에 직렬화 안 됨(에셋 스프라이트와 차이). 벽 전용이 아니라 건물 폴백 등 모든 비-에셋 렌더 리소스를 범용 헬퍼로 한 번에 처리. 복사본 저장으로 다음 베이크의 공유 싱글톤이 첫 프리팹에 묶이는 사고 방지. |
| 2026-05-30 | F키 플립이 안 먹힘(그림 좌우 반전 안 됨) | **`sr.flipX` 대신 트랜스폼 X스케일 부호로 미러링.** 원인: props.mat의 커스텀 `InkCity/Prop` 셰이더(Prop.shader)가 `TransformObjectToHClip(positionOS)`만 하고 빌트인 sprite의 `_Flip` 벡터를 안 봐서 `SpriteRenderer.flipX`가 무시됨. `PropQuadBuilder.ApplyFlip`을 재작성: `sr.flipX=false`로 끄고 Visual 트랜스폼의 `localScale.x` 부호를 뒤집어 **지오메트리 레벨**에서 반전(셰이더 무관). `Mathf.Abs`로 멱등(여러 번 호출해도 안정). 맵툴 F키→`ToggleFlipX`→고스트/이동중 프롭/배치 프롭(`prop.flipX`)에 적용. | 커스텀 셰이더라 sr.flipX가 화면에 반영 안 됨. 트랜스폼 스케일 반전은 어떤 셰이더에도 통하고 접지 오프셋(localPosition.y)과 독립적이라 안전. |
