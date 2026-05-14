class ATBSystem {
    constructor(scene) {
        this.scene = scene;
        this.characters = [];
        this.battleOver = false;
        this.turnQueue = [];
        this.totalTurns = 0;
        this.processing = false;
    }

    addCharacter(c) { this.characters.push(c); }

    update(delta) {
        if (this.battleOver || this.processing) return;

        this.characters.forEach(c => {
            if (!c.alive) return;
            c.gauge += c.speed * (delta / 1000);
        });

        const ready = this.characters
            .filter(c => c.alive && c.gauge >= c.maxGauge)
            .sort((a, b) => b.gauge - a.gauge);

        if (ready.length > 0) {
            this.executeTurn(ready[0]);
        }

        this.characters.forEach(c => {
            if (c.alive) {
                const pct = Math.floor((c.gauge / c.maxGauge) * 100);
                c.turnIndicator.setText(pct >= 100 ? 'READY' : pct + '%');
                c.updateUI();
            }
        });

        this.checkBattleEnd();
    }

    executeTurn(char) {
        this.processing = true;
        char.gauge = 0;
        char.turnCount++;
        this.totalTurns++;

        char.drawCharacter(true);

        if (char.stunTurns > 0) {
            char.stunTurns--;
            this.scene.showSkillText(char, '기절!', 0xffcc00);
            this.scene.time.delayedCall(400, () => {
                char.drawCharacter(false);
                this.processing = false;
            });
            return;
        }

        this.processStatusEffects(char);
        if (!char.alive) { this.processing = false; return; }

        if (char.forcedTargetTurns > 0) char.forcedTargetTurns--;
        if (char.forcedTargetTurns <= 0) char.forcedTarget = null;

        const all = this.characters;
        char.currentTarget = char.findTarget(all);

        let usedSkill = false;
        for (const skill of char.skills) {
            if ((char.skillCooldowns[skill.name] || 0) <= 0) {
                skill.execute(char, all, this.scene);
                char.skillCooldowns[skill.name] = skill.cooldownTurns;
                usedSkill = true;
                break;
            }
        }

        if (!usedSkill && char.currentTarget) {
            let dmg = Math.max(1, char.atk - char.currentTarget.def);
            let isCrit = Math.random() < char.critRate;
            if (isCrit) dmg = Math.floor(dmg * char.critDmg);
            this.scene.applyDamage(char.currentTarget, dmg, isCrit, char);
        }

        for (const skill of char.skills) {
            if (char.skillCooldowns[skill.name] > 0) char.skillCooldowns[skill.name]--;
        }

        this.scene.time.delayedCall(350, () => {
            char.drawCharacter(false);
            this.processing = false;
        });
    }

    processStatusEffects(char) {
        char.statusEffects = char.statusEffects.filter(e => {
            if (e.type === 'bleed') {
                const dmg = e.dmgPerTurn * (e.stacks || 1);
                char.hp -= dmg;
                DamagePopup.show(this.scene, char.container.x, char.container.y - 20, dmg, 0xff6666, false);
                if (char.hp <= 0) { char.hp = 0; char.die(); }
                e.turnsLeft--;
                return e.turnsLeft > 0;
            }
            return true;
        });
    }

    checkBattleEnd() {
        const allies = this.characters.filter(c => c.team === 'ally' && c.alive);
        const enemies = this.characters.filter(c => c.team === 'enemy' && c.alive);
        if (enemies.length === 0) { this.battleOver = true; this.scene.onBattleEnd(true); }
        else if (allies.length === 0) { this.battleOver = true; this.scene.onBattleEnd(false); }
    }

    getStats() {
        return this.characters.map(c => ({
            name: c.name, team: c.team, alive: c.alive,
            hp: c.hp, maxHp: c.maxHp, damageDealt: c.totalDamageDealt,
            turns: c.turnCount
        }));
    }

    destroy() { this.characters.forEach(c => c.destroy()); this.characters = []; }
}
