class Mercenary {
    static _nextId = 1;

    constructor(classKey, rarity, name, traits) {
        this.id = Mercenary._nextId++;
        this.classKey = classKey;
        this.rarity = rarity;
        this.name = name;
        this.level = 1;
        this.xp = 0;
        this.traits = traits || [];
        this.equipment = { weapon: null, armor: null, accessory: null };
        this.alive = true;
        this.zoneAffinity = { bloodpit: 0, cargo: 0, blackout: 0 };
        this._maxHp = this.getStats().hp;
        this.currentHp = this._maxHp;
    }

    getBaseClass() {
        return CLASS_DATA[this.classKey];
    }

    getHireCost() {
        return Math.floor(BASE_HIRE_COST * RARITY_DATA[this.rarity].hireCostMult);
    }

    getXpToNextLevel() {
        if (this.level >= 10) return Infinity;
        return 40 + this.level * 20;
    }

    getStats() {
        const base = this.getBaseClass();
        const rarityMult = RARITY_DATA[this.rarity].statMult;
        const growthMult = RARITY_DATA[this.rarity].growthMult;
        const lvl = this.level - 1;

        const stats = {
            hp: Math.floor((base.baseHp + base.growthHp * lvl * growthMult) * rarityMult),
            atk: Math.floor((base.baseAtk + base.growthAtk * lvl * growthMult) * rarityMult),
            def: Math.floor((base.baseDef + base.growthDef * lvl * growthMult) * rarityMult),
            attackSpeed: base.attackSpeed,
            range: base.range,
            moveSpeed: base.moveSpeed,
            critRate: base.critRate,
            critDmg: base.critDmg,
            skillCooldown: base.skillCooldown
        };

        for (const eq of Object.values(this.equipment)) {
            if (eq && eq.stats) {
                if (eq.stats.hp) stats.hp += eq.stats.hp;
                if (eq.stats.atk) stats.atk += eq.stats.atk;
                if (eq.stats.def) stats.def += eq.stats.def;
                if (eq.stats.critRate) stats.critRate += eq.stats.critRate;
                if (eq.stats.moveSpeed) stats.moveSpeed += eq.stats.moveSpeed;
            }
        }

        for (const trait of this.traits) {
            if (trait.apply) trait.apply(stats);
        }

        return stats;
    }

    gainXp(amount) {
        if (this.level >= 10) return false;
        this.xp += amount;
        let leveled = false;
        while (this.level < 10 && this.xp >= this.getXpToNextLevel()) {
            this.xp -= this.getXpToNextLevel();
            this.level++;
            leveled = true;
            const newStats = this.getStats();
            this._maxHp = newStats.hp;
            this.currentHp = Math.min(this.currentHp + Math.floor(newStats.hp * 0.2), newStats.hp);
        }
        return leveled;
    }

    equip(item) {
        if (!item || !item.slot) return null;
        const prev = this.equipment[item.slot];
        this.equipment[item.slot] = item;
        this._maxHp = this.getStats().hp;
        this.currentHp = Math.min(this.currentHp, this._maxHp);
        return prev;
    }

    unequip(slot) {
        const item = this.equipment[slot];
        this.equipment[slot] = null;
        this._maxHp = this.getStats().hp;
        this.currentHp = Math.min(this.currentHp, this._maxHp);
        return item;
    }

    fullHeal() {
        this._maxHp = this.getStats().hp;
        this.currentHp = this._maxHp;
    }

    toJSON() {
        return {
            id: this.id,
            classKey: this.classKey,
            rarity: this.rarity,
            name: this.name,
            level: this.level,
            xp: this.xp,
            traits: this.traits.map(t => ({ id: t.id, type: t.type })),
            equipment: this.equipment,
            alive: this.alive,
            currentHp: this.currentHp,
            zoneAffinity: this.zoneAffinity
        };
    }

    static fromJSON(data) {
        const allTraits = [...POSITIVE_TRAITS, ...NEGATIVE_TRAITS];
        const traitMap = {};
        allTraits.forEach(t => traitMap[t.id] = t);
        Object.values(LEGENDARY_TRAITS).forEach(t => traitMap[t.id] = t);

        const traits = data.traits.map(saved => {
            const full = traitMap[saved.id];
            if (full) return { ...full, type: saved.type };
            return saved;
        });

        const merc = new Mercenary(data.classKey, data.rarity, data.name, traits);
        merc.id = data.id;
        merc.level = data.level;
        merc.xp = data.xp;
        merc.equipment = data.equipment || { weapon: null, armor: null, accessory: null };
        merc.alive = data.alive;
        merc.currentHp = data.currentHp;
        merc.zoneAffinity = data.zoneAffinity || { bloodpit: 0, cargo: 0, blackout: 0 };
        merc._maxHp = merc.getStats().hp;
        if (Mercenary._nextId <= data.id) Mercenary._nextId = data.id + 1;
        return merc;
    }
}
