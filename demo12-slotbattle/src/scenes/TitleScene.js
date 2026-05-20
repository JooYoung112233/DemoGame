class TitleScene extends Phaser.Scene {
    constructor() {
        super('TitleScene');
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a0a');

        this.add.text(W / 2, 120, '🎭', { fontSize: '80px' }).setOrigin(0.5);

        this.add.text(W / 2, 220, 'THE LAST THEATER', {
            fontSize: '44px', fontFamily: 'monospace', color: '#cc8844', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(W / 2, 270, '나는 싸우는 게 아니라, 죽음을 연출한다.', {
            fontSize: '15px', fontFamily: 'monospace', color: '#886644', fontStyle: 'italic'
        }).setOrigin(0.5);

        const rules = [
            '🎭 감독이 되어 슬롯으로 장면(Scene)을 연출한다',
            '⚔️ 심볼 조합에 따라 공격/방어/회복이 결정된다',
            '✨ 같은 심볼 3개 또는 특수 조합 = Scene 발동!',
            '🗺️ 저주받은 극장의 막(Act)을 탐험한다',
            '💎 소품과 유물로 연출 스타일을 강화한다',
            '👑 모든 배역을 끝내면 저주가 풀린다',
        ];
        for (let i = 0; i < rules.length; i++) {
            this.add.text(W / 2, 320 + i * 28, rules[i], {
                fontSize: '14px', fontFamily: 'monospace', color: '#666655'
            }).setOrigin(0.5);
        }

        const startBtn = this.add.text(W / 2, 540, '[ 개막 ]', {
            fontSize: '32px', fontFamily: 'monospace', color: '#cc8844',
            fontStyle: 'bold', backgroundColor: '#1a1a10',
            padding: { x: 40, y: 12 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        startBtn.on('pointerover', () => startBtn.setColor('#ffcc88'));
        startBtn.on('pointerout', () => startBtn.setColor('#cc8844'));
        startBtn.on('pointerdown', () => this.scene.start('MapScene', { act: 0 }));

        this.tweens.add({
            targets: startBtn, scaleX: 1.05, scaleY: 1.05,
            duration: 800, yoyo: true, repeat: -1, ease: 'Sine.easeInOut'
        });
    }
}
