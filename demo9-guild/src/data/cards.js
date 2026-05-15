const CARD_DATA = {
    // ── Tier 1: 기본 스탯 (항상 등장) ──
    atk_1:     { name: '날카로운 칼날',   category: 'buff',  tier: 1, desc: '전체 ATK +5%',     apply: units => units.forEach(u => { u.atk = Math.floor(u.atk * 1.05); }) },
    hp_1:      { name: '강인한 체력',     category: 'buff',  tier: 1, desc: '전체 HP +8%',      apply: units => units.forEach(u => { u.maxHp = Math.floor(u.maxHp * 1.08); u.hp = Math.min(u.hp + Math.floor(u.maxHp * 0.08), u.maxHp); }) },
    def_1:     { name: '두꺼운 갑옷',     category: 'buff',  tier: 1, desc: '전체 DEF +3',      apply: units => units.forEach(u => { u.def += 3; }) },
    spd_1:     { name: '가벼운 발걸음',   category: 'buff',  tier: 1, desc: '전체 SPD +8%',     apply: units => units.forEach(u => { u.moveSpeed = Math.floor(u.moveSpeed * 1.08); }) },
    crit_1:    { name: '예리한 눈',       category: 'buff',  tier: 1, desc: '전체 CRIT +3%',    apply: units => units.forEach(u => { u.critRate = Math.min(0.6, u.critRate + 0.03); }) },
    heal_1:    { name: '응급 치료',       category: 'heal',  tier: 1, desc: '전체 HP 15% 회복', apply: units => units.forEach(u => { u.hp = Math.min(u.maxHp, u.hp + Math.floor(u.maxHp * 0.15)); }) },

    // ── Tier 2: 중급 스탯 (라운드3+ 또는 구역Lv2+) ──
    atk_2:     { name: '전투 본능',       category: 'buff',  tier: 2, desc: '전체 ATK +8%',     apply: units => units.forEach(u => { u.atk = Math.floor(u.atk * 1.08); }) },
    hp_2:      { name: '생존 의지',       category: 'buff',  tier: 2, desc: '전체 HP +12%',     apply: units => units.forEach(u => { u.maxHp = Math.floor(u.maxHp * 1.12); u.hp = Math.min(u.hp + Math.floor(u.maxHp * 0.12), u.maxHp); }) },
    def_2:     { name: '단단한 방진',     category: 'buff',  tier: 2, desc: '전체 DEF +5',      apply: units => units.forEach(u => { u.def += 5; }) },
    spd_2:     { name: '신속 대형',       category: 'buff',  tier: 2, desc: '전체 SPD +12%',    apply: units => units.forEach(u => { u.moveSpeed = Math.floor(u.moveSpeed * 1.12); }) },
    crit_2:    { name: '약점 포착',       category: 'buff',  tier: 2, desc: '전체 CRIT +5%',    apply: units => units.forEach(u => { u.critRate = Math.min(0.6, u.critRate + 0.05); }) },
    critdmg_1: { name: '치명 강화',       category: 'combat', tier: 2, desc: '크리 피해 +15%',   apply: units => units.forEach(u => { u.critDmg += 0.15; }) },
    lifesteal: { name: '흡혈 본능',       category: 'combat', tier: 2, desc: '처치 시 HP 3% 회복', apply: units => units.forEach(u => { u.lifestealOnKill = (u.lifestealOnKill || 0) + 0.03; }) },
    heal_2:    { name: '전장 치유',       category: 'heal',  tier: 2, desc: '전체 HP 25% 회복', apply: units => units.forEach(u => { u.hp = Math.min(u.maxHp, u.hp + Math.floor(u.maxHp * 0.25)); }) },
    golden_hands: { name: '황금손',       category: 'loot',  tier: 2, desc: '골드 획득 +20%',   apply: () => {} },

    // ── Tier 3: 고급 스탯 (라운드5+ AND 구역Lv3+) ──
    atk_3:     { name: '살기',           category: 'buff',  tier: 3, desc: '전체 ATK +12%',    apply: units => units.forEach(u => { u.atk = Math.floor(u.atk * 1.12); }) },
    hp_3:      { name: '불굴',           category: 'buff',  tier: 3, desc: '전체 HP +15%',     apply: units => units.forEach(u => { u.maxHp = Math.floor(u.maxHp * 1.15); u.hp = Math.min(u.hp + Math.floor(u.maxHp * 0.15), u.maxHp); }) },
    allstat:   { name: '전투 숙련',       category: 'buff',  tier: 3, desc: 'ATK/DEF/SPD +5%', apply: units => units.forEach(u => { u.atk = Math.floor(u.atk * 1.05); u.def = Math.floor(u.def * 1.05); u.moveSpeed = Math.floor(u.moveSpeed * 1.05); }) },
    crit_3:    { name: '급소 간파',       category: 'combat', tier: 3, desc: 'CRIT +8% 크리피해+20%', apply: units => units.forEach(u => { u.critRate = Math.min(0.6, u.critRate + 0.08); u.critDmg += 0.2; }) },
    laststand: { name: '최후의 저항',     category: 'combat', tier: 3, desc: 'HP 25%↓ 시 ATK+15%', apply: units => units.forEach(u => { u.lastStand = true; }) },
    heal_3:    { name: '대치유',         category: 'heal',  tier: 3, desc: '전체 HP 40% 회복', apply: units => units.forEach(u => { u.hp = Math.min(u.maxHp, u.hp + Math.floor(u.maxHp * 0.40)); }) },
    lucky:     { name: '행운의 기운',     category: 'loot',  tier: 3, desc: '드랍 희귀도 +1',   apply: () => {} }
};

const CARD_KEYS = Object.keys(CARD_DATA);

function generateCardChoices(round, collectedCards, zoneLevel) {
    zoneLevel = zoneLevel || 1;
    const available = CARD_KEYS.filter(k => {
        if (collectedCards.includes(k)) return false;
        const card = CARD_DATA[k];
        if (card.tier === 2 && round < 3 && zoneLevel < 2) return false;
        if (card.tier === 3 && (round < 5 || zoneLevel < 3)) return false;
        return true;
    });
    const shuffled = [...available].sort(() => Math.random() - 0.5);
    return shuffled.slice(0, Math.min(3, shuffled.length));
}

const ROUND_DROP_RARITY = {
    1: 'common', 2: 'common', 3: 'uncommon',
    4: 'uncommon', 5: 'rare', 6: 'rare',
    7: 'rare', 8: 'epic', 9: 'epic', 10: 'epic'
};
