const SYMBOL_DATA = {
    sword: {
        id: 'sword', name: '검', icon: '⚔️', tier: 1,
        color: 0xff4444, type: 'attack',
        effect: { damage: 8 },
        desc: '기본 근접 공격', cost: 4
    },
    arrow: {
        id: 'arrow', name: '화살', icon: '🏹', tier: 1,
        color: 0x44ff44, type: 'attack',
        effect: { damage: 6, pierce: true },
        desc: '방어 무시 공격', cost: 4
    },
    fire: {
        id: 'fire', name: '불꽃', icon: '🔥', tier: 1,
        color: 0xff8800, type: 'attack',
        effect: { damage: 5, aoe: true },
        desc: '전체 공격', cost: 5
    },
    dagger: {
        id: 'dagger', name: '단검', icon: '🗡️', tier: 1,
        color: 0xcc44cc, type: 'attack',
        effect: { damage: 4, hits: 2 },
        desc: '2회 연속 공격', cost: 4
    },
    axe: {
        id: 'axe', name: '도끼', icon: '🪓', tier: 2,
        color: 0xbb5533, type: 'attack',
        effect: { damage: 12 },
        desc: '높은 단일 데미지', cost: 6
    },
    bomb: {
        id: 'bomb', name: '폭탄', icon: '💣', tier: 2,
        color: 0xff3333, type: 'attack',
        effect: { damage: 8, aoe: true },
        desc: '강력한 전체 공격', cost: 7
    },
    lightning: {
        id: 'lightning', name: '번개', icon: '⚡', tier: 2,
        color: 0xffff44, type: 'attack',
        effect: { damage: 5, hits: 3 },
        desc: '3회 랜덤 공격', cost: 7
    },
    ice: {
        id: 'ice', name: '얼음', icon: '❄️', tier: 2,
        color: 0x88ccff, type: 'attack',
        effect: { damage: 6, slow: 1 },
        desc: '공격 + 적 쿨타임 1 증가', cost: 6
    },
    shield: {
        id: 'shield', name: '방패', icon: '🛡️', tier: 1,
        color: 0x4488ff, type: 'defense',
        effect: { block: 8 },
        desc: '방어막 획득', cost: 4
    },
    armor: {
        id: 'armor', name: '갑옷', icon: '🪖', tier: 1,
        color: 0x8888aa, type: 'defense',
        effect: { block: 5, thorns: 3 },
        desc: '방어 + 반사 데미지', cost: 5
    },
    fortress: {
        id: 'fortress', name: '성벽', icon: '🏰', tier: 2,
        color: 0x6688cc, type: 'defense',
        effect: { block: 14 },
        desc: '높은 방어막', cost: 7
    },
    potion: {
        id: 'potion', name: '포션', icon: '🧪', tier: 1,
        color: 0x44ff88, type: 'heal',
        effect: { heal: 8 },
        desc: 'HP 회복', cost: 4
    },
    herb: {
        id: 'herb', name: '약초', icon: '🌿', tier: 1,
        color: 0x33cc66, type: 'heal',
        effect: { heal: 5, block: 3 },
        desc: '소량 회복 + 방어', cost: 4
    },
    heart: {
        id: 'heart', name: '생명력', icon: '❤️', tier: 2,
        color: 0xff4488, type: 'heal',
        effect: { heal: 15 },
        desc: '대량 HP 회복', cost: 7
    },
    coin: {
        id: 'coin', name: '금화', icon: '🪙', tier: 1,
        color: 0xffcc00, type: 'gold',
        effect: { gold: 3 },
        desc: '골드 획득', cost: 3
    },
    crown: {
        id: 'crown', name: '왕관', icon: '👑', tier: 2,
        color: 0xffaa00, type: 'gold',
        effect: { gold: 6 },
        desc: '대량 골드 획득', cost: 6
    },
    gem: {
        id: 'gem', name: '보석', icon: '💎', tier: 3,
        color: 0x44ccff, type: 'wild',
        effect: {},
        desc: '와일드카드 (아무 심볼 대체)', cost: 12
    },
    skull: {
        id: 'skull', name: '해골', icon: '💀', tier: 1,
        color: 0x888888, type: 'curse',
        effect: { selfDamage: 3 },
        desc: '자해 데미지 (제거 추천)', cost: 0
    },
};

