#!/usr/bin/env node
// 12시간 자동 플레이 시뮬레이터 v2 (정교화)
// 사용: node balance/simulator.js [--strategy balanced] [--hours 12] [--seed 42]

const { BALANCE, BalanceHelper } = require('../src/data/balance.js');

// === 시드 RNG ===
let _rngState = 42;
function seedRng(seed) { _rngState = seed; }
function rng() {
    _rngState = (_rngState * 9301 + 49297) % 233280;
    return _rngState / 233280;
}
function rngInt(min, max) { return Math.floor(rng() * (max - min + 1)) + min; }
function rngPick(arr) { return arr[Math.floor(rng() * arr.length)]; }
function rngWeighted(weightMap) {
    const total = Object.values(weightMap).reduce((s, w) => s + w, 0);
    let r = rng() * total;
    for (const [key, w] of Object.entries(weightMap)) {
        r -= w;
        if (r <= 0) return key;
    }
    return Object.keys(weightMap)[0];
}

// === 게임 상태 ===
let state;
let _mercIdCounter = 0;

function createGameState() {
    return {
        gold: BALANCE.START_GOLD,
        guildLevel: BALANCE.START_GUILD_LEVEL,
        guildXp: 0,
        reputation: 0,
        roster: [],
        recruitPool: [],
        recruitPoolGeneratedAt: -10,
        zoneLevel: { bloodpit: 1, cargo: 0, blackout: 0 },
        bossDefeated: { bp: false, cargo: false, blackout: false },
        activeExpeditions: [],
        bonds: {},
        npcFavor: { krough: 0, mira: 0, herta: 0, golden: 0, speedcar: 0, iron: 0, shadow: 0, priest: 0 },
        unlockedFacilities: BALANCE.START_FACILITIES.slice(),
        guildHall: { operations: 0, infrastructure: 0, recovery: 0, automation: 0, intel: 0 },
        simTimeMin: 0,
        stats: {
            mainRuns: 0, mainSuccess: 0, mainFail: 0,
            subRuns: 0, subSuccess: 0, subFail: 0,
            deaths: 0,
            totalGoldEarned: BALANCE.START_GOLD,
            totalXpEarned: 0,
            bossKills: 0,
            facilitiesUnlocked: BALANCE.START_FACILITIES.length,
            guildHallStages: 0
        },
        timeline: [],
        topBondMaxAt: null,
        topAffinityLv8At: null
    };
}

// === 용병 ===
function createMerc(guildLv) {
    const rarity = rngWeighted(BALANCE.RARITY_POOL[Math.min(guildLv, 8)]);
    const cls = rngPick(Object.keys(BALANCE.CLASS_BASE));
    const stats = BalanceHelper.getClassStats(cls, 1, rarity);
    return {
        id: 'merc_' + (++_mercIdCounter),
        classKey: cls, rarity, level: 1, xp: 0, stamina: 100,
        ...stats,
        zoneAffinity: {
            bloodpit:  { xp: 0, level: 0, points: 0 },
            cargo:     { xp: 0, level: 0, points: 0 },
            blackout:  { xp: 0, level: 0, points: 0 }
        },
        alive: true
    };
}

function rarityRank(r) {
    return ['common', 'uncommon', 'rare', 'epic', 'legendary'].indexOf(r);
}

// === 파워 계산 ===
function calcMercPower(merc) {
    const stats = BalanceHelper.getClassStats(merc.classKey, merc.level, merc.rarity);
    const offense = stats.atk * 1.1;
    const defense = stats.hp * (1 + stats.def * BALANCE.RUN_SIM.POWER_DEFENSE_MULT);
    let power = Math.sqrt(offense * defense);
    power *= (1 + (merc.level - 1) * BALANCE.RUN_SIM.POWER_LEVEL_GROWTH);
    power *= BALANCE.RUN_SIM.POWER_RARITY_BONUS[merc.rarity];

    // 친화도 적용 (구역 노드별 일반화 +1%/Lv)
    // 시뮬은 zone 무관 평균으로
    const avgAffinity = (merc.zoneAffinity.bloodpit.level + merc.zoneAffinity.cargo.level + merc.zoneAffinity.blackout.level) / 3;
    power *= (1 + avgAffinity * 0.02);

    if (merc.stamina < 30) power *= 0.7;
    else if (merc.stamina < 50) power *= 0.85;
    else if (merc.stamina < 80) power *= 0.95;
    return power;
}
function calcPartyPower(party) {
    return party.reduce((s, m) => s + calcMercPower(m), 0);
}

