class DangerSystem {
    constructor() {
        this.level = 0;
        this.maxLevel = 20;
        this.killCount = 0;
    }

    setLevel(level) {
        this.level = Math.min(level, this.maxLevel);
    }

    addKill() {
        this.killCount++;
        if (this.level < this.maxLevel) {
            this.level++;
        }
    }

    getEnemyScale() {
        return 1 + this.level * 0.08 + Math.max(0, this.level - 12) * 0.03;
    }

    getEnemyComposition() {
        const comp = [];
        const scale = this.getEnemyScale();

        if (this.level <= 3) {
            for (let i = 0; i < 3 + this.level; i++) comp.push(this.scaled('runner', scale));
        } else if (this.level <= 6) {
            for (let i = 0; i < 2 + Math.floor(this.level / 2); i++) comp.push(this.scaled('runner', scale));
            for (let i = 0; i < this.level - 2; i++) comp.push(this.scaled('bruiser', scale));
        } else if (this.level <= 9) {
            for (let i = 0; i < 3; i++) comp.push(this.scaled('runner', scale));
            for (let i = 0; i < 2; i++) comp.push(this.scaled('bruiser', scale));
            for (let i = 0; i < this.level - 5; i++) comp.push(this.scaled('spitter', scale));
            if (Math.random() < 0.3 * (this.level - 6)) comp.push(this.scaled('nest', scale));
        } else if (this.level === 10) {
            // mid-boss
            comp.push(this.scaledMiniBoss(scale));
            for (let i = 0; i < 3; i++) comp.push(this.scaled('runner', scale));
            for (let i = 0; i < 2; i++) comp.push(this.scaled('bruiser', scale));
            comp.push(this.scaled('spitter', scale));
        } else if (this.level <= 14) {
            for (let i = 0; i < 3; i++) comp.push(this.scaled('runner', scale));
            for (let i = 0; i < 2; i++) comp.push(this.scaled('bruiser', scale));
            for (let i = 0; i < 3; i++) comp.push(this.scaled('spitter', scale));
            comp.push(this.scaled('nest', scale));
            const eliteChance = 0.4 + (this.level - 11) * 0.15;
            if (Math.random() < eliteChance) {
                const eliteType = Math.random() < 0.5 ? 'elite_runner' : 'elite_bruiser';
                comp.push(this.scaledElite(eliteType, scale));
            }
        } else if (this.level <= 19) {
            for (let i = 0; i < 4; i++) comp.push(this.scaled('runner', scale));
            for (let i = 0; i < 3; i++) comp.push(this.scaled('bruiser', scale));
            for (let i = 0; i < 3; i++) comp.push(this.scaled('spitter', scale));
            comp.push(this.scaled('nest', scale));
            // always an elite
            const eliteType = Math.random() < 0.5 ? 'elite_runner' : 'elite_bruiser';
            comp.push(this.scaledElite(eliteType, scale));
            if (this.level >= 17 && Math.random() < 0.5) {
                comp.push(this.scaledElite('elite_bruiser', scale));
            }
        } else {
            // final boss round 20
            comp.push(this.scaledBoss(scale));
            for (let i = 0; i < 4; i++) comp.push(this.scaled('runner', scale));
            for (let i = 0; i < 2; i++) comp.push(this.scaled('spitter', scale));
            comp.push(this.scaled('nest', scale));
        }

        return comp;
    }

    scaled(type, scale) {
        const base = BP_ENEMIES[type];
        return {
            ...base,
            hp: Math.floor(base.hp * scale),
            atk: Math.floor(base.atk * scale),
            def: Math.floor(base.def * scale)
        };
    }

    scaledElite(type, scale) {
        const base = BP_ELITES[type];
        return {
            ...base,
            hp: Math.floor(base.hp * scale),
            atk: Math.floor(base.atk * scale),
            def: Math.floor(base.def * scale)
        };
    }

    scaledBoss(scale) {
        const base = BP_BOSS.pitlord;
        return {
            ...base,
            hp: Math.floor(base.hp * scale),
            atk: Math.floor(base.atk * scale),
            def: Math.floor(base.def * scale)
        };
    }

