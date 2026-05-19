class GameOverScene extends Phaser.Scene {
    constructor() {
        super('GameOverScene');
    }

    init(data) {
        this.round = data.round;
        this.playerState = data.playerState;
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a1a');

        this.add.text(W / 2, 200, '💀', { fontSize: '80px' }).setOrigin(0.5);

        this.add.text(W / 2, 300, 'GAME OVER', {
            fontSize: '48px', fontFamily: 'monospace', color: '#ff4444',
            fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(W / 2, 370, `라운드 ${this.round}에서 사망`, {
            fontSize: '20px', fontFamily: 'monospace', color: '#aaaaaa'
        }).setOrigin(0.5);

        this.add.text(W / 2, 410, `획득 골드: ${this.playerState.gold}  |  덱 크기: ${this.playerState.symbolPool.length}`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);

        const deckIcons = this.playerState.symbolPool
            .map(id => SYMBOL_DATA[id] ? SYMBOL_DATA[id].icon : '?').join(' ');
        this.add.text(W / 2, 450, `최종 덱: ${deckIcons}`, {
            fontSize: '16px', wordWrap: { width: 800 }, align: 'center'
        }).setOrigin(0.5);

        const retryBtn = this.add.text(W / 2, 550, '[ 다시 도전 ]', {
            fontSize: '28px', fontFamily: 'monospace', color: '#ffcc00',
            fontStyle: 'bold', backgroundColor: '#2a2a1a',
            padding: { x: 30, y: 10 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        retryBtn.on('pointerover', () => retryBtn.setColor('#ffffff'));
        retryBtn.on('pointerout', () => retryBtn.setColor('#ffcc00'));
        retryBtn.on('pointerdown', () => this.scene.start('TitleScene'));

        this.tweens.add({
            targets: retryBtn,
            scaleX: 1.05, scaleY: 1.05,
            duration: 800, yoyo: true, repeat: -1, ease: 'Sine.easeInOut'
        });
    }
}