function calcAvgBond(party) {
    if (party.length < 2) return 0;
    let total = 0, count = 0;
    for (let i = 0; i < party.length; i++) {
        for (let j = i + 1; j < party.length; j++) {
            const key = [party[i].id, party[j].id].sort().join('_');
            total += (state.bonds[key] || 0) / 100;
            count++;
        }
    }
    return count > 0 ? total / count : 0;
}

function calcSynergyBonus(party) {
    // 같은 클래스 2+/3+/5+ 시너지 단순화
    const counts = {};
    party.forEach(m => { counts[m.classKey] = (counts[m.classKey] || 0) + 1; });
    let bonus = 0;
    Object.values(counts).forEach(c => {
        if (c >= 5) bonus += 0.12;
        else if (c >= 3) bonus += 0.08;
        else if (c >= 2) bonus += 0.04;
    });
    return Math.min(0.15, bonus);
}

function calcSuccessRate(party, zone, zoneLevel) {
    const power = calcPartyPower(party);
    const ratio = power / BalanceHelper.getZoneRecommendedPower(zone, zoneLevel);
    const k = BALANCE.RUN_SIM.SUCCESS_SIGMOID_K;
    const mid = BALANCE.RUN_SIM.SUCCESS_SIGMOID_MID;
    let rate = 1 / (1 + Math.exp(-k * (ratio - mid)));
    rate += calcAvgBond(party) * 0.05;
    rate += calcSynergyBonus(party);
    const affinityAvg = party.reduce((s, m) => s + m.zoneAffinity[zone].level, 0) / party.length / 10;
    rate += affinityAvg * 0.10;
    // 길드회관 D(자동화) 보너스 단순화
    rate += state.guildHall.automation * 0.005;
    return Math.max(0.05, Math.min(0.99, rate));
}

// === 가용 용병 분류 ===
function getAliveRoster() {
    return state.roster.filter(m => m.alive);
}
function isOnExpedition(merc) {
    return state.activeExpeditions.some(e => e.party.includes(merc));
}

// 메인용 후보 = 강한 순 + stamina 50+
function getMainSquad() {
    const candidates = getAliveRoster().filter(m => !isOnExpedition(m) && m.stamina >= 50);
    candidates.sort((a, b) => {
        const r = rarityRank(b.rarity) - rarityRank(a.rarity);
        if (r !== 0) return r;
        const l = b.level - a.level;
        if (l !== 0) return l;
        return calcMercPower(b) - calcMercPower(a);
    });
    const deployMax = BALANCE.ROSTER_LIMITS[state.guildLevel].deploy;
    return candidates.slice(0, deployMax);
}

// 메인 1군 reserve = 가장 강한 deployMax명 (stamina 무관)
function getReservedForMain() {
    const candidates = getAliveRoster().filter(m => !isOnExpedition(m));
    candidates.sort((a, b) => {
        const r = rarityRank(b.rarity) - rarityRank(a.rarity);
        if (r !== 0) return r;
        return b.level - a.level;
    });
    const deployMax = BALANCE.ROSTER_LIMITS[state.guildLevel].deploy;
    return candidates.slice(0, deployMax);
}

// 서브용 = 메인 reserve 제외 + stamina 40+ + 살아있음
function getSubAvailable() {
    const reserved = new Set(getReservedForMain().map(m => m.id));
    return getAliveRoster().filter(m =>
        !reserved.has(m.id) &&
        !isOnExpedition(m) &&
        m.stamina >= 40
    );
}

