// === 경매 전용 아이템 풀 ===
// 경매에서만 나오는 장비. 고유효과·세트·유니크로 매력도 확보.
// common/uncommon = 강화된 스탯, rare+ = 패시브 효과, epic+ = 세트, legendary = 유니크

const AUCTION_PASSIVES = {
    // key: { name, desc, effect: { type, ...params } }
    lifesteal:    { name: '흡혈', desc: '공격 피해의 8% HP 회복', effect: { type: 'lifesteal', pct: 0.08 } },
    thorns:       { name: '가시', desc: '피격 시 공격자에게 고정 5 피해', effect: { type: 'thorns', flat: 5 } },
    swift:        { name: '질풍', desc: '전투 시작 시 SPD +20% (3초)', effect: { type: 'buff_start', stat: 'spd', pct: 0.20, duration: 3000 } },
    ironwall:     { name: '철벽', desc: 'HP 30% 이하 시 DEF 2배 (1회)', effect: { type: 'threshold_buff', stat: 'def', mult: 2, hpPct: 0.3, once: true } },
    execute:      { name: '처형', desc: '적 HP 20% 이하 시 ATK +50%', effect: { type: 'execute', hpPct: 0.2, atkMult: 1.5 } },
    regenerate:   { name: '재생', desc: '매 5초 HP 3% 회복', effect: { type: 'regen', interval: 5000, pct: 0.03 } },
    critboost:    { name: '급소', desc: '크리티컬 확률 +12%', effect: { type: 'stat_flat', stat: 'critRate', value: 0.12 } },
    doubleStrike: { name: '연격', desc: '20% 확률로 2회 공격', effect: { type: 'double_strike', chance: 0.20 } },
    guardian:     { name: '수호', desc: '인접 아군 받는 피해 -10%', effect: { type: 'aura', stat: 'dmgReduce', pct: 0.10 } },
    vengeance:    { name: '복수', desc: '아군 사망 시 ATK +25% (영구 누적)', effect: { type: 'on_ally_death', stat: 'atk', pct: 0.25 } },
    goldFind:     { name: '황금눈', desc: '전투 보상 골드 +15%', effect: { type: 'gold_bonus', pct: 0.15 } },
    expBoost:     { name: '수련', desc: '획득 경험치 +20%', effect: { type: 'exp_bonus', pct: 0.20 } }
};

const AUCTION_SETS = {
    bloodKnight: {
        name: '혈기사',
        pieces: ['blood_blade', 'blood_plate', 'blood_ring'],
        bonuses: {
            2: { desc: '흡혈 +5%', effect: { type: 'lifesteal', pct: 0.05 } },
            3: { desc: 'HP 50% 이하 시 ATK +30%, 흡혈 +10%', effect: { type: 'threshold_buff', hpPct: 0.5, atk: 0.30, lifesteal: 0.10 } }
        }
    },
    shadowWalker: {
        name: '그림자 보행자',
        pieces: ['shadow_dagger', 'shadow_cloak', 'shadow_charm'],
        bonuses: {
            2: { desc: 'SPD +15%, 첫 공격 크리티컬 확정', effect: { type: 'first_crit', spdBonus: 0.15 } },
            3: { desc: '회피 +20%, 회피 시 다음 공격 2배', effect: { type: 'evasion_counter', dodgePct: 0.20, counterMult: 2.0 } }
        }
    },
    ironFortress: {
        name: '철의 요새',
        pieces: ['fortress_shield', 'fortress_helm', 'fortress_sigil'],
        bonuses: {
            2: { desc: 'DEF +20%, 피격 시 20% 확률로 경직 무효', effect: { type: 'def_bonus', pct: 0.20, stunResist: 0.20 } },
            3: { desc: '도발 시작 + 받는 피해 -30%', effect: { type: 'auto_taunt', dmgReduce: 0.30 } }
        }
    },
    stormCaller: {
        name: '폭풍 소환사',
        pieces: ['storm_staff', 'storm_robe', 'storm_orb'],
        bonuses: {
            2: { desc: 'ATK +15%, 스킬 쿨다운 -1턴', effect: { type: 'atk_cdr', atkPct: 0.15, cdr: 1 } },
            3: { desc: '3회 공격마다 연쇄번개 (ATK×0.5, 적 전체)', effect: { type: 'chain_lightning', every: 3, mult: 0.5 } }
        }
    }
};

