class Unit {
    static _nextId = 1;

    constructor(classKey) {
        this.id = Unit._nextId++;
        this.classKey = classKey;
        this.tier = UNIT_DATA[classKey].tier;
        this.traits = [];
        this.bonusHp = 0;
        this.bonusAtk = 0;
        this.bonusDef = 0;

        const base = UNIT_DATA[classKey];
        this.currentHp = base.hp;
    }

    getName() {
        return UNIT_DATA[this.classKey].name;
    }

    getBaseClass() {
        const data = UNIT_DATA[this.classKey];
        return data.baseClass || this.classKey;
    }

    getStats() {
        const base = UNIT_DATA[this.classKey];
        const stats = {
            hp: base.hp + this.bonusHp,
            atk: base.atk + this.bonusAtk,
            def: base.def + this.bonusDef,
            attackSpeed: base.attackSpeed,
            range: base.range,
            moveSpeed: base.moveSpeed,
            critRate: base.critRate,
            critDmg: base.critDmg,
            color: base.color,
            role: base.role,
            name: base.name,
            skills: base.skills || []
        };

        this.traits.forEach(trait => {
            if (trait.apply) trait.apply(stats);
        });

        return stats;
    }

    getAdvanceOptions() {
        const data = UNIT_DATA[this.classKey];
        return (data.advanceTo || []).map(key => ({
            key,
            data: UNIT_DATA[key]
        }));
    }

    canAdvance() {
        const data = UNIT_DATA[this.classKey];
        return data.advanceTo && data.advanceTo.length > 0;
    }

    getAdvanceCost(targetKey) {
        return UNIT_DATA[targetKey].advanceCost || 0;
    }

    advance(newClassKey) {
        const oldMaxHp = UNIT_DATA[this.classKey].hp + this.bonusHp;
        const hpRatio = this.currentHp / oldMaxHp;

        this.classKey = newClassKey;
        this.tier = UNIT_DATA[newClassKey].tier;

        const trait = rollRandomTrait();
        this.traits.push(trait);

        const newStats = this.getStats();
        this.currentHp = Math.floor(newStats.hp * hpRatio);

        return trait;
    }

    toCharacterData() {
        const stats = this.getStats();
        return {
            name: stats.name,
            hp: stats.hp,
            atk: stats.atk,
            def: stats.def,
            attackSpeed: stats.attackSpeed,
            range: stats.range,
            moveSpeed: stats.moveSpeed,
            critRate: stats.critRate,
            critDmg: stats.critDmg,
            color: stats.color,
            role: stats.role,
            skills: stats.skills,
            unitRef: this
        };
    }

    applyBattleResult(battleChar) {
        if (battleChar.alive) {
            const stats = this.getStats();
            this.currentHp = Math.min(stats.hp, Math.max(1, battleChar.hp));
        }
    }
}
