/**
 * GuildHallManager — 길드 회관 트리 관리.
 * - 단계별 구매 (purchaseNextStage)
 * - 모든 카테고리의 누적 효과 조회 (getEffects)
 * - 기존 시설/시스템에서 이 효과를 참조해 적용
 */
class GuildHallManager {
    /** gameState 초기화 보장 */
    static ensureState(gs) {
        if (!gs.guildHall) gs.guildHall = {};
        GUILD_HALL_KEYS.forEach(k => {
            if (gs.guildHall[k] === undefined) gs.guildHall[k] = 0;
        });
        if (gs.guildReputation === undefined) gs.guildReputation = 0;
    }

    /** 카테고리의 현재 단계 (0 = 미구매) */
    static getStage(gs, categoryKey) {
        GuildHallManager.ensureState(gs);
        return gs.guildHall[categoryKey] || 0;
    }

    /** 다음 단계 구매 가능 여부 + 이유 */
    static canPurchase(gs, categoryKey) {
        GuildHallManager.ensureState(gs);
        const data = GUILD_HALL_DATA[categoryKey];
        if (!data) return { ok: false, reason: '없는 카테고리' };
        if (!isGuildHallUnlocked(gs, categoryKey)) {
            return { ok: false, reason: '해금 안 됨 (구역 클리어 필요)' };
        }
        const cur = GuildHallManager.getStage(gs, categoryKey);
        if (cur >= 12) return { ok: false, reason: '최대 단계' };
        const nextStage = cur + 1;
        const cost = getGuildHallStageCost(categoryKey, nextStage);
        if (gs.gold < cost) return { ok: false, reason: `골드 부족 (${cost}G 필요)` };
        const gate = getGuildHallStageGate(nextStage);
        if (gs.guildLevel < gate.guildLv) return { ok: false, reason: `길드 Lv.${gate.guildLv} 필요` };
        if ((gs.guildReputation || 0) < gate.rep) return { ok: false, reason: `평판 ${gate.rep} 필요` };
        return { ok: true, cost, nextStage };
    }

    /** 구매 — 성공 시 단계 +1, 골드 차감 */
    static purchaseNextStage(gs, categoryKey) {
        const check = GuildHallManager.canPurchase(gs, categoryKey);
        if (!check.ok) return { success: false, msg: check.reason };

        gs.gold -= check.cost;
        gs.guildHall[categoryKey] = check.nextStage;

        const data = GUILD_HALL_DATA[categoryKey];
        const stageData = data.stages[check.nextStage - 1];
        if (typeof GuildManager !== 'undefined' && GuildManager.addMessage) {
            GuildManager.addMessage(gs, `🏛 ${data.name} Lv.${check.nextStage} ${stageData.name} 해금!`);
        }
        return { success: true, stage: check.nextStage, stageData };
    }

    /**
     * 모든 카테고리의 누적 효과 객체 반환.
     * 같은 키는 SUM/MAX 정책 적용.
     */
    static getEffects(gs) {
        GuildHallManager.ensureState(gs);
        const effects = {
            // SUM
            subSlotsBonus: 0,
            mainPartyBonus: 0,
            rosterBonus: 0,
            storageBonus: 0,
            secureBonus: 0,
            restBonus: 0,
            returnStaminaBonus: 0,
            rumorSlots: 0,
            duplicateDispatch: 0,
            autoSell: 0,
            autoSurvive: 0,
            equipStorage: 0,
            // BOOL — true 우선
            storageTabs: false, swapDuringRun: false, autoEquip: false,
            autoEquipOnPickup: false, autoOptimizeDispatch: false,
            autoCollect: false, autoRedispatch: false, autoConsign: false,
            autoHealStamina: false, autoRecruit: false, fullAuto: false,
            dispatchPreview: false, showEnemyStats: false, eventPriority: false,
            marketForecast: false, showTraitWarning: false, omniscient: false,
            injuredCanDispatch: false, instantStaminaRecovery: false,
            // 0.0~1.0 — MAX
            injuryRecovery: 0, fallenItemRecovery: 0, dispatchStaminaRecovery: 0,
            reputationGain: 1, npcAffinityGain: 1, npcStockSpeed: 0,
            // BP/Cargo/BO 특수
            pitMaxBonus: 0, pitMaxDropBonus: 0, pitDecayReduction: 0, eliteKillCharge: 0,
            pitOverflowGold: false, bpRoundHeal: 0, eliteRateBonus: 1, eliteDropBonus: 0,
            pitChainBleed: false, bpBossPreCharge: 0, berserkerField: false,
            bpBossLegendary: 0, pitLordPermaMax: false,
            trainCarHpBonus: 0, crateMagnet: false, stationRepairBonus: 1,
            car6Armory: false, intruderPreview: false, stormShield: 0,
            autoRepair: 0, cargoHpDouble: false, stationMerchant: false,
            car7Comms: false, autoKillIntruder: false, goldenTrain: false,
            curseRateReduction: 0, curseVision: false, secretRoomBonus: 0,
            curseAbsorb: 0, trapDmgReduction: 0, trapReflect: 0,
            safeRetreat: 0, cursedItemReduction: 0, purificationSpring: false,
            bossClueBonus: 1, dualExplore: false, curseInfighting: 0, shadowLord: false,
            // 자동생존 쿨다운 (낮을수록 좋음) — MIN
            autoSurviveCooldown: 5
        };

        for (const cat of GUILD_HALL_KEYS) {
            const stage = gs.guildHall[cat] || 0;
            const data = GUILD_HALL_DATA[cat];
            // 1~stage 모든 단계의 효과를 적용 (마지막 효과가 우선 — desc상 누적된 값)
            for (let s = 1; s <= stage; s++) {
                const eff = data.stages[s - 1].effect;
                if (!eff) continue;
                for (const [key, val] of Object.entries(eff)) {
                    if (typeof val === 'boolean') {
                        effects[key] = effects[key] || val;
                    } else if (typeof val === 'number') {
                        // 정책: 누적 키는 SUM, 비율/배율 키는 MAX
                        if (['subSlotsBonus','mainPartyBonus','rosterBonus','storageBonus','secureBonus',
                             'restBonus','returnStaminaBonus','rumorSlots','duplicateDispatch','autoSell',
                             'autoSurvive','equipStorage','safeRetreat','autoRepair'].includes(key)) {
                            effects[key] = Math.max(effects[key] || 0, val);  // 단계별 효과는 누적값으로 정의됨 (덮어쓰기)
                        } else if (key === 'autoSurviveCooldown') {
                            effects[key] = Math.min(effects[key], val);
                        } else {
                            // 비율류 — 단계별 효과로 직접 정의 (덮어쓰기 = MAX)
                            if (val > effects[key]) effects[key] = val;
                        }
                    }
                }
            }
        }
        return effects;
    }

    /** 평판 추가 */
    static addReputation(gs, amount) {
        GuildHallManager.ensureState(gs);
        const mult = GuildHallManager.getEffects(gs).reputationGain || 1;
        gs.guildReputation = Math.min(100, (gs.guildReputation || 0) + Math.floor(amount * mult));
    }

    /** 총 누적 비용 (디버그/표시용) */
    static getTotalSpent(gs) {
        let total = 0;
        for (const cat of GUILD_HALL_KEYS) {
            const stage = gs.guildHall[cat] || 0;
            for (let s = 1; s <= stage; s++) {
                total += getGuildHallStageCost(cat, s);
            }
        }
        return total;
    }
}
