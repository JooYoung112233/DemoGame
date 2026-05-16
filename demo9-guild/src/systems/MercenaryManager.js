class MercenaryManager {
    static generateRecruitPool(state) {
        const baseCount = 3 + Math.floor(Math.random() * 3);
        // 길드 회관 인프라 — 모집 풀 보너스
        const ghBonus = (typeof GuildHallManager !== 'undefined')
            ? (GuildHallManager.getEffects(state).recruitPoolBonus || 0) : 0;
        const count = baseCount + ghBonus;
        const pool = [];
        for (let i = 0; i < count; i++) {
            pool.push(MercenaryManager.generateMerc(state.guildLevel));
        }
        state.recruitPool = pool;
    }

    static generateMerc(guildLevel) {
        const classKey = CLASS_KEYS[Math.floor(Math.random() * CLASS_KEYS.length)];
        const rarity = MercenaryManager.rollRarity(guildLevel);
        const name = generateMercName();
        const traits = getRandomTraits(rarity, classKey);
        return new Mercenary(classKey, rarity, name, traits);
    }

    static rollRarity(guildLevel) {
        const poolLevel = Math.min(guildLevel, 8);
        let pool = RARITY_POOL[poolLevel];
        if (!pool) pool = RARITY_POOL[1];

        const roll = Math.random() * 100;
        let cumulative = 0;
        for (const [rarity, weight] of Object.entries(pool)) {
            cumulative += weight;
            if (roll < cumulative) return rarity;
        }
        return 'common';
    }

    static hire(state, mercId) {
        const idx = state.recruitPool.findIndex(m => m.id === mercId);
        if (idx === -1) return { success: false, msg: '용병을 찾을 수 없습니다' };

        const merc = state.recruitPool[idx];
        const cost = merc.getHireCost();
        const maxRoster = GuildManager.getMaxRoster(state);

        if (state.roster.length >= maxRoster) {
            return { success: false, msg: `로스터가 가득 찼습니다 (${maxRoster}/${maxRoster})` };
        }
        if (state.gold < cost) {
            return { success: false, msg: `골드가 부족합니다 (${cost}G 필요)` };
        }

        state.gold -= cost;
        state.recruitPool.splice(idx, 1);
        merc.fullHeal();
        state.roster.push(merc);
        GuildManager.addMessage(state, `${merc.name} 고용! (-${cost}G)`);
        SaveManager.save(state);
        return { success: true, merc, msg: `${merc.name}을(를) 고용했습니다` };
    }

    static dismiss(state, mercId) {
        const idx = state.roster.findIndex(m => m.id === mercId);
        if (idx === -1) return { success: false, msg: '용병을 찾을 수 없습니다' };

        const merc = state.roster[idx];
        const droppedItems = [];
        for (const slot of ['weapon', 'armor', 'accessory']) {
            if (merc.equipment[slot]) {
                droppedItems.push(merc.equipment[slot]);
                merc.equipment[slot] = null;
            }
        }
        droppedItems.forEach(item => StorageManager.addItem(state, item));
        // 해고 보상 (퇴직금) — 레벨 × 50G × 희귀도 배율
        const rarityMult = { common: 1.0, uncommon: 1.2, rare: 1.5, epic: 2.0, legendary: 2.5 }[merc.rarity] || 1.0;
        const severance = Math.floor(50 * merc.level * rarityMult);
        GuildManager.addGold(state, severance);
        state.roster.splice(idx, 1);
        GuildManager.addMessage(state, `${merc.name} 해고 (+${severance}G 퇴직금)`);
        SaveManager.save(state);
        return { success: true, msg: `${merc.name} 해고 (+${severance}G)`, droppedItems, severance };
    }

    /** 일괄 해고 — common 등급 모두, 또는 Lv N 이하 모두 */
    static dismissAll(state, filter) {
        const toDismiss = state.roster.filter(filter);
        let total = 0;
        toDismiss.forEach(m => {
            const r = MercenaryManager.dismiss(state, m.id);
            if (r.severance) total += r.severance;
        });
        return { count: toDismiss.length, totalSeverance: total };
    }
}
