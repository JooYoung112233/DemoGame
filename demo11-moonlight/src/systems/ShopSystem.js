class ShopSystem {
    static generateCustomer(gameState) {
        const types = Object.entries(CUSTOMER_TYPES);
        const totalWeight = types.reduce((s, [, t]) => s + t.weight, 0);
        let roll = Math.random() * totalWeight;

        let chosen = types[0];
        for (const entry of types) {
            roll -= entry[1].weight;
            if (roll <= 0) { chosen = entry; break; }
        }

        const [typeId, typeData] = chosen;
        const budget = Phaser.Math.Between(typeData.budget.min, typeData.budget.max);

        if (gameState.reputation >= 10 && typeId === 'rich') {
            return { typeId, ...typeData, budget: budget + 50 };
        }

        return { typeId, ...typeData, budget };
    }

    static evaluateItem(customer, itemId, price, gameState) {
        const data = ITEM_DATA[itemId];
        if (!data) return { action: 'leave', reason: '알 수 없는 아이템' };

        const demandMult = gameState.getDemandMultiplier(data.category);
        let perceivedValue = data.basePrice * demandMult;

        if (customer.preferredCategories && customer.preferredCategories.includes(data.category)) {
            perceivedValue *= 1.3;
        }

        const maxWilling = perceivedValue * customer.priceTolerance;

        if (price > customer.budget) {
            return { action: 'leave', reason: '예산 초과' };
        }

        if (price <= maxWilling) {
            return { action: 'buy', reason: '적정 가격' };
        }

        if (Math.random() < customer.haggleChance) {
            const offer = Math.floor(price * (1 - customer.haggleDiscount));
            return { action: 'haggle', offer: Math.max(offer, Math.floor(perceivedValue * 0.8)), reason: '너무 비싸요' };
        }

        return { action: 'leave', reason: '너무 비싸다' };
    }

    static completeSale(gameState, itemId, price) {
        gameState.gold += price;
        gameState.totalEarned += price;
        gameState.totalSold++;

        const data = ITEM_DATA[itemId];
        const ratio = price / data.basePrice;

        if (ratio <= 1.3) {
            gameState.reputation = Math.min(gameState.reputation + 1, 50);
        } else {
            gameState.reputation = Math.max(gameState.reputation - 2, -20);
        }
    }

    static failedSale(gameState) {
        gameState.reputation = Math.max(gameState.reputation - 1, -20);
    }
}
