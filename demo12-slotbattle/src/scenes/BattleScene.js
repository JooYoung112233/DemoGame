class BattleScene extends Phaser.Scene {
    constructor() {
        super('BattleScene');
    }

    init(data) {
        this.round = data.round || 1;
        this.act = data.act != null ? data.act : 0;
        this.map = data.map || null;
        this.encounter = data.encounter || null;
        this.encounterType = data.encounterType || 'battle';
        this.playerState = data.playerState || {
            hp: 80, maxHp: 80, block: 0, gold: 10,
            symbolPool: ['sword', 'sword', 'shield', 'shield', 'potion', 'dagger', 'arrow', 'fire', 'coin', 'skull'],
            codex: {}, comboUpgrades: {}, relics: [], visitedNodes: [], currentFloor: 0
        };
        if (!this.playerState.codex) this.playerState.codex = {};
        if (!this.playerState.comboUpgrades) this.playerState.comboUpgrades = {};
        if (!this.playerState.relics) this.playerState.relics = [];
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
        this.combatLogScrollY = 0;
        this.autoScrollLog = true;

        // Respin: base 1/round + relic bonus
        this.respinMax = 1;
        if (this.playerState.relics && this.playerState.relics.includes('respin_charm')) {
            this.respinMax += 1;
        }
        this.respinLeft = this.respinMax;

        // P2: Event round — ~17% chance on normal battles (avg every 6 rounds)
        this._isEventRound = false;
        if (this.encounterType === 'battle' && Math.random() < 0.17) {
            this._isEventRound = true;
        }

        this._createTopBar();
        this._createEnemyDisplay();
        this._createSlotDisplay();
        this._createPlayerHUD();
        this._createCombatLog();
        this._createSideButtons();
        this._updateAllDisplays();

        // show event banner if event round
        if (this._isEventRound) {
            this._showEventBanner();
        }
    }

    _spawnEnemies() {
        let pool, count;
        if (this.encounter) {
            pool = this.encounter.pool;
            count = this.encounter.count;
        } else {
            const roundData = ROUND_ENEMIES[Math.min(this.round - 1, ROUND_ENEMIES.length - 1)];
            pool = roundData.pool;
            count = roundData.count;
        }

        const enemies = [];
        for (let i = 0; i < count; i++) {
            const id = pool[i < pool.length ? i : Phaser.Math.Between(0, pool.length - 1)];
            const data = ENEMY_DATA[id];
            if (!data) continue;
            const enemy = {
                ...data, hp: data.hp, maxHp: data.hp,
                burn: 0, poison: 0, block: 0, index: i,
                cooldownTimer: data.cooldown,
                nextIntent: null
            };
            // relic: ice_crystal — enemies start slowed
            if (this.playerState.relics.includes('ice_crystal')) {
                enemy.cooldownTimer += 1;
            }
            enemy.nextIntent = this.combatResolver.rollEnemyIntent(enemy);
            enemies.push(enemy);
        }

        // relic: battle_drum — start with block
        if (this.playerState.relics.includes('battle_drum')) {
            this.playerState.block += 5;
        }

        return enemies;
    }

    _createTopBar() {
        const W = 1280;
        const topBg = this.add.graphics();
        topBg.fillStyle(0x111128, 1);
        topBg.fillRect(0, 0, W, 50);

        const encounterLabel = this.encounterType === 'boss' ? '⚠️ BOSS' :
                               this.encounterType === 'elite' ? '💀 강적' : '⚔️ 전투';
        const eventTag = this._isEventRound ? '  ✨이벤트' : '';
        const actLabel = this.map ? `Act ${this.act + 1}` : `라운드 ${this.round}/10`;
        this.add.text(W / 2, 25, `${actLabel}  ${encounterLabel}${eventTag}`, {
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
            container.setData('baseX', x);

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

            const intentText = this.add.text(0, -50, '', {
                fontSize: '14px', fontFamily: 'monospace', color: '#ff6666', fontStyle: 'bold'
            }).setOrigin(0.5);
            container.add(intentText);

            const hpBg = this.add.graphics();
            hpBg.fillStyle(0x333333, 1);
            hpBg.fillRoundedRect(-45, 90, 90, 14, 4);
            container.add(hpBg);

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

            this.enemyContainers.push({ container, hpBar, hpText, body, statusText, icon, intentText });
        }
    }

    _createSlotDisplay() {
        const W = 1280, slotY = 390;

        this.comboText = this.add.text(W / 2, 305, '', {
            fontSize: '28px', fontFamily: 'monospace', color: '#ffcc00', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5).setAlpha(0);

        const slotBg = this.add.graphics();
        slotBg.fillStyle(0x151530, 1);
        slotBg.lineStyle(2, 0x3355aa, 0.8);
        slotBg.fillRoundedRect(W / 2 - 220, slotY - 55, 440, 110, 14);
        slotBg.strokeRoundedRect(W / 2 - 220, slotY - 55, 440, 110, 14);

        this.slotMachine.reelContainers = [];
        for (let i = 0; i < 3; i++) {
            const x = W / 2 - 120 + i * 120;
            this.add.graphics()
                .fillStyle(0x0a0a1a, 1).lineStyle(1, 0x334466, 0.5)
                .fillRoundedRect(x - 50, slotY - 43, 100, 86, 10)
                .strokeRoundedRect(x - 50, slotY - 43, 100, 86, 10);

            const container = this.add.container(x, slotY);
            const iconText = this.add.text(0, -10, '❓', { fontSize: '32px' }).setOrigin(0.5);
            const nameLabel = this.add.text(0, 24, '', {
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

        this.spinBtn.on('pointerover', () => { if (this.turnPhase === 'ready') this.spinBtn.setColor('#ffffff'); });
        this.spinBtn.on('pointerout', () => this.spinBtn.setColor('#ffcc00'));
        this.spinBtn.on('pointerdown', () => this._onSpin());

        this.tweens.add({
            targets: this.spinBtn, scaleX: 1.04, scaleY: 1.04,
            duration: 500, yoyo: true, repeat: -1, ease: 'Sine.easeInOut'
        });
    }

    _createPlayerHUD() {
        const W = 1280, barY = 560;

        this.add.graphics().fillStyle(0x111128, 1).fillRect(0, barY - 10, W, 90);

        this.add.text(20, barY, '❤️ HP', { fontSize: '14px', fontFamily: 'monospace', color: '#ff6666' });
        this.add.graphics().fillStyle(0x331111, 1).fillRoundedRect(80, barY, 300, 24, 4);
        this.playerHpBar = this.add.graphics();
        this.playerHpText = this.add.text(230, barY + 11, '', {
            fontSize: '13px', fontFamily: 'monospace', color: '#ffffff'
        }).setOrigin(0.5);

        this.add.text(20, barY + 34, '🛡️ 방어', { fontSize: '14px', fontFamily: 'monospace', color: '#6688ff' });
        this.add.graphics().fillStyle(0x111133, 1).fillRoundedRect(80, barY + 34, 300, 18, 4);
        this.playerBlockBarGfx = this.add.graphics();
        this.playerBlockText = this.add.text(230, barY + 42, '', {
            fontSize: '11px', fontFamily: 'monospace', color: '#ffffff'
        }).setOrigin(0.5);

        this.goldLabel = this.add.text(420, barY + 6, '', { fontSize: '18px', fontFamily: 'monospace', color: '#ffcc00' });
        this.respinLabel = this.add.text(600, barY + 6, '', { fontSize: '16px', fontFamily: 'monospace', color: '#44ccff' });
        this.deckLabel = this.add.text(420, barY + 34, '', { fontSize: '13px', fontFamily: 'monospace', color: '#888888' });
        this.deckIconsText = this.add.text(560, barY + 34, '', {
            fontSize: '13px', fontFamily: 'monospace', color: '#666666', wordWrap: { width: 700 }
        });
    }

    _createCombatLog() {
        const logY = 650, logH = 65, logW = 900;
        // mask for scrollable log area
        const maskShape = this.make.graphics();
        maskShape.fillRect(20, logY, logW, logH);
        const mask = maskShape.createGeometryMask();

        this.logContainer = this.add.container(0, 0).setMask(mask);
        this.logText = this.add.text(20, logY, '', {
            fontSize: '12px', fontFamily: 'monospace', color: '#777777',
            wordWrap: { width: logW }, lineSpacing: 2
        });
        this.logContainer.add(this.logText);

        // scroll log area with mouse wheel
        this.input.on('wheel', (pointer, gameObjects, deltaX, deltaY) => {
            if (pointer.y >= logY && pointer.y <= logY + logH) {
                this.autoScrollLog = false;
                this.combatLogScrollY -= deltaY * 0.5;
                this._clampLogScroll();
            }
        });

        // "latest" button (hidden until manual scroll)
        this.logLatestBtn = this.add.text(logW - 40, logY + logH - 14, '▼ 최신', {
            fontSize: '10px', fontFamily: 'monospace', color: '#44ccff',
            backgroundColor: '#1a1a35', padding: { x: 4, y: 2 }
        }).setInteractive({ useHandCursor: true }).setDepth(200).setAlpha(0);
        this.logLatestBtn.on('pointerdown', () => {
            this.autoScrollLog = true;
            this._scrollLogToBottom();
        });
    }

    _clampLogScroll() {
        const logY = 650, logH = 65;
        const textHeight = this.logText.height || 0;
        const maxScroll = 0;
        const minScroll = Math.min(0, -(textHeight - logH));
        this.combatLogScrollY = Phaser.Math.Clamp(this.combatLogScrollY, minScroll, maxScroll);
        this.logText.y = logY + this.combatLogScrollY;
        // show/hide latest button
        if (this.logLatestBtn) {
            this.logLatestBtn.setAlpha(this.combatLogScrollY < minScroll + 5 ? 0 : 1);
        }
    }

    _scrollLogToBottom() {
        const logY = 650, logH = 65;
        const textHeight = this.logText.height || 0;
        this.combatLogScrollY = Math.min(0, -(textHeight - logH));
        this.logText.y = logY + this.combatLogScrollY;
        if (this.logLatestBtn) this.logLatestBtn.setAlpha(0);
    }

    _createSideButtons() {
        const W = 1280;
        const helpBtn = this.add.text(W - 20, 70, '📖 도움말', {
            fontSize: '14px', fontFamily: 'monospace', color: '#888888',
            backgroundColor: '#1a1a35', padding: { x: 10, y: 6 }
        }).setOrigin(1, 0).setInteractive({ useHandCursor: true });
        helpBtn.on('pointerover', () => helpBtn.setColor('#ffffff'));
        helpBtn.on('pointerout', () => helpBtn.setColor('#888888'));
        helpBtn.on('pointerdown', () => { this.scene.launch('HelpScene', { playerState: this.playerState }); this.scene.pause(); });

        const codexBtn = this.add.text(W - 20, 105, '📚 도감', {
            fontSize: '14px', fontFamily: 'monospace', color: '#888888',
            backgroundColor: '#1a1a35', padding: { x: 10, y: 6 }
        }).setOrigin(1, 0).setInteractive({ useHandCursor: true });
        codexBtn.on('pointerover', () => codexBtn.setColor('#ffffff'));
        codexBtn.on('pointerout', () => codexBtn.setColor('#888888'));
        codexBtn.on('pointerdown', () => { this.scene.launch('CodexScene', { playerState: this.playerState }); this.scene.pause(); });
    }

    // ── TURN FLOW ──────────────────────────────────

    _onSpin() {
        if (this.turnPhase !== 'ready') return;
        this.turnPhase = 'spinning';
        this.spinBtn.setAlpha(0.3);
        this.comboText.setAlpha(0);

        this.slotMachine.spin((results) => {
            // always offer respin/confirm if respins remain
            if (this.respinLeft > 0) {
                this.turnPhase = 'awaitRespin';
                this._showRespinChoice(results);
            } else {
                this.turnPhase = 'resolving';
                this._doPlayerTurn(results);
            }
        });
    }

    _showRespinChoice(results) {
        const W = 1280;
        this._cleanupRespinUI();

        this.respinBtn = this.add.text(W / 2 + 180, 498, `🔄 리스핀 [${this.respinLeft}]`, {
            fontSize: '18px', fontFamily: 'monospace', color: '#44ccff',
            backgroundColor: '#1a2a3a', padding: { x: 14, y: 8 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true }).setDepth(100);

        this.respinBtn.on('pointerover', () => this.respinBtn.setColor('#ffffff'));
        this.respinBtn.on('pointerout', () => this.respinBtn.setColor('#44ccff'));
        this.respinBtn.on('pointerdown', () => {
            this.respinLeft--;
            this._cleanupRespinUI();
            this._popText(640, 350, '🔄 리스핀!', '#44ccff', 26);
            this._updateRespinDisplay();
            this.slotMachine.spin((newResults) => {
                if (this.respinLeft > 0) {
                    this.turnPhase = 'awaitRespin';
                    this._showRespinChoice(newResults);
                } else {
                    this.turnPhase = 'resolving';
                    this._doPlayerTurn(newResults);
                }
            });
        });

        this.confirmBtn = this.add.text(W / 2 - 180, 498, '✔️ 확정', {
            fontSize: '18px', fontFamily: 'monospace', color: '#44ff88',
            backgroundColor: '#1a3a2a', padding: { x: 14, y: 8 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true }).setDepth(100);

        this.confirmBtn.on('pointerdown', () => {
            this._cleanupRespinUI();
            this.turnPhase = 'resolving';
            this._doPlayerTurn(results);
        });
    }

    _cleanupRespinUI() {
        if (this.respinBtn && this.respinBtn.scene) { this.respinBtn.destroy(); this.respinBtn = null; }
        if (this.confirmBtn && this.confirmBtn.scene) { this.confirmBtn.destroy(); this.confirmBtn = null; }
    }

    _doPlayerTurn(results) {
        // P2: Event round — skull in results triggers random relic
        if (this.encounterType === 'event_round' || this._isEventRound) {
            const skullIdx = results.findIndex(r => r.id === 'skull');
            if (skullIdx >= 0) {
                this._skullToRelic(skullIdx, results);
            }
        }

        const comboUpgrades = this.playerState.comboUpgrades || {};
        const { log, actions, hasCombo } = this.combatResolver.resolve(
            results, this.playerState, this.enemies, this.slotMachine, comboUpgrades
        );

        // relic: berserk — damage boost at low HP
        if (this.playerState.relics && this.playerState.relics.includes('berserker_mark')) {
            if (this.playerState.hp <= this.playerState.maxHp * 0.5) {
                for (const entry of log) {
                    if (entry.type === 'damage') {
                        entry.value = Math.floor(entry.value * 1.5);
                    }
                }
            }
        }

        // relic: golden_hand — double gold
        if (this.playerState.relics && this.playerState.relics.includes('golden_hand')) {
            for (const entry of log) {
                if (entry.type === 'gold') {
                    entry.value *= 2;
                    this.playerState.gold += entry.value / 2;
                }
            }
        }

        if (hasCombo) {
            const comboAction = actions.find(a => a.type === 'combo');
            this.comboText.setText(`✨ ${comboAction.name}! ✨`);
            this.comboText.setAlpha(1).setScale(0.5);
            this.tweens.add({ targets: this.comboText, scaleX: 1.2, scaleY: 1.2, duration: 120, yoyo: true });
            this.cameras.main.shake(120, 0.012);
        }

        this._showActionPopups(log);
        this._updateAllDisplays();
        this._appendLog(log);

        if (this._checkBattleEnd(300)) return;

        // relic: overcharge_core — extra spin on combo
        if (hasCombo && this.playerState.relics &&
            this.playerState.relics.includes('overcharge_core') && !this._extraSpinUsed) {
            this._extraSpinUsed = true;
            this._popText(640, 350, '⚡ 추가 스핀!', '#ffff44', 24);
            this.time.delayedCall(300, () => {
                this.slotMachine.spin((extraResults) => {
                    this._doPlayerTurn(extraResults);
                });
            });
            return;
        }
        this._extraSpinUsed = false;

        // tick cooldowns → enemy turn
        const attackers = this.combatResolver.tickEnemyCooldowns(this.enemies);
        this._updateIntentDisplays();

        if (attackers.length > 0) {
            this.time.delayedCall(350, () => this._doEnemyTurn(attackers));
        } else {
            this._appendLog([{ type: 'info', text: '적이 공격을 준비 중…' }]);
            this.time.delayedCall(150, () => this._readyForSpin());
        }
    }

    _doEnemyTurn(actors) {
        this.turnPhase = 'enemyTurn';

        for (const enemy of actors) {
            const ec = this.enemyContainers[enemy.index];
            if (!ec) continue;
            this.tweens.add({
                targets: ec.container, y: ec.container.y + 20,
                duration: 80, yoyo: true,
                onYoyo: () => this.cameras.main.shake(60, 0.006)
            });
        }

        this.time.delayedCall(120, () => {
            const log = this.combatResolver.enemyAct(actors, this.playerState, this.enemies);
            this._showEnemyActPopups(log);
            this._updateAllDisplays();
            this._appendLog(log);

            if (this._checkBattleEnd(250)) return;
            this.time.delayedCall(200, () => this._readyForSpin());
        });
    }

    _readyForSpin() {
        this.turnPhase = 'ready';
        this.spinBtn.setAlpha(1);
    }

    _checkBattleEnd(delay) {
        if (this.enemies.every(e => e.hp <= 0)) {
            this.time.delayedCall(delay, () => this._onRoundWin());
            return true;
        }
        if (this.playerState.hp <= 0) {
            this.time.delayedCall(delay, () => this._onPlayerDeath());
            return true;
        }
        return false;
    }

    // ── VISUAL FEEDBACK ────────────────────────────

    _showActionPopups(log) {
        let dmgDelay = 0;
        for (const entry of log) {
            if (entry.type === 'damage' || entry.type === 'dot') {
                const idx = entry.targetIdx !== undefined ? entry.targetIdx : this.enemies.findIndex(e => e.name === entry.target);
                if (idx >= 0 && this.enemyContainers[idx]) {
                    const ec = this.enemyContainers[idx];
                    this.time.delayedCall(dmgDelay, () => {
                        this._popText(ec.container.x, ec.container.y - 50, `-${entry.value}`, '#ff4444', 24);
                        this._shakeObj(ec.container);
                        this._flashWhite(ec.icon);
                    });
                    dmgDelay += 60;
                }
            }
            if (entry.type === 'heal') this._popText(200, 540, `+${entry.value} HP`, '#44ff88', 22);
            if (entry.type === 'gold') this._popText(450, 540, `+${entry.value} G`, '#ffcc00', 22);
            if (entry.type === 'block') this._popText(200, 520, `+${entry.value} 🛡️`, '#4488ff', 22);
            if (entry.type === 'slow') this._popText(200, 500, `❄️ 둔화!`, '#88ccff', 20);
            if (entry.type === 'burn') this._popText(200, 500, `🔥 화상!`, '#ff8800', 20);
            if (entry.type === 'poison') this._popText(200, 500, `☠️ 독!`, '#88ff00', 20);
        }
    }

    _showEnemyActPopups(log) {
        for (const entry of log) {
            if (entry.type === 'playerHit') {
                this._popText(230, 545, `💥 -${entry.value}`, '#ff2222', 26);
                this.cameras.main.shake(100, 0.012);
                this.cameras.main.flash(80, 255, 30, 30, false, null, null, 0.4);
            }
            if (entry.type === 'blocked') {
                this._popText(230, 520, `🛡️ -${entry.value}`, '#4488ff', 22);
            }
            if (entry.type === 'dot') {
                const idx = entry.targetIdx !== undefined ? entry.targetIdx : -1;
                if (idx >= 0 && this.enemyContainers[idx]) {
                    const ec = this.enemyContainers[idx];
                    this._popText(ec.container.x, ec.container.y - 50, `-${entry.value}`, entry.dotType === 'burn' ? '#ff8800' : '#88ff00', 20);
                }
            }
            if (entry.type === 'enemyDefend') {
                const idx = entry.targetIdx;
                if (idx >= 0 && this.enemyContainers[idx]) {
                    const ec = this.enemyContainers[idx];
                    this._popText(ec.container.x, ec.container.y - 50, `🛡️+${entry.value}`, '#6688ff', 22);
                }
            }
            if (entry.type === 'enemyBuff') {
                const idx = entry.targetIdx;
                if (idx >= 0 && this.enemyContainers[idx]) {
                    const ec = this.enemyContainers[idx];
                    this._popText(ec.container.x, ec.container.y - 50, `⬆️ 공격+${entry.value}`, '#ff8844', 20);
                }
            }
            if (entry.type === 'enemyHeal') {
                const idx = entry.targetIdx;
                if (idx >= 0 && this.enemyContainers[idx]) {
                    const ec = this.enemyContainers[idx];
                    this._popText(ec.container.x, ec.container.y - 50, `💚+${entry.value}`, '#44ff88', 22);
                }
            }
            if (entry.type === 'statusApplied') {
                const icons = { bleed: '🩸', burn: '🔥' };
                this._popText(230, 500, `${icons[entry.status] || '⚠️'} ${entry.status} +${entry.value}!`, '#ff6644', 20);
            }
            if (entry.type === 'playerDot') {
                const icons = { bleed: '🩸', burn: '🔥' };
                this._popText(230, 545, `${icons[entry.dotType] || ''} -${entry.value}`, '#ff6644', 22);
            }
        }
    }

    _popText(x, y, text, color, size) {
        const t = this.add.text(x, y, text, {
            fontSize: size + 'px', fontFamily: 'monospace', color: color, fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(200).setScale(0.3);

        this.tweens.add({
            targets: t, scaleX: 1.1, scaleY: 1.1, y: y - 30,
            duration: 150, ease: 'Back.easeOut',
            onComplete: () => {
                this.tweens.add({
                    targets: t, alpha: 0, y: y - 55,
                    duration: 350, ease: 'Power2',
                    onComplete: () => t.destroy()
                });
            }
        });
    }

    _shakeObj(obj) {
        const bx = obj.getData('baseX') || obj.x;
        this.tweens.add({
            targets: obj, x: bx + 8, duration: 30, yoyo: true, repeat: 2,
            onComplete: () => { obj.x = bx; }
        });
    }

    _flashWhite(textObj) {
        const orig = textObj.style.color;
        textObj.setTint(0xffffff);
        this.time.delayedCall(80, () => textObj.clearTint());
    }

    // ── DISPLAY UPDATES ────────────────────────────

    _updateAllDisplays() {
        const ps = this.playerState;

        // Player HP
        this.playerHpBar.clear();
        const hpRatio = Math.max(0, ps.hp / ps.maxHp);
        const hpColor = hpRatio > 0.5 ? 0x44cc44 : hpRatio > 0.25 ? 0xcccc44 : 0xcc4444;
        this.playerHpBar.fillStyle(hpColor, 1);
        this.playerHpBar.fillRoundedRect(80, 560, 300 * hpRatio, 24, 4);
        this.playerHpText.setText(`${Math.max(0, ps.hp)} / ${ps.maxHp}`);

        // Block
        this.playerBlockBarGfx.clear();
        if (ps.block > 0) {
            const blockRatio = Math.min(1, ps.block / ps.maxHp);
            this.playerBlockBarGfx.fillStyle(0x4488ff, 1);
            this.playerBlockBarGfx.fillRoundedRect(80, 594, 300 * blockRatio, 18, 4);
        }
        this.playerBlockText.setText(ps.block > 0 ? `${ps.block}` : '');

        // Player status effects
        const playerStatuses = [];
        if (ps.bleed > 0) playerStatuses.push(`🩸${ps.bleed}`);
        if (ps.burn > 0) playerStatuses.push(`🔥${ps.burn}`);
        this.playerBlockText.setText(
            (ps.block > 0 ? `${ps.block}` : '') +
            (playerStatuses.length > 0 ? '  ' + playerStatuses.join(' ') : '')
        );

        this.goldLabel.setText(`🪙 ${ps.gold}`);
        this.goldTopLabel.setText(`🪙 ${ps.gold}`);
        this._updateRespinDisplay();
        this.deckLabel.setText(`덱 ${ps.symbolPool.length}장:`);
        this.deckIconsText.setText(ps.symbolPool.map(id => { const s = SYMBOL_DATA[id]; return s ? s.icon : '?'; }).join(''));

        // Enemies
        for (let i = 0; i < this.enemies.length; i++) {
            const enemy = this.enemies[i];
            const ec = this.enemyContainers[i];
            if (!ec) continue;

            ec.hpBar.clear();
            const ratio = Math.max(0, enemy.hp / enemy.maxHp);
            const barColor = ratio > 0.5 ? 0x44cc44 : ratio > 0.25 ? 0xcccc44 : 0xcc4444;
            ec.hpBar.fillStyle(barColor, 1);
            ec.hpBar.fillRoundedRect(-45, 90, 90 * ratio, 14, 4);
            ec.hpText.setText(`${Math.max(0, enemy.hp)}/${enemy.maxHp}`);

            const statuses = [];
            if (enemy.block > 0) statuses.push(`🛡️${enemy.block}`);
            if (enemy.burn > 0) statuses.push(`🔥${enemy.burn}`);
            if (enemy.poison > 0) statuses.push(`☠️${enemy.poison}`);
            ec.statusText.setText(statuses.join(' '));

            if (enemy.hp <= 0) {
                ec.container.setAlpha(0.15);
                ec.intentText.setText('💀');
            }
        }

        this._updateIntentDisplays();
    }

    _updateIntentDisplays() {
        for (let i = 0; i < this.enemies.length; i++) {
            const enemy = this.enemies[i];
            const ec = this.enemyContainers[i];
            if (!ec || enemy.hp <= 0) continue;

            const intent = enemy.nextIntent || { type: 'attack' };
            const intentLabel = this._intentLabel(intent, enemy);

            if (enemy.cooldownTimer <= 1) {
                ec.intentText.setText(intentLabel);
                ec.intentText.setColor(this._intentColor(intent));
                ec.intentText.setFontSize(14);
            } else {
                ec.intentText.setText(`${enemy.cooldownTimer}턴 ${intentLabel}`);
                ec.intentText.setColor(enemy.cooldownTimer === 2 ? '#ff8844' : '#888888');
                ec.intentText.setFontSize(12);
            }
        }
    }

    _updateRespinDisplay() {
        if (this.respinLabel) {
            const color = this.respinLeft > 0 ? '#44ccff' : '#555555';
            this.respinLabel.setText(`🔄 리스핀 [${this.respinLeft}/${this.respinMax}]`);
            this.respinLabel.setColor(color);
        }
    }

    _intentLabel(intent, enemy) {
        switch (intent.type) {
            case 'attack': return `⚔️${enemy.attack}`;
            case 'bleed': return `🩸${Math.floor(enemy.attack * 0.6)}+출혈`;
            case 'attackBleed': return `⚔️${enemy.attack}+🩸`;
            case 'attackBurn': return `⚔️${enemy.attack}+🔥`;
            case 'lifesteal': return `🧛${enemy.attack}`;
            case 'doubleStrike': return `⚔️⚔️${Math.floor(enemy.attack * 0.6)}×2`;
            case 'defend': return `🛡️+${intent.block || 8}`;
            case 'buff': return `⬆️공격+${intent.atkUp || 0}`;
            case 'healAll': return `💚${intent.heal || 10}`;
            default: return `⚔️${enemy.attack}`;
        }
    }

    _intentColor(intent) {
        switch (intent.type) {
            case 'defend': return '#6688ff';
            case 'buff': return '#ff8844';
            case 'healAll': return '#44ff88';
            default: return '#ff4444';
        }
    }

    _appendLog(log) {
        const lines = [];
        for (const entry of log) {
            if (entry.type === 'info') lines.push(entry.text);
            if (entry.type === 'damage') lines.push(`⚔️ ${entry.target} -${entry.value}`);
            if (entry.type === 'heal') lines.push(`💚 +${entry.value} HP`);
            if (entry.type === 'block') lines.push(`🛡️ +${entry.value} 방어`);
            if (entry.type === 'gold') lines.push(`🪙 +${entry.value} G`);
            if (entry.type === 'selfDamage') lines.push(`💀 자해 -${entry.value}`);
            if (entry.type === 'playerHit') lines.push(`💥 ${entry.from} → -${entry.value}!`);
            if (entry.type === 'blocked') lines.push(`🛡️ ${entry.from} → 방어 ${entry.value}`);
            if (entry.type === 'dot') lines.push(`${entry.dotType === 'burn' ? '🔥' : '☠️'} ${entry.target} -${entry.value}`);
            if (entry.type === 'burn') lines.push(`🔥 ${entry.target} 화상!`);
            if (entry.type === 'poison') lines.push(`☠️ ${entry.target} 독!`);
            if (entry.type === 'slow') lines.push(`❄️ ${entry.target} 둔화!`);
            if (entry.type === 'enemyDefend') lines.push(`🛡️ ${entry.target} 방어+${entry.value}`);
            if (entry.type === 'enemyBuff') lines.push(`⬆️ ${entry.target} 공격+${entry.value}`);
            if (entry.type === 'enemyHeal') lines.push(`💚 ${entry.target} 회복+${entry.value}`);
            if (entry.type === 'enemyBlocked') lines.push(`🛡️ ${entry.target} 방어로 ${entry.value} 흡수`);
            if (entry.type === 'statusApplied') lines.push(`${entry.status === 'bleed' ? '🩸' : '🔥'} ${entry.status} +${entry.value}!`);
            if (entry.type === 'playerDot') lines.push(`${entry.dotType === 'bleed' ? '🩸' : '🔥'} ${entry.dotType} -${entry.value}`);
        }
        this.combatLog = this.combatLog.concat(lines);
        if (this.combatLog.length > 50) this.combatLog = this.combatLog.slice(-50);
        this.logText.setText(this.combatLog.join('\n'));
        if (this.autoScrollLog) {
            this._scrollLogToBottom();
        }
    }

    _showEventBanner() {
        const W = 1280;
        const banner = this.add.text(W / 2, 270, '✨ 이벤트 라운드! 해골 → 유물 교체 ✨', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffcc00', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 4, backgroundColor: '#2a1a3a', padding: { x: 16, y: 8 }
        }).setOrigin(0.5).setDepth(300).setAlpha(0);

        this.tweens.add({
            targets: banner, alpha: 1, duration: 300,
            onComplete: () => {
                this.tweens.add({
                    targets: banner, alpha: 0, duration: 500, delay: 2000,
                    onComplete: () => banner.destroy()
                });
            }
        });
    }

    _skullToRelic(skullIdx, results) {
        // Weighted random: common 60%, uncommon 30%, rare 10%
        const roll = Math.random();
        let pool;
        if (roll < 0.6) pool = RELIC_POOLS.common;
        else if (roll < 0.9) pool = RELIC_POOLS.uncommon;
        else pool = RELIC_POOLS.rare;

        const owned = this.playerState.relics || [];
        let candidates = pool.filter(r => !owned.includes(r.id));
        // fallback: try any pool if empty
        if (candidates.length === 0) {
            candidates = Object.values(RELIC_DATA).filter(r => !owned.includes(r.id));
        }
        if (candidates.length === 0) return; // all relics owned

        const relic = candidates[Phaser.Math.Between(0, candidates.length - 1)];
        this.playerState.relics.push(relic.id);

        // Visual: replace skull icon in reel display
        const reelContainer = this.slotMachine.reelContainers[skullIdx];
        if (reelContainer) {
            const iconText = reelContainer.list[0];
            const nameLabel = reelContainer.list[1];
            if (iconText) iconText.setText(relic.icon);
            if (nameLabel) nameLabel.setText(relic.name);
        }

        this._popText(640, 300, `${relic.icon} ${relic.name} 획득!`, '#aa88ff', 28);
        this._appendLog([{ type: 'info', text: `✨ 해골 → 유물 [${relic.name}] 획득!` }]);
    }

    _onRoundWin() {
        const goldReward = this.encounterType === 'boss' ? 20 :
                           this.encounterType === 'elite' ? 12 : 5 + (this.act + 1) * 2;
        this.playerState.gold += goldReward;

        // relic: vampiric_fang — heal on kill
        if (this.playerState.relics.includes('vampiric_fang')) {
            const deadCount = this.enemies.filter(e => e.hp <= 0).length;
            const healAmt = deadCount * 5;
            this.playerState.hp = Math.min(this.playerState.maxHp, this.playerState.hp + healAmt);
        }

        if (this.map) {
            if (this.encounterType === 'boss') {
                // boss defeated — next act or victory
                if (this.act >= MAP_CONFIG.acts.length - 1) {
                    this.scene.start('GameOverScene', {
                        playerState: this.playerState,
                        victory: true
                    });
                } else {
                    // show reward popup then next act
                    this.playerState.currentFloor = 0;
                    this.playerState.visitedNodes = [];
                    this.scene.start('MapScene', {
                        act: this.act + 1,
                        playerState: this.playerState
                    });
                }
            } else {
                // return to map
                this.scene.start('MapScene', {
                    act: this.act,
                    map: this.map,
                    playerState: this.playerState
                });
            }
        } else {
            // legacy round-based flow
            this.scene.start('ShopScene', {
                round: this.round,
                playerState: this.playerState,
                goldReward: goldReward
            });
        }
    }

    _onPlayerDeath() {
        // relic: phoenix_feather — revive once
        if (this.playerState.relics.includes('phoenix_feather')) {
            const idx = this.playerState.relics.indexOf('phoenix_feather');
            this.playerState.relics.splice(idx, 1);
            this.playerState.hp = Math.floor(this.playerState.maxHp * 0.3);
            this._popText(640, 300, '🔥 불사조 부활!', '#ff8800', 32);
            this._updateAllDisplays();
            this.time.delayedCall(500, () => this._readyForSpin());
            return;
        }
        this.scene.start('GameOverScene', {
            round: this.round,
            act: this.act,
            playerState: this.playerState
        });
    }
}
