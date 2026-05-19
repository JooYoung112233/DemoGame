const ENEMY_DATA = {
    slime: {
        id: 'slime', name: '슬라임', icon: '🟢',
        hp: 20, attack: 4, defense: 0,
        color: 0x44cc44, tier: 1,
        cooldown: 2,
        intents: [
            { type: 'attack', weight: 3 },
        ]
    },
    rat: {
        id: 'rat', name: '쥐', icon: '🐀',
        hp: 15, attack: 6, defense: 0,
        color: 0x886644, tier: 1,
        cooldown: 1,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'bleed', value: 3, weight: 1 },
        ]
    },
    bat: {
        id: 'bat', name: '박쥐', icon: '🦇',
        hp: 12, attack: 5, defense: 2,
        color: 0x664488, tier: 1,
        cooldown: 2,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'lifesteal', weight: 1 },
        ]
    },
    goblin: {
        id: 'goblin', name: '고블린', icon: '👺',
        hp: 35, attack: 8, defense: 2,
        color: 0x448844, tier: 2,
        cooldown: 2,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'attackBleed', bleed: 3, weight: 1 },
        ]
    },
    skeleton: {
        id: 'skeleton', name: '스켈레톤', icon: '💀',
        hp: 30, attack: 10, defense: 4,
        color: 0xccccaa, tier: 2,
        cooldown: 3,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'defend', block: 8, weight: 1 },
        ]
    },
    wolf: {
        id: 'wolf', name: '늑대', icon: '🐺',
        hp: 25, attack: 12, defense: 1,
        color: 0x888899, tier: 2,
        cooldown: 1,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'doubleStrike', weight: 1 },
        ]
    },
    orc: {
        id: 'orc', name: '오크', icon: '👹',
        hp: 55, attack: 12, defense: 6,
        color: 0x668844, tier: 3,
        cooldown: 3,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'defend', block: 12, weight: 1 },
            { type: 'buff', atkUp: 4, weight: 1 },
        ]
    },
    mage: {
        id: 'mage', name: '흑마법사', icon: '🧙',
        hp: 40, attack: 18, defense: 3,
        color: 0x6644aa, tier: 3,
        cooldown: 4,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'attackBurn', burn: 4, weight: 1 },
            { type: 'healAll', heal: 10, weight: 1 },
        ]
    },
    dragon: {
        id: 'dragon', name: '드래곤', icon: '🐉',
        hp: 100, attack: 20, defense: 8,
        color: 0xff4444, tier: 4,
        isBoss: true, cooldown: 3,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'attackBurn', burn: 5, weight: 1 },
            { type: 'defend', block: 15, weight: 1 },
            { type: 'buff', atkUp: 5, weight: 1 },
        ]
    },
};

const ROUND_ENEMIES = [
    { round: 1, pool: ['slime'], count: 1 },
    { round: 2, pool: ['slime', 'rat'], count: 2 },
    { round: 3, pool: ['rat', 'bat'], count: 2 },
    { round: 4, pool: ['goblin'], count: 2 },
    { round: 5, pool: ['goblin', 'skeleton'], count: 2 },
    { round: 6, pool: ['skeleton', 'wolf'], count: 2 },
    { round: 7, pool: ['orc'], count: 2 },
    { round: 8, pool: ['orc', 'mage'], count: 2 },
    { round: 9, pool: ['mage', 'orc'], count: 3 },
    { round: 10, pool: ['dragon'], count: 1 },
];
