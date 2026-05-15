const RECIPE_DATA = [
    // ── 무기 ──
    {
        id: 'craft_sword',
        name: '강철 검',
        category: 'weapon',
        materials: { '철광석': 3, '황금 모래': 1 },
        goldCost: 100,
        result: { name: '강철 검', slot: 'weapon', type: 'equipment', baseAtk: 10, baseDef: 0, baseHp: 0 }
    },
    {
        id: 'craft_staff',
        name: '마법 지팡이',
        category: 'weapon',
        materials: { '마법 가루': 3, '마법 동력석': 1 },
        goldCost: 120,
        result: { name: '마법 지팡이', slot: 'weapon', type: 'equipment', baseAtk: 12, baseDef: 0, baseHp: 0 }
    },
    {
        id: 'craft_bow',
        name: '사냥꾼의 활',
        category: 'weapon',
        materials: { '철광석': 2, '황금 모래': 2 },
        goldCost: 90,
        result: { name: '사냥꾼의 활', slot: 'weapon', type: 'equipment', baseAtk: 8, baseDef: 0, baseHp: 0 }
    },
    {
        id: 'craft_dagger',
        name: '의식용 단검',
        category: 'weapon',
        materials: { '저주 유물': 2, '철광석': 2 },
        goldCost: 150,
        result: { name: '의식용 단검', slot: 'weapon', type: 'equipment', baseAtk: 15, baseDef: 0, baseHp: 0 }
    },
    {
        id: 'craft_blood_blade',
        name: '흡혈의 칼날',
        category: 'weapon',
        materials: { '혈정석': 3, '철광석': 2 },
        goldCost: 200,
        result: { name: '흡혈의 칼날', slot: 'weapon', type: 'equipment', baseAtk: 14, baseDef: 0, baseHp: 0, lifesteal: 0.05 }
    },

    // ── 방어구 ──
    {
        id: 'craft_chain',
        name: '사슬 갑옷',
        category: 'armor',
        materials: { '철광석': 4, '황금 모래': 1 },
        goldCost: 130,
        result: { name: '사슬 갑옷', slot: 'armor', type: 'equipment', baseAtk: 0, baseDef: 10, baseHp: 40 }
    },
    {
        id: 'craft_robe',
        name: '마법사 로브',
        category: 'armor',
        materials: { '마법 가루': 3, '황금 모래': 1 },
        goldCost: 100,
        result: { name: '마법사 로브', slot: 'armor', type: 'equipment', baseAtk: 3, baseDef: 3, baseHp: 15 }
    },
    {
        id: 'craft_plate',
        name: '판금 갑옷',
        category: 'armor',
        materials: { '철광석': 5, '혈정석': 1 },
        goldCost: 200,
        result: { name: '판금 갑옷', slot: 'armor', type: 'equipment', baseAtk: 0, baseDef: 15, baseHp: 60 }
    },

    // ── 장신구 ──
    {
        id: 'craft_ring',
        name: '행운의 반지',
        category: 'accessory',
        materials: { '황금 모래': 3, '마법 가루': 1 },
        goldCost: 110,
        result: { name: '행운의 반지', slot: 'accessory', type: 'equipment', baseAtk: 0, baseDef: 0, baseHp: 0, critRate: 0.05 }
    },
    {
        id: 'craft_necklace',
        name: '힘의 목걸이',
        category: 'accessory',
        materials: { '혈정석': 2, '황금 모래': 2 },
        goldCost: 120,
        result: { name: '힘의 목걸이', slot: 'accessory', type: 'equipment', baseAtk: 5, baseDef: 0, baseHp: 0 }
    },
    {
        id: 'craft_charm',
        name: '수호의 부적',
        category: 'accessory',
        materials: { '마법 가루': 2, '저주 유물': 1, '황금 모래': 1 },
        goldCost: 140,
        result: { name: '수호의 부적', slot: 'accessory', type: 'equipment', baseAtk: 0, baseDef: 5, baseHp: 30 }
    },
    {
        id: 'craft_gloves',
        name: '속도의 장갑',
        category: 'accessory',
        materials: { '마법 동력석': 2, '황금 모래': 2 },
        goldCost: 130,
        result: { name: '속도의 장갑', slot: 'accessory', type: 'equipment', baseAtk: 0, baseDef: 0, baseHp: 0, moveSpeed: 15 }
    }
];

const ENHANCE_COST = {
    common:   { gold: 150, materials: 2 },
    uncommon: { gold: 300, materials: 3 },
    rare:     { gold: 600, materials: 5 },
    epic:     { gold: 1200, materials: 8 }
};

const RARITY_ORDER = ['common', 'uncommon', 'rare', 'epic', 'legendary'];

function getNextRarity(current) {
    const idx = RARITY_ORDER.indexOf(current);
    if (idx < 0 || idx >= RARITY_ORDER.length - 1) return null;
    return RARITY_ORDER[idx + 1];
}

function getEnhanceCost(rarity) {
    return ENHANCE_COST[rarity] || null;
}
