# 지역별 루트 · 전용 아이템

> **현 상태 = 진실.** **5지역**(`WorldRegionCatalog`, 7→5 병합 2026-06-11)마다 드롭 테이블·지역 전용 SO 분리. 병합: railway 루트→industrial(묻힌 정비창), sanctuary 루트→entertainment(기억의 극장). 전용 SO(RailwayScrap/SanctuaryMemorial 폴더)는 itemId로 그대로 사용.

## 루팅 정리 결정 — 2026-09-11 (시스템 정리 3단계)

조사 결과(요약): 레이드 상자·바닥 루팅은 `MapSpawnController`가 **아이템 DB 전체에서 카테고리·희귀도로 무작위**로 뽑고 있었고 이 문서의 드롭 표(`region_loot`)는 **시체에만** 쓰였다. `SpawnTable`·`RegionLootBootstrap`·상자 `initialLoot`은 비어 있거나 안 쓰였고, 폴백은 켜지면 루팅 0개. Zone1 상자 117개 중 46개와 실내 상자 전부는 열리지 않는 모양뿐인 박스. 상자를 섞지 않아 매 판 앞쪽 상자만 찼다. 캐릭터 패널로 꺼낸 아이템은 레이드 결과·퀘스트에 안 잡혔다.

| 날짜 | 질문(선택지) | 사용자 결정 |
|---|---|---|
| 2026-09-11 | 상자·바닥 루팅은 누가 정하나 — 맵 예산(몇 개·어디) + 지역 표(무엇·몇 개씩) / 지금 방식(DB 무작위) | **예산 + 지역 표** |
| 2026-09-11 | 열리지 않는 가짜 상자(Zone1 46 + 실내 전부) — 진짜 상자로 / 장식으로 두고 스폰만 빼기 / 없앤다 | **진짜 상자로** |
| 2026-09-11 | 루팅 화면 — 빠른 목록 하나로 / 상자는 인벤 화면 유지 | "상자를 연 뒤 연출은 **원래 있던 그 느낌** 살려서, 모든 아이템이 다 보이면 **다 가져갈지 하나씩 가져갈지** 나오게. **가치 표기는 넣지 마**" → 확인 질문: 원래 느낌 = **옛 수색 연출 되살리기**(열면 '?'로 가려져 있다가 하나씩 드러남, 희귀할수록 조금 더 걸림 — 2026-09-09 볼륨 축소 때 뺀 것) |
| 2026-09-11 | (사용자 추가 지시) | "존에서의 루팅 아이템은 **랜덤이 아니라 정해져 있도록**, 가중치 넣어서. **고철은 특정 오브젝트나 시체에서만** 나오게" |
| 2026-09-11 | 루팅 표 단위 — 지역 × 상자 종류 / 맵 안 구역별 / 지역 단위 하나 | **지역 × 상자 종류** — 지역마다 상자 종류별(길가 쓰레기·차량 트렁크·노점·나무상자·바닥 …) 가중치 표. 실내 건물은 건물 종류(약국·경찰서 …)로 따로 |
| 2026-09-11 | 고철(◈)이 나오는 오브젝트(시체는 기본) — 계산대·금고 신설 / 차량 트렁크 / 노점 좌판 / 시체만 (복수) | **계산대·금고 신설 + 노점 좌판** (+ 시체). 그 밖의 상자·바닥에선 고철이 안 나온다 |

## 개요

