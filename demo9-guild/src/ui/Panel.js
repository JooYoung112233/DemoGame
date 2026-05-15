class UIPanel {
    static create(scene, x, y, width, height, opts = {}) {
        const {
            fillColor = 0x151525,
            strokeColor = 0x333355,
            alpha = 1,
            title = null,
            titleColor = '#aaaacc'
        } = opts;

        const container = scene.add.container(x, y);

        const bg = scene.add.graphics();
        bg.fillStyle(fillColor, alpha);
        bg.fillRoundedRect(0, 0, width, height, 4);
        bg.lineStyle(1, strokeColor, 0.8);
        bg.strokeRoundedRect(0, 0, width, height, 4);
        container.add(bg);

        if (title) {
            const titleText = scene.add.text(width / 2, 12, title, {
                fontSize: '13px',
                fontFamily: 'monospace',
                color: titleColor,
                fontStyle: 'bold'
            }).setOrigin(0.5, 0);
            container.add(titleText);
        }

        container._bg = bg;
        container._width = width;
        container._height = height;
        return container;
    }
}
