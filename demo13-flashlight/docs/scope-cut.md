# 볼륨 축소 (2026-09-09) — 타르코프 → RPG

> **이 문서가 "무엇을 왜 잘랐나"의 SSOT다.** 각 시스템의 현재 상태는 해당 시스템 md가 진실이고,
> 여기는 **결정과 범위**만 남긴다. 되살릴 땐 git 이력(`chore/scope-cut-rpg` 브랜치 이전).

## 왜

프로젝트가 기획서상 데모 범위(`gdd-demo.md`: 1지역·아이템 30~40종)를 한참 넘어 퍼졌다.
축소 착수 시점 실측:

| 지표 | 값 |
|---|---:|
| C# 파일 / 줄 | 232 / 56,703 |
| 기획 문서 / 줄 | 67 / 18,732 |
| 아이템 SO | 236 |
| 특성(퍽) SO | 41 |
| CharacterPanelUI 단일 파일 | 4,112줄 |

사용자 판단: **"코드·문서 다이어트 + 기획적 기능 축소. 부위별 치료 같은 타르코프 시스템 삭제하고
장비창도 간단하게 RPG처럼."**

## 결정 (2026-09-09)

던진 질문: "볼륨을 줄인다 = 어디까지? (하드코어 생존요소 / 인벤토리 / 메타 시스템 / 큰 덩어리)"

| # | 항목 | 결정 | 상태 |
|:-:|---|---|:-:|
| 1 | **부위별 의료** (5부위 × 출혈·골절·통증) | **삭제** → 단일 HP + 회복 아이템 | ✅ |
| 2 | **소음 시스템** (`PlayerNoise`/`NoiseSystem`) | **삭제** → 적 감지는 시야 기반만 | ✅ |
| 3 | **격자(테트리스) 인벤토리** | **폐기** → 슬롯형. **무게는 유지** | ✅ |
| 4 | **파견(Dispatch) + 아르바이트 보드** | **삭제** | ✅ |
| 5 | **특성(퍽) 41종** | **축소** → 11종 (긍정 8 · 부정 3) | ✅ |
| 6 | **QA 자동화** (4,215줄) | **분리** (삭제 아님 — 아래 근거) | ✅ |

### 자르지 **않기로** 한 것 (같은 질문에서 선택지에 있었으나 미선택)

| 항목 | 처리 |
|---|---|
| 수분·포만감·수면 (`SurvivalStats`, 494줄) | **유지** |
| 내구도 (`hasDurability`) | **유지** |
| 컨테이너 수색 타이머·무기 파츠 부착 | 3번(격자 폐기)에 딸려 정리 |
| 도감·업적·평판 / 라디오 / 일일퀘스트·포스트레이드 이벤트 | **유지** |
| 총기 / 투척물 | **유지** |
| 부위 피격 데미지 배율(`BodyZones`)·적 부위 부상(`UnitInjuries`) | **유지** — 치료가 아니라 전투 감각 |

## 진행

### 1. 부위별 의료 삭제 ✅ (2026-09-09)
- 삭제: `Assets/Scripts/Medical/` 전체(5파일 1,060줄), `Resources/Data/Medical/` SO 13종
- 정리: `GameHUD` 부상 아이콘, `CharacterPanelUI` 부위 상태, `DebugTestUI` 의료 패널,
  `PlayerHitReaction`/`PlayerInventory`/`RaidManager`/`SaveManager` 호출부, `PlayerRig.prefab` 컴포넌트 2종
- 전환: 의료 아이템 13종 → `HealHP` 15~50. `BodyPartType` enum은 `Combat/BodyZones.cs`로 이관
- 상세: [`medical.md`](medical.md)

### 2. 소음 시스템 삭제 ✅ (2026-09-09)
- 삭제: `Combat/PlayerNoise.cs`(102줄) · `Combat/NoiseSystem.cs`(67) · `UI/NoiseHUD.cs`(귀 아이콘 HUD)
- 정리: 이동/타격/총성/문 소음 호출부 전부(`AttackPerformer`·`PlayerGun`·`DoorController`·`BlockedPassage`),
  GameTuning 소음 필드 8종 + `barricadeNoiseRadius`, `WeaponData.noiseRadius`,
  `ItemData.partNoiseMult`, `PlayerEquipment.WeaponPartNoiseMult`, F1 소음 패널
- **판단이 필요했던 지점**: 투척물(돌 유인)은 **유지** 결정인데 소음이 사라지면 돌이 아무 일도 못 한다.
  그래서 **`Distraction.cs`(47줄)** 를 남겼다 — 던진 물건 착탄 지점만 등록하는 최소 API.
  적 `Investigate` 상태는 그대로 살아 있고, 발생원이 투척물 하나로 줄었을 뿐이다.
