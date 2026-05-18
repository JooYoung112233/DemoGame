class UITooltip {
    static show(scene, x, y, lines) {
        UITooltip.hide(scene);
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        const padding = 10;
        const maxWidth = 260;

        const textContent = lines.join('\n');
        const text = scene.add.text(0, 0, textContent, {
            fontSize: `${(T.fontSize && T.fontSize.body) || 12}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textPrimary || '#e8d8c0',
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
        bg.fillStyle(T.panelFill || 0x2a2218, 0.95);
        bg.fillRoundedRect(0, 0, w, h, 5);
        // 내부 글로우
        bg.fillStyle(0xffffff, 0.03);
        bg.fillRect(2, 2, w - 4, Math.min(12, h * 0.2));
        // 테두리
        bg.lineStyle(1, T.ornament || 0x8a7a4a, 0.7);
        bg.strokeRoundedRect(0, 0, w, h, 5);

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
