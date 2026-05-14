class HotelResultScene extends Phaser.Scene {
    constructor() {
        super('HotelResultScene');
    }

    init(data) {
        this.runState = data.runState;
        this.floorManager = data.floorManager;
        this.victory = data.victory;
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');

        const titleColor = this.victory ? '#44ff88' : '#ff4466';
        const titleText = this.victory ? '클리어!' : '사망';

        this.add.text(640, 80, titleText, {
            fontSize: '48px', fontFamily: 'monospace', fontStyle: 'bold',
            color: titleColor
        }).setOrigin(0.5);

        this.add.text(640, 140, '호텔 100층', {
            fontSize: '18px', fontFamily: 'monospace',
            color: '#666666'
        }).setOrigin(0.5);

        const stats = [
            ['도달 층', this.floorManager.currentFloor + 'F'],
            ['총 처치', this.runState.totalKills + '마리'],
            ['총 피해량', Math.floor(this.runState.totalDamage) + ''],
            ['소요 시간', this.formatTime(this.runState.totalTime)],
            ['획득 코인', this.runState.coins + ''],
            ['적용 강화', this.runState.appliedUpgrades.length + '개']
        ];

        const startY = 210;
        stats.forEach((stat, i) => {
            const y = startY + i * 36;
            this.add.text(480, y, stat[0], {
                fontSize: '16px', fontFamily: 'monospace',
                color: '#888888'
            }).setOrigin(0, 0.5);

            this.add.text(800, y, stat[1], {
                fontSize: '16px', fontFamily: 'monospace', fontStyle: 'bold',
                color: '#ffffff'
            }).setOrigin(1, 0.5);
        });

        if (this.runState.appliedUpgrades.length > 0) {
            this.add.text(640, 440, '강화 목록', {
                fontSize: '14px', fontFamily: 'monospace',
                color: '#555566'
            }).setOrigin(0.5);

            const upgrades = this.runState.appliedUpgrades;
            const perRow = 8;
            const rows = Math.ceil(upgrades.length / perRow);

            upgrades.forEach((u, i) => {
                const row = Math.floor(i / perRow);
                const col = i % perRow;
                const ux = 640 - (Math.min(perRow, upgrades.length - row * perRow) * 30) / 2 + col * 30;
                const uy = 475 + row * 30;

                const g = this.add.graphics();
                const c = Phaser.Display.Color.HexStringToColor(u.color).color;
                g.fillStyle(c, 0.7);
                g.fillCircle(ux, uy, 9);
            });
        }

        const retryBtn = this.add.text(520, 580, '다시 도전', {
            fontSize: '20px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#ff4466', backgroundColor: '#2a0a1a',
            padding: { x: 24, y: 10 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        retryBtn.on('pointerover', () => retryBtn.setColor('#ff6688'));
        retryBtn.on('pointerout', () => retryBtn.setColor('#ff4466'));
        retryBtn.on('pointerdown', () => this.scene.start('HotelTitleScene'));

        const menuBtn = this.add.text(760, 580, '메인으로', {
            fontSize: '20px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#888888', backgroundColor: '#1a1a2a',
            padding: { x: 24, y: 10 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        menuBtn.on('pointerover', () => menuBtn.setColor('#aaaaaa'));
        menuBtn.on('pointerout', () => menuBtn.setColor('#888888'));
        menuBtn.on('pointerdown', () => { window.location.href = '../'; });

        this.add.text(640, 680, '전투 데모 프로토타입 v0.1', {
            fontSize: '12px', fontFamily: 'monospace',
            color: '#333344'
        }).setOrigin(0.5);
    }

    formatTime(ms) {
        const seconds = Math.floor(ms / 1000);
        const minutes = Math.floor(seconds / 60);
        const secs = seconds % 60;
        return minutes + '분 ' + secs + '초';
    }
}
