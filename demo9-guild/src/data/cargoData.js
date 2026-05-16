// ══════════════════════════════════════════════════════════════════
// Cargo 구역 전용 데이터 — 나락식 층 진행 + RTS + 기차방어
// ══════════════════════════════════════════════════════════════════

// ─── 카드 시스템 (뱀서식 레벨업) ──────────────────────────────────
// 스탯 카드 8종 × 3티어 + 효과 카드 8장 + 도박 카드 8장 = 40장

const CARGO_CARDS = {
    // ── 스탯 카드 (8종 × 3티어 = 24장) ──
    // 화력 집중
    atk_n:   { name: '화력 집중',     rarity: 'normal',    category: 'stat', statGroup: 'atk',   desc: 'ATK +3%',       apply: (ctx) => { ctx.allies.forEach(u => { u.atk = Math.floor(u.atk * 1.03); }); } },
    atk_r:   { name: '화력 집중 II',  rarity: 'rare',      category: 'stat', statGroup: 'atk',   desc: 'ATK +5%',       apply: (ctx) => { ctx.allies.forEach(u => { u.atk = Math.floor(u.atk * 1.05); }); }, upgradeFrom: 'atk_n' },
    atk_l:   { name: '화력 집중 III', rarity: 'legendary', category: 'stat', statGroup: 'atk',   desc: 'ATK +8%',       apply: (ctx) => { ctx.allies.forEach(u => { u.atk = Math.floor(u.atk * 1.08); }); }, upgradeFrom: 'atk_r' },

    // 외벽 보강
    wall_n:  { name: '외벽 보강',     rarity: 'normal',    category: 'stat', statGroup: 'wall',  desc: '외벽 HP +3%',   apply: (ctx) => { ctx.train.wallHp += Math.floor(ctx.train.wallMaxHp * 0.03); ctx.train.wallMaxHp = Math.floor(ctx.train.wallMaxHp * 1.03); } },
    wall_r:  { name: '외벽 보강 II',  rarity: 'rare',      category: 'stat', statGroup: 'wall',  desc: '외벽 HP +5%',   apply: (ctx) => { ctx.train.wallHp += Math.floor(ctx.train.wallMaxHp * 0.05); ctx.train.wallMaxHp = Math.floor(ctx.train.wallMaxHp * 1.05); }, upgradeFrom: 'wall_n' },
    wall_l:  { name: '외벽 보강 III', rarity: 'legendary', category: 'stat', statGroup: 'wall',  desc: '외벽 HP +8%',   apply: (ctx) => { ctx.train.wallHp += Math.floor(ctx.train.wallMaxHp * 0.08); ctx.train.wallMaxHp = Math.floor(ctx.train.wallMaxHp * 1.08); }, upgradeFrom: 'wall_r' },

    // 강화 장갑
    def_n:   { name: '강화 장갑',     rarity: 'normal',    category: 'stat', statGroup: 'def',   desc: 'DEF +1',        apply: (ctx) => { ctx.allies.forEach(u => { u.def += 1; }); } },
    def_r:   { name: '강화 장갑 II',  rarity: 'rare',      category: 'stat', statGroup: 'def',   desc: 'DEF +3',        apply: (ctx) => { ctx.allies.forEach(u => { u.def += 3; }); }, upgradeFrom: 'def_n' },
    def_l:   { name: '강화 장갑 III', rarity: 'legendary', category: 'stat', statGroup: 'def',   desc: 'DEF +5',        apply: (ctx) => { ctx.allies.forEach(u => { u.def += 5; }); }, upgradeFrom: 'def_r' },

    // 빠른 손
    aspd_n:  { name: '빠른 손',       rarity: 'normal',    category: 'stat', statGroup: 'aspd',  desc: '공속 +5%',      apply: (ctx) => { ctx.allies.forEach(u => { u.attackSpeed = Math.floor(u.attackSpeed * 0.95); }); } },
    aspd_r:  { name: '빠른 손 II',    rarity: 'rare',      category: 'stat', statGroup: 'aspd',  desc: '공속 +8%',      apply: (ctx) => { ctx.allies.forEach(u => { u.attackSpeed = Math.floor(u.attackSpeed * 0.92); }); }, upgradeFrom: 'aspd_n' },
    aspd_l:  { name: '빠른 손 III',   rarity: 'legendary', category: 'stat', statGroup: 'aspd',  desc: '공속 +12%',     apply: (ctx) => { ctx.allies.forEach(u => { u.attackSpeed = Math.floor(u.attackSpeed * 0.88); }); }, upgradeFrom: 'aspd_r' },

    // 예리한 눈
    crit_n:  { name: '예리한 눈',     rarity: 'normal',    category: 'stat', statGroup: 'crit',  desc: '크리 +2%',      apply: (ctx) => { ctx.allies.forEach(u => { u.critRate = Math.min(0.8, u.critRate + 0.02); }); } },
    crit_r:  { name: '예리한 눈 II',  rarity: 'rare',      category: 'stat', statGroup: 'crit',  desc: '크리 +4%',      apply: (ctx) => { ctx.allies.forEach(u => { u.critRate = Math.min(0.8, u.critRate + 0.04); }); }, upgradeFrom: 'crit_n' },
    crit_l:  { name: '예리한 눈 III', rarity: 'legendary', category: 'stat', statGroup: 'crit',  desc: '크리 +7%',      apply: (ctx) => { ctx.allies.forEach(u => { u.critRate = Math.min(0.8, u.critRate + 0.07); }); }, upgradeFrom: 'crit_r' },

    // 두꺼운 강판 (외벽 방어)
    wdef_n:  { name: '두꺼운 강판',     rarity: 'normal',    category: 'stat', statGroup: 'wdef', desc: '외벽 방어 +1',  apply: (ctx) => { ctx.train.wallDef += 1; } },
    wdef_r:  { name: '두꺼운 강판 II',  rarity: 'rare',      category: 'stat', statGroup: 'wdef', desc: '외벽 방어 +2',  apply: (ctx) => { ctx.train.wallDef += 2; }, upgradeFrom: 'wdef_n' },
    wdef_l:  { name: '두꺼운 강판 III', rarity: 'legendary', category: 'stat', statGroup: 'wdef', desc: '외벽 방어 +4',  apply: (ctx) => { ctx.train.wallDef += 4; }, upgradeFrom: 'wdef_r' },

    // 수리 나노봇
    repair_n: { name: '수리 나노봇',     rarity: 'normal',    category: 'stat', statGroup: 'repair', desc: '외벽 15초당 1% 수리', apply: (ctx) => { ctx.train.autoRepairInterval = Math.min(ctx.train.autoRepairInterval || 15000, 15000); ctx.train.autoRepairPercent = (ctx.train.autoRepairPercent || 0) + 0.01; } },
    repair_r: { name: '수리 나노봇 II',  rarity: 'rare',      category: 'stat', statGroup: 'repair', desc: '외벽 10초당 1% 수리', apply: (ctx) => { ctx.train.autoRepairInterval = Math.min(ctx.train.autoRepairInterval || 15000, 10000); ctx.train.autoRepairPercent = (ctx.train.autoRepairPercent || 0) + 0.01; }, upgradeFrom: 'repair_n' },
    repair_l: { name: '수리 나노봇 III', rarity: 'legendary', category: 'stat', statGroup: 'repair', desc: '외벽 7초당 1% 수리',  apply: (ctx) => { ctx.train.autoRepairInterval = Math.min(ctx.train.autoRepairInterval || 15000, 7000); ctx.train.autoRepairPercent = (ctx.train.autoRepairPercent || 0) + 0.01; }, upgradeFrom: 'repair_r' },

    // 응급 치료
    heal_n:  { name: '응급 치료',     rarity: 'normal',    category: 'stat', statGroup: 'heal',  desc: '파티 HP 5% 회복',  apply: (ctx) => { ctx.allies.forEach(u => { u.hp = Math.min(u.maxHp, u.hp + Math.floor(u.maxHp * 0.05)); }); } },
    heal_r:  { name: '응급 치료 II',  rarity: 'rare',      category: 'stat', statGroup: 'heal',  desc: '파티 HP 8% 회복',  apply: (ctx) => { ctx.allies.forEach(u => { u.hp = Math.min(u.maxHp, u.hp + Math.floor(u.maxHp * 0.08)); }); }, upgradeFrom: 'heal_n' },
    heal_l:  { name: '응급 치료 III', rarity: 'legendary', category: 'stat', statGroup: 'heal',  desc: '파티 HP 12% 회복', apply: (ctx) => { ctx.allies.forEach(u => { u.hp = Math.min(u.maxHp, u.hp + Math.floor(u.maxHp * 0.12)); }); }, upgradeFrom: 'heal_r' },

    // ── 효과 카드 (8장, 희귀도 고정) ──
    barricade:    { name: '바리케이드', rarity: 'rare',      category: 'effect', desc: '침입 시 적 1.5초 기절',     apply: (ctx) => { ctx.train.barricadeStun = 1500; } },
    elec_barrier: { name: '전기 장벽', rarity: 'rare',      category: 'effect', desc: '외벽 타격 적 0.5초 스턴',   apply: (ctx) => { ctx.train.wallStun = 500; } },
    auto_turret:  { name: '자동 포탑', rarity: 'rare',      category: 'effect', desc: '가장 가까운 적 자동 공격',  apply: (ctx) => { ctx.train.turretActive = true; ctx.train.turretDmg = Math.floor(ctx.allies[0] ? ctx.allies[0].atk * 0.5 : 15); } },
    flamethrower: { name: '화염 방사', rarity: 'rare',      category: 'effect', desc: '침입 적 AoE 피해',          apply: (ctx) => { ctx.train.flameDmg = Math.floor(ctx.allies[0] ? ctx.allies[0].atk * 0.8 : 20); } },
    thorns:       { name: '가시 장갑', rarity: 'normal',    category: 'effect', desc: '외벽 타격 시 반사 3%',      apply: (ctx) => { ctx.train.thornPercent = (ctx.train.thornPercent || 0) + 0.03; } },
    trap:         { name: '함정 설치', rarity: 'normal',    category: 'effect', desc: '침입 적 DOT 2초',           apply: (ctx) => { ctx.train.trapDot = true; ctx.train.trapDuration = 2000; } },
    unsinkable:   { name: '불침함',   rarity: 'legendary', category: 'effect', desc: '외벽 HP 1 이하 안 떨어짐 (3초, 쿨 90초)', apply: (ctx) => { ctx.train.unsinkable = true; ctx.train.unsinkableDuration = 3000; ctx.train.unsinkableCooldown = 90000; } },
    last_defense: { name: '최종 방어선', rarity: 'legendary', category: 'effect', desc: '전멸 시 전원 3초 부활 (런 1회)', apply: (ctx) => { ctx.train.lastDefense = true; } },

    // ── 도박 카드 (8장, 리스크+리워드) ──
    glass_cannon:  { name: '유리 대포',       rarity: 'rare',      category: 'gamble', desc: 'ATK +10% BUT 캐릭터 HP -15%', apply: (ctx) => { ctx.allies.forEach(u => { u.atk = Math.floor(u.atk * 1.10); u.maxHp = Math.floor(u.maxHp * 0.85); u.hp = Math.min(u.hp, u.maxHp); }); } },
    overcharge:    { name: '폭주 기관',       rarity: 'rare',      category: 'gamble', desc: '공속 +15% BUT 외벽 자동 수리 비활성화', apply: (ctx) => { ctx.allies.forEach(u => { u.attackSpeed = Math.floor(u.attackSpeed * 0.85); }); ctx.train.autoRepairPercent = 0; } },
    gambler_dice:  { name: '도박꾼의 주사위', rarity: 'rare',      category: 'gamble', desc: '50% → 랜덤 스탯 +10% / 50% → -5%', apply: (ctx) => { const lucky = Math.random() < 0.5; const mult = lucky ? 1.10 : 0.95; ctx.allies.forEach(u => { u.atk = Math.floor(u.atk * mult); u.def = Math.floor(u.def * mult); }); ctx._gambleResult = lucky ? '성공! +10%' : '실패... -5%'; } },
    blood_pact:    { name: '피의 계약',       rarity: 'legendary', category: 'gamble', desc: 'ATK +12% BUT 매 30초 파티 HP 3% 감소', apply: (ctx) => { ctx.allies.forEach(u => { u.atk = Math.floor(u.atk * 1.12); }); ctx.train.bloodPact = true; ctx.train.bloodPactInterval = 30000; ctx.train.bloodPactPercent = 0.03; } },
    self_destruct: { name: '자폭 코어',       rarity: 'legendary', category: 'gamble', desc: '기차 HP 20%↓ 시 적 전체 25% 폭발 BUT 외벽 -10% 즉시', apply: (ctx) => { ctx.train.selfDestruct = true; ctx.train.selfDestructThreshold = 0.2; ctx.train.selfDestructDmg = 0.25; ctx.train.wallHp = Math.floor(ctx.train.wallHp * 0.9); } },
    all_in:        { name: '올인',           rarity: 'legendary', category: 'gamble', desc: '다음 레벨업 선택지 전부 전설 BUT 보유 카드 1장 랜덤 삭제', apply: (ctx) => { ctx.train.allInNextLevel = true; if (ctx.heldCards.length > 0) { const idx = Math.floor(Math.random() * ctx.heldCards.length); ctx.heldCards.splice(idx, 1); } } },
    looter:        { name: '약탈자',         rarity: 'normal',    category: 'gamble', desc: '보상 +15% BUT 적 수 +10%', apply: (ctx) => { ctx.train.lootBonus = (ctx.train.lootBonus || 0) + 0.15; ctx.train.enemyCountMult = (ctx.train.enemyCountMult || 1) * 1.1; } },
    thiefs_luck:   { name: '도둑의 운',       rarity: 'normal',    category: 'gamble', desc: '전설 등장률 2배 BUT 일반 카드 효과 -50%', apply: (ctx) => { ctx.train.legendaryMult = (ctx.train.legendaryMult || 1) * 2; ctx.train.normalEffectMult = (ctx.train.normalEffectMult || 1) * 0.5; } }
};

