// 상점 아이템 데이터
const SHOP_ITEMS = {
    heal_potion: {
        id: 'heal_potion', name: 'HP 물약', icon: '❤️‍🩹',
        desc: 'HP를 20 회복한다',
        cost: 8, type: 'consumable',
        effect: { heal: 20 }
    },
    random_symbol: {
        id: 'random_symbol', name: '미지의 심볼', icon: '🎲',
        desc: '랜덤 심볼 1개를 덱에 추가',
        cost: 5, type: 'consumable',
        effect: { randomSymbol: true }
    },
    random_combo: {
        id: 'random_combo', name: '비전서', icon: '📜',
        desc: '랜덤 조합 1개를 강화',
        cost: 12, type: 'consumable',
        effect: { randomComboUpgrade: true }
    },
    max_hp_up: {
        id: 'max_hp_up', name: '생명의 결정', icon: '💖',
        desc: '최대 HP +10',
        cost: 15, type: 'consumable',
        effect: { maxHpUp: 10 }
    },
    remove_skull: {
        id: 'remove_skull', name: '정화의 부적', icon: '✨',
        desc: '덱에서 해골 1개 제거',
        cost: 6, type: 'consumable',
        effect: { removeSkull: true }
    },
};

const SYMBOL_REMOVE_COST = 3;
