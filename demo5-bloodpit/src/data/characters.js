const BP_ALLIES = {
    warrior: { name: '전사', hp: 450, atk: 22, def: 12, attackSpeed: 1800, range: 60, moveSpeed: 85, critRate: 0.08, critDmg: 1.5, color: 0x4488ff, role: 'tank', bleedChance: 0, dodgeRate: 0.05 },
    rogue: { name: '도적', hp: 160, atk: 35, def: 3, attackSpeed: 1000, range: 55, moveSpeed: 140, critRate: 0.25, critDmg: 2.2, color: 0xff4444, role: 'dps', bleedChance: 0.15, dodgeRate: 0.15 },
    priest: { name: '사제', hp: 200, atk: 12, def: 5, attackSpeed: 2500, range: 200, moveSpeed: 70, critRate: 0.05, critDmg: 1.5, color: 0x44ff88, role: 'healer', bleedChance: 0, dodgeRate: 0.05 },
    mage: { name: '마법사', hp: 140, atk: 50, def: 2, attackSpeed: 2600, range: 220, moveSpeed: 65, critRate: 0.12, critDmg: 1.8, color: 0xaa44ff, role: 'dps', bleedChance: 0, dodgeRate: 0.05 },
    ranger: { name: '궁수', hp: 170, atk: 30, def: 3, attackSpeed: 1600, range: 200, moveSpeed: 90, critRate: 0.18, critDmg: 2.0, color: 0x88cc44, role: 'dps', bleedChance: 0, dodgeRate: 0.10 },
    paladin: { name: '성기사', hp: 380, atk: 18, def: 10, attackSpeed: 2000, range: 60, moveSpeed: 75, critRate: 0.06, critDmg: 1.5, color: 0xffdd44, role: 'tank', bleedChance: 0, dodgeRate: 0.03 },
    assassin: { name: '암살자', hp: 120, atk: 45, def: 1, attackSpeed: 800, range: 55, moveSpeed: 160, critRate: 0.30, critDmg: 2.5, color: 0x8844aa, role: 'dps', bleedChance: 0.20, dodgeRate: 0.20 },
    shaman: { name: '주술사', hp: 220, atk: 15, def: 4, attackSpeed: 2200, range: 180, moveSpeed: 68, critRate: 0.08, critDmg: 1.5, color: 0x44aaaa, role: 'healer', bleedChance: 0, dodgeRate: 0.05 },

    // --- Advanced Classes (obtained via promotion only) ---
    berserker: { name: '광전사', hp: 520, atk: 40, def: 8, attackSpeed: 1500, range: 65, moveSpeed: 100, critRate: 0.15, critDmg: 1.8, color: 0x6699ff, role: 'tank', bleedChance: 0.10, dodgeRate: 0.08, lifesteal: 0.12 },
    blade_master: { name: '검성', hp: 210, atk: 48, def: 5, attackSpeed: 850, range: 60, moveSpeed: 150, critRate: 0.32, critDmg: 2.5, color: 0xff6666, role: 'dps', bleedChance: 0.20, dodgeRate: 0.18 },
    bishop: { name: '주교', hp: 260, atk: 18, def: 7, attackSpeed: 2200, range: 220, moveSpeed: 75, critRate: 0.08, critDmg: 1.5, color: 0x66ffaa, role: 'healer', bleedChance: 0, dodgeRate: 0.08 },
    archmage: { name: '대마법사', hp: 180, atk: 70, def: 3, attackSpeed: 2400, range: 240, moveSpeed: 70, critRate: 0.15, critDmg: 2.0, color: 0xcc66ff, role: 'dps', bleedChance: 0, dodgeRate: 0.08 },
    sniper: { name: '저격수', hp: 200, atk: 42, def: 4, attackSpeed: 1800, range: 280, moveSpeed: 85, critRate: 0.25, critDmg: 2.8, color: 0xaaee55, role: 'dps', bleedChance: 0, dodgeRate: 0.12 },
    crusader: { name: '십자군', hp: 480, atk: 25, def: 14, attackSpeed: 1800, range: 65, moveSpeed: 80, critRate: 0.10, critDmg: 1.6, color: 0xffee66, role: 'tank', bleedChance: 0, dodgeRate: 0.05 },
    phantom: { name: '환영', hp: 150, atk: 58, def: 2, attackSpeed: 700, range: 60, moveSpeed: 180, critRate: 0.38, critDmg: 3.0, color: 0xaa66cc, role: 'dps', bleedChance: 0.25, dodgeRate: 0.30 },
    witch_doctor: { name: '위치닥터', hp: 280, atk: 22, def: 6, attackSpeed: 2000, range: 200, moveSpeed: 72, critRate: 0.10, critDmg: 1.6, color: 0x66cccc, role: 'healer', bleedChance: 0.15, dodgeRate: 0.08 }
};

