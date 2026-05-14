const LootGenerator = {
    generate(demoId, difficulty, count) {
        const items = ItemRegistry.getByDemo(demoId);
        if (items.length === 0) return [];

        const drops = [];
        for (let i = 0; i < count; i++) {
            const rarity = this._rollRarity(difficulty);
            const pool = this._filterPool(items, rarity);
            if (pool.length === 0) continue;
            const base = pool[Math.floor(Math.random() * pool.length)];
            drops.push({ ...base, rarity });
        }
        return drops;
    },

    _rollRarity(difficulty) {
        const d = Math.max(0, Math.min(difficulty, 20));
        const r = Math.random();
        const ancientChance = d >= 15 ? Math.min(0.01 + (d - 15) * 0.015, 0.08) : 0;
        const legendChance = Math.min(0.01 + d * 0.015, 0.12);
        const epicChance = Math.min(0.03 + d * 0.025, 0.20);
        const rareChance = Math.min(0.08 + d * 0.035, 0.35);
        const uncommonChance = 0.30;

        if (r < ancientChance) return 'ancient';
        if (r < ancientChance + legendChance) return 'legendary';
        if (r < ancientChance + legendChance + epicChance) return 'epic';
        if (r < ancientChance + legendChance + epicChance + rareChance) return 'rare';
        if (r < ancientChance + legendChance + epicChance + rareChance + uncommonChance) return 'uncommon';
        return 'common';
    },

    _filterPool(items, rarity) {
        const rarityIdx = FARMING.RARITY_ORDER.indexOf(rarity);
        const droppable = items.filter(i => i.rarity !== 'mythical');
        const equipPool = droppable.filter(i => i.category === 'equipment');
        const matPool = droppable.filter(i => i.category === 'material');
        const conPool = droppable.filter(i => i.category === 'consumable');

        const roll = Math.random();
        let pool;
        if (roll < 0.35) {
            pool = matPool;
        } else if (roll < 0.70) {
            pool = equipPool;
        } else {
            pool = conPool;
        }

        if (pool.length === 0) pool = droppable;

        const baseRarityIdx = Math.max(0, rarityIdx - 1);
        const maxRarityIdx = Math.min(FARMING.RARITY_ORDER.length - 1, rarityIdx + 1);
        const filtered = pool.filter(i => {
            const idx = FARMING.RARITY_ORDER.indexOf(i.rarity);
            return idx >= baseRarityIdx && idx <= maxRarityIdx;
        });

        return filtered.length > 0 ? filtered : pool;
    }
};
