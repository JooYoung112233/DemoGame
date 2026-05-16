const ITEM_RARITY = {
    common:   { name: '일반', color: 0x888888, textColor: '#888888', valueMult: 1.0,  statMult: 1.0 },
    uncommon: { name: '고급', color: 0x44cc44, textColor: '#44cc44', valueMult: 1.5,  statMult: 1.4 },
    rare:     { name: '희귀', color: 0x4488ff, textColor: '#4488ff', valueMult: 2.5,  statMult: 2.0 },
    epic:     { name: '에픽', color: 0xcc44ff, textColor: '#cc44ff', valueMult: 5.0,  statMult: 3.0 },
    legendary:{ name: '전설', color: 0xffaa00, textColor: '#ffaa00', valueMult: 10.0, statMult: 4.5 }
};

// ═══════════════════════════════════════════════
//  일반 장비 — 모든 희귀도에서 드롭 가능
//  같은 "낡은 검"이라도 epic이면 스탯이 3배
// ═══════════════════════════════════════════════

const EQUIPMENT_TEMPLATES = {
    weapon: [
        // === Tier 1 — 초반 (common~uncommon 위주) ===
        { id: 'w01', name: '녹슨 검',        baseAtk: 5,  tier: 1, flavor: '언젠간 쓸모있었던 검' },
        { id: 'w02', name: '나무 지팡이',    baseAtk: 4,  tier: 1, baseCrit: 0.02, flavor: '마력이 희미하게 깃들어 있다' },
        { id: 'w03', name: '낡은 단궁',      baseAtk: 4,  tier: 1, baseSpd: 5, flavor: '줄이 느슨하다' },
        { id: 'w04', name: '부엌칼',         baseAtk: 6,  tier: 1, flavor: '본래 용도와 다르게 쓰이는 중' },
        // === Tier 2 — 중반 (uncommon~rare 위주) ===
        { id: 'w05', name: '강철 검',        baseAtk: 8,  tier: 2, flavor: '대장장이의 표준 작품' },
        { id: 'w06', name: '마력 지팡이',    baseAtk: 7,  tier: 2, baseCrit: 0.04, flavor: '동력석이 박혀 은은히 빛난다' },
        { id: 'w07', name: '사냥꾼의 활',    baseAtk: 7,  tier: 2, baseSpd: 8, flavor: '먼 거리에서도 정확하다' },
        { id: 'w08', name: '전투 도끼',      baseAtk: 10, tier: 2, flavor: '무겁지만 한 방이 묵직하다' },
        { id: 'w09', name: '의식용 단검',    baseAtk: 6,  tier: 2, baseCrit: 0.06, flavor: '이상한 문양이 새겨져 있다' },
        { id: 'w10', name: '미스릴 레이피어', baseAtk: 7, tier: 2, baseSpd: 10, baseCrit: 0.03, flavor: '가볍고 날카롭다' },
        // === Tier 3 — 후반 (rare~epic 위주) ===
        { id: 'w11', name: '흑요석 대검',    baseAtk: 14, tier: 3, flavor: '화산에서 단조된 검은 칼날' },
        { id: 'w12', name: '번개 지팡이',    baseAtk: 11, tier: 3, baseCrit: 0.06, baseSpd: 5, flavor: '손끝에서 정전기가 튄다' },
        { id: 'w13', name: '뼈 장궁',        baseAtk: 12, tier: 3, baseSpd: 8, baseCrit: 0.04, flavor: '마수의 뼈로 만든 활' },
        { id: 'w14', name: '성스러운 메이스', baseAtk: 10, tier: 3, baseHp: 20, flavor: '치유와 파괴를 겸한다' },
        { id: 'w15', name: '그림자 검',      baseAtk: 11, tier: 3, baseSpd: 12, flavor: '칼날이 그림자처럼 흔들린다' },
        { id: 'w16', name: '용암 도끼',      baseAtk: 16, tier: 3, flavor: '잡으면 뜨겁다 — 진짜로' },
        // === Tier 4 — 최후반 (epic~legendary) ===
        { id: 'w17', name: '천공의 검',      baseAtk: 13, tier: 4, baseCrit: 0.08, baseSpd: 8, flavor: '하늘의 별빛이 칼날에 응축되어 있다' },
        { id: 'w18', name: '파멸의 지팡이',  baseAtk: 15, tier: 4, baseCrit: 0.07, flavor: '마력이 넘쳐 주변 공기가 왜곡된다' },
        { id: 'w19', name: '영혼 갈취자',    baseAtk: 18, tier: 4, baseCrit: 0.05, flavor: '적의 영혼을 깎아먹는 무기' },
        { id: 'w20', name: '심판자의 창',    baseAtk: 15, tier: 4, baseHp: 15, baseCrit: 0.04, flavor: '죄인에게 내리는 심판' }
    ],
    armor: [
        // === Tier 1 ===
        { id: 'a01', name: '누더기 옷',      baseDef: 2,  baseHp: 10, tier: 1, flavor: '없는 것보다야 낫다' },
        { id: 'a02', name: '가죽 갑옷',      baseDef: 4,  baseHp: 15, tier: 1, flavor: '부드럽고 가볍다' },
        { id: 'a03', name: '천 로브',         baseDef: 2,  baseHp: 8,  tier: 1, baseAtk: 2, flavor: '마력 전도율이 좋다' },
        // === Tier 2 ===
        { id: 'a04', name: '사슬 갑옷',      baseDef: 7,  baseHp: 25, tier: 2, flavor: '칼날은 막지만 둔기는...' },
        { id: 'a05', name: '마법사 로브',    baseDef: 4,  baseHp: 12, tier: 2, baseAtk: 4, flavor: '마력 집속 직물로 짠 로브' },
        { id: 'a06', name: '정찰 조끼',      baseDef: 5,  baseHp: 18, tier: 2, baseSpd: 8, flavor: '가볍고 움직이기 편하다' },
        { id: 'a07', name: '강화 가죽',      baseDef: 6,  baseHp: 22, tier: 2, baseCrit: 0.02, flavor: '마수 가죽으로 보강됨' },
        { id: 'a08', name: '수도사 법의',    baseDef: 5,  baseHp: 20, tier: 2, baseAtk: 2, baseSpd: 5, flavor: '기도와 수련의 옷' },
        // === Tier 3 ===
        { id: 'a09', name: '판금 갑옷',      baseDef: 12, baseHp: 45, tier: 3, flavor: '무겁지만 확실한 방호' },
        { id: 'a10', name: '뼈 갑옷',        baseDef: 9,  baseHp: 35, tier: 3, baseCrit: 0.03, flavor: '마수의 뼈를 이어붙인 갑옷' },
        { id: 'a11', name: '바람의 망토',    baseDef: 6,  baseHp: 20, tier: 3, baseSpd: 15, flavor: '바람처럼 빠르게 움직인다' },
        { id: 'a12', name: '현자의 로브',    baseDef: 5,  baseHp: 15, tier: 3, baseAtk: 7, baseCrit: 0.04, flavor: '고대 마법이 직조되어 있다' },
        // === Tier 4 ===
        { id: 'a13', name: '용비늘 갑옷',    baseDef: 16, baseHp: 60, tier: 4, flavor: '용의 비늘로 만든 최강의 방어구' },
        { id: 'a14', name: '성기사 전투복',  baseDef: 12, baseHp: 40, tier: 4, baseAtk: 5, flavor: '공방 균형의 극치' },
        { id: 'a15', name: '차원 로브',      baseDef: 8,  baseHp: 25, tier: 4, baseAtk: 10, baseCrit: 0.05, flavor: '다른 차원의 힘이 깃들어 있다' }
    ],
    accessory: [
        // === Tier 1 ===
        { id: 'x01', name: '구리 반지',      tier: 1, baseAtk: 2, flavor: '초라하지만 시작점' },
        { id: 'x02', name: '행운 동전',      tier: 1, baseCrit: 0.03, flavor: '주머니에 넣으면 운이 좋아진다나' },
        { id: 'x03', name: '가죽 팔찌',      tier: 1, baseDef: 2, baseHp: 10, flavor: '기본적인 보호' },
        // === Tier 2 ===
        { id: 'x04', name: '힘의 목걸이',    tier: 2, baseAtk: 5, flavor: '착용자에게 힘이 솟는다' },
        { id: 'x05', name: '수호의 부적',    tier: 2, baseDef: 4, baseHp: 20, flavor: '위험을 막아주는 부적' },
        { id: 'x06', name: '속도의 장갑',    tier: 2, baseSpd: 12, flavor: '손놀림이 빨라진다' },
        { id: 'x07', name: '행운의 반지',    tier: 2, baseCrit: 0.05, flavor: '운 좋은 일격을 부른다' },
        { id: 'x08', name: '집중의 귀걸이',  tier: 2, baseAtk: 3, baseCrit: 0.03, flavor: '정신을 집중시킨다' },
        // === Tier 3 ===
        { id: 'x09', name: '핏빛 보석',      tier: 3, baseAtk: 4, baseCrit: 0.06, flavor: '피의 힘이 응축되어 있다' },
        { id: 'x10', name: '강철 팔찌',      tier: 3, baseDef: 6, baseHp: 30, flavor: '단단한 보호' },
        { id: 'x11', name: '질풍 부츠',      tier: 3, baseSpd: 18, baseCrit: 0.03, flavor: '바람보다 빠르게' },
        { id: 'x12', name: '현자의 돋보기',  tier: 3, baseAtk: 6, baseCrit: 0.05, flavor: '약점이 보인다' },
        // === Tier 4 ===
        { id: 'x13', name: '용의 눈 반지',   tier: 4, baseAtk: 8, baseCrit: 0.08, flavor: '용의 시야로 적을 꿰뚫는다' },
        { id: 'x14', name: '불멸의 팬던트',  tier: 4, baseDef: 8, baseHp: 45, flavor: '죽음을 거부하는 힘' },
        { id: 'x15', name: '시간의 모래시계',tier: 4, baseAtk: 5, baseSpd: 15, baseCrit: 0.05, flavor: '시간이 느려진다' }
    ]
};

