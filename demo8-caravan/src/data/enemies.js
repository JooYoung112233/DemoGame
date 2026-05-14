const ENEMY_DATA = {
    bandit: {
        name: '산적', hp: 150, atk: 18, def: 4,
        attackSpeed: 1600, range: 55, moveSpeed: 90,
        color: 0x886644, critRate: 0.05, critDmg: 1.5,
        role: 'melee', skills: []
    },
    bandit_archer: {
        name: '산적 궁수', hp: 100, atk: 24, def: 2,
        attackSpeed: 2000, range: 200, moveSpeed: 65,
        color: 0xaa8844, critRate: 0.10, critDmg: 1.5,
        role: 'ranged', skills: []
    },
    bandit_chief: {
        name: '산적 두목', hp: 300, atk: 25, def: 8,
        attackSpeed: 2000, range: 60, moveSpeed: 75,
        color: 0xcc6622, critRate: 0.10, critDmg: 1.8,
        role: 'melee', skills: ['enemy_war_cry']
    },
    demon_soldier: {
        name: '마족 병사', hp: 250, atk: 30, def: 10,
        attackSpeed: 1600, range: 55, moveSpeed: 85,
        color: 0x662244, critRate: 0.10, critDmg: 1.5,
        role: 'melee', skills: []
    },
    demon_mage: {
        name: '마족 술사', hp: 180, atk: 40, def: 3,
        attackSpeed: 2500, range: 220, moveSpeed: 60,
        color: 0x8844aa, critRate: 0.15, critDmg: 1.8,
        role: 'ranged', skills: ['enemy_dark_bolt']
    },
    demon_king: {
        name: '마왕', hp: 1200, atk: 55, def: 18,
        attackSpeed: 2200, range: 80, moveSpeed: 70,
        color: 0xff2244, critRate: 0.15, critDmg: 2.0,
        role: 'melee', skills: ['enemy_dark_wave', 'enemy_self_heal']
    }
};
