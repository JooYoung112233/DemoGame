const BP_RARITIES = {
    common:    { name: '일반', color: '#aaaaaa', borderColor: 0x888888 },
    uncommon:  { name: '고급', color: '#44ff88', borderColor: 0x44ff88 },
    rare:      { name: '희귀', color: '#4488ff', borderColor: 0x4488ff },
    epic:      { name: '에픽', color: '#aa44ff', borderColor: 0xaa44ff },
    legendary: { name: '전설', color: '#ffaa00', borderColor: 0xffaa00 }
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
    { id: 'sword', name: '검', desc: 'ATK+15, 공속+10%', rarity: 'uncommon', type: 'weapon', atkBonus: 15, spdBonus: 0.10 },
    { id: 'axe', name: '도끼', desc: 'ATK+30, 공속-15%', rarity: 'rare', type: 'weapon', atkBonus: 30, spdBonus: -0.15 },
    { id: 'spear', name: '창', desc: 'ATK+10, 사거리+50', rarity: 'uncommon', type: 'weapon', atkBonus: 10, spdBonus: 0, rangeBonus: 50 },
    { id: 'shotgun', name: '샷건', desc: 'ATK+40, 공속-30%, 원거리', rarity: 'epic', type: 'weapon', atkBonus: 40, spdBonus: -0.30, rangeBonus: 150 }
];

const BP_PASSIVES = [
    { id: 'bleed_1', name: '출혈 강화', desc: '출혈 확률 +10%', rarity: 'common', type: 'passive', apply(allies) { allies.forEach(a => a.bleedChance = (a.bleedChance || 0) + 0.10); }, tag: 'bleed' },
    { id: 'bleed_2', name: '출혈 폭발', desc: '출혈 중 사망 시 50% 확률로 폭발', rarity: 'rare', type: 'passive', apply(allies) { allies.forEach(a => a.bleedExplodeChance = (a.bleedExplodeChance || 0) + 0.50); }, tag: 'bleed' },
    { id: 'bleed_3', name: '출혈 추가뎀', desc: '출혈 적에게 피해 +25%', rarity: 'uncommon', type: 'passive', apply(allies) { allies.forEach(a => a.bleedBonusDmg = (a.bleedBonusDmg || 0) + 0.25); }, tag: 'bleed' },
    { id: 'dodge_1', name: '회피 강화', desc: '회피 확률 +10%', rarity: 'common', type: 'passive', apply(allies) { allies.forEach(a => a.dodgeRate = Math.min(0.6, (a.dodgeRate || 0) + 0.10)); }, tag: 'dodge' },
    { id: 'dodge_2', name: '회피 반격', desc: '회피 성공 시 40% 확률로 반격', rarity: 'rare', type: 'passive', apply(allies) { allies.forEach(a => a.dodgeCounterChance = (a.dodgeCounterChance || 0) + 0.40); }, tag: 'dodge' },
    { id: 'dodge_3', name: '회피 공속', desc: '회피 성공 시 공속 20% 증가 (3초)', rarity: 'uncommon', type: 'passive', apply(allies) { allies.forEach(a => a.dodgeSpdBuff = true); }, tag: 'dodge' },
    { id: 'corpse_1', name: '시체 폭발', desc: '적 사망 시 30% 확률로 주변 폭발', rarity: 'rare', type: 'passive', apply(allies) { allies.forEach(a => a.corpseExplodeChance = (a.corpseExplodeChance || 0) + 0.30); }, tag: 'corpse' },
    { id: 'corpse_2', name: '시체 흡수', desc: '적 사망 시 HP 5% 회복', rarity: 'uncommon', type: 'passive', apply(allies) { allies.forEach(a => a.corpseHeal = (a.corpseHeal || 0) + 0.05); }, tag: 'corpse' },
    { id: 'atk_up', name: '공격력 +20%', desc: '모든 아군 ATK 증가', rarity: 'common', type: 'passive', apply(allies) { allies.forEach(a => a.atk = Math.floor(a.atk * 1.2)); } },
    { id: 'spd_up', name: '공격속도 +15%', desc: '모든 아군 공격속도 증가', rarity: 'common', type: 'passive', apply(allies) { allies.forEach(a => a.attackSpeed = Math.floor(a.attackSpeed * 0.85)); } },
    { id: 'hp_up', name: '최대체력 +25%', desc: '모든 아군 HP 증가 및 회복', rarity: 'common', type: 'passive', apply(allies) { allies.forEach(a => { const b = Math.floor(a.maxHp * 0.25); a.maxHp += b; a.hp += b; }); } },
    { id: 'crit_up', name: '치명타율 +10%', desc: '치명타 확률 증가', rarity: 'uncommon', type: 'passive', apply(allies) { allies.forEach(a => a.critRate = Math.min(0.8, a.critRate + 0.1)); } },
    { id: 'crit_dmg', name: '치명타 피해 +50%', desc: '치명타 배율 증가', rarity: 'uncommon', type: 'passive', apply(allies) { allies.forEach(a => a.critDmg += 0.5); } },
    { id: 'lifesteal', name: '흡혈 10%', desc: '피해량의 10% HP 회복', rarity: 'rare', type: 'passive', apply(allies) { allies.forEach(a => a.lifesteal = (a.lifesteal || 0) + 0.1); } },
    { id: 'thorns', name: '가시 갑옷', desc: '피격 시 10 반사 피해', rarity: 'uncommon', type: 'passive', apply(allies) { allies.forEach(a => a.thorns = (a.thorns || 0) + 10); } }
];

const BP_CONSUMABLES = [
    { id: 'bandage', name: '붕대', desc: '전체 HP 30% 즉시 회복', rarity: 'common', type: 'consumable', use(allies, scene) { allies.forEach(a => { if (!a.alive) return; const h = Math.floor(a.maxHp * 0.3); a.hp = Math.min(a.maxHp, a.hp + h); DamagePopup.show(scene, a.container.x, a.container.y - 30, h, 0x44ff88, true); }); } },
    { id: 'bomb', name: '폭탄', desc: '모든 적에게 150 피해', rarity: 'uncommon', type: 'consumable', use(enemies, scene) { enemies.forEach(e => { if (!e.alive) return; e.takeDamage(150, null); DamagePopup.show(scene, e.container.x, e.container.y - 20, 150, 0xff4444, false); }); } },
    { id: 'antidote', name: '해독제', desc: '모든 출혈/독 상태 해제 + DEF+5', rarity: 'common', type: 'consumable', use(allies, scene) { allies.forEach(a => { if (!a.alive) return; a.bleeding = false; a.def += 5; DamagePopup.show(scene, a.container.x, a.container.y - 30, 'DEF↑', 0x4488ff, true); }); } }
];

function generateDrops(danger, count) {
    const drops = [];
    for (let i = 0; i < count; i++) {
        const rarity = getRarityByDanger(danger);
        const roll = Math.random();
        let pool;
        if (roll < 0.30) {
            pool = BP_WEAPONS.filter(w => w.id !== 'fist');
        } else {
            pool = BP_PASSIVES;
        }
        const item = { ...pool[Math.floor(Math.random() * pool.length)] };
        item.rarity = rarity;
        drops.push(item);
    }
    return drops;
}
