const ITEM_RARITY = {
    common:   { name: '일반', color: 0x888888, textColor: '#888888', valueMult: 1.0 },
    uncommon: { name: '고급', color: 0x44cc44, textColor: '#44cc44', valueMult: 1.5 },
    rare:     { name: '희귀', color: 0x4488ff, textColor: '#4488ff', valueMult: 2.5 },
    epic:     { name: '에픽', color: 0xcc44ff, textColor: '#cc44ff', valueMult: 5.0 },
    legendary:{ name: '전설', color: 0xffaa00, textColor: '#ffaa00', valueMult: 10.0 }
};

const EQUIPMENT_TEMPLATES = {
    weapon: [
        { name: '낡은 검',      slot: 'weapon', baseAtk: 5,  baseDef: 0, baseHp: 0 },
        { name: '강철 검',      slot: 'weapon', baseAtk: 10, baseDef: 0, baseHp: 0 },
        { name: '마법 지팡이',  slot: 'weapon', baseAtk: 12, baseDef: 0, baseHp: 0 },
        { name: '사냥꾼의 활',  slot: 'weapon', baseAtk: 8,  baseDef: 0, baseHp: 0 },
        { name: '의식용 단검',  slot: 'weapon', baseAtk: 15, baseDef: 0, baseHp: 0 }
    ],
    armor: [
        { name: '가죽 갑옷',    slot: 'armor', baseAtk: 0, baseDef: 5,  baseHp: 20 },
        { name: '사슬 갑옷',    slot: 'armor', baseAtk: 0, baseDef: 10, baseHp: 40 },
        { name: '마법사 로브',  slot: 'armor', baseAtk: 3, baseDef: 3,  baseHp: 15 },
        { name: '판금 갑옷',    slot: 'armor', baseAtk: 0, baseDef: 15, baseHp: 60 }
    ],
    accessory: [
        { name: '행운의 반지',  slot: 'accessory', baseAtk: 0,  baseDef: 0, baseHp: 0,  critRate: 0.05 },
        { name: '힘의 목걸이',  slot: 'accessory', baseAtk: 5,  baseDef: 0, baseHp: 0 },
        { name: '수호의 부적',  slot: 'accessory', baseAtk: 0,  baseDef: 5, baseHp: 30 },
        { name: '속도의 장갑',  slot: 'accessory', baseAtk: 0,  baseDef: 0, baseHp: 0, moveSpeed: 15 }
    ]
};

const MATERIAL_TEMPLATES = [
    { name: '혈정석',      zone: 'bloodpit',  baseValue: 50,  desc: '흡혈 장비 제작에 사용' },
    { name: '마법 동력석', zone: 'cargo',     baseValue: 50,  desc: '원거리/기술 장비 제작에 사용' },
    { name: '저주 유물',   zone: 'blackout',  baseValue: 50,  desc: '고위험 장비 제작에 사용' },
    { name: '황금 모래',   zone: 'common',    baseValue: 30,  desc: '기본 제작 소재' },
    { name: '철광석',      zone: 'common',    baseValue: 20,  desc: '기본 제작 소재' },
    { name: '마법 가루',   zone: 'common',    baseValue: 25,  desc: '마법 부여 소재' }
];

const CONSUMABLE_TEMPLATES = [
    { name: 'HP 포션',     effect: 'heal',   value: 30, desc: 'HP 30% 회복', baseValue: 20 },
    { name: '전투 물약',   effect: 'atkBuff', value: 20, desc: 'ATK +20% (1전투)', baseValue: 35 },
    { name: '방어 물약',   effect: 'defBuff', value: 20, desc: 'DEF +20% (1전투)', baseValue: 35 },
    { name: '화염 폭탄',   effect: 'aoe',    value: 50, desc: '적 전체 50 피해', baseValue: 40 }
];

const CURSED_EQUIPMENT = [
    {
        name: '저주받은 대검', slot: 'weapon', rarity: 'epic',
        stats: { atk: 35, def: -5 }, debuff: 'bleed_self',
        debuffDesc: '매 전투 시작 시 HP 5% 손실', desc: '어둠에 물든 검 — 해제 불가'
    },
    {
        name: '원혼의 갑옷', slot: 'armor', rarity: 'epic',
        stats: { def: 25, hp: 80 }, debuff: 'slow',
        debuffDesc: '이동속도 -20%', desc: '영혼이 깃든 갑옷 — 해제 불가'
    },
    {
        name: '탐욕의 반지', slot: 'accessory', rarity: 'epic',
        stats: { atk: 15, critRate: 0.10 }, debuff: 'gold_drain',
        debuffDesc: '전투마다 50G 소모', desc: '끝없는 탐욕 — 해제 불가'
    },
    {
        name: '광기의 투구', slot: 'armor', rarity: 'legendary',
        stats: { atk: 20, def: 30, hp: 50 }, debuff: 'berserk',
        debuffDesc: 'HP 30% 이하 시 아군 공격', desc: '광기의 무장 — 해제 불가'
    },
    {
        name: '피의 단검', slot: 'weapon', rarity: 'rare',
        stats: { atk: 20, critRate: 0.08 }, debuff: 'lifedrain',
        debuffDesc: '매 공격 시 자기 HP 2% 손실', desc: '피를 갈구하는 단검 — 해제 불가'
    },
    {
        name: '그림자 목걸이', slot: 'accessory', rarity: 'rare',
        stats: { moveSpeed: 30, atk: 10 }, debuff: 'fragile',
        debuffDesc: '받는 피해 +15%', desc: '그림자와 거래한 대가 — 해제 불가'
    }
];

