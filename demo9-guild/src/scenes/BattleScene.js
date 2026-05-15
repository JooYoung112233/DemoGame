class BattleScene extends Phaser.Scene {
    constructor() { super('BattleScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.zoneKey = data.zoneKey;
        this.party = data.party;
        this.currentRound = data.currentRound || 1;
        this.collectedCards = data.collectedCards || [];
        this.totalGold = data.totalGold || 0;
        this.totalXp = data.totalXp || 0;
        this.loot = data.loot || [];
        this.casualties = data.casualties || [];
        this.partyHpState = data.partyHpState || null;
        this.zoneLevel = this.gameState.zoneLevel[this.zoneKey] || 1;
        this.maxRounds = getMaxRounds(this.zoneLevel);

        // Cargo: compartment defense
        this.cargoHp = data.cargoHp !== undefined ? data.cargoHp : 100;
        this.cargoMaxHp = 100;
        this.cargoDestroyed = data.cargoDestroyed || false;

        // Blackout: curse system
        this.curseLevel = data.curseLevel || 0;
        this.ambushTriggered = false;
    }

    create() {
        this.allies = [];
        this.enemies = [];
        this.allUnits = [];
        this.battleOver = false;
        this.cardSelectActive = false;
        this.speedMultiplier = parseInt(localStorage.getItem('demo9_speed') || '1', 10) || 1;
        this.killStreak = 0;
        this.killStreakTimer = 0;
        this.battleTime = 0;
        this._dangerVignette = null;
        this._cargoAttackTimer = 0;
        this._ambushTimer = 0;

        this._drawBackground();
        this._drawHUD();
        this._spawnAllies();
        if (this.currentRound === 1) this._applyBlessings();
        this._spawnEnemies();
        this._applyCards();
        this._applyZoneEffects();
        this._initZoneMechanics();
        this._showRoundAnnounce();
    }

    _drawBackground() {
        const zone = ZONE_DATA[this.zoneKey];
        const gfx = this.add.graphics();

        if (this.zoneKey === 'cargo') {
            gfx.fillGradientStyle(0x1a1508, 0x1a1508, 0x2a2010, 0x2a2010);
            gfx.fillRect(0, 0, 1280, 720);
            gfx.fillStyle(0x22190a);
            gfx.fillRect(0, 450, 1280, 270);
            gfx.fillStyle(0x443311);
            gfx.fillRect(0, 448, 1280, 4);
            for (let i = 0; i < 6; i++) {
                gfx.fillStyle(0x332200, 0.4);
                const bx = 50 + i * 200 + Phaser.Math.Between(-20, 20);
                gfx.fillRect(bx, 460, 60, 40);
                gfx.lineStyle(1, 0x554422, 0.3);
                gfx.strokeRect(bx, 460, 60, 40);
            }
            gfx.lineStyle(2, 0x554422, 0.15);
            gfx.lineBetween(0, 500, 1280, 500);
            gfx.lineBetween(0, 520, 1280, 520);
        } else if (this.zoneKey === 'blackout') {
            gfx.fillGradientStyle(0x0a0515, 0x0a0515, 0x15102a, 0x15102a);
            gfx.fillRect(0, 0, 1280, 720);
            gfx.fillStyle(0x110a22);
            gfx.fillRect(0, 450, 1280, 270);
            gfx.fillStyle(0x221144);
            gfx.fillRect(0, 448, 1280, 4);
            for (let i = 0; i < 12; i++) {
                gfx.fillStyle(0x220044, 0.2);
                gfx.fillCircle(Phaser.Math.Between(50, 1230), Phaser.Math.Between(460, 700), Phaser.Math.Between(3, 12));
            }
            for (let i = 0; i < 5; i++) {
                const fx = Phaser.Math.Between(100, 1180);
                const fy = Phaser.Math.Between(20, 420);
                gfx.fillStyle(0x8844ff, 0.04);
                gfx.fillCircle(fx, fy, Phaser.Math.Between(30, 80));
            }
        } else {
            gfx.fillGradientStyle(0x1a0a0a, 0x1a0a0a, 0x2a1515, 0x2a1515);
            gfx.fillRect(0, 0, 1280, 720);
            gfx.fillStyle(0x221111);
            gfx.fillRect(0, 450, 1280, 270);
            gfx.fillStyle(0x331818);
            gfx.fillRect(0, 448, 1280, 4);
            for (let i = 0; i < 8; i++) {
                gfx.fillStyle(0x330000, 0.3);
                gfx.fillCircle(Phaser.Math.Between(50, 1230), Phaser.Math.Between(460, 700), Phaser.Math.Between(5, 15));
            }
        }

        gfx.fillStyle(zone.color, 0.02);
        gfx.fillRect(0, 0, 1280, 720);
    }

    _drawHUD() {
        const zone = ZONE_DATA[this.zoneKey];
        const isBoss = this.currentRound >= this.maxRounds;

        const dangerLabel = isBoss ? '💀 보스 출현!' : `라운드 ${this.currentRound} / ${this.maxRounds}`;
        this.add.text(640, 25, dangerLabel, {
            fontSize: '20px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(100);

        const meterX = 440, meterY = 42, meterW = 400, meterH = 6;
        this.add.rectangle(meterX + meterW / 2, meterY, meterW, meterH, 0x331111).setDepth(100);
        const fillW = (this.currentRound / this.maxRounds) * meterW;
        const progress = this.currentRound / this.maxRounds;
        const fillColor = progress <= 0.4 ? 0x44aa44 : progress <= 0.7 ? 0xffaa00 : 0xff2222;
        if (fillW > 0) {
            this.add.rectangle(meterX + fillW / 2, meterY, fillW, meterH, fillColor).setDepth(101);
        }

        this.timerText = this.add.text(640, 50, '0.0초', {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa6666'
        }).setOrigin(0.5).setDepth(100);

        this.add.text(60, 25, `${zone.icon} ${zone.name}`, {
            fontSize: '14px', fontFamily: 'monospace', color: zone.textColor, fontStyle: 'bold'
        }).setDepth(100);

        this.add.text(60, 45, `구역 Lv.${this.zoneLevel}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#886666'
        }).setDepth(100);

        this.goldText = this.add.text(1240, 50, `💰 ${this.totalGold}G`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44'
        }).setOrigin(1, 0).setDepth(100);

        this.enemyCountText = this.add.text(1240, 25, '', {
            fontSize: '12px', fontFamily: 'monospace', color: '#ff8888'
        }).setOrigin(1, 0).setDepth(100);

        if (this.collectedCards.length > 0) {
            let cx = 20;
            this.add.text(cx, 690, '카드:', {
                fontSize: '10px', fontFamily: 'monospace', color: '#886644'
            }).setDepth(100);
            cx += 40;
            this.collectedCards.forEach(key => {
                const card = CARD_DATA[key];
                if (card) {
                    this.add.text(cx, 690, `[${card.name}]`, {
                        fontSize: '10px', fontFamily: 'monospace', color: '#ffaa44'
                    }).setDepth(100);
                    cx += card.name.length * 10 + 20;
                }
            });
        }

        const speedBtn = this.add.text(1220, 690, `×${this.speedMultiplier}`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
            backgroundColor: '#331111', padding: { x: 6, y: 3 }
        }).setOrigin(0.5).setDepth(100).setInteractive();
        speedBtn.on('pointerdown', () => {
            this.speedMultiplier = this.speedMultiplier === 1 ? 2 : this.speedMultiplier === 2 ? 3 : 1;
            speedBtn.setText(`×${this.speedMultiplier}`);
            localStorage.setItem('demo9_speed', this.speedMultiplier);
        });

        // --- CARGO: Cargo HP bar ---
        if (this.zoneKey === 'cargo' && !this.cargoDestroyed) {
            this._drawCargoHpBar();
        }

        // --- BLACKOUT: Curse level indicator ---
        if (this.zoneKey === 'blackout') {
            this._drawCurseIndicator();
        }
    }

    _drawCargoHpBar() {
        const barX = 40, barY = 670, barW = 200, barH = 14;
        const ratio = this.cargoHp / this.cargoMaxHp;
        const bgBar = this.add.graphics().setDepth(100);
        bgBar.fillStyle(0x332211, 1);
        bgBar.fillRoundedRect(barX, barY, barW, barH, 3);
        const hpColor = ratio > 0.5 ? 0xff8844 : ratio > 0.25 ? 0xff6622 : 0xff2222;
        bgBar.fillStyle(hpColor, 1);
        bgBar.fillRoundedRect(barX, barY, barW * ratio, barH, 3);
        bgBar.lineStyle(1, 0x886644, 0.5);
        bgBar.strokeRoundedRect(barX, barY, barW, barH, 3);

        this.add.text(barX + 5, barY + 1, `📦 화물 HP: ${this.cargoHp}/${this.cargoMaxHp}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setDepth(101);

        this.cargoHpBar = bgBar;

        // cargo visual on battlefield
        this._cargoSprite = this.add.graphics().setDepth(3);
        this._cargoSprite.fillStyle(0xff8844, 0.8);
        this._cargoSprite.fillRect(60, 435, 40, 25);
        this._cargoSprite.lineStyle(2, 0xffaa44, 0.6);
        this._cargoSprite.strokeRect(60, 435, 40, 25);
        this._cargoSprite.fillStyle(0xffcc44);
        this._cargoSprite.fillRect(72, 440, 16, 8);
        this.add.text(80, 425, '📦', { fontSize: '12px' }).setOrigin(0.5).setDepth(4);
    }

    _updateCargoHpBar() {
        if (!this.cargoHpBar || this.cargoDestroyed) return;
        const barX = 40, barY = 670, barW = 200, barH = 14;
        const ratio = this.cargoHp / this.cargoMaxHp;
        this.cargoHpBar.clear();
        this.cargoHpBar.fillStyle(0x332211, 1);
        this.cargoHpBar.fillRoundedRect(barX, barY, barW, barH, 3);
        const hpColor = ratio > 0.5 ? 0xff8844 : ratio > 0.25 ? 0xff6622 : 0xff2222;
        this.cargoHpBar.fillStyle(hpColor, 1);
        this.cargoHpBar.fillRoundedRect(barX, barY, barW * Math.max(0, ratio), barH, 3);
        this.cargoHpBar.lineStyle(1, 0x886644, 0.5);
        this.cargoHpBar.strokeRoundedRect(barX, barY, barW, barH, 3);
    }

    _drawCurseIndicator() {
        const cx = 40, cy = 665;
        const skulls = '💀'.repeat(Math.min(this.curseLevel, 5));
        const label = this.curseLevel === 0 ? '저주 없음' : `저주 Lv.${this.curseLevel} ${skulls}`;
        const color = this.curseLevel === 0 ? '#666688' : this.curseLevel <= 2 ? '#aa66ff' : this.curseLevel <= 4 ? '#cc44ff' : '#ff22ff';
        this.curseText = this.add.text(cx, cy, `🔮 ${label}`, {
            fontSize: '11px', fontFamily: 'monospace', color, fontStyle: 'bold'
        }).setDepth(100);

        if (this.curseLevel > 0) {
            this.add.text(cx, cy + 16, `적 강화 +${this.curseLevel * 8}% | 드랍 보너스 +${this.curseLevel}등급`, {
                fontSize: '9px', fontFamily: 'monospace', color: '#886688'
            }).setDepth(100);
        }

        // darkness overlay
        if (this.curseLevel > 0) {
            const darkness = this.add.rectangle(640, 360, 1280, 720, 0x000000, Math.min(0.35, this.curseLevel * 0.07)).setDepth(2);
            this.tweens.add({ targets: darkness, alpha: darkness.alpha * 0.6, duration: 2000, yoyo: true, repeat: -1 });
        }
    }

    _initZoneMechanics() {
        if (this.zoneKey === 'cargo') {
            this._initCargoMechanics();
        } else if (this.zoneKey === 'blackout') {
            this._initBlackoutMechanics();
        }
    }

    _initCargoMechanics() {
        if (this.cargoDestroyed) return;
        // enemies periodically damage cargo
        this._cargoAttackInterval = 4000 + Math.random() * 2000;
        this._cargoAttackTimer = this._cargoAttackInterval * 0.5;
    }

    _updateCargoMechanics(dt) {
        if (this.cargoDestroyed || this.zoneKey !== 'cargo') return;
        this._cargoAttackTimer += dt;
        if (this._cargoAttackTimer >= this._cargoAttackInterval) {
            this._cargoAttackTimer = 0;
            this._cargoAttackInterval = 3000 + Math.random() * 2000;
            const aliveEnemies = this.enemies.filter(e => e.alive);
            if (aliveEnemies.length > 0) {
                const attacker = aliveEnemies[Math.floor(Math.random() * aliveEnemies.length)];
                const dmg = Math.floor(3 + this.currentRound * 2 + this.zoneLevel * 2);
                this.cargoHp = Math.max(0, this.cargoHp - dmg);
                this._updateCargoHpBar();

                // visual
                if (this._cargoSprite) {
                    this.tweens.add({ targets: this._cargoSprite, alpha: 0.3, duration: 80, yoyo: true });
                }
                const dmgTxt = this.add.text(80, 420, `-${dmg}`, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#ff6644', fontStyle: 'bold',
                    stroke: '#000', strokeThickness: 2
                }).setOrigin(0.5).setDepth(90);
                this.tweens.add({ targets: dmgTxt, y: 400, alpha: 0, duration: 600, onComplete: () => dmgTxt.destroy() });

                if (this.cargoHp <= 0) {
                    this.cargoDestroyed = true;
                    DamagePopup.show(this, 640, 400, '화물 파괴!', 0xff4422, false);
                    this.cameras.main.shake(200, 0.004);
                    if (this._cargoSprite) {
                        this.tweens.add({ targets: this._cargoSprite, alpha: 0, duration: 500 });
                    }
                    // explosion
                    for (let i = 0; i < 6; i++) {
                        const p = this.add.circle(80 + Phaser.Math.Between(-20, 20), 445 + Phaser.Math.Between(-15, 15), 4, 0xff8844, 0.8).setDepth(50);
                        this.tweens.add({ targets: p, alpha: 0, scaleX: 3, scaleY: 3, duration: 400, delay: i * 50, onComplete: () => p.destroy() });
                    }
                }
            }
        }
    }

    _initBlackoutMechanics() {
        // curse increases each round automatically
        if (this.currentRound > 1 && this.curseLevel === 0) {
            this.curseLevel = this.currentRound - 1;
        }

        // apply curse scaling to enemies
        if (this.curseLevel > 0) {
            const curseMult = 1 + this.curseLevel * 0.08;
            this.enemies.forEach(e => {
                e.atk = Math.floor(e.atk * curseMult);
                e.maxHp = Math.floor(e.maxHp * curseMult);
                e.hp = Math.min(e.hp, e.maxHp);
            });
        }

        // ambush chance
        this._ambushChance = 0.15 + this.curseLevel * 0.05;
        this._ambushTimer = 5000 + Math.random() * 5000;
    }

    _updateBlackoutMechanics(dt) {
        if (this.zoneKey !== 'blackout') return;
        if (this.ambushTriggered) return;

        this._ambushTimer -= dt;
        if (this._ambushTimer <= 0 && Math.random() < this._ambushChance) {
            this.ambushTriggered = true;
            this._triggerAmbush();
        }
    }

    _triggerAmbush() {
        DamagePopup.show(this, 640, 350, '⚠ 기습!', 0xaa44ff, false);
        this.cameras.main.shake(150, 0.003);

        // flash darkness
        const flash = this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.5).setDepth(150);
        this.tweens.add({ targets: flash, alpha: 0, duration: 800, onComplete: () => flash.destroy() });

        const count = 1 + Math.floor(this.curseLevel * 0.5);
        const types = ['wraith', 'shade'];
        for (let i = 0; i < count; i++) {
            const type = types[Math.floor(Math.random() * types.length)];
            const x = Phaser.Math.Between(800, 1200);
            const y = 420 + Phaser.Math.Between(-10, 20);
            const scaleMult = 1 + (this.zoneLevel - 1) * 0.1;
            const unit = BattleUnit.fromEnemyData(this, type, scaleMult * 0.8, x, y);
            unit.isSummon = true;
            this.enemies.push(unit);
            this.allUnits.push(unit);

            const portal = this.add.circle(x, y, 10, 0x8844ff, 0.6).setDepth(50);
            this.tweens.add({ targets: portal, scaleX: 3, scaleY: 3, alpha: 0, duration: 500, onComplete: () => portal.destroy() });
        }
        this._updateEnemyCount();
    }

    _spawnAllies() {
        const startX = 180;
        const spacing = 70;
        this.party.forEach((merc, idx) => {
            if (!merc.alive) return;
            merc._trainingRef = this.gameState.training;
            if (this.partyHpState && this.partyHpState[merc.id] !== undefined) {
                merc.currentHp = this.partyHpState[merc.id];
            }
            const x = startX + idx * spacing;
            const y = 430 + (idx % 2 === 0 ? 0 : 15);
            const unit = BattleUnit.fromMercenary(this, merc, x, y);
            this.allies.push(unit);
            this.allUnits.push(unit);
        });
    }

    _spawnEnemies() {
        const { composition, scaleMult } = getEnemyComposition(this.currentRound, this.zoneLevel, this.zoneKey);

        const startX = 1100;
        const spacing = 55;
        let idx = 0;

        composition.forEach(group => {
            for (let i = 0; i < group.count; i++) {
                const x = startX - (idx % 6) * spacing;
                const y = 420 + Math.floor(idx / 6) * 40 + (idx % 2 === 0 ? 0 : 15);
                const unit = BattleUnit.fromEnemyData(this, group.type, scaleMult, x, y);
                this.enemies.push(unit);
                this.allUnits.push(unit);
                idx++;
            }
        });

        if (this.currentRound >= this.maxRounds) {
            const bossType = getZoneBoss(this.zoneKey);
            const bossScale = scaleMult * (0.8 + this.zoneLevel * 0.15);
            const bossUnit = BattleUnit.fromEnemyData(this, bossType, bossScale, 1050, 420);
            this.enemies.push(bossUnit);
            this.allUnits.push(bossUnit);
        }

        this._updateEnemyCount();
    }

    _applyCards() {
        this.collectedCards.forEach(key => {
            const card = CARD_DATA[key];
            if (card && card.apply) {
                const allyUnits = this.allies.filter(u => u.alive);
                card.apply(allyUnits);
            }
        });
    }

    _applyZoneEffects() {
        const progress = this.currentRound / this.maxRounds;
        switch (this.zoneKey) {
            case 'bloodpit':
                this.enemies.forEach(e => {
                    e.atk = Math.floor(e.atk * (1 + progress * 0.15));
                    e.critRate = Math.min(0.5, e.critRate + progress * 0.05);
                });
                this.allies.forEach(a => {
                    a.lifesteal += 0.03;
                });
                this._zoneLabel = '🩸 핏 게이지: 적 공격력↑, 아군 흡혈 +3%';
                break;
            case 'cargo':
                this.allies.forEach(a => {
                    a.def = Math.floor(a.def * 1.15);
                });
                this.enemies.forEach(e => {
                    e.moveSpeed = Math.floor(e.moveSpeed * 1.2);
                });
                this._zoneLabel = this.cargoDestroyed
                    ? '📦 화물 파괴됨! 보상 감소'
                    : '📦 화물 방어: 화물을 지키면 보너스 보상!';
                break;
            case 'blackout': {
                const curseMult = 1 + this.curseLevel * 0.05;
                this.allies.forEach(a => {
                    a.critDmg += 0.3;
                    a.range = Math.floor(a.range * 0.7);
                });
                this.enemies.forEach(e => {
                    e.moveSpeed = Math.floor(e.moveSpeed * 0.85);
                    e.bleedChance = Math.min(0.5, (e.bleedChance || 0) + 0.1 + this.curseLevel * 0.03);
                });
                this._zoneLabel = `🔦 저주 Lv.${this.curseLevel}: 크리피해 +30%, 기습 위험, 적 저주 출혈`;
                break;
            }
        }
        if (this._zoneLabel) {
            this.add.text(640, 70, this._zoneLabel, {
                fontSize: '10px', fontFamily: 'monospace', color: '#886666'
            }).setOrigin(0.5).setDepth(100);
        }
    }

    _applyBlessings() {
        const gs = this.gameState;
        if (!gs.blessings || gs.blessings.length === 0) return;
        this.allies.forEach(a => {
            if (gs.blessings.includes('bless_atk'))   a.atk = Math.floor(a.atk * 1.1);
            if (gs.blessings.includes('bless_def'))   a.def = Math.floor(a.def * 1.1);
            if (gs.blessings.includes('bless_hp'))  { a.maxHp = Math.floor(a.maxHp * 1.1); a.hp = Math.min(a.hp + Math.floor(a.maxHp * 0.1), a.maxHp); }
            if (gs.blessings.includes('bless_crit'))  { a.critRate = Math.min(0.8, a.critRate + 0.1); a.critDmg += 0.2; }
            if (gs.blessings.includes('bless_speed')) { a.moveSpeed = Math.floor(a.moveSpeed * 1.1); a.attackSpeed = Math.floor(a.attackSpeed * 0.9); }
        });
        gs.blessings = [];
        SaveManager.save(gs);
    }

    _showRoundAnnounce() {
        const isBoss = this.currentRound >= this.maxRounds;
        const label = isBoss ? '💀 보스 출현!' : `라운드 ${this.currentRound}`;
        const txt = this.add.text(640, 300, label, {
            fontSize: '38px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(200).setAlpha(0);
        this.tweens.add({
            targets: txt, alpha: 1, duration: 300,
            yoyo: true, hold: 600,
            onComplete: () => txt.destroy()
        });

        this.onUnitDeath = (unit, killer) => {
            if (unit.team === 'enemy') {
                this._updateEnemyCount();
                let goldDrop = Phaser.Math.Between(8, 18) + this.currentRound * 4;
                if (unit.isElite) goldDrop += 20;
                if (unit.isBoss) goldDrop += 100;
                if (this.collectedCards.includes('golden_hands')) goldDrop = Math.floor(goldDrop * 1.2);
                // Cargo: bonus gold for alive cargo
                if (this.zoneKey === 'cargo' && !this.cargoDestroyed) {
                    goldDrop = Math.floor(goldDrop * 1.15);
                }
                this.totalGold += goldDrop;
                this.goldText.setText(`💰 ${this.totalGold}G`);

                const gText = this.add.text(unit.container.x, unit.container.y - 10, `+${goldDrop}G`, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
                    stroke: '#000', strokeThickness: 2
                }).setOrigin(0.5).setDepth(90);
                this.tweens.add({ targets: gText, y: gText.y - 30, alpha: 0, duration: 800, onComplete: () => gText.destroy() });

                const luckyBonus = this.collectedCards.includes('lucky') ? 1 : 0;
                const dropChance = unit.isBoss ? 1.0 : 0.3 + this.currentRound * 0.04;
                const bossRarityBonus = unit.isBoss ? 2 : (unit.isElite ? 1 : 0);
                // Blackout: curse level adds rarity bonus
                const curseRarityBonus = this.zoneKey === 'blackout' ? this.curseLevel : 0;
                if (Math.random() < dropChance) {
                    const item = generateItem(this.zoneKey, this.gameState.guildLevel, luckyBonus + bossRarityBonus + curseRarityBonus);
                    if (item) {
                        this.loot.push(item);
                        this._showLootPopup(unit.container.x, unit.container.y - 40, item);
                    }
                }
            }

            this._checkRoundEnd();
        };
    }

    _showLootPopup(x, y, item) {
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
        const bg = this.add.rectangle(x, y, 120, 24, 0x111111, 0.9).setStrokeStyle(1, rarity.color).setDepth(90);
        const txt = this.add.text(x, y, `📦 ${item.name}`, {
            fontSize: '10px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(91);

        this.tweens.add({
            targets: [bg, txt], y: y - 40, alpha: 0, duration: 1500, delay: 800,
            onComplete: () => { bg.destroy(); txt.destroy(); }
        });
    }

    _updateEnemyCount() {
        const alive = this.enemies.filter(e => e.alive).length;
        this.enemyCountText.setText(`적: ${alive}`);
    }

    _checkRoundEnd() {
        if (this.battleOver) return;
        const allyAlive = this.allies.filter(u => u.alive);
        const enemyAlive = this.enemies.filter(u => u.alive);

        if (allyAlive.length === 0) {
            this.battleOver = true;
            this.cameras.main.shake(200, 0.005);
            this.time.delayedCall(800, () => this._endRun(false));
            return;
        }

        if (enemyAlive.length === 0) {
            this.battleOver = true;
            this.cameras.main.shake(100, 0.002);

            this._snapshotAllyState();

            const isBoss = this.currentRound >= this.maxRounds;
            const roundXp = 10 + this.currentRound * 5 + (isBoss ? 30 : 0);
            this.totalXp += roundXp;

            const roundBonus = 15 + this.currentRound * 10 + (isBoss ? 50 : 0);
            this.totalGold += roundBonus;
            this.goldText.setText(`💰 ${this.totalGold}G`);

            const clearTxt = this.add.text(640, 300, 'ROUND CLEAR', {
                fontSize: '36px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold',
                stroke: '#000', strokeThickness: 4
            }).setOrigin(0.5).setDepth(300).setAlpha(0);
            this.tweens.add({
                targets: clearTxt, alpha: 1, duration: 300, hold: 800,
                onComplete: () => {
                    clearTxt.destroy();
                    if (this.currentRound >= this.maxRounds) {
                        this.cameras.main.fadeOut(400, 0, 0, 0, (cam, progress) => {
                            if (progress === 1) this._endRun(true);
                        });
                    } else {
                        this.cameras.main.fadeOut(400, 0, 0, 0, (cam, progress) => {
                            if (progress === 1) this._showCardSelect();
                        });
                    }
                }
            });
        }
    }

    _snapshotAllyState() {
        this._lastAllyHp = {};
        this.allies.forEach(u => {
            const merc = this.party.find(m => m.id === u.mercId);
            if (!merc) return;
            if (u.alive) {
                this._lastAllyHp[u.mercId] = u.hp;
                merc.currentHp = u.hp;
            } else {
                if (!this.casualties.find(c => c.id === merc.id)) {
                    this.casualties.push(merc);
                }
                merc.alive = false;
            }
        });
    }

    _getRerollCost() {
        if (this._rerollCount === 0) return 0;
        return Math.min(1000, this._rerollCount * 150);
    }

    _showCardSelect() {
        this.cardSelectActive = true;
        this.cameras.main.fadeIn(300);

        this.allUnits.forEach(u => u.destroy());
        this.allUnits = [];
        this.allies = [];
        this.enemies = [];

        if (this._rerollCount === undefined) this._rerollCount = 0;
        this._cardUIObjects = [];

        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a).setDepth(80);

        this.add.text(640, 60, `라운드 ${this.currentRound} 클리어!`, {
            fontSize: '24px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(81);

        let statusLine = `생존: ${this.party.filter(m => m.alive).length}/${this.party.length}  |  💰 ${this.totalGold}G  |  💚 HP 5% 회복`;
        if (this.zoneKey === 'cargo' && !this.cargoDestroyed) {
            statusLine += `  |  📦 화물 ${this.cargoHp}%`;
        }
        if (this.zoneKey === 'blackout') {
            statusLine += `  |  🔮 저주 Lv.${this.curseLevel}`;
        }
        this._cardGoldText = this.add.text(640, 90, statusLine, {
            fontSize: '11px', fontFamily: 'monospace', color: '#aa6666'
        }).setOrigin(0.5).setDepth(81);

        this.add.text(640, 115, '카드를 선택하세요', {
            fontSize: '14px', fontFamily: 'monospace', color: '#cc8888'
        }).setOrigin(0.5).setDepth(81);

        this._drawCardChoices();
    }

    _drawCardChoices() {
        if (this._cardUIObjects) {
            this._cardUIObjects.forEach(obj => obj.destroy());
        }
        this._cardUIObjects = [];

        // re-draw zone choices since they were cleared
        this._drawZoneChoicesOnly();

        const cardChoices = generateCardChoices(this.currentRound, this.collectedCards, this.zoneLevel);

        if (cardChoices.length === 0) {
            const t = this.add.text(640, 360, '선택 가능한 카드가 없습니다', {
                fontSize: '14px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5).setDepth(81);
            this._cardUIObjects.push(t);
            this.time.delayedCall(1000, () => this._proceedToNextRound(null));
            return;
        }

        const cardW = 280, cardH = 200;
        const gap = 30;
        const totalW = cardChoices.length * cardW + (cardChoices.length - 1) * gap;
        const startX = 640 - totalW / 2;

        cardChoices.forEach((key, idx) => {
            const card = CARD_DATA[key];
            const cx = startX + idx * (cardW + gap) + cardW / 2;
            const cy = 310;

            const categoryColors = {
                buff: { bg: 0x1a2a3a, border: 0x4488ff, text: '#4488ff', label: '강화' },
                combat: { bg: 0x3a1a1a, border: 0xff4444, text: '#ff4444', label: '전투' },
                loot: { bg: 0x2a2a1a, border: 0xffcc44, text: '#ffcc44', label: '파밍' },
                heal: { bg: 0x1a3a1a, border: 0x44ff88, text: '#44ff88', label: '회복' }
            };
            const cc = categoryColors[card.category] || categoryColors.buff;
            const tierLabel = card.tier === 3 ? '★' : card.tier === 2 ? '◆' : '';

            const bg = this.add.graphics().setDepth(82);
            bg.fillStyle(cc.bg, 1);
            bg.fillRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);
            bg.lineStyle(2, cc.border, 0.7);
            bg.strokeRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);
            this._cardUIObjects.push(bg);

            const t1 = this.add.text(cx, cy - cardH / 2 + 20, `${cc.label} ${tierLabel}`, {
                fontSize: '10px', fontFamily: 'monospace', color: cc.text
            }).setOrigin(0.5).setDepth(83);
            this._cardUIObjects.push(t1);

            const t2 = this.add.text(cx, cy - 20, card.name, {
                fontSize: '18px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(83);
            this._cardUIObjects.push(t2);

            const t3 = this.add.text(cx, cy + 20, card.desc, {
                fontSize: '12px', fontFamily: 'monospace', color: '#aaaacc',
                wordWrap: { width: cardW - 30 }, align: 'center'
            }).setOrigin(0.5).setDepth(83);
            this._cardUIObjects.push(t3);

            const hitZone = this.add.zone(cx, cy, cardW, cardH).setInteractive({ useHandCursor: true }).setDepth(84);
            this._cardUIObjects.push(hitZone);
            hitZone.on('pointerover', () => {
                bg.clear();
                bg.fillStyle(cc.bg, 1);
                bg.fillRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);
                bg.lineStyle(3, cc.border, 1);
                bg.strokeRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);
            });
            hitZone.on('pointerout', () => {
                bg.clear();
                bg.fillStyle(cc.bg, 1);
                bg.fillRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);
                bg.lineStyle(2, cc.border, 0.7);
                bg.strokeRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);
            });
            hitZone.on('pointerdown', () => {
                this._proceedToNextRound(key);
            });
        });

        // retreat button
        const retBg = this.add.rectangle(380, 500, 110, 32, 0x332222).setStrokeStyle(1, 0x664444).setDepth(84).setInteractive({ useHandCursor: true });
        const retTxt = this.add.text(380, 500, '🚪 철수', {
            fontSize: '12px', fontFamily: 'monospace', color: '#ff8888'
        }).setOrigin(0.5).setDepth(85);
        retBg.on('pointerover', () => retBg.setFillStyle(0x443333));
        retBg.on('pointerout', () => retBg.setFillStyle(0x332222));
        retBg.on('pointerdown', () => this._endRun(false));
        this._cardUIObjects.push(retBg, retTxt);

        // skip button
        const skipBg = this.add.rectangle(540, 500, 100, 32, 0x222222).setStrokeStyle(1, 0x444444).setDepth(84).setInteractive({ useHandCursor: true });
        const skipTxt = this.add.text(540, 500, '스킵', {
            fontSize: '12px', fontFamily: 'monospace', color: '#666677'
        }).setOrigin(0.5).setDepth(85);
        skipBg.on('pointerover', () => skipBg.setFillStyle(0x333333));
        skipBg.on('pointerout', () => skipBg.setFillStyle(0x222222));
        skipBg.on('pointerdown', () => this._proceedToNextRound(null));
        this._cardUIObjects.push(skipBg, skipTxt);

        // reroll button
        const rerollCost = this._getRerollCost();
        const costLabel = rerollCost === 0 ? '🎲 리롤 (무료)' : `🎲 리롤 (${rerollCost}G)`;
        const canAfford = this.totalGold >= rerollCost;
        const rrColor = canAfford ? 0x2a2244 : 0x222222;
        const rrBorder = canAfford ? 0x8855cc : 0x444444;
        const rrTextColor = canAfford ? '#bb88ff' : '#555555';

        const rrBg = this.add.rectangle(740, 500, 140, 32, rrColor).setStrokeStyle(1, rrBorder).setDepth(84);
        const rrTxt = this.add.text(740, 500, costLabel, {
            fontSize: '11px', fontFamily: 'monospace', color: rrTextColor
        }).setOrigin(0.5).setDepth(85);
        this._cardUIObjects.push(rrBg, rrTxt);

        if (canAfford) {
            rrBg.setInteractive({ useHandCursor: true });
            rrBg.on('pointerover', () => rrBg.setFillStyle(0x3a3355));
            rrBg.on('pointerout', () => rrBg.setFillStyle(0x2a2244));
            rrBg.on('pointerdown', () => {
                this.totalGold -= rerollCost;
                this._rerollCount++;
                this._cardGoldText.setText(`생존: ${this.party.filter(m => m.alive).length}/${this.party.length}  |  💰 ${this.totalGold}G`);
                this.goldText.setText(`💰 ${this.totalGold}G`);
                this._drawCardChoices();
            });
        }
    }

    _drawZoneChoicesOnly() {
        // Separated so card reroll doesn't destroy zone choice buttons
        if (this.zoneKey === 'cargo' && !this.cargoDestroyed) {
            const repairCost = 30 + this.currentRound * 10;
            const canRepair = this.totalGold >= repairCost && this.cargoHp < this.cargoMaxHp;
            const repairAmt = Math.min(25, this.cargoMaxHp - this.cargoHp);

            if (repairAmt > 0) {
                const btn = UIButton.create(this, 200, 560, 180, 26, `🔧 수리 +${repairAmt}HP (${repairCost}G)`, {
                    color: canRepair ? 0x443322 : 0x222222,
                    hoverColor: canRepair ? 0x554433 : 0x222222,
                    textColor: canRepair ? '#ffcc88' : '#555555',
                    fontSize: 10,
                    onClick: () => {
                        if (!canRepair) return;
                        this.totalGold -= repairCost;
                        this.cargoHp = Math.min(this.cargoMaxHp, this.cargoHp + repairAmt);
                        UIToast.show(this, `화물 수리! HP ${this.cargoHp}/${this.cargoMaxHp}`, { color: '#ff8844' });
                    }
                }).setDepth(86);
                this._cardUIObjects.push(btn);
            }

            const fortifyCost = 50 + this.currentRound * 15;
            const canFortify = this.totalGold >= fortifyCost;
            const fBtn = UIButton.create(this, 420, 560, 180, 26, `🛡 방벽 강화 +20HP (${fortifyCost}G)`, {
                color: canFortify ? 0x334455 : 0x222222,
                hoverColor: canFortify ? 0x445566 : 0x222222,
                textColor: canFortify ? '#88ccff' : '#555555',
                fontSize: 10,
                onClick: () => {
                    if (!canFortify) return;
                    this.totalGold -= fortifyCost;
                    this.cargoMaxHp += 20;
                    this.cargoHp += 20;
                    UIToast.show(this, `화물 방벽 강화! 최대 HP +20`, { color: '#88ccff' });
                    fBtn.destroy();
                }
            }).setDepth(86);
            this._cardUIObjects.push(fBtn);
        }

        if (this.zoneKey === 'blackout') {
            const cleanseCost = 50 + this.curseLevel * 40;
            const canCleanse = this.totalGold >= cleanseCost && this.curseLevel > 0;

            const cBtn = UIButton.create(this, 200, 560, 170, 26, `✨ 정화 -1 (${cleanseCost}G)`, {
                color: canCleanse ? 0x332244 : 0x222222,
                hoverColor: canCleanse ? 0x443355 : 0x222222,
                textColor: canCleanse ? '#bb88ff' : '#555555',
                fontSize: 10,
                onClick: () => {
                    if (!canCleanse) return;
                    this.totalGold -= cleanseCost;
                    this.curseLevel = Math.max(0, this.curseLevel - 1);
                    UIToast.show(this, `저주 정화! Lv.${this.curseLevel}`, { color: '#bb88ff' });
                }
            }).setDepth(86);
            this._cardUIObjects.push(cBtn);

            const eBtn = UIButton.create(this, 400, 560, 170, 26, `🔮 수용 +2Lv (보상↑)`, {
                color: 0x442233, hoverColor: 0x553344,
                textColor: '#ff66cc', fontSize: 10,
                onClick: () => {
                    this.curseLevel += 2;
                    UIToast.show(this, `저주 수용! Lv.${this.curseLevel}`, { color: '#ff66cc' });
                }
            }).setDepth(86);
            this._cardUIObjects.push(eBtn);
        }
    }

    _proceedToNextRound(selectedCardKey) {
        this._rerollCount = 0;
        if (selectedCardKey) {
            this.collectedCards.push(selectedCardKey);
        }

        // Blackout: curse auto-increases each round
        if (this.zoneKey === 'blackout') {
            this.curseLevel++;
        }

        const hpState = {};
        this.party.forEach(merc => {
            if (!merc.alive) return;
            const snapshotHp = this._lastAllyHp && this._lastAllyHp[merc.id] !== undefined
                ? this._lastAllyHp[merc.id] : merc.currentHp;
            const stats = merc.getStats();
            const healAmt = Math.floor(stats.hp * 0.05);
            hpState[merc.id] = Math.min(stats.hp, snapshotHp + healAmt);
        });

        this.scene.restart({
            gameState: this.gameState,
            zoneKey: this.zoneKey,
            party: this.party,
            currentRound: this.currentRound + 1,
            collectedCards: this.collectedCards,
            totalGold: this.totalGold,
            totalXp: this.totalXp,
            loot: this.loot,
            casualties: this.casualties,
            partyHpState: hpState,
            cargoHp: this.cargoHp,
            cargoMaxHp: this.cargoMaxHp,
            cargoDestroyed: this.cargoDestroyed,
            curseLevel: this.curseLevel
        });
    }

    _endRun(success) {
        this._snapshotAllyState();
        const survivors = this.party.filter(m => m.alive);
        const allCasualties = [...this.casualties];

        // Cargo bonus
        let cargoBonus = 0;
        if (this.zoneKey === 'cargo' && !this.cargoDestroyed && success) {
            cargoBonus = Math.floor(50 + this.cargoHp * 2);
            this.totalGold += cargoBonus;
        }

        // Cargo penalty
        if (this.zoneKey === 'cargo' && this.cargoDestroyed) {
            this.totalGold = Math.floor(this.totalGold * 0.6);
        }

        // Blackout curse loot bonus already applied via drop rarity

        const result = {
            success,
            zoneKey: this.zoneKey,
            zoneLevel: this.zoneLevel,
            rounds: this.currentRound,
            goldEarned: this.totalGold,
            xpEarned: this.totalXp,
            loot: this.loot,
            casualties: allCasualties,
            survivors,
            cards: this.collectedCards,
            zoneLevelUp: success,
            cargoBonus: cargoBonus,
            cargoDestroyed: this.cargoDestroyed,
            curseLevel: this.curseLevel
        };

        this.scene.start('RunResultScene', {
            gameState: this.gameState,
            result
        });
    }

    update(time, delta) {
        if (this.battleOver || this.cardSelectActive) return;

        const dt = delta * this.speedMultiplier;
        this.battleTime += dt;
        this.timerText.setText((this.battleTime / 1000).toFixed(1) + '초');

        if (this.killStreakTimer > 0) {
            this.killStreakTimer -= dt;
            if (this.killStreakTimer <= 0) this.killStreak = 0;
        }

        const prevDeadCount = this.enemies.filter(e => !e.alive).length;

        for (const unit of this.allUnits) {
            if (unit.alive) unit.update(dt, this.allUnits);
        }

        const newDeadCount = this.enemies.filter(e => !e.alive).length;

        if (newDeadCount > prevDeadCount) {
            const killsThisFrame = newDeadCount - prevDeadCount;
            this.killStreak += killsThisFrame;
            this.killStreakTimer = 3000;
            this._showKillStreak();
        }

        this._lastAllyHp = {};
        this.allies.forEach(u => {
            if (u.alive && u.mercId) {
                this._lastAllyHp[u.mercId] = u.hp;
            }
        });

        // Zone-specific update
        this._updateCargoMechanics(dt);
        this._updateBlackoutMechanics(dt);

        const critAlly = this.allies.find(a => a.alive && a.hp / a.maxHp <= 0.25);
        if (critAlly && !this._dangerVignette) {
            this._dangerVignette = this.add.rectangle(640, 360, 1280, 720)
                .setStrokeStyle(6, 0xff0000, 0.3).setFillStyle(0x000000, 0).setDepth(200);
            this.tweens.add({ targets: this._dangerVignette, alpha: 0.15, duration: 400, yoyo: true, repeat: -1 });
        } else if (!critAlly && this._dangerVignette) {
            this._dangerVignette.destroy();
            this._dangerVignette = null;
        }
    }

    _showKillStreak() {
        const labels = { 2: 'DOUBLE KILL!', 3: 'TRIPLE KILL!', 4: 'ULTRA KILL!', 5: 'MASSACRE!' };
        const label = this.killStreak >= 5 ? 'MASSACRE!' : labels[this.killStreak];
        if (!label) return;
        const colors = { 2: '#ffcc44', 3: '#ff8844', 4: '#ff4444', 5: '#ff2222' };
        const color = this.killStreak >= 5 ? '#ff2222' : colors[this.killStreak] || '#ffcc44';
        const txt = this.add.text(640, 200, label, {
            fontSize: '28px', fontFamily: 'monospace', color, fontStyle: 'bold',
            stroke: '#000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(300).setScale(0.5);
        this.tweens.add({ targets: txt, scale: 1.2, duration: 200, yoyo: true, hold: 400 });
        this.tweens.add({ targets: txt, alpha: 0, y: 170, duration: 800, delay: 600, onComplete: () => txt.destroy() });
    }

    shutdown() {
        this.allUnits.forEach(u => u.destroy());
        if (this._dangerVignette) { this._dangerVignette.destroy(); this._dangerVignette = null; }
    }
}