// ═══════════════════════════════════════════════
//  구역 특수 장비 — 강한 트레이드오프, 세분화
//  각 구역 × 각 슬롯 × 2~3종 변형
// ═══════════════════════════════════════════════

const ZONE_EQUIPMENT = {
    bloodpit: {
        weapon: [
            {
                id: 'zbw01', name: '피에 물든 철검', minRarity: 'uncommon',
                stats: { atk: 12 }, penalty: { def: -4 },
                special: 'bleed', specialDesc: '공격 시 출혈 DoT (3초간 ATK×15% 피해)',
                zoneBonus: 'BP에서 출혈 +50%', flavor: '칼끝에서 끊이지 않는 핏방울'
            },
            {
                id: 'zbw02', name: '광기의 전투도끼', minRarity: 'rare',
                stats: { atk: 16 }, penalty: { hp: -25 },
                special: 'frenzy', specialDesc: 'HP 50% 이하 시 ATK +35%',
                zoneBonus: 'BP에서 광기 ATK +50%', flavor: '들수록 정신이 흐려진다'
            },
            {
                id: 'zbw03', name: '핏로드의 이빨',  minRarity: 'epic',
                stats: { atk: 22, critRate: 0.08 }, penalty: { def: -6, hp: -15 },
                special: 'lifesteal_bleed', specialDesc: '출혈 적 공격 시 HP 흡수 25%',
                zoneBonus: 'BP에서 흡수 25%→40%', flavor: '보스의 송곳니를 재가공한 검'
            }
        ],
        armor: [
            {
                id: 'zba01', name: '피갑옷', minRarity: 'uncommon',
                stats: { def: 8, hp: 30 }, penalty: { spd: -8 },
                special: 'bleed_reflect', specialDesc: '피격 시 10% 확률로 공격자에게 출혈',
                zoneBonus: 'BP에서 반사 확률 +10%', flavor: '피를 머금은 갑옷이 적을 물든다'
            },
            {
                id: 'zba02', name: '투기장 전사의 흉갑', minRarity: 'rare',
                stats: { def: 10, hp: 40, atk: 5 }, penalty: { spd: -12 },
                special: 'pit_endure', specialDesc: '핏 게이지 MAX 동안 받는 피해 -20%',
                zoneBonus: 'BP에서 감소량 -30%', flavor: '투기장 챔피언이 입었던 갑옷'
            },
            {
                id: 'zba03', name: '핏로드의 심장 갑옷', minRarity: 'epic',
                stats: { def: 14, hp: 60, atk: 8 }, penalty: { spd: -15 },
                special: 'blood_pulse', specialDesc: '매 5초마다 주변 적에게 출혈 자동 부여',
                zoneBonus: 'BP에서 간격 5초→3초', flavor: '심장이 아직 뛰고 있다'
            }
        ],
        accessory: [
            {
                id: 'zbx01', name: '핏빛 인장 반지', minRarity: 'uncommon',
                stats: { atk: 5, critRate: 0.05 }, penalty: { hp: -15 },
                special: 'bleed_bonus', specialDesc: '출혈 중인 적에게 피해 +20%',
                zoneBonus: 'BP에서 +20%→+35%', flavor: '피로 맺은 계약의 증표'
            },
            {
                id: 'zbx02', name: '투기장 관중의 함성', minRarity: 'rare',
                stats: { atk: 8, critRate: 0.06 }, penalty: { def: -3 },
                special: 'kill_momentum', specialDesc: '적 처치 시 ATK +5% (최대 25%)',
                zoneBonus: 'BP에서 최대 +35%', flavor: '환호성이 힘을 준다'
            },
            {
                id: 'zbx03', name: '핏로드의 왕관 파편', minRarity: 'epic',
                stats: { atk: 12, critRate: 0.10 }, penalty: { hp: -30, def: -4 },
                special: 'blood_lord_fragment', specialDesc: '처치 시 HP 10% 회복 + 출혈 강화',
                zoneBonus: 'BP에서 회복 10%→20%', flavor: '왕의 파편이 피를 부른다'
            }
        ]
    },
    cargo: {
        weapon: [
            {
                id: 'zcw01', name: '증기 해머', minRarity: 'uncommon',
                stats: { atk: 11 }, penalty: { spd: -6 },
                special: 'overheat', specialDesc: '공격 3회 누적→4번째 폭발 (ATK×180%)',
                zoneBonus: 'Cargo에서 폭발 +50%', flavor: '내부 보일러가 과열된다'
            },
            {
                id: 'zcw02', name: '증기 기관총', minRarity: 'rare',
                stats: { atk: 9, spd: 10 }, penalty: { def: -4 },
                special: 'rapidfire', specialDesc: '공격속도 +25%, 매 5번째 공격 2연타',
                zoneBonus: 'Cargo에서 2연타→3연타', flavor: '쉴 새 없이 탄환을 뿜는다'
            },
            {
                id: 'zcw03', name: '화물 제독의 포', minRarity: 'epic',
                stats: { atk: 20, critRate: 0.06 }, penalty: { spd: -12 },
                special: 'cannon_blast', specialDesc: '과열 폭발 시 인접 적 2명 추가 피해',
                zoneBonus: 'Cargo에서 범위 +1명', flavor: '한 발이면 전열이 무너진다'
            }
        ],
        armor: [
            {
                id: 'zca01', name: '증기 갑옷', minRarity: 'uncommon',
                stats: { def: 9, hp: 28 }, penalty: { spd: -10 },
                special: 'overheat_shield', specialDesc: '과열 폭발 시 3초간 받는 피해 -40%',
                zoneBonus: 'Cargo에서 -40%→-60%', flavor: '증기 배출구가 방어막을 만든다'
            },
            {
                id: 'zca02', name: '기관사 작업복', minRarity: 'rare',
                stats: { def: 7, hp: 35, spd: 5 }, penalty: { def: -2 },
                special: 'train_speed', specialDesc: '칸 피해 감소 +15% (전투 중 열차 보호)',
                zoneBonus: 'Cargo에서 +25%', flavor: '열차를 지키는 자의 유니폼'
            },
            {
                id: 'zca03', name: '화물 제독의 외투', minRarity: 'epic',
                stats: { def: 15, hp: 55, atk: 6 }, penalty: { spd: -14 },
                special: 'fortress', specialDesc: '전투 시작 3초 무적 + 과열 폭발 전체 피해',
                zoneBonus: 'Cargo에서 무적 3초→5초', flavor: '열차의 주인이 입는 철벽 외투'
            }
        ],
        accessory: [
            {
                id: 'zcx01', name: '동력 코어 목걸이', minRarity: 'uncommon',
                stats: { atk: 4, spd: 6 }, penalty: { hp: -10 },
                special: 'overheat_accel', specialDesc: '과열 누적 속도 +40%',
                zoneBonus: 'Cargo에서 +60%', flavor: '과열을 부추기는 동력원'
            },
            {
                id: 'zcx02', name: '연료 조절기', minRarity: 'rare',
                stats: { def: 4, hp: 20, spd: 8 }, penalty: { atk: -3 },
                special: 'fuel_bonus', specialDesc: '역 정차 시 연료 +2 추가 생성',
                zoneBonus: 'Cargo에서 +3', flavor: '연료 효율을 극대화한다'
            },
            {
                id: 'zcx03', name: '제독의 시계', minRarity: 'epic',
                stats: { atk: 10, spd: 12, critRate: 0.05 }, penalty: { hp: -20 },
                special: 'time_pressure', specialDesc: '전투 시간 제한 10초 감소, ATK +15% 버프',
                zoneBonus: 'Cargo에서 ATK +25%', flavor: '시간은 돈이다 — 문자 그대로'
            }
        ]
    },
    blackout: {
        weapon: [
            {
                id: 'zow01', name: '저주받은 단검', minRarity: 'uncommon',
                stats: { atk: 9, critRate: 0.05 }, penalty: { hp: -15 },
                special: 'fear', specialDesc: '공격 시 12% 확률로 적 1초 행동불가',
                zoneBonus: 'BO에서 확률 +10%', flavor: '닿는 것만으로도 공포가 퍼진다'
            },
            {
                id: 'zow02', name: '그림자 낫', minRarity: 'rare',
                stats: { atk: 15 }, penalty: { def: -5, spd: -5 },
                special: 'execute', specialDesc: 'HP 20% 이하 적 즉사 (보스 제외)',
                zoneBonus: 'BO에서 즉사 20%→30%', flavor: '그림자가 생명을 거둔다'
            },
            {
                id: 'zow03', name: '심연의 지팡이', minRarity: 'epic',
                stats: { atk: 18, critRate: 0.08 }, penalty: { hp: -30 },
                special: 'fear_cascade', specialDesc: '공포 적용 시 인접 적 1명에게 전파',
                zoneBonus: 'BO에서 전파 2명', flavor: '공포가 전염된다'
            }
        ],
        armor: [
            {
                id: 'zoa01', name: '저주받은 로브', minRarity: 'uncommon',
                stats: { def: 5, hp: 18, atk: 5 }, penalty: { def: -2 },
                special: 'fear_dodge', specialDesc: '공포 적용 시 3초간 회피율 +25%',
                zoneBonus: 'BO에서 +40%', flavor: '두려운 것은 적 뿐만이 아니다'
            },
            {
                id: 'zoa02', name: '망령의 갑옷', minRarity: 'rare',
                stats: { def: 10, hp: 30 }, penalty: { spd: -8 },
                special: 'ghost_phase', specialDesc: '전투당 1회 치명타 무효화',
                zoneBonus: 'BO에서 2회 무효', flavor: '반쯤 저세상에 걸쳐있는 갑옷'
            },
            {
                id: 'zoa03', name: '그림자 군주의 흉갑', minRarity: 'epic',
                stats: { def: 12, hp: 45, atk: 8 }, penalty: { spd: -10, def: -3 },
                special: 'curse_armor', specialDesc: '저주 Lv당 DEF +3, 받는 피해 -2%',
                zoneBonus: 'BO에서 효과 2배', flavor: '어둠이 깊을수록 단단해진다'
            }
        ],
        accessory: [
            {
                id: 'zox01', name: '저주받은 반지', minRarity: 'uncommon',
                stats: { critRate: 0.06, atk: 4 }, penalty: { hp: -18 },
                special: 'fear_duration', specialDesc: '공포 지속시간 +1초',
                zoneBonus: 'BO에서 +2초', flavor: '끼는 순간 손가락이 차가워진다'
            },
            {
                id: 'zox02', name: '어둠의 눈', minRarity: 'rare',
                stats: { atk: 7, critRate: 0.07 }, penalty: { def: -3 },
                special: 'dark_sight', specialDesc: '어둠 속 적 약점 노출 (피해 +15%)',
                zoneBonus: 'BO에서 +25%', flavor: '어둠 속에서 모든 것이 보인다'
            },
            {
                id: 'zox03', name: '그림자 군주의 가면 조각', minRarity: 'epic',
                stats: { atk: 12, critRate: 0.10, spd: 8 }, penalty: { hp: -35, def: -5 },
                special: 'shadow_lord', specialDesc: '저주 Lv당 전체 스탯 +3%, 공포 적 피해 2배',
                zoneBonus: 'BO에서 Lv당 +5%', flavor: '군주의 힘 일부가 깃들어 있다'
            }
        ]
    }
};

