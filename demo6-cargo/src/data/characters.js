const LC_ALLIES = {
    guard: { name: '경비원', hp: 400, atk: 20, def: 10, attackSpeed: 1800, range: 60, moveSpeed: 80, critRate: 0.08, critDmg: 1.5, color: 0x4488ff, role: 'tank' },
    gunner: { name: '사수', hp: 160, atk: 35, def: 3, attackSpeed: 1200, range: 200, moveSpeed: 70, critRate: 0.15, critDmg: 2.0, color: 0xff6644, role: 'dps' },
    medic: { name: '의무병', hp: 200, atk: 10, def: 5, attackSpeed: 2500, range: 180, moveSpeed: 65, critRate: 0.05, critDmg: 1.5, color: 0x44ff88, role: 'healer' },
    engineer: { name: '기관사', hp: 180, atk: 15, def: 8, attackSpeed: 2000, range: 100, moveSpeed: 75, critRate: 0.10, critDmg: 1.8, color: 0xffcc44, role: 'support' }
};

const LC_ENEMIES = {
    crawler: { name: '크롤러', type: 'crawler', hp: 90, atk: 14, def: 2, attackSpeed: 1400, range: 55, moveSpeed: 120, critRate: 0.05, critDmg: 1.5, color: 0x88aa44, desc: '빠른 접근' },
    tanker: { name: '탱커', type: 'tanker', hp: 350, atk: 22, def: 12, attackSpeed: 2400, range: 60, moveSpeed: 45, critRate: 0.05, critDmg: 1.5, color: 0x886622, desc: '문 파괴' },
    jumper: { name: '점퍼', type: 'jumper', hp: 120, atk: 18, def: 3, attackSpeed: 1600, range: 55, moveSpeed: 100, critRate: 0.12, critDmg: 1.8, color: 0xaa44aa, desc: '뒷칸 침입' },
    explosive: { name: '폭발체', type: 'explosive', hp: 60, atk: 80, def: 0, attackSpeed: 3000, range: 40, moveSpeed: 90, critRate: 0, critDmg: 1, color: 0xff4422, desc: '자폭 공격' }
};
