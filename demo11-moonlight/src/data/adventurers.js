const ADVENTURER_DATA = [
    {
        id: 'rookie',
        name: '초보 모험가',
        icon: '🧑',
        cost: 10,
        desc: '싸지만 실력은 글쎄...',
        successRate: 0.65,
        lootMult: 0.8,
        zones: ['forest'],
        quest: {
            name: '첫 번째 시련',
            desc: '숲에서 3번 성공적으로 돌아오기',
            zone: 'forest',
            required: 3,
            reward: { successRate: 0.10, lootMult: 0.2, newZone: 'plains' },
            rewardDesc: '성공률+10%, 전리품+0.2, 들판 해금',
        },
    },
    {
        id: 'hunter',
        name: '사냥꾼',
        icon: '🏹',
        cost: 25,
        desc: '들판의 야생동물은 맡겨라.',
        successRate: 0.80,
        lootMult: 1.0,
        zones: ['forest', 'plains'],
        bonusCategory: 'hide',
        quest: {
            name: '아버지의 활',
            desc: '들판에서 3번 성공하여 잃어버린 활을 찾기',
            zone: 'plains',
            required: 3,
            reward: { successRate: 0.05, lootMult: 0.3, bonusCategory: 'monster' },
            rewardDesc: '성공률+5%, 전리품+0.3, 몬스터 드랍 보너스',
        },
    },
    {
        id: 'veteran',
        name: '베테랑 전사',
        icon: '⚔️',
        cost: 50,
        desc: '비싸지만 확실하다.',
        successRate: 0.90,
        lootMult: 1.3,
        zones: ['forest', 'plains', 'mine'],
        quest: {
            name: '전우의 유품',
            desc: '광산에서 4번 성공하여 전우의 검을 찾기',
            zone: 'mine',
            required: 4,
            reward: { successRate: 0.05, lootMult: 0.2, newZone: 'ruins' },
            rewardDesc: '성공률+5%, 전리품+0.2, 폐허 해금',
        },
    },
    {
        id: 'miner',
        name: '광부',
        icon: '⛏️',
        cost: 35,
        desc: '광산 전문. 보석을 잘 캔다.',
        successRate: 0.85,
        lootMult: 1.0,
        zones: ['forest', 'mine'],
        bonusCategory: 'ore',
        quest: {
            name: '전설의 광맥',
            desc: '광산에서 4번 성공하여 전설의 광맥을 발견',
            zone: 'mine',
            required: 4,
            reward: { successRate: 0.05, lootMult: 0.4, bonusCategory: 'gem' },
            rewardDesc: '성공률+5%, 전리품+0.4, 보석 드랍 보너스',
        },
    },
    {
        id: 'scholar',
        name: '학자',
        icon: '📚',
        cost: 60,
        desc: '폐허의 유물을 알아보는 눈.',
        successRate: 0.75,
        lootMult: 1.2,
        zones: ['forest', 'plains', 'ruins'],
        bonusCategory: 'relic',
        quest: {
            name: '잃어버린 문헌',
            desc: '폐허에서 3번 성공하여 고대 문헌을 해독',
            zone: 'ruins',
            required: 3,
            reward: { successRate: 0.10, lootMult: 0.3 },
            rewardDesc: '성공률+10%, 전리품+0.3',
        },
    },
    {
        id: 'elite',
        name: '엘리트 용병',
        icon: '🗡️',
        cost: 80,
        desc: '최고의 실력. 어디든 간다.',
        successRate: 0.95,
        lootMult: 1.5,
        zones: ['forest', 'plains', 'mine', 'ruins'],
        unlockDay: 8,
        quest: {
            name: '최후의 임무',
            desc: '폐허에서 5번 성공하여 봉인된 유물 회수',
            zone: 'ruins',
            required: 5,
            reward: { lootMult: 0.5 },
            rewardDesc: '전리품+0.5',
        },
    },
];

// 레벨업 테이블: [필요 누적 경험치, 성공률 보너스, 전리품 보너스]
const ADV_LEVEL_TABLE = [
    { exp: 0,   srBonus: 0,    lootBonus: 0 },    // Lv1
    { exp: 15,  srBonus: 0.03, lootBonus: 0.05 },  // Lv2
    { exp: 40,  srBonus: 0.06, lootBonus: 0.10 },  // Lv3
    { exp: 80,  srBonus: 0.10, lootBonus: 0.15 },  // Lv4
    { exp: 150, srBonus: 0.15, lootBonus: 0.25 },  // Lv5
];
const ADV_MAX_LEVEL = 5;