// ═══════════════════════════════════════════════
//  경매장 전용 아이템 — 경매장에서만 등장
//  NPC 상인이 간헐적으로 올리는 특이한 물건들
// ═══════════════════════════════════════════════

const AUCTION_EXCLUSIVE = [
    {
        id: 'auc01', name: '떠돌이 상인의 부적', slot: 'accessory', rarity: 'rare',
        stats: { atk: 4, def: 4, hp: 20, critRate: 0.04 }, penalty: {},
        special: 'gold_find', specialDesc: '전투 골드 획득 +20%',
        basePrice: 800, flavor: '가격 이상의 가치가 있다 — 상인의 말'
    },
    {
        id: 'auc02', name: '밀수품 증기 권총', slot: 'weapon', rarity: 'rare',
        stats: { atk: 13, spd: 10 }, penalty: { def: -3 },
        special: 'smuggled_shot', specialDesc: '첫 공격 무조건 크리티컬',
        basePrice: 1200, flavor: '출처를 묻지 마라'
    },
    {
        id: 'auc03', name: '골동품 갑옷', slot: 'armor', rarity: 'epic',
        stats: { def: 18, hp: 70 }, penalty: { spd: -10 },
        special: 'antique_guard', specialDesc: 'HP 100% 시 첫 피격 무효',
        basePrice: 3000, flavor: '수백 년을 견딘 갑옷'
    },
    {
        id: 'auc04', name: '현상금 사냥꾼의 활', slot: 'weapon', rarity: 'rare',
        stats: { atk: 11, critRate: 0.08 }, penalty: {},
        special: 'bounty', specialDesc: '엘리트/보스 처치 시 골드 +50%',
        basePrice: 1500, flavor: '적의 머리에 현상금이 걸려있다'
    },
    {
        id: 'auc05', name: '도박꾼의 주사위', slot: 'accessory', rarity: 'rare',
        stats: { critRate: 0.10 }, penalty: { atk: -3 },
        special: 'gambler', specialDesc: '크리 시 50% 확률 데미지 3배, 50% 확률 0.5배',
        basePrice: 1000, flavor: '운명은 주사위에 달렸다'
    },
    {
        id: 'auc06', name: '유령선 선장의 코트', slot: 'armor', rarity: 'epic',
        stats: { def: 10, hp: 40, spd: 12 }, penalty: { hp: -15 },
        special: 'ghost_captain', specialDesc: '사망 시 1회 부활 (HP 30%, 전투당 1회)',
        basePrice: 5000, flavor: '죽어도 배를 떠나지 못한 선장'
    },
    {
        id: 'auc07', name: '시장 상인의 저울', slot: 'accessory', rarity: 'uncommon',
        stats: { def: 3, hp: 15 }, penalty: {},
        special: 'market_sense', specialDesc: '아이템 판매가 +15%',
        basePrice: 500, flavor: '물건의 진짜 가치를 안다'
    },
    {
        id: 'auc08', name: '암흑가 단검', slot: 'weapon', rarity: 'epic',
        stats: { atk: 16, critRate: 0.12, spd: 8 }, penalty: { hp: -20 },
        special: 'assassin', specialDesc: '전투 시작 5초간 ATK 2배 (선제 암살)',
        basePrice: 4000, flavor: '첫 일격이 마지막이 되도록'
    },
    {
        id: 'auc09', name: '연금술사의 플라스크', slot: 'accessory', rarity: 'rare',
        stats: { atk: 5, hp: 20 }, penalty: {},
        special: 'alchemy', specialDesc: '소비 아이템 효과 +30%',
        basePrice: 1800, flavor: '물약의 효능을 극대화한다'
    },
    {
        id: 'auc10', name: '용병단장의 어깨걸이', slot: 'armor', rarity: 'rare',
        stats: { def: 8, hp: 30, atk: 4 }, penalty: {},
        special: 'commander', specialDesc: '파티원 전체 DEF +5 (본인 제외)',
        basePrice: 2200, flavor: '부하를 지키는 것이 리더의 의무'
    }
];

