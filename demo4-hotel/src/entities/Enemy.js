class Enemy {
    constructor(data, scale, isElite) {
        this.data = data;
        this.name = data.name;
        this.color = data.color;
        this.size = data.size;
        this.isElite = isElite || false;

        this.maxHp = Math.floor(data.hp * scale * (isElite ? 2 : 1));
        this.hp = this.maxHp;
        this.coinValue = data.coinValue * (isElite ? 2 : 1);
        this.actions = data.actions;

        this.alive = true;
        this.defending = false;
        this.dodging = false;

        this.poisonStacks = 0;
        this.poisonTurnsLeft = 0;
    }

    chooseAction() {
        const totalWeight = this.actions.reduce((sum, a) => sum + a.weight, 0);
        let roll = Math.random() * totalWeight;

        for (const action of this.actions) {
            roll -= action.weight;
            if (roll <= 0) return action;
        }
        return this.actions[0];
    }

    executeAction(scale) {
        this.defending = false;
        this.dodging = false;

        const action = this.chooseAction();
        const result = {
            actionName: action.name,
            special: action.special || null,
            damage: 0
        };

        if (action.special === 'defend') {
            this.defending = true;
            return result;
        }

        if (action.special === 'dodge') {
            this.dodging = true;
            return result;
        }

        if (action.special === 'idle') {
            return result;
        }

        if (action.special === 'debuff') {
            result.debuffType = action.effect;
            return result;
        }

        if (action.dmgMin && action.dmgMax) {
            const min = Math.floor(action.dmgMin * scale);
            const max = Math.floor(action.dmgMax * scale);
            result.damage = min + Math.floor(Math.random() * (max - min + 1));
        }

        return result;
    }

    takeDamage(amount) {
        if (!this.alive) return 0;

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
