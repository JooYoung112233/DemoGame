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
