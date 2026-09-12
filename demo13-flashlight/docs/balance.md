# 밸런스 단일 컨트롤 표면 (Balance Control Surface)

## 2026-09-12 이동 체감 조정

사용자 결정: 맵 크기에 비해 느려 걷기·달리기를 높이고 모션 배속을 허용한다. `StatDB.playerStat` 걷기 1.2m/s, 달리기 배율 3(3.6m/s), 앉기 배율 0.5(0.6m/s). `PlayerRig` 발 접지 기준 0.45/2.3m/s, 애니 배속 상한 2.7. 후속 “걷기가 경박스럽다” 피드백으로 walk.animSpeed=0.5를 적용해 실제 걷기 약 1.33배·달리기 약 1.57배·앉아 이동 약 1.33배 재생. 이전 원본 배속 고정 수치를 대체하며 근거·검증은 [combat.md](combat.md)에 기록한다.

> **이 문서 = "어떤 밸런스 값이 어디에 있나"의 단일 색인.**
> 수치를 바꿀 때 "코드 어디를 뒤져야 하지?"를 없애기 위한 지도.
> 튜닝 가능한 전역 값은 전부 **`GameTuning`(Control Panel)** 에 모이고, 양이 많은 테이블형 데이터(드랍·전투·평판)는 **데이터 파일**에 둔다.

관련 문서: [`tuning.md`](tuning.md)(Control Panel 메커니즘), [`survival.md`](survival.md)(생존 수치 의미), [`combat.md`](combat.md)(전투), [`region-loot.md`](region-loot.md)(드랍 테이블).

---

## 1. 단일 진실원 (영역별)

