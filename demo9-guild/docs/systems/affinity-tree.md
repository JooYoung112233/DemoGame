# 시스템: 친화도 트리 (구역별)

> **역할 분리**:
> - **친화도 트리** = 해당 구역에서 전투/파견할 때 적용되는 **전투력 버프** (이 문서)
> - 구역 메커니즘 자체 변경(핏게이지, 열차칸, 저주) = [guild-hall.md](guild-hall.md) F/G/H

## 1. 트리 구조 (공통)

구역마다 동일한 분기 패턴을 쓰지만, 노드 내용은 구역색에 맞춤.

```
Lv1 ── Lv2 ── Lv3 ──┐
                    ├── 분기 A (둘 중 택1) ── Lv5 ── Lv6 ──┐
                    │                                      ├── 분기 B (둘 중 택1) ── Lv8 ──┐
                    │                                                                       ├── 정상 (Lv10, 보스 토큰 1개 소비)
```

| Lv | 종류 | 비고 |
|---|---|---|
| 1 | 자동 | 친화도 XP 누적 시 순차 해금 |
| 2 | 자동 | |
| 3 | 자동 | |
| 4 | **분기 A** | 둘 중 1택 (영구, 재선택 불가) |
| 5 | 자동 | |
| 6 | 자동 | |
| 7 | **분기 B** | 둘 중 1택 (영구) |
| 8 | 자동 | |
| 9 | (예약) | 향후 확장 슬롯 |
| 10 | **정상** | 보스 토큰 1개 소비 (해당 구역 보스 처치 보상) |

**구역당 정의 노드 수**: 11개 (자동 6 + 분기 4 + 정상 1)
**플레이어가 1회차에 획득하는 노드 수**: 9개 (자동 6 + 분기 2 + 정상 1)
**3구역 총합**: 정의 33개, 획득 27개 → 기획 24-30 목표 충족 ✅

## 2. 친화도 XP 커브

```javascript
function affinityXpToNext(currentLevel) {
  const table = [100, 150, 200, 300, 400, 600, 800, 1000, 1500, /* Lv10 = 토큰 */];
  return table[currentLevel - 1];  // Lv1→2 = 100, ...
}
// Lv8 누적: 3,550 XP. 그 이상은 토큰 없으면 진입 불가.
```

파견 1회당 affinityXp ≈ 15-50 (RunSimulator §7). Lv8까지 약 100-200 파견 필요 → 12시간 중반-후반에 도달.

## 3. 보스 토큰

| 토큰 | 획득 | 용도 |
|---|---|---|
| `bp_lord_token` | Blood Pit 보스 1회 처치 | BP 친화도 Lv10 노드 해금 |
| `cargo_admiral_token` | Cargo 보스 1회 처치 | Cargo Lv10 해금 |
| `shadow_lord_token` | Blackout 보스 1회 처치 | BO Lv10 해금 |

보스 토큰은 길드회관 F/G/H 12단계(피니쉬)와는 **별개 자원**. 보스 첫 처치 시 1개만 드롭(이후 재처치 시 추가 드롭은 미정).

## 4. Blood Pit (혈투) 노드 — 11개

| Lv | 키 | 이름 | 효과 |
|---|---|---|---|
| 1 | `pit_gold` | 혈투 호위 | BP 골드 +8% |
| 2 | `pit_smell` | 피 냄새 | BP ATK +5% |
| 3 | `pit_drop` | 약탈 본능 | BP 드롭률 +8% |
| 4A1 | `berserker` | 광전사 | HP 25%↓ 시 BP ATK +25% |
| 4A2 | `executioner` | 처형인 | BP에서 처치 시 다음 공격 100% 크리 |
| 5 | `pit_endurance` | 강철 늑골 | BP HP +8% |
| 6 | `pit_speed` | 핏 본능 | BP 공속 +8% |
| 7B1 | `butcher` | 도살자 | BP 보스에 ATK +15% |
| 7B2 | `pit_master` | 핏 마스터 | BP 라운드 보상 +10% (골드/드롭) |
| 8 | `blood_oath` | 혈의 맹세 | BP 사망률 -25% |
| 10 | `blood_lord` | **🏆 혈의 군주** | BP 한정 ATK +20%, 첫 피격 무효, 핏게이지 시작 50% (`bp_lord_token` 소비) |

## 5. Cargo (운송) 노드 — 11개

