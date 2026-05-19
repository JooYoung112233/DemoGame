const BASE_W = 1280;
const BASE_H = 720;
const CANVAS_W = 1920;
const CANVAS_H = 1080;
const ZOOM = CANVAS_W / BASE_W;

const config = {
    type: Phaser.WEBGL,
    width: CANVAS_W,
    height: CANVAS_H,
    parent: 'game-container',
    backgroundColor: '#0a0a1a',
    pixelArt: false,
    antialias: true,
    audio: { noAudio: true },
    scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH,
    },
    scene: [TitleScene, MorningScene, ShopScene, CommissionScene],
};

window.game = new Phaser.Game(config);

const DPR = window.devicePixelRatio || 1;
const textResolution = DPR * ZOOM;
const origFactory = Phaser.GameObjects.GameObjectFactory.prototype.text;
Phaser.GameObjects.GameObjectFactory.prototype.text = function (x, y, text, style) {
    const t = origFactory.call(this, x, y, text, style);
    t.setResolution(textResolution);
    return t;
};

window.game.events.on('ready', () => {
    const applyZoom = (scene) => {
        scene.cameras.main.setZoom(ZOOM);
        scene.cameras.main.centerOn(BASE_W / 2, BASE_H / 2);
    };
    window.game.scene.scenes.forEach(s => {
        s.events.on('create', () => applyZoom(s));
        s.events.on('wake', () => applyZoom(s));
    });
});