const AUCTION_EQUIPMENT_POOL = [
    // === 무기 ===
    // common
    { id: 'auction_sword_1', name: '연마된 장검', slot: 'weapon', tier: 'common',
      stats: { atk: 12, def: 0, hp: 0 }, value: 40, desc: '꽤 잘 벼려진 검' },
    { id: 'auction_bow_1', name: '복합궁', slot: 'weapon', tier: 'common',
      stats: { atk: 10, def: 0, hp: 0, critRate: 0.03 }, value: 45, desc: '복합 재질의 강궁' },
    { id: 'auction_staff_1', name: '수정 지팡이', slot: 'weapon', tier: 'common',
      stats: { atk: 14, def: 0, hp: 0 }, value: 42, desc: '맑은 수정이 박힌 지팡이' },

    // uncommon
    { id: 'auction_axe_2', name: '도끼: 이리갈래', slot: 'weapon', tier: 'uncommon',
      stats: { atk: 18, def: 0, hp: 0 }, value: 70, desc: '묵직한 전투도끼. 일격이 무겁다' },
    { id: 'auction_dagger_2', name: '쌍단검: 은빛', slot: 'weapon', tier: 'uncommon',
      stats: { atk: 14, def: 0, hp: 0, critRate: 0.06 }, value: 75, desc: '빠른 연속 공격에 특화' },

    // rare (패시브 부착)
    { id: 'auction_sword_3', name: '피갈증의 검', slot: 'weapon', tier: 'rare',
      stats: { atk: 22, def: 0, hp: 0 }, passive: 'lifesteal', value: 140,
      desc: '벨 때마다 붉게 빛난다' },
    { id: 'auction_bow_3', name: '처형자의 활', slot: 'weapon', tier: 'rare',
      stats: { atk: 18, def: 0, hp: 0, critRate: 0.05 }, passive: 'execute', value: 150,
      desc: '약한 적에게 더 치명적' },
    { id: 'auction_staff_3', name: '연격의 완드', slot: 'weapon', tier: 'rare',
      stats: { atk: 25, def: 0, hp: 0 }, passive: 'doubleStrike', value: 160,
      desc: '가끔 마력이 두 번 터진다' },

    // epic (세트 피스)
    { id: 'blood_blade', name: '혈기사의 대검', slot: 'weapon', tier: 'epic',
      stats: { atk: 30, def: 0, hp: 20 }, passive: 'lifesteal', set: 'bloodKnight', value: 280,
      desc: '피를 먹고 자라는 검. 세트: 혈기사' },
    { id: 'shadow_dagger', name: '그림자 비수', slot: 'weapon', tier: 'epic',
      stats: { atk: 25, def: 0, hp: 0, critRate: 0.10 }, passive: 'swift', set: 'shadowWalker', value: 290,
      desc: '그림자에서 태어난 단검. 세트: 그림자 보행자' },
    { id: 'storm_staff', name: '뇌신의 지팡이', slot: 'weapon', tier: 'epic',
      stats: { atk: 35, def: 0, hp: 0 }, passive: 'doubleStrike', set: 'stormCaller', value: 300,
      desc: '번개가 깃든 지팡이. 세트: 폭풍 소환사' },
    { id: 'fortress_shield', name: '요새의 방패검', slot: 'weapon', tier: 'epic',
      stats: { atk: 15, def: 12, hp: 40 }, passive: 'guardian', set: 'ironFortress', value: 270,
      desc: '공격과 방어를 겸한다. 세트: 철의 요새' },

    // legendary (유니크)
    { id: 'leg_crimson', name: '진홍의 유산 — 아르가스', slot: 'weapon', tier: 'legendary',
      stats: { atk: 45, def: 5, hp: 30 }, passive: 'vengeance', value: 600,
      lore: '몰락한 길드장의 유품. 동료를 잃을수록 강해진다.',
      desc: '유니크. 아군 사망 시 ATK +25% 영구 누적' },
    { id: 'leg_tempest', name: '종말의 폭풍 — 케라우노스', slot: 'weapon', tier: 'legendary',
      stats: { atk: 50, def: 0, hp: 0 }, passive: 'doubleStrike', value: 650,
      lore: '하늘의 노여움을 담은 지팡이. 쥐는 자의 정신을 갈아먹는다.',
      desc: '유니크. 20% 확률 2회 공격 + ATK 최고치' },

    // === 방어구 ===
    // common
    { id: 'auction_leather_1', name: '강화 가죽갑', slot: 'armor', tier: 'common',
      stats: { atk: 0, def: 8, hp: 30 }, value: 38, desc: '경매장에서 자주 볼 수 있는 갑옷' },
    { id: 'auction_robe_1', name: '마력 로브', slot: 'armor', tier: 'common',
      stats: { atk: 4, def: 4, hp: 20 }, value: 40, desc: '마력이 스며든 천 갑옷' },

    // uncommon
    { id: 'auction_chain_2', name: '미스릴 쇄갑', slot: 'armor', tier: 'uncommon',
      stats: { atk: 0, def: 14, hp: 50 }, value: 80, desc: '가볍지만 단단하다' },
    { id: 'auction_robe_2', name: '현자의 외투', slot: 'armor', tier: 'uncommon',
      stats: { atk: 6, def: 6, hp: 30 }, value: 75, desc: '지식과 보호를 동시에' },

    // rare
    { id: 'auction_plate_3', name: '가시 갑옷', slot: 'armor', tier: 'rare',
      stats: { atk: 0, def: 18, hp: 60 }, passive: 'thorns', value: 160,
      desc: '치면 아프다. 피격 시 고정 피해 반사' },
    { id: 'auction_robe_3', name: '재생의 로브', slot: 'armor', tier: 'rare',
      stats: { atk: 5, def: 8, hp: 40 }, passive: 'regenerate', value: 145,
      desc: '살아있는 천. 서서히 상처를 아물게 한다' },

    // epic (세트 피스)
    { id: 'blood_plate', name: '혈기사의 흉갑', slot: 'armor', tier: 'epic',
      stats: { atk: 5, def: 20, hp: 80 }, passive: 'regenerate', set: 'bloodKnight', value: 300,
      desc: '피로 물든 갑옷. 세트: 혈기사' },
    { id: 'shadow_cloak', name: '그림자 망토', slot: 'armor', tier: 'epic',
      stats: { atk: 8, def: 10, hp: 30 }, passive: 'swift', set: 'shadowWalker', value: 280,
      desc: '빛을 삼키는 망토. 세트: 그림자 보행자' },
    { id: 'storm_robe', name: '번개구름 법의', slot: 'armor', tier: 'epic',
      stats: { atk: 12, def: 8, hp: 35 }, passive: 'critboost', set: 'stormCaller', value: 290,
      desc: '전류가 흐르는 법복. 세트: 폭풍 소환사' },
    { id: 'fortress_helm', name: '요새의 투구', slot: 'armor', tier: 'epic',
      stats: { atk: 0, def: 25, hp: 100 }, passive: 'ironwall', set: 'ironFortress', value: 310,
      desc: '무너지지 않는 성벽. 세트: 철의 요새' },

    // legendary
    { id: 'leg_undying', name: '불멸의 판금 — 이지스', slot: 'armor', tier: 'legendary',
      stats: { atk: 0, def: 35, hp: 150 }, passive: 'ironwall', value: 620,
      lore: '한 번 죽었다가 돌아온 기사의 갑옷. 절대 부서지지 않는다.',
      desc: '유니크. HP 30% 이하 DEF 2배 + 최고 방어력' },

    // === 악세서리 ===
    // common
    { id: 'auction_ring_1', name: '은빛 반지', slot: 'accessory', tier: 'common',
      stats: { atk: 3, def: 3, hp: 10, critRate: 0.03 }, value: 35, desc: '무난한 장신구' },
    { id: 'auction_neck_1', name: '호박 목걸이', slot: 'accessory', tier: 'common',
      stats: { atk: 0, def: 5, hp: 25 }, value: 35, desc: '따뜻한 기운이 감돈다' },

    // uncommon
    { id: 'auction_ring_2', name: '사파이어 인장', slot: 'accessory', tier: 'uncommon',
      stats: { atk: 5, def: 0, hp: 0, critRate: 0.08 }, value: 80, desc: '예리한 눈빛' },
    { id: 'auction_charm_2', name: '전투 부적', slot: 'accessory', tier: 'uncommon',
      stats: { atk: 8, def: 4, hp: 15 }, value: 72, desc: '전투 시 용기를 준다' },

    // rare
    { id: 'auction_ring_3', name: '황금눈 반지', slot: 'accessory', tier: 'rare',
      stats: { atk: 4, def: 4, hp: 0 }, passive: 'goldFind', value: 130,
      desc: '이 반지를 낀 자는 돈이 보인다' },
    { id: 'auction_charm_3', name: '수련자의 부적', slot: 'accessory', tier: 'rare',
      stats: { atk: 6, def: 6, hp: 20 }, passive: 'expBoost', value: 135,
      desc: '경험이 빠르게 쌓인다' },

    // epic (세트 피스)
    { id: 'blood_ring', name: '혈기사의 인장', slot: 'accessory', tier: 'epic',
      stats: { atk: 10, def: 5, hp: 30, critRate: 0.06 }, passive: 'lifesteal', set: 'bloodKnight', value: 260,
      desc: '피의 맹세. 세트: 혈기사' },
    { id: 'shadow_charm', name: '그림자의 눈', slot: 'accessory', tier: 'epic',
      stats: { atk: 12, def: 0, hp: 0, critRate: 0.12 }, passive: 'execute', set: 'shadowWalker', value: 270,
      desc: '약점을 꿰뚫어 본다. 세트: 그림자 보행자' },
    { id: 'storm_orb', name: '번개 구슬', slot: 'accessory', tier: 'epic',
      stats: { atk: 15, def: 0, hp: 20 }, passive: 'doubleStrike', set: 'stormCaller', value: 280,
      desc: '마력을 증폭시킨다. 세트: 폭풍 소환사' },
    { id: 'fortress_sigil', name: '요새의 인장', slot: 'accessory', tier: 'epic',
      stats: { atk: 0, def: 15, hp: 60 }, passive: 'guardian', set: 'ironFortress', value: 265,
      desc: '동료를 지키는 인장. 세트: 철의 요새' },

    // legendary
    { id: 'leg_greed', name: '끝없는 탐욕 — 마몬의 눈', slot: 'accessory', tier: 'legendary',
      stats: { atk: 10, def: 10, hp: 50, critRate: 0.10 }, passive: 'goldFind', value: 580,
      lore: '골드가 모이는 곳에 이 보석이 있었다. 대가는... 아직 아무도 모른다.',
      desc: '유니크. 전투 보상 골드 +15% + 올스탯' }
];