| 항목 | 경로 |
|------|------|
| 지역 정의 | `Assets/Scripts/Data/WorldRegionCatalog.cs` |
| 드롭 CSV (원본) | `tools/region_loot.csv` |
| 런타임 로드 — 일반 표 (지역 × 시간대) | `Assets/Resources/region_loot.txt` (Control Panel「지역 루트」탭) |
| 런타임 로드 — 상자 종류 표 (지역 × 종류) | `Assets/Resources/loot_tables.txt` (2026-09-11 신설, Control Panel「CSV 밸런스」탭) — §2.2 |
| 롤 로직 | `Assets/Scripts/Data/RegionLootCatalog.cs` (`TryFindPool` · `RollOnce` · `RollSource`) |
| 예산 (몇 개·어디에) | `Assets/Scripts/Inventory/MapSpawnController.cs` + 예산 SO `MapSpawnProfile` — §5 |
| 스폰 앵커 | `Assets/Scripts/Inventory/ItemSpawnPoint.cs` — 위치 표시만. Fixed만 스스로 고정 아이템을 놓는다 |
| 상자 | `Assets/Scripts/Inventory/LootContainer.cs` (`lootKind` = §2.2 종류) |
| 루팅 화면 · 기록 | `Assets/Scripts/UI/LootListUI.cs` · `Assets/Scripts/Inventory/LootTake.cs` |
| 지역 전용 아이템 CSV | `tools/region_items.csv` |
| SO 출력 | `Assets/Resources/Items/Regional/{지구폴더}/` |

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

## 2. 루팅 표 (무엇이·몇 개씩)

> 2026-09-11부터 레이드 상자·바닥·시체에 **무엇이 몇 개씩 나오는지는 이 표만 정한다**(결정: §루팅 정리 결정). 몇 개·어디에(예산)는 §5.

두 파일, 같은 컬럼 `region,tier,roll_count,item_id,weight,min,max`(`#` 줄은 주석). `RegionLootCatalog`가 둘 다 읽는다. 뽑기 1번 = 가중치로 1행 → `min~max`개. `roll_count` = 표를 한 번 쓸 때 뽑는 횟수.

### 2.1 일반 표 — `region_loot.txt` (지역 × 시간대)

| 티어 | 용도 | 기본 roll |
|------|------|-----------|
| `container_day` | 낮 상자 | 4 |
| `container_night` | 밤 상자 (밴딧·위험) | 4 |
| `ground_day` | 낮 바닥 | 2 |
| `ground_night` | 밤 바닥 | 2 |
| `container_anomaly` | 짙은현상 상자 (`silence_living`만) | 3 |
| `ground_anomaly` | 짙은현상 바닥 (`silence_living`만) | 2 |

5지역 모두 day/night 4티어, anomaly 2티어는 `silence_living`만. `RegionTimeManager`의 해당 지역 `isNight`에 따라 day↔night 자동 전환.

### 2.2 상자 종류 표 — `loot_tables.txt` (지역 × 상자 종류, 2026-09-11 신설)

tier 칸 = 상자 종류(`LootContainer.lootKind`). **지금은 `scrap_market`만 있고 수치는 가안**(플레이하며 조정). `{종류}_night` 표가 있으면 밤에 그걸 먼저 쓴다(현재 없음).

| 종류 | 무엇 / 어디 | roll | 고철(◈) |
|------|-------------|:---:|:---:|
| `junk` | 길가 잡동사니 | 1 | |
| `trunk` | 자동차 트렁크 | 2 | |
| `stall` | 노점 좌판 — 예산과 무관하게 늘 채움 | 2 | ◈ |
| `crate` | 나무상자 (종류를 안 정한 상자의 기본값) | 2 | |
| `register` | 계산대 — 예산과 무관하게 늘 채움 | 2 | ◈ |
| `safe` | 금고 — 예산과 무관하게 늘 채움 | 2 | ◈ |
| `ground` | 바닥 | 1 | |
| `corpse` | 시체 (+ 시체 가방 속) | 2 | 유닛별 `cashMin/cashMax`로 따로 |
| `int_shop` | 실내: 폐가게·붕괴 쇼핑몰·세탁소·골목 상점·공용 | 2 | |
| `int_food` | 실내: 식당 | 2 | |
| `int_medical` | 실내: 약국 | 2 | |
| `int_tools` | 실내: 차고·창고·철물점 | 2 | |
| `int_electronics` | 실내: 컴퓨터가게 | 2 | |
| `int_police` | 실내: 경찰서 | 2 | |
| `int_jewelry` | 실내: 보석상 | 2 | |
| `int_basement` | 실내: 짙은현상 지하·돔 | 2 | |

### 2.3 표 찾는 순서 — `RegionLootCatalog.TryFindPool`

