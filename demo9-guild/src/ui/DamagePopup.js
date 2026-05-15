class DamagePopup {
    static _fontSize(amount, base) {
        if (typeof amount !== 'number') return base;
        if (amount >= 500) return base + 10;
        if (amount >= 200) return base + 6;
        if (amount >= 100) return base + 3;
        return base;
    }

    static show(scene, x, y, amount, color, isHeal) {
        const prefix = isHeal ? '+' : '-';
        const label = typeof amount === 'string' ? amount : prefix + Math.floor(amount);
        const baseSize = isHeal ? 16 : 14;
        const size = DamagePopup._fontSize(amount, baseSize);
        const text = scene.add.text(x, y, label, {
            fontSize: size + 'px', fontFamily: 'monospace',
            color: Phaser.Display.Color.IntegerToColor(color).rgba,
            fontStyle: 'bold', stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(100);
        scene.tweens.add({ targets: text, y: y - 40, alpha: 0, duration: 800, ease: 'Power2', onComplete: () => text.destroy() });
    }

    static showCritical(scene, x, y, amount) {
        const label = typeof amount === 'string' ? amount + '!' : Math.floor(amount) + '!';
        const size = DamagePopup._fontSize(amount, 22);
        const text = scene.add.text(x, y, label, {
            fontSize: size + 'px', fontFamily: 'monospace', color: '#ffff00',
            fontStyle: 'bold', stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(100).setScale(1.5);
        scene.tweens.add({ targets: text, y: y - 50, alpha: 0, scaleX: 1, scaleY: 1, duration: 1000, onComplete: () => text.destroy() });
    }

    static showDodge(scene, x, y) {
        const text = scene.add.text(x, y, 'DODGE!', {
            fontSize: '16px', fontFamily: 'monospace', color: '#44ffff',
            fontStyle: 'bold', stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(100);
        scene.tweens.add({ targets: text, y: y - 35, alpha: 0, duration: 600, onComplete: () => text.destroy() });
    }

    static showBleed(scene, x, y, amount) {
        const text = scene.add.text(x, y, '-' + Math.floor(amount) + ' 출혈', {
            fontSize: '12px', fontFamily: 'monospace', color: '#ff6666',
            fontStyle: 'bold', stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5).setDepth(100);
        scene.tweens.add({ targets: text, y: y - 30, alpha: 0, duration: 600, onComplete: () => text.destroy() });
    }
}