// 경매 전용 아이템 생성 (등급 가중치 적용)
function generateAuctionItem(gs) {
    const guildLv = gs.guildLevel || 1;
    const pool = AUCTION_EQUIPMENT_POOL;

    // 등급 결정 (길드 레벨 기반 — BALANCE.RARITY_POOL 참조)
    const rarityTable = BALANCE.RARITY_POOL[guildLv] || BALANCE.RARITY_POOL[1];
    const roll = Math.random() * 100;
    let rarity;
    let acc = 0;
    for (const [r, w] of Object.entries(rarityTable)) {
        acc += w;
        if (roll < acc) { rarity = r; break; }
    }
    if (!rarity) rarity = 'common';

    // 등급 → tier 매핑
    const tierMap = { common: 'common', uncommon: 'uncommon', rare: 'rare', epic: 'epic', legendary: 'legendary' };
    const tier = tierMap[rarity];

    // 해당 tier 아이템 필터
    const candidates = pool.filter(item => item.tier === tier);
    if (!candidates.length) {
        // fallback: 아무거나
        const fallback = pool[Math.floor(Math.random() * pool.length)];
        return _instantiate(fallback, rarity);
    }

    const template = candidates[Math.floor(Math.random() * candidates.length)];
    return _instantiate(template, rarity);
}

