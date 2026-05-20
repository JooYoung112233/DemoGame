const RECIPE_DATA = {
    health_potion: {
        name: '체력 포션',
        result: 'health_potion',
        category: 'potion',
        ingredients: [
            { id: 'heal_herb', count: 2 },
        ],
        unlockDay: 1,
    },
    antidote: {
        name: '해독제',
        result: 'antidote',
        category: 'potion',
        ingredients: [
            { id: 'poison_herb', count: 1 },
            { id: 'heal_herb', count: 1 },
        ],
        unlockDay: 3,
    },
    iron_dagger: {
        name: '철 단검',
        result: 'iron_dagger',
        category: 'weapon',
        ingredients: [
            { id: 'iron_ore', count: 2 },
            { id: 'wood', count: 1 },
        ],
        unlockDay: 7,
    },
    silver_ring: {
        name: '은반지',
        result: 'silver_ring',
        category: 'accessory',
        ingredients: [
            { id: 'silver_ore', count: 2 },
        ],
        unlockDay: 10,
    },
};

// === 연구 레시피 (발견해야 해금) ===
const RESEARCH_RECIPES = {
    mana_potion: {
        name: '마나 포션',
        result: 'mana_potion',
        category: 'potion',
        ingredients: [
            { id: 'light_flower', count: 2 },
            { id: 'magic_stone', count: 1 },
        ],
        hint: '빛꽃과 마석을 합치면...',
    },
    enchanted_blade: {
        name: '마력 칼날',
        result: 'enchanted_blade',
        category: 'weapon',
        ingredients: [
            { id: 'iron_ore', count: 2 },
            { id: 'rune_fragment', count: 1 },
        ],
        hint: '철광석에 룬의 힘을 담으면...',
    },
    beast_armor: {
        name: '야수 갑옷',
        result: 'beast_armor',
        category: 'weapon',
        ingredients: [
            { id: 'bear_hide', count: 2 },
            { id: 'wolf_hide', count: 1 },
            { id: 'iron_ore', count: 1 },
        ],
        hint: '가죽과 철로 방어구를...',
    },
    gem_amulet: {
        name: '보석 부적',
        result: 'gem_amulet',
        category: 'accessory',
        ingredients: [
            { id: 'ruby', count: 1 },
            { id: 'sapphire', count: 1 },
            { id: 'ancient_coin', count: 1 },
        ],
        hint: '보석과 고대의 힘을 합치면...',
    },
    elixir: {
        name: '만능약',
        result: 'elixir',
        category: 'potion',
        ingredients: [
            { id: 'heal_herb', count: 2 },
            { id: 'slime_jelly', count: 2 },
            { id: 'light_flower', count: 1 },
        ],
        hint: '치유초, 슬라임 젤리, 빛꽃의 조합...',
    },
};

// === 장인 등급 시스템 ===
const ARTISAN_CATEGORIES = ['potion', 'weapon', 'accessory'];

const ARTISAN_LEVELS = [
    { name: '견습', exp: 0,   perfectBonus: 0,    saveMaterialChance: 0,    color: '#aaaaaa' },
    { name: '숙련', exp: 10,  perfectBonus: 0.05, saveMaterialChance: 0.05, color: '#44ccff' },
    { name: '장인', exp: 30,  perfectBonus: 0.10, saveMaterialChance: 0.10, color: '#ffcc44' },
    { name: '명장', exp: 60,  perfectBonus: 0.15, saveMaterialChance: 0.15, color: '#ff88ff' },
    { name: '거장', exp: 100, perfectBonus: 0.20, saveMaterialChance: 0.20, color: '#ffaa00' },
];

// === 확장/투자 ===
const UPGRADES = {
    display_slot: {
        name: '진열대 추가',
        icon: '🪟',
        desc: '진열대 슬롯 +1',
        costs: [50, 100, 200, 400],  // 구매 횟수별 가격
        maxLevel: 4,
        effect: 'shopSlots',
    },
    craft_slot: {
        name: '제작대 추가',
        icon: '🔨',
        desc: '동시 제작 가능 수 +1',
        costs: [80, 200],
        maxLevel: 2,
        effect: 'craftSlots',
    },
    storage: {
        name: '창고 확장',
        icon: '📦',
        desc: '재고 보관 한계 증가',
        costs: [60, 120, 240],
        maxLevel: 3,
        effect: 'storageCap',
    },
};
