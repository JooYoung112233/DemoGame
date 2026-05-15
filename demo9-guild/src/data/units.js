const CLASS_DATA = {
    warrior: {
        name: '전사', role: 'tank', icon: '🛡',
        baseHp: 250, baseAtk: 20, baseDef: 18,
        attackSpeed: 1800, range: 55, moveSpeed: 90,
        critRate: 0.05, critDmg: 1.5,
        color: 0x4488ff,
        growthHp: 30, growthAtk: 2, growthDef: 2,
        skillName: '방패막이', skillDesc: '피해 30% 감소 2턴', skillCooldown: 15000, skillUnlockLevel: 5
    },
    rogue: {
        name: '도적', role: 'melee_dps', icon: '🗡',
        baseHp: 160, baseAtk: 35, baseDef: 8,
        attackSpeed: 1000, range: 55, moveSpeed: 140,
        critRate: 0.25, critDmg: 2.0,
        color: 0xcc44cc,
        growthHp: 15, growthAtk: 4, growthDef: 1,
        skillName: '급소 찌르기', skillDesc: '크리 확정+출혈', skillCooldown: 12000, skillUnlockLevel: 5
    },
    mage: {
        name: '마법사', role: 'ranged_dps', icon: '🔮',
        baseHp: 120, baseAtk: 45, baseDef: 5,
        attackSpeed: 2200, range: 280, moveSpeed: 50,
        critRate: 0.10, critDmg: 1.8,
        color: 0x8844ff,
        growthHp: 12, growthAtk: 5, growthDef: 0,
        skillName: '마력 폭발', skillDesc: '범위 피해', skillCooldown: 20000, skillUnlockLevel: 5
    },
    archer: {
        name: '궁수', role: 'ranged_dps', icon: '🏹',
        baseHp: 150, baseAtk: 30, baseDef: 7,
        attackSpeed: 1400, range: 250, moveSpeed: 80,
        critRate: 0.18, critDmg: 1.8,
        color: 0x44cc44,
        growthHp: 14, growthAtk: 3, growthDef: 1,
        skillName: '관통 사격', skillDesc: '일직선 관통', skillCooldown: 15000, skillUnlockLevel: 5
    },
    priest: {
        name: '사제', role: 'healer', icon: '✝',
        baseHp: 180, baseAtk: 12, baseDef: 10,
        attackSpeed: 2000, range: 200, moveSpeed: 60,
        critRate: 0.05, critDmg: 1.3,
        color: 0xffcc44,
        growthHp: 20, growthAtk: 1, growthDef: 1,
        skillName: '신성 치유', skillDesc: '전체 HP 20% 회복', skillCooldown: 18000, skillUnlockLevel: 5
    },
    alchemist: {
        name: '연금술사', role: 'support', icon: '⚗',
        baseHp: 170, baseAtk: 15, baseDef: 12,
        attackSpeed: 1800, range: 180, moveSpeed: 60,
        critRate: 0.08, critDmg: 1.4,
        color: 0x44cccc,
        growthHp: 18, growthAtk: 2, growthDef: 1,
        skillName: '강화 물약', skillDesc: '전체 ATK +20% 2턴', skillCooldown: 20000, skillUnlockLevel: 5
    }
};

const RARITY_DATA = {
    common:    { name: '일반',  color: 0x888888, textColor: '#888888', statMult: 1.0,  growthMult: 1.0, posTraits: 1, negTraits: 1,    hireCostMult: 1.0 },
    uncommon:  { name: '고급',  color: 0x44cc44, textColor: '#44cc44', statMult: 1.1,  growthMult: 1.1, posTraits: 1, negTraits: 0.5,  hireCostMult: 1.5 },
    rare:      { name: '희귀',  color: 0x4488ff, textColor: '#4488ff', statMult: 1.2,  growthMult: 1.2, posTraits: 2, negTraits: 0.5,  hireCostMult: 2.5 },
    epic:      { name: '에픽',  color: 0xcc44ff, textColor: '#cc44ff', statMult: 1.35, growthMult: 1.3, posTraits: 2, negTraits: 0,    hireCostMult: 4.0 },
    legendary: { name: '전설',  color: 0xffaa00, textColor: '#ffaa00', statMult: 1.5,  growthMult: 1.5, posTraits: 2, negTraits: 0,    hireCostMult: 6.0 }
};

const RARITY_POOL = {
    1: { common: 60, uncommon: 35, rare: 5,  epic: 0,  legendary: 0 },
    2: { common: 60, uncommon: 35, rare: 5,  epic: 0,  legendary: 0 },
    3: { common: 40, uncommon: 40, rare: 18, epic: 2,  legendary: 0 },
    4: { common: 40, uncommon: 40, rare: 18, epic: 2,  legendary: 0 },
    5: { common: 25, uncommon: 35, rare: 30, epic: 9,  legendary: 1 },
    6: { common: 25, uncommon: 35, rare: 30, epic: 9,  legendary: 1 },
    7: { common: 15, uncommon: 30, rare: 30, epic: 20, legendary: 5 },
    8: { common: 15, uncommon: 30, rare: 30, epic: 20, legendary: 5 }
};

const BASE_HIRE_COST = 80;

const CLASS_KEYS = Object.keys(CLASS_DATA);
