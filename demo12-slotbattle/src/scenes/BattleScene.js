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

    // ── RELIC HELPERS (stacking) ─────────────────────
    _relicCount(relicId) {
        return this.playerState.relics.filter(r => r === relicId).length;
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a1a');

        this.slotMachine = new SlotMachine(this);
        this.slotMachine.setSymbolPool(this.playerState.symbolPool);
        this.combatResolver = new CombatResolver();
        this.enemies = this._spawnEnemies();
        this.turnPhase = 'ready';
        this.turnCount = 0; // for time_crystal periodic slow
        this.combatLog = [];
        this.combatLogScrollY = 0;
        this.autoScrollLog = true;

        // P2: Event round — ~17% chance on normal battles
        this._isEventRound = false;
        if (this.encounterType === 'battle' && Math.random() < 0.17) {
            this._isEventRound = true;
        }

        this._createTopBar();
        this._createEnemyDisplay();
        this._createSlotDisplay();
        this._createActionPreview();
        this._createPlayerHUD();
        this._createCombatLog();
        this._createSideButtons();
        this._updateAllDisplays();

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
            // relic: ice_crystal — enemies start slowed (stacking)
            const iceCount = this._relicCount('ice_crystal');
            if (iceCount > 0) enemy.cooldownTimer += iceCount;
            enemy.nextIntent = this.combatResolver.rollEnemyIntent(enemy);
            enemies.push(enemy);
        }

        // relic: battle_drum — start with block (stacking: 5 per copy)
        const drumCount = this._relicCount('battle_drum');
        if (drumCount > 0) this.playerState.block += 5 * drumCount;

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
        const W = 1280, slotY = 370;

        this.comboText = this.add.text(W / 2, 290, '', {
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

        // Respin chance indicator (relic-only)
        const respinCount = this._relicCount('respin_charm');
        if (respinCount > 0) {
            const chance = Math.round((1 - Math.pow(0.9, respinCount)) * 100);
            this.add.text(W / 2 + 240, slotY, `🔄${chance}%`, {
                fontSize: '14px', fontFamily: 'monospace', color: '#44ccff'
            }).setOrigin(0, 0.5);
        }

        this.spinBtn = this.add.text(W / 2, 478, '[ SPIN! ]', {
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

    _createActionPreview() {
        const W = 1280;
        // Area between slot and HUD to show action preview
        this.actionPreviewText = this.add.text(W / 2, 440, '', {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setAlpha(0).setDepth(100);
    }

    _createPlayerHUD() {
        const W = 1280, barY = 540;

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
        this.deckLabel = this.add.text(420, barY + 34, '', { fontSize: '13px', fontFamily: 'monospace', color: '#888888' });
        this.deckIconsText = this.add.text(560, barY + 34, '', {
            fontSize: '13px', fontFamily: 'monospace', color: '#666666', wordWrap: { width: 700 }
        });

        // Relic display
        if (this.playerState.relics.length > 0) {
            const relicCounts = {};
            for (const r of this.playerState.relics) {
                relicCounts[r] = (relicCounts[r] || 0) + 1;
            }
            const relicStr = Object.entries(relicCounts).map(([id, cnt]) => {
                const rd = RELIC_DATA[id];
                return rd ? (cnt > 1 ? `${rd.icon}×${cnt}` : rd.icon) : '?';
            }).join(' ');
            this.add.text(700, barY + 6, relicStr, { fontSize: '16px', fontFamily: 'monospace', color: '#aa88ff' });
        }
    }

    _createCombatLog() {
        const logY = 635, logH = 80, logW = 900;
        const maskShape = this.make.graphics();
        maskShape.fillRect(20, logY, logW, logH);
        const mask = maskShape.createGeometryMask();

        this.logContainer = this.add.container(0, 0).setMask(mask);
        this.logText = this.add.text(20, logY, '', {
            fontSize: '12px', fontFamily: 'monospace', color: '#777777',
            wordWrap: { width: logW }, lineSpacing: 2
        });
        this.logContainer.add(this.logText);

        this.input.on('wheel', (pointer, gameObjects, deltaX, deltaY) => {
            if (pointer.y >= logY && pointer.y <= logY + logH) {
                this.autoScrollLog = false;
                this.combatLogScrollY -= deltaY * 0.5;
                this._clampLogScroll();
            }
        });

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
        const logY = 635, logH = 80;
        const textHeight = this.logText.height || 0;
        const minScroll = Math.min(0, -(textHeight - logH));
        this.combatLogScrollY = Phaser.Math.Clamp(this.combatLogScrollY, minScroll, 0);
        this.logText.y = logY + this.combatLogScrollY;
        if (this.logLatestBtn) {
            this.logLatestBtn.setAlpha(this.combatLogScrollY < minScroll + 5 ? 0 : 1);
        }
    }

    _scrollLogToBottom() {
        const logY = 635, logH = 80;
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
        this.turnCount++;
        this.spinBtn.setAlpha(0.3);
        this.comboText.setAlpha(0);
        this.actionPreviewText.setAlpha(0);

        // relic: focus_lens — boost pool weighting for symbols owned 3+
        const focusCount = this._relicCount('focus_lens');
        if (focusCount > 0) {
            this._applyFocusLensPool();
        } else {
            this.slotMachine.setSymbolPool(this.playerState.symbolPool);
        }

        this.slotMachine.spin((results) => {
            // Apply post-spin relic effects to results
            results = this._applyPostSpinRelics(results);

            // Check auto-respin from relic (10% per respin_charm, stacking)
            const respinCount = this._relicCount('respin_charm');
            const respinChance = 1 - Math.pow(0.9, respinCount);

            if (respinCount > 0 && Math.random() < respinChance) {
                // Auto-respin triggered! Store first results
                this._firstSpinResults = results;
                this._popText(640, 330, '🔄 자동 리스핀!', '#44ccff', 24);
                this._appendLog([{ type: 'info', text: '🔄 재회전 부적 발동! 자동 리스핀!' }]);

                this.time.delayedCall(600, () => {
                    this.slotMachine.spin((secondResults) => {
                        secondResults = this._applyPostSpinRelics(secondResults);
                        // Both results apply — combine them
                        this.turnPhase = 'previewing';
                        this._doPlayerTurnDouble(this._firstSpinResults, secondResults);
                    });
                });
            } else {
                this.turnPhase = 'previewing';
                this._showPreviewThenAct(results);
            }
        });
    }

    // ── POST-SPIN RELIC EFFECTS ─────────────────────

    _applyFocusLensPool() {
        // Boost appearance of symbols owned 3+ copies: add extra copies to pool
        const pool = this.playerState.symbolPool.slice();
        const counts = {};
        for (const s of pool) counts[s] = (counts[s] || 0) + 1;
        for (const [sym, cnt] of Object.entries(counts)) {
            if (cnt >= 3) {
                // Add 50% more copies (rounded up)
                const extra = Math.ceil(cnt * 0.5);
                for (let i = 0; i < extra; i++) pool.push(sym);
            }
        }
        this.slotMachine.setSymbolPool(pool);
    }

    _applyPostSpinRelics(results) {
        // relic: lucky_clover — wild card chance (+10% per copy)
        const cloverCount = this._relicCount('lucky_clover');
        if (cloverCount > 0) {
            const wildChance = 0.1 * cloverCount;
            for (let i = 0; i < results.length; i++) {
                if (results[i] !== 'gem' && Math.random() < wildChance) {
                    results[i] = 'gem';
                    this._appendLog([{ type: 'info', text: `🍀 행운의 클로버: 슬롯 ${i + 1} → 와일드카드!` }]);
                }
            }
        }

        // relic: resonance_stone — 2 same symbols → trigger 3rd effect
        if (this._relicCount('resonance_stone') > 0 && results.length === 3) {
            if (results[0] === results[1] && results[0] !== results[2] && results[0] !== 'skull') {
                results[2] = results[0];
                this._appendLog([{ type: 'info', text: `🔗 공명석: ${SYMBOL_DATA[results[0]]?.name || results[0]} 공명 발동!` }]);
            } else if (results[0] === results[2] && results[0] !== results[1] && results[0] !== 'skull') {
                results[1] = results[0];
                this._appendLog([{ type: 'info', text: `🔗 공명석: ${SYMBOL_DATA[results[0]]?.name || results[0]} 공명 발동!` }]);
            } else if (results[1] === results[2] && results[1] !== results[0] && results[1] !== 'skull') {
                results[0] = results[1];
                this._appendLog([{ type: 'info', text: `🔗 공명석: ${SYMBOL_DATA[results[1]]?.name || results[1]} 공명 발동!` }]);
            }
        }

        return results;
    }

    // ── ACTION PREVIEW ──────────────────────────────

    _buildPreviewText(log) {
        const parts = [];
        let totalDmg = 0, totalBlock = 0, totalHeal = 0, totalGold = 0;
        for (const entry of log) {
            if (entry.type === 'damage' || entry.type === 'dot') totalDmg += entry.value;
            if (entry.type === 'block') totalBlock += entry.value;
            if (entry.type === 'heal') totalHeal += entry.value;
            if (entry.type === 'gold') totalGold += entry.value;
        }
        if (totalDmg > 0) parts.push(`⚔️${totalDmg}`);
        if (totalBlock > 0) parts.push(`🛡️${totalBlock}`);
        if (totalHeal > 0) parts.push(`❤️${totalHeal}`);
        if (totalGold > 0) parts.push(`🪙${totalGold}`);
        return parts.join('  ') || '—';
    }

    _showPreviewThenAct(results) {
        // Event round skull→relic check
        if (this.encounterType === 'event_round' || this._isEventRound) {
            const skullIdx = results.findIndex(r => r === 'skull');
            if (skullIdx >= 0) this._skullToRelic(skullIdx, results);
        }

        const comboUpgrades = this.playerState.comboUpgrades || {};
        // Dry-run: calculate what WILL happen (using copies)
        const previewPlayer = { ...this.playerState };
        const previewEnemies = this.enemies.map(e => ({ ...e }));
        const { log, actions, hasCombo } = this.combatResolver.resolve(
            results, previewPlayer, previewEnemies, this.slotMachine, comboUpgrades
        );

        // Apply relic modifiers to preview
        this._applyRelicModifiers(log);

        // Show combo name
        if (hasCombo) {
            const comboAction = actions.find(a => a.type === 'combo');
            this.comboText.setText(`✨ ${comboAction.name}! ✨`);
            this.comboText.setAlpha(1).setScale(0.5);
            this.tweens.add({ targets: this.comboText, scaleX: 1.2, scaleY: 1.2, duration: 200, yoyo: true });
        }

        // Show preview of total effects
        const previewStr = this._buildPreviewText(log);
        this.actionPreviewText.setText(previewStr);
        this.actionPreviewText.setAlpha(0);
        this.tweens.add({
            targets: this.actionPreviewText, alpha: 1, duration: 200, ease: 'Power2',
        });

        // Wait, then actually apply
        this.time.delayedCall(1200, () => {
            this.actionPreviewText.setAlpha(0);
            this.turnPhase = 'resolving';
            this._doPlayerTurn(results);
        });
    }

    _doPlayerTurnDouble(firstResults, secondResults) {
        // Both spin results apply — resolve each, combine effects
        if (this._isEventRound) {
            const skullIdx1 = firstResults.findIndex(r => r.id === 'skull');
            if (skullIdx1 >= 0) this._skullToRelic(skullIdx1, firstResults);
            const skullIdx2 = secondResults.findIndex(r => r.id === 'skull');
            if (skullIdx2 >= 0) this._skullToRelic(skullIdx2, secondResults);
        }

        const comboUpgrades = this.playerState.comboUpgrades || {};

        // Resolve first spin
        const r1 = this.combatResolver.resolve(firstResults, this.playerState, this.enemies, this.slotMachine, comboUpgrades);
        this._applyRelicModifiers(r1.log);

        // Preview combined text for both
        const previewStr1 = this._buildPreviewText(r1.log);

        // Show spin 1 preview
        this.actionPreviewText.setText(`1st: ${previewStr1}`);
        this.actionPreviewText.setAlpha(1);

        if (r1.hasCombo) {
            const comboAction = r1.actions.find(a => a.type === 'combo');
            this.comboText.setText(`✨ ${comboAction.name}! ✨`);
            this.comboText.setAlpha(1).setScale(0.5);
            this.tweens.add({ targets: this.comboText, scaleX: 1.2, scaleY: 1.2, duration: 200, yoyo: true });
        }

        // Apply first spin effects with visual feedback
        this.time.delayedCall(800, () => {
            this._showActionPopups(r1.log);
            this._updateAllDisplays();
            this._appendLog(r1.log);

            if (this._checkBattleEnd(400)) return;

            // Now resolve second spin
            this.time.delayedCall(600, () => {
                const r2 = this.combatResolver.resolve(secondResults, this.playerState, this.enemies, this.slotMachine, comboUpgrades);
                this._applyRelicModifiers(r2.log);

                const previewStr2 = this._buildPreviewText(r2.log);
                this.actionPreviewText.setText(`2nd: ${previewStr2}`);

                if (r2.hasCombo) {
                    const comboAction = r2.actions.find(a => a.type === 'combo');
                    this.comboText.setText(`✨ ${comboAction.name}! ✨`);
                    this.comboText.setAlpha(1).setScale(0.5);
                    this.tweens.add({ targets: this.comboText, scaleX: 1.2, scaleY: 1.2, duration: 200, yoyo: true });
                    this.cameras.main.shake(120, 0.012);
                }

                this.time.delayedCall(800, () => {
                    this.actionPreviewText.setAlpha(0);
                    this._showActionPopups(r2.log);
                    this._updateAllDisplays();
                    this._appendLog(r2.log);

                    if (this._checkBattleEnd(400)) return;

                    // Check overcharge on either combo
                    const hadCombo = r1.hasCombo || r2.hasCombo;
                    this._afterPlayerActions(hadCombo);
                });
            });
        });
    }

    _applyRelicModifiers(log) {
        // relic: berserker_mark — damage boost at low HP (stacking: +50% per copy)
        const berserkCount = this._relicCount('berserker_mark');
        if (berserkCount > 0 && this.playerState.hp <= this.playerState.maxHp * 0.5) {
            const mult = 1 + 0.5 * berserkCount;
            for (const entry of log) {
                if (entry.type === 'damage') {
                    entry.value = Math.floor(entry.value * mult);
                }
            }
        }

        // relic: golden_hand — gold multiplier (stacking: 2x per copy)
        const goldCount = this._relicCount('golden_hand');
        if (goldCount > 0) {
            for (const entry of log) {
                if (entry.type === 'gold') {
                    const mult = Math.pow(2, goldCount);
                    const bonus = entry.value * (mult - 1);
                    entry.value = Math.floor(entry.value * mult);
                    this.playerState.gold += Math.floor(bonus);
                }
            }
        }

        // relic: mirror_shield — excess block → damage bonus
        const mirrorCount = this._relicCount('mirror_shield');
        if (mirrorCount > 0 && this.playerState.block > this.playerState.maxHp) {
            const excess = this.playerState.block - this.playerState.maxHp;
            const bonusDmg = Math.floor(excess * 0.5 * mirrorCount);
            if (bonusDmg > 0) {
                // Add bonus damage to existing damage entries
                const dmgEntries = log.filter(e => e.type === 'damage');
                if (dmgEntries.length > 0) {
                    dmgEntries[0].value += bonusDmg;
                    log.push({ type: 'info', text: `🪞 거울 방패: 초과 방어 → 공격 +${bonusDmg}` });
                }
            }
        }

        // relic: flame_heart — auto burn random enemy per spin (stacking: burn +2 per copy)
        const flameCount = this._relicCount('flame_heart');
        if (flameCount > 0) {
            const alive = this.enemies.filter(e => e.hp > 0);
            if (alive.length > 0) {
                const target = alive[Phaser.Math.Between(0, alive.length - 1)];
                const burnAmt = 2 * flameCount;
                target.burn = (target.burn || 0) + burnAmt;
                log.push({ type: 'burn', value: burnAmt, target: target.name });
                log.push({ type: 'info', text: `🔥 불꽃 심장: ${target.name}에게 화상 ${burnAmt}` });
            }
        }

        // relic: chaos_orb — bonus 4th slot effect (random symbol)
        const chaosCount = this._relicCount('chaos_orb');
        if (chaosCount > 0) {
            for (let c = 0; c < chaosCount; c++) {
                const pool = this.playerState.symbolPool.filter(s => s !== 'skull');
                if (pool.length === 0) break;
                const bonusSym = pool[Phaser.Math.Between(0, pool.length - 1)];
                const symData = SYMBOL_DATA[bonusSym];
                if (!symData) continue;
                const e = symData.effect;
                if (e.damage) {
                    const alive = this.enemies.filter(en => en.hp > 0);
                    if (alive.length > 0) {
                        const t = alive[0];
                        const dmg = Math.max(1, e.damage - (t.block > 0 ? t.block : t.defense));
                        t.hp -= dmg;
                        log.push({ type: 'damage', value: dmg, target: t.name, targetIdx: t.index });
                    }
                }
                if (e.block) {
                    this.playerState.block += e.block;
                    log.push({ type: 'block', value: e.block });
                }
                if (e.heal) {
                    const healed = Math.min(e.heal, this.playerState.maxHp - this.playerState.hp);
                    this.playerState.hp += healed;
                    log.push({ type: 'heal', value: healed });
                }
                if (e.gold) {
                    this.playerState.gold += e.gold;
                    log.push({ type: 'gold', value: e.gold });
                }
                log.push({ type: 'info', text: `🌀 혼돈의 오브: ${symData.icon} ${symData.name} 추가!` });
            }
        }
    }

    _doPlayerTurn(results) {
        // Event round skull→relic check
        if (this.encounterType === 'event_round' || this._isEventRound) {
            const skullIdx = results.findIndex(r => r === 'skull');
            if (skullIdx >= 0) this._skullToRelic(skullIdx, results);
        }

        const comboUpgrades = this.playerState.comboUpgrades || {};
        const { log, actions, hasCombo } = this.combatResolver.resolve(
            results, this.playerState, this.enemies, this.slotMachine, comboUpgrades
        );

        this._applyRelicModifiers(log);

        if (hasCombo) {
            const comboAction = actions.find(a => a.type === 'combo');
            this.comboText.setText(`✨ ${comboAction.name}! ✨`);
            this.comboText.setAlpha(1).setScale(0.5);
            this.tweens.add({ targets: this.comboText, scaleX: 1.2, scaleY: 1.2, duration: 200, yoyo: true });
            this.cameras.main.shake(120, 0.012);
        }

        this._showActionPopups(log);
        this._updateAllDisplays();
        this._appendLog(log);

        if (this._checkBattleEnd(500)) return;

        this._afterPlayerActions(hasCombo);
    }

    _afterPlayerActions(hasCombo) {
        // relic: overcharge_core — extra spin on combo (stacking: more chances)
        const overchargeCount = this._relicCount('overcharge_core');
        if (hasCombo && overchargeCount > 0 && !this._extraSpinUsed) {
            this._extraSpinUsed = true;
            this._popText(640, 330, '⚡ 추가 스핀!', '#ffff44', 24);
            this.time.delayedCall(800, () => {
                this.slotMachine.spin((extraResults) => {
                    this.turnPhase = 'previewing';
                    this._showPreviewThenAct(extraResults);
                });
            });
            return;
        }
        this._extraSpinUsed = false;

        // relic: time_crystal — periodic slow (every 3 turns, stacking reduces interval)
        const timeCount = this._relicCount('time_crystal');
        if (timeCount > 0) {
            const interval = Math.max(1, 3 - (timeCount - 1)); // 3 turns base, 2 with 2 copies, 1 with 3+
            if (this.turnCount % interval === 0) {
                const alive = this.enemies.filter(e => e.hp > 0);
                for (const e of alive) {
                    e.cooldownTimer++;
                }
                this._popText(640, 270, `⏳ 시간 왜곡!`, '#aa88ff', 22);
                this._appendLog([{ type: 'info', text: `⏳ 시간의 수정: 모든 적 쿨다운 +1` }]);
                this._updateIntentDisplays();
            }
        }

        // tick cooldowns → enemy turn (with tempo delay)
        this.time.delayedCall(600, () => {
            const attackers = this.combatResolver.tickEnemyCooldowns(this.enemies);
            this._updateIntentDisplays();

            if (attackers.length > 0) {
                this.time.delayedCall(500, () => this._doEnemyTurn(attackers));
            } else {
                this._appendLog([{ type: 'info', text: '적이 공격을 준비 중…' }]);
                this.time.delayedCall(400, () => this._readyForSpin());
            }
        });
    }

    _doEnemyTurn(actors) {
        this.turnPhase = 'enemyTurn';

        for (const enemy of actors) {
            const ec = this.enemyContainers[enemy.index];
            if (!ec) continue;
            this.tweens.add({
                targets: ec.container, y: ec.container.y + 20,
                duration: 120, yoyo: true,
                onYoyo: () => this.cameras.main.shake(80, 0.008)
            });
        }

        this.time.delayedCall(300, () => {
            const log = this.combatResolver.enemyAct(actors, this.playerState, this.enemies);

            // relic: thorn_armor — reflect damage (stacking: 3 per copy)
            const thornCount = this._relicCount('thorn_armor');
            if (thornCount > 0) {
                for (const entry of log) {
                    if (entry.type === 'playerHit' && entry.from) {
                        const attacker = actors.find(a => a.name === entry.from) || actors[0];
                        if (attacker && attacker.hp > 0) {
                            const thornDmg = 3 * thornCount;
                            attacker.hp -= thornDmg;
                            log.push({ type: 'damage', value: thornDmg, target: attacker.name, targetIdx: attacker.index });
                            this._appendLog([{ type: 'info', text: `🛡️ 가시 갑옷 반사 ${thornDmg} → ${attacker.name}` }]);
                        }
                    }
                }
            }

            this._showEnemyActPopups(log);
            this._updateAllDisplays();
            this._appendLog(log);

            if (this._checkBattleEnd(500)) return;
            this.time.delayedCall(500, () => this._readyForSpin());
        });
    }

    _readyForSpin() {
        this.turnPhase = 'ready';
        this.spinBtn.setAlpha(1);

        // relic: iron_boots — +2 block per turn per copy
        const bootsCount = this._relicCount('iron_boots');
        if (bootsCount > 0) {
            const amt = 2 * bootsCount;
            this.playerState.block += amt;
            this._popText(200, 500, `🥾 +${amt} 🛡️`, '#4488ff', 18);
            this._appendLog([{ type: 'info', text: `🥾 무쇠 장화: 방어 +${amt}` }]);
            this._updateAllDisplays();
        }
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
                    dmgDelay += 120;
                }
            }
            if (entry.type === 'heal') this._popText(200, 520, `+${entry.value} HP`, '#44ff88', 22);
            if (entry.type === 'gold') this._popText(450, 520, `+${entry.value} G`, '#ffcc00', 22);
            if (entry.type === 'block') this._popText(200, 500, `+${entry.value} 🛡️`, '#4488ff', 22);
            if (entry.type === 'slow') this._popText(200, 480, `❄️ 둔화!`, '#88ccff', 20);
            if (entry.type === 'burn') this._popText(200, 480, `🔥 화상!`, '#ff8800', 20);
            if (entry.type === 'poison') this._popText(200, 480, `☠️ 독!`, '#88ff00', 20);
        }
    }

    _showEnemyActPopups(log) {
        for (const entry of log) {
            if (entry.type === 'playerHit') {
                this._popText(230, 525, `💥 -${entry.value}`, '#ff2222', 26);
                this.cameras.main.shake(100, 0.012);
                this.cameras.main.flash(80, 255, 30, 30, false, null, null, 0.4);
            }
            if (entry.type === 'blocked') {
                this._popText(230, 500, `🛡️ -${entry.value}`, '#4488ff', 22);
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
                this._popText(230, 480, `${icons[entry.status] || '⚠️'} ${entry.status} +${entry.value}!`, '#ff6644', 20);
            }
            if (entry.type === 'playerDot') {
                const icons = { bleed: '🩸', burn: '🔥' };
                this._popText(230, 525, `${icons[entry.dotType] || ''} -${entry.value}`, '#ff6644', 22);
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
            duration: 200, ease: 'Back.easeOut',
            onComplete: () => {
                this.tweens.add({
                    targets: t, alpha: 0, y: y - 55,
                    duration: 500, ease: 'Power2',
                    onComplete: () => t.destroy()
                });
            }
        });
    }

    _shakeObj(obj) {
        const bx = obj.getData('baseX') || obj.x;
        this.tweens.add({
            targets: obj, x: bx + 8, duration: 40, yoyo: true, repeat: 2,
            onComplete: () => { obj.x = bx; }
        });
    }

    _flashWhite(textObj) {
        textObj.setTint(0xffffff);
        this.time.delayedCall(100, () => textObj.clearTint());
    }

    // ── DISPLAY UPDATES ────────────────────────────

    _updateAllDisplays() {
        const ps = this.playerState;
        const barY = 540;

        this.playerHpBar.clear();
        const hpRatio = Math.max(0, ps.hp / ps.maxHp);
        const hpColor = hpRatio > 0.5 ? 0x44cc44 : hpRatio > 0.25 ? 0xcccc44 : 0xcc4444;
        this.playerHpBar.fillStyle(hpColor, 1);
        this.playerHpBar.fillRoundedRect(80, barY, 300 * hpRatio, 24, 4);
        this.playerHpText.setText(`${Math.max(0, ps.hp)} / ${ps.maxHp}`);

        this.playerBlockBarGfx.clear();
        if (ps.block > 0) {
            const blockRatio = Math.min(1, ps.block / ps.maxHp);
            this.playerBlockBarGfx.fillStyle(0x4488ff, 1);
            this.playerBlockBarGfx.fillRoundedRect(80, barY + 34, 300 * blockRatio, 18, 4);
        }

        const playerStatuses = [];
        if (ps.bleed > 0) playerStatuses.push(`🩸${ps.bleed}`);
        if (ps.burn > 0) playerStatuses.push(`🔥${ps.burn}`);
        this.playerBlockText.setText(
            (ps.block > 0 ? `${ps.block}` : '') +
            (playerStatuses.length > 0 ? '  ' + playerStatuses.join(' ') : '')
        );

        this.goldLabel.setText(`🪙 ${ps.gold}`);
        this.goldTopLabel.setText(`🪙 ${ps.gold}`);
        this.deckLabel.setText(`덱 ${ps.symbolPool.length}장:`);
        this.deckIconsText.setText(ps.symbolPool.map(id => { const s = SYMBOL_DATA[id]; return s ? s.icon : '?'; }).join(''));

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
        if (this.autoScrollLog) this._scrollLogToBottom();
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
        const roll = Math.random();
        let pool;
        if (roll < 0.6) pool = RELIC_POOLS.common;
        else if (roll < 0.9) pool = RELIC_POOLS.uncommon;
        else pool = RELIC_POOLS.rare;

        const owned = this.playerState.relics || [];
        // Allow duplicates for stacking — filter only if ALL copies are max (no limit for now)
        let candidates = pool.slice();
        if (candidates.length === 0) candidates = Object.values(RELIC_DATA);
        if (candidates.length === 0) return;

        const relic = candidates[Phaser.Math.Between(0, candidates.length - 1)];
        this.playerState.relics.push(relic.id);

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

        // relic: vampiric_fang — heal on kill (stacking: 5hp per copy per kill)
        const vampCount = this._relicCount('vampiric_fang');
        if (vampCount > 0) {
            const deadCount = this.enemies.filter(e => e.hp <= 0).length;
            const healAmt = deadCount * 5 * vampCount;
            this.playerState.hp = Math.min(this.playerState.maxHp, this.playerState.hp + healAmt);
        }

        // relic: lucky_coin — bonus gold on win (stacking: +3 per copy)
        const coinCount = this._relicCount('lucky_coin');
        if (coinCount > 0) {
            const bonus = 3 * coinCount;
            this.playerState.gold += bonus;
        }

        // relic: soul_lantern — +2 maxHP per kill (stacking: +2 per copy)
        const lanternCount = this._relicCount('soul_lantern');
        if (lanternCount > 0) {
            const deadCount = this.enemies.filter(e => e.hp <= 0).length;
            const maxHpGain = deadCount * 2 * lanternCount;
            this.playerState.maxHp += maxHpGain;
            this.playerState.hp += maxHpGain; // also heal the gained amount
        }

        if (this.map) {
            if (this.encounterType === 'boss') {
                if (this.act >= MAP_CONFIG.acts.length - 1) {
                    this.scene.start('GameOverScene', { playerState: this.playerState, victory: true });
                } else {
                    this.playerState.currentFloor = 0;
                    this.playerState.visitedNodes = [];
                    this.scene.start('MapScene', { act: this.act + 1, playerState: this.playerState });
                }
            } else {
                this.scene.start('MapScene', { act: this.act, map: this.map, playerState: this.playerState });
            }
        } else {
            this.scene.start('ShopScene', { round: this.round, playerState: this.playerState, goldReward: goldReward });
        }
    }

    _onPlayerDeath() {
        // relic: phoenix_feather — revive once (remove one copy)
        if (this._relicCount('phoenix_feather') > 0) {
            const idx = this.playerState.relics.indexOf('phoenix_feather');
            this.playerState.relics.splice(idx, 1);
            this.playerState.hp = Math.floor(this.playerState.maxHp * 0.3);
            this._popText(640, 300, '🔥 불사조 부활!', '#ff8800', 32);
            this._updateAllDisplays();
            this.time.delayedCall(800, () => this._readyForSpin());
            return;
        }
        this.scene.start('GameOverScene', { round: this.round, act: this.act, playerState: this.playerState });
    }
}
