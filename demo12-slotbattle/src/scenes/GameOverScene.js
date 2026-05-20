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
        this.cameras.main.setBackgroundColor('#0a0a0a');

        if (this.victory) {
            this.add.text(W / 2, 160, '🎭', { fontSize: '80px' }).setOrigin(0.5);
            this.add.text(W / 2, 260, 'CURTAIN CALL', {
                fontSize: '48px', fontFamily: 'monospace', color: '#cc8844', fontStyle: 'bold'
            }).setOrigin(0.5);
            this.add.text(W / 2, 320, '모든 배역이 끝나고, 저주가 풀렸다.', {
                fontSize: '18px', fontFamily: 'monospace', color: '#886644'
            }).setOrigin(0.5);
        } else {
            this.add.text(W / 2, 160, '💀', { fontSize: '80px' }).setOrigin(0.5);
            this.add.text(W / 2, 260, '막이 내려간다', {
                fontSize: '44px', fontFamily: 'monospace', color: '#cc4444', fontStyle: 'bold'
            }).setOrigin(0.5);
            this.add.text(W / 2, 320, `제${this.act + 1}막에서 감독이 쓰러졌다`, {
                fontSize: '20px', fontFamily: 'monospace', color: '#aaaaaa'
            }).setOrigin(0.5);
        }

        this.add.text(W / 2, 370, `HP: ${Math.max(0, this.playerState.hp)}/${this.playerState.maxHp}  |  골드: ${this.playerState.gold}  |  덱: ${this.playerState.symbolPool.length}장`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);

        const deckIcons = this.playerState.symbolPool
            .map(id => SYMBOL_DATA[id] ? SYMBOL_DATA[id].icon : '?').join(' ');
        this.add.text(W / 2, 410, `최종 대본: ${deckIcons}`, {
            fontSize: '16px', wordWrap: { width: 800 }, align: 'center'
        }).setOrigin(0.5);

        if (this.playerState.relics && this.playerState.relics.length > 0) {
            const relicStr = this.playerState.relics.map(id => {
                const r = RELIC_DATA[id];
                return r ? `${r.icon} ${r.name}` : id;
            }).join('  ');
            this.add.text(W / 2, 450, `소품: ${relicStr}`, {
                fontSize: '14px', fontFamily: 'monospace', color: '#cc8866'
            }).setOrigin(0.5);
        }

        const retryBtn = this.add.text(W / 2, 550, '[ 다시 개막 ]', {
            fontSize: '28px', fontFamily: 'monospace', color: '#cc8844',
            fontStyle: 'bold', backgroundColor: '#1a1a10',
            padding: { x: 30, y: 10 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        retryBtn.on('pointerover', () => retryBtn.setColor('#ffcc88'));
        retryBtn.on('pointerout', () => retryBtn.setColor('#cc8844'));
        retryBtn.on('pointerdown', () => this.scene.start('TitleScene'));

        this.tweens.add({
            targets: retryBtn,
            scaleX: 1.05, scaleY: 1.05,
            duration: 800, yoyo: true, repeat: -1, ease: 'Sine.easeInOut'
        });
    }
}
