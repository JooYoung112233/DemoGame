class DamagePopup {
    static show(scene, x, y, value, color = '#ff4444') {
        const text = scene.add.text(x, y, `-${value}`, {
            fontSize: '16px', fontFamily: 'monospace', fontStyle: 'bold',
            color, stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(1000);

        scene.tweens.add({
            targets: text,
            y: y - 30,
            alpha: 0,
            duration: 800,
            ease: 'Power2',
            onComplete: () => text.destroy()
        });
    }

    static showHeal(scene, x, y, value) {
        const text = scene.add.text(x, y, `+${value}`, {
            fontSize: '16px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#44ff44', stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(1000);

        scene.tweens.add({
            targets: text,
            y: y - 30,
            alpha: 0,
            duration: 800,
            ease: 'Power2',
            onComplete: () => text.destroy()
        });
    }

    static showText(scene, x, y, msg, color = '#ffffff') {
        const text = scene.add.text(x, y, msg, {
            fontSize: '13px', fontFamily: 'monospace', fontStyle: 'bold',
            color, stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(1000);

        scene.tweens.add({
            targets: text,
            y: y - 25,
            alpha: 0,
            duration: 1000,
            ease: 'Power2',
            onComplete: () => text.destroy()
        });
    }
}
