const LC_MODULES = [
    { id: 'electric_fence', name: '전기 철조망', desc: '침입 적 70% 확률로 자동 처리', color: '#44ccff', autoDefenseRate: 0.70, icon: '⚡' },
    { id: 'auto_turret', name: '자동 터렛', desc: '침입 적 85% 확률로 자동 처리', color: '#ff8844', autoDefenseRate: 0.85, icon: '🔫' },
    { id: 'freeze_round', name: '냉각탄', desc: '적 공격속도 30% 감소', color: '#88ddff', autoDefenseRate: 0.50, spdDebuff: 0.30, icon: '❄️' },
    { id: 'overcharge', name: '과부하 발전기', desc: '아군 ATK +40%, 흡혈 8%', color: '#ffff44', autoDefenseRate: 0.40, atkBuff: 0.40, allyLifesteal: 0.08, icon: '⚡' },
    { id: 'barricade', name: '바리케이드', desc: '칸 HP 30% 추가, 가시 5, 방어율 60%', color: '#8888aa', autoDefenseRate: 0.60, hpBonus: 0.30, allyThorns: 5, icon: '🛡️' },
    { id: 'mine_field', name: '지뢰밭', desc: '침입 적에게 50 피해, 방어율 55%', color: '#ff4444', autoDefenseRate: 0.55, entryDamage: 50, icon: '💣' },
    { id: 'repair_bot', name: '수리 봇', desc: '매 라운드 칸 HP 10% 자동 회복', color: '#44ff88', autoDefenseRate: 0.30, autoRepair: 0.10, icon: '🔧' },
    { id: 'radar', name: '레이더', desc: '침입 예측 정확도 증가, 방어율 45%', color: '#44ffff', autoDefenseRate: 0.45, revealChance: 1.0, icon: '📡' },
    { id: 'flamethrower', name: '화염방사기', desc: '다수 적 처리, 방어율 75%', color: '#ff6622', autoDefenseRate: 0.75, icon: '🔥' },
    { id: 'shield_gen', name: '실드 발생기', desc: '칸 피해 40% 감소, 방어율 50%', color: '#4488ff', autoDefenseRate: 0.50, dmgReduction: 0.40, icon: '🛡️' }
];

function getRandomModules(count) {
    const shuffled = [...LC_MODULES].sort(() => Math.random() - 0.5);
    return shuffled.slice(0, count);
}
