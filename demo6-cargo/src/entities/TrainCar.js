class TrainCar {
    constructor(data, index) {
        this.id = data.id;
        this.name = data.name;
        this.maxHp = data.maxHp;
        this.hp = data.maxHp;
        this.color = data.color;
        this.desc = data.desc;
        this.function = data.function;
        this.lostEffect = data.lostEffect;
        this.healRate = data.healRate || 0;
        this.index = index;

        this.destroyed = false;
        this.module = null;
        this.doorBroken = false;
        this.invasionEnemies = [];
    }

    isOperational() {
        return !this.destroyed && this.hp > 0;
    }

    takeDamage(amount) {
        if (this.destroyed) return;
        const reduction = (this.module && this.module.dmgReduction) ? this.module.dmgReduction : 0;
        const actual = Math.floor(amount * (1 - reduction));
        this.hp = Math.max(0, this.hp - actual);
        if (this.hp <= 0) {
            this.destroyed = true;
        }
        return actual;
    }

    repair(amount) {
        if (this.hp >= this.maxHp) return 0;
        const healed = Math.min(amount, this.maxHp - this.hp);
        this.hp += healed;
        if (this.hp > 0) this.destroyed = false;
        return healed;
    }

    autoRepairTick() {
        if (this.module && this.module.autoRepair && !this.destroyed) {
            const heal = Math.floor(this.maxHp * this.module.autoRepair);
            return this.repair(heal);
        }
        return 0;
    }

    getAutoDefenseRate() {
        if (!this.module) return 0;
        return this.module.autoDefenseRate || 0;
    }

    installModule(mod) {
        this.module = mod;
        if (mod.hpBonus) {
            const bonus = Math.floor(this.maxHp * mod.hpBonus);
            this.maxHp += bonus;
            this.hp += bonus;
        }
    }
}