// === 모집 ===
function tryHire() {
    const rosterMax = BALANCE.ROSTER_LIMITS[state.guildLevel].max;
    const alive = getAliveRoster().length;
    if (alive >= rosterMax) return false;

    // 모집 풀 갱신 (10분 cycle)
    if (state.simTimeMin - state.recruitPoolGeneratedAt >= BALANCE.RECRUIT_FREE_REFRESH_MIN || state.recruitPool.length === 0) {
        state.recruitPool = [];
        for (let i = 0; i < 3; i++) state.recruitPool.push(createMerc(state.guildLevel));
        state.recruitPoolGeneratedAt = state.simTimeMin;
    }

    // 가장 강한 희귀도 선택, 단 가능한 골드 내
    state.recruitPool.sort((a, b) => rarityRank(b.rarity) - rarityRank(a.rarity));
    for (const candidate of state.recruitPool) {
        const cost = BalanceHelper.getHireCost(candidate.rarity, state.guildLevel);
        const reserveGold = state.guildLevel < 3 ? 200 : 1000;
        if (state.gold >= cost + reserveGold) {
            state.gold -= cost;
            state.roster.push(candidate);
            state.recruitPool = state.recruitPool.filter(m => m !== candidate);
            return true;
        }
    }
    // 골드 부족하지만 더 강한 후보 노리기 위해 refresh (중반+)
    if (state.guildLevel >= 4 && state.gold >= BALANCE.RECRUIT_REFRESH_COST + 2000) {
        const bestRarity = state.recruitPool.length > 0 ? state.recruitPool[0].rarity : 'common';
        // 평범한 풀이면 100G로 refresh (rare+ 노림)
        if (rarityRank(bestRarity) < 2) {
            state.gold -= BALANCE.RECRUIT_REFRESH_COST;
            state.recruitPool = [];
            for (let i = 0; i < 3; i++) state.recruitPool.push(createMerc(state.guildLevel));
            state.recruitPoolGeneratedAt = state.simTimeMin;
        }
    }
    return false;
}

