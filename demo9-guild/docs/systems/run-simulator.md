# 시스템: RunSimulator v2 (서브 파견)

> 서브 파견 자동 회전 엔진. 메인 직접 전투(BattleScene)와는 별개.

## 1. 파티 파워 (Power Rating)

```javascript
function calcPartyPower(party) {
  let totalPower = 0;

  for (const merc of party) {
    const stats = merc.getStats();  // 장비/훈련 반영 최종 스탯

    // 개인 파워 = 공격력 × 생존력의 기하평균
    const offense = stats.atk * (1 + stats.critRate * stats.critDmg);
    const defense = stats.hp * (1 + stats.def * 0.02);
    let power = Math.sqrt(offense * defense);

    // 레벨 보정
    power *= (1 + (merc.level - 1) * 0.05);

    // 희귀도 보정
    const rarityBonus = {
      common: 1.0, uncommon: 1.05, rare: 1.1,
      epic: 1.2, legendary: 1.35
    };
    power *= rarityBonus[merc.rarity];

    // 피로도 페널티
    if (merc.stamina < 30) power *= 0.7;
    else if (merc.stamina < 50) power *= 0.85;
    else if (merc.stamina < 80) power *= 0.95;

    totalPower += power;
  }

  return totalPower;
}
```

## 2. 구역 권장 파워

```javascript
function getZoneRecommendedPower(zone, zoneLevel) {
  const basePower = {
    bloodpit: 200,
    cargo: 300,
    blackout: 450
  };

  return basePower[zone]
    * (1 + (zoneLevel - 1) * 0.25)
    * (1 + (zoneLevel - 1) * 0.03);

  // BP:  Lv1=200, Lv3=320, Lv5=480, Lv8=760, Lv10=1000
  // Cargo: Lv1=300, Lv5=720, Lv10=1500
  // BO:  Lv1=450, Lv5=1080, Lv10=2250
}
```

## 3. 파워 비율 해석

```
powerRatio = partyPower / recommendedPower

0.3 이하 → 무모 (거의 확정 실패, 높은 사망)
0.5      → 위험 (40% 성공, 중간 사망)
0.7      → 도전 (60% 성공)
1.0      → 적정 (80% 성공)
1.3      → 여유 (95% 성공, 사망 거의 없음)
1.5+     → 안전 (99% 성공, 사망 0%, 보상 약간↓)
```

## 4. 성공률 공식

```javascript
function calcSuccessRate(powerRatio, party, zone, zoneLevel, gameState) {
  // 시그모이드 커브
  let baseRate = 1 / (1 + Math.exp(-6 * (powerRatio - 0.7)));
  // ratio 0.3→8%, 0.5→23%, 0.7→50%, 1.0→86%, 1.3→97%

  baseRate += calcSynergyBonus(party);                            // 시너지 (0 ~ +0.15)
  baseRate += calcAvgBondBonus(party) * 0.05;                     // 친밀도 (0 ~ +0.05)
  baseRate += getPartyAvgAffinity(party, zone) * 0.02;            // 친화도 (0 ~ +0.10)
  baseRate += getZoneControlBonus(gameState, zone, 'successRate');// 구역업 (길드회관 F/G/H)
  baseRate += calcNegativeTraitPenalty(party);                    // 음성 특성 (0 ~ -0.15)

  // 피로도 페널티
  const avgStamina = party.reduce((s, m) => s + m.stamina, 0) / party.length;
  if (avgStamina < 30) baseRate -= 0.15;
  else if (avgStamina < 50) baseRate -= 0.08;

  return Math.max(0.05, Math.min(0.99, baseRate));
}
```

## 5. 라운드 진행 (부분 성공)

```javascript
function calcRoundsCompleted(successRate, maxRounds) {
  if (Math.random() < successRate) {
    return maxRounds;  // 전 라운드 클리어
  }

  // 실패: 부분 진행
  const progressRatio = successRate * (0.6 + Math.random() * 0.3);
  return Math.max(1, Math.floor(maxRounds * progressRatio));
}

// maxRounds = min(10, 4 + floor((zoneLevel-1) * 1.5))
// Lv1: 4, Lv3: 7, Lv5: 10
```

