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
| 3 | **격자(테트리스) 인벤토리** | **폐기** → 슬롯형. **무게는 유지** | ⬜ |
| 4 | **파견(Dispatch) + 아르바이트 보드** | **삭제** | ✅ |
| 5 | **특성(퍽) 41종** | **축소** (핵심만 남김) | ⬜ |
| 6 | **QA 자동화** (4,215줄) | **분리/삭제** | ⬜ |

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

---

## 변경 로그

| 날짜 | 내용 |
|---|---|
| 2026-09-09 | 볼륨 축소 착수. 6개 항목 확정(위 표), 비대상 항목 명시. 1번(부위별 의료) 완료. |
| 2026-09-09 | 2번(소음) 완료. 투척물 유지 결정과 충돌해 `Distraction` 최소 API만 남김. |
| 2026-09-09 | 4번(파견+아르바이트) 완료. 공용 헬퍼는 `OwnedItems`로 이관, `InteractType.Dispatch`는 예약 슬롯 유지. |