1. **종류 표**: (밤이면 `{종류}_night` →) `{종류}` — 이 지역, 없으면 `scrap_market`.
2. 종류 표가 어디에도 없을 때만 **일반 표**: 바닥은 `ground_day/night`, 그 밖엔 `container_day/night` — 이 지역 → `scrap_market`.

- 종류 표가 일반 표보다 먼저라서 **다른 지역 건물의 계산대·금고에서도 고철이 나온다**.
- 그 결과 **지금은 모든 종류에 `scrap_market` 표가 있으므로 다른 지역도 상자·바닥·시체에 `scrap_market` 종류 표를 쓰고**, 지역별 일반 표(§2.1·§3)는 종류 표가 없는 종류에서만 쓰인다. 지역별 종류 표를 추가하면 그 지역 표가 먼저 쓰인다.
- 전역 노브: `GameTuning.lootChanceMult`(뽑기마다 꽝 게이트) · `valuableWeightMult`(귀중품 가중치) — `RegionLootCatalog.RollOnce`.

### 2.4 고철(◈) 규칙

- 고철 화폐 `scrap_money`는 **`stall` · `register` · `safe` 표에만** 있다. 시체는 유닛별 `cashMin/cashMax`(`EnemyController.AddScrap`)로 따로 받는다. 그 밖의 상자·바닥에서는 나오지 않는다.
- 2026-09-11 `region_loot` 일반 표의 `scrap_market` `scrap_money` 3행(낮 상자·밤 상자·바닥)을 삭제했다(txt·csv 둘 다).

---

## 3. 지역별 시그니처 + 대표 드롭

> 아래 "낮/밤 상자" 드롭은 §2.1 **일반 표** 기준이다. 지금 레이드 상자·바닥·시체는 §2.3 순서에 따라 **종류 표를 먼저** 쓴다(현재 전 지역 → `scrap_market` 종류 표).

### 침묵 생활 (`silence_living`)
- **시그니처**: 시장 식표, 쪽지 묶음
- **낮 상자**: `note_scrap`, `coupon_faded`, `coin_old`, 의료 소량, **가정용 귀중품**(`val_silver_spoon`·`val_camera_film`·`val_watch_wrist`·`val_bracelet`, 2026-07-02 EV 보정)
- **밤 상자**: `code_paper`, `coin_military`, `morphine_ampule`, `tourniquet`, **귀금속**(`val_ring_silver`·`val_earring_pair`·`val_jade_pendant`, 2026-07-02 EV 보정)

### 폐상가 교역 (`scrap_market`) — 1차 데모
- **시그니처**: 네온관, 할인권 묶음
- **낮**: `battery_aa`, `canned_food`, `val_lottery_ticket`, **조리** `ingredient_salt/flour/sugar`, 희귀 `recipe_bread` — 고철 화폐 `scrap_money`는 2026-09-11 일반 표에서 빠졌다(§2.4, 지금은 좌판·계산대·금고·시체에서만)
- **밤**: `val_chip_credit`, `smoke_bomb`, `stim_injector`, `hemostatic_powder`, 조리 재료 소량

### 침묵 생활 · 공연 (`silence_living`, `entertainment`) — 주방 루트
- **재료**: `water_bottle`, `ingredient_meat`, `ingredient_vegetable`, `ingredient_salt`, `ingredient_herb`(공업)
- **레시피**: `recipe_stew`, `recipe_soup`(침묵), `recipe_tea`(공업), `recipe_special`(공연 밤 희귀)

### 공업 설비 (`industrial`)
- **시그니처**: 압력계, 냉각수
- **낮·밤**: `wire`, `scrap_metal`, `screw`, `circuit_board`, `ruby_dust`

### 공연 오락 (`entertainment`)
- **시그니처**: 입장권, LUNA 스티커
- **낮**: **중가 귀중품 라인**(`val_chain_gold`·`val_gem_loose`·`val_watch_pocket`·`val_cigar_box`·`val_dental_gold`, 2026-07-02 신설) — 유흥가 컨셉의 전당포행 장물
- **밤**: `whisper_tape`, `junk_mannequin_arm`, `val_cigar_box`, **`val_ring_wedding`(w1) 잭팟** — 2026-07-02 낮→밤 이동

