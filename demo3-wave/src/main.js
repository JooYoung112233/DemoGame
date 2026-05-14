const config = {
    type: Phaser.CANVAS,
    width: 1280,
    height: 720,
    parent: 'game-container',
    backgroundColor: '#1a1a2e',
    scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH
    },
    physics: { default: 'arcade', arcade: { debug: false } },
    scene: [WaveSetupScene, WaveBattleScene, UpgradeScene, WaveResultScene],
    disableVisibilityChange: true,
    audio: { noAudio: true }
};

const game = new Phaser.Game(config);