// ═══════════════════════════════════════════════
//  제작 전용 — 드롭 불가, 파밍+제작으로만 획득
// ═══════════════════════════════════════════════

const CRAFT_ONLY_EQUIPMENT = [
    // === 구역별 고급 제작 (rare) ===
    {
        id: 'co01', name: '흡혈의 칼날', slot: 'weapon', rarity: 'rare', zone: 'bloodpit',
        stats: { atk: 14, critRate: 0.05 }, special: 'lifesteal',
        specialDesc: '공격 시 피해의 10% HP 회복', desc: '피를 먹고 자라는 검'
    },
    {
        id: 'co02', name: '출혈 촉진제 갑옷', slot: 'armor', rarity: 'rare', zone: 'bloodpit',
        stats: { def: 9, hp: 35, atk: 4 }, special: 'bleed_amplify',
        specialDesc: '파티 전원의 출혈 데미지 +25%', desc: '적의 피가 더 잘 흐른다'
    },
    {
        id: 'co03', name: '증기 기관 팔찌', slot: 'accessory', rarity: 'rare', zone: 'cargo',
        stats: { atk: 7, spd: 12 }, special: 'momentum',
        specialDesc: '전투 중 매 5초마다 ATK +4% 누적 (최대 +32%)', desc: '멈추지 않는 동력'
    },
    {
        id: 'co04', name: '기관차 방패', slot: 'armor', rarity: 'rare', zone: 'cargo',
        stats: { def: 12, hp: 40 }, special: 'ram',
        specialDesc: '전투 시작 시 적 전열에 돌진 피해 (DEF×2)', desc: '기관차처럼 밀어붙인다'
    },
    {
        id: 'co05', name: '허공의 망토', slot: 'armor', rarity: 'rare', zone: 'blackout',
        stats: { def: 7, hp: 25, spd: 10 }, special: 'phase',
        specialDesc: '전투당 첫 피격 무효 (쿨 없음)', desc: '실체를 잃은 자의 옷'
    },
    {
        id: 'co06', name: '공포의 수정구', slot: 'accessory', rarity: 'rare', zone: 'blackout',
        stats: { atk: 8, critRate: 0.06 }, special: 'mass_fear',
        specialDesc: '전투 시작 시 적 전체 2초 행동불가', desc: '바라보는 것만으로 공포가 퍼진다'
    },
    // === 크로스 구역 제작 (epic) — 2구역 소재 필요 ===
    {
        id: 'co07', name: '핏빛 증기 검', slot: 'weapon', rarity: 'epic', zone: 'cross',
        stats: { atk: 22, critRate: 0.07 }, special: 'blood_steam',
        specialDesc: '출혈+과열 동시 부여, 출혈 적에게 과열 폭발 +50%', desc: '피와 증기의 결합'
    },
    {
        id: 'co08', name: '공포의 증기 헬름', slot: 'armor', rarity: 'epic', zone: 'cross',
        stats: { def: 14, hp: 50, atk: 6 }, special: 'terror_engine',
        specialDesc: '과열 폭발 시 주변 적 공포 부여, 공포 적 과열 가속', desc: '공포와 폭발의 악순환'
    },
    {
        id: 'co09', name: '피와 그림자의 반지', slot: 'accessory', rarity: 'epic', zone: 'cross',
        stats: { atk: 10, critRate: 0.08, spd: 6 }, special: 'blood_shadow',
        specialDesc: '처치 시 HP 15% 회복 + 3초간 회피 +30%', desc: '살육 후 그림자에 숨는다'
    },
    // === 최종 제작 (legendary) — 3구역 보스 소재 전부 필요 ===
    {
        id: 'co10', name: '삼위일체 검', slot: 'weapon', rarity: 'legendary', zone: 'all',
        stats: { atk: 28, critRate: 0.10 }, special: 'trinity',
        specialDesc: '출혈+과열+공포 동시 부여 (각 60% 확률), 3중 중첩 시 즉사', desc: '세 구역의 힘이 하나로'
    },
    {
        id: 'co11', name: '신화의 갑옷', slot: 'armor', rarity: 'legendary', zone: 'all',
        stats: { def: 22, hp: 100, atk: 5 }, special: 'mythic_endure',
        specialDesc: '치명타 무효화 (쿨 15초) + HP 30% 이하 시 5초 무적', desc: '보스의 영혼이 수호하는 갑옷'
    },
    {
        id: 'co12', name: '군주의 왕관', slot: 'accessory', rarity: 'legendary', zone: 'all',
        stats: { atk: 18, def: 8, hp: 40, critRate: 0.10 }, special: 'lord_aura',
        specialDesc: '파티 전원 ATK/DEF +12% 오라 + 적 처치 시 전원 HP 5% 회복', desc: '세 구역을 정복한 자의 증표'
    }
];