const COMBO_DATA = [
    {
        id: 'triple_sword', name: '검의 폭풍',
        symbols: ['sword', 'sword', 'sword'],
        effect: { damage: 30 },
        desc: '검 3개 — 대폭발 데미지',
        recipe: '⚔️⚔️⚔️'
    },
    {
        id: 'triple_shield', name: '철벽 방어',
        symbols: ['shield', 'shield', 'shield'],
        effect: { block: 30 },
        desc: '방패 3개 — 완전 방어',
        recipe: '🛡️🛡️🛡️'
    },
    {
        id: 'fire_sword', name: '화염검',
        symbols: ['sword', 'fire'], matchType: 'includes',
        effect: { damage: 20, burn: 3 },
        desc: '검+불 — 화염 데미지 + 화상',
        recipe: '⚔️🔥 + 아무거나'
    },
    {
        id: 'poison_dagger', name: '독날',
        symbols: ['dagger', 'potion'], matchType: 'includes',
        effect: { damage: 10, poison: 5 },
        desc: '단검+포션 — 독 데미지',
        recipe: '🗡️🧪 + 아무거나'
    },
    {
        id: 'shield_potion', name: '보호의 물약',
        symbols: ['shield', 'potion'], matchType: 'includes',
        effect: { block: 10, heal: 10 },
        desc: '방패+포션 — 방어+회복',
        recipe: '🛡️🧪 + 아무거나'
    },
    {
        id: 'arrow_fire', name: '화살비',
        symbols: ['arrow', 'fire'], matchType: 'includes',
        effect: { damage: 15, aoe: true },
        desc: '화살+불 — 전체 화염 화살',
        recipe: '🏹🔥 + 아무거나'
    },
    {
        id: 'triple_coin', name: '잭팟',
        symbols: ['coin', 'coin', 'coin'],
        effect: { gold: 20 },
        desc: '금화 3개 — 대박 골드',
        recipe: '🪙🪙🪙'
    },
    {
        id: 'ice_shield', name: '빙벽',
        symbols: ['ice', 'shield'], matchType: 'includes',
        effect: { block: 15, slow: 2 },
        desc: '얼음+방패 — 방어 + 적 둔화',
        recipe: '❄️🛡️ + 아무거나'
    },
    {
        id: 'lightning_dagger', name: '뇌전 난무',
        symbols: ['lightning', 'dagger'], matchType: 'includes',
        effect: { damage: 8, hits: 5 },
        desc: '번개+단검 — 5연타',
        recipe: '⚡🗡️ + 아무거나'
    },
    {
        id: 'bomb_fire', name: '대폭발',
        symbols: ['bomb', 'fire'], matchType: 'includes',
        effect: { damage: 25, aoe: true, burn: 2 },
        desc: '폭탄+불 — 전체 대폭발 + 화상',
        recipe: '💣🔥 + 아무거나'
    },
    {
        id: 'axe_sword', name: '이중 참격',
        symbols: ['axe', 'sword'], matchType: 'includes',
        effect: { damage: 28 },
        desc: '도끼+검 — 초강력 단일 공격',
        recipe: '🪓⚔️ + 아무거나'
    },
    {
        id: 'herb_heart', name: '생명의 축복',
        symbols: ['herb', 'heart'], matchType: 'includes',
        effect: { heal: 25, block: 8 },
        desc: '약초+생명력 — 대회복 + 방어',
        recipe: '🌿❤️ + 아무거나'
    },
    {
        id: 'all_different', name: '무지개',
        matchType: 'allDifferent',
        effect: { damage: 5, block: 5, heal: 5, gold: 3 },
        desc: '모두 다른 심볼 — 만능 효과',
        recipe: '전부 다른 심볼'
    },
];
