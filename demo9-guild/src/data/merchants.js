const MERCHANT_DATA = {
    krog: {
        name: '크로그', icon: '🧌', role: '무기상',
        desc: '투기장 출신 무기 딜러. 장비 거래 할인.',
        tiers: [
            { level: 0, name: '낯선 이', discount: 0, perks: '' },
            { level: 3, name: '단골', discount: 5, perks: '장비 구매 5% 할인' },
            { level: 6, name: '동업자', discount: 10, perks: '흥정 성공률 +15%' },
            { level: 10, name: '맹우', discount: 15, perks: '전설 장비 출현 확률 UP' }
        ],
        favorActions: { buy_equipment: 1, sell_equipment: 0.5, forge: 2 }
    },
    speedka: {
        name: '스피드카', icon: '🦊', role: '정보상',
        desc: '뒷골목 정보 브로커. 정보소/경매 관련 보너스.',
        tiers: [
            { level: 0, name: '경계 대상', discount: 0, perks: '' },
            { level: 3, name: '거래처', discount: 0, perks: '경매 위탁 판매 확률 +10%' },
            { level: 6, name: '파트너', discount: 0, perks: '정보소 비용 -30%' },
            { level: 10, name: '혈맹', discount: 0, perks: '경매 슬롯 +2, 자동 시세 알림' }
        ],
        favorActions: { consign: 1, bid: 1, intel: 2, run_success: 0.5 }
    },
    iron: {
        name: '철강', icon: '⚒', role: '대장장이',
        desc: '전설의 대장장이. 제작/강화 관련 보너스.',
        tiers: [
            { level: 0, name: '초면', discount: 0, perks: '' },
            { level: 3, name: '의뢰인', discount: 0, perks: '제작 비용 -10%' },
            { level: 6, name: '제자', discount: 0, perks: '레시피 추가 개방' },
            { level: 10, name: '후계자', discount: 0, perks: '제작 시 2배 결과 확률 10%' }
        ],
        favorActions: { forge: 2, buy_material: 1, sell_material: 0.5 }
    },
    gold: {
        name: '금빛 리리', icon: '💰', role: '경매사',
        desc: '경매장 관리인. 경매 수수료 할인 및 시세 정보.',
        tiers: [
            { level: 0, name: '신입', discount: 0, perks: '' },
            { level: 3, name: '회원', discount: 0, perks: '경매 수수료 -3%' },
            { level: 6, name: 'VIP', discount: 0, perks: '시세 정보 공개, 수수료 -5%' },
            { level: 10, name: '대주주', discount: 0, perks: '즉시 구매 가격 -20%' }
        ],
        favorActions: { buy: 1, sell: 1, bid: 0.5, auction_refresh: 1 }
    }
};

const MERCHANT_KEYS = Object.keys(MERCHANT_DATA);

function initMerchantFavor(gs) {
    if (gs.merchantFavor) return;
    gs.merchantFavor = {};
    for (const key of MERCHANT_KEYS) {
        gs.merchantFavor[key] = 0;
    }
    gs.merchantFavor.auction = 0;
}

function addMerchantFavor(gs, merchantKey, amount) {
    initMerchantFavor(gs);
    gs.merchantFavor[merchantKey] = Math.min(12, (gs.merchantFavor[merchantKey] || 0) + amount);
    if (merchantKey === 'gold' || merchantKey === 'speedka') {
        gs.merchantFavor.auction = Math.max(gs.merchantFavor.gold || 0, gs.merchantFavor.speedka || 0);
    }
}

function getMerchantTier(gs, merchantKey) {
    initMerchantFavor(gs);
    const favor = gs.merchantFavor[merchantKey] || 0;
    const data = MERCHANT_DATA[merchantKey];
    if (!data) return null;
    let current = data.tiers[0];
    for (const tier of data.tiers) {
        if (favor >= tier.level) current = tier;
    }
    return current;
}

function getMerchantDiscount(gs, merchantKey) {
    const tier = getMerchantTier(gs, merchantKey);
    return tier ? tier.discount : 0;
}

function processMerchantAction(gs, action) {
    initMerchantFavor(gs);
    for (const [key, data] of Object.entries(MERCHANT_DATA)) {
        const amount = data.favorActions[action];
        if (amount) addMerchantFavor(gs, key, amount);
    }
}
