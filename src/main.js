const config = {
    type: Phaser.CANVAS,
    width: 1280,
    height: 720,
    parent: 'game-container',
    backgroundColor: '#1a1a2e',
    scene: [SetupScene, BattleScene, ResultScene],
    physics: {
        default: 'arcade',
        arcade: { debug: false }
    },
    scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH
    },
    render: {
        pixelArt: true,
        antialias: false
    },
    audio: {
        noAudio: true
    },
    disableVisibilityChange: true
};

const game = new Phaser.Game(config);