- **딸려 사라진 것**: 총의 "총성이 사람을 부른다" 대가(→ 탄약 유한성만 남음),
  바리케이드 돌파의 소음 대가(→ 시간만), 잠행 특성 `move_noise`(물릴 시스템 없음 → 5번에서 정리)
- 상세: [`combat.md`](combat.md) §유인

### 4. 파견 + 아르바이트 보드 삭제 ✅ (2026-09-09)
- 삭제: `Systems/DispatchRoster.cs`(152줄) · `UI/DispatchUI.cs`(838) · `Resources/UI/DispatchUI.prefab` ·
  `Systems/ArbeitBoard.cs`(224) · MapSelectUI 아르바이트 오버레이(약 190줄)
- 정리: 하이드아웃 파견 시설 타일·`HideoutUI`/`HideoutDockPanel`/`UIManager`/F1 진입점,
  GameTuning `arbeit*` 5필드, `UIPrefabBaker` 베이크 항목
- **살려 옮긴 것**: `ArbeitBoard.CountOwned`/`TryRemoveOwned`(창고+가방+주머니 집계·원자 차감)는
  `QuestBoard` 납품형 의뢰가 계속 쓴다 → **`Inventory/OwnedItems.cs`**(72줄)로 이관
- **예약 슬롯 유지**: `InteractType.Dispatch` — 뒤의 `Generator`/`Passage` 직렬화 인덱스가 밀리면
  씬의 시설 타일이 엉뚱한 UI를 연다. enum 멤버만 남기고 동작은 제거
- **딸려 사라진 것**: 파산 방지 안전판(레이드 없이 버는 유일한 수단), 비전투 PP 루트(납품 3회당 +1),
  인텔 발생원 3개 → 2개(라디오·랜드마크)
- 상세: [`economy.md`](economy.md) §아르바이트 · [`safehouse.md`](safehouse.md) · [`safehouse-intel.md`](safehouse-intel.md)

### 6. QA 자동화 분리 ✅ (2026-09-09)
사용자 선택지는 "분리/삭제"였다. **분리를 택했다** — 근거:
- 게임 코드가 QA를 참조하는 곳이 **0곳**이다(유일한 접점은 `SaveManager.SuppressWrites` bool 하나).
  그래서 떼어내는 데 아무 대가가 없다.
- `QaBot`은 **빌드된 게임 안에서** 돌아야 해서(`-qa-serve`) 에디터 전용 어셈블리로는 못 옮긴다.
- 지우면 `/qa`·`/qa-loop`·`qa-runner`↔`dev-fixer` 워크플로가 통째로 죽는다. 되살리는 비용이 크다.

구현: `Assets/Scripts/QA/Game.QA.asmdef` 신설 — `Game.Scripts` 참조, `defineConstraints: ["QA_ENABLED"]`.
**평소 빌드에 QA 4,215줄이 한 줄도 안 들어간다.** 켜기 = `Tools ▸ TopDown ▸ QA ▸ QA 자동화 켜기`
(`Assets/Editor/QaDefineToggle.cs`, 체크 표시로 현재 상태 확인).
완전 삭제를 원하면 `Assets/Scripts/QA/` + `tools/qa_*` 제거만 하면 된다 — 언제든 가능.
- 상세: [`qa.md`](qa.md)

### 5. 특성 41종 → 11종 ✅ (2026-09-09)
남긴 기준은 하나: **코드가 실제로 읽는 `effectKey`를 가진 퍽만.**
41개 중 27개는 `effects` 리스트만 채워져 있고 런타임에 아무도 읽지 않는 **종이 퍽**이었다.
- 삭제: `Resources/Data/Traits/` SO 30종(+meta). 코드 변경 0줄 —
  `TraitManager`는 `Resources.LoadAll`로 읽고 세이브 로드는 **정의 없는 id를 이미 버린다**(기존 세이브 안전).
  `TraitPanelUI`도 빈 카테고리를 자동으로 건너뛴다.
- 남은 11종: 끈질긴 폐활량 · 냉정한 손 · 받아넘기기 · 불굴 · 노새 · 감정가 · 그림자 · 단골
  / 부정 3(유리 어깨 · 허약한 위장 · 악몽)
- **손실**: ★현상(Anomaly) 7종 전멸 — 세계관 고유 트리라 "정체성"으로 잡았던 축이다.
  한 줄도 배선 안 된 기획 단계였고, `traits.md` §3.6에 기획은 보존했다. **재개통 1순위.**
