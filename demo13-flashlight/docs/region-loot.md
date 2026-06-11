# 지역별 루트 · 전용 아이템

> **현 상태 = 진실.** **5지역**(`WorldRegionCatalog`, 7→5 병합 2026-06-11)마다 드롭 테이블·지역 전용 SO 분리. 병합: railway 루트→industrial(묻힌 정비창), sanctuary 루트→entertainment(기억의 극장). 전용 SO(RailwayScrap/SanctuaryMemorial 폴더)는 itemId로 그대로 사용.

## 개요

| 항목 | 경로 |
|------|------|
| 지역 정의 | `Assets/Scripts/Data/WorldRegionCatalog.cs` |
| 드롭 CSV (원본) | `tools/region_loot.csv` |
| 런타임 로드 | `Assets/Resources/region_loot.txt` |
| 롤 로직 | `Assets/Scripts/Data/RegionLootCatalog.cs` |
| 스폰 연동 | `Assets/Scripts/Inventory/ItemSpawnPoint.cs` (`useRegionLoot`) |
| 지역 전용 아이템 CSV | `tools/region_items.csv` |
| SO 출력 | `Assets/Resources/Items/Regional/{지구폴더}/` |
| 데모 바닥 스폰 | `Assets/Scripts/Data/RegionLootBootstrap.cs` (선택 부착) |

`ItemData.primaryRegionId`가 비어 있으면 **공용** 아이템. `reg_*` 접두 + `Regional/` 폴더 = 해당 지구 **시그니처** 루트.

---

## 1. 지역 전용 아이템 (14종)

| regionId | itemId | 표시명 | 역할 |
|----------|--------|--------|------|
| silence_living | `reg_silence_market_chit` | 침묵 시장 식표 | 생존자 시장 |
| silence_living | `reg_silence_note_bundle` | 뭉친 쪽지 묶음 | 정보·단서 |
| scrap_market | `reg_scrap_neon_tube` | 네온관 잔해 | 폐상가 간판 |
| scrap_market | `reg_scrap_coupon_stack` | 상가 할인권 묶음 | 교역 쿠폰 |
| industrial | `reg_industrial_pressure_gauge` | 압력계 다이얼 | 설비 부품 |
| industrial | `reg_industrial_coolant_can` | 냉각수 캔 | 발전소 |
| entertainment | `reg_entertain_show_ticket` | 공연 입장권 조각 | 폐극장 |
| entertainment | `reg_entertain_luna_sticker` | LUNA 스티커 | 네온 거리 |
| railway_scrap | `reg_railway_rail_clip` | 레일 클립 | 철도 |
| railway_scrap | `reg_railway_grease_tin` | 기차 윤활유 | 야드 |
| sanctuary_memorial | `reg_sanctuary_prayer_bead` | 기도 구슬 | 추모 |
| sanctuary_memorial | `reg_sanctuary_wilted_flower` | 시든 헌화 | 대성당 |
| eternal_night_core | `reg_core_observation_log` | 관측 일지 페이지 | 스토리 (판매 0) |
| eternal_night_core | `reg_core_void_residue` | 공허 잔류물 | 영야 코어 |

추가: `powershell tools/GenerateRegionItems.ps1`

---

## 2. 드롭 티어

| 티어 | 용도 | 기본 roll |
|------|------|-----------|
| `container_day` | 낮 루팅 상자 | 4 |
| `container_night` | 밤 상자 (밴딧·위험) | 4 |
| `ground_day` | 낮 바닥 | 2 |
| `ground_night` | 밤 바닥 | 2 |

`RegionTimeManager`의 해당 지역 `isNight`에 따라 day↔night 자동 전환.

---

## 3. 지역별 시그니처 + 대표 드롭

### 침묵 생활 (`silence_living`)
- **시그니처**: 시장 식표, 쪽지 묶음
- **낮 상자**: `note_scrap`, `coupon_faded`, `coin_old`, 의료 소량
- **밤 상자**: `code_paper`, `coin_military`, `morphine_ampule`, `tourniquet`

