class MomentumSystem {
    constructor() {
        this.value = 0;
        this.max = 100;
        this.tier = 0;
        this.decayRate = 2;
    }

    reset() {
        this.value = 0;
        this.tier = 0;
    }

    onKill() { this.add(8); }
    onCrit() { this.add(2); }
    onAllyDeath() { this.add(-25); }

    add(amount) {
        this.value = Math.max(0, Math.min(this.max, this.value + amount));
        this._updateTier();
    }

    update(delta) {
        if (this.value > 0) {
            this.value = Math.max(0, this.value - this.decayRate * (delta / 1000));
            this._updateTier();
        }
    }

    _updateTier() {
        const prev = this.tier;
        if (this.value >= this.max) this.tier = 2;
        else if (this.value >= 50) this.tier = 1;
        else this.tier = 0;
        return prev !== this.tier;
    }

    getAtkSpeedMult() {
        if (this.tier >= 1) return 0.85;
        return 1;
    }

    getAtkMult() {
        if (this.tier >= 2) return 1.20;
        return 1;
    }

    getSkillCdMult() {
        if (this.tier >= 2) return 0.5;
        return 1;
    }

    getTierLabel() {
        if (this.tier === 2) return '🔥 MAX';
        if (this.tier === 1) return '⚡ 가속';
        return '';
    }

    getTierColor() {
        if (this.tier === 2) return 0xff4422;
        if (this.tier === 1) return 0xffaa44;
        return 0x666666;
    }
}
