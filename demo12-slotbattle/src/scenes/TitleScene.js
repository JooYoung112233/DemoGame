class TitleScene extends Phaser.Scene {
    constructor() {
        super('TitleScene');
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a1a');

        this.add.text(W / 2, 180, '🎰', { fontSize: '80px' }).setOrigin(0.5);

        this.add.text(W / 2, 280, 'SLOT BATTLE', {
            fontSize: '48px', fontFamily: 'monospace', color: '#ffcc00',
            fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(W / 2, 330, '슬롯을 돌려 전투하라. 심볼을 모아 빌드를 완성하라.', {
            fontSize: '16px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);

        this.add.text(W / 2, 400, '조작법', {
            fontSize: '18px', fontFamily: 'monospace', color: '#aaaaaa'
        }).setOrigin(0.5);

        const rules = [
            '• SPIN 버튼으로 슬롯 3개를 돌린다',
            '• 나온 심볼 조합으로 전투 액션이 결정된다',
            '• 같은 심볼 3개 또는 특수 조합 = 콤보 보너스!',
            '• 라운드 클리어 시 새 심볼을 덱에 추가',
            '• 10라운드를 돌파하면 승리!',
        ];
        for (let i = 0; i < rules.length; i++) {
            this.add.text(W / 2, 440 + i * 26, rules[i], {
                fontSize: '14px', fontFamily: 'monospace', color: '#666666'
            }).setOrigin(0.5);
        }

        const startBtn = this.add.text(W / 2, 620, '[ 시작 ]', {
            fontSize: '32px', fontFamily: 'monospace', color: '#ffcc00',
            fontStyle: 'bold', backgroundColor: '#2a2a1a',
            padding: { x: 40, y: 12 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        startBtn.on('pointerover', () => startBtn.setColor('#ffffff'));
        startBtn.on('pointerout', () => startBtn.setColor('#ffcc00'));
        startBtn.on('pointerdown', () => {
            this.scene.start('BattleScene', { round: 1 });
        });

        this.tweens.add({
            targets: startBtn,
            scaleX: 1.05, scaleY: 1.05,
            duration: 800,
            yoyo: true,
            repeat: -1,
            ease: 'Sine.easeInOut'
        });
    }
}
