/**
 * ExpeditionManager — 서브 파견 시간 경과형 시스템.
 * 실시간 ms 기반으로 진행되며 메인 전투와 별개로 작동.
 *
 * gameState.activeExpeditions = [
 *   { id, zoneKey, partyIds, startedAtMs, completesAtMs, simResult }
 * ]
 * gameState.pendingResults = [ ... 결과 미수령 ]
 */
class ExpeditionManager {
    static MAX_SLOTS_BASE = 2;
    static _nextId = 1;

    /**
     * 가용 슬롯 수 (길드 레벨 기반).
     * 길드회관 A(operations) 단계로 확장 가능 (추후).
     */
    static getMaxSlots(gs) {
        const tableByLv = { 1: 1, 2: 1, 3: 2, 4: 2, 5: 3, 6: 3, 7: 4, 8: 4 };
        const base = tableByLv[gs.guildLevel] || 1;
        return base;
    }

    /**
     * 파견 시작. zoneLevel을 명시하지 않으면 해금된 최고 레벨 자동 사용.
     * @returns {object|null} 파견 객체 또는 null (슬롯/해금 미충족)
     */
    static dispatch(gs, zoneKey, party, requestedLevel) {
        if (!gs.activeExpeditions) gs.activeExpeditions = [];
        if (gs.activeExpeditions.length >= ExpeditionManager.getMaxSlots(gs)) return null;
        if (!party || party.length < 1) return null;

        // 마스터리 체크 — 메인 3회 클리어한 레벨만 서브 가능
        const maxUnlocked = GuildManager.getMaxUnlockedSubLevel(gs, zoneKey);
        if (maxUnlocked === 0) return null;  // 해금된 레벨 없음

        const zoneLevel = requestedLevel
            ? Math.min(requestedLevel, maxUnlocked)
            : maxUnlocked;
        if (!GuildManager.isSubUnlocked(gs, zoneKey, zoneLevel)) return null;

        const partySize = party.length;
        const power = party.reduce((s, m) => s + ExpeditionManager._calcMercPower(m), 0);

        const recommended = ExpeditionManager._getRecommendedPower(zoneKey, zoneLevel);

        // 시간 = base × Lv 비례 / 파워 비율
        const baseTimeSec = { bloodpit: 90, cargo: 240, blackout: 480 }[zoneKey] || 120;
        const timeSec = baseTimeSec * Math.max(0.7, zoneLevel * 0.6) / Math.max(0.7, power / recommended);
        const durationMs = Math.max(15 * 1000, timeSec * 1000);  // 최소 15초

        const exp = {
            id: 'exp_' + (++ExpeditionManager._nextId),
            zoneKey,
            zoneLevel,
            partyIds: party.map(m => m.id),
            startedAtMs: Date.now(),
            completesAtMs: Date.now() + durationMs,
            durationMs
        };

        gs.activeExpeditions.push(exp);

        // 파견 중 용병들 임시 표시 (isOnExpedition 체크용)
        party.forEach(m => { m._onExpedition = exp.id; });

        return exp;
    }

    /**
     * 완료된 파견 처리. 결과를 pendingResults로 이동.
     * 마을 진입 시 / 주기적으로 호출.
     * @returns {Array} 새로 완료된 파견 결과 배열
     */
    static processCompleted(gs) {
        if (!gs.activeExpeditions) gs.activeExpeditions = [];
        if (!gs.pendingResults) gs.pendingResults = [];

        const now = Date.now();
        const completed = [];
        const remaining = [];

        for (const exp of gs.activeExpeditions) {
            if (now >= exp.completesAtMs) {
                const result = ExpeditionManager._resolveExpedition(gs, exp);
                gs.pendingResults.push(result);
                completed.push(result);
            } else {
                remaining.push(exp);
            }
        }

        gs.activeExpeditions = remaining;
        return completed;
    }

    /**
     * 파견 결과 수령 (보상 지급, 부상 처리).
     */
    static collectResult(gs, resultId) {
        if (!gs.pendingResults) return null;
        const idx = gs.pendingResults.findIndex(r => r.id === resultId);
        if (idx === -1) return null;

        const result = gs.pendingResults[idx];
        gs.pendingResults.splice(idx, 1);

        // 골드/XP
        if (typeof GuildManager !== 'undefined') {
            GuildManager.addGold(gs, result.goldEarned);
            GuildManager.addXp(gs, result.xpEarned);
            GuildManager.addMessage(gs, `파견 완료: ${result.zoneName} (${result.success ? '성공' : '실패'}) +${result.goldEarned}G`);
        } else {
            gs.gold += result.goldEarned;
            gs.guildXp += result.xpEarned;
        }

        // 용병 XP/친화도/부상
        const partyMercs = gs.roster.filter(m => result.partyIds.includes(m.id));
        partyMercs.forEach(merc => {
            merc.gainXp(result.mercXp);
            if (typeof merc.gainAffinityXp === 'function') {
                merc.gainAffinityXp(result.zoneKey, result.affinityXp);
            }
            // 사망 처리 → fallenMercs (신전 부활 가능)
            if (result.casualtyIds && result.casualtyIds.includes(merc.id)) {
                const items = GuildManager.markFallen(gs, merc);
                items.forEach(item => StorageManager.addItem(gs, item));
                const cost = GuildManager.getRevivalCost(merc);
                GuildManager.addMessage(gs, `💀 ${merc.name} 사망 (서브 파견) — ${cost}G로 부활 가능`);
            }
            // _onExpedition 해제
            delete merc._onExpedition;
        });

        // 장비 보관함에 추가
        if (typeof StorageManager !== 'undefined' && result.loot) {
            for (const item of result.loot) {
                if (!StorageManager.addItem(gs, item)) {
                    GuildManager.addMessage(gs, `보관함 가득! ${item.name} 손실`);
                }
            }
        }

        return result;
    }

