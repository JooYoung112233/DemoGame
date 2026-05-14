class HealthBar {
    constructor(scene, x, y, width, height, maxHp) {
        this.scene = scene; this.width = width; this.height = height; this.maxHp = maxHp;
        this.bg = scene.add.rectangle(x, y, width, height, 0x333333).setOrigin(0.5).setDepth(10);
        this.bar = scene.add.rectangle(x - width/2, y, width, height, 0x44ff44).setOrigin(0, 0.5).setDepth(11);
    }
    update(hp, newMax) {
        if (newMax) this.maxHp = newMax;
        const ratio = Math.max(0, hp / this.maxHp);
        this.bar.width = this.width * ratio;
        this.bar.setFillStyle(ratio > 0.6 ? 0x44ff44 : ratio > 0.3 ? 0xffaa00 : 0xff4444);
    }
    setPosition(x, y) { this.bg.setPosition(x, y); this.bar.setPosition(x - this.width/2, y); }
    destroy() { this.bg.destroy(); this.bar.destroy(); }
}
