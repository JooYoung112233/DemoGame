/**
 * 길드 회관 업그레이드 트리 — 8 카테고리 × 12 단계 = 96 업그레이드.
 *
 * 각 카테고리는 단계별 효과 정의 (effect 객체) + 누적 효과 합산 방식.
 * effect는 `effects` 함수로 누적값 계산해서 게임 다른 시스템에서 조회.
 *
 * 비용 공식: round(baseCost × 1.5^(stage-1))
 */
const GUILD_HALL_DATA = {
    operations: {
        key: 'operations',
        name: '운영',
        icon: '⚙',
        color: 0x4488ff,
        textColor: '#88ccff',
        baseCost: 300,
        unlockCondition: 'always',
        desc: '파견 슬롯, 메인 파티 인원 확장',
        stages: [
            { stage: 1,  name: '서브 슬롯 +1',        desc: '총 2슬롯', effect: { subSlotsBonus: 1 } },
            { stage: 2,  name: '서브 슬롯 +1',        desc: '총 3슬롯', effect: { subSlotsBonus: 2 } },
            { stage: 3,  name: '메인 파티 +1',        desc: '3→4명',   effect: { mainPartyBonus: 1 } },
            { stage: 4,  name: '서브 슬롯 +1',        desc: '총 4슬롯', effect: { subSlotsBonus: 3 } },
            { stage: 5,  name: '서브 슬롯 +1',        desc: '총 5슬롯', effect: { subSlotsBonus: 4 } },
            { stage: 6,  name: '메인 파티 +1',        desc: '4→5명',   effect: { mainPartyBonus: 2 } },
            { stage: 7,  name: '동일구역 중복 파견', desc: '같은 곳 2팀', effect: { duplicateDispatch: 1 } },
            { stage: 8,  name: '서브 슬롯 +1',        desc: '총 6슬롯', effect: { subSlotsBonus: 5 } },
            { stage: 9,  name: '서브 슬롯 +1',        desc: '총 7슬롯', effect: { subSlotsBonus: 6 } },
            { stage: 10, name: '메인 파티 +1',        desc: '5→6명',   effect: { mainPartyBonus: 3 } },
            { stage: 11, name: '동일구역 +1팀 / 슬롯 +1', desc: '3팀, 총 8슬롯', effect: { subSlotsBonus: 7, duplicateDispatch: 2 } },
            { stage: 12, name: '🏆 총사령관',         desc: '교체 가능 + 슬롯 10', effect: { subSlotsBonus: 9, duplicateDispatch: 3, swapDuringRun: true } }
        ]
    },
    infrastructure: {
        key: 'infrastructure',
        name: '인프라',
        icon: '🏗',
        color: 0xaa8844,
        textColor: '#ffcc88',
        baseCost: 400,
        unlockCondition: 'always',
        desc: '로스터, 보관함, 보안 컨테이너',
        stages: [
            { stage: 1,  name: '로스터 +2',         desc: '4→6',  effect: { rosterBonus: 2 } },
            { stage: 2,  name: '보관함 +4',         desc: '12→16', effect: { storageBonus: 4 } },
            { stage: 3,  name: '로스터 +2',         desc: '6→8',  effect: { rosterBonus: 4 } },
            { stage: 4,  name: '보안 컨테이너 +1', desc: '2→3',   effect: { secureBonus: 1 } },
            { stage: 5,  name: '보관함 +6',         desc: '16→22', effect: { storageBonus: 10 } },
            { stage: 6,  name: '로스터 +3',         desc: '8→11', effect: { rosterBonus: 7 } },
            { stage: 7,  name: '보관함 탭 분리',    desc: '장비/소재/소비 분리', effect: { storageTabs: true } },
            { stage: 8,  name: '보안 컨테이너 +2', desc: '3→5',   effect: { secureBonus: 3 } },
            { stage: 9,  name: '로스터 +4',         desc: '11→15', effect: { rosterBonus: 11 } },
            { stage: 10, name: '보관함 +8 / 장비전용',desc: '+20 장비창', effect: { storageBonus: 18, equipStorage: 20 } },
            { stage: 11, name: '사망 인벤 80%',     desc: '50→80%', effect: { fallenItemRecovery: 0.8 } },
            { stage: 12, name: '🏆 차원 창고',      desc: '+30 / 보안 무제한', effect: { storageBonus: 48, secureBonus: 999 } }
        ]
    },
    recovery: {
        key: 'recovery',
        name: '휴식',
        icon: '🛌',
        color: 0x44cc88,
        textColor: '#88ffaa',
        baseCost: 250,
        unlockCondition: 'always',
        desc: '스테미너 회복, 부상, 사망 구제',
        stages: [
            { stage: 1,  name: '대기 스테미너 +5',  desc: '복귀 시 +5 추가', effect: { restBonus: 5 } },
            { stage: 2,  name: '대기 스테미너 +5',  desc: '+10 추가',         effect: { restBonus: 10 } },
            { stage: 3,  name: '부상 회복 -30%',    desc: '시간 단축',         effect: { injuryRecovery: 0.3 } },
            { stage: 4,  name: '파견 페널티 -10',   desc: '서브 후 스테미너 회복', effect: { dispatchStaminaRecovery: 10 } },
            { stage: 5,  name: '대기 스테미너 +10', desc: '+20 추가',         effect: { restBonus: 20 } },
            { stage: 6,  name: '사망 위기 자동생존', desc: '1회/5런',          effect: { autoSurvive: 1, autoSurviveCooldown: 5 } },
            { stage: 7,  name: '복귀 시 +20 즉시',  desc: '귀환자 회복',        effect: { returnStaminaBonus: 20 } },
            { stage: 8,  name: '대기 스테미너 +15', desc: '+35 추가',         effect: { restBonus: 35 } },
            { stage: 9,  name: '부상 회복 -50%',    desc: '누적 -80%',         effect: { injuryRecovery: 0.8 } },
            { stage: 10, name: '자동생존 쿨다운 2런', desc: '5→2런',           effect: { autoSurviveCooldown: 2 } },
            { stage: 11, name: '부상자 서브 가능',  desc: '보상 50%',          effect: { injuredCanDispatch: true } },
            { stage: 12, name: '🏆 불사의 길드',    desc: '자동생존 2회 + 즉시 풀회복', effect: { autoSurvive: 2, instantStaminaRecovery: true } }
        ]
    },
    automation: {
        key: 'automation',
        name: '자동화',
        icon: '🤖',
        color: 0x88aaaa,
        textColor: '#aaccdd',
        baseCost: 500,
        unlockCondition: 'always',
        desc: '자동 장착/판매/회수/재파견',
        stages: [
            { stage: 1,  name: '자동 장착',         desc: '수동 버튼',          effect: { autoEquip: true } },
            { stage: 2,  name: '자동 판매 (common)',desc: 'common 잡템',       effect: { autoSell: 1 } },
            { stage: 3,  name: '신규 장비 자동 착용',desc: '획득 즉시',         effect: { autoEquipOnPickup: true } },
            { stage: 4,  name: '자동 판매 (uncommon)', desc: 'uncommon까지',   effect: { autoSell: 2 } },
            { stage: 5,  name: '파견 자동 최적화', desc: '출발 시',           effect: { autoOptimizeDispatch: true } },
            { stage: 6,  name: '자동 회수',         desc: '귀환 즉시 보상',     effect: { autoCollect: true } },
            { stage: 7,  name: '자동 재파견',       desc: '같은 구역',          effect: { autoRedispatch: true } },
            { stage: 8,  name: '자동 판매 (rare)',  desc: '중복 한정',          effect: { autoSell: 3 } },
            { stage: 9,  name: '자동 위탁',         desc: '잡템 위탁',          effect: { autoConsign: true } },
            { stage: 10, name: '자동 치유/스테미너', desc: '관리',              effect: { autoHealStamina: true } },
            { stage: 11, name: '자동 모집',         desc: '조건 매칭',          effect: { autoRecruit: true } },
            { stage: 12, name: '🏆 완전 자동화',    desc: '풀 자동 마을',       effect: { fullAuto: true } }
        ]
    },
    intel: {
        key: 'intel',
        name: '정보',
        icon: '🔍',
        color: 0xcc88cc,
        textColor: '#ddaaee',
        baseCost: 600,
        unlockCondition: 'always',
        desc: '미리보기, 소문, 평판',
        stages: [
            { stage: 1,  name: '파견 결과 미리보기', desc: '예상 보상/위험',    effect: { dispatchPreview: true } },
            { stage: 2,  name: '소문 슬롯 +1',      desc: '1→2',               effect: { rumorSlots: 1 } },
            { stage: 3,  name: 'NPC 매물 -30% 시간', desc: '갱신 가속',         effect: { npcStockSpeed: 0.3 } },
            { stage: 4,  name: '적 능력치 표시',    desc: '약점 노출',          effect: { showEnemyStats: true } },
            { stage: 5,  name: '이벤트 우선순위',   desc: '우선 알림',          effect: { eventPriority: true } },
            { stage: 6,  name: '소문 슬롯 +1',      desc: '→3',                effect: { rumorSlots: 2 } },
            { stage: 7,  name: '평판 +30%',         desc: '누적 가속',          effect: { reputationGain: 1.3 } },
            { stage: 8,  name: 'NPC 호감도 +25%',   desc: '누적 가속',          effect: { npcAffinityGain: 1.25 } },
            { stage: 9,  name: '소문 슬롯 +1',      desc: '→4',                effect: { rumorSlots: 3 } },
            { stage: 10, name: '시장 트렌드 예측', desc: '다음 변동',          effect: { marketForecast: true } },
            { stage: 11, name: '특성 위험 표시',   desc: '음성 표기',          effect: { showTraitWarning: true } },
            { stage: 12, name: '🏆 전능의 시야',    desc: '모든 정보 + 소문 6', effect: { rumorSlots: 5, omniscient: true } }
        ]
    },
    pit_control: {
        key: 'pit_control',
        name: '핏 통제',
        icon: '💀',
        color: 0xcc4444,
        textColor: '#ff8888',
        baseCost: 800,
        unlockCondition: 'zone_clear:bloodpit',
        desc: '[BP 첫 클리어] 핏 게이지 메커니즘',
        stages: [
            { stage: 1,  name: '핏 게이지 최대 +50%', desc: 'MAX 증가',          effect: { pitMaxBonus: 0.5 } },
            { stage: 2,  name: 'MAX 드롭률 +15%',     desc: '+20→+35%',          effect: { pitMaxDropBonus: 0.15 } },
            { stage: 3,  name: '게이지 감소 -30%',    desc: '유지 강화',         effect: { pitDecayReduction: 0.3 } },
            { stage: 4,  name: '엘리트 처치 +50%',    desc: '즉시 충전',         effect: { eliteKillCharge: 0.5 } },
            { stage: 5,  name: '핏 오버플로우',       desc: 'MAX 초과 → 골드 +1%/초', effect: { pitOverflowGold: true } },
            { stage: 6,  name: 'BP 라운드 회복 +10%', desc: 'HP 회복',           effect: { bpRoundHeal: 0.1 } },
            { stage: 7,  name: '엘리트 확률 2배 + 드롭+1', desc: '강화',         effect: { eliteRateBonus: 2, eliteDropBonus: 1 } },
            { stage: 8,  name: '핏 연쇄',             desc: '처치 시 인접 출혈', effect: { pitChainBleed: true } },
            { stage: 9,  name: 'BP 보스 사전 충전',   desc: '50% 시작',          effect: { bpBossPreCharge: 0.5 } },
            { stage: 10, name: '광전사의 전장',       desc: 'MAX 시 ATK+25%/DEF-15%', effect: { berserkerField: true } },
            { stage: 11, name: 'BP 보스 전설 +15%',   desc: '드롭 강화',         effect: { bpBossLegendary: 0.15 } },
            { stage: 12, name: '🏆 피의 군주',         desc: '게이지 영구 MAX + 보스 확정 전설', effect: { pitLordPermaMax: true } }
        ]
    },
    cargo_control: {
        key: 'cargo_control',
        name: '화물 통제',
        icon: '🚂',
        color: 0xaa8855,
        textColor: '#ffcc99',
        baseCost: 1000,
        unlockCondition: 'zone_clear:cargo',
        desc: '[Cargo 첫 클리어] 열차 칸 시스템',
        stages: [
            { stage: 1,  name: '열차 칸 HP +25%',     desc: '강화',              effect: { trainCarHpBonus: 0.25 } },
            { stage: 2,  name: '크레이트 자석',       desc: '아군 가까이',       effect: { crateMagnet: true } },
            { stage: 3,  name: '역 수리 2배',         desc: '정차 시',           effect: { stationRepairBonus: 2 } },
            { stage: 4,  name: '6번째 칸: 무기고',    desc: 'ATK +10%',          effect: { car6Armory: true } },
            { stage: 5,  name: '침입자 사전 경고',    desc: '위치 표시',         effect: { intruderPreview: true } },
            { stage: 6,  name: '폭풍 차폐',           desc: '데미지 -50%',       effect: { stormShield: 0.5 } },
            { stage: 7,  name: '자동 수리 1회',       desc: 'HP0→30%',           effect: { autoRepair: 1 } },
            { stage: 8,  name: '화물 보너스 2배',     desc: 'HP×2',              effect: { cargoHpDouble: true } },
            { stage: 9,  name: '역 상인 출현',        desc: '랜덤 할인',         effect: { stationMerchant: true } },
            { stage: 10, name: '7번째 칸: 통신실',    desc: '보스 약점',         effect: { car7Comms: true } },
            { stage: 11, name: '침입자 자동 처치',    desc: '칸 손실 없음',      effect: { autoKillIntruder: true } },
            { stage: 12, name: '🏆 황금 열차',         desc: '보상×2 + 칸 무적', effect: { goldenTrain: true } }
        ]
    },
    dark_control: {
        key: 'dark_control',
        name: '어둠 통제',
        icon: '🕯',
        color: 0x8866cc,
        textColor: '#bb99ee',
        baseCost: 1200,
        unlockCondition: 'zone_clear:blackout',
        desc: '[BO 첫 클리어] 저주 트레이드오프',
        stages: [
            { stage: 1,  name: '저주 상승 -25%',      desc: '속도 감소',         effect: { curseRateReduction: 0.25 } },
            { stage: 2,  name: '저주 시야',           desc: '인접 방 미리보기',  effect: { curseVision: true } },
            { stage: 3,  name: '비밀방 +5%',          desc: '확률',              effect: { secretRoomBonus: 0.05 } },
            { stage: 4,  name: '저주 흡수: ATK +3%/Lv', desc: '양날의 검',       effect: { curseAbsorb: 0.03 } },
            { stage: 5,  name: '함정 -40% + 반사 20%', desc: '데미지 경감',      effect: { trapDmgReduction: 0.4, trapReflect: 0.2 } },
            { stage: 6,  name: '안전한 후퇴',         desc: '취소 2회/층',       effect: { safeRetreat: 2 } },
            { stage: 7,  name: '저주 장비 -30%',      desc: '음성 효과',         effect: { cursedItemReduction: 0.3 } },
            { stage: 8,  name: '정화의 샘',           desc: '3층마다 음성 제거', effect: { purificationSpring: true } },
            { stage: 9,  name: '보스 단서 2배',       desc: '약점 발견',         effect: { bossClueBonus: 2 } },
            { stage: 10, name: '이중 탐색',           desc: '같은 층 2갈래',     effect: { dualExplore: true } },
            { stage: 11, name: '저주 Lv7+ 동족상잔', desc: '20% 확률',           effect: { curseInfighting: 0.2 } },
            { stage: 12, name: '🏆 그림자 군주',       desc: '저주→양성 전환 + 비밀방 3배', effect: { shadowLord: true } }
        ]
    }
};

