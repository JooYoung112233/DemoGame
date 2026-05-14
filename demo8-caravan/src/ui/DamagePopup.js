class DamagePopup {
    static show(scene, x, y, amount, color, isHeal) {
        const prefix = isHeal ? '+' : '-';
        const text = scene.add.text(x, y, `${prefix}${Math.floor(amount)}`, {
            fontSize: isHeal ? '16px' : '14px',
            fontFamily: 'monospace',
            color: Phaser.Display.Color.IntegerToColor(color).rgba,
            fontStyle: 'bold',
            stroke: '#000000',
            strokeThickness: 3
        }).setOrigin(0.5).setDepth(100);

        scene.tweens.add({
            targets: text,
            y: y - 40,
            alpha: 0,
            duration: 800,
            ease: 'Power2',
            onComplete: () => text.destroy()
        });
    }

    static showCritical(scene, x, y, amount) {
        const text = scene.add.text(x, y, `${Math.floor(amount)}!`, {
            fontSize: '22px',
            fontFamily: 'monospace',
            color: '#ffff00',
            fontStyle: 'bold',
            stroke: '#000000',
            strokeThickness: 4
        }).setOrigin(0.5).setDepth(100);

        text.setScale(1.5);
        scene.tweens.add({
            targets: text,
            y: y - 50,
            alpha: 0,
            scaleX: 1,
            scaleY: 1,
            duration: 1000,
            ease: 'Power2',
            onComplete: () => text.destroy()
        });
    }
}