| 밸런스 영역 | 단일 진실원(SSOT) | 조정 방법 |
|------------|-------------------|-----------|
| 시간(낮/밤) · 레이드 · 수색 · 드랍 배율 · 아노말리 · **생존** · **수면** · **약탈자 회수**(`scavengerCount`) | **`GameTuning`** SO (`Assets/Resources/Data/GameTuning.asset`) | **Control Panel** (`Tools ▸ TopDown ▸ 컨트롤 패널`) — 필드 자동 노출 |
| 루팅 표 (무엇이·몇 개씩 나오나) — 상자·바닥·시체 | `Assets/Resources/region_loot.txt`(지역×시간대 일반 표, 소스 `tools/region_loot.csv`) + `Assets/Resources/loot_tables.txt`(지역×상자 종류 표, 2026-09-11 신설) — 둘 다 `RegionLootCatalog`가 읽음 | Control Panel「지역 루트」탭 / 「CSV 밸런스」탭. **몇 개·어디에(예산)**는 `MapSpawnProfile` + GameTuning `lootCountMult`·`itemSpawnCountMult`·`interiorLootBudgetMult` ([region-loot.md §2 · §5](region-loot.md)) |
| 전투·이동 수치 (플레이어/적 스탯, **이동속도 포함**) | `StatDB` SO (`Assets/Resources/Data/StatDB.asset`) — `playerStat.moveSpeed` 등 | **Control Panel ▸ 🎮 스탯 DB 섹션** (한 창에서 조정) / StatDB 에디터 / 인스펙터 |
| 평판/평판 티어 | `tools/balance/reputation.csv`, `tools/balance/reputation_tiers.csv` | CSV 편집 (→ `ReputationManager`/`ReputationTier`) |
| 회복 아이템 수치 (음식 effectValue 등) | `Assets/Resources/Items/**/*.asset` (ItemData SO) | 아이템 인스펙터 (→ [survival.md §4](survival.md), [items.md](items.md)) |
| **상점 해금 평판 + 재고 회전** (희귀도별 진열 해금 등급 · 일일 회전 슬롯 수) | **`GameTuning`** (`shopTierRare`/`shopTierEpic`/`shopTierLegendary`/`shopRotationSlots`) | **Control Panel** — `ShopUI`가 읽음 (재고 회전 → [economy.md §A](economy.md)) |
| **스토리/대화 페이싱** (프롤로그 대기·wait 배율·페이드인·타이핑 속도·튜토 표시) | **`GameTuning`** (`prologueStartDelay`/`storyWaitScale`/`storyFadeInDuration`/`narrationTypingSpeed`/`dialogueTypingSpeed`/`tutorialDefaultDuration`) | **Control Panel** — `StoryPlayer`/`GameStartHandler`/`NarrationUI`/`DialogueUI`가 읽음 |
| **건물 내부**(진입 가능 비율 · 내부 루팅 예산 배율) | **`GameTuning`** (`buildingEnterRatio`/`interiorLootBudgetMult`) | **Control Panel** — 비율은 `빌드 ▸ 지역1` 재실행 시 반영, 예산은 런타임 즉시. 루트 **표**는 건물 종류별 `int_*` 상자 종류 표(`loot_tables.txt`, [region-loot.md §2.2](region-loot.md)) — 2026-09-11부터(그 전엔 지역 일반 표). QA가 "건물을 더 열지/파밍을 늘릴지" 판단해 조절 |
| **투척물 유인** (돌 사거리·착탄 유인 반경·비행·조사 시간) | **`GameTuning`** (`throwRange`/`throwNoiseRadius`/`throwSpeed`/`noisePulseDuration`/`noiseInvestigateLook`) | **Control Panel** — `ThrowSystem`/`Distraction`/`EnemyController`가 읽음. ⚠️ 소음 시스템은 2026-09-09 폐기 — 행동별 소음 필드(`noiseWalk`/`noiseRun`/`noiseAttack`/`noiseDoor`/`noiseUiMax`) 삭제됨 (→ [combat.md §유인](combat.md)) |
| **근접 전투** (콤보 on/off · 적 강공 확률·강제주기·예비동작/데미지 배율) | **`GameTuning`** (`comboEnabled`/`enemyHeavyChance`/`enemyHeavyForceAfter`/`enemyHeavyWindupMult`/`enemyHeavyDamageMult`) | **Control Panel** — 런타임 즉시. 콤보는 2026-07-11 사용자 결정으로 **기본 OFF**(데이터는 보존) (→ [combat.md](combat.md)) |
| **원거리 적(총기 밴딧)** (사거리·멈추는 거리·조준 경고·탄속·점사·퍼짐) | 총 자체는 **`WeaponData`**(플레이어와 같은 에셋) · 적 전용은 **`StatDB`** 유닛별 (`rangedWeaponData`/`rangedDamageMult`/`rangedBulletSpeedMult`/`attackRange`/`preferredRange`/`attackWindup`/`burstCount`/`burstInterval`/`spreadDeg` — 2026-09-11 총 수치 통합) + 공통 타이밍은 **`GameTuning`** (`enemyAimLockTime` 0.2 / `enemyRangedRecover` 0.35 / `enemyGunDrawTime` 0.6 / `enemyBulletRangeMult` 1.25) | **Control Panel ▸ 🎮 스탯 DB** + 총기 밴딧(AI). 점사 간격은 플레이어 피격 무적창 0.35초보다 길게 (→ [combat.md §총기 밴딧](combat.md)) |
| **적 고철 드랍** (유닛별 고철 범위) | **`StatDB`** 유닛별 `cashMin`/`cashMax`(이름은 옛 현금 시절 그대로) (일반 50~150 · 총기 80~200 · 탱크 150~400) — 고철 화폐 아이템(`scrap_money`) 1개 = ◈1. 2026-09-11 현금 → 고철 교체(현금은 퀘스트·이벤트 전용) | **Control Panel ▸ 🎮 스탯 DB** (→ [economy.md §적 고철 드랍](economy.md)) |
| **총격(플레이어)** (총기 스펙 · 탄 스펙 · 조준 카메라 · 관통 감쇠) | 총마다 **`WeaponData`** (`rpm`/`damage`/`projectileSpeed`/`effectiveRange`/`hip·adsSpreadDeg`/`recoilPerShot·Max·Recover`/`reloadSeconds`/`adsMoveMult`) · 탄마다 **탄 아이템 `ItemData`** (`ammoDamageMult`/`ammoPenetration`/`ammoSpeedMult`/`ammoRangeMult`/`ammoSpreadMult`) · 공통 **`GameTuning`** (`aimLookAhead` 0.35 / `aimLookAheadMax` 4 / `gunPierceDamageKeep` 0.6) | 무기·탄 SO 인스펙터 + **Control Panel ▸ 총격(플레이어)** (→ [combat.md §총격전](combat.md)) |
| **도로 장애물·막힌 통로** (장애물 밀도·최소 통행폭, 잔해 치우기/강제돌파 시간) | **`GameTuning`** (`roadObstacleDensity`/`roadMinPassWidth`/`barricadeClearSeconds`/`barricadeBreachSeconds`) | **Control Panel** — 밀도·통행폭은 `빌드 ▸ 지역1` 재실행 시 반영, 시간은 런타임 즉시. ⚠️ `barricadeNoiseRadius`는 소음 폐기(2026-09-09)로 삭제 — **돌파의 대가는 시간뿐** (→ [level-scrapmarket.md 2026-07-11](level-scrapmarket.md)) |
| NPC 상점별 판매 목록 (뭘 파나) · 가격배율 · 위탁허용 | `ShopData` SO (`Assets/Resources/Data/Shops/Shop_*.asset`: stock/buyRate/sellRate/allowConsignment) | 상점 에셋 인스펙터 (※ Control Panel 통합 상점 에디터는 후속 과제) |
| **특성(Trait) 효과 수치** (effectKey value, 41종) | `Assets/Resources/Data/Traits/*.asset` (`TraitData.effects`) — 런타임 합성 `TraitManager` | 트레잇 에셋 인스펙터 (→ [traits.md §4](traits.md)). ⚠ 현재 SO가 진실원 · **GameTuning 이관은 TBD**(미정) |

