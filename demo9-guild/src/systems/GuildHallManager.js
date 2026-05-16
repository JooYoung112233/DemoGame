class GuildHallManager {
    static CATEGORIES = {
        operations:     { name: '운영',      code: 'A', icon: '📋', base: 300,  maxStage: 12 },
        infrastructure: { name: '인프라',    code: 'B', icon: '🏗', base: 400,  maxStage: 12 },
        recovery:       { name: '휴식',      code: 'C', icon: '💤', base: 250,  maxStage: 12 },
        automation:     { name: '자동화',    code: 'D', icon: '⚙',  base: 500,  maxStage: 8  },
        intel:          { name: '정보',      code: 'E', icon: '🔍', base: 600,  maxStage: 12 },
        pit_control:    { name: '핏 통제',   code: 'F', icon: '🩸', base: 800,  maxStage: 12 },
        cargo_control:  { name: '화물 통제', code: 'G', icon: '🚂', base: 800,  maxStage: 12 },
        dark_control:   { name: '어둠 통제', code: 'H', icon: '🌑', base: 800,  maxStage: 12 }
    };

    static GATE_REQUIREMENTS = [
        { stageMin: 1,  stageMax: 3,  reputation: 0,  guildLevel: 1 },
        { stageMin: 4,  stageMax: 6,  reputation: 0,  guildLevel: 2 },
        { stageMin: 7,  stageMax: 9,  reputation: 15, guildLevel: 4 },
        { stageMin: 10, stageMax: 12, reputation: 35, guildLevel: 6 }
    ];

    static ensureState(gs) {
        if (!gs.guildHall) {
            gs.guildHall = {
                operations: 0, infrastructure: 0, recovery: 0,
                automation: 0, intel: 0,
                pit_control: 0, cargo_control: 0, dark_control: 0
            };
        }
    }

    static getStage(gs, catKey) {
        GuildHallManager.ensureState(gs);
        return gs.guildHall[catKey] || 0;
    }

    static getMaxStage(catKey) {
        const cat = GuildHallManager.CATEGORIES[catKey];
        return cat ? cat.maxStage : 12;
    }

    static getUpgradeCost(catKey, nextStage) {
        const cat = GuildHallManager.CATEGORIES[catKey];
        if (!cat) return Infinity;
        return Math.round(cat.base * Math.pow(1.5, nextStage - 1));
    }

    static getGateForStage(stage) {
        for (const gate of GuildHallManager.GATE_REQUIREMENTS) {
            if (stage >= gate.stageMin && stage <= gate.stageMax) return gate;
        }
        return GuildHallManager.GATE_REQUIREMENTS[3];
    }

    static canUpgrade(gs, catKey) {
        GuildHallManager.ensureState(gs);
        const current = gs.guildHall[catKey] || 0;
        const max = GuildHallManager.getMaxStage(catKey);
        if (current >= max) return { ok: false, reason: '최대 단계' };

        const nextStage = current + 1;
        const cost = GuildHallManager.getUpgradeCost(catKey, nextStage);
        if (gs.gold < cost) return { ok: false, reason: `골드 부족 (${cost}G 필요)` };

        const gate = GuildHallManager.getGateForStage(nextStage);
        if (gs.guildLevel < gate.guildLevel) {
            return { ok: false, reason: `길드 Lv.${gate.guildLevel} 필요` };
        }
        if (gate.reputation > 0 && (gs.guildReputation || 0) < gate.reputation) {
            return { ok: false, reason: `평판 ${gate.reputation} 필요` };
        }

        // F/G/H 해금 조건
        if (catKey === 'pit_control' && (!gs.zoneClearCount || !gs.zoneClearCount['bloodpit_1'])) {
            return { ok: false, reason: 'Blood Pit 첫 클리어 필요' };
        }
        if (catKey === 'cargo_control' && (!gs.zoneClearCount || !gs.zoneClearCount['cargo_1'])) {
            return { ok: false, reason: 'Cargo 첫 클리어 필요' };
        }
        if (catKey === 'dark_control' && (!gs.zoneClearCount || !gs.zoneClearCount['blackout_1'])) {
            return { ok: false, reason: 'Blackout 첫 클리어 필요' };
        }

        return { ok: true, cost, nextStage };
    }

    static upgrade(gs, catKey) {
        const check = GuildHallManager.canUpgrade(gs, catKey);
        if (!check.ok) return false;

        GuildManager.spendGold(gs, check.cost);
        gs.guildHall[catKey] = check.nextStage;

        const cat = GuildHallManager.CATEGORIES[catKey];
        GuildManager.addMessage(gs, `🏛 ${cat.name} ${cat.code}${check.nextStage} 해금!`);
        SaveManager.save(gs);
        return true;
    }

    static getEffectDescription(catKey, stage) {
        const effects = GuildHallManager.EFFECTS[catKey];
        if (!effects || !effects[stage]) return '';
        return effects[stage];
    }

    static EFFECTS = {
        operations: {
            1: '서브 슬롯 +1',
            2: '서브 슬롯 +1',
            3: '메인 파티 +1',
            4: '서브 슬롯 +1',
            5: '서브 슬롯 +1',
            6: '메인 파티 +1',
            7: '동일구역 중복 파견',
            8: '서브 슬롯 +1',
            9: '서브 슬롯 +1',
            10: '메인 파티 +1',
            11: '동일구역 중복 +1, 서브 +1',
            12: '🏆 총사령관'
        },
        infrastructure: {
            1: '로스터 +2',
            2: '보관함 +4',
            3: '로스터 +2',
            4: '보안 컨테이너 +1',
            5: '보관함 +6',
            6: '로스터 +3',
            7: '보관함 카테고리 탭',
            8: '보안 컨테이너 +2',
            9: '로스터 +4',
            10: '보관함 +8, 장비 전용 보관함',
            11: '사망 시 인벤 보존 80%',
            12: '🏆 차원 창고'
        },
        recovery: {
            1: '스테미너 회복 +0.5/분',
            2: '스테미너 회복 +0.5/분',
            3: '부상 회복 -30%',
            4: '파견 후 스테미너 페널티 면제',
            5: '스테미너 회복 +1/분',
            6: '사망 위기 자동 생존 1회',
            7: '마을 복귀 +20 스테미너',
            8: '스테미너 회복 +2/분',
            9: '부상 회복 -50%',
            10: '사망 위기 쿨다운 감소',
            11: '부상자 서브 파견 가능',
            12: '🏆 불사의 길드'
        },
        automation: {
            1: '자동 장착 (전체 최적화 버튼)',
            2: '자동 판매 룰 해금',
            3: '자동 회수 (파견 즉시 처리)',
            4: '파견 출발 자동 최적화',
            5: '자동 재파견',
            6: '자동 위탁 (경매장)',
            7: '자동 치유/스테미너',
            8: '🏆 완전 자동화'
        },
        intel: {
            1: '파견 결과 미리보기',
            2: '소문 슬롯 +1',
            3: 'NPC 매물 갱신 -30%',
            4: '적 능력치/약점 표시',
            5: '마을 이벤트 알림',
            6: '소문 슬롯 +1',
            7: '평판 +30% 가속',
            8: 'NPC 호감도 +25% 가속',
            9: '소문 슬롯 +1',
            10: '시장 트렌드 예측',
            11: '음성 특성 위험도 표시',
            12: '🏆 전능의 시야'
        },
        pit_control: {
            1: '핏 게이지 최대 +50%',
            2: '핏 MAX 드롭률 +35%',
            3: '핏 감소 속도 -30%',
            4: '엘리트 처치 시 핏 +50%',
            5: '핏 오버플로우',
            6: 'BP 라운드 간 HP +10%',
            7: '엘리트 확률 2배',
            8: '핏 게이지 연쇄',
            9: '출혈 DoT 데미지 +30%',
            10: '핏 MAX 유지 보너스 2배',
            11: '보스 출현 시 핏 풀충전',
            12: '🏆 혈맹'
        },
        cargo_control: {
            1: '칸 HP +15%',
            2: '연료 +1/역',
            3: '칸 HP +15%',
            4: '정비공 수리량 +20%',
            5: '보급 크레이트 +50%',
            6: '역 정차 카드 리롤 1회',
            7: '칸 HP +20%',
            8: '연료 +2/역',
            9: '카드 장착 +1',
            10: '전설 카드 확률 2배',
            11: '칸 파괴 시 폭발 피해',
            12: '🏆 무적 열차'
        },
        dark_control: {
            1: '저주 슬롯 +1',
            2: '저주 긍정 효과 +20%',
            3: '탐색 속도 +15%',
            4: '저주 슬롯 +1',
            5: '부정 효과 -25%',
            6: '타일 시야 +1',
            7: '저주 슬롯 +1',
            8: '긍정 효과 +30%',
            9: '부정 효과 -30%',
            10: '비밀 타일 감지',
            11: '저주 선택적 해제',
            12: '🏆 어둠의 지배자'
        }
    };
}