### 폐상가 교역 (`scrap_market`) — 1차 데모
- **시그니처**: 네온관, 할인권 묶음
- **낮**: `coin_scrap`, `battery`, `canned_food`, `val_lottery_ticket`, **조리** `ingredient_salt/flour/sugar`, 희귀 `recipe_bread`
- **밤**: `val_chip_credit`, `smoke_bomb`, `stim_injector`, `hemostatic_powder`, 조리 재료 소량

### 침묵 생활 · 공연 (`silence_living`, `entertainment`) — 주방 루트
- **재료**: `water_bottle`, `ingredient_meat`, `ingredient_vegetable`, `ingredient_salt`, `ingredient_herb`(공업)
- **레시피**: `recipe_stew`, `recipe_soup`(침묵), `recipe_tea`(공업), `recipe_special`(공연 밤 희귀)

### 공업 설비 (`industrial`)
- **시그니처**: 압력계, 냉각수
- **낮·밤**: `wire`, `scrap_metal`, `screw`, `circuit_board`, `ruby_dust`

### 공연 오락 (`entertainment`)
- **시그니처**: 입장권, LUNA 스티커
- **밤**: `whisper_tape`, `junk_mannequin_arm`, `val_cigar_box`

### 철도 폐기 (`railway_scrap`)
- **시그니처**: 레일 클립, 윤활유
- **공통**: `scrap_metal` 가중치 최대, `junk_bike_wheel`, `hammer`

### 성역 추모 (`sanctuary_memorial`)
- **시그니처**: 기도 구슬, 시든 헌화
- **밤**: `photo_brother`, `val_artifact_shard`, `black_liquid`

### 중앙 영야 (`eternal_night_core`)
- **시그니처**: 관측 일지, 공허 잔류물
- **밤**: `ruby_core`, `ruby_corrupted`, `anomaly_blade`, `val_id_government`

전체 가중치·수량: `tools/region_loot.csv` 참고.

---

## 4. 씬 연동

1. **ItemSpawnPoint** 배치 → `Spawn Table` 비움, `Use Region Loot` 체크
2. `Container` 타입이면 `LootContainer` 연결
3. 출전 지역은 `MapSelectUI` → `RegionTimeManager.ActiveRegionId`
4. 테스트: `H` → 아이템 탭 → 「지역 상자/바닥 루트」

## 5. 맵 스폰 컨트롤러 (MapSpawnController)

맵 단위로 아이템 스폰 총량·희귀도·카테고리를 통제하는 시스템.

### 구조

| 컴포넌트 | 역할 |
|----------|------|
| `MapSpawnProfile` (SO) | 맵별 스폰 설정 (예산, 희귀도 분포, 카테고리 쿼터, 난이도 배율) |
| `MapSpawnController` (씬) | 런타임에 프로파일 기반으로 아이템 생성·분배 |
| `MapSpawnProfileEditor` | 인스펙터에서 분포 시각화 + 시뮬레이션 |

### 흐름
1. 맵 진입 → `MapSpawnController.Start()`
2. 씬의 모든 `ItemSpawnPoint` 수집, `managedByController = true` 설정
3. Fixed 포인트는 자체 스폰 (열쇠 등)
4. 프로파일에서 예산 산정 (낮/밤 보정 적용)
5. 카테고리 가중치 → 희귀도 가중치 → ItemDatabase에서 후보 선택
6. Ground 포인트에 균등 분배 (WorldItem.Drop)
7. Container 포인트에 분배 (LootContainer.Grid.TryAutoPlace)
8. 통계 로그 출력

### 프로파일 설정