> 원칙: **전역 단일 스칼라 값 = GameTuning(Control Panel)**. **행이 많은 표(아이템×지역, 유닛×스탯, 상점별 목록) = 데이터 파일/SO.** 표를 코드로 옮기지 않는다.
>
> **컨벤션(2026-06-18 확정): 앞으로 모든 신규 밸런스는 이 컨트롤 표면에 병합한다** — 튜닝 가능한 전역 값은 GameTuning에 추가(→ Control Panel 자동 노출), 표형 데이터는 데이터 파일/SO에 두되 **반드시 이 문서(§1·§3)에 색인**한다. "기능적 밸런스"(상점 해금 평판 등)도 가능하면 GameTuning으로.

---

## 2. GameTuning 필드 전체 (= Control Panel에서 조정)

`Assets/Scripts/Systems/GameTuning.cs`. `GameTuning.Instance.필드`로 읽고, **에셋이 없으면 각 시스템이 자체 기본값으로 폴백**(크래시 없음).

### 수색 (2026-09-09 삭제 → 2026-09-11 되살림)

> 볼륨 축소(2026-09-09) 때 수색 연출과 함께 지웠다가, 2026-09-11 사용자 요청으로 루팅 목록(`LootListUI`)에 되살렸다([region-loot.md §루팅 정리 결정](region-loot.md)). 처음 여는 필드 상자·시체에서 칸이 위에서부터 하나씩 드러나는 시간이다. 옛 `CharacterPanelUI.GetSearchDelay`는 없다 — `LootListUI.RevealDelay`가 읽는다.

| 필드 | 기본값 | 의미 | 읽는 곳 |
|------|--------|------|---------|
| `searchSpeedMult` | 1 | 수색 속도 배율. 한 칸 시간 = `searchSec*` ÷ 이 값 → **>1 빠름**, <1 느림 | `LootListUI.RevealDelay` |
| `searchSecCommon` | 0.4 | Common 한 칸이 드러나는 시간(초) | `LootListUI.RevealDelay` |
| `searchSecUncommon` | 0.6 | Uncommon (초) | `LootListUI.RevealDelay` |
| `searchSecRare` | 0.9 | Rare (초) | `LootListUI.RevealDelay` |
| `searchSecEpic` | 1.3 | Epic (초) | `LootListUI.RevealDelay` |
| `searchSecLegendary` | 1.8 | Legendary (초). 고급일수록 늦게 공개 | `LootListUI.RevealDelay` |

### 시간 / 현상
| 필드 | 기본값 | 의미 | 읽는 곳 |
|------|--------|------|---------|
| `dayDuration` | 120 | 지역 낮 길이(초) | `RegionTimeManager.InitRegions` |
| `nightDuration` | 600 | 지역 밤(현상) 길이(초) | `RegionTimeManager.InitRegions` |

### 레이드
| 필드 | 기본값 | 의미 | 읽는 곳 |
|------|--------|------|---------|
| `raidDuration` | 1200 | 레이드 제한 시간(초). 0=무제한 | `RaidManager.Start` |

