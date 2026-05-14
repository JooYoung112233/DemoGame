const BP_RARITIES = {
    common:    { name: '일반', color: '#aaaaaa', borderColor: 0x888888 },
    uncommon:  { name: '고급', color: '#44ff88', borderColor: 0x44ff88 },
    rare:      { name: '희귀', color: '#4488ff', borderColor: 0x4488ff },
    epic:      { name: '에픽', color: '#aa44ff', borderColor: 0xaa44ff },
    legendary: { name: '전설', color: '#ffaa00', borderColor: 0xffaa00 }
};

const BP_RARITY_MULT = {
    common: 1.0, uncommon: 1.3, rare: 1.6, epic: 2.0, legendary: 2.5, ancient: 3.0, mythical: 4.0
};

function getRarityByDanger(danger) {
    const r = Math.random();
    const ancientChance = danger >= 15 ? Math.min(0.01 + (danger - 15) * 0.015, 0.08) : 0;
    const legendChance = Math.min(0.02 + danger * 0.02, 0.15);
    const epicChance = Math.min(0.05 + danger * 0.03, 0.25);
    const rareChance = Math.min(0.10 + danger * 0.04, 0.40);
    const uncommonChance = 0.30;

    if (r < ancientChance) return 'ancient';
    if (r < ancientChance + legendChance) return 'legendary';
    if (r < ancientChance + legendChance + epicChance) return 'epic';
    if (r < ancientChance + legendChance + epicChance + rareChance) return 'rare';
    if (r < ancientChance + legendChance + epicChance + rareChance + uncommonChance) return 'uncommon';
    return 'common';
}

const BP_WEAPONS = [
    { id: 'fist', name: '주먹', desc: '기본 무기', rarity: 'common', type: 'weapon', atkBonus: 0, spdBonus: 0 },
    { id: 'sword', name: '검', type: 'weapon', atkBonus: 15, spdBonus: 0.10 },
    { id: 'axe', name: '도끼', type: 'weapon', atkBonus: 30, spdBonus: -0.15 },
    { id: 'spear', name: '창', type: 'weapon', atkBonus: 10, spdBonus: 0, rangeBonus: 50 },
    { id: 'shotgun', name: '샷건', type: 'weapon', atkBonus: 40, spdBonus: -0.30, rangeBonus: 150 }
];

// Base passive definitions — values are base (common) tier
const BP_PASSIVE_DEFS = [
    { id: 'bleed_1', name: '출혈 강화', baseVal: 0.10, unit: '%', stat: 'bleedChance', descFn(v) { return `출혈 확률 +${Math.floor(v*100)}%`; }, applyFn(allies, v) { allies.forEach(a => a.bleedChance = (a.bleedChance||0) + v); }, tag: 'bleed' },
    { id: 'bleed_2', name: '출혈 폭발', baseVal: 0.50, unit: '%', stat: 'bleedExplodeChance', descFn(v) { return `출혈 사망 시 ${Math.floor(v*100)}% 폭발`; }, applyFn(allies, v) { allies.forEach(a => a.bleedExplodeChance = (a.bleedExplodeChance||0) + v); }, tag: 'bleed' },
    { id: 'bleed_3', name: '출혈 추가뎀', baseVal: 0.25, unit: '%', stat: 'bleedBonusDmg', descFn(v) { return `출혈 적 피해 +${Math.floor(v*100)}%`; }, applyFn(allies, v) { allies.forEach(a => a.bleedBonusDmg = (a.bleedBonusDmg||0) + v); }, tag: 'bleed' },
    { id: 'dodge_1', name: '회피 강화', baseVal: 0.10, unit: '%', stat: 'dodgeRate', descFn(v) { return `회피 확률 +${Math.floor(v*100)}%`; }, applyFn(allies, v) { allies.forEach(a => a.dodgeRate = Math.min(0.6, (a.dodgeRate||0) + v)); }, tag: 'dodge' },
    { id: 'dodge_2', name: '회피 반격', baseVal: 0.40, unit: '%', stat: 'dodgeCounterChance', descFn(v) { return `회피 시 ${Math.floor(v*100)}% 반격`; }, applyFn(allies, v) { allies.forEach(a => a.dodgeCounterChance = (a.dodgeCounterChance||0) + v); }, tag: 'dodge' },
    { id: 'dodge_3', name: '회피 공속', baseVal: 0.20, unit: '%', stat: 'dodgeSpdBuff', descFn(v) { return `회피 시 공속 ${Math.floor(v*100)}% 증가`; }, applyFn(allies, v) { allies.forEach(a => { a.dodgeSpdBuff = true; a.dodgeSpdBuffAmount = v; }); }, tag: 'dodge' },
    { id: 'corpse_1', name: '시체 폭발', baseVal: 0.30, unit: '%', stat: 'corpseExplodeChance', descFn(v) { return `적 사망 시 ${Math.floor(v*100)}% 주변 폭발`; }, applyFn(allies, v) { allies.forEach(a => a.corpseExplodeChance = (a.corpseExplodeChance||0) + v); }, tag: 'corpse' },
    { id: 'corpse_2', name: '시체 흡수', baseVal: 0.05, unit: '%', stat: 'corpseHeal', descFn(v) { return `적 사망 시 HP ${Math.floor(v*100)}% 회복`; }, applyFn(allies, v) { allies.forEach(a => a.corpseHeal = (a.corpseHeal||0) + v); }, tag: 'corpse' },
    { id: 'atk_up', name: '공격력 증가', baseVal: 0.20, unit: '%', descFn(v) { return `모든 아군 ATK +${Math.floor(v*100)}%`; }, applyFn(allies, v) { allies.forEach(a => a.atk = Math.floor(a.atk * (1 + v))); } },
    { id: 'spd_up', name: '공격속도 증가', baseVal: 0.15, unit: '%', descFn(v) { return `모든 아군 공속 +${Math.floor(v*100)}%`; }, applyFn(allies, v) { allies.forEach(a => a.attackSpeed = Math.floor(a.attackSpeed * (1 - v))); } },
    { id: 'hp_up', name: '최대체력 증가', baseVal: 0.25, unit: '%', descFn(v) { return `모든 아군 HP +${Math.floor(v*100)}%`; }, applyFn(allies, v) { allies.forEach(a => { const b = Math.floor(a.maxHp * v); a.maxHp += b; a.hp += b; }); } },
    { id: 'crit_up', name: '치명타율 증가', baseVal: 0.10, unit: '%', descFn(v) { return `치명타 확률 +${Math.floor(v*100)}%`; }, applyFn(allies, v) { allies.forEach(a => a.critRate = Math.min(0.8, a.critRate + v)); } },
    { id: 'crit_dmg', name: '치명타 피해 증가', baseVal: 0.50, unit: '%', descFn(v) { return `치명타 배율 +${Math.floor(v*100)}%`; }, applyFn(allies, v) { allies.forEach(a => a.critDmg += v); } },
    { id: 'lifesteal', name: '흡혈', baseVal: 0.10, unit: '%', descFn(v) { return `피해량의 ${Math.floor(v*100)}% HP 회복`; }, applyFn(allies, v) { allies.forEach(a => a.lifesteal = (a.lifesteal||0) + v); } },
    { id: 'thorns', name: '가시 갑옷', baseVal: 10, unit: '', descFn(v) { return `피격 시 ${Math.floor(v)} 반사 피해`; }, applyFn(allies, v) { allies.forEach(a => a.thorns = (a.thorns||0) + Math.floor(v)); } }
];

