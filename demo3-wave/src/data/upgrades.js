const UPGRADES = [
    { id: 'atk_up', name: '공격력 +20%', desc: '모든 아군의 공격력 증가', color: '#ff6644', apply(allies) { allies.forEach(a => a.atk = Math.floor(a.atk * 1.2)); } },
    { id: 'spd_up', name: '공격속도 +15%', desc: '모든 아군의 공격속도 증가', color: '#ffcc44', apply(allies) { allies.forEach(a => a.attackSpeed = Math.floor(a.attackSpeed * 0.85)); } },
    { id: 'hp_up', name: '최대체력 +25%', desc: '모든 아군의 최대체력 증가 및 회복', color: '#44ff88', apply(allies) { allies.forEach(a => { const bonus = Math.floor(a.maxHp * 0.25); a.maxHp += bonus; a.hp += bonus; }); } },
    { id: 'crit_up', name: '치명타율 +10%', desc: '모든 아군의 치명타 확률 증가', color: '#ffff44', apply(allies) { allies.forEach(a => a.critRate = Math.min(0.8, a.critRate + 0.1)); } },
    { id: 'crit_dmg', name: '치명타 피해 +50%', desc: '치명타 시 추가 피해', color: '#ff8844', apply(allies) { allies.forEach(a => a.critDmg += 0.5); } },
    { id: 'def_up', name: '방어력 +30%', desc: '모든 아군의 방어력 증가', color: '#4488ff', apply(allies) { allies.forEach(a => a.def = Math.floor(a.def * 1.3) + 2); } },
    { id: 'heal_all', name: '전체 회복 50%', desc: '모든 아군의 체력 50% 회복', color: '#88ffaa', apply(allies) { allies.forEach(a => { a.hp = Math.min(a.maxHp, a.hp + Math.floor(a.maxHp * 0.5)); }); } },
    { id: 'glass_cannon', name: '유리대포', desc: '체력 -30% 대신 공격력 +60%', color: '#ff4488', apply(allies) { allies.forEach(a => { a.maxHp = Math.floor(a.maxHp * 0.7); a.hp = Math.min(a.hp, a.maxHp); a.atk = Math.floor(a.atk * 1.6); }); } },
    { id: 'thorns', name: '가시 갑옷', desc: '피격 시 공격자에게 피해 반사 8', color: '#aa88ff', apply(allies) { allies.forEach(a => a.thorns = (a.thorns || 0) + 8); } },
    { id: 'lifesteal', name: '흡혈 10%', desc: '공격 시 피해량의 10% 회복', color: '#ff44aa', apply(allies) { allies.forEach(a => a.lifesteal = (a.lifesteal || 0) + 0.1); } },
    { id: 'move_up', name: '이동속도 +30%', desc: '모든 아군 이동속도 증가', color: '#44ffff', apply(allies) { allies.forEach(a => a.moveSpeed = Math.floor(a.moveSpeed * 1.3)); } },
    { id: 'death_explode', name: '사망 시 폭발', desc: '아군 사망 시 주변 적에게 100 피해', color: '#ff2222', apply(allies) { allies.forEach(a => a.deathExplosion = (a.deathExplosion || 0) + 100); } }
];

function getRandomUpgrades(count) {
    const shuffled = [...UPGRADES].sort(() => Math.random() - 0.5);
    return shuffled.slice(0, count);
}