### 드랍
| 필드 | 기본값 | 의미 | 읽는 곳 |
|------|--------|------|---------|
| `lootCountMult` | 1 | 맵 루팅 **예산(뽑기 횟수)** 배율 — 바닥·상자 둘 다(2026-09-11~, 그 전엔 삭제된 `RegionLootBootstrap`만 읽음). 표 자체는 region_loot·loot_tables | `MapSpawnController.ExecuteSpawn` |
| `lootChanceMult` | 1 | 루트 롤이 실제로 떨어질 **확률** 배율(전역). 1=항상 통과(기존 동작), <1=빈손 증가 | `RegionLootCatalog.RollOnce` |
| `valuableWeightMult` | 1 | **귀중품(Valuable)** 카테고리 등장 가중치 배율. 1=동일, >1=귀중품 더 자주 | `RegionLootCatalog.RollOnce` |
| `itemSpawnCountMult` | 1 | 맵 아이템 스폰 **총량** 전역 배율 (프로파일 spawnMultiplier에 추가로 곱) | `MapSpawnProfile.GetGroundBudget`/`GetContainerBudget` |

> ~~몹 스폰 마릿수/밀도는 외부화하지 않음~~ — **해소됨.** `EnemySpawner`가 씬의 `SpawnZone`을 읽어 실제로 스폰하므로 `enemySpawnCountMult`는 살아 있는 노브다(위 §근접/적 표 참조).

### 적 무리 · 레이드 스폰 안전 (2026-09-09)
| 필드 | 기본값 | 의미 | 읽는 곳 |
|------|--------|------|---------|
| `enemyPackRadius` | 12 | 한 '무리'로 볼 반경(m). 이 안의 스폰존은 사실상 동시에 달려든다 | `Zone1GreyboxLayout.TrimEnemyPacks` (빌드 시) |
| `enemyPackMaxWeight` | **3** | 무리 하나의 최대 무게(일반=1, 중장=2). 초과분은 빌드 때 잘림 | `Zone1GreyboxLayout.TrimEnemyPacks` (빌드 시) |
| `raidSpawnSafeRadius` | 20 | 레이드 진입 스폰이 확보해야 할 '적 없는' 반경(m) | `RaidSpawnDirector.PickSpawn` (런타임) |

> 앞의 둘은 **빌드 시** 적용 — 바꾸면 `Tools ▸ TopDown ▸ 빌드3D ▸ 지역1` 재실행. 마지막 하나는 런타임 즉시.

### 생존 (2026-06-18 중앙화)
| 필드 | 기본값 | 의미 | 읽는 곳 |
|------|--------|------|---------|
| `survivalWaterMinutesToEmpty` | 18 | 수분 100→0까지 분(레이드 실시간). SurvivalStats가 `Max/(분·60)`로 초당 환산 | `SurvivalStats` |
| `survivalSatietyMinutesToEmpty` | 36 | 포만감 100→0까지 분 | `SurvivalStats` |
| `survivalStarveHpPerSec` | 0.6 | 빈 스탯(0) 1개당 초당 HP 감소(silent DoT). 둘 다 0이면 2배 | `SurvivalStats` |

### 수면 (2026-06-18 중앙화)
| 필드 | 기본값 | 의미 | 읽는 곳 |
|------|--------|------|---------|
| `sleep4hHpPct` | 0.40 | 4시간 수면 HP 회복 비율(MaxHp 기준) | `SleepUI` |
| `sleep4hWater` | 18 | 4시간 수면 수분 차감 | `SleepUI` |
| `sleep4hSatiety` | 18 | 4시간 수면 포만감 차감 | `SleepUI` |
| `sleep8hHpPct` | 1.00 | 8시간 수면 HP 회복 비율 | `SleepUI` |
| `sleep8hWater` | 38 | 8시간 수면 수분 차감 | `SleepUI` |
| `sleep8hSatiety` | 38 | 8시간 수면 포만감 차감 | `SleepUI` |

> 수면의 `hours`(4/8)와 옵션 구조는 코드 고정, 회복/차감 수치만 GameTuning 경유. 시간 경과(시계 진행)는 `hours` 그대로 사용.

