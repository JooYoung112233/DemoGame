// === 원정 중 랜덤 이벤트 (기존) ===
const EXPEDITION_EVENTS = [
    {
        id: 'monster_ambush',
        text: '⚠️ 수풀에서 몬스터가 튀어나왔다!',
        type: 'choice',
        choices: [
            { label: '⚔️ 싸운다', outcomes: [
                { weight: 60, text: '✅ 몬스터를 물리쳤다! 추가 전리품 획득!', loot: 2, rep: 1 },
                { weight: 40, text: '💥 고전 끝에 물리쳤지만 부상을 입었다...', loot: 1, injury: true },
            ]},
            { label: '🏃 도망친다', outcomes: [
                { weight: 70, text: '💨 무사히 도망쳤다.', loot: 0 },
                { weight: 30, text: '😰 도망치다가 짐을 떨어뜨렸다!', loot: -1 },
            ]},
        ],
    },
    {
        id: 'treasure_chest',
        text: '✨ 오래된 상자를 발견했다!',
        type: 'choice',
        choices: [
            { label: '🔓 연다', outcomes: [
                { weight: 50, text: '💎 보물이다! 귀한 아이템 발견!', lootRare: true, loot: 1 },
                { weight: 30, text: '🪙 동전 몇 닢이 들어있다.', gold: 15 },
                { weight: 20, text: '💀 함정이었다! 독가스가...', injury: true, loot: 0 },
            ]},
            { label: '🚫 무시한다', outcomes: [
                { weight: 100, text: '조심스럽게 지나쳤다.', loot: 0 },
            ]},
        ],
    },
    {
        id: 'herb_field',
        text: '🌿 약초가 가득한 풀밭을 발견했다!',
        type: 'auto',
        outcomes: [
            { weight: 100, text: '🌿 약초를 잔뜩 채집했다!', loot: 2, category: 'herb' },
        ],
    },
    {
        id: 'lost_merchant',
        text: '👤 길 잃은 상인을 만났다.',
        type: 'choice',
        choices: [
            { label: '🤝 도와준다', outcomes: [
                { weight: 70, text: '감사합니다! 이것을 받아주세요.', loot: 1, lootRare: true, rep: 2 },
                { weight: 30, text: '감사합니다! 마을에서 좋은 소문 내겠습니다.', rep: 3 },
            ]},
            { label: '🚶 지나친다', outcomes: [
                { weight: 100, text: '바쁜 걸음을 재촉했다.', loot: 0 },
            ]},
        ],
    },
    {
        id: 'cave_entrance',
        text: '🕳️ 어두운 동굴 입구를 발견했다...',
        type: 'choice',
        choices: [
            { label: '🔦 들어간다', outcomes: [
                { weight: 40, text: '💎 동굴 안에 광맥이! 대박!', loot: 3, category: 'ore' },
                { weight: 30, text: '🦇 박쥐 떼가 덮쳤다! 겨우 빠져나왔다.', injury: true, loot: 1 },
                { weight: 30, text: '😶 아무것도 없는 빈 동굴이었다.', loot: 0 },
            ]},
            { label: '🚫 지나친다', outcomes: [
                { weight: 100, text: '위험해 보여서 지나쳤다.', loot: 0 },
            ]},
        ],
    },
    {
        id: 'nice_weather',
        text: '☀️ 날씨가 좋다. 기분 좋게 탐색 중...',
        type: 'auto',
        outcomes: [
            { weight: 100, text: '순조롭게 재료를 모았다.', loot: 1 },
        ],
    },
    {
        id: 'wild_animal',
        text: '🐾 야생동물의 흔적을 발견했다.',
        type: 'choice',
        choices: [
            { label: '🏹 추적한다', outcomes: [
                { weight: 50, text: '🎯 사냥 성공! 가죽을 얻었다.', loot: 1, category: 'hide' },
                { weight: 50, text: '😤 놓쳤다. 시간만 낭비했다.', loot: 0 },
            ]},
            { label: '🚶 무시한다', outcomes: [
                { weight: 100, text: '다른 곳을 탐색했다.', loot: 0 },
            ]},
        ],
    },
];

// === 날씨 시스템 ===
const WEATHER_DATA = {
    clear: {
        name: '맑음',
        icon: '☀️',
        desc: '화창한 날씨. 평범한 하루.',
        customerMult: 1.0,      // 손님 수 배율
        successRateBonus: 0.0,  // 원정 성공률 보너스
        demandBoost: null,      // 특정 카테고리 수요 증가
        weight: 40,
    },
    sunny: {
        name: '쾌청',
        icon: '🌤️',
        desc: '상쾌한 날! 모험하기 좋다.',
        customerMult: 1.1,
        successRateBonus: 0.10,
        demandBoost: null,
        weight: 20,
    },
    rain: {
        name: '비',
        icon: '🌧️',
        desc: '비가 내린다. 손님이 적고 포션 수요 증가.',
        customerMult: 0.7,
        successRateBonus: -0.05,
        demandBoost: 'herb',
        weight: 20,
    },
    fog: {
        name: '안개',
        icon: '🌫️',
        desc: '짙은 안개. 길을 잃기 쉽다.',
        customerMult: 0.85,
        successRateBonus: -0.08,
        demandBoost: null,
        weight: 10,
    },
    storm: {
        name: '폭풍',
        icon: '⛈️',
        desc: '폭풍이 몰아친다! 원정 불가.',
        customerMult: 0.5,
        successRateBonus: -1.0, // 사실상 원정 불가
        demandBoost: null,
        expeditionBlocked: true,
        weight: 10,
    },
};