### 철도 폐기 (`railway_scrap`)
- **시그니처**: 레일 클립, 윤활유
- **공통**: `scrap_metal` 가중치 최대, `junk_bike_wheel`, `hammer`

### 성역 추모 (`sanctuary_memorial`)
- **시그니처**: 기도 구슬, 시든 헌화
- **밤**: `photo_brother`, `val_artifact_shard`, `black_liquid`

### 중앙 영야 (`eternal_night_core`)
- **시그니처**: 관측 일지, 공허 잔류물
- **밤**: `ruby_core`, `ruby_corrupted`, `anomaly_blade`, `val_id_government`

전체 가중치·수량: `tools/region_loot.csv`(일반 표) · `Assets/Resources/loot_tables.txt`(상자 종류 표) 참고.

---

## 4. 씬 연동 (2026-09-11)

1. **상자** = `LootContainer`(`lootKind` = §2.2 종류) + `InteractableObject(Container)` + `ItemSpawnPoint(Container)` 앵커. 빌더는 `InteriorBuild.MakeSearchable`로 한 번에 붙인다(§6).
2. **바닥** = `ItemSpawnPoint(Ground)` 앵커. **고정 아이템**(열쇠 등) = `ItemSpawnPoint(Fixed)` — 이것만 스스로 놓는다.
3. 씬에 **`MapSpawnController`**(프로파일 참조)가 있어야 Ground/Container 앵커가 채워진다. 없으면 앵커는 경고만 남기고 비어 있다.
4. 루팅 지역 = `MapSpawnController`의 `regionIdOverride`, 비우면 `RegionTimeManager.ActiveRegionId`(`MapSelectUI`에서 정함). 정해진 지역은 `MapSpawnController.CurrentRegionId`로 공개돼 시체 루팅도 같은 지역 표를 쓴다.

## 5. 맵 스폰 컨트롤러 (MapSpawnController) — 예산: 몇 개·어디에

> 2026-09-11부터 **몇 개·어디에만** 정한다. 무엇이·몇 개씩은 §2 루팅 표만 정한다. 예전처럼 아이템 DB 전체에서 카테고리·희귀도로 무작위로 뽑는 방식은 폐기됐다.

### 구조

| 컴포넌트 | 역할 |
|----------|------|
| `MapSpawnProfile` (SO) | 예산 — 바닥/상자 `min~max`, `spawnMultiplier`, `nightSpawnMultiplier`(기본 1.2). 희귀도·카테고리 가중치·`qualityMultiplier`·`nightQualityMultiplier` 필드는 남아 있지만 **안 쓴다** |
| `MapSpawnController` (씬) | 씬 로드 시 1회: 앵커 수집 → 예산 → 상자·바닥 채우기. 씬별 `isInterior` · `budgetMult`(유니크 건물 가산) · `regionIdOverride` |
| `ItemSpawnPoint` | 앵커(§4) |

에셋: `Assets/Resources/Data/MapSpawn/`에 5개(5지역). 씬(Zone1 + 실내 15개)이 참조하는 건 **`scrap_market.asset`뿐**이다.

### 예산 (단위 = 뽑기 횟수)

바닥·상자 각각 = 프로파일 `min~max` 랜덤 × `spawnMultiplier` × (밤이면 `nightSpawnMultiplier`) × `GameTuning.itemSpawnCountMult` × `GameTuning.lootCountMult` × (실내면 `GameTuning.interiorLootBudgetMult`) × 씬 `budgetMult`.
프로파일이 없으면 기본 예산(바닥 20 · 상자 30)으로 채우고 경고를 남긴다. 예전 폴백은 루팅이 0개였다.

