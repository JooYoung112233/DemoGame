class Mercenary {
    static _nextId = 1;

    constructor(classKey, rarity, name, traits) {
        this.id = Mercenary._nextId++;
        this.classKey = classKey;
        this.rarity = rarity;
        this.name = name || generateMercName();
        this.level = 1;
        this.xp = 0;
        this.traits = traits || [];
        this.equipment = { weapon: null, armor: null, accessory: null };
        this.alive = true;
        this.injured = false;          // 부상 상태 (사망 보호) — HP 0 = 부상, 신전에서 회복
        this.injuredUntilMs = 0;       // 자동 회복 시각 (실시간 ms)
        this.zoneAffinity = { bloodpit: 0, cargo: 0, blackout: 0 };
        this.affinityLevel = { bloodpit: 0, cargo: 0, blackout: 0 };
        this.affinityXp = { bloodpit: 0, cargo: 0, blackout: 0 };
        this.affinityPoints = { bloodpit: 0, cargo: 0, blackout: 0 };
        this.affinityNodes = [];
        this._maxHp = this.getStats().hp;
        this.currentHp = this._maxHp;
        // === 스테미너 (0-100) — 출전 시 소모, 휴식/시간으로 회복 ===
        this.stamina = 100;
        this.maxStamina = 100;
    }

    /** 스테미너 회복 — 마을 복귀 시(시설별) / 시간 경과 */
    restStamina(amount) {
        this.stamina = Math.min(this.maxStamina, this.stamina + amount);
    }

    /** 스테미너 소모 — 출전 시 */
    drainStamina(amount) {
        this.stamina = Math.max(0, this.stamina - amount);
    }

    /** 출전 가능 여부 — 스테미너 20 이상 권장, 0이면 페널티 */
    canDeploy() {
        return this.alive && this.stamina > 0;
    }

    /** 스테미너 페널티 — 낮을수록 stat 감소 (전투에 적용) */
    getStaminaMultiplier() {
        if (this.stamina >= 60) return 1.0;
        if (this.stamina >= 30) return 0.9;
        if (this.stamina >= 10) return 0.75;
        return 0.6;  // 매우 피로
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

        if (this._trainingRef) {
            const t = this._trainingRef;
            if (t.hp)       stats.hp = Math.floor(stats.hp * (1 + t.hp * 0.03));
            if (t.atk)      stats.atk = Math.floor(stats.atk * (1 + t.atk * 0.03));
            if (t.survival) stats.def += t.survival * 2;
            if (t.recovery) stats.skillCooldown = Math.floor(stats.skillCooldown * (1 - t.recovery * 0.03));
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
        if (prev && prev.cursed) return null;
        this.equipment[item.slot] = item;
        this._maxHp = this.getStats().hp;
        this.currentHp = Math.min(this.currentHp, this._maxHp);
        return prev;
    }

    unequip(slot) {
        const item = this.equipment[slot];
        if (item && item.cursed) return null;
        this.equipment[slot] = null;
        this._maxHp = this.getStats().hp;
        this.currentHp = Math.min(this.currentHp, this._maxHp);
        return item;
    }

    fullHeal() {
        this._maxHp = this.getStats().hp;
        this.currentHp = this._maxHp;
        this.injured = false;
        this.injuredUntilMs = 0;
    }

    /**
     * 부상 처리 — 사망 대신 부상 상태로 (사망 보호).
     * @param {number} recoveryMs 자동 회복까지 걸리는 실시간 ms (기본 5분).
     */
    setInjured(recoveryMs = 5 * 60 * 1000) {
        this.injured = true;
        this.currentHp = Math.max(1, Math.floor(this._maxHp * 0.05));
        this.injuredUntilMs = Date.now() + recoveryMs;
    }

    /**
     * 자동 회복 체크 — 시간이 지났으면 회복. RosterScene/TownScene 진입 시 호출.
     */
    tickRecovery(now = Date.now()) {
        if (this.injured && now >= this.injuredUntilMs) {
            this.injured = false;
            this.injuredUntilMs = 0;
            this.currentHp = this._maxHp;
            return true;
        }
        return false;
    }

    /** 출전 가능 여부 — 부상 중이거나 사망이면 불가 */
    isDeployable() {
        return this.alive && !this.injured;
    }

    gainAffinityXp(zoneKey, amount) {
        if (!this.affinityXp) { this.affinityXp = { bloodpit: 0, cargo: 0, blackout: 0 }; }
        if (!this.affinityLevel) { this.affinityLevel = { bloodpit: 0, cargo: 0, blackout: 0 }; }
        if (!this.affinityPoints) { this.affinityPoints = { bloodpit: 0, cargo: 0, blackout: 0 }; }
        if (this.affinityLevel[zoneKey] >= 5) return false;
        this.affinityXp[zoneKey] += amount;
        let leveled = false;
        while (this.affinityLevel[zoneKey] < 5) {
            const needed = getAffinityXpNeeded(this.affinityLevel[zoneKey]);
            if (this.affinityXp[zoneKey] >= needed) {
                this.affinityXp[zoneKey] -= needed;
                this.affinityLevel[zoneKey]++;
                this.affinityPoints[zoneKey]++;
                leveled = true;
            } else break;
        }
        return leveled;
    }

    hasAffinityNode(nodeId) {
        return (this.affinityNodes || []).includes(nodeId);
    }

    canUnlockAffinityNode(zoneKey, nodeId) {
        if (!this.affinityPoints || !this.affinityNodes) return false;
        if (this.affinityNodes.includes(nodeId)) return false;
        if (this.affinityPoints[zoneKey] <= 0) return false;
        const tree = AFFINITY_TREES[zoneKey];
        if (!tree) return false;
        const node = tree.nodes[nodeId];
        if (!node) return false;
        if (this.affinityLevel[zoneKey] < node.level) return false;
        if (node.requires.length > 0 && !node.requires.every(r => this.affinityNodes.includes(r))) return false;
        if (node.branch) {
            const sameLevelNodes = Object.entries(tree.nodes).filter(([id, n]) => n.level === node.level && n.branch && n.branch !== node.branch);
            for (const [id] of sameLevelNodes) {
                if (this.affinityNodes.includes(id)) return false;
            }
        }
        return true;
    }

    unlockAffinityNode(zoneKey, nodeId) {
        if (!this.canUnlockAffinityNode(zoneKey, nodeId)) return false;
        this.affinityPoints[zoneKey]--;
        this.affinityNodes.push(nodeId);
        return true;
    }

    getAffinityEffects(zoneKey) {
        const effects = [];
        const tree = AFFINITY_TREES[zoneKey];
        if (!tree || !this.affinityNodes) return effects;
        for (const nodeId of this.affinityNodes) {
            const node = tree.nodes[nodeId];
            if (node) effects.push(node.effect);
        }
        return effects;
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
            injured: this.injured || false,
            injuredUntilMs: this.injuredUntilMs || 0,
            currentHp: this.currentHp,
            zoneAffinity: this.zoneAffinity,
            affinityLevel: this.affinityLevel,
            affinityXp: this.affinityXp,
            affinityPoints: this.affinityPoints,
            affinityNodes: this.affinityNodes,
            revivalToken: this.revivalToken || false,
            stamina: this.stamina !== undefined ? this.stamina : 100,
            maxStamina: this.maxStamina || 100
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
        merc.injured = data.injured || false;
        merc.injuredUntilMs = data.injuredUntilMs || 0;
        merc.currentHp = data.currentHp;
        merc.zoneAffinity = data.zoneAffinity || { bloodpit: 0, cargo: 0, blackout: 0 };
        merc.affinityLevel = data.affinityLevel || { bloodpit: 0, cargo: 0, blackout: 0 };
        merc.affinityXp = data.affinityXp || { bloodpit: 0, cargo: 0, blackout: 0 };
        merc.affinityPoints = data.affinityPoints || { bloodpit: 0, cargo: 0, blackout: 0 };
        merc.affinityNodes = data.affinityNodes || [];
        merc.revivalToken = data.revivalToken || false;
        merc.stamina = (data.stamina !== undefined) ? data.stamina : 100;
        merc.maxStamina = data.maxStamina || 100;
        merc._maxHp = merc.getStats().hp;
        if (Mercenary._nextId <= data.id) Mercenary._nextId = data.id + 1;
        return merc;
    }
}
