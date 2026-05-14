class GaugeBar {
    constructor(scene, x, y, width, height, color) {
        this.scene = scene;
        this.width = width;
        this.height = height;

        this.bg = scene.add.rectangle(x, y, width, height, 0x222233).setOrigin(0.5).setDepth(10);
        this.bar = scene.add.rectangle(x - width / 2, y, 0, height, color).setOrigin(0, 0.5).setDepth(11);
        this.color = color;
    }

    update(ratio) {
        this.bar.width = this.width * Math.min(1, Math.max(0, ratio));
        if (ratio >= 1) {
            this.bar.setFillStyle(0xffffff);
        } else {
            this.bar.setFillStyle(this.color);
        }
    }

    setPosition(x, y) {
        this.bg.setPosition(x, y);
        this.bar.setPosition(x - this.width / 2, y);
    }

    destroy() {
        this.bg.destroy();
        this.bar.destroy();
    }
}

class HealthBar {
    constructor(scene, x, y, width, height, maxHp) {
        this.scene = scene;
        this.width = width;
        this.height = height;
        this.maxHp = maxHp;
        this.bg = scene.add.rectangle(x, y, width, height, 0x333333).setOrigin(0.5).setDepth(10);
        this.bar = scene.add.rectangle(x - width / 2, y, width, height, 0x44ff44).setOrigin(0, 0.5).setDepth(11);
    }

    update(hp) {
        const ratio = Math.max(0, hp / this.maxHp);
        this.bar.width = this.width * ratio;
        if (ratio > 0.6) this.bar.setFillStyle(0x44ff44);
        else if (ratio > 0.3) this.bar.setFillStyle(0xffaa00);
        else this.bar.setFillStyle(0xff4444);
    }

    setPosition(x, y) {
        this.bg.setPosition(x, y);
        this.bar.setPosition(x - this.width / 2, y);
    }

    destroy() { this.bg.destroy(); this.bar.destroy(); }
}
