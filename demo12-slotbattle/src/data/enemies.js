// ── THE LAST THEATER — ENEMY DATA ─────────────────────
// 적 = 배역에 갇힌 배우들. 무대 위에서 연극을 반복하는 존재들.

const ENEMY_DATA = {
    // ── Act 1: 버려진 객석 ──────────────────────
    audience: {
        id: 'audience', name: '잊혀진 관객', icon: '👤',
        hp: 18, attack: 4, defense: 0,
        color: 0x888888, tier: 1,
        cooldown: 2,
        intents: [
            { type: 'attack', weight: 3 },
        ]
    },
    maskless: {
        id: 'maskless', name: '가면 없는 배우', icon: '🎭',
        hp: 22, attack: 5, defense: 1,
        color: 0xcc88aa, tier: 1,
        cooldown: 2,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'bleed', value: 2, weight: 1 },
        ]
    },
    stage_spider: {
        id: 'stage_spider', name: '무대 거미', icon: '🕷️',
        hp: 14, attack: 6, defense: 0,
        color: 0x664444, tier: 1,
        cooldown: 1,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'doubleStrike', weight: 1 },
        ]
    },
    shadow_dancer: {
        id: 'shadow_dancer', name: '춤추는 그림자', icon: '💃',
        hp: 20, attack: 5, defense: 2,
        color: 0x443366, tier: 1,
        cooldown: 2,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'defend', block: 6, weight: 1 },
        ]
    },

    // ── Act 2: 왕의 비극 ──────────────────────
    mad_king: {
        id: 'mad_king', name: '미친 왕', icon: '👑',
        hp: 32, attack: 9, defense: 3,
        color: 0xddaa22, tier: 2,
        cooldown: 2,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'buff', atkUp: 4, weight: 1 },
        ]
    },
    laughing_soldier: {
        id: 'laughing_soldier', name: '웃는 병사', icon: '🗡️',
        hp: 28, attack: 10, defense: 2,
        color: 0x886644, tier: 2,
        cooldown: 2,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'attackBleed', bleed: 3, weight: 1 },
        ]
    },
    singing_skull: {
        id: 'singing_skull', name: '노래하는 해골', icon: '💀',
        hp: 24, attack: 7, defense: 1,
        color: 0xccccaa, tier: 2,
        cooldown: 3,
        intents: [
            { type: 'attack', weight: 1 },
            { type: 'attackBurn', burn: 3, weight: 1 },
            { type: 'healAll', heal: 8, weight: 1 },
        ]
    },
    clapping_ghost: {
        id: 'clapping_ghost', name: '박수치는 관객', icon: '👻',
        hp: 26, attack: 8, defense: 2,
        color: 0x6644aa, tier: 2,
        cooldown: 2,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'defend', block: 8, weight: 1 },
            { type: 'buff', atkUp: 3, weight: 1 },
        ]
    },

    // ── BOSSES ──────────────────────────────
    stage_director: {
        id: 'stage_director', name: '무대감독', icon: '🎪',
        hp: 65, attack: 8, defense: 3,
        color: 0xcc4444, tier: 2,
        isBoss: true, cooldown: 2,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'attackBleed', bleed: 3, weight: 1 },
            { type: 'buff', atkUp: 3, weight: 1 },
            { type: 'defend', block: 10, weight: 1 },
        ]
    },
    tyrant: {
        id: 'tyrant', name: '폭군', icon: '👑',
        hp: 100, attack: 14, defense: 5,
        color: 0xff4444, tier: 3,
        isBoss: true, cooldown: 3,
        intents: [
            { type: 'attack', weight: 2 },
            { type: 'attackBurn', burn: 4, weight: 1 },
            { type: 'defend', block: 12, weight: 1 },
            { type: 'buff', atkUp: 5, weight: 1 },
        ]
    },
};

// ── ACT-BASED ENCOUNTER TABLES ──
const ACT_ENCOUNTERS = {
    1: {
        battle: [
            { pool: ['audience'], count: 1 },
            { pool: ['audience', 'maskless'], count: 2 },
            { pool: ['stage_spider', 'audience'], count: 2 },
            { pool: ['shadow_dancer', 'maskless'], count: 2 },
        ],
        elite: [
            { pool: ['shadow_dancer', 'stage_spider'], count: 2 },
        ],
    },
    2: {
        battle: [
            { pool: ['mad_king'], count: 1 },
            { pool: ['laughing_soldier', 'singing_skull'], count: 2 },
            { pool: ['clapping_ghost', 'laughing_soldier'], count: 2 },
            { pool: ['singing_skull', 'clapping_ghost'], count: 2 },
        ],
        elite: [
            { pool: ['mad_king', 'laughing_soldier'], count: 2 },
            { pool: ['clapping_ghost', 'clapping_ghost'], count: 2 },
        ],
    },
};

// legacy compat
const ROUND_ENEMIES = [
    { round: 1, pool: ['audience'], count: 1 },
    { round: 2, pool: ['audience', 'maskless'], count: 2 },
    { round: 3, pool: ['stage_spider', 'shadow_dancer'], count: 2 },
    { round: 4, pool: ['shadow_dancer', 'maskless'], count: 2 },
    { round: 5, pool: ['laughing_soldier', 'singing_skull'], count: 2 },
    { round: 6, pool: ['mad_king'], count: 1 },
    { round: 7, pool: ['clapping_ghost', 'laughing_soldier'], count: 2 },
    { round: 8, pool: ['mad_king', 'singing_skull'], count: 2 },
    { round: 9, pool: ['clapping_ghost', 'laughing_soldier', 'singing_skull'], count: 3 },
    { round: 10, pool: ['tyrant'], count: 1 },
];
