// ── RELIC DATA ─────────────────────────────────
// Passive items that define builds. Duplicate relics = stacking effects.

const RELIC_DATA = {
    // ── COMMON ──────────────────────────────────
    respin_charm: {
        id: 'respin_charm', name: '재회전 부적', icon: '🔄',
        desc: '스핀 후 10% 확률로 자동 리스핀 (중첩 시 확률 증가)',
        rarity: 'common', tier: 1,
        effect: { respin: 0.1 },
        shopCost: 15
    },
    thorn_armor: {
        id: 'thorn_armor', name: '가시 갑옷', icon: '🛡️',
        desc: '피격 시 공격자에게 반사 3 데미지 (중첩 시 증가)',
        rarity: 'common', tier: 1,
        effect: { thornsOnHit: 3 },
        shopCost: 12
    },
    golden_hand: {
        id: 'golden_hand', name: '황금 손', icon: '💰',
        desc: '금화 심볼 효과 2배 (중첩 시 4배, 8배…)',
        rarity: 'common', tier: 1,
        effect: { goldMultiplier: 2 },
        shopCost: 10
    },
    battle_drum: {
        id: 'battle_drum', name: '전투 북', icon: '🥁',
        desc: '전투 시작 시 방어 +5 (중첩 시 +10, +15…)',
        rarity: 'common', tier: 1,
        effect: { startBlock: 5 },
        shopCost: 10
    },
    iron_boots: {
        id: 'iron_boots', name: '무쇠 장화', icon: '🥾',
        desc: '매 턴 방어 +2 자동 획득 (중첩 시 +4, +6…)',
        rarity: 'common', tier: 1,
        effect: { turnBlock: 2 },
        shopCost: 12
    },
    lucky_coin: {
        id: 'lucky_coin', name: '행운의 동전', icon: '🪙',
        desc: '전투 승리 시 추가 골드 +3 (중첩 시 +6, +9…)',
        rarity: 'common', tier: 1,
        effect: { bonusGold: 3 },
        shopCost: 8
    },

    // ── UNCOMMON ─────────────────────────────────
    focus_lens: {
        id: 'focus_lens', name: '집중의 렌즈', icon: '🎯',
        desc: '같은 심볼 3개 이상 보유 시 출현율 +50%',
        rarity: 'uncommon', tier: 2,
        effect: { focusBoost: 0.5 },
        shopCost: 20
    },
    vampiric_fang: {
        id: 'vampiric_fang', name: '흡혈 송곳니', icon: '🧛',
        desc: '적 처치 시 HP 5 회복 (중첩 시 +10, +15…)',
        rarity: 'uncommon', tier: 2,
        effect: { killHeal: 5 },
        shopCost: 18
    },
    lucky_clover: {
        id: 'lucky_clover', name: '행운의 클로버', icon: '🍀',
        desc: '콤보 판정 시 와일드카드 확률 +10%',
        rarity: 'uncommon', tier: 2,
        effect: { wildChance: 0.1 },
        shopCost: 20
    },
    berserker_mark: {
        id: 'berserker_mark', name: '광전사의 표식', icon: '🔴',
        desc: 'HP 50% 이하일 때 공격 데미지 +50% (중첩 시 +100%…)',
        rarity: 'uncommon', tier: 2,
        effect: { berserk: 0.5 },
        shopCost: 18
    },
    ice_crystal: {
        id: 'ice_crystal', name: '빙결 수정', icon: '❄️',
        desc: '전투 시작 시 모든 적 쿨타임 +1 (중첩 시 +2, +3…)',
        rarity: 'uncommon', tier: 2,
        effect: { startSlow: 1 },
        shopCost: 16
    },
    flame_heart: {
        id: 'flame_heart', name: '불꽃 심장', icon: '🔥',
        desc: '매 스핀 시 랜덤 적 1명에게 화상 2 부여 (중첩 시 증가)',
        rarity: 'uncommon', tier: 2,
        effect: { autoBurn: 2 },
        shopCost: 18
    },
    mirror_shield: {
        id: 'mirror_shield', name: '거울 방패', icon: '🪞',
        desc: '방어가 100% 이상이면 초과분의 50%를 공격력으로 전환',
        rarity: 'uncommon', tier: 2,
        effect: { blockToDmg: 0.5 },
        shopCost: 22
    },
    soul_lantern: {
        id: 'soul_lantern', name: '영혼 등불', icon: '🏮',
        desc: '적 처치 시 최대HP +2 영구 증가 (중첩 시 +4, +6…)',
        rarity: 'uncommon', tier: 2,
        effect: { killMaxHp: 2 },
        shopCost: 20
    },

    // ── RARE ─────────────────────────────────────
    resonance_stone: {
        id: 'resonance_stone', name: '공명석', icon: '🔗',
        desc: '같은 심볼 2개 → 3개째 효과 추가 발동',
        rarity: 'rare', tier: 3,
        effect: { resonance: true },
        shopCost: 25
    },
    overcharge_core: {
        id: 'overcharge_core', name: '과부하 코어', icon: '⚡',
        desc: '콤보 발동 시 추가 스핀 1회',
        rarity: 'rare', tier: 3,
        effect: { comboExtraSpin: 1 },
        shopCost: 25
    },
    phoenix_feather: {
        id: 'phoenix_feather', name: '불사조 깃털', icon: '🐦',
        desc: '사망 시 1회 HP 30%로 부활 (중첩 시 여러 번)',
        rarity: 'rare', tier: 3,
        effect: { revive: true },
        shopCost: 30
    },
    chaos_orb: {
        id: 'chaos_orb', name: '혼돈의 오브', icon: '🌀',
        desc: '매 스핀 결과에 랜덤 심볼 1개 추가 (4번째 슬롯 효과)',
        rarity: 'rare', tier: 3,
        effect: { bonusSlot: 1 },
        shopCost: 28
    },
    time_crystal: {
        id: 'time_crystal', name: '시간의 수정', icon: '⏳',
        desc: '3턴마다 적 전체 쿨다운 +1 (중첩 시 2턴마다)',
        rarity: 'rare', tier: 3,
        effect: { periodicSlow: 3 },
        shopCost: 28
    },
};

const RELIC_POOLS = {
    common:   Object.values(RELIC_DATA).filter(r => r.rarity === 'common'),
    uncommon: Object.values(RELIC_DATA).filter(r => r.rarity === 'uncommon'),
    rare:     Object.values(RELIC_DATA).filter(r => r.rarity === 'rare'),
};

function getRelicChoices(count, ownedRelicIds) {
    // Allow duplicates — all relics always available
    const pool = [];
    pool.push(...RELIC_POOLS.common, ...RELIC_POOLS.common);
    pool.push(...RELIC_POOLS.uncommon);
    pool.push(...RELIC_POOLS.rare);
    const shuffled = pool.sort(() => Math.random() - 0.5);
    // Remove exact same relic appearing twice in choices
    const seen = new Set();
    const unique = shuffled.filter(r => {
        if (seen.has(r.id)) return false;
        seen.add(r.id);
        return true;
    });
    return unique.slice(0, count);
}
