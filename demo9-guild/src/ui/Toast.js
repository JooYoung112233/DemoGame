class UIToast {
    static show(scene, msg, opts = {}) {
        const {
            x = 640,
            y = 690,
            duration = 2500,
            color = '#ffcc44'
        } = opts;

        const text = scene.add.text(x, y, msg, {
            fontSize: '14px',
            fontFamily: 'monospace',
            color,
            fontStyle: 'bold',
            stroke: '#000000',
            strokeThickness: 3
        }).setOrigin(0.5).setDepth(999);

        scene.tweens.add({
            targets: text,
            y: y - 20,
            alpha: 0,
            duration,
            ease: 'Power2',
            onComplete: () => text.destroy()
        });
    }
}
