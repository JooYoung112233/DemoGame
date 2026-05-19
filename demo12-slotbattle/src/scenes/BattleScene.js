class BattleScene extends Phaser.Scene {
    constructor() {
        super('BattleScene');
    }

    init(data) {
        this.round = data.round || 1;
        this.playerState = data.playerState || {
            hp: 80, maxHp: 80, block: 0, gold: 0,
            symbolPool: ['sword', 'sword', 'shield', 'shield', 'potion', 'dagger', 'arrow', 'fire', 'coin', 'skull']
        };
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a1a');

        this.slotMachine = new SlotMachine(this);
        this.slotMachine.setSymbolPool(this.playerState.symbolPool);
        this.combatResolver = new CombatResolver();

        this.enemies = this._spawnEnemies();
        this.turnPhase = 'ready'; // ready, spinning, resolving, enemyTurn
        this.combatLog = [];

        this._createUI();
        this._createEnemyDisplay();
        this._createSlotDisplay();
        this._createPlayerHUD();
        this._updateAllDisplays();
    }

    _spawnEnemies() {
        const roundData = ROUND_ENEMIES[Math.min(this.round - 1, ROUND_ENEMIES.length - 1)];
        const enemies = [];
        for (let i = 0; i < roundData.count; i++) {
            const id = roundData.pool[Phaser.Math.Between(0, roundData.pool.length - 1)];
            const data = ENEMY_DATA[id];
            enemies.push({
                ...data,
                hp: data.hp,
                maxHp: data.hp,
                burn: 0, poison: 0,
                index: i
            });
        }
        return enemies;
    }

    _createUI() {
        const W = 1280;
        this.add.text(W / 2, 24, `라운드 ${this.round}`, {
            fontSize: '24px', fontFamily: 'monospace', color: '#ffffff'
        }).setOrigin(0.5);

        this.comboText = this.add.text(W / 2, 320, '', {
            fontSize: '28px', fontFamily: 'monospace', color: '#ffcc00',
            fontStyle: 'bold'
        }).setOrigin(0.5).setAlpha(0);

        this.logText = this.add.text(20, 540, '', {
            fontSize: '13px', fontFamily: 'monospace', color: '#aaaaaa',
            wordWrap: { width: 400 }, lineSpacing: 4
        });
    }

    _createEnemyDisplay() {
        const W = 1280;
        this.enemyContainers = [];
        const startX = W / 2 - ((this.enemies.length - 1) * 160) / 2;

        for (let i = 0; i < this.enemies.length; i++) {
            const enemy = this.enemies[i];
            const x = startX + i * 160;
            const y = 140;
            const container = this.add.container(x, y);

            const body = this.add.graphics();
            body.fillStyle(enemy.color, 1);
            body.fillRoundedRect(-40, -40, 80, 80, 12);
            container.add(body);

            const icon = this.add.text(0, -12, enemy.icon, {
                fontSize: '36px'
            }).setOrigin(0.5);
            container.add(icon);

            const nameText = this.add.text(0, 50, enemy.name, {
                fontSize: '14px', fontFamily: 'monospace', color: '#cccccc'
            }).setOrigin(0.5);
            container.add(nameText);

            const hpBg = this.add.graphics();
            hpBg.fillStyle(0x333333, 1);
            hpBg.fillRoundedRect(-40, 64, 80, 10, 3);
            container.add(hpBg);

            const hpBar = this.add.graphics();
            container.add(hpBar);

            const hpText = this.add.text(0, 68, '', {
                fontSize: '10px', fontFamily: 'monospace', color: '#ffffff'
            }).setOrigin(0.5);
            container.add(hpText);

            const statusText = this.add.text(0, -55, '', {
                fontSize: '12px', fontFamily: 'monospace', color: '#ffaa00'
            }).setOrigin(0.5);
            container.add(statusText);

            this.enemyContainers.push({ container, hpBar, hpText, body, statusText, icon });
        }
    }

    _createSlotDisplay() {
        const W = 1280, slotY = 420;
        this.slotBg = this.add.graphics();
        this.slotBg.fillStyle(0x1a1a35, 1);
        this.slotBg.lineStyle(2, 0x4466aa, 1);
        this.slotBg.fillRoundedRect(W / 2 - 240, slotY - 55, 480, 110, 16);
        this.slotBg.strokeRoundedRect(W / 2 - 240, slotY - 55, 480, 110, 16);

        this.slotMachine.reelContainers = [];
        for (let i = 0; i < 3; i++) {
            const x = W / 2 - 130 + i * 130;
            const reelBg = this.add.graphics();
            reelBg.fillStyle(0x0a0a20, 1);
            reelBg.fillRoundedRect(x - 50, slotY - 40, 100, 80, 8);

            const container = this.add.container(x, slotY);
            const iconText = this.add.text(0, -10, '❓', { fontSize: '32px' }).setOrigin(0.5);
            const nameLabel = this.add.text(0, 22, '???', {
                fontSize: '12px', fontFamily: 'monospace', color: '#888888'
            }).setOrigin(0.5);
            container.add(iconText);
            container.add(nameLabel);
            this.slotMachine.reelContainers.push(container);
        }

        this.spinBtn = this.add.text(W / 2, 530, '[ SPIN! ]', {
            fontSize: '28px', fontFamily: 'monospace', color: '#ffcc00',
            fontStyle: 'bold', padding: { x: 20, y: 10 },
            backgroundColor: '#2a2a1a'
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        this.spinBtn.on('pointerover', () => this.spinBtn.setColor('#ffffff'));
        this.spinBtn.on('pointerout', () => this.spinBtn.setColor('#ffcc00'));
        this.spinBtn.on('pointerdown', () => this._onSpin());
    }

    _createPlayerHUD() {
        const y = 670;
        this.hpLabel = this.add.text(20, y, '', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ff4444'
        });
        this.blockLabel = this.add.text(280, y, '', {
            fontSize: '16px', fontFamily: 'monospace', color: '#4488ff'
        });
        this.goldLabel = this.add.text(480, y, '', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc00'
        });
        this.deckLabel = this.add.text(680, y, '', {
            fontSize: '14px', fontFamily: 'monospace', color: '#888888'
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
        const { log, actions, hasCombo } = this.combatResolver.resolve(
            results, this.playerState, this.enemies, this.slotMachine
        );

        if (hasCombo) {
            const comboAction = actions.find(a => a.type === 'combo');
            this.comboText.setText(`✨ ${comboAction.name}! ✨`);
            this.comboText.setAlpha(1);
            this.tweens.add({
                targets: this.comboText,
                scaleX: 1.3, scaleY: 1.3,
                duration: 200,
                yoyo: true,
            });

            this.cameras.main.shake(200, 0.008);
        }

        this._showDamagePopups(log);
        this._updateAllDisplays();
        this._appendLog(log);

        const allDead = this.enemies.every(e => e.hp <= 0);
        if (allDead) {
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

        const allDead = this.enemies.every(e => e.hp <= 0);
        if (allDead) {
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
                    this._floatingText(ec.container.x, ec.container.y - 50,
                        `-${entry.value}`, '#ff4444');

                    this.tweens.add({
                        targets: ec.container,
                        x: ec.container.x + 8,
                        duration: 50,
                        yoyo: true,
                        repeat: 2
                    });
                }
            }
            if (entry.type === 'heal') {
                this._floatingText(150, 640, `+${entry.value} HP`, '#44ff88');
            }
            if (entry.type === 'gold') {
                this._floatingText(500, 640, `+${entry.value} G`, '#ffcc00');
            }
            if (entry.type === 'block') {
                this._floatingText(300, 640, `+${entry.value} 🛡️`, '#4488ff');
            }
        }
    }

    _showPlayerHitEffect(log) {
        for (const entry of log) {
            if (entry.type === 'playerHit') {
                this._floatingText(150, 620, `-${entry.value}`, '#ff2222');
                this.cameras.main.shake(100, 0.005);
            }
            if (entry.type === 'blocked') {
                this._floatingText(300, 620, `방어 ${entry.value}`, '#4488ff');
            }
        }
    }

    _floatingText(x, y, text, color) {
        const t = this.add.text(x, y, text, {
            fontSize: '20px', fontFamily: 'monospace', color: color, fontStyle: 'bold'
        }).setOrigin(0.5);

        this.tweens.add({
            targets: t,
            y: y - 40,
            alpha: 0,
            duration: 800,
            ease: 'Power2',
            onComplete: () => t.destroy()
        });
    }

    _appendLog(log) {
        const lines = [];
        for (const entry of log) {
            if (entry.type === 'damage') lines.push(`${entry.target}에게 ${entry.value} 데미지${entry.name ? ' (' + entry.name + ')' : ''}`);
            if (entry.type === 'heal') lines.push(`HP ${entry.value} 회복`);
            if (entry.type === 'block') lines.push(`방어 ${entry.value} 획득`);
            if (entry.type === 'gold') lines.push(`골드 ${entry.value} 획득`);
            if (entry.type === 'selfDamage') lines.push(`저주! 자해 ${entry.value}`);
            if (entry.type === 'playerHit') lines.push(`${entry.from}의 공격! ${entry.value} 피해`);
            if (entry.type === 'blocked') lines.push(`${entry.from}의 공격 ${entry.value} 방어`);
            if (entry.type === 'dot') lines.push(`${entry.target} ${entry.dotType} ${entry.value} 피해`);
            if (entry.type === 'burn') lines.push(`${entry.target}에 화상 ${entry.value} 부여`);
            if (entry.type === 'poison') lines.push(`${entry.target}에 독 ${entry.value} 부여`);
        }
        this.combatLog = this.combatLog.concat(lines);
        if (this.combatLog.length > 12) this.combatLog = this.combatLog.slice(-12);
        this.logText.setText(this.combatLog.join('\n'));
    }

    _updateAllDisplays() {
        const ps = this.playerState;
        this.hpLabel.setText(`❤️ HP: ${ps.hp}/${ps.maxHp}`);
        this.blockLabel.setText(`🛡️ 방어: ${ps.block}`);
        this.goldLabel.setText(`🪙 골드: ${ps.gold}`);
        this.deckLabel.setText(`덱: ${ps.symbolPool.length}심볼`);

        for (let i = 0; i < this.enemies.length; i++) {
            const enemy = this.enemies[i];
            const ec = this.enemyContainers[i];
            if (!ec) continue;

            ec.hpBar.clear();
            const ratio = Math.max(0, enemy.hp / enemy.maxHp);
            const barColor = ratio > 0.5 ? 0x44cc44 : ratio > 0.25 ? 0xcccc44 : 0xcc4444;
            ec.hpBar.fillStyle(barColor, 1);
            ec.hpBar.fillRoundedRect(-40, 64, 80 * ratio, 10, 3);
            ec.hpText.setText(`${Math.max(0, enemy.hp)}/${enemy.maxHp}`);

            const statuses = [];
            if (enemy.burn > 0) statuses.push(`🔥${enemy.burn}`);
            if (enemy.poison > 0) statuses.push(`☠️${enemy.poison}`);
            ec.statusText.setText(statuses.join(' '));

            if (enemy.hp <= 0) {
                ec.container.setAlpha(0.3);
            }
        }
    }

    _onRoundWin() {
        this.scene.start('RewardScene', {
            round: this.round,
            playerState: this.playerState
        });
    }

    _onPlayerDeath() {
        this.scene.start('GameOverScene', {
            round: this.round,
            playerState: this.playerState
        });
    }
}
