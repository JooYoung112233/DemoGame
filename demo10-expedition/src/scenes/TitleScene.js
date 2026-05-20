class TitleScene extends Phaser.Scene {
    constructor() {
        super('TitleScene');
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a0a');

        this.add.text(W / 2, 140, '🏚️', { fontSize: '80px' }).setOrigin(0.5);

        this.add.text(W / 2, 240, 'OPENING CITY', {
            fontSize: '48px', fontFamily: 'monospace', color: '#cc8844', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(W / 2, 290, '폐허가 된 도시에서 살아남아라', {
            fontSize: '16px', fontFamily: 'monospace', color: '#886644', fontStyle: 'italic'
        }).setOrigin(0.5);

        const rules = [
            '🏠 안전가옥에서 출발, 폐허 도시를 탐색한다',
            '📦 건물을 수색하여 물자를 확보한다',
            '🎒 인벤토리는 제한적 — 무엇을 가져갈지 선택하라',
            '🌙 밤이 되면 감염자가 나타난다',
            '🚪 탈출 지점에 도달하면 물자를 정산한다',
            '⚔️ 전투는 최후의 수단 — 도망이 상책이다',
        ];
        for (let i = 0; i < rules.length; i++) {
            this.add.text(W / 2, 340 + i * 28, rules[i], {
                fontSize: '14px', fontFamily: 'monospace', color: '#666655'
            }).setOrigin(0.5);
        }

        const startBtn = this.add.text(W / 2, 560, '[ 출발 ]', {
            fontSize: '32px', fontFamily: 'monospace', color: '#cc8844',
            fontStyle: 'bold', backgroundColor: '#1a1a10',
            padding: { x: 40, y: 12 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        startBtn.on('pointerover', () => startBtn.setColor('#ffcc88'));
        startBtn.on('pointerout', () => startBtn.setColor('#cc8844'));
        startBtn.on('pointerdown', () => this.scene.start('CityScene'));

        this.tweens.add({
            targets: startBtn, scaleX: 1.05, scaleY: 1.05,
            duration: 800, yoyo: true, repeat: -1, ease: 'Sine.easeInOut'
        });

        // Controls info
        this.add.text(W / 2, 640, 'WASD 이동 | E 상호작용 | TAB 인벤토리 | 클릭 공격', {
            fontSize: '12px', fontFamily: 'monospace', color: '#444444'
        }).setOrigin(0.5);
    }
}
