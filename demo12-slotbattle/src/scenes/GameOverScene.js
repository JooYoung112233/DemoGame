class GameOverScene extends Phaser.Scene {
    constructor() {
        super('GameOverScene');
    }

    init(data) {
        this.round = data.round || 0;
        this.act = data.act || 0;
        this.playerState = data.playerState;
        this.victory = data.victory || false;
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a1a');

        if (this.victory) {
            this.add.text(W / 2, 160, '🏆', { fontSize: '80px' }).setOrigin(0.5);
            this.add.text(W / 2, 260, 'VICTORY!', {
                fontSize: '52px', fontFamily: 'monospace', color: '#ffcc00', fontStyle: 'bold'
            }).setOrigin(0.5);
            this.add.text(W / 2, 320, '드래곤을 처치하고 던전을 정복했다!', {
                fontSize: '18px', fontFamily: 'monospace', color: '#44ff88'
            }).setOrigin(0.5);
        } else {
            this.add.text(W / 2, 160, '💀', { fontSize: '80px' }).setOrigin(0.5);
            this.add.text(W / 2, 260, 'GAME OVER', {
                fontSize: '48px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
            }).setOrigin(0.5);
            this.add.text(W / 2, 320, `Act ${this.act + 1}에서 사망`, {
                fontSize: '20px', fontFamily: 'monospace', color: '#aaaaaa'
            }).setOrigin(0.5);
        }

        this.add.text(W / 2, 370, `HP: ${Math.max(0, this.playerState.hp)}/${this.playerState.maxHp}  |  골드: ${this.playerState.gold}  |  덱: ${this.playerState.symbolPool.length}장`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);

        const deckIcons = this.playerState.symbolPool
            .map(id => SYMBOL_DATA[id] ? SYMBOL_DATA[id].icon : '?').join(' ');
        this.add.text(W / 2, 410, `최종 덱: ${deckIcons}`, {
            fontSize: '16px', wordWrap: { width: 800 }, align: 'center'
        }).setOrigin(0.5);

        if (this.playerState.relics && this.playerState.relics.length > 0) {
            const relicStr = this.playerState.relics.map(id => {
                const r = RELIC_DATA[id];
                return r ? `${r.icon} ${r.name}` : id;
            }).join('  ');
            this.add.text(W / 2, 450, `유물: ${relicStr}`, {
                fontSize: '14px', fontFamily: 'monospace', color: '#cc88ff'
            }).setOrigin(0.5);
        }

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