### 흐름
1. `Awake` — 씬의 `ItemSpawnPoint`를 수집해 Ground / Container / Fixed로 나누고 `managedByController = true`.
2. Fixed 앵커는 스스로 고정 아이템을 한 번 놓는다.
3. 예산 산정(위).
4. **상자**: 매 판 섞는다. 고철 오브젝트 — 계산대·금고·좌판(`register`/`safe`/`stall`) — 는 **예산과 무관하게 늘** 채운다. 나머지는 섞은 순서로 예산이 닿는 데까지 채운다. 상자마다 자기 종류 표(§2.3)를 `roll_count`번 뽑고, 1번 = 예산 1. 예산 밖 상자는 비어 있다(섞으므로 매 판 다른 상자).
5. **바닥**: 앵커를 섞어 지점마다 최대 2개씩 `ground` 표에서 뽑는다. 예전엔 남은 걸 마지막 지점에 전부 몰았다.
6. 통계 로그.

### 기획 결정
- 날짜: 2026-05-26
- 질문: 맵 스폰 통제 범위?
- 결정: 총량 예산 + 희귀도 곡선 + 카테고리 쿼터 + 난이도 배율 전부 통제. 배치는 혼합 (핵심 고정 + 나머지 랜덤).
- 근거: 맵 밸런싱을 중앙에서 조절 가능. 지역별 프로파일 SO로 관리.
- **→ 2026-09-11 대체**: 희귀도 곡선·카테고리 쿼터는 폐기. 총량 예산·난이도 배율만 남고 내용물은 루팅 표가 정한다(§루팅 정리 결정).

## 6. 씬 빌더 연동 (2026-09-11)

> 옛 맵 빌더(`MapBuilderUI` · `MapObjectSpawner` · `PlacedMapObject` · `MapSerializer`)와 "컨트롤러 자동 생성·지역별 프로파일 자동 로드"는 **현존하지 않는다**. 지금 레이드 씬은 에디터 빌더(`Zone1GreyboxLayout` · `Zone1Interiors`)가 만들고, 각 씬에 `MapSpawnController`(프로파일 `scrap_market.asset`)가 배치돼 있다.

- **`InteriorBuild.MakeSearchable(go, kind, …)`** — 오브젝트를 열 수 있는 상자로 만든다: `LootContainer`(lootKind) + `InteractableObject(Container)` + `ItemSpawnPoint(Container)` 앵커. 모양뿐이던 3D 상자(Zone1 `Scatter`/`CrateAnchor` 46개, 실내 `InteriorBuild.Crate` 전부)도 이걸로 진짜 상자가 됐다.
- **Zone1**: 길가 잡동사니 = `junk`, 자동차 트렁크 = `trunk`, 좌판 골목 좌판 = `stall`, 나무상자 = `crate`.
- **실내**: 건물마다 `InteriorBuild.CrateKind`로 상자 종류를 정한다.

| 건물 | 종류 |
|------|------|
| 약국 | `int_medical` |
| 폐가게 · 붕괴 쇼핑몰 · 세탁소 · 골목 상점 · 공용 | `int_shop` |
| 차고 · 창고 · 철물점 | `int_tools` |
| 짙은현상 지하 · 돔 | `int_basement` |
| 식당 | `int_food` |
| 컴퓨터가게 | `int_electronics` |
| 경찰서 | `int_police` |
| 보석상 | `int_jewelry` |

- **계산대(`register`)**: 약국 카운터, 밀도형 상점(철물점·식당·컴퓨터가게·세탁소·골목 상점·공용)의 첫 상자, 보석상 카운터.
- **금고(`safe`)**: 보석상 금고실 ×3, 경찰서 압수품 보관함, 돔 최고 보상.

---

## 7. 지역 진행 EV 곡선 (2026-07-02 확립)

> **원칙**: ①낮/밤 각각 **지역 진행 순서(1→5)대로 컨테이너 EV 단조 증가**. ②같은 지역에서 **밤 EV > 낮 EV**(위험 프리미엄). ③**잭팟(초고가 단품)은 밤에만** — 낮은 안정 수익, 밤은 분산 큰 고수익. ④R2 `anomaly` 티어는 루디 소득 축(2026-06-16) — EV 곡선과 별도, 불변.
>
> **EV 산식**: EV/컨테이너 = roll_count × Σ(weight×(min+max)/2×sellPrice) / Σweight. 직판 기준가 = 전당포 sellRate **0.6**(EV는 sellPrice 기준 — 실수령은 ×0.6).

