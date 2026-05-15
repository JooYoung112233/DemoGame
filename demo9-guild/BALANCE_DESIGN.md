# Demo9 — 길드 경영 로그라이크: 밸런스 & 시스템 설계서

> 목표 플레이타임: **6시간** (신규 시작 → 길드 Lv8 + 3구역 보스 클리어)
> 최종 업데이트: 2026-05-15

---

## 목차
1. [플레이타임 타임라인](#1-플레이타임-타임라인)
2. [경제 시스템 — 골드](#2-경제-시스템--골드)
3. [경험치 시스템 — 길드 XP](#3-경험치-시스템--길드-xp)
4. [용병 성장 — 개인 XP & 레벨](#4-용병-성장--개인-xp--레벨)
5. [용병 희귀도 & 모집](#5-용병-희귀도--모집)
6. [전투 밸런스 — 아군 스탯](#6-전투-밸런스--아군-스탯)
7. [전투 밸런스 — 적 스탯 & 보스](#7-전투-밸런스--적-스탯--보스)
8. [구역별 메카닉](#8-구역별-메카닉)
9. [아이템 & 드랍](#9-아이템--드랍)
10. [경매장 경제](#10-경매장-경제)
11. [시설 해금 순서](#11-시설-해금-순서)
12. [친화도 시스템](#12-친화도-시스템)
13. [상인 호감도](#13-상인-호감도)
14. [저주 장비](#14-저주-장비)
15. [카드 시스템 (Blood Pit / Cargo)](#15-카드-시스템)
16. [특성 시스템](#16-특성-시스템)
17. [밸런스 공식 모음](#17-밸런스-공식-모음)

---

## 1. 플레이타임 타임라인

| 시간대 | 런 횟수 | 길드 레벨 | 핵심 이벤트 |
|--------|---------|-----------|------------|
| 0:00–0:30 | 1–3 | 1→2 | 튜토리얼 구간. 모집소에서 2명 고용, Blood Pit Lv1 반복 |
| 0:30–1:00 | 4–7 | 2→3 | 제작소 해금(500G). 장비 제작 시작. BP Lv2 도전 |
| 1:00–1:30 | 8–11 | 3 | 경매장 해금(800G). Cargo 구역 해금. 로스터 6명 확장 |
| 1:30–2:30 | 12–18 | 3→4 | Cargo Lv1–2 공략. 훈련소 해금(1200G). 용병 Lv3–5 |
| 2:30–3:30 | 19–25 | 4→5 | 신전 해금(1500G). Blackout 구역 해금. 스킬 시스템 개방(Lv5) |
| 3:30–4:30 | 26–32 | 5→6 | 정보소 해금(2000G). 3구역 동시 운영. 용병 Lv6–7 |
| 4:30–5:30 | 33–40 | 6→7 | 고급 모집소 해금(3000G). 에픽/전설 용병 등장. 보스전 본격 도전 |
| 5:30–6:00 | 41–45 | 7→8 | 금고 해금(4000G). 3구역 보스 클리어. 엔드게임 |

### 런 1회 예상 소요시간
| 구역 | 라운드 수 (Lv1) | 전투 시간 | 카드/수리 등 | 총 소요 |
|------|----------------|----------|------------|--------|
| Blood Pit | 4+보스 | 3–5분 | 카드선택 1분 | **5–7분** |
| Cargo | 4+보스 | 3–5분 | 수리+카드 2분 | **6–8분** |
| Blackout | 8–12방 | 5–8분 | 탐색+선택 3분 | **8–12분** |

---

## 2. 경제 시스템 — 골드

### 골드 수입원

#### A. 전투 중 적 처치 골드
```
Blood Pit / Cargo (BattleScene):
  일반 적: random(8, 18) + currentRound * 4
  정예: 위 + 20
  보스: 위 + 100
  카드 보너스: golden_hands → ×1.2
  핏게이지 MAX: pitDropMult 적용

Blackout (방 전투):
  일반 적: random(8, 18) + depth * 4 + currentFloor * 5
  보스전: random(15, 30)
```

#### B. 라운드 클리어 보너스
```
roundBonus = 15 + currentRound * 10 + (isBoss ? 50 : 0)
```

#### C. 구역별 추가 보상
```
Cargo 화물칸 생존: 50 + cargoHp * 1.5 ~ 2.0 (화물HP 100% 기준 250G)
Cargo 전차 파괴: totalGold × 0.5 (50% 감소 페널티)
Blackout 비밀방: goldDrop = random(30, 60) + currentFloor * 20
Blackout 보물방: 아이템 + 골드
```

#### D. 아이템 판매
```
보관함 직접 판매: item.value × 0.7
경매장 판매: item.value × (0.9–1.3) × marketModifier
위탁 판매: 런 중 자동, 성공 확률 = 0.4 + 상인호감도 × 0.05
```

### 골드 지출처

| 항목 | 비용 | 빈도 |
|------|------|------|
| 용병 고용 (common) | 80G | 매 런 전후 |
| 용병 고용 (uncommon) | 120G | |
| 용병 고용 (rare) | 200G | |
| 용병 고용 (epic) | 320G | |
| 용병 고용 (legendary) | 480G | |
| 시설 해금 | 500–4000G | 1회성 |
| 경매장 구매 | 가변 | 수시 |
| 경매 갱신 | 80 + guildLv × 20 | 수시 |
| 카드 리롤 | rerollCount × 150 | 전투 중 |
| Cargo 칸 수리 | 30 + round × 10 | 전투 중 |
| Blackout 정화 | 50G | 전투 중 |
| 저주 장비 gold_drain | 50G/전투 | 장착 중 |

### 골드 흐름 시뮬레이션 (런 1회 평균)

| 구간 | Blood Pit Lv1 | Cargo Lv1 | Blackout Lv1 |
|------|--------------|-----------|-------------|
| 적 처치 골드 | ~80G | ~90G | ~100G |
| 라운드 보너스 | ~100G | ~110G | — |
| 구역 보너스 | — | ~200G (화물) | ~80G (비밀방) |
| 기본 보상 | 60G (base) | 80G (base) | 100G (base) |
| **런 1회 총 수입** | **~240G** | **~480G** | **~280G** |
| 지출 (고용+장비) | ~100G | ~100G | ~100G |
| **순수입** | **~140G** | **~380G** | **~180G** |

### 시설 해금 골드 요구량 (누적)
```
Lv1: 시작 (무료 3시설)
Lv2: 제작소 500G  → 누적 500G → 런 ~3회
Lv3: 경매장 800G  → 누적 1,300G → 런 ~7회
Lv4: 훈련소 1,200G → 누적 2,500G → 런 ~14회
Lv5: 신전 1,500G  → 누적 4,000G → 런 ~21회
Lv6: 정보소 2,000G → 누적 6,000G → 런 ~28회
Lv7: 고급모집 3,000G → 누적 9,000G → 런 ~35회
Lv8: 금고 4,000G  → 누적 13,000G → 런 ~42회
```

---

## 3. 경험치 시스템 — 길드 XP

### 길드 레벨 테이블
```javascript
GUILD_LEVEL_XP = [0, 100, 250, 500, 800, 1200, 1800, 2500]
// index = 현재 레벨 → 다음 레벨 필요 XP
// Lv1→2: 100, Lv2→3: 250, Lv3→4: 500, Lv4→5: 800
// Lv5→6: 1200, Lv6→7: 1800, Lv7→8: 2500
// 총 필요 XP: 7,150
```

### 길드 XP 획득원
```
런 완료 시: result.xpEarned (런 중 누적된 totalXp)
  - 라운드 클리어: 10 + currentRound * 5 + (isBoss ? 30 : 0)
  - 시너지 보너스: roundXp × (1 + synergyXpBonus)
  - Blackout: 5 + depth * 2 + currentFloor * 3 (적 처치마다)

RunResultScene에서 GuildManager.addXp(gs, r.xpEarned)
```

### 런당 평균 길드 XP
| 구역 | Lv1 (4라운드) | Lv3 (7라운드) | Lv5 (10라운드) |
|------|-------------|-------------|--------------|
| Blood Pit | ~80 XP | ~160 XP | ~250 XP |
| Cargo | ~85 XP | ~170 XP | ~260 XP |
| Blackout | ~70 XP | ~140 XP | ~220 XP |

### 레벨업 속도 시뮬레이션
```
Lv1→2: 100 XP ÷ 80/런 = ~2런 (0:15)
Lv2→3: 250 XP ÷ 85/런 = ~3런 (0:35)
Lv3→4: 500 XP ÷ 100/런 = ~5런 (1:15)
Lv4→5: 800 XP ÷ 120/런 = ~7런 (2:15)
Lv5→6: 1200 XP ÷ 160/런 = ~8런 (3:15)
Lv6→7: 1800 XP ÷ 200/런 = ~9런 (4:30)
Lv7→8: 2500 XP ÷ 240/런 = ~11런 (6:00)
총: ~45런, ~6시간
```

---

## 4. 용병 성장 — 개인 XP & 레벨

### 레벨 테이블
```javascript
getXpToNextLevel() = 40 + level * 20
// Lv1→2: 60, Lv2→3: 80, Lv3→4: 100, Lv4→5: 120
// Lv5→6: 140, Lv6→7: 160, Lv7→8: 180, Lv8→9: 200, Lv9→10: 220
// 총 1→10: 1,260 XP
// 최대 레벨: 10
```

### 용병 XP 획득 (RunResultScene)
```javascript
xpGain = Math.floor(r.xpEarned * 0.5 + r.rounds * 5 + bossBonus)
// bossBonus = r.success ? 15 : 0
// 생존자만 XP 획득
```

### 런당 평균 용병 XP
```
Blood Pit Lv1 성공: floor(80 * 0.5 + 4 * 5 + 15) = 75 XP
Cargo Lv1 성공: floor(85 * 0.5 + 4 * 5 + 15) = 77 XP
Blackout Lv1 성공: floor(70 * 0.5 + 8 * 5 + 15) = 90 XP
```

### 레벨업 속도
```
Lv1→2: 60 XP ÷ 75/런 = 1런
Lv1→5: 360 XP ÷ 75/런 = ~5런 (스킬 해금)
Lv1→10: 1260 XP ÷ ~85/런 (평균) = ~15런
```

### 스탯 성장 (레벨당)
```javascript
stats.hp  = (baseHp  + growthHp  * (level-1) * growthMult) * rarityMult
stats.atk = (baseAtk + growthAtk * (level-1) * growthMult) * rarityMult
stats.def = (baseDef + growthDef * (level-1) * growthMult) * rarityMult
```

| 클래스 | Lv1 HP/ATK/DEF | Lv5 HP/ATK/DEF | Lv10 HP/ATK/DEF | 성장률(HP/ATK/DEF) |
|--------|---------------|---------------|----------------|------------------|
| 전사 | 250/20/18 | 370/28/26 | 520/38/36 | 30/2/2 |
| 도적 | 160/35/8 | 220/51/12 | 295/71/17 | 15/4/1 |
| 마법사 | 120/45/5 | 168/65/5 | 228/90/5 | 12/5/0 |
| 궁수 | 150/30/7 | 206/42/11 | 276/57/16 | 14/3/1 |
| 사제 | 180/12/10 | 260/16/14 | 360/21/19 | 20/1/1 |
| 연금술사 | 170/15/12 | 242/23/16 | 332/33/21 | 18/2/1 |

> 위 표는 common (1.0x) 기준. 희귀도별 statMult/growthMult 적용됨.

---

## 5. 용병 희귀도 & 모집

### 희귀도 스탯 배율
| 희귀도 | statMult | growthMult | 긍정특성 | 부정특성 | 고용비 |
|--------|----------|-----------|---------|---------|-------|
| common | 1.0 | 1.0 | 1 | 1 (50%) | 80G |
| uncommon | 1.1 | 1.1 | 1 | 0.5 (50%→1) | 120G |
| rare | 1.2 | 1.2 | 2 | 0.5 | 200G |
| epic | 1.35 | 1.3 | 2 | 0 | 320G |
| legendary | 1.5 | 1.5 | 2 | 0 | 480G |

> legendary는 클래스 전설 특성 추가 부여

### 모집 풀 희귀도 확률 (RARITY_POOL)
| 길드 레벨 | common | uncommon | rare | epic | legendary |
|-----------|--------|----------|------|------|-----------|
| 1–2 | 60% | 35% | 5% | 0% | 0% |
| 3–4 | 40% | 40% | 18% | 2% | 0% |
| 5–6 | 25% | 35% | 30% | 9% | 1% |
| 7–8 | 15% | 30% | 30% | 20% | 5% |

### 모집 풀 크기
```
count = 3 + random(0, 2)  →  3~5명
```

---

## 6. 전투 밸런스 — 아군 스탯

### 클래스 기본 스탯 (Lv1 Common)
| 클래스 | HP | ATK | DEF | 공속(ms) | 사거리 | 이동속도 | 크리율 | 크리배 |
|--------|-----|-----|-----|---------|-------|---------|-------|-------|
| 전사 | 250 | 20 | 18 | 1800 | 55 | 90 | 5% | 1.5x |
| 도적 | 160 | 35 | 8 | 1000 | 55 | 140 | 25% | 2.0x |
| 마법사 | 120 | 45 | 5 | 2200 | 280 | 50 | 10% | 1.8x |
| 궁수 | 150 | 30 | 7 | 1400 | 250 | 80 | 18% | 1.8x |
| 사제 | 180 | 12 | 10 | 2000 | 200 | 60 | 5% | 1.3x |
| 연금술사 | 170 | 15 | 12 | 1800 | 180 | 60 | 8% | 1.4x |

### 역할 분류
- **tank**: 전사 — 높은 HP/DEF, 방패막이 스킬
- **melee_dps**: 도적 — 높은 크리율, 급소찌르기 (크리확정+출혈)
- **ranged_dps**: 마법사/궁수 — 긴 사거리, 범위/관통 스킬
- **healer**: 사제 — 전체 20% 회복 스킬
- **support**: 연금술사 — 전체 ATK +20% 버프 스킬

### 스킬 해금: 레벨 5

### 파티 구성 제한 (ROSTER_LIMITS)
| 길드 레벨 | 최대 로스터 | 최대 파견 |
|-----------|-----------|----------|
| 1–2 | 4 | 2 |
| 3–4 | 6 | 3 |
| 5–6 | 8 | 4 |
| 7–8 | 10 | 5 |

---

## 7. 전투 밸런스 — 적 스탯 & 보스

### 일반 적 (Blood Pit)
| 타입 | HP | ATK | DEF | 공속 | 사거리 | 이동속도 | 특수 |
|------|-----|-----|-----|------|-------|---------|------|
| 러너 | 70 | 10 | 2 | 1300 | 50 | 110 | — |
| 브루저 | 220 | 20 | 10 | 2200 | 55 | 50 | — |
| 스피터 | 60 | 14 | 2 | 1600 | 250 | 60 | 출혈15% |
| 소환사 | 90 | 8 | 3 | 2000 | 200 | 40 | — |
| 정예 러너 | 280 | 25 | 7 | 1000 | 50 | 130 | isElite |
| 정예 브루저 | 600 | 38 | 16 | 2000 | 55 | 55 | isElite |

### 일반 적 (Cargo)
| 타입 | HP | ATK | DEF | 공속 | 사거리 | 이동속도 | 특수 |
|------|-----|-----|-----|------|-------|---------|------|
| 기계 졸병 | 90 | 12 | 6 | 1500 | 50 | 80 | — |
| 자동 포탑 | 120 | 18 | 12 | 1800 | 300 | 0 | 고정 |
| 방패병 | 300 | 10 | 20 | 2200 | 50 | 45 | — |
| 폭파병 | 50 | 30 | 1 | 2500 | 200 | 90 | 크리20% |
| 정예 방패병 | 650 | 22 | 28 | 2000 | 55 | 50 | isElite |
| 정예 포탑 | 350 | 35 | 18 | 1400 | 350 | 0 | isElite |

### 일반 적 (Blackout)
| 타입 | HP | ATK | DEF | 공속 | 사거리 | 이동속도 | 특수 |
|------|-----|-----|-----|------|-------|---------|------|
| 레이스 | 55 | 16 | 1 | 1000 | 50 | 140 | 크리15% |
| 저주술사 | 70 | 22 | 3 | 2000 | 280 | 50 | 출혈25% |
| 뼈 골렘 | 400 | 18 | 8 | 2500 | 55 | 35 | — |
| 그림자 | 40 | 25 | 0 | 800 | 50 | 160 | 크리25% |
| 정예 레이스 | 350 | 35 | 5 | 900 | 50 | 150 | isElite |
| 정예 저주술사 | 250 | 42 | 8 | 1600 | 300 | 55 | isElite |
| 저주 화신 | 800 | 45 | 12 | 1100 | 55 | 120 | isElite |

### 보스 스탯 (베이스)
| 보스 | HP | ATK | DEF | 공속 | 사거리 | 크리 | 특수 |
|------|-----|-----|-----|------|-------|------|------|
| 핏로드 | 1500 | 45 | 15 | 1800 | 60 | 15%/2.0x | — |
| 아이언클래드 | 1800 | 35 | 25 | 2200 | 60 | 10%/1.8x | — |
| 리치 킹 | 1200 | 50 | 10 | 1600 | 250 | 20%/2.5x | 출혈30% |
| 저택 주인 | 2000 | 55 | 18 | 1800 | 60 | 18%/2.2x | 출혈20% |

### 보스 스케일링 — getBossScaleMult(zoneLevel)
```javascript
// Blood Pit, Cargo 공통 (BattleScene, CargoBattleScene)
bossScale = scaleMult * getBossScaleMult(zoneLevel)

getBossScaleMult: { 1: 0.5, 2: 0.65, 3: 0.8, 4: 0.9, 5+: 1.0+ }

// scaleMult = 1 + (zoneLevel - 1) * 0.1
// 실효 보스 배율:
//   Lv1: 1.0 × 0.5 = 0.50  → 핏로드 HP 750, ATK 22
//   Lv2: 1.1 × 0.65 = 0.715 → 핏로드 HP 1072, ATK 32
//   Lv3: 1.2 × 0.8 = 0.96  → 핏로드 HP 1440, ATK 43
//   Lv5: 1.4 × 1.0 = 1.40  → 핏로드 HP 2100, ATK 63

// Blackout (BlackoutBattleScene) — 층 기반
bossScale = 0.4 + currentFloor * 0.2 + curseLevel * 0.05
//   Floor 1, Curse 0: 0.6 → 저택주인 HP 1200, ATK 33
//   Floor 2, Curse 3: 0.95 → 저택주인 HP 1900, ATK 52
//   Floor 3, Curse 5: 1.25 → 저택주인 HP 2500, ATK 68
```

### 보스 라운드 호위병 (구역 레벨별)
| 구역 레벨 | Blood Pit 호위 | Cargo 호위 | Blackout 호위 |
|-----------|--------------|-----------|-------------|
| Lv1 | 러너 × 2 | 기계졸병 × 2 | 레이스 × 2 |
| Lv2 | 러너 × 3 | 방패병 + 포탑 | 레이스2 + 저주술사 |
| Lv3+ | 정예러너 + 러너2 | 정예방패 + 포탑2 | 정예레이스 + 저주술사2 |

### 적 스케일링 공식
```javascript
scaleMult = 1 + (zoneLevel - 1) * 0.1
// Lv1: 1.0, Lv2: 1.1, Lv3: 1.2, Lv5: 1.4, Lv10: 1.9

// Blood Pit 추가 효과 (라운드 진행):
enemies.forEach(e => {
    e.atk *= (1 + progress * 0.15);   // 최대 +15% ATK
    e.critRate += progress * 0.05;     // 최대 +5% CRIT
});

// Blackout 저주 레벨 효과:
curseMult = 1 + curseLevel * 0.08     // 적 ATK/HP 배율
```

### 적 편성 (라운드별 진행)
```
진행도 0–25%: 기본 몹 2–3마리
진행도 25–50%: 기본 + 원거리 혼합
진행도 50–75%: 탱커 + 원거리 + 기본 (구역Lv2+: 정예 추가)
진행도 75–100%: 탱커 다수 + 정예 (구역Lv2+: 정예 2종)
보스 라운드: 보스 + 호위병
```

---

## 8. 구역별 메카닉

### Blood Pit — 핏 게이지
```
- 적 처치 시 게이지 상승
- MAX 도달: 드랍률 증가, 위험도 증가
- 라운드 간 자연 감소
- 구역 레벨 효과:
  Lv5: arena_shrink — 전장 -20%, 시간제한 45초
  Lv7: crowd_pressure — 미스 ATK -3%, 킬 ATK +5%
  Lv10: pitlord_rage — 감소속도 2배, MAX시 ATK +50%
```

### Cargo — 열차 방어
```
- 5칸 열차: deck/hold/engine/infirmary/bridge
- 각 칸 HP 보유, 파괴 가능
- 역 정차 시 칸 수리/카드 획득
- 침입자: 직접 전투 없이 칸에 침투 → 칸 HP 감소
- 구역 레벨 효과:
  Lv5: train_speed — 시간제한 50초, 정차보너스 +50%
  Lv7: storm_zone — 10초마다 전체 피해, 칸 HP 감소
  Lv10: final_cargo — 폭발 위협, 방어성공 보상 3배
```

### Blackout — 저주 저택 탐색
```
- 그리드 기반 방 탐색 (5×4 ~ 6×5)
- 방 타입: 전투/함정/보물/상점/비밀/단서/빈방/보스
- 저주 레벨: 이벤트마다 상승, 적 강화/드랍 향상 트레이드오프
- 3층 구조, 층마다 보스 가능
- 구역 레벨 효과:
  Lv5: full_dark — 시야 -1칸, 저주속도 +50%
  Lv7: maze_shift — 탐색방 재배치
  Lv10: mansion_rage — 적 +30%, 보물 등급 +2
```

---

## 9. 아이템 & 드랍

### 아이템 타입 분포 (generateItem)
```
35% 장비 (weapon/armor/accessory)
30% 소재 (혈정석, 동력석, 저주유물, 황금모래, 철광석, 마법가루)
35% 소비 (HP포션, 전투물약, 방어물약, 화염폭탄)
```

### 아이템 희귀도 결정
```javascript
levelBonus = guildLevel * 3 + rarityBonus * 15
roll = random(0, 100)

epic:     roll < 5 + levelBonus * 0.5
rare:     roll < 15 + levelBonus
uncommon: roll < 40 + levelBonus * 0.5
common:   나머지
```

| 길드 레벨 | common | uncommon | rare | epic |
|-----------|--------|----------|------|------|
| 1 (bonus 0) | 60% | 25% | 10% | 5% |
| 3 (bonus 0) | 47% | 27% | 18% | 8% |
| 5 (bonus 0) | 35% | 30% | 23% | 12% |
| 5 (bonus 3) | 5% | 15% | 33% | 47% |
| 8 (bonus 0) | 21% | 31% | 27% | 21% |

### 아이템 가치 (ITEM_RARITY.valueMult)
| 희귀도 | valueMult | 장비 기준가 | 소재 기준가 | 소비 기준가 |
|--------|-----------|-----------|-----------|-----------|
| common | 1.0 | 30G | 20–50G | 20–40G |
| uncommon | 1.5 | 45G | 30–75G | 30–60G |
| rare | 2.5 | 75G | 50–125G | 50–100G |
| epic | 5.0 | 150G | 100–250G | 100–200G |
| legendary | 10.0 | 300G | 200–500G | 200–400G |

### 장비 스탯 (rarityMult 적용)
| 슬롯 | 템플릿 | baseAtk | baseDef | baseHp | 기타 |
|------|--------|---------|---------|--------|------|
| weapon | 낡은 검 | 5 | — | — | |
| weapon | 강철 검 | 10 | — | — | |
| weapon | 마법 지팡이 | 12 | — | — | |
| weapon | 사냥꾼의 활 | 8 | — | — | |
| weapon | 의식용 단검 | 15 | — | — | |
| armor | 가죽 갑옷 | — | 5 | 20 | |
| armor | 사슬 갑옷 | — | 10 | 40 | |
| armor | 마법사 로브 | 3 | 3 | 15 | |
| armor | 판금 갑옷 | — | 15 | 60 | |
| accessory | 행운의 반지 | — | — | — | critRate +0.05 |
| accessory | 힘의 목걸이 | 5 | — | — | |
| accessory | 수호의 부적 | — | 5 | 30 | |
| accessory | 속도의 장갑 | — | — | — | moveSpeed +15 |

### 런당 드랍 아이템 수
```
Blood Pit: lootCount min:1, max:3
Cargo:     lootCount min:2, max:4
Blackout:  lootCount min:1, max:5 (+ 방 탐색 보상 별도)
```

### 드랍 조건 (BattleScene 적 처치)
```javascript
dropChance = 0.20 + (unit.isElite ? 0.30 : 0) + (unit.isBoss ? 0.80 : 0)
// 카드 'lucky' 보유 시 rarityBonus += 1
// 핏게이지 MAX 시 rarityBonus += 1
// Blackout 저주 레벨 반영: curseRarityBonus
```

---

## 10. 경매장 경제

### 구매 탭
```
매물 수: 6 + random(0, 3)  →  6~9개
가격: item.value × (1.2 + random(0, 0.8)) × marketModifier
핫딜 (15%): 가격 × 0.6
대량 (10%): 개수 2–4, 가격 × 0.85/개
갱신 비용: 80 + guildLevel × 20
```

### 판매 탭
```
판매가: item.value × 0.7 (보관함 직접 판매)
경매 판매: item.value × (0.9–1.3) × marketModifier
```

### 위탁 탭
```
런 중 자동 판매 시도
성공 확률: 0.4 + merchantFavor[gold] × 0.05
판매가: item.value × (0.8–1.2)
최대 위탁 슬롯: gs.autoAuctionSlots (기본 2)
```

### 입찰 탭
```
매물 수: 2 + random(0, 1)  →  2~3개
시작 입찰: item.value × 0.5
최소 증액: startBid × 0.15 (최소 10G)
NPC 입찰자: 1–3명
라운드: 2–3회
```

### 시장 동향 시스템
```
3이벤트마다 랜덤 drift: ±0.1
범위: 0.5 ~ 1.6
상태: surplus (≤0.7), normal, shortage (≥1.3)

이벤트별 영향:
  run_success: material -0.05, equipment +0.03
  run_fail: consumable +0.08, equipment -0.03
  bulk_sell: material -0.08
  bulk_buy: equipment +0.05
```

---

## 11. 시설 해금 순서

| 시설 | 해금 레벨 | 비용 | 해금 효과 |
|------|----------|------|----------|
| 용병 모집소 | 1 | 무료 | 3–5명 모집 풀 |
| 보관함 | 1 | 무료 | 12칸 저장 |
| 출발 게이트 | 1 | 무료 | Blood Pit 접근 |
| 장비 제작소 | 2 | 500G | 소재→장비 제작 |
| 경매장 | 3 | 800G | 매매+위탁+입찰, **Cargo 구역 해금** |
| 훈련소 | 4 | 1200G | 글로벌 훈련 (HP/ATK/생존/회복) |
| 신전 | 5 | 1500G | 치유+축복, **Blackout 구역 해금** |
| 정보소 | 6 | 2000G | 구역 정보+의뢰 |
| 고급 모집소 | 7 | 3000G | 높은 희귀도 모집 풀 |
| 비밀 금고 | 8 | 4000G | 보관함 20칸, 보안컨테이너 +2 |

### 훈련소 — 글로벌 버프
```javascript
// 훈련 포인트: 길드 레벨업 시 +1 (최대 7포인트)
// 각 카테고리 최대 레벨 없음 (투자량 비례)
stats.hp  *= (1 + training.hp * 0.03)        // HP +3%/포인트
stats.atk *= (1 + training.atk * 0.03)       // ATK +3%/포인트
stats.def += training.survival * 2            // DEF +2/포인트
stats.skillCooldown *= (1 - training.recovery * 0.03)  // 쿨 -3%/포인트
```

---

## 12. 친화도 시스템

### 친화도 XP 획득 (RunResultScene)
```javascript
affinityGain = Math.floor(10 + r.rounds * 3 + (r.success ? 10 : 0))
// Blood Pit 4라운드 성공: 10 + 12 + 10 = 32
// Cargo 4라운드 성공: 32
// Blackout 8방 성공: 10 + 24 + 10 = 44
```

### 친화도 레벨 테이블
```javascript
getAffinityXpNeeded(level) = 30 + level * 20
// Lv0→1: 30, Lv1→2: 50, Lv2→3: 70, Lv3→4: 90, Lv4→5: 110
// 총 0→5: 350 XP → ~11런 (성공 기준)
// 최대 레벨: 5
```

### 친화도 트리 구조
각 구역 8–10개 노드, 2–3개 분기. 레벨업 시 포인트 획득, 노드에 투자.

**Blood Pit 트리**: 핏 게이지 운영 vs 순수 전투력
- A분기: 핏게이지 파밍 (드랍 3배) → 보스약점 → 전체 +8% + 게이지감소무효
- B분기: 전투력 (ATK +15%) → 전설드랍 +10% → 처치 ATK중첩

**Cargo 트리**: 3분기 (운영/전투/특수)
- A분기: 칸 패시브 +30% → 무상수리 → 패시브 2배
- B분기: 침입예측 → 카드한도 +2 → 적 ATK -15%
- C분기: 파괴칸 수리 → 추가정차 → 칸 1회 부활

**Blackout 트리**: 안전 탐색 vs 저주 활용
- A분기: 재방문 무전투 → 방미리공개/기습무효 → 완전면역
- B분기: 저주ATK +5% → 저주Lv5 +20%/저주드랍상승 → 저주 버프전환

---

## 13. 상인 호감도

### 상인 목록
| 상인 | 아이콘 | 역할 |
|------|--------|------|
| 크로그 (Krog) | ⚔ | 무기상 |
| 스피드카 (Speedka) | 🔍 | 정보상 |
| 아이언 (Iron) | 🔨 | 대장장이 |
| 골든 (Gold) | 🏛 | 경매사 |

### 호감도 티어
| 상인 | Tier1 (0) | Tier2 (3) | Tier3 (6) | Tier4 (10) |
|------|-----------|-----------|-----------|------------|
| 크로그 | — | 무기 -10% | 희귀무기 등장 | 전설무기 확정 |
| 스피드카 | — | 정보 -15% | 비밀구역 해금 | 보스약점 공개 |
| 아이언 | — | 수리 -20% | 장비 강화 개방 | 전설 제작 |
| 골든 | — | 판매 +5% | 위탁 확률 +15% | 독점 매물 |

### 호감도 획득 (favorActions)
```
buy_weapon: krog +1
buy_material: iron +1
buy_consumable: speedka +1
sell_item: gold +1
run_success: gold +0.5
bid_win: gold +1, speedka +0.5
consign_success: gold +1
forge_item: iron +1.5
temple_use: (없음, 향후 추가 가능)
```

---

## 14. 저주 장비

### 저주 장비 목록
| 이름 | 슬롯 | 희귀도 | 스탯 | 저주 효과 |
|------|------|--------|------|----------|
| 저주받은 대검 | weapon | epic | ATK+35, DEF-5 | 전투 시작 HP 5% 손실 |
| 원혼의 갑옷 | armor | epic | DEF+25, HP+80 | 이동속도 -20% |
| 탐욕의 반지 | accessory | epic | ATK+15, CRIT+10% | 전투마다 50G 소모 |
| 광기의 투구 | armor | legendary | ATK+20, DEF+30, HP+50 | HP 30%↓ 아군 공격 |
| 피의 단검 | weapon | rare | ATK+20, CRIT+8% | 매 공격 HP 2% 손실 |
| 그림자 목걸이 | accessory | rare | SPD+30, ATK+10 | 받는 피해 +15% |

### 저주 장비 규칙
- **해제 불가**: equip/unequip 모두 null 반환
- **드랍 조건**: Blackout 적 처치 시 `cursedChance = 0.08 + curseLevel * 0.04`
- **전투 효과**: BattleUnit에서 장비 읽어서 자동 적용
- **판매 가치**: ITEM_RARITY[rarity].valueMult × 80

---

## 15. 카드 시스템

### Blood Pit 카드 (CARD_DATA)
- **Tier 1** (항상): ATK+5%, HP+8%, DEF+3, SPD+8%, CRIT+3%, HP15%회복
- **Tier 2** (라운드3+ or 구역Lv2+): ATK+8%, HP+12%, DEF+5, SPD+12%, CRIT+5%, 크리피해+15%, 흡혈, HP25%회복, 골드+20%
- **Tier 3** (라운드5+ AND 구역Lv3+): ATK+12%, HP+15%, 올스탯+5%, CRIT+8%+크리피해+20%, 최후의저항, HP40%회복, 드랍등급+1

### Cargo 카드 (CARGO_CAR_CARDS)
칸별 전용 카드 + 범용 카드. 카드 보유 한도: 5장 (친화도로 +2 가능).

### 카드 선택
- 라운드 클리어 후 3장 중 1장 선택
- 리롤: 첫 무료, 이후 rerollCount × 150G

---

## 16. 특성 시스템

### 긍정 특성 (20종)
| ID | 이름 | 효과 |
|----|------|------|
| strong_body | 강인한 체격 | HP +15% |
| sharp_sense | 날카로운 감각 | CRIT +10% |
| battle_instinct | 전투 본능 | ATK +10% |
| iron_wall | 철벽 수비 | DEF +15% |
| swift_feet | 신속한 발놀림 | SPD +15% |
| quick_recovery | 빠른 회복 | 전투 후 HP 5% |
| blood_fighter | 피의 투사 | BP ATK +20% |
| arena_hero | 투기장 영웅 | 처치시 HP 3% |
| cargo_expert | 화물 전문가 | Cargo DEF +15% |
| guard_instinct | 수호 본능 | Cargo 위기 이동 |
| dark_friend | 어둠의 친구 | BO CRIT +15% |
| curse_resist | 저주 내성 | 저주 -30% |
| revenge | 복수의 화신 | 아군사망 ATK +10% |
| crisis_fighter | 위기의 투사 | HP30%↓ ATK/CRIT↑ |
| cool_head | 냉철한 판단 | 보스전 페널티 무효 |
| veteran | 노련한 생존자 | 런 HP +10% |
| fast_hands | 빠른 손 | 스킬 쿨 -15% |
| comrade_love | 동료 사랑 | 힐 +5% |
| loot_hunter | 전리품 사냥꾼 | 드랍 +10% |
| negotiator | 협상가 | 귀환 골드 +5% |

### 부정 특성 (15종)
| ID | 이름 | 효과 |
|----|------|------|
| weak_body | 허약한 체질 | HP -10% |
| dull_sense | 둔한 감각 | CRIT -10% |
| slow_feet | 느린 발 | SPD -15% |
| dull_attack | 무딘 공격 | ATK -8% |
| thin_armor | 얇은 갑옷 | DEF -10% |
| blood_phobia | 피 공포증 | BP ATK -15% |
| claustrophobia | 폐쇄 공포증 | BO SPD -20% |
| no_tech | 기계치 | Cargo DEF보너스 없음 |
| coward | 겁쟁이 | 아군사망 스탯 -10% |
| greedy | 욕심쟁이 | 보안컨테이너 -1 |
| stubborn | 고집쟁이 | 시너지 -5% |
| scarred | 상처투성이 | 런 HP -10% |
| unlucky | 불운아 | 드랍 희귀도 -1 |
| lone_wolf | 단독 행동 | 3명+ ATK -5% |
| slow_learner | 더딘 성장 | XP -10% |

### 전설 특성 (클래스별, legendary 등급만)
| 클래스 | 이름 | 효과 |
|--------|------|------|
| 전사 | 불굴의 방패 | HP20%↓ 사망→HP1 (런1회) |
| 도적 | 그림자 발걸음 | 첫 공격 크리+출혈 확정 |
| 마법사 | 마력 과부하 | 스킬 추가 폭발, 쿨+5초 |
| 궁수 | 매의 눈 | 사거리+30%, 크리시 2연타 |
| 사제 | 성스러운 희생 | 아군 사망직전 즉시 힐 |
| 연금술사 | 비약의 대가 | 물약효과+50%, 지속2배 |

---

## 17. 밸런스 공식 모음

### 핵심 공식 참조표

```javascript
// ─── 전투 대미지 ───
damage = Math.max(1, atk - target.def * 0.5)
critDamage = damage * critDmg  // if random() < critRate

// ─── 보스 스케일링 ───
getBossScaleMult(zoneLevel):
  { 1: 0.5, 2: 0.65, 3: 0.8, 4: 0.9, 5+: 1.0 + (zoneLevel-5)*0.1 }

bossScale_standard = (1 + (zoneLevel-1)*0.1) * getBossScaleMult(zoneLevel)
bossScale_blackout = 0.4 + currentFloor * 0.2 + curseLevel * 0.05

// ─── 적 편성 스케일 ───
scaleMult = 1 + (zoneLevel - 1) * 0.1

// ─── 최대 라운드 ───
maxRounds = min(10, 4 + floor((zoneLevel-1) * 1.5))
// Lv1: 4, Lv2: 5, Lv3: 7, Lv4: 8, Lv5: 10

// ─── 길드 XP ───
GUILD_LEVEL_XP = [0, 100, 250, 500, 800, 1200, 1800, 2500]
// 총 필요: 7,150 XP

// ─── 용병 XP ───
xpToNextLevel = 40 + level * 20  // 총 1→10: 1,260
mercXpGain = floor(guildXp * 0.5 + rounds * 5 + (success ? 15 : 0))

// ─── 친화도 XP ───
affinityXpNeeded = 30 + affinityLevel * 20  // 총 0→5: 350
affinityGain = floor(10 + rounds * 3 + (success ? 10 : 0))

// ─── 골드 ───
enemyGold = random(8, 18) + round * 4 + (isElite ? 20 : 0) + (isBoss ? 100 : 0)
roundBonus = 15 + round * 10 + (isBoss ? 50 : 0)
hireCost = 80 * RARITY_DATA[rarity].hireCostMult
sellValue = item.value * 0.7

// ─── 아이템 희귀도 ───
levelBonus = guildLevel * 3 + rarityBonus * 15
epic:     roll < 5 + levelBonus * 0.5
rare:     roll < 15 + levelBonus
uncommon: roll < 40 + levelBonus * 0.5
common:   나머지

// ─── 저주 장비 드랍 ───
cursedChance = 0.08 + curseLevel * 0.04

// ─── 훈련 효과 ───
hp  *= (1 + training.hp * 0.03)
atk *= (1 + training.atk * 0.03)
def += training.survival * 2
skillCooldown *= (1 - training.recovery * 0.03)

// ─── 시장 동향 ───
modifier 범위: 0.5 ~ 1.6
drift: 3이벤트마다 ±0.1 랜덤

// ─── 보관함 ───
기본: 12칸 → 금고: 20칸
보안컨테이너: 기본 2 + training.survival + (금고 ? 2 : 0)
```

---

## 파일 맵 (코드 위치 참조)

| 시스템 | 파일 | 핵심 함수/상수 |
|--------|------|-------------|
| 클래스 스탯 | `src/data/units.js` | CLASS_DATA, RARITY_DATA, RARITY_POOL |
| 적 스탯 | `src/data/enemies.js` | ENEMY_DATA, getEnemyComposition, getBossScaleMult |
| 시설 | `src/data/facilities.js` | FACILITY_DATA, GUILD_LEVEL_XP, ROSTER_LIMITS |
| 아이템 | `src/data/items.js` | generateItem, ITEM_RARITY, EQUIPMENT_TEMPLATES, CURSED_EQUIPMENT |
| 구역 | `src/data/zones.js` | ZONE_DATA, ZONE_LEVEL_EFFECTS, AFFINITY_TREES |
| 특성 | `src/data/traits.js` | POSITIVE_TRAITS, NEGATIVE_TRAITS, LEGENDARY_TRAITS |
| 카드 | `src/data/cards.js` | CARD_DATA, CARGO_CAR_CARDS |
| 상인 | `src/data/merchants.js` | MERCHANT_DATA, processMerchantAction |
| 용병 | `src/entities/Mercenary.js` | getStats, gainXp, gainAffinityXp |
| 전투유닛 | `src/entities/BattleUnit.js` | fromMercenary, fromEnemyData, 저주효과 |
| 길드 | `src/systems/GuildManager.js` | addXp, addGold, unlockFacility |
| 모집 | `src/systems/MercenaryManager.js` | generateRecruitPool, rollRarity, hire |
| 보관 | `src/systems/StorageManager.js` | addItem, sellItem |
| 저장 | `src/systems/SaveManager.js` | save, load |
| BP 전투 | `src/scenes/BattleScene.js` | _spawnEnemies, _checkRoundEnd, _endRun |
| Cargo 전투 | `src/scenes/CargoBattleScene.js` | _spawnEnemies, 역 시스템, 칸 수리 |
| BO 전투 | `src/scenes/BlackoutBattleScene.js` | 그리드 탐색, _startBossCombat |
| 결과 | `src/scenes/RunResultScene.js` | _applyResults, 용병XP/친화도 분배 |
| 경매 | `src/scenes/AuctionScene.js` | _refreshStock, processConsignments |
| 마을 | `src/scenes/TownScene.js` | 시설그리드, 상인호감도 |

---

## 밸런스 조정 가이드

### 너무 쉬울 때
- `getBossScaleMult` 값 올리기 (현재 Lv1: 0.5)
- 보스 호위병 구성 강화 (enemies.js composition 함수)
- `deathChance` 올리기 (zones.js)
- 적 스케일링 `scaleMult` 계수 올리기

### 너무 어려울 때
- `getBossScaleMult` 값 내리기
- 보스 호위병 줄이기
- 라운드 보너스 골드/XP 올리기
- 카드 티어1 효과 강화

### 진행이 너무 빠를 때
- `GUILD_LEVEL_XP` 값 올리기
- `용병 getXpToNextLevel` 올리기
- 시설 해금 비용 올리기
- 런당 골드 수입 줄이기 (enemyGold, roundBonus)

### 진행이 너무 느릴 때
- `GUILD_LEVEL_XP` 값 내리기
- `baseGoldReward`, `baseXpReward` 올리기
- 아이템 가치 올리기 (valueMult)
- 모집 비용 내리기 (BASE_HIRE_COST)
