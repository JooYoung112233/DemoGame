const WAVE_CHARS = {
    tank: { name: '탱커', hp: 500, atk: 18, def: 14, attackSpeed: 2000, range: 60, moveSpeed: 80, critRate: 0.05, critDmg: 1.5, color: 0x4488ff, role: 'tank' },
    rogue: { name: '도적', hp: 150, atk: 32, def: 3, attackSpeed: 1100, range: 55, moveSpeed: 130, critRate: 0.22, critDmg: 2.0, color: 0xff4444, role: 'dps' },
    priest: { name: '사제', hp: 180, atk: 10, def: 5, attackSpeed: 2500, range: 200, moveSpeed: 70, critRate: 0.05, critDmg: 1.5, color: 0x44ff88, role: 'healer' },
    mage: { name: '마법사', hp: 130, atk: 48, def: 2, attackSpeed: 2800, range: 220, moveSpeed: 65, critRate: 0.1, critDmg: 1.8, color: 0xaa44ff, role: 'dps' }
};

const WAVE_ENEMIES = {
    melee: { name: '고블린', hp: 120, atk: 14, def: 3, attackSpeed: 1600, range: 55, moveSpeed: 100, critRate: 0.05, critDmg: 1.5, color: 0x88aa44 },
    ranged: { name: '아처', hp: 80, atk: 18, def: 1, attackSpeed: 2000, range: 200, moveSpeed: 70, critRate: 0.1, critDmg: 1.5, color: 0xcc8844 },
    tank: { name: '오거', hp: 350, atk: 20, def: 8, attackSpeed: 2400, range: 60, moveSpeed: 50, critRate: 0.05, critDmg: 1.5, color: 0x886622 },
    boss: { name: '드래곤', hp: 1200, atk: 40, def: 12, attackSpeed: 1800, range: 80, moveSpeed: 60, critRate: 0.15, critDmg: 2.0, color: 0xff2244 }
};
