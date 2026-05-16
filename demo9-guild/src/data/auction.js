// === 경매장 데이터 (v2) ===
// docs/systems/auction.md 와 동기화 유지.
//
// ⚠️ PLACEHOLDER:
// - AUCTION_CUSTOMERS의 이름/외양/선호/대사는 코드 동작용 임시값.
//   캐릭터화(이름·외양·말투·라인업)는 사용자 결정 영역 — docs/systems/auction.md §13 TODO.
// - 대사 풀(AUCTION_DIALOGUE)도 동일. 표정 4단계 × 카테고리별 톤은 추후 결정.

const AUCTION_CUSTOMERS = [
    {
        id: 'guest_warrior',
        name: '떠돌이 전사',
        emoji: '⚔',
        prefCategories: ['equipment'],           // 무기/방어구 선호
        prefRarities: ['uncommon', 'rare'],      // 중급 선호
        priceMult: 1.00,                         // 만족가 시세 대비 (1.0 = 표준)
        patience: 1.0                            // 타이머 배율 (1.0 = 표준)
    },
    {
        id: 'guest_alchemist',
        name: '약초 수집가',
        emoji: '🧪',
        prefCategories: ['material', 'consumable'],
        prefRarities: ['common', 'uncommon', 'rare'],
        priceMult: 0.92,                         // 짜다 (싸게 사려 함)
        patience: 1.2
    },
    {
        id: 'guest_noble',
        name: '귀족',
        emoji: '👑',
        prefCategories: ['equipment', 'consumable'],
        prefRarities: ['rare', 'epic', 'legendary'],
        priceMult: 1.15,                         // 후하다 (비싸도 산다)
        patience: 0.8
    },
    {
        id: 'guest_priest',
        name: '순례자',
        emoji: '✝',
        prefCategories: ['equipment', 'consumable'],
        prefRarities: ['uncommon', 'rare'],
        priceMult: 1.00,
        patience: 1.0
    }
];

// === 손님 대사 (😠 강거절 / 😐 약거절 / 🙂 수용 / 😍 대만족) ===
// placeholder — 카테고리/캐릭터별 분기는 추후.
const AUCTION_DIALOGUE = {
    angry: [
        '터무니없군!',
        '강도놈인가?',
        '도둑맞기 싫소',
        '농담은 그만두지'
    ],
    meh: [
        '조금 비싸군…',
        '글쎄…',
        '흠… 다시 생각해 보지',
        '값을 좀 낮춰 보시오'
    ],
    happy: [
        '그 정도라면 사겠소',
        '거래하지',
        '괜찮은 가격이군',
        '좋소, 받아주시오'
    ],
    elated: [
        '이런 가격이 어디 있겠나!',
        '당장 사겠소!',
        '당신 천재요!',
        '오늘 운이 좋군'
    ]
};

// === 표정·대사 → 시세 힌트 (호감도 60+ 시 노출) ===
// reaction tier 별 시세 위치 화살표
const AUCTION_PRICE_HINT = {
    angry:  '↑↑ (너무 비쌈)',
    meh:    '↑ (약간 비쌈)',
    happy:  '= (적정)',
    elated: '↓ (싸게 줌)'
};

// === 위작/저주 트리거 정의 (Backlog — MVP 미사용) ===
// 트리거 발동 조건은 system 레이어에서 검사
const AUCTION_FAKE_TRIGGERS = [
    { source: 'boss_bp',      label: 'BP 보스 처치',  zone: 'bloodpit', durationH: 24 },
    { source: 'boss_cargo',   label: 'Cargo 보스 처치', zone: 'cargo',    durationH: 24 },
    { source: 'boss_blackout', label: 'BO 보스 처치',  zone: 'blackout', durationH: 24 },
    { source: 'event_blackmarket', label: '암시장의 손길', zone: null,       durationH: 6 }
];

if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        AUCTION_CUSTOMERS,
        AUCTION_DIALOGUE,
        AUCTION_PRICE_HINT,
        AUCTION_FAKE_TRIGGERS
    };
}
