const config = {
    type: Phaser.AUTO,
    width: 1280,
    height: 720,
    parent: document.body,
    backgroundColor: '#0a0a0a',
    scene: [TitleScene, CityScene, ResultScene],
    input: {
        keyboard: true,
        mouse: true,
    },
    scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH,
    },
};

const game = new Phaser.Game(config);
