class UIPanel {
    constructor(scene, x, y, w, h, opts = {}) {
        this.scene = scene;
        this.container = scene.add.container(x, y);
        const bg = opts.bg || 0x151530;
        const border = opts.border || 0x2a2a50;
        const alpha = opts.alpha || 0.95;

        this.bg = scene.add.rectangle(0, 0, w, h, bg, alpha);
        this.bg.setStrokeStyle(1, border);
        this.container.add(this.bg);

        if (opts.title) {
            this.title = scene.add.text(0, -h / 2 + 20, opts.title, {
                fontSize: '18px', fontFamily: 'monospace', color: '#ffffff', align: 'center'
            }).setOrigin(0.5);
            this.container.add(this.title);
        }
    }

    add(child) { this.container.add(child); return this; }
    setVisible(v) { this.container.setVisible(v); return this; }
    destroy() { this.container.destroy(); }
}
