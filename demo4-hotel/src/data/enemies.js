const HOTEL_ENEMIES = {
    guest: {
        name: '투숙객',
        color: 0x88aa44,
        hp: 40,
        size: 10,
        coinValue: 1,
        actions: [
            { name: '약공격', weight: 60, dmgMin: 5, dmgMax: 8 },
            { name: '강공격', weight: 15, dmgMin: 12, dmgMax: 18 },
            { name: '방어', weight: 15, special: 'defend' },
            { name: '대기', weight: 10, special: 'idle' }
        ]
    },
    bellboy: {
        name: '벨보이',
        color: 0xff8844,
        hp: 25,
        size: 8,
        coinValue: 2,
        actions: [
            { name: '돌진', weight: 40, dmgMin: 10, dmgMax: 16 },
            { name: '약공격', weight: 35, dmgMin: 6, dmgMax: 10 },
            { name: '회피', weight: 15, special: 'dodge' },
            { name: '대기', weight: 10, special: 'idle' }
        ]
    },
    drone: {
        name: '청소 드론',
        color: 0x44aaff,
        hp: 30,
        size: 9,
        coinValue: 2,
        actions: [
            { name: '원거리 사격', weight: 50, dmgMin: 6, dmgMax: 10 },
            { name: '집속 포격', weight: 20, dmgMin: 14, dmgMax: 22 },
            { name: '방어 모드', weight: 20, special: 'defend' },
            { name: '재장전', weight: 10, special: 'idle' }
        ]
    },
    shadow: {
        name: '그림자 손님',
        color: 0xaa44ff,
        hp: 35,
        size: 9,
        coinValue: 3,
        actions: [
            { name: '기습', weight: 35, dmgMin: 15, dmgMax: 22 },
            { name: '그림자 베기', weight: 30, dmgMin: 8, dmgMax: 14 },
            { name: '은신', weight: 25, special: 'dodge' },
            { name: '응시', weight: 10, special: 'debuff', effect: 'hitDown' }
        ]
    }
};

const BOSS_DATA = {
    name: '호텔 경비 로봇',
    color: 0xff2244,
    hp: 300,
    size: 28,
    coinValue: 20,
    phases: [
        {
            hpThreshold: 1.0,
            actions: [
                { name: '돌진', weight: 40, dmgMin: 12, dmgMax: 18 },
                { name: '약공격', weight: 35, dmgMin: 8, dmgMax: 14 },
                { name: '방어', weight: 15, special: 'defend' },
                { name: '잡몹 소환', weight: 10, special: 'summon' }
            ]
        },
        {
            hpThreshold: 0.6,
            actions: [
                { name: '레이저', weight: 35, dmgMin: 18, dmgMax: 28 },
                { name: '돌진', weight: 30, dmgMin: 14, dmgMax: 22 },
                { name: '방어', weight: 15, special: 'defend' },
                { name: '잡몹 소환', weight: 10, special: 'summon' },
                { name: '수리', weight: 10, special: 'heal' }
            ]
        },
        {
            hpThreshold: 0.3,
            actions: [
                { name: '미사일 난사', weight: 40, dmgMin: 22, dmgMax: 35 },
                { name: '레이저', weight: 30, dmgMin: 20, dmgMax: 30 },
                { name: '돌진', weight: 20, dmgMin: 16, dmgMax: 25 },
                { name: '수리', weight: 10, special: 'heal' }
            ]
        }
    ]
};
