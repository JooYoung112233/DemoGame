class UIToast {
    static show(scene, msg, opts = {}) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const {
            x = 640,
            y = 690,
            duration = 2500,
            color = T.textGold || '#ffcc44',
            type = 'info',  // 'info' | 'success' | 'danger'
        } = opts;

        const colors = {
            info: color,
            success: T.textSuccess || '#66bb55',
            danger: T.textDanger || '#cc4422',
        };

        const text = scene.add.text(x, y, msg, {
            fontSize: `${(T.fontSize && T.fontSize.body) || 12}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: colors[type] || color,
            fontStyle: 'bold',
            stroke: '#000000',
            strokeThickness: 3,
            padding: { x: 12, y: 6 },
            backgroundColor: 'rgba(26,21,16,0.85)',
        }).setOrigin(0.5).setDepth(999);

        scene.tweens.add({
            targets: text,
            y: y - 25,
            alpha: 0,
            duration,
            ease: 'Power2',
            onComplete: () => text.destroy()
        });
    }
}
