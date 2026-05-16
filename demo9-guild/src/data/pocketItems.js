/**
 * 수동 아이템 (pocket) 데이터 — BP 쉬는 곳 전용.
 * 파티 공용 2칸, 길드회관 F2 해금.
 */
const POCKET_ITEM_DATA = {
    pot_heal_l: {
        name: '큰 치유 물약',
        icon: '🧪',
        desc: '아군 1명 HP 100% 회복',
        targetType: 'single_ally',
        effect: { healPct: 1.0 }
    },
    pot_heal_g: {
        name: '단체 치유 물약',
        icon: '💊',
        desc: '아군 전체 HP 40% 회복',
        targetType: 'all_ally',
        effect: { healPct: 0.4 }
    },
    pot_revive: {
        name: '부활의 영약',
        icon: '✨',
        desc: '사망자 1명 HP 50% 부활',
        targetType: 'single_dead',
        effect: { revivePct: 0.5 }
    },
    pot_purify: {
        name: '정화의 향유',
        icon: '🕊',
        desc: '아군 전체 상태이상 제거',
        targetType: 'all_ally',
        effect: { purify: true }
    },
    pot_atk: {
        name: '광기의 술',
        icon: '🍷',
        desc: '아군 전체 ATK +20% (다음 1R)',
        targetType: 'all_ally',
        effect: { atkBuff: 0.2, buffDuration: 1 }
    },
    pot_pit: {
        name: '피의 술잔',
        icon: '🩸',
        desc: '핏 게이지 즉시 +15%',
        targetType: 'none',
        effect: { pitGaugeAdd: 15 }
    }
};

/**
 * 마을 상점에서 구매 가능한 pocket 아이템 목록 + 가격.
 */
const POCKET_ITEM_SHOP = {
    pot_heal_l:  { cost: 80 },
    pot_heal_g:  { cost: 120 },
    pot_revive:  { cost: 200 },
    pot_purify:  { cost: 100 },
    pot_atk:     { cost: 150 },
    pot_pit:     { cost: 100 }
};
