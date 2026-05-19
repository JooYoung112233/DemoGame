class TitleScene extends Phaser.Scene {
    constructor() { super('TitleScene'); }

    create() {
        const cx = 640, cy = 360;
        this.add.rectangle(cx, cy, 1280, 720, 0x0a0a1a);

        // 별
        for (let i = 0; i < 50; i++) {
            const s = this.add.circle(
                Phaser.Math.Between(0, 1280), Phaser.Math.Between(0, 400),
                Phaser.Math.Between(1, 2), 0xffffff, Phaser.Math.FloatBetween(0.05, 0.4)
            );
            this.tweens.add({ targets: s, alpha: 0.05, duration: Phaser.Math.Between(1500, 4000), yoyo: true, repeat: -1 });
        }

        // 달
        this.add.circle(cx, 120, 50, 0xffeebb, 0.12);
        const moon = this.add.circle(cx, 120, 35, 0xffeebb, 0.35);
        this.tweens.add({ targets: moon, scaleX: 1.1, scaleY: 1.1, alpha: 0.2, duration: 3000, yoyo: true, repeat: -1 });

        // 건물 실루엣
        this.add.rectangle(200, 600, 120, 180, 0x111122);
        this.add.rectangle(380, 580, 100, 220, 0x0e0e1e);
        this.add.rectangle(640, 570, 160, 240, 0x131325);
        this.add.rectangle(900, 590, 110, 200, 0x101020);
        this.add.rectangle(1080, 600, 130, 180, 0x111122);

        // 상점 간판 빛
        const signGlow = this.add.rectangle(640, 490, 80, 20, 0xffaa44, 0.3);
        this.tweens.add({ targets: signGlow, alpha: 0.1, duration: 1500, yoyo: true, repeat: -1 });

        this.add.text(cx, 250, '🌙 Moonlight Shop', {
            fontSize: '48px', fontFamily: 'monospace', color: '#ffeebb',
            stroke: '#000', strokeThickness: 5,
        }).setOrigin(0.5);

        this.add.text(cx, 310, '의뢰하고, 만들고, 팔아라', {
            fontSize: '16px', fontFamily: 'monospace', color: '#887766',
        }).setOrigin(0.5);

        new UIButton(this, cx, 420, '▶  게임 시작', {
            width: 240, height: 56, bg: 0x2a2210, hoverBg: 0x3a3320,
            textColor: '#ffcc88', fontSize: '20px',
            onClick: () => {
                window.gameState = new GameState();
                this.scene.start('MorningScene');
            }
        });

        this.add.text(cx, 690, 'Demo 11 — Moonlight Shop', {
            fontSize: '11px', fontFamily: 'monospace', color: '#333',
        }).setOrigin(0.5);
    }
}
