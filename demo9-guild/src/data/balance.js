// 밸런스 상수 중앙화
// 모든 매직 넘버는 여기서 관리. 변경 시 balance-config.md 동기화 필수.
// 코드 곳곳에서 BALANCE.XXX로 참조.

const BALANCE = {
    // === 시작 자원 ===
    START_GOLD: 500,
    START_GUILD_LEVEL: 1,
    START_FACILITIES: ['recruit', 'storage', 'gate'],

    // === 모집 ===
    BASE_HIRE_COST: 60,
    HIRE_COST_RARITY_MULT: { common: 1.0, uncommon: 1.5, rare: 2.5, epic: 4.0, legendary: 6.0 },
    HIRE_GUILD_LV_MULT: 0.15,
    RECRUIT_REFRESH_COST: 100,
    RECRUIT_FREE_REFRESH_MIN: 10,

    // === 길드 ===
    GUILD_LEVEL_XP: [0, 100, 250, 500, 800, 1200, 1800, 2500],
    GUILD_MAX_LEVEL: 8,
    ROSTER_LIMITS: {
        1: { max: 4, deploy: 2 }, 2: { max: 4, deploy: 2 },
        3: { max: 6, deploy: 3 }, 4: { max: 6, deploy: 3 },
        5: { max: 8, deploy: 4 }, 6: { max: 8, deploy: 4 },
        7: { max: 10, deploy: 5 }, 8: { max: 10, deploy: 5 }
    },

    // === 길드 회관 ===
    GUILD_HALL_BASE_COST: 300,
    GUILD_HALL_COST_MULT: 1.5,
    GUILD_HALL_MAX_STAGE: 12,

    // === 클래스 베이스 (Lv1) ===
    CLASS_BASE: {
        warrior:   { hp: 250, atk: 20, def: 18, spd: 90  },
        rogue:     { hp: 160, atk: 35, def: 8,  spd: 140 },
        mage:      { hp: 120, atk: 45, def: 5,  spd: 50  },
        archer:    { hp: 150, atk: 30, def: 7,  spd: 80  },
        priest:    { hp: 180, atk: 12, def: 10, spd: 60  },
        alchemist: { hp: 170, atk: 15, def: 12, spd: 60  }
    },
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
        common:    { stat: 1.0,  growth: 1.0 },
        uncommon:  { stat: 1.1,  growth: 1.1 },
        rare:      { stat: 1.2,  growth: 1.2 },
        epic:      { stat: 1.35, growth: 1.3 },
        legendary: { stat: 1.5,  growth: 1.5 }
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

    MERC_XP_CURVE: { coef: 50, exp: 1.5 },
    MERC_MAX_LEVEL: 30,

    // === 데미지 / DEF ===
    DEF_MODE: 'percent',  // damage * (1 - def / (def + 100))
    DEF_DENOMINATOR: 100,

    // === 피로도 ===
    STAMINA_MAX: 100,
    STAMINA_RECOVERY_BASE: 2,      // /분 — 자연 회복 강화 (12시간 페이싱 맞춤)
    STAMINA_RECOVERY_C_POOL: 6,    // C 풀 업그레이드 시 추가
    STAMINA_DEPLETION: {
        main_bp: 25, main_cargo: 30, main_blackout: 40,
        sub_bp: 12, sub_cargo: 16, sub_blackout: 20
    },

    // === Bond ===
    BOND_MAX: 100,
    BOND_GAIN: { sub_success: 4, sub_fail: 2, main_success: 8, main_fail: 4 },
    BOND_TIER_THRESHOLDS: [0, 10, 30, 50, 75, 100],
    BOND_TIER_BONUS:      [0, 0.02, 0.05, 0.08, 0.12, 0.15],

    // === 친화도 ===
    AFFINITY_XP_TABLE: [100, 150, 200, 300, 400, 600, 800, 1000, 1500],
    AFFINITY_XP_PER_RUN: { min: 15, max: 50 },
    AFFINITY_MAX_LEVEL: 10,

    // === 평판 ===
    REPUTATION_MAX: 100,
    REPUTATION_GAIN: {
        main_success: 0.5, main_fail: -0.2,
        boss_kill: 5, merc_death: -1,
        event_positive: 3, event_negative: -2
    },

    // === NPC ===
    NPC_FAVOR_MAX: 100,
    NPC_FAVOR_GAIN: { trade: 0.5, quest: 8 },
    NPC_FAVOR_TIERS: [0, 10, 30, 60, 100],

    // === RunSimulator ===
    RUN_SIM: {
        POWER_LEVEL_GROWTH: 0.05,
        POWER_DEFENSE_MULT: 0.02,
        POWER_RARITY_BONUS: { common: 1.0, uncommon: 1.05, rare: 1.1, epic: 1.2, legendary: 1.35 },
        ZONE_BASE_POWER: { bloodpit: 200, cargo: 300, blackout: 450 },
        ZONE_POWER_SCALE_PRIMARY: 0.25,
        ZONE_POWER_SCALE_SECONDARY: 0.03,
        SUCCESS_SIGMOID_K: 6,
        SUCCESS_SIGMOID_MID: 0.7
    },

    // === 구역 보상 ===
    // expeditionTimeMin = Lv1 서브 파견 1회 기본 시간 (분). Lv 비례 증가.
    ZONE_REWARDS: {
        bloodpit:  { baseGold: 60,  baseXp: 30, deathChance: 0.12, expeditionTimeMin: 1.5 },
        cargo:     { baseGold: 80,  baseXp: 35, deathChance: 0.10, expeditionTimeMin: 4   },
        blackout:  { baseGold: 100, baseXp: 40, deathChance: 0.18, expeditionTimeMin: 8   }
    },
    PIT_GAUGE_MAX_MULT: 2.0,

    // === 적 스탯 ===
    ENEMY_BASE: {
        normal: { hp: 80,  atk: 12, def: 3,  threat: 1.0 },
        elite:  { hp: 180, atk: 25, def: 6,  threat: 2.0 },
        guard:  { hp: 280, atk: 35, def: 12, threat: 3.0 }
    },
    ENEMY_SCALING: { hpPerLevel: 0.25, atkPerLevel: 0.20, defAddPerLevel: 2 },

    // === 경매장 (v2 신메커닉) ===
    // 전체 정의: docs/systems/auction.md §11
    AUCTION: {
        // 참가
        TIME_TOKENS: 2,
        ENTRY_FEE: { min: 50, max: 100 },
        ROUND_BUDGET_PCT: 0.25,
        ROUND_BUDGET_MAX: 5000,
        // 1단계 — 비공개 입찰
        ITEM_COUNT: 3,
        NPC_BIDDER_COUNT_BY_RARITY: { common: 1.0, uncommon: 1.2, rare: 2.0, epic: 2.5, legendary: 3.0 },
        NPC_BID_MEAN_MULT:        { common: 0.95, uncommon: 0.95, rare: 1.05, epic: 1.15, legendary: 1.30 },
        NPC_BID_STDDEV_PCT:       { common: 0.15, uncommon: 0.15, rare: 0.25, epic: 0.40, legendary: 0.60 },
        NPC_NO_BID_CHANCE: 0.20,
        PLAYER_BID_MIN_MULT: 0.5,
        PLAYER_BID_MAX_MULT: 2.0,
        TIE_GOES_TO_NPC: true,
        // 2단계 — 손님 판매
        CUSTOMER_PER_ROUND: { min: 1, max: 3 },
        CUSTOMER_TIMER_SEC: { common: 10, uncommon: 12, rare: 15, epic: 18, legendary: 20 },
        CUSTOMER_SATISFY_MULT_RANGE: { min: 0.85, max: 0.95 },
        CUSTOMER_PRICE_ADJUST_TRIES: 2,
        CUSTOMER_PREFERENCE_REVEAL: { category_at: 3, price_range_at: 7, dislike_at: 15 },
        // 시세
        MARKET_MODIFIER_RANGE: { min: 0.9, max: 1.3 },
        // 위작/저주 (Backlog — MVP 미구현)
        FAKE_TRIGGER_DURATION_H: 24,
        FAKE_RECOVERY_MULT: { min: 0.10, max: 0.20 },
        IDENTIFY_BUTTON_COST: 30
    },

    // === 시간 ===
    TIME_OFFLINE_MAX_HOURS: 4,
    TIME_EVENT_INTERVAL_MIN: 30
};

