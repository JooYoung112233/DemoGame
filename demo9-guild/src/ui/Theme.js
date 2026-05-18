// ─── UI 테마 상수 (판타지 길드) ────────────────────────────────────
// 모든 UI 색상/폰트는 여기서 참조. 직접 매직넘버 금지.

const UI_THEME = {
    // ── 배경 ──
    bg:             0x1a1510,   // 메인 배경 (따뜻한 갈-검)
    bgGrad1:        0x1a1510,   // 그라데이션 시작
    bgGrad2:        0x0e0c08,   // 그라데이션 끝 (좀 더 어두운)

    // ── 패널 ──
    panelFill:      0x2a2218,   // 패널 배경 (다크 레더)
    panelStroke:    0x5a4a2a,   // 패널 테두리 (청동)
    panelTitleBg:   0x352a1a,   // 패널 제목 배경

    // ── 카드 ──
    cardFill:       0x231e14,   // 카드 배경
    cardHover:      0x3a3020,   // 카드 호버
    cardSelected:   0x4a3a20,   // 카드 선택
    cardStroke:     0x4a3c28,   // 카드 기본 테두리

    // ── 헤더 ──
    headerBg:       0x1e1810,   // 상단바 배경
    headerHeight:   55,         // 상단바 높이

    // ── 버튼 ──
    buttonPrimary:  0x8a6a2a,   // 주 버튼 (골드)
    buttonHover:    0xaa8a3a,   // 버튼 호버
    buttonPressed:  0x6a5020,   // 버튼 누름
    buttonDanger:   0x8a2a2a,   // 위험 버튼
    buttonDangerHover: 0xaa3a3a,
    buttonInfo:     0x2a5a8a,   // 정보 버튼
    buttonInfoHover: 0x3a6a9a,
    buttonText:     '#f0e8d0',  // 버튼 텍스트 (밝은 양피지)
    buttonDisabled: 0x333028,
    buttonDisabledText: '#665e48',

    // ── 텍스트 ──
    textPrimary:    '#e8d8c0',  // 주 텍스트 (양피지 백)
    textSecondary:  '#b8a888',  // 보조 텍스트
    textMuted:      '#887860',  // 흐린 텍스트
    textGold:       '#ffcc44',  // 골드 강조
    textAccent:     '#cc8833',  // 청동 액센트
    textDanger:     '#cc4422',  // 위험/빨강
    textSuccess:    '#66bb55',  // 성공/초록
    textBlue:       '#6699cc',  // 정보/파랑

    // ── 희귀도 색상 ──
    rarity: {
        common:     { color: 0x888878, text: '#aaa89a', label: '일반' },
        uncommon:   { color: 0x44aa55, text: '#66cc77', label: '고급' },
        rare:       { color: 0x4488cc, text: '#66aaee', label: '희귀' },
        epic:       { color: 0x9944cc, text: '#bb66ee', label: '영웅' },
        legendary:  { color: 0xdd8800, text: '#ffaa22', label: '전설' }
    },

    // ── 구분선 / 장식 ──
    divider:        0x5a4a2a,   // 구분선 색
    dividerAlpha:   0.5,
    ornament:       0x8a7a4a,   // 장식 금색
    ornamentAlpha:  0.4,

    // ── 모달 ──
    modalOverlay:   0x000000,
    modalOverlayAlpha: 0.7,
    modalBg:        0x2a2218,
    modalStroke:    0x8a7a4a,

    // ── 스크롤바 ──
    scrollTrack:    0x2a2218,
    scrollThumb:    0x6a5a3a,
    scrollThumbHover: 0x8a7a4a,

    // ── 폰트 ──
    fontFamily:     'monospace',
    fontSize: {
        title:   20,
        header:  16,
        body:    12,
        caption: 10,
        tiny:    9
    },

    // ── 레이아웃 ──
    sceneWidth:     1280,
    sceneHeight:    720,
    headerY:        55,         // 헤더 아래 컨텐츠 시작
    padding:        12,
    gap:            10,
    borderRadius:   6,
};

// 헬퍼: 텍스트 스타일 생성
UI_THEME.textStyle = function(tier = 'body', overrides = {}) {
    const size = typeof tier === 'number' ? tier : (UI_THEME.fontSize[tier] || 12);
    return Object.assign({
        fontSize: `${size}px`,
        fontFamily: UI_THEME.fontFamily,
        color: UI_THEME.textPrimary,
    }, overrides);
};

// 헬퍼: 희귀도 색상 가져오기
UI_THEME.getRarityColor = function(rarity) {
    return UI_THEME.rarity[rarity] || UI_THEME.rarity.common;
};

if (typeof module !== 'undefined' && module.exports) {
    module.exports = { UI_THEME };
}