const CARGO_CARD_KEYS = Object.keys(CARGO_CARDS);

// ─── 카드 등장 확률 (층별) ────────────────────────────────────
const CARGO_RARITY_BY_FLOOR = [
    { maxFloor: 10,  normal: 0.70, rare: 0.25, legendary: 0.05 },
    { maxFloor: 25,  normal: 0.55, rare: 0.35, legendary: 0.10 },
    { maxFloor: 50,  normal: 0.40, rare: 0.40, legendary: 0.20 },
    { maxFloor: 99,  normal: 0.25, rare: 0.45, legendary: 0.30 }
];

function getCargoRarityWeights(floor) {
    for (const tier of CARGO_RARITY_BY_FLOOR) {
        if (floor <= tier.maxFloor) return tier;
    }
    return CARGO_RARITY_BY_FLOOR[CARGO_RARITY_BY_FLOOR.length - 1];
}

// 3택1 카드 생성 (중복 방지 + 업그레이드 형태 허용)
function generateCargoLevelUpCards(floor, heldCardIds, options = {}) {
    const count = options.choiceCount || 3;
    const forceAllLegendary = options.forceAllLegendary || false;
    const legendaryMult = options.legendaryMult || 1;
    const weights = getCargoRarityWeights(floor);

    const choices = [];
    const usedStatGroups = new Set(); // 3택1 중복 방지 (같은 카드 2장 이상 방지)

    const pool = CARGO_CARD_KEYS.filter(k => {
        const card = CARGO_CARDS[k];
        // 업그레이드: 하위 티어 보유 시 상위 등장 가능
        if (card.upgradeFrom && !heldCardIds.includes(card.upgradeFrom)) {
            // 하위 미보유 시 상위만 나오면 안 됨 (스탯카드의 경우)
            if (card.category === 'stat') return false;
        }
        return true;
    });

    const attempts = 100;
    for (let i = 0; i < attempts && choices.length < count; i++) {
        // 희귀도 결정
        let targetRarity;
        if (forceAllLegendary) {
            targetRarity = 'legendary';
        } else {
            const roll = Math.random();
            const legendaryChance = Math.min(0.6, weights.legendary * legendaryMult);
            const rareChance = weights.rare;
            if (roll < legendaryChance) targetRarity = 'legendary';
            else if (roll < legendaryChance + rareChance) targetRarity = 'rare';
            else targetRarity = 'normal';
        }

        // 해당 희귀도 카드 필터
        const candidates = pool.filter(k => {
            const card = CARGO_CARDS[k];
            if (card.rarity !== targetRarity) return false;
            if (choices.includes(k)) return false;
            // 같은 statGroup은 3택1에 1번만
            if (card.statGroup && usedStatGroups.has(card.statGroup)) return false;
            return true;
        });

        if (candidates.length === 0) continue;
        const pick = candidates[Math.floor(Math.random() * candidates.length)];
        const card = CARGO_CARDS[pick];
        choices.push(pick);
        if (card.statGroup) usedStatGroups.add(card.statGroup);
    }

    return choices;
}