- 연쇄 정리: 소음 폐기로 `move_noise`, 의료 폐기로 `pain_penalty`/`fracture_chance`/`bleed_duration`이
  죽어 해당 퍽도 함께 나갔다. 「강골」 삭제로 「불굴」의 `prereqTraitId`를 비웠다(단독 T3).
- 상세: [`traits.md`](traits.md)

### 3. 격자 인벤토리 → 슬롯형 ✅ (2026-09-09)
사용자 결정: **"격자 폐기, 무게만 유지하고 나머지 폐기."**

| 항목 | 이전 | 현재 |
|---|---|---|
| 아이템 크기 | 1×1 ~ 3×3 footprint | **전부 슬롯 1칸** |
| 90도 회전 (R키) | 있음 | 없음 |
| 용량 | 칸 면적 + 무게 | **칸 수 + 무게**(유지) |
| 컨테이너 수색 | 희귀도별 딜레이·프로그레스바·"?" 가림 | 없음 — 열면 바로 다 보인다 → **2026-09-11 되살림**(아래 주) |
| 무기 파츠 | 조준경·소염기·탄창·손잡이 | **탄창만** |

- **방식**: 클래스·API 이름(`InventoryGrid`/`gridX`/`rotated`)을 **일부러 유지**했다 —
  세이브 포맷(`GridItemEntry`)과 호출부 20여 곳을 건드리면 위험만 커지고 얻는 게 없다.
  `EffectiveWidth/Height`가 항상 1을 돌려주고, `CanPlace`는 한 칸만 본다. x/y = 슬롯 좌표.
- **세이브 호환**: 기존 세이브 그대로 열린다. 옛 배치는 슬롯이 더 **넉넉해질 뿐** 좁아지지 않는다.
  `ItemData.gridWidth/gridHeight`는 `[HideInInspector]`로 남겼다(SO 236개 직렬화 값 보존용, 읽는 코드 0).
- 삭제: 회전 토글·고스트 회전(`CharacterPanelUI`/`GridDragManager`), 수색 연출 전체(약 170줄),
  `GameTuning` 수색 필드 6종, 무기 파츠 3종(조준경·소염기·손잡이)
- **주 — 수색 연출 되살림 (2026-09-11, 사용자 요청)**: 루팅 화면을 정리하면서 사용자가 "원래 있던 그 느낌"을 원해서
  필드 상자·시체의 새 루팅 목록(`LootListUI`)에 되살렸다. 칸이 "? ? ?"로 가려졌다가 희귀도별 시간으로 하나씩 드러난다.
  `GameTuning.searchSpeedMult`·`searchSec*` 6필드도 복귀했다. 캐릭터 패널의 옛 수색 코드는 되살리지 않았다.
  결정: [region-loot.md §루팅 정리 결정](region-loot.md)
- **탄창을 남긴 판단**: 파츠를 전부 없애면 탄창을 못 끼워 **총기가 통째로 죽는다**(총기는 유지 결정).
  수치 미세 보정용 파츠 3종만 잘랐다.
- 상세: [`inventory.md`](inventory.md)

---

## 변경 로그

| 날짜 | 내용 |
|---|---|
| 2026-09-09 | 볼륨 축소 착수. 6개 항목 확정(위 표), 비대상 항목 명시. 1번(부위별 의료) 완료. |
| 2026-09-09 | 2번(소음) 완료. 투척물 유지 결정과 충돌해 `Distraction` 최소 API만 남김. |
| 2026-09-09 | 4번(파견+아르바이트) 완료. 공용 헬퍼는 `OwnedItems`로 이관, `InteractType.Dispatch`는 예약 슬롯 유지. |
| 2026-09-09 | 6번(QA) 완료 — **삭제가 아니라 분리**(`QA_ENABLED` 어셈블리 게이트). 근거는 §6. |
| 2026-09-09 | 5번(특성) 완료 — 41→11. 기준 = "코드가 읽는 effectKey가 있는가". 현상 트리 전멸(기획은 보존). |
| 2026-09-09 | 3번(인벤) 완료 — 격자→슬롯 1칸, 회전·수색 제거, 무게 유지, 무기 파츠 4→1(탄창). **6/6 전부 완료.** |
| 2026-09-11 | 3번에서 뺀 **컨테이너 수색 연출을 되살림**(사용자 요청 — 루팅 목록 `LootListUI`, `searchSpeedMult`·`searchSec*` 복귀). 격자·회전·파츠 폐기는 그대로. [region-loot.md §루팅 정리 결정](region-loot.md) |
