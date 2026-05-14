class Character {
    constructor(scene, data, team, x, y) {
        this.scene = scene;
        this.name = data.name;
        this.team = team;
        this.alive = true;

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
        this.spriteKey = data.spriteKey;

        this.attackTimer = 0;
        this.target = null;
        this.statusEffects = [];
        this.skills = [];
        this.skillTimers = {};
        this.totalDamageDealt = 0;
        this.totalHealDone = 0;
        this.animState = 'idle';
        this.animFrame = 0;
        this.animTimer = 0;
        this.attackAnimTimer = 0;

        this.container = scene.add.container(x, y);
        this.container.setDepth(5);
        this.drawCharacter();

        this.healthBar = new HealthBar(scene, x, y - 30, 40, 5, this.maxHp);

        this.nameText = scene.add.text(x, y - 40, data.name, {
            fontSize: '10px',
            fontFamily: 'monospace',
            color: '#ffffff',
            stroke: '#000000',
            strokeThickness: 2
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
        const dark = Phaser.Display.Color.GetColor(Math.floor(r*0.6), Math.floor(g*0.6), Math.floor(b*0.6));
        const light = Phaser.Display.Color.GetColor(Math.min(255,r+60), Math.min(255,g+60), Math.min(255,b+60));

        const bounce = (this.animFrame % 2 === 0) ? 0 : -2;
        const flip = this.team === 'enemy' ? -1 : 1;
        const attacking = this.animState === 'attack';

        // shadow
        this.gfx.fillStyle(0x000000, 0.3);
        this.gfx.fillEllipse(0, 14, 20, 6);

        // body
        this.gfx.fillStyle(this.color);
        this.gfx.fillRect(-7, -4 + bounce, 14, 14);

        // body accent
        this.gfx.fillStyle(dark);
        this.gfx.fillRect(-7, -4 + bounce, 14, 3);

        // head
        this.gfx.fillStyle(0xffcc99);
        this.gfx.fillRect(-4, -14 + bounce, 8, 8);

        // eyes
        this.gfx.fillStyle(0xffffff);
        this.gfx.fillRect(flip * 1, -12 + bounce, 3, 3);
        this.gfx.fillStyle(0x333333);
        this.gfx.fillRect(flip * 2, -12 + bounce, 2, 2);

        // hair/helmet
        this.gfx.fillStyle(dark);
        this.gfx.fillRect(-5, -16 + bounce, 10, 3);

        // legs
        const legOff = this.animState === 'walk' ? (this.animFrame % 2 === 0 ? -1 : 1) : 0;
        this.gfx.fillStyle(dark);
        this.gfx.fillRect(-4, 10 + bounce, 3, 5);
        this.gfx.fillRect(2 + legOff, 10 + bounce, 3, 5);

        // feet
        this.gfx.fillStyle(0x664422);
        this.gfx.fillRect(-4, 14 + bounce, 4, 2);
        this.gfx.fillRect(2 + legOff, 14 + bounce, 4, 2);

        // weapon
        if (attacking) {
            this.gfx.fillStyle(light);
            this.gfx.fillRect(flip * 10, -8 + bounce, 8 * flip, 3);
            this.gfx.fillRect(flip * 14, -12 + bounce, 3, 8);
        } else {
            this.gfx.fillStyle(0xaaaaaa);
            if (this.role === 'tank' || this.spriteKey === 'enemy_tank') {
                // shield
                this.gfx.fillRect(-flip * 9, -4 + bounce, 3, 12);
                this.gfx.fillStyle(light);
                this.gfx.fillRect(-flip * 9, -2 + bounce, 3, 4);
            } else if (this.role === 'healer' || this.spriteKey === 'mage') {
                // staff
                this.gfx.fillRect(flip * 8, -10 + bounce, 2, 16);
                this.gfx.fillStyle(light);
                this.gfx.fillEllipse(flip * 9, -12 + bounce, 6, 6);
            } else {
                // dagger/sword
                this.gfx.fillRect(flip * 8, -2 + bounce, 6 * flip, 2);
            }
        }

        // status effect indicators
        let effectY = -22;
        this.statusEffects.forEach(e => {
            if (e.type === 'bleed') {
                this.gfx.fillStyle(0xff4444, 0.8);
                this.gfx.fillCircle(6, effectY + bounce, 2);
            } else if (e.type === 'taunt') {
                this.gfx.fillStyle(0x4488ff, 0.8);
                this.gfx.fillCircle(6, effectY + bounce, 2);
            } else if (e.type === 'shield') {
                this.gfx.fillStyle(0x88ccff, 0.8);
                this.gfx.fillCircle(6, effectY + bounce, 2);
            }
            effectY -= 5;
        });
    }

    assignSkills(skillKeys) {
        this.skills = skillKeys.map(key => SKILL_DATA[key]).filter(Boolean);
        this.skills.forEach(skill => {
            this.skillTimers[skill.name] = skill.cooldown * 0.5;
        });
    }

    get sprite() {
        return this.container;
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
        if (enemies.length === 0) {
            this.drawCharacter();
            return;
        }

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
                if (dir === 1 && opp.container.x > this.container.x) {
                    newX = Math.min(newX, opp.container.x - minDist);
                } else if (dir === -1 && opp.container.x < this.container.x) {
                    newX = Math.max(newX, opp.container.x + minDist);
                }
            }

            const teammates = allCharacters.filter(c => c.team === this.team && c.alive && c !== this);
            for (const mate of teammates) {
                if (dir === 1 && mate.container.x > this.container.x) {
                    newX = Math.min(newX, mate.container.x - MIN_SAME_TEAM_GAP);
                } else if (dir === -1 && mate.container.x < this.container.x) {
                    newX = Math.max(newX, mate.container.x + MIN_SAME_TEAM_GAP);
                }
            }

            if (this.range > 100) {
                const meleeAllies = allCharacters.filter(
                    c => c.team === this.team && c.alive && c.range <= 100 && c !== this
                );
                if (meleeAllies.length > 0) {
                    if (dir === 1) {
                        const frontMelee = Math.max(...meleeAllies.map(c => c.container.x));
                        newX = Math.min(newX, frontMelee - MIN_SAME_TEAM_GAP);
                    } else {
                        const frontMelee = Math.min(...meleeAllies.map(c => c.container.x));
                        newX = Math.max(newX, frontMelee + MIN_SAME_TEAM_GAP);
                    }
                }
            }

            this.container.x = newX;
            if (this.attackAnimTimer <= 0) this.animState = 'walk';
        } else {
            this.attackTimer += delta;
            if (this.attackTimer >= this.attackSpeed) {
                this.attackTimer = 0;
                this.performAttack();
            } else {
                if (this.attackAnimTimer <= 0) this.animState = 'idle';
            }
        }

        this.drawCharacter();

        this.healthBar.setPosition(this.container.x, this.container.y - 30);
        const shieldEffect = this.statusEffects.find(e => e.type === 'shield');
        this.healthBar.update(this.hp, shieldEffect ? shieldEffect.amount : 0);
        this.nameText.setPosition(this.container.x, this.container.y - 40);
    }

    findTarget(enemies) {
        const taunt = this.statusEffects.find(e => e.type === 'taunt');
        if (taunt && taunt.source && taunt.source.alive) {
            return taunt.source;
        }
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

        let dmg = Math.max(1, this.atk - this.target.def);
        let isCrit = false;

        if (Math.random() < this.critRate) {
            dmg = Math.floor(dmg * this.critDmg);
            isCrit = true;
        }

        this.scene.dealDamage(this.target, dmg, isCrit, this);
        this.scene.showAttackEffect(this.container.x, this.container.y, this.target.container.x, this.target.container.y);

        if (this.range > 100) {
            this.scene.showProjectile(this.container.x, this.container.y, this.target.container.x, this.target.container.y, this.color);
        }
    }

    updateSkills(delta, allCharacters) {
        this.skills.forEach(skill => {
            this.skillTimers[skill.name] = (this.skillTimers[skill.name] || 0) + delta;
            if (this.skillTimers[skill.name] >= skill.cooldown) {
                this.skillTimers[skill.name] = 0;
                skill.execute(this, allCharacters, this.scene);
            }
        });
    }

    updateStatusEffects(delta) {
        this.statusEffects = this.statusEffects.filter(effect => {
            effect.elapsed += delta;

            if (effect.type === 'bleed') {
                effect.tickElapsed = (effect.tickElapsed || 0) + delta;
                if (effect.tickElapsed >= effect.tickRate) {
                    effect.tickElapsed = 0;
                    const dmg = effect.damagePerTick * (effect.stacks || 1);
                    this.hp -= dmg;
                    DamagePopup.show(this.scene, this.container.x, this.container.y - 20, dmg, 0xff6666, false);
                }
            }

            if (effect.elapsed >= effect.duration) return false;
            return true;
        });
    }

    takeDamage(amount) {
        const shield = this.statusEffects.find(e => e.type === 'shield');
        if (shield) {
            if (shield.amount >= amount) {
                shield.amount -= amount;
                return;
            } else {
                amount -= shield.amount;
                shield.amount = 0;
                this.statusEffects = this.statusEffects.filter(e => e !== shield);
            }
        }

        this.hp -= amount;
        if (this.hp <= 0) {
            this.hp = 0;
            this.die();
        }
    }

    die() {
        this.alive = false;
        this.scene.tweens.add({
            targets: this.container,
            alpha: 0,
            y: this.container.y + 10,
            duration: 500,
            ease: 'Power2'
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
