class TitleScene extends Phaser.Scene {
    constructor() { super('TitleScene'); }

    create() {
        const cx = 640, cy = 360;

        this.add.rectangle(cx, cy, 1280, 720, 0x0a0a1a);

        this.add.text(cx, 160, '⚔', { fontSize: '64px' }).setOrigin(0.5);
        this.add.text(cx, 240, '용병 길드', {
            fontSize: '40px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.add.text(cx, 290, '판타지 로그라이크 루트 & 길드 경영', {
            fontSize: '14px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5);

        const hasSave = SaveManager.hasSave();

        UIButton.create(this, cx, 400, 200, 44, '새 게임', {
            color: 0x446688, hoverColor: 0x5588aa, textColor: '#ffffff',
            fontSize: 16,
            onClick: () => {
                if (hasSave) {
                    this._showConfirm();
                } else {
                    this._startNewGame();
                }
            }
        });

        if (hasSave) {
            UIButton.create(this, cx, 460, 200, 44, '이어하기', {
                color: 0xffaa44, hoverColor: 0xffcc66, textColor: '#000000',
                fontSize: 16,
                onClick: () => this._continueGame()
            });
        }

        this.add.text(cx, 650, 'Demo 9 — Guild Management Roguelike', {
            fontSize: '11px', fontFamily: 'monospace', color: '#444466'
        }).setOrigin(0.5);
    }

    _startNewGame() {
        SaveManager.deleteSave();
        const gameState = GuildManager.createDefaultState();
        MercenaryManager.generateRecruitPool(gameState);
        GuildManager.addMessage(gameState, '길드가 설립되었습니다. 용병을 모집하세요!');
        SaveManager.save(gameState);
        this.scene.start('TownScene', { gameState });
    }

    _continueGame() {
        const gameState = SaveManager.load();
        if (!gameState) {
            this._startNewGame();
            return;
        }
        this.scene.start('TownScene', { gameState });
    }

    _showConfirm() {
        const cx = 640, cy = 360;
        const overlay = this.add.rectangle(cx, cy, 1280, 720, 0x000000, 0.7).setDepth(100).setInteractive();
        const panel = UIPanel.create(this, cx - 160, cy - 60, 320, 120, { title: '기존 저장을 삭제하시겠습니까?' });
        panel.setDepth(101);

        const yesBtn = UIButton.create(this, cx - 60, cy + 20, 100, 34, '삭제 후 시작', {
            color: 0xff4444, hoverColor: 0xff6666, textColor: '#ffffff', fontSize: 12,
            onClick: () => { overlay.destroy(); panel.destroy(); yesBtn.destroy(); noBtn.destroy(); this._startNewGame(); }
        });
        yesBtn.setDepth(102);

        const noBtn = UIButton.create(this, cx + 60, cy + 20, 100, 34, '취소', {
            color: 0x555555, hoverColor: 0x777777, textColor: '#ffffff', fontSize: 12,
            onClick: () => { overlay.destroy(); panel.destroy(); yesBtn.destroy(); noBtn.destroy(); }
        });
        noBtn.setDepth(102);
    }
}