// ═══════════════════════════════════════════════
//  저주 장비 — 해제 불가, 극강 스탯 + 치명적 대가
// ═══════════════════════════════════════════════

const CURSED_EQUIPMENT = [
    {
        id: 'cur01', name: '저주받은 대검', slot: 'weapon', rarity: 'epic',
        stats: { atk: 30, critRate: 0.08 }, penalty: { def: -8 },
        cursed: true, curseDebuff: 'bleed_self',
        curseDebuffDesc: '매 전투 시작 시 HP 10% 손실', desc: '어둠에 물든 검 — 해제 불가'
    },
    {
        id: 'cur02', name: '원혼의 갑옷', slot: 'armor', rarity: 'epic',
        stats: { def: 25, hp: 100 }, penalty: { spd: -20 },
        cursed: true, curseDebuff: 'slow',
        curseDebuffDesc: '이동속도 -30%, 공격속도 -15%', desc: '영혼이 몸을 짓누른다 — 해제 불가'
    },
    {
        id: 'cur03', name: '탐욕의 반지', slot: 'accessory', rarity: 'epic',
        stats: { atk: 18, critRate: 0.12 }, penalty: {},
        cursed: true, curseDebuff: 'gold_drain',
        curseDebuffDesc: '전투마다 100G 소모 (부족 시 ATK -60%)', desc: '끝없는 탐욕 — 해제 불가'
    },
    {
        id: 'cur04', name: '광기의 투구', slot: 'armor', rarity: 'legendary',
        stats: { atk: 25, def: 28, hp: 60, critRate: 0.06 }, penalty: {},
        cursed: true, curseDebuff: 'berserk',
        curseDebuffDesc: 'HP 25% 이하 시 아군 공격 (4초간)', desc: '광기의 무장 — 해제 불가'
    },
    {
        id: 'cur05', name: '피의 단검', slot: 'weapon', rarity: 'rare',
        stats: { atk: 20, critRate: 0.10 }, penalty: {},
        cursed: true, curseDebuff: 'lifedrain',
        curseDebuffDesc: '매 공격 시 자기 HP 4% 손실', desc: '피를 갈구하는 단검 — 해제 불가'
    },
    {
        id: 'cur06', name: '그림자 목걸이', slot: 'accessory', rarity: 'rare',
        stats: { spd: 30, atk: 12, critRate: 0.06 }, penalty: {},
        cursed: true, curseDebuff: 'fragile',
        curseDebuffDesc: '받는 피해 +30%', desc: '그림자와 거래한 대가 — 해제 불가'
    },
    {
        id: 'cur07', name: '영원의 족쇄', slot: 'accessory', rarity: 'legendary',
        stats: { def: 15, hp: 80, atk: 12 }, penalty: {},
        cursed: true, curseDebuff: 'no_heal',
        curseDebuffDesc: '전투 중 회복 불가 (힐/흡혈 무효)', desc: '절대 풀리지 않는 사슬 — 해제 불가'
    },
    {
        id: 'cur08', name: '배신자의 검', slot: 'weapon', rarity: 'legendary',
        stats: { atk: 35, critRate: 0.15, spd: 10 }, penalty: {},
        cursed: true, curseDebuff: 'betrayal',
        curseDebuffDesc: '전투 시작 20% 확률로 아군 1명에게 첫 공격', desc: '신뢰를 갈가리 찢는 검 — 해제 불가'
    }
];