// ─── 모디파이어 시스템 (다키스트식) ──────────────────────────────

const CARGO_POSITIVE_MODIFIERS = [
    { id: 'iron_wall',       name: '강철 외벽',   desc: '기차 외벽 HP +15%',              apply: (ctx) => { ctx.train.wallMaxHp = Math.floor(ctx.train.wallMaxHp * 1.15); ctx.train.wallHp = ctx.train.wallMaxHp; } },
    { id: 'combat_instinct', name: '전투 본능',   desc: '파티 ATK +8%',                   apply: (ctx) => { ctx.allies.forEach(u => { u.atk = Math.floor(u.atk * 1.08); }); } },
    { id: 'quick_hands',     name: '빠른 손',     desc: '공격속도 +10%',                  apply: (ctx) => { ctx.allies.forEach(u => { u.attackSpeed = Math.floor(u.attackSpeed * 0.9); }); } },
    { id: 'first_aid',       name: '응급 키트',   desc: '30초마다 파티 HP 5% 회복',       apply: (ctx) => { ctx.train.periodicHeal = true; ctx.train.periodicHealInterval = 30000; ctx.train.periodicHealPercent = 0.05; } },
    { id: 'armor_up',        name: '강화 장갑',   desc: '파티 DEF +5',                    apply: (ctx) => { ctx.allies.forEach(u => { u.def += 5; }); } },
    { id: 'mechanic',        name: '수리공',      desc: '외벽 15초당 1% 자동 수리',       apply: (ctx) => { ctx.train.autoRepairInterval = 15000; ctx.train.autoRepairPercent = (ctx.train.autoRepairPercent || 0) + 0.01; } },
    { id: 'keen_eye',        name: '예리한 눈',   desc: '크리 확률 +5%',                  apply: (ctx) => { ctx.allies.forEach(u => { u.critRate = Math.min(0.8, u.critRate + 0.05); }); } },
    { id: 'double_wall',     name: '이중 장벽',   desc: '외벽 파괴 후 내부 벽 (본체 30%)', apply: (ctx) => { ctx.train.doubleWall = true; ctx.train.innerWallHp = Math.floor(ctx.train.wallMaxHp * 0.3); } },
    { id: 'supply_drop',     name: '보급 지원',   desc: '시작 레벨 +1 (즉시 카드 1장)',   apply: (ctx) => { ctx.train.startBonus = true; } },
    { id: 'focus_fire',      name: '집중 화력',   desc: '동일 적 집중 시 추가 피해 +10%', apply: (ctx) => { ctx.train.focusFireBonus = 0.10; } }
];

