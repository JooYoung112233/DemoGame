class BattleUnit {
    static _nextBattleId = 1;

    constructor(scene, config) {
        this.scene = scene;
        this.battleId = BattleUnit._nextBattleId++;
        this.team = config.team;
        this.name = config.name;
        this.classKey = config.classKey || 'warrior';
        this.role = config.role || 'dps';

        this.maxHp = config.hp;
        this.hp = config.hp;
        this.atk = config.atk;
        this.def = config.def;
        this.attackSpeed = config.attackSpeed;
        this.range = config.range;
        this.moveSpeed = config.moveSpeed;
        this.critRate = config.critRate || 0.05;
        this.critDmg = config.critDmg || 1.5;
        this.color = config.color;
        this.mercId = config.mercId || null;
        this.isElite = config.isElite || false;
        this.isBoss = config.isBoss || false;
        this.enemyType = config.enemyType || null;

        this.healBonus = config.healBonus || 0;
        this.buffBonus = config.buffBonus || 0;
        this.lifestealOnKill = config.lifestealOnKill || 0;
        this.lastStand = config.lastStand || false;
        this.chainChance = config.chainChance || 0;
        this.bleedChance = config.bleedChance || 0;
        this.lifesteal = config.lifesteal || 0;

        this.bleeding = false;
        this.bleedTimer = 0;
        this.bleedTickTimer = 0;
        this.bleedDamage = 0;

        this.alive = true;
        this.target = null;
        this.attackTimer = 0;
        this.skillTimer = 0;
        this.skillCooldown = config.skillCooldown || 0;
        this.level = config.level || 1;
        this.lastStandActive = false;
        this.totalDamageDealt = 0;

        this.healTimer = 0;
        this.healCooldown = this.role === 'healer' ? 7000 : 0;
        this.healAmount = this.role === 'healer' ? Math.floor(this.atk * 2) : 0;
        this.aoeCooldown = (this.role === 'ranged_dps' || (this.role === 'dps' && this.range > 100)) ? 10000 : 0;
        this.aoeTimer = 0;
        this.aoeDamage = this.atk * 1.5;
        this.abilityTimer = 0;
        this.bossPhase = 0;

        this.skillReady = false;
        this.shieldAmount = 0;
        this.shieldTimer = 0;
        this.atkBuff = 0;
        this.atkBuffTimer = 0;
        this.defBuff = 0;
        this.defBuffTimer = 0;

        this.animFrame = 0;
        this.animTimer = 0;

        this.container = scene.add.container(config.x, config.y).setDepth(5);
        this.gfx = scene.add.graphics();
        this.container.add(this.gfx);
        this.drawCharacter();
        this.healthBar = new BattleHealthBar(scene, config.x, config.y - 30, 36, 4, this.maxHp);
    }

    drawCharacter() {
        this.gfx.clear();
        const flip = this.team === 'enemy' ? -1 : 1;
        const r = (this.color >> 16) & 0xff, g_ = (this.color >> 8) & 0xff, b = this.color & 0xff;
        const dark = Phaser.Display.Color.GetColor(Math.floor(r * .6), Math.floor(g_ * .6), Math.floor(b * .6));
        const light = Phaser.Display.Color.GetColor(Math.min(255, Math.floor(r * 1.3)), Math.min(255, Math.floor(g_ * 1.3)), Math.min(255, Math.floor(b * 1.3)));
        const bounce = this.animFrame % 2 === 0 ? 0 : -1;
        const gfx = this.gfx;

        if (this.isBoss) {
            this._drawBoss(gfx, dark, light, bounce, flip);
            return;
        }

        if (this.isElite && this.team === 'enemy') {
            gfx.fillStyle(this.color, 0.15);
            gfx.fillCircle(0, 2 + bounce, 18);
        }

        if (this.team === 'enemy' && this.enemyType) {
            this._drawEnemy(gfx, dark, light, bounce, flip);
        } else {
            switch (this.role) {
                case 'tank': this._drawTank(gfx, dark, light, bounce, flip); break;
                case 'melee_dps': this._drawMeleeDPS(gfx, dark, light, bounce, flip); break;
                case 'ranged_dps': this._drawRangedDPS(gfx, dark, light, bounce, flip); break;
                case 'healer': this._drawHealer(gfx, dark, light, bounce, flip); break;
                case 'support': this._drawSupport(gfx, dark, light, bounce, flip); break;
                default: this._drawDefault(gfx, dark, bounce, flip); break;
            }
        }

        if (this.bleeding) {
            gfx.fillStyle(0xff0000, 0.4);
            gfx.fillCircle(Phaser.Math.Between(-4, 4), Phaser.Math.Between(-2, 8), 2);
        }
    }

    _drawTank(g, dark, light, b, flip) {
        g.fillStyle(0x000000, 0.3); g.fillEllipse(0, 15, 22, 6);
        g.fillStyle(this.color); g.fillRect(-8, -4 + b, 16, 14);
        g.fillStyle(dark); g.fillRect(-8, -4 + b, 16, 3);
        g.fillStyle(light); g.fillRect(-10, -3 + b, 4, 5); g.fillRect(6, -3 + b, 4, 5);
        g.fillStyle(0xffcc99); g.fillRect(-3, -13 + b, 7, 7);
        g.fillStyle(dark); g.fillRect(-5, -16 + b, 11, 4);
        g.fillStyle(0x999999); g.fillRect(-4, -13 + b, 9, 2);
        g.fillStyle(0xffffff); g.fillRect(flip, -11 + b, 2, 2);
        g.fillStyle(0x888899); g.fillRect(flip * 5, -2 + b, flip * 5, 10);
        g.fillStyle(light); g.fillRect(flip * 6, 0 + b, flip * 3, 6);
        g.fillStyle(dark); g.fillRect(-4, 10 + b, 4, 4); g.fillRect(2, 10 + b, 4, 4);
        g.fillStyle(0x664422); g.fillRect(-4, 13 + b, 4, 2); g.fillRect(2, 13 + b, 4, 2);
    }

    _drawMeleeDPS(g, dark, light, b, flip) {
        g.fillStyle(0x000000, 0.3); g.fillEllipse(0, 14, 16, 5);
        g.fillStyle(this.color); g.fillRect(-5, -2 + b, 10, 11);
        g.fillStyle(dark); g.fillRect(-5, -2 + b, 10, 2);
        g.fillStyle(0xffcc99); g.fillRect(-3, -11 + b, 6, 7);
        g.fillStyle(dark); g.fillRect(-4, -13 + b, 8, 4);
        g.fillStyle(this.color, 0.7); g.fillRect(-3, -11 + b, 6, 2);
        g.fillStyle(0xffffff); g.fillRect(flip, -9 + b, 3, 1);
        g.fillStyle(0xff4444, 0.6); g.fillRect(flip + 1, -9 + b, 1, 1);
        g.fillStyle(0xccccdd); g.fillRect(flip * 5, -4 + b, flip * 2, 12);
        g.fillStyle(0xeeeeff); g.fillRect(flip * 5, -4 + b, flip * 1, 10);
        g.fillStyle(dark); g.fillRect(-3, 9 + b, 3, 4); g.fillRect(1, 9 + b, 3, 4);
        g.fillStyle(0x443322); g.fillRect(-3, 12 + b, 3, 2); g.fillRect(1, 12 + b, 3, 2);
    }

    _drawRangedDPS(g, dark, light, b, flip) {
        g.fillStyle(0x000000, 0.3); g.fillEllipse(0, 14, 18, 5);
        g.fillStyle(this.color); g.fillRect(-5, -3 + b, 10, 8);
        g.fillStyle(this.color); g.fillRect(-7, 3 + b, 14, 6);
        g.fillStyle(dark); g.fillRect(-5, -3 + b, 10, 2);
        g.fillStyle(light); g.fillRect(-7, 8 + b, 14, 1);
        g.fillStyle(0xffcc99); g.fillRect(-3, -11 + b, 6, 6);
        g.fillStyle(dark);
        g.beginPath(); g.moveTo(0, -20 + b); g.lineTo(-6, -10 + b); g.lineTo(6, -10 + b); g.closePath(); g.fillPath();
        g.fillStyle(this.color); g.fillRect(-6, -11 + b, 12, 2);
        g.fillStyle(light); g.fillCircle(0, -19 + b, 1.5);
        g.fillStyle(0xffffff); g.fillRect(flip, -9 + b, 2, 2);
        g.fillStyle(light, 0.8); g.fillRect(flip + 1, -9 + b, 1, 1);
        g.fillStyle(0x886644); g.fillRect(flip * 6, -8 + b, flip * 2, 18);
        g.fillStyle(light); g.fillCircle(flip * 7, -9 + b, 3);
        g.fillStyle(0x664422); g.fillRect(-3, 9 + b, 3, 2); g.fillRect(2, 9 + b, 3, 2);
    }

    _drawHealer(g, dark, light, b, flip) {
        g.fillStyle(0x000000, 0.3); g.fillEllipse(0, 14, 18, 5);
        g.fillStyle(this.color); g.fillRect(-6, -3 + b, 12, 8);
        g.fillStyle(this.color); g.fillRect(-7, 3 + b, 14, 6);
        g.fillStyle(dark); g.fillRect(-6, -3 + b, 12, 2);
        g.fillStyle(0xffffff, 0.7);
        g.fillRect(-1, 0 + b, 2, 6);
        g.fillRect(-3, 2 + b, 6, 2);
        g.fillStyle(0xffcc99); g.fillRect(-3, -11 + b, 6, 6);
        g.fillStyle(dark); g.fillRect(-5, -13 + b, 10, 5);
        g.fillStyle(this.color); g.fillRect(-4, -12 + b, 8, 3);
        g.lineStyle(1, 0xffdd88, 0.6); g.strokeCircle(0, -15 + b, 5); g.lineStyle(0);
        g.fillStyle(0xffffff); g.fillRect(flip, -9 + b, 2, 2);
        g.fillStyle(0x44aa44); g.fillRect(flip + 1, -9 + b, 1, 1);
        g.fillStyle(0xccbb88); g.fillRect(flip * 6, -6 + b, flip * 2, 16);
        g.fillStyle(0xffdd88); g.fillRect(flip * 5, -8 + b, flip * 4, 2);
        g.fillStyle(0xffdd88); g.fillRect(flip * 6, -10 + b, flip * 2, 5);
        g.fillStyle(0x664422); g.fillRect(-3, 9 + b, 3, 2); g.fillRect(2, 9 + b, 3, 2);
    }

    _drawSupport(g, dark, light, b, flip) {
        g.fillStyle(0x000000, 0.3); g.fillEllipse(0, 14, 18, 5);
        g.fillStyle(this.color); g.fillRect(-5, -3 + b, 10, 8);
        g.fillStyle(this.color); g.fillRect(-6, 3 + b, 12, 6);
        g.fillStyle(dark); g.fillRect(-5, -3 + b, 10, 2);
        g.fillStyle(0xffcc99); g.fillRect(-3, -11 + b, 6, 6);
        g.fillStyle(dark); g.fillRect(-4, -13 + b, 8, 4);
        g.fillStyle(light, 0.6); g.fillCircle(0, -14 + b, 2);
        g.fillStyle(0xffffff); g.fillRect(flip, -9 + b, 2, 2);
        g.fillStyle(0x886644); g.fillRect(flip * 5, -5 + b, flip * 2, 14);
        g.fillStyle(light); g.fillCircle(flip * 6, -6 + b, 3);
        g.fillStyle(0x664422); g.fillRect(-3, 9 + b, 3, 2); g.fillRect(2, 9 + b, 3, 2);
    }

    _drawEnemy(g, dark, light, b, flip) {
        const etype = this.enemyType;
        if (etype === 'runner' || etype === 'elite_runner') {
            g.fillStyle(0x000000, 0.3); g.fillEllipse(0, 14, 16, 5);
            g.fillStyle(this.color); g.fillRect(-4 + flip * 2, -3 + b, 9, 10);
            g.fillStyle(dark); g.fillRect(-4 + flip * 2, -3 + b, 9, 2);
            g.fillStyle(this.color); g.fillRect(-2 + flip * 2, -10 + b, 6, 6);
            g.fillStyle(light);
            g.beginPath(); g.moveTo(flip * 2, -14 + b); g.lineTo(-2 + flip * 2, -10 + b); g.lineTo(2 + flip * 2, -10 + b); g.closePath(); g.fillPath();
            g.beginPath(); g.moveTo(flip * 4, -13 + b); g.lineTo(1 + flip * 2, -10 + b); g.lineTo(5 + flip * 2, -10 + b); g.closePath(); g.fillPath();
            g.fillStyle(0xff4444); g.fillRect(flip * 2 + 1, -8 + b, 2, 2);
            g.fillStyle(0xccccaa); g.fillRect(flip * 6, 0 + b, flip * 3, 2); g.fillRect(flip * 6, 3 + b, flip * 3, 2);
            g.fillStyle(dark); g.fillRect(-2, 7 + b, 2, 5); g.fillRect(2, 7 + b, 2, 5);
            g.fillStyle(0x554422); g.fillRect(-2, 11 + b, 2, 2); g.fillRect(2, 11 + b, 2, 2);
        } else if (etype === 'bruiser' || etype === 'elite_bruiser') {
            g.fillStyle(0x000000, 0.3); g.fillEllipse(0, 16, 26, 7);
            g.fillStyle(this.color); g.fillRect(-10, -6 + b, 20, 16);
            g.fillStyle(dark); g.fillRect(-10, -6 + b, 20, 3);
            g.fillStyle(this.color); g.fillRect(-5, -14 + b, 10, 8);
            g.fillStyle(dark); g.fillRect(-5, -14 + b, 10, 2);
            g.fillStyle(0xff6644); g.fillRect(-3, -10 + b, 2, 2); g.fillRect(2, -10 + b, 2, 2);
            g.fillStyle(0xeeeecc); g.fillRect(-4, -7 + b, 2, 3); g.fillRect(3, -7 + b, 2, 3);
            g.fillStyle(dark); g.fillRect(-13, -3 + b, 4, 10); g.fillRect(9, -3 + b, 4, 10);
            g.fillStyle(this.color); g.fillRect(-12, -2 + b, 3, 8); g.fillRect(10, -2 + b, 3, 8);
            g.fillStyle(dark); g.fillRect(-6, 10 + b, 5, 5); g.fillRect(2, 10 + b, 5, 5);
            g.fillStyle(0x554422); g.fillRect(-6, 14 + b, 5, 2); g.fillRect(2, 14 + b, 5, 2);
        } else if (etype === 'spitter') {
            g.fillStyle(0x000000, 0.3); g.fillEllipse(0, 14, 16, 5);
            g.fillStyle(this.color); g.fillRect(-5, -2 + b, 10, 10);
            g.fillStyle(dark); g.fillRect(-5, -2 + b, 10, 2);
            g.fillStyle(light, 0.7); g.fillCircle(0, 1 + b, 5);
            g.fillStyle(this.color); g.fillRect(-3, -10 + b, 6, 6);
            g.fillStyle(0x44ff88); g.fillRect(-2, -8 + b, 2, 1); g.fillRect(1, -8 + b, 2, 1);
            g.lineStyle(1, light);
            g.beginPath(); g.moveTo(-2, -10 + b); g.lineTo(-4, -14 + b); g.strokePath();
            g.beginPath(); g.moveTo(2, -10 + b); g.lineTo(4, -14 + b); g.strokePath();
            g.lineStyle(0);
            g.fillStyle(light); g.fillCircle(-4, -14 + b, 1.5); g.fillCircle(4, -14 + b, 1.5);
            g.fillStyle(dark); g.fillRect(-3, 8 + b, 3, 4); g.fillRect(1, 8 + b, 3, 4);
        } else if (etype === 'summoner') {
            g.fillStyle(0x000000, 0.3); g.fillEllipse(0, 14, 18, 5);
            g.fillStyle(this.color); g.fillRect(-5, -3 + b, 10, 8);
            g.fillStyle(this.color); g.fillRect(-6, 3 + b, 12, 6);
            g.fillStyle(dark); g.fillRect(-5, -3 + b, 10, 2);
            g.fillStyle(this.color); g.fillRect(-3, -11 + b, 6, 6);
            g.fillStyle(0xaa44ff, 0.5); g.fillCircle(0, -13 + b, 3);
            g.fillStyle(0xff44ff); g.fillRect(-1, -9 + b, 2, 2);
            g.fillStyle(dark); g.fillRect(-3, 9 + b, 3, 3); g.fillRect(1, 9 + b, 3, 3);
        } else {
            this._drawDefault(g, dark, b, flip);
        }
    }

    _drawBoss(g, dark, light, b, flip) {
        g.fillStyle(0xff2244, 0.08); g.fillCircle(0, 0 + b, 30);
        g.fillStyle(0xff2244, 0.05); g.fillCircle(0, 0 + b, 38);
        g.fillStyle(0x000000, 0.3); g.fillEllipse(0, 22, 36, 10);
        g.fillStyle(this.color); g.fillRect(-14, -10 + b, 28, 24);
        g.fillStyle(dark); g.fillRect(-14, -10 + b, 28, 4);
        g.fillStyle(light, 0.4);
        g.fillRect(-12, -6 + b, 6, 8); g.fillRect(6, -6 + b, 6, 8);
        g.fillStyle(this.color); g.fillRect(-7, -22 + b, 14, 12);
        g.fillStyle(dark); g.fillRect(-7, -22 + b, 14, 3);
        g.fillStyle(0xddccaa);
        g.beginPath(); g.moveTo(-6, -22 + b); g.lineTo(-12, -30 + b); g.lineTo(-4, -22 + b); g.closePath(); g.fillPath();
        g.beginPath(); g.moveTo(6, -22 + b); g.lineTo(12, -30 + b); g.lineTo(4, -22 + b); g.closePath(); g.fillPath();
        g.fillStyle(0xff4444); g.fillRect(-4, -17 + b, 3, 3); g.fillRect(2, -17 + b, 3, 3);
        g.fillStyle(0xffaa44, 0.5); g.fillCircle(-3, -16 + b, 3); g.fillCircle(3, -16 + b, 3);
        g.fillStyle(0xeeeecc); g.fillRect(-3, -12 + b, 2, 3); g.fillRect(2, -12 + b, 2, 3);
        g.fillStyle(dark); g.fillRect(-18, -6 + b, 5, 16); g.fillRect(13, -6 + b, 5, 16);
        g.fillStyle(this.color); g.fillRect(-17, -5 + b, 4, 14); g.fillRect(14, -5 + b, 4, 14);
        g.fillStyle(light); g.fillRect(-18, 8 + b, 6, 5); g.fillRect(13, 8 + b, 6, 5);
        g.fillStyle(dark); g.fillRect(-8, 14 + b, 7, 6); g.fillRect(2, 14 + b, 7, 6);
        g.fillStyle(0x554422); g.fillRect(-8, 19 + b, 7, 3); g.fillRect(2, 19 + b, 7, 3);
    }

    _drawDefault(g, dark, b, flip) {
        g.fillStyle(0x000000, 0.3); g.fillEllipse(0, 14, 18, 5);
        g.fillStyle(this.color); g.fillRect(-6, -3 + b, 12, 12);
        g.fillStyle(dark); g.fillRect(-6, -3 + b, 12, 2);
        g.fillStyle(0xffcc99); g.fillRect(-3, -12 + b, 7, 7);
        g.fillStyle(0xffffff); g.fillRect(flip, -10 + b, 2, 2);
        g.fillStyle(0x333333); g.fillRect(flip + 1, -10 + b, 1, 2);
        g.fillStyle(dark);
        g.fillRect(-4, -14 + b, 9, 3);
        g.fillRect(-3, 9 + b, 3, 4); g.fillRect(2, 9 + b, 3, 4);
        g.fillStyle(0x664422); g.fillRect(-3, 12 + b, 3, 2); g.fillRect(2, 12 + b, 3, 2);
    }

    update(delta, allUnits) {
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
                if (this.hp <= 0) { this.hp = 0; this.die(null, allUnits); return; }
            }
            if (this.bleedTimer <= 0) this.bleeding = false;
        }

        if (this.lastStand && !this.lastStandActive && this.hp <= this.maxHp * 0.25) {
            this.lastStandActive = true;
            this.atk = Math.floor(this.atk * 1.2);
            this.def = Math.floor(this.def * 1.2);
            this.moveSpeed = Math.floor(this.moveSpeed * 1.2);
            DamagePopup.show(this.scene, this.container.x, this.container.y - 35, '각성!', 0xffaa44, false);
        }

        const enemies = allUnits.filter(u => u.alive && u.team !== this.team);
        if (!enemies.length) return;

        this.target = enemies.reduce((a, b) =>
            Math.abs(this.container.x - a.container.x) < Math.abs(this.container.x - b.container.x) ? a : b);
        const dist = Math.abs(this.container.x - this.target.container.x);

        if (dist > this.range) {
            const dir = this.target.container.x > this.container.x ? 1 : -1;
            this.container.x += dir * this.moveSpeed * (delta / 1000);
        } else {
            this.attackTimer += delta;
            if (this.attackTimer >= this.attackSpeed) {
                this.attackTimer = 0;
                this.performAttack(allUnits);
            }
        }

        if (this.shieldTimer > 0) {
            this.shieldTimer -= delta;
            if (this.shieldTimer <= 0) { this.shieldAmount = 0; this.shieldTimer = 0; }
        }
        if (this.atkBuffTimer > 0) {
            this.atkBuffTimer -= delta;
            if (this.atkBuffTimer <= 0) { this.atk -= this.atkBuff; this.atkBuff = 0; }
        }
        if (this.defBuffTimer > 0) {
            this.defBuffTimer -= delta;
            if (this.defBuffTimer <= 0) { this.def -= this.defBuff; this.defBuff = 0; }
        }

        if (this.skillCooldown > 0 && this.team === 'ally') {
            this.skillTimer += delta;
            if (this.skillTimer >= this.skillCooldown) {
                this.skillTimer = 0;
                this._useSkill(allUnits);
            }
        }

        if (this.healCooldown > 0 && this.team === 'enemy') {
            this.healTimer += delta;
            if (this.healTimer >= this.healCooldown) {
                this.healTimer = 0;
                this.performHeal(allUnits);
            }
        }

        if (this.aoeCooldown > 0 && this.team === 'enemy') {
            this.aoeTimer += delta;
            if (this.aoeTimer >= this.aoeCooldown) {
                this.aoeTimer = 0;
                const foes = allUnits.filter(u => u.team !== this.team && u.alive);
                this.performAoe(foes);
            }
        }

        if (this.enemyType === 'bruiser' && this.team === 'enemy') {
            this.abilityTimer += delta;
            if (this.abilityTimer >= 6000) {
                this.abilityTimer = 0;
                const targets = allUnits.filter(u => u.team !== this.team && u.alive && Math.abs(this.container.x - u.container.x) <= 80);
                if (targets.length > 0) {
                    const slamDmg = Math.floor(this.atk * 0.5);
                    targets.forEach(t => {
                        t.takeDamage(slamDmg, this, allUnits);
                        DamagePopup.show(this.scene, t.container.x, t.container.y - 20, slamDmg, 0xcc8822, false);
                    });
                    DamagePopup.show(this.scene, this.container.x, this.container.y - 35, '강타!', 0xcc8822, false);
                    const slam = this.scene.add.circle(this.container.x, this.container.y + 10, 8, 0xcc8822, 0.5).setDepth(50);
                    this.scene.tweens.add({ targets: slam, scaleX: 4, scaleY: 2, alpha: 0, duration: 300, onComplete: () => slam.destroy() });
                }
            }
        }

        if (this.enemyType === 'summoner' && this.team === 'enemy') {
            this.abilityTimer += delta;
            if (this.abilityTimer >= 8000) {
                this.abilityTimer = 0;
                const existingMinions = allUnits.filter(u => u.team === 'enemy' && u.alive && u.isSummon);
                if (existingMinions.length < 3) {
                    const spawnX = this.container.x + Phaser.Math.Between(-40, 40);
                    const spawnY = this.container.y + Phaser.Math.Between(-15, 15);
                    const minion = BattleUnit.fromEnemyData(this.scene, 'runner', 0.6, spawnX, spawnY);
                    minion.isSummon = true;
                    minion.maxHp = Math.floor(minion.maxHp * 0.5);
                    minion.hp = minion.maxHp;
                    this.scene.enemies.push(minion);
                    this.scene.allUnits.push(minion);
                    this.scene._updateEnemyCount();
                    DamagePopup.show(this.scene, this.container.x, this.container.y - 35, '소환!', 0x8844cc, false);
                    const portal = this.scene.add.circle(spawnX, spawnY, 8, 0x8844cc, 0.6).setDepth(50);
                    this.scene.tweens.add({ targets: portal, scaleX: 3, scaleY: 3, alpha: 0, duration: 400, onComplete: () => portal.destroy() });
                }
            }
        }

        if (this.isBoss && this.team === 'enemy') {
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

    _useSkill(allUnits) {
        const allies = allUnits.filter(u => u.team === this.team && u.alive);
        const enemies = allUnits.filter(u => u.team !== this.team && u.alive);

        switch (this.classKey) {
            case 'warrior': {
                const shieldAmt = Math.floor(this.maxHp * 0.3);
                this.shieldAmount = shieldAmt;
                this.shieldTimer = 5000;
                this.defBuff = Math.floor(this.def * 0.3);
                this.def += this.defBuff;
                this.defBuffTimer = 5000;
                DamagePopup.show(this.scene, this.container.x, this.container.y - 35, '방패막이!', 0x4488ff, false);
                const shield = this.scene.add.circle(this.container.x, this.container.y, 15, 0x4488ff, 0.4).setDepth(50);
                this.scene.tweens.add({ targets: shield, scaleX: 2, scaleY: 2, alpha: 0, duration: 400, onComplete: () => shield.destroy() });
                break;
            }
            case 'rogue': {
                if (!this.target || !this.target.alive) break;
                const dmg = Math.floor(this.atk * 2.0);
                this.target.takeDamage(dmg, this, allUnits);
                this.target.bleeding = true;
                this.target.bleedTimer = 4000;
                this.target.bleedTickTimer = 800;
                this.target.bleedDamage = Math.floor(this.atk * 0.3);
                DamagePopup.showCritical(this.scene, this.target.container.x, this.target.container.y - 20, dmg);
                DamagePopup.show(this.scene, this.target.container.x, this.target.container.y - 40, '급소!', 0xff4488, false);
                break;
            }
            case 'mage': {
                if (enemies.length === 0) break;
                const aoeDmg = Math.floor(this.atk * 1.8);
                enemies.forEach(e => {
                    const finalDmg = Math.max(1, aoeDmg - e.def * 0.3);
                    e.takeDamage(Math.floor(finalDmg), this, allUnits);
                });
                const cx = enemies.reduce((s, e) => s + e.container.x, 0) / enemies.length;
                DamagePopup.show(this.scene, cx, 380, '마력 폭발!', 0x8844ff, false);
                const blast = this.scene.add.circle(cx, 430, 20, 0x8844ff, 0.5).setDepth(50);
                this.scene.tweens.add({ targets: blast, scaleX: 6, scaleY: 3, alpha: 0, duration: 500, onComplete: () => blast.destroy() });
                break;
            }
            case 'archer': {
                if (enemies.length === 0) break;
                const sorted = [...enemies].sort((a, b) => a.container.x - b.container.x);
                const pierceDmg = Math.floor(this.atk * 1.5);
                sorted.forEach((e, i) => {
                    const dmg = Math.max(1, Math.floor(pierceDmg * (1 - i * 0.15)) - e.def * 0.3);
                    this.scene.time.delayedCall(i * 100, () => {
                        if (e.alive) {
                            e.takeDamage(Math.floor(dmg), this, allUnits);
                            DamagePopup.show(this.scene, e.container.x, e.container.y - 20, Math.floor(dmg), 0x44cc44, false);
                        }
                    });
                });
                DamagePopup.show(this.scene, this.container.x, this.container.y - 35, '관통!', 0x44cc44, false);
                const arrow = this.scene.add.rectangle(this.container.x, this.container.y - 8, 6, 2, 0x44cc44).setDepth(55);
                this.scene.tweens.add({ targets: arrow, x: 1280, duration: 400, onComplete: () => arrow.destroy() });
                break;
            }
            case 'priest': {
                const healAmt = Math.floor(this.atk * 2.5);
                allies.forEach(a => {
                    const healed = Math.min(healAmt, a.maxHp - a.hp);
                    if (healed > 0) {
                        a.hp += healed;
                        DamagePopup.show(this.scene, a.container.x, a.container.y - 30, healed, 0x44ff88, true);
                    }
                });
                DamagePopup.show(this.scene, this.container.x, this.container.y - 35, '신성 치유!', 0xffcc44, false);
                allies.forEach(a => {
                    const glow = this.scene.add.circle(a.container.x, a.container.y, 10, 0x44ff88, 0.3).setDepth(50);
                    this.scene.tweens.add({ targets: glow, scaleX: 2, scaleY: 2, alpha: 0, duration: 400, onComplete: () => glow.destroy() });
                });
                break;
            }
            case 'alchemist': {
                const buffAmt = Math.floor(this.atk * 0.5);
                allies.forEach(a => {
                    a.atkBuff += buffAmt;
                    a.atk += buffAmt;
                    a.atkBuffTimer = 8000;
                });
                DamagePopup.show(this.scene, this.container.x, this.container.y - 35, '강화 물약!', 0x44cccc, false);
                allies.forEach(a => {
                    const sparkle = this.scene.add.circle(a.container.x, a.container.y - 5, 5, 0x44cccc, 0.5).setDepth(50);
                    this.scene.tweens.add({ targets: sparkle, y: a.container.y - 25, alpha: 0, duration: 500, onComplete: () => sparkle.destroy() });
                });
                break;
            }
        }
    }

    performAttack(allUnits) {
        if (!this.target || !this.target.alive) return;

        let dmg = Math.max(1, this.atk - this.target.def * 0.5);
        let isCrit = Math.random() < this.critRate;
        if (isCrit) dmg = Math.floor(dmg * this.critDmg);
        dmg = Math.floor(dmg * (0.9 + Math.random() * 0.2));

        this.target.takeDamage(dmg, this, allUnits);
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
            DamagePopup.showCritical(this.scene, this.target.container.x, this.target.container.y - 20, dmg);
        } else {
            DamagePopup.show(this.scene, this.target.container.x, this.target.container.y - 20, dmg, 0xffffff, false);
        }

        // chain attack on crit
        if (isCrit && this.chainChance > 0 && Math.random() < this.chainChance) {
            const others = allUnits.filter(u => u.alive && u.team !== this.team && u !== this.target);
            if (others.length > 0) {
                const chainTarget = others[0];
                const chainDmg = Math.floor(dmg * 0.5);
                this.scene.time.delayedCall(200, () => {
                    if (chainTarget.alive) {
                        chainTarget.takeDamage(chainDmg, this, allUnits);
                        DamagePopup.show(this.scene, chainTarget.container.x, chainTarget.container.y - 20, chainDmg, 0xff8844, false);
                    }
                });
            }
        }

        // ranged projectile
        if (this.range > 100) {
            const proj = this.scene.add.circle(this.container.x, this.container.y - 8, 3, this.color).setDepth(50);
            this.scene.tweens.add({ targets: proj, x: this.target.container.x, y: this.target.container.y - 8, duration: 250, onComplete: () => proj.destroy() });
        }
    }

    performHeal(allUnits) {
        const allies = allUnits.filter(u => u.team === this.team && u.alive && u !== this);
        if (!allies.length) return;
        const lowest = allies.reduce((a, b) => (a.hp / a.maxHp) < (b.hp / b.maxHp) ? a : b);
        const healAmt = Math.floor(this.healAmount * (1 + this.healBonus));
        const healed = Math.min(healAmt, lowest.maxHp - lowest.hp);
        if (healed <= 0) return;
        lowest.hp += healed;
        DamagePopup.show(this.scene, lowest.container.x, lowest.container.y - 30, healed, 0x44ff88, true);
    }

    performAoe(enemies) {
        if (enemies.length === 0) return;
        enemies.forEach(e => {
            const dmg = Math.max(1, Math.floor(this.aoeDamage) - e.def);
            e.takeDamage(dmg, this, null);
            this.totalDamageDealt += dmg;
        });
        const cx = enemies.reduce((s, e) => s + e.container.x, 0) / enemies.length;
        const circle = this.scene.add.circle(cx, 430, 100, 0xaa44ff, 0.3).setDepth(50);
        this.scene.tweens.add({ targets: circle, alpha: 0, scaleX: 1.5, scaleY: 1.5, duration: 500, onComplete: () => circle.destroy() });
    }

    takeDamage(amount, attacker, allUnits) {
        if (!this.alive) return;
        if (this.shieldAmount > 0) {
            const absorbed = Math.min(this.shieldAmount, amount);
            this.shieldAmount -= absorbed;
            amount -= absorbed;
            if (absorbed > 0) {
                DamagePopup.show(this.scene, this.container.x, this.container.y - 45, `🛡${absorbed}`, 0x4488ff, false);
            }
            if (amount <= 0) return;
        }
        this.hp -= amount;

        // knockback
        const dir = attacker ? (this.container.x > attacker.container.x ? 1 : -1) : 1;
        const kb = amount > this.maxHp * 0.15 ? 10 : 6;
        this.scene.tweens.add({ targets: this.container, x: this.container.x + dir * kb, duration: 50, yoyo: true });

        if (this.hp <= 0) {
            const overkill = Math.abs(this.hp);
            this.hp = 0;
            this.die(attacker, allUnits);
            if (overkill > 0 && attacker && attacker.alive && allUnits) {
                this._splashOverkill(overkill, attacker, allUnits);
            }
        }
    }

    _splashOverkill(overkill, attacker, allUnits) {
        const maxChains = 3;
        const falloff = 0.7;
        const splashRange = 120;
        let remaining = overkill;
        const hit = new Set();
        let srcX = this.container.x;
        let srcY = this.container.y;
        for (let chain = 0; chain < maxChains && remaining >= 2; chain++) {
            const nearby = allUnits.filter(u =>
                u.team === this.team && u.alive && !hit.has(u) &&
                Math.abs(u.container.x - srcX) < splashRange
            );
            if (nearby.length === 0) break;
            nearby.sort((a, b) => Math.abs(a.container.x - srcX) - Math.abs(b.container.x - srcX));
            const victim = nearby[0];
            hit.add(victim);
            const splashDmg = Math.floor(remaining);
            victim.takeDamage(splashDmg, attacker, allUnits);
            DamagePopup.show(this.scene, victim.container.x, victim.container.y - 25, splashDmg, 0xff8844, false);
            const line = this.scene.add.line(0, 0, srcX, srcY, victim.container.x, victim.container.y, 0xff8844, 0.6).setDepth(55);
            this.scene.tweens.add({ targets: line, alpha: 0, duration: 300, onComplete: () => line.destroy() });
            srcX = victim.container.x;
            srcY = victim.container.y;
            remaining = Math.floor(remaining * falloff);
        }
    }

    die(killer, allUnits) {
        if (!this.alive) return;
        this.alive = false;

        if (killer && killer.lifestealOnKill > 0) {
            const healAmt = Math.floor(killer.maxHp * killer.lifestealOnKill);
            killer.hp = Math.min(killer.maxHp, killer.hp + healAmt);
            DamagePopup.show(this.scene, killer.container.x, killer.container.y - 30, healAmt, 0x44ff88, true);
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

        // ally death: bigger camera shake
        if (this.team === 'ally') {
            this.scene.cameras.main.shake(150, 0.004);
        }

        this.scene.tweens.add({ targets: this.container, alpha: 0, y: this.container.y - 10, duration: 500, ease: 'Power2' });
        this.healthBar.destroy();

        if (this.scene.onUnitDeath) {
            this.scene.onUnitDeath(this, killer);
        }
    }

    destroy() {
        if (this.container) this.container.destroy();
        if (this.healthBar) this.healthBar.destroy();
    }

    static fromMercenary(scene, merc, x, y) {
        const stats = merc.getStats();
        const base = merc.getBaseClass();
        return new BattleUnit(scene, {
            team: 'ally',
            name: merc.name,
            classKey: merc.classKey,
            role: base.role,
            mercId: merc.id,
            hp: merc.currentHp,
            atk: stats.atk,
            def: stats.def,
            attackSpeed: stats.attackSpeed,
            range: stats.range,
            moveSpeed: stats.moveSpeed,
            critRate: stats.critRate,
            critDmg: stats.critDmg,
            color: base.color,
            level: merc.level,
            skillCooldown: stats.skillCooldown || 0,
            x, y
        });
    }

    static fromEnemyData(scene, enemyKey, scaleMult, x, y) {
        const data = ENEMY_DATA[enemyKey];
        return new BattleUnit(scene, {
            team: 'enemy',
            name: data.name,
            classKey: enemyKey,
            role: data.role,
            enemyType: enemyKey,
            hp: Math.floor(data.hp * scaleMult),
            atk: Math.floor(data.atk * scaleMult),
            def: Math.floor(data.def * scaleMult),
            attackSpeed: data.attackSpeed,
            range: data.range,
            moveSpeed: data.moveSpeed,
            critRate: data.critRate,
            critDmg: data.critDmg,
            color: data.color,
            isElite: data.isElite || false,
            isBoss: data.isBoss || false,
            bleedChance: data.bleedChance || 0,
            x, y
        });
    }
}
