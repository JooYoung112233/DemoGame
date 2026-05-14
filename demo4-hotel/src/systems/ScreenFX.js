class ScreenFX {
    constructor(scene) {
        this.scene = scene;
    }

    shake(intensity, duration) {
        if (this.scene.cameras && this.scene.cameras.main) {
            this.scene.cameras.main.shake(duration || 100, (intensity || 3) / 1000);
        }
    }

    flash(color, duration) {
        if (this.scene.cameras && this.scene.cameras.main) {
            const hex = typeof color === 'number' ? color : 0xffffff;
            this.scene.cameras.main.flash(duration || 200,
                (hex >> 16) & 0xff, (hex >> 8) & 0xff, hex & 0xff);
        }
    }
}
