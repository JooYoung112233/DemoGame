const FARMING = {
    RARITIES: {
        common:    { name: '일반', color: '#aaaaaa', borderColor: 0x888888, valueMultiplier: 1 },
        uncommon:  { name: '고급', color: '#44ff88', borderColor: 0x44ff88, valueMultiplier: 2 },
        rare:      { name: '희귀', color: '#4488ff', borderColor: 0x4488ff, valueMultiplier: 5 },
        epic:      { name: '에픽', color: '#aa44ff', borderColor: 0xaa44ff, valueMultiplier: 12 },
        legendary: { name: '전설', color: '#ffaa00', borderColor: 0xffaa00, valueMultiplier: 30 },
        ancient:   { name: '고대', color: '#ff4488', borderColor: 0xff4488, valueMultiplier: 60 },
        mythical:  { name: '신화', color: '#ff2200', borderColor: 0xff2200, valueMultiplier: 150 }
    },

    RARITY_ORDER: ['common', 'uncommon', 'rare', 'epic', 'legendary', 'ancient', 'mythical'],

    RARITY_STAT_MULT: {
        common: 1.0, uncommon: 1.2, rare: 1.5,
        epic: 2.0, legendary: 2.5, ancient: 3.0, mythical: 4.0
    },

    STASH_CAPACITY: 30,
    SECURE_CONTAINER_SIZE: 3,
    MAX_WEIGHT: 40,

    CATEGORIES: {
        equipment: '장비',
        material: '거래재',
        consumable: '소모품',
        key: '열쇠'
    },

    SLOTS: {
        weapon: '무기',
        armor: '방어구',
        accessory: '악세서리'
    },

    ICON: {
        weapon: '⚔️',
        armor: '🛡️',
        accessory: '💍',
        material: '💎',
        consumable: '🧪',
        key: '🔑'
    }
};
