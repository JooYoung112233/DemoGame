class UIButton {
    constructor(scene, x, y, text, opts = {}) {
        const w = opts.width || 200;
        const h = opts.height || 48;
        const bg = opts.bg || 0x2a2a50;
        const hoverBg = opts.hoverBg || 0x3a3a70;
        const textColor = opts.textColor || '#ffffff';
        const fontSize = opts.fontSize || '16px';

        this.container = scene.add.container(x, y);

        this.bg = scene.add.rectangle(0, 0, w, h, bg).setInteractive({ useHandCursor: true });
        this.bg.setStrokeStyle(1, 0x4466aa);

        this.label = scene.add.text(0, 0, text, {
            fontSize, fontFamily: 'monospace', color: textColor, align: 'center'
        }).setOrigin(0.5);

        this.container.add([this.bg, this.label]);

        this.bg.on('pointerover', () => this.bg.setFillStyle(hoverBg));
        this.bg.on('pointerout', () => this.bg.setFillStyle(bg));

        if (opts.onClick) {
            this.bg.on('pointerdown', opts.onClick);
        }
    }

    setVisible(v) { this.container.setVisible(v); return this; }
    destroy() { this.container.destroy(); }
}