// ═══════════════════════════════════════════════
//  소재 (crafting.md 동기화 — 15종)
// ═══════════════════════════════════════════════

const MATERIAL_TEMPLATES = [
    // Blood Pit
    { id: 'mat01', name: '가죽',            zone: 'bloodpit', rarity: 'common',    baseValue: 15, desc: '기본 방어구 소재' },
    { id: 'mat02', name: '뼛조각',          zone: 'bloodpit', rarity: 'common',    baseValue: 15, desc: '무기 강화 소재' },
    { id: 'mat03', name: '혈정석',          zone: 'bloodpit', rarity: 'rare',      baseValue: 80, desc: '출혈 장비 핵심 소재' },
    { id: 'mat04', name: '핏로드의 송곳니', zone: 'bloodpit', rarity: 'legendary', baseValue: 300, desc: '전설 장비 제작' },
    // Cargo
    { id: 'mat05', name: '강철 조각',       zone: 'cargo',    rarity: 'common',    baseValue: 18, desc: '갑옷 제작 기본 소재' },
    { id: 'mat06', name: '기계 부품',       zone: 'cargo',    rarity: 'common',    baseValue: 18, desc: '증기 장비 강화 소재' },
    { id: 'mat07', name: '마법 동력석',     zone: 'cargo',    rarity: 'rare',      baseValue: 80, desc: '증기/과열 장비 핵심 소재' },
    { id: 'mat08', name: '화물 제독의 인장', zone: 'cargo',   rarity: 'legendary', baseValue: 300, desc: '전설 장비 제작' },
    // Blackout
    { id: 'mat09', name: '어둠의 정수',     zone: 'blackout', rarity: 'common',    baseValue: 20, desc: '저주 장비 강화 소재' },
    { id: 'mat10', name: '영혼 조각',       zone: 'blackout', rarity: 'common',    baseValue: 20, desc: '범용 강화 소재' },
    { id: 'mat11', name: '저주 유물',       zone: 'blackout', rarity: 'rare',      baseValue: 80, desc: '저주/공포 장비 핵심 소재' },
    { id: 'mat12', name: '그림자 군주의 가면 조각', zone: 'blackout', rarity: 'legendary', baseValue: 300, desc: '전설 장비 제작' },
    // 공통
    { id: 'mat13', name: '기본 금속',       zone: 'common',   rarity: 'common',    baseValue: 10, desc: '모든 제작 기본 소재' },
    { id: 'mat14', name: '마력의 가루',     zone: 'common',   rarity: 'rare',      baseValue: 60, desc: '마법 부여/강화 소재' },
    { id: 'mat15', name: '순수 결정',       zone: 'common',   rarity: 'legendary', baseValue: 500, desc: '전설 제작 필수 소재' }
];

// ═══════════════════════════════════════════════
//  소비 아이템
// ═══════════════════════════════════════════════

