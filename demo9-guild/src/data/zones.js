const ZONE_DATA = {
    bloodpit: {
        name: 'Blood Pit',
        subtitle: '투기장',
        desc: '욕심 vs 탈출 — 핏 게이지가 핵심',
        icon: '💀',
        color: 0xff2244,
        textColor: '#ff2244',
        unlockLevel: 1,
        baseGoldReward: 60,
        baseXpReward: 30,
        specialMaterial: '혈정석',
        deathChance: 0.12,
        lootCount: { min: 1, max: 3 }
    },
    cargo: {
        name: 'Cargo',
        subtitle: '화물선',
        desc: '전략적 거점 방어 — 칸 카드 운영',
        icon: '🚂',
        color: 0xff8844,
        textColor: '#ff8844',
        unlockLevel: 3,
        baseGoldReward: 80,
        baseXpReward: 35,
        specialMaterial: '마법 동력석',
        deathChance: 0.10,
        lootCount: { min: 2, max: 4 }
    },
    blackout: {
        name: 'Blackout',
        subtitle: '저주받은 저택',
        desc: '탐색 기습형 — 저주 레벨 딜레마',
        icon: '🔦',
        color: 0x8844ff,
        textColor: '#8844ff',
        unlockLevel: 5,
        baseGoldReward: 100,
        baseXpReward: 40,
        specialMaterial: '저주 유물',
        deathChance: 0.18,
        lootCount: { min: 1, max: 5 }
    }
};

const ZONE_KEYS = Object.keys(ZONE_DATA);

function getAffinityXpNeeded(level) {
    return 30 + level * 20;
}

