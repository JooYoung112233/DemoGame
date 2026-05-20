// Item definitions for looting system
// Each item has grid size (w x h cells), weight, value, and rarity

const ITEM_DATA = {
    // === Food & Medicine (1x1, light) ===
    canned_food: {
        id: 'canned_food', name: '통조림', icon: '🥫',
        w: 1, h: 1, weight: 0.5, value: 15,
        rarity: 'common', category: 'food',
        desc: '유통기한은 아직 남았다'
    },
    water: {
        id: 'water', name: '생수', icon: '💧',
        w: 1, h: 1, weight: 0.6, value: 10,
        rarity: 'common', category: 'food',
        desc: '깨끗한 물 한 병'
    },
    bandage: {
        id: 'bandage', name: '붕대', icon: '🩹',
        w: 1, h: 1, weight: 0.2, value: 20,
        rarity: 'common', category: 'medicine',
        desc: '응급 처치용'
    },
    medkit: {
        id: 'medkit', name: '구급상자', icon: '🧰',
        w: 2, h: 1, weight: 1.5, value: 60,
        rarity: 'uncommon', category: 'medicine',
        desc: 'HP를 크게 회복'
    },

    // === Materials (various sizes) ===
    scrap_metal: {
        id: 'scrap_metal', name: '고철', icon: '🔩',
        w: 1, h: 1, weight: 1.0, value: 8,
        rarity: 'common', category: 'material',
        desc: '녹슨 금속 조각'
    },
    electronics: {
        id: 'electronics', name: '전자부품', icon: '🔌',
        w: 1, h: 2, weight: 0.8, value: 35,
        rarity: 'uncommon', category: 'material',
        desc: '아직 쓸 수 있는 회로 기판'
    },
    toolbox: {
        id: 'toolbox', name: '공구세트', icon: '🔧',
        w: 2, h: 1, weight: 2.5, value: 45,
        rarity: 'uncommon', category: 'material',
        desc: '수리와 제작에 필요'
    },
    fuel: {
        id: 'fuel', name: '연료통', icon: '⛽',
        w: 2, h: 2, weight: 5.0, value: 80,
        rarity: 'rare', category: 'material',
        desc: '귀중한 연료. 무겁지만 가치있다'
    },

    // === Valuables (high value, various) ===
    gold_ring: {
        id: 'gold_ring', name: '금반지', icon: '💍',
        w: 1, h: 1, weight: 0.1, value: 50,
        rarity: 'rare', category: 'valuable',
        desc: '거래 가치가 높다'
    },
    watch: {
        id: 'watch', name: '손목시계', icon: '⌚',
        w: 1, h: 1, weight: 0.2, value: 40,
        rarity: 'uncommon', category: 'valuable',
        desc: '아직 작동하는 시계'
    },
    documents: {
        id: 'documents', name: '기밀문서', icon: '📄',
        w: 1, h: 1, weight: 0.1, value: 70,
        rarity: 'rare', category: 'valuable',
        desc: '누군가에게 가치가 있을 정보'
    },
    laptop: {
        id: 'laptop', name: '노트북', icon: '💻',
        w: 2, h: 2, weight: 2.0, value: 120,
        rarity: 'rare', category: 'valuable',
        desc: '데이터가 남아있을지도'
    },

    // === Equipment ===
    ammo_box: {
        id: 'ammo_box', name: '탄약', icon: '🔶',
        w: 1, h: 1, weight: 0.5, value: 25,
        rarity: 'common', category: 'equipment',
        desc: '권총 탄약 한 상자'
    },
    kevlar: {
        id: 'kevlar', name: '방탄조끼', icon: '🦺',
        w: 2, h: 2, weight: 3.0, value: 100,
        rarity: 'rare', category: 'equipment',
        desc: '피격 데미지 감소'
    },
    knife: {
        id: 'knife', name: '전투칼', icon: '🔪',
        w: 1, h: 2, weight: 0.8, value: 30,
        rarity: 'uncommon', category: 'equipment',
        desc: '근접 공격력 증가'
    },
    flashlight: {
        id: 'flashlight', name: '손전등', icon: '🔦',
        w: 1, h: 1, weight: 0.3, value: 20,
        rarity: 'common', category: 'equipment',
        desc: '밤 시야 범위 확장'
    },
};

// Loot tables by location type
const LOOT_TABLES = {
    // Generic urban loot
    urban: {
        common:   ['canned_food', 'water', 'bandage', 'scrap_metal', 'ammo_box'],
        uncommon: ['electronics', 'toolbox', 'watch', 'knife', 'medkit'],
        rare:     ['gold_ring', 'documents', 'fuel', 'laptop', 'kevlar'],
    },
    // Residential buildings
    residential: {
        common:   ['canned_food', 'water', 'bandage', 'flashlight'],
        uncommon: ['watch', 'medkit', 'electronics'],
        rare:     ['gold_ring', 'documents', 'laptop'],
    },
    // Commercial/industrial
    commercial: {
        common:   ['scrap_metal', 'ammo_box', 'scrap_metal'],
        uncommon: ['toolbox', 'electronics', 'knife'],
        rare:     ['fuel', 'kevlar', 'laptop'],
    },
};

// Generate loot from a table
function generateLoot(tableId, count) {
    const table = LOOT_TABLES[tableId] || LOOT_TABLES.urban;
    const result = [];
    for (let i = 0; i < count; i++) {
        const roll = Math.random();
        let pool;
        if (roll < 0.1) pool = table.rare;
        else if (roll < 0.35) pool = table.uncommon;
        else pool = table.common;

        const itemId = pool[Math.floor(Math.random() * pool.length)];
        const item = ITEM_DATA[itemId];
        if (item) result.push({ ...item });
    }
    return result;
}
