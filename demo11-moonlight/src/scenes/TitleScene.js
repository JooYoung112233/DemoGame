class TitleScene extends Phaser.Scene {
    constructor() { super('TitleScene'); }

    create() {
        const cx = 640, cy = 360;

        this.add.rectangle(cx, cy, 1280, 720, 0x0a0a1a);

        const moonGlow = this.add.circle(cx, 140, 60, 0xffeebb, 0.15);
        this.add.circle(cx, 140, 40, 0xffeebb, 0.3);
        this.tweens.add({ targets: moonGlow, scaleX: 1.2, scaleY: 1.2, alpha: 0.08, duration: 2000, yoyo: true, repeat: -1 });

        this.add.text(cx, 220, '🌙 Moonlight', {
            fontSize: '52px', fontFamily: 'monospace', color: '#ffeebb',
            stroke: '#000', strokeThickness: 4,
        }).setOrigin(0.5);

        this.add.text(cx, 280, '낮에는 모험, 밤에는 장사', {
            fontSize: '18px', fontFamily: 'monospace', color: '#888',
        }).setOrigin(0.5);

        const startBtn = new UIButton(this, cx, 400, '▶  게임 시작', {
            width: 240, height: 56, bg: 0x224422, hoverBg: 0x336633,
            textColor: '#44ff88', fontSize: '20px',
            onClick: () => {
                window.gameState = new GameState();
                this.scene.start('HubScene');
            }
        });

        this.add.text(cx, 680, 'Demo 11 — Phaser 3 프로토타입', {
            fontSize: '12px', fontFamily: 'monospace', color: '#444',
        }).setOrigin(0.5);
    }
}
