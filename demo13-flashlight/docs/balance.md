# 밸런스 단일 컨트롤 표면 (Balance Control Surface)

> **이 문서 = "어떤 밸런스 값이 어디에 있나"의 단일 색인.**
> 수치를 바꿀 때 "코드 어디를 뒤져야 하지?"를 없애기 위한 지도.
> 튜닝 가능한 전역 값은 전부 **`GameTuning`(Control Panel)** 에 모이고, 양이 많은 테이블형 데이터(드랍·전투·평판)는 **데이터 파일**에 둔다.

관련 문서: [`tuning.md`](tuning.md)(Control Panel 메커니즘), [`survival.md`](survival.md)(생존 수치 의미), [`combat.md`](combat.md)(전투), [`region-loot.md`](region-loot.md)(드랍 테이블).

---

## 1. 단일 진실원 (영역별)

| 밸런스 영역 | 단일 진실원(SSOT) | 조정 방법 |
|------------|-------------------|-----------|
| 시간(낮/밤) · 레이드 · 수색 · 드랍 배율 · 아노말리 · **생존** · **수면** | **`GameTuning`** SO (`Assets/Resources/Data/GameTuning.asset`) | **Control Panel** (`Tools ▸ TopDown ▸ 컨트롤 패널`) — 필드 자동 노출 |
| 맵별 드랍 테이블 (어떤 아이템이 얼마나 나오나) | `tools/region_loot.csv` → `Assets/Resources/region_loot.txt` (런타임은 `.txt`를 `RegionLootCatalog`가 읽음) | CSV 편집 후 `.txt`로 반영 (드랍 **수량 배율**만 GameTuning.lootCountMult) |
| 전투·이동 수치 (플레이어/적 스탯, **이동속도 포함**) | `StatDB` SO (`Assets/Resources/Data/StatDB.asset`) — `playerStat.moveSpeed` 등 | **Control Panel ▸ 🎮 스탯 DB 섹션** (한 창에서 조정) / StatDB 에디터 / 인스펙터 |
| 평판/평판 티어 | `tools/balance/reputation.csv`, `tools/balance/reputation_tiers.csv` | CSV 편집 (→ `ReputationManager`/`ReputationTier`) |
| 회복 아이템 수치 (음식 effectValue 등) | `Assets/Resources/Items/**/*.asset` (ItemData SO) | 아이템 인스펙터 (→ [survival.md §4](survival.md), [items.md](items.md)) |
| **상점 해금 평판** (희귀도별 진열 해금 등급) | **`GameTuning`** (`shopTierRare`/`shopTierEpic`/`shopTierLegendary`) | **Control Panel** — `ShopUI`가 읽음 |
| **아르바이트** (납품 슬롯 수·수량·보수 배율·PP 주기) | **`GameTuning`** (`arbeitSlotCount`/`arbeitQtyMin`/`arbeitQtyMax`/`arbeitRewardMult`/`arbeitPpEvery`) | **Control Panel** — `ArbeitBoard`가 읽음 (→ [economy.md §아르바이트](economy.md)) |
| **스토리/대화 페이싱** (프롤로그 대기·wait 배율·페이드인·타이핑 속도·튜토 표시) | **`GameTuning`** (`prologueStartDelay`/`storyWaitScale`/`storyFadeInDuration`/`narrationTypingSpeed`/`dialogueTypingSpeed`/`tutorialDefaultDuration`) | **Control Panel** — `StoryPlayer`/`GameStartHandler`/`NarrationUI`/`DialogueUI`가 읽음 |
| NPC 상점별 판매 목록 (뭘 파나) · 가격배율 · 위탁허용 | `ShopData` SO (`Assets/Resources/Data/Shops/Shop_*.asset`: stock/buyRate/sellRate/allowConsignment) | 상점 에셋 인스펙터 (※ Control Panel 통합 상점 에디터는 후속 과제) |
| **특성(Trait) 효과 수치** (effectKey value, 41종) | `Assets/Resources/Data/Traits/*.asset` (`TraitData.effects`) — 런타임 합성 `TraitManager` | 트레잇 에셋 인스펙터 (→ [traits.md §4](traits.md)). ⚠ 현재 SO가 진실원 · **GameTuning 이관은 TBD**(미정) |

> 원칙: **전역 단일 스칼라 값 = GameTuning(Control Panel)**. **행이 많은 표(아이템×지역, 유닛×스탯, 상점별 목록) = 데이터 파일/SO.** 표를 코드로 옮기지 않는다.
>
> **컨벤션(2026-06-18 확정): 앞으로 모든 신규 밸런스는 이 컨트롤 표면에 병합한다** — 튜닝 가능한 전역 값은 GameTuning에 추가(→ Control Panel 자동 노출), 표형 데이터는 데이터 파일/SO에 두되 **반드시 이 문서(§1·§3)에 색인**한다. "기능적 밸런스"(상점 해금 평판 등)도 가능하면 GameTuning으로.

---

## 2. GameTuning 필드 전체 (= Control Panel에서 조정)

`Assets/Scripts/Systems/GameTuning.cs`. `GameTuning.Instance.필드`로 읽고, **에셋이 없으면 각 시스템이 자체 기본값으로 폴백**(크래시 없음).

