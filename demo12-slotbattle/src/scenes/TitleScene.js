class TitleScene extends Phaser.Scene {
    constructor() {
        super('TitleScene');
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a1a');

        this.add.text(W / 2, 140, '🎰', { fontSize: '80px' }).setOrigin(0.5);

        this.add.text(W / 2, 240, 'SLOT BATTLE', {
            fontSize: '48px', fontFamily: 'monospace', color: '#ffcc00', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(W / 2, 290, '슬롯을 돌려 전투하라. 심볼을 모아 빌드를 완성하라.', {
            fontSize: '15px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);

        const rules = [
            '🎰 SPIN으로 슬롯 3개를 돌린다',
            '⚡ 심볼 조합에 따라 공격/방어/회복이 결정된다',
            '✨ 같은 심볼 3개 또는 특수 조합 = 콤보!',
            '🗺️ 트리 맵에서 경로를 선택하며 진행',
            '💎 보물 노드에서 유물을 획득하여 빌드 강화',
            '🐉 3개 Act의 보스를 처치하면 승리!',
        ];
        for (let i = 0; i < rules.length; i++) {
            this.add.text(W / 2, 340 + i * 28, rules[i], {
                fontSize: '14px', fontFamily: 'monospace', color: '#666666'
            }).setOrigin(0.5);
        }

        const startBtn = this.add.text(W / 2, 560, '[ 시작 ]', {
            fontSize: '32px', fontFamily: 'monospace', color: '#ffcc00',
            fontStyle: 'bold', backgroundColor: '#2a2a1a',
            padding: { x: 40, y: 12 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        startBtn.on('pointerover', () => startBtn.setColor('#ffffff'));
        startBtn.on('pointerout', () => startBtn.setColor('#ffcc00'));
        startBtn.on('pointerdown', () => this.scene.start('MapScene', { act: 0 }));

        this.tweens.add({
            targets: startBtn, scaleX: 1.05, scaleY: 1.05,
            duration: 800, yoyo: true, repeat: -1, ease: 'Sine.easeInOut'
        });
    }
}
