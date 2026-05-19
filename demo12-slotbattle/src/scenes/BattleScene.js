class BattleScene extends Phaser.Scene {
    constructor() {
        super('BattleScene');
    }

    init(data) {
        this.round = data.round || 1;
        this.playerState = data.playerState || {
            hp: 80, maxHp: 80, block: 0, gold: 10,
            symbolPool: ['sword', 'sword', 'shield', 'shield', 'potion', 'dagger', 'arrow', 'fire', 'coin', 'skull'],
            codex: {},
            comboUpgrades: {}
        };
        if (!this.playerState.codex) this.playerState.codex = {};
        if (!this.playerState.comboUpgrades) this.playerState.comboUpgrades = {};
        for (const sid of this.playerState.symbolPool) {
            this.playerState.codex[sid] = true;
        }
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a1a');

        this.slotMachine = new SlotMachine(this);
        this.slotMachine.setSymbolPool(this.playerState.symbolPool);
        this.combatResolver = new CombatResolver();
        this.enemies = this._spawnEnemies();
        this.turnPhase = 'ready';
        this.combatLog = [];
        this.animatedHp = this.playerState.hp;
        this.animatedBlock = this.playerState.block;

        this._createTopBar();
        this._createEnemyDisplay();
        this._createSlotDisplay();
        this._createPlayerHUD();
        this._createCombatLog();
        this._createSideButtons();
        this._updateAllDisplays();
    }

    _spawnEnemies() {
        const roundData = ROUND_ENEMIES[Math.min(this.round - 1, ROUND_ENEMIES.length - 1)];
        const enemies = [];
        for (let i = 0; i < roundData.count; i++) {
            const id = roundData.pool[Phaser.Math.Between(0, roundData.pool.length - 1)];
            const data = ENEMY_DATA[id];
            enemies.push({
                ...data, hp: data.hp, maxHp: data.hp,
                burn: 0, poison: 0, index: i,
                cooldownTimer: data.cooldown,
                animatedHp: data.hp
            });
        }
        return enemies;
    }

    _createTopBar() {
        const W = 1280;
        const topBg = this.add.graphics();
        topBg.fillStyle(0x111128, 1);
        topBg.fillRect(0, 0, W, 50);

        this.add.text(W / 2, 25, `라운드 ${this.round} / 10`, {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.goldTopLabel = this.add.text(W - 20, 25, '', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc00'
        }).setOrigin(1, 0.5);
    }

    _createEnemyDisplay() {
        const W = 1280;
        this.enemyContainers = [];
        const startX = W / 2 - ((this.enemies.length - 1) * 180) / 2;

        for (let i = 0; i < this.enemies.length; i++) {
            const enemy = this.enemies[i];
            const x = startX + i * 180;
            const y = 160;
            const container = this.add.container(x, y);

            const body = this.add.graphics();
            body.fillStyle(enemy.color, 0.3);
            body.lineStyle(2, enemy.color, 0.8);
            body.fillRoundedRect(-45, -30, 90, 90, 12);
            body.strokeRoundedRect(-45, -30, 90, 90, 12);
            container.add(body);

            const icon = this.add.text(0, 10, enemy.icon, { fontSize: '40px' }).setOrigin(0.5);
            container.add(icon);

            const nameText = this.add.text(0, 70, enemy.name, {
                fontSize: '13px', fontFamily: 'monospace', color: '#cccccc'
            }).setOrigin(0.5);
            container.add(nameText);

            const intentBg = this.add.graphics();
            container.add(intentBg);
            const intentText = this.add.text(0, -50, '', {
                fontSize: '13px', fontFamily: 'monospace', color: '#ff6666', fontStyle: 'bold'
            }).setOrigin(0.5);
            container.add(intentText);

            const hpBg = this.add.graphics();
            hpBg.fillStyle(0x333333, 1);
            hpBg.fillRoundedRect(-45, 90, 90, 14, 4);
            container.add(hpBg);

            const hpBarBehind = this.add.graphics();
            container.add(hpBarBehind);
            const hpBar = this.add.graphics();
            container.add(hpBar);

            const hpText = this.add.text(0, 96, '', {
                fontSize: '11px', fontFamily: 'monospace', color: '#ffffff'
            }).setOrigin(0.5);
            container.add(hpText);

            const statusText = this.add.text(0, 112, '', {
                fontSize: '11px', fontFamily: 'monospace', color: '#ffaa00'
            }).setOrigin(0.5);
            container.add(statusText);

            this.enemyContainers.push({ container, hpBar, hpBarBehind, hpText, body, statusText, icon, intentText, intentBg });
        }
    }

    _createSlotDisplay() {
        const W = 1280, slotY = 390;

        this.comboText = this.add.text(W / 2, 305, '', {
            fontSize: '24px', fontFamily: 'monospace', color: '#ffcc00', fontStyle: 'bold'
        }).setOrigin(0.5).setAlpha(0);

        const slotBg = this.add.graphics();
        slotBg.fillStyle(0x151530, 1);
        slotBg.lineStyle(2, 0x3355aa, 0.8);
        slotBg.fillRoundedRect(W / 2 - 220, slotY - 55, 440, 110, 14);
        slotBg.strokeRoundedRect(W / 2 - 220, slotY - 55, 440, 110, 14);

        this.slotMachine.reelContainers = [];
        for (let i = 0; i < 3; i++) {
            const x = W / 2 - 120 + i * 120;
            const reelBg = this.add.graphics();
            reelBg.fillStyle(0x0a0a1a, 1);
            reelBg.lineStyle(1, 0x334466, 0.5);
            reelBg.fillRoundedRect(x - 50, slotY - 43, 100, 86, 10);
            reelBg.strokeRoundedRect(x - 50, slotY - 43, 100, 86, 10);

            const container = this.add.container(x, slotY);
            container.setData('originY', slotY);
            const iconText = this.add.text(0, -10, '❓', { fontSize: '32px' }).setOrigin(0.5);
            const nameLabel = this.add.text(0, 24, '???', {
                fontSize: '11px', fontFamily: 'monospace', color: '#666666'
            }).setOrigin(0.5);
            container.add(iconText);
            container.add(nameLabel);
            this.slotMachine.reelContainers.push(container);
        }

        this.spinBtn = this.add.text(W / 2, 498, '[ SPIN! ]', {
            fontSize: '28px', fontFamily: 'monospace', color: '#ffcc00',
            fontStyle: 'bold', padding: { x: 28, y: 10 },
            backgroundColor: '#2a2a1a'
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        this.spinBtn.on('pointerover', () => this.spinBtn.setColor('#ffffff'));
        this.spinBtn.on('pointerout', () => this.spinBtn.setColor('#ffcc00'));
        this.spinBtn.on('pointerdown', () => this._onSpin());

        this.tweens.add({
            targets: this.spinBtn, scaleX: 1.03, scaleY: 1.03,
            duration: 600, yoyo: true, repeat: -1, ease: 'Sine.easeInOut'
        });
    }

    _createPlayerHUD() {
        const W = 1280, barY = 560;

        const hudBg = this.add.graphics();
        hudBg.fillStyle(0x111128, 1);
        hudBg.fillRect(0, barY - 10, W, 90);

        this.add.text(20, barY, '❤️ HP', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff6666'
        });

        const hpBarBg = this.add.graphics();
        hpBarBg.fillStyle(0x331111, 1);
        hpBarBg.fillRoundedRect(80, barY, 300, 24, 4);
        this.playerHpBarBehind = this.add.graphics();
        this.playerHpBar = this.add.graphics();
        this.playerHpText = this.add.text(230, barY + 11, '', {
            fontSize: '13px', fontFamily: 'monospace', color: '#ffffff'
        }).setOrigin(0.5);

        this.add.text(20, barY + 34, '🛡️ 방어', {
            fontSize: '14px', fontFamily: 'monospace', color: '#6688ff'
        });
        const blockBarBg = this.add.graphics();
        blockBarBg.fillStyle(0x111133, 1);
        blockBarBg.fillRoundedRect(80, barY + 34, 300, 18, 4);
        this.playerBlockBarGfx = this.add.graphics();
        this.playerBlockText = this.add.text(230, barY + 42, '', {
            fontSize: '11px', fontFamily: 'monospace', color: '#ffffff'
        }).setOrigin(0.5);

        this.goldLabel = this.add.text(420, barY + 6, '', {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffcc00'
        });
        this.deckLabel = this.add.text(420, barY + 34, '', {
            fontSize: '13px', fontFamily: 'monospace', color: '#888888'
        });

        const deckIcons = this.playerState.symbolPool.map(id => {
            const s = SYMBOL_DATA[id];
            return s ? s.icon : '?';
        }).join('');
        this.deckIconsText = this.add.text(560, barY + 34, deckIcons, {
            fontSize: '13px', fontFamily: 'monospace', color: '#666666',
            wordWrap: { width: 700 }
        });
    }

    _createCombatLog() {
        this.logText = this.add.text(20, 660, '', {
            fontSize: '12px', fontFamily: 'monospace', color: '#777777',
            wordWrap: { width: 900 }, lineSpacing: 2
        });
    }

    _createSideButtons() {
        const W = 1280;

        const helpBtn = this.add.text(W - 20, 70, '📖 도움말', {
            fontSize: '14px', fontFamily: 'monospace', color: '#888888',
            backgroundColor: '#1a1a35', padding: { x: 10, y: 6 }
        }).setOrigin(1, 0).setInteractive({ useHandCursor: true });
        helpBtn.on('pointerover', () => helpBtn.setColor('#ffffff'));
        helpBtn.on('pointerout', () => helpBtn.setColor('#888888'));
        helpBtn.on('pointerdown', () => {
            this.scene.launch('HelpScene', { playerState: this.playerState });
            this.scene.pause();
        });

        const codexBtn = this.add.text(W - 20, 105, '📚 도감', {
            fontSize: '14px', fontFamily: 'monospace', color: '#888888',
            backgroundColor: '#1a1a35', padding: { x: 10, y: 6 }
        }).setOrigin(1, 0).setInteractive({ useHandCursor: true });
        codexBtn.on('pointerover', () => codexBtn.setColor('#ffffff'));
        codexBtn.on('pointerout', () => codexBtn.setColor('#888888'));
        codexBtn.on('pointerdown', () => {
            this.scene.launch('CodexScene', { playerState: this.playerState });
            this.scene.pause();
        });
    }

    _onSpin() {
        if (this.turnPhase !== 'ready') return;
        this.turnPhase = 'spinning';
        this.spinBtn.setAlpha(0.3);
        this.comboText.setAlpha(0);

        this.slotMachine.spin((results) => {
            this.turnPhase = 'resolving';
            this._resolveSlotResults(results);
        });
    }

    _resolveSlotResults(results) {
        const comboUpgrades = this.playerState.comboUpgrades || {};
        const { log, actions, hasCombo } = this.combatResolver.resolve(
            results, this.playerState, this.enemies, this.slotMachine, comboUpgrades
        );

        if (hasCombo) {
            const comboAction = actions.find(a => a.type === 'combo');
            this.comboText.setText(`✨ ${comboAction.name}! ✨`);
            this.comboText.setAlpha(1);
            this.tweens.add({
                targets: this.comboText, scaleX: 1.3, scaleY: 1.3,
                duration: 200, yoyo: true,
            });
            this.cameras.main.shake(200, 0.008);
        }

        this._showDamagePopups(log);
        this._animateDisplayChanges();
        this._appendLog(log);

        if (this.enemies.every(e => e.hp <= 0)) {
            this.time.delayedCall(800, () => this._onRoundWin());
            return;
        }
        if (this.playerState.hp <= 0) {
            this.time.delayedCall(800, () => this._onPlayerDeath());
            return;
        }

        const attackers = this.combatResolver.tickEnemyCooldowns(this.enemies);
        this._updateIntentDisplays();

        if (attackers.length > 0) {
            this.time.delayedCall(600, () => this._enemyTurn(attackers));
        } else {
            this._appendLog([{ type: 'info', text: '적이 공격을 준비하는 중...' }]);
            this.time.delayedCall(400, () => {
                this.turnPhase = 'ready';
                this.spinBtn.setAlpha(1);
            });
        }
    }

    _enemyTurn(attackers) {
        this.turnPhase = 'enemyTurn';

        const attackerNames = attackers.map(e => e.name).join(', ');
        this._appendLog([{ type: 'info', text: `⚔️ ${attackerNames}의 공격!` }]);

        const log = this.combatResolver.enemyAttack(attackers, this.playerState);
        this._showPlayerHitEffect(log);
        this._animateDisplayChanges();
        this._appendLog(log);

        if (this.enemies.every(e => e.hp <= 0)) {
            this.time.delayedCall(600, () => this._onRoundWin());
            return;
        }
        if (this.playerState.hp <= 0) {
            this.time.delayedCall(600, () => this._onPlayerDeath());
            return;
        }
        this.time.delayedCall(400, () => {
            this.turnPhase = 'ready';
            this.spinBtn.setAlpha(1);
        });
    }

    _showDamagePopups(log) {
        for (const entry of log) {
            if (entry.type === 'damage' || entry.type === 'dot') {
                const idx = entry.targetIdx !== undefined ? entry.targetIdx : this.enemies.findIndex(e => e.name === entry.target);
                if (idx >= 0 && this.enemyContainers[idx]) {
                    const ec = this.enemyContainers[idx];
                    this._floatingText(ec.container.x, ec.container.y - 50, `-${entry.value}`, '#ff4444');
                    this.tweens.add({
                        targets: ec.container, x: ec.container.x + 10,
                        duration: 50, yoyo: true, repeat: 2
                    });
                    this._flashSprite(ec.body, 0xff0000);
                }
            }
            if (entry.type === 'heal') this._floatingText(200, 540, `+${entry.value} HP`, '#44ff88');
            if (entry.type === 'gold') this._floatingText(450, 540, `+${entry.value} G`, '#ffcc00');
            if (entry.type === 'block') this._floatingText(200, 525, `+${entry.value} 🛡️`, '#4488ff');
            if (entry.type === 'slow') this._floatingText(200, 510, `❄️ 둔화!`, '#88ccff');
        }
    }

    _showPlayerHitEffect(log) {
        for (const entry of log) {
            if (entry.type === 'playerHit') {
                this._floatingText(200, 545, `💥 -${entry.value}`, '#ff2222');
                this.cameras.main.shake(150, 0.008);
                this.cameras.main.flash(150, 255, 50, 50, false, null, null, 0.3);
            }
            if (entry.type === 'blocked') {
                this._floatingText(200, 525, `🛡️ 방어 ${entry.value}`, '#4488ff');
            }
            if (entry.type === 'dot') {
                const idx = entry.targetIdx !== undefined ? entry.targetIdx : this.enemies.findIndex(e => e.name === entry.target);
                if (idx >= 0 && this.enemyContainers[idx]) {
                    const ec = this.enemyContainers[idx];
                    this._floatingText(ec.container.x, ec.container.y - 50, `-${entry.value}`, entry.dotType === 'burn' ? '#ff8800' : '#88ff00');
                }
            }
        }
    }

    _flashSprite(gfx, color) {
        const origAlpha = gfx.alpha;
        this.tweens.add({
            targets: gfx, alpha: 0.2,
            duration: 80, yoyo: true, repeat: 1,
            onComplete: () => gfx.setAlpha(origAlpha)
        });
    }

    _floatingText(x, y, text, color) {
        const t = this.add.text(x, y, text, {
            fontSize: '20px', fontFamily: 'monospace', color: color, fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(100);
        this.tweens.add({
            targets: t, y: y - 40, alpha: 0,
            duration: 900, ease: 'Power2',
            onComplete: () => t.destroy()
        });
    }

    _animateDisplayChanges() {
        const ps = this.playerState;

        // Animate player HP
        this.tweens.addCounter({
            from: this.animatedHp,
            to: ps.hp,
            duration: 400,
            ease: 'Power2',
            onUpdate: (tween) => {
                this.animatedHp = Math.round(tween.getValue());
                this._drawPlayerBars();
            }
        });

        // Animate player block
        this.tweens.addCounter({
            from: this.animatedBlock,
            to: ps.block,
            duration: 300,
            onUpdate: (tween) => {
                this.animatedBlock = Math.round(tween.getValue());
                this._drawPlayerBars();
            }
        });

        // Animate enemy HP bars
        for (let i = 0; i < this.enemies.length; i++) {
            const enemy = this.enemies[i];
            const ec = this.enemyContainers[i];
            if (!ec) continue;

            const fromHp = enemy.animatedHp;
            const toHp = enemy.hp;
            this.tweens.addCounter({
                from: fromHp,
                to: toHp,
                duration: 400,
                ease: 'Power2',
                onUpdate: (tween) => {
                    enemy.animatedHp = Math.round(tween.getValue());
                    this._drawEnemyBar(i);
                },
                onComplete: () => {
                    enemy.animatedHp = enemy.hp;
                    if (enemy.hp <= 0) {
                        this.tweens.add({
                            targets: ec.container, alpha: 0.2, scaleX: 0.8, scaleY: 0.8,
                            duration: 300
                        });
                    }
                }
            });
        }

        this._updateStaticDisplays();
    }

    _drawPlayerBars() {
        const ps = this.playerState;
        const barY = 560;

        this.playerHpBarBehind.clear();
        const behindRatio = Math.max(0, this.animatedHp / ps.maxHp);
        this.playerHpBarBehind.fillStyle(0x882222, 1);
        this.playerHpBarBehind.fillRoundedRect(80, barY, 300 * Math.max(behindRatio, Math.max(0, ps.hp / ps.maxHp)), 24, 4);

        this.playerHpBar.clear();
        const hpRatio = Math.max(0, this.animatedHp / ps.maxHp);
        const hpColor = hpRatio > 0.5 ? 0x44cc44 : hpRatio > 0.25 ? 0xcccc44 : 0xcc4444;
        this.playerHpBar.fillStyle(hpColor, 1);
        this.playerHpBar.fillRoundedRect(80, barY, 300 * hpRatio, 24, 4);
        this.playerHpText.setText(`${Math.max(0, this.animatedHp)} / ${ps.maxHp}`);

        this.playerBlockBarGfx.clear();
        if (this.animatedBlock > 0) {
            const blockRatio = Math.min(1, this.animatedBlock / ps.maxHp);
            this.playerBlockBarGfx.fillStyle(0x4488ff, 1);
            this.playerBlockBarGfx.fillRoundedRect(80, barY + 34, 300 * blockRatio, 18, 4);
        }
        this.playerBlockText.setText(this.animatedBlock > 0 ? `${this.animatedBlock}` : '');
    }

    _drawEnemyBar(i) {
        const enemy = this.enemies[i];
        const ec = this.enemyContainers[i];
        if (!ec) return;

        ec.hpBarBehind.clear();
        const behindRatio = Math.max(0, enemy.animatedHp / enemy.maxHp);
        ec.hpBarBehind.fillStyle(0x882222, 1);
        ec.hpBarBehind.fillRoundedRect(-45, 90, 90 * Math.max(behindRatio, Math.max(0, enemy.hp / enemy.maxHp)), 14, 4);

        ec.hpBar.clear();
        const ratio = Math.max(0, enemy.animatedHp / enemy.maxHp);
        const barColor = ratio > 0.5 ? 0x44cc44 : ratio > 0.25 ? 0xcccc44 : 0xcc4444;
        ec.hpBar.fillStyle(barColor, 1);
        ec.hpBar.fillRoundedRect(-45, 90, 90 * ratio, 14, 4);
        ec.hpText.setText(`${Math.max(0, Math.round(enemy.animatedHp))}/${enemy.maxHp}`);
    }

    _updateIntentDisplays() {
        for (let i = 0; i < this.enemies.length; i++) {
            const enemy = this.enemies[i];
            const ec = this.enemyContainers[i];
            if (!ec || enemy.hp <= 0) {
                if (ec) ec.intentText.setText('');
                continue;
            }

            if (enemy.cooldownTimer <= 0) {
                ec.intentText.setText(`⚔️ ${enemy.attack}`);
                ec.intentText.setColor('#ff4444');
            } else {
                ec.intentText.setText(`${enemy.cooldownTimer}턴 후 ⚔️${enemy.attack}`);
                ec.intentText.setColor(enemy.cooldownTimer === 1 ? '#ff8844' : '#888888');
            }
        }
    }

    _updateStaticDisplays() {
        const ps = this.playerState;

        this.goldLabel.setText(`🪙 ${ps.gold}`);
        this.goldTopLabel.setText(`🪙 ${ps.gold}`);
        this.deckLabel.setText(`덱 ${ps.symbolPool.length}장:`);

        const deckIcons = ps.symbolPool.map(id => {
            const s = SYMBOL_DATA[id];
            return s ? s.icon : '?';
        }).join('');
        this.deckIconsText.setText(deckIcons);

        for (let i = 0; i < this.enemies.length; i++) {
            const enemy = this.enemies[i];
            const ec = this.enemyContainers[i];
            if (!ec) continue;

            const statuses = [];
            if (enemy.burn > 0) statuses.push(`🔥${enemy.burn}`);
            if (enemy.poison > 0) statuses.push(`☠️${enemy.poison}`);
            ec.statusText.setText(statuses.join(' '));
        }

        this._updateIntentDisplays();
    }

    _updateAllDisplays() {
        this.animatedHp = this.playerState.hp;
        this.animatedBlock = this.playerState.block;
        this._drawPlayerBars();
        this._updateStaticDisplays();

        for (let i = 0; i < this.enemies.length; i++) {
            this.enemies[i].animatedHp = this.enemies[i].hp;
            this._drawEnemyBar(i);
        }
    }

    _appendLog(log) {
        const lines = [];
        for (const entry of log) {
            if (entry.type === 'info') lines.push(entry.text);
            if (entry.type === 'damage') lines.push(`⚔️ ${entry.target}에게 ${entry.value} 데미지`);
            if (entry.type === 'heal') lines.push(`💚 HP ${entry.value} 회복`);
            if (entry.type === 'block') lines.push(`🛡️ 방어 ${entry.value} 획득`);
            if (entry.type === 'gold') lines.push(`🪙 골드 ${entry.value} 획득`);
            if (entry.type === 'selfDamage') lines.push(`💀 저주! ${entry.value} 자해`);
            if (entry.type === 'playerHit') lines.push(`💥 ${entry.from}의 공격! ${entry.value} 피해!`);
            if (entry.type === 'blocked') lines.push(`🛡️ ${entry.from}의 공격 ${entry.value} 방어!`);
            if (entry.type === 'dot') lines.push(`${entry.dotType === 'burn' ? '🔥' : '☠️'} ${entry.target} ${entry.dotType} ${entry.value}`);
            if (entry.type === 'burn') lines.push(`🔥 ${entry.target} 화상 ${entry.value}`);
            if (entry.type === 'poison') lines.push(`☠️ ${entry.target} 독 ${entry.value}`);
            if (entry.type === 'slow') lines.push(`❄️ ${entry.target} 둔화!`);
        }
        this.combatLog = this.combatLog.concat(lines);
        if (this.combatLog.length > 6) this.combatLog = this.combatLog.slice(-6);
        this.logText.setText(this.combatLog.join('\n'));
    }

    _onRoundWin() {
        const goldReward = 5 + this.round * 2;
        this.playerState.gold += goldReward;
        this.scene.start('ShopScene', {
            round: this.round,
            playerState: this.playerState,
            goldReward: goldReward
        });
    }

    _onPlayerDeath() {
        this.scene.start('GameOverScene', {
            round: this.round,
            playerState: this.playerState
        });
    }
}
