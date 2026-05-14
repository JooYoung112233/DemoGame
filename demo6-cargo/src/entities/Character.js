class LCCharacter {
    constructor(scene, data, team, x, y) {
        this.scene = scene;
        this.name = data.name;
        this.team = team;
        this.alive = true;
        this.maxHp = data.hp; this.hp = data.hp;
        this.atk = data.atk; this.def = data.def;
        this.attackSpeed = data.attackSpeed;
        this.range = data.range; this.moveSpeed = data.moveSpeed;
        this.critRate = data.critRate; this.critDmg = data.critDmg;
        this.color = data.color; this.role = data.role || 'enemy';
        this.attackTimer = 0;
        this.target = null;
        this.totalDamageDealt = 0;
        this.thorns = 0; this.lifesteal = 0;

        this.healTimer = 0;
        this.healCooldown = data.role === 'healer' ? 7000 : 0;
        this.healAmount = data.role === 'healer' ? 50 : 0;

        this.skill = null;
        this.skillTimer = 0;
        this.skillCooldown = 0;
        this.currentCar = null;
        this.stunTimer = 0;
        this.enemyType = data.type || null;
        this.abilityTimer = 0;

        this.animFrame = 0; this.animTimer = 0;
        this.container = scene.add.container(x, y).setDepth(5);
        this.gfx = scene.add.graphics();
        this.container.add(this.gfx);
        this.drawCharacter();
        this.healthBar = new HealthBar(scene, x, y - 30, 36, 4, this.maxHp);
    }

    drawCharacter() {
        this.gfx.clear();
        const flip = this.team === 'enemy' ? -1 : 1;
        const r = (this.color >> 16) & 0xff, g = (this.color >> 8) & 0xff, b = this.color & 0xff;
        const dark = Phaser.Display.Color.GetColor(Math.floor(r*.6), Math.floor(g*.6), Math.floor(b*.6));
        const bounce = this.animFrame % 2 === 0 ? 0 : -1;

        this.gfx.fillStyle(0x000000, 0.3); this.gfx.fillEllipse(0, 14, 18, 5);
        this.gfx.fillStyle(this.color); this.gfx.fillRect(-6, -3+bounce, 12, 12);
        this.gfx.fillStyle(dark); this.gfx.fillRect(-6, -3+bounce, 12, 2);
        this.gfx.fillStyle(0xffcc99); this.gfx.fillRect(-3, -12+bounce, 7, 7);
        this.gfx.fillStyle(0xffffff); this.gfx.fillRect(flip, -10+bounce, 2, 2);
        this.gfx.fillStyle(0x333333); this.gfx.fillRect(flip+1, -10+bounce, 1, 2);
        this.gfx.fillStyle(dark);
        this.gfx.fillRect(-4, -14+bounce, 9, 3);
        this.gfx.fillRect(-3, 9+bounce, 3, 4); this.gfx.fillRect(2, 9+bounce, 3, 4);
        this.gfx.fillStyle(0x664422); this.gfx.fillRect(-3, 12+bounce, 3, 2); this.gfx.fillRect(2, 12+bounce, 3, 2);
    }

    assignSkill(skillData) {
        this.skill = skillData;
        this.skillCooldown = skillData.cooldown;
        this.skillTimer = 0;
    }

    update(delta, allCharacters) {
        if (!this.alive) return;

        if (this.stunTimer > 0) {
            this.stunTimer -= delta;
            this.healthBar.update(this.hp, this.maxHp);
            this.healthBar.setPosition(this.container.x, this.container.y - 30);
            return;
        }

        this.animTimer += delta;
        if (this.animTimer > 250) { this.animTimer = 0; this.animFrame++; this.drawCharacter(); }

        const enemies = allCharacters.filter(c => c.team !== this.team && c.alive);
        if (!enemies.length) return;

        if (this.enemyType === 'jumper') {
            this.target = enemies.reduce((a, b) =>
                Math.abs(this.container.x - a.container.x) > Math.abs(this.container.x - b.container.x) ? a : b
            );
        } else {
            this.target = enemies.reduce((a, b) =>
                Math.abs(this.container.x - a.container.x) < Math.abs(this.container.x - b.container.x) ? a : b
            );
        }
        const dist = Math.abs(this.container.x - this.target.container.x);

        if (dist > this.range) {
            const dir = this.target.container.x > this.container.x ? 1 : -1;
            this.container.x += dir * this.moveSpeed * (delta / 1000);
        } else {
            this.attackTimer += delta;
            if (this.attackTimer >= this.attackSpeed) {
                this.attackTimer = 0;
                this.performAttack();
            }
        }

        if (this.healCooldown > 0) {
            this.healTimer += delta;
            if (this.healTimer >= this.healCooldown) {
                this.healTimer = 0;
                this.performHeal(allCharacters);
            }
        }

        if (this.skill && this.skillCooldown > 0) {
            this.skillTimer += delta;
            if (this.skillTimer >= this.skillCooldown) {
                this.skillTimer = 0;
                if (this.role === 'healer') {
                    const allies = allCharacters.filter(c => c.team === this.team && c.alive);
                    this.skill.execute(this, allies, this.scene);
                } else if (this.role === 'support') {
                    this.skill.execute(this, this.currentCar, this.scene);
                } else {
                    const foes = allCharacters.filter(c => c.team !== this.team && c.alive);
                    this.skill.execute(this, foes, this.scene);
                }
            }
        }

        if (this.enemyType === 'tanker') {
            this.abilityTimer += delta;
            if (this.abilityTimer >= 5000 && this.target && this.target.alive) {
                this.abilityTimer = 0;
                const dist2 = Math.abs(this.container.x - this.target.container.x);
                if (dist2 <= this.range + 10) {
                    this.target.stunTimer = 1000;
                    DamagePopup.show(this.scene, this.target.container.x, this.target.container.y - 30, '스턴!', 0xffcc00, false);
                }
            }
        }

        this.healthBar.update(this.hp, this.maxHp);
        this.healthBar.setPosition(this.container.x, this.container.y - 30);
    }

    performAttack() {
        if (!this.target || !this.target.alive) return;
        let dmg = Math.max(1, this.atk - this.target.def);
        let isCrit = Math.random() < this.critRate;
        if (isCrit) dmg = Math.floor(dmg * this.critDmg);

        this.target.takeDamage(dmg, this);
        this.totalDamageDealt += dmg;

        if (this.lifesteal > 0) {
            const heal = Math.floor(dmg * this.lifesteal);
            this.hp = Math.min(this.maxHp, this.hp + heal);
        }

        const spark = this.scene.add.circle(this.target.container.x, this.target.container.y - 5, 5, 0xffffff, 0.8).setDepth(50);
        this.scene.tweens.add({ targets: spark, alpha: 0, scaleX: 2, scaleY: 2, duration: 200, onComplete: () => spark.destroy() });

        if (isCrit) {
            DamagePopup.showCritical(this.scene, this.target.container.x, this.target.container.y - 20, dmg);
        } else {
            DamagePopup.show(this.scene, this.target.container.x, this.target.container.y - 20, dmg, 0xffffff, false);
        }

        if (this.range > 100) {
            const proj = this.scene.add.circle(this.container.x, this.container.y - 8, 3, this.color).setDepth(50);
            this.scene.tweens.add({ targets: proj, x: this.target.container.x, y: this.target.container.y - 8, duration: 250, onComplete: () => proj.destroy() });
        }
    }

    performHeal(all) {
        const allies = all.filter(c => c.team === this.team && c.alive && c !== this);
        if (!allies.length) return;
        const lowest = allies.reduce((a, b) => (a.hp / a.maxHp) < (b.hp / b.maxHp) ? a : b);
        const healed = Math.min(this.healAmount, lowest.maxHp - lowest.hp);
        lowest.hp += healed;
        DamagePopup.show(this.scene, lowest.container.x, lowest.container.y - 30, healed, 0x44ff88, true);
    }

    takeDamage(amount, attacker) {
        this.hp -= amount;
        if (this.thorns > 0 && attacker && attacker.alive) {
            attacker.hp -= this.thorns;
            if (attacker.hp <= 0) { attacker.hp = 0; attacker.die(); }
        }
        const dir = attacker ? (this.container.x > attacker.container.x ? 1 : -1) : 1;
        this.scene.tweens.add({ targets: this.container, x: this.container.x + dir * 4, duration: 50, yoyo: true });
        if (this.hp <= 0) { this.hp = 0; this.die(); }
    }

    die() {
        if (!this.alive) return;
        this.alive = false;

        if (this.enemyType === 'explosive') {
            const allChars = [...(this.scene.allies || []), ...(this.scene.enemies || [])];
            const targets = allChars.filter(c => c.team !== this.team && c.alive);
            const aoeRange = 100;
            targets.forEach(t => {
                const d = Math.abs(this.container.x - t.container.x);
                if (d <= aoeRange) {
                    const dmg = Math.floor(this.atk * 0.8);
                    t.takeDamage(dmg, this);
                    DamagePopup.show(this.scene, t.container.x, t.container.y - 20, dmg, 0xff4422, false);
                }
            });
            const boom = this.scene.add.circle(this.container.x, this.container.y, 6, 0xff4422, 0.9).setDepth(50);
            this.scene.tweens.add({ targets: boom, scaleX: 8, scaleY: 8, alpha: 0, duration: 400, onComplete: () => boom.destroy() });
        }

        this.scene.tweens.add({ targets: this.container, alpha: 0, y: this.container.y + 8, duration: 400 });
        this.healthBar.destroy();
    }

    destroy() {
        if (this.container) this.container.destroy();
        if (this.healthBar) this.healthBar.destroy();
    }
}