// === 메인 도전 ===
function tryMainRun(strategy) {
    const squad = getMainSquad();
    if (squad.length < 2) return false;
    // Stamina 50+인 강한 용병들로 squad 구성. 60 미만이어도 평균만 OK면 진행.
    const avgStamina = squad.reduce((s, m) => s + m.stamina, 0) / squad.length;
    if (avgStamina < 55) return false;

    // 도전할 zone 결정
    let zone;
    if (state.bossDefeated.cargo && !state.bossDefeated.blackout) zone = 'blackout';
    else if (state.bossDefeated.bp && !state.bossDefeated.cargo) zone = 'cargo';
    else if (!state.bossDefeated.bp) zone = 'bloodpit';
    else return false;

    const currentLv = state.zoneLevel[zone];

    // 진척 알고리즘: 현재 레벨 95%+ 면 다음 레벨, 95% 미만이면 현재 반복
    // 단 currentLv 0이면 무조건 Lv 1 도전
    let zoneLevel;
    if (currentLv === 0) {
        zoneLevel = 1;
    } else if (currentLv >= 10) {
        // 보스 도전
        if (!state.bossDefeated[zone === 'bloodpit' ? 'bp' : zone]) {
            zoneLevel = 10;
        } else {
            return false;
        }
    } else {
        const currentRate = calcSuccessRate(squad, zone, currentLv);
        const upRate = calcSuccessRate(squad, zone, currentLv + 1);
        const minUpRate = strategy === 'safe' ? 0.85 : strategy === 'aggressive' ? 0.6 : 0.75;

        if (upRate >= minUpRate) {
            zoneLevel = currentLv + 1;  // 진척
        } else if (currentRate >= 0.7) {
            zoneLevel = currentLv;  // 현재 레벨 반복 (파밍/Bond/친화도)
        } else {
            return false;  // 너무 약함, 휴식
        }
    }

    const successRate = calcSuccessRate(squad, zone, zoneLevel);
    // 사망 방지: 너무 위험한 도전(50% 미만)은 안 함
    if (successRate < 0.55 && strategy !== 'aggressive') return false;

    state.stats.mainRuns++;
    const success = rng() < successRate;
    const reward = BALANCE.ZONE_REWARDS[zone];
    const zoneKey = zone === 'bloodpit' ? 'bp' : zone;

    if (success) {
        state.stats.mainSuccess++;
        const pitMult = zone === 'bloodpit' ? 1.5 : 1.0;
        const gold = Math.round(reward.baseGold * zoneLevel * 1.5 * pitMult);
        const xp = Math.round(reward.baseXp * zoneLevel * 1.5);
        state.gold += gold;
        state.stats.totalGoldEarned += gold;
        state.guildXp += xp;
        state.stats.totalXpEarned += xp;
        state.reputation += BALANCE.REPUTATION_GAIN.main_success;

        for (const merc of squad) {
            merc.xp += Math.round(xp / squad.length);
            checkLevelUp(merc);
            merc.stamina -= BALANCE.STAMINA_DEPLETION['main_' + zoneKey];
            merc.zoneAffinity[zone].xp += rngInt(30, 50);
            checkAffinityLevelUp(merc, zone);
        }
        for (let i = 0; i < squad.length; i++) {
            for (let j = i + 1; j < squad.length; j++) {
                const key = [squad[i].id, squad[j].id].sort().join('_');
                state.bonds[key] = Math.min(100, (state.bonds[key] || 0) + BALANCE.BOND_GAIN.main_success);
            }
        }

        if (zoneLevel === 10 && !state.bossDefeated[zoneKey]) {
            state.bossDefeated[zoneKey] = true;
            state.stats.bossKills++;
            state.reputation += BALANCE.REPUTATION_GAIN.boss_kill;
            // 보스 클리어 시 신규 zone 1레벨 도달
            if (zoneKey === 'bp' && state.zoneLevel.cargo === 0) state.zoneLevel.cargo = 1;
            else if (zoneKey === 'cargo' && state.zoneLevel.blackout === 0) state.zoneLevel.blackout = 1;
        }
        if (zoneLevel > state.zoneLevel[zone]) state.zoneLevel[zone] = zoneLevel;
        checkGuildLevelUp();
    } else {
        state.stats.mainFail++;
        state.reputation += BALANCE.REPUTATION_GAIN.main_fail;
        for (const merc of squad) {
            merc.stamina -= 15;
            const deathChance = reward.deathChance * (1 - successRate) * 0.6; // 보호적
            if (rng() < deathChance) {
                merc.alive = false;
                state.stats.deaths++;
                state.reputation += BALANCE.REPUTATION_GAIN.merc_death;
            }
        }
    }
    return true;
}

// === 서브 파견 ===
function trySubExpedition() {
    const deployMax = BALANCE.ROSTER_LIMITS[state.guildLevel].deploy;
    const activeSlots = state.activeExpeditions.length;
    if (activeSlots >= deployMax) return false;

    const available = getSubAvailable();
    if (available.length < 2) return false;

    // 클리어된 zone 중 골드 효율 좋은 곳
    const zoneOptions = [];
    if (state.zoneLevel.bloodpit > 0) zoneOptions.push('bloodpit');
    if (state.zoneLevel.cargo > 0) zoneOptions.push('cargo');
    if (state.zoneLevel.blackout > 0) zoneOptions.push('blackout');
    if (zoneOptions.length === 0) return false;

    // 약간의 다양성을 위해 가중 랜덤
    const weights = { bloodpit: 0.5, cargo: 0.3, blackout: 0.2 };
    const filteredWeights = {};
    zoneOptions.forEach(z => filteredWeights[z] = weights[z]);
    const zone = rngWeighted(filteredWeights);

    const zoneLevel = state.zoneLevel[zone];

    // 서브 파티 구성 (약한 것부터)
    available.sort((a, b) => calcMercPower(a) - calcMercPower(b));
    const partySize = Math.min(3, available.length);
    const party = available.slice(0, partySize);

    // 서브 파견 적정성 체크 (50% 이상)
    const successRate = calcSuccessRate(party, zone, zoneLevel);
    if (successRate < 0.5) {
        // 강한 용병 끼워주기
        const stronger = available.slice(-1);
        if (stronger.length > 0 && !party.includes(stronger[0])) {
            party.push(stronger[0]);
        }
    }

    const power = calcPartyPower(party);
    const recommended = BalanceHelper.getZoneRecommendedPower(zone, zoneLevel);
    const baseTime = BALANCE.ZONE_REWARDS[zone].expeditionTimeMin;
    const time = baseTime * Math.max(0.7, zoneLevel * 0.6) / Math.max(0.7, power / recommended);

    state.activeExpeditions.push({
        party, zone, zoneLevel,
        timeMin: Math.max(1, time),
        completesAtMin: state.simTimeMin + Math.max(1, time)
    });
    return true;
}

