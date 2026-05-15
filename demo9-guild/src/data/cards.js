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
    laststand: { name: '최후의 저항',     category: 'combat', tier: 3, desc: 'HP 25%↓ 시 전투력 +20%', apply: units => units.forEach(u => { u.lastStand = true; }) },
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

// ── Cargo 칸별 카드 시스템 ──────────────────────────────────
const CARGO_CAR_CARDS = {
    ammo: {
        name: '탄약칸', icon: '🔫', category: 'combat',
        cards: {
            ammo_atk1:   { name: '관통탄',       tier: 1, desc: 'ATK +8%',            apply: units => units.forEach(u => { u.atk = Math.floor(u.atk * 1.08); }) },
            ammo_crit1:  { name: '정밀 조준경',  tier: 1, desc: 'CRIT +5%',           apply: units => units.forEach(u => { u.critRate = Math.min(0.6, u.critRate + 0.05); }) },
            ammo_atk2:   { name: '폭렬탄',       tier: 2, desc: 'ATK +12%, 크리피해+10%', apply: units => units.forEach(u => { u.atk = Math.floor(u.atk * 1.12); u.critDmg += 0.1; }) },
            ammo_speed1: { name: '속사 장전',     tier: 2, desc: '공속 10% 빨라짐',     apply: units => units.forEach(u => { u.attackSpeed = Math.floor(u.attackSpeed * 0.9); }) },
            ammo_atk3:   { name: '파멸의 탄환',   tier: 3, desc: 'ATK +15%, CRIT +8%',  apply: units => units.forEach(u => { u.atk = Math.floor(u.atk * 1.15); u.critRate = Math.min(0.6, u.critRate + 0.08); }) }
        }
    },
    medical: {
        name: '의무칸', icon: '🏥', category: 'heal',
        cards: {
            med_heal1:   { name: '응급 처치',     tier: 1, desc: '전체 HP 20% 회복',     apply: units => units.forEach(u => { u.hp = Math.min(u.maxHp, u.hp + Math.floor(u.maxHp * 0.2)); }) },
            med_regen1:  { name: '재생 주사',     tier: 1, desc: '최대HP +10%',          apply: units => units.forEach(u => { u.maxHp = Math.floor(u.maxHp * 1.1); u.hp = Math.min(u.hp + Math.floor(u.maxHp * 0.1), u.maxHp); }) },
            med_heal2:   { name: '전장 의무병',   tier: 2, desc: '전체 HP 35% 회복',     apply: units => units.forEach(u => { u.hp = Math.min(u.maxHp, u.hp + Math.floor(u.maxHp * 0.35)); }) },
            med_shield1: { name: '방호막 주사',   tier: 2, desc: 'DEF +5, 최대HP +5%',   apply: units => units.forEach(u => { u.def += 5; u.maxHp = Math.floor(u.maxHp * 1.05); }) },
            med_revive:  { name: '기적의 치료',   tier: 3, desc: '전체 HP 50% 회복',     apply: units => units.forEach(u => { u.hp = Math.min(u.maxHp, u.hp + Math.floor(u.maxHp * 0.5)); }) }
        }
    },
    generator: {
        name: '발전칸', icon: '⚡', category: 'utility',
        cards: {
            gen_spd1:    { name: '과충전',        tier: 1, desc: 'SPD +10%',            apply: units => units.forEach(u => { u.moveSpeed = Math.floor(u.moveSpeed * 1.1); }) },
            gen_all1:    { name: '에너지 분배',   tier: 1, desc: 'ATK/DEF +3%',         apply: units => units.forEach(u => { u.atk = Math.floor(u.atk * 1.03); u.def = Math.floor(u.def * 1.03); }) },
            gen_spd2:    { name: '터보 충전',     tier: 2, desc: 'SPD +15%, 공속 8% 증가', apply: units => units.forEach(u => { u.moveSpeed = Math.floor(u.moveSpeed * 1.15); u.attackSpeed = Math.floor(u.attackSpeed * 0.92); }) },
            gen_shield1: { name: '전자기 장벽',   tier: 2, desc: 'DEF +8',              apply: units => units.forEach(u => { u.def += 8; }) },
            gen_all2:    { name: '최대 출력',     tier: 3, desc: 'ATK/DEF/SPD +8%',     apply: units => units.forEach(u => { u.atk = Math.floor(u.atk * 1.08); u.def = Math.floor(u.def * 1.08); u.moveSpeed = Math.floor(u.moveSpeed * 1.08); }) }
        }
    },
    cargo: {
        name: '화물칸', icon: '📦', category: 'loot',
        cards: {
            crg_gold1:   { name: '금화 상자',     tier: 1, desc: '즉시 골드 +30',        apply: () => {}, goldBonus: 30 },
            crg_loot1:   { name: '보급 물자',     tier: 1, desc: '다음 라운드 드랍률 +20%', apply: () => {}, lootBonus: 0.2 },
            crg_gold2:   { name: '보물 상자',     tier: 2, desc: '즉시 골드 +60',        apply: () => {}, goldBonus: 60 },
            crg_repair1: { name: '수리 키트',     tier: 2, desc: '임의 칸 HP 30% 수리',   apply: () => {}, repairRatio: 0.3 },
            crg_gold3:   { name: '전설의 금고',   tier: 3, desc: '즉시 골드 +100, 전리품 1개', apply: () => {}, goldBonus: 100, bonusLoot: true }
        }
    }
};

function generateCargoCardChoices(carKey, round, heldCardIds, zoneLevel) {
    const carPool = CARGO_CAR_CARDS[carKey];
    if (!carPool) return [];
    const available = Object.keys(carPool.cards).filter(k => {
        if (heldCardIds.includes(k)) return false;
        const card = carPool.cards[k];
        if (card.tier === 2 && round < 3 && zoneLevel < 2) return false;
        if (card.tier === 3 && (round < 5 || zoneLevel < 3)) return false;
        return true;
    });
    const shuffled = [...available].sort(() => Math.random() - 0.5);
    return shuffled.slice(0, Math.min(3, shuffled.length));
}

function getCargoCard(cardId) {
    for (const carKey of Object.keys(CARGO_CAR_CARDS)) {
        const card = CARGO_CAR_CARDS[carKey].cards[cardId];
        if (card) return { ...card, id: cardId, carKey };
    }
    return null;
}

const CARGO_MAX_HAND = 5;
