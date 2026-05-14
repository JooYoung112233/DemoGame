class HealthBar {
    constructor(scene, x, y, width, height, maxHp) {
        this.scene = scene;
        this.width = width;
        this.height = height;
        this.maxHp = maxHp;

        this.bg = scene.add.rectangle(x, y, width, height, 0x333333).setOrigin(0.5);
        this.bar = scene.add.rectangle(x - width / 2, y, width, height, 0x44ff44).setOrigin(0, 0.5);
        this.shieldBar = scene.add.rectangle(x - width / 2, y, 0, height, 0x88ccff).setOrigin(0, 0.5);
        this.shieldBar.setAlpha(0.7);

        this.bg.setDepth(10);
        this.bar.setDepth(11);
        this.shieldBar.setDepth(12);
    }

    update(hp, shieldAmount) {
        const ratio = Math.max(0, hp / this.maxHp);
        this.bar.width = this.width * ratio;

        if (ratio > 0.6) this.bar.setFillStyle(0x44ff44);
        else if (ratio > 0.3) this.bar.setFillStyle(0xffaa00);
        else this.bar.setFillStyle(0xff4444);

        if (shieldAmount > 0) {
            const shieldRatio = Math.min(shieldAmount / this.maxHp, 1 - ratio);
            this.shieldBar.x = this.bar.x + this.bar.width;
            this.shieldBar.width = this.width * shieldRatio;
            this.shieldBar.setVisible(true);
        } else {
            this.shieldBar.setVisible(false);
        }
    }

    setPosition(x, y) {
        this.bg.setPosition(x, y);
        this.bar.setPosition(x - this.width / 2, y);
        this.shieldBar.setPosition(x - this.width / 2 + this.bar.width, y);
    }

    destroy() {
        this.bg.destroy();
        this.bar.destroy();
        this.shieldBar.destroy();
    }
}