| Lv | 키 | 이름 | 효과 |
|---|---|---|---|
| 1 | `cargo_gold` | 운임 정산 | Cargo 골드 +10% |
| 2 | `train_savvy` | 차량 정비 | Cargo 칸 HP +8% |
| 3 | `engineer` | 정비공 | 정차 수리량 +20% |
| 4A1 | `storm_rider` | 폭풍 라이더 | 폭풍 데미지 -30% |
| 4A2 | `track_runner` | 선로 주자 | Cargo 공속 +10% |
| 5 | `cargo_drop` | 화물 약탈 | Cargo 드롭률 +10% |
| 6 | `conductor` | 차장 | 전투당 정차역 +1 (수리 기회 추가) |
| 7B1 | `quartermaster` | 보급관 | 보급 크레이트 등장 2배 |
| 7B2 | `signal_master` | 신호수 | 침입 위치 예고 + 침입 데미지 -20% |
| 8 | `cargo_endurance` | 강철 차체 | Cargo 칸 HP +10% (누적) |
| 10 | `cargo_admiral` | **🏆 화물 제독** | Cargo 보상 +25%, 칸 자동 1회 부활 (`cargo_admiral_token` 소비) |

## 6. Blackout (저주) 노드 — 11개

| Lv | 키 | 이름 | 효과 |
|---|---|---|---|
| 1 | `dark_torch` | 어둠의 횃불 | BO 시야 +1방 |
| 2 | `shadow_step` | 그림자 발걸음 | BO 이동속도 +10% |
| 3 | `occultist` | 비전 학자 | 비밀방 확률 +5% |
| 4A1 | `curse_bearer` | 저주 적응자 | 저주 Lv당 BO ATK +3% (양날의 검 강화) |
| 4A2 | `purifier` | 정화사 | 저주 상승 속도 -25% |
| 5 | `dark_loot` | 어둠의 약탈 | BO 드롭률 +10% |
| 6 | `cartographer` | 지도제작자 | BO 시야 +1방 (누적 2방) |
| 7B1 | `trap_savant` | 함정 통달 | 함정 데미지 -50%, 반사 30% |
| 7B2 | `ghost_speaker` | 영혼 술사 | BO 영혼 이벤트 보상 2배 |
| 8 | `dark_resilience` | 어둠 적응 | BO 사망률 -25%, HP +5% |
| 10 | `shadow_lord` | **🏆 그림자 군주** | BO 한정 ATK +20%, 저주 Lv5↓ 시 양성 효과 (`shadow_lord_token` 소비) |

## 7. 분기 선택 규칙

- 분기 노드(4A/7B)는 친화도가 해당 Lv에 도달했을 때 둘 중 1택 — UI 모달로 선택.
- **선택 후 영구**. 변경하려면 신전 "재각인 의식" (TODO: 비용 길드Lv × 3000G, 쿨다운 미정).
- 분기 선택 전에는 다음 자동 노드(Lv5/Lv8)가 잠긴다.

## 8. UI 흐름

```
AffinityScene
├─ 구역 탭 (BP / Cargo / BO)
├─ 트리 그래프 (해금/대기/잠금 색상)
├─ 노드 호버: 효과 + 잠금 조건
├─ 분기 노드 해금 시 모달: "둘 중 하나를 영구 선택하시오"
└─ 정상 노드: "보스 토큰 1개 소비" 확인
```

## 9. RunSimulator/BattleScene 연동

- `getZoneAffinity(state, zone)` → 누적 노드 키 배열 반환
- 메인 BattleScene 진입 시 ATK/HP/CRIT 등 패시브 적용
- RunSimulator v2 `calcPartyPower` / `calcRewards` 가 affinity Lv를 보조 변수로 사용 (시간 -4%/Lv 등은 §8 참고)
- 보스 토큰은 `gameState.bossTokens = { bp: 0, cargo: 0, blackout: 0 }` 으로 별도 보관

## 10. gameState 발췌

```javascript
affinity: {
  bloodpit:  { xp: 0, level: 0, branch4: null, branch7: null, capstone: false },
  cargo:     { xp: 0, level: 0, branch4: null, branch7: null, capstone: false },
  blackout:  { xp: 0, level: 0, branch4: null, branch7: null, capstone: false }
},
bossTokens: { bp: 0, cargo: 0, blackout: 0 }
```

전체 스키마는 [game-state.md](game-state.md) 참고.
