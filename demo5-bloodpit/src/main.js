const config = {
    type: Phaser.CANVAS,
    width: 1280,
    height: 720,
    parent: 'game-container',
    backgroundColor: '#1a0a0a',
    scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH
    },
    render: {
        antialias: true,
        roundPixels: true
    },
    physics: { default: 'arcade', arcade: { debug: false } },
    scene: [BPSetupScene, BPBattleScene, BPDropScene, BPChoiceScene, BPResultScene, BPShopScene, BPForgeScene, BPEventScene, BPVaultScene],
    disableVisibilityChange: true,
    audio: { noAudio: true }
};

const game = new Phaser.Game(config);
