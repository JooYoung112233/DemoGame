// ── THE LAST THEATER — RELIC DATA ─────────────────────
// 유물 = 감독의 소품. 무대 뒤에서 발견한 연출 도구들.

const RELIC_DATA = {
    // ── COMMON ──────────────────────────────────
    respin_charm: {
        id: 'respin_charm', name: '무대 반복 장치', icon: '🔄',
        desc: '연출 후 10% 확률로 자동 재연출 (중첩 시 확률 증가)',
        rarity: 'common', tier: 1,
        effect: { respin: 0.1 },
        shopCost: 15
    },
    thorn_armor: {
        id: 'thorn_armor', name: '가시 의상', icon: '🪡',
        desc: '피격 시 공격자에게 반사 3 데미지 (중첩 시 증가)',
        rarity: 'common', tier: 1,
        effect: { thornsOnHit: 3 },
        shopCost: 12
    },
    golden_hand: {
        id: 'golden_hand', name: '황금 가면', icon: '🎭',
        desc: '축제 심볼 골드 효과 2배 (중첩 시 4배, 8배…)',
        rarity: 'common', tier: 1,
        effect: { goldMultiplier: 2 },
        shopCost: 10
    },
    battle_drum: {
        id: 'battle_drum', name: '개막 북', icon: '🥁',
        desc: '공연 시작 시 방어 +5 (중첩 시 +10, +15…)',
        rarity: 'common', tier: 1,
        effect: { startBlock: 5 },
        shopCost: 10
    },
    iron_boots: {
        id: 'iron_boots', name: '무거운 커튼', icon: '🎪',
        desc: '매 턴 방어 +2 자동 획득 (중첩 시 +4, +6…)',
        rarity: 'common', tier: 1,
        effect: { turnBlock: 2 },
        shopCost: 12
    },
    lucky_coin: {
        id: 'lucky_coin', name: '행운의 티켓', icon: '🎫',
        desc: '공연 성공 시 추가 골드 +3 (중첩 시 +6, +9…)',
        rarity: 'common', tier: 1,
        effect: { bonusGold: 3 },
        shopCost: 8
    },

    // ── UNCOMMON ─────────────────────────────────
    focus_lens: {
        id: 'focus_lens', name: '감독의 돋보기', icon: '🔍',
        desc: '같은 심볼 3개 이상 보유 시 출현율 +50%',
        rarity: 'uncommon', tier: 2,
        effect: { focusBoost: 0.5 },
        shopCost: 20
    },
    vampiric_fang: {
        id: 'vampiric_fang', name: '흡혈 가면', icon: '🦇',
        desc: '배우 퇴장 시 HP 5 회복 (중첩 시 +10, +15…)',
        rarity: 'uncommon', tier: 2,
        effect: { killHeal: 5 },
        shopCost: 18
    },
    lucky_clover: {
        id: 'lucky_clover', name: '행운의 대본', icon: '📜',
        desc: '연출 시 와일드카드 확률 +10%',
        rarity: 'uncommon', tier: 2,
        effect: { wildChance: 0.1 },
        shopCost: 20
    },
    berserker_mark: {
        id: 'berserker_mark', name: '광기의 분장', icon: '🔴',
        desc: 'HP 50% 이하일 때 데미지 +50% (중첩 시 +100%…)',
        rarity: 'uncommon', tier: 2,
        effect: { berserk: 0.5 },
        shopCost: 18
    },
    ice_crystal: {
        id: 'ice_crystal', name: '얼어붙은 시계', icon: '🕰️',
        desc: '공연 시작 시 모든 배우 쿨타임 +1 (중첩 시 +2, +3…)',
        rarity: 'uncommon', tier: 2,
        effect: { startSlow: 1 },
        shopCost: 16
    },
    flame_heart: {
        id: 'flame_heart', name: '불꽃 조명', icon: '💡',
        desc: '매 연출 시 랜덤 배우 1명에게 화상 2 부여 (중첩 시 증가)',
        rarity: 'uncommon', tier: 2,
        effect: { autoBurn: 2 },
        shopCost: 18
    },
    mirror_shield: {
        id: 'mirror_shield', name: '무대 거울', icon: '🪞',
        desc: '방어가 100% 이상이면 초과분의 50%를 데미지로 전환',
        rarity: 'uncommon', tier: 2,
        effect: { blockToDmg: 0.5 },
        shopCost: 22
    },
    soul_lantern: {
        id: 'soul_lantern', name: '영혼의 촛불', icon: '🕯️',
        desc: '배우 퇴장 시 최대HP +2 영구 증가 (중첩 시 +4, +6…)',
        rarity: 'uncommon', tier: 2,
        effect: { killMaxHp: 2 },
        shopCost: 20
    },

    // ── RARE ─────────────────────────────────────
    resonance_stone: {
        id: 'resonance_stone', name: '공명하는 대본', icon: '📖',
        desc: '같은 심볼 2개 → 3개째 효과 추가 발동',
        rarity: 'rare', tier: 3,
        effect: { resonance: true },
        shopCost: 25
    },
    overcharge_core: {
        id: 'overcharge_core', name: '앙코르 종', icon: '🔔',
        desc: 'Scene 발동 시 추가 연출 1회',
        rarity: 'rare', tier: 3,
        effect: { comboExtraSpin: 1 },
        shopCost: 25
    },
    phoenix_feather: {
        id: 'phoenix_feather', name: '불사조의 깃펜', icon: '🪶',
        desc: '커튼콜 — 쓰러져도 1회 HP 30%로 부활 (중첩 시 여러 번)',
        rarity: 'rare', tier: 3,
        effect: { revive: true },
        shopCost: 30
    },
    chaos_orb: {
        id: 'chaos_orb', name: '즉흥의 수정구', icon: '🔮',
        desc: '매 연출에 랜덤 심볼 1개 추가 (4번째 슬롯 효과)',
        rarity: 'rare', tier: 3,
        effect: { bonusSlot: 1 },
        shopCost: 28
    },
    time_crystal: {
        id: 'time_crystal', name: '시간의 모래시계', icon: '⏳',
        desc: '3턴마다 배우 전체 쿨다운 +1 (중첩 시 2턴마다)',
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
