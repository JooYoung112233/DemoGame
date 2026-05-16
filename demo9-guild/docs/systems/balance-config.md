# 시스템: 밸런스 상수 중앙화

> 모든 밸런스 수치를 **단일 파일**(`src/data/balance.js`)에 모아 빠른 조정 가능.
> 코드 곳곳에 흩어진 매직 넘버 금지. 변경 시 이 문서 + balance.js 동시 업데이트.

## 1. 파일 구조 (`src/data/balance.js`)

```javascript
const BALANCE = {
  // === 시작 자원 ===
  START_GOLD: 500,
  START_GUILD_LEVEL: 1,
  START_FACILITIES: ['recruit', 'storage', 'gate'],
  START_ROSTER: 0,
  START_DEPLOY: 2,

  // === 모집 ===
  BASE_HIRE_COST: 60,
  HIRE_COST_RARITY_MULT: { common: 1.0, uncommon: 1.5, rare: 2.5, epic: 4.0, legendary: 6.0 },
  HIRE_GUILD_LV_MULT: 0.15,  // (1 + guildLv × 0.15)
  RECRUIT_REFRESH_COST: 100,
  RECRUIT_FREE_REFRESH_MIN: 10,  // 게임 내 분

  // === 길드 ===
  GUILD_LEVEL_XP: [0, 100, 250, 500, 800, 1200, 1800, 2500],
  GUILD_MAX_LEVEL: 8,
  ROSTER_LIMITS: {
    1: { max: 4, deploy: 2 },
    2: { max: 4, deploy: 2 },
    3: { max: 6, deploy: 3 },
    4: { max: 6, deploy: 3 },
    5: { max: 8, deploy: 4 },
    6: { max: 8, deploy: 4 },
    7: { max: 10, deploy: 5 },
    8: { max: 10, deploy: 5 }
  },

  // === 길드 회관 ===
  GUILD_HALL_BASE_COST: 300,
  GUILD_HALL_COST_MULT: 1.5,   // cost(stage) = base × mult^(stage-1)
  GUILD_HALL_MAX_STAGE: 12,

  // === 클래스 베이스 스탯 (Lv1) ===
  CLASS_BASE: {
    warrior:   { hp: 250, atk: 20, def: 18, spd: 90,  critRate: 0.05, critDmg: 1.5, attackSpeed: 1800, range: 55, moveSpeed: 90  },
    rogue:     { hp: 160, atk: 35, def: 8,  spd: 140, critRate: 0.25, critDmg: 2.0, attackSpeed: 1000, range: 55, moveSpeed: 140 },
    mage:      { hp: 120, atk: 45, def: 5,  spd: 50,  critRate: 0.10, critDmg: 1.8, attackSpeed: 2200, range: 280, moveSpeed: 50  },
    archer:    { hp: 150, atk: 30, def: 7,  spd: 80,  critRate: 0.18, critDmg: 1.8, attackSpeed: 1400, range: 250, moveSpeed: 80  },
    priest:    { hp: 180, atk: 12, def: 10, spd: 60,  critRate: 0.05, critDmg: 1.3, attackSpeed: 2000, range: 200, moveSpeed: 60  },
    alchemist: { hp: 170, atk: 15, def: 12, spd: 60,  critRate: 0.08, critDmg: 1.4, attackSpeed: 1800, range: 180, moveSpeed: 60  }
  },

  // === 클래스 레벨업 성장 (per Lv) ===
  CLASS_GROWTH: {
    warrior:   { hp: 30, atk: 2, def: 2 },
    rogue:     { hp: 15, atk: 4, def: 1 },
    mage:      { hp: 12, atk: 5, def: 0 },
    archer:    { hp: 14, atk: 3, def: 1 },
    priest:    { hp: 20, atk: 1, def: 1 },
    alchemist: { hp: 18, atk: 2, def: 1 }
  },

  // === 희귀도 ===
  RARITY_MULT: {
    common:    { stat: 1.0,  growth: 1.0,  pos: 1, neg: 1.0  },
    uncommon:  { stat: 1.1,  growth: 1.1,  pos: 1, neg: 0.5  },
    rare:      { stat: 1.2,  growth: 1.2,  pos: 2, neg: 0.5  },
    epic:      { stat: 1.35, growth: 1.3,  pos: 2, neg: 0.0  },
    legendary: { stat: 1.5,  growth: 1.5,  pos: 2, neg: 0.0  }
  },
  RARITY_POOL: {
    1: { common: 60, uncommon: 35, rare: 5,  epic: 0,  legendary: 0 },
    2: { common: 60, uncommon: 35, rare: 5,  epic: 0,  legendary: 0 },
    3: { common: 40, uncommon: 40, rare: 18, epic: 2,  legendary: 0 },
    4: { common: 40, uncommon: 40, rare: 18, epic: 2,  legendary: 0 },
    5: { common: 25, uncommon: 35, rare: 30, epic: 9,  legendary: 1 },
    6: { common: 25, uncommon: 35, rare: 30, epic: 9,  legendary: 1 },
    7: { common: 15, uncommon: 30, rare: 30, epic: 20, legendary: 5 },
    8: { common: 15, uncommon: 30, rare: 30, epic: 20, legendary: 5 }
  },

  // === 용병 XP 곡선 ===
  MERC_XP_CURVE: { coef: 50, exp: 1.5 },  // needed = coef × lv^exp
  MERC_MAX_LEVEL: 30,

  // === 데미지 공식 ===
  DEF_MODE: 'percent',  // 'subtract' | 'percent'
  DEF_PERCENT_FORMULA: 'def / (def + 100)',  // % 감소
  CRIT_MIN: 0.0,
  CRIT_MAX: 1.0,

  // === 피로도 ===
  STAMINA_MAX: 100,
  STAMINA_RECOVERY_BASE: 1,        // /분
  STAMINA_RECOVERY_C_POOL: 5,      // C 풀 (길드회관 C 업)
  STAMINA_DEPLETION: {
    main_bp: 25, main_cargo: 30, main_blackout: 40,
    sub_bp: 12, sub_cargo: 16, sub_blackout: 20,
    sub_near_death_extra: 25,
    main_injured_extra: 10
  },
  STAMINA_TIERS: {
    full:    { min: 80, max: 100, atkMod: 0.05  },  // 컨디션 최상
    normal:  { min: 50, max: 79,  atkMod: 0     },
    tired:   { min: 30, max: 49,  atkMod: -0.10, hitMod: -0.10 },
    exhaust: { min: 10, max: 29,  atkMod: -0.25, timeMod: 0.25, deathMod: 0.5 },
    forced:  { min: 0,  max: 9,   blocked: true }
  },

  // === Bond ===
  BOND_MAX: 100,
  BOND_GAIN: {
    sub_success: 4, sub_fail: 2,
    main_success: 8, main_fail: 4,
    main_multiplier: 2.0  // 메인은 ×2
  },
  BOND_TIER_THRESHOLDS: [0, 10, 30, 50, 75, 100],
  BOND_TIER_BONUS: [0, 0.02, 0.05, 0.08, 0.12, 0.15],  // 모든 스탯 %

  // === 친화도 트리 ===
  AFFINITY_XP_TABLE: [100, 150, 200, 300, 400, 600, 800, 1000, 1500],
  AFFINITY_XP_PER_RUN: { min: 15, max: 50 },
  AFFINITY_MAX_LEVEL: 10,
  AFFINITY_POINTS_PER_LEVEL: 1,
  COMMON_AFFINITY_POINT_PER_GUILD_LEVEL: 1,

  // === 길드 평판 ===
  REPUTATION_MAX: 100,
  REPUTATION_GAIN: {
    main_success: 0.5, main_fail: -0.2,
    boss_kill: 5,
    npc_tier_up: 2, npc_trade: 0.2,
    event_positive: 3, event_negative: -2,
    merc_death: -1
  },
  REPUTATION_MILESTONES: [0, 15, 35, 60, 100],

  // === NPC ===
  NPC_FAVOR_MAX: 100,
  NPC_FAVOR_GAIN: {
    trade: 0.5, quest: 8, gift: 2, event_positive: 3, event_negative: -5
  },
  NPC_FAVOR_TIERS: [0, 10, 30, 60, 100],
  NPC_INVENTORY_ROTATION_MIN: 60,  // 게임 내 분
  NPC_INVENTORY_SLOTS: [3, 5, 7, 8, 10],  // 티어별

  // === RunSimulator ===
  RUN_SIM: {
    POWER_OFFENSE_MULT: 0.05,    // 레벨당
    POWER_DEFENSE_MULT: 0.02,    // def × 0.02
    POWER_LEVEL_GROWTH: 0.05,
    POWER_RARITY_BONUS: { common: 1.0, uncommon: 1.05, rare: 1.1, epic: 1.2, legendary: 1.35 },
    POWER_STAMINA_PENALTY: { 30: 0.7, 50: 0.85, 80: 0.95 },
    ZONE_BASE_POWER: { bloodpit: 200, cargo: 300, blackout: 450 },
    ZONE_POWER_SCALE_PRIMARY: 0.25,
    ZONE_POWER_SCALE_SECONDARY: 0.03,
    SUCCESS_SIGMOID_K: 6,           // 1 / (1 + exp(-k × (ratio - mid)))
    SUCCESS_SIGMOID_MID: 0.7,
    BONUS_SYNERGY_MAX: 0.15,
    BONUS_BOND_AVG_MULT: 0.05,
    BONUS_AFFINITY_MAX: 0.10,
    NEGATIVE_TRAIT_MAX: -0.15,
    STAMINA_AVG_PENALTY: { 30: -0.15, 50: -0.08 }
  },

  // === 구역 보상 ===
  ZONE_REWARDS: {
    bloodpit:  { baseGold: 60,  baseXp: 30, lootMin: 1, lootMax: 3, deathChance: 0.12 },
    cargo:     { baseGold: 80,  baseXp: 35, lootMin: 2, lootMax: 4, deathChance: 0.10 },
    blackout:  { baseGold: 100, baseXp: 40, lootMin: 1, lootMax: 5, deathChance: 0.18 }
  },
  PIT_GAUGE_MAX_MULT: 2.0,        // 0%=1.0x, 100%=2.0x

  // === 적 스탯 ===
  ENEMY_BASE: {
    normal:   { hp: 80,  atk: 12, def: 3,  spd: 100, threat: 1.0 },
    elite:    { hp: 180, atk: 25, def: 6,  spd: 110, threat: 2.0 },
    guard:    { hp: 280, atk: 35, def: 12, spd: 100, threat: 3.0 }
  },
  ENEMY_SCALING: {
    hpPerLevel:  0.25,   // (1 + (lv-1) × 0.25)
    atkPerLevel: 0.20,   // (1 + (lv-1) × 0.20)
    defAddPerLevel: 2    // def + (lv-1) × 2
  },

  // === 보스 ===
  BOSS_PHASES: 3,
  BOSS_HP_BASE: { bp: 2500, cargo: 3000, blackout: 3500 },
  BOSS_FIRST_KILL_LEGENDARY: 1.0,    // 100%
  BOSS_REPEAT_LEGENDARY: 0.30,
  BOSS_REKILL_BUFF_PER_KILL: 0.10,   // +10%/처치
  BOSS_MASTER_KILLS: 10,

  // === 제작 ===
  CRAFT_TIME: { uncommon: 30, rare: 120, epic: 600, legendary: 1800 },  // 초
  ENHANCE_BASE_COST: { 1: 100, 2: 300, 3: 800, 4: 2000, 5: 5000 },
  ENHANCE_SUCCESS: { 1: 1.0, 2: 0.9, 3: 0.7, 4: 0.5, 5: 0.3 },
  ENHANCE_STAT_BONUS: { 1: 0.05, 2: 0.10, 3: 0.15, 4: 0.25, 5: 0.35 },
  CRAFT_SLOTS_BY_GUILD: { 1: 1, 3: 2, 5: 3, 7: 4 },

  // === 시간 ===
  TIME_OFFLINE_MAX_HOURS: 4,
  TIME_OFFLINE_MAX_HOURS_AUTOMATION: 8,
  TIME_EVENT_INTERVAL_MIN: 30,       // 게임 내 분
  TIME_NPC_ROTATION_MIN: 60,

  // === 사망/은퇴 ===
  PERMADEATH_MODE_DEFAULT: false,
  PERMADEATH_HONOR_MULT: 1.5,
  REVIVE_COST_BASE: 1000,            // × guildLevel
  RETIREMENT_GOLD: 100,              // × mercLevel

  // === NG+ ===
  HONOR_BASE_PER_CYCLE: 100,
  HONOR_SPEED_8H: 200,
  HONOR_SPEED_12H: 50,
  HONOR_PER_DEATH: 10,
  HONOR_PER_BOSS: 50,
  HONOR_NPC_ALLY: 100,
  NG_PLUS_ENEMY_BOOST_PER_CYCLE: 0.10,
  NG_PLUS_DIFFICULTY_MULT: {
    normal: 1.0, challenge_1: 1.3, challenge_2: 1.6, challenge_3: 2.0, nightmare: 3.0
  }
};
```