function processExpeditions() {
    const completed = state.activeExpeditions.filter(e => state.simTimeMin >= e.completesAtMin);
    for (const exp of completed) {
        resolveSubExpedition(exp);
        state.activeExpeditions.splice(state.activeExpeditions.indexOf(exp), 1);
    }
}

function resolveSubExpedition(exp) {
    const { party, zone, zoneLevel } = exp;
    state.stats.subRuns++;
    const successRate = calcSuccessRate(party, zone, zoneLevel);
    const success = rng() < successRate;
    const reward = BALANCE.ZONE_REWARDS[zone];
    const zoneKey = zone === 'bloodpit' ? 'bp' : zone;

    if (success) {
        state.stats.subSuccess++;
        const gold = Math.round(reward.baseGold * zoneLevel);
        const xp = Math.round(reward.baseXp * zoneLevel);
        state.gold += gold;
        state.stats.totalGoldEarned += gold;
        state.guildXp += xp;
        state.stats.totalXpEarned += xp;
        for (const merc of party) {
            merc.xp += Math.round(xp / party.length);
            checkLevelUp(merc);
            merc.zoneAffinity[zone].xp += rngInt(15, 25);
            checkAffinityLevelUp(merc, zone);
            merc.stamina -= BALANCE.STAMINA_DEPLETION['sub_' + zoneKey];
        }
        for (let i = 0; i < party.length; i++) {
            for (let j = i + 1; j < party.length; j++) {
                const key = [party[i].id, party[j].id].sort().join('_');
                state.bonds[key] = Math.min(100, (state.bonds[key] || 0) + BALANCE.BOND_GAIN.sub_success);
            }
        }
        checkGuildLevelUp();
    } else {
        state.stats.subFail++;
        for (const merc of party) {
            merc.stamina -= 20;
            const deathChance = reward.deathChance * (1 - successRate) * 0.4;
            if (rng() < deathChance) {
                merc.alive = false;
                state.stats.deaths++;
            }
        }
    }
}

// === 레벨업 ===
function checkLevelUp(merc) {
    while (merc.level < BALANCE.MERC_MAX_LEVEL) {
        const needed = BalanceHelper.getMercXpToNext(merc.level);
        if (merc.xp >= needed) { merc.xp -= needed; merc.level++; } else break;
    }
}
function checkAffinityLevelUp(merc, zone) {
    const aff = merc.zoneAffinity[zone];
    while (aff.level < BALANCE.AFFINITY_MAX_LEVEL - 1) {
        const needed = BALANCE.AFFINITY_XP_TABLE[aff.level];
        if (aff.xp >= needed) { aff.xp -= needed; aff.level++; aff.points++; } else break;
    }
}
function checkGuildLevelUp() {
    while (state.guildLevel < BALANCE.GUILD_MAX_LEVEL) {
        const needed = BALANCE.GUILD_LEVEL_XP[state.guildLevel];
        if (state.guildXp >= needed) { state.guildXp -= needed; state.guildLevel++; } else break;
    }
}

