const RECIPE_DATA = [
    // ── 일반 무기 ──
    {
        id: 'craft_steel_sword',
        name: '강철 검',
        category: 'weapon',
        materials: { '강철 조각': 3, '기본 금속': 2 },
        goldCost: 200,
        resultRarity: 'uncommon',
        result: { name: '강철 검', slot: 'weapon', type: 'equipment', baseAtk: 8 }
    },
    {
        id: 'craft_magic_staff',
        name: '마력 지팡이',
        category: 'weapon',
        materials: { '마력의 가루': 3, '마법 동력석': 1 },
        goldCost: 600,
        resultRarity: 'rare',
        result: { name: '마력 지팡이', slot: 'weapon', type: 'equipment', baseAtk: 10, baseCrit: 0.03 }
    },
    {
        id: 'craft_hunter_bow',
        name: '사냥꾼의 활',
        category: 'weapon',
        materials: { '가죽': 4, '뼛조각': 2 },
        goldCost: 220,
        resultRarity: 'uncommon',
        result: { name: '사냥꾼의 활', slot: 'weapon', type: 'equipment', baseAtk: 7, baseSpd: 8 }
    },
    {
        id: 'craft_war_axe',
        name: '전투 도끼',
        category: 'weapon',
        materials: { '강철 조각': 4, '뼛조각': 3 },
        goldCost: 350,
        resultRarity: 'uncommon',
        result: { name: '전투 도끼', slot: 'weapon', type: 'equipment', baseAtk: 14 }
    },

    // ── 일반 방어구 ──
    {
        id: 'craft_chain_armor',
        name: '사슬 갑옷',
        category: 'armor',
        materials: { '강철 조각': 4, '기본 금속': 2 },
        goldCost: 250,
        resultRarity: 'uncommon',
        result: { name: '사슬 갑옷', slot: 'armor', type: 'equipment', baseDef: 8, baseHp: 30 }
    },
    {
        id: 'craft_plate_armor',
        name: '판금 갑옷',
        category: 'armor',
        materials: { '강철 조각': 6, '기본 금속': 3, '가죽': 2 },
        goldCost: 500,
        resultRarity: 'rare',
        result: { name: '판금 갑옷', slot: 'armor', type: 'equipment', baseDef: 14, baseHp: 50 }
    },
    {
        id: 'craft_mage_robe',
        name: '마법사 로브',
        category: 'armor',
        materials: { '마력의 가루': 2, '가죽': 3 },
        goldCost: 280,
        resultRarity: 'uncommon',
        result: { name: '마법사 로브', slot: 'armor', type: 'equipment', baseDef: 3, baseHp: 10, baseAtk: 4 }
    },

    // ── 일반 장신구 ──
    {
        id: 'craft_luck_ring',
        name: '행운의 반지',
        category: 'accessory',
        materials: { '기본 금속': 3, '마력의 가루': 1 },
        goldCost: 180,
        resultRarity: 'uncommon',
        result: { name: '행운의 반지', slot: 'accessory', type: 'equipment', baseCrit: 0.04 }
    },
    {
        id: 'craft_str_necklace',
        name: '힘의 목걸이',
        category: 'accessory',
        materials: { '혈정석': 1, '기본 금속': 2 },
        goldCost: 250,
        resultRarity: 'uncommon',
        result: { name: '힘의 목걸이', slot: 'accessory', type: 'equipment', baseAtk: 5 }
    },
    {
        id: 'craft_guard_charm',
        name: '수호의 부적',
        category: 'accessory',
        materials: { '영혼 조각': 2, '마력의 가루': 1, '기본 금속': 1 },
        goldCost: 300,
        resultRarity: 'uncommon',
        result: { name: '수호의 부적', slot: 'accessory', type: 'equipment', baseDef: 4, baseHp: 25 }
    },
    {
        id: 'craft_speed_gloves',
        name: '속도의 장갑',
        category: 'accessory',
        materials: { '기계 부품': 3, '가죽': 2 },
        goldCost: 280,
        resultRarity: 'uncommon',
        result: { name: '속도의 장갑', slot: 'accessory', type: 'equipment', baseSpd: 15 }
    },

    // ── 구역 특수 장비 제작 ──
    {
        id: 'craft_blood_sword',
        name: '피에 물든 철검',
        category: 'weapon',
        materials: { '강철 조각': 3, '혈정석': 2, '뼛조각': 2 },
        goldCost: 500,
        resultRarity: 'rare',
        result: { name: '피에 물든 철검', slot: 'weapon', type: 'equipment', baseAtk: 15, special: 'bleed' },
        zoneRequired: 'bloodpit'
    },
    {
        id: 'craft_steam_hammer',
        name: '증기 해머',
        category: 'weapon',
        materials: { '강철 조각': 4, '마법 동력석': 2, '기계 부품': 3 },
        goldCost: 700,
        resultRarity: 'rare',
        result: { name: '증기 해머', slot: 'weapon', type: 'equipment', baseAtk: 14, special: 'overheat' },
        zoneRequired: 'cargo'
    },
    {
        id: 'craft_cursed_dagger',
        name: '저주받은 단검',
        category: 'weapon',
        materials: { '영혼 조각': 3, '저주 유물': 2, '어둠의 정수': 2 },
        goldCost: 600,
        resultRarity: 'rare',
        result: { name: '저주받은 단검', slot: 'weapon', type: 'equipment', baseAtk: 12, baseCrit: 0.08, special: 'fear' },
        zoneRequired: 'blackout'
    },
    {
        id: 'craft_blood_blade',
        name: '흡혈의 칼날',
        category: 'weapon',
        materials: { '혈정석': 3, '강철 조각': 2, '기본 금속': 2 },
        goldCost: 450,
        resultRarity: 'rare',
        result: { name: '흡혈의 칼날', slot: 'weapon', type: 'equipment', baseAtk: 14, baseCrit: 0.05, special: 'lifesteal' },
        zoneRequired: 'bloodpit'
    },
    {
        id: 'craft_steam_armor',
        name: '증기 갑옷',
        category: 'armor',
        materials: { '강철 조각': 5, '마법 동력석': 2, '기계 부품': 3 },
        goldCost: 800,
        resultRarity: 'rare',
        result: { name: '증기 갑옷', slot: 'armor', type: 'equipment', baseDef: 12, baseHp: 35, special: 'overheat_shield' },
        zoneRequired: 'cargo'
    },
    {
        id: 'craft_cursed_ring',
        name: '저주받은 반지',
        category: 'accessory',
        materials: { '영혼 조각': 3, '저주 유물': 1, '어둠의 정수': 2 },
        goldCost: 600,
        resultRarity: 'rare',
        result: { name: '저주받은 반지', slot: 'accessory', type: 'equipment', baseCrit: 0.10, baseAtk: 5, special: 'fear_duration' },
        zoneRequired: 'blackout'
    },

    // ── 제작 전용 (드롭 불가) ──
    {
        id: 'craft_trinity_sword',
        name: '삼위일체 검',
        category: 'weapon',
        materials: { '혈정석': 2, '마법 동력석': 2, '저주 유물': 2, '마력의 가루': 2 },
        goldCost: 2000,
        resultRarity: 'epic',
        result: { name: '삼위일체 검', slot: 'weapon', type: 'equipment', baseAtk: 25, baseCrit: 0.08, special: 'trinity' },
        craftOnly: true,
        requireAllZones: true
    },
    {
        id: 'craft_myth_armor',
        name: '신화의 갑옷',
        category: 'armor',
        materials: { '핏로드의 송곳니': 1, '화물 제독의 인장': 1, '강철 조각': 5 },
        goldCost: 5000,
        resultRarity: 'epic',
        result: { name: '신화의 갑옷', slot: 'armor', type: 'equipment', baseDef: 22, baseHp: 100, special: 'mythic_endure' },
        craftOnly: true,
        requireAllZones: true
    },
    {
        id: 'craft_lord_crown',
        name: '군주의 왕관',
        category: 'accessory',
        materials: { '핏로드의 송곳니': 1, '화물 제독의 인장': 1, '그림자 군주의 가면 조각': 1, '순수 결정': 3 },
        goldCost: 10000,
        resultRarity: 'legendary',
        result: { name: '군주의 왕관', slot: 'accessory', type: 'equipment', baseAtk: 20, baseDef: 10, baseHp: 50, baseCrit: 0.10, special: 'lord_aura' },
        craftOnly: true,
        requireAllZones: true
    },
    {
        id: 'craft_steam_engine_bracelet',
        name: '증기 기관 팔찌',
        category: 'accessory',
        materials: { '마법 동력석': 3, '기계 부품': 4, '기본 금속': 3 },
        goldCost: 800,
        resultRarity: 'rare',
        result: { name: '증기 기관 팔찌', slot: 'accessory', type: 'equipment', baseAtk: 8, baseSpd: 15, special: 'momentum' },
        craftOnly: true,
        zoneRequired: 'cargo'
    },
    {
        id: 'craft_void_cloak',
        name: '허공의 망토',
        category: 'armor',
        materials: { '어둠의 정수': 4, '영혼 조각': 3, '마력의 가루': 2 },
        goldCost: 900,
        resultRarity: 'rare',
        result: { name: '허공의 망토', slot: 'armor', type: 'equipment', baseDef: 8, baseHp: 30, baseSpd: 10, special: 'phase' },
        craftOnly: true,
        zoneRequired: 'blackout'
    }
];

const ENHANCE_COST = {
    1: { gold: 100,  materials: 1, successRate: 1.00 },
    2: { gold: 300,  materials: 2, successRate: 0.90 },
    3: { gold: 800,  materials: 3, successRate: 0.70 },
    4: { gold: 2000, materials: 1, successRate: 0.50, requireRare: true },
    5: { gold: 5000, materials: 2, successRate: 0.30, requireRare: true }
};

const RARITY_ORDER = ['common', 'uncommon', 'rare', 'epic', 'legendary'];

function getNextRarity(current) {
    const idx = RARITY_ORDER.indexOf(current);
    if (idx < 0 || idx >= RARITY_ORDER.length - 1) return null;
    return RARITY_ORDER[idx + 1];
}

function getEnhanceCost(enhanceLevel) {
    return ENHANCE_COST[enhanceLevel] || null;
}
