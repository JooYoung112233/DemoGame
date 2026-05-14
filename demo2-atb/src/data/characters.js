const ATB_CHARS = {
    tank: {
        name: '탱커',
        hp: 450,
        atk: 22,
        def: 12,
        speed: 60,
        critRate: 0.05,
        critDmg: 1.5,
        color: 0x4488ff,
        role: 'tank'
    },
    rogue: {
        name: '도적',
        hp: 140,
        atk: 32,
        def: 3,
        speed: 130,
        critRate: 0.25,
        critDmg: 2.0,
        color: 0xff4444,
        role: 'dps'
    },
    priest: {
        name: '사제',
        hp: 160,
        atk: 12,
        def: 5,
        speed: 85,
        critRate: 0.05,
        critDmg: 1.5,
        color: 0x44ff88,
        role: 'healer'
    },
    mage: {
        name: '마법사',
        hp: 120,
        atk: 55,
        def: 2,
        speed: 70,
        critRate: 0.12,
        critDmg: 1.8,
        color: 0xaa44ff,
        role: 'dps'
    },
    enemy_normal: {
        name: '고블린',
        hp: 300,
        atk: 20,
        def: 5,
        speed: 90,
        critRate: 0.08,
        critDmg: 1.5,
        color: 0x88aa44,
        role: 'enemy'
    },
    enemy_tank: {
        name: '오거',
        hp: 800,
        atk: 28,
        def: 10,
        speed: 45,
        critRate: 0.05,
        critDmg: 1.5,
        color: 0x886622,
        role: 'enemy'
    },
    enemy_ranged: {
        name: '아처',
        hp: 200,
        atk: 30,
        def: 2,
        speed: 100,
        critRate: 0.12,
        critDmg: 1.8,
        color: 0xcc8844,
        role: 'enemy'
    }
};

const ATB_DEFAULT_ENEMIES = ['enemy_normal', 'enemy_normal', 'enemy_tank', 'enemy_ranged', 'enemy_normal'];
