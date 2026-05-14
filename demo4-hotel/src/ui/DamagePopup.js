class HotelDamagePopup {
    constructor(scene) {
        this.scene = scene;
        this.popups = [];
    }

    show(x, y, amount, isCrit, isPlayerDamage, isHeal, customColor) {
        let color, fontSize, prefix;

        if (isHeal) {
            color = '#44ff44';
            fontSize = 14;
            prefix = '+';
        } else if (isCrit) {
            color = '#ffff44';
            fontSize = 18;
            prefix = '';
        } else if (isPlayerDamage) {
            color = '#ff4444';
            fontSize = 16;
            prefix = '-';
        } else {
            color = '#ffffff';
            fontSize = 14;
            prefix = '';
        }

        if (customColor) {
            color = '#' + customColor.toString(16).padStart(6, '0');
        }

        const text = this.scene.add.text(
            x + (Math.random() - 0.5) * 20,
            y,
            prefix + Math.floor(amount),
            {
                fontSize: fontSize + 'px',
                fontFamily: 'monospace',
                fontStyle: 'bold',
                color: color,
                stroke: '#000000',
                strokeThickness: 3
            }
        );
        text.setOrigin(0.5);
        text.setDepth(200);

        this.scene.tweens.add({
            targets: text,
            y: y - 30 - Math.random() * 15,
            alpha: 0,
            duration: isCrit ? 800 : 600,
            ease: 'Power2',
            onComplete: () => text.destroy()
        });

        if (isCrit) {
            this.scene.tweens.add({
                targets: text,
                scaleX: 1.5,
                scaleY: 1.5,
                duration: 100,
                yoyo: true,
                ease: 'Bounce'
            });
        }
    }
}
