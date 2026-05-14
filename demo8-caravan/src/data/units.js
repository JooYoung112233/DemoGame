const UNIT_DATA = {
    soldier: {
        name: '병사', tier: 1, role: 'melee', cost: 30,
        hp: 180, atk: 22, def: 8, attackSpeed: 1800, range: 55, moveSpeed: 90,
        color: 0x6688cc, critRate: 0.08, critDmg: 1.5,
        skills: [],
        advanceTo: ['knight', 'berserker']
    },
    knight: {
        name: '기사', tier: 2, role: 'tank', advanceCost: 40, baseClass: 'soldier',
        hp: 260, atk: 28, def: 14, attackSpeed: 1800, range: 55, moveSpeed: 85,
        color: 0x4488ff, critRate: 0.08, critDmg: 1.5,
        skills: ['shield_block'],
        advanceTo: ['paladin']
    },
    paladin: {
        name: '성기사', tier: 3, role: 'tank', advanceCost: 80, baseClass: 'soldier',
        hp: 350, atk: 35, def: 20, attackSpeed: 1800, range: 55, moveSpeed: 80,
        color: 0x44aaff, critRate: 0.10, critDmg: 1.5,
        skills: ['shield_block', 'holy_strike'],
        advanceTo: []
    },
    berserker: {
        name: '광전사', tier: 2, role: 'melee', advanceCost: 40, baseClass: 'soldier',
        hp: 220, atk: 38, def: 5, attackSpeed: 1400, range: 60, moveSpeed: 100,
        color: 0xcc4444, critRate: 0.15, critDmg: 1.8,
        skills: ['frenzy'],
        advanceTo: ['warlord']
    },
    warlord: {
        name: '군주', tier: 3, role: 'melee', advanceCost: 80, baseClass: 'soldier',
        hp: 280, atk: 50, def: 8, attackSpeed: 1200, range: 65, moveSpeed: 105,
        color: 0xff4444, critRate: 0.18, critDmg: 2.0,
        skills: ['frenzy', 'war_cry'],
        advanceTo: []
    },

    archer: {
        name: '궁수', tier: 1, role: 'ranged', cost: 35,
        hp: 120, atk: 28, def: 3, attackSpeed: 2000, range: 200, moveSpeed: 70,
        color: 0x88cc44, critRate: 0.12, critDmg: 1.5,
        skills: [],
        advanceTo: ['sniper', 'flame_archer']
    },
    sniper: {
        name: '저격수', tier: 2, role: 'ranged', advanceCost: 40, baseClass: 'archer',
        hp: 140, atk: 38, def: 4, attackSpeed: 2500, range: 250, moveSpeed: 65,
        color: 0x66aa22, critRate: 0.20, critDmg: 2.0,
        skills: ['precise_shot'],
        advanceTo: ['marksman']
    },
    marksman: {
        name: '명사수', tier: 3, role: 'ranged', advanceCost: 80, baseClass: 'archer',
        hp: 160, atk: 50, def: 5, attackSpeed: 2800, range: 280, moveSpeed: 60,
        color: 0x44cc00, critRate: 0.25, critDmg: 2.2,
        skills: ['precise_shot', 'pierce'],
        advanceTo: []
    },
    flame_archer: {
        name: '화염궁수', tier: 2, role: 'ranged', advanceCost: 40, baseClass: 'archer',
        hp: 130, atk: 32, def: 3, attackSpeed: 1800, range: 200, moveSpeed: 70,
        color: 0xff8844, critRate: 0.12, critDmg: 1.5,
        skills: ['fire_arrow'],
        advanceTo: ['pyromancer']
    },
    pyromancer: {
        name: '화염술사', tier: 3, role: 'ranged', advanceCost: 80, baseClass: 'archer',
        hp: 150, atk: 42, def: 4, attackSpeed: 1800, range: 220, moveSpeed: 65,
        color: 0xff4400, critRate: 0.15, critDmg: 1.8,
        skills: ['fire_arrow', 'firestorm'],
        advanceTo: []
    },

    cleric: {
        name: '성직자', tier: 1, role: 'healer', cost: 40,
        hp: 140, atk: 10, def: 5, attackSpeed: 2500, range: 180, moveSpeed: 65,
        color: 0x44ddaa, critRate: 0.05, critDmg: 1.5,
        skills: [],
        advanceTo: ['priest', 'battle_cleric']
    },
    priest: {
        name: '사제', tier: 2, role: 'healer', advanceCost: 40, baseClass: 'cleric',
        hp: 180, atk: 12, def: 7, attackSpeed: 2500, range: 200, moveSpeed: 60,
        color: 0x44ffaa, critRate: 0.05, critDmg: 1.5,
        skills: ['heal'],
        advanceTo: ['high_priest']
    },
    high_priest: {
        name: '대사제', tier: 3, role: 'healer', advanceCost: 80, baseClass: 'cleric',
        hp: 220, atk: 15, def: 10, attackSpeed: 2500, range: 220, moveSpeed: 55,
        color: 0x88ffcc, critRate: 0.05, critDmg: 1.5,
        skills: ['heal', 'grand_heal'],
        advanceTo: []
    },
    battle_cleric: {
        name: '전투사제', tier: 2, role: 'melee', advanceCost: 40, baseClass: 'cleric',
        hp: 200, atk: 22, def: 10, attackSpeed: 2000, range: 80, moveSpeed: 75,
        color: 0xaadd44, critRate: 0.10, critDmg: 1.5,
        skills: ['judgment'],
        advanceTo: ['inquisitor']
    },
    inquisitor: {
        name: '심판관', tier: 3, role: 'melee', advanceCost: 80, baseClass: 'cleric',
        hp: 260, atk: 30, def: 14, attackSpeed: 1800, range: 80, moveSpeed: 80,
        color: 0xddff44, critRate: 0.12, critDmg: 1.8,
        skills: ['judgment', 'divine_justice'],
        advanceTo: []
    },

    thief: {
        name: '도둑', tier: 1, role: 'melee', cost: 35,
        hp: 100, atk: 32, def: 2, attackSpeed: 1200, range: 55, moveSpeed: 120,
        color: 0xcc44cc, critRate: 0.20, critDmg: 1.8,
        skills: [],
        advanceTo: ['assassin', 'ranger']
    },
    assassin: {
        name: '암살자', tier: 2, role: 'melee', advanceCost: 40, baseClass: 'thief',
        hp: 110, atk: 45, def: 3, attackSpeed: 1000, range: 55, moveSpeed: 130,
        color: 0x8822aa, critRate: 0.25, critDmg: 2.0,
        skills: ['vital_strike'],
        advanceTo: ['reaper']
    },
    reaper: {
        name: '사신', tier: 3, role: 'melee', advanceCost: 80, baseClass: 'thief',
        hp: 130, atk: 60, def: 4, attackSpeed: 900, range: 60, moveSpeed: 135,
        color: 0x6600cc, critRate: 0.30, critDmg: 2.5,
        skills: ['vital_strike', 'death_blow'],
        advanceTo: []
    },
    ranger: {
        name: '유격병', tier: 2, role: 'ranged', advanceCost: 40, baseClass: 'thief',
        hp: 140, atk: 35, def: 6, attackSpeed: 1300, range: 120, moveSpeed: 110,
        color: 0xaa66cc, critRate: 0.15, critDmg: 1.8,
        skills: ['poison_arrow'],
        advanceTo: ['outlaw']
    },
    outlaw: {
        name: '의적', tier: 3, role: 'ranged', advanceCost: 80, baseClass: 'thief',
        hp: 170, atk: 42, def: 8, attackSpeed: 1300, range: 140, moveSpeed: 115,
        color: 0xcc88ee, critRate: 0.18, critDmg: 2.0,
        skills: ['poison_arrow', 'plunder'],
        advanceTo: []
    }
};

const BASE_CLASSES = ['soldier', 'archer', 'cleric', 'thief'];