### 짙은현상 (아노말리)
| 필드 | 기본값 | 의미 | 읽는 곳 |
|------|--------|------|---------|
| `anomalyActiveDuration` | 180 | 현상 활성 지속(초) | `AnomalyZone` |
| `anomalyTelegraph` | 8 | 징조(텔레그래프) 시간(초) | `AnomalyZone` |
| `anomalyWarning` | 30 | 종료 경고 시간(초) | `AnomalyZone` |
| `anomalyCollapse` | 5 | 붕괴 연출 시간(초) | `AnomalyZone` |
| `anomalyIntervalMin` | 90 | 자동 발생 간격 최소(초) | `AnomalyManager` |
| `anomalyIntervalMax` | 180 | 자동 발생 간격 최대(초) | `AnomalyManager` |
| `anomalyMaxConcurrent` | 1 | 동시 활성 최대 개수 | `AnomalyManager` |
| `anomalyMonsterMinDist` | 6 | 몬스터 최소 스폰 거리(m) | (Phase 2) |

### 경험치/레벨 (2026-07-06, 레이드 XP→레벨업→PP — traits.md §2)
| 필드 | 기본값 | 의미 | 읽는 곳 |
|------|--------|------|---------|
| `xpExtractBonus` | 100 | 탈출 성공 고정 보너스 XP | `RaidManager.SettleXp` |
| `xpPerLootValue` | 0.02 | 루팅 XP = 소지품 가치(sellPrice×스택) **시작/종료 차액** × 이 값 (성공 시만) | `RaidManager.SettleXp` |
| `xpFailMult` | 0.5 | 실패(사망/시간초과) 시 킬 XP 배율 | `RaidManager.SettleXp` |
| `xpPerLevelBase` | 100 | 다음 레벨 필요 XP = 이 값 × 현재 레벨 (선형) | `PlayerProgress.XpToNext` |
| `xpPpPerLevel` | 1 | 레벨업당 PP 지급량 | `PlayerProgress.GrantXp` |

> 킬 XP 자체는 유닛별 `UnitStatData.expReward`(StatDB — Control Panel 스탯 DB 섹션) — 표형 데이터라 §1 원칙대로 SO 유지.

### ~~아르바이트~~ — **폐기 (2026-09-09)** · `arbeit*` 필드 5종 삭제 ([scope-cut.md](scope-cut.md))

### 스토리/대화 페이싱 (2026-07-10, 프롤로그·내레이션·대화·튜토리얼 완급)

> 새 게임 프롤로그 흑화면이 길다는 피드백 → 전역 페이싱 값을 GameTuning으로 노출(컨트롤 패널 슬라이더로 조절). authored `wait` 노드는 `storyWaitScale` 전역 배율로 JSON 안 건드리고 완급 조정. S-000의 fade_in은 명시값 제거 → `storyFadeInDuration` 사용.

| 필드 | 기본값 | 의미 | 읽는 곳 |
|------|--------|------|---------|
| `prologueStartDelay` | 0.2 | 새 게임 프롤로그 시작 전 대기(초) | `GameStartHandler` |
| `storyWaitScale` | 0.6 | 스토리 `wait` 노드 전역 배율(1=원본) | `StoryPlayer` |
| `storyFadeInDuration` | 0.8 | 스토리 `fade_in` 기본 지속(초, 노드 미명시 시) | `StoryPlayer` |
| `narrationTypingSpeed` | 0.02 | 내레이션 타이핑 속도(자당 초) | `NarrationUI` |
| `dialogueTypingSpeed` | 0.02 | 대화 타이핑 속도(자당 초) | `DialogueUI` |
| `tutorialDefaultDuration` | 4 | 튜토리얼 프롬프트 기본 표시(초, 노드 미명시 시) | `StoryPlayer` |

### 무게 초과 페널티 (2026-07-10 — ✅ **구현 완료** · GameTuning 6필드)

> 하드컷 폐지 → 3구간 페널티 곡선. 기준 = `PlayerInventory.MaxWeight`(30kg × 특성 `weight_max`). 수치 1차 초안(플레이 조정 전제). [→ inventory.md §무게 시스템](inventory.md)

| GameTuning 필드 | 값 | 의미 | 읽는 곳 |
|------|--------|------|---------|
| `overweightStartPct` | 1.0 | 과적 시작 비율(스프린트 불가 + 이속 −slow1) | `PlayerInventory`→`TopDownPlayer` |
| `overweightSeverePct` | 1.15 | 심각 시작 비율(이속 −slow2 + 스태미너 회복 배율) | `PlayerInventory`→`TopDownPlayer` |
| `overweightHardPct` | 1.30 | 이 비율 넘게는 외부 루팅 못 담음(하드컷) | `PlayerInventory` |
| `overweightSlow1` | 0.15 | 과적(100~115%) 이동속도 감소율 | `TopDownPlayer` |
| `overweightSlow2` | 0.30 | 심각(115~130%+) 이동속도 감소율 | `TopDownPlayer` |
| `overweightRegenMult` | 0.5 | 심각 이상 스태미너 회복 배율 | `TopDownPlayer` |

