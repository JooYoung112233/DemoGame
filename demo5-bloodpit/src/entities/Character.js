class BPCharacter {
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

        this.bleedChance = data.bleedChance || 0;
        this.dodgeRate = data.dodgeRate || 0;
        this.bleeding = false;
        this.bleedTimer = 0;
        this.bleedTickTimer = 0;
        this.bleedDamage = 0;

        this.bleedExplodeChance = 0;
        this.bleedBonusDmg = 0;
        this.dodgeCounterChance = 0;
        this.dodgeSpdBuff = false;
        this.corpseExplodeChance = 0;
        this.corpseHeal = 0;

        this.thorns = data.thorns || 0;
        this.lifesteal = data.lifesteal || 0;
        this.defReduction = 0;
        this.isNest = data.isNest || false;

        this.skill = null;
        this.skillTimer = 0;
        this.skillCooldown = 0;

        this.enemyType = data.type || null;
        this.abilityTimer = 0;
        this.bossPhase = 0;
        this.nestHealTimer = 0;

        this.healTimer = 0;
        this.healCooldown = data.role === 'healer' ? 7000 : 0;
        this.healAmount = data.role === 'healer' ? 60 : 0;
        this.aoeCooldown = data.role === 'dps' && data.range > 100 ? 10000 : 0;
        this.aoeTimer = 0;
        this.aoeDamage = data.atk * 1.5;

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

        if (this.isNest) {
            this.gfx.fillStyle(0x000000, 0.3); this.gfx.fillEllipse(0, 14, 28, 8);
            this.gfx.fillStyle(this.color); this.gfx.fillRect(-10, -5+bounce, 20, 18);
            this.gfx.fillStyle(dark); this.gfx.fillRect(-10, -5+bounce, 20, 3);
            this.gfx.fillStyle(0xff4444, 0.5); this.gfx.fillCircle(0, 4+bounce, 4);
            return;
        }

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

        if (this.bleeding) {
            this.gfx.fillStyle(0xff0000, 0.4);
            this.gfx.fillCircle(Phaser.Math.Between(-4, 4), Phaser.Math.Between(-2, 8), 2);
        }
    }

    update(delta, allCharacters) {
        if (!this.alive) return;
        this.animTimer += delta;
        if (this.animTimer > 250) { this.animTimer = 0; this.animFrame++; this.drawCharacter(); }

        if (this.bleeding) {
            this.bleedTimer -= delta;
            this.bleedTickTimer -= delta;
            if (this.bleedTickTimer <= 0) {
                this.bleedTickTimer = 1000;
                this.hp -= this.bleedDamage;
                DamagePopup.showBleed(this.scene, this.container.x, this.container.y - 15, this.bleedDamage);
                if (this.hp <= 0) { this.hp = 0; this.die(allCharacters); return; }
            }
            if (this.bleedTimer <= 0) this.bleeding = false;
        }

        if (this.isNest) {
            this.nestHealTimer += delta;
            if (this.nestHealTimer >= 3000) {
                this.nestHealTimer = 0;
                const nearby = allCharacters.filter(c => c.team === this.team && c.alive && c !== this && Math.abs(this.container.x - c.container.x) < 200);
                nearby.forEach(a => {
                    const heal = Math.floor(a.maxHp * 0.03);
                    a.hp = Math.min(a.maxHp, a.hp + heal);
                    DamagePopup.show(this.scene, a.container.x, a.container.y - 30, heal, 0x66aa44, true);
                });
                if (nearby.length > 0) {
                    const pulse = this.scene.add.circle(this.container.x, this.container.y, 6, 0x66aa44, 0.4).setDepth(50);
                    this.scene.tweens.add({ targets: pulse, scaleX: 4, scaleY: 4, alpha: 0, duration: 400, onComplete: () => pulse.destroy() });
                }
            }
            this.healthBar.update(this.hp, this.maxHp);
            this.healthBar.setPosition(this.container.x, this.container.y - 30);
            return;
        }

        const enemies = allCharacters.filter(c => c.team !== this.team && c.alive);
        if (!enemies.length) return;

        this.target = enemies.reduce((a, b) => Math.abs(this.container.x-a.container.x) < Math.abs(this.container.x-b.container.x) ? a : b);
        const dist = Math.abs(this.container.x - this.target.container.x);

        if (dist > this.range) {
            const dir = this.target.container.x > this.container.x ? 1 : -1;
            this.container.x += dir * this.moveSpeed * (delta / 1000);
        } else {
            this.attackTimer += delta;
            if (this.attackTimer >= this.attackSpeed) {
                this.attackTimer = 0;
                this.performAttack(allCharacters);
            }
        }

        if (this.healCooldown > 0) {
            this.healTimer += delta;
            if (this.healTimer >= this.healCooldown) {
                this.healTimer = 0;
                this.performHeal(allCharacters);
            }
        }

        if (this.aoeCooldown > 0) {
            this.aoeTimer += delta;
            if (this.aoeTimer >= this.aoeCooldown) {
                this.aoeTimer = 0;
                const enemies2 = allCharacters.filter(c => c.team !== this.team && c.alive);
                this.performAoe(enemies2);
            }
        }

        if (this.skill && this.skillCooldown > 0) {
            this.skillTimer += delta;
            if (this.skillTimer >= this.skillCooldown) {
                this.skillTimer = 0;
                if (this.role === 'healer') {
                    const allies = allCharacters.filter(c => c.team === this.team && c.alive);
                    this.skill.execute(this, allies, this.scene);
                } else {
                    const foes = allCharacters.filter(c => c.team !== this.team && c.alive);
                    this.skill.execute(this, foes, this.scene);
                }
            }
        }

        if (this.enemyType === 'bruiser' && this.team === 'enemy') {
            this.abilityTimer += delta;
            if (this.abilityTimer >= 6000) {
                this.abilityTimer = 0;
                const targets = allCharacters.filter(c => c.team !== this.team && c.alive && Math.abs(this.container.x - c.container.x) <= 80);
                if (targets.length > 0) {
                    const slamDmg = Math.floor(this.atk * 0.5);
                    targets.forEach(t => {
                        t.takeDamage(slamDmg, this, allCharacters);
                        DamagePopup.show(this.scene, t.container.x, t.container.y - 20, slamDmg, 0xcc8822, false);
                    });
                    DamagePopup.show(this.scene, this.container.x, this.container.y - 35, '강타!', 0xcc8822, false);
                    const slam = this.scene.add.circle(this.container.x, this.container.y + 10, 8, 0xcc8822, 0.5).setDepth(50);
                    this.scene.tweens.add({ targets: slam, scaleX: 4, scaleY: 2, alpha: 0, duration: 300, onComplete: () => slam.destroy() });
                }
            }
        }

        if (this.enemyType === 'boss' && this.team === 'enemy') {
            const hpRatio = this.hp / this.maxHp;
            if (this.bossPhase === 0 && hpRatio <= 0.5) {
                this.bossPhase = 1;
                this.atk = Math.floor(this.atk * 1.3);
                this.attackSpeed = Math.floor(this.attackSpeed * 0.8);
                DamagePopup.show(this.scene, this.container.x, this.container.y - 40, '분노!', 0xff2244, false);
                const rage = this.scene.add.circle(this.container.x, this.container.y, 10, 0xff2244, 0.6).setDepth(50);
                this.scene.tweens.add({ targets: rage, scaleX: 5, scaleY: 5, alpha: 0, duration: 500, onComplete: () => rage.destroy() });
            }
            if (this.bossPhase === 1 && hpRatio <= 0.25) {
                this.bossPhase = 2;
                this.lifesteal = 0.15;
                this.critRate = 1.0;
                DamagePopup.show(this.scene, this.container.x, this.container.y - 40, '필사!', 0xff0000, false);
            }
        }

        this.healthBar.update(this.hp, this.maxHp);
        this.healthBar.setPosition(this.container.x, this.container.y - 30);
    }

    performAttack(allCharacters) {
        if (!this.target || !this.target.alive) return;

        if (this.target.dodgeRate > 0 && Math.random() < this.target.dodgeRate) {
            DamagePopup.showDodge(this.scene, this.target.container.x, this.target.container.y - 20);
            if (this.target.dodgeCounterChance > 0 && Math.random() < this.target.dodgeCounterChance) {
                const counterDmg = Math.max(1, this.target.atk - this.def);
                this.takeDamage(counterDmg, this.target, allCharacters);
                DamagePopup.show(this.scene, this.container.x, this.container.y - 20, counterDmg, 0x44ffff, false);
            }
            if (this.target.dodgeSpdBuff) {
                const origSpd = this.target.attackSpeed;
                this.target.attackSpeed = Math.floor(this.target.attackSpeed * 0.8);
                this.scene.time.delayedCall(3000, () => { if (this.target) this.target.attackSpeed = origSpd; });
            }
            return;
        }

        let effectiveDef = Math.max(0, this.target.def - Math.floor(this.target.def * (this.target.defReduction || 0)));
        let dmg = Math.max(1, this.atk - effectiveDef);

        if (this.target.bleeding && this.bleedBonusDmg > 0) {
            dmg = Math.floor(dmg * (1 + this.bleedBonusDmg));
        }

        let isCrit = Math.random() < this.critRate;
        if (isCrit) dmg = Math.floor(dmg * this.critDmg);

        this.target.takeDamage(dmg, this, allCharacters);
        this.totalDamageDealt += dmg;

        if (this.lifesteal > 0) {
            const heal = Math.floor(dmg * this.lifesteal);
            this.hp = Math.min(this.maxHp, this.hp + heal);
        }

        if (this.bleedChance > 0 && Math.random() < this.bleedChance && !this.target.bleeding) {
            this.target.bleeding = true;
            this.target.bleedTimer = 5000;
            this.target.bleedTickTimer = 1000;
            this.target.bleedDamage = Math.max(1, Math.floor(this.atk * 0.2));
        }

        // hit flash
        this.target.gfx.setAlpha(0.3);
        this.scene.time.delayedCall(50, () => { if (this.target && this.target.gfx) this.target.gfx.setAlpha(1); });

        // spark
        const tx = this.target.container.x, ty = this.target.container.y - 5;
        const spark = this.scene.add.circle(tx, ty, isCrit ? 7 : 5, 0xffffff, 0.9).setDepth(50);
        this.scene.tweens.add({ targets: spark, alpha: 0, scaleX: 2.5, scaleY: 2.5, duration: 150, onComplete: () => spark.destroy() });

        // hit particles
        const pCount = isCrit ? 5 : 3;
        const hitColor = isCrit ? 0xffff44 : this.color;
        for (let pi = 0; pi < pCount; pi++) {
            const p = this.scene.add.circle(tx, ty, 2, hitColor, 0.8).setDepth(50);
            this.scene.tweens.add({
                targets: p,
                x: tx + Phaser.Math.Between(-20, 20), y: ty + Phaser.Math.Between(-20, 10),
                alpha: 0, scale: 0.3, duration: Phaser.Math.Between(150, 300),
                onComplete: () => p.destroy()
            });
        }

        // crit slash
        if (isCrit) {
            const slash = this.scene.add.line(0, 0, tx - 12, ty + 8, tx + 12, ty - 8, 0xffff44, 0.8).setDepth(55);
            this.scene.tweens.add({ targets: slash, alpha: 0, duration: 200, onComplete: () => slash.destroy() });
            this.scene.cameras.main.shake(60, 0.002);
        }

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
        const lowest = allies.reduce((a, b) => (a.hp/a.maxHp) < (b.hp/b.maxHp) ? a : b);
        const healed = Math.min(this.healAmount, lowest.maxHp - lowest.hp);
        lowest.hp += healed;
        DamagePopup.show(this.scene, lowest.container.x, lowest.container.y - 30, healed, 0x44ff88, true);
    }

    performAoe(enemies) {
        enemies.forEach(e => {
            const dmg = Math.max(1, Math.floor(this.aoeDamage) - e.def);
            e.takeDamage(dmg, this, null);
            this.totalDamageDealt += dmg;
        });
        const cx = enemies.reduce((s,e) => s + e.container.x, 0) / enemies.length;
        const circle = this.scene.add.circle(cx, 430, 100, 0xaa44ff, 0.3).setDepth(50);
        this.scene.tweens.add({ targets: circle, alpha: 0, scaleX: 1.5, scaleY: 1.5, duration: 500, onComplete: () => circle.destroy() });
    }

    takeDamage(amount, attacker, allCharacters) {
        this.hp -= amount;
        if (this.thorns > 0 && attacker && attacker.alive) {
            attacker.hp -= this.thorns;
            if (attacker.hp <= 0) { attacker.hp = 0; attacker.die(allCharacters); }
        }
        const dir = attacker ? (this.container.x > attacker.container.x ? 1 : -1) : 1;
        const kb = amount > this.maxHp * 0.15 ? 10 : 6;
        this.scene.tweens.add({ targets: this.container, x: this.container.x + dir * kb, duration: 50, yoyo: true });
        if (this.hp <= 0) { this.hp = 0; this.die(allCharacters); }
    }

    die(allCharacters) {
        if (!this.alive) return;
        this.alive = false;

        if (allCharacters) {
            const opponents = allCharacters.filter(c => c.team !== this.team && c.alive);

            if (this.bleeding && opponents.length > 0) {
                const bleedExplode = opponents[0].bleedExplodeChance || 0;
                if (bleedExplode > 0 && Math.random() < bleedExplode) {
                    allCharacters.filter(c => c.team === this.team && c.alive).forEach(e => {
                        const dist = Math.abs(this.container.x - e.container.x);
                        if (dist < 150) {
                            const explodeDmg = Math.floor(this.maxHp * 0.3);
                            e.takeDamage(explodeDmg, null, allCharacters);
                            DamagePopup.show(this.scene, e.container.x, e.container.y - 20, explodeDmg, 0xff4444, false);
                        }
                    });
                    const boom = this.scene.add.circle(this.container.x, this.container.y, 60, 0xff2222, 0.5).setDepth(50);
                    this.scene.tweens.add({ targets: boom, alpha: 0, scaleX: 2, scaleY: 2, duration: 400, onComplete: () => boom.destroy() });
                }
            }

            if (opponents.length > 0) {
                const corpseExplode = opponents[0].corpseExplodeChance || 0;
                if (corpseExplode > 0 && Math.random() < corpseExplode) {
                    allCharacters.filter(c => c.team === this.team && c.alive).forEach(e => {
                        const dist = Math.abs(this.container.x - e.container.x);
                        if (dist < 120) {
                            const cDmg = 80;
                            e.takeDamage(cDmg, null, allCharacters);
                            DamagePopup.show(this.scene, e.container.x, e.container.y - 20, cDmg, 0xff8844, false);
                        }
                    });
                    const boom = this.scene.add.circle(this.container.x, this.container.y, 50, 0xff8844, 0.4).setDepth(50);
                    this.scene.tweens.add({ targets: boom, alpha: 0, scaleX: 1.8, scaleY: 1.8, duration: 350, onComplete: () => boom.destroy() });
                }

                const corpseHeal = opponents[0].corpseHeal || 0;
                if (corpseHeal > 0) {
                    opponents.forEach(a => {
                        const heal = Math.floor(a.maxHp * corpseHeal);
                        a.hp = Math.min(a.maxHp, a.hp + heal);
                        DamagePopup.show(this.scene, a.container.x, a.container.y - 30, heal, 0x44ff88, true);
                    });
                }
            }
        }

        // death particles
        const dx = this.container.x, dy = this.container.y;
        for (let di = 0; di < 8; di++) {
            const shard = this.scene.add.rectangle(dx, dy, 3, 3, this.color).setDepth(50);
            this.scene.tweens.add({
                targets: shard,
                x: dx + Phaser.Math.Between(-35, 35), y: dy + Phaser.Math.Between(-40, 15),
                alpha: 0, angle: Phaser.Math.Between(-180, 180),
                duration: Phaser.Math.Between(300, 500), onComplete: () => shard.destroy()
            });
        }

        // blood stain for enemies
        if (this.team === 'enemy') {
            const stain = this.scene.add.ellipse(dx, dy + 14, 14, 5, 0x660000, 0.25).setDepth(1);
            this.scene.tweens.add({ targets: stain, alpha: 0, duration: 6000, onComplete: () => stain.destroy() });
        }

        // ally death: camera shake
        if (this.team === 'ally') {
            this.scene.cameras.main.shake(150, 0.004);
        }

        this.scene.tweens.add({ targets: this.container, alpha: 0, y: this.container.y - 10, duration: 500, ease: 'Power2' });
        this.healthBar.destroy();
    }

    destroy() {
        if (this.container) this.container.destroy();
        if (this.healthBar) this.healthBar.destroy();
    }
}
