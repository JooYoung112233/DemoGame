class GuildManager {
    static createDefaultState() {
        return {
            guildLevel: 1,
            guildXp: 0,
            gold: 500,                   // v2: 200 → 500 (초반 모집 부담 완화)
            roster: [],
            storage: [],
            secureContainer: [],
            unlockedFacilities: ['recruit', 'storage', 'gate'],
            recruitPool: [],
            runCount: 0,
            zoneLevel: { bloodpit: 1, cargo: 0, blackout: 0 },
            trainingPoints: 0,
            training: { hp: 0, atk: 0, survival: 0, recovery: 0 },
            messages: [],
            // v2 신규 필드
            activeExpeditions: [],       // 진행 중 서브 파견
            pendingResults: []           // 미수령 파견 결과
        };
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
