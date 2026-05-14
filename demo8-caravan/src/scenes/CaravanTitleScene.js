class CaravanTitleScene extends Phaser.Scene {
    constructor() {
        super({ key: 'CaravanTitleScene' });
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');

        const g = this.add.graphics();
        for (let i = 0; i < 720; i++) {
            const ratio = i / 720;
            const r = Math.floor(10 + ratio * 30);
            const gr = Math.floor(8 + ratio * 15);
            const b = Math.floor(20 + ratio * 20);
            g.fillStyle(Phaser.Display.Color.GetColor(r, gr, b));
            g.fillRect(0, i, 1280, 1);
        }

        g.fillStyle(0x2a3a2a);
        g.fillRect(0, 500, 1280, 220);
        g.lineStyle(2, 0x4a5a4a);
        g.lineBetween(0, 500, 1280, 500);

        g.fillStyle(0x664422);
        g.fillRect(200, 490, 880, 8);

        this.drawCaravan(g, 640, 480);

        this.add.text(640, 160, '마왕성까지의 마차', {
            fontSize: '48px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#ffaa44', stroke: '#000000', strokeThickness: 6
        }).setOrigin(0.5);

        this.add.text(640, 220, 'Caravan to the Demon King\'s Castle', {
            fontSize: '16px', fontFamily: 'monospace',
            color: '#886644', stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5);

        this.add.text(640, 290, [
            '마을에서 동료를 징집하고, 전직시켜 강하게 키워라.',
            '도적을 처치하고, 이벤트를 헤쳐나가며',
            '마왕성까지 마차를 이끌어라!',
        ].join('\n'), {
            fontSize: '14px', fontFamily: 'monospace',
            color: '#aaaaaa', lineSpacing: 8, align: 'center'
        }).setOrigin(0.5);

        const startBtn = this.add.rectangle(640, 420, 200, 50, 0x3a2a1a)
            .setStrokeStyle(2, 0xffaa44)
            .setInteractive({ useHandCursor: true });

        const startText = this.add.text(640, 420, '▶ 출 발', {
            fontSize: '22px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#ffaa44'
        }).setOrigin(0.5);

        startBtn.on('pointerover', () => {
            startBtn.setFillStyle(0x5a4a2a);
            startText.setColor('#ffcc66');
        });
        startBtn.on('pointerout', () => {
            startBtn.setFillStyle(0x3a2a1a);
            startText.setColor('#ffaa44');
        });
        startBtn.on('pointerdown', () => {
            const runState = {
                gold: 100,
                party: [new Unit('soldier'), new Unit('soldier')],
                currentNode: 0,
                maxPartySize: 6,
                totalGoldEarned: 0,
                totalBattles: 0,
                totalRecruits: 0,
                gameOver: false
            };
            this.scene.start('MapScene', { runState });
        });

        this.add.text(640, 680, '전투 데모 프로토타입 #8 — Phaser 3', {
            fontSize: '11px', fontFamily: 'monospace', color: '#444444'
        }).setOrigin(0.5);
    }

    drawCaravan(g, x, y) {
        g.fillStyle(0x8B6914);
        g.fillRect(x - 25, y - 15, 50, 22);

        g.fillStyle(0xC4A035);
        g.fillRect(x - 22, y - 28, 44, 15);

        g.fillStyle(0x333333);
        g.fillCircle(x - 15, y + 10, 6);
        g.fillCircle(x + 15, y + 10, 6);
        g.fillStyle(0x666666);
        g.fillCircle(x - 15, y + 10, 3);
        g.fillCircle(x + 15, y + 10, 3);

        g.fillStyle(0x8B5A2B);
        g.fillRect(x + 25, y - 4, 20, 4);

        g.fillStyle(0x996633);
        g.fillRect(x + 42, y - 12, 12, 16);
        g.fillStyle(0xffcc99);
        g.fillRect(x + 44, y - 10, 8, 6);
    }
}
