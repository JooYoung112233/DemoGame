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
                burn: 0, poison: 0, index: i
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

            const icon = this.add.text(0, 10, enemy.icon, {
                fontSize: '40px'
            }).setOrigin(0.5);
            container.add(icon);

            const nameText = this.add.text(0, 70, enemy.name, {
                fontSize: '13px', fontFamily: 'monospace', color: '#cccccc'
            }).setOrigin(0.5);
            container.add(nameText);

            const atkText = this.add.text(0, 86, `⚔️${enemy.attack}  🛡️${enemy.defense}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#ff8888'
            }).setOrigin(0.5);
            container.add(atkText);

            const hpBg = this.add.graphics();
            hpBg.fillStyle(0x333333, 1);
            hpBg.fillRoundedRect(-45, 100, 90, 12, 4);
            container.add(hpBg);

            const hpBar = this.add.graphics();
            container.add(hpBar);

            const hpText = this.add.text(0, 105, '', {
                fontSize: '11px', fontFamily: 'monospace', color: '#ffffff'
            }).setOrigin(0.5);
            container.add(hpText);

            const statusText = this.add.text(0, -42, '', {
                fontSize: '12px', fontFamily: 'monospace', color: '#ffaa00'
            }).setOrigin(0.5);
            container.add(statusText);

            this.enemyContainers.push({ container, hpBar, hpText, body, statusText, icon });
        }
    }

    _createSlotDisplay() {
        const W = 1280, slotY = 400;

        this.comboText = this.add.text(W / 2, 310, '', {
            fontSize: '24px', fontFamily: 'monospace', color: '#ffcc00', fontStyle: 'bold'
        }).setOrigin(0.5).setAlpha(0);

        const slotBg = this.add.graphics();
        slotBg.fillStyle(0x151530, 1);
        slotBg.lineStyle(2, 0x3355aa, 0.8);
        slotBg.fillRoundedRect(W / 2 - 220, slotY - 50, 440, 100, 14);
        slotBg.strokeRoundedRect(W / 2 - 220, slotY - 50, 440, 100, 14);

        this.slotMachine.reelContainers = [];
        for (let i = 0; i < 3; i++) {
            const x = W / 2 - 120 + i * 120;
            const reelBg = this.add.graphics();
            reelBg.fillStyle(0x0a0a1a, 1);
            reelBg.lineStyle(1, 0x334466, 0.5);
            reelBg.fillRoundedRect(x - 45, slotY - 38, 90, 76, 8);
            reelBg.strokeRoundedRect(x - 45, slotY - 38, 90, 76, 8);

            const container = this.add.container(x, slotY);
            const iconText = this.add.text(0, -8, '❓', { fontSize: '30px' }).setOrigin(0.5);
            const nameLabel = this.add.text(0, 22, '???', {
                fontSize: '11px', fontFamily: 'monospace', color: '#666666'
            }).setOrigin(0.5);
            container.add(iconText);
            container.add(nameLabel);
            this.slotMachine.reelContainers.push(container);
        }

        this.spinBtn = this.add.text(W / 2, 500, '[ SPIN! ]', {
            fontSize: '26px', fontFamily: 'monospace', color: '#ffcc00',
            fontStyle: 'bold', padding: { x: 24, y: 8 },
            backgroundColor: '#2a2a1a'
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        this.spinBtn.on('pointerover', () => this.spinBtn.setColor('#ffffff'));
        this.spinBtn.on('pointerout', () => this.spinBtn.setColor('#ffcc00'));
        this.spinBtn.on('pointerdown', () => this._onSpin());
    }

    _createPlayerHUD() {
        const W = 1280, barY = 570;

        const hudBg = this.add.graphics();
        hudBg.fillStyle(0x111128, 1);
        hudBg.fillRect(0, barY - 10, W, 80);

        this.add.text(20, barY, '❤️ HP', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff6666'
        });

        const hpBarBg = this.add.graphics();
        hpBarBg.fillStyle(0x331111, 1);
        hpBarBg.fillRoundedRect(80, barY, 300, 22, 4);
        this.playerHpBar = this.add.graphics();
        this.playerBlockBar = this.add.graphics();
        this.playerHpText = this.add.text(230, barY + 10, '', {
            fontSize: '13px', fontFamily: 'monospace', color: '#ffffff'
        }).setOrigin(0.5);

        this.add.text(20, barY + 32, '🛡️ 방어', {
            fontSize: '14px', fontFamily: 'monospace', color: '#6688ff'
        });
        const blockBarBg = this.add.graphics();
        blockBarBg.fillStyle(0x111133, 1);
        blockBarBg.fillRoundedRect(80, barY + 32, 300, 16, 4);
        this.playerBlockBarGfx = this.add.graphics();
        this.playerBlockText = this.add.text(230, barY + 39, '', {
            fontSize: '11px', fontFamily: 'monospace', color: '#ffffff'
        }).setOrigin(0.5);

        this.goldLabel = this.add.text(420, barY + 6, '', {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffcc00'
        });
        this.deckLabel = this.add.text(420, barY + 32, '', {
            fontSize: '13px', fontFamily: 'monospace', color: '#888888'
        });

        const deckIcons = this.playerState.symbolPool.map(id => {
            const s = SYMBOL_DATA[id];
            return s ? s.icon : '?';
        }).join('');
        this.deckIconsText = this.add.text(560, barY + 32, deckIcons, {
            fontSize: '13px', fontFamily: 'monospace', color: '#666666',
            wordWrap: { width: 700 }
        });
    }

    _createCombatLog() {
        this.logText = this.add.text(20, 660, '', {
            fontSize: '12px', fontFamily: 'monospace', color: '#777777',
            wordWrap: { width: 600 }, lineSpacing: 2
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
        this._updateAllDisplays();
        this._appendLog(log);

        if (this.enemies.every(e => e.hp <= 0)) {
            this.time.delayedCall(800, () => this._onRoundWin());
            return;
        }
        if (this.playerState.hp <= 0) {
            this.time.delayedCall(800, () => this._onPlayerDeath());
            return;
        }
        this.time.delayedCall(600, () => this._enemyTurn());
    }

    _enemyTurn() {
        this.turnPhase = 'enemyTurn';
        const log = this.combatResolver.enemyAttack(this.enemies, this.playerState);
        this._showPlayerHitEffect(log);
        this._updateAllDisplays();
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
                const enemy = this.enemies.find(e => e.name === entry.target);
                if (enemy && this.enemyContainers[enemy.index]) {
                    const ec = this.enemyContainers[enemy.index];
                    this._floatingText(ec.container.x, ec.container.y - 40, `-${entry.value}`, '#ff4444');
                    this.tweens.add({
                        targets: ec.container, x: ec.container.x + 8,
                        duration: 50, yoyo: true, repeat: 2
                    });
                }
            }
            if (entry.type === 'heal') this._floatingText(200, 550, `+${entry.value} HP`, '#44ff88');
            if (entry.type === 'gold') this._floatingText(450, 550, `+${entry.value} G`, '#ffcc00');
            if (entry.type === 'block') this._floatingText(200, 535, `+${entry.value} 🛡️`, '#4488ff');
        }
    }

    _showPlayerHitEffect(log) {
        for (const entry of log) {
            if (entry.type === 'playerHit') {
                this._floatingText(200, 555, `-${entry.value}`, '#ff2222');
                this.cameras.main.shake(100, 0.005);
            }
            if (entry.type === 'blocked') {
                this._floatingText(200, 535, `방어 ${entry.value}`, '#4488ff');
            }
        }
    }

    _floatingText(x, y, text, color) {
        const t = this.add.text(x, y, text, {
            fontSize: '18px', fontFamily: 'monospace', color: color, fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(100);
        this.tweens.add({
            targets: t, y: y - 35, alpha: 0,
            duration: 800, ease: 'Power2',
            onComplete: () => t.destroy()
        });
    }

    _appendLog(log) {
        const lines = [];
        for (const entry of log) {
            if (entry.type === 'damage') lines.push(`${entry.target}에게 ${entry.value} 데미지`);
            if (entry.type === 'heal') lines.push(`HP ${entry.value} 회복`);
            if (entry.type === 'block') lines.push(`방어 ${entry.value} 획득`);
            if (entry.type === 'gold') lines.push(`골드 ${entry.value} 획득`);
            if (entry.type === 'selfDamage') lines.push(`저주! 자해 ${entry.value}`);
            if (entry.type === 'playerHit') lines.push(`${entry.from}의 공격! ${entry.value} 피해`);
            if (entry.type === 'blocked') lines.push(`${entry.from}의 공격 ${entry.value} 방어`);
            if (entry.type === 'dot') lines.push(`${entry.target} ${entry.dotType} ${entry.value}`);
            if (entry.type === 'burn') lines.push(`${entry.target} 화상 ${entry.value}`);
            if (entry.type === 'poison') lines.push(`${entry.target} 독 ${entry.value}`);
        }
        this.combatLog = this.combatLog.concat(lines);
        if (this.combatLog.length > 6) this.combatLog = this.combatLog.slice(-6);
        this.logText.setText(this.combatLog.join('\n'));
    }

    _updateAllDisplays() {
        const ps = this.playerState;

        // Player HP bar
        this.playerHpBar.clear();
        const hpRatio = Math.max(0, ps.hp / ps.maxHp);
        const hpColor = hpRatio > 0.5 ? 0x44cc44 : hpRatio > 0.25 ? 0xcccc44 : 0xcc4444;
        this.playerHpBar.fillStyle(hpColor, 1);
        this.playerHpBar.fillRoundedRect(80, 570, 300 * hpRatio, 22, 4);
        this.playerHpText.setText(`${ps.hp} / ${ps.maxHp}`);

        // Block bar
        this.playerBlockBarGfx.clear();
        if (ps.block > 0) {
            const blockRatio = Math.min(1, ps.block / ps.maxHp);
            this.playerBlockBarGfx.fillStyle(0x4488ff, 1);
            this.playerBlockBarGfx.fillRoundedRect(80, 602, 300 * blockRatio, 16, 4);
        }
        this.playerBlockText.setText(ps.block > 0 ? `${ps.block}` : '');

        this.goldLabel.setText(`🪙 ${ps.gold}`);
        this.goldTopLabel.setText(`🪙 ${ps.gold}`);
        this.deckLabel.setText(`덱 ${ps.symbolPool.length}장:`);

        const deckIcons = ps.symbolPool.map(id => {
            const s = SYMBOL_DATA[id];
            return s ? s.icon : '?';
        }).join('');
        this.deckIconsText.setText(deckIcons);

        // Enemy displays
        for (let i = 0; i < this.enemies.length; i++) {
            const enemy = this.enemies[i];
            const ec = this.enemyContainers[i];
            if (!ec) continue;

            ec.hpBar.clear();
            const ratio = Math.max(0, enemy.hp / enemy.maxHp);
            const barColor = ratio > 0.5 ? 0x44cc44 : ratio > 0.25 ? 0xcccc44 : 0xcc4444;
            ec.hpBar.fillStyle(barColor, 1);
            ec.hpBar.fillRoundedRect(-45, 100, 90 * ratio, 12, 4);
            ec.hpText.setText(`${Math.max(0, enemy.hp)}/${enemy.maxHp}`);

            const statuses = [];
            if (enemy.burn > 0) statuses.push(`🔥${enemy.burn}`);
            if (enemy.poison > 0) statuses.push(`☠️${enemy.poison}`);
            ec.statusText.setText(statuses.join(' '));

            if (enemy.hp <= 0) ec.container.setAlpha(0.3);
        }
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