## 2. 클래스 성장 곡선 검증 (Lv1-30)

### 2.1 Warrior 곡선

| Lv | HP | ATK | DEF | 계산 |
|---|---|---|---|---|
| 1 | 250 | 20 | 18 | base |
| 5 | 370 | 28 | 26 | base + 4×grow |
| 10 | 520 | 38 | 36 | base + 9×grow |
| 15 | 670 | 48 | 46 | |
| 20 | 820 | 58 | 56 | |
| 30 | 1,120 | 78 | 76 | base + 29×grow |

희귀도 적용 (legendary, statMult 1.5):
- Lv1: 375/30/27, Lv10: 780/57/54, Lv30: 1,680/117/114

### 2.2 Rogue 곡선

| Lv | HP | ATK | CRIT |
|---|---|---|---|
| 1 | 160 | 35 | 25% |
| 10 | 295 | 71 | 25% |
| 30 | 595 | 151 | 25% |

CRIT은 레벨업으로 안 오름 (스킬/장비/특성으로 조정).

### 2.3 Mage 곡선

| Lv | HP | ATK |
|---|---|---|
| 1 | 120 | 45 |
| 10 | 228 | 90 |
| 30 | 468 | 190 |

Mage는 DEF 0 성장 → 유리 대포 유지.

## 3. DEF 처리 방식 결정

