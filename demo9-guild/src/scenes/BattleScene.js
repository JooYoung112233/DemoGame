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

        this._drawBackground();
        this._drawHUD();
        this._spawnAllies();
        this._spawnEnemies();
        this._applyCards();
        this._showRoundAnnounce();
    }

    _drawBackground() {
        const zone = ZONE_DATA[this.zoneKey];
        const gfx = this.add.graphics();
        gfx.fillGradientStyle(0x1a0a0a, 0x1a0a0a, 0x2a1515, 0x2a1515);
        gfx.fillRect(0, 0, 1280, 720);
        gfx.fillStyle(0x221111);
        gfx.fillRect(0, 450, 1280, 270);
        gfx.fillStyle(0x331818);
        gfx.fillRect(0, 448, 1280, 4);
        gfx.fillStyle(zone.color, 0.02);
        gfx.fillRect(0, 0, 1280, 720);
        for (let i = 0; i < 8; i++) {
            gfx.fillStyle(0x330000, 0.3);
            gfx.fillCircle(Phaser.Math.Between(50, 1230), Phaser.Math.Between(460, 700), Phaser.Math.Between(5, 15));
        }
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
    }

    _spawnAllies() {
        const startX = 180;
        const spacing = 70;
        this.party.forEach((merc, idx) => {
            if (!merc.alive) return;
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
        const { composition, scaleMult } = getEnemyComposition(this.currentRound, this.zoneLevel);

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
            const bossUnit = BattleUnit.fromEnemyData(this, 'pitlord', scaleMult * 1.3, 1050, 420);
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
                let goldDrop = Phaser.Math.Between(5, 15) + this.currentRound * 3;
                if (unit.isElite) goldDrop += 20;
                if (unit.isBoss) goldDrop += 100;
                if (this.collectedCards.includes('golden_hands')) goldDrop = Math.floor(goldDrop * 1.2);
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
                if (Math.random() < dropChance) {
                    const item = generateItem(this.zoneKey, this.gameState.guildLevel, luckyBonus + bossRarityBonus);
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

        this.add.text(640, 80, `라운드 ${this.currentRound} 클리어!`, {
            fontSize: '24px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(81);

        this._cardGoldText = this.add.text(640, 115, `생존: ${this.party.filter(m => m.alive).length}/${this.party.length}  |  💰 ${this.totalGold}G  |  💚 HP 5% 회복`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa6666'
        }).setOrigin(0.5).setDepth(81);

        this.add.text(640, 145, '카드를 선택하세요', {
            fontSize: '14px', fontFamily: 'monospace', color: '#cc8888'
        }).setOrigin(0.5).setDepth(81);

        this._drawCardChoices();
    }

    _drawCardChoices() {
        if (this._cardUIObjects) {
            this._cardUIObjects.forEach(obj => obj.destroy());
        }
        this._cardUIObjects = [];

        const cardChoices = generateCardChoices(this.currentRound, this.collectedCards, this.zoneLevel);

        if (cardChoices.length === 0) {
            const t = this.add.text(640, 360, '선택 가능한 카드가 없습니다', {
                fontSize: '14px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5).setDepth(81);
            this._cardUIObjects.push(t);
            this.time.delayedCall(1000, () => this._proceedToNextRound(null));
            return;
        }

        const cardW = 280, cardH = 210;
        const gap = 30;
        const totalW = cardChoices.length * cardW + (cardChoices.length - 1) * gap;
        const startX = 640 - totalW / 2;

        cardChoices.forEach((key, idx) => {
            const card = CARD_DATA[key];
            const cx = startX + idx * (cardW + gap) + cardW / 2;
            const cy = 340;

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

        // skip button
        const skipBg = this.add.rectangle(540, 530, 100, 32, 0x222222).setStrokeStyle(1, 0x444444).setDepth(84).setInteractive({ useHandCursor: true });
        const skipTxt = this.add.text(540, 530, '스킵', {
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

        const rrBg = this.add.rectangle(740, 530, 140, 32, rrColor).setStrokeStyle(1, rrBorder).setDepth(84);
        const rrTxt = this.add.text(740, 530, costLabel, {
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
                this._cardGoldText.setText(`생존: ${this.party.filter(m => m.alive).length}/${this.party.length}  |  💰 ${this.totalGold}G  |  💚 HP 5% 회복`);
                this.goldText.setText(`💰 ${this.totalGold}G`);
                this._drawCardChoices();
            });
        }
    }

    _proceedToNextRound(selectedCardKey) {
        this._rerollCount = 0;
        if (selectedCardKey) {
            this.collectedCards.push(selectedCardKey);
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
            partyHpState: hpState
        });
    }

    _endRun(success) {
        this._snapshotAllyState();
        const survivors = this.party.filter(m => m.alive);
        const allCasualties = [...this.casualties];

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
            zoneLevelUp: success
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