    scaledMiniBoss(scale) {
        return {
            name: '핏워든',
            type: 'miniboss',
            hp: Math.floor(1200 * scale),
            atk: Math.floor(38 * scale),
            def: Math.floor(12 * scale),
            attackSpeed: 1800,
            range: 70,
            moveSpeed: 60,
            critRate: 0.15,
            critDmg: 2.0,
            color: 0xcc4422,
            bleedChance: 0.2,
            dodgeRate: 0.05
        };
    }

    getEncounterModifier() {
        if (this.level <= 3) return null;
        const mods4to6 = [
            { name: '적 공격 강화', desc: '적 ATK +20%', apply(enemies) { enemies.forEach(e => { e.atk = Math.floor(e.atk * 1.2); }); } },
            { name: '적 회피 강화', desc: '적 회피 +15%', apply(enemies) { enemies.forEach(e => { e.dodgeRate = (e.dodgeRate || 0) + 0.15; }); } },
            { name: '출혈 시작', desc: '아군 전원 출혈 상태', applyAllies(allies) { allies.forEach(a => { a.bleeding = true; a.bleedTimer = 4000; a.bleedTickTimer = 1000; a.bleedDamage = Math.floor(a.maxHp * 0.02); }); } }
        ];
        const mods7to9 = [
            { name: '적 흡혈', desc: '적 흡혈 10%', apply(enemies) { enemies.forEach(e => { e.lifesteal = (e.lifesteal || 0) + 0.10; }); } },
            { name: '적 가시', desc: '적 가시 반사 8', apply(enemies) { enemies.forEach(e => { e.thorns = (e.thorns || 0) + 8; }); } },
            { name: '아군 약화', desc: '아군 ATK -15%', applyAllies(allies) { allies.forEach(a => { a.atk = Math.floor(a.atk * 0.85); }); } }
        ];
        const mods11to14 = [
            { name: '적 광폭', desc: '적 ATK +30%, 공속 +20%', apply(enemies) { enemies.forEach(e => { e.atk = Math.floor(e.atk * 1.3); e.attackSpeed = Math.floor(e.attackSpeed * 0.8); }); } },
            { name: '독안개', desc: '아군 전원 출혈 + DEF -20%', applyAllies(allies) { allies.forEach(a => { a.bleeding = true; a.bleedTimer = 6000; a.bleedTickTimer = 1000; a.bleedDamage = Math.floor(a.maxHp * 0.03); a.def = Math.floor(a.def * 0.8); }); } },
            { name: '적 재생', desc: '적 HP 1%/초 재생', apply(enemies) { enemies.forEach(e => { e.regenRate = 0.01; }); } }
        ];
        const mods15to19 = [
            { name: '혈의 의식', desc: '적 흡혈 15%, 가시 12', apply(enemies) { enemies.forEach(e => { e.lifesteal = (e.lifesteal || 0) + 0.15; e.thorns = (e.thorns || 0) + 12; }); } },
            { name: '죽음의 땅', desc: '아군 ATK -20%, 적 CRIT +15%', apply(enemies) { enemies.forEach(e => { e.critRate = Math.min(0.8, (e.critRate || 0) + 0.15); }); }, applyAllies(allies) { allies.forEach(a => { a.atk = Math.floor(a.atk * 0.8); }); } },
            { name: '엘리트 강화', desc: '적 전원 HP +30%', apply(enemies) { enemies.forEach(e => { e.hp = Math.floor(e.hp * 1.3); e.maxHp = e.hp; }); } }
        ];

        if (this.level === 10) return { name: '핏워든의 도전', desc: '미니보스 등장', apply() {} };
        if (this.level >= 20) return { name: '핏로드의 분노', desc: '적 ATK +20%, HP +15%', apply(enemies) { enemies.forEach(e => { e.atk = Math.floor(e.atk * 1.2); e.hp = Math.floor(e.hp * 1.15); e.maxHp = e.hp; }); } };

        let pool;
        if (this.level <= 6) pool = mods4to6;
        else if (this.level <= 9) pool = mods7to9;
        else if (this.level <= 14) pool = mods11to14;
        else pool = mods15to19;

        return pool[Math.floor(Math.random() * pool.length)];
    }

    isBossRound() {
        return this.level >= this.maxLevel;
    }

    isMiniBossRound() {
        return this.level === 10;
    }
}
