const config = {
    type: Phaser.CANVAS,
    width: 1280,
    height: 720,
    parent: 'game-container',
    backgroundColor: '#0a0a1a',
    scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH
    },
    scene: [HotelTitleScene, HotelBattleScene, HotelUpgradeScene, HotelEventScene, HotelResultScene],
    disableVisibilityChange: true,
    audio: { noAudio: true }
};

const game = new Phaser.Game(config);