// === 피로도 회복 ===
function recoverStamina(min) {
    const recovery = BALANCE.STAMINA_RECOVERY_BASE +
        (state.guildHall.recovery >= 3 ? BALANCE.STAMINA_RECOVERY_C_POOL : 0);
    for (const merc of state.roster) {
        if (!merc.alive || isOnExpedition(merc)) continue;
        merc.stamina = Math.min(100, merc.stamina + recovery * min);
    }
}

// === 시설 해금 ===
function tryUnlockFacility() {
    const FACILITIES = [
        { key: 'forge', cost: 500, lv: 2 }, { key: 'auction', cost: 800, lv: 3 },
        { key: 'training', cost: 1200, lv: 4 }, { key: 'temple', cost: 1500, lv: 5 },
        { key: 'intel', cost: 2000, lv: 6 }, { key: 'eliteRecruit', cost: 3000, lv: 7 },
        { key: 'vault', cost: 4000, lv: 8 }
    ];
    for (const f of FACILITIES) {
        if (state.guildLevel >= f.lv && !state.unlockedFacilities.includes(f.key) && state.gold >= f.cost + 1000) {
            state.gold -= f.cost;
            state.unlockedFacilities.push(f.key);
            state.stats.facilitiesUnlocked++;
        }
    }
}

// === 길드회관 업그레이드 (우선순위 전략) ===
function tryUpgradeGuildHall() {
    // 우선순위: A 운영(파티 인원↑) > D 자동화(성공률↑) > C 휴식(스태미나↑) > E 정보 > B 인프라
    const priority = ['operations', 'automation', 'recovery', 'intel', 'infrastructure'];
    for (const cat of priority) {
        const stage = state.guildHall[cat];
        if (stage >= 12) continue;
        const cost = BalanceHelper.getGuildHallCost(stage + 1);
        const reserveGold = state.guildLevel < 4 ? 2000 : 10000;
        if (state.gold >= cost + reserveGold) {
            state.gold -= cost;
            state.guildHall[cat]++;
            state.stats.guildHallStages++;
            return true;
        }
    }
    return false;
}

function formatTime(min) {
    const h = Math.floor(min / 60);
    const m = Math.floor(min % 60);
    return `${h}:${m.toString().padStart(2, '0')}`;
}

// === 메인 시뮬 루프 ===
function runSimulation(strategy, hours) {
    state = createGameState();
    const endMin = hours * 60;
    let lastSnapshot = 0;

    while (state.simTimeMin < endMin) {
        state.simTimeMin++;
        recoverStamina(1);
        processExpeditions();

        // 매분 모집 (로스터 안 차 있으면)
        const aliveCount = getAliveRoster().length;
        if (aliveCount < BALANCE.ROSTER_LIMITS[state.guildLevel].max) {
            tryHire();
        }

        // 매분 서브 시도
        while (trySubExpedition()) {}

        // 3분마다 메인 도전 시도 (stamina 검사가 있어서 자주 시도 OK)
        if (state.simTimeMin % 3 === 0 && aliveCount >= 2) {
            tryMainRun(strategy);
        }

        // 시설/길드회관 매 10분
        if (state.simTimeMin % 10 === 0) {
            tryUnlockFacility();
            tryUpgradeGuildHall();
        }

        // 스냅샷 (30분 단위)
        if (state.simTimeMin - lastSnapshot >= 30) {
            const topBond = Math.max(0, ...Object.values(state.bonds));
            if (topBond >= 100 && !state.topBondMaxAt) state.topBondMaxAt = state.simTimeMin;
            const topAffinity = aliveCount > 0 ? Math.max(...state.roster.filter(m => m.alive).map(m =>
                Math.max(m.zoneAffinity.bloodpit.level, m.zoneAffinity.cargo.level, m.zoneAffinity.blackout.level))) : 0;
            if (topAffinity >= 8 && !state.topAffinityLv8At) state.topAffinityLv8At = state.simTimeMin;

            state.timeline.push({
                time: formatTime(state.simTimeMin),
                guildLv: state.guildLevel,
                gold: state.gold,
                roster: aliveCount,
                reputation: Math.round(state.reputation),
                zoneLevel: { ...state.zoneLevel },
                activeExp: state.activeExpeditions.length,
                bonds: Object.keys(state.bonds).filter(k => state.bonds[k] >= 50).length,
                topBond, topAffinity,
                deaths: state.stats.deaths,
                facilities: state.stats.facilitiesUnlocked,
                ghStages: state.stats.guildHallStages
            });
            lastSnapshot = state.simTimeMin;
        }
    }

    return computeValidation(state, strategy);
}

