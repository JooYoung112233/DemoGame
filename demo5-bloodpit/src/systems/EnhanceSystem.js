const ENHANCE_CONFIG = {
    maxLevel: 15,
    breakStartLevel: 10,
    costs: {
        1: 20, 2: 30, 3: 50, 4: 80, 5: 120,
        6: 180, 7: 250, 8: 350, 9: 500, 10: 700,
        11: 1000, 12: 1500, 13: 2200, 14: 3500, 15: 5000
    },
    successRate: {
        1: 1.0, 2: 0.95, 3: 0.90, 4: 0.85, 5: 0.80,
        6: 0.70, 7: 0.60, 8: 0.50, 9: 0.40, 10: 0.30,
        11: 0.25, 12: 0.20, 13: 0.15, 14: 0.10, 15: 0.07
    },
    breakRate: {
        10: 0.05, 11: 0.10, 12: 0.20, 13: 0.35, 14: 0.50, 15: 0.60
    }
};

class EnhanceSystem {
    enhance(item) {
        const currentLevel = item.enhanceLevel || 0;
        const nextLevel = currentLevel + 1;
        if (nextLevel > ENHANCE_CONFIG.maxLevel) {
            return { result: 'max_level' };
        }

        const cost = this.getCost(item);
        const gold = StashManager.getGold();
        if (gold < cost) {
            return { result: 'not_enough_gold', cost };
        }

        StashManager.spendGold(cost);

        const success = Math.random() < ENHANCE_CONFIG.successRate[nextLevel];
        if (success) {
            item.enhanceLevel = nextLevel;
            return { result: 'success', level: nextLevel, cost };
        }

        if (nextLevel >= ENHANCE_CONFIG.breakStartLevel) {
            const breakChance = ENHANCE_CONFIG.breakRate[nextLevel] || 0;
            if (Math.random() < breakChance) {
                return { result: 'broken', level: currentLevel, cost };
            }
        }

        return { result: 'fail', level: currentLevel, cost };
    }

    getCost(item) {
        const nextLevel = (item.enhanceLevel || 0) + 1;
        return ENHANCE_CONFIG.costs[nextLevel] || 99999;
    }

    getSuccessRate(item) {
        const nextLevel = (item.enhanceLevel || 0) + 1;
        return ENHANCE_CONFIG.successRate[nextLevel] || 0;
    }

    getBreakRate(item) {
        const nextLevel = (item.enhanceLevel || 0) + 1;
        if (nextLevel < ENHANCE_CONFIG.breakStartLevel) return 0;
        return ENHANCE_CONFIG.breakRate[nextLevel] || 0;
    }

    getPreview(item) {
        const reg = ItemRegistry.get(item.itemId);
        if (!reg || !reg.stats) return null;
        const currentLevel = item.enhanceLevel || 0;
        const nextLevel = currentLevel + 1;
        const rarityMult = (FARMING.RARITY_STAT_MULT && FARMING.RARITY_STAT_MULT[item.rarity]) || 1;
        const currentMult = rarityMult * (1 + currentLevel * 0.05);
        const nextMult = rarityMult * (1 + nextLevel * 0.05);

        const current = {};
        const next = {};
        Object.keys(reg.stats).forEach(key => {
            current[key] = +(reg.stats[key] * currentMult).toFixed(2);
            next[key] = +(reg.stats[key] * nextMult).toFixed(2);
        });
        return { current, next, cost: this.getCost(item), successRate: this.getSuccessRate(item), breakRate: this.getBreakRate(item) };
    }

    canEnhance(item) {
        const reg = ItemRegistry.get(item.itemId);
        if (!reg || reg.category !== 'equipment') return false;
        if ((item.enhanceLevel || 0) >= ENHANCE_CONFIG.maxLevel) return false;
        return true;
    }
}

const BP_ENHANCE = new EnhanceSystem();