const CONSUMABLE_TEMPLATES = [
    // 기본 (Tier 1)
    { id: 'con01', name: 'HP 포션',       effect: 'heal',     value: 30, desc: 'HP 30% 회복',         baseValue: 20, tier: 1 },
    { id: 'con02', name: '전투 물약',     effect: 'atkBuff',  value: 20, desc: 'ATK +20% (1전투)',    baseValue: 35, tier: 1 },
    { id: 'con03', name: '방어 물약',     effect: 'defBuff',  value: 20, desc: 'DEF +20% (1전투)',    baseValue: 35, tier: 1 },
    { id: 'con04', name: '화염 폭탄',     effect: 'aoe',      value: 50, desc: '적 전체 50 피해',     baseValue: 40, tier: 1 },
    // 고급 (Tier 2)
    { id: 'con05', name: '완전 회복약',   effect: 'fullHeal', value: 100, desc: 'HP 100% 회복',       baseValue: 80, tier: 2 },
    { id: 'con06', name: '치명 엘릭서',   effect: 'critBuff', value: 30,  desc: 'CRIT +30% (1전투)',  baseValue: 60, tier: 2 },
    { id: 'con07', name: '가속 물약',     effect: 'spdBuff',  value: 30,  desc: '공속 +30% (1전투)',  baseValue: 50, tier: 2 },
    { id: 'con08', name: '강철 피부약',   effect: 'shield',   value: 50,  desc: '피해 흡수 50 실드',  baseValue: 55, tier: 2 },
    { id: 'con09', name: '해독제',        effect: 'cleanse',  value: 0,   desc: '출혈/저주 등 디버프 해제', baseValue: 40, tier: 2 },
    // 구역 특수 (Tier 3)
    { id: 'con10', name: '피의 성수',     effect: 'lifesteal', value: 15, desc: '1전투 동안 흡혈 15%', baseValue: 70, tier: 3, zone: 'bloodpit' },
    { id: 'con11', name: '증기 충전팩',   effect: 'burst',     value: 80, desc: '즉시 과열 폭발 발동', baseValue: 70, tier: 3, zone: 'cargo' },
    { id: 'con12', name: '그림자 베일',   effect: 'dodge',     value: 40, desc: '5초간 회피율 +40%',  baseValue: 70, tier: 3, zone: 'blackout' }
];

// ═══════════════════════════════════════════════
//  아이템 생성
// ═══════════════════════════════════════════════

let _itemIdCounter = 1;

function generateItem(zone, guildLevel, rarityBonus) {
    rarityBonus = rarityBonus || 0;
    const roll = Math.random();
    let type, template, slot;

    if (roll < 0.30) {
        type = 'equipment';
        // 구역 특수 장비 확률 (해당 구역에서만)
        if (zone !== 'common' && Math.random() < 0.22) {
            return _generateZoneEquipment(zone, guildLevel, rarityBonus);
        }
        const slots = ['weapon', 'armor', 'accessory'];
        slot = slots[Math.floor(Math.random() * slots.length)];
        const pool = EQUIPMENT_TEMPLATES[slot];
        // 길드 레벨에 따라 tier 가중치
        const maxTier = Math.min(4, 1 + Math.floor(guildLevel / 2));
        const eligible = pool.filter(t => t.tier <= maxTier);
        template = { ...eligible[Math.floor(Math.random() * eligible.length)] };
    } else if (roll < 0.68) {
        type = 'material';
        const pool = zone !== 'common'
            ? MATERIAL_TEMPLATES.filter(m => m.zone === zone || m.zone === 'common')
            : MATERIAL_TEMPLATES.filter(m => m.zone === 'common');
        template = { ...pool[Math.floor(Math.random() * pool.length)] };
    } else {
        type = 'consumable';
        const tierMax = Math.min(3, 1 + Math.floor(guildLevel / 3));
        const pool = CONSUMABLE_TEMPLATES.filter(c => c.tier <= tierMax && (!c.zone || c.zone === zone));
        template = { ...pool[Math.floor(Math.random() * pool.length)] };
    }

    const rarity = _rollRarity(guildLevel, rarityBonus, type === 'material' ? template.rarity : null);
    const statMult = ITEM_RARITY[rarity].statMult;
    const valueMult = ITEM_RARITY[rarity].valueMult;

    const item = {
        id: _itemIdCounter++,
        type,
        rarity,
        baseId: template.id || template.name,
        name: _applyRarityPrefix(template.name, rarity, type),
        slot: slot || template.slot || null,
        desc: template.desc || template.flavor || '',
        value: Math.floor((template.baseValue || 30) * valueMult),
        zone: template.zone || zone
    };

    if (type === 'equipment') {
        item.stats = {};
        if (template.baseAtk) item.stats.atk = Math.floor(template.baseAtk * statMult);
        if (template.baseDef) item.stats.def = Math.floor(template.baseDef * statMult);
        if (template.baseHp)  item.stats.hp = Math.floor(template.baseHp * statMult);
        if (template.baseCrit) item.stats.critRate = +Math.min(0.40, template.baseCrit * statMult).toFixed(3);
        if (template.baseSpd) item.stats.moveSpeed = Math.floor(template.baseSpd * Math.min(statMult, 2.5));
        item.slot = slot;
        item.flavor = template.flavor || '';
    }

    if (type === 'consumable') {
        item.effect = template.effect;
        item.effectValue = Math.floor(template.value * (type === 'consumable' ? Math.min(statMult, 2) : 1));
    }

    return item;
}

function _applyRarityPrefix(baseName, rarity, type) {
    if (type !== 'equipment') return baseName;
    const prefixes = {
        common: '',
        uncommon: '',
        rare: '정제된 ',
        epic: '고대의 ',
        legendary: '신성한 '
    };
    const prefix = prefixes[rarity] || '';
    return prefix + baseName;
}

