const config = {
    type: Phaser.CANVAS,
    width: 1280,
    height: 720,
    parent: 'game-container',
    backgroundColor: '#0a0a1a',
    scene: [TitleScene, TownScene, RecruitScene, RosterScene, StorageScene, ForgeScene, AuctionScene, TrainingScene, TempleScene, IntelScene, EliteRecruitScene, DeployScene, PlaceholderBattleScene, BattleScene, CargoBattleScene, BlackoutBattleScene, RunResultScene],
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
window.game = game;
