const config = {
    type: Phaser.CANVAS,
    width: 1280,
    height: 720,
    parent: 'game-container',
    backgroundColor: '#1a1510',
    scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH
    },
    physics: { default: 'arcade', arcade: { debug: false } },
    scene: [LCSetupScene, LCTrainScene, LCBattleScene, LCRepairScene, LCResultScene],
    disableVisibilityChange: true,
    audio: { noAudio: true }
};

const game = new Phaser.Game(config);
