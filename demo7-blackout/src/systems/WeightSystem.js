class WeightSystem {
    constructor() {
        this.maxCapacity = 40;
        this.inventory = [];
    }

    getCurrentWeight() {
        return this.inventory.reduce((sum, item) => sum + item.weight, 0);
    }

    getWeightRatio() {
        return this.getCurrentWeight() / this.maxCapacity;
    }

    canCarry(item) {
        return this.getCurrentWeight() + item.weight <= this.maxCapacity;
    }

    addItem(item) {
        this.inventory.push(item);
    }

    removeItem(index) {
        return this.inventory.splice(index, 1)[0];
    }

    removeRandom() {
        if (this.inventory.length === 0) return null;
        const idx = Math.floor(Math.random() * this.inventory.length);
        return this.removeItem(idx);
    }

    getFleeChance() {
        const ratio = this.getWeightRatio();
        if (ratio <= 0.3) return 0.90;
        if (ratio <= 0.6) return 0.70;
        if (ratio <= 0.8) return 0.40;
        return 0.15;
    }

    getEscapeChance(tension) {
        const ratio = this.getWeightRatio();
        let base;
        if (ratio <= 0.3) base = 0.95;
        else if (ratio <= 0.6) base = 0.80;
        else if (ratio <= 0.8) base = 0.50;
        else base = 0.20;
        return Math.max(0.05, base - tension * 0.05);
    }

    getTotalValue() {
        return this.inventory.reduce((sum, item) => sum + item.value, 0);
    }
}
