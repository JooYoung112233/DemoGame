# 레이드 시스템

## 현재 상태

### 레이드 타이머
- 제한시간: **20분** (1200초)
- HUD: 화면 상단 중앙 `MM:SS` 표시
  - 2분 이하: 주황색 경고
  - 30초 이하: 빨간색 깜빡임
- 시간초과: 아이템 50% 랜덤 손실 후 안전가옥 강제 귀환

### 탈출
- ExitPoint 타입 InteractableObject 사용
- `exitWaitTime > 0` 설정 시 카운트다운 대기 (거리 이탈 시 취소)
- 탈출 성공 시 RaidManager에 기록 → Safehouse 복귀 → RaidResultUI 표시

### 루팅
- LootContainer에 `initialLoot` 배열로 초기 아이템 설정 (Inspector)
  - itemId, count, chance(스폰확률) per entry
- 상자 열면(E키) 내부 아이템 전부 자동으로 PlayerInventory로 이동
- 공간 부족 시 남은 아이템은 상자에 유지
- 모든 아이템 획득 시 IsLooted = true (재상호작용 시 "비어있음" 표시)
- 획득한 아이템은 RaidManager에 추적 기록

### 귀환 정산 (RaidResultUI)
- Safehouse 씬 로드 시 자동 표시
- 실제 데이터 표시:
  - 생존 시간
  - 획득 아이템 목록 (희귀도 색상 포함)
  - 아이템 총 개수/무게
  - 아이템 총 가치 (sellPrice 합산)
- Enter/클릭으로 닫기

---

## 변경 로그

| 날짜 | 내용 |
|---|---|
| 2026-05-25 | 레이드 시스템 v1 구현. RaidManager(타이머+루트추적), LootContainer(initialLoot+자동루팅), RaidResultUI(실데이터 연동), InteractableObject(탈출 시 RaidManager 알림, Pickup 시 루트 추적). |
| 2026-06-02 | **플레이어 사망 → 레이드 실패 처리.** `RaidManager`가 플레이어 `Health.OnDeath` 구독(지연 바인딩). 사망 시: **가방 아이템 전부 손실**(`deathLossRate=1`, 창고는 안전), 붉은 화면 플래시 + 셰이크 연출, 토스트("사망 — 가방을 잃었다"), **안전가옥에서 풀회복 부활**(Health.FullHeal + Medical.HealAll), `deathSpawnPointId="raid_death"`로 강제 귀환. 사망은 정산(PostRaidEvent/RaidResultUI) 스킵. 장착 무기는 유지(가방만 손실) — 추후 조정 가능. | 생존 게임의 긴장("죽으면 가방 다 잃음") 핵심. 시간초과(일부 손실)보다 가혹. |
| 2026-06-06 | **레이드 제한 15→20분** (`RaidManager`/`GameTuning.raidDuration`/InGameScene 직렬화 = 1200). **짙은 현상(밤) 1회 지속 5→≈10분** (`RegionTimeManager`/`GameTuning.nightDuration` = 600). | '밤'을 '짙은 현상' 조우로 재정의(아래 결정 로그). 한 레이드(20분) 안에 현상(10분) 창에서 루디 회수. |
| 2026-06-08 | **S-015_RECOVER 예외** — 첫 짙은 현상 1회: 스크립트 킬 → 레이드 내 부활, **현상 구간 진입 이후 경과 타이머 회수**, 가방 손실 없음 (`watch_time_recovery_tutorial_done`). 이후 일반 사망도 '시간 회수' 연출로 개념 각인. | [story-script.md S-015](story-script.md), [navigation.md §1.3](navigation.md). |

---

## 기획 결정 로그

### 2026-06-06 — '밤' → '짙은 현상' 재정의 + 시간 수치
- **질문**: 레이드 위협을 '밤'으로 볼지, '짙은 현상'이라는 존재로 볼지?
  - **결정**: **'밤'은 통칭일 뿐, 실제 위협 = 짙은 현상.** 회수꾼 브리핑(S-013)도 "밤 출격"이 아니라 "현상을 조심하라"로 변경. 현상은 한낮에도 번질 수 있고, 닿으면 사람이 안 돌아온다. 정체는 후반 다른 개념으로 공개 — 지금은 모호하게.
  - **수치**: 레이드 제한 **20분**, 짙은 현상 1회 지속 **≈10분**(루디 출현·최대 위험 창).
  - ~~**현상 계측기**~~ → **2026-06-08 폐기.** 예고 = **랜턴 점멸**. 상세 → [navigation.md](navigation.md) §4.

### 2026-06-08 — 계측기 → 랜턴 점멸
- **결정**: 현상 근접 예고를 별도 아이템 대신 **착용 랜턴 불빛 깜빡임**으로. MQ-002(랜턴 의뢰)와 통합.