const GUILD_HALL_KEYS = Object.keys(GUILD_HALL_DATA);

/** 단계별 비용 = round(baseCost × 1.5^(stage-1)) */
function getGuildHallStageCost(category, stage) {
    const data = GUILD_HALL_DATA[category];
    if (!data) return 0;
    return Math.round(data.baseCost * Math.pow(1.5, stage - 1));
}

/** 게이트 조건 — 단계별 길드 평판 / 길드 Lv 요구 */
function getGuildHallStageGate(stage) {
    if (stage <= 3) return { rep: 0,  guildLv: 1 };
    if (stage <= 6) return { rep: 0,  guildLv: 2 };
    if (stage <= 9) return { rep: 15, guildLv: 4 };
    return                  { rep: 35, guildLv: 6 };
}

/** 카테고리 해금 여부 */
function isGuildHallUnlocked(gs, categoryKey) {
    const data = GUILD_HALL_DATA[categoryKey];
    if (!data) return false;
    if (data.unlockCondition === 'always') return true;
    if (typeof data.unlockCondition === 'string' && data.unlockCondition.startsWith('zone_clear:')) {
        const zone = data.unlockCondition.split(':')[1];
        // 해당 zone Lv1 클리어 누적이 있으면 해금
        if (!gs.zoneClearCount) return false;
        return (gs.zoneClearCount[`${zone}_1`] || 0) > 0;
    }
    return false;
}