const CARGO_NEGATIVE_MODIFIERS = [
    { id: 'mass_invasion',   name: '대규모 침공', desc: '적 수 +15%',                     apply: (ctx) => { ctx.train.enemyCountMult = (ctx.train.enemyCountMult || 1) * 1.15; } },
    { id: 'strong_enemies',  name: '강화 적',     desc: '적 HP/ATK +10%',                 apply: (ctx) => { ctx.train.enemyStatMult = (ctx.train.enemyStatMult || 1) * 1.10; } },
    { id: 'weak_wall',       name: '약한 외벽',   desc: '외벽 HP -15%',                   apply: (ctx) => { ctx.train.wallMaxHp = Math.floor(ctx.train.wallMaxHp * 0.85); ctx.train.wallHp = Math.min(ctx.train.wallHp, ctx.train.wallMaxHp); } },
    { id: 'fast_invasion',   name: '급속 침입',   desc: '적 이동속도 +15%',               apply: (ctx) => { ctx.train.enemySpeedMult = (ctx.train.enemySpeedMult || 1) * 1.15; } },
    { id: 'no_repair',       name: '수리 불능',   desc: '외벽 자동 수리 비활성화',         apply: (ctx) => { ctx.train.noAutoRepair = true; } },
    { id: 'elite_swarm',     name: '엘리트 다수', desc: '엘리트 등장 빈도 +50%',          apply: (ctx) => { ctx.train.eliteFreqMult = (ctx.train.eliteFreqMult || 1) * 1.5; } },
    { id: 'skill_seal',      name: '스킬 봉인',   desc: '랜덤 1명 스킬 불가',             apply: (ctx) => { if (ctx.allies.length > 0) { const idx = Math.floor(Math.random() * ctx.allies.length); ctx.allies[idx].skillSealed = true; } } },
    { id: 'fog',             name: '안개',        desc: '적 접근 경고 -30%',              apply: (ctx) => { ctx.train.fogWarningReduction = 0.3; } }
];