// 헬퍼 함수
const BalanceHelper = {
    getMercXpToNext(level) {
        return Math.round(BALANCE.MERC_XP_CURVE.coef * Math.pow(level, BALANCE.MERC_XP_CURVE.exp));
    },
    getClassStats(classKey, level, rarity) {
        const base = BALANCE.CLASS_BASE[classKey];
        const growth = BALANCE.CLASS_GROWTH[classKey];
        const mult = BALANCE.RARITY_MULT[rarity];
        return {
            hp:  Math.round((base.hp  + growth.hp  * (level - 1)) * mult.stat),
            atk: Math.round((base.atk + growth.atk * (level - 1)) * mult.stat),
            def: Math.round((base.def + growth.def * (level - 1)) * mult.stat),
            spd: base.spd
        };
    },
    calcDamage(rawAtk, def) {
        if (BALANCE.DEF_MODE === 'percent') {
            return rawAtk * (1 - def / (def + BALANCE.DEF_DENOMINATOR));
        }
        return Math.max(1, rawAtk - def);
    },
    getEnemyStats(type, zoneLevel) {
        const base = BALANCE.ENEMY_BASE[type];
        const scale = BALANCE.ENEMY_SCALING;
        return {
            hp:  base.hp  * (1 + (zoneLevel - 1) * scale.hpPerLevel),
            atk: base.atk * (1 + (zoneLevel - 1) * scale.atkPerLevel),
            def: base.def + (zoneLevel - 1) * scale.defAddPerLevel,
            threat: base.threat
        };
    },
    getZoneRecommendedPower(zone, zoneLevel) {
        const rs = BALANCE.RUN_SIM;
        return rs.ZONE_BASE_POWER[zone]
            * (1 + (zoneLevel - 1) * rs.ZONE_POWER_SCALE_PRIMARY)
            * (1 + (zoneLevel - 1) * rs.ZONE_POWER_SCALE_SECONDARY);
    },
    getHireCost(rarity, guildLv) {
        return Math.round(BALANCE.BASE_HIRE_COST
            * BALANCE.HIRE_COST_RARITY_MULT[rarity]
            * (1 + guildLv * BALANCE.HIRE_GUILD_LV_MULT));
    },
    getGuildHallCost(stage) {
        return Math.round(BALANCE.GUILD_HALL_BASE_COST * Math.pow(BALANCE.GUILD_HALL_COST_MULT, stage - 1));
    }
};

// 모듈 export (Node.js + 브라우저 양쪽 지원)
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { BALANCE, BalanceHelper };
}