// === 일일 랜덤 이벤트 ===
const DAILY_EVENTS = [
    {
        id: 'festival',
        name: '마을 축제',
        icon: '🎉',
        desc: '마을에 축제가 열렸다! 손님이 몰려온다.',
        effect: {
            customerMult: 2.0,
            demandBoost: null,  // 모든 카테고리 수요 증가는 아님
            repBonus: 1,
        },
        weight: 10,
        minDay: 3,
    },
    {
        id: 'merchant_caravan',
        name: '상인 행렬',
        icon: '🐫',
        desc: '먼 곳에서 상인 행렬이 왔다. 부자 손님이 많다.',
        effect: {
            richCustomerBoost: true, // 부자 손님 출현률 증가
            repBonus: 0,
        },
        weight: 12,
        minDay: 4,
    },
    {
        id: 'bandit_rumor',
        name: '도적 출현 소문',
        icon: '🏴‍☠️',
        desc: '도적이 출현한다는 소문. 원정이 위험하지만 보상도 크다.',
        effect: {
            successRateBonus: -0.10,
            lootBonus: 0.5,
        },
        weight: 12,
        minDay: 3,
    },
    {
        id: 'royal_demand',
        name: '왕실 납품 요청',
        icon: '👑',
        desc: '왕실에서 긴급 납품을 요청했다. 가공품 가격 상승!',
        effect: {
            priceBoost: { category: 'crafted', mult: 1.5 },
        },
        weight: 8,
        minDay: 5,
    },
    {
        id: 'gem_rush',
        name: '보석 열풍',
        icon: '💎',
        desc: '보석에 대한 수요가 폭증하고 있다!',
        effect: {
            priceBoost: { category: 'gem', mult: 2.0 },
        },
        weight: 8,
        minDay: 7,
    },
    {
        id: 'herb_blight',
        name: '약초 흉작',
        icon: '🥀',
        desc: '올해는 약초가 부족하다. 약초 가격 상승!',
        effect: {
            priceBoost: { category: 'herb', mult: 1.8 },
        },
        weight: 10,
        minDay: 4,
    },
    {
        id: 'wandering_healer',
        name: '떠돌이 치료사',
        icon: '💊',
        desc: '치료사가 마을에 왔다. 부상 모험가를 치료해준다.',
        effect: {
            healAllAdventurers: true,
        },
        weight: 8,
        minDay: 4,
    },
    {
        id: 'guild_inspection',
        name: '길드 점검',
        icon: '📋',
        desc: '상인 길드에서 점검이 왔다. 평판에 따라 보상.',
        effect: {
            guildReward: true,  // 평판 기반 골드 보상
        },
        weight: 8,
        minDay: 5,
    },
    {
        id: 'monster_wave',
        name: '몬스터 출몰',
        icon: '👹',
        desc: '마을 근처에 몬스터가 출몰! 모험가 장비 수요 급증.',
        effect: {
            priceBoost: { category: 'crafted', mult: 1.4 },
            customerMult: 1.3,
        },
        weight: 10,
        minDay: 3,
    },
    {
        id: 'tax_day',
        name: '세금 징수',
        icon: '📜',
        desc: '오늘은 세금 납부일. 보유 골드의 10%를 납부한다.',
        effect: {
            taxRate: 0.10,
        },
        weight: 8,
        minDay: 6,
    },
];

// === 평판 단계 ===
const REPUTATION_TIERS = [
    { min: -20, name: '악덕 상인',     icon: '💀', color: '#ff4444', customerBonus: -3, desc: '가격이 너무 비싸다는 소문' },
    { min: -5,  name: '무명',          icon: '❔', color: '#666666', customerBonus: 0,  desc: '아직 이름이 알려지지 않았다' },
    { min: 5,   name: '골목 상점',     icon: '🏪', color: '#aaaaaa', customerBonus: 1,  desc: '동네에서 알아주는 가게' },
    { min: 15,  name: '인기 가게',     icon: '⭐', color: '#ffcc44', customerBonus: 3,  desc: '손님이 줄을 선다!' },
    { min: 25,  name: '명문 상점',     icon: '🏆', color: '#ff88ff', customerBonus: 5,  desc: '도시 전체에 소문난 가게' },
    { min: 35,  name: '왕실 납품처',   icon: '👑', color: '#44ccff', customerBonus: 7,  desc: '왕실의 인정을 받았다' },
    { min: 45,  name: '전설의 상점',   icon: '🌟', color: '#ffaa00', customerBonus: 10, desc: '대륙에 이름이 알려진 전설' },
];