// 양성 3택1 생성
function generatePositiveModifierChoices(rerollUsed) {
    const shuffled = [...CARGO_POSITIVE_MODIFIERS].sort(() => Math.random() - 0.5);
    return shuffled.slice(0, 3);
}

// 음성 1개 강제 선택
function pickNegativeModifier() {
    return CARGO_NEGATIVE_MODIFIERS[Math.floor(Math.random() * CARGO_NEGATIVE_MODIFIERS.length)];
}

// ─── 층 스케일링 ──────────────────────────────────────────────────

function getCargoFloorScaling(floor) {
    // 적 스탯 스케일링 (층 높을수록 강해짐)
    const hpMult = 1 + (floor - 1) * 0.06;    // 층당 6% HP 증가
    const atkMult = 1 + (floor - 1) * 0.04;   // 층당 4% ATK 증가
    const countMult = 1 + (floor - 1) * 0.03; // 층당 3% 적 수 증가
    const wallHp = 500 + floor * 30;           // 외벽 기본 HP

    return { hpMult, atkMult, countMult, wallHp };
}

// ─── 성적 판정 ────────────────────────────────────────────────────

function judgeCargoPerformance(wallHpPercent, killPercent) {
    // +3: 기차 HP 50%+ AND 적 전멸률 80%+
    if (wallHpPercent >= 0.5 && killPercent >= 0.8) return 3;
    // +2: 둘 중 하나만 충족
    if (wallHpPercent >= 0.5 || killPercent >= 0.8) return 2;
    // +1: 최소 클리어
    return 1;
}

