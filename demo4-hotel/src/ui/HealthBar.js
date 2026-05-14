class HotelHealthBar {
    constructor(scene, x, y, width, height) {
        this.scene = scene;
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;

        this.graphics = scene.add.graphics();
        this.graphics.setDepth(100);
    }

    update(current, max) {
        this.graphics.clear();
        const ratio = Math.max(0, current / max);

        this.graphics.fillStyle(0x222222, 0.8);
        this.graphics.fillRoundedRect(this.x - 1, this.y - 1, this.width + 2, this.height + 2, 3);

        let color = 0x44ff44;
        if (ratio < 0.25) color = 0xff4444;
        else if (ratio < 0.5) color = 0xffaa44;

        this.graphics.fillStyle(color, 1);
        this.graphics.fillRoundedRect(this.x, this.y, this.width * ratio, this.height, 2);

        this.graphics.lineStyle(1, 0x666666, 0.5);
        this.graphics.strokeRoundedRect(this.x - 1, this.y - 1, this.width + 2, this.height + 2, 3);
    }

    destroy() {
        if (this.graphics) this.graphics.destroy();
    }
}
