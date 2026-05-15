class UITooltip {
    static show(scene, x, y, lines) {
        UITooltip.hide(scene);

        const padding = 10;
        const lineHeight = 16;
        const maxWidth = 250;

        const textContent = lines.join('\n');
        const text = scene.add.text(0, 0, textContent, {
            fontSize: '12px',
            fontFamily: 'monospace',
            color: '#dddddd',
            wordWrap: { width: maxWidth - padding * 2 },
            lineSpacing: 4
        });

        const w = Math.min(maxWidth, text.width + padding * 2);
        const h = text.height + padding * 2;

        let tx = x;
        let ty = y - h - 5;
        if (ty < 0) ty = y + 20;
        if (tx + w > 1280) tx = 1280 - w - 5;
        if (tx < 0) tx = 5;

        const container = scene.add.container(tx, ty).setDepth(1000);

        const bg = scene.add.graphics();
        bg.fillStyle(0x222244, 0.95);
        bg.fillRoundedRect(0, 0, w, h, 4);
        bg.lineStyle(1, 0x6666aa, 0.8);
        bg.strokeRoundedRect(0, 0, w, h, 4);

        text.setPosition(padding, padding);

        container.add([bg, text]);
        scene._tooltip = container;
    }

    static hide(scene) {
        if (scene._tooltip) {
            scene._tooltip.destroy();
            scene._tooltip = null;
        }
    }
}
