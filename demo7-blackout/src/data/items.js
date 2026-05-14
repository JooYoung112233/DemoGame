const BO_ITEMS = [
    { id: 'circuit', name: '회로 기판', desc: '기본 전자부품', value: 30, weight: 2, rarity: 'common', icon: '🔌' },
    { id: 'chip', name: '데이터 칩', desc: '암호화된 데이터', value: 60, weight: 1, rarity: 'uncommon', icon: '💾' },
    { id: 'battery', name: '고용량 배터리', desc: '충전된 배터리', value: 45, weight: 4, rarity: 'common', icon: '🔋' },
    { id: 'medpack', name: '의료 팩', desc: '아군 HP 30% 회복', value: 40, weight: 3, rarity: 'common', icon: '💊', usable: true,
        use(allies, scene) { allies.forEach(a => { if (!a.alive) return; const h = Math.floor(a.maxHp * 0.3); a.hp = Math.min(a.maxHp, a.hp + h); }); } },
    { id: 'keycard', name: '보안 키카드', desc: '잠긴 방 접근 가능', value: 80, weight: 1, rarity: 'rare', icon: '🔑', special: true },
    { id: 'weapon_mod', name: '무기 개조 키트', desc: 'ATK +20%', value: 100, weight: 5, rarity: 'rare', icon: '🔧',
        apply(allies) { allies.forEach(a => a.atk = Math.floor(a.atk * 1.2)); } },
    { id: 'armor_plate', name: '방탄 플레이트', desc: 'DEF +10', value: 70, weight: 6, rarity: 'uncommon', icon: '🛡️',
        apply(allies) { allies.forEach(a => a.def += 10); } },
    { id: 'stim', name: '전투 자극제', desc: '공격속도 +15%', value: 55, weight: 2, rarity: 'uncommon', icon: '💉',
        apply(allies) { allies.forEach(a => a.attackSpeed = Math.floor(a.attackSpeed * 0.85)); } },
    { id: 'gold_bar', name: '금괴', desc: '높은 가치, 무거움', value: 200, weight: 10, rarity: 'epic', icon: '🥇' },
    { id: 'black_box', name: '블랙 박스', desc: '최고 가치 데이터', value: 300, weight: 8, rarity: 'legendary', icon: '📦' },
    { id: 'emp', name: 'EMP 수류탄', desc: '적 전체 30% HP 감소', value: 90, weight: 3, rarity: 'rare', icon: '💥', usable: true,
        use(enemies, scene) { enemies.forEach(e => { if (!e.alive) return; const dmg = Math.floor(e.maxHp * 0.3); e.takeDamage(dmg, null); }); } },
    { id: 'crit_lens', name: '정밀 조준경', desc: '크리티컬 +15%', value: 85, weight: 2, rarity: 'rare', icon: '🔭',
        apply(allies) { allies.forEach(a => a.critRate = Math.min(0.8, a.critRate + 0.15)); } },
    { id: 'nano_fiber', name: '나노 섬유', desc: 'HP +25%', value: 75, weight: 3, rarity: 'uncommon', icon: '🧬',
        apply(allies) { allies.forEach(a => { const b = Math.floor(a.maxHp * 0.25); a.maxHp += b; a.hp += b; }); } },
    { id: 'servo', name: '서보 모터', desc: '이동속도 +20%', value: 50, weight: 4, rarity: 'common', icon: '⚙️',
        apply(allies) { allies.forEach(a => a.moveSpeed = Math.floor(a.moveSpeed * 1.2)); } },
    { id: 'thermal_scope', name: '열감지 스코프', desc: '사거리 +60', value: 65, weight: 2, rarity: 'uncommon', icon: '🔴',
        apply(allies) { allies.forEach(a => a.range += 60); } },
    { id: 'jammer', name: '전파 교란기', desc: '적 명중률 -20%', value: 95, weight: 3, rarity: 'rare', icon: '📡',
        apply(allies) { allies.forEach(a => a.dodgeRate = Math.min(0.5, (a.dodgeRate || 0) + 0.20)); } },
    { id: 'c4', name: 'C4 폭약', desc: '적 전체 200 피해', value: 120, weight: 5, rarity: 'epic', icon: '🧨', usable: true,
        use(enemies, scene) { enemies.forEach(e => { if (!e.alive) return; e.takeDamage(200, null); }); } },
    { id: 'core_sample', name: '코어 샘플', desc: '희귀 연구 재료', value: 250, weight: 7, rarity: 'epic', icon: '🔮' },
    { id: 'scrambler', name: '스크램블러', desc: '적 공격력 -25%', value: 110, weight: 3, rarity: 'rare', icon: '🔇',
        apply(enemies) {} },
    { id: 'ration', name: '비상 식량', desc: '긴장도 -1', value: 20, weight: 2, rarity: 'common', icon: '🍫', tensionReduce: 1 }
];

const BO_RARITIES = {
    common:    { name: '일반', color: '#aaaaaa', borderColor: 0x888888 },
    uncommon:  { name: '고급', color: '#44ff88', borderColor: 0x44ff88 },
    rare:      { name: '희귀', color: '#4488ff', borderColor: 0x4488ff },
    epic:      { name: '에픽', color: '#aa44ff', borderColor: 0xaa44ff },
    legendary: { name: '전설', color: '#ffaa00', borderColor: 0xffaa00 }
};

function getItemByDepth(depth) {
    const r = Math.random();
    const epicChance = Math.min(0.02 + depth * 0.03, 0.15);
    const rareChance = Math.min(0.08 + depth * 0.04, 0.30);
    const uncommonChance = 0.25;

    let rarity;
    if (r < epicChance) rarity = depth > 3 ? 'legendary' : 'epic';
    else if (r < epicChance + rareChance) rarity = 'epic';
    else if (r < epicChance + rareChance + uncommonChance) rarity = 'rare';
    else rarity = Math.random() < 0.5 ? 'uncommon' : 'common';

    const pool = BO_ITEMS.filter(it => {
        if (rarity === 'legendary') return it.rarity === 'legendary' || it.rarity === 'epic';
        return true;
    });

    const item = { ...pool[Math.floor(Math.random() * pool.length)] };
    return item;
}