→ 실패해도 3/7까지 갔으면 3/7만큼 보상.

## 6. 사망률

```javascript
function calcDeathChance(merc, powerRatio, zone, roundsCompleted, maxRounds, gameState) {
  let deathRate;
  if (powerRatio < 0.5) deathRate = 0.30;
  else if (powerRatio < 0.7) deathRate = 0.15;
  else if (powerRatio < 1.0) deathRate = 0.08;
  else if (powerRatio < 1.3) deathRate = 0.03;
  else deathRate = 0.01;

  if (roundsCompleted < maxRounds) deathRate *= 1.5;

  const classDeathMod = {
    warrior: 1.3, rogue: 1.1, mage: 0.7,
    archer: 0.7, priest: 0.8, alchemist: 0.9
  };
  deathRate *= classDeathMod[merc.classKey];

  if (merc.stamina < 30) deathRate *= 2.0;
  else if (merc.stamina < 50) deathRate *= 1.3;

  if (merc.hasTrait('weak_body')) deathRate *= 1.3;
  if (merc.hasTrait('coward')) deathRate *= 1.2;
  if (merc.hasTrait('veteran')) deathRate *= 0.7;

  deathRate *= (1 - getZoneControlBonus(gameState, zone, 'deathReduction'));

  return Math.min(0.50, Math.max(0.005, deathRate));
}
```

## 7. 보상 산출

```javascript
function calcRewards(zone, zoneLevel, roundsCompleted, maxRounds, party, gameState) {
  const progress = roundsCompleted / maxRounds;
  const success = roundsCompleted === maxRounds;

  // === 골드 ===
  const baseGold = { bloodpit: 60, cargo: 80, blackout: 100 };
  let gold = baseGold[zone];
  for (let r = 1; r <= roundsCompleted; r++) {
    gold += 15 + r * 10;
  }
  if (success) gold += 50 + zoneLevel * 20;
  const enemyGoldPerRound = 12 + zoneLevel * 4;
  gold += roundsCompleted * enemyGoldPerRound;
  if (zone === 'cargo' && success) gold += 50 + 100 * progress;
  if (zone === 'blackout') gold += roundsCompleted * 5;
  gold *= (1 + getZoneControlBonus(gameState, zone, 'goldBonus'));

  // === 길드 XP ===
  let guildXp = 0;
  for (let r = 1; r <= roundsCompleted; r++) {
    guildXp += 10 + r * 5;
  }
  if (success) guildXp += 30;
  guildXp *= (1 + getZoneControlBonus(gameState, zone, 'xpBonus'));

  // === 용병 XP / 친화도 XP ===
  const mercXp = Math.floor(guildXp * 0.5 + roundsCompleted * 5 + (success ? 15 : 0));
  const affinityXp = Math.floor(10 + roundsCompleted * 3 + (success ? 10 : 0));

  // === 아이템 드롭 ===
  const loot = generateLoot(zone, zoneLevel, roundsCompleted, maxRounds, gameState);

  // === 피로도 소모 ===
  const staminaCost = {
    bloodpit: 12 + roundsCompleted * 1,
    cargo: 15 + roundsCompleted * 1.5,
    blackout: 20 + roundsCompleted * 2
  };

  return {
    success, roundsCompleted, maxRounds, progress,
    gold: Math.floor(gold),
    guildXp: Math.floor(guildXp),
    mercXp: Math.floor(mercXp),
    affinityXp: Math.floor(affinityXp),
    loot,
    staminaCost: Math.floor(staminaCost[zone])
  };
}
```

## 8. 파견 시간 공식