---

## 3. 데이터 파일 (테이블형 — 코드로 옮기지 않음)

| 파일 | 무엇 | 읽는 코드 |
|------|------|-----------|
| `Assets/Resources/region_loot.txt` (소스 `tools/region_loot.csv`) | **일반 루팅 표** — 지역×시간대 티어(container/ground × day/night/anomaly)별 아이템·가중치·수량 | `RegionLootCatalog` |
| `Assets/Resources/loot_tables.txt` (2026-09-11 신설) | **상자 종류 루팅 표** — 지역×상자 종류(junk·trunk·stall·crate·register·safe·ground·corpse·int_*). 종류 표가 일반 표보다 먼저. 현재 scrap_market만·가안 | `RegionLootCatalog.TryFindPool` ([region-loot.md §2.2~2.4](region-loot.md)) |
| `Assets/Resources/Data/StatDB.asset` | **전투 수치** — 플레이어 스탯 + 유닛(적)별 스탯 | `StatDB.Instance.GetUnit(key)` / `.playerStat` |
| `Assets/Resources/Data/MapSpawn/*.asset` (`MapSpawnProfile`) | **맵별 루팅 예산**(뽑기 횟수) — 바닥/상자 min~max·`spawnMultiplier`·밤 배율. 5지역 5개 에셋, 씬(Zone1·실내)이 쓰는 건 **`scrap_market.asset`뿐**. 희귀도·카테고리 필드는 2026-09-11부터 안 쓴다(내용물 = 루팅 표). 예산엔 GameTuning `itemSpawnCountMult`·`lootCountMult`(실내면 `interiorLootBudgetMult`)가 곱해진다 | 씬의 `MapSpawnController`(profile 참조) → `ItemSpawnPoint` 앵커 ([region-loot.md §5](region-loot.md)) |
| `tools/balance/reputation.csv` | 평판 변동 값 | `ReputationManager` |
| `tools/balance/reputation_tiers.csv` | 평판 티어 경계 | `ReputationTier` |
| `Assets/Resources/Items/**/*.asset` | 아이템 개별 수치(가격·무게·회복량 등) | `ItemDatabase` |

---

## 4. 값 추가법 (GameTuning 확장)

1. `GameTuning.cs`에 필드 하나 추가(Tooltip 권장, 직관 단위 사용).
2. 쓰는 시스템에서 `GameTuning.Instance.필드` 읽기 — **`Instance`가 null이거나 비정상 값이면 자체 기본값으로 폴백**.
3. Control Panel에 **자동 노출** (SerializedObject 제너릭 드로우). 별도 UI 코드 불필요.
4. 이 문서 §2 표에 한 줄 추가.

---

## 변경 로그