function _generateZoneEquipment(zone, guildLevel, rarityBonus) {
    const zonePool = ZONE_EQUIPMENT[zone];
    if (!zonePool) return generateItem('common', guildLevel, rarityBonus);

    const slots = ['weapon', 'armor', 'accessory'];
    const slot = slots[Math.floor(Math.random() * slots.length)];
    const slotPool = zonePool[slot];
    if (!slotPool || slotPool.length === 0) return generateItem('common', guildLevel, rarityBonus);

    const rarity = _rollRarity(guildLevel, rarityBonus + 1);
    const rarityOrder = ['common', 'uncommon', 'rare', 'epic', 'legendary'];
    const rarityIdx = rarityOrder.indexOf(rarity);

    const eligible = slotPool.filter(t => rarityIdx >= rarityOrder.indexOf(t.minRarity));
    if (eligible.length === 0) return generateItem(zone, guildLevel, rarityBonus);

    const template = eligible[Math.floor(Math.random() * eligible.length)];
    const statMult = ITEM_RARITY[rarity].statMult;
    const valueMult = ITEM_RARITY[rarity].valueMult;

    const item = {
        id: _itemIdCounter++,
        type: 'equipment',
        rarity,
        baseId: template.id,
        name: template.name,
        slot: slot,
        desc: template.specialDesc,
        flavor: template.flavor || '',
        value: Math.floor(80 * valueMult),
        zone,
        stats: {},
        penalty: {},
        special: template.special,
        specialDesc: template.specialDesc,
        zoneBonus: template.zoneBonus,
        isZoneEquipment: true
    };

    for (const [k, v] of Object.entries(template.stats)) {
        if (k === 'critRate') item.stats[k] = +Math.min(0.40, v * statMult).toFixed(3);
        else item.stats[k] = Math.floor(v * statMult);
    }
    if (template.penalty) {
        for (const [k, v] of Object.entries(template.penalty)) {
            item.penalty[k] = Math.floor(v * Math.min(statMult, 2));
        }
    }

    return item;
}

function _rollRarity(guildLevel, rarityBonus, forceRarity) {
    if (forceRarity) return forceRarity;

    const rarityRoll = Math.random() * 100;
    const lvBonus = (guildLevel - 1) * 3.5;
    const bonusPts = (rarityBonus || 0) * 5;
    const totalBonus = lvBonus + bonusPts;

    if (rarityRoll < Math.min(3, 0 + totalBonus * 0.08))              return 'legendary';
    if (rarityRoll < Math.min(12, 0 + totalBonus * 0.3))              return 'epic';
    if (rarityRoll < Math.min(30, 3 + totalBonus * 0.6))              return 'rare';
    if (rarityRoll < Math.min(55, 20 + totalBonus * 0.8))             return 'uncommon';
    return 'common';
}

function generateCursedItem() {
    const template = CURSED_EQUIPMENT[Math.floor(Math.random() * CURSED_EQUIPMENT.length)];
    const statMult = ITEM_RARITY[template.rarity].statMult;
    return {
        id: _itemIdCounter++,
        type: 'equipment',
        rarity: template.rarity,
        baseId: template.id,
        name: template.name,
        slot: template.slot,
        desc: template.desc,
        stats: { ...template.stats },
        penalty: template.penalty || {},
        cursed: true,
        curseDebuff: template.curseDebuff,
        curseDebuffDesc: template.curseDebuffDesc,
        value: Math.floor(80 * ITEM_RARITY[template.rarity].valueMult),
        isZoneEquipment: false
    };
}

function generateAuctionItem() {
    const template = AUCTION_EXCLUSIVE[Math.floor(Math.random() * AUCTION_EXCLUSIVE.length)];
    const statMult = ITEM_RARITY[template.rarity].statMult;
    return {
        id: _itemIdCounter++,
        type: 'equipment',
        rarity: template.rarity,
        baseId: template.id,
        name: template.name,
        slot: template.slot,
        desc: template.flavor || '',
        stats: { ...template.stats },
        penalty: template.penalty || {},
        special: template.special,
        specialDesc: template.specialDesc,
        value: template.basePrice,
        isAuctionExclusive: true
    };
}

// ── Market Trend System ──────────────────────────────────────
const MARKET_CATEGORIES = ['equipment', 'material', 'consumable'];

function initMarketTrends(gs) {
    if (gs.marketTrends) return;
    gs.marketTrends = {
        equipment: { modifier: 1.0, trend: 0, supply: 'normal' },
        material:  { modifier: 1.0, trend: 0, supply: 'normal' },
        consumable:{ modifier: 1.0, trend: 0, supply: 'normal' }
    };
    gs.marketTrendTick = 0;
}

function updateMarketTrends(gs, event) {
    initMarketTrends(gs);
    const trends = gs.marketTrends;

    switch (event) {
        case 'run_success':
            trends.material.modifier = Math.max(0.6, trends.material.modifier - 0.05);
            trends.equipment.modifier = Math.min(1.5, trends.equipment.modifier + 0.03);
            break;
        case 'run_fail':
            trends.consumable.modifier = Math.min(1.5, trends.consumable.modifier + 0.08);
            trends.equipment.modifier = Math.max(0.6, trends.equipment.modifier - 0.03);
            break;
        case 'bulk_sell':
            trends.material.modifier = Math.max(0.6, trends.material.modifier - 0.08);
            break;
        case 'bulk_buy':
            trends.equipment.modifier = Math.min(1.5, trends.equipment.modifier + 0.05);
            break;
    }

    gs.marketTrendTick = (gs.marketTrendTick || 0) + 1;
    if (gs.marketTrendTick % 3 === 0) {
        for (const cat of MARKET_CATEGORIES) {
            const drift = (Math.random() - 0.5) * 0.1;
            trends[cat].modifier = Math.max(0.5, Math.min(1.6, trends[cat].modifier + drift));
            trends[cat].modifier = Math.round(trends[cat].modifier * 100) / 100;

            if (trends[cat].modifier <= 0.7) trends[cat].supply = 'surplus';
            else if (trends[cat].modifier >= 1.3) trends[cat].supply = 'shortage';
            else trends[cat].supply = 'normal';

            trends[cat].trend = drift > 0.02 ? 1 : drift < -0.02 ? -1 : 0;
        }
    }
}

function getMarketPriceModifier(gs, itemType) {
    initMarketTrends(gs);
    return gs.marketTrends[itemType]?.modifier || 1.0;
}