### 컨테이너 EV (스크랩, 2026-07-02 검증치)

| 순서 | 지역 | 낮 before | 낮 after | 밤 before | 밤 after |
|:---:|---|---:|---:|---:|---:|
| 1 | scrap_market | 3,611 | 3,611 (불변) | 5,835 | 5,835 (불변) |
| 2 | silence_living | 2,546 | **4,592** | 3,668 | **7,064** |
| 3 | industrial | 7,958 | 7,958 (불변) | 13,511 | 13,511 (불변) |
| 4 | entertainment | 53,589 | **13,721** | 12,166 | **44,200** |
| 5 | eternal_night_core | 462,759 | 462,759 (불변) | 1,130,906 | 1,130,906 (불변) |

- R2 anomaly(불변): container_anomaly 93,364 / ground_anomaly 33,382.
- R4 밤 44,200은 목표 밴드(30,000~40,000) 상단 초과지만 반지 w1 최소치 결과 — 낮(13,721) 대비 확실히 높음 조건 충족으로 수용. 실측 후 과하면 반지 이외 밤 풀 하향으로 조정.
- ⚠️ **2026-09-11 이후 재계산 필요 (미계산)**: 이 표는 §2.1 **일반 표** 기준이다. ① `scrap_market` 일반 표에서 `scrap_money` 3행이 빠져 R1 값(3,611 / 5,835)은 옛 값이다. ② 지금 레이드 상자는 §2.3 순서에 따라 **상자 종류 표**(현재 전 지역 → `scrap_market` 종류 표, 가안)를 쓰므로, 실제 컨테이너 EV는 종류 표 기준으로 다시 봐야 한다. 수치 확정은 기획 판단 후.
- ~~데이터 이슈~~ (2026-07-02 해결): R1 낮 `battery` 행은 ItemData에 없는 id였음 — `battery_aa`로 교정(오타 버그, EV 영향 미미 +~100/컨테이너). CSV·txt 동기 수정.

## 변경 로그

