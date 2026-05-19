class Toast {
    static show(scene, message, opts = {}) {
        const x = opts.x || 640;
        const y = opts.y || 100;
        const color = opts.color || '#44ff88';
        const duration = opts.duration || 2000;

        const text = scene.add.text(x, y, message, {
            fontSize: '18px', fontFamily: 'monospace', color, align: 'center',
            stroke: '#000000', strokeThickness: 3,
        }).setOrigin(0.5).setAlpha(0);

        scene.tweens.add({
            targets: text,
            alpha: 1,
            y: y - 20,
            duration: 300,
            ease: 'Power2',
            onComplete: () => {
                scene.tweens.add({
                    targets: text,
                    alpha: 0,
                    y: y - 50,
                    duration: 500,
                    delay: duration,
                    ease: 'Power2',
                    onComplete: () => text.destroy(),
                });
            }
        });
    }
}
