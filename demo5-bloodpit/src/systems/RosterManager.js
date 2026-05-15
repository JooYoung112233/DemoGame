class RosterManager {
    constructor() {
        this.roster = [];
        this.maxRoster = 10;
        this.demoId = 'demo5';
        this._nextId = 1;
    }

    init() {
        this.load();
        if (this.roster.length === 0) {
            ['warrior', 'rogue', 'priest', 'mage'].forEach(classKey => {
                const char = this._createCharacter(classKey, 1);
                const grade = this._rollStarterGrade();
                const m = grade.statMult;
                const v = () => 0.9 + Math.random() * 0.2;
                char.baseStats.hp = Math.floor(char.baseStats.hp * m * v());
                char.baseStats.atk = Math.floor(char.baseStats.atk * m * v());
                char.baseStats.def = Math.floor(char.baseStats.def * m * v());
                char.baseStats.critRate = Math.min(0.8, +(char.baseStats.critRate * (0.9 + m * 0.1 + Math.random() * 0.1)).toFixed(3));
                char.baseStats.dodgeRate = Math.min(0.6, +(char.baseStats.dodgeRate * (0.9 + m * 0.1 + Math.random() * 0.1)).toFixed(3));
                char.grade = grade.grade;
                char.gradeLabel = grade.label;
                char.gradeColor = grade.color;
                this.roster.push(char);
            });
            this.save();
        }
    }

    _rollStarterGrade() {
        const r = Math.random();
        if (r < 0.10) return { grade: 'rare', color: '#4488ff', label: '★희귀', statMult: 1.2 };
        if (r < 0.40) return { grade: 'uncommon', color: '#44ff88', label: '고급', statMult: 1.1 };
        return { grade: 'common', color: '#aaaaaa', label: '일반', statMult: 1.0 };
    }

    _createCharacter(classKey, level) {
        const base = BP_ALLIES[classKey];
        if (!base) return null;
        const name = this._generateName();
        const levelMult = 1 + (level - 1) * 0.08;
        const id = 'char_' + (this._nextId++);
        return {
            id,
            classKey,
            name,
            level,
            exp: 0,
            expToNext: this._expForLevel(level),
            baseStats: {
                hp: Math.floor(base.hp * levelMult),
                atk: Math.floor(base.atk * (1 + (level - 1) * 0.06)),
                def: Math.floor(base.def * (1 + (level - 1) * 0.04)),
                attackSpeed: base.attackSpeed,
                range: base.range,
                moveSpeed: base.moveSpeed,
                critRate: base.critRate,
                critDmg: base.critDmg,
                bleedChance: base.bleedChance,
                dodgeRate: base.dodgeRate
            },
            color: base.color,
            role: base.role,
            equipment: { weapon: null, armor: null, accessory: null },
            status: 'ready',
            recoveryRounds: 0,
            hireCost: this._calcHireCost(classKey, level)
        };
    }

    _generateName() {
        const p = BP_NAME_POOL.prefix[Math.floor(Math.random() * BP_NAME_POOL.prefix.length)];
        const s = BP_NAME_POOL.suffix[Math.floor(Math.random() * BP_NAME_POOL.suffix.length)];
        return p + ' ' + s;
    }

    _expForLevel(level) {
        return Math.floor(100 * Math.pow(1.3, level - 1));
    }

    _calcHireCost(classKey, level) {
        const baseCosts = {
            warrior: 60, rogue: 70, priest: 80, mage: 80,
            ranger: 75, paladin: 90, assassin: 100, shaman: 85
        };
        return Math.floor((baseCosts[classKey] || 70) * (1 + (level - 1) * 0.4));
    }

    _rollRecruitGrade() {
        const r = Math.random();
        if (r < 0.03) return { grade: 'legendary', color: '#ffaa00', label: '★전설', statMult: 1.5, costMult: 3.0 };
        if (r < 0.10) return { grade: 'epic', color: '#aa44ff', label: '★에픽', statMult: 1.35, costMult: 2.2 };
        if (r < 0.25) return { grade: 'rare', color: '#4488ff', label: '★희귀', statMult: 1.2, costMult: 1.6 };
        if (r < 0.55) return { grade: 'uncommon', color: '#44ff88', label: '고급', statMult: 1.1, costMult: 1.2 };
        return { grade: 'common', color: '#aaaaaa', label: '일반', statMult: 1.0, costMult: 1.0 };
    }

    generateRecruits(count) {
        count = count || 3;
        const recruits = [];
        for (let i = 0; i < count; i++) {
            const classKey = BP_CLASS_POOL[Math.floor(Math.random() * BP_CLASS_POOL.length)];
            const level = Math.random() < 0.3 ? 2 : 1;
            const char = this._createCharacter(classKey, level);
            const gradeInfo = this._rollRecruitGrade();
            const m = gradeInfo.statMult;
            const variance = () => 0.9 + Math.random() * 0.2;
            char.baseStats.hp = Math.floor(char.baseStats.hp * m * variance());
            char.baseStats.atk = Math.floor(char.baseStats.atk * m * variance());
            char.baseStats.def = Math.floor(char.baseStats.def * m * variance());
            char.baseStats.critRate = Math.min(0.8, +(char.baseStats.critRate * (0.9 + m * 0.1 + Math.random() * 0.1)).toFixed(3));
            char.baseStats.dodgeRate = Math.min(0.6, +(char.baseStats.dodgeRate * (0.9 + m * 0.1 + Math.random() * 0.1)).toFixed(3));
            char.grade = gradeInfo.grade;
            char.gradeLabel = gradeInfo.label;
            char.gradeColor = gradeInfo.color;
            char.hireCost = Math.floor(char.hireCost * gradeInfo.costMult);
            recruits.push(char);
        }
        return recruits;
    }

    hireCharacter(recruit) {
        if (this.roster.length >= this.maxRoster) return { success: false, reason: 'roster_full' };
        const gold = StashManager.getGold();
        if (gold < recruit.hireCost) return { success: false, reason: 'not_enough_gold' };
        StashManager.spendGold(recruit.hireCost);
        this.roster.push(recruit);
        this.save();
        return { success: true };
    }

    dismissCharacter(charId) {
        const idx = this.roster.findIndex(c => c.id === charId);
        if (idx === -1) return false;
        const char = this.roster[idx];
        // unequip all gear back to stash
        ['weapon', 'armor', 'accessory'].forEach(slot => {
            if (char.equipment[slot]) {
                StashManager.addToStash(char.equipment[slot]);
                char.equipment[slot] = null;
            }
        });
        this.roster.splice(idx, 1);
        this.save();
        return true;
    }

    markDead(charId) {
        const char = this.getById(charId);
        if (!char) return;
        char.status = 'dead';
        char.recoveryRounds = 3;
        // lose 10% exp
        const expLoss = Math.floor(char.expToNext * 0.1);
        char.exp = Math.max(0, char.exp - expLoss);
        this.save();
    }

    tickRecovery() {
        this.roster.forEach(c => {
            if (c.status === 'injured') {
                c.recoveryRounds--;
                if (c.recoveryRounds <= 0) {
                    c.status = 'ready';
                    c.recoveryRounds = 0;
                }
            }
        });
        this.save();
    }

    reviveCharacter(charId, cost) {
        const char = this.getById(charId);
        if (!char || char.status === 'ready') return { success: false };
        const gold = StashManager.getGold();
        if (gold < cost) return { success: false, reason: 'not_enough_gold' };
        StashManager.spendGold(cost);
        char.status = 'ready';
        char.recoveryRounds = 0;
        this.save();
        return { success: true };
    }

    getReviveCost(char) {
        if (!char || char.status === 'ready') return 0;
        return Math.floor(50 + char.level * 20);
    }

    addExp(charId, amount) {
        const char = this.getById(charId);
        if (!char) return;
        char.exp += amount;
        while (char.exp >= char.expToNext && char.level < 20) {
            char.exp -= char.expToNext;
            this.levelUp(char);
        }
        if (char.level >= 20) char.exp = 0;
        this.save();
    }

    levelUp(char) {
        char.level++;
        char.expToNext = this._expForLevel(char.level);
        const base = BP_ALLIES[char.classKey];
        const levelMult = 1 + (char.level - 1) * 0.08;
        char.baseStats.hp = Math.floor(base.hp * levelMult);
        char.baseStats.atk = Math.floor(base.atk * (1 + (char.level - 1) * 0.06));
        char.baseStats.def = Math.floor(base.def * (1 + (char.level - 1) * 0.04));
    }

    getPromotionPaths(charId) {
        const char = this.getById(charId);
        if (!char) return null;
        const promo = BP_PROMOTIONS[char.classKey];
        if (!promo) return null;
        return promo.paths.map(path => {
            const advBase = BP_ALLIES[path.advancedClass];
            if (!advBase) return null;
            return {
                currentClass: char.classKey,
                advancedClass: path.advancedClass,
                advancedName: advBase.name,
                tier: advBase.tier,
                cost: path.cost,
                minLevel: path.minLevel,
                label: path.label,
                currentStats: { ...char.baseStats },
                newStats: this._calcStatsForClass(path.advancedClass, char.level)
            };
        }).filter(p => p);
    }

    getPromotionInfo(charId) {
        const paths = this.getPromotionPaths(charId);
        if (!paths || paths.length === 0) return null;
        return paths[0];
    }

    canPromote(charId) {
        const char = this.getById(charId);
        if (!char) return { can: false, reason: 'not_found' };
        const promo = BP_PROMOTIONS[char.classKey];
        if (!promo) return { can: false, reason: 'no_promotion' };
        const minLevel = Math.min(...promo.paths.map(p => p.minLevel));
        const minCost = Math.min(...promo.paths.map(p => p.cost));
        if (char.level < minLevel) return { can: false, reason: 'level_low', minLevel };
        if (StashManager.getGold() < minCost) return { can: false, reason: 'not_enough_gold', cost: minCost };
        return { can: true };
    }

    promoteCharacter(charId, pathIndex) {
        const char = this.getById(charId);
        if (!char) return { success: false, reason: 'not_found' };
        const promo = BP_PROMOTIONS[char.classKey];
        if (!promo) return { success: false, reason: 'no_promotion' };
        pathIndex = pathIndex || 0;
        const path = promo.paths[pathIndex];
        if (!path) return { success: false, reason: 'invalid_path' };
        if (char.level < path.minLevel) return { success: false, reason: 'level_low' };
        if (StashManager.getGold() < path.cost) return { success: false, reason: 'not_enough_gold' };

        const advBase = BP_ALLIES[path.advancedClass];
        StashManager.spendGold(path.cost);
        char.classKey = path.advancedClass;
        char.color = advBase.color;
        char.role = advBase.role;
        char.baseStats = this._calcStatsForClass(path.advancedClass, char.level);
        this.save();
        return { success: true, newClass: path.advancedClass, newName: advBase.name, tier: advBase.tier };
    }

    _calcStatsForClass(classKey, level) {
        const base = BP_ALLIES[classKey];
        const levelMult = 1 + (level - 1) * 0.08;
        return {
            hp: Math.floor(base.hp * levelMult),
            atk: Math.floor(base.atk * (1 + (level - 1) * 0.06)),
            def: Math.floor(base.def * (1 + (level - 1) * 0.04)),
            attackSpeed: base.attackSpeed,
            range: base.range,
            moveSpeed: base.moveSpeed,
            critRate: base.critRate,
            critDmg: base.critDmg,
            bleedChance: base.bleedChance,
            dodgeRate: base.dodgeRate
        };
    }

    getById(charId) {
        return this.roster.find(c => c.id === charId) || null;
    }

    getAvailable() {
        return this.roster.filter(c => c.status === 'ready');
    }

    getEffectiveStats(char) {
        const stats = { ...char.baseStats };
        ['weapon', 'armor', 'accessory'].forEach(slot => {
            const item = char.equipment[slot];
            if (!item) return;
            const reg = ItemRegistry.get(item.itemId);
            if (!reg || !reg.stats) return;
            const rarityMult = (FARMING.RARITY_STAT_MULT && FARMING.RARITY_STAT_MULT[item.rarity]) || 1;
            const enhMult = 1 + (item.enhanceLevel || 0) * 0.07;
            Object.keys(reg.stats).forEach(key => {
                const val = reg.stats[key] * rarityMult * enhMult;
                if (key === 'hp' || key === 'atk' || key === 'def' || key === 'moveSpeed') {
                    stats[key] = (stats[key] || 0) + Math.floor(val);
                } else {
                    stats[key] = (stats[key] || 0) + val;
                }
            });
        });
        return stats;
    }

    save() {
        const data = {
            roster: this.roster,
            nextId: this._nextId
        };
        try {
            localStorage.setItem('BP_ROSTER', JSON.stringify(data));
        } catch (e) { /* ignore */ }
    }

    load() {
        try {
            const raw = localStorage.getItem('BP_ROSTER');
            if (raw) {
                const data = JSON.parse(raw);
                this.roster = data.roster || [];
                this._nextId = data.nextId || 1;
            }
        } catch (e) {
            this.roster = [];
        }
    }

    equipToCharacter(charId, slot, stashId) {
        const char = this.getById(charId);
        if (!char) return false;
        const stash = StashManager.getStash();
        const stashEntry = stash.find(s => s.id === stashId);
        if (!stashEntry) return false;
        const reg = ItemRegistry.get(stashEntry.itemId);
        if (!reg || reg.slot !== slot) return false;
        if (char.equipment[slot]) {
            this.unequipFromCharacter(charId, slot);
        }
        char.equipment[slot] = {
            itemId: stashEntry.itemId,
            rarity: stashEntry.rarity,
            enhanceLevel: stashEntry.enhanceLevel || 0
        };
        StashManager.removeFromStash(stashId);
        this.save();
        return true;
    }

    unequipFromCharacter(charId, slot) {
        const char = this.getById(charId);
        if (!char || !char.equipment[slot]) return false;
        const item = char.equipment[slot];
        StashManager.addToStash(item);
        char.equipment[slot] = null;
        this.save();
        return true;
    }

    reset() {
        this.roster = [];
        this._nextId = 1;
        localStorage.removeItem('BP_ROSTER');
    }
}

const BP_ROSTER = new RosterManager();
