// ── THE LAST THEATER — SYMBOL & SCENE DATA ─────────────────
// 심볼 = 연출 재료, Scene = 조합으로 만들어지는 장면

const SYMBOL_DATA = {
    // ── 역할 (Role) ──────────────────────────────
    knight: {
        id: 'knight', name: '기사', icon: '⚔️', tier: 1,
        color: 0xff4444, type: 'role', category: '역할',
        effect: { damage: 5 },
        desc: '무대 위 충직한 검사', cost: 4
    },
    king: {
        id: 'king', name: '왕', icon: '👑', tier: 2,
        color: 0xffaa00, type: 'role', category: '역할',
        effect: { damage: 7 },
        desc: '권좌의 무게를 짊어진 자', cost: 6
    },
    jester: {
        id: 'jester', name: '광대', icon: '🎭', tier: 1,
        color: 0xcc44cc, type: 'role', category: '역할',
        effect: { damage: 3, hits: 2 },
        desc: '웃음 뒤에 칼날을 숨긴 자', cost: 4
    },
    priest: {
        id: 'priest', name: '사제', icon: '🕯️', tier: 1,
        color: 0x44ff88, type: 'role', category: '역할',
        effect: { heal: 5 },
        desc: '촛불로 상처를 어루만지는 자', cost: 4
    },
    dead: {
        id: 'dead', name: '망자', icon: '💀', tier: 1,
        color: 0x888888, type: 'curse', category: '역할',
        effect: { selfDamage: 3 },
        desc: '저주받은 배역 (제거 추천)', cost: 0
    },

    // ── 감정 (Emotion) ──────────────────────────────
    rage: {
        id: 'rage', name: '분노', icon: '🔥', tier: 1,
        color: 0xff8800, type: 'emotion', category: '감정',
        effect: { damage: 4, burn: 2 },
        desc: '무대를 태우는 격정', cost: 5
    },
    tragedy: {
        id: 'tragedy', name: '비극', icon: '🩸', tier: 2,
        color: 0xcc2244, type: 'emotion', category: '감정',
        effect: { damage: 6, poison: 2 },
        desc: '피로 물든 비극의 장면', cost: 6
    },
    cold: {
        id: 'cold', name: '냉기', icon: '❄️', tier: 1,
        color: 0x88ccff, type: 'emotion', category: '감정',
        effect: { damage: 3, slow: 1 },
        desc: '시간을 얼리는 겨울의 감정', cost: 5
    },
    fear: {
        id: 'fear', name: '공포', icon: '👻', tier: 2,
        color: 0x8844cc, type: 'emotion', category: '감정',
        effect: { damage: 4, slow: 1 },
        desc: '관객의 등줄기를 서늘케 하는 공포', cost: 5
    },

    // ── 무대 (Stage) ──────────────────────────────
    festival: {
        id: 'festival', name: '축제', icon: '🎪', tier: 1,
        color: 0xffcc00, type: 'stage', category: '무대',
        effect: { gold: 3 },
        desc: '화려한 축제 장면', cost: 3
    },
    blackout: {
        id: 'blackout', name: '암전', icon: '🌑', tier: 1,
        color: 0x4466aa, type: 'stage', category: '무대',
        effect: { block: 6 },
        desc: '어둠 속에 숨는 장면', cost: 4
    },
    mirror: {
        id: 'mirror', name: '거울', icon: '🪞', tier: 2,
        color: 0x88aacc, type: 'stage', category: '무대',
        effect: { block: 4, thorns: 3 },
        desc: '비춰진 것이 되돌아오는 장면', cost: 5
    },
    prison: {
        id: 'prison', name: '감옥', icon: '⛓️', tier: 2,
        color: 0x886644, type: 'stage', category: '무대',
        effect: { damage: 4, slow: 1 },
        desc: '쇠사슬로 무대를 옥죄는 장면', cost: 5
    },

    // ── 기타 ──────────────────────────────
    dagger: {
        id: 'dagger', name: '단검', icon: '🗡️', tier: 1,
        color: 0xcc6644, type: 'role', category: '역할',
        effect: { damage: 4 },
        desc: '암막 뒤에서 번뜩이는 칼날', cost: 4
    },
    rose: {
        id: 'rose', name: '장미', icon: '🌹', tier: 1,
        color: 0xff4488, type: 'stage', category: '무대',
        effect: { heal: 4, block: 2 },
        desc: '아름다운 한 송이, 가시를 품은 회복', cost: 4
    },
    gem: {
        id: 'gem', name: '보석', icon: '💎', tier: 3,
        color: 0x44ccff, type: 'wild', category: '특수',
        effect: {},
        desc: '와일드카드 — 어떤 배역이든 대체', cost: 12
    },
};

