const BO_ALLIES = {
    scout: {
        name: '정찰병', hp: 220, atk: 28, def: 8, attackSpeed: 900,
        range: 60, moveSpeed: 110, critRate: 0.15, critDmg: 1.8,
        color: 0x44cc88, role: 'dps',
        desc: '빠른 이동, 높은 크리티컬'
    },
    breacher: {
        name: '돌파병', hp: 350, atk: 35, def: 15, attackSpeed: 1200,
        range: 50, moveSpeed: 80, critRate: 0.10, critDmg: 1.5,
        color: 0xcc6644, role: 'tank',
        desc: '높은 HP와 방어력'
    },
    hacker: {
        name: '해커', hp: 180, atk: 22, def: 5, attackSpeed: 1100,
        range: 180, moveSpeed: 70, critRate: 0.12, critDmg: 1.6,
        color: 0x44aaff, role: 'support',
        desc: '원거리 공격, 해킹 지원'
    },
    medic: {
        name: '의무관', hp: 200, atk: 15, def: 10, attackSpeed: 1400,
        range: 60, moveSpeed: 75, critRate: 0.05, critDmg: 1.5,
        color: 0x88ff44, role: 'healer',
        desc: '아군 치료 전문'
    }
};

const BO_ENEMIES = {
    stalker: {
        name: '스토커', type: 'stalker', hp: 150, atk: 25, def: 5, attackSpeed: 800,
        range: 50, moveSpeed: 120, critRate: 0.20, critDmg: 1.8,
        color: 0x884488, dodgeRate: 0.25,
        desc: '빠르고 회피율 높음'
    },
    charger: {
        name: '차저', type: 'charger', hp: 280, atk: 40, def: 12, attackSpeed: 1500,
        range: 50, moveSpeed: 100, critRate: 0.10, critDmg: 2.0,
        color: 0xcc4444,
        desc: '강력한 선공'
    },
    creeper: {
        name: '크리퍼', type: 'creeper', hp: 120, atk: 18, def: 3, attackSpeed: 1000,
        range: 100, moveSpeed: 60, critRate: 0.05, critDmg: 1.5,
        color: 0x44aa44, poisonChance: 0.30,
        desc: '독 공격'
    },
    collector: {
        name: '수집가', type: 'collector', hp: 200, atk: 22, def: 8, attackSpeed: 1100,
        range: 60, moveSpeed: 90, critRate: 0.10, critDmg: 1.5,
        color: 0xffaa44, stealChance: 0.20,
        desc: '아이템 도난 확률'
    }
};