const BP_CLASS_POOL = ['warrior', 'rogue', 'priest', 'mage', 'ranger', 'paladin', 'assassin', 'shaman'];

const BP_PROMOTIONS = {
    warrior: { advancedClass: 'berserker', cost: 200, minLevel: 5, label: '전직: 광전사' },
    rogue: { advancedClass: 'blade_master', cost: 250, minLevel: 5, label: '전직: 검성' },
    priest: { advancedClass: 'bishop', cost: 250, minLevel: 5, label: '전직: 주교' },
    mage: { advancedClass: 'archmage', cost: 300, minLevel: 5, label: '전직: 대마법사' },
    ranger: { advancedClass: 'sniper', cost: 250, minLevel: 5, label: '전직: 저격수' },
    paladin: { advancedClass: 'crusader', cost: 300, minLevel: 5, label: '전직: 십자군' },
    assassin: { advancedClass: 'phantom', cost: 300, minLevel: 5, label: '전직: 환영' },
    shaman: { advancedClass: 'witch_doctor', cost: 250, minLevel: 5, label: '전직: 위치닥터' }
};

const BP_ADVANCED_CLASSES = Object.values(BP_PROMOTIONS).map(p => p.advancedClass);

const BP_NAME_POOL = {
    prefix: ['강철의', '피바람', '어둠의', '붉은', '그림자', '황금', '냉혈', '폭풍의', '고독한', '잔인한', '맹독의', '불꽃', '서리', '천둥', '대지의'],
    suffix: ['바르크', '세라', '칸', '리라', '도르', '미르', '제인', '곤', '아르', '텔라', '비스', '크론', '릭스', '오딘', '발라']
};

const BP_ENEMIES = {
    runner: { name: '러너', type: 'runner', hp: 100, atk: 12, def: 2, attackSpeed: 1400, range: 55, moveSpeed: 130, critRate: 0.05, critDmg: 1.5, color: 0x88aa44, bleedChance: 0, dodgeRate: 0.1 },
    bruiser: { name: '브루저', type: 'bruiser', hp: 300, atk: 18, def: 10, attackSpeed: 2200, range: 60, moveSpeed: 50, critRate: 0.05, critDmg: 1.5, color: 0x886622, bleedChance: 0, dodgeRate: 0 },
    spitter: { name: '스피터', type: 'spitter', hp: 80, atk: 22, def: 1, attackSpeed: 2000, range: 200, moveSpeed: 60, critRate: 0.08, critDmg: 1.5, color: 0x44aa88, bleedChance: 0.2, dodgeRate: 0.05 },
    nest: { name: '둥지', type: 'nest', hp: 250, atk: 0, def: 5, attackSpeed: 99999, range: 0, moveSpeed: 0, critRate: 0, critDmg: 1, color: 0x664444, bleedChance: 0, dodgeRate: 0, isNest: true }
};

const BP_ELITES = {
    elite_runner: { name: '엘리트 러너', type: 'elite_runner', hp: 350, atk: 28, def: 5, attackSpeed: 1200, range: 60, moveSpeed: 150, critRate: 0.15, critDmg: 2.0, color: 0xccff44, bleedChance: 0.1, dodgeRate: 0.2 },
    elite_bruiser: { name: '엘리트 브루저', type: 'elite_bruiser', hp: 800, atk: 35, def: 15, attackSpeed: 2000, range: 65, moveSpeed: 55, critRate: 0.1, critDmg: 1.8, color: 0xcc8822, bleedChance: 0, dodgeRate: 0 }
};

const BP_BOSS = {
    pitlord: { name: '핏로드', type: 'boss', hp: 2000, atk: 55, def: 15, attackSpeed: 1600, range: 80, moveSpeed: 70, critRate: 0.2, critDmg: 2.5, color: 0xff2244, bleedChance: 0.3, dodgeRate: 0.1 }
};