function generateCursedItem() {
    const template = CURSED_EQUIPMENT[Math.floor(Math.random() * CURSED_EQUIPMENT.length)];
    return {
        id: _itemIdCounter++,
        type: 'equipment',
        rarity: template.rarity,
        name: template.name,
        slot: template.slot,
        desc: template.desc,
        stats: { ...template.stats },
        cursed: true,
        curseDebuff: template.debuff,
        curseDebuffDesc: template.debuffDesc,
        value: Math.floor(ITEM_RARITY[template.rarity].valueMult * 80),
        weight: 3
    };
}

let _itemIdCounter = 1;

function generateItem(zone, guildLevel, rarityBonus) {
    rarityBonus = rarityBonus || 0;
    const roll = Math.random();
    let type, template;

    // 타입 분포: 장비 25%, 소재 35%, 소비 40%
    if (roll < 0.25) {
        type = 'equipment';
        const slots = ['weapon', 'armor', 'accessory'];
        const slot = slots[Math.floor(Math.random() * slots.length)];
        const pool = EQUIPMENT_TEMPLATES[slot];
        template = { ...pool[Math.floor(Math.random() * pool.length)] };
    } else if (roll < 0.60) {
        type = 'material';
        const pool = zone !== 'common'
            ? MATERIAL_TEMPLATES.filter(m => m.zone === zone || m.zone === 'common')
            : MATERIAL_TEMPLATES;
        template = { ...pool[Math.floor(Math.random() * pool.length)] };
    } else {
        type = 'consumable';
        template = { ...CONSUMABLE_TEMPLATES[Math.floor(Math.random() * CONSUMABLE_TEMPLATES.length)] };
    }

    // 희귀도 분포 — common 위주, 친화도/길드Lv/보스로만 상위 등급
    // 기본 (보너스 0, Lv1): common 75%, uncommon 22%, rare 3%, epic 0%
    // 후반 (Lv8, 보너스 3): common 25%, uncommon 40%, rare 25%, epic 10%
    const rarityRoll = Math.random() * 100;
    const lvBonus = (guildLevel - 1) * 4;
    const bonusPts = rarityBonus * 6;
    const totalBonus = lvBonus + bonusPts;
    let rarity;
    if (rarityRoll < Math.min(15, 0 + totalBonus * 0.4))           rarity = 'epic';
    else if (rarityRoll < Math.min(30, 3 + totalBonus * 0.7))       rarity = 'rare';
    else if (rarityRoll < Math.min(50, 22 + totalBonus * 0.8))      rarity = 'uncommon';
    else                                                              rarity = 'common';

    const mult = ITEM_RARITY[rarity].valueMult;
    const item = {
        id: _itemIdCounter++,
        type,
        rarity,
        name: template.name,
        slot: template.slot || null,
        desc: template.desc || '',
        value: Math.floor((template.baseValue || 30) * mult),
        weight: type === 'equipment' ? 3 : type === 'material' ? 1 : 2
    };

    if (type === 'equipment') {
        item.stats = {};
        if (template.baseAtk) item.stats.atk = Math.floor(template.baseAtk * mult);
        if (template.baseDef) item.stats.def = Math.floor(template.baseDef * mult);
        if (template.baseHp)  item.stats.hp = Math.floor(template.baseHp * mult);
        if (template.critRate) item.stats.critRate = template.critRate * mult;
        if (template.moveSpeed) item.stats.moveSpeed = Math.floor(template.moveSpeed * mult);
    }

    return item;
}

// ── Market Trend System ──────────────────────────────────────
const MARKET_CATEGORIES = ['equipment', 'material', 'consumable'];

function initMarketTrends(gs) {
    if (gs.marketTrends) return;
    gs.marketTrends = {
        equipment: { modifier: 1.0, trend: 0, supply: 'normal' },
        material:  { modifier: 1.0, trend: 0, supply: 'normal' },
        consumable:{ modifier: 1.0, trend: 0, supply: 'normal' }
    };
    gs.marketTrendTick = 0;
}

function updateMarketTrends(gs, event) {
    initMarketTrends(gs);
    const trends = gs.marketTrends;

    switch (event) {
        case 'run_success':
            trends.material.modifier = Math.max(0.6, trends.material.modifier - 0.05);
            trends.equipment.modifier = Math.min(1.5, trends.equipment.modifier + 0.03);
            break;
        case 'run_fail':
            trends.consumable.modifier = Math.min(1.5, trends.consumable.modifier + 0.08);
            trends.equipment.modifier = Math.max(0.6, trends.equipment.modifier - 0.03);
            break;
        case 'bulk_sell':
            trends.material.modifier = Math.max(0.6, trends.material.modifier - 0.08);
            break;
        case 'bulk_buy':
            trends.equipment.modifier = Math.min(1.5, trends.equipment.modifier + 0.05);
            break;
    }

    gs.marketTrendTick = (gs.marketTrendTick || 0) + 1;
    if (gs.marketTrendTick % 3 === 0) {
        for (const cat of MARKET_CATEGORIES) {
            const drift = (Math.random() - 0.5) * 0.1;
            trends[cat].modifier = Math.max(0.5, Math.min(1.6, trends[cat].modifier + drift));
            trends[cat].modifier = Math.round(trends[cat].modifier * 100) / 100;

            if (trends[cat].modifier <= 0.7) trends[cat].supply = 'surplus';
            else if (trends[cat].modifier >= 1.3) trends[cat].supply = 'shortage';
            else trends[cat].supply = 'normal';

            trends[cat].trend = drift > 0.02 ? 1 : drift < -0.02 ? -1 : 0;
        }
    }
}

function getMarketPriceModifier(gs, itemType) {
    initMarketTrends(gs);
    return gs.marketTrends[itemType]?.modifier || 1.0;
}
