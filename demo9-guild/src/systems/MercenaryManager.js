class MercenaryManager {
    static generateRecruitPool(state) {
        const count = 3 + Math.floor(Math.random() * 3);
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
        state.roster.splice(idx, 1);
        GuildManager.addMessage(state, `${merc.name} 해고`);
        SaveManager.save(state);
        return { success: true, msg: `${merc.name}을(를) 해고했습니다`, droppedItems };
    }
}
