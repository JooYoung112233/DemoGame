class GuildManager {
    static createDefaultState() {
        return {
            guildLevel: 1,
            guildXp: 0,
            gold: 500,                   // v2: 200 → 500 (초반 모집 부담 완화)
            roster: [],
            storage: [],
            secureContainer: [],
            unlockedFacilities: ['recruit', 'storage', 'equipment', 'gate', 'guildHall'],
            recruitPool: [],
            runCount: 0,
            zoneLevel: { bloodpit: 1, cargo: 0, blackout: 0 },
            trainingPoints: 0,
            training: { hp: 0, atk: 0, survival: 0, recovery: 0 },
            messages: [],
            // v2 신규 필드
            activeExpeditions: [],       // 진행 중 서브 파견
            pendingResults: [],          // 미수령 파견 결과
            zoneClearCount: {},          // { 'bloodpit_1': 3, ... } 메인 클리어 누적 (서브 해금용)
            fallenMercs: [],             // 사망 용병 (부활 대기)
            savedParties: []             // 저장된 파티 편성 [{ id, name, mercIds }]
        };
    }

    /**
     * 사망 용병을 fallenMercs로 이동 (장비 보관, 부활 대기).
     */
    static markFallen(state, merc) {
        if (!state.fallenMercs) state.fallenMercs = [];
        // 장비 보관함으로 회수
        const items = [];
        for (const slot of ['weapon', 'armor', 'accessory']) {
            if (merc.equipment[slot]) items.push(merc.equipment[slot]);
            merc.equipment[slot] = null;
        }
        merc.alive = false;
        merc.currentHp = 0;
        state.fallenMercs.push(merc);
        state.roster = state.roster.filter(m => m.id !== merc.id);
        return items;
    }

    /**
     * 부활 비용 계산 — 레벨 × 클래스 × 희귀도.
     * 게임이 짧으니 적정 가격대 (Lv5 common ≈ 600G, Lv15 rare ≈ 2700G, Lv25 epic ≈ 6000G).
     */
    static getRevivalCost(merc) {
        const baseCost = 100 + merc.level * 120;
        const classCost = {
            warrior: 1.1, priest: 1.2, alchemist: 1.1,   // 탱커/힐러 비쌈
            rogue: 0.95, mage: 1.0, archer: 0.95         // dps 싸게
        }[merc.classKey] || 1.0;
        const rarityCost = {
            common: 1.0, uncommon: 1.15, rare: 1.3,
            epic: 1.6, legendary: 2.0
        }[merc.rarity] || 1.0;
        // HP 회복도 비용 영향 (낮은 hp = 더 큰 회복)
        return Math.floor(baseCost * classCost * rarityCost);
    }

    /**
     * 부활 처리. 골드 차감 후 fallenMercs → roster.
     */
    static reviveMerc(state, mercId) {
        if (!state.fallenMercs) return false;
        const merc = state.fallenMercs.find(m => m.id === mercId);
        if (!merc) return false;
        const cost = GuildManager.getRevivalCost(merc);
        if (state.gold < cost) return false;

        // 로스터 한도 체크
        const max = GuildManager.getMaxRoster(state);
        if (state.roster.length >= max) return false;

        GuildManager.spendGold(state, cost);
        merc.alive = true;
        merc.currentHp = Math.floor(merc.getStats().hp * 0.5);
        state.fallenMercs = state.fallenMercs.filter(m => m.id !== mercId);
        state.roster.push(merc);
        GuildManager.addMessage(state, `${merc.name} 부활! (-${cost}G)`);
        return true;
    }

    /** 서브 파견 해금 임계 — 메인 N번 클리어 시 해금 */
    static SUB_UNLOCK_CLEARS = 3;

    /** 특정 구역+레벨 메인 클리어 횟수 반환 */
    static getZoneClearCount(state, zoneKey, zoneLevel) {
        const k = `${zoneKey}_${zoneLevel}`;
        return (state.zoneClearCount && state.zoneClearCount[k]) || 0;
    }

    /** 메인 클리어 시 카운트 +1 (RunResultScene에서 호출) */
    static incrementZoneClear(state, zoneKey, zoneLevel) {
        if (!state.zoneClearCount) state.zoneClearCount = {};
        const k = `${zoneKey}_${zoneLevel}`;
        state.zoneClearCount[k] = (state.zoneClearCount[k] || 0) + 1;
        // 해금 도달 시 메시지
        if (state.zoneClearCount[k] === GuildManager.SUB_UNLOCK_CLEARS) {
            const zone = (typeof ZONE_DATA !== 'undefined') ? ZONE_DATA[zoneKey] : null;
            const name = zone ? zone.name : zoneKey;
            GuildManager.addMessage(state, `🎯 ${name} Lv.${zoneLevel} 서브 파견 해금!`);
        }
        return state.zoneClearCount[k];
    }

    /** 서브 파견 가능 여부 (해금됐는지) */
    static isSubUnlocked(state, zoneKey, zoneLevel) {
        return GuildManager.getZoneClearCount(state, zoneKey, zoneLevel) >= GuildManager.SUB_UNLOCK_CLEARS;
    }

    /** 구역의 서브 파견 가능한 최대 레벨 (해금된 것 중 최고) */
    static getMaxUnlockedSubLevel(state, zoneKey) {
        const maxZoneLv = state.zoneLevel[zoneKey] || 0;
        for (let lv = maxZoneLv; lv >= 1; lv--) {
            if (GuildManager.isSubUnlocked(state, zoneKey, lv)) return lv;
        }
        return 0;
    }

    static addGold(state, amount) {
        state.gold += amount;
    }

    static spendGold(state, amount) {
        if (state.gold < amount) return false;
        state.gold -= amount;
        return true;
    }

    static addXp(state, amount) {
        if (state.guildLevel >= 8) return false;
        state.guildXp += amount;
        let leveled = false;
        while (state.guildLevel < 8) {
            const needed = GUILD_LEVEL_XP[state.guildLevel];
            if (state.guildXp >= needed) {
                state.guildXp -= needed;
                state.guildLevel++;
                state.trainingPoints++;
                leveled = true;
                GuildManager.addMessage(state, `길드 레벨 ${state.guildLevel} 달성!`);
            } else {
                break;
            }
        }
        return leveled;
    }

    static getXpToNextLevel(state) {
        if (state.guildLevel >= 8) return Infinity;
        return GUILD_LEVEL_XP[state.guildLevel];
    }

    static getMaxRoster(state) {
        const limits = ROSTER_LIMITS[state.guildLevel] || ROSTER_LIMITS[8];
        return limits.max;
    }

    static getMaxDeploy(state) {
        const limits = ROSTER_LIMITS[state.guildLevel] || ROSTER_LIMITS[8];
        return limits.deploy;
    }

    static canUnlockFacility(state, facilityKey) {
        const fac = FACILITY_DATA[facilityKey];
        if (!fac) return false;
        if (state.unlockedFacilities.includes(facilityKey)) return false;
        if (state.guildLevel < fac.unlockLevel) return false;
        if (state.gold < fac.cost) return false;
        return true;
    }

    static unlockFacility(state, facilityKey) {
        const fac = FACILITY_DATA[facilityKey];
        if (!GuildManager.canUnlockFacility(state, facilityKey)) return false;
        state.gold -= fac.cost;
        state.unlockedFacilities.push(facilityKey);
        GuildManager.addMessage(state, `${fac.name} 해금!`);

        if (facilityKey === 'auction' && state.zoneLevel.cargo === 0) {
            state.zoneLevel.cargo = 1;
            GuildManager.addMessage(state, 'Cargo 구역 해금!');
        }
        if (facilityKey === 'temple' && state.zoneLevel.blackout === 0) {
            state.zoneLevel.blackout = 1;
            GuildManager.addMessage(state, 'Blackout 구역 해금!');
        }

        SaveManager.save(state);
        return true;
    }

    static isFacilityUnlocked(state, facilityKey) {
        return state.unlockedFacilities.includes(facilityKey);
    }

    static getStorageCapacity(state) {
        return state.unlockedFacilities.includes('vault') ? 20 : 12;
    }

    static getSecureContainerCapacity(state) {
        let base = 2;
        if (state.training.survival) base += state.training.survival;
        if (state.unlockedFacilities.includes('vault')) base += 2;
        return base;
    }

    static addMessage(state, msg) {
        state.messages = state.messages || [];
        state.messages.unshift(msg);
        if (state.messages.length > 20) state.messages.length = 20;
    }
}
