class GameState {
    constructor() {
        this.day = 1;
        this.gold = 80;
        this.reputation = 0;
        this.inventory = [];
        this.shopSlots = 6;
        this.craftSlots = 2;
        this.totalEarned = 0;
        this.totalSold = 0;
        this.demandCategory = null;
        this.pendingCommissions = [];
        this.commissionResults = [];
        this.activeOrders = [];
        this.workers = [];
        this.workerCostPerDay = 0;

        this.addItems([
            { id: 'heal_herb', qty: 3 },
            { id: 'wood', qty: 2 },
            { id: 'poison_herb', qty: 1 },
        ]);
        this._rollDemand();
    }

    addItems(items) {
        items.forEach(item => {
            const existing = this.inventory.find(i => i.id === item.id);
            if (existing) existing.qty += (item.qty || 1);
            else this.inventory.push({ id: item.id, qty: item.qty || 1 });
        });
    }

    removeItem(itemId, count = 1) {
        const item = this.inventory.find(i => i.id === itemId);
        if (!item || item.qty < count) return false;
        item.qty -= count;
        if (item.qty <= 0) this.inventory = this.inventory.filter(i => i.qty > 0);
        return true;
    }

    getItemCount(itemId) {
        const item = this.inventory.find(i => i.id === itemId);
        return item ? item.qty : 0;
    }

    _rollDemand() {
        const idx = Math.floor((this.day - 1) / DEMAND_CYCLE_DAYS) % DEMAND_CATEGORIES.length;
        this.demandCategory = DEMAND_CATEGORIES[idx];
    }

    getDemandMultiplier(category) {
        return category === this.demandCategory ? 1.5 : 1.0;
    }

    advanceDay() {
        this.day++;
        this._rollDemand();
        if (this.workers.length > 0) this.gold -= this.workerCostPerDay;

        this.activeOrders = this.activeOrders.filter(o => {
            if (this.day > o.deadlineDay) {
                this.reputation = Math.max(this.reputation - 3, -20);
                return false;
            }
            return true;
        });

        this.commissionResults = [];
        this.pendingCommissions.forEach(c => {
            this.commissionResults.push(ExpeditionSystem.run(c.zoneId, c.adventurer));
        });
        this.pendingCommissions = [];
    }

    generateOrders() {
        const newOrders = [];
        if (this.activeOrders.length >= ORDER_CONFIG.maxActiveOrders) return newOrders;
        let added = 0;
        for (const template of ORDER_TEMPLATES) {
            if (added >= ORDER_CONFIG.maxNewPerDay) break;
            if (this.activeOrders.length + newOrders.length >= ORDER_CONFIG.maxActiveOrders) break;
            if (Math.random() > ORDER_CONFIG.newOrderChance) continue;
            const recipe = RECIPE_DATA[template.item];
            if (recipe && this.day < recipe.unlockDay) continue;
            newOrders.push({ ...template, deadlineDay: this.day + template.deadline });
            added++;
        }
        return newOrders;
    }

    acceptOrder(order) { this.activeOrders.push(order); }

    fulfillOrder(idx) {
        const order = this.activeOrders[idx];
        if (!order || this.getItemCount(order.item) < order.qty) return false;
        this.removeItem(order.item, order.qty);
        this.gold += order.reward;
        this.totalEarned += order.reward;
        this.reputation = Math.min(this.reputation + 2, 50);
        this.activeOrders.splice(idx, 1);
        return true;
    }

    getUnlockedZones() {
        return Object.entries(ZONE_DATA)
            .filter(([, z]) => this.day >= z.unlockDay)
            .map(([id, z]) => ({ id, ...z }));
    }

    getAvailableAdventurers() {
        return ADVENTURER_DATA.filter(a => !a.unlockDay || this.day >= a.unlockDay);
    }

    getCustomerCount() {
        const base = 6;
        const repBonus = Math.floor(this.reputation / 5);
        const dayBonus = Math.floor(this.day / 5);
        return Math.min(Math.max(base + repBonus + dayBonus, 3), 18);
    }
}
