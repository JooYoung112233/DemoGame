const TOWN_EVENTS = {
    // --- Tier 1: 길드 Lv 1~3 (일상 이벤트) ---
    injured_merc: {
        tier: 1, name: '부상당한 용병',
        desc: '길드에 부상당한 용병이 도움을 요청하고 있다.',
        icon: '🩹',
        choiceA: { label: '치료 (100G)', cost: { gold: 100 } },
        choiceB: { label: '방치' },
        applyA(gs) {
            const injured = gs.roster.find(m => m.alive && m.currentHp < m.getStats().hp);
            if (injured) {
                injured.currentHp = injured.getStats().hp;
                return `${injured.name}의 HP가 완전히 회복되었다.`;
            }
            return '부상자가 없어 기부금으로 처리했다.';
        },
        applyB(gs) {
            const target = gs.roster.find(m => m.alive);
            if (target) {
                target._eventDebuff = { stat: 'all', mult: -0.10, runs: 1 };
                return `${target.name}의 사기가 떨어졌다. (다음 런 스탯 -10%)`;
            }
            return '아무 일도 일어나지 않았다.';
        }
    },
    stranger_merchant: {
        tier: 1, name: '낯선 상인 방문',
        desc: '수상한 상인이 특별한 물건을 팔겠다고 한다.',
        icon: '🧳',
        choiceA: { label: '구매 (200G)', cost: { gold: 200 } },
        choiceB: { label: '거절' },
        applyA(gs) {
            const item = generateItem('bloodpit', gs.guildLevel, 1);
            if (item && StorageManager.addItem(gs, item)) {
                return `${item.name}을(를) 획득했다!`;
            }
            return '보관함이 가득 차 물건을 받을 수 없었다.';
        },
        applyB() { return '상인이 아쉬운 듯 떠났다.'; }
    },
    new_recruit: {
        tier: 1, name: '신입 용병 지원',
        desc: '길드 소문을 듣고 신입 용병이 찾아왔다.',
        icon: '⚔',
        choiceA: { label: '고용 (150G)', cost: { gold: 150 } },
        choiceB: { label: '거절' },
        applyA(gs) {
            const merc = new Mercenary(
                ['warrior','rogue','mage','archer','priest','alchemist'][Math.floor(Math.random()*6)],
                'common'
            );
            gs.roster.push(merc);
            return `${merc.name} (${CLASS_DATA[merc.classKey].name}) 합류!`;
        },
        applyB() { return '신입을 돌려보냈다.'; }
    },
    guild_rumor: {
        tier: 1, name: '길드 소문',
        desc: '길드에 대한 좋은 소문이 퍼지고 있다. 활용할 수 있을 것 같다.',
        icon: '📢',
        choiceA: { label: '소문 퍼뜨리기' },
        choiceB: { label: '무시' },
        applyA(gs) {
            gs._auctionFeeDiscount = (gs._auctionFeeDiscount || 0) + 2;
            gs._auctionFeeDiscountRuns = 1;
            return '경매 수수료 -2% (1런 동안)';
        },
        applyB() { return '소문은 자연스럽게 사라졌다.'; }
    },
    training_request: {
        tier: 1, name: '훈련 요청',
        desc: '용병 한 명이 특별 훈련을 요청했다.',
        icon: '🏋',
        choiceA: { label: '허가 (훈련 포인트 1)', cost: { trainingPoints: 1 } },
        choiceB: { label: '거절' },
        applyA(gs) {
            const target = gs.roster.find(m => m.alive);
            if (target) {
                target.xp += 50;
                return `${target.name}에게 XP +50 지급!`;
            }
            return '훈련할 용병이 없다.';
        },
        applyB() { return '훈련 요청을 거절했다.'; }
    },
    repair_request: {
        tier: 1, name: '장비 수리 요청',
        desc: '장비가 심하게 손상된 용병이 수리를 부탁한다.',
        icon: '🔧',
        choiceA: { label: '수리 (80G)', cost: { gold: 80 } },
        choiceB: { label: '거절' },
        applyA() { return '장비가 깨끗하게 수리되었다.'; },
        applyB(gs) {
            const target = gs.roster.find(m => m.alive);
            if (target) {
                target._eventDebuff = { stat: 'all', mult: -0.05, runs: 1 };
                return `${target.name}의 장비 성능이 떨어졌다. (다음 런 -5%)`;
            }
            return '아무 일도 일어나지 않았다.';
        }
    },

    // --- Tier 2: 길드 Lv 4~6 (구역 연계 이벤트) ---
    bloodpit_survivor: {
        tier: 2, name: 'Blood Pit 생존자',
        desc: 'Blood Pit에서 살아 돌아온 자가 있다. 구출하면 합류할지도…',
        icon: '💀',
        choiceA: { label: '구조 (300G)', cost: { gold: 300 } },
        choiceB: { label: '무시' },
        applyA(gs) {
            if (Math.random() < 0.3) {
                const merc = new Mercenary(
                    ['warrior','rogue'][Math.floor(Math.random()*2)], 'rare'
                );
                gs.roster.push(merc);
                return `희귀 용병 ${merc.name} 합류!`;
            }
            return '생존자를 구조했지만 이미 너무 약해져 합류할 수 없었다.';
        },
        applyB() { return '안타깝지만 무시했다.'; }
    },
    cargo_wreck: {
        tier: 2, name: '화물선 난파 소식',
        desc: '근처에서 화물선이 난파되었다는 소식이 들린다.',
        icon: '🚂',
        choiceA: { label: '구조대 파견' },
        choiceB: { label: '무시' },
        applyA(gs) {
            if (Math.random() < 0.6) {
                for (let i = 0; i < 3; i++) {
                    const item = generateItem('cargo', gs.guildLevel, 0);
                    if (item) StorageManager.addItem(gs, item);
                }
                return '구조 성공! 동력석 관련 전리품 ×3 획득!';
            }
            const target = gs.roster.find(m => m.alive);
            if (target) {
                target.currentHp = Math.floor(target.currentHp * 0.7);
                return `구조 실패… ${target.name}이(가) 부상당했다. (HP -30%)`;
            }
            return '구조 실패. 아무것도 건지지 못했다.';
        },
        applyB() { return '소식을 무시했다.'; }
    },
    mansion_detective: {
        tier: 2, name: '저택 탐정 의뢰',
        desc: '저주받은 저택을 조사해달라는 의뢰가 들어왔다.',
        icon: '🔦',
        choiceA: { label: '수락' },
        choiceB: { label: '거절' },
        applyA(gs) {
            for (let i = 0; i < 2; i++) {
                const item = generateItem('blackout', gs.guildLevel, 1);
                if (item) StorageManager.addItem(gs, item);
            }
            GuildManager.addGold(gs, 500);
            return '의뢰 완수! 저주 유물 ×2 + 500G 획득!';
        },
        applyB() { return '위험한 의뢰를 거절했다.'; }
    },
    rival_guild: {
        tier: 2, name: '경쟁 길드 도발',
        desc: '경쟁 길드가 도발해왔다. 응전하겠는가?',
        icon: '🏴',
        choiceA: { label: '응전' },
        choiceB: { label: '무시' },
        applyA(gs) {
            if (Math.random() < 0.5) {
                const item = generateItem('bloodpit', gs.guildLevel, 3);
                if (item) StorageManager.addItem(gs, item);
                return `승리! 에픽 전리품 획득!`;
            }
            GuildManager.addGold(gs, -Math.min(gs.gold, 500));
            return '패배… 골드 -500G';
        },
        applyB() { return '도발을 무시했다.'; }
    },
    pin_info: {
        tier: 2, name: '상인 핀의 제보',
        desc: '상인 핀이 유용한 정보를 팔겠다고 한다.',
        icon: '🔍',
        choiceA: { label: '구매 (400G)', cost: { gold: 400 } },
        choiceB: { label: '거절' },
        applyA(gs) {
            gs._nextRunDropBonus = (gs._nextRunDropBonus || 0) + 0.20;
            return '다음 런 드랍률 +20% 확보!';
        },
        applyB() { return '핀이 아쉬운 듯 떠났다.'; }
    },
    krog_quest: {
        tier: 2, name: '크로그의 특별 의뢰',
        desc: '대장장이 크로그가 특별한 임무를 제안했다.',
        icon: '🔨',
        choiceA: { label: '수락' },
        choiceB: { label: '거절' },
        applyA(gs) {
            const item = generateItem('cargo', gs.guildLevel, 2);
            if (item) { item.name = '크로그의 특수 무기'; StorageManager.addItem(gs, item); }
            return '의뢰 완수! 크로그의 특수 무기 획득!';
        },
        applyB() { return '크로그가 실망한 표정을 지었다.'; }
    },

    // --- Tier 3: 길드 Lv 7~8 (세계관 이벤트) ---
    pitlord_envoy: {
        tier: 3, name: '핏로드의 사자 방문',
        desc: '핏로드가 사자를 보내 협력을 제안한다. 위험하지만 보상이 크다.',
        icon: '👹',
        choiceA: { label: '협력' },
        choiceB: { label: '거절' },
        applyA(gs) {
            gs._bloodpitDropBonus = (gs._bloodpitDropBonus || 0) + 0.20;
            return 'Blood Pit 드랍 +20% (영구)! 하지만 적대 세력이 생겼다…';
        },
        applyB() { return '사자를 돌려보냈다.'; }
    },
    cargo_crisis: {
        tier: 3, name: '마법 동력선 침몰 위기',
        desc: '마법 동력선이 침몰 직전이다. 지원하면 큰 보상이 있을 것이다.',
        icon: '⚡',
        choiceA: { label: '지원 (1000G)', cost: { gold: 1000 } },
        choiceB: { label: '방치' },
        applyA(gs) {
            gs.zoneLevel.cargo = (gs.zoneLevel.cargo || 1) + 1;
            return `Cargo 구역 레벨 +1! (현재 Lv.${gs.zoneLevel.cargo})`;
        },
        applyB() { return '동력선은 결국 침몰했다.'; }
    },
    curse_spread: {
        tier: 3, name: '저주의 확산',
        desc: '저주가 퍼지고 있다. 정화하지 않으면 위험해질 것이다.',
        icon: '🔮',
        choiceA: { label: '정화 (500G)', cost: { gold: 500 } },
        choiceB: { label: '방치' },
        applyA() { return '저주를 성공적으로 정화했다.'; },
        applyB(gs) {
            gs._blackoutBaseCurse = (gs._blackoutBaseCurse || 0) + 1;
            return '저주가 확산됐다. Blackout 기본 저주 Lv +1 (영구)';
        }
    },
    legendary_rumor: {
        tier: 3, name: '전설 용병 소문',
        desc: '전설적인 용병의 행방에 대한 소문이 돌고 있다.',
        icon: '👑',
        choiceA: { label: '수소문 (500G)', cost: { gold: 500 } },
        choiceB: { label: '무시' },
        applyA(gs) {
            const merc = new Mercenary(
                ['warrior','rogue','mage','archer','priest','alchemist'][Math.floor(Math.random()*6)],
                'legendary'
            );
            gs.roster.push(merc);
            return `전설 용병 ${merc.name} 합류!`;
        },
        applyB() { return '소문은 곧 잊혀졌다.'; }
    },
    guild_alliance: {
        tier: 3, name: '길드 연합 제안',
        desc: '다른 길드로부터 연합 제안이 들어왔다.',
        icon: '🤝',
        choiceA: { label: '수락' },
        choiceB: { label: '거절' },
        applyA(gs) {
            gs._auctionSlotBonus = (gs._auctionSlotBonus || 0) + 2;
            return '길드 연합 성사! 자동 경매 슬롯 +2!';
        },
        applyB() { return '연합 제안을 정중히 거절했다.'; }
    }
};

const TOWN_EVENT_KEYS = Object.keys(TOWN_EVENTS);

function pickTownEvent(guildLevel) {
    let maxTier = 1;
    if (guildLevel >= 7) maxTier = 3;
    else if (guildLevel >= 4) maxTier = 2;

    const pool = TOWN_EVENT_KEYS.filter(k => TOWN_EVENTS[k].tier <= maxTier);
    return pool[Math.floor(Math.random() * pool.length)];
}

function canAffordEventChoice(gs, choice) {
    if (!choice.cost) return true;
    if (choice.cost.gold && gs.gold < choice.cost.gold) return false;
    if (choice.cost.trainingPoints && (gs.trainingPoints || 0) < choice.cost.trainingPoints) return false;
    return true;
}

function payEventCost(gs, choice) {
    if (!choice.cost) return;
    if (choice.cost.gold) GuildManager.addGold(gs, -choice.cost.gold);
    if (choice.cost.trainingPoints) gs.trainingPoints = (gs.trainingPoints || 0) - choice.cost.trainingPoints;
}
