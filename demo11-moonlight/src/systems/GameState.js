class GameState {
    constructor() {
        this.day = 1;
        this.gold = 100;
        this.reputation = 0;
        this.inventory = [];
        this.shopSlots = 6;
        this.shopDisplay = [];
        this.totalEarned = 0;
        this.totalSold = 0;
        this.demandCategory = null;
        this.craftUnlocked = false;

        this._giveStarterItems();
        this._rollDemand();
    }

    _giveStarterItems() {
        const starters = ['heal_herb', 'heal_herb', 'poison_herb', 'iron_ore', 'wood'];
        starters.forEach(id => this.inventory.push({ id, qty: 1 }));
        this._consolidateInventory();
    }

    _consolidateInventory() {
        const map = {};
        this.inventory.forEach(item => {
            if (map[item.id]) map[item.id].qty += item.qty;
            else map[item.id] = { id: item.id, qty: item.qty };
        });
        this.inventory = Object.values(map).filter(i => i.qty > 0);
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

    getEffectivePrice(itemId, setPrice) {
        const data = ITEM_DATA[itemId];
        if (!data) return setPrice;
        return setPrice;
    }

    getDemandMultiplier(category) {
        if (category === this.demandCategory) return 1.5;
        return 1.0;
    }

    advanceDay() {
        this.day++;
        this._rollDemand();
    }

    getUnlockedZones() {
        return Object.entries(ZONE_DATA)
            .filter(([, z]) => this.day >= z.unlockDay)
            .map(([id, z]) => ({ id, ...z }));
    }

    getCustomerCount() {
        const base = 8;
        const repBonus = Math.floor(this.reputation / 5);
        const dayBonus = Math.floor(this.day / 4);
        return Math.min(base + repBonus + dayBonus, 20);
    }
}