const AFFINITY_TREES = {
    bloodpit: {
        name: 'Blood Pit 친화도',
        icon: '💀',
        color: '#ff2244',
        nodes: {
            bp_1: { level: 1, name: '피의 각성', desc: 'Blood Pit 입장 시 전체 ATK +10%', effect: { stat: 'atk', mult: 0.10 }, requires: [], x: 0, y: 0 },
            bp_2: { level: 2, name: '투기장 적응', desc: '핏 게이지 상승속도 +20%', effect: { special: 'pitGaugeSpeed', value: 0.20 }, requires: ['bp_1'], x: 0, y: 1 },
            bp_3a: { level: 3, name: '황금 핏 (파밍)', desc: '핏게이지 MAX 시 드랍 3배', effect: { special: 'pitGaugeDrop', value: 3 }, requires: ['bp_2'], x: -1, y: 2, branch: 'A' },
            bp_3b: { level: 3, name: '광전사의 기운 (전투)', desc: '라운드 시작 시 전체 ATK +15%', effect: { stat: 'atk', mult: 0.15 }, requires: ['bp_2'], x: 1, y: 2, branch: 'B' },
            bp_4a: { level: 4, name: '핏로드의 눈', desc: '핏로드 약점 노출 (피해 +25%)', effect: { special: 'bossWeakness', value: 0.25 }, requires: ['bp_3a'], x: -1, y: 3, branch: 'A' },
            bp_4b: { level: 4, name: '전설의 사냥꾼', desc: '보스 처치 시 전설 드랍 확률 +10%', effect: { special: 'legendaryDrop', value: 0.10 }, requires: ['bp_3b'], x: 1, y: 3, branch: 'B' },
            bp_5a: { level: 5, name: '피의 군주', desc: '전체 스탯 +8%, 핏게이지 감소 없음', effect: { stat: 'all', mult: 0.08, special: 'noGaugeDecay' }, requires: ['bp_4a'], x: -1, y: 4, branch: 'A' },
            bp_5b: { level: 5, name: '학살의 화신', desc: '처치 시 ATK +5% 중첩 (최대 5)', effect: { special: 'killStack', value: 0.05, max: 5 }, requires: ['bp_4b'], x: 1, y: 4, branch: 'B' }
        }
    },
    cargo: {
        name: 'Cargo 친화도',
        icon: '🚂',
        color: '#ff8844',
        nodes: {
            cg_1: { level: 1, name: '화물선 적응', desc: 'Cargo 입장 시 칸 HP +20%', effect: { special: 'carHp', value: 0.20 }, requires: [], x: 0, y: 0 },
            cg_2a: { level: 2, name: '칸 전문가 (운영)', desc: '칸 패시브 효과 +30%', effect: { special: 'carPassive', value: 0.30 }, requires: ['cg_1'], x: -1, y: 1, branch: 'A' },
            cg_2b: { level: 2, name: '침입 예측 (전투)', desc: '웨이브 시작 전 침입 방향 미리 공개', effect: { special: 'invasionPreview' }, requires: ['cg_1'], x: 0, y: 1, branch: 'B' },
            cg_2c: { level: 2, name: '긴급 수리 (특수)', desc: '역 정차 시 파괴 칸 수리 가능', effect: { special: 'repairDestroyed' }, requires: ['cg_1'], x: 1, y: 1, branch: 'C' },
            cg_3a: { level: 3, name: '무상 수리', desc: '칸 수리 비용 무료', effect: { special: 'freeRepair' }, requires: ['cg_2a'], x: -1, y: 2, branch: 'A' },
            cg_3b: { level: 3, name: '카드 전문가', desc: '카드 보유 한도 +2 (최대 7장)', effect: { special: 'cardLimit', value: 2 }, requires: ['cg_2b'], x: 0, y: 2, branch: 'B' },
            cg_3c: { level: 3, name: '추가 정차', desc: '런 중 역 정차 1회 추가', effect: { special: 'extraStop', value: 1 }, requires: ['cg_2c'], x: 1, y: 2, branch: 'C' },
            cg_4a: { level: 4, name: '강화 화물선', desc: '모든 칸 패시브 효과 2배', effect: { special: 'carPassiveDouble' }, requires: ['cg_3a'], x: -1, y: 3, branch: 'A' },
            cg_4b: { level: 4, name: '방어 전문가', desc: '웨이브 적 ATK -15%', effect: { special: 'enemyAtkDebuff', value: 0.15 }, requires: ['cg_3b'], x: 0, y: 3, branch: 'B' },
            cg_4c: { level: 4, name: '불사 화물선', desc: '파괴된 칸 1개 런 중 1회 부활', effect: { special: 'reviveCar', value: 1 }, requires: ['cg_3c'], x: 1, y: 3, branch: 'C' }
        }
    },
    blackout: {
        name: 'Blackout 친화도',
        icon: '🔦',
        color: '#8844ff',
        nodes: {
            bo_1: { level: 1, name: '어둠 적응', desc: '안개 제거 범위 +1칸', effect: { special: 'fogRange', value: 1 }, requires: [], x: 0, y: 0 },
            bo_2a: { level: 2, name: '저택의 기억 (안전)', desc: '이미 간 방 재진입 시 전투 없음', effect: { special: 'noRevisitCombat' }, requires: ['bo_1'], x: -1, y: 1, branch: 'A' },
            bo_2b: { level: 2, name: '저주 흡수 (저주)', desc: '저주 레벨 높을수록 ATK +5% 중첩', effect: { special: 'curseAtk', value: 0.05 }, requires: ['bo_1'], x: 1, y: 1, branch: 'B' },
            bo_3aa: { level: 3, name: '저택의 눈', desc: '인접 방 타입 사전 공개', effect: { special: 'roomPreview' }, requires: ['bo_2a'], x: -2, y: 2, branch: 'A' },
            bo_3ab: { level: 3, name: '그림자 발걸음', desc: '기습 방 선제 피해 무효', effect: { special: 'noAmbush' }, requires: ['bo_2a'], x: -0.5, y: 2, branch: 'A' },
            bo_3ba: { level: 3, name: '저주의 화신', desc: '저주 Lv 5 이상 시 전체 스탯 +20%', effect: { special: 'curseBoost', value: 0.20, threshold: 5 }, requires: ['bo_2b'], x: 0.5, y: 2, branch: 'B' },
            bo_3bb: { level: 3, name: '어둠의 수확', desc: '저주 레벨 높을수록 드랍 희귀도 상승', effect: { special: 'curseLoot' }, requires: ['bo_2b'], x: 2, y: 2, branch: 'B' },
            bo_4a: { level: 4, name: '완벽한 기억', desc: '탐색한 방 전투/함정 완전 면역', effect: { special: 'fullImmunity' }, requires: ['bo_3aa', 'bo_3ab'], x: -1, y: 3, branch: 'A' },
            bo_4b: { level: 4, name: '저주의 지배자', desc: '저주 효과 역전 — 버프로 전환', effect: { special: 'curseReverse' }, requires: ['bo_3ba', 'bo_3bb'], x: 1, y: 3, branch: 'B' }
        }
    }
};
