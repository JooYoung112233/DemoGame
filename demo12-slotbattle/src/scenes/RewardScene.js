class RewardScene extends Phaser.Scene {
    constructor() {
        super('RewardScene');
    }

    init(data) {
        this.round = data.round;
        this.playerState = data.playerState;
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a1a');

        this.add.text(W / 2, 40, `라운드 ${this.round} 클리어!`, {
            fontSize: '32px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
        }).setOrigin(0.5);

        if (this.round >= 10) {
            this.add.text(W / 2, 120, '🐉 드래곤 처치! 승리! 🐉', {
                fontSize: '36px', fontFamily: 'monospace', color: '#ffcc00', fontStyle: 'bold'
            }).setOrigin(0.5);

            this.add.text(W / 2, 200, `최종 HP: ${this.playerState.hp}/${this.playerState.maxHp}  |  골드: ${this.playerState.gold}`, {
                fontSize: '18px', fontFamily: 'monospace', color: '#aaaaaa'
            }).setOrigin(0.5);

            const retryBtn = this.add.text(W / 2, 400, '[ 처음부터 다시 ]', {
                fontSize: '24px', fontFamily: 'monospace', color: '#ffcc00',
                backgroundColor: '#2a2a1a', padding: { x: 20, y: 10 }
            }).setOrigin(0.5).setInteractive({ useHandCursor: true });
            retryBtn.on('pointerover', () => retryBtn.setColor('#ffffff'));
            retryBtn.on('pointerout', () => retryBtn.setColor('#ffcc00'));
            retryBtn.on('pointerdown', () => this.scene.start('TitleScene'));
            return;
        }

        this.add.text(W / 2, 90, '심볼을 선택하여 덱에 추가하세요', {
            fontSize: '16px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);

        const choices = this._generateChoices();
        this._displayChoices(choices);
        this._displayRemoveOption();
        this._displaySkipButton();
        this._displayCurrentDeck();
    }

    _generateChoices() {
        const available = Object.keys(SYMBOL_DATA).filter(id => id !== 'skull');
        const picks = [];
        const shuffled = Phaser.Utils.Array.Shuffle(available.slice());
        for (let i = 0; i < 3 && i < shuffled.length; i++) {
            picks.push(shuffled[i]);
        }
        return picks;
    }

    _displayChoices(choices) {
        const W = 1280;
        const startX = W / 2 - (choices.length - 1) * 160 / 2;

        for (let i = 0; i < choices.length; i++) {
            const sym = SYMBOL_DATA[choices[i]];
            const x = startX + i * 160;
            const y = 240;

            const bg = this.add.graphics();
            bg.fillStyle(0x1a1a35, 1);
            bg.lineStyle(2, sym.color, 0.8);
            bg.fillRoundedRect(x - 65, y - 70, 130, 180, 12);
            bg.strokeRoundedRect(x - 65, y - 70, 130, 180, 12);

            this.add.text(x, y - 35, sym.icon, { fontSize: '40px' }).setOrigin(0.5);
            this.add.text(x, y + 10, sym.name, {
                fontSize: '18px', fontFamily: 'monospace', color: '#ffffff'
            }).setOrigin(0.5);
            this.add.text(x, y + 35, sym.desc, {
                fontSize: '11px', fontFamily: 'monospace', color: '#999999',
                wordWrap: { width: 110 }, align: 'center'
            }).setOrigin(0.5);

            const effectText = this._effectToString(sym.effect);
            this.add.text(x, y + 65, effectText, {
                fontSize: '12px', fontFamily: 'monospace', color: '#44ff88'
            }).setOrigin(0.5);

            const hitArea = this.add.rectangle(x, y + 15, 130, 180, 0x000000, 0)
                .setInteractive({ useHandCursor: true });

            hitArea.on('pointerover', () => bg.clear()
                .fillStyle(0x2a2a50, 1).lineStyle(2, 0xffffff, 1)
                .fillRoundedRect(x - 65, y - 70, 130, 180, 12)
                .strokeRoundedRect(x - 65, y - 70, 130, 180, 12));
            hitArea.on('pointerout', () => bg.clear()
                .fillStyle(0x1a1a35, 1).lineStyle(2, sym.color, 0.8)
                .fillRoundedRect(x - 65, y - 70, 130, 180, 12)
                .strokeRoundedRect(x - 65, y - 70, 130, 180, 12));
            hitArea.on('pointerdown', () => {
                this.playerState.symbolPool.push(sym.id);
                this._goNextRound();
            });
        }
    }

    _displayRemoveOption() {
        const W = 1280;
        if (this.playerState.gold < 5 || this.playerState.symbolPool.length <= 3) return;

        this.add.text(W / 2, 460, '🗑️ 심볼 제거 (5 골드)', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ff8844',
            backgroundColor: '#2a1a0a', padding: { x: 16, y: 8 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true })
            .on('pointerdown', () => this._showRemovePanel());
    }

    _showRemovePanel() {
        if (this.removePanel) return;
        const W = 1280;

        this.removePanel = this.add.container(0, 0);
        const overlay = this.add.rectangle(W / 2, 360, W, 720, 0x000000, 0.7)
            .setInteractive();
        this.removePanel.add(overlay);

        this.add.text(W / 2, 200, '제거할 심볼을 선택하세요', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffffff'
        }).setOrigin(0.5).setDepth(10);

        const pool = this.playerState.symbolPool;
        const cols = Math.min(pool.length, 8);
        const startX = W / 2 - (cols - 1) * 70 / 2;

        for (let i = 0; i < pool.length; i++) {
            const sym = SYMBOL_DATA[pool[i]];
            if (!sym) continue;
            const col = i % cols;
            const row = Math.floor(i / cols);
            const x = startX + col * 70;
            const y = 280 + row * 80;

            const btn = this.add.text(x, y, sym.icon, {
                fontSize: '28px', backgroundColor: '#1a1a35',
                padding: { x: 8, y: 8 }
            }).setOrigin(0.5).setInteractive({ useHandCursor: true }).setDepth(10);

            btn.on('pointerdown', () => {
                this.playerState.symbolPool.splice(i, 1);
                this.playerState.gold -= 5;
                this._goNextRound();
            });
        }

        const cancelBtn = this.add.text(W / 2, 500, '[ 취소 ]', {
            fontSize: '18px', fontFamily: 'monospace', color: '#aaaaaa',
            padding: { x: 16, y: 8 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true }).setDepth(10);
        cancelBtn.on('pointerdown', () => {
            this.removePanel.destroy();
            this.removePanel = null;
        });
    }

    _displaySkipButton() {
        const W = 1280;
        const skipBtn = this.add.text(W / 2, 520, '[ 스킵 — 다음 라운드 ]', {
            fontSize: '18px', fontFamily: 'monospace', color: '#666666',
            padding: { x: 16, y: 8 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });
        skipBtn.on('pointerover', () => skipBtn.setColor('#aaaaaa'));
        skipBtn.on('pointerout', () => skipBtn.setColor('#666666'));
        skipBtn.on('pointerdown', () => this._goNextRound());
    }

    _displayCurrentDeck() {
        const W = 1280;
        this.add.text(W / 2, 590, '현재 덱:', {
            fontSize: '14px', fontFamily: 'monospace', color: '#666666'
        }).setOrigin(0.5);

        const pool = this.playerState.symbolPool;
        const icons = pool.map(id => SYMBOL_DATA[id] ? SYMBOL_DATA[id].icon : '?').join(' ');
        this.add.text(W / 2, 620, icons, {
            fontSize: '20px', wordWrap: { width: 800 }, align: 'center'
        }).setOrigin(0.5);

        this.add.text(W / 2, 660, `HP: ${this.playerState.hp}/${this.playerState.maxHp}  |  골드: ${this.playerState.gold}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);
    }

    _effectToString(effect) {
        const parts = [];
        if (effect.damage) parts.push(`⚔️${effect.damage}`);
        if (effect.block) parts.push(`🛡️${effect.block}`);
        if (effect.heal) parts.push(`❤️${effect.heal}`);
        if (effect.gold) parts.push(`🪙${effect.gold}`);
        if (effect.aoe) parts.push('(전체)');
        if (effect.hits) parts.push(`(${effect.hits}연타)`);
        if (effect.pierce) parts.push('(관통)');
        if (effect.thorns) parts.push(`(반사${effect.thorns})`);
        return parts.join(' ');
    }

    _goNextRound() {
        this.scene.start('BattleScene', {
            round: this.round + 1,
            playerState: this.playerState
        });
    }
}
