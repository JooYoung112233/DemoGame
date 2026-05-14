const CRAFT_COSTS = {
    common: 10,
    uncommon: 30,
    rare: 80,
    epic: 200,
    legendary: 500,
    ancient: 1500
};

class CraftSystem {
    canCombine(item1, item2) {
        if (!item1 || !item2) return { can: false, reason: 'missing_item' };
        if (item1.itemId !== item2.itemId) return { can: false, reason: 'different_items' };
        if (item1.rarity !== item2.rarity) return { can: false, reason: 'different_rarity' };
        if (item1.rarity === 'mythical') return { can: false, reason: 'max_rarity' };

        const rarityIdx = FARMING.RARITY_ORDER.indexOf(item1.rarity);
        if (rarityIdx < 0) return { can: false, reason: 'invalid_rarity' };

        const nextRarity = this._getNextRarity(item1.rarity);
        if (!nextRarity) return { can: false, reason: 'no_next_rarity' };

        return { can: true, nextRarity, cost: this.getCombineCost(item1.rarity) };
    }

    _getNextRarity(rarity) {
        if (rarity === 'legendary' || rarity === 'ancient') return 'mythical';
        const idx = FARMING.RARITY_ORDER.indexOf(rarity);
        if (idx < 0 || idx >= FARMING.RARITY_ORDER.length - 1) return null;
        return FARMING.RARITY_ORDER[idx + 1];
    }

    combine(item1, item2) {
        const check = this.canCombine(item1, item2);
        if (!check.can) return { result: 'fail', reason: check.reason };

        const cost = check.cost;
        const gold = StashManager.getGold();
        if (gold < cost) return { result: 'fail', reason: 'not_enough_gold' };

        StashManager.spendGold(cost);

        const newItem = {
            ...item1,
            rarity: check.nextRarity,
            enhanceLevel: 0,
            id: 'craft_' + Date.now() + '_' + Math.floor(Math.random() * 10000)
        };

        // For mythical, map to mythical-specific itemId if available
        if (check.nextRarity === 'mythical') {
            const mythMap = this._getMythicalMapping(item1.itemId);
            if (mythMap) {
                newItem.itemId = mythMap;
            }
        }

        return { result: 'success', newItem, cost };
    }

    _getMythicalMapping(baseItemId) {
        const reg = ItemRegistry.get(baseItemId);
        if (!reg) return null;
        const slot = reg.slot;
        const mythicals = {
            weapon: 'mythical_destroyer',
            armor: 'mythical_aegis',
            accessory: 'mythical_crown'
        };
        return mythicals[slot] || null;
    }

    getCombineCost(rarity) {
        return CRAFT_COSTS[rarity] || 100;
    }

    getPreview(item1, item2) {
        const check = this.canCombine(item1, item2);
        if (!check.can) return null;

        const nextRarity = check.nextRarity;
        let resultItemId = item1.itemId;
        if (nextRarity === 'mythical') {
            const mythMap = this._getMythicalMapping(item1.itemId);
            if (mythMap) resultItemId = mythMap;
        }

        const resultReg = ItemRegistry.get(resultItemId);
        return {
            itemId: resultItemId,
            name: resultReg ? resultReg.name : item1.name || '???',
            rarity: nextRarity,
            rarityInfo: FARMING.RARITIES[nextRarity],
            stats: resultReg ? resultReg.stats : null,
            cost: check.cost
        };
    }
}

const BP_CRAFT = new CraftSystem();