const BP_CONSUMABLES = [
    { id: 'bandage', name: '붕대', desc: '전체 HP 30% 즉시 회복', rarity: 'common', type: 'consumable', use(allies, scene) { allies.forEach(a => { if (!a.alive) return; const h = Math.floor(a.maxHp * 0.3); a.hp = Math.min(a.maxHp, a.hp + h); DamagePopup.show(scene, a.container.x, a.container.y - 30, h, 0x44ff88, true); }); } },
    { id: 'bomb', name: '폭탄', desc: '모든 적에게 150 피해', rarity: 'uncommon', type: 'consumable', use(enemies, scene) { enemies.forEach(e => { if (!e.alive) return; e.takeDamage(150, null); DamagePopup.show(scene, e.container.x, e.container.y - 20, 150, 0xff4444, false); }); } },
    { id: 'antidote', name: '해독제', desc: '모든 출혈/독 상태 해제 + DEF+5', rarity: 'common', type: 'consumable', use(allies, scene) { allies.forEach(a => { if (!a.alive) return; a.bleeding = false; a.def += 5; DamagePopup.show(scene, a.container.x, a.container.y - 30, 'DEF↑', 0x4488ff, true); }); } }
];

// Keep BP_PASSIVES for backward compatibility (DropSystem.collectedPassiveIds checks)
const BP_PASSIVES = BP_PASSIVE_DEFS;

function _scaleWeapon(base, rarity) {
    const m = BP_RARITY_MULT[rarity] || 1;
    const item = { ...base, rarity, type: 'weapon' };
    item.atkBonus = Math.floor(base.atkBonus * m);
    item.spdBonus = base.spdBonus < 0 ? base.spdBonus : +(base.spdBonus * m).toFixed(2);
    if (base.rangeBonus) item.rangeBonus = Math.floor(base.rangeBonus * m);
    const parts = [];
    if (item.atkBonus) parts.push(`ATK+${item.atkBonus}`);
    if (item.spdBonus > 0) parts.push(`공속+${Math.floor(item.spdBonus*100)}%`);
    if (item.spdBonus < 0) parts.push(`공속${Math.floor(item.spdBonus*100)}%`);
    if (item.rangeBonus) parts.push(`사거리+${item.rangeBonus}`);
    item.desc = parts.join(', ') || '기본 무기';
    return item;
}

function _scalePassive(def, rarity) {
    const m = BP_RARITY_MULT[rarity] || 1;
    const scaledVal = def.baseVal * m;
    const item = {
        id: def.id,
        name: def.name,
        rarity,
        type: 'passive',
        tag: def.tag,
        desc: def.descFn(scaledVal),
        _scaledVal: scaledVal,
        apply(allies) { def.applyFn(allies, scaledVal); }
    };
    return item;
}

function generateDrops(danger, count) {
    const drops = [];
    for (let i = 0; i < count; i++) {
        const rarity = getRarityByDanger(danger);
        const roll = Math.random();
        let item;
        if (roll < 0.30) {
            const pool = BP_WEAPONS.filter(w => w.id !== 'fist');
            const base = pool[Math.floor(Math.random() * pool.length)];
            item = _scaleWeapon(base, rarity);
        } else {
            const def = BP_PASSIVE_DEFS[Math.floor(Math.random() * BP_PASSIVE_DEFS.length)];
            item = _scalePassive(def, rarity);
        }
        drops.push(item);
    }
    return drops;
}