| 설정 | 설명 |
|------|------|
| 총량 예산 | Ground/Container 각각 min~max 범위 |
| spawnMultiplier | 전체 스폰량 배율 (0.1~3) |
| qualityMultiplier | 고급 아이템 확률 배율 (1 기본) |
| 희귀도 가중치 | Common~Legendary 각각 설정 |
| 카테고리 가중치 | Weapon~Misc 각각 설정 |
| nightSpawnMultiplier | 밤 스폰량 배율 (기본 1.2) |
| nightQualityMultiplier | 밤 품질 배율 (기본 1.5) |

### 기존 시스템과의 관계
- **MapSpawnController 있음**: 프로파일 기반 스폰. ItemSpawnPoint는 위치 제공만.
- **MapSpawnController 없음**: 기존 방식 유지 (각 ItemSpawnPoint가 자체 SpawnTable/RegionLoot 사용).
- **SpawnTable SO**: 프로파일과 별개로 특수 상자에 직접 지정 가능.

### 기획 결정
- 날짜: 2026-05-26
- 질문: 맵 스폰 통제 범위?
- 결정: 총량 예산 + 희귀도 곡선 + 카테고리 쿼터 + 난이도 배율 전부 통제. 배치는 혼합 (핵심 고정 + 나머지 랜덤).
- 근거: 맵 밸런싱을 중앙에서 조절 가능. 지역별 프로파일 SO로 관리.

## 6. 맵 빌더 연동

맵 빌더 도구에서 스폰 포인트를 배치·설정하면 런타임에 자동으로 시스템이 구성된다.

### 배치 시 설정 (MapBuilderUI 좌하단 스폰 패널)

| 오브젝트 | 설정 항목 |
|----------|-----------|
| **루팅 상자** (LootContainer) | 상자 이름, 격자 가로×세로, 지역 루트 사용 여부, 고정 아이템 ID/수량 |
| **바닥 아이템** (ItemDrop) | 지역 루트 사용 여부, 고정 아이템 ID/수량 |
| **적 스폰** (EnemySpawn) | 유닛 키(StatDB), 수량 |

### 런타임 변환 (MapObjectSpawner)

| 맵 빌더 오브젝트 | 생성되는 컴포넌트 |
|------------------|-------------------|
| LootContainer | `InteractableObject(Container)` + `LootContainer` + `ItemSpawnPoint(Container)` |
| ItemDrop | `InteractableObject(Pickup)` + `ItemSpawnPoint(Ground)` 또는 `ItemSpawnPoint(Fixed)` |
| EnemySpawn | 마커 (EnemySpawnManager가 읽어서 처리) |

- **MapSpawnController 자동 생성**: 맵에 ItemSpawnPoint가 하나라도 있으면 자동으로 `MapSpawnController` 생성. 현재 활성 지역(`RegionTimeManager.ActiveRegionId`)에 맞는 프로파일을 `Resources/Data/MapSpawn/{regionId}.asset`에서 자동 로드.
- **PlacedMapObject 직렬화**: 스폰 설정(격자 크기, 지역루트 여부, 고정아이템 등)은 맵 JSON에 함께 저장/로드.

---

## 변경 로그

| 날짜 | 내용 |
|------|------|
| 2026-05-25 | 7지구 지역 전용 아이템 14종·`region_loot.csv` 드롭 테이블·`RegionLootCatalog`·ItemSpawnPoint 연동. |
| 2026-05-26 | **맵 스폰 컨트롤러 구현.** MapSpawnProfile(SO) + MapSpawnController(씬). 총량 예산·희귀도 분포·카테고리 쿼터·낮밤 보정. 7개 지역 프리셋 자동 생성(Tools > Dev Tools > Data > Generate Map Spawn Profiles). ItemSpawnPoint에 managedByController 가드 추가. |
| 2026-05-28 | **맵 빌더 스폰 연동.** PlacedMapObject에 스폰 필드 추가. MapObjectSpawner에서 LootContainer/ItemDrop/EnemySpawn → ItemSpawnPoint+LootContainer 자동 부착. MapSpawnController 자동 생성. MapBuilderUI에 스폰 설정 패널 추가. MapSerializer 직렬화 대응. |