```
DEF_MODE: 'percent' (확정)

데미지 감소 공식:
  damage_taken = raw_damage × (1 - def / (def + 100))

예시:
  def 10: 데미지 × 0.91 (-9%)
  def 30: 데미지 × 0.77 (-23%)
  def 50: 데미지 × 0.67 (-33%)
  def 100: 데미지 × 0.50 (-50%)
  def 200: 데미지 × 0.33 (-67%)
```

**이유**: 정수 감산 방식(def 1 = -1)은 후반에 의미 없어짐. % 방식은 후반에도 가치 유지.

## 4. 적 스탯 검증 (Lv1-10)

### 4.1 일반 몹

| Lv | HP | ATK | DEF | 워리어 Lv 처치 시간 |
|---|---|---|---|---|
| 1 | 80 | 12 | 3 | 80 / (20 × 0.97) = 4.1초 |
| 5 | 160 | 22 | 11 | 160 / (28 × 0.91) = 6.3초 |
| 10 | 260 | 34 | 21 | 260 / (38 × 0.83) = 8.2초 |

워리어 vs 일반 몹 1회 처치 시간 4-8초 → 라운드당 다수 적 처치 가능.

### 4.2 엘리트 vs 파티

| Lv | HP | 4인 파티 누적 DPS | 처치 시간 |
|---|---|---|---|
| 1 | 180 | 28+35+45+30 = 138 ATK × 0.97 = 134 | 1.3초 |
| 5 | 360 | 200 × 0.91 = 182 | 2.0초 |
| 10 | 585 | 270 × 0.83 = 224 | 2.6초 |

