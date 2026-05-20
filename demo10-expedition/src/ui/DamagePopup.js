class DamagePopup {
    static show(scene, x, y, amount, color = '#ffffff') {
        const txt = scene.add.text(x, y, `-${amount}`, {
            fontSize: '16px', fontFamily: 'monospace', color: color,
            stroke: '#000000', strokeThickness: 3, fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(200);

        scene.tweens.add({
            targets: txt,
            y: y - 30,
            alpha: 0,
            duration: 800,
            ease: 'Power2',
            onComplete: () => txt.destroy()
        });
    }

    static showText(scene, x, y, text, color = '#ffffff') {
        const txt = scene.add.text(x, y, text, {
            fontSize: '13px', fontFamily: 'monospace', color: color,
            stroke: '#000000', strokeThickness: 2, fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(200);

        scene.tweens.add({
            targets: txt,
            y: y - 25,
            alpha: 0,
            duration: 1000,
            ease: 'Power2',
            onComplete: () => txt.destroy()
        });
    }
}
