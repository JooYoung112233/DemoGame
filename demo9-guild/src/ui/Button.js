class UIButton {
    static create(scene, x, y, width, height, label, opts = {}) {
        const {
            color = 0xffaa44,
            hoverColor = 0xffcc66,
            disabledColor = 0x444444,
            textColor = '#000000',
            fontSize = 14,
            disabled = false,
            onClick = null,
            depth = 0
        } = opts;

        const container = scene.add.container(x, y);
        if (depth) container.setDepth(depth);

        const bg = scene.add.graphics();
        const drawBg = (c, strokeC) => {
            bg.clear();
            bg.fillStyle(c, 1);
            bg.fillRoundedRect(-width / 2, -height / 2, width, height, 6);
            bg.lineStyle(1, strokeC, 0.6);
            bg.strokeRoundedRect(-width / 2, -height / 2, width, height, 6);
        };
        drawBg(disabled ? disabledColor : color, disabled ? 0x333333 : 0xffffff);

        const text = scene.add.text(0, 0, label, {
            fontSize: `${fontSize}px`,
            fontFamily: 'monospace',
            color: disabled ? '#666666' : textColor,
            fontStyle: 'bold'
        }).setOrigin(0.5);

        container.add([bg, text]);

        const hitZone = scene.add.zone(0, 0, width, height).setInteractive({ useHandCursor: !disabled });
        container.add(hitZone);

        if (!disabled && onClick) {
            hitZone.on('pointerover', () => drawBg(hoverColor, 0xffffff));
            hitZone.on('pointerout', () => drawBg(color, 0xffffff));
            hitZone.on('pointerdown', () => {
                drawBg(0xdddddd, 0xffffff);
                scene.time.delayedCall(100, () => drawBg(color, 0xffffff));
                onClick();
            });
        }

        container._bg = bg;
        container._text = text;
        container._hitZone = hitZone;
        return container;
    }
}
