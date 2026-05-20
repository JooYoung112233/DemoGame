// ── THE LAST THEATER — SHOP (소품실) DATA ─────────────────
const SHOP_ITEMS = {
    heal_potion: {
        id: 'heal_potion', name: '치유의 장미', icon: '🌹',
        desc: '감독의 HP를 20 회복한다',
        cost: 8, type: 'consumable',
        effect: { heal: 20 }
    },
    random_symbol: {
        id: 'random_symbol', name: '미지의 배역', icon: '🎲',
        desc: '랜덤 심볼 1개를 연출 덱에 추가',
        cost: 5, type: 'consumable',
        effect: { randomSymbol: true }
    },
    random_combo: {
        id: 'random_combo', name: '잊혀진 대본', icon: '📜',
        desc: '랜덤 Scene 1개를 강화',
        cost: 12, type: 'consumable',
        effect: { randomComboUpgrade: true }
    },
    max_hp_up: {
        id: 'max_hp_up', name: '생명의 촛불', icon: '🕯️',
        desc: '최대 HP +10',
        cost: 15, type: 'consumable',
        effect: { maxHpUp: 10 }
    },
    remove_skull: {
        id: 'remove_skull', name: '저주 해제', icon: '✨',
        desc: '덱에서 망자(💀) 1개 제거',
        cost: 6, type: 'consumable',
        effect: { removeSkull: true }
    },
};

const SYMBOL_REMOVE_COST = 3;