// ─── 마일스톤 ─────────────────────────────────────────────────────

const CARGO_MILESTONES = {
    10: { name: '엘리트 보스', reward: 'basic' },
    25: { name: '강화 보스', reward: 'rare' },
    50: { name: '메가 보스', reward: 'legendary' },
    99: { name: '최종 보스', reward: 'special' }
};

function isCargoMilestone(floor) {
    return CARGO_MILESTONES[floor] || null;
}

// ─── XP / 레벨업 곡선 ────────────────────────────────────────────

function getCargoXpToLevel(level) {
    // 5~8회 레벨업 목표 (5분 런), 초반 빠르고 후반 느림
    return Math.floor(20 + level * level * 8);
}

// ─── 적 웨이브 생성 ──────────────────────────────────────────────

function generateCargoWave(elapsed, floor, modifiers = {}) {
    const scaling = getCargoFloorScaling(floor);
    const countMult = scaling.countMult * (modifiers.enemyCountMult || 1);
    const eliteFreq = modifiers.eliteFreqMult || 1;
    const speedMult = modifiers.enemySpeedMult || 1;
    const statMult = modifiers.enemyStatMult || 1;

    // 시간에 따라 웨이브 강도 증가
    const timeFactor = 1 + (elapsed / 300000) * 0.5; // 5분 동안 50% 증가
    const baseCount = Math.floor((3 + Math.floor(elapsed / 20000)) * countMult * timeFactor);

    const enemies = [];
    const cargoEnemyTypes = ['mechling', 'turret', 'shielder', 'bomber'];

    for (let i = 0; i < baseCount; i++) {
        let type = cargoEnemyTypes[Math.floor(Math.random() * cargoEnemyTypes.length)];
        let isElite = Math.random() < (0.05 * eliteFreq * timeFactor);

        if (isElite) {
            type = Math.random() < 0.5 ? 'elite_shielder' : 'elite_turret';
        }

        enemies.push({ type, speedMult, statMult: statMult * scaling.hpMult, atkMult: statMult * scaling.atkMult });
    }

    return enemies;
}
