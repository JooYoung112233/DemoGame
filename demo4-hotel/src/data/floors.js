const FLOOR_CONFIG = {
    floorTypes: [
        'normal',   // 1
        'event',    // 2
        'normal',   // 3
        'shop',     // 4
        'elite',    // 5
        'event',    // 6
        'normal',   // 7
        'event',    // 8
        'elite',    // 9
        'boss'      // 10
    ],

    normalFloors: {
        1: [{ type: 'guest', count: 1 }],
        3: [{ type: 'bellboy', count: 1 }],
        7: [{ type: 'drone', count: 1 }]
    },

    eliteFloors: {
        5: [{ type: 'bellboy', count: 1, elite: true }],
        9: [{ type: 'shadow', count: 1, elite: true }]
    },

    bossFloor: {
        boss: true,
        adds: [{ type: 'guest', count: 1 }]
    },

    getScale(floor) {
        return 1 + (floor - 1) * 0.12;
    },

    getFloorLabel(type) {
        const labels = {
            normal: '일반 전투',
            elite: '엘리트 전투',
            event: '이벤트',
            shop: '상점',
            boss: '보스 전투'
        };
        return labels[type] || '???';
    },

    getFloorColor(type) {
        const colors = {
            normal: '#888888',
            elite: '#ffaa44',
            event: '#44ff88',
            shop: '#44aaff',
            boss: '#ff4466'
        };
        return colors[type] || '#888888';
    }
};

const EVENT_DATA = [
    {
        title: '수상한 자판기',
        desc: '낡은 자판기에서 희미한 빛이 새어나온다.',
        choices: [
            { text: '음료 마시기', desc: 'HP 30% 회복', apply(state) { state.hp = Math.min(state.hp + Math.floor(state.maxHp * 0.3), state.maxHp); } },
            { text: '부수기', desc: '랜덤 강화 1개 획득', apply(state) { return 'random_upgrade'; } }
        ]
    },
    {
        title: '쓰러진 생존자',
        desc: '복도에 쓰러진 누군가가 도움을 요청한다.',
        choices: [
            { text: '도와주기', desc: 'HP 20% 소모, 랜덤 강화 획득', apply(state) { state.hp = Math.max(1, state.hp - Math.floor(state.maxHp * 0.2)); return 'random_upgrade'; } },
            { text: '무시하기', desc: '코인 5개 획득', apply(state) { state.coins += 5; } }
        ]
    },
    {
        title: '저주받은 장비',
        desc: '바닥에 붉게 빛나는 장비가 놓여있다.',
        choices: [
            { text: '장착하기', desc: '공격 +5, 최대 HP -20%', apply(state) { state.bonusDmg = (state.bonusDmg || 0) + 5; state.maxHp = Math.floor(state.maxHp * 0.8); state.hp = Math.min(state.hp, state.maxHp); } },
            { text: '지나가기', desc: 'HP 10% 회복', apply(state) { state.hp = Math.min(state.hp + Math.floor(state.maxHp * 0.1), state.maxHp); } }
        ]
    },
    {
        title: '호텔 전화기',
        desc: '벽에 걸린 전화기가 울리고 있다.',
        choices: [
            { text: '받기', desc: '랜덤 효과', apply(state) { const roll = Math.random(); if (roll < 0.4) { state.hp = Math.min(state.hp + Math.floor(state.maxHp * 0.25), state.maxHp); return 'heal'; } else if (roll < 0.7) { state.stamina = Math.min(state.stamina + 20, state.maxStamina); return 'stamina'; } else { return 'random_upgrade'; } } },
            { text: '무시하기', desc: '아무 일도 일어나지 않음', apply(state) {} }
        ]
    },
    {
        title: '응급 의료 키트',
        desc: '벽에 걸린 빨간 응급 상자를 발견했다.',
        choices: [
            { text: '사용하기', desc: 'HP 50% 회복', apply(state) { state.hp = Math.min(state.hp + Math.floor(state.maxHp * 0.5), state.maxHp); } },
            { text: '챙기기', desc: '코인 8개 획득', apply(state) { state.coins += 8; } }
        ]
    },
    {
        title: '휴게 소파',
        desc: '복도 한 구석에 편안해 보이는 소파가 있다.',
        choices: [
            { text: '쉬기', desc: 'SP 완전 회복', apply(state) { state.stamina = state.maxStamina; } },
            { text: '뒤지기', desc: '코인 3개 + 랜덤 효과', apply(state) { state.coins += 3; if (Math.random() < 0.5) { return 'random_upgrade'; } } }
        ]
    },
    {
        title: '갑작스런 정전',
        desc: '갑자기 불이 꺼졌다. 어둠 속에서 뭔가 빛난다.',
        choices: [
            { text: '빛을 따라가기', desc: '랜덤: 강화 또는 HP 손실', apply(state) { if (Math.random() < 0.6) { return 'random_upgrade'; } else { state.hp = Math.max(1, state.hp - Math.floor(state.maxHp * 0.15)); } } },
            { text: '가만히 기다리기', desc: 'SP 10 회복', apply(state) { state.stamina = Math.min(state.stamina + 10, state.maxStamina); } }
        ]
    },
    {
        title: '카지노 슬롯머신',
        desc: '아직 작동하는 슬롯머신이 있다. 코인을 넣어볼까?',
        choices: [
            { text: '도박하기 (5코인)', desc: '50%: 코인 20개 / 50%: 없음', apply(state) { if (state.coins >= 5) { state.coins -= 5; if (Math.random() < 0.5) { state.coins += 20; } } } },
            { text: '무시하기', desc: '안전하게 지나가기', apply(state) {} }
        ]
    }
];

const SHOP_ITEMS = [
    { name: '응급 치료', desc: 'HP 완전 회복', cost: 10, apply(state) { state.hp = state.maxHp; } },
    { name: '최대 HP 증가', desc: '최대 HP +20', cost: 8, apply(state) { state.maxHp += 20; state.hp += 20; } },
    { name: '스태미나 회복', desc: 'SP 완전 회복', cost: 6, apply(state) { state.stamina = state.maxStamina; } },
    { name: '스태미나 강화', desc: '최대 SP +15', cost: 10, apply(state) { state.maxStamina += 15; state.stamina += 15; } },
    { name: '랜덤 강화', desc: '랜덤 강화 1개', cost: 15, apply(state) { return 'random_upgrade'; } }
];
