// ── RELIC DATA ─────────────────────────────────
// Passive items that define builds

const RELIC_DATA = {
    respin_charm: {
        id: 'respin_charm', name: '재회전 부적', icon: '🔄',
        desc: '턴당 리스핀 1회 가능',
        rarity: 'common', tier: 1,
        effect: { respin: 1 },
        shopCost: 15
    },
    focus_lens: {
        id: 'focus_lens', name: '집중의 렌즈', icon: '🎯',
        desc: '같은 심볼 3개 이상 보유 시 출현율 +50%',
        rarity: 'uncommon', tier: 2,
        effect: { focusBoost: 0.5 },
        shopCost: 20
    },
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
    thorn_armor: {
        id: 'thorn_armor', name: '가시 갑옷', icon: '🛡️',
        desc: '피격 시 공격자에게 반사 3 데미지',
        rarity: 'common', tier: 1,
        effect: { thornsOnHit: 3 },
        shopCost: 12
    },
    golden_hand: {
        id: 'golden_hand', name: '황금 손', icon: '💰',
        desc: '금화 심볼 효과 2배',
        rarity: 'common', tier: 1,
        effect: { goldMultiplier: 2 },
        shopCost: 10
    },
    battle_drum: {
        id: 'battle_drum', name: '전투 북', icon: '🥁',
        desc: '전투 시작 시 방어 +5',
        rarity: 'common', tier: 1,
        effect: { startBlock: 5 },
        shopCost: 10
    },
    vampiric_fang: {
        id: 'vampiric_fang', name: '흡혈 송곳니', icon: '🧛',
        desc: '적 처치 시 HP 5 회복',
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
        desc: 'HP 50% 이하일 때 공격 데미지 +50%',
        rarity: 'uncommon', tier: 2,
        effect: { berserk: 0.5 },
        shopCost: 18
    },
    ice_crystal: {
        id: 'ice_crystal', name: '빙결 수정', icon: '❄️',
        desc: '전투 시작 시 모든 적 쿨타임 +1',
        rarity: 'uncommon', tier: 2,
        effect: { startSlow: 1 },
        shopCost: 16
    },
    phoenix_feather: {
        id: 'phoenix_feather', name: '불사조 깃털', icon: '🔥',
        desc: '사망 시 1회 HP 30%로 부활 (1회용)',
        rarity: 'rare', tier: 3,
        effect: { revive: true },
        shopCost: 30
    },
};

const RELIC_POOLS = {
    common:   Object.values(RELIC_DATA).filter(r => r.rarity === 'common'),
    uncommon: Object.values(RELIC_DATA).filter(r => r.rarity === 'uncommon'),
    rare:     Object.values(RELIC_DATA).filter(r => r.rarity === 'rare'),
};

function getRelicChoices(count, ownedRelicIds) {
    const pool = [];
    pool.push(...RELIC_POOLS.common, ...RELIC_POOLS.common);
    pool.push(...RELIC_POOLS.uncommon);
    pool.push(...RELIC_POOLS.rare);
    const filtered = pool.filter(r => !ownedRelicIds.includes(r.id));
    const shuffled = filtered.sort(() => Math.random() - 0.5);
    return shuffled.slice(0, count);
}