### 수색
| 필드 | 기본값 | 의미 | 읽는 곳 |
|------|--------|------|---------|
| `searchSpeedMult` | 1 | 루팅 상자 아이템 공개 딜레이 배율 (>1 느림) | `CharacterPanelUI.GetSearchDelay` |
| `searchSecCommon` | 0.4 | Common 수색 기본 딜레이(초). ×searchSpeedMult | `CharacterPanelUI.GetSearchDelay` |
| `searchSecUncommon` | 0.6 | Uncommon 수색 기본 딜레이(초) | `CharacterPanelUI.GetSearchDelay` |
| `searchSecRare` | 0.9 | Rare 수색 기본 딜레이(초) | `CharacterPanelUI.GetSearchDelay` |
| `searchSecEpic` | 1.3 | Epic 수색 기본 딜레이(초) | `CharacterPanelUI.GetSearchDelay` |
| `searchSecLegendary` | 1.8 | Legendary 수색 기본 딜레이(초). 고급일수록 늦게 공개 | `CharacterPanelUI.GetSearchDelay` |

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
| `lootCountMult` | 1 | 지역 루트 바닥 스폰 **수량** 배율 (테이블 자체는 region_loot) | `RegionLootBootstrap.DropTier` |
| `lootChanceMult` | 1 | 루트 롤이 실제로 떨어질 **확률** 배율(전역). 1=항상 통과(기존 동작), <1=빈손 증가 | `RegionLootCatalog.Roll` |
| `valuableWeightMult` | 1 | **귀중품(Valuable)** 카테고리 등장 가중치 배율. 1=동일, >1=귀중품 더 자주 | `RegionLootCatalog.Roll` |
| `itemSpawnCountMult` | 1 | 맵 아이템 스폰 **총량** 전역 배율 (프로파일 spawnMultiplier에 추가로 곱) | `MapSpawnProfile.GetGroundBudget`/`GetContainerBudget` |

> **몹 스폰 마릿수/밀도는 외부화하지 않음** — `SpawnZone.EnemyCount`를 런타임에 읽어 적을 인스턴스화하는 시스템이 현재 코드에 **없다**(SpawnZone은 정의·씬 데이터만, 소비처 0). 곱할 지점이 없어 노브가 죽은 값이 됨. 적 스폰 런타임 시스템이 생기면 그때 `enemySpawnCountMult`를 신설·연결할 것.

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

### 아르바이트 (2026-07-02, 게시판 납품 — economy.md §아르바이트)
| 필드 | 기본값 | 의미 | 읽는 곳 |
|------|--------|------|---------|
| `arbeitSlotCount` | 3 | 게시판 동시 의뢰 수 | `ArbeitBoard` |
| `arbeitQtyMin` | 2 | 의뢰당 요구 수량 최소 | `ArbeitBoard` |
| `arbeitQtyMax` | 5 | 의뢰당 요구 수량 최대 | `ArbeitBoard` |
| `arbeitRewardMult` | 1.0 | 보수 배율 (sellPrice×수량×이 값, 10 단위 올림) | `ArbeitBoard` |
| `arbeitPpEvery` | 3 | 납품 n회마다 PP +1 (0=지급 안 함) | `ArbeitBoard` |

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

### 무게 초과 페널티 (2026-07-10 — **구현 전, 기획 확정 · 필드 예정**)

> 하드컷 폐지 → 3구간 페널티 곡선. 기준 = `PlayerInventory.MaxWeight`(30kg × 특성 `weight_max`). 수치는 1차 초안(플레이 조정 전제). [→ inventory.md §무게 시스템](inventory.md)

| 필드(예정) | 기획값 | 의미 | 읽는 곳(예정) |
|------|--------|------|---------|
| `overweightSoftPct` | 1.0 | 이 비율(현재무게/MaxWeight)까지 정상 | `TopDownPlayer`/`PlayerInventory` |
| `overweightSprintBlockPct` | 1.0 | 이 비율 초과 시 스프린트 불가(=과적 시작) | `TopDownPlayer` |
| `overweightSlow1` | 0.15 | 과적(100~115%) 이동속도 감소율 | `TopDownPlayer` |
| `overweightSlow2` | 0.30 | 심각(115~130%) 이동속도 감소율(+스태미너 회복 절반) | `TopDownPlayer` |
| `overweightHardPct` | 1.3 | 이 비율 이상 더 못 담음(하드컷) | `PlayerInventory` |

---

## 3. 데이터 파일 (테이블형 — 코드로 옮기지 않음)

| 파일 | 무엇 | 읽는 코드 |
|------|------|-----------|
| `Assets/Resources/region_loot.txt` (소스 `tools/region_loot.csv`) | **맵별 드랍 테이블** — 지역×티어별 아이템·확률·수량 | `RegionLootCatalog` |
| `Assets/Resources/Data/StatDB.asset` | **전투 수치** — 플레이어 스탯 + 유닛(적)별 스탯 | `StatDB.Instance.GetUnit(key)` / `.playerStat` |
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
| 2026-06-18 | 밸런스 수치가 코드·SO·CSV에 흩어져 "어디서 고치지?"가 매번 발생. 단일 컨트롤 표면이 필요. (값은 바꾸지 말고 위치만 중앙화) | **생존(`SurvivalStats`)·수면(`SleepUI`) 수치를 `GameTuning` 필드로 외부화** — survivalWater/SatietyMinutesToEmpty, survivalStarveHpPerSec, sleep4h/8h(HpPct/Water/Satiety). 각 시스템은 GameTuning 경유로 읽고 **에셋 없으면 기존 값으로 폴백**(값 동일 유지). 드랍 테이블·StatDB·reputation은 데이터 파일에 유지하고 이 문서에 단일 진실원 표로 정리. | 디자이너가 Control Panel 한 창에서 전역 스칼라 조정, 행이 많은 테이블은 데이터 파일에 분리. 단일 색인으로 "밸런스가 어디 있는지"를 고정. |