```javascript
function calcExpeditionTime(zone, zoneLevel, party, gameState) {
  const baseMinutes = { bloodpit: 2, cargo: 5, blackout: 10 };
  let time = baseMinutes[zone];

  time *= (1 + zoneLevel * 0.12);                            // 깊이

  const powerRatio = calcPartyPower(party) / getZoneRecommendedPower(zone, zoneLevel);
  const powerCoef = Math.max(0.6, Math.min(1.4, 1.5 - powerRatio * 0.5));
  time *= powerCoef;                                          // 전투력

  const avgAffinity = getPartyAvgAffinity(party, zone);
  time *= (1 - avgAffinity * 0.04);                          // 친화도 (Lv5: -20%)

  const avgStamina = party.reduce((s, m) => s + m.stamina, 0) / party.length;
  if (avgStamina < 30) time *= 1.4;
  else if (avgStamina < 50) time *= 1.15;                    // 피로도

  time *= (1 - getZoneControlBonus(gameState, zone, 'timeReduction'));  // 구역업

  return Math.max(0.5, time);  // 최소 30초
}
```

## 9. 파견 시간 예시

| 상황 | BP Lv1 | BP Lv5 | Cargo Lv3 | BO Lv5 |
|---|---|---|---|---|
| 베이스 | 2분 | 2분 | 5분 | 10분 |
| ×구역레벨 | 2.2분 | 3.2분 | 6.8분 | 16분 |
| ×파워1.0 | 2.2분 | 3.2분 | 6.8분 | 16분 |
| ×파워1.5 | 1.5분 | 2.2분 | 4.8분 | 11분 |
| ×친화도Lv5 | 1.2분 | 1.8분 | 3.8분 | 9분 |
| +구역업 풀 (-45%) | **0.7분(42초)** | **1.0분** | **2.1분** | **5분** |

## 10. 파견 중 이벤트

25% 확률로 진행 중 알림 팝업. 선택에 따라 결과 보정.

**Blood Pit 이벤트 풀:**
- `pit_ambush`: 매복 → 정면 돌파 (+골드, +사망↑) / 우회 (+시간, 안전)
- `pit_treasure`: 숨겨진 보물 → 즉시 회수 (+아이템) / 무시 (+XP)
- `pit_elite_challenge`: 엘리트 도발 → 도전 (전설소재/사망↑) / 무시

**Cargo 이벤트 풀:**
- `cargo_storm`: 폭풍 진입 → 화물칸/무기고/의무실 보강 선택
- `cargo_stowaway`: 밀항자 → 추방/고용(50G)/심문(+소문)

**Blackout 이벤트 풀:**
- `bo_curse_altar`: 저주 제단 → 기도(저주+2, 보상2배) / 파괴(저주-1) / 무시
- `bo_ghost`: 과거 용병 영혼 → 위로(ATK+10%) / 정보(보스약점) / 무시(저주+1)

## 11. 친밀도 누적

```javascript
function updateBonds(party, success) {
  for (let i = 0; i < party.length; i++) {
    for (let j = i + 1; j < party.length; j++) {
      const key = getBondKey(party[i].id, party[j].id);
      const gain = success ? 3 : 1;
      gameState.bonds[key] = (gameState.bonds[key] || 0) + gain;
    }
  }
}
// Bond 50: 같이 출전 시 +5% 스탯
// Bond 100: 전용 페어 스킬 또는 사망 시 광폭화
```

상세: [bonds.md](bonds.md)

## 12. 결과 로그

파견 완료 시 텍스트 로그 생성. 라운드별 진행, 이벤트, 사망, 보상 요약.

## 13. gameState 발췌

```javascript
activeExpeditions: [
  {
    id: 'exp_001',
    zone: 'bloodpit',
    zoneLevel: 3,
    partyIds: ['merc_01', 'merc_05', 'merc_08'],
    startTime: Date.now(),
    estimatedDuration: 180000,  // ms
    midEvent: null,
    autoRedeploy: true
  }
],
pendingResults: [],
bonds: {}  // { 'merc01_merc05': 45, ... }
```