엘리트는 빠르게 처치 가능. **밸런스 OK**.

### 4.3 적 ATK vs 파티 DEF

Lv10 일반 몹 ATK 34 vs 워리어 DEF 36 → DEF mode percent:
- 데미지 = 34 × (1 - 36/136) = 34 × 0.735 = 25 / 공격
- 워리어 HP 520 / 25 = 21 공격 버팀 → 안정적

## 5. 변경 방법 (워크플로)

밸런스 조정 시:

1. **balance.js 수정** — 해당 상수 값 변경
2. **balance-config.md 동기화** — 본 문서 업데이트
3. **테스트 플레이** — 자동 시뮬 OR 수동 플레이
4. **결과 기록** — balance-review.md에 노트 추가

코드 곳곳의 매직 넘버 사용 금지. 항상 `BALANCE.XXX` 참조.

## 6. 검증 체크리스트

코드 구현 후 다음 항목 시뮬로 검증:

- [ ] 12시간 자동 플레이 후 길드 Lv 8 도달
- [ ] 12시간 후 누적 골드 300K~350K
- [ ] 메인 클리어률: 초반 95%, 중반 80%, 보스 70%
- [ ] 사망률: 12시간 0~3명
- [ ] 주력 페어 Bond 5-6시간 Max
- [ ] 주력 용병 친화도 Lv 8+ 도달 (1구역)
- [ ] NPC 중 2-3명 호감 60+ 도달
- [ ] 길드회관 약 50% 완성
- [ ] 명예 포인트 150-300 획득
