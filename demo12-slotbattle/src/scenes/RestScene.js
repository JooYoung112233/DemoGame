class RestScene extends Phaser.Scene {
    constructor() {
        super('RestScene');
    }

    init(data) {
        this.act = data.act;
        this.map = data.map;
        this.playerState = data.playerState;
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a0a');

        this.add.text(W / 2, 100, '🕯️', { fontSize: '64px' }).setOrigin(0.5);
        this.add.text(W / 2, 170, '분장실', {
            fontSize: '28px', fontFamily: 'monospace', color: '#cc8844', fontStyle: 'bold'
        }).setOrigin(0.5);

        const healAmount = Math.floor(this.playerState.maxHp * 0.3);
        const currentHp = this.playerState.hp;
        const maxHp = this.playerState.maxHp;

        this.add.text(W / 2, 220, `현재 HP: ${currentHp} / ${maxHp}`, {
            fontSize: '18px', fontFamily: 'monospace', color: '#aaaaaa'
        }).setOrigin(0.5);

        // Rest option
        const restBtn = this.add.text(W / 2, 320, `🌹 휴식 — HP ${healAmount} 회복`, {
            fontSize: '20px', fontFamily: 'monospace', color: '#cc8866',
            backgroundColor: '#1a1510', padding: { x: 24, y: 12 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        restBtn.on('pointerover', () => restBtn.setColor('#ffcc88'));
        restBtn.on('pointerout', () => restBtn.setColor('#cc8866'));
        restBtn.on('pointerdown', () => {
            const healed = Math.min(healAmount, maxHp - currentHp);
            this.playerState.hp += healed;

            restBtn.disableInteractive();
            const healText = this.add.text(W / 2, 380, `🌹 +${healed} HP 회복! (${this.playerState.hp}/${maxHp})`, {
                fontSize: '22px', fontFamily: 'monospace', color: '#cc8844', fontStyle: 'bold',
                stroke: '#000000', strokeThickness: 3
            }).setOrigin(0.5).setScale(0.5);

            this.tweens.add({
                targets: healText, scaleX: 1.1, scaleY: 1.1,
                duration: 200, ease: 'Back.easeOut',
                onComplete: () => {
                    this.time.delayedCall(500, () => this._goBack());
                }
            });
        });

        // Upgrade max HP option (costs gold)
        if (this.playerState.gold >= 15) {
            const upgradeBtn = this.add.text(W / 2, 400, `🕯️ 수련 — 최대 HP +5 (15G)`, {
                fontSize: '18px', fontFamily: 'monospace', color: '#aa88cc',
                backgroundColor: '#1a1020', padding: { x: 20, y: 10 }
            }).setOrigin(0.5).setInteractive({ useHandCursor: true });

            upgradeBtn.on('pointerover', () => upgradeBtn.setColor('#ddaaff'));
            upgradeBtn.on('pointerout', () => upgradeBtn.setColor('#aa88cc'));
            upgradeBtn.on('pointerdown', () => {
                this.playerState.gold -= 15;
                this.playerState.maxHp += 5;
                this.playerState.hp += 5;
                upgradeBtn.disableInteractive();
                upgradeBtn.setText('🕯️ 수련 완료!');
                this.time.delayedCall(500, () => this._goBack());
            });
        }

        // Skip
        const skipBtn = this.add.text(W / 2, 480, '[ 그냥 지나가기 ]', {
            fontSize: '16px', fontFamily: 'monospace', color: '#666666',
            padding: { x: 16, y: 8 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });
        skipBtn.on('pointerdown', () => this._goBack());
    }

    _goBack() {
        this.scene.start('MapScene', {
            act: this.act,
            map: this.map,
            playerState: this.playerState
        });
    }
}