function computeValidation(state, strategy) {
    const mainSuccessRate = state.stats.mainRuns > 0 ? state.stats.mainSuccess / state.stats.mainRuns : 0;
    const subSuccessRate = state.stats.subRuns > 0 ? state.stats.subSuccess / state.stats.subRuns : 0;
    const topBond = Math.max(0, ...Object.values(state.bonds));
    const aliveRoster = getAliveRoster();
    const topAffinity = aliveRoster.reduce((max, m) => Math.max(max,
        m.zoneAffinity.bloodpit.level, m.zoneAffinity.cargo.level, m.zoneAffinity.blackout.level), 0);
    const goldDev = Math.abs(state.gold - 320000) / 320000;

    return {
        strategy,
        summary: {
            duration: formatTime(state.simTimeMin),
            finalGold: state.gold,
            totalEarned: state.stats.totalGoldEarned,
            guildLevel: state.guildLevel,
            mainRuns: state.stats.mainRuns,
            mainSuccess: (mainSuccessRate * 100).toFixed(1) + '%',
            subRuns: state.stats.subRuns,
            subSuccess: (subSuccessRate * 100).toFixed(1) + '%',
            deaths: state.stats.deaths,
            bossKills: state.stats.bossKills,
            facilities: state.stats.facilitiesUnlocked,
            ghStages: state.stats.guildHallStages,
            topBond, topAffinity,
            topBondMaxAt: state.topBondMaxAt ? formatTime(state.topBondMaxAt) : 'N/A',
            roster: aliveRoster.length,
            zoneLevel: state.zoneLevel
        },
        validation: {
            goldCurve:     { actual: state.gold, target: '320K±30%', deviation: (goldDev * 100).toFixed(1) + '%', pass: goldDev < 0.30 },
            guildLv8:      { actual: state.guildLevel, target: 8, pass: state.guildLevel >= 8 },
            mainSuccess:   { actual: (mainSuccessRate * 100).toFixed(1) + '%', target: '70-95%', pass: mainSuccessRate >= 0.70 && mainSuccessRate <= 0.95 },
            deaths:        { actual: state.stats.deaths, target: '≤5', pass: state.stats.deaths <= 5 },
            topBond:       { actual: topBond, target: '≥75', pass: topBond >= 75 },
            topAffinity:   { actual: topAffinity, target: '≥6', pass: topAffinity >= 6 },
            bossKills:     { actual: state.stats.bossKills, target: '≥1', pass: state.stats.bossKills >= 1 },
            facilities:    { actual: state.stats.facilitiesUnlocked, target: '≥7', pass: state.stats.facilitiesUnlocked >= 7 }
        },
        timeline: state.timeline
    };
}