// ── SCENE DATA (조합 = 장면 연출) ─────────────────────
const COMBO_DATA = [
    // ── 트리플 매치 (같은 심볼 3개) ──────────────
    {
        id: 'knights_oath', name: '「기사의 맹세」',
        symbols: ['knight', 'knight', 'knight'],
        effect: { damage: 20 },
        desc: '기사 ×3 — 맹세의 일격',
        recipe: '⚔️⚔️⚔️'
    },
    {
        id: 'kings_soliloquy', name: '「왕의 독백」',
        symbols: ['king', 'king', 'king'],
        effect: { damage: 28 },
        desc: '왕 ×3 — 권좌에서 내리치다',
        recipe: '👑👑👑'
    },
    {
        id: 'jesters_frenzy', name: '「광대의 난무」',
        symbols: ['jester', 'jester', 'jester'],
        effect: { damage: 6, hits: 4 },
        desc: '광대 ×3 — 미친 듯이 휘두르다',
        recipe: '🎭🎭🎭'
    },
    {
        id: 'burning_stage', name: '「불타는 무대」',
        symbols: ['rage', 'rage', 'rage'],
        effect: { damage: 10, aoe: true, burn: 3 },
        desc: '분노 ×3 — 무대 전체가 불타오르다',
        recipe: '🔥🔥🔥'
    },
    {
        id: 'frozen_opera', name: '「얼어붙은 오페라」',
        symbols: ['cold', 'cold', 'cold'],
        effect: { damage: 8, aoe: true, slow: 3 },
        desc: '냉기 ×3 — 무대가 얼어붙다',
        recipe: '❄️❄️❄️'
    },
    {
        id: 'total_blackout', name: '「완전한 암전」',
        symbols: ['blackout', 'blackout', 'blackout'],
        effect: { block: 24 },
        desc: '암전 ×3 — 어둠 속 절대 방어',
        recipe: '🌑🌑🌑'
    },
    {
        id: 'act_of_blood', name: '「피의 3막」',
        symbols: ['tragedy', 'tragedy', 'tragedy'],
        effect: { damage: 18, poison: 4 },
        desc: '비극 ×3 — 피로 쓴 대본',
        recipe: '🩸🩸🩸'
    },
    {
        id: 'triple_festival', name: '「성대한 축연」',
        symbols: ['festival', 'festival', 'festival'],
        effect: { gold: 18 },
        desc: '축제 ×3 — 관객의 환호와 금화의 비',
        recipe: '🎪🎪🎪'
    },

    // ── 씬 조합 (2심볼 includes) ──────────────
    {
        id: 'kings_execution', name: '「왕의 처형」',
        symbols: ['king', 'rage'], matchType: 'includes',
        effect: { damage: 22, burn: 3 },
        desc: '왕+분노 — 화형에 처하다',
        recipe: '👑🔥 + 아무거나'
    },
    {
        id: 'bloody_duel', name: '「피의 결투」',
        symbols: ['knight', 'tragedy'], matchType: 'includes',
        effect: { damage: 16, poison: 3 },
        desc: '기사+비극 — 피 흘리며 싸우다',
        recipe: '⚔️🩸 + 아무거나'
    },
    {
        id: 'sacred_blessing', name: '「성스러운 축복」',
        symbols: ['priest', 'rose'], matchType: 'includes',
        effect: { heal: 14, block: 8 },
        desc: '사제+장미 — 빛의 치유',
        recipe: '🕯️🌹 + 아무거나'
    },
    {
        id: 'winters_curtain', name: '「겨울의 장막」',
        symbols: ['cold', 'blackout'], matchType: 'includes',
        effect: { block: 14, slow: 2 },
        desc: '냉기+암전 — 서리로 뒤덮인 장막',
        recipe: '❄️🌑 + 아무거나'
    },
    {
        id: 'horror_puppet', name: '「공포의 인형극」',
        symbols: ['jester', 'fear'], matchType: 'includes',
        effect: { damage: 12, aoe: true },
        desc: '광대+공포 — 관객 모두를 공포에 빠뜨리다',
        recipe: '🎭👻 + 아무거나'
    },
    {
        id: 'prison_judgment', name: '「감옥의 심판」',
        symbols: ['prison', 'knight'], matchType: 'includes',
        effect: { damage: 16, pierce: true },
        desc: '감옥+기사 — 방어를 무시하는 처형',
        recipe: '⛓️⚔️ + 아무거나'
    },
    {
        id: 'mirror_tragedy', name: '「거울 속 비극」',
        symbols: ['mirror', 'tragedy'], matchType: 'includes',
        effect: { damage: 12, block: 6, poison: 2 },
        desc: '거울+비극 — 비춰진 고통이 되돌아오다',
        recipe: '🪞🩸 + 아무거나'
    },
    {
        id: 'grand_finale', name: '「성대한 피날레」',
        symbols: ['festival', 'king'], matchType: 'includes',
        effect: { gold: 8, damage: 8 },
        desc: '축제+왕 — 왕에게 바치는 최후의 축제',
        recipe: '🎪👑 + 아무거나'
    },
    // ── 올디퍼런트 ──────────────
    {
        id: 'improvisation', name: '「즉흥 연극」',
        matchType: 'allDifferent',
        effect: { damage: 5, block: 5, heal: 4, gold: 3 },
        desc: '모두 다른 심볼 — 감독의 즉흥 연출',
        recipe: '전부 다른 심볼'
    },
];
