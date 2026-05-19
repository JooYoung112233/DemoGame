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