function printReport(result) {
    console.log('\n═══════════════════════════════════════════════');
    console.log(`  시뮬 결과 — 전략: ${result.strategy.toUpperCase()}`);
    console.log('═══════════════════════════════════════════════\n');
    console.log('📊 요약:');
    for (const [k, v] of Object.entries(result.summary)) {
        console.log(`  ${k.padEnd(20)} ${typeof v === 'object' ? JSON.stringify(v) : v}`);
    }
    console.log('\n✅ 검증 결과:');
    let pass = 0;
    for (const [k, v] of Object.entries(result.validation)) {
        const icon = v.pass ? '✅' : '❌';
        if (v.pass) pass++;
        console.log(`  ${icon} ${k.padEnd(20)} actual=${typeof v.actual === 'object' ? JSON.stringify(v.actual) : v.actual} target=${v.target}`);
    }
    console.log(`\n  점수: ${pass}/${Object.keys(result.validation).length}`);
    console.log('\n📈 타임라인 (30분 단위):');
    console.log('  time   lv  gold      roster  rep  zone(BP/Cg/BO)  exp  bonds  tBond  tAff  D  fac  gh');
    for (const t of result.timeline) {
        const zone = `${t.zoneLevel.bloodpit}/${t.zoneLevel.cargo}/${t.zoneLevel.blackout}`;
        console.log(`  ${t.time.padEnd(6)} ${String(t.guildLv).padStart(2)} ${String(t.gold).padStart(8)}   ${String(t.roster).padStart(3)}  ${String(t.reputation).padStart(3)}  ${zone.padEnd(13)}  ${String(t.activeExp).padStart(2)}   ${String(t.bonds).padStart(3)}   ${String(Math.round(t.topBond)).padStart(3)}    ${t.topAffinity}  ${String(t.deaths).padStart(2)}  ${String(t.facilities).padStart(2)}  ${t.ghStages}`);
    }
}

function main() {
    const args = process.argv.slice(2);
    let strategy = 'balanced';
    let hours = 12;
    let seed = 42;
    let runs = 1;
    let compare = null;

    for (let i = 0; i < args.length; i++) {
        if (args[i] === '--strategy') strategy = args[++i];
        else if (args[i] === '--hours') hours = parseFloat(args[++i]);
        else if (args[i] === '--seed') seed = parseInt(args[++i]);
        else if (args[i] === '--runs') runs = parseInt(args[++i]);
        else if (args[i] === '--compare') compare = args[++i].split(',');
    }

    if (compare) {
        for (const s of compare) {
            seedRng(seed);
            _mercIdCounter = 0;
            const result = runSimulation(s, hours);
            printReport(result);
        }
    } else if (runs > 1) {
        console.log(`\n${runs}회 시뮬 평균 (전략: ${strategy})\n`);
        const results = [];
        for (let i = 0; i < runs; i++) {
            seedRng(seed + i);
            _mercIdCounter = 0;
            results.push(runSimulation(strategy, hours));
        }
        const avg = {
            gold: Math.round(results.reduce((s, r) => s + r.summary.finalGold, 0) / runs),
            earned: Math.round(results.reduce((s, r) => s + r.summary.totalEarned, 0) / runs),
            guildLv: (results.reduce((s, r) => s + r.summary.guildLevel, 0) / runs).toFixed(1),
            mainRuns: Math.round(results.reduce((s, r) => s + r.summary.mainRuns, 0) / runs),
            mainSuccess: (results.reduce((s, r) => s + parseFloat(r.summary.mainSuccess), 0) / runs).toFixed(1) + '%',
            subRuns: Math.round(results.reduce((s, r) => s + r.summary.subRuns, 0) / runs),
            deaths: (results.reduce((s, r) => s + r.summary.deaths, 0) / runs).toFixed(1),
            bossKills: (results.reduce((s, r) => s + r.summary.bossKills, 0) / runs).toFixed(1),
            topBond: Math.round(results.reduce((s, r) => s + r.summary.topBond, 0) / runs),
            topAff: (results.reduce((s, r) => s + r.summary.topAffinity, 0) / runs).toFixed(1),
            facilities: (results.reduce((s, r) => s + r.summary.facilities, 0) / runs).toFixed(1),
            ghStages: (results.reduce((s, r) => s + r.summary.ghStages, 0) / runs).toFixed(1)
        };
        const passes = results.filter(r => Object.values(r.validation).filter(v => v.pass).length >= 6).length;
        for (const [k, v] of Object.entries(avg)) {
            console.log(`  ${k.padEnd(15)} ${v}`);
        }
        console.log(`\nPASS (6/8+): ${passes}/${runs}`);
    } else {
        seedRng(seed);
        _mercIdCounter = 0;
        const result = runSimulation(strategy, hours);
        printReport(result);
    }
}

main();
