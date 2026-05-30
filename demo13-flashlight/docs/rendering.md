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
