class Character {
    constructor(scene, data, team, x, y) {
        this.scene = scene;
        this.name = data.name;
        this.team = team;
        this.alive = true;
        this.unitRef = data.unitRef || null;

        this.maxHp = data.hp;
        this.hp = data.hp;
        this.atk = data.atk;
        this.def = data.def;
        this.attackSpeed = data.attackSpeed;
        this.range = data.range;
        this.moveSpeed = data.moveSpeed;
        this.critRate = data.critRate;
        this.critDmg = data.critDmg;
        this.role = data.role;
        this.color = data.color;

        this.attackTimer = 0;
        this.target = null;
        this.statusEffects = [];
        this.skills = [];
        this.skillTimers = {};
        this.totalDamageDealt = 0;
        this.totalHealDone = 0;
        this.plunderBonus = 0;
        this.animState = 'idle';
        this.animFrame = 0;
        this.animTimer = 0;
        this.attackAnimTimer = 0;

        this.container = scene.add.container(x, y);
        this.container.setDepth(5);
        this.drawCharacter();

        this.healthBar = new HealthBar(scene, x, y - 30, 40, 5, this.maxHp);

        this.nameText = scene.add.text(x, y - 40, data.name, {
            fontSize: '10px', fontFamily: 'monospace',
            color: '#ffffff', stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5).setDepth(15);
    }

    drawCharacter() {
        if (!this.gfx) {
            this.gfx = this.scene.add.graphics();
            this.container.add(this.gfx);
        }
        this.gfx.clear();

        const r = (this.color >> 16) & 0xff;
        const g = (this.color >> 8) & 0xff;
        const b = this.color & 0xff;
        const dark = Phaser.Display.Color.GetColor(Math.floor(r * 0.6), Math.floor(g * 0.6), Math.floor(b * 0.6));
        const light = Phaser.Display.Color.GetColor(Math.min(255, r + 60), Math.min(255, g + 60), Math.min(255, b + 60));

        const bounce = (this.animFrame % 2 === 0) ? 0 : -2;
        const flip = this.team === 'enemy' ? -1 : 1;
        const attacking = this.animState === 'attack';

        this.gfx.fillStyle(0x000000, 0.3);
        this.gfx.fillEllipse(0, 14, 20, 6);

        this.gfx.fillStyle(this.color);
        this.gfx.fillRect(-7, -4 + bounce, 14, 14);
        this.gfx.fillStyle(dark);
        this.gfx.fillRect(-7, -4 + bounce, 14, 3);

        this.gfx.fillStyle(0xffcc99);
        this.gfx.fillRect(-4, -14 + bounce, 8, 8);

        this.gfx.fillStyle(0xffffff);
        this.gfx.fillRect(flip * 1, -12 + bounce, 3, 3);
        this.gfx.fillStyle(0x333333);
        this.gfx.fillRect(flip * 2, -12 + bounce, 2, 2);

        this.gfx.fillStyle(dark);
        this.gfx.fillRect(-5, -16 + bounce, 10, 3);

        const legOff = this.animState === 'walk' ? (this.animFrame % 2 === 0 ? -1 : 1) : 0;
        this.gfx.fillStyle(dark);
        this.gfx.fillRect(-4, 10 + bounce, 3, 5);
        this.gfx.fillRect(2 + legOff, 10 + bounce, 3, 5);

        this.gfx.fillStyle(0x664422);
        this.gfx.fillRect(-4, 14 + bounce, 4, 2);
        this.gfx.fillRect(2 + legOff, 14 + bounce, 4, 2);

        if (attacking) {
            this.gfx.fillStyle(light);
            this.gfx.fillRect(flip * 10, -8 + bounce, 8 * flip, 3);
            this.gfx.fillRect(flip * 14, -12 + bounce, 3, 8);
        } else {
            this.gfx.fillStyle(0xaaaaaa);
            if (this.role === 'tank') {
                this.gfx.fillRect(-flip * 9, -4 + bounce, 3, 12);
                this.gfx.fillStyle(light);
                this.gfx.fillRect(-flip * 9, -2 + bounce, 3, 4);
            } else if (this.role === 'healer') {
                this.gfx.fillRect(flip * 8, -10 + bounce, 2, 16);
                this.gfx.fillStyle(light);
                this.gfx.fillEllipse(flip * 9, -12 + bounce, 6, 6);
            } else if (this.range > 100) {
                this.gfx.fillRect(flip * 8, -6 + bounce, 2, 14);
                this.gfx.fillStyle(light);
                this.gfx.fillRect(flip * 7, -6 + bounce, 4, 2);
            } else {
                this.gfx.fillRect(flip * 8, -2 + bounce, 6 * flip, 2);
            }
        }

        let effectY = -22;
        this.statusEffects.forEach(e => {
            let c = 0xffffff;
            if (e.type === 'burn') c = 0xff4400;
            else if (e.type === 'poison') c = 0x44cc44;
            else if (e.type === 'shield_block') c = 0x4488ff;
            else if (e.type === 'frenzy') c = 0xff4444;
            else if (e.type === 'war_cry') c = 0xffaa44;
            this.gfx.fillStyle(c, 0.8);
            this.gfx.fillCircle(6, effectY + bounce, 2);
            effectY -= 5;
        });
    }

    assignSkills(skillKeys) {
        this.skills = skillKeys.map(key => ({ ...SKILL_DATA[key], key })).filter(Boolean);
        this.skills.forEach(skill => {
            this.skillTimers[skill.name] = skill.cooldown * 0.5;
        });
    }

    get sprite() { return this.container; }

    getEffectiveAttackSpeed() {
        let speed = this.attackSpeed;
        const frenzy = this.statusEffects.find(e => e.type === 'frenzy');
        if (frenzy) speed *= frenzy.atkSpeedMult;

        if (this.unitRef) {
            const hasFocused = this.unitRef.traits.find(t => t.id === 'focused');
            const hasLazy = this.unitRef.traits.find(t => t.id === 'lazy');
            if (hasFocused) speed *= 0.85;
            if (hasLazy) speed *= 1.20;
        }
        return speed;
    }

    getEffectiveAtk() {
        let atk = this.atk;
        const warCry = this.statusEffects.find(e => e.type === 'war_cry');
        if (warCry) atk = Math.floor(atk * (1 + warCry.atkBonusPct));

        if (this.unitRef) {
            const brave = this.unitRef.traits.find(t => t.id === 'brave');
            if (brave && this.hp / this.maxHp <= 0.3) atk = Math.floor(atk * 1.25);
            const cowardly = this.unitRef.traits.find(t => t.id === 'cowardly');
            if (cowardly && this.hp / this.maxHp <= 0.5) atk = Math.floor(atk * 0.85);
            const inspiring = this.unitRef.traits.find(t => t.id === 'inspiring');
            if (inspiring) atk += 5;
        }
        return atk;
    }

    update(delta, allCharacters) {
        if (!this.alive) return;

        this.animTimer += delta;
        if (this.animTimer > 200) {
            this.animTimer = 0;
            this.animFrame++;
        }

        if (this.attackAnimTimer > 0) {
            this.attackAnimTimer -= delta;
            this.animState = 'attack';
        }

        this.updateStatusEffects(delta);
        this.updateSkills(delta, allCharacters);

        const enemies = allCharacters.filter(c => c.team !== this.team && c.alive);
        if (enemies.length === 0) { this.drawCharacter(); return; }

        this.target = this.findTarget(enemies);
        if (!this.target) { this.drawCharacter(); return; }

        const dist = Math.abs(this.container.x - this.target.container.x);
        const dir = this.target.container.x > this.container.x ? 1 : -1;
        const UNIT_HALF_WIDTH = 14;
        const MIN_SAME_TEAM_GAP = 30;

        if (dist > this.range) {
            let newX = this.container.x + dir * this.moveSpeed * (delta / 1000);

            const opposingUnits = allCharacters.filter(c => c.team !== this.team && c.alive);
            for (const opp of opposingUnits) {
                const minDist = UNIT_HALF_WIDTH * 2;
                if (dir === 1 && opp.container.x > this.container.x)
                    newX = Math.min(newX, opp.container.x - minDist);
                else if (dir === -1 && opp.container.x < this.container.x)
                    newX = Math.max(newX, opp.container.x + minDist);
            }

            const teammates = allCharacters.filter(c => c.team === this.team && c.alive && c !== this);
            for (const mate of teammates) {
                if (dir === 1 && mate.container.x > this.container.x)
                    newX = Math.min(newX, mate.container.x - MIN_SAME_TEAM_GAP);
                else if (dir === -1 && mate.container.x < this.container.x)
                    newX = Math.max(newX, mate.container.x + MIN_SAME_TEAM_GAP);
            }

            if (this.range > 100) {
                const meleeAllies = allCharacters.filter(
                    c => c.team === this.team && c.alive && c.range <= 100 && c !== this
                );
                if (meleeAllies.length > 0) {
                    if (dir === 1) {
                        const front = Math.max(...meleeAllies.map(c => c.container.x));
                        newX = Math.min(newX, front - MIN_SAME_TEAM_GAP);
                    } else {
                        const front = Math.min(...meleeAllies.map(c => c.container.x));
                        newX = Math.max(newX, front + MIN_SAME_TEAM_GAP);
                    }
                }
            }

            this.container.x = newX;
            if (this.attackAnimTimer <= 0) this.animState = 'walk';
        } else {
            this.attackTimer += delta;
            const effectiveSpeed = this.getEffectiveAttackSpeed();
            if (this.attackTimer >= effectiveSpeed) {
                this.attackTimer = 0;
                this.performAttack();
            } else {
                if (this.attackAnimTimer <= 0) this.animState = 'idle';
            }
        }

        this.drawCharacter();
        this.healthBar.setPosition(this.container.x, this.container.y - 30);
        this.healthBar.update(this.hp, 0);
        this.nameText.setPosition(this.container.x, this.container.y - 40);
    }

    findTarget(enemies) {
        const taunt = this.statusEffects.find(e => e.type === 'taunt');
        if (taunt && taunt.source && taunt.source.alive) return taunt.source;
        return enemies.reduce((closest, enemy) => {
            const dist = Math.abs(this.container.x - enemy.container.x);
            const closestDist = Math.abs(this.container.x - closest.container.x);
            return dist < closestDist ? enemy : closest;
        });
    }

    performAttack() {
        if (!this.target || !this.target.alive) return;

        this.attackAnimTimer = 300;
        this.animState = 'attack';

        let atk = this.getEffectiveAtk();
        let dmg = Math.max(1, atk - this.target.def);
        let isCrit = false;

        const preciseShot = this.statusEffects.find(e => e.type === 'precise_shot');
        if (preciseShot) {
            isCrit = true;
            dmg = Math.floor(dmg * this.critDmg * preciseShot.critBonus);
            this.statusEffects = this.statusEffects.filter(e => e !== preciseShot);
        } else if (Math.random() < this.critRate) {
            let critDmg = this.critDmg;
            if (this.unitRef) {
                const enduring = this.unitRef.traits.find(t => t.id === 'enduring');
                if (enduring) critDmg *= 0.8;
            }
            dmg = Math.floor(dmg * critDmg);
            isCrit = true;
        }

        const shieldBlock = this.target.statusEffects.find(e => e.type === 'shield_block');
        if (shieldBlock) dmg = Math.floor(dmg * (1 - shieldBlock.damageReduction));

        const frenzyTarget = this.target.statusEffects.find(e => e.type === 'frenzy');
        if (frenzyTarget) dmg = Math.floor(dmg * frenzyTarget.damageTakenMult);

        this.scene.dealDamage(this.target, dmg, isCrit, this);
        this.scene.showAttackEffect(this.container.x, this.container.y, this.target.container.x, this.target.container.y);

        if (this.range > 100) {
            this.scene.showProjectile(this.container.x, this.container.y, this.target.container.x, this.target.container.y, this.color);
        }
    }

    updateSkills(delta, allCharacters) {
        this.skills.forEach(skill => {
            let cd = skill.cooldown;
            if (this.unitRef) {
                const focused = this.unitRef.traits.find(t => t.id === 'focused');
                const lazy = this.unitRef.traits.find(t => t.id === 'lazy');
                if (focused) cd *= 0.85;
                if (lazy) cd *= 1.20;
            }
            this.skillTimers[skill.name] = (this.skillTimers[skill.name] || 0) + delta;
            if (this.skillTimers[skill.name] >= cd) {
                this.skillTimers[skill.name] = 0;
                skill.execute(this, allCharacters, this.scene);
            }
        });
    }

    updateStatusEffects(delta) {
        this.statusEffects = this.statusEffects.filter(effect => {
            if (!effect.elapsed) effect.elapsed = 0;
            effect.elapsed += delta;

            if (effect.type === 'burn' || effect.type === 'poison') {
                effect.tickTimer = (effect.tickTimer || 0) + delta;
                if (effect.tickTimer >= effect.tickRate) {
                    effect.tickTimer = 0;
                    const dmg = effect.damagePerTick;
                    this.hp -= dmg;
                    const color = effect.type === 'burn' ? 0xff6600 : 0x44cc44;
                    DamagePopup.show(this.scene, this.container.x, this.container.y - 20, dmg, color, false);
                    if (this.hp <= 0) { this.hp = 0; this.die(); }
                }
            }

            if (effect.elapsed >= effect.duration) return false;
            return true;
        });
    }

    takeDamage(amount) {
        this.hp -= amount;
        if (this.hp <= 0) { this.hp = 0; this.die(); }
    }

    die() {
        this.alive = false;
        this.scene.tweens.add({
            targets: this.container,
            alpha: 0, y: this.container.y + 10,
            duration: 500, ease: 'Power2'
        });
        this.healthBar.destroy();
        this.nameText.destroy();
    }

    destroy() {
        if (this.container) this.container.destroy();
        if (this.healthBar) this.healthBar.destroy();
        if (this.nameText) this.nameText.destroy();
    }
}
