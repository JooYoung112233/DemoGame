class Boss {
    constructor(scale) {
        this.name = BOSS_DATA.name;
        this.color = BOSS_DATA.color;
        this.size = BOSS_DATA.size;
        this.scale = scale || 1;

        this.maxHp = Math.floor(BOSS_DATA.hp * this.scale);
        this.hp = this.maxHp;
        this.coinValue = BOSS_DATA.coinValue;
        this.phases = BOSS_DATA.phases;

        this.alive = true;
        this.phase = 0;
        this.defending = false;
        this.dodging = false;
        this.invulnerable = false;

        this.poisonStacks = 0;
        this.poisonTurnsLeft = 0;

        this.summonRequested = false;
    }

    getCurrentPhase() {
        const hpRatio = this.hp / this.maxHp;
        for (let i = this.phases.length - 1; i >= 0; i--) {
            if (hpRatio <= this.phases[i].hpThreshold) {
                return i;
            }
        }
        return 0;
    }

    chooseAction() {
        const phaseIndex = this.getCurrentPhase();
        if (phaseIndex > this.phase) {
            this.phase = phaseIndex;
            this.invulnerable = false;
        }

        const actions = this.phases[phaseIndex].actions;
        const totalWeight = actions.reduce((sum, a) => sum + a.weight, 0);
        let roll = Math.random() * totalWeight;

        for (const action of actions) {
            roll -= action.weight;
            if (roll <= 0) return action;
        }
        return actions[0];
    }

    executeAction() {
        this.defending = false;
        this.dodging = false;
        this.summonRequested = false;

        const action = this.chooseAction();
        const result = {
            actionName: action.name,
            special: action.special || null,
            damage: 0,
            phaseChanged: false
        };

        if (action.special === 'defend') {
            this.defending = true;
            return result;
        }

        if (action.special === 'summon') {
            this.summonRequested = true;
            return result;
        }

        if (action.special === 'heal') {
            const healAmt = Math.floor(this.maxHp * 0.05);
            this.hp = Math.min(this.hp + healAmt, this.maxHp);
            result.healing = healAmt;
            return result;
        }

        if (action.dmgMin && action.dmgMax) {
            const min = Math.floor(action.dmgMin * this.scale);
            const max = Math.floor(action.dmgMax * this.scale);
            result.damage = min + Math.floor(Math.random() * (max - min + 1));
        }

        return result;
    }

    takeDamage(amount) {
        if (!this.alive || this.invulnerable) return 0;

        if (this.dodging) {
            this.dodging = false;
            return 0;
        }

        if (this.defending) {
            amount = Math.floor(amount * 0.5);
        }

        this.hp -= amount;
        if (this.hp <= 0) {
            this.hp = 0;
            this.alive = false;
        }
        return amount;
    }

    applyPoison(dmg, duration) {
        this.poisonStacks = dmg;
        this.poisonTurnsLeft = duration;
    }

    tickPoison() {
        if (this.poisonTurnsLeft > 0 && this.poisonStacks > 0) {
            this.poisonTurnsLeft--;
            const dmg = this.poisonStacks;
            this.hp -= dmg;
            if (this.hp <= 0) {
                this.hp = 0;
                this.alive = false;
            }
            return dmg;
        }
        return 0;
    }

    onTurnStart() {
        this.defending = false;
        this.dodging = false;
    }
}