| 날짜 | 내용 |
|------|------|
| 2026-09-12 | `loot_tables.txt` `int_police`(경찰서 실내)에 `key_police_armory`(경찰서 무기고 열쇠) 가중치 2 추가(가안) — `Int_Police` 무기고 문 열쇠. 결정은 [items.md 변경 로그](items.md). |
| 2026-05-25 | 7지구 지역 전용 아이템 14종·`region_loot.csv` 드롭 테이블·`RegionLootCatalog`·ItemSpawnPoint 연동. (현재: 5지역 — 2026-06-11 병합. ItemSpawnPoint 연동은 2026-09-11 앵커로 대체) |
| 2026-05-26 | **맵 스폰 컨트롤러 구현.** MapSpawnProfile(SO) + MapSpawnController(씬). 총량 예산·희귀도 분포·카테고리 쿼터·낮밤 보정. 7개 지역 프리셋 자동 생성(Tools > Dev Tools > Data > Generate Map Spawn Profiles). ItemSpawnPoint에 managedByController 가드 추가. (현재: 프로파일 에셋 5개(5지역), 씬이 쓰는 건 `scrap_market.asset`뿐. 생성 메뉴는 현존하지 않음. 희귀도·카테고리는 2026-09-11 폐기) |
| 2026-05-28 | **맵 빌더 스폰 연동.** PlacedMapObject에 스폰 필드 추가. MapObjectSpawner에서 LootContainer/ItemDrop/EnemySpawn → ItemSpawnPoint+LootContainer 자동 부착. MapSpawnController 자동 생성. MapBuilderUI에 스폰 설정 패널 추가. MapSerializer 직렬화 대응. (현재: 이 맵 빌더 계열은 현존하지 않음 — §6) |
| 2026-07-02 | **지역 진행 EV 곡선 확립(§7).** 질문: R2 낮 EV(2,546)가 R1(3,611)보다 낮은 역전 + R4 낮 EV의 89%가 `val_ring_wedding`(900,000, w2, 컨테이너당 ~5%) 복권이라 낮(53,589) > 밤(12,166) 역전. 결정: ①반지 낮 삭제 → **밤 w1 이동**(잭팟은 밤에만) ②R4 낮에 중가 귀중품 5행 신설(chain_gold w8 2-4 / gem_loose w8 2-3 / watch_pocket·cigar_box 각 w6 1-3 / dental_gold w8 2-4) → 낮 13,721 ③R2 낮 가정용 귀중품 4행(silver_spoon w7 2-4 / camera_film w6 2-4 / watch_wrist w5 1-3 / bracelet w5 1-2) → 4,592, 밤 귀금속 3행(ring_silver·earring_pair 각 w6 2-4 / jade_pendant w5 1-2) → 7,064. R1/R3/R5·anomaly 불변. `tools/region_loot.csv` + `Resources/region_loot.txt` 동기 수정(diff 확인). 근거·검증 EV 표 = §7. |
| 2026-09-11 | **루팅 정리 — 예산 + 지역 표 (결정 = §루팅 정리 결정).** `MapSpawnController`는 몇 개·어디에(예산 = 뽑기 횟수)만, 무엇이·몇 개씩은 루팅 표만(`RegionLootCatalog`). DB 무작위 선택·프로파일 희귀도/카테고리 가중치 폐기. **신설 `Resources/loot_tables.txt`**(지역 × 상자 종류, 현재 scrap_market만·가안) + 찾는 순서 `TryFindPool`(종류_night → 종류, 지역 → scrap_market, 없으면 일반 표). 상자는 매 판 섞고 계산대·금고는 늘 채움, 바닥은 지점당 최대 2개, 프로파일 없으면 기본 예산 20/30. 고철은 stall·register·safe(+시체)만 — `region_loot` scrap_market `scrap_money` 3행 삭제(txt·csv). 삭제: `SpawnTable`·`RegionLootBootstrap`·`ItemSpawnPoint` 자체 스폰/재스폰/`linkedContainer`/`useRegionLoot`/`regionIdOverride`·`MapSpawnController.fallbackToRegionLoot`·`LootContainer.initialLoot`. 가짜 3D 상자 → `InteriorBuild.MakeSearchable`로 진짜 상자. 시체 = `corpse` 표(씬 루팅 지역). 루팅 화면 `LootListUI`(옛 수색 연출 되살림, 가치 표기 없음) + 기록 `LootTake`. |
| 2026-09-11 | **좌판도 늘 채움.** 검증(Systems → Zone1): 좌판을 보통 상자와 같이 섞었더니 상자 117개 · 예산 약 42뽑기에 밀려 좌판 4개가 전부 비었고 **Zone1 상자에서 고철이 0**이었다. 좌판도 계산대·금고처럼 예산과 무관하게 늘 채운다(`MapSpawnController.IsSpecial`). 같은 검증에서: 나무상자 17/46·잡동사니 6/47·트렁크 2/20 찬 상자, 바닥 한 자리 최대 2, 현금·옛 코인 0, 보석상 계산대 1·금고 3 모두 채워지고 고철 포함, 수색 → 전부 가져가기 → 레이드 기록 0→2. **재확인(수정 후)**: 좌판 4/4 채움, Zone1 상자 고철 = 좌판에서만, 현금·옛 코인 0, 에러 0. (수색 도중의 '? ? ?' 상태는 자동 확인 간격이 길어 캡처하지 못함 — 눈으로 확인 필요) |
| 2026-09-11 | **문서 현행화.** §개요 표(스폰 앵커·loot_tables·LootListUI/LootTake, `RegionLootBootstrap` 행 삭제), §2를 일반 표 6티어 + 상자 종류 표·찾는 순서·고철 규칙(§2.1~2.4)으로, §3 scrap_market 낮에서 `scrap_money` 제거, §4 씬 연동·§5 예산·§6 씬 빌더를 현 코드로 교체(현존하지 않는 `MapSpawnProfileEditor`·Generate 메뉴·`MapObjectSpawner`·컨트롤러 자동 생성·앵커 자체 스폰·상자별 SpawnTable 서술 제거), §7 EV 재계산 필요 표시. |
