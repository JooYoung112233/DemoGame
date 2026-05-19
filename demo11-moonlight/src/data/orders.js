const ORDER_TEMPLATES = [
    { item: 'health_potion', qty: 2, reward: 80, deadline: 3, npc: '부상당한 기사', icon: '🤕', dialog: '포션이 급해요...' },
    { item: 'health_potion', qty: 3, reward: 130, deadline: 3, npc: '모험가 길드', icon: '⚔️', dialog: '원정 준비 중입니다.' },
    { item: 'antidote', qty: 2, reward: 70, deadline: 3, npc: '약사 할머니', icon: '👵', dialog: '해독제가 떨어졌어요.' },
    { item: 'iron_dagger', qty: 1, reward: 90, deadline: 4, npc: '신입 경비병', icon: '💂', dialog: '무기가 필요합니다.' },
    { item: 'iron_dagger', qty: 2, reward: 170, deadline: 4, npc: '용병단장', icon: '🗡️', dialog: '단검 두 자루 급구.' },
    { item: 'silver_ring', qty: 1, reward: 110, deadline: 5, npc: '젊은 귀족', icon: '💍', dialog: '프로포즈용 반지를...' },
    { item: 'antidote', qty: 3, reward: 100, deadline: 3, npc: '마을 이장', icon: '👴', dialog: '마을에 독충이 퍼졌소.' },
    { item: 'health_potion', qty: 5, reward: 220, deadline: 4, npc: '원정대장', icon: '🧭', dialog: '대량 주문입니다.' },
];

const ORDER_CONFIG = {
    maxActiveOrders: 3,
    newOrderChance: 0.6,
    maxNewPerDay: 2,
};