| 날짜 | 던진 질문/맥락 | 결정 | 근거 |
|------|----------------|------|------|
| 2026-06-18 | 사용자: "하드코딩·분산된 밸런스를 GameTuning으로 모으고 소비처가 읽게. 값은 바꾸지 말고 위치만, 에셋 null 폴백." | **수색 시간(희귀도별 5필드)·전역 드랍 노브 3개를 GameTuning에 추가·연결.** ① 수색: `searchSecCommon/Uncommon/Rare/Epic/Legendary`(0.4/0.6/0.9/1.3/1.8) ← `CharacterPanelUI.GetSearchDelay`가 하드코딩했던 값을 읽음(메서드만 수정). ② 드랍: `lootChanceMult`(1, 롤 게이트), `valuableWeightMult`(1, Valuable 가중치 배율) ← `RegionLootCatalog.Roll`. ③ `itemSpawnCountMult`(1) ← `MapSpawnProfile.Get*Budget`. **전부 기본값=기존 실효값, 곱/폴백이 항등이라 동작 불변.** **몹 스폰 마릿수는 런타임 소비처(SpawnZone 사용처)가 없어 외부화 보류**(명시). | 분산 밸런스를 단일 컨트롤 표면으로. region_loot 테이블 자체는 CSV 유지하고 전역 배율·확률만 GameTuning. 값 무변경(위치 중앙화). |
| 2026-06-18 | 사용자: "플레이어 이속을 밸런스 툴에서도 조절하게 — 모든 밸런스는 통일." (이속이 StatDB라 Control Panel 밖에 있었음) | **`StatDB`(플레이어/적 스탯, 이동속도 포함)를 Control Panel에 「🎮 스탯 DB」 섹션으로 임베드** — GameTuning + StatDB가 한 창에서 조정. 이속 = Player Stat ▸ Move Speed. `GameControlPanel.cs`에 SerializedObject 제너릭 드로우 추가. §1 표 갱신. | "모든 밸런스 = 하나의 컨트롤 표면" 컨벤션 강화: 전역 스칼라(GameTuning) + 표형 SO(StatDB)를 같은 창에 모음. |
| 2026-06-18 | 사용자: "밸런스 에디터에서 NPC 상점마다 뭘 팔고 평판 몇에 열리고 같은 기능 밸런스도 추가. 앞으로 밸런스는 전부 거기에 병합하고 그렇게 가자." | **컨벤션 확정: 모든 신규 밸런스 → 이 컨트롤 표면(GameTuning/Control Panel + balance.md 색인)에 병합.** 구체: **상점 희귀도별 해금 평판**을 `GameTuning.shopTierRare/Epic/Legendary`로 외부화(ShopUI가 읽음, 폴백 D/B/A). NPC 상점별 판매목록은 `ShopData` SO에 두고 §1 표에 색인(통합 상점 에디터는 후속). | 밸런스 산재 방지 + 디자이너 단일 창 조정. 앞으로 기능 밸런스도 가능한 GameTuning으로. |
| 2026-07-10 | 무게 초과 페널티(inventory.md 2026-07-10 확정)의 튜닝 수치를 어디에 두나 | **GameTuning 필드 예정 항목으로 색인 등재(구현 전, 기획 확정)** — `overweightSoftPct=1.0`/`overweightSprintBlockPct`/`overweightSlow1=0.15`/`overweightSlow2=0.30`/`overweightHardPct=1.3`. §2 「무게 초과 페널티」 표 신설. 구현 시 GameTuning.cs에 추가 + Control Panel 자동 노출. | 2026-06-18 컨벤션(모든 신규 밸런스 = 이 컨트롤 표면에 병합·색인) 준수. 수치는 1차 초안, 플레이 조정 전제. |
| 2026-09-11 | 루팅 정리(결정 = [region-loot.md §루팅 정리 결정](region-loot.md)) — 문서가 삭제된 코드를 가리킴 | **색인 현행화.** ①§1·§3 루팅 표 = `region_loot.txt` + 신설 `loot_tables.txt`(상자 종류), 예산은 별도. ②§2 수색 6필드 **복귀**(2026-09-09 삭제 → 되살림), 읽는 곳 `LootListUI.RevealDelay`, `searchSpeedMult`는 나누는 값(>1 빠름 — 예전 ">1 느림"은 반대였음). ③`lootCountMult` = `MapSpawnController` 예산 배율(`RegionLootBootstrap` 삭제). `lootChanceMult`·`valuableWeightMult` 읽는 곳 = `RegionLootCatalog.RollOnce`. ④프로파일 경로 `Data/SpawnProfiles/` → 실제 `Data/MapSpawn/`(씬은 `scrap_market.asset`만). ⑤건물 내부 루트 표 = `int_*` 종류 표. **값 변경 없음.** | 코드 = 진실. |
| 2026-06-18 | 밸런스 수치가 코드·SO·CSV에 흩어져 "어디서 고치지?"가 매번 발생. 단일 컨트롤 표면이 필요. (값은 바꾸지 말고 위치만 중앙화) | **생존(`SurvivalStats`)·수면(`SleepUI`) 수치를 `GameTuning` 필드로 외부화** — survivalWater/SatietyMinutesToEmpty, survivalStarveHpPerSec, sleep4h/8h(HpPct/Water/Satiety). 각 시스템은 GameTuning 경유로 읽고 **에셋 없으면 기존 값으로 폴백**(값 동일 유지). 드랍 테이블·StatDB·reputation은 데이터 파일에 유지하고 이 문서에 단일 진실원 표로 정리. | 디자이너가 Control Panel 한 창에서 전역 스칼라 조정, 행이 많은 테이블은 데이터 파일에 분리. 단일 색인으로 "밸런스가 어디 있는지"를 고정. |
