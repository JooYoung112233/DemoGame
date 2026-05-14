class Player {
    constructor(runState) {
        this.runState = runState;
        this.hp = runState.hp;
        this.maxHp = runState.maxHp;
        this.stamina = runState.stamina;
        this.maxStamina = runState.maxStamina;
        this.weapon = runState.weapon;

        this.critRate = runState.critRate || 0.05;
        this.critDmg = runState.critDmg || 1.5;
        this.bonusDmg = runState.bonusDmg || 0;
        this.bonusHitRate = runState.bonusHitRate || 0;
        this.bonusDodge = runState.bonusDodge || 0;
        this.defendReduction = runState.defendReduction || 0.5;
        this.staminaRegen = runState.staminaRegen || 0;
        this.staminaDiscount = runState.staminaDiscount || 0;
        this.lifesteal = runState.lifesteal || 0;
        this.poisonDmg = runState.poisonDmg || 0;
        this.poisonDuration = runState.poisonDuration || 0;
        this.counterRate = runState.counterRate || 0;
        this.counterDmgMin = runState.counterDmgMin || 0;
        this.counterDmgMax = runState.counterDmgMax || 0;
        this.focusMultiplier = runState.focusMultiplier || 2;
        this.desperatePower = runState.desperatePower || false;
        this.onKillHeal = runState.onKillHeal || 0;
        this.dodgeCounter = runState.dodgeCounter || false;
        this.dodgeCounterMin = runState.dodgeCounterMin || 0;
        this.dodgeCounterMax = runState.dodgeCounterMax || 0;
        this.hasShield = runState.hasShield || false;
        this.shieldActive = false;

        this.alive = true;
        this.focused = false;
        this.defending = false;
        this.dodging = false;

        this.turnDebuffs = {};
    }

    getAction(actionId) {
        return this.weapon.actions.find(a => a.id === actionId);
    }

    getEffectiveHitRate(action) {
        let rate = action.hitRate + this.bonusHitRate;
        if (action.special === 'dodge') {
            rate = action.hitRate + this.bonusDodge;
        }
        if (this.turnDebuffs.hitDown) {
            rate -= 0.10;
        }
        if (this.stamina <= this.maxStamina * 0.2) {
            rate -= 0.10;
        }
        return Math.max(0.05, Math.min(0.99, rate));
    }

    getEffectiveStaminaCost(action) {
        if (action.staminaCost === 0) return 0;
        return Math.max(1, action.staminaCost - this.staminaDiscount);
    }

    canUseAction(action) {
        return this.stamina >= this.getEffectiveStaminaCost(action);
    }

    executeAction(actionId) {
        const action = this.getAction(actionId);
        if (!action) return null;

        const cost = this.getEffectiveStaminaCost(action);
        this.stamina -= cost;

        this.defending = false;
        this.dodging = false;

        const hitRate = this.getEffectiveHitRate(action);
        const roll = Math.random();
        const success = roll < hitRate;

        const result = {
            actionName: action.name,
            actionId: action.id,
            special: action.special,
            success,
            roll: Math.floor(roll * 100),
            needed: Math.floor(hitRate * 100),
            damage: 0,
            isCrit: false,
            healing: 0,
            poisonApplied: false
        };

        if (action.special === 'rest') {
            const restAmount = 15;
            this.stamina = Math.min(this.stamina + restAmount, this.maxStamina);
            result.success = true;
            result.staminaRecovered = restAmount;
            return result;
        }

        if (action.special === 'defend') {
            this.defending = true;
            result.success = true;
            return result;
        }

        if (action.special === 'dodge') {
            this.dodging = success;
            return result;
        }

        if (action.special === 'focus') {
            if (success) {
                this.focused = true;
            }
            return result;
        }

        if (success && action.dmgMin > 0) {
            let dmg = action.dmgMin + Math.floor(Math.random() * (action.dmgMax - action.dmgMin + 1));
            dmg += this.bonusDmg;

            if (this.focused) {
                dmg = Math.floor(dmg * this.focusMultiplier);
                this.focused = false;
            }

            if (this.desperatePower && this.hp <= this.maxHp * 0.3) {
                dmg = Math.floor(dmg * 1.5);
            }

            if (Math.random() < this.critRate) {
                dmg = Math.floor(dmg * this.critDmg);
                result.isCrit = true;
            }

            result.damage = dmg;

            if (this.lifesteal > 0) {
                result.healing = Math.max(1, Math.floor(dmg * this.lifesteal));
                this.heal(result.healing);
            }

            if (this.poisonDmg > 0) {
                result.poisonApplied = true;
            }
        } else if (!success) {
            this.focused = false;
        }

        return result;
    }

    takeDamage(amount) {
        if (!this.alive) return 0;

        if (this.shieldActive) {
            this.shieldActive = false;
            return -1;
        }

        if (this.dodging) {
            return 0;
        }

        if (this.defending) {
            amount = Math.floor(amount * (1 - this.defendReduction));
        }

        this.hp -= amount;
        if (this.hp <= 0) {
            this.hp = 0;
            this.alive = false;
        }
        this.syncToRunState();
        return amount;
    }

    heal(amount) {
        this.hp = Math.min(this.hp + amount, this.maxHp);
        this.syncToRunState();
    }

    regenStamina() {
        if (this.staminaRegen > 0) {
            this.stamina = Math.min(this.stamina + this.staminaRegen, this.maxStamina);
        }
    }

    onTurnStart() {
        this.defending = false;
        this.dodging = false;
        this.regenStamina();
    }

    syncToRunState() {
        this.runState.hp = this.hp;
        this.runState.stamina = this.stamina;
    }

    syncFromRunState() {
        this.hp = this.runState.hp;
        this.maxHp = this.runState.maxHp;
        this.stamina = this.runState.stamina;
        this.maxStamina = this.runState.maxStamina;
        this.critRate = this.runState.critRate || 0.05;
        this.critDmg = this.runState.critDmg || 1.5;
        this.bonusDmg = this.runState.bonusDmg || 0;
        this.bonusHitRate = this.runState.bonusHitRate || 0;
        this.bonusDodge = this.runState.bonusDodge || 0;
        this.defendReduction = this.runState.defendReduction || 0.5;
        this.staminaRegen = this.runState.staminaRegen || 0;
        this.staminaDiscount = this.runState.staminaDiscount || 0;
        this.lifesteal = this.runState.lifesteal || 0;
        this.poisonDmg = this.runState.poisonDmg || 0;
        this.poisonDuration = this.runState.poisonDuration || 0;
        this.counterRate = this.runState.counterRate || 0;
        this.focusMultiplier = this.runState.focusMultiplier || 2;
        this.desperatePower = this.runState.desperatePower || false;
        this.onKillHeal = this.runState.onKillHeal || 0;
        this.hasShield = this.runState.hasShield || false;
        this.dodgeCounter = this.runState.dodgeCounter || false;
        this.dodgeCounterMin = this.runState.dodgeCounterMin || 0;
        this.dodgeCounterMax = this.runState.dodgeCounterMax || 0;
    }
}