function _instantiate(template, rarity) {
    const mult = ITEM_RARITY[rarity]?.valueMult || 1.0;
    const item = {
        id: _itemIdCounter++,
        templateId: template.id,
        type: 'equipment',
        rarity: rarity,
        tier: template.tier,
        name: template.name,
        slot: template.slot,
        desc: template.desc,
        lore: template.lore || null,
        stats: { ...template.stats },
        passive: template.passive || null,
        set: template.set || null,
        value: Math.round(template.value * (mult / ITEM_RARITY[template.tier].valueMult)),
        weight: 3,
        auctionExclusive: true
    };

    // 패시브 정보 인라인
    if (item.passive && AUCTION_PASSIVES[item.passive]) {
        item.passiveData = AUCTION_PASSIVES[item.passive];
    }

    // 세트 정보 인라인
    if (item.set && AUCTION_SETS[item.set]) {
        item.setData = {
            name: AUCTION_SETS[item.set].name,
            pieces: AUCTION_SETS[item.set].pieces,
            bonuses: AUCTION_SETS[item.set].bonuses
        };
    }

    return item;
}

// 세트 보너스 계산 (용병의 장비 목록 기준)
function getActiveSetBonuses(equipment) {
    const setCounts = {};
    for (const item of Object.values(equipment)) {
        if (item && item.set) {
            setCounts[item.set] = (setCounts[item.set] || 0) + 1;
        }
    }

    const active = [];
    for (const [setKey, count] of Object.entries(setCounts)) {
        const setDef = AUCTION_SETS[setKey];
        if (!setDef) continue;
        for (const [threshold, bonus] of Object.entries(setDef.bonuses)) {
            if (count >= parseInt(threshold)) {
                active.push({ set: setKey, setName: setDef.name, threshold: parseInt(threshold), ...bonus });
            }
        }
    }
    return active;
}

if (typeof module !== 'undefined' && module.exports) {
    module.exports = { AUCTION_PASSIVES, AUCTION_SETS, AUCTION_EQUIPMENT_POOL, generateAuctionItem, getActiveSetBonuses };
}
