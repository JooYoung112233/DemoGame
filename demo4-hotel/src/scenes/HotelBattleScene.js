class HotelBattleScene extends Phaser.Scene {
    constructor() {
        super('HotelBattleScene');
    }

    init(data) {
        this.runState = data.runState;
        this.floorManager = data.floorManager || new FloorManager();
    }

    create() {
        this.floorManager.nextFloor();
        const floorData = this.floorManager.getFloorEnemies();

        if (floorData.type === 'event' || floorData.type === 'shop') {
            this.scene.start('HotelEventScene', {
                runState: this.runState,
                floorManager: this.floorManager,
                eventType: floorData.type
            });
            return;
        }

        this.cameras.main.setBackgroundColor('#1a1020');

        this.player = new Player(this.runState);
        if (this.runState.hasShield && this.floorManager.currentFloor % 3 === 1) {
            this.player.shieldActive = true;
        }

        this.enemies = [];
        this.boss = null;
        this.currentEnemyIndex = 0;
        this.turnCount = 0;
        this.floorTime = 0;
        this.battleOver = false;
        this.isProcessing = false;
        this.battleLog = [];

        this.isBossFloor = floorData.type === 'boss';

        if (this.isBossFloor) {
            this.boss = new Boss(floorData.scale);
        } else {
            const scale = floorData.scale || 1;
            floorData.enemies.forEach(data => {
                const enemy = new Enemy(data, scale, data.isElite);
                this.enemies.push(enemy);
            });
        }

        this.drawUI();
        this.showFloorAnnounce();
    }

    drawUI() {
        this.drawBackground();

        this.floorText = this.add.text(20, 15, this.floorManager.currentFloor + 'F', {
            fontSize: '22px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#ffffff'
        }).setDepth(100);

        this.floorTypeText = this.add.text(20, 42, this.floorManager.getFloorLabel(), {
            fontSize: '13px', fontFamily: 'monospace',
            color: this.floorManager.getFloorColor()
        }).setDepth(100);

        this.coinText = this.add.text(1260, 15, '코인 ' + (this.runState.coins || 0), {
            fontSize: '16px', fontFamily: 'monospace',
            color: '#ffcc44'
        }).setOrigin(1, 0).setDepth(100);

        this.turnText = this.add.text(640, 15, '턴 1', {
            fontSize: '16px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#ffffff'
        }).setOrigin(0.5, 0).setDepth(100);

        this.drawPlayerPanel();
        this.drawEnemyPanel();
        this.drawActionButtons();
        this.drawBattleLog();
    }

    drawBackground() {
        const bg = this.add.graphics().setDepth(0);
        const floor = this.floorManager.currentFloor;
        const hue = (floor * 30) % 360;
        const baseColor = Phaser.Display.Color.HSLToColor(hue / 360, 0.15, 0.08).color;

        bg.fillStyle(baseColor, 1);
        bg.fillRect(0, 0, 1280, 720);

        bg.lineStyle(1, 0xffffff, 0.03);
        for (let x = 0; x < 1280; x += 40) bg.lineBetween(x, 0, x, 720);
        for (let y = 0; y < 720; y += 40) bg.lineBetween(0, y, 1280, y);
    }

    drawPlayerPanel() {
        const px = 200, py = 300;
        this.playerPanel = this.add.graphics().setDepth(10);
        this.playerPanel.fillStyle(0x112211, 0.8);
        this.playerPanel.fillRoundedRect(px - 130, py - 120, 260, 240, 12);
        this.playerPanel.lineStyle(2, 0x44ff88, 0.5);
        this.playerPanel.strokeRoundedRect(px - 130, py - 120, 260, 240, 12);

        this.playerSprite = this.add.graphics().setDepth(11);
        this.drawPlayerSprite(px, py - 40);

        this.playerNameText = this.add.text(px, py + 30, this.runState.weapon.name, {
            fontSize: '18px', fontFamily: 'monospace', fontStyle: 'bold',
            color: this.runState.weapon.color
        }).setOrigin(0.5).setDepth(11);

        this.playerHpBar = this.add.graphics().setDepth(11);
        this.playerHpText = this.add.text(px, py + 55, '', {
            fontSize: '13px', fontFamily: 'monospace', color: '#ffffff'
        }).setOrigin(0.5).setDepth(11);

        this.playerStaminaBar = this.add.graphics().setDepth(11);
        this.playerStaminaText = this.add.text(px, py + 80, '', {
            fontSize: '13px', fontFamily: 'monospace', color: '#88ccff'
        }).setOrigin(0.5).setDepth(11);

        this.playerStatusText = this.add.text(px, py + 100, '', {
            fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa'
        }).setOrigin(0.5).setDepth(11);

        this.updatePlayerPanel();
    }

    drawPlayerSprite(cx, cy) {
        this.playerSprite.clear();
        this.playerSprite.fillStyle(0x44ff88, 1);
        this.playerSprite.fillRect(cx - 14, cy - 14, 28, 28);
        this.playerSprite.fillStyle(0xffffff, 1);
        this.playerSprite.fillCircle(cx, cy - 22, 8);
        this.playerSprite.fillStyle(0x000000, 1);
        this.playerSprite.fillCircle(cx + 2, cy - 23, 2);

        const wColor = Phaser.Display.Color.HexStringToColor(this.runState.weapon.color).color;
        this.playerSprite.fillStyle(wColor, 1);
        this.playerSprite.fillRect(cx + 14, cy - 4, 14, 4);
    }

    drawEnemyPanel() {
        const ex = 1080, ey = 300;
        this.enemyPanel = this.add.graphics().setDepth(10);
        this.enemySprite = this.add.graphics().setDepth(11);
        this.enemyNameText = this.add.text(ex, ey + 30, '', {
            fontSize: '18px', fontFamily: 'monospace', fontStyle: 'bold', color: '#ffffff'
        }).setOrigin(0.5).setDepth(11);

        this.enemyHpBar = this.add.graphics().setDepth(11);
        this.enemyHpText = this.add.text(ex, ey + 55, '', {
            fontSize: '13px', fontFamily: 'monospace', color: '#ffffff'
        }).setOrigin(0.5).setDepth(11);

        this.enemyStatusText = this.add.text(ex, ey + 75, '', {
            fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa'
        }).setOrigin(0.5).setDepth(11);

        this.enemyCountText = this.add.text(ex, ey - 135, '', {
            fontSize: '12px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5).setDepth(11);

        this.updateEnemyPanel();
    }

    drawActionButtons() {
        this.actionButtons = [];
        const actions = this.player.weapon.actions;
        const startX = 200;
        const startY = 560;
        const btnW = 180;
        const btnH = 55;
        const gap = 12;

        actions.forEach((action, i) => {
            const bx = startX + (i % 5) * (btnW + gap);
            const by = startY + Math.floor(i / 5) * (btnH + gap);

            const bg = this.add.graphics().setDepth(50);
            const hitRate = this.player.getEffectiveHitRate(action);
            const cost = this.player.getEffectiveStaminaCost(action);

            const nameText = this.add.text(bx + btnW / 2, by + 14, action.name, {
                fontSize: '16px', fontFamily: 'monospace', fontStyle: 'bold',
                color: '#ffffff'
            }).setOrigin(0.5).setDepth(51);

            const infoText = this.add.text(bx + btnW / 2, by + 34, '', {
                fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa'
            }).setOrigin(0.5).setDepth(51);

            const hitArea = this.add.rectangle(bx + btnW / 2, by + btnH / 2, btnW, btnH, 0x000000, 0)
                .setInteractive({ useHandCursor: true }).setDepth(52);

            hitArea.on('pointerdown', () => this.onActionSelected(action.id));
            hitArea.on('pointerover', () => {
                if (!this.isProcessing) bg.clear().fillStyle(0x334433, 1).fillRoundedRect(bx, by, btnW, btnH, 8).lineStyle(2, 0x66ff99, 0.8).strokeRoundedRect(bx, by, btnW, btnH, 8);
            });
            hitArea.on('pointerout', () => {
                this.refreshButton(i);
            });

            this.actionButtons.push({ bg, nameText, infoText, hitArea, bx, by, btnW, btnH, action });
        });

        this.refreshAllButtons();
    }

    refreshButton(index) {
        const btn = this.actionButtons[index];
        const action = btn.action;
        const hitRate = this.player.getEffectiveHitRate(action);
        const cost = this.player.getEffectiveStaminaCost(action);
        const canUse = this.player.canUseAction(action) && !this.isProcessing;

        btn.bg.clear();
        const bgColor = canUse ? 0x1a2a1a : 0x1a1a1a;
        const borderColor = canUse ? 0x44ff88 : 0x333333;

        btn.bg.fillStyle(bgColor, 1);
        btn.bg.fillRoundedRect(btn.bx, btn.by, btn.btnW, btn.btnH, 8);
        btn.bg.lineStyle(2, borderColor, canUse ? 0.6 : 0.3);
        btn.bg.strokeRoundedRect(btn.bx, btn.by, btn.btnW, btn.btnH, 8);

        btn.nameText.setColor(canUse ? '#ffffff' : '#555555');

        let infoStr = Math.floor(hitRate * 100) + '%';
        if (action.dmgMin > 0) {
            const dmgMin = action.dmgMin + this.player.bonusDmg;
            const dmgMax = action.dmgMax + this.player.bonusDmg;
            infoStr += ' | ' + dmgMin + '~' + dmgMax;
        }
        infoStr += ' | SP ' + cost;
        btn.infoText.setText(infoStr);
        btn.infoText.setColor(canUse ? '#aaaaaa' : '#444444');
    }

    refreshAllButtons() {
        for (let i = 0; i < this.actionButtons.length; i++) {
            this.refreshButton(i);
        }
    }

    drawBattleLog() {
        this.logBg = this.add.graphics().setDepth(40);
        this.logBg.fillStyle(0x0a0a1a, 0.9);
        this.logBg.fillRoundedRect(390, 80, 500, 440, 8);
        this.logBg.lineStyle(1, 0x333355, 0.5);
        this.logBg.strokeRoundedRect(390, 80, 500, 440, 8);

        this.add.text(640, 90, '전투 로그', {
            fontSize: '13px', fontFamily: 'monospace', color: '#555566'
        }).setOrigin(0.5).setDepth(41);

        this.logTexts = [];
        const maxLines = 16;
        for (let i = 0; i < maxLines; i++) {
            const t = this.add.text(410, 115 + i * 24, '', {
                fontSize: '13px', fontFamily: 'monospace', color: '#aaaaaa',
                wordWrap: { width: 460 }
            }).setDepth(41);
            this.logTexts.push(t);
        }

        this.vsText = this.add.text(640, 300, 'VS', {
            fontSize: '40px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#ff4466'
        }).setOrigin(0.5).setDepth(12).setAlpha(0.15);
    }

    addLog(text, color) {
        this.battleLog.push({ text, color: color || '#aaaaaa' });
        this.updateLogDisplay();
    }

    updateLogDisplay() {
        const maxLines = this.logTexts.length;
        const startIdx = Math.max(0, this.battleLog.length - maxLines);

        for (let i = 0; i < maxLines; i++) {
            const logIdx = startIdx + i;
            if (logIdx < this.battleLog.length) {
                this.logTexts[i].setText(this.battleLog[logIdx].text);
                this.logTexts[i].setColor(this.battleLog[logIdx].color);
            } else {
                this.logTexts[i].setText('');
            }
        }
    }

    updatePlayerPanel() {
        const px = 200, py = 300;
        const hpRatio = this.player.hp / this.player.maxHp;
        const stRatio = this.player.stamina / this.player.maxStamina;

        this.playerHpBar.clear();
        const barW = 200, barH = 10, barX = px - barW / 2, barY = py + 45;
        this.playerHpBar.fillStyle(0x333333, 1);
        this.playerHpBar.fillRoundedRect(barX, barY, barW, barH, 3);
        this.playerHpBar.fillStyle(hpRatio > 0.5 ? 0x44ff44 : hpRatio > 0.25 ? 0xffaa44 : 0xff4444, 1);
        this.playerHpBar.fillRoundedRect(barX, barY, barW * hpRatio, barH, 3);

        this.playerHpText.setText('HP ' + this.player.hp + '/' + this.player.maxHp);

        this.playerStaminaBar.clear();
        const stY = barY + 22;
        this.playerStaminaBar.fillStyle(0x333333, 1);
        this.playerStaminaBar.fillRoundedRect(barX, stY, barW, barH, 3);
        this.playerStaminaBar.fillStyle(stRatio > 0.3 ? 0x4488ff : 0xff8844, 1);
        this.playerStaminaBar.fillRoundedRect(barX, stY, barW * stRatio, barH, 3);

        this.playerStaminaText.setText('SP ' + this.player.stamina + '/' + this.player.maxStamina);

        let status = '';
        if (this.player.focused) status += '[집중] ';
        if (this.player.defending) status += '[방어] ';
        if (this.player.shieldActive) status += '[보호막] ';
        this.playerStatusText.setText(status);
    }

    updateEnemyPanel() {
        const ex = 1080, ey = 300;
        const target = this.getCurrentTarget();

        this.enemyPanel.clear();
        this.enemySprite.clear();

        if (!target) return;

        const isB = target instanceof Boss;
        this.enemyPanel.fillStyle(isB ? 0x220a0a : 0x221111, 0.8);
        this.enemyPanel.fillRoundedRect(ex - 130, ey - 120, 260, 240, 12);
        this.enemyPanel.lineStyle(2, isB ? 0xff4466 : 0xff6644, 0.5);
        this.enemyPanel.strokeRoundedRect(ex - 130, ey - 120, 260, 240, 12);

        this.enemySprite.fillStyle(target.color, 1);
        if (isB) {
            this.enemySprite.fillRect(ex - 20, ey - 60, 40, 40);
            this.enemySprite.fillStyle(0xffcc00, 1);
            this.enemySprite.fillCircle(ex, ey - 45, 6);
        } else {
            this.enemySprite.fillCircle(ex, ey - 40, 16);
            if (target.isElite) {
                this.enemySprite.lineStyle(2, 0xffcc00, 1);
                this.enemySprite.strokeCircle(ex, ey - 40, 20);
            }
        }
        this.enemySprite.fillStyle(0xff0000, 1);
        this.enemySprite.fillCircle(ex - 4, ey - 44, 2);
        this.enemySprite.fillCircle(ex + 4, ey - 44, 2);

        const prefix = target.isElite ? '[엘리트] ' : '';
        this.enemyNameText.setText(prefix + target.name);
        this.enemyNameText.setColor('#' + target.color.toString(16).padStart(6, '0'));

        const hpRatio = target.hp / target.maxHp;
        const barW = 200, barH = 10, barX = ex - barW / 2, barY = ey + 45;
        this.enemyHpBar.clear();
        this.enemyHpBar.fillStyle(0x333333, 1);
        this.enemyHpBar.fillRoundedRect(barX, barY, barW, barH, 3);
        this.enemyHpBar.fillStyle(hpRatio > 0.5 ? 0xff4444 : hpRatio > 0.25 ? 0xff8844 : 0xff2222, 1);
        this.enemyHpBar.fillRoundedRect(barX, barY, barW * hpRatio, barH, 3);

        this.enemyHpText.setText('HP ' + target.hp + '/' + target.maxHp);

        let status = '';
        if (target.defending) status += '[방어] ';
        if (target.dodging) status += '[회피] ';
        if (target.poisonTurnsLeft > 0) status += '[독 ' + target.poisonTurnsLeft + '턴] ';
        this.enemyStatusText.setText(status);

        if (this.isBossFloor) {
            this.enemyCountText.setText('보스');
        } else {
            const alive = this.enemies.filter(e => e.alive).length;
            this.enemyCountText.setText('적 ' + (this.currentEnemyIndex + 1) + '/' + alive);
        }
    }

    getCurrentTarget() {
        if (this.isBossFloor) return this.boss;
        const alive = this.enemies.filter(e => e.alive);
        if (alive.length === 0) return null;
        return alive[0];
    }

    showFloorAnnounce() {
        const floor = this.floorManager.currentFloor;
        const color = this.floorManager.getFloorColor();
        const label = this.floorManager.getFloorLabel();

        const floorAnnounce = this.add.text(640, 300, floor + 'F', {
            fontSize: '64px', fontFamily: 'monospace', fontStyle: 'bold', color: color
        }).setOrigin(0.5).setAlpha(0).setDepth(300);

        const typeAnnounce = this.add.text(640, 360, label, {
            fontSize: '20px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5).setAlpha(0).setDepth(300);

        this.tweens.add({
            targets: [floorAnnounce, typeAnnounce],
            alpha: 1, duration: 300, hold: 800, yoyo: true,
            onComplete: () => {
                floorAnnounce.destroy();
                typeAnnounce.destroy();
                this.addLog('--- ' + floor + 'F ' + label + ' ---', '#888888');
                const target = this.getCurrentTarget();
                if (target) {
                    this.addLog(target.name + ' 등장!', '#ff6644');
                }
            }
        });
    }

    onActionSelected(actionId) {
        if (this.isProcessing || this.battleOver) return;

        const action = this.player.getAction(actionId);
        if (!this.player.canUseAction(action)) {
            this.addLog('스태미나가 부족합니다!', '#ff4444');
            return;
        }

        this.isProcessing = true;
        this.refreshAllButtons();

        this.turnCount++;
        this.turnText.setText('턴 ' + this.turnCount);
        this.player.onTurnStart();

        this.time.delayedCall(100, () => this.executePlayerTurn(actionId));
    }

    executePlayerTurn(actionId) {
        const result = this.player.executeAction(actionId);
        const target = this.getCurrentTarget();
        if (!target) return;

        let logText = '▶ ' + result.actionName;

        if (result.special === 'rest') {
            logText += ' → SP ' + result.staminaRecovered + ' 회복!';
            this.addLog(logText, '#66ddff');
        } else if (result.special === 'defend') {
            logText += ' → 방어 태세!';
            this.addLog(logText, '#88aaff');
        } else if (result.special === 'dodge') {
            if (result.success) {
                logText += ' → 회피 준비! (' + result.roll + '/' + result.needed + ')';
                this.addLog(logText, '#cc88ff');
            } else {
                logText += ' → 실패! (' + result.roll + '/' + result.needed + ')';
                this.addLog(logText, '#ff4444');
            }
        } else if (result.special === 'focus') {
            if (result.success) {
                logText += ' → 집중 성공! 다음 공격 강화 (' + result.roll + '/' + result.needed + ')';
                this.addLog(logText, '#aaddff');
            } else {
                logText += ' → 집중 실패! (' + result.roll + '/' + result.needed + ')';
                this.addLog(logText, '#ff4444');
            }
        } else {
            if (result.success) {
                const actualDmg = target.takeDamage(result.damage);
                this.runState.totalDamage += actualDmg;

                if (actualDmg === 0 && target.dodging) {
                    logText += ' → ' + target.name + ' 회피! (' + result.roll + '/' + result.needed + ')';
                    this.addLog(logText, '#ffaa44');
                } else {
                    const critStr = result.isCrit ? ' 치명타!' : '';
                    const reducedStr = target.defending ? ' (방어 감소)' : '';
                    logText += ' → ' + actualDmg + ' 피해!' + critStr + reducedStr + ' (' + result.roll + '/' + result.needed + ')';
                    this.addLog(logText, result.isCrit ? '#ffff44' : '#44ff88');
                }

                if (result.healing > 0) {
                    this.addLog('  흡혈 +' + result.healing + ' HP', '#ff88aa');
                }

                if (result.poisonApplied && target.alive) {
                    target.applyPoison(this.player.poisonDmg, this.player.poisonDuration);
                    this.addLog('  독 부여! (' + this.player.poisonDmg + ' x ' + this.player.poisonDuration + '턴)', '#44ff00');
                }

                if (!target.alive) {
                    this.onTargetKilled(target);
                    this.updatePlayerPanel();
                    this.updateEnemyPanel();
                    this.refreshAllButtons();
                    return;
                }
            } else {
                logText += ' → 빗나감! (' + result.roll + '/' + result.needed + ')';
                this.addLog(logText, '#ff4444');
            }
        }

        this.updatePlayerPanel();
        this.updateEnemyPanel();

        this.time.delayedCall(600, () => this.executeEnemyTurn());
    }

    executeEnemyTurn() {
        const target = this.getCurrentTarget();
        if (!target || !target.alive) {
            this.isProcessing = false;
            this.refreshAllButtons();
            return;
        }

        target.onTurnStart();
        const scale = this.floorManager.getScale();

        let eResult;
        if (target instanceof Boss) {
            eResult = target.executeAction();
        } else {
            eResult = target.executeAction(scale);
        }

        let logText = '◀ ' + target.name + ': ' + eResult.actionName;

        if (eResult.special === 'defend') {
            logText += ' → 방어 태세!';
            this.addLog(logText, '#8888aa');
        } else if (eResult.special === 'dodge') {
            logText += ' → 회피 준비!';
            this.addLog(logText, '#8888cc');
        } else if (eResult.special === 'idle') {
            logText += ' → 대기';
            this.addLog(logText, '#666666');
        } else if (eResult.special === 'debuff') {
            this.player.turnDebuffs[eResult.debuffType] = true;
            logText += ' → 명중률 감소!';
            this.addLog(logText, '#ff88ff');
        } else if (eResult.special === 'summon') {
            logText += ' → 잡몹 소환!';
            this.addLog(logText, '#ff8844');
            this.spawnBossAdds();
        } else if (eResult.special === 'heal' && eResult.healing) {
            logText += ' → 수리 +' + eResult.healing + ' HP';
            this.addLog(logText, '#44ff44');
        } else if (eResult.damage > 0) {
            const actualDmg = this.player.takeDamage(eResult.damage);

            if (actualDmg === -1) {
                logText += ' → 보호막 흡수!';
                this.addLog(logText, '#44aaff');
            } else if (actualDmg === 0) {
                logText += ' → 회피 성공!';
                this.addLog(logText, '#cc88ff');

                if (this.player.dodgeCounter) {
                    const counterDmg = this.player.dodgeCounterMin +
                        Math.floor(Math.random() * (this.player.dodgeCounterMax - this.player.dodgeCounterMin + 1));
                    const actualCounter = target.takeDamage(counterDmg);
                    this.addLog('  회피 반격! ' + actualCounter + ' 피해!', '#ff8800');
                    this.runState.totalDamage += actualCounter;

                    if (!target.alive) {
                        this.onTargetKilled(target);
                        this.updatePlayerPanel();
                        this.updateEnemyPanel();
                        this.refreshAllButtons();
                        return;
                    }
                }
            } else {
                const reducedStr = this.player.defending ? ' (방어 감소)' : '';
                logText += ' → ' + actualDmg + ' 피해!' + reducedStr;
                this.addLog(logText, '#ff4466');

                if (this.player.defending && this.player.counterRate > 0 && Math.random() < this.player.counterRate) {
                    const cDmg = this.player.counterDmgMin +
                        Math.floor(Math.random() * (this.player.counterDmgMax - this.player.counterDmgMin + 1));
                    const actualC = target.takeDamage(cDmg);
                    this.addLog('  반격! ' + actualC + ' 피해!', '#ff8844');
                    this.runState.totalDamage += actualC;

                    if (!target.alive) {
                        this.onTargetKilled(target);
                        this.updatePlayerPanel();
                        this.updateEnemyPanel();
                        this.refreshAllButtons();
                        return;
                    }
                }
            }
        } else {
            logText += ' → 아무 일도 없었다';
            this.addLog(logText, '#666666');
        }

        const poisonDmg = target.tickPoison();
        if (poisonDmg > 0) {
            this.addLog('  독 피해 ' + poisonDmg + '!', '#44ff00');
            if (!target.alive) {
                this.onTargetKilled(target);
                this.updatePlayerPanel();
                this.updateEnemyPanel();
                this.refreshAllButtons();
                return;
            }
        }

        if (!this.player.alive) {
            this.endBattle(false);
            return;
        }

        this.player.turnDebuffs = {};

        this.updatePlayerPanel();
        this.updateEnemyPanel();

        this.time.delayedCall(300, () => {
            this.isProcessing = false;
            this.refreshAllButtons();
        });
    }

    spawnBossAdds() {
        const adds = this.floorManager.getBossAdds();
        adds.forEach(data => {
            const enemy = new Enemy(data, this.floorManager.getScale(), false);
            this.enemies.push(enemy);
        });
    }

    onTargetKilled(target) {
        this.runState.totalKills++;
        this.runState.coins += (target.coinValue || 1);
        this.coinText.setText('코인 ' + this.runState.coins);

        if (this.player.onKillHeal > 0) {
            this.player.heal(this.player.onKillHeal);
            this.addLog('  처치 회복 +' + this.player.onKillHeal + ' HP!', '#88ff88');
        }

        this.addLog(target.name + ' 처치!', '#ffcc44');

        if (this.isBossFloor && target instanceof Boss) {
            this.time.delayedCall(1000, () => this.endBattle(true));
            return;
        }

        const alive = this.enemies.filter(e => e.alive);
        if (alive.length === 0) {
            if (this.isBossFloor && this.boss && this.boss.alive) {
                this.addLog('잡몹 정리 완료!', '#888888');
                this.updateEnemyPanel();
                this.time.delayedCall(500, () => {
                    this.isProcessing = false;
                    this.refreshAllButtons();
                });
            } else {
                this.time.delayedCall(800, () => this.endBattle(true));
            }
            return;
        }

        this.addLog(alive[0].name + ' 등장!', '#ff6644');
        this.updateEnemyPanel();
        this.time.delayedCall(500, () => {
            this.isProcessing = false;
            this.refreshAllButtons();
        });
    }

    endBattle(victory) {
        if (this.battleOver) return;
        this.battleOver = true;

        this.player.syncToRunState();
        this.runState.totalTime += this.turnCount * 3000;

        if (!victory) {
            this.addLog('=== 사망 ===', '#ff4466');
            this.time.delayedCall(2000, () => {
                this.scene.start('HotelResultScene', {
                    runState: this.runState,
                    floorManager: this.floorManager,
                    victory: false
                });
            });
            return;
        }

        if (this.isBossFloor) {
            this.addLog('=== 보스 클리어! ===', '#44ff88');
            this.time.delayedCall(2000, () => {
                this.scene.start('HotelResultScene', {
                    runState: this.runState,
                    floorManager: this.floorManager,
                    victory: true
                });
            });
            return;
        }

        this.addLog('=== 전투 승리! ===', '#44ff88');
        this.time.delayedCall(1200, () => {
            this.scene.start('HotelUpgradeScene', {
                runState: this.runState,
                floorManager: this.floorManager
            });
        });
    }
}