    /**
     * 파견 시뮬 — RunSimulator를 활용하되 결과만 저장 (즉시 적용은 안 함).
     */
    static _resolveExpedition(gs, exp) {
        const party = gs.roster.filter(m => exp.partyIds.includes(m.id));
        const zone = ZONE_DATA[exp.zoneKey];

        const power = party.reduce((s, m) => s + ExpeditionManager._calcMercPower(m), 0);
        const recommended = ExpeditionManager._getRecommendedPower(exp.zoneKey, exp.zoneLevel);
        const ratio = power / recommended;

        // 시그모이드 성공률
        const k = 6, mid = 0.7;
        let successRate = 1 / (1 + Math.exp(-k * (ratio - mid)));
        // 친화도/시너지 보너스 (단순화)
        const affinityAvg = party.reduce((s, m) => s + (m.affinityLevel ? m.affinityLevel[exp.zoneKey] || 0 : 0), 0) / party.length / 10;
        successRate += affinityAvg * 0.10;
        successRate = Math.max(0.05, Math.min(0.99, successRate));

        const success = Math.random() < successRate;

        const goldBase = zone.baseGoldReward + exp.zoneLevel * 15;
        const xpBase = zone.baseXpReward + exp.zoneLevel * 8;
        const goldEarned = success
            ? Math.floor(goldBase * (0.8 + Math.random() * 0.4))
            : Math.floor(goldBase * 0.3);
        const xpEarned = success
            ? Math.floor(xpBase * (0.8 + Math.random() * 0.4))
            : Math.floor(xpBase * 0.3);
        const mercXp = Math.floor(xpEarned * 0.6);
        const affinityXp = success ? 15 + Math.floor(Math.random() * 15) : 5;

        // 부상 (사망 보호 — 영구사망 X)
        const casualtyIds = [];
        const deathChance = zone.deathChance * (1 - successRate) * 0.6;
        party.forEach(m => {
            if (Math.random() < deathChance) casualtyIds.push(m.id);
        });

        // 드롭
        const loot = [];
        const lootCount = success ? (zone.lootCount.min + Math.floor(Math.random() * (zone.lootCount.max - zone.lootCount.min + 1))) : Math.floor(Math.random() * 2);
        for (let i = 0; i < lootCount; i++) {
            if (typeof generateItem === 'function') {
                loot.push(generateItem(exp.zoneKey, gs.guildLevel));
            }
        }

        return {
            id: exp.id,
            zoneKey: exp.zoneKey,
            zoneName: zone.name,
            zoneLevel: exp.zoneLevel,
            partyIds: exp.partyIds,
            success,
            goldEarned,
            xpEarned,
            mercXp,
            affinityXp,
            casualtyIds,
            loot,
            completedAtMs: exp.completesAtMs
        };
    }

    /**
     * 단순 파워 계산 (RunSimulator v2 공식 단순화).
     */
    static _calcMercPower(merc) {
        const stats = merc.getStats();
        const offense = stats.atk * (1 + (stats.critRate || 0.1) * (stats.critDmg || 1.5));
        const defense = stats.hp * (1 + stats.def * 0.02);
        let power = Math.sqrt(offense * defense);
        power *= (1 + (merc.level - 1) * 0.05);
        const rarityBonus = { common: 1.0, uncommon: 1.05, rare: 1.1, epic: 1.2, legendary: 1.35 };
        power *= (rarityBonus[merc.rarity] || 1);
        return power;
    }

    static _getRecommendedPower(zoneKey, zoneLevel) {
        const base = { bloodpit: 200, cargo: 300, blackout: 450 }[zoneKey] || 200;
        return base * (1 + (zoneLevel - 1) * 0.25) * (1 + (zoneLevel - 1) * 0.03);
    }

    /**
     * 파견 진행률 0-1.
     */
    static getProgress(exp, now = Date.now()) {
        const total = exp.completesAtMs - exp.startedAtMs;
        const elapsed = now - exp.startedAtMs;
        return Math.max(0, Math.min(1, elapsed / total));
    }

    /**
     * 남은 시간 ms.
     */
    static getRemainingMs(exp, now = Date.now()) {
        return Math.max(0, exp.completesAtMs - now);
    }

    /**
     * 용병이 파견 중인지 체크.
     */
    static isOnExpedition(gs, mercId) {
        if (!gs.activeExpeditions) return false;
        return gs.activeExpeditions.some(e => e.partyIds.includes(mercId));
    }
}
